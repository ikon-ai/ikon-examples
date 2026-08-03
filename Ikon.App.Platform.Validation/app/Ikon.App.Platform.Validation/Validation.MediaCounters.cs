using StbImageSharp;

public partial class Validation
{
    // Published to the UI on a timer rather than per frame: audio runs at 50 frames a second per
    // stream, and a reactive write per frame would turn a counter into a render storm.
    private static readonly TimeSpan MediaCounterPublishInterval = TimeSpan.FromMilliseconds(500);

    // A microphone that is open but picking up nothing still delivers frames, so presence of
    // audio is not evidence of audio. This is the line between "the pipe works" and "sound came
    // through it" — well under any real capture, well over the noise floor of a muted device.
    private const float AudibleAudioPeak = 0.01f;

    // Likewise for stills: a camera that fails to expose returns a perfectly decodable flat
    // frame. Anything with real content varies far more than this across its pixels.
    private const double NonBlankImageStdDev = 4.0;

    private long _audioFramesFromClients;
    private long _audioFramesToClients;
    private long _videoFramesFromClients;
    private long _videoFramesToClients;

    private readonly Reactive<MediaCounters> _mediaCounters = new(default);
    private readonly Reactive<string> _clientAudioVerdict = new("none yet");
    private readonly Reactive<string> _clientVideoVerdict = new("none yet");
    private readonly Reactive<string> _clientImageVerdict = new("none yet");

    private float _clientAudioPeak;
    private long _clientVideoKeyFrames;
    private long _clientImageCaptures;
    private volatile string _clientVideoDescription = "unknown";

    private void RenderMediaCountersSection(UIView view)
    {
        var counters = _mediaCounters.Value;

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Media Counters");
            view.Text([Text.Caption, "mb-4"], "Frames the app has taken from clients and handed back to them — the direction that stops moving names the broken half");

            view.Column([Layout.Column.Sm], content: view =>
            {
                view.Text([Text.Body], $"Audio frames from clients: {counters.AudioFromClients}");
                view.Text([Text.Body], $"Audio frames to clients: {counters.AudioToClients}");
                view.Text([Text.Body], $"Video frames from clients: {counters.VideoFromClients}");
                view.Text([Text.Body], $"Video frames to clients: {counters.VideoToClients}");
            });

            view.Separator([Separator.Horizontal, "my-4"]);

            view.Text([Text.BodyStrong, "mb-1"], "What the app makes of it");
            view.Text([Text.Caption, "mb-2"], "Arrival is not correctness — a silent stream and a blank frame both count as frames");

            view.Column([Layout.Column.Sm], content: view =>
            {
                view.Text([Text.Body], $"Client audio: {_clientAudioVerdict.Value}");
                view.Text([Text.Body], $"Client video: {_clientVideoVerdict.Value}");
                view.Text([Text.Body], $"Client image: {_clientImageVerdict.Value}");
            });
        });
    }

    /// <summary>
    /// Accumulates the evidence behind the "Client audio" verdict. The peak is per recording, not
    /// per session: a verdict carried over from an earlier recording would report on media that is
    /// no longer flowing, and read as healthy while the current capture delivers nothing.
    /// </summary>
    private void RecordClientAudioFrame(ReadOnlySpan<float> samples, bool isFirst)
    {
        Interlocked.Increment(ref _audioFramesFromClients);

        if (isFirst)
        {
            _clientAudioPeak = 0;
        }

        var peak = 0f;

        foreach (var sample in samples)
        {
            var magnitude = Math.Abs(sample);

            if (magnitude > peak)
            {
                peak = magnitude;
            }
        }

        if (peak > _clientAudioPeak)
        {
            _clientAudioPeak = peak;
        }
    }

    private void RecordClientVideoStreamBegin(VideoInputStreamBeginEventArgs args)
    {
        // The codec here is the one the CLIENT's encoder picked, which is not necessarily the one
        // its WebRTC peer connection negotiated — so it says what arrived, and must not be taken
        // as the codec to send back in.
        _clientVideoDescription = $"{args.Width}x{args.Height} {args.Codec}";
        Interlocked.Exchange(ref _clientVideoKeyFrames, 0);
    }

    private void RecordClientVideoFrame(VideoInputFrameEventArgs args)
    {
        Interlocked.Increment(ref _videoFramesFromClients);

        if (args.IsKey && args.Data.Length > 0)
        {
            Interlocked.Increment(ref _clientVideoKeyFrames);
        }
    }

    /// <summary>
    /// Judges a still the client captured. Decoding is the point: a truncated or mis-encoded
    /// JPEG still has a plausible length and a valid header, and only a decode says otherwise.
    /// </summary>
    private void RecordClientImage(byte[] imageData)
    {
        // Numbered so a reader — and the validation script — can tell this capture's verdict from
        // the one still on screen from the last.
        var capture = Interlocked.Increment(ref _clientImageCaptures);
        ImageResult image;

        try
        {
            image = ImageResult.FromMemory(imageData, ColorComponents.Grey);
        }
        catch (Exception ex)
        {
            _clientImageVerdict.Value = $"undecodable #{capture} ({ex.GetType().Name})";
            return;
        }

        if (image.Width <= 0 || image.Height <= 0 || image.Data.Length == 0)
        {
            _clientImageVerdict.Value = $"empty #{capture}";
            return;
        }

        double sum = 0;
        double sumOfSquares = 0;

        foreach (var luminance in image.Data)
        {
            sum += luminance;
            sumOfSquares += (double)luminance * luminance;
        }

        var mean = sum / image.Data.Length;
        var standardDeviation = Math.Sqrt(Math.Max(0, sumOfSquares / image.Data.Length - mean * mean));

        _clientImageVerdict.Value = standardDeviation >= NonBlankImageStdDev
            ? $"valid #{capture} ({image.Width}x{image.Height}, spread {standardDeviation:F1})"
            : $"blank #{capture} ({image.Width}x{image.Height}, spread {standardDeviation:F1})";
    }

    private void StartMediaCounterPublishing()
    {
        var cancellation = new CancellationTokenSource();
        app.OnStopping(() =>
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        });

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(MediaCounterPublishInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(cancellation.Token))
                {
                    PublishMediaCounters();
                }
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private void PublishMediaCounters()
    {
        var counters = new MediaCounters(
            Interlocked.Read(ref _audioFramesFromClients),
            Interlocked.Read(ref _audioFramesToClients),
            Interlocked.Read(ref _videoFramesFromClients),
            Interlocked.Read(ref _videoFramesToClients));

        if (counters != _mediaCounters.Value)
        {
            _mediaCounters.Value = counters;
        }

        var audioVerdict = counters.AudioFromClients == 0
            ? "none yet"
            : _clientAudioPeak >= AudibleAudioPeak
                ? $"valid (peak {_clientAudioPeak:F3})"
                : $"silent (peak {_clientAudioPeak:F3})";

        if (audioVerdict != _clientAudioVerdict.Value)
        {
            _clientAudioVerdict.Value = audioVerdict;
        }

        var keyFrames = Interlocked.Read(ref _clientVideoKeyFrames);
        var videoVerdict = counters.VideoFromClients == 0
            ? "none yet"
            : keyFrames == 0
                ? $"no keyframe ({counters.VideoFromClients} frames)"
                : $"valid ({_clientVideoDescription}, {keyFrames} keyframes)";

        if (videoVerdict != _clientVideoVerdict.Value)
        {
            _clientVideoVerdict.Value = videoVerdict;
        }
    }
}

internal readonly record struct MediaCounters(long AudioFromClients, long AudioToClients, long VideoFromClients, long VideoToClients);
