using System.Linq;

public partial class Validation
{

    private readonly Reactive<string> _billingSeatQty = new("1");

    private BillingService? ActiveBilling => _billing;

    private string? ActiveCustomerId => _billingDemoCustomerId;

    private string ActiveCustomerKey => BillingDemoCustomerKey;

    private static readonly IReadOnlyList<BillingPlanView> ValidationPlans =
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

    private void RenderBillingSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: hdr =>
            {
                hdr.Text([Text.H2, "mb-1"], "Billing");
                hdr.Text([Text.BodySm, "text-tertiary mb-4"], "Live Stripe sandbox integration showing how an Ikon app wires Ikon.App.Billing on the v2 surface (Accounts v2 / Billing v2, API version 2026-04-22.dahlia). Admin tab = catalog + customer + invoicing ops. End-user tab = checkout + subscription self-service.");

                if (!string.IsNullOrEmpty(_billingError.Value))
                {
                    hdr.Box(["mt-2 p-3 rounded-md bg-error/10 border border-error/30 text-error text-sm"], content: e => e.Text([], _billingError.Value!));
                }
                else if (!_billingReady.Value)
                {
                    hdr.Box(["mt-2 p-3 rounded-md bg-info/10 border border-info/30 text-info text-sm"], content: e => e.Text([], "Initializing Stripe sandbox …"));
                }
            });

            if (!_billingReady.Value)
            {
                return;
            }

            if (_billingProviderValue == BillingProvider.Disabled)
            {
                return;
            }

            view.Row([Layout.Row.Md, "items-center justify-between flex-wrap"], content: bar =>
            {
                bar.Text([Text.Caption], "Integration mode:");
                bar.ToggleGroupSingle(
                    value: _billingMode.Value,
                    onValueChange: async v =>
                    {
                        _billingMode.Value = string.IsNullOrEmpty(v) ? BillingModeByok : v;

                        _billingLastCheckoutUrl.Value = null;

                        await RefreshBillingDataAsync();
                    },
                    style: ["inline-flex items-center gap-1"],
                    content: tg =>
                    {
                        tg.ToggleGroupItem(value: BillingModeConnect, style: [Toggle.Default], content: t => t.Text([], "Connect (platform-managed)"));
                        tg.ToggleGroupItem(value: BillingModeByok, style: [Toggle.Default], content: t => t.Text([], "BYOK"));
                    });

                bar.Button(
                    style: [Button.OutlineSm, Button.IconLeft],
                    text: "Refresh data", content: row => { row.Icon([Icon.Xs], name: "refresh-cw"); row.Text([], "Refresh data"); },
                    onClick: async () =>
                    {
                        await RefreshBillingDataAsync();
                        _billingActionStatus.Value = "Refreshed customer data from Stripe.";
                    });
            });

            RenderBusyAndReadinessBanner(view);

            view.Tabs(
                value: _billingTab.Value,
                onValueChange: async v => { _billingTab.Value = string.IsNullOrEmpty(v) ? "end-user" : v; await Task.CompletedTask; },
                listStyle: [Tabs.List, "mt-2"],
                triggerStyle: [Tabs.Trigger],
                contentStyle: [Tabs.Content],
                tabs:
                [
                    new TabItem("admin", "Admin actions", view => RenderAdminTab(view)),
                    new TabItem("end-user", "End-user actions", view => RenderEndUserTab(view)),
                ]);

            RenderActionStatusSection(view);
        });
    }

    private static string FormatBillingError(Exception ex)
    {
        var body = ex is BillingApiException api ? api.ResponseBody : null;
        return string.IsNullOrEmpty(body) ? ex.Message : ex.Message + " · " + body;
    }

    /// <summary>
    /// Resolve a human-readable plan name for a subscription by matching its
    /// first item's price id against the projected catalog. Falls back to the
    /// product id (or "Subscription" placeholder) when no catalog entry
    /// matches — e.g. for subscriptions targeting products outside this app's
    /// filter.
    /// </summary>
    private string ResolvePlanName(BillingSubscription sub)
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
    /// Canonical busy flag for the billing tab — drives single-flight
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
    /// the top of the billing tab in red.
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
            _billingActionStatus.Value = statusOk;
        }
        catch (Exception ex)
        {
            var msg = $"{actionId} → {FormatBillingError(ex)}";
            _lastActionError.Value = msg;
            _billingActionStatus.Value = msg;
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

    /// <summary>True when the billing surface is wired and an <see cref="ActiveBilling"/> is available for API calls.</summary>
    private bool BillingReady => _billingReady.Value
        && _billingProviderValue != BillingProvider.Disabled
        && ActiveBilling is not null;

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

        if (!BillingReady)
        {
            view.Box(["mt-2 p-3 rounded-md bg-warning/10 border border-warning/30 text-warning text-sm"], content: b =>
            {
                if (_billingProviderValue == BillingProvider.IkonConnect
                    && _connectAccount.Value is { ChargesEnabled: false })
                {
                    b.Text([], "Billing actions disabled — complete Stripe Connect onboarding first. Status: " +
                        (_connectAccount.Value.RequirementsCurrentlyDue.Count > 0
                            ? $"{_connectAccount.Value.RequirementsCurrentlyDue.Count} requirements pending"
                            : "Awaiting capability activation"));
                }
                else
                {
                    b.Text([], "Billing not ready. See banner above.");
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

        if (string.IsNullOrEmpty(_billingActionStatus.Value))
        {
            return;
        }

        view.Box(["p-3 rounded-md bg-info/10 border border-info/30 text-info text-sm font-mono break-all"], content: s =>
        {
            s.Text([], _billingActionStatus.Value!);
        });
    }
}
