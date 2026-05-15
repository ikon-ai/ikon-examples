<!-- mined-from: Threads -->
# Streaming Agent Status — Live block of in-progress agent activity

A reactive box that renders the *current* state of a running agent — its model, capability tier, active skill packs, in-flight tool calls, written artifacts, and token counts — by reading from a `LiveState` object that the agent updates in-place as it runs. The block reappears every render because the underlying reactive bumps; closures don't loop, the reactive system does.

## When to use

When an agent or LLM call takes more than 1-2 seconds and you want to show the user *what's happening* (which tool is running, what file is being written, how many tokens have streamed) rather than a generic spinner. Surfaces tool calls, partial text, and intermediate artifacts as they arrive. Pair with `chatbot-streaming` (token-level streaming for the final answer) — this pattern surfaces the orchestration layer above it.

## Snippet

```csharp
if (_selectedThreadId.Value != null && (_mind.GetThread(_selectedThreadId.Value)?.IsRunning ?? false))
{
    var thread = _threadStore.GetThread(_selectedThreadId.Value);
    var liveState = _mind.GetThread(_selectedThreadId.Value)?.LiveState;
    var activeCall = _emergenceObserver.GetActiveCall(_selectedThreadId.Value);

    view.Box(["py-4 mt-3 px-5 rounded-xl bg-gradient-to-r from-muted/40 to-muted/20 shadow-sm"], content: view =>
    {
        view.Row(["items-center gap-3 mb-1"], content: view =>
        {
            view.Text(["text-xs font-bold text-sky-600 dark:text-sky-400"], thread?.AgentName ?? "agent");
            view.Box(["flex-1"]);

            if (liveState?.Capability != null)
            {
                var (capText, capColor) = liveState.Capability.Value switch
                {
                    Capability.Quick    => ("quick", "text-blue-400 bg-blue-900/40"),
                    Capability.Standard => ("std",   "text-yellow-400 bg-yellow-900/40"),
                    Capability.Deep     => ("deep",  "text-orange-400 bg-orange-900/40"),
                    _                   => ("?",     "text-muted-foreground bg-muted")
                };
                view.Text([$"text-xs px-1 py-0.5 rounded font-mono {capColor}"], capText);
            }
        });

        if (liveState?.ActiveSkillPacks.Count > 0)
        {
            view.Row(["items-center gap-1 flex-wrap mb-1"], content: view =>
            {
                foreach (var pack in liveState.ActiveSkillPacks)
                {
                    view.Text(["text-xs px-1 py-0.5 rounded bg-cyan-900/40 text-sky-300 font-mono"], pack);
                }
            });
        }

        if (liveState?.Blocks.Count > 0)
        {
            foreach (var block in liveState.Blocks)
            {
                if (block is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
                {
                    view.Text(["text-xs text-muted-foreground break-words"], tb.Text);
                }
                else if (block is ToolCallBlock tcb)
                {
                    view.Row(["items-center gap-1.5 py-0.5"], content: view =>
                    {
                        if (tcb.IsComplete) view.Icon(["w-3 h-3 text-emerald-400"], name: "check");
                        else view.Box([Icon.Spinner, "w-3 h-3 text-sky-400"]);

                        view.Text(["text-xs text-muted-foreground font-mono"], tcb.ToolName);
                        if (!string.IsNullOrEmpty(tcb.Summary))
                        {
                            view.Text(["text-xs text-muted-foreground/50 truncate"], tcb.Summary);
                        }
                    });
                }
            }
        }

        // Thinking icon — animates without a timer, by reading the wall clock at render
        var thinkingIcons = new[] { "circle", "triangle", "square" };
        var thinkingIcon = thinkingIcons[(int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 500 % thinkingIcons.Length)];
        view.Icon(["w-3 h-3 text-muted-foreground/60"], name: thinkingIcon);

        var tokens = activeCall != null ? activeCall.InputTokens + activeCall.OutputTokens : 0;
        if (tokens > 0)
        {
            view.Text(["text-xs text-muted-foreground/50 mt-1"],
                $"{liveState?.Model} · {tokens} tokens · iter {liveState?.Iterations ?? 0}");
        }
    });
}
```

## Notes

- The block is rendered by *reading* a `LiveState` snapshot — your background runner mutates the same object, and the reactive system re-renders. Mutate-in-place is fine for a single live snapshot; copy-on-write is overkill here.
- Tool calls render with a spinner-vs-checkmark based on `IsComplete` — the same block appears throughout the call's lifetime, just with the icon flipping.
- The thinking-icon trick — `(unixMs / 500) % icons.Length` — animates without a `Task.Delay` loop because the parent reactive bumps every time the agent updates state. If you need it to animate when *nothing* is updating, attach a 500ms timer that bumps a `Reactive<int> _tick`.
- Show the `Iterations` count for multi-iteration agents (Refine, BestOf, plan-and-execute) — that number tells the user roughly where they are inside the loop.
- Use `bg-gradient-to-r from-muted/40 to-muted/20` to make the live block visually distinct from finished messages.

## See also

- `chatbot-streaming` — token-level streaming for the final answer; this pattern wraps that
- `multi-agent-parallel-discussion` — when several agents are running and each needs its own status block
