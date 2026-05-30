using System.Linq;

public partial class Validation
{

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

    private const string BillingDemoCustomerKey = "validation-demo-customer";

    private const string PlanIdPro = "validation-pro";
    private const string PlanIdTeam = "validation-team";

    private const string BillingModeByok = "byok";
    private const string BillingModeConnect = "connect";

    private BillingService? _billing;
    private BillingProvider _billingProviderValue = BillingProvider.IkonConnect;
    private string? _billingDemoCustomerId;
    private string? _billingProPriceId;
    private string? _billingTeamPriceId;
    private string? _billingProProductId;
    private string? _billingTeamProductId;

    private BillingConnectService? _connect;

    private readonly Reactive<bool> _billingReady = new(false);
    private readonly Reactive<string?> _billingError = new(null);
    private readonly Reactive<IReadOnlyList<string>> _billingEventLog = new([]);
    private readonly Reactive<string?> _billingActionStatus = new(null);
    private readonly Reactive<IReadOnlyList<BillingPaymentMethod>> _billingPaymentMethods = new([]);
    private readonly Reactive<IReadOnlyList<BillingInvoiceSummary>> _billingInvoices = new([]);
    private readonly Reactive<IReadOnlyList<BillingSubscription>> _billingSubscriptions = new([]);
    private readonly Reactive<IReadOnlyList<BillingCharge>> _billingCharges = new([]);
    private readonly Reactive<string?> _billingLastCheckoutUrl = new(null);

    private readonly ClientReactive<string> _billingMode = new(BillingModeConnect);

    private readonly Reactive<BillingUpcomingInvoice?> _upcomingInvoice = new(null);

    private readonly Reactive<IReadOnlyList<BillingProduct>> _adminListedProducts = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedCustomers = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedCoupons = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedPromoCodes = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedTaxIds = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedCreditNotes = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedWebhookEndpoints = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedPaymentLinks = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminListedApplePayDomains = new([]);
    private readonly Reactive<IReadOnlyList<string>> _adminRecentEventIds = new([]);

    private readonly ClientReactive<string> _billingTab = new("admin");

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

    private readonly Reactive<BillingPlanCatalog?> _planCatalog = new(null);

    private IReadOnlyList<BillingPlanView> ActivePlans()
    {
        var catalog = _planCatalog.Value;

        if (catalog is null)
        {
            return Array.Empty<BillingPlanView>();
        }

        return catalog.Plans.Select(ToPlanView).ToList();
    }

    private static BillingPlanView ToPlanView(BillingPlanProjection p)
    {
        var priceLabel = FormatPlanPrice(p.UnitAmountMinor, p.Currency);
        var highlighted = p.ProductMetadata is not null
            && p.ProductMetadata.TryGetValue("highlighted", out var h)
            && string.Equals(h, "true", StringComparison.OrdinalIgnoreCase);
        var badge = p.ProductMetadata is not null && p.ProductMetadata.TryGetValue("badge", out var b) ? b : null;
        var features = p.MarketingFeatures is { Count: > 0 } ? p.MarketingFeatures : null;

        return new BillingPlanView(
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
    private readonly Reactive<BillingConnectAccount?> _connectAccount = new(null);
    private readonly Reactive<string?> _connectError = new(null);
    private readonly Reactive<bool> _connectSettingUp = new(false);

    private readonly Reactive<string?> _connectOnboardingUrl = new(null);
    private readonly Reactive<string?> _connectDashboardUrl = new(null);

    private static string ResolveIkonAppId() =>
        string.IsNullOrEmpty(IkonBackend.Instance.SpaceId) ? "validation" : IkonBackend.Instance.SpaceId;

    private async Task InitBillingAsync()
    {
        try
        {
            var autoOpts = BillingAppHelpers.AutoDetectFromApp(app, defaultAppId: "validation");
            _billingProviderValue = autoOpts.Provider;

            if (_billingProviderValue == BillingProvider.Disabled)
            {
                _billingError.Value = "Billing not configured. Set BILLING_PROVIDER=byok (+ STRIPE_API_KEY) or BILLING_PROVIDER=ikon-connect (+ IKON_BACKEND_BILLING_URL). IkonConnect mode reuses the app's standard Ikon backend session token — no separate IKON_APP_TOKEN required.";
                _billingReady.Value = true;
                return;
            }

            if (_billingProviderValue == BillingProvider.Byok)
            {
                if (string.IsNullOrEmpty(autoOpts.ApiKey))
                {
                    _billingError.Value = "BYOK mode: STRIPE_API_KEY not set. Add via `ikon app secret set STRIPE_API_KEY sk_test_...` (or env var). Validation only runs against sandbox keys (sk_test_ or rk_test_).";
                    return;
                }

                if (!IsSandboxKey(autoOpts.ApiKey))
                {
                    _billingError.Value = "STRIPE_API_KEY is not a sandbox key. Validation refuses to run against live Stripe. Use a secret key (sk_test_...) or restricted key (rk_test_...).";
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(autoOpts.IkonBackendUrl))
                {
                    _billingError.Value = "IkonConnect mode: IKON_BACKEND_BILLING_URL not set and no ambient Ikon backend URL available. Add via `ikon app secret set IKON_BACKEND_BILLING_URL https://backend.ikonai.live` (or env var). To switch to BYOK mode, set BILLING_PROVIDER=byok.";
                    return;
                }

                if (!IkonBackend.Instance.IsLoggedIn)
                {
                    _billingError.Value = "IkonConnect mode: no active Ikon backend session. Billing proxy reuses the standard Ikon backend token, but the app is not logged in.";
                    return;
                }
            }

            var opts = autoOpts with
            {
                DefaultSuccessUrl = "https://ikon.live/billing/success",
                DefaultCancelUrl = "https://ikon.live/billing/cancel",
                DefaultPortalReturnUrl = "https://ikon.live/billing/portal-return",
            };

            var adapter = new ValidationBillingAdapter(this);
            _billing = new BillingService(opts, adapter);

            await BootstrapCatalogAsync();
            await RefreshCatalogAsync();
            await RefreshBillingDataAsync();

            _connect = new BillingConnectService(opts);

            await BootstrapConnectAsync();

            _billingReady.Value = true;
            Log.Instance.Info($"[billing] sandbox ready · provider={_billingProviderValue} · pro={_billingProPriceId} team={_billingTeamPriceId} customer={_billingDemoCustomerId}");
        }
        catch (Exception ex)
        {
            _billingError.Value = $"Billing init failed: {ex.Message}";
            Log.Instance.Error($"[billing] init failed: {ex}");
        }
    }

    private async Task BootstrapCatalogAsync()
    {
        var products = await _billing!.ListProductsAsync(activeOnly: true, limit: 100);
        BillingProduct? pro = products.FirstOrDefault(p => p.Name == "Validation Pro");
        BillingProduct? team = products.FirstOrDefault(p => p.Name == "Validation Team");

        _billingProProductId = pro?.Id ?? await _billing.CreateProductAsync(new BillingProductInfo
        {
            Name = "Validation Pro",
            Description = "Sandbox plan exercised by Ikon validation app — safe to delete.",
            Metadata = new Dictionary<string, string> { ["validation"] = "true" },
        });
        _billingTeamProductId = team?.Id ?? await _billing.CreateProductAsync(new BillingProductInfo
        {
            Name = "Validation Team",
            Description = "Sandbox plan exercised by Ikon validation app — safe to delete.",
            Metadata = new Dictionary<string, string> { ["validation"] = "true" },
        });

        await BackfillValidationMetadataAsync(pro);
        await BackfillValidationMetadataAsync(team);

        _billingProPriceId = await EnsureRecurringPriceAsync(_billingProProductId, 1900, "eur", "month");
        _billingTeamPriceId = await EnsureRecurringPriceAsync(_billingTeamProductId, 4900, "eur", "month");

        _billingDemoCustomerId = await new ValidationBillingAdapter(this)
            .ResolveStripeCustomerIdAsync(BillingDemoCustomerKey, "validation@ikon.live", CancellationToken.None);
    }

    private async Task<string> EnsureRecurringPriceAsync(string productId, long amountMinor, string currency, string interval)
    {
        var existing = await _billing!.ListPricesAsync(productId: productId, activeOnly: true, limit: 100);
        var match = existing.FirstOrDefault(p =>
            p.UnitAmountMinor == amountMinor &&
            string.Equals(p.Currency, currency, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.RecurringInterval, interval, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            return match.Id;
        }

        return await _billing.CreatePriceAsync(new BillingPriceInfo
        {
            ProductId = productId,
            UnitAmountMinor = amountMinor,
            Currency = currency,
            RecurringInterval = interval,
        });
    }

    private int _refreshInFlight;

    private async Task RefreshCatalogAsync(BillingService? service = null)
    {
        service ??= _billing;

        if (service is null)
        {
            return;
        }

        try
        {
            var projector = new BillingCatalogProjector(service);
            var catalog = await projector.ProjectAsync(
                productFilter: p => p.Metadata is not null
                    && p.Metadata.TryGetValue("validation", out var v)
                    && string.Equals(v, "true", StringComparison.OrdinalIgnoreCase));
            _planCatalog.Value = catalog;
        }
        catch (Exception ex)
        {
            Ikon.Common.Core.Log.Instance.Warning($"[validation-billing] RefreshCatalogAsync failed: {ex.Message}");
        }
    }

    private async Task BackfillValidationMetadataAsync(BillingProduct? product)
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

            await _billing!.UpdateProductAsync(product.Id, metadata: merged);
        }
        catch (Exception ex)
        {
            Ikon.Common.Core.Log.Instance.Warning($"[validation-billing] backfill validation metadata on {product.Id} failed: {ex.Message}");
        }
    }

    private async Task RefreshBillingDataAsync()
    {
        if (Interlocked.CompareExchange(ref _refreshInFlight, 1, 0) != 0)
        {
            return;
        }

        try
        {
            var service = _billing;
            var customerId = _billingDemoCustomerId;

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

                _billingSubscriptions.Value = subsTask.Result;
                _billingPaymentMethods.Value = pmsTask.Result;
                _billingInvoices.Value = invTask.Result;
                _billingCharges.Value = chargesTask.Result;
            }
            catch (Exception ex)
            {
                _billingActionStatus.Value = $"Refresh failed: {ex.Message}";
                Log.Instance.Warning($"[billing] refresh failed: {ex.Message}");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _refreshInFlight, 0);
        }
    }

    private BillingProvider AutoDetectProvider()
    {
        if (!string.IsNullOrEmpty(TryGetBillingSecret("STRIPE_API_KEY")))
        {
            return BillingProvider.Byok;
        }

        var hasBillingUrl = !string.IsNullOrEmpty(TryGetBillingSecret("IKON_BACKEND_BILLING_URL"))
            || !string.IsNullOrEmpty(IkonBackend.Instance.Url);

        if (hasBillingUrl && IkonBackend.Instance.IsLoggedIn)
        {
            return BillingProvider.IkonConnect;
        }

        return BillingProvider.Disabled;
    }

    private string? TryGetBillingSecret(string key)
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

    private string ResolveBillingWebhookUrl()
    {
        var hook = app.Webhooks.FirstOrDefault(w =>
            string.Equals(w.FunctionName, "stripe", StringComparison.OrdinalIgnoreCase));
        return hook?.PublicUrl ?? "(webhook URL unavailable — start the app to register endpoints)";
    }

    private string ResolveConnectWebhookUrl()
    {
        var hook = app.Webhooks.FirstOrDefault(w =>
            string.Equals(w.FunctionName, "stripe-connect", StringComparison.OrdinalIgnoreCase));
        return hook?.PublicUrl ?? "(webhook URL unavailable — start the app to register endpoints)";
    }

    [Function(Webhook = true, Name = "stripe", Description = "Platform Stripe webhook receiver — POST with Stripe-Signature header. Verified against STRIPE_WEBHOOK_SECRET. Configure in Stripe Dashboard → Webhooks → Endpoint type: Account.")]
    public async Task<string> StripeWebhook(
        Dictionary<string, string> queryParams,
        Dictionary<string, string> headers,
        string body)
    {
        if (_billing is null)
        {
            return """{"received":true,"reason":"billing not initialized"}""";
        }

        headers.TryGetValue("Stripe-Signature", out var signature);

        var result = await _billing.HandleWebhookAsync(signature, body);

        if (!result.Verified)
        {
            Log.Instance.Warning($"[billing] platform webhook unverified: {result.Reason}");
        }
        else if (result.AdapterError is not null)
        {
            Log.Instance.Warning($"[billing] platform webhook adapter error: {result.AdapterError}");
        }

        return """{"received":true}""";
    }

    [Function(Webhook = true, Name = "stripe-connect", Description = "Stripe Connect webhook receiver. Verified against STRIPE_CONNECT_WEBHOOK_SECRET. Configure in Stripe Dashboard → Webhooks → Endpoint type: Connect.")]
    public async Task<string> StripeConnectWebhook(
        Dictionary<string, string> queryParams,
        Dictionary<string, string> headers,
        string body)
    {
        if (_billing is null)
        {
            return """{"received":true,"reason":"billing not initialized"}""";
        }

        headers.TryGetValue("Stripe-Signature", out var signature);
        var result = await _billing.HandleWebhookAsync(signature, body);

        if (!result.Verified)
        {
            Log.Instance.Warning($"[billing] connect webhook unverified: {result.Reason}");
        }
        else if (result.AdapterError is not null)
        {
            Log.Instance.Warning($"[billing] connect webhook adapter error: {result.AdapterError}");
        }

        return """{"received":true}""";
    }

    private readonly object _billingEventLogLock = new();

    private void LogBillingEvent(string line)
    {
        lock (_billingEventLogLock)
        {
            var current = _billingEventLog.Value.ToList();
            current.Insert(0, $"{DateTime.UtcNow:HH:mm:ss}  {line}");

            if (current.Count > 50)
            {
                current.RemoveAt(current.Count - 1);
            }

            _billingEventLog.Value = current;
        }
    }

    private sealed class ValidationBillingAdapter(Validation owner) : IBillingAppAdapter
    {
        public Task<BillingPlanDescriptor?> GetPlanAsync(string planId, CancellationToken cancellationToken)
        {
            BillingPlanDescriptor? descriptor = planId switch
            {
                PlanIdPro when owner._billingProPriceId is not null
                    => new BillingPlanDescriptor(planId, owner._billingProPriceId, BillingMode.Subscription, AllowPromotionCodes: true),
                PlanIdTeam when owner._billingTeamPriceId is not null
                    => new BillingPlanDescriptor(planId, owner._billingTeamPriceId, BillingMode.Subscription, AllowPromotionCodes: true),
                _ when owner._planCatalog.Value is { } catalog && catalog.PlanIdToPriceId.TryGetValue(planId, out var priceFromCatalog)
                    => new BillingPlanDescriptor(planId, priceFromCatalog, BillingMode.Subscription, AllowPromotionCodes: true),
                _ => null,
            };

            return Task.FromResult(descriptor);
        }

        public async Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(owner._billingDemoCustomerId))
            {
                return owner._billingDemoCustomerId;
            }

            var query = $"metadata['validation_customer_key']:'{appCustomerKey}'";
            var existing = await owner._billing!.SearchCustomersAsync(query, limit: 1, cancellationToken);

            if (existing.Count > 0)
            {
                owner._billingDemoCustomerId = existing[0];
                return existing[0];
            }

            var id = await owner._billing!.CreateCustomerAsync(
                new BillingCustomerInfo
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

            owner._billingDemoCustomerId = id;
            return id;
        }

        public Task ApplyEventAsync(BillingEvent evt, CancellationToken cancellationToken)
        {
            owner.LogBillingEvent($"[event] {evt.Type} · {evt.EventId}{(evt.Status is null ? string.Empty : " · " + evt.Status)}");

            _ = owner.RefreshBillingDataAsync();

            if (evt.Type is BillingEventType.ProductUpdated or BillingEventType.PriceUpdated)
            {
                _ = owner.RefreshCatalogAsync();
            }

            return Task.CompletedTask;
        }
    }

    private async Task BootstrapConnectAsync()
    {
        if (_billingProviderValue != BillingProvider.IkonConnect)
        {
            return;
        }

        await RefreshConnectStatusAsync();
    }

    public async Task<string?> EnableConnectAsync(string country = "FI")
    {
        if (_billingProviderValue != BillingProvider.IkonConnect)
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

            var result = await IkonBackend.Instance.CreateAppBillingConnectAccountAsync(
                ResolveIkonAppId(),
                new IkonBackend.AppBillingConnectAccountRequest
                {
                    ContactEmail = "validation-connect@ikon.live",
                    DisplayName = "Ikon Validation",
                    Country = country,
                    DefaultCurrency = "eur",
                });

            _connectAccountId.Value = result.ConnectedAccountId;
            _connectOnboardingUrl.Value = result.KycUrl;
            _connectDashboardUrl.Value = result.DashboardUrl;
            _connectError.Value = null;

            await RefreshConnectStatusAsync();

            return result.ConnectedAccountId;
        }
        catch (Exception ex)
        {
            _connectError.Value = $"Connect enable failed: {ex.Message}";
            Log.Instance.Warning($"[billing] connect enable failed: {ex.Message}");
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
        if (_billingProviderValue != BillingProvider.IkonConnect || !IkonBackend.Instance.IsLoggedIn)
        {
            return;
        }

        try
        {
            var status = await IkonBackend.Instance.GetAppBillingConnectStatusAsync(ResolveIkonAppId());

            if (string.IsNullOrEmpty(status.ConnectedAccountId))
            {
                _connectAccountId.Value = null;
                _connectAccount.Value = null;
                _connectDashboardUrl.Value = null;
                _lastRefreshStatusError = null;
                return;
            }

            _connectAccountId.Value = status.ConnectedAccountId;
            _connectDashboardUrl.Value = status.DashboardUrl;

            _connectAccount.Value = new BillingConnectAccount(
                Id: status.ConnectedAccountId!,
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
