<!-- mined-from: Sentinel -->
# Tiny inline bar sparkline (from a list)

> **For real charts, use the first-class components — not this pattern.** Ikon.Parallax ships `view.PieChart`, `view.BarChart`, and `view.LineChart` (themed, interactive, with axes, legends, and tooltips). For any "show a pie / bar / line chart of this data" requirement — dashboards, analytics, breakdowns by category — use those (see the **Charts** guide). Do not hand-roll charts with `view.Box`, `conic-gradient`, or inline width styles.
>
> This pattern is ONLY a tiny **sparkline-style** bar strip drawn from `view.Box` divs, for when a full chart would be overkill.

A discrete bar sparkline drawn from `view.Box` divs. Aggregate a list into N buckets (24 hours, 7 days), find the max, then for each bucket pick a tailwind height class (`h-px / h-3 / h-5 / h-7 / h-10`) and a tint. Fits in a small card next to KPIs and updates the moment the underlying reactive list changes.

## When to use

Only for sparkline-class strips: a glanceable mini-bar next to other content (hourly event counts, daily attempts, weekly volume), with a small bar count (≤30) and no axes, legend, or tooltips. For a real bar chart — or any pie or line chart — use `view.BarChart` / `view.PieChart` / `view.LineChart` instead.

## Snippet

```csharp
private void RenderHourlyChart(UIView view)
{
    const int hours = 24;
    var now = DateTime.UtcNow;
    var counts = new int[hours];
    var severities = new Severity[hours];

    foreach (var ev in _events.Value)
    {
        var ageHours = (now - ev.LastSeen).TotalHours;
        if (ageHours < 0 || ageHours >= hours) continue;
        var idx = hours - 1 - (int)ageHours;
        counts[idx]++;
        if (ev.Severity > severities[idx]) severities[idx] = ev.Severity;
    }

    var max = 0;
    for (var i = 0; i < hours; i++) if (counts[i] > max) max = counts[i];

    view.Column(["px-4 pb-2 gap-1 border-t border-zinc-800 pt-2"], content: view =>
    {
        view.Row(["items-center justify-between"], content: v =>
        {
            v.Text(["text-xs text-zinc-500 font-medium"], "Last 24h");
            v.Text(["text-xs text-zinc-600"], $"max ×{max}");
        });

        view.Row(["items-end gap-px h-10"], content: view =>
        {
            for (var i = 0; i < hours; i++)
            {
                var count = counts[i];
                var sev = severities[i];

                var heightClass = (count, max) switch
                {
                    (0, _) => "h-px",
                    (_, 0) => "h-1",
                    _ => count switch
                    {
                        var c when c >= max     => "h-10",
                        var c when c * 2 >= max => "h-7",
                        var c when c * 3 >= max => "h-5",
                        _                       => "h-3"
                    }
                };

                // These tints are SEVERITY SEMANTICS (this app grades events: alert=rose,
                // watch=amber). A sparkline WITHOUT state semantics — activity volume, weekly
                // progress — uses ONE color: the app's brand accent (bg-brand-solid or the
                // committed accent class), with a muted track (bg-muted) for empty buckets.
                // Do not import this severity palette into a single-meaning chart.
                var colorClass = (count, sev) switch
                {
                    (0, _)               => "bg-zinc-800",
                    (_, Severity.Alert)  => "bg-rose-500",
                    (_, Severity.Watch)  => "bg-amber-500",
                    _                    => "bg-zinc-500"
                };

                view.Box([$"flex-1 {heightClass} {colorClass}"]);
            }
        });

        view.Row(["items-center justify-between"], content: v =>
        {
            v.Text(["text-xs text-zinc-600"], "24h ago");
            v.Text(["text-xs text-zinc-600"], $"now · {DateTime.Now.Hour:D2}:00");
        });
    });
}
```

## Notes

- Pick height classes from a fixed tailwind set (`h-px / h-1 / h-3 / h-5 / h-7 / h-10`). Crosswind purges unused tailwind classes — using `h-[{n}px]` arbitrary values for every bar would explode the generated CSS, while these few classes cost nothing.
- Track *both* count and worst-severity per bucket. A single Alert in a bucket of 4 events should color the bar red, not the dominant gray.
- The two end labels ("24h ago", "now · 14:00") give the chart a temporal anchor without needing axis ticks for every bar.
- `flex-1` on each bar + `gap-px` between them makes them auto-size to the container — you don't need to compute pixel widths.
- The bucket-walking loop runs every reactive update. For very large source lists, cache the buckets behind a derived reactive instead of recomputing here.

## See also

- `kpi-card-grid` — the headline numbers above the chart
- `score-bar-meter` — for bounded scores rather than counts
