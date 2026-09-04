namespace Ikon.App.Patterns.Patterns;

// Pattern: streaming-agent-status — see docs/patterns/streaming-agent-status.md.
// The orchestrator and selected-thread reactive stand in for the caller's real agent wiring; the
// live block reads the running thread's own reactives, so touching them inside the lambda subscribes.
internal sealed class StreamingAgentStatus : IPatternDemo
{
    public string Slug => "streaming-agent-status";
    public string Title => "Streaming agent status";
    public string Category => "AI";
    public void RenderDemo(IView view) => Render(view);

    private readonly Orchestrator _orchestrator = null!;
    private readonly Reactive<string?> _selectedThreadId = new((string?)null);

    private void Render(IView view)
    {
        #region docsnippet:pattern-streaming-agent-status
        var thread = _selectedThreadId.Value is { } id ? _orchestrator.GetThread(id) : null;

        if (thread is not null && thread.Status.Value == ThreadStatus.Active)
        {
            var activity = thread.Activity.Value;
            var usage = thread.Usage.Value;

            view.Box(["py-4 mt-3 px-5 rounded-xl bg-gradient-to-r from-muted/40 to-muted/20 shadow-sm"], content: view =>
            {
                view.Row(["items-center gap-3 mb-1"], content: view =>
                {
                    view.Text(["text-xs font-bold text-sky-600 dark:text-sky-400"], text: thread.AgentName);
                    view.Box(["flex-1"]);

                    var activityText = activity.Kind switch
                    {
                        ActivityKind.Thinking => "thinking",
                        ActivityKind.Streaming => "streaming",
                        ActivityKind.RunningTool => activity.Tool ?? "tool",
                        _ => "idle"
                    };
                    view.Text(["text-xs px-1 py-0.5 rounded font-mono text-muted-foreground bg-muted"], text: activityText);
                });

                if (thread.Stage.Value is { Length: > 0 } stage)
                {
                    view.Text(["text-xs text-muted-foreground/70 mb-1"], text: stage);
                }

                // IsError is null while the call is still in flight — spinner, then check or error.
                foreach (var call in thread.ToolCallTimeline.Value)
                {
                    view.Row(["items-center gap-1.5 py-0.5"], key: $"{call.PrecedingAgentMessages}-{call.ToolName}", content: view =>
                    {
                        if (call.IsError is null)
                        {
                            view.Spinner(["text-sky-400"], size: SpinnerSize.Sm);
                        }
                        else if (call.IsError == true)
                        {
                            view.Icon(["w-3 h-3 text-red-400"], name: "x");
                        }
                        else
                        {
                            view.Icon(["w-3 h-3 text-emerald-400"], name: "check");
                        }

                        view.Text(["text-xs text-muted-foreground font-mono"], text: call.ToolName);

                        if (!string.IsNullOrEmpty(call.ResultText))
                        {
                            view.Text(["text-xs text-muted-foreground/50 truncate"], text: call.ResultText);
                        }
                    });
                }

                view.Text(["text-xs text-muted-foreground/50 mt-1"],
                    text: $"turn {usage.Turns} · {usage.InputTokens + usage.OutputTokens} tokens · {usage.WallTime.TotalSeconds:F0}s");
            });
        }
        #endregion
    }
}
