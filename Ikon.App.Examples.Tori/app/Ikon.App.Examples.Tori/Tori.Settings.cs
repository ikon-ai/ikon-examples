public partial class Tori
{
    private static readonly SelectOption[] SpeechModelOptions =
    [
        new("AzureSpeechService", "Azure Speech Service"),
        new("DeepgramNova3General", "Deepgram Nova 3"),
        new("Gpt4OmniMiniTranscribe", "GPT-4o Mini Transcribe"),
        new("Gpt4OmniTranscribe", "GPT-4o Transcribe"),
        new("Whisper2", "Whisper v2"),
        new("WhisperLarge3", "Whisper Large v3"),
        new("WhisperLarge3Turbo", "Whisper Large v3 Turbo"),
    ];

    private static readonly SelectOption[] LanguageOptions =
    [
        new("ar-SA", "Arabic"),
        new("zh-CN", "Chinese"),
        new("nl-NL", "Dutch"),
        new("en-US", "English"),
        new("fi-FI", "Finnish"),
        new("fr-FR", "French"),
        new("de-DE", "German"),
        new("hi-IN", "Hindi"),
        new("it-IT", "Italian"),
        new("ja-JP", "Japanese"),
        new("ko-KR", "Korean"),
        new("pl-PL", "Polish"),
        new("pt-BR", "Portuguese"),
        new("es-ES", "Spanish"),
        new("sv-SE", "Swedish")
    ];

    private static readonly SelectOption[] SummaryModelOptions =
    [
        new("Gpt5", "GPT-5"),
        new("Gpt5Mini", "GPT-5 Mini"),
        new("Gemini25Flash", "Gemini 2.5 Flash"),
        new("Gemini25Pro", "Gemini 2.5 Pro"),
        new("Gemini3Flash", "Gemini 3 Flash"),
        new("Gemini3Pro", "Gemini 3 Pro"),
        new("Claude45Sonnet", "Claude 4.5 Sonnet")
    ];

    private static readonly SelectOption[] CameraResolutionOptions =
    [
        new("640x480", "480p"),
        new("1280x720", "720p"),
        new("1920x1080", "1080p")
    ];

    private static readonly SelectOption[] ScreenResolutionOptions =
    [
        new("1280x720", "720p"),
        new("1920x1080", "1080p"),
        new("2560x1440", "1440p")
    ];

    private static readonly SelectOption[] FramerateOptions =
    [
        new("15", "15 fps"),
        new("24", "24 fps"),
        new("30", "30 fps"),
        new("60", "60 fps")
    ];

    private static readonly SelectOption[] QualityOptions =
    [
        new("very_low", "Very Low"),
        new("low", "Low"),
        new("medium", "Medium"),
        new("high", "High"),
        new("ultra", "Ultra")
    ];

    private static readonly SelectOption[] CodecOptions =
    [
        new("H264", "H.264"),
        new("Vp8", "VP8"),
        new("Vp9", "VP9"),
        new("Av1", "AV1")
    ];

    private void RenderSettingsDialog(UIView view)
    {
        view.Dialog(
            open: _settingsOpen.Value,
            onOpenChange: async open =>
            {
                _settingsOpen.Value = open ?? false;

                if (open == true)
                {
                    var clientScope = ReactiveScope.TryGet<ClientScope>();

                    if (clientScope != null)
                    {
                        var context = app.GlobalState.GetClientContext(clientScope.Value.Id);
                        _ = RefreshDevicesAsync(clientScope.Value.Id, context);
                    }
                }
            },
            overlayStyle: [AlertDialog.Overlay],
            contentStyle: [AlertDialog.Content, IsMobileLayout() ? "w-[calc(100vw-2rem)]" : "w-[42rem]"],
            contentSlot: content =>
            {
                content.Column([Layout.Column.Md], content: col =>
                {
                    // Header
                    col.Row([Layout.Row.SpaceBetween, "items-center mb-4"], content: row =>
                    {
                        row.Text([Text.H3], "Settings");
                        row.Button(
                            [Button.GhostMd, Button.Size.Icon],
                            onClick: async () => _settingsOpen.Value = false,
                            content: v => v.Icon([Icon.Default], name: "x"));
                    });

                    // Settings tabs (screen share not available on mobile layout)
                    var settingsTabs = IsMobileLayout()
                        ? new TabItem[]
                        {
                            new("audio", "Microphone", RenderAudioSettings),
                            new("camera", "Camera", RenderCameraSettings),
                            new("transcript", "Transcript", RenderTranscriptSettings),
                            new("summary", "Summary", RenderSummarySettings)
                        }
                        : new TabItem[]
                        {
                            new("audio", "Microphone", RenderAudioSettings),
                            new("camera", "Camera", RenderCameraSettings),
                            new("screen", "Screen Share", RenderScreenSettings),
                            new("transcript", "Transcript", RenderTranscriptSettings),
                            new("summary", "Summary", RenderSummarySettings)
                        };

                    col.Tabs(
                        value: _settingsTab.Value,
                        onValueChange: async value => _settingsTab.Value = value,
                        listContainerStyle: ["mb-4"],
                        listStyle: ["w-full inline-flex h-9 items-center justify-center rounded-lg bg-muted p-1"],
                        triggerStyle: ["flex-1 inline-flex items-center justify-center whitespace-nowrap rounded-md px-2 py-1 text-sm font-medium text-muted-foreground transition-all focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50 data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm"],
                        contentStyle: ["h-[320px]"],
                        tabs: settingsTabs);

                    // Close button
                    col.Row(["justify-end mt-4"], content: row =>
                    {
                        row.Button(
                            [Button.PrimaryMd],
                            label: "Close",
                            onClick: async () => _settingsOpen.Value = false);
                    });
                });
            });
    }

    private void RenderTranscriptSettings(UIView view)
    {
        view.Column([Layout.Column.Sm], content: col =>
        {
            col.Row(["gap-4 mb-4"], content: row =>
            {
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Row([Layout.Row.SpaceBetween, "items-center h-9"], content: r =>
                    {
                        r.Text([Text.Body], "Enable transcription");
                        r.Switch(
                            [Switch.Default],
                            @checked: _speechEnabled.Value,
                            onCheckedChange: async value =>
                            {
                                _speechEnabled.Value = value;

                                if (value)
                                {
                                    RestartSpeechRecognition();
                                }
                                else
                                {
                                    StopSpeechRecognition();
                                }
                            },
                            content: v => v.SwitchThumb([Switch.Thumb]));
                    });
                });
                row.Column(["flex-1"], content: _ => { });
            });

            col.Row(["gap-4 mb-4"], content: row =>
            {
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Model");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: SpeechModelOptions,
                        value: _speechModel.Value.ToString(),
                        disabled: !_speechEnabled.Value,
                        onValueChange: async value =>
                        {
                            if (Enum.TryParse<SpeechRecognizerModel>(value, out var model) && model != _speechModel.Value)
                            {
                                _speechModel.Value = model;
                                RestartSpeechRecognition();
                            }
                        });
                });
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Language");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: LanguageOptions,
                        value: _speechLanguage.Value,
                        disabled: !_speechEnabled.Value,
                        onValueChange: async value =>
                        {
                            if (value != _speechLanguage.Value)
                            {
                                _speechLanguage.Value = value;
                                RestartSpeechRecognition();
                            }
                        });
                });
            });
        });
    }

    private void RenderSummarySettings(UIView view)
    {
        view.Column([Layout.Column.Sm], content: col =>
        {
            col.Row(["gap-4 mb-4"], content: row =>
            {
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Row([Layout.Row.SpaceBetween, "items-center h-9"], content: r =>
                    {
                        r.Text([Text.Body], "Enable summary");
                        r.Switch(
                            [Switch.Default],
                            @checked: _summaryEnabled.Value,
                            onCheckedChange: async value =>
                            {
                                _summaryEnabled.Value = value;
                            },
                            content: v => v.SwitchThumb([Switch.Thumb]));
                    });
                });
                row.Column(["flex-1"], content: _ => { });
            });

            col.Row(["gap-4"], content: row =>
            {
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Model");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: SummaryModelOptions,
                        value: _summaryModel.Value.ToString(),
                        disabled: !_summaryEnabled.Value,
                        onValueChange: async value =>
                        {
                            if (Enum.TryParse<LLMModel>(value, out var model) && model != _summaryModel.Value)
                            {
                                _summaryModel.Value = model;
                            }
                        });
                });
                row.Column(["flex-1"], content: _ => { });
            });
        });
    }

    private void RenderAudioSettings(UIView view)
    {
        var microphoneDevices = _availableDevices.Value
            .Where(d => d.Kind == "audioinput" && !string.IsNullOrEmpty(d.DeviceId) && d.DeviceId != "default" && d.DeviceId != "communications")
            .Select(d => new SelectOption(d.DeviceId, string.IsNullOrEmpty(d.Label) ? "Microphone" : d.Label))
            .ToArray();
        var microphoneOptions = new[] { new SelectOption("default", "Default") }.Concat(microphoneDevices).ToArray();

        view.Column([Layout.Column.Sm], content: col =>
        {
            col.Column([FormField.Root, "mb-4"], content: field =>
            {
                field.Row([Layout.Row.SpaceBetween, "items-center"], content: labelRow =>
                {
                    labelRow.Text([FormField.Label], "Microphone");
                    labelRow.Tooltip(
                        contentStyle: [Tooltip.Content, "z-[100]"],
                        trigger: v => v.Button(
                            [Button.GhostMd, Button.Size.Icon, "h-6 w-6"],
                            disabled: _devicesLoading.Value,
                            onClick: async () =>
                            {
                                var clientScope = ReactiveScope.TryGet<ClientScope>();

                                if (clientScope != null)
                                {
                                    var context = app.GlobalState.GetClientContext(clientScope.Value.Id);
                                    await RefreshDevicesAsync(clientScope.Value.Id, context);
                                }
                            },
                            content: vv => vv.Icon([Icon.Size.Xs], name: _devicesLoading.Value ? "loader-2" : "refresh-cw")),
                        contentSlot: v => v.Text([Text.Caption], "Refresh"));
                });
                field.Select(
                    triggerStyle: [Select.Trigger, "w-full"],
                    contentStyle: [Select.Content],
                    itemStyle: [Select.Item],
                    options: microphoneOptions,
                    value: _selectedMicrophoneId.Value,
                    disabled: _devicesLoading.Value,
                    onValueChange: async value => _selectedMicrophoneId.Value = value);
            });

            col.Row(["gap-4 mb-4"], content: row =>
            {
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Quality");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: QualityOptions,
                        value: _audioQuality.Value,
                        onValueChange: async value => _audioQuality.Value = value);
                });
                row.Column(["flex-1"], content: _ => { });
            });

            col.Row(["gap-4 mb-3"], content: row =>
            {
                row.Column(["flex-1"], content: field =>
                {
                    field.Row([Layout.Row.SpaceBetween, "items-center"], content: r =>
                    {
                        r.Text([Text.Body], "Echo cancellation");
                        r.Switch(
                            [Switch.Default],
                            @checked: _audioEchoCancellation.Value,
                            onCheckedChange: async value => _audioEchoCancellation.Value = value,
                            content: v => v.SwitchThumb([Switch.Thumb]));
                    });
                });
                row.Column(["flex-1"], content: _ => { });
            });

            col.Row(["gap-4 mb-3"], content: row =>
            {
                row.Column(["flex-1"], content: field =>
                {
                    field.Row([Layout.Row.SpaceBetween, "items-center"], content: r =>
                    {
                        r.Text([Text.Body], "Noise suppression");
                        r.Switch(
                            [Switch.Default],
                            @checked: _audioNoiseSuppression.Value,
                            onCheckedChange: async value => _audioNoiseSuppression.Value = value,
                            content: v => v.SwitchThumb([Switch.Thumb]));
                    });
                });
                row.Column(["flex-1"], content: _ => { });
            });

            col.Row(["gap-4"], content: row =>
            {
                row.Column(["flex-1"], content: field =>
                {
                    field.Row([Layout.Row.SpaceBetween, "items-center"], content: r =>
                    {
                        r.Text([Text.Body], "Auto gain control");
                        r.Switch(
                            [Switch.Default],
                            @checked: _audioAutoGainControl.Value,
                            onCheckedChange: async value => _audioAutoGainControl.Value = value,
                            content: v => v.SwitchThumb([Switch.Thumb]));
                    });
                });
                row.Column(["flex-1"], content: _ => { });
            });
        });
    }

    private void RenderCameraSettings(UIView view)
    {
        var cameraDevices = _availableDevices.Value
            .Where(d => d.Kind == "videoinput" && !string.IsNullOrEmpty(d.DeviceId) && d.DeviceId != "default" && d.DeviceId != "communications")
            .Select(d => new SelectOption(d.DeviceId, string.IsNullOrEmpty(d.Label) ? "Camera" : d.Label))
            .ToArray();
        var cameraOptions = new[] { new SelectOption("default", "Default") }.Concat(cameraDevices).ToArray();

        view.Column([Layout.Column.Sm], content: col =>
        {
            col.Column([FormField.Root, "mb-4"], content: field =>
            {
                field.Row([Layout.Row.SpaceBetween, "items-center"], content: labelRow =>
                {
                    labelRow.Text([FormField.Label], "Camera");
                    labelRow.Tooltip(
                        contentStyle: [Tooltip.Content, "z-[100]"],
                        trigger: v => v.Button(
                            [Button.GhostMd, Button.Size.Icon, "h-6 w-6"],
                            disabled: _devicesLoading.Value,
                            onClick: async () =>
                            {
                                var clientScope = ReactiveScope.TryGet<ClientScope>();

                                if (clientScope != null)
                                {
                                    var context = app.GlobalState.GetClientContext(clientScope.Value.Id);
                                    await RefreshDevicesAsync(clientScope.Value.Id, context);
                                }
                            },
                            content: vv => vv.Icon([Icon.Size.Xs], name: _devicesLoading.Value ? "loader-2" : "refresh-cw")),
                        contentSlot: v => v.Text([Text.Caption], "Refresh"));
                });
                field.Select(
                    triggerStyle: [Select.Trigger, "w-full"],
                    contentStyle: [Select.Content],
                    itemStyle: [Select.Item],
                    options: cameraOptions,
                    value: _selectedCameraId.Value,
                    disabled: _devicesLoading.Value,
                    onValueChange: async value => _selectedCameraId.Value = value);
            });

            col.Row(["gap-4 mb-4"], content: row =>
            {
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Resolution");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: CameraResolutionOptions,
                        value: _cameraResolution.Value,
                        onValueChange: async value => _cameraResolution.Value = value);
                });
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Framerate");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: FramerateOptions,
                        value: _cameraFramerate.Value,
                        onValueChange: async value => _cameraFramerate.Value = value);
                });
            });

            col.Row(["gap-4"], content: row =>
            {
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Quality");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: QualityOptions,
                        value: _cameraQuality.Value,
                        onValueChange: async value => _cameraQuality.Value = value);
                });
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Codec");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: CodecOptions,
                        value: _cameraCodec.Value,
                        onValueChange: async value => _cameraCodec.Value = value);
                });
            });
        });
    }

    private void RenderScreenSettings(UIView view)
    {
        view.Column([Layout.Column.Sm], content: col =>
        {
            col.Row(["gap-4 mb-4"], content: row =>
            {
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Framerate");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: FramerateOptions,
                        value: _screenFramerate.Value,
                        onValueChange: async value => _screenFramerate.Value = value);
                });
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Quality");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: QualityOptions,
                        value: _screenQuality.Value,
                        onValueChange: async value => _screenQuality.Value = value);
                });
            });

            col.Row(["gap-4"], content: row =>
            {
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    field.Text([FormField.Label], "Codec");
                    field.Select(
                        triggerStyle: [Select.Trigger, "w-full"],
                        contentStyle: [Select.Content],
                        itemStyle: [Select.Item],
                        options: CodecOptions,
                        value: _screenCodec.Value,
                        onValueChange: async value => _screenCodec.Value = value);
                });
                row.Column([FormField.Root, "flex-1"], content: field =>
                {
                    // Empty column to balance the layout
                });
            });
        });
    }
}
