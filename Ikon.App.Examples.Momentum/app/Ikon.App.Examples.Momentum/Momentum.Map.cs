using System.Runtime.CompilerServices;

public sealed class MapPoint
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}

public sealed class MapMarker
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    // "here" | "start" | "end" | "highlight"
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

public static class MomentumMapExtensions
{
    private const string NodeType = "momentum-map";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// A dark tiled map with the magenta track drawn over it. The React frontend renders it with
    /// Leaflet and the Flutter frontend with flutter_map, from the same props — one call in C# drives
    /// both, which is the whole reason the node exists rather than two per-frontend paths.
    /// </summary>
    public static void MomentumMap(
        this UIView view,
        IReadOnlyList<GeoPoint> track,
        List<MapMarker>? markers = null,
        IReadOnlyList<GeoPoint>? emphasis = null,
        GeoPoint? center = null,
        int? zoom = null,
        bool follow = false,
        string[]? style = null,
        string? key = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        view.AddNode(
            NodeType,
            new Dictionary<string, object?>
            {
                ["track"] = Serialize(track),
                ["emphasis"] = emphasis is { Count: > 1 } ? Serialize(emphasis) : null,
                ["markers"] = markers != null ? JsonSerializer.Serialize(markers, JsonOptions) : null,
                ["center"] = center is { } c ? JsonSerializer.Serialize(new[] { c.Lat, c.Lon }) : null,
                ["zoom"] = zoom,
                ["follow"] = follow ? true : null,
                ["lineColor"] = Brand.Magenta,
                ["emphasisColor"] = Brand.Gold
            },
            key: key,
            style: style,
            file: file,
            line: line);
    }

    /// <summary>
    /// The route's silhouette with no basemap under it — what a feed card wants. Cheap enough to draw
    /// a screenful of them, and it renders identically on every client without a tile request.
    /// </summary>
    public static void RouteTrace(this UIView view, IReadOnlyList<GeoPoint> track, string[]? style = null,
        string? color = null, double lineWidth = 2)
    {
        if (track.Count < 2)
        {
            view.Box(style ?? []);
            return;
        }

        double minLat = track.Min(p => p.Lat);
        double maxLat = track.Max(p => p.Lat);
        double minLon = track.Min(p => p.Lon);
        double maxLon = track.Max(p => p.Lon);

        // Longitude degrees shrink with latitude; padding the narrower axis keeps the silhouette in
        // proportion instead of stretching a north-south loop into a circle.
        double lonScale = Math.Cos(Geo.ToRad((minLat + maxLat) / 2));
        double spanLat = Math.Max(0.001, maxLat - minLat);
        double spanLon = Math.Max(0.001, (maxLon - minLon) * lonScale);
        double span = Math.Max(spanLat, spanLon) * 1.12;
        double midLat = (minLat + maxLat) / 2;
        double midLon = (minLon + maxLon) / 2;
        string stroke = color ?? Brand.Magenta;

        view.Box(["overflow-hidden", .. style ?? []], content: view =>
        {
            view.LineChart(["w-full h-full"],
                data:
                [
                    new LineChartSeries
                    {
                        Id = "route",
                        Color = stroke,
                        Data = track.Select(p => new LineChartPoint
                        {
                            X = Math.Round((p.Lon - midLon) * lonScale, 5),
                            Y = Math.Round(p.Lat - midLat, 5)
                        })
                    }
                ],
                xScaleType: ScaleType.Linear,
                yScaleType: ScaleType.Linear,
                xScaleMin: -span / 2,
                xScaleMax: span / 2,
                yScaleMin: -span / 2,
                yScaleMax: span / 2,
                margin: new ChartMargin { Top = 2, Right = 2, Bottom = 2, Left = 2 },
                // A route silhouette is a shape, not a reading: no axes, no grid, no ticks.
                axisBottom: new AxisConfig { Hidden = true },
                axisLeft: new AxisConfig { Hidden = true },
                enableGridX: false,
                enableGridY: false,
                enablePoints: false,
                curve: LineCurve.Linear,
                lineWidth: lineWidth,
                isInteractive: false,
                colors: [stroke],
                theme: ChartThemes.DefaultDark);
        });
    }

    private static string Serialize(IReadOnlyList<GeoPoint> track) =>
        JsonSerializer.Serialize(track.Select(p => new MapPoint { Lat = p.Lat, Lon = p.Lon }).ToList(), JsonOptions);
}
