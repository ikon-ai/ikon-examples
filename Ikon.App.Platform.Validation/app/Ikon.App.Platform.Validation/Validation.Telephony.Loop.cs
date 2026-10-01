public partial class Validation
{
    private static readonly TimeSpan TelSmsLoopTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan TelCallLoopRing = TimeSpan.FromSeconds(40);
    private static readonly TimeSpan TelCallLoopListen = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan TelCallLoopToneLength = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan TelCallLoopCalleeWait = TimeSpan.FromSeconds(15);

    // B stays on the line past A's listen window, so A's interrupt check still has a call to run on.
    private static readonly TimeSpan TelCallLoopCalleeLine = TimeSpan.FromSeconds(25);

    // Enough of the probe tone that it cannot be a burst of line noise, well under what either side plays.
    private const double TelLoopToneSecondsNeeded = 0.5;

    // Long enough that a missed interrupt is unmistakable: playback would still be running seconds later.
    private static readonly TimeSpan TelInterruptToneLength = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TelInterruptLimit = TimeSpan.FromSeconds(4);

    private readonly Reactive<bool> _telLoopBusy = new(false);
    private readonly Reactive<string> _telLoopProgress = new("");
    private readonly ReactiveDictionary<string, string> _telLoopResults = new();
    private readonly SemaphoreSlim _telLoopGate = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<SmsMessage>> _telSmsLoopWaiters = new();
    private TelLoopCall? _telLoopCall;
    private volatile string? _telLoopRunning;

    private static readonly (string Key, string Label, bool Sms, bool Back)[] TelLoops =
    [
        ("sms", "SMS A → B", true, false),
        ("sms-back", "SMS B → A", true, true),
        ("call", "Call A → B", false, false),
        ("call-back", "Call B → A", false, true),
    ];

    private void RenderTelephonyLoopCard(UIView view)
    {
        view.Box([Card.Elevated, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Loopback");

            var pair = TelLoopPair();
            view.Text([Text.Caption, "mb-3"], pair.Skip ?? $"A {pair.A!.Number} ({pair.A.Provider}) · B {pair.B!.Number} ({pair.B.Provider})", props: TestId("tel-loop-pair"));

            view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
            {
                foreach (var loop in TelLoops)
                {
                    view.Button([Button.PrimaryMd],
                        key: loop.Key,
                        text: loop.Label,
                        disabled: _telLoopBusy.Value,
                        props: TestId($"tel-loop-{loop.Key}"),
                        onClick: async () => await RunTelephonyLoopAsync(loop.Key, loop.Sms, loop.Back));
                }

                if (_telLoopBusy.Value)
                {
                    view.Spinner();
                }
            });

            if (_telLoopProgress.Value.Length > 0)
            {
                view.Text([Text.Body, "mt-2"], _telLoopProgress.Value, props: TestId("tel-loop-progress"));
            }

            foreach (var loop in TelLoops)
            {
                if (_telLoopResults.TryGetValue(loop.Key, out var result))
                {
                    view.Text([Text.Body, "mt-2"], result, key: $"{loop.Key}-result", props: TestId($"tel-loop-{loop.Key}-result"));
                }
            }
        });
    }

    // A is the first number, B the first on another provider, so every loop crosses providers and
    // the two directions between them prove each provider's sending and receiving.
    private (TelephonyNumber? A, TelephonyNumber? B, string? Skip) TelLoopPair(string? capability = null)
    {
        var numbers = _telNumbers.Value
            .Where(n => capability == null || n.Capabilities.Count == 0 || n.Capabilities.Any(c => c.Contains(capability, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (numbers.Count < 2)
        {
            return (null, null, $"SKIP: needs two numbers{(capability == null ? "" : $" with {capability}")}, the space holds {numbers.Count}");
        }

        var a = numbers[0];
        var b = numbers.Skip(1).FirstOrDefault(n => !string.Equals(n.Provider, a.Provider, StringComparison.OrdinalIgnoreCase)) ?? numbers[1];

        return (a, b, null);
    }

    private async Task RunTelephonyLoopAsync(string key, bool sms, bool back)
    {
        // The buttons are disabled while a loop runs, but a press can get through before that renders.
        // It is told so, except on the running loop's own result line.
        if (!await _telLoopGate.WaitAsync(0))
        {
            if (key != _telLoopRunning)
            {
                _telLoopResults[key] = "SKIP: another loopback check is running";
            }

            return;
        }

        _telLoopRunning = key;

        _telLoopBusy.Value = true;
        _telLoopResults.Remove(key);
        _telLoopProgress.Value = "Reading the space's numbers…";
        bool resetAfter = false;

        try
        {
            await RefreshTelephonyAsync();

            // The tab's own first load may still be in flight, in which case the refresh above
            // returned at once; the pair is only known once it lands.
            for (int i = 0; i < 75 && _telLoading.Value; i++)
            {
                await Task.Delay(200);
            }

            var (a, b, skip) = TelLoopPair(sms ? "sms" : "voice");

            if (skip != null)
            {
                _telLoopResults[key] = skip;
                return;
            }

            var (from, to) = back ? (b!, a!) : (a!, b!);

            // Inbound goes to the shared instance unless something bound it, and a browser is in a
            // session of its own, so the receiving end would land where nothing waits for it. Put
            // back afterwards when nothing had bound it before.
            resetAfter = _telNumbers.All(n => n.SessionIdentity.Count == 0);
            _telLoopProgress.Value = "Binding inbound here…";
            await app.Telephony.BindInboundToThisInstanceAsync();

            _telLoopResults[key] = sms
                ? await RunSmsLoopAsync(from, to)
                : await RunCallLoopAsync(from, to);
        }
        catch (TelephonyNumberNotAvailableException ex)
        {
            _telLoopResults[key] = $"FAIL: {TelNoNumbersText} ({ex.Message})";
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Validation telephony {key} loop failed: {ex.GetType().Name}: {ex.Message}");
            _telLoopResults[key] = $"FAIL {key} loop: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            if (resetAfter)
            {
                try
                {
                    await app.Telephony.ResetInboundAsync();
                }
                catch (Exception ex)
                {
                    // The verdict above stands; a binding left on this identity only moves where the
                    // next inbound lands, and the next loop binds again anyway.
                    Log.Instance.Warning($"Validation telephony loop could not reset the inbound binding: {ex.GetType().Name}: {ex.Message}");
                }
            }

            _telLoopProgress.Value = "";
            _telLoopBusy.Value = false;
            _telLoopRunning = null;
            _telLoopGate.Release();
            _ = RefreshTelephonyAsync();
        }
    }

    private async Task<string> RunSmsLoopAsync(TelephonyNumber a, TelephonyNumber b)
    {
        var token = $"IKONLOOP{Random.Shared.Next(100000, 999999)}";
        var waiter = new TaskCompletionSource<SmsMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _telSmsLoopWaiters[token] = waiter;
        var started = DateTime.UtcNow;

        try
        {
            _telLoopProgress.Value = $"Sending {token} from {a.Number} to {b.Number}…";
            var sent = await app.Telephony.SendSmsAsync(b.Number, $"Ikon validation loopback {token}", from: a.Number);
            var sentMs = (DateTime.UtcNow - started).TotalMilliseconds;
            _telLoopProgress.Value = $"Accepted as {sent.MessageId} after {sentMs:0} ms, waiting for it on {b.Number}…";

            var arrived = await Task.WhenAny(waiter.Task, Task.Delay(TelSmsLoopTimeout));

            if (arrived != waiter.Task)
            {
                return $"FAIL sms loop {a.Number}→{b.Number}: sent (id {sent.MessageId}, {sent.Parts} part, status {sent.Status}) but not received here within {TelSmsLoopTimeout.TotalSeconds:0} s";
            }

            var message = waiter.Task.Result;
            var totalMs = (DateTime.UtcNow - started).TotalMilliseconds;

            return $"PASS sms loop {a.Number}→{b.Number} in {totalMs:0}ms (accepted after {sentMs:0} ms, {sent.Parts} part, replyable {sent.Replyable}, arrived from {message.From} to {message.To})";
        }
        finally
        {
            _telSmsLoopWaiters.TryRemove(token, out _);
        }
    }

    private void CompleteSmsLoop(SmsMessage message)
    {
        foreach (var (token, waiter) in _telSmsLoopWaiters)
        {
            if (message.Text.Contains(token, StringComparison.Ordinal))
            {
                waiter.TrySetResult(message);
            }
        }
    }

    private async Task<string> RunCallLoopAsync(TelephonyNumber a, TelephonyNumber b)
    {
        var loop = new TelLoopCall(a.Number, b.Number);
        _telLoopCall = loop;
        var started = DateTime.UtcNow;

        try
        {
            _telLoopProgress.Value = $"Calling {b.Number} from {a.Number}…";
            await using var call = await app.Telephony.CallAsync(b.Number, TelCallLoopRing, a.Number);
            var connectedMs = (DateTime.UtcNow - started).TotalMilliseconds;
            _telLoopProgress.Value = $"Connected after {connectedMs:0} ms ({call.CallId}), exchanging tones…";

            var meter = new TelAudioMeter(TelSampleRate, TelToneHz);
            using var window = new CancellationTokenSource(TelCallLoopListen);
            var listening = ListenIntoAsync(call, meter, window.Token);

            // A second of silence first, so the tone is not sent before the far end is listening.
            await Task.Delay(TimeSpan.FromSeconds(1), window.Token);
            await call.SpeakAsync(TelTone(TelCallLoopToneLength), window.Token);

            try
            {
                await call.WaitForPlaybackAsync(window.Token);
            }
            catch (OperationCanceledException)
            {
                // The listen window closed first; what B heard is reported by B's own meter.
            }

            while (!window.IsCancellationRequested && meter.ToneSeconds < TelLoopToneSecondsNeeded * 2)
            {
                await Task.Delay(200);
            }

            // Past the tone we sent, so B has heard all of it before the line drops.
            await Task.Delay(TimeSpan.FromSeconds(1));
            await window.CancelAsync();
            await listening;

            var interrupt = await CheckInterruptAsync(call);
            await call.HangUpAsync();

            var callee = await Task.WhenAny(loop.Result.Task, Task.Delay(TelCallLoopCalleeWait)) == loop.Result.Task
                ? loop.Result.Task.Result
                : null;

            var totalMs = (DateTime.UtcNow - started).TotalMilliseconds;
            string aHeard = $"A heard B's tone {meter.ToneSeconds:0.0} s{(meter.FirstToneAfterMs is { } first ? $" (first after {first:0} ms)" : "")}";
            string bHeard = callee == null
                ? "B's side never answered on this instance (inbound binding?)"
                : $"B heard A's tone {callee.ToneSeconds:0.0} s ({callee.Summary})";

            bool pass = meter.ToneSeconds >= TelLoopToneSecondsNeeded && callee != null && callee.ToneSeconds >= TelLoopToneSecondsNeeded && interrupt.Ok;

            return $"{(pass ? "PASS" : "FAIL")} call loop {a.Number}→{b.Number}: connected in {connectedMs:0}ms, {aHeard}, {bHeard}, {interrupt.Summary}, total {totalMs:0}ms";
        }
        finally
        {
            _telLoopCall = null;
        }
    }

    // Starts a tone far longer than the limit, interrupts it, and expects playback to be reported
    // caught up well before the tone would have ended.
    private static async Task<(bool Ok, string Summary)> CheckInterruptAsync(IVoiceCall call)
    {
        // An ended call reports playback caught up at once, which would read as an instant interrupt.
        if (!call.IsConnected)
        {
            return (false, "the call ended before the interrupt check");
        }

        using var limit = new CancellationTokenSource(TelInterruptLimit + TimeSpan.FromSeconds(2));

        try
        {
            await call.SpeakAsync(TelTone(TelInterruptToneLength), limit.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(500), limit.Token);

            var started = DateTime.UtcNow;
            await call.InterruptAsync(limit.Token);
            await call.WaitForPlaybackAsync(limit.Token);
            var ms = (DateTime.UtcNow - started).TotalMilliseconds;

            return ms <= TelInterruptLimit.TotalMilliseconds
                ? (true, $"interrupt settled in {ms:0} ms")
                : (false, $"interrupt took {ms:0} ms");
        }
        catch (OperationCanceledException)
        {
            return (false, $"interrupt never settled within {TelInterruptLimit.TotalSeconds:0} s");
        }
        catch (Exception ex)
        {
            return (false, $"interrupt failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private bool TryTakeLoopCall(IVoiceCall call, out TelLoopCall loop)
    {
        var armed = _telLoopCall;

        // Either end identifies it: the caller is our own number A, and a provider bridging the call
        // may report its bridge rather than B as the number called.
        if (armed != null && (SameNumber(call.From, armed.CallerNumber) || SameNumber(call.To, armed.CalleeNumber)) && armed.TryTake())
        {
            loop = armed;
            return true;
        }

        loop = null!;
        return false;
    }

    private static bool SameNumber(string x, string y)
    {
        var xDigits = new string(x.Where(char.IsDigit).ToArray());
        var yDigits = new string(y.Where(char.IsDigit).ToArray());

        return xDigits.Length > 0 && xDigits == yDigits;
    }

    // B's side of the loop: answer, send the tone at once, and measure what A sends back.
    private async Task AnswerLoopCallAsync(IVoiceCall call, TelLoopCall loop)
    {
        var meter = new TelAudioMeter(TelSampleRate, TelToneHz);

        try
        {
            using var window = new CancellationTokenSource(TelCallLoopCalleeLine);
            var listening = ListenIntoAsync(call, meter, window.Token);

            try
            {
                await call.SpeakAsync(TelTone(TelCallLoopToneLength), window.Token);
                await listening;
            }
            catch (OperationCanceledException)
            {
                // A hangs up once it has heard enough; the window only bounds a line that never drops.
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Validation loop callee on {call.CallId} failed: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                await call.HangUpAsync();
            }
            catch (Exception ex)
            {
                // A usually hangs up first, which is the expected end of the loop.
                Log.Instance.Debug($"Hang-up of validation loop call {call.CallId}: {ex.Message}");
            }

            loop.Result.TrySetResult(new TelLoopCallee(meter.ToneSeconds, meter.Describe()));
            AddCallLog(new TelCallEntry(DateTime.UtcNow, "loop", call.CallId, call.From, call.To, meter.Describe()));
        }
    }
}

internal sealed class TelLoopCall(string callerNumber, string calleeNumber)
{
    private int _taken;

    public string CallerNumber { get; } = callerNumber;

    public string CalleeNumber { get; } = calleeNumber;

    public TaskCompletionSource<TelLoopCallee> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool TryTake()
    {
        return Interlocked.Exchange(ref _taken, 1) == 0;
    }
}

internal sealed record TelLoopCallee(double ToneSeconds, string Summary);
