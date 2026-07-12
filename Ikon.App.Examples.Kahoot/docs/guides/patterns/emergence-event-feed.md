<!-- mined-from: Ikon.App.Examples.Emergence -->
# Emergence Event Feed — Color-Coded Log Of Run Events

`Emerge.Run<T>(...)` is an `IAsyncEnumerable<EmergeEvent<T>>` — every iteration, tool call, stage transition, and completion arrives as a separate event. Switching on the event type and pushing a typed `LogEntry` into a `ReactiveList<LogEntry>` gives you a debugger-style live log: timestamp + level + message, color-coded by category. This is what makes long-running agentic patterns (MapReduce, BestOf, TaskGraph, agentic coder) feel transparent instead of opaque.

## When to use

You're shipping any Emergence pattern beyond `.ResultAsync()` — the user needs to *see* the agent thinking. Also valuable for debugging your own prompt: when iterations balloon or tool calls go in circles, the feed shows it instantly.

## Snippet

```csharp
public enum LogLevel { Info, Event, Tool, Result, Error, Stage, Iteration }
public record LogEntry(DateTime Timestamp, LogLevel Level, string Message);

public class ExampleState
{
    public ReactiveList<LogEntry> Logs { get; } = new();
    public Reactive<string> CurrentStage { get; } = new("Ready");
    public Reactive<int> CurrentIteration { get; } = new(0);
    public Reactive<int> ToolCallCount { get; } = new(0);

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        Logs.Add(new LogEntry(DateTime.Now, level, message));   // mutator notifies — no list rebuild
    }
}

private void LogEvent<T>(ExampleState state, EmergeEvent<T> ev)
{
    switch (ev)
    {
        case ToolCallPlanned<T> tool:
            state.ToolCallCount.Value++;
            state.Log($"Calling tool: {tool.Call.Function.Name}({TruncateJson(tool.Call.ParametersJson)})", LogLevel.Tool);
            break;
        case ToolCallResult<T> result:
            state.Log($"Tool returned: {TruncateJson(JsonSerializer.Serialize(result.Result))}", LogLevel.Tool);
            break;
        case Stage<T> stage:
            state.CurrentStage.Value = stage.Name;
            state.Log($"Entering stage: {stage.Name}", LogLevel.Stage);
            break;
        case Ikon.AI.Emergence.Progress<T> progress:
            state.Log(progress.Message, LogLevel.Info);
            break;
        case Completed<T> completed:
            state.Log($"Completed! {completed.Trace.Iterations} iterations, {completed.Trace.ToolCalls} tool calls", LogLevel.Result);
            break;
    }
}

// Render — color per level
foreach (var log in state.Logs.TakeLast(100))
{
    var (bg, text) = log.Level switch
    {
        LogLevel.Tool      => ("bg-purple-500/10", "text-purple-400"),
        LogLevel.Result    => ("bg-green-500/10",  "text-green-400"),
        LogLevel.Error     => ("bg-red-500/10",    "text-red-400"),
        LogLevel.Stage     => ("bg-yellow-500/10", "text-yellow-400"),
        LogLevel.Iteration => ("bg-cyan-500/10",   "text-cyan-400"),
        _                  => ("",                 "text-foreground")
    };
    view.Box([$"py-1 px-2 mb-1 rounded {bg}"], content: view =>
    {
        view.Row([Layout.Row.Sm], content: view =>
        {
            view.Text([Text.Caption, "text-muted-foreground w-20 shrink-0"], log.Timestamp.ToString("HH:mm:ss.fff"));
            view.Text([$"font-semibold {text}"], $"[{log.Level}]");
            view.Text([Text.Caption, "break-all"], log.Message);
        });
    });
}
```

## Notes

- The feed is a `ReactiveList<LogEntry>` — `Logs.Add(entry)` mutates AND broadcasts in one call; enumeration and LINQ (`TakeLast`) run straight on the reactive. `Logs.Value` is an `IReadOnlyList<T>` snapshot, so `.Value.Add(entry)` does not compile.
- Cap the rendered list with `TakeLast(100)` — the underlying list can grow huge but the DOM stays bounded.
- Don't log every `ModelText<T>` chunk — it's per-token and floods the feed; use `chatbot-streaming` for that.
- Track `_cts` so the same UI can show a "Stop" button that cancels mid-run; the feed will show the `Stopped<T>` event.
- For MapReduce/TaskGraph, increment `CurrentIteration` on `Progress<T>` to show "chunks processed" alongside the feed.

## See also

- `streaming-agent-status`
- `agent-streaming-with-tool-status`
- `orchestrator-thread-with-tools`
