public partial class Validation
{
    private static readonly int[] CostsDayOptions = [7, 30, 90];

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
            view.Box([Card.Default, "p-6 mb-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Costs");
                view.Text([Text.Caption], "Query this space's AI usage credit costs through app.Costs (GetDailyCostsAsync + GetTotalCreditsAsync). Cost data comes from the analytics pipeline, so very recent usage can take a short while to appear.");
            });

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
                    view.Row([Layout.Row.Sm], content: view =>
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
            else if (!_costsLoading.Value)
            {
                view.Text([Text.Caption], "Press Refresh to load costs.");
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
            view.Text([Text.H3, "mb-2"], "By model");
            view.Text([Text.Caption, "mb-4"], "Daily rows aggregated per usage event name, most expensive first.");

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
                                view.Text([Text.Body, "font-mono text-sm truncate"], entry.Model);
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
            view.Text([Text.H3, "mb-2"], "Daily rows");
            view.Text([Text.Caption, "mb-4"], "Raw GetDailyCostsAsync results, newest first. Raw EUR shows only when the space has raw cost visibility.");

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
                                view.Text([Text.Body, "font-mono text-sm truncate"], row.EventName);
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
