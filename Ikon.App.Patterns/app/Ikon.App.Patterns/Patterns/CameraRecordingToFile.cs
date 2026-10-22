using System.Buffers.Binary;
using System.Diagnostics;
using VideoCodec = Ikon.Common.Core.Protocol.VideoCodec;

namespace Ikon.App.Patterns.Patterns;

// Pattern: camera-recording-to-file — see docs/patterns/camera-recording-to-file.md.
// The gallery has no camera reaching Video, so it shows the record button and an empty list; the
// recorder runs in an app where a camera starts. It needs ffmpeg on the machine that runs it.
internal sealed class CameraRecordingToFile(IAppBase app) : IPatternDemo
{
    public string Slug => "camera-recording-to-file";
    public string Title => "Record a camera to a file";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => Render(view);

    private Video Video => throw new NotImplementedException();

    #region example:pattern-camera-recording-to-file
    // In the app class: private Video Video { get; } = new(app); Video is an instance property, not a static class
    private sealed record Recording(string Path, DateTime StartedAtUtc, bool Saved);

    private readonly ReactiveList<Recording> _recordings = new();
    private readonly ClientReactive<string?> _playingUrl = new(null);

    // Wire once, in OnStarting: camera inputs are app-wide and carry the client that sent them
    private void WireRecorder()
    {
        Video.InputStartedAsync += async input =>
        {
            if (input.Kind != VideoSourceKind.Camera)
            {
                return;
            }

            // The frames arrive encoded: H.264 goes into an .mp4 file and VP8 into a .webm one, never re-encoded
            bool vp8 = input.Codec == VideoCodec.Vp8;
            string path = $"recordings/{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.{(vp8 ? "webm" : "mp4")}";
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(path));
            var ffmpeg = StartFfmpeg(vp8, tempPath);
            var writer = new FrameWriter(ffmpeg.StandardInput.BaseStream, vp8, input.Width, input.Height);
            input.FrameReceivedAsync += async frame => await writer.WriteAsync(frame);

            // Not awaited here: Ended completes only once the last frame has been through the handler above
            _ = Task.Run(() => FinishAsync(input, ffmpeg, tempPath, path, vp8));
        };
    }

    private static Process StartFfmpeg(bool vp8, string tempPath)
    {
        var start = new ProcessStartInfo("ffmpeg") { RedirectStandardInput = true, RedirectStandardError = true, UseShellExecute = false };

        // Raw H.264 carries no timing and a camera sends fewer frames than it announces, so stamp
        // each frame as it arrives; the IVF headers carry their own times
        string[] input = vp8 ? ["-f", "ivf"] : ["-use_wallclock_as_timestamps", "1", "-f", "h264"];

        // faststart moves the MP4 index to the front, so a browser plays the file before it has all of it
        string[] output = vp8 ? ["-c", "copy"] : ["-c", "copy", "-movflags", "+faststart"];

        foreach (var arg in (string[])["-loglevel", "error", .. input, "-i", "pipe:0", .. output, tempPath])
        {
            start.ArgumentList.Add(arg);
        }

        return Process.Start(start) ?? throw new InvalidOperationException("ffmpeg did not start");
    }

    private async Task FinishAsync(VideoInput input, Process ffmpeg, string tempPath, string path, bool vp8)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            // The camera stopped (or input.StopAsync() stopped it) and every frame went through the writer
            await input.Ended;
            var errorsRead = ffmpeg.StandardError.ReadToEndAsync();
            ffmpeg.StandardInput.Close();
            await ffmpeg.WaitForExitAsync();
            string errors = await errorsRead;

            // Read before the process is disposed, which makes ExitCode throw
            if (ffmpeg.ExitCode != 0 || !File.Exists(tempPath))
            {
                Log.Instance.Error($"Recording {path} was not saved, ffmpeg exited {ffmpeg.ExitCode}: {errors}");
                _recordings.Add(new Recording(path, startedAt, Saved: false));
                return;
            }

            await using (var file = File.OpenRead(tempPath))
            {
                await app.Files.Data.WriteStreamAsync(path, file, vp8 ? "video/webm" : "video/mp4");
            }

            _recordings.Add(new Recording(path, startedAt, Saved: true));
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Recording {path} was lost: {ex.Message}");
        }
        finally
        {
            ffmpeg.Dispose();
            File.Delete(tempPath);
        }
    }

    // One camera's frames into ffmpeg's input, from the first keyframe on. Frames arrive one at a time.
    private sealed class FrameWriter(Stream stdin, bool vp8, int width, int height)
    {
        private readonly Stopwatch _sinceFirstFrame = new();
        private bool _failed;

        public async Task WriteAsync(VideoInputFrame frame)
        {
            if (_failed || (!_sinceFirstFrame.IsRunning && !frame.IsKey))
            {
                return;
            }

            try
            {
                if (!_sinceFirstFrame.IsRunning)
                {
                    _sinceFirstFrame.Start();

                    if (vp8)
                    {
                        await stdin.WriteAsync(IvfFileHeader(width, height));
                    }
                }

                if (vp8)
                {
                    // Each IVF frame: its length, then the milliseconds since the first frame
                    var header = new byte[12];
                    BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)frame.Data.Length);
                    BinaryPrimitives.WriteUInt64LittleEndian(header.AsSpan(4), (ulong)_sinceFirstFrame.ElapsedMilliseconds);
                    await stdin.WriteAsync(header);
                }

                await stdin.WriteAsync(frame.Data);
            }
            catch (IOException ex)
            {
                // ffmpeg exited; FinishAsync reports why, so only the rest of the frames are skipped
                _failed = true;
                Log.Instance.Debug($"Recording stopped taking frames: {ex.Message}");
            }
        }

        private static byte[] IvfFileHeader(int width, int height)
        {
            var header = new byte[32];
            "DKIF"u8.CopyTo(header);
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(6), 32);
            "VP80"u8.CopyTo(header.AsSpan(8));
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(12), (ushort)width);
            BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(14), (ushort)height);
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(16), 1000);
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(20), 1);
            return header;
        }
    }

    private void Render(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            // Pressing it again stops the camera; the recording is saved once its last frame is written
            col.CaptureButton([Button.PrimaryMd, "self-start"],
                kind: MediaCaptureKind.Camera,
                captureMode: MediaCaptureButtonMode.Toggle,
                text: "Record");

            if (_recordings.Count == 0)
            {
                col.EmptyState([EmptyState.RootSm], title: "No recordings yet", description: "Press Record to capture one.");
            }

            foreach (var recording in _recordings)
            {
                col.Row(["items-center gap-3"], key: recording.Path, content: row =>
                {
                    row.Text([Text.Body], text: $"{recording.StartedAtUtc:HH:mm} UTC");

                    if (!recording.Saved)
                    {
                        row.Text(["text-sm text-destructive"], text: "Not saved");
                        return;
                    }

                    // A private file's URL is signed and expires, so fetch it when someone presses Play
                    row.Button([Button.OutlineSm], text: "Play", ariaLabel: $"Play the recording from {recording.StartedAtUtc:HH:mm}", onClick: async () =>
                    {
                        _playingUrl.Value = await app.Files.Data.GetUrlAsync(recording.Path);
                    });
                });
            }

            if (_playingUrl.Value is { } url)
            {
                col.VideoUrlPlayer(["w-full max-w-lg rounded-lg bg-black"], url: url, controls: true, playsInline: true, key: url);
            }
        });
    }
    #endregion
}
