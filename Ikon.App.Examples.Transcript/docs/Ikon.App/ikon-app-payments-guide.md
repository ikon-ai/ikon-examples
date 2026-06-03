# Ikon.App.Payments Guide

Stripe-backed payments for Ikon apps — checkout, subscriptions, refunds, marketplace splits — without owning a payments backend. The library handles the Stripe surface; your app keeps owning _what_ it sells and _who_ it sells to. Customer-facing payment flows redirect to Stripe-hosted pages (Checkout, Customer Portal, KYC, dashboard) — no Stripe.js, no embedded iframes.

## TL;DR — what you wire

```csharp
// 1. Implement IPaymentsAppAdapter — three callbacks
public sealed class MyAppPaymentsAdapter(MyApp app) : IPaymentsAppAdapter
{
    public Task<PaymentsPlanDescriptor?> GetPlanAsync(string planId, CancellationToken ct)
        => Task.FromResult<PaymentsPlanDescriptor?>(planId switch
        {
            "pro"  => PaymentsPlanDescriptor.Subscription(planId, "price_pro_monthly"),
            "team" => PaymentsPlanDescriptor.Subscription(planId, "price_team_monthly"),
            _      => null,
        });

    public async Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken ct)
    {
        if (app.Db.TryGetCustomer(appCustomerKey, out var cid)) return cid;
        var newId = await app.Payments.CreateCustomerAsync(new PaymentsCustomerInfo { Email = email });
        app.Db.PersistCustomer(appCustomerKey, newId);
        return newId;
    }

    public Task ApplyEventAsync(PaymentsEvent evt, CancellationToken ct)
        => app.Db.ApplyPaymentsEvent(evt);    // dedupe on evt.EventId
}

// 2. Construct the service once at startup
var options = PaymentsAppHelpers.AutoDetectFromApp(app, defaultAppId: "my-app") with
{
    DefaultSuccessUrl = "https://my-app.ikonai.app/payments/success",
    DefaultCancelUrl  = "https://my-app.ikonai.app/payments/cancel",
};

if (options.Provider == PaymentsProvider.Disabled) { /* show CTA, return */ }

_payments = new PaymentsService(options, new MyAppPaymentsAdapter(this));

// 3. Drive checkout from app code
var session = await _payments.CreateCheckoutAsync(
    planId: "pro",
    appCustomerKey: currentUserId,
    email: currentUserEmail);
await ClientFunctions.SetUrlAsync(session.Url);
```

That's the whole surface. Webhooks land at a `[Function(Webhook = true)]` you forward to `_payments.HandleWebhookAsync` — see [Webhook lifecycle](#webhook-lifecycle).

## Naming note

The library, CLI verb (`ikon app payments`), C# namespace (`Ikon.App.Payments`), backend lib (`app-payments`), and REST routes (`/apps/:space/payments/*`) all use **payments**. The per-app **secret names keep the `BILLING_*` prefix** — `BILLING_PROVIDER`, `IKON_BACKEND_BILLING_URL`, `IKON_WEBHOOK_SECRET` — for backward compatibility with apps that already set them. Don't be thrown by the mismatch: secrets say `BILLING_`, everything else says `payments`. (The separate `ikon billing` verb and `libs/billing` are a _different_ system — the platform's own org-level Canvas/AI usage credits, unrelated to what your app charges its end users.)

## Provider model

The whole payments surface runs behind an **`IPaymentsProvider`** abstraction so the same app API can target more than one payment platform. This mirrors how `Ikon.AI` abstracts LLM providers (a neutral interface + per-provider implementations + capability flags). The seam is at the **operation level** (`CreateCheckoutAsync`, `ListSubscriptionsAsync`, `RefundAsync`, …) returning neutral `Payments*` DTOs — _not_ at the HTTP transport level, because provider APIs differ fundamentally (Stripe form-encoded `/v1/`; Worldpay JSON + HATEOAS; Vipps JSON + OAuth-token + MSN + wallet redirect). A shared "post a Stripe form to a path" transport can't express them all.

- **C# (`Ikon.App.Payments`)**: `PaymentsService` is a thin façade over the active `IPaymentsProvider`, chosen from `PaymentsOptions.Provider`. `StripePaymentsProvider` holds the BYOK-vs-ikon-connect transport choice internally (that's a Stripe-internal detail, orthogonal to provider selection). The app API and `Payments*` DTOs are identical regardless of provider.
- **Backend (`app-payments` lib)**: `PaymentsProviderFactory` selects the provider for merchant provisioning + webhook routing per space; the binding stores which provider owns each merchant.

| Provider            | Status | Notes                                                                                                                                                                                                                                 |
| ------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Stripe**          | Full   | The only fully implemented provider. Everything else in this guide is Stripe.                                                                                                                                                         |
| **Worldpay**        | Stub   | Capabilities declared; operations throw `PaymentsNotSupportedException`. A future PR fills in the real Worldpay Access calls (JSON + HATEOAS `_links`, Onboarding API, stored-credential recurring) without changing the abstraction. |
| **Vipps MobilePay** | Stub   | Capabilities declared; operations throw. Future impl maps to ePayment (wallet redirect) + Recurring API (agreement + charges) + token/MSN auth.                                                                                       |

Operations a provider doesn't support throw `PaymentsNotSupportedException`. Apps that target multiple providers branch on **`PaymentsService.GetCapabilities()`** first — e.g. `SupportsNativeSubscriptions` (Stripe/PayPal yes; Worldpay/Vipps no), `SupportsCatalog` (Vipps has no products/prices), `SupportsHostedCheckout`, `SupportsProgrammaticOnboarding`. The C# `PaymentsProvider` enum (`Disabled` / `Byok` / `IkonConnect` / `Worldpay` / `Vipps`) selects the provider; `Worldpay`/`Vipps` resolve to throwing stubs until implemented.

## What you do once per app

```bash
# Pick a mode (ikon-connect = zero-config, byok = your own Stripe account)
ikon app secret set BILLING_PROVIDER ikon-connect
```

`ikon-connect` is the default — apps onboard as a Stripe Connect sub-account under Ikon's platform Stripe account. No extra setup. See [Payments provider modes](#payments-provider-modes) for the BYOK variant.

## Mental model

| Layer                                  | What it owns                                                                                                                                                                  |
| -------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Stripe**                             | Source of truth for money — customers, subscriptions, invoices, refunds, payouts. The library never duplicates Stripe state.                                                  |
| **Ikon backend** (`ikon-connect` only) | Proxies Stripe API calls; attaches `Stripe-Account` per app; routes the connected-account onboarding + payout pipeline. Transparent to the app.                               |
| **`Ikon.App.Payments`**                | The C# façade. Builds checkout sessions, manages subscriptions, verifies webhooks, normalizes events into `PaymentsEvent`. Stateless — no DB writes.                          |
| **Your app + `IPaymentsAppAdapter`**   | Owns plan catalog, customer mapping (`appCustomerKey` → Stripe `cus_…`), entitlement persistence, idempotency on event ids. The library calls back via three adapter methods. |

The library is provider-neutral on the public surface (`Payments*` types) so a non-Stripe backend can be added later without breaking apps.

## Payments provider modes

Apps pick transport via the `BILLING_PROVIDER` secret. `PaymentsAppHelpers.AutoDetectFromApp(app)` resolves it once at startup.

| Mode                           | Selector                                 | When to use                                                                                                                | Customer setup                                                                                                                   | Fee                                                         |
| ------------------------------ | ---------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| **`ikon-connect`** _(default)_ | `BILLING_PROVIDER=ikon-connect` or unset | Default. Customer becomes a connected sub-account on Ikon's Stripe Connect platform. Ikon handles webhooks, dispatch, KYC. | Configure `IKON_BACKEND_BILLING_URL` (defaults to ambient). No Ikon-issued token — the app's standard backend session is reused. | Ikon takes 5% (configurable); Stripe processing fees on top |
| **`byok`**                     | `BILLING_PROVIDER=byok`                  | Customer wants full Stripe control, lower fees, or operates where Ikon Connect can't.                                      | Customer creates own Stripe account, sets `STRIPE_API_KEY`, registers own webhooks.                                              | Standard Stripe fees only                                   |

```bash
# ikon-connect (default)
ikon app secret set BILLING_PROVIDER ikon-connect
ikon app secret set IKON_BACKEND_BILLING_URL https://backend.ikonai.live   # optional — ambient if unset

# byok — secret key (sk_) or restricted key (rk_); see "API key types" below
ikon app secret set BILLING_PROVIDER byok
ikon app secret set STRIPE_API_KEY rk_test_...
ikon app secret set STRIPE_WEBHOOK_SECRET whsec_...
ikon app secret set STRIPE_CONNECT_WEBHOOK_SECRET whsec_...    # if using marketplace
```

`PaymentsService` is constructed from these at boot — restart after changing `BILLING_PROVIDER`.

### Regional restrictions

Stripe applies country-specific rules that aren't checked client-side — the library surfaces them as plain 400s. Worth knowing:

| Country / region                    | Restriction                                                                                                                                                         | Library behaviour                                                                                                                                                                                                                                                            |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Brazil ↔ Brazil**                 | Platforms registered in BR cannot collect `application_fee_*` from BR connected accounts. Affects `application_fee_amount` and `application_fee_percent`.           | Stripe returns 400 on the charge call. No client-side check; document this for apps deploying in BR.                                                                                                                                                                         |
| **India (RBI mandate)**             | Recurring card payments require a one-time customer mandate. Plain subscription create on IN cards may fail.                                                        | Drive the mandate via Stripe-hosted Checkout in `setup` mode before creating the subscription; the library has no inline SetupIntent surface.                                                                                                                                |
| **Japan**                           | Three-letter currency code constraints + installment plans gated. JCB cards may require the `cartes_bancaires_payments` capability not the default `card_payments`. | Capability set is per-app — request `jcb_payments` in `PaymentsAccountCapabilities.Merchant` for JP connected accounts.                                                                                                                                                      |
| **EU / UK SCA**                     | Strong Customer Authentication triggers a `requires_action` PI status on cards needing 3DS.                                                                         | Inside Stripe-hosted Checkout / Customer Portal the 3DS challenge is handled by Stripe automatically. Programmatic off-session charges (`CreatePaymentIntentAsync` with `confirm: true`) that hit `requires_action` must be retried via a fresh on-session Checkout session. |
| **Korea / Cartes Bancaires / etc.** | Local payment-method capabilities need explicit enablement on the connected account.                                                                                | Add to `PaymentsAccountCapabilities.Merchant` at account create time.                                                                                                                                                                                                        |

See <https://docs.stripe.com/connect/saas/tasks/app-fees.md> + <https://docs.stripe.com/india-recurring-payments.md> for the Brazil and India cases. None of these are blockers for the default Ikon SaaS posture (EU/US card payments), but apps deploying regionally hit them.

### API key types

The library accepts both Stripe key flavours for `STRIPE_API_KEY` and treats them identically. Restricted keys are recommended for new deployments.

| Prefix                    | Type           | Permissions                           | Recommended            |
| ------------------------- | -------------- | ------------------------------------- | ---------------------- |
| `sk_live_…` / `sk_test_…` | Secret key     | Full account access                   | Legacy / fastest setup |
| `rk_live_…` / `rk_test_…` | Restricted key | Per-resource scopes (least privilege) | ✓                      |

**Recommended restricted-key permission set.** Create the key in [Dashboard → Developers → API Keys → "Create restricted key"](https://dashboard.stripe.com/apikeys/create) with these scopes:

- **Core**: Customers (Write), Products (Write), Prices (Write), Subscriptions (Write), Checkout Sessions (Write), Payment Links (Write), Payment Intents (Write), Payment Methods (Read), Customer Portal (Write), Refunds (Write), Coupons (Write), Promotion Codes (Write), Credit Notes (Write), Invoices (Write), Tax IDs (Write).
- **Connect** (only if the app uses marketplace flows): Accounts (Write), Account Links (Write), Account Sessions (Write), Transfers (Write).
- **Optional**: Apple Pay Domains (Write — if the app verifies Apple Pay), Webhook Endpoints (Read — if the app reads its own webhook registrations).
- **Leave at None**: Files, Radar, Issuing, Treasury, anything else the app doesn't use.

Apps that don't use Connect or a particular feature can leave those rows at None — the library's calls degrade with a `403` you'll catch in dev.

If a key is exposed, [roll it immediately](https://dashboard.stripe.com/apikeys) and review request logs. The restricted-key blast radius is the scope you assigned, not the whole account — one more reason to prefer `rk_…`.

## How the flow works

### BYOK mode

```
┌────────────┐  CreateCheckoutAsync       ┌────────────┐
│ Ikon app   │ ─────────────────────────► │PaymentsSvc  │
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
│ Ikon app   │ ─────────────────────────► │PaymentsSvc  │ ──►│ Ikon backend │
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

The app code is identical in both modes — only the constructor's `PaymentsOptions` differs. The proxy injection in ikon-connect happens inside `PaymentsService`.

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

- **`ikon-connect` mode**: each app gets its own Stripe Connect connected account created via Accounts v2 (`POST /v2/core/accounts`) in the Stripe-managed posture (`dashboard=full`, `defaults.responsibilities.fees_collector=stripe`, `defaults.responsibilities.losses_collector=stripe`). Stripe handles KYC, processing fees, loss liability, and the full merchant dashboard at `https://dashboard.stripe.com/`. The Ikon platform backend owns the create call + the v2 AccountLinks redirect; apps do not drive Stripe write operations themselves. The space↔account binding lives on `acct.metadata.ikon_space` (Stripe metadata = source of truth).
- **BYOK mode**: each app gets its own `STRIPE_API_KEY`. Total isolation — separate Stripe accounts entirely.

There is no path for one app to see another app's customers, charges, or subscriptions. Isolation is enforced server-side at the Stripe-account boundary.

### What the full Stripe Dashboard exposes (ikon-connect mode)

Once KYC completes, the merchant logs into `https://dashboard.stripe.com/{acct_id}` with the email used at onboarding. The full Stripe Dashboard exposes everything Stripe offers:

| Full dashboard surfaces                                |
| ------------------------------------------------------ |
| Product catalog · prices · promotion codes · coupons   |
| Customers · subscriptions · invoices · payment methods |
| Payments (charges) list · disputes · refunds           |
| Balance · payouts · payout schedule · bank accounts    |
| Webhooks · API keys · tax settings                     |
| Account settings (business info · identity · branding) |

All admin operations are driven by the merchant in their own Stripe Dashboard — the Ikon platform proxy rejects admin mutations (`POST/DELETE` on `/v1/products`, `/v1/prices`, `/v1/coupons`, `/v1/promotion_codes`, `/v1/tax_ids`, `/v1/credit_notes`, `/v1/webhook_endpoints`, `/v1/payment_links`, `/v1/apple_pay/domains`) with 403. Customer-facing payment operations (`/v1/checkout/sessions`, `/v1/payment_intents`, `/v1/subscriptions`, `/v1/invoices`, `/v1/payment_methods`, `/v1/charges`) plus all `/v2/*` and read-only `GET /v1/*` remain allowed.

### Connect account configuration (Accounts v2)

Stripe v2 replaced the legacy `type: "standard" | "express" | "custom"` account labels with **three independent axes**:

1. **Dashboard access** (`dashboard`) — `full` (account holder gets the full Stripe Dashboard), `express` (limited dashboard — balance/payouts/payments only, no catalog UI), or `none` (platform builds the entire UI).
2. **Responsibilities** (`defaults.responsibilities`) — `fees_collector` ∈ {`stripe`, `application`} and `losses_collector` ∈ {`stripe`, `application`}. Controls who pays Stripe fees and who covers negative balances.
3. **Capabilities** (`configuration.merchant/customer/recipient.capabilities`) — fine-grained per-feature flags (`card_payments`, `automatic_indirect_tax`, etc.). Each capability transitions through `active` / `pending` / `inactive` / `restricted`.

Ikon-connect locked configuration (set at create time by the platform backend, immutable post-create):

| Axis                                                | Value     | Why                                                                         |
| --------------------------------------------------- | --------- | --------------------------------------------------------------------------- |
| `dashboard`                                         | `full`    | Stripe owns merchant UX; merchant logs into `dashboard.stripe.com` directly |
| `defaults.responsibilities.fees_collector`          | `stripe`  | Stripe collects processing fees from the connected account                  |
| `defaults.responsibilities.losses_collector`        | `stripe`  | Stripe absorbs negative balances; platform avoids liability                 |
| `configuration.merchant.capabilities.card_payments` | requested | Minimum capability for charging                                             |

Platform revenue model: with `fees_collector=stripe`, the platform still earns via per-transaction application fees (`application_fee_amount` on Checkout/PaymentIntent, `application_fee_percent` on Subscription). Set the per-app rate via `PaymentsOptions.PlatformApplicationFeePercent` (default 5%). Stripe routes the application fee to the platform balance on capture, separate from the processing fees the connected account pays.

### BYOK is _not_ Connect

Common confusion. BYOK and a connected account configured with `dashboard: "full"` look similar (account holder has full Stripe dashboard) but are unrelated:

|                                      | BYOK                                          | Connect, `dashboard: "full"`           | Connect, `dashboard: "express"`                                |
| ------------------------------------ | --------------------------------------------- | -------------------------------------- | -------------------------------------------------------------- |
| Connect involved?                    | no                                            | yes                                    | yes                                                            |
| Account holder's Stripe dashboard    | full                                          | full                                   | Express-only (limited)                                         |
| Platform application fee             | no — direct charges to app's own account      | yes                                    | yes                                                            |
| `Stripe-Account` header on API calls | no                                            | yes (direct charges)                   | yes (direct charges)                                           |
| Setup                                | app's own `STRIPE_API_KEY` (rk\_ recommended) | platform key + `ConnectedAccountId`    | platform key + `ConnectedAccountId` (proxied via Ikon backend) |
| Stripe fees                          | Stripe fees only                              | Stripe fees + platform application fee | Stripe fees + platform application fee                         |

Mental model:

- **BYOK** = "I have my own Stripe account, leave me alone." Zero Connect.
- **Connect, full dashboard** = "I have a Stripe account, but I'm playing in someone else's marketplace and they take a cut. I keep my own Dashboard." Default for ikon-connect.
- **Connect, express dashboard** = same as above but Stripe hides complexity; platform manages catalog/UX. Not used by ikon-connect.

No clean migration BYOK → Connect — different `PaymentsProvider`, different secrets, customers/subs don't transfer across accounts. Pick the right mode at app design time.

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

`appCustomerKey` is whatever stable id makes sense for the _paying entity_ in your app. Most common choices:

| App shape                                          | Use as `appCustomerKey` |
| -------------------------------------------------- | ----------------------- |
| Per-user paid app (one subscription per user)      | Ikon user id            |
| Per-org paid app (org pays, all members share)     | Ikon org id             |
| Multi-tenant SaaS (one Stripe customer per tenant) | Tenant id               |
| Guest checkout (one-shot, no account)              | `null`                  |

The library:

1. Calls `adapter.ResolveStripeCustomerIdAsync(appCustomerKey, email)` on every checkout / portal / setup-intent.
2. Adapter returns existing `cus_…` or creates one and persists the (key → cus\_) mapping.
3. Library stamps Stripe-side metadata `app_customer_key` on every checkout session so Stripe rows trace back to the app entity.

In ikon-connect mode customers live on the connected account → per-app isolation is automatic. In BYOK mode the key still scopes the customer because the app's Stripe account is dedicated.

Real-world adapter sketch:

```csharp
public async Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken ct)
{
    // appCustomerKey is whatever you choose to pass in — e.g. ikonUserId.ToString()
    if (_db.TryGetCustomer(appCustomerKey, out var cid)) return cid;

    var newId = await _payments.CreateCustomerAsync(new PaymentsCustomerInfo
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

    var newId = await _payments.CreateCustomerAsync(new PaymentsCustomerInfo
    {
        Email = email,
        Metadata = new Dictionary<string, string> { ["app_customer_key"] = appCustomerKey },
    }, idempotencyKey: $"customer-{appCustomerKey}");

    _db.PersistCustomer(appCustomerKey, newId);
    return newId;
}
```

## Adapter contract

`IPaymentsAppAdapter` has three methods. The library calls them; the app owns persistence.

| Method                                                    | When called                                         | App responsibility                                                                                          |
| --------------------------------------------------------- | --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| `GetPlanAsync(planId, ct)`                                | Inside `CreateCheckoutAsync` / `OfferCheckoutAsync` | Resolve app plan id → `PaymentsPlanDescriptor` (Stripe price id, mode, optional metered price id, metadata) |
| `ResolveStripeCustomerIdAsync(appCustomerKey, email, ct)` | Same call sites                                     | Return existing Stripe customer id or create one and persist the mapping                                    |
| `ApplyEventAsync(PaymentsEvent, ct)`                      | After webhook signature verification                | Update app DB; dedupe on `PaymentsEvent.EventId`                                                            |

The library never reads or writes the app DB itself. Returning `null` from `GetPlanAsync` causes the caller to throw `PaymentsException(unknown plan)`.

`AutoDetectFromApp` + `AssetStripeMerchantStore` exist so apps don't need to hand-roll provider auto-detect and Connect-account-id persistence — both are zero-config defaults.

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
    var result = await _payments.HandleWebhookAsync(signature, body);
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
    var result = await _paymentsViaConnect.HandleWebhookAsync(signature, body);
    return """{"received":true}""";
}
```

Delivery → verification → adapter dispatch:

```
   Stripe                 [Function]               PaymentsService          IPaymentsAppAdapter
   ──────                 ──────────               ──────────────          ──────────────────
   POST signed body ────► StripeWebhook  ────────► HandleWebhookAsync
                                                   │
                                                   ├── verify signature
                                                   │
                                                   ├── parse → PaymentsEvent
                                                   │
                                                   └── invoke ──────────► ApplyEventAsync(evt)
                                                                          (dedupe on evt.EventId)
                          return {received:true}
                          (ALWAYS 200, even on
                           verify/adapter failure)
```

`HandleWebhookAsync` returns `PaymentsWebhookResult { Verified, Reason, AdapterError }`. It never throws — return 200 either way to avoid Stripe retry storms. The library logs unverified events but does not invoke the adapter for them.

**Webhook URL**: register the function's public URL in the Stripe Dashboard. The validation app's Admin tab → Webhook configuration card surfaces the live URL.

### Event types

| `PaymentsEventType`                                               | Stripe event(s)                                                                                                         |
| ----------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `CheckoutCompleted`                                               | `checkout.session.completed`                                                                                            |
| `CheckoutAsyncPaymentSucceeded`                                   | `checkout.session.async_payment_succeeded` (fulfil here for async PMs)                                                  |
| `CheckoutAsyncPaymentFailed`                                      | `checkout.session.async_payment_failed`                                                                                 |
| `InvoicePaid`                                                     | `invoice.paid`, `invoice.payment_succeeded`                                                                             |
| `InvoicePaymentFailed`                                            | `invoice.payment_failed`                                                                                                |
| `InvoiceFinalized`                                                | `invoice.finalized`                                                                                                     |
| `PaymentActionRequired`                                           | `invoice.payment_action_required` (3DS / SCA)                                                                           |
| `SubscriptionUpdated`                                             | `customer.subscription.created`, `customer.subscription.updated`                                                        |
| `SubscriptionDeleted`                                             | `customer.subscription.deleted`                                                                                         |
| `SubscriptionTrialWillEnd`                                        | `customer.subscription.trial_will_end`                                                                                  |
| `SubscriptionScheduleUpdated`                                     | `subscription_schedule.*` (created / updated / canceled / released / expiring / completed)                              |
| `ChargeRefunded`                                                  | `charge.refunded`                                                                                                       |
| `ChargeDisputed`                                                  | `charge.dispute.created`                                                                                                |
| `ChargeDisputeClosed`                                             | `charge.dispute.closed`                                                                                                 |
| `SetupIntentSucceeded`                                            | `setup_intent.succeeded`                                                                                                |
| `PaymentMethodAttached`                                           | `payment_method.attached`                                                                                               |
| `CreditNoteCreated`                                               | `credit_note.created`                                                                                                   |
| `CreditNoteVoided`                                                | `credit_note.voided`                                                                                                    |
| `ConnectAccountUpdated`                                           | `v2.core.account.updated`, `v2.core.account.created` (also legacy `account.updated`, flagged `IsLegacyEventName: true`) |
| `ConnectAccountRequirementsUpdated`                               | `v2.core.account[requirements].updated` (gate "billing ready" UI on this)                                               |
| `ConnectAccountCapabilityUpdated`                                 | `v2.core.account_capability.updated` (per-capability transitions: active / pending / inactive / restricted)             |
| `ConnectOAuthAuthorized`                                          | `account.application.authorized`                                                                                        |
| `ConnectOAuthDeauthorized`                                        | `account.application.deauthorized`                                                                                      |
| `PayoutCreated` / `PayoutUpdated` / `PayoutPaid` / `PayoutFailed` | `payout.created` / `payout.updated` / `payout.paid` / `payout.failed`                                                   |
| `Unknown`                                                         | anything else (raw payload on `PaymentsEvent.RawPayload`)                                                               |

### Snapshot vs thin payloads (v2)

Stripe ships every event in one of two shapes. The library reads both:

|                             | Snapshot (v1-style)                                                    | Thin (v2)                                                                                          |
| --------------------------- | ---------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `object` field on root      | (e.g. `checkout.session`)                                              | `"v2.core.event"`                                                                                  |
| Payload size                | full object snapshot embedded in `data.object`                         | minimal — only `related_object.{id, type, url}`                                                    |
| Library populates           | `CustomerId`, `SubscriptionId`, `Status`, period fields, etc. directly | `RelatedObjectId`, `RelatedObjectType`, `RelatedObjectUrl` (app fetches the full object if needed) |
| `PaymentsEvent.IsThinEvent` | `false`                                                                | `true`                                                                                             |
| Schema drift                | snapshot follows the version pinned at endpoint create time            | always current — Stripe re-fetches on read                                                         |

Choose at registration time via `PaymentsWebhookPayloadShape`:

```csharp
await payments.CreateWebhookEndpointAsync(
    url: "https://app/wh",
    enabledEvents: new[] { "v2.core.account.updated", "checkout.session.completed" },
    payloadShape: PaymentsWebhookPayloadShape.Thin);   // default = Snapshot
```

For thin events, dispatch:

```csharp
public async Task ApplyEventAsync(PaymentsEvent evt, CancellationToken ct)
{
    if (evt.IsThinEvent && evt.RelatedObjectUrl is not null)
    {
        var json = await _connect.FetchRelatedObjectAsync(evt.RelatedObjectUrl, ct);
        // parse the full object yourself — Stripe returns the current state
    }
    else
    {
        // snapshot path: read evt.CustomerId / evt.SubscriptionId / evt.CurrentPeriod* etc.
    }
}
```

Snapshot is the default — keep it unless you specifically want thin payloads (smaller deliveries, no version drift). The library's typed fields (`CustomerId`, `SubscriptionId`, etc.) are populated only on snapshot events; thin-event consumers must do the follow-up GET.

### Ping a registered endpoint

Stripe ships a diagnostic ping. Call after registering to verify the HTTP plumbing + signature secret without waiting for a real event:

```csharp
await payments.PingWebhookEndpointAsync(endpointId);
// → Stripe POSTs a synthetic v2.core.event_destination.ping event to the registered URL
```

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

`SubscriptionUpdated` fires on every state change; `SubscriptionDeleted` fires on `canceled`. Apps gate features off `PaymentsEntitlement.SubscriptionActive` (active or trialing).

## End-user surface

Six end-user primitives, all backed by Parallax components. Live in the validation app's **End-user actions** tab.

### Discover (`PricingTable`, `PlanCard`)

```csharp
view.PricingTable(plans: _plans.Value, onSelect: async planId =>
{
    var session = await _payments.CreateCheckoutAsync(planId, appCustomerKey, userEmail);
    await ClientFunctions.SetUrlAsync(session.Url);
});
```

`view.PlanCard(plan, onSelect)` is the same callout outside the grid.

### Buy — hosted Checkout

```csharp
var session = await _payments.CreateCheckoutAsync(
    planId: "pro", appCustomerKey: userId, email: userEmail);
// session.Url is a Stripe-hosted page
await ClientFunctions.SetUrlAsync(session.Url);
```

Use `view.CheckoutButton(onCheckout, text)` for one-shot CTAs — handler returns the URL, component redirects.

### Buy — tips and one-shot payments

```csharp
// Dynamic-amount one-shot — opens hosted Stripe Checkout in a new tab
var tip = await _payments.CreateTipCheckoutAsync(amountMinor: 500, currency: "eur", title: "Thanks!");
await ClientFunctions.OpenExternalUrlAsync(tip.Url);

// Off-session capture against a saved card (marketplace escrow, scheduled charges)
var pi = await _payments.CreatePaymentIntentAsync(
    amountMinor: 10000, currency: "eur", stripeCustomerId: cid,
    paymentMethodId: savedPmId, confirm: true);
// pi.Status ∈ { "succeeded", "requires_action", "requires_payment_method" }
```

`view.TipPresetGrid(presetsMinor, currencySymbol, onTip)` renders preset amounts.

To **save a new card without charging**, open a Stripe Checkout session in `setup` mode (Stripe-hosted page) and let the webhook (`setup_intent.succeeded` → `PaymentsEventType.SetupIntentSucceeded`) fire when the customer completes it. There is no inline card-entry surface.

### Manage (subscriptions, portal, upcoming invoice)

```csharp
var subs = await _payments.ListSubscriptionsAsync(stripeCustomerId: cid, status: "all");
view.SubscriptionList(
    subscriptions: subs,
    onCancel:        id => _payments.CancelSubscriptionAsync(id, immediate: false),
    onPause:         id => _payments.PauseSubscriptionAsync(id),
    onResumeFromPause: id => _payments.ResumeSubscriptionAsync(id),
    onResume:        id => _payments.ResumeCanceledSubscriptionAsync(id));

// Preview a plan change before committing
var preview = await _payments.PreviewUpcomingInvoiceAsync(cid, subscriptionId: subs[0].Id);
view.UpcomingInvoicePreview(preview);
```

Stripe-hosted Customer Portal — opens in a new tab. In Connect mode the same Portal works against the connected account when `onBehalfOf: connectedAccountId` is passed to `CreatePortalAsync`; the merchant separately has the full Stripe Dashboard at `https://dashboard.stripe.com/{acct_id}` for admin-level account management.

```csharp
view.PaymentsPortalButton(onOpenPortal: async () =>
{
    var portal = await _payments.CreatePortalAsync(stripeCustomerId: cid);
    return portal.Url;
});
```

### Account history (payment methods, invoices, charges)

```csharp
var pms      = await _payments.ListPaymentMethodsAsync(cid);
var invoices = await _payments.ListInvoicesAsync(stripeCustomerId: cid);
var charges  = await _payments.ListChargesAsync(stripeCustomerId: cid);

view.PaymentMethodList(
    methods: pms,
    onDetach: pmId => _payments.DetachPaymentMethodAsync(pmId),
    onAddCard: async () =>
    {
        // Customer Portal handles add-card via Stripe-hosted UI — opens in a new tab
        var portal = await _payments.CreatePortalAsync(stripeCustomerId: cid);
        await ClientFunctions.OpenExternalUrlAsync(portal.Url);
    });

view.InvoiceList(invoices);
view.ChargeList(charges,
    onRefund: pi => _payments.RefundAsync(pi, idempotencyKey: $"refund-{pi}"));
```

## Admin surface

Seven admin primitives, all programmatic (no one-size-fits-all component). Live in the validation app's **Admin actions** tab.

### Catalog (products + prices + payment links)

```csharp
var productId = await _payments.CreateProductAsync(new PaymentsProductInfo {
    Name = "Pro",
    Description = "Pro tier",
    MarketingFeatures = ["Unlimited workshops", "Priority support"],
    Metadata = new Dictionary<string, string> { ["app_id"] = "my-app" },
});
var priceId   = await _payments.CreatePriceAsync(new PaymentsPriceInfo {
    ProductId = productId, UnitAmountMinor = 1900, Currency = "eur",
    RecurringInterval = "month", LookupKey = "pro-monthly" });

var link = await _payments.CreatePaymentLinkAsync([PaymentsLineItem.ForPrice(priceId)], allowPromotionCodes: true);
```

`MarketingFeatures` populates Stripe's `marketing_features` array (visible on Stripe-hosted pricing tables, adaptive Checkout UIs; max 15 entries × 80 chars). `LookupKey` gives the price a stable handle independent of the opaque `price_…` id — required for the catalog-projection pattern below.

### Catalog: push vs pull

The library supports both directions of catalog management. Pick per app — most apps use both.

**Push (`PaymentsCatalogSync`)** — code is source of truth. App declares plans in a static class, library ensures Stripe matches at startup. Use for deploy-time provisioning where pricing is owned by engineering.

```csharp
public static class Plans
{
    public static readonly PaymentsPlanSpec Pro = new(
        AppPlanId: "pro",
        ProductName: "Pro",
        UnitAmountMinor: 1900,
        Currency: "eur",
        Interval: "month",
        Description: "Pro tier");
}

var sync = new PaymentsCatalogSync(_payments);
var map = await sync.SyncFromCatalogClassAsync(typeof(Plans));
// map.GetPriceId("pro") -> "price_xyz..."  (use in adapter's GetPlanAsync)
```

**Pull (`PaymentsCatalogProjector`)** — Stripe is source of truth. Library lists active products + prices, filters to the app's slice, returns a `PaymentsPlanCatalog`. Use when operators tweak prices via Stripe Dashboard or apps create plans dynamically at runtime. Mirrors whatever's there, including products this app instance didn't create.

```csharp
var projector = new PaymentsCatalogProjector(_payments);
var catalog = await projector.ProjectAsync(
    productFilter: p => p.Metadata?["app_id"] == "my-app");

// catalog.Plans          → IReadOnlyList<PaymentsPlanProjection> for PricingTable
// catalog.PlanIdToPriceId → "pro-monthly" → "price_xyz..." (adapter lookup)
```

`PlanId` defaults to the price's `LookupKey` when set, otherwise the Stripe price id. Stamp `LookupKey` on every price you intend to surface in `PricingTable` so the id stays stable across re-creates.

**Refresh strategy** — cache the projection in `Reactive<PaymentsPlanCatalog?>` and refresh on:

1. App startup (once after `PaymentsService` is constructed)
2. Admin actions that create/archive products (eager refresh after `CreateProductAsync` / `UpdateProductAsync(active: false)`)
3. Stripe webhooks: `PaymentsEventType.ProductUpdated` and `PaymentsEventType.PriceUpdated` fire when products or prices change in Stripe Dashboard — invalidate the cache on these

```csharp
public Task ApplyEventAsync(PaymentsEvent evt, CancellationToken ct)
{
    if (evt.Type is PaymentsEventType.ProductUpdated or PaymentsEventType.PriceUpdated)
    {
        _ = RefreshCatalogAsync();
    }
    // … other handling
}
```

**When to use which**:

| Scenario                                       | Pattern                                                 |
| ---------------------------------------------- | ------------------------------------------------------- |
| Plans live in code, infrequent changes         | Push only                                               |
| Operators tweak prices via Stripe Dashboard    | Pull only                                               |
| Code seeds defaults, operators tune later      | Push at deploy + Pull at runtime + webhook invalidation |
| Multi-tenant: each tenant has own plan catalog | Pull with per-tenant `metadata` filter                  |

### Customers

```csharp
var cid = await _payments.CreateCustomerAsync(new PaymentsCustomerInfo { Email = "biz@acme.com", Name = "Acme" });
await _payments.UpdateCustomerAsync(cid, new PaymentsCustomerInfo { AddressCountry = "FI" });
var matches = await _payments.SearchCustomersByAppKeyAsync("user-42");
await _payments.AdjustCustomerBalanceAsync(cid, -1500, "eur", "Goodwill credit", idempotencyKey: $"credit-{Guid.NewGuid()}");
```

### Discounts (coupons + promo codes)

```csharp
var couponId = await _payments.CreateCouponAsync(new PaymentsCouponInfo
{
    Id = "LAUNCH50", Name = "Launch 50%", PercentOff = 50,
    Duration = PaymentsCouponDuration.Repeating, DurationInMonths = 3,
});
await _payments.CreatePromotionCodeAsync(couponId, code: "LAUNCH50", maxRedemptions: 100);
```

Surface on checkout: `PaymentsPlanDescriptor.AllowPromotionCodes = true`.

### Invoicing (hosted invoices + credit notes)

```csharp
// B2B net-30 — customer pays via emailed hosted link
var invoice = await _payments.CreateHostedInvoiceAsync(
    stripeCustomerId: cid,
    lines: [PaymentsLineItem.Dynamic(50000, "eur", "Consulting · April 2026")],
    daysUntilDue: 30, autoSend: true);

// Formal partial refund — handles tax reversal + regenerates PDF
var cn = await _payments.CreateCreditNoteAsync(new PaymentsCreditNoteInfo
{
    InvoiceId = invoice.Id, AmountMinor = 1500, RefundAmountMinor = 1000, CreditAmountMinor = 500,
    Memo = "Service downtime", Reason = "duplicate",
});
```

### Subscription management

```csharp
await _payments.UpdateSubscriptionItemQuantityAsync(subItemId, 5);    // seat scaling
await _payments.UpdateSubscriptionPriceAsync(subItemId, newPriceId);  // migrate to new price
await _payments.PauseSubscriptionAsync(subId);
await _payments.ResumeSubscriptionAsync(subId);
await _payments.CancelSubscriptionAsync(subId, immediate: false);     // at period end
await _payments.ResumeCanceledSubscriptionAsync(subId);               // un-cancel
```

Stripe prices are immutable — bumping €19 → €25 creates a new price. Migrate active subscribers via `UpdateSubscriptionPriceAsync` (prorate by default).

### Webhook operations

```csharp
// Self-provision endpoints (alternative to Stripe Dashboard)
var ep = await _payments.CreateWebhookEndpointAsync(
    url: $"{appOrigin}/ikon/webhook/stripe",
    enabledEvents: ["invoice.paid", "customer.subscription.updated"]);
// Persist ep.Secret immediately — Stripe returns it only on creation.

// Replay after an outage
var since = DateTimeOffset.UtcNow.AddHours(-24);
foreach (var id in await _payments.ListEventIdsAsync(createdAfter: since, limit: 100))
{
    var evt = await _payments.RetrieveEventAsync(id);
    await _adapter.ApplyEventAsync(evt, ct);
}
```

### Reporting + Connect surfaces

```csharp
var charges  = await _payments.ListChargesAsync(stripeCustomerId: cid, limit: 50);
var invoices = await _payments.ListInvoicesAsync(stripeCustomerId: cid, status: "paid");
```

Connect admin lives in the merchant's full Stripe Dashboard at `https://dashboard.stripe.com/{acct_id}`. Apps that want a one-click entry surface fetch the dashboard URL from the platform backend status endpoint (or call `StripeMerchantService.RetrieveAccountAsync`) and open it in a new tab via `ClientFunctions.OpenExternalUrlAsync`.

## Marketplace + Stripe Connect

Two distinct Connect patterns:

| Pattern                 | Selector                                                         | Use when                                                                                                               |
| ----------------------- | ---------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| **Destination charges** | `PaymentsDestination` on checkout                                | Platform owns the customer; routes funds to creator with an `ApplicationFee`. E.g. creator storefront, tip jar.        |
| **Platform-managed**    | `PaymentsOptions.ConnectedAccountId` (ikon-connect injects this) | Platform owns the Stripe relationship; each app onboards as a sub-account. All charges run on `Stripe-Account` header. |

```csharp
// Destination charge — fund routing per checkout
await payments.CreateCheckoutAsync(
    planId: "tip-jar", appCustomerKey: null, email: fanEmail,
    destination: new PaymentsDestination(creatorAcctId, ApplicationFeeAmountMinor: 200));

// Platform-managed — every call runs on behalf of acctId
var payments = new PaymentsService(new PaymentsOptions
{
    ApiKey = platformKey, ConnectedAccountId = "acct_xyz",
    PlatformApplicationFeePercent = 5m,
}, adapter);
```

`PaymentsDestination` and `ConnectedAccountId` are mutually exclusive.

### Redirect-only Connect onboarding (ikon-connect mode)

Embedded Connect components (account session + Connect.js) are no longer used. The platform backend creates the v2 connected account in the Stripe-managed posture and mints a Stripe-hosted KYC URL; the merchant opens that URL in a new tab, completes KYC, and returns. Apps then read live capability state via the platform backend's status endpoint or directly via `StripeMerchantService.RetrieveAccountAsync`.

```bash
# Provision the connected account + KYC URL
ikon app payments init                     # default mode = ikon-connect
ikon app payments init --open-browser      # also auto-open the KYC link

# After KYC, confirm and get the Stripe dashboard URL
ikon app payments status
```

The platform backend uses Stripe v2 throughout — `stripe.v2.core.accounts.create` (with `dashboard=full`, `defaults.responsibilities.fees_collector=stripe`, `defaults.responsibilities.losses_collector=stripe`) and `stripe.v2.core.accountLinks.create` for the KYC redirect. The connected account is bound to the Ikon space via `acct.metadata.ikon_space`.

```csharp
// App-side: read the live state of an existing connected account
var acct = await StripeMerchantService.Current.RetrieveAccountAsync(acctId);
if (acct.ChargesEnabled && acct.PayoutsEnabled) { /* unlock billing flows */ }
```

**Stripe-managed posture trade-off.** With `fees_collector=stripe` + `losses_collector=stripe` Stripe handles processing fees and loss liability. The platform still earns via `application_fee_amount` / `application_fee_percent` set per Checkout session, PaymentIntent, or Subscription (5% default via `PaymentsOptions.PlatformApplicationFeePercent`). Per €100 charge with 5% app fee + 2.9%+€0.30 Stripe fee, customer pays €100, Stripe deducts €3.20 from the connected account, €5.00 transfers to the platform balance, connected account nets €91.80.

### Async payment methods — fulfilment timing

If an app enables async-pull payment methods (Alipay, BECS, Boleto, iDEAL, OXXO, SEPA Debit), Checkout fires events in a different order than card payments:

| Event                                      | Fires when                        | Money status                  |
| ------------------------------------------ | --------------------------------- | ----------------------------- |
| `checkout.session.completed`               | Customer finishes the checkout UI | **Pending** for async methods |
| `checkout.session.async_payment_succeeded` | Bank/wallet settles the pull      | **Settled**                   |
| `checkout.session.async_payment_failed`    | Bank/wallet rejects the pull      | **Failed**                    |

**Rule:** for async methods, **fulfil on `async_payment_succeeded`, not `completed`**. Fulfilling on `completed` for async means you ship goods before the money lands — irrecoverable if `async_payment_failed` arrives later.

```csharp
public Task ApplyEventAsync(PaymentsEvent evt, CancellationToken ct) => evt.Type switch
{
    PaymentsEventType.CheckoutCompleted              => MarkPendingAsync(evt, ct),    // record intent
    PaymentsEventType.CheckoutAsyncPaymentSucceeded  => FulfilOrderAsync(evt, ct),    // ship here
    PaymentsEventType.CheckoutAsyncPaymentFailed     => NotifyCustomerAsync(evt, ct),
    _ => Task.CompletedTask,
};
```

For **card** payments `completed` and `async_payment_succeeded` collapse into the single `completed` event — your adapter must idempotently handle both code paths landing for the same `appCustomerKey` (dedupe on `evt.EventId`).

### v2 wire-format gotchas

When debugging Connect calls against the v2 API, three things bite:

- **Indexed include syntax.** v2 endpoints reject bare `include[]=foo&include[]=bar` with `parameter_invalid_empty`. Use indexed brackets: `include[0]=foo&include[1]=bar`. The library handles this — apps wiring their own retrieve calls must follow the same shape.
- **Capability status path is nested.** `ChargesEnabled` is derived from `configuration.merchant.capabilities.<cap>.status == "active"`. The legacy top-level `charges_enabled` / `payouts_enabled` booleans do NOT exist on v2 account objects. `StripeMerchantAccount` exposes the derived booleans so apps don't have to read the raw structure.

### Connect webhooks

Register a second endpoint with `connect: true`:

```csharp
var endpoint = await connect.CreateConnectWebhookEndpointAsync(
    url: $"{appOrigin}/ikon/webhook/stripe-connect",
    enabledEvents: [
        // Accounts v2 family (recommended)
        "v2.core.account.updated",
        "v2.core.account[requirements].updated",
        "v2.core.account_capability.updated",
        // Billing + checkout (unchanged)
        "invoice.paid",
        "customer.subscription.updated",
        "checkout.session.completed",
        "checkout.session.async_payment_succeeded",
        "checkout.session.async_payment_failed",
        // Payouts
        "payout.paid",
        "payout.failed",
    ]);
// endpoint.Secret → STRIPE_CONNECT_WEBHOOK_SECRET
```

The library still parses the legacy `account.updated` event for one transition release — apps that haven't migrated their Stripe Dashboard webhook registrations yet will continue to receive events, but the resulting `PaymentsEvent` will have `IsLegacyEventName = true` (log it / migrate when you see it). Legacy will be dropped in the next major.

Connect events have a top-level `account` field. The validation app's `StripeWebhook` inspects this to route to the right `PaymentsService` instance.

## Code-bootstrap catalog

Declare plans in code, let the library provision matching Stripe products + prices.

```csharp
public static class Plans
{
    public static readonly PaymentsPlanSpec Pro  = PaymentsPlanSpec.Subscription("pro",  "Pro",  1900, "eur", "month");
    public static readonly PaymentsPlanSpec Team = PaymentsPlanSpec.Subscription("team", "Team", 4900, "eur", "month");
}

var sync = new PaymentsCatalogSync(_payments);
var map = await sync.SyncFromCatalogClassAsync(typeof(Plans));

// In adapter:
public Task<PaymentsPlanDescriptor?> GetPlanAsync(string planId, CancellationToken ct)
    => Task.FromResult<PaymentsPlanDescriptor?>(map.TryGetPriceId(planId, out var pid)
        ? PaymentsPlanDescriptor.Subscription(planId, pid)
        : null);
```

Sync is idempotent: existing rows reused, new rows created. Resolution uses Stripe `lookup_key` (O(1) per plan). Run once at startup (~200ms cold).

## Entitlement gating

`PaymentsService.GetEntitlementAsync` composes a single `PaymentsEntitlement` from three sources: Stripe subscriptions, customer metadata (`unlock_{planId}`), and an optional `IPaymentsCreditStore`.

```csharp
var ent = await _payments.GetEntitlementAsync("pro", appCustomerKey);
if (ent.SubscriptionActive)      Render($"Pro · renews {ent.SubscriptionEndsAt:d}");
else if (ent.UnlockGranted)      Render($"Lifetime · purchased {ent.UnlockGrantedAt:d}");
else                             RenderUpgradeCTA();
```

Three declarative policy attributes gate `[Function]`-registered methods:

```csharp
[Function]
[PaymentsRequireSubscription("pro")]
public Task<Image> Premium(string prompt) { /* runs only when entitled */ }

[Function]
[PaymentsRequireUnlock("hd-pack")]
public Task<Image> HighDef(string prompt) { /* gated on one-time unlock */ }

[Function]
[PaymentsChargeCredits("image-credits", credits: 1)]
public Task<Image> Standard(string prompt) { /* deducts 1 credit on entry */ }
```

| Attribute                       | Deny code when missing          | Other deny codes                                             |
| ------------------------------- | ------------------------------- | ------------------------------------------------------------ |
| `[PaymentsRequireSubscription]` | `payments_subscription_required` | `payments_no_user`, `payments_not_initialized`                 |
| `[PaymentsRequireUnlock]`       | `payments_unlock_required`       | same                                                         |
| `[PaymentsChargeCredits]`       | `payments_credits_insufficient`  | `payments_no_credit_store`, `payments_credits_deduction_error` |

Policies are webhook-driven, **not** polling-driven. They gate + signal — the app's UI catches the deny code, calls `OfferCheckoutAsync`, and waits for the webhook to flip entitlement.

For credit-based products, supply an `IPaymentsCreditStore` (or set `payments.CreditStore` once at startup):

```csharp
public sealed class MyCreditStore : IPaymentsCreditStore
{
    public Task<int> GetCreditsAsync(string customerKey, string sku, CancellationToken ct) { /* DB read */ }
    public Task<int> DeductAsync(string customerKey, string sku, int credits, string idemKey, CancellationToken ct) { /* DB write, dedup */ }
    public Task<int> GrantAsync(string customerKey, string sku, int credits, string idemKey, CancellationToken ct) { /* DB write */ }
}
```

## Error catalog

| Symptom                                                     | Cause                                                                    | App fix                                                                                                                                |
| ----------------------------------------------------------- | ------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------- |
| `PaymentsConfigurationException` at construction            | Missing required option (`ApiKey` in BYOK, `Space` in ikon-connect)  | Check secrets; restart after `BILLING_PROVIDER` change.                                                                                |
| `PaymentsApiException` `card_declined`                      | Stripe declined the card                                                 | Read `ex.DeclineCode`; surface specific message (`insufficient_funds`, `expired_card`, …).                                             |
| `PaymentsApiException` `resource_missing`                   | Object id doesn't exist (deleted product, archived price, bogus `cus_…`) | Refresh listings; treat as a 404 in your UI.                                                                                           |
| `PaymentsApiException` `parameter_invalid_integer` / 400    | Bad payload — amount ≤ 0, qty 0, malformed URL                           | Validate input client-side before the call.                                                                                            |
| `PaymentsException("unknown plan")`                         | Adapter returned `null` from `GetPlanAsync`                              | Adapter must map the planId to a price id; check catalog sync ran.                                                                     |
| Webhook `Verified=false`                                    | Wrong signing secret, body mutated by middleware, or wrong endpoint type | Endpoints come in pairs — Account secret ≠ Connect secret. Verify which secret matches which endpoint.                                 |
| Webhook `Verified=true, AdapterError=<msg>`                 | Adapter threw (DB transient, deserialization, etc.)                      | Return 500 to let Stripe retry, or 200 + log + manual replay (`ListEventIdsAsync` + `RetrieveEventAsync` + `adapter.ApplyEventAsync`). |
| Checkout in Connect mode fails before onboarding done       | `acct.ChargesEnabled == false`                                           | Gate the end-user surface on charges_enabled; complete KYC first.                                                                      |
| Customer Portal 400 `No such customer portal configuration` | New connected account, portal disabled                                   | Call `CreatePortalConfigurationAsync` post-onboarding, or surface a "configure portal" CTA.                                            |
| `requires_action` payment intent                            | 3DS / SCA prompt                                                         | Listen for `PaymentActionRequired` event; open invoice's `HostedInvoiceUrl`.                                                           |
| Subscription stuck `past_due`                               | Retries exhausted                                                        | Show top-of-app banner with Customer Portal link to update card; downgrade access on `SubscriptionDeleted`.                            |

`PaymentsApiException` exposes structured Stripe fields parsed from the response body: `StatusCode`, `ResponseBody`, `ErrorType`, `ErrorCode`, `DeclineCode`, `StripeMessage`, `ParamPath`. Branch on `ErrorCode` / `DeclineCode` — not on string matching `Message`.

```csharp
try { await _payments.CreateCheckoutAsync(...); }
catch (PaymentsApiException ex) when (ex.ErrorCode == "card_declined")
{
    if (ex.DeclineCode == "insufficient_funds") { /* show top-up prompt */ }
}
```

The validation app puts a **Force error** button next to every action — same SDK call with bad input — so each path above can be triggered live against the sandbox.

## Idempotency

Every public POST method on `PaymentsService` and `StripeMerchantService` accepts a `string? idempotencyKey`. Pass a stable key derived from app state so concurrent replicas, network retries, and pre-commit loops don't duplicate.

| Method                              | Suggested key                                           |
| ----------------------------------- | ------------------------------------------------------- |
| `CreateCustomerAsync`               | `customer-{appCustomerKey}`                             |
| `CreateProductAsync`                | `product-{stableName}` (used by `PaymentsCatalogSync`)  |
| `CreatePriceAsync`                  | `price-{lookupKey}-{amountMinor}-{currency}-{interval}` |
| `CreateCheckoutAsync`               | `checkout-{planId}-{userId}-{minute}`                   |
| `CreatePaymentIntentAsync`          | `pi-{orderId}`                                          |
| `RefundAsync`                       | `refund-{chargeId}` (required by signature)             |
| `CreateConnectedAccountAsync`       | `acct-{orgId}`                                          |
| `CreateConnectWebhookEndpointAsync` | `webhook-{environment}`                                 |

`PaymentsCatalogSync` passes deterministic keys for its product / price creates automatically.

Webhook deliveries dedupe on `PaymentsEvent.EventId`:

```csharp
public async Task ApplyEventAsync(PaymentsEvent evt, CancellationToken ct)
{
    if (await _seenEvents.HasAsync(evt.EventId)) return;
    await ApplyToDb(evt);
    await _seenEvents.RecordAsync(evt.EventId);
}
```

## Retries and timeouts

Transient failures (429, 5xx, network faults) auto-retry with exponential backoff + jitter on idempotent calls (every GET + any POST with `Idempotency-Key`).

```csharp
new PaymentsOptions
{
    MaxRetryAttempts = 3,
    RetryBaseDelay = TimeSpan.FromMilliseconds(500),
    RequestTimeout = TimeSpan.FromSeconds(30),
}
```

## Frontend wiring

The Parallax payments components (`PricingTable`, `PlanCard`, `CheckoutButton`, `TipPresetGrid`, `PaymentsPortalButton`, `PaymentMethodList`, `ChargeList`, `InvoiceList`, `UpcomingInvoicePreview`, `SubscriptionStatus`, `SubscriptionList`) are pure compositions of native `view.*` primitives — Box / Text / Button / Icon / Column / Row. They display Stripe data and route clicks through callbacks; they ship with `Ikon.Parallax`, no extra frontend package is needed.

| Flow                                                                             | UX                                                                      |
| -------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| Hosted Checkout (`CreateCheckoutAsync` + `ClientFunctions.OpenExternalUrlAsync`) | New-tab redirect                                                        |
| Customer Portal (`CreatePortalAsync` + `OpenExternalUrlAsync`)                   | New-tab redirect                                                        |
| KYC onboarding (ikon-connect)                                                    | New-tab redirect to Stripe-hosted KYC URL from `ikon app payments init` |
| Stripe Dashboard (ikon-connect, post-KYC)                                        | New-tab redirect to `https://dashboard.stripe.com/{acct_id}`            |
| `CreatePaymentLinkAsync`                                                         | External shareable URL — chat / email / QR distribution                 |

No Stripe.js, no embedded iframes, no per-app frontend dependency on `@stripe/*` packages.

## Testing

```bash
stripe listen --forward-to https://localhost:9443/ikon/webhook/stripe
stripe trigger checkout.session.completed
stripe trigger invoice.payment_failed
```

Test card `4242 4242 4242 4242` (any future expiry, any 3-digit CVC, any postal code).

Connect onboarding KYC shortcuts (test mode only, `sk_test_…`):

| Field         | Value                                       | Effect         |
| ------------- | ------------------------------------------- | -------------- |
| Date of birth | `1901-01-01`                                | Verified match |
| SSN / tax-id  | `000000000`                                 | Auto-pass      |
| ID document   | file token `file_identity_document_success` | Auto-verify    |
| SMS code      | `000-000`                                   | Auto-confirm   |

## Multi-environment apps

Apps with several `ikon-config.{development,staging,production}.toml` files carry a **distinct SpaceId per environment**, and each env's Ikon backend holds its own Stripe platform key. Stripe forbids sharing a Connect account across test and live mode, so **each environment gets its own connected account**.

Init once per environment (the verb targets whichever backend you're logged into + the SpaceId from the chosen config):

```bash
ikon app payments init --target development   # test-mode merchant, fake KYC auto-passes
ikon app payments init --target production    # live-mode merchant, real KYC
```

App code is environment-agnostic — `PaymentsAppHelpers.AutoDetectFromApp(app)` resolves the backend the running IkonServer instance is bound to, and the connected account resolves automatically. The backend persists the space↔merchant binding in Mongo (`paymentsmerchantaccounts`), so `ikon app payments status` returns the merchant consistently across backend restarts; if a binding is ever missing it auto-heals from the provider's space tag (Stripe `metadata.ikon_space`).

**Catalog across environments** — declare plans once in a `Plans` class and call `PaymentsCatalogSync` at startup; the library provisions matching products + prices in whichever Stripe account the current env points at. KYC is fake in test mode (dev backend `sk_test_…`) and real in live (prod backend `sk_live_…`).

Developers who only have production-backend access can still iterate against a sandbox by running a dev SpaceId in **BYOK mode** with their own `sk_test_…` keys, reserving `ikon-connect` for the production SpaceId.

## Common pitfalls

- **Forgetting the second webhook endpoint.** Connect events go to a separate URL with a separate signing secret. Without `STRIPE_CONNECT_WEBHOOK_SECRET`, Connect events deliver but fail signature verification silently.
- **Calling end-user flows before `acct.ChargesEnabled = true`.** Onboarding incomplete = no payments. Gate the end-user UI on the connected-account state.
- **Holding `ConfigService` / secret reads on hot paths.** Read once into a private readonly field at construction. `PaymentsService` already does this; cache app-side secret reads in app fields.
- **Mutating Stripe prices.** Prices are immutable — bumping amounts creates a new price. Migrate active subscribers via `UpdateSubscriptionPriceAsync`.
- **Using random idempotency keys.** A random GUID per attempt defeats the point. Derive keys from stable app state (orderId, customerKey, refund target).
- **Returning 500 from the webhook function on adapter errors you can't replay.** Stripe retries every 5xx for ~3 days. If the adapter bug is fixed, return 200 and replay manually via `ListEventIdsAsync` + `RetrieveEventAsync`.
- **Trusting `Message` strings to branch on errors.** Branch on `ex.ErrorCode` / `ex.DeclineCode` — message text drifts.
- **Mixing live + test keys across an app's secrets.** A `sk_live_*` API key with an `sk_test_*` webhook secret fails opaquely. Use one mode (`test` or `live`) consistently.
- **Persisting subscription state from API reads.** Stripe is the source of truth; cache only what the adapter projects via `ApplyEventAsync`.

## CLI reference

```bash
# Default: ikon-connect mode. Provisions per-space secrets, creates a v2
# connected account in the Stripe-managed posture, and prints the KYC URL.
ikon app payments init
ikon app payments init --contact-email merchant@example.com --display-name "Acme" --country FI
ikon app payments init --open-browser            # also open the KYC URL in the default browser

# BYOK mode — customer brings own Stripe account (prompts for keys).
ikon app payments init --mode byok               # or shorthand: --byok

# Disable — clear all billing secrets, deactivate the surface.
ikon app payments init --disable

# After completing KYC at the printed URL, confirm + get the dashboard URL.
ikon app payments status

# Stripe CLI for local webhook testing
stripe listen --forward-to https://localhost:9443/ikon/webhook/stripe
stripe trigger checkout.session.completed
```

## Related

- [Ikon Signature Guide](ikon-signature-guide.md) — eID-backed document signing, often paired with billing for contract-on-payment flows.
- [Ikon.AI Library Overview](ikon-ai-library-overview.md) — the AI services that policy attributes (`[PaymentsChargeCredits]`) gate.
- Reference implementation: `Validation.Payments.cs` + `Validation.Payments.Bootstrap.cs` in [Ikon.App.Platform.Validation](../../platform-dotnet/Ikon.App.Platform.Validation). Every primitive in this guide is exercised live against the Stripe sandbox.
