using System.Runtime.CompilerServices;
using Ikon.Parallax;

namespace Ikon.App.Examples.VRMChat.VRM;

public static class VRMExtensions
{
    /// <summary>
    /// Renders a VRM 3D canvas with an animated character model.
    /// </summary>
    /// <param name="view">The UI view to add the canvas to.</param>
    /// <param name="source">Path to the VRM model file (.vrm).</param>
    /// <param name="isListening">Whether the model should show a listening animation.</param>
    /// <param name="expression">Name of the expression to display (happy, angry, sad, relaxed, surprised).</param>
    /// <param name="motion">Name of the motion to play.</param>
    /// <param name="viewMode">View mode controlling camera position: "fullBody", "portrait", or "face".</param>
    /// <param name="style">CSS style classes.</param>
    /// <param name="styleId">Style ID for the element.</param>
    /// <param name="key">Unique key for the element.</param>
    public static void VRMCanvas(
        this UIView view,
        string source,
        bool? isListening = null,
        string? expression = null,
        string? motion = null,
        string? viewMode = null,
        string[]? style = null,
        string? styleId = null,
        string? key = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("VRM source must be provided", nameof(source));
        }

        view.AddNode(
            NodeTypes.VRMCanvas,
            new Dictionary<string, object?>
            {
                ["src"] = source,
                ["isListening"] = isListening,
                ["expression"] = expression,
                ["motion"] = motion,
                ["viewMode"] = viewMode
            },
            key: key,
            style: style,
            styleId: styleId,
            file: file,
            line: line);
    }
}
