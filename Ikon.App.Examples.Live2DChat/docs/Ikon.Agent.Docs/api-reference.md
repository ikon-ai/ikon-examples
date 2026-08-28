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
    // Set this or ScoreDetailed — with neither, every candidate scores 0.0 and the FIRST candidate always wins after paying for all Count runs. Ignored when ScoreDetailed is set. Candidates run sequentially, so budget wall time for Count full calls.
    Func<T, EmergenceTrace, double>? Score { get; set; }
    // Ranks candidates by ScoreBreakdown.TotalScore and passes the breakdown to BuildCriticFeedback. Takes precedence over Score.
    Func<T, EmergenceTrace, ScoreBreakdown>? ScoreDetailed { get; set; }
    void Candidate(Action<CandidateScope<T>> configure)
    // Configuring the critic also enables it — an explicitly configured critic that silently never ran was the trap; set EnableCritic back to false afterward for the rare case of pre-configuring a critic to toggle later.
    void Critic(Action<EmergeScope<T>> configure)
  sealed class CandidateScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    // Not a sampler seed (the chat models expose none), so it does not make a run reproducible — it only drives sibling candidates to diverge.
    int? Seed { get; set; }
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
    // Return this from a tool body to end the run right after the current tool batch, with toolResult fed to the transcript as the tool result. The value also becomes the run result when it is assignable to the run's T; otherwise the run completes with default(T).
    static EndRun<TValue> EndRun<TValue>(TValue toolResult)
    // Return from a tool body to end the run after the current tool batch; the completion is recorded as a plain marker with no value and the run completes with default(T).
    static EndRun EndRun()
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
    static EmergeRun<TreeSearchResult> TreeSearch(LLMModel model, KernelContext context, Action<TreeSearchOptions> configure, CancellationToken ct = default)
    static EmergeRun<TreeSearchResult> TreeSearch(string model, KernelContext context, Action<TreeSearchOptions> configure, CancellationToken ct = default)
    static EmergeRun<TreeSearchResult> TreeSearch(LLMModel model, KernelContext context, Action<TreeSearchOptions> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<TreeSearchResult> TreeSearch(string model, KernelContext context, Action<TreeSearchOptions> configure, ILLM llm, CancellationToken ct = default)
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
    // Wins over Model when both are set; null inherits the run's model.
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
    // Null does NOT mean unbounded — the executor caps at 10 iterations and stops the run with "MaxIterationsExceeded", which an awaited run surfaces as EmergenceStoppedException. Raise this explicitly for long tool loops.
    int? MaxIterations { get; set; }
    // Default when null: 16000.
    int? MaxOutputTokens { get; set; }
    // Default when null: 3 retries.
    int? MaxRetries { get; set; }
    // Default when null: 50 tool calls, then the run stops with "MaxToolCallsExceeded".
    int? MaxToolCalls { get; set; }
    // Default when null: 5 minutes of wall time, then the run stops with "MaxWallTimeExceeded".
    TimeSpan? MaxWallTime { get; set; }
    LLMModel? Model { get; set; }
    // Wins over Model when both are set.
    string? ModelName { get; set; }
    ReasoningEffort? ReasoningEffort { get; set; }
    int? ReasoningTokenBudget { get; set; }
    IReadOnlyList<ModelRegion>? Regions { get; set; }
    TimeSpan? RetryDelay { get; set; }
    int? SkipLastNMessages { get; set; }
    string? SystemPrompt { get; set; }
    // Default when null: 0.7.
    double? Temperature { get; set; }
    // Default when null: 15 minutes.
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
  // Return this from a tool body to end the run immediately after the current tool batch instead of looping back to the model. The value (if any) is fed to the model transcript as this tool's result AND becomes the run's result when it is assignable to T; EndRun() with no value, or a value of an unrelated type, completes with default(T). Both await Emerge.Run<T>(...) and enumerating for the Completed<T> event observe that result. Create via Emerge.EndRun<TValue> or Emerge.EndRun.
  class EndRun
  // ToolResult is written to the model transcript as the tool result and becomes the run's result when assignable to the run's result type.
  sealed class EndRun<TValue> : EndRun
    TValue ToolResult { get; }
  sealed class EnsembleMergeOptions<T> : EmergeScope<T>
    ctor()
    // Must be at least 1 — there is no "unbounded" sentinel.
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
    // Must be at least 1 — there is no "unbounded" sentinel.
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
  sealed record ToolCallResult<T> : EmergeEvent<T>
    ctor(FunctionCall Call, LLMEvent[] Events, object Result)
    FunctionCall Call { get; init; }
    LLMEvent[] Events { get; init; }
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
    // Also repairs the TreeNode.Parent and TreeNode.Depth links of nodes that were added to TreeNode.Children directly rather than through TreeNode.AddChild.
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
    // Prefer AddChild, which also sets the child's Parent and Depth; a node added to this list directly gets those links when the tree is put into a TreeIndex (or on TreeIndex.RebuildIndex), not before.
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

# Ikon.AI Public API

namespace Ikon.AI
  class AIException : Exception
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class AITimeoutException : RetryableAIException
    ctor(string message)
    ctor(TimeSpan configuredTimeout, string targetName)
    TimeSpan ConfiguredTimeout { get; }
    string TargetName { get; }
  // When a result's ResultKind is ResultKind.Url the payload lives behind a signed download link valid for roughly one hour; GetDataAsync returns the bytes either way, downloading transparently when needed.
  static class AssetOutputs
    static Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default)
    static Task<byte[]> GetDataAsync(this IResultPayload result, CancellationToken cancellationToken = default)
  // The name selects the model in the category's string-based APIs (e.g. new LLM("my-model")). An empty ApiKey means the endpoint needs no authentication header.
  abstract class CustomModel
    string ApiKey { get; init; }
    // Defaults to Name when left unset.
    string ApiModelName { get; init; }
    // The full URL including the API path, e.g. http://localhost:8000/v1/chat/completions.
    required string EndpointUrl { get; init; }
    // Must not collide with a built-in model name and must not contain dots or whitespace.
    required string Name { get; init; }
  // Register a model at app startup, then select it by name anywhere a model name string is accepted:
  // CustomModels.Instance.Register(new CustomLLMModel
  // {
  //     Name = "my-model",
  //     EndpointUrl = "http://localhost:8000/v1/chat/completions",
  //     Api = CustomLLMApi.OpenAICompletions,
  //     ApiKey = "sk-...",
  //     ContextWindowSize = 32768,
  // });
  //
  // var reply = await Emerge.AskAsync("Hello", "my-model");
  // Custom models always execute in the local process — calls never go through the Ikon RPC mechanism. Usage is reported with a .user suffix and billed as a flat per-request fee instead of per-token provider pricing. The registry is async-local (like CredentialStorage): register models on the main flow at startup, before spawning parallel work, so every flow sees them. Registering the same name again replaces the previous registration; instances constructed before the replacement keep the configuration they were created with.
  sealed class CustomModels : AsyncLocalInstance<CustomModels>
    ctor()
    bool IsRegistered(string name)
    void Register(CustomLLMModel model)
    void Register(CustomEmbeddingModel model)
    void Register(CustomRerankModel model)
    void Register(CustomClassificationModel model)
    // Removes the name from every category it is registered in; true when at least one registration was removed.
    bool Unregister(string name)
  interface IResultPayload
    byte[]? Data { get; }
    ResultKind Kind { get; }
    string? Url { get; }
  // Transient (network blip, server restart, flaky link) and therefore retryable — the RPC layer retries with a forced reconnect, and exhausted attempts still surface as retryable.
  sealed class IkonServerConnectException : RetryableAIException
    ctor(string message)
    ctor(string message, Exception inner)
  // A reference clip for prompt-driven audio editing: the model preserves this clip's timing and structure while the prompt re-styles it. Supply the clip exactly one way: Data (with MimeType), Url, or AssetUri (resolved automatically).
  sealed record InputAudio
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    // End of the region to edit, in seconds. null means to the end.
    double? EndSeconds { get; init; }
    string? MimeType { get; init; }
    // Start of the region to edit, in seconds. null means from the beginning.
    double? StartSeconds { get; init; }
    // How strongly the output adheres to this reference, in [0, 1]; higher keeps the original melody/timing closer. null defaults to strong adherence.
    double? Strength { get; init; }
    string? Url { get; init; }
  // Supply the image exactly one way: inline via Data (with MimeType), by Url, or by AssetUri — all consumers resolve the asset to a URL. Type, Strength, and MaskDilution apply only to image-editing/inpainting models; depth, segmentation, mesh, and video generation ignore them.
  sealed record InputImage
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    double? MaskDilution { get; init; }
    string? MimeType { get; init; }
    double? Strength { get; init; }
    InputImageType Type { get; init; }
    string? Url { get; init; }
  enum InputImageType
    Normal
    Mask
  // A reference clip for video generation: footage the model is shown rather than asked to invent, addressed from the prompt the way reference images are. Supply the clip exactly one way: Data (with MimeType), Url, or AssetUri (resolved automatically). Providers impose their own length and size limits.
  sealed record InputVideo
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  static class ModelFailure
    static ModelFailureKind Classify(Exception exception)
  // Unlike the retryable/non-retryable split, which answers "should this call be tried again" while saying nothing about the cause, this answers "what does the failure say about the model" — which is what decides whether a human has to act.
  enum ModelFailureKind
    // Callers that gate on this should treat it as a real failure: an unrecognised error is far more likely to be a genuine defect than a benign one.
    Unknown
    // Transport error, timeout, throttling or a provider-side fault; says nothing about whether the model is healthy.
    Transient
    // The model has been removed, renamed or retired and the configuration has to be updated.
    Unavailable
    // Missing or rejected credentials, exhausted credits, or a quota that is not a transient rate limit. An operator has to act, but nothing is wrong with the model or the code.
    AccessDenied
    // No content, an unusable tool call, or output that failed validation. Non-deterministic by nature and often not reproducible on the next call.
    Quality
  enum ModelRegion
    Global
    Eu
    EuNorth
    EuWest
    EuCentral
    EuSouth
    Us
    UsEast
    UsWest
  class NonRetryableAIException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // An image produced by an analysis model (depth map, segmentation mask, preview). Kind tells how it was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record OutputImage : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    int Height { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
    int Width { get; init; }
  class RegionNotSupportedException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // With Auto the payload stays inline in-process; only when the result is returned from a remotely hosted AI function is it uploaded to a short-lived asset URL, and then only if it exceeds an internal size threshold (a few MB), keeping the protocol message small. Url always uploads, in any context. Check the result's ResultKind field to see which delivery was used.
  enum ResultDelivery
    Auto
    Url
  // Data guarantees the result's Data is non-null; Url guarantees its Url is non-null. Call result.GetDataAsync() (AssetOutputs.GetDataAsync) to get the bytes either way.
  enum ResultKind
    Data
    Url
  class RetryableAIException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // The URLs these clients fetch come from app code, from LLM tool arguments and from provider responses — none of which the platform controls. Checking the URL string is not enough: the name is resolved later, so a host that resolves to 169.254.169.254 passes any check made up front, and a redirect or a second DNS answer moves the target after the check. The decision therefore happens at SocketsHttpHandler.ConnectCallback, on the address actually being connected to. That covers every redirect hop, because each one connects again, and it closes DNS rebinding, because the address checked is the address used.
  static class SsrfGuard
    static SocketsHttpHandler CreateHandler()
    static bool IsAllowedScheme(Uri uri)

namespace Ikon.AI.Classification
  sealed record ClassificationDetail
    ctor()
    ctor(ClassificationLabel label, string originalCategory, bool isFlagged, double score)
    bool IsFlagged { get; init; }
    ClassificationLabel Label { get; init; }
    string OriginalCategory { get; init; }
    double Score { get; init; }
  // Supply Text, Data (with MimeType), Url, or AssetUri (resolved to a URL automatically).
  sealed record ClassificationInput
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Text { get; init; }
    string? Url { get; init; }
    static ClassificationInput FromMessagePart(IMessagePart messagePart)
  enum ClassificationLabel
    Unknown
    SafetyHateSpeech
    SafetyHarassment
    SafetySelfHarm
    SafetySexualContent
    SafetyChildAbuse
    SafetyViolence
    SafetyJailbreak
    SafetyCopyright
    SafetyDangerousContent
    SafetyHealth
    SafetyFinancial
    SafetyLegal
    SafetyPII
  enum ClassificationModel
    OpenAIOmniModeration
    MistralModeration
    // Not directly usable — select custom models (see CustomModels) by their registered name string.
    Custom
  static class ClassificationModelExtensions
    static string DisplayName(this ClassificationModel model)
  sealed record ClassificationResult
    ctor()
    List<ClassificationDetail> Details { get; init; }
    bool IsFlagged { get; init; }
  class ClassificationResultException : NonRetryableAIException
    ctor(ClassificationResult classificationResult)
    ctor(ClassificationResult classificationResult, Exception inner)
    ClassificationResult ClassificationResult { get; }
  sealed class Classifier : IClassifier
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ClassificationModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsImageInput { get; }
    TimeSpan Timeout { get; set; }
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, CancellationToken cancellationToken = default)
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, CancellationToken cancellationToken = default)
    Task<ClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a Classifier per call. Defaults to ClassificationModel.OpenAIOmniModeration (free); override via model. Read result.IsFlagged and per-label result.Details. Use the constructor plus the instance overloads for image/message-part input, a custom Timeout, or reusing one instance across many inputs.
    static Task<ClassificationResult> ClassifyAsync(string text, ClassificationModel model = OpenAIOmniModeration, CancellationToken cancellationToken = default)
    void Dispose()
    static ClassifierCapabilities GetCapabilities(ClassificationModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ClassificationModel model)
  sealed class ClassifierCapabilities : IClassifierInfo
    ctor()
    bool SupportsImageInput { get; init; }
  class ClassifierException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum CustomClassificationApi
    OpenAI
    Mistral
  sealed class CustomClassificationModel : CustomModel
    ctor()
    required CustomClassificationApi Api { get; init; }
    bool SupportsImageInput { get; init; }
  interface IClassifier : IClassifierInfo, IDisposable
    // Defaults to 10 seconds.
    TimeSpan Timeout { get; set; }
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, CancellationToken cancellationToken = default)
    virtual Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, CancellationToken cancellationToken = default)
    virtual Task<ClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken = default)
  interface IClassifierInfo
    bool SupportsImageInput { get; }
  class NonRetryableClassifierException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.Database
  class DatabaseColumnInfo
    ctor()
    string ColumnName { get; set; }
    string DataType { get; set; }
    string? Description { get; set; }
    string? ExtraInfo { get; set; }
    string? ForeignKeyColumnName { get; set; }
    string? ForeignKeyTableName { get; set; }
    bool? IsForeignKey { get; set; }
    bool? IsPrimaryKey { get; set; }
    List<string>? Values { get; set; }
  // For app code prefer the typed factories (Trino, Postgres, Sqlite, BigQuery), passing the password from app.Secrets. CreateAsync instead reads every connection field from environment variables or space secrets, for shared pipelines.
  class DatabaseConnection : IDisposable
    string BigQueryDataset { get; set; }
    string BigQueryProjectId { get; set; }
    DatabaseType DatabaseType { get; set; }
    DbConnection DbConnection { get; set; }
    static DatabaseConnection BigQuery(string projectId, string dataset)
    static Task<DatabaseConnection> CreateAsync(DatabaseConnection.Config config)
    // Disposes the owned DbConnection — a pooled connection returns to its pool. Wrap per-request use in using; without it every construction leaks a live connection until the pool is exhausted.
    void Dispose()
    static DatabaseConnection Postgres(string host, int port, string database, string user, string password)
    static DatabaseConnection Sqlite(string path)
    static DatabaseConnection Trino(string host, int port, string catalog, string user, string password)
  class DatabaseConnection.Config
    ctor()
    string? EnvVarPrefix { get; set; }
    DatabaseConnection.SpaceSecret? SpaceSecret { get; set; }
  class DatabaseConnection.SpaceSecret
    ctor()
    string Prefix { get; set; }
    string SpaceId { get; set; }
  class DatabaseInfo
    ctor()
    DatabaseType DatabaseType { get; set; }
    List<string>? ExampleQuestions { get; set; }
    string? SqlCteCommand { get; set; }
    List<DatabaseTableInfo> Tables { get; set; }
  class DatabaseInfoExtractor
    ctor(DatabaseConnection databaseConnection)
    Task<DatabaseInfo> ExtractAsync(DatabaseInfoExtractor.Config config, CancellationToken cancellationToken)
  class DatabaseInfoExtractor.Config
    ctor()
    // Regex patterns matched against the three-part schema.table.column name.
    List<string>? ColumnExcludeRegex { get; set; }
    Dictionary<string, string> ColumnExtraInfo { get; set; }
    bool IncludeEmptyColumns { get; set; }
    int JsonSampleLengthLimit { get; set; }
    int JsonSampleRowLimit { get; set; }
    int NonTextSampleRowLimit { get; set; }
    // When empty the default depends on the database type (e.g. public for PostgreSQL).
    List<string>? Schemas { get; set; }
    List<string>? TableExcludeRegex { get; set; }
    Dictionary<string, string> TableExtraInfo { get; set; }
    // Regex patterns matched against schema.table (or just table); an empty/null list includes all.
    List<string>? TableIncludeRegex { get; set; }
    int TextSampleLengthLimit { get; set; }
    int TextSampleRowLimit { get; set; }
  class DatabaseTableInfo
    ctor()
    List<DatabaseColumnInfo> Columns { get; set; }
    string? Description { get; set; }
    string? ExtraInfo { get; set; }
    string TableName { get; set; }
  enum DatabaseType
    Unknown
    PostgreSql
    Sqlite
    BigQuery
    Trino
  sealed class ResultCell
    ctor(string column, object? value)
    string Column { get; }
    object? Value { get; }
  sealed class ResultRow
    ctor(IReadOnlyList<ResultCell> cells)
    IReadOnlyList<ResultCell> Cells { get; }
    // Null is returned both for a genuine SQL NULL and for a column that is not present — use TryGetValue to tell the two apart.
    object? this[string column] { get; }
    // Returns false only when no such column exists; a column present but holding SQL NULL returns true with value set to null.
    bool TryGetValue(string column, out object? value)
  sealed class ResultSet
    ctor(IReadOnlyList<string> columns, IReadOnlyList<ResultRow> rows, int limitedRowCount, int totalRowCount, CultureInfo culture)
    IReadOnlyList<string> Columns { get; }
    int LimitedRowCount { get; }
    IReadOnlyList<ResultRow> Rows { get; }
    int TotalRowCount { get; }
    static Task<ResultSet> Create(DbDataReader reader, int maxRows, CultureInfo? culture = null, List<string>? columnNames = null)
    string ToCsv()
    string ToJson()
    string ToMarkdown()
  static class SqlValidator
    // Rejects SQL carrying a write/side-effect keyword or a table outside allowedTables. It is a keyword blocklist plus a FROM/JOIN allowlist, NOT a dialect-aware parser, so it does not prove the statement is side-effect free. Where the query runs against real data, back it with a read-only transaction or role.
    static void ValidateReadOnly(string sql, IReadOnlySet<string> allowedTables)

namespace Ikon.AI.DepthEstimation
  sealed class DepthEstimator : IDepthEstimator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(DepthEstimatorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    Task<DepthEstimatorResult> EstimateAsync(byte[] imageData, string mimeType, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a DepthEstimator per call. Defaults to DepthEstimatorModel.DepthAnythingV2 (cheap+fast); override via model (Marigold is slower, higher quality). The depth map is in result.Depth (.Data/.MimeType). Use the constructor + EstimateDepthAsync for a URL source or the Marigold tuning fields.
    static Task<DepthEstimatorResult> EstimateAsync(byte[] imageData, string mimeType, DepthEstimatorModel model = DepthAnythingV2, CancellationToken cancellationToken = default)
    Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(DepthEstimatorModel model)
  sealed record DepthEstimatorConfig
    ctor()
    int? EnsembleSize { get; init; }
    InputImage InputImage { get; init; }
    int? NumInferenceSteps { get; init; }
    int? ProcessingResolution { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    TimeSpan Timeout { get; init; }
  class DepthEstimatorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum DepthEstimatorModel
    DepthAnythingV2
    Marigold
    Midas
  static class DepthEstimatorModelExtensions
    static string DisplayName(this DepthEstimatorModel model)
  sealed record DepthEstimatorResult
    ctor()
    OutputImage Depth { get; init; }
  interface IDepthEstimator : IDisposable
    Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = default)
  class NonRetryableDepthEstimatorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.Embeddings
  enum CustomEmbeddingApi
    OpenAI
    Cohere
    Mistral
    Google
    Jina
    Voyage
  sealed class CustomEmbeddingModel : CustomModel
    ctor()
    required CustomEmbeddingApi Api { get; init; }
    required int EmbeddingVectorSize { get; init; }
    // Larger batches are split automatically. Defaults to 96.
    int MaxInputCount { get; init; }
  enum EmbeddingEncoding
    Base64
    GzipBase64
  sealed class EmbeddingGenerator : IEmbeddingGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(EmbeddingModel model, IReadOnlyList<ModelRegion>? regions = null)
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    void Dispose()
    Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an EmbeddingGenerator per call. Defaults to EmbeddingModel.OpenAI3Small and EmbeddingType.Generic; override the model via model, and pass an explicit EmbeddingType when embedding documents vs. queries for asymmetric retrieval. Returns one float[] per input, in input order. Use the constructor + GenerateEmbeddingsAsync for per-request batch caps or a custom timeout.
    static Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingModel model = OpenAI3Small, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    Task<List<float[]>> GenerateEmbeddingsAsync(EmbeddingGeneratorConfig config, CancellationToken cancellationToken = default)
    static EmbeddingGeneratorCapabilities GetCapabilities(EmbeddingModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(EmbeddingModel model)
  sealed class EmbeddingGeneratorCapabilities : IEmbeddingGeneratorInfo
    ctor()
    int EmbeddingVectorSize { get; init; }
    int MaxInputCount { get; init; }
  sealed record EmbeddingGeneratorConfig
    ctor()
    List<string> Inputs { get; init; }
    // Per-request batch cap; larger input lists are split into batches of this size. 0 means the model's maximum.
    int MaxInputCount { get; init; }
    // Per-request; scaled up internally with the batch size.
    TimeSpan Timeout { get; init; }
    EmbeddingType Type { get; init; }
  class EmbeddingGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class EmbeddingItem
    ctor(string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, string embedding)
    string Context { get; init; }
    string Embedding { get; init; }
    float[] EmbeddingArray { get; }
    EmbeddingEncoding Encoding { get; init; }
    EmbeddingModel Model { get; init; }
    EmbeddingType Type { get; init; }
    static Task<EmbeddingItem> CreateAsync(string input, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, CancellationToken cancellationToken = default)
    static Task<EmbeddingItem> CreateAsync(float[] embedding, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding)
  enum EmbeddingModel
    OpenAIAda2
    OpenAI3Small
    OpenAI3Large
    CohereEmbed4
    MistralEmbed
    CodestralEmbed
    GeminiEmbedding1
    GoogleTextEmbedding5
    GoogleTextMultilingualEmbedding2
    JinaEmbeddings3
    JinaEmbeddings4
    JinaEmbeddings5TextSmall
    JinaEmbeddings5TextNano
    JinaEmbeddings5OmniSmall
    JinaEmbeddings5OmniNano
    Voyage35
    Voyage35Lite
    Voyage4
    Voyage4Lite
    Voyage4Large
    VoyageCode3
    // Not directly usable — select custom models (see CustomModels) by their registered name string.
    Custom
  static class EmbeddingModelExtensions
    static string DisplayName(this EmbeddingModel model)
  enum EmbeddingType
    Generic
    Document
    Query
    Clustering
    Classification
  interface IEmbeddingGenerator : IDisposable, IEmbeddingGeneratorInfo
    // Returns one vector per input, in input order.
    Task<List<float[]>> GenerateEmbeddingsAsync(EmbeddingGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IEmbeddingGeneratorInfo
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
  class NonRetryableEmbeddingGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  static class VectorMath
    // embeddings: List of embeddings (each as a float array)
    static float[] CalculateAverageEmbedding(IList<float[]> embeddings)
    // throws ArgumentException: The vectors differ in length, or either vector has zero magnitude (e.g. a blank or failed embedding), for which cosine similarity is undefined. Guard degenerate vectors before calling when scoring in a loop.
    static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateDotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateEuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Neighbors are ranked by Euclidean distance.
    // embeddings: List of embeddings (each as a float array)
    // k: Number of neighbors to retrieve for each embedding
    static List<List<VectorMath.Neighbor>> CalculateKNearestNeighbors(IList<float[]> embeddings, int k)
    static float GetMagnitude(ReadOnlySpan<float> vector)
  readonly struct VectorMath.Neighbor
    ctor(int index, float distance)
    float Distance { get; }
    int Index { get; }

namespace Ikon.AI.FileConversion
  // Kind tells how the file was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record ConvertedFile : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    string? Url { get; init; }
  sealed class FileConverter : IFileConverter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(FileConverterModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
    Task<ConvertedFile> ConvertToPdfAsync(byte[] data, string fileName, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a FileConverter per call. fileName must carry the source extension (e.g. report.docx) — it determines the input format. The PDF is in result.Data. Use the constructor + ConvertToPdfAsync for a URL or AssetUri source, or a custom timeout.
    static Task<ConvertedFile> ConvertToPdfAsync(byte[] data, string fileName, FileConverterModel model = ConvertApi, CancellationToken cancellationToken = default)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(FileConverterModel model)
  // Supply the file exactly one way: Data, Url, or AssetUri (resolved to a URL automatically). FileName must carry the source extension (e.g. report.docx) — it determines the input format.
  sealed record FileConverterConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string FileName { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    TimeSpan Timeout { get; init; }
    string? Url { get; init; }
  class FileConverterException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum FileConverterModel
    ConvertApi
  static class FileConverterModelExtensions
    static string DisplayName(this FileConverterModel model)
  interface IFileConverter : IDisposable
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
  class NonRetryableFileConverterException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.ImageGeneration
  interface IImageGenerator : IDisposable, IImageGeneratorInfo
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IImageGeneratorInfo
    // True when the model accepts reference input images (image-to-image / editing).
    bool SupportsInputImage { get; }
    // True when an InputImageType.Mask gets dedicated inpainting handling rather than being treated as a plain reference image.
    bool SupportsMask { get; }
    // True when the model can return more than one image from a single request (ImageGeneratorConfig.Count > 1).
    bool SupportsMultipleOutputs { get; }
    // True when the model honours ImageGeneratorConfig.NegativePrompt.
    bool SupportsNegativePrompt { get; }
    // True when the model can produce output with a transparent background (ImageGeneratorConfig.Background = ImageBackground.Transparent). Check this first: only the OpenAI implementation rejects an unsupported request, and the others ignore ImageGeneratorConfig.Background outright, so the image comes back opaque with no error.
    bool SupportsTransparentBackground { get; }
  enum ImageBackground
    Auto
    Opaque
    Transparent
  sealed class ImageGenerator : IImageGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsInputImage { get; }
    bool SupportsMask { get; }
    bool SupportsMultipleOutputs { get; }
    bool SupportsNegativePrompt { get; }
    bool SupportsTransparentBackground { get; }
    void Dispose()
    Task<ImageGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an ImageGenerator per call. Defaults to ImageGeneratorModel.Gemini25FlashImage (cheap+fast); override via model. Never returns null — throws ImageGeneratorException on failure or empty output, so wrap in try/catch to continue without the image. Use the constructor + GenerateImageAsync for batch/size/input-image or any other ImageGeneratorConfig field.
    static Task<ImageGeneratorResult> GenerateAsync(string prompt, ImageGeneratorModel model = Gemini25FlashImage, CancellationToken cancellationToken = default)
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = default)
    static ImageGeneratorCapabilities GetCapabilities(ImageGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageGeneratorModel model)
  sealed class ImageGeneratorCapabilities : IImageGeneratorInfo
    ctor()
    bool SupportsInputImage { get; init; }
    bool SupportsMask { get; init; }
    bool SupportsMultipleOutputs { get; init; }
    bool SupportsNegativePrompt { get; init; }
    bool SupportsTransparentBackground { get; init; }
  sealed record ImageGeneratorConfig
    ctor()
    ImageBackground Background { get; init; }
    int Count { get; init; }
    // Requested pixel height; see Width for how tiered providers treat it.
    int Height { get; init; }
    List<InputImage> InputImages { get; init; }
    // Embed Ikon's imperceptible provenance watermark in the result pixels (EU AI Act Article 50 machine-readable marking, uniform across providers). The XMP metadata mark is always written regardless of this flag; disabling this skips the pixel pass — and, for JPEG results, the one high-quality re-encode it costs.
    bool InvisibleWatermark { get; init; }
    string NegativePrompt { get; init; }
    string Prompt { get; init; }
    ImageQuality Quality { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    SafetyLevel SafetyLevel { get; init; }
    string SearchPrompt { get; init; }
    int Seed { get; init; }
    int Steps { get; init; }
    string Style { get; init; }
    TimeSpan Timeout { get; init; }
    bool UpsamplePrompt { get; init; }
    // Renders a small corner badge with this text on the result (e.g. "AI"). Empty = no visible mark. Intended as a plan-tier lever, not a compliance requirement — the machine-readable marks above are what Article 50 asks for.
    string VisibleWatermark { get; init; }
    // The only way to request a size. Providers with fixed resolution tiers (e.g. Gemini 1K/2K/4K) round the longer edge up to the nearest tier and take the aspect ratio from Width:Height — ask for 2048x2048 to get a 2K image.
    int Width { get; init; }
  class ImageGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum ImageGeneratorModel
    GptImage1Mini
    GptImage15
    GptImage2
    Gemini25FlashImage
    Gemini3ProImage
    Gemini31FlashImage
    Gemini31FlashLiteImage
    Flux1Dev
    Flux11Pro
    Flux11ProUltra
    Flux11ProUltraRaw
    Flux1Fill
    Flux1KontextPro
    Flux1KontextMax
    Flux2Dev
    Flux2Flex
    Flux2Pro
    Flux2Max
    Flux2Klein9B
    Flux2Klein4B
    GrokImagineImage
    GrokImagineImageQuality
  static class ImageGeneratorModelExtensions
    static string DisplayName(this ImageGeneratorModel model)
  // Kind tells how the image was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record ImageGeneratorResult : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    int Height { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
    int Width { get; init; }
  enum ImageQuality
    Auto
    Low
    Medium
    High
  class NonRetryableImageGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // Provider-mapped moderation strength; Moderate is the default.
  enum SafetyLevel
    None
    Minimal
    Low
    Moderate
    High
    VeryHigh
    Maximum

namespace Ikon.AI.ImageSegmentation
  interface IImageSegmenter : IDisposable
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed class ImageSegmenter : IImageSegmenter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageSegmenterModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageSegmenterModel model)
    Task<ImageSegmenterResult> SegmentAsync(byte[] imageData, string mimeType, string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an ImageSegmenter per call. Defaults to ImageSegmenterModel.Sam31; override via model. Each detected object is in result.Segments with its mask image, score, and bounding box. Use the constructor + SegmentImageAsync for a URL source, point/box prompts, multiple masks per object, or other fields.
    static Task<ImageSegmenterResult> SegmentAsync(byte[] imageData, string mimeType, string prompt, ImageSegmenterModel model = Sam31, CancellationToken cancellationToken = default)
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed record ImageSegmenterConfig
    ctor()
    List<ImageSegmenterConfig.BoxPrompt> BoxPrompts { get; init; }
    InputImage InputImage { get; init; }
    int MaxMasks { get; init; }
    List<ImageSegmenterConfig.PointPrompt> PointPrompts { get; init; }
    string? Prompt { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    bool ReturnMultipleMasks { get; init; }
    TimeSpan Timeout { get; init; }
  sealed record ImageSegmenterConfig.BoxPrompt
    ctor()
    int? ObjectId { get; init; }
    double XMax { get; init; }
    double XMin { get; init; }
    double YMax { get; init; }
    double YMin { get; init; }
  sealed record ImageSegmenterConfig.PointPrompt
    ctor()
    bool IsBackground { get; init; }
    int? ObjectId { get; init; }
    double X { get; init; }
    double Y { get; init; }
  class ImageSegmenterException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum ImageSegmenterModel
    Sam3
    Sam31
  static class ImageSegmenterModelExtensions
    static string DisplayName(this ImageSegmenterModel model)
  sealed record ImageSegmenterResult
    ctor()
    OutputImage? Preview { get; init; }
    List<ImageSegmenterResult.Segment> Segments { get; init; }
  sealed record ImageSegmenterResult.Segment
    ctor()
    List<double> Box { get; init; }
    OutputImage Mask { get; init; }
    double? Score { get; init; }
  class NonRetryableImageSegmenterException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.ImageUpscaling
  interface IImageUpscaler : IDisposable, IImageUpscalerInfo
    Task<ImageUpscalerResult> UpscaleImageAsync(ImageUpscalerConfig config, CancellationToken cancellationToken = default)
  interface IImageUpscalerInfo
    // Whether the model invents detail; see UpscaleFidelity.
    UpscaleFidelity Fidelity { get; }
    // Largest output this model will produce, or 0 when it is uncapped. A request whose input size and scale factor would exceed it is refused before the provider is called, so a model priced in steps of output size can never be charged at a step above the one we bill. Only checked when the input is supplied as bytes — a URL's size is not known up front.
    double MaxOutputMegapixels { get; }
    // The largest ImageUpscalerConfig.ScaleFactor the provider accepts, or 0 when SupportsScaleFactor is false. A high ceiling is what the API allows, not a promise the provider will render it — the output size limit still applies.
    double MaxScaleFactor { get; }
    // True when the model honours ImageUpscalerConfig.Creativity. False on every UpscaleFidelity.Faithful model.
    bool SupportsCreativity { get; }
    // True when the model honours ImageUpscalerConfig.EnhanceFaces.
    bool SupportsFaceEnhancement { get; }
    // True when the model honours ImageUpscalerConfig.OutputFormat; on the rest the provider's own encoding is returned.
    bool SupportsOutputFormat { get; }
    // True when the model honours ImageUpscalerConfig.ScaleFactor. Models with a single built-in step size report false.
    bool SupportsScaleFactor { get; }
    // True when the model honours ImageUpscalerConfig.TargetResolution.
    bool SupportsTargetResolution { get; }
  sealed class ImageUpscaler : IImageUpscaler
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageUpscalerModel model, IReadOnlyList<ModelRegion>? regions = null)
    UpscaleFidelity Fidelity { get; }
    double MaxOutputMegapixels { get; }
    double MaxScaleFactor { get; }
    bool SupportsCreativity { get; }
    bool SupportsFaceEnhancement { get; }
    bool SupportsOutputFormat { get; }
    bool SupportsScaleFactor { get; }
    bool SupportsTargetResolution { get; }
    void Dispose()
    // Read ImageUpscalerCapabilities.Fidelity before picking a model when it matters whether the result may contain detail the input never had.
    static ImageUpscalerCapabilities GetCapabilities(ImageUpscalerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageUpscalerModel model)
    Task<ImageUpscalerResult> UpscaleAsync(byte[] imageData, string mimeType, double scaleFactor = 0.0, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an ImageUpscaler per call. Defaults to ImageUpscalerModel.SeedVr2, which reconstructs detail faithfully and bills per output megapixel. scaleFactor of 0 leaves the model's own default in place. Every default model is UpscaleFidelity.Faithful — reach for ImageUpscalerModel.Crystal and ImageUpscalerConfig.Creativity to let a model invent detail. The upscaled image is in result.Image (.Data/.MimeType). Use the constructor + UpscaleImageAsync for a URL source or any other config field.
    static Task<ImageUpscalerResult> UpscaleAsync(byte[] imageData, string mimeType, ImageUpscalerModel model = SeedVr2, double scaleFactor = 0.0, CancellationToken cancellationToken = default)
    Task<ImageUpscalerResult> UpscaleImageAsync(ImageUpscalerConfig config, CancellationToken cancellationToken = default)
  sealed class ImageUpscalerCapabilities : IImageUpscalerInfo
    ctor()
    UpscaleFidelity Fidelity { get; init; }
    double MaxOutputMegapixels { get; init; }
    double MaxScaleFactor { get; init; }
    bool SupportsCreativity { get; init; }
    bool SupportsFaceEnhancement { get; init; }
    bool SupportsOutputFormat { get; init; }
    bool SupportsScaleFactor { get; init; }
    bool SupportsTargetResolution { get; init; }
  sealed record ImageUpscalerConfig
    ctor()
    // 0 keeps the model as close to the input as it can get; 1 lets it invent detail freely. Only models reporting IImageUpscalerInfo.SupportsCreativity accept a non-zero value — on the rest it throws, so a faithful model can never quietly start hallucinating.
    double Creativity { get; init; }
    // Restore faces beyond what the rest of the frame gets. This invents detail even on an otherwise faithful model, so it is off unless asked for.
    bool EnhanceFaces { get; init; }
    InputImage InputImage { get; init; }
    bool InvisibleWatermark { get; init; }
    // Defaults to UpscaleOutputFormat.Png: re-encoding a freshly recovered image as JPEG throws away detail that was just paid for.
    UpscaleOutputFormat OutputFormat { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    // Linear multiplier applied to both edges; 0 leaves the model's own default in place. Requesting a factor from a model that does not expose one, or one above the model's IImageUpscalerInfo.MaxScaleFactor, throws rather than being clamped.
    double ScaleFactor { get; init; }
    // Upscale towards a fixed resolution instead of by a factor. Mutually exclusive with ScaleFactor.
    UpscaleTargetResolution TargetResolution { get; init; }
    TimeSpan Timeout { get; init; }
    string VisibleWatermark { get; init; }
  class ImageUpscalerException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum ImageUpscalerModel
    SeedVr2
    Topaz
    RecraftCrisp
    Crystal
  static class ImageUpscalerModelExtensions
    static string DisplayName(this ImageUpscalerModel model)
  sealed record ImageUpscalerResult
    ctor()
    OutputImage Image { get; init; }
  class NonRetryableImageUpscalerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // The distinction is the whole point of picking one upscaler over another. Faithful models reconstruct only what the input supports, so the result can still be read as evidence of the original. Creative models synthesize plausible detail that was never in the input. Tunable models move between the two as ImageUpscalerConfig.Creativity rises, and sit at the faithful end when it is left at zero.
  enum UpscaleFidelity
    Faithful
    Tunable
    Creative
  enum UpscaleOutputFormat
    Png
    Jpeg
  // The longer edge is driven to the named height and the aspect ratio is preserved. Only models whose capabilities report IImageUpscalerInfo.SupportsTargetResolution accept this.
  enum UpscaleTargetResolution
    None
    Hd720
    Fhd1080
    Qhd1440
    Uhd2160

namespace Ikon.AI.Kernel
  static class AsyncEnumerableExtensions
    static Task<T1[]> AsArrayAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<T1> AsFirstAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<string> AsStringAsync(this IAsyncEnumerable<LLMEvent> source)
    static IAsyncEnumerable<LLMEvent> WithParsedTagsAsync(this IAsyncEnumerable<LLMEvent> source, List<string>? tagWhitelist = null, List<string>? tagBlacklist = null)
    static IAsyncEnumerable<LLMEvent> WithReasoningFromTagAsync(this IAsyncEnumerable<LLMEvent> source, string reasoningTagName)
    static IAsyncEnumerable<LLMEvent> WithThrottlingAsync(this IAsyncEnumerable<LLMEvent> source, int charsPerSecond, int charsPerUpdate, CancellationToken cancellationToken = default)
    static IAsyncEnumerable<LLMEvent> WithWindowedProcessingAsync(this IAsyncEnumerable<LLMEvent> source, Func<string, List<LLMEvent>, Task<(bool, List<LLMEvent>)>> processAsync, int windowSize = 0, int windowOverlap = 0)
  readonly struct AudioIdPart : IMessagePart
    ctor(string id)
    string Id { get; }
    MessagePartType Type { get; }
  readonly struct AudioPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  class BinaryDataContainer
    ctor(byte[] data, string mimeType)
    byte[] Data { get; }
    string MimeType { get; }
  class FunctionCall
    ctor(Function function, object?[] parameters, string parametersJson, string callId, string hash, string thoughtSignature = "", string reasoningContent = "")
    string CallId { get; }
    Function Function { get; }
    string Hash { get; }
    object?[] Parameters { get; }
    string ParametersJson { get; }
    string ReasoningContent { get; }
    string ThoughtSignature { get; }
  // Only providers that support media in tool results inline the media for the model to see; all other consumers fall back to ToString, which summarizes the media without emitting the bytes.
  sealed class FunctionMediaResult
    ctor(string text, params BinaryDataContainer[] media)
    IReadOnlyList<BinaryDataContainer> Media { get; }
    string Text { get; }
    override string ToString()
  class FunctionResult
    ctor(object? result = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null)
    string? ModelMessagePrefix { get; set; }
    string? ModelMessageSuffix { get; set; }
    object? Result { get; set; }
  readonly struct FunctionResultPart : IMessagePart
    ctor(FunctionCall functionCall, LLMEvent[] events, object result)
    LLMEvent[] Events { get; }
    FunctionCall FunctionCall { get; }
    object Result { get; }
    MessagePartType Type { get; }
  interface IMessagePart
    MessagePartType Type { get; }
  readonly struct ImagePart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  readonly struct ImageUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  readonly struct Instruction
    ctor(InstructionType type, string content)
    string Content { get; }
    InstructionType Type { get; }
  enum InstructionType
    Context
    Command
  readonly record struct KernelContext
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    // Threshold is measured in input tokens; clearing runs after prompt-cache lookup so cached prefixes survive. Null disables it. Only providers with server-side context editing (Anthropic context-management) act on it — others ignore it.
    int? ClearToolResultsAfterInputTokens { get; init; }
    // Tool names exempt from ClearToolResultsAfterInputTokens clearing — use for results that stay semantically load-bearing all run (verdicts, anchors).
    IReadOnlyList<string>? ClearToolResultsExcludedTools { get; init; }
    bool DisableFunctionCalling { get; init; }
    // When true (the DEFAULT — set in the constructor), any assistant text the model emits on the same turn as a tool call is DROPPED — only the tool call flows on. Set false to keep that interleaved text (e.g. a model that narrates before calling a tool). A direct Kernel/LLM consumer who does not set this loses same-turn text with no signal.
    bool DiscardTextOutputWithFunctionCalls { get; init; }
    // Prefer this or new KernelContext() over default(KernelContext): default leaves the collections unset, though the mutation helpers tolerate it.
    static KernelContext Empty { get; }
    ImmutableDictionary<string, Function> Functions { get; init; }
    string GbnfGrammar { get; init; }
    ImmutableList<Instruction> Instructions { get; init; }
    object? JsonSchema { get; init; }
    bool LogFullRequest { get; init; }
    bool LogFullResponse { get; init; }
    int MaxOutputTokens { get; init; }
    ImmutableList<MessageBlock> Messages { get; init; }
    ReasoningEffort ReasoningEffort { get; init; }
    int ReasoningTokenBudget { get; init; }
    // Travels with the context over RPC, so the process that actually talks to the provider honours it — which is the only way a remote generation can report progress at all.
    bool StreamProgress { get; init; }
    double Temperature { get; init; }
    TimeSpan Timeout { get; init; }
    string ToolPlan { get; init; }
    bool UseAudioOutput { get; init; }
    bool UseCaching { get; init; }
    bool UseCitations { get; init; }
    bool UseJson { get; init; }
    bool UseStreaming { get; init; }
    bool UseUserNames { get; init; }
    KernelContext Add(Instruction instruction)
    KernelContext Add(MessageBlock message)
    // Runs a single model turn. A tool call whose function has LlmInlineResult set is executed here and its events replace the call in the stream — recursively, so a function that itself emits tool calls is handled the same way. Every other tool call is yielded for the caller to run. The model is invoked once and never sees the results, so this is not an agent loop: a Shader pass is what feeds results back as FunctionResult messages and re-runs the model. Use ILLM.GenerateAsync directly for the raw provider stream that never runs a tool.
    IAsyncEnumerable<LLMEvent> GenerateAsync(ILLM llm, CancellationToken cancellationToken = default)
    KernelContext KeepMessagesMax(int count)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
  // Consume by switching on the concrete record case; forward any case you do not handle unchanged so downstream consumers still receive it.
  abstract record LLMEvent
    // E.g. "generate", "generate.reasoning", "Shader.Output.AfterPass". Combinators re-tag events they transform so the origin of each event stays visible.
    string Source { get; init; }
  sealed record LLMEvent.AudioDelta : LLMEvent
    ctor(AudioChunk Audio)
    AudioChunk Audio { get; init; }
  // Replayable as an AudioIdPart in a follow-up context.
  sealed record LLMEvent.AudioId : LLMEvent
    ctor(string Id)
    string Id { get; init; }
  sealed record LLMEvent.AudioTranscript : LLMEvent
    ctor(string Transcript)
    string Transcript { get; init; }
  // ReferStartIndex/ReferEndIndex bound the citing text span; PositionIndex is the character index of the citation marker itself.
  sealed record LLMEvent.Citation : LLMEvent
    ctor(string OriginalId, string MappedId, int ReferStartIndex, int ReferEndIndex, int PositionIndex)
    string MappedId { get; init; }
    string OriginalId { get; init; }
    int PositionIndex { get; init; }
    int ReferEndIndex { get; init; }
    int ReferStartIndex { get; init; }
  sealed record LLMEvent.ContentFiltered : LLMEvent
    ctor(ClassificationResult Classification)
    ClassificationResult Classification { get; init; }
  // Emitted once at the end of a shader run; may differ from the text response.
  sealed record LLMEvent.FinalModelMessage : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // Emitted once at the end of a shader run.
  sealed record LLMEvent.FinalText : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // Reason is the provider's finish reason, e.g. "stop", "max_tokens".
  sealed record LLMEvent.Finished : LLMEvent
    ctor(string Reason)
    string Reason { get; init; }
  // Opt-in via KernelContext.StreamProgress — off by default, because it changes the event stream every consumer sees. Nothing else can answer "is the model working right now" over RPC: usage is reported once a turn has ended, Reasoning and tool arguments are only emitted after the stream drains, and text may be suppressed entirely on a tool-calling turn. Carries the SIZE and not the content — the content still arrives in its own event.
  sealed record LLMEvent.GenerationProgress : LLMEvent
    ctor(LlmStreamKind Kind, int Characters)
    int Characters { get; init; }
    LlmStreamKind Kind { get; init; }
  sealed record LLMEvent.Reasoning : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // Extracted from the text stream by AsyncEnumerableExtensions.WithParsedTagsAsync.
  sealed record LLMEvent.Tag : LLMEvent
    ctor(string Name, string Content, IReadOnlyDictionary<string, string>? Attributes)
    IReadOnlyDictionary<string, string>? Attributes { get; init; }
    string Content { get; init; }
    string Name { get; init; }
  sealed record LLMEvent.TextDelta : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  sealed record LLMEvent.ToolCallRequested : LLMEvent
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  // Emitted by Cohere models only.
  sealed record LLMEvent.ToolPlan : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // ValueType is the value's runtime type name, used to rehydrate Value to its original type after a JSON round-trip (e.g. over RPC).
  sealed record LLMEvent.ToolResult : LLMEvent
    ctor(string functionName, object? value)
    ctor(string functionName, object? value, string? valueType)
    string FunctionName { get; }
    object? Value { get; }
    string? ValueType { get; }
  // The buckets are disjoint: total input = InputTokens + CachedInputTokens + CacheCreationInputTokens. A fully cached prompt reports InputTokens=0 with all input in CachedInputTokens.
  sealed record LLMEvent.Usage : LLMEvent
    ctor(int InputTokens, int CachedInputTokens, int CacheCreationInputTokens, int OutputTokens)
    int CacheCreationInputTokens { get; init; }
    int CachedInputTokens { get; init; }
    int InputTokens { get; init; }
    int OutputTokens { get; init; }
  enum MediaResolution
    Default
    Low
    Medium
    High
    UltraHigh
  readonly struct MessageBlock
    ctor(MessageBlockRole role, IMessagePart[] parts, string? userName = null)
    ctor(MessageBlockRole role, IEnumerable<IMessagePart> parts, string? userName = null)
    ctor(MessageBlockRole role, string message, string? userName = null)
    IMessagePart[] Parts { get; }
    MessageBlockRole Role { get; }
    string? UserName { get; }
    // Each input must be a string or a BinaryDataContainer whose MIME type is an image, audio, video, or PDF; any other input type or MIME type is rejected rather than silently dropped. Returns null only when inputs is empty.
    static MessageBlock? CreateFromObjects(IReadOnlyList<object?> inputs, MessageBlockRole role)
    override string ToString()
  enum MessageBlockRole
    User
    Model
    FunctionResult
  enum MessagePartType
    Text
    Image
    ImageUrl
    Audio
    AudioId
    Video
    VideoUrl
    VideoAsset
    Pdf
    PdfUrl
    FunctionResult
  readonly struct PdfPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  readonly struct PdfUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  enum ReasoningEffort
    None
    Minimal
    Low
    Medium
    High
  // All Ikon-side schema shapes (primitives, arrays, dictionaries, polymorphism) are expressible in both dialects; the two differ in how they encode nullability and how strictly they police unknown keywords.
  enum SchemaDialect
    // Nullable primitives expand their "type" into a ["X", "null"] union. Accepted by OpenAI strict structured outputs and Anthropic tool-use schemas.
    JsonSchema202012
    // "type" is always a single string and nullability is carried on a separate "nullable": true flag. Accepted by Google's Gemini response_schema validator, which rejects the 2020-12 union-type form outright.
    OpenApi30
  readonly struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  readonly struct VideoAssetPart : IMessagePart
    ctor(AssetUri uri, string? mimeType = null, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string? MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    AssetUri Uri { get; }
  readonly struct VideoPart : IMessagePart
    ctor(byte[] content, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    byte[] Content { get; }
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
  readonly struct VideoUrlPart : IMessagePart
    ctor(string url, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    string Url { get; }

namespace Ikon.AI.LLM
  enum CustomLLMApi
    OpenAICompletions
    OpenAIResponses
    Anthropic
    Google
    Cohere
  // Capability flags default to what a typical self-hosted OpenAI-compatible model supports; enable more (e.g. SupportsJsonSchema) when the endpoint provides them.
  sealed class CustomLLMModel : CustomModel
    ctor()
    required CustomLLMApi Api { get; init; }
    required int ContextWindowSize { get; init; }
    // Leave at 0 when the endpoint has no such cap: a request asking for more than the model can produce is capped at this value instead of being sent as-is, and 0 means "send the caller's value".
    int MaxOutputTokens { get; init; }
    bool SupportsCaching { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsSingleToolCalling { get; init; }
    bool SupportsStreaming { get; init; }
    bool SupportsStrictJsonSchema { get; init; }
    bool SupportsSystemMessages { get; init; }
    bool SupportsTemperature { get; init; }
  // Returns the exact JSON schema each provider ships to the model for a Function; use it rather than re-deriving your own projection.
  static class FunctionSchema
    static string ToJson(Function function)
  interface ILLM : IDisposable, ILLMInfo
    IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext context, CancellationToken cancellationToken = default)
  interface ILLMInfo
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
    // In tokens. 0 means "no published limit", not "no output allowed".
    int MaxOutputTokens { get; }
    SchemaDialect SchemaDialect { get; }
    bool SupportsGbnfGrammar { get; }
    bool SupportsInputAudio { get; }
    bool SupportsInputImages { get; }
    bool SupportsInputPdf { get; }
    bool SupportsInputVideo { get; }
    bool SupportsJsonSchema { get; }
    bool SupportsOutputAudio { get; }
    bool SupportsParallelToolCalling { get; }
    bool SupportsReasoning { get; }
    bool SupportsSingleToolCalling { get; }
    bool SupportsStreaming { get; }
    bool SupportsZeroDataRetention { get; }
    bool UsesInlineReasoning { get; }
  sealed class LLMCapabilities : ILLMInfo
    ctor()
    int ContextWindowSize { get; init; }
    string InlineReasoningTagName { get; init; }
    int MaxOutputTokens { get; init; }
    SchemaDialect SchemaDialect { get; init; }
    bool SupportsGbnfGrammar { get; init; }
    // Distinct from SupportsInputImages: a vision model whose tool results are JSON-only (e.g. Gemini functionResponse) accepts images in messages but not inside tool_result blocks.
    bool SupportsImagesInToolResults { get; init; }
    bool SupportsInputAudio { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsInputPdf { get; init; }
    bool SupportsInputVideo { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsOutputAudio { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsSingleToolCalling { get; init; }
    bool SupportsStreaming { get; init; }
    bool SupportsZeroDataRetention { get; init; }
    bool UsesInlineReasoning { get; init; }
  class LLMMaxOutputTokensException : NonRetryableLLMException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum LLMModel
    Gpt4OmniMini
    Gpt41
    Gpt41Mini
    Gpt5
    Gpt5Mini
    Gpt5Nano
    Gpt51
    Gpt52
    Gpt5Pro
    Gpt52Pro
    Gpt53Codex
    Gpt54
    Gpt54Mini
    Gpt54Nano
    Gpt54Pro
    Gpt55
    Gpt55Pro
    Gpt56Sol
    Gpt56Terra
    Gpt56Luna
    O3
    O3Pro
    Claude45Haiku
    Claude45Sonnet
    Claude45Opus
    Claude46Opus
    Claude46Sonnet
    Claude47Opus
    Claude48Opus
    Claude5Sonnet
    Claude5Opus
    Claude5Fable
    Gemini25Flash
    Gemini25FlashLite
    Gemini25Pro
    Gemini3Flash
    Gemini31Pro
    Gemini31FlashLite
    Gemini35Flash
    Gemini35FlashLite
    Gemini36Flash
    Gemini37Flash
    Grok43
    Grok45
    GrokBuild01
    Grok420Reasoning
    Grok420NonReasoning
    MistralSmall
    MistralMedium
    MistralLarge
    Ministral14B
    Ministral8B
    Ministral3B
    MagistralSmall
    MagistralMedium
    Codestral
    Devstral2
    VoxtralSmall
    CommandR
    CommandRPlus
    CommandA
    CommandAReasoning
    CommandAPlus
    CommandAVision
    CommandR7B
    KimiK25
    KimiK26
    KimiK27Code
    KimiK3
    Qwen36
    Qwen37
    Qwen37Max
    Qwen38Max
    Qwen37Flash
    Qwen3827B
    GptOss120B
    Glm5
    Glm51
    Glm52
    Glm53
    Glm5VTurbo
    MiniMaxM25
    MiniMaxM27
    MiniMaxM3
    DeepSeekV32
    DeepSeekV4Pro
    DeepSeekV4Flash
    DeepSeekV4FlashVision
    Seed21Turbo
    Seed20Code
    Seed20Lite
    Seed20Mini
    MiMoV25
    MiMoV25Pro
    Step37Flash
    NovaPro
    NovaLite
    NovaMicro
    Nova2Lite
  static class LLMModelExtensions
    // In tokens. Returns 0 when the model can't be resolved — treat 0 as "unknown" and skip utilization math rather than dividing by zero.
    static int ContextWindowSize(this LLMModel model)
    static string DisplayName(this LLMModel model)
    // In tokens. Returns 0 when the limit is unknown (unresolvable model, or a provider that publishes none) — treat 0 as "no cap known", not as a zero budget.
    static int MaxOutputTokens(this LLMModel model)
  class ModelOutputException : RetryableLLMException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class NonRetryableLLMException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class RetryableLLMException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.MeshGeneration
  interface IMeshGenerator : IDisposable, IMeshGeneratorInfo
    Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IMeshGeneratorInfo
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
  sealed class MeshGenerator : IMeshGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MeshGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
    void Dispose()
    Task<MeshGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a MeshGenerator per call. Defaults to MeshGeneratorModel.Meshy6; override via model. Returns signed per-format download URLs (.GlbUrl, .FbxUrl, …) that expire roughly three days after generation — download promptly. Use the constructor + GenerateMeshAsync for image-to-mesh, PBR textures, or topology control.
    static Task<MeshGeneratorResult> GenerateAsync(string prompt, MeshGeneratorModel model = Meshy6, CancellationToken cancellationToken = default)
    Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = default)
    static MeshGeneratorCapabilities GetCapabilities(MeshGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(MeshGeneratorModel model)
  sealed class MeshGeneratorCapabilities : IMeshGeneratorInfo
    ctor()
    int MaxInputImages { get; init; }
    bool SupportsImageToMesh { get; init; }
    bool SupportsLowPoly { get; init; }
    bool SupportsPbr { get; init; }
    bool SupportsTextToMesh { get; init; }
  sealed record MeshGeneratorConfig
    ctor()
    bool EnablePbr { get; init; }
    List<InputImage> InputImages { get; init; }
    MeshGeneratorMeshStyle MeshStyle { get; init; }
    string? Prompt { get; init; }
    bool Remesh { get; init; }
    int TargetPolycount { get; init; }
    bool Texture { get; init; }
    string? TexturePrompt { get; init; }
    TimeSpan Timeout { get; init; }
    MeshGeneratorTopology Topology { get; init; }
  class MeshGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum MeshGeneratorMeshStyle
    Standard
    LowPoly
  enum MeshGeneratorModel
    Meshy5
    Meshy6
  static class MeshGeneratorModelExtensions
    static string DisplayName(this MeshGeneratorModel model)
  // The download URLs are signed and expire roughly three days after generation — fetch the model files promptly.
  sealed record MeshGeneratorResult
    ctor()
    DateTimeOffset? ExpiresAt { get; init; }
    string? FbxUrl { get; init; }
    string? GlbUrl { get; init; }
    string? MtlUrl { get; init; }
    string? ObjUrl { get; init; }
    string? ThumbnailUrl { get; init; }
    string? UsdzUrl { get; init; }
  enum MeshGeneratorTopology
    Triangle
    Quad
  class NonRetryableMeshGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.MusicGeneration
  interface IMusicGenerator : IDisposable, IMusicGeneratorInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    // Requires IMusicGeneratorInfo.SupportsStreaming; otherwise throws NonRetryableMusicGeneratorException. Use GenerateMusicFileAsync for a buffered encoded file.
    IAsyncEnumerable<AudioChunk> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IMusicGeneratorInfo
    // When false the model ignores MusicGeneratorConfig.DurationSeconds, emitting a fixed-length clip or (when editing) matching the input clip's length.
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    // When false, IMusicGenerator.GenerateMusicAsync throws; use the buffered IMusicGenerator.GenerateMusicFileAsync instead.
    bool SupportsStreaming { get; }
  sealed class MusicGenerator : IMusicGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MusicGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    bool SupportsStreaming { get; }
    void Dispose()
    Task<MusicGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a MusicGenerator per call. Defaults to MusicGeneratorModel.ElevenLabsMusicV2 (supports duration control and editing); override via model. Returns a buffered, encoded audio file (.Data/.MimeType/.DurationSeconds). Use the constructor + GenerateMusicFileAsync for duration/input-audio/seed, or GenerateMusicAsync for streaming PCM chunks.
    static Task<MusicGeneratorResult> GenerateAsync(string prompt, MusicGeneratorModel model = ElevenLabsMusicV2, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    static MusicGeneratorCapabilities GetCapabilities(MusicGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(MusicGeneratorModel model)
  sealed class MusicGeneratorCapabilities : IMusicGeneratorInfo
    ctor()
    bool SupportsDurationControl { get; init; }
    bool SupportsEditing { get; init; }
    bool SupportsStreaming { get; init; }
  // With an empty InputAudios the model generates from the prompt alone; with one or more it performs audio-to-audio editing (the prompt re-styles the clips, timing preserved). The underlying music model works on clips of at least 3 seconds. For shorter UI/game sound effects use SoundEffectGenerator instead.
  sealed record MusicGeneratorConfig
    ctor()
    // Seconds, clamped to the model's supported range. When editing, set it to the source clip's length to keep the original timing. Ignored unless IMusicGeneratorInfo.SupportsDurationControl is true.
    double? DurationSeconds { get; init; }
    bool ForceInstrumental { get; init; }
    List<InputAudio> InputAudios { get; init; }
    string Prompt { get; init; }
    // Applies to the buffered IMusicGenerator.GenerateMusicFileAsync result; the streaming IMusicGenerator.GenerateMusicAsync chunks are unaffected.
    ResultDelivery ResultDelivery { get; init; }
    int Seed { get; init; }
    TimeSpan Timeout { get; init; }
  class MusicGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum MusicGeneratorModel
    ElevenLabsMusicV2
    FalStableAudio
    FalLyria2
    // The platform provides the Suno key, so these behave like every other model here and need no per-app secret. An app may still override it with its own subscription by setting IKON_SUNO_API_KEY (ikon app secret set IKON_SUNO_API_KEY <key>), which is then billed as bring-your-own-key usage.
    SunoV5
    SunoV55
  static class MusicGeneratorModelExtensions
    static string DisplayName(this MusicGeneratorModel model)
  // Kind tells how the audio was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record MusicGeneratorResult : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    double DurationSeconds { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
  class NonRetryableMusicGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.OCR
  enum DocumentType
    General
  interface IOCR : IDisposable, IOCRInfo
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = default)
  interface IOCRInfo
    // Largest document the model accepts, in bytes, or 0 when it publishes no limit — never read 0 as a zero budget. Only checked when the document is supplied as OCRConfig.Data; the size behind a OCRConfig.Url or OCRConfig.AssetUri is not known before the request is made.
    long MaxDocumentSizeBytes { get; }
    // Most pages the model reads in one request, or 0 when it publishes no limit. A longer document has to be split into several requests with OCRConfig.Pages.
    int MaxPagesSupported { get; }
    // Mime types the provider documents as accepted input, or empty when it publishes no list. Advisory: a type outside the list is passed to the provider rather than refused here, because the provider is the authority on what it will read.
    IReadOnlyList<string> SupportedMimeTypes { get; }
    // True when the model fills OCRResult.Words for OCRConfig.IncludeWords. A request that asks a model reporting false for words is refused rather than answered with an empty list.
    bool SupportsWordLevelText { get; }
  class NonRetryableOCRException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class OCR : IOCR
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion>? regions = null)
    long MaxDocumentSizeBytes { get; }
    int MaxPagesSupported { get; }
    IReadOnlyList<string> SupportedMimeTypes { get; }
    bool SupportsWordLevelText { get; }
    Task<OCRResult> AnalyzeAsync(byte[] data, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an OCR per call. Accepts image or PDF bytes. Defaults to OCRModel.AzureDocumentIntelligence; override via model. Extracted text is in result.Text; result.Paragraphs/result.Pages carry structure. Use the constructor + AnalyzeDocumentAsync for a URL/AssetUri source or other fields, or AnalyzeDocumentStreamingAsync for page-by-page streaming.
    static Task<OCRResult> AnalyzeAsync(byte[] data, OCRModel model = AzureDocumentIntelligence, CancellationToken cancellationToken = default)
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    static OCRCapabilities GetCapabilities(OCRModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(OCRModel model)
  sealed record OCRBoundingBox
    ctor()
    int PageNumber { get; init; }
    List<float> Polygon { get; init; }
  sealed class OCRCapabilities : IOCRInfo
    ctor()
    long MaxDocumentSizeBytes { get; init; }
    int MaxPagesSupported { get; init; }
    IReadOnlyList<string> SupportedMimeTypes { get; init; }
    bool SupportsWordLevelText { get; init; }
  // Supply the document exactly one way: Data (with MimeType; detected from the bytes when unset), Url, or AssetUri (resolved to a URL automatically).
  sealed record OCRConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    DocumentType DocumentType { get; init; }
    bool IncludeWords { get; init; }
    string? MimeType { get; init; }
    string? Pages { get; init; }
    TimeSpan Timeout { get; init; }
    string? Url { get; init; }
  class OCRException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum OCRModel
    AzureDocumentIntelligence
    MistralOCR
  static class OCRModelExtensions
    static string DisplayName(this OCRModel model)
  sealed record OCRPage
    ctor()
    float Height { get; init; }
    int PageNumber { get; init; }
    string Unit { get; init; }
    float Width { get; init; }
  sealed record OCRParagraph
    ctor()
    List<OCRBoundingBox> BoundingRegions { get; init; }
    string Content { get; init; }
  sealed record OCRResult
    ctor()
    List<OCRPage> Pages { get; init; }
    List<OCRParagraph> Paragraphs { get; init; }
    string Text { get; init; }
    List<OCRWord> Words { get; init; }
  sealed record OCRWord
    ctor()
    OCRBoundingBox BoundingBox { get; init; }
    float Confidence { get; init; }
    string Content { get; init; }

namespace Ikon.AI.Provenance
  // The platform's EU AI Act Article 50 marking, applied identically for every provider. Three layers behind one call: an XMP metadata mark (always; IPTC DigitalSourceType=trainedAlgorithmicMedia), an imperceptible tiled pixel watermark (default on; detectable via MeasureInvisibleMark), and an optional visible corner badge. PNG and JPEG take all three; WebP takes the metadata mark alone; any other encoding passes through untouched. Ask GetMarkingSupport rather than assuming. Streamed media (WebRTC, TTS) is out of scope by design — disclosure there is interaction-level.
  static class ImageProvenance
    static byte[] Apply(byte[] data, string model, bool invisibleWatermark = true, string visibleWatermark = "")
    static ProvenanceMarking GetMarkingSupport(byte[] data)
    // At or above DetectionThreshold the image carries Ikon's mark; unmarked images score near zero.
    static double MeasureInvisibleMark(byte[] data)
    static string? ReadMetadataMark(byte[] data)
    // Scores are normal-deviates: an unmarked image scores |z| ≲ 3, a marked one scores in the tens to hundreds depending on size and recompression.
    const double DetectionThreshold = 12.0
  enum ProvenanceMarking
    None
    // Machine-readable and standards-compliant, but strippable by anything that rewrites the file's metadata.
    MetadataOnly
    // The pixel watermark survives a re-encode.
    Full

namespace Ikon.AI.Reranking
  enum CustomRerankApi
    Cohere
    Jina
    Voyage
    Together
  sealed class CustomRerankModel : CustomModel
    ctor()
    required CustomRerankApi Api { get; init; }
  interface IReranker : IDisposable
    // Returns items ordered most relevant first; RerankItem.Index is the document's position in RerankerConfig.Documents.
    Task<List<RerankItem>> RerankAsync(RerankerConfig config, CancellationToken cancellationToken = default)
  class NonRetryableRerankerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed record RerankItem
    ctor()
    int Index { get; init; }
    double Score { get; init; }
  enum RerankModel
    CohereRerank4Fast
    CohereRerank4Pro
    JinaReranker3
    VoyageRerank25
    VoyageRerank25Lite
    // Not directly usable — select custom models (see CustomModels) by their registered name string.
    Custom
  static class RerankModelExtensions
    static string DisplayName(this RerankModel model)
  sealed class Reranker : IReranker
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(RerankModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(RerankModel model)
    Task<List<RerankItem>> RerankAsync(RerankerConfig config, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a Reranker per call. Defaults to RerankModel.CohereRerank4Fast; override via model. Pass topN to cap returned items (0 returns all). Each RerankItem carries the document's original .Index and relevance .Score, ordered most relevant first. Use the constructor + RerankAsync for a custom timeout or reusing one instance across many queries.
    static Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, RerankModel model = CohereRerank4Fast, int topN = 0, CancellationToken cancellationToken = default)
  sealed record RerankerConfig
    ctor()
    List<string> Documents { get; init; }
    string Query { get; init; }
    // Scaled up internally with the document count.
    TimeSpan Timeout { get; init; }
    // Caps how many items are returned; 0 returns all.
    int TopN { get; init; }
  class RerankerException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.Retrieving
  class Content
    ctor(object value, ContentLink link, string mimeType)
    override string ToString()
    ContentLink Link
    string MimeType
    object Value
  class ContentLink
    ctor(string link, float score = 0f)
    ctor(List<string> segments, float score = 0f)
    ctor(ContentLink parent, string secondPart, float score = 0f)
    ctor(string link, string secondPart, float score = 0f)
    ContentLink Parent { get; }
    ContentLink Root { get; }
    override bool Equals(object? obj)
    List<(string Link, string Internal)> GenerateHierarchicalSplitLinks()
    override int GetHashCode()
    override string ToString()
    static bool operator ==(ContentLink? lhs, ContentLink? rhs)
    static bool operator !=(ContentLink? lhs, ContentLink? rhs)
    readonly string Link
    readonly float Score
    readonly List<string> Segments
  class IdMapperException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class JsonAsset
    ctor(string content)
    IEnumerable<string> GetAllKeys()
    string[] GetKeys()
    bool TryGetValue(string keyPath, out object? value)
    bool TryGetValueAsObject(string keyPath, out object? value)
  class Retriever : IAsyncDisposable
    ctor()
    KernelContext Context { get; }
    ValueTask DisposeAsync()
    Task<ContentLink[]> ExpandAsync(ContentLink[] links)
    Task<ContentLink[]> ExpandAsync(ContentLink link)
    Task<Content?> GetContentAsync(ContentLink link)
    Retriever.ContentMetadata? GetContentMetadata(string metadataId)
    Task<string> GetContentsAsync(string query, Retriever.GetContentsOptions options)
    ContentLink? Ignore(ContentLink link, string detail)
    Task InitializeAsync(string dataDirectory, EmbeddingModel embeddingModel = OpenAI3Small, VectorStoreConfig? vectorStore = null)
    Task InitializeAsync(IReadOnlyList<AssetUri> assetUris, EmbeddingModel embeddingModel = OpenAI3Small, VectorStoreConfig? vectorStore = null)
    ContentLink[] Prefer(ContentLink link, string detail)
    ContentLink[] Prefer(ContentLink[] links, string detail)
    Task<ContentLink[]> SearchAsync(string query, int maxLinks = 25, float searchThreshold = 0.1f)
    Task<Retriever.Event[]> SearchEventsAsync(string startUtcTimestamp, string endUtcTimestamp, int maxResults = 100)
    Task<Retriever.Event[]> SearchEventsAsync(string startUtcTimestamp, string endUtcTimestamp, string searchString, int maxResults = 100)
    Task<KeywordSearchResult[]> SearchKeywordsAsync(string searchString, int maxResults = 100)
    Task StopAsync()
    Task WaitForLoadingToEndAsync()
  class Retriever.ContentMetadata
    ctor()
    DateTime CreatedAt { get; set; }
    string DocumentTitle { get; set; }
    string OriginalName { get; set; }
    string OriginalPath { get; set; }
    int PageNumber { get; set; }
    List<int> PageNumbers { get; set; }
    List<string> TitleHierarchy { get; set; }
    DateTime UpdatedAt { get; set; }
  class Retriever.Event
    ctor()
    DateTime Date { get; set; }
    string Description { get; set; }
    override string ToString()
    ContentLink Source
  class Retriever.GetContentsOptions
    ctor()
    string ContentPostfixes { get; set; }
    float CumulativeScoreThreshold { get; set; }
    int HitCountThreshold { get; set; }
    int MaxContentCount { get; set; }
    int MaxSearchResults { get; set; }
    int MinContentCount { get; set; }
    float SearchThreshold { get; set; }
    bool UseCumulativeScore { get; set; }
    bool UseIdMapper { get; set; }

namespace Ikon.AI.Shader
  class Actions
    ctor()
    ScriptableStringValue AfterPass { get; set; }
    ScriptableStringValue AfterShader { get; set; }
    ScriptableStringValue BeforePass { get; set; }
    ScriptableStringValue BeforeShader { get; set; }
    Dictionary<string, ScriptableStringValue> Listeners
  class FunctionDetailsDictionaryConverter<T> : JsonConverter where T : IFunctionDetails, new()
    ctor()
    override bool CanConvert(Type objectType)
    override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class History
    ctor()
    ScriptableValue<int> Max { get; set; }
    ScriptableValue<int> Skip { get; set; }
  interface IFunctionDetails
    ScriptableValue<bool> Select { get; set; }
  interface IScriptContext
    void AddFilter(string name, KernelContext context, Function function)
    void AddFunction(string name, KernelContext context, Function function)
    bool ContainsKey(string key)
    IEnumerable<string> GetKeys()
    object? GetValue(string key)
    string GetValueAsString(string key)
    void Register<T>() where T : class
    void SetValue(string key, object? value)
  interface IScriptEngine
    IScriptContext CreateContext()
    bool TryParse(string template, out IScriptTemplate? parsedTemplate, out string? errorMessage)
  interface IScriptTemplate
    Task<string> RenderAsync(IScriptContext context)
  class Intent
    ctor()
    History? History { get; set; }
    string Id { get; set; }
    Dictionary<string, object?>? Input { get; set; }
    Misc? Misc { get; set; }
    Model? Model { get; set; }
    List<Pass> Passes { get; set; }
    ScriptableValue<bool> Select { get; set; }
  class JTokenConverter
    ctor()
    static object? ConvertJTokenToObject(JToken? token)
  class Misc
    ctor()
    ScriptableStringValue CitationInsertionCommand { get; set; }
    ScriptableStringValue CitationUserMessageExtension { get; set; }
    List<string> FailClassificationLabels { get; set; }
    ScriptableStringValue FailureMessage { get; set; }
    ScriptableValue<bool> InsertCitationsBackToModelMessage { get; set; }
    ScriptableValue<bool> UseTrimming { get; set; }
  class Model
    ctor()
    ScriptableStringValue AudioOutputVoiceId { get; set; }
    ScriptableValue<int> CharsPerSecond { get; set; }
    ScriptableValue<int> CharsPerUpdate { get; set; }
    ScriptableValue<bool> DisableFunctionCalling { get; set; }
    ScriptableValue<bool> DiscardTextOutputWithFunctionCalls { get; set; }
    ScriptableValue<bool> ForceCitations { get; set; }
    ScriptableStringValue GbnfGrammar { get; set; }
    ExpandoObject? JsonSchema { get; set; }
    ScriptableStringValue JsonSchemaString { get; set; }
    ScriptableValue<bool> LogFullRequest { get; set; }
    ScriptableValue<bool> LogRenderedShader { get; set; }
    ScriptableValue<int> MaxOutputTokens { get; set; }
    ScriptableValue<int> MaxRecursionDepth { get; set; }
    ScriptableStringValue Name { get; set; }
    ScriptableStringValue ReasoningEffort { get; set; }
    ScriptableValue<int> ReasoningTokenBudget { get; set; }
    List<ModelRegion> Regions { get; set; }
    ScriptableValue<int> RequestTimeoutSeconds { get; set; }
    ScriptableValue<double> Temperature { get; set; }
    List<Transform> Transforms { get; set; }
    ScriptableValue<bool> UseAudioOutput { get; set; }
    ScriptableValue<bool> UseCaching { get; set; }
    ScriptableValue<bool> UseCitations { get; set; }
    ScriptableValue<bool> UseJson { get; set; }
    ScriptableValue<bool> UseStreaming { get; set; }
    ScriptableValue<bool> UseThrottling { get; set; }
    ScriptableValue<bool> UseUserNames { get; set; }
  class ModelFunctionDetails : IFunctionDetails
    ctor()
    ScriptableStringValue? Call { get; set; }
    ScriptableValue<bool>? CallOnlyOnce { get; set; }
    ScriptableStringValue Description { get; set; }
    ScriptableValue<bool>? InlineCall { get; set; }
    Dictionary<string, ParameterDetails> Parameters { get; set; }
    ScriptableStringValue Process { get; set; }
    ScriptableValue<bool> Select { get; set; }
    ScriptableStringValue? Use { get; set; }
  class Output
    ctor()
    ScriptableStringValue AfterPass { get; set; }
    ScriptableStringValue AfterShader { get; set; }
    ScriptableStringValue BeforePass { get; set; }
    ScriptableStringValue BeforeShader { get; set; }
  class ParameterDetails
    ctor()
    object? DefaultValue { get; set; }
    ScriptableStringValue? Description { get; set; }
    ScriptableValue<bool>? HasDefaultValue { get; set; }
    ScriptableStringValue? Type { get; set; }
    ScriptableStringValue? Use { get; set; }
  class Pass
    ctor()
    Actions Actions { get; set; }
    ScriptableStringValue Command { get; set; }
    ScriptableStringValue Context { get; set; }
    History? History { get; set; }
    string Id { get; set; }
    Dictionary<string, object?>? Input { get; set; }
    Misc? Misc { get; set; }
    Model? Model { get; set; }
    Dictionary<string, ModelFunctionDetails> ModelFunctions { get; set; }
    Output Output { get; set; }
    ScriptableValue<bool> Select { get; set; }
    Dictionary<string, TemplateFunctionDetails> TemplateFunctions { get; set; }
  class ScriptableStringDictionaryConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class ScriptableStringValue
    ctor(string? value = "")
    bool IsScript { get; }
    string? Value { get; }
    Task<string?> GetValueAsync(Func<string, Task<string>> renderer)
  class ScriptableStringValueConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class ScriptableValue<T> where T : struct
    ctor(T value)
    ctor(string script)
    string? Script { get; }
    T? Value { get; }
    Task<T> GetValueAsync(Func<string, Task<string>> renderer)
  class ScriptableValueConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class Shader
    ctor(string shaderConfigAsJsonString, bool enableRenderedShaderLogging = false)
    Dictionary<string, object?> Input { get; }
    static string Escape(string? text)
    IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, ExpandoObject? implicitJsonSchema = null, string? implicitJsonExample = null, IdMapper? idMapper = null, string modelUserName = "", string modelMessagePrefix = "", string modelMessageSuffix = "", int iteration = 0, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default)
    Task<T> GenerateObjectAsync<T>(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, JsonSerializerOptions? jsonSerializerOptions = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default) where T : new()
    Task<string> GenerateStringAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default)
    void SetActiveState<T>(string key, T? value)
    static string Unescape(string? text)
    event EventHandler<string>? RenderedShader
  class Shader.TemplateMessage
    ctor()
    string Content { get; set; }
    string Role { get; set; }
  class ShaderCache : AsyncLocalInstance<ShaderCache>
    ctor()
    string? DefaultSpaceId { get; set; }
    ShaderCache.ImplicitShader GetImplicitShader()
  class ShaderCache.ImplicitShader
    ctor(AssetUri? shaderUri, string callerFilePath, ShaderCache outer)
    IAsyncEnumerable<LLMEvent> GenerateAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters)
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters)
    Task<Shader> GetShaderAsync()
  class ShaderConfig
    ctor()
    static object Default { get; }
    History History { get; set; }
    Dictionary<string, object?> Input { get; set; }
    List<Intent> Intents { get; set; }
    ScriptableValue<int> MaxLogLineLength { get; set; }
    ScriptableValue<int> MaxLogSectionLineCount { get; set; }
    Misc Misc { get; set; }
    Model Model { get; set; }
    string ShaderLanguage { get; set; }
    int? ShaderVersion { get; set; }
  class ShaderInvocationContext
    ctor()
    string FailureMessage { get; }
    string Reasoning { get; }
  class StyleInvariantComparer : IEqualityComparer<string>
    ctor()
    bool Equals(string? x, string? y)
    int GetHashCode(string obj)
  class TemplateFunctionDetails : IFunctionDetails
    ctor()
    ScriptableStringValue Name { get; set; }
    ScriptableValue<bool> Select { get; set; }
  class Transform
    ctor()
    Dictionary<string, object?> Config { get; set; }
    ScriptableStringValue Name { get; set; }
    ScriptableValue<bool> ProcessInput { get; set; }
    ScriptableValue<bool> ProcessOutput { get; set; }
    ScriptableValue<int> WindowOverlap { get; set; }
    ScriptableValue<int> WindowSize { get; set; }

namespace Ikon.AI.Shader.Scriban
  class ScribanScriptEngine : IScriptEngine
    ctor()
    IScriptContext CreateContext()
    bool TryParse(string template, out IScriptTemplate? parsedTemplate, out string? errorMessage)

namespace Ikon.AI.SoundEffectGeneration
  interface ISoundEffectGenerator : IDisposable, ISoundEffectGeneratorInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    // Streams raw PCM chunks; use GenerateSoundEffectFileAsync for a buffered, encoded audio file instead.
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<SoundEffectGeneratorResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
  interface ISoundEffectGeneratorInfo
    bool SupportsLooping { get; }
  class NonRetryableSoundEffectGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class SoundEffectGenerator : ISoundEffectGenerator
    ctor(string modelName)
    ctor(SoundEffectGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SoundEffectGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsLooping { get; }
    void Dispose()
    Task<SoundEffectGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a SoundEffectGenerator per call. Returns a buffered WAV file (.Data/.MimeType/.DurationSeconds). Use the constructor + GenerateSoundEffectFileAsync for duration/looping/prompt-influence, or GenerateSoundEffectAsync for streaming PCM chunks.
    static Task<SoundEffectGeneratorResult> GenerateAsync(string prompt, SoundEffectGeneratorModel model = ElevenLabsV2, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<SoundEffectGeneratorResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    static SoundEffectGeneratorCapabilities GetCapabilities(SoundEffectGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SoundEffectGeneratorModel model)
  sealed class SoundEffectGeneratorCapabilities : ISoundEffectGeneratorInfo
    ctor()
    bool SupportsLooping { get; init; }
  sealed record SoundEffectGeneratorConfig
    ctor()
    double? DurationSeconds { get; init; }
    bool Loop { get; init; }
    string Prompt { get; init; }
    double PromptInfluence { get; init; }
    // Applies to the buffered ISoundEffectGenerator.GenerateSoundEffectFileAsync result; the streaming ISoundEffectGenerator.GenerateSoundEffectAsync chunks are unaffected.
    ResultDelivery ResultDelivery { get; init; }
    TimeSpan Timeout { get; init; }
  class SoundEffectGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SoundEffectGeneratorModel
    ElevenLabsV2
  static class SoundEffectGeneratorModelExtensions
    static string DisplayName(this SoundEffectGeneratorModel model)
  // Kind tells how the audio was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record SoundEffectGeneratorResult : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    double DurationSeconds { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }

namespace Ikon.AI.SpeechGeneration
  interface ISpeechGenerator : IDisposable
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
  class NonRetryableSpeechGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class SpeechGenerator : ISpeechGenerator
    ctor(string modelName)
    ctor(SpeechGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    void Dispose()
    Task<AudioChunk> GenerateAsync(string text, string? voice = null, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a SpeechGenerator per call. Defaults to SpeechGeneratorModel.ElevenFlash25; override via model. Pass voice to pick a voice (model default otherwise). Streamed chunks are concatenated into one PCM AudioChunk. Never returns null — throws SpeechGeneratorException on failure or empty output. Use the constructor + GenerateSpeechAsync for chunk-by-chunk streaming or other fields.
    static Task<AudioChunk> GenerateAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechGeneratorModel model)
    static IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> GetVoiceIdsByModel()
  sealed record SpeechGeneratorConfig
    ctor()
    string Instructions { get; init; }
    string Language { get; init; }
    // Speaking-rate multiplier (1.0 = normal); null keeps the model's own default. Honored by OpenAI and Google; ElevenLabs ignores it.
    double? Speed { get; init; }
    string Text { get; init; }
    TimeSpan Timeout { get; init; }
    string VoiceId { get; init; }
  class SpeechGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SpeechGeneratorModel
    AzureSpeechService
    OpenAITts1
    OpenAITts1Hd
    Gpt4OmniMiniTts
    ElevenFlash2
    ElevenMultilingual2
    ElevenFlash25
    Eleven3
    GoogleChirp3
    Gemini25FlashTts
    Gemini25ProTts
    Gemini31FlashTts
  static class SpeechGeneratorModelExtensions
    static string DisplayName(this SpeechGeneratorModel model)
  static class TextFilter
    static string Filter(string text, TextFilter.Config config)
  sealed class TextFilter.Config
    ctor()
    int MaxTextLength { get; set; }
    bool RemoveEmojis { get; set; }
    bool SimplifyUrls { get; set; }
    bool SpeakOnlyFirstParagraph { get; set; }

namespace Ikon.AI.SpeechRecognition
  sealed record AnalyzePronunciationConfig
    ctor()
    int ChannelCount { get; init; }
    string Language { get; init; }
    string ReferenceText { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    TimeSpan Timeout { get; init; }
  interface ISpeechRecognizer : IDisposable, ISpeechRecognizerInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  interface ISpeechRecognizerInfo
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
  class NonRetryableSpeechRecognizerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed record Pronunciation.Break
    ctor()
    int BreakLength { get; init; }
    List<string> ErrorTypes { get; init; }
    Pronunciation.MissingBreak MissingBreak { get; init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get; init; }
  sealed record Pronunciation.Feedback
    ctor()
    Pronunciation.Prosody Prosody { get; init; }
  sealed record Pronunciation.Intonation
    ctor()
    List<string> ErrorTypes { get; init; }
    Pronunciation.Monotone Monotone { get; init; }
  sealed record Pronunciation.MissingBreak
    ctor()
    double Confidence { get; init; }
  sealed record Pronunciation.Monotone
    ctor()
    double SyllablePitchDeltaConfidence { get; init; }
  sealed record Pronunciation.NBest
    ctor()
    double Confidence { get; init; }
    string Display { get; init; }
    string ITN { get; init; }
    string Lexical { get; init; }
    string MaskedITN { get; init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Word> Words { get; init; }
  sealed record Pronunciation.Phoneme
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed record Pronunciation.PhonemePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
  sealed record Pronunciation.PronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    double CompletenessScore { get; init; }
    double FluencyScore { get; init; }
    double PronScore { get; init; }
    double ProsodyScore { get; init; }
  sealed record Pronunciation.Prosody
    ctor()
    Pronunciation.Break Break { get; init; }
    Pronunciation.Intonation Intonation { get; init; }
  sealed record Pronunciation.Result
    ctor()
    int Channel { get; init; }
    string DisplayText { get; init; }
    long Duration { get; init; }
    string Id { get; init; }
    List<Pronunciation.NBest> NBest { get; init; }
    long Offset { get; init; }
    string RecognitionStatus { get; init; }
    double SNR { get; init; }
  sealed record Pronunciation.Syllable
    ctor()
    long Duration { get; init; }
    string Grapheme { get; init; }
    long Offset { get; init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed record Pronunciation.SyllablePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
  sealed record Pronunciation.UnexpectedBreak
    ctor()
    double Confidence { get; init; }
  sealed record Pronunciation.Word
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    List<Pronunciation.Phoneme> Phonemes { get; init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Syllable> Syllables { get; init; }
    string Text { get; init; }
  sealed record Pronunciation.WordPronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    string ErrorType { get; init; }
    Pronunciation.Feedback Feedback { get; init; }
  sealed record RecognizeContinuousSpeechConfig
    ctor()
    string[] CandidateLanguages { get; init; }
    int ChannelCount { get; init; }
    string Language { get; init; }
    int SampleRate { get; init; }
  // Supply the audio exactly one way: raw PCM via Samples or SamplesPcm16 (with SampleRate/ChannelCount), or an encoded audio file via Data (with MimeType), Url, or AssetUri (resolved automatically).
  sealed record RecognizeSpeechConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    int ChannelCount { get; init; }
    byte[]? Data { get; init; }
    string Language { get; init; }
    string? MimeType { get; init; }
    string? Prompt { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    double Temperature { get; init; }
    TimeSpan Timeout { get; init; }
    string? Url { get; init; }
  sealed class SpeechRecognizer : ISpeechRecognizer
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(SpeechRecognizerModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    static SpeechRecognizerCapabilities GetCapabilities(SpeechRecognizerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechRecognizerModel model)
    Task<string> RecognizeAsync(float[] samples, int sampleRate, int channelCount = 1, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a SpeechRecognizer per call. Defaults to SpeechRecognizerModel.WhisperLarge3Turbo; override via model. Returns the recognized text (empty when nothing was recognized). Use the constructor + RecognizeBatchSpeechAsync for a language hint, prompt, or other fields, or RecognizeContinuousSpeechAsync for streaming.
    static Task<string> RecognizeAsync(float[] samples, int sampleRate, SpeechRecognizerModel model = WhisperLarge3Turbo, int channelCount = 1, CancellationToken cancellationToken = default)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerAdapter : ISpeechRecognizer
    ctor(ISpeechRecognizer speechRecognizer, SpeechRecognizerAdapter.Config? config = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    // SilenceTriggered mode only: forces recognition after this much continuous speech without a pause. TimeSpan.Zero or negative disables the limit. Defaults to 30s.
    TimeSpan MaxSpeechDuration { get; set; }
    // Defaults to Mode.SilenceTriggered.
    SpeechRecognizerAdapter.Mode Mode { get; set; }
    // Used only in GrowingWindow/SlidingWindow modes (GrowingWindow recognizes all accumulated audio, SlidingWindow only audio since the last run); defaults to 5s.
    TimeSpan RecognitionInterval { get; set; }
    TimeSpan RequestTimeout { get; set; }
    // SilenceTriggered mode only: a pause of this length flushes accumulated speech for recognition. Defaults to 750ms.
    TimeSpan SilenceDuration { get; set; }
    float SilenceThreshold { get; set; }
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
  sealed class SpeechRecognizerCapabilities : ISpeechRecognizerInfo
    ctor()
    bool SupportsBatchRecognition { get; init; }
    bool SupportsContinuousRecognition { get; init; }
    bool SupportsPronunciationAnalysis { get; init; }
  class SpeechRecognizerException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SpeechRecognizerModel
    AzureSpeechService
    Whisper2
    WhisperLarge3
    WhisperLarge3Turbo
    Gpt4OmniTranscribe
    Gpt4OmniMiniTranscribe
    Gpt4OmniTranscribeDiarize
    DeepgramNova3General
    DeepgramNova3Medical
    AssemblyAIUniversal3ProStreaming
    AssemblyAIUniversalStreamingEnglish
    AssemblyAIUniversalStreamingMultilingual
    VoxtralMiniTranscribe2
  static class SpeechRecognizerModelExtensions
    static string DisplayName(this SpeechRecognizerModel model)

namespace Ikon.AI.Storage
  class KeywordIndex
    ctor()
    Task AddAsync(string word, string link)
    static KeywordIndex Deserialize(Stream stream)
    Task InitializeAsync()
    void RemoveTooCommonTerms(double threshold = 0.5, int minDocumentCount = 5)
    List<KeywordSearchResult> Search(string words)
    void Serialize(Stream stream)
  struct KeywordSearchResult
    ctor(string link, float score)
    string Link
    float Score
  enum Metric
    DotProduct
    CosineSimilarity
    EuclideanDistance
  struct Result<T>
    ctor(int key, float score, T value)
    int Key
    float Score
    T Value
  class VectorDatabase
    ctor(VectorStoreConfig? config = null)
    Task CreateCollectionAsync(string collectionName, EmbeddingModel model)
    Task<int> GetDataItemCountAsync(string collectionName)
    Task RemoveAsync(string collectionName, IEnumerable<string> tags)
    Task<List<Result<object>>> SearchAsync(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<object>>> SearchAsync(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<int> SetAsync(string collectionName, int? key, string text, object value, IEnumerable<string>? tags = null)
    Task<int> SetAsync(string collectionName, int? key, float[] vector, object value, IEnumerable<string>? tags = null)
  enum VectorStoreBackend
    InMemory
    PgVector
  // The default (or a null config) keeps the in-memory store, so existing callers are unaffected; pass one with VectorStoreBackend.PgVector to persist and scale.
  sealed class VectorStoreConfig
    ctor()
    VectorStoreBackend Backend { get; init; }
    // Each operation opens and disposes its own connection, so the call belongs inside the factory: () => DatabaseConnection.Postgres(...).DbConnection. Required when Backend is VectorStoreBackend.PgVector.
    Func<DbConnection>? ConnectionFactory { get; init; }
    string TablePrefix { get; init; }

namespace Ikon.AI.Utils
  static class ImageUtils
    static byte[] ConvertAlphaMaskToBlackWhiteMask(byte[] maskData)
    static byte[] ConvertBlackWhiteMaskToAlphaMask(byte[] maskData)
    // Caps both dimensions at maxDimension (aspect preserved) and re-encodes as JPEG; returns the source bytes unchanged when the image already fits and is at most maxBytes.
    static (byte[] Bytes, string MimeType, int Width, int Height) EncodeJpegCapped(byte[] source, string sourceMimeType, int maxDimension = 1568, int quality = 70, int maxBytes = 204800)
    static (int width, int height) GetImageDimensions(byte[] buffer)
    static byte[] InvertMask(byte[] maskData)
    static bool IsWebP(byte[] data)

namespace Ikon.AI.VideoEnhancement
  interface IVideoEnhancer : IDisposable
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
  class NonRetryableVideoEnhancerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class VideoEnhancer : IVideoEnhancer
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoEnhancerModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    Task<VideoEnhancerResult> EnhanceAsync(string videoUrl, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a VideoEnhancer per call. Defaults to VideoEnhancerModel.TensorPixUpscale2xUltra41; override via model. Returns the enhanced video as a download URL in .Url plus .OutputFps/.OutputSizeBytes. Use the constructor + EnhanceVideoAsync for raw bytes (Data), frame-range trim, target FPS, or other fields.
    static Task<VideoEnhancerResult> EnhanceAsync(string videoUrl, VideoEnhancerModel model = TensorPixUpscale2xUltra41, CancellationToken cancellationToken = default)
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoEnhancerModel model)
  // Supply the video exactly one way: Data (with MimeType), Url, or AssetUri (resolved to a URL automatically).
  sealed record VideoEnhancerConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    int? EndFrame { get; init; }
    string? MimeType { get; init; }
    int? StartFrame { get; init; }
    int? TargetFps { get; init; }
    TimeSpan Timeout { get; init; }
    string? Url { get; init; }
  class VideoEnhancerException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum VideoEnhancerModel
    TensorPixFpsBoost
    TensorPixUpscale2xUltra4
    TensorPixUpscale2xUltra41
    TensorPixUpscale4xUltra4
  static class VideoEnhancerModelExtensions
    static string DisplayName(this VideoEnhancerModel model)
  sealed record VideoEnhancerResult
    ctor()
    int? OutputFps { get; init; }
    long? OutputSizeBytes { get; init; }
    string Url { get; init; }

namespace Ikon.AI.VideoGeneration
  interface IVideoGenerator : IDisposable, IVideoGeneratorInfo
    Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IVideoGeneratorInfo
    int MaxInputAudios { get; }
    int MaxInputImages { get; }
    int MaxInputVideos { get; }
    VideoGeneratorResolutionMode ResolutionMode { get; }
    IReadOnlyList<int> SupportedLengths { get; }
    IReadOnlyList<VideoGeneratorResolution> SupportedResolutions { get; }
    bool SupportsAudio { get; }
    bool SupportsImageToVideo { get; }
    bool SupportsMultipleImages { get; }
    bool SupportsNegativePrompt { get; }
    bool SupportsSeed { get; }
    bool SupportsTailImage { get; }
    bool SupportsTextToVideo { get; }
  class NonRetryableVideoGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class VideoGenerator : IVideoGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputAudios { get; }
    int MaxInputImages { get; }
    int MaxInputVideos { get; }
    VideoGeneratorResolutionMode ResolutionMode { get; }
    IReadOnlyList<int> SupportedLengths { get; }
    IReadOnlyList<VideoGeneratorResolution> SupportedResolutions { get; }
    bool SupportsAudio { get; }
    bool SupportsImageToVideo { get; }
    bool SupportsMultipleImages { get; }
    bool SupportsNegativePrompt { get; }
    bool SupportsSeed { get; }
    bool SupportsTailImage { get; }
    bool SupportsTextToVideo { get; }
    void Dispose()
    Task<VideoGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a VideoGenerator per call. Defaults to VideoGeneratorModel.Veo31Fast; override via model. Returns the result with the generated clip's .Url. Use the constructor + GenerateVideoAsync for image-to-video, length, resolution, aspect ratio, negative prompt, audio, or other fields.
    static Task<VideoGeneratorResult> GenerateAsync(string prompt, VideoGeneratorModel model = Veo31Fast, CancellationToken cancellationToken = default)
    Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = default)
    static VideoGeneratorCapabilities GetCapabilities(VideoGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoGeneratorModel model)
  enum VideoGeneratorAspectRatio
    Ratio16x9
    Ratio9x16
    Ratio4x3
    Ratio3x4
    Ratio1x1
  sealed class VideoGeneratorCapabilities : IVideoGeneratorInfo
    ctor()
    int MaxInputAudios { get; init; }
    int MaxInputImages { get; init; }
    int MaxInputVideos { get; init; }
    // In characters; zero when the model states no limit.
    int MaxPromptLength { get; init; }
    VideoGeneratorResolutionMode ResolutionMode { get; init; }
    IReadOnlyList<int> SupportedLengths { get; init; }
    IReadOnlyList<VideoGeneratorResolution> SupportedResolutions { get; init; }
    bool SupportsAudio { get; init; }
    bool SupportsImageToVideo { get; init; }
    bool SupportsMultipleImages { get; init; }
    bool SupportsNegativePrompt { get; init; }
    bool SupportsSeed { get; init; }
    bool SupportsTailImage { get; init; }
    bool SupportsTextToVideo { get; init; }
  sealed record VideoGeneratorConfig
    ctor()
    VideoGeneratorAspectRatio AspectRatio { get; init; }
    bool? GenerateAudio { get; init; }
    // Reference audio, for models that accept it. Addressed from the prompt as @Audio1 and so on, in prompt order.
    List<InputAudio> InputAudios { get; init; }
    List<InputImage> InputImages { get; init; }
    // Reference footage, for models that accept it. Addressed from the prompt in the provider's own notation — fal's Seedance uses @Video1, @Video2 in prompt order.
    List<InputVideo> InputVideos { get; init; }
    int Length { get; init; }
    string? NegativePrompt { get; init; }
    string? Prompt { get; init; }
    VideoGeneratorResolution Resolution { get; init; }
    int? Seed { get; init; }
    TimeSpan Timeout { get; init; }
  class VideoGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum VideoGeneratorModel
    Kling26
    Kling30
    Kling30Omni
    KlingVideoO1
    LumaRay20
    LumaRay20Flash
    Pika22
    Pixverse55
    Pixverse6
    Pollo20
    RunwayGen4
    Seedance15Pro
    Seedance20
    Seedance20Fast
    Seedance20Mini
    Seedance25
    Seedance25Reference
    Sora2
    Sora2Pro
    Veo31
    Veo31Fast
    ViduQ2Pro
    ViduQ2Turbo
    ViduQ3Pro
    ViduQ3Turbo
    Wan26
    Wan27
    GrokImagineVideo
    GrokImagineVideo15
  static class VideoGeneratorModelExtensions
    static string DisplayName(this VideoGeneratorModel model)
  enum VideoGeneratorResolution
    Resolution360p
    Resolution480p
    Resolution540p
    Resolution720p
    Resolution768p
    Resolution1080p
    Resolution4K
  enum VideoGeneratorResolutionMode
    Discrete
    AspectRatio
  sealed record VideoGeneratorResult
    ctor()
    string Url { get; init; }

namespace Ikon.AI.WebScraping
  sealed record Cookie
    ctor()
    string Domain { get; init; }
    double ExpirationDate { get; init; }
    bool HostOnly { get; init; }
    bool HttpOnly { get; init; }
    int Id { get; init; }
    string Name { get; init; }
    string Path { get; init; }
    string SameSite { get; init; }
    bool Secure { get; init; }
    bool Session { get; init; }
    string StoreId { get; init; }
    string Value { get; init; }
  sealed record DownloadFileConfig
    ctor()
    string CountryCode { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
  sealed record DownloadFileResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Url { get; init; }
  interface IWebScraper : IDisposable, IWebScraperInfo
    Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = default)
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = default)
  interface IWebScraperInfo
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
  sealed record MultiPageScrapeConfig
    ctor()
    bool AddGivenUrlsToWhitelist { get; init; }
    bool AllowOnlyGivenUrls { get; init; }
    List<Cookie> Cookies { get; init; }
    string CountryCode { get; init; }
    int DelayMs { get; init; }
    string ExcludedCSSElements { get; init; }
    List<string> ExcludedLineStarts { get; init; }
    List<string> ExcludedWholeLines { get; init; }
    bool Headless { get; init; }
    bool IgnoreRobotsTxt { get; init; }
    bool IncludeLinkedFiles { get; init; }
    string IncludedCSSElements { get; init; }
    string JavaScript { get; init; }
    bool LoadResources { get; init; }
    string Locale { get; init; }
    int MaxDepth { get; init; }
    int MaxPages { get; init; }
    WebScraperOutputFormat OutputFormat { get; init; }
    string PlaywrightScript { get; init; }
    bool RerunIfGivenUrlsMissing { get; init; }
    TimeSpan SinglePageTimeout { get; init; }
    TimeSpan Timeout { get; init; }
    List<string> UrlBlacklist { get; init; }
    List<string> UrlWhitelist { get; init; }
    List<string> Urls { get; init; }
    bool UseReadability { get; init; }
    bool UseSitemap { get; init; }
    bool UseSitemapOnly { get; init; }
    bool UseStreaming { get; init; }
    TimeSpan WaitAfter { get; init; }
  class NonRetryableWebScraperException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed record PageResult
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string MimeType { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed record ScreenshotConfig
    ctor()
    List<Cookie> Cookies { get; init; }
    string CountryCode { get; init; }
    bool FullPage { get; init; }
    bool Headless { get; init; }
    int Height { get; init; }
    string JavaScript { get; init; }
    string Locale { get; init; }
    string PlaywrightScript { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
    bool UseCaptchaSolver { get; init; }
    TimeSpan WaitAfter { get; init; }
    int Width { get; init; }
  sealed record ScreenshotResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
  sealed record SinglePageScrapeConfig
    ctor()
    List<Cookie> Cookies { get; init; }
    string CountryCode { get; init; }
    string ExcludedCSSElements { get; init; }
    List<string> ExcludedLineStarts { get; init; }
    List<string> ExcludedWholeLines { get; init; }
    bool Headless { get; init; }
    bool IncludeLinkedFiles { get; init; }
    string IncludedCSSElements { get; init; }
    string JavaScript { get; init; }
    bool LoadResources { get; init; }
    string Locale { get; init; }
    WebScraperOutputFormat OutputFormat { get; init; }
    string PlaywrightScript { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
    bool UseCaptchaSolver { get; init; }
    bool UseReadability { get; init; }
    TimeSpan WaitAfter { get; init; }
  sealed class WebScraper : IWebScraper
    ctor(string modelName)
    ctor(WebScraperModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(WebScraperModel model, IReadOnlyList<ModelRegion>? regions)
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
    void Dispose()
    Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = default)
    static WebScraperCapabilities GetCapabilities(WebScraperModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebScraperModel model)
    Task<PageResult> ScrapeAsync(string url, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a WebScraper per call. Defaults to WebScraperModel.Jina; override via model. Returns the page as Markdown in .Content plus .Title/.Url. Use the constructor + ScrapeSinglePageAsync for output format/cookies/JS or other fields, or ScrapeMultiplePagesAsync/TakeScreenshotAsync/DownloadFileAsync for crawling, screenshots, and downloads.
    static Task<PageResult> ScrapeAsync(string url, WebScraperModel model = Jina, CancellationToken cancellationToken = default)
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = default)
  sealed class WebScraperCapabilities : IWebScraperInfo
    ctor()
    bool SupportsFileDownload { get; init; }
    bool SupportsMultiPageScraping { get; init; }
    bool SupportsScreenshotting { get; init; }
    bool SupportsSinglePageScraping { get; init; }
  class WebScraperException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum WebScraperModel
    Spider
    Jina
    LocalPuppeteer
    LocalNodriver
    LocalPlaywright
  static class WebScraperModelExtensions
    static string DisplayName(this WebScraperModel model)
  enum WebScraperOutputFormat
    Text
    Markdown
    Html

namespace Ikon.AI.WebSearching
  interface IWebSearcher : IDisposable, IWebSearcherInfo
    Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
    Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
  interface IWebSearcherInfo
    bool SupportsImageSearching { get; }
  class NonRetryableWebSearcherException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed record SearchConfig
    ctor()
    string CountryCode { get; init; }
    string InSiteUrl { get; init; }
    string Language { get; init; }
    int MaxResults { get; init; }
    WebSearcherOutputFormat OutputFormat { get; init; }
    string Query { get; init; }
    TimeSpan Timeout { get; init; }
  sealed record SearchResult
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string MimeType { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed class WebSearcher : IWebSearcher
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(WebSearcherModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsImageSearching { get; }
    void Dispose()
    static WebSearcherCapabilities GetCapabilities(WebSearcherModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebSearcherModel model)
    Task<List<SearchResult>> SearchAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a WebSearcher per call. Defaults to WebSearcherModel.Google; override via model. Each SearchResult exposes .Url/.Title/.Content. Use the constructor + SearchPagesAsync for site-restricted search, country/language targeting, or other SearchConfig fields, or SearchImagesAsync (with an image-capable model) for image search.
    static Task<List<SearchResult>> SearchAsync(string query, WebSearcherModel model = Google, int maxResults = 10, CancellationToken cancellationToken = default)
    Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
    Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
  sealed class WebSearcherCapabilities : IWebSearcherInfo
    ctor()
    bool SupportsImageSearching { get; init; }
  class WebSearcherException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum WebSearcherModel
    Spider
    Jina
    Google
    GoogleImages
    Amazon
    Bing
    BingImages
    Youtube
  static class WebSearcherModelExtensions
    static string DisplayName(this WebSearcherModel model)
  enum WebSearcherOutputFormat
    Text
    Markdown
    Html

# Ikon.Parallax Public API

namespace Ikon.Parallax
  sealed class ActionArgs<T>
    ctor()
    Context ClientContext { get; init; }
    T Value { get; init; }
  // Collapses the busy/status ceremony of an async handler to await _busy.RunAsync(_status, LoadAsync). For the busy flag alone (no status reactive), use _busy.AsToken() from Ikon.Common.Core.Reactive instead.
  static class ReactiveBusyExtensions
    // Clears status, raises busy for the duration of the work (via ReactiveBoolExtensions.AsToken, so it always returns to false), and routes a failure's message into status instead of throwing. Cancellation (OperationCanceledException) propagates to the caller. Returns whether the work completed, so callers can add their own failure handling on top.
    static Task<bool> RunAsync(this Reactive<bool> busy, Reactive<string?> status, Func<Task> work)
  // Per-client theme state created by UI.UseTheme. Holds each client's active theme and switches it: Current is bindable in views, and ToggleAsync can be bound directly to a button's onClick.
  sealed class ThemeControl
    ClientReactive<Theme> Current { get; }
    Task SetAsync(Theme theme)
    Task ToggleAsync()
  class UI
    ctor(IAppBase app, ITheme theme)
    // When true, each render cycle logs per-frame timing breakdowns to the app log.
    bool EnableProfiling { get; set; }
    // Default true. A subtree that reads only non-reactive data will not refresh until one of its reactive dependencies changes; set false to force a full re-render every cycle.
    bool EnableSubtreeCaching { get; set; }
    // Build the whole component tree inside content; it re-renders automatically when any reactive state read during the build changes. This is the app's root render entry point. This overload BLOCKS the calling thread until the initial render completes (it drives the async render with GetAwaiter().GetResult()). Call it from a synchronous startup path; from an async or single-threaded synchronization context call RootAsync and await it instead, to avoid stalling or deadlocking that context.
    void Root(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
    // Build the whole component tree inside content; it re-renders automatically when any reactive state read during the build changes. This is the app's root render entry point.
    Task RootAsync(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
    // Call once in Main, before clients join. With followClient true (the default) a joining client keeps its own saved theme and clients without one get defaultTheme; false forces defaultTheme on every client. Bind the returned Current in views and ToggleAsync to a button's onClick.
    // defaultTheme: The theme applied to clients that have none of their own (or to all clients when followClient is false).
    // followClient: When true, respects a joining client's own saved theme; when false, forces defaultTheme on every join.
    ThemeControl UseTheme(Theme defaultTheme = Dark, bool followClient = true)
  class UIView
    string DefaultIconLibrary { get; }
    // True only while capturing the build-time boot snapshot — a public asset shown to everyone before the live UI connects (always false on the live render). Gate per-user or sensitive content on this, preferably via the SnapshotReveal/SnapshotHide/SnapshotOnly wrappers.
    bool IsSnapshot { get; }
    // The boot-snapshot variant id this capture render was asked for (the client's Context.SnapshotVariant): the app's [BootSnapshot] seed rules name variant skeletons, and the capture client passes each id here so the app can branch to the matching skeleton. Empty on route captures (render the real page) and on every live render.
    string SnapshotVariant { get; }
    void AddNode(string type, IDictionary? props = null, List<UIViewNode>? children = null, string? key = null, string[]? style = null, string? styleId = null)
    string? CreateAction<T>(Func<ActionArgs<T>, Task>? callback)
    // The returned string is an opaque reference to use as an image src (e.g. on an Image component), not a data URL. The data buffer is copied on registration, so the caller may reuse or mutate it immediately after the call. data must be non-empty — an empty buffer has no valid reference and throws ArgumentException.
    string RegisterPayload(byte[] data, string mimeType)
  sealed class UIViewNode
    // Treat as immutable: the node is shared by reference into the subtree cache, and the differ relies on the child list being the pristine as-built content, so mutating it corrupts diffing and the cache. The mutable backing list is builder-internal.
    IReadOnlyList<UIViewNode> Children { get; }
    string? ContentFingerprint { get; }
    bool HasExplicitKey { get; }
    string Id { get; }
    int IdHash { get; }
    // Debug-only, process-global switch: when true, EVERY node built by ANY view on ANY thread and for ANY client emits a source file/line marker that is serialized into the wire payload, inflating all UI updates. Despite reading like a per-instance toggle it is static mutable state with no thread-safety, so flip it only for local debugging (the runtime sets it from the app's DebugMode) and never leave it on in production.
    static bool IncludeSourceMarkers { get; set; }
    // Backed by the compact shape-interned PropsMap on server-built trees; treat as immutable.
    IReadOnlyDictionary<string, object?> Props { get; }
    string? SourceMarker { get; }
    string? StableHint { get; }
    IReadOnlyList<string> StyleIds { get; }
    string Type { get; }

namespace Ikon.Parallax.Components.Charts
  sealed record AxisConfig
    ctor()
    // For time scales this is a d3-time-format token string (e.g. "%H:%M", "%m/%d %H:%M"), not a .NET format.
    string? Format { get; init; }
    bool Hidden { get; init; }
    string? Legend { get; init; }
    int? LegendOffset { get; init; }
    // When set, the axis shows approximately this many evenly-spaced ticks instead of one per data point.
    int? TickCount { get; init; }
    int? TickPadding { get; init; }
    int? TickRotation { get; init; }
    int? TickSize { get; init; }
    int? TruncateTickAt { get; init; }
  enum BarGroupMode
    Stacked
    Grouped
  enum BarLayout
    Vertical
    Horizontal
  record ChartAxisStyle
    ctor()
    string? DomainColor { get; init; }
    ChartTextStyle? Legend { get; init; }
    string? TickColor { get; init; }
    ChartTextStyle? TickLabel { get; init; }
  sealed record ChartClickArgs
    ctor()
    string? Id { get; init; }
    string? IndexValue { get; init; }
    string? SerieId { get; init; }
    object? Value { get; init; }
  enum ChartColorScheme
    Nivo
    Category10
    Accent
    Dark2
    Paired
    Pastel1
    Pastel2
    Set1
    Set2
    Set3
    Tableau10
    BrownBlueGreen
    PurpleRedGreen
    PinkYellowGreen
    PurpleOrange
    RedBlue
    RedGrey
    RedYellowBlue
    RedYellowGreen
    Spectral
    Blues
    Greens
    Greys
    Oranges
    Purples
    Reds
    BlueGreen
    BluePurple
    GreenBlue
    OrangeRed
    PurpleBlueGreen
    PurpleBlue
    PurpleRed
    RedPurple
    YellowGreenBlue
    YellowGreen
    YellowOrangeBrown
    YellowOrangeRed
  record ChartCrosshairStyle
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  static class ChartExtensions
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip values and value-axis ticks human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void BarChart(this UIView view, string[]? style = null, IEnumerable<Dictionary<string, object>>? data = null, IEnumerable<string>? keys = null, string? indexBy = null, BarGroupMode? groupMode = null, BarLayout? layout = null, ScaleType? valueScale = null, ScaleType? indexScale = null, bool? reverse = null, double? minValue = null, double? maxValue = null, double? padding = null, double? innerPadding = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enableLabel = null, int? labelSkipWidth = null, int? labelSkipHeight = null, string? labelTextColor = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderRadius = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip Y values and left-axis ticks human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void LineChart(this UIView view, string[]? style = null, IEnumerable<LineChartSeries>? data = null, ScaleType? xScaleType = null, ScaleType? yScaleType = null, double? xScaleMin = null, double? xScaleMax = null, double? yScaleMin = null, double? yScaleMax = null, bool? yScaleStacked = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enablePoints = null, int? pointSize = null, string? pointColor = null, string? pointBorderColor = null, int? pointBorderWidth = null, bool? enableArea = null, double? areaOpacity = null, double? areaBaselineValue = null, bool? enableCrosshair = null, CrosshairType? crosshairType = null, LineCurve? curve = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, double? lineWidth = null, bool? isInteractive = null, bool? useMesh = null, bool? enableSlices = null, string? xFormat = null, string? yFormat = null, string? valueUnit = null, string? gradientFromColor = null, string? gradientToColor = null, IEnumerable<double>? gridXValues = null, IEnumerable<double>? gridYValues = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip values human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void PieChart(this UIView view, string[]? style = null, IEnumerable<PieChartDatum>? data = null, double? innerRadius = null, double? padAngle = null, double? cornerRadius = null, double? startAngle = null, double? endAngle = null, bool? sortByValue = null, ChartMargin? margin = null, bool? enableArcLabels = null, string? arcLabelsTextColor = null, double? arcLabelsSkipAngle = null, bool? enableArcLinkLabels = null, string? arcLinkLabelsTextColor = null, double? arcLinkLabelsSkipAngle = null, double? arcLinkLabelsThickness = null, string? arcLinkLabelsColor = null, double? activeOuterRadiusOffset = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? arcLabelAsPercentage = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
  record ChartGridStyle
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  record ChartLabelsStyle
    ctor()
    ChartTextStyle? Text { get; init; }
  record ChartLegendStyle
    ctor()
    ChartTextStyle? Text { get; init; }
    ChartTextStyle? Title { get; init; }
  sealed record ChartMargin
    ctor()
    int? Bottom { get; init; }
    int? Left { get; init; }
    int? Right { get; init; }
    int? Top { get; init; }
  record ChartTextStyle
    ctor()
    string? Color { get; init; }
    string? FontFamily { get; init; }
    int? FontSize { get; init; }
  record ChartTheme
    ctor()
    ChartAxisStyle? Axis { get; init; }
    ChartColorScheme? ColorScheme { get; init; }
    string[]? Colors { get; init; }
    ChartCrosshairStyle? Crosshair { get; init; }
    ChartGridStyle? Grid { get; init; }
    ChartLabelsStyle? Labels { get; init; }
    ChartLegendStyle? Legends { get; init; }
    ChartTextStyle? Text { get; init; }
    ChartTooltipStyle? Tooltip { get; init; }
  static class ChartThemes
    static ChartTheme DefaultDark { get; }
    static ChartTheme DefaultLight { get; }
  record ChartTooltipStyle
    ctor()
    string? BackgroundColor { get; init; }
    string? BorderColor { get; init; }
    int? BorderRadius { get; init; }
    ChartTextStyle? Text { get; init; }
  enum CrosshairType
    X
    Y
    TopLeft
    Top
    TopRight
    Right
    BottomRight
    Bottom
    BottomLeft
    Left
    Cross
  enum LegendAnchor
    Top
    TopRight
    Right
    BottomRight
    Bottom
    BottomLeft
    Left
    TopLeft
    Center
  sealed record LegendConfig
    ctor()
    LegendAnchor? Anchor { get; init; }
    LegendDirection? Direction { get; init; }
    int? ItemHeight { get; init; }
    int? ItemWidth { get; init; }
    int? ItemsSpacing { get; init; }
    int? SymbolSize { get; init; }
    int? TranslateX { get; init; }
    int? TranslateY { get; init; }
  enum LegendDirection
    Row
    Column
  sealed record LineChartPoint
    ctor()
    // Pass a string label for point scales, or a number for linear/time scales — the object type is genuinely mixed.
    required object X { get; init; }
    required double Y { get; init; }
  sealed record LineChartSeries
    ctor()
    string? Color { get; init; }
    IEnumerable<LineChartPoint>? Data { get; init; }
    required string Id { get; init; }
  enum LineCurve
    Linear
    MonotoneX
    Step
    StepBefore
    StepAfter
    Cardinal
    Basis
  sealed record PieChartDatum
    ctor()
    string? Color { get; init; }
    required string Id { get; init; }
    string? Label { get; init; }
    required double Value { get; init; }
  enum ScaleType
    Point
    Linear
    Time
    Log

namespace Ikon.Parallax.Components.DataTable
  record Cell
    ctor()
    string? ActionId { get; init; }
    CellAction[]? Actions { get; init; }
    bool? Disabled { get; init; }
    string? Label { get; init; }
    string[]? Style { get; init; }
    SemanticTone? Tone { get; init; }
    CellType Type { get; init; }
    // For checkbox cells this is the checked state as the string "true" or "false".
    string? Value { get; init; }
    static Cell Action(string label, string actionId, string[]? style = null)
    static Cell ActionGroup(CellAction[] actions)
    // style classes replace the themed tone token; lead the array with the "default" marker to merge the tone token underneath them instead.
    static Cell Badge(string value, SemanticTone? tone = null, string[]? style = null)
    static Cell Checkbox(bool value, string actionId, string[]? style = null, bool disabled = false)
    static Cell Text(string? value, string[]? style = null)
  record CellAction
    ctor(string Label, string ActionId, string[]? Style = null, string? Icon = null)
    string ActionId { get; init; }
    string? Icon { get; init; }
    string Label { get; init; }
    string[]? Style { get; init; }
  enum CellType
    Text
    Badge
    Action
    Actions
    Checkbox
  record DataTableColumn
    ctor(string Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left, string? MinWidth = null, bool Wrap = false)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string Header { get; init; }
    string? MinWidth { get; init; }
    string? Width { get; init; }
    bool Wrap { get; init; }
  static class DataTableExtensions
    // Per-slot styling (header, rows, cells, pagination, …) goes through styles; see DataTableStyles for the slots.
    static void DataTable(this UIView view, DataTableColumn[] columns, DataTableRow[] rows, int totalCount, int pageIndex, int pageSize, Func<int, Task>? onPageChange = null, Func<string, Task>? onRowClick = null, Func<string, Task>? onActionClick = null, Action<UIView>? emptyContent = null, int[]? columnWidths = null, Func<string, Task>? onColumnResize = null, string[]? style = null, DataTableStyles? styles = null, string? prevLabel = null, string? nextLabel = null, string? pageLabel = null, string? key = null)
  record DataTableRow
    ctor(string Id, Cell[] Cells)
    Cell[] Cells { get; init; }
    string Id { get; init; }
  // Each slot is a Crosswind class array that merges on top of the slot's themed default, exactly like a component's style: parameter; set only the slots you are changing.
  sealed record DataTableStyles
    ctor()
    string[]? ActionButton { get; init; }
    string[]? Cell { get; init; }
    string[]? DataCell { get; init; }
    string[]? Empty { get; init; }
    string[]? Header { get; init; }
    string[]? HeaderCell { get; init; }
    string[]? PageNumber { get; init; }
    string[]? PageNumberActive { get; init; }
    string[]? Pagination { get; init; }
    string[]? PaginationButton { get; init; }
    string[]? ResizeHandle { get; init; }
    string[]? Row { get; init; }
    string[]? Tooltip { get; init; }

namespace Ikon.Parallax.Components.ImageEditor
  static class ImageEditorExtensions
    // triggerSave/triggerUndo/triggerRedo are edge-triggered — increment the value to fire that action.
    // brushColor: Hex color, e.g. "#ff0000".
    // tool: Defaults to ImageEditorTool.Brush on the frontend.
    // zoom: Zoom level: 1.0 = 100%.
    // highResolution: Keeps the canvas at the image's native resolution (capped): sharp zoom, full-quality export, but capped undo history. When false the canvas is downscaled to fit its container.
    // fillShapes: When true, the region and lasso tools fill the drawn shape with the brush color instead of stroking its outline. Defaults to false on the frontend.
    // textMaxLength: Max length of the text tool's floating input; null = no limit.
    // textFontSize: Font size in pixels; null = derived from brush width.
    // textPadding: Padding in pixels around the text; null = 4.
    // onSave: Receives the saved image as base64 data.
    static void ImageEditorCanvas(this UIView view, string[]? style = null, string? src = null, int? brushWidth = null, string? brushColor = null, ImageEditorTool? tool = null, double? zoom = null, bool? highResolution = null, bool? fitContainer = null, bool? fillShapes = null, int? textMaxLength = null, int? textFontSize = null, int? textPadding = null, Func<ImageEditorSaveArgs, Task>? onSave = null, Func<ImageEditorHistoryArgs, Task>? onHistoryChange = null, int? triggerSave = null, int? triggerUndo = null, int? triggerRedo = null, string? styleId = null, string? key = null)
  sealed record ImageEditorHistoryArgs
    ctor(bool CanUndo, bool CanRedo)
    bool CanRedo { get; init; }
    bool CanUndo { get; init; }
  sealed record ImageEditorSaveArgs
    ctor(string ImageData)
    string ImageData { get; init; }
  enum ImageEditorTool
    Brush
    Eraser
    Text
    Arrow
    Region
    Lasso
    Line

namespace Ikon.Parallax.Components.Standard
  static class AccessibilityExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void AccessibleIcon(this UIView view, string[]? style = null, string? label = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Hidden visually but still exposed to screen readers.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void VisuallyHidden(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  record ActionEvent
    ctor(string ActionType, bool Success)
    string ActionType { get; init; }
    bool Success { get; init; }
  enum ActionKind
    Unknown
    CaptureImage
    CopyToClipboard
    DownloadFile
    ExitFullscreen
    GetLocation
    PickContacts
    RequestFullscreen
    Share
  abstract record ActionOptions
  enum ActivationMode
    Automatic
    Manual
  static class AlertExtensions
    // The icon defaults per tone (success check, warning triangle, error alert, info circle).
    // tone: Selects the Alert color variant; Neutral and Brand use the default surface.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // description: Muted body text under the title.
    // icon: Lucide icon name overriding the tone's default icon.
    // onDismiss: When set, renders a dismiss button in the top-right corner.
    // content: Extra elements rendered under the description.
    static void Alert(this UIView view, string title, SemanticTone tone = Neutral, string[]? style = null, string? description = null, string? icon = null, bool showIcon = true, Func<Task>? onDismiss = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? iconStyle = null, string[]? dismissStyle = null, Action<UIView>? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum Align
    Start
    Center
    End
  static class BadgeExtensions
    // With no style args it renders the themed Theming.Badge.* pill for the tone; caller styles replace the base token, or merge on top of it when the array leads with "default".
    // outline: When true, uses the outlined variant: the tone's border becomes visible instead of transparent. The fill is unchanged.
    // dot: When true, renders a small status dot before the label in the badge's current color.
    // dotStyle: Style for the dot. Defaults to a 6px circle filled with the badge foreground color.
    static void Badge(this UIView view, string text, SemanticTone tone = Neutral, string[]? style = null, BadgeSize size = Md, bool outline = false, bool dot = false, string[]? dotStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum BadgeSize
    // 22px pill with extra-small text.
    Sm
    // 24px pill with small text (the default).
    Md
    // 28px pill with small text.
    Lg
  static class BreadcrumbExtensions
    // Items with an OnClick render as clickable links; the last item always renders as the non-clickable current page (aria-current="page") regardless of its OnClick.
    // items: Trail entries in root-to-current order.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // separatorIcon: Lucide icon name for the separator; defaults to "chevron-right".
    static void Breadcrumb(this UIView view, IReadOnlyList<BreadcrumbItem> items, string[]? style = null, string? separatorIcon = null, string[]? linkStyle = null, string[]? itemStyle = null, string[]? pageStyle = null, string[]? separatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record BreadcrumbItem
    // Label: Visible text of the crumb.
    // OnClick: Navigation callback. Ignored for the last item, which always renders as the non-clickable current page.
    ctor(string Label, Func<Task>? OnClick = null)
    string Label { get; init; }
    Func<Task>? OnClick { get; init; }
  static class CalendarExtensions
    // All date values (value, defaultValue, minDate, maxDate, callbacks) are ISO yyyy-MM-dd strings; month is yyyy-MM. Controlled via value+onValueChange; omit both and pass defaultValue for uncontrolled.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // month: Controlled display month; accepts yyyy-MM or yyyy-MM-dd.
    // minDate: Earliest selectable date (inclusive).
    // maxDate: Latest selectable date (inclusive).
    // locale: BCP-47 locale used for weekday and month labels (e.g. en-US).
    static void Calendar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? month = null, string? defaultMonth = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, string? locale = null, bool? disabled = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onMonthChange = null)
    // Renders a trigger button plus a popover Calendar. Date values are ISO yyyy-MM-dd strings; controlled via value+onValueChange, uncontrolled via defaultValue.
    // format: BCP-47 locale format hint for the trigger label (e.g. en-US).
    // minDate: Earliest selectable date (inclusive).
    // maxDate: Latest selectable date (inclusive).
    // label: Field label rendered above the picker, matching TextField.
    static void DatePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, string? format = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? calendarStyle = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Maps to the W3C MediaStream facingMode constraint as an "ideal" hint — the browser falls back to whatever camera is available if the requested side does not exist (e.g. desktops without a rear camera).
  enum CameraFacing
    User
    Environment
  sealed record CaptureImageActionOptions : ActionOptions
    ctor()
    CaptureImageConstraints? Constraints { get; init; }
    ClientImageCaptureFormat? Format { get; init; }
    int? Height { get; init; }
    // Defaults to CaptureImageMode.Headless.
    CaptureImageMode? Mode { get; init; }
    // 0.0 to 1.0; applies to lossy formats.
    double? Quality { get; init; }
    int? Width { get; init; }
  // Applied directly in CaptureImageMode.Headless mode. In CaptureImageMode.Native mode only FacingMode is honored (mapped to the file input's capture attribute); the OS camera UI ignores the other constraints.
  sealed record CaptureImageConstraints
    ctor()
    string? DeviceId { get; init; }
    CameraFacing? FacingMode { get; init; }
  enum CaptureImageMode
    // Native OS camera UI (preview + shutter + front/back toggle on phones). On mobile it uses a transient <input type="file" capture> and must be invoked from a user gesture; the user can dismiss without capturing. On desktop browsers it transparently falls back to the headless getUserMedia grab.
    Native
    // Silent capture: getUserMedia grabs a single frame off-screen and tears the stream down — no preview, no shutter. Honors CaptureImageConstraints.
    Headless
  static class CardExtensions
    // With no style args it renders the themed card token (Theming.Card.Default, or Theming.Card.Interactive when onClick is set); caller styles replace it, or merge on top of it when the array leads with "default".
    // header: Extra header elements rendered after the title/description.
    // contentStyle: Defaults to Theming.Card.Content when a header is present, plain padding otherwise.
    // onClick: Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Card(this UIView view, string[]? style = null, string? title = null, string? description = null, Action<UIView>? header = null, Action<UIView>? content = null, Action<UIView>? footer = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? contentStyle = null, string[]? footerStyle = null, Delegate? onClick = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void Card(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // description: Muted explanation text under the title.
    // icon: Lucide icon name rendered inside the tinted icon square.
    // action: Builder for the action row (e.g. a "Create" button).
    static void EmptyState(this UIView view, string title, string[]? style = null, string? description = null, string? icon = null, Action<UIView>? action = null, string[]? iconWrapStyle = null, string[]? iconStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? actionsStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // delta: Delta text rendered next to the value (e.g. "+12%").
    // trendLabel: Muted context text after the delta (e.g. "vs last month").
    // icon: Lucide icon name rendered inside the tinted icon box on the right.
    static void StatCard(this UIView view, string label, string value, string[]? style = null, string? delta = null, StatTrend trend = Flat, string? trendLabel = null, string? icon = null, SemanticTone iconTone = Neutral, string[]? labelStyle = null, string[]? valueStyle = null, string[]? trendStyle = null, string[]? iconBoxStyle = null, string[]? iconStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum CarouselAlign
    Start
    Center
    End
  sealed record CarouselBreakpoint
    // MinWidth: Container width (in CSS pixels) at which this breakpoint becomes active. The active breakpoint is the one with the largest MinWidth still less than or equal to the current container width.
    // SlidesPerView: Number of slides visible in the viewport at this breakpoint.
    // SlidesPerGroup: Number of slides advanced per navigation step at this breakpoint. Defaults to SlidesPerView when null.
    // SlideGapPx: Gap in CSS pixels between adjacent slides at this breakpoint. Defaults to the top-level slideGapPx when null.
    ctor(int MinWidth, int SlidesPerView, int? SlidesPerGroup = null, int? SlideGapPx = null)
    int MinWidth { get; init; }
    int? SlideGapPx { get; init; }
    int? SlidesPerGroup { get; init; }
    int SlidesPerView { get; init; }
  static class CarouselExtensions
    // Provide slides via slides for the simple case, or via the content builder using Slide for fully custom children.
    // index: Controlled zero-based slide index.
    // defaultIndex: Initial slide index for uncontrolled mode.
    // slidesPerView: Slides visible at once; defaults to 1. Overridden by the matching breakpoints entry.
    // slidesPerGroup: Slides advanced per navigation step; defaults to slidesPerView. Overridden by the matching breakpoints entry.
    // slideGapPx: Gap between adjacent slides; only takes effect when the effective slides-per-view exceeds 1. Defaults to 0.
    // breakpoints: Responsive overrides keyed by container width; see CarouselBreakpoint.
    // showArrows: Renders the Previous/Next buttons; defaults to true.
    // showIndicators: Renders the indicator dots; defaults to true.
    static void Carousel(this UIView view, string[]? style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, int? slidesPerView = null, int? slidesPerGroup = null, int? slideGapPx = null, IEnumerable<CarouselBreakpoint>? breakpoints = null, IEnumerable<CarouselSlideItem>? slides = null, bool? showArrows = null, bool? showIndicators = null, string? previousLabel = null, string? nextLabel = null, string? previousIconName = null, string? nextIconName = null, string[]? rootStyle = null, string[]? viewportStyle = null, string[]? slideStyle = null, string[]? previousStyle = null, string[]? nextStyle = null, string[]? indicatorsStyle = null, string[]? indicatorStyle = null, string[]? indicatorActiveStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<int, Task>? onIndexChange = null)
    static void Slide(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  sealed record CarouselSlideItem
    // Content: Builder function for rendering the slide.
    // Key: Optional stable key used for diffing.
    ctor(Action<UIView> Content, string? Key = null)
    Action<UIView> Content { get; init; }
    string? Key { get; init; }
  static class ChatLogExtensions
    // Use instead of a manual Column(overflow-auto) for any "newest at the bottom, follow when content grows" layout. autoScrollKey tells the framework when to re-anchor to the bottom — pass the reactive message collection, a count, or any other value that changes when the content does.
    // autoScrollKey: Anything whose value changes when the content changes — the reactive message collection, a count, or a composite string (see LayoutExtensions.ScrollArea).
    static void ChatLog(this UIView view, string[]? style = null, object? autoScrollKey = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, string? styleId = null, string? key = null)
  enum CheckedState
    Unchecked
    Checked
    Indeterminate
  static class CodeEditorExtensions
    // value: Controlled text value; with no write-back handler (onValueChange or onSubmit) the editor renders read-only.
    // defaultValue: Initial value for uncontrolled mode.
    // language: Syntax-highlighting language identifier (e.g. typescript, csharp, json).
    // readOnly: Prevents editing but allows selection and copy.
    // showLineNumbers: Defaults to true.
    // tabSize: Spaces inserted by Tab; defaults to 2.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // onSubmit: Invoked when the user presses Ctrl+Enter. The parameter contains the submitted value — prefer it over re-reading the bound reactive.
    static void CodeEditor(this UIView view, string? value = null, string? defaultValue = null, string? language = null, string? placeholder = null, bool? readOnly = null, bool? disabled = null, bool? showLineNumbers = null, int? tabSize = null, bool? insertSpaces = null, bool? wrap = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? gutterStyle = null, string[]? contentStyle = null, string[]? languageBadgeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  enum CollisionDetection
    ClosestCenter
    ClosestCorners
    RectIntersection
    PointerWithin
  enum ColorFormat
    Hex
    Rgb
    Hsl
  static class ColorPickerExtensions
    // value: Controlled color in the chosen format.
    // defaultValue: Initial color for uncontrolled mode.
    // format: Output format produced by onValueChange.
    // showAlpha: When true, shows an alpha slider and emits #RRGGBBAA/rgba()/hsla().
    // onValueChange: Fires continuously as the user drags or types a new color.
    // onValueCommit: Fires once the user releases a drag or commits a typed value.
    // label: Field label rendered above the picker, matching TextField.
    static void ColorPicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, ColorFormat format = Hex, bool? showAlpha = null, IReadOnlyList<string>? presets = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onValueCommit = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  enum ColumnAlign
    Left
    Center
    Right
  sealed record ContactsActionEvent : ActionEvent
    ctor(bool Success, IReadOnlyList<ClientContact>? Contacts)
    IReadOnlyList<ClientContact>? Contacts { get; init; }
  static class ContainerExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // onClick: Accepts sync (() => …) and async (async () => …) lambdas alike. A clickable Box automatically carries button semantics — role="button", tabIndex=0, Enter/Space activation. Override either through props, and give an icon-only Box an ["aria-label"].
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void Box(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Box(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes appended to the fixed flex flex-col base class, which cannot be removed or replaced.
    static void Column(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Column(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes appended to the fixed flex base class, which cannot be removed or replaced.
    static void Flex(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Flex(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes appended to the fixed grid base class, which cannot be removed or replaced.
    static void Grid(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Grid(this UIView view, string[]? style, Action<UIView> children)
    // Absolutely positioned; place inside a Stack container.
    // style: Crosswind utility classes; absolute is prepended when the array lacks it, and a null style defaults to absolute inset-0.
    static void Layer(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Layer(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes appended to the fixed flex flex-row base class, which cannot be removed or replaced.
    static void Row(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Row(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind/Tailwind utility classes appended to the base spinner styling (e.g. a colour or margin).
    static void Spinner(this UIView view, string[]? style = null, SpinnerSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Layers children on top of each other; give each layer a Layer child.
    // style: Crosswind utility classes appended to the fixed relative base class, which cannot be removed or replaced.
    static void Stack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Stack(this UIView view, string[]? style, Action<UIView> children)
  record ContentGridColumn
    ctor(string? Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string? Header { get; init; }
    string? Width { get; init; }
  static class ContentGridExtensions
    static void ContentGrid(this UIView view, ContentGridColumn[] columns, Action<UIView>? content = null, string[]? style = null, string[]? headerStyle = null, string? key = null)
  sealed record CopyToClipboardActionOptions : ActionOptions
    ctor()
    required string Text { get; init; }
  static class CoreExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // text: Visible button text. When content is provided it instead becomes the accessible aria-label.
    static void ActionButton(this UIView view, string[]? style = null, ActionKind action = Unknown, string? text = null, ActionOptions? options = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<ActionEvent, Task>? onActionComplete = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // text: Visible button text. When content is provided it instead becomes the accessible aria-label.
    // href: URL to navigate to when clicked; renders the button as an anchor element.
    // icon: Lucide icon name rendered alongside the text; content (when provided) wins over it.
    // tooltip: Hover text rendered with the themed Tooltip; it also becomes the accessible name when nothing else names the control. Do not use a title prop instead.
    // tooltipRootStyle: Styles for the tooltip wrapper, the element that sits in the parent's layout — responsive and positioning classes go here, not on the button. Defaults to inline-flex shrink-0.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void Button(this UIView view, string[]? style = null, string? text = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null, Delegate? onPressStart = null, Delegate? onPressEnd = null)
    static void Button(this UIView view, string buttonText, string[]? style = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null, Delegate? onPressStart = null, Delegate? onPressEnd = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void Heading(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Heading(this UIView view, string headingText, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // size: Merged as the icon's base sizing, so a w-*/h-* class in style still wins. Omit it to leave sizing entirely to style.
    // library: Defaults to the view's default icon library.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void Icon(this UIView view, string[]? style = null, string? name = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Icon(this UIView view, string iconName, string[]? style = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // style: Crosswind utility classes; defaults to the theme's Button.Link styling.
    // href: Required. A same-origin path navigates in place without a document reload (surfacing as app.Navigation.PathChangedAsync), so the connection survives it and no onClick is needed to navigate.
    // rel: When target is "_blank" and rel is null, defaults to "noopener noreferrer". Pass "external" to force a full document load for a same-origin link.
    // onClick: Fires alongside navigation; for side effects only — href already handles the destination.
    // content: Custom child content; text then becomes the aria-label.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void Link(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Link(this UIView view, string linkText, string[]? style = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // style: Crosswind/Tailwind utility classes for styling. With no array the body renders with Markdown.Default — heading scale, list markers, table rules, blockquote bar and a self-scrolling fenced-code box. Pass "default" as the first class to keep those and add your own on top; any other array replaces them.
    static void Markdown(this UIView view, string[]? style = null, string? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void Markdown(this UIView view, string markdownContent, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // href: URL to navigate to when clicked. Renders as an anchor element when specified.
    static void Text(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Text(this UIView view, string textContent, string[]? style = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // defaultValue: Initial pressed state when not controlling value.
    // label: Trailing text label; wraps the toggle and the text in a <label>, so clicking the text toggles the control and the text is the toggle's accessible name.
    static void Toggle(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void ToggleGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // defaultValue: Initial selection when not controlling value.
    static void ToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // defaultValue: Initial selection when not controlling value.
    static void ToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
  enum Dir
    Ltr
    Rtl
  static class DisclosureExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // forceMount: When true, keeps content in DOM when hidden.
    static void AccordionContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Wraps an AccordionTrigger.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void AccordionHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void AccordionItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // defaultValue: Initial value for uncontrolled mode.
    static void AccordionMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // defaultValue: Initial value for uncontrolled mode.
    // collapsible: Allows the open item to be closed again, leaving none open.
    static void AccordionSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? collapsible = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void AccordionTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // defaultOpen: Initial open state for uncontrolled mode.
    static void Collapsible(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // forceMount: When true, keeps content in DOM when hidden.
    static void CollapsibleContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void CollapsibleTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  sealed record DownloadFileActionOptions : ActionOptions
    ctor()
    byte[]? Data { get; init; }
    string? Filename { get; init; }
    string? MimeType { get; init; }
    // Regular or data URL. When Data is set, auto-generated as a data URL using MimeType, falling back to "application/octet-stream" when MimeType is unset.
    string Url { get; init; }
  static class DragAndDropExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // onDragEnd: Invoked when the drag operation ends (dropped or cancelled).
    // activationDistance: Pixels of pointer movement before a drag activates; a pointerdown below the threshold is delivered as a normal click (inner Button.onClick fires). Null: drag activates immediately.
    static void DndContext(this UIView view, string[]? style = null, CollisionDetection collisionDetection = ClosestCenter, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<DragStartArgs, Task>? onDragStart = null, Func<DragMoveArgs, Task>? onDragMove = null, Func<DragOverArgs, Task>? onDragOver = null, Func<DragEndArgs, Task>? onDragEnd = null, Func<Task>? onDragCancel = null, int? activationDistance = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // activeDragId: The ID of the currently dragged item. When set, the overlay only renders its content after the server has sent content matching this drag ID, preventing stale content from a previous drag.
    static void DragOverlay(this UIView view, string[]? style = null, bool? dropAnimation = true, string? activeDragId = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // hideOnDrag: When true, hides the original element during drag. Use with DragOverlay.
    // data: Custom data attached to this draggable, available in drag event arguments.
    static void Draggable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, bool? hideOnDrag = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // data: Custom data attached to this droppable, available in drag event arguments.
    static void Droppable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // items: List of item identifiers in the current sort order.
    static void SortableContext(this UIView view, string[]? style = null, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // When a SortableHandle descendant is present, only pointerdown on the handle starts a drag; the rest of the item stays free for inner clickable elements. Place inside a SortableItem (or a SortableList itemContent); outside one it renders as a plain container.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void SortableHandle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void SortableItem(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // items: List of item identifiers in the current sort order.
    // onReorder: Invoked with the new order after a drag. The only write-back — persist args.NewOrder here, or reorders show on the client but never reach the app.
    // itemContent: Renders each item's content, receiving the item id; omitted, each item renders a drag-handle icon plus the id as text.
    // activationDistance: Pixels of pointer movement before a drag activates; a pointerdown below the threshold is delivered as a normal click (inner Button.onClick fires). Null: drag activates immediately.
    static void SortableList(this UIView view, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, CollisionDetection collisionDetection = ClosestCenter, Func<SortableReorderArgs, Task>? onReorder = null, Func<DragStartArgs, Task>? onDragStart = null, Action<UIView, string>? itemContent = null, string[]? listStyle = null, string[]? itemStyle = null, int? activationDistance = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record DragCancelArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  sealed record DragEndArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  sealed record DragMoveArgs
    ctor(string ActiveId, double DeltaX, double DeltaY)
    string ActiveId { get; init; }
    double DeltaX { get; init; }
    double DeltaY { get; init; }
  sealed record DragOverArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  sealed record DragStartArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  sealed record EscapeKeyDownArgs
    ctor()
  // Backed by a ClientReactive<T>: each client expands and collapses independently, and reads during UI rendering are dependency-tracked, so the tree re-renders automatically. Access it where a client scope is active (UI render or event handlers).
  sealed class ExpandedSet
    // expandedIds: Node ids that start expanded.
    ctor(params string[] expandedIds)
    void Clear()
    void Collapse(string id)
    void Expand(string id)
    bool IsExpanded(string id)
    void Set(string id, bool expanded)
    void Toggle(string id)
  // Hint used by FeedSlide to preload the slide's primary media asset.
  enum FeedMediaKind
    None
    Image
    // Preloads the video's metadata only, not the full payload.
    Video
    // Preloads the full video payload. Use sparingly — costs bandwidth.
    VideoFull
  // Performance model: • Native CSS scroll-snap drives the snap — no JS scroll loop. • Active slide is detected with IntersectionObserver, not scroll events. • Only slides inside [active - preloadBehind, active + preloadAhead] render their content; slides outside the window render as fixed-height spacers that preserve scroll position. • Media declared on FeedSlide is warmed with off-DOM Image/<video> elements as soon as a slide enters the preload window. • Autoplay is gated on the active slide only — neighbour videos are paused.
  static class FeedScrollerExtensions
    // slides: Slides rendered in order; grow the list and use onScrollNearEnd to page more in.
    // preloadAhead: Slides after the active one to keep mounted and preload media for. Default 2.
    // preloadBehind: Slides before the active one to keep mounted. Default 1.
    // autoPlay: Autoplay videos on the active slide. Default true.
    // muted: Controlled mute state for all media. Default true (browsers require muted autoplay).
    // scrollEndThreshold: Slides from the end at which onScrollNearEnd fires. Default 2.
    // style: Outermost viewport container; default token FeedScroller.Root.
    // slideStyle: Applied to every slide; default token FeedScroller.Slide.
    // onActiveChange: Invoked with the new active slide index.
    // onScrollNearEnd: Fires within scrollEndThreshold slides of the end, with the active slide index — use it to append the next page.
    // onMuteChange: Invoked when the user toggles mute on an in-slide control.
    static void FeedScroller(this UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[]? style = null, string[]? slideStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<int, Task>? onActiveChange = null, Func<int, Task>? onScrollNearEnd = null, Func<bool, Task>? onMuteChange = null)
    // index: Zero-based index of this slide.
    // style: Style classes for the slide container.
    // mediaKind: Kind of media to preload for this slide.
    // mediaUrl: URL of the media asset.
    // mediaPoster: Optional poster image URL for video slides.
    static void FeedSlide(this UIView view, int index, string[]? style = null, FeedMediaKind mediaKind = None, string? mediaUrl = null, string? mediaPoster = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  sealed record FeedSlide
    // Content: Builder invoked to render the slide. Only slides inside the render window are realized.
    // Key: Stable key used for diffing and preload identity. Defaults to slide index.
    ctor(Action<UIView> Content, string? Key = null, FeedMediaKind MediaKind = None, string? MediaUrl = null, string? MediaPoster = null)
    Action<UIView> Content { get; init; }
    string? Key { get; init; }
    FeedMediaKind MediaKind { get; init; }
    string? MediaPoster { get; init; }
    string? MediaUrl { get; init; }
  static class FilePickerExtensions
    // Only reports picked-file metadata to the server — the bytes stay on the client and are not uploaded until a FileUploadExtensions.FileUpload with a matching seedSelectionIds prop is mounted.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // maxFileSize: Maximum file size in bytes (enforced client-side before emitting selection).
    // onFileSelected: Invoked once per picked file, with client-generated SelectionId and metadata.
    // onValidationError: Invoked when a picked file is rejected client-side (e.g. over maxFileSize). Surface Reason to the user — without this the rejection is silent.
    static void FilePicker(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FilePickerSelectedArgs, Task>? onFileSelected = null, Func<FilePickerValidationErrorArgs, Task>? onValidationError = null, Action<UIView>? content = null)
  // Metadata for a file chosen in a FilePickerExtensions.FilePicker. The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
  sealed record FilePickerSelectedArgs
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  // Reported when client-side validation rejects a picked file (e.g. over maxFileSize). Surface Reason to the user — without a handler the rejection is silent.
  sealed record FilePickerValidationErrorArgs
    ctor(string FileName, string MimeType, long Size, string Reason)
    string FileName { get; init; }
    string MimeType { get; init; }
    string Reason { get; init; }
    long Size { get; init; }
  static class FileUploadExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // maxFileSize: Maximum file size in bytes.
    // onUploadPreStart: First accept/reject hook, before the client hashes or transfers anything. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system. Receives a Cancel delegate.
    // onUploadStart: Second hook, after the file hash is computed and before any chunks arrive; same return contract as onUploadPreStart.
    // onUploadError: Invoked when a file upload fails, is cancelled, or times out.
    // seedSelectionIds: Ids from a prior FilePickerExtensions.FilePicker selection; on first mount the client uploads the cached File handles through the normal pipeline, reusing each SelectionId as the UploadId.
    static void FileUpload(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string? capture = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, string[]? seedSelectionIds = null, Action<UIView>? content = null)
    // Style slots: zoneStyle (drop-zone container; the first positional style array is its alias), activeStyle (while a file is dragged over). The MIME filter is the named accept: parameter.
    // maxFileSize: Maximum file size in bytes.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // onUploadPreStart: First accept/reject hook, before the client hashes or transfers anything. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system.
    // onUploadStart: Second hook, after the file hash is computed and before any chunks arrive; same return contract as onUploadPreStart.
    // onUploadError: Invoked when a file upload fails, is cancelled, or times out.
    // seedSelectionIds: Ids from a prior FilePickerExtensions.FilePicker selection; on mount the client uploads the cached File handles through the normal pipeline, reusing each SelectionId as the UploadId.
    static void FileUploadZone(this UIView view, string[]? style = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string[]? accept = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, Func<bool, Task>? onDragActiveChange = null, Action<UIView>? content = null, string[]? zoneStyle = null, string[]? activeStyle = null, string? activeStyleId = null, string? styleId = null, string? key = null, string[]? seedSelectionIds = null, IReadOnlyDictionary<string, object>? props = null)
  static class FocusHintExtensions
    // targetViewId: View ID to receive focus. Defaults to the current view.
    static void FocusHint(this UIView view, FocusHintProps props, string? key = null, Guid? targetViewId = null)
  sealed record FocusHintProps
    ctor()
    TimeSpan? Cooldown { get; init; }
    bool FocusOnly { get; init; }
    FocusPriority Priority { get; init; }
    int Ranking { get; init; }
  sealed record FocusOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Maps to ARIA live region politeness.
  enum FocusPriority
    Polite
    Assertive
  static class FormExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // formValue: HTML form value submitted when checked.
    // label: Trailing text label wrapped with the checkbox in a <label> — clicking the text toggles the control and the text becomes its accessible name. Prefer this over placing your own Text beside a bare Checkbox, which associates nothing.
    // bind: Two-way binds the checkbox to a Reactive<T> — reads bind.Value and writes it back on every toggle. When set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void Checkbox(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // forceMount: When true, forces the indicator to render even when the checkbox is unchecked.
    static void CheckboxIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // onClearServerErrors: Invoked when server-side validation errors should be cleared.
    static void Form(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClearServerErrors = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void FormControl(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // name: The name of the form field, used for validation and form submission.
    // serverInvalid: When true, indicates the field has a server-side validation error.
    static void FormField(this UIView view, string[]? style = null, string? name = null, bool? serverInvalid = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void FormLabel(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // match: The validation condition that must be met for this message to display.
    // forceMatch: When true, forces the message to display regardless of the match condition.
    static void FormMessage(this UIView view, string[]? style = null, FormMessageMatch? match = null, bool? forceMatch = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void FormSubmit(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // htmlFor: The id of the element this label is associated with.
    static void Label(this UIView view, string[]? style = null, string? htmlFor = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // loop: When true, keyboard navigation loops from last item to first, and vice versa.
    // orientation: Orientation used for keyboard navigation.
    // label: Group-level label rendered above the radio group (same field ergonomics as TextField).
    // bind: Two-way binds the group to a Reactive<T> — reads bind.Value and writes it back on every selection. When set, value: is ignored and onValueChange still fires after the write-back.
    static void RadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<string>? bind = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // forceMount: When true, forces the indicator to render even when the radio is not selected.
    static void RadioGroupIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: The unique value for this radio item within the group.
    // required: When true, indicates this radio item must be selected before the form can be submitted.
    static void RadioGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, bool? required = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // onValueChange: Fires continuously while dragging.
    // onValueCommit: Fires once when dragging ends.
    // content: The default content's thumb carries aria-readonly for a read-only slider (controlled value: with no write-back); custom thumbs should set it too.
    // label: Also the accessible name of the thumbs, where role="slider" lives — a name on the root names nothing; multi-thumb thumbs are numbered from it.
    // bind: Two-way binds a single-thumb slider to a Reactive<T>, writing back as the user drags; value: is ignored and onValueChange still fires. Multi-thumb ranges use the value: list form.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void Slider(this UIView view, string[]? style = null, IReadOnlyList<double>? value = null, IReadOnlyList<double>? defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<double>, Task>? onValueChange = null, Func<IReadOnlyList<double>, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null, Reactive<double>? bind = null, string? ariaLabel = null)
    static void Slider(this UIView view, double value, string[]? style = null, double? min = null, double? max = null, double? step = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onValueChange = null, Func<double, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void SliderRange(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void SliderThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void SliderTrack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // formValue: HTML form value submitted when checked.
    // label: Trailing text label wrapped with the switch in a <label> — clicking the text toggles it and the text becomes the switch's accessible name; without this or ariaLabel it is announced as an unlabelled control.
    // bind: Two-way binds the switch to a Reactive<T> — reads bind.Value and writes it back on every toggle. When set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void Switch(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void SwitchThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // formValue: HTML form value submitted when checked.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void TriStateCheckbox(this UIView view, string[]? style = null, CheckedState? value = null, CheckedState? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onValueChange = null, Action<UIView>? content = null, string? ariaLabel = null)
  enum FormMessageMatch
    ValueMissing
    TypeMismatch
    TooShort
    TooLong
    PatternMismatch
    RangeUnderflow
    RangeOverflow
    StepMismatch
    BadInput
    CustomError
  enum HourFormat
    Hour24
    Hour12
  // The size: form of the Theming.Icon.Xs..Xl tokens. The style-array form (view.Icon([Icon.Lg], ...)) stays valid and, being a caller class, wins over size: when both are given.
  enum IconSize
    Xs
    Sm
    Md
    Lg
    Xl
  sealed record ImageCaptureActionEvent : ActionEvent
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string? Data { get; init; }
    int Height { get; init; }
    string? Mime { get; init; }
    int Width { get; init; }
  static class ImageExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void Avatar(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // delayMs: Delay in milliseconds before showing the fallback.
    static void AvatarFallback(this UIView view, string[]? style = null, int? delayMs = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // src: URL of the image to display.
    // alt: Alternative text description for accessibility.
    // onLoadingStatusChange: Invoked when the image loading status changes.
    static void AvatarImage(this UIView view, string[]? style = null, string? src = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onLoadingStatusChange = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // src: URL or path to the image source.
    // assetUri: Asset URI to resolve the image source from. Takes precedence over src.
    // alt: Alternative text description for accessibility.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, string? src = null, AssetUri? assetUri = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // data: Binary image data.
    // mimeType: MIME type of the image (e.g., "image/png", "image/jpeg").
    // alt: Alternative text description for accessibility.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, byte[]? data = null, string? mimeType = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
  static class InputExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: A controlled value with no onValueChange renders the field read-only.
    // autoSubmit: When true, onAutoSubmit fires once all characters are entered.
    static void OtpField(this UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string? label = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // index: Zero-based index of this slot in the OTP field.
    static void OtpFieldInput(this UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void PasswordToggleField(this UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string? label = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void PasswordToggleFieldIcon(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // autoComplete: Browser autocomplete hint (e.g., "current-password", "new-password").
    static void PasswordToggleFieldInput(this UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void PasswordToggleFieldToggle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // autoResize: When true, grows to fit content up to maxRows (default 6), then scrolls.
    // submitOnEnter: When true, Enter submits and Shift+Enter inserts a newline; default false (Ctrl/Cmd+Enter submits).
    // onSubmit: Receives the submitted value; prefer it over re-reading the bound reactive, which may lag (onValueChange is a separate round-trip).
    // clearOnSubmit: Defaults to true when onSubmit/onSubmitWithContext is set.
    // debounceMs: Throttles onValueChange round-trips (ms).
    // bind: Two-way binds a Reactive<T>, writing back on every keystroke; value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void TextArea(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, string? ariaLabel = null)
    // A controlled value: with no write-back handler (bind:, onValueChange:, or onSubmit:) is read-only — the rule every input component shares.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // onSubmit: Receives the submitted value on Enter; prefer it over re-reading the bound reactive, which may lag (onValueChange is a separate round-trip).
    // clearOnSubmit: Defaults to true only when onSubmit is set; without one Enter does not empty a bound field.
    // debounceMs: Throttles onValueChange round-trips (ms).
    // bind: Two-way binds a Reactive<T>, writing back on every keystroke; value: is ignored and onValueChange still fires.
    // multiline: Delegates to TextArea (MUI-style spelling); rows: alone implies it.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void TextField(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, bool? multiline = null, int? rows = null, string? ariaLabel = null)
  sealed record InteractOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Values match the browser KeyboardEvent.key specification; raw strings also work for keys not listed here.
  static class Key
    const string Alt
    const string ArrowDown
    const string ArrowLeft
    const string ArrowRight
    const string ArrowUp
    const string Backspace
    const string Control
    const string Delete
    const string End
    const string Enter
    const string Escape
    const string F1
    const string F10
    const string F11
    const string F12
    const string F2
    const string F3
    const string F4
    const string F5
    const string F6
    const string F7
    const string F8
    const string F9
    const string Home
    const string Meta
    const string PageDown
    const string PageUp
    const string Shift
    const string Space
    const string Tab
  // Property semantics match the browser KeyboardEvent.
  sealed record KeyboardEventArgs
    ctor(string Key, string Code, bool AltKey, bool CtrlKey, bool MetaKey, bool ShiftKey, bool Repeat)
    bool AltKey { get; init; }
    string Code { get; init; }
    bool CtrlKey { get; init; }
    string Key { get; init; }
    bool MetaKey { get; init; }
    bool Repeat { get; init; }
    bool ShiftKey { get; init; }
  static class KeyboardExtensions
    // keys: Only forward events for these key names (Key constants); null forwards all keys.
    // global: Default true: listens at document level; false listens only on the wrapper element.
    // requireCtrlOrMeta: When true, the client drops events without Ctrl or Cmd held — the filter every ⌘X-style shortcut needs. Filtering only in the server callback is not enough: preventDefault applies client-side to every matched key, so a bare-key listener with it swallows that letter in every text field of the app.
    // preventDefault: Prevents the default browser behavior for matched keys; pair with requireCtrlOrMeta for modifier shortcuts.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void KeyboardListener(this UIView view, Func<KeyboardEventArgs, Task>? onKeyDown = null, Func<KeyboardEventArgs, Task>? onKeyUp = null, IReadOnlyList<string>? keys = null, bool? global = true, bool? requireCtrlOrMeta = null, bool? preventDefault = null, bool? stopPropagation = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  static class LayoutExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // ratio: The width-to-height ratio to maintain (e.g., 16.0/9.0 for widescreen).
    static void AspectRatio(this UIView view, string[]? style = null, double ratio = 1.0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // dir: Text direction for descendants.
    static void DirectionProvider(this UIView view, string[]? style = null, Dir dir = Ltr, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb; rootStyle rarely needed.
    // threshold: Distance from end (in pixels) to trigger onNearEnd. Default 200.
    // debounceMs: Debounce time in ms to prevent rapid callback firing. Default 100.
    // loading: When true, shows loading indicator and prevents duplicate callbacks.
    // hasMore: When false, disables the onNearEnd callback (end of data reached).
    // direction: Whether to detect scroll near end going Down (append) or Up (prepend).
    static void InfiniteScrollView(this UIView view, string[]? style = null, int threshold = 200, int debounceMs = 100, bool loading = false, bool hasMore = true, ScrollDirection direction = Down, ScrollAreaScrollbars scrollbars = Vertical, Action<UIView>? loadingIndicator = null, Func<ScrollNearEndArgs, Task>? onNearEnd = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // SemanticTone.Success, SemanticTone.Warning, and SemanticTone.Error tones map to the matching Theming.Progress.Variant tokens; other tones use the default (brand) fill. Style slots: rootStyle → Progress.Root, indicatorStyle → Progress.Indicator.
    // indeterminate: When true, displays an indeterminate progress animation.
    static void Progress(this UIView view, string[]? style = null, double? value = null, double? max = null, SemanticTone tone = Neutral, bool indeterminate = false, Func<double?, string>? getValueLabel = null, string[]? rootStyle = null, string[]? indicatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Resize is handled entirely on the client — only the final size reaches the server via onResized.
    static void ResizableSplit(this UIView view, Orientation orientation = Horizontal, double initialSize = 200.0, double minSize = 100.0, double maxSize = 500.0, bool reversed = false, Func<double, Task>? onResized = null, Action<UIView>? first = null, Action<UIView>? second = null, string[]? style = null, string[]? handleStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb, cornerStyle (when both scrollbars show); rootStyle rarely needed.
    // scrollHideDelay: Delay in milliseconds before hiding scrollbars when type is Scroll or Hover.
    // autoScroll: When true, automatically scrolls to the bottom when content changes (chat-style).
    // autoScrollKey: Anything whose value changes when the content does — auto-scroll re-fires on change. Pass the collection itself (any reactive contributes its change version), a count, or a composite string. Required when autoScroll is true.
    static void ScrollArea(this UIView view, string[]? style = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType type = Hover, int? scrollHideDelay = null, Dir dir = Ltr, bool autoScroll = false, object? autoScrollKey = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? cornerStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void ScrollArea(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // orientation: Whether the separator is horizontal or vertical.
    // decorative: When true, the separator is purely visual and not announced by screen readers.
    static void Separator(this UIView view, string[]? style = null, Orientation orientation = Horizontal, bool decorative = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record LocationActionEvent : ActionEvent
    ctor(bool Success, double? Latitude, double? Longitude, double? Accuracy)
    double? Accuracy { get; init; }
    double? Latitude { get; init; }
    double? Longitude { get; init; }
  enum MediaCaptureButtonMode
    Hold
    Toggle
  // ClientContext identifies the initiating user and is populated for all capture kinds; prefer ClientSessionId/UserId over tracking streamId-to-client mappings yourself.
  sealed record MediaCaptureEvent
    ctor(string StreamId, MediaCaptureKind Kind)
    Context? ClientContext { get; init; }
    int? ClientSessionId { get; }
    MediaCaptureKind Kind { get; init; }
    string StreamId { get; init; }
    string? UserId { get; }
  enum MediaCaptureKind
    Audio
    Camera
    Screen
  static class MediaExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // url: URL of the audio source.
    // controls: When true, displays audio playback controls.
    // autoplay: When true, audio starts playing automatically.
    // loop: When true, audio loops continuously.
    // muted: When true, audio is muted.
    // preload: Specifies if/how the audio should be loaded when the page loads ("none", "metadata", or "auto").
    static void AudioUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, string? preload = null, string? styleId = null, string? key = null)
    // Pure presentation: renders the same loop whatever the microphone is doing (per-frame amplitude would cost a server round trip per frame). To reveal it only while capturing, give the row containing the mic button and the wave the group class and style the wave's container with Theming.MicButton.WhileCapturing — the reveal keys on the client-stamped data-ikon-capture-active attribute and lands on press, with no server involvement.
    // style: Crosswind/Tailwind utility classes merged over Theming.AudioWave.Root.
    // bars: How many bars to draw.
    // barStyle: Style for each bar. Defaults to Theming.AudioWave.Bar.
    static void AudioWave(this UIView view, string[]? style = null, int bars = 7, string[]? barStyle = null, string? key = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // text: Visible button text; becomes the aria-label when content is provided.
    // holdReleaseDelayMs: In Hold mode, keeps capturing this many milliseconds after release — speech users often release slightly before finishing.
    // content: When provided, enables icon mode: content is displayed and text becomes the aria-label.
    static void CaptureButton(this UIView view, string[]? style = null, MediaCaptureKind kind = Audio, string? text = null, MediaCaptureButtonMode captureMode = Hold, ClientAudioCaptureOptions? audioOptions = null, ClientVideoCaptureOptions? videoOptions = null, int? holdReleaseDelayMs = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Tap to open the microphone, tap again to close — the segment between is one utterance. After Audio.UseSpeechRecognition(...), subscribe to Audio.SpeechRecognizedAsync to receive the transcription when the mic is toggled off. Ships the MicButton.Default themed default: the button stays visibly red while the mic is open, via the zero-latency data-ikon-capture-active attribute. A custom style array replaces the default; start with "default" to layer, or include MicButton.Active.
    // text: Text or icon shown on the button.
    // audioOptions: Audio capture configuration options (auto gain, noise suppression, etc.).
    // onCaptureStart: Optional callback fired when the mic opens (audio path; race-free with frame handlers).
    // onCaptureStop: Optional callback fired when the mic closes.
    // content: Optional child content; when provided, text becomes the aria-label.
    static void MicToggleButton(this UIView view, string[]? style = null, string? text = "🎤", ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Enable speech recognition once via Audio.UseSpeechRecognition(...), then subscribe to Audio.SpeechRecognizedAsync to receive transcriptions when the button is released; the initiating user's client context is carried on the event args.
    // text: Text or icon shown on the button.
    // holdReleaseDelayMs: Delay before stopping capture after release. Useful for trailing-syllable tolerance.
    // audioOptions: Audio capture configuration options (auto gain, noise suppression, etc.).
    // onCaptureStart: Optional callback fired when capture begins (audio path; race-free with frame handlers).
    // onCaptureStop: Optional callback fired when capture ends.
    // content: Optional child content; when provided, text becomes the aria-label.
    static void PushToTalkButton(this UIView view, string[]? style = null, string? text = "⏺", int holdReleaseDelayMs = 500, ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // streamId: Identifier of the video stream to display.
    // width: Width of the canvas in pixels.
    // height: Height of the canvas in pixels.
    static void VideoStreamCanvas(this UIView view, string[]? style = null, string? streamId = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // url: URL of the video source.
    // controls: When true, displays video playback controls.
    // autoplay: When true, video starts playing automatically.
    // loop: When true, video loops continuously.
    // muted: When true, video is muted.
    // playsInline: When true, plays inline on mobile devices instead of fullscreen.
    // poster: URL of the poster image shown before playback.
    // width: Width of the video player in pixels.
    // height: Height of the video player in pixels.
    static void VideoUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, bool? playsInline = null, string? poster = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  static class NavigationExtensions
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // dir: Text direction (ltr/rtl).
    // loop: When true, keyboard navigation wraps.
    // onValueChange: Invoked when value changes.
    static void Menubar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // isChecked: Checked state for checkbox items.
    // onCheckedChange: Invoked when checked changes.
    static void MenubarCheckboxItem(this UIView view, string[]? style = null, CheckedState isChecked = Unchecked, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onCheckedChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // loop: When true, keyboard navigation wraps.
    // side: Which side content appears on.
    // align: Content alignment.
    // sideOffset: Pixel offset from anchor on the side axis.
    // alignOffset: Pixel offset from anchor on the align axis.
    // forceMount: When true, keeps content in DOM when hidden.
    static void MenubarContent(this UIView view, string[]? style = null, bool loop = true, Side side = Bottom, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // onSelect: Invoked when item is selected.
    static void MenubarItem(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // forceMount: When true, keeps content in DOM when hidden.
    static void MenubarItemIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: Controlled value identifying the active item.
    static void MenubarMenu(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // onValueChange: Invoked when value changes.
    static void MenubarRadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: Controlled value identifying the active item.
    static void MenubarRadioItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void MenubarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // onOpenChange: Invoked when open state changes.
    static void MenubarSub(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // loop: When true, keyboard navigation wraps.
    // side: Which side content appears on.
    // align: Content alignment.
    // sideOffset: Pixel offset from anchor on the side axis.
    // alignOffset: Pixel offset from anchor on the align axis.
    // forceMount: When true, keeps content in DOM when hidden.
    static void MenubarSubContent(this UIView view, string[]? style = null, bool loop = true, Side side = Right, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void MenubarSubTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void MenubarTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // orientation: Layout orientation.
    // delayDuration: Timing delay in milliseconds.
    // skipDelayDuration: Skip delay duration in milliseconds.
    // onValueChange: Invoked when value changes.
    static void NavigationMenu(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, int? delayDuration = null, int? skipDelayDuration = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // forceMount: When true, keeps content in DOM when hidden.
    static void NavigationMenuContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // forceMount: When true, keeps content in DOM when hidden.
    static void NavigationMenuIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: Controlled value identifying the active item.
    static void NavigationMenuItem(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // active: Whether item is marked as active.
    // onSelect: Invoked when item is selected.
    static void NavigationMenuLink(this UIView view, string[]? style = null, bool? active = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void NavigationMenuList(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void NavigationMenuTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // forceMount: When true, keeps content in DOM when hidden.
    static void NavigationMenuViewport(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // orientation: Layout orientation.
    // dir: Text direction (ltr/rtl).
    // loop: When true, keyboard navigation wraps.
    static void Toolbar(this UIView view, string[]? style = null, Orientation orientation = Horizontal, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // onClick: Invoked when the button is clicked.
    static void ToolbarButton(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // href: URL to navigate to.
    // target: Link target attribute.
    static void ToolbarLink(this UIView view, string[]? style = null, string? href = null, string? target = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    static void ToolbarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: Controlled value identifying the active items.
    // defaultValue: Initial value for uncontrolled mode.
    // rovingFocus: Whether roving focus is enabled.
    // loop: When true, keyboard navigation wraps.
    // onValueChange: Invoked when value changes.
    static void ToolbarToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // rovingFocus: Whether roving focus is enabled.
    // loop: When true, keyboard navigation wraps.
    // onValueChange: Invoked when value changes.
    static void ToolbarToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // value: Controlled value identifying the active item.
    static void ToolbarToggleItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  enum Orientation
    Horizontal
    Vertical
  // Each component manages its Portal/Overlay wrapper automatically.
  static class OverlayExtensions
    // Style slots: overlayStyle → AlertDialog.Overlay, contentStyle → AlertDialog.Content, titleStyle → AlertDialog.Title, descriptionStyle → AlertDialog.Description, footerStyle → AlertDialog.Footer, cancelStyle → AlertDialog.Cancel, actionStyle → AlertDialog.Action.
    // cancelLabel: Label for the cancel button. Defaults to "Cancel".
    // contentSlot: When provided, overrides the title/description/action parameters for full custom control.
    static void AlertDialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? title = null, string? description = null, string? cancelLabel = null, string? actionLabel = null, Func<Task>? onAction = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? cancelStyle = null, string[]? actionStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Style slots: overlayStyle → Dialog.Overlay, contentStyle → Dialog.Content.
    // modal: When true, prevents interaction with elements behind the dialog.
    static void Dialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? headerStyle = null)
    // Style slots: contentStyle → HoverCard.Content.
    // style: Alias for contentStyle — the first positional styles the floating content panel; contentStyle wins when both are given.
    static void HoverCard(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: contentStyle → Popover.Content.
    // style: Alias for contentStyle — the first positional styles the floating content panel; contentStyle wins when both are given.
    // modal: When true, prevents interaction with elements outside the popover.
    static void Popover(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: toastStyle → Toast.Default, viewportStyle → Toast.Viewport, titleStyle → Toast.Title, descriptionStyle → Toast.Description, closeStyle → Toast.Close.
    // durationMs: Duration in milliseconds before auto-dismiss.
    // forceMount: When true, keeps the toast in the DOM even when closed.
    // showClose: Whether to show the close button. Defaults to true with the simplified API.
    // closeLabel: Label for the close button. Defaults to "×".
    // content: When provided, overrides the title/description/close parameters for full custom control.
    // onPause: Invoked when the toast timer pauses (e.g., on hover).
    static void Toast(this UIView view, string[]? style = null, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string? title = null, string? description = null, bool? showClose = null, string? closeLabel = null, Action<UIView>? content = null, string[]? toastStyle = null, string[]? viewportStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Func<Task>? onEscapeKeyDown = null, Func<Task>? onPause = null, Func<Task>? onResume = null, Func<ToastSwipeArgs, Task>? onSwipeStart = null, Func<ToastSwipeArgs, Task>? onSwipeMove = null, Func<ToastSwipeArgs, Task>? onSwipeEnd = null, Func<ToastSwipeArgs, Task>? onSwipeCancel = null)
    // Style slots: contentStyle → Tooltip.Content.
    // style: Alias for contentStyle — the first positional styles the floating content bubble; contentStyle wins when both are given.
    // skipDelayDuration: Delay in milliseconds when switching between tooltips.
    // disableHoverableContent: When true, prevents hoverable content from keeping the tooltip open.
    static void Tooltip(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
  // C# composites over the Popover/Dialog primitives (no bespoke node type). Filtering is server-side over the app's reactive search state.
  static class OverlayMenuExtensions
    // Filtering is server-side: bind searchValue to a reactive and echo edits via onSearchChange for the list to narrow by case-insensitive label match. Without a bound search value it renders as a plain Popover-select (no filtering).
    // options: The full option set; the component filters it by searchValue.
    // value: The selected option's value (drives the trigger label and the check mark).
    // onValueChange: Fires with the chosen option's value.
    // searchValue: Current search text (bind to a reactive for live filtering).
    // onSearchChange: Fires as the user types in the search field.
    // open: Controlled open state; omit to let the popover self-manage.
    // onOpenChange: Fires when the panel opens or closes.
    // placeholder: Trigger text when nothing is selected.
    // searchPlaceholder: Placeholder in the search field.
    // emptyText: Shown when the filter matches no option.
    static void Combobox(this UIView view, IReadOnlyList<SelectOption> options, string? value = null, Func<string, Task>? onValueChange = null, string? searchValue = null, Func<string, Task>? onSearchChange = null, bool? open = null, Func<bool, Task>? onOpenChange = null, string? placeholder = "Select…", string? searchPlaceholder = "Search…", string? emptyText = "No results.", string[]? style = null, string[]? triggerStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Filtering is server-side over searchValue: each group narrows by case-insensitive label match and empty groups drop out. onSelect fires with the chosen option's value.
    // groups: Grouped actions; each option's Value is what onSelect receives.
    // open: Controlled open state of the dialog.
    // onOpenChange: Fires when the palette opens or closes.
    // onSelect: Fires with the selected option's value.
    // searchValue: Current search text (bind to a reactive for live filtering).
    // onSearchChange: Fires as the user types.
    // placeholder: Search-field placeholder.
    // emptyText: Shown when nothing matches.
    static void CommandPalette(this UIView view, IReadOnlyList<SelectOptionGroup> groups, bool? open = null, Func<bool, Task>? onOpenChange = null, Func<string, Task>? onSelect = null, string? searchValue = null, Func<string, Task>? onSearchChange = null, string? placeholder = "Type a command or search…", string? emptyText = "No results.", string[]? panelStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Fill content with view.Button([Menu.Item]) / [Menu.ItemDestructive] rows plus Menu.Label / Menu.Separator; the component supplies the trigger wiring and the menu-shaped popover panel.
    // trigger: The clickable element that opens the menu (usually a Button).
    // content: The menu body — item rows, labels, separators.
    // open: Controlled open state; omit to let the popover self-manage.
    // side: Which side of the trigger the panel opens on. Defaults to below.
    // align: Panel alignment along the trigger edge. Defaults to start (left).
    // contentStyle: Extra classes on the menu panel (defaults to the popover-menu surface).
    // onOpenChange: Fires when the menu opens or closes.
    static void DropdownMenu(this UIView view, Action<UIView> trigger, Action<UIView> content, bool? open = null, Side side = Bottom, Align align = Start, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Pass text for a single key, or keys for a combo (one chip per key); keys wins over text.
    // text: The single key/label to render (e.g. "⌘K", "Esc"). Ignored when keys is set.
    // keys: A combo rendered as one chip per key. Wins over text.
    // style: Extra classes layered on the Theming.Kbd.Default chip (or the group wrapper when keys is set).
    static void Kbd(this UIView view, string? text = null, IReadOnlyList<string>? keys = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record Page<T>
    // Items: The slice of Source for the current page.
    // Index: Zero-based current page index.
    // TotalPages: Total number of pages (always >= 1, even when Source is empty).
    // PageSize: Items per page (the configured page size, not necessarily Items.Count).
    // CanPrev: True if there is a previous page.
    // CanNext: True if there is a next page.
    // Prev: Action to bind to a Prev button's onClick. Decrements page; no-op at first.
    // Next: Action to bind to a Next button's onClick. Increments page; no-op at last.
    // JumpTo: Action that moves to a specific page (0-based). Clamps to valid range.
    // First: Action that jumps to page 0.
    // Last: Action that jumps to the last page.
    // Source: The full input list, if the caller wants the original.
    ctor(IReadOnlyList<T> Items, int Index, int TotalPages, int PageSize, bool CanPrev, bool CanNext, Func<Task> Prev, Func<Task> Next, Func<int, Task> JumpTo, Func<Task> First, Func<Task> Last, IReadOnlyList<T> Source)
    bool CanNext { get; init; }
    bool CanPrev { get; init; }
    Func<Task> First { get; init; }
    int Index { get; init; }
    IReadOnlyList<T> Items { get; init; }
    Func<int, Task> JumpTo { get; init; }
    Func<Task> Last { get; init; }
    Func<Task> Next { get; init; }
    int PageSize { get; init; }
    Func<Task> Prev { get; init; }
    IReadOnlyList<T> Source { get; init; }
    int TotalPages { get; init; }
  // Slices an in-memory list and returns the slice plus bound actions (Prev/Next/JumpTo/First/Last) the caller binds to whatever UI fits; holds zero rendering opinion. Most apps don't need pagination — live feeds and large lists are covered by ReactiveList<T> + ScrollArea(autoScroll: true) or virtualization; use this for a static list large enough to warrant explicit page navigation. For DB-backed pagination (load only the current page), drive a ClientReactive<T> page index directly in your data-loading code.
  static class PaginationExtensions
    // page must be a field-level ClientReactive<T>; each client sees its own page, and the returned slice is a snapshot read once, not a live view.
    // view: UIView (extension receiver — unused, present for fluency).
    // items: Source list. Read once; the slice is a snapshot, not a live view.
    // page: Per-client page index. Use a field-level ClientReactive<T> initialized to 0.
    // pageSize: Items per page (must be >= 1; clamped if not).
    static Page<T> Paginate<T>(this UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  sealed record PickContactsActionOptions : ActionOptions
    ctor()
    bool Multiple { get; init; }
  sealed record PointerDownOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  static class QrCodeExtensions
    // The QR code is generated server-side and rendered as an image.
    // size: Size of the QR code in pixels (default 256).
    static void QR(this UIView view, string[]? style = null, string? value = null, int size = 256, string? key = null)
  static class RichTextEditorExtensions
    // Values are HTML strings. A controlled value with no write-back handler (onValueChange or onSubmit) renders the editor read-only.
    // tools: Explicit toolbar contents; null shows a default toolbar.
    // maxRows: Rows before the content area scrolls.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // toolbarStyle: Toolbar slot; merges over RichTextEditor.Toolbar.
    // toolbarButtonStyle: Toolbar-button slot; merges over RichTextEditor.ToolbarButton.
    // contentStyle: Editable-content slot; merges over RichTextEditor.Content.
    // onSubmit: Invoked when the user presses Ctrl+Enter.
    static void RichTextEditor(this UIView view, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, IReadOnlyList<RichTextTool>? tools = null, bool? showToolbar = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? toolbarStyle = null, string[]? toolbarButtonStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  enum RichTextTool
    Bold
    Italic
    Underline
    Strikethrough
    Heading1
    Heading2
    Heading3
    Paragraph
    AlignLeft
    AlignCenter
    AlignRight
    BulletList
    NumberedList
    Blockquote
    Code
    Link
    ClearFormatting
    Undo
    Redo
  // Routes, tabs, modes, panel selections, "which dialog is open" — same shape, same primitives; intentionally minimal, with no URL coupling or rendering bias. For URL ↔ signal sync (browser bar, deep links, back/forward), use Navigation on the host app instead.
  static class RoutingExtensions
    // signal holds the active key (per-client); cases maps each known key to a render lambda. Falls back to fallback (or empty) when the active key isn't in the dictionary.
    static void Routed<T>(this UIView view, ClientReactive<T> signal, Dictionary<T, Action<UIView>> cases, Action<UIView>? fallback = null)
    // For the "button that activates a specific route/tab/mode" case: view.Button(text: "Open settings", onClick: view.Set(_route, "settings")).
    static Func<Task> Set<T>(this UIView view, ClientReactive<T> signal, T value)
  enum ScrollAreaScrollbars
    None
    Vertical
    Horizontal
    Both
  enum ScrollAreaType
    Auto
    Always
    Scroll
    Hover
  static class ScrollColumnExtensions
    // Header and footer stay pinned; the body scrolls. Canonical shape for dialogs, side panels, and chat layouts needing fixed chrome around an overflowing region; avoids the flex-1 ScrollArea that won't shrink inside a flex parent (the min-height: auto quirk). The outer height is the caller's responsibility — set it via style (e.g. "h-[82vh]") or let a flex-1 parent provide bounds.
    // autoScroll: Auto-scroll the body to the bottom when content changes.
    // autoScrollKey: Anything whose value changes when the content changes — the reactive collection itself, a count, or a composite string.
    // bodyStyle: Applied to the inner ScrollArea root.
    static void ScrollColumn(this UIView view, string[]? style = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType scrollType = Hover, bool autoScroll = false, object? autoScrollKey = null, string[]? bodyStyle = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string? styleId = null, string? key = null)
  enum ScrollDirection
    Down
    Up
  sealed record ScrollNearEndArgs
    ctor(double ScrollTop, double ScrollHeight, double ClientHeight, ScrollDirection Direction)
    double ClientHeight { get; init; }
    ScrollDirection Direction { get; init; }
    double ScrollHeight { get; init; }
    double ScrollTop { get; init; }
  static class SelectExtensions
    // Provide either options (a flat list) or groups — not both. An Input.* token passed as the Select's own style is ignored (with a dev warning) — the trigger is the field element and already carries the field theme; customize it through triggerStyle, where Select.Size tokens ([Select.Size.Sm] / [Select.Size.Lg], default medium) control sizing.
    // value: Controlled selected value. A controlled value with no write-back handler (no bind, no onValueChange) renders the select read-only.
    // label: Optional field label rendered above the select.
    // bind: Two-way binds the select to a Reactive<T> — reads bind.Value and writes it back on every selection. When set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for a control whose visible content cannot supply one; prefer a visible label.
    static void Select(this UIView view, string[]? style = null, IReadOnlyList<SelectOption>? options = null, IReadOnlyList<SelectOptionGroup>? groups = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, bool? required = null, bool? open = null, string? name = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? itemStyle = null, string[]? itemIndicatorStyle = null, string? indicatorIconName = "check", string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null, Reactive<string>? bind = null, string? ariaLabel = null)
  sealed record SelectOption
    ctor(string Value, string Label, bool Disabled = false)
    bool Disabled { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  sealed record SelectOptionGroup
    ctor(string? Label, IReadOnlyList<SelectOption> Options)
    string? Label { get; init; }
    IReadOnlyList<SelectOption> Options { get; init; }
  // Tones resolve to the theme's semantic color tokens, so they render correctly in both light and dark mode.
  enum SemanticTone
    Neutral
    Brand
    Success
    Warning
    Error
    Info
  readonly struct ShaderUniform
    string Type { get; }
    object Value { get; }
    static ShaderUniform Bool(bool value)
    static ShaderUniform Float(float value)
    static ShaderUniform Int(int value)
    static ShaderUniform Vec2(float x, float y)
    static ShaderUniform Vec3(float x, float y, float z)
    static ShaderUniform Vec4(float x, float y, float z, float w)
  static class ShadertoyExtensions
    // The shader source must define void mainImage(out vec4 color, in vec2 fragCoord). Built-in uniforms: iResolution (vec3: width, height, 1.0), iTime and iTimeDelta (float, seconds), iFrame (int), iMouse (vec4: x, y, click x, click y; requires enableMouse), iDate (vec4: year, month, day, seconds of day). Channel textures use Shadertoy's defaults (vertical flip on, repeat wrap, mipmap filtering); iChannelResolution[4] is 0 until a texture loads and iChannelTime[4] is always 0. Limitations: 2D image channels only — no cubemap, buffer, audio, or video — and single output.
    // style: Crosswind utility classes; lead with the "default" marker or a Theming.* composite to merge the component's themed default underneath.
    // shaderSource: Required — an empty source throws ArgumentException.
    // channels: Up to four image URLs (data URIs or http(s)) bound to iChannel0..iChannel3 in array order.
    static void ShadertoyCanvas(this UIView view, string[]? style = null, string? shaderSource = null, int? fps = null, IReadOnlyDictionary<string, ShaderUniform>? uniforms = null, IReadOnlyList<string>? channels = null, bool? enableMouse = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  sealed record ShareActionOptions : ActionOptions
    ctor()
    string? Text { get; init; }
    string? Title { get; init; }
    string? Url { get; init; }
  // The dialog's portal + content styling is repositioned per side via the Theming.Sheet / Theming.Drawer token recipes; slide-in/out motion is driven by the panel's data-state attribute.
  static class SheetExtensions
    // Same open/close model as Sheet: in controlled mode (open set) pass onOpenChange and flip your state to false there, or the drawer cannot be dismissed.
    // trigger: Builder for the element that opens the drawer (uncontrolled mode).
    static void Drawer(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showHandle = true, string[]? style = null, string[]? overlayStyle = null, string[]? handleStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string? key = null)
    // In controlled mode (open set) pass onOpenChange and flip your state to false there, or the close button and outside clicks cannot dismiss the sheet. Caller styles replace the themed panel token, or merge over it with a leading "default" marker.
    // trigger: Builder for the element that opens the sheet (uncontrolled mode).
    static void Sheet(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, Side side = Right, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showClose = true, string[]? style = null, string[]? overlayStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? closeStyle = null, string? key = null)
  enum Side
    Top
    Right
    Bottom
    Left
  static class SkeletonExtensions
    // The default fill for content redacted from the build-time boot snapshot (see SnapshotReveal). A typed convenience over the Skeleton.* theme tokens (a div with animate-pulse styling); size and shape via size / shape, or override freely through style.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes appended to the base skeleton styling (e.g. an explicit width).
    // shape: Outline shape — Rectangle (default), Circle, or Square.
    // size: Height preset — Xs, Sm, Md (default), Lg, or Xl.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Skeleton(this UIView view, string[]? style = null, SkeletonShape shape = Rectangle, SkeletonSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum SkeletonShape
    Rectangle
    Circle
    Square
  enum SkeletonSize
    Xs
    Sm
    Md
    Lg
    Xl
  // The boot snapshot is a public asset painted to everyone before the live connection, so by default the snapshot render replaces every content leaf with a skeleton — per-user content can never leak. These wrappers override that default for specific regions, branching on UIView.IsSnapshot so the app keeps a single UI.Root definition.
  static class SnapshotExtensions
    // Renders content live but omits it entirely from the boot snapshot — not even a skeleton placeholder.
    static void SnapshotHide(this UIView view, Action<UIView> content)
    // Renders content only in the boot snapshot, never live; the filler is rendered as authored (not auto-skeletonized).
    static void SnapshotOnly(this UIView view, Action<UIView> content)
    // Renders content as real content in the boot snapshot instead of skeletons — use only for content safe to bake into the public snapshot (logos, static chrome, marketing copy). The opt-out covers the whole subtree.
    static void SnapshotReveal(this UIView view, Action<UIView> content)
  enum SortStrategy
    VerticalList
    HorizontalList
  sealed record SortableReorderArgs
    ctor(string ActiveId, string OverId, int OldIndex, int NewIndex, IReadOnlyList<string> NewOrder)
    string ActiveId { get; init; }
    int NewIndex { get; init; }
    IReadOnlyList<string> NewOrder { get; init; }
    int OldIndex { get; init; }
    string OverId { get; init; }
  enum SpinnerSize
    Sm
    Md
    Lg
  enum StatTrend
    // The delta renders in a neutral tone without an arrow.
    Flat
    // Trending-up arrow in the success tone.
    Up
    // Trending-down arrow in the error tone.
    Down
  enum Sticky
    Partial
    Always
  record TabItem
    // Value: Unique identifier for the tab.
    // Label: Text label displayed on the tab trigger.
    // Content: Builder function for rendering the tab's content panel.
    // Disabled: When true, prevents user interaction with this tab.
    // ForceMount: When true, the tab's content is mounted in the DOM even when inactive (Radix hides via data-state="inactive"). Use this for heavy panels you want to amortise into initial paint and keep mounted across tab switches; the trade-off is a slower first render and any mount-time effects firing on hidden panels.
    ctor(string Value, string Label, Action<UIView> Content, bool Disabled = false, bool ForceMount = false)
    Action<UIView> Content { get; init; }
    bool Disabled { get; init; }
    bool ForceMount { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  // The styled middle ground between hand-rolled Grid/Row layouts and the payload-driven DataTable component. CSS table display utilities align columns automatically without a shared grid template. Compose Table > TableHeader/TableBody > TableRow > TableHead/TableCell.
  static class TableExtensions
    // Caller styles replace the base token; lead the array with "default" to merge over it.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the table base token.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for the table's header/body groups.
    static void Table(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Table(this UIView view, string[]? style, Action<UIView> children)
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the row group token.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for the body rows.
    static void TableBody(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the cell token.
    // text: Cell text. For custom content use content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for custom cell content.
    static void TableCell(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void TableCell(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the header cell token.
    // text: Column label. For custom content use content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for custom cell content.
    static void TableHead(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void TableHead(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the header group token.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for the header rows.
    static void TableHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Rows with onClick also get hover highlight + pointer cursor.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the row token.
    // striped: When true, even rows get a subtle background (zebra striping via CSS :nth-child).
    // onClick: Invoked when the user clicks the row. Accepts sync (() => …) and async (async () => …) lambdas alike.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for the row's cells.
    static void TableRow(this UIView view, string[]? style = null, bool striped = false, Delegate? onClick = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
  static class TabsExtensions
    // Style slots (default theme tokens): listStyle → Tabs.List, triggerStyle → Tabs.Trigger, contentStyle → Tabs.Content; rootStyle is the outer container (rarely needed).
    // listContainerStyle: When provided, wraps the TabsList in a styled Box.
    // disabledTriggerStyle: Style for disabled tab triggers; falls back to triggerStyle.
    // contentContainerStyle: When provided, wraps all content panels in a styled Box.
    // lazyPanels: When true (controlled tabs only), the server builds only the active tab's panel plus any TabItem.ForceMount panels; a switch fetches the new panel in the same round-trip that confirms it, and the client keeps the old panel visible until it arrives. Cuts per-client server memory and wire size by roughly the tab count at the cost of one round-trip per switch. Default false: every panel ships and switching is instant. Ignored for uncontrolled tabs (they switch client-side).
    static void Tabs(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, ActivationMode activationMode = Automatic, IEnumerable<TabItem>? tabs = null, string[]? listContainerStyle = null, string[]? listStyle = null, string[]? triggerStyle = null, string[]? disabledTriggerStyle = null, string[]? contentContainerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, bool lazyPanels = false)
  enum TimeGranularity
    Hour
    Minute
    Second
  static class TimePickerExtensions
    // Values are ISO-8601 HH:mm or HH:mm:ss strings; the emitted value is always 24-hour regardless of hourFormat. A controlled value without onValueChange renders read-only.
    // minuteStep: Minute step (5, 10, 15, 30…); defaults to 1.
    // secondStep: Second step; defaults to 1.
    // label: Optional field label rendered above the picker.
    static void TimePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, HourFormat hourFormat = Hour24, TimeGranularity granularity = Minute, int? minuteStep = null, int? secondStep = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string? placeholder = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? columnStyle = null, string[]? itemStyle = null, string[]? itemSelectedStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  sealed record ToastItem
    // Id: Queue-unique identifier used to dismiss the toast.
    // Title: Headline text.
    // Description: Optional muted body text.
    // Tone: Semantic tone controlling the icon and its color.
    // DurationMs: Milliseconds before the client auto-dismisses the toast.
    ctor(long Id, string Title, string? Description, SemanticTone Tone, int DurationMs)
    string? Description { get; init; }
    int DurationMs { get; init; }
    long Id { get; init; }
    string Title { get; init; }
    SemanticTone Tone { get; init; }
  sealed record ToastSwipeArgs
    ctor(ToastSwipeDirection Direction, double DeltaX, double DeltaY)
    double DeltaX { get; init; }
    double DeltaY { get; init; }
    ToastSwipeDirection Direction { get; init; }
  enum ToastSwipeDirection
    Left
    Right
    Up
    Down
  enum ToastType
    Foreground
    Background
  // Wiring: construct one instance as an app field, mount ToastsExtensions.ToastHost once in the root UI, then fire notifications (e.g. _toasts.Success(...)) from any handler. State lives in a ClientReactive<T>, so methods must be called where a client scope is active (UI render or event handlers) and each client sees only its own toasts. Auto-dismiss is client-driven off ToastItem.DurationMs.
  sealed class Toasts
    ctor()
    IReadOnlyList<ToastItem> Items { get; }
    void Clear()
    void Dismiss(long id)
    long Error(string title, string? description = null, int durationMs = 5000)
    long Info(string title, string? description = null, int durationMs = 5000)
    // title: Headline text.
    // description: Optional muted body text.
    // tone: Semantic tone controlling the icon and its color.
    // durationMs: Milliseconds before the client auto-dismisses the toast.
    long Show(string title, string? description = null, SemanticTone tone = Neutral, int durationMs = 5000)
    long Success(string title, string? description = null, int durationMs = 5000)
    long Warning(string title, string? description = null, int durationMs = 5000)
    const int DefaultDurationMs = 5000
  static class ToastsExtensions
    // Mount exactly once in the root UI; every queued toast renders as a themed toast (tone icon, title, description, close button) that the client auto-dismisses after its duration. Both auto-dismiss and the close button report back and remove the item from the queue.
    // view: The UIView to render into.
    // toasts: The queue to render.
    // viewportStyle: Style for the toast viewport. Defaults to Theming.Toast.Viewport.
    // toastStyle: Crosswind/Tailwind utility classes merged on top of Theming.Toast.Default for each toast.
    // titleStyle: Style for the title. Defaults to Theming.Toast.Title.
    // descriptionStyle: Style for the description. Defaults to Theming.Toast.Description.
    // closeStyle: Style for the close button. Defaults to Theming.Toast.Close.
    // showClose: Whether to render the × close button on each toast.
    static void ToastHost(this UIView view, Toasts toasts, string[]? viewportStyle = null, string[]? toastStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, bool showClose = true)
  static class TreeViewExtensions
    // Expansion state lives in a caller-held ExpandedSet — declare it as an app field (private readonly ExpandedSet _expanded = new();). Clicking a branch toggles its expansion and selects it in the same click.
    // id: Stable unique id per node — used for diff keys, expansion, and selection.
    // children: Child nodes per node; null or empty marks a leaf.
    // style: Merged on top of Theming.NavPanel.Ghost for the tree container.
    // icon: Optional per-node Lucide icon name rendered before the label.
    // itemStyle: Row style; defaults to Theming.NavItem.Md + Theming.NavItem.Default.
    // selectedItemStyle: Selected-row style; defaults to Theming.NavItem.Md + Theming.NavItem.Active.
    static void TreeView<T>(this UIView view, IReadOnlyList<T> roots, Func<T, string> id, Func<T, string> label, Func<T, IReadOnlyList<T>?> children, ExpandedSet expanded, string[]? style = null, Func<T, Task>? onSelect = null, string? selectedId = null, Func<T, string?>? icon = null, string[]? itemStyle = null, string[]? selectedItemStyle = null, string[]? labelStyle = null, string[]? childrenStyle = null, string? styleId = null, string? key = null)
  // Performance model: the server emits one wrapper node per item up to itemCount and runs every per-item content builder eagerly server-side (keep content trees inexpensive); the client mounts only the wrappers inside [start - overscan, end + overscan] and leaves the rest out of the DOM entirely. onNearEnd fires when the window enters the last nearEndThreshold rows — append items to grow the list.
  static class VirtualListExtensions
    // columns: Fixed number of columns; ignored when minItemWidthPx is set.
    // rowHeight: Fixed row height in pixels; ignored when aspectRatio is set.
    // overscan: Extra rows to render before/after the visible window. Default 2.
    // gap: Gap in pixels between rows and between columns. Default 12.
    // minItemWidthPx: When set, column count is computed from container width / minItemWidthPx, overriding columns.
    // maxColumns: Upper bound on auto-computed columns (with minItemWidthPx).
    // aspectRatio: Row height = column width × aspectRatio (1.0 = square, 0.75 = 4:3 landscape, 1.4 = portrait card); overrides rowHeight.
    // resetScrollKey: Opaque token that resets scroll to the top whenever it changes (e.g. on filter/sort changes) without remounting the grid.
    // onNearEnd: Fires when scrolled within nearEndThresholdRows rows of the end.
    // nearEndThresholdRows: Distance from end (in rows) to trigger onNearEnd. Default 2.
    static void VirtualGrid(this UIView view, int itemCount, int columns, double rowHeight, Action<UIView, int> onRenderItem, int overscan = 2, int gap = 12, int? minItemWidthPx = null, int? maxColumns = null, double? aspectRatio = null, string? resetScrollKey = null, Func<int, Task>? onNearEnd = null, int nearEndThresholdRows = 2, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // view: The UIView to render into.
    // itemCount: Total number of items in the list. Wrapper divs are emitted for all of them.
    // itemHeight: Fixed height in pixels for every item. Required for windowing math.
    // onRenderItem: Callback invoked per item with its zero-based index. Builds the item content.
    // overscan: Extra items to render before/after the visible window. Default 4.
    // onNearEnd: Fires when the user scrolls within nearEndThreshold items of the end. Use to fetch more data and grow itemCount.
    // nearEndThreshold: Distance from end (in items) to trigger onNearEnd. Default 5.
    // style: Style for the outermost scrollable viewport container.
    // itemStyle: Style applied to each item wrapper. Use sparingly — wrappers are sized by itemHeight.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    // props: Additional properties passed directly to the underlying component.
    static void VirtualList(this UIView view, int itemCount, double itemHeight, Action<UIView, int> onRenderItem, int overscan = 4, Func<int, Task>? onNearEnd = null, int nearEndThreshold = 5, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum WeekStart
    Sunday
    Monday

namespace Ikon.Parallax.Theming
  static class Accessibility
    static string RequiredLabel(string baseLabel)
    const string NotScreenReaderOnly
    const string ScreenReaderOnly
    const string SkipLink
  static class Accessibility.Aria
    const string Busy
    const string Checked
    const string CurrentPage
    const string CurrentStep
    const string Disabled
    const string Expanded
    const string Invalid
    const string Required
    const string Selected
  static class Accessibility.Focus
    const string HighContrast
    const string None
    const string Sentinel
    const string Within
  static class Accessibility.Motion
    const string Reduce
    const string ReduceFade
    const string Respectful
    const string Safe
  static class Accordion
    const string ChevronIcon
    const string Content
    const string ContentInner
    const string Default
    const string Header
    const string Item
    const string Root
    const string Trigger
  static class Alert
    const string Base
    const string Default
    const string Description
    const string Error
    const string Info
    const string Success
    const string Title
    const string Warning
  static class Alert.Variant
    const string Default
    const string Error
    const string Info
    const string Success
    const string Warning
  static class AlertDialog
    const string Action
    const string Cancel
    const string Content
    const string Default
    const string Description
    const string Footer
    const string Header
    const string Overlay
    const string Title
  static class AspectRatio
    const string Base
    const string Default
    const string PlaceholderContent
  static class AspectRatio.Ratio
    const string Photo
    const string Portrait
    const string Square
    const string Video
    const string Wide
  static class AudioWave
    const string Bar
    // Cycled across however many bars are asked for. Uneven on purpose: an even ramp reads as a loading spinner rather than a level meter.
    static readonly int[] Heights
    const string Root
  static class Avatar
    const string Base
    const string Default
    const string Fallback
    const string Image
    const string Root
  static class Avatar.Shape
    const string Circle
    const string Square
  static class Avatar.Size
    const string Lg
    const string Md
    const string Sm
    const string Xl
    const string Xl2
    const string Xs
  static class Badge
    const string Base
    const string Brand
    const string BrandLg
    const string BrandMd
    const string BrandSm
    const string Default
    const string DefaultLg
    const string DefaultMd
    const string DefaultSm
    const string Error
    const string ErrorLg
    const string ErrorMd
    const string ErrorSm
    const string IconLeft
    const string IconRight
    const string Info
    const string InfoLg
    const string InfoMd
    const string InfoSm
    const string Neutral
    const string NeutralLg
    const string NeutralMd
    const string NeutralSm
    const string OutlineBrand
    const string OutlineBrandLg
    const string OutlineBrandMd
    const string OutlineBrandSm
    const string OutlineError
    const string OutlineErrorLg
    const string OutlineErrorMd
    const string OutlineErrorSm
    const string OutlineInfo
    const string OutlineInfoLg
    const string OutlineInfoMd
    const string OutlineInfoSm
    const string OutlineNeutral
    const string OutlineNeutralLg
    const string OutlineNeutralMd
    const string OutlineNeutralSm
    const string OutlineSuccess
    const string OutlineSuccessLg
    const string OutlineSuccessMd
    const string OutlineSuccessSm
    const string OutlineWarning
    const string OutlineWarningLg
    const string OutlineWarningMd
    const string OutlineWarningSm
    const string Success
    const string SuccessLg
    const string SuccessMd
    const string SuccessSm
    const string Warning
    const string WarningLg
    const string WarningMd
    const string WarningSm
  static class Breadcrumb
    const string Ellipsis
    const string Item
    const string Link
    const string List
    const string Page
    const string Root
    const string Separator
  static class Button
    const string Base
    const string Default
    const string DefaultLg
    const string DefaultMd
    const string DefaultSm
    const string Error
    const string ErrorLg
    const string ErrorMd
    const string ErrorSm
    const string Ghost
    const string GhostLg
    const string GhostMd
    const string GhostSm
    const string Icon
    const string IconLeft
    const string IconRight
    const string IconSm
    const string IconXs
    const string Info
    const string InfoLg
    const string InfoMd
    const string InfoSm
    const string Link
    const string LinkLg
    const string LinkMd
    const string LinkSm
    const string Neutral
    const string NeutralLg
    const string NeutralMd
    const string NeutralSm
    const string Outline
    const string OutlineLg
    const string OutlineMd
    const string OutlineSm
    const string Primary
    const string PrimaryLg
    const string PrimaryMd
    const string PrimarySm
    const string Secondary
    const string SecondaryLg
    const string SecondaryMd
    const string SecondarySm
    const string SolidLg
    const string SolidMd
    const string SolidSm
    const string Success
    const string SuccessLg
    const string SuccessMd
    const string SuccessSm
    const string Warning
    const string WarningLg
    const string WarningMd
    const string WarningSm
  static class Button.Size
    const string Lg
    const string Md
    const string Sm
  static class Calendar
    const string Day
    const string DayDisabled
    const string DayOutside
    const string DaySelected
    const string DayToday
    const string Default
    const string Grid
    const string Header
    const string HeaderTitle
    const string NavButton
    const string Root
    const string Row
    const string Weekday
  static class Card
    const string Base
    const string Content
    const string Default
    const string Description
    const string Elevated
    const string Flat
    const string Footer
    const string Ghost
    const string Glass
    const string GlassSubtle
    const string Header
    const string HeaderRow
    const string Interactive
    const string InteractiveFill
    const string Outline
    const string Selected
    const string Strong
    const string Subtle
    const string Title
  static class Carousel
    const string Default
    const string Indicator
    const string IndicatorActive
    const string Indicators
    const string NavButton
    const string Next
    const string Previous
    const string Root
    const string Slide
    const string Track
    const string TrackVertical
    const string Viewport
  static class Chart
    const string Container
    const string ContainerLg
    const string ContainerMd
    const string ContainerSm
    const string ContainerXl
    const string Default
  static class Checkbox
    const string Default
    const string Indicator
    const string Root
  static class CodeEditor
    const string Body
    const string Content
    const string Default
    const string Gutter
    const string Header
    const string LanguageBadge
    const string Line
    const string Root
  static class Collapsible
    const string Content
    const string Default
    const string Root
    const string Trigger
    const string TriggerIcon
  static class ColorPicker
    const string AlphaTrack
    const string Content
    const string Default
    const string HexInput
    const string HueThumb
    const string HueTrack
    const string PresetSwatch
    const string PresetsGrid
    const string SaturationArea
    const string Swatch
    const string SwatchLg
    const string SwatchSm
    const string Thumb
    const string Trigger
  static class Combobox
    const string Content
    const string Empty
    const string Item
    const string ItemSelected
    const string List
    const string Search
    const string Trigger
  static class Command
    const string Default
    const string Dialog
    const string Empty
    const string Group
    const string GroupHeading
    const string Input
    const string InputWrapper
    const string Item
    const string List
    const string Root
    const string Separator
    const string Shortcut
  static class CommandPalette
    const string Empty
    const string GroupLabel
    const string Item
    const string List
    const string Panel
    const string Search
  static class Container
    const string Full
    const string Lg
    const string Md
    const string Prose
    const string Screen
    const string Sm
    const string Xl
    const string Xl2
    const string Xl3
    const string Xl4
    const string Xl5
    const string Xl6
    const string Xl7
    const string Xs
  static class ContentGrid
    const string Bordered
    const string Cell
    const string CellMuted
    const string Default
    const string Header
  static class DataTable
    const string Cell
    const string DataCell
    const string Default
    const string EmptyState
    const string Header
    const string HeaderCell
    const string PageNumber
    const string PageNumberActive
    const string Pagination
    const string PaginationButton
    const string ResizeHandle
    const string Row
    const string RowClickable
  static class DatePicker
    const string Content
    const string Default
    const string Trigger
    const string TriggerLg
    const string TriggerSm
  static class Dialog
    const string CloseButton
    const string Content
    const string Description
    const string Footer
    const string Header
    const string Overlay
    const string Title
  static class DragDrop
    const string Container
    const string ContainerHorizontal
    const string DropZone
    const string DropZoneActive
    const string Overlay
    const string OverlayContent
  static class DragDrop.Droppable
    const string Base
    const string Default
    const string Disabled
    const string Info
    const string Success
  static class DragDrop.Item
    const string Base
    const string Dashed
    const string Default
    const string Disabled
    const string Dragging
  static class Drawer
    const string Content
    const string Default
    const string Description
    const string Footer
    const string Handle
    const string Header
    const string Overlay
    const string Title
  static class Drawer.Snap
    const string Full
    const string Half
    const string Quarter
    const string ThreeQuarter
  static class DropdownMenu
    const string CheckboxItem
    const string Content
    const string Group
    const string Item
    const string Label
    const string RadioItem
    const string Separator
    const string Shortcut
    const string SubContent
    const string SubTrigger
  static class EmptyState
    const string Actions
    const string Description
    const string IconSize
    const string IconSizeSm
    const string IconWrap
    const string IconWrapSm
    const string IllustrationSize
    const string IllustrationWrap
    const string IllustrationWrapSm
    const string Root
    const string RootFull
    const string RootSm
    const string Title
  static class FeedScroller
    const string Default
    const string MuteToggle
    const string Root
    const string Slide
    const string SlideMedia
    const string SlideOverlay
  static class FileUpload
    const string FileItem
    const string FileList
    const string FileName
    const string FileSize
    const string RemoveButton
    const string TypeIcon
  static class FileUpload.Icon
    const string Base
    const string Brand
    const string Disabled
    const string Error
    const string Info
    const string Neutral
    const string Success
    const string Warning
  static class FileUpload.Zone
    const string Active
    const string ActiveRing
    const string Base
    const string Code
    const string Compact
    const string Default
    const string Disabled
    const string Documents
    const string DragOverlay
    const string Images
    const string Wrapper
  // The rhythm is load-bearing, not decoration: Root binds a field's own label and support text to its control at 8px, and Group separates whole fields at 20px, so a field group reads as one thing rather than the form reading as one undifferentiated column. Wrap help and error text in Support — the two share that one reserved line, so a validation message appearing does not push the rest of the form down.
  static class FormField
    const string ErrorText
    const string Group
    const string HelpText
    const string Label
    const string LabelRequired
    const string ParamRow
    const string Root
    const string SuccessText
    const string Support
    const string WarningText
  static class HoverCard
    const string Content
    const string Default
  interface ITheme
    string Css { get; }
    string DefaultIconLibrary { get; }
  static class Icon
    const string Default
    const string Lg
    const string Md
    const string Sm
    const string Spinner
    const string SpinnerLg
    const string SpinnerSm
    const string Xl
    const string Xs
  // A key/value override map on top of the Ikon CSS baseline. Keys are a vocabulary alias (ThemeVocabulary, e.g. primary, card, radius), a CSS variable name without the leading --, or a Tailwind token; values are Crosswind/Tailwind classes or raw CSS. Set entries via the indexer during object initialization; pair DarkMode for the dark scheme.
  sealed class IkonTheme : ITheme
    ctor()
    // Valid only in ThemeMode.Adaptive mode; combining it with ThemeMode.Fixed throws InvalidOperationException at render time.
    IkonTheme? DarkMode { get; init; }
    string this[string token] { get; set; }
    ThemeMode Mode { get; init; }
  static class ImageCard
    const string Caption
    const string Image
    const string Root
    const string Title
  static class ImageCard.Hover
    const string Dim
    const string Zoom
  static class ImageCard.Overlay
    const string Center
    const string Dim
    const string Reveal
  static class Input
    const string Base
    const string Default
    const string DefaultLg
    const string DefaultSm
    const string Error
    const string ErrorLg
    const string ErrorSm
    const string Ghost
    const string GhostLg
    const string GhostSm
    const string Invalid
    const string InvalidLg
    const string InvalidSm
    const string Success
    const string SuccessLg
    const string SuccessSm
    const string Warning
    const string WarningLg
    const string WarningSm
  static class Input.Password
    const string Input
    const string Toggle
    const string Wrapper
  static class Interaction
    const string HoverCard
    const string HoverGlow
    const string HoverLift
  static class Kbd
    const string Default
    const string Group
  static class Label
    const string Base
    const string Default
    const string Error
    const string Optional
    const string Required
  static class Layout
    const string Center
    const string Page
    const string RowWrap
    const string Section
    const string SectionBody
    const string SectionHeader
    const string Stretch
  static class Layout.Column
    const string Center
    const string Default
    const string Lg
    const string Md
    const string Sm
    const string Xl
    const string Xs
  static class Layout.Grid
    const string Cols2
    const string Cols3
    const string Cols4
  static class Layout.Row
    const string Default
    const string InlineCenter
    const string Lg
    const string Md
    const string Sm
    const string SpaceBetween
    const string Xl
    const string Xs
  static class Layout.Split
    const string Detail
    const string DetailLg
    const string Gapped
    const string Main
    const string Root
    const string Sidebar
    const string SidebarLg
    const string SidebarSm
  // The Crosswind preflight zeroes borders and spacing on every element, so a markdown document with no token renders as undifferentiated prose — tables without rules, blockquotes without a bar, fenced code indistinguishable from a paragraph. Deliberately sets no base color, size or width: markdown is embedded in a page that has already chosen those. Anchors are styled by the renderer itself.
  static class Markdown
    const string Blocks
    const string Code
    const string Default
    const string Headings
    const string Table
  static class Media
    const string CanvasFill
    const string Default
    const string EmptyState
    const string Fill
    const string ImageEmptyState
    const string Mirror
    const string PlaceholderHint
    const string PlaceholderIcon
    const string PlaceholderText
    const string VideoContainer
  // A menu row is NOT a button look: it rests transparent, fills the row, reads left, and highlights on hover — these are complete default-marked composites for view.Button, not additions to the Button tones. Selection/active state stays a caller concern (add bg-brand-selected on the active row).
  static class Menu
    const string Content
    const string Item
    const string ItemDestructive
    const string Label
    const string Separator
    const string Shortcut
  static class Menubar
    const string Content
    const string Default
    const string Item
    const string Root
    const string Separator
    const string Trigger
  // A mic button must always show its live state: Active keys on the client-stamped data-ikon-capture-active attribute, so recording feedback flips the moment capture starts, with no server round trip. Compose Active into any custom mic style so recording never becomes invisible.
  static class MicButton
    const string Active
    const string Base
    const string Default
    const string Lg
    const string Md
    const string Sm
    // Reveals its element only while a capture button inside the same group is held; like Active it keys on the client-stamped attribute, so it lands on press rather than a round trip later. Put group on the row containing both the button and this element; pair with AudioWave for the recording cue.
    const string WhileCapturing
  static class NavItem
    const string Active
    const string ActiveAccent
    const string ActiveBrand
    const string ActiveSubtle
    const string Count
    const string Default
    const string Icon
    const string Label
    const string Lg
    const string Md
    const string Sm
    const string Subtle
  static class NavPanel
    const string Base
    const string Border
    const string Divided
    const string Filled
    const string Ghost
  static class NavSection
    const string Divider
    const string Label
    const string Root
  static class NavigationMenu
    const string Content
    const string ContentNarrow
    const string ContentPopover
    const string ContentPopoverSide
    const string ContentWide
    const string Default
    const string Indicator
    const string Link
    const string LinkCompact
    const string List
    const string ListVertical
    const string Root
    const string Trigger
    const string TriggerDisabled
    const string TriggerIcon
    const string TriggerIconRotate180
    const string TriggerIconRotate90
    const string TriggerVertical
    const string Viewport
  static class OnSurface.Card
    const string Caption
    const string Muted
    const string Subtle
    const string Text
  static class OnSurface.Default
    const string Caption
    const string Muted
    const string Subtle
    const string Text
  static class OnSurface.Popover
    const string Caption
    const string Muted
    const string Subtle
    const string Text
  static class OtpField
    const string Default
    const string Input
    const string Root
  static class Page
    const string Base
    const string Default
    const string Plain
  static class Pagination
    const string Active
    const string Disabled
    const string Ellipsis
    const string Item
    const string List
    const string Next
    const string Previous
    const string Root
  static class Panel
    const string Fill
    const string Side
    const string Sidebar
    const string SidebarNarrow
    const string Wide
  static class Popover
    const string Content
    const string Default
  static class Progress
    // Composes the indicator class list from the base recipe, a fill variant (Variant, defaulting to the brand fill), the optional indeterminate shimmer, and caller overrides appended last so they win.
    static string ComposeIndicator(string? variant = null, bool indeterminate = false, params string?[] overrides)
    // Arbitrary-value transform class that fills the indicator to value percent (clamped to 0–100) by translating it left from the fully-filled position.
    static string IndicatorTransform(double value)
    const string Base
    const string Default
    const string Indeterminate
    const string Indicator
    const string IndicatorBase
    const string Label
    const string Root
    const string Value
  static class Progress.Size
    const string Lg
    const string Md
    const string Sm
    const string Xs
  static class Progress.Variant
    const string Default
    const string Error
    const string Success
    const string Warning
  static class RadioGroup
    const string Default
    const string Indicator
    const string Item
    const string Root
    const string RootHorizontal
  static class ResizableSplit
    const string FirstPane
    const string FirstPaneVertical
    const string Handle
    const string HandleVertical
    const string Root
    const string SecondPane
    const string SecondPaneVertical
  static class Responsive
    const string CenterToEnd
    const string CenterToLeft
    const string CenterToSpaceBetween
    const string CenterToStart
    const string ColToRow
    const string ColToRowMd
    const string HiddenDesktop
    const string HiddenMobile
    const string HiddenTablet
    const string LeftToCenter
    const string RowToCol
    const string VisibleMobile
    const string VisibleTablet
  static class RichTextEditor
    const string Content
    const string Default
    const string Root
    const string Toolbar
    const string ToolbarButton
    const string ToolbarSeparator
  static class ScrollArea
    const string Bordered
    const string Default
    const string Root
    const string Scrollbar
    const string Thumb
    const string Viewport
  // Use for a header strip, a horizontal chip row, anything an overlay-based ScrollArea would over-serve — a bare overflow-auto shows the OS scrollbar, which matches no theme and can move the layout when it appears. The rules cover Firefox and WebKit alike, and both axes are sized on purpose: a width alone leaves the horizontal bar at its default height.
  static class Scrollbar
    const string Default
    // Only for a strip whose overflow is obvious from its content (a carousel, a chip row that visibly cuts off) — content that scrolls with nothing to say so is content most people never find.
    const string Hidden
    const string Thin
  static class Select
    const string Content
    const string Default
    const string Item
    const string ItemIndicator
    const string Label
    const string ScrollButton
    const string Separator
    const string Trigger
    const string TriggerBase
  static class Select.Group
    const string Label
    const string Root
  static class Select.Size
    const string Lg
    const string Md
    const string Sm
  static class Separator
    const string Base
    const string Horizontal
    const string Vertical
  static class Separator.Orientation
    const string Horizontal
    const string Vertical
  static class Separator.Variant
    const string Default
    const string Strong
    const string Subtle
  static class Sheet
    const string Base
    const string CloseButton
    const string Default
    const string Description
    const string Footer
    const string Header
    const string Overlay
    const string Title
  static class Sheet.Side
    const string Bottom
    const string Left
    const string Right
    const string Top
  static class Skeleton
    const string Avatar
    const string AvatarLg
    const string AvatarSm
    const string Base
    const string Button
    const string Card
    const string Default
    const string Input
    const string Text
    const string TextLg
    const string TextSm
  static class Skeleton.Shape
    const string Circle
    const string Rectangle
    const string Square
  static class Skeleton.Size
    const string Lg
    const string Md
    const string Sm
    const string Xl
    const string Xs
  static class Slider
    const string Default
    const string Range
    const string Root
    const string RootVertical
    const string Thumb
    const string Track
    const string TrackVertical
  static class StatCard
    const string Header
    const string IconBox
    const string IconBoxBrand
    const string IconBoxError
    const string IconBoxInfo
    const string IconBoxSuccess
    const string IconBoxWarning
    const string IconSize
    const string Label
    const string Root
    const string Trend
    const string TrendIcon
    const string TrendLabel
    const string TrendValue
    const string Value
    const string ValueRow
  static class StatCard.TrendVariant
    const string Negative
    const string Neutral
    const string Positive
  static class State
    const string Checked
    const string Disabled
    const string Empty
    const string Focusable
    const string Indeterminate
    const string Invalid
    const string Loading
    const string Pending
    const string Pressable
    const string Readonly
    const string Selected
    const string Success
    const string Validating
    const string Warning
  static class Switch
    const string Default
    const string Root
    const string Thumb
  // List/Trigger are the SEGMENTED control — mutually exclusive parallel values of one setting (Day/Week/Month, List/Grid), equal-width, the active one filled. NavList with NavTrigger* are page NAVIGATION between peer panels (Overview/Activity/Files): each tab hugs its label, the row sits flush on a shared rail, and the active tab is marked by the rail indicator plus a weight change, never a fill. Choose by meaning, not by tab count or width — navigation rendered as filled segments reads as a row of buttons.
  static class Tabs
    const string Content
    const string List
    const string ListVertical
    const string NavList
    const string NavTriggerLg
    const string NavTriggerMd
    const string NavTriggerSm
    const string Trigger
    const string TriggerDisabled
  static class Text
    const string Body
    const string BodySm
    const string BodyStrong
    const string Caption
    const string Code
    const string Display
    const string DisplaySm
    const string H1
    const string H2
    const string H3
    const string H4
    const string H5
    const string H6
    const string Label
    const string Link
    const string Muted
    const string Numeric
    const string Overline
    const string Small
    const string Tabular
  static class Textarea
    const string Base
    const string Default
    const string DefaultLg
    const string DefaultSm
    const string Error
    const string ErrorLg
    const string ErrorSm
    const string Ghost
    const string GhostLg
    const string GhostSm
    const string Invalid
    const string InvalidLg
    const string InvalidSm
    const string Success
    const string SuccessLg
    const string SuccessSm
    const string Warning
    const string WarningLg
    const string WarningSm
  // Adaptive (the default) supports switchable light + dark; Fixed commits to one scheme so a client-side theme flip changes nothing the theme defines. Use Fixed for game, atmospheric, or brand-locked looks that must never light/dark switch.
  enum ThemeMode
    Adaptive
    Fixed
  // Each alias expands to the canonical CSS variables that make its intent real across every consumer; the theme renderer, the codegen styling tools, and the docs drift tests all read this table as the single source of truth. Collision policy: primary as a theme key means brand; bare accent and secondary are deliberately NOT aliases — their shadcn and Ikon meanings genuinely conflict, so they stay unknown-key warnings instead of guessing.
  static class ThemeVocabulary
    static IReadOnlyDictionary<string, ThemeVocabulary.Alias> Aliases { get; }
  // Targets are always canonical (never other aliases), so expansion is one step.
  sealed record ThemeVocabulary.Alias
    ctor(string Name, IReadOnlyList<string> Targets, ThemeVocabulary.ValueKind Kind)
    ThemeVocabulary.ValueKind Kind { get; init; }
    string Name { get; init; }
    IReadOnlyList<string> Targets { get; init; }
  enum ThemeVocabulary.ValueKind
    Color
    FontFamily
    Radius
    Duration
    Easing
    Spacing
  static class TimePicker
    const string Column
    const string ColumnSeparator
    const string Content
    const string Default
    const string Item
    const string ItemSelected
    const string Trigger
  static class Toast
    const string Action
    const string Base
    const string Close
    const string Default
    const string Description
    const string Title
    const string Viewport
    const string ViewportBottomCenter
  static class Toggle
    const string Base
    const string Default
    const string DefaultLg
    const string DefaultMd
    const string DefaultSm
    const string Group
    const string GroupVertical
    const string IconDefault
    const string IconDefaultLg
    const string IconDefaultMd
    const string IconDefaultSm
  static class Toggle.Size
    const string Lg
    const string Md
    const string Sm
  static class Toggle.Size.Icon
    const string Lg
    const string Md
    const string Sm
  static class Toggle.Variant
    const string Default
  static class Tokens.Blur
    const string Lg
    const string Md
    const string Sm
  static class Tokens.Duration
    const string Fast
    const string Instant
    const string Normal
    const string Slow
    const string Slower
  static class Tokens.Opacity
    const string GlassLg
    const string GlassMd
    const string GlassSm
    const string O10
    const string O15
    const string O20
    const string O25
    const string O30
    const string O40
    const string O5
    const string O50
  static class Tokens.Radius
    const string Full
    const string Lg
    const string Md
    const string None
    const string Sm
    const string Xl
    const string Xl2
  static class Tokens.Shadow
    const string Lg
    const string Md
    const string None
    const string Sm
    const string Xl
    const string Xl2
  static class Tokens.Width
    const string Dialog
    const string DialogLg
    const string DialogMd
    const string DialogSm
    const string DialogXl
    const string Drawer
    const string Popover
    const string Sheet
    const string Toast
  // The status/meaning axis, mapped to semantic tokens so colors read correctly in light and dark; for a meaning-neutral fill use Variant.
  static class Tone
    const string Error
    const string Ghost
    const string Info
    const string Link
    const string Muted
    const string Neutral
    const string Outline
    const string Primary
    const string Solid
    const string Subtle
    const string Success
    const string Warning
  static class Toolbar
    const string Button
    const string Default
    const string IconStyle
    const string Root
    const string Separator
    const string ToggleGroup
    const string ToggleItem
  static class Tooltip
    const string Content
    const string Default
  static class Transition
    const string Fast
    const string None
    const string Normal
    const string Slow
    const string Slower
  static class Transition.Ease
    const string In
    const string InOut
    const string Linear
    const string Out
  static class Transition.Property
    const string All
    const string Colors
    const string Opacity
    const string Shadow
    const string Transform
  // The fill axis, independent of meaning; pair with a Tone class when the button also carries a status color.
  static class Variant
    const string Ghost
    const string Link
    const string Muted
    const string Outline
    const string Primary
    const string Solid
    const string Subtle
  static class ZIndex
    const string Dropdown
    const string Modal
    const string Overlay
    const string Popover
    const string Sticky
    const string Toast
    const string Tooltip

# Ikon.Crosswind Public API

namespace Ikon.Crosswind
  sealed class CanvasDesignTokenDocument
    ctor()
    Dictionary<string, CanvasTokenValue<string>> BackdropBlur { get; init; }
    Dictionary<string, Dictionary<string, CanvasTokenValue<string>>> ColorScales { get; init; }
    CanvasEffectTokens Effects { get; init; }
    List<string> Guidelines { get; init; }
    Dictionary<string, string> Metadata { get; init; }
    string Name { get; init; }
    Dictionary<string, CanvasTokenValue<string>> Radii { get; init; }
    CanvasSemanticColorTokens SemanticColors { get; init; }
    CanvasTypographyTokens Typography { get; init; }
    string? Version { get; init; }
    void Validate()
  static class CanvasDesignTokenLoader
    static CanvasDesignTokenDocument Load(Stream stream)
    static CanvasDesignTokenDocument LoadFromFile(string path)
    static CanvasDesignTokenDocument LoadFromJson(string json)
  sealed class CanvasEffectTokens
    ctor()
    CanvasModeTokenSet BoxShadows { get; init; }
    CanvasModeTokenSet FocusRing { get; init; }
    CanvasModeTokenSet ShadowPalette { get; init; }
    IEnumerable<KeyValuePair<string, CanvasModeTokenSet>> Enumerate()
    void Validate()
  sealed class CanvasModeTokenSet
    ctor()
    Dictionary<string, CanvasTokenValue<string>> Dark { get; init; }
    Dictionary<string, CanvasTokenValue<string>> Light { get; init; }
    void Validate(string category)
  sealed class CanvasSemanticColorTokens
    ctor()
    CanvasModeTokenSet Background { get; init; }
    CanvasModeTokenSet Border { get; init; }
    CanvasModeTokenSet Foreground { get; init; }
    CanvasModeTokenSet Text { get; init; }
    IEnumerable<KeyValuePair<string, CanvasModeTokenSet>> Enumerate()
    void Validate()
  sealed class CanvasTokenValue<T>
    ctor()
    string? Description { get; init; }
    required T Value { get; init; }
  sealed class CanvasTypographyScale
    ctor()
    string? Description { get; init; }
    string? FontFamily { get; init; }
    string FontSize { get; init; }
    string? LetterSpacing { get; init; }
    string LineHeight { get; init; }
    void Validate(string tokenName)
  sealed class CanvasTypographyTokens
    ctor()
    Dictionary<string, CanvasTypographyScale> Display { get; init; }
    Dictionary<string, CanvasTokenValue<string>> FontFamilies { get; init; }
    Dictionary<string, CanvasTokenValue<int>> FontWeights { get; init; }
    Dictionary<string, CanvasTypographyScale> Text { get; init; }
    void Validate()
  // To take effect, assign an instance to TailwindCustomStyleScope.FlutterTheme and pin that scope via TailwindCustomStyleRegistry.PushScope; the resolver then resolves colour scales and semantic tokens against it instead of the platform baseline. Lookup values may be concrete colours (#hex, rgb(), hsl(), oklch()), scale references ("neutral-800"), or other semantic tokens — the resolver chases references and normalizes concrete colours to hex. Construct with the object-initializer form, which names each map (new FlutterThemeSource { ScaleColors = …, LightSemantic = …, DarkSemantic = … }); ScaleColors, LightSemantic, and DarkSemantic share a dictionary type, so a positional form would let a transposition of the light and dark maps compile and silently invert the two modes. Each unset map defaults to empty.
  sealed class FlutterThemeSource
    ctor()
    // Dark-mode semantic tokens keyed by "prefix-name" ("bg-surface"). Values take the same forms as LightSemantic.
    IReadOnlyDictionary<string, string> DarkSemantic { get; init; }
    // Keyed by role ("body", "display", "heading", …); values are plain family names ("Fraunces"), not CSS font stacks.
    IReadOnlyDictionary<string, string> FontFamilies { get; init; }
    // Light-mode semantic tokens keyed by "prefix-name" ("bg-surface"). Values are colour literals in a form the resolver can normalize (#hex, rgb(), hsl(), oklch()), scale references ("neutral-800"), or other semantic tokens — copied verbatim from the tokens, so not necessarily hex.
    IReadOnlyDictionary<string, string> LightSemantic { get; init; }
    // Logical px. Rung values derive from this unless RadiusPx pins a rung explicitly; null means platform default. Must be a finite value above zero.
    double? RadiusBasePx { get; init; }
    // Values are logical px, keyed by rung name (e.g. "lg"); a pinned rung overrides the value derived from RadiusBasePx. Every value must be finite and non-negative.
    IReadOnlyDictionary<string, double> RadiusPx { get; init; }
    // Colour-scale entries keyed by "scale-shade" ("neutral-800"). Values are the raw colour strings copied verbatim from the tokens, in any form the resolver can normalize: #rrggbb, rgb()/rgba(), hsl()/hsla() or oklch(). Other CSS colour forms are dropped on Flutter.
    IReadOnlyDictionary<string, string> ScaleColors { get; init; }
    // Logical px per spacing unit; scales every numeric spacing utility. Null means platform default (4px). Must be a finite value above zero.
    double? SpacingUnitPx { get; init; }
    // Maps colours only (colour scales plus light/dark semantic tokens). Radii, typography, and spacing are NOT mapped and stay at platform defaults unless supplied via the object initializer.
    static FlutterThemeSource FromDesignTokens(CanvasDesignTokenDocument document)
  // The split form that makes shadow rungs themable without losing per-element recoloring. A sized shadow utility reads each layer's geometry and colour from separate variables (--shadow-{rung}-{n} / --shadow-{rung}-{n}-color) and composes them on the element, where --tw-shadow-color from shadow-red-500 can still take precedence.
  static class ShadowLayers
    // The --tw-shadow value a sized rung utility emits: every layer read from its split variables with the stock value as fallback, colour deferring to --tw-shadow-color.
    static string Compose(string rung)
    // True when name is a sized Tailwind rung (sm, lg, …) whose utility reads the split variables; none and bespoke names are not.
    static bool IsRung(string name)
    // Pads or truncates to MaxLayers.
    static IReadOnlyList<ShadowLayers.Layer> Pad(IReadOnlyList<ShadowLayers.Layer> layers)
    // The stock layers of a rung, padded with Empty to MaxLayers.
    static IReadOnlyList<ShadowLayers.Layer> RungDefaults(string rung)
    // Splits a box-shadow value into layers, taking the colour of each layer to be its first token that is neither inset nor a length; a layer without one gets currentcolor.
    static IReadOnlyList<ShadowLayers.Layer> Split(string value)
    static string VariableName(string rung, int layer, bool color)
    // The layer a rung emits for a slot its value does not fill: zero geometry, so it stays invisible even when recoloured.
    static readonly ShadowLayers.Layer Empty
    // Layers a rung carries. Tailwind's own scale never exceeds two; a theme value with more is truncated.
    const int MaxLayers = 2
  readonly record struct ShadowLayers.Layer
    ctor(string Geometry, string Color)
    string Color { get; init; }
    string Geometry { get; init; }
  enum TailwindColorContext
    // Untyped context (rings, shadows, gradients). The only context that falls back to the union of all aliases — background, foreground, text, and border merged — when the name is not found in a family-scoped map.
    Generic
    // Family-scoped to background aliases only; unlike Generic, it does not fall back to the merged union.
    Background
    // Family-scoped to foreground aliases only; unlike Generic, it does not fall back to the merged union.
    Foreground
    // Family-scoped to text aliases only; unlike Generic, it does not fall back to the merged union.
    Text
    // Family-scoped to border aliases only; unlike Generic, it does not fall back to the merged union.
    Border
  // Custom colour alias maps split by role. Construct with the object-initializer form (new TailwindColorDefinitions { Background = …, Text = … }); the four maps share a dictionary type, so a positional form would let a transposition of any two compile and silently mis-map the roles. An omitted map defaults to empty.
  sealed class TailwindColorDefinitions
    ctor()
    IReadOnlyDictionary<string, string> Background { get; init; }
    IReadOnlyDictionary<string, string> Border { get; init; }
    IReadOnlyDictionary<string, string> Foreground { get; init; }
    IReadOnlyDictionary<string, string> Text { get; init; }
    void Validate()
  static class TailwindCssBaseline
    static string AdditionalCss { get; }
    // Intentionally empty: Tailwind's stock palette has no separate dark root-variable set. A theme's dark appearance comes entirely from the dark overrides the app passes to TailwindCssVariables, merged onto this empty base — an app that emits dark CSS must supply its own dark values rather than expecting a baseline to fall back on.
    static IReadOnlyDictionary<string, string> DarkVariables { get; }
    static IReadOnlyDictionary<string, string> LightVariables { get; }
    // Keyed "{name}-{step}" (e.g. "red-50") → OKLCH value.
    static IReadOnlyDictionary<string, string> PaletteColors { get; }
    // Ordered as first seen in the baseline.
    static IReadOnlyList<string> PaletteNames { get; }
    // Ascending numeric order.
    static IReadOnlyList<string> PaletteSteps { get; }
    static string GetFullBaseline()
  // Light and dark CSS variable maps for a compiled theme, each merged over the Tailwind baseline. Construct with the object-initializer form (new TailwindCssVariables { Light = …, Dark = … }); the two maps share a dictionary type, so a positional form would let a transposition compile and silently invert the emitted light/dark CSS. An omitted map defaults to the baseline alone.
  sealed class TailwindCssVariables
    ctor()
    // CSS variables for the dark theme, merged over the Tailwind dark baseline. Keys are bare variable names, exactly as for Light.
    IReadOnlyDictionary<string, string> Dark { get; init; }
    string DarkThemeName { get; init; }
    // CSS variables for the light theme, merged over the Tailwind light baseline. Keys are bare variable names ("color-primary", not "--color-primary"); a leading -- is stripped so a CSS-style key still overrides the baseline entry.
    IReadOnlyDictionary<string, string> Light { get; init; }
    // Emits the dark theme variable block under DarkThemeName, or an empty string when there are no dark variables.
    string EmitDark()
    // Emits the :root variable block for the light theme followed by TailwindCssBaseline.AdditionalCss (the keyframes and animation rules the utilities depend on). Use EmitLight with false when the caller composes the baseline CSS itself, otherwise every keyframe is emitted twice.
    string EmitLight()
    string EmitLight(bool includeBaselineCss)
  // Pin a TailwindCustomStyleScope with PushScope around each compile; lookups prefer the ambient scope and fall back to a process-wide scope for legacy single-app hosts.
  static class TailwindCustomStyleRegistry
    // Flutter theme data of the scope active for the current compile, preferring the ambient scope like the alias lookups do.
    static FlutterThemeSource? CurrentFlutterTheme { get; }
    static bool IsFontFamilyToken(string name)
    static bool IsFontWeightToken(string name)
    // Makes the given scope the ambient alias source for the current async flow until the returned handle is disposed.
    static IDisposable PushScope(TailwindCustomStyleScope scope)
    static bool TryResolve(string name, TailwindColorContext context, out string value)
    static bool TryResolveFontFamily(string name, out string value)
    static bool TryResolveFontWeight(string name, out string value)
  // Compilation resolves aliases against the ambient scope pinned by TailwindCustomStyleRegistry.PushScope, falling back to the process-wide scope; pin an instance around a compile so co-hosted apps stay isolated.
  sealed class TailwindCustomStyleScope
    ctor()
    // Optional Flutter theme data derived from the same app theme as the alias definitions. The Flutter style resolver reads it through the ambient scope so each app in a shared process renders its own brand colors on native clients.
    FlutterThemeSource? FlutterTheme { get; set; }
    bool IsFontFamilyToken(string name)
    bool IsFontWeightToken(string name)
    // Returns true when the merge added or changed at least one alias — the signal that already-compiled styles may now resolve differently and need recompilation.
    bool MergeDefinitions(TailwindStyleDefinitions definitions)
    void SetDefinitions(TailwindStyleDefinitions? definitions)
    bool TryResolve(string name, TailwindColorContext context, out string value)
    bool TryResolveFontFamily(string name, out string value)
    bool TryResolveFontWeight(string name, out string value)
  // Custom font family and weight alias maps. Construct with the object-initializer form (new TailwindFontDefinitions { Family = …, Weight = … }); the two maps share a dictionary type, so a positional form would let a transposition compile and silently mis-map the roles. An omitted map defaults to empty.
  sealed class TailwindFontDefinitions
    ctor()
    IReadOnlyDictionary<string, string> Family { get; init; }
    IReadOnlyDictionary<string, string> Weight { get; init; }
    void Validate()
  sealed class TailwindStyleDefinitions
    ctor()
    ctor(TailwindColorDefinitions colors, TailwindFontDefinitions? fonts = null)
    TailwindColorDefinitions Colors { get; init; }
    TailwindFontDefinitions Fonts { get; init; }
    void Validate()
  // flutter:-prefixed classes apply only on the Flutter renderer, web: only on web/CSS, unprefixed on both; the active renderer strips its own marker and drops the other's classes. Variant-group syntax flutter:(bg-slate-900 text-slate-100) applies the marker to every grouped class.
  static class TargetVariant
    static bool Has(IReadOnlyList<string> variants, string target)
    // Returns the same reference (no copy) when the marker is absent.
    static IReadOnlyList<string> Without(IReadOnlyList<string> variants, string target)
    const string Flutter
    const string Web

# Ikon.App Public API

namespace Ikon.App
  // The decorated class must declare the entry point as a public parameterless method named Main — synchronous void or async Task, never async void (an async void Main is never awaited, so its exceptions escape startup error handling). It is discovered by reflection and invoked once after dependencies are ready; a missing or misnamed Main throws at startup. Declare the UI and endpoints in Main and return — do not block or await indefinitely.
  sealed class AppAttribute : Attribute
    // name: Defaults to the class name
    // productId: Defaults to the full type name
    // description: Defaults to "{ClassName} App"
    // guid: Stable identity that survives class renames, for external systems
    // userType: Machine runs autonomously; Human represents a human user connecting through the app
    // receiveOpcodeGroups: Leave at the default except for specialized protocol-level message filtering
    // sendOpcodeGroups: Leave at the default except for specialized protocol-level message filtering
    // dependencies: Product IDs of apps awaited during connect, before Main() runs and StartingAsync fires
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, string[]? dependencies = null)
    // Each listed app is awaited during connect — before this app's Main() runs and before its StartingAsync event fires — so ordering logic belongs in Main()/ StartingAsync, not in ClientJoinedAsync. Use it to order dependent app startup.
    string[] Dependencies { get; }
    string? Description { get; }
    string? Guid { get; }
    string? Name { get; }
    string? ProductId { get; }
    Opcode ReceiveOpcodeGroups { get; }
    Opcode SendOpcodeGroups { get; }
    UserType UserType { get; }
    int Version { get; }
  // Register every route before calling StartAsync; routes added afterward are not served.
  sealed class AppEndpointHost : IAsyncDisposable
    // The relay tunnel is not allocated until StartAsync is called.
    // app: The app instance.
    // secure: When true (the default) the public URL is https://… with TLS terminated at the relay. When false, plain http://….
    // webSocketKeepAliveInterval: WebSocket keep-alive ping interval. Defaults to 10 seconds.
    // stablePortName: When non-empty, the relay assigns a deterministic public port for this name, so PublicUrl stays the same across reconnects and process restarts. Empty = ephemeral.
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    // False before StartAsync, and after it when the relay was unreachable — the host then serves on LocalPort only and retries the allocation in the background; subscribe to PublicUrlAvailable to learn when the tunnel comes up.
    bool HasPublicUrl { get; }
    // Throws InvalidOperationException when read before StartAsync has completed.
    int LocalPort { get; }
    // Marks external activity (e.g. resets the server's idle timer) so an endpoint-served instance isn't reaped while serving traffic. Null = no hook.
    Action? OnRequest { get; set; }
    // Throws InvalidOperationException when read before the relay tunnel is allocated; guard with HasPublicUrl when the relay may be unreachable.
    string PublicUrl { get; }
    ValueTask DisposeAsync()
    void MapDelete(string pattern, Func<HttpContext, Task> handler)
    void MapGet(string pattern, Func<HttpContext, Task> handler)
    void MapMethods(string pattern, string method, Func<HttpContext, Task> handler)
    void MapPatch(string pattern, Func<HttpContext, Task> handler)
    void MapPost(string pattern, Func<HttpContext, Task> handler)
    void MapPut(string pattern, Func<HttpContext, Task> handler)
    // The framework closes and disposes the socket once the handler returns; do not dispose it or use it past the handler's completion.
    void MapWebSocket(string pattern, Func<HttpContext, WebSocket, Task> handler)
    // Returns as soon as the host is serving and keeps running in the background — it does not block for the host's lifetime. A failed relay allocation is non-fatal.
    Task StartAsync(CancellationToken cancellationToken = default)
    // Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = default)
    // Only for an app whose endpoints are useless without their public URL, and which would rather start late than start wrong — a relay being redeployed takes a few seconds to come back. Do NOT await this on the app initialization path of an app that renders UI: it blocks first paint on something the app does not need in order to draw.
    Task<bool> WaitForPublicUrlAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    // Fires only for the background-retry allocation; not raised when the tunnel was already allocated during StartAsync.
    event Action<string>? PublicUrlAvailable
  // One of the app's two file trees (AppFiles.Public / AppFiles.Data). Paths are plain relative file paths ("thumbnails/42.png") — no leading slash, no .. segments; anything else throws ArgumentException. Read precedence: a runtime-written file wins over a repo-seeded file at the same path. Writes always go to cloud storage (never the local disk), so they persist across deploys; repo-seeded files change by changing the repo. The public tree cannot READ repo-seeded files (in the cloud they live with the frontend, not the app) — it reads and writes runtime files, and GetUrlAsync covers seeded files by returning the path URL the frontend serves.
  sealed class AppFileTree
    // Deleting a missing file is a no-op. A repo-seeded file cannot be deleted here — it ships with the app, so remove it from the repo instead.
    Task DeleteAsync(string path, CancellationToken ct = default)
    Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    // A runtime-written file returns its cloud storage URL. On the public tree, any other path returns the root-relative path URL ("logo.png" → "/logo.png") the frontend serves repo-seeded statics at — derived from the path, not verified to exist. Private repo-seeded files have no URL: read them with ReadBytesAsync.
    Task<string> GetUrlAsync(string path, CancellationToken ct = default)
    Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default)
    Task<string> ReadTextAsync(string path, CancellationToken ct = default)
    // mimeType: Set it for anything a browser will load, so the file is served with the right content type.
    Task WriteBytesAsync(string path, byte[] bytes, string? mimeType = null, CancellationToken ct = default)
    Task WriteTextAsync(string path, string text, CancellationToken ct = default)
  // Public is world-visible by URL (repo files under the root public/ folder are served at their path: public/hero.png → /hero.png); Data is private to the app, seeded from the root data/ folder. Runtime-written files persist across deploys; repo files redeploy with the app.
  sealed class AppFiles
    AppFileTree Data { get; }
    AppFileTree Public { get; }
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own schema/*.tp files (compiled by ikon app teleport build) and are sent and received as native types — no JSON marshalling.
  static class AppMessaging
    // Filtered by the type's opcode; the handler receives the decoded payload and the sender's client session id. Dispose the returned handle to unsubscribe.
    static IDisposable OnMessage<T>(this IMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    // There is no implicit broadcast — you must pass the explicit recipient session IDs. Whether the type travels reliably or unreliably is declared on its .tp schema, not here.
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
  // The app session's ambient databases and secrets, for code with no IApp<TSessionIdentity, TClientParameters> reference — cell types above all. Reach them through AppServices.Instance and never cache them in a static: they are async-local per server session, and a process-global would bleed one tenant's database and secrets into another. A cell can be constructed before the app has started, so await WhenReadyAsync — or check IsReady — before first use.
  sealed class AppServices : AsyncLocalInstance<AppServices>
    ctor()
    // Set ONLY in cell-host mode, where the session serves exactly one cell instance; null in ordinary app instances (a cell shared by many per-user instances has no single app, and media there belongs to whichever instance the client connected to).
    IAppBase? HostApp { get; }
    bool IsReady { get; }
    Secrets Secrets { get; }
    // The connection comes back unopened. No name means the app's default database; the built-in database is provisioned on first use.
    Task<DbConnection> DatabaseAsync(string? databaseName = null)
    // Waits for readiness, then creates and opens the connection — the per-operation shape.
    Task<DbConnection> OpenDatabaseAsync(string? databaseName = null, CancellationToken ct = default)
    Task WhenReadyAsync()
  delegate AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
  // Three ways to send audio, by pacing: SpeakAsync / SendSpeech are real-time paced by the speech mixer and new speech interrupts current speech with a fade — the default for spoken replies. StreamAsync plays a complete clip (decoded file, generated music) paced to real time, without the mixer's interruption semantics. SendImmediateAsync transmits at once with no pacing — only for audio already produced in real time or very short clips; a long clip sent this way arrives all at once and can overflow client audio buffers.
  class Audio
    ctor(IAppBase app)
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    AudioMetrics Metrics { get; }
    SpeechMixer SpeechMixer { get; }
    ValueTask CloseAllAsync()
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    // streamId: The stream id
    AudioOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // How far the client has actually rendered the audio and whether the user can currently hear it. Null when the client has not reported yet (older SDKs never report). Reports arrive roughly twice per second while audio is playing; check AudioPlaybackStatus.ReceivedAtUtc for staleness.
    // clientSessionId: The client session id
    // streamId: The output stream. Null uses the default (speech mixer) stream
    AudioPlaybackStatus? GetPlaybackStatus(int clientSessionId, string? streamId = null)
    // Delivery is unpaced: the client receives everything as fast as it encodes. Callers own the real-time pacing, so feed this method chunks as they are produced, not a whole clip at once.
    // samples: Floating point PCM samples in range [-1.0, 1.0]
    // sampleRate: Sample rate in Hz
    // channelCount: Number of audio channels
    // isFirst: True when this call carries the beginning of a clip (starts a new playback on the client)
    // isLast: True when this call carries the end of the clip (a single complete clip passes true for both)
    // streamId: Optional id to distinguish between multiple concurrent audio streams. Required when sending multiple streams simultaneously
    // totalDuration: Optional total duration of the audio to be output, if known
    // encoderOptions: Optional encoder options. Falls back to DefaultEncoderOptions if not specified
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    ValueTask SendImmediateAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Real-time paced by the speech mixer, so fast producers (typical TTS) cannot overflow client audio buffers; a chunk with a new id interrupts current playback with a fade. Returns immediately — playback happens in the background.
    // audio: Audio chunk with samples
    // effects: Optional audio effects to apply
    // analyzers: Optional audio analyzers
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    void SendSpeech(AudioChunk audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Completes at end of mixer playout (pause-aware, real-time paced), not at end of generation. Long texts are backpressure-paced against the bounded mixer buffer, so any length is safe. An interruption by a newer Speak call completes the task quietly.
    // text: The text to speak. Whitespace-only text is a no-op
    // model: The speech generator model to use
    // voice: Optional voice id. Null uses the model's default voice
    // instructions: Optional delivery instructions (tone, emotion, style). Support is model-specific; unsupported models ignore them
    // speed: Optional speaking speed, where 1.0 is normal (e.g. 0.8 is slower, 1.2 is faster). Null leaves the model's default. Support is model-specific; unsupported models ignore it
    // effects: Optional audio effects to apply
    // analyzers: Optional audio analyzers
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    // cancellationToken: Cancels generation and playback of this utterance
    Task SpeakAndWaitAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // Each call interrupts the previous one: it fades out whatever is still playing and cancels the prior call's generation, so a new utterance supersedes the old. Defaults to SpeechGeneratorModel.ElevenFlash25. Drive SpeechGenerator + SendSpeech yourself instead when you need overlapping speakers, playback that must not interrupt what is already playing, or raw access to the generated samples.
    // text: Whitespace-only text is a no-op
    // voice: Null uses the model's default voice
    // instructions: Delivery instructions (tone, emotion, style); unsupported models ignore them
    // speed: 1.0 is normal. Null leaves the model's default; unsupported models ignore it
    // targetIds: Null broadcasts to all clients
    // cancellationToken: Cancels generation and playback of this utterance
    Task SpeakAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // One call streams one whole clip on its stream id. Do not run two concurrent calls on the same stream id — the interleaved frames would corrupt client playback; use distinct stream ids or await the previous call first. Cancelling stops the clip early and closes it with a final end-of-stream frame.
    // samples: Floating point PCM samples in range [-1.0, 1.0] for the whole clip
    // sampleRate: Sample rate in Hz
    // channelCount: Number of audio channels
    // streamId: Optional id to distinguish between multiple concurrent audio streams. Required when sending multiple streams simultaneously
    // encoderOptions: Optional encoder options. Falls back to DefaultEncoderOptions if not specified
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    // cancellationToken: Stops the clip early, closing the stream cleanly
    Task StreamAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, string? streamId = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // Call once during app setup. Mutually exclusive with UseTurnDetection, and calling it a second time throws — either conflict raises InvalidOperationException.
    // model: The speech recognizer model to use (e.g., WhisperLarge3Turbo).
    // silenceThresholdRms: RMS threshold below which the segment is treated as silence and skipped.
    // requireCorrelatedStream: When true (default), only fires for streams initiated through a CaptureButton (those with a CorrelationId). Set false to transcribe every audio stream including ad-hoc ones.
    // language: Optional language hint (e.g., "en", "fi"); empty string lets the model autodetect.
    // timeout: Per-segment recognition timeout.
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01f, bool requireCorrelatedStream = true, string language = "", TimeSpan? timeout = null)
    // Call once during app setup. Mutually exclusive with UseSpeechRecognition, and calling it a second time throws — either conflict raises InvalidOperationException.
    // language: Language hint (e.g. "en", "fi"); empty lets the model autodetect.
    // config: Turn detector tuning; null uses defaults tuned for conversational voice.
    // speculative: Starts transcription at the probable turn end so a confirmed turn has zero added recognition latency.
    // pauseWhileAppSpeaking: Suppresses detection while the app is audibly speaking so its own voice can't trigger turns; set false for barge-in apps.
    // requireCorrelatedStream: Only detects turns on streams initiated through a CaptureButton (those with a CorrelationId); false detects on every stream.
    // timeout: Per-recognition timeout; null means one minute.
    void UseTurnDetection(SpeechRecognizerModel model = WhisperLarge3Turbo, string language = "", TurnDetectorConfig? config = null, bool speculative = true, bool pauseWhileAppSpeaking = true, bool requireCorrelatedStream = true, TimeSpan? timeout = null)
    // args.Samples are decoded float PCM at the sample rate from the stream's begin event; IsFirst/IsLast bracket one captured segment (e.g. one push-to-talk press).
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Handlers may set args.StreamingMode to control when the stream's frames are delivered (streamed live, or buffered until the total duration is known / until the last frame).
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Reports arrive periodically while a stream is active and immediately on state changes; GetPlaybackStatus holds the latest snapshot per client.
    event AsyncEventHandler<AudioPlaybackReportEventArgs> PlaybackReportReceivedAsync
    // Exactly one of this and SpeechRecognizedAsync fires per completed segment (neither fires once the app is shutting down). An app that latches busy state when capture stops — a "Transcribing..." spinner, a disabled button — must release it here as well as in SpeechRecognizedAsync; handling only the success event leaves that state stuck on for any press that produces no speech.
    event AsyncEventHandler<SpeechNotRecognizedEventArgs> SpeechNotRecognizedAsync
    // Fires only after UseSpeechRecognition or UseTurnDetection has been called once at setup; subscribing without one of those means this event never fires.
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
    // Fires only after UseTurnDetection has been called once at setup. Start downstream work (e.g. generating a reply) with the args' cancellation token: it is cancelled if the user resumes speaking; otherwise SpeechRecognizedAsync confirms the turn with the same TurnSpeculativeEventArgs.TurnId.
    event AsyncEventHandler<TurnSpeculativeEventArgs> TurnSpeculativeAsync
    // Fires only after UseTurnDetection has been called once at setup. A barge-in or listening-indicator hook.
    event AsyncEventHandler<TurnStartedEventArgs> TurnStartedAsync
  class AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, Context clientContext, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Inherited from the AudioStreamBegin (set by the originating CaptureButton); null for ad-hoc streams.
    string? CorrelationId { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    // Decoded PCM samples in range [-1.0, 1.0]
    float[] Samples { get; }
    string StreamId { get; }
    TimeSpan TotalDuration { get; set; }
    string UserId { get; }
  class AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, int sampleRate, int channelCount, Context clientContext, int trackId, string? correlationId)
    int ChannelCount { get; }
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    string Description { get; }
    int SampleRate { get; }
    string SourceType { get; }
    string StreamId { get; }
    AudioInputStreamingMode StreamingMode { get; set; }
    int TrackId { get; }
    string UserId { get; }
  class AudioInputStreamEndEventArgs : EventArgs
    ctor(string streamId, Context clientContext, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Inherited from the AudioStreamBegin (set by the originating CaptureButton); null for ad-hoc streams.
    string? CorrelationId { get; }
    string StreamId { get; }
    string UserId { get; }
  record AudioOutputStreamInfo
    ctor(string StreamId, int TrackId, AudioCodec Codec, int SampleRate, int ChannelCount)
    int ChannelCount { get; init; }
    AudioCodec Codec { get; init; }
    int SampleRate { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
  class AudioPlaybackReportEventArgs : EventArgs
    ctor(AudioPlaybackStatus status)
    AudioPlaybackStatus Status { get; }
  sealed class AudioPlaybackStatus
    ctor()
    TimeSpan BufferedDuration { get; init; }
    int ClientSessionId { get; init; }
    uint Epoch { get; init; }
    // Null when the client cannot observe the playout position (e.g. WebRTC playback)
    TimeSpan? PlayedDuration { get; init; }
    DateTime ReceivedAtUtc { get; init; }
    AudioPlaybackState State { get; init; }
    int TrackId { get; init; }
  class BackgroundWork
    // Calls are ref-counted: the server is notified only on the first StartAsync and the last StopAsync. Dispose the returned scope (or call StopAsync) to release — pair every Start with exactly one release or idle shutdown stays blocked.
    ValueTask<IAsyncDisposable> StartAsync()
    ValueTask StopAsync()
  // Every null property leaves that setting to the client. Start from Default and override what you need.
  sealed record ClientAudioCaptureOptions
    ctor()
    bool? AutoGainControl { get; init; }
    int? Bitrate { get; init; }
    // 32 kbit/s, auto gain control and noise suppression on, echo cancellation off (nothing is being played back in the common server-transcription case); device is left to the client.
    static ClientAudioCaptureOptions Default { get; }
    string? DeviceId { get; init; }
    // Needed for two-way calls on a loudspeaker; pointless — and lossy — when nothing is being played back, which is why Default leaves it off.
    bool? EchoCancellation { get; init; }
    bool? NoiseSuppression { get; init; }
  sealed record ClientContact
    // Names: The contact's names.
    // Emails: The contact's email addresses.
    // Phones: The contact's phone numbers.
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    IReadOnlyList<string> Emails { get; init; }
    IReadOnlyList<string> Names { get; init; }
    IReadOnlyList<string> Phones { get; init; }
  // Each method targets the calling client resolved from the current reactive scope unless a targetId is supplied. When the target client has not registered the backing function the call degrades to the failure value (false/null/empty list) rather than throwing — except the capture methods (StartVideoCaptureAsync, StartAudioCaptureAsync, CaptureImageAsync), which throw NotSupportedException.
  static class ClientFunctions
    // options: Optional image capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support image capture.
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> EndLiveActivityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> ExitFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> FlushRecordingArchivesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<int?> GetBatteryLevelAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetLanguageAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<ClientLocation?> GetLocationAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // The value is whatever the browser's Network Information API exposes and mixes two vocabularies: a speed class ("slow-2g", "2g", "3g", "4g") where only that is available — note a fast wifi connection commonly reports "4g" — or a connection medium ("wifi", "cellular", "ethernet", "bluetooth", "none", ...) on platforms that expose it. Treat it as an informational hint, not a reliable wifi/cellular discriminator.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetNetworkTypeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetTimezoneAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetUrlAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<ClientVisibility> GetVisibilityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // enabled: Whether to keep the screen awake.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> KeepScreenAwakeAsync(bool enabled, int? targetId = null, CancellationToken cancellationToken = default)
    // The page navigates to the provider and returns authenticated, so the current session ends and the client reconnects with its real identity. Use from a server-drawn sign-in button in a deferred-login app; guest/email/passkey flows are client-initiated and not supported here.
    // provider: The OAuth provider to sign in with (e.g. "google").
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LoginAsync(string provider, int? targetId = null, CancellationToken cancellationToken = default)
    // reason: Optional reason shown in the login dialog.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LoginShowAsync(string? reason = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Clears the auth session and reloads the page, returning the client to the login screen.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LogoutAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // url: The URL to open. Must be absolute (e.g., starts with https://).
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> OpenExternalUrlAsync(string url, int? targetId = null, CancellationToken cancellationToken = default)
    // url: The URL of the sound to play. Can be a regular URL or a data URL.
    // volume: Volume level from 0.0 to 1.0. Defaults to 1.0.
    // loop: Whether to loop the sound. Defaults to false.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> PlaySoundAsync(string url, double volume = 1.0, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Audio bytes are de-duplicated per client session by content hash: the first call uploads the data, later calls with identical bytes send only the hash reference, so a reused sound is never re-transmitted.
    // data: The audio data as a byte array.
    // mimeType: The MIME type of the audio (e.g., "audio/mp3", "audio/wav").
    // volume: Volume level from 0.0 to 1.0. Defaults to 1.0.
    // loop: Whether to loop the sound. Defaults to false.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> PlaySoundAsync(byte[] data, string mimeType, double volume = 1.0, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> RequestFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // x: Horizontal scroll position in pixels.
    // y: Vertical scroll position in pixels.
    // smooth: Whether to animate the scroll.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, int? targetId = null, CancellationToken cancellationToken = default)
    // theme: The theme to set.
    // persist: Whether to persist the theme as a user preference.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> SetThemeAsync(Theme theme, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Prefer SetThemeAsync for the built-in dark and light themes; this overload exists for custom theme names.
    // themeName: The theme name to set (e.g., "light", "dark", or a custom theme name).
    // persist: Whether to persist the theme as a user preference.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when themeName is null or whitespace.
    static Task<bool> SetThemeAsync(string themeName, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // url: The URL path to set (relative paths only).
    // replace: If true, replaces current history entry instead of adding a new one.
    // preserveQueryParams: If true, preserves existing query parameters when the URL does not contain a query string.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Call when a route's content finishes loading (guard with Context.IsSnapshot); without the signal, capture falls back to a quiescence heuristic that may record loading skeletons for slow-loading routes. No-op outside snapshot capture.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> SnapshotReadyAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // options: Optional audio capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support audio capture.
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // title: Fixed for the life of the activity; the app's own name usually.
    // accentHex: The app's accent as #rrggbb, so the banner matches the app.
    // metricsJson: A JSON array of {"value","label"}, at most three shown.
    // status: The small tracked line above the metrics — a phase, a state, a name.
    // muted: Shows the activity as held or paused, which mutes the accent.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StartLiveActivityAsync(string title, string accentHex, string metricsJson, string status, bool muted = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Prefer app.Locations.StartTrackingAsync over calling this directly; each fix is pushed back to the server and surfaces via app.Locations.OnUpdate.
    // intervalSeconds: Minimum seconds between fixes.
    // distanceFilterMeters: Minimum metres of movement before a new fix is reported.
    // background: Keep streaming while the app is backgrounded.
    // notificationTitle: Android foreground-service notification title.
    // notificationBody: Android foreground-service notification body.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StartLocationUpdatesAsync(int intervalSeconds = 10, int distanceFilterMeters = 10, bool background = true, string notificationTitle = "Sharing your location", string notificationBody = "Your location is shared while this is on.", int? targetId = null, CancellationToken cancellationToken = default)
    // hertz: Samples per second per sensor; honoured approximately.
    // sensors: Bit flags matching MotionSensors.
    // batchMilliseconds: How long the client buffers before sending.
    // background: Keep reading while the app is backgrounded.
    // liveHertz: Send only this many a second, keeping the rest for the device archive; 0 sends everything.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StartMotionUpdatesAsync(int hertz = 25, int sensors = 1, int batchMilliseconds = 200, bool background = false, int liveHertz = 0, int? targetId = null, CancellationToken cancellationToken = default)
    // archiveId: Names the activity; one id is one file.
    // fixes: Record position fixes.
    // motion: Record motion samples at their full rate.
    // maxBytes: Refuse to grow the file past this.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StartRecordingArchiveAsync(string archiveId, bool fixes = true, bool motion = true, long maxBytes = 268435456, int? targetId = null, CancellationToken cancellationToken = default)
    // source: The video source (Camera or Screen).
    // options: Optional video capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support video capture.
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // streamId: The stream ID of the capture to stop.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when streamId is null or whitespace.
    static Task<bool> StopCaptureAsync(string streamId, int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopLocationUpdatesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopMotionUpdatesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // archiveId: The id given to StartRecordingArchiveAsync.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopRecordingArchiveAsync(string archiveId, int? targetId = null, CancellationToken cancellationToken = default)
    // playbackId: The playback ID returned from PlaySoundAsync.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopSoundAsync(string playbackId, int? targetId = null, CancellationToken cancellationToken = default)
    // metricsJson: A JSON array of {"value","label"}, at most three shown.
    // status: The small tracked line above the metrics.
    // muted: Shows the activity as held or paused.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> UpdateLiveActivityAsync(string metricsJson, string status, bool muted = false, int? targetId = null, CancellationToken cancellationToken = default)
    // durationMs: The vibration duration in milliseconds.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentOutOfRangeException: Thrown when durationMs is not positive.
    static Task<bool> VibrateAsync(int durationMs, int? targetId = null, CancellationToken cancellationToken = default)
    // pattern: The alternating vibrate/pause durations in milliseconds.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when pattern is null, empty, or contains a negative duration.
    static Task<bool> VibrateAsync(IReadOnlyList<int> pattern, int? targetId = null, CancellationToken cancellationToken = default)
    // pattern: Duration in ms, or comma-separated pattern (e.g., "200" or "100,50,100").
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when pattern is null or whitespace.
    static Task<bool> VibrateAsync(string pattern, int? targetId = null, CancellationToken cancellationToken = default)
  // A preference, not a guarantee — the client falls back to whatever encoder it has.
  enum ClientHardwareAcceleration
    PreferHardware
    PreferSoftware
  sealed record ClientImageCapture
    // Mime: The image's mime type, as encoded by the client: image/jpeg or image/png.
    // Width: The image's actual width in pixels, which can differ from a requested width the client could not honor.
    // Height: The image's actual height in pixels, which can differ from a requested height the client could not honor.
    // Data: The encoded image bytes (a complete JPEG or PNG file, not raw pixels), ready to write to disk or hand to an asset or a vision model.
    ctor(string Mime, int Width, int Height, byte[] Data)
    byte[] Data { get; init; }
    int Height { get; init; }
    string Mime { get; init; }
    int Width { get; init; }
  enum ClientImageCaptureFormat
    Jpeg
    Png
  // Every null property leaves that setting to the client.
  sealed record ClientImageCaptureOptions
    ctor()
    // Null captures JPEG.
    ClientImageCaptureFormat? Format { get; init; }
    int? Height { get; init; }
    // 0.0 (smallest, most artifacts) to 1.0 (largest, near-lossless); only meaningful for ClientImageCaptureFormat.Jpeg — PNG is lossless and ignores it.
    double? Quality { get; init; }
    int? Width { get; init; }
  class ClientJoinedEventArgs : EventArgs
    ctor(Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string UserId { get; }
  class ClientLeftEventArgs : EventArgs
    ctor(Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string UserId { get; }
  sealed record ClientLocation
    // Latitude: The latitude coordinate.
    // Longitude: The longitude coordinate.
    // Accuracy: The accuracy of the coordinates in meters.
    ctor(double Latitude, double Longitude, double Accuracy)
    double Accuracy { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
  sealed record ClientMediaDevice
    // DeviceId: The unique identifier for the device.
    // Kind: The kind of device (audio input or video input).
    // Label: A human-readable label for the device.
    // GroupId: The group identifier for devices that share the same physical device.
    ctor(string DeviceId, ClientMediaDeviceKind Kind, string Label, string GroupId)
    string DeviceId { get; init; }
    string GroupId { get; init; }
    ClientMediaDeviceKind Kind { get; init; }
    string Label { get; init; }
  enum ClientMediaDeviceKind
    Unknown
    AudioInput
    VideoInput
  sealed class ClientProfile
    ProfileAddress? Address { get; }
    string? BirthDate { get; }
    string? Email { get; }
    string? FirstName { get; }
    string? Gender { get; }
    string Id { get; }
    string? Language { get; }
    string? LastName { get; }
    string? Name { get; }
    string? PhoneNumber { get; }
    string? PreferredName { get; }
    IReadOnlyList<string> Roles { get; }
    string UserId { get; }
    // Computed: PreferredName ?? FirstName ?? empty
    string VisibleName { get; }
    object? GetAttribute(string key)
    TAttributes GetAttributes<TAttributes>() where TAttributes : IProfileAttributes, new()
    bool HasRole(UserRole role)
    void RequireRole(UserRole role)
  // A connected client's profile is cached when it joins, so lookups for connected clients return from cache; a cache miss loads from the backend asynchronously. Lookups return null when the context carries no UserId or the backend has no matching profile.
  class ClientProfiles
    ctor(IAppBase app)
    Task AddRoleAsync(Context clientContext, UserRole role)
    Task AddRoleAsync(Context clientContext, string role)
    void ClearCache()
    Task<IReadOnlyList<ClientProfile>> FindProfilesAsync(Dictionary<string, string> filters, int maxResults = 1000)
    Task<IReadOnlyList<ClientProfile>> GetAllProfilesAsync(int maxResults = 1000)
    Task<TAttributes?> GetAttributesAsync<TAttributes>(Context clientContext) where TAttributes : IProfileAttributes, new()
    Task<ClientProfile?> GetProfileAsync(Context clientContext)
    Task<ClientProfile?> GetProfileAsync(string userId)
    Task RefreshProfileAsync(Context clientContext)
    Task RefreshProfileAsync(string userId)
    Task RemoveRoleAsync(Context clientContext, UserRole role)
    Task RemoveRoleAsync(Context clientContext, string role)
    Task SetAttributesAsync<TAttributes>(Context clientContext, TAttributes attrs) where TAttributes : IProfileAttributes
    Task SetRolesAsync(Context clientContext, IEnumerable<UserRole> roles)
    Task SetRolesAsync(Context clientContext, IEnumerable<string> roles)
    Task UpdateAsync(Context clientContext, Action<ProfileData> update)
  // Listed in ClientVideoCaptureOptions.PreferredCodecs in priority order; the client picks the first one it can actually encode with and falls back to its own default if none are available.
  enum ClientVideoCaptureCodec
    H264
    Vp8
    Vp9
    Av1
  // Every null property leaves that setting to the client. Start from DefaultCamera or DefaultScreen and override what you need.
  sealed record ClientVideoCaptureOptions
    ctor()
    int? Bitrate { get; init; }
    // 720p (1280x720) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference; codec, bitrate, and device are left to the client.
    static ClientVideoCaptureOptions DefaultCamera { get; }
    // 1080p (1920x1080) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference; codec and bitrate are left to the client.
    static ClientVideoCaptureOptions DefaultScreen { get; }
    // A camera id — ignored for screen capture. Null uses the client's default device.
    string? DeviceId { get; init; }
    int? Framerate { get; init; }
    ClientHardwareAcceleration? HardwareAcceleration { get; init; }
    int? Height { get; init; }
    // A receiver can only start decoding on a key frame, so this is the worst-case join latency for anyone who starts watching mid-stream, and the resync granularity after packet loss. Lower means faster joins and more bandwidth. The presets use 90 frames — three seconds at their 30 fps.
    int? KeyFrameIntervalFrames { get; init; }
    IReadOnlyList<ClientVideoCaptureCodec>? PreferredCodecs { get; init; }
    int? Width { get; init; }
  enum ClientVideoCaptureSource
    Camera
    Screen
  enum ClientVisibility
    Unknown
    Visible
    Hidden
  // Filter parameters for a credit cost query. Dates are inclusive and interpreted in UTC. Category filters to one usage category (e.g. llm, image-generation); EventName filters to one full usage event name (e.g. llm.openai.gpt4o.global.output-text-tokens); Scopes narrows to usage carrying the given scopes, and GroupByScopeType breaks the result down by the id of one scope type.
  sealed record CostQuery
    ctor(DateOnly StartDate, DateOnly EndDate, string? Category = null, string? EventName = null, IReadOnlyList<CostScopeFilter>? Scopes = null, string? GroupByScopeType = null)
    string? Category { get; init; }
    DateOnly EndDate { get; init; }
    string? EventName { get; init; }
    string? GroupByScopeType { get; init; }
    IReadOnlyList<CostScopeFilter>? Scopes { get; init; }
    DateOnly StartDate { get; init; }
  // Narrows a cost query to usage carrying a scope; a null Value matches any id of that type. Scopes are the app's own attribution: whatever the app pushed with Log.Instance.UseScope(new CustomScope(name, id)) around a piece of work is stamped on every usage that work emits, and can be filtered and grouped on here. Several filters are ANDed — usage must carry all of them.
  sealed record CostScopeFilter
    ctor(string Type, string? Value = null)
    string Type { get; init; }
    string? Value { get; init; }
  // Credit cost surface for an Ikon app: what AI models its space has used and what that usage cost in platform credits. Accessed via app.Costs, reported per day and per usage event name. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
  sealed class CostsService
    // The date range still has to cover when the work ran: usage is stored by day, and a query is only as cheap as the range it scans. An operation that emitted no priced usage sums to zero, which is indistinguishable from one whose usage has not landed yet — see the note on aggregation delay on CostsService before showing the number as final.
    Task<double> GetCreditsForScopeAsync(string scopeType, string scopeId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    // Throws ArgumentException when CostQuery.StartDate is after CostQuery.EndDate. Returns one row per day and usage event name; days without usage produce no rows. Under CostQuery.GroupByScopeType the breakdown is per scope id as well. The result is ordered by date, then event name.
    Task<IReadOnlyList<DailyCost>> GetDailyCostsAsync(CostQuery query, CancellationToken ct = default)
    // The date range is inclusive and interpreted in UTC.
    Task<double> GetTotalCreditsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
  // A [Cron] method behaves like a [Function] in that the trigger resolves it through the FunctionRegistry by name. Applying [Cron] is enough to register the method (as a Local function) — you do not also need [Function], though combining them is fine. The handler takes no caller-supplied arguments. It may optionally accept a host-injected CronContext (fire time + schedule) and/or a CancellationToken that signals app shutdown, in any order — mirroring how an [HttpPost] handler may accept an HttpRequest. Any other parameter fails registration at startup, since the scheduler has nothing to bind it to. Overlap is allowed: a tick fires even if the previous invocation is still running, so guard re-entrancy yourself if it matters.
  sealed class CronAttribute : Attribute
    ctor(string schedule)
    // When null or empty the function is registered (and triggered) under "{DeclaringType.FullName}.{Method}" — the identity the bundle manifest records, so the backend trigger resolves it even when the method is inherited or overridden.
    string? Name { get; init; }
    // Standard 5/6-field cron syntax (e.g. "0 * * * *" for hourly), evaluated by the backend scheduler. The platform enforces a minimum interval of 5 minutes: a faster schedule is clamped to a slower equivalent when a safe one exists, and rejected at bundle time otherwise.
    string Schedule { get; }
  // Credit cost aggregate for one usage event name on one day. Credits is the cost in platform credits — the unit users are billed in. EventName identifies the AI model and usage kind (e.g. llm.openai.gpt4o.global.output-text-tokens) and Category is its first segment (e.g. llm). TotalUsage is the summed usage amount in the event's native unit (tokens, seconds, generations, ...). RawCostEur is the underlying provider cost in EUR and is null unless the space has raw cost visibility enabled. ScopeId is populated only under CostQuery.GroupByScopeType, and is null for usage carrying no scope of that type.
  sealed record DailyCost
    ctor(DateOnly Date, string Category, string EventName, double TotalUsage, double Credits, double? RawCostEur, string? ScopeId = null)
    string Category { get; init; }
    double Credits { get; init; }
    DateOnly Date { get; init; }
    string EventName { get; init; }
    double? RawCostEur { get; init; }
    string? ScopeId { get; init; }
    double TotalUsage { get; init; }
  sealed class EmailNotificationChannel : INotificationChannel
    // email: The app's email service.
    // addressOf: Returns the user's email address, or null when none is known.
    // senderLocalPart: Optional sender local part, as on EmailSendRequest.
    // senderDisplayName: Optional sender display name.
    ctor(EmailService email, Func<string, string?> addressOf, string? senderLocalPart = null, string? senderDisplayName = null)
    string Name { get; }
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  // Accessed via app.Email. Every operation requires the app's space to have the Email feature enabled; a call against a non-entitled space throws FeatureNotEnabledException.
  sealed class EmailService
    // The backend resolves the id before deleting and rejects an unknown one, so a repeated delete throws HttpRequestException carrying a 404 rather than being treated as a no-op. Callers sweeping ids they no longer track should catch it.
    Task DeleteAsync(string id, CancellationToken ct = default)
    // The returned EmailAttachmentDownload owns the content stream; dispose it (e.g. await using) to release the underlying connection.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = default)
    // Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = default)
    // Paginate by passing the returned InboxPage.NextCursor back as InboxQuery.Cursor.
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = default)
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = default)
    // A request that names a sender identity needs a verified sending domain: when the space has none, or the requested EmailSendRequest.SenderDomain is not one of the space's verified sending domains, the send throws EmailSenderNotAvailableException — catch it and resend without the sender fields to deliver from the platform's own address. Invalid field values throw ArgumentException before anything is sent, and a space without the Email feature throws FeatureNotEnabledException.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
  abstract class EndpointAttribute : Attribute
    // Defaults to EndpointAuth.Grant; setting AuthPolicy overrides it.
    EndpointAuth Auth { get; init; }
    // When non-empty, takes precedence over Auth.
    string? AuthPolicy { get; init; }
    // Empty = derived from the method name (kebab-cased). A {name} segment whose name matches a field of the owner's SessionIdentity record binds the routing identity; other {name} segments bind as ordinary handler parameters. Never declare a /.well-known/*, /ikon/*, or /api path — those are reserved.
    string Path { get; }
  enum EndpointAuth
    // Requires a valid signed grant in the URL (the default). Possession authorizes.
    Grant
    // Anonymous — no credential; identity comes from the URL, gated only by anti-abuse.
    Public
    // Always rejected. Declares an endpoint while keeping it closed.
    Deny
    // Unlike Grant, nothing here is minted by the app or pasted into a URL: the client discovers the space's authorization server, the human signs in with the space's own [Auth] Methods, and the client holds a short-lived token it refreshes itself. Anonymous sign-in methods (guest, global) cannot satisfy this — a global visitor is one shared space-wide user, so honouring it would hand every client the same identity and the same data. A space declaring only anonymous methods cannot host a User endpoint.
    User
  sealed record EndpointInfo
    ctor()
    // When non-empty, the gateway cell-routes the request to that cell's partitioned instance, keyed by the cell's IdentityFields in the URL; empty means the endpoint resolves to the app instance.
    string CellType { get; init; }
    // {Owner}_{Method}, derived unconditionally from the owner type and the handler method; the backend resolves this name when routing.
    string FunctionName { get; init; }
    // Carries no grant: a public endpoint is callable as-is, but a grant/policy endpoint needs a working, identity-bound URL minted via IApp.MintUrlAsync.
    string PublicUrl { get; init; }
  // Fired per chunk with the raw bytes for streaming (transcode/scan/forward); the platform already writes the chunk itself. Bytes are not yet verified — the SHA-256 check runs only after the last chunk and a mismatch discards the whole upload, so never act irreversibly. Data is valid only during the callback — copy it to retain it.
  sealed record FileUploadChunkArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The total file size in bytes the client announced.
    // Data: This chunk's bytes. Only valid for the duration of the callback — copy them if you keep them.
    // BytesWritten: Total bytes received and written so far, including this chunk.
    ctor(string UploadId, string FileName, string MimeType, long Size, byte[] Data, long BytesWritten)
    long BytesWritten { get; init; }
    byte[] Data { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Fires only after the byte count and recomputed SHA-256 both match. Exactly one of LocalTempFilePath and AssetUri is non-null. The temp file is deleted when the app stops — move or copy it here to keep it.
  sealed record FileUploadCompleteArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The file size in bytes.
    // LocalTempFilePath: Path to the received file in a temp directory, when the upload was not redirected to the asset system. Null when AssetUri is set. The temp directory is deleted when the app stops, so move or copy anything you want to keep.
    // AssetUri: The asset the upload was written into, when an earlier hook set FileUploadResult.AssetUri. Null when the file went to a local temp file instead. Exactly one of the two is non-null. It is the same AssetUri every Asset.Instance.* call takes, so it needs no parsing — null-check it and pass .Value straight on.
    ctor(string UploadId, string FileName, string MimeType, long Size, string? LocalTempFilePath, AssetUri? AssetUri)
    AssetUri? AssetUri { get; init; }
    string FileName { get; init; }
    string? LocalTempFilePath { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Terminal hook for an upload that had started (cancel, 60 s stall, out-of-sequence chunk, byte-count or SHA-256 mismatch, write failure). Uploads the app rejected from PreStart or Start never reach here. Any partial file/asset is already deleted — clean up only app-side state.
  sealed record FileUploadErrorArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The file size in bytes the client announced.
    // ErrorMessage: Why the upload failed — the cancellation reason when the app cancelled it, otherwise the platform's description of the failure.
    ctor(string UploadId, string FileName, string MimeType, long Size, string ErrorMessage)
    string ErrorMessage { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // First hook, before any bytes transfer — the cheapest place to reject (return false or a FileUploadResult and nothing is sent). Hook order: PreStart → Start → Chunk/Progress (per chunk) → Complete on success or Error on failure. Capture Cancel to abort the upload later, e.g. from a UI cancel button.
  sealed record FileUploadPreStartArgs
    // UploadId: Id identifying this upload; the same value appears on every later hook's args.
    // FileName: The client-supplied file name. Untrusted — never join it into a path yourself.
    // MimeType: The client-supplied mime type. Untrusted — the bytes are not verified against it.
    // Size: The file size in bytes the client claims it will send. The upload fails with an error if the actual byte count differs.
    // Cancel: Aborts this upload: deletes whatever was written, fires the error hook with the reason, and tells the client to stop. Usable at any point during the upload, not just from this callback — capture it to cancel later (e.g. from a UI cancel button).
    ctor(string UploadId, string FileName, string MimeType, long Size, Func<string?, Task> Cancel)
    Func<string?, Task> Cancel { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Fired once per received chunk, after the chunk has been written and acknowledged. Meant for driving a progress bar; use onChunkReceived if you need the bytes themselves.
  sealed record FileUploadProgressArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The total file size in bytes the client announced.
    // ProgressPercentage: Bytes received so far as a percentage of Size, 0 to 100. Zero for the whole upload when the client announced a size of 0.
    // BytesUploaded: Bytes received and written so far.
    ctor(string UploadId, string FileName, string MimeType, long Size, double ProgressPercentage, long BytesUploaded)
    long BytesUploaded { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    double ProgressPercentage { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Accepted defaults to true; return true; works via the implicit bool conversion. Set AssetUri to write the upload straight into the asset system instead of a local temp file.
  sealed record FileUploadResult
    ctor()
    bool Accepted { get; init; }
    AssetUri? AssetUri { get; init; }
    static implicit operator FileUploadResult(bool accepted)
  // Last chance to reject the upload, and the last hook where setting FileUploadResult.AssetUri can redirect the bytes into the asset system instead of a temp file. Only hook that carries Hash — do content-duplicate checks here.
  sealed record FileUploadStartArgs
    // UploadId: Id identifying this upload; the same value appears on every other hook's args.
    // FileName: The client-supplied file name. Untrusted — never join it into a path yourself.
    // MimeType: The client-supplied mime type. Untrusted — the bytes are not verified against it.
    // Size: The file size in bytes the client claims it will send.
    // Hash: The client-declared SHA-256 of the file contents, lowercase hex. The platform recomputes it while receiving and fails the upload with a hash mismatch if the received bytes disagree, so a match here is a genuine content identity — but it is the client's claim, not yet verification, at this point.
    ctor(string UploadId, string FileName, string MimeType, long Size, string Hash)
    string FileName { get; init; }
    string Hash { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class HttpDeleteAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed class HttpGetAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // All verbs share the addressing + identity model on EndpointAttribute. Auth defaults to EndpointAuth.Grant — the gateway answers 401 on the bare URL unless the caller holds a minted grant URL; set Auth = EndpointAuth.Public for an anonymously reachable route (a public webhook, a health check).
  abstract class HttpMethodAttribute : EndpointAttribute
    abstract string Method { get; }
  sealed class HttpPatchAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed class HttpPostAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed class HttpPutAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed record HttpRequest
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  // An endpoint method may return any serializable value for an automatic 200 + JSON response, or return an HttpResult to control status code, content type, and body.
  sealed record HttpResult
    ctor(int StatusCode, object? Body = null, string ContentType = "application/json")
    object? Body { get; init; }
    string ContentType { get; init; }
    int StatusCode { get; init; }
    static HttpResult Accepted(object? body = null)
    static HttpResult BadRequest(string? reason = null)
    static HttpResult Conflict(string? reason = null)
    static HttpResult Created(object? body = null)
    static HttpResult Forbidden(string? reason = null)
    static HttpResult Json(object body, int statusCode = 200)
    static HttpResult NoContent()
    static HttpResult NotFound(string? reason = null)
    static HttpResult Ok(object? body = null)
    static HttpResult Text(string body, int statusCode = 200)
    static HttpResult Unauthorized(string? reason = null)
  interface IApp<out TSessionIdentity, out TClientParameters> : IAppBase
    // Resolves the current client from the ambient reactive scope — call it only inside UI.Root() or another ReactiveScope context; outside one there is no current client and it throws.
    virtual TClientParameters ClientParameters { get; }
    IClientCollection<TClientParameters> Clients { get; }
    TSessionIdentity SessionIdentity { get; }
  interface IAppBase : IMessageChannel
    BackgroundWork BackgroundWork { get; }
    // Costs are reported per day and per usage event name; credits are the billing unit. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
    CostsService Costs { get; }
    // Resolved from the ambient reactive scope: null outside a client scope (e.g. background work, a timer). Identifies the client being served, never this plugin's own connection context.
    virtual Context? CurrentClientContext { get; }
    // Empty string when no client is in scope. This is the correct key for a payment customer key, subscription gating, and per-user state — always populated for a connected client (the real user id when authenticated, else a stable anonymous id).
    virtual string CurrentUserId { get; }
    // An escape hatch for libraries that need a real filesystem path. Prefer Files (Files.Data) — same seeded files, plus runtime writes that persist. Read-only in the cloud — writing to it throws.
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // It compares ABSOLUTE occupancy against a share of the memory limit, so it cannot tell an instance filling up with arrivals from an app that is simply large: an app whose own resting footprint already exceeds that share is refused from its first client onward, answering 429 to every one of them. Measure your app's idle footprint before turning this on.
    bool DynamicMaxClientsEnabled { get; set; }
    // Requires the Email feature enabled on the app's organisation/space; calls from a non-entitled space throw FeatureNotEnabledException.
    EmailService Email { get; }
    // Built once before Main() runs, from the endpoints declared on the app class and on loaded [Cell] types.
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    // The default implementation throws so hand-rolled test doubles keep compiling; the real app host always provides it.
    virtual AppFiles Files { get; }
    GlobalState GlobalState { get; }
    virtual LiveActivityService LiveActivity { get; }
    // null except in local dev on a localhost address (no --host-public), where it lets an in-process client reach this exact process over loopback. Via the relay or in the cloud it is null — connect through the normal relay/ApiKey path instead.
    virtual (string Host, int Port)? LocalLoopbackEndpoint { get; }
    virtual LocationService Locations { get; }
    // 0 lifts the cap entirely, which means exactly that: nothing then stops arrivals before the container runs out of memory and the kernel kills the instance with no warning and no chance to shed load. Prefer a measured number, or turn on DynamicMaxClientsEnabled alongside it.
    int MaxClients { get; set; }
    int MaxMemoryLimitMb { get; }
    virtual MotionService Motion { get; }
    // Each mount produces an independent UI stream addressable from a host UI as <ParallaxView mount="..." />. Defaults to a single mount named "ikon-ui". The value can be replaced with a longer list at any time; the render loop reacts and emits UIStreamBegin/UIStreamEnd for additions and removals.
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    NotificationService Notifications { get; }
    PaymentsService Payments { get; }
    // Reading it inside UI code subscribes to changes; for a URL with query parameters (e.g. a session join link) use JoinUrl.
    virtual string PublicUrl { get; }
    virtual RecordingArchiveService Recordings { get; }
    // Values are fetched once at startup and read synchronously; changes made with ikon app secret set while the app runs take effect only after a restart.
    Secrets Secrets { get; }
    // Consulted only during build-time snapshot capture. Returned routes are unioned with the [BootSnapshot] Routes list from ikon-config.toml, validated, and deduped.
    Func<Task<IEnumerable<string>>>? SnapshotRoutesProvider { get; set; }
    // Named by StateDatabase in the app's ikon-config toml; empty means the built-in app database. An app whose databases carry other names sets this so its state lives in Postgres rather than falling back to asset storage.
    virtual string StateDatabase { get; }
    // Call TelephonyService.GetStatusAsync to find out whether the space has telephony, or TelephonyService.GetNumbersAsync for the numbers themselves, rather than discovering either from a failed send.
    TelephonyService Telephony { get; }
    // Enabled by default. Applies only to clients that connect after it is set; already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    virtual UploadService Uploads { get; }
    // Enabled by default. Disable (e.g. in Main) for apps with no audio/video or low-latency data to save per-client peer-setup cost. Applies only to clients that connect afterward; already-connected clients are unaffected until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Persist the returned bytes as your system of record — the platform's session retention is short. Blocks until the signer completes the ceremony and the platform packages the signed PDF.
    // signerClientSessionId: The client session ID whose browser should perform the signing ceremony.
    // request: The signature order specification (documents, signer policy, purpose).
    // ct: Cancellation token. The order expires server-side after the configured TTL regardless.
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default)
    // The connection comes back unopened — open and dispose it yourself: await using var connection = await app.DatabaseAsync(); await connection.OpenAsync();. Name nothing to get the app's default database — the built-in app one, or the app's own when it declares exactly one; names come from the Databases list in the app's ikon-config toml. The built-in database is provisioned on demand, so the first call may wait while it is created; a declared database is provisioned at activation.
    // databaseName: The database to connect to, or null for the app's default one.
    // throws ArgumentException: Thrown when a named database is not among the app's databases, or when no name was given and the app has several to choose from.
    virtual Task<DbConnection> DatabaseAsync(string? databaseName = null)
    // Provisions the built-in database if the space does not have one yet and adds it to Databases; concurrent callers share one provisioning attempt. DatabaseAsync calls this for you — call it directly only to pay the first-use cost somewhere other than the first query.
    // throws InvalidOperationException: Thrown when the database could not be provisioned.
    virtual Task<DatabaseConnectionInfo> EnsureDefaultDatabaseAsync()
    // Completes only when the persisted deletions have finished. Erasure is idempotent — erasing a user with no stored state is a no-op.
    // userId: The user whose persistent state to erase.
    virtual Task EraseUserStateAsync(string userId)
    // Each readable property becomes a URL-encoded name=value pair and null-valued properties are skipped, so app.JoinUrl(new { id = sessionId }) yields {PublicUrl}?id={sessionId}. Null returns PublicUrl as-is.
    // queryParams: Anonymous object (e.g. new { id = sessionId, host = true }) or string dictionary whose entries become the query string. Null for no query string.
    virtual string JoinUrl(object? queryParams = null)
    // Identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), never by URL path — the path is what minting returns. Omitting identity (null) pins this instance's own session on an app endpoint so the URL routes back here, and pins nothing on a cell endpoint. Grants are non-expiring unless you pass expiresIn.
    // endpoint: Identifies the endpoint by its HANDLER, NOT by its URL path: pass the handler method name (e.g. nameof(GetDocument)) — or the full {Owner}_{Method} registry name when the bare name is ambiguous. Use nameof so a rename stays in sync. You never pass the path here (an endpoint's path is often derived from the method name, and may be templated) — the path is what minting RETURNS, built from this handler's EndpointInfo.PublicUrl.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // One backend round-trip; the result is keyed by the endpoints you passed. See MintUrlAsync for identity pinning and grant lifetime.
    // endpoints: The endpoints to mint, each identified by its HANDLER (a method name such as nameof(GetDoc), or the full {Owner}_{Method} registry name) — never by its URL path. See MintUrlAsync.
    virtual Task<IReadOnlyDictionary<string, MintedUrl>> MintUrlsAsync(IEnumerable<string> endpoints, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // The counterpart to MintUrlAsync when the caller is a person rather than a registered machine. The result is NOT a URL — send it as Authorization: Bearer {token}, never as a query parameter. It is bound to this one endpoint, expires (15 minutes by default), and a call made with it runs under that user's UserScope.
    // endpoint: The endpoint's HANDLER, exactly as MintUrlAsync takes it — a method name, or the full {Owner}_{Method} registry name when the bare one is ambiguous. An owner's JSON-RPC multiplexer is {Owner}_mcp; bare "mcp" resolves only in an app with exactly one MCP surface, so an app with cells that expose tools must name the owner.
    // userId: The space user id the token runs as.
    virtual Task<MintedUserToken> MintUserTokenAsync(string endpoint, string userId, TimeSpan? expiresIn = null, IEnumerable<string>? scopes = null, CancellationToken ct = default)
    // Databases is the list the session was started with. A database created since then — with ikon app db create or from the Portal, neither of which restarts anything — is not in it. DatabaseAsync calls this for you when it meets a name it does not recognise, so an app rarely needs it directly; call it to pick up a new database without naming it, or to see one appear in Databases.
    virtual Task<IReadOnlyList<DatabaseConnectionInfo>> RefreshDatabasesAsync()
    // Bind your listener to the returned RelayEndpoint.LocalPort; the tunnel is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the endpoint to release it.
    // protocol: The endpoint protocol. EndpointProtocol.Tls enables TLS termination at the relay.
    // stablePortName: When non-empty, the relay assigns a deterministic public port for this name, so the endpoint's public URL stays the same across reconnects and process restarts. Empty = ephemeral.
    // localPort: When positive, the tunnel forwards to this local port instead of a freshly picked one — used to attach a tunnel to a listener that is already bound. 0 = pick automatically.
    // ct: Optional cancellation token.
    Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", int localPort = 0, CancellationToken ct = default)
    // Verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier. Blocks until the user completes the challenge in their browser.
    // clientSessionId: The client session ID whose browser should perform the challenge.
    // purpose: App-declared reason for the challenge, e.g. "case.delete".
    // acrValues: Optional identity-provider hints to constrain the authentication method, encoded in the platform's agreed format. When omitted, the platform uses its configured defaults.
    // clientReturnUrl: Optional URL the platform redirects the user's browser to after the IdP flow completes. The platform appends ?stepup=<completed|failed>&challengeId=<id>. When omitted, the user lands on a generic close-window page. Set this to bring the user back into the app UI after step-up.
    // ct: Cancellation token. The challenge expires server-side after the configured TTL regardless.
    Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = default)
    virtual Task RevokeGroupAsync(string group, CancellationToken ct = default)
    virtual Task RevokeUrlAsync(string grantId, CancellationToken ct = default)
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    // Fires after app creation but before Main(). Do not subscribe from inside Main() — it has already fired by then and the handler will never run.
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
    // At-least-once delivery — the handler must be idempotent. Throwing marks the erasure incomplete and it is redelivered on a later session start.
    event AsyncEventHandler<UserDataErasureEventArgs> UserDataErasureAsync
  static class IAppEventExtensions
    static void OnClientJoined(this IAppBase app, Func<Context, Task> handler)
    static void OnClientJoined<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnClientLeft(this IAppBase app, Func<Context, Task> handler)
    static void OnClientLeft<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnMessageReceived(this IAppBase app, Func<ProtocolMessage, Task> handler)
    static void OnSnapshotRoutes(this IAppBase app, Func<Task<IEnumerable<string>>> provider)
    static void OnStarting(this IAppBase app, Func<Task> handler)
    static void OnStopping(this IAppBase app, Func<Task> handler)
    // Clean APP-OWNED data here (own database tables, PII embedded in session/global values) — the platform has already erased the user's platform-managed state. Delivery is at-least-once, so the handler must be idempotent.
    static void OnUserDataErasure(this IAppBase app, Func<string, Task> handler)
  interface IClient<out TClientParameters>
    TClientParameters Parameters { get; }
    int SessionId { get; }
  interface IClientCollection<out TClientParameters> : IEnumerable<IClient<TClientParameters>>
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters>? this[int clientSessionId] { get; }
  interface INotificationChannel
    // Used in NotificationInbox.NotifyAsync's channel list and in the per-user mutes — "email", "sms", "telegram", "whatsapp", or your own.
    string Name { get; }
    // Return false when the channel has no address for the user or is not configured; throw only for a real delivery failure.
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  interface IProfileAttributes
  // A phone call whose audio the app both hears and speaks, for building a voice agent. The two streaming members are shaped to plug straight into Ikon.AI: ListenAsync yields what ISpeechRecognizer.RecognizeContinuousSpeechAsync consumes, and SpeakAsync takes what ISpeechGenerator.GenerateSpeechAsync produces. So a conversational loop needs no adapter between them:
  // await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new("How can I help?")));
  //
  // await foreach (var heard in ai.SpeechRecognizer.RecognizeContinuousSpeechAsync(config, call.ListenAsync()))
  // {
  //     await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new(await Reply(heard))));
  // }
  // Sample rates are handled here: the provider's telephony audio and whatever rate the model wants are resampled to meet, so an app never has to know that 8 kHz exists.
  interface IVoiceCall : IAsyncDisposable
    string CallId { get; }
    // In E.164; empty on a call the app placed, where there is no such person.
    string From { get; }
    bool IsConnected { get; }
    // In E.164: the number they dialled on an incoming call, and the number the app asked for on one it placed.
    string To { get; }
    Task HangUpAsync(CancellationToken ct = default)
    // What barge-in needs when the caller starts talking over the agent.
    Task InterruptAsync(CancellationToken ct = default)
    // Ends when the call does.
    // sampleRate: What the consumer wants, typically the recognizer's rate.
    IAsyncEnumerable<float[]> ListenAsync(int sampleRate = 16000, CancellationToken ct = default)
    // Speaks audio to the caller, sending each chunk as it is produced. Returns once every chunk has been sent, which is before the caller has finished hearing it — the provider buffers and plays at its own rate. Use WaitForPlaybackAsync to wait for the audio to actually land, and InterruptAsync to abandon it.
    Task SpeakAsync(IAsyncEnumerable<AudioChunk> audio, CancellationToken ct = default)
    Task WaitForPlaybackAsync(CancellationToken ct = default)
  sealed record InboxItem
    // Id: Stable id, generated by the inbox.
    // Title: Notification title.
    // Body: Optional body text.
    // Kind: App-defined category, e.g. "order" or "payment". Free text.
    // LaunchUrl: Optional in-app path the UI opens when the item is tapped.
    // Data: Optional opaque payload the app stored with the item.
    // Tag: Optional collapse key — a later item with the same tag replaces this one, as it does for the push notification.
    // CreatedAt: UTC time the item was recorded.
    // Read: Whether the user has seen it.
    ctor(string Id, string Title, string? Body, string? Kind, string? LaunchUrl, string? Data, string? Tag, DateTime CreatedAt, bool Read)
    string? Body { get; init; }
    DateTime CreatedAt { get; init; }
    string? Data { get; init; }
    string Id { get; init; }
    string? Kind { get; init; }
    string? LaunchUrl { get; init; }
    bool Read { get; init; }
    string? Tag { get; init; }
    string Title { get; init; }
  // Every call answers false rather than throwing when the client cannot show one — a browser, an Android device, an iOS version below 16.2, or a Flutter app whose shell predates the bridge. A banner is a nicety and its absence must never take an app down with it.
  // await app.LiveActivity.StartAsync("Momentum", "#db176e",
  //     [new LiveMetric("0.00 km", "distance"), new LiveMetric("0:00", "moving")], "Run");
  sealed class LiveActivityService
    // Prefer EndEverywhereAsync when finishing whatever the activity was showing. A phone that reconnects — a dropped socket, a restarted app, a redeploy — comes back as a NEW session, so ending on the session that started the activity aims at an id that no longer exists and strands a live-looking banner on the lock screen.
    // sessionId: The client to clear, or null for the calling client.
    // ct: Optional cancellation token.
    Task<bool> EndAsync(int? sessionId = null, CancellationToken ct = default)
    // ct: Optional cancellation token.
    Task EndEverywhereAsync(CancellationToken ct = default)
    // title: Fixed for the life of the activity; usually the app's name.
    // accentHex: The app's accent as #rrggbb.
    // metrics: Up to three; any beyond that are not shown.
    // status: The tracked line above the metrics — a phase, a state, a kind.
    // muted: Show it held or paused, which mutes the accent.
    // sessionId: The client to show it on, or null for the calling client.
    // ct: Optional cancellation token.
    Task<bool> StartAsync(string title, string accentHex, IReadOnlyList<LiveMetric> metrics, string status, bool muted = false, int? sessionId = null, CancellationToken ct = default)
    // metrics: Up to three; any beyond that are not shown.
    // status: The tracked line above the metrics.
    // muted: Show it held or paused.
    // sessionId: The client to update, or null for the calling client.
    // ct: Optional cancellation token.
    Task<bool> UpdateAsync(IReadOnlyList<LiveMetric> metrics, string status, bool muted = false, int? sessionId = null, CancellationToken ct = default)
  sealed record LiveMetric
    // Value: Already formatted — the app owns its units and the banner must not reinvent them.
    // Label: The small caption under it, upper-cased by the banner.
    ctor(string Value, string Label)
    string Label { get; init; }
    string Value { get; init; }
  // The one-shot ClientFunctions.GetLocationAsync is a pull that only works while the client is connected and awake; this is the push model that survives backgrounding. Continuous background location needs the user's "Always"/background permission and is subject to app-store review, so start it only for a real reason (an active delivery, a live trip) and stop it when done.
  // app.Locations.OnUpdate(u => _couriers.Update(cs => cs.Select(c => c.SessionId == u.SessionId ? c with { Lat = u.Latitude, Lon = u.Longitude } : c)));
  // await app.Locations.StartTrackingAsync(ReactiveScope.ClientId, new LocationTrackingOptions(IntervalSeconds: 5));
  sealed class LocationService
    // Handlers run on the pushing client's reactive scope, so writing per-user or per-session reactive state from here just works.
    void OnUpdate(Action<LocationUpdate> handler)
    // Not for app code — call OnUpdate to observe. Public because the function registry binds to it by reflection.
    bool ReceiveLocationUpdate(double latitude, double longitude, double accuracy, double speed, double heading, double? altitude = null, double timestampMs = 0.0)
    void RemoveHandler(Action<LocationUpdate> handler)
    // Returns true when the client accepted (it supports geolocation and permission was not denied outright).
    // sessionId: The client session to track.
    // options: Interval, distance filter, background flag and the Android notification text.
    // ct: Optional cancellation token.
    Task<bool> StartTrackingAsync(int sessionId, LocationTrackingOptions? options = null, CancellationToken ct = default)
    // sessionId: The client session to stop tracking.
    // ct: Optional cancellation token.
    Task<bool> StopTrackingAsync(int sessionId, CancellationToken ct = default)
  sealed record LocationTrackingOptions
    // IntervalSeconds: Minimum seconds between reported fixes.
    // DistanceFilterMeters: Minimum metres of movement before a new fix is reported.
    // Background: Keep streaming while the app is backgrounded (Android foreground service + iOS background-location mode). When false the stream stops on backgrounding.
    // NotificationTitle: Android foreground-service notification title shown while tracking.
    // NotificationBody: Android foreground-service notification body.
    ctor(int IntervalSeconds = 10, int DistanceFilterMeters = 10, bool Background = true, string NotificationTitle = "Sharing your location", string NotificationBody = "Your location is shared while this is on.")
    bool Background { get; init; }
    int DistanceFilterMeters { get; init; }
    int IntervalSeconds { get; init; }
    string NotificationBody { get; init; }
    string NotificationTitle { get; init; }
  sealed record LocationUpdate
    // SessionId: The client session the fix came from.
    // UserId: The signed-in user id, or empty for an anonymous session.
    // Latitude: Latitude in degrees.
    // Longitude: Longitude in degrees.
    // AccuracyMeters: Reported horizontal accuracy in metres.
    // SpeedMps: Ground speed in metres/second, or 0 when unknown.
    // Heading: Heading in degrees (0–360), or -1 when unknown.
    // At: Server time the fix was received (UTC).
    // AltitudeMeters: Altitude in metres above the WGS-84 ellipsoid, or NaN when the device did not report one. Clients published before altitude was carried always report NaN.
    // MeasuredAt: Device time the fix was taken (UTC). Equal to At when the client did not report a timestamp. Prefer this over At for anything derived from elapsed time: a batch of fixes delivered after a network stall all arrive at once, so arrival time collapses the intervals between them and every speed and pace computed from it is wrong.
    ctor(int SessionId, string UserId, double Latitude, double Longitude, double AccuracyMeters, double SpeedMps, double Heading, DateTime At, double AltitudeMeters, DateTime MeasuredAt)
    double AccuracyMeters { get; init; }
    double AltitudeMeters { get; init; }
    DateTime At { get; init; }
    double Heading { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
    DateTime MeasuredAt { get; init; }
    int SessionId { get; init; }
    double SpeedMps { get; init; }
    string UserId { get; init; }
  // Sibling of HttpMethodAttribute: both declare an inbound HTTP endpoint over the shared addressing + identity model (see EndpointAttribute), differing only in the wire protocol. Each tool is reachable two ways: through the owner's fixed JSON-RPC multiplexer ({owner}/mcp — tools/list + tools/call, the only surface that streams progress over SSE), and as its own directly-callable POST endpoint whose body IS the tool's arguments object; that per-tool path defaults to the kebab-cased method name, and an EndpointAttribute.Path override adjusts only it, never the multiplexer. A method also carrying a verb-named REST attribute serves the REST surface and suppresses the per-tool MCP endpoint. The governance subject id is always "{Type}.{Method}". Unlike its sibling, EndpointAttribute.Auth defaults to EndpointAuth.User — a grant is a credential no MCP client can obtain; set Auth explicitly for a tool that really is reachable without a user.
  sealed class McpAttribute : EndpointAttribute
    ctor()
    ctor(string path)
    // Set this explicitly; the method's XML doc summary is never used as a fallback.
    string Description { get; init; }
    // Defaults to the method name when null or empty; the governance subject id stays "{Type}.{Method}" regardless.
    string? Name { get; init; }
    // Scopes narrow WITHIN an authorization; they do not replace it. A tool that names a scope must also be reachable — an EndpointAuth.User tool is the case this exists for, because only a token carries scopes at all. Naming one on a Public tool would be meaningless and is ignored. A caller whose token lacks the scope gets 403 with error="insufficient_scope", which is the one refusal an MCP client will re-authorize for. That is why it is a 403 and not a 401: a bare 401 says "who are you", and the client already knows.
    string Scope { get; init; }
  // Sibling of McpAttribute — same cell-method-as-callable model, different MCP verb shape: • Static resource — method takes no arguments; the URI is the literal UriTemplate with no placeholders. Lists in resources/list. • Dynamic resource — method takes parameters that map to {placeholder} segments in the URI template by name. Lists in resources/templates/list; the client crafts a concrete URI and reads it. Read-only by spec — authors should not put side effects in resource methods (the same governance hook still fires on every read with Operation = "resource", so policy authors can distinguish read access from tool dispatch).
  sealed class McpResourceAttribute : Attribute
    ctor(string uriTemplate)
    string Description { get; init; }
    // Defaults to text/plain for string returns and application/octet-stream for binary; override to be more specific (text/markdown, application/json, image/png).
    string MimeType { get; init; }
    // Defaults to the method name when null or empty.
    string? Name { get; init; }
    // Required. Placeholder names must exactly match the cell method's parameter names.
    string UriTemplate { get; }
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
  // Url is the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended; GrantId revokes it, and ExpiresAt is null for the default non-expiring grant.
  sealed record MintedUrl
    ctor(string Url, string GrantId, DateTimeOffset? ExpiresAt)
    DateTimeOffset? ExpiresAt { get; init; }
    string GrantId { get; init; }
    string Url { get; init; }
  // Deliberately not a URL. An access token in a query string is forbidden by the MCP specification and leaks into connector lists, access logs and proxies; this one belongs in a header and nowhere else.
  sealed record MintedUserToken
    ctor(string Token, string Resource, DateTimeOffset ExpiresAt)
    DateTimeOffset ExpiresAt { get; init; }
    string Resource { get; init; }
    string Token { get; init; }
  sealed record MotionBatch
    // SessionId: The client session the batch came from.
    // UserId: The signed-in user id, or empty for an anonymous session.
    // Samples: In the order the device produced them.
    // At: Server time the batch was received (UTC).
    ctor(int SessionId, string UserId, IReadOnlyList<MotionSample> Samples, DateTime At)
    DateTime At { get; init; }
    IReadOnlyList<MotionSample> Samples { get; init; }
    int SessionId { get; init; }
    string UserId { get; init; }
  sealed record MotionOptions
    // Hertz: Samples per second per sensor. 25 is plenty to tell a walk from a trot; a controller wants 60 or more. Devices honour this approximately.
    // Sensors: Which sensors to read.
    // BatchMilliseconds: How long the client buffers before sending. Sending each sample on its own would put a round trip on every one of them; batching turns fifty calls a second into five. Lower it for a controller, raise it to save battery.
    // Background: Keep streaming while the app is backgrounded. On iOS this needs an already-running background mode — motion alone does not keep an app alive, so pair it with location tracking if the app must keep reading in a pocket.
    // LiveHertz: Send only this many samples a second, while RecordingArchiveService keeps every one on the device. Zero streams everything. Use it when the live stream only drives a screen and the analysis happens afterwards.
    ctor(int Hertz = 25, MotionSensors Sensors = UserAcceleration, int BatchMilliseconds = 200, bool Background = false, int LiveHertz = 0)
    bool Background { get; init; }
    int BatchMilliseconds { get; init; }
    int Hertz { get; init; }
    int LiveHertz { get; init; }
    MotionSensors Sensors { get; init; }
  readonly record struct MotionSample
    // AtMillis: Device time in milliseconds since the epoch, when the sample was taken.
    // X: Acceleration in m/s², or rotation in rad/s, or field strength in µT.
    // Y: The second axis.
    // Z: The third axis.
    // Sensor: Which sensor produced it.
    ctor(double AtMillis, double X, double Y, double Z, MotionSensors Sensor)
    double AtMillis { get; init; }
    double Magnitude { get; }
    MotionSensors Sensor { get; init; }
    double X { get; init; }
    double Y { get; init; }
    double Z { get; init; }
  enum MotionSensors
    UserAcceleration
    Acceleration
    Gyroscope
    Magnetometer
  // Samples arrive in batches rather than one at a time, because a round trip per sample at fifty hertz is fifty round trips a second. MotionOptions.BatchMilliseconds is the knob: lower is more responsive, higher is cheaper. **This is not the right transport for a low-latency controller.** Batched function calls carry a scheduling delay of at least one batch, and every sample is delivered reliably whether or not it still matters. A phone used as a pointing device wants an unreliable app-defined .tp message instead, where a dropped sample is simply superseded by the next one. Use this for analysis — gait, cadence, activity, impact — and a .tp channel for control.
  // app.Motion.OnBatch(batch => _cadence.Push(batch.Samples));
  // await app.Motion.StartTrackingAsync(ReactiveScope.ClientId,
  //     new MotionOptions(Hertz: 50, Sensors: MotionSensors.UserAcceleration | MotionSensors.Gyroscope));
  sealed class MotionService
    void OnBatch(Action<MotionBatch> handler)
    bool ReceiveMotionBatch(string samplesJson)
    void RemoveHandler(Action<MotionBatch> handler)
    // sessionId: The client session to stream from.
    // options: Rate, sensors, batching and whether to keep going in the background.
    // ct: Optional cancellation token.
    Task<bool> StartTrackingAsync(int sessionId, MotionOptions? options = null, CancellationToken ct = default)
    // sessionId: The client session to stop.
    // ct: Optional cancellation token.
    Task<bool> StopTrackingAsync(int sessionId, CancellationToken ct = default)
  class Navigation
    // Query string stripped; null outside a client scope or before any path is known. Tracked before the client's first frame renders, so route-dependent server UI can branch on it from the very first render — unlike state set from joined handlers, which run on a background task and can lose the race against the first frame.
    string? CurrentPath { get; }
    // Round-trips to the live client over the connection rather than reading server state; returns null when the client doesn't answer or isn't connected.
    // targetId: Session id of the client to ask
    Task<string?> GetPathAsync(int targetId)
    // Acts on the client of the ambient ClientScope — call from a client-scoped context. Returns null outside a client scope or when the client doesn't answer.
    Task<string?> GetPathAsync()
    // Rejects paths under the platform-reserved /ikon and /api prefixes (throws ArgumentException) — the load balancer owns those. The client's existing query string is preserved unless path carries its own.
    // targetId: Session id of the client to navigate
    // path: App-owned path to navigate to, e.g. /orders/7
    // replace: Replaces the current history entry instead of pushing a new one, so the client's back button skips the path being left behind
    // throws ArgumentException: path falls under a platform-reserved prefix (/ikon or /api)
    Task<bool> SetPathAsync(int targetId, string path, bool replace = false)
    // Acts on the client of the ambient ClientScope — call from a client-scoped context (event handler, function call, reactive render). Rejects reserved /ikon and /api paths (throws ArgumentException), same as the targetId overload.
    // path: App-owned path to navigate to, e.g. /orders/7
    // replace: Replaces the current history entry instead of pushing a new one, so the client's back button skips the path being left behind
    // throws ArgumentException: path falls under a platform-reserved prefix (/ikon or /api)
    Task<bool> SetPathAsync(string path, bool replace = false)
    // Fires on any client URL change — link, back button, reload, or the app's own SetPathAsync. Handlers run on a background task in the navigating client's UserScope/ClientScope, so scoped reactives resolve to that client. A handler exception is logged and swallowed, never reaching the client.
    event AsyncEventHandler<NavigationPathChangedEventArgs> PathChangedAsync
  class NavigationPathChangedEventArgs : EventArgs
    // url: The URL the client navigated to, query string included
    // clientContext: The client that navigated
    ctor(string url, Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string Path { get; }
    string Url { get; }
    string UserId { get; }
  // Tapping it opens the app and routes to the action's LaunchUrl, or reports its Id to the app's notification-tap handler.
  sealed record NotificationAction
    // Id: Stable id reported to the app when this action is tapped.
    // Title: Button label.
    // LaunchUrl: Optional in-app path to open when this action is tapped.
    ctor(string Id, string Title, string? LaunchUrl = null)
    string Id { get; init; }
    string? LaunchUrl { get; init; }
    string Title { get; init; }
  sealed record NotificationContent
    // Title: Notification title. Required.
    // Body: Optional body text shown below the title.
    // IconUrl: Optional URL of an icon image shown with the notification.
    // Tag: Optional collapse key — a later notification with the same tag replaces an existing one instead of stacking.
    // LaunchUrl: Optional in-app path the client navigates to when the user taps the notification.
    // Data: Optional opaque JSON payload the app receives back when the user taps the notification.
    ctor(string Title, string? Body = null, string? IconUrl = null, string? Tag = null, string? LaunchUrl = null, string? Data = null, NotificationPriority Priority = Normal, IReadOnlyList<NotificationAction>? Actions = null)
    IReadOnlyList<NotificationAction>? Actions { get; init; }
    string? Body { get; init; }
    string? Data { get; init; }
    string? IconUrl { get; init; }
    string? LaunchUrl { get; init; }
    NotificationPriority Priority { get; init; }
    string? Tag { get; init; }
    string Title { get; init; }
  // Declare it as a field of the app so it is constructed with the other persisted state, and register the channels the app can address:
  // private readonly NotificationInbox _inbox = new(app);
  //
  // _inbox.Channels.Add(new EmailNotificationChannel(app.Email, userId => _profiles.ValueFor(userId).Email));
  // _inbox.Channels.Add(new SmsNotificationChannel(app.Telephony, userId => _profiles.ValueFor(userId).Phone));
  //
  // await _inbox.NotifyAsync(order.CustomerUserId,
  //     new NotificationContent("Order delivered", "Enjoy your meal", LaunchUrl: $"/orders/{order.Id}", Tag: order.Id),
  //     kind: "order", route: NotificationRoute.Everywhere("email"));
  // Inside a UI lambda or handler Items and MarkRead act on the signed-in user; from a background task use the …For(userId) forms. A user mutes a channel with Mute; push is the channel named "push".
  sealed class NotificationInbox
    // app: The app; its Notifications service delivers the push side.
    // key: Storage key of the inbox list. Change it only to keep two inboxes apart.
    ctor(IAppBase app, string key = "ikon.notifications.inbox")
    // push: Null makes an inbox-only instance with no device push.
    ctor(NotificationService? push, string key = "ikon.notifications.inbox")
    List<INotificationChannel> Channels { get; }
    // Newest first. A tracked read — a UI lambda re-renders when it changes.
    IReadOnlyList<InboxItem> Items { get; }
    // Oldest items are dropped once a user's inbox grows past this; 200 by default.
    int MaxItems { get; init; }
    // 0 (the default) disables the cap. High-priority notifications ignore it, and the excess is still recorded in the inbox — only the device buzz is dropped.
    int MaxPushPerWindow { get; init; }
    // A tracked read.
    IReadOnlyList<string> Muted { get; }
    // Ten minutes by default.
    TimeSpan PushWindow { get; init; }
    // A tracked read.
    QuietHours? QuietHours { get; }
    // A tracked read.
    int UnreadCount { get; }
    void Clear()
    void ClearFor(string userId)
    void ClearQuietHours()
    void ClearQuietHoursFor(string userId)
    // A tracked read.
    bool IsMuted(string channel)
    IReadOnlyList<InboxItem> ItemsFor(string userId)
    void MarkAllRead()
    void MarkRead(string itemId)
    void MarkReadFor(string userId, string itemId)
    void Mute(string channel, bool muted = true)
    void MuteFor(string userId, string channel, bool muted = true)
    // userId: The user to notify.
    // content: Title, body, launch url, tag and data, as for NotificationService.
    // kind: App-defined category stored on the item for filtering.
    // route: Where to deliver; NotificationRoute.Default is inbox plus push.
    // ct: Optional cancellation token.
    Task<NotificationOutcome> NotifyAsync(string userId, NotificationContent content, string? kind = null, NotificationRoute? route = null, CancellationToken ct = default)
    QuietHours? QuietHoursFor(string userId)
    void Remove(string itemId)
    void SetQuietHours(TimeOnly startUtc, TimeOnly endUtc)
    void SetQuietHoursFor(string userId, TimeOnly startUtc, TimeOnly endUtc)
    int UnreadCountFor(string userId)
    const string PushChannel
  sealed record NotificationOutcome
    // Item: The inbox item, or null when the route skipped the inbox.
    // PushResults: Per-session push outcomes; empty when the user was offline or push was off.
    // Delivered: Names of the extra channels that sent ("email", "sms", …).
    // Skipped: Channels that had no address for the user, were unconfigured, or are muted by the user.
    // Failed: Channels that threw; the error is logged, the notification still stands in the inbox.
    ctor(InboxItem? Item, IReadOnlyList<NotificationSendResult> PushResults, IReadOnlyList<string> Delivered, IReadOnlyList<string> Skipped, IReadOnlyList<string> Failed)
    IReadOnlyList<string> Delivered { get; init; }
    IReadOnlyList<string> Failed { get; init; }
    InboxItem? Item { get; init; }
    IReadOnlyList<NotificationSendResult> PushResults { get; init; }
    IReadOnlyList<string> Skipped { get; init; }
  enum NotificationPermission
    Default
    Granted
    Denied
    Unsupported
  enum NotificationPriority
    // Ambient: recorded in the inbox, no device push or channel send.
    Low
    // Default: push and channels, subject to quiet hours and frequency caps.
    Normal
    // Urgent: bypasses quiet hours and frequency caps (an explicit mute still wins).
    High
  enum NotificationReach
    // Offline push is used solely when no session is connected — a user reading the app on a laptop does not also get a buzz on their phone.
    ConnectedFirst
    // Connected sessions get the foreground notification and the offline push hub delivers to each registered device as well. Set NotificationContent.Tag so a device that is connected collapses its foreground and push copies into one.
    AllDevices
  sealed record NotificationRoute
    // Inbox: Record the item in the user's in-app inbox.
    // Push: Show it on the user's devices through app.Notifications — web push on browsers, OS notifications on iOS and Android from the Flutter app.
    // Reach: Whether push stops at the connected devices or reaches every registered one.
    // Channels: Names of the extra channels to deliver on; each must be registered in NotificationInbox.Channels. Unknown names are skipped with a warning.
    ctor(bool Inbox = true, bool Push = true, NotificationReach Reach = ConnectedFirst, IReadOnlyList<string>? Channels = null)
    IReadOnlyList<string>? Channels { get; init; }
    bool Inbox { get; init; }
    bool Push { get; init; }
    NotificationReach Reach { get; init; }
    static NotificationRoute Everywhere(params string[] channels)
    NotificationRoute With(params string[] channels)
    static readonly NotificationRoute AllDevices
    static readonly NotificationRoute Default
    static readonly NotificationRoute Silent
  sealed record NotificationSendResult
    // SessionId: The target client session id.
    // Delivered: True when the client actually displayed the notification (permission granted).
    // Permission: The client's resulting permission state after the send attempt.
    ctor(int SessionId, bool Delivered, NotificationPermission Permission)
    bool Delivered { get; init; }
    NotificationPermission Permission { get; init; }
    int SessionId { get; init; }
  // Accessed via app.Notifications. Client permission is requested lazily on the first actual send, not when the app opens. SendToUserAsync automatically falls back to offline OS push (Web Push / FCM) when the target user has no connected session.
  sealed class NotificationService
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    // sessionId: The target client session id.
    // ct: Optional cancellation token.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    // sessionId: The target client session id.
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // Returns one result per connected session for the user. An empty list means the user had no connected session and only offline push was attempted — it is not an error.
    // userId: The persistent user id to notify.
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = default)
    // userId: The persistent user id to notify.
    // content: The notification content. Give it a NotificationContent.Tag so a device that is both connected and pushed shows one notification, not two.
    // reach: How many of the user's devices to reach.
    // ct: Optional cancellation token.
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, NotificationReach reach, CancellationToken ct = default)
  // Use for app-wide configuration the app instance owns. For per-session-identity state (the typical app routing key) use PersistentSessionReactive<T>; for per-user state use PersistentUserReactive<T>.
  class PersistentReactive<T> : Reactive<T>
    ctor(T initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentReactive<T>. For per-user dictionaries use PersistentUserReactiveDictionary<TKey, TValue>.
  class PersistentReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue> where TKey : notnull
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentReactive<T>. For per-user sets use PersistentUserReactiveHashSet<T>.
  class PersistentReactiveHashSet<T> : ReactiveHashSet<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentReactive<T>. For per-user lists use PersistentUserReactiveList<T>.
  class PersistentReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // This is the natural choice for state that belongs to a specific app instance, since the session identity already determines instance routing.
  class PersistentSessionReactive<T> : Reactive<T>
    ctor(T initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentSessionReactive<T>, which is the natural choice for dictionary state belonging to a specific app instance.
  class PersistentSessionReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue> where TKey : notnull
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentSessionReactive<T>, which is the natural choice for set state belonging to a specific app instance.
  class PersistentSessionReactiveHashSet<T> : ReactiveHashSet<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentSessionReactive<T>, which is the natural choice for list state belonging to a specific app instance.
  class PersistentSessionReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Partitioned at runtime by UserScope: each user sees their own value across all of their client sessions.
  class PersistentUserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // The in-memory value is dropped (the next read sees the initial value) and the persisted copy is deleted from every store it routes to, so it cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // The background-task form of Value = x: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then write to it from anywhere.
    void SetFor(string userId, T value)
    // An atomic read-modify-write under that user's lock.
    void UpdateFor(string userId, Func<T, T> mutator)
    T ValueFor(string userId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    void ClearFor(string userId)
    bool RemoveFor(string userId, TKey key)
    void SetFor(string userId, TKey key, TValue value)
    void UpdateFor(string userId, Action<Dictionary<TKey, TValue>> transform)
    IReadOnlyDictionary<TKey, TValue> ValueFor(string userId)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveHashSet<T> : ReactiveHashSet<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    bool AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Action<HashSet<T>> transform)
    IReadOnlyCollection<T> ValueFor(string userId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    void AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    IReadOnlyList<T> ValueFor(string userId)
  sealed class ProfileAddress
    string? City { get; }
    string? Country { get; }
    string? Municipality { get; }
    string? State { get; }
    string? Street { get; }
    string? Zip { get; }
  // Only properties assigned on this instance are sent; untouched properties are left unchanged. Assigning null to a property is a change too — it clears that field rather than leaving it untouched.
  sealed class ProfileData
    ctor()
    string? AddressCity { get; set; }
    string? AddressCountry { get; set; }
    string? AddressState { get; set; }
    string? AddressStreet { get; set; }
    string? AddressZip { get; set; }
    string? BirthDate { get; set; }
    string? Email { get; set; }
    string? FirstName { get; set; }
    string? Gender { get; set; }
    string? Language { get; set; }
    string? LastName { get; set; }
    string? Name { get; set; }
    string? PhoneNumber { get; set; }
    string? PreferredName { get; set; }
  // Within it, Normal and Low notifications are recorded in the inbox but not pushed to devices (High priority ignores it). The window may wrap past midnight (e.g. 21:00 → 06:00); convert from the user's local time before setting it.
  sealed record QuietHours
    // StartUtc: Inclusive start of the quiet window, as a UTC time of day.
    // EndUtc: Exclusive end of the quiet window, as a UTC time of day.
    ctor(TimeOnly StartUtc, TimeOnly EndUtc)
    TimeOnly EndUtc { get; init; }
    TimeOnly StartUtc { get; init; }
    bool Contains(TimeOnly utcTimeOfDay)
  // Raw on purpose. The app's own recorder is the processor — smoothing, auto-pause, elevation — and re-running it over a complete set of fixes gives a better track than one assembled live from whatever the network happened to deliver. Storing the processed result instead would bake in the gaps this archive exists to remove.
  readonly record struct RecordedFix
    ctor(double AtMillis, double Latitude, double Longitude, double AccuracyMeters, double SpeedMps, double Heading, double AltitudeMeters)
    double AccuracyMeters { get; init; }
    double AltitudeMeters { get; init; }
    double AtMillis { get; init; }
    double Heading { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
    double SpeedMps { get; init; }
  sealed record RecordingArchive
    // ArchiveId: The activity this archive belongs to, as the app named it.
    // SessionId: The client session that uploaded it.
    // UserId: The signed-in user, or empty.
    // StartedAt: When the device opened the archive (UTC).
    // Fixes: In the order the device recorded them.
    // Motion: In the order the device recorded them.
    // Asset: Where the raw bytes are stored. Keep it if the recording itself is worth keeping — a corpus to train on, or a re-analysis a later build will want to run.
    ctor(string ArchiveId, int SessionId, string UserId, DateTime StartedAt, IReadOnlyList<RecordedFix> Fixes, IReadOnlyList<MotionSample> Motion, AssetUri Asset)
    string ArchiveId { get; init; }
    AssetUri Asset { get; init; }
    IReadOnlyList<RecordedFix> Fixes { get; init; }
    IReadOnlyList<MotionSample> Motion { get; init; }
    int SessionId { get; init; }
    DateTime StartedAt { get; init; }
    string UserId { get; init; }
  // Little-endian throughout. File header, 24 bytes: magic IKAR (4), version u16, reserved u16, startedUnixMs i64, baseAtMs f64. Then records, each opening with kind u8 and offsetMs u32 measured from baseAtMs: a fix carries latitude f64, longitude f64, accuracy f32, speed f32, heading f32, altitude f32 (37 bytes in total); a motion sample carries sensor u8, x f32, y f32, z f32 (18 bytes). Offsets are relative to a base rather than absolute because a millisecond epoch is around 1.7e12, which single precision resolves no better than about 130 ms — coarser than the gap between samples, so absolute float timestamps would destroy every rhythm in the file.
  static class RecordingArchiveCodec
    // throws InvalidDataException: The header is missing or from a newer format.
    static (DateTime StartedAt, List<RecordedFix> Fixes, List<MotionSample> Motion) Decode(ReadOnlySpan<byte> archive)
    static byte[] EncodeFix(RecordedFix value, double baseAtMillis)
    static byte[] EncodeHeader(DateTime startedAt, double baseAtMillis)
    static byte[] EncodeMotion(MotionSample value, double baseAtMillis)
    const int FixBytes = 37
    const int HeaderBytes = 24
    const int MotionBytes = 18
  // It pairs with the live stream rather than replacing it: the live stream drives the screen and may be decimated and gappy, the archive arrives at the end and repairs the record. Keep the server-side recording as it is and let the archive correct it, so that a failed upload or a client too old to record degrades to the live track rather than to nothing. The device keeps each file until the server acknowledges it, so a failed upload is retried on the next connection, and deletes it after.
  // app.Recordings.OnArchive(archive => Repair(archive.Fixes));
  // await app.Recordings.StartAsync(sessionId, activityId);
  sealed class RecordingArchiveService
    void OnArchive(Action<RecordingArchive> handler)
    void RemoveHandler(Action<RecordingArchive> handler)
    // sessionId: The client session to ask.
    // ct: Optional cancellation token.
    Task<bool> RequestPendingAsync(int sessionId, CancellationToken ct = default)
    // sessionId: The client session that should record.
    // archiveId: Names the activity. The same id must be given to StopAsync, and it is what arrives back on RecordingArchive.ArchiveId. One id is one file, so starting and stopping repeatedly produces one archive per activity and never a blend of two.
    // options: What to record.
    // ct: Optional cancellation token.
    Task<bool> StartAsync(int sessionId, string archiveId, RecordingOptions? options = null, CancellationToken ct = default)
    // sessionId: The client session that was recording.
    // archiveId: The id given to StartAsync.
    // ct: Optional cancellation token.
    Task<bool> StopAsync(int sessionId, string archiveId, CancellationToken ct = default)
    const string UploadActionId
  sealed record RecordingOptions
    // Fixes: Record position fixes. Almost always yes — this is what survives an outage.
    // Motion: Record motion samples at the full rate asked of MotionService, independently of the decimated rate being streamed live.
    // MaxBytes: Refuse to grow the file past this. A device with no space left must fail the recording rather than the phone.
    ctor(bool Fixes = true, bool Motion = true, long MaxBytes = 268435456)
    bool Fixes { get; init; }
    long MaxBytes { get; init; }
    bool Motion { get; init; }
  enum RecordingRecordKind
    Fix
    Motion
  class RoleRequiredException : Exception
    ctor(string role, string? userId = null)
    string RequiredRole { get; }
    string? UserId { get; }
  // Shards do NOT share reactive state — each shard is an independent instance of the same identity. Declare sharding only for surfaces designed for it: stateless or read-mostly apps (public landing pages, broadcast views), or apps that synchronize through external state (database, assets). Clients are not sticky to a shard across reconnects. Example:
  // [Sharded(2000)]
  // public record SessionIdentity(string? UserId, [property: Sharded(50)] string? Team);
  sealed class ShardedAttribute : Attribute
    // maxClientsPerShard: Connected-client capacity of one shard before the platform spills to the next one
    ctor(int maxClientsPerShard = 100)
    int MaxClientsPerShard { get; }
    // Cost ceiling on the shard family size; 0 (the default) means unlimited. When every allowed shard is at capacity, new connections still join the last shard over capacity — visitors are never turned away by sharding
    int MaxShards { get; set; }
  // The text is the title, then the body on the next line.
  sealed class SmsNotificationChannel : INotificationChannel
    // telephony: The app's telephony service.
    // phoneOf: Returns the user's E.164 phone number, or null when none is known.
    ctor(TelephonyService telephony, Func<string, string?> phoneOf)
    string Name { get; }
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  sealed class SpeechNotRecognizedEventArgs : EventArgs
    ctor(SpeechNotRecognizedReason reason, Context clientContext, string streamId, string? correlationId, Exception? error = null)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    // The failure when Reason is SpeechNotRecognizedReason.Error; otherwise null.
    Exception? Error { get; }
    SpeechNotRecognizedReason Reason { get; }
    string StreamId { get; }
    string UserId { get; }
  enum SpeechNotRecognizedReason
    NoAudio
    Silence
    NoText
    Error
  sealed class SpeechRecognizedEventArgs : EventArgs
    ctor(string text, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount, int turnId = 0)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    TimeSpan Duration { get; }
    int SampleCount { get; }
    string StreamId { get; }
    string Text { get; }
    // Identifier of the detected turn when the recognition came from Audio.UseTurnDetection, shared with the matching TurnStartedEventArgs and TurnSpeculativeEventArgs; 0 for push-to-talk recognitions.
    int TurnId { get; }
    string UserId { get; }
  class StartingEventArgs : EventArgs
    ctor()
  class StoppingEventArgs : EventArgs
    ctor()
  sealed class TelegramNotificationChannel : INotificationChannel
    // botToken: Bot token from @BotFather; empty disables the channel.
    // chatIdOf: Returns the user's Telegram chat id, or null when none is known.
    ctor(string botToken, Func<string, string?> chatIdOf)
    string Name { get; }
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  // Platform telephony surface for an Ikon app: sending SMS and placing phone calls from a number the platform holds for the app's space. Accessed via app.Telephony. The space needs a number first (ikon app telephony create --country se); until then every operation throws TelephonyNumberNotAvailableException, which names that command. A space may hold several numbers, in different markets and on different providers — omit from and the platform picks one, or name one to send as it. Sending is metered, so a space out of credits is suspended like any other overspend.
  sealed class TelephonyService
    // Routes incoming messages and calls to this app instance, so a reply reaches the person waiting for it rather than whichever instance an empty identity resolves to. The binding outlives this process: it pins an identity, not an instance, so if this one is reaped the next message provisions a fresh instance with the same identity rather than being lost. That is what makes an app wake up when someone texts it. Running locally is the exception. There the binding also carries this machine's instance id, which is minted fresh on every run and cannot outlive it — so a local binding is reverted automatically when the app shuts down, rather than leaving the number pointed at a dead process. It applies to every number the space holds: one number cannot serve two identities, so an app wanting inbound per user needs a number per user.
    Task BindInboundToThisInstanceAsync(CancellationToken ct = default)
    // The same IVoiceCall an incoming call gives, so a conversation reads the same whichever end started it:
    // await using var call = await app.Telephony.CallAsync("+358401234567");
    // await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new("Your build finished")));
    // Returns only once the call is connected and audio can flow; throws if nobody answers before ringTimeout. Dispose it — or call IVoiceCall.HangUpAsync — to end the call. It counts against the space's concurrent-call limit, carries the platform duration cap, and is refused for a disallowed destination.
    // from: Which of the app's numbers to call from. Omit to let the platform choose: the app's default number if it has one, else a number local to the destination's market, else the first it holds. Naming a number the app does not hold is refused rather than substituted.
    Task<IVoiceCall> CallAsync(string to, TimeSpan? ringTimeout = null, string? from = null, CancellationToken ct = default)
    // Every number the app holds, across every provider serving it. Worth reading when the app wants to choose a sender itself rather than let the platform pick one — to answer as the same number a user last saw, say. Most apps never need it: omitting from already sends from a number local to the recipient.
    Task<IReadOnlyList<TelephonyNumber>> GetNumbersAsync(CancellationToken ct = default)
    Task<TelephonyStatus> GetStatusAsync(CancellationToken ct = default)
    // Answers incoming calls with handler. Call it once at startup, and the space's phone number rings this app. The caller's audio reaches the handler as it is spoken and the app can speak back over the same call; see IVoiceCall for the conversational loop. Nothing else has to be configured. Calling this tells the platform that this app answers calls, which is when the provider side is wired up — so an app can start answering the phone without anyone touching a number, and a call that arrives while the app is not running starts it, exactly as an incoming message does.
    Task HandleCallsAsync(Func<IVoiceCall, Task> handler, CancellationToken ct = default)
    // Undoes BindInboundToThisInstanceAsync.
    Task ResetInboundAsync(CancellationToken ct = default)
    // Sends an SMS to the given number, which must be in E.164 form (+ followed by country code and number, for example +358401234567). Check SmsSendResult.Replyable on the result: when it is false the recipient received the message but cannot answer it, because the space holds no number local to their market and a foreign sender is stripped in transit. Long messages are split into billable segments; SmsSendResult.Parts reports how many were charged.
    // from: Which of the app's numbers to send as. Omit to let the platform choose: the app's default number if it has one, else a number local to the recipient's market — which is what keeps a message replyable — else the first it holds. Naming a number the app does not hold is refused rather than substituted, since sending as a different number reaches the recipient as a stranger.
    Task<SmsSendResult> SendSmsAsync(string to, string text, string? from = null, CancellationToken ct = default)
    // Raised for each message one of the space's numbers receives. The app declares no webhook: the platform owns the endpoint the provider posts to and delivers the message here, so a message reaches whichever instance inbound is bound to — starting one if none is running. Reply by calling SendSmsAsync with SmsMessage.From. There is deliberately no "return a string to reply" shortcut: a reply the provider sends on our behalf is billed inside the provider, where nothing can meter it or refuse it for a space out of credit.
    event Func<SmsMessage, Task>? SmsReceived
  enum Theme
    Dark
    Light
  static class ThemeExtensions
    // False for the light theme, custom theme names, and clients that have not reported a theme.
    static bool IsDarkTheme(this Context clientContext)
    static string ToThemeName(this Theme theme)
  sealed class TurnSpeculativeEventArgs : EventArgs
    ctor(int turnId, string text, TimeSpan duration, CancellationToken cancellationToken, string streamId, Context clientContext)
    CancellationToken CancellationToken { get; }
    Context ClientContext { get; }
    int ClientSessionId { get; }
    TimeSpan Duration { get; }
    string StreamId { get; }
    string Text { get; }
    int TurnId { get; }
    string UserId { get; }
  sealed class TurnStartedEventArgs : EventArgs
    ctor(int turnId, string streamId, Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string StreamId { get; }
    int TurnId { get; }
    string UserId { get; }
  // Return an AssetUri from onStart and the bytes stream straight into asset storage without ever being held in the app — which is what a large file needs, since an app container has far less memory than the files people send it.
  // app.Uploads.Register("my-app.telemetry",
  //     onStart: args => Task.FromResult(new FileUploadResult
  //     {
  //         AssetUri = new AssetUri(AssetClass.CloudFile, $"telemetry/{args.FileName}", app.GlobalState.SpaceId),
  //     }),
  //     onComplete: async args =>
  //     {
  //         if (args.AssetUri is { } uri) { await ProcessAsync(uri); }
  //     });
  sealed class UploadService
    // uploadActionId: The id clients tag their upload with. Namespace it — the ids rendered view.FileUpload components generate live in the same table.
    // onStart: Decides where the bytes go, and whether to accept at all. Return a FileUploadResult carrying an AssetUri to stream into asset storage, or one that is not accepted to refuse.
    // onComplete: Runs once every byte has landed.
    // onError: Runs when a transfer fails partway.
    void Register(string uploadActionId, Func<FileUploadStartArgs, Task<FileUploadResult>> onStart, Func<FileUploadCompleteArgs, Task>? onComplete = null, Func<FileUploadErrorArgs, Task>? onError = null)
  class UserDataErasureEventArgs : EventArgs
    ctor(string userId)
    string UserId { get; }
  enum UserRole
    // Maps to the "anonymous" role string, not "guest"
    Guest
    User
    Moderator
    Admin
  class Video
    ctor(IAppBase app)
    ValueTask CloseAllAsync()
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    // streamId: The stream id
    VideoOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Frames are transmitted immediately — the caller owns the pacing. Call once per frame at the source framerate (typically forwarding each incoming frame as it arrives); never loop over a stored clip's frames without pacing.
    // data: Encoded video frame data
    // frameNumber: Frame number in the sequence
    // isKey: Whether this is a keyframe
    // timestampInUs: Timestamp in microseconds
    // durationInUs: Frame duration in microseconds
    // codec: Video codec
    // width: Video width in pixels
    // height: Video height in pixels
    // framerate: Video framerate
    // streamId: Optional id to distinguish between multiple concurrent video streams. Required when sending multiple streams simultaneously
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    // trackId: Optional track id override. When specified, the protocol message will use this track id instead of an auto-assigned one. Use this when echoing WebRTC video to preserve the original track index
    ValueTask SendFrameAsync(byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, VideoCodec codec, int width, int height, double framerate, string? streamId = null, IReadOnlyList<int>? targetIds = null, int? trackId = null)
    // args.Data is encoded codec bitstream (see the codec on the stream's begin event), not decoded pixels — forward it as-is (e.g. via SendFrameAsync) or decode it before analysis.
    event AsyncEventHandler<VideoInputFrameEventArgs> VideoInputFrameAsync
    event AsyncEventHandler<VideoInputStreamBeginEventArgs> VideoInputStreamBeginAsync
    event AsyncEventHandler<VideoInputStreamEndEventArgs> VideoInputStreamEndAsync
  class VideoInputFrameEventArgs : EventArgs
    ctor(string streamId, Context clientContext, int trackId, byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Inherited from the originating VideoStreamBegin (set by a CaptureButton); null for ad-hoc streams.
    string? CorrelationId { get; }
    byte[] Data { get; }
    uint DurationInUs { get; }
    int FrameNumber { get; }
    bool IsKey { get; }
    string StreamId { get; }
    ulong TimestampInUs { get; }
    int TrackId { get; }
    string UserId { get; }
  class VideoInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, VideoCodec codec, string codecDetails, int width, int height, double framerate, Context clientContext, int trackId, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    VideoCodec Codec { get; }
    string CodecDetails { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    string Description { get; }
    double Framerate { get; }
    int Height { get; }
    string SourceType { get; }
    string StreamId { get; }
    int TrackId { get; }
    string UserId { get; }
    int Width { get; }
  class VideoInputStreamEndEventArgs : EventArgs
    ctor(string streamId, Context clientContext, int trackId, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Inherited from the originating VideoStreamBegin (set by a CaptureButton); null for ad-hoc streams.
    string? CorrelationId { get; }
    string StreamId { get; }
    int TrackId { get; }
    string UserId { get; }
  record VideoOutputStreamInfo
    ctor(string StreamId, int TrackId, VideoCodec Codec, int Width, int Height, double Framerate)
    VideoCodec Codec { get; init; }
    double Framerate { get; init; }
    int Height { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
    int Width { get; init; }
  // Free-form text reaches a user only inside the 24-hour customer-service window; outside it the API requires an approved template, so pass templateName to send the same notification text as the template's single body parameter instead.
  sealed class WhatsAppNotificationChannel : INotificationChannel
    // accessToken: Cloud API access token; empty disables the channel.
    // phoneNumberId: The business phone number id the message is sent from.
    // phoneOf: Returns the user's phone number in international format, or null.
    // templateName: Optional approved template with one body parameter.
    // templateLanguage: Template language code, "en" by default.
    ctor(string accessToken, string phoneNumberId, Func<string, string?> phoneOf, string? templateName = null, string templateLanguage = "en")
    string Name { get; }
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)

namespace Ikon.App.Cells
  // A cell is always shared by its SessionIdentity: every caller that Cells.Connects with the same identity reaches the same instance and its Reactive<T> state — the identity IS the sharing scope (parameterless = one global; keyed = one per key). The runtime picks the transport: a local run hosts every cell in-process (a direct object); in the cloud the cell lives in its own cell-host and callers reach it through a proxy ([HttpGet]/[HttpPost] over HTTP, [Function] methods and Reactive<T> members over an SDK connection). App authors never choose or think about placement — they declare [Cell] and a SessionIdentity, and get exactly what those mean.
  sealed class CellAttribute : Attribute
    ctor()
    // Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin CellHost.Resolve<TInterface> across them: globals (parameterless SessionIdentity) eager-spawn at host construction, keyed cells spawn together on first access. Sharded keyed cells must tolerate eventual consistency between shards — hold no per-instance state, or persist shared state externally.
    int Capacity { get; init; }
    // Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
  // Each in-process server runs in its own async-local scope, so Cells.Instance resolves to that server's own host and wiring. The framework calls Initialize once at startup; apps call Connect<TInterface> for each cell access.
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // On a CLOUD run, when TInterface is an interface backed by a [Cell] type, returns a SubstrateCellProxy<TInterface> that dispatches per member: [HttpGet]/[HttpPost] methods over stateless HTTP, [Function] methods and Reactive<T> members over a standard SDK connection to the cell-host. Otherwise — a concrete-type request, or ANY cell on a LOCAL run — returns the local cell instance from this server's CellHost. Local runs host every cell in-process (there is no deployed cell-host to proxy to, and a local run is a single process), so every cell behaves as a normal shared instance locally.
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    ValueTask DisposeAsync()
    const string CellTypeParam
  // Injected into a cell's primary constructor by the framework.
  interface ICell<out TSessionIdentity>
    TSessionIdentity Identity { get; }

namespace Ikon.App.Cron
  sealed record CronContext
    ctor(DateTime FireTimeUtc, string Schedule)
    static CronContext? Current { get; }
    DateTime FireTimeUtc { get; init; }
    string Schedule { get; init; }
    static IDisposable Use(CronContext context)

namespace Ikon.App.Http
  // Exposes the request's resolved identity to handler code on endpoint/MCP-dispatched calls, where the connection-level context carries none. Headers and RawBody are untrusted request inputs — read them for handler logic such as inline webhook-signature verification, but never to derive identity; the target instance is already chosen from trusted sources before the handler runs.
  sealed record HttpCallContext
    ctor(IReadOnlyDictionary<string, string>? SessionIdentity = null, CancellationToken CancellationToken = default, IReadOnlyDictionary<string, string>? Headers = null, string? RawBody = null)
    CancellationToken CancellationToken { get; init; }
    static HttpCallContext? Current { get; }
    IReadOnlyDictionary<string, string>? Headers { get; init; }
    string? RawBody { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentity { get; init; }
    // Null when no HttpCallContext is current or the identity carries no userid (e.g. an anonymous endpoint).
    string? UserId { get; }
    string? Header(string name)
    static IDisposable Use(HttpCallContext context)

namespace Ikon.App.Mcp
  sealed record McpCallContext
    ctor(CancellationToken CancellationToken, Func<ProgressUpdate, Task>? OnProgress, IReadOnlyDictionary<string, string>? SessionIdentityFields = null)
    CancellationToken CancellationToken { get; init; }
    static McpCallContext? Current { get; }
    Func<ProgressUpdate, Task>? OnProgress { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentityFields { get; init; }
    // Null when no McpCallContext is current or the request's claims carry no userid.
    string? UserId { get; }
    static IDisposable Use(McpCallContext context)
  // Progress is a monotonic counter; keep Total constant across a call's updates so clients can render a stable percentage.
  sealed record ProgressUpdate
    ctor(double Progress, double? Total = null, string? Message = null)
    string? Message { get; init; }
    double Progress { get; init; }
    double? Total { get; init; }

namespace Ikon.App.Payments
  enum EntitlementSource
    Unknown
    Subscription
    OneTime
  // Omit Interval for a one-time offer.
  sealed record OfferPriceSpec
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval? Interval = null, int? IntervalCount = null)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval? Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  sealed record OfferSpec
    ctor(string OfferId, string Name, OfferPriceSpec Price)
    string Name { get; init; }
    string OfferId { get; init; }
    OfferPriceSpec Price { get; init; }
  // OfferId is null for ad-hoc charges and records written before offer tracking.
  sealed record Payment
    ctor(string Id, PaymentProvider? Provider, PaymentStatus Status, PaymentKind Kind, string? OfferId, long AmountMinor, string Currency, long AmountRefundedMinor, DateTimeOffset? CreatedAt)
    long AmountMinor { get; init; }
    long AmountRefundedMinor { get; init; }
    DateTimeOffset? CreatedAt { get; init; }
    string Currency { get; init; }
    string Id { get; init; }
    PaymentKind Kind { get; init; }
    string? OfferId { get; init; }
    PaymentProvider? Provider { get; init; }
    PaymentStatus Status { get; init; }
  // The access-control answer [PaymentsRequireEntitlement] gates on. Subscription access carries ExpiresAt (period end plus a grace window) and reports Active false once it has passed; a one-time purchase never expires.
  sealed record PaymentEntitlement
    ctor(string OfferId, bool Active, DateTimeOffset? ExpiresAt, EntitlementSource Source)
    bool Active { get; init; }
    DateTimeOffset? ExpiresAt { get; init; }
    string OfferId { get; init; }
    EntitlementSource Source { get; init; }
  sealed record PaymentEvent
    ctor(string EventId, PaymentProvider? Provider, PaymentEventType? Type, DateTimeOffset? OccurredAt, long Sequence, string PayloadJson)
    string EventId { get; init; }
    DateTimeOffset? OccurredAt { get; init; }
    string PayloadJson { get; init; }
    PaymentProvider? Provider { get; init; }
    long Sequence { get; init; }
    PaymentEventType? Type { get; init; }
    JsonElement Payload()
  enum PaymentEventType
    PaymentAuthorized
    PaymentPaid
    PaymentRefunded
    PaymentCanceled
    PaymentExpired
    PaymentFailed
    SubscriptionActivated
    SubscriptionUpdated
    SubscriptionRenewed
    SubscriptionRenewalFailed
    SubscriptionCanceled
    CatalogUpdated
  enum PaymentKind
    Unknown
    OneTime
    Subscription
  sealed record PaymentLink
    ctor(string Url, string Reference, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    string Reference { get; init; }
    string Url { get; init; }
  sealed record PaymentOffer
    ctor(string OfferId, string Name, IReadOnlyList<PaymentPrice> Prices)
    string Name { get; init; }
    string OfferId { get; init; }
    IReadOnlyList<PaymentPrice> Prices { get; init; }
  // Interval and IntervalCount are meaningful only when Kind is PriceKind.Recurring; a one-time price reports PriceInterval.Unknown.
  sealed record PaymentPrice
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval Interval, int? IntervalCount)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  enum PaymentProvider
    Stripe
    Mollie
    Surfboard
  // Url is a provider-hosted receipt page. Pdf holds downloadable PDF bytes only when the provider exposes one; today every provider returns a hosted URL only, so Pdf is null.
  sealed record PaymentReceipt
    ctor(string? Url, byte[]? Pdf, string? PdfContentType)
    byte[]? Pdf { get; init; }
    string? PdfContentType { get; init; }
    string? Url { get; init; }
  // Enqueued counts the provider objects queued for re-processing; their effects arrive asynchronously as normal payment events.
  sealed record PaymentReconcileResult
    ctor(PaymentProvider? Provider, int Enqueued)
    int Enqueued { get; init; }
    PaymentProvider? Provider { get; init; }
  sealed record PaymentRefund
    ctor(string Reference, RefundStatus Status)
    string Reference { get; init; }
    RefundStatus Status { get; init; }
  enum PaymentStatus
    Unknown
    Pending
    Paid
    Failed
    Canceled
  sealed record PaymentSubscription
    ctor(string Id, PaymentProvider? Provider, SubscriptionStatus Status, string? OfferId, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd)
    bool CancelAtPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    string Id { get; init; }
    string? OfferId { get; init; }
    PaymentProvider? Provider { get; init; }
    SubscriptionStatus Status { get; init; }
  // On missing access it DENIES with the stable code payments_entitlement_required — catch that in the UI to open a payment link. The customer is resolved from PolicyCallContext.UserId, so a call with no user denies with payments_no_user.
  sealed class PaymentsRequireEntitlementAttribute : PolicyAttribute
    ctor(string offerId)
    string OfferId { get; }
    override IFunctionPolicy CreatePolicy()
  // Reached via app.Payments; one instance per app. Every command takes an optional per-call provider; with none given it uses DefaultProvider or, failing that, the space's enabled provider. The service holds no payment state — every read hits the backend except the synchronous IsEntitled.
  sealed class PaymentsService : AsyncLocalInstance<PaymentsService>
    ctor()
    // Off by default: a payment link for a guest throws InvalidOperationException, because the guest's device-scoped user id changes when they sign in, orphaning the payment and its entitlement. Enable only for purchases that may stay behind (e.g. anonymous tips).
    bool AllowAnonymousPayments { get; set; }
    string? DefaultCancelUrl { get; set; }
    // Leave null (the default) so each command uses the space's enabled provider; set it only to pin one provider for an app with several enabled. A per-call provider argument overrides it.
    PaymentProvider? DefaultProvider { get; set; }
    string? DefaultSuccessUrl { get; set; }
    // Cancels at period end by default; pass immediate to end it now. The entitlement lapses only when the cancellation takes effect.
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Moves subscriptionId to newOfferId (another recurring offer, same currency and interval). On an upgrade (pricier offer) the prorated difference is charged now and the new offer's entitlement is granted immediately; on a downgrade nothing is charged, the current (higher) plan stays available until the next renewal, and renewals then bill the new price. The previous offer's entitlement is left to lapse at its stored expiry. immediateChargeMinor overrides the platform's computed proration for Mollie/Surfboard (developer-owned pricing); it is rejected for Stripe, which prorates natively. Returns a SubscriptionOfferChange whose SubscriptionOfferChange.Changed is false when the subscription was already on the requested offer.
    Task<SubscriptionOfferChange> ChangeSubscriptionOfferAsync(string subscriptionId, string newOfferId, long? immediateChargeMinor = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Idempotent on OfferSpec.OfferId — calling again updates the offer. Stripe provisions a Product + Price; catalog-less providers (Mollie, Surfboard) store the offer on the platform.
    Task<PaymentOffer> CreateOfferAsync(OfferSpec offer, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Paying grants the customer an entitlement for the offer; a recurring offer also starts a subscription. customerKey defaults to the current user. Throws for an anonymous (not signed-in) customer unless AllowAnonymousPayments is set. allowPromotionCodes is honored by Stripe only; other providers ignore it. amountMinorOverride charges the given amount (in minor units) instead of the offer's stored price while still granting the offer's entitlement — for developer-computed pricing such as an upgrade credit. It is supported on one-time offers only; supplying it for a recurring offer is rejected (use ChangeSubscriptionOfferAsync to change a subscription's plan).
    Task<PaymentLink> CreatePaymentLinkAsync(string offerId, string? customerKey = null, string? email = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, bool allowPromotionCodes = false, long? amountMinorOverride = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Charges an ad-hoc amount and grants NO entitlement — reach for the offer overload when a purchase should unlock access. customerKey defaults to the current user; throws for an anonymous customer unless AllowAnonymousPayments is set. allowPromotionCodes is Stripe-only.
    Task<PaymentLink> CreatePaymentLinkAsync(long amountMinor, string currency, string? customerKey = null, string? description = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, bool allowPromotionCodes = false, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Makes a backend call; customerKey defaults to the current user. For gating UI every render, prefer the synchronous IsEntitled instead.
    Task<PaymentEntitlement> GetEntitlementAsync(string offerId, string? customerKey = null, CancellationToken cancellationToken = default)
    // No backend call — safe to read every render, and reading it inside a UI lambda re-renders when the entitlement changes. The first read for an unseen offer returns false and warms the cache in the background, flipping to the real value on a later render. customerKey defaults to the current user.
    bool IsEntitled(string offerId, string? customerKey = null)
    Task<IReadOnlyList<PaymentOffer>> ListOffersAsync(CancellationToken cancellationToken = default)
    // customerKey defaults to the current user.
    Task<IReadOnlyList<Payment>> ListPaymentsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // customerKey defaults to the current user.
    Task<IReadOnlyList<PaymentSubscription>> ListSubscriptionsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // Recovery path for a missed provider webhook or an app that was offline. Eventually consistent: pulled objects surface as ordinary PaymentEventReceived pushes and entitlement refreshes. A reference (a payment link's checkout-session reference or a subscription id) scopes the pull to one object; otherwise the customer's recent objects, or the space's recent window when no customer is in scope.
    Task<PaymentReconcileResult> ReconcileAsync(string? customerKey = null, string? reference = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Refunds in full by default, or partially via amountMinor. A refund does NOT revoke an entitlement the original payment granted.
    Task<PaymentRefund> RefundAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Returns false if no such active offer existed.
    Task<bool> RemoveOfferAsync(string offerId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    Task<PaymentReceipt> RequestReceiptAsync(string paymentId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Valid only while the subscription is cancel-at-period-end and its paid period has not ended; an immediate cancel or a fully-ended subscription needs a new checkout. Returns a SubscriptionResume whose SubscriptionResume.SubscriptionId may differ from the input when the provider recreated the subscription (Mollie).
    Task<SubscriptionResume> ResumeSubscriptionAsync(string subscriptionId, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Subscribing registers the receiver on first use.
    event Func<PaymentEvent, Task>? PaymentEventReceived
  enum PlanChangeDirection
    Unknown
    Upgrade
    Downgrade
  enum PriceInterval
    Unknown
    Day
    Week
    Month
    Year
  enum PriceKind
    Unknown
    OneTime
    Recurring
  enum RefundStatus
    Unknown
    Pending
    Succeeded
    Failed
  // Changed is false when the subscription was already on the requested offer (a no-op). On an upgrade ProrationAmountMinor was charged immediately and the new plan is active now; on a downgrade nothing is charged and the new plan takes over at the next renewal (Effective is "immediate" or "next_cycle").
  sealed record SubscriptionOfferChange
    ctor(bool Changed, PlanChangeDirection? Direction, long ProrationAmountMinor, string? ProratedChargeRef, string? Currency, string? Effective, PaymentProvider? Provider)
    bool Changed { get; init; }
    string? Currency { get; init; }
    PlanChangeDirection? Direction { get; init; }
    string? Effective { get; init; }
    string? ProratedChargeRef { get; init; }
    long ProrationAmountMinor { get; init; }
    PaymentProvider? Provider { get; init; }
  sealed record SubscriptionResume
    ctor(bool Resumed, string? SubscriptionId, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    bool Resumed { get; init; }
    string? SubscriptionId { get; init; }
  enum SubscriptionStatus
    Unknown
    Incomplete
    IncompleteExpired
    Trialing
    Active
    PastDue
    Unpaid
    Paused
    Canceled

# Ikon.Connectors Public API

namespace Ikon.Connectors
  sealed class ConnectorException : Exception
    ctor(string provider, string message, int? statusCode = null)
    string Provider { get; }
    // HTTP status of the failed response, when the failure was an HTTP error. Lets a caller distinguish a permanent 401/403 (reconnect required) from a transient failure.
    int? StatusCode { get; }
  // Repositories are addressed as "owner/name".
  sealed class GitHub
    ctor(string token, HttpClient? http = null)
    // Works on both issues and pull requests; returns the created comment's html_url.
    Task<string> CommentAsync(string repo, int number, string body, CancellationToken ct = default)
    Task<GitHubIssue> CreateIssueAsync(string repo, string title, string body, CancellationToken ct = default)
    Task<GitHubIssue> GetIssueAsync(string repo, int number, CancellationToken ct = default)
    // Unlike the connector's JSON calls, this does NOT retry on HTTP 429 (rate limit); a 429 surfaces a ConnectorException immediately. A GitHub 403 may itself indicate a rate limit (check X-RateLimit-Remaining / Retry-After) rather than a permanent auth failure, so do not unconditionally treat a 403 as a dead credential.
    Task<string> GetPullRequestDiffAsync(string repo, int number, CancellationToken ct = default)
    // Ordered by update time ascending and paged to completion (bounded by maxPages). See the ListIssuesSinceAsync overload for the paging, truncation and inclusivity caveats.
    Task<IReadOnlyList<GitHubIssue>> ListIssuesSinceAsync(string repo, DateTimeOffset since, int maxPages = 50, CancellationToken ct = default)
    // The result may be silently truncated at maxPages with no signal. Detect this by comparing the result length against the page cap (maxPages × 100): if it reaches the cap, resume by calling again with since raised to the newest GitHubIssue.UpdatedAt returned. A GitHub 403 may indicate a rate limit (check X-RateLimit-Remaining / Retry-After) rather than a permanent auth failure, so do not unconditionally treat a 403 as a dead credential. since is INCLUSIVE (returns issues updated at-or-after it) while results are ordered by update time ascending, so resuming with since set to the last item's GitHubIssue.UpdatedAt re-returns every item updated in that same second. When resuming, dedupe on GitHubIssue.Number (unlike Slack's exclusive oldest).
    Task<IReadOnlyList<GitHubIssue>> ListIssuesSinceAsync(string repo, string since, int maxPages = 50, CancellationToken ct = default)
    // Unlike the connector's JSON calls, this does NOT retry on HTTP 429 (rate limit); a 429 surfaces a ConnectorException immediately.
    Task<GitHubMergeResult> MergePullRequestAsync(string repo, int number, string? commitTitle = null, CancellationToken ct = default)
  sealed record GitHubIssue
    // UpdatedAt: The raw ISO-8601 timestamp exactly as GitHub returns it — callers that page by updated use it as an opaque ordered cursor, so reformatting it would break resume-from-cursor round-trips.
    ctor(int Number, string Title, string Body, string State, string Author, string? HtmlUrl, bool IsPullRequest, IReadOnlyList<string> Labels, string UpdatedAt)
    string Author { get; init; }
    string Body { get; init; }
    string? HtmlUrl { get; init; }
    bool IsPullRequest { get; init; }
    IReadOnlyList<string> Labels { get; init; }
    int Number { get; init; }
    string State { get; init; }
    string Title { get; init; }
    string UpdatedAt { get; init; }
  sealed record GitHubMergeResult
    ctor(bool Merged, string Message)
    bool Merged { get; init; }
    string Message { get; init; }
  sealed class GitHubSkill : Skill
    ctor(GitHub gitHub)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  sealed class Slack
    ctor(string botToken, HttpClient? http = null)
    // Only Slack-owned hosts (slack.com and subdomains) are fetched. A URL pointing anywhere else — e.g. one parsed out of untrusted message text — is rejected with an ArgumentException rather than fetched, so this cannot be turned into a server-side request against an internal host, and the workspace token can never leak to an attacker-controlled server.
    Task<byte[]> DownloadFileAsync(string url, CancellationToken ct = default)
    Task<SlackConversation> GetConversationAsync(string channelId, CancellationToken ct = default)
    // Returns only the most recent limit messages (default 20) as a single bounded peek — it does not paginate. For a complete range use HistorySinceAsync.
    Task<IReadOnlyList<SlackMessage>> HistoryAsync(string channel, int limit = 20, CancellationToken ct = default)
    // The result may be silently truncated at maxPages with no signal. Because pages go backward in time, the OLDEST messages are the ones dropped, leaving a gap at the start of the range. Comparing the result length against the page cap (maxPages × pageLimit) under-counts and is NOT a reliable truncation signal: conversations.history routinely returns fewer than pageLimit per page even when more pages remain, so a genuinely truncated backfill rarely reaches the product. The certain approach is to raise maxPages until a call returns a short (unfilled) final page; on truncation, resume by calling again with oldestTs raised to the oldest ts returned.
    Task<IReadOnlyList<SlackMessage>> HistorySinceAsync(string channel, string oldestTs, int pageLimit = 200, int maxPages = 50, CancellationToken ct = default)
    // The result may be silently truncated at maxPages with no signal, so a caller cannot trust "completion" for a workspace with more conversations than the cap admits.
    Task<IReadOnlyList<SlackConversation>> ListConversationsAsync(int maxPages = 50, CancellationToken ct = default)
    // appToken: An app-level token (xapp-...), not the bot token.
    Task<string> OpenSocketUrlAsync(string appToken, CancellationToken ct = default)
    // Accepts a message object from a history page or a Socket Mode event; returns null when the object has no ts (not a message).
    static SlackMessage? ParseMessage(JsonElement message, string channel)
    // The returned SlackMessage is synthesized from the request, not fetched back: only SlackMessage.Ts and SlackMessage.Channel are populated from the server response. SlackMessage.User is always empty, SlackMessage.Subtype is always null, SlackMessage.Files is always empty, and SlackMessage.ThreadTs merely echoes the argument — callers must not read those back.
    Task<SlackMessage> PostAsync(string channel, string text, string? threadTs = null, CancellationToken ct = default)
  sealed record SlackConversation
    ctor(string Id, string Name, bool IsMember, bool IsPrivate, bool IsIm, bool IsMpim)
    string Id { get; init; }
    bool IsIm { get; init; }
    bool IsMember { get; init; }
    bool IsMpim { get; init; }
    bool IsPrivate { get; init; }
    string Name { get; init; }
  sealed record SlackFile
    ctor(string Id, string MimeType, string? DownloadUrl)
    string? DownloadUrl { get; init; }
    string Id { get; init; }
    string MimeType { get; init; }
  sealed record SlackMessage
    ctor(string Channel, string User, string Text, string Ts, string? ThreadTs = null, string? Subtype = null, IReadOnlyList<SlackFile>? Files = null)
    string Channel { get; init; }
    // Empty, never null, when the message has none.
    IReadOnlyList<SlackFile> Files { get; init; }
    string? Subtype { get; init; }
    string Text { get; init; }
    string? ThreadTs { get; init; }
    string Ts { get; init; }
    string User { get; init; }
  sealed class SlackSkill : Skill
    ctor(Slack slack)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()

# Ikon.Connectors.Google Public API

namespace Ikon.Connectors.Google
  sealed class Drive : IDisposable
    ctor(GoogleCredentials credentials)
    // Disposes the underlying Drive service and its HttpClient; construct one Drive per credential and reuse it rather than constructing per call.
    void Dispose()
    // Only files with binary content can be downloaded. Google-native Docs, Sheets and Slides (mime types application/vnd.google-apps.document, .spreadsheet, .presentation) have no binary content and Google rejects this call with HTTP 403 "Only files with binary content can be downloaded" — those require an Export, which this connector does not provide. Buffers the entire file into memory before returning, rather than streaming it — the returned stream is a fully-populated MemoryStream. Do not use it for very large files.
    Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    // Trashed files are included by default — the folder clause is only "<folder> in parents". Pass extraQuery "trashed = false" to exclude them.
    IAsyncEnumerable<DriveFile> ListAllAsync(string? folderId = null, string? extraQuery = null, CancellationToken ct = default)
    // Fetches a single page — limit caps that page, it is not a total across the folder. The query is only "<folder> in parents", so trashed files are included. For a complete listing that also filters them out, use ListAllAsync with extraQuery "trashed = false".
    Task<IReadOnlyList<DriveFile>> ListAsync(string? folderId = null, int limit = 50, CancellationToken ct = default)
    Task<DriveFile> UploadAsync(string name, string mimeType, Stream content, string? folderId = null, CancellationToken ct = default)
  sealed record DriveFile
    ctor(string Id, string Name, string MimeType, long? Size, string? WebViewLink, DateTimeOffset? ModifiedTime = null)
    string Id { get; init; }
    string MimeType { get; init; }
    DateTimeOffset? ModifiedTime { get; init; }
    string Name { get; init; }
    long? Size { get; init; }
    string? WebViewLink { get; init; }
  sealed class DriveSkill : Skill
    ctor(Drive drive)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  // ReceivedAt is DateTimeOffset.MinValue (year 0001) when Gmail supplies no internal date for the message, so guard for it before sorting or displaying.
  sealed record EmailSummary
    ctor(string Id, string ThreadId, string From, string Subject, string Snippet, DateTimeOffset ReceivedAt)
    string From { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Snippet { get; init; }
    string Subject { get; init; }
    string ThreadId { get; init; }
  // Authenticates with Google OAuth2 (refresh-token) credentials. Raw connector — no agent logic.
  sealed class Gmail : IDisposable
    ctor(GoogleCredentials credentials)
    // Disposes the underlying Gmail service and its HttpClient; construct one Gmail per credential and reuse it rather than constructing per call.
    void Dispose()
    // Returns the text/plain part when present, else the raw HTML of the text/html part, else an empty string.
    Task<string> GetBodyAsync(string id, CancellationToken ct = default)
    // Pages through the entire result set, unlike ListAsync which is capped by its limit. Bound a historical backfill with query date operators, e.g. "after:2024/01/01".
    IAsyncEnumerable<EmailSummary> ListAllAsync(string? query = null, CancellationToken ct = default)
    Task<IReadOnlyList<EmailSummary>> ListAsync(string? query = null, int limit = 20, CancellationToken ct = default)
    // to: One or more recipient addresses, comma- or semicolon-separated.
    // cc: Optional CC addresses, comma- or semicolon-separated.
    // isHtml: When true, body is sent as an HTML part; otherwise plain text.
    // throws ArgumentException: No recipient address remains after trimming empty entries from to.
    // throws ConnectorException: A recipient or CC address is malformed and cannot be parsed.
    Task<string> SendAsync(string to, string subject, string body, string? cc = null, bool isHtml = false, CancellationToken ct = default)
  sealed class GmailSkill : Skill
    ctor(Gmail gmail)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  static class GoogleAuth
    // The returned UserCredential is a third-party type from the Google.Apis.Auth NuGet package (namespace Google.Apis.Auth.OAuth2), which ships transitively with this library. Assign it as the HttpClientInitializer in any Google API service initializer (Drive, Sheets, Gmail, Calendar, ...) from the corresponding Google.Apis.* package.
    // credentials: The stored OAuth2 client and refresh-token credentials.
    // scopes: Informational only. The credential refreshes via a refresh-token grant, which never sends a scope, so this argument has no runtime effect — it does not restrict or broaden the credential. The effective scopes are whatever the refresh token was granted at consent.
    static UserCredential CredentialFor(GoogleCredentials credentials, IEnumerable<string> scopes)
    // Branch on this to stop retrying and surface a "reconnect required" state: it is true only for permanent auth failures (revoked/expired refresh token, bad client), never for transient or network errors.
    static bool IsAuthFailure(Exception ex)
  sealed record GoogleCredentials
    ctor(string ClientId, string ClientSecret, string RefreshToken)
    string ClientId { get; init; }
    string ClientSecret { get; init; }
    string RefreshToken { get; init; }

# Ikon.Connectors.Browser Public API

namespace Ikon.Connectors.Browser
  static class BrowserOperatorPersona
    static Persona Create(string name = "browser-operator", string? systemPrompt = null, LLMModel visionModel = Claude46Sonnet, Reasoning? reasoning = null)
    const string DefaultName
  // Owns the browser lifecycle: start once, dispose to release the process. Resolves a WebTarget by mark first, then accessibility role+name, then selector.
  sealed class BrowserSession : IAsyncDisposable
    ctor()
    // The last ~40 console messages, page errors, and failed requests from the page — the page's own account of why it is in whatever state it is in. Check it when a page that should render stays blank (auth failures, websocket errors, bundle errors).
    IReadOnlyList<string> ConsoleTail { get; }
    string CurrentUrl { get; }
    ValueTask DisposeAsync()
    // script is a JavaScript function-expression (e.g. "() => { ...; return 'x'; }"); the result is returned as a string.
    Task<string?> EvaluateAsync(string script)
    Task<WebActionResult> ExecuteAsync(WebAction action)
    Task<IReadOnlyList<MarkedElement>> MarkElementsAsync()
    Task NavigateAsync(string url)
    Task<byte[]> ScreenshotAsync()
    // Prefer this over ScreenshotAsync when the image enters an LLM context — a PNG's 3-5x larger payload rides along for every later turn.
    Task<byte[]> ScreenshotJpegAsync(int quality = 70)
    // Call once; throws InvalidOperationException if already started (dispose first). captureGrade renders at a 1440×900 2× viewport for high-fidelity single-shot screenshots — leave false for interactive driving, where the larger payload is pure token cost.
    // headless: Run the browser without a visible window.
    // captureGrade: High-fidelity capture mode for single-shot visual grading: 1440×900 viewport at 2x device scale, so small text, hairline borders, and gradients survive to the vision model. Leave false for agentic driving sessions — their screenshots ride along in every later LLM turn, where the 4x pixel payload is pure token cost.
    Task StartAsync(bool headless, bool captureGrade = false, CancellationToken ct = default)
  // Each action runs on the per-thread BrowserSession and returns a TEXT observation (URL + numbered interactable elements). Screenshots are saved as artifacts (references), never posted into the thread; look runs an on-demand vision pass on a referenced screenshot and returns a text description.
  sealed class BrowserSkill : Skill
    ctor(LLMModel visionModel = Claude46Sonnet)
    override string Instructions { get; }
    override string Name { get; }
    // For standalone use where the persona is registered directly on a custom orchestrator rather than driven through WebAgent.OperateAsync. Without this, every tool resolves no per-run state and returns "No active browser session." The caller owns the session's lifetime; call DetachSession when the run ends. The step trace and named outputs the tools produce accumulate against the state registered here.
    // threadId: Id of the thread whose tool calls should operate the session.
    // session: The started browser session the tools act on.
    static void AttachSession(string threadId, BrowserSession session)
    // Returns the WebRun the tools produced (steps + named outputs), or null if none was attached or the run never reached a finish. Does not dispose the session — the caller owns it. Safe to call for a thread that has no attached session.
    // threadId: Id of the thread whose attached session should be released.
    static WebRun? DetachSession(string threadId)
    override IEnumerable<Tool> Tools()
  sealed record MarkedElement
    ctor(int Mark, string Role, string Name, string Selector)
    int Mark { get; init; }
    string Name { get; init; }
    string Role { get; init; }
    string Selector { get; init; }
  abstract record WebAction
  sealed record WebAction.Click : WebAction
    ctor(WebTarget Target)
    WebTarget Target { get; init; }
  sealed record WebAction.Extract : WebAction
    ctor(WebTarget Target, string OutputName)
    string OutputName { get; init; }
    WebTarget Target { get; init; }
  sealed record WebAction.Fill : WebAction
    // Secret: Set for credentials: the live fill uses the value, but step traces and distilled flows store RedactedText in its place, so a replay must re-supply the value through its input slot rather than reusing the captured one.
    // InputName: Marks the value as a flow input slot that a replay substitutes.
    ctor(WebTarget Target, string Text, bool Secret = false, string? InputName = null)
    string? InputName { get; init; }
    bool Secret { get; init; }
    WebTarget Target { get; init; }
    string Text { get; init; }
    const string RedactedText
  sealed record WebAction.Navigate : WebAction
    ctor(string Url)
    string Url { get; init; }
  sealed record WebAction.Press : WebAction
    // Key: A key name such as "Enter" or "Escape".
    ctor(string Key)
    string Key { get; init; }
  sealed record WebAction.Scroll : WebAction
    ctor(int Dx, int Dy)
    int Dx { get; init; }
    int Dy { get; init; }
  sealed record WebActionResult
    ctor(bool Ok, string Selector, string? Extracted = null, string? Failure = null)
    string? Extracted { get; init; }
    string? Failure { get; init; }
    bool Ok { get; init; }
    string Selector { get; init; }
  // The persona named personaName must be registered on the orchestrator — build it with BrowserOperatorPersona.Create.
  static class WebAgent
    static WebFlow Distill(WebRun run, string? name = null)
    static Task<WebRun> OperateAsync(AgentThread parent, string url, string objective, WebAgentOptions? options = null, string personaName = "browser-operator", CancellationToken ct = default)
    static Task<WebReplay> ReplayAsync(WebFlow flow, IReadOnlyDictionary<string, string> inputs, bool headless = true, CancellationToken ct = default)
  sealed record WebAgentOptions
    ctor(int MaxSteps = 25, bool Headless = true)
    bool Headless { get; init; }
    int MaxSteps { get; init; }
  sealed record WebFlow
    ctor(string Name, string Origin, IReadOnlyList<WebStep> Steps, IReadOnlyList<string> Inputs)
    IReadOnlyList<string> Inputs { get; init; }
    string Name { get; init; }
    string Origin { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
  // Keeps only the steps that succeeded and parameterizes each filled field into a named input slot. Deterministic; secret fills are redacted in the produced WebFlow.
  static class WebFlowDistiller
    static WebFlow Distill(WebRun run, string? name = null)
  // Replays a distilled WebFlow deterministically (no LLM), substituting each input slot from inputs. A secret fill's slot must be supplied — a missing one fails upfront rather than typing the redaction placeholder.
  static class WebFlowPlayer
    static Task<WebReplay> ReplayAsync(BrowserSession session, WebFlow flow, IReadOnlyDictionary<string, string> inputs, CancellationToken ct = default)
  enum WebOutcome
    Succeeded
    Failed
    BudgetExhausted
  sealed record WebReplay
    // Healed: Reserved for self-healing replay, which is not yet implemented — this is currently always false, so do not branch on it expecting a meaningful value.
    ctor(bool Ok, IReadOnlyDictionary<string, string> Outputs, bool Healed)
    bool Healed { get; init; }
    bool Ok { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
  sealed record WebRun
    // Looks: Count of visual inspections — they consume agent budget without appearing in Steps, so budget analysis needs both numbers.
    ctor(WebOutcome Outcome, string Summary, IReadOnlyList<WebStep> Steps, IReadOnlyDictionary<string, string> Outputs, int Looks = 0)
    int Looks { get; init; }
    WebOutcome Outcome { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
    string Summary { get; init; }
  sealed record WebStep
    ctor(WebAction action, string resolvedSelector, bool ok)
    WebAction Action { get; init; }
    bool Ok { get; init; }
    string ResolvedSelector { get; init; }
  // Resolution tries the perception mark id first, then accessibility role + name, then a CSS/XPath selector — populate whichever are known, since the later ones are what let a replay still find the element once the marks have gone stale.
  sealed record WebTarget
    ctor(string? Role = null, string? Name = null, string? Selector = null, int? Mark = null)
    int? Mark { get; init; }
    string? Name { get; init; }
    string? Role { get; init; }
    string? Selector { get; init; }

# Ikon.Resonance Public API

namespace Ikon.Resonance
  // When Enabled, an AudioMetricsReport is published to Reports once per UpdateIntervalSeconds while packets are being recorded.
  class AudioMetrics
    ctor()
    bool Enabled { get; set; }
    bool LogMetrics { get; set; }
    double UpdateIntervalSeconds { get; set; }
    // A no-op unless Enabled is set to true first — while disabled, nothing is tracked and Reports never yields, so a caller expecting reports must enable the collector before recording.
    void RecordPacket(string streamId, double encodingTimeMs)
    void Remove(string streamId)
    // A single-consumer diagnostics stream: only the latest unread report is kept, and concurrent enumerations compete for reports.
    // cancellationToken: Ends the stream when cancelled.
    IAsyncEnumerable<AudioMetricsReport> Reports(CancellationToken cancellationToken = default)
    void Reset(string streamId)
    void ResetAll()
  sealed record AudioMetricsReport
    ctor(int StreamCount, double MinIpdMs, double AvgIpdMs, double MaxIpdMs, double JitterMs, double AvgEncodeTimeMs, double CpuUsagePercent)
    double AvgEncodeTimeMs { get; init; }
    double AvgIpdMs { get; init; }
    double CpuUsagePercent { get; init; }
    double JitterMs { get; init; }
    double MaxIpdMs { get; init; }
    double MinIpdMs { get; init; }
    int StreamCount { get; init; }
  // Supports mono and stereo audio only; sample rate conversion uses linear interpolation.
  static class AudioResampler
    // inputFrameCount: The number of input frames (samples per channel).
    // inputSampleRate: The input sample rate in Hz.
    // outputSampleRate: The desired output sample rate in Hz.
    static int CalculateResampledFrameCount(int inputFrameCount, int inputSampleRate, int outputSampleRate)
    // Stereo to mono averages both channels; mono to stereo duplicates the channel.
    // source: The source audio samples in interleaved format.
    // destination: The destination buffer for converted samples.
    // inputChannelCount: The number of input channels (1 or 2).
    // outputChannelCount: The number of output channels (1 or 2).
    static void ConvertChannels(ReadOnlySpan<float> source, Span<float> destination, int inputChannelCount, int outputChannelCount)
    // channelCount: The number of channels to check.
    static bool IsSupportedChannelCount(int channelCount)
    // source: The source audio samples in interleaved format.
    // destination: The destination buffer for resampled samples.
    // inputSampleRate: The input sample rate in Hz.
    // outputSampleRate: The output sample rate in Hz.
    // inputChannelCount: The number of input channels (1 or 2).
    // outputChannelCount: The number of output channels (1 or 2).
    static int Resample(ReadOnlySpan<float> source, Span<float> destination, int inputSampleRate, int outputSampleRate, int inputChannelCount, int outputChannelCount)
    const int MaxSupportedChannelCount = 2
  static class AudioUtils
    // Output bytes are little-endian; input is clamped to [-1, 1] first. output must be at least 2 * input.Length; throws ArgumentException otherwise.
    // input: The input buffer containing float samples.
    // output: The output buffer for raw bytes. Must be at least twice the length of input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input, Span<byte> output)
    // Output bytes are little-endian; input is clamped to [-1, 1] first.
    // input: The input buffer containing float samples.
    static byte[] ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input)
    // Input is clamped to [-1, 1] first. output must be at least input.Length; throws ArgumentException otherwise.
    // input: The input buffer containing float samples.
    // output: The output buffer for 16-bit PCM samples. Must be at least as long as input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input, Span<short> output)
    // Input is clamped to [-1, 1] first.
    // input: The input buffer containing float samples.
    static short[] ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input)
    // Normalizes to [-1, 1]. output must be at least input.Length; throws ArgumentException otherwise. Returns the sample count.
    // input: The input buffer containing 16-bit PCM samples.
    // output: The output buffer for float samples. Must be at least as long as input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertPcm16ToFloat(ReadOnlySpan<short> input, Span<float> output)
    // Output is normalized to [-1, 1].
    // input: The input buffer containing 16-bit PCM samples.
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<short> input)
    // Bytes are little-endian; input length must be a multiple of 2 and output at least input.Length / 2. Normalizes to [-1, 1].
    // input: The input buffer containing raw bytes representing 16-bit PCM samples (little-endian).
    // output: The output buffer for float samples. Must be at least half the length of input.
    // throws ArgumentException: Thrown when the input length is not a multiple of 2 or output buffer is too small.
    static int ConvertPcm16ToFloat(ReadOnlySpan<byte> input, Span<float> output)
    // Output is normalized to [-1, 1].
    // input: The input buffer containing raw bytes representing 16-bit PCM samples (little-endian).
    // throws ArgumentException: Thrown when the input length is not a multiple of 2.
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<byte> input)
    // For input normalized to [-1, 1] the result is in [0, 1]. Returns 0 for an empty span; channel layout does not matter.
    // samples: The samples to measure. Channel layout is irrelevant; all samples contribute equally.
    static float Rms(ReadOnlySpan<float> samples)
  // Decides when to interrupt the agent's speech (barge-in): the caller must produce sustained speech for a few consecutive frames, and only after a short grace period from when the agent started speaking, so the first syllables and any echo don't false-trigger.
  sealed class BargeInDetector
    ctor(int sustainedFrames = 3, double graceMs = 300.0)
    void Reset()
    bool ShouldInterrupt(bool isSpeech, bool agentSpeaking, double msSinceSpeakStart)
  enum CrossfadeCurve
    Linear
    EqualPower
  enum FadeMode
    Sequential
    Crossfade
  readonly struct GroupAudioFrame
    ctor(int participantId, PcmAudioFrame frame)
    PcmAudioFrame Frame { get; }
    int ParticipantId { get; }
    void Deconstruct(out int participantId, out PcmAudioFrame frame)
  // Each participant receives a personalized mix of all input streams except those tagged with their own id; every input stream is tagged with its owning participant id (typically a client session id) to control the exclusion. Participants must be registered with AddParticipant before they receive mixed output, streams are added/removed independently via AddStream/RemoveStream, and a participant with no streams of their own still receives output. Uses power-preserving normalization (1/sqrt(N)) and tanh soft-clipping to prevent distortion when many participants speak simultaneously.
  sealed class GroupAudioMixer : IAsyncDisposable
    ctor(GroupAudioMixerConfig? config = null)
    void AddParticipant(int participantId)
    // Re-adding a stream id that is already registered keeps its buffered audio; if the owning participantId differs (the id was reclaimed by a reconnecting participant) the ownership tag is updated so exclusion routing follows the new owner.
    void AddStream(string streamId, int participantId)
    ValueTask DisposeAsync()
    void RemoveParticipant(int participantId)
    // Discards any samples still buffered for the stream. Removing an unknown stream id is a no-op.
    void RemoveStream(string streamId)
    // The personalized mixes as a stream of 20 ms frames, paced at best-effort real time. Each tick yields one GroupAudioFrame per registered participant, except a participant whose tick mix would contain only their own audio (e.g. a lone speaker), who is skipped for that tick. Single consumer: a concurrent second enumeration throws, but the stream may be re-entered after an enumeration ends (including via an exception unwinding the consumer's loop) — this is how a pump recovers after a frame-handling failure. Yielded frames alias one reused sample buffer — consume (or copy) each frame's samples within the loop body. Cancelling cancellationToken or disposing the mixer ends the stream gracefully: each participant that received audio gets one final empty frame marked PcmAudioFrame.IsLast, then the enumeration completes without throwing.
    // cancellationToken: Ends the stream when cancelled.
    // throws InvalidOperationException: Thrown when the mixer is already streaming.
    IAsyncEnumerable<GroupAudioFrame> StreamAsync(CancellationToken cancellationToken = default)
    // Buffers interleaved samples for a registered input stream, resampling to the mixer's native 48 kHz stereo format when needed. When the stream's buffer is full the oldest samples are dropped to make room; writes to an unknown stream are dropped with a throttled warning (stream teardown races with in-flight frames, so this is not an error).
    // throws ArgumentException: channelCount is less than 1 or sampleRate is not positive.
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  // Immutable — the mixer captures these values at construction; build a new config (and mixer) to change them.
  sealed record GroupAudioMixerConfig
    ctor()
    double MaxBufferSizeMs { get; init; }
  // The middle of the three audio currencies: AudioChunk is producer audio flowing into a mixer (TTS output, synthesized samples), identified by its speech-event id; PcmAudioFrame is the paced PCM output flowing out of the mixers toward the Opus encoder, identified by its output stream id; the encoded result travels on the wire as the protocol type AudioFrame.
  readonly struct PcmAudioFrame
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, IReadOnlyList<AudioAnalysisResult>? analysisResults = null, IReadOnlyList<AudioShapeSetDeclaration>? shapeSetDeclarations = null)
    IReadOnlyList<AudioAnalysisResult>? AnalysisResults { get; }
    int ChannelCount { get; }
    AudioEncoderOptions? EncoderOptions { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    int SampleRate { get; }
    // Interleaved float PCM samples. When a frame comes from SpeechMixer.StreamAsync or GroupAudioMixer.StreamAsync this memory ALIASES a buffer the mixer reuses for the next frame, so it is valid only within the current loop iteration. To keep a frame past the loop body (queue it, hand it to another task), take a self-owned copy first with ToOwned.
    ReadOnlyMemory<float> Samples { get; }
    IReadOnlyList<AudioShapeSetDeclaration>? ShapeSetDeclarations { get; }
    string StreamId { get; }
    IReadOnlyList<int>? TargetIds { get; }
    TimeSpan TotalDuration { get; }
    // Returns a copy whose Samples are backed by a freshly allocated array rather than the mixer's reused buffer, so the copy stays valid after the enumeration advances. Use this whenever a frame from a mixer's StreamAsync must outlive the loop body — storing it, queueing it, or handing it to another task. Every other field is a value, an immutable string, or an already-owned list and is forwarded unchanged.
    PcmAudioFrame ToOwned()
  // Uses asymmetric EMA level tracking, an adaptive noise floor, and a circular pre-buffer so speech onsets are never clipped. Designed for real-time audio at typical frame sizes (20 ms). Push-based usage: call ProcessChunk per audio chunk and forward non-null results. Stream-based usage: wrap an IAsyncEnumerable<T> source with FilterAsync.
  sealed class SilenceRemover
    // sampleRate: Sample rate of the incoming audio in Hz (e.g. 48000).
    // channelCount: Number of audio channels (e.g. 1 for mono).
    // config: Optional configuration. When null, sensible defaults tuned for voice-over-IP audio are used.
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    // source: The async enumerable producing audio chunks.
    // sampleRate: Sample rate of the audio in Hz.
    // channelCount: Number of audio channels.
    // config: Optional silence remover configuration.
    // ct: Cancellation token.
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = default)
    // Returns the samples to forward — on speech onset the pre-buffered look-back audio is concatenated in front of the current chunk — or null when the chunk is silence that should be suppressed.
    // chunk: The audio samples to process. Expected to be interleaved float samples in [-1, 1].
    float[]? ProcessChunk(ReadOnlySpan<float> chunk)
    void Reset()
  // The speech threshold is computed as noiseFloor * NoiseFloorMultiplier + NoiseFloorOffset. Immutable — the remover captures the values at construction; build a new config (and remover) to change them.
  sealed record SilenceRemoverConfig
    ctor()
    // EMA smoothing factor for rising audio levels (0..1). Higher values respond faster to speech onset.
    float AttackAlpha { get; init; }
    float InitialNoiseFloor { get; init; }
    float MaxNoiseFloor { get; init; }
    // How fast the noise floor adapts — during silence only — in (0..1). Keep low to prevent speech from contaminating the noise floor estimate.
    float NoiseFloorAlpha { get; init; }
    float NoiseFloorMultiplier { get; init; }
    float NoiseFloorOffset { get; init; }
    int PreBufferMs { get; init; }
    // EMA smoothing factor for falling audio levels (0..1). Lower values decay slower, holding through natural pauses in speech.
    float ReleaseAlpha { get; init; }
    int SpeechOnsetChunks { get; init; }
    int TrailingSilenceMs { get; init; }
  // Handles one speech event at a time, mixing it into precisely timed 20 ms output frames with smooth fade/crossfade transitions between events.
  sealed class SpeechMixer : IAsyncDisposable
    ctor(SpeechMixerConfig? config = null)
    AudioEncoderOptions? EncoderOptions { get; set; }
    // Whether output is currently paused (a pending Pause fade-out counts once it completes).
    bool IsPaused { get; }
    string StreamId { get; }
    // The chunk id identifies the speech event: a chunk carrying the current event's id appends to it, while a new id interrupts the current event with the configured fade. Effects, analyzers, and target ids are captured from the event's first chunk; audio is resampled to 48 kHz stereo when needed.
    // throws ArgumentException: The chunk's ChannelCount is less than 1 or its SampleRate is not positive — an object-initialized AudioChunk leaves these at 0; use the full constructor.
    void AddSamples(AudioChunk chunk, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Immediately discards all speech state — current, pending, and paused — without fading. Use for hard resets (e.g. conversation restart); prefer FadeOut for a graceful stop.
    void Clear()
    ValueTask DisposeAsync()
    // Starts fading out the current speech event over the configured fade-out duration. The event completes when the fade reaches silence. No-op when nothing is playing or a fade-out is already in progress.
    void FadeOut()
    // The duration of audio currently buffered for the given speech event, or zero when the event is unknown. Producers that generate faster than real time can use this to pace themselves and keep the bounded mixer buffer from overflowing.
    // speechEventId: The speech event id (the chunk id of the utterance)
    TimeSpan GetBufferedDuration(string speechEventId)
    // Pauses output by fading the current speech out, then holding it (buffered samples are kept) until Resume. No-op when already paused or pausing.
    void Pause()
    // Resumes paused output, fading the held speech event back in from where it stopped. No-op when not paused.
    void Resume()
    // Single consumer: a concurrent second enumeration throws, but the stream may be re-entered after an enumeration ends. Yielded frames alias one reused buffer — consume (or copy) each frame's samples within the loop body. Cancelling cancellationToken or disposing the mixer ends the stream gracefully, emitting a final PcmAudioFrame.IsLast frame when a speech event had started.
    // cancellationToken: Ends the stream when cancelled.
    // throws InvalidOperationException: Thrown when the mixer is already streaming.
    IAsyncEnumerable<PcmAudioFrame> StreamAsync(CancellationToken cancellationToken = default)
    // Returns a task that completes when the given speech event has finished playing out — its samples fully mixed into the output (pause time included), it was interrupted by a newer event, or it was discarded. Register before or after feeding the event's chunks; an already-completed event resolves immediately. The task also completes when the mixer is cleared or disposed, so callers never hang on a torn-down mixer.
    // speechEventId: The speech event id (the chunk id of the utterance)
    Task WaitForCompletionAsync(string speechEventId)
  // Immutable — the mixer captures these values at construction; build a new config (and mixer) to change them.
  sealed record SpeechMixerConfig
    ctor()
    CrossfadeCurve CrossfadeCurve { get; init; }
    double EndPaddingMs { get; init; }
    double FadeInMs { get; init; }
    FadeMode FadeMode { get; init; }
    double FadeOutMs { get; init; }
    // Upper bound only; the queue grows on demand from a small size. Samples added beyond this bound are dropped with a throttled warning, never thrown.
    double MaxBufferSizeMs { get; init; }
    // Caps effect tail padding in case an effect's output never decays below PaddingThreshold.
    double MaxPaddingTimeMs { get; init; }
    // RMS threshold below which effect tail padding stops. Default is 0.001 (~-60 dB), meaning padding continues until output is essentially silent.
    double PaddingThreshold { get; init; }
  // The segmentation an always-listening voice app needs between "raw mic frames" and "transcribe and respond". Deterministic: time is counted in received samples, not wall-clock, so the same frame sequence always produces the same events; this assumes the source keeps delivering frames during silence (true for platform mic capture, which streams continuously while active). Push-based usage: call Process per audio chunk and act on the returned event. Stream-based usage: wrap an IAsyncEnumerable<T> source with DetectAsync.
  sealed class TurnDetector
    // sampleRate: Sample rate of the incoming audio in Hz (e.g. 48000).
    // channelCount: Number of audio channels (e.g. 1 for mono).
    // config: Optional configuration. When null, defaults tuned for conversational voice are used.
    ctor(int sampleRate, int channelCount, TurnDetectorConfig? config = null)
    // When the source completes, a still-open turn is flushed as a final TurnEventKind.TurnEnded event.
    // source: The async enumerable producing audio chunks.
    // sampleRate: Sample rate of the audio in Hz.
    // channelCount: Number of audio channels.
    // config: Optional turn detector configuration.
    // ct: Cancellation token.
    static IAsyncEnumerable<TurnEvent> DetectAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, TurnDetectorConfig? config = null, CancellationToken ct = default)
    // Reports the end of the audio stream. A confirmed turn still in progress is finalized and returned as a TurnEventKind.TurnEnded event; otherwise returns null. The detector is reset either way.
    TurnEvent? Flush()
    // Processes one audio chunk (interleaved float samples in [-1, 1]) and returns the transition it caused, or null when nothing changed.
    TurnEvent? Process(ReadOnlyMemory<float> samples)
    void Reset()
  // Immutable — construct a new config (and detector) instead of mutating a shared instance.
  sealed record TurnDetectorConfig
    ctor()
    // Tuning for the built-in level gate and the onset pre-buffer (SilenceRemoverConfig.PreBufferMs). Only the level-tracking and pre-buffer fields apply; the onset/trailing fields belong to SilenceRemover. When null, SilenceRemover defaults are used except SilenceRemoverConfig.ReleaseAlpha is raised to 0.3 — turn detection needs the level to fall promptly when speech stops (the hold-through-pauses role is played by TurnEndSilence instead), where the slow default would add noticeable latency to every turn end.
    SilenceRemoverConfig? GateConfig { get; init; }
    // Maximum turn length; a turn still running at this point is force-ended.
    TimeSpan MaxTurnDuration { get; init; }
    // Minimum cumulative speech required before a turn is confirmed. Shorter bursts (coughs, clicks) are discarded without producing any events.
    TimeSpan MinSpeechDuration { get; init; }
    // Silence duration after which the turn has probably ended and a TurnEventKind.SpeculativeTurnEnd fires, letting downstream work start before the turn end is certain. Must be shorter than TurnEndSilence. Null disables speculative turn ends.
    TimeSpan? SpeculativeSilence { get; init; }
    // Optional external speech classifier (e.g. a neural VAD such as Silero) that replaces the built-in adaptive level gate. Receives one chunk of interleaved float PCM and returns whether it contains speech. Null uses the built-in gate.
    Func<ReadOnlyMemory<float>, bool>? SpeechClassifier { get; init; }
    // Silence duration that ends a turn. This window — not the level gate — provides the "hold through natural pauses" behavior, so mid-sentence breaths don't split a turn.
    TimeSpan TurnEndSilence { get; init; }
  // Samples carries the utterance audio (interleaved float PCM, including pre-buffered onset audio) for TurnEventKind.SpeculativeTurnEnd and TurnEventKind.TurnEnded and is empty for the other kinds.
  readonly struct TurnEvent
    TimeSpan Duration { get; }
    TurnEventKind Kind { get; }
    float[] Samples { get; }
  enum TurnEventKind
    // The user has produced sustained speech (at least TurnDetectorConfig.MinSpeechDuration).
    SpeechStarted
    // Silence has lasted TurnDetectorConfig.SpeculativeSilence — the turn has probably ended. Carries the utterance audio so far, so downstream work (transcription, a reply) can start early. Followed by either SpeechResumed (the guess was wrong) or TurnEnded.
    SpeculativeTurnEnd
    // Speech resumed after a SpeculativeTurnEnd — discard the speculative result.
    SpeechResumed
    // The turn has ended: silence lasted TurnDetectorConfig.TurnEndSilence (or the turn hit TurnDetectorConfig.MaxTurnDuration). Carries the complete utterance audio.
    TurnEnded
  // Samples are written incrementally; the WAV header is finalized when the file is first accessed, after which adding samples throws.
  class WavFile : IDisposable
    // sampleRate: The sample rate in Hz (e.g., 44100, 48000).
    // channelCount: The number of audio channels (1 for mono, 2 for stereo).
    // sampleFormat: The sample format to use for the WAV file.
    ctor(int sampleRate, int channelCount, WavFile.SampleFormat sampleFormat)
    // samples: The interleaved audio samples to add.
    // throws InvalidOperationException: Thrown if the file has been finalized or the sample format is not Short.
    void AddSamples(ReadOnlySpan<short> samples)
    // samples: The interleaved audio samples to add.
    // throws InvalidOperationException: Thrown if the file has been finalized or the sample format is not Float.
    void AddSamples(ReadOnlySpan<float> samples)
    byte[] AsArray()
    // Gets the WAV file as a fresh readable stream over a copy of the data. The returned stream is independent of this WavFile, so it survives disposal of the builder and each call returns its own stream.
    Stream AsStream()
    void Dispose()
    // filePath: The path where the WAV file will be saved.
    void SaveToFile(string filePath)
  enum WavFile.SampleFormat
    Short
    Float

namespace Ikon.Resonance.Analysis
  readonly struct AudioAnalysisResult
    ctor(uint setId, IReadOnlyList<float> values)
    uint SetId { get; }
    // The analysis values for this shape set. Analyzers may reuse the backing storage between frames — copy the values if you need them beyond the current frame.
    IReadOnlyList<float> Values { get; }
  readonly struct AudioShapeSetDeclaration
    ctor(uint setId, string name, IReadOnlyList<string> shapeNames)
    string Name { get; }
    uint SetId { get; }
    IReadOnlyList<string> ShapeNames { get; }
  interface IAudioAnalyzer
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    // sampleRate: Mixer output sample rate.
    // channelCount: Mixer output channel count.
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)
  interface IAudioAnalyzerInstance
    // buffer: The audio buffer to analyze (interleaved samples).
    AudioAnalysisResult Analyze(ReadOnlySpan<float> buffer)
    void Reset()
  // Produces MouthOpenY (0-1) from RMS and MouthForm (-1 to +1) from spectral analysis.
  sealed class VisemeAnalyzer : IAudioAnalyzer
    ctor()
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)

namespace Ikon.Resonance.Effects
  sealed class BitCrusherAudioEffect : IAudioEffect
    ctor()
    ctor(int bitDepth, int downsampleFactor, float mix)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class ChorusAudioEffect : IAudioEffect
    ctor()
    ctor(float baseDelayMs, float depthMs, float rateHz, float mix, float stereoPhaseOffsetDegrees = 90f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class DelayAudioEffect : IAudioEffect
    ctor()
    ctor(float delayMs, float feedback, float mix, float feedbackDamping = 0.25f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  interface IAudioEffect
    // sampleRate: Mixer output sample rate.
    // channelCount: Mixer output channel count.
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  interface IAudioEffectInstance
    // buffer: The audio buffer to transform.
    void Process(Span<float> buffer)
    void Reset()
  // The parameterless constructor yields a natural small-room reverb (four delay lines, 120–320 ms). For the array constructor, the feedbacks/mixes/delayTimesMs/cutoffFrequencies arrays must all be the same length (one entry per delay line): delay time sets perceived room size, feedback (< 1.0) sets tail length, mix the wet blend, and cutoff damps highs inside the feedback loop.
  sealed class ReverbAudioEffect : IAudioEffect
    ctor()
    // roomSize: Room size from 0 (tiny) to 1 (cathedral). Scales delay times.
    // decay: Reverb tail decay from 0 (short) to 1 (long). Scales feedback.
    // damping: High-frequency damping from 0 (bright) to 1 (dark/muffled).
    // mix: Wet/dry mix from 0 (dry) to 1 (fully wet).
    ctor(float roomSize, float decay, float damping, float mix)
    ctor(IReadOnlyList<float> feedbacks, IReadOnlyList<float> mixes, IReadOnlyList<float> delayTimesMs, IReadOnlyList<float> cutoffFrequencies)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class RobotVoiceAudioEffect : IAudioEffect
    ctor()
    ctor(float carrierFrequencyHz, float mix, float drive)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class SaturationAudioEffect : IAudioEffect
    ctor()
    ctor(float drive, float mix)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class TelephoneAudioEffect : IAudioEffect
    ctor()
    ctor(float lowCutHz, float highCutHz, float mix, float drive)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class TremoloAudioEffect : IAudioEffect
    ctor()
    ctor(float rateHz, float depth, float mix, float stereoPhaseOffsetDegrees = 90f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
