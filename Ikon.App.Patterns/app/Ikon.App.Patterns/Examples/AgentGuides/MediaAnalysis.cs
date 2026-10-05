using System.Diagnostics;

namespace Ikon.App.Patterns.Examples;

file sealed class MediaUploadExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:media-upload-state
    private readonly Reactive<AssetUri?> _mediaAssetUri = new(null);
    #endregion

    private static Task AnalyzeMediaAsync(AssetUri assetUri) => Task.CompletedTask;

    public void Render(UIView view)
    {
        #region example:media-upload
        view.FileUpload(
            accept: ["video/*", "audio/*"],
            maxFileSize: 2L * 1024 * 1024 * 1024,
            onUploadStart: async args =>
            {
                var assetUri = new AssetUri(AssetClass.CloudFile, $"uploads/{args.Hash}/{args.FileName}", spaceId: app.GlobalState.SpaceId);
                return new FileUploadResult { Accepted = true, AssetUri = assetUri };
            },
            onUploadComplete: async args =>
            {
                if (args.AssetUri is not { } assetUri)
                {
                    return;
                }

                _mediaAssetUri.Value = assetUri;

                var clientId = ReactiveScope.ClientId;
                _ = Task.Run(async () =>
                {
                    using var _ = ReactiveScope.Use(new ClientScope(clientId));
                    await using var work = await app.BackgroundWork.StartAsync();
                    await AnalyzeMediaAsync(assetUri);
                });
            });
        #endregion
    }
}

file static class MediaProbeExamples
{
    #region example:media-probe
    private static async Task<JsonDocument?> ProbeMediaAsync(AssetUri assetUri)
    {
        // The signed URL is temporary (.UrlIsTemporal) — fetch it fresh
        // right before each ffprobe/ffmpeg invocation, never persist it
        var metadata = await Asset.Instance.GetMetadataAsync(assetUri);

        if (metadata.Url is null)
        {
            return null;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffprobe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // ArgumentList, never an interpolated Arguments string — URL characters can
        // otherwise be misparsed as ffprobe option flags
        foreach (var arg in new[] { "-v", "quiet", "-print_format", "json", "-show_format", "-show_streams", metadata.Url })
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            return null;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            string output = await process.StandardOutput.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);
            return process.ExitCode == 0 ? JsonDocument.Parse(output) : null;
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(); } catch { /* the probe is already being abandoned; a kill that fails changes nothing */ }

            return null;
        }
    }
    #endregion

    public static async Task ExtractAudioAsync(string url)
    {
        #region example:media-ffmpeg-pipe
        // Extract mono 16 kHz PCM audio (e.g. for speech recognition); for thumbnail
        // frames instead, swap the output args for "-f", "image2pipe", "-vcodec", "mjpeg"
        // with a select/fps filter
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in new[] { "-loglevel", "quiet", "-i", url, "-vn", "-f", "f32le", "-ac", "1", "-ar", "16000", "pipe:1" })
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            return;
        }

        await using var audioStream = process.StandardOutput.BaseStream;
        // read fixed-size chunks from audioStream — do NOT ReadToEnd a long file
        #endregion

        await ProbeMediaAsync(default);
    }
}
