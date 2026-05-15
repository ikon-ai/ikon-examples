<!-- mined-from: Sentinel -->
# Bar Chart From List — Time-bucketed activity histogram

A discrete bar chart drawn entirely from `view.Box` divs — no SVG, no chart library. Aggregate a list into N time buckets (24 hours, 7 days), find the max, then for each bucket pick a tailwind height class (`h-px / h-3 / h-5 / h-7 / h-10`) and a severity-tinted color class. Fits in a small card next to KPIs and updates the moment the underlying reactive list changes.

## When to use

When you want a sparkline-class chart without pulling in a charting library: hourly event counts, daily attempts, weekly volume. Best when the bar count is small (≤30) and you can decide bucket height with a `switch` on coarse ratios. For higher resolution or interactive tooltips, reach for an SVG-based component.

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
