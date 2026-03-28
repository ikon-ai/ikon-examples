return await App.Run(args);

public record SessionIdentity(string UserId);

public record ClientParams;

public record ViewModeTransform(float Scale, float OffsetY);

public record Live2DModelConfig(
    string Name,
    string Path,
    ViewModeTransform FullBody,
    ViewModeTransform Portrait,
    ViewModeTransform Face,
    ViewModeTransform Mobile,
    ViewModeTransform MobileChat);

public enum ChatRole { User, Assistant }

public sealed record ChatMessage(ChatRole Role, Reactive<string> Content)
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public bool IsStructuredBrief { get; init; } = false;
    public Reactive<Translation?> TranslationValue { get; } = new(null);
    public Reactive<bool> IsTranslating { get; } = new(false);
    public Reactive<bool> ShowTranslation { get; } = new(false);
}

[App]
public partial class LearningApp(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new Ikon.Parallax.Themes.Default.Theme());
    private Audio Audio { get; set; } = new(app);

    // State machine
    internal LearningStateMachineManager States { get; private set; } = null!;
    internal Reactive<LearningState> CurrentStateReactive { get; } = new(LearningState.Null);

    // Viseme analyzer for lip sync
    private readonly VisemeAnalyzer _visemeAnalyzer = new();

    // Live2D state
    internal Reactive<bool> IsListening { get; } = new(false);
    private readonly Reactive<string> _currentExpression = new("");
    private readonly Reactive<string> _currentMotion = new("");
    internal Reactive<int> SelectedModelIndex { get; } = new(0);
    internal Reactive<int> ViewModeIndex { get; } = new(1);
    internal Reactive<int> SelectedVoiceIndex { get; } = new(0);

    // Chat state
    private readonly object _chatMessagesLock = new();
    internal List<ChatMessage> ChatMessages { get; } = [];
    internal Reactive<int> ChatMessagesVersion { get; } = new(0);
    internal Reactive<string> InputText { get; } = new("");
    internal Reactive<bool> IsRecording { get; } = new(false);
    private readonly Reactive<bool> _isProcessingMessage = new(false);

    // User state
    private readonly Reactive<UserState?> _userState = new(null);
    internal UserState? UserState => _userState.Value;
    internal Exercise? CurrentExercise { get; set; }
    internal Article? CurrentArticle { get; set; }

    // Translations
    private readonly Reactive<Translations> _translations = new(new Translations());
    internal Translations Translations => _translations.Value;

    // Audio playback state tracking
    private readonly Reactive<Dictionary<string, AudioPlaybackState>> _messageAudioStates = new([]);
    private string? _currentAudioMessageId = null;
    internal IReadOnlyDictionary<string, AudioPlaybackState> MessageAudioStates => _messageAudioStates.Value;

    // Settings panel visibility
    internal Reactive<bool> ShowSettingsPanel { get; } = new(false);

    // Theme selection
    internal Reactive<LearningTheme> SelectedTheme { get; } = new(LearningTheme.LakeBlue);

    // Content
    private Content _content = new([], [], [], []);
    internal Content Content => _content;

    // TTS state
    private readonly Reactive<SpeechGeneratorModel> _ttsModel = new(SpeechGeneratorModel.Eleven3);
    private readonly Reactive<string> _ttsVoice = new("");
    private readonly Reactive<bool> _ttsSpeaking = new(false);
    private SpeechGenerator? _speechGenerator;
    private CancellationTokenSource? _speechCts;
    private readonly object _speechLock = new();

    // Current language context (detected from conversation, used as hint for STT/TTS)
    private readonly Reactive<string> _currentLanguage = new(""); // Empty = auto-detect

    // STT state
    private readonly Reactive<SpeechRecognizerModel> _sttModel = new(SpeechRecognizerModel.WhisperLarge3Turbo);
    private readonly Dictionary<string, SttStreamState> _sttStreamStates = new();

    // Callback for custom transcription handling (used by scenario builder etc.)
    internal Action<string>? TranscriptionCallback { get; set; }

    // LLM Chat
    private BasicChat Chat { get; } = new(new AssetUri(AssetClass.EmbeddedFile, "Ikon/App/Examples/Learning/mind.shader"));

    private static readonly Live2DModelConfig[] AvailableModels =
    [
        new("Pina", "/models/pina/pina.model3.json",
            FullBody: new(1.00f, 0.00f),
            Portrait: new(1.00f, 0.00f),
            Face: new(1.35f, -0.15f),
            Mobile: new(1.50f, -0.20f),
            MobileChat: new(1.05f, 0.05f)),
        new("Haru", "/models/Haru/Haru.model3.json",
            FullBody: new(1.00f, 0.00f),
            Portrait: new(3.05f, -0.90f),
            Face: new(4.60f, -1.60f),
            Mobile: new(5.20f, -1.80f),
            MobileChat: new(3.80f, -1.20f)),
        new("Hibiki", "/models/hibiki/runtime/hibiki.model3.json",
            FullBody: new(1.00f, 0.00f),
            Portrait: new(2.25f, -0.60f),
            Face: new(3.80f, -1.30f),
            Mobile: new(4.20f, -1.45f),
            MobileChat: new(3.20f, -1.00f)),
        new("Hiyori", "/models/hiyori/runtime/hiyori_free_t08.model3.json",
            FullBody: new(1.00f, 0.05f),
            Portrait: new(2.50f, -0.65f),
            Face: new(3.70f, -1.10f),
            Mobile: new(4.10f, -1.25f),
            MobileChat: new(3.10f, -0.85f)),
        new("Mark", "/models/mark/runtime/mark_free_t04.model3.json",
            FullBody: new(1.35f, 0.00f),
            Portrait: new(1.90f, -0.20f),
            Face: new(2.60f, -0.30f),
            Mobile: new(2.90f, -0.40f),
            MobileChat: new(2.20f, -0.15f))
    ];

    private static readonly (string Name, string ViewMode)[] ViewModes =
    [
        ("Full Body", "fullBody"),
        ("Portrait", "portrait"),
        ("Face", "face")
    ];

    // Voice configuration record
    internal record VoiceConfig(string Name, string VoiceId, SpeechGeneratorModel Model, string Provider);

    // Voice options grouped by provider
    internal static readonly VoiceConfig[] AvailableVoices =
    [
        // ElevenLabs voices (Eleven3 model - best quality)
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

        // OpenAI voices (GPT-4o Mini TTS - expressive)
        new("Alloy", "alloy", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Echo", "echo", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Fable", "fable", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Onyx", "onyx", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Nova", "nova", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),
        new("Shimmer", "shimmer", SpeechGeneratorModel.Gpt4OmniMiniTts, "OpenAI"),

        // Azure voices (Multilingual Neural)
        new("Adam", "en-US-AdamMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
        new("Emma", "en-US-EmmaMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
        new("Brian", "en-US-BrianMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
        new("Ava", "en-US-AvaMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
        new("Andrew", "en-US-AndrewMultilingualNeural", SpeechGeneratorModel.AzureSpeechService, "Azure"),
    ];

    public async Task Main()
    {
        States = new LearningStateMachineManager(this);

        app.StoppingAsync += async _ =>
        {
            StopSpeaking();
            _speechCts?.Dispose();
            await SaveUserStateAsync();
            await Chat.DisposeAsync();
        };

        SetupAudioInputHandlers();
        await LoadUserStateAsync();
        LoadTranslations();

        // Set default voice and subscribe to changes
        UpdateVoiceFromIndex(SelectedVoiceIndex.Value);
        SelectedVoiceIndex.ValueChanged += UpdateVoiceFromIndex;

        // Subscribe to settings changes to persist them
        SelectedVoiceIndex.ValueChanged += idx => SaveSettingToUserState(s => s.SelectedVoiceIndex = idx);
        SelectedModelIndex.ValueChanged += idx => SaveSettingToUserState(s => s.SelectedCharacterIndex = idx);
        ViewModeIndex.ValueChanged += idx => SaveSettingToUserState(s => s.SelectedViewModeIndex = idx);

        RenderUI();

        await States.StateMachine.FireAsync(Trigger.ReturnToMainMenu);
    }

    private void UpdateVoiceFromIndex(int index)
    {
        if (index >= 0 && index < AvailableVoices.Length)
        {
            var voice = AvailableVoices[index];
            _ttsVoice.Value = voice.VoiceId;
            _ttsModel.Value = voice.Model;
        }
    }

    private void LoadTranslations()
    {
        var userStateSnapshot = _userState.Value;

        if (userStateSnapshot == null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            var preferredLang = userStateSnapshot.PreferredLanguage;

            if (preferredLang == "en-US" || preferredLang == "en-GB")
            {
                _translations.Value = new Translations();
                return;
            }

            var translationsAssetUri = new AssetUri(
                AssetClass.CloudFile,
                $"translations/{preferredLang}",
                spaceId: app.GlobalState.SpaceId,
                channelId: app.GlobalState.ChannelId
            );

            try
            {
                if (await Asset.Instance.ExistsAsync(translationsAssetUri))
                {
                    var result = await Asset.Instance.GetWithMetadataAsync<Translations>(translationsAssetUri);
                    _translations.Value = result.Content;
                    Log.Instance.Info($"Loaded translations for {preferredLang}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Warning($"Failed to load translations for {preferredLang}: {ex.Message}");
            }

            try
            {
                Log.Instance.Info($"Generating translations for {preferredLang}");

                var translations = await UpdateTranslationsShader.GenerateAsync(
                    LLMModel.Gpt41Mini.ToString(),
                    nameof(ReasoningEffort.None),
                    preferredLang
                );

                await Asset.Instance.SetAsync(translationsAssetUri, translations);
                _translations.Value = translations;

                Log.Instance.Info($"Generated and saved translations for {preferredLang}");
            }
            catch (Exception ex)
            {
                Log.Instance.Warning($"Failed to generate translations for {preferredLang}: {ex.Message}");
                _translations.Value = new Translations();
            }
        });
    }

    private readonly Dictionary<string, string> _imageCache = new();

    private async Task LoadUserStateAsync()
    {
        try
        {
            var uri = new AssetUri(AssetClass.CloudFile, "user_state", app.GlobalState.SpaceId, app.GlobalState.ChannelId, app.ClientContext.UserId);

            if (await Asset.Instance.ExistsAsync(uri))
            {
                var result = await Asset.Instance.GetWithMetadataAsync<UserState>(uri);
                _userState.Value = result.Content;
                UpdateStreak();
                LoadSettingsFromUserState();
            }
            else
            {
                _userState.Value = CreateDefaultUserState();
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error loading user state: {ex.Message}");
            _userState.Value = CreateDefaultUserState();
        }
    }

    private UserState CreateDefaultUserState()
    {
        return new UserState
        {
            UserId = app.ClientContext.UserId,
            TargetLanguage = "Finnish",
            PreferredLanguage = "en-US",
            CurrentLanguageLevel = "A2.2",
            TargetLanguageLevel = "B1.1",
            LastActivityDate = DateTime.UtcNow
        };
    }

    private void LoadSettingsFromUserState()
    {
        if (_userState.Value == null)
        {
            return;
        }

        // Load theme
        SelectedTheme.Value = _userState.Value.Theme switch
        {
            "PineGreen" => LearningTheme.PineGreen,
            "NordicTeal" => LearningTheme.NordicTeal,
            _ => LearningTheme.LakeBlue
        };

        // Load character selection (with bounds check)
        if (_userState.Value.SelectedCharacterIndex >= 0 && _userState.Value.SelectedCharacterIndex < AvailableModels.Length)
        {
            SelectedModelIndex.Value = _userState.Value.SelectedCharacterIndex;
        }

        // Load voice selection (with bounds check)
        if (_userState.Value.SelectedVoiceIndex >= 0 && _userState.Value.SelectedVoiceIndex < AvailableVoices.Length)
        {
            SelectedVoiceIndex.Value = _userState.Value.SelectedVoiceIndex;
        }

        // Load view mode selection (with bounds check)
        if (_userState.Value.SelectedViewModeIndex >= 0 && _userState.Value.SelectedViewModeIndex < ViewModes.Length)
        {
            ViewModeIndex.Value = _userState.Value.SelectedViewModeIndex;
        }
    }

    internal void SetTheme(LearningTheme theme)
    {
        SelectedTheme.Value = theme;

        if (_userState.Value != null)
        {
            _userState.Value.Theme = theme switch
            {
                LearningTheme.PineGreen => "PineGreen",
                LearningTheme.NordicTeal => "NordicTeal",
                _ => "LakeBlue"
            };

            _ = SaveUserStateAsync();
        }
    }

    private void SaveSettingToUserState(Action<UserState> updateAction)
    {
        if (_userState.Value == null)
        {
            return;
        }

        updateAction(_userState.Value);
        _ = SaveUserStateAsync();
    }

    internal async Task SaveUserStateAsync()
    {
        if (_userState.Value == null)
        {
            return;
        }

        try
        {
            _userState.Value.LastActivityDate = DateTime.UtcNow;
            var uri = new AssetUri(AssetClass.CloudFile, "user_state", app.GlobalState.SpaceId, app.GlobalState.ChannelId, app.ClientContext.UserId);
            await Asset.Instance.SetAsync(uri, _userState.Value);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error saving user state: {ex.Message}");
        }
    }

    private void UpdateStreak()
    {
        if (_userState.Value == null)
        {
            return;
        }

        var today = DateTime.UtcNow.Date;
        var lastActivity = _userState.Value.LastActivityDate.Date;

        if (lastActivity == today)
        {
            return;
        }

        if (lastActivity == today.AddDays(-1))
        {
            _userState.Value.CurrentStreak++;
        }
        else if (lastActivity < today.AddDays(-1))
        {
            _userState.Value.CurrentStreak = 1;
        }
    }

    internal async Task CheckAndAwardAchievementsAsync()
    {
        if (_userState.Value == null)
        {
            return;
        }

        var newAchievements = new List<Achievement>();
        var state = _userState.Value;

        if (!HasAchievement(Achievement.FirstStep) && state.ExerciseHistory.Count >= 1)
        {
            newAchievements.Add(Achievement.FirstStep);
        }

        if (!HasAchievement(Achievement.Perfectionist) && state.ExerciseHistory.Any(e => e.Score == 100))
        {
            newAchievements.Add(Achievement.Perfectionist);
        }

        if (!HasAchievement(Achievement.DailyGrind) && state.CurrentStreak >= 7)
        {
            newAchievements.Add(Achievement.DailyGrind);
        }

        if (!HasAchievement(Achievement.Devoted) && state.CurrentStreak >= 30)
        {
            newAchievements.Add(Achievement.Devoted);
        }

        if (!HasAchievement(Achievement.SuperLearner) && state.ExerciseHistory.Count >= 50)
        {
            newAchievements.Add(Achievement.SuperLearner);
        }

        if (!HasAchievement(Achievement.Master) && state.ExerciseHistory.Count >= 100)
        {
            newAchievements.Add(Achievement.Master);
        }

        if (!HasAchievement(Achievement.Marathon) && state.ExerciseHistory.Count(e => e.CompletedAt.Date == DateTime.UtcNow.Date) >= 10)
        {
            newAchievements.Add(Achievement.Marathon);
        }

        if (!HasAchievement(Achievement.Creator) && state.CreatedExercises.Count >= 1)
        {
            newAchievements.Add(Achievement.Creator);
        }

        if (!HasAchievement(Achievement.MasterCreator) && state.CreatedExercises.Count >= 10)
        {
            newAchievements.Add(Achievement.MasterCreator);
        }

        var hour = DateTime.Now.Hour;

        if (!HasAchievement(Achievement.EarlyBird) && hour >= 5 && hour < 8)
        {
            newAchievements.Add(Achievement.EarlyBird);
        }

        if (!HasAchievement(Achievement.NightOwl) && (hour >= 23 || hour < 4))
        {
            newAchievements.Add(Achievement.NightOwl);
        }

        var newsExercises = state.ExerciseHistory.Count(e =>
            state.CreatedExercises.Any(ce => ce.Id == e.ExerciseId && ce.Source == ExerciseSource.News));

        if (!HasAchievement(Achievement.UpToDate) && newsExercises >= 1)
        {
            newAchievements.Add(Achievement.UpToDate);
        }

        if (!HasAchievement(Achievement.Reporter) && newsExercises >= 10)
        {
            newAchievements.Add(Achievement.Reporter);
        }

        CheckLevelAchievements(state, newAchievements);

        foreach (var achievement in newAchievements)
        {
            state.Achievements.Add(new AchievementRecord
            {
                Achievement = achievement,
                Timestamp = DateTime.UtcNow
            });

            state.TotalPoints += 50;
        }

        if (newAchievements.Count > 0)
        {
            await SaveUserStateAsync();
        }
    }

    private void CheckLevelAchievements(UserState state, List<Achievement> newAchievements)
    {
        var levelAchievementMap = new Dictionary<string, Achievement>
        {
            { "A1.1", Achievement.A1_1 },
            { "A1.2", Achievement.A1_2 },
            { "A1.3", Achievement.A1_3 },
            { "A2.1", Achievement.A2_1 },
            { "A2.2", Achievement.A2_2 },
            { "B1.1", Achievement.B1_1 }
        };

        if (levelAchievementMap.TryGetValue(state.CurrentLanguageLevel, out var achievement) && !HasAchievement(achievement))
        {
            newAchievements.Add(achievement);
        }
    }

    private bool HasAchievement(Achievement achievement)
    {
        return _userState.Value?.Achievements.Any(a => a.Achievement == achievement) ?? false;
    }

    internal async Task<string> GetOrCreateImageAsync(string prefix, string id, string description, int width = 1024, int height = 1024, string? defaultUrl = null)
    {
        var cacheKey = $"{prefix}/{id}";

        if (_imageCache.TryGetValue(cacheKey, out var cachedUrl) && !string.IsNullOrEmpty(cachedUrl))
        {
            return cachedUrl;
        }

        try
        {
            var assetUri = new AssetUri(AssetClass.CloudFilePublic, $"{prefix}/generated-image-v2-{id}.jpg", app.GlobalState.SpaceId, app.GlobalState.ChannelId);
            var meta = await Asset.Instance.TryGetMetadataAsync(assetUri);

            if (meta != null && !string.IsNullOrEmpty(meta.Value.Url))
            {
                Log.Instance.Info($"[GetOrCreateImageAsync] Found cached image for {cacheKey}: {meta.Value.Url}");
                _imageCache[cacheKey] = meta.Value.Url;
                return meta.Value.Url;
            }

            Log.Instance.Info($"[GetOrCreateImageAsync] Generating new image for {cacheKey}");

            var prompt = await CreateImageGeneratorPromptShader.GenerateAsync(
                LLMModel.Gpt41Mini.ToString(),
                nameof(ReasoningEffort.None),
                description,
                prefix
            );

            Log.Instance.Info($"[GetOrCreateImageAsync] Generated prompt: {prompt.Prompt}");

            var generator = new ImageGenerator(ImageGeneratorModel.Flux1KontextPro);
            var images = await generator.GenerateImageAsync(new ImageGeneratorConfig
            {
                Prompt = prompt.Prompt,
                Width = width,
                Height = height
            });

            if (images.Count == 0 || images[0].Data == null || images[0].Data.Length < 1000)
            {
                Log.Instance.Warning($"[GetOrCreateImageAsync] No valid images generated for {cacheKey} (count={images.Count}, size={images.FirstOrDefault()?.Data?.Length ?? 0})");
                return defaultUrl ?? string.Empty;
            }

            Log.Instance.Info($"[GetOrCreateImageAsync] Image generated ({images[0].Data.Length} bytes), saving to assets for {cacheKey}");

            await Asset.Instance.SetAsync(assetUri, images[0].Data);
            var savedMeta = await Asset.Instance.GetMetadataAsync(assetUri);

            if (string.IsNullOrEmpty(savedMeta.Url))
            {
                Log.Instance.Warning($"[GetOrCreateImageAsync] Saved image but URL is empty for {cacheKey}");
                return defaultUrl ?? string.Empty;
            }

            Log.Instance.Info($"[GetOrCreateImageAsync] Image saved successfully: {savedMeta.Url}");
            _imageCache[cacheKey] = savedMeta.Url;
            return savedMeta.Url;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"[GetOrCreateImageAsync] Error generating image for {cacheKey}: {ex.Message}");
            return defaultUrl ?? string.Empty;
        }
    }

    internal async Task<string> GetOrFetchNetworkImageAsync(string prefix, string id, string url)
    {
        var cacheKey = $"{prefix}/{id}";

        if (_imageCache.TryGetValue(cacheKey, out var cachedUrl))
        {
            return cachedUrl;
        }

        try
        {
            var assetUri = new AssetUri(AssetClass.CloudFilePublic, $"{prefix}/fetched-image-{id}.jpg", app.GlobalState.SpaceId, app.GlobalState.ChannelId);
            var meta = await Asset.Instance.TryGetMetadataAsync(assetUri);

            if (meta != null)
            {
                _imageCache[cacheKey] = meta.Value.Url!;
                return meta.Value.Url!;
            }

            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(url);

            await Asset.Instance.SetAsync(assetUri, imageBytes);
            var savedMeta = await Asset.Instance.GetMetadataAsync(assetUri);
            _imageCache[cacheKey] = savedMeta.Url!;
            return savedMeta.Url!;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error fetching network image: {ex.Message}");
            return url;
        }
    }

    // News caching - cache for the current day
    internal async Task<News?> GetCachedNewsAsync()
    {
        try
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var assetUri = new AssetUri(AssetClass.CloudFile, $"news/daily-news-{today}.json", app.GlobalState.SpaceId, app.GlobalState.ChannelId);

            if (await Asset.Instance.ExistsAsync(assetUri))
            {
                var result = await Asset.Instance.GetWithMetadataAsync<News>(assetUri);
                return result.Content;
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Error loading cached news: {ex.Message}");
            return null;
        }
    }

    internal async Task SaveNewsToCacheAsync(News news)
    {
        try
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var assetUri = new AssetUri(AssetClass.CloudFile, $"news/daily-news-{today}.json", app.GlobalState.SpaceId, app.GlobalState.ChannelId);
            await Asset.Instance.SetAsync(assetUri, news);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Error saving news to cache: {ex.Message}");
        }
    }

    // Theme scenarios caching
    internal async Task<List<Scenario>> GetThemeScenariosAsync(string themeId)
    {
        try
        {
            var assetUri = new AssetUri(AssetClass.CloudFile, $"themes/{themeId}/scenarios.json", app.GlobalState.SpaceId, app.GlobalState.ChannelId);

            if (await Asset.Instance.ExistsAsync(assetUri))
            {
                var result = await Asset.Instance.GetWithMetadataAsync<List<Scenario>>(assetUri);
                return result.Content ?? [];
            }

            return [];
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Error loading scenarios for theme {themeId}: {ex.Message}");
            return [];
        }
    }

    internal async Task SaveThemeScenariosAsync(string themeId, List<Scenario> scenarios)
    {
        try
        {
            var assetUri = new AssetUri(AssetClass.CloudFile, $"themes/{themeId}/scenarios.json", app.GlobalState.SpaceId, app.GlobalState.ChannelId);
            await Asset.Instance.SetAsync(assetUri, scenarios);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Error saving scenarios for theme {themeId}: {ex.Message}");
        }
    }

    // News exercise caching
    internal async Task<Exercise?> GetNewsExerciseAsync(string articleId)
    {
        try
        {
            var assetUri = new AssetUri(AssetClass.CloudFile, $"news/exercises/{articleId}.json", app.GlobalState.SpaceId, app.GlobalState.ChannelId);

            if (await Asset.Instance.ExistsAsync(assetUri))
            {
                var result = await Asset.Instance.GetWithMetadataAsync<Exercise>(assetUri);
                return result.Content;
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Error loading news exercise {articleId}: {ex.Message}");
            return null;
        }
    }

    internal async Task SaveNewsExerciseAsync(string articleId, Exercise exercise)
    {
        try
        {
            var assetUri = new AssetUri(AssetClass.CloudFile, $"news/exercises/{articleId}.json", app.GlobalState.SpaceId, app.GlobalState.ChannelId);
            await Asset.Instance.SetAsync(assetUri, exercise);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Error saving news exercise {articleId}: {ex.Message}");
        }
    }

    internal void AddChatMessage(ChatRole role, string content, bool isStructuredBrief = false)
    {
        var message = new ChatMessage(role, new Reactive<string>(content)) { IsStructuredBrief = isStructuredBrief };
        lock (_chatMessagesLock)
        {
            ChatMessages.Add(message);
            ChatMessagesVersion.Value++;
        }
    }

    internal async Task ProcessUserMessageAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        InterruptSpeaking();
        _isProcessingMessage.Value = true;

        try
        {
            // Set exercise context if in exercise mode
            UpdateChatContext();

            Chat.AddUserMessage(text);
            var reply = await Chat.GenerateStringAsync();
            Chat.AddModelMessage(reply);

            // Detect language from response and update current language hint
            _currentLanguage.Value = DetectLanguageFromText(reply);

            await States.CurrentState.HandleAIMessageAsync(reply);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error processing message: {ex}");
            AddChatMessage(ChatRole.Assistant, "Sorry, I encountered an error. Please try again.");
        }
        finally
        {
            _isProcessingMessage.Value = false;
        }
    }

    private void UpdateChatContext()
    {
        var exercise = CurrentExercise;
        var article = CurrentArticle;

        if (exercise != null)
        {
            Chat.SetState("ExerciseMode", true);
            Chat.SetState("ExerciseScenario", exercise.Scenario ?? "");
            Chat.SetState("ExerciseAIRole", exercise.Roles?.AI ?? "Language tutor");
            Chat.SetState("ExerciseUserRole", exercise.Roles?.User?.Role ?? "Language learner");

            // Build goals string
            var goals = exercise.Roles?.User?.SubGoals?
                .Where(g => !g.Optional)
                .Select(g => g.Description)
                .ToList();
            Chat.SetState("ExerciseGoals", goals != null && goals.Count > 0 ? string.Join("; ", goals) : "");

            // Set article content if this is a news exercise
            if (article != null)
            {
                Chat.SetState("ArticleContent", $"Title: {article.Title}\n\n{article.Content}");
            }
            else
            {
                Chat.SetState("ArticleContent", "");
            }
        }
        else
        {
            // Free chat mode - clear exercise context
            Chat.SetState("ExerciseMode", false);
            Chat.SetState("ExerciseScenario", "");
            Chat.SetState("ExerciseAIRole", "");
            Chat.SetState("ExerciseUserRole", "");
            Chat.SetState("ExerciseGoals", "");
            Chat.SetState("ArticleContent", "");
        }
    }

    internal async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        CancellationToken cancellationToken;
        SpeechGenerator generator;

        lock (_speechLock)
        {
            _speechCts?.Cancel();
            _speechCts?.Dispose();
            _speechCts = new CancellationTokenSource();
            cancellationToken = _speechCts.Token;

            // Don't dispose the old generator synchronously - the async operation may still be
            // using it. The cancellation token will cause it to stop, and it will be GC'd after.
            _speechGenerator = new SpeechGenerator(_ttsModel.Value);
            generator = _speechGenerator;

            _ttsSpeaking.Value = true;
        }

        try
        {
            var config = new SpeechGeneratorConfig
            {
                Text = text,
                VoiceId = _ttsVoice.Value,
                Language = _currentLanguage.Value
            };

            var analyzers = new IAudioAnalyzer[] { _visemeAnalyzer };

            await foreach (var audio in generator.GenerateSpeechAsync(config).WithCancellation(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                Audio.SendSpeech(audio, [], analyzers);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Speech error: {ex.Message}");
        }
        finally
        {
            lock (_speechLock)
            {
                _ttsSpeaking.Value = false;
            }
        }
    }

    private void StopSpeaking()
    {
        lock (_speechLock)
        {
            _speechCts?.Cancel();
            _speechCts?.Dispose();
            _speechCts = null;

            // Don't dispose the generator synchronously - the async operation may still be
            // using it. The cancellation token will cause it to stop, and it will be GC'd after.
            _speechGenerator = null;

            _ttsSpeaking.Value = false;
        }
    }

    internal void InterruptSpeaking()
    {
        Audio.SpeechMixer.FadeOut();
        StopSpeaking();
    }

    internal AudioPlaybackState GetMessageAudioState(string messageId)
    {
        if (_messageAudioStates.Value.TryGetValue(messageId, out var state))
        {
            return state;
        }

        return AudioPlaybackState.Stopped;
    }

    internal void SetMessageAudioState(string messageId, AudioPlaybackState state)
    {
        var states = new Dictionary<string, AudioPlaybackState>(_messageAudioStates.Value)
        {
            [messageId] = state
        };
        _messageAudioStates.Value = states;
    }

    internal async Task PlayMessageAudioAsync(string messageId, string text)
    {
        if (_currentAudioMessageId != null && _currentAudioMessageId != messageId)
        {
            SetMessageAudioState(_currentAudioMessageId, AudioPlaybackState.Stopped);
        }

        var currentState = GetMessageAudioState(messageId);

        switch (currentState)
        {
            case AudioPlaybackState.Stopped:
                _currentAudioMessageId = messageId;
                SetMessageAudioState(messageId, AudioPlaybackState.Playing);
                await SpeakAsync(text);
                SetMessageAudioState(messageId, AudioPlaybackState.Stopped);
                _currentAudioMessageId = null;
                break;

            case AudioPlaybackState.Playing:
                StopSpeaking();
                SetMessageAudioState(messageId, AudioPlaybackState.Stopped);
                _currentAudioMessageId = null;
                break;

            case AudioPlaybackState.Paused:
                _currentAudioMessageId = messageId;
                SetMessageAudioState(messageId, AudioPlaybackState.Playing);
                await SpeakAsync(text);
                SetMessageAudioState(messageId, AudioPlaybackState.Stopped);
                _currentAudioMessageId = null;
                break;
        }
    }

    internal string GetAudioButtonLabel(string messageId)
    {
        var state = GetMessageAudioState(messageId);
        return state switch
        {
            AudioPlaybackState.Playing => "Stop",
            AudioPlaybackState.Paused => "Resume",
            _ => "Play"
        };
    }

    /// <summary>
    /// Detects language from text based on character analysis.
    /// Returns ISO 639-1 language code or empty string for auto-detect.
    /// </summary>
    private static string DetectLanguageFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        int koreanCount = 0;
        int japaneseCount = 0;
        int chineseCount = 0;
        int cyrillicCount = 0;
        int latinCount = 0;
        int totalLetters = 0;

        foreach (char c in text)
        {
            if (char.IsLetter(c))
            {
                totalLetters++;

                // Korean Hangul
                if (c >= 0xAC00 && c <= 0xD7AF || c >= 0x1100 && c <= 0x11FF)
                {
                    koreanCount++;
                }
                // Japanese Hiragana and Katakana
                else if (c >= 0x3040 && c <= 0x309F || c >= 0x30A0 && c <= 0x30FF)
                {
                    japaneseCount++;
                }
                // CJK (Chinese characters, also used in Japanese)
                else if (c >= 0x4E00 && c <= 0x9FFF)
                {
                    chineseCount++;
                }
                // Cyrillic
                else if (c >= 0x0400 && c <= 0x04FF)
                {
                    cyrillicCount++;
                }
                // Latin
                else if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z')
                {
                    latinCount++;
                }
            }
        }

        if (totalLetters == 0)
        {
            return "";
        }

        // Detect by dominant script (threshold: 30% of letters)
        float threshold = totalLetters * 0.3f;

        if (koreanCount > threshold)
        {
            return "ko";
        }

        if (japaneseCount > threshold || (japaneseCount > 0 && chineseCount > 0 && japaneseCount + chineseCount > threshold))
        {
            return "ja";
        }

        if (chineseCount > threshold)
        {
            return "zh";
        }

        if (cyrillicCount > threshold)
        {
            return "ru";
        }

        // For Latin scripts, return empty to let TTS auto-detect (Finnish, English, etc.)
        return "";
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

    public void Reset()
    {
        lock (_lock)
        {
            _samples.Clear();
            _channel.Writer.TryComplete();
            _channel = System.Threading.Channels.Channel.CreateUnbounded<float[]>();
            EffectInstances = null;
        }
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
