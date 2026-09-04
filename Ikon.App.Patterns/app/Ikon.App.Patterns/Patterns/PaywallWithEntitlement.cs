namespace Ikon.App.Patterns.Patterns;

// Pattern: paywall-with-entitlement — see docs/patterns/paywall-with-entitlement.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class PaywallWithEntitlement : IPatternDemo
{
    public string Slug => "paywall-with-entitlement";
    public string Title => "Paywall driven by an entitlement";
    public string Category => "Platform mechanics";
    public void RenderDemo(IView view) => Render(view);

    private static void RenderProContent(IView view) => throw new NotImplementedException();

    #region docsnippet:pattern-paywall-with-entitlement
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
    #endregion
}
