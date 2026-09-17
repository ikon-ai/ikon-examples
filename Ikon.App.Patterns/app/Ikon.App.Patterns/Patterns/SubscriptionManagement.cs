namespace Ikon.App.Patterns.Patterns;

// Pattern: subscription-management — see docs/patterns/subscription-management.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class SubscriptionManagement : IPatternDemo
{
    public string Slug => "subscription-management";
    public string Title => "Managing an existing subscription";
    public string Category => "Platform mechanics";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-subscription-management
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
    #endregion
}
