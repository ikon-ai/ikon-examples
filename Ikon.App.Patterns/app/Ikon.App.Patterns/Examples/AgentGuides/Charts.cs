namespace Ikon.App.Patterns.Examples;

internal sealed partial class AgentGuideExamples
{

    private static void DocPieChart(UIView view, IReadOnlyList<DocCategory> categories)
    {
        #region example:pie-chart
        view.PieChart(
            ["h-72 w-72"],
            data: categories.Select(c => new PieChartDatum
            {
                Id = c.Name, Label = c.Name, Value = c.Total, Color = c.Hex
            }),
            innerRadius: 0.5);
        #endregion
    }

    private static void DocLineChart(UIView view, IReadOnlyList<DocDay> days)
    {
        #region example:line-chart
        view.LineChart(
            ["h-72 w-full"],
            data: [new LineChartSeries
            {
                Id = "Daily", Color = "#34d399",
                Data = days.Select(d => new LineChartPoint { X = d.Label, Y = d.Amount })
            }]);
        #endregion
    }

    private static void DocBarChart(UIView view, IReadOnlyList<DocCategory> categories)
    {
        #region example:bar-chart
        view.BarChart(
            ["h-72 w-full"],
            data: categories.Select(c => new Dictionary<string, object>
            {
                ["category"] = c.Name,
                ["spend"] = c.Total
            }),
            keys: ["spend"],
            indexBy: "category");
        #endregion
    }

    private static void DocFunnelChart(UIView view)
    {
        #region example:funnel-chart
        view.FunnelChart(
            ["w-full"],
            data:
            [
                new FunnelStep { Id = "visited", Label = "Visited", Value = 1840 },
                new FunnelStep { Id = "signed-up", Label = "Signed up", Value = 412 },
                new FunnelStep { Id = "paid", Label = "Paid", Value = 37 },
            ],
            valueUnit: "people");
        #endregion
    }

    private static void DocHeatMap(UIView view, IReadOnlyList<(string Day, int Hour, int Visits)> visits)
    {
        #region example:heat-map
        view.HeatMap(
            ["w-full"],
            data: visits.Select(v => new HeatMapCell { Row = v.Day, Column = $"{v.Hour:00}", Value = v.Visits }),
            rows: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
            valueUnit: "visits");
        #endregion
    }

    private static void DocSparklines(UIView view, IReadOnlyList<DocDay> days)
    {
        #region example:sparklines
        view.StatCard("Spend this week", ValueUnits.Format(days.Sum(d => d.Amount), "usd"),
            sparkline: days.Select(d => (double?)d.Amount));

        view.DataTable(
            columns: [new DataTableColumn("Day"), new DataTableColumn("Spend", Align: ColumnAlign.Right)],
            rows: days.Select(d => new DataTableRow(d.Label,
                [Cell.Text(d.Label), Cell.Bar(d.Amount, max: days.Max(x => x.Amount), text: ValueUnits.Format(d.Amount, "usd"))])).ToArray());
        #endregion
    }
}
