return await App.Run(args);

public record SessionIdentity(string UserId);
public record ClientParams(string Name = "Ikon");

[App]
public class VoiceTutor(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new Theme());
    private Audio Audio { get; } = new(app);

    private readonly Reactive<bool> _isMicActive = new(false);
    private readonly Reactive<bool> _isThinking = new(false);
    private readonly Reactive<bool> _isSpeaking = new(false);
    private readonly Reactive<int> _selectedVoiceIndex = new(0);
    private readonly Reactive<string> _lastUserUtterance = new("");
    private readonly Reactive<string> _lastAssistantUtterance = new("");

    private readonly Reactive<SpeechRecognizerModel> _sttModel = new(SpeechRecognizerModel.WhisperLarge3Turbo);
    private readonly Reactive<string> _sttLanguage = new("en-US");

    private readonly Dictionary<string, TurnDetectionState> _turnStates = new();
    private readonly object _conversationLock = new();
    private readonly List<ConversationTurn> _conversation = new();
    private readonly object _speechLock = new();
    private SpeechGenerator? _speechGenerator;
    private CancellationTokenSource? _speechCts;

    private const float SpeechThreshold = 0.008f;
    private static readonly TimeSpan SpeculativeSilenceTimeout = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan FinalSilenceTimeout = TimeSpan.FromMilliseconds(600);
    private const int MaxConversationTurns = 6;

    internal record VoiceConfig(string Name, string VoiceId, SpeechGeneratorModel Model, string Provider);

    internal static readonly VoiceConfig[] AvailableVoices =
    [
        new("Aria", "9BWtsMINqrJLrRacOk9x", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("Sarah", "EXAVITQu4vr4xnSDxMaL", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("Laura", "FGY2WhTYpPnrIDTdsKH5", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("Charlie", "IKne3meq5aSn9XLyUdCD", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("George", "JBFqnCBsd6RMkjVDRZzb", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("Charlotte", "XB0fDUnXU5powFXDhCwa", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("Alice", "Xb7hH8MSUJpSbSDYk0k2", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("Matilda", "XrExE9yKIg1WjnnlVkGX", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("Jessica", "cgSgspJ2msm6clMCkdW9", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("Lily", "pFZP5JQG7iQjIQuC4Bku", SpeechGeneratorModel.Eleven3, "ElevenLabs"),
        new("Alloy", "alloy", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Echo", "echo", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Fable", "fable", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Onyx", "onyx", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Nova", "nova", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Shimmer", "shimmer", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Adam", "en-US-AdamMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
        new("Emma", "en-US-EmmaMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
        new("Brian", "en-US-BrianMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
        new("Ava", "en-US-AvaMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
        new("Andrew", "en-US-AndrewMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
    ];

    public async Task Main()
    {
        SetupAudioInputHandlers();

        UI.Root([Page.Default], content: view =>
        {
            view.Box(["min-h-screen bg-background text-foreground"], content: wrapper =>
            {
                wrapper.Column(["mx-auto w-full max-w-3xl px-4 py-8", Layout.Column.Lg], content: column =>
                {
                    column.Box([Card.Default, "p-6"], content: card =>
                    {
                        card.Text([Text.H1], "TUN35 audio coach");
                        card.Text([Text.Body, "mt-2 text-muted-foreground"], "Short audio chats with a friendly robot guide");
                    });

                    column.Box([Card.Default, "p-6"], content: card =>
                    {
                        card.Text([Text.H2], "Microphone");
                        card.Text([Text.Caption, "mt-1 text-muted-foreground"], "Toggle once to keep the mic active for long sessions");

                        card.Row([Layout.Row.Md, "mt-4 items-center flex-wrap gap-3"], content: row =>
                        {
                            var micLabel = _isMicActive.Value ? "Mic on" : "Mic off";

                            row.CaptureButton(
                                [Button.PrimaryMd],
                                kind: MediaCaptureKind.Audio,
                                label: micLabel,
                                captureMode: MediaCaptureButtonMode.Toggle,
                                onCaptureStart: OnAudioCaptureStart,
                                onCaptureStop: OnAudioCaptureStop);

                            var statusText = _isThinking.Value
                                ? "Thinking"
                                : _isSpeaking.Value
                                    ? "Talking"
                                    : _isMicActive.Value
                                        ? "Listening"
                                        : "Idle";
                            var statusStyle = _isSpeaking.Value
                                ? "bg-amber-100 text-amber-800"
                                : _isThinking.Value
                                    ? "bg-indigo-100 text-indigo-800"
                                    : _isMicActive.Value
                                        ? "bg-emerald-100 text-emerald-800"
                                        : "bg-slate-100 text-slate-700";

                            row.Box(["px-3 py-1 rounded-full text-xs font-semibold", statusStyle], content: badge =>
                            {
                                badge.Text(["tracking-wide"], statusText);
                            });
                        });
                    });

                    column.Box([Card.Default, "p-6"], content: card =>
                    {
                        card.Text([Text.H2], "Voice");
                        card.Text([Text.Caption, "mt-1 text-muted-foreground"], "Choose a calm robot voice");

                        card.Column([Layout.Column.Sm, "mt-4"], content: voiceColumn =>
                        {
                            var voicesByProvider = AvailableVoices
                                .Select((voice, index) => (voice, index))
                                .GroupBy(v => v.voice.Provider)
                                .ToList();

                            foreach (var providerGroup in voicesByProvider)
                            {
                                voiceColumn.Box(["rounded-xl border border-secondary p-4"], content: providerBox =>
                                {
                                    providerBox.Text([Text.Caption, "text-muted-foreground uppercase tracking-wide"], providerGroup.Key);

                                    providerBox.Row(["mt-3 flex-wrap gap-2"], content: voicesRow =>
                                    {
                                        foreach (var (voice, index) in providerGroup)
                                        {
                                            var voiceIndex = index;
                                            var isSelected = _selectedVoiceIndex.Value == voiceIndex;

                                            voicesRow.Button(
                                                [isSelected ? Button.PrimarySm : Button.OutlineSm],
                                                label: voice.Name,
                                                onClick: async () => _selectedVoiceIndex.Value = voiceIndex);
                                        }
                                    });
                                });
                            }
                        });
                    });

                    column.Box([Card.Default, "p-6"], content: card =>
                    {
                        card.Text([Text.H2], "Conversation" );
                        card.Text([Text.Caption, "mt-1 text-muted-foreground"], "TUN35 uses short sentences and gentle pacing");

                        card.Column([Layout.Column.Sm, "mt-4"], content: convo =>
                        {
                            if (!string.IsNullOrWhiteSpace(_lastUserUtterance.Value))
                            {
                                convo.Text([Text.Caption, "text-muted-foreground"], "You said");
                                convo.Text([Text.Body, "font-medium"], _lastUserUtterance.Value);
                            }

                            if (!string.IsNullOrWhiteSpace(_lastAssistantUtterance.Value))
                            {
                                convo.Text([Text.Caption, "mt-4 text-muted-foreground"], "TUN35 replied");
                                convo.Text([Text.Body, "font-medium"], _lastAssistantUtterance.Value);
                            }

                            if (string.IsNullOrWhiteSpace(_lastUserUtterance.Value) && string.IsNullOrWhiteSpace(_lastAssistantUtterance.Value))
                            {
                                convo.Text([Text.Body, "text-muted-foreground"], "Say hello to begin");
                            }
                        });
                    });
                });
            });
        });
    }

    private void SetupAudioInputHandlers()
    {
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            var state = new TurnDetectionState(args.SampleRate, args.ChannelCount, _sttModel.Value, _sttLanguage.Value);
            _turnStates[args.StreamId.ToString()] = state;
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (_isSpeaking.Value || _isThinking.Value)
            {
                return;
            }

            if (!_turnStates.TryGetValue(args.StreamId.ToString(), out var state))
            {
                return;
            }

            var rms = CalculateRms(args.Samples);
            var now = DateTime.UtcNow;

            if (rms >= SpeechThreshold)
            {
                if (state.IsSpeculating)
                {
                    state.CancelSpeculation();
                }

                state.LastSpeechAt = now;
                state.HasSpeech = true;
            }

            if (state.HasSpeech)
            {
                state.AppendSamples(args.Samples);
            }

            if (state.ShouldStartSpeculation(now, SpeculativeSilenceTimeout))
            {
                _ = StartSpeculativeProcessingAsync(state);
            }

            if (state.ShouldFinalize(now, FinalSilenceTimeout))
            {
                _ = FinalizeAndSpeakAsync(state);
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            if (_turnStates.TryGetValue(args.StreamId.ToString(), out var state))
            {
                state.CancelSpeculation();
                state.Reset();
                _turnStates.Remove(args.StreamId.ToString());
            }
        };
    }

    private async Task StartSpeculativeProcessingAsync(TurnDetectionState state)
    {
        float[] samples;
        CancellationToken ct;

        if (!state.TryStartSpeculation(out samples, out ct))
        {
            return;
        }

        try
        {
            var text = await RecognizeSpeechAsync(samples, state.SampleRate, state.ChannelCount, ct);

            if (ct.IsCancellationRequested || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var response = await GenerateReplyAsync(text.Trim(), ct);

            if (ct.IsCancellationRequested || string.IsNullOrWhiteSpace(response))
            {
                return;
            }

            state.SetSpeculativeResult(text.Trim(), response);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Speculative processing error: {ex.Message}");
        }
    }

    private async Task FinalizeAndSpeakAsync(TurnDetectionState state)
    {
        if (!state.TryFinalize(out var userText, out var response))
        {
            return;
        }

        _isThinking.Value = true;

        try
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                float[] samples;

                if (!state.TryConsumeFinalSamples(out samples))
                {
                    return;
                }

                userText = await RecognizeSpeechAsync(samples, state.SampleRate, state.ChannelCount, CancellationToken.None);

                if (string.IsNullOrWhiteSpace(userText))
                {
                    return;
                }

                response = await GenerateReplyAsync(userText.Trim(), CancellationToken.None);
            }

            if (string.IsNullOrWhiteSpace(userText) || string.IsNullOrWhiteSpace(response))
            {
                return;
            }

            _lastUserUtterance.Value = userText;
            AddConversationTurn(ConversationRole.User, userText);

            _lastAssistantUtterance.Value = response;
            AddConversationTurn(ConversationRole.Assistant, response);

            await SpeakAsync(response);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Finalize error: {ex.Message}");
        }
        finally
        {
            _isThinking.Value = false;
            state.MarkProcessed();
        }
    }

    private async Task OnAudioCaptureStart(MediaCaptureEvent e)
    {
        _isMicActive.Value = true;
    }

    private async Task OnAudioCaptureStop(MediaCaptureEvent e)
    {
        _isMicActive.Value = false;
        InterruptSpeaking();
    }

    private async Task<string> RecognizeSpeechAsync(float[] samples, int sampleRate, int channelCount, CancellationToken ct)
    {
        var rms = CalculateRms(samples);

        if (rms < SpeechThreshold)
        {
            return string.Empty;
        }

        try
        {
            using var recognizer = new SpeechRecognizer(_sttModel.Value);
            var config = new RecognizeSpeechConfig
            {
                Samples = samples,
                SampleRate = sampleRate,
                ChannelCount = channelCount,
                Language = _sttLanguage.Value
            };

            return await recognizer.RecognizeBatchSpeechAsync(config);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Speech recognition error: {ex.Message}");
            return string.Empty;
        }
    }

    private async Task<string> GenerateReplyAsync(string userText, CancellationToken ct)
    {
        string contextSummary;

        lock (_conversationLock)
        {
            var tempConversation = _conversation.ToList();
            tempConversation.Add(new ConversationTurn(ConversationRole.User, userText));

            if (tempConversation.Count == 0)
            {
                contextSummary = "Start a friendly audio chat";
            }
            else
            {
                var lines = tempConversation.Select(turn =>
                    turn.Role == ConversationRole.User
                        ? $"Child: {turn.Text}"
                        : $"TUN35: {turn.Text}");
                contextSummary = string.Join("\n", lines);
            }
        }

        try
        {
            var ctx = new KernelContext();
            var responseText = new System.Text.StringBuilder();

            await foreach (var ev in Emerge.Run<VoiceTutorReply>(LLMModel.Claude45Sonnet, ctx, pass =>
            {
                pass.Command = contextSummary;
                pass.SystemPrompt = GetSystemPrompt();
                pass.MaxOutputTokens = 220;
            }).WithCancellation(ct))
            {
                switch (ev)
                {
                    case ModelText<VoiceTutorReply> text:
                        responseText.Append(text.Text);
                        break;
                    case Completed<VoiceTutorReply> completed:
                        responseText.Clear();
                        responseText.Append(completed.Result.Response);
                        break;
                }
            }

            return responseText.ToString().Trim();
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"LLM response error: {ex.Message}");
            return string.Empty;
        }
    }

    private async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        CancellationToken cancellationToken;
        SpeechGenerator generator;
        var voice = GetSelectedVoice();

        lock (_speechLock)
        {
            _speechCts?.Cancel();
            _speechCts?.Dispose();
            _speechCts = new CancellationTokenSource();
            cancellationToken = _speechCts.Token;

            _speechGenerator = new SpeechGenerator(voice.Model);
            generator = _speechGenerator;
            _isSpeaking.Value = true;
        }

        try
        {
            var config = new SpeechGeneratorConfig
            {
                Text = text,
                VoiceId = voice.VoiceId,
                Language = _sttLanguage.Value
            };

            await foreach (var audio in generator.GenerateSpeechAsync(config).WithCancellation(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                Audio.SendSpeech(audio);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Speech generation error: {ex.Message}");
        }
        finally
        {
            lock (_speechLock)
            {
                _isSpeaking.Value = false;
            }
        }
    }

    private void InterruptSpeaking()
    {
        Audio.SpeechMixer.FadeOut();
        StopSpeaking();
    }

    private void StopSpeaking()
    {
        lock (_speechLock)
        {
            _speechCts?.Cancel();
            _speechCts?.Dispose();
            _speechCts = null;
            _speechGenerator = null;
            _isSpeaking.Value = false;
        }
    }

    private VoiceConfig GetSelectedVoice()
    {
        var index = _selectedVoiceIndex.Value;

        if (index < 0 || index >= AvailableVoices.Length)
        {
            return AvailableVoices[0];
        }

        return AvailableVoices[index];
    }

    private void AddConversationTurn(ConversationRole role, string text)
    {
        lock (_conversationLock)
        {
            _conversation.Add(new ConversationTurn(role, text));

            if (_conversation.Count > MaxConversationTurns)
            {
                _conversation.RemoveAt(0);
            }
        }
    }

    private static string GetSystemPrompt()
    {
        return """
            You are TUN35, a small, gentle robot who teaches and supports ADHD kids
            Speak in short sentences with calm, friendly language
            Use simple words and one idea at a time
            Ask short, open questions when helpful
            Be warm, curious, and never judgmental
            Offer choices instead of commands
            Avoid long explanations and avoid lists longer than three items
            Keep responses brief and easy to follow
            """;
    }

    private static float CalculateRms(ReadOnlySpan<float> samples)
    {
        if (samples.IsEmpty)
        {
            return 0f;
        }

        double sumSquares = 0;

        foreach (var sample in samples)
        {
            sumSquares += sample * sample;
        }

        return (float)Math.Sqrt(sumSquares / samples.Length);
    }

    private sealed class TurnDetectionState
    {
        public int SampleRate { get; }
        public int ChannelCount { get; }
        public DateTime LastSpeechAt { get; set; }
        public bool HasSpeech { get; set; }
        public bool IsSpeculating => _speculationCts != null;

        private readonly SpeechRecognizerModel _sttModel;
        private readonly string _sttLanguage;
        private readonly object _lock = new();
        private readonly List<float> _samples = new();
        private readonly List<float> _speculativeSamples = new();
        private bool _isProcessing;
        private bool _speculationStarted;
        private CancellationTokenSource? _speculationCts;
        private string? _speculativeUserText;
        private string? _speculativeResponse;

        public TurnDetectionState(int sampleRate, int channelCount, SpeechRecognizerModel sttModel, string sttLanguage)
        {
            SampleRate = sampleRate;
            ChannelCount = channelCount;
            _sttModel = sttModel;
            _sttLanguage = sttLanguage;
        }

        public void AppendSamples(ReadOnlySpan<float> samples)
        {
            lock (_lock)
            {
                var arr = samples.ToArray();
                _samples.AddRange(arr);

                if (_speculationStarted && _speculationCts != null)
                {
                    _speculativeSamples.AddRange(arr);
                }
            }
        }

        public bool ShouldStartSpeculation(DateTime now, TimeSpan timeout)
        {
            if (!HasSpeech || _speculationStarted || _isProcessing)
            {
                return false;
            }

            return now - LastSpeechAt > timeout;
        }

        public bool ShouldFinalize(DateTime now, TimeSpan timeout)
        {
            if (!HasSpeech || _isProcessing)
            {
                return false;
            }

            return now - LastSpeechAt > timeout;
        }

        public bool TryStartSpeculation(out float[] samples, out CancellationToken ct)
        {
            lock (_lock)
            {
                if (_speculationStarted || _samples.Count == 0)
                {
                    samples = Array.Empty<float>();
                    ct = CancellationToken.None;
                    return false;
                }

                _speculationStarted = true;
                _speculationCts = new CancellationTokenSource();
                _speculativeSamples.Clear();
                samples = _samples.ToArray();
                ct = _speculationCts.Token;
                return true;
            }
        }

        public void CancelSpeculation()
        {
            lock (_lock)
            {
                _speculationCts?.Cancel();
                _speculationCts?.Dispose();
                _speculationCts = null;
                _speculationStarted = false;
                _speculativeUserText = null;
                _speculativeResponse = null;
                _speculativeSamples.Clear();
            }
        }

        public void SetSpeculativeResult(string userText, string response)
        {
            lock (_lock)
            {
                if (_speculationCts == null || _speculationCts.IsCancellationRequested)
                {
                    return;
                }

                _speculativeUserText = userText;
                _speculativeResponse = response;
            }
        }

        public bool TryFinalize(out string? userText, out string? response)
        {
            lock (_lock)
            {
                if (_isProcessing)
                {
                    userText = null;
                    response = null;
                    return false;
                }

                _isProcessing = true;

                if (_speculativeSamples.Count == 0 && _speculativeResponse != null)
                {
                    userText = _speculativeUserText;
                    response = _speculativeResponse;
                    HasSpeech = false;
                    _samples.Clear();
                    return true;
                }

                userText = null;
                response = null;
                return true;
            }
        }

        public bool TryConsumeFinalSamples(out float[] samples)
        {
            lock (_lock)
            {
                if (_samples.Count == 0)
                {
                    samples = Array.Empty<float>();
                    return false;
                }

                samples = _samples.ToArray();
                _samples.Clear();
                HasSpeech = false;
                return true;
            }
        }

        public void MarkProcessed()
        {
            lock (_lock)
            {
                _isProcessing = false;
                _speculationStarted = false;
                _speculationCts?.Dispose();
                _speculationCts = null;
                _speculativeUserText = null;
                _speculativeResponse = null;
                _speculativeSamples.Clear();
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _samples.Clear();
                _speculativeSamples.Clear();
                _isProcessing = false;
                _speculationStarted = false;
                _speculationCts?.Cancel();
                _speculationCts?.Dispose();
                _speculationCts = null;
                _speculativeUserText = null;
                _speculativeResponse = null;
                HasSpeech = false;
            }
        }
    }

    private enum ConversationRole
    {
        User,
        Assistant
    }

    private record ConversationTurn(ConversationRole Role, string Text);

    private sealed class VoiceTutorReply
    {
        public string Response { get; set; } = string.Empty;
    }
}
