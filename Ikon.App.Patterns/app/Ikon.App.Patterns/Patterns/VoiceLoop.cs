namespace Ikon.App.Patterns.Patterns;

// Pattern: voice-loop — see docs/patterns/voice-loop.md.
// `app` is the App's primary-constructor handle and UI is its view root; both stand outside the region.
// The example region is the canonical push-to-talk mic → STT → LLM → TTS loop.
internal sealed class VoiceLoop(IAppBase app) : IPatternDemo
{
    public string Slug => "voice-loop";
    public string Title => "Voice loop";
    public string Category => "Voice";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title, "Push-to-talk mic capture through STT, an LLM turn, and TTS playback back to the client — see docs/patterns/voice-loop.md.");

    private UI UI { get; } = new(app, new IkonTheme());

    #region example:pattern-voice-loop
    public sealed record VoiceTurn(string Role, string Text);

    private Audio Audio { get; } = new(app);

    private readonly ReactiveList<VoiceTurn> _turns = new();
    private readonly ClientReactive<bool> _processing = new(false);

    // Per-press sample buffer and per-stream format, both keyed by stream id.
    private readonly Dictionary<string, List<float>> _presses = new();
    private readonly Dictionary<string, (int SampleRate, int ChannelCount)> _streamMeta = new();

    public Task Main()
    {
        // The stream is the open microphone, and off iOS it OUTLIVES a press: it begins once and ends
        // when the page closes, the button unmounts or the device changes (any iOS browser ends it after every press). Waiting for
        // AudioInputStreamEndAsync to act on a release means nothing happens until then. It carries the format and is where that is forgotten.
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            _streamMeta[args.StreamId] = (args.SampleRate, args.ChannelCount);
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            _presses.Remove(args.StreamId);
            _streamMeta.Remove(args.StreamId);
        };

        // ONE press is one segment: its first frame has IsFirst, its last has IsLast. The
        // release is the IsLast frame -- that is the moment to transcribe.
        Audio.AudioInputFrameAsync += async args =>
        {
            if (args.IsFirst || !_presses.TryGetValue(args.StreamId, out var samples))
            {
                samples = new List<float>();
                _presses[args.StreamId] = samples;
            }

            samples.AddRange(args.Samples);

            if (!args.IsLast)
            {
                return;
            }

            _presses.Remove(args.StreamId);

            if (samples.Count == 0 || !_streamMeta.TryGetValue(args.StreamId, out var format))
            {
                return;
            }

            var (sampleRate, channelCount) = format;

            // The frame handler holds up every client's messages for as long as it runs, and the
            // round trip below takes seconds, so start it and return. A task started here keeps
            // the client's scope, so _processing still writes to this client.
            _ = Task.Run(() => RespondAsync(samples.ToArray(), sampleRate, channelCount));
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
                            vv.Box([isUser ? "self-end bg-brand-solid text-primary-on-brand" : "self-start bg-card", "rounded-lg p-3 max-w-[80%]"],
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

    private async Task RespondAsync(float[] samples, int sampleRate, int channelCount)
    {
        _processing.Value = true;
        try
        {
            using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
            var heard = (await recognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
            {
                Samples = samples,
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

            Audio.Speak(MediaTargets.Everyone, reply);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Voice turn failed: {ex.Message}");
            _turns.Add(new VoiceTurn("Tutor", "Sorry, I couldn't answer that. Try again."));
        }
        finally
        {
            _processing.Value = false;
        }
    }
    #endregion
}
