namespace Ikon.App.Patterns.Patterns;

// Pattern: sample-stills-to-vision — see docs/patterns/sample-stills-to-vision.md.
// The demo draws the record button; recording needs a running app's microphone and camera.
internal sealed class SampleStillsToVision(IAppBase app) : IPatternDemo
{
    public string Slug => "sample-stills-to-vision";
    public string Title => "Recording with camera stills for a vision model";
    public string Category => "Voice & audio";

    private Audio Audio { get; } = new(app);

    public void RenderDemo(IView view) => Render((UIView)view);

    #region example:pattern-sample-stills-to-vision
    private sealed record Feedback(string Speech, string Presence);

    private sealed record Take(string Id, string Transcript, Feedback Feedback);

    private readonly ConcurrentDictionary<string, List<float>> _recording = new();
    private readonly ConcurrentDictionary<string, (int SampleRate, int ChannelCount)> _formats = new();

    // Per microphone stream: what stops its still loop, and the stills taken so far
    private readonly ConcurrentDictionary<string, (CancellationTokenSource Stop, List<byte[]> Stills)> _sampling = new();

    private readonly ClientReactiveList<Take> _takes = new();
    private readonly ClientReactive<bool> _analysing = new(false);

    private static readonly TimeSpan MaxTake = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan StillInterval = TimeSpan.FromSeconds(5);

    // Every still is billed as input tokens, so a long take sends an evenly spaced few
    private const int MaxStills = 8;

    /// <summary>Call once from Main.</summary>
    private void WireRecording()
    {
        Audio.AudioInputStreamBeginAsync += async args => _formats[args.StreamId] = (args.SampleRate, args.ChannelCount);

        Audio.AudioInputFrameAsync += async args =>
        {
            if (args.IsFirst)
            {
                _recording[args.StreamId] = [];
                StartSampling(args.StreamId, args.ClientSessionId);
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
                var stills = StopSampling(args.StreamId);
                var clientSessionId = args.ClientSessionId;
                _ = Task.Run(() => AnalyseAsync(clientSessionId, [.. take], format.SampleRate, format.ChannelCount, stills));
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            _recording.TryRemove(args.StreamId, out _);
            _formats.TryRemove(args.StreamId, out _);
            StopSampling(args.StreamId);
        };
    }

    // The app cannot decode the camera's video, so it asks the client for a still every few
    // seconds while the microphone is open
    private void StartSampling(string streamId, int clientSessionId)
    {
        var stop = new CancellationTokenSource(MaxTake);
        var stills = new List<byte[]>();
        _sampling[streamId] = (stop, stills);

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(StillInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(stop.Token))
                {
                    var still = await ClientFunctions.CaptureImageAsync(
                        new ClientImageCaptureOptions { Width = 640, Format = ClientImageCaptureFormat.Jpeg, Quality = 0.7 },
                        targetId: clientSessionId, cancellationToken: stop.Token);

                    lock (stills)
                    {
                        stills.Add(still.Data);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The take ended or reached MaxTake; the stills taken so far go to the model
            }
            catch (Exception ex) when (ex is NotSupportedException or FunctionCallException)
            {
                Log.Instance.Warning($"Camera stills stopped for client {clientSessionId}, the take is analysed from its words alone: {ex.Message}");
            }
        });
    }

    private List<byte[]> StopSampling(string streamId)
    {
        if (!_sampling.TryRemove(streamId, out var sampling))
        {
            return [];
        }

        sampling.Stop.Cancel();

        lock (sampling.Stills)
        {
            return [.. sampling.Stills];
        }
    }

    private async Task AnalyseAsync(int clientSessionId, float[] samples, int sampleRate, int channelCount, List<byte[]> stills)
    {
        _analysing.SetFor(clientSessionId, true);

        try
        {
            var transcript = await SpeechRecognizer.RecognizeAsync(samples, sampleRate, channelCount: channelCount);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                _takes.AddFor(clientSessionId, new Take(Guid.NewGuid().ToString("N"), "", new Feedback("No speech in that take", "")));
                return;
            }

            var step = Math.Max(1, (int)Math.Ceiling(stills.Count / (double)MaxStills));
            var sent = stills.Where((_, index) => index % step == 0).Take(MaxStills).ToList();

            // The words and the pictures go in one user message, so the model reads them together
            List<IMessagePart> parts =
            [
                new TextPart($"Transcript:\n{transcript}\n\n{sent.Count} camera stills from the take follow, in order."),
                .. sent.Select(still => new ImagePart(still, "image/jpeg")),
            ];

            var context = new KernelContext().Add(new MessageBlock(MessageBlockRole.User, parts));
            var feedback = await Emerge.Run<Feedback>(LLMModel.Claude55Sonnet, context, pass =>
            {
                pass.SystemPrompt = "You coach people who practise speaking. Speech: one sentence on what they said and how. "
                    + "Presence: one sentence on posture and eye contact in the stills, or say that the stills show nobody.";
            });

            _takes.AddFor(clientSessionId, new Take(Guid.NewGuid().ToString("N"), transcript, feedback));
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Analysis of a take with {stills.Count} stills for client {clientSessionId} failed, the user can record again: {ex.Message}");
            _takes.AddFor(clientSessionId, new Take(Guid.NewGuid().ToString("N"), "", new Feedback("Couldn't analyse that take. Try again.", "")));
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
            col.MicToggleButton(text: "Record", audioOptions: new ClientAudioCaptureOptions
            {
                AutoGainControl = false,
                NoiseSuppression = false,
            });

            col.Text([Text.Caption], "While you speak, the camera takes a still every few seconds for the coach.");

            if (_analysing.Value)
            {
                col.Text([Text.Caption], "Watching your take…");
            }

            foreach (var take in _takes)
            {
                col.Column([Card.Default, "gap-1 p-4"], key: take.Id, content: card =>
                {
                    card.Text([Text.Body], take.Feedback.Speech);
                    if (take.Feedback.Presence.Length > 0)
                    {
                        card.Text([Text.Body], take.Feedback.Presence);
                    }

                    card.Text([Text.Caption], take.Transcript);
                });
            }
        });
    }
    #endregion
}
