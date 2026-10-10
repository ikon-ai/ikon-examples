public partial class Validation
{
    // Matches Data/synthetic-video.h264, regenerate it with:
    //   ffmpeg -f lavfi -i "testsrc2=size=320x240:rate=10:duration=2" -pix_fmt yuv420p -c:v libx264 \
    //     -preset veryfast -tune zerolatency -profile:v baseline -level 3.1 -b:v 400k -g 1 -keyint_min 1 \
    //     -sc_threshold 0 -threads 1 -x264-params repeat-headers=1:sliced-threads=0:slices=1 -f h264 synthetic-video.h264
    private const string SyntheticVideoSurface = "synthetic";
    private const int SyntheticVideoWidth = 320;
    private const int SyntheticVideoHeight = 240;
    private const double SyntheticVideoFramerate = 10;

    private readonly Reactive<bool> _syntheticVideoRunning = new(false);
    private readonly Reactive<string> _syntheticVideoStatus = new("(idle)");
    private readonly Reactive<string> _syntheticVideoCodec = new("h264");

    private LiveVideoPlayback? _syntheticVideo;
    private readonly Dictionary<VideoCodec, IReadOnlyList<byte[]>> _syntheticVideoFramesByCodec = new();

    private void RenderSyntheticVideoSection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Synthetic Video");

            view.Column([Layout.Column.Md], content: view =>
            {
                // Must match what the viewer's WebRTC peer connection negotiated: the server
                // packetizes every outbound track with that one codec and does not transcode, so
                // a stream in the other one arrives in full and decodes to a black rectangle.
                view.Row([Layout.Row.InlineCenter, "mb-2 flex-wrap"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-32"], "Codec");
                    view.Select(
                        value: _syntheticVideoCodec.Value,
                        options:
                        [
                            new SelectOption("h264", "H.264"),
                            new SelectOption("vp8", "VP8"),
                            new SelectOption("ffmpeg", "H.264 from ffmpeg"),
                        ],
                        disabled: _syntheticVideoRunning.Value,
                        onValueChange: async v => _syntheticVideoCodec.Value = v);
                });

                view.Row([Layout.Row.InlineCenter, "mb-2 flex-wrap"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-32"], "Status");
                    view.Text([Text.Body], _syntheticVideoStatus.Value, props: TestId("video-synthetic-status"));
                });

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Button(
                        [_syntheticVideoRunning.Value ? Button.OutlineMd : Button.PrimaryMd],
                        text: "Start Synthetic Video",
                        disabled: _syntheticVideoRunning.Value,
                        onClick: async () => await StartSyntheticVideoAsync());

                    view.Button([Button.ErrorMd],
                        text: "Stop Synthetic Video",
                        disabled: !_syntheticVideoRunning.Value,
                        onClick: async () => StopSyntheticVideo());
                });

                if (_syntheticVideoRunning.Value)
                {
                    view.Box([Media.VideoContainer], content: view =>
                    {
                        view.VideoSurface(
                            [Media.Fill], surface: SyntheticVideoSurface,
                            width: SyntheticVideoWidth,
                            height: SyntheticVideoHeight);
                    });
                }
                else
                {
                    view.Box([Media.EmptyState], content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([Media.PlaceholderIcon], name: "video-off");
                            view.Text([Media.PlaceholderText], "No synthetic stream");
                            view.Text([Media.PlaceholderHint], "Click Start Synthetic Video to begin");
                        });
                    });
                }
            });
        });
    }

    private async Task StartSyntheticVideoAsync()
    {
        if (_syntheticVideoRunning.Value)
        {
            return;
        }

        if (_syntheticVideoCodec.Value == "ffmpeg")
        {
            StartFfmpegSyntheticVideo();
            return;
        }

        var codec = _syntheticVideoCodec.Value == "vp8" ? VideoCodec.Vp8 : VideoCodec.H264;
        IReadOnlyList<byte[]> frames;

        try
        {
            frames = LoadSyntheticVideoFrames(codec);
        }
        catch (Exception ex)
        {
            _syntheticVideoStatus.Value = $"Failed to load the {codec} test pattern: {ex.Message}";
            return;
        }

        // Deliberately to everyone: this is the app-to-every-client fan-out, the exact path a
        // client's own capture must never take.
        var live = Video.PlayLive(MediaTargets.Everyone, SyntheticVideoSurface, codec);
        _syntheticVideo = live;
        _syntheticVideoRunning.Value = true;
        _syntheticVideoStatus.Value = $"Streaming {frames.Count} {codec} frames at {SyntheticVideoFramerate:F0} fps, waiting for a viewer";

        _ = Task.Run(() => RunSyntheticVideoAsync(live, frames, codec));
        _ = ReportSyntheticVideoStartedAsync(live, frames.Count, codec);

        await Task.CompletedTask;
    }

    private void StopSyntheticVideo()
    {
        _syntheticVideo?.Stop();
    }

    private async Task RunSyntheticVideoAsync(LiveVideoPlayback live, IReadOnlyList<byte[]> frames, VideoCodec codec)
    {
        var frameNumber = 0;

        try
        {
            // The fixture runs at a fixed rate, so each frame's presentation time is known and the
            // playback paces the writes: no timer here
            while (true)
            {
                var frame = frames[frameNumber % frames.Count];
                var timestamp = TimeSpan.FromSeconds(frameNumber / SyntheticVideoFramerate);

                if (!await live.WriteAsync(frame, isKey: true, timestamp))
                {
                    break;
                }

                Interlocked.Increment(ref _videoFramesToClients);
                frameNumber++;
            }
        }
        catch (Exception ex)
        {
            _syntheticVideoStatus.Value = $"Stream failed after {frameNumber} frames: {ex.Message}";
        }
        finally
        {
            await live.DisposeAsync();
            var outcome = await live.Completion;
            _syntheticVideoRunning.Value = false;

            if (outcome == VideoPlaybackOutcome.Failed)
            {
                _syntheticVideoStatus.Value = $"Stream failed after {frameNumber} frames: {live.Error?.Message}";
            }
            else if (!_syntheticVideoStatus.Value.StartsWith("Stream failed", StringComparison.Ordinal))
            {
                _syntheticVideoStatus.Value = $"(idle, the last {codec} stream ended {outcome} after {frameNumber} frames)";
            }
        }
    }

    // A live encoder rather than the fixture: x264 cuts each picture into slices and its output
    // carries no timestamps, which LiveVideoPlayback.WriteH264StreamAsync is there to absorb
    private void StartFfmpegSyntheticVideo()
    {
        // The surface first: a playback that cannot start must not leave an encoder running
        var live = Video.PlayLive(MediaTargets.Everyone, SyntheticVideoSurface, VideoCodec.H264);
        System.Diagnostics.Process ffmpeg;

        try
        {
            ffmpeg = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ffmpeg",
                $"-hide_banner -loglevel error -f lavfi -i testsrc2=size={SyntheticVideoWidth}x{SyntheticVideoHeight}:rate={SyntheticVideoFramerate} " +
                "-c:v libx264 -profile:v baseline -pix_fmt yuv420p -preset veryfast -tune zerolatency -bf 0 -g 10 " +
                "-x264-params repeat-headers=1 -f h264 pipe:1")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            })!;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            live.Stop();
            _syntheticVideoStatus.Value = "SKIP ffmpeg is not installed where the app runs";
            return;
        }

        _syntheticVideo = live;
        _syntheticVideoRunning.Value = true;
        _syntheticVideoStatus.Value = "Streaming ffmpeg's test pattern, waiting for a viewer";

        _ = Task.Run(() => RunFfmpegSyntheticVideoAsync(live, ffmpeg));
        _ = ReportFfmpegSyntheticVideoStartedAsync(live);
    }

    private async Task RunFfmpegSyntheticVideoAsync(LiveVideoPlayback live, System.Diagnostics.Process ffmpeg)
    {
        // Drained, or a chatty encoder blocks on a full pipe; kept, to say why one that quits early did
        var errors = ffmpeg.StandardError.ReadToEndAsync();

        try
        {
            // The test pattern never ends: a stream that does means ffmpeg exited on its own
            if (await live.WriteH264StreamAsync(ffmpeg.StandardOutput.BaseStream, SyntheticVideoFramerate))
            {
                await ffmpeg.WaitForExitAsync();
                _syntheticVideoStatus.Value = $"FAIL ffmpeg exited with {ffmpeg.ExitCode}: {OneLine(await errors)}";
            }
        }
        catch (Exception ex)
        {
            _syntheticVideoStatus.Value = $"FAIL ffmpeg stream: {ex.Message}";
        }
        finally
        {
            try
            {
                ffmpeg.Kill();
            }
            catch (InvalidOperationException)
            {
                // It already exited: the stream ended because ffmpeg did
            }

            ffmpeg.Dispose();
            await live.DisposeAsync();
            var outcome = await live.Completion;
            _syntheticVideoRunning.Value = false;

            if (!_syntheticVideoStatus.Value.StartsWith("FAIL", StringComparison.Ordinal))
            {
                _syntheticVideoStatus.Value = $"(idle, the last ffmpeg stream ended {outcome})";
            }
        }
    }

    private async Task ReportFfmpegSyntheticVideoStartedAsync(LiveVideoPlayback live)
    {
        await live.Started;

        if (!live.IsEnded)
        {
            _syntheticVideoStatus.Value = "PASS ffmpeg: its H.264 stream reached a viewer through WriteH264StreamAsync";
        }
    }

    private async Task ReportSyntheticVideoStartedAsync(LiveVideoPlayback live, int frameCount, VideoCodec codec)
    {
        await live.Started;

        if (!live.IsEnded)
        {
            _syntheticVideoStatus.Value = $"Streaming {frameCount} {codec} frames at {SyntheticVideoFramerate:F0} fps, the first frame reached a viewer";
        }
    }

    /// <summary>
    /// Loads the test pattern pre-encoded in the codec the clients' peer connections negotiated.
    /// The bridge packetizes every outbound track with that one codec, so a stream encoded in the
    /// other one arrives as bytes the browser cannot decode — plenty of traffic, a black canvas.
    /// </summary>
    private IReadOnlyList<byte[]> LoadSyntheticVideoFrames(VideoCodec codec)
    {
        if (_syntheticVideoFramesByCodec.TryGetValue(codec, out var cached))
        {
            return cached;
        }

        var fileName = codec == VideoCodec.Vp8 ? "synthetic-video.ivf" : "synthetic-video.h264";
        var path = Path.Combine(app.DataDirectory, fileName);
        var data = File.ReadAllBytes(path);
        var frames = codec == VideoCodec.Vp8 ? SplitIvfFrames(data) : SplitAnnexBAccessUnits(data);

        if (frames.Count == 0)
        {
            throw new InvalidOperationException($"No frames found in {path}");
        }

        _syntheticVideoFramesByCodec[codec] = frames;
        return frames;
    }

    /// <summary>
    /// Splits an IVF elementary stream into its VP8 frames: a 32-byte file header, then each frame
    /// behind a 12-byte header whose first four bytes are the frame length.
    /// </summary>
    private static List<byte[]> SplitIvfFrames(byte[] stream)
    {
        const int FileHeaderLength = 32;
        const int FrameHeaderLength = 12;

        var frames = new List<byte[]>();

        if (stream.Length < FileHeaderLength || stream[0] != 'D' || stream[1] != 'K' || stream[2] != 'I' || stream[3] != 'F')
        {
            throw new InvalidOperationException("Not an IVF stream");
        }

        int offset = BitConverter.ToUInt16(stream, 6);

        while (offset + FrameHeaderLength <= stream.Length)
        {
            var frameLength = BitConverter.ToInt32(stream, offset);
            offset += FrameHeaderLength;

            if (frameLength <= 0 || offset + frameLength > stream.Length)
            {
                break;
            }

            frames.Add(stream.AsSpan(offset, frameLength).ToArray());
            offset += frameLength;
        }

        return frames;
    }

    /// <summary>
    /// Splits an Annex-B elementary stream into access units, one per encoded frame, in the shape
    /// <see cref="LiveVideoPlayback.WriteAsync"/> expects: start codes intact, each keyframe preceded by
    /// its own SPS and PPS. The fixture is encoded with every frame a keyframe carrying repeated
    /// headers, so an SPS NAL is exactly a frame boundary and any frame can start a stream.
    /// </summary>
    private static List<byte[]> SplitAnnexBAccessUnits(byte[] stream)
    {
        var accessUnitStarts = new List<int>();

        for (var i = 0; i + 3 < stream.Length; i++)
        {
            if (stream[i] != 0 || stream[i + 1] != 0 || stream[i + 2] != 1)
            {
                continue;
            }

            const int SequenceParameterSet = 7;

            if ((stream[i + 3] & 0x1f) == SequenceParameterSet)
            {
                // A four-byte start code is a three-byte one with a leading zero; keep it whole.
                accessUnitStarts.Add(i > 0 && stream[i - 1] == 0 ? i - 1 : i);
            }

            i += 2;
        }

        var accessUnits = new List<byte[]>(accessUnitStarts.Count);

        for (var i = 0; i < accessUnitStarts.Count; i++)
        {
            var start = accessUnitStarts[i];
            var end = i + 1 < accessUnitStarts.Count ? accessUnitStarts[i + 1] : stream.Length;
            accessUnits.Add(stream.AsSpan(start, end - start).ToArray());
        }

        return accessUnits;
    }
}
