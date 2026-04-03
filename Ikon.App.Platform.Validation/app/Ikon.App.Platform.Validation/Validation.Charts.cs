public partial class Validation
{
    private ChartTheme GetChartTheme() =>
        _currentTheme.Value == Constants.DarkTheme ? ChartThemes.DefaultDark : ChartThemes.DefaultLight;

    private static LineChartSeries[] GenerateDenseLineData()
    {
        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        int[] years = [22, 23, 24, 25];

        var labels = new List<string>();

        foreach (var y in years)
        {
            foreach (var m in months)
            {
                labels.Add($"{m} {y}");
            }
        }

        double Noise(int seed, int i) => Math.Sin(seed * 17.3 + i * 2.7) * 0.15 + 1.0;

        LineChartPoint[] MakeSeries(double baseVal, double growth, int seed)
        {
            var points = new LineChartPoint[labels.Count];

            for (int i = 0; i < labels.Count; i++)
            {
                var seasonal = Math.Sin(i * Math.PI / 6) * baseVal * 0.08;
                var trend = baseVal + growth * i + seasonal;
                var value = Math.Round(trend * Noise(seed, i));
                points[i] = new LineChartPoint { X = labels[i], Y = value };
            }

            return points;
        }

        return
        [
            new LineChartSeries { Id = "API Requests", Data = MakeSeries(1200, 55, 1) },
            new LineChartSeries { Id = "Database Queries", Data = MakeSeries(900, 45, 2) },
            new LineChartSeries { Id = "Cache Hits", Data = MakeSeries(700, 40, 3) },
            new LineChartSeries { Id = "Auth Events", Data = MakeSeries(400, 25, 4) },
            new LineChartSeries { Id = "Background Jobs", Data = MakeSeries(300, 20, 5) },
            new LineChartSeries { Id = "WebSocket Messages", Data = MakeSeries(550, 35, 6) },
            new LineChartSeries { Id = "Error Rate", Data = MakeSeries(50, 2, 7) },
            new LineChartSeries { Id = "CDN Requests", Data = MakeSeries(800, 50, 8) }
        ];
    }

    private void RenderChartsSection(UIView view)
    {
        var chartTheme = GetChartTheme();

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Bar Chart
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Bar Chart");
                view.Text([Text.Caption, "mb-4"], "12 categories with 6 grouped keys");
                view.Box(["h-80"], content: v =>
                {
                    v.BarChart([Chart.Container],
                        theme: chartTheme,
                        data:
                        [
                            new Dictionary<string, object> { ["product"] = "Analytics Platform", ["q1"] = 142, ["q2"] = 158, ["q3"] = 135, ["q4"] = 172, ["q1Forecast"] = 155, ["q2Forecast"] = 168 },
                            new Dictionary<string, object> { ["product"] = "Cloud Storage", ["q1"] = 118, ["q2"] = 124, ["q3"] = 131, ["q4"] = 145, ["q1Forecast"] = 130, ["q2Forecast"] = 140 },
                            new Dictionary<string, object> { ["product"] = "API Gateway", ["q1"] = 95, ["q2"] = 112, ["q3"] = 128, ["q4"] = 156, ["q1Forecast"] = 140, ["q2Forecast"] = 160 },
                            new Dictionary<string, object> { ["product"] = "Auth Service", ["q1"] = 82, ["q2"] = 78, ["q3"] = 91, ["q4"] = 88, ["q1Forecast"] = 85, ["q2Forecast"] = 92 },
                            new Dictionary<string, object> { ["product"] = "CDN", ["q1"] = 64, ["q2"] = 71, ["q3"] = 68, ["q4"] = 75, ["q1Forecast"] = 72, ["q2Forecast"] = 78 },
                            new Dictionary<string, object> { ["product"] = "Database", ["q1"] = 108, ["q2"] = 115, ["q3"] = 122, ["q4"] = 138, ["q1Forecast"] = 128, ["q2Forecast"] = 142 },
                            new Dictionary<string, object> { ["product"] = "Monitoring", ["q1"] = 55, ["q2"] = 62, ["q3"] = 58, ["q4"] = 65, ["q1Forecast"] = 60, ["q2Forecast"] = 68 },
                            new Dictionary<string, object> { ["product"] = "Message Queue", ["q1"] = 44, ["q2"] = 51, ["q3"] = 48, ["q4"] = 55, ["q1Forecast"] = 50, ["q2Forecast"] = 58 },
                            new Dictionary<string, object> { ["product"] = "Search Engine", ["q1"] = 72, ["q2"] = 68, ["q3"] = 85, ["q4"] = 92, ["q1Forecast"] = 88, ["q2Forecast"] = 95 },
                            new Dictionary<string, object> { ["product"] = "ML Pipeline", ["q1"] = 38, ["q2"] = 45, ["q3"] = 52, ["q4"] = 68, ["q1Forecast"] = 62, ["q2Forecast"] = 75 },
                            new Dictionary<string, object> { ["product"] = "Edge Functions", ["q1"] = 28, ["q2"] = 35, ["q3"] = 42, ["q4"] = 55, ["q1Forecast"] = 50, ["q2Forecast"] = 62 },
                            new Dictionary<string, object> { ["product"] = "Video Streaming", ["q1"] = 52, ["q2"] = 48, ["q3"] = 61, ["q4"] = 58, ["q1Forecast"] = 55, ["q2Forecast"] = 64 }
                        ],
                        keys: ["q1", "q2", "q3", "q4", "q1Forecast", "q2Forecast"],
                        indexBy: "product",
                        groupMode: BarGroupMode.Grouped,
                        labelSkipWidth: 16,
                        labelSkipHeight: 16,
                        margin: new ChartMargin { Top = 50, Right = 140, Bottom = 80, Left = 60 },
                        axisBottom: new AxisConfig { Legend = "Product", LegendOffset = 60, TickPadding = 5, TickRotation = -35, TruncateTickAt = 12 },
                        axisLeft: new AxisConfig { Legend = "Revenue ($M)", LegendOffset = -45, TickPadding = 5 },
                        legends:
                        [
                            new LegendConfig
                            {
                                Anchor = "bottom-right",
                                Direction = "column",
                                TranslateX = 130,
                                ItemWidth = 110,
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
                view.Text([Text.Caption, "mb-4"], "24 months with 6 stacked categories");
                view.Box(["h-80"], content: v =>
                {
                    v.BarChart([Chart.Container],
                        theme: chartTheme,
                        data:
                        [
                            new Dictionary<string, object> { ["month"] = "Jan 24", ["organic"] = 42, ["paid"] = 28, ["referral"] = 15, ["direct"] = 12, ["social"] = 8, ["email"] = 5 },
                            new Dictionary<string, object> { ["month"] = "Feb 24", ["organic"] = 38, ["paid"] = 32, ["referral"] = 18, ["direct"] = 10, ["social"] = 11, ["email"] = 6 },
                            new Dictionary<string, object> { ["month"] = "Mar 24", ["organic"] = 45, ["paid"] = 35, ["referral"] = 12, ["direct"] = 14, ["social"] = 9, ["email"] = 7 },
                            new Dictionary<string, object> { ["month"] = "Apr 24", ["organic"] = 51, ["paid"] = 30, ["referral"] = 20, ["direct"] = 16, ["social"] = 12, ["email"] = 8 },
                            new Dictionary<string, object> { ["month"] = "May 24", ["organic"] = 48, ["paid"] = 38, ["referral"] = 22, ["direct"] = 11, ["social"] = 14, ["email"] = 6 },
                            new Dictionary<string, object> { ["month"] = "Jun 24", ["organic"] = 55, ["paid"] = 42, ["referral"] = 19, ["direct"] = 15, ["social"] = 10, ["email"] = 9 },
                            new Dictionary<string, object> { ["month"] = "Jul 24", ["organic"] = 52, ["paid"] = 45, ["referral"] = 16, ["direct"] = 18, ["social"] = 13, ["email"] = 7 },
                            new Dictionary<string, object> { ["month"] = "Aug 24", ["organic"] = 49, ["paid"] = 40, ["referral"] = 21, ["direct"] = 13, ["social"] = 11, ["email"] = 8 },
                            new Dictionary<string, object> { ["month"] = "Sep 24", ["organic"] = 58, ["paid"] = 36, ["referral"] = 24, ["direct"] = 17, ["social"] = 15, ["email"] = 10 },
                            new Dictionary<string, object> { ["month"] = "Oct 24", ["organic"] = 62, ["paid"] = 44, ["referral"] = 18, ["direct"] = 20, ["social"] = 12, ["email"] = 9 },
                            new Dictionary<string, object> { ["month"] = "Nov 24", ["organic"] = 56, ["paid"] = 48, ["referral"] = 25, ["direct"] = 14, ["social"] = 16, ["email"] = 11 },
                            new Dictionary<string, object> { ["month"] = "Dec 24", ["organic"] = 64, ["paid"] = 52, ["referral"] = 22, ["direct"] = 19, ["social"] = 13, ["email"] = 10 },
                            new Dictionary<string, object> { ["month"] = "Jan 25", ["organic"] = 58, ["paid"] = 45, ["referral"] = 20, ["direct"] = 16, ["social"] = 14, ["email"] = 8 },
                            new Dictionary<string, object> { ["month"] = "Feb 25", ["organic"] = 52, ["paid"] = 50, ["referral"] = 24, ["direct"] = 18, ["social"] = 17, ["email"] = 9 },
                            new Dictionary<string, object> { ["month"] = "Mar 25", ["organic"] = 65, ["paid"] = 48, ["referral"] = 19, ["direct"] = 21, ["social"] = 15, ["email"] = 11 },
                            new Dictionary<string, object> { ["month"] = "Apr 25", ["organic"] = 70, ["paid"] = 55, ["referral"] = 26, ["direct"] = 22, ["social"] = 18, ["email"] = 12 },
                            new Dictionary<string, object> { ["month"] = "May 25", ["organic"] = 68, ["paid"] = 58, ["referral"] = 28, ["direct"] = 19, ["social"] = 20, ["email"] = 10 },
                            new Dictionary<string, object> { ["month"] = "Jun 25", ["organic"] = 72, ["paid"] = 52, ["referral"] = 23, ["direct"] = 24, ["social"] = 16, ["email"] = 13 },
                            new Dictionary<string, object> { ["month"] = "Jul 25", ["organic"] = 75, ["paid"] = 60, ["referral"] = 21, ["direct"] = 20, ["social"] = 19, ["email"] = 11 },
                            new Dictionary<string, object> { ["month"] = "Aug 25", ["organic"] = 69, ["paid"] = 56, ["referral"] = 27, ["direct"] = 23, ["social"] = 17, ["email"] = 14 },
                            new Dictionary<string, object> { ["month"] = "Sep 25", ["organic"] = 78, ["paid"] = 62, ["referral"] = 30, ["direct"] = 25, ["social"] = 21, ["email"] = 12 },
                            new Dictionary<string, object> { ["month"] = "Oct 25", ["organic"] = 82, ["paid"] = 65, ["referral"] = 25, ["direct"] = 28, ["social"] = 18, ["email"] = 15 },
                            new Dictionary<string, object> { ["month"] = "Nov 25", ["organic"] = 76, ["paid"] = 68, ["referral"] = 32, ["direct"] = 22, ["social"] = 23, ["email"] = 13 },
                            new Dictionary<string, object> { ["month"] = "Dec 25", ["organic"] = 85, ["paid"] = 72, ["referral"] = 28, ["direct"] = 26, ["social"] = 20, ["email"] = 16 }
                        ],
                        keys: ["organic", "paid", "referral", "direct", "social", "email"],
                        indexBy: "month",
                        groupMode: BarGroupMode.Stacked,
                        labelSkipWidth: 24,
                        labelSkipHeight: 12,
                        margin: new ChartMargin { Top = 50, Right = 130, Bottom = 70, Left = 60 },
                        axisBottom: new AxisConfig { Legend = "Month", LegendOffset = 50, TickPadding = 5, TickRotation = -45, TickValues = 12 },
                        axisLeft: new AxisConfig { Legend = "Traffic (K)", LegendOffset = -45, TickPadding = 5 },
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
                view.Text([Text.Caption, "mb-4"], "8 series x 48 data points with crosshair");
                view.Box(["h-96"], content: v =>
                {
                    v.LineChart([Chart.Container],
                        theme: chartTheme,
                        data: GenerateDenseLineData(),
                        curve: LineCurve.MonotoneX,
                        margin: new ChartMargin { Top = 50, Right = 170, Bottom = 70, Left = 70 },
                        yScaleMin: 0,
                        enableArea: true,
                        areaOpacity: 0.06,
                        areaBaselineValue: 0,
                        enablePoints: false,
                        enableCrosshair: true,
                        crosshairType: CrosshairType.Cross,
                        useMesh: true,
                        lineWidth: 2,
                        axisBottom: new AxisConfig { Legend = "Month", LegendOffset = 50, TickPadding = 5, TickRotation = -45, TickValues = 12 },
                        axisLeft: new AxisConfig { Legend = "Count (K)", LegendOffset = -55, TickPadding = 5 },
                        legends:
                        [
                            new LegendConfig
                            {
                                Anchor = "bottom-right",
                                Direction = "column",
                                TranslateX = 160,
                                ItemWidth = 150,
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
                view.Text([Text.Caption, "mb-4"], "15 slices with arc link labels");
                view.Box(["h-96"], content: v =>
                {
                    v.PieChart([Chart.Container],
                        theme: chartTheme,
                        data:
                        [
                            new PieChartDatum { Id = "react", Label = "React", Value = 22.5 },
                            new PieChartDatum { Id = "vue", Label = "Vue.js", Value = 14.2 },
                            new PieChartDatum { Id = "angular", Label = "Angular", Value = 12.8 },
                            new PieChartDatum { Id = "svelte", Label = "Svelte", Value = 9.4 },
                            new PieChartDatum { Id = "nextjs", Label = "Next.js", Value = 8.1 },
                            new PieChartDatum { Id = "nuxt", Label = "Nuxt", Value = 5.8 },
                            new PieChartDatum { Id = "solid", Label = "SolidJS", Value = 4.6 },
                            new PieChartDatum { Id = "qwik", Label = "Qwik", Value = 3.8 },
                            new PieChartDatum { Id = "htmx", Label = "HTMX", Value = 3.2 },
                            new PieChartDatum { Id = "alpine", Label = "Alpine.js", Value = 2.9 },
                            new PieChartDatum { Id = "preact", Label = "Preact", Value = 2.5 },
                            new PieChartDatum { Id = "lit", Label = "Lit", Value = 2.1 },
                            new PieChartDatum { Id = "ember", Label = "Ember.js", Value = 1.8 },
                            new PieChartDatum { Id = "astro", Label = "Astro", Value = 3.5 },
                            new PieChartDatum { Id = "other", Label = "Other", Value = 2.8 }
                        ],
                        margin: new ChartMargin { Top = 40, Right = 160, Bottom = 40, Left = 80 },
                        enableArcLinkLabels: true,
                        arcLinkLabelsSkipAngle: 10,
                        arcLinkLabelsThickness: 2,
                        enableArcLabels: true,
                        arcLabelsSkipAngle: 10,
                        legends:
                        [
                            new LegendConfig
                            {
                                Anchor = "right",
                                Direction = "column",
                                TranslateX = 140,
                                ItemWidth = 120,
                                ItemHeight = 20,
                                SymbolSize = 12
                            }
                        ]);
                });
            });

            // Donut Chart
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Donut Chart");
                view.Text([Text.Caption, "mb-4"], "12 segments with many small slices");
                view.Box(["h-96"], content: v =>
                {
                    v.PieChart([Chart.Container],
                        theme: chartTheme,
                        data:
                        [
                            new PieChartDatum { Id = "completed", Label = "Completed", Value = 35 },
                            new PieChartDatum { Id = "in-review", Label = "In Review", Value = 14 },
                            new PieChartDatum { Id = "in-progress", Label = "In Progress", Value = 12 },
                            new PieChartDatum { Id = "blocked", Label = "Blocked", Value = 8 },
                            new PieChartDatum { Id = "backlog", Label = "Backlog", Value = 6 },
                            new PieChartDatum { Id = "cancelled", Label = "Cancelled", Value = 4.5 },
                            new PieChartDatum { Id = "deferred", Label = "Deferred", Value = 4 },
                            new PieChartDatum { Id = "duplicate", Label = "Duplicate", Value = 3.5 },
                            new PieChartDatum { Id = "wontfix", Label = "Won't Fix", Value = 3 },
                            new PieChartDatum { Id = "needs-info", Label = "Needs Info", Value = 4 },
                            new PieChartDatum { Id = "reopened", Label = "Reopened", Value = 3.5 },
                            new PieChartDatum { Id = "triaged", Label = "Triaged", Value = 2.5 }
                        ],
                        innerRadius: 0.5,
                        padAngle: 0.7,
                        cornerRadius: 3,
                        margin: new ChartMargin { Top = 40, Right = 160, Bottom = 40, Left = 80 },
                        enableArcLinkLabels: true,
                        activeOuterRadiusOffset: 8,
                        legends:
                        [
                            new LegendConfig
                            {
                                Anchor = "right",
                                Direction = "column",
                                TranslateX = 140,
                                ItemWidth = 120,
                                ItemHeight = 20,
                                SymbolSize = 12
                            }
                        ]);
                });
            });
        });
    }
}
