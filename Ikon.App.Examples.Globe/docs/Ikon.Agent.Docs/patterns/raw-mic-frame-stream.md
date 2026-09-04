<!-- mined-from: Ikon.App.Patterns -->
# Raw Microphone Frames — The Layer Below Recognition

`AudioInputFrameAsync` hands the app decoded **float PCM, live**, as the client captures it. That
is the layer beneath speech recognition — reach for it for a level meter, a waveform, a custom
recognizer or a recording, and not for transcription, which `UseSpeechRecognition` already does.

## When to use

Anything that needs the audio itself rather than what was said. If you want the words, use
`SpeechRecognizedAsync`; if you want a mouth to move in time with playback, that is
`audio-reactive-visuals`.

## Notes

- **`AsyncEventHandler<T>` takes ONE argument.** The `(sender, args)` shape from ordinary .NET
  events is CS1593 here.
- **The BEGIN event carries the format.** `Samples` in a frame are at that stream's `SampleRate`,
  so a handler that hardcodes 48 kHz is wrong on any client capturing at something else.
- **`IsFirst`/`IsLast` bracket ONE captured segment** — one push-to-talk press — not the whole
  session. Per-utterance state resets on `IsFirst`, not on stream begin.
- Handlers run outside any client's scope, so a write names its target: `SetFor(clientSessionId, …)`
  rather than `.Value`, which throws rather than silently writing to nowhere.
- **`CorrelationId` is set by the `CaptureButton` that started the stream** and is null for an
  ad-hoc one — the hook for telling "this press" apart from background audio.
- A begin handler may set `args.StreamingMode` to control delivery: live, or buffered until the
  total duration is known.
- Wire these **once at setup**, not per client — they are app-level events carrying the client id.
- `SpeechNotRecognizedAsync` fires for a segment that produced no speech, and exactly one of it and
  `SpeechRecognizedAsync` fires per segment. An app that latches a "Transcribing…" state when
  capture stops must release it in **both**, or a silent press leaves the spinner stuck on.

## Snippet

```csharp
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
```

## See also

- `push-to-talk-button` — the CaptureButton that originates a correlated stream.
- `voice-loop` — the recognition path, when the words are what you want.
