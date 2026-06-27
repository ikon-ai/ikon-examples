# Ikon.App.Payments Guide

Charge your app's end users — subscriptions and one-off payments — without owning a payments backend.
The **Ikon backend** owns the payment store, drives the provider (Stripe or Mollie, chosen at enable
time), ingests provider webhooks, and **pushes normalized events to your app**. Your app sends commands
and reacts to events. There is no webhook to host and no payment state to persist.

> This is separate from **platform billing** (`ikon billing` / the Canvas AI-credit system). Payments is
> *your app's* merchant revenue. For the architecture, see `docs/private/ikon-payments.md`.

## Enable a provider (once per app)

```bash
ikon app payments enable --provider stripe      # or: --provider mollie
ikon app payments status                        # check onboarding / charges-enabled
```

`enable` provisions a connected merchant under Ikon's platform account and prints a hosted KYC link.
Default mode is **ikon-connect** (zero-config, Ikon-managed). `--default` picks the active provider when
an app has more than one enabled. BYOK (`--mode byok`) is admin-only.

## Wire it in your app

`app.Payments` is the entry point — no construction needed.

```csharp
// 1. Pick a default provider once at startup. Override per call if you enable more than one.
app.Payments.DefaultProvider = PaymentProvider.Stripe;

// 2. React to normalized events the backend pushes — no webhook to host.
app.Payments.PaymentEventReceived += async evt =>
{
    // evt.Type = PaymentPaid | PaymentRefunded | SubscriptionRenewed | SubscriptionCanceled
    // Idempotent on evt.EventId; evt.PayloadJson is the normalized projection.
    await MyApp.OnPaymentAsync(evt);
};

// 3. Create a payment link for an offer and redirect the user to it.
var link = await app.Payments.CreatePaymentLinkAsync(
    offerId: "pro", appCustomerKey: currentUserId, email: currentUserEmail);
// redirect the user to link.Url
```

That's the whole surface: send commands, react to `PaymentEventReceived`.

## The command surface (provider-neutral)

All commands go to the backend, which runs them on the app's provider and returns a typed result. They
are identical whether the provider is Stripe or Mollie. Every command takes an optional
`provider:` override; without it the service's default provider is used.

| Method | Does |
|---|---|
| `CreatePaymentLinkAsync(offerId, appCustomerKey, email?, …)` | A provider-hosted payment link for an offer — recurring offers start a subscription. Returns `PaymentLink` (`Url`, `Reference`, `Provider`). |
| `CreatePaymentLinkAsync(amountMinor, currency, appCustomerKey, …)` | A payment link for an ad-hoc amount (tips, one-off charges). |
| `RefundAsync(paymentId, amountMinor?, reason?)` | Full or partial refund. |
| `CancelSubscriptionAsync(subscriptionId, immediate?)` | Cancel now or at period end. |
| `GetEntitlementAsync(offerId, appCustomerKey)` | Is this customer entitled to an offer? |
| `ListSubscriptionsAsync(appCustomerKey)` | List the customer's subscriptions. |
| `ListPaymentsAsync(appCustomerKey)` | List the customer's payments. |
| `ListOffersAsync()` | The app's catalog of offers. |
| `ReconcileAsync()` | Force convergence after downtime (also runs periodically + on startup). |

`appCustomerKey` is whatever stable id identifies the paying entity in your app (user id, org id, tenant
id) — the backend maps it to the provider's customer.

## Offers

Offers are an **Ikon-level catalog** (`offerId` → amount / currency / interval), not provider catalog
objects. Stripe maps each offer to a price; Mollie uses the inline amount. Read them with
`ListOffersAsync()`; surface them with the Parallax `PricingTable` component.

## Receiving events

Your app does **not** host a webhook. The backend normalizes every provider webhook and pushes it as a
`PaymentEventReceived` event (delivered over the existing protocol-message channel). Handle the normalized
types and dedupe on `EventId`:

```csharp
app.Payments.PaymentEventReceived += evt => evt.Type switch
{
    PaymentEventType.PaymentPaid           => FulfilAsync(evt),   // captured (sync or async) — safe to ship
    PaymentEventType.SubscriptionRenewed   => ExtendAsync(evt),
    PaymentEventType.SubscriptionCanceled  => RevokeAsync(evt),
    PaymentEventType.PaymentRefunded       => RefundAsync(evt),
    _ => Task.CompletedTask,
};
```

Webhooks are not the source of truth: if a delivery is missed, a backend **reconciliation** sweep
re-pulls provider state and converges the store, and your app re-reads entitlement on startup.

## Gating features

Use the policy attributes (backed by the backend entitlement ledger):

```csharp
[PaymentsRequireSubscription("pro")]   // deny code: payments_subscription_required
[PaymentsRequireUnlock("report-42")]   // deny code: payments_unlock_required
```

Your UI catches the deny code and opens a payment link; the next `PaymentEventReceived` flips the entitlement.

## Providers

| | Stripe | Mollie |
|---|---|---|
| Reach | Global | EU-centric |
| Onboarding | hosted KYC (v2 Connect accounts) | hosted Client Links + OAuth |
| Webhooks | signed, provider-retried | thin/unsigned → backend re-fetches |

The app code and the methods above are identical for both. MobilePay/Vipps, iDEAL, Bancontact, etc. are
available as ordinary payment methods inside whichever provider you enable — no extra integration.

## Modes

- **ikon-connect** (default): the app onboards as a connected merchant under Ikon's platform account;
  Ikon takes a configurable application fee. Zero setup.
- **byok** (admin-only): the app uses its own provider account; no Ikon fee. `ikon app payments enable
  --mode byok --provider stripe|mollie` stores the app's own key as a secret.

## Removing a provider

```bash
ikon app payments disable                    # remove every provider from the app
ikon app payments disable --provider mollie  # remove just one
```
