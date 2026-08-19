using System.Globalization;
using System.Text.Json;

public partial class Validation
{
    private const string SimulatedSubscriptionPrefix = "sub_validation_expiry_";

    private readonly Reactive<string> _payProvider = new("stripe");
    private readonly Reactive<string> _payOfferId = new("validation_subscription");
    private readonly Reactive<string> _payAmount = new("5.00");
    private readonly Reactive<string> _payCurrency = new("eur");
    private readonly Reactive<string> _payChargeDescription = new("Validation charge");
    private readonly Reactive<string> _payCustomerOverride = new("");
    private readonly Reactive<bool> _payAllowPromo = new(false);
    private readonly Reactive<string> _payPriceOverride = new("");
    private readonly Reactive<string> _payChangeOfferId = new("");

    private readonly Reactive<string> _payNewOfferId = new("my_offer");
    private readonly Reactive<string> _payNewOfferName = new("My Offer");
    private readonly Reactive<string> _payNewOfferPrice = new("9.99");
    private readonly Reactive<string> _payNewOfferCurrency = new("eur");
    private readonly Reactive<string> _payNewOfferKind = new("one_time");

    private readonly Reactive<IReadOnlyList<PaymentOffer>> _payOffers = new([]);
    private readonly Reactive<bool> _payOffersLoaded = new(false);
    private readonly Reactive<IReadOnlyList<PaymentSubscription>> _paySubs = new([]);
    private readonly Reactive<IReadOnlyList<Payment>> _payHistory = new([]);
    private readonly Reactive<bool> _payCustomerLoaded = new(false);

    private readonly Reactive<bool> _payBusy = new(false);
    private readonly Reactive<string> _payEntitlement = new("");
    private readonly Reactive<string> _payGate = new("");
    private readonly ReactiveList<string> _payEventLog = new();
    private readonly Reactive<PaymentReceipt?> _payReceipt = new((PaymentReceipt?)null);

    private async Task InitPaymentsAsync()
    {
        // Default to Stripe; the selector below repins per request to test a
        // multi-provider space. Leaving this null would fall back to the space's
        // default merchant, which isn't necessarily the one you want to validate.
        // Redirects default to this app's own URL — no need to set DefaultSuccessUrl/DefaultCancelUrl.
        app.Payments.DefaultProvider = PaymentProvider.Stripe;
        app.Payments.PaymentEventReceived += OnPaymentEventAsync;

        // The joining client carries the user scope the SDK's customerKey default
        // resolves from, so the catalog and the signed-in user's billing data are
        // ready without pressing anything.
        app.OnClientJoined(async ctx =>
        {
            await RunPaymentsActionAsync(RefreshOffersAsync);
            await RunPaymentsActionAsync(ReloadCustomerAsync);
        });
        await Task.CompletedTask;
    }

    private Task OnPaymentEventAsync(PaymentEvent evt)
    {
        var what = evt.Type switch
        {
            PaymentEventType.PaymentPaid => "A payment succeeded",
            PaymentEventType.PaymentRefunded => "A payment was refunded",
            PaymentEventType.SubscriptionActivated => "A subscription activated",
            PaymentEventType.SubscriptionUpdated => "A subscription was updated",
            PaymentEventType.SubscriptionRenewed => "A subscription renewed",
            PaymentEventType.SubscriptionCanceled => "A subscription was canceled",
            _ => "An event arrived",
        };
        LogPayments(what);
        return Task.CompletedTask;
    }

    // Empty override = act as the signed-in user, the SDK's own customerKey default.
    private string? CustomerOverrideOrNull
        => string.IsNullOrWhiteSpace(_payCustomerOverride.Value) ? null : _payCustomerOverride.Value.Trim();

    // Synthetic provider events need a concrete key even where no client scope exists.
    private string SimulationCustomer
        => CustomerOverrideOrNull ?? ReactiveScope.UserIdOrNull ?? "demo-customer";

    private void RenderPaymentsSection(UIView view)
    {
        if (RenderSectionLocked(view, "Payments"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: col =>
        {
            // Provider ------------------------------------------------------
            col.Column([Card.Default, "p-6 gap-3"], content: card =>
            {
                card.Text([Text.H3], text: "Payment provider");
                card.Row(["gap-3 flex-wrap items-end"], content: row =>
                {
                    row.Select(
                        value: _payProvider.Value,
                        options: [new SelectOption("auto", "Auto (space default)"), new SelectOption("stripe", "Stripe"), new SelectOption("mollie", "Mollie"), new SelectOption("surfboard", "Surfboard")],
                        label: "Provider",
                        onValueChange: async v =>
                        {
                            _payProvider.Value = v;
                            app.Payments.DefaultProvider = v switch
                            {
                                "stripe" => PaymentProvider.Stripe,
                                "mollie" => PaymentProvider.Mollie,
                                "surfboard" => PaymentProvider.Surfboard,
                                _ => null,
                            };
                        });
                });
            });

            // 1. What's for sale ------------------------------------------
            col.Column([Card.Default, "p-6 gap-3"], content: card =>
            {
                card.Text([Text.H3], text: "Offers");

                if (_payOffersLoaded.Value && _payOffers.Value.Count == 0)
                {
                    card.Text([Text.BodySm, "text-tertiary"], text: "No offers yet — create one below.");
                }

                foreach (var offer in _payOffers.Value)
                {
                    var offerId = offer.OfferId;
                    var price = offer.Prices.FirstOrDefault();
                    card.Row([Card.Default, "p-3 items-center justify-between gap-3 flex-wrap"], key: $"offer-{offerId}", content: r =>
                    {
                        r.Column([Layout.Column.Xs, "min-w-0"], content: c =>
                        {
                            c.Text([Text.Body, "font-medium"], text: offer.Name);
                            c.Row(["gap-4 flex-wrap"], content: facts =>
                            {
                                RenderOfferFact(facts, "Offer ID", offerId);
                                RenderOfferFact(facts, "Type", price?.Kind == PriceKind.Recurring ? "Recurring" : "One-time");
                                RenderOfferFact(facts, "Amount", price is null ? "—" : $"{price.AmountMinor / 100.0:0.00} {price.Currency.ToUpperInvariant()}" + (price.Kind == PriceKind.Recurring ? $" / {IntervalLabel(price)}" : ""));
                            });
                        });
                        r.Row([Layout.Row.Xs, "flex-wrap"], content: actions =>
                        {
                            actions.Button([Button.PrimarySm], text: "Take payment", disabled: _payBusy.Value, onClick: () => PayAsync(offerId));
                            actions.Button([Button.PrimarySm], text: "Remove", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                            {
                                var removed = await app.Payments.RemoveOfferAsync(offerId);
                                LogPayments(removed ? $"Removed offer '{offerId}' from the catalog" : $"Offer '{offerId}' was not an active offer");
                                await RefreshOffersAsync();
                            }));
                        });
                    });
                }

                card.Row([Layout.Row.Sm, "flex-wrap items-center"], content: btns =>
                {
                    btns.Button([Button.PrimarySm], text: "Refresh offers", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(RefreshOffersAsync));
                    btns.Button([Button.PrimarySm], text: "Create validation offers", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(CreateValidationOffersAsync));
                    btns.Row([Layout.Row.InlineCenter, "flex-wrap"], content: promo =>
                    {
                        promo.Switch([Switch.Default], bind: _payAllowPromo, props: TestId("pay-allow-promo"),
                            content: v => v.SwitchThumb([Switch.Thumb]));
                        promo.Text([Text.Caption], text: "Promo codes at checkout (Stripe)");
                    });
                    btns.TextField(bind: _payPriceOverride, label: "Price override", placeholder: "empty = offer price", style: [Input.Default, "w-32"], type: "number", min: "0", step: "0.01", props: TestId("pay-price-override"));
                });

                card.AccordionSingle([Accordion.Root], collapsible: true, content: acc =>
                {
                    acc.AccordionItem([Accordion.Item, "border-0"], value: "create-offer", content: item =>
                    {
                        item.AccordionHeader([Accordion.Header], content: header =>
                        {
                            header.AccordionTrigger([Accordion.Trigger], content: trigger =>
                            {
                                trigger.Text(text: "Create an offer");
                                trigger.Icon([Accordion.ChevronIcon], name: "chevron-down");
                            });
                        });
                        item.AccordionContent([Accordion.Content], content: body =>
                        {
                            body.Row([Accordion.ContentInner, "gap-3 flex-wrap items-end"], content: row =>
                            {
                                row.TextField(bind: _payNewOfferId, label: "Offer ID", style: [Input.Default, "w-36"]);
                                row.TextField(bind: _payNewOfferName, label: "Name", style: [Input.Default, "w-40"]);
                                row.TextField(bind: _payNewOfferPrice, label: "Price", style: [Input.Default, "w-24"], type: "number", min: "0", step: "0.01");
                                row.Select(
                                    value: _payNewOfferCurrency.Value,
                                    options: CurrencyOptions,
                                    label: "Currency",
                                    onValueChange: async v => _payNewOfferCurrency.Value = v);
                                row.Select(
                                    value: _payNewOfferKind.Value,
                                    options: [new SelectOption("one_time", "One-time"), new SelectOption("month", "Monthly subscription")],
                                    label: "Type",
                                    onValueChange: async v => _payNewOfferKind.Value = v);
                                row.Button([Button.PrimarySm], text: "Create offer", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(CreateOfferAsync));
                            });
                        });
                    });
                });
            });

            // 2. Custom amount --------------------------------------------
            col.Column([Card.Default, "p-6 gap-3"], content: card =>
            {
                card.Text([Text.H3], text: "Custom charges");
                card.Row(["gap-3 flex-wrap items-end"], content: row =>
                {
                    row.TextField(bind: _payAmount, label: "Amount", style: [Input.Default, "w-24"], type: "number", min: "0", step: "0.01");
                    row.Select(
                        value: _payCurrency.Value,
                        options: CurrencyOptions,
                        label: "Currency",
                        onValueChange: async v => _payCurrency.Value = v);
                    row.TextField(bind: _payChargeDescription, label: "Description", style: [Input.Default, "min-w-[220px]"]);
                    row.Button([Button.PrimarySm], text: "Charge", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                    {
                        if (!TryParseAmountMinor(_payAmount.Value, out var amountMinor))
                        {
                            LogPayments("Error: enter a positive amount, e.g. 5.00");
                            return;
                        }
                        var link = await app.Payments.CreatePaymentLinkAsync(amountMinor, _payCurrency.Value, CustomerOverrideOrNull, description: _payChargeDescription.Value, allowPromotionCodes: _payAllowPromo.Value);
                        LogPayments($"Opened a payment page for {amountMinor / 100.0:0.00} {_payCurrency.Value.ToUpperInvariant()}");
                        await OpenLinkAsync(link);
                    }));
                });
            });

            // 3. Does this customer have access ---------------------------
            col.Column([Card.Default, "p-6 gap-3"], content: card =>
            {
                card.Text([Text.H3], text: "Entitlements");
                card.Row(["gap-3 flex-wrap items-end"], content: row =>
                {
                    row.TextField(bind: _payOfferId, label: "Offer ID", style: [Input.Default, "w-48"]);
                    row.Button([Button.PrimarySm], text: "Check access", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                    {
                        var e = await app.Payments.GetEntitlementAsync(_payOfferId.Value, CustomerOverrideOrNull);
                        _payEntitlement.Value = e.Active
                            ? $"✓ Access to '{_payOfferId.Value}' (via {e.Source})" + (e.ExpiresAt is { } d ? $" — until {d:yyyy-MM-dd}" : "")
                            : $"✗ No access to '{_payOfferId.Value}'" + (e.ExpiresAt is { } x && x < DateTimeOffset.UtcNow ? $" — expired {x:yyyy-MM-dd}" : "");
                    }));
                });
                if (!string.IsNullOrEmpty(_payEntitlement.Value))
                {
                    card.Text([Text.Body], text: $"Backend says: {_payEntitlement.Value}", props: TestId("pay-entitlement"));
                }

                // Synchronous, cached gate — safe to read every render, re-renders on change.
                var entitledNow = app.Payments.IsEntitled(_payOfferId.Value, CustomerOverrideOrNull);
                card.Text([Text.BodySm, entitledNow ? "text-success-primary" : "text-tertiary", "font-mono"],
                    text: (entitledNow ? "✓" : "✗") + $" IsEntitled(\"{_payOfferId.Value}\") — live, updates automatically",
                    props: TestId("pay-entitled-live"));
            });

            // 4. The customer's stuff -------------------------------------
            col.Column([Card.Default, "p-6 gap-3"], content: card =>
            {
                card.Text([Text.H3], text: "Subscriptions & payments");
                card.Row(["gap-3 flex-wrap items-end"], content: row =>
                {
                    row.Button([Button.PrimarySm], text: "Refresh", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(ReloadCustomerAsync));
                    row.TextField(bind: _payChangeOfferId, label: "New offer ID", placeholder: "for Change offer", style: [Input.Default, "w-48"], props: TestId("pay-change-offer-id"));
                });

                if (_payCustomerLoaded.Value)
                {
                    card.Text([Text.Label, "mt-2"], text: "Subscriptions");
                    if (_paySubs.Value.Count == 0)
                    {
                        card.Text([Text.BodySm, "text-tertiary"], text: "None.", props: TestId("pay-subs-empty"));
                    }
                    foreach (var sub in _paySubs.Value)
                    {
                        var id = sub.Id;
                        var isSimulated = id.StartsWith(SimulatedSubscriptionPrefix, StringComparison.Ordinal);
                        card.Row([Card.Default, "p-3 items-center justify-between"], key: $"sub-{id}", content: r =>
                        {
                            r.Column([Layout.Column.Xs, "min-w-0"], content: c =>
                            {
                                c.Text([Text.Body, "font-medium"], text: OfferLabel(sub.OfferId) ?? "Subscription");
                                c.Text([Text.Caption], text: $"{id} · {sub.Status}" + (sub.CurrentPeriodEnd is { } d ? $" · {(sub.CancelAtPeriodEnd ? "ends" : "renews")} {d:yyyy-MM-dd}" : "") + (isSimulated ? " · simulated" : ""));
                            });
                            r.Row([Layout.Row.Xs, "flex-wrap"], content: actions =>
                            {
                                if (isSimulated)
                                {
                                    // A simulated subscription exists only in the normalized store, so a
                                    // real provider cancel can only fail — end it through the simulator.
                                    actions.Button([Button.PrimarySm], text: "Revoke (simulated)", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(
                                        () => SimulateSubscriptionRevokedAsync(id.Substring(SimulatedSubscriptionPrefix.Length))));
                                    return;
                                }
                                actions.Button([Button.PrimarySm], text: "Cancel at period end", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                                {
                                    await app.Payments.CancelSubscriptionAsync(id);
                                    LogPayments("Canceled a subscription — it stays active until the period ends");
                                    await ReloadCustomerAsync();
                                }));
                                actions.Button([Button.PrimarySm], text: "Cancel now", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                                {
                                    await app.Payments.CancelSubscriptionAsync(id, immediate: true);
                                    LogPayments("Canceled a subscription immediately");
                                    await ReloadCustomerAsync();
                                }));
                                actions.Button([Button.PrimarySm], text: "Change offer", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                                {
                                    var change = await app.Payments.ChangeSubscriptionOfferAsync(id, _payChangeOfferId.Value);
                                    LogPayments(change.Changed
                                        ? $"Plan {change.Direction} ({change.Effective})" + (change.ProratedChargeRef is { } chargeRef ? $" — charged {change.ProrationAmountMinor / 100.0:0.00} {change.Currency?.ToUpperInvariant()} ({chargeRef})" : " — no charge")
                                        : "Already on that offer");
                                    await ReloadCustomerAsync();
                                }));
                                if (sub.CancelAtPeriodEnd)
                                {
                                    actions.Button([Button.PrimarySm], text: "Resume", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                                    {
                                        var resume = await app.Payments.ResumeSubscriptionAsync(id);
                                        LogPayments(resume.Resumed ? $"Resumed subscription ({resume.SubscriptionId})" : "Resume did nothing");
                                        await ReloadCustomerAsync();
                                    }));
                                }
                            });
                        });
                    }

                    card.Text([Text.Label, "mt-2"], text: "Payments");
                    if (_payHistory.Value.Count == 0)
                    {
                        card.Text([Text.BodySm, "text-tertiary"], text: "None.", props: TestId("pay-payments-empty"));
                    }
                    foreach (var pay in _payHistory.Value)
                    {
                        var id = pay.Id;
                        var paidFor = OfferLabel(pay.OfferId) ?? (pay.Kind == PaymentKind.Subscription ? "Subscription payment" : "Custom charge");
                        card.Row([Card.Default, "p-3 items-center justify-between"], key: $"pay-{id}", content: r =>
                        {
                            r.Column([Layout.Column.Xs, "min-w-0"], content: c =>
                            {
                                c.Text([Text.Body, "font-medium"], text: $"{paidFor} — {pay.AmountMinor / 100.0:0.00} {pay.Currency.ToUpperInvariant()}");
                                c.Text([Text.Caption], text: $"{pay.Status}" + (pay.CreatedAt is { } d ? $" · {d:yyyy-MM-dd}" : "") + (pay.OfferId is { } payOfferId ? $" · {payOfferId}" : ""));
                            });
                            r.Row([Layout.Row.Xs, "flex-wrap"], content: actions =>
                            {
                                actions.Button([Button.PrimarySm], text: "Receipt", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(() => RequestReceiptAsync(id)));
                                actions.Button([Button.PrimarySm], text: "Refund", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                                {
                                    var refund = await app.Payments.RefundAsync(id);
                                    LogPayments($"Refund {refund.Status}");
                                    await ReloadCustomerAsync();
                                }));
                            });
                        });
                    }
                }
            });

            // 5. Gate a feature -------------------------------------------
            col.Column([Card.Default, "p-6 gap-3"], content: card =>
            {
                card.Text([Text.H3], text: "Feature gating");
                card.Button([Button.PrimarySm, "self-start"], text: "Evaluate the gate", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                {
                    var policy = new PaymentsRequireEntitlementAttribute(_payOfferId.Value).CreatePolicy();
                    var ctx = new PolicyCallContext(Guid.NewGuid(), "validation-demo", 0, SimulationCustomer, null, null, true, CancellationToken.None);
                    var decision = await policy.EvaluateAsync([], ctx);
                    _payGate.Value = decision is PolicyDecision.Deny
                        ? $"Blocked — the customer has no active '{_payOfferId.Value}' entitlement."
                        : "Allowed — the call would run.";
                }));
                if (!string.IsNullOrEmpty(_payGate.Value))
                {
                    card.Text([Text.Body], text: _payGate.Value, props: TestId("pay-gate-result"));
                }
            });

            // 6. Testing tools --------------------------------------------
            col.Column([Card.Default, "p-6 gap-3"], content: card =>
            {
                card.Text([Text.H3], text: "Testing tools");
                card.Row(["gap-3 flex-wrap items-end"], content: row =>
                {
                    row.TextField(bind: _payCustomerOverride, label: "Customer override", placeholder: "empty = signed-in user", style: [Input.Default, "min-w-[220px]"], props: TestId("pay-customer-override"));
                });

                card.Text([Text.Label, "mt-2"], text: "Simulate provider webhooks");
                card.Row([Layout.Row.Sm, "flex-wrap"], content: btns =>
                {
                    btns.Button([Button.PrimarySm], text: "Grant expired access", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(() => SimulateSubscriptionAccessAsync(expired: true)));
                    btns.Button([Button.PrimarySm], text: "Grant active access", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(() => SimulateSubscriptionAccessAsync(expired: false)));
                    btns.Button([Button.PrimarySm], text: "Revoke access", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(() => SimulateSubscriptionRevokedAsync(SimulationCustomer)));
                });
            });

            // 7. Events ---------------------------------------------------
            col.Column([Card.Default, "p-6 gap-3"], content: card =>
            {
                card.Text([Text.H3], text: "Events");
                if (_payEventLog.Value.Count == 0)
                {
                    card.Text([Text.BodySm, "text-tertiary"], text: "Nothing yet.");
                }
                foreach (var line in _payEventLog.Value.Take(20))
                {
                    card.Text([Text.BodySm], text: $"• {line}", props: TestId("pay-event-line"));
                }

                card.Button([Button.PrimarySm, "self-start mt-2"], text: "Reconcile", disabled: _payBusy.Value, onClick: () => RunPaymentsActionAsync(async () =>
                {
                    // The outcome reads naturally as an event-feed entry right above
                    // this button — no separate status banner needed.
                    var result = await app.Payments.ReconcileAsync(CustomerOverrideOrNull);
                    LogPayments(result.Enqueued == 0
                        ? "Reconcile found nothing new at the provider"
                        : $"Reconcile re-processed {result.Enqueued} record(s) from the provider");
                }));
            });

            if (_payReceipt.Value?.Pdf is { Length: > 0 } pdf)
            {
                col.ActionButton([Button.PrimarySm], action: ActionKind.DownloadFile,
                    options: new DownloadFileActionOptions { Filename = "receipt.pdf", Data = pdf },
                    content: v => v.Text([Text.BodySm], text: "Download receipt PDF"));
            }
        });
    }

    private async Task RequestReceiptAsync(string paymentId)
    {
        _payReceipt.Value = null;
        var receipt = await app.Payments.RequestReceiptAsync(paymentId);
        LogPayments($"Requested a receipt for {paymentId}");

        if (!string.IsNullOrEmpty(receipt.Url))
        {
            LogPayments("Opened the hosted receipt");
            await ClientFunctions.OpenExternalUrlAsync(receipt.Url);
        }
        else if (receipt.Pdf is { Length: > 0 })
        {
            _payReceipt.Value = receipt;
            LogPayments($"Receipt PDF ready ({receipt.Pdf.Length} bytes) — use the download button at the bottom");
        }
        else
        {
            LogPayments("No receipt is available for this payment");
        }
    }

    private Task PayAsync(string offerId) => RunPaymentsActionAsync(async () =>
    {
        long? amountMinorOverride = null;
        if (!string.IsNullOrWhiteSpace(_payPriceOverride.Value))
        {
            if (!TryParseAmountMinor(_payPriceOverride.Value, out var overrideMinor))
            {
                LogPayments("Error: enter a positive amount, e.g. 5.00");
                return;
            }
            amountMinorOverride = overrideMinor;
        }

        var link = await app.Payments.CreatePaymentLinkAsync(offerId, CustomerOverrideOrNull, amountMinorOverride: amountMinorOverride, allowPromotionCodes: _payAllowPromo.Value);
        LogPayments($"Opened a payment page for '{offerId}'" + (amountMinorOverride is { } o ? $" at {o / 100.0:0.00} override" : "") + (_payAllowPromo.Value ? " with promo codes enabled" : ""));
        await OpenLinkAsync(link);
    });

    private async Task RefreshOffersAsync()
    {
        _payOffers.Value = await app.Payments.ListOffersAsync();
        _payOffersLoaded.Value = true;
    }

    private async Task CreateValidationOffersAsync()
    {
        await app.Payments.CreateOfferAsync(new OfferSpec("validation_one_time", "Validation One-Time",
            new OfferPriceSpec(500, "eur", PriceKind.OneTime)));
        await app.Payments.CreateOfferAsync(new OfferSpec("validation_subscription", "Validation Subscription",
            new OfferPriceSpec(999, "eur", PriceKind.Recurring, PriceInterval.Month)));
        await RefreshOffersAsync();
        LogPayments("Created offers: validation_one_time (5.00 EUR one-time) + validation_subscription (9.99 EUR / month)");
    }

    private static void RenderOfferFact(UIView view, string label, string value)
    {
        view.Row(["gap-1 items-baseline min-w-0"], content: fact =>
        {
            fact.Text([Text.Caption, "text-tertiary"], text: $"{label}:");
            fact.Text([Text.Caption, "min-w-0 break-all"], text: value);
        });
    }

    private static string IntervalLabel(PaymentPrice price)
        => price.Interval == PriceInterval.Unknown ? "period" : price.Interval.ToString().ToLowerInvariant();

    // Records reference offers by id; the loaded catalog turns that into the
    // human name when the offer still exists.
    private string? OfferLabel(string? offerId)
    {
        if (string.IsNullOrEmpty(offerId))
        {
            return null;
        }

        return _payOffers.Value.FirstOrDefault(o => o.OfferId == offerId)?.Name ?? offerId;
    }

    private async Task CreateOfferAsync()
    {
        var offerId = _payNewOfferId.Value.Trim();
        var name = _payNewOfferName.Value.Trim();
        if (offerId.Length == 0 || name.Length == 0 || !TryParseAmountMinor(_payNewOfferPrice.Value, out var amountMinor))
        {
            LogPayments("Error: enter an offer id, a name, and a positive price, e.g. 9.99");
            return;
        }

        var recurring = _payNewOfferKind.Value == "month";
        await app.Payments.CreateOfferAsync(new OfferSpec(offerId, name,
            recurring
                ? new OfferPriceSpec(amountMinor, _payNewOfferCurrency.Value, PriceKind.Recurring, PriceInterval.Month)
                : new OfferPriceSpec(amountMinor, _payNewOfferCurrency.Value, PriceKind.OneTime)));
        await RefreshOffersAsync();
        LogPayments($"Created offer '{offerId}' — {(recurring ? "monthly subscription" : "one-time")}");
    }

    // Feeds a synthetic Stripe subscription event through the real webhook pipeline
    // (normalize → entitlement upsert → app push) so expiry handling can be exercised
    // without waiting for a real billing period to lapse.
    private async Task SimulateSubscriptionAccessAsync(bool expired)
    {
        var customer = SimulationCustomer;
        var offerId = _payOfferId.Value;
        // The backend adds a two-day grace window on top of the period end, so an
        // expired grant must sit further back than that.
        var periodEnd = DateTimeOffset.UtcNow.AddDays(expired ? -3 : 30);
        // The event is Stripe-shaped, so route it to the Stripe normalizer explicitly —
        // the space merchant's provider (e.g. Surfboard) would silently drop it.
        var providerEvent = new
        {
            id = $"evt_validation_expiry_{Guid.NewGuid():N}",
            type = "customer.subscription.updated",
            provider = "stripe",
            data = new
            {
                @object = new
                {
                    id = $"{SimulatedSubscriptionPrefix}{customer}",
                    status = "active",
                    cancel_at_period_end = false,
                    current_period_start = periodEnd.AddMonths(-1).ToUnixTimeSeconds(),
                    current_period_end = periodEnd.ToUnixTimeSeconds(),
                    metadata = new Dictionary<string, string>
                    {
                        ["app_customer_key"] = customer,
                        ["feature_key"] = offerId,
                    },
                },
            },
        };
        await IkonBackend.Instance.IngestPaymentsProviderEventAsync(JsonSerializer.Serialize(providerEvent));
        _payEntitlement.Value = "";
        LogPayments(expired
            ? $"Simulated an expired subscription grant for '{offerId}'"
            : $"Simulated an active subscription grant for '{offerId}' until {periodEnd:yyyy-MM-dd}");
    }

    // The counterpart to the grants above: a synthetic cancellation for the same
    // subscription, so the lifecycle ends cleanly. Without this, every simulated
    // grant leaves an "active" subscription with a past period end behind, which the
    // backend's stale-subscription reconcile sweep then re-pulls forever.
    private async Task SimulateSubscriptionRevokedAsync(string customer)
    {
        var offerId = _payOfferId.Value;
        var providerEvent = new
        {
            id = $"evt_validation_expiry_{Guid.NewGuid():N}",
            type = "customer.subscription.deleted",
            provider = "stripe",
            data = new
            {
                @object = new
                {
                    id = $"{SimulatedSubscriptionPrefix}{customer}",
                    status = "canceled",
                    metadata = new Dictionary<string, string>
                    {
                        ["app_customer_key"] = customer,
                        ["feature_key"] = offerId,
                    },
                },
            },
        };
        await IkonBackend.Instance.IngestPaymentsProviderEventAsync(JsonSerializer.Serialize(providerEvent));
        _payEntitlement.Value = "";
        LogPayments($"Simulated a subscription cancellation for '{offerId}'");
    }

    private async Task OpenLinkAsync(PaymentLink link)
    {
        if (!string.IsNullOrEmpty(link.Url))
        {
            await ClientFunctions.OpenExternalUrlAsync(link.Url);
        }
        else
        {
            LogPayments($"Payment created ({link.Reference}) but no redirect URL was returned");
        }
    }

    private async Task ReloadCustomerAsync()
    {
        _paySubs.Value = await app.Payments.ListSubscriptionsAsync(CustomerOverrideOrNull);
        _payHistory.Value = await app.Payments.ListPaymentsAsync(CustomerOverrideOrNull);
        _payCustomerLoaded.Value = true;
    }

    private static readonly IReadOnlyList<SelectOption> CurrencyOptions =
    [
        new SelectOption("eur", "EUR"),
        new SelectOption("usd", "USD"),
        new SelectOption("gbp", "GBP"),
        new SelectOption("sek", "SEK"),
    ];

    // Amounts are typed in major units ("5.00"); providers charge in minor units.
    private static bool TryParseAmountMinor(string text, out long amountMinor)
    {
        amountMinor = 0;
        if (!decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var major) || major <= 0)
        {
            return false;
        }
        amountMinor = (long)Math.Round(major * 100m, MidpointRounding.AwayFromZero);
        return amountMinor > 0;
    }

    private async Task RunPaymentsActionAsync(Func<Task> action)
    {
        _payBusy.Value = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            LogPayments($"Error: {ex.Message}");
        }
        finally
        {
            _payBusy.Value = false;
        }
    }

    private void LogPayments(string line) => _payEventLog.Insert(0, line);
}
