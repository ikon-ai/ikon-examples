# Chart For The Question — Matching the Visual to What Is Being Asked

A dashboard is a set of answers, not a collection of charts. The defect is picking a chart because it looks good on a screenshot: a pie chart with eleven slices, a line chart over unordered categories, a bar chart where the user actually needed the exact numbers.

Pick from the question the surface exists to answer, and lead with the summary values before any chart.

| The question | The visual |
|---|---|
| "How is this trending?" — a measure over ordered time | `view.LineChart` |
| "Which is biggest?" — comparison or ranking across categories | `view.BarChart` |
| "What is it made of?" — part-to-whole, **few** meaningful slices, one point in time | `view.PieChart` (`innerRadius` for a donut) |
| "What exactly happened on the 14th?" — exact values, many fields, per-row work | A table. A chart does not replace one |
| "Where do we stand right now?" — one number that matters | A stat tile, no chart at all |

The platform ships **three** chart components. Anything else the question needs — distribution, funnel, waterfall, heatmap, cohort, scatter — is either a table or a hand-built SVG rendered server-side (`server-side-svg-visual`). Do not approximate them with a bar chart, and never hand-roll a chart from `view.Box` divs, `conic-gradient`, or percentage widths.

## When to use

Any surface that helps someone monitor, compare or decide from data. Also the corrective when a plan says "a chart of X" without saying what X is being asked about.

## Snippet

```csharp
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
```

## Notes

- **Pie charts degrade fast.** Above roughly six slices nobody can compare them; the guard here falls back to the bar ranking, which answers the same question better. A donut (`innerRadius: 0.6`) reads more cleanly than a full pie and leaves room for the total in the middle.
- Sort the bars. An unsorted category bar chart makes the reader do the ranking themselves, which was the entire question.
- `valueUnit:` gives tooltips and axis ticks human scaling — `"usd"`, `"percent"`, `"bytes"`, `"seconds"`, `"milliseconds"` are well known and anything else is appended as a suffix. Bare numbers on a money chart look unfinished.
- Axes render by default. You do not need `margin:` or `axisBottom:` just to get tick labels.
- Omit `theme:` and charts pick up sensible defaults in both schemes; for a dark-committed app pass the ready-made `theme: ChartThemes.DefaultDark`. Never hand-construct a `ChartTheme` — it has no flat colour properties.
- A datum's `Color` is honoured when set, so charts can be pulled onto the app's committed palette; leave it unset and they use the theme palette, which is already coherent. Do not pass a second accent family just to make the chart colourful.
- If the underlying numbers are demo seed data, they are records, not claims — do not seed a chart that implies a real-world outcome (a settled payment, a confirmed external status).
- An empty chart is not an empty state. With no data, render the collection's zero-results state instead of an axis with nothing on it.

## See also

- `server-side-svg-visual` — the escape hatch for gauges, rings and anything the three components do not cover.
- `record-list-toolbar` — the table half of a dashboard, for the exact-values question.
- `zero-results-state` — what the surface shows before any data exists.
- `theme-commitment` — pulling chart colours onto the app's committed palette.
