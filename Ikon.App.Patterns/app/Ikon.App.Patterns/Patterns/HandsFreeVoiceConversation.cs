namespace Ikon.App.Patterns.Patterns;

// Pattern: hands-free-voice-conversation — see docs/patterns/hands-free-voice-conversation.md.
// `app` is the App's primary-constructor handle and UI is its view root; both stand outside the region.
internal sealed class HandsFreeVoiceConversation(IAppBase app) : IPatternDemo
{
    public string Slug => "hands-free-voice-conversation";
    public string Title => "Hands-free voice conversation";
    public string Category => "Voice";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Open-mic conversation: turn detection instead of a press per sentence, barge-in that stops the reply and its model call, music ducked under both voices, and the raw microphone recorded beside it. It needs a live microphone — see docs/patterns/hands-free-voice-conversation.md.");

    private UI UI { get; } = new(app, new IkonTheme());


    #region example:pattern-hands-free-voice-conversation
    public sealed record Line(string Speaker, string Text);

    private Audio Audio { get; } = new(app);

    // The background track's absolute URL, such as a stored public asset's.
    private readonly Uri _musicUrl = new("https://example.com/lesson-loop.mp3");

    private readonly ReactiveList<Line> _lines = new();
    private readonly ClientReactive<bool> _listening = new(false);

    // The reply in progress per client, so a new turn can cancel the model call and the speech.
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _replies = new();

    // The raw microphone per stream, recorded beside turn detection from the same frames.
    private readonly ConcurrentDictionary<string, List<float>> _recordings = new();
    private readonly ConcurrentDictionary<string, (int SampleRate, int ChannelCount)> _formats = new();

    public Task Main()
    {
        // A turn ends after 1.2 s of silence rather than the default 0.6 s, so someone who pauses
        // mid-sentence (a learner, a slow speaker) is not split into two turns. With
        // pauseWhileAppSpeaking: false the microphone stays live while the app talks, so the user
        // can interrupt it (barge-in).
        Audio.UseTurnDetection(
            SpeechRecognizerModel.WhisperLarge3Turbo,
            config: new TurnDetectorConfig
            {
                TurnEndSilence = TimeSpan.FromSeconds(1.2),
                SpeculativeSilence = TimeSpan.FromSeconds(0.7),
            },
            pauseWhileAppSpeaking: false);

        Audio.TurnStartedAsync += async args =>
        {
            var speaker = MediaTargets.To([args.ClientSessionId]);

            // Barge-in: stop the line being talked over, and the model call that would speak the next.
            Audio.Stop(speaker, "speech");

            if (_replies.TryRemove(args.ClientSessionId, out var reply))
            {
                await reply.CancelAsync();
            }

            Audio.SetSlotVolume(speaker, "music", 0.15f, TimeSpan.FromMilliseconds(200));
            _listening.SetFor(args.ClientSessionId, true);
        };

        Audio.SpeechRecognizedAsync += async args =>
        {
            EndTurn(args.ClientSessionId);

            if (string.IsNullOrWhiteSpace(args.Text))
            {
                return;
            }

            _lines.Add(new Line("You", args.Text));

            var reply = new CancellationTokenSource();
            _replies[args.ClientSessionId] = reply;
            _ = Task.Run(() => ReplyAsync(args.ClientSessionId, reply.Token));
        };

        // Exactly one of SpeechRecognizedAsync and this ends every turn, so both release the turn.
        Audio.SpeechNotRecognizedAsync += async args => EndTurn(args.ClientSessionId);

        Audio.AudioInputStreamBeginAsync += async args => _formats[args.StreamId] = (args.SampleRate, args.ChannelCount);

        Audio.AudioInputFrameAsync += async args =>
        {
            // One open-to-close of the mic is one recording: IsFirst starts it, IsLast ends it.
            if (args.IsFirst)
            {
                _recordings[args.StreamId] = [];
            }

            if (_recordings.TryGetValue(args.StreamId, out var samples))
            {
                samples.AddRange(args.Samples);
            }

            if (args.IsLast && _recordings.TryRemove(args.StreamId, out var recorded) && _formats.TryGetValue(args.StreamId, out var format))
            {
                _ = SaveRecordingAsync([.. recorded], format.SampleRate, format.ChannelCount);
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            _recordings.TryRemove(args.StreamId, out _);
            _formats.TryRemove(args.StreamId, out _);
        };

        // Background music in its own slot, which the turns below duck.
        app.OnClientJoined(async ctx =>
            Audio.Play(MediaTargets.To([ctx.ClientSessionId]), _musicUrl,
                new PlayOptions { Slot = "music", Loop = true, Volume = 0.5f }));

        UI.Root([Page.Default], content: view =>
        {
            view.Column([Layout.Page, "p-6 gap-4"], content: view =>
            {
                foreach (var line in _lines)
                {
                    view.Text(text: $"{line.Speaker}: {line.Text}");
                }

                view.Text([Text.Caption], _listening.Value ? "Listening…" : "Tap the microphone once and talk");

                // MicToggleButton turns echo cancellation on by default. An open mic next to a loudspeaker
                // needs it, or the app's own voice is picked up again and starts new turns.
                view.MicToggleButton(text: "Talk");
            });
        });

        return Task.CompletedTask;
    }

    // Recordings go to the app's private storage as WAV files.
    private async Task SaveRecordingAsync(float[] samples, int sampleRate, int channelCount)
    {
        using var wav = new WavFile(sampleRate, channelCount, WavFile.SampleFormat.Short);
        wav.AddSamples(samples);
        await app.Files.Data.WriteBytesAsync($"recordings/{Guid.NewGuid():N}.wav", wav.AsArray(), "audio/wav");
    }

    private void EndTurn(int clientId)
    {
        Audio.SetSlotVolume(MediaTargets.To([clientId]), "music", 1f, TimeSpan.FromMilliseconds(800));
        _listening.SetFor(clientId, false);
    }

    private async Task ReplyAsync(int clientId, CancellationToken ct)
    {
        try
        {
            var transcript = string.Join("\n", _lines.Select(l => $"{l.Speaker}: {l.Text}"));
            var reply = await Emerge.Run<string>(LLMModel.Claude45Haiku, pass =>
            {
                pass.Command = $"Conversation:\n{transcript}\n\nReply in one or two short spoken sentences.";
            }, ct);

            _lines.Add(new Line("Tutor", reply));

            // The music dips under the app's own voice too, and comes back when the line ends.
            Audio.Speak(MediaTargets.To([clientId]), reply,
                options: new PlayOptions { Duck = [new SlotDuck("music", 0.2f)] });
        }
        catch (OperationCanceledException)
        {
            // The user spoke again before this reply was ready, and their new turn gets its own reply.
        }
        catch (Exception ex)
        {
            Log.Instance.Warning(ex, $"Voice reply for client {clientId} failed");
            _lines.Add(new Line("Tutor", "Sorry, I lost that. Say it again?"));
        }
    }
    #endregion
}
