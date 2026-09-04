namespace Ikon.App.Patterns.Patterns;

// Pattern: chart-axes-and-colors — see docs/patterns/chart-axes-and-colors.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class ChartAxesAndColors : IPatternDemo
{
    public string Slug => "chart-axes-and-colors";
    public string Title => "Chart axes, margins and colour";
    public string Category => "Visualization";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-chart-axes-and-colors
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
    #endregion
}
