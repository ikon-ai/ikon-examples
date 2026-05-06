<!-- mined-from: Vienola -->
# Batched Turn Window — Wait For The Party Before The AI Responds

Multiplayer AI loops where each player submits an action, and the AI/GM responds *once* with a unified narrative. The window opens when the first player submits, closes when everyone has submitted OR a countdown elapses. Players who are still typing pause the countdown. Anyone can hit "extend" to add more time.

## When to use

Multi-user games, collaborative drawing prompts, group polling — anywhere the AI should wait for human submissions before working, but can't wait forever. Smooths over slow typers without holding fast players hostage.

## Snippet

```csharp
private readonly Reactive<Dictionary<int, string>> _pendingActions = new(new Dictionary<int, string>());
private readonly Reactive<int> _batchCountdownMs = new(0);
private readonly Reactive<int> _batchTotalMs = new(20000);
private readonly Reactive<HashSet<int>> _typingClients = new(new HashSet<int>());
private const int BatchWindowMs = 20000;
private const int ExtendTimeMs = 10000;
private const int TypingIdleMs = 3000;
private CancellationTokenSource? _batchTimerCts;
private readonly ConcurrentDictionary<int, DateTimeOffset> _lastTypingTime = new();

private async Task SubmitPlayerActionAsync(string action)
{
    int clientId = ReactiveScope.ClientId;
    var pending = new Dictionary<int, string>(_pendingActions.Value) { [clientId] = action };
    _pendingActions.Value = pending;

    int totalPlayers = _players.Value.Count(p => p.Character != null);
    if (pending.Count >= totalPlayers)
    {
        _batchTimerCts?.Cancel();
        await ProcessBatchedActionsAsync();
        return;
    }

    if (pending.Count == 1)
    {
        _batchTotalMs.Value = BatchWindowMs;
        _batchCountdownMs.Value = BatchWindowMs;
        _batchTimerCts?.Cancel();
        _batchTimerCts = new CancellationTokenSource();
        _ = Task.Run(() => RunBatchTimerAsync(_batchTimerCts.Token));
    }
}

private async Task RunBatchTimerAsync(CancellationToken ct)
{
    while (_batchCountdownMs.Value > 0 && !ct.IsCancellationRequested)
    {
        await Task.Delay(500, ct);

        // Pause if someone who hasn't submitted is typing
        bool anyoneTyping = _typingClients.Value
            .Any(cid => !_pendingActions.Value.ContainsKey(cid) &&
                        _lastTypingTime.TryGetValue(cid, out var lt) &&
                        (DateTimeOffset.UtcNow - lt).TotalMilliseconds < TypingIdleMs);

        if (!anyoneTyping)
        {
            _batchCountdownMs.Value = Math.Max(0, _batchCountdownMs.Value - 500);
        }
    }
    if (!ct.IsCancellationRequested) await ProcessBatchedActionsAsync();
}

private void ExtendBatchTime()
{
    if (_batchCountdownMs.Value <= 0) return;
    _batchCountdownMs.Value = Math.Min(_batchCountdownMs.Value + ExtendTimeMs, _batchTotalMs.Value + ExtendTimeMs);
    _batchTotalMs.Value = Math.Max(_batchTotalMs.Value, _batchCountdownMs.Value);
}
```

## Notes

- Countdown pauses while *unsubmitted* players are actively typing — pure wall clock would feel unfair.
- One `CancellationTokenSource` per window so a fast unanimous submission cancels the timer immediately.
- Show per-player chips during the wait — `✓` submitted, `✎` typing, `·` idle — turning the wait into a social cue.
- Tap-to-extend is on the progress bar itself, no extra button — see `RenderInputBar`.
- After `ProcessBatchedActionsAsync` runs, clear all batch state; the next submission opens a fresh window.

## See also

- `multi-user-game` — base shape for shared `Reactive<List<Player>>` state
- `chatbot-streaming` — used inside `ProcessBatchedActionsAsync` to stream the unified GM response
