namespace Ikon.App.Patterns.Patterns;

// Pattern: run-trace-and-cost — see docs/patterns/run-trace-and-cost.md.
// The example region below is the canonical body the doc extracts.
internal sealed class RunTraceAndCost : IPatternDemo
{
    public string Slug => "run-trace-and-cost";
    public string Title => "What a run cost and what it did";
    public string Category => "Conversational AI";

    private sealed record Answer(string Text);

    // The gallery never calls the model: the buttons load the traces of two runs that finished, a
    // short one and one that kept re-reading its own output. A run cut off at the output cap throws
    // instead of returning a trace, so there is none to show for it.
    private static readonly EmergenceTrace CompletedTrace = new(
        iterations: 2, toolCalls: 0, duration: TimeSpan.FromSeconds(4.8), finishReason: "end_turn",
        inputTokens: 1_840, cachedInputTokens: 12_400, outputTokens: 612);

    private static readonly EmergenceTrace LongTrace = new(
        iterations: 6, toolCalls: 0, duration: TimeSpan.FromSeconds(21.3), finishReason: "end_turn",
        inputTokens: 9_215, cachedInputTokens: 61_900, outputTokens: 3_480);

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Row(["gap-2 flex-wrap"], content: row =>
            {
                row.Button([Button.OutlineSm], text: "Show a completed run", onClick: async () => _trace.Value = CompletedTrace);
                row.Button([Button.OutlineSm], text: "Show a long run", onClick: async () => _trace.Value = LongTrace);
            });
            col.Text(["text-xs text-zinc-400"], "Sample traces; no model is called");
            Render(col);
        });
    }

    #region example:pattern-run-trace-and-cost
    private readonly ClientReactive<EmergenceTrace?> _trace = new(null);
    private readonly ClientReactiveList<string> _activity = new();

    /// <summary>
    /// FinalWithTraceAsync is the awaited form that also hands back the trace. Plain
    /// `await Emerge.Run&lt;T&gt;(...)` returns only the result, so a run whose cost or tool use
    /// matters has to ask for the trace at the point it is started.
    /// </summary>
    private async Task AskAsync(string question)
    {
        // The trace belongs to the client who asked, so the fields are ClientReactive. Their .Value
        // needs that client's scope. Capture the session here, while the action callback's scope
        // is active, and write to it by id -- ReactiveScope.ClientId throws where no client scope
        // exists (a timer, an endpoint handler, a background loop), so hand those the id instead.
        var clientSessionId = ReactiveScope.ClientId;

        var (result, _, trace) = await Emerge.Run<Answer>(LLMModel.Claude46Sonnet, pass =>
        {
            pass.Command = question;

            // Reasoning is a cost lever, not a quality dial: High spends tokens the trace will
            // show under OutputTokens.
            pass.ReasoningEffort = ReasoningEffort.Low;
        }).FinalWithTraceAsync();

        _trace.SetFor(clientSessionId, trace);

        // Result stays NULLABLE on this path -- a run can complete without producing one. The
        // trace does not say why (its FinishReason is the provider's ordinary one); the reason is
        // Completed<T>.NoResultReason, which only the streamed event carries.
        if (result is null)
        {
            _activity.AddFor(clientSessionId, $"No result after {trace.Iterations} iterations");
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
            // InputTokens already excludes the cached read, so the two are shown side by side and
            // the request's total input is their sum plus CacheCreationInputTokens.
            col.Text(["text-muted-foreground text-xs"],
                text: $"{trace.InputTokens:N0} in + {trace.CachedInputTokens:N0} cached, "
                    + $"{trace.OutputTokens:N0} out, {trace.Duration.TotalSeconds:0.0}s, "
                    + $"{trace.Iterations} iterations, {trace.ToolCalls} tool calls");

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
