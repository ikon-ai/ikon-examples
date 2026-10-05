using System.Runtime.CompilerServices;

namespace Ikon.App.Patterns.Examples;

#region example:custom-map-pin-drag-data
public class PinDragData
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lon")] public double Lon { get; set; }
}
#endregion

public static class MyMapDragExtensions
{
    #region example:custom-map-pin-drag-wiring
    public static void MyMapWithDrag(
        this UIView view,
        IReadOnlyList<MapPin>? pins = null,
        Func<PinDragData, Task>? onPinDrag = null,
        string[]? style = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        string? onPinDragId = null;

        if (onPinDrag != null)
        {
            onPinDragId = view.CreateAction<PinDragData>(args => onPinDrag(args.Value));
        }

        view.AddNode(
            MyMapNodeTypes.MyMap,
            new Dictionary<string, object?>
            {
                ["pins"] = pins is null ? null : JsonSerializer.Serialize(pins),
                ["onPinDragId"] = onPinDragId,
            },
            style: style,
            file: file,
            line: line);
    }
    #endregion
}

#region example:custom-map-polygon-overlay
public class PolygonOverlay
{
    [JsonPropertyName("vertices")] public List<double[]>? Vertices { get; set; }
}
#endregion

file static class MapPolygonExamples
{
    private sealed record GeoPoint(double Latitude, double Longitude);

    public static void BuildVertices()
    {
        IReadOnlyList<GeoPoint> points = [new(51.5, -0.09), new(51.51, -0.08), new(51.52, -0.07)];

        #region example:custom-map-polygon-vertices
        var overlay = new PolygonOverlay
        {
            Vertices = points.Select(p => new double[] { p.Latitude, p.Longitude }).ToList()
        };
        #endregion

        Log.Instance.Debug($"{overlay.Vertices?.Count}");
    }
}
