using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ikon.App.Platform.Validation.Protocol;
using Ikon.Common;
using Ikon.Parallax;

// The last fences that were descriptions rather than code.
//
// A listing of member signatures reads correctly forever while being wrong — rename
// `ToTeleportBytes` and the Teleport spec still looks right. The same is true of "add this
// parameter, then wire it here": instructions compile nowhere, so nothing catches the day
// `CreateAction` changes shape. Each is now a call against a type this repo really generates.

file static class DocTeleportSpec
{
    public static void TomlWriter()
    {
        #region docsnippet:teleport-toml-writer
        // AppProjectConfig is this repo's own toml-mode schema — the one behind ikon-config.toml.
        var config = new AppProjectConfig();
        string toml = config.ToToml();

        // extraLinesBySection appends raw lines: "" targets the root block, a section field name that
        // section, and any other key becomes a trailing [Key] block.
        string annotated = config.ToToml(new Dictionary<string, IReadOnlyList<string>>
        {
            [""] = ["# written by the deploy step"],
        });
        #endregion

        Log.Instance.Debug($"{toml.Length} {annotated.Length}");
    }

    public static void BinaryCodecs()
    {
        #region docsnippet:teleport-binary-codecs
        // PlayerProfile is a data schema; every data schema's root class gets these.
        var profile = new PlayerProfile { DisplayName = "Ada", Score = 12 };

        byte[] bytes = profile.ToTeleportBytes();
        PlayerProfile roundTripped = PlayerProfile.FromTeleportBytes(bytes);
        #endregion

        Log.Instance.Debug($"{roundTripped.DisplayName}");
    }

    public static void RetiredLedger(byte[] stored)
    {
        #region docsnippet:teleport-retired-ledger
        IReadOnlyList<string> names = PlayerProfile.RetiredKeys;   // the ledger's names

        var loaded = PlayerProfile.FromTeleportBytes(stored);
        PlayerProfile.RetiredFields? captured = loaded.GetRetiredFields();   // null when the payload carried none

        // Populate before writing, and carry the bag across a clone the codec did not make.
        var next = new PlayerProfile { DisplayName = captured?.Nickname ?? loaded.DisplayName };
        next.GetOrCreateRetiredFields().Nickname = captured?.Nickname;
        next.CopyRetiredFieldsFrom(loaded);
        #endregion

        Log.Instance.Debug($"{names.Count} {next.DisplayName}");
    }
}

#region docsnippet:custom-map-pin-drag-data
public class PinDragData
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lon")] public double Lon { get; set; }
}
#endregion

public static class MyMapDragExtensions
{
    #region docsnippet:custom-map-pin-drag-wiring
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

#region docsnippet:custom-map-polygon-overlay
public class PolygonOverlay
{
    [JsonPropertyName("vertices")] public List<double[]>? Vertices { get; set; }
}
#endregion

file static class DocMapPolygon
{
    private sealed record GeoPoint(double Latitude, double Longitude);

    public static void BuildVertices()
    {
        IReadOnlyList<GeoPoint> points = [new(51.5, -0.09), new(51.51, -0.08), new(51.52, -0.07)];

        #region docsnippet:custom-map-polygon-vertices
        var overlay = new PolygonOverlay
        {
            Vertices = points.Select(p => new double[] { p.Latitude, p.Longitude }).ToList()
        };
        #endregion

        Log.Instance.Debug($"{overlay.Vertices?.Count}");
    }
}

public class DocPaymentsGating
{
    #region docsnippet:payments-gating
    [Function(Visibility = FunctionVisibility.External)]
    [PaymentsRequireEntitlement("pro")]   // deny code: payments_entitlement_required
    public string ProOnlyReport() => "the paid-tier report";
    #endregion
}
