return await App.Run(args);

public record SessionIdentity(string UserId, string Id);
public record ClientParams(string Id, string Test);

[App]
public partial class Validation(IApp<SessionIdentity, ClientParams> host)
{
    private UI UI { get; } = new(host, new Theme()) { EnableProfiling = false };
    private Audio Audio { get; set; } = new(host);
    private Video Video { get; } = new(host);
    private AudioGenerator AudioGenerator { get; } = new();

    // Tab state
    private readonly Reactive<string> _activeTab = new("buttons");

    private static readonly HashSet<string> ValidTabs =
    [
        "buttons", "inputs", "cards", "overlays", "navigation", "layout", "forms",
        "typography", "files", "assets", "actions", "video", "audio", "drag-drop", "nav-menu",
        "rive", "shadertoy", "crosswind", "charts", "profiling", "ikon-ai", "identity", "functions"
    ];

    // Input states
    private readonly Reactive<string> _textFieldValue = new("");
    private readonly Reactive<string> _textAreaValue = new("");
    private readonly Reactive<string> _selectValue = new("");
    private readonly Reactive<bool> _checkboxChecked = new(false);
    private readonly Reactive<string> _checkboxIndeterminate = new("unchecked");
    private readonly Reactive<bool> _switchChecked = new(false);
    private readonly Reactive<string> _radioValue = new("option1");
    private readonly Reactive<double> _sliderValue = new(50);
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

    // ActionButton/ClientFunction states
    private readonly Reactive<string> _clientFunctionResultText = new("(no function called)");
    private readonly Reactive<bool> _clientFunctionToastOpen = new(false);

    // Drag and drop states
    private readonly Reactive<string> _dndStatus = new("");
    private readonly Reactive<string> _activeDragId = new("");
    private readonly Reactive<IReadOnlyList<string>> _sortableSimpleVertical = new(["Simple 1", "Simple 2", "Simple 3", "Simple 4"]);
    private readonly Reactive<IReadOnlyList<string>> _sortableSimpleHorizontal = new(["X", "Y", "Z"]);
    private readonly Reactive<Dictionary<string, string?>> _draggableItemZones = new(
        new Dictionary<string, string?>
        {
            { "drag-1", null },
            { "drag-2", null },
            { "drag-disabled", null }
        });
    private readonly Reactive<Dictionary<string, string?>> _overlayItemZones = new(
        new Dictionary<string, string?>
        {
            { "overlay-item-1", null },
            { "overlay-item-2", null }
        });

    // NavigationMenu states
    private readonly Reactive<string> _navMenuValue = new("");
    private readonly Reactive<string> _navMenuStatus = new("");

    // Rive state - Layout options
    private readonly Reactive<RiveFit> _riveFit = new(RiveFit.Contain);
    private readonly Reactive<RiveAlignment> _riveAlignment = new(RiveAlignment.Center);

    // Rive state - Rating.riv event test
    private readonly Reactive<string> _riveLastEventName = new("(no event)");
    private readonly Reactive<string> _riveLastEventRating = new("");
    private readonly Reactive<string> _riveLastEventMessage = new("");

    // Rive state - Person databinding test
    private readonly Reactive<string> _rivePersonName = new("John Doe");
    private readonly Reactive<double> _rivePersonAge = new(25);
    private readonly Reactive<bool> _rivePersonAgreedToTerms = new(false);
    private readonly RiveTrigger _riveSubmitTrigger = new("onFormSubmit");
    private readonly RiveTrigger _riveResetTrigger = new("onFormReset");

    // Rive state - Color and enum testing (person_databinding_test.riv)
    private readonly Reactive<int> _riveFavColorR = new(255);
    private readonly Reactive<int> _riveFavColorG = new(128);
    private readonly Reactive<int> _riveFavColorB = new(0);
    private readonly Reactive<int> _riveCountry = new(0);

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
    private readonly ClientReactive<string> _currentTheme = new(Constants.LightTheme);

    // Video capture state
    private readonly Reactive<bool> _isCameraCaptureActive = new(false);
    private readonly Reactive<bool> _isScreenCaptureActive = new(false);

    // Video echo state
    private readonly Dictionary<string, VideoEchoInfo> _videoEchos = new();
    private readonly Dictionary<int, int> _echoToInputTrack = new();

    // Video UI state
    private readonly Reactive<string?> _cameraEchoStreamId = new(null);
    private readonly Reactive<int> _cameraWidth = new(0);
    private readonly Reactive<int> _cameraHeight = new(0);
    private readonly Reactive<string?> _screenEchoStreamId = new(null);
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

    // Microphone device selection
    private readonly Reactive<string> _selectedMicrophoneId = new("default");

    // Audio capture options
    private readonly Reactive<bool> _audioEchoCancellation = new(true);
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
    private readonly Reactive<List<string>> _sineWaveIds = new([]);
    private readonly Reactive<List<string>> _drumMachineIds = new([]);
    private readonly Reactive<List<string>> _moogSynthIds = new([]);

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
    private readonly Reactive<List<EffectEntry>> _activeEffects = new([]);

    // Audio recording state
    private readonly Reactive<bool> _isAudioHoldRecording = new(false);
    private readonly Reactive<bool> _isAudioToggleRecording = new(false);
    private readonly Reactive<bool> _audioPlaybackEnabled = new(true);
    private readonly Reactive<List<EffectEntry>> _voiceEffects = new([]);
    private readonly Dictionary<string, AudioStreamState> _audioStreamStates = new();

    // Chat state
    private readonly Reactive<string> _chatInputText = new("");
    private readonly Reactive<bool> _chatIsProcessing = new(false);
    private readonly Reactive<List<ChatMessageEntry>> _chatMessages = new([]);

    // Infinite scroll state
    private readonly Reactive<List<string>> _infiniteScrollItems = new([]);
    private readonly Reactive<bool> _infiniteScrollLoading = new(false);
    private readonly Reactive<bool> _infiniteScrollHasMore = new(true);
    private int _infiniteScrollPage;

    // Auto-scroll test state
    private readonly Reactive<List<string>> _autoScrollPoliteItems = new([]);
    private readonly Reactive<List<string>> _autoScrollAssertiveItems = new([]);
    private CancellationTokenSource? _autoScrollCts;

    // Sound playback state
    private readonly Reactive<string> _lastSoundPlaybackId = new("(no sound playing)");
    private readonly Reactive<bool> _soundToastOpen = new(false);
    private readonly Reactive<string> _soundToastMessage = new("");

    // Keyboard listener state
    private readonly Reactive<string> _globalKeyDownEvent = new("(no event)");
    private readonly Reactive<string> _scopedKeyDownEvent = new("(no event)");
    private readonly Reactive<string> _globalKeyUpEvent = new("(no event)");

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
    private readonly Reactive<string?> _assetCloudFileUrl = new(null);

    private readonly Reactive<bool> _assetCloudFilePublicUploading = new(false);
    private readonly Reactive<string> _assetCloudFilePublicStatus = new("Not uploaded");
    private readonly Reactive<string> _assetCloudFilePublicMetadata = new("");
    private readonly Reactive<string> _assetCloudFilePublicBackingUrl = new("");
    private readonly Reactive<byte[]?> _assetCloudFilePublicDownloaded = new(null);
    private readonly Reactive<string?> _assetCloudFilePublicUrl = new(null);

    private readonly Reactive<string> _assetCloudJsonStatus = new("Not uploaded");
    private readonly Reactive<string> _assetCloudJsonMetadata = new("");
    private readonly Reactive<string> _assetCloudJsonUploaded = new("");
    private readonly Reactive<string> _assetCloudJsonDownloaded = new("");

    public async Task Main()
    {
        FunctionRegistry.Instance.RegisterFromType(typeof(ValidationFunctions));

        await StartAudioGeneratorAsync();
        SetupAudioMetricsTracking();
        SetupVideoInputHandlers();
        SetupAudioInputHandlers();

        host.Navigation.PathChangedAsync += async args =>
        {
            var tab = args.Path.TrimStart('/');

            if (ValidTabs.Contains(tab))
            {
                _activeTab.Value = tab;
            }
            else
            {
                await host.Navigation.SetPathAsync(args.ClientSessionId, $"/{_activeTab.Value}", replace: true);
            }
        };

        host.ClientJoinedAsync += async args =>
        {
            if (!string.IsNullOrEmpty(args.ClientContext.Theme))
            {
                _currentTheme.Value = args.ClientContext.Theme == Constants.DarkTheme ? Constants.DarkTheme : Constants.LightTheme;
            }

            var tab = args.ClientContext.InitialPath.TrimStart('/');

            if (ValidTabs.Contains(tab))
            {
                _activeTab.Value = tab;
            }
            else
            {
                await host.Navigation.SetPathAsync(args.ClientSessionId, $"/{_activeTab.Value}", replace: true);
            }

            _ = RefreshDevicesAsync(args.ClientSessionId);
        };

        UI.Root([Page.Default],
            content: view =>
            {
                view.Column([Container.Xl4, "py-8 px-4"], content: view =>
                {
                    // Header with title and theme toggle
                    view.Row(["flex justify-between items-center mb-6"], content: view =>
                    {
                        view.Text([Text.Display], "Validation");
                        var isDark = _currentTheme.Value == Constants.DarkTheme;
                        var iconName = isDark ? "sun" : "moon";
                        view.Button([Button.GhostMd, Button.Size.Icon],
                            onClick: ToggleThemeAsync,
                            content: v => v.Icon([Icon.Default], name: iconName));
                    });

                    // Main tabs
                    view.Tabs(
                        value: _activeTab.Value,
                        onValueChange: async value =>
                        {
                            var tab = value ?? "buttons";
                            _activeTab.Value = tab;
                            await host.Navigation.SetPathAsync($"/{tab}");
                        },
                        listContainerStyle: [Card.Default, "p-2 mb-4"],
                        listStyle: [Tabs.List, "flex-wrap bg-transparent"],
                        triggerStyle: [Tabs.Trigger],
                        contentStyle: [Tabs.Content],
                        tabs: [
                            new TabItem("buttons", "Buttons", RenderButtonsSection),
                            new TabItem("inputs", "Inputs", RenderInputsSection),
                            new TabItem("cards", "Cards", RenderCardsSection),
                            new TabItem("overlays", "Overlays", RenderOverlaysSection),
                            new TabItem("navigation", "Navigation", RenderNavigationSection),
                            new TabItem("layout", "Layout", RenderLayoutSection),
                            new TabItem("forms", "Forms", RenderFormsSection),
                            new TabItem("typography", "Typography", RenderTypographySection),
                            new TabItem("files", "Files", RenderFilesSection),
                            new TabItem("assets", "Assets", RenderAssetsSection),
                            new TabItem("actions", "Actions", RenderActionsSection),
                            new TabItem("video", "Video", RenderVideoSection),
                            new TabItem("audio", "Audio", RenderAudioSection),
                            new TabItem("drag-drop", "Drag & Drop", RenderDragDropSection),
                            new TabItem("nav-menu", "Nav Menu", RenderNavMenuSection),
                            new TabItem("rive", "Rive", RenderRiveSection),
                            new TabItem("shadertoy", "Shadertoy", RenderShadertoySection),
                            new TabItem("crosswind", "Crosswind", RenderCrosswindSection),
                            new TabItem("charts", "Charts", RenderChartsSection),
                            new TabItem("profiling", "Profiling", RenderProfilingSection),
                            new TabItem("ikon-ai", "Ikon.AI Library", RenderIkonAISection),
                            new TabItem("identity", "Identity", RenderIdentitySection),
                            new TabItem("functions", "Functions", RenderFunctionsSection),
                        ]);
                });
            });
    }

    private Task StartAudioGeneratorAsync()
    {
        return AudioGenerator.StartAsync(
            frame => Audio.SendAsync(frame.Samples, frame.SampleRate, frame.ChannelCount, frame.IsFirst, frame.IsLast, frame.StreamId),
            cancellationToken: CancellationToken.None);
    }

    private void SetupAudioMetricsTracking()
    {
        Audio.Metrics.Enabled = true;
        Audio.Metrics.Updated += () =>
        {
            _audioStreamCount.Value = Audio.Metrics.StreamCount;
            _audioMinIpdMs.Value = Audio.Metrics.MinIpdMs;
            _audioAvgIpdMs.Value = Audio.Metrics.AvgIpdMs;
            _audioMaxIpdMs.Value = Audio.Metrics.MaxIpdMs;
            _audioJitterMs.Value = Audio.Metrics.JitterMs;
            _audioCpuUsagePercent.Value = Audio.Metrics.CpuUsagePercent;
        };
    }

    private void CleanupCameraEcho()
    {
        if (_cameraEchoStreamId.Value == null) return;
        _cameraEchoStreamId.Value = null;
        _cameraWidth.Value = 0;
        _cameraHeight.Value = 0;
    }

    private void CleanupScreenEcho()
    {
        if (_screenEchoStreamId.Value == null) return;
        _screenEchoStreamId.Value = null;
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

    private void SetupVideoInputHandlers()
    {
        Video.VideoInputStreamBeginAsync += async args =>
        {
            if (_videoEchos.TryGetValue(args.StreamId, out var existing))
            {
                // Resolution change on existing stream — update dimensions in-place
                existing.Width = args.Width;
                existing.Height = args.Height;

                if (args.SourceType == "camera")
                {
                    _cameraWidth.Value = args.Width;
                    _cameraHeight.Value = args.Height;
                }
                else if (args.SourceType == "screen")
                {
                    _screenWidth.Value = args.Width;
                    _screenHeight.Value = args.Height;
                }

                return;
            }

            var echoStreamId = $"{args.SourceType}_{Guid.NewGuid()}";
            _videoEchos[args.StreamId] = new VideoEchoInfo(echoStreamId, args.Codec, args.Width, args.Height, args.Framerate, args.TrackId, args.SourceType);

            if (args.SourceType == "camera")
            {
                _cameraEchoStreamId.Value = echoStreamId;
                _cameraWidth.Value = args.Width;
                _cameraHeight.Value = args.Height;
            }
            else if (args.SourceType == "screen")
            {
                _screenEchoStreamId.Value = echoStreamId;
                _screenWidth.Value = args.Width;
                _screenHeight.Value = args.Height;
            }
        };

        Video.VideoInputFrameAsync += async args =>
        {
            if (!_videoEchos.TryGetValue(args.StreamId, out var echo))
            {
                return;
            }

            // Re-show video canvas when WebRTC capture resumes (server reuses the same stream)
            if (echo.SourceType == "camera" && _isCameraCaptureActive.Value && _cameraEchoStreamId.Value == null)
            {
                _cameraEchoStreamId.Value = echo.EchoStreamId;
                _cameraWidth.Value = echo.Width;
                _cameraHeight.Value = echo.Height;
            }
            else if (echo.SourceType == "screen" && _isScreenCaptureActive.Value && _screenEchoStreamId.Value == null)
            {
                _screenEchoStreamId.Value = echo.EchoStreamId;
                _screenWidth.Value = echo.Width;
                _screenHeight.Value = echo.Height;
            }

            await Video.SendAsync(args.Data, args.FrameNumber, args.IsKey, args.TimestampInUs, args.DurationInUs,
                echo.Codec, echo.Width, echo.Height, echo.Framerate, echo.EchoStreamId, trackId: echo.InputTrackId);

            var outputInfo = Video.GetOutputStreamInfo(echo.EchoStreamId);

            if (outputInfo != null)
            {
                _echoToInputTrack.TryAdd(outputInfo.TrackId, echo.InputTrackId);

                // Request a keyframe shortly after echo starts so the viewer's decoder can begin
                if (!echo.InitialIdrRequested)
                {
                    echo.InitialIdrRequested = true;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(200);
                        await host.SendMessageAsync(ProtocolMessage.Create(host.ClientContext.SessionId, new RequestIdrVideoFrame(),
                            trackId: echo.InputTrackId, targetIds: [args.ClientSessionId]));
                    });
                }
            }
        };

        Video.VideoInputStreamEndAsync += async args =>
        {
            if (!_videoEchos.Remove(args.StreamId, out var echo))
            {
                return;
            }

            var outputInfo = Video.GetOutputStreamInfo(echo.EchoStreamId);
            if (outputInfo != null)
            {
                _echoToInputTrack.Remove(outputInfo.TrackId);
            }

            await Video.CloseAsync(echo.EchoStreamId);

            if (_cameraEchoStreamId.Value == echo.EchoStreamId)
            {
                _cameraEchoStreamId.Value = null;
                _cameraWidth.Value = 0;
                _cameraHeight.Value = 0;
            }
            else if (_screenEchoStreamId.Value == echo.EchoStreamId)
            {
                _screenEchoStreamId.Value = null;
                _screenWidth.Value = 0;
                _screenHeight.Value = 0;
            }
        };

        host.MessageReceivedAsync += async args =>
        {
            if (args.Message.Opcode != Opcode.VIDEO_REQUEST_IDR_FRAME)
            {
                return;
            }

            if (_echoToInputTrack.TryGetValue(args.Message.TrackId, out var inputTrackId))
            {
                await host.SendMessageAsync(ProtocolMessage.Create(host.ClientContext.SessionId, new RequestIdrVideoFrame(),
                    trackId: inputTrackId, targetIds: [args.Message.SenderId]));
            }
        };
    }

    private void SetupAudioInputHandlers()
    {
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

                await Audio.SendAsync(processedSamples, state.SampleRate, state.ChannelCount, args.IsFirst, args.IsLast, args.StreamId);
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
}

internal class AudioStreamState(int sampleRate, int channelCount)
{
    public int SampleRate { get; } = sampleRate;
    public int ChannelCount { get; } = channelCount;
    public List<IAudioEffectInstance>? EffectInstances { get; set; }
}

internal class VideoEchoInfo(string echoStreamId, VideoCodec codec, int width, int height, double framerate, int inputTrackId, string sourceType)
{
    public string EchoStreamId { get; } = echoStreamId;
    public VideoCodec Codec { get; } = codec;
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;
    public double Framerate { get; } = framerate;
    public int InputTrackId { get; } = inputTrackId;
    public string SourceType { get; } = sourceType;
    public bool InitialIdrRequested { get; set; }
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
