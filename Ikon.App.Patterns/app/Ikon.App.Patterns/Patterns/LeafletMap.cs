namespace Ikon.App.Patterns.Patterns;

#region example:pattern-leaflet-map-extension
public sealed record MapPin(string Id, double Lat, double Lng, string Label);

public sealed record MapPoint(double Lat, double Lng);

public static class MapViewExtensions
{
    public static void MapView(this UIView view, IReadOnlyList<MapPin> pins, IReadOnlyList<MapPoint>? route = null,
        MapPoint? center = null, int zoom = 13, Func<string, Task>? onPinClick = null, Func<MapPoint, Task>? onMapClick = null,
        string[]? style = null, string? key = null)
    {
        view.AddNode("custom.map", new Dictionary<string, object?>
        {
            // Anonymous objects arrive with these lower-case names; a record's would arrive capitalised.
            ["pins"] = pins.Select(p => new { id = p.Id, lat = p.Lat, lng = p.Lng, label = p.Label }).ToList(),
            ["route"] = route?.Select(p => new { lat = p.Lat, lng = p.Lng }).ToList(),
            ["center"] = center is null ? null : new { lat = center.Lat, lng = center.Lng },
            ["zoom"] = zoom,
            ["onPinClickId"] = onPinClick is null ? null : view.CreateAction<string>(args => onPinClick(args.Value ?? "")),
            ["onMapClickId"] = onMapClick is null ? null : view.CreateAction<MapPoint>(args => onMapClick(args.Value)),
        }, style: style, key: key);
    }
}
#endregion

// Pattern: leaflet-map — see docs/patterns/leaflet-map.md.
// The demo shows its note: the custom.map node needs a React component this gallery's frontend
// does not register.
internal sealed class LeafletMap : IPatternDemo
{
    public string Slug => "leaflet-map";
    public string Title => "Leaflet map";
    public string Category => "Custom frontend";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "A Leaflet map as a custom node: pins from C# with a tap handler, a route drawn as a line, and a tap on the map sent back as a point. See docs/patterns/leaflet-map.md.");

    #region example:pattern-leaflet-map
    private static readonly MapPin[] Parks =
    [
        new("kaivopuisto", 60.1563, 24.9562, "Kaivopuisto"),
        new("sinebrychoff", 60.1617, 24.9312, "Sinebrychoff Park"),
        new("toolonlahti", 60.1801, 24.9295, "Töölönlahti"),
    ];

    private readonly ReactiveList<MapPoint> _route = new();
    private readonly ClientReactive<string?> _selectedPark = new(null);

    private void RenderMap(UIView view)
    {
        view.Column([Card.Default, "p-4 gap-3"], content: view =>
        {
            // isolate keeps Leaflet's panes (z-index 400 and up) under an open Sheet or Dialog.
            view.MapView(Parks, route: _route.Value, center: new MapPoint(60.1660, 24.9420), zoom: 13,
                onPinClick: async id => _selectedPark.Value = id,
                onMapClick: async point => _route.Add(point),
                style: ["isolate h-96 w-full rounded-lg"]);

            var selected = Parks.FirstOrDefault(p => p.Id == _selectedPark.Value);
            view.Text([Text.Body], selected is null ? "Tap a park to meet there, or tap the map to extend the route." : $"Meet at {selected.Label}");
            view.Button([Button.OutlineMd, "self-start"], text: "Clear route", disabled: _route.Count == 0,
                onClick: async () => _route.Clear());
        });
    }
    #endregion
}
