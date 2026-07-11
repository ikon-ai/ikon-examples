<!-- mined-from: VoiceTutor + Tori (verified against live API surface 2026-05-02) -->
# Voice Loop — Mic → STT → LLM → TTS

Push-to-talk: client mic streams to the app; on stream end, transcribe with SpeechRecognizer, ask an LLM, and speak the reply with `Audio.SpeakAsync`.

## When to use

Voice tutor, voice journaling, audio conversation with a character, voice command interface, language practice partner.

## Snippet

```csharp
public sealed record VoiceTurn(string Role, string Text);

private Audio Audio { get; } = new(app);

private readonly Reactive<List<VoiceTurn>> _turns = new([]);
private readonly ClientReactive<bool> _processing = new(false);

// Per-stream sample buffer keyed by stream id.
private readonly Dictionary<string, List<float>> _streams = new();
private readonly Dictionary<string, (int SampleRate, int ChannelCount)> _streamMeta = new();

public Task Main()
{
    Audio.AudioInputStreamBeginAsync += async args =>
    {
        _streams[args.StreamId.ToString()] = new List<float>();
        _streamMeta[args.StreamId.ToString()] = (args.SampleRate, args.ChannelCount);
    };

    Audio.AudioInputFrameAsync += async args =>
    {
        if (_streams.TryGetValue(args.StreamId.ToString(), out var buf))
        {
            buf.AddRange(args.Samples);
        }
    };

    Audio.AudioInputStreamEndAsync += async args =>
    {
        if (!_streams.TryGetValue(args.StreamId.ToString(), out var samples)) return;
        var (sampleRate, channelCount) = _streamMeta[args.StreamId.ToString()];
        _streams.Remove(args.StreamId.ToString());
        _streamMeta.Remove(args.StreamId.ToString());
        if (samples.Count == 0) return;

        _processing.Value = true;
        try
        {
            using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
            var heard = await recognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
            {
                Samples = samples.ToArray(),
                SampleRate = sampleRate,
                ChannelCount = channelCount,
            });
            if (string.IsNullOrWhiteSpace(heard)) return;
            _turns.Value = [.. _turns.Value, new VoiceTurn("You", heard)];

            var transcript = string.Join("\n", _turns.Value.Select(t => $"{t.Role}: {t.Text}"));
            var replyRaw = await Emerge.Run<string>(LLMModel.Claude45Haiku,
                pass => { pass.Command = $"Conversation:\n{transcript}\n\nReply briefly as the tutor."; })
                .ResultAsync();
            var reply = string.IsNullOrEmpty(replyRaw) ? "(no reply)" : replyRaw;
            _turns.Value = [.. _turns.Value, new VoiceTurn("Tutor", reply)];

            await Audio.SpeakAsync(reply);
        }
        finally
        {
            _processing.Value = false;
        }
    };

    UI.Root([Page.Default], content: view =>
    {
        view.Column([Layout.Page, "p-6 gap-4"], content: view =>
        {
            view.ScrollArea(rootStyle: ["flex-1 min-h-0"], content: v =>
            {
                v.Column(["gap-3 p-2"], content: vv =>
                {
                    foreach (var t in _turns.Value)
                    {
                        var isUser = t.Role == "You";
                        vv.Box([isUser ? "self-end bg-primary text-primary-foreground" : "self-start bg-surface", "rounded-lg p-3 max-w-[80%]"],
                            content: c => c.Text(text: $"{t.Role}: {t.Text}"));
                    }
                });
            });

            view.CaptureButton(
                style: [Button.Default, "transition-colors duration-150 hover:opacity-90"],
                mode: MediaCaptureButtonMode.Hold,
                disabled: _processing.Value,
                content: c => c.Text(text: _processing.Value ? "Thinking…" : "Hold to talk"));
        });
    });
    return Task.CompletedTask;
}
```

## Notes

- `Audio` is a field on the App class: `private Audio Audio { get; } = new(app);`. Subscribe events through it — there is no `app.AudioInputStreamBeginAsync` shortcut; that produces CS1061.
- The audio input streams as frames. Buffer samples per `StreamId` in a Dictionary; flush at `AudioInputStreamEndAsync`. `args.AudioStream` does NOT exist on the args; you reconstruct the buffer yourself.
- `view.CaptureButton(mode: MediaCaptureButtonMode.Hold, ...)` is the platform's push-to-talk primitive — no separate Start/Stop wiring needed. (`Toggle` mode is the alternative for tap-to-start tap-to-stop.)
- `await Audio.SpeakAsync(text)` runs the whole TTS chain — generation, streaming, playback — and a new call fades out and replaces the previous utterance (barge-in for free). Optional parameters pick the model/voice (`Audio.SpeakAsync(text, SpeechGeneratorModel.Eleven3, voice: "Aria")`) or target specific clients (`targetIds:`).
- Hand-roll the loop only for custom mixing, no-interrupt overlap, raw sample access, or config beyond text+voice: `using var tts = new SpeechGenerator(model); await foreach (var audio in tts.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = reply })) { Audio.SendSpeech(audio); }`. `AudioChunk` carries PCM samples (`float[] Samples`, `int SampleRate`, `int ChannelCount`) — it does NOT have `.Data` or `.MimeType` properties; do not call `ClientFunctions.PlaySoundAsync(chunk.Data, chunk.MimeType)` (that combination doesn't compile).
- `ClientFunctions.PlaySoundAsync(byte[] bytes, string mimeType)` is for playing already-encoded sound files (MP3, WAV); use `Audio.SpeakAsync` / `Audio.SendSpeech` for generated speech.
- Wrap STT + LLM + TTS in try/finally so `_processing` always resets.

## See also

- `chatbot-streaming` — text-only equivalent.
- `push-to-talk-button` — just the CaptureButton primitive in isolation.
- `ai-speech-and-audio` (top-level guide) — full SpeechRecognizer + SpeechGenerator API.
- `busy-flag-loading` — generalised async pattern.
