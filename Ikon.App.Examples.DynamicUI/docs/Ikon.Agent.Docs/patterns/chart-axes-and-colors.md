<!-- mined-from: Ikon.App.Patterns -->
# Chart Axes, Margins And Colour — Where The Labels Actually Go

The chart components take their configuration as records rather than style classes, and the one
that catches people is **`ChartMargin`**: axis legends and rotated tick labels render *inside* the
chart box, so a legend with no margin reserved for it is simply clipped away. The chart looks fine
and the label is gone.

## When to use

Any chart that needs axis labels, a specific tick density, or colours that mean something. For
choosing *which* chart, see `chart-for-the-question`.

## Notes

- **`LineChartSeries` and `LineChartPoint` are object-initialized with `required` members**, not
  positional — there is no two-argument constructor.
- **`LineChartPoint.X` is `object` on purpose**: a string label for point scales, a number for
  linear and time scales. The mixed type is the API, not a lapse.
- **`AxisConfig.LegendOffset` is measured outward**, so it has to fit inside the corresponding
  `ChartMargin` side. A left legend usually takes a negative offset.
- **`TickCount` asks for approximately N evenly-spaced ticks instead of one per data point** —
  that is the fix for a crowded axis, rather than rotating labels until they fit.
- `AxisConfig.Format` for a time scale is a **d3-time-format** token (`"%H:%M"`), not a .NET format
  string.
- `Hidden = true` drops an axis entirely. Right for a sparkline; wrong for anything a reader has to
  take a number from.
- `ChartColorScheme` keeps series colours consistent across a chart; an explicit `colors` list
  overrides it, which is what you want when a series' colour carries meaning (red = churn).
- `ChartTheme` (`ChartThemes.DefaultLight`/`DefaultDark`) styles text, grid and tooltip; per-slot
  records like `ChartTooltipStyle` sit under it.

## Snippet

```csharp
// Both records are object-initialized with REQUIRED members, not positional -- there is no
// two-argument constructor. LineChartPoint.X is `object` on purpose: a string label for point
// scales, a number for linear and time ones.
private static readonly LineChartSeries[] Series =
[
    new()
    {
        Id = "Signups",
        Data = [new() { X = 1, Y = 12 }, new() { X = 2, Y = 19 },
                new() { X = 3, Y = 15 }, new() { X = 4, Y = 27 }],
    },
    new()
    {
        Id = "Churn",
        Data = [new() { X = 1, Y = 3 }, new() { X = 2, Y = 5 },
                new() { X = 3, Y = 4 }, new() { X = 4, Y = 6 }],
    },
];

private void Render(IView view)
{
    view.LineChart(
        ["h-64"],
        data: Series,

        // Margin is not decoration: axis legends and rotated tick labels render INSIDE the
        // chart box, so a legend with no margin for it is simply clipped away.
        margin: new ChartMargin { Top = 16, Right = 24, Bottom = 48, Left = 56 },

        axisBottom: new AxisConfig
        {
            Legend = "Week",
            // LegendOffset moves the legend away from the ticks; it is measured outward, so
            // it has to fit inside the margin above.
            LegendOffset = 36,
            // TickCount asks for approximately N evenly-spaced ticks INSTEAD of one per data
            // point -- the fix for a crowded axis, rather than rotating labels.
            TickCount = 4,
        },

        axisLeft: new AxisConfig { Legend = "People", LegendOffset = -44 },

        // Hidden drops an axis entirely, which is right for a sparkline and wrong for
        // anything a reader has to take a number from.
        axisRight: new AxisConfig { Hidden = true },

        // One scheme for the whole chart keeps series colours consistent; an explicit colors
        // list overrides it when a series has a meaning attached to its colour.
        colorScheme: ChartColorScheme.Category10,
        enableGridY: true,
        enablePoints: true);
}
```

## See also

- `chart-for-the-question` — matching line / bar / pie / table to what is being asked.
- `server-side-svg-visual` — when the platform ships no such chart.
