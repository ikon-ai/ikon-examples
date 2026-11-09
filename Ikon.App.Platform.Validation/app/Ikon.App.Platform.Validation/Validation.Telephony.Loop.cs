using System.Text.Json;
using System.Text.RegularExpressions;

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

    // How often the sending instance looks for what the receiving instance wrote
    private static readonly TimeSpan TelLoopPoll = TimeSpan.FromSeconds(2);

    private const string TelArmedCallAsset = "call-armed.json";
    private const string TelCallResultAsset = "call-result.json";
    private static readonly Regex TelLoopToken = new(@"IKONLOOP\d{6}", RegexOptions.CultureInvariant);

    private readonly Reactive<bool> _telLoopBusy = new(false);
    private readonly Reactive<string> _telLoopProgress = new("");
    private readonly ReactiveDictionary<string, string> _telLoopResults = new();
    private readonly SemaphoreSlim _telLoopGate = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<SmsReceivedPayload>> _telSmsLoopWaiters = new();
    private TelLoopCall? _telLoopCall;
    private volatile string? _telLoopRunning;

    // The two ends of a loop run in different instances: the browser that presses the button has its
    // own, and a text or a call reaches the shared one. What the receiving end saw is handed over
    // through small cloud JSON assets under this prefix.
    private AssetUri TelLoopUri(string name) => new(AssetClass.CloudJson, $"validation/telephony/{name}", spaceId: app.GlobalState.SpaceId);

    private sealed record TelLoopArrival(string From, string To, string Text, string Id);

    private sealed record TelArmedCall(string CallerNumber, string CalleeNumber);

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
        var waiter = new TaskCompletionSource<SmsReceivedPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        _telSmsLoopWaiters[token] = waiter;
        var started = DateTime.UtcNow;

        try
        {
            _telLoopProgress.Value = $"Sending {token} from {a.Number} to {b.Number}…";
            var sent = await app.Telephony.SendSmsAsync(b.Number, $"IKON validation loopback {token}", from: a.Number);
            var sentMs = (DateTime.UtcNow - started).TotalMilliseconds;
            _telLoopProgress.Value = $"Accepted as {sent.MessageId} after {sentMs:0} ms, waiting for it on {b.Number}…";

            // The text reaches the shared instance, which writes its arrival; this instance gets it
            // in memory only when it is the shared one itself
            var arrival = await WaitForSmsLoopArrivalAsync(token, waiter.Task, started + TelSmsLoopTimeout);

            if (arrival == null)
            {
                return $"FAIL sms loop {a.Number}→{b.Number}: sent (id {sent.MessageId}, {sent.Parts} part, status {sent.Status}) but not received by the app within {TelSmsLoopTimeout.TotalSeconds:0} s";
            }

            var totalMs = (DateTime.UtcNow - started).TotalMilliseconds;

            return $"PASS sms loop {a.Number}→{b.Number} in {totalMs:0}ms (accepted after {sentMs:0} ms, {sent.Parts} part, replyable {sent.Replyable}, arrived from {arrival.From} to {arrival.To} as {arrival.Id})";
        }
        finally
        {
            _telSmsLoopWaiters.TryRemove(token, out _);
            await DeleteTelLoopAssetAsync($"loop/{token}.json");
        }
    }

    private async Task<TelLoopArrival?> WaitForSmsLoopArrivalAsync(string token, Task<SmsReceivedPayload> inMemory, DateTime deadlineUtc)
    {
        while (DateTime.UtcNow < deadlineUtc)
        {
            if (inMemory.IsCompletedSuccessfully)
            {
                var message = inMemory.Result;
                return new TelLoopArrival(message.From, message.To, message.Text, message.Id);
            }

            if (await ReadTelLoopAssetAsync<TelLoopArrival>($"loop/{token}.json") is { } written)
            {
                return written;
            }

            await Task.WhenAny(inMemory, Task.Delay(TelLoopPoll));
        }

        return null;
    }

    private void CompleteSmsLoop(SmsReceivedPayload message)
    {
        foreach (var (token, waiter) in _telSmsLoopWaiters)
        {
            if (message.Text.Contains(token, StringComparison.Ordinal))
            {
                waiter.TrySetResult(message);
            }
        }
    }

    // A loopback text names its token, and the instance that sent it polls for this file
    private async Task RecordSmsLoopArrivalAsync(SmsReceivedPayload message)
    {
        if (TelLoopToken.Match(message.Text) is not { Success: true } match)
        {
            return;
        }

        var arrival = new TelLoopArrival(message.From, message.To, message.Text, message.Id);
        await Asset.Instance.SetTextAsync(TelLoopUri($"loop/{match.Value}.json"), JsonSerializer.Serialize(arrival));
    }

    private async Task<T?> ReadTelLoopAssetAsync<T>(string name) where T : class
    {
        var uri = TelLoopUri(name);

        try
        {
            if (!await Asset.Instance.ExistsAsync(uri))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(await Asset.Instance.GetTextAsync(uri));
        }
        catch (Exception ex)
        {
            // The other end may be writing the file this moment; the next poll reads it whole
            Log.Instance.Debug($"Validation telephony loop could not read {name} yet: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private async Task DeleteTelLoopAssetAsync(string name)
    {
        try
        {
            await Asset.Instance.DeleteAsync(TelLoopUri(name));
        }
        catch (Exception ex)
        {
            // A file that was never written, or is already gone; the next loop overwrites what is left
            Log.Instance.Debug($"Validation telephony loop could not delete {name}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task<string> RunCallLoopAsync(TelephonyNumber a, TelephonyNumber b)
    {
        var loop = new TelLoopCall(a.Number, b.Number);
        _telLoopCall = loop;
        var started = DateTime.UtcNow;

        try
        {
            // The call rings the shared instance, which reads this to know the call is the loop's and
            // where to write what it heard; in memory for the case where this is the shared instance
            await DeleteTelLoopAssetAsync(TelCallResultAsset);
            await Asset.Instance.SetTextAsync(TelLoopUri(TelArmedCallAsset), JsonSerializer.Serialize(new TelArmedCall(a.Number, b.Number)));

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

            var callee = await WaitForCalleeResultAsync(loop.Result.Task, DateTime.UtcNow + TelCallLoopCalleeWait);

            var totalMs = (DateTime.UtcNow - started).TotalMilliseconds;
            string aHeard = $"A heard B's tone {meter.ToneSeconds:0.0} s{(meter.FirstToneAfterMs is { } first ? $" (first after {first:0} ms)" : "")}";
            string bHeard = callee == null
                ? "B's side never reported answering (did the call reach the app's shared instance?)"
                : $"B heard A's tone {callee.ToneSeconds:0.0} s ({callee.Summary})";

            bool pass = meter.ToneSeconds >= TelLoopToneSecondsNeeded && callee != null && callee.ToneSeconds >= TelLoopToneSecondsNeeded && interrupt.Ok;

            return $"{(pass ? "PASS" : "FAIL")} call loop {a.Number}→{b.Number}: connected in {connectedMs:0}ms, {aHeard}, {bHeard}, {interrupt.Summary}, total {totalMs:0}ms";
        }
        finally
        {
            _telLoopCall = null;
            await DeleteTelLoopAssetAsync(TelArmedCallAsset);
            await DeleteTelLoopAssetAsync(TelCallResultAsset);
        }
    }

    private async Task<TelLoopCallee?> WaitForCalleeResultAsync(Task<TelLoopCallee> inMemory, DateTime deadlineUtc)
    {
        while (DateTime.UtcNow < deadlineUtc)
        {
            if (inMemory.IsCompletedSuccessfully)
            {
                return inMemory.Result;
            }

            if (await ReadTelLoopAssetAsync<TelLoopCallee>(TelCallResultAsset) is { } written)
            {
                return written;
            }

            await Task.WhenAny(inMemory, Task.Delay(TelLoopPoll));
        }

        return null;
    }

    // The shared instance's side of a loop another instance started: the armed file names the numbers,
    // and is taken once so a second call does not read as the loop's
    private async Task<TelLoopCall?> TryTakeArmedLoopCallAsync(IVoiceCall call)
    {
        if (await ReadTelLoopAssetAsync<TelArmedCall>(TelArmedCallAsset) is not { } armed
            || !(SameNumber(call.From, armed.CallerNumber) || SameNumber(call.To, armed.CalleeNumber)))
        {
            return null;
        }

        await DeleteTelLoopAssetAsync(TelArmedCallAsset);
        return new TelLoopCall(armed.CallerNumber, armed.CalleeNumber, reportThroughAsset: true);
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

            var callee = new TelLoopCallee(meter.ToneSeconds, meter.Describe());
            loop.Result.TrySetResult(callee);

            if (loop.ReportThroughAsset)
            {
                try
                {
                    await Asset.Instance.SetTextAsync(TelLoopUri(TelCallResultAsset), JsonSerializer.Serialize(callee));
                }
                catch (Exception ex)
                {
                    // The caller's side then reports B as never answering, which is this failure's verdict
                    Log.Instance.Warning($"Validation loop callee on {call.CallId} could not report what it heard: {ex.GetType().Name}: {ex.Message}");
                }
            }

            AddCallLog(new TelCallEntry(DateTime.UtcNow, "loop", call.CallId, call.From, call.To, meter.Describe()));
        }
    }
}

internal sealed class TelLoopCall(string callerNumber, string calleeNumber, bool reportThroughAsset = false)
{
    private int _taken;

    public string CallerNumber { get; } = callerNumber;

    public string CalleeNumber { get; } = calleeNumber;

    /// <summary>The caller waits in another instance, so the result is written to the shared asset as well.</summary>
    public bool ReportThroughAsset { get; } = reportThroughAsset;

    public TaskCompletionSource<TelLoopCallee> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool TryTake()
    {
        return Interlocked.Exchange(ref _taken, 1) == 0;
    }
}

internal sealed record TelLoopCallee(double ToneSeconds, string Summary);
