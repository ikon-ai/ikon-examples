namespace Ikon.App.Patterns.Patterns;

// Pattern: billing-history-and-refunds — see docs/patterns/billing-history-and-refunds.md.
// The example region below is the canonical body the doc extracts. The gallery never runs Main, so
// nothing reads a real ledger: the demo loads sample rows covering each branch the render takes
// (paid, partly refunded, fully refunded, failed, pending).
internal sealed class BillingHistoryAndRefunds(IApp<SessionIdentity, ClientParameters> app) : IPatternDemo
{
    public string Slug => "billing-history-and-refunds";
    public string Title => "Billing history, receipts and refunds";
    public string Category => "Platform mechanics";

    private static readonly Payment[] SamplePayments =
    [
        new("pay_demo_4821", PaymentProvider.Stripe, PaymentStatus.Paid, PaymentKind.Subscription, "pro-monthly", 900, "EUR", 0, new DateTimeOffset(2026, 9, 1, 8, 12, 0, TimeSpan.Zero)),
        new("pay_demo_4710", PaymentProvider.Stripe, PaymentStatus.Paid, PaymentKind.OneTime, "credit-pack-500", 2500, "EUR", 1000, new DateTimeOffset(2026, 8, 17, 14, 40, 0, TimeSpan.Zero)),
        new("pay_demo_4655", PaymentProvider.Stripe, PaymentStatus.Paid, PaymentKind.Subscription, "pro-monthly", 900, "EUR", 900, new DateTimeOffset(2026, 8, 1, 8, 12, 0, TimeSpan.Zero)),
        new("pay_demo_4602", PaymentProvider.Stripe, PaymentStatus.Failed, PaymentKind.OneTime, null, 4900, "EUR", 0, new DateTimeOffset(2026, 7, 22, 19, 5, 0, TimeSpan.Zero)),
        new("pay_demo_4598", PaymentProvider.Stripe, PaymentStatus.Pending, PaymentKind.OneTime, "credit-pack-100", 600, "EUR", 0, new DateTimeOffset(2026, 7, 21, 9, 30, 0, TimeSpan.Zero)),
    ];

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Button([Button.OutlineSm, "self-start"], text: "Load sample billing history",
                onClick: async () => _payments.ReplaceAll(SamplePayments));
            col.Text(["text-xs text-zinc-400"],
                "Sample rows that exist at no payment provider, so Refund has no real payment to act on");
            Render(col);
        });
    }

    #region example:pattern-billing-history-and-refunds
    private readonly ClientReactiveList<Payment> _payments = new();
    private readonly ClientReactive<string?> _notice = new(null);

    private readonly ClientReactiveList<string> _refundsSubmitted = new();

    // Read the ledger when the customer arrives. A refund reaches it only when the provider's
    // refund webhook does -- the app hears that as a PaymentRefunded PaymentEventReceived event --
    // so the re-read after RefundAsync may still show the payment unrefunded: _refundsSubmitted
    // keeps its Refund button hidden until it lands.
    public void Main() => app.OnClientJoined(async _ => await RefreshAsync());

    private async Task RefreshAsync()
    {
        _payments.ReplaceAll(await PaymentsService.Instance.ListPaymentsAsync());
    }

    /// <summary>
    /// A refund does NOT revoke the entitlement the payment granted. Revoking is a separate
    /// decision the app makes -- read the PaymentEntitlement to see what the customer still has.
    /// </summary>
    private async Task RefundAsync(Payment payment)
    {
        // Hidden before the call, so a second click cannot land while the first is in flight.
        _refundsSubmitted.Add(payment.Id);
        PaymentRefund refund;

        try
        {
            // Stripe answers a repeated key with the same refund; Mollie ignores it.
            refund = await PaymentsService.Instance.RefundAsync(
                payment.Id, reason: "requested by customer", idempotencyKey: $"refund:{payment.Id}");
        }
        catch
        {
            _refundsSubmitted.Remove(payment.Id);
            throw;
        }

        await RefreshAsync();

        _notice.Value = refund.Status == RefundStatus.Unknown
            ? "Refund submitted; the provider reported a status we do not map."
            : $"Refund {refund.Status} ({refund.Reference})";

        // OfferId is null for an ad-hoc charge, which granted no entitlement to check.
        if (payment.OfferId is { } offerId)
        {
            PaymentEntitlement entitlement = await PaymentsService.Instance.GetEntitlementAsync(offerId);

            if (entitlement.Active)
            {
                _notice.Value += " — access is still granted until it is revoked or expires.";
            }
        }
    }

    /// <summary>
    /// A receipt arrives as a Url or as neither (Mollie has no customer-facing receipt); Pdf is
    /// null from every provider today. A screen offering one hides the button on neither.
    /// </summary>
    private static async Task<PaymentReceipt> ReceiptAsync(string paymentId) =>
        await PaymentsService.Instance.RequestReceiptAsync(paymentId);

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            foreach (var payment in _payments)
            {
                col.Row(["gap-3 items-center"], key: payment.Id, content: row =>
                {
                    row.Text(["flex-1"],
                        // A ternary inside an interpolation must be parenthesized -- the ':' would
                        // otherwise end the interpolation and start a format specifier (CS8361).
                        text: $"{payment.AmountMinor / 100.0:0.00} {payment.Currency} "
                            + $"({(payment.Kind == PaymentKind.Subscription ? "subscription" : "one-off")})");

                    // AmountRefundedMinor is what separates a partly-refunded payment from a
                    // whole one; after a partial refund Status still reads Paid.
                    if (payment.AmountRefundedMinor > 0)
                    {
                        row.Text(["text-muted-foreground text-sm"],
                            text: $"−{payment.AmountRefundedMinor / 100.0:0.00}");
                    }

                    // Only a Paid payment can be refunded; Pending and Failed cannot, and
                    // offering the button anyway is a control that breaks its promise.
                    if (payment.Status == PaymentStatus.Paid
                        && payment.AmountRefundedMinor < payment.AmountMinor
                        && !_refundsSubmitted.Contains(payment.Id))
                    {
                        row.Button(onClick: async () => await RefundAsync(payment),
                            content: v => v.Text(text: "Refund"));
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
