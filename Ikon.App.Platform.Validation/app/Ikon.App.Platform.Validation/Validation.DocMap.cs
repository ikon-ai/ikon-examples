#region docsnippet:custom-map-csharp
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ikon.Parallax;

// --- Data classes (serialized to JSON for the frontend) ---

public class MapPin
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lon")] public double Lon { get; set; }
    [JsonPropertyName("label")] public string? Label { get; set; }
    [JsonPropertyName("color")] public string? Color { get; set; }
}

public class AreaOverlay
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lon")] public double Lon { get; set; }
    [JsonPropertyName("radiusMeters")] public float RadiusMeters { get; set; }
    [JsonPropertyName("color")] public string Color { get; set; } = "#3388ff";
    [JsonPropertyName("fillOpacity")] public float FillOpacity { get; set; } = 0.15f;
    [JsonPropertyName("label")] public string? Label { get; set; }
}

// --- Event data classes (deserialized from frontend dispatches) ---

public class PinClickData
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lon")] public double Lon { get; set; }
}

public class MapClickData
{
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lon")] public double Lon { get; set; }
}

// --- Node type constant ---

internal static class MyMapNodeTypes
{
    public const string MyMap = "my-map";
}

// --- Extension method ---

public static class MyMapExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void MyMap(
        this UIView view,
        IReadOnlyList<MapPin>? pins = null,
        IReadOnlyList<AreaOverlay>? areas = null,
        double? centerLat = null,
        double? centerLon = null,
        int? zoom = null,
        Func<PinClickData, Task>? onPinClick = null,
        Func<MapClickData, Task>? onMapClick = null,
        string[]? style = null,
        string? styleId = null,
        string? key = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        // Serialize data to JSON
        string? pinsJson = pins != null ? JsonSerializer.Serialize(pins, JsonOptions) : null;
        string? areasJson = areas != null ? JsonSerializer.Serialize(areas, JsonOptions) : null;
        string? centerJson = centerLat.HasValue && centerLon.HasValue
            ? JsonSerializer.Serialize(new[] { centerLat.Value, centerLon.Value })
            : null;

        // Create action IDs for callbacks
        string? onPinClickId = null;
        string? onMapClickId = null;

        if (onPinClick != null)
        {
            onPinClickId = view.CreateAction<PinClickData>(args => onPinClick(args.Value));
        }

        if (onMapClick != null)
        {
            onMapClickId = view.CreateAction<MapClickData>(args => onMapClick(args.Value));
        }

        // Register the UI node with all props
        view.AddNode(
            MyMapNodeTypes.MyMap,
            new Dictionary<string, object?>
            {
                ["pins"] = pinsJson,
                ["areas"] = areasJson,
                ["center"] = centerJson,
                ["zoom"] = zoom,
                ["onPinClickId"] = onPinClickId,
                ["onMapClickId"] = onMapClickId,
            },
            key: key,
            style: style,
            styleId: styleId,
            file: file,
            line: line);
    }
}
#endregion

// The guide's worked app for this component. Its fence used to wrap these members in a second
// `[App] public class MyApp(…)` shell; an assembly may declare exactly one of those, so the shell
// could never be part of a compiled example — and the reader already has an app class to put these
// in. Same trim as the secrets and HTTP-endpoint examples.
file sealed class DocMapApp(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    #region docsnippet:custom-map-usage
    private readonly ReactiveList<MapPin> _pins = new();
    private readonly Reactive<string?> _selectedPinId = new(null);
    private readonly Reactive<string?> _lastClickInfo = new(null);

    public async Task Main()
    {
        // Seed some example pins — one AddRange, one change notification
        _pins.AddRange(
        [
            new MapPin { Id = "hq", Lat = 51.505, Lon = -0.09, Label = "HQ", Color = "#00ff88" },
            new MapPin { Id = "depot", Lat = 51.51, Lon = -0.08, Label = "Depot", Color = "#ff6600" }
        ]);

        UI.Root([Page.Default], content: view =>
        {
            view.Column(["flex-1"], content: view =>
            {
                view.Row(["flex-1"], content: view =>
                {
                    // The map takes up most of the space
                    view.MyMap(
                        pins: _pins.Value,
                        areas:
                        [
                            new AreaOverlay
                            {
                                Id = "zone1",
                                Lat = 51.505, Lon = -0.09,
                                RadiusMeters = 500,
                                Color = "#33D17A",
                                FillOpacity = 0.1f,
                                Label = "Safe Zone"
                            }
                        ],
                        centerLat: 51.505,
                        centerLon: -0.09,
                        zoom: 14,
                        onPinClick: async data =>
                        {
                            _selectedPinId.Value = data.Id;
                            _lastClickInfo.Value = $"Pin: {data.Id} at ({data.Lat:F4}, {data.Lon:F4})";
                        },
                        onMapClick: async data =>
                        {
                            _lastClickInfo.Value = $"Map click: ({data.Lat:F4}, {data.Lon:F4})";
                        },
                        style: ["flex-1"]);

                    // Side panel
                    view.Column(["w-64 p-4 border-l border-gray-700 bg-gray-900"], content: view =>
                    {
                        view.Text(["text-sm font-bold text-gray-300"], "MAP INFO");

                        if (_lastClickInfo.Value != null)
                        {
                            view.Text(["text-xs text-gray-400 mt-2"], _lastClickInfo.Value);
                        }

                        if (_selectedPinId.Value != null)
                        {
                            view.Text(["text-xs text-green-400 mt-2"], $"Selected: {_selectedPinId.Value}");
                        }
                    });
                });
            });
        });
    }
    #endregion
}
