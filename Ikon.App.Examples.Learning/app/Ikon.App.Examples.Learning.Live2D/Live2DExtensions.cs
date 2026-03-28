using System.Runtime.CompilerServices;
using Ikon.Parallax;

namespace Ikon.App.Examples.Learning.Live2D;

public static class Live2DExtensions
{
    /// <summary>
    /// Renders a Live2D canvas with an animated character model.
    /// </summary>
    /// <param name="view">The UI view to add the canvas to.</param>
    /// <param name="source">Path to the Live2D model file (.model.json or .model3.json).</param>
    /// <param name="mouthOpenY">Mouth open value for lip-sync (0.0-1.0).</param>
    /// <param name="isListening">Whether the model should show a listening animation.</param>
    /// <param name="expression">Name of the expression to display.</param>
    /// <param name="motion">Name of the motion to play.</param>
    /// <param name="idleMotionGroup">Name of the idle motion group (default: "idle").</param>
    /// <param name="viewMode">View mode controlling zoom level: "fullBody", "portrait", or "face".</param>
    /// <param name="scale">Scale multiplier for the model (1.0 = fit to viewport height).</param>
    /// <param name="offsetY">Vertical offset as fraction of viewport height (negative = show more of head).</param>
    /// <param name="canvasWidth">Internal canvas width in pixels (default: auto-detect from container).</param>
    /// <param name="canvasHeight">Internal canvas height in pixels (default: auto-detect from container).</param>
    /// <param name="style">CSS style classes (use Tailwind classes like "w-96 h-96" for sizing).</param>
    /// <param name="styleId">Style ID for the element.</param>
    /// <param name="key">Unique key for the element.</param>
    public static void Live2DCanvas(
        this UIView view,
        string source,
        float? mouthOpenY = null,
        bool? isListening = null,
        string? expression = null,
        string? motion = null,
        string? idleMotionGroup = null,
        string? viewMode = null,
        float? scale = null,
        float? offsetY = null,
        int? canvasWidth = null,
        int? canvasHeight = null,
        string[]? style = null,
        string? styleId = null,
        string? key = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Live2D source must be provided", nameof(source));
        }

        view.AddNode(
            NodeTypes.Live2DCanvas,
            new Dictionary<string, object?>
            {
                ["src"] = source,
                ["mouthOpenY"] = mouthOpenY,
                ["isListening"] = isListening,
                ["expression"] = expression,
                ["motion"] = motion,
                ["idleMotionGroup"] = idleMotionGroup,
                ["viewMode"] = viewMode,
                ["scale"] = scale,
                ["offsetY"] = offsetY,
                ["canvasWidth"] = canvasWidth,
                ["canvasHeight"] = canvasHeight
            },
            key: key,
            style: style,
            styleId: styleId,
            file: file,
            line: line);
    }
}
