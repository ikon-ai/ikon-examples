return await App.Run(args);

public record SessionIdentity(string? UserId, string Id);
public record ClientParams(string Id, string Test);

[App]
public partial class Validation(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, ValidationTheme.Brand) { EnableProfiling = false, EnableSubtreeCaching = true, EnableSubtreeRendering = true };
    private Audio Audio { get; set; } = new(app);
    private Video Video { get; } = new(app);
    private AudioGenerator AudioGenerator { get; } = new();

    // Tab state — per-client so concurrent sessions don't trample each other's
    // active tab via the shared-Reactive write path.
    private readonly ClientReactive<string> _activeTab = new("typography");

    private static readonly HashSet<string> ValidTabs =
    [
        "buttons", "inputs", "advanced-inputs", "typography", "icons", "cards",
        "layout", "forms", "navigation", "nav-menu", "overlays", "drag-drop", "pan-zoom",
        "crosswind", "brand",
        "charts",
        "files", "assets", "actions", "notifications",
        "video", "audio", "shadertoy",
        "ikon-ai", "mcp", "cells", "cron",
        "virtualization", "drawing",
        "profiling", "memory", "session-identity", "account", "react-sdk", "consent",
        "payments", "email", "costs", "custom-messages", "database", "persistent-state",
        "self-test", "device", "telephony", "signatures", "sharepoint", "google-drive"
    ];

    // Input states
    private readonly Reactive<string> _textFieldValue = new("");
    private readonly Reactive<string> _successStateValue = new("valid@email.com");
    private readonly Reactive<string> _warningStateValue = new("user123");
    private readonly Reactive<string> _errorStateValue = new("invalid");
    private readonly Reactive<string> _formVerifiedValue = new("valid@email.com");
    private readonly Reactive<string> _formUsernameValue = new("user123");
    private readonly Reactive<CheckedState> _triStateChecked = new(CheckedState.Indeterminate);
    private readonly Reactive<string> _lastUploadedFileName = new("");
    private readonly Reactive<string> _textAreaValue = new("");
    private readonly Reactive<int> _autoResizePlaygroundRows = new(1);
    private readonly Reactive<int> _autoResizePlaygroundMaxRows = new(6);
    private readonly Reactive<bool> _autoResizePlaygroundMaxRowsDefined = new(true);
    private readonly Reactive<bool> _autoResizePlaygroundEnabled = new(true);
    private readonly Reactive<string> _selectValue = new("");
    private readonly Reactive<bool> _checkboxChecked = new(false);
    private readonly Reactive<string> _checkboxIndeterminate = new("unchecked");
    private readonly Reactive<bool> _switchChecked = new(false);
    private readonly Reactive<string> _radioValue = new("option1");
    private readonly Reactive<double> _sliderValue = new(50);
    private readonly Reactive<double> _panZoomScale = new(1);
    private readonly Reactive<bool> _togglePressed = new(false);
    private readonly Reactive<string> _toggleGroupSingleValue = new("center");
    private readonly Reactive<IReadOnlyList<string>> _toggleGroupValues = new([]);

    // Dialog states
    private readonly Reactive<bool> _dialogOpen = new(false);
    private readonly Reactive<bool> _alertDialogOpen = new(false);
    private readonly Reactive<bool> _popoverOpen = new(false);
    private readonly Reactive<bool> _toastOpen = new(false);

    // Accordion/Collapsible states
    private readonly Reactive<string> _accordionValue = new("");
    private readonly Reactive<IReadOnlyList<string>> _accordionMultipleValues = new([]);
    private readonly Reactive<bool> _collapsibleOpen = new(false);

    // Progress demo
    private readonly Reactive<double> _progressValue = new(60);

    // Nested tabs for navigation demo
    private readonly Reactive<string> _nestedTabValue = new("nested1");

    // Crosswind sub-tabs
    private readonly Reactive<string> _crosswindSubTab = new("retro");

    // File upload states
    private readonly Reactive<string> _basicUploadStatus = new("");
    private readonly Reactive<string> _multiUploadStatus = new("");
    private readonly Reactive<string> _zoneUploadStatus = new("");
    private readonly Reactive<string> _imagesUploadStatus = new("");
    private readonly Reactive<string> _pdfsUploadStatus = new("");
    private readonly Reactive<string> _codeUploadStatus = new("");

    // Advanced file upload states
    private readonly Reactive<string> _advUploadMode = new("local");
    private readonly Reactive<bool> _advUploadRejectAll = new(false);
    private readonly Reactive<string> _advUploadInitStatus = new("");
    private readonly Reactive<string> _advUploadStartStatus = new("");
    private readonly Reactive<string> _advUploadProgressStatus = new("");
    private readonly Reactive<string> _advUploadCompleteStatus = new("");
    private readonly Reactive<string> _advUploadErrorStatus = new("");
    private readonly Reactive<double> _advUploadProgress = new(0);
    private readonly Reactive<string?> _advUploadAssetUrl = new(null);

    // The panel shows one upload's callbacks at a time; hooks of different uploads run in parallel, so
    // an upload that a newer one superseded must stop writing to it.
    private readonly Lock _advUploadLock = new();
    private string? _advUploadCurrentId;

    // ActionButton/ClientFunction states
    private readonly Reactive<string> _clientFunctionResultText = new("(no function called)");
    private readonly Reactive<bool> _clientFunctionToastOpen = new(false);

    // Drag and drop states
    private readonly Reactive<string> _dndStatus = new("");
    private readonly Reactive<string> _activeDragId = new("");
    private readonly Reactive<IReadOnlyList<string>> _sortableHandleCards = new(["Card A", "Card B", "Card C", "Card D"]);
    private readonly Reactive<IReadOnlyList<string>> _sortableClickThroughList = new(["Item 1", "Item 2", "Item 3", "Item 4"]);
    private readonly ReactiveDictionary<string, string?> _draggableItemZones = new(
        new Dictionary<string, string?>
        {
            { "drag-1", null },
            { "drag-2", null },
            { "drag-disabled", null }
        });
    private readonly ReactiveDictionary<string, string?> _overlayItemZones = new(
        new Dictionary<string, string?>
        {
            { "overlay-item-1", null },
            { "overlay-item-2", null }
        });

    // NavigationMenu states
    private readonly Reactive<string> _navMenuValue = new("");
    private readonly Reactive<string> _navMenuStatus = new("");

    // Additional input states
    private readonly Reactive<string> _numericIntValue = new("50");
    private readonly Reactive<string> _numericDecimalValue = new("3.14");
    private readonly Reactive<string> _radioHorizontalValue = new("h-opt1");
    private readonly Reactive<double> _sliderVerticalValue = new(50);
    private readonly Reactive<double> _sliderInvertedValue = new(30);
    private readonly Reactive<IReadOnlyList<double>> _sliderRangeValues = new([25.0, 75.0]);

    // Additional navigation states
    private readonly Reactive<string> _verticalTabValue = new("vtab1");
    private readonly Reactive<string> _manualTabValue = new("manual1");
    private readonly Reactive<int> _paginationPage = new(3);
    private readonly Reactive<IReadOnlyList<string>> _breadcrumbPath = new(["Home", "Products", "Electronics"]);

    // Server-side validation states
    private readonly Reactive<string> _serverEmailValue = new("taken@example.com");
    private readonly Reactive<bool> _serverEmailInvalid = new(true);
    private readonly Reactive<string> _serverUsernameValue = new("admin");
    private readonly Reactive<bool> _serverUsernameInvalid = new(true);

    // Theme state
    private ThemeControl _theme = null!;

    // Video capture state
    private readonly Reactive<bool> _isCameraCaptureActive = new(false);
    private readonly Reactive<bool> _isScreenCaptureActive = new(false);

    // Video echo state. The surface names start with the source type because
    // validation/validate_platform_current.py finds the echoed camera by the "camera_" prefix.
    private const string CameraEchoSurface = "camera_echo";
    private const string ScreenEchoSurface = "screen_echo";
    private volatile VideoEcho? _cameraEcho;
    private volatile VideoEcho? _screenEcho;

    // Video UI state
    private readonly Reactive<string?> _cameraEchoSurface = new(null);
    private readonly Reactive<string> _cameraEchoStatus = new("");
    private readonly Reactive<int> _cameraWidth = new(0);
    private readonly Reactive<int> _cameraHeight = new(0);
    private readonly Reactive<string?> _screenEchoSurface = new(null);
    private readonly Reactive<string> _screenEchoStatus = new("");
    private readonly Reactive<int> _screenWidth = new(0);
    private readonly Reactive<int> _screenHeight = new(0);

    // Camera capture options
    private readonly Reactive<string> _cameraCodec = new("h264");
    private readonly Reactive<string> _cameraResolution = new("720p");
    private readonly Reactive<string> _cameraBitrate = new("5");
    private readonly Reactive<string> _cameraFramerate = new("30");

    // Screen capture options
    private readonly Reactive<string> _screenCodec = new("h264");
    private readonly Reactive<string> _screenBitrate = new("5");
    private readonly Reactive<string> _screenFramerate = new("30");

    // Device enumeration
    private readonly Reactive<IReadOnlyList<ClientMediaDevice>> _availableDevices = new([]);
    private readonly Reactive<bool> _devicesLoading = new(false);

    // Camera device selection
    private readonly Reactive<string> _selectedCameraId = new("default");
    private readonly Reactive<string> _selectedImageCameraId = new("default");
    private readonly Reactive<string> _selectedImageFacing = new("auto");

    // Microphone device selection
    private readonly Reactive<string> _selectedMicrophoneId = new("default");

    // Audio capture options
    private readonly Reactive<bool> _audioEchoCancellation = new(false);
    private readonly Reactive<bool> _audioNoiseSuppression = new(true);
    private readonly Reactive<bool> _audioAutoGainControl = new(true);
    private readonly Reactive<string> _audioBitrate = new("32");

    // Image capture state
    private readonly Reactive<string?> _capturedImageData = new(null);
    private readonly Reactive<string?> _capturedImageMime = new(null);
    private readonly Reactive<int> _capturedImageWidth = new(0);
    private readonly Reactive<int> _capturedImageHeight = new(0);

    // Video URL player state
    private readonly Reactive<string> _videoUrl = new("https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8");
    private readonly Reactive<bool> _videoUrlLoop = new(true);
    private readonly Reactive<bool> _videoUrlMuted = new(false);
    private readonly Reactive<bool> _videoUrlControls = new(true);

    // Audio source tracking
    private readonly ReactiveList<string> _sineWaveIds = new();
    private readonly ReactiveList<string> _drumMachineIds = new();
    private readonly ReactiveList<string> _moogSynthIds = new();

    // Audio metrics
    private readonly Reactive<int> _audioStreamCount = new(0);
    private readonly Reactive<double> _audioMinIpdMs = new(0);
    private readonly Reactive<double> _audioAvgIpdMs = new(0);
    private readonly Reactive<double> _audioMaxIpdMs = new(0);
    private readonly Reactive<double> _audioJitterMs = new(0);
    private readonly Reactive<double> _audioCpuUsagePercent = new(0);

    // Synth state
    private readonly Reactive<string> _currentPatch = new("None");
    private readonly Reactive<string> _currentPattern = new("None");
    private readonly Reactive<bool> _generativeMode = new(false);
    private static readonly MoogSynthPatch[] Patches = MoogSynthPresets.All();
    private int _patchIndex;

    // Audio effects
    private readonly ReactiveList<EffectEntry> _activeEffects = new();

    // Audio recording state
    private readonly Reactive<bool> _isAudioHoldRecording = new(false);
    private readonly Reactive<bool> _isAudioToggleRecording = new(false);
    private readonly Reactive<bool> _audioPlaybackEnabled = new(true);
    private readonly ReactiveList<EffectEntry> _voiceEffects = new();
    private readonly Dictionary<string, AudioStreamState> _audioStreamStates = new();

    // PushToTalk validation
    private readonly Reactive<bool> _isPushToTalkRecording = new(false);
    private readonly Reactive<string> _lastSpeechRecognized = new("");
    private readonly ClientReactive<string> _lastMicPermission = new("");

    // Chat state
    private readonly Reactive<string> _chatInputText = new("");
    private readonly Reactive<bool> _chatIsProcessing = new(false);
    private readonly ReactiveList<ChatMessageEntry> _chatMessages = new();

    // Infinite scroll state
    private readonly ReactiveList<string> _infiniteScrollItems = new();
    private readonly Reactive<bool> _infiniteScrollLoading = new(false);
    private readonly Reactive<bool> _infiniteScrollHasMore = new(true);
    private int _infiniteScrollPage;

    // Split layout demos
    private readonly Reactive<bool> _splitSidebarOpen = new(true);
    private readonly Reactive<double> _resizableSplitSize = new(224);
    private readonly Reactive<double> _resizableNestedOuterSize = new(320);
    private readonly Reactive<double> _resizableNestedInnerSize = new(140);

    // Card selected demo
    private readonly Reactive<string> _selectedCardId = new("card-2");

    // Auto-scroll test state
    private readonly ReactiveList<string> _autoScrollPoliteItems = new();
    private readonly ReactiveList<string> _autoScrollAssertiveItems = new();
    private CancellationTokenSource? _autoScrollCts;

    // Sound playback state
    private readonly Reactive<string> _lastSoundPlaybackId = new("(no sound playing)");
    private AudioPlayback? _lastSoundPlayback;
    private readonly Reactive<bool> _soundToastOpen = new(false);
    private readonly Reactive<string> _soundToastMessage = new("");

    // Keyboard listener state
    private readonly Reactive<string> _globalKeyDownEvent = new("(no event)");
    private readonly Reactive<string> _scopedKeyDownEvent = new("(no event)");
    private readonly Reactive<string> _globalKeyUpEvent = new("(no event)");

    // Calendar / pickers state
    private readonly Reactive<string> _calendarValue = new("2026-04-23");
    private readonly Reactive<string> _datePickerValue = new("");
    private readonly Reactive<string> _colorPickerValue = new("#db176e");
    private readonly Reactive<string> _timePickerValue = new("09:30");

    // Rich text / code editor state
    private readonly Reactive<string> _richTextValue = new("<p>Edit <b>me</b> — try the toolbar!</p>");
    private readonly Reactive<string> _codeEditorValue = new("public class Hello\n{\n    public static void Main() =>\n        Console.WriteLine(\"Hi\");\n}\n");

    // Carousel state
    private readonly Reactive<int> _carouselIndex = new(0);
    private readonly Reactive<bool> _carouselLoop = new(true);
    private readonly Reactive<int> _multiCarouselIndex = new(0);
    private readonly Reactive<bool> _multiCarouselLoop = new(true);

    // Feed scroller state
    private readonly Reactive<int> _feedActiveIndex = new(0);
    private readonly Reactive<bool> _feedMuted = new(true);
    private readonly Reactive<int> _feedPagesLoaded = new(1);

    // Interactions test state
    private readonly Reactive<string> _textFieldSubmitStatus = new("(not submitted)");
    private readonly Reactive<string> _textAreaSubmitStatus = new("(not submitted)");
    private readonly Reactive<string> _imageClickStatus = new("(not clicked)");
    private readonly Reactive<int> _imageClickCount = new(0);
    private readonly Reactive<string> _boxClickStatus = new("(not clicked)");
    private readonly Reactive<int> _boxClickCount = new(0);

    // Asset tab state
    private readonly Reactive<byte[]?> _assetLocalFileData = new(null);
    private readonly Reactive<string> _assetLocalFileStatus = new("Not loaded");

    private readonly Reactive<bool> _assetCloudFileUploading = new(false);
    private readonly Reactive<string> _assetCloudFileStatus = new("Not uploaded");
    private readonly Reactive<string> _assetCloudFileMetadata = new("");
    private readonly Reactive<string> _assetCloudFileBackingUrl = new("");
    private readonly Reactive<byte[]?> _assetCloudFileDownloaded = new(null);

    private readonly Reactive<bool> _assetCloudFilePublicUploading = new(false);
    private readonly Reactive<string> _assetCloudFilePublicStatus = new("Not uploaded");
    private readonly Reactive<string> _assetCloudFilePublicMetadata = new("");
    private readonly Reactive<string> _assetCloudFilePublicBackingUrl = new("");
    private readonly Reactive<byte[]?> _assetCloudFilePublicDownloaded = new(null);
    private readonly Reactive<string> _assetCloudFilePublicSameOriginUrl = new("");
    private readonly Reactive<bool> _assetCloudFilePublicShowByUri = new(false);

    private readonly Reactive<string> _assetCloudJsonStatus = new("Not uploaded");
    private readonly Reactive<string> _assetCloudJsonMetadata = new("");
    private readonly Reactive<string> _assetCloudJsonUploaded = new("");
    private readonly Reactive<string> _assetCloudJsonDownloaded = new("");

    public async Task Main()
    {
        _theme = UI.UseTheme(Theme.Light);

        FunctionRegistry.Instance.RegisterFromType(typeof(ValidationFunctions));
        FunctionRegistry.Instance.RegisterFromInstance(this);

        _ = InitPaymentsAsync();

        await StartAudioGeneratorAsync();
        SetupAudioMetricsTracking();
        StartMediaCounterPublishing();
        SetupVideoInputHandlers();
        SetupAudioInputHandlers();
        SetupCustomMessageHandlers();
        await StartMcpAsync();
        InitSelfTest();
        InitDevice();
        InitTelephony();
        InitSignatures();
        InitConsent();
        InitSiblingClientEvents();
        InitGoogleDrive();

        app.OnStopping(ClearDatabaseAsync);

        // Exercises the dynamic snapshot-route provider: these ship as route snapshots exactly like
        // the statically configured ones. The platform validation script asserts both sets reach the
        // sitemap and prerendered crawler pages.
        app.OnSnapshotRoutes(() => Task.FromResult<IEnumerable<string>>(["/charts", "/icons"]));

        app.Navigation.PathChangedAsync += args =>
        {
            RecordNotificationActionTap(args.ClientSessionId, args.Url);
            var tab = args.Path.TrimStart('/');

            if (ValidTabs.Contains(tab))
            {
                ActivateTab(tab);
            }

            return Task.CompletedTask;
        };

        app.ClientJoinedAsync += args =>
        {
            // InitialPath carries the deep link's query string too — tab names are path-only
            var tab = args.ClientContext.InitialPath.Split('?', 2)[0].TrimStart('/');

            if (ValidTabs.Contains(tab))
            {
                ActivateTab(tab);
            }

            _ = RefreshDevicesAsync(args.ClientSessionId);
            _ = LoadIdentityAsync(args.ClientContext);

            return Task.CompletedTask;
        };

        UI.Root([Page.Default],
            content: view =>
            {
                AddColorScaleCss();

                view.Column([Container.Xl4, "py-4 px-3 sm:py-8 sm:px-4"], content: view =>
                {
                    // Header with title and theme toggle
                    view.Row(["flex justify-between items-center mb-4 sm:mb-6"], content: view =>
                    {
                        view.Text([Text.Display, "text-3xl sm:text-5xl"], "Validation");

                        var isDark = _theme.Current.Value == Theme.Dark;
                        var iconName = isDark ? "sun" : "moon";
                        view.Button([Button.GhostMd, Button.Icon],
                            onClick: _theme.ToggleAsync,
                            content: vv => vv.Icon([Icon.Default], name: iconName));
                    });

                    // Main tabs. lazyPanels keeps only the active tab's panel in each client's
                    // server-side tree — with this many tabs, shipping all panels costs ~20 MB of
                    // live heap per connected client.
                    view.Tabs(
                        value: _activeTab.Value,
                        lazyPanels: true,
                        onValueChange: async value =>
                        {
                            var tab = value ?? "typography";
                            ActivateTab(tab);
                            await app.Navigation.SetPathAsync($"/{tab}");
                        },
                        listContainerStyle: [Card.Default, "p-2 mb-4"],
                        listStyle: [Tabs.List, "flex-wrap bg-transparent"],
                        triggerStyle: [Tabs.Trigger, "px-2 text-xs sm:px-3 sm:text-sm"],
                        contentStyle: [Tabs.Content],
                        tabs: [
                            // Basic UI building blocks, ordered simple -> complex. The
                            // Flutter-frontend audit walks these top-to-bottom.
                            new TabItem("typography", "Typography", RenderTypographySection),
                            new TabItem("icons", "Icons", ProfilingSkippable(RenderIconsSection), forceMount: true),
                            new TabItem("buttons", "Buttons", RenderButtonsSection),
                            new TabItem("inputs", "Inputs", RenderInputsSection),
                            new TabItem("forms", "Forms", RenderFormsSection),
                            new TabItem("cards", "Cards", RenderCardsSection),
                            new TabItem("layout", "Layout", RenderLayoutSection),
                            new TabItem("overlays", "Overlays", RenderOverlaysSection),
                            new TabItem("navigation", "Navigation", RenderNavigationSection),
                            new TabItem("nav-menu", "Nav Menu", RenderNavMenuSection),
                            new TabItem("advanced-inputs", "Advanced Inputs", RenderAdvancedInputsSection),
                            new TabItem("drag-drop", "Drag & Drop", RenderDragDropSection),
                            new TabItem("pan-zoom", "Pan & Zoom", RenderPanZoomSection),
                            // Specialized / feature areas.
                            new TabItem("crosswind", "Crosswind", RenderCrosswindSection),
                            new TabItem("brand", "Brand", RenderBrandSection),
                            new TabItem("charts", "Charts", ProfilingSkippable(RenderChartsSection)),
                            new TabItem("files", "Files", RenderFilesSection),
                            new TabItem("assets", "Assets", RenderAssetsSection),
                            new TabItem("actions", "Actions", RenderActionsSection),
                            new TabItem("notifications", "Notifications", RenderNotificationsSection),
                            new TabItem("video", "Video", RenderVideoSection),
                            new TabItem("audio", "Audio", RenderAudioSection),
                            new TabItem("shadertoy", "Shadertoy", RenderShadertoySection),
                            new TabItem("ikon-ai", "Ikon.AI Library", ProfilingSkippable(RenderIkonAISection)),
                            new TabItem("mcp", "MCP", RenderMcpSection),
                            new TabItem("cells", "Cells", RenderCellsSection),
                            new TabItem("cron", "Cron", RenderCronSection),
                            new TabItem("virtualization", "Virtualization", RenderVirtualizationSection),
                            new TabItem("drawing", "Drawing", RenderDrawingSection),
                            new TabItem("profiling", "Profiling", RenderProfilingSection),
                            new TabItem("memory", "Memory", ProfilingSkippable(RenderMemorySection)),
                            new TabItem("session-identity", "Session Identity", RenderSessionIdentitySection),
                            new TabItem("account", "Account", RenderAccountSection),
                            new TabItem("react-sdk", "React SDK", RenderReactSdkSection),
                            new TabItem("consent", "Consent", RenderConsentSection),
                            new TabItem("payments", "Payments", ProfilingSkippable(RenderPaymentsSection)),
                            new TabItem("email", "Email", RenderEmailSection),
                            new TabItem("costs", "Costs", RenderCostsSection),
                            new TabItem("custom-messages", "Custom Messages", RenderCustomMessagesSection),
                            new TabItem("database", "Database", RenderDatabaseSection),
                            new TabItem("persistent-state", "Persistent State", RenderPersistentStateSection),
                            new TabItem("self-test", "Self-Test", RenderSelfTestSection),
                            new TabItem("device", "Device", RenderDeviceSection),
                            new TabItem("telephony", "Telephony", RenderTelephonySection),
                            new TabItem("signatures", "Signatures", RenderSignaturesSection),
                            new TabItem("sharepoint", "SharePoint", RenderSharePointSection),
                            new TabItem("google-drive", "Google Drive", RenderGoogleDriveSection),
                        ]);
                });
            });
    }

    private void ActivateTab(string tab)
    {
        _activeTab.Value = tab;

        // The Database tab's EF Core + Npgsql stack initializes on first activation instead of at
        // startup, keeping it out of every instance's fresh memory footprint. Activation is a real
        // navigation event, so the boot-snapshot capture (which renders content but never navigates)
        // cannot trigger it, and the refresh runs in handler context where reactive writes are legal.
        if (tab == "database" && _dbInitTask == null)
        {
            _ = RefreshEntriesAsync();
        }

        // The status read runs on every visit, so one failed read does not stick on a long-lived
        // instance; a render cannot start it, because the writes of a task a render spawns are
        // discarded along with the render's own.
        if (tab == "telephony")
        {
            _ = RefreshTelephonyAsync();
        }
    }

    private Task StartAudioGeneratorAsync()
    {
        return AudioGenerator.StartAsync(
            frame =>
            {
                Interlocked.Increment(ref _audioFramesToClients);
                return Audio.Raw.SendFrameAsync(MediaTargets.Everyone, frame.StreamId, frame.Samples, frame.SampleRate, frame.ChannelCount, frame.IsFirst, frame.IsLast);
            },
            onStreamEnd: streamId => Audio.Raw.CloseStreamAsync(streamId),
            cancellationToken: CancellationToken.None);
    }

    private void SetupAudioMetricsTracking()
    {
        Audio.Metrics.Enabled = true;

        _ = Task.Run(async () =>
        {
            await foreach (var report in Audio.Metrics.Reports())
            {
                _audioStreamCount.Value = report.StreamCount;
                _audioMinIpdMs.Value = report.MinIpdMs;
                _audioAvgIpdMs.Value = report.AvgIpdMs;
                _audioMaxIpdMs.Value = report.MaxIpdMs;
                _audioJitterMs.Value = report.JitterMs;
                _audioCpuUsagePercent.Value = report.CpuUsagePercent;
            }
        });
    }

    private void CleanupCameraEcho()
    {
        if (_cameraEchoSurface.Value == null) return;
        _cameraEchoSurface.Value = null;
        _cameraWidth.Value = 0;
        _cameraHeight.Value = 0;
    }

    private void CleanupScreenEcho()
    {
        if (_screenEchoSurface.Value == null) return;
        _screenEchoSurface.Value = null;
        _screenWidth.Value = 0;
        _screenHeight.Value = 0;
    }

    private ClientVideoCaptureCodec ParseCodec(string codec) => codec switch
    {
        "vp8" => ClientVideoCaptureCodec.Vp8,
        "vp9" => ClientVideoCaptureCodec.Vp9,
        "av1" => ClientVideoCaptureCodec.Av1,
        _ => ClientVideoCaptureCodec.H264,
    };

    private (int Width, int Height) ParseResolution(string res) => res switch
    {
        "480p" => (854, 480),
        "1080p" => (1920, 1080),
        "1440p" => (2560, 1440),
        _ => (1280, 720),
    };

    private int ParseBitrateMbps(string mbps)
    {
        if (double.TryParse(mbps, System.Globalization.CultureInfo.InvariantCulture, out var val) && val > 0)
        {
            return (int)(val * 1_000_000);
        }

        return 5_000_000;
    }

    private int ParseFramerate(string fps)
    {
        if (int.TryParse(fps, out var val) && val > 0)
        {
            return val;
        }

        return 30;
    }

    private int ParseBitrateKbps(string kbps)
    {
        if (double.TryParse(kbps, System.Globalization.CultureInfo.InvariantCulture, out var val) && val > 0)
        {
            return (int)(val * 1_000);
        }

        return 32_000;
    }

    private static string? GetSelectedDeviceId(string selectedId)
    {
        return selectedId == "default" ? null : selectedId;
    }

    private async Task RefreshDevicesAsync(int clientSessionId)
    {
        if (_devicesLoading.Value)
        {
            return;
        }

        _devicesLoading.Value = true;

        try
        {
            var devices = await ClientFunctions.GetMediaDevicesAsync(targetId: clientSessionId);
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

    private void SetupVideoInputHandlers()
    {
        Video.InputStartedAsync += async input =>
        {
            RecordClientVideoStreamBegin(input);

            var surface = input.Kind switch
            {
                VideoSourceKind.Camera => CameraEchoSurface,
                VideoSourceKind.Screen => ScreenEchoSurface,
                _ => null
            };

            VideoEcho? echo = null;

            if (surface != null)
            {
                // Through the server on purpose, not a local preview: the round trip is what this tab tests
                echo = new VideoEcho(input, Video.Play(MediaTargets.Everyone, surface, input));

                if (input.Kind == VideoSourceKind.Camera)
                {
                    _cameraEcho = echo;
                    _echoAudience.Value = "everyone";
                    _echoAudienceResult.Value = "";
                    _echoKeyFrameResult.Value = "";
                }
                else
                {
                    _screenEcho = echo;
                }

                ShowEcho(input);
                _ = ReportEchoAsync(echo);
            }

            input.ResizedAsync += async resized =>
            {
                if (IsCurrentEcho(echo))
                {
                    ShowEcho(resized);
                }
            };

            input.FrameReceivedAsync += async frame =>
            {
                RecordClientVideoFrame(frame);

                if (echo != null && echo == _cameraEcho)
                {
                    NoteEchoFrame(frame);
                }

                if (echo == null || !IsCurrentEcho(echo))
                {
                    return;
                }

                // A capture stopped and started again on the same track keeps its input, so the
                // echo has to come back on its frames rather than on a new input
                if (input.Kind == VideoSourceKind.Camera && _isCameraCaptureActive.Value && _cameraEchoSurface.Value == null)
                {
                    ShowEcho(input);
                }
                else if (input.Kind == VideoSourceKind.Screen && _isScreenCaptureActive.Value && _screenEchoSurface.Value == null)
                {
                    ShowEcho(input);
                }

                // The relay forwards on the platform's side; a frame counts as sent to the clients
                // once the echo's first frame has reached one of them
                if (echo.Playback.Started.IsCompleted && !echo.Playback.IsEnded)
                {
                    Interlocked.Increment(ref _videoFramesToClients);
                }
            };
        };

        Video.InputEndedAsync += async input =>
        {
            if (_cameraEcho?.Input == input)
            {
                _cameraEcho = null;
                CleanupCameraEcho();
            }
            else if (_screenEcho?.Input == input)
            {
                _screenEcho = null;
                CleanupScreenEcho();
            }
        };
    }

    private bool IsCurrentEcho(VideoEcho? echo) => echo != null && (echo == _cameraEcho || echo == _screenEcho);

    private void ShowEcho(VideoInput input)
    {
        if (input.Kind == VideoSourceKind.Camera)
        {
            _cameraEchoSurface.Value = CameraEchoSurface;
            _cameraWidth.Value = input.Width;
            _cameraHeight.Value = input.Height;
        }
        else if (input.Kind == VideoSourceKind.Screen)
        {
            _screenEchoSurface.Value = ScreenEchoSurface;
            _screenWidth.Value = input.Width;
            _screenHeight.Value = input.Height;
        }
    }

    private async Task ReportEchoAsync(VideoEcho echo)
    {
        var status = echo.Input.Kind == VideoSourceKind.Camera ? _cameraEchoStatus : _screenEchoStatus;
        status.Value = "Echo: waiting for the first frame to reach a viewer";

        await echo.Playback.Started;
        var reachedViewer = !echo.Playback.IsEnded;

        if (reachedViewer && IsCurrentEcho(echo))
        {
            status.Value = "PASS Echo started: the first frame reached a viewer";
        }

        var outcome = await echo.Playback.Completion;

        if (!reachedViewer && IsCurrentEcho(echo))
        {
            status.Value = $"FAIL Echo ended {outcome} before any frame reached a viewer";
        }
    }

    private void SetupAudioInputHandlers()
    {
        // Enable the high-level Audio.SpeechRecognizedAsync pipeline so the PushToTalkButton
        // in the audio tab produces transcriptions end-to-end.
        Audio.UseSpeechRecognition(SpeechRecognizerModel.WhisperLarge3Turbo);

        Audio.SpeechRecognizedAsync += async args =>
        {
            _lastSpeechRecognized.Value = args.Text;
        };

        Audio.AudioInputStreamBeginAsync += async args =>
        {
            // Store stream info for speech recognizer (stream stays open, isFirst/isLast mark recordings)
            _speechRecognizerStreamInfo[args.StreamId] = (args.SampleRate, args.ChannelCount);

            // Audio section echo playback
            var state = new AudioStreamState(args.SampleRate, args.ChannelCount);
            _audioStreamStates[args.StreamId] = state;
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            if (IsCallMicrophone(args.StreamId))
            {
                if (_audioStreamStates.TryGetValue(args.StreamId, out var micState))
                {
                    TakeCallMicrophoneFrame(args.StreamId, args.Samples.ToArray(), micState.SampleRate, micState.ChannelCount);
                }

                return;
            }

            RecordClientAudioFrame(args.Samples, args.IsFirst);

            // Speech recognizer continuous mode - write samples to channel
            if (_speechRecognizerContinuous.Value && _speechRecognizerChannel != null)
            {
                _speechRecognizerChannel.Writer.TryWrite(args.Samples.ToArray());
                return;
            }

            // Speech recognizer batch mode - create buffer on isFirst when recording
            if (!_speechRecognizerContinuous.Value && args.IsFirst && _speechRecognizerRecording.Value && _speechRecognizerStreamInfo.TryGetValue(args.StreamId, out var info))
            {
                _speechRecognizerBuffers[args.StreamId] = new SpeechRecognizerBuffer
                {
                    SampleRate = info.SampleRate,
                    ChannelCount = info.ChannelCount
                };
            }

            // Speech recognizer batch mode - always process if buffer exists (regardless of recording state)
            if (_speechRecognizerBuffers.TryGetValue(args.StreamId, out var buffer))
            {
                buffer.Samples.AddRange(args.Samples.ToArray());

                if (args.IsLast)
                {
                    _ = ProcessSpeechRecognizerBufferAsync(args.StreamId, buffer);
                }

                return;
            }

            // Audio section echo playback
            if (!_audioStreamStates.TryGetValue(args.StreamId, out var state))
            {
                return;
            }

            if (_audioPlaybackEnabled.Value)
            {
                var processedSamples = args.Samples.ToArray();
                var effectInstances = GetVoiceEffectInstances(state);

                foreach (var effect in effectInstances)
                {
                    effect.Process(processedSamples);
                }

                Interlocked.Increment(ref _audioFramesToClients);
                await Audio.Raw.SendFrameAsync(MediaTargets.Everyone, args.StreamId, processedSamples, state.SampleRate, state.ChannelCount, args.IsFirst, args.IsLast);
            }
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            // Complete continuous recognition channel when stream ends
            _speechRecognizerChannel?.Writer.TryComplete();

            // Clean up stream info
            _speechRecognizerStreamInfo.Remove(args.StreamId);
            _speechRecognizerBuffers.Remove(args.StreamId);

            // Audio section cleanup
            if (_audioStreamStates.TryGetValue(args.StreamId, out var state))
            {
                state.EffectInstances = null;
                _audioStreamStates.Remove(args.StreamId);
            }
        };
    }

    private List<IAudioEffectInstance> GetVoiceEffectInstances(AudioStreamState state)
    {
        if (state.EffectInstances == null || state.EffectInstances.Count != _voiceEffects.Value.Count)
        {
            state.EffectInstances = _voiceEffects.Value
                .Select(e => e.Effect.Create(state.SampleRate, state.ChannelCount))
                .ToList();
        }

        return state.EffectInstances;
    }

}

internal class AudioStreamState(int sampleRate, int channelCount)
{
    public int SampleRate { get; } = sampleRate;
    public int ChannelCount { get; } = channelCount;
    public List<IAudioEffectInstance>? EffectInstances { get; set; }
}

internal sealed record VideoEcho(VideoInput Input, VideoPlayback Playback);

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
