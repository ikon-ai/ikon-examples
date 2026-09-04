namespace Ikon.App.Patterns.Patterns;

// Pattern: run-trace-and-cost — see docs/patterns/run-trace-and-cost.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class RunTraceAndCost : IPatternDemo
{
    public string Slug => "run-trace-and-cost";
    public string Title => "What a run cost and what it did";
    public string Category => "Conversational AI";
    public void RenderDemo(IView view) => Render(view);

    private sealed record Answer(string Text);

    #region docsnippet:pattern-run-trace-and-cost
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
    #endregion
}
