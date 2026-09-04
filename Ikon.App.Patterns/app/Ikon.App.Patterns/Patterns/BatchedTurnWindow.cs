namespace Ikon.App.Patterns.Patterns;

// Pattern: batched-turn-window — see docs/patterns/batched-turn-window.md.
// The stubs outside the region stand in for the shared player roster and the batch processor the app
// owns; the docsnippet region is the canonical body the doc extracts.
internal sealed class BatchedTurnWindow : IPatternDemo
{
    public string Slug => "batched-turn-window";
    public string Title => "Batched turn window";
    public string Category => "Realtime";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: collects per-player actions inside a countdown window that pauses while someone is typing and fires early once everyone has submitted. See the source and docs/patterns/batched-turn-window.md.");

    private sealed record Player(int ClientId, string? Character);

    private readonly ReactiveList<Player> _players = new();

    private Task ProcessBatchedActionsAsync() => throw new NotImplementedException();

    #region docsnippet:pattern-batched-turn-window
    private readonly ReactiveDictionary<int, string> _pendingActions = new();
    private readonly Reactive<int> _batchCountdownMs = new(0);
    private readonly Reactive<int> _batchTotalMs = new(20000);
    private readonly ReactiveHashSet<int> _typingClients = new();
    private const int BatchWindowMs = 20000;
    private const int ExtendTimeMs = 10000;
    private const int TypingIdleMs = 3000;
    private CancellationTokenSource? _batchTimerCts;
    private readonly ConcurrentDictionary<int, DateTimeOffset> _lastTypingTime = new();

    private async Task SubmitPlayerActionAsync(string action)
    {
        int clientId = ReactiveScope.ClientId;
        _pendingActions[clientId] = action;

        int totalPlayers = _players.Count(p => p.Character != null);

        if (_pendingActions.Count >= totalPlayers)
        {
            _batchTimerCts?.Cancel();
            await ProcessBatchedActionsAsync();
            return;
        }

        if (_pendingActions.Count == 1)
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

            // Pause the clock while an unsubmitted player is actively typing — pure wall time would feel unfair.
            bool anyoneTyping = _typingClients
                .Any(cid => !_pendingActions.ContainsKey(cid) &&
                            _lastTypingTime.TryGetValue(cid, out var lt) &&
                            (DateTimeOffset.UtcNow - lt).TotalMilliseconds < TypingIdleMs);

            if (!anyoneTyping)
            {
                _batchCountdownMs.Value = Math.Max(0, _batchCountdownMs.Value - 500);
            }
        }

        if (!ct.IsCancellationRequested)
        {
            await ProcessBatchedActionsAsync();
        }
    }

    private void ExtendBatchTime()
    {
        if (_batchCountdownMs.Value <= 0)
        {
            return;
        }

        _batchCountdownMs.Value = Math.Min(_batchCountdownMs.Value + ExtendTimeMs, _batchTotalMs.Value + ExtendTimeMs);
        _batchTotalMs.Value = Math.Max(_batchTotalMs.Value, _batchCountdownMs.Value);
    }
    #endregion
}
