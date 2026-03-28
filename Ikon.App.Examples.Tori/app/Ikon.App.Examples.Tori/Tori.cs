return await App.Run(args);

public record SessionIdentity(string Id);
public record ClientParams(string Id = "", string Name = "");

[App]
public partial class Tori(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new Theme());
    private Audio Audio { get; } = new(app);
    private Video Video { get; } = new(app);

    // Participant tracking
    public record Participant(
        int ClientSessionId,
        string UserId,
        string Name,
        string? VideoStreamId,
        string? EchoVideoStreamId,
        bool IsVideoEnabled,
        bool IsAudioEnabled,
        bool IsScreenSharing = false,
        string? ScreenShareStreamId = null,
        bool IsMobile = false);

    // Participant background gradients (light mode) - expanded to fill more of the tile
    private static readonly string[] LightModeGradients =
    [
        "bg-[radial-gradient(circle_at_center,rgba(147,51,234,0.3)_0%,rgba(236,72,153,0.2)_50%,rgba(147,51,234,0.1)_100%)]",  // Purple-Pink
        "bg-[radial-gradient(circle_at_center,rgba(59,130,246,0.3)_0%,rgba(6,182,212,0.2)_50%,rgba(59,130,246,0.1)_100%)]",   // Blue-Cyan
        "bg-[radial-gradient(circle_at_center,rgba(34,197,94,0.3)_0%,rgba(16,185,129,0.2)_50%,rgba(34,197,94,0.1)_100%)]",    // Green-Emerald
        "bg-[radial-gradient(circle_at_center,rgba(249,115,22,0.3)_0%,rgba(234,179,8,0.2)_50%,rgba(249,115,22,0.1)_100%)]",   // Orange-Yellow
        "bg-[radial-gradient(circle_at_center,rgba(236,72,153,0.3)_0%,rgba(244,63,94,0.2)_50%,rgba(236,72,153,0.1)_100%)]",   // Pink-Rose
        "bg-[radial-gradient(circle_at_center,rgba(99,102,241,0.3)_0%,rgba(139,92,246,0.2)_50%,rgba(99,102,241,0.1)_100%)]",  // Indigo-Violet
    ];

    // Participant background gradients (dark mode) - expanded to fill more of the tile
    private static readonly string[] DarkModeGradients =
    [
        "bg-[radial-gradient(circle_at_center,rgba(147,51,234,0.4)_0%,rgba(236,72,153,0.25)_50%,rgba(147,51,234,0.15)_100%)]",  // Purple-Pink
        "bg-[radial-gradient(circle_at_center,rgba(59,130,246,0.4)_0%,rgba(6,182,212,0.25)_50%,rgba(59,130,246,0.15)_100%)]",   // Blue-Cyan
        "bg-[radial-gradient(circle_at_center,rgba(34,197,94,0.4)_0%,rgba(16,185,129,0.25)_50%,rgba(34,197,94,0.15)_100%)]",    // Green-Emerald
        "bg-[radial-gradient(circle_at_center,rgba(249,115,22,0.4)_0%,rgba(234,179,8,0.25)_50%,rgba(249,115,22,0.15)_100%)]",   // Orange-Yellow
        "bg-[radial-gradient(circle_at_center,rgba(236,72,153,0.4)_0%,rgba(244,63,94,0.25)_50%,rgba(236,72,153,0.15)_100%)]",   // Pink-Rose
        "bg-[radial-gradient(circle_at_center,rgba(99,102,241,0.4)_0%,rgba(139,92,246,0.25)_50%,rgba(99,102,241,0.15)_100%)]",  // Indigo-Violet
    ];

    private readonly Reactive<List<Participant>> _participants = new([]);
    private readonly Reactive<int> _participantsVersion = new(0);

    // Per-client state
    private readonly ClientReactive<bool> _isVideoEnabled = new(false);
    private readonly ClientReactive<bool> _isAudioEnabled = new(false);
    private readonly ClientReactive<bool> _isScreenShareEnabled = new(false);

    // Video stream tracking
    private readonly Dictionary<string, VideoStreamInfo> _videoStreamStates = new();
    private readonly Dictionary<int, (int ClientSessionId, int InputTrackId)> _outputToInputTrack = new();

    // Audio stream tracking
    private readonly Dictionary<string, AudioStreamState> _audioStreamStates = new();

    // Speaking detection state
    private readonly Dictionary<int, SpeakingState> _speakingStates = new();
    private readonly Reactive<int> _speakingVersion = new(0);
    private const float SpeakingVolumeThreshold = 0.003f;
    private const float EmaAlphaUp = 0.4f;
    private const float EmaAlphaDown = 0.03f;
    private static readonly TimeSpan SpeakingTimeout = TimeSpan.FromMilliseconds(1800);

    // Max entries for transcript and chat lists to prevent unbounded memory growth
    private const int MaxTranscriptEntries = 1000;
    private const int MaxChatEntries = 1000;

    // Chat state
    public record ChatMessage(string Id, string SenderName, DateTime Timestamp, string Content);
    private readonly Reactive<List<ChatMessage>> _chatMessages = new([]);
    private readonly Reactive<int> _chatMessagesVersion = new(0);
    private readonly ClientReactive<string> _chatInputText = new("");

    // Speech recognition state
    public record TranscriptEntry(string ParticipantName, string Text, DateTime Timestamp);
    private readonly Reactive<List<TranscriptEntry>> _recognizedSpeech = new([]);
    private readonly Reactive<int> _recognizedSpeechVersion = new(0);
    private readonly Reactive<bool> _speechEnabled = new(true);
    private readonly Reactive<SpeechRecognizerModel> _speechModel = new(SpeechRecognizerModel.WhisperLarge3Turbo);
    private readonly Reactive<string> _speechLanguage = new("en-US");
    private readonly ClientReactive<bool> _settingsOpen = new(false);
    private readonly Dictionary<int, ParticipantSpeechState> _participantSpeechStates = new();

    // Right panel state
    private readonly ClientReactive<string> _rightPanelTab = new("people");
    private readonly Reactive<string> _summary = new("");
    private readonly Reactive<bool> _summaryExtracting = new(false);
    private readonly Reactive<int> _summaryVersion = new(0);
    private readonly Reactive<bool> _summaryEnabled = new(true);
    private readonly Reactive<LLMModel> _summaryModel = new(LLMModel.Gemini25Pro);
    private readonly ClientReactive<string> _settingsTab = new("audio");

    // Track what has been sent to LLM for summary extraction (using version numbers since lists can be capped)
    private int _lastExtractedTranscriptVersion;
    private int _lastExtractedChatVersion;

    // Theme state
    private readonly ClientReactive<string> _currentTheme = new(Constants.LightTheme);

    // Timezone state (IANA timezone identifier, e.g. "America/New_York")
    private readonly ClientReactive<string> _clientTimezone = new("UTC");

    // Mobile device state (screen sharing not supported on mobile)
    private readonly ClientReactive<bool> _isMobile = new(false);

    // Mobile panel state
    private readonly ClientReactive<bool> _mobilePanelOpen = new(false);
    private readonly ClientReactive<string> _mobilePanelTab = new("chat");

    // Mobile layout breakpoint (width in pixels)
    private const int MobileLayoutBreakpoint = 768;

    // Media capture settings (per-client)
    private readonly ClientReactive<string> _cameraResolution = new("1280x720");
    private readonly ClientReactive<string> _cameraFramerate = new("30");
    private readonly ClientReactive<string> _cameraQuality = new("medium");
    private readonly ClientReactive<string> _cameraCodec = new("H264");
    private readonly ClientReactive<string> _screenFramerate = new("30");
    private readonly ClientReactive<string> _screenQuality = new("medium");
    private readonly ClientReactive<string> _screenCodec = new("H264");
    private readonly ClientReactive<string> _audioQuality = new("medium");
    private readonly ClientReactive<bool> _audioEchoCancellation = new(true);
    private readonly ClientReactive<bool> _audioNoiseSuppression = new(true);
    private readonly ClientReactive<bool> _audioAutoGainControl = new(true);

    // Device selection state (per-client)
    private readonly ClientReactive<IReadOnlyList<ClientMediaDevice>> _availableDevices = new([]);
    private readonly ClientReactive<string> _selectedMicrophoneId = new("default");
    private readonly ClientReactive<string> _selectedCameraId = new("default");
    private readonly ClientReactive<bool> _devicesLoading = new(false);

    // Join flow state
    private readonly ClientReactive<string> _nameInput = new("");
    private readonly ClientReactive<bool> _hasJoined = new(false);
    private readonly ClientReactive<string> _joinedName = new("");
    private readonly ClientReactive<bool> _newMeetMenuOpen = new(false);
    private readonly ClientReactive<bool> _meetLinkDialogOpen = new(false);
    private readonly ClientReactive<string> _generatedMeetLink = new("");

    // Leave meeting state
    private readonly ClientReactive<bool> _hasLeft = new(false);
    private readonly ClientReactive<string?> _activeAudioStreamId = new((string?)null);
    private readonly ClientReactive<string?> _activeVideoStreamId = new((string?)null);
    private readonly ClientReactive<string?> _activeScreenShareStreamId = new((string?)null);
    private readonly ClientReactive<bool> _leaveConfirmDialogOpen = new(false);

    // ID format: xxxx-xxxx-xxxx where x is lowercase a-z
    private static readonly System.Text.RegularExpressions.Regex MeetIdPattern =
        new(@"^[a-z]{4}-[a-z]{4}-[a-z]{4}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static bool IsValidMeetId(string? id)
    {
        return !string.IsNullOrEmpty(id) && MeetIdPattern.IsMatch(id);
    }

    private static string GenerateMeetId()
    {
        var random = new Random();
        var chars = "abcdefghijklmnopqrstuvwxyz";
        var parts = new string[3];

        for (var i = 0; i < 3; i++)
        {
            var part = new char[4];

            for (var j = 0; j < 4; j++)
            {
                part[j] = chars[random.Next(chars.Length)];
            }

            parts[i] = new string(part);
        }

        return string.Join("-", parts);
    }

    public async Task Main()
    {
        SetupVideoInputHandlers();
        SetupAudioInputHandlers();
        SetupClientHandlers();

        _ = RunUpdateLoopAsync();

        UI.Root([Page.Default, "font-sans h-screen overflow-hidden"], content: RenderApp);
    }

    private async Task RunUpdateLoopAsync()
    {
        var keyPointsCheckCounter = 0;

        while (true)
        {
            await Task.Delay(1000);

            // Check for speaking timeouts (participant muted or disconnected audio)
            var now = DateTime.UtcNow;
            var speakingChanged = false;

            foreach (var (_, state) in _speakingStates)
            {
                if (state.IsSpeaking && now - state.LastAudioTime > SpeakingTimeout)
                {
                    state.IsSpeaking = false;
                    state.EmaVolume = 0;
                    speakingChanged = true;
                }
            }

            if (speakingChanged)
            {
                _speakingVersion.Value++;
            }

            // Every 60 seconds, check if there's new content and extract summary
            keyPointsCheckCounter++;

            if (keyPointsCheckCounter >= 60)
            {
                keyPointsCheckCounter = 0;

                var hasNewTranscript = _recognizedSpeechVersion.Value > _lastExtractedTranscriptVersion;
                var hasNewChat = _chatMessagesVersion.Value > _lastExtractedChatVersion;

                if ((hasNewTranscript || hasNewChat) && _summaryEnabled.Value)
                {
                    _ = ExtractSummaryAsync();
                }
            }
        }
    }

    private async Task ExtractSummaryAsync(bool force = false)
    {
        if (_summaryExtracting.Value)
        {
            return;
        }

        // Calculate how many new entries since last extraction using version numbers
        var transcriptVersionDelta = _recognizedSpeechVersion.Value - _lastExtractedTranscriptVersion;
        var chatVersionDelta = _chatMessagesVersion.Value - _lastExtractedChatVersion;

        // Check if there's actually new content (skip for forced/manual extraction)
        if (!force && transcriptVersionDelta <= 0 && chatVersionDelta <= 0)
        {
            return;
        }

        // For forced extraction, use all available content if no new content
        if (force && transcriptVersionDelta <= 0 && chatVersionDelta <= 0)
        {
            transcriptVersionDelta = _recognizedSpeech.Value.Count;
            chatVersionDelta = _chatMessages.Value.Count;
        }

        // Get new entries - take the last N entries where N = version delta (capped to list size)
        var transcriptEntries = _recognizedSpeech.Value;
        var chatEntries = _chatMessages.Value;
        var newTranscriptCount = Math.Min(transcriptVersionDelta, transcriptEntries.Count);
        var newChatCount = Math.Min(chatVersionDelta, chatEntries.Count);
        var newTranscriptEntries = newTranscriptCount > 0 ? transcriptEntries.TakeLast(newTranscriptCount).ToList() : [];
        var newChatEntries = newChatCount > 0 ? chatEntries.TakeLast(newChatCount).ToList() : [];

        _summaryExtracting.Value = true;

        try
        {
            var model = _summaryModel.Value;

            var contextParts = new List<string>();

            // Add previous summary if available
            if (!string.IsNullOrWhiteSpace(_summary.Value))
            {
                contextParts.Add($"## Previous Summary\n\n{_summary.Value}");
            }

            // Add only NEW transcript entries (without timestamps - they're not needed for summary)
            if (newTranscriptEntries.Count > 0)
            {
                var transcriptText = string.Join("\n\n", newTranscriptEntries.Select(e => $"{e.ParticipantName}: {e.Text}"));
                contextParts.Add($"## New Transcript (since last update)\n\n{transcriptText}");
            }

            // Add only NEW chat messages (without timestamps - they're not needed for summary)
            if (newChatEntries.Count > 0)
            {
                var chatText = string.Join("\n", newChatEntries.Select(m => $"{m.SenderName}: {m.Content}"));
                contextParts.Add($"## New Chat Messages (since last update)\n\n{chatText}");
            }

            var context = string.Join("\n\n---\n\n", contextParts);

            Log.Instance.Info($"Summary extraction: {newTranscriptEntries.Count} new transcript entries, {newChatEntries.Count} new chat entries");
            Log.Instance.Info($"Summary context:\n{context}");

            var (result, _) = await Emerge.Run<SummaryResult>(model, new KernelContext(), pass =>
            {
                pass.SystemPrompt = """
                    You are a meeting assistant that extracts and summarizes key points from meeting transcripts and chat messages.
                    Your output should be concise, well-structured markdown.
                    You are given the previous summary and only NEW transcript/chat content since the last extraction.
                    Update the summary with the new information, integrating it with the existing content.
                    If the information amount is getting big, try filtering out less important details.
                    """;
                pass.Command = $"""
                    Update the summary with the new meeting content. You are only given NEW transcript and chat messages since the last extraction - not the full history.

                    Use this exact structure:

                    ## Key Points
                    - List the main topics discussed, decisions made, and important information
                    - Keep each point concise and actionable
                    - Integrate new information with existing content
                    - If there starts to be a lot of information, consider filtering out less important details

                    ## Action Items
                    For each participant who has action items, create a subsection:
                    ### [Participant Name]
                    - List their specific action items, tasks, or commitments
                    - Include any deadlines mentioned

                    Only include participants who have action items. If no action items were discussed, omit the Action Items section entirely.

                    Content:
                    {context}
                    """;
                pass.Temperature = 0.3f;
                pass.MaxOutputTokens = 32000;
            }).FinalAsync();

            // Update tracking versions after successful extraction
            _lastExtractedTranscriptVersion = _recognizedSpeechVersion.Value;
            _lastExtractedChatVersion = _chatMessagesVersion.Value;

            Log.Instance.Info($"Summary extraction result:\n{result.Markdown}");

            _summary.Value = result.Markdown;
            _summaryVersion.Value++;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to extract summary: {ex.Message}");
        }
        finally
        {
            _summaryExtracting.Value = false;
        }
    }

    private string GetTranscriptAsText()
    {
        if (_recognizedSpeech.Value.Count == 0)
        {
            return "";
        }

        return string.Join("\n\n", _recognizedSpeech.Value.Select(e => $"{e.ParticipantName}: {e.Text}"));
    }

    private void SetupClientHandlers()
    {
        app.ClientJoinedAsync += async args =>
        {
            await ClientFunctions.KeepScreenAwakeAsync(args.ClientSessionId, true);

            InitializeClientThemeAndTimezone(args.ClientSessionId, args.ClientContext);

            _ = RefreshDevicesAsync(args.ClientSessionId, args.ClientContext);

            var client = app.Clients[args.ClientSessionId];
            var meetId = client?.Parameters.Id ?? "";
            var name = client?.Parameters.Name ?? "";

            if (!IsValidMeetId(meetId) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var isMobile = args.ClientContext.ClientType is ClientType.MobileWeb or ClientType.MobileApp;
            AddParticipant(args.ClientSessionId, args.ClientContext.UserId, name, isMobile);
        };

        app.ClientLeftAsync += async args =>
        {
            await CleanupClientStreamsAsync(args.ClientSessionId);

            var list = _participants.Value.Where(p => p.ClientSessionId != args.ClientSessionId).ToList();
            _participants.Value = list;
            _participantsVersion.Value++;
        };

        app.MessageReceivedAsync += async args =>
        {
            if (args.Message.Opcode == Opcode.VIDEO_REQUEST_IDR_FRAME)
            {
                // Forward IDR request to the video source client
                if (_outputToInputTrack.TryGetValue(args.Message.TrackId, out var inputInfo))
                {
                    await app.SendMessageAsync(ProtocolMessage.Create(
                        app.ClientContext.SessionId,
                        new RequestIdrVideoFrame(),
                        trackId: inputInfo.InputTrackId,
                        targetIds: [inputInfo.ClientSessionId]));
                }
            }
        };
    }

    private void InitializeClientThemeAndTimezone(int clientSessionId, Context clientContext)
    {
        using var _ = ReactiveScope.Use(new ClientScope(clientSessionId));

        if (!string.IsNullOrEmpty(clientContext.Theme))
        {
            _currentTheme.Value = clientContext.Theme == Constants.DarkTheme ? Constants.DarkTheme : Constants.LightTheme;
        }

        if (!string.IsNullOrEmpty(clientContext.Timezone))
        {
            _clientTimezone.Value = clientContext.Timezone;
        }

        _isMobile.Value = clientContext.ClientType is ClientType.MobileWeb or ClientType.MobileApp;
    }

    private async Task CleanupClientStreamsAsync(int clientSessionId)
    {
        var participant = _participants.Value.FirstOrDefault(p => p.ClientSessionId == clientSessionId);

        if (participant?.VideoStreamId != null)
        {
            // Clean up output→input mapping
            var outputInfo = Video.GetOutputStreamInfo(participant.VideoStreamId);

            if (outputInfo != null)
            {
                _outputToInputTrack.Remove(outputInfo.TrackId);
            }

            await Video.CloseAsync(participant.VideoStreamId);
            _videoStreamStates.Remove(participant.VideoStreamId);
        }

        // Clean up speech recognition state
        if (_participantSpeechStates.TryGetValue(clientSessionId, out var speechState))
        {
            speechState.Dispose();
            _participantSpeechStates.Remove(clientSessionId);
        }

        // Audio streams will be cleaned up when AudioInputStreamEndAsync fires
    }

    private Participant? GetCurrentClientParticipant()
    {
        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope == null)
        {
            return null;
        }

        return _participants.Value.FirstOrDefault(p => p.ClientSessionId == clientScope.Value.Id);
    }

    private void UpdateParticipant(int clientSessionId, Func<Participant, Participant> update)
    {
        var list = _participants.Value.ToList();
        var index = list.FindIndex(p => p.ClientSessionId == clientSessionId);

        if (index >= 0)
        {
            list[index] = update(list[index]);
            _participants.Value = list;
            _participantsVersion.Value++;
        }
    }

    private void AddParticipant(int clientSessionId, string userId, string name, bool isMobile)
    {
        // Check if participant already exists
        if (_participants.Value.Any(p => p.ClientSessionId == clientSessionId))
        {
            return;
        }

        var participant = new Participant(clientSessionId, userId, name, null, null, false, false, IsMobile: isMobile);
        var list = _participants.Value.ToList();
        list.Add(participant);
        _participants.Value = list;
        _participantsVersion.Value++;
    }

    private void JoinMeeting(string name)
    {
        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope == null || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var clientSessionId = clientScope.Value.Id;
        var context = app.GlobalState.GetClientContext(clientSessionId);
        var userId = context.UserId;
        var isMobile = context.ClientType is ClientType.MobileWeb or ClientType.MobileApp;

        _joinedName.Value = name;
        _hasJoined.Value = true;

        AddParticipant(clientSessionId, userId, name, isMobile);
    }

    private async Task ToggleThemeAsync()
    {
        var currentTheme = _currentTheme.Value;
        var nextTheme = currentTheme == Constants.DarkTheme ? Constants.LightTheme : Constants.DarkTheme;
        var updated = await ClientFunctions.SetThemeAsync(nextTheme);

        if (updated)
        {
            _currentTheme.Value = nextTheme;
        }
    }

    private async Task RefreshDevicesAsync(int clientSessionId, Context clientContext)
    {
        using var _ = ReactiveScope.Use(new ClientScope(clientSessionId));

        if (_devicesLoading.Value)
        {
            return;
        }

        _devicesLoading.Value = true;

        try
        {
            var devices = await ClientFunctions.GetMediaDevicesAsync(clientSessionId);
            _availableDevices.Value = devices;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Failed to get media devices: {ex.Message}");
        }
        finally
        {
            _devicesLoading.Value = false;
        }
    }

    private async Task LeaveCallAsync()
    {
        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope == null)
        {
            return;
        }

        var sessionId = clientScope.Value.Id;

        // Stop all active capture streams
        var stopTasks = new List<Task>();

        if (_activeAudioStreamId.Value != null)
        {
            stopTasks.Add(ClientFunctions.StopCaptureAsync(sessionId, _activeAudioStreamId.Value));
        }

        if (_activeVideoStreamId.Value != null)
        {
            stopTasks.Add(ClientFunctions.StopCaptureAsync(sessionId, _activeVideoStreamId.Value));
        }

        if (_activeScreenShareStreamId.Value != null)
        {
            stopTasks.Add(ClientFunctions.StopCaptureAsync(sessionId, _activeScreenShareStreamId.Value));
        }

        await Task.WhenAll(stopTasks);

        // Remove from participants list
        var list = _participants.Value.Where(p => p.ClientSessionId != sessionId).ToList();
        _participants.Value = list;
        _participantsVersion.Value++;

        // Mark as left
        _hasLeft.Value = true;
    }

    private string GetParticipantGradient(string userId, bool isDarkMode)
    {
        var hash = userId.GetHashCode();
        var index = Math.Abs(hash) % LightModeGradients.Length;
        return isDarkMode ? DarkModeGradients[index] : LightModeGradients[index];
    }

    private string FormatTimeInClientTimezone(DateTime utcTimestamp)
    {
        var timezone = _clientTimezone.Value;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, tz);
            return $"{localTime:HH:mm}";
        }
        catch
        {
            return $"{utcTimestamp:HH:mm} (UTC)";
        }
    }

    private bool IsMobileLayout()
    {
        // Subscribe to the reactive clients dictionary to trigger re-renders on context changes
        _ = app.ReactiveGlobalState.Clients.Value;

        var clientScope = ReactiveScope.TryGet<ClientScope>();

        if (clientScope == null)
        {
            return false;
        }

        var context = app.GlobalState.GetClientContext(clientScope.Value.Id);

        if (context == null)
        {
            return _isMobile.Value;
        }

        // Use viewport width if available, otherwise fall back to device type detection
        if (context.ViewportWidth > 0)
        {
            return context.ViewportWidth < MobileLayoutBreakpoint;
        }

        return _isMobile.Value;
    }
}

internal class VideoStreamInfo(VideoCodec codec, int width, int height, double framerate, int clientSessionId, int inputTrackId)
{
    public VideoCodec Codec { get; set; } = codec;
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;
    public double Framerate { get; set; } = framerate;
    public int ClientSessionId { get; } = clientSessionId;
    public int InputTrackId { get; } = inputTrackId;
    public bool OutputMappingRegistered { get; set; }
}

internal class AudioStreamState(int sampleRate, int channelCount, int clientSessionId)
{
    public int SampleRate { get; } = sampleRate;
    public int ChannelCount { get; } = channelCount;
    public int ClientSessionId { get; } = clientSessionId;
}

internal class ParticipantSpeechState : IDisposable
{
    public string ParticipantName { get; }
    public SpeechRecognizer Recognizer { get; }
    public SpeechRecognizerAdapter Adapter { get; }
    public Channel<float[]> AudioChannel { get; }
    public CancellationTokenSource Cts { get; }

    public ParticipantSpeechState(string participantName, SpeechRecognizerModel model)
    {
        ParticipantName = participantName;
        Recognizer = new SpeechRecognizer(model);
        Adapter = new SpeechRecognizerAdapter(Recognizer, new SpeechRecognizerAdapter.Config
        {
            Mode = SpeechRecognizerAdapter.Mode.SilenceTriggered,
            SilenceDuration = TimeSpan.FromMilliseconds(750),
            SilenceThreshold = 0.01f,
            MaxSpeechDuration = TimeSpan.FromSeconds(30)
        });
        AudioChannel = Channel.CreateUnbounded<float[]>();
        Cts = new CancellationTokenSource();
    }

    public void Dispose()
    {
        Cts.Cancel();
        Cts.Dispose();
        AudioChannel.Writer.TryComplete();
        Adapter.Dispose();
        Recognizer.Dispose();
    }
}

internal class SpeakingState
{
    public float EmaVolume { get; set; }
    public DateTime LastAudioTime { get; set; }
    public bool IsSpeaking { get; set; }
}

internal class SummaryResult
{
    public string Markdown { get; set; } = "";
}
