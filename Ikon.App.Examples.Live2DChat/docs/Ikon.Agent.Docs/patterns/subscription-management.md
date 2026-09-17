<!-- mined-from: Ikon.App.Patterns -->
# Subscription Management — Changing, Cancelling And Resuming
<!-- checked-against: 88f6c9dfda27c00a -->
Selling a subscription is `paywall-with-entitlement`; living with one is this. The provider is the
source of truth, so the app **re-reads** rather than caching a status of its own, and subscribes to
`PaymentEventReceived` so the screen stays honest when a renewal, a failure or a cancellation
happens outside the app entirely.

## When to use

Any account or billing screen behind a recurring offer: showing the current plan, switching plans,
cancelling, resuming, or reacting to a failed renewal.

## Notes

- **Change plan with `ChangeSubscriptionOfferAsync` — never cancel-then-resubscribe.** That loses
  the proration and leaves a gap in access. An **upgrade** charges the prorated difference now and
  grants the new entitlement immediately; a **downgrade** charges nothing, keeps the richer plan
  until the period ends, then bills the new price. `SubscriptionOfferChange.Changed` is `false`
  when it was already on that offer — a no-op, not an error.
- **Cancel is at period end by default.** The entitlement lapses when the cancellation takes
  effect, so the customer keeps what they paid for. Pass `immediate: true` to end it now.
- **Resume only works while `CancelAtPeriodEnd` is set and the paid period has not ended.** After
  that it needs a new checkout. `SubscriptionResume.SubscriptionId` can differ from the input when
  the provider recreated the subscription (Mollie).
- `PastDue` and `Unpaid` still read as "has a subscription". Say so plainly rather than rendering a
  row that looks active.
- **A refund does not revoke an entitlement.** Revoking is a separate decision the app makes.
- **`PaymentEventReceived` is a backend push, not a client action** — no user or client scope is
  active in the handler. A call that resolves the customer from scope (`ListSubscriptionsAsync()`
  with no key) throws there, and so does a `ClientReactive`/`UserReactive` `.Value`. Read the
  customer from `paymentEvent.Payload()` (`appCustomerKey`, the user id the checkout defaulted to),
  pass it explicitly, and write the keyed way (`UpdateFor` / `SetFor`).
- Missed a webhook, or the app was offline? `ReconcileAsync` pulls the objects and surfaces them as
  ordinary `PaymentEventReceived` pushes — eventually consistent, so do not await a state change.
- `[PaymentsRequireEntitlement(offerId)]` gates a registered function; on missing access it denies
  with the stable code `payments_entitlement_required`, which the UI catches to open a payment
  link. A call with no signed-in user denies with `payments_no_user`.

## Snippet

```csharp
// Per user, not per client: a subscription belongs to the customer, and the backend push that
// changes it names the customer rather than any client session.
private readonly UserReactiveList<PaymentSubscription> _subscriptions = new();
private readonly ClientReactive<string?> _notice = new(null);

/// <summary>
/// The provider is the source of truth, so the app re-reads rather than caching a status of
/// its own. Wiring PaymentEventReceived is what keeps the screen honest when a renewal, a
/// failure or a cancellation happens outside the app.
/// </summary>
private void WatchForChanges()
{
    PaymentsService.Instance.PaymentEventReceived += async paymentEvent =>
    {
        if (paymentEvent.Type is PaymentEventType.SubscriptionRenewed
            or PaymentEventType.SubscriptionCanceled
            or PaymentEventType.SubscriptionUpdated
            or PaymentEventType.SubscriptionRenewalFailed)
        {
            // The push comes from the backend, so no user or client scope is active here:
            // the customer key rides in the payload, and it is the user id the checkout
            // defaulted to.
            var payload = paymentEvent.Payload();

            if (payload.ValueKind == JsonValueKind.Object
                && payload.TryGetProperty("appCustomerKey", out var customerKey)
                && customerKey.GetString() is { } userId)
            {
                await RefreshAsync(userId);
            }
        }
    };
}

private async Task RefreshAsync(string userId)
{
    var subscriptions = await PaymentsService.Instance.ListSubscriptionsAsync(userId);
    _subscriptions.UpdateFor(userId, _ => subscriptions);
}

/// <summary>
/// Changing plan is ONE call -- never cancel-then-resubscribe, which loses the proration and
/// leaves a gap in access. An upgrade charges the difference now and grants immediately; a
/// downgrade charges nothing and keeps the richer plan until the period ends.
/// </summary>
private async Task ChangePlanAsync(string subscriptionId, string newOfferId)
{
    var change = await PaymentsService.Instance.ChangeSubscriptionOfferAsync(subscriptionId, newOfferId);

    // Changed is false when it was already on that offer -- not an error, just a no-op.
    _notice.Value = change.Changed
        ? $"{change.Direction}: {change.ProrationAmountMinor / 100.0:0.00} {change.Currency}"
        : "Already on that plan";

    await RefreshAsync(ReactiveScope.UserId);
}

private async Task CancelAsync(string subscriptionId)
{
    // Cancels at period end by default; the entitlement lapses only when it takes effect, so
    // the user keeps what they paid for.
    await PaymentsService.Instance.CancelSubscriptionAsync(subscriptionId);
    await RefreshAsync(ReactiveScope.UserId);
}

private async Task ResumeAsync(string subscriptionId)
{
    // Only valid while cancel-at-period-end and the paid period has not ended. After that it
    // needs a new checkout.
    await PaymentsService.Instance.ResumeSubscriptionAsync(subscriptionId);
    await RefreshAsync(ReactiveScope.UserId);
}

private void Render(IView view)
{
    view.Column(["gap-3"], content: col =>
    {
        foreach (var subscription in _subscriptions)
        {
            col.Card(["p-3 gap-2"], key: subscription.Id, content: card =>
            {
                card.Text(text: $"{subscription.OfferId} — {subscription.Status}");

                // PastDue and Unpaid still read as "has a subscription": say so plainly rather
                // than showing an active-looking row.
                if (subscription.Status is SubscriptionStatus.PastDue or SubscriptionStatus.Unpaid)
                {
                    card.Text(["text-destructive text-sm"], text: "Payment failed — update your card");
                }

                if (subscription.CancelAtPeriodEnd)
                {
                    card.Text(["text-muted-foreground text-sm"],
                        text: $"Ends {subscription.CurrentPeriodEnd:d}");
                    card.Button(onClick: async () => await ResumeAsync(subscription.Id),
                        content: v => v.Text(text: "Resume"));
                }
                else
                {
                    card.Button(onClick: async () => await CancelAsync(subscription.Id),
                        content: v => v.Text(text: "Cancel"));
                }
            });
        }

        if (_notice.Value is { } notice)
        {
            col.Text(["text-muted-foreground text-sm"], text: notice);
        }
    });
}
```

## See also

- `paywall-with-entitlement` — declaring the offer and selling it in the first place.
