public partial class Validation
{
    private static readonly int[] CostsDayOptions = [7, 30, 90];

    // Far below one Haiku call, so the first call alone carries the budget past its cap.
    private const double BudgetCheckMaxCredits = 0.000001;
    private static readonly TimeSpan BudgetFlushTimeout = TimeSpan.FromSeconds(30);
    // CreditLimitChecker caches a verdict for 3 s per scope set; waiting past it makes the second check fresh.
    private static readonly TimeSpan BudgetVerdictCacheWait = TimeSpan.FromSeconds(4);

    private readonly ClientReactive<bool> _costsSpendLoading = new(false);
    private readonly ClientReactive<SpendStatus?> _costsSpendStatus = new(null);
    private readonly ClientReactive<string?> _costsSpendResult = new(null);
    private readonly ClientReactive<bool> _costsBudgetRunning = new(false);
    private readonly ClientReactive<string> _costsBudgetProgress = new("");
    private readonly ClientReactive<string?> _costsBudgetResult = new(null);

    private readonly Reactive<int> _costsDays = new(30);
    private readonly Reactive<string> _costsCategory = new("");
    private readonly Reactive<bool> _costsLoading = new(false);
    private readonly Reactive<string?> _costsError = new(null);
    private readonly ReactiveList<DailyCost> _costsRows = new();
    private readonly Reactive<double?> _costsTotalCredits = new(null);
    private readonly Reactive<(DateOnly Start, DateOnly End)?> _costsRange = new(null);

    private void RenderCostsSection(UIView view)
    {
        if (RenderSectionLocked(view, "Costs"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Costs");

            RenderCostsSpendStatusCard(view);
            RenderCostsBudgetCard(view);
            RenderCostsQueryCard(view);

            if (_costsRows.Value.Count > 0)
            {
                RenderCostsModelSummaryCard(view);
                RenderCostsDailyRowsCard(view);
            }
        });
    }

    private void RenderCostsQueryCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Query");

            view.Row([Layout.Row.Md, "items-end mb-4 flex-wrap"], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Days");
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        foreach (var days in CostsDayOptions)
                        {
                            view.Button(
                                [_costsDays.Value == days ? Button.PrimaryMd : Button.OutlineMd],
                                text: days.ToString(),
                                onClick: async () => _costsDays.Value = days);
                        }
                    });
                });

                view.Box([FormField.Root, "flex-1 min-w-[200px]"], content: view =>
                {
                    view.Text([FormField.Label], "Category filter (e.g. llm)");
                    view.TextField(
                        [Input.Default],
                        value: _costsCategory.Value,
                        placeholder: "All categories",
                        onValueChange: async v => _costsCategory.Value = v ?? "");
                });

                view.Button(
                    [Button.PrimaryMd],
                    text: "Refresh",
                    disabled: _costsLoading.Value,
                    onClick: RefreshCostsAsync);

                if (_costsLoading.Value)
                {
                    view.Box([Icon.Spinner]);
                }
            });

            if (!string.IsNullOrEmpty(_costsError.Value))
            {
                view.Box([Alert.Error, "mb-4"], content: view =>
                {
                    view.Text([Alert.Description], _costsError.Value);
                });
            }

            if (_costsTotalCredits.Value is { } total && _costsRange.Value is { } range)
            {
                view.Row([Layout.Row.InlineCenter, "gap-2 flex-wrap"], content: view =>
                {
                    view.Text([Text.BodyStrong], $"Space total: {total:F2} credits");

                    if (!string.IsNullOrWhiteSpace(_costsCategory.Value))
                    {
                        view.Text([Text.BodyStrong], $"· Filtered: {_costsRows.Value.Sum(r => r.Credits):F2} credits");
                    }

                    view.Text([Text.Caption], $"{range.Start:yyyy-MM-dd} to {range.End:yyyy-MM-dd}, {_costsRows.Value.Count} daily row(s)");
                });
            }
        });
    }

    private void RenderCostsModelSummaryCard(UIView view)
    {
        var perModel = _costsRows.Value
            .GroupBy(row => (row.EventName, row.Category))
            .Select(group => (
                Model: group.Key.EventName,
                group.Key.Category,
                TotalUsage: group.Sum(row => row.TotalUsage),
                Credits: group.Sum(row => row.Credits)))
            .OrderByDescending(entry => entry.Credits)
            .ToList();

        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "By model");

            view.Column([Layout.Column.Sm], content: view =>
            {
                foreach (var entry in perModel)
                {
                    view.Box([Card.Elevated, "p-3"], content: view =>
                    {
                        view.Row(["items-center justify-between gap-3 flex-wrap"], content: view =>
                        {
                            view.Column([Layout.Column.Xs, "min-w-0"], content: view =>
                            {
                                view.Text([Text.Body, "text-sm truncate"], entry.Model);
                                view.Text([Text.Caption], $"{entry.Category} · usage {entry.TotalUsage:#,##0.##}");
                            });

                            view.Text([Text.BodyStrong, "shrink-0"], $"{entry.Credits:F2} cr");
                        });
                    });
                }
            });
        });
    }

    private void RenderCostsDailyRowsCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Daily rows");

            view.Column([Layout.Column.Sm], content: view =>
            {
                foreach (var row in _costsRows.Value.OrderByDescending(r => r.Date).ThenByDescending(r => r.Credits))
                {
                    view.Box([Card.Elevated, "p-3"], content: view =>
                    {
                        view.Row(["items-center justify-between gap-3 flex-wrap"], content: view =>
                        {
                            view.Column([Layout.Column.Xs, "min-w-0"], content: view =>
                            {
                                view.Text([Text.Body, "text-sm truncate"], row.EventName);
                                view.Text([Text.Caption], $"{row.Date:yyyy-MM-dd} · {row.Category} · usage {row.TotalUsage:#,##0.##}");
                            });

                            view.Column([Layout.Column.Xs, "items-end shrink-0"], content: view =>
                            {
                                view.Text([Text.BodyStrong], $"{row.Credits:F2} cr");

                                if (row.RawCostEur is { } eur)
                                {
                                    view.Text([Text.Caption], $"{eur:F4} EUR");
                                }
                            });
                        });
                    });
                }
            });
        });
    }

    private void RenderCostsSpendStatusCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Spend limits");

            view.Row([Layout.Row.Md, "items-center mb-4 flex-wrap"], content: view =>
            {
                view.Button(
                    [Button.PrimaryMd],
                    text: "Load spend status",
                    disabled: _costsSpendLoading.Value,
                    onClick: LoadSpendStatusAsync,
                    props: TestId("cost-spend-load"));

                if (_costsSpendLoading.Value)
                {
                    view.Spinner();
                }
            });

            if (_costsSpendResult.Value is { } result)
            {
                view.Text([Text.Body, result.StartsWith("PASS", StringComparison.Ordinal) ? "text-success-primary" : "text-error-primary"], result, props: TestId("cost-spend-status"));
            }

            if (_costsSpendStatus.Value is not { } status)
            {
                return;
            }

            view.Column([Layout.Column.Sm, "mt-4"], content: view =>
            {
                foreach (var counter in status.Counters)
                {
                    view.Box([Card.Elevated, "p-3"], key: $"counter-{counter.Scope}-{counter.Limit}", content: view =>
                    {
                        view.Row(["items-center justify-between gap-3 flex-wrap"], content: view =>
                        {
                            view.Column([Layout.Column.Xs, "min-w-0"], content: view =>
                            {
                                view.Text([Text.Body, "text-sm truncate"], $"{counter.Scope} · {counter.Limit}");
                                view.Text([Text.Caption, "truncate"], counter.ScopeId + (counter.ResetsAt is { } resetsAt ? $" · resets {resetsAt:yyyy-MM-dd HH:mm} UTC" : " · lifetime"));
                            });

                            view.Text(
                                [Text.BodyStrong, "shrink-0", counter.Exceeded ? "text-error-primary" : ""],
                                $"{counter.CurrentCredits:0.####} / {(counter.LimitCredits > 0 ? counter.LimitCredits.ToString("0.####") : "no cap")} cr" + (counter.Exceeded ? " EXCEEDED" : ""),
                                props: TestId("cost-spend-counter"));
                        });
                    });
                }

                foreach (var breach in status.Breaches)
                {
                    view.Box([Alert.Error], key: $"breach-{breach.Scope}-{breach.Limit}", content: view =>
                    {
                        view.Text([Alert.Description], $"{breach.Describe()} (action {breach.Action})", props: TestId("cost-spend-breach"));
                    });
                }
            });
        });
    }

    private void RenderCostsBudgetCard(UIView view)
    {
        view.Box([Card.Default, "p-6 mb-6"], content: view =>
        {
            // app.Costs.Budget around two tiny LLM calls: the first runs (the counter is still zero), the
            // ledger flushes its usage past the cap, and in enforce mode the second must throw
            // SpendLimitExceededException with Breach.Scope == "budget". Warn and off modes only count,
            // so there the check ends at the ledger.
            view.Text([Text.H3, "mb-2"], "Operation budget");
            view.Text([Text.Caption, "mb-4"], $"{BudgetCheckMaxCredits} credit cap · spends one cheap LLM call");

            view.Row([Layout.Row.Md, "items-center mb-4 flex-wrap"], content: view =>
            {
                view.Button(
                    [Button.PrimaryMd],
                    text: "Run budget check",
                    disabled: _costsBudgetRunning.Value,
                    onClick: RunBudgetCheckAsync,
                    props: TestId("cost-budget-run"));

                if (_costsBudgetRunning.Value)
                {
                    view.Spinner();
                }
            });

            if (!string.IsNullOrEmpty(_costsBudgetProgress.Value))
            {
                view.Text([Text.Caption, "mb-2"], _costsBudgetProgress.Value, props: TestId("cost-budget-progress"));
            }

            if (_costsBudgetResult.Value is { } result)
            {
                view.Text(
                    [Text.Body, result.StartsWith("PASS", StringComparison.Ordinal) ? "text-success-primary" : result.StartsWith("SKIP", StringComparison.Ordinal) ? "text-warning-primary" : "text-error-primary"],
                    result,
                    props: TestId("cost-budget-result"));
            }
        });
    }

    private async Task LoadSpendStatusAsync()
    {
        _costsSpendLoading.Value = true;

        try
        {
            var status = await app.Costs.GetSpendStatusAsync();
            _costsSpendStatus.Value = status;
            _costsSpendResult.Value = $"PASS spend status: Applicable={status.Applicable} Mode={status.Mode} OnExceeded={status.OnExceeded} Counters={status.Counters.Count} Breaches={status.Breaches.Count}";
        }
        catch (Exception ex)
        {
            _costsSpendStatus.Value = null;
            _costsSpendResult.Value = $"FAIL spend status: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _costsSpendLoading.Value = false;
        }
    }

    private async Task RunBudgetCheckAsync()
    {
        if (_costsBudgetRunning.Value)
        {
            return;
        }

        _costsBudgetRunning.Value = true;
        _costsBudgetResult.Value = null;
        _costsBudgetProgress.Value = "Reading the spend mode";

        try
        {
            _costsBudgetResult.Value = await BudgetCheckAsync(progress => _costsBudgetProgress.Value = progress);
        }
        catch (Exception ex)
        {
            _costsBudgetResult.Value = $"FAIL budget check: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _costsBudgetRunning.Value = false;
            _costsBudgetProgress.Value = "";
        }
    }

    private async Task<string> BudgetCheckAsync(Action<string> progress)
    {
        // A budget only denies where the platform enforces spend limits; in warn and off modes the
        // ledger still counts the budget but the call runs, and a billing-exempt organisation is never limited.
        var status = await app.Costs.GetSpendStatusAsync();

        if (!status.Applicable)
        {
            return $"SKIP budget check: spend limits do not apply to this billing-exempt organisation (Applicable=False Mode={status.Mode})";
        }

        if (status.Mode == "enforce" && status.Breaches.Count > 0)
        {
            return $"SKIP budget check: a platform cap is already exceeded, so every AI call is denied: {status.Breaches[0].Describe()}";
        }

        using var budget = app.Costs.Budget("validation-budget-check", BudgetCheckMaxCredits);
        var nonce = Guid.NewGuid().ToString("N")[..8];
        var firstCallAt = DateTime.UtcNow;

        progress("First call under the budget");

        try
        {
            await Emerge.AskAsync($"Reply with the single word OK. ({nonce}-1)", LLMModel.Default);
        }
        catch (Exception ex) when (SpendLimitExceededException.Find(ex) is { } denied)
        {
            return $"FAIL first call was denied before the budget had spent anything: {denied.Breach?.Describe() ?? denied.Message}";
        }

        // The platform checks a budget's counter before each call, and the counter moves only when the
        // ledger flushes the first call's usage a few seconds later. Asking again before that would run.
        var spent = 0.0;
        var flushDeadline = DateTime.UtcNow + BudgetFlushTimeout;

        while (true)
        {
            spent = await budget.SpentCreditsAsync();
            progress($"Waiting for the ledger: spent {spent:0.######} of {budget.MaxCredits} credits");

            if (spent >= budget.MaxCredits)
            {
                break;
            }

            if (DateTime.UtcNow > flushDeadline)
            {
                return $"FAIL budget never counted the first call: SpentCreditsAsync={spent:0.######} after {BudgetFlushTimeout.TotalSeconds:0}s (budget {budget.Id})";
            }

            await Task.Delay(1000);
        }

        if (status.Mode != "enforce")
        {
            return $"PASS budget counted past its cap: spent {spent:0.######} >= {budget.MaxCredits} credits (Mode={status.Mode}, so the platform records the crossing without denying)";
        }

        // The allow verdict for this budget is cached for a few seconds on this server.
        var sinceFirstCall = DateTime.UtcNow - firstCallAt;

        if (sinceFirstCall < BudgetVerdictCacheWait)
        {
            await Task.Delay(BudgetVerdictCacheWait - sinceFirstCall);
        }

        progress("Second call, which the budget must deny");

        try
        {
            await Emerge.AskAsync($"Reply with the single word OK. ({nonce}-2)", LLMModel.Default);
            return $"FAIL second call ran although the budget had spent {spent:0.######} >= {budget.MaxCredits} credits";
        }
        catch (Exception ex) when (SpendLimitExceededException.Find(ex) is { } denied)
        {
            if (denied.Breach is not { Scope: "budget" } breach)
            {
                return $"FAIL second call was denied by another cap (Scope={denied.Breach?.Scope ?? "none"}): {denied.Message}";
            }

            return $"PASS budget tripped: {breach.Describe()}";
        }
    }

    private async Task RefreshCostsAsync()
    {
        if (_costsLoading.Value)
        {
            return;
        }

        _costsLoading.Value = true;
        _costsError.Value = null;

        try
        {
            var end = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = end.AddDays(-(_costsDays.Value - 1));
            var query = new CostQuery(start, end, Category: NullIfEmpty(_costsCategory.Value));

            var rows = await app.Costs.GetDailyCostsAsync(query);
            var total = await app.Costs.GetTotalCreditsAsync(start, end);

            _costsRows.ReplaceAll(rows);
            _costsTotalCredits.Value = total;
            _costsRange.Value = (start, end);
        }
        catch (Exception ex)
        {
            _costsError.Value = ex.Message;
        }
        finally
        {
            _costsLoading.Value = false;
        }
    }
}
