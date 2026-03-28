using Ikon.AI.LLM;
using Ikon.App.Examples.VRMChat.VRM;
using Ikon.Parallax.Components.Standard;

return await App.Run(args);

public record SessionIdentity(string UserId);

public record ClientParams;

public record VRMModelConfig(string Name, string Path);

public class ChatReply
{
    public string Message { get; set; } = "";
    public string Motion { get; set; } = "idle";
    public string Expression { get; set; } = "neutral";
}

public class IdleAction
{
    public string Motion { get; set; } = "idle";
    public string Expression { get; set; } = "neutral";
}

[App]
public class VRMChat(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new Theme());
    private Audio Audio { get; set; } = new(app);

    private readonly VisemeAnalyzer _visemeAnalyzer = new();

    // VRM state
    private readonly Reactive<bool> _isListening = new(false);
    private readonly Reactive<string> _currentExpression = new("");
    private readonly Reactive<string> _currentMotion = new("");
    private readonly Reactive<int> _selectedModelIndex = new(0);
    private readonly Reactive<int> _viewModeIndex = new(1);
    private readonly Reactive<bool> _settingsPanelOpen = new(false);

    // Chat messages history
    private readonly object _chatMessagesLock = new();
    private readonly List<ChatMessage> _chatMessages = [];
    private readonly Reactive<int> _chatMessagesVersion = new(0);
    private readonly Reactive<bool> _isProcessingMessage = new(false);

    // Message queue for sequential processing
    private readonly System.Threading.Channels.Channel<string> _messageQueue =
        System.Threading.Channels.Channel.CreateUnbounded<string>();
    private readonly CancellationTokenSource _appCts = new();
    private bool _messageProcessorStarted;

    // Emergence chat context
    private KernelContext _chatContext = new();

    // Idle behavior tracking
    private DateTime _lastInteractionTime = DateTime.UtcNow;
    private const int IdleCheckIntervalSeconds = 5;
    private const int IdleThresholdSeconds = 15;

    private enum ChatRole { User, Assistant }
    private sealed record ChatMessage(ChatRole Role, Reactive<string> Content);

    private static readonly VRMModelConfig[] AvailableModels =
    [
        new("Avatar A", "/models/AvatarSample_A.vrm"),
        new("Avatar B", "/models/AvatarSample_B.vrm")
    ];

    private static readonly (string Name, string ViewMode)[] ViewModes =
    [
        ("Full Body", "fullBody"),
        ("Portrait", "portrait"),
        ("Face", "face")
    ];

    // Text-to-Speech state
    private readonly Reactive<string> _ttsText = new("");
    private readonly Reactive<SpeechGeneratorModel> _ttsModel = new(SpeechGeneratorModel.Eleven3);
    private readonly Reactive<string> _ttsVoice = new("");
    private readonly Reactive<bool> _ttsSpeaking = new(false);
    private readonly Reactive<bool> _ttsPaused = new(false);
    private readonly List<EffectEntry> _ttsEffects = [];
    private readonly object _ttsEffectsLock = new();
    private readonly Reactive<int> _ttsEffectsCount = new(0);
    private SpeechGenerator? _speechGenerator;
    private CancellationTokenSource? _speechCts;
    private readonly object _speechLock = new();
    private int _ttsModelIndex;
    private int _ttsVoiceIndex;
    private static readonly IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> VoicesByModel = SpeechGenerator.GetVoiceIdsByModel();

    // Speech-to-Text state
    private readonly Reactive<SpeechRecognizerModel> _sttModel = new(SpeechRecognizerModel.WhisperLarge3Turbo);
    private readonly Reactive<string> _sttLanguage = new("en-US");
    private readonly Reactive<bool> _sttContinuousMode = new(false);
    private readonly Reactive<bool> _sttPlaybackEnabled = new(false);
    private readonly Reactive<string> _sttRecognizedText = new("");
    private readonly Reactive<bool> _sttIsHoldRecording = new(false);
    private readonly Reactive<bool> _sttIsToggleRecording = new(false);
    private readonly List<EffectEntry> _sttEffects = [];
    private readonly Reactive<int> _sttEffectsCount = new(0);
    private readonly Dictionary<string, SttStreamState> _sttStreamStates = new();
    private int _sttModelIndex;
    private static readonly SpeechRecognizerModel[] SttModels = Enum.GetValues<SpeechRecognizerModel>();

    private static readonly SpeechGeneratorModel[] TtsModels = Enum.GetValues<SpeechGeneratorModel>();

    private static readonly string[] EffectTypes =
    [
        "Delay",
        "Reverb",
        "Chorus",
        "Tremolo",
        "BitCrusher",
        "Saturation",
        "RobotVoice",
        "Telephone"
    ];

    public async Task Main()
    {
        app.StoppingAsync += async _ =>
        {
            await _appCts.CancelAsync();
            _messageQueue.Writer.TryComplete();
            StopSpeaking();
            _speechCts?.Dispose();
            _appCts.Dispose();
        };

        Audio.AudioInputStreamBeginAsync += async args =>
        {
            _isListening.Value = true;
            _currentMotion.Value = "listening";
            _lastInteractionTime = DateTime.UtcNow;
            var state = new SttStreamState(args.SampleRate, args.ChannelCount);
            _sttStreamStates[args.StreamId.ToString()] = state;
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (!_sttStreamStates.TryGetValue(args.StreamId.ToString(), out var state))
            {
                return;
            }

            if (args.IsFirst)
            {
                state.Reset();
                state.RecordingWavFile = new WavFile(state.SampleRate, state.ChannelCount, WavFile.SampleFormat.Float);

                if (_sttContinuousMode.Value)
                {
                    RunContinuousRecognitionAsync(state).RunParallel();
                }
            }

            state.AddSamples(args.Samples);
            state.RecordingWavFile?.AddSamples(args.Samples);

            if (_sttPlaybackEnabled.Value)
            {
                var processedSamples = args.Samples.ToArray();
                var effectInstances = GetSttEffectInstances(state);

                foreach (var effect in effectInstances)
                {
                    effect.Process(processedSamples);
                }

                await Audio.SendAsync(processedSamples, state.SampleRate, state.ChannelCount, args.IsFirst, args.IsLast, args.StreamId.ToString());
            }

            if (args.IsLast)
            {
                if (state.RecordingWavFile != null)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "recording.wav");
                    state.RecordingWavFile.SaveToFile(filePath);
                    state.RecordingWavFile.Dispose();
                    state.RecordingWavFile = null;
                }

                state.Complete();

                if (!_sttContinuousMode.Value)
                {
                    RunBatchRecognitionAsync(state).RunParallel();
                }
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            _isListening.Value = false;
            _currentMotion.Value = "idle";
            _lastInteractionTime = DateTime.UtcNow;

            if (_sttStreamStates.TryGetValue(args.StreamId.ToString(), out var state))
            {
                state.RecordingWavFile?.Dispose();
                state.Complete();
                _sttStreamStates.Remove(args.StreamId.ToString());
            }
        };

        _currentMotion.Value = "idle";
        _currentExpression.Value = "relaxed";
        RunIdleLoopAsync().RunParallel();

        UI.Root(style: ["font-sans h-screen w-screen bg-white text-gray-900 overflow-hidden relative"], content: view =>
        {
            var currentModel = AvailableModels[_selectedModelIndex.Value];
            var currentView = ViewModes[_viewModeIndex.Value];

            view.VRMCanvas(
                source: currentModel.Path,
                isListening: _isListening.Value,
                expression: _currentExpression.Value,
                motion: _currentMotion.Value,
                viewMode: currentView.ViewMode,
                style: ["absolute inset-0 w-full h-full"]
            );

            // Top bar with controls
            view.Row(style: ["absolute top-0 left-0 right-0 p-5 flex justify-end items-center z-10"], content: view =>
            {
                view.Button(
                    style: [_settingsPanelOpen.Value
                        ? "w-12 h-12 rounded-2xl bg-blue-500 text-white text-xl flex items-center justify-center shadow-lg"
                        : "w-12 h-12 rounded-2xl bg-white/80 backdrop-blur-xl text-gray-600 text-xl flex items-center justify-center shadow-lg border border-gray-200 hover:bg-gray-100 transition-all"],
                    label: "⚙",
                    onClick: async () => { _settingsPanelOpen.Value = !_settingsPanelOpen.Value; });
            });

            // Settings panel
            if (_settingsPanelOpen.Value)
            {
                view.Column(style: ["absolute top-20 right-5 w-72 max-h-[calc(100vh-10rem)] overflow-y-auto bg-white/95 backdrop-blur-2xl rounded-3xl shadow-2xl border border-gray-200 z-20"], content: view =>
                {
                    // Model selector
                    view.Column(style: ["p-5"], content: view =>
                    {
                        view.Text(style: ["text-sm font-semibold text-gray-700 mb-3 uppercase tracking-wide"], text: "Character");
                        view.Row(style: ["flex flex-wrap gap-2"], content: view =>
                        {
                            foreach (var (index, model) in AvailableModels.Select((m, i) => (i, m)))
                            {
                                var isSelected = _selectedModelIndex.Value == index;
                                var idx = index;
                                view.Button(
                                    style: [isSelected
                                        ? "text-sm px-4 py-2 rounded-xl font-semibold bg-blue-500 text-white shadow-md"
                                        : "text-sm px-4 py-2 rounded-xl font-medium bg-gray-100 hover:bg-gray-200 text-gray-700 border border-gray-300 transition-all"],
                                    label: model.Name,
                                    onClick: async () => { _selectedModelIndex.Value = idx; });
                            }
                        });
                    });

                    // View mode selector
                    view.Column(style: ["px-5 pb-5"], content: view =>
                    {
                        view.Text(style: ["text-sm font-semibold text-gray-700 mb-3 uppercase tracking-wide"], text: "View");
                        view.Row(style: ["flex flex-wrap gap-2"], content: view =>
                        {
                            foreach (var (index, mode) in ViewModes.Select((m, i) => (i, m)))
                            {
                                var isSelected = _viewModeIndex.Value == index;
                                var idx = index;
                                view.Button(
                                    style: [isSelected
                                        ? "text-sm px-4 py-2 rounded-xl font-semibold bg-blue-500 text-white shadow-md"
                                        : "text-sm px-4 py-2 rounded-xl font-medium bg-gray-100 hover:bg-gray-200 text-gray-700 border border-gray-300 transition-all"],
                                    label: mode.Name,
                                    onClick: async () => { _viewModeIndex.Value = idx; });
                            }
                        });
                    });

                    RenderTtsSettings(view);
                    RenderAudioEffectsSettings(view);
                    RenderSttSettings(view);
                });
            }

            // Chat messages overlay
            var _ = _chatMessagesVersion.Value;
            List<ChatMessage> recentMessages;
            lock (_chatMessagesLock)
            {
                recentMessages = _chatMessages.TakeLast(4).ToList();
            }
            view.Column(style: ["absolute bottom-32 right-8 top-20 w-[340px] z-10 flex flex-col justify-end gap-3 pointer-events-none"], content: view =>
            {
                foreach (var message in recentMessages)
                {
                    var messageText = message.Content.Value;
                    if (string.IsNullOrEmpty(messageText)) continue;

                    if (message.Role == ChatRole.User)
                    {
                        view.Row(style: ["w-full justify-end"], content: view =>
                        {
                            view.Column(style: [
                                "max-w-[90%] px-5 py-3 rounded-3xl bg-blue-500 shadow-lg",
                                "motion-[0:opacity-0 translate-y-[12px] blur-[4px],100:opacity-100 translate-y-0 blur-0] motion-duration-400ms motion-fill-both motion-ease-[cubic-bezier(0.25,1,0.35,1)]"
                            ], content: view =>
                            {
                                view.Text(
                                    style: [
                                        "text-white text-lg font-medium",
                                        "letter:motion-[0:opacity-0 blur-[3px],100:opacity-100 blur-0] letter:motion-duration-200ms letter:motion-stagger-50ms letter:motion-per-letter letter:motion-fill-both"
                                    ],
                                    text: messageText);
                            });
                        });
                    }
                    else
                    {
                        view.Row(style: ["w-full justify-end"], content: view =>
                        {
                            view.Column(style: [
                                "max-w-[90%] px-5 py-3 rounded-3xl bg-gray-100 shadow-lg border border-gray-200",
                                "motion-[0:opacity-0 translate-y-[12px] blur-[4px],100:opacity-100 translate-y-0 blur-0] motion-duration-400ms motion-fill-both motion-ease-[cubic-bezier(0.25,1,0.35,1)]"
                            ], content: view =>
                            {
                                view.Text(
                                    style: [
                                        "text-gray-800 text-lg font-medium",
                                        "letter:motion-[0:opacity-0 blur-[3px],100:opacity-100 blur-0] letter:motion-duration-200ms letter:motion-stagger-50ms letter:motion-per-letter letter:motion-fill-both"
                                    ],
                                    text: messageText);
                            });
                        });
                    }
                }
            });

            // Bottom input area
            view.Column(style: ["absolute bottom-0 left-0 right-0 p-5 z-10"], content: view =>
            {
                view.Row(style: ["flex gap-3 items-center bg-white/90 backdrop-blur-2xl rounded-3xl px-6 py-4 shadow-2xl border border-gray-200"], content: view =>
                {
                    view.TextField(
                        style: ["flex-1 bg-transparent border-0 border-none outline-none focus:outline-none focus:ring-0 focus:border-0 focus:border-transparent shadow-none text-gray-800 text-xl placeholder-gray-400 font-medium [&]:border-0 [&]:outline-0"],
                        value: _ttsText.Value,
                        placeholder: "Type a message...",
                        props: new Dictionary<string, object?>
                        {
                            ["spellCheck"] = false,
                            ["autoComplete"] = "off",
                            ["autoCorrect"] = "off",
                            ["autoCapitalize"] = "off",
                            ["data-form-type"] = "other"
                        },
                        onValueChange: value =>
                        {
                            _ttsText.Value = value;
                            return Task.CompletedTask;
                        },
                        onSubmit: async _ =>
                        {
                            var text = _ttsText.Value;
                            _ttsText.Value = "";
                            QueueUserMessage(text);
                        });

                    view.Button(
                        style: [_isProcessingMessage.Value
                            ? "w-14 h-14 rounded-full bg-gray-300 text-gray-500 text-2xl flex items-center justify-center shadow-lg cursor-not-allowed"
                            : "w-14 h-14 rounded-full bg-blue-500 hover:bg-blue-600 hover:scale-105 text-white text-2xl flex items-center justify-center shadow-lg transition-all duration-200"],
                        label: _isProcessingMessage.Value ? "⏳" : "➤",
                        onClick: async () =>
                        {
                            var text = _ttsText.Value;
                            _ttsText.Value = "";
                            QueueUserMessage(text);
                        });

                    view.CaptureButton(
                        style: [_sttIsToggleRecording.Value
                            ? "w-14 h-14 rounded-full bg-red-500 text-white text-2xl flex items-center justify-center shadow-lg animate-pulse"
                            : "w-14 h-14 rounded-full bg-gray-100 hover:bg-gray-200 hover:scale-105 text-gray-600 text-2xl flex items-center justify-center shadow-lg border border-gray-300 transition-all duration-200"],
                        kind: MediaCaptureKind.Audio,
                        label: "🎤",
                        captureMode: MediaCaptureButtonMode.Hold,
                        onCaptureStart: async _ =>
                        {
                            Audio.SpeechMixer.FadeOut();
                            StopSpeaking();
                            _sttIsToggleRecording.Value = true;
                            _sttRecognizedText.Value = "";
                        },
                        onCaptureStop: async _ =>
                        {
                            _sttIsToggleRecording.Value = false;
                        });
                });
            });
        });
    }

    private void RenderTtsSettings(UIView view)
    {
        view.Column(style: ["px-5 pb-4"], content: view =>
        {
            view.Text(style: ["text-sm font-semibold text-gray-700 mb-3 uppercase tracking-wide"], text: "Voice");

            view.Column(style: ["flex flex-col gap-2"], content: view =>
            {
                view.Button(
                    style: ["text-sm px-4 py-2 rounded-xl font-medium bg-gray-100 hover:bg-gray-200 text-gray-700 border border-gray-300 transition-all text-left"],
                    label: $"Model: {_ttsModel.Value}",
                    onClick: async () => { CycleTtsModel(); });

                view.Button(
                    style: ["text-sm px-4 py-2 rounded-xl font-medium bg-gray-100 hover:bg-gray-200 text-gray-700 border border-gray-300 transition-all text-left"],
                    label: $"Voice: {GetCurrentVoiceLabel()}",
                    onClick: async () => { CycleTtsVoice(); });
            });
        });
    }

    private void RenderAudioEffectsSettings(UIView view)
    {
        var _ = _ttsEffectsCount.Value;

        List<EffectEntry> effectsSnapshot;
        lock (_ttsEffectsLock)
        {
            effectsSnapshot = _ttsEffects.ToList();
        }

        view.Column(style: ["px-5 pb-4"], content: view =>
        {
            view.Text(style: ["text-sm font-semibold text-gray-700 mb-3 uppercase tracking-wide"], text: "Audio Effects");

            view.Column(style: ["flex flex-col gap-2"], content: view =>
            {
                for (var i = 0; i < effectsSnapshot.Count; i++)
                {
                    var effect = effectsSnapshot[i];
                    var effectIndex = i;
                    view.Column(style: ["px-4 py-3 rounded-xl bg-blue-50 border border-blue-200 mb-2"], content: view =>
                    {
                        view.Row(style: ["flex items-center justify-between mb-2"], content: view =>
                        {
                            view.Text(style: ["text-sm font-medium text-blue-700"], text: effect.EffectType);
                            view.Button(
                                style: ["text-xs px-2 py-1 rounded-lg bg-red-100 hover:bg-red-200 text-red-600 border border-red-300 transition-all"],
                                label: "✕",
                                onClick: async () => { RemoveTtsEffect(effectIndex); });
                        });
                        RenderTtsEffectParams(view, effect, effectIndex);
                    });
                }

                view.Row(style: ["flex flex-wrap gap-1 mt-2"], content: view =>
                {
                    foreach (var effectType in EffectTypes)
                    {
                        var type = effectType;
                        view.Button(
                            style: ["text-xs px-3 py-1.5 rounded-lg font-medium bg-gray-100 hover:bg-gray-200 text-gray-700 border border-gray-300 transition-all"],
                            label: $"+ {effectType}",
                            onClick: async () => { AddTtsEffect(type); });
                    }
                });

                if (effectsSnapshot.Count > 0)
                {
                    view.Button(
                        style: ["text-xs px-4 py-2 rounded-xl font-medium bg-red-100 hover:bg-red-200 text-red-600 border border-red-300 transition-all mt-2"],
                        label: "Clear All Effects",
                        onClick: async () => { ClearTtsEffects(); });
                }
            });
        });
    }

    private void RenderSttSettings(UIView view)
    {
        view.Column(style: ["px-5 pb-5"], content: view =>
        {
            view.Text(style: ["text-sm font-semibold text-gray-700 mb-3 uppercase tracking-wide"], text: "Recognition");

            view.Column(style: ["flex flex-col gap-2"], content: view =>
            {
                view.Button(
                    style: ["text-sm px-4 py-2 rounded-xl font-medium bg-gray-100 hover:bg-gray-200 text-gray-700 border border-gray-300 transition-all text-left"],
                    label: $"Model: {_sttModel.Value}",
                    onClick: async () => { CycleSttModel(); });

                view.Row(style: ["flex items-center gap-3 px-4 py-2 rounded-xl bg-gray-100 border border-gray-300"], content: view =>
                {
                    view.Switch(
                        style: ["w-10 h-5 rounded-full bg-gray-300 data-[state=checked]:bg-blue-500"],
                        @checked: _sttContinuousMode.Value,
                        onCheckedChange: value =>
                        {
                            _sttContinuousMode.Value = value;
                            return Task.CompletedTask;
                        },
                        content: view =>
                        {
                            view.SwitchThumb(style: ["block w-4 h-4 rounded-full bg-white shadow-md transition-transform data-[state=checked]:translate-x-5"]);
                        });
                    view.Text(style: ["text-sm font-medium text-gray-700"], text: _sttContinuousMode.Value ? "Continuous" : "Batch");
                });
            });
        });
    }

    private void CycleTtsModel()
    {
        var currentIndex = Array.IndexOf(TtsModels, _ttsModel.Value);
        _ttsModelIndex = (currentIndex + 1) % TtsModels.Length;
        _ttsModel.Value = TtsModels[_ttsModelIndex];
        _ttsVoiceIndex = 0;
        UpdateCurrentVoice();
    }

    private void CycleTtsVoice()
    {
        var voices = GetVoicesForCurrentModel();

        if (voices.Count == 0)
        {
            return;
        }

        _ttsVoiceIndex = (_ttsVoiceIndex + 1) % voices.Count;
        UpdateCurrentVoice();
    }

    private void UpdateCurrentVoice()
    {
        var voices = GetVoicesForCurrentModel();
        _ttsVoice.Value = voices.Count > 0 ? voices[_ttsVoiceIndex % voices.Count] : "";
    }

    private IReadOnlyList<string> GetVoicesForCurrentModel()
    {
        return VoicesByModel.TryGetValue(_ttsModel.Value, out var voices) ? voices : [];
    }

    private string GetCurrentVoiceLabel()
    {
        var voices = GetVoicesForCurrentModel();

        if (voices.Count == 0)
        {
            return "(default)";
        }

        var currentVoice = string.IsNullOrEmpty(_ttsVoice.Value) ? voices[0] : _ttsVoice.Value;
        return $"{currentVoice} ({_ttsVoiceIndex + 1}/{voices.Count})";
    }

    private async Task RunIdleLoopAsync()
    {
        var ct = _appCts.Token;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(IdleCheckIntervalSeconds), ct);

                var secondsIdle = (DateTime.UtcNow - _lastInteractionTime).TotalSeconds;
                if (secondsIdle < IdleThresholdSeconds) continue;
                if (_isProcessingMessage.Value || _ttsSpeaking.Value || _isListening.Value) continue;

                try
                {
                    var currentMotion = _currentMotion.Value;
                    var (idleAction, _) = await Emerge.Run<IdleAction>(
                        LLMModel.Gpt41Nano,
                        new KernelContext(),
                        pass =>
                        {
                            pass.SystemPrompt = "You control a 3D virtual character's idle behavior. "
                                + "Pick a motion and expression for the character to do while idle.\n"
                                + "Available motions: idle, thinking, shy, confident, stretching, looking_around\n"
                                + "Available expressions: happy, relaxed, surprised, neutral\n"
                                + "Current motion: " + currentMotion + "\n"
                                + "Pick something different from the current motion to keep the character lively.";
                            pass.Command = "Choose an idle action";
                            pass.Temperature = 0.9f;
                            pass.MaxOutputTokens = 100;
                        },
                        ct
                    ).FinalAsync(ct);

                    if (_isProcessingMessage.Value || _ttsSpeaking.Value || _isListening.Value) continue;

                    _currentMotion.Value = idleAction.Motion;
                    _currentExpression.Value = idleAction.Expression;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VRMChat] Idle loop error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(12), ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when app is stopping
        }
    }

    private void QueueUserMessage(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            return;
        }

        _lastInteractionTime = DateTime.UtcNow;
        StopSpeaking();

        var userContent = new Reactive<string>(userText);
        lock (_chatMessagesLock)
        {
            _chatMessages.Add(new ChatMessage(ChatRole.User, userContent));
            _chatMessagesVersion.Value++;
        }

        _messageQueue.Writer.TryWrite(userText);

        if (!_messageProcessorStarted)
        {
            _messageProcessorStarted = true;
            ProcessMessageQueueAsync().RunParallel();
        }
    }

    private async Task ProcessMessageQueueAsync()
    {
        var ct = _appCts.Token;

        try
        {
            await foreach (var userText in _messageQueue.Reader.ReadAllAsync(ct))
            {
                _isProcessingMessage.Value = true;
                var assistantContent = new Reactive<string>("");

                lock (_chatMessagesLock)
                {
                    _chatMessages.Add(new ChatMessage(ChatRole.Assistant, assistantContent));
                    _chatMessagesVersion.Value++;
                }

                try
                {
                    _currentMotion.Value = "thinking";
                    _currentExpression.Value = "neutral";
                    _lastInteractionTime = DateTime.UtcNow;

                    var (reply, updatedContext) = await Emerge.Run<ChatReply>(
                        LLMModel.Gpt41,
                        _chatContext,
                        pass =>
                        {
                            pass.SystemPrompt = "You are a friendly and helpful VRM 3D virtual assistant. "
                                + "You have an expressive animated 3D avatar that users can see. "
                                + "Keep your responses conversational, warm, and concise - typically 1-3 sentences.\n\n"
                                + "For each reply, also choose a Motion and Expression for your avatar.\n"
                                + "Available motions: idle, thinking, excited, shy, confident, waving, listening, talking, stretching, looking_around\n"
                                + "Available expressions: happy, angry, sad, relaxed, surprised, neutral\n"
                                + "Pick the motion and expression that best match the emotional tone of your reply.";
                            pass.Command = userText;
                            pass.Temperature = 0.7f;
                        },
                        ct
                    ).FinalAsync(ct);

                    _chatContext = updatedContext;
                    _lastInteractionTime = DateTime.UtcNow;

                    var responseText = reply.Message;
                    assistantContent.Value = responseText;
                    _chatMessagesVersion.Value++;

                    _currentMotion.Value = reply.Motion;
                    _currentExpression.Value = reply.Expression;

                    _currentMotion.Value = "talking";
                    await SpeakTextAsync(responseText);

                    _currentMotion.Value = "idle";
                    _currentExpression.Value = "neutral";
                    _lastInteractionTime = DateTime.UtcNow;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    assistantContent.Value = $"Sorry, I encountered an error: {ex.Message}";
                    _chatMessagesVersion.Value++;
                }
                finally
                {
                    _isProcessingMessage.Value = false;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when app is stopping
        }
    }

    private void StopSpeaking()
    {
        lock (_speechLock)
        {
            _speechCts?.Cancel();
            _speechCts?.Dispose();
            _speechCts = null;

            _speechGenerator?.Dispose();
            _speechGenerator = null;

            _ttsSpeaking.Value = false;
        }
    }

    private async Task SpeakTextAsync(string textToSpeak)
    {
        if (string.IsNullOrWhiteSpace(textToSpeak))
        {
            return;
        }

        CancellationToken cancellationToken;
        SpeechGenerator generator;

        lock (_speechLock)
        {
            if (_ttsSpeaking.Value)
            {
                return;
            }

            _ttsSpeaking.Value = true;

            _speechCts?.Cancel();
            _speechCts?.Dispose();
            _speechCts = new CancellationTokenSource();
            cancellationToken = _speechCts.Token;

            _speechGenerator?.Dispose();
            _speechGenerator = new SpeechGenerator(_ttsModel.Value);
            generator = _speechGenerator;
        }

        try
        {
            var config = new SpeechGeneratorConfig
            {
                Text = textToSpeak,
                VoiceId = _ttsVoice.Value
            };

            List<IAudioEffect> effects;
            lock (_ttsEffectsLock)
            {
                effects = _ttsEffects.Select(e => e.Effect).ToList();
            }

            var analyzers = new IAudioAnalyzer[] { _visemeAnalyzer };

            await foreach (var audio in generator.GenerateSpeechAsync(config).WithCancellation(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                Audio.SendSpeech(audio, effects, analyzers);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when speech is stopped
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VRMChat] Speech error: {ex.Message}");
        }
        finally
        {
            lock (_speechLock)
            {
                _ttsSpeaking.Value = false;
            }
        }
    }

    private void AddTtsEffect(string effectType)
    {
        var defaultParams = GetDefaultParams(effectType);
        var reactiveParams = EffectEntry.ToReactiveParams(defaultParams);
        var effect = CreateEffect(effectType, defaultParams);
        var entry = new EffectEntry(effectType, effect, reactiveParams);

        lock (_ttsEffectsLock)
        {
            _ttsEffects.Add(entry);
            _ttsEffectsCount.Value = _ttsEffects.Count;
        }
    }

    private void RemoveTtsEffect(int index)
    {
        lock (_ttsEffectsLock)
        {
            if (index < 0 || index >= _ttsEffects.Count)
            {
                return;
            }

            _ttsEffects.RemoveAt(index);
            _ttsEffectsCount.Value = _ttsEffects.Count;
        }
    }

    private void UpdateTtsEffectParam(int index, string paramKey, float value)
    {
        lock (_ttsEffectsLock)
        {
            if (index < 0 || index >= _ttsEffects.Count)
            {
                return;
            }

            var entry = _ttsEffects[index];
            entry.Params[paramKey].Value = value;
            var newEffect = CreateEffect(entry.EffectType, entry.GetParamValues());
            entry.Effect = newEffect;
        }
    }

    private void ClearTtsEffects()
    {
        lock (_ttsEffectsLock)
        {
            _ttsEffects.Clear();
            _ttsEffectsCount.Value = 0;
        }
    }

    private void RenderTtsEffectParams(UIView view, EffectEntry entry, int effectIndex)
    {
        foreach (var kvp in entry.Params)
        {
            RenderEffectParamRow(view, effectIndex, kvp.Key, kvp.Value.Value, UpdateTtsEffectParam);
        }
    }

    private void RenderEffectParamRow(UIView view, int effectIndex, string paramKey, float paramValue, Action<int, string, float> updateAction)
    {
        view.Row(style: ["flex items-center gap-2"], content: rowView =>
        {
            rowView.Text(style: ["text-xs text-gray-500 w-20"], text: paramKey);
            rowView.TextField(
                style: ["w-16 px-2 py-1 text-sm font-mono bg-white border border-gray-300 rounded text-gray-700"],
                value: paramValue.ToString("F2"),
                type: "number",
                props: new Dictionary<string, object?> { ["step"] = "0.1" },
                onValueChange: value =>
                {
                    if (float.TryParse(value, out var v))
                    {
                        updateAction(effectIndex, paramKey, v);
                    }

                    return Task.CompletedTask;
                });
        });
    }

    private void CycleSttModel()
    {
        var currentIndex = Array.IndexOf(SttModels, _sttModel.Value);
        _sttModelIndex = (currentIndex + 1) % SttModels.Length;
        _sttModel.Value = SttModels[_sttModelIndex];
    }

    private List<IAudioEffectInstance> GetSttEffectInstances(SttStreamState state)
    {
        if (state.EffectInstances == null || state.EffectInstances.Count != _sttEffects.Count)
        {
            state.EffectInstances = _sttEffects
                .Select(e => e.Effect.Create(state.SampleRate, state.ChannelCount))
                .ToList();
        }

        return state.EffectInstances;
    }

    private async Task RunBatchRecognitionAsync(SttStreamState state)
    {
        try
        {
            var samples = state.GetAllSamples();

            if (samples.Length == 0)
            {
                return;
            }

            using var recognizer = new SpeechRecognizer(_sttModel.Value);
            var config = new RecognizeSpeechConfig
            {
                Samples = samples,
                SampleRate = state.SampleRate,
                ChannelCount = state.ChannelCount,
                Language = _sttLanguage.Value
            };

            var result = await recognizer.RecognizeBatchSpeechAsync(config);
            _sttRecognizedText.Value = result;

            if (!string.IsNullOrWhiteSpace(result))
            {
                QueueUserMessage(result);
            }
        }
        catch (Exception ex)
        {
            _sttRecognizedText.Value = $"Error: {ex.Message}";
        }
    }

    private async Task RunContinuousRecognitionAsync(SttStreamState state)
    {
        try
        {
            using var recognizer = new SpeechRecognizer(_sttModel.Value);

            var config = new RecognizeContinuousSpeechConfig
            {
                SampleRate = state.SampleRate,
                ChannelCount = state.ChannelCount,
                Language = _sttLanguage.Value
            };

            string lastText = "";
            if (recognizer.SupportsContinuousRecognition)
            {
                await foreach (var text in recognizer.RecognizeContinuousSpeechAsync(config, state.GetSamplesAsync()))
                {
                    _sttRecognizedText.Value = text;
                    lastText = text;
                }
            }
            else
            {
                var adapter = new SpeechRecognizerAdapter(recognizer, new SpeechRecognizerAdapter.Config
                {
                    Mode = SpeechRecognizerAdapter.Mode.SilenceTriggered,
                    SilenceDuration = TimeSpan.FromMilliseconds(500),
                    SilenceThreshold = 0.01f
                });

                await foreach (var text in adapter.RecognizeContinuousSpeechAsync(config, state.GetSamplesAsync()))
                {
                    _sttRecognizedText.Value = text;
                    lastText = text;
                }
            }

            if (!string.IsNullOrWhiteSpace(lastText))
            {
                QueueUserMessage(lastText);
            }
        }
        catch (Exception ex)
        {
            _sttRecognizedText.Value = $"Error: {ex.Message}";
        }
    }

    private static Dictionary<string, float> GetDefaultParams(string effectType)
    {
        return effectType switch
        {
            "Delay" => new Dictionary<string, float>
            {
                ["delayMs"] = 320f,
                ["feedback"] = 0.35f,
                ["mix"] = 0.4f,
                ["damping"] = 0.35f
            },
            "Reverb" => new Dictionary<string, float>
            {
                ["roomSize"] = 0.5f,
                ["decay"] = 0.5f,
                ["damping"] = 0.3f,
                ["mix"] = 0.6f
            },
            "Chorus" => new Dictionary<string, float>
            {
                ["baseDelayMs"] = 18f,
                ["depthMs"] = 6f,
                ["rateHz"] = 0.8f,
                ["mix"] = 0.35f
            },
            "Tremolo" => new Dictionary<string, float>
            {
                ["rateHz"] = 5f,
                ["depth"] = 0.65f,
                ["mix"] = 0.8f
            },
            "BitCrusher" => new Dictionary<string, float>
            {
                ["bitDepth"] = 12f,
                ["downsample"] = 4f,
                ["mix"] = 0.5f
            },
            "Saturation" => new Dictionary<string, float>
            {
                ["drive"] = 2.5f,
                ["mix"] = 0.65f
            },
            "RobotVoice" => new Dictionary<string, float>
            {
                ["carrierHz"] = 110f,
                ["mix"] = 0.85f,
                ["drive"] = 0.25f
            },
            "Telephone" => new Dictionary<string, float>
            {
                ["lowCutHz"] = 300f,
                ["highCutHz"] = 3400f,
                ["mix"] = 1f,
                ["drive"] = 0.15f
            },
            _ => new Dictionary<string, float>()
        };
    }

    private static IAudioEffect CreateEffect(string effectType, Dictionary<string, float> p)
    {
        return effectType switch
        {
            "Delay" => new DelayAudioEffect(p["delayMs"], p["feedback"], p["mix"], p["damping"]),
            "Reverb" => new ReverbAudioEffect(p["roomSize"], p["decay"], p["damping"], p["mix"]),
            "Chorus" => new ChorusAudioEffect(p["baseDelayMs"], p["depthMs"], p["rateHz"], p["mix"]),
            "Tremolo" => new TremoloAudioEffect(p["rateHz"], p["depth"], p["mix"]),
            "BitCrusher" => new BitCrusherAudioEffect((int)p["bitDepth"], (int)p["downsample"], p["mix"]),
            "Saturation" => new SaturationAudioEffect(p["drive"], p["mix"]),
            "RobotVoice" => new RobotVoiceAudioEffect(p["carrierHz"], p["mix"], p["drive"]),
            "Telephone" => new TelephoneAudioEffect(p["lowCutHz"], p["highCutHz"], p["mix"], p["drive"]),
            _ => throw new ArgumentException($"Unknown effect type: {effectType}")
        };
    }
}

internal class SttStreamState(int sampleRate, int channelCount)
{
    private readonly List<float> _samples = [];
    private System.Threading.Channels.Channel<float[]> _channel = System.Threading.Channels.Channel.CreateUnbounded<float[]>();
    private readonly object _lock = new();

    public int SampleRate { get; } = sampleRate;
    public int ChannelCount { get; } = channelCount;
    public List<IAudioEffectInstance>? EffectInstances { get; set; }
    public WavFile? RecordingWavFile { get; set; }

    public void Reset()
    {
        lock (_lock)
        {
            _samples.Clear();
            _channel.Writer.TryComplete();
            _channel = System.Threading.Channels.Channel.CreateUnbounded<float[]>();
            EffectInstances = null;
        }

        RecordingWavFile?.Dispose();
        RecordingWavFile = null;
    }

    public void AddSamples(ReadOnlySpan<float> samples)
    {
        var copy = samples.ToArray();

        lock (_lock)
        {
            _samples.AddRange(copy);
        }

        _channel.Writer.TryWrite(copy);
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }

    public float[] GetAllSamples()
    {
        lock (_lock)
        {
            return _samples.ToArray();
        }
    }

    public async IAsyncEnumerable<float[]> GetSamplesAsync()
    {
        await foreach (var chunk in _channel.Reader.ReadAllAsync())
        {
            yield return chunk;
        }
    }
}

internal class EffectEntry(string effectType, IAudioEffect effect, Dictionary<string, Reactive<float>> reactiveParams)
{
    public string EffectType { get; } = effectType;
    public IAudioEffect Effect { get; set; } = effect;
    public Dictionary<string, Reactive<float>> Params { get; } = reactiveParams;

    public Dictionary<string, float> GetParamValues()
    {
        var result = new Dictionary<string, float>();
        foreach (var kvp in Params)
        {
            result[kvp.Key] = kvp.Value.Value;
        }
        return result;
    }

    public static Dictionary<string, Reactive<float>> ToReactiveParams(Dictionary<string, float> plainParams)
    {
        var result = new Dictionary<string, Reactive<float>>();
        foreach (var kvp in plainParams)
        {
            result[kvp.Key] = new Reactive<float>(kvp.Value);
        }
        return result;
    }
}
