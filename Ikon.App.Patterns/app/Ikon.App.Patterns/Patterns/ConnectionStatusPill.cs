namespace Ikon.App.Patterns.Patterns;

// Pattern: connection-status-pill — see docs/patterns/connection-status-pill.md.
// The mode enum, stream state, and primitive reactives below are the source of truth the pill
// derives its one-sentence summary from; ComputeNextScheduleFlip stands in for the app's scheduler.
internal sealed class ConnectionStatusPill : IPatternDemo
{
    public string Slug => "connection-status-pill";
    public string Title => "Connection status pill";
    public string Category => "Feedback";
    // The buttons flip the states the precedence ranks, so each sentence the pill can say is one
    // press away.
    public void RenderDemo(IView view)
    {
        view.Row(["gap-2 flex-wrap mb-3"], content: row =>
        {
            row.Button([Button.OutlineSm], text: _globalPauseAll.Value ? "Resume all" : "Pause all",
                onClick: () => _globalPauseAll.Value = !_globalPauseAll.Value);
            row.Button([Button.OutlineSm], text: _drillUntil.Value is null ? "Start a drill" : "End the drill",
                onClick: () => _drillUntil.Value = _drillUntil.Value is null ? DateTime.UtcNow.AddMinutes(15) : null);
            row.Button([Button.OutlineSm], text: _facilityBlackoutSince.Value is null ? "Black out" : "Restore power",
                onClick: () => _facilityBlackoutSince.Value = _facilityBlackoutSince.Value is null ? DateTime.UtcNow : null);
        });

        view.Row(["p-4 rounded-lg bg-zinc-950 w-fit"], content: RenderTopStatusPill);
    }

    private enum SentinelMode { ShopHours, Vacant }

    private sealed class StreamState
    {
        public bool OfflineFlagged { get; set; }
    }

    // Three cameras, one of them offline, so the healthy sentence shows the partial count.
    private readonly Dictionary<int, StreamState> _streams = new()
    {
        [1] = new StreamState(),
        [2] = new StreamState(),
        [3] = new StreamState { OfflineFlagged = true },
    };
    private readonly Reactive<SentinelMode> _mode = new(SentinelMode.ShopHours);
    private readonly Reactive<DateTime?> _facilityBlackoutSince = new((DateTime?)null);
    private readonly Reactive<DateTime?> _drillUntil = new((DateTime?)null);
    private readonly Reactive<bool> _globalPauseAll = new(false);
    private readonly Reactive<bool> _autoSchedule = new(true);

    private (SentinelMode Mode, DateTime? At) ComputeNextScheduleFlip() => (SentinelMode.Vacant, DateTime.Now.Date.AddHours(DateTime.Now.Hour + 3));

    #region example:pattern-connection-status-pill
    private void RenderTopStatusPill(UIView view)
    {
        var camCount = _streams.Count;
        var offlineCount = _streams.Values.Count(s => s.OfflineFlagged);
        var modeText = _mode.Value == SentinelMode.ShopHours ? "Occupied" : "Vacant";
        var modeDot = _mode.Value == SentinelMode.ShopHours ? "bg-emerald-500" : "bg-blue-500";

        string statusText;
        string ringColor;

        // Most-urgent-wins precedence: blackout > drill > paused > no-cameras > healthy
        if (_facilityBlackoutSince.Value != null)
        {
            statusText = $"Facility blackout · {modeText}";
            ringColor = "ring-rose-500/50";
            modeDot = "bg-rose-500";
        }
        else if (_drillUntil.Value is { } du && du > DateTime.UtcNow)
        {
            var rem = du - DateTime.UtcNow;
            statusText = $"Drill mode · {(int)rem.TotalMinutes}m left";
            ringColor = "ring-violet-500/40";
            modeDot = "bg-violet-500";
        }
        else if (_globalPauseAll.Value)
        {
            statusText = "All paused";
            ringColor = "ring-amber-500/40";
            modeDot = "bg-amber-500";
        }
        else if (camCount == 0)
        {
            statusText = $"No cameras · {modeText}";
            ringColor = "ring-zinc-700";
            modeDot = "bg-zinc-500";
        }
        else
        {
            statusText = offlineCount > 0
                ? $"Watching · {camCount - offlineCount} of {camCount} cameras · {modeText}"
                : $"Watching · {camCount} camera{(camCount == 1 ? "" : "s")} · {modeText}";
            ringColor = offlineCount > 0 ? "ring-amber-500/40" : "ring-zinc-700";
        }

        view.Box([$"px-3 py-1 rounded-full bg-zinc-900 ring-1 {ringColor} flex items-center gap-2 text-xs"], content: pill =>
        {
            pill.Box([$"w-1.5 h-1.5 rounded-full {modeDot}"]);
            pill.Text(["text-zinc-200 font-medium"], statusText);

            // Optional sub-detail — what's coming next
            if (_autoSchedule.Value)
            {
                var (nextMode, nextAt) = ComputeNextScheduleFlip();

                if (nextAt is { } at)
                {
                    var rem = at - DateTime.Now;
                    var remText = rem.TotalHours < 1 ? $"{(int)rem.TotalMinutes}m" : $"{(int)rem.TotalHours}h";
                    var nextLabel = nextMode == SentinelMode.ShopHours ? "Occ" : "Vac";
                    pill.Text(["text-zinc-500 hidden md:inline"], $"· auto → {nextLabel} {remText}");
                }
            }
        });
    }
    #endregion
}
