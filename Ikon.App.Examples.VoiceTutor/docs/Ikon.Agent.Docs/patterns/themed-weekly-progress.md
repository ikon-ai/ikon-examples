<!-- mined-from: HabitPulse (live generated-app audit: off-palette orange bar + one-bar-in-a-void chart) -->
# Themed weekly progress chart (brand accent + full period structure)

A weekly/periodic progress chart that (1) renders in the app's OWN brand accent — never a
default palette color the theme doesn't contain — and (2) always shows its FULL period
structure: every day/slot gets a mark, with empty periods as a muted track instead of
disappearing. A chart that paints one lonely bar in an empty void reads as broken; a chart
whose color ignores the committed palette reads as off-brand. Both graded down by the visual
gate.

## When to use

Any single-meaning progress/volume chart: weekly consistency, daily activity, per-habit
streak history. (Charts with STATE SEMANTICS — severity, pass/fail thresholds — legitimately
use semantic colors per state; that is a different pattern.)

## Snippet

```csharp
private void RenderWeeklyChart(UIView view)
{
    // Aggregate: one bucket per weekday, EVERY weekday present — days without data get 0,
    // not omission. The chart's shape is the week, independent of how much data exists.
    var week = Enumerable.Range(0, 7)
        .Select(i => DateTime.Today.AddDays(-6 + i))
        .Select(day => new Dictionary<string, object>
        {
            ["day"] = day.ToString("ddd"),
            ["done"] = _completions.Count(c => c.Date.Date == day),
        })
        .ToList();

    // Single series + no colors: view.BarChart automatically uses the app's brand accent
    // (var(--color-brand-500)) — it follows the committed IkonTheme in BOTH light and dark.
    // Only pass colors:/colorScheme: when you have a REASON (multi-series, state semantics).
    view.BarChart(
        style: ["h-40 w-full"],
        data: week,
        keys: ["done"],
        indexBy: "day",
        padding: 0.35,
        borderRadius: 3,
        enableGridY: false,
        axisLeft: null,
        minValue: 0);
}
```

For a tiny hand-rolled sparkline strip (only when a full chart is overkill — see
`bar-chart-from-list`), the same two rules hold, expressed as classes:

```csharp
view.Row(["items-end gap-1.5 h-16"], content: v =>
{
    foreach (var (label, count) in week)
    {
        // Empty day: a short muted track mark — the day stays visible in the rhythm.
        var barClass = count == 0
            ? "h-1.5 bg-muted rounded-sm"
            : $"h-{Math.Clamp(count * 3, 3, 16)} bg-brand-solid rounded-sm";
        v.Box([$"flex-1 {barClass}"]);
    }
});
```

## Notes

- **Brand accent, one hue.** A progress chart has ONE meaning, so it gets ONE color — the
  committed accent. Reaching for amber/orange "for the partial days" imports severity
  semantics the data doesn't have (the exact defect this pattern was mined from).
- **Full structure always.** All 7 days render every time. `bg-muted` track marks for empty
  buckets keep the chart's silhouette stable from day one — critical because the first-boot
  state is the product shot (see the plan's DEMO CONTENT section).
- **Height on the style array** (`h-40 w-full`) — charts need an explicit height or they
  collapse.
