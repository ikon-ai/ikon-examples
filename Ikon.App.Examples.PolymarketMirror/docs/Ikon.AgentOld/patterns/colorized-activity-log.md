<!-- mined-from: Nanobot -->
# Colorized Activity Log — Pattern-Match Rows To Color By Substring

A monospace, append-only event stream where each line is colored by what it contains: ERROR/❌ → red bg, ✓/COMPLETED → green bg, Tool:/🔧 → yellow bg, heartbeat → purple. The log is a single `Reactive<IReadOnlyList<string>>` truncated to the last N entries; the renderer just reverses it (newest first) and applies the color rules in a switch ladder. No per-entry struct needed.

## When to use

Long-running agent / bot / automation processes where the user watches a debug stream. Compact, scannable, no boilerplate per log call. Better than uniform text and faster to add than per-event types.

## Snippet

```csharp
private readonly Reactive<IReadOnlyList<string>> _logEntries = new(Array.Empty<string>());
private const int MaxLogEntries = 200;

private void AddLogEntry(string entry)
{
    var ts = DateTime.Now.ToString("HH:mm:ss");
    var line = $"[{ts}] {entry}";
    var current = _logEntries.Value.ToList();
    current.Add(line);
    while (current.Count > MaxLogEntries) current.RemoveAt(0);
    _logEntries.Value = current;
}

private void RenderLogsTab(UIView col)
{
    col.Column(["flex-1 bg-slate-900/50 rounded-2xl p-4 font-mono text-xs space-y-1.5 overflow-y-auto"],
        content: logCol =>
    {
        foreach (var entry in _logEntries.Value.Reverse())
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

- `Reactive<IReadOnlyList<string>>` not `List<string>` — only assigning a new list triggers UI updates; mutating in place is silent.
- Truncate at write time, not render time. A 10k-line log re-rendering 60 times a second tanks the diff.
- Reverse on render so the newest line is at the top — operators don't want to scroll.
- Use lowercase substrings only when you `ToLowerInvariant` the entry first; otherwise be case-sensitive on tags you control.

## See also

- `streaming-agent-status`
- `status-badge-from-enum`
