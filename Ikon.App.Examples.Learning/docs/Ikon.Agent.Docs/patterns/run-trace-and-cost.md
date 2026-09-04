<!-- mined-from: Ikon.App.Patterns -->
# Run Trace And Cost — Asking What That Actually Did

`await Emerge.Run<T>(...)` returns the result and nothing else. When the cost, the tool use or the
reason a run came back empty matters, ask for the **trace** at the point the run is started —
there is no way to recover it afterwards.

`FinalWithTraceAsync()` gives `(T? Result, KernelContext Context, EmergenceTrace Trace)`. The
result stays **nullable** on that path, and the trace is what explains why.

## When to use

A cost or usage display, a debug panel, an audit trail of what an agent did, or any run where
"it just returned nothing" needs an answer better than a shrug.

## Notes

- **`CachedInputTokens` is a SUBSET of `InputTokens`, not an addition.** Summing them
  double-counts the cached read and overstates the bill. `CacheCreationInputTokens` is the separate
  write cost.
- **`IsTruncated` is the flag to branch on.** It means the run hit the output cap, so the result is
  half-formed rather than wrong — the fix is a bigger budget or a smaller ask, not a retry.
- `FinishReason` is what to show when the result is null. `Error` carries the exception when one
  was captured rather than thrown.
- `ToolCallHistory` is what the model actually did, in order — each `FunctionCall` carries the
  `Function`, `ParametersJson` and a `CallId`. That is the audit trail behind "why did it answer
  that".
- `Iterations` counts model turns and `ToolCalls` counts tool invocations; a run with many
  iterations and few tool calls is usually one that kept re-reading its own output.
- `ReasoningEffort` is a **cost** lever, not a quality dial. `High` spends tokens that show up
  under `OutputTokens`; start `Low` and raise it only when the task provably needs it.
- Streaming instead? The same information arrives as events — `Retry<T>` (with `Reason`,
  `AttemptNumber`, `MaxAttempts`) and `Completed<T>` (which carries the `Trace`).
- `FinalAsync()` is the same shape without the trace, for when only the updated `KernelContext` is
  wanted.

## Snippet

```csharp
private readonly ClientReactive<EmergenceTrace?> _trace = new(null);
private readonly ClientReactiveList<string> _activity = new();

/// <summary>
/// FinalWithTraceAsync is the awaited form that also hands back the trace. Plain
/// `await Emerge.Run&lt;T&gt;(...)` returns only the result, so a run whose cost or tool use
/// matters has to ask for the trace at the point it is started.
/// </summary>
private async Task AskAsync(string question)
{
    var (result, _, trace) = await Emerge.Run<Answer>(LLMModel.Claude46Sonnet, pass =>
    {
        pass.Command = question;

        // Reasoning is a cost lever, not a quality dial: High spends tokens the trace will
        // show under OutputTokens.
        pass.ReasoningEffort = ReasoningEffort.Low;
    }).FinalWithTraceAsync();

    _trace.Value = trace;

    // Result stays NULLABLE on this path -- a run can complete without producing one, which
    // is exactly the case the trace explains.
    if (result is null)
    {
        _activity.Add($"No result: {trace.FinishReason}");
    }
}

private void Render(IView view)
{
    if (_trace.Value is not { } trace)
    {
        return;
    }

    view.Column(["gap-1"], content: col =>
    {
        // CachedInputTokens is a SUBSET of InputTokens, not an addition -- summing them
        // double-counts the cached read and overstates the bill.
        col.Text(["text-muted-foreground text-xs"],
            text: $"{trace.InputTokens:N0} in ({trace.CachedInputTokens:N0} cached), "
                + $"{trace.OutputTokens:N0} out, {trace.Duration.TotalSeconds:0.0}s, "
                + $"{trace.Iterations} iterations, {trace.ToolCalls} tool calls");

        // IsTruncated is the one to branch on: the run hit the output cap, so the result is
        // half-formed rather than wrong, and re-running with a bigger budget is the fix.
        if (trace.IsTruncated)
        {
            col.Text(["text-destructive text-sm"], text: "Answer was cut short — ask for less at once.");
        }

        // ToolCallHistory is what the model actually did, in order: the audit trail for
        // "why did it answer that".
        foreach (var call in trace.ToolCallHistory)
        {
            col.Text(["text-muted-foreground text-xs"], key: call.CallId,
                text: $"{call.Function.Name}({call.ParametersJson})");
        }
    });
}
```

## See also

- `emergence-event-feed` — watching a run as it happens rather than after.
- `best-of-with-rubric` — where a trace is handed to the scorer for each candidate.
