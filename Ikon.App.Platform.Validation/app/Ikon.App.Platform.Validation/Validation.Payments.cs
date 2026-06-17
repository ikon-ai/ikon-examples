using System.Linq;

public partial class Validation
{

    private readonly Reactive<string> _paymentsSeatQty = new("1");

    private PaymentsService? ActivePayments => _payments;

    private string? ActiveCustomerId => _paymentsDemoCustomerId;

    private string ActiveCustomerKey => PaymentsDemoCustomerKey;

    private static readonly IReadOnlyList<PaymentsPlanView> ValidationPlans =
    [
        new(
            PlanId: PlanIdPro,
            Name: "Validation Pro",
            PriceLabel: "€19",
            IntervalLabel: "month",
            Features: ["Sandbox plan", "Subscription mode", "Promotion codes allowed"],
            Badge: "Sandbox",
            Highlighted: true),
        new(
            PlanId: PlanIdTeam,
            Name: "Validation Team",
            PriceLabel: "€49",
            IntervalLabel: "month",
            Features: ["Sandbox plan", "Subscription mode", "Higher seat ceiling"]),
    ];

    private void RenderPaymentsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: hdr =>
            {
                hdr.Text([Text.H2, "mb-1"], "Payments");
                hdr.Text([Text.BodySm, "text-tertiary mb-2"], "Live Stripe sandbox integration showing how an Ikon app wires Ikon.App.Payments on the v2 surface (Accounts v2 / Payments v2, API version 2026-04-22.dahlia).");
                hdr.Text([Text.BodySm, "text-tertiary"], "Admin tab = catalog + customer + invoicing ops.");
                hdr.Text([Text.BodySm, "text-tertiary mb-4"], "End-user tab = checkout + subscription self-service.");

                if (!string.IsNullOrEmpty(_paymentsError.Value))
                {
                    hdr.Box(["mt-2 p-3 rounded-md bg-error/10 border border-error/30 text-error text-sm"], content: e => e.Text([], _paymentsError.Value!));
                }
                else if (!_paymentsReady.Value)
                {
                    hdr.Box(["mt-2 p-3 rounded-md bg-info/10 border border-info/30 text-info text-sm"], content: e => e.Text([], "Initializing Stripe sandbox …"));
                }
            });

            if (!_paymentsReady.Value)
            {
                return;
            }

            if (_paymentsProviderValue == PaymentsProvider.Disabled)
            {
                return;
            }

            RenderBusyAndReadinessBanner(view);

            view.Box(["relative mt-2"], content: wrap =>
            {
                wrap.Tabs(
                    value: _paymentsTab.Value,
                    onValueChange: async v => { _paymentsTab.Value = string.IsNullOrEmpty(v) ? "end-user" : v; await Task.CompletedTask; },
                    listStyle: [Tabs.List],
                    triggerStyle: [Tabs.Trigger],
                    contentStyle: [Tabs.Content],
                    tabs:
                    [
                        new TabItem("admin", "Admin actions", view => RenderAdminTab(view)),
                        new TabItem("end-user", "End-user actions", view => RenderEndUserTab(view)),
                    ]);

                wrap.Box(["absolute right-0 top-0"], content: slot =>
                {
                    slot.Button(
                        style: [Button.OutlineSm, Button.IconLeft],
                        text: "Refresh data", content: row => { row.Icon([Icon.Xs], name: "refresh-cw"); row.Text([], "Refresh data"); },
                        onClick: async () =>
                        {
                            await RefreshPaymentsDataAsync();
                            _paymentsActionStatus.Value = "Refreshed customer data from Stripe.";
                        });
                });
            });

            RenderActionStatusSection(view);
        });
    }

    private static string FormatPaymentsError(Exception ex)
    {
        var body = ex is PaymentsApiException api ? api.ResponseBody : null;
        return string.IsNullOrEmpty(body) ? ex.Message : ex.Message + " · " + body;
    }

    /// <summary>
    /// Resolve a human-readable plan name for a subscription by matching its
    /// first item's price id against the projected catalog. Falls back to the
    /// product id (or "Subscription" placeholder) when no catalog entry
    /// matches — e.g. for subscriptions targeting products outside this app's
    /// filter.
    /// </summary>
    private string ResolvePlanName(PaymentsSubscription sub)
    {
        var catalog = _planCatalog.Value;

        if (catalog is not null && !string.IsNullOrEmpty(sub.FirstPriceId))
        {
            var match = catalog.Plans.FirstOrDefault(p => p.StripePriceId == sub.FirstPriceId);

            if (match is not null)
            {
                return match.ProductName;
            }
        }

        if (catalog is not null && !string.IsNullOrEmpty(sub.FirstProductId))
        {
            var match = catalog.Plans.FirstOrDefault(p => p.ProductId == sub.FirstProductId);

            if (match is not null)
            {
                return match.ProductName;
            }
        }

        return sub.FirstProductId ?? "Subscription";
    }

    /// <summary>
    /// Canonical busy flag for the payments tab — drives single-flight
    /// enforcement across every <see cref="GuardedButton"/>. Flipped via
    /// <see cref="ReactiveBoolExtensions.AsToken"/> so the bool can never
    /// get stuck on, even if the wrapped click body throws.
    /// </summary>
    private readonly Reactive<bool> _busy = new(false);

    /// <summary>
    /// Stable identifier of the click currently in flight (or null when
    /// idle). Pairs with <see cref="_busy"/> so the UI can show <i>which</i>
    /// button is running while the busy flag drives disable state.
    /// </summary>
    private readonly Reactive<string?> _busyActionName = new(null);

    /// <summary>
    /// Last error from a <see cref="RunActionAsync"/> call. Surfaced inline at
    /// the top of the payments tab in red.
    /// </summary>
    private readonly Reactive<string?> _lastActionError = new(null);

    /// <summary>
    /// Wraps any click body with: busy-state tracking, try/catch with typed
    /// error formatting, and single-flight enforcement. Every guarded button's
    /// onClick goes through this so the UI consistently reflects async state.
    /// </summary>
    private async Task RunActionAsync(string actionId, string statusOk, Func<Task> body)
    {
        if (_busy.Value)
        {
            return;
        }

        _lastActionError.Value = null;
        using var _ = _busy.AsToken();
        _busyActionName.Value = actionId;

        try
        {
            await body();
            _paymentsActionStatus.Value = statusOk;
        }
        catch (Exception ex)
        {
            var msg = $"{actionId} → {FormatPaymentsError(ex)}";
            _lastActionError.Value = msg;
            _paymentsActionStatus.Value = msg;
        }
        finally
        {
            _busyActionName.Value = null;
        }
    }

    /// <summary>
    /// Render a single button whose click runs <paramref name="action"/> through
    /// <see cref="RunActionAsync"/>. Disabled while ANY action is in flight (the
    /// clicked one OR any other guarded button across the tab). Shows a spinner
    /// in place of the label while busy.
    /// </summary>
    /// <param name="id">Stable identifier — also used as the busy-state key + the action-status prefix on error.</param>
    /// <param name="label">Button label shown when idle.</param>
    /// <param name="statusOk">Status banner shown after a successful run.</param>
    /// <param name="action">The async work to perform on click.</param>
    /// <param name="enabled">When false the button stays disabled regardless of busy state. Use for readiness gating.</param>
    /// <param name="style">Optional override for button style array. Defaults to <see cref="Button.OutlineSm"/>.</param>
    private void GuardedButton(
        UIView view,
        string id,
        string label,
        string statusOk,
        Func<Task> action,
        bool enabled = true,
        string[]? style = null)
    {
        var anyBusy = _busy.Value;
        var thisBusy = _busyActionName.Value == id;
        var disabled = !enabled || anyBusy;
        var displayLabel = thisBusy ? "Running …" : label;

        view.Button(
            style: style ?? [Button.OutlineSm],
            label: displayLabel,
            disabled: disabled,
            onClick: async () => await RunActionAsync(id, statusOk, action));
    }

    /// <summary>True when the payments surface is wired and an <see cref="ActivePayments"/> is available for API calls.</summary>
    private bool PaymentsReady => _paymentsReady.Value
        && _paymentsProviderValue != PaymentsProvider.Disabled
        && ActivePayments is not null;

    /// <summary>Render the busy banner (when an action is in flight) + readiness gate explanation.</summary>
    private void RenderBusyAndReadinessBanner(UIView view)
    {
        if (_busy.Value)
        {
            view.Row([Layout.Row.Sm, "items-center p-3 rounded-md bg-info/10 border border-info/30 text-info text-sm"], content: r =>
            {
                r.Spinner();
                r.Text(["ml-2"], $"Running: {_busyActionName.Value ?? "action"} …");
            });
        }

        if (!PaymentsReady)
        {
            view.Box(["mt-2 p-3 rounded-md bg-warning/10 border border-warning/30 text-warning text-sm"], content: b =>
            {
                if (_paymentsProviderValue == PaymentsProvider.IkonConnect
                    && _connectAccount.Value is { ChargesEnabled: false })
                {
                    b.Text([], "Payments actions disabled — complete Stripe Connect onboarding first. Status: " +
                        (_connectAccount.Value.RequirementsCurrentlyDue.Count > 0
                            ? $"{_connectAccount.Value.RequirementsCurrentlyDue.Count} requirements pending"
                            : "Awaiting capability activation"));
                }
                else
                {
                    b.Text([], "Payments not ready. See banner above.");
                }
            });
        }
    }

    private void RenderActionStatusSection(UIView view)
    {
        if (_lastActionError.Value is { } err)
        {
            view.Box(["p-3 rounded-md bg-error/10 border border-error/30 text-error text-sm font-mono break-all"], content: s =>
            {
                s.Text([], err);
            });
            return;
        }

        if (string.IsNullOrEmpty(_paymentsActionStatus.Value))
        {
            return;
        }

        view.Box(["p-3 rounded-md bg-info/10 border border-info/30 text-info text-sm font-mono break-all"], content: s =>
        {
            s.Text([], _paymentsActionStatus.Value!);
        });
    }


    private static readonly string[] SandboxKeyPrefixes = ["sk_test_", "rk_test_"];

    private static bool IsSandboxKey(string apiKey)
    {
        foreach (var prefix in SandboxKeyPrefixes)
        {
            if (apiKey.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private const string PaymentsDemoCustomerKey = "validation-demo-customer";

    private const string PlanIdPro = "validation-pro";
    private const string PlanIdTeam = "validation-team";

    private PaymentsService? _payments;
    private PaymentsProvider _paymentsProviderValue = PaymentsProvider.IkonConnect;
    private string? _paymentsDemoCustomerId;
    private string? _paymentsProPriceId;
    private string? _paymentsTeamPriceId;
    private string? _paymentsProProductId;
    private string? _paymentsTeamProductId;

    private StripeMerchantService? _connect;

    private readonly Reactive<bool> _paymentsReady = new(false);
    private readonly Reactive<string?> _paymentsError = new(null);
    private readonly Reactive<IReadOnlyList<string>> _paymentsEventLog = new([]);
    private readonly Reactive<string?> _paymentsActionStatus = new(null);
    private readonly Reactive<IReadOnlyList<PaymentsPaymentMethod>> _paymentsPaymentMethods = new([]);
    private readonly Reactive<IReadOnlyList<PaymentsInvoiceSummary>> _paymentsInvoices = new([]);
    private readonly Reactive<IReadOnlyList<PaymentsSubscription>> _paymentsSubscriptions = new([]);
    private readonly Reactive<IReadOnlyList<PaymentsCharge>> _paymentsCharges = new([]);
    private readonly Reactive<string?> _paymentsLastCheckoutUrl = new(null);

    private readonly Reactive<PaymentsUpcomingInvoice?> _upcomingInvoice = new(null);

    private readonly Reactive<IReadOnlyList<PaymentsProduct>> _adminListedProducts = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedCustomers = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedCoupons = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedPromoCodes = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedTaxIds = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedCreditNotes = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedWebhookEndpoints = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedPaymentLinks = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedApplePayDomains = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminRecentEventIds = new([]);

    private readonly ClientReactive<string> _paymentsTab = new("admin");

    private readonly ClientReactive<string> _adminPlanName = new("");
    private readonly ClientReactive<string> _adminPlanAmount = new("");
    private readonly ClientReactive<string> _adminPlanCurrency = new("eur");
    private readonly ClientReactive<string> _adminPlanInterval = new("month");
    private readonly ClientReactive<string> _adminPlanFeatures = new("");
    private readonly ClientReactive<string> _adminProductIdToArchive = new("");

    private readonly ClientReactive<string> _adminCustomerEmail = new("");
    private readonly ClientReactive<string> _adminCustomerName = new("");
    private readonly ClientReactive<string> _adminCustomerIdToUpdate = new("");
    private readonly ClientReactive<string> _adminTaxIdCustomer = new("");
    private readonly ClientReactive<string> _adminTaxIdType = new("eu_vat");
    private readonly ClientReactive<string> _adminTaxIdValue = new("");
    private readonly ClientReactive<string> _adminTaxIdToDelete = new("");

    private readonly ClientReactive<string> _adminCouponPercent = new("10");
    private readonly ClientReactive<string> _adminPromoCouponId = new("");
    private readonly ClientReactive<string> _adminPromoCode = new("");

    private readonly ClientReactive<string> _adminCreditInvoiceId = new("");
    private readonly ClientReactive<string> _adminCreditAmount = new("");
    private readonly ClientReactive<string> _adminCreditNoteIdToVoid = new("");

    private readonly ClientReactive<string> _adminWebhookUrl = new("");
    private readonly ClientReactive<string> _adminWebhookIdToDelete = new("");

    private readonly ClientReactive<string> _adminApplePayDomain = new("");
    private readonly ClientReactive<string> _adminPaymentLinkPriceId = new("");

    private readonly Reactive<PaymentsPlanCatalog?> _planCatalog = new(null);

    private IReadOnlyList<PaymentsPlanView> ActivePlans()
    {
        var catalog = _planCatalog.Value;

        if (catalog is null)
        {
            return Array.Empty<PaymentsPlanView>();
        }

        return catalog.Plans.Select(ToPlanView).ToList();
    }

    private static PaymentsPlanView ToPlanView(PaymentsPlanProjection p)
    {
        var priceLabel = FormatPlanPrice(p.UnitAmountMinor, p.Currency);
        var highlighted = p.ProductMetadata is not null
            && p.ProductMetadata.TryGetValue("highlighted", out var h)
            && string.Equals(h, "true", StringComparison.OrdinalIgnoreCase);
        var badge = p.ProductMetadata is not null && p.ProductMetadata.TryGetValue("badge", out var b) ? b : null;
        var features = p.MarketingFeatures is { Count: > 0 } ? p.MarketingFeatures : null;

        return new PaymentsPlanView(
            PlanId: p.PlanId,
            Name: p.ProductName,
            PriceLabel: priceLabel,
            IntervalLabel: p.RecurringInterval,
            Features: features,
            Badge: badge,
            Highlighted: highlighted);
    }

    private static string FormatPlanPrice(long minor, string currency)
    {
        var c = currency?.ToLowerInvariant() ?? "eur";
        var decimals = c switch
        {
            "bif" or "clp" or "djf" or "gnf" or "jpy" or "kmf" or "krw" or "mga"
                or "pyg" or "rwf" or "ugx" or "vnd" or "vuv" or "xaf" or "xof" or "xpf" => 0,
            "bhd" or "jod" or "kwd" or "omr" or "tnd" => 3,
            _ => 2,
        };
        var divisor = (decimal)Math.Pow(10, decimals);
        var major = minor / divisor;
        return $"{major.ToString($"0.{new string('0', decimals)}", System.Globalization.CultureInfo.InvariantCulture)} {c.ToUpperInvariant()}";
    }

    private readonly Reactive<string?> _connectAccountId = new(null);
    private readonly Reactive<StripeMerchantAccount?> _connectAccount = new(null);
    private readonly Reactive<string?> _connectError = new(null);
    private readonly Reactive<bool> _connectSettingUp = new(false);

    private readonly Reactive<string?> _connectOnboardingUrl = new(null);
    private readonly Reactive<string?> _connectDashboardUrl = new(null);

    private static string ResolveSpace() =>
        string.IsNullOrEmpty(IkonBackend.Instance.SpaceId) ? "validation" : IkonBackend.Instance.SpaceId;

    private async Task InitPaymentsAsync()
    {
        try
        {
            var autoOpts = PaymentsAppHelpers.AutoDetectFromApp(app, defaultSpaceId: "validation");
            _paymentsProviderValue = autoOpts.Provider;

            if (_paymentsProviderValue == PaymentsProvider.Disabled)
            {
                _paymentsError.Value = "Payments not configured. Set BILLING_PROVIDER=byok (+ STRIPE_API_KEY) or BILLING_PROVIDER=ikon-connect (+ IKON_BACKEND_BILLING_URL). IkonConnect mode reuses the app's standard Ikon backend session token — no separate IKON_APP_TOKEN required.";
                _paymentsReady.Value = true;
                return;
            }

            if (_paymentsProviderValue == PaymentsProvider.Byok)
            {
                if (string.IsNullOrEmpty(autoOpts.ApiKey))
                {
                    _paymentsError.Value = "BYOK mode: STRIPE_API_KEY not set. Add via `ikon app secret set STRIPE_API_KEY sk_test_...` (or env var). Validation only runs against sandbox keys (sk_test_ or rk_test_).";
                    return;
                }

                if (!IsSandboxKey(autoOpts.ApiKey))
                {
                    _paymentsError.Value = "STRIPE_API_KEY is not a sandbox key. Validation refuses to run against live Stripe. Use a secret key (sk_test_...) or restricted key (rk_test_...).";
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(autoOpts.IkonBackendUrl))
                {
                    _paymentsError.Value = "IkonConnect mode: IKON_BACKEND_BILLING_URL not set and no ambient Ikon backend URL available. Add via `ikon app secret set IKON_BACKEND_BILLING_URL https://backend.ikonai.live` (or env var). To switch to BYOK mode, set BILLING_PROVIDER=byok.";
                    return;
                }

                if (!IkonBackend.Instance.IsLoggedIn)
                {
                    _paymentsError.Value = "IkonConnect mode: no active Ikon backend session. Payments proxy reuses the standard Ikon backend token, but the app is not logged in.";
                    return;
                }
            }

            var opts = autoOpts with
            {
                DefaultSuccessUrl = "https://ikon.live/payments/success",
                DefaultCancelUrl = "https://ikon.live/payments/cancel",
                DefaultPortalReturnUrl = "https://ikon.live/payments/portal-return",
            };

            var adapter = new ValidationPaymentsAdapter(this);
            _payments = new PaymentsService(opts, adapter);

            _payments.PaymentReceived += OnValidationPaymentReceivedAsync;

            await BootstrapCatalogAsync();
            await RefreshCatalogAsync();
            await RefreshPaymentsDataAsync();

            _connect = new StripeMerchantService(opts);

            await BootstrapConnectAsync();

            _paymentsReady.Value = true;
            Log.Instance.Info($"[payments] sandbox ready · provider={_paymentsProviderValue} · pro={_paymentsProPriceId} team={_paymentsTeamPriceId} customer={_paymentsDemoCustomerId}");
        }
        catch (Exception ex)
        {
            _paymentsError.Value = $"Payments init failed: {ex.Message}";
            Log.Instance.Error($"[payments] init failed: {ex}");
        }
    }

    private async Task BootstrapCatalogAsync()
    {
        var products = await _payments!.ListProductsAsync(activeOnly: true, limit: 100);
        PaymentsProduct? pro = products.FirstOrDefault(p => p.Name == "Validation Pro");
        PaymentsProduct? team = products.FirstOrDefault(p => p.Name == "Validation Team");

        _paymentsProProductId = pro?.Id ?? await _payments.CreateProductAsync(new PaymentsProductInfo
        {
            Name = "Validation Pro",
            Description = "Sandbox plan exercised by Ikon validation app — safe to delete.",
            Metadata = new Dictionary<string, string> { ["validation"] = "true" },
        });
        _paymentsTeamProductId = team?.Id ?? await _payments.CreateProductAsync(new PaymentsProductInfo
        {
            Name = "Validation Team",
            Description = "Sandbox plan exercised by Ikon validation app — safe to delete.",
            Metadata = new Dictionary<string, string> { ["validation"] = "true" },
        });

        await BackfillValidationMetadataAsync(pro);
        await BackfillValidationMetadataAsync(team);

        _paymentsProPriceId = await EnsureRecurringPriceAsync(_paymentsProProductId, 1900, "eur", "month");
        _paymentsTeamPriceId = await EnsureRecurringPriceAsync(_paymentsTeamProductId, 4900, "eur", "month");

        _paymentsDemoCustomerId = await new ValidationPaymentsAdapter(this)
            .ResolveStripeCustomerIdAsync(PaymentsDemoCustomerKey, "validation@ikon.live", CancellationToken.None);
    }

    private async Task<string> EnsureRecurringPriceAsync(string productId, long amountMinor, string currency, string interval)
    {
        var existing = await _payments!.ListPricesAsync(productId: productId, activeOnly: true, limit: 100);
        var match = existing.FirstOrDefault(p =>
            p.UnitAmountMinor == amountMinor &&
            string.Equals(p.Currency, currency, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.RecurringInterval, interval, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            return match.Id;
        }

        return await _payments.CreatePriceAsync(new PaymentsPriceInfo
        {
            ProductId = productId,
            UnitAmountMinor = amountMinor,
            Currency = currency,
            RecurringInterval = interval,
        });
    }

    private int _refreshInFlight;

    private async Task RefreshCatalogAsync(PaymentsService? service = null)
    {
        service ??= _payments;

        if (service is null)
        {
            return;
        }

        try
        {
            var projector = new PaymentsCatalogProjector(service);
            var catalog = await projector.ProjectAsync(
                productFilter: p => p.Metadata is not null
                    && p.Metadata.TryGetValue("validation", out var v)
                    && string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));
            _planCatalog.Value = catalog;
        }
        catch (Exception ex)
        {
            Ikon.Common.Core.Log.Instance.Warning($"[validation-payments] RefreshCatalogAsync failed: {ex.Message}");
        }
    }

    private async Task BackfillValidationMetadataAsync(PaymentsProduct? product)
    {
        if (product is null)
        {
            return;
        }

        if (product.Metadata is not null
            && product.Metadata.TryGetValue("validation", out var v)
            && string.Equals(v, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var merged = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["validation"] = "true",
            };

            if (product.Metadata is not null)
            {
                foreach (var kv in product.Metadata)
                {
                    merged[kv.Key] = kv.Value;
                }
            }

            await _payments!.UpdateProductAsync(product.Id, metadata: merged);
        }
        catch (Exception ex)
        {
            Ikon.Common.Core.Log.Instance.Warning($"[validation-payments] backfill validation metadata on {product.Id} failed: {ex.Message}");
        }
    }

    private async Task RefreshPaymentsDataAsync()
    {
        if (Interlocked.CompareExchange(ref _refreshInFlight, 1, 0) != 0)
        {
            return;
        }

        try
        {
            var service = _payments;
            var customerId = _paymentsDemoCustomerId;

            if (service is null || string.IsNullOrEmpty(customerId))
            {
                return;
            }

            try
            {
                var subsTask = service.ListSubscriptionsAsync(stripeCustomerId: customerId, status: "all");
                var pmsTask = service.ListPaymentMethodsAsync(customerId, type: "card");
                var invTask = service.ListInvoicesAsync(stripeCustomerId: customerId, limit: 25);
                var chargesTask = service.ListChargesAsync(stripeCustomerId: customerId, limit: 25);

                await Task.WhenAll(subsTask, pmsTask, invTask, chargesTask);

                _paymentsSubscriptions.Value = subsTask.Result;
                _paymentsPaymentMethods.Value = pmsTask.Result;
                _paymentsInvoices.Value = invTask.Result;
                _paymentsCharges.Value = chargesTask.Result;
            }
            catch (Exception ex)
            {
                _paymentsActionStatus.Value = $"Refresh failed: {ex.Message}";
                Log.Instance.Warning($"[payments] refresh failed: {ex.Message}");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _refreshInFlight, 0);
        }
    }

    private PaymentsProvider AutoDetectProvider()
    {
        if (!string.IsNullOrEmpty(TryGetPaymentsSecret("STRIPE_API_KEY")))
        {
            return PaymentsProvider.Byok;
        }

        var hasPaymentsUrl = !string.IsNullOrEmpty(TryGetPaymentsSecret("IKON_BACKEND_BILLING_URL"))
            || !string.IsNullOrEmpty(IkonBackend.Instance.Url);

        if (hasPaymentsUrl && IkonBackend.Instance.IsLoggedIn)
        {
            return PaymentsProvider.IkonConnect;
        }

        return PaymentsProvider.Disabled;
    }

    private string? TryGetPaymentsSecret(string key)
    {
        try
        {
            if (app.Secrets.TryGet(key, out var v) && !string.IsNullOrEmpty(v))
            {
                return v;
            }
        }
        catch
        {
        }

        return Environment.GetEnvironmentVariable(key);
    }

    private string ResolvePaymentsWebhookUrl()
    {
        var hook = app.Endpoints.FirstOrDefault(w =>
            string.Equals(w.FunctionName, "Validation_StripeWebhook", StringComparison.OrdinalIgnoreCase));
        return hook?.PublicUrl ?? "(webhook URL unavailable — start the app to register endpoints)";
    }

    private string ResolveConnectWebhookUrl()
    {
        var hook = app.Endpoints.FirstOrDefault(w =>
            string.Equals(w.FunctionName, "Validation_StripeConnectWebhook", StringComparison.OrdinalIgnoreCase));
        return hook?.PublicUrl ?? "(webhook URL unavailable — start the app to register endpoints)";
    }

    [HttpPost("/billing/stripe")]
    public async Task<string> StripeWebhook(Ikon.App.HttpRequest req)
    {
        var headers = req.Headers;
        var body = req.Body;
        if (_payments is null)
        {
            return """{"received":true,"reason":"payments not initialized"}""";
        }

        headers.TryGetValue("Stripe-Signature", out var signature);

        var result = await _payments.HandleWebhookAsync(signature, body);

        if (!result.Verified)
        {
            Log.Instance.Warning($"[payments] platform webhook unverified: {result.Reason}");
        }
        else if (result.AdapterError is not null)
        {
            Log.Instance.Warning($"[payments] platform webhook adapter error: {result.AdapterError}");
        }
        else if (result.BackendIngestError is not null)
        {
            Log.Instance.Warning($"[payments] platform webhook backend ingest error: {result.BackendIngestError}");
        }

        return """{"received":true}""";
    }

    [HttpPost("/billing/stripe-connect")]
    public async Task<string> StripeConnectWebhook(Ikon.App.HttpRequest req)
    {
        var headers = req.Headers;
        var body = req.Body;
        if (_payments is null)
        {
            return """{"received":true,"reason":"payments not initialized"}""";
        }

        headers.TryGetValue("Stripe-Signature", out var signature);
        var result = await _payments.HandleWebhookAsync(signature, body);

        if (!result.Verified)
        {
            Log.Instance.Warning($"[payments] connect webhook unverified: {result.Reason}");
        }
        else if (result.AdapterError is not null)
        {
            Log.Instance.Warning($"[payments] connect webhook adapter error: {result.AdapterError}");
        }
        else if (result.BackendIngestError is not null)
        {
            Log.Instance.Warning($"[payments] connect webhook backend ingest error: {result.BackendIngestError}");
        }

        return """{"received":true}""";
    }

    private Task OnValidationPaymentReceivedAsync(PaymentsPushEvent evt)
    {
        LogPaymentsEvent($"push  {evt.Type}  provider={evt.Provider} seq={evt.Sequence} id={evt.EventId}");
        return Task.CompletedTask;
    }

    private async Task RunBackendCheckoutAsync(string planId)
    {
        if (_payments is null)
        {
            return;
        }

        try
        {
            _paymentsActionStatus.Value = $"Backend RPC checkout ({planId})…";
            var resultJson = await _payments.CreateBackendCheckoutAsync(
                planId,
                ActiveCustomerKey,
                email: null,
                successUrl: "https://ikon.live/payments/success",
                cancelUrl: "https://ikon.live/payments/cancel");

            LogPaymentsEvent($"rpc   create_checkout planId={planId} -> {resultJson}");

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(resultJson);
                if (doc.RootElement.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    _paymentsLastCheckoutUrl.Value = urlEl.GetString();
                }
            }
            catch (System.Text.Json.JsonException)
            {
            }

            _paymentsActionStatus.Value = $"Backend RPC checkout ({planId}) ok";
        }
        catch (PaymentsNotSupportedException ex)
        {
            _paymentsActionStatus.Value = $"Backend RPC checkout not supported: {ex.Message}";
            LogPaymentsEvent($"rpc   create_checkout not_supported: {ex.Message}");
        }
        catch (Exception ex)
        {
            _paymentsActionStatus.Value = $"Backend RPC checkout failed: {ex.Message}";
            LogPaymentsEvent($"rpc   create_checkout error: {ex.Message}");
        }
    }

    private async Task RunBackendEntitlementAsync(string featureKey)
    {
        if (_payments is null)
        {
            return;
        }

        try
        {
            var resultJson = await _payments.GetBackendEntitlementAsync(featureKey, ActiveCustomerKey);
            LogPaymentsEvent($"rpc   get_entitlement {featureKey} -> {resultJson}");
            _paymentsActionStatus.Value = $"Backend entitlement ({featureKey}) read";
        }
        catch (Exception ex)
        {
            _paymentsActionStatus.Value = $"Backend entitlement failed: {ex.Message}";
            LogPaymentsEvent($"rpc   get_entitlement error: {ex.Message}");
        }
    }

    private readonly object _paymentsEventLogLock = new();

    private void LogPaymentsEvent(string line)
    {
        lock (_paymentsEventLogLock)
        {
            var current = _paymentsEventLog.Value.ToList();
            current.Insert(0, $"{DateTime.UtcNow:HH:mm:ss}  {line}");

            if (current.Count > 50)
            {
                current.RemoveAt(current.Count - 1);
            }

            _paymentsEventLog.Value = current;
        }
    }

    private sealed class ValidationPaymentsAdapter(Validation owner) : IPaymentsAppAdapter
    {
        public Task<PaymentsPlanDescriptor?> GetPlanAsync(string planId, CancellationToken cancellationToken)
        {
            PaymentsPlanDescriptor? descriptor = planId switch
            {
                PlanIdPro when owner._paymentsProPriceId is not null
                    => new PaymentsPlanDescriptor(planId, owner._paymentsProPriceId, PaymentsMode.Subscription, AllowPromotionCodes: true),
                PlanIdTeam when owner._paymentsTeamPriceId is not null
                    => new PaymentsPlanDescriptor(planId, owner._paymentsTeamPriceId, PaymentsMode.Subscription, AllowPromotionCodes: true),
                _ when owner._planCatalog.Value is { } catalog && catalog.PlanIdToPriceId.TryGetValue(planId, out var priceFromCatalog)
                    => new PaymentsPlanDescriptor(planId, priceFromCatalog, PaymentsMode.Subscription, AllowPromotionCodes: true),
                _ => null,
            };

            return Task.FromResult(descriptor);
        }

        public async Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(owner._paymentsDemoCustomerId))
            {
                return owner._paymentsDemoCustomerId;
            }

            var query = $"metadata['validation_customer_key']:'{appCustomerKey}'";
            var existing = await owner._payments!.SearchCustomersAsync(query, limit: 1, cancellationToken);

            if (existing.Count > 0)
            {
                owner._paymentsDemoCustomerId = existing[0];
                return existing[0];
            }

            var id = await owner._payments!.CreateCustomerAsync(
                new PaymentsCustomerInfo
                {
                    Email = email ?? "validation@ikon.live",
                    Name = "Validation Demo",
                    Metadata = new Dictionary<string, string>
                    {
                        ["validation_customer_key"] = appCustomerKey,
                    },
                },
                idempotencyKey: $"customer-{appCustomerKey}",
                cancellationToken: cancellationToken);

            owner._paymentsDemoCustomerId = id;
            return id;
        }

        public Task ApplyEventAsync(PaymentsEvent evt, CancellationToken cancellationToken)
        {
            owner.LogPaymentsEvent($"[event] {evt.Type} · {evt.EventId}{(evt.Status is null ? string.Empty : " · " + evt.Status)}");

            _ = owner.RefreshPaymentsDataAsync();

            if (evt.Type is PaymentsEventType.ProductUpdated or PaymentsEventType.PriceUpdated)
            {
                _ = owner.RefreshCatalogAsync();
            }

            return Task.CompletedTask;
        }
    }

    private async Task BootstrapConnectAsync()
    {
        if (_paymentsProviderValue != PaymentsProvider.IkonConnect)
        {
            return;
        }

        await RefreshConnectStatusAsync();
    }

    public async Task<string?> EnableConnectAsync(string country = "FI")
    {
        if (_paymentsProviderValue != PaymentsProvider.IkonConnect)
        {
            _connectError.Value = "EnableConnect is only available in ikon-connect mode.";
            return null;
        }

        if (!IkonBackend.Instance.IsLoggedIn)
        {
            _connectError.Value = "Connect enable requires an active Ikon backend session.";
            return null;
        }

        try
        {
            _connectSettingUp.Value = true;

            var result = await IkonBackend.Instance.CreateAppPaymentsMerchantAsync(
                ResolveSpace(),
                new IkonBackend.AppPaymentsMerchantRequest
                {
                    ContactEmail = "validation-connect@ikon.live",
                    DisplayName = "Ikon Validation",
                    Country = country,
                    DefaultCurrency = "eur",
                });

            _connectAccountId.Value = result.MerchantId;
            _connectOnboardingUrl.Value = result.KycUrl;
            _connectDashboardUrl.Value = result.DashboardUrl;
            _connectError.Value = null;

            await RefreshConnectStatusAsync();

            return result.MerchantId;
        }
        catch (Exception ex)
        {
            _connectError.Value = $"Connect enable failed: {ex.Message}";
            Log.Instance.Warning($"[payments] connect enable failed: {ex.Message}");
            return null;
        }
        finally
        {
            _connectSettingUp.Value = false;
        }
    }

    public async Task DisableConnectAsync()
    {
        _connectAccountId.Value = null;
        _connectAccount.Value = null;
        _connectOnboardingUrl.Value = null;
        _connectDashboardUrl.Value = null;
        _connectError.Value = null;
        _lastRefreshStatusError = null;
        await Task.CompletedTask;
    }

    public async Task RefreshConnectStatusAsync()
    {
        if (_paymentsProviderValue != PaymentsProvider.IkonConnect || !IkonBackend.Instance.IsLoggedIn)
        {
            return;
        }

        try
        {
            var status = await IkonBackend.Instance.GetAppPaymentsStatusAsync(ResolveSpace());

            if (string.IsNullOrEmpty(status.MerchantId))
            {
                _connectAccountId.Value = null;
                _connectAccount.Value = null;
                _connectDashboardUrl.Value = null;
                _lastRefreshStatusError = null;
                return;
            }

            _connectAccountId.Value = status.MerchantId;
            _connectDashboardUrl.Value = status.DashboardUrl;

            _connectAccount.Value = new StripeMerchantAccount(
                Id: status.MerchantId!,
                DetailsSubmitted: status.DetailsSubmitted,
                ChargesEnabled: status.ChargesEnabled,
                PayoutsEnabled: status.PayoutsEnabled,
                RequirementsCurrentlyDue: status.RequirementsCurrentlyDue,
                RequirementsEventuallyDue: Array.Empty<string>(),
                RequirementsDisabledReason: null);

            _lastRefreshStatusError = null;
        }
        catch (Exception ex)
        {
            _lastRefreshStatusError = $"Refresh status failed: {ex.Message}";
            _connectError.Value = _lastRefreshStatusError;
        }
    }

    private string? _lastRefreshStatusError;

    public bool LastRefreshStatusSucceeded => _lastRefreshStatusError is null;
}
