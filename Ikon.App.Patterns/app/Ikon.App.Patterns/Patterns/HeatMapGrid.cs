using System.Globalization;
using System.Security;

namespace Ikon.App.Patterns.Patterns;

// Pattern: heat-map-grid — see docs/patterns/heat-map-grid.md.
// The sample counts stand in for rentals per station and hour that an app reads from its database.
internal sealed class HeatMapGrid : IPatternDemo
{
    public string Slug => "heat-map-grid";
    public string Title => "Heat map grid";
    public string Category => "Visualization";

    private static readonly string[] Stations = ["Central Station", "Harbour", "University", "Market Square", "Hospital"];
    private static readonly string[] Hours = Enumerable.Range(0, 24).Select(hour => $"{hour:00}").ToArray();

    private readonly Reactive<bool> _weekend = new(false);

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 w-full"], content: col =>
        {
            col.Row(["gap-2"], content: row =>
            {
                row.Button([_weekend.Value ? Button.OutlineSm : Button.PrimarySm], text: "Weekdays",
                    onClick: async () => _weekend.Value = false);
                row.Button([_weekend.Value ? Button.PrimarySm : Button.OutlineSm], text: "Weekend",
                    onClick: async () => _weekend.Value = true);
            });
            RenderHeatMap((UIView)col, Stations, Hours, SampleCounts(_weekend.Value));
        });
    }

    private static double[,] SampleCounts(bool weekend)
    {
        var counts = new double[Stations.Length, Hours.Length];

        for (var station = 0; station < Stations.Length; station++)
        {
            for (var hour = 0; hour < Hours.Length; hour++)
            {
                var commute = weekend ? 0 : 30 * Math.Exp(-Math.Pow(hour - 8, 2) / 2) + 26 * Math.Exp(-Math.Pow(hour - 17, 2) / 3);
                var leisure = (weekend ? 22 : 8) * Math.Exp(-Math.Pow(hour - 14, 2) / 10);
                counts[station, hour] = Math.Round((commute + leisure) * (1 + 0.3 * ((station * 7 + hour) % 5) / 4.0) / (station + 1.5) * 3);
            }
        }

        return counts;
    }

    #region example:pattern-heat-map-grid
    // One row per station and one column per hour. A cell is more opaque the higher its count. One
    // colour at varying opacity looks the same on a light and a dark page.
    private static byte[] HeatMapSvg(IReadOnlyList<string> rows, IReadOnlyList<string> columns, double[,] values, string hue)
    {
        const double cell = 22, gap = 2, labelWidth = 110, headerHeight = 20, legendHeight = 30;
        var width = labelWidth + columns.Count * cell;
        var height = headerHeight + rows.Count * cell + legendHeight;
        var max = values.Cast<double>().DefaultIfEmpty(0).Max();
        var invariant = CultureInfo.InvariantCulture;

        var sb = new StringBuilder();
        sb.Append(invariant, $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}' font-family='ui-sans-serif,system-ui' font-size='11' fill='#71717a'>");

        // Every third column label, so 24 hours stay readable on a phone
        for (var c = 0; c < columns.Count; c += 3)
        {
            sb.Append(invariant, $"<text x='{labelWidth + c * cell + (cell - gap) / 2}' y='{headerHeight - 6}' text-anchor='middle'>{SecurityElement.Escape(columns[c])}</text>");
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var y = headerHeight + r * cell;
            sb.Append(invariant, $"<text x='{labelWidth - 8}' y='{y + (cell - gap) / 2}' text-anchor='end' dominant-baseline='central'>{SecurityElement.Escape(rows[r])}</text>");

            for (var c = 0; c < columns.Count; c++)
            {
                var share = max > 0 ? values[r, c] / max : 0;
                sb.Append(invariant, $"<rect x='{labelWidth + c * cell}' y='{y}' width='{cell - gap}' height='{cell - gap}' rx='3' fill='{hue}' fill-opacity='{0.06 + 0.94 * share:F2}'/>");
            }
        }

        // The legend: five steps from zero to the highest count
        var legendY = headerHeight + rows.Count * cell + 10;
        sb.Append(invariant, $"<text x='{labelWidth - 8}' y='{legendY + 6}' text-anchor='end' dominant-baseline='central'>0</text>");

        for (var step = 0; step < 5; step++)
        {
            sb.Append(invariant, $"<rect x='{labelWidth + step * cell}' y='{legendY}' width='{cell - gap}' height='12' rx='2' fill='{hue}' fill-opacity='{0.06 + 0.94 * step / 4.0:F2}'/>");
        }

        sb.Append(invariant, $"<text x='{labelWidth + 5 * cell + 4}' y='{legendY + 6}' dominant-baseline='central'>{max:0}</text>");
        sb.Append("</svg>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void RenderHeatMap(UIView view, IReadOnlyList<string> stations, IReadOnlyList<string> hours, double[,] counts)
    {
        // The busiest cell is also given in text, because an image has no hover and a screen reader reads only the alt
        var (busiestStation, busiestHour) = (0, 0);

        for (var r = 0; r < stations.Count; r++)
        {
            for (var c = 0; c < hours.Count; c++)
            {
                if (counts[r, c] > counts[busiestStation, busiestHour])
                {
                    (busiestStation, busiestHour) = (r, c);
                }
            }
        }

        var busiest = $"busiest: {stations[busiestStation]} at {hours[busiestHour]}:00, {counts[busiestStation, busiestHour]:0} rentals";
        view.Image(["w-full max-w-3xl h-auto"], data: HeatMapSvg(stations, hours, counts, "#0ea5e9"),
            mimeType: "image/svg+xml", alt: $"Rentals per hour at {stations.Count} stations, {busiest}");
        view.Text([Text.Caption], $"Rentals per hour, {busiest}");
    }
    #endregion
}
