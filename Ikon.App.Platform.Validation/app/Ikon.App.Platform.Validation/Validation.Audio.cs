public partial class Validation
{
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

    private void RenderAudioSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Audio Metrics
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Audio Metrics");
                view.Text([Text.Caption, "mb-4"], "Real-time audio stream statistics");

                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Row([Layout.Row.Md], content: view =>
                    {
                        view.Text([Text.BodyStrong, "w-32"], "Streams");
                        view.Text([Text.Body], _audioStreamCount.Value.ToString());
                    });
                    view.Row([Layout.Row.Md], content: view =>
                    {
                        view.Text([Text.BodyStrong, "w-32"], "CPU");
                        view.Text([Text.Body], $"{_audioCpuUsagePercent.Value:F1}%");
                    });
                    view.Row([Layout.Row.Md], content: view =>
                    {
                        view.Text([Text.BodyStrong, "w-32"], "IPD");
                        view.Text([Text.Body], $"{_audioMinIpdMs.Value:F1} / {_audioAvgIpdMs.Value:F1} / {_audioMaxIpdMs.Value:F1} ms (min/avg/max)");
                    });
                    view.Row([Layout.Row.Md], content: view =>
                    {
                        view.Text([Text.BodyStrong, "w-32"], "Jitter");
                        view.Text([Text.Body], $"{_audioJitterMs.Value:F2} ms");
                    });
                });
            });

            // Audio Sources
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Audio Sources");
                view.Text([Text.Caption, "mb-4"], "Add or remove synthesized audio sources");

                view.Column([Layout.Column.Md], content: view =>
                {
                    // Sine Wave
                    view.Text([Text.BodyStrong, "mb-2"], $"Sine Wave ({_sineWaveIds.Value.Count})");
                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.Button([Button.PrimaryMd],
                            label: "Add Sine Wave",
                            onClick: async () =>
                            {
                                var source = new SineWaveSource(_sineWaveIds.Value.Count);
                                var streamId = AudioGenerator.AddSource(source);
                                _sineWaveIds.Value.Add(streamId);
                                _sineWaveIds.NotifyUpdate();
                            });

                        view.Button([Button.DangerMd],
                            label: "Remove",
                            onClick: async () =>
                            {
                                if (_sineWaveIds.Value.Count > 0)
                                {
                                    var streamId = _sineWaveIds.Value[^1];
                                    _sineWaveIds.Value.RemoveAt(_sineWaveIds.Value.Count - 1);
                                    AudioGenerator.RemoveSource(streamId);
                                    _sineWaveIds.NotifyUpdate();
                                }
                            });
                    });

                    // Drum Machine
                    view.Text([Text.BodyStrong, "mb-2"], $"Drum Machine ({_drumMachineIds.Value.Count})");
                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.Button([Button.PrimaryMd],
                            label: "Add Drum Machine",
                            onClick: async () =>
                            {
                                double bpm = 120 + _drumMachineIds.Value.Count * 10;
                                var source = new DrumMachineSource(bpm);
                                var streamId = AudioGenerator.AddSource(source);
                                _drumMachineIds.Value.Add(streamId);
                                _drumMachineIds.NotifyUpdate();
                            });

                        view.Button([Button.DangerMd],
                            label: "Remove",
                            onClick: async () =>
                            {
                                if (_drumMachineIds.Value.Count > 0)
                                {
                                    var streamId = _drumMachineIds.Value[^1];
                                    _drumMachineIds.Value.RemoveAt(_drumMachineIds.Value.Count - 1);
                                    AudioGenerator.RemoveSource(streamId);
                                    _drumMachineIds.NotifyUpdate();
                                }
                            });
                    });

                    // Moog Synth
                    view.Text([Text.BodyStrong, "mb-2"], $"Moog Synth ({_moogSynthIds.Value.Count})");
                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.Button([Button.PrimaryMd],
                            label: "Add Synth",
                            onClick: async () =>
                            {
                                var patch = Patches[_moogSynthIds.Value.Count % Patches.Length];
                                var source = new MoogSynthSource(patch);

                                if (_generativeMode.Value)
                                {
                                    source.SetSequencerMode(SequencerMode.Generative);
                                }

                                var streamId = AudioGenerator.AddSource(source);
                                _moogSynthIds.Value.Add(streamId);
                                _moogSynthIds.NotifyUpdate();
                                UpdateSynthInfo();
                            });

                        view.Button([Button.DangerMd],
                            label: "Remove",
                            onClick: async () =>
                            {
                                if (_moogSynthIds.Value.Count > 0)
                                {
                                    var streamId = _moogSynthIds.Value[^1];
                                    _moogSynthIds.Value.RemoveAt(_moogSynthIds.Value.Count - 1);
                                    AudioGenerator.RemoveSource(streamId);
                                    _moogSynthIds.NotifyUpdate();
                                    UpdateSynthInfo();
                                }
                            });
                    });

                    // Synth Controls
                    if (_moogSynthIds.Value.Count > 0)
                    {
                        view.Text([Text.Caption, "mb-2"], $"Patch: {_currentPatch.Value} | Pattern: {_currentPattern.Value}");
                        view.Row([Layout.Row.Md, "flex-wrap mb-2"], content: view =>
                        {
                            view.Button([Button.SecondaryMd],
                                label: "Next Patch",
                                onClick: async () => CycleSynthPatch());

                            view.Button([Button.SecondaryMd],
                                label: "Next Pattern",
                                onClick: async () => CycleSynthPattern());

                            view.Button(
                                [_generativeMode.Value ? Button.PrimaryMd : Button.OutlineMd],
                                label: _generativeMode.Value ? "Generative Mode" : "Pattern Mode",
                                onClick: async () =>
                                {
                                    _generativeMode.Value = !_generativeMode.Value;
                                    SetSynthMode(_generativeMode.Value ? SequencerMode.Generative : SequencerMode.Pattern);
                                });
                        });
                    }
                });
            });

            // Audio Effects
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Audio Effects");
                view.Text([Text.Caption, "mb-4"], "Apply effects to audio output");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.BodyStrong, "mb-2"], $"Active Effects ({_activeEffects.Value.Count})");
                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        foreach (var effectType in EffectTypes)
                        {
                            view.Button([Button.OutlineSm],
                                label: effectType,
                                onClick: async () => AddEffect(effectType));
                        }
                    });

                    view.Button([Button.DangerMd],
                        label: "Clear All Effects",
                        onClick: async () => ClearAllEffects());

                    if (_activeEffects.Value.Count > 0)
                    {
                        view.Separator([Separator.Horizontal, "my-4"]);

                        for (int i = 0; i < _activeEffects.Value.Count; i++)
                        {
                            var index = i;
                            var entry = _activeEffects.Value[i];
                            view.Box([Card.Elevated, "p-4 mb-2"], content: view =>
                            {
                                view.Row([Card.HeaderRow], content: view =>
                                {
                                    view.Text([Text.BodyStrong], entry.EffectType);
                                    view.Button([Button.DangerMd],
                                        label: "Remove",
                                        onClick: async () => RemoveEffect(index));
                                });
                                RenderEffectParams(view, entry, index);
                            });
                        }
                    }
                });
            });

            // Audio Recording
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Audio Recording & Echo");
                view.Text([Text.Caption, "mb-4"], "Record audio and echo it back through effects");

                view.Column([Layout.Column.Md], content: view =>
                {
                    var micDevices = _availableDevices.Value
                        .Where(d => d.Kind == "audioinput" && !string.IsNullOrEmpty(d.DeviceId) && d.DeviceId != "default" && d.DeviceId != "communications")
                        .Select(d => new SelectOption(d.DeviceId, string.IsNullOrEmpty(d.Label) ? "Microphone" : d.Label))
                        .ToArray();
                    var micOptions = new[] { new SelectOption("default", "Default") }.Concat(micDevices).ToArray();

                    view.Row([Layout.Row.Md, "flex-wrap mb-4 items-end"], content: view =>
                    {
                        view.Column(["flex-1 min-w-[200px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Microphone");
                            view.Select(
                                value: _selectedMicrophoneId.Value,
                                options: micOptions,
                                disabled: _devicesLoading.Value,
                                onValueChange: async v => _selectedMicrophoneId.Value = v);
                        });
                        view.Button([Button.OutlineMd, Button.Size.Icon],
                            disabled: _devicesLoading.Value,
                            onClick: async () =>
                            {
                                var clientScope = ReactiveScope.TryGet<ClientScope>();

                                if (clientScope != null)
                                {
                                    await RefreshDevicesAsync(clientScope.Value.Id);
                                }
                            },
                            content: v => v.Icon([Icon.Default], name: "refresh-cw"));
                        view.Column(["flex-1 min-w-[120px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Bitrate (Kbps)");
                            view.TextField([Input.Default], value: _audioBitrate.Value, type: "number", step: "1", min: "8",
                                onValueChange: async v => _audioBitrate.Value = v);
                        });
                    });

                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.Row([Layout.Row.InlineCenter], content: view =>
                        {
                            view.Switch(
                                [Switch.Default],
                                @checked: _audioEchoCancellation.Value,
                                onCheckedChange: value =>
                                {
                                    _audioEchoCancellation.Value = value;
                                    return Task.CompletedTask;
                                },
                                content: view => view.SwitchThumb([Switch.Thumb]));
                            view.Text([Text.Caption], "Echo Cancellation");
                        });

                        view.Row([Layout.Row.InlineCenter], content: view =>
                        {
                            view.Switch(
                                [Switch.Default],
                                @checked: _audioNoiseSuppression.Value,
                                onCheckedChange: value =>
                                {
                                    _audioNoiseSuppression.Value = value;
                                    return Task.CompletedTask;
                                },
                                content: view => view.SwitchThumb([Switch.Thumb]));
                            view.Text([Text.Caption], "Noise Suppression");
                        });

                        view.Row([Layout.Row.InlineCenter], content: view =>
                        {
                            view.Switch(
                                [Switch.Default],
                                @checked: _audioAutoGainControl.Value,
                                onCheckedChange: value =>
                                {
                                    _audioAutoGainControl.Value = value;
                                    return Task.CompletedTask;
                                },
                                content: view => view.SwitchThumb([Switch.Thumb]));
                            view.Text([Text.Caption], "Auto Gain Control");
                        });
                    });

                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.CaptureButton(
                            [_isAudioHoldRecording.Value ? Button.DangerMd : Button.PrimaryMd],
                            kind: MediaCaptureKind.Audio,
                            label: "Hold to Record",
                            captureMode: MediaCaptureButtonMode.Hold,
                            audioOptions: new ClientAudioCaptureOptions
                            {
                                AutoGainControl = _audioAutoGainControl.Value,
                                NoiseSuppression = _audioNoiseSuppression.Value,
                                EchoCancellation = _audioEchoCancellation.Value,
                                Bitrate = ParseBitrateKbps(_audioBitrate.Value),
                                DeviceId = GetSelectedDeviceId(_selectedMicrophoneId.Value),
                                TargetIds = [app.ClientContext.SessionId]
                            },
                            onCaptureStart: async e => _isAudioHoldRecording.Value = true,
                            onCaptureStop: async e => _isAudioHoldRecording.Value = false);

                        view.CaptureButton(
                            [_isAudioHoldRecording.Value ? Button.DangerMd : Button.PrimaryMd, Button.Size.Icon],
                            kind: MediaCaptureKind.Audio,
                            label: "Hold to Record",
                            captureMode: MediaCaptureButtonMode.Hold,
                            audioOptions: new ClientAudioCaptureOptions
                            {
                                AutoGainControl = _audioAutoGainControl.Value,
                                NoiseSuppression = _audioNoiseSuppression.Value,
                                EchoCancellation = _audioEchoCancellation.Value,
                                Bitrate = ParseBitrateKbps(_audioBitrate.Value),
                                DeviceId = GetSelectedDeviceId(_selectedMicrophoneId.Value),
                                TargetIds = [app.ClientContext.SessionId]
                            },
                            onCaptureStart: async e => _isAudioHoldRecording.Value = true,
                            onCaptureStop: async e => _isAudioHoldRecording.Value = false,
                            content: v => v.Icon([Icon.Default], name: "mic"));

                        view.CaptureButton(
                            [_isAudioToggleRecording.Value ? Button.DangerMd : Button.SecondaryMd],
                            kind: MediaCaptureKind.Audio,
                            label: _isAudioToggleRecording.Value ? "Stop Recording" : "Toggle Record",
                            captureMode: MediaCaptureButtonMode.Toggle,
                            audioOptions: new ClientAudioCaptureOptions
                            {
                                AutoGainControl = _audioAutoGainControl.Value,
                                NoiseSuppression = _audioNoiseSuppression.Value,
                                EchoCancellation = _audioEchoCancellation.Value,
                                Bitrate = ParseBitrateKbps(_audioBitrate.Value),
                                DeviceId = GetSelectedDeviceId(_selectedMicrophoneId.Value),
                                TargetIds = [app.ClientContext.SessionId]
                            },
                            onCaptureStart: async e => _isAudioToggleRecording.Value = true,
                            onCaptureStop: async e => _isAudioToggleRecording.Value = false);

                        view.CaptureButton(
                            [_isAudioToggleRecording.Value ? Button.DangerMd : Button.SecondaryMd, Button.Size.Icon],
                            kind: MediaCaptureKind.Audio,
                            label: _isAudioToggleRecording.Value ? "Stop Recording" : "Toggle Record",
                            captureMode: MediaCaptureButtonMode.Toggle,
                            audioOptions: new ClientAudioCaptureOptions
                            {
                                AutoGainControl = _audioAutoGainControl.Value,
                                NoiseSuppression = _audioNoiseSuppression.Value,
                                EchoCancellation = _audioEchoCancellation.Value,
                                Bitrate = ParseBitrateKbps(_audioBitrate.Value),
                                DeviceId = GetSelectedDeviceId(_selectedMicrophoneId.Value),
                                TargetIds = [app.ClientContext.SessionId]
                            },
                            onCaptureStart: async e => _isAudioToggleRecording.Value = true,
                            onCaptureStop: async e => _isAudioToggleRecording.Value = false,
                            content: v => v.Icon([Icon.Default], name: "circle"));
                    });

                    view.Row([Layout.Row.InlineCenter, "mb-4"], content: view =>
                    {
                        view.Switch(
                            [Switch.Default],
                            @checked: _audioPlaybackEnabled.Value,
                            onCheckedChange: value =>
                            {
                                _audioPlaybackEnabled.Value = value;
                                return Task.CompletedTask;
                            },
                            content: view => view.SwitchThumb([Switch.Thumb]));
                        view.Text([Text.Body], "Echo Playback");
                    });

                    // Voice Effects
                    view.Text([Text.BodyStrong, "mb-2"], $"Voice Effects ({_voiceEffects.Value.Count})");
                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        foreach (var effectType in EffectTypes)
                        {
                            view.Button([Button.OutlineSm],
                                label: effectType,
                                onClick: async () => AddVoiceEffect(effectType));
                        }
                    });

                    view.Button([Button.DangerMd],
                        label: "Clear Voice Effects",
                        onClick: async () => ClearVoiceEffects());

                    if (_voiceEffects.Value.Count > 0)
                    {
                        view.Separator([Separator.Horizontal, "my-4"]);

                        for (int i = 0; i < _voiceEffects.Value.Count; i++)
                        {
                            var index = i;
                            var entry = _voiceEffects.Value[i];
                            view.Box([Card.Elevated, "p-4 mb-2"], content: view =>
                            {
                                view.Row([Card.HeaderRow], content: view =>
                                {
                                    view.Text([Text.BodyStrong], entry.EffectType);
                                    view.Button([Button.DangerMd],
                                        label: "Remove",
                                        onClick: async () => RemoveVoiceEffect(index));
                                });
                                RenderVoiceEffectParams(view, entry, index);
                            });
                        }
                    }
                });
            });

            // Sound Functions
            view.Box([Card.Default, "p-6"], content: view =>
            {
                var clientSessionId = ReactiveScope.ClientId;

                view.Text([Text.H2, "mb-4"], "Sound Functions");
                view.Text([Text.Caption, "mb-4"], "Play sounds using ClientFunctions.PlaySoundAsync (server-triggered playback)");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.BodyStrong, "mb-2"], $"Last Playback ID: {_lastSoundPlaybackId.Value}");
                    view.Row([Layout.Row.Md, "flex-wrap mb-4"], content: view =>
                    {
                        view.Button([Button.PrimaryMd],
                            label: "Play from byte[]",
                            onClick: async () =>
                            {
                                var soundPath = Path.Combine(app.DataDirectory, "whoosh.mp3");
                                var soundData = await File.ReadAllBytesAsync(soundPath);
                                var playbackId = await ClientFunctions.PlaySoundAsync(clientSessionId, soundData, "audio/mpeg", volume: 1.0);
                                _lastSoundPlaybackId.Value = playbackId ?? "(failed)";
                                _soundToastMessage.Value = playbackId != null ? $"Playing from byte[]: {playbackId}" : "Failed to play sound";
                                _soundToastOpen.Value = true;
                            });

                        view.Button([Button.SecondaryMd],
                            label: "Play from data URL",
                            onClick: async () =>
                            {
                                var soundPath = Path.Combine(app.DataDirectory, "whoosh.mp3");
                                var soundData = await File.ReadAllBytesAsync(soundPath);
                                var base64 = Convert.ToBase64String(soundData);
                                var dataUrl = $"data:audio/mpeg;base64,{base64}";
                                var playbackId = await ClientFunctions.PlaySoundAsync(clientSessionId, dataUrl, volume: 0.8);
                                _lastSoundPlaybackId.Value = playbackId ?? "(failed)";
                                _soundToastMessage.Value = playbackId != null ? $"Playing from data URL: {playbackId}" : "Failed to play sound";
                                _soundToastOpen.Value = true;
                            });

                        view.Button([Button.OutlineMd],
                            label: "Play from URL",
                            onClick: async () =>
                            {
                                var playbackId = await ClientFunctions.PlaySoundAsync(clientSessionId, "https://cdn.freesound.org/previews/256/256113_3263906-lq.mp3", volume: 0.6);
                                _lastSoundPlaybackId.Value = playbackId ?? "(failed)";
                                _soundToastMessage.Value = playbackId != null ? $"Playing from URL: {playbackId}" : "Failed to play sound";
                                _soundToastOpen.Value = true;
                            });

                        view.Button([Button.DangerMd],
                            label: "Stop Sound",
                            onClick: async () =>
                            {
                                if (_lastSoundPlaybackId.Value != "(no sound playing)" && _lastSoundPlaybackId.Value != "(failed)")
                                {
                                    var stopped = await ClientFunctions.StopSoundAsync(clientSessionId, _lastSoundPlaybackId.Value);
                                    _soundToastMessage.Value = stopped ? $"Stopped: {_lastSoundPlaybackId.Value}" : "Sound not found or already stopped";
                                    _lastSoundPlaybackId.Value = "(no sound playing)";
                                }
                                else
                                {
                                    _soundToastMessage.Value = "No sound to stop";
                                }

                                _soundToastOpen.Value = true;
                            });
                    });

                    view.Text([Text.Caption, "text-muted-foreground"], "Note: byte[] and data URL use caching - same audio is only transmitted once per session");
                });

                view.Toast(
                    viewportStyle: [Toast.ViewportBottomCenter],
                    open: _soundToastOpen.Value,
                    onOpenChange: async open => _soundToastOpen.Value = open ?? false,
                    durationMs: 3000,
                    toastStyle: [Toast.Base],
                    title: "Sound",
                    titleStyle: [Toast.Title],
                    description: _soundToastMessage.Value,
                    descriptionStyle: [Toast.Description],
                    showClose: true,
                    closeStyle: [Toast.Close]);
            });
        });
    }

    private void RenderEffectParams(UIView view, EffectEntry entry, int index)
    {
        foreach (var param in entry.Params)
        {
            var paramKey = param.Key;
            view.Row([FormField.ParamRow], content: view =>
            {
                view.Text([Text.Caption, "w-20"], paramKey);
                view.TextField(
                    [Input.Default, "w-24"],
                    value: param.Value.Value.ToString("F2"),
                    type: "number",
                    onValueChange: value =>
                    {
                        if (float.TryParse(value, out var v))
                        {
                            UpdateEffectParam(index, paramKey, v);
                        }

                        return Task.CompletedTask;
                    });
            });
        }
    }

    private void RenderVoiceEffectParams(UIView view, EffectEntry entry, int index)
    {
        foreach (var param in entry.Params)
        {
            var paramKey = param.Key;
            view.Row([FormField.ParamRow], content: view =>
            {
                view.Text([Text.Caption, "w-20"], paramKey);
                view.TextField(
                    [Input.Default, "w-24"],
                    value: param.Value.Value.ToString("F2"),
                    type: "number",
                    onValueChange: value =>
                    {
                        if (float.TryParse(value, out var v))
                        {
                            UpdateVoiceEffectParam(index, paramKey, v);
                        }

                        return Task.CompletedTask;
                    });
            });
        }
    }

    private void UpdateSynthInfo()
    {
        if (_moogSynthIds.Value.Count > 0)
        {
            var synth = AudioGenerator.GetSource<MoogSynthSource>(_moogSynthIds.Value[0]);

            if (synth != null)
            {
                _currentPatch.Value = synth.Synth.Patch.Name;
                _currentPattern.Value = synth.Sequencer.Pattern.Name;
            }
        }
        else
        {
            _currentPatch.Value = "None";
            _currentPattern.Value = "None";
        }
    }

    private void CycleSynthPatch()
    {
        if (_moogSynthIds.Value.Count == 0)
        {
            return;
        }

        _patchIndex = (_patchIndex + 1) % Patches.Length;
        var patch = Patches[_patchIndex];

        foreach (var (_, synth) in AudioGenerator.GetSourcesOfType<MoogSynthSource>())
        {
            synth.SetPatch(patch);
        }

        UpdateSynthInfo();
    }

    private void CycleSynthPattern()
    {
        foreach (var (_, synth) in AudioGenerator.GetSourcesOfType<MoogSynthSource>())
        {
            synth.NextPattern();
        }

        UpdateSynthInfo();
    }

    private void SetSynthMode(SequencerMode mode)
    {
        foreach (var (_, synth) in AudioGenerator.GetSourcesOfType<MoogSynthSource>())
        {
            synth.SetSequencerMode(mode);
        }
    }

    private void AddEffect(string effectType)
    {
        var defaultParams = GetDefaultParams(effectType);
        var reactiveParams = EffectEntry.ToReactiveParams(defaultParams);
        var effect = CreateEffect(effectType, defaultParams);
        var entry = new EffectEntry(effectType, effect, reactiveParams);
        _activeEffects.Value.Add(entry);
        AudioGenerator.AddEffect(effect);
        _activeEffects.NotifyUpdate();
    }

    private void RemoveEffect(int index)
    {
        if (index < 0 || index >= _activeEffects.Value.Count)
        {
            return;
        }

        _activeEffects.Value.RemoveAt(index);
        AudioGenerator.RemoveEffectAt(index);
        _activeEffects.NotifyUpdate();
    }

    private void UpdateEffectParam(int index, string paramKey, float value)
    {
        if (index < 0 || index >= _activeEffects.Value.Count)
        {
            return;
        }

        var entry = _activeEffects.Value[index];
        entry.Params[paramKey].Value = value;
        var newEffect = CreateEffect(entry.EffectType, entry.GetParamValues());
        entry.Effect = newEffect;
        AudioGenerator.ReplaceEffect(index, newEffect);
    }

    private void ClearAllEffects()
    {
        _activeEffects.Value.Clear();
        AudioGenerator.ClearEffects();
        _activeEffects.NotifyUpdate();
    }

    private void AddVoiceEffect(string effectType)
    {
        var defaultParams = GetDefaultParams(effectType);
        var reactiveParams = EffectEntry.ToReactiveParams(defaultParams);
        var effect = CreateEffect(effectType, defaultParams);
        var entry = new EffectEntry(effectType, effect, reactiveParams);
        _voiceEffects.Value.Add(entry);
        _voiceEffects.NotifyUpdate();
    }

    private void RemoveVoiceEffect(int index)
    {
        if (index < 0 || index >= _voiceEffects.Value.Count)
        {
            return;
        }

        _voiceEffects.Value.RemoveAt(index);
        _voiceEffects.NotifyUpdate();
    }

    private void UpdateVoiceEffectParam(int index, string paramKey, float value)
    {
        if (index < 0 || index >= _voiceEffects.Value.Count)
        {
            return;
        }

        var entry = _voiceEffects.Value[index];
        entry.Params[paramKey].Value = value;
        var newEffect = CreateEffect(entry.EffectType, entry.GetParamValues());
        entry.Effect = newEffect;
    }

    private void ClearVoiceEffects()
    {
        _voiceEffects.Value.Clear();
        _voiceEffects.NotifyUpdate();
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
