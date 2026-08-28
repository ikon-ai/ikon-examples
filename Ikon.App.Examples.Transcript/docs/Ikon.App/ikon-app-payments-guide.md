# Ikon.App.Payments Guide

Charge your app's end users — subscriptions, one-off payments, refunds — without owning a payments
backend. The **Ikon backend** owns the payment store, drives the provider (Stripe, Mollie, or Surfboard,
chosen at enable time), ingests provider webhooks, and **pushes normalized events to your app**. Your app
sends commands and reacts to events: there is no webhook to host and no payment state to persist.

> This is *your app's* merchant revenue. It is separate from **platform billing** (`ikon billing` / the
> Canvas AI-credit system that funds the platform) — different system, different money.

## Enable a provider (once per app)

```bash
ikon app payments enable --provider stripe      # Stripe is the generally-available provider
ikon app payments status                        # check onboarding / charges-enabled
```

`enable` provisions a connected merchant under Ikon's platform account and prints a hosted onboarding link
(Stripe KYC, a Mollie OAuth grant, or a Surfboard KYB form). Open it to finish onboarding. Default mode is **ikon-connect**
(zero-config, Ikon-managed); `--default` picks the active provider when an app has more than one enabled;
BYOK (`--mode byok`) is admin-only. There is no separate "enabled" flag — payments is on once a provider
is configured.

Onboarding links are **single-use and short-lived, and always use the newest one** — requesting a new link
can invalidate older ones. If a link has gone stale (it bounces to an explanatory page instead of the
provider's form), get a fresh one with `ikon app payments status`, or just re-run
`ikon app payments enable` — while onboarding is unfinished it prints a fresh link instead of demanding
`--force`.

> **Mollie and Surfboard are currently admin-only** (in preview) — regular apps enable **Stripe**. Your app
> code is provider-neutral either way, so nothing changes when they become generally available.

## Wire it into your app

`app.Payments` (a `PaymentsService`) is the entry point — no construction needed.

```csharp
// 1. No provider setup needed — commands charge with the provider you enabled for the app.
//    Pin a default ONLY if you enabled more than one and want to skip passing provider: each call:
//    app.Payments.DefaultProvider = PaymentProvider.Stripe;   // optional; unset = the space's provider
// Success/cancel redirects default to your app's own URL — the user returns to the app after paying.
// Set DefaultSuccessUrl / DefaultCancelUrl only for a custom destination, e.g. app-URL + "/paid" and
// "/cancel" routes where you render a confirmation (branch on the client's InitialPath in UI.Root —
// strip the query first: InitialPath carries the deep link's query string, and payment providers
// may append their own params to the return URL: var path = InitialPath.Split('?', 2)[0];).

// 2. React to normalized events the backend pushes — no webhook to host.
app.Payments.PaymentEventReceived += async evt =>
{
    // evt.Type = PaymentPaid | PaymentRefunded | SubscriptionActivated | SubscriptionUpdated |
    //   SubscriptionRenewed | SubscriptionRenewalFailed | SubscriptionCanceled | … (null if unknown)
    // Deduped on evt.EventId; evt.Payload() is the normalized projection.
    await OnPaymentAsync(evt);
};

// 3. Take a payment, then redirect the user to the returned Url.
// customerKey defaults to the current user, so in a UI/event handler you just pass the offer id.
var link = await app.Payments.CreatePaymentLinkAsync(offerId: "pro");
await ClientFunctions.OpenExternalUrlAsync(link.Url);
```

That's the whole loop: send a command, redirect to the link, react to `PaymentEventReceived`.

## The command surface (provider-neutral)

All commands go to the backend, which runs them on the app's provider and returns a typed result —
identical whether the provider is Stripe or Mollie. Every command takes an optional `provider:` override;
without it the backend charges with the provider you enabled for the app (or the `DefaultProvider` you
pinned when more than one is enabled).

| Method | Does |
|---|---|
| `CreateOfferAsync(offer, provider?)` | Create (or update) an offer customers can pay for by id → `PaymentOffer`. |
| `RemoveOfferAsync(offerId, provider?)` | Remove an offer from the catalog → `bool` (`false` if no such offer existed). |
| `CreatePaymentLinkAsync(offerId, customerKey?, email?, …, amountMinorOverride?)` | A provider-hosted payment link for an offer — a recurring offer starts a subscription, a one-time offer is a single charge. `amountMinorOverride` charges a custom amount instead of the catalog price (one-time offers only — see Upgrades below). Returns `PaymentLink` (`Url`, `Reference`, `Provider`). |
| `CreatePaymentLinkAsync(amountMinor, currency, customerKey?, …)` | A payment link for an ad-hoc amount (tips, one-off charges). Grants no entitlement — use an offer for that. |
| `RefundAsync(paymentId, amountMinor?, reason?)` | Full or partial refund → `PaymentRefund`. |
| `RequestReceiptAsync(paymentId, provider?)` | Fetch a receipt for a completed payment → `PaymentReceipt` (`Url` hosted receipt page; `Pdf` bytes when the provider offers a downloadable PDF, else `null`). |
| `CancelSubscriptionAsync(subscriptionId, immediate?)` | Cancel now (`immediate: true`) or at period end (default). |
| `ChangeSubscriptionOfferAsync(subscriptionId, newOfferId, immediateChargeMinor?)` | Switch a subscription to another recurring offer with proration → `SubscriptionOfferChange`, whose `Direction` is a `PlanChangeDirection` saying whether it was an upgrade or a downgrade (see Upgrades below). |
| `ResumeSubscriptionAsync(subscriptionId)` | Re-enable a subscription canceled at period end but not yet lapsed → `SubscriptionResume`. No charge; billing resumes on the original date. |
| `ReconcileAsync(customerKey?, reference?)` | Re-pull live provider state (missed-webhook recovery) → `PaymentReconcileResult`. Eventually consistent — results arrive as normal payment events (see below). |
| `IsEntitled(offerId, customerKey?)` | **Synchronous, no backend call** — the fast UI gate (see below). |
| `GetEntitlementAsync(offerId, customerKey?)` | Does this customer have access to an offer? → `PaymentEntitlement` (`Active`, `ExpiresAt`, `Source`). Access past its `ExpiresAt` reports inactive. A backend call — for gating UI prefer `IsEntitled`. |
| `ListSubscriptionsAsync(customerKey?)` | The customer's subscriptions, each a `PaymentSubscription`. |
| `ListPaymentsAsync(customerKey?)` | The customer's payments. |
| `ListOffersAsync()` | The app's catalog of offers. |

### `customerKey` — who is paying

`customerKey` is your app's own stable id for the paying entity. **In almost every app that's your user
id** — pass `app.CurrentUserId` (or your `SessionIdentity.UserId`). It only differs when you bill a
non-user entity (an organization or tenant id for team plans). The backend maps it to the provider's
own customer record; the same key ties together a customer's offers, subscriptions, payments, and
entitlements.

**You can usually omit it.** Every method that takes `customerKey` makes it optional and defaults to the
**current user** whenever a client is in scope — a UI render, an `onClick`/`onSubmit`, or an
`OnClientJoined` handler — so `CreatePaymentLinkAsync("pro")`, `GetEntitlementAsync("pro")`, and
`ListSubscriptionsAsync()` all "just work" for the logged-in user. **Pass it explicitly** for another
user/org, or when there is no current user in scope: a **background task**, or — importantly — the
**`PaymentEventReceived` handler** (a server-side push, not tied to any client). Omitting it there throws a
clear error telling you to supply it; the event's `Payload()` carries the customer it concerns.

### Anonymous (guest) users — refused by default

A guest (a user who hasn't signed in) still has a valid user id, but it is **device-scoped**: signing in
later gives them a **different** user id, so a payment taken under the guest id — and any entitlement it
granted — would not follow them. `CreatePaymentLinkAsync` therefore **throws** when the paying customer
is a connected guest. Either require sign-in before taking the payment, or opt in explicitly:

```csharp
app.Payments.AllowAnonymousPayments = true;   // accept guest payments (e.g. anonymous tips)
```

Opt in only for purchases the app can afford to lose track of — say an ad-hoc tip that grants no
entitlement. The guard fires only when it can see the payer: an explicit `customerKey` that matches no
connected client (an offline user, an org/tenant id) passes through.

Statuses and kinds are typed enums (`PriceKind`,
`SubscriptionStatus`, `PaymentStatus`, `PaymentKind`, `RefundStatus`, `EntitlementSource`), each with an
`Unknown` fallback.

## Offers

An offer is an **Ikon-level catalog entry** (`offerId` → a price) that customers pay for by id. Create one
from code or the CLI — no provider dashboard required — and it works the same across Stripe, Mollie, and
Surfboard:

```csharp
await app.Payments.CreateOfferAsync(new OfferSpec("pro", "Pro",
    new OfferPriceSpec(AmountMinor: 999, Currency: "eur", Kind: PriceKind.Recurring, Interval: PriceInterval.Month)));
// one-time offer: new OfferPriceSpec(500, "eur", PriceKind.OneTime)
```

or

```
ikon app payments offer create --id pro --name Pro --amount 999 --currency eur --interval month
ikon app payments offer list
ikon app payments offer delete --id pro
```

For Stripe this provisions a Product + Price (`lookup_key = offerId`); for providers without a catalog
(Mollie, Surfboard) the platform stores the offer definition. Either way you reference the offer by its
`offerId` (e.g. `[PaymentsRequireEntitlement("pro")]` — the `PaymentsRequireEntitlementAttribute`).

Discover offers with `ListOffersAsync()` — each `PaymentOffer` carries `Prices` (a `PaymentPrice` per currency and interval; `PriceKind.Recurring` →
subscription, `PriceKind.OneTime` → single charge) — and render your own pricing UI from them, calling
`CreatePaymentLinkAsync(offerId)` when the user picks a plan.

> Already have Products/Prices in your Stripe dashboard? Those still sync into the catalog automatically —
> set a Price **lookup key** (or product `metadata.app_plan_id`) to control the `offerId`; otherwise the
> offer syncs under its Stripe product id.

## Upgrades, downgrades & resubscribe

### One-time offers — a custom-priced upgrade

For **one-time** (permanent-unlock) offers, charge a developer-computed amount while still granting the
offer's entitlement by passing `amountMinorOverride` to the offer payment link. The classic case is
"upgrade from `level1` to `level2`, crediting what was already paid":

```csharp
// The customer already bought level1; charge only the difference for level2.
var payments = await app.Payments.ListPaymentsAsync();
var credit = payments
    .Where(p => p.OfferId == "level1" && p.Status == PaymentStatus.Paid)
    .Sum(p => p.AmountMinor - p.AmountRefundedMinor);

var link = await app.Payments.CreatePaymentLinkAsync("level2", amountMinorOverride: level2PriceMinor - credit);
await ClientFunctions.OpenExternalUrlAsync(link.Url);
```

Paying grants the `level2` entitlement exactly as at full price — the amount charged never affects the
grant. The platform fee and the recorded payment follow the overridden amount. **One-time offers only**:
supplying `amountMinorOverride` for a recurring offer is rejected (subscriptions use
`ChangeSubscriptionOfferAsync`). After the upgrade the customer holds both `level1` and `level2`; gate your
premium features on `level2` and hide the buy button with `IsEntitled` as needed.

### Subscriptions — change plan with proration

Switch an active subscription to another **recurring** offer (same currency and interval) with
`ChangeSubscriptionOfferAsync`:

```csharp
var change = await app.Payments.ChangeSubscriptionOfferAsync(subscriptionId, "level2");
```

- **Upgrade** (pricier offer): the **prorated difference** — the price gap scaled to the time left in the
  current period — is charged immediately, the new plan takes effect now, and renewals continue on the
  **existing renewal date** at the full new price.
- **Downgrade** (cheaper/equal offer): **no charge, no credit**. The current (higher) plan stays available
  until the next renewal, when the new plan takes over and renewals bill the lower price.

The result's `Changed` is `false` when the subscription was already on that offer; `Direction`,
`ProrationAmountMinor`, `ProratedChargeRef`, and `Effective` (`"immediate"` for an upgrade, `"next_cycle"`
for a downgrade) describe what happened. The previous offer's entitlement is left to lapse at its stored
expiry — so on a downgrade the higher plan remains usable until the period ends, and on an upgrade the old
plan lingering alongside the new one is harmless.

The platform computes the proration. To own the pricing yourself (Mollie/Surfboard), pass
`immediateChargeMinor` to set the exact upgrade charge; it is rejected for Stripe, which prorates natively.

### Resubscribe (un-cancel)

A subscription canceled at period end but whose paid period hasn't lapsed can be re-enabled — no charge,
billing resumes on the original renewal date:

```csharp
var resume = await app.Payments.ResumeSubscriptionAsync(subscriptionId);
// resume.SubscriptionId may differ from the input when the provider recreated the subscription (Mollie).
```

An immediately-canceled or fully-ended subscription can't be resumed — start a new checkout instead.

## Promotion codes

Let customers apply a discount code on the checkout page by passing `allowPromotionCodes: true` when
creating a payment link — works for one-time and subscription offers, and for ad-hoc charges:

```csharp
var link = await app.Payments.CreatePaymentLinkAsync("pro", allowPromotionCodes: true);
```

The hosted checkout then shows an "Add promotion code" field. The codes themselves (and the coupons
behind them) are created and managed in your provider dashboard, not through Ikon — for Stripe under
**Product catalog → Coupons**, where each coupon can carry customer-facing promotion codes like `SALE20`.

**Stripe only.** Mollie and Surfboard have no promotion-code concept on their hosted checkouts; they
ignore the flag and the checkout proceeds at full price.

## Receiving events

Your app does **not** host a webhook. The backend normalizes every provider webhook and pushes it as a
`PaymentEventReceived` event over the existing protocol-message channel. Handle the normalized types;
delivery is deduped on `EventId`:

```csharp
app.Payments.PaymentEventReceived += evt => evt.Type switch
{
    PaymentEventType.PaymentPaid           => FulfilAsync(evt),
    PaymentEventType.SubscriptionRenewed   => ExtendAsync(evt),
    PaymentEventType.SubscriptionCanceled  => RevokeAsync(evt),
    PaymentEventType.PaymentRefunded       => OnRefundAsync(evt),
    _ => Task.CompletedTask,
};
```

Webhooks are not the source of truth — three recovery paths keep the backend's payment store correct when
a delivery is missed or the app is offline when an event is pushed:

1. **Checkout return** — a Stripe payer's success redirect hops through the backend, which verifies the
   session and re-ingests it before forwarding the payer to your app. The common "user paid and came back
   but the webhook got lost" case heals itself with no code on your side.
2. **Periodic sweep** — the backend re-pulls subscriptions whose stored period end has passed without a
   renewal or cancellation event landing.
3. **`app.Payments.ReconcileAsync(customerKey?, reference?)`** — on-demand re-pull for anything else. Pass
   a `PaymentLink.Reference` (checkout session) or a subscription id to pull one object, a `customerKey`
   for that customer's recent objects, or nothing (outside a client scope) for the space's recent window.
   It is eventually consistent: the pulled objects flow through the normal pipeline and surface as ordinary
   `PaymentEventReceived` pushes and entitlement refreshes within seconds — the return value only reports
   how many objects were queued.

Because a reconciled copy and a late-arriving webhook copy of the same business event carry different
`EventId`s, write event handlers to be idempotent (fulfilling the same payment twice must be harmless).
The stored state converges either way, so treat events as best-effort nudges and `GetEntitlementAsync`
as the authority.

## Gating features

Gate a server `[Function]` on an active entitlement declaratively:

```csharp
[PaymentsRequireEntitlement("pro")]   // deny code: payments_entitlement_required
```

The call is denied unless the caller holds an active entitlement for the offer (resolved from the caller's
id) — access granted by an active subscription **or** a one-time purchase of that offer. Your UI catches the
deny code and opens a payment link; the next `PaymentEventReceived` flips the entitlement and the user
retries. `GetEntitlementAsync(offerId).Source` tells you whether the access came from a `Subscription` or a
`OneTime` purchase.

Subscription access is period-bound: each renewal refreshes `ExpiresAt` (the period end plus a grace
window), and an entitlement past its `ExpiresAt` counts as inactive even if the final cancellation webhook
never arrived. A **one-time purchase never expires** — it's a permanent unlock for that offer, with no
`ExpiresAt`. Note that refunding a one-time payment does not revoke the entitlement it granted.

Nothing on the platform stops a customer from paying for an offer they already hold — a re-purchase is a
second charge (and, for a recurring offer, a second subscription). If re-buying shouldn't be allowed, gate
your Buy button on `IsEntitled(offerId)` and hide or disable it when the customer is already entitled.

### Gating the UI — `IsEntitled` (synchronous)

Inside a UI render you can't `await`, and you must not make a backend call every frame. Use
`app.Payments.IsEntitled(offerId)` — a **synchronous, cached, no-backend-call** check safe to read every
render:

```csharp
if (app.Payments.IsEntitled("pro"))
{
    view.Text([Text.Body], "✨ Pro feature");
}
```

Reading it inside a UI lambda registers a reactive dependency, so the subtree **re-renders automatically**
when the entitlement changes — the moment a purchase's event lands, the gated content appears with no manual
refresh. The first read for an offer the app hasn't seen returns `false` and warms the cache in the
background, flipping to the real value on the next render. `customerKey` defaults to the current user, as
everywhere else.

## Receipts

Hand a customer a receipt for a completed payment with `RequestReceiptAsync(paymentId)` — the same
`paymentId` you'd refund with:

```csharp
var receipt = await app.Payments.RequestReceiptAsync(paymentId);
if (!string.IsNullOrEmpty(receipt.Url))
{
    await ClientFunctions.OpenExternalUrlAsync(receipt.Url);   // hosted receipt page
}
else if (receipt.Pdf is { Length: > 0 } pdf)
{
    // The provider returned downloadable bytes — offer them with a DownloadFile action.
    view.ActionButton([Button.OutlineSm], action: ActionKind.DownloadFile,
        options: new DownloadFileActionOptions { Filename = "receipt.pdf", Data = pdf },
        content: v => v.Text([Text.BodySm], "Download receipt"));
}
```

`PaymentReceipt.Url` is a provider-hosted receipt page (Stripe and Surfboard both return one); `Pdf` carries
downloadable PDF bytes only when the provider exposes one (today a hosted URL is the norm, so `Pdf` is
usually `null`). Return shape is uniform across providers; a provider with no customer-facing receipt at all
(Mollie) returns both fields `null` rather than failing — check for that before showing a receipt button.

## Providers

| | Stripe | Mollie | Surfboard |
|---|---|---|---|
| Reach | Global | EU-centric | Nordics (SE/DK/FI/NO) |
| Onboarding | hosted KYC (v2 Connect accounts) | hosted Client Links + OAuth | hosted KYB (partner merchants) |
| Subscriptions | native | native | backend-orchestrated (see below) |
| Promotion codes | native checkout field | — | — |

The app code and the methods above are identical for all three. MobilePay/Vipps, iDEAL, Bancontact, etc.
are available as ordinary payment methods inside whichever provider you enable — no extra integration.

Surfboard is a Nordic acquirer; enable it only for apps whose merchants are in SE/DK/FI/NO. It has no
native subscription objects (it uses token-based merchant-initiated charges), so the **backend** owns the
recurring schedule for Surfboard and bills each cycle itself. This is invisible to your app: you still
create a recurring offer link and react to `SubscriptionRenewed` / `SubscriptionCanceled` exactly as with
Stripe or Mollie.

## Modes

- **ikon-connect** (default): the app onboards as a connected merchant under Ikon's platform account;
  Ikon takes a platform fee (see below). Zero setup.
- **byok** (admin-only): the app uses its own provider account; no Ikon fee. `ikon app payments enable
  --mode byok --provider stripe|mollie|surfboard` stores the app's own key as a secret.

## Platform fee

On the **ikon-connect** path Ikon takes a percentage cut of each payment (default **10%**), set per space
by Ikon staff. byok takes no fee (the funds are in your own account). You do not set or see the fee from
app code — the backend applies it via the provider's native split primitive (Stripe application fees,
Mollie application fee, Surfboard Flow service-provider split), so the cut settles to Ikon automatically.

## Removing a provider

```bash
ikon app payments disable                    # remove every provider from the app
ikon app payments disable --provider mollie  # remove just one
```

## How it works (the mental model)

You only ever send commands and react to events — the backend does the rest:

```
 Your app (C#)            Ikon backend                     Provider
 ─────────────            ────────────                     ────────
 CreatePaymentLinkAsync ───►  command  ──────────────────►  Stripe / Mollie
 PaymentEventReceived  ◄───  normalized event  ◄───────────  webhook
```

The backend holds the payment store, drives the provider, and turns each provider webhook into a single
normalized `PaymentEvent` it pushes to your app. The providers behave differently underneath (signed vs.
thin webhooks, KYC vs. OAuth vs. KYB onboarding, native vs. backend-orchestrated subscriptions), but your
app code is identical for all of them — that's the point of the surface above.
