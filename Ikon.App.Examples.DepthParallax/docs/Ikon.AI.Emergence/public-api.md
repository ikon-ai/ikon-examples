# Ikon.AI.Emergence Public API

namespace Ikon.AI.Emergence
  sealed class AgentScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    // Prepended to the solver's system prompt so ensemble members differentiate. Defaults to Solver{Index}; set a meaningful value (e.g. "the security reviewer") to steer each member.
    string? Role { get; set; }
    // Same as CandidateScope<T>.Seed: drives divergence between solvers, not a sampler seed and not reproducible.
    int? Seed { get; set; }
  sealed class BestOfOptions<T> : EmergeScope<T>
    ctor()
    // The ScoreBreakdown is non-null exactly when ScoreDetailed produced one, and null when ranking with the plain Score delegate.
    Func<T, ScoreBreakdown?, string>? BuildCriticFeedback { get; set; }
    Action<CandidateScope<T>>? CandidateConfig { get; set; }
    int Count { get; set; }
    bool CriticMustImprove { get; set; }
    EmergeScope<T> CriticScope { get; }
    // Runs a critic pass over the winning candidate and keeps its result when it scores better (see CriticMustImprove). The prompt comes from BuildCriticFeedback; without one, the best candidate and its score are appended to CriticScope's Command.
    bool EnableCritic { get; set; }
    // Ignored when ScoreDetailed is set.
    Func<T, EmergenceTrace, double>? Score { get; set; }
    // Ranks candidates by ScoreBreakdown.TotalScore and passes the breakdown to BuildCriticFeedback. Takes precedence over Score.
    Func<T, EmergenceTrace, ScoreBreakdown>? ScoreDetailed { get; set; }
    void Candidate(Action<CandidateScope<T>> configure)
    void Critic(Action<EmergeScope<T>> configure)
  sealed class CandidateScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    // Not a sampler seed (the chat models expose none), so it does not make a run reproducible — it only drives sibling candidates to diverge.
    int? Seed { get; set; }
  // Return this from a tool body to end the run immediately after the current tool batch instead of looping back to the model; the run completes with a default result and the completion surfaces as a Completed<T> event. Create via Emerge.Complete<TValue> or Emerge.Complete.
  class Complete
  // Value is written to the model transcript as the tool result before the run completes.
  sealed class Complete<TValue> : Complete
    TValue Value { get; }
  sealed record Completed<T> : EmergeEvent<T>
    ctor(T? Result, KernelContext Context, EmergenceTrace Trace)
    KernelContext Context { get; init; }
    T? Result { get; init; }
    EmergenceTrace Trace { get; init; }
  static class Emerge
    // Defaults to LLMModel.Claude45Haiku (cheap and fast — right for short transformations); use the model overload for a stronger tier. Never returns null; throws EmergenceStoppedException if the run stops or completes without a reply.
    static Task<string> AskAsync(string command, CancellationToken ct = default)
    static Task<string> AskAsync(string command, LLMModel model, CancellationToken ct = default)
    static Task<string> AskAsync(string command, string model, CancellationToken ct = default)
    // Asks the model for JSON matching T's schema; defaults to LLMModel.Claude45Haiku. Throws EmergenceStoppedException when the run stops, completes without a result, or returns invalid JSON.
    static Task<T> AskAsync<T>(string command, CancellationToken ct = default) where T : class
    static Task<T> AskAsync<T>(string command, LLMModel model, CancellationToken ct = default) where T : class
    static Task<T> AskAsync<T>(string command, string model, CancellationToken ct = default) where T : class
    static EmergeRun<T> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> BestOf<T>(string model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> BestOf<T>(string model, KernelContext context, Action<BestOfOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    // Return the result from a tool body to complete the run right after the current tool batch, with value fed to the transcript as the tool result.
    static Complete<TValue> Complete<TValue>(TValue value)
    // Return from a tool body to complete the run after the current tool batch; the tool result is recorded as a plain completion marker with no value.
    static Complete Complete()
    static EmergeRun<T> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> EnsembleMerge<T>(string model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> EnsembleMerge<T>(string model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<TResult> MapReduce<TInput, TMapped, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TInput, TMapped, TResult>> configure, CancellationToken ct = default)
    static EmergeRun<TResult> MapReduce<TInput, TMapped, TResult>(string model, KernelContext context, Action<MapReduceOptions<TInput, TMapped, TResult>> configure, CancellationToken ct = default)
    static EmergeRun<TResult> MapReduce<TInput, TMapped, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TInput, TMapped, TResult>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<TResult> MapReduce<TInput, TMapped, TResult>(string model, KernelContext context, Action<MapReduceOptions<TInput, TMapped, TResult>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Refine<T>(string model, KernelContext context, Action<RefineOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> Refine<T>(string model, KernelContext context, Action<RefineOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    // Awaiting returns a non-null T and throws EmergenceStoppedException if the run stops without a result. This overload creates a fresh KernelContext; pass an explicit one via the other overloads to seed input (images, prior turns) or carry conversation history across calls.
    static EmergeRun<T> Run<T>(LLMModel model, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(LLMModel model, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, KernelContext context, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> TreeSearch<T>(string model, KernelContext context, Action<TreeSearchOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> TreeSearch<T>(string model, KernelContext context, Action<TreeSearchOptions<T>> configure, ILLM llm, CancellationToken ct = default)
  abstract record EmergeEvent<T>
  static class EmergeEventExtensions
    // Returns the result together with the updated KernelContext (for conversation continuity). The result stays nullable — a run can complete without producing one — so guard it before use.
    static Task<(T? Result, KernelContext Context)> FinalAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Like FinalAsync<T> but also returns the EmergenceTrace (duration, token usage, tool-call history). The result stays nullable.
    static Task<(T? Result, KernelContext Context, EmergenceTrace Trace)> FinalWithTraceAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Drains the stream and returns the completed result — the same thing awaiting an EmergeRun<T> does. Never returns null; throws EmergenceStoppedException if the run stops or completes without a result. Use FinalAsync<T> when you need the updated KernelContext back or want to handle a missing result yourself.
    static Task<T> ResultAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
  sealed class EmergePass<T>
    ctor()
    bool CaseInsensitiveJson { get; set; }
    string? Command { get; set; }
    KernelContext Context { get; }
    bool HasFunctionResults { get; }
    bool HasNewFunctionResults { get; }
    bool? IncludeJsonExample { get; set; }
    bool IsStopped { get; }
    int Iteration { get; }
    string JsonExample { get; }
    string JsonSchema { get; }
    int? MaxIterations { get; set; }
    int? MaxOutputTokens { get; set; }
    int? MaxRetries { get; set; }
    int? MaxToolCalls { get; set; }
    TimeSpan? MaxWallTime { get; set; }
    // Null inherits the run's model; set it to override the model for this pass only.
    LLMModel? Model { get; set; }
    string? ModelName { get; set; }
    // Tools named here are treated as side-effect-free: the executor runs consecutive calls to them from one model turn concurrently, while results are still recorded in the model's original order. Any tool not listed acts as a barrier and runs alone.
    ISet<string> ReadOnlyToolNames { get; }
    ReasoningEffort? ReasoningEffort { get; set; }
    int? ReasoningTokenBudget { get; set; }
    IReadOnlyList<ModelRegion>? Regions { get; set; }
    TimeSpan? RetryDelay { get; set; }
    int? SkipLastNMessages { get; set; }
    string? StopReason { get; }
    string? SystemPrompt { get; set; }
    double? Temperature { get; set; }
    TimeSpan? Timeout { get; set; }
    IList<Function> Tools { get; }
    bool? UseCitations { get; set; }
    bool UseJson { get; set; }
    int? UseLastNMessages { get; set; }
    void Stop(string? reason = null)
    void UseLastMessages(int count, int skipLast = 0)
  // Both awaitable (one-shot non-null result, throws EmergenceStoppedException on failure) and enumerable (event stream). Single-shot: consumed exactly once — awaiting twice hands back the same result, but mixing the two shapes (enumerate then await, or the reverse) throws.
  sealed class EmergeRun<T> : IAsyncEnumerable<EmergeEvent<T>>
    IAsyncEnumerator<EmergeEvent<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    // Awaiting drains the stream and returns the completed result: never null, and throws EmergenceStoppedException if the run stops without producing one.
    TaskAwaiter<T> GetAwaiter()
  class EmergeScope<T> : EmergeScopeBase
    ctor()
    // Defaults to true.
    bool CaseInsensitiveJson { get; set; }
    string JsonExample { get; }
    string JsonSchema { get; }
    // Defaults to true for every T except string.
    bool UseJson { get; set; }
  abstract class EmergeScopeBase
    string? Command { get; set; }
    bool? IncludeJsonExample { get; set; }
    int? MaxIterations { get; set; }
    int? MaxOutputTokens { get; set; }
    int? MaxRetries { get; set; }
    int? MaxToolCalls { get; set; }
    TimeSpan? MaxWallTime { get; set; }
    LLMModel? Model { get; set; }
    string? ModelName { get; set; }
    ReasoningEffort? ReasoningEffort { get; set; }
    int? ReasoningTokenBudget { get; set; }
    IReadOnlyList<ModelRegion>? Regions { get; set; }
    TimeSpan? RetryDelay { get; set; }
    int? SkipLastNMessages { get; set; }
    string? SystemPrompt { get; set; }
    double? Temperature { get; set; }
    TimeSpan? Timeout { get; set; }
    IList<Function> Tools { get; }
    bool? UseCitations { get; set; }
    int? UseLastNMessages { get; set; }
    void UseLastMessages(int count, int skipLast = 0)
  enum EmergenceStatus
    Completed
    Stopped
    Failed
  class EmergenceStoppedException : Exception
    ctor(EmergenceStatus status, string? stopReason)
    ctor(EmergenceStatus status, string? stopReason, Exception innerException)
    EmergenceStatus Status { get; }
    string? StopReason { get; }
  sealed record EmergenceTrace
    ctor()
    ctor(int iterations, int toolCalls, TimeSpan duration, IReadOnlyList<FunctionCall>? toolCallHistory = null, string? finishReason = null, Exception? error = null, long inputTokens = 0, long cachedInputTokens = 0, long cacheCreationInputTokens = 0, long outputTokens = 0)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    TimeSpan Duration { get; init; }
    Exception? Error { get; init; }
    string? FinishReason { get; init; }
    long InputTokens { get; init; }
    bool IsTruncated { get; }
    int Iterations { get; init; }
    long OutputTokens { get; init; }
    IReadOnlyList<FunctionCall> ToolCallHistory { get; init; }
    int ToolCalls { get; init; }
  sealed class EnsembleMergeOptions<T> : EmergeScope<T>
    ctor()
    int MaxParallel { get; set; }
    EmergeScope<T> MergerScope { get; }
    Action<AgentScope<T>>? SolverConfig { get; set; }
    int SolverCount { get; set; }
    void Merger(Action<EmergeScope<T>> configure)
    void Solver(Action<AgentScope<T>> configure)
  sealed record FoundSection
    ctor(string NodeId, string Path, string Content, string Relevance, int? Page = null)
    string Content { get; init; }
    string NodeId { get; init; }
    int? Page { get; init; }
    string Path { get; init; }
    string Relevance { get; init; }
  static class KernelContextExtensions
    static IReadOnlyList<FunctionCall> GetFunctionCalls(this KernelContext ctx, int take = 10)
    static IReadOnlyList<FunctionResultPart> GetFunctionResults(this KernelContext ctx, int take = 10)
    static bool HasFunctionResults(this KernelContext ctx)
    // Keeps the last take message blocks (after ignoring the last skipLast), then advances the start to the next User block so the result never begins on an orphan Model or FunctionResult turn (which providers reject). Instructions and all other fields are preserved.
    static KernelContext TrimToLastMessages(this KernelContext ctx, int take, int skipLast = 0)
  // Each TInput chunk is mapped by its own LLM call into a TMapped, then all mapped results are reduced by one final call into the TResult. Chunks are passed to the map prompt as JSON, so any serializable type works (a string per chunk is the common case).
  sealed class MapReduceOptions<TInput, TMapped, TResult> : EmergeScope<TResult>
    ctor()
    // Set this or Input; each chunk is one map call.
    IReadOnlyList<TInput>? Chunks { get; set; }
    // Split into chunks by Split; without a Split it is mapped as a single chunk. Alternative to Chunks.
    TInput? Input { get; set; }
    EmergeScope<TMapped> MapScope { get; }
    int MaxParallel { get; set; }
    EmergeScope<TResult> ReduceScope { get; }
    Func<TInput, IEnumerable<TInput>>? Split { get; set; }
    void Map(Action<EmergeScope<TMapped>> configure)
    void Reduce(Action<EmergeScope<TResult>> configure)
  // Call ConnectAsync before reading Tools or calling a tool — it performs the MCP handshake and populates the tool list. Uses Streamable HTTP transport.
  sealed class McpClient : IDisposable
    ctor(string endpoint, Dictionary<string, string>? headers = null)
    IReadOnlyList<McpTool> Tools { get; }
    Task<string> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = default)
    // Returns the content plus a pagination cursor; pass a cursor from a previous response to fetch the next page.
    Task<McpToolResult> CallToolRawAsync(string name, JsonElement arguments, string? cursor = null, CancellationToken ct = default)
    Task ConnectAsync(CancellationToken ct = default)
    void Dispose()
  record McpTool
    ctor(string Name, string? Description, JsonElement? InputSchema)
    string? Description { get; init; }
    JsonElement? InputSchema { get; init; }
    string Name { get; init; }
  record McpToolResult
    ctor(string Content, string? NextCursor)
    string Content { get; init; }
    string? NextCursor { get; init; }
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
    ScoreBreakdownBuilder<T> Metric(string name, double weight, Func<T, double> evaluate)
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
  sealed record ToolCallResult<T> : EmergeEvent<T>
    ctor(FunctionCall Call, LLMEvent[] Events, object Result)
    FunctionCall Call { get; init; }
    LLMEvent[] Events { get; init; }
    object Result { get; init; }
  sealed class TreeSearchOptions<T> : EmergeScope<T>
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

namespace Ikon.AI.Emergence.Structured
  // Tag matching is case-insensitive and tolerates attributes and formatting variations.
  static class StructuredTagParser
    // Returns the first occurrence's inner content, or null if the tag is absent.
    static string? GetTagContent(string content, string tagName)
    static bool HasTag(string content, string tagName)
    static StructuredTagParser.ParsedResponse Parse(string content, params string[] tagNames)
  sealed record StructuredTagParser.ParsedBlock
    ctor(string TagName, string Content, int StartIndex, int EndIndex)
    string Content { get; init; }
    int EndIndex { get; init; }
    int StartIndex { get; init; }
    string TagName { get; init; }
  sealed record StructuredTagParser.ParsedResponse
    ctor(string PlainText, IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks)
    IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks { get; init; }
    string PlainText { get; init; }

namespace Ikon.AI.Emergence.Tree
  record ContentSection
    ctor(string Title, string Content, int? Page = null)
    string Content { get; init; }
    int? Page { get; init; }
    string Title { get; init; }
  interface IContentReader
    IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = default)
  class StringContentReader : IContentReader
    ctor(string content)
    IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = default)
  class TreeIndex
    ctor()
    ctor(TreeNode root)
    TreeNode Root { get; set; }
    static EmergeRun<TreeIndex> BuildAsync(LLMModel model, string content, TreeIndexOptions? options = null, CancellationToken ct = default)
    static EmergeRun<TreeIndex> BuildAsync(string model, string content, TreeIndexOptions? options = null, CancellationToken ct = default)
    static EmergeRun<TreeIndex> BuildAsync(LLMModel model, IContentReader reader, TreeIndexOptions? options = null, CancellationToken ct = default)
    static EmergeRun<TreeIndex> BuildAsync(string model, IContentReader reader, TreeIndexOptions? options = null, CancellationToken ct = default)
    TreeNode? FindById(string id)
    void RebuildIndex()
    string ToTableOfContents(int maxDepth = -1)
    IEnumerable<TreeNode> Traverse()
  class TreeIndexOptions
    ctor()
    bool GenerateSummaries { get; set; }
    int MaxDepth { get; set; }
    int MaxSummaryTokens { get; set; }
  class TreeNode
    ctor()
    ctor(string id, string title, string content = "")
    List<TreeNode> Children { get; }
    string Content { get; set; }
    int Depth { get; }
    string Id { get; set; }
    int? Page { get; set; }
    TreeNode? Parent { get; }
    string Summary { get; set; }
    string Title { get; set; }
    void AddChild(TreeNode child)
    string GetPath()
    IEnumerable<TreeNode> Traverse()
