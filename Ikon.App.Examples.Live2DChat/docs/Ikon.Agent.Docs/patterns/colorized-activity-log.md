<!-- mined-from: Nanobot -->
# Colorized Activity Log — Pattern-Match Rows To Color By Substring

A monospace, append-only event stream where each line is colored by what it contains: ERROR/❌ → red bg, ✓/COMPLETED → green bg, Tool:/🔧 → yellow bg, heartbeat → purple. The log is a single `ReactiveList<string>` truncated to the last N entries; the renderer just reverses it (newest first) and applies the color rules in a switch ladder. No per-entry struct needed.

## When to use

Long-running agent / bot / automation processes where the user watches a debug stream. Compact, scannable, no boilerplate per log call. Better than uniform text and faster to add than per-event types.

## Snippet

```csharp
private readonly ReactiveList<string> _logEntries = new();
private const int MaxLogEntries = 200;

private void AddLogEntry(string entry)
{
    var line = $"[{DateTime.Now:HH:mm:ss}] {entry}";

    // Append + truncate in ONE transform — one change notification for both.
    _logEntries.Update(current => current.Append(line).TakeLast(MaxLogEntries));
}

private void RenderLogsTab(UIView col)
{
    col.Column(["flex-1 bg-slate-900/50 rounded-2xl p-4 font-mono text-xs space-y-1.5 overflow-y-auto"],
        content: logCol =>
    {
        foreach (var entry in _logEntries.Reverse())
        {
            var (color, bg) = ClassifyLogLine(entry);
            logCol.Text([$"{color} px-2 py-1 rounded {bg}"], entry);
        }
    });
}

private static (string color, string bg) ClassifyLogLine(string entry)
{
    if (entry.Contains("ERROR") || entry.Contains("FAILED"))
        return ("text-red-300", "bg-red-500/10");
    if (entry.Contains("✓") || entry.Contains("COMPLETED"))
        return ("text-green-300", "bg-green-500/10");
    if (entry.Contains("▶") || entry.Contains("STARTING"))
        return ("text-blue-300", "bg-blue-500/10");
    if (entry.Contains("Tool:") || entry.Contains("🔧"))
        return ("text-yellow-300", "bg-yellow-500/10");
    if (entry.Contains("heartbeat"))
        return ("text-purple-300", "bg-purple-500/10");
    return ("text-slate-400", "");
}
```

## Notes

- `ReactiveList<string>`, never `Reactive<List<string>>` — every mutator (`Add`, `Update`, `RemoveAt`, …) notifies on its own, and `_logEntries.Value` is a read-only snapshot so the "mutated in place, UI never updated" bug cannot compile. Enumerate the reactive directly (`foreach (var e in _logEntries.Reverse())`) — that read is tracked.
- Append and truncate in a single `_logEntries.Update(...)` — one notification instead of one per operation.
- Truncate at write time, not render time. A 10k-line log re-rendering 60 times a second tanks the diff.
- Reverse on render so the newest line is at the top — operators don't want to scroll.
- Use lowercase substrings only when you `ToLowerInvariant` the entry first; otherwise be case-sensitive on tags you control.

## See also

- `streaming-agent-status`
- `status-badge-from-enum`
