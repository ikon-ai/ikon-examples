public partial class Validation
{
    private const int TelSampleRate = 16000;
    private const double TelToneHz = 1000;
    private static readonly TimeSpan TelRingTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TelEchoLimit = TimeSpan.FromSeconds(60);

    // Outbound call
    private readonly Reactive<string> _telCallTo = new("");
    private readonly Reactive<string> _telCallFrom = new("auto");
    private readonly Reactive<string> _telCallLine = new("This is the Ikon validation app calling. One, two, three.");
    private readonly Reactive<bool> _telCallBusy = new(false);
    private readonly Reactive<string> _telCallInfo = new("");
    private readonly Reactive<string> _telCallEnergy = new("");
    private readonly Reactive<string> _telCallResult = new("");
    private readonly Reactive<bool> _telCallLive = new(false);
    private IVoiceCall? _telActiveCall;
    private CancellationTokenSource? _telActiveCallCts;

    // Incoming
    private readonly Reactive<string> _telIncomingMode = new("tone");
    private readonly ReactiveList<TelCallEntry> _telCallLog = new();

    private void RenderTelephonyCallCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Place a call");

            view.Column([Layout.Column.Md], content: view =>
            {
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Box([FormField.Root, "flex-1 min-w-[200px]"], content: view =>
                    {
                        view.Text([FormField.Label], "To (E.164)");
                        view.TextField([Input.Default],
                            value: _telCallTo.Value,
                            placeholder: "+358401234567",
                            disabled: _telCallLive.Value,
                            props: TestId("tel-call-to"),
                            onValueChange: async v => _telCallTo.Value = (v ?? "").Trim());
                    });

                    view.Box([FormField.Root, "flex-1 min-w-[200px]"], content: view =>
                    {
                        view.Text([FormField.Label], "From");
                        view.Select(
                            value: _telCallFrom.Value,
                            options: TelFromOptions(),
                            disabled: _telCallLive.Value,
                            props: TestId("tel-call-from"),
                            onValueChange: async v => _telCallFrom.Value = v ?? "auto");
                    });
                });

                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Line to speak");
                    view.TextField([Input.Default],
                        value: _telCallLine.Value,
                        props: TestId("tel-call-line"),
                        onValueChange: async v => _telCallLine.Value = v ?? "");
                });

                view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: _telCallBusy.Value ? "Ringing…" : "Call",
                        disabled: _telCallBusy.Value || _telCallLive.Value || !IsE164(_telCallTo.Value),
                        props: TestId("tel-call-place"),
                        onClick: PlaceTelephonyCallAsync);

                    view.Button([Button.PrimaryMd],
                        text: "Speak line",
                        disabled: !_telCallLive.Value,
                        props: TestId("tel-call-speak"),
                        onClick: async () => await SpeakOnActiveCallAsync(tone: false));

                    view.Button([Button.PrimaryMd],
                        text: "Play tone",
                        disabled: !_telCallLive.Value,
                        props: TestId("tel-call-tone"),
                        onClick: async () => await SpeakOnActiveCallAsync(tone: true));

                    view.Button([Button.OutlineMd],
                        text: "Interrupt",
                        disabled: !_telCallLive.Value,
                        props: TestId("tel-call-interrupt"),
                        onClick: InterruptActiveCallAsync);

                    view.Button([Button.ErrorMd],
                        text: "Hang up",
                        disabled: !_telCallLive.Value,
                        props: TestId("tel-call-hangup"),
                        onClick: HangUpActiveCallAsync);
                });

                RenderTelephonyBrowserControls(view);

                if (_telCallInfo.Value.Length > 0)
                {
                    view.Text([Text.Caption], _telCallInfo.Value, props: TestId("tel-call-info"));
                }

                if (_telCallEnergy.Value.Length > 0)
                {
                    view.Text([Text.Caption], _telCallEnergy.Value, props: TestId("tel-call-energy"));
                }

                if (_telCallResult.Value.Length > 0)
                {
                    view.Text([Text.Body], _telCallResult.Value, props: TestId("tel-call-result"));
                }
            });
        });
    }

    private async Task PlaceTelephonyCallAsync()
    {
        _telCallBusy.Value = true;
        _telCallResult.Value = "";
        _telCallEnergy.Value = "";
        _telCallInfo.Value = $"Ringing {_telCallTo.Value}…";
        var started = DateTime.UtcNow;

        try
        {
            var call = await app.Telephony.CallAsync(_telCallTo.Value, TelRingTimeout, TelFrom(_telCallFrom.Value));
            var cts = new CancellationTokenSource();
            _telActiveCall = call;
            _telActiveCallCts = cts;
            _telCallLive.Value = true;
            _telCallInfo.Value = $"{DescribeCall(call)} connected after {(DateTime.UtcNow - started).TotalMilliseconds:0} ms";
            OpenBrowserMicrophone();
            _ = MeterActiveCallAsync(call, cts.Token);
        }
        catch (TelephonyNumberNotAvailableException ex)
        {
            _telCallInfo.Value = "";
            _telCallResult.Value = $"Error: {TelNoNumbersText} ({ex.Message})";
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Validation call to {_telCallTo.Value} failed: {ex.GetType().Name}: {ex.Message}");
            _telCallInfo.Value = "";
            _telCallResult.Value = $"Error: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _telCallBusy.Value = false;
        }
    }

    private static string DescribeCall(IVoiceCall call)
    {
        return $"call {call.CallId} from={(call.From.Length == 0 ? "(placed by app)" : call.From)} to={call.To} connected={call.IsConnected}";
    }

    private async Task MeterActiveCallAsync(IVoiceCall call, CancellationToken ct)
    {
        var meter = new TelAudioMeter(TelSampleRate, TelToneHz);
        var lastPublish = DateTime.MinValue;
        var browserStream = $"tel-call-{call.CallId}";

        try
        {
            await foreach (var frame in call.ListenAsync(TelSampleRate, ct))
            {
                meter.Add(frame);
                await SendCallAudioToListenersAsync(browserStream, frame, false);

                if (DateTime.UtcNow - lastPublish > TimeSpan.FromMilliseconds(250))
                {
                    lastPublish = DateTime.UtcNow;
                    _telCallEnergy.Value = meter.Describe();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Hang-up from this side cancels the listen; the summary below is still written.
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Listening on validation call {call.CallId} failed: {ex.GetType().Name}: {ex.Message}");
        }

        CloseBrowserMicrophone();
        await SendCallAudioToListenersAsync(browserStream, new float[TelSampleRate / 50], true);
        await CloseCallAudioStreamAsync(browserStream);

        _telCallEnergy.Value = meter.Describe();
        _telCallLive.Value = false;
        _telCallInfo.Value = $"{DescribeCall(call)} — ended";
        _telCallResult.Value = $"{(meter.SoundSeconds > 0 ? "PASS" : "FAIL")} call ended: {meter.Describe()}";
    }

    private async Task InterruptActiveCallAsync()
    {
        var call = _telActiveCall;

        if (call == null || !call.IsConnected)
        {
            _telCallResult.Value = "Error: no call is connected";
            return;
        }

        try
        {
            await call.InterruptAsync();
        }
        catch (Exception ex)
        {
            _telCallResult.Value = $"Error: interrupt failed: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private async Task SpeakOnActiveCallAsync(bool tone)
    {
        var call = _telActiveCall;

        if (call == null || !call.IsConnected)
        {
            _telCallResult.Value = "Error: no call is connected";
            return;
        }

        try
        {
            if (tone)
            {
                await call.SpeakAsync(TelTone(TimeSpan.FromSeconds(2)));
            }
            else
            {
                await SpeakTextOnCallAsync(call, _telCallLine.Value, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _telCallResult.Value = $"Error: speaking failed: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private static async Task SpeakTextOnCallAsync(IVoiceCall call, string text, CancellationToken ct)
    {
        using var generator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
        await call.SpeakAsync(generator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = text }, ct), ct);
    }

    private async Task HangUpActiveCallAsync()
    {
        var call = _telActiveCall;
        _telActiveCall = null;

        // The microphone's open stream counts as speech still being queued, which hanging up waits for.
        CloseBrowserMicrophone();

        try
        {
            if (call != null)
            {
                await call.HangUpAsync();
                await call.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _telCallResult.Value = $"Error: hang-up failed: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _telActiveCallCts?.Cancel();
            _telActiveCallCts?.Dispose();
            _telActiveCallCts = null;
            _telCallLive.Value = false;
        }
    }

    private void RenderTelephonyIncomingCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Incoming calls");

            view.Box([FormField.Root, "max-w-md"], content: view =>
            {
                view.Text([FormField.Label], "Mode");
                view.Select(
                    value: _telIncomingMode.Value,
                    options:
                    [
                        new SelectOption("browser", "Browser"),
                        new SelectOption("tone", "Tone"),
                        new SelectOption("echo", "Echo")
                    ],
                    props: TestId("tel-incoming-mode"),
                    onValueChange: async v => _telIncomingMode.Value = v ?? "tone");
            });

            if (_telCallLog.Count == 0)
            {
                view.Text([Text.Caption, "text-muted-foreground mt-3"], "No calls");
                return;
            }

            view.Column([Layout.Column.Sm, "mt-3"], content: view =>
            {
                foreach (var entry in _telCallLog)
                {
                    view.Text([Text.Caption], $"{entry.AtUtc:HH:mm:ss} {entry.Mode} {entry.From} → {entry.To} ({entry.CallId}): {entry.Result}", key: $"{entry.CallId}-{entry.Result.Length}", props: TestId("tel-call-log-item"));
                }
            });
        });
    }

    private async Task HandleIncomingCallAsync(IVoiceCall call)
    {
        if (TryTakeLoopCall(call, out var loop))
        {
            await AnswerLoopCallAsync(call, loop);
            return;
        }

        string mode = _telIncomingMode.Value;
        string result;

        try
        {
            result = mode switch
            {
                "echo" => await AnswerEchoAsync(call),
                "browser" => await AnswerInBrowserAsync(call),
                _ => await AnswerToneAsync(call)
            };
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Validation incoming call {call.CallId} from {call.From} failed in {mode} mode: {ex.GetType().Name}: {ex.Message}");
            result = $"Error: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            try
            {
                await call.HangUpAsync();
            }
            catch (Exception ex)
            {
                // The caller hanging up first is the ordinary end of these calls.
                Log.Instance.Debug($"Hang-up of validation call {call.CallId} after the handler: {ex.Message}");
            }
        }

        AddCallLog(new TelCallEntry(DateTime.UtcNow, mode, call.CallId, call.From, call.To, result));
    }

    private void AddCallLog(TelCallEntry entry)
    {
        _telCallLog.Insert(0, entry);

        while (_telCallLog.Count > TelLogCap)
        {
            _telCallLog.RemoveAt(_telCallLog.Count - 1);
        }
    }

    // The call becomes the card's live call, so the browser hears it and the card's buttons act on it.
    private async Task<string> AnswerInBrowserAsync(IVoiceCall call)
    {
        // One live call at a time: the card's buttons and the browser audio act on a single call, and
        // a call this card is placing — to one of the space's own numbers, say — already holds them.
        if (_telCallLive.Value || _telCallBusy.Value)
        {
            return await AnswerToneAsync(call);
        }

        using var cts = new CancellationTokenSource();
        _telActiveCall = call;
        _telActiveCallCts = cts;
        _telCallLive.Value = true;
        _telCallResult.Value = "";
        _telCallInfo.Value = $"{DescribeCall(call)} answered";
        OpenBrowserMicrophone();

        await MeterActiveCallAsync(call, cts.Token);

        _telActiveCall = null;
        _telActiveCallCts = null;
        return _telCallEnergy.Value;
    }

    private async Task<string> AnswerToneAsync(IVoiceCall call)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var meter = new TelAudioMeter(TelSampleRate, TelToneHz);
        var listening = ListenIntoAsync(call, meter, cts.Token);

        await call.SpeakAsync(TelTone(TimeSpan.FromSeconds(2)), cts.Token);
        await SpeakTextOnCallAsync(call, "Ikon validation app. You are heard. Goodbye.", cts.Token);
        await call.WaitForPlaybackAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
        await cts.CancelAsync();
        await listening;

        return meter.Describe();
    }

    private async Task<string> AnswerEchoAsync(IVoiceCall call)
    {
        using var cts = new CancellationTokenSource(TelEchoLimit);
        var meter = new TelAudioMeter(TelSampleRate, TelToneHz);
        var frames = Channel.CreateBounded<float[]>(new BoundedChannelOptions(200) { FullMode = BoundedChannelFullMode.DropOldest });

        async IAsyncEnumerable<AudioChunk> Echo()
        {
            await foreach (var frame in frames.Reader.ReadAllAsync(CancellationToken.None))
            {
                yield return new AudioChunk("echo", frame, TelSampleRate, 1, false, false);
            }
        }

        var speaking = call.SpeakAsync(Echo(), cts.Token);

        try
        {
            await foreach (var frame in call.ListenAsync(TelSampleRate, cts.Token))
            {
                meter.Add(frame);
                frames.Writer.TryWrite(frame.ToArray());
            }
        }
        catch (OperationCanceledException)
        {
            // The echo limit ran out; the call is hung up by the caller of this method.
        }
        finally
        {
            frames.Writer.TryComplete();
        }

        try
        {
            await speaking;
        }
        catch (OperationCanceledException)
        {
            // The echo limit ran out while audio was still being sent back.
        }

        return meter.Describe();
    }

    private static async Task ListenIntoAsync(IVoiceCall call, TelAudioMeter meter, CancellationToken ct)
    {
        try
        {
            await foreach (var frame in call.ListenAsync(TelSampleRate, ct))
            {
                meter.Add(frame);
            }
        }
        catch (OperationCanceledException)
        {
            // Listening ends when its window closes; the meter holds what was heard until then.
        }
    }

    // A sine at the probe frequency, in 20 ms chunks, eased in and out so the line does not click.
    private static async IAsyncEnumerable<AudioChunk> TelTone(TimeSpan duration)
    {
        int total = (int)(TelSampleRate * duration.TotalSeconds);
        int chunk = TelSampleRate / 50;
        int fade = TelSampleRate / 100;
        var id = $"tone-{Guid.NewGuid():N}";

        for (int start = 0; start < total; start += chunk)
        {
            int count = Math.Min(chunk, total - start);
            var samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                int n = start + i;
                double gain = Math.Min(1.0, Math.Min(n, total - n) / (double)fade);
                samples[i] = (float)(0.4 * gain * Math.Sin(2 * Math.PI * TelToneHz * n / TelSampleRate));
            }

            yield return new AudioChunk(id, samples, TelSampleRate, 1, start == 0, start + count >= total);
        }

        await Task.CompletedTask;
    }
}

public sealed record TelCallEntry(DateTime AtUtc, string Mode, string CallId, string From, string To, string Result);

// What one side of a call heard: how much audio, how much of it carried a voice or a sound, and how
// much of it was the probe tone. The tone test is a Goertzel filter over 100 ms blocks — the share
// of a block's energy at the probe frequency — so a noisy line or speech cannot pass for the tone.
internal sealed class TelAudioMeter(int sampleRate, double toneHz)
{
    private const double SoundRms = 0.01;
    private const double ToneShare = 0.5;

    private readonly int _block = sampleRate / 10;
    private readonly double _coefficient = 2 * Math.Cos(2 * Math.PI * toneHz / sampleRate);
    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private readonly object _sync = new();
    private double _s1;
    private double _s2;
    private double _sumSquares;
    private int _inBlock;
    private long _samples;
    private int _soundBlocks;
    private int _toneBlocks;
    private double _peakRms;
    private DateTime? _firstToneUtc;

    public double HeardSeconds
    {
        get
        {
            lock (_sync)
            {
                return (double)_samples / sampleRate;
            }
        }
    }

    public double SoundSeconds
    {
        get
        {
            lock (_sync)
            {
                return _soundBlocks * 0.1;
            }
        }
    }

    public double ToneSeconds
    {
        get
        {
            lock (_sync)
            {
                return _toneBlocks * 0.1;
            }
        }
    }

    public double? FirstToneAfterMs
    {
        get
        {
            lock (_sync)
            {
                return _firstToneUtc is { } at ? (at - _startedUtc).TotalMilliseconds : null;
            }
        }
    }

    public void Add(ReadOnlySpan<float> frame)
    {
        lock (_sync)
        {
            foreach (float sample in frame)
            {
                double s0 = sample + _coefficient * _s1 - _s2;
                _s2 = _s1;
                _s1 = s0;
                _sumSquares += sample * sample;
                _inBlock++;
                _samples++;

                if (_inBlock == _block)
                {
                    CloseBlock();
                }
            }
        }
    }

    public void Add(float[] frame)
    {
        Add(frame.AsSpan());
    }

    public string Describe()
    {
        lock (_sync)
        {
            return $"heard {(double)_samples / sampleRate:0.0} s, sound {_soundBlocks * 0.1:0.0} s, {toneHz:0} Hz tone {_toneBlocks * 0.1:0.0} s, peak rms {_peakRms:0.000}";
        }
    }

    private void CloseBlock()
    {
        double rms = Math.Sqrt(_sumSquares / _block);
        double tonePower = _s2 * _s2 + _s1 * _s1 - _coefficient * _s1 * _s2;
        double share = _sumSquares > 0 ? tonePower / (_sumSquares * _block / 2.0) : 0;

        if (rms > _peakRms)
        {
            _peakRms = rms;
        }

        if (rms > SoundRms)
        {
            _soundBlocks++;

            if (share > ToneShare)
            {
                _toneBlocks++;
                _firstToneUtc ??= DateTime.UtcNow;
            }
        }

        _s1 = 0;
        _s2 = 0;
        _sumSquares = 0;
        _inBlock = 0;
    }
}
