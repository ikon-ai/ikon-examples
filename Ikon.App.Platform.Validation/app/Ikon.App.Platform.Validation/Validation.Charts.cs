public partial class Validation
{
    private ChartTheme GetChartTheme() =>
        _currentTheme.Value == Constants.DarkTheme ? ChartThemes.DefaultDark : ChartThemes.DefaultLight;

    private void RenderChartsSection(UIView view)
    {
        var chartTheme = GetChartTheme();

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Bar Chart
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Bar Chart");
                view.Text([Text.Caption, "mb-4"], "Categorical data comparison with grouped bars");
                view.Box(["h-80"], content: v =>
                {
                    v.BarChart([Chart.Container],
                        theme: chartTheme,
                        data:
                        [
                            new Dictionary<string, object> { ["country"] = "USA", ["sales"] = 120, ["profit"] = 45 },
                            new Dictionary<string, object> { ["country"] = "UK", ["sales"] = 95, ["profit"] = 32 },
                            new Dictionary<string, object> { ["country"] = "Germany", ["sales"] = 110, ["profit"] = 38 },
                            new Dictionary<string, object> { ["country"] = "France", ["sales"] = 85, ["profit"] = 28 }
                        ],
                        keys: ["sales", "profit"],
                        indexBy: "country",
                        groupMode: BarGroupMode.Grouped,
                        margin: new ChartMargin { Top = 50, Right = 130, Bottom = 50, Left = 60 },
                        axisBottom: new AxisConfig { Legend = "Country", LegendOffset = 36, TickPadding = 5 },
                        axisLeft: new AxisConfig { Legend = "Amount ($K)", LegendOffset = -40, TickPadding = 5 },
                        legends:
                        [
                            new LegendConfig
                            {
                                Anchor = "bottom-right",
                                Direction = "column",
                                TranslateX = 120,
                                ItemWidth = 100,
                                ItemHeight = 20,
                                SymbolSize = 12
                            }
                        ]);
                });
            });

            // Stacked Bar Chart
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Stacked Bar Chart");
                view.Text([Text.Caption, "mb-4"], "Stacked bars showing composition");
                view.Box(["h-80"], content: v =>
                {
                    v.BarChart([Chart.Container],
                        theme: chartTheme,
                        data:
                        [
                            new Dictionary<string, object> { ["month"] = "Jan", ["desktop"] = 65, ["mobile"] = 35 },
                            new Dictionary<string, object> { ["month"] = "Feb", ["desktop"] = 59, ["mobile"] = 41 },
                            new Dictionary<string, object> { ["month"] = "Mar", ["desktop"] = 55, ["mobile"] = 45 },
                            new Dictionary<string, object> { ["month"] = "Apr", ["desktop"] = 50, ["mobile"] = 50 }
                        ],
                        keys: ["desktop", "mobile"],
                        indexBy: "month",
                        groupMode: BarGroupMode.Stacked,
                        margin: new ChartMargin { Top = 50, Right = 130, Bottom = 50, Left = 60 },
                        axisBottom: new AxisConfig { Legend = "Month", LegendOffset = 36, TickPadding = 5 },
                        axisLeft: new AxisConfig { Legend = "Visitors (%)", LegendOffset = -40, TickPadding = 5 },
                        legends:
                        [
                            new LegendConfig
                            {
                                Anchor = "bottom-right",
                                Direction = "column",
                                TranslateX = 120,
                                ItemWidth = 100,
                                ItemHeight = 20,
                                SymbolSize = 12
                            }
                        ]);
                });
            });

            // Line Chart
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Line Chart");
                view.Text([Text.Caption, "mb-4"], "Trends over time with area fill");
                view.Box(["h-80"], content: v =>
                {
                    v.LineChart([Chart.Container],
                        theme: chartTheme,
                        data:
                        [
                            new LineChartSeries
                            {
                                Id = "Revenue",
                                Data =
                                [
                                    new LineChartPoint { X = "Jan", Y = 100 },
                                    new LineChartPoint { X = "Feb", Y = 120 },
                                    new LineChartPoint { X = "Mar", Y = 145 },
                                    new LineChartPoint { X = "Apr", Y = 130 },
                                    new LineChartPoint { X = "May", Y = 165 },
                                    new LineChartPoint { X = "Jun", Y = 190 }
                                ]
                            },
                            new LineChartSeries
                            {
                                Id = "Expenses",
                                Data =
                                [
                                    new LineChartPoint { X = "Jan", Y = 80 },
                                    new LineChartPoint { X = "Feb", Y = 85 },
                                    new LineChartPoint { X = "Mar", Y = 90 },
                                    new LineChartPoint { X = "Apr", Y = 95 },
                                    new LineChartPoint { X = "May", Y = 100 },
                                    new LineChartPoint { X = "Jun", Y = 105 }
                                ]
                            }
                        ],
                        margin: new ChartMargin { Top = 50, Right = 110, Bottom = 50, Left = 60 },
                        yScaleMin: 0,
                        enableArea: true,
                        areaOpacity: 0.15,
                        areaBaselineValue: 0,
                        enablePoints: true,
                        pointSize: 8,
                        useMesh: true,
                        axisBottom: new AxisConfig { Legend = "Month", LegendOffset = 36, TickPadding = 5 },
                        axisLeft: new AxisConfig { Legend = "Amount ($K)", LegendOffset = -40, TickPadding = 5 },
                        legends:
                        [
                            new LegendConfig
                            {
                                Anchor = "bottom-right",
                                Direction = "column",
                                TranslateX = 100,
                                ItemWidth = 80,
                                ItemHeight = 20,
                                SymbolSize = 12
                            }
                        ]);
                });
            });

            // Pie Chart
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Pie Chart");
                view.Text([Text.Caption, "mb-4"], "Proportional relationships");
                view.Box(["h-80"], content: v =>
                {
                    v.PieChart([Chart.Container],
                        theme: chartTheme,
                        data:
                        [
                            new PieChartDatum { Id = "javascript", Label = "JavaScript", Value = 40 },
                            new PieChartDatum { Id = "python", Label = "Python", Value = 30 },
                            new PieChartDatum { Id = "csharp", Label = "C#", Value = 20 },
                            new PieChartDatum { Id = "other", Label = "Other", Value = 10 }
                        ],
                        margin: new ChartMargin { Top = 40, Right = 80, Bottom = 80, Left = 80 },
                        enableArcLinkLabels: true,
                        arcLinkLabelsSkipAngle: 10,
                        arcLinkLabelsThickness: 2,
                        enableArcLabels: true,
                        arcLabelsSkipAngle: 10,
                        legends:
                        [
                            new LegendConfig
                            {
                                Anchor = "bottom",
                                Direction = "row",
                                TranslateY = 56,
                                ItemWidth = 100,
                                ItemHeight = 18,
                                SymbolSize = 18
                            }
                        ]);
                });
            });

            // Donut Chart
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Donut Chart");
                view.Text([Text.Caption, "mb-4"], "Pie chart with inner radius");
                view.Box(["h-80"], content: v =>
                {
                    v.PieChart([Chart.Container],
                        theme: chartTheme,
                        data:
                        [
                            new PieChartDatum { Id = "completed", Label = "Completed", Value = 65 },
                            new PieChartDatum { Id = "in-progress", Label = "In Progress", Value = 25 },
                            new PieChartDatum { Id = "pending", Label = "Pending", Value = 10 }
                        ],
                        innerRadius: 0.5,
                        padAngle: 0.7,
                        cornerRadius: 3,
                        margin: new ChartMargin { Top = 40, Right = 80, Bottom = 80, Left = 80 },
                        enableArcLinkLabels: true,
                        activeOuterRadiusOffset: 8,
                        legends:
                        [
                            new LegendConfig
                            {
                                Anchor = "bottom",
                                Direction = "row",
                                TranslateY = 56,
                                ItemWidth = 100,
                                ItemHeight = 18,
                                SymbolSize = 18
                            }
                        ]);
                });
            });
        });
    }
}
