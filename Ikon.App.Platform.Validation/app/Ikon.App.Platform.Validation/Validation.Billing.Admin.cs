using System.Linq;

public partial class Validation
{
    private void RenderAdminTab(UIView view)
    {
        view.Column([Layout.Column.Lg], content: col =>
        {
            RenderIdentityPanel(col);

            if (_billingMode.Value != BillingModeConnect)
            {
                RenderWebhookInfoSection(col);
            }

            if (!BillingReady)
            {
                return;
            }

            RenderAdminActionsSection(col);
            RenderPayoutLogSection(col);
            RenderEventLogSection(col);
        });
    }

    private void RenderPayoutLogSection(UIView view)
    {
        var payouts = _billingEventLog.Value
            .Where(line => line.Contains("Payout", StringComparison.Ordinal))
            .Take(20)
            .ToList();

        if (payouts.Count == 0)
        {
            return;
        }

        view.Box([Card.Default, "p-6"], content: card =>
        {
            card.Text([Text.H3, "mb-1"], "Recent payouts");
            card.Text([Text.BodySm, "text-tertiary mb-3"], "Filtered subset of the event log — payout.created / .updated / .paid / .failed deliveries from connected accounts. `payout.failed` indicates Stripe disabled the external account; surface the alert to the account holder.");

            card.Column([Layout.Column.Xs, "font-mono text-xs"], content: col =>
            {
                foreach (var line in payouts)
                {
                    var isFailure = line.Contains("PayoutFailed", StringComparison.Ordinal);
                    col.Text([isFailure ? "text-error" : ""], line);
                }
            });
        });
    }

    private void RenderIdentityPanel(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            card.Text([Text.H3, "mb-1"], "App identity");
            card.Text([Text.BodySm, "text-tertiary mb-3"], "Who Stripe thinks this app is, and how the demo user maps to a Stripe customer. Connect mode → one Stripe sub-account per app; BYOK → one Stripe account per app. The `app-customer-key` is whatever stable id the app passes to IBillingAppAdapter — typically the Ikon user id or org id in real apps; here a fixed demo string.");

            var apiKey = BillingAppHelpers.GetSecretOrEnv(app, "STRIPE_API_KEY");
            var apiKeyDisplay = string.IsNullOrEmpty(apiKey)
                ? "(unset — Connect mode proxies via Ikon backend)"
                : apiKey.Length > 12 ? apiKey[..12] + "…" : apiKey;

            var ikonAppId = BillingAppHelpers.GetSecretOrEnv(app, "IKON_APP_ID")
                ?? (string.IsNullOrEmpty(IkonBackend.Instance.SpaceId) ? "validation" : IkonBackend.Instance.SpaceId);

            card.Column([Layout.Column.Xs, "font-mono text-xs"], content: rows =>
            {
                IdentityRow(rows, "provider",            _billingProviderValue.ToString());
                IdentityRow(rows, "billing-mode",        _billingMode.Value);
                IdentityRow(rows, "ikon-app-id",         ikonAppId);
                IdentityRow(rows, "connect-account-id", _connectAccountId.Value ?? "(none)");

                if (_connectAccount.Value is { } acct)
                {
                    IdentityRow(rows, "connect-dashboard",  acct.Dashboard ?? "(unset)");
                    IdentityRow(rows, "connect-entity-type", acct.EntityType ?? "(unset)");
                    IdentityRow(rows, "connect-country",    acct.Country ?? "(unset)");
                }

                IdentityRow(rows, "stripe-api-key",      apiKeyDisplay);
                IdentityRow(rows, "app-customer-key",    $"{ActiveCustomerKey}   (real apps: pass ikonUserId / orgId here)");
                IdentityRow(rows, "stripe-customer-id", ActiveCustomerId ?? "(not bootstrapped)");
            });
        });
    }

    private static void IdentityRow(UIView view, string label, string value)
    {
        view.Row([Layout.Row.Sm], content: r =>
        {
            r.Text(["text-tertiary w-44"], label);
            r.Text(["break-all"], value);
        });
    }

    private void RenderWebhookInfoSection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            card.Text([Text.H3, "mb-1"], "Webhook configuration");
            card.Text([Text.BodySm, "text-tertiary mb-3"], "Two distinct endpoints (Account vs Connect). Register both in Stripe Dashboard → Developers → Webhooks; map each whsec_ to the env var.");

            card.Column([Layout.Column.Md], content: col =>
            {
                col.Column([Layout.Column.Xs], content: g =>
                {
                    g.Text([Text.Caption], "PLATFORM endpoint (Endpoint type: Account):");
                    g.Box(["p-2 rounded-md bg-secondary/40 border border-secondary text-xs font-mono break-all"], content: u => u.Text([], ResolveBillingWebhookUrl()));
                });

                col.Column([Layout.Column.Xs], content: g =>
                {
                    g.Text([Text.Caption], "CONNECT endpoint (Endpoint type: Connect):");
                    g.Box(["p-2 rounded-md bg-secondary/40 border border-secondary text-xs font-mono break-all"], content: u => u.Text([], ResolveConnectWebhookUrl()));
                });

                col.Column([Layout.Column.Xs], content: g =>
                {
                    g.Text([Text.Caption], "Required secrets:");
                    g.Box(["p-2 rounded-md bg-secondary/40 border border-secondary text-xs font-mono"], content: u =>
                    {
                        u.Text([], "STRIPE_API_KEY                 sk_test_... (sandbox only)");
                        u.Text([], "STRIPE_PUBLISHABLE_KEY         pk_test_... (frontend Stripe.js)");
                        u.Text([], "STRIPE_WEBHOOK_SECRET          whsec_...  (PLATFORM endpoint signing)");
                        u.Text([], "STRIPE_CONNECT_WEBHOOK_SECRET  whsec_...  (CONNECT endpoint signing)");
                    });
                });
            });
        });
    }

    private void RenderAdminActionsSection(UIView view)
    {
        RenderAdminCatalog(view);
        RenderAdminCustomers(view);
        RenderAdminDiscounts(view);
        RenderAdminInvoicing(view);
        RenderAdminWebhooks(view);
        RenderAdminMisc(view);
    }

    private static void AdminCardHeader(UIView view, string title, string description)
    {
        view.Text([Text.H3, "mb-1"], title);
        view.Text([Text.BodySm, "text-tertiary mb-4"], description);
    }

    private static void ActionRow(UIView view, string title, string description, Action<UIView> body)
    {
        view.Box(["py-4 border-t border-secondary/40 first:border-t-0 first:pt-0"], content: row =>
        {
            row.Text([Text.Body, "font-semibold mb-1"], title);

            if (!string.IsNullOrEmpty(description))
            {
                row.Text([Text.BodySm, "text-tertiary mb-3"], description);
            }

            row.Row([Layout.Row.Sm, "flex-wrap items-end gap-3"], content: body);
        });
    }

    private static void FieldColumn(UIView view, string label, Action<UIView> input)
    {
        view.Column([Layout.Column.Xs], content: c =>
        {
            c.Text([Text.Caption], label);
            input(c);
        });
    }

    private void RenderAdminCatalog(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            AdminCardHeader(card, "Catalog · plans & products", "Create Stripe products + recurring prices. Archive removed products. Wraps CreateProductAsync / CreatePriceAsync / UpdateProductAsync / ListProductsAsync.");

            ActionRow(card, "Create plan", "Product + monthly recurring price in one call. Added live to PricingTable.", body: r =>
            {
                FieldColumn(r, "Name", c => c.TextField(_adminPlanName, style: [Input.DefaultSm, "w-48"], placeholder: "Plan name"));
                FieldColumn(r, "Price", c => c.TextField(_adminPlanAmount, style: [Input.DefaultSm, "w-32"], placeholder: "19.00"));
                FieldColumn(r, "Currency", c => c.TextField(_adminPlanCurrency, style: [Input.DefaultSm, "w-20"]));
                FieldColumn(r, "Interval", c => c.TextField(_adminPlanInterval, style: [Input.DefaultSm, "w-24"], placeholder: "month"));
                FieldColumn(r, "Features (one per line)", c => c.TextArea(_adminPlanFeatures, style: [Input.DefaultSm, "w-64 min-h-[60px]"], placeholder: "Unlimited workshops\nPriority support"));

                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Create plan",
                    content: row => { row.Icon([Icon.Xs], name: "plus"); row.Text([], "Create plan"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Create plan", "Create plan: done", async () =>
                    {
                        var name = _adminPlanName.Value.Trim();
                        if (string.IsNullOrEmpty(name)) { throw new InvalidOperationException("Name required"); }

                        var currency = string.IsNullOrEmpty(_adminPlanCurrency.Value) ? "eur" : _adminPlanCurrency.Value.Trim().ToLowerInvariant();
                        var interval = string.IsNullOrEmpty(_adminPlanInterval.Value) ? "month" : _adminPlanInterval.Value.Trim().ToLowerInvariant();
                        var amount = ParseMajorToMinor(_adminPlanAmount.Value, currency);

                        var features = ParseFeatureLines(_adminPlanFeatures.Value);
                        var lookupKey = $"validation-{Guid.NewGuid():N}";

                        var productId = await ActiveBilling!.CreateProductAsync(new BillingProductInfo
                        {
                            Name = name,
                            Description = "Validation app dynamic plan — safe to delete.",
                            MarketingFeatures = features.Count > 0 ? features : null,
                            Metadata = new Dictionary<string, string>
                            {
                                ["validation"] = "true",
                                ["validation_dynamic"] = "true",
                                ["badge"] = "Dynamic",
                            },
                        });
                        var priceId = await ActiveBilling.CreatePriceAsync(new BillingPriceInfo
                        {
                            ProductId = productId,
                            UnitAmountMinor = amount,
                            Currency = currency,
                            RecurringInterval = interval,
                            LookupKey = lookupKey,
                        });

                        _adminPlanName.Value = "";
                        _adminPlanAmount.Value = "";
                        _adminPlanFeatures.Value = "";
                        await RefreshAdminProductsAsync();
                        await RefreshCatalogAsync();
                        _billingActionStatus.Value = $"Plan created → product={productId} · price={priceId} · planId={lookupKey}";
                    }));
            });

            ActionRow(card, "List products", "Read all products from Stripe (active + archived).", body: r =>
            {
                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "List products",
                    content: row => { row.Icon([Icon.Xs], name: "list"); row.Text([], "List products"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("List products", "List products: done", async () =>
                    {
                        var products = await ActiveBilling!.ListProductsAsync();
                        _adminListedProducts.Value = products;
                        _billingActionStatus.Value = $"Loaded {products.Count} products";
                    }));
            });

            ActionRow(card, "Archive product", "Sets active=false. Existing subscribers unaffected.", body: r =>
            {
                FieldColumn(r, "Product id", c => c.TextField(_adminProductIdToArchive, style: [Input.DefaultSm, "w-64"], placeholder: "prod_…"));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Archive",
                    content: row => { row.Icon([Icon.Xs], name: "archive"); row.Text([], "Archive"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Archive product", "Archive: done", async () =>
                    {
                        var pid = _adminProductIdToArchive.Value.Trim();
                        if (string.IsNullOrEmpty(pid)) { throw new InvalidOperationException("Product id required"); }

                        await ActiveBilling!.UpdateProductAsync(pid, active: false);
                        _adminProductIdToArchive.Value = "";
                        await RefreshAdminProductsAsync();
                        await RefreshCatalogAsync();
                        _billingActionStatus.Value = $"Archived product {pid}";
                    }));
            });

            if (_adminListedProducts.Value.Count > 0)
            {
                RenderListPanel(
                    card,
                    "Products",
                    [.. _adminListedProducts.Value.Select(p =>
                    {
                        var status = p.Active ? "active" : "inactive";
                        var desc = string.IsNullOrEmpty(p.Description) ? "" : $" · {p.Description}";
                        var features = p.MarketingFeatures is { Count: > 0 } ? $" · features: {string.Join(", ", p.MarketingFeatures)}" : "";
                        return $"{p.Name}\n{p.Id} · {status}{desc}{features}";
                    })],
                    () => { _adminListedProducts.Value = []; });
            }
        });
    }

    private void RenderAdminCustomers(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            AdminCardHeader(card, "Customers", "Create, update, search Stripe customers + manage their tax ids. Wraps CreateCustomerAsync / UpdateCustomerAsync / SearchCustomersDetailedAsync / *CustomerTaxId*.");

            ActionRow(card, "Create customer", "New Stripe customer tagged with `validation_admin_created` metadata.", body: r =>
            {
                FieldColumn(r, "Email", c => c.TextField(_adminCustomerEmail, style: [Input.DefaultSm, "w-56"], placeholder: "user@example.com"));
                FieldColumn(r, "Name", c => c.TextField(_adminCustomerName, style: [Input.DefaultSm, "w-48"]));

                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Create customer",
                    content: row => { row.Icon([Icon.Xs], name: "user-plus"); row.Text([], "Create customer"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Create customer", "Create customer: done", async () =>
                    {
                        var id = await ActiveBilling!.CreateCustomerAsync(new BillingCustomerInfo
                        {
                            Email = string.IsNullOrEmpty(_adminCustomerEmail.Value) ? null : _adminCustomerEmail.Value,
                            Name = string.IsNullOrEmpty(_adminCustomerName.Value) ? null : _adminCustomerName.Value,
                            Metadata = new Dictionary<string, string> { ["validation_admin_created"] = "true" },
                        });
                        _adminCustomerEmail.Value = "";
                        _adminCustomerName.Value = "";
                        try
                        {
                            _adminListedCustomers.Value = await ActiveBilling.SearchCustomersDetailedAsync("metadata['validation_admin_created']:'true'");
                        }
                        catch { _adminListedCustomers.Value = [id, .._adminListedCustomers.Value]; }
                        _billingActionStatus.Value = $"Customer created → {id}";
                    }));
            });

            ActionRow(card, "Update customer name", "Real admin op — customer asks to change displayed name. Wraps UpdateCustomerAsync with a partial BillingCustomerInfo.", body: r =>
            {
                FieldColumn(r, "Stripe customer id", c => c.TextField(_adminCustomerIdToUpdate, style: [Input.DefaultSm, "w-56"], placeholder: "cus_…"));
                FieldColumn(r, "New name", c => c.TextField(_adminCustomerName, style: [Input.DefaultSm, "w-48"], placeholder: "Updated name"));

                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Update",
                    content: row => { row.Icon([Icon.Xs], name: "edit"); row.Text([], "Update"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Update customer", "Update customer: done", async () =>
                    {
                        var cid = _adminCustomerIdToUpdate.Value.Trim();
                        var newName = _adminCustomerName.Value.Trim();
                        if (string.IsNullOrEmpty(cid)) { throw new InvalidOperationException("Customer id required"); }
                        if (string.IsNullOrEmpty(newName)) { throw new InvalidOperationException("New name required"); }

                        await ActiveBilling!.UpdateCustomerAsync(cid, new BillingCustomerInfo { Name = newName });
                        _adminCustomerName.Value = "";
                        _billingActionStatus.Value = $"Customer {cid} name → {newName}";
                    }));
            });

            ActionRow(card, "Search customer", "Find a Stripe customer by `app_customer_key` metadata tag. This is how an app maps its own user/org ids back to Stripe customers — see how `ResolveStripeCustomerIdAsync` in the adapter persists the link.", body: r =>
            {
                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: $"Search by app_customer_key = {ActiveCustomerKey}",
                    content: row => { row.Icon([Icon.Xs], name: "search"); row.Text([], $"Search by app_customer_key = {ActiveCustomerKey}"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Search customer", "Search: done", async () =>
                    {
                        var rows = await ActiveBilling!.SearchCustomersDetailedAsync($"metadata['validation_customer_key']:'{BillingDemoCustomerKey}'");
                        _adminListedCustomers.Value = rows;
                        _billingActionStatus.Value = $"Customer search → {rows.Count} match";
                    }));
            });

            if (_adminListedCustomers.Value.Count > 0)
            {
                RenderListPanel(card, "Customer ids", _adminListedCustomers.Value, () => { _adminListedCustomers.Value = []; });
            }

            ActionRow(card, "Attach tax id (B2B)", "EU VAT or US EIN. Type examples: `eu_vat`, `us_ein`. Required for net-30 invoicing.", body: r =>
            {
                FieldColumn(r, "Customer id", c => c.TextField(_adminTaxIdCustomer, style: [Input.DefaultSm, "w-48"], placeholder: "cus_…"));
                FieldColumn(r, "Type", c => c.TextField(_adminTaxIdType, style: [Input.DefaultSm, "w-32"]));
                FieldColumn(r, "Value", c => c.TextField(_adminTaxIdValue, style: [Input.DefaultSm, "w-48"], placeholder: "DE123456789"));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Attach",
                    content: row => { row.Icon([Icon.Xs], name: "plus"); row.Text([], "Attach"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Attach tax id", "Attach tax id: done", async () =>
                    {
                        var cid = _adminTaxIdCustomer.Value.Trim();
                        var type = _adminTaxIdType.Value.Trim();
                        var value = _adminTaxIdValue.Value.Trim();
                        if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(value))
                        {
                            throw new InvalidOperationException("Customer + type + value all required");
                        }

                        var tax = await ActiveBilling!.CreateCustomerTaxIdAsync(cid, type, value);
                        _adminTaxIdValue.Value = "";
                        _adminListedTaxIds.Value = await ActiveBilling.ListCustomerTaxIdsAsync(cid);
                        _billingActionStatus.Value = $"Tax id attached → {tax.Id}";
                    }));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "List for customer",
                    content: row => { row.Icon([Icon.Xs], name: "list"); row.Text([], "List for customer"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("List tax ids", "List tax ids: done", async () =>
                    {
                        var cid = _adminTaxIdCustomer.Value.Trim();
                        if (string.IsNullOrEmpty(cid)) { throw new InvalidOperationException("Customer id required"); }

                        _adminListedTaxIds.Value = await ActiveBilling!.ListCustomerTaxIdsAsync(cid);
                        _billingActionStatus.Value = $"Loaded {_adminListedTaxIds.Value.Count} tax ids for cus {cid}";
                    }));
            });

            ActionRow(card, "Delete tax id", "Detach a tax id from its customer. Requires the customer id field above to be set.", body: r =>
            {
                FieldColumn(r, "Tax id (txi_…)", c => c.TextField(_adminTaxIdToDelete, style: [Input.DefaultSm, "w-64"], placeholder: "txi_…"));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Delete",
                    content: row => { row.Icon([Icon.Xs], name: "trash-2"); row.Text([], "Delete"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Delete tax id", "Delete tax id: done", async () =>
                    {
                        var cid = _adminTaxIdCustomer.Value.Trim();
                        var txid = _adminTaxIdToDelete.Value.Trim();
                        if (string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(txid))
                        {
                            throw new InvalidOperationException("Customer id + tax id both required");
                        }

                        await ActiveBilling!.DeleteCustomerTaxIdAsync(cid, txid);
                        _adminTaxIdToDelete.Value = "";
                        _adminListedTaxIds.Value = await ActiveBilling.ListCustomerTaxIdsAsync(cid);
                        _billingActionStatus.Value = $"Tax id {txid} deleted from {cid}";
                    }));
            });

            if (_adminListedTaxIds.Value.Count > 0)
            {
                RenderListPanel(card, "Tax ids", _adminListedTaxIds.Value, () => { _adminListedTaxIds.Value = []; });
            }
        });
    }

    private void RenderAdminDiscounts(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            AdminCardHeader(card, "Discounts · coupons & promo codes", "Two-tier model: internal coupons (% off, server-managed) + customer-facing promo codes that map to a coupon. Wraps CreateCouponAsync / CreatePromotionCodeAsync / List*.");

            ActionRow(card, "Create coupon", "Server-side discount (one-time). Customer never sees the coupon id — promo codes wrap it.", body: r =>
            {
                FieldColumn(r, "% off", c => c.TextField(_adminCouponPercent, style: [Input.DefaultSm, "w-20"], type: "number"));

                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Create coupon",
                    content: row => { row.Icon([Icon.Xs], name: "percent"); row.Text([], "Create coupon"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Create coupon", "Create coupon: done", async () =>
                    {
                        if (!decimal.TryParse(_adminCouponPercent.Value, out var pct) || pct <= 0 || pct > 100)
                        {
                            throw new InvalidOperationException("% off must be in (0, 100]");
                        }

                        var id = await ActiveBilling!.CreateCouponAsync(new BillingCouponInfo
                        {
                            Id = $"VAL_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                            Name = $"Validation {pct}%",
                            PercentOff = pct,
                            Duration = BillingCouponDuration.Once,
                        });
                        _adminListedCoupons.Value = [id, .._adminListedCoupons.Value];
                        _billingActionStatus.Value = $"Coupon created → {id}";
                    }));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "List coupons",
                    content: row => { row.Icon([Icon.Xs], name: "list"); row.Text([], "List coupons"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("List coupons", "List coupons: done", async () =>
                    {
                        var ids = await ActiveBilling!.ListCouponsAsync();
                        _adminListedCoupons.Value = ids;
                        _billingActionStatus.Value = $"Loaded {ids.Count} coupons";
                    }));
            });

            if (_adminListedCoupons.Value.Count > 0)
            {
                RenderListPanel(card, "Coupon ids", _adminListedCoupons.Value, () => { _adminListedCoupons.Value = []; });
            }

            ActionRow(card, "Create promo code", "Customer-facing redemption code. Bind to a coupon id from the list above.", body: r =>
            {
                FieldColumn(r, "Coupon id", c => c.TextField(_adminPromoCouponId, style: [Input.DefaultSm, "w-48"], placeholder: "VAL_…"));
                FieldColumn(r, "Promo code", c => c.TextField(_adminPromoCode, style: [Input.DefaultSm, "w-40"], placeholder: "LAUNCH50"));

                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Create promo code",
                    content: row => { row.Icon([Icon.Xs], name: "tag"); row.Text([], "Create promo code"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Create promo code", "Create promo code: done", async () =>
                    {
                        var couponId = _adminPromoCouponId.Value.Trim();
                        var code = _adminPromoCode.Value.Trim();
                        if (string.IsNullOrEmpty(couponId) || string.IsNullOrEmpty(code))
                        {
                            throw new InvalidOperationException("Coupon id + code both required");
                        }

                        var id = await ActiveBilling!.CreatePromotionCodeAsync(couponId: couponId, code: code);
                        _adminPromoCode.Value = "";
                        _adminListedPromoCodes.Value = [id, .._adminListedPromoCodes.Value];
                        _billingActionStatus.Value = $"Promo code {code} → {id}";
                    }));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "List promo codes",
                    content: row => { row.Icon([Icon.Xs], name: "list"); row.Text([], "List promo codes"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("List promo codes", "List promo codes: done", async () =>
                    {
                        var ids = await ActiveBilling!.ListPromotionCodesAsync();
                        _adminListedPromoCodes.Value = ids;
                        _billingActionStatus.Value = $"Loaded {ids.Count} promo codes";
                    }));
            });

            if (_adminListedPromoCodes.Value.Count > 0)
            {
                RenderListPanel(card, "Promo code ids", _adminListedPromoCodes.Value, () => { _adminListedPromoCodes.Value = []; });
            }
        });
    }

    private async Task RefreshAdminProductsAsync()
    {
        if (ActiveBilling is null) { return; }
        try
        {
            _adminListedProducts.Value = await ActiveBilling.ListProductsAsync();
        }
        catch
        {
        }
    }

    private static void RenderListPanel(UIView view, string title, IReadOnlyList<string> items, System.Action onClear)
    {
        view.Box(["mt-4 rounded-lg border border-secondary bg-card"], content: panel =>
        {
            panel.Row(["px-4 py-3 border-b border-secondary items-center justify-between"], content: hdr =>
            {
                hdr.Text([Text.Caption, "font-semibold uppercase tracking-wide"], $"{title} ({items.Count})");
                hdr.Button(style: [Button.GhostSm], text: "Clear", onClick: onClear);
            });
            panel.Column([], content: rows =>
            {
                foreach (var raw in items)
                {
                    var parts = raw.Split('\n', 2);
                    var primary = parts[0];
                    var secondary = parts.Length > 1 ? parts[1] : string.Empty;

                    rows.Column([Layout.Column.Xs, "px-4 py-3 border-t border-secondary/40"], content: row =>
                    {
                        row.Text([Text.Body, "font-medium"], primary);

                        if (!string.IsNullOrEmpty(secondary))
                        {
                            row.Text(["font-mono text-xs text-tertiary"], secondary);
                        }
                    });
                }
            });
        });
    }

    private void RenderAdminInvoicing(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            AdminCardHeader(card, "Invoicing · hosted invoices & credit notes", "B2B invoicing (net-30 hosted links) + credit notes for tax-aware partial refunds. Wraps CreateHostedInvoiceAsync / PreviewUpcomingInvoiceAsync / *CreditNote*.");

            ActionRow(card, "Hosted invoice", "B2B net-30 — customer receives an emailed Stripe-hosted invoice link to pay.", body: r =>
            {
                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Create €5 net-30",
                    content: row => { row.Icon([Icon.Xs], name: "file-text"); row.Text([], "Create €5 net-30"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Hosted invoice", "Hosted invoice: done", async () =>
                    {
                        if (ActiveCustomerId is null) { throw new InvalidOperationException("Customer not ready"); }

                        var inv = await ActiveBilling!.CreateHostedInvoiceAsync(
                            stripeCustomerId: ActiveCustomerId,
                            lines: [BillingLineItem.Dynamic(500, "eur", "Validation hosted invoice")],
                            daysUntilDue: 30,
                            autoSend: false);
                        _billingActionStatus.Value = $"Hosted invoice → {inv.Id} · {inv.HostedInvoiceUrl}";
                        await RefreshBillingDataAsync();
                    }));
            });

            ActionRow(card, "Preview upcoming invoice", "What Stripe will bill the customer next cycle. Requires an active subscription.", body: r =>
            {
                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Preview",
                    content: row => { row.Icon([Icon.Xs], name: "calculator"); row.Text([], "Preview"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Preview upcoming invoice", "Preview: done", async () =>
                    {
                        if (ActiveCustomerId is null) { throw new InvalidOperationException("Customer not ready"); }

                        var sub = _billingSubscriptions.Value.FirstOrDefault();
                        if (sub is null) { throw new InvalidOperationException("Needs an active subscription"); }

                        var preview = await ActiveBilling!.PreviewUpcomingInvoiceAsync(ActiveCustomerId, subscriptionId: sub.Id);
                        _billingActionStatus.Value = $"Upcoming: {preview.AmountDueMinor / 100m:0.00} {preview.Currency.ToUpperInvariant()} · {preview.Lines.Count} line(s)";
                    }));
            });

            ActionRow(card, "Create credit note", "Formal partial refund — Stripe handles tax reversal + regenerates the invoice PDF.", body: r =>
            {
                FieldColumn(r, "Invoice id", c => c.TextField(_adminCreditInvoiceId, style: [Input.DefaultSm, "w-56"], placeholder: "in_…"));
                FieldColumn(r, "Credit amount", c => c.TextField(_adminCreditAmount, style: [Input.DefaultSm, "w-36"], placeholder: "5.00"));

                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Create",
                    content: row => { row.Icon([Icon.Xs], name: "minus-circle"); row.Text([], "Create"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Create credit note", "Credit note: done", async () =>
                    {
                        var invId = _adminCreditInvoiceId.Value.Trim();

                        if (string.IsNullOrEmpty(invId))
                        {
                            throw new InvalidOperationException("Invoice id required");
                        }

                        var amt = ParseMajorToMinor(_adminCreditAmount.Value, currency: "eur");

                        var note = await ActiveBilling!.CreateCreditNoteAsync(new BillingCreditNoteInfo
                        {
                            InvoiceId = invId,
                            AmountMinor = amt,
                        });
                        _adminCreditAmount.Value = "";
                        _adminListedCreditNotes.Value = await ActiveBilling.ListCreditNotesAsync();
                        _billingActionStatus.Value = $"Credit note → {note.Id} · {note.Status}";
                        await RefreshBillingDataAsync();
                    }));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "List",
                    content: row => { row.Icon([Icon.Xs], name: "list"); row.Text([], "List"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("List credit notes", "List credit notes: done", async () =>
                    {
                        var invFilter = _adminCreditInvoiceId.Value.Trim();
                        _adminListedCreditNotes.Value = await ActiveBilling!.ListCreditNotesAsync(string.IsNullOrEmpty(invFilter) ? null : invFilter);
                        _billingActionStatus.Value = $"Loaded {_adminListedCreditNotes.Value.Count} credit notes";
                    }));
            });

            ActionRow(card, "Void credit note", "Reverse a mistakenly-issued credit note.", body: r =>
            {
                FieldColumn(r, "Credit note id", c => c.TextField(_adminCreditNoteIdToVoid, style: [Input.DefaultSm, "w-56"], placeholder: "cn_…"));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Void",
                    content: row => { row.Icon([Icon.Xs], name: "octagon-x"); row.Text([], "Void"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Void credit note", "Void credit note: done", async () =>
                    {
                        var id = _adminCreditNoteIdToVoid.Value.Trim();
                        if (string.IsNullOrEmpty(id)) { throw new InvalidOperationException("Credit note id required"); }

                        await ActiveBilling!.VoidCreditNoteAsync(id);
                        _adminCreditNoteIdToVoid.Value = "";
                        _adminListedCreditNotes.Value = await ActiveBilling.ListCreditNotesAsync();
                        _billingActionStatus.Value = $"Voided credit note {id}";
                    }));
            });

            if (_adminListedCreditNotes.Value.Count > 0)
            {
                RenderListPanel(card, "Credit notes", _adminListedCreditNotes.Value, () => { _adminListedCreditNotes.Value = []; });
            }
        });
    }

    private void RenderAdminWebhooks(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            AdminCardHeader(card, "Webhooks · endpoint admin", "Self-provision Stripe webhook endpoints via API. Alternative to Stripe Dashboard clicks. Wraps CreateWebhookEndpointAsync / DeleteWebhookEndpointAsync / ListWebhookEndpointsAsync.");

            ActionRow(card, "Register endpoint", "Stripe returns a signing secret only once at creation — captured server-side.", body: r =>
            {
                FieldColumn(r, "Endpoint URL", c => c.TextField(_adminWebhookUrl, style: [Input.DefaultSm, "w-96"], placeholder: "https://example.com/stripe"));

                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Register",
                    content: row => { row.Icon([Icon.Xs], name: "link"); row.Text([], "Register"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Register webhook endpoint", "Register: done", async () =>
                    {
                        var url = _adminWebhookUrl.Value.Trim();
                        if (string.IsNullOrEmpty(url)) { throw new InvalidOperationException("URL required"); }

                        var ep = await ActiveBilling!.CreateWebhookEndpointAsync(
                            url: url,
                            enabledEvents: ["invoice.paid", "customer.subscription.updated", "payment_intent.succeeded"]);
                        _adminWebhookUrl.Value = "";
                        _adminListedWebhookEndpoints.Value = await ActiveBilling.ListWebhookEndpointsAsync();
                        _billingActionStatus.Value = $"Webhook endpoint → {ep.Id} · secret captured server-side";
                    }));
            });

            ActionRow(card, "Ping / list / delete endpoints", "Send a v2 test event (Stripe POSTs a synthetic `v2.core.event_destination.ping` to verify wire). Inspect existing endpoints or remove one.", body: r =>
            {
                FieldColumn(r, "Endpoint id (we_…)", c => c.TextField(_adminWebhookIdToDelete, style: [Input.DefaultSm, "w-64"], placeholder: "we_…"));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Ping",
                    content: row => { row.Icon([Icon.Xs], name: "send"); row.Text([], "Ping"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Ping webhook endpoint", "Ping: sent", async () =>
                    {
                        var id = _adminWebhookIdToDelete.Value.Trim();
                        if (string.IsNullOrEmpty(id)) { throw new InvalidOperationException("Endpoint id required"); }

                        await ActiveBilling!.PingWebhookEndpointAsync(id);
                        _billingActionStatus.Value = $"Pinged endpoint {id} — check the event log for the synthetic ping delivery";
                    }));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Delete",
                    content: row => { row.Icon([Icon.Xs], name: "trash-2"); row.Text([], "Delete"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Delete webhook endpoint", "Delete webhook: done", async () =>
                    {
                        var id = _adminWebhookIdToDelete.Value.Trim();
                        if (string.IsNullOrEmpty(id)) { throw new InvalidOperationException("Endpoint id required"); }

                        await ActiveBilling!.DeleteWebhookEndpointAsync(id);
                        _adminWebhookIdToDelete.Value = "";
                        _adminListedWebhookEndpoints.Value = await ActiveBilling.ListWebhookEndpointsAsync();
                        _billingActionStatus.Value = $"Deleted endpoint {id}";
                    }));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "List",
                    content: row => { row.Icon([Icon.Xs], name: "list"); row.Text([], "List"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("List webhook endpoints", "List endpoints: done", async () =>
                    {
                        _adminListedWebhookEndpoints.Value = await ActiveBilling!.ListWebhookEndpointsAsync();
                        _billingActionStatus.Value = $"Loaded {_adminListedWebhookEndpoints.Value.Count} webhook endpoints";
                    }));
            });

            if (_adminListedWebhookEndpoints.Value.Count > 0)
            {
                RenderListPanel(card, "Webhook endpoints", _adminListedWebhookEndpoints.Value, () => { _adminListedWebhookEndpoints.Value = []; });
            }
        });
    }

    private void RenderAdminMisc(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            AdminCardHeader(card, "Misc · balance, payment links, Apple Pay, event replay", "Operational admin — customer balance adjustments, payment link generation, Apple Pay domain registration, webhook outage recovery.");

            ActionRow(card, "Adjust customer balance", "Goodwill credit (negative = reduces future invoices). Persists on Stripe customer.", body: r =>
            {
                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Adjust −€5",
                    content: row => { row.Icon([Icon.Xs], name: "trending-down"); row.Text([], "Adjust −€5"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Adjust customer balance", "Balance adjust: done", async () =>
                    {
                        if (ActiveCustomerId is null) { throw new InvalidOperationException("Customer not ready"); }

                        await ActiveBilling!.AdjustCustomerBalanceAsync(ActiveCustomerId, -500, "eur", "Validation goodwill credit", $"validation-credit-{Guid.NewGuid():N}");
                        _billingActionStatus.Value = "Customer balance adjusted by −€5";
                    }));
            });

            ActionRow(card, "Replay events (outage recovery)", "List Stripe events from the last 24h and re-deliver to the adapter. Use after a webhook outage.", body: r =>
            {
                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Replay last 24h",
                    content: row => { row.Icon([Icon.Xs], name: "rewind"); row.Text([], "Replay last 24h"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Replay events", "Replay: done", async () =>
                    {
                        var since = DateTimeOffset.UtcNow.AddHours(-24);
                        var ids = await ActiveBilling!.ListEventIdsAsync(createdAfter: since, limit: 25);
                        var summaries = new List<string>();

                        foreach (var evtId in ids.Take(25))
                        {
                            try
                            {
                                var evt = await ActiveBilling.RetrieveEventAsync(evtId);
                                summaries.Add(FormatReplayEvent(evt));

                                if (summaries.Count <= 10)
                                {
                                    LogBillingEvent($"[replay] {evt.Type} · {evt.EventId}");
                                }
                            }
                            catch (Exception ex)
                            {
                                summaries.Add($"{evtId}\n(retrieve failed: {ex.Message})");
                            }
                        }

                        _adminRecentEventIds.Value = summaries;
                        _billingActionStatus.Value = $"Loaded {summaries.Count} recent events";
                    }));
            });

            if (_adminRecentEventIds.Value.Count > 0)
            {
                RenderListPanel(card, "Recent events (last 24h)", _adminRecentEventIds.Value, () => { _adminRecentEventIds.Value = []; });
            }

            ActionRow(card, "Apple Pay domain", "Register a domain so Apple Pay shows up in Checkout. Stripe handles the verification file.", body: r =>
            {
                FieldColumn(r, "Domain", c => c.TextField(_adminApplePayDomain, style: [Input.DefaultSm, "w-64"], placeholder: "app.example.com"));

                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Register",
                    content: row => { row.Icon([Icon.Xs], name: "apple"); row.Text([], "Register"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Register Apple Pay domain", "Apple Pay register: done", async () =>
                    {
                        var d = _adminApplePayDomain.Value.Trim();
                        if (string.IsNullOrEmpty(d)) { throw new InvalidOperationException("Domain required"); }

                        var id = await ActiveBilling!.RegisterApplePayDomainAsync(d);
                        _adminApplePayDomain.Value = "";
                        _adminListedApplePayDomains.Value = await ActiveBilling.ListApplePayDomainsAsync();
                        _billingActionStatus.Value = $"Apple Pay domain registered → {id}";
                    }));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "List",
                    content: row => { row.Icon([Icon.Xs], name: "list"); row.Text([], "List"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("List Apple Pay domains", "List domains: done", async () =>
                    {
                        _adminListedApplePayDomains.Value = await ActiveBilling!.ListApplePayDomainsAsync();
                        _billingActionStatus.Value = $"Loaded {_adminListedApplePayDomains.Value.Count} Apple Pay domains";
                    }));
            });

            if (_adminListedApplePayDomains.Value.Count > 0)
            {
                RenderListPanel(card, "Apple Pay domains", _adminListedApplePayDomains.Value, () => { _adminListedApplePayDomains.Value = []; });
            }

            ActionRow(card, "Payment link", "Shareable Stripe-hosted shopping URL — distribute via chat/email/QR. No customer required.", body: r =>
            {
                FieldColumn(r, "Price id", c => c.TextField(_adminPaymentLinkPriceId, style: [Input.DefaultSm, "w-56"], placeholder: "price_…"));

                r.Button(
                    style: [Button.PrimarySm, Button.IconLeft],
                    text: "Create link",
                    content: row => { row.Icon([Icon.Xs], name: "external-link"); row.Text([], "Create link"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("Create payment link", "Payment link: done", async () =>
                    {
                        var priceId = _adminPaymentLinkPriceId.Value.Trim();
                        if (string.IsNullOrEmpty(priceId)) { throw new InvalidOperationException("Price id required"); }

                        var link = await ActiveBilling!.CreatePaymentLinkAsync(
                            lines: [BillingLineItem.ForPrice(priceId, quantity: 1)]);
                        _adminPaymentLinkPriceId.Value = "";
                        _adminListedPaymentLinks.Value = await ActiveBilling.ListPaymentLinksAsync();
                        _billingActionStatus.Value = $"Payment link → {link.Url}";
                    }));

                r.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "List",
                    content: row => { row.Icon([Icon.Xs], name: "list"); row.Text([], "List"); },
                    disabled: _busy.Value,
                    onClick: async () => await RunActionAsync("List payment links", "List payment links: done", async () =>
                    {
                        _adminListedPaymentLinks.Value = await ActiveBilling!.ListPaymentLinksAsync();
                        _billingActionStatus.Value = $"Loaded {_adminListedPaymentLinks.Value.Count} payment links";
                    }));
            });

            if (_adminListedPaymentLinks.Value.Count > 0)
            {
                RenderListPanel(card, "Payment links", _adminListedPaymentLinks.Value, () => { _adminListedPaymentLinks.Value = []; });
            }
        });
    }

    private void RenderEventLogSection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: card =>
        {
            card.Text([Text.H3, "mb-1"], "Webhook event log");
            card.Text([Text.BodySm, "text-tertiary mb-3"], "Most recent verified Stripe events. Use Stripe CLI to forward locally: `stripe listen --forward-to <webhook-url>`.");

            var log = _billingEventLog.Value;

            if (log.Count == 0)
            {
                card.Text([Text.Caption], "No events received yet.");
                return;
            }

            card.Column([Layout.Column.Xs, "font-mono text-xs max-h-72 overflow-y-auto"], content: col =>
            {
                foreach (var line in log)
                {
                    col.Text([], line);
                }
            });
        });
    }

    /// <summary>
    /// One-line summary of a replay event for the admin "Recent events" panel.
    /// Renders as <c>"{Type} · {Status}\n{EventId} · {key facts}"</c> with whatever
    /// fields the event carries (customer, subscription, amount).
    /// </summary>
    private static string FormatReplayEvent(BillingEvent evt)
    {
        var headline = evt.Status is null ? evt.Type.ToString() : $"{evt.Type} · {evt.Status}";
        var details = new List<string> { evt.EventId };

        if (!string.IsNullOrEmpty(evt.CustomerId))
        {
            details.Add($"cus={evt.CustomerId}");
        }

        if (!string.IsNullOrEmpty(evt.SubscriptionId))
        {
            details.Add($"sub={evt.SubscriptionId}");
        }

        if (evt.AmountPaid is long paid && !string.IsNullOrEmpty(evt.Currency))
        {
            details.Add($"{paid / 100m:0.##} {evt.Currency.ToUpperInvariant()}");
        }

        if (evt.IsLegacyEventName)
        {
            details.Add($"legacy:{evt.RawEventName}");
        }

        if (evt.IsThinEvent && !string.IsNullOrEmpty(evt.RelatedObjectId))
        {
            details.Add($"thin→{evt.RelatedObjectId}");
        }

        return $"{headline}\n{string.Join(" · ", details)}";
    }

    /// <summary>
    /// Split a multi-line textarea value into one feature per non-empty line.
    /// Trims each entry; caps at Stripe's product-features limit (15 entries).
    /// </summary>
    private static List<string> ParseFeatureLines(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        return input
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 0)
            .Take(15)
            .ToList();
    }

    /// <summary>
    /// Parses a major-unit decimal string (e.g. "19.00") into Stripe's minor-unit
    /// integer (e.g. 1900 cents). Aware of zero-decimal currencies (JPY, KRW) and
    /// three-decimal currencies (KWD, BHD) per Stripe's currency list. Throws
    /// <see cref="InvalidOperationException"/> on parse failure or non-positive
    /// values.
    /// </summary>
    private static long ParseMajorToMinor(string input, string currency)
    {
        var trimmed = input?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidOperationException("Amount required");
        }

        if (!decimal.TryParse(trimmed, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var major) || major <= 0m)
        {
            throw new InvalidOperationException("Amount must be a positive number like 19.00");
        }

        var decimals = MinorUnitDecimals(currency);
        var multiplier = (decimal)Math.Pow(10, decimals);
        return (long)Math.Round(major * multiplier, MidpointRounding.AwayFromZero);
    }

    private static string FormatMinorAsMajor(long minor, string currency)
    {
        var decimals = MinorUnitDecimals(currency);
        var divisor = (decimal)Math.Pow(10, decimals);
        var major = minor / divisor;
        return major.ToString($"0.{new string('0', decimals)}", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Stripe minor-unit decimal places per ISO 4217 currency code. Defaults to
    /// 2 for the common case; lists zero-decimal + three-decimal currencies per
    /// <see href="https://docs.stripe.com/currencies"/>.
    /// </summary>
    private static int MinorUnitDecimals(string currency) => currency?.ToLowerInvariant() switch
    {
        "bif" or "clp" or "djf" or "gnf" or "jpy" or "kmf" or "krw" or "mga"
            or "pyg" or "rwf" or "ugx" or "vnd" or "vuv" or "xaf" or "xof" or "xpf" => 0,
        "bhd" or "jod" or "kwd" or "omr" or "tnd" => 3,
        _ => 2,
    };
}
