<!-- mined-from: Ikon.App.Patterns -->
# Paywall With Entitlement — Offer, Link, Access

Three steps, and the platform owns the middle one. Declare an **offer** once at startup, hand the
user a **payment link** for it when they choose to buy, and gate the feature on the **entitlement**
that paying grants. The app never sees a card number and never decides whether a payment
succeeded — it asks whether the customer is entitled.

## When to use

Any paid feature: a subscription, a one-time unlock, a pro tier, credits. Also the answer to "how
do I check whether this user has paid" — that is `IsEntitled`, not a flag the app stores.

## Notes

- **Prices are MINOR units.** `AmountMinor: 900` is €9.00. Writing `9` charges nine cents.
- `CreateOfferAsync` is idempotent on `OfferId`, so declaring offers every boot updates rather than
  duplicates them.
- **Two `CreatePaymentLinkAsync` overloads, and only one grants access.** The `offerId` overload
  grants the offer's entitlement (and starts a subscription for a recurring offer). The
  `amountMinor` overload charges an ad-hoc amount and grants **nothing** — right for a tip, wrong
  for an unlock.
- `IsEntitled` is synchronous, makes no backend call, and re-renders when the entitlement changes —
  use it to gate UI. `GetEntitlementAsync` makes a call and is for a one-off check, not per frame.
  The first `IsEntitled` read for an unseen offer returns false and warms the cache in the
  background, so a freshly-entitled user may need one more render.
- Anonymous customers are rejected by default: a guest's device-scoped id changes when they sign
  in, which would orphan the payment and its entitlement. `AllowAnonymousPayments` opts in, and
  only suits purchases that may stay behind.
- **A refund does not revoke an entitlement.** Revoking is a separate decision the app makes.
- Change a plan with `ChangeSubscriptionOfferAsync`, not by cancelling and re-subscribing —
  it prorates and moves the entitlement.
- Missed a provider webhook, or the app was offline? `ReconcileAsync` pulls the objects and
  surfaces them as ordinary `PaymentEventReceived` pushes.
- `[PaymentsRequireEntitlement]` gates a registered function the same way this gates a view.

A charge is consequential: a person presses something that names the action. Never as a side
effect of an AI turn, and never from rendering.

## Snippet

```csharp
private const string ProOfferId = "pro-monthly";

private readonly ClientReactive<string?> _checkoutUrl = new(null);

/// <summary>
/// Offers are declared once at startup. CreateOfferAsync is idempotent on OfferId, so calling
/// it every boot updates the offer rather than creating duplicates.
/// </summary>
private static async Task DeclareOffersAsync()
{
    await PaymentsService.Instance.CreateOfferAsync(new OfferSpec(
        OfferId: ProOfferId,
        Name: "Pro",
        Price: new OfferPriceSpec(
            AmountMinor: 900,               // 9.00 -- MINOR units, never 9.0
            Currency: "EUR",
            Kind: PriceKind.Recurring,
            Interval: PriceInterval.Month)));
}

private async Task StartCheckoutAsync()
{
    // Paying an OFFER grants the entitlement; the ad-hoc amount overload charges money and
    // grants nothing, which is the wrong one for unlocking access.
    var link = await PaymentsService.Instance.CreatePaymentLinkAsync(ProOfferId);
    _checkoutUrl.Value = link.Url;
}

private void Render(IView view)
{
    // IsEntitled is synchronous and makes no backend call, so it is safe to read every render
    // and re-renders when the entitlement changes. GetEntitlementAsync is the awaited form --
    // right for a one-off check, wrong for gating UI on every frame.
    if (PaymentsService.Instance.IsEntitled(ProOfferId))
    {
        RenderProContent(view);
        return;
    }

    view.Column(["gap-3"], content: col =>
    {
        col.Text([Text.H2], text: "Pro");
        col.Text(["text-muted-foreground"], text: "€9 per month");

        // A person presses the button that starts a charge -- never an AI turn, and never a
        // side effect of rendering.
        col.Button([Button.PrimaryMd], onClick: StartCheckoutAsync, content: v => v.Text(text: "Upgrade"));

        // Checkout happens on the provider's page; the app hands the user the link.
        if (_checkoutUrl.Value is { } url)
        {
            col.Link(["underline"], text: "Continue to checkout", href: url);
        }
    });
}
```

## See also

- `destructive-confirm-dialog` — the confirmation shape for other consequential actions.
