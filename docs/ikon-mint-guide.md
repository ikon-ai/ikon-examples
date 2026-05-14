# Ikon Mint Guide

Creator monetization for Ikon apps. Wire up Stripe-backed payments — top-ups, one-time unlocks, recurring subscriptions, tips — without owning a payments backend.

## TL;DR — what you wire

```csharp
// 1. Declare the catalog (one file per app)
public static class Products
{
    public static readonly MintCreditsProduct ImageGen = new(
        Sku: "image-generation",
        TopupBundles: [new(Credits: 20, PriceEur: 4.99m), new(100, 19.99m)],
        Title: "Image generation credits");

    public static readonly MintUnlockProduct Pro = new(
        Sku: "pro", PriceEur: 9.99m, Title: "Unlock Pro");

    public static readonly MintSubscriptionProduct ProMonthly = new(
        Sku: "pro-monthly", PriceEur: 14.99m, Period: SubscriptionPeriod.Monthly,
        Title: "Pro Plan");
}

// 2. Wire it in your app's Main()
private Mint? _mint;

public async Task Main()
{
    _mint = new Mint(app).RegisterProducts(typeof(Products));
    // ...
}

// 3. Use the four primitives
await _mint.ChargeAsync(Products.ImageGen, credits: 1);     // pay-per-use
await _mint.OfferUnlockAsync(Products.Pro);                  // one-time
await _mint.OfferSubscriptionAsync(Products.ProMonthly);     // recurring
await _mint.TipAsync(presets: [1m, 5m, 20m]);                // voluntary
```

That's the whole surface. No webhook handling, no Stripe SDK in your app, no manual fulfillment.

## What you do once per app

```bash
cd path/to/your/Ikon.App.Foo
ikon app mint enable
```

This issues a `MintApiKey` for the creator org, stores it as the `MINT_API_KEY` space secret, and binds the app's earnings to your org's payout pipeline. Restart the app afterwards.

You **never** type or copy the key value. If you need to rotate it, run the verb again with `--yes` to overwrite. To see issued keys: `ikon app mint list`. To revoke: `ikon app mint disable <id>`.

## Mental model

| Layer | What it owns |
|---|---|
| **Stripe** | Source of truth for all money — payment methods, subscriptions, invoices, refunds, payouts. |
| **Mint backend** | Projects Stripe state into per-customer entitlement rows; runs the platform-fee / creator-credit split on every charge; exposes a REST surface gated by `MINT_API_KEY`. |
| **`Ikon.Mint` library** | The C# wrapper your app sees. Builds checkout sessions, polls fulfillment, reads entitlements. Stateless — every call hits the backend. |
| **Your `Products.cs`** | Single source of truth for SKUs, prices, copy. Catalogue is reflected once at startup and synced to the backend. |

The library never persists anything itself. Wallet balances, unlock flags, subscription end dates — all live in the backend's `MintCustomerEntitlement` collection, projected from Stripe events. You read them via `GetEntitlementAsync`; you don't cache them.

## The four primitives

### 1. Wallet (pay-per-use)

```csharp
var result = await _mint.ChargeAsync(Products.ImageGen, credits: 1, reason: "generate cat photo");

if (result.Succeeded) {
    GenerateImage();
}
else if (result.DeclineReason == "cancelled") {
    Toast("Top-up cancelled");
}
```

Behaviour:
- If wallet has ≥ `credits`, deducts and returns `Succeeded=true`.
- If wallet is empty, opens Stripe Checkout for the **first** top-up bundle (`TopupBundles[0]`), polls until paid (or cancelled / timed out), then retries the charge automatically.
- `idempotencyKey:` lets you safely retry the same logical charge without double-deducting.

### 2. Unlock (one-time)

```csharp
if (await _mint.HasUnlockAsync(Products.Pro)) {
    ShowProFeatures();
} else {
    var result = await _mint.OfferUnlockAsync(Products.Pro);
    if (result.Granted) ShowProFeatures();
}
```

Once granted, the unlock never expires. `result.AlreadyHeld == true` if the customer already had it (no checkout opened, no charge).

### 3. Subscription (recurring)

```csharp
var result = await _mint.OfferSubscriptionAsync(Products.ProMonthly);

if (result.Active) {
    // result.CurrentPeriodEnd tells you when the next renewal hits
}
```

Stripe handles renewals automatically; the backend listens for `invoice.paid` (extends `SubscriptionEndsAt`, credits the creator), `customer.subscription.deleted` (deactivates at period end), and `customer.subscription.updated` with `past_due`/`unpaid` status (deactivates immediately to stop gating).

To let the customer cancel / change card / view invoices:

```csharp
await _mint.OpenCustomerPortalAsync();
```

Returns `false` if the customer hasn't completed any Mint checkout yet (no Stripe customer record exists).

### 4. Tip (voluntary)

```csharp
var result = await _mint.TipAsync(Products.Tip);
// or imperative:
var result = await _mint.TipAsync(presets: [1m, 5m, 20m], title: "Buy me a coffee");

if (result.Sent) Toast($"Thanks for the €{result.AmountEur}!");
```

Confers no entitlement. Used for attribution and creator earnings only.

## Reading state

### Rich entitlement snapshot

```csharp
MintEntitlement e = await _mint.GetEntitlementAsync(Products.Pro);

view.Text(e.UnlockGranted
    ? $"Pro since {e.UnlockGrantedAt:yyyy-MM-dd}"
    : "Free tier");

view.Text(e.SubscriptionActive
    ? $"Pro Plan active until {e.SubscriptionEndsAt:yyyy-MM-dd}"
    : "Not subscribed");

view.Text($"{e.CreditsRemaining} credits ({e.LifetimePurchased} lifetime)");
```

Includes `UnlockGrantedAt`, `SubscriptionEndsAt`, `LastPurchaseAt` — read these instead of just the boolean shortcuts when you want to render anything richer than yes/no.

### Customer's transactions (receipts panel)

```csharp
IReadOnlyList<MintTransactionInfo> mine = await _mint.MyTransactionsAsync(limit: 20);
foreach (var tx in mine) {
    view.Text($"{tx.CreatedAt:yyyy-MM-dd} {tx.Kind} {tx.Sku} — €{tx.GrossEur}");
}
```

Returns the customer's own transactions. The customer id is resolved server-side from the API key + reactive scope — never trust client input here.

### Creator's app revenue

```csharp
IReadOnlyList<MintCreatorTransactionInfo> all = await _mint.AppTransactionsAsync(
    limit: 100, since: DateTime.UtcNow.AddDays(-30));

decimal earned = all.Sum(t => t.CreatorEur);
```

Powered by the same `MINT_API_KEY` — possessing it implies creator-scoped permission. Includes the `PlatformFeeEur` / `CreatorEur` split and the customer's `EndUserId` so you can build per-customer dashboards.

### Creator's accumulated earnings

```csharp
Earnings e = await _mint.MyEarningsAsync();
view.Text($"Earned {e.Credits} credits (~€{e.EurEquivalent}) since {e.Since:yyyy-MM-dd}");
```

Earnings deposit as Ikon credits (`source = "creator-earnings"`) on the org balance. Spendable on platform usage immediately. Cashout to a bank is a separate, threshold-gated flow (see Cashout below).

## Policy attributes — gate without imperative checks

For the common case "this function requires Pro / charges 1 credit / requires a subscription", attribute the function instead of writing the `if (Has...)` plumbing:

```csharp
[Mint.RequireUnlock("pro")]
public async Task ExportHighRes() { /* ... */ }

[Mint.RequireSubscription("pro-monthly")]
public async Task UseAdvancedModels() { /* ... */ }

[Mint.Charge("image-generation", Credits = 1, Reason = "generate")]
public async Task GenerateImage() { /* ... */ }
```

Behaviour:
- The attribute reads price/copy from the registered catalog entry — no need to repeat it.
- If the customer doesn't hold the entitlement, Stripe Checkout opens, the function blocks until paid (or cancelled), then runs.
- For `[Mint.Charge]`, the credit is deducted before the function body runs.

The attribute resolves `Mint.Current` (set by the most recent `new Mint(app)` constructor). Construct Mint once in `Main()` before any function carrying these attributes can fire.

If you need an inline price (no catalog entry):

```csharp
[Mint.RequireUnlock("legacy-export", priceEur: 4.99)]
```

## Multi-currency

Each product declares one currency:

```csharp
public static readonly MintUnlockProduct ProUsd = new(
    Sku: "pro-us", PriceEur: 12.99m, Title: "Unlock Pro", Currency: "usd");
```

`PriceEur` is a legacy name — the value is interpreted in the declared `Currency`. Customers pay in that currency at Stripe Checkout. Internally the backend converts to EUR (via `CurrencyRateRepository`) for the platform-fee / creator-credit ledger so accounting stays single-currency.

If you want one product offered in multiple currencies, declare separate SKUs (`pro-us`, `pro-eu`, `pro-gb`) and pick at runtime based on the customer's locale or a stored preference.

## Subscription trials

```csharp
public static readonly MintSubscriptionProduct ProMonthly = new(
    Sku: "pro-monthly", PriceEur: 14.99m, Period: SubscriptionPeriod.Monthly,
    Title: "Pro Plan", TrialDays: 7);
```

On checkout completion the customer becomes `SubscriptionActive=true` immediately; Stripe charges only after the trial expires. `SubscriptionEndsAt` is set to the trial-end timestamp on Stripe's side, then advances on the first paid invoice.

## Promo codes / coupons

Create a coupon (admin-side, via the CLI or `MintAdminController`):

```bash
ikon app mint promotion create --code SUMMER --percent-off 25 --max-redemptions 100
```

Apply at checkout time:

```csharp
await _mint.OfferUnlockAsync(Products.Pro, promoCode: userEnteredCode);
await _mint.OfferSubscriptionAsync(Products.ProMonthly, promoCode: userEnteredCode);
```

The backend resolves the code, attaches the Stripe `promotion_code` to the checkout session, and lets Stripe enforce expiry / redemption limits / per-SKU restrictions. Invalid codes pass through transparently — Stripe shows an error on its checkout page; your app sees the regular `cancelled` outcome if the customer abandons.

## Refunds

Triggered by the creator (CLI or admin endpoint), not by the customer:

```bash
ikon app mint refund <transactionId> --reason "duplicate purchase"
```

Behaviour:
- Issues a Stripe refund (full or partial via `--amount`).
- Writes a new `kind=refund` transaction with negative `GrossEur` for clean audit.
- Full refund of an unlock revokes the entitlement (`UnlockGranted=false`).
- Deducts proportional creator credits via the existing `PurchasedCredits` ledger.
- Idempotent on the Stripe refund id — safe to retry.

Externally-issued refunds (Stripe dashboard) flow through `charge.refunded` and apply the same logic.

## Disputes / chargebacks

Stripe `charge.dispute.created` → backend marks the transaction `disputed`, deducts creator credits as a hold, revokes any granted unlock. On `charge.dispute.closed`:
- Won → restores credits, marks `complete`.
- Lost → hold becomes permanent.

You don't need to handle this in app code — the entitlement reads will reflect the new state on the next call.

## Cashout (Stripe Connect Express)

Earnings sit as Ikon credits on the creator org's balance. To pay them out to a bank account:

```bash
ikon app mint cashout setup       # opens Stripe Express onboarding link
ikon app mint cashout status      # KYC state, available balance, recent payouts
ikon app mint cashout request 250 # cash out €250 (after KYC verified)
```

Gates:
- KYC must be `verified` (Stripe sets this on `account.updated` webhook).
- Minimum payout threshold (€50 default, override via `MINT_CASHOUT_MIN_EUR` env).
- Available balance ≥ requested amount. Credits are deducted up-front; rolled back on Stripe transfer failure.

Funds typically arrive in the linked bank account in 1-2 business days.

## How fulfillment actually flows

```
Customer click → Mint.OfferUnlockAsync()
  ↓
  Library calls POST /mint/checkout (backend creates Stripe Checkout Session)
  ↓
  Library navigates browser to Stripe-hosted checkout
  ↓
  Customer pays
  ↓
  Stripe → backend webhook (checkout.session.completed, mintFlow=true)
  ↓
  MintCheckoutCompletedUseCase:
    - Updates MintCustomerEntitlement (UnlockGranted=true, etc.)
    - Splits charge: PlatformFee → platform org, Creator → PurchasedCredits ledger
    - Writes MintTransaction record
  ↓
  Library polls GET /mint/entitlement until success predicate matches
  ↓
  Returns UnlockResult to caller
```

The library polls; it doesn't subscribe to webhooks. Polling timeout default is 10 minutes (the lifetime of a Stripe Checkout session). On timeout you get `DeclineReason="timeout"` — the entitlement may still be granted later by the webhook; the next page load will see it.

## Idempotency

- **Wallet charges**: pass `idempotencyKey:` when retrying. Re-running with the same key returns the original outcome.
- **Webhook fulfillment**: the backend dedupes on Stripe's `checkout.session.id` / `payment_intent.id` / `charge.id`. Replaying a webhook never double-grants entitlements or double-credits the creator.
- **Refunds**: idempotent on the Stripe refund id.
- **Catalog sync**: re-running `RegisterProducts` upserts by `(space, sku)`; removed entries are marked `active=false` rather than deleted.

## Pricing model (what the customer pays vs what the creator gets)

Per charge:
- **Customer pays**: gross price in declared currency.
- **Backend converts** to EUR via `CurrencyRateRepository` (cached daily rates).
- **Platform fee**: percentage of gross EUR (configurable per-org default; see `PlatformFeeService`).
- **Creator earns**: gross EUR minus platform fee, deposited as Ikon credits.

Inspect any transaction with `AppTransactionsAsync` to see the per-row split (`GrossEur`, `PlatformFeeEur`, `CreatorEur`).

## Common pitfalls

- **Calling Mint outside a client scope.** `_mint.ChargeAsync(...)` resolves the customer from `ReactiveScope.UserId`. Calling from a non-scoped background `Task.Run` throws `InvalidOperationException`. Always call from inside an onClick handler / UI render / scoped function — the same place you'd read `ClientReactive<T>`.
- **Using boolean readers when you need detail.** `IsSubscribedAsync` returns a bool; `GetEntitlementAsync` gives you `SubscriptionEndsAt`, `LastPurchaseAt`, `UnlockGrantedAt` in one call. Prefer the snapshot — same backend round-trip, more info.
- **Manually setting `MINT_API_KEY`.** Don't. `ikon app mint enable` does both halves (issue + store). The plaintext is shown once at issuance and is unrecoverable; if you lost it, run `enable --yes` again to rotate.
- **Treating `Products.PriceEur` as EUR-specific.** It's named for back-compat but interpreted in `Currency`. A `MintUnlockProduct(Sku: "x", PriceEur: 9.99m, Currency: "usd")` charges $9.99.
- **Caching entitlement state.** Don't. The backend is the source of truth and webhook-driven changes (renewals, cancellations, refunds, disputes) won't reach your in-memory cache. Read on demand.
- **Calling `_mint.OpenCustomerPortalAsync()` before any purchase.** Returns `false` — the customer has no Stripe billing record yet. Gate the "Manage subscription" button on `SubscriptionActive` or render it conditionally.
- **Forgetting `RegisterProducts` before policy attributes fire.** Attributes resolve `Mint.Current` lazily; `Mint.Current` is set by the constructor; the catalog is loaded by `RegisterProducts`. Wire `new Mint(app).RegisterProducts(typeof(Products))` in `Main()` before any annotated function can be invoked.
- **Charging EUR + credits in the same low-level call.** `CustomChargeAsync` accepts both `AmountEur` and `Credits` for unusual monetization shapes. Most apps should use the four primitives instead — the custom path bypasses the auto-topup and entitlement logic.

## Working example

`platform-dotnet/Ikon.App.MintDemo` is the reference app — one of each primitive, the rich state row, the receipts panel, the customer portal button. Read `MintDemoApp.cs` and `Products.cs` for the canonical wire-up.

## CLI reference

| Verb | Purpose |
|---|---|
| `ikon app mint enable` | Issue + store the `MINT_API_KEY` secret. Run once per app. |
| `ikon app mint list` | Show issued keys (label, age, last-used, revoked). |
| `ikon app mint disable <id>` | Revoke a key. Other keys for the app keep working. |
| `ikon app mint cashout setup` | Begin Stripe Express onboarding for the creator org. |
| `ikon app mint cashout request <amount>` | Pay out the given EUR amount to the linked bank. |
| `ikon app mint cashout status` | KYC state, available balance, recent cashouts. |
