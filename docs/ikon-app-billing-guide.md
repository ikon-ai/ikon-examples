# Ikon.App.Billing Guide

Stripe-backed billing for Ikon apps — checkout, subscriptions, refunds, embedded payments, marketplace splits — without owning a payments backend. The library handles the Stripe surface; your app keeps owning *what* it sells and *who* it sells to.

## TL;DR — what you wire

```csharp
// 1. Implement IBillingAppAdapter — three callbacks
public sealed class MyAppBillingAdapter(MyApp app) : IBillingAppAdapter
{
    public Task<BillingPlanDescriptor?> GetPlanAsync(string planId, CancellationToken ct)
        => Task.FromResult<BillingPlanDescriptor?>(planId switch
        {
            "pro"  => BillingPlanDescriptor.Subscription(planId, "price_pro_monthly"),
            "team" => BillingPlanDescriptor.Subscription(planId, "price_team_monthly"),
            _      => null,
        });

    public async Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken ct)
    {
        if (app.Db.TryGetCustomer(appCustomerKey, out var cid)) return cid;
        var newId = await app.Billing.CreateCustomerAsync(new BillingCustomerInfo { Email = email });
        app.Db.PersistCustomer(appCustomerKey, newId);
        return newId;
    }

    public Task ApplyEventAsync(BillingEvent evt, CancellationToken ct)
        => app.Db.ApplyBillingEvent(evt);    // dedupe on evt.EventId
}

// 2. Construct the service once at startup
var options = BillingAppHelpers.AutoDetectFromApp(app, defaultAppId: "my-app") with
{
    DefaultSuccessUrl = "https://my-app.ikonai.app/billing/success",
    DefaultCancelUrl  = "https://my-app.ikonai.app/billing/cancel",
};

if (options.Provider == BillingProvider.Disabled) { /* show CTA, return */ }

_billing = new BillingService(options, new MyAppBillingAdapter(this));

// 3. Drive checkout from app code
var session = await _billing.CreateCheckoutAsync(
    planId: "pro",
    appCustomerKey: currentUserId,
    email: currentUserEmail);
await ClientFunctions.SetUrlAsync(session.Url);
```

That's the whole surface. Webhooks land at a `[Function(Webhook = true)]` you forward to `_billing.HandleWebhookAsync` — see [Webhook lifecycle](#webhook-lifecycle).

## What you do once per app

```bash
# Pick a mode (ikon-connect = zero-config, byok = your own Stripe account)
ikon app secret set BILLING_PROVIDER ikon-connect

# Frontend Stripe.js needs the publishable key in both modes
ikon app secret set STRIPE_PUBLISHABLE_KEY pk_test_...
```

`ikon-connect` is the default — apps onboard as a Stripe Connect sub-account under Ikon's platform Stripe account. No extra setup. See [Billing provider modes](#billing-provider-modes) for the BYOK variant.

## Mental model

| Layer | What it owns |
|---|---|
| **Stripe** | Source of truth for money — customers, subscriptions, invoices, refunds, payouts. The library never duplicates Stripe state. |
| **Ikon backend** (`ikon-connect` only) | Proxies Stripe API calls; attaches `Stripe-Account` per app; routes the connected-account onboarding + payout pipeline. Transparent to the app. |
| **`Ikon.App.Billing`** | The C# façade. Builds checkout sessions, manages subscriptions, verifies webhooks, normalizes events into `BillingEvent`. Stateless — no DB writes. |
| **Your app + `IBillingAppAdapter`** | Owns plan catalog, customer mapping (`appCustomerKey` → Stripe `cus_…`), entitlement persistence, idempotency on event ids. The library calls back via three adapter methods. |

The library is provider-neutral on the public surface (`Billing*` types) so a non-Stripe backend can be added later without breaking apps.

## Billing provider modes

Apps pick transport via the `BILLING_PROVIDER` secret. `BillingAppHelpers.AutoDetectFromApp(app)` resolves it once at startup.

| Mode | Selector | When to use | Customer setup | Fee |
|------|----------|-------------|----------------|-----|
| **`ikon-connect`** *(default)* | `BILLING_PROVIDER=ikon-connect` or unset | Default. Customer becomes a connected sub-account on Ikon's Stripe Connect platform. Ikon handles webhooks, dispatch, KYC. | Configure `IKON_BACKEND_BILLING_URL` (defaults to ambient). No Ikon-issued token — the app's standard backend session is reused. | Ikon takes 5% (configurable); Stripe processing fees on top |
| **`byok`** | `BILLING_PROVIDER=byok` | Customer wants full Stripe control, lower fees, or operates where Ikon Connect can't. | Customer creates own Stripe account, sets `STRIPE_API_KEY`, registers own webhooks. | Standard Stripe fees only |

```bash
# ikon-connect (default)
ikon app secret set BILLING_PROVIDER ikon-connect
ikon app secret set IKON_BACKEND_BILLING_URL https://backend.ikonai.live   # optional — ambient if unset
ikon app secret set STRIPE_PUBLISHABLE_KEY pk_live_<ikon-platform-publishable-key>

# byok
ikon app secret set BILLING_PROVIDER byok
ikon app secret set STRIPE_API_KEY sk_test_...
ikon app secret set STRIPE_PUBLISHABLE_KEY pk_test_...
ikon app secret set STRIPE_WEBHOOK_SECRET whsec_...
ikon app secret set STRIPE_CONNECT_WEBHOOK_SECRET whsec_...    # if using marketplace
```

`BillingService` is constructed from these at boot — restart after changing `BILLING_PROVIDER`.

## How the flow works

### BYOK mode

```
┌────────────┐  CreateCheckoutAsync       ┌────────────┐
│ Ikon app   │ ─────────────────────────► │BillingSvc  │
│  server    │                            │            │
│            │  Adapter.GetPlanAsync ◄─── │            │
│            │  Adapter.ResolveCust  ◄─── │            │
│            │                            │            │
│            │                            │  POST      │
│            │                            │  /sessions │
│            │                            │  + sk_test │ ────► Stripe
│            │  session.Url               │            │
│            │ ◄───────────────────────── │            │
│            │                                          
│            │  ClientFunctions.SetUrlAsync ─► browser ─► Stripe-hosted checkout
└────────────┘
```

### ikon-connect mode

```
┌────────────┐  CreateCheckoutAsync       ┌────────────┐    ┌──────────────┐
│ Ikon app   │ ─────────────────────────► │BillingSvc  │ ──►│ Ikon backend │
│  server    │                            │            │    │  (proxy)     │
│            │                            │            │    │              │
│            │                            │            │    │  attaches    │
│            │                            │            │    │  Stripe-     │
│            │                            │            │    │  Account hdr │
│            │                            │            │    │              │ ───► Stripe
│            │  session.Url               │            │ ◄──│              │
│            │ ◄───────────────────────── │            │    └──────────────┘
└────────────┘
```

The app code is identical in both modes — only the constructor's `BillingOptions` differs. The proxy injection in ikon-connect happens inside `BillingService`.

## Multi-app topology

One Ikon org can run many apps, each with its own billing. Each app stays isolated by construction.

```
                   ┌───────────────────────────────────────┐
                   │  Acme org                              │
                   │                                        │
   ┌────────────┐  │   ┌──────────┐  ┌──────────┐  ┌────┐  │
   │ Stripe     │  │   │ App A    │  │ App B    │  │ …  │  │
   │ (Ikon's    │  │   │ payments │  │ payments │  │    │  │
   │  Connect   │  │   └────┬─────┘  └────┬─────┘  └────┘  │
   │  platform) │  │        │             │                │
   │            │  │        ▼             ▼                │
   │  acct_A    │ ◄┼────────┘             │                │
   │  acct_B    │ ◄┼──────────────────────┘                │
   │  acct_…    │  │                                       │
   └────────────┘  └───────────────────────────────────────┘
```

- **`ikon-connect` mode**: each app gets its own Stripe Connect **Express** connected account. Catalog, customers, subscriptions, payouts all scoped per-app. The app's connected-account id persists in a per-app `CloudJson` asset (`<app-name>/billing/connect-account-id.json`) via `AssetBillingConnectAccountStore`.
- **BYOK mode**: each app gets its own `STRIPE_API_KEY`. Total isolation — separate Stripe accounts entirely.

There is no path for one app to see another app's customers, charges, or subscriptions. Isolation is enforced server-side at the Stripe-account boundary.

### What the Express dashboard exposes (ikon-connect mode)

The "Open Stripe dashboard" button mints a one-time `CreateLoginLinkAsync` URL into the connected account's **Express** dashboard. Express is intentionally locked-down:

| Express dashboard shows | Express dashboard does NOT show |
|---|---|
| Balance · payouts · payout schedule | Product catalog · prices |
| Payments (charges) list | Webhooks · API keys |
| Account settings (business info · bank · identity) | Coupons · promo codes |
| Tax forms (US 1099-K) | Customers list |

Catalog stays **platform-managed**: the Ikon app calls `BillingService.CreateProductAsync` / `CreatePriceAsync` with the `Stripe-Account` header injected — products land on the connected account but only the app creates them. The account holder cannot self-edit pricing via Stripe; they go through the app's admin UI (see [Admin surface → Catalog](#admin-surface)).

### Connect account types

Stripe offers three connected-account types **for Connect mode**. Trade-off: more platform control = more eng work + less account-holder Stripe access.

| Type | Onboarding | Account dashboard | Platform control | Used by ikon-connect |
|---|---|---|---|---|
| **Standard** | Stripe-hosted, full Stripe ToS | Full Stripe dashboard | Minimal — account holder owns relationship | no |
| **Express** | Stripe-hosted Express onboarding (lighter KYC UX) | Express dashboard — balance/payouts/payments only, no catalog | Platform owns catalog, webhooks, pricing UX | **yes (default)** |
| **Custom** | Platform-built UI · no Stripe branding | None — platform builds entire UX | Maximum | no |

Switching ikon-connect from Express to Standard: change `connect.CreateExpressAccountAsync(...)` → equivalent Standard call (Stripe API param `type=standard`), accept that connected users can self-edit catalog in their Stripe Dashboard (may diverge from app's product list), update onboarding redirect URLs. Custom requires building full onboarding + dashboard UI; rarely worth it.

### BYOK is *not* Standard Connect

Common confusion. BYOK and Standard Connect look similar (account holder has full Stripe dashboard) but are unrelated:

| | BYOK | Standard Connect | Express Connect |
|---|---|---|---|
| Connect involved? | no | yes | yes |
| Account holder's Stripe dashboard | full | full | Express-only (limited) |
| Platform application fee | no — direct charges | yes | yes |
| `Stripe-Account` header on API calls | no | yes | yes |
| Setup | app's own `STRIPE_API_KEY` | platform key + `ConnectedAccountId` | platform key + `ConnectedAccountId` (proxied via Ikon backend) |
| Stripe fees | Stripe fees only | Stripe fees + platform fee | Stripe fees + platform fee |

Mental model:
- **BYOK** = "I have my own Stripe account, leave me alone." Zero Connect.
- **Standard Connect** = "I have a Stripe account, but I'm playing in someone else's marketplace and they take a cut."
- **Express Connect** = same as Standard but Stripe hides complexity; platform manages catalog/UX.

No clean migration BYOK → Standard Connect — different `BillingProvider`, different secrets, customers/subs don't transfer across accounts. Pick the right mode at app design time.

## How users are identified

Stripe doesn't know about Ikon users — no automatic link between an Ikon user id and a Stripe customer. Mapping is the app's job, via the adapter's `ResolveStripeCustomerIdAsync(appCustomerKey, email, ct)`.

```
   app side                       Stripe side
   ────────                       ───────────
   appCustomerKey                 cus_…
   = your Ikon userId / orgId ──► (Stripe customer object)
     (or tenantId, etc.)

      ┌──────────────────────────────────────────┐
      │  adapter persists the (key → cus_) map   │
      │  in the app DB / asset                    │
      └──────────────────────────────────────────┘
```

`appCustomerKey` is whatever stable id makes sense for the *paying entity* in your app. Most common choices:

| App shape | Use as `appCustomerKey` |
|---|---|
| Per-user paid app (one subscription per user) | Ikon user id |
| Per-org paid app (org pays, all members share) | Ikon org id |
| Multi-tenant SaaS (one Stripe customer per tenant) | Tenant id |
| Guest checkout (one-shot, no account) | `null` |

The library:

1. Calls `adapter.ResolveStripeCustomerIdAsync(appCustomerKey, email)` on every checkout / portal / setup-intent.
2. Adapter returns existing `cus_…` or creates one and persists the (key → cus_) mapping.
3. Library stamps Stripe-side metadata `app_customer_key` on every checkout session so Stripe rows trace back to the app entity.

In ikon-connect mode customers live on the connected account → per-app isolation is automatic. In BYOK mode the key still scopes the customer because the app's Stripe account is dedicated.

Real-world adapter sketch:

```csharp
public async Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken ct)
{
    // appCustomerKey is whatever you choose to pass in — e.g. ikonUserId.ToString()
    if (_db.TryGetCustomer(appCustomerKey, out var cid)) return cid;

    var newId = await _billing.CreateCustomerAsync(new BillingCustomerInfo
    {
        Email = email,
        Metadata = new Dictionary<string, string> { ["app_customer_key"] = appCustomerKey },
    }, idempotencyKey: $"customer-{appCustomerKey}");

    _db.PersistCustomer(appCustomerKey, newId);
    return newId;
}
```

Validation app caveat: the demo uses a fixed `appCustomerKey = "validation-demo-customer"` (single-customer sandbox). Real apps replace that with their per-user / per-org id.

```csharp
public async Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken ct)
{
    if (_db.TryGetCustomer(appCustomerKey, out var cid)) return cid;

    var newId = await _billing.CreateCustomerAsync(new BillingCustomerInfo
    {
        Email = email,
        Metadata = new Dictionary<string, string> { ["app_customer_key"] = appCustomerKey },
    }, idempotencyKey: $"customer-{appCustomerKey}");

    _db.PersistCustomer(appCustomerKey, newId);
    return newId;
}
```

## Adapter contract

`IBillingAppAdapter` has three methods. The library calls them; the app owns persistence.

| Method | When called | App responsibility |
|--------|-------------|--------------------|
| `GetPlanAsync(planId, ct)` | Inside `CreateCheckoutAsync` / `OfferCheckoutAsync` / `CreateEmbeddedCheckoutAsync` | Resolve app plan id → `BillingPlanDescriptor` (Stripe price id, mode, optional metered price id, metadata) |
| `ResolveStripeCustomerIdAsync(appCustomerKey, email, ct)` | Same call sites + setup intents | Return existing Stripe customer id or create one and persist the mapping |
| `ApplyEventAsync(BillingEvent, ct)` | After webhook signature verification | Update app DB; dedupe on `BillingEvent.EventId` |

The library never reads or writes the app DB itself. Returning `null` from `GetPlanAsync` causes the caller to throw `BillingException(unknown plan)`.

`AutoDetectFromApp` + `AssetBillingConnectAccountStore` exist so apps don't need to hand-roll provider auto-detect and Connect-account-id persistence — both are zero-config defaults.

## Webhook lifecycle

Wire two `[Function(Webhook = true)]` endpoints. Stripe issues a separate signing secret per endpoint type — **Account** events go to the platform endpoint, **Connect** events (per-connected-account) to the Connect endpoint.

```csharp
[Function(Webhook = true, Name = "stripe")]
public async Task<string> StripeWebhook(
    Dictionary<string, string> queryParams,
    Dictionary<string, string> headers,
    string body)
{
    headers.TryGetValue("Stripe-Signature", out var signature);
    var result = await _billing.HandleWebhookAsync(signature, body);
    if (!result.Verified) Log.Instance.Warning($"Unverified: {result.Reason}");
    return """{"received":true}""";
}

[Function(Webhook = true, Name = "stripe-connect")]
public async Task<string> StripeConnectWebhook(
    Dictionary<string, string> queryParams,
    Dictionary<string, string> headers,
    string body)
{
    headers.TryGetValue("Stripe-Signature", out var signature);
    var result = await _billingViaConnect.HandleWebhookAsync(signature, body);
    return """{"received":true}""";
}
```

Delivery → verification → adapter dispatch:

```
   Stripe                 [Function]               BillingService          IBillingAppAdapter
   ──────                 ──────────               ──────────────          ──────────────────
   POST signed body ────► StripeWebhook  ────────► HandleWebhookAsync
                                                   │
                                                   ├── verify signature
                                                   │
                                                   ├── parse → BillingEvent
                                                   │
                                                   └── invoke ──────────► ApplyEventAsync(evt)
                                                                          (dedupe on evt.EventId)
                          return {received:true}
                          (ALWAYS 200, even on
                           verify/adapter failure)
```

`HandleWebhookAsync` returns `BillingWebhookResult { Verified, Reason, AdapterError }`. It never throws — return 200 either way to avoid Stripe retry storms. The library logs unverified events but does not invoke the adapter for them.

**Webhook URL**: register the function's public URL in the Stripe Dashboard. The validation app's Admin tab → Webhook configuration card surfaces the live URL.

### Event types

| `BillingEventType` | Stripe event(s) |
|--------------------|-----------------|
| `CheckoutCompleted` | `checkout.session.completed` |
| `InvoicePaid` | `invoice.paid`, `invoice.payment_succeeded` |
| `InvoicePaymentFailed` | `invoice.payment_failed` |
| `InvoiceFinalized` | `invoice.finalized` |
| `PaymentActionRequired` | `invoice.payment_action_required` (3DS / SCA) |
| `SubscriptionUpdated` | `customer.subscription.created`, `customer.subscription.updated` |
| `SubscriptionDeleted` | `customer.subscription.deleted` |
| `SubscriptionTrialWillEnd` | `customer.subscription.trial_will_end` |
| `ChargeRefunded` | `charge.refunded` |
| `ChargeDisputed` | `charge.dispute.created` |
| `ChargeDisputeClosed` | `charge.dispute.closed` |
| `SetupIntentSucceeded` | `setup_intent.succeeded` |
| `PaymentMethodAttached` | `payment_method.attached` |
| `CreditNoteCreated` | `credit_note.created` |
| `CreditNoteVoided` | `credit_note.voided` |
| `Unknown` | anything else (raw payload on `BillingEvent.RawPayload`) |

## Subscription state machine

```
                       CreateCheckoutAsync
                              │
                              ▼
                     ┌──────────────────┐
        ┌────────────│   incomplete     │  (waiting for first payment)
        │            └──────────────────┘
        │                     │ payment ok
        │                     ▼
        │            ┌──────────────────┐  ◄── ResumeSubscriptionAsync
        │            │     active       │ ───┐
        │            └──────────────────┘    │ PauseSubscriptionAsync
        │  payment            │              ▼
        │  fails              │     ┌──────────────────┐
        │       ◄─────────────┤     │     paused       │
        │                     │     └──────────────────┘
        │                     │
        │            ┌──────────────────┐
        ├────────────│    past_due      │  (dunning retries firing)
        │            └──────────────────┘
        │                     │ retries exhausted
        │                     ▼
        ▼            ┌──────────────────┐
   ┌────────────┐    │     unpaid       │  (terminal — invoice abandoned)
   │ incomplete │    └──────────────────┘
   │  _expired  │             │
   └────────────┘             │ CancelSubscriptionAsync OR retries exhausted
                              ▼
                     ┌──────────────────┐
                     │    canceled      │  ─── ResumeCanceledSubscriptionAsync ──┐
                     └──────────────────┘                                        │
                                                                                 ▼
                                                                          (back to active)
```

`SubscriptionUpdated` fires on every state change; `SubscriptionDeleted` fires on `canceled`. Apps gate features off `BillingEntitlement.SubscriptionActive` (active or trialing).

## End-user surface

Six end-user primitives, all backed by Parallax components. Live in the validation app's **End-user actions** tab.

### Discover (`PricingTable`, `PlanCard`)

```csharp
view.PricingTable(plans: _plans.Value, onSelect: async planId =>
{
    var session = await _billing.CreateCheckoutAsync(planId, appCustomerKey, userEmail);
    await ClientFunctions.SetUrlAsync(session.Url);
});
```

`view.PlanCard(plan, onSelect)` is the same callout outside the grid.

### Buy — hosted Checkout

```csharp
var session = await _billing.CreateCheckoutAsync(
    planId: "pro", appCustomerKey: userId, email: userEmail);
// session.Url is a Stripe-hosted page
await ClientFunctions.SetUrlAsync(session.Url);
```

Use `view.CheckoutButton(onCheckout, text)` for one-shot CTAs — handler returns the URL, component redirects.

### Buy — embedded Checkout

Stays inline; no redirect.

```csharp
var embed = await _billing.CreateEmbeddedCheckoutAsync(
    planId: "pro", appCustomerKey: userId, email: userEmail,
    returnUrl: $"{appOrigin}/billing/done?session={{CHECKOUT_SESSION_ID}}");

_clientSecret.Value = embed.ClientSecret;
// In UI:
view.EmbeddedCheckoutFrame(clientSecret: _clientSecret.Value, publishableKey: pubKey);
```

### Buy — tips, one-shot payments, save-card-on-file

```csharp
// Dynamic-amount one-shot
var tip = await _billing.CreateTipCheckoutAsync(amountMinor: 500, currency: "eur", title: "Thanks!");

// Custom in-app payment form (deferred capture, marketplace escrow)
var pi = await _billing.CreatePaymentIntentAsync(amountMinor: 10000, currency: "eur", stripeCustomerId: cid);
view.PaymentIntentFrame(clientSecret: pi.ClientSecret, publishableKey: pubKey);

// Save a card without charging (trial → paid, future off-session)
var setup = await _billing.CreateSetupIntentAsync(cid);
view.SetupIntentFrame(clientSecret: setup.ClientSecret, publishableKey: pubKey);
```

`view.TipPresetGrid(presetsMinor, currencySymbol, onTip)` renders preset amounts.

### Manage (subscriptions, portal, upcoming invoice)

```csharp
var subs = await _billing.ListSubscriptionsAsync(stripeCustomerId: cid, status: "all");
view.SubscriptionList(
    subscriptions: subs,
    onCancel:        id => _billing.CancelSubscriptionAsync(id, immediate: false),
    onPause:         id => _billing.PauseSubscriptionAsync(id),
    onResumeFromPause: id => _billing.ResumeSubscriptionAsync(id),
    onResume:        id => _billing.ResumeCanceledSubscriptionAsync(id));

// Preview a plan change before committing
var preview = await _billing.PreviewUpcomingInvoiceAsync(cid, subscriptionId: subs[0].Id);
view.UpcomingInvoicePreview(preview);
```

Stripe-hosted Customer Portal (BYOK only — Connect mode uses the embedded `ConnectAccountManagementFrame`):

```csharp
view.BillingPortalButton(onOpenPortal: async () =>
{
    var portal = await _billing.CreatePortalAsync(stripeCustomerId: cid);
    return portal.Url;
});
```

### Account history (payment methods, invoices, charges)

```csharp
var pms      = await _billing.ListPaymentMethodsAsync(cid);
var invoices = await _billing.ListInvoicesAsync(stripeCustomerId: cid);
var charges  = await _billing.ListChargesAsync(stripeCustomerId: cid);

view.PaymentMethodList(
    methods: pms,
    onDetach: pmId => _billing.DetachPaymentMethodAsync(pmId),
    onAddCard: async () =>
    {
        var setup = await _billing.CreateSetupIntentAsync(cid);
        _setupIntentClientSecret.Value = setup.ClientSecret;   // mounts SetupIntentFrame inline
    },
    setupIntentClientSecret: _setupIntentClientSecret.Value,
    publishableKey: pubKey);

view.InvoiceList(invoices);
view.ChargeList(charges,
    onRefund: pi => _billing.RefundAsync(pi, idempotencyKey: $"refund-{pi}"));
```

## Admin surface

Seven admin primitives, all programmatic (no one-size-fits-all component). Live in the validation app's **Admin actions** tab.

### Catalog (products + prices + payment links)

```csharp
var productId = await _billing.CreateProductAsync(new BillingProductInfo { Name = "Pro", Description = "Pro tier" });
var priceId   = await _billing.CreatePriceAsync(new BillingPriceInfo {
    ProductId = productId, UnitAmountMinor = 1900, Currency = "eur", RecurringInterval = "month" });

var link = await _billing.CreatePaymentLinkAsync([BillingLineItem.ForPrice(priceId)], allowPromotionCodes: true);
```

For declarative code-first catalogs, use `BillingCatalogSync.SyncFromCatalogClassAsync(typeof(Plans))` — see [Code-bootstrap catalog](#code-bootstrap-catalog).

### Customers

```csharp
var cid = await _billing.CreateCustomerAsync(new BillingCustomerInfo { Email = "biz@acme.com", Name = "Acme" });
await _billing.UpdateCustomerAsync(cid, new BillingCustomerInfo { AddressCountry = "FI" });
var matches = await _billing.SearchCustomersByAppKeyAsync("user-42");
await _billing.AdjustCustomerBalanceAsync(cid, -1500, "eur", "Goodwill credit", idempotencyKey: $"credit-{Guid.NewGuid()}");
```

### Discounts (coupons + promo codes)

```csharp
var couponId = await _billing.CreateCouponAsync(new BillingCouponInfo
{
    Id = "LAUNCH50", Name = "Launch 50%", PercentOff = 50,
    Duration = BillingCouponDuration.Repeating, DurationInMonths = 3,
});
await _billing.CreatePromotionCodeAsync(couponId, code: "LAUNCH50", maxRedemptions: 100);
```

Surface on checkout: `BillingPlanDescriptor.AllowPromotionCodes = true`.

### Invoicing (hosted invoices + credit notes)

```csharp
// B2B net-30 — customer pays via emailed hosted link
var invoice = await _billing.CreateHostedInvoiceAsync(
    stripeCustomerId: cid,
    lines: [BillingLineItem.Dynamic(50000, "eur", "Consulting · April 2026")],
    daysUntilDue: 30, autoSend: true);

// Formal partial refund — handles tax reversal + regenerates PDF
var cn = await _billing.CreateCreditNoteAsync(new BillingCreditNoteInfo
{
    InvoiceId = invoice.Id, AmountMinor = 1500, RefundAmountMinor = 1000, CreditAmountMinor = 500,
    Memo = "Service downtime", Reason = "duplicate",
});
```

### Subscription management

```csharp
await _billing.UpdateSubscriptionItemQuantityAsync(subItemId, 5);    // seat scaling
await _billing.UpdateSubscriptionPriceAsync(subItemId, newPriceId);  // migrate to new price
await _billing.PauseSubscriptionAsync(subId);
await _billing.ResumeSubscriptionAsync(subId);
await _billing.CancelSubscriptionAsync(subId, immediate: false);     // at period end
await _billing.ResumeCanceledSubscriptionAsync(subId);               // un-cancel
```

Stripe prices are immutable — bumping €19 → €25 creates a new price. Migrate active subscribers via `UpdateSubscriptionPriceAsync` (prorate by default).

### Webhook operations

```csharp
// Self-provision endpoints (alternative to Stripe Dashboard)
var ep = await _billing.CreateWebhookEndpointAsync(
    url: $"{appOrigin}/ikon/webhook/stripe",
    enabledEvents: ["invoice.paid", "customer.subscription.updated"]);
// Persist ep.Secret immediately — Stripe returns it only on creation.

// Replay after an outage
var since = DateTimeOffset.UtcNow.AddHours(-24);
foreach (var id in await _billing.ListEventIdsAsync(createdAfter: since, limit: 100))
{
    var evt = await _billing.RetrieveEventAsync(id);
    await _adapter.ApplyEventAsync(evt, ct);
}
```

### Reporting + Connect surfaces

```csharp
var charges  = await _billing.ListChargesAsync(stripeCustomerId: cid, limit: 50);
var invoices = await _billing.ListInvoicesAsync(stripeCustomerId: cid, status: "paid");
```

Connect admin surfaces (embedded — no redirect to stripe.com):

```csharp
var session = await _connect.CreateAccountSessionAsync(new BillingAccountSessionRequest
{
    ConnectedAccountId = acctId,
    AccountManagement = true, Balances = true, Payouts = true,
    Payments = true, NotificationBanner = true, Documents = true,
});
// Frontend:
view.ConnectAccountManagementFrame(session.ClientSecret, pubKey);
view.ConnectBalancesFrame(session.ClientSecret, pubKey);
view.ConnectPayoutsFrame(session.ClientSecret, pubKey);
view.ConnectPaymentsFrame(session.ClientSecret, pubKey);
view.ConnectNotificationBanner(session.ClientSecret, pubKey);
view.ConnectDocumentsFrame(session.ClientSecret, pubKey);     // US only
```

Account session secrets expire ~30 min. The frontend resolver round-trips back to the server (`FetchConnectManagementSecret` `[Function]`) for a fresh secret on each call — see [Frontend wiring](#frontend-wiring).

## Marketplace + Stripe Connect

Two distinct Connect patterns:

| Pattern | Selector | Use when |
|---|---|---|
| **Destination charges** | `BillingDestination` on checkout | Platform owns the customer; routes funds to creator with an `ApplicationFee`. E.g. creator storefront, tip jar. |
| **Platform-managed** | `BillingOptions.ConnectedAccountId` (ikon-connect injects this) | Platform owns the Stripe relationship; each app onboards as a sub-account. All charges run on `Stripe-Account` header. |

```csharp
// Destination charge — fund routing per checkout
await billing.CreateCheckoutAsync(
    planId: "tip-jar", appCustomerKey: null, email: fanEmail,
    destination: new BillingDestination(creatorAcctId, ApplicationFeeAmountMinor: 200));

// Platform-managed — every call runs on behalf of acctId
var billing = new BillingService(new BillingOptions
{
    ApiKey = platformKey, ConnectedAccountId = "acct_xyz",
    PlatformApplicationFeePercent = 5m,
}, adapter);
```

`BillingDestination` and `ConnectedAccountId` are mutually exclusive.

### Embedded Connect onboarding

```csharp
var acctId = await connect.CreateExpressAccountAsync(ownerEmail, "FI");
var session = await connect.CreateAccountSessionAsync(new BillingAccountSessionRequest
{
    ConnectedAccountId = acctId, AccountOnboarding = true, NotificationBanner = true,
});

view.ConnectOnboardingFrame(session.ClientSecret, pubKey);
// After onExit:
var acct = await connect.RetrieveAccountAsync(acctId);
if (acct.ChargesEnabled && acct.PayoutsEnabled) { /* unlock billing flows */ }
```

Identity verification + bank-account linking pop a Stripe-controlled popup window (non-overridable for security). Everything else stays inline.

### Connect webhooks

Register a second endpoint with `connect: true`:

```csharp
var endpoint = await connect.CreateConnectWebhookEndpointAsync(
    url: $"{appOrigin}/ikon/webhook/stripe-connect",
    enabledEvents: ["account.updated", "capability.updated", "invoice.paid", "customer.subscription.updated"]);
// endpoint.Secret → STRIPE_CONNECT_WEBHOOK_SECRET
```

Connect events have a top-level `account` field. The validation app's `StripeWebhook` inspects this to route to the right `BillingService` instance.

## Code-bootstrap catalog

Declare plans in code, let the library provision matching Stripe products + prices.

```csharp
public static class Plans
{
    public static readonly BillingPlanSpec Pro  = BillingPlanSpec.Subscription("pro",  "Pro",  1900, "eur", "month");
    public static readonly BillingPlanSpec Team = BillingPlanSpec.Subscription("team", "Team", 4900, "eur", "month");
}

var sync = new BillingCatalogSync(_billing);
var map = await sync.SyncFromCatalogClassAsync(typeof(Plans));

// In adapter:
public Task<BillingPlanDescriptor?> GetPlanAsync(string planId, CancellationToken ct)
    => Task.FromResult<BillingPlanDescriptor?>(map.TryGetPriceId(planId, out var pid)
        ? BillingPlanDescriptor.Subscription(planId, pid)
        : null);
```

Sync is idempotent: existing rows reused, new rows created. Resolution uses Stripe `lookup_key` (O(1) per plan). Run once at startup (~200ms cold).

## Entitlement gating

`BillingService.GetEntitlementAsync` composes a single `BillingEntitlement` from three sources: Stripe subscriptions, customer metadata (`unlock_{planId}`), and an optional `IBillingCreditStore`.

```csharp
var ent = await _billing.GetEntitlementAsync("pro", appCustomerKey);
if (ent.SubscriptionActive)      Render($"Pro · renews {ent.SubscriptionEndsAt:d}");
else if (ent.UnlockGranted)      Render($"Lifetime · purchased {ent.UnlockGrantedAt:d}");
else                             RenderUpgradeCTA();
```

Three declarative policy attributes gate `[Function]`-registered methods:

```csharp
[Function]
[BillingRequireSubscription("pro")]
public Task<Image> Premium(string prompt) { /* runs only when entitled */ }

[Function]
[BillingRequireUnlock("hd-pack")]
public Task<Image> HighDef(string prompt) { /* gated on one-time unlock */ }

[Function]
[BillingChargeCredits("image-credits", credits: 1)]
public Task<Image> Standard(string prompt) { /* deducts 1 credit on entry */ }
```

| Attribute | Deny code when missing | Other deny codes |
|---|---|---|
| `[BillingRequireSubscription]` | `billing_subscription_required` | `billing_no_user`, `billing_not_initialized` |
| `[BillingRequireUnlock]` | `billing_unlock_required` | same |
| `[BillingChargeCredits]` | `billing_credits_insufficient` | `billing_no_credit_store`, `billing_credits_deduction_error` |

Policies are webhook-driven, **not** polling-driven. They gate + signal — the app's UI catches the deny code, calls `OfferCheckoutAsync`, and waits for the webhook to flip entitlement.

For credit-based products, supply an `IBillingCreditStore` (or set `billing.CreditStore` once at startup):

```csharp
public sealed class MyCreditStore : IBillingCreditStore
{
    public Task<int> GetCreditsAsync(string customerKey, string sku, CancellationToken ct) { /* DB read */ }
    public Task<int> DeductAsync(string customerKey, string sku, int credits, string idemKey, CancellationToken ct) { /* DB write, dedup */ }
    public Task<int> GrantAsync(string customerKey, string sku, int credits, string idemKey, CancellationToken ct) { /* DB write */ }
}
```

## Error catalog

| Symptom | Cause | App fix |
|---|---|---|
| `BillingConfigurationException` at construction | Missing required option (`ApiKey` in BYOK, `IkonAppId` in ikon-connect) | Check secrets; restart after `BILLING_PROVIDER` change. |
| `BillingApiException` `card_declined` | Stripe declined the card | Read `ex.DeclineCode`; surface specific message (`insufficient_funds`, `expired_card`, …). |
| `BillingApiException` `resource_missing` | Object id doesn't exist (deleted product, archived price, bogus `cus_…`) | Refresh listings; treat as a 404 in your UI. |
| `BillingApiException` `parameter_invalid_integer` / 400 | Bad payload — amount ≤ 0, qty 0, malformed URL | Validate input client-side before the call. |
| `BillingException("unknown plan")` | Adapter returned `null` from `GetPlanAsync` | Adapter must map the planId to a price id; check catalog sync ran. |
| Webhook `Verified=false` | Wrong signing secret, body mutated by middleware, or wrong endpoint type | Endpoints come in pairs — Account secret ≠ Connect secret. Verify which secret matches which endpoint. |
| Webhook `Verified=true, AdapterError=<msg>` | Adapter threw (DB transient, deserialization, etc.) | Return 500 to let Stripe retry, or 200 + log + manual replay (`ListEventIdsAsync` + `RetrieveEventAsync` + `adapter.ApplyEventAsync`). |
| Checkout in Connect mode fails before onboarding done | `acct.ChargesEnabled == false` | Gate the end-user surface on charges_enabled; complete KYC first. |
| Customer Portal 400 `No such customer portal configuration` | New Express account, portal disabled | Call `CreatePortalConfigurationAsync` post-onboarding, or surface a "configure portal" CTA. |
| `requires_action` payment intent | 3DS / SCA prompt | Listen for `PaymentActionRequired` event; open invoice's `HostedInvoiceUrl`. |
| Subscription stuck `past_due` | Retries exhausted | Show top-of-app banner with Customer Portal link to update card; downgrade access on `SubscriptionDeleted`. |

`BillingApiException` exposes structured Stripe fields parsed from the response body: `StatusCode`, `ResponseBody`, `ErrorType`, `ErrorCode`, `DeclineCode`, `StripeMessage`, `ParamPath`. Branch on `ErrorCode` / `DeclineCode` — not on string matching `Message`.

```csharp
try { await _billing.CreateCheckoutAsync(...); }
catch (BillingApiException ex) when (ex.ErrorCode == "card_declined")
{
    if (ex.DeclineCode == "insufficient_funds") { /* show top-up prompt */ }
}
```

The validation app puts a **Force error** button next to every action — same SDK call with bad input — so each path above can be triggered live against the sandbox.

## Idempotency

Every public POST method on `BillingService` and `BillingConnectService` accepts a `string? idempotencyKey`. Pass a stable key derived from app state so concurrent replicas, network retries, and pre-commit loops don't duplicate.

| Method | Suggested key |
|---|---|
| `CreateCustomerAsync` | `customer-{appCustomerKey}` |
| `CreateProductAsync` | `product-{stableName}` (used by `BillingCatalogSync`) |
| `CreatePriceAsync` | `price-{lookupKey}-{amountMinor}-{currency}-{interval}` |
| `CreateCheckoutAsync` / `CreateEmbeddedCheckoutAsync` | `checkout-{planId}-{userId}-{minute}` |
| `CreatePaymentIntentAsync` | `pi-{orderId}` |
| `CreateSetupIntentAsync` | `si-{customerId}-{purpose}` |
| `RefundAsync` | `refund-{chargeId}` (required by signature) |
| `CreateExpressAccountAsync` | `acct-{orgId}` |
| `CreateConnectWebhookEndpointAsync` | `webhook-{environment}` |

`BillingCatalogSync` passes deterministic keys for its product / price creates automatically.

Webhook deliveries dedupe on `BillingEvent.EventId`:

```csharp
public async Task ApplyEventAsync(BillingEvent evt, CancellationToken ct)
{
    if (await _seenEvents.HasAsync(evt.EventId)) return;
    await ApplyToDb(evt);
    await _seenEvents.RecordAsync(evt.EventId);
}
```

## Retries and timeouts

Transient failures (429, 5xx, network faults) auto-retry with exponential backoff + jitter on idempotent calls (every GET + any POST with `Idempotency-Key`).

```csharp
new BillingOptions
{
    MaxRetryAttempts = 3,
    RetryBaseDelay = TimeSpan.FromMilliseconds(500),
    RequestTimeout = TimeSpan.FromSeconds(30),
}
```

## Frontend wiring

The Parallax billing components emit custom node types resolved by `@ikonai/sdk-react-ui-billing`. One module registration covers all 8 billing node types.

```bash
npm install @ikonai/sdk-react-ui-billing \
  @stripe/connect-js @stripe/react-connect-js \
  @stripe/stripe-js @stripe/react-stripe-js
```

```tsx
// frontend-node/src/app.tsx
import { registerBillingModule } from '@ikonai/sdk-react-ui-billing';

useIkonApp({
  modules: [registerStandardUiModule, registerBillingModule, /* … */],
});
```

Connect-mode resolvers need three `[Function(Visibility = Shared)]` exports on the app's main class so the frontend can refresh expired account-session secrets:

```csharp
[Function(Name = "FetchConnectOnboardingSecret", Visibility = FunctionVisibility.Shared)]
public Task<string> FetchConnectOnboardingSecretAsync() { ... }

[Function(Name = "FetchConnectManagementSecret", Visibility = FunctionVisibility.Shared)]
public Task<string> FetchConnectManagementSecretAsync() { ... }

[Function(Name = "OnConnectOnboardingExit", Visibility = FunctionVisibility.Shared)]
public Task OnConnectOnboardingExitAsync() { ... }
```

The validation app's `Validation.Billing.Bootstrap.cs` has the reference implementation — mint a fresh `BillingAccountSession` per call.

### Embedded vs hosted

| Flow | Default | Notes |
|---|---|---|
| `EmbeddedCheckoutFrame` | Inline iframe | Pair with `CreateEmbeddedCheckoutAsync`. |
| `ConnectOnboardingFrame` + the 6 Connect surfaces | Inline iframe | Identity/bank linking pops a Stripe-controlled popup (non-overridable). |
| Hosted Checkout (`CreateCheckoutAsync` + `ClientFunctions.SetUrlAsync`) | Same-tab redirect | By Stripe design. Prefer embedded for in-app. |
| Customer Portal (BYOK) | Same-tab redirect | No embedded equivalent in BYOK. In Connect mode use `ConnectAccountManagementFrame`. |
| `CreatePaymentLinkAsync` | External shareable URL | By design — for chat/email/QR distribution. |

## Testing

```bash
stripe listen --forward-to https://localhost:9443/ikon/webhook/stripe
stripe trigger checkout.session.completed
stripe trigger invoice.payment_failed
```

Test card `4242 4242 4242 4242` (any future expiry, any 3-digit CVC, any postal code).

Connect onboarding KYC shortcuts (test mode only, `sk_test_…`):

| Field | Value | Effect |
|---|---|---|
| Date of birth | `1901-01-01` | Verified match |
| SSN / tax-id | `000000000` | Auto-pass |
| ID document | file token `file_identity_document_success` | Auto-verify |
| SMS code | `000-000` | Auto-confirm |

## Common pitfalls

- **Forgetting the second webhook endpoint.** Connect events go to a separate URL with a separate signing secret. Without `STRIPE_CONNECT_WEBHOOK_SECRET`, Connect events deliver but fail signature verification silently.
- **Calling end-user flows before `acct.ChargesEnabled = true`.** Onboarding incomplete = no payments. Gate the end-user UI on the connected-account state.
- **Holding `ConfigService` / secret reads on hot paths.** Read once into a private readonly field at construction. `BillingService` already does this; if you read `STRIPE_PUBLISHABLE_KEY` ad-hoc in handlers, cache it.
- **Mutating Stripe prices.** Prices are immutable — bumping amounts creates a new price. Migrate active subscribers via `UpdateSubscriptionPriceAsync`.
- **Using random idempotency keys.** A random GUID per attempt defeats the point. Derive keys from stable app state (orderId, customerKey, refund target).
- **Returning 500 from the webhook function on adapter errors you can't replay.** Stripe retries every 5xx for ~3 days. If the adapter bug is fixed, return 200 and replay manually via `ListEventIdsAsync` + `RetrieveEventAsync`.
- **Trusting `Message` strings to branch on errors.** Branch on `ex.ErrorCode` / `ex.DeclineCode` — message text drifts.
- **Mixing live + test keys across an app's secrets.** A `pk_live_*` publishable key with an `sk_test_*` API key fails opaquely. Use one mode (`test` or `live`) consistently.
- **Persisting subscription state from API reads.** Stripe is the source of truth; cache only what the adapter projects via `ApplyEventAsync`.

## CLI reference

```bash
# Pick the transport (default ikon-connect)
ikon app secret set BILLING_PROVIDER ikon-connect     # or byok / disabled

# ikon-connect mode
ikon app secret set IKON_BACKEND_BILLING_URL https://backend.ikonai.live   # optional — ambient if unset
ikon app secret set STRIPE_PUBLISHABLE_KEY pk_live_<platform-publishable-key>

# byok mode
ikon app secret set STRIPE_API_KEY sk_test_...
ikon app secret set STRIPE_PUBLISHABLE_KEY pk_test_...
ikon app secret set STRIPE_WEBHOOK_SECRET whsec_...
ikon app secret set STRIPE_CONNECT_WEBHOOK_SECRET whsec_...    # marketplace only

# Stripe CLI for local webhook testing
stripe listen --forward-to https://localhost:9443/ikon/webhook/stripe
stripe trigger checkout.session.completed
```

## Related

- [Ikon Mint Guide](ikon-mint-guide.md) — higher-level creator-monetization layer on top of `Ikon.App.Billing`. Use Mint when you want declarative `Products.cs` + auto-fulfillment; use this guide directly when you need the full Stripe surface.
- [Ikon Signature Guide](ikon-signature-guide.md) — eID-backed document signing, often paired with billing for contract-on-payment flows.
- [Ikon.AI Library Overview](ikon-ai-library-overview.md) — the AI services that policy attributes (`[BillingChargeCredits]`) gate.
- Reference implementation: `Validation.Billing.cs` + `Validation.Billing.Bootstrap.cs` in [Ikon.App.Platform.Validation](../../platform-dotnet/Ikon.App.Platform.Validation). Every primitive in this guide is exercised live against the Stripe sandbox.
