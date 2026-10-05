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
}
