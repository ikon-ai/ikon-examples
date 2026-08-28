<!-- mined-from: Threads (rewritten against the real Ikon.Agent AgentThread surface) -->
# Streaming Agent Status — Live block of in-progress agent activity

A reactive box that renders the *current* state of a running `AgentThread` — what it is doing right now, which tools have fired, and how many tokens it has burned — by reading the thread's own reactives. No bespoke "live state" object is needed: `AgentThread` already exposes all of it as `Reactive<T>`, so reading them inside the UI lambda registers the dependency and the block re-renders as the agent runs.

## When to use

When an agent or LLM call takes more than 1-2 seconds and you want to show the user *what's happening* (which tool is running, which turn it is on, how many tokens have streamed) rather than a generic spinner. Pair with `chatbot-streaming` (token-level streaming for the final answer) — this pattern surfaces the orchestration layer above it.

## The API this reads

Everything below is on `AgentThread`; get one with `orchestrator.GetThread(threadId)`, which returns `AgentThread?`.

| Member | Type | What it gives you |
|---|---|---|
| `AgentName` | `string` | plain property, not reactive |
| `Status` | `Reactive<ThreadStatus>` | `Pending`, `Active`, `WaitingForChildren`, `WaitingForInput`, `Idle`, `Done`, `Failed`, `Archived` — "running" is `Active` |
| `Activity` | `Reactive<Activity>` | `Activity(ActivityKind Kind, string? Tool)`; kinds `Idle` / `Thinking` / `Streaming` / `RunningTool` |
| `Stage` | `Reactive<string?>` | the runner's free-text stage label, when set |
| `ToolCallTimeline` | `Reactive<IReadOnlyList<ToolCallEntry>>` | `(int PrecedingAgentMessages, string ToolName, string ArgsJson, string? ResultText, bool? IsError)` |
| `ActiveTools` | `Reactive<IReadOnlyList<ToolInfo>>` | tools the LLM can currently call |
| `Messages` | `Reactive<IReadOnlyList<Message>>` | every turn on the thread |
| `Usage` | `Reactive<ThreadUsage>` | `InputTokens`, `OutputTokens`, `Turns`, `WallTime`, … (token kinds are never summed for you) |
| `FailureReason` | `Reactive<string?>` | why it last failed |

## Snippet

```csharp
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
```

## Notes

- `Activity` is the "what is it doing right now" signal — `ActivityKind.RunningTool` carries the tool name in `activity.Tool`. `Status` is the coarser lifecycle (`Active` vs `WaitingForInput` vs `Done`); render the live block only while `Active`.
- Reading `thread.Activity.Value` / `thread.ToolCallTimeline.Value` inside the UI lambda IS the subscription — no timer, no manual re-render, no bespoke live-state object. The runner mutates the thread's reactives; the subtree re-renders.
- `ToolCallEntry.IsError` is a `bool?`: **null = still running**, `false` = succeeded, `true` = failed. That tri-state is what drives spinner → check/×; there is no `IsComplete` flag.
- `ThreadUsage` keeps token kinds independent (`InputTokens`, `CachedInputTokens`, `CacheCreationInputTokens`, `OutputTokens`) — add only the ones you mean to display, and use `Turns` for "which iteration are we on".
- `PrecedingAgentMessages` is how many agent messages had landed when the call started — use it to interleave tool rows with `thread.Messages.Value` in a Claude-Code-style transcript instead of stacking them in a separate box.
- `bg-gradient-to-r from-muted/40 to-muted/20` makes the live block visually distinct from finished messages.
- When a run stops on a cap rather than finishing, `ThreadEvent.BudgetExceeded` carries a `BudgetSnapshot` naming *which* cap tripped — surface that instead of a generic "stopped", or the user cannot tell a token ceiling from a turn limit. `PassRecord` holds the per-pass detail behind it.

## See also

- `chatbot-streaming` — token-level streaming for the final answer; this pattern wraps that
