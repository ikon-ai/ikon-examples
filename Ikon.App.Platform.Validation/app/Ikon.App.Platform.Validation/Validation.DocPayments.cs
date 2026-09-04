// The payments guide, as one file that compiles.
file sealed class DocPaymentsGuide(IApp<SessionIdentity, ClientParams> app)
{
    private static Task OnPaymentAsync(PaymentEvent evt) => Task.CompletedTask;

    private static Task FulfilAsync(PaymentEvent evt) => Task.CompletedTask;

    private static Task ExtendAsync(PaymentEvent evt) => Task.CompletedTask;

    private static Task RevokeAsync(PaymentEvent evt) => Task.CompletedTask;

    private static Task OnRefundAsync(PaymentEvent evt) => Task.CompletedTask;

    public async Task SetUpAsync()
    {
        #region docsnippet:payments-setup
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
        #endregion
    }

    public void AllowAnonymous()
    {
        #region docsnippet:payments-anonymous
        app.Payments.AllowAnonymousPayments = true;   // accept guest payments (e.g. anonymous tips)
        #endregion
    }

    public async Task CreateOfferAsync()
    {
        #region docsnippet:payments-create-offer
        await app.Payments.CreateOfferAsync(new OfferSpec("pro", "Pro",
            new OfferPriceSpec(AmountMinor: 999, Currency: "eur", Kind: PriceKind.Recurring, Interval: PriceInterval.Month)));
        // one-time offer: new OfferPriceSpec(500, "eur", PriceKind.OneTime)
        #endregion
    }

    public async Task UpgradeCreditAsync(long level2PriceMinor)
    {
        #region docsnippet:payments-upgrade-credit
        // The customer already bought level1; charge only the difference for level2.
        var payments = await app.Payments.ListPaymentsAsync();
        var credit = payments
            .Where(p => p.OfferId == "level1" && p.Status == PaymentStatus.Paid)
            .Sum(p => p.AmountMinor - p.AmountRefundedMinor);

        var link = await app.Payments.CreatePaymentLinkAsync("level2", amountMinorOverride: level2PriceMinor - credit);
        await ClientFunctions.OpenExternalUrlAsync(link.Url);
        #endregion
    }

    public async Task ChangeOfferAsync(string subscriptionId)
    {
        #region docsnippet:payments-change-offer
        var change = await app.Payments.ChangeSubscriptionOfferAsync(subscriptionId, "level2");
        #endregion

        Log.Instance.Debug($"{change}");
    }

    public async Task ResumeAsync(string subscriptionId)
    {
        #region docsnippet:payments-resume
        var resume = await app.Payments.ResumeSubscriptionAsync(subscriptionId);
        // resume.SubscriptionId may differ from the input when the provider recreated the subscription (Mollie).
        #endregion

        Log.Instance.Debug($"{resume}");
    }

    public async Task PromotionCodesAsync()
    {
        #region docsnippet:payments-promotion-codes
        var link = await app.Payments.CreatePaymentLinkAsync("pro", allowPromotionCodes: true);
        #endregion

        Log.Instance.Debug($"{link}");
    }

    public void RouteEvents()
    {
        #region docsnippet:payments-route-events
        app.Payments.PaymentEventReceived += evt => evt.Type switch
        {
            PaymentEventType.PaymentPaid           => FulfilAsync(evt),
            PaymentEventType.SubscriptionRenewed   => ExtendAsync(evt),
            PaymentEventType.SubscriptionCanceled  => RevokeAsync(evt),
            PaymentEventType.PaymentRefunded       => OnRefundAsync(evt),
            _ => Task.CompletedTask,
        };
        #endregion
    }

    public void Entitlement(UIView view)
    {
        #region docsnippet:payments-entitlement
        if (app.Payments.IsEntitled("pro"))
        {
            view.Text([Text.Body], "✨ Pro feature");
        }
        #endregion
    }

    public async Task ReceiptAsync(UIView view, string paymentId)
    {
        #region docsnippet:payments-receipt
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
        #endregion
    }
}
