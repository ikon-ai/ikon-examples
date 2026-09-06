namespace Ikon.App.Patterns.Patterns;

// Pattern: voice-loop — see docs/patterns/voice-loop.md.
// `app` is the App's primary-constructor handle and UI is its view root; both stand outside the region.
// The docsnippet region is the canonical push-to-talk mic → STT → LLM → TTS loop.
internal sealed class VoiceLoop(IAppBase app) : IPatternDemo
{
    public string Slug => "voice-loop";
    public string Title => "Voice loop";
    public string Category => "Voice";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title, "Push-to-talk mic capture through STT, an LLM turn, and TTS playback back to the client — see docs/patterns/voice-loop.md.");

    private UI UI { get; } = new(app, new IkonTheme());

    #region docsnippet:pattern-voice-loop
    public sealed record VoiceTurn(string Role, string Text);

    private Audio Audio { get; } = new(app);

    private readonly ReactiveList<VoiceTurn> _turns = new();
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
            if (!_streams.TryGetValue(args.StreamId.ToString(), out var samples))
            {
                return;
            }

            var (sampleRate, channelCount) = _streamMeta[args.StreamId.ToString()];
            _streams.Remove(args.StreamId.ToString());
            _streamMeta.Remove(args.StreamId.ToString());

            if (samples.Count == 0)
            {
                return;
            }

            _processing.Value = true;
            try
            {
                using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
                var heard = (await recognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
                {
                    Samples = samples.ToArray(),
                    SampleRate = sampleRate,
                    ChannelCount = channelCount,
                })).Text;

                if (string.IsNullOrWhiteSpace(heard))
                {
                    return;
                }

                _turns.Add(new VoiceTurn("You", heard));

                var transcript = string.Join("\n", _turns.Select(t => $"{t.Role}: {t.Text}"));
                var replyRaw = await Emerge.Run<string>(LLMModel.Claude45Haiku,
                    pass => { pass.Command = $"Conversation:\n{transcript}\n\nReply briefly as the tutor."; });
                var reply = string.IsNullOrEmpty(replyRaw) ? "(no reply)" : replyRaw;
                _turns.Add(new VoiceTurn("Tutor", reply));

                await Audio.SpeakAsync(MediaTargets.Everyone, reply);
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
                        foreach (var t in _turns)
                        {
                            var isUser = t.Role == "You";
                            vv.Box([isUser ? "self-end bg-primary text-primary-foreground" : "self-start bg-surface", "rounded-lg p-3 max-w-[80%]"],
                                content: c => c.Text(text: $"{t.Role}: {t.Text}"));
                        }
                    });
                });

                // _processing is the STT -> LLM -> TTS round trip, not capture state: it is
                // genuine server work, so the caption and disabled: may wait for it. The recording
                // cue itself never does — that comes from the themed default's client-stamped
                // state. disabled: is for "the app is busy" only, never for the microphone
                // permission, which the button asks for itself and cannot ask for while disabled.
                view.PushToTalkButton(
                    style: ["default", "w-auto px-5 gap-2"],
                    text: _processing.Value ? "Thinking…" : "Hold to talk",
                    disabled: _processing.Value);
            });
        });
        return Task.CompletedTask;
    }
    #endregion
}
