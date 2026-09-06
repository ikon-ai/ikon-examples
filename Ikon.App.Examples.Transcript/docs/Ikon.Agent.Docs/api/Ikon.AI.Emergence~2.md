namespace Ikon.AI.Emergence
  record McpTool
    ctor(string Name, string? Description, JsonElement? InputSchema)
    string? Description { get; init; }
    JsonElement? InputSchema { get; init; }
    string Name { get; init; }
  record McpToolResult
    ctor(string Content, string? NextCursor)
    string Content { get; init; }
    string? NextCursor { get; init; }
  // The whole seam for substituting a model: a lambda satisfies it. It carries no capabilities because those describe the MODEL, which every overload taking one of these is passed separately — a substitute reporting its own would be either a copy of the real ones or a fiction the run then acted on.
  delegate ModelStream
    IAsyncEnumerable<LLMEvent> ModelStream(KernelContext context, CancellationToken ct = default)
  sealed record ModelText<T> : EmergeEvent<T>
    ctor(string Text)
    string Text { get; init; }
  sealed record NavigationDecision
    ctor(string Reasoning = "", bool Complete = false)
    bool Complete { get; init; }
    string Reasoning { get; init; }
  sealed record Progress<T> : EmergeEvent<T>
    ctor(string Message)
    string Message { get; init; }
  sealed class RefineOptions<T> : EmergeScope<T>
    ctor()
    EmergeScope<T> InitialScope { get; }
    int MaxRefinements { get; set; }
    EmergeScope<T> RefinementScope { get; }
    Func<T, EmergenceTrace, Task<bool>>? ShouldContinue { get; set; }
    void Initial(Action<EmergeScope<T>> configure)
    void Refinement(Action<EmergeScope<T>> configure)
  sealed record Retry<T> : EmergeEvent<T>
    ctor(string Reason, int AttemptNumber, int MaxAttempts)
    int AttemptNumber { get; init; }
    int MaxAttempts { get; init; }
    string Reason { get; init; }
  sealed class ScoreBreakdown
    ctor()
    IReadOnlyList<ScoreMetric> Metrics { get; init; }
    double TotalScore { get; init; }
    ScoreMetric? Weakest { get; init; }
    string FormatBreakdown()
  sealed class ScoreBreakdownBuilder<T>
    ctor()
    // evaluate must return a score in [0, 1]: values outside that range are clamped, so a rubric on a 0..10 or 0..100 scale collapses to 1.0 for every candidate and the ranking stops discriminating. Divide by the scale's maximum in the callback.
    ScoreBreakdownBuilder<T> Metric(string name, double weight, Func<T, double> evaluate)
    // Each metric score is clamped to [0, 1] and the total is the weight-normalized sum.
    ScoreBreakdown Score(T value)
  sealed class ScoreMetric
    ctor()
    string Name { get; init; }
    double Score { get; init; }
    double Weight { get; init; }
    double WeightedScore { get; }
  sealed record Stage<T> : EmergeEvent<T>
    ctor(string Name)
    string Name { get; init; }
  sealed record Stopped<T> : EmergeEvent<T>
    ctor(KernelContext Context, string? Reason)
    KernelContext Context { get; init; }
    string? Reason { get; init; }
  // Counts are cumulative running totals across all iterations, not per-iteration deltas — take the last event's values, never sum them.
  sealed record TokenUpdate<T> : EmergeEvent<T>
    ctor(long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
  sealed record ToolCallPlanned<T> : EmergeEvent<T>
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  // IsError is true when the call did not complete — the tool body threw, or it returned a result marked as an error. Result then holds the failure text that was fed back to the model rather than a tool return value.
  sealed record ToolCallResult<T> : EmergeEvent<T>
    ctor(FunctionCall Call, LLMEvent[] Events, object Result, bool IsError = false)
    FunctionCall Call { get; init; }
    LLMEvent[] Events { get; init; }
    bool IsError { get; init; }
    object Result { get; init; }
  sealed class TreeSearchOptions : EmergeScope<TreeSearchResult>
    ctor()
    TreeIndex? Index { get; set; }
    int MaxResults { get; set; }
    int MaxSteps { get; set; }
    EmergeScope<NavigationDecision> NavigatorScope { get; }
    string Query { get; set; }
    void Navigator(Action<EmergeScope<NavigationDecision>> configure)
  sealed record TreeSearchResult
    ctor(List<FoundSection> Sections, string ReasoningTrace = "")
    string ReasoningTrace { get; init; }
    List<FoundSection> Sections { get; init; }
