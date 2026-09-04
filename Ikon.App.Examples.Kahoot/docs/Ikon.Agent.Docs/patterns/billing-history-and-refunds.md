<!-- mined-from: Ikon.App.Patterns -->
# Billing History And Refunds — What The Customer Actually Has

The third payments surface, after selling (`paywall-with-entitlement`) and managing
(`subscription-management`): the ledger a customer or a support agent looks at. Every number here
has a trap in it, and each one produces a screen that looks right and is wrong.

## When to use

An account's billing page, a support tool, a receipts list, anywhere a refund can be issued.

## Notes

- **A refund does NOT revoke the entitlement** the payment granted. Revoking is a separate decision
  the app makes — read the `PaymentEntitlement` (`Active`, `ExpiresAt`, `Source`) to see what the
  customer still has after the money goes back.
- **`AmountRefundedMinor` is what separates a partly-refunded payment from a whole one.**
  `Status` alone still reads `Paid`, so a screen branching only on status shows a refunded payment
  as if nothing happened.
- **Only a `Paid` payment can be refunded.** Offering the button on `Pending` or `Failed` is a
  control that breaks its promise — and one already fully refunded has nothing left to return.
- `RefundStatus.Unknown` means submitted but not yet confirmed by the provider, not failed. Say
  "submitted" rather than implying it is done.
- **A `PaymentReceipt` carries EITHER a `Url` or `Pdf` bytes** (with `PdfContentType`). A screen
  offering one renders both branches or it silently offers nothing.
- `EntitlementSource` distinguishes a `Subscription` entitlement from a `OneTime` purchase — the
  same `Active: true` means different things for what happens next month.
- `ReconcileAsync` returns a `PaymentReconcileResult` whose `Enqueued` counts objects queued for
  re-processing. Their effects arrive **asynchronously** as ordinary payment events, so do not
  await a state change after calling it.
- A ternary inside a string interpolation must be parenthesized — the `:` would otherwise end the
  interpolation and start a format specifier (CS8361).

## Snippet

```csharp
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
```

## See also

- `paywall-with-entitlement` — declaring an offer and selling it.
- `subscription-management` — changing, cancelling and resuming a subscription.
