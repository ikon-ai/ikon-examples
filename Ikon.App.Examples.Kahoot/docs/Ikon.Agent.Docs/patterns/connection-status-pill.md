<!-- mined-from: Sentinel -->
# Connection Status Pill — Tone-coded "system is healthy" indicator

A small rounded-full pill in the top bar that summarises the *current overall state* of the system in one sentence: "Watching · 3 cameras · Occupied", "All paused", "Drill mode · 12m left", "Facility blackout". A switch over reactives picks the dominant condition (most-urgent-wins precedence), assigns a tone-coded ring color and an inline dot, and renders a single text. Optionally appends a sub-detail like "auto → Vac 2h" for the next scheduled change.

## When to use

Any always-on operations app where the user needs to know *at a glance* whether the system is doing its job — security monitoring, agent dashboards, network health, recording rigs. Place in the top strip alongside the brand. Use this rather than scattered indicators (one for each subsystem) when the user mostly just wants a single answer to "are we good right now."

## Snippet

```csharp
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
```

## Notes

- **Most-urgent-wins precedence** is a key idea: order the `if` chain so a *more critical* state always overrides a less critical one. Drill mode wins over paused, paused wins over no-cameras, etc. Get this order wrong and operators see "All paused" while a fire drill is active — bad.
- Ring color and dot color move together — both are the system's mood. Text stays neutral so the colored chrome carries the signal. Don't tint the text too: it muddles the message.
- The sub-detail (`· auto → Vac 2h`) is hidden on small screens (`hidden md:inline`) — pill must fit on a phone navbar without wrapping.
- The pill is *derived* state — there is no `_systemState` reactive. The renderer recomputes from primitive reactives (`_facilityBlackoutSince`, `_drillUntil`, `_globalPauseAll`, `_streams`) on every render. This keeps the truth in the source reactives and the pill always correct.
- For a chat / agent app, the equivalent state-precedence might be: "Disconnected > Reconnecting > Token limit reached > Idle (N agents) > Idle".
