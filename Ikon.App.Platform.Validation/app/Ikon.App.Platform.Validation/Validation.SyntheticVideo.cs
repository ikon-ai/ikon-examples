using System.Diagnostics;

public partial class Validation
{
    // Matches Data/synthetic-video.h264, regenerate it with:
    //   ffmpeg -f lavfi -i "testsrc2=size=320x240:rate=10:duration=2" -pix_fmt yuv420p -c:v libx264 \
    //     -preset veryfast -tune zerolatency -profile:v baseline -level 3.1 -b:v 400k -g 1 -keyint_min 1 \
    //     -sc_threshold 0 -threads 1 -x264-params repeat-headers=1:sliced-threads=0:slices=1 -f h264 synthetic-video.h264
    private const string SyntheticVideoStreamId = "synthetic";
    private const int SyntheticVideoWidth = 320;
    private const int SyntheticVideoHeight = 240;
    private const double SyntheticVideoFramerate = 10;

    private readonly Reactive<bool> _syntheticVideoRunning = new(false);
    private readonly Reactive<string> _syntheticVideoStatus = new("(idle)");
    private readonly Reactive<string> _syntheticVideoCodec = new("h264");

    private CancellationTokenSource? _syntheticVideoCts;
    private readonly Dictionary<VideoCodec, IReadOnlyList<byte[]>> _syntheticVideoFramesByCodec = new();

    private void RenderSyntheticVideoSection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Synthetic Video");
            view.Text([Text.Caption, "mb-4"], "Streams a pre-encoded test pattern from the app to every client — no camera, no capture, so it isolates the app-to-client direction on its own");

            view.Column([Layout.Column.Md], content: view =>
            {
                // Must match what the viewer's WebRTC peer connection negotiated: the server
                // packetizes every outbound track with that one codec and does not transcode, so
                // a stream in the other one arrives in full and decodes to a black rectangle.
                view.Row([Layout.Row.InlineCenter, "mb-2"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-32"], "Codec");
                    view.Select(
                        value: _syntheticVideoCodec.Value,
                        options:
                        [
                            new SelectOption("h264", "H.264"),
                            new SelectOption("vp8", "VP8"),
                        ],
                        disabled: _syntheticVideoRunning.Value,
                        onValueChange: async v => _syntheticVideoCodec.Value = v);
                });

                view.Row([Layout.Row.InlineCenter, "mb-2"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-32"], "Status");
                    view.Text([Text.Body], _syntheticVideoStatus.Value);
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
                        view.VideoStreamCanvas(
                            [Media.Fill],
                            streamId: SyntheticVideoStreamId,
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

        _syntheticVideoCts = new CancellationTokenSource();
        _syntheticVideoRunning.Value = true;
        _syntheticVideoStatus.Value = $"Streaming {frames.Count} {codec} frames at {SyntheticVideoFramerate:F0} fps";

        var token = _syntheticVideoCts.Token;
        _ = Task.Run(() => RunSyntheticVideoAsync(frames, codec, token));

        await Task.CompletedTask;
    }

    private void StopSyntheticVideo()
    {
        _syntheticVideoCts?.Cancel();
    }

    private async Task RunSyntheticVideoAsync(IReadOnlyList<byte[]> frames, VideoCodec codec, CancellationToken cancellationToken)
    {
        var frameDurationUs = (uint)(1_000_000 / SyntheticVideoFramerate);
        var stopwatch = Stopwatch.StartNew();
        var frameNumber = 0;

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1 / SyntheticVideoFramerate));

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var frame = frames[frameNumber % frames.Count];
                var timestampUs = (ulong)(stopwatch.Elapsed.TotalMilliseconds * 1000);

                Interlocked.Increment(ref _videoFramesToClients);

                // Deliberately untargeted: this is the app-to-every-client fan-out, the exact
                // path a client's own capture must never take.
                await Video.SendFrameAsync(frame, frameNumber, isKey: true, timestampUs, frameDurationUs,
                    codec, SyntheticVideoWidth, SyntheticVideoHeight, SyntheticVideoFramerate,
                    SyntheticVideoStreamId);

                frameNumber++;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _syntheticVideoStatus.Value = $"Stream failed after {frameNumber} frames: {ex.Message}";
        }
        finally
        {
            await Video.CloseAsync(SyntheticVideoStreamId);
            _syntheticVideoRunning.Value = false;

            if (!_syntheticVideoStatus.Value.StartsWith("Stream failed", StringComparison.Ordinal))
            {
                _syntheticVideoStatus.Value = "(idle)";
            }
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
    /// <see cref="Video.SendFrameAsync"/> expects: start codes intact, each keyframe preceded by
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
