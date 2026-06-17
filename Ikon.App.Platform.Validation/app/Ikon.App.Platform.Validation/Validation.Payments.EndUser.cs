using System.Linq;

public partial class Validation
{
    private void RenderEndUserTab(UIView view)
    {
        view.Column([Layout.Column.Lg], content: col =>
        {
            if (_paymentsProviderValue == PaymentsProvider.IkonConnect && ActivePayments is null)
            {
                col.Box(["p-4 rounded-md bg-warning/10 border border-warning/30 text-warning text-sm"], content: w =>
                {
                    w.Text([Text.Body, "font-medium mb-1"], "Payments not yet active");
                    w.Text([Text.Caption], "Connect onboarding incomplete. End-users can't check out until the admin completes setup. Open the Admin tab → Connect.");
                });
                return;
            }

            RenderDiscoverAndBuySection(col);
            RenderBackendChannelSection(col);
            RenderManageSection(col);
            RenderAccountHistorySection(col);
        });
    }

    private void RenderBackendChannelSection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            card.Text([Text.H2, "mb-1"], "Backend channel · RPC + push");
            card.Text([Text.BodySm, "text-tertiary mb-4"], "New protocol-message control plane: the app asks the backend (which owns the normalized store + drives the provider) over /events, and receives normalized events back as pushes. Distinct from the raw Stripe proxy used above.");
            card.Row([Layout.Row.Sm, "flex-wrap"], content: r =>
            {
                r.Button(
                    style: [Button.OutlineSm],
                    text: "RPC checkout · Pro",
                    content: b => b.Text([], "RPC checkout · Pro"),
                    onClick: async () => await RunBackendCheckoutAsync(PlanIdPro));
                r.Button(
                    style: [Button.OutlineSm],
                    text: "RPC checkout · Team",
                    content: b => b.Text([], "RPC checkout · Team"),
                    onClick: async () => await RunBackendCheckoutAsync(PlanIdTeam));
                r.Button(
                    style: [Button.OutlineSm],
                    text: "RPC entitlement · Pro",
                    content: b => b.Text([], "RPC entitlement · Pro"),
                    onClick: async () => await RunBackendEntitlementAsync(PlanIdPro));
            });
        });
    }

    private void RenderDiscoverAndBuySection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            card.Text([Text.H2, "mb-1"], "Discover & buy");
            card.Text([Text.BodySm, "text-tertiary mb-4"], "Plan grid, single-plan card, primary CTA, and one-shot tips. Every selection calls a live PaymentsService method against the sandbox; checkout opens in a new tab via Stripe-hosted Checkout.");

            card.Text([Text.H3, "mt-2 mb-2"], "PricingTable");
            card.PricingTable(
                plans: ActivePlans(),
                onSelect: async planId => await RunActionAsync(
                    $"PricingTable.Select({planId})",
                    "Checkout session created",
                    async () =>
                    {
                        var session = await ActivePayments!.CreateCheckoutAsync(
                            planId: planId,
                            appCustomerKey: ActiveCustomerKey,
                            email: "validation@ikon.live");
                        _paymentsLastCheckoutUrl.Value = session.Url;
                        _paymentsActionStatus.Value = $"Redirect session created (opening in new tab): {session.SessionId}";
                        await ClientFunctions.OpenExternalUrlAsync(session.Url);
                    }));

            if (!string.IsNullOrEmpty(_paymentsLastCheckoutUrl.Value))
            {
                card.Box(["mt-4 p-3 rounded-md bg-secondary/30 border border-secondary text-xs"], content: link =>
                {
                    link.Row([Layout.Row.Sm, "items-center"], content: r =>
                    {
                        r.Button(
                            style: [Button.OutlineSm, Button.IconLeft],
                            text: "Open Stripe Checkout", content: row => { row.Icon([Icon.Xs], name: "external-link"); row.Text([], "Open Stripe Checkout"); },
                            onClick: async () => await ClientFunctions.OpenExternalUrlAsync(_paymentsLastCheckoutUrl.Value!));
                        r.Text(["font-mono break-all text-tertiary"], _paymentsLastCheckoutUrl.Value!);
                    });
                });
            }

            card.Text([Text.H3, "mt-8 mb-2"], "PlanCard · standalone");
            card.Box(["max-w-sm"], content: pc =>
                pc.PlanCard(
                    plan: ValidationPlans[0],
                    onSelect: async planId =>
                    {
                        _paymentsActionStatus.Value = $"PlanCard onSelect → {planId}";
                        await Task.CompletedTask;
                    }));

            card.Text([Text.H3, "mt-8 mb-2"], "CheckoutButton");
            card.Row([Layout.Row.Md, "items-center"], content: r =>
            {
                r.CheckoutButton(
                    onCheckout: async () =>
                    {
                        if (ActivePayments is null) { _paymentsActionStatus.Value = "CheckoutButton: payments not ready"; return null; }

                        try
                        {
                            var session = await ActivePayments.CreateCheckoutAsync(
                                planId: PlanIdPro,
                                appCustomerKey: ActiveCustomerKey,
                                email: "validation@ikon.live");
                            _paymentsActionStatus.Value = $"CheckoutButton → {session.SessionId}";
                            return session.Url;
                        }
                        catch (Exception ex)
                        {
                            _paymentsActionStatus.Value = $"CheckoutButton failed: {FormatPaymentsError(ex)}";
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
                        var session = await ActivePayments!.CreateTipCheckoutAsync(
                            amountMinor: amountMinor,
                            currency: "eur",
                            title: "Support the validation app",
                            message: "Test tip — Stripe sandbox",
                            appCustomerKey: ActiveCustomerKey);
                        _paymentsActionStatus.Value = $"TipPresetGrid → €{amountMinor / 100m:0.00} · {session.SessionId}";
                        _paymentsLastCheckoutUrl.Value = session.Url;
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
                subscriptions: _paymentsSubscriptions.Value,
                projector: sub => new PaymentsSubscriptionView(
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
                        r.TextField(_paymentsSeatQty, style: [Input.DefaultSm, "w-24"], type: "number", min: "1");
                        r.Button(
                            style: [Button.OutlineSm, Button.IconLeft],
                            text: "Update seats",
                            content: row => { row.Icon([Icon.Xs], name: "check"); row.Text([], "Update seats"); },
                            onClick: async () =>
                            {
                                if (long.TryParse(_paymentsSeatQty.Value, out var qty) && qty > 0)
                                {
                                    try
                                    {
                                        await ActivePayments!.UpdateSubscriptionItemQuantityAsync(sub.ItemIds[0], qty);
                                        _paymentsActionStatus.Value = $"Updated {sub.ItemIds[0]} → qty {qty}";
                                        await RefreshPaymentsDataAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        _paymentsActionStatus.Value = $"Update qty failed: {FormatPaymentsError(ex)}";
                                    }
                                }
                            });

                        var targetPriceId = _paymentsTeamPriceId;
                        r.Button(
                            style: [Button.OutlineSm, Button.IconLeft],
                            text: "Migrate to Team",
                            content: row => { row.Icon([Icon.Xs], name: "arrow-right"); row.Text([], "Migrate to Team"); },
                            onClick: async () =>
                            {
                                if (string.IsNullOrEmpty(targetPriceId))
                                {
                                    _paymentsActionStatus.Value = "Team price not bootstrapped yet.";
                                    return;
                                }

                                try
                                {
                                    await ActivePayments!.UpdateSubscriptionPriceAsync(sub.ItemIds[0], targetPriceId, prorate: true);
                                    _paymentsActionStatus.Value = $"Migrated {sub.ItemIds[0]} → {targetPriceId}";
                                    await RefreshPaymentsDataAsync();
                                }
                                catch (Exception ex)
                                {
                                    _paymentsActionStatus.Value = $"Migrate price failed: {FormatPaymentsError(ex)}";
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
                        var sub = _paymentsSubscriptions.Value.FirstOrDefault();

                        if (sub is null || ActivePayments is null || ActiveCustomerId is null)
                        {
                            _paymentsActionStatus.Value = "Upcoming preview: needs an active subscription + payments ready.";
                            return;
                        }

                        try
                        {
                            _upcomingInvoice.Value = await ActivePayments.PreviewUpcomingInvoiceAsync(ActiveCustomerId, subscriptionId: sub.Id);
                            _paymentsActionStatus.Value = "Upcoming invoice fetched.";
                        }
                        catch (Exception ex)
                        {
                            _paymentsActionStatus.Value = $"Preview failed: {FormatPaymentsError(ex)}";
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

            if (_paymentsProviderValue == PaymentsProvider.Byok)
            {
                card.Text([Text.H3, "mt-8 mb-2"], "PaymentsPortalButton");
                card.Text([Text.BodySm, "text-tertiary mb-2"], "Stripe-hosted Customer Portal — end-user updates card, downloads invoices, cancels subscription. BYOK only; Connect mode end-users manage payment methods via PaymentMethodList above.");

                card.Row([Layout.Row.Md], content: r =>
                {
                    r.PaymentsPortalButton(
                        onOpenPortal: async () =>
                        {
                            try
                            {
                                var portal = await ActivePayments!.CreatePortalAsync(stripeCustomerId: ActiveCustomerId!);
                                _paymentsActionStatus.Value = "Portal session created";
                                return portal.Url;
                            }
                            catch (Exception ex)
                            {
                                _paymentsActionStatus.Value = $"Portal failed: {FormatPaymentsError(ex)}";
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
            card.Text([Text.BodySm, "text-tertiary mb-4"], "Saved cards, invoices, charges with inline refund. All wrap PaymentsService list + mutate calls.");

            card.Text([Text.H3, "mt-2 mb-2"], "PaymentMethodList");

            var pms = _paymentsPaymentMethods.Value
                .Select(pm => new PaymentsPaymentMethodView(
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
                        await ActivePayments!.DetachPaymentMethodAsync(pmId);
                        _paymentsActionStatus.Value = $"Detached {pmId}";
                        await RefreshPaymentsDataAsync();
                    }
                    catch (Exception ex)
                    {
                        _paymentsActionStatus.Value = $"Detach failed: {FormatPaymentsError(ex)}";
                    }
                });

            card.Text([Text.H3, "mt-8 mb-2"], "InvoiceList");

            var invoices = _paymentsInvoices.Value
                .Select(i => new PaymentsInvoiceView(
                    Id: i.Id,
                    Date: i.Created,
                    AmountLabel: $"{i.AmountDueMinor / 100m:0.00} {i.Currency.ToUpperInvariant()}",
                    Status: i.Status,
                    HostedUrl: i.HostedInvoiceUrl,
                    PdfUrl: i.InvoicePdfUrl))
                .ToList();

            card.InvoiceList(invoices: invoices);

            card.Text([Text.H3, "mt-8 mb-2"], "ChargeList");

            var charges = _paymentsCharges.Value
                .Select(ch => new PaymentsChargeView(
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
                        await ActivePayments!.RefundAsync(
                            paymentIntentId: paymentIntentId,
                            amountMinor: null,
                            reason: "requested_by_customer",
                            idempotencyKey: $"validation-refund-{paymentIntentId}-{Guid.NewGuid():N}");
                        _paymentsActionStatus.Value = $"Refunded paymentIntent={paymentIntentId}";
                        await RefreshPaymentsDataAsync();
                    }
                    catch (Exception ex)
                    {
                        _paymentsActionStatus.Value = $"Refund failed: {FormatPaymentsError(ex)}";
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
                case "pause": await ActivePayments!.PauseSubscriptionAsync(subscriptionId); break;
                case "resume": await ActivePayments!.ResumeSubscriptionAsync(subscriptionId); break;
                case "cancel": await ActivePayments!.CancelSubscriptionAsync(subscriptionId, immediate: false); break;
                case "cancel-now": await ActivePayments!.CancelSubscriptionAsync(subscriptionId, immediate: true); break;
                case "uncancel": await ActivePayments!.ResumeCanceledSubscriptionAsync(subscriptionId); break;
            }

            _paymentsActionStatus.Value = $"{action} → {subscriptionId}";
            await RefreshPaymentsDataAsync();
        }
        catch (Exception ex)
        {
            _paymentsActionStatus.Value = $"{action} failed: {FormatPaymentsError(ex)}";
        }
    }
}
