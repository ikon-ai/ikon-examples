using System.Runtime.CompilerServices;
using Ikon.Parallax;

namespace Ikon.App.Examples.Ambient;

public static class AmbientVideoExtensions
{
    public static void AmbientVideoPlayer(
        this UIView view,
        string url,
        float playbackRate = 0.25f,
        bool loop = true,
        bool muted = true,
        bool autoplay = true,
        float crossfadeDuration = 2f,
        string[]? style = null,
        string? styleId = null,
        string? key = null,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        view.AddNode(
            "ambient-video-player",
            new Dictionary<string, object?>
            {
                ["url"] = url,
                ["playbackRate"] = playbackRate,
                ["loop"] = loop,
                ["muted"] = muted,
                ["autoplay"] = autoplay,
                ["crossfadeDuration"] = crossfadeDuration
            },
            key: key,
            style: style,
            styleId: styleId,
            file: file,
            line: line);
    }
}
