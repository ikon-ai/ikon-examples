using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Ikon.Parallax;

namespace Ikon.App.Examples.Globe.WebGLGlobe;

public class SelectedSpikeData
{
    [JsonPropertyName("lat")]
    public float Lat { get; set; }

    [JsonPropertyName("lon")]
    public float Lon { get; set; }

    [JsonPropertyName("magnitude")]
    public float Magnitude { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
}

public static class GlobeExtensions
{
    /// <summary>
    /// Renders a WebGL globe with data visualization spikes.
    /// </summary>
    /// <param name="view">The UI view to add the globe to.</param>
    /// <param name="data">JSON data array: [[lat, lon, magnitude], ...] where magnitude is 0.0-1.0.</param>
    /// <param name="seriesName">Name of the data series being displayed.</param>
    /// <param name="seriesColor">Hex color for the data spikes (e.g. "#00ff00").</param>
    /// <param name="autoRotate">Whether the globe should auto-rotate.</param>
    /// <param name="rotationSpeed">Rotation speed multiplier (1.0 = default).</param>
    /// <param name="globeColor">Hex color for the globe surface (e.g. "#1a1a2e").</param>
    /// <param name="atmosphereColor">Hex color for the atmosphere glow (e.g. "#3b82f6").</param>
    /// <param name="onSpikeClick">Callback invoked when a spike is clicked.</param>
    /// <param name="style">CSS style classes (use Tailwind classes for sizing).</param>
    /// <param name="styleId">Style ID for the element.</param>
    /// <param name="key">Unique key for the element.</param>
    public static void WebGLGlobe(
        this UIView view,
        string? data = null,
        string? seriesName = null,
        string? seriesColor = null,
        bool? autoRotate = null,
        float? rotationSpeed = null,
        string? globeColor = null,
        string? atmosphereColor = null,
        Func<SelectedSpikeData, Task>? onSpikeClick = null,
        string[]? style = null,
        string? styleId = null,
        string? key = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        string? onSpikeClickId = null;

        if (onSpikeClick != null)
        {
            onSpikeClickId = view.CreateAction<SelectedSpikeData>(args => onSpikeClick(args.Value));
        }

        view.AddNode(
            NodeTypes.WebGLGlobe,
            new Dictionary<string, object?>
            {
                ["data"] = data,
                ["seriesName"] = seriesName,
                ["seriesColor"] = seriesColor,
                ["autoRotate"] = autoRotate,
                ["rotationSpeed"] = rotationSpeed,
                ["globeColor"] = globeColor,
                ["atmosphereColor"] = atmosphereColor,
                ["onSpikeClickId"] = onSpikeClickId
            },
            key: key,
            style: style,
            styleId: styleId,
            file: file,
            line: line);
    }
}
