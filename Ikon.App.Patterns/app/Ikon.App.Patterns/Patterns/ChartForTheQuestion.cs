namespace Ikon.App.Patterns.Patterns;

// Pattern: chart-for-the-question — see docs/patterns/chart-for-the-question.md.
// The docsnippet region leads with the summary values and then picks each chart from the question
// it answers; the stubs outside it stand in for the two aggregates a real dashboard would compute.
internal sealed class ChartForTheQuestion : IPatternDemo
{
    public string Slug => "chart-for-the-question";
    public string Title => "Chart for the question";
    public string Category => "Data";
    public void RenderDemo(IView view) => RenderDashboard(view);

    private sealed record SpendCategory(string Name, double Total);
    private sealed record Day(string Label, double Amount);

    private readonly ReactiveList<SpendCategory> _categories = new();
    private readonly ReactiveList<Day> _days = new();

    public ChartForTheQuestion()
    {
        _categories.AddRange(
        [
            new SpendCategory("Groceries", 412.50),
            new SpendCategory("Transport", 188.00),
            new SpendCategory("Eating out", 96.25),
        ]);

        _days.AddRange(Enumerable.Range(1, 14)
            .Select(d => new Day($"{d}", 20 + (d * 7 % 40))));
    }

    #region docsnippet:pattern-chart-for-the-question
    private void RenderDashboard(IView view)
    {
        var total = _categories.Sum(c => c.Total);
        var top = _categories.OrderByDescending(c => c.Total).FirstOrDefault();

        // Lead with the answer, not the chart. The number a person came for goes first.
        view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
        {
            RenderStat(view, "wallet", "Spent this month", total.ToString("C0"));
            RenderStat(view, "trending-up", "Largest category", top?.Name ?? "—");
        });

        // "How is this trending?" — ordered time on X → line.
        view.Column([Card.Default, "p-4", Layout.Column.Sm], content: view =>
        {
            view.Text([Text.H3], text: "Daily spend");
            view.LineChart(["h-64 w-full"], valueUnit: "usd", data:
            [
                new LineChartSeries
                {
                    Id = "Daily",
                    Data = _days.Select(d => new LineChartPoint { X = d.Label, Y = d.Amount }),
                },
            ]);
        });

        // "Which is biggest?" — ranking across categories → bar, sorted.
        view.Column([Card.Default, "p-4", Layout.Column.Sm], content: view =>
        {
            view.Text([Text.H3], text: "By category");
            view.BarChart(["h-64 w-full"], indexBy: "category", keys: ["spend"], valueUnit: "usd",
                data: _categories
                    .OrderByDescending(c => c.Total)
                    .Select(c => new Dictionary<string, object>
                    {
                        ["category"] = c.Name,
                        ["spend"] = c.Total,
                    }));
        });

        // "What is it made of?" — part-to-whole, and ONLY while the slice count stays readable.
        // Past a handful of slices a pie stops being comparable; fall back to the ranking.
        if (_categories.Count is > 1 and <= 6)
        {
            view.Column([Card.Default, "p-4", Layout.Column.Sm], content: view =>
            {
                view.Text([Text.H3], text: "Share of spend");
                view.PieChart(["h-64 w-64"], innerRadius: 0.6, data: _categories.Select(c =>
                    new PieChartDatum { Id = c.Name, Label = c.Name, Value = c.Total }));
            });
        }
    }

    private static void RenderStat(IView view, string icon, string label, string value)
    {
        view.Column([StatCard.Root, "min-w-48 flex-1"], content: view =>
        {
            view.Box([StatCard.IconBox], content: v => v.Icon([StatCard.IconSize], name: icon));
            view.Text([StatCard.Label], text: label);
            view.Text([StatCard.Value], text: value);
        });
    }
    #endregion
}
