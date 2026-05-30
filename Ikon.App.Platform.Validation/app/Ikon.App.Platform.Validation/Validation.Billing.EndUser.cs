using System.Linq;

public partial class Validation
{
    private void RenderEndUserTab(UIView view)
    {
        view.Column([Layout.Column.Lg], content: col =>
        {
            if (_billingMode.Value == BillingModeConnect && ActiveBilling is null)
            {
                col.Box(["p-4 rounded-md bg-warning/10 border border-warning/30 text-warning text-sm"], content: w =>
                {
                    w.Text([Text.Body, "font-medium mb-1"], "Payments not yet active");
                    w.Text([Text.Caption], "Connect onboarding incomplete. End-users can't check out until the admin completes setup. Open the Admin tab → Connect.");
                });
                return;
            }

            RenderDiscoverAndBuySection(col);
            RenderManageSection(col);
            RenderAccountHistorySection(col);
        });
    }

    private void RenderDiscoverAndBuySection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            card.Text([Text.H2, "mb-1"], "Discover & buy");
            card.Text([Text.BodySm, "text-tertiary mb-4"], "Plan grid, single-plan card, primary CTA, and one-shot tips. Every selection calls a live BillingService method against the sandbox; checkout opens in a new tab via Stripe-hosted Checkout.");

            card.Text([Text.H3, "mt-2 mb-2"], "PricingTable");
            card.PricingTable(
                plans: ActivePlans(),
                onSelect: async planId => await RunActionAsync(
                    $"PricingTable.Select({planId})",
                    "Checkout session created",
                    async () =>
                    {
                        var session = await ActiveBilling!.CreateCheckoutAsync(
                            planId: planId,
                            appCustomerKey: ActiveCustomerKey,
                            email: "validation@ikon.live");
                        _billingLastCheckoutUrl.Value = session.Url;
                        _billingActionStatus.Value = $"Redirect session created (opening in new tab): {session.SessionId}";
                        await ClientFunctions.OpenExternalUrlAsync(session.Url);
                    }));

            if (!string.IsNullOrEmpty(_billingLastCheckoutUrl.Value))
            {
                card.Box(["mt-4 p-3 rounded-md bg-secondary/30 border border-secondary text-xs"], content: link =>
                {
                    link.Row([Layout.Row.Sm, "items-center"], content: r =>
                    {
                        r.Button(
                            style: [Button.OutlineSm, Button.IconLeft],
                            text: "Open Stripe Checkout", content: row => { row.Icon([Icon.Xs], name: "external-link"); row.Text([], "Open Stripe Checkout"); },
                            onClick: async () => await ClientFunctions.OpenExternalUrlAsync(_billingLastCheckoutUrl.Value!));
                        r.Text(["font-mono break-all text-tertiary"], _billingLastCheckoutUrl.Value!);
                    });
                });
            }

            card.Text([Text.H3, "mt-8 mb-2"], "PlanCard · standalone");
            card.Box(["max-w-sm"], content: pc =>
                pc.PlanCard(
                    plan: ValidationPlans[0],
                    onSelect: async planId =>
                    {
                        _billingActionStatus.Value = $"PlanCard onSelect → {planId}";
                        await Task.CompletedTask;
                    }));

            card.Text([Text.H3, "mt-8 mb-2"], "CheckoutButton");
            card.Row([Layout.Row.Md, "items-center"], content: r =>
            {
                r.CheckoutButton(
                    onCheckout: async () =>
                    {
                        if (ActiveBilling is null) { _billingActionStatus.Value = "CheckoutButton: billing not ready"; return null; }

                        try
                        {
                            var session = await ActiveBilling.CreateCheckoutAsync(
                                planId: PlanIdPro,
                                appCustomerKey: ActiveCustomerKey,
                                email: "validation@ikon.live");
                            _billingActionStatus.Value = $"CheckoutButton → {session.SessionId}";
                            return session.Url;
                        }
                        catch (Exception ex)
                        {
                            _billingActionStatus.Value = $"CheckoutButton failed: {FormatBillingError(ex)}";
                            return null;
                        }
                    },
                    text: "Checkout Pro");
            });

            card.Text([Text.H3, "mt-8 mb-2"], "TipPresetGrid");
            card.TipPresetGrid(
                presetsMinor: [100, 500, 2000, 10000],
                currencySymbol: "€",
                onTip: async amountMinor => await RunActionAsync(
                    $"TipPresetGrid.Tip(€{amountMinor / 100m:0.00})",
                    $"Tip session created (€{amountMinor / 100m:0.00})",
                    async () =>
                    {
                        var session = await ActiveBilling!.CreateTipCheckoutAsync(
                            amountMinor: amountMinor,
                            currency: "eur",
                            title: "Support the validation app",
                            message: "Test tip — Stripe sandbox",
                            appCustomerKey: ActiveCustomerKey);
                        _billingActionStatus.Value = $"TipPresetGrid → €{amountMinor / 100m:0.00} · {session.SessionId}";
                        _billingLastCheckoutUrl.Value = session.Url;
                        await ClientFunctions.OpenExternalUrlAsync(session.Url);
                    }));

        });
    }

    private void RenderManageSection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            card.Text([Text.H2, "mb-1"], "Manage subscription");
            card.Text([Text.BodySm, "text-tertiary mb-4"], "Live subscriptions for the demo customer. Cancel / pause / resume + seat scaling + plan migration. Customer Portal opens Stripe-hosted self-service (BYOK) or embedded management surface (Connect).");

            card.Text([Text.H3, "mt-2 mb-2"], "SubscriptionList + SubscriptionStatus");
            card.SubscriptionList(
                subscriptions: _billingSubscriptions.Value,
                projector: sub => new BillingSubscriptionView(
                    PlanName: $"{ResolvePlanName(sub)} · {sub.Id}",
                    Status: sub.Status,
                    CurrentPeriodEnd: sub.CurrentPeriodEnd,
                    CancelAtPeriodEnd: sub.CancelAtPeriodEnd),
                onCancel: id => SubscriptionAction(id, "cancel"),
                onCancelImmediate: id => SubscriptionAction(id, "cancel-now"),
                onPause: id => SubscriptionAction(id, "pause"),
                onResumeFromPause: id => SubscriptionAction(id, "resume"),
                onResume: id => SubscriptionAction(id, "uncancel"),
                footer: (footerView, sub) =>
                {
                    if (sub.ItemIds.Count == 0) { return; }

                    footerView.Row([Layout.Row.Sm, "items-center mt-2 flex-wrap"], content: r =>
                    {
                        r.Text([Text.Caption], "Seats");
                        r.TextField(_billingSeatQty, style: [Input.DefaultSm, "w-24"], type: "number", min: "1");
                        r.Button(
                            style: [Button.OutlineSm, Button.IconLeft],
                            text: "Update seats",
                            content: row => { row.Icon([Icon.Xs], name: "check"); row.Text([], "Update seats"); },
                            onClick: async () =>
                            {
                                if (long.TryParse(_billingSeatQty.Value, out var qty) && qty > 0)
                                {
                                    try
                                    {
                                        await ActiveBilling!.UpdateSubscriptionItemQuantityAsync(sub.ItemIds[0], qty);
                                        _billingActionStatus.Value = $"Updated {sub.ItemIds[0]} → qty {qty}";
                                        await RefreshBillingDataAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        _billingActionStatus.Value = $"Update qty failed: {FormatBillingError(ex)}";
                                    }
                                }
                            });

                        var targetPriceId = _billingTeamPriceId;
                        r.Button(
                            style: [Button.OutlineSm, Button.IconLeft],
                            text: "Migrate to Team",
                            content: row => { row.Icon([Icon.Xs], name: "arrow-right"); row.Text([], "Migrate to Team"); },
                            onClick: async () =>
                            {
                                if (string.IsNullOrEmpty(targetPriceId))
                                {
                                    _billingActionStatus.Value = "Team price not bootstrapped yet.";
                                    return;
                                }

                                try
                                {
                                    await ActiveBilling!.UpdateSubscriptionPriceAsync(sub.ItemIds[0], targetPriceId, prorate: true);
                                    _billingActionStatus.Value = $"Migrated {sub.ItemIds[0]} → {targetPriceId}";
                                    await RefreshBillingDataAsync();
                                }
                                catch (Exception ex)
                                {
                                    _billingActionStatus.Value = $"Migrate price failed: {FormatBillingError(ex)}";
                                }
                            });
                    });
                });

            card.Text([Text.H3, "mt-8 mb-2"], "UpcomingInvoicePreview");
            card.Row([Layout.Row.Md, "mb-3"], content: actions =>
            {
                actions.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Refresh upcoming invoice", content: row => { row.Icon([Icon.Xs], name: "calculator"); row.Text([], "Refresh upcoming invoice"); },
                    onClick: async () =>
                    {
                        var sub = _billingSubscriptions.Value.FirstOrDefault();

                        if (sub is null || ActiveBilling is null || ActiveCustomerId is null)
                        {
                            _billingActionStatus.Value = "Upcoming preview: needs an active subscription + billing ready.";
                            return;
                        }

                        try
                        {
                            _upcomingInvoice.Value = await ActiveBilling.PreviewUpcomingInvoiceAsync(ActiveCustomerId, subscriptionId: sub.Id);
                            _billingActionStatus.Value = "Upcoming invoice fetched.";
                        }
                        catch (Exception ex)
                        {
                            _billingActionStatus.Value = $"Preview failed: {FormatBillingError(ex)}";
                        }
                    });
            });

            if (_upcomingInvoice.Value is { } preview)
            {
                card.UpcomingInvoicePreview(preview);
            }
            else
            {
                card.Text([Text.Caption], "Click 'Refresh upcoming invoice' to fetch.");
            }

            if (_billingMode.Value == BillingModeByok)
            {
                card.Text([Text.H3, "mt-8 mb-2"], "BillingPortalButton");
                card.Text([Text.BodySm, "text-tertiary mb-2"], "Stripe-hosted Customer Portal — end-user updates card, downloads invoices, cancels subscription. BYOK only; Connect mode end-users manage payment methods via PaymentMethodList above.");

                card.Row([Layout.Row.Md], content: r =>
                {
                    r.BillingPortalButton(
                        onOpenPortal: async () =>
                        {
                            try
                            {
                                var portal = await ActiveBilling!.CreatePortalAsync(stripeCustomerId: ActiveCustomerId!);
                                _billingActionStatus.Value = "Portal session created";
                                return portal.Url;
                            }
                            catch (Exception ex)
                            {
                                _billingActionStatus.Value = $"Portal failed: {FormatBillingError(ex)}";
                                return null;
                            }
                        });
                });
            }
        });
    }

    private void RenderAccountHistorySection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            card.Text([Text.H2, "mb-1"], "Account & history");
            card.Text([Text.BodySm, "text-tertiary mb-4"], "Saved cards, invoices, charges with inline refund. All wrap BillingService list + mutate calls.");

            card.Text([Text.H3, "mt-2 mb-2"], "PaymentMethodList");

            var pms = _billingPaymentMethods.Value
                .Select(pm => new BillingPaymentMethodView(
                    pm.Id,
                    pm.CardBrand ?? pm.Type,
                    pm.CardLast4 ?? "----",
                    pm.CardExpMonth ?? 0,
                    pm.CardExpYear ?? 0))
                .ToList();

            card.PaymentMethodList(
                methods: pms,
                onDetach: async pmId =>
                {
                    try
                    {
                        await ActiveBilling!.DetachPaymentMethodAsync(pmId);
                        _billingActionStatus.Value = $"Detached {pmId}";
                        await RefreshBillingDataAsync();
                    }
                    catch (Exception ex)
                    {
                        _billingActionStatus.Value = $"Detach failed: {FormatBillingError(ex)}";
                    }
                });

            card.Text([Text.H3, "mt-8 mb-2"], "InvoiceList");

            var invoices = _billingInvoices.Value
                .Select(i => new BillingInvoiceView(
                    Id: i.Id,
                    Date: i.Created,
                    AmountLabel: $"{i.AmountDueMinor / 100m:0.00} {i.Currency.ToUpperInvariant()}",
                    Status: i.Status,
                    HostedUrl: i.HostedInvoiceUrl,
                    PdfUrl: i.InvoicePdfUrl))
                .ToList();

            card.InvoiceList(invoices: invoices);

            card.Text([Text.H3, "mt-8 mb-2"], "ChargeList");

            var charges = _billingCharges.Value
                .Select(ch => new BillingChargeView(
                    Id: ch.Id,
                    AmountLabel: $"{(ch.AmountMinor / 100m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} {ch.Currency.ToUpperInvariant()}",
                    Status: ch.Status,
                    Created: ch.Created,
                    Paid: ch.Paid,
                    Refunded: ch.Refunded,
                    PaymentIntentId: ch.PaymentIntentId,
                    ReceiptUrl: ch.ReceiptUrl,
                    Description: ch.Description))
                .ToList();

            card.ChargeList(
                charges: charges,
                emptyText: "No charges yet. Complete a checkout with test card 4242 4242 4242 4242.",
                onRefund: async paymentIntentId =>
                {
                    try
                    {
                        await ActiveBilling!.RefundAsync(
                            paymentIntentId: paymentIntentId,
                            amountMinor: null,
                            reason: "requested_by_customer",
                            idempotencyKey: $"validation-refund-{paymentIntentId}-{Guid.NewGuid():N}");
                        _billingActionStatus.Value = $"Refunded paymentIntent={paymentIntentId}";
                        await RefreshBillingDataAsync();
                    }
                    catch (Exception ex)
                    {
                        _billingActionStatus.Value = $"Refund failed: {FormatBillingError(ex)}";
                    }
                });

        });
    }

    private async Task SubscriptionAction(string subscriptionId, string action)
    {
        try
        {
            switch (action)
            {
                case "pause": await ActiveBilling!.PauseSubscriptionAsync(subscriptionId); break;
                case "resume": await ActiveBilling!.ResumeSubscriptionAsync(subscriptionId); break;
                case "cancel": await ActiveBilling!.CancelSubscriptionAsync(subscriptionId, immediate: false); break;
                case "cancel-now": await ActiveBilling!.CancelSubscriptionAsync(subscriptionId, immediate: true); break;
                case "uncancel": await ActiveBilling!.ResumeCanceledSubscriptionAsync(subscriptionId); break;
            }

            _billingActionStatus.Value = $"{action} → {subscriptionId}";
            await RefreshBillingDataAsync();
        }
        catch (Exception ex)
        {
            _billingActionStatus.Value = $"{action} failed: {FormatBillingError(ex)}";
        }
    }
}
