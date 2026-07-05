using System;
using System.Linq;
using System.Text.Json;
using System.Threading;

public partial class Validation
{
    private readonly Reactive<string> _payCustomer = new("demo-customer");
    private readonly Reactive<string> _payProvider = new("stripe");
    private readonly Reactive<string> _payOfferId = new("pro");
    private readonly Reactive<string> _payAmount = new("500");
    private readonly Reactive<string> _payCurrency = new("eur");

    private readonly Reactive<IReadOnlyList<PaymentOffer>> _payOffers = new([]);
    private readonly Reactive<bool> _payOffersLoaded = new(false);
    private readonly Reactive<IReadOnlyList<PaymentSubscription>> _paySubs = new([]);
    private readonly Reactive<IReadOnlyList<Payment>> _payHistory = new([]);
    private readonly Reactive<bool> _payCustomerLoaded = new(false);

    private readonly Reactive<string> _payEntitlement = new("");
    private readonly Reactive<string> _payGate = new("");
    private readonly Reactive<string> _payStatus = new("");
    private readonly Reactive<List<string>> _payEventLog = new([]);
    private readonly Reactive<PaymentReceipt?> _payReceipt = new((PaymentReceipt?)null);

    private async Task InitPaymentsAsync()
    {
        // Default to Stripe; the selector below repins per request to test a
        // multi-provider space. Leaving this null would fall back to the space's
        // default merchant, which isn't necessarily the one you want to validate.
        // Redirects default to this app's own URL — no need to set DefaultSuccessUrl/DefaultCancelUrl.
        app.Payments.DefaultProvider = PaymentProvider.Stripe;
        app.Payments.PaymentEventReceived += OnPaymentEventAsync;
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

    private void RenderPaymentsSection(UIView view)
    {
        if (RenderSectionLocked(view, "Payments"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: col =>
        {
            col.Box([Card.Default, "p-6"], content: hdr =>
            {
                hdr.Text([Text.H2, "mb-1"], text: "Payments");
                hdr.Text([Text.BodySm, "text-tertiary"], text: "A walkthrough of everything app.Payments can do. Follow it top to bottom — the backend drives the provider and pushes events; your app just sends commands and reacts.");
            });

            // Who's paying ------------------------------------------------
            col.Box([Card.Default, "p-6"], content: card =>
            {
                card.Text([Text.H3, "mb-1"], text: "Who's paying");
                card.Text([Text.BodySm, "text-tertiary mb-3"], text: "Pick any stable id for the customer you're testing with. The provider defaults to your app's; switch it only if you've set up more than one.");
                card.Row(["gap-3 flex-wrap items-end"], content: row =>
                {
                    row.TextField(bind: _payCustomer, label: "Customer", style: [Input.Default, "min-w-[200px]"]);
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
            col.Box([Card.Default, "p-6"], content: card =>
            {
                card.Text([Text.H3, "mb-1"], text: "1 · What can I sell?");
                card.Text([Text.BodySm, "text-tertiary mb-3"], text: "Offers are your catalog — create them from code with CreateOfferAsync (no provider dashboard) or load ones already synced. Click an offer to take a payment: a recurring offer starts a subscription, a one-time offer is a single charge.");
                card.Row([Layout.Row.Sm, "mb-3 flex-wrap"], content: btns =>
                {
                    btns.Button([Button.PrimaryMd], label: "Create sample offers", onClick: () => RunPaymentsActionAsync(CreateSampleOffersAsync));
                    btns.Button([Button.OutlineMd], label: "Load offers", onClick: () => RunPaymentsActionAsync(async () =>
                    {
                        _payOffers.Value = await app.Payments.ListOffersAsync();
                        _payOffersLoaded.Value = true;
                    }));
                });

                if (_payOffersLoaded.Value && _payOffers.Value.Count == 0)
                {
                    card.Text([Text.BodySm, "text-tertiary"], text: "No offers found. Create one at your provider, then reload.");
                }

                foreach (var offer in _payOffers.Value)
                {
                    var offerId = offer.OfferId;
                    card.Row([Card.Default, "p-3 mb-2 items-center justify-between"], key: $"offer-{offerId}", content: r =>
                    {
                        r.Column([Layout.Column.Xs], content: c =>
                        {
                            c.Text([Text.Body, "font-medium"], text: offer.Name);
                            c.Text([Text.Caption], text: $"{offerId} · {PriceLabel(offer)}");
                        });
                        r.Button([Button.PrimarySm], label: "Take payment", onClick: () => PayAsync(offerId));
                    });
                }
            });

            // 2. Custom amount --------------------------------------------
            col.Box([Card.Default, "p-6"], content: card =>
            {
                card.Text([Text.H3, "mb-1"], text: "2 · Or charge a custom amount");
                card.Text([Text.BodySm, "text-tertiary mb-3"], text: "For tips, donations, or anything not in your catalog.");
                card.Row(["gap-3 flex-wrap items-end"], content: row =>
                {
                    row.TextField(bind: _payAmount, label: "Amount (cents)", style: [Input.Default, "w-32"]);
                    row.TextField(bind: _payCurrency, label: "Currency", style: [Input.Default, "w-24"]);
                    row.Button([Button.OutlineMd], label: "Charge", onClick: () => RunPaymentsActionAsync(async () =>
                    {
                        if (!long.TryParse(_payAmount.Value, out var amount) || amount <= 0)
                        {
                            _payStatus.Value = "Enter a positive amount in cents.";
                            return;
                        }
                        var link = await app.Payments.CreatePaymentLinkAsync(amount, _payCurrency.Value, _payCustomer.Value, description: "Validation charge");
                        LogPayments($"Opened a payment page for {amount / 100.0:0.00} {_payCurrency.Value.ToUpperInvariant()}");
                        await OpenLinkAsync(link);
                    }));
                });
            });

            // 3. What happened --------------------------------------------
            col.Box([Card.Default, "p-6"], content: card =>
            {
                card.Text([Text.H3, "mb-1"], text: "3 · What just happened?");
                card.Text([Text.BodySm, "text-tertiary mb-3"], text: "When a customer finishes paying, the backend pushes a normalized event to your app. No webhook to host. Complete a sandbox payment above to see one arrive.");
                if (_payEventLog.Value.Count == 0)
                {
                    card.Text([Text.BodySm, "text-tertiary"], text: "Nothing yet.");
                }
                foreach (var line in _payEventLog.Value.Take(20))
                {
                    card.Text([Text.BodySm], text: $"• {line}");
                }
            });

            // 4. Does this customer have access ---------------------------
            col.Box([Card.Default, "p-6"], content: card =>
            {
                card.Text([Text.H3, "mb-1"], text: "4 · Can this customer use it?");
                card.Text([Text.BodySm, "text-tertiary mb-3"], text: "GetEntitlementAsync is a backend call → access + Source (subscription | one-time). For gating UI, read IsEntitled synchronously in render (no backend call) — shown live below and it re-renders when access changes.");
                card.Row(["gap-3 flex-wrap items-end"], content: row =>
                {
                    row.TextField(bind: _payOfferId, label: "Offer", style: [Input.Default, "w-32"]);
                    row.Button([Button.OutlineMd], label: "Check access", onClick: () => RunPaymentsActionAsync(async () =>
                    {
                        var e = await app.Payments.GetEntitlementAsync(_payOfferId.Value, _payCustomer.Value);
                        _payEntitlement.Value = e.Active
                            ? $"✓ Access to '{_payOfferId.Value}' (via {e.Source})" + (e.ExpiresAt is { } d ? $" — until {d:yyyy-MM-dd}" : "")
                            : $"✗ No access to '{_payOfferId.Value}'" + (e.ExpiresAt is { } x && x < DateTimeOffset.UtcNow ? $" — expired {x:yyyy-MM-dd}" : "");
                    }));
                });
                if (!string.IsNullOrEmpty(_payEntitlement.Value))
                {
                    card.Text([Text.Body, "mt-2"], text: _payEntitlement.Value);
                }

                // Synchronous, cached gate — safe to read every render, re-renders on change.
                var entitledNow = app.Payments.IsEntitled(_payOfferId.Value, _payCustomer.Value);
                card.Text([Text.BodySm, entitledNow ? "text-success" : "text-tertiary", "mt-2 font-mono"],
                    text: (entitledNow ? "✓" : "✗") + $" IsEntitled(\"{_payOfferId.Value}\")");

                card.Text([Text.Label, "mt-4 mb-1"], text: "Test expiry");
                card.Text([Text.BodySm, "text-tertiary mb-2"], text: "Grant this customer subscription access whose expiry is already past or a month away — a synthetic provider event run through the real webhook pipeline. Access past its expiry counts as inactive, so re-check above once the event lands.");
                card.Row([Layout.Row.Sm, "flex-wrap"], content: btns =>
                {
                    btns.Button([Button.OutlineMd], label: "Grant expired access", onClick: () => RunPaymentsActionAsync(() => SimulateSubscriptionAccessAsync(expired: true)));
                    btns.Button([Button.OutlineMd], label: "Grant active access", onClick: () => RunPaymentsActionAsync(() => SimulateSubscriptionAccessAsync(expired: false)));
                });
            });

            // 5. The customer's stuff -------------------------------------
            col.Box([Card.Default, "p-6"], content: card =>
            {
                card.Text([Text.H3, "mb-1"], text: "5 · This customer's subscriptions & payments");
                card.Text([Text.BodySm, "text-tertiary mb-3"], text: "What you'd show on a billing page — with cancel and refund actions.");
                card.Button([Button.OutlineMd, "mb-3"], label: "Load", onClick: () => RunPaymentsActionAsync(ReloadCustomerAsync));

                if (_payCustomerLoaded.Value)
                {
                    card.Text([Text.Label, "mt-2 mb-1"], text: "Subscriptions");
                    if (_paySubs.Value.Count == 0)
                    {
                        card.Text([Text.BodySm, "text-tertiary"], text: "None.");
                    }
                    foreach (var sub in _paySubs.Value)
                    {
                        var id = sub.Id;
                        card.Row([Card.Default, "p-3 mb-2 items-center justify-between"], key: $"sub-{id}", content: r =>
                        {
                            r.Column([Layout.Column.Xs], content: c =>
                            {
                                c.Text([Text.Body, "font-medium"], text: sub.OfferId ?? id);
                                c.Text([Text.Caption], text: $"{sub.Status}" + (sub.CurrentPeriodEnd is { } d ? $" · {(sub.CancelAtPeriodEnd ? "ends" : "renews")} {d:yyyy-MM-dd}" : ""));
                            });
                            r.Row([Layout.Row.Xs], content: actions =>
                            {
                                actions.Button([Button.GhostSm], label: "Cancel at period end", onClick: () => RunPaymentsActionAsync(async () =>
                                {
                                    await app.Payments.CancelSubscriptionAsync(id);
                                    _payStatus.Value = "Canceled — stays active until the period ends.";
                                    await ReloadCustomerAsync();
                                }));
                                actions.Button([Button.GhostSm], label: "Cancel now", onClick: () => RunPaymentsActionAsync(async () =>
                                {
                                    await app.Payments.CancelSubscriptionAsync(id, immediate: true);
                                    _payStatus.Value = "Canceled immediately — access ends now.";
                                    await ReloadCustomerAsync();
                                }));
                            });
                        });
                    }

                    card.Text([Text.Label, "mt-3 mb-1"], text: "Payments");
                    if (_payHistory.Value.Count == 0)
                    {
                        card.Text([Text.BodySm, "text-tertiary"], text: "None.");
                    }
                    foreach (var pay in _payHistory.Value)
                    {
                        var id = pay.Id;
                        card.Row([Card.Default, "p-3 mb-2 items-center justify-between"], key: $"pay-{id}", content: r =>
                        {
                            r.Column([Layout.Column.Xs], content: c =>
                            {
                                c.Text([Text.Body, "font-medium"], text: $"{pay.AmountMinor / 100.0:0.00} {pay.Currency.ToUpperInvariant()}");
                                c.Text([Text.Caption], text: $"{pay.Status}" + (pay.CreatedAt is { } d ? $" · {d:yyyy-MM-dd}" : ""));
                            });
                            r.Row([Layout.Row.Xs], content: actions =>
                            {
                                actions.Button([Button.GhostSm], label: "Receipt", onClick: () => RunPaymentsActionAsync(() => RequestReceiptAsync(id)));
                                actions.Button([Button.GhostSm], label: "Refund", onClick: () => RunPaymentsActionAsync(async () =>
                                {
                                    var refund = await app.Payments.RefundAsync(id);
                                    _payStatus.Value = $"Refund {refund.Status}.";
                                    await ReloadCustomerAsync();
                                }));
                            });
                        });
                    }
                }
            });

            // 6. Gate a feature -------------------------------------------
            col.Box([Card.Default, "p-6"], content: card =>
            {
                card.Text([Text.H3, "mb-1"], text: "6 · Gate a feature on an entitlement");
                card.Text([Text.BodySm, "text-tertiary mb-3"], text: "Put [PaymentsRequireEntitlement(\"pro\")] on a server function and the call is blocked unless the caller has access (via a subscription or a one-time purchase). The button below evaluates that check for the customer above.");
                card.Button([Button.OutlineMd], label: "Evaluate the gate", onClick: () => RunPaymentsActionAsync(async () =>
                {
                    var policy = new PaymentsRequireEntitlementAttribute(_payOfferId.Value).CreatePolicy();
                    var ctx = new PolicyCallContext(Guid.NewGuid(), "validation-demo", 0, _payCustomer.Value, null, null, true, CancellationToken.None);
                    var decision = await policy.EvaluateAsync([], ctx);
                    _payGate.Value = decision is PolicyDecision.Deny
                        ? $"Blocked — the customer has no active '{_payOfferId.Value}' entitlement."
                        : "Allowed — the call would run.";
                }));
                if (!string.IsNullOrEmpty(_payGate.Value))
                {
                    card.Text([Text.Body, "mt-2"], text: _payGate.Value);
                }
            });

            if (!string.IsNullOrEmpty(_payStatus.Value))
            {
                col.Text([Text.BodySm, "text-tertiary"], text: _payStatus.Value);
            }

            if (_payReceipt.Value?.Pdf is { Length: > 0 } pdf)
            {
                col.ActionButton([Button.OutlineSm], action: ActionKind.DownloadFile,
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
            _payStatus.Value = "Receipt ready — opening the hosted receipt.";
            await ClientFunctions.OpenExternalUrlAsync(receipt.Url);
        }
        else if (receipt.Pdf is { Length: > 0 })
        {
            _payReceipt.Value = receipt;
            _payStatus.Value = $"Receipt PDF ready ({receipt.Pdf.Length} bytes) — use the download button below.";
        }
        else
        {
            _payStatus.Value = "No receipt is available for this payment.";
        }
    }

    private Task PayAsync(string offerId) => RunPaymentsActionAsync(async () =>
    {
        var link = await app.Payments.CreatePaymentLinkAsync(offerId, _payCustomer.Value, "demo@ikon.live");
        LogPayments($"Opened a payment page for '{offerId}'");
        await OpenLinkAsync(link);
    });

    private async Task CreateSampleOffersAsync()
    {
        // Provision a catalog straight from code — no provider dashboard.
        await app.Payments.CreateOfferAsync(new OfferSpec("validation_pro", "Validation Pro",
            new OfferPriceSpec(999, "eur", PriceKind.Recurring, PriceInterval.Month)));
        await app.Payments.CreateOfferAsync(new OfferSpec("validation_unlock", "Validation Unlock",
            new OfferPriceSpec(500, "eur", PriceKind.OneTime)));
        _payOffers.Value = await app.Payments.ListOffersAsync();
        _payOffersLoaded.Value = true;
        LogPayments("Created offers: validation_pro (subscription) + validation_unlock (one-time)");
    }

    // Feeds a synthetic Stripe subscription event through the real webhook pipeline
    // (normalize → entitlement upsert → app push) so expiry handling can be exercised
    // without waiting for a real billing period to lapse.
    private async Task SimulateSubscriptionAccessAsync(bool expired)
    {
        var customer = _payCustomer.Value;
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
                    id = $"sub_validation_expiry_{customer}",
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
        _payStatus.Value = expired
            ? $"Granted '{offerId}' access that has already expired — once the event lands, Check access should say no."
            : $"Granted '{offerId}' access until {periodEnd:yyyy-MM-dd} — once the event lands, Check access should say yes.";
        LogPayments(expired ? "Simulated an expired subscription grant" : "Simulated an active subscription grant");
    }

    private async Task OpenLinkAsync(PaymentLink link)
    {
        if (!string.IsNullOrEmpty(link.Url))
        {
            await ClientFunctions.OpenExternalUrlAsync(link.Url);
        }
        else
        {
            _payStatus.Value = $"Payment created ({link.Reference}) but no redirect URL was returned.";
        }
    }

    private async Task ReloadCustomerAsync()
    {
        _paySubs.Value = await app.Payments.ListSubscriptionsAsync(_payCustomer.Value);
        _payHistory.Value = await app.Payments.ListPaymentsAsync(_payCustomer.Value);
        _payCustomerLoaded.Value = true;
    }

    private static string PriceLabel(PaymentOffer offer)
    {
        var price = offer.Prices.FirstOrDefault();
        if (price is null)
        {
            return "no price";
        }
        var amount = $"{price.AmountMinor / 100.0:0.00} {price.Currency.ToUpperInvariant()}";
        var interval = price.Interval == PriceInterval.Unknown ? "period" : price.Interval.ToString().ToLowerInvariant();
        return price.Kind == PriceKind.Recurring ? $"{amount}/{interval}" : amount;
    }

    private async Task RunPaymentsActionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _payStatus.Value = $"Error: {ex.Message}";
            LogPayments($"Error: {ex.Message}");
        }
    }

    private void LogPayments(string line) => _payEventLog.Insert(0, line);
}
