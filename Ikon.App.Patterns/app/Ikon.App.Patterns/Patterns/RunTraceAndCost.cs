namespace Ikon.App.Patterns.Patterns;

// Pattern: run-trace-and-cost — see docs/patterns/run-trace-and-cost.md.
// The example region below is the canonical body the doc extracts.
internal sealed class RunTraceAndCost : IPatternDemo
{
    public string Slug => "run-trace-and-cost";
    public string Title => "What a run cost and what it did";
    public string Category => "Conversational AI";

    private sealed record Answer(string Text);

    // The gallery never calls the model: the buttons load the trace of a run that finished and of
    // one that hit its output cap, the two branches the render takes.
    private static readonly EmergenceTrace CompletedTrace = new(
        iterations: 2, toolCalls: 0, duration: TimeSpan.FromSeconds(4.8), finishReason: "end_turn",
        inputTokens: 1_840, cachedInputTokens: 12_400, outputTokens: 612);

    private static readonly EmergenceTrace TruncatedTrace = new(
        iterations: 1, toolCalls: 0, duration: TimeSpan.FromSeconds(21.3), finishReason: "max_tokens",
        inputTokens: 2_215, cachedInputTokens: 12_400, outputTokens: 8_192);

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Row(["gap-2 flex-wrap"], content: row =>
            {
                row.Button([Button.OutlineSm], text: "Show a completed run", onClick: async () => _trace.Value = CompletedTrace);
                row.Button([Button.OutlineSm], text: "Show a run cut short", onClick: async () => _trace.Value = TruncatedTrace);
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
        // needs that client's scope, which a run started from a timer, an endpoint handler or a
        // background loop does not carry -- capture the session here and write to it by id.
        var clientSessionId = ReactiveScope.ClientId;

        var (result, _, trace) = await Emerge.Run<Answer>(LLMModel.Claude46Sonnet, pass =>
        {
            pass.Command = question;

            // Reasoning is a cost lever, not a quality dial: High spends tokens the trace will
            // show under OutputTokens.
            pass.ReasoningEffort = ReasoningEffort.Low;
        }).FinalWithTraceAsync();

        _trace.SetFor(clientSessionId, trace);

        // Result stays NULLABLE on this path -- a run can complete without producing one, which
        // is exactly the case the trace explains.
        if (result is null)
        {
            _activity.AddFor(clientSessionId, $"No result: {trace.FinishReason}");
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
