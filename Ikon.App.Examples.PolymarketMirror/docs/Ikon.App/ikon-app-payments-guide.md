# Ikon.App.Payments Guide

Charge your app's end users — subscriptions, one-off payments, refunds — without owning a payments
backend. The **Ikon backend** owns the payment store, drives the provider (Stripe, Mollie, or Surfboard,
chosen at enable time), ingests provider webhooks, and **pushes normalized events to your app**. Your app
sends commands and reacts to events: there is no webhook to host and no payment state to persist.

> This is *your app's* merchant revenue. It is separate from **platform billing** (`ikon billing` / the
> Canvas AI-credit system that funds the platform) — different system, different money.

## Enable a provider (once per app)

```bash
ikon app payments enable --provider stripe      # or: --provider mollie | --provider surfboard
ikon app payments status                        # check onboarding / charges-enabled
```

`enable` provisions a connected merchant under Ikon's platform account and prints a hosted onboarding link
(Stripe KYC, a Mollie OAuth grant, or a Surfboard KYB form). Open it to finish onboarding. Default mode is **ikon-connect**
(zero-config, Ikon-managed); `--default` picks the active provider when an app has more than one enabled;
BYOK (`--mode byok`) is admin-only. There is no separate "enabled" flag — payments is on once a provider
is configured.

## Wire it into your app

`app.Payments` is the entry point — no construction needed.

```csharp
// 1. Pick a default provider once at startup. Override per call if you enable more than one.
app.Payments.DefaultProvider = PaymentProvider.Stripe;
app.Payments.DefaultSuccessUrl = "https://<your-app>/paid";
app.Payments.DefaultCancelUrl  = "https://<your-app>/cancel";

// 2. React to normalized events the backend pushes — no webhook to host.
app.Payments.PaymentEventReceived += async evt =>
{
    // evt.Type = PaymentPaid | PaymentRefunded | SubscriptionRenewed | SubscriptionCanceled
    // Deduped on evt.EventId; evt.Payload() is the normalized projection.
    await OnPaymentAsync(evt);
};

// 3. Take a payment, then redirect the user to the returned Url.
var link = await app.Payments.CreatePaymentLinkAsync(
    offerId: "pro", appCustomerKey: currentUserId, email: currentUserEmail);
await ClientFunctions.OpenExternalUrlAsync(link.Url);
```

That's the whole loop: send a command, redirect to the link, react to `PaymentEventReceived`.

## The command surface (provider-neutral)

All commands go to the backend, which runs them on the app's provider and returns a typed result —
identical whether the provider is Stripe or Mollie. Every command takes an optional `provider:` override;
without it the service's `DefaultProvider` is used.

| Method | Does |
|---|---|
| `CreatePaymentLinkAsync(offerId, appCustomerKey, email?, …)` | A provider-hosted payment link for an offer — a recurring offer starts a subscription, a one-time offer is a single charge. Returns `PaymentLink` (`Url`, `Reference`, `Provider`). |
| `CreatePaymentLinkAsync(amountMinor, currency, appCustomerKey, …)` | A payment link for an ad-hoc amount (tips, one-off charges). |
| `RefundAsync(paymentId, amountMinor?, reason?)` | Full or partial refund → `PaymentRefund`. |
| `CancelSubscriptionAsync(subscriptionId, immediate?)` | Cancel now (`immediate: true`) or at period end (default). |
| `GetEntitlementAsync(offerId, appCustomerKey)` | Does this customer have an active subscription to an offer? → `PaymentEntitlement`. |
| `ListSubscriptionsAsync(appCustomerKey)` | The customer's subscriptions. |
| `ListPaymentsAsync(appCustomerKey)` | The customer's payments. |
| `ListOffersAsync()` | The app's catalog of offers. |

`appCustomerKey` is whatever stable id identifies the paying entity in your app (user id, org id, tenant
id) — the backend maps it to the provider's customer.

## Offers

Offers are an **Ikon-level catalog** (`offerId` → amount / currency / interval), not provider catalog
objects. They are **provisioned at the provider** and synced in — there is no programmatic offer-creation
API. Discover them with `ListOffersAsync()` (each `PaymentOffer` carries `Prices`, where a `recurring`
price means subscribing and `one_time` means a single charge), and surface them with the Parallax
`PricingTable` component.

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

Webhooks are not the source of truth: if a delivery is ever missed, the backend reconciles provider state
automatically, and your app re-reads entitlement on startup — so you can treat events as best-effort
nudges and `GetEntitlementAsync` as the authority.

## Gating features

Gate a server `[Function]` on an active subscription declaratively:

```csharp
[PaymentsRequireSubscription("pro")]   // deny code: payments_subscription_required
```

The call is denied unless the caller holds an active subscription for the offer (resolved from the
caller's id). Your UI catches the deny code and opens a payment link; the next `PaymentEventReceived` flips
the entitlement and the user retries.

## Providers

| | Stripe | Mollie | Surfboard |
|---|---|---|---|
| Reach | Global | EU-centric | Nordics (SE/DK/FI/NO) |
| Onboarding | hosted KYC (v2 Connect accounts) | hosted Client Links + OAuth | hosted KYB (partner merchants) |
| Subscriptions | native | native | backend-orchestrated (see below) |

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
