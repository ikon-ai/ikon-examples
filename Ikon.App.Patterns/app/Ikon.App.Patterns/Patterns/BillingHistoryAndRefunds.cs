namespace Ikon.App.Patterns.Patterns;

// Pattern: billing-history-and-refunds — see docs/patterns/billing-history-and-refunds.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class BillingHistoryAndRefunds : IPatternDemo
{
    public string Slug => "billing-history-and-refunds";
    public string Title => "Billing history, receipts and refunds";
    public string Category => "Platform mechanics";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-billing-history-and-refunds
    private readonly ClientReactiveList<Payment> _payments = new();
    private readonly ClientReactive<string?> _notice = new(null);

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
        PaymentRefund refund = await PaymentsService.Instance.RefundAsync(
            payment.Id, reason: "requested by customer");

        _notice.Value = refund.Status == RefundStatus.Unknown
            ? "Refund submitted; the provider has not confirmed it yet."
            : $"Refund {refund.Status} ({refund.Reference})";

        PaymentEntitlement entitlement = await PaymentsService.Instance.GetEntitlementAsync(
            payment.OfferId ?? "");

        if (entitlement.Active)
        {
            _notice.Value += " — access is still granted until it is revoked or expires.";
        }

        await RefreshAsync();
    }

    /// <summary>
    /// A receipt arrives as EITHER a Url or Pdf bytes, so a screen offering one renders both
    /// branches or it silently offers nothing.
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
                    // whole one; Status alone still reads Paid.
                    if (payment.AmountRefundedMinor > 0)
                    {
                        row.Text(["text-muted-foreground text-sm"],
                            text: $"−{payment.AmountRefundedMinor / 100.0:0.00}");
                    }

                    // Only a Paid payment can be refunded; Pending and Failed cannot, and
                    // offering the button anyway is a control that breaks its promise.
                    if (payment.Status == PaymentStatus.Paid
                        && payment.AmountRefundedMinor < payment.AmountMinor)
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
