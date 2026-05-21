# Ikon.App.Billing Guide

`Ikon.App.Billing` provides Stripe-backed billing primitives for Ikon AI apps:
hosted Stripe Checkout, the Customer Portal, webhook verification, metered
usage reporting, marketplace (Stripe Connect) flows, refunds, and subscription
management. The library is app-agnostic — apps map their own plan and customer
model onto a small adapter interface and own all persistence.

The C# library is paired with the `@ikonai/sdk-react-ui-billing` frontend
package which mounts Stripe.js / Stripe Connect.js inside the host app so
embedded checkout + the seven Stripe Connect embedded components render
inline. See the **Wiring billing into an Ikon app's frontend** section below.

## Billing provider modes

Customer Ikon apps choose between two billing transport modes via the `BILLING_PROVIDER` secret:

| Mode | Selector | When to use | Customer setup | Fee |
|------|----------|-------------|----------------|-----|
| **`ikon-connect`** *(default)* | `BILLING_PROVIDER=ikon-connect` or unset | Default. Zero-config. Customer becomes a connected sub-account on Ikon's Stripe Connect platform. Ikon handles webhooks, dispatch, KYC. | None for Stripe. Just configure `IKON_BACKEND_BILLING_URL` + `IKON_APP_TOKEN` (Ikon-issued). | Ikon takes 5% (configurable); Stripe processing fees on top |
| **`byok`** | `BILLING_PROVIDER=byok` | Customer wants full Stripe control, lower fees, or operates in jurisdiction Ikon Connect doesn't cover. | Customer creates own Stripe account + sets `STRIPE_API_KEY` + registers own webhooks. | Standard Stripe fees only (no Ikon cut) |

### `ikon-connect` mode setup

```bash
ikon app secret set BILLING_PROVIDER ikon-connect              # optional — this is the default
ikon app secret set IKON_BACKEND_BILLING_URL https://backend.ikonai.live
ikon app secret set IKON_APP_TOKEN <token-issued-by-ikon>
ikon app secret set IKON_WEBHOOK_SECRET <secret-issued-by-ikon>
ikon app secret set STRIPE_PUBLISHABLE_KEY pk_live_<ikon-platform-publishable-key>
```

In customer-app C# code, nothing changes — `BillingService` constructor auto-picks the transport from `BillingOptions.Provider`. Customer apps that don't read `BILLING_PROVIDER` default to `ikon-connect`.

Customer-app webhook receiver uses `HandleIkonWebhookAsync` instead of `HandleWebhookAsync` to verify Ikon's signature:

```csharp
[Function(Webhook = true, Name = "ikon-billing")]
public async Task<string> IkonBillingWebhook(Dictionary<string, string> queryParams, Dictionary<string, string> headers, string body)
{
    headers.TryGetValue("Ikon-Signature", out var signature);
    await _billing.HandleIkonWebhookAsync(signature, body);
    return """{"received":true}""";
}
```

After deploying the app, register the webhook URL once:

```bash
curl -X POST https://backend.ikonai.live/billing/platform/<ikon-app-id>/register-webhook-url \
  -H "Authorization: Bearer <IKON_APP_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"connectedAccountId":"acct_X","webhookUrl":"<your-app-relay-webhook-url>"}'
```

### BYOK mode setup

Customer creates own Stripe account, then:

```bash
ikon app secret set BILLING_PROVIDER byok
ikon app secret set STRIPE_API_KEY sk_test_...                 # or sk_live_
ikon app secret set STRIPE_PUBLISHABLE_KEY pk_test_...
ikon app secret set STRIPE_WEBHOOK_SECRET whsec_...
ikon app secret set STRIPE_CONNECT_WEBHOOK_SECRET whsec_...    # if using marketplace
```

Customer registers own webhook endpoints in their own Stripe Dashboard (see "Webhook setup" section below).

Customer-app webhook receiver uses `HandleWebhookAsync` (Stripe signature):

```csharp
[Function(Webhook = true, Name = "stripe")]
public async Task<string> StripeWebhook(Dictionary<string, string> queryParams, Dictionary<string, string> headers, string body)
{
    headers.TryGetValue("Stripe-Signature", out var signature);
    await _billing.HandleWebhookAsync(signature, body);
    return """{"received":true}""";
}
```

### Switching modes

Restart required after changing `BILLING_PROVIDER`. `BillingService` constructed at boot from secrets; toggling mid-session not supported.

## Scope boundary

The library handles **payments, subscriptions, invoices** and only those:
checkout sessions, the customer portal, webhook verification + dispatch, meter
events, refunds, subscription updates / cancellations / pauses, marketplace
splits, and ad-hoc invoice items.

The library does **not** handle:

- What the app offers (features, capabilities, plan→capability mapping)
- Limits, caps, seat counts, credit balances
- When a feature is enabled or blocked
- How usage is counted, aggregated, or rate-limited
- Persistence of customer / plan / subscription state in the app's database

Those are app concerns. Apps own them in their own code (e.g. a
`SubscriptionGate` / `SubscriptionService` / `UsagePolicy` inside the app).
The library only carries the data needed to talk to Stripe and hands verified
events back to the app's `IBillingAppAdapter`. Apps decide what each event
means for their domain.

## Overview

The library exposes one façade: `BillingService`. Apps:

1. Implement `IBillingAppAdapter` to bridge the library to their domain model.
2. Construct `BillingService(BillingOptions, IBillingAppAdapter)` once at app startup.
3. Call `CreateCheckoutAsync` / `CreatePortalAsync` / `ReportUsageAsync` from app code.
4. Forward Stripe webhook deliveries to `HandleWebhookAsync` from a webhook function.

Stripe is the current backend; the public surface uses provider-neutral
`Billing*` names so a second backend can be added later without breaking apps.

## Quick start

```csharp
using Ikon.App.Billing;

// 1. Implement the adapter
public sealed class MyAppBillingAdapter : IBillingAppAdapter
{
    public Task<BillingPlanDescriptor?> GetPlanAsync(string planId, CancellationToken ct)
    {
        // Map app plan id to Stripe price id
        return Task.FromResult<BillingPlanDescriptor?>(
            new BillingPlanDescriptor(planId, "price_1ABC...", BillingMode.Subscription));
    }

    public Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken ct)
    {
        // Look up or create a Stripe customer for this app entity, persist the mapping
        return Task.FromResult("cus_1XYZ...");
    }

    public Task ApplyEventAsync(BillingEvent evt, CancellationToken ct)
    {
        // Update app DB based on evt.Type. Use evt.EventId for idempotency.
        return Task.CompletedTask;
    }
}

// 2. Construct the service
var billing = new BillingService(
    new BillingOptions
    {
        ApiKey = stripeApiKey,
        WebhookSecret = stripeWebhookSecret,
        DefaultSuccessUrl = $"{appOrigin}/billing/success",
        DefaultCancelUrl = $"{appOrigin}/billing/cancel",
        DefaultPortalReturnUrl = $"{appOrigin}/billing/portal-return",
    },
    new MyAppBillingAdapter());

// 3. Start checkout from app code
var checkout = await billing.CreateCheckoutAsync(
    planId: "pro-monthly",
    appCustomerKey: appId.ToString(),
    email: ownerEmail);
// Redirect user to checkout.Url
```

### Zero-config bootstrap from Ikon secrets

`BillingAppHelpers.AutoDetectFromApp(app)` reads `BILLING_PROVIDER` + per-mode secrets from the app's secret store (with env-var fallback). Auto-detects provider when `BILLING_PROVIDER` is unset (`STRIPE_API_KEY` present → BYOK, `IKON_BACKEND_BILLING_URL` + `IKON_APP_TOKEN` present → IkonConnect, else Disabled). Removes the per-app secret-reading boilerplate:

```csharp
var options = BillingAppHelpers.AutoDetectFromApp(app, defaultAppId: "my-app") with
{
    DefaultSuccessUrl = $"{appOrigin}/billing/success",
    DefaultCancelUrl = $"{appOrigin}/billing/cancel",
    DefaultPortalReturnUrl = $"{appOrigin}/billing/portal-return",
};

if (options.Provider == BillingProvider.Disabled)
{
    // App not configured for billing yet — show "enable billing" CTA in UI
    return;
}

var billing = new BillingService(options, new MyAppBillingAdapter());
```

### Connected-account persistence (IkonConnect mode)

`IBillingConnectAccountStore` abstracts where the customer app remembers its connected `acct_X` between reboots. Default `AssetBillingConnectAccountStore` persists as a JSON asset; apps with their own DB swap in a custom implementation.

```csharp
var store = new AssetBillingConnectAccountStore("my-app/billing/connect-account-id.json");
await store.SetAsync(accountId);
var saved = await store.GetAsync();   // null if not yet set
await store.ClearAsync();              // tenant-reset
```

## Adapter contract

The library calls back into `IBillingAppAdapter` for three things only:

| Method | When called | App responsibility |
|--------|-------------|--------------------|
| `GetPlanAsync` | Inside `CreateCheckoutAsync` | Resolve app plan id → `BillingPlanDescriptor` (Stripe price id, mode, optional metered price id, metadata) |
| `ResolveStripeCustomerIdAsync` | Inside `CreateCheckoutAsync` | Return existing Stripe customer id or create one and persist the mapping |
| `ApplyEventAsync` | After webhook signature verification | Update app DB; implement idempotency on `BillingEvent.EventId` |

The library never reads or writes the app database itself.

## Webhook setup

Wire a Stripe webhook to your app's `[Function(Webhook = true)]` endpoint:

```csharp
[Function(Webhook = true, Name = "stripe", Description = "Stripe webhook receiver")]
public async Task<string> StripeWebhook(
    Dictionary<string, string> queryParams,
    Dictionary<string, string> headers,
    string body)
{
    headers.TryGetValue("Stripe-Signature", out var signature);
    var result = await _billing.HandleWebhookAsync(signature, body);
    if (!result.Verified)
    {
        Log.Instance.Warning($"Stripe webhook unverified: {result.Reason}");
    }
    return JsonSerializer.Serialize(new { received = true });
}
```

`HandleWebhookAsync` verifies the signature against `BillingOptions.WebhookSecret`,
parses the event into a typed `BillingEvent`, and delivers it to
`IBillingAppAdapter.ApplyEventAsync`. It never throws on signature failure or on
adapter exceptions — return HTTP 200 with `{ received: true }` either way to
avoid Stripe retry storms. When `Verified` is true but `AdapterError` is set,
the signature was valid and the event parsed cleanly but `ApplyEventAsync` threw;
log it and decide whether to acknowledge (200) or surface 500 to trigger a retry.

The Ikon webhook URL is exposed by the platform as
`https://{space}.ikonai.app/ikon/webhook/stripe`. Configure that URL in the
Stripe Dashboard and copy the signing secret into `BillingOptions.WebhookSecret`.

In Connect-platform mode, register a **second** endpoint with a separate
function (Stripe issues a different signing secret per endpoint type):

```csharp
[Function(Webhook = true, Name = "stripe-connect", Description = "Connect events")]
public async Task<string> StripeConnectWebhook(
    Dictionary<string, string> queryParams,
    Dictionary<string, string> headers,
    string body)
{
    headers.TryGetValue("Stripe-Signature", out var signature);
    var result = await _billingViaConnect.HandleWebhookAsync(signature, body);
    return JsonSerializer.Serialize(new { received = true });
}
```

Stripe Dashboard → Webhooks → "Add endpoint" twice: once with type
`Account` (paste `…/ikon/webhook/stripe`, copy `whsec_…` →
`STRIPE_WEBHOOK_SECRET`) and once with type `Connect` (paste
`…/ikon/webhook/stripe-connect`, copy the second `whsec_…` →
`STRIPE_CONNECT_WEBHOOK_SECRET`).

## Event types

| `BillingEventType` | Stripe event(s) |
|--------------------|-----------------|
| `CheckoutCompleted` | `checkout.session.completed` |
| `InvoicePaid` | `invoice.paid`, `invoice.payment_succeeded` |
| `InvoicePaymentFailed` | `invoice.payment_failed` |
| `InvoiceFinalized` | `invoice.finalized` (hosted-invoice flow signal) |
| `PaymentActionRequired` | `invoice.payment_action_required` (3DS / SCA prompts) |
| `SubscriptionUpdated` | `customer.subscription.created`, `customer.subscription.updated` |
| `SubscriptionDeleted` | `customer.subscription.deleted` |
| `ChargeRefunded` | `charge.refunded` |
| `ChargeDisputed` | `charge.dispute.created` |
| `ChargeDisputeClosed` | `charge.dispute.closed` |
| `SetupIntentSucceeded` | `setup_intent.succeeded` (card-on-file ready) |
| `PaymentMethodAttached` | `payment_method.attached` |
| `CreditNoteCreated` | `credit_note.created` |
| `CreditNoteVoided` | `credit_note.voided` |
| `Unknown` | anything else (raw payload preserved on `BillingEvent.RawPayload`) |

## Marketplace (Stripe Connect)

For apps where end-creators receive a share of payments (e.g. a creator
storefront), use `BillingConnectService` to onboard creators and
`BillingDestination` on checkout to route funds:

```csharp
var connect = new BillingConnectService(billingOptions);

// 1. Create an Express account for the creator
var acctId = await connect.CreateExpressAccountAsync(creatorEmail, "FI");

// 2. Send creator through hosted onboarding
var onboardingUrl = await connect.CreateOnboardingLinkAsync(
    acctId,
    refreshUrl: $"{appOrigin}/connect/refresh",
    returnUrl: $"{appOrigin}/connect/done");
// Redirect creator to onboardingUrl

// 3. Route a checkout payment to the creator with a 10% platform fee
await billing.CreateCheckoutAsync(
    planId: "tip-jar",
    appCustomerKey: null,
    email: fanEmail,
    destination: new BillingDestination(acctId, ApplicationFeeAmountMinor: 200));
```

Subscriptions use `ApplicationFeePercent` instead of `ApplicationFeeAmountMinor`.

## Platform-managed Connect mode

The marketplace pattern above is *destination charges*: platform-side
checkout, transferred to the connected account. The other Connect flow
is *direct charges*, where every API call runs on behalf of a connected
account via the `Stripe-Account` header. Use this when **the platform owns
the Stripe relationship** and each app/org onboards as a sub-account
(no per-app API keys to manage).

Set `BillingOptions.ConnectedAccountId` and the library injects the
header automatically on every call:

```csharp
var billing = new BillingService(
    new BillingOptions
    {
        ApiKey = platformMasterKey,           // single platform key
        ConnectedAccountId = "acct_xyz",       // resolved per app/org
        PlatformApplicationFeePercent = 5m,    // platform take rate (subscriptions)
        PlatformApplicationFeeAmountMinor = 100, // or flat fee (one-time)
        WebhookSecret = ...,
        DefaultMetadata = new Dictionary<string, string>
        {
            ["ikon_app_id"] = "myapp",         // tag every record with the app id
        },
    },
    new MyAdapter());
```

Charges, customers, products, prices land on `acct_xyz`; the platform
fee is applied automatically (no per-call `BillingDestination` needed).
Apps that share one connected account stay separable in reporting via
`DefaultMetadata`.

`BillingDestination` and `ConnectedAccountId` are mutually exclusive —
combining them throws `BillingConfigurationException` (Stripe rejects
the call too with an opaque 400).

### Embedded Connect Components — onboarding without leaving your app

`BillingConnectService.CreateAccountSessionAsync` mints a short-lived
`client_secret` you hand to Stripe's Connect.js. The frontend mounts
`<ConnectAccountOnboarding>` / `<ConnectAccountManagement>` /
`<ConnectPayouts>` / etc. **inline** — no redirect to stripe.com.

```csharp
var connect = new BillingConnectService(new BillingOptions { ApiKey = platformMasterKey });

// 1. Create the Express account (one-time per org)
var acctId = await connect.CreateExpressAccountAsync(
    email: ownerEmail, country: "FI",
    metadata: new Dictionary<string, string> { ["ikon_app_id"] = "myapp" });

// 2. Mint a session for the onboarding component
var session = await connect.CreateAccountSessionAsync(new BillingAccountSessionRequest
{
    ConnectedAccountId = acctId,
    AccountOnboarding = true,
    NotificationBanner = true,
});

// 3. Hand session.ClientSecret + your platform publishable key to the frontend.
//    Parallax: view.ConnectOnboardingFrame(session.ClientSecret, publishableKey);

// 4. After onExit, poll account status:
var acct = await connect.RetrieveAccountAsync(acctId);
if (acct.ChargesEnabled && acct.PayoutsEnabled) { /* unlock billing flows */ }

// 5. For ongoing self-service, mint a management session:
var mgmt = await connect.CreateAccountSessionAsync(new BillingAccountSessionRequest
{
    ConnectedAccountId = acctId,
    AccountManagement = true,
    Payouts = true,
    Balances = true,
    Payments = true,
    NotificationBanner = true,
    Documents = true,
});
// Frontend: view.ConnectAccountManagementFrame, ConnectPayoutsFrame, ...
```

Account session client secrets expire (~30 min). Stripe Connect.js
auto-rotates by calling `fetchClientSecret` again — the frontend resolver
should round-trip back to the server for a fresh secret on each call,
not memoize.

### Connect-mode Customer Portal

`BillingService.CreatePortalAsync` works in Connect mode — the
`Stripe-Account` header routes the portal session creation to the
connected account. **Caveat**: each connected account must have its
Customer Portal enabled in its own Stripe Express dashboard (Settings →
Customer Portal). New Express accounts have it disabled by default;
calling `CreatePortalAsync` against an unconfigured account returns 400
`No such customer portal configuration`. Two workarounds:

1. **Pre-provision per account.** Call `CreatePortalConfigurationAsync`
   on the connect-scoped service immediately after onboarding completes
   so the connected account starts with a default config.
2. **Document the gap.** Surface a "Configure billing portal in your
   Stripe dashboard" CTA when 400 errors fire.

### Connect webhooks

Connected-account events go to a separate endpoint registered with
`connect: true`:

```csharp
var endpoint = await connect.CreateConnectWebhookEndpointAsync(
    url: "https://myapp.ikonai.app/ikon/webhook/stripe-connect",
    enabledEvents: new[] {
        "account.updated",
        "capability.updated",
        "person.updated",
        "invoice.paid",
        "customer.subscription.updated",
        "customer.subscription.deleted",
    });
// Persist endpoint.Secret as your CONNECT webhook signing secret —
// it's separate from the platform endpoint's secret.
```

Connect events have a top-level `account` field. Apps that handle both
platform and Connect events from the same `[Function(Webhook = true)]`
endpoint inspect the body's `account` field and route to the appropriate
`BillingService` (one per connected account or shared depending on the
app shape).

### Code-bootstrap catalog with `BillingCatalogSync`

Apps that own their plan model (features, limits, seats) can declare
plans in code and let the library provision matching Stripe products and
prices. Stripe is still the source of truth for price ids, but devs
never touch the Dashboard:

```csharp
var sync = new BillingCatalogSync(billing);

var map = await sync.SyncAsync(new[]
{
    new BillingPlanSpec("free",  "Free",  0,    "eur", null,    Description: "Solo plan"),
    new BillingPlanSpec("pro",   "Pro",   1900, "eur", "month", Description: "Pro · monthly"),
    new BillingPlanSpec("team",  "Team",  4900, "eur", "month", Description: "Team · monthly"),
});

// Plug map into your adapter:
public Task<BillingPlanDescriptor?> GetPlanAsync(string planId, CancellationToken ct)
    => Task.FromResult<BillingPlanDescriptor?>(
        map.TryGetPriceId(planId, out var priceId)
            ? new BillingPlanDescriptor(planId, priceId, BillingMode.Subscription)
            : null);
```

Sync is idempotent: existing rows are reused, new rows created. Stripe
prices are immutable — changing a plan's price creates a *new* price;
existing subscribers stay on the old one until you migrate them via
`UpdateSubscriptionItemQuantityAsync` + a separate price-swap call.

Run `SyncAsync` once at app startup (~200ms cold), or persist the map
after first sync to skip the API hop on warm boots.

`SyncAsync` resolves prices via Stripe `lookup_key` (O(1) per plan,
no listing/pagination needed). Each plan auto-stamps lookup key
`ikon_app_<plan_id>` on its price; override via
`BillingPlanSpec.LookupKeyOverride` when you need a custom key.
Lookup-key-stamped prices use `transfer_lookup_key=true` on creation
so re-syncs after a price change keep the same lookup-key handle
pointing at the active price.

You can also bypass the sync helper entirely:

```csharp
// Lookup-key-only resolution — direct, no listing.
var price = await billing.RetrievePriceByLookupKeyAsync("ikon_app_pro");
// price.Id, price.UnitAmountMinor, price.RecurringInterval, price.LookupKey
```

### Paginating large catalogs

`ListProductsAsync` / `ListPricesAsync` return up to 100 rows. For
catalogs with more than 100 products, use the paginated overload:

```csharp
string? cursor = null;
do
{
    var page = await billing.ListProductsPageAsync(limit: 100, startingAfter: cursor);
    foreach (var product in page.Items) { /* ... */ }
    cursor = page.LastId;
}
while (page.HasMore);
```

Same pattern for `ListPricesPageAsync`. Cursors are Stripe-style
(`starting_after = last_id_seen`).

## Subscription management

```csharp
await billing.UpdateSubscriptionItemQuantityAsync(subItemId, 5);   // seat scaling
await billing.UpdateSubscriptionPriceAsync(subItemId, newPriceId); // migrate to new price (immutable-price replacement)
await billing.PauseSubscriptionAsync(subId);                       // pause invoicing
await billing.ResumeSubscriptionAsync(subId);                      // resume after pause
await billing.CancelSubscriptionAsync(subId, immediate: false);    // cancel at period end
await billing.ResumeCanceledSubscriptionAsync(subId);              // un-cancel (customer changed mind)
await billing.AddInvoiceItemAsync(customerId, 12500, "eur", "Mid-cycle add-on");
```

### Migrating subscribers after a plan price change

Stripe prices are immutable — bumping `Pro` from €19 to €25 creates a
new price; existing subscribers stay on the old one until you migrate
them. With `BillingCatalogSync` + `UpdateSubscriptionPriceAsync` the
recipe is:

```csharp
// 1. Update plan spec, sync catalog. New price replaces old under the
//    same lookup_key (transfer_lookup_key=true).
var map = await sync.SyncAsync(new[]
{
    new BillingPlanSpec("pro", "Validation Pro", 2500, "eur", "month"),  // bumped
});

// 2. Migrate active subscribers. Stripe prorates by default — pass
//    prorate: false for clean cycle boundaries.
foreach (var sub in await billing.ListSubscriptionsAsync(status: "active"))
{
    if (sub.ItemIds.Count == 0) continue;
    await billing.UpdateSubscriptionPriceAsync(sub.ItemIds[0], map.GetPriceId("pro"));
}
```

Keep the old price `Active = false` afterward via the Stripe Dashboard
(or expose a `BillingService.UpdatePriceAsync` if you need it
programmatically).

## Upcoming-invoice preview

Show "your next bill will be X" before committing a plan change:

```csharp
var preview = await billing.PreviewUpcomingInvoiceAsync(
    stripeCustomerId: customerId,
    subscriptionId: subId,
    newPriceId: "price_pro_yearly",   // preview as if customer switched
    newQuantity: 5);                   // and bumped to 5 seats

Console.WriteLine($"Next bill: {preview.AmountDueMinor / 100m:0.00} {preview.Currency}");
foreach (var line in preview.Lines)
{
    Console.WriteLine($"  {line.Description} · {line.AmountMinor / 100m:0.00}{(line.Proration ? " (prorated)" : "")}");
}
```

## Subscription listing + management

```csharp
var subs = await billing.ListSubscriptionsAsync(stripeCustomerId: customerId, status: "active");
foreach (var s in subs)
{
    Console.WriteLine($"{s.Id} · {s.Status} · ends {s.CurrentPeriodEnd:yyyy-MM-dd}");
}

await billing.UpdateSubscriptionScheduleAsync(scheduleId, newPhases);
await billing.CancelSubscriptionScheduleAsync(scheduleId);
```

## Credit notes (formal partial refunds)

When tax was charged on the original invoice, use a credit note rather than a
plain refund — Stripe handles the tax reversal and regenerates the invoice PDF:

```csharp
var cn = await billing.CreateCreditNoteAsync(new BillingCreditNoteInfo
{
    InvoiceId = "in_abc",
    AmountMinor = 1500,
    RefundAmountMinor = 1000,        // back to card
    CreditAmountMinor = 500,         // to customer balance
    Memo = "Service downtime · 2026-04-12",
    Reason = "duplicate",
});
// cn.PdfUrl — link to the regenerated PDF
await billing.VoidCreditNoteAsync(cn.Id);  // if issued by mistake
```

Listen for `BillingEventType.CreditNoteCreated` / `CreditNoteVoided`.

## Customer Portal configuration

For more control than the Stripe Dashboard defaults, create a configuration
once and reuse the id when opening portal sessions:

```csharp
var configId = await billing.CreatePortalConfigurationAsync(new BillingPortalConfigurationInfo
{
    BusinessProfileHeadline = "Manage your subscription",
    AllowSubscriptionCancel = true,
    SubscriptionCancelMode = "at_period_end",
    AllowSubscriptionPause = false,
    PrivacyPolicyUrl = $"{appOrigin}/privacy",
    TermsOfServiceUrl = $"{appOrigin}/terms",
});
// Persist configId; pass to CreatePortalAsync via Stripe Dashboard or future overload.
```

## Webhook replay (audit / outage recovery)

If the app misses webhook deliveries (downtime, dropped requests), replay
events directly from Stripe:

```csharp
var since = DateTimeOffset.UtcNow.AddHours(-24);
var ids = await billing.ListEventIdsAsync(createdAfter: since, limit: 100);
foreach (var evtId in ids)
{
    var evt = await billing.RetrieveEventAsync(evtId);
    await myAdapter.ApplyEventAsync(evt, ct);  // skip signature check; came from Stripe API
}
```

## Customer tax IDs (B2B)

```csharp
var tax = await billing.CreateCustomerTaxIdAsync(customerId, "eu_vat", "FI12345678");
// Persist tax.Id if you need to delete later
await billing.DeleteCustomerTaxIdAsync(customerId, tax.Id);
```

## Apple Pay domain

```csharp
await billing.RegisterApplePayDomainAsync("checkout.example.com");
// Domain must serve the Apple Pay verification file (Stripe handles this in Checkout).
```

## Reporting (charge + invoice listings)

For receipts, billing history, admin dashboards:

```csharp
var charges = await billing.ListChargesAsync(stripeCustomerId: customerId, limit: 50);
foreach (var c in charges)
{
    Console.WriteLine($"{c.Created:yyyy-MM-dd} · {c.AmountMinor / 100m:0.00} {c.Currency} · {c.Status}");
}

var invoices = await billing.ListInvoicesAsync(stripeCustomerId: customerId, status: "paid");
```

## Webhook endpoint registration

Apps can self-provision webhook endpoints instead of clicking through the Stripe Dashboard:

```csharp
var endpoint = await billing.CreateWebhookEndpointAsync(
    url: "https://myapp.ikonai.app/ikon/webhook/stripe",
    enabledEvents: new[]
    {
        "checkout.session.completed",
        "invoice.paid",
        "invoice.payment_failed",
        "customer.subscription.updated",
        "customer.subscription.deleted",
    },
    description: "MyApp prod webhook");

// IMPORTANT: persist endpoint.Secret immediately — Stripe returns it only on creation.
// Use it as BillingOptions.WebhookSecret on the next BillingService construction.
```

## Embedded Checkout

For in-app checkout without redirecting to Stripe's hosted page:

```csharp
var embed = await billing.CreateEmbeddedCheckoutAsync(
    planId: "pro-monthly",
    appCustomerKey: appId.ToString(),
    email: ownerEmail,
    returnUrl: $"{appOrigin}/billing/done?session={{CHECKOUT_SESSION_ID}}");

// Pass embed.ClientSecret to Stripe.js / @stripe/react-stripe-js EmbeddedCheckoutProvider
```

## Payment intents (custom in-app flows)

When Checkout isn't the right fit (custom card form, deferred capture, off-session subscription charge):

```csharp
// Authorize now, capture later
var pi = await billing.CreatePaymentIntentAsync(
    amountMinor: 10000, currency: "eur", stripeCustomerId: customerId,
    captureMethod: "manual");

// Pass pi.ClientSecret to Stripe Elements for collection.

// Later, when goods ship / service rendered:
await billing.CapturePaymentIntentAsync(pi.Id);

// Or off-session charge with saved card:
var pi2 = await billing.CreatePaymentIntentAsync(
    5000, "eur", customerId, paymentMethodId: "pm_123", confirm: true);
```

## Customer search

```csharp
var ids = await billing.SearchCustomersAsync("email:'biz@example.com'");
var byMeta = await billing.SearchCustomersAsync("metadata['app_id']:'abc'");
```

## Coupon / promo code listings

```csharp
var couponIds = await billing.ListCouponsAsync();
var promoIds = await billing.ListPromotionCodesAsync();
```

## Customer management

```csharp
var customerId = await billing.CreateCustomerAsync(new BillingCustomerInfo
{
    Email = "biz@example.com",
    Name = "Acme Oy",
    AddressLine1 = "Main 1",
    AddressCity = "Helsinki",
    AddressCountry = "FI",
});

await billing.UpdateCustomerAsync(customerId, new BillingCustomerInfo { Name = "Renamed Oy" });

// Goodwill credit (negative = reduces future invoices)
await billing.AdjustCustomerBalanceAsync(customerId, -1500, "eur", "Service downtime credit", idempotencyKey: "credit-2026-04");
```

## Hosted invoices (B2B net-30)

For invoiced sales — no Checkout flow, customer pays via emailed link:

```csharp
var invoice = await billing.CreateHostedInvoiceAsync(
    stripeCustomerId: customerId,
    lines: new[] { BillingLineItem.Dynamic(50000, "eur", "Consulting · April 2026") },
    daysUntilDue: 30,
    autoSend: true);

// invoice.HostedInvoiceUrl — payable link
// invoice.InvoicePdfUrl   — PDF download
```

## Coupons

```csharp
var couponId = await billing.CreateCouponAsync(new BillingCouponInfo
{
    Id = "LAUNCH50",
    Name = "Launch 50%",
    PercentOff = 50,
    Duration = BillingCouponDuration.Repeating,
    DurationInMonths = 3,
});
```

Set exactly one of `PercentOff` or `AmountOffMinor` (the latter requires `Currency`). For repeating coupons, `DurationInMonths` is required.

## Setup intents (card on file, trial → paid)

When a customer signs up for a trial without paying, capture a payment method
up front so the conversion is silent at trial end:

```csharp
var setup = await billing.CreateSetupIntentAsync(stripeCustomerId);
// Pass setup.ClientSecret to Stripe.js / Elements on the frontend to confirm.
// On success, listen for BillingEventType.SetupIntentSucceeded.
```

## Payment methods

```csharp
var methods = await billing.ListPaymentMethodsAsync(stripeCustomerId);
foreach (var m in methods)
{
    Console.WriteLine($"{m.CardBrand} ****{m.CardLast4} {m.CardExpMonth}/{m.CardExpYear}");
}

await billing.DetachPaymentMethodAsync("pm_1");
```

## Subscription schedules (multi-phase pricing)

Schedule a subscription that transitions through phases — e.g. discounted intro
followed by full price:

```csharp
var phases = new[]
{
    new BillingSubscriptionPhase("price_intro_50pct", Iterations: 3),
    new BillingSubscriptionPhase("price_full"),  // open-ended after iter 3
};

var scheduleId = await billing.CreateSubscriptionScheduleAsync(stripeCustomerId, phases);
```

## Promotion codes

```csharp
// Create a launch campaign code attached to an existing Stripe coupon
await billing.CreatePromotionCodeAsync(
    couponId: "coup_50_off",
    code: "LAUNCH50",
    expiresAt: DateTimeOffset.UtcNow.AddMonths(1),
    maxRedemptions: 100);
```

## Patterns by app type

| App type | Use |
|---|---|
| **SaaS subscription with seats** (CoPlanAI, Mitigram) | `BillingPlanDescriptor` with `MeteredPriceId` for overage. `UpdateSubscriptionItemQuantityAsync` for seat changes. |
| **Trial then paid** (B2B onboarding) | `BillingPlanDescriptor.TrialPeriodDays = 14`. |
| **One-time per-item with guest checkout** (RailGo, ReissuJuna) | `CreateCheckoutAsync(planId, appCustomerKey: null, email)`. |
| **Cart with multiple items** (commerce) | `CreateCartCheckoutAsync(lines, BillingMode.OneTime, ...)`. |
| **Tipping / donation / pay-what-you-want** | `BillingLineItem.Dynamic(amountMinor, currency, productName)`. |
| **Credit packs** (LLM/image-gen apps) | `CreateCheckoutAsync` with metadata `{ "credits": "100" }`; on `CheckoutCompleted` event credit the app balance. |
| **Promo codes** | `BillingPlanDescriptor.AllowPromotionCodes = true`. |
| **VAT / sales tax** | `BillingOptions.AutomaticTax = true` (configure tax rates in Stripe Dashboard). |
| **Refunds** (cancellations) | `RefundAsync(paymentIntentId, amountMinor: null, reason: "requested_by_customer", idempotencyKey)`. |
| **Programmatic cancel** | `CancelSubscriptionAsync(subId, immediate: false)` to cancel at period end. |
| **3DS / SCA flows** | Listen for `BillingEventType.PaymentActionRequired`; surface a payment-update prompt to the user. |

## Metering

For metered overage prices, apps report usage as it occurs:

```csharp
await billing.ReportUsageAsync(
    meterEventName: "image_generations",
    stripeCustomerId: "cus_...",
    value: 1,
    idempotencyKey: usageRecordId.ToString());
```

The library posts to Stripe's Meter Events API. Use a stable `idempotencyKey`
(usage record id, not a random GUID) so retries do not double-bill.

## Secrets

The library does not read environment variables. Apps load secrets from
wherever they prefer — typically `IkonBackend.GetSecretsAsync(spaceId)` with
env-var fallback — and pass them on `BillingOptions`. Required:

- `STRIPE_API_KEY` (`sk_test_…` local, `sk_live_…` cloud)
- `STRIPE_WEBHOOK_SECRET` (`whsec_…`)

## Testing

Use the Stripe CLI to forward events to a local app:

```
stripe listen --forward-to https://localhost:9443/ikon/webhook/stripe
stripe trigger checkout.session.completed
stripe trigger invoice.payment_failed
```

Test card `4242 4242 4242 4242` works in test mode (any future expiry,
any 3-digit CVC, any postal code).

For Connect onboarding KYC shortcuts in test mode:

| Field | Value | Effect |
|---|---|---|
| Date of birth | `1901-01-01` | Verified match (no real ID needed) |
| SSN / tax-id | `000000000` | Auto-pass |
| ID document | file token `file_identity_document_success` | Auto-verify |
| SMS code | `000-000` | Auto-confirm |

These bypass real KYC; they only work with `sk_test_…`. The validation
app's onboarding frame accepts them so Connect testing doesn't require
an actual identity.

## Errors

- `BillingConfigurationException` — missing or empty configuration value.
- `BillingApiException` — Stripe API returned a non-2xx response. Exposes `StatusCode`, `ResponseBody`, plus structured Stripe fields parsed from the body: `ErrorType`, `ErrorCode`, `DeclineCode`, `StripeMessage`, `ParamPath`. Apps branch on these:

```csharp
try { await billing.CreateCheckoutAsync(...); }
catch (BillingApiException ex) when (ex.ErrorCode == "card_declined")
{
    if (ex.DeclineCode == "insufficient_funds") { /* show top-up prompt */ }
}
```

- `BillingException` — base class for the above; also thrown for unknown plan ids.

Webhook signature failures do not throw — they return
`BillingWebhookResult { Verified = false, Reason = ... }`.

## Idempotency

Every public POST method on `BillingService` and `BillingConnectService`
accepts a `string? idempotencyKey` parameter. Pass a stable key derived
from app state (e.g. `checkout-{userId}-{planId}-{sessionStart}`,
`customer-{appCustomerKey}`, `transfer-{transactionId}`) so:

- Concurrent app replicas booting together can't double-create rows.
- Network-level retries (load balancer, browser double-click) replay
  Stripe's original response within 24h instead of duplicating.
- Pre-commit retry loops (after a 5xx) are safe to fire.

Recommended key formats:

| Method | Suggested key |
|---|---|
| `CreateCustomerAsync` | `customer-{appCustomerKey}` |
| `CreateProductAsync` | `product-{stableName}` (used by `BillingCatalogSync`) |
| `CreatePriceAsync` | `price-{lookupKey}-{amountMinor}-{currency}-{interval}` |
| `CreateCheckoutAsync` / `CreateEmbeddedCheckoutAsync` | `checkout-{planId}-{userId}-{minute}` |
| `CreatePaymentIntentAsync` | `pi-{orderId}` |
| `CreateSetupIntentAsync` | `si-{customerId}-{purpose}` |
| `RefundAsync` | `refund-{chargeId}` (already required by signature) |
| `CreateExpressAccountAsync` | `acct-{orgId}` |
| `CreateAccountSessionAsync` | rarely needed (sessions short-lived; React Strict Mode duplicate is the main case) |
| `CreateConnectWebhookEndpointAsync` / `CreateWebhookEndpointAsync` | `webhook-{environment}` |

`BillingCatalogSync` already passes deterministic keys for its product
and price creates; you don't need to plumb keys through it manually.

## Retries and timeouts

Transient failures (HTTP 429 rate limit, 5xx, network faults) auto-retry with
exponential backoff + jitter on idempotent calls. Tune via `BillingOptions`:

```csharp
new BillingOptions
{
    ApiKey = key,
    MaxRetryAttempts = 3,                                  // 0 disables retries
    RetryBaseDelay = TimeSpan.FromMilliseconds(500),       // doubles each attempt
    RequestTimeout = TimeSpan.FromSeconds(30),             // per-call HTTP timeout
}
```

Retries fire only when the call is idempotent — every GET, plus any POST that
supplied an `Idempotency-Key`. Apps that want POST-side retries should pass
idempotency keys (already done for `RefundAsync`, `ReportUsageAsync`,
`AdjustCustomerBalanceAsync`, `TransferAsync`).

## Catalog (products + prices + payment links)

```csharp
var prodId = await billing.CreateProductAsync(new BillingProductInfo
{
    Id = "pro-monthly",
    Name = "Pro · Monthly",
    Description = "Pro tier",
});

var priceId = await billing.CreatePriceAsync(new BillingPriceInfo
{
    ProductId = prodId,
    UnitAmountMinor = 4999,
    Currency = "eur",
    RecurringInterval = "month",
});

var products = await billing.ListProductsAsync();
var prices = await billing.ListPricesAsync(productId: prodId);

var link = await billing.CreatePaymentLinkAsync(
    new[] { BillingLineItem.ForPrice(priceId) },
    allowPromotionCodes: true);
// Share link.Url anywhere — chat, email, QR code.
```

## Declarative gating with policy attributes

The library ships three `[Billing.*]` policy attributes for declarative
gating of `[Function]`-registered methods. Each resolves the ambient
`BillingService.Current` (set automatically by the constructor), reads the
caller's `PolicyCallContext.UserId`, and either allows the function to
proceed or returns a typed `PolicyDecision.Deny` with a stable code so the
calling UI can react.

```csharp
public class ImageGenerator
{
    [Function]
    [BillingRequireSubscription("pro")]
    public Task<Image> Premium(string prompt) { /* runs only when entitled */ }

    [Function]
    [BillingRequireUnlock("hd-pack")]
    public Task<Image> HighDef(string prompt) { /* gated on one-time unlock */ }

    [Function]
    [BillingChargeCredits("image-credits", credits: 1)]
    public Task<Image> Standard(string prompt) { /* deducts 1 credit on entry */ }
}
```

Deny codes (stable across versions):

| Attribute | Deny code when missing | Other deny codes |
|---|---|---|
| `[BillingRequireSubscription]` | `billing_subscription_required` | `billing_no_user`, `billing_not_initialized` |
| `[BillingRequireUnlock]` | `billing_unlock_required` | same |
| `[BillingChargeCredits]` | `billing_credits_insufficient` | `billing_no_credit_store`, `billing_credits_deduction_error` |

**Design note**: unlike Mint's equivalent, these policies **do NOT
auto-open Stripe Checkout** on denial. Ikon.App.Billing is webhook-driven,
not polling-driven. The policy's role is gate + signal; the app's UI
catches the deny code, calls `OfferCheckoutAsync`, and waits for the
webhook to flip entitlement. The user clicks the same button again and
the policy now allows.

## Reading entitlements

`BillingService.GetEntitlementAsync` composes a single `BillingEntitlement`
record from three sources: Stripe subscriptions (filtered by price),
customer metadata (`unlock_{planId}` stamp), and an optional
`IBillingCreditStore`. Apps read this one record instead of orchestrating
three Stripe calls.

```csharp
var ent = await billing.GetEntitlementAsync("pro", appCustomerKey);

if (ent.SubscriptionActive)
    Render($"Pro · renews {ent.SubscriptionEndsAt:d}");
else if (ent.UnlockGranted)
    Render($"Lifetime · purchased {ent.UnlockGrantedAt:d}");
else
    RenderUpgradeCTA();
```

For credit-based products, supply an `IBillingCreditStore` (or set
`billing.CreditStore` once at startup so the policy attribute can find it):

```csharp
public sealed class MyCreditStore : IBillingCreditStore
{
    public Task<int> GetCreditsAsync(string customerKey, string sku, CancellationToken ct) { /* DB read */ }
    public Task<int> DeductAsync(string customerKey, string sku, int credits, string idemKey, CancellationToken ct) { /* DB write, dedup by idemKey */ }
    public Task<int> GrantAsync(string customerKey, string sku, int credits, string idemKey, CancellationToken ct) { /* DB write */ }
}

billing.CreditStore = new MyCreditStore(db);
```

The adapter calls `GrantAsync` from `ApplyEventAsync` on
`CheckoutCompleted` for credit-bundle products; the
`[BillingChargeCredits]` policy calls `DeductAsync` on each function entry.

## Named factories

For readability, `BillingPlanDescriptor` and `BillingPlanSpec` ship named
static factories that hide the mode enum:

```csharp
// In your adapter:
BillingPlanDescriptor.Subscription("pro", "price_pro_monthly", trialPeriodDays: 7)
BillingPlanDescriptor.Unlock("hd-pack", "price_hd_pack")
BillingPlanDescriptor.Credits("image-credits", "price_credits_100", creditsGranted: 100)

// In your catalog declaration:
public static readonly BillingPlanSpec Pro = BillingPlanSpec.Subscription("pro", "Pro plan", 1900, "eur", "month");
public static readonly BillingPlanSpec Lifetime = BillingPlanSpec.Unlock("life", "Lifetime", 9900, "eur");
public static readonly BillingPlanSpec ImgCredits = BillingPlanSpec.Credits("img-credits", "Image credits", 999, "eur", creditsGranted: 100);
```

Pair with `BillingCatalogSync.SyncFromCatalogClassAsync(typeof(Plans))` to
provision Stripe products + prices in one call, mirroring Mint's catalog
pattern but talking directly to Stripe:

```csharp
public static class Plans
{
    public static readonly BillingPlanSpec Pro = BillingPlanSpec.Subscription("pro", "Pro", 1900, "eur", "month");
    public static readonly BillingPlanSpec Team = BillingPlanSpec.Subscription("team", "Team", 4900, "eur", "month");
}

var sync = new BillingCatalogSync(billing);
var map = await sync.SyncFromCatalogClassAsync(typeof(Plans));
// map.GetPriceId("pro") -> the Stripe price id resolved/created for the Pro plan.
```

## Auto-checkout convenience

`OfferCheckoutAsync` short-circuits when the customer already holds the
plan and otherwise mints a fresh hosted Checkout session:

```csharp
var offer = await billing.OfferCheckoutAsync("pro", appCustomerKey, email);

if (offer.AlreadyEntitled)
{
    _status.Value = "You're already on Pro. Enjoy!";
}
else
{
    await ClientFunctions.SetUrlAsync(offer.Url!);
}
```

No polling — the webhook completes the entitlement and the reactive UI
re-renders when the user returns to the app.

## Tips

`CreateTipCheckoutAsync` issues a one-time checkout for a dynamic amount.
Pair with the Parallax `TipPresetGrid` component for the preset UI:

```csharp
view.TipPresetGrid(
    presetsMinor: new long[] { 100, 500, 2000 },
    currencySymbol: "€",
    onTip: async minor =>
    {
        var session = await billing.CreateTipCheckoutAsync(minor, "eur", title: "Thanks!");
        await ClientFunctions.SetUrlAsync(session.Url);
    });
```

## Resuming a canceled subscription

`SubscriptionStatus` accepts an optional `onResume` callback. When
`CancelAtPeriodEnd` is true the component renders a one-click "Resume
subscription" button that the app wires to
`BillingService.ResumeCanceledSubscriptionAsync`.

```csharp
view.SubscriptionStatus(
    sub,
    onResume: () => billing.ResumeCanceledSubscriptionAsync(sub.Id));
```

## Customer search

`SearchCustomersByAppKeyAsync` wraps the metadata-search idiom for
resolving a Stripe customer from an app's stable user key:

```csharp
var ids = await billing.SearchCustomersByAppKeyAsync("user-42");
// Or with a non-default metadata key:
// var ids = await billing.SearchCustomersByAppKeyAsync("user-42", metadataKey: "org_id");
```

Returns matched customer ids (typically 0 or 1). Implements proper
single-quote escaping per Stripe Search query syntax.

## Handling payment failures (dunning)

When `BillingEventType.InvoicePaymentFailed` arrives, Stripe has already
queued automatic retries (up to 3 over ~3 weeks by default). After the
last retry the subscription transitions to `past_due` and an
`InvoicePaymentActionRequired` may fire if the customer's card needs
re-authentication. Recommended app flow:

1. **First failure**: log it, optionally email the customer ("update your
   card to avoid losing access").
2. **Past-due status** (read via `BillingEntitlement.SubscriptionStatus`):
   surface a top-of-app banner with a link to the Customer Portal so the
   user can update their payment method.
3. **`PaymentActionRequired`**: open the latest invoice's hosted URL —
   Stripe's UI walks the user through 3DS re-auth.
4. **Final cancellation** (`SubscriptionDeleted`): downgrade access and
   prompt re-subscription.

The library doesn't impose this flow; it just delivers the typed events.

## Trial-will-end notifications

Stripe fires `customer.subscription.trial_will_end` ~3 days before a free
trial ends. The library maps this to
`BillingEventType.SubscriptionTrialWillEnd`. Apps use the event to remind
users to confirm payment method or to surface a "trial ending" banner.

```csharp
public Task ApplyEventAsync(BillingEvent evt, CancellationToken ct)
{
    if (evt.Type == BillingEventType.SubscriptionTrialWillEnd)
    {
        // Notify customer; surface in UI.
    }
    return Task.CompletedTask;
}
```

## Webhook idempotency

`BillingEvent.EventId` is unique per Stripe delivery; Stripe replays the
same event on retry. Apps MUST dedupe by storing seen event ids:

```csharp
public async Task ApplyEventAsync(BillingEvent evt, CancellationToken ct)
{
    if (await _seenEvents.HasAsync(evt.EventId))
        return;  // already processed

    // mutate app DB
    await _seenEvents.RecordAsync(evt.EventId);
}
```

Critically: when the adapter throws (DB transient failure, deserialization
bug, etc.), `BillingService.HandleWebhookAsync` returns
`Verified=true, AdapterError=<message>`. Apps decide whether to return 200
(acknowledge — Stripe won't retry) or 500 (let Stripe retry). For
adapter bugs causing DB writes to fail, 500 + log is usually correct so
Stripe replays once the bug is fixed.

## Wiring billing into an Ikon app's frontend

The C# Parallax billing components (`view.EmbeddedCheckoutFrame`,
`view.ConnectOnboardingFrame`, etc.) emit custom node types that need a
React resolver on the frontend to mount Stripe.js / Stripe Connect.js.
This lives in the shared `@ikonai/sdk-react-ui-billing` package — every
Ikon app uses the same resolver.

### Install

```bash
npm install @ikonai/sdk-react-ui-billing \
  @stripe/connect-js \
  @stripe/react-connect-js \
  @stripe/stripe-js \
  @stripe/react-stripe-js
```

The four `@stripe/*` packages are peer-dependencies of the SDK package —
the consumer app pins the versions.

### Register the module

```tsx
// frontend-node/src/app.tsx
import { registerBillingModule } from '@ikonai/sdk-react-ui-billing';
import { registerStandardUiModule } from '@ikonai/sdk-react-ui-standard';

const app = useIkonApp({
  modules: [
    registerStandardUiModule,
    registerBillingModule,
    // ...other modules
  ],
});
```

That single call wires resolvers for all 8 billing node types
(`ikon-billing-embedded-checkout` + 7 `ikon-billing-connect-*`).

### Required server-side `[Function]` exports

The Connect-mode resolvers expect three `[Function(Visibility = Shared)]`
methods on the host app's main class so the frontend can refresh expired
account-session client secrets:

```csharp
[Function(Name = "FetchConnectOnboardingSecret", Visibility = FunctionVisibility.Shared)]
public Task<string> FetchConnectOnboardingSecretAsync() { ... }

[Function(Name = "FetchConnectManagementSecret", Visibility = FunctionVisibility.Shared)]
public Task<string> FetchConnectManagementSecretAsync() { ... }

[Function(Name = "OnConnectOnboardingExit", Visibility = FunctionVisibility.Shared)]
public Task OnConnectOnboardingExitAsync() { ... }
```

`Validation.Billing.Bootstrap.cs` in the validation app has the reference
implementation — mint a fresh `BillingAccountSession` each call.

## Embedded vs hosted flow matrix

The validation app showcases every billing component end-to-end. This
table summarises which Stripe flows stay inline in the host app and which
unavoidably navigate to a Stripe-hosted page.

| Flow | Default behavior | Notes |
|---|---|---|
| `EmbeddedCheckoutFrame` | Inline iframe | Default checkout mode; pair with `BillingService.CreateEmbeddedCheckoutAsync`. |
| `ConnectOnboardingFrame` | Inline iframe | `collectionOptions: { fields: eventually_due, futureRequirements: include }` is set inside the SDK so onboarding collects everything in one pass. Identity-verification / bank-account-linking still pop a Stripe-controlled popup window — non-overridable for security. |
| `ConnectAccountManagementFrame` / `ConnectPayoutsFrame` / `ConnectBalancesFrame` / `ConnectPaymentsFrame` / `ConnectNotificationBanner` / `ConnectDocumentsFrame` | Inline iframe | All embedded; no popouts. |
| Hosted Stripe Checkout (`CreateCheckoutAsync` + `ClientFunctions.SetUrlAsync`) | Same-tab redirect to Stripe-hosted page | By Stripe design. Prefer `CreateEmbeddedCheckoutAsync` to stay in-app. |
| Stripe Customer Portal (`CreatePortalAsync` + `BillingPortalButton`) | Same-tab redirect | No Stripe-supplied embedded equivalent for the regular Customer Portal. In Connect mode use `ConnectAccountManagementFrame` for inline payment-method / invoice management. |
| `CreateOnboardingLinkAsync` / `CreateLoginLinkAsync` | Same-tab redirect | Legacy hosted paths. Prefer `ConnectOnboardingFrame` and the Connect embedded components. |
| `CreatePaymentLinkAsync` | Always external (shareable URL) | By Stripe design — used for chat/email/QR distribution. |

**Rule of thumb**: in Connect mode every customer-facing flow is
embedded by default. In BYOK mode hosted Checkout + Customer Portal are
Stripe-locked external redirects; embedded checkout is the in-app
alternative.

## Parallax billing components reference

The SDK ships 21 drop-in Parallax components covering every end-user
billing surface (catalog browsing, checkout, subscription management,
saved cards, invoices, Connect onboarding). All components live in
`Ikon.Parallax.Components.Standard.BillingExtensions` (C#) and resolve
their Stripe iframes via `@ikonai/sdk-react-ui-billing` (TypeScript).

Each entry below shows the C# signature, when to use it, and the
`BillingService` method it conceptually wraps. See the validation app's
**Components catalog** tab for live samples with synthetic data.

### Plan & pricing

| Component | Wraps | Use when |
|-----------|-------|----------|
| `view.PricingTable(plans, onSelect)` | `CreateCheckoutAsync` or `CreateEmbeddedCheckoutAsync` | Render a 1/2/3/4-column plan grid. `onSelect` receives the plan id; your handler calls the checkout method and either redirects or stores the embedded `client_secret`. |
| `view.PlanCard(plan, onSelect)` | Same as above | Single-plan callout outside the grid. Used internally by `PricingTable`. |

### Checkout & CTAs

| Component | Wraps | Use when |
|-----------|-------|----------|
| `view.CheckoutButton(onCheckout, text)` | `CreateCheckoutAsync` | Primary CTA. Handler returns a URL; component redirects via `ClientFunctions.SetUrlAsync`. |
| `view.BillingPortalButton(onOpenPortal)` (BYOK) | `CreatePortalAsync` | Same-tab redirect to Stripe-hosted Customer Portal. |
| `view.BillingPortalButton(connectAccountSessionClientSecret, publishableKey)` (Connect) | `CreateAccountSessionAsync` | Auto-switches to embedded `ConnectAccountManagementFrame` when a Connect secret is supplied — there's no hosted portal for connected accounts. |
| `view.TipPresetGrid(presetsMinor, currencySymbol, onTip)` | `CreateTipCheckoutAsync` | Preset tip amounts row → one-shot tip checkout. |

### Stripe embedded iframes

| Component | Wraps | Use when |
|-----------|-------|----------|
| `view.EmbeddedCheckoutFrame(clientSecret, publishableKey)` | `CreateEmbeddedCheckoutAsync` | In-app Stripe Checkout. Show after `PricingTable` `onSelect` stores the `client_secret`. |
| `view.SetupIntentFrame(clientSecret, publishableKey, returnUrl?)` | `CreateSetupIntentAsync` | Save card without immediate charge. Mounts Stripe `<PaymentElement>` and confirms via `stripe.confirmSetup`. |
| `view.PaymentIntentFrame(clientSecret, publishableKey, returnUrl?)` | `CreatePaymentIntentAsync` | Confirm a one-shot payment (deferred capture, marketplace escrow). Mounts `<PaymentElement>` and confirms via `stripe.confirmPayment`. |
| `view.ConnectOnboardingFrame(accountSessionClientSecret, publishableKey)` | `BillingConnectService.CreateAccountSessionAsync` w/ `AccountOnboarding=true` | Embedded Stripe Express KYC. |
| `view.ConnectAccountManagementFrame(...)` | Same with `AccountManagement=true` | Bank / business detail updates. |
| `view.ConnectPayoutsFrame(...)` | `Payouts=true` | Payouts list + schedule. |
| `view.ConnectBalancesFrame(...)` | `Balances=true` | Available + pending balance. |
| `view.ConnectPaymentsFrame(...)` | `Payments=true` | Charges + dispute/capture controls (Connect-scoped). |
| `view.ConnectNotificationBanner(...)` | `NotificationBanner=true` | Stripe-issued action items (KYC reminders, etc.). |
| `view.ConnectDocumentsFrame(...)` | `Documents=true` | Tax forms / 1099 docs. |

### Lists

| Component | Wraps | Use when |
|-----------|-------|----------|
| `view.PaymentMethodList(methods, onDetach?, onAddCard?, setupIntentClientSecret?, publishableKey?)` | `ListPaymentMethodsAsync` + `DetachPaymentMethodAsync` + `CreateSetupIntentAsync` | Saved-cards "wallet". Pass `onAddCard` to enable the in-component Add card flow; when your handler stores the new `setupIntentClientSecret`, the component mounts `SetupIntentFrame` inline automatically. |
| `view.InvoiceList(invoices)` | `ListInvoicesAsync` | Past invoices with hosted + PDF links. |
| `view.ChargeList(charges, onRefund?)` | `ListChargesAsync` + `RefundAsync` | Charge rows with Receipt links and optional inline refund button (only renders for paid, non-refunded charges with a `PaymentIntentId`). |

### Status

| Component | Wraps | Use when |
|-----------|-------|----------|
| `view.SubscriptionStatus(subscription, onResume?, onCancel?, onCancelImmediate?, onPause?, onResumeFromPause?, footer?)` | `Cancel/Resume/Pause/ResumeCanceledSubscriptionAsync` | Single subscription card with status pill + 5 optional action callbacks. Each button renders only when its handler is supplied and the action makes sense for the current state. Use `footer` for app-specific extras (seat scaling, plan migration). |
| `view.SubscriptionList(subscriptions, onResume?, onCancel?, ..., projector?, footer?)` | `ListSubscriptionsAsync` + the same action methods | Renders N `SubscriptionStatus` cards. Each callback receives the subscription id of the row that fired it; `projector` lets you map a `BillingSubscription` to a richer `BillingSubscriptionView` (resolve plan label + price label from the price id). |
| `view.UpcomingInvoicePreview(preview)` | `PreviewUpcomingInvoiceAsync` | "Your next bill = €49 · €5 proration credit". Pure display; fetch the preview upstream and pass the resulting `BillingUpcomingInvoice`. |

### What NOT to expect as a component

Catalog CRUD, customer create/update/search, webhook endpoint
registration, coupon / promo code creation, customer balance
adjustments, tax-id and Apple-Pay domain registration, hosted invoice
forms, credit notes, subscription schedules, and portal configuration
are **intentionally programmatic** — they vary too much across app
admin surfaces to ship as one-size-fits-all components. Wire them via
`BillingService.*Async` calls inside your app's own admin UI (see the
validation app's **Admin actions** tab for examples).

### Frontend resolver

Every Stripe-iframe component (`EmbeddedCheckoutFrame`,
`SetupIntentFrame`, `PaymentIntentFrame`, and all 7 `Connect*Frame`s)
emits a Parallax node that the React resolver in
`@ikonai/sdk-react-ui-billing` mounts as a Stripe.js component. Register
the module in your app's `app.tsx`:

```tsx
import { registerBillingModule } from '@ikonai/sdk-react-ui-billing';

useIkonApp({
  modules: [registerStandardUiModule, registerLucideIconsModule, registerBillingModule, ...]
});
```

Then add the four Stripe peer dependencies in `package.json`:

```bash
npm install @ikonai/sdk-react-ui-billing \
  @stripe/connect-js @stripe/react-connect-js \
  @stripe/stripe-js @stripe/react-stripe-js
```
