namespace Ikon.App.Patterns.Patterns;

// Pattern: record-then-analyse-audio — see docs/patterns/record-then-analyse-audio.md.
// The demo draws the record button; recording needs a running app's microphone stream.
internal sealed class RecordThenAnalyseAudio(IAppBase app) : IPatternDemo
{
    public string Slug => "record-then-analyse-audio";
    public string Title => "Record, then analyse";
    public string Category => "Voice & audio";

    private Audio Audio { get; } = new(app);

    public void RenderDemo(IView view) => Render((UIView)view);

    #region example:pattern-record-then-analyse-audio
    private sealed record Take(string Id, string Transcript, string Feedback);

    // One take runs from opening the microphone to closing it. The samples are collected per stream.
    private readonly ConcurrentDictionary<string, List<float>> _recording = new();
    private readonly ConcurrentDictionary<string, (int SampleRate, int ChannelCount)> _formats = new();

    private readonly ClientReactiveList<Take> _takes = new();
    private readonly ClientReactive<bool> _analysing = new(false);

    // Five minutes of 48 kHz float samples is about 58 MB of memory per take
    private static readonly TimeSpan MaxTake = TimeSpan.FromMinutes(5);

    /// <summary>Call once from Main.</summary>
    private void WireRecording()
    {
        // The begin event carries the format; frames carry only samples
        Audio.AudioInputStreamBeginAsync += async args => _formats[args.StreamId] = (args.SampleRate, args.ChannelCount);

        Audio.AudioInputFrameAsync += async args =>
        {
            if (args.IsFirst)
            {
                _recording[args.StreamId] = [];
            }

            if (!_recording.TryGetValue(args.StreamId, out var samples) || !_formats.TryGetValue(args.StreamId, out var format))
            {
                return;
            }

            if (samples.Count < MaxTake.TotalSeconds * format.SampleRate * format.ChannelCount)
            {
                samples.AddRange(args.Samples);
            }

            if (args.IsLast && _recording.TryRemove(args.StreamId, out var take))
            {
                // Analysis takes seconds, and the frame handler has to return at once
                var clientSessionId = args.ClientSessionId;
                _ = Task.Run(() => AnalyseAsync(clientSessionId, [.. take], format.SampleRate, format.ChannelCount));
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            _recording.TryRemove(args.StreamId, out _);
            _formats.TryRemove(args.StreamId, out _);
        };
    }

    private async Task AnalyseAsync(int clientSessionId, float[] samples, int sampleRate, int channelCount)
    {
        _analysing.SetFor(clientSessionId, true);

        try
        {
            // The take is kept, so a person can listen to it again and a later analysis can reread it
            var id = Guid.NewGuid().ToString("N");
            using var wav = new WavFile(sampleRate, channelCount, WavFile.SampleFormat.Float);
            wav.AddSamples(samples);
            await app.Files.Data.WriteBytesAsync($"takes/{id}.wav", wav.AsArray(), "audio/wav");

            var transcript = await SpeechRecognizer.RecognizeAsync(samples, sampleRate, channelCount: channelCount);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                _takes.AddFor(clientSessionId, new Take(id, "", "No speech in that take"));
                return;
            }

            // Duration and speaking rate are computed here, and the model gets them along with the words
            var seconds = samples.Length / (double)(sampleRate * channelCount);
            var wordsPerMinute = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / seconds * 60;

            var feedback = await Emerge.Run<string>(LLMModel.Claude55Sonnet, pass =>
            {
                pass.SystemPrompt = "You coach people on how they speak. Answer in two short sentences: one thing that worked, one thing to change.";
                pass.Command = $"Take of {seconds:F0} s at {wordsPerMinute:F0} words a minute. Transcript:\n{transcript}";
            });

            _takes.AddFor(clientSessionId, new Take(id, transcript, feedback));
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Analysis of a take for client {clientSessionId} failed, the user can record again: {ex.Message}");
            _takes.AddFor(clientSessionId, new Take(Guid.NewGuid().ToString("N"), "", "Couldn't analyse that take. Try again."));
        }
        finally
        {
            _analysing.SetFor(clientSessionId, false);
        }
    }

    private void Render(UIView view)
    {
        view.Column(["gap-4 max-w-xl"], content: col =>
        {
            // The processing a call needs would change the sound being analysed, so it is all off.
            // Turn echo cancellation back on if the app plays sound out loud while recording.
            col.MicToggleButton(text: "Record", audioOptions: new ClientAudioCaptureOptions
            {
                AutoGainControl = false,
                NoiseSuppression = false,
                EchoCancellation = false,
            });

            if (_analysing.Value)
            {
                col.Text([Text.Caption], "Listening to your take…");
            }

            foreach (var take in _takes)
            {
                col.Column([Card.Default, "gap-1 p-4"], key: take.Id, content: card =>
                {
                    card.Text([Text.Body], take.Feedback);
                    card.Text([Text.Caption], take.Transcript);
                });
            }
        });
    }
    #endregion
}
