namespace Ikon.App.Patterns.Patterns;

// Pattern: subscription-management — see docs/patterns/subscription-management.md.
// The example region below is the canonical body the doc extracts. The gallery never runs Main, so
// nothing reads real subscriptions: the demo loads sample ones covering each branch the render
// takes (active, past due, cancelling at period end).
internal sealed class SubscriptionManagement(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{
    public string Slug => "subscription-management";
    public string Title => "Managing an existing subscription";
    public string Category => "Platform mechanics";

    private static readonly PaymentSubscription[] SampleSubscriptions =
    [
        new("sub_demo_team", PaymentProvider.Stripe, SubscriptionStatus.Active, "team-yearly", new DateTimeOffset(2027, 3, 14, 0, 0, 0, TimeSpan.Zero), false),
        new("sub_demo_pro", PaymentProvider.Stripe, SubscriptionStatus.PastDue, "pro-monthly", new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero), false),
        new("sub_demo_storage", PaymentProvider.Stripe, SubscriptionStatus.Active, "extra-storage-monthly", new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero), true),
    ];

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Button([Button.OutlineSm, "self-start"], text: "Load sample subscriptions",
                onClick: async () => _subscriptions.ReplaceAll(SampleSubscriptions));
            col.Text(["text-xs text-zinc-400"],
                "Sample subscriptions that exist at no payment provider, so Cancel and Resume have no real subscription to act on");
            Render(col);
        });
    }

    #region example:pattern-subscription-management
    // Per user, not per client: a subscription belongs to the customer, and the backend push that
    // changes it names the customer rather than any client session.
    private readonly UserReactiveList<PaymentSubscription> _subscriptions = new();
    private readonly ClientReactive<string?> _notice = new(null);

    public void Main()
    {
        WatchForChanges();
        app.OnClientJoined(async _ => await RefreshAsync(ReactiveScope.UserId));
    }

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
                // The push comes from the backend, so no user scope is active here and the client
                // scope is the backend session's:
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
        // Stripe invoices an upgrade's proration itself and reports 0 here; ProratedChargeRef
        // names that invoice.
        _notice.Value = !change.Changed
            ? "Already on that plan"
            : change.ProrationAmountMinor > 0
                ? $"{change.Direction}: {change.ProrationAmountMinor / 100.0:0.00} {change.Currency}"
                : $"{change.Direction}";

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
                col.Card(key: subscription.Id, contentStyle: ["p-3 flex flex-col gap-2"], content: card =>
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
