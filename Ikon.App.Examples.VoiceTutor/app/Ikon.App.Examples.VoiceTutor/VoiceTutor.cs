return await App.Run(args);

public record SessionIdentity(string? UserId);
public record ClientParams(string Name = "Ikon");

[App]
public class VoiceTutor(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new IkonTheme());
    private Audio Audio { get; } = new(app);

    private readonly Reactive<bool> _isMicActive = new(false);
    private readonly Reactive<bool> _isThinking = new(false);
    private readonly Reactive<bool> _isSpeaking = new(false);
    private readonly Reactive<int> _selectedVoiceIndex = new(0);
    private readonly Reactive<string> _lastUserUtterance = new("");
    private readonly Reactive<string> _lastAssistantUtterance = new("");

    private readonly Reactive<SpeechRecognizerModel> _sttModel = new(SpeechRecognizerModel.WhisperLarge3Turbo);
    private readonly Reactive<string> _sttLanguage = new("en-US");

    private readonly object _conversationLock = new();
    private readonly List<ConversationTurn> _conversation = new();
    private readonly object _speechLock = new();
    private SpeechGenerator? _speechGenerator;
    private CancellationTokenSource? _speechCts;
    private (int TurnId, Task<string> Reply)? _speculativeReply;

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
        Audio.UseTurnDetection(_sttModel.Value, language: _sttLanguage.Value);

        Audio.TurnSpeculativeAsync += async args =>
        {
            _speculativeReply = (args.TurnId, GenerateReplyAsync(args.Text, args.CancellationToken));
        };

        Audio.SpeechRecognizedAsync += OnSpeechRecognized;

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

                            // The status pill below still reads _isMicActive, but the button's own
                            // colour must not wait for that round trip — MicButton.States flips it
                            // from the client-stamped capture state.
                            row.CaptureButton(
                                [Button.PrimaryMd, MicButton.States],
                                kind: MediaCaptureKind.Audio,
                                text: micLabel,
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
                                                text: voice.Name,
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

    private async Task OnSpeechRecognized(SpeechRecognizedEventArgs args)
    {
        if (_isThinking.Value || _isSpeaking.Value)
        {
            return;
        }

        _isThinking.Value = true;

        try
        {
            var speculative = _speculativeReply;
            _speculativeReply = null;

            var response = speculative is { } pending && pending.TurnId == args.TurnId
                ? await pending.Reply
                : await GenerateReplyAsync(args.Text, CancellationToken.None);

            if (string.IsNullOrWhiteSpace(response))
            {
                return;
            }

            _lastUserUtterance.Value = args.Text;
            AddConversationTurn(ConversationRole.User, args.Text);

            _lastAssistantUtterance.Value = response;
            AddConversationTurn(ConversationRole.Assistant, response);

            await SpeakAsync(response);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Turn handling error: {ex.Message}");
        }
        finally
        {
            _isThinking.Value = false;
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
                    case Completed<VoiceTutorReply> { Result: { } result }:
                        responseText.Clear();
                        responseText.Append(result.Response);
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

                Audio.SpeakChunk(MediaTargets.Everyone, audio);
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
