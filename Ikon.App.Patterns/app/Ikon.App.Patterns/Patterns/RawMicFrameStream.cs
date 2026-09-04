namespace Ikon.App.Patterns.Patterns;

// Pattern: raw-mic-frame-stream — see docs/patterns/raw-mic-frame-stream.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class RawMicFrameStream : IPatternDemo
{
    public string Slug => "raw-mic-frame-stream";
    public string Title => "Raw microphone frames";
    public string Category => "Voice & audio";
    public void RenderDemo(IView view) => Render(view);

    private Audio Audio => throw new NotImplementedException();

    #region docsnippet:pattern-raw-mic-frame-stream
    private readonly ClientReactive<double> _level = new(0);
    private readonly ClientReactive<int> _sampleRate = new(0);

    /// <summary>
    /// The frame events are the layer BELOW speech recognition: decoded float PCM, live. Reach for
    /// them for a level meter, a custom recognizer or a recording — not for transcription, which
    /// UseSpeechRecognition already does.
    ///
    /// Wire these once at setup, not per client.
    /// </summary>
    private void WireCapture()
    {
        // The BEGIN event carries the format. Samples in the frame event are at THIS rate, so a
        // handler that assumes 48 kHz is wrong on any client that captures at something else.
        // AsyncEventHandler<T> takes ONE argument. The (sender, args) shape from ordinary .NET
        // events is CS1593 here.
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            _sampleRate.SetFor(args.ClientSessionId, args.SampleRate);
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            // IsFirst/IsLast bracket ONE captured segment -- one push-to-talk press -- rather than
            // the whole session, so per-utterance state resets on IsFirst, not on stream begin.
            if (args.IsFirst)
            {
                _level.SetFor(args.ClientSessionId, 0);
            }

            var sum = 0.0;

            foreach (var sample in args.Samples)
            {
                sum += sample * sample;
            }

            // A handler runs off any client's scope, so writes name their target explicitly --
            // a bare .Value would throw rather than silently write to nowhere.
            _level.SetFor(args.ClientSessionId, Math.Sqrt(sum / Math.Max(args.Samples.Length, 1)));
        };

        // CorrelationId is set by the CaptureButton that started the stream and is null for an
        // ad-hoc one -- the hook for telling "this press" from background audio.
        Audio.AudioInputStreamEndAsync += async args =>
        {
            Log.Instance.Debug($"Stream ended for {args.ClientSessionId} ({args.CorrelationId ?? "ad-hoc"})");
        };
    }

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            col.Progress(value: _level.Value * 100, max: 100);
            col.Text(["text-muted-foreground text-xs"], text: $"{_sampleRate.Value} Hz");
        });
    }
    #endregion
}
