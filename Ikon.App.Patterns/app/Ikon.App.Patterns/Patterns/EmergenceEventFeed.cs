namespace Ikon.App.Patterns.Patterns;

// Pattern: emergence-event-feed — see docs/patterns/emergence-event-feed.md.
// The members outside the region stand in for the app's state instance and the JSON-shortening
// helper. Filling the log for real takes an Emerge run against a paid model, so the demo replays a
// recorded run's entries into the same state, one level of each colour.
internal sealed class EmergenceEventFeed : IPatternDemo
{
    public string Slug => "emergence-event-feed";
    public string Title => "Emergence event feed";
    public string Category => "AI";

    private static readonly (LogLevel Level, string Message)[] RecordedRun =
    [
        (LogLevel.Info, "Starting run: summarise open support tickets for the Nordics team"),
        (LogLevel.Stage, "Entering stage: gather"),
        (LogLevel.Iteration, "Iteration 1"),
        (LogLevel.Tool, "Calling tool: search_tickets({\"region\":\"nordics\",\"status\":\"open\"})"),
        (LogLevel.Tool, "Tool returned: [{\"id\":\"T-4812\",\"subject\":\"Invoice PDF missing VAT line\"},{\"id\":\"T-4815\",\"subject\":\"SSO login loop…"),
        (LogLevel.Error, "Tool failed: fetch_ticket(\"T-4790\") timed out after 10 s, retrying"),
        (LogLevel.Tool, "Calling tool: fetch_ticket({\"id\":\"T-4790\"})"),
        (LogLevel.Stage, "Entering stage: summarise"),
        (LogLevel.Iteration, "Iteration 2"),
        (LogLevel.Info, "Grouped 14 tickets into 4 themes"),
        (LogLevel.Result, "Completed! 2 iterations, 4 tool calls"),
    ];

    private readonly ExampleState state = new();
    private readonly Reactive<bool> _replaying = new(false);

    public EmergenceEventFeed()
    {
        foreach (var (level, message) in RecordedRun)
        {
            state.Log(message, level);
        }
    }

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-2xl"], content: col =>
        {
            col.Button([Button.OutlineSm, "self-start"], text: _replaying.Value ? "Replaying…" : "Replay the recorded run",
                disabled: _replaying.Value,
                onClick: async () =>
                {
                    _replaying.Value = true;
                    _ = ReplayAsync();
                });
            col.Column(["gap-0"], content: feed => Render(feed));
        });
    }

    private async Task ReplayAsync()
    {
        try
        {
            state.Logs.Clear();

            foreach (var (level, message) in RecordedRun)
            {
                await Task.Delay(450);
                state.Log(message, level);
            }
        }
        finally
        {
            _replaying.Value = false;
        }
    }

    private static string TruncateJson(string json) => json.Length <= 80 ? json : json[..80] + "…";

    #region example:pattern-emergence-event-feed
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
    private void Render(IView view)
    {
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
    }
    #endregion
}
