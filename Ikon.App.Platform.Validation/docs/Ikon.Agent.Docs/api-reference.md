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
    // Like AskAsync but with an explicit model override.
    static Task<string> AskAsync(string command, LLMModel model, CancellationToken ct = default)
    // Like AskAsync but with the model given by name string — resolves user-registered custom models (see CustomModels) as well as built-in ones.
    static Task<string> AskAsync(string command, string model, CancellationToken ct = default)
    // Asks the model for JSON matching T's schema; defaults to LLMModel.Claude45Haiku. Throws EmergenceStoppedException when the run stops, completes without a result, or returns invalid JSON.
    static Task<T> AskAsync<T>(string command, CancellationToken ct = default) where T : class
    // Like AskAsync<T> but with an explicit model override.
    static Task<T> AskAsync<T>(string command, LLMModel model, CancellationToken ct = default) where T : class
    // Like AskAsync<T> but with the model given by name string — resolves user-registered custom models (see CustomModels) as well as built-in ones.
    static Task<T> AskAsync<T>(string command, string model, CancellationToken ct = default) where T : class
    static EmergeRun<T> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> BestOf<T>(string model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> BestOf<T>(string model, KernelContext context, Action<BestOfOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    // Return this from a tool body to end the run right after the current tool batch, with toolResult fed to the transcript as the tool result. The run completes with a default result.
    static EndRun<TValue> EndRun<TValue>(TValue toolResult)
    // Return from a tool body to end the run after the current tool batch; the completion is recorded as a plain marker with no value.
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
    // Like Run<T> but with the model given by name string — resolves user-registered custom models (see CustomModels) as well as built-in ones.
    static EmergeRun<T> Run<T>(string model, Action<EmergePass<T>> configure, CancellationToken ct = default)
    // Like Run<T> but with an explicit ILLM (e.g. a mock for testing).
    static EmergeRun<T> Run<T>(LLMModel model, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static EmergeRun<T> Run<T>(string model, KernelContext context, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    // Navigates a document tree to find the sections relevant to the context, returning a TreeSearchResult — the sections the navigator marked relevant plus its final reasoning.
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
    // Model by name string for this pass — resolves user-registered custom models (see CustomModels) as well as built-in ones. Wins over Model when both are set; null inherits the run's model.
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
    // Model by name string — resolves user-registered custom models (see CustomModels) as well as built-in ones. Wins over Model when both are set.
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
    int MaxParallel { get; set; }
    EmergeScope<T> MergerScope { get; }
    Action<AgentScope<T>>? SolverConfig { get; set; }
    int SolverCount { get; set; }
    void Merger(Action<EmergeScope<T>> configure)
    void Solver(Action<AgentScope<T>> configure)
  // One tree section the navigator marked relevant, with the reason it gave.
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
    // Splits Input into the chunks to map over.
    Func<TInput, IEnumerable<TInput>>? Split { get; set; }
    void Map(Action<EmergeScope<TMapped>> configure)
    void Reduce(Action<EmergeScope<TResult>> configure)
  // Call ConnectAsync before reading Tools or calling a tool — it performs the MCP handshake and populates the tool list. Uses Streamable HTTP transport.
  sealed class McpClient : IDisposable
    ctor(string endpoint, Dictionary<string, string>? headers = null)
    IReadOnlyList<McpTool> Tools { get; }
    // Calls an MCP tool by name with the given JSON arguments.
    Task<string> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = default)
    // Returns the content plus a pagination cursor; pass a cursor from a previous response to fetch the next page.
    Task<McpToolResult> CallToolRawAsync(string name, JsonElement arguments, string? cursor = null, CancellationToken ct = default)
    // Initializes the MCP session and discovers available tools.
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
  // The navigator's structured verdict at the end of a TreeSearch run.
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
  sealed class TreeSearchOptions : EmergeScope<TreeSearchResult>
    ctor()
    TreeIndex? Index { get; set; }
    int MaxResults { get; set; }
    int MaxSteps { get; set; }
    EmergeScope<NavigationDecision> NavigatorScope { get; }
    string Query { get; set; }
    void Navigator(Action<EmergeScope<NavigationDecision>> configure)
  // Result of a TreeSearch run: the sections the navigator marked relevant, plus its final reasoning.
  sealed record TreeSearchResult
    ctor(List<FoundSection> Sections, string ReasoningTrace = "")
    string ReasoningTrace { get; init; }
    List<FoundSection> Sections { get; init; }

namespace Ikon.AI.Emergence.Structured
  // Tag matching is case-insensitive and tolerates attributes and formatting variations.
  static class StructuredTagParser
    // Returns the first occurrence's inner content, or null if the tag is absent.
    static string? GetTagContent(string content, string tagName)
    // Check if content contains a specific tag
    static bool HasTag(string content, string tagName)
    // Parse content and extract structured blocks for the specified tag names
    static StructuredTagParser.ParsedResponse Parse(string content, params string[] tagNames)
  // A parsed block from the content
  sealed record StructuredTagParser.ParsedBlock
    ctor(string TagName, string Content, int StartIndex, int EndIndex)
    string Content { get; init; }
    int EndIndex { get; init; }
    int StartIndex { get; init; }
    string TagName { get; init; }
  // Complete parsed response with plain text and extracted blocks
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
    // Builds the tree index for a document, as an EmergeRun<T> — awaitable for the finished index, enumerable for the event stream, just like Emerge.TreeSearch and the other Emerge patterns.
    static EmergeRun<TreeIndex> BuildAsync(LLMModel model, string content, TreeIndexOptions? options = null, CancellationToken ct = default)
    // Like BuildAsync but with the model given by name string — resolves user-registered custom models (see CustomModels) as well as built-in ones.
    static EmergeRun<TreeIndex> BuildAsync(string model, string content, TreeIndexOptions? options = null, CancellationToken ct = default)
    // Like BuildAsync but reads the document through an IContentReader.
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
  // Downloads URL-delivered generation results. When a result's ResultKind is ResultKind.Url the payload lives behind a signed download link valid for roughly one hour; GetDataAsync returns the bytes either way, downloading transparently when needed.
  static class AssetOutputs
    static Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default)
    // Returns the result's payload bytes: Data when delivered inline, otherwise downloaded from Url.
    static Task<byte[]> GetDataAsync(this IResultPayload result, CancellationToken cancellationToken = default)
  // The name selects the model in the category's string-based APIs (e.g. new LLM("my-model")). An empty ApiKey means the endpoint needs no authentication header.
  abstract class CustomModel
    // API key for the endpoint. Empty means no authentication header is sent (e.g. a local Ollama instance).
    string ApiKey { get; init; }
    // Model name sent to the endpoint in the request payload. Defaults to Name when left unset.
    string ApiModelName { get; init; }
    // Full URL of the endpoint, e.g. http://localhost:8000/v1/chat/completions.
    required string EndpointUrl { get; init; }
    // Name the model is registered and selected by. Must not collide with a built-in model name and must not contain dots or whitespace.
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
    // True when the name is registered in any category.
    bool IsRegistered(string name)
    // Registers a custom LLM endpoint, selectable with new LLM(name) and the string-model overloads of Emerge.
    void Register(CustomLLMModel model)
    // Registers a custom embedding endpoint, selectable with new EmbeddingGenerator(name).
    void Register(CustomEmbeddingModel model)
    // Registers a custom rerank endpoint, selectable with new Reranker(name).
    void Register(CustomRerankModel model)
    // Registers a custom classification endpoint, selectable with new Classifier(name).
    void Register(CustomClassificationModel model)
    // Removes the named model from every category it is registered in. Returns true when at least one registration was removed.
    bool Unregister(string name)
  // A generation result payload delivered either as inline bytes or as a short-lived download URL, as told by Kind.
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
  // A reference clip for video generation: footage the model is shown rather than asked to invent, addressed from the prompt the way reference images are. What it is *for* is the prompt's business — carrying a subject's appearance across a cut, holding a camera move, or regenerating a stretch of an existing film with one thing changed. Supply the clip exactly one way: Data (with MimeType), Url, or AssetUri (resolved automatically). Providers impose their own length and size limits.
  sealed record InputVideo
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  // Attributes a failed model call to a ModelFailureKind.
  static class ModelFailure
    static ModelFailureKind Classify(Exception exception)
  // Why a model call failed, independent of the modality that raised it. The retryable/non-retryable split answers "should this call be tried again"; it deliberately says nothing about the cause, so a removed model, an unpaid account and a model that merely answered badly all land in the same bucket. This answers "what does the failure say about the model", which is what decides whether a human has to act.
  enum ModelFailureKind
    // The failure could not be attributed. Callers that gate on this should treat it as a real failure: an unrecognised error is far more likely to be a genuine defect than a benign one.
    Unknown
    // The call never reached a verdict about the model: transport error, timeout, throttling or a provider-side fault. Says nothing about whether the model is healthy.
    Transient
    // The provider no longer serves this model id. The model has been removed, renamed or retired and the configuration has to be updated.
    Unavailable
    // The model exists but this account cannot call it: missing or rejected credentials, exhausted credits, or a quota that is not a transient rate limit. An operator has to act, but nothing is wrong with the model or the code.
    AccessDenied
    // The model answered and the answer did not meet the contract: no content, an unusable tool call, or output that failed validation. Non-deterministic by nature and often not reproducible on the next call.
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
    // A handler that resolves the host itself and connects only to a public address.
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
    // Sentinel for user-registered custom models (see CustomModels); not directly usable — select custom models by their registered name string.
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
  // Request/response format a custom classification endpoint speaks.
  enum CustomClassificationApi
    // OpenAI moderations format (/v1/moderations).
    OpenAI
    // Mistral moderations format.
    Mistral
  // Configuration for a user-provided custom classification endpoint, registered via CustomModels.Register and selected by name with new Classifier(name).
  sealed class CustomClassificationModel : CustomModel
    ctor()
    // Request/response format the endpoint speaks.
    required CustomClassificationApi Api { get; init; }
    // True when the endpoint accepts image inputs.
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
  // Configuration for database info extraction.
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
    // Regex patterns for table names to exclude.
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
    // Value of the named column, or null. Null is returned both for a genuine SQL NULL and for a column that is not present — use TryGetValue to tell the two apart.
    object? this[string column] { get; }
    // Looks up a column by name. Returns false only when no such column exists; a column present but holding SQL NULL returns true with value set to null.
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
    // Best-effort guard that rejects LLM-authored SQL carrying a write/side-effect keyword or a table outside allowedTables. It is a keyword blocklist plus a FROM/JOIN allowlist, NOT a dialect-aware parser, so it does not prove the statement is side-effect free. Where the query runs against real data, back it with a read-only transaction or role.
    static void ValidateReadOnly(string sql, IReadOnlySet<string> allowedTables)

namespace Ikon.AI.DepthEstimation
  sealed class DepthEstimator : IDepthEstimator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(DepthEstimatorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // Estimate depth for one image — the instance form of the EstimateAsync one-shot, for when you already hold an estimator. Reach for EstimateDepthAsync when the request needs any other DepthEstimatorConfig field.
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
  // Request/response format a custom embedding endpoint speaks.
  enum CustomEmbeddingApi
    // OpenAI embeddings format (/v1/embeddings) — also spoken by most self-hosted embedding servers.
    OpenAI
    // Cohere embed format.
    Cohere
    // Mistral embeddings format.
    Mistral
    // Google Vertex prediction format.
    Google
    // Jina embeddings format.
    Jina
    // Voyage embeddings format.
    Voyage
  // Configuration for a user-provided custom embedding endpoint, registered via CustomModels.Register and selected by name with new EmbeddingGenerator(name).
  sealed class CustomEmbeddingModel : CustomModel
    ctor()
    // Request/response format the endpoint speaks.
    required CustomEmbeddingApi Api { get; init; }
    // Dimension of the returned embedding vectors.
    required int EmbeddingVectorSize { get; init; }
    // Maximum number of inputs per request; larger batches are split automatically.
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
    // Embed a batch of texts — the instance form of the EmbedAsync one-shot, for when you already hold a generator. Reach for GenerateEmbeddingsAsync when the request needs any other EmbeddingGeneratorConfig field (batch cap, timeout).
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
    // Sentinel for user-registered custom models (see CustomModels); not directly usable — select custom models by their registered name string.
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
    // Calculates the element-wise average embedding from a list of embeddings. Each embedding must be a float array of the same length.
    // embeddings: List of embeddings (each as a float array)
    static float[] CalculateAverageEmbedding(IList<float[]> embeddings)
    // Calculates the cosine similarity between two vectors.
    // throws ArgumentException: The vectors differ in length, or either vector has zero magnitude (e.g. a blank or failed embedding), for which cosine similarity is undefined. Guard degenerate vectors before calling when scoring in a loop.
    static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Calculates the dot product of two vectors.
    static float CalculateDotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Calculates the Euclidean distance between two vectors.
    static float CalculateEuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // For each embedding in the list, finds the k nearest neighbors (using Euclidean distance).
    // embeddings: List of embeddings (each as a float array)
    // k: Number of neighbors to retrieve for each embedding
    static List<List<VectorMath.Neighbor>> CalculateKNearestNeighbors(IList<float[]> embeddings, int k)
    // Calculates the magnitude (L2 norm) of a vector.
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
    // Convert one file's bytes to PDF — the instance form of the ConvertToPdfAsync one-shot, for when you already hold a converter. Reach for ConvertToPdfAsync when the request needs any other FileConverterConfig field.
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
    // True when the model can produce output with a transparent background (ImageGeneratorConfig.Background = ImageBackground.Transparent). Requesting transparency from a model without it throws instead of failing at the provider.
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
    // Generate one image from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateImageAsync when the request needs any other ImageGeneratorConfig field (input images, size, image count).
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
    Flux1Schnell
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
    // Segment one image against a prompt — the instance form of the SegmentAsync one-shot, for when you already hold a segmenter. Reach for SegmentImageAsync when the request needs any other ImageSegmenterConfig field.
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
    // Upscale one image — the instance form of the UpscaleAsync one-shot, for when you already hold an upscaler. Reach for UpscaleImageAsync when the request needs any other ImageUpscalerConfig field.
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
    // A fresh, blank `KernelContext` — equivalent to `new KernelContext()`. Prefer this or `new KernelContext()` over `default(KernelContext)`: `default` leaves the collections unset, though the mutation helpers below tolerate it. Provided as a named constant for code generated against frameworks that expect an `.Empty` affordance.
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
    // Runs the model over this context AND executes registered functions in-loop: it wraps ILLM.GenerateAsync and, whenever the model calls a function whose result is produced inline, runs it and feeds the output back into the event stream (recursing until the model stops calling functions). Prefer this when the context has functions registered. Use ILLM.GenerateAsync directly for the RAW provider stream that never runs a tool.
    IAsyncEnumerable<LLMEvent> GenerateAsync(ILLM llm, CancellationToken cancellationToken = default)
    KernelContext KeepMessagesMax(int count)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
  // Consume by switching on the concrete record case; forward any case you do not handle unchanged so downstream consumers still receive it.
  abstract record LLMEvent
    // Name of the pipeline stage that produced this event (e.g. "generate", "generate.reasoning", "Shader.Output.AfterPass"). Combinators re-tag events they transform so the origin of each event stays visible.
    string Source { get; init; }
  // An incremental chunk of generated output audio.
  sealed record LLMEvent.AudioDelta : LLMEvent
    ctor(AudioChunk Audio)
    AudioChunk Audio { get; init; }
  // The provider-side id of the generated output audio, replayable as an AudioIdPart in a follow-up context.
  sealed record LLMEvent.AudioId : LLMEvent
    ctor(string Id)
    string Id { get; init; }
  // The transcript of generated output audio.
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
  // Generation was stopped by a content-safety classifier.
  sealed record LLMEvent.ContentFiltered : LLMEvent
    ctor(ClassificationResult Classification)
    ClassificationResult Classification { get; init; }
  // The complete model message of a shader run (may differ from the text response), emitted once at the end.
  sealed record LLMEvent.FinalModelMessage : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // The complete text response of a shader run, emitted once at the end.
  sealed record LLMEvent.FinalText : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // The provider's finish reason for the generation (e.g. "stop", "max_tokens").
  sealed record LLMEvent.Finished : LLMEvent
    ctor(string Reason)
    string Reason { get; init; }
  // How much content of a given kind arrived in one chunk, emitted as the chunk is decoded. Opt-in via KernelContext.StreamProgress — off by default, because it changes the event stream every consumer sees. It exists because nothing else can answer "is the model working right now" over RPC: usage is reported once a turn has ended, Reasoning and tool arguments are only emitted after the stream drains, and text may be suppressed entirely on a tool-calling turn. Carries the SIZE and not the content, so it costs a few bytes and never puts the same text on the wire twice — the content still arrives in its own event.
  sealed record LLMEvent.GenerationProgress : LLMEvent
    ctor(LlmStreamKind Kind, int Characters)
    int Characters { get; init; }
    LlmStreamKind Kind { get; init; }
  // The model's reasoning trace for this generation.
  sealed record LLMEvent.Reasoning : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // A parsed XML-style tag extracted from the text stream by AsyncEnumerableExtensions.WithParsedTagsAsync.
  sealed record LLMEvent.Tag : LLMEvent
    ctor(string Name, string Content, IReadOnlyDictionary<string, string>? Attributes)
    IReadOnlyDictionary<string, string>? Attributes { get; init; }
    string Content { get; init; }
    string Name { get; init; }
  // An incremental chunk of generated text.
  sealed record LLMEvent.TextDelta : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // The model requested a tool invocation.
  sealed record LLMEvent.ToolCallRequested : LLMEvent
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  // The model's plan for upcoming tool calls (Cohere).
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
    // Builds a block from a heterogeneous input list. Each input must be a string or a BinaryDataContainer whose MIME type is an image, audio, video, or PDF; any other input type or MIME type is rejected rather than silently dropped. Returns null only when inputs is empty.
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
  // Selects which JSON-schema dialect the generator emits. All Ikon-side schema shapes (primitives, arrays, dictionaries, polymorphism) are expressible in both dialects; the two differ in how they encode nullability and how strictly they police unknown keywords.
  enum SchemaDialect
    // JSON Schema 2020-12 / OpenAPI 3.1. Nullable primitives expand their "type" into a ["X", "null"] union. Accepted by OpenAI strict structured outputs and Anthropic tool-use schemas.
    JsonSchema202012
    // OpenAPI 3.0 Schema Object. "type" is always a single string and nullability is carried on a separate "nullable": true flag. Accepted by Google's Gemini response_schema validator, which rejects the 2020-12 union-type form outright.
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
  // Request/response format a custom LLM endpoint speaks.
  enum CustomLLMApi
    // OpenAI chat completions format (/v1/chat/completions) — spoken by most self-hosted stacks: vLLM, Ollama, llama.cpp, LM Studio, TGI, SGLang.
    OpenAICompletions
    // OpenAI responses format (/v1/responses).
    OpenAIResponses
    // Anthropic messages format.
    Anthropic
    // Google Gemini format (Vertex-style streamGenerateContent).
    Google
    // Cohere chat format.
    Cohere
  // Capability flags default to what a typical self-hosted OpenAI-compatible model supports; enable more (e.g. SupportsJsonSchema) when the endpoint provides them.
  sealed class CustomLLMModel : CustomModel
    ctor()
    // Request/response format the endpoint speaks.
    required CustomLLMApi Api { get; init; }
    // Maximum input-context window of the model, in tokens.
    required int ContextWindowSize { get; init; }
    // Largest number of tokens the endpoint will generate in one response. Leave at 0 when the endpoint has no such cap: a request asking for more than the model can produce is capped at this value instead of being sent as-is, and 0 means "send the caller's value".
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
    // Projects the function's parameter list into its provider JSON schema: an object schema with type/properties/required, including parameter descriptions and allowed-value enums.
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
    // Generate a mesh from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateMeshAsync when the request needs any other MeshGeneratorConfig field.
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
    // Channel count of the PCM samples produced by GenerateMusicAsync.
    int ChannelCount { get; }
    // Sample rate of the PCM samples produced by GenerateMusicAsync.
    int SampleRate { get; }
    // Requires IMusicGeneratorInfo.SupportsStreaming; otherwise throws NonRetryableMusicGeneratorException. Use GenerateMusicFileAsync for a buffered encoded file.
    IAsyncEnumerable<AudioChunk> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    // Generates the music and returns it as a single buffered, encoded audio file. Supported by all models, including those that cannot stream.
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
    // Generate a music file from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateMusicFileAsync when the request needs any other MusicGeneratorConfig field (duration, input audio, seed).
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
    int MaxPagesSupported { get; }
  class NonRetryableOCRException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class OCR : IOCR
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxPagesSupported { get; }
    // Read one document's bytes — the instance form of the AnalyzeAsync one-shot, for when you already hold an OCR instance. Reach for AnalyzeDocumentAsync when the request needs any other OCRConfig field (asset uri, url, document type).
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
    int MaxPagesSupported { get; init; }
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
  // Ikon's uniform provenance layer for AI-generated images — the platform's EU AI Act Article 50 marking, applied identically for every provider (per-provider watermarks are deliberately opted out of upstream). Three layers behind one call: an XMP metadata mark (always; the machine-readable baseline — IPTC DigitalSourceType=trainedAlgorithmicMedia), an imperceptible tiled pixel watermark (default on; detectable via MeasureInvisibleMark), and an optional visible corner badge (the self-service trial-tier lever). PNG and JPEG take all three; WebP takes the metadata mark alone, because the bundled codec neither reads nor writes it and reaching the pixels would mean transcoding the caller's image to another format. Ask GetMarkingSupport rather than assuming. Any other encoding passes through untouched. Streamed media (WebRTC, TTS) is out of scope by design — disclosure there is interaction-level.
  static class ImageProvenance
    // Mark a generated image. Returns new bytes for PNG/JPEG input; any other format is returned unchanged (recorded roadmap gap, not an error — the caller cannot do better).
    static byte[] Apply(byte[] data, string model, bool invisibleWatermark = true, string visibleWatermark = "")
    // What Apply would achieve on these bytes, so a caller can tell a fully marked result from a metadata-only one instead of assuming.
    static ProvenanceMarking GetMarkingSupport(byte[] data)
    // Correlation score of the invisible mark. At or above DetectionThreshold the image carries Ikon's mark; unmarked images score near zero. Public so tooling (and the trial-tier pipeline) can prove our own marks.
    static double MeasureInvisibleMark(byte[] data)
    // The embedded XMP packet, or null when none is present. Format-agnostic scan (the packet carries a globally unique id), usable on PNG and JPEG alike.
    static string? ReadMetadataMark(byte[] data)
    // Detection score at or above which an image is considered marked. Scores are normal-deviates: an unmarked image scores |z| ≲ 3, a marked one scores in the tens to hundreds depending on size and recompression.
    const double DetectionThreshold = 12.0
  // How completely ImageProvenance.Apply can mark a given encoding.
  enum ProvenanceMarking
    // No mark at all — an encoding this layer does not know.
    None
    // The XMP metadata mark only. Machine-readable and standards-compliant, but strippable by anything that rewrites the file's metadata.
    MetadataOnly
    // Metadata plus the imperceptible pixel watermark, which survives a re-encode.
    Full

namespace Ikon.AI.Reranking
  // Request/response format a custom rerank endpoint speaks.
  enum CustomRerankApi
    // Cohere rerank format.
    Cohere
    // Jina rerank format.
    Jina
    // Voyage rerank format.
    Voyage
    // Together rerank format.
    Together
  // Configuration for a user-provided custom rerank endpoint, registered via CustomModels.Register and selected by name with new Reranker(name).
  sealed class CustomRerankModel : CustomModel
    ctor()
    // Request/response format the endpoint speaks.
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
    // Sentinel for user-registered custom models (see CustomModels); not directly usable — select custom models by their registered name string.
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
    // Channel count of the PCM samples produced by GenerateSoundEffectAsync.
    int ChannelCount { get; }
    // Sample rate of the PCM samples produced by GenerateSoundEffectAsync.
    int SampleRate { get; }
    // Streams raw PCM chunks; use GenerateSoundEffectFileAsync for a buffered, encoded audio file instead.
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    // Generates the sound effect and returns it as a single buffered, encoded audio file (WAV).
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
    // Generate a sound-effect file from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateSoundEffectFileAsync when the request needs any other SoundEffectGeneratorConfig field.
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
    // Speak a line of text and collect it into one audio chunk — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateSpeechAsync when you want the chunks as they stream, or any other SpeechGeneratorConfig field.
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
    // Transcribe a buffer of samples — the instance form of the RecognizeAsync one-shot, for when you already hold a recognizer. Reach for RecognizeBatchSpeechAsync when the request needs any other RecognizeSpeechConfig field (language, prompt, timestamps).
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
    // The timeout for individual speech recognition API requests.
    TimeSpan RequestTimeout { get; set; }
    // SilenceTriggered mode only: a pause of this length flushes accumulated speech for recognition. Defaults to 750ms.
    TimeSpan SilenceDuration { get; set; }
    // The amplitude threshold below which audio is considered silence. Sample values with absolute amplitude below this threshold are treated as silent.
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
    // Brute-force in-process store. The default — no external dependency.
    InMemory
    // Postgres + pgvector, with an HNSW index. Scales past what an in-RAM linear scan can.
    PgVector
  // Chooses the backing store for a VectorDatabase. The default (or a null config) keeps the in-memory store, so existing callers are unaffected; pass one with VectorStoreBackend.PgVector to persist and scale.
  sealed class VectorStoreConfig
    ctor()
    VectorStoreBackend Backend { get; init; }
    // Opens a fresh connection for a pgvector operation (each op opens and disposes its own, as PgVectorCorpus does), so the call belongs inside the factory: () => DatabaseConnection.Postgres(...).DbConnection. Required when Backend is VectorStoreBackend.PgVector.
    Func<DbConnection>? ConnectionFactory { get; init; }
    // Table-name prefix, so several vector databases can share one Postgres database.
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
    // Enhance a video by URL — the instance form of the EnhanceAsync one-shot, for when you already hold an enhancer. Reach for EnhanceVideoAsync when the request needs any other VideoEnhancerConfig field.
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
    // Generate a video from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateVideoAsync when the request needs any other VideoGeneratorConfig field.
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
    // Scrape one page by URL — the instance form of the ScrapeAsync one-shot, for when you already hold a scraper. Reach for ScrapeSinglePageAsync when the request needs any other SinglePageScrapeConfig field.
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
    // Web page search for a plain query — the instance form of the SearchAsync one-shot, for when you already hold a searcher. Reach for SearchPagesAsync when the search needs any other SearchConfig field (site restriction, country, language).
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
  // Arguments passed to a UI action callback, containing the client context and the deserialized payload.
  sealed class ActionArgs<T>
    ctor()
    // The client context of the user who triggered the action.
    Context ClientContext { get; init; }
    // The deserialized action payload.
    T Value { get; init; }
  // The busy/status pattern every async UI handler repeats, as one call. Without this, the standard shape is five lines of ceremony around one line of work:
  // _busy.Value = true;
  // _status.Value = null;
  //
  // try { await LoadAsync(); }
  // catch (Exception ex) { _status.Value = ex.Message; }
  // finally { _busy.Value = false; }
  // RunAsync collapses it to:
  // await _busy.RunAsync(_status, LoadAsync);
  // For the busy flag alone (no status reactive), use _busy.AsToken() from Ikon.Common.Core.Reactive instead.
  static class ReactiveBusyExtensions
    // Runs work with busy raised: clears status, sets the flag for the duration of the work (via ReactiveBoolExtensions.AsToken, so it always returns to false), and routes a failure's message into status instead of throwing. Cancellation (OperationCanceledException) is not treated as a failure and propagates to the caller. Returns whether the work completed, so callers can add their own failure handling on top:
    // if (!await _busy.RunAsync(_status, RefreshAsync))
    // {
    //     _entries.Value = [];
    // }
    static Task<bool> RunAsync(this Reactive<bool> busy, Reactive<string?> status, Func<Task> work)
  // Per-client theme state created by UI.UseTheme. Holds each client's active theme and switches it: Current is bindable in views, and ToggleAsync can be bound directly to a button's onClick.
  sealed class ThemeControl
    // The calling client's active theme. Bindable in views, e.g. name: theme.Current.Value == Theme.Dark ? "sun" : "moon".
    ClientReactive<Theme> Current { get; }
    // Sets the calling client's theme and pushes it to that client.
    Task SetAsync(Theme theme)
    // Flips the calling client between dark and light.
    Task ToggleAsync()
  // Main entry point for the Ikon Parallax reactive UI system. Manages client connections, render cycles, style distribution, and action handling for server-driven UI.
  class UI
    // Creates a new UI instance bound to the given app and theme.
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
  // Represents a UI view scope for building the component tree. Extension methods on this type provide the component API (e.g. Text, Button, Input).
  class UIView
    // The default icon library name used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }
    // True only while capturing the build-time boot snapshot — a public asset shown to everyone before the live UI connects (always false on the live render). Gate per-user or sensitive content on this, preferably via the SnapshotReveal/SnapshotHide/SnapshotOnly wrappers.
    bool IsSnapshot { get; }
    // The boot-snapshot variant id this capture render was asked for (the client's Context.SnapshotVariant): the app's [BootSnapshot] seed rules name variant skeletons, and the capture client passes each id here so the app can branch to the matching skeleton. Empty on route captures (render the real page) and on every live render.
    string SnapshotVariant { get; }
    // Adds a child node with the given type and props. The props parameter is the non-generic IDictionary on purpose: it's the ONLY type that cleanly accepts BOTH a `Dictionary<string, object>` (the natural non-null shape a model builds) AND a `Dictionary<string, object?>` (props that carry null values) with no nullability warning and no suppression. A generic `Dictionary<string, object?>` param warns CS8620 on the non-null form (identity-modulo-nullability), and no PAIR of generic overloads works either — nullability annotations are erased for overload resolution, so two such overloads are CS0111 (same signature) or CS0121 (ambiguous).
    void AddNode(string type, IDictionary? props = null, List<UIViewNode>? children = null, string? key = null, string[]? style = null, string? styleId = null)
    // Registers a callback as a UI action and returns its ID for use in component props.
    string? CreateAction<T>(Func<ActionArgs<T>, Task>? callback)
    // The returned string is an opaque reference to use as an image src (e.g. on an Image component), not a data URL. The data buffer is copied on registration, so the caller may reuse or mutate it immediately after the call. data must be non-empty — an empty buffer has no valid reference and throws ArgumentException.
    string RegisterPayload(byte[] data, string mimeType)
  // Represents a single node in the UI view tree, with identity, props, children, and style information.
  sealed class UIViewNode
    // Ordered child nodes. Treat as immutable: the node is shared by reference into the subtree cache, and the differ relies on the child list being the pristine as-built content, so mutating it corrupts diffing and the cache. The mutable backing list is builder-internal.
    IReadOnlyList<UIViewNode> Children { get; }
    // Lazily computed content signature used for subtree caching and diffing.
    string? ContentFingerprint { get; }
    // True when StableHint came from an explicit key argument, not from a prop such as value or text.
    bool HasExplicitKey { get; }
    // Stable unique identifier for this node.
    string Id { get; }
    // Precomputed hash of Id for fast lookups.
    int IdHash { get; }
    // Debug-only, process-global switch: when true, EVERY node built by ANY view on ANY thread and for ANY client emits a source file/line marker that is serialized into the wire payload, inflating all UI updates. Despite reading like a per-instance toggle it is static mutable state with no thread-safety, so flip it only for local debugging (the runtime sets it from the app's DebugMode) and never leave it on in production.
    static bool IncludeSourceMarkers { get; set; }
    // Component properties passed to the frontend renderer. Backed by the compact shape-interned PropsMap on server-built trees; treat as immutable.
    IReadOnlyDictionary<string, object?> Props { get; }
    // Source file and line marker for debugging, included only when IncludeSourceMarkers is true.
    string? SourceMarker { get; }
    // Hint string used by the stable ID generator to produce deterministic IDs.
    string? StableHint { get; }
    // Resolved Crosswind style class identifiers.
    IReadOnlyList<string> StyleIds { get; }
    // The component type name (e.g. "div", "button").
    string Type { get; }

namespace Ikon.Parallax.Components.Charts
  // Configuration for a chart axis including legend text, tick appearance, and label truncation.
  sealed record AxisConfig
    ctor()
    // For time scales this is a d3-time-format token string (e.g. "%H:%M", "%m/%d %H:%M"), not a .NET format.
    string? Format { get; init; }
    string? Legend { get; init; }
    int? LegendOffset { get; init; }
    // Number of ticks to display. When set, the axis will show approximately this many evenly-spaced ticks instead of one per data point.
    int? TickCount { get; init; }
    int? TickPadding { get; init; }
    int? TickRotation { get; init; }
    int? TickSize { get; init; }
    // Truncate tick label text at this character length.
    int? TruncateTickAt { get; init; }
  // Controls how multiple bar series are displayed.
  enum BarGroupMode
    Stacked
    Grouped
  // Controls the orientation of a bar chart.
  enum BarLayout
    Vertical
    Horizontal
  // Styling for chart axis elements including ticks, legends, and domain lines.
  record ChartAxisStyle
    ctor()
    string? DomainColor { get; init; }
    ChartTextStyle? Legend { get; init; }
    string? TickColor { get; init; }
    ChartTextStyle? TickLabel { get; init; }
  // Event arguments for chart click interactions.
  sealed record ChartClickArgs
    ctor()
    string? Id { get; init; }
    string? IndexValue { get; init; }
    string? SerieId { get; init; }
    object? Value { get; init; }
  // Predefined color schemes for chart series, based on D3 color scales.
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
  // Styling for chart crosshair lines.
  record ChartCrosshairStyle
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Extension methods for rendering interactive chart components (bar, line, pie).
  static class ChartExtensions
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip values and value-axis ticks human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void BarChart(this UIView view, string[]? style = null, IEnumerable<Dictionary<string, object>>? data = null, IEnumerable<string>? keys = null, string? indexBy = null, BarGroupMode? groupMode = null, BarLayout? layout = null, ScaleType? valueScale = null, ScaleType? indexScale = null, bool? reverse = null, double? minValue = null, double? maxValue = null, double? padding = null, double? innerPadding = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enableLabel = null, int? labelSkipWidth = null, int? labelSkipHeight = null, string? labelTextColor = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderRadius = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip Y values and left-axis ticks human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void LineChart(this UIView view, string[]? style = null, IEnumerable<LineChartSeries>? data = null, ScaleType? xScaleType = null, ScaleType? yScaleType = null, double? xScaleMin = null, double? xScaleMax = null, double? yScaleMin = null, double? yScaleMax = null, bool? yScaleStacked = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enablePoints = null, int? pointSize = null, string? pointColor = null, string? pointBorderColor = null, int? pointBorderWidth = null, bool? enableArea = null, double? areaOpacity = null, double? areaBaselineValue = null, bool? enableCrosshair = null, CrosshairType? crosshairType = null, LineCurve? curve = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, double? lineWidth = null, bool? isInteractive = null, bool? useMesh = null, bool? enableSlices = null, string? xFormat = null, string? yFormat = null, string? valueUnit = null, string? gradientFromColor = null, string? gradientToColor = null, IEnumerable<double>? gridXValues = null, IEnumerable<double>? gridYValues = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip values human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void PieChart(this UIView view, string[]? style = null, IEnumerable<PieChartDatum>? data = null, double? innerRadius = null, double? padAngle = null, double? cornerRadius = null, double? startAngle = null, double? endAngle = null, bool? sortByValue = null, ChartMargin? margin = null, bool? enableArcLabels = null, string? arcLabelsTextColor = null, double? arcLabelsSkipAngle = null, bool? enableArcLinkLabels = null, string? arcLinkLabelsTextColor = null, double? arcLinkLabelsSkipAngle = null, double? arcLinkLabelsThickness = null, string? arcLinkLabelsColor = null, double? activeOuterRadiusOffset = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? arcLabelAsPercentage = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
  // Styling for chart grid lines.
  record ChartGridStyle
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Styling for chart data labels.
  record ChartLabelsStyle
    ctor()
    ChartTextStyle? Text { get; init; }
  // Styling for chart legend text and title.
  record ChartLegendStyle
    ctor()
    ChartTextStyle? Text { get; init; }
    ChartTextStyle? Title { get; init; }
  // Margin configuration for chart containers.
  sealed record ChartMargin
    ctor()
    int? Bottom { get; init; }
    int? Left { get; init; }
    int? Right { get; init; }
    int? Top { get; init; }
  // Text styling for chart elements.
  record ChartTextStyle
    ctor()
    string? Color { get; init; }
    string? FontFamily { get; init; }
    int? FontSize { get; init; }
  // Complete theme configuration for chart components, combining all styling aspects.
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
  // Built-in chart theme presets for light and dark backgrounds.
  static class ChartThemes
    // Chart theme optimized for dark backgrounds with muted but saturated series colors.
    static ChartTheme DefaultDark { get; }
    // Chart theme optimized for light backgrounds with soft, pastel-like series colors.
    static ChartTheme DefaultLight { get; }
  // Styling for chart tooltips.
  record ChartTooltipStyle
    ctor()
    string? BackgroundColor { get; init; }
    string? BorderColor { get; init; }
    int? BorderRadius { get; init; }
    ChartTextStyle? Text { get; init; }
  // Crosshair display type for interactive charts.
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
  // Where a chart legend is anchored within the chart area.
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
  // Configuration for a chart legend including positioning, layout direction, and item sizing.
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
  // Layout direction for chart legend items.
  enum LegendDirection
    Row
    Column
  // A single data point in a line chart series.
  sealed record LineChartPoint
    ctor()
    // Pass a string label for point scales, or a number for linear/time scales — the object type is genuinely mixed.
    required object X { get; init; }
    required double Y { get; init; }
  // A named data series for a line chart, containing an ordered collection of points.
  sealed record LineChartSeries
    ctor()
    string? Color { get; init; }
    IEnumerable<LineChartPoint>? Data { get; init; }
    required string Id { get; init; }
  // Interpolation curve type for line charts.
  enum LineCurve
    Linear
    MonotoneX
    Step
    StepBefore
    StepAfter
    Cardinal
    Basis
  // A single slice in a pie chart.
  sealed record PieChartDatum
    ctor()
    string? Color { get; init; }
    required string Id { get; init; }
    string? Label { get; init; }
    required double Value { get; init; }
  // Scale type for chart axes.
  enum ScaleType
    Point
    Linear
    Time
    Log

namespace Ikon.Parallax.Components.DataTable
  // A single cell in a data table row. Use the static factory methods to create typed cells.
  record Cell
    ctor()
    // Action identifier passed to the onActionClick callback.
    string? ActionId { get; init; }
    // Action buttons for "actions" type cells.
    CellAction[]? Actions { get; init; }
    // When true, the cell's interactive element is disabled.
    bool? Disabled { get; init; }
    // Button label for action cells.
    string? Label { get; init; }
    // Crosswind style classes for the cell.
    string[]? Style { get; init; }
    // Semantic tone for badge cells.
    SemanticTone? Tone { get; init; }
    // The kind of content this cell renders.
    CellType Type { get; init; }
    // Display value or checkbox state ("true"/"false").
    string? Value { get; init; }
    // Creates an action button cell.
    static Cell Action(string label, string actionId, string[]? style = null)
    // Creates a cell containing multiple action buttons.
    static Cell ActionGroup(CellAction[] actions)
    // style classes replace the themed tone token; lead the array with the "default" marker to merge the tone token underneath them instead.
    static Cell Badge(string value, SemanticTone? tone = null, string[]? style = null)
    // Creates a checkbox cell.
    static Cell Checkbox(bool value, string actionId, string[]? style = null, bool disabled = false)
    // Creates a text cell.
    static Cell Text(string? value, string[]? style = null)
  // An action button that can be displayed within a data table cell.
  record CellAction
    ctor(string Label, string ActionId, string[]? Style = null, string? Icon = null)
    string ActionId { get; init; }
    string? Icon { get; init; }
    string Label { get; init; }
    string[]? Style { get; init; }
  // The kind of content a data table cell renders.
  enum CellType
    // Plain display text.
    Text
    // Status badge with an optional semantic tone.
    Badge
    // Single action button.
    Action
    // Group of action buttons.
    Actions
    // Checkbox bound to an action id.
    Checkbox
  // Defines a column in a data table including header text, width, and alignment.
  record DataTableColumn
    ctor(string Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left, string? MinWidth = null, bool Wrap = false)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string Header { get; init; }
    string? MinWidth { get; init; }
    string? Width { get; init; }
    bool Wrap { get; init; }
  // Extension methods for rendering paginated data tables.
  static class DataTableExtensions
    // Renders a paginated data table with configurable columns, rows, actions, and styling. Per-slot styling (header, rows, cells, pagination, …) goes through styles; see DataTableStyles for the slots.
    static void DataTable(this UIView view, DataTableColumn[] columns, DataTableRow[] rows, int totalCount, int pageIndex, int pageSize, Func<int, Task>? onPageChange = null, Func<string, Task>? onRowClick = null, Func<string, Task>? onActionClick = null, Action<UIView>? emptyContent = null, int[]? columnWidths = null, Func<string, Task>? onColumnResize = null, string[]? style = null, DataTableStyles? styles = null, string? prevLabel = null, string? nextLabel = null, string? pageLabel = null, string? key = null)
  // A single row in a data table, identified by a unique ID and containing an array of cells.
  record DataTableRow
    ctor(string Id, Cell[] Cells)
    Cell[] Cells { get; init; }
    string Id { get; init; }
  // Each slot is a Crosswind class array that merges on top of the slot's themed default, exactly like a component's style: parameter; set only the slots you are changing.
  sealed record DataTableStyles
    ctor()
    // Action buttons rendered from action cells.
    string[]? ActionButton { get; init; }
    // Every cell (header and data).
    string[]? Cell { get; init; }
    // Data cells only.
    string[]? DataCell { get; init; }
    // The empty-state container shown when there are no rows.
    string[]? Empty { get; init; }
    // The header row.
    string[]? Header { get; init; }
    // Header cells only.
    string[]? HeaderCell { get; init; }
    // Page number buttons.
    string[]? PageNumber { get; init; }
    // The active page number button.
    string[]? PageNumberActive { get; init; }
    // The pagination bar.
    string[]? Pagination { get; init; }
    // The previous/next pagination buttons.
    string[]? PaginationButton { get; init; }
    // Column resize handles.
    string[]? ResizeHandle { get; init; }
    // Every data row.
    string[]? Row { get; init; }
    // Truncated-cell hover tooltips.
    string[]? Tooltip { get; init; }

namespace Ikon.Parallax.Components.ImageEditor
  // Extension methods for the image editor canvas component.
  static class ImageEditorExtensions
    // triggerSave/triggerUndo/triggerRedo are edge-triggered — increment the value to fire that action. highResolution keeps the canvas at native resolution (sharp zoom, full-quality export, but capped undo history); when false the canvas is downscaled to fit its container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // src: URL or data URL of the image to edit.
    // brushWidth: Brush size in pixels.
    // brushColor: Brush color as hex code (e.g. "#ff0000").
    // tool: Currently active drawing tool. Defaults to ImageEditorTool.Brush on the frontend.
    // zoom: Zoom level (1.0 = 100%, 2.0 = 200%, etc.).
    // highResolution: Keep the canvas at the image's native resolution (capped) so zooming stays sharp and saves export at full quality; also caps undo history. When false the canvas is downscaled to fit its container.
    // textMaxLength: Max character length for the floating text input shown when the text tool is active. Null means no limit.
    // textFontSize: Font size in pixels for the rendered text. Null = derived from brush width.
    // textPadding: Padding in pixels around the text (applied to both input overlay and rendered background). Null = default 4.
    // onSave: Callback when user saves, receives base64 image data.
    // onHistoryChange: Callback when undo/redo history state changes.
    // triggerSave: Increment to trigger a save action.
    // triggerUndo: Increment to trigger an undo action.
    // triggerRedo: Increment to trigger a redo action.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    static void ImageEditorCanvas(this UIView view, string[]? style = null, string? src = null, int? brushWidth = null, string? brushColor = null, ImageEditorTool? tool = null, double? zoom = null, bool? highResolution = null, bool? fitContainer = null, int? textMaxLength = null, int? textFontSize = null, int? textPadding = null, Func<ImageEditorSaveArgs, Task>? onSave = null, Func<ImageEditorHistoryArgs, Task>? onHistoryChange = null, int? triggerSave = null, int? triggerUndo = null, int? triggerRedo = null, string? styleId = null, string? key = null)
  // Event args for when the undo/redo history state changes.
  sealed record ImageEditorHistoryArgs
    ctor(bool CanUndo, bool CanRedo)
    bool CanRedo { get; init; }
    bool CanUndo { get; init; }
  // Event args for when the image editor saves the edited image.
  sealed record ImageEditorSaveArgs
    ctor(string ImageData)
    string ImageData { get; init; }
  // Drawing tool active on an ImageEditorCanvas.
  enum ImageEditorTool
    // Freehand brush strokes.
    Brush
    // Erases previously drawn content.
    Eraser
    // Places text via a floating input.
    Text
    // Draws arrow annotations.
    Arrow
    // Marks a rectangular region.
    Region
    // Draws a freehand outline that closes into a region on release.
    Lasso
    // Draws a straight line segment from press to release.
    Line

namespace Ikon.Parallax.Components.Standard
  // Extension methods for accessibility components.
  static class AccessibilityExtensions
    // Wraps an icon with accessible label for screen readers.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // label: Accessible label announced by screen readers.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering the icon within this component.
    static void AccessibleIcon(this UIView view, string[]? style = null, string? label = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Hides content visually while keeping it accessible to screen readers.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void VisuallyHidden(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Base event returned from a client-side action, indicating the action type and whether it succeeded.
  record ActionEvent
    ctor(string ActionType, bool Success)
    string ActionType { get; init; }
    bool Success { get; init; }
  // Types of client-side actions that can be triggered from the server.
  enum ActionKind
    // Unknown or unrecognized action.
    Unknown
    // Capture an image from the client's camera.
    CaptureImage
    // Copy text to the system clipboard.
    CopyToClipboard
    // Download a file to the client.
    DownloadFile
    // Exit fullscreen mode.
    ExitFullscreen
    // Request the client's geographic location.
    GetLocation
    // Open the contact picker.
    PickContacts
    // Enter fullscreen mode.
    RequestFullscreen
    // Open the native share dialog.
    Share
  // Base class for client-side action configuration.
  abstract record ActionOptions
  // Represents activation mode for Tabs.
  enum ActivationMode
    Automatic
    Manual
  // Inline alert banner composite over the theme's Alert token recipe.
  static class AlertExtensions
    // Caller style replaces the tone's Theming.Alert token; lead the array with "default" to merge that token underneath it. The icon defaults per tone (success/warning/error/info).
    // view: The UIView to render into.
    // title: Alert headline.
    // tone: Semantic tone selecting the Alert color variant (Neutral and Brand use the default surface).
    // style: Crosswind/Tailwind utility classes merged on top of the themed alert token.
    // description: Muted body text under the title.
    // icon: Lucide icon name overriding the tone's default icon.
    // showIcon: When false, no icon is rendered.
    // onDismiss: When set, renders a dismiss (×) button in the top-right corner that invokes this callback.
    // titleStyle: Style for the title text. Defaults to Theming.Alert.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.Alert.Description.
    // iconStyle: Style for the icon.
    // dismissStyle: Style for the dismiss button.
    // content: Builder for extra elements rendered under the description (e.g. action links).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Alert(this UIView view, string title, SemanticTone tone = Neutral, string[]? style = null, string? description = null, string? icon = null, bool showIcon = true, Func<Task>? onDismiss = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? iconStyle = null, string[]? dismissStyle = null, Action<UIView>? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Represents alignment for overlay positioning.
  enum Align
    Start
    Center
    End
  // Status pill composite over the theme's Badge token recipe. Replaces the hand-rolled inline-flex/rounded-full/px-2 pill pattern with a one-liner: view.Badge("Live", SemanticTone.Success).
  static class BadgeExtensions
    // With no style args it renders the themed Theming.Badge.* pill for the tone; caller styles replace the base token, or merge on top of it when the array leads with "default".
    // view: The UIView to render into.
    // text: Badge label.
    // tone: Semantic tone selecting the Badge color variant.
    // style: Crosswind/Tailwind utility classes merged on top of the themed badge token.
    // size: Pill size (Sm/Md/Lg).
    // outline: When true, uses the outlined variant: the tone's border becomes visible instead of transparent. The fill is unchanged.
    // dot: When true, renders a small status dot before the label in the badge's current color.
    // dotStyle: Style for the dot. Defaults to a 6px circle filled with the badge foreground color.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Badge(this UIView view, string text, SemanticTone tone = Neutral, string[]? style = null, BadgeSize size = Md, bool outline = false, bool dot = false, string[]? dotStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Size of a BadgeExtensions.Badge.
  enum BadgeSize
    // Compact — 22px pill with extra-small text.
    Sm
    // Default — 24px pill with small text.
    Md
    // Roomy — 28px pill with small text.
    Lg
  // Breadcrumb trail composite over the theme's Breadcrumb token recipe.
  static class BreadcrumbExtensions
    // Items with an OnClick render as clickable links; the last item always renders as the non-clickable current page (aria-current="page") regardless of its OnClick.
    // view: The UIView to render into.
    // items: Trail entries in root-to-current order.
    // style: Crosswind/Tailwind utility classes merged on top of Theming.Breadcrumb.Root.
    // separatorIcon: Lucide icon name for the separator. Defaults to "chevron-right".
    // linkStyle: Style for clickable items. Defaults to Theming.Breadcrumb.Link.
    // itemStyle: Style for non-clickable, non-current items. Defaults to Theming.Breadcrumb.Item.
    // pageStyle: Style for the current page (last item). Defaults to Theming.Breadcrumb.Page.
    // separatorStyle: Style for the separator icon. Defaults to Theming.Breadcrumb.Separator with a 14px size.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Breadcrumb(this UIView view, IReadOnlyList<BreadcrumbItem> items, string[]? style = null, string? separatorIcon = null, string[]? linkStyle = null, string[]? itemStyle = null, string[]? pageStyle = null, string[]? separatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // One entry in a BreadcrumbExtensions.Breadcrumb trail.
  sealed record BreadcrumbItem
    // Label: Visible text of the crumb.
    // OnClick: Navigation callback. Ignored for the last item, which always renders as the non-clickable current page.
    ctor(string Label, Func<Task>? OnClick = null)
    // Visible text of the crumb.
    string Label { get; init; }
    // Navigation callback. Ignored for the last item, which always renders as the non-clickable current page.
    Func<Task>? OnClick { get; init; }
  // Extension methods for Calendar and DatePicker components.
  static class CalendarExtensions
    // All date values (value, defaultValue, minDate, maxDate, callbacks) are ISO yyyy-MM-dd strings; month is yyyy-MM. Controlled via value+onValueChange; omit both and pass defaultValue for uncontrolled.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for the root grid container. Use Calendar.Root.
    // value: Controlled selected date (ISO yyyy-MM-dd).
    // defaultValue: Initial selected date for uncontrolled mode.
    // month: Controlled display month (ISO yyyy-MM or yyyy-MM-dd).
    // defaultMonth: Initial display month for uncontrolled mode.
    // minDate: Earliest selectable date (inclusive).
    // maxDate: Latest selectable date (inclusive).
    // disabledDates: Explicit dates that cannot be selected.
    // weekStart: Day of the week the grid starts on. Defaults to Monday.
    // locale: BCP-47 locale used for weekday and month labels (e.g. en-US).
    // disabled: When true, prevents user interaction with this component.
    // headerStyle: Style for the month header row. Use Calendar.Header.
    // weekdayStyle: Style for the weekday-label row. Use Calendar.Weekday.
    // dayStyle: Style for day cells. Use Calendar.Day.
    // daySelectedStyle: Style for the selected day cell. Use Calendar.DaySelected.
    // dayTodayStyle: Style for today's cell. Use Calendar.DayToday.
    // dayOutsideStyle: Style for cells outside the current month. Use Calendar.DayOutside.
    // dayDisabledStyle: Style for disabled day cells. Use Calendar.DayDisabled.
    // navButtonStyle: Style for the previous/next month nav buttons. Use Calendar.NavButton.
    // titleStyle: Style for the month/year title. Use Calendar.HeaderTitle.
    // gridStyle: Style for the body container that stacks weekday + week rows. Use Calendar.Grid.
    // rowStyle: Style for each 7-cell week row (also the weekday-label row). Use Calendar.Row.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the selected date changes (ISO yyyy-MM-dd).
    // onMonthChange: Invoked when the display month changes (ISO yyyy-MM).
    static void Calendar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? month = null, string? defaultMonth = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, string? locale = null, bool? disabled = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onMonthChange = null)
    // Button that opens a popover containing a Calendar.
    // view: The UIView to render into.
    // value: Controlled selected date (ISO yyyy-MM-dd).
    // defaultValue: Initial selected date for uncontrolled mode.
    // placeholder: Text shown in the trigger when no date is selected.
    // format: BCP-47 locale format hint for the trigger label (e.g. en-US).
    // minDate: Earliest selectable date (inclusive).
    // maxDate: Latest selectable date (inclusive).
    // disabledDates: Explicit dates that cannot be selected.
    // weekStart: Day of the week the grid starts on. Defaults to Monday.
    // disabled: When true, prevents user interaction.
    // open: Controlled open state of the popover.
    // defaultOpen: Initial open state for uncontrolled mode.
    // side: Side the popover content renders against.
    // align: Alignment of the content along the chosen side.
    // triggerStyle: Style for the trigger button. Use DatePicker.Trigger.
    // contentStyle: Style for the popover content container. Use DatePicker.Content.
    // calendarStyle: Style applied to the embedded Calendar grid root.
    // headerStyle: Style for the embedded Calendar's month header row.
    // weekdayStyle: Style for the embedded Calendar's weekday labels.
    // dayStyle: Style for the embedded Calendar's day cells.
    // daySelectedStyle: Style for the embedded Calendar's selected day cell.
    // dayTodayStyle: Style for the embedded Calendar's today cell.
    // dayOutsideStyle: Style for the embedded Calendar's cells outside the current month.
    // dayDisabledStyle: Style for the embedded Calendar's disabled day cells.
    // navButtonStyle: Style for the embedded Calendar's previous/next month nav buttons.
    // titleStyle: Style for the embedded Calendar's month/year title.
    // gridStyle: Style for the embedded Calendar's body container.
    // rowStyle: Style for each 7-cell row in the embedded Calendar.
    // rootStyle: Style for the outermost container. Rarely needed.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the selected date changes (ISO yyyy-MM-dd).
    // onOpenChange: Invoked when the popover open state changes.
    // label: Optional field label rendered above the date picker (same field ergonomics as TextField).
    static void DatePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, string? format = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? calendarStyle = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Which physical camera to prefer when starting the capture. Maps to the W3C MediaStream facingMode constraint and is treated as an "ideal" hint — the browser falls back to whatever camera is available if the requested side does not exist (e.g. desktops without a rear camera).
  enum CameraFacing
    // Front-facing camera (user-facing). Maps to facingMode: "user".
    User
    // Rear-facing camera (away from the user). Maps to facingMode: "environment".
    Environment
  // Options for capturing an image from the client's camera.
  sealed record CaptureImageActionOptions : ActionOptions
    ctor()
    // Hardware constraints for camera selection.
    CaptureImageConstraints? Constraints { get; init; }
    // Output image format.
    ClientImageCaptureFormat? Format { get; init; }
    // Desired image height in pixels.
    int? Height { get; init; }
    // How the capture is presented (native OS camera UI vs. headless silent grab). Defaults to CaptureImageMode.Headless — silent webcam capture via getUserMedia, which works uniformly on desktop and mobile. Set to CaptureImageMode.Native to opt in to the OS camera app on phones (preview + shutter + front/back toggle); on desktop browsers Native transparently falls back to the headless path because the web platform doesn't expose a camera-app launch.
    CaptureImageMode? Mode { get; init; }
    // Image quality (0.0 to 1.0) for lossy formats.
    double? Quality { get; init; }
    // Desired image width in pixels.
    int? Width { get; init; }
  // Hardware constraints for image capture. Applied directly when CaptureImageActionOptions.Mode is CaptureImageMode.Headless. In CaptureImageMode.Native mode only FacingMode is honored (mapped to the file input's capture attribute); the OS camera UI ignores other constraints.
  sealed record CaptureImageConstraints
    ctor()
    // Preferred camera device ID. Headless mode only.
    string? DeviceId { get; init; }
    // Preferred camera side (front vs. rear). Most useful on phones where CameraFacing.Environment opens the rear camera by default. On desktops with only a webcam this is ignored.
    CameraFacing? FacingMode { get; init; }
  // How the image capture is presented to the user. Controls whether the OS camera UI is invoked or whether the capture happens silently.
  enum CaptureImageMode
    // Open the native OS camera UI (preview + shutter + front/back toggle on phones). Best UX for "take a photo" buttons. On mobile this is implemented via a transient <input type="file" capture> and therefore must be invoked from a user gesture; the user can dismiss without capturing. On desktop browsers — where that input degrades to a plain file picker — the SDK transparently falls back to the headless getUserMedia grab so the click still produces a webcam frame.
    Native
    // Silent, headless capture: getUserMedia opens the camera, the SDK grabs a single frame off-screen and tears the stream down. No preview, no shutter. Useful for kiosks, automation, ID-scan flows where the timing is server-driven, or when you render your own preview UI elsewhere. Honors CaptureImageConstraints.
    Headless
  // Card-family composites: Card, StatCard, and EmptyState. All are server-side compositions over the container/text primitives styled by the Theming.Card / Theming.StatCard / Theming.EmptyState token recipes — beautiful by default, every part overridable.
  static class CardExtensions
    // With no style args it renders the themed card token (Theming.Card.Default, or Theming.Card.Interactive when onClick is set); caller styles replace it, or merge on top of it when the array leads with "default".
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the themed card base token.
    // title: Title text rendered in the card header.
    // description: Muted description text rendered under the title.
    // header: Builder for extra header elements rendered after the title/description.
    // content: Builder for the card body.
    // footer: Builder for the card footer (actions row).
    // headerStyle: Style for the header container. Defaults to Theming.Card.Header.
    // titleStyle: Style for the title text. Defaults to Theming.Card.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.Card.Description.
    // contentStyle: Style for the body container. Defaults to Theming.Card.Content when a header is present, plain padding otherwise.
    // footerStyle: Style for the footer container. Defaults to Theming.Card.Footer.
    // onClick: Invoked when the user clicks the card. Accepts sync (() => …) and async (async () => …) lambdas alike. When set, the interactive card token is used by default.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Card(this UIView view, string[]? style = null, string? title = null, string? description = null, Action<UIView>? header = null, Action<UIView>? content = null, Action<UIView>? footer = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? contentStyle = null, string[]? footerStyle = null, Delegate? onClick = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Card — positional (style, children) overload so view.Card([style], v => {...}) binds the lambda to the body instead of tripping on the title parameter.
    static void Card(this UIView view, string[]? style, Action<UIView> children)
    // Centered placeholder for empty lists/pages — optional icon, title, optional description, and an optional action row — per the theme's EmptyState recipe.
    // view: The UIView to render into.
    // title: Short headline (e.g. "No results yet").
    // style: Crosswind/Tailwind utility classes merged on top of Theming.EmptyState.Root.
    // description: Muted explanation text under the title.
    // icon: Lucide icon name rendered inside the tinted icon square.
    // action: Builder for the action row (e.g. a "Create" button).
    // iconWrapStyle: Style for the icon square. Defaults to Theming.EmptyState.IconWrap.
    // iconStyle: Style for the icon itself. Defaults to Theming.EmptyState.IconSize.
    // titleStyle: Style for the title text. Defaults to Theming.EmptyState.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.EmptyState.Description.
    // actionsStyle: Style for the action row. Defaults to Theming.EmptyState.Actions.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void EmptyState(this UIView view, string title, string[]? style = null, string? description = null, string? icon = null, Action<UIView>? action = null, string[]? iconWrapStyle = null, string[]? iconStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? actionsStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Dashboard statistic card — label, large value, optional delta with trend arrow, and an optional icon box — per the theme's StatCard recipe.
    // view: The UIView to render into.
    // label: Small muted metric label (e.g. "Revenue").
    // value: Large headline value (e.g. "€12,400").
    // style: Crosswind/Tailwind utility classes merged on top of Theming.StatCard.Root.
    // delta: Delta text rendered next to the value (e.g. "+12%").
    // trend: Direction of the delta — controls the trend arrow and its tone.
    // trendLabel: Muted context text after the delta (e.g. "vs last month").
    // icon: Lucide icon name rendered inside the tinted icon box on the right.
    // iconTone: SemanticTone of the icon box background/foreground.
    // labelStyle: Style for the label text. Defaults to Theming.StatCard.Label.
    // valueStyle: Style for the value text. Defaults to Theming.StatCard.Value.
    // trendStyle: Style for the delta row. Defaults to Theming.StatCard.Trend plus the trend tone.
    // iconBoxStyle: Style for the icon box. Defaults to the tone variant of Theming.StatCard.IconBox.
    // iconStyle: Style for the icon itself. Defaults to Theming.StatCard.IconSize.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void StatCard(this UIView view, string label, string value, string[]? style = null, string? delta = null, StatTrend trend = Flat, string? trendLabel = null, string? icon = null, SemanticTone iconTone = Neutral, string[]? labelStyle = null, string[]? valueStyle = null, string[]? trendStyle = null, string[]? iconBoxStyle = null, string[]? iconStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Alignment of slides relative to the carousel viewport.
  enum CarouselAlign
    // Align slides to the start of the viewport.
    Start
    // Align slides to the center of the viewport.
    Center
    // Align slides to the end of the viewport.
    End
  // Responsive carousel configuration applied above a container-width threshold.
  sealed record CarouselBreakpoint
    // MinWidth: Container width (in CSS pixels) at which this breakpoint becomes active. The active breakpoint is the one with the largest MinWidth still less than or equal to the current container width.
    // SlidesPerView: Number of slides visible in the viewport at this breakpoint.
    // SlidesPerGroup: Number of slides advanced per navigation step at this breakpoint. Defaults to SlidesPerView when null.
    // SlideGapPx: Gap in CSS pixels between adjacent slides at this breakpoint. Defaults to the top-level slideGapPx when null.
    ctor(int MinWidth, int SlidesPerView, int? SlidesPerGroup = null, int? SlideGapPx = null)
    // Container width (in CSS pixels) at which this breakpoint becomes active. The active breakpoint is the one with the largest MinWidth still less than or equal to the current container width.
    int MinWidth { get; init; }
    // Gap in CSS pixels between adjacent slides at this breakpoint. Defaults to the top-level slideGapPx when null.
    int? SlideGapPx { get; init; }
    // Number of slides advanced per navigation step at this breakpoint. Defaults to SlidesPerView when null.
    int? SlidesPerGroup { get; init; }
    // Number of slides visible in the viewport at this breakpoint.
    int SlidesPerView { get; init; }
  // Extension methods for Carousel components.
  static class CarouselExtensions
    // Provide slides via slides for the simple case, or via the content builder using Slide for fully custom children.
    // view: The UIView to render into.
    // index: Controlled zero-based slide index.
    // defaultIndex: Initial slide index for uncontrolled mode.
    // orientation: Scroll axis.
    // align: Alignment of slides in the viewport.
    // loop: When true, navigation wraps from last to first and vice versa.
    // autoPlayMs: When set, advances to the next slide every N milliseconds.
    // slidesPerView: Default number of slides visible in the viewport at once. Defaults to 1 (single-slide carousel). Set to a value greater than 1 for a multi-item carousel. Overridden by the matching entry in breakpoints when one applies.
    // slidesPerGroup: Default number of slides advanced per navigation step (arrows, indicators, autoplay). Defaults to slidesPerView so paging matches the visible window; set to 1 for one-at-a-time scrolling through a multi-item view. Overridden by the matching entry in breakpoints when one applies.
    // slideGapPx: Default gap in CSS pixels between adjacent slides. Only takes effect when the effective slides-per-view is greater than 1. Defaults to 0. Overridden by the matching entry in breakpoints when one applies.
    // breakpoints: Responsive configurations applied based on the carousel container width. The active breakpoint is the one with the largest MinWidth still less than or equal to the current container width; values fall back to the top-level slidesPerView / slidesPerGroup / slideGapPx when no breakpoint applies.
    // slides: Collection of slides rendered in order.
    // showArrows: When true, renders Previous/Next buttons. Defaults to true.
    // showIndicators: When true, renders indicator dots. Defaults to true.
    // previousLabel: Accessible label for the previous button.
    // nextLabel: Accessible label for the next button.
    // previousIconName: Icon name for the previous button.
    // nextIconName: Icon name for the next button.
    // rootStyle: Style for the outermost container. Use Carousel.Root.
    // viewportStyle: Style for the scrolling viewport. Use Carousel.Viewport.
    // slideStyle: Style applied to each slide.
    // previousStyle: Style for the previous button. Use Carousel.Previous.
    // nextStyle: Style for the next button. Use Carousel.Next.
    // indicatorsStyle: Style for the indicator bar. Use Carousel.Indicators.
    // indicatorStyle: Style for a single indicator dot.
    // indicatorActiveStyle: Style for the active indicator dot.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering custom slides via Slide.
    // onIndexChange: Invoked when the active slide index changes.
    static void Carousel(this UIView view, string[]? style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, int? slidesPerView = null, int? slidesPerGroup = null, int? slideGapPx = null, IEnumerable<CarouselBreakpoint>? breakpoints = null, IEnumerable<CarouselSlideItem>? slides = null, bool? showArrows = null, bool? showIndicators = null, string? previousLabel = null, string? nextLabel = null, string? previousIconName = null, string? nextIconName = null, string[]? rootStyle = null, string[]? viewportStyle = null, string[]? slideStyle = null, string[]? previousStyle = null, string[]? nextStyle = null, string[]? indicatorsStyle = null, string[]? indicatorStyle = null, string[]? indicatorActiveStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<int, Task>? onIndexChange = null)
    // A single slide inside a Carousel. Use when rendering slides manually.
    // view: The UIView to render into.
    // style: Style classes for the slide container.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this slide.
    static void Slide(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Declarative slide definition for CarouselExtensions.Carousel.
  sealed record CarouselSlideItem
    // Content: Builder function for rendering the slide.
    // Key: Optional stable key used for diffing.
    ctor(Action<UIView> Content, string? Key = null)
    // Builder function for rendering the slide.
    Action<UIView> Content { get; init; }
    // Optional stable key used for diffing.
    string? Key { get; init; }
  // Extension methods for the ChatLog primitive — the canonical chat-bubble layout shape: header + scrolling auto-scrolled body + composer. Wraps ScrollColumnExtensions.ScrollColumn with chat-friendly defaults so callers don't have to remember to set autoScroll: true.
  static class ChatLogExtensions
    // Use instead of a manual Column(overflow-auto) for any "newest at the bottom, follow when content grows" layout. autoScrollKey tells the framework when to re-anchor to the bottom — pass the reactive message collection, a count, or any other value that changes when the content does.
    // autoScrollKey: Anything whose value changes when the content changes — the reactive message collection, a count, or a composite string (see LayoutExtensions.ScrollArea).
    static void ChatLog(this UIView view, string[]? style = null, object? autoScrollKey = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, string? styleId = null, string? key = null)
  // Represents the checked state for checkbox-like components.
  enum CheckedState
    Unchecked
    Checked
    Indeterminate
  // Extension methods for the CodeEditor component.
  static class CodeEditorExtensions
    // Monospace code editor with an optional line-number gutter.
    // view: The UIView to render into.
    // value: Controlled text value. A controlled value with no write-back handler (no onValueChange, no onSubmit) renders the editor read-only, since edits would have nowhere to go.
    // defaultValue: Initial value for uncontrolled mode.
    // language: Language identifier used for syntax highlighting (e.g. typescript, csharp, json).
    // placeholder: Placeholder text shown when empty.
    // readOnly: When true, prevents editing but allows selection and copy.
    // disabled: When true, prevents user interaction entirely.
    // showLineNumbers: When true, renders a line-number gutter. Defaults to true.
    // tabSize: Number of spaces inserted by Tab. Defaults to 2.
    // insertSpaces: When true, Tab inserts spaces; when false, a tab character.
    // wrap: When true, long lines wrap instead of scrolling horizontally.
    // minRows: Minimum number of visible rows.
    // maxRows: Maximum number of rows before scrolling.
    // style: Style for the outermost container. Use CodeEditor.Root.
    // gutterStyle: Style for the line-number gutter. Use CodeEditor.Gutter.
    // contentStyle: Style for the code content area. Use CodeEditor.Content.
    // languageBadgeStyle: Style for the language badge in the top-right corner.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the text value changes.
    // onSubmit: Invoked when the user presses Ctrl+Enter. The parameter contains the submitted value — prefer it over re-reading the bound reactive.
    static void CodeEditor(this UIView view, string? value = null, string? defaultValue = null, string? language = null, string? placeholder = null, bool? readOnly = null, bool? disabled = null, bool? showLineNumbers = null, int? tabSize = null, bool? insertSpaces = null, bool? wrap = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? gutterStyle = null, string[]? contentStyle = null, string[]? languageBadgeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  // Represents collision detection strategy for @dnd-kit.
  enum CollisionDetection
    ClosestCenter
    ClosestCorners
    RectIntersection
    PointerWithin
  // Output string format for ColorPickerExtensions.ColorPicker.
  enum ColorFormat
    // Hex notation, e.g. #ff0000.
    Hex
    // CSS rgb() notation, e.g. rgb(255, 0, 0).
    Rgb
    // CSS hsl() notation, e.g. hsl(0, 100%, 50%).
    Hsl
  // Extension methods for ColorPicker components.
  static class ColorPickerExtensions
    // Swatch-triggered color picker with hue slider, saturation/lightness square, and hex input.
    // view: The UIView to render into.
    // value: Controlled color in the chosen format.
    // defaultValue: Initial color for uncontrolled mode.
    // format: Output format produced by onValueChange.
    // showAlpha: When true, shows an alpha slider and emits #RRGGBBAA/rgba()/hsla().
    // presets: Optional preset swatches displayed beneath the picker.
    // disabled: When true, prevents user interaction.
    // open: Controlled open state of the popover.
    // defaultOpen: Initial open state for uncontrolled mode.
    // side: Side the popover content renders against.
    // align: Alignment of the content along the chosen side.
    // triggerStyle: Style for the swatch trigger. Use ColorPicker.Trigger.
    // contentStyle: Style for the popover content container. Use ColorPicker.Content.
    // rootStyle: Style for the outermost container. Rarely needed.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked as the user drags or types a new color.
    // onValueCommit: Invoked once the user releases a drag or commits a typed value.
    // onOpenChange: Invoked when the popover open state changes.
    // label: Optional field label rendered above the color picker (same field ergonomics as TextField).
    static void ColorPicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, ColorFormat format = Hex, bool? showAlpha = null, IReadOnlyList<string>? presets = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onValueCommit = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Horizontal alignment for a content grid or data table column.
  enum ColumnAlign
    // Align content to the left.
    Left
    // Align content to the center.
    Center
    // Align content to the right.
    Right
  // Event returned from a contact picker action with the selected contacts.
  sealed record ContactsActionEvent : ActionEvent
    ctor(bool Success, IReadOnlyList<ClientContact>? Contacts)
    IReadOnlyList<ClientContact>? Contacts { get; init; }
  // Extension methods for container components.
  static class ContainerExtensions
    // Generic container element.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the user clicks the container. Accepts sync (() => …) and async (async () => …) lambdas alike. A clickable Box carries button semantics automatically — role="button", tabIndex=0 and Enter/Space activation — so it is reachable by keyboard, by assistive tech and by the app validator. Override either prop through props (e.g. ["role"] = "listitem"), and give an icon-only Box an ["aria-label"], since a button with no text content has no accessible name.
    // content: Builder function for rendering child elements within this component.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Box(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Box — positional (style, children) overload. Models reach for view.Box([style], v => {...}) with the lambda as the 2nd positional; without this overload it tries to bind to styleId (string?) and trips CS1660. The lambda parameter is named children (not content) so existing callers that use content: by name unambiguously match the original.
    static void Box(this UIView view, string[]? style, Action<UIView> children)
    // Container with vertical flexbox layout (flex-col).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Column(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Column — positional (style, children) overload.
    static void Column(this UIView view, string[]? style, Action<UIView> children)
    // Container with flexbox layout enabled.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Flex(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Flex — positional (style, children) overload (see Box).
    static void Flex(this UIView view, string[]? style, Action<UIView> children)
    // Container with CSS grid layout enabled.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Grid(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Grid — positional (style, children) overload.
    static void Grid(this UIView view, string[]? style, Action<UIView> children)
    // Absolutely positioned layer within a Stack container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Layer(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Layer — positional (style, children) overload (see Box).
    static void Layer(this UIView view, string[]? style, Action<UIView> children)
    // Container with horizontal flexbox layout (flex-row).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Row(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Row — positional (style, children) overload (see Box).
    static void Row(this UIView view, string[]? style, Action<UIView> children)
    // Loading spinner — an animated circular indicator for async/pending states. A typed convenience over the spin utility classes (equivalent to a div with the Theming.Icon.Spinner style): render it while waiting on data, e.g. if (_loading.Value) { view.Spinner(); }. Override colour/size via the style array; the default tracks the theme's muted foreground.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes appended to the base spinner styling (e.g. a colour or margin).
    // size: Spinner diameter — Sm, Md (default), or Lg.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Spinner(this UIView view, string[]? style = null, SpinnerSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Container for layering children on top of each other. Use with Layer components as children.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Stack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Stack — positional (style, children) overload (see Box).
    static void Stack(this UIView view, string[]? style, Action<UIView> children)
  // Defines a column in a content grid including optional header, width, flex, and alignment.
  record ContentGridColumn
    ctor(string? Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string? Header { get; init; }
    string? Width { get; init; }
  // Extension methods for CSS grid-based content layout.
  static class ContentGridExtensions
    // Renders a CSS grid layout with configurable columns, optional headers, and child content.
    static void ContentGrid(this UIView view, ContentGridColumn[] columns, Action<UIView>? content = null, string[]? style = null, string[]? headerStyle = null, string? key = null)
  // Options for copying text to the clipboard.
  sealed record CopyToClipboardActionOptions : ActionOptions
    ctor()
    // The text to copy.
    required string Text { get; init; }
  // Extension methods for core UI components including buttons, toggles, text inputs, dialogs, and typography.
  static class CoreExtensions
    // Button that triggers a client-side action (e.g., clipboard, download). Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // action: The type of action to perform.
    // text: Text displayed on the button. When content is provided, becomes aria-label for accessibility.
    // options: Configuration options for the action.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onActionComplete: Invoked when the action completes. The parameter contains action result details.
    // content: Builder function for rendering child elements within this component. When provided together with text, enables icon mode where text becomes aria-label.
    static void ActionButton(this UIView view, string[]? style = null, ActionKind action = Unknown, string? text = null, ActionOptions? options = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<ActionEvent, Task>? onActionComplete = null, Action<UIView>? content = null)
    // Clickable button that triggers an action. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // text: Text displayed on the button. When content is provided, becomes aria-label for accessibility.
    // disabled: When true, prevents user interaction with this component.
    // href: URL to navigate to when clicked. Renders as an anchor element when specified.
    // type: Button type attribute (e.g., "submit", "button", "reset").
    // target: Link target (e.g., "_blank" for new tab). Only applies when href is set.
    // rel: Link relationship (e.g., "noopener noreferrer"). Only applies when href is set.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the user clicks the button.
    // icon: Optional Lucide icon name (e.g. "arrow-right", "refresh-cw"). When set, renders the icon alongside the text using a horizontal flex layout. Use iconPosition to switch sides. For full custom icon layouts use content instead.
    // iconPosition: Align.Start (default) puts the icon before the text; Align.End puts it after. Ignored when icon is null.
    // content: Builder function for rendering child elements within this component. When provided together with text, enables icon mode where text becomes aria-label.
    // tooltip: Hover name for the button, rendered with the themed Tooltip. This is all an icon-only button needs: the tooltip also becomes the accessible name when nothing else names the control, so there is no aria-label to write by hand. An explicit text or props["aria-label"] still wins. Do NOT reach for a title prop instead — that is the browser's own unstyleable tooltip.
    // tooltipRootStyle: Styles for the tooltip's wrapper, which is the element that sits in the parent's layout — so responsive and positioning classes (hidden lg:inline-flex, absolute top-2 right-2) belong here, not on the button. Defaults to inline-flex shrink-0, which is what an icon button in a flex row wants.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Button(this UIView view, string[]? style = null, string? text = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null)
    // Button — positional-text-first overload accepting the label as the first argument.
    static void Button(this UIView view, string buttonText, string[]? style = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null)
    // Semantic heading element for titles and section headers.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // text: Heading text to display (alternative to content).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Heading(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Heading — positional-text-first overload, same rationale as the matching Text overload: view.Heading("Settings", style: [Text.H2]) is the shape models reach for. Parameter is named headingText to avoid ambiguity with callers using text: by name.
    static void Heading(this UIView view, string headingText, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Renders an icon from an icon library.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // name: Name of the icon in the icon library.
    // size: Icon size, the way Spinner takes one (size: IconSize.Lg). Merged as the icon's base sizing, so a w-*/h-* class in style — including the equivalent Theming.Icon.Xs..Xl token — still wins. Omit it to leave sizing entirely to style.
    // library: Icon library to use. Defaults to the view's default icon library.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering custom icon content (alternative to name).
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Icon(this UIView view, string[]? style = null, string? name = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Icon — positional-name-first overload. Same rationale as the matching Text overload: view.Icon("check", style: [Icon.Sm]) is the shape models reach for. Parameter is named iconName to avoid ambiguity with callers using name: by name.
    static void Icon(this UIView view, string iconName, string[]? style = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Inline anchor link — sugar for a `Button` styled like a hyperlink with an `href`. Mirrors HTML anchor semantics. By default opens in the same tab; pass target: "_blank" to open in a new tab (we automatically add `rel="noopener noreferrer"` for `_blank` if no other `rel` is provided). Generated code naturally reaches for `view.Link(text:, href:)`; this gives it the canonical shape rather than forcing every link into `view.Button(href:, …)`.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. Defaults to the theme's `Button.Link` styling — call-sites can override by passing any style array.
    // text: Link text shown to the user (the anchor body).
    // href: URL the link points to. Required. A same-origin path is routed in place: the client turns the click into a path change (surfacing as app.Navigation.PathChangedAsync) instead of reloading the document, so the connection survives it and the link needs no onClick to navigate.
    // target: Anchor target — pass `"_blank"` for new-tab. Default: same tab.
    // rel: Anchor rel attribute. When `target == "_blank"` and rel is null, defaults to `"noopener noreferrer"`. Pass `"external"` to force a full document load for a same-origin link.
    // onClick: Optional click handler (fires alongside navigation). For a side effect only — analytics, closing a menu — never to restate the destination `href` already names. Most use cases don't need this.
    // icon: Optional Lucide icon name rendered alongside the link text.
    // iconPosition: Align.Start (default) or Align.End — which side the icon sits on.
    // styleId: CSS class name applied directly. For exceptional cases.
    // key: Stable diff key.
    // props: HTML attributes forwarded to the anchor.
    // content: Custom child content; if provided, `text` becomes aria-label.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Link(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Link — positional-text-first overload. Same rationale as the matching Text overload: view.Link("Docs", href: "https://…") is the shape models reach for. Parameter is named linkText to avoid ambiguity with callers using text: by name.
    static void Link(this UIView view, string linkText, string[]? style = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Renders markdown content with formatting support.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling. With no array the body renders with Markdown.Default — heading scale, list markers, table rules, blockquote bar and a self-scrolling fenced-code box. Pass "default" as the first class to keep those and add your own on top; any other array replaces them.
    // content: Markdown text to render.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void Markdown(this UIView view, string[]? style = null, string? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Markdown — positional-content-first overload: view.Markdown("# Hello"). Parameter is named markdownContent to avoid ambiguity with callers using content: by name.
    static void Markdown(this UIView view, string markdownContent, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Text element for displaying content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // text: Text content to display.
    // href: URL to navigate to when clicked. Renders as an anchor element when specified.
    // target: Link target (e.g., "_blank" for new tab). Only applies when href is set.
    // rel: Link relationship (e.g., "noopener noreferrer"). Only applies when href is set.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Text(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Text element — positional-text-first overload accepting the content as the first argument.
    static void Text(this UIView view, string textContent, string[]? style = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Single toggle button.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled pressed state of the toggle.
    // defaultValue: The default pressed state when initially rendered. Use when not controlling the state.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the pressed state changes. The parameter is true when pressed, false when released.
    // content: Builder function for rendering child elements within this component.
    // label: Optional trailing text label. Wraps the toggle and the text in a <label>, so clicking the text toggles the control and the text is the toggle's accessible name.
    static void Toggle(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null)
    // Item within a toggle group.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The unique value for this toggle item within the group.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void ToggleGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggle group with multiple selection.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled list of selected toggle item values.
    // defaultValue: The default list of values when initially rendered. Use when not controlling the state.
    // rovingFocus: When true, enables roving tabindex for keyboard navigation between items.
    // loop: When true, keyboard navigation loops from last item to first, and vice versa.
    // orientation: The orientation of the toggle group for keyboard navigation.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the selection. The parameter contains the new list of selected values.
    // content: Builder function for rendering child elements within this component.
    static void ToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggle group with single selection.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled value of the selected toggle item.
    // defaultValue: The default value when initially rendered. Use when not controlling the state.
    // rovingFocus: When true, enables roving tabindex for keyboard navigation between items.
    // loop: When true, keyboard navigation loops from last item to first, and vice versa.
    // orientation: The orientation of the toggle group for keyboard navigation.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // content: Builder function for rendering child elements within this component.
    static void ToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
  // Represents the text direction for DirectionProvider.
  enum Dir
    Ltr
    Rtl
  // Extension methods for Accordion and Collapsible components.
  static class DisclosureExtensions
    // Content for an accordion item, collapsed or expanded.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AccordionContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Wraps an AccordionTrigger.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AccordionHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for an accordion item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the accordion item.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AccordionItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Accordion with multiple items open at a time.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the open accordion items.
    // defaultValue: Initial value for uncontrolled mode.
    // orientation: Layout orientation.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value.
    // content: Builder function for rendering child elements within this component.
    static void AccordionMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Accordion with single item open at a time.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the open accordion item.
    // defaultValue: Initial value for uncontrolled mode.
    // collapsible: Whether the open item can be collapsed.
    // orientation: Layout orientation.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value.
    // content: Builder function for rendering child elements within this component.
    static void AccordionSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? collapsible = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggles the collapsed state of an accordion item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AccordionTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Expandable/collapsible container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when open state changes.
    // content: Builder function for rendering child elements within this component.
    static void Collapsible(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Content that is shown or hidden.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void CollapsibleContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggles the collapsed state.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void CollapsibleTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Options for downloading a file to the client.
  sealed record DownloadFileActionOptions : ActionOptions
    ctor()
    // Binary data to download. When set, Url is auto-generated as a data URL.
    byte[]? Data { get; init; }
    // Suggested filename for the downloaded file.
    string? Filename { get; init; }
    // MIME type for binary data (e.g. "image/png"). Optional — defaults to "application/octet-stream" when Data is set without a MIME type.
    string? MimeType { get; init; }
    // URL to download. Can be a regular URL or a data URL. If Data is provided, this is auto-generated from the binary data using MimeType, falling back to "application/octet-stream" when MimeType is unset so the download still fires.
    string Url { get; init; }
  // Extension methods for drag and drop components.
  static class DragAndDropExtensions
    // Root context for drag and drop operations.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // collisionDetection: Algorithm for detecting which droppable is under the dragged item.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onDragStart: Invoked when a drag operation begins.
    // onDragMove: Invoked as the dragged item moves.
    // onDragOver: Invoked when the dragged item moves over a droppable.
    // onDragEnd: Invoked when the drag operation ends (dropped or cancelled).
    // onDragCancel: Invoked when the drag operation is cancelled.
    // activationDistance: Pixels of pointer movement required before a drag activates. When set, a pointerdown that doesn't move past this threshold is delivered as a normal click instead of starting a drag — so an inner Button.onClick fires. Leave null for the default behaviour (drag activates immediately).
    // content: Builder function for rendering child elements within this component.
    static void DndContext(this UIView view, string[]? style = null, CollisionDetection collisionDetection = ClosestCenter, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<DragStartArgs, Task>? onDragStart = null, Func<DragMoveArgs, Task>? onDragMove = null, Func<DragOverArgs, Task>? onDragOver = null, Func<DragEndArgs, Task>? onDragEnd = null, Func<Task>? onDragCancel = null, int? activationDistance = null, Action<UIView>? content = null)
    // Overlay shown while dragging.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // dropAnimation: When true, animates the drop action.
    // activeDragId: The ID of the currently dragged item. When set, the overlay only renders its content after the server has sent content matching this drag ID, preventing stale content from a previous drag.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering the drag overlay content.
    static void DragOverlay(this UIView view, string[]? style = null, bool? dropAnimation = true, string? activeDragId = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Element that can be dragged.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // id: Unique identifier for this draggable element.
    // disabled: When true, prevents user interaction with this component.
    // hideOnDrag: When true, hides the original element during drag. Use with DragOverlay.
    // data: Custom data attached to this draggable, available in drag event arguments.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Draggable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, bool? hideOnDrag = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Drop target area.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // id: Unique identifier for this droppable area.
    // disabled: When true, prevents user interaction with this component.
    // data: Custom data attached to this droppable, available in drag event arguments.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Droppable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Context for sortable list operations.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // items: List of item identifiers in the current sort order.
    // strategy: Sorting strategy (VerticalList or HorizontalList).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SortableContext(this UIView view, string[]? style = null, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Drag handle for a SortableItem. When a SortableHandle descendant is present, only pointerdown on the handle starts a drag; the rest of the item remains free for inner clickable elements like buttons. Place inside a SortableItem (or a SortableList itemContent). Outside a SortableItem the handle renders as a plain container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SortableHandle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Sortable item within a SortableContext.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // id: Unique identifier for this sortable item.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SortableItem(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Style slots: listStyle (container holding all sortable items), itemStyle (each item).
    // view: The UIView to render into.
    // items: List of item identifiers in the current sort order.
    // strategy: Sorting strategy (VerticalList or HorizontalList).
    // collisionDetection: Algorithm for detecting which droppable is under the dragged item.
    // onReorder: Invoked when items are reordered. The parameter contains the new order.
    // onDragStart: Invoked when a drag operation begins.
    // itemContent: Builder function for rendering each item's content. Receives the item id.
    // listStyle: Style classes for the container holding all sortable items.
    // itemStyle: Style classes applied to each sortable item.
    // activationDistance: Pixels of pointer movement required before a drag activates. When set, a pointerdown that doesn't move past this threshold is delivered as a normal click instead of starting a drag — so an inner Button.onClick fires. Leave null for the default behaviour (drag activates immediately).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void SortableList(this UIView view, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, CollisionDetection collisionDetection = ClosestCenter, Func<SortableReorderArgs, Task>? onReorder = null, Func<DragStartArgs, Task>? onDragStart = null, Action<UIView, string>? itemContent = null, string[]? listStyle = null, string[]? itemStyle = null, int? activationDistance = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Event args for drag cancel in @dnd-kit.
  sealed record DragCancelArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for drag end in @dnd-kit.
  sealed record DragEndArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag move in @dnd-kit.
  sealed record DragMoveArgs
    ctor(string ActiveId, double DeltaX, double DeltaY)
    string ActiveId { get; init; }
    double DeltaX { get; init; }
    double DeltaY { get; init; }
  // Event args for drag over in @dnd-kit.
  sealed record DragOverArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag start in @dnd-kit.
  sealed record DragStartArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for escape key down events on overlays.
  sealed record EscapeKeyDownArgs
    ctor()
  // Backed by a ClientReactive<T>: each client expands and collapses independently, and reads during UI rendering are dependency-tracked, so the tree re-renders automatically. Access it where a client scope is active (UI render or event handlers).
  sealed class ExpandedSet
    // Create the set, optionally pre-expanding the given node ids for every client.
    // expandedIds: Node ids that start expanded.
    ctor(params string[] expandedIds)
    // Collapse every node for the calling client.
    void Clear()
    // Collapse the node for the calling client.
    void Collapse(string id)
    // Expand the node for the calling client.
    void Expand(string id)
    // Whether the node is expanded for the calling client (reactive read).
    bool IsExpanded(string id)
    // Set the node's expanded state for the calling client.
    void Set(string id, bool expanded)
    // Toggle the node's expanded state for the calling client.
    void Toggle(string id)
  // Hint used by FeedSlide to preload the slide's primary media asset.
  enum FeedMediaKind
    // Slide has no external media to preload.
    None
    // Preload an image URL with an off-DOM Image.
    Image
    // Preload a video URL's metadata (not full payload) via a hidden <video preload="metadata">.
    Video
    // Preload the full video payload. Use sparingly — costs bandwidth.
    VideoFull
  // Performance model: • Native CSS scroll-snap drives the snap — no JS scroll loop. • Active slide is detected with IntersectionObserver, not scroll events. • Only slides inside [active - preloadBehind, active + preloadAhead] render their content; slides outside the window render as fixed-height spacers that preserve scroll position. • Media declared on FeedSlide is warmed with off-DOM Image/<video> elements as soon as a slide enters the preload window. • Autoplay is gated on the active slide only — neighbour videos are paused.
  static class FeedScrollerExtensions
    // Renders a TikTok-style vertical feed: each slide occupies the viewport and snaps into place.
    // view: The UIView to render into.
    // slides: Collection of slides rendered in order. Pass an async-growing list and use onScrollNearEnd to page more in.
    // activeIndex: Controlled zero-based index of the active (centered) slide.
    // defaultActiveIndex: Initial active slide for uncontrolled mode.
    // preloadAhead: Number of slides after the active one to mount and warm media for. Defaults to 2.
    // preloadBehind: Number of slides before the active one to keep mounted. Defaults to 1.
    // autoPlay: When true, videos on the active slide play automatically. Defaults to true.
    // muted: Controlled mute state applied to all media. Defaults to true (required for browser autoplay).
    // loop: When true, passing the last slide wraps to the first.
    // scrollEndThreshold: How many slides from the end before onScrollNearEnd fires. Defaults to 2.
    // style: Style for the outermost viewport container. Use FeedScroller.Root.
    // slideStyle: Style applied to every slide. Use FeedScroller.Slide.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onActiveChange: Invoked when the active slide changes. The parameter is the zero-based active slide index as an int (the same value as activeIndex).
    // onScrollNearEnd: Invoked when the user scrolls within scrollEndThreshold slides of the end — use this to fetch the next page of content. The parameter is the zero-based active slide index as an int (the same value as activeIndex).
    // onMuteChange: Invoked when the user toggles mute on an in-slide control.
    static void FeedScroller(this UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[]? style = null, string[]? slideStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<int, Task>? onActiveChange = null, Func<int, Task>? onScrollNearEnd = null, Func<bool, Task>? onMuteChange = null)
    // A single slide inside a FeedScroller. Use when rendering slides manually rather than via the FeedSlide declarative API.
    // view: The UIView to render into.
    // index: Zero-based index of this slide.
    // style: Style classes for the slide container.
    // mediaKind: Kind of media to preload for this slide.
    // mediaUrl: URL of the media asset.
    // mediaPoster: Optional poster image URL for video slides.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this slide.
    static void FeedSlide(this UIView view, int index, string[]? style = null, FeedMediaKind mediaKind = None, string? mediaUrl = null, string? mediaPoster = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // A single slide in a FeedScrollerExtensions.FeedScroller.
  sealed record FeedSlide
    // Content: Builder invoked to render the slide. Only slides inside the render window are realized.
    // Key: Stable key used for diffing and preload identity. Defaults to slide index.
    // MediaKind: Kind of media the slide needs preloaded.
    // MediaUrl: URL of the media asset matching MediaKind.
    // MediaPoster: Optional poster image URL for video slides.
    ctor(Action<UIView> Content, string? Key = null, FeedMediaKind MediaKind = None, string? MediaUrl = null, string? MediaPoster = null)
    // Builder invoked to render the slide. Only slides inside the render window are realized.
    Action<UIView> Content { get; init; }
    // Stable key used for diffing and preload identity. Defaults to slide index.
    string? Key { get; init; }
    // Kind of media the slide needs preloaded.
    FeedMediaKind MediaKind { get; init; }
    // Optional poster image URL for video slides.
    string? MediaPoster { get; init; }
    // URL of the media asset matching MediaKind.
    string? MediaUrl { get; init; }
  // Extension methods for file picker components. Unlike FileUploadExtensions.FileUpload, a FilePicker only opens the native file picker and reports selected file metadata to the server — it does not transfer bytes. The picked File handles are cached on the client and uploaded later by a FileUploadExtensions.FileUpload rendered with a matching seedSelectionIds prop.
  static class FilePickerExtensions
    // Only reports picked-file metadata to the server — the bytes stay on the client and are not uploaded until a FileUploadExtensions.FileUpload with a matching seedSelectionIds prop is mounted. Without an onValidationError handler, client-side rejections (e.g. over maxFileSize) are silent.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // multiple: When true, allows multiple file selection.
    // maxFileSize: Maximum file size in bytes (enforced client-side before emitting selection).
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    // props: Additional properties passed directly to the underlying component.
    // onFileSelected: Invoked once per picked file, with client-generated SelectionId and metadata.
    // onValidationError: Invoked when a picked file is rejected client-side (e.g. exceeds maxFileSize). Surface Reason to the user — without this the rejection is silent and looks like "click did nothing".
    // content: Builder function for custom content rendered inside the picker surface.
    static void FilePicker(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FilePickerSelectedArgs, Task>? onFileSelected = null, Func<FilePickerValidationErrorArgs, Task>? onValidationError = null, Action<UIView>? content = null)
  // Metadata for a file chosen in a FilePickerExtensions.FilePicker. The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
  sealed record FilePickerSelectedArgs
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  // Reported when client-side validation rejects a picked file (e.g. file too large for maxFileSize). Host UIs should surface Reason to the user — without a handler the rejection is silent and the user just sees "nothing happened" after clicking the picker.
  sealed record FilePickerValidationErrorArgs
    ctor(string FileName, string MimeType, long Size, string Reason)
    string FileName { get; init; }
    string MimeType { get; init; }
    string Reason { get; init; }
    long Size { get; init; }
  // Extension methods for file upload components.
  static class FileUploadExtensions
    // File upload component with explicit upload area, button click, drag-drop, and paste support.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // multiple: When true, allows multiple file selection.
    // maxFileSize: Maximum file size in bytes.
    // disabled: When true, prevents user interaction with this component.
    // allowPaste: When true, enables paste support for file upload.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onUploadPreStart: First accept/reject hook: invoked when a file upload is initiated, before the client hashes or transfers anything. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system. Receives a Cancel delegate for user-initiated cancellation.
    // onUploadStart: Second accept/reject hook: invoked after onUploadPreStart once the file hash is computed, before any data chunks arrive. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system.
    // onUploadProgress: Invoked with upload progress updates.
    // onUploadComplete: Invoked when a file upload completes successfully.
    // onUploadError: Invoked when a file upload fails, is cancelled, or times out.
    // onChunkReceived: Invoked when a file chunk is received during chunked upload.
    // seedSelectionIds: When set, on first mount the client looks these ids up in the shared pendingSelections stash (populated by a prior FilePickerExtensions.FilePicker) and uploads those File handles through the normal upload pipeline. Each SelectionId is reused verbatim as the UploadId.
    // content: Builder function for custom content inside the upload area.
    static void FileUpload(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string? capture = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, string[]? seedSelectionIds = null, Action<UIView>? content = null)
    // Style slots: zoneStyle (drop-zone container), activeStyle (applied while a file is dragged over the zone). The MIME filter is the NAMED accept: parameter — a leading positional array is always the zone style, never the filter.
    // view: The UIView to render into.
    // style: Style classes for the drop zone container (the ergonomic first-positional; alias of zoneStyle). The MIME filter is the NAMED accept: parameter — a leading positional array is always the zone style, never the filter.
    // multiple: When true, allows multiple file selection.
    // maxFileSize: Maximum file size in bytes.
    // disabled: When true, prevents user interaction with this component.
    // allowPaste: When true, enables paste support for file upload.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]). Pass by name.
    // onUploadPreStart: First accept/reject hook: invoked when a file upload is initiated, before the client hashes or transfers anything. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system. Receives a Cancel delegate for user-initiated cancellation.
    // onUploadStart: Second accept/reject hook: invoked after onUploadPreStart once the file hash is computed, before any data chunks arrive. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system.
    // onUploadProgress: Invoked with upload progress updates.
    // onUploadComplete: Invoked when a file upload completes successfully.
    // onUploadError: Invoked when a file upload fails, is cancelled, or times out.
    // onChunkReceived: Invoked when a file chunk is received during chunked upload.
    // onDragActiveChange: Invoked when drag active state changes. The parameter is true when drag enters, false when it leaves.
    // content: Builder function for rendering child elements to wrap with file upload capability.
    // zoneStyle: Style classes for the drop zone container.
    // activeStyle: Style classes applied when drag is active over the zone.
    // activeStyleId: CSS class name for the active drag state.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // seedSelectionIds: When set, on mount the client looks these ids up in the shared pendingSelections stash (populated by a prior FilePickerExtensions.FilePicker) and uploads those File handles through the normal upload pipeline. Each SelectionId is reused verbatim as the UploadId.
    // props: Additional properties passed directly to the underlying component.
    static void FileUploadZone(this UIView view, string[]? style = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string[]? accept = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, Func<bool, Task>? onDragActiveChange = null, Action<UIView>? content = null, string[]? zoneStyle = null, string[]? activeStyle = null, string? activeStyleId = null, string? styleId = null, string? key = null, string[]? seedSelectionIds = null, IReadOnlyDictionary<string, object>? props = null)
  // Extension methods for focus hint management.
  static class FocusHintExtensions
    // Requests focus attention for a UI element, typically for accessibility announcements.
    // view: The UIView to render into.
    // props: Configuration for the focus hint behavior.
    // key: Unique identifier for this focus hint request.
    // targetViewId: View ID to receive focus. Defaults to the current view.
    static void FocusHint(this UIView view, FocusHintProps props, string? key = null, Guid? targetViewId = null)
  // Configuration for a focus hint request including priority, ranking, and cooldown behavior.
  sealed record FocusHintProps
    ctor()
    // Minimum time between repeated focus hints for the same element.
    TimeSpan? Cooldown { get; init; }
    // When true, only moves focus without making an accessibility announcement.
    bool FocusOnly { get; init; }
    // Announcement priority level. Polite waits for idle; Assertive interrupts immediately.
    FocusPriority Priority { get; init; }
    // Numeric ranking to resolve conflicts when multiple hints compete.
    int Ranking { get; init; }
  // Event args for focus outside events on overlays.
  sealed record FocusOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Priority level for focus hint announcements, matching ARIA live region politeness.
  enum FocusPriority
    // Waits for the user agent to finish current tasks before announcing.
    Polite
    // Interrupts immediately to announce the change.
    Assertive
  // Extension methods for Form, Checkbox, RadioGroup, Switch, Slider, and Label components.
  static class FormExtensions
    // Checkbox control with simple boolean state. For tri-state support (indeterminate), use TriStateCheckbox.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled checked state of the checkbox.
    // defaultValue: The default checked state when initially rendered. Use when not controlling the state.
    // required: When true, indicates the checkbox must be checked before the form can be submitted.
    // disabled: When true, prevents user interaction with this component.
    // name: The name of the checkbox for form submission.
    // formValue: The HTML form value submitted when checked (the value attribute).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the checked state changes. The parameter is true when checked, false when unchecked.
    // content: Builder function for rendering child elements within this component.
    // label: Optional trailing text label — the idiomatic checkbox row. Wraps the checkbox and the text in a <label>, so clicking the text toggles the control and the text is the checkbox's accessible name. Prefer this to placing your own Text beside a bare Checkbox, which looks the same but associates nothing.
    // bind: Two-way binds the checkbox to a Reactive<T> in one call — reads bind.Value for the controlled state and writes it back on every toggle. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Checkbox(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    // Visual indicator for the checkbox state.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, forces the indicator to render even when the checkbox is unchecked.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void CheckboxIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Form container with validation support.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClearServerErrors: Invoked when server-side validation errors should be cleared.
    // content: Builder function for rendering child elements within this component.
    static void Form(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClearServerErrors = null, Action<UIView>? content = null)
    // Wraps the input control.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormControl(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for a form field with label and validation.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // name: The name of the form field, used for validation and form submission.
    // serverInvalid: When true, indicates the field has a server-side validation error.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormField(this UIView view, string[]? style = null, string? name = null, bool? serverInvalid = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Label for a form field.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormLabel(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Validation message for a form field.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // match: The validation condition that must be met for this message to display.
    // forceMatch: When true, forces the message to display regardless of the match condition.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormMessage(this UIView view, string[]? style = null, FormMessageMatch? match = null, bool? forceMatch = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Submit button for the form.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormSubmit(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Accessible label for form controls.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // htmlFor: The id of the element this label is associated with.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Label(this UIView view, string[]? style = null, string? htmlFor = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for radio buttons.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled value of the selected radio item.
    // defaultValue: The default value when initially rendered. Use when not controlling the state.
    // required: When true, indicates a selection must be made before the form can be submitted.
    // disabled: When true, prevents user interaction with this component.
    // loop: When true, keyboard navigation loops from last item to first, and vice versa.
    // orientation: The orientation of the radio group for keyboard navigation.
    // name: The name of the radio group for form submission.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // content: Builder function for rendering child elements within this component.
    // label: Optional group-level label rendered above the radio group (same field ergonomics as TextField).
    // bind: Two-way binds the group to a Reactive<T> in one call — reads bind.Value for the selected value and writes it back on every selection. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    static void RadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<string>? bind = null)
    // Visual indicator for the selected radio.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, forces the indicator to render even when the radio is not selected.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void RadioGroupIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual radio button.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The unique value for this radio item within the group.
    // disabled: When true, prevents user interaction with this component.
    // required: When true, indicates this radio item must be selected before the form can be submitted.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void RadioGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, bool? required = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Range slider control.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled value of the slider as a list of thumb positions.
    // defaultValue: The default value when initially rendered. Use when not controlling the state.
    // min: The minimum value for the slider.
    // max: The maximum value for the slider.
    // step: The stepping interval between selectable values.
    // minStepsBetweenThumbs: The minimum number of steps between thumbs in a multi-thumb slider.
    // orientation: The orientation of the slider.
    // disabled: When true, prevents user interaction with this component.
    // inverted: When true, inverts the slider direction.
    // name: The name of the slider for form submission.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the value changes during dragging. The parameter contains the current thumb positions.
    // onValueCommit: Invoked when the user finishes dragging. The parameter contains the final thumb positions.
    // content: Builder function for rendering the slider's track and thumbs. Note: a read-only slider (a controlled value: with no write-back handler) is still functionally inert — the root gates all writes — but the default content carries the aria-readonly signal on its thumb, which the slider's root cannot legally hold. Custom content that replaces the thumbs should put aria-readonly="true" on each thumb to keep the a11y state, or omit content: and style the default thumbs via the theme tokens.
    // label: Optional field label rendered above the slider (same field ergonomics as TextField). It also becomes the accessible name of the slider's thumbs, which is where role="slider" lives — a name left on the root names nothing. Thumbs on a multi-thumb range are numbered from it.
    // bind: Two-way binds a single-thumb slider to a Reactive<T> in one call — reads bind.Value for the thumb position and writes it back as the user drags. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back. For multi-thumb ranges use the value: list form.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Slider(this UIView view, string[]? style = null, IReadOnlyList<double>? value = null, IReadOnlyList<double>? defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<double>, Task>? onValueChange = null, Func<IReadOnlyList<double>, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null, Reactive<double>? bind = null, string? ariaLabel = null)
    // Single-thumb slider with a scalar value — the common case. Sugar over the list form so callers write Slider(value: 50, onValueChange: async v => …) without the one-element-list dance. Use the list form for multi-thumb ranges.
    static void Slider(this UIView view, double value, string[]? style = null, double? min = null, double? max = null, double? step = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onValueChange = null, Func<double, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null)
    // Filled range portion of the slider.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void SliderRange(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Draggable thumb on the slider.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void SliderThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Track for the slider.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SliderTrack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggle switch control.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled checked state of the switch.
    // defaultValue: The default checked state when initially rendered. Use when not controlling the state.
    // required: When true, indicates the switch must be checked before the form can be submitted.
    // disabled: When true, prevents user interaction with this component.
    // name: The name of the switch for form submission.
    // formValue: The HTML form value submitted when checked (the value attribute).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the checked state changes. The parameter is true when checked, false when unchecked.
    // content: Builder function for rendering child elements within this component.
    // label: Optional trailing text label. Wraps the switch and the text in a <label>, so clicking the text toggles the control and the text is the switch's accessible name — a switch has no name of its own, so this or an aria-label is what keeps it from being announced as an unlabelled control.
    // bind: Two-way binds the switch to a Reactive<T> in one call — reads bind.Value for the controlled state and writes it back on every toggle. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Switch(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    // The thumb that moves when the switch is toggled.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SwitchThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Checkbox control with tri-state support (checked, unchecked, indeterminate).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled tri-state value: Checked, Unchecked, or Indeterminate.
    // defaultValue: The default tri-state value when initially rendered. Use when not controlling the state.
    // required: When true, indicates the checkbox must be checked before the form can be submitted.
    // disabled: When true, prevents user interaction with this component.
    // name: The name of the checkbox for form submission.
    // formValue: The HTML form value submitted when checked (the value attribute).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the checked state changes. The parameter contains the new CheckedState value.
    // content: Builder function for rendering child elements within this component.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void TriStateCheckbox(this UIView view, string[]? style = null, CheckedState? value = null, CheckedState? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onValueChange = null, Action<UIView>? content = null, string? ariaLabel = null)
  // Represents form validation message types matching browser constraint validation.
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
  // Hour display format for TimePickerExtensions.TimePicker.
  enum HourFormat
    // 24-hour display, e.g. 13:45.
    Hour24
    // 12-hour display with AM/PM, e.g. 1:45 PM.
    Hour12
  // Size of an Icon — the size: form of the Theming.Icon.Xs..Xl tokens, so an icon sizes the same way a Spinner does (size: IconSize.Lg). The style-array form (view.Icon([Icon.Lg], name: "check")) stays valid and, being a caller class, still wins over size: when both are given.
  enum IconSize
    Xs
    Sm
    Md
    Lg
    Xl
  // Event returned from an image capture action with the captured image data.
  sealed record ImageCaptureActionEvent : ActionEvent
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string? Data { get; init; }
    int Height { get; init; }
    string? Mime { get; init; }
    int Width { get; init; }
  // Extension methods for image and avatar components.
  static class ImageExtensions
    // Avatar container with image and fallback.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Avatar(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Fallback content shown when image fails to load.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // delayMs: Delay in milliseconds before showing the fallback.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AvatarFallback(this UIView view, string[]? style = null, int? delayMs = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Image element for the avatar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // src: URL of the image to display.
    // alt: Alternative text description for accessibility.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onLoadingStatusChange: Invoked when the image loading status changes.
    static void AvatarImage(this UIView view, string[]? style = null, string? src = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onLoadingStatusChange = null)
    // Image element.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // src: URL or path to the image source.
    // assetUri: Asset URI to resolve the image source from. Takes precedence over src.
    // alt: Alternative text description for accessibility.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, string? src = null, AssetUri? assetUri = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
    // Image element with binary data payload.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // data: Binary image data.
    // mimeType: MIME type of the image (e.g., "image/png", "image/jpeg").
    // alt: Alternative text description for accessibility.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, byte[]? data = null, string? mimeType = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
  // Extension methods for input components (TextField, TextArea, OTP, Password).
  static class InputExtensions
    // One-time password input field.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value of the OTP input. A controlled value with no onValueChange renders the field read-only, since entered digits would have nowhere to go.
    // maxLength: Maximum number of characters allowed.
    // autoSubmit: When true, automatically triggers onAutoSubmit when all characters are entered.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // onAutoSubmit: Invoked when all characters are entered and autoSubmit is enabled.
    // content: Builder function for rendering OtpFieldInput slots within this component.
    // label: Optional field label rendered above the OTP slots (same field ergonomics as TextField).
    static void OtpField(this UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string? label = null)
    // Individual input slot for OTP.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // index: Zero-based index of this slot in the OTP field.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void OtpFieldInput(this UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Password input with visibility toggle.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // visible: Controlled visibility state. When true, password is shown as plain text.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onVisibilityChange: Invoked when visibility changes. The parameter is true when visible, false when hidden.
    // content: Builder function for rendering PasswordToggleFieldInput and PasswordToggleFieldToggle within this component.
    // label: Optional field label rendered above the field (same field ergonomics as TextField).
    static void PasswordToggleField(this UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string? label = null)
    // Icon that changes based on visibility state.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // visibleIcon: Builder function for the icon shown when password is visible.
    // hiddenIcon: Builder function for the icon shown when password is hidden.
    static void PasswordToggleFieldIcon(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null)
    // The password input element.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // autoComplete: Browser autocomplete hint (e.g., "current-password", "new-password").
    // placeholder: Placeholder text shown when empty.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void PasswordToggleFieldInput(this UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Button to toggle password visibility.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering the toggle button content (typically an icon).
    static void PasswordToggleFieldToggle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Multi-line text input area.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value of the input. A controlled value with no write-back handler (no bind, no onValueChange, no onSubmit) renders the input read-only, since edits would have nowhere to go.
    // defaultValue: Initial value for uncontrolled mode.
    // placeholder: Placeholder text shown when empty.
    // disabled: When true, prevents user interaction with this component.
    // rows: Number of visible text rows.
    // autoResize: When true, the textarea grows to fit its content up to maxRows, then scrolls.
    // maxRows: Upper bound on visible rows when autoResize is true. Defaults to 6 if not specified.
    // submitOnEnter: When true, plain Enter submits and Shift+Enter inserts a newline. Default is false (Ctrl/Cmd+Enter submits, Enter inserts newline) — matches the platform default.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // onSubmit: Invoked when the user submits (e.g., Ctrl+Enter). The parameter contains the submitted value — prefer it over re-reading the bound reactive, which may lag the keystroke (onValueChange is a separate round-trip and is not guaranteed to land before onSubmit).
    // onSubmitWithContext: Invoked when the user submits, with additional context information.
    // clearOnSubmit: When true, clears the input value after submit. Defaults to true when onSubmit/onSubmitWithContext is set.
    // content: Builder function for rendering child elements within this component.
    // autoFocus: When true, the input takes keyboard focus as soon as it mounts — e.g. an inline add/edit form that appears on a click. Defaults to false.
    // label: Optional field label rendered above the textarea (same field ergonomics as TextField).
    // debounceMs: Throttles the onValueChange round-trip, in milliseconds.
    // bind: Two-way binds the textarea to a Reactive<T> in one call — reads bind.Value for the controlled value and writes bind.Value = v on every keystroke. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void TextArea(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, string? ariaLabel = null)
    // Controlled/read-only rule (shared by every input component — text, select, checkbox, calendar, color, OTP, …): passing a controlled value: with no write-back handler (bind:, onValueChange:, or onSubmit:) renders the field read-only, since edits would have nowhere to go. Pass bind: <reactive> to two-way bind a Reactive<T> in one call, or value: together with an onValueChange:/onSubmit: handler.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value of the input. A controlled value with no write-back handler (no bind, no onValueChange, no onSubmit) renders the input read-only, since edits would have nowhere to go.
    // defaultValue: Initial value for uncontrolled mode.
    // placeholder: Placeholder text shown when empty.
    // disabled: When true, prevents user interaction with this component.
    // type: Input type (e.g., "text", "email", "number", "password").
    // step: Step increment for number inputs.
    // min: Minimum value for number inputs.
    // max: Maximum value for number inputs.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // onSubmit: Invoked when the user presses Enter. The parameter contains the submitted value — prefer it over re-reading the bound reactive, which may lag the keystroke (onValueChange is a separate round-trip and is not guaranteed to land before onSubmit).
    // clearOnSubmit: When true, clears the input value after submit. Defaults to clearing only when an onSubmit handler is set (matching TextArea); a bound field with no onSubmit is not emptied on Enter. Pass true/false to override.
    // content: Builder function for rendering child elements within this component.
    // autoFocus: When true, the input takes keyboard focus as soon as it mounts — e.g. an inline add/edit form that appears on a click. Defaults to false.
    // label: Optional field label rendered above the input, wrapped together with it in a Column.
    // debounceMs: Throttles the onValueChange round-trip, in milliseconds.
    // bind: Two-way binds the field to a Reactive<T> in one call — reads bind.Value for the controlled value and writes bind.Value = v on every keystroke. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // multiline: MUI / Chakra / Material-style API: TextField(multiline: true, rows: 4). Parallax has a dedicated TextArea component, but the multi-line use case is so commonly expressed as "TextField with multiline=true" that accepting it here saves the caller from learning a different component name. Delegates to TextArea.
    // rows: Number of visible text rows; setting it implies multiline (delegates to TextArea).
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void TextField(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, bool? multiline = null, int? rows = null, string? ariaLabel = null)
  // Event args for interact outside events on overlays (combines pointer and focus).
  sealed record InteractOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // String constants for common keyboard key names, matching the browser KeyboardEvent.key specification. Use these with KeyboardExtensions.KeyboardListener for type-safe key filtering. Raw strings can also be used for uncommon keys not listed here.
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
  // Event args for keyboard events, matching the browser KeyboardEvent properties.
  sealed record KeyboardEventArgs
    ctor(string Key, string Code, bool AltKey, bool CtrlKey, bool MetaKey, bool ShiftKey, bool Repeat)
    bool AltKey { get; init; }
    string Code { get; init; }
    bool CtrlKey { get; init; }
    string Key { get; init; }
    bool MetaKey { get; init; }
    bool Repeat { get; init; }
    bool ShiftKey { get; init; }
  // Extension methods for keyboard input listening.
  static class KeyboardExtensions
    // Listens for keyboard events and invokes callbacks on key presses.
    // view: The UIView to render into.
    // onKeyDown: Invoked when a key is pressed down.
    // onKeyUp: Invoked when a key is released.
    // keys: Optional filter: only forward events for these key names (use Key constants). When null, all key events are forwarded.
    // global: When true (default), listens on the document level. When false, listens only on the wrapper element.
    // requireCtrlOrMeta: When true, the CLIENT ignores events without Ctrl or Cmd held — the filter every ⌘X-style shortcut needs. Filtering only in the server callback is not enough: preventDefault applies client-side to every matched key, so a bare-key shortcut without this flag swallows that letter in every text field of the app (a global ["k"] + preventDefault listener made the letter k untypeable product-wide).
    // preventDefault: When true, prevents the default browser behavior for matched keys. For modifier shortcuts, pair with requireCtrlOrMeta — see its remarks.
    // stopPropagation: When true, stops event propagation for matched keys.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void KeyboardListener(this UIView view, Func<KeyboardEventArgs, Task>? onKeyDown = null, Func<KeyboardEventArgs, Task>? onKeyUp = null, IReadOnlyList<string>? keys = null, bool? global = true, bool? requireCtrlOrMeta = null, bool? preventDefault = null, bool? stopPropagation = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Extension methods for scroll area and layout components.
  static class LayoutExtensions
    // Maintains a specific aspect ratio for content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // ratio: The width-to-height ratio to maintain (e.g., 16.0/9.0 for widescreen).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AspectRatio(this UIView view, string[]? style = null, double ratio = 1.0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Provides text direction context (ltr/rtl) to descendants.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // dir: Text direction for descendants.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void DirectionProvider(this UIView view, string[]? style = null, Dir dir = Ltr, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb; rootStyle rarely needed.
    // view: The UIView to render into.
    // threshold: Distance from end (in pixels) to trigger onNearEnd. Default 200.
    // debounceMs: Debounce time in ms to prevent rapid callback firing. Default 100.
    // loading: When true, shows loading indicator and prevents duplicate callbacks.
    // hasMore: When false, disables the onNearEnd callback (end of data reached).
    // direction: Whether to detect scroll near end going Down (append) or Up (prepend).
    // scrollbars: Which scrollbars to display.
    // loadingIndicator: Builder for custom loading indicator content.
    // onNearEnd: Invoked when user scrolls near the end of content.
    // content: Builder function for rendering child elements.
    // viewportStyle: Style classes for the viewport element. Use ScrollArea.Viewport for default styling.
    // scrollbarStyle: Style classes for the scrollbar elements. Use ScrollArea.Scrollbar for default styling.
    // thumbStyle: Style classes for the scrollbar thumb elements. Use ScrollArea.Thumb for default styling.
    // rootStyle: Style for the outermost container. Rarely needed; prefer styling the viewport instead.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    // props: Additional properties passed directly to the underlying component.
    static void InfiniteScrollView(this UIView view, string[]? style = null, int threshold = 200, int debounceMs = 100, bool loading = false, bool hasMore = true, ScrollDirection direction = Down, ScrollAreaScrollbars scrollbars = Vertical, Action<UIView>? loadingIndicator = null, Func<ScrollNearEndArgs, Task>? onNearEnd = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // SemanticTone.Success, SemanticTone.Warning, and SemanticTone.Error tones map to the matching Theming.Progress.Variant tokens; other tones use the default (brand) fill. Style slots: rootStyle → Progress.Root, indicatorStyle → Progress.Indicator.
    // view: The UIView to render into.
    // value: Controlled value representing current progress.
    // max: Maximum value for the progress indicator.
    // tone: Semantic tone of the indicator fill.
    // indeterminate: When true, displays an indeterminate progress animation.
    // getValueLabel: Function to format the value for display.
    // rootStyle: Style classes for the progress track/container. Use Progress.Root for default styling.
    // indicatorStyle: Style classes for the progress indicator element. Use Progress.Indicator for default styling.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void Progress(this UIView view, string[]? style = null, double? value = null, double? max = null, SemanticTone tone = Neutral, bool indeterminate = false, Func<double?, string>? getValueLabel = null, string[]? rootStyle = null, string[]? indicatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Resizable split panel with a drag handle between two panes. Resize is handled entirely on the client — only the final size is sent to the server via onResized.
    static void ResizableSplit(this UIView view, Orientation orientation = Horizontal, double initialSize = 200.0, double minSize = 100.0, double maxSize = 500.0, bool reversed = false, Func<double, Task>? onResized = null, Action<UIView>? first = null, Action<UIView>? second = null, string[]? style = null, string[]? handleStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb, cornerStyle (when both scrollbars show); rootStyle rarely needed.
    // view: The UIView to render into.
    // scrollbars: Which scrollbars to display (Vertical, Horizontal, or Both).
    // type: Scrollbar visibility behavior (Always, Scroll, Hover, or Auto).
    // scrollHideDelay: Delay in milliseconds before hiding scrollbars when type is Scroll or Hover.
    // dir: Text direction (Ltr or Rtl).
    // autoScroll: When true, automatically scrolls to the bottom when content changes. Ideal for chat interfaces.
    // autoScrollKey: Anything whose value changes when the content changes — auto-scroll re-fires when it does. Pass the collection itself (autoScrollKey: _messages — any reactive contributes its change version), a count (autoScrollKey: _messages.Count), or a composite string. Required when autoScroll is true.
    // content: Builder function for rendering child elements within this component.
    // viewportStyle: Style classes for the viewport element. Use ScrollArea.Viewport for default styling.
    // scrollbarStyle: Style classes for the scrollbar elements. Use ScrollArea.Scrollbar for default styling.
    // thumbStyle: Style classes for the scrollbar thumb elements. Use ScrollArea.Thumb for default styling.
    // cornerStyle: Style classes for the corner element.
    // rootStyle: Style for the outermost ScrollArea container. Rarely needed; prefer styling the viewport instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void ScrollArea(this UIView view, string[]? style = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType type = Hover, int? scrollHideDelay = null, Dir dir = Ltr, bool autoScroll = false, object? autoScrollKey = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? cornerStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // ScrollArea — positional (style, children) overload (see ContainerExtensions.Box).
    static void ScrollArea(this UIView view, string[]? style, Action<UIView> children)
    // Visual separator between content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // orientation: Whether the separator is horizontal or vertical.
    // decorative: When true, the separator is purely visual and not announced by screen readers.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void Separator(this UIView view, string[]? style = null, Orientation orientation = Horizontal, bool decorative = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Event returned from a geolocation action with latitude/longitude coordinates.
  sealed record LocationActionEvent : ActionEvent
    ctor(bool Success, double? Latitude, double? Longitude, double? Accuracy)
    double? Accuracy { get; init; }
    double? Latitude { get; init; }
    double? Longitude { get; init; }
  // Specifies the behavior of a CaptureButton when pressed.
  enum MediaCaptureButtonMode
    // Start capturing when pressed; stop capturing when released.
    Hold
    // Toggle capturing on and off when pressed.
    Toggle
  // Event data for media capture start/stop callbacks, containing the stream identifier and capture kind. ClientContext identifies the user who initiated the capture and is populated for all capture kinds (audio, camera, screen). Prefer reading ClientSessionId / UserId rather than tracking streamId-to-client mappings yourself.
  sealed record MediaCaptureEvent
    ctor(string StreamId, MediaCaptureKind Kind)
    // Client context of the user who initiated the capture.
    Context? ClientContext { get; init; }
    // Client session id of the user who initiated the capture.
    int? ClientSessionId { get; }
    MediaCaptureKind Kind { get; init; }
    string StreamId { get; init; }
    // User id of the user who initiated the capture.
    string? UserId { get; }
  // Specifies the type of media to capture with a CaptureButton.
  enum MediaCaptureKind
    // Capture audio from the user's microphone.
    Audio
    // Capture video from the user's camera.
    Camera
    // Capture the user's screen.
    Screen
  // Extension methods for media playback components.
  static class MediaExtensions
    // Audio player for URL-based audio content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // url: URL of the audio source.
    // controls: When true, displays audio playback controls.
    // autoplay: When true, audio starts playing automatically.
    // loop: When true, audio loops continuously.
    // muted: When true, audio is muted.
    // preload: Specifies if/how the audio should be loaded when the page loads ("none", "metadata", or "auto").
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void AudioUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, string? preload = null, string? styleId = null, string? key = null)
    // Animated audio level bars — the "you are being heard" cue. Pure presentation: it renders the same loop whatever the microphone is doing, because per-frame amplitude would cost a server round trip per frame and the point of this cue is that it costs none. The usual placement is over the text input, so pressing a PushToTalkButton visibly turns the field into a recording surface rather than leaving a button to look toggled. Give the row containing both the button and the wave the group class and style the wave's container with Theming.MicButton.WhileCapturing: the reveal then keys on the client-stamped data-ikon-capture-active attribute and lands on press, with no server involvement.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged over Theming.AudioWave.Root.
    // bars: How many bars to draw.
    // barStyle: Style for each bar. Defaults to Theming.AudioWave.Bar.
    // key: Stable diffing key.
    static void AudioWave(this UIView view, string[]? style = null, int bars = 7, string[]? barStyle = null, string? key = null)
    // Button that captures media (audio, camera, or screen) based on the specified kind. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // kind: The type of media to capture (Audio, Camera, or Screen).
    // text: Text displayed on the button. When content is provided, becomes aria-label for accessibility.
    // captureMode: Whether to hold the button to capture (Hold) or toggle capture on/off (Toggle).
    // audioOptions: Audio capture configuration options. Used when kind is Audio.
    // videoOptions: Video capture configuration options. Used when kind is Camera or Screen.
    // holdReleaseDelayMs: In Hold mode, delays stopping capture by this many milliseconds after the button is released. Useful for speech capture where users may release the button slightly before finishing their sentence.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onCaptureStart: Invoked when capture begins. The parameter contains capture event details.
    // onCaptureStop: Invoked when capture ends. The parameter contains capture event details.
    // content: Builder function for rendering child elements within this component. When provided together with text, enables icon mode where text becomes aria-label.
    static void CaptureButton(this UIView view, string[]? style = null, MediaCaptureKind kind = Audio, string? text = null, MediaCaptureButtonMode captureMode = Hold, ClientAudioCaptureOptions? audioOptions = null, ClientVideoCaptureOptions? videoOptions = null, int? holdReleaseDelayMs = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Toggle microphone button: a CaptureButton(kind: Audio, captureMode: Toggle). Tap to open the microphone, tap again to close it — the segment in between is one utterance. After enabling speech recognition once (Audio.UseSpeechRecognition(...)), subscribe to Audio.SpeechRecognizedAsync to receive the transcription when the mic is toggled off, exactly like PushToTalkButton. Ships the same MicButton.Default themed default: the button stays visibly red (via the zero-latency data-ikon-capture-active attribute) for as long as the mic is open — essential for a toggle, where an invisible open mic means recording without knowing it. A custom style array replaces the default; start with "default" to layer, or include MicButton.Active.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // text: Text or icon shown on the button.
    // audioOptions: Audio capture configuration options (auto gain, noise suppression, etc.).
    // disabled: When true, prevents user interaction.
    // styleId: CSS class name to apply directly. Prefer style.
    // key: Stable diffing key.
    // props: Additional properties forwarded to the underlying component.
    // onCaptureStart: Optional callback fired when the mic opens (audio path; race-free with frame handlers).
    // onCaptureStop: Optional callback fired when the mic closes.
    // content: Optional child content; when provided, text becomes the aria-label.
    static void MicToggleButton(this UIView view, string[]? style = null, string? text = "🎤", ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Enable speech recognition once via Audio.UseSpeechRecognition(...), then subscribe to Audio.SpeechRecognizedAsync to receive transcriptions when the button is released; the initiating user's client context is carried on the event args.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // text: Text or icon shown on the button.
    // holdReleaseDelayMs: Delay before stopping capture after release. Useful for trailing-syllable tolerance.
    // audioOptions: Audio capture configuration options (auto gain, noise suppression, etc.).
    // disabled: When true, prevents user interaction.
    // styleId: CSS class name to apply directly. Prefer style.
    // key: Stable diffing key.
    // props: Additional properties forwarded to the underlying component.
    // onCaptureStart: Optional callback fired when capture begins (audio path; race-free with frame handlers).
    // onCaptureStop: Optional callback fired when capture ends.
    // content: Optional child content; when provided, text becomes the aria-label.
    static void PushToTalkButton(this UIView view, string[]? style = null, string? text = "⏺", int holdReleaseDelayMs = 500, ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Canvas element for rendering a live video stream.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // streamId: Identifier of the video stream to display.
    // width: Width of the canvas in pixels.
    // height: Height of the canvas in pixels.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void VideoStreamCanvas(this UIView view, string[]? style = null, string? streamId = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
    // Video player for URL-based video content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // url: URL of the video source.
    // controls: When true, displays video playback controls.
    // autoplay: When true, video starts playing automatically.
    // loop: When true, video loops continuously.
    // muted: When true, video is muted.
    // playsInline: When true, plays inline on mobile devices instead of fullscreen.
    // poster: URL of the poster image shown before playback.
    // width: Width of the video player in pixels.
    // height: Height of the video player in pixels.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void VideoUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, bool? playsInline = null, string? poster = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  // Extension methods for NavigationMenu, Menubar, and Toolbar components.
  static class NavigationExtensions
    // Menubar root container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // dir: Text direction (ltr/rtl).
    // loop: When true, keyboard navigation wraps.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void Menubar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Checkbox item in menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // isChecked: Checked state for checkbox items.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onCheckedChange: Invoked when checked changes.
    // content: Builder function for rendering child elements within this component.
    static void MenubarCheckboxItem(this UIView view, string[]? style = null, CheckedState isChecked = Unchecked, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onCheckedChange = null, Action<UIView>? content = null)
    // Dropdown content for the menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // loop: When true, keyboard navigation wraps.
    // side: Which side content appears on.
    // align: Content alignment.
    // sideOffset: Pixel offset from anchor on the side axis.
    // alignOffset: Pixel offset from anchor on the align axis.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarContent(this UIView view, string[]? style = null, bool loop = true, Side side = Bottom, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Clickable menu item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onSelect: Invoked when item is selected.
    // content: Builder function for rendering child elements within this component.
    static void MenubarItem(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // Visual indicator for checkbox/radio state.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarItemIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual menu in the menubar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarMenu(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Radio group in menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void MenubarRadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Radio item in menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarRadioItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Separator between menu items.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void MenubarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Submenu container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when open state changes.
    // content: Builder function for rendering child elements within this component.
    static void MenubarSub(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Content for submenu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // loop: When true, keyboard navigation wraps.
    // side: Which side content appears on.
    // align: Content alignment.
    // sideOffset: Pixel offset from anchor on the side axis.
    // alignOffset: Pixel offset from anchor on the align axis.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarSubContent(this UIView view, string[]? style = null, bool loop = true, Side side = Right, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Trigger for submenu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarSubTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Button that opens a menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Navigation menu root.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // orientation: Layout orientation.
    // delayDuration: Timing delay in milliseconds.
    // skipDelayDuration: Skip delay duration in milliseconds.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenu(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, int? delayDuration = null, int? skipDelayDuration = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Content shown when navigation item is active.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Visual indicator for active navigation item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual navigation menu item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuItem(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Link within navigation menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // active: Whether item is marked as active.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onSelect: Invoked when item is selected.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuLink(this UIView view, string[]? style = null, bool? active = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // List of navigation menu items.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuList(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Trigger that opens navigation content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Viewport for navigation menu content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuViewport(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toolbar container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // orientation: Layout orientation.
    // dir: Text direction (ltr/rtl).
    // loop: When true, keyboard navigation wraps.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Toolbar(this UIView view, string[]? style = null, Orientation orientation = Horizontal, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Button in the toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the button is clicked.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarButton(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null)
    // Link in the toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // href: URL to navigate to.
    // target: Link target attribute.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarLink(this UIView view, string[]? style = null, string? href = null, string? target = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Separator in the toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void ToolbarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Multi-select toggle group in toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active items.
    // defaultValue: Initial value for uncontrolled mode.
    // rovingFocus: Whether roving focus is enabled.
    // loop: When true, keyboard navigation wraps.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Single-select toggle group in toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // rovingFocus: Whether roving focus is enabled.
    // loop: When true, keyboard navigation wraps.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggle item in toolbar toggle group.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarToggleItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Represents the orientation for components like Tabs, Slider, etc.
  enum Orientation
    Horizontal
    Vertical
  // Overlay components (Dialog, AlertDialog, Popover, Tooltip, HoverCard, Toast). Each handles Portal/Overlay management automatically.
  static class OverlayExtensions
    // Style slots: overlayStyle → AlertDialog.Overlay, contentStyle → AlertDialog.Content, titleStyle → AlertDialog.Title, descriptionStyle → AlertDialog.Description, footerStyle → AlertDialog.Footer, cancelStyle → AlertDialog.Cancel, actionStyle → AlertDialog.Action.
    // view: The UIView to render into.
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // title: Title text for the alert dialog.
    // description: Description text for the alert dialog.
    // cancelLabel: Label for the cancel button. Defaults to "Cancel".
    // actionLabel: Label for the action button.
    // onAction: Callback invoked when the action button is clicked.
    // trigger: Builder function for the element that triggers the alert dialog.
    // contentSlot: Builder function for the alert dialog content. When provided, overrides title/description/action parameters for full custom control.
    // overlayStyle: Style classes for the background overlay. Use AlertDialog.Overlay for default styling.
    // overlayStyleId: CSS class name for the overlay.
    // contentStyle: Style classes for the dialog content container. Use AlertDialog.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // titleStyle: Style classes for the title. Use AlertDialog.Title for default styling.
    // descriptionStyle: Style classes for the description. Use AlertDialog.Description for default styling.
    // footerStyle: Style classes for the footer container. Use AlertDialog.Footer for default styling.
    // cancelStyle: Style classes for the cancel button. Use AlertDialog.Cancel for default styling.
    // actionStyle: Style classes for the action button. Use AlertDialog.Action for default styling.
    // rootStyle: Style for the outermost AlertDialog container. Rarely needed; prefer styling the overlay and content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void AlertDialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? title = null, string? description = null, string? cancelLabel = null, string? actionLabel = null, Func<Task>? onAction = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? cancelStyle = null, string[]? actionStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Style slots: overlayStyle → Dialog.Overlay, contentStyle → Dialog.Content.
    // view: The UIView to render into.
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // modal: When true, prevents interaction with elements behind the dialog.
    // trigger: Builder function for the element that triggers the dialog.
    // contentSlot: Builder function for the dialog content.
    // content: Builder function for rendering child elements within this component.
    // overlayStyle: Style classes for the background overlay. Use Dialog.Overlay for default styling.
    // overlayStyleId: CSS class name for the overlay.
    // contentStyle: Style classes for the dialog content container. Use Dialog.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // rootStyle: Style for the outermost Dialog container. Rarely needed; prefer styling the overlay and content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void Dialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? headerStyle = null)
    // Style slots: contentStyle → HoverCard.Content.
    // view: The UIView to render into.
    // style: Alias for contentStyle — on a hover card the surface the ergonomic first-positional styles is the floating content panel. Ignored when contentStyle is also provided (contentStyle wins).
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // openDelay: Delay in milliseconds before showing the card.
    // closeDelay: Delay in milliseconds before hiding the card after mouse leaves.
    // trigger: Builder function for the element that triggers the hover card.
    // contentSlot: Builder function for the hover card content.
    // content: Builder function for rendering child elements within this component.
    // contentStyle: Style classes for the hover card content. Use HoverCard.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // rootStyle: Style for the HoverCard container. Rarely needed; prefer styling the content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void HoverCard(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: contentStyle → Popover.Content.
    // view: The UIView to render into.
    // style: Alias for contentStyle — on a popover the surface the ergonomic first-positional styles is the floating content panel. Ignored when contentStyle is also provided (contentStyle wins).
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // modal: When true, prevents interaction with elements outside the popover.
    // side: Side of the trigger the content is rendered against.
    // align: Alignment of the content relative to the trigger along the chosen side.
    // sideOffset: Distance in pixels between the trigger and the content along side.
    // alignOffset: Offset in pixels of the content from the aligned edge.
    // trigger: Builder function for the element that triggers the popover.
    // contentSlot: Builder function for the popover content.
    // content: Builder function for rendering child elements within this component.
    // contentStyle: Style classes for the popover content container. Use Popover.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // rootStyle: Style for the outermost Popover container. Rarely needed; prefer styling the content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void Popover(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: toastStyle → Toast.Default, viewportStyle → Toast.Viewport, titleStyle → Toast.Title, descriptionStyle → Toast.Description, closeStyle → Toast.Close.
    // view: The UIView to render into.
    // type: Toast type affecting layering behavior (Foreground or Background).
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // durationMs: Duration in milliseconds before auto-dismiss.
    // forceMount: When true, keeps the toast in the DOM even when closed.
    // swipeDirection: Direction to swipe to dismiss the toast.
    // swipeThreshold: Distance in pixels required to trigger a swipe dismiss.
    // title: Title text for the toast.
    // description: Description text for the toast.
    // showClose: Whether to show the close button. Defaults to true when using simplified API.
    // closeLabel: Label for the close button. Defaults to "×".
    // content: Builder function for rendering child elements within this component. When provided, overrides title/description/close parameters for full custom control.
    // toastStyle: Style classes for the toast container. Use Toast.Default for default styling.
    // viewportStyle: Style classes for the viewport where toasts are rendered. Use Toast.Viewport for default styling.
    // titleStyle: Style classes for the title. Use Toast.Title for default styling.
    // descriptionStyle: Style classes for the description. Use Toast.Description for default styling.
    // closeStyle: Style classes for the close button. Use Toast.Close for default styling.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    // onEscapeKeyDown: Invoked when the escape key is pressed.
    // onPause: Invoked when the toast timer pauses (e.g., on hover).
    // onResume: Invoked when the toast timer resumes.
    // onSwipeStart: Invoked when a swipe gesture starts.
    // onSwipeMove: Invoked during a swipe gesture.
    // onSwipeEnd: Invoked when a swipe gesture completes.
    // onSwipeCancel: Invoked when a swipe gesture is cancelled.
    static void Toast(this UIView view, string[]? style = null, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string? title = null, string? description = null, bool? showClose = null, string? closeLabel = null, Action<UIView>? content = null, string[]? toastStyle = null, string[]? viewportStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Func<Task>? onEscapeKeyDown = null, Func<Task>? onPause = null, Func<Task>? onResume = null, Func<ToastSwipeArgs, Task>? onSwipeStart = null, Func<ToastSwipeArgs, Task>? onSwipeMove = null, Func<ToastSwipeArgs, Task>? onSwipeEnd = null, Func<ToastSwipeArgs, Task>? onSwipeCancel = null)
    // Style slots: contentStyle → Tooltip.Content.
    // view: The UIView to render into.
    // style: Alias for contentStyle — on a tooltip the surface the ergonomic first-positional styles is the floating content bubble. Ignored when contentStyle is also provided (contentStyle wins).
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // delayDuration: Delay in milliseconds before showing the tooltip.
    // skipDelayDuration: Delay in milliseconds when switching between tooltips.
    // disableHoverableContent: When true, prevents hoverable content from keeping the tooltip open.
    // trigger: Builder function for the element that triggers the tooltip.
    // contentSlot: Builder function for the tooltip content.
    // content: Builder function for rendering child elements within this component.
    // contentStyle: Style classes for the tooltip content. Use Tooltip.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // rootStyle: Style for the Tooltip container. Rarely needed; prefer styling the content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void Tooltip(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
  // Composite overlay-menu components built on the Popover/Dialog primitives and the Menu/Combobox/ Command theme tokens — the shadcn Combobox, DropdownMenu, Command-palette, and Kbd, expressed as C# composites (no bespoke node type). Filtering is server-side over the app's reactive search state, matching Parallax's reactive model; client-side typeahead/roving-focus is a later renderer concern, not required for the components to work.
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
  // One page of items plus the controls needed to render prev/next buttons. Returned by PaginationExtensions.Paginate<T>.
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
    // True if there is a next page.
    bool CanNext { get; init; }
    // True if there is a previous page.
    bool CanPrev { get; init; }
    // Action that jumps to page 0.
    Func<Task> First { get; init; }
    // Zero-based current page index.
    int Index { get; init; }
    // The slice of Source for the current page.
    IReadOnlyList<T> Items { get; init; }
    // Action that moves to a specific page (0-based). Clamps to valid range.
    Func<int, Task> JumpTo { get; init; }
    // Action that jumps to the last page.
    Func<Task> Last { get; init; }
    // Action to bind to a Next button's onClick. Increments page; no-op at last.
    Func<Task> Next { get; init; }
    // Items per page (the configured page size, not necessarily Items.Count).
    int PageSize { get; init; }
    // Action to bind to a Prev button's onClick. Decrements page; no-op at first.
    Func<Task> Prev { get; init; }
    // The full input list, if the caller wants the original.
    IReadOnlyList<T> Source { get; init; }
    // Total number of pages (always >= 1, even when Source is empty).
    int TotalPages { get; init; }
  // Bounded-cursor primitive on top of ClientReactive<T>. Slices an in-memory list, returns the slice + bound actions (Prev/Next/JumpTo/First/Last) the caller binds to whatever UI fits. Holds zero rendering opinion — no tab bars, no default control rows, no opinionated layout. Most Ikon apps don't need pagination at all (live feeds, autoscroll, virtualization handle the common cases via ReactiveList<T> + ScrollArea(autoScroll: true)). Use this when you have a static list large enough to warrant explicit page navigation. For DB-backed pagination (load only the current page from a backend), drive ClientReactive<T> directly and observe its value in your data-loading code — same per-client semantics, no special helper needed.
  static class PaginationExtensions
    // page must be a field-level ClientReactive<T>; each client sees its own page, and the returned slice is a snapshot read once, not a live view.
    // view: UIView (extension receiver — unused, present for fluency).
    // items: Source list. Read once; the slice is a snapshot, not a live view.
    // page: Per-client page index. Use a field-level ClientReactive<T> initialized to 0.
    // pageSize: Items per page (must be >= 1; clamped if not).
    static Page<T> Paginate<T>(this UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  // Options for the Contact Picker API action.
  sealed record PickContactsActionOptions : ActionOptions
    ctor()
    // When true, allows selecting multiple contacts.
    bool Multiple { get; init; }
  // Event args for pointer down outside events on overlays.
  sealed record PointerDownOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Extension methods for QR code generation.
  static class QrCodeExtensions
    // QR code image. Generates a QR code server-side and renders it as an image.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // value: The text or URL to encode in the QR code.
    // size: Size of the QR code in pixels (default 256).
    // key: Unique identifier to assist stable diffing across renders.
    static void QR(this UIView view, string[]? style = null, string? value = null, int size = 256, string? key = null)
  // Extension methods for the RichTextEditor component.
  static class RichTextEditorExtensions
    // Values are HTML strings. A controlled value with no write-back handler (onValueChange or onSubmit) renders the editor read-only.
    // view: The UIView to render into.
    // value: Controlled HTML value. A controlled value with no write-back handler (no onValueChange, no onSubmit) renders the editor read-only, since edits would have nowhere to go.
    // defaultValue: Initial HTML value for uncontrolled mode.
    // placeholder: Placeholder text shown when empty.
    // disabled: When true, prevents user interaction.
    // tools: Explicit toolbar contents. When null, a sensible default toolbar is shown.
    // showToolbar: When false, hides the toolbar entirely.
    // minRows: Minimum number of text rows.
    // maxRows: Maximum number of text rows before scrolling.
    // style: Style for the outermost container. Use RichTextEditor.Root.
    // toolbarStyle: Style for the toolbar. Use RichTextEditor.Toolbar.
    // toolbarButtonStyle: Style for toolbar buttons. Use RichTextEditor.ToolbarButton.
    // contentStyle: Style for the editable content area. Use RichTextEditor.Content.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the HTML value changes.
    // onSubmit: Invoked when the user presses Ctrl+Enter.
    static void RichTextEditor(this UIView view, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, IReadOnlyList<RichTextTool>? tools = null, bool? showToolbar = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? toolbarStyle = null, string[]? toolbarButtonStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  // Formatting action available in the RichTextEditorExtensions.RichTextEditor toolbar.
  enum RichTextTool
    // Bold toggle.
    Bold
    // Italic toggle.
    Italic
    // Underline toggle.
    Underline
    // Strikethrough toggle.
    Strikethrough
    // Convert block to H1.
    Heading1
    // Convert block to H2.
    Heading2
    // Convert block to H3.
    Heading3
    // Convert block to paragraph.
    Paragraph
    // Align text left.
    AlignLeft
    // Align text center.
    AlignCenter
    // Align text right.
    AlignRight
    // Bullet list.
    BulletList
    // Numbered list.
    NumberedList
    // Block quote.
    Blockquote
    // Inline or block code.
    Code
    // Insert link.
    Link
    // Clear inline formatting.
    ClearFormatting
    // Undo.
    Undo
    // Redo.
    Redo
  // Tiny primitives for using ClientReactive<T> as a signal the app reads to decide what to render. Routes, tabs, modes, panel selections, "which dialog is open" — same shape, same primitives. Intentionally minimal: no opinionated tab bars, no URL coupling, no rendering bias. The signal is the building block; the app decides how to consume it. For URL ↔ signal sync (browser bar, deep links, back/forward), use Navigation on the host app — keeps URL concerns in one place instead of forking them through this layer.
  static class RoutingExtensions
    // Renders the content for the currently-active key. signal holds the active key (per-client); cases maps each known key to a render lambda. Falls back to fallback (or empty) when the active key isn't in the dictionary. private ClientReactive<string> _route = new("home"); ... view.Routed(_route, new() { ["home"] = v => RenderHome(v), ["about"] = v => RenderAbout(v), ["settings"] = v => RenderSettings(v), });
    static void Routed<T>(this UIView view, ClientReactive<T> signal, Dictionary<T, Action<UIView>> cases, Action<UIView>? fallback = null)
    // Returns an onClick-shaped handler that sets the signal to a constant value. Convenience for the very common "button that activates a specific route/tab/mode" case so the caller doesn't write a lambda at every call site. view.Button(text: "Open settings", onClick: view.Set(_route, "settings"));
    static Func<Task> Set<T>(this UIView view, ClientReactive<T> signal, T value)
  // Represents which scrollbars to show in a ScrollArea.
  enum ScrollAreaScrollbars
    None
    Vertical
    Horizontal
    Both
  // Represents the scrollbar visibility type for ScrollArea.
  enum ScrollAreaType
    Auto
    Always
    Scroll
    Hover
  // Extension methods for the ScrollColumn primitive — a header/body/footer dialog pattern where the body scrolls. Wraps a LayoutExtensions.ScrollArea with the correct flex sizing so scrolling engages without ceremony.
  static class ScrollColumnExtensions
    // Canonical shape for dialogs, side panels, and chat layouts needing fixed chrome around an overflowing region; avoids the flex-1 ScrollArea that won't shrink inside a flex parent (the min-height: auto quirk). The outer height is the caller's responsibility — set it via style (e.g. "h-[82vh]") or let a flex-1 parent provide bounds.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for the outer flex column. Provide a bounded height here (e.g. h-[82vh]).
    // header: Optional builder for the pinned top region.
    // footer: Optional builder for the pinned bottom region.
    // content: Builder for the scrollable body region.
    // scrollbars: Which scrollbars to display inside the body (default ScrollAreaScrollbars.Vertical).
    // scrollType: Scrollbar visibility behavior.
    // autoScroll: Auto-scroll the body to bottom when content changes — ideal for chat.
    // autoScrollKey: Anything whose value changes when the content changes — pass the reactive collection itself, a count, or a composite string (see LayoutExtensions.ScrollArea).
    // bodyStyle: Extra utility classes applied to the ScrollArea root (rarely needed).
    // viewportStyle: Extra utility classes applied to the ScrollArea viewport.
    // scrollbarStyle: Extra utility classes applied to the ScrollArea scrollbar.
    // thumbStyle: Extra utility classes applied to the ScrollArea thumb.
    // styleId: CSS class name to apply directly to the outer column. For exceptional cases.
    // key: Unique identifier to assist stable diffing across renders.
    static void ScrollColumn(this UIView view, string[]? style = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType scrollType = Hover, bool autoScroll = false, object? autoScrollKey = null, string[]? bodyStyle = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string? styleId = null, string? key = null)
  // Direction for infinite scroll loading.
  enum ScrollDirection
    Down
    Up
  // Event args for when user scrolls near the end of content.
  sealed record ScrollNearEndArgs
    ctor(double ScrollTop, double ScrollHeight, double ClientHeight, ScrollDirection Direction)
    double ClientHeight { get; init; }
    ScrollDirection Direction { get; init; }
    double ScrollHeight { get; init; }
    double ScrollTop { get; init; }
  // Extension methods for Select components.
  static class SelectExtensions
    // An Input.* token passed as the Select's own style is ignored (with a dev warning) — it would style the outer wrapper, not the field element; the trigger already carries the field theme, so customize it through triggerStyle. Trigger sizing uses Select.Size tokens ([Select.Size.Sm] / [Select.Size.Lg], default medium) in triggerStyle.
    // view: The UIView to render into.
    // options: A flat list of selectable options.
    // groups: Grouped selectable options with optional labels.
    // value: The controlled value of the selected option. A controlled value with no write-back handler (no bind, no onValueChange) renders the select read-only, since a change would have nowhere to go.
    // defaultValue: The default value when initially rendered. Use when not controlling the state.
    // placeholder: Text displayed when no value is selected.
    // disabled: When true, prevents user interaction with this component.
    // required: When true, indicates a selection must be made before the form can be submitted.
    // open: The controlled open state of the dropdown.
    // name: The name of the select for form submission.
    // triggerStyle: Additional styles for the trigger button. Use Select.Size.* for sizing.
    // contentStyle: Additional styles for the dropdown content panel.
    // itemStyle: Additional styles for each selectable item in the dropdown.
    // itemIndicatorStyle: Additional styles for the selected item indicator (checkmark).
    // indicatorIconName: The name of the icon to display for the selected item indicator.
    // rootStyle: Styles for the root Select container. Rarely needed; prefer triggerStyle for most customizations.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using style parameters.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    // label: Optional field label rendered above the select (same field ergonomics as TextField).
    // bind: Two-way binds the select to a Reactive<T> in one call — reads bind.Value for the selected value and writes it back on every selection. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Select(this UIView view, string[]? style = null, IReadOnlyList<SelectOption>? options = null, IReadOnlyList<SelectOptionGroup>? groups = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, bool? required = null, bool? open = null, string? name = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? itemStyle = null, string[]? itemIndicatorStyle = null, string? indicatorIconName = "check", string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null, Reactive<string>? bind = null, string? ariaLabel = null)
  // Represents a selectable option in a Select component.
  sealed record SelectOption
    ctor(string Value, string Label, bool Disabled = false)
    bool Disabled { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  // Represents a group of selectable options in a Select component.
  sealed record SelectOptionGroup
    ctor(string? Label, IReadOnlyList<SelectOption> Options)
    string? Label { get; init; }
    IReadOnlyList<SelectOption> Options { get; init; }
  // Tones resolve to the theme's semantic color tokens, so they render correctly in both light and dark mode.
  enum SemanticTone
    // Neutral grey — default, carries no signal.
    Neutral
    // Brand accent color.
    Brand
    // Positive / confirmation (green family).
    Success
    // Caution (amber family).
    Warning
    // Failure / destructive (red family).
    Error
    // Informational (blue family).
    Info
  // A typed uniform value to pass to a WebGL shader. Use the static factory methods to create instances.
  readonly struct ShaderUniform
    // The GLSL type name (e.g. "float", "vec2", "vec3").
    string Type { get; }
    // The uniform value.
    object Value { get; }
    // Creates a boolean uniform.
    static ShaderUniform Bool(bool value)
    // Creates a float uniform.
    static ShaderUniform Float(float value)
    // Creates an integer uniform.
    static ShaderUniform Int(int value)
    // Creates a vec2 uniform from two floats.
    static ShaderUniform Vec2(float x, float y)
    // Creates a vec3 uniform from three floats.
    static ShaderUniform Vec3(float x, float y, float z)
    // Creates a vec4 uniform from four floats.
    static ShaderUniform Vec4(float x, float y, float z, float w)
  // Extension methods for WebGL shader components.
  static class ShadertoyExtensions
    // Renders GLSL fragment shaders with Shadertoy-compatible uniforms. The shader code must define a mainImage function with signature: void mainImage(out vec4 color, in vec2 fragCoord) Built-in uniforms (automatically provided): • iResolution (vec3) - canvas width, height, and 1.0 • iTime (float) - elapsed time in seconds • iTimeDelta (float) - time since last frame • iFrame (int) - current frame number • iMouse (vec4) - mouse x, y, click x, click y (requires enableMouse=true) • iDate (vec4) - year, month, day, seconds of day Texture channels: Pass image URLs (data URIs or http(s)) via channels to bind them to the Shadertoy channel uniforms, matching Shadertoy's default sampler behavior so shaders copied from shadertoy.com that sample 2D textures render the same way: • iChannel0..iChannel3 (sampler2D) - channel textures, in array order • iChannelResolution[4] (vec3) - per-channel pixel size (0 until loaded) • iChannelTime[4] (float) - always 0 for static images Textures use Shadertoy's defaults: vertical flip on (upright with uv = fragCoord/iResolution), repeat wrap, and mipmap filtering. Sample with texture(iChannel0, uv). Limitations: 2D image channels only - no cubemap (samplerCube), buffer, audio, or video channels; single output only.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // shaderSource: GLSL fragment shader source code.
    // fps: Target frames per second for shader rendering.
    // uniforms: Custom uniform values to pass to the shader.
    // channels: Image URLs (data URIs or http(s)) bound to iChannel0..3 in order. Up to four.
    // enableMouse: When true, passes mouse position as a uniform.
    // width: Width of the shader canvas in pixels.
    // height: Height of the shader canvas in pixels.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void ShadertoyCanvas(this UIView view, string[]? style = null, string? shaderSource = null, int? fps = null, IReadOnlyDictionary<string, ShaderUniform>? uniforms = null, IReadOnlyList<string>? channels = null, bool? enableMouse = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  // Options for the Web Share API action.
  sealed record ShareActionOptions : ActionOptions
    ctor()
    // Text body for the shared content.
    string? Text { get; init; }
    // Title for the shared content.
    string? Title { get; init; }
    // URL to share.
    string? Url { get; init; }
  // Slide-over panel composites (Sheet, Drawer) built on the Dialog primitive. The dialog's portal + content styling is repositioned per side via the Theming.Sheet / Theming.Drawer token recipes, including Crosswind slide-in/out motion classes driven by the panel's data-state attribute.
  static class SheetExtensions
    // Same open/close model as Sheet: in controlled mode (open set) pass onOpenChange and flip your state to false there, or the drawer cannot be dismissed.
    // view: The UIView to render into.
    // open: Controlled open state.
    // onOpenChange: Invoked when the open state changes (true when opening, false when closing).
    // title: Title rendered in the drawer header.
    // description: Muted description rendered under the title.
    // trigger: Builder for the element that opens the drawer (uncontrolled mode).
    // content: Builder for the drawer body.
    // footer: Builder for the footer (actions column).
    // defaultOpen: Initial open state for uncontrolled mode.
    // modal: When true, prevents interaction with elements behind the drawer.
    // showHandle: Whether to render the drag handle bar at the top of the panel.
    // style: Crosswind/Tailwind utility classes merged on top of Theming.Drawer.Content.
    // overlayStyle: Style for the background overlay. Defaults to Theming.Drawer.Overlay.
    // handleStyle: Style for the drag handle. Defaults to Theming.Drawer.Handle.
    // headerStyle: Style for the header container. Defaults to Theming.Drawer.Header.
    // titleStyle: Style for the title text. Defaults to Theming.Drawer.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.Drawer.Description.
    // footerStyle: Style for the footer container. Defaults to Theming.Drawer.Footer.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void Drawer(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showHandle = true, string[]? style = null, string[]? overlayStyle = null, string[]? handleStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string? key = null)
    // In controlled mode (open set) pass onOpenChange and flip your state to false there, or the close button and outside clicks cannot dismiss the sheet. Caller styles replace the themed panel token, or merge over it with a leading "default" marker.
    // view: The UIView to render into.
    // open: Controlled open state.
    // onOpenChange: Invoked when the open state changes (true when opening, false when closing).
    // side: Screen edge the panel is anchored to and slides in from.
    // title: Title rendered in the sheet header.
    // description: Muted description rendered under the title.
    // trigger: Builder for the element that opens the sheet (uncontrolled mode).
    // content: Builder for the sheet body.
    // footer: Builder for the footer (actions row).
    // defaultOpen: Initial open state for uncontrolled mode.
    // modal: When true, prevents interaction with elements behind the sheet.
    // showClose: Whether to render the × close button in the top-right corner.
    // style: Crosswind/Tailwind utility classes merged on top of the themed panel token.
    // overlayStyle: Style for the background overlay. Defaults to Theming.Sheet.Overlay.
    // headerStyle: Style for the header container. Defaults to Theming.Sheet.Header.
    // titleStyle: Style for the title text. Defaults to Theming.Sheet.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.Sheet.Description.
    // footerStyle: Style for the footer container. Defaults to Theming.Sheet.Footer.
    // closeStyle: Style for the close button. Defaults to Theming.Sheet.CloseButton.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void Sheet(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, Side side = Right, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showClose = true, string[]? style = null, string[]? overlayStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? closeStyle = null, string? key = null)
  // Represents the side for positioning overlays.
  enum Side
    Top
    Right
    Bottom
    Left
  // Extension methods for the Skeleton component.
  static class SkeletonExtensions
    // Pulsing placeholder block for loading / not-yet-available content — the visual stand-in used while real content is pending, and the default fill for content redacted from the build-time boot snapshot (see SnapshotReveal). A typed convenience over the Skeleton.* theme tokens (a div with animate-pulse styling); size and shape via size / shape, or override freely through style.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes appended to the base skeleton styling (e.g. an explicit width).
    // shape: Outline shape — Rectangle (default), Circle, or Square.
    // size: Height preset — Xs, Sm, Md (default), Lg, or Xl.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Skeleton(this UIView view, string[]? style = null, SkeletonShape shape = Rectangle, SkeletonSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Outline shape of a SkeletonExtensions.Skeleton placeholder.
  enum SkeletonShape
    Rectangle
    Circle
    Square
  // Height preset for a SkeletonExtensions.Skeleton placeholder.
  enum SkeletonSize
    Xs
    Sm
    Md
    Lg
    Xl
  // Wrappers for controlling how the UI renders into the build-time boot snapshot. The boot snapshot is a public asset painted to everyone before the live connection, so by default the snapshot render replaces every content leaf with a skeleton — per-user content can never leak. These wrappers let the app override that default for specific regions, branching on UIView.IsSnapshot at build time so it keeps a single UI.Root definition instead of two separate UIs. On the normal live render path every wrapper is a single bool check plus the content the developer already wrote.
  static class SnapshotExtensions
    // Renders content live but omits it entirely from the boot snapshot — not even a skeleton placeholder.
    static void SnapshotHide(this UIView view, Action<UIView> content)
    // Renders content only in the boot snapshot, never live; the filler is rendered as authored (not auto-skeletonized).
    static void SnapshotOnly(this UIView view, Action<UIView> content)
    // Renders content as real content in the boot snapshot instead of skeletons — use only for content safe to bake into the public snapshot (logos, static chrome, marketing copy). The opt-out covers the whole subtree.
    static void SnapshotReveal(this UIView view, Action<UIView> content)
  // Represents sort strategy for @dnd-kit SortableContext.
  enum SortStrategy
    VerticalList
    HorizontalList
  // Contains information about a reorder operation in SortableList.
  sealed record SortableReorderArgs
    ctor(string ActiveId, string OverId, int OldIndex, int NewIndex, IReadOnlyList<string> NewOrder)
    string ActiveId { get; init; }
    int NewIndex { get; init; }
    IReadOnlyList<string> NewOrder { get; init; }
    int OldIndex { get; init; }
    string OverId { get; init; }
  // Size of the loading Spinner.
  enum SpinnerSize
    Sm
    Md
    Lg
  // Trend direction for a CardExtensions.StatCard delta.
  enum StatTrend
    // No direction — the delta renders in a neutral tone without an arrow.
    Flat
    // Upward trend — trending-up arrow in the success tone.
    Up
    // Downward trend — trending-down arrow in the error tone.
    Down
  // Represents sticky behavior for Select/DropdownMenu.
  enum Sticky
    Partial
    Always
  // Defines a tab for use with the Tabs component.
  record TabItem
    // Value: Unique identifier for the tab.
    // Label: Text label displayed on the tab trigger.
    // Content: Builder function for rendering the tab's content panel.
    // Disabled: When true, prevents user interaction with this tab.
    // ForceMount: When true, the tab's content is mounted in the DOM even when inactive (Radix hides via data-state="inactive"). Use this for heavy panels you want to amortise into initial paint and keep mounted across tab switches; the trade-off is a slower first render and any mount-time effects firing on hidden panels.
    ctor(string Value, string Label, Action<UIView> Content, bool Disabled = false, bool ForceMount = false)
    // Builder function for rendering the tab's content panel.
    Action<UIView> Content { get; init; }
    // When true, prevents user interaction with this tab.
    bool Disabled { get; init; }
    // When true, the tab's content is mounted in the DOM even when inactive (Radix hides via data-state="inactive"). Use this for heavy panels you want to amortise into initial paint and keep mounted across tab switches; the trade-off is a slower first render and any mount-time effects firing on hidden panels.
    bool ForceMount { get; init; }
    // Text label displayed on the tab trigger.
    string Label { get; init; }
    // Unique identifier for the tab.
    string Value { get; init; }
  // Lightweight semantic table composites — the styled middle ground between hand-rolled Grid/Row layouts and the payload-driven DataTable component. Uses CSS table display utilities, so columns align automatically without a shared grid template:
  // view.Table(content: t =>
  // {
  //     t.TableHeader(content: h => h.TableRow(content: r =>
  //     {
  //         r.TableHead("Name");
  //         r.TableHead("Status");
  //     }));
  //     t.TableBody(content: b =>
  //     {
  //         foreach (var user in users)
  //         {
  //             b.TableRow(key: user.Id, striped: true, content: r =>
  //             {
  //                 r.TableCell(user.Name);
  //                 r.TableCell(content: c => c.Badge(user.Status, SemanticTone.Success));
  //             });
  //         }
  //     });
  // });
  static class TableExtensions
    // Table container (CSS display: table). Compose with TableHeader, TableBody, TableRow, TableHead, and TableCell. Caller styles replace the base token; lead the array with "default" to merge over it.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the table base token.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for the table's header/body groups.
    static void Table(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Table — positional (style, children) overload.
    static void Table(this UIView view, string[]? style, Action<UIView> children)
    // Body row group (CSS display: table-row-group).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the row group token.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for the body rows.
    static void TableBody(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Data cell (CSS display: table-cell).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the cell token.
    // text: Cell text. For custom content use content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for custom cell content.
    static void TableCell(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // TableCell — positional-text-first overload: r.TableCell(user.Name).
    static void TableCell(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // Header cell (CSS display: table-cell) with muted uppercase column-label styling.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the header cell token.
    // text: Column label. For custom content use content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for custom cell content.
    static void TableHead(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // TableHead — positional-text-first overload: r.TableHead("Name").
    static void TableHead(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // Header row group (CSS display: table-header-group). Put one TableRow of TableHead cells inside.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the header group token.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for the header rows.
    static void TableHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Table row (CSS display: table-row) with a bottom border. Rows with onClick also get hover highlight + pointer cursor.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the row token.
    // striped: When true, even rows get a subtle background (zebra striping via CSS :nth-child).
    // onClick: Invoked when the user clicks the row. Accepts sync (() => …) and async (async () => …) lambdas alike.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for the row's cells.
    static void TableRow(this UIView view, string[]? style = null, bool striped = false, Delegate? onClick = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
  // Extension methods for Tabs components.
  static class TabsExtensions
    // Style slots (default theme tokens): listStyle → Tabs.List, triggerStyle → Tabs.Trigger, contentStyle → Tabs.Content; rootStyle is the outer container (rarely needed).
    // view: The UIView to render into.
    // value: Controlled value identifying the active tab/item.
    // defaultValue: Initial value for uncontrolled mode.
    // orientation: Layout orientation (horizontal or vertical).
    // activationMode: How tabs are activated: Automatic (on focus) or Manual (on click/enter).
    // tabs: Collection of tab definitions that defines all tabs.
    // listContainerStyle: Style for an optional Box wrapper around the TabsList. When provided, wraps the list in a styled container.
    // listStyle: Style for the TabsList container. Use Tabs.List for default styling.
    // triggerStyle: Default style for enabled tab triggers. Use Tabs.Trigger for default styling.
    // disabledTriggerStyle: Style for disabled tab triggers. If not provided, uses triggerStyle for all triggers.
    // contentContainerStyle: Style for an optional Box wrapper around all TabsContent panels. When provided, wraps content in a styled container.
    // contentStyle: Default style for all content panels. Use Tabs.Content for default styling.
    // rootStyle: Style for the outermost Tabs container. Rarely needed; prefer styling the list and triggers instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // lazyPanels: When true (controlled tabs only), the server builds only the active tab's panel plus any TabItem.ForceMount panels; a tab switch then fetches the new panel in the same round-trip that confirms the switch, and the client keeps the old panel visible until the new one arrives (no flicker). Opt in for apps with many or heavy tabs to cut per-client server memory and wire size by roughly the tab count — the trade-off is one network round-trip of latency per switch instead of an instant client-side swap. Default false: every panel ships and switching is instant. Ignored for uncontrolled tabs, which switch client-side and therefore always need every panel.
    static void Tabs(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, ActivationMode activationMode = Automatic, IEnumerable<TabItem>? tabs = null, string[]? listContainerStyle = null, string[]? listStyle = null, string[]? triggerStyle = null, string[]? disabledTriggerStyle = null, string[]? contentContainerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, bool lazyPanels = false)
  // Smallest time unit shown by a TimePickerExtensions.TimePicker.
  enum TimeGranularity
    // Hours only.
    Hour
    // Hours and minutes.
    Minute
    // Hours, minutes, and seconds.
    Second
  // Extension methods for TimePicker components.
  static class TimePickerExtensions
    // Values are ISO-8601 HH:mm or HH:mm:ss strings; the emitted value is always 24-hour regardless of hourFormat. A controlled value without onValueChange renders read-only.
    // view: The UIView to render into.
    // value: Controlled value (HH:mm or HH:mm:ss).
    // defaultValue: Initial value for uncontrolled mode.
    // hourFormat: 12h or 24h display. Emitted value is always 24h.
    // granularity: Smallest unit shown.
    // minuteStep: Minute step (1, 5, 10, 15, 30…). Defaults to 1.
    // secondStep: Second step. Defaults to 1.
    // disabled: When true, prevents user interaction.
    // open: Controlled open state of the popover.
    // defaultOpen: Initial open state for uncontrolled mode.
    // side: Side the popover content renders against.
    // align: Alignment of the content along the chosen side.
    // placeholder: Text shown in the trigger when no time is selected.
    // triggerStyle: Style for the trigger button. Use TimePicker.Trigger.
    // contentStyle: Style for the popover content container. Use TimePicker.Content.
    // columnStyle: Style for each hour/minute/second column.
    // itemStyle: Style for a single time option.
    // itemSelectedStyle: Style for the selected time option.
    // rootStyle: Style for the outermost container. Rarely needed.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the selected time changes.
    // onOpenChange: Invoked when the popover open state changes.
    static void TimePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, HourFormat hourFormat = Hour24, TimeGranularity granularity = Minute, int? minuteStep = null, int? secondStep = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string? placeholder = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? columnStyle = null, string[]? itemStyle = null, string[]? itemSelectedStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // One notification held by a Toasts queue.
  sealed record ToastItem
    // Id: Queue-unique identifier used to dismiss the toast.
    // Title: Headline text.
    // Description: Optional muted body text.
    // Tone: Semantic tone controlling the icon and its color.
    // DurationMs: Milliseconds before the client auto-dismisses the toast.
    ctor(long Id, string Title, string? Description, SemanticTone Tone, int DurationMs)
    // Optional muted body text.
    string? Description { get; init; }
    // Milliseconds before the client auto-dismisses the toast.
    int DurationMs { get; init; }
    // Queue-unique identifier used to dismiss the toast.
    long Id { get; init; }
    // Headline text.
    string Title { get; init; }
    // Semantic tone controlling the icon and its color.
    SemanticTone Tone { get; init; }
  // Event args for toast swipe events.
  sealed record ToastSwipeArgs
    ctor(ToastSwipeDirection Direction, double DeltaX, double DeltaY)
    double DeltaX { get; init; }
    double DeltaY { get; init; }
    ToastSwipeDirection Direction { get; init; }
  // Represents swipe direction for Toast.
  enum ToastSwipeDirection
    Left
    Right
    Up
    Down
  // Represents the type of Toast (foreground/background).
  enum ToastType
    Foreground
    Background
  // Wiring: construct one instance as an app field, mount ToastsExtensions.ToastHost once in the root UI, then fire notifications (e.g. _toasts.Success(...)) from any handler. State lives in a ClientReactive<T>, so methods must be called where a client scope is active (UI render or event handlers) and each client sees only its own toasts. Auto-dismiss is client-driven off ToastItem.DurationMs.
  sealed class Toasts
    ctor()
    // Toasts currently visible for the calling client (reactive read).
    IReadOnlyList<ToastItem> Items { get; }
    // Remove all toasts from the calling client's queue.
    void Clear()
    // Remove one toast from the calling client's queue.
    void Dismiss(long id)
    // Enqueue an error toast.
    long Error(string title, string? description = null, int durationMs = 5000)
    // Enqueue an info toast.
    long Info(string title, string? description = null, int durationMs = 5000)
    // Enqueue a toast for the calling client.
    // title: Headline text.
    // description: Optional muted body text.
    // tone: Semantic tone controlling the icon and its color.
    // durationMs: Milliseconds before the client auto-dismisses the toast.
    long Show(string title, string? description = null, SemanticTone tone = Neutral, int durationMs = 5000)
    // Enqueue a success toast.
    long Success(string title, string? description = null, int durationMs = 5000)
    // Enqueue a warning toast.
    long Warning(string title, string? description = null, int durationMs = 5000)
    // Default auto-dismiss duration in milliseconds.
    const int DefaultDurationMs = 5000
  // Host composite that renders a Toasts queue with the toast primitives.
  static class ToastsExtensions
    // Render the toast viewport for a Toasts queue. Mount exactly once in the root UI; every queued toast renders as a themed toast (tone icon, title, description, close button) that the client auto-dismisses after its duration. Both auto-dismiss and the close button report back and remove the item from the queue.
    // view: The UIView to render into.
    // toasts: The queue to render.
    // viewportStyle: Style for the toast viewport. Defaults to Theming.Toast.Viewport.
    // toastStyle: Crosswind/Tailwind utility classes merged on top of Theming.Toast.Default for each toast.
    // titleStyle: Style for the title. Defaults to Theming.Toast.Title.
    // descriptionStyle: Style for the description. Defaults to Theming.Toast.Description.
    // closeStyle: Style for the close button. Defaults to Theming.Toast.Close.
    // showClose: Whether to render the × close button on each toast.
    static void ToastHost(this UIView view, Toasts toasts, string[]? viewportStyle = null, string[]? toastStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, bool showClose = true)
  // Recursive tree composite over the Collapsible primitive, styled with the NavPanel/NavItem token recipes.
  static class TreeViewExtensions
    // Expansion state lives in a caller-held ExpandedSet — declare it as an app field (private readonly ExpandedSet _expanded = new();). Clicking a branch toggles its expansion and selects it in the same click.
    // view: The UIView to render into.
    // roots: Top-level nodes.
    // id: Stable unique id per node — used for diff keys, expansion, and selection.
    // label: Visible text per node.
    // children: Child nodes per node; null or empty marks a leaf.
    // expanded: Per-client expansion state. Declare as an app field: private readonly ExpandedSet _expanded = new();
    // style: Crosswind/Tailwind utility classes merged on top of Theming.NavPanel.Ghost for the tree container.
    // onSelect: Invoked when a row is clicked (branches toggle and select on the same click).
    // selectedId: Id of the currently selected node, rendered with the active item style.
    // icon: Optional per-node Lucide icon name rendered before the label.
    // itemStyle: Style for rows. Defaults to Theming.NavItem.Md + Theming.NavItem.Default.
    // selectedItemStyle: Style for the selected row. Defaults to Theming.NavItem.Md + Theming.NavItem.Active.
    // labelStyle: Style for row labels. Defaults to Theming.NavItem.Label.
    // childrenStyle: Style for the nested children container (indent + guide line).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void TreeView<T>(this UIView view, IReadOnlyList<T> roots, Func<T, string> id, Func<T, string> label, Func<T, IReadOnlyList<T>?> children, ExpandedSet expanded, string[]? style = null, Func<T, Task>? onSelect = null, string? selectedId = null, Func<T, string?>? icon = null, string[]? itemStyle = null, string[]? selectedItemStyle = null, string[]? labelStyle = null, string[]? childrenStyle = null, string? styleId = null, string? key = null)
  // Performance model: the server emits one wrapper node per item up to itemCount and runs every per-item content builder eagerly server-side (keep content trees inexpensive); the client mounts only the wrappers inside [start - overscan, end + overscan] and leaves the rest out of the DOM entirely. onNearEnd fires when the window enters the last nearEndThreshold rows — append items to grow the list.
  static class VirtualListExtensions
    // DOM-virtualized scrollable grid. Items are laid out in a fixed number of columns and rows outside the visible window are not mounted in the DOM.
    // view: The UIView to render into.
    // itemCount: Total number of items in the grid.
    // columns: Fixed number of columns. Ignored when minItemWidthPx is set.
    // rowHeight: Fixed height in pixels for every row. Ignored when aspectRatio is set.
    // onRenderItem: Callback invoked per item with its zero-based index. Builds the cell content.
    // overscan: Extra rows to render before/after the visible window. Default 2.
    // gap: Pixels of gap between rows and between columns. Default 12.
    // minItemWidthPx: When set, column count is computed from container width / minItemWidthPx, overriding columns. Use for responsive layouts.
    // maxColumns: Upper bound on auto-computed columns (only relevant with minItemWidthPx).
    // aspectRatio: Ratio of row height to column width (rowHeight = columnWidth × aspectRatio). Use for cells that should scale proportionally with column width across screen sizes (e.g. 1.0 = square, 0.75 = 4:3 landscape, 1.4 = portrait card). Overrides rowHeight when set.
    // resetScrollKey: Optional opaque token that resets the scroll position to the top whenever it changes. Use to reset scroll on filter/sort changes without remounting the grid (which would churn all child actions).
    // onNearEnd: Fires when the user scrolls within nearEndThresholdRows rows of the end.
    // nearEndThresholdRows: Distance from end (in rows) to trigger onNearEnd. Default 2.
    // style: Style for the outermost scrollable viewport container.
    // itemStyle: Style applied to each cell wrapper.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    // props: Additional properties passed directly to the underlying component.
    static void VirtualGrid(this UIView view, int itemCount, int columns, double rowHeight, Action<UIView, int> onRenderItem, int overscan = 2, int gap = 12, int? minItemWidthPx = null, int? maxColumns = null, double? aspectRatio = null, string? resetScrollKey = null, Func<int, Task>? onNearEnd = null, int nearEndThresholdRows = 2, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // DOM-virtualized vertical list with fixed item height. Renders only items inside the visible window plus an overscan buffer.
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
  // Day of the week used as the first column in the calendar grid.
  enum WeekStart
    // Week starts on Sunday.
    Sunday
    // Week starts on Monday (ISO-8601).
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
  // The animated level bars of view.AudioWave — the "you are being heard" cue that replaces an input field, or sits beside a mic, while audio is captured.
  static class AudioWave
    const string Bar
    // Bar heights in spacing units, cycled across however many bars are asked for. Uneven on purpose: an even ramp reads as a loading spinner rather than a level meter.
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
  // Combobox (searchable Select): a Popover whose trigger shows the current value and whose content is a search field over a filtered option list. Slot tokens for the whole surface; the trigger deliberately reuses the outline Button look so a Combobox and a Select read the same in a form.
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
  // Command palette (the shadcn Command in a dialog): a centred search field over a grouped, filtered action list. Slot tokens for the surface, groups, and rows.
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
  static class FormField
    const string ErrorText
    const string HelpText
    const string Label
    const string LabelRequired
    const string ParamRow
    const string Root
    const string SuccessText
    const string WarningText
  static class HoverCard
    const string Content
    const string Default
  // Defines a UI theme providing base CSS and a default icon library.
  interface ITheme
    // Global CSS injected into the client as the theme baseline.
    string Css { get; }
    // The default icon library name (e.g. "lucide") used when no library is specified on an icon component.
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
    // Per-token override addressed by CSS variable name (without the leading --) or by Tailwind utility token. Set during object initialization.
    string this[string token] { get; set; }
    // How the app relates to light/dark switching. ThemeMode.Adaptive (the default) keeps today's behavior: overrides restyle the light theme, DarkMode restyles the dark one, and the client's theme preference picks between them. ThemeMode.Fixed commits to ONE scheme: every override is also emitted under the dark selectors, so a client-side theme flip cannot pull the platform's dark palette in under the app's committed colors. For atmospheric, game, or brand-locked looks that should never light/dark switch.
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
  // Keyboard-key display (the shadcn Kbd): a small inset chip for a shortcut key or combo. Complete default-marked composite for view.Kbd; the Group wrapper spaces several keys in a combo.
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
  // Rendered-markdown body. The renderer hands react-markdown's plain HTML straight to the document, and the Crosswind preflight zeroes borders and spacing on every element, so a markdown document with no token renders as undifferentiated prose — tables without rules, blockquotes without a bar, fenced code indistinguishable from a paragraph. Deliberately sets no base color, size or width: markdown is embedded in a page that has already chosen those, and a token color here would override the surrounding text. Anchors are styled by the renderer itself.
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
  // Menu-surface primitives (the shadcn DropdownMenuItem / Label / Separator family), for the rows inside popover menus, account menus, and context menus. A menu row is NOT a button look: it rests transparent, fills the row, reads left, and highlights on hover — so these are complete default-marked composites for view.Button rather than additions to the Button tones. Selection/active state stays a caller concern (add bg-brand-selected on the active row).
  static class Menu
    const string Content
    const string Item
    // The destructive row (Log out, Delete) — error text with an error-tinted hover, same geometry as Item.
    const string ItemDestructive
    // A non-interactive section heading between item groups.
    const string Label
    // The thin rule between item groups.
    const string Separator
    // Right-aligned muted shortcut hint on a menu row (pairs with Kbd).
    const string Shortcut
  static class Menubar
    const string Content
    const string Default
    const string Item
    const string Root
    const string Separator
    const string Trigger
  // Microphone capture buttons (PushToTalkButton, MicToggleButton). A mic button must always show its live state: Active keys on the client-stamped data-ikon-capture-active attribute, so the recording feedback is zero-latency — it flips the moment the capture starts, with no server round trip. Compose Active into any custom mic style so recording never becomes invisible.
  static class MicButton
    const string Active
    const string Base
    const string Default
    const string Lg
    const string Md
    const string Sm
    // Reveals its element only while a capture button inside the same group is held. Like Active it keys on the client-stamped attribute, so it lands on press rather than a round trip later — which is what makes a hold-to-talk control read as held rather than toggled. Put group on the row containing both the button and this element; pair with AudioWave for the recording cue.
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
  // Themed native scrollbars, for the container that has to scroll itself — a header strip, a horizontal chip row, anything an overlay-based ScrollArea would over-serve. A bare overflow-auto shows the OS scrollbar, which on Windows is a wide grey slab that matches no theme and moves the layout when it appears. scrollbar-width/scrollbar-color cover Firefox; the ::-webkit-scrollbar rules cover Chrome and Safari, which ignore them. Both axes are sized on purpose: a width alone leaves the HORIZONTAL bar at its default height, which is the usual way this lands half-applied.
  static class Scrollbar
    const string Default
    // No scrollbar at all, still scrollable by wheel, drag and keyboard. Only for a strip whose overflow is obvious from its content (a carousel, a chip row that visibly cuts off) — content that scrolls with nothing to say so is content most people never find.
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
  static class Tabs
    const string Content
    const string List
    const string ListVertical
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
    // Light + dark, switchable (the default). Base overrides restyle the light theme, IkonTheme.DarkMode restyles the dark one, and the client preference picks. Style code should use theme-aware semantic classes for every surface that must adapt.
    Adaptive
    // One committed scheme, no light/dark switching. Every override is emitted for both theme states, so a client-side flip changes nothing the theme defines. Hardcoded palette classes are first-class citizens here — the look is intentionally theme-invariant.
    Fixed
  // The canonical theming vocabulary: shadcn-style theme keys and what they commit. Each alias expands to the canonical CSS variables that make its intent real across every consumer (components, focus rings, native clients). This table is the single source of truth — the theme renderer expands aliases through it, the codegen styling tools fan roles out through it, and the docs drift tests lock the published reference tables to it. Collision policy: `primary` as a THEME KEY means brand (the shadcn reading; the Untitled-UI tiered reading only ever existed on the prefixed utility classes, which are untouched). Bare `accent` and `secondary` are deliberately NOT aliases — their shadcn and Ikon meanings genuinely conflict, so they stay unknown-key warnings instead of guessing.
  static class ThemeVocabulary
    // Every accepted alias, keyed by name.
    static IReadOnlyDictionary<string, ThemeVocabulary.Alias> Aliases { get; }
  // One vocabulary entry: an accepted theme key and the canonical variable keys it commits. Targets are always canonical (never other aliases), so expansion is one step.
  sealed record ThemeVocabulary.Alias
    ctor(string Name, IReadOnlyList<string> Targets, ThemeVocabulary.ValueKind Kind)
    ThemeVocabulary.ValueKind Kind { get; init; }
    string Name { get; init; }
    IReadOnlyList<string> Targets { get; init; }
  // What value shape an alias expects, for docs and tooling.
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
  // To take effect, assign an instance to TailwindCustomStyleScope.FlutterTheme and pin that scope via TailwindCustomStyleRegistry.PushScope; the resolver then resolves colour scales and semantic tokens against it instead of the platform baseline. Lookup values may be concrete colours, scale references ("neutral-800"), or other semantic tokens — the resolver chases references and normalizes concrete colours to hex. Construct with the object-initializer form, which names each map (new FlutterThemeSource { ScaleColors = …, LightSemantic = …, DarkSemantic = … }); ScaleColors, LightSemantic, and DarkSemantic share a dictionary type, so a positional form would let a transposition of the light and dark maps compile and silently invert the two modes. Each unset map defaults to empty.
  sealed class FlutterThemeSource
    ctor()
    // Dark-mode semantic tokens keyed by "prefix-name" ("bg-surface"). Values are raw colour strings, scale references ("neutral-800"), or other semantic tokens — copied verbatim from the tokens, so not necessarily hex.
    IReadOnlyDictionary<string, string> DarkSemantic { get; init; }
    // Keyed by role ("body", "display", "heading", …); values are plain family names ("Fraunces"), not CSS font stacks.
    IReadOnlyDictionary<string, string> FontFamilies { get; init; }
    // Light-mode semantic tokens keyed by "prefix-name" ("bg-surface"). Values are raw colour strings, scale references ("neutral-800"), or other semantic tokens — copied verbatim from the tokens, so not necessarily hex.
    IReadOnlyDictionary<string, string> LightSemantic { get; init; }
    // Logical px. Rung values derive from this unless RadiusPx pins a rung explicitly; null means platform default.
    double? RadiusBasePx { get; init; }
    // Values are logical px, keyed by rung name (e.g. "lg"); a pinned rung overrides the value derived from RadiusBasePx.
    IReadOnlyDictionary<string, double> RadiusPx { get; init; }
    // Colour-scale entries keyed by "scale-shade" ("neutral-800"). Values are the raw colour strings copied verbatim from the tokens — they may be oklch(...), #rrggbb, or any other CSS colour form, not necessarily hex.
    IReadOnlyDictionary<string, string> ScaleColors { get; init; }
    // Logical px per spacing unit; scales every numeric spacing utility. Null means platform default (4px).
    double? SpacingUnitPx { get; init; }
    // Maps colours only (colour scales plus light/dark semantic tokens). Radii, typography, and spacing are NOT mapped and stay at platform defaults unless supplied via the object initializer.
    static FlutterThemeSource FromDesignTokens(CanvasDesignTokenDocument document)
  enum TailwindColorContext
    // Untyped context (rings, shadows, gradients). The only context that falls back to the union of all aliases — background, foreground, text, and border merged — when the name is not found in a family-scoped map.
    Generic
    // Family-scoped to background aliases only. An alias defined under another context does not resolve here; unlike Generic, it does not fall back to the merged union.
    Background
    // Family-scoped to foreground aliases only. An alias defined under another context does not resolve here; unlike Generic, it does not fall back to the merged union.
    Foreground
    // Family-scoped to text aliases only. An alias defined under another context does not resolve here; unlike Generic, it does not fall back to the merged union.
    Text
    // Family-scoped to border aliases only. An alias defined under another context does not resolve here; unlike Generic, it does not fall back to the merged union.
    Border
  // Custom colour alias maps split by role. Construct with the object-initializer form, which names each property (new TailwindColorDefinitions { Background = …, Text = … }); the four maps share a dictionary type, so a positional form would let a transposition of any two compile and silently mis-map the roles. An omitted map defaults to empty.
  sealed class TailwindColorDefinitions
    ctor()
    IReadOnlyDictionary<string, string> Background { get; init; }
    IReadOnlyDictionary<string, string> Border { get; init; }
    IReadOnlyDictionary<string, string> Foreground { get; init; }
    IReadOnlyDictionary<string, string> Text { get; init; }
    void Validate()
  static class TailwindCssBaseline
    static string AdditionalCss { get; }
    // Intentionally empty. Tailwind's stock palette has no separate dark root-variable set — dark mode is expressed through utility classes, not a second baseline — so there is nothing to parse here. A theme's dark appearance comes entirely from the dark overrides the app passes to TailwindCssVariables; those are merged onto this empty base, so an app that emits dark CSS must supply its own dark values rather than expecting a baseline to fall back on.
    static IReadOnlyDictionary<string, string> DarkVariables { get; }
    static IReadOnlyDictionary<string, string> LightVariables { get; }
    // Keyed "{name}-{step}" (e.g. "red-50") → OKLCH value.
    static IReadOnlyDictionary<string, string> PaletteColors { get; }
    // Ordered as first seen in the baseline.
    static IReadOnlyList<string> PaletteNames { get; }
    // Ascending numeric order.
    static IReadOnlyList<string> PaletteSteps { get; }
    static string GetFullBaseline()
  // Light and dark CSS variable maps for a compiled theme, each merged over the Tailwind baseline. Construct with the object-initializer form, which names each map (new TailwindCssVariables { Light = …, Dark = … }); the two maps share a dictionary type, so a positional form would let a transposition compile and silently invert the emitted light/dark CSS. An omitted map defaults to the baseline alone.
  sealed class TailwindCssVariables
    ctor()
    // CSS variables for the dark theme, merged over the Tailwind dark baseline.
    IReadOnlyDictionary<string, string> Dark { get; init; }
    // Theme name the dark variables are emitted under.
    string DarkThemeName { get; init; }
    // CSS variables for the light theme, merged over the Tailwind light baseline.
    IReadOnlyDictionary<string, string> Light { get; init; }
    string EmitDark()
    string EmitLight()
  // Pin a TailwindCustomStyleScope with PushScope around each compile; lookups prefer the ambient scope and fall back to a process-wide scope for legacy single-app hosts.
  static class TailwindCustomStyleRegistry
    // Flutter theme data of the scope active for the current compile, preferring the ambient scope like the alias lookups do.
    static FlutterThemeSource? CurrentFlutterTheme { get; }
    static bool IsFontFamilyToken(string name)
    static bool IsFontWeightToken(string name)
    // Makes the given scope the ambient alias source for the current async flow until the returned handle is disposed. Compilation call sites stay static, but each caller can pin its own scope for the duration of a compile.
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
  // Custom font family and weight alias maps. Construct with the object-initializer form, which names each property (new TailwindFontDefinitions { Family = …, Weight = … }); the two maps share a dictionary type, so a positional form would let a transposition compile and silently mis-map the roles. An omitted map defaults to empty.
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
    // True when variants contains the given target marker.
    static bool Has(IReadOnlyList<string> variants, string target)
    // Returns the same reference (no copy) when the marker is absent.
    static IReadOnlyList<string> Without(IReadOnlyList<string> variants, string target)
    const string Flutter
    const string Web

# Ikon.App Public API

namespace Ikon.App
  // Attribute that decorates app classes to configure their connection and messaging behavior. The decorated class must declare the app entry point as a public parameterless method named Main — either a synchronous void method or an async Task method, but NOT async void (an async void Main is fire-and-forget: it is never awaited, so its exceptions escape startup error handling and the app can report ready while Main faulted). It is discovered by reflection and invoked once at startup after dependencies are ready; a missing or misnamed Main throws at startup. Declare the UI and endpoints in Main and return — do not block or await indefinitely.
  sealed class AppAttribute : Attribute
    // name: Display name of the app. Defaults to the class name if not specified
    // productId: Unique identifier for the app. Defaults to the full type name if not specified
    // description: Human-readable description of the app. Defaults to "{ClassName} App" if not specified
    // version: Version number of the app
    // guid: Stable identifier for the app that persists across class renames. Used by external systems to identify apps independently of their type name
    // userType: Indicates whether the app operates autonomously (Machine) or represents a human user connecting through it (Human). Defaults to Machine
    // receiveOpcodeGroups: Opcode groups this app subscribes to receive messages from. Almost all apps leave this at the default; change it only for specialized protocol-level message filtering
    // sendOpcodeGroups: Opcode groups this app is allowed to send messages to. Almost all apps leave this at the default; change it only for specialized protocol-level message filtering
    // dependencies: Product IDs of other apps that must reach ready state before this app's Main() runs (and before its StartingAsync event fires); they are awaited during connect
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, string[]? dependencies = null)
    // Each listed app is awaited during connect — before this app's Main() runs and before its StartingAsync event fires — so ordering logic belongs in Main()/ StartingAsync, not in ClientJoinedAsync. Use it to order dependent app startup.
    string[] Dependencies { get; }
    // Human-readable description of the app. Defaults to "{ClassName} App" if not specified
    string? Description { get; }
    // Stable identifier for the app that persists across class renames. Used by external systems to identify apps independently of their type name
    string? Guid { get; }
    // Display name of the app. Defaults to the class name if not specified
    string? Name { get; }
    // Unique identifier for the app. Defaults to the full type name if not specified
    string? ProductId { get; }
    // Opcode groups this app subscribes to receive messages from. Almost all apps leave this at the default; change it only for specialized protocol-level message filtering
    Opcode ReceiveOpcodeGroups { get; }
    // Opcode groups this app is allowed to send messages to. Almost all apps leave this at the default; change it only for specialized protocol-level message filtering
    Opcode SendOpcodeGroups { get; }
    // Indicates whether the app operates autonomously (Machine) or represents a human user connecting through it (Human). Defaults to Machine
    UserType UserType { get; }
    // Version number of the app
    int Version { get; }
  // Register every route before calling StartAsync; routes added afterward are not served.
  sealed class AppEndpointHost : IAsyncDisposable
    // Creates a new HTTP/WebSocket endpoint host. The relay tunnel is not allocated until StartAsync is called.
    // app: The app instance.
    // secure: When true (the default) the public URL is https://… with TLS terminated at the relay. When false, plain http://….
    // webSocketKeepAliveInterval: WebSocket keep-alive ping interval. Defaults to 10 seconds.
    // stablePortName: When non-empty, the relay assigns a deterministic public port for this name, so PublicUrl stays the same across reconnects and process restarts. Empty = ephemeral.
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    // True once the relay tunnel is allocated and PublicUrl can be read. False before StartAsync, and after it when the relay was unreachable — the host then serves on LocalPort only and retries the allocation in the background; subscribe to PublicUrlAvailable to learn when the tunnel comes up.
    bool HasPublicUrl { get; }
    // Throws InvalidOperationException when read before StartAsync has completed.
    int LocalPort { get; }
    // Invoked once per inbound HTTP/WebSocket request before it is routed. Used to mark external activity (e.g. reset the server's idle timer) so an endpoint-served instance isn't reaped while it is serving traffic. Null = no hook.
    Action? OnRequest { get; set; }
    // Throws InvalidOperationException when read before the relay tunnel is allocated; guard with HasPublicUrl when the relay may be unreachable.
    string PublicUrl { get; }
    // Stops the host, releases the relay tunnel, and releases all resources.
    ValueTask DisposeAsync()
    // Registers a handler for HTTP DELETE requests matching the specified route pattern.
    void MapDelete(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for HTTP GET requests matching the specified route pattern.
    void MapGet(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for the given HTTP verb(s) matching the specified route pattern.
    void MapMethods(string pattern, string method, Func<HttpContext, Task> handler)
    // Registers a handler for HTTP PATCH requests matching the specified route pattern.
    void MapPatch(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for HTTP POST requests matching the specified route pattern.
    void MapPost(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for HTTP PUT requests matching the specified route pattern.
    void MapPut(string pattern, Func<HttpContext, Task> handler)
    // The framework closes and disposes the socket once the handler returns; do not dispose it or use it past the handler's completion.
    void MapWebSocket(string pattern, Func<HttpContext, WebSocket, Task> handler)
    // Returns as soon as the host is serving and keeps running in the background — it does not block for the host's lifetime. A failed relay allocation is non-fatal.
    Task StartAsync(CancellationToken cancellationToken = default)
    // Stops the endpoint host gracefully. Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = default)
    // Only for an app whose endpoints are useless without their public URL, and which would rather start late than start wrong — a relay being redeployed takes a few seconds to come back. Do NOT await this on the app initialization path of an app that renders UI: it blocks first paint on something the app does not need in order to draw.
    Task<bool> WaitForPublicUrlAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    // Fires only for the background-retry allocation; not raised when the tunnel was already allocated during StartAsync.
    event Action<string>? PublicUrlAvailable
  // Read precedence: a runtime-written file wins over a repo-seeded file at the same path. Writes always go to cloud storage (never the local disk), so they persist across deploys; repo-seeded files change by changing the repo. The public tree cannot READ repo-seeded files (in the cloud they live with the frontend, not the app) — it reads and writes runtime files, and GetUrlAsync covers seeded files by returning the path URL the frontend serves.
  sealed class AppFileTree
    // Deletes a runtime-written file; deleting a missing file is a no-op. A repo-seeded file cannot be deleted here — it ships with the app, so remove it from the repo instead.
    Task DeleteAsync(string path, CancellationToken ct = default)
    // Whether the file exists — as a runtime-written file or a repo-seeded one.
    Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    // The URL a browser (or an external service) loads this file from. A runtime-written file returns its cloud storage URL. On the public tree, any other path returns the root-relative path URL ("logo.png" → "/logo.png") the frontend serves repo-seeded statics at — derived from the path, not verified to exist. Private repo-seeded files have no URL: read them with ReadBytesAsync.
    Task<string> GetUrlAsync(string path, CancellationToken ct = default)
    // Reads a file — a runtime-written file first, then a repo-seeded one. Throws FileNotFoundException when neither exists.
    Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default)
    // Reads a file as UTF-8 text — a runtime-written file first, then a repo-seeded one. Throws FileNotFoundException when neither exists.
    Task<string> ReadTextAsync(string path, CancellationToken ct = default)
    // Writes a file to cloud storage, creating or replacing it. Pass mimeType for anything a browser will load, so it is served with the right content type.
    Task WriteBytesAsync(string path, byte[] bytes, string? mimeType = null, CancellationToken ct = default)
    // Writes UTF-8 text to cloud storage, creating or replacing the file.
    Task WriteTextAsync(string path, string text, CancellationToken ct = default)
  // The app's two file trees, one namespace each for repo-seeded and runtime-written files: Public is world-visible by URL, Data is private to the app. The repo seeds the trees (root public/ and data/ folders); the app writes to them at runtime through this API. Runtime-written files persist across deploys; repo files redeploy with the app.
  sealed class AppFiles
    // The private tree: readable only by the app. Repo-seeded files come from the app's root data/ folder (shipped with the app, read-only); files the app writes here land in private cloud storage and survive restarts and deploys.
    AppFileTree Data { get; }
    // The public tree: everything here is reachable by URL. Repo-seeded files under the app's root public/ folder are served by the frontend at their path (public/hero.png → /hero.png); files the app writes here land in public cloud storage with a stable URL. Use it for anything a browser should load — generated images, exports, share cards.
    AppFileTree Public { get; }
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own schema/*.tp files (compiled by ikon app teleport build); each carries its own Opcode.GROUP_APP_LOCAL opcode and is sent/received as a native type — no JSON marshalling. Delivery is server-controlled and explicit: SendMessageAsync<T> always takes the recipient client session IDs — there is no implicit broadcast to every client. Whether a type travels reliably or unreliably is declared on the .tp schema (unreliable = true), not here.
  static class AppMessaging
    // Filtered by the type's opcode; the handler receives the decoded payload and the sender's client session id. Dispose the returned handle to unsubscribe.
    static IDisposable OnMessage<T>(this IMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    // There is no implicit broadcast — you must pass the explicit recipient session IDs. Whether the type travels reliably or unreliably is declared on its .tp schema, not here.
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    // Send a typed app message to a single client.
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
  // The app session's ambient services — the sanctioned way for code with no IApp<TSessionIdentity, TClientParameters> reference (cell types above all) to reach the session's databases and secrets. Async-local per server session: under shared hosting several servers run in one process, each with its own instance on its own execution flow — which is why app code must reach these through AppServices.Instance and never cache them in true statics (a process-global would bleed one tenant's database and secrets into another). Cells can be CONSTRUCTED before the app instance finishes starting (the cell host instantiates cell types for endpoint discovery, and a cell-host process never runs the user's Main at all), so consumers must not assume initialization order: await WhenReadyAsync — or check IsReady from synchronous paths — before first use.
  sealed class AppServices : AsyncLocalInstance<AppServices>
    ctor()
    // The hosting app of a CELL-HOST session — the handle a cell needs to construct session services like Audio/Video and receive that session's media. Set ONLY in cell-host mode, where the session serves exactly one cell instance; null in ordinary app instances (a cell shared by many per-user instances has no single app, and media there belongs to whichever instance the client connected to).
    IAppBase? HostApp { get; }
    // False until the session's app startup has provided the services.
    bool IsReady { get; }
    Secrets Secrets { get; }
    // Create an unopened connection to one of the app's databases, or to its default one when no name is given. Provisions the built-in database on first use.
    Task<DbConnection> DatabaseAsync(string? databaseName = null)
    // Wait for readiness, then create and open a connection — the per-operation shape.
    Task<DbConnection> OpenDatabaseAsync(string? databaseName = null, CancellationToken ct = default)
    // Completes when the services are available. Safe to await from a cell constructor's background work regardless of construction order.
    Task WhenReadyAsync()
  // Delegate for async event handlers in the app lifecycle.
  delegate AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
  // Three ways to send audio, by pacing: SpeakAsync / SendSpeech are real-time paced by the speech mixer and new speech interrupts current speech with a fade — the default for spoken replies. StreamAsync plays a complete clip (decoded file, generated music) paced to real time, without the mixer's interruption semantics. SendImmediateAsync transmits at once with no pacing — only for audio already produced in real time or very short clips; a long clip sent this way arrives all at once and can overflow client audio buffers.
  class Audio
    ctor(IAppBase app)
    // Default encoder options for audio output
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    // Audio stream metrics
    AudioMetrics Metrics { get; }
    // The default speech mixer
    SpeechMixer SpeechMixer { get; }
    // Closes all audio streams.
    ValueTask CloseAllAsync()
    // Closes an audio stream and sends the stream end message.
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    // Gets information about an output stream if it exists.
    // streamId: The stream id
    AudioOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Gets a client's most recent playback report for an output stream — how far it has actually rendered the audio and whether the user can currently hear it. Null when the client has not reported yet (older SDKs never report). Reports arrive roughly twice per second while audio is playing; check AudioPlaybackStatus.ReceivedAtUtc for staleness.
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
    // text: The text to speak. Whitespace-only text is a no-op
    // model: The speech generator model to use
    // voice: Optional voice id. Null uses the model's default voice
    // instructions: Optional delivery instructions (tone, emotion, style). Support is model-specific; unsupported models ignore them
    // speed: Optional speaking speed, where 1.0 is normal (e.g. 0.8 is slower, 1.2 is faster). Null leaves the model's default. Support is model-specific; unsupported models ignore it
    // effects: Optional audio effects to apply
    // analyzers: Optional audio analyzers
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
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
    // model: The speech recognizer model to use (e.g., WhisperLarge3Turbo).
    // language: Optional language hint (e.g., "en", "fi"); empty string lets the model autodetect.
    // config: Optional turn detector tuning (silence windows, min speech length, VAD plug-in). Null uses defaults tuned for conversational voice.
    // speculative: When true (default), transcription starts at the probable turn end so the confirmed turn has zero added recognition latency.
    // pauseWhileAppSpeaking: When true (default), detection is suppressed while the app is audibly speaking, so the app's own voice played through speakers can't trigger turns. Set false for barge-in apps (best paired with an echo-robust TurnDetectorConfig.SpeechClassifier).
    // requireCorrelatedStream: When true (default), only detects turns on streams initiated through a CaptureButton (those with a CorrelationId). Set false to detect on every audio stream including ad-hoc ones.
    // timeout: Per-recognition timeout.
    void UseTurnDetection(SpeechRecognizerModel model = WhisperLarge3Turbo, string language = "", TurnDetectorConfig? config = null, bool speculative = true, bool pauseWhileAppSpeaking = true, bool requireCorrelatedStream = true, TimeSpan? timeout = null)
    // args.Samples are decoded float PCM at the sample rate from the stream's begin event; IsFirst/IsLast bracket one captured segment (e.g. one push-to-talk press).
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Handlers may set args.StreamingMode to control when the stream's frames are delivered (streamed live, or buffered until the total duration is known / until the last frame).
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    // Event raised when an incoming audio stream ends
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Event raised when a client reports its audio playback status — actual playout position and audibility (playing, blocked on a user gesture, or hidden). Clients send reports periodically while a stream is active and immediately on state changes. Use GetPlaybackStatus for the latest snapshot per client.
    event AsyncEventHandler<AudioPlaybackReportEventArgs> PlaybackReportReceivedAsync
    // Exactly one of this and SpeechRecognizedAsync fires per completed segment (neither fires once the app is shutting down). An app that latches busy state when capture stops — a "Transcribing..." spinner, a disabled button — must release it here as well as in SpeechRecognizedAsync; handling only the success event leaves that state stuck on for any press that produces no speech.
    event AsyncEventHandler<SpeechNotRecognizedEventArgs> SpeechNotRecognizedAsync
    // Fires only after UseSpeechRecognition or UseTurnDetection has been called once at setup; subscribing without one of those means this event never fires.
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
    // Event raised when a turn has probably ended and its speculative transcript is ready. Requires UseTurnDetection to be called once during app setup. Start downstream work (e.g. generating a reply) with the args' cancellation token: it is cancelled if the user resumes speaking; otherwise SpeechRecognizedAsync confirms the turn with the same TurnSpeculativeEventArgs.TurnId.
    event AsyncEventHandler<TurnSpeculativeEventArgs> TurnSpeculativeAsync
    // Event raised when a user starts a speech turn on a turn-detected stream. Requires UseTurnDetection to be called once during app setup. Useful as a barge-in or listening-indicator hook.
    event AsyncEventHandler<TurnStartedEventArgs> TurnStartedAsync
  // Event arguments raised when an incoming audio frame is received
  class AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, Context clientContext, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Correlation identifier inherited from the AudioStreamBegin (e.g., set by a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Whether this is the first frame in a sequence
    bool IsFirst { get; }
    // Whether this is the last frame in a sequence
    bool IsLast { get; }
    // Decoded floating point PCM samples in range [-1.0, 1.0]
    float[] Samples { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // Total duration of the audio if known, otherwise zero
    TimeSpan TotalDuration { get; set; }
    // User identifier
    string UserId { get; }
  // Event arguments raised when an incoming audio stream begins
  class AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, int sampleRate, int channelCount, Context clientContext, int trackId, string? correlationId)
    // Number of audio channels
    int ChannelCount { get; }
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Optional correlation identifier set by the originator (e.g., a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Description of the audio stream
    string Description { get; }
    // Sample rate in Hz
    int SampleRate { get; }
    // Source type of the audio stream (e.g., "microphone")
    string SourceType { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // Controls when frames are output (can be modified by event handler)
    AudioInputStreamingMode StreamingMode { get; set; }
    // Client- and audio-specific track number for the audio stream
    int TrackId { get; }
    // User identifier
    string UserId { get; }
  // Event arguments raised when an incoming audio stream ends
  class AudioInputStreamEndEventArgs : EventArgs
    ctor(string streamId, Context clientContext, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Correlation identifier inherited from the AudioStreamBegin (e.g., set by a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // User identifier
    string UserId { get; }
  // Information about an output audio stream
  record AudioOutputStreamInfo
    ctor(string StreamId, int TrackId, AudioCodec Codec, int SampleRate, int ChannelCount)
    int ChannelCount { get; init; }
    AudioCodec Codec { get; init; }
    int SampleRate { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
  // Event arguments for the Audio.PlaybackReportReceivedAsync event.
  class AudioPlaybackReportEventArgs : EventArgs
    ctor(AudioPlaybackStatus status)
    // The client's reported playback status
    AudioPlaybackStatus Status { get; }
  // A client's most recent playback report for an outgoing audio stream — how far it has actually rendered the audio and whether the user can currently hear it.
  sealed class AudioPlaybackStatus
    ctor()
    // Audio buffered on the client, awaiting playout
    TimeSpan BufferedDuration { get; init; }
    // The reporting client's session id
    int ClientSessionId { get; init; }
    // The stream epoch the report refers to
    uint Epoch { get; init; }
    // Playout position within the epoch. Null when the client cannot observe it (e.g. WebRTC playback)
    TimeSpan? PlayedDuration { get; init; }
    // When the report was received (UTC)
    DateTime ReceivedAtUtc { get; init; }
    // Whether the client is audibly playing, blocked on a user gesture, or hidden/backgrounded
    AudioPlaybackState State { get; init; }
    // The reported stream's track id
    int TrackId { get; init; }
  // Signals the server that the plugin is doing background work, preventing the idle shutdown timer from advancing. Supports ref counting for multiple concurrent background work scopes.
  class BackgroundWork
    // Calls are ref-counted: the server is notified only on the first StartAsync and the last StopAsync. Dispose the returned scope (or call StopAsync) to release — pair every Start with exactly one release or idle shutdown stays blocked.
    ValueTask<IAsyncDisposable> StartAsync()
    // Signals that one unit of background work has completed. The server is only notified when the last active scope is stopped.
    ValueTask StopAsync()
  // Options for a client-side microphone capture started with ClientFunctions.StartAudioCaptureAsync. Every property is optional; a null property leaves that setting to the client. Start from Default and override what you need.
  sealed record ClientAudioCaptureOptions
    ctor()
    // Whether the client normalizes the microphone level. Null lets the client choose.
    bool? AutoGainControl { get; init; }
    // Target encoder bitrate in bits per second. Null lets the client choose.
    int? Bitrate { get; init; }
    // Sensible speech defaults: 32 kbit/s, auto gain control and noise suppression on, echo cancellation off (nothing is being played back in the common server-transcription case). Device is left to the client; the server receives the stream.
    static ClientAudioCaptureOptions Default { get; }
    // Id of a specific microphone to use. Null uses the client's default device.
    string? DeviceId { get; init; }
    // Whether the client cancels the audio it is playing back out of the microphone signal. Needed for two-way calls on a loudspeaker; pointless — and lossy — when nothing is being played back, which is why Default leaves it off. Null lets the client choose.
    bool? EchoCancellation { get; init; }
    // Whether the client filters steady background noise out of the microphone signal. Null lets the client choose.
    bool? NoiseSuppression { get; init; }
  // Represents a contact picked from the client's contact list.
  sealed record ClientContact
    // Names: The contact's names.
    // Emails: The contact's email addresses.
    // Phones: The contact's phone numbers.
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    // The contact's email addresses.
    IReadOnlyList<string> Emails { get; init; }
    // The contact's names.
    IReadOnlyList<string> Names { get; init; }
    // The contact's phone numbers.
    IReadOnlyList<string> Phones { get; init; }
  // Each method targets the calling client resolved from the current reactive scope unless a targetId is supplied. When the target client has not registered the backing function the call degrades to the failure value (false/null/empty list) rather than throwing — except the capture methods (StartVideoCaptureAsync, StartAudioCaptureAsync, CaptureImageAsync), which throw NotSupportedException.
  static class ClientFunctions
    // Captures a single image from the client's camera.
    // options: Optional image capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support image capture.
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Requests the client to exit fullscreen mode.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> ExitFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current battery level on the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<int?> GetBatteryLevelAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the browser language preference from the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetLanguageAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current GPS location from the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<ClientLocation?> GetLocationAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the list of available media input devices on the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // The value is whatever the browser's Network Information API exposes and mixes two vocabularies: a speed class ("slow-2g", "2g", "3g", "4g") where only that is available — note a fast wifi connection commonly reports "4g" — or a connection medium ("wifi", "cellular", "ethernet", "bluetooth", "none", ...) on platforms that expose it. Treat it as an informational hint, not a reliable wifi/cellular discriminator.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetNetworkTypeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the browser timezone from the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetTimezoneAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current browser URL path and query string from the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetUrlAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current page visibility state on the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<ClientVisibility> GetVisibilityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Prevents or allows the screen to sleep on the client.
    // enabled: Whether to keep the screen awake.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> KeepScreenAwakeAsync(bool enabled, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts the client's sign-in flow for a redirect-based OAuth provider (e.g. "google", "microsoft"). The page navigates to the provider and returns authenticated, so the current session ends and the client reconnects with its real identity. Use from a server-drawn sign-in button in a deferred-login app; guest/email/passkey flows are client-initiated and not supported here
    // provider: The OAuth provider to sign in with (e.g. "google").
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LoginAsync(string provider, int? targetId = null, CancellationToken cancellationToken = default)
    // Prompts the client to show its login UI (deferred login flow).
    // reason: Optional reason shown in the login dialog.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LoginShowAsync(string? reason = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Clears the auth session and reloads the page, returning the client to the login screen.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LogoutAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Opens an external URL in a new browser tab on the client.
    // url: The URL to open. Must be absolute (e.g., starts with https://).
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> OpenExternalUrlAsync(string url, int? targetId = null, CancellationToken cancellationToken = default)
    // Plays a sound on the client from a URL.
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
    // Requests the client to enter fullscreen mode.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> RequestFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Scrolls the page to a specific position on the client.
    // x: Horizontal scroll position in pixels.
    // y: Vertical scroll position in pixels.
    // smooth: Whether to animate the scroll.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the UI theme on the client.
    // theme: The theme to set.
    // persist: Whether to persist the theme as a user preference.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> SetThemeAsync(Theme theme, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the UI theme on the client by its wire name. Prefer SetThemeAsync for the built-in dark and light themes; this overload exists for custom theme names.
    // themeName: The theme name to set (e.g., "light", "dark", or a custom theme name).
    // persist: Whether to persist the theme as a user preference.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when themeName is null or whitespace.
    static Task<bool> SetThemeAsync(string themeName, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the browser URL without triggering a page reload.
    // url: The URL path to set (relative paths only).
    // replace: If true, replaces current history entry instead of adding a new one.
    // preserveQueryParams: If true, preserves existing query parameters when the URL does not contain a query string.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Signals the build-time snapshot capture client that the current view has settled and is ready to be captured. Call when a route's content finishes loading (guard with Context.IsSnapshot); without the signal, capture falls back to a quiescence heuristic that may record loading skeletons for slow-loading routes. No-op outside snapshot capture.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> SnapshotReadyAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Starts audio capture on the client from the microphone.
    // options: Optional audio capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support audio capture.
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts video capture on the client from camera or screen.
    // source: The video source (Camera or Screen).
    // options: Optional video capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support video capture.
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a media capture on the client by its stream ID.
    // streamId: The stream ID of the capture to stop.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when streamId is null or whitespace.
    static Task<bool> StopCaptureAsync(string streamId, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a playing sound on the client.
    // playbackId: The playback ID returned from PlaySoundAsync.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopSoundAsync(string playbackId, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices for the given duration.
    // durationMs: The vibration duration in milliseconds.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentOutOfRangeException: Thrown when durationMs is not positive.
    static Task<bool> VibrateAsync(int durationMs, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices with a vibrate/pause pattern. Values alternate between vibration and pause durations in milliseconds, starting with a vibration — so [100, 50, 100] vibrates 100 ms, pauses 50 ms, then vibrates 100 ms again.
    // pattern: The alternating vibrate/pause durations in milliseconds.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when pattern is null, empty, or contains a negative duration.
    static Task<bool> VibrateAsync(IReadOnlyList<int> pattern, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices from a pattern in its wire form. Prefer the typed overloads taking an int duration or an int pattern; this overload exists for pattern strings that already arrive pre-formatted.
    // pattern: Duration in ms, or comma-separated pattern (e.g., "200" or "100,50,100").
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when pattern is null or whitespace.
    static Task<bool> VibrateAsync(string pattern, int? targetId = null, CancellationToken cancellationToken = default)
  // Whether the client should prefer a hardware or a software video encoder. This is a preference, not a guarantee — the client falls back to whatever encoder it has.
  enum ClientHardwareAcceleration
    // Prefer a hardware encoder: lower CPU use, but the codec/parameter support is device-dependent.
    PreferHardware
    // Prefer a software encoder: more predictable across devices, at a higher CPU cost.
    PreferSoftware
  // A single still image captured on a client with ClientFunctions.CaptureImageAsync.
  sealed record ClientImageCapture
    // Mime: The image's mime type, as encoded by the client: image/jpeg or image/png.
    // Width: The image's actual width in pixels, which can differ from a requested width the client could not honor.
    // Height: The image's actual height in pixels, which can differ from a requested height the client could not honor.
    // Data: The encoded image bytes (a complete JPEG or PNG file, not raw pixels), ready to write to disk or hand to an asset or a vision model.
    ctor(string Mime, int Width, int Height, byte[] Data)
    // The encoded image bytes (a complete JPEG or PNG file, not raw pixels), ready to write to disk or hand to an asset or a vision model.
    byte[] Data { get; init; }
    // The image's actual height in pixels, which can differ from a requested height the client could not honor.
    int Height { get; init; }
    // The image's mime type, as encoded by the client: image/jpeg or image/png.
    string Mime { get; init; }
    // The image's actual width in pixels, which can differ from a requested width the client could not honor.
    int Width { get; init; }
  // Encoding of a single image captured with ClientFunctions.CaptureImageAsync.
  enum ClientImageCaptureFormat
    // JPEG (image/jpeg): lossy, small — the right default for camera frames.
    Jpeg
    // PNG (image/png): lossless, much larger — for screenshots and graphics.
    Png
  // Options for a single still image captured with ClientFunctions.CaptureImageAsync. Every property is optional; a null property leaves that setting to the client. The captured image is always returned to the caller on the server.
  sealed record ClientImageCaptureOptions
    ctor()
    // Image encoding. Null captures JPEG.
    ClientImageCaptureFormat? Format { get; init; }
    // Target image height in pixels. Null keeps the capture device's own height.
    int? Height { get; init; }
    // Encoder quality from 0.0 (smallest, most artifacts) to 1.0 (largest, near-lossless). Only meaningful for ClientImageCaptureFormat.Jpeg — PNG is lossless and ignores it. Null lets the client choose.
    double? Quality { get; init; }
    // Target image width in pixels. Null keeps the capture device's own width.
    int? Width { get; init; }
  // Event arguments for the IAppBase.ClientJoinedAsync event.
  class ClientJoinedEventArgs : EventArgs
    ctor(Context clientContext)
    // Gets the context of the client that joined.
    Context ClientContext { get; }
    // Gets the session ID of the client that joined.
    int ClientSessionId { get; }
    // Gets the user ID of the client that joined, or an empty string if not authenticated.
    string UserId { get; }
  // Event arguments for the IAppBase.ClientLeftAsync event.
  class ClientLeftEventArgs : EventArgs
    ctor(Context clientContext)
    // Gets the context of the client that left.
    Context ClientContext { get; }
    // Gets the session ID of the client that left.
    int ClientSessionId { get; }
    // Gets the user ID of the client that left, or an empty string if not authenticated.
    string UserId { get; }
  // Represents a geolocation with latitude, longitude, and accuracy in meters.
  sealed record ClientLocation
    // Latitude: The latitude coordinate.
    // Longitude: The longitude coordinate.
    // Accuracy: The accuracy of the coordinates in meters.
    ctor(double Latitude, double Longitude, double Accuracy)
    // The accuracy of the coordinates in meters.
    double Accuracy { get; init; }
    // The latitude coordinate.
    double Latitude { get; init; }
    // The longitude coordinate.
    double Longitude { get; init; }
  // Represents a media input device available on the client.
  sealed record ClientMediaDevice
    // DeviceId: The unique identifier for the device.
    // Kind: The kind of device (audio input or video input).
    // Label: A human-readable label for the device.
    // GroupId: The group identifier for devices that share the same physical device.
    ctor(string DeviceId, ClientMediaDeviceKind Kind, string Label, string GroupId)
    // The unique identifier for the device.
    string DeviceId { get; init; }
    // The group identifier for devices that share the same physical device.
    string GroupId { get; init; }
    // The kind of device (audio input or video input).
    ClientMediaDeviceKind Kind { get; init; }
    // A human-readable label for the device.
    string Label { get; init; }
  // The kind of a media input device available on the client.
  enum ClientMediaDeviceKind
    // The client reported a device kind this SDK does not recognize.
    Unknown
    // An audio input device, such as a microphone.
    AudioInput
    // A video input device, such as a camera.
    VideoInput
  // Read-only view of a client's profile. Use ClientProfiles.UpdateAsync to modify profile data.
  sealed class ClientProfile
    // Address information
    ProfileAddress? Address { get; }
    // Birth date
    string? BirthDate { get; }
    // Email address
    string? Email { get; }
    // First name
    string? FirstName { get; }
    // Gender
    string? Gender { get; }
    // Profile ID
    string Id { get; }
    // Preferred language code
    string? Language { get; }
    // Last name
    string? LastName { get; }
    // Display name
    string? Name { get; }
    // Phone number
    string? PhoneNumber { get; }
    // Preferred display name
    string? PreferredName { get; }
    // Raw roles list from backend
    IReadOnlyList<string> Roles { get; }
    // User ID (from Context.UserId)
    string UserId { get; }
    // Computed visible name (PreferredName ?? FirstName ?? empty)
    string VisibleName { get; }
    // Get a specific attribute value by key
    object? GetAttribute(string key)
    // Get typed custom attributes from profile
    TAttributes GetAttributes<TAttributes>() where TAttributes : IProfileAttributes, new()
    // Check if user has a specific built-in role. For roles outside UserRole, check Roles directly.
    bool HasRole(UserRole role)
    // Require that the user has the specified role. Throws RoleRequiredException if not.
    void RequireRole(UserRole role)
  // A connected client's profile is cached when it joins, so lookups for connected clients return from cache; a cache miss loads from the backend asynchronously. Lookups return null when the context carries no UserId or the backend has no matching profile.
  class ClientProfiles
    ctor(IAppBase app)
    // Add a role to a client
    Task AddRoleAsync(Context clientContext, UserRole role)
    // Add a role to a client using string role name
    Task AddRoleAsync(Context clientContext, string role)
    // Clear all cached profiles
    void ClearCache()
    // Find profiles by filter criteria
    Task<IReadOnlyList<ClientProfile>> FindProfilesAsync(Dictionary<string, string> filters, int maxResults = 1000)
    // Get all profiles in the space
    Task<IReadOnlyList<ClientProfile>> GetAllProfilesAsync(int maxResults = 1000)
    // Get typed custom attributes for a client, loading the profile on a cache miss. Returns null if the client has no profile.
    Task<TAttributes?> GetAttributesAsync<TAttributes>(Context clientContext) where TAttributes : IProfileAttributes, new()
    // Get a client's profile, loading it from the backend on a cache miss and caching the result. Connected clients are normally already cached (their profile is loaded when they join), so this usually returns instantly and only hits the backend for an uncached user. Returns null when the context carries no UserId or the backend has no profile for it.
    Task<ClientProfile?> GetProfileAsync(Context clientContext)
    // Get a profile by userId, loading it from the backend on a cache miss.
    Task<ClientProfile?> GetProfileAsync(string userId)
    // Refresh a client's profile from the backend
    Task RefreshProfileAsync(Context clientContext)
    // Refresh a profile from the backend by userId
    Task RefreshProfileAsync(string userId)
    // Remove a role from a client
    Task RemoveRoleAsync(Context clientContext, UserRole role)
    // Remove a role from a client using string role name
    Task RemoveRoleAsync(Context clientContext, string role)
    // Set custom attributes for a client
    Task SetAttributesAsync<TAttributes>(Context clientContext, TAttributes attrs) where TAttributes : IProfileAttributes
    // Set roles for a client
    Task SetRolesAsync(Context clientContext, IEnumerable<UserRole> roles)
    // Set roles for a client using string role names
    Task SetRolesAsync(Context clientContext, IEnumerable<string> roles)
    // Update profile fields using a typed ProfileData object
    Task UpdateAsync(Context clientContext, Action<ProfileData> update)
  // A video codec a client may encode a capture with. Listed in ClientVideoCaptureOptions.PreferredCodecs in priority order; the client picks the first one it can actually encode with and falls back to its own default if none are available.
  enum ClientVideoCaptureCodec
    // H.264 / AVC.
    H264
    // VP8.
    Vp8
    // VP9.
    Vp9
    // AV1.
    Av1
  // Options for a client-side video capture started with ClientFunctions.StartVideoCaptureAsync. Every property is optional; a null property leaves that setting to the client. Start from DefaultCamera or DefaultScreen and override what you need.
  sealed record ClientVideoCaptureOptions
    ctor()
    // Target encoder bitrate in bits per second. Null lets the client choose.
    int? Bitrate { get; init; }
    // Sensible camera defaults: 720p (1280x720) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference. Codec, bitrate, and device are left to the client; the server receives the stream.
    static ClientVideoCaptureOptions DefaultCamera { get; }
    // Sensible screen-share defaults: 1080p (1920x1080) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference. Codec and bitrate are left to the client; the server receives the stream.
    static ClientVideoCaptureOptions DefaultScreen { get; }
    // Id of a specific capture device to use (a camera; ignored for screen capture). Null uses the client's default device.
    string? DeviceId { get; init; }
    // Target frames per second. Null lets the client choose.
    int? Framerate { get; init; }
    // Hardware vs software encoder preference. Null lets the client choose.
    ClientHardwareAcceleration? HardwareAcceleration { get; init; }
    // Target frame height in pixels. Null lets the client choose.
    int? Height { get; init; }
    // How many frames apart key frames (full, independently decodable frames) are emitted. A receiver can only start decoding on a key frame, so this is the worst-case join latency for anyone who starts watching mid-stream, and the resync granularity after packet loss. Lower means faster joins and more bandwidth; higher means the opposite. The presets use 90 frames — three seconds at their 30 fps. Null lets the client choose.
    int? KeyFrameIntervalFrames { get; init; }
    // Codecs to try, in priority order. Null lets the client choose.
    IReadOnlyList<ClientVideoCaptureCodec>? PreferredCodecs { get; init; }
    // Target frame width in pixels. Null lets the client choose.
    int? Width { get; init; }
  // Where a client-side video capture takes its frames from.
  enum ClientVideoCaptureSource
    // The client's camera.
    Camera
    // A screen, window, or browser tab the user picks in the client's screen-share dialog.
    Screen
  // The page visibility state reported by a client.
  enum ClientVisibility
    // The visibility state could not be determined: no connected client, the client does not implement the visibility function, or it reported a state this SDK does not recognize.
    Unknown
    // The page is at least partially visible on the client.
    Visible
    // The page is not visible on the client (background tab, minimized window, locked screen).
    Hidden
  // Dates are inclusive and interpreted in UTC. Category filters to one usage category (e.g. llm, image-generation); EventName filters to one full usage event name (e.g. llm.openai.gpt4o.global.output-text-tokens); Scopes narrows to usage carrying the given scopes, and GroupByScopeType breaks the result down by the id of one scope type.
  sealed record CostQuery
    ctor(DateOnly StartDate, DateOnly EndDate, string? Category = null, string? EventName = null, IReadOnlyList<CostScopeFilter>? Scopes = null, string? GroupByScopeType = null)
    string? Category { get; init; }
    DateOnly EndDate { get; init; }
    string? EventName { get; init; }
    string? GroupByScopeType { get; init; }
    IReadOnlyList<CostScopeFilter>? Scopes { get; init; }
    DateOnly StartDate { get; init; }
  // Scopes are the app's own attribution: whatever the app pushed with Log.Instance.UseScope(new CustomScope(name, id)) around a piece of work is stamped on every usage that work emits, and can be filtered and grouped on here. Several filters are ANDed — usage must carry all of them.
  sealed record CostScopeFilter
    ctor(string Type, string? Value = null)
    string Type { get; init; }
    string? Value { get; init; }
  // Accessed via app.Costs. Costs are reported per day and per usage event name; credits are the billing unit. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
  sealed class CostsService
    // The date range still has to cover when the work ran: usage is stored by day, and a query is only as cheap as the range it scans. An operation that emitted no priced usage sums to zero, which is indistinguishable from one whose usage has not landed yet — see the note on aggregation delay on CostsService before showing the number as final.
    Task<double> GetCreditsForScopeAsync(string scopeType, string scopeId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    // Returns one row per day and usage event name; days without usage produce no rows. Under CostQuery.GroupByScopeType the breakdown is per scope id as well. The result is ordered by date, then event name.
    Task<IReadOnlyList<DailyCost>> GetDailyCostsAsync(CostQuery query, CancellationToken ct = default)
    // Sums the credit cost of all usage in the app's space over the date range (inclusive, UTC).
    Task<double> GetTotalCreditsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
  // A [Cron] method behaves like a [Function] in that the trigger resolves it through the FunctionRegistry by name. Applying [Cron] is enough to register the method (as a Local function) — you do not also need [Function], though combining them is fine. The handler takes no caller-supplied arguments. It may optionally accept a host-injected CronContext (fire time + schedule) and/or a CancellationToken that signals app shutdown, in any order — mirroring how an [HttpPost] handler may accept an HttpRequest. Any other parameter fails registration at startup, since the scheduler has nothing to bind it to. Overlap is allowed: a tick fires even if the previous invocation is still running, so guard re-entrancy yourself if it matters.
  sealed class CronAttribute : Attribute
    // Declares a cron job that runs on schedule.
    ctor(string schedule)
    // Optional registry-name override. When null or empty the function is registered (and triggered) under the full member name of the declaration carrying the attribute, "{DeclaringType.FullName}.{Method}" — the same identity the bundle manifest records, so the backend trigger resolves it even when the method is inherited or overridden.
    string? Name { get; init; }
    // The cron expression that schedules this method (standard 5/6-field cron syntax, e.g. "0 * * * *" for hourly). Evaluated by the backend scheduler. The platform enforces a minimum interval of 5 minutes: a faster schedule is clamped to a slower equivalent when a safe one exists, and rejected at bundle time otherwise.
    string Schedule { get; }
  // Credits is the cost in platform credits — the unit users are billed in. EventName identifies the AI model and usage kind (e.g. llm.openai.gpt4o.global.output-text-tokens) and Category is its first segment (e.g. llm). TotalUsage is the summed usage amount in the event's native unit (tokens, seconds, generations, ...). RawCostEur is the underlying provider cost in EUR and is null unless the space has raw cost visibility enabled. ScopeId is populated only under CostQuery.GroupByScopeType, and is null for usage carrying no scope of that type.
  sealed record DailyCost
    ctor(DateOnly Date, string Category, string EventName, double TotalUsage, double Credits, double? RawCostEur, string? ScopeId = null)
    string Category { get; init; }
    double Credits { get; init; }
    DateOnly Date { get; init; }
    string EventName { get; init; }
    double? RawCostEur { get; init; }
    string? ScopeId { get; init; }
    double TotalUsage { get; init; }
  // Accessed via app.Email. Every operation requires the app's space to have the Email feature enabled; a call against a non-entitled space throws FeatureNotEnabledException.
  sealed class EmailService
    // The backend resolves the id before deleting and rejects an unknown one, so a repeated delete throws HttpRequestException carrying a 404 rather than being treated as a no-op. Callers sweeping ids they no longer track should catch it.
    Task DeleteAsync(string id, CancellationToken ct = default)
    // The returned EmailAttachmentDownload owns the content stream; dispose it (e.g. await using) to release the underlying connection.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = default)
    // Lazily enumerates all received emails matching query, transparently following pages until exhausted. Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single page of received emails for the app's space. Paginate by passing the returned InboxPage.NextCursor back as InboxQuery.Cursor.
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single inbound email with decrypted body and parsed envelope.
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = default)
    // A request that names a sender identity needs a verified sending domain: when the space has none, or the requested EmailSendRequest.SenderDomain is not one of the space's verified sending domains, the send throws EmailSenderNotAvailableException — catch it and resend without the sender fields to deliver from the platform's own address. Invalid field values throw ArgumentException before anything is sent, and a space without the Email feature throws FeatureNotEnabledException.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
  // Shared base for the two developer-facing inbound HTTP surfaces, the verb-named REST attributes (HttpMethodAttribute: [HttpGet], [HttpPost], …) and [Mcp]. They differ only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients; addressing, path templating, identity binding, auth, and abuse-control are identical and live here so there is exactly one place to reason about them.
  abstract class EndpointAttribute : Attribute
    // Defaults to EndpointAuth.Grant; setting AuthPolicy overrides it.
    EndpointAuth Auth { get; init; }
    // When non-empty, takes precedence over Auth.
    string? AuthPolicy { get; init; }
    // Empty = derived from the method name (kebab-cased). A {name} segment whose name matches a field of the owner's SessionIdentity record binds the routing identity; other {name} segments bind as ordinary handler parameters. Never declare a /.well-known/*, /ikon/*, or /api path — those are reserved.
    string Path { get; }
  // The built-in authorization for an endpoint — the discoverable, no-/router/-needed options. For a custom edge policy (an apiKey/hmac/ipAllow helper you defined in /router/), set EndpointAttribute.AuthPolicy to its name instead.
  enum EndpointAuth
    // Requires a valid signed grant in the URL (the default). Possession authorizes.
    Grant
    // Anonymous — no credential; identity comes from the URL, gated only by anti-abuse.
    Public
    // Always rejected. Declares an endpoint while keeping it closed.
    Deny
    // Unlike Grant, nothing here is minted by the app or pasted into a URL: the client discovers the space's authorization server, the human signs in with the space's own [Auth] Methods, and the client holds a short-lived token it refreshes itself. Anonymous sign-in methods (guest, global) cannot satisfy this — a global visitor is one shared space-wide user, so honouring it would hand every client the same identity and the same data. A space declaring only anonymous methods cannot host a User endpoint.
    User
  // Information about an HTTP endpoint exposed by the app — an [HttpGet]/[HttpPost]/[Mcp] surface. Returned by IAppBase.Endpoints for developer convenience.
  sealed record EndpointInfo
    ctor()
    // The cell type for a substrate-cell endpoint (empty for app + AppProcess-cell endpoints). When non-empty, the gateway cell-routes the request to that cell's partitioned instance, keyed by the cell's IdentityFields in the URL; empty means the endpoint resolves to the app instance.
    string CellType { get; init; }
    // The endpoint's registry name — {Owner}_{Method}, derived unconditionally from the owner type and the handler method; endpoints carry no name override. The backend resolves this name when routing.
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
    // Total bytes received and written so far, including this chunk.
    long BytesWritten { get; init; }
    // This chunk's bytes. Only valid for the duration of the callback — copy them if you keep them.
    byte[] Data { get; init; }
    // The client-supplied file name.
    string FileName { get; init; }
    // The client-supplied mime type.
    string MimeType { get; init; }
    // The total file size in bytes the client announced.
    long Size { get; init; }
    // Id identifying this upload.
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
    // The asset the upload was written into, when an earlier hook set FileUploadResult.AssetUri. Null when the file went to a local temp file instead. Exactly one of the two is non-null. It is the same AssetUri every Asset.Instance.* call takes, so it needs no parsing — null-check it and pass .Value straight on.
    AssetUri? AssetUri { get; init; }
    // The client-supplied file name.
    string FileName { get; init; }
    // Path to the received file in a temp directory, when the upload was not redirected to the asset system. Null when AssetUri is set. The temp directory is deleted when the app stops, so move or copy anything you want to keep.
    string? LocalTempFilePath { get; init; }
    // The client-supplied mime type.
    string MimeType { get; init; }
    // The file size in bytes.
    long Size { get; init; }
    // Id identifying this upload.
    string UploadId { get; init; }
  // Terminal hook for an upload that had started (cancel, 60 s stall, out-of-sequence chunk, byte-count or SHA-256 mismatch, write failure). Uploads the app rejected from PreStart or Start never reach here. Any partial file/asset is already deleted — clean up only app-side state.
  sealed record FileUploadErrorArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The file size in bytes the client announced.
    // ErrorMessage: Why the upload failed — the cancellation reason when the app cancelled it, otherwise the platform's description of the failure.
    ctor(string UploadId, string FileName, string MimeType, long Size, string ErrorMessage)
    // Why the upload failed — the cancellation reason when the app cancelled it, otherwise the platform's description of the failure.
    string ErrorMessage { get; init; }
    // The client-supplied file name.
    string FileName { get; init; }
    // The client-supplied mime type.
    string MimeType { get; init; }
    // The file size in bytes the client announced.
    long Size { get; init; }
    // Id identifying this upload.
    string UploadId { get; init; }
  // First hook, before any bytes transfer — the cheapest place to reject (return false or a FileUploadResult and nothing is sent). Hook order: PreStart → Start → Chunk/Progress (per chunk) → Complete on success or Error on failure. Capture Cancel to abort the upload later, e.g. from a UI cancel button.
  sealed record FileUploadPreStartArgs
    // UploadId: Id identifying this upload; the same value appears on every later hook's args.
    // FileName: The client-supplied file name. Untrusted — never join it into a path yourself.
    // MimeType: The client-supplied mime type. Untrusted — the bytes are not verified against it.
    // Size: The file size in bytes the client claims it will send. The upload fails with an error if the actual byte count differs.
    // Cancel: Aborts this upload: deletes whatever was written, fires the error hook with the reason, and tells the client to stop. Usable at any point during the upload, not just from this callback — capture it to cancel later (e.g. from a UI cancel button).
    ctor(string UploadId, string FileName, string MimeType, long Size, Func<string?, Task> Cancel)
    // Aborts this upload: deletes whatever was written, fires the error hook with the reason, and tells the client to stop. Usable at any point during the upload, not just from this callback — capture it to cancel later (e.g. from a UI cancel button).
    Func<string?, Task> Cancel { get; init; }
    // The client-supplied file name. Untrusted — never join it into a path yourself.
    string FileName { get; init; }
    // The client-supplied mime type. Untrusted — the bytes are not verified against it.
    string MimeType { get; init; }
    // The file size in bytes the client claims it will send. The upload fails with an error if the actual byte count differs.
    long Size { get; init; }
    // Id identifying this upload; the same value appears on every later hook's args.
    string UploadId { get; init; }
  // Passed to the onUploadProgress callback — fired once per received chunk, after the chunk has been written and acknowledged. Meant for driving a progress bar; use onChunkReceived if you need the bytes themselves.
  sealed record FileUploadProgressArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The total file size in bytes the client announced.
    // ProgressPercentage: Bytes received so far as a percentage of Size, 0 to 100. Zero for the whole upload when the client announced a size of 0.
    // BytesUploaded: Bytes received and written so far.
    ctor(string UploadId, string FileName, string MimeType, long Size, double ProgressPercentage, long BytesUploaded)
    // Bytes received and written so far.
    long BytesUploaded { get; init; }
    // The client-supplied file name.
    string FileName { get; init; }
    // The client-supplied mime type.
    string MimeType { get; init; }
    // Bytes received so far as a percentage of Size, 0 to 100. Zero for the whole upload when the client announced a size of 0.
    double ProgressPercentage { get; init; }
    // The total file size in bytes the client announced.
    long Size { get; init; }
    // Id identifying this upload.
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
    // The client-supplied file name. Untrusted — never join it into a path yourself.
    string FileName { get; init; }
    // The client-declared SHA-256 of the file contents, lowercase hex. The platform recomputes it while receiving and fails the upload with a hash mismatch if the received bytes disagree, so a match here is a genuine content identity — but it is the client's claim, not yet verification, at this point.
    string Hash { get; init; }
    // The client-supplied mime type. Untrusted — the bytes are not verified against it.
    string MimeType { get; init; }
    // The file size in bytes the client claims it will send.
    long Size { get; init; }
    // Id identifying this upload; the same value appears on every other hook's args.
    string UploadId { get; init; }
  // Marks a method as a DELETE REST endpoint. See EndpointAttribute.
  sealed class HttpDeleteAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Marks a method on an app or cell as a GET REST endpoint. The framework mounts a route on the owner's AppEndpointHost, binds the request, invokes the method, and serializes the return value; authorization runs at the gateway edge (the endpoint's Auth /router/ policy), not in-process. Defaults to Auth = EndpointAuth.Grant (401 on the bare URL); set Auth = EndpointAuth.Public for an anonymous route. See EndpointAttribute for path templating and URL-supplied identity.
  sealed class HttpGetAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Shared base for the verb-named REST attributes ([HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch]). The verb is baked into the attribute type — there is no verb enum — which mirrors the ASP.NET Core idiom and so generates reliably from LLMs. All of them share the addressing + identity model on EndpointAttribute; only the HTTP method differs. Authorization defaults to Auth = EndpointAuth.Grant: the gateway rejects the bare URL with 401 unless the caller was handed a minted grant URL. For an endpoint meant to be anonymously reachable (a public webhook, a health check, an open REST route), set Auth = EndpointAuth.Public explicitly — see EndpointAttribute.Auth.
  abstract class HttpMethodAttribute : EndpointAttribute
    // HTTP verb as an uppercase string (GET / POST / PUT / DELETE / PATCH).
    abstract string Method { get; }
  // Marks a method as a PATCH REST endpoint. See EndpointAttribute.
  sealed class HttpPatchAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Marks a method as a POST REST endpoint — the common case (third-party webhooks included; verify the signature from the injected request context). Defaults to Auth = EndpointAuth.Grant (401 on the bare URL); a public webhook must set Auth = EndpointAuth.Public. See EndpointAttribute.
  sealed class HttpPostAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Marks a method as a PUT REST endpoint. See EndpointAttribute.
  sealed class HttpPutAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Immutable view of an inbound HTTP request — its method, path, query, headers, and raw body. The dispatcher constructs one per inbound request and passes it to any handler that declares an HttpRequest parameter, surfacing the untrusted inputs the typed binding doesn't, such as the raw body needed to verify a webhook signature inline.
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
  // App host interface providing typed session identity and client parameters.
  interface IApp<out TSessionIdentity, out TClientParameters> : IAppBase
    // Resolves the current client from the ambient reactive scope — call it only inside UI.Root() or another ReactiveScope context; outside one there is no current client and it throws.
    virtual TClientParameters ClientParameters { get; }
    // Gets the collection of connected clients with typed parameters. Automatically synced with IAppBase.GlobalState.
    IClientCollection<TClientParameters> Clients { get; }
    // Gets the typed session identity used to determine app instance routing.
    TSessionIdentity SessionIdentity { get; }
  // Base interface for Ikon app hosts providing access to shared state, reactive infrastructure, and lifecycle events.
  interface IAppBase : IMessageChannel
    // Gets the background work tracker that prevents server idle shutdown while work is in progress.
    BackgroundWork BackgroundWork { get; }
    // Costs are reported per day and per usage event name; credits are the billing unit. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
    CostsService Costs { get; }
    // Resolved from the ambient reactive scope: null outside a client scope (e.g. background work, a timer). Identifies the client being served, never this plugin's own connection context.
    virtual Context? CurrentClientContext { get; }
    // Empty string when no client is in scope. This is the correct key for a payment customer key, subscription gating, and per-user state — always populated for a connected client (the real user id when authenticated, else a stable anonymous id).
    virtual string CurrentUserId { get; }
    // An escape hatch for libraries that need a real filesystem path. Prefer Files (Files.Data) — same seeded files, plus runtime writes that persist. Read-only in the cloud — writing to it throws.
    string DataDirectory { get; }
    // Gets the database connection configurations for this app instance.
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // It compares ABSOLUTE occupancy against a share of the memory limit, so it cannot tell an instance filling up with arrivals from an app that is simply large: an app whose own resting footprint already exceeds that share is refused from its first client onward, answering 429 to every one of them. Measure your app's idle footprint before turning this on.
    bool DynamicMaxClientsEnabled { get; set; }
    // Requires the Email feature enabled on the app's organisation/space; calls from a non-entitled space throw FeatureNotEnabledException.
    EmailService Email { get; }
    // Gets the HTTP endpoints ([HttpGet]/[HttpPost]/[Mcp] surfaces) exposed by this app instance, including ready-to-use public URLs with the current session identity and signed token prefilled. The list is built once before Main() runs, from the endpoints declared on the app class and on loaded [Cell] types.
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    // The default implementation throws so hand-rolled test doubles keep compiling; the real app host always provides it.
    virtual AppFiles Files { get; }
    // Gets the platform-wide shared state from the server containing clients, streams, and space/session info.
    GlobalState GlobalState { get; }
    // null except in local dev on a localhost address (no --host-public), where it lets an in-process client reach this exact process over loopback. Via the relay or in the cloud it is null — connect through the normal relay/ApiKey path instead.
    virtual (string Host, int Port)? LocalLoopbackEndpoint { get; }
    // 0 lifts the cap entirely, which means exactly that: nothing then stops arrivals before the container runs out of memory and the kernel kills the instance with no warning and no chance to shed load. Prefer a measured number, or turn on DynamicMaxClientsEnabled alongside it.
    int MaxClients { get; set; }
    // Gets the configured maximum memory limit in megabytes for this server instance.
    int MaxMemoryLimitMb { get; }
    // The Parallax mounts this app renders. Each mount produces an independent UI stream addressable from a host UI as <ParallaxView mount="..." />. Defaults to a single mount named "ikon-ui" — the wire-identical shape of every Ikon app today. Apps with multiple panels or mixed Parallax/external regions can replace the value with a longer list at any time; the render loop reacts and emits UIStreamBegin/UIStreamEnd for additions and removals.
    Reactive<IReadOnlyList<string>> Mounts { get; }
    // Gets the navigation helper for managing URL paths and listening to URL changes.
    Navigation Navigation { get; }
    // Gets the notification service for this app — shows user-facing notifications on connected clients (browser notifications on the web, OS notifications on Flutter native apps). Permission is requested on the client lazily, the first time a notification is actually sent.
    NotificationService Notifications { get; }
    // Gets the payments service for this app — offer plans, take one-off and recurring payments, and react to PaymentReceived events. Set up a provider with ikon app payments enable; the backend drives it and the app holds no payment state.
    PaymentsService Payments { get; }
    // The app's public URL — the address a browser opens to join this app through its space domain. Replaces the app.ReactiveGlobalState.SpaceUrl.Value incantation; reading it inside UI code subscribes to changes the same way. For a URL with query parameters (e.g. a session join link) use JoinUrl.
    virtual string PublicUrl { get; }
    // Values are fetched once at startup and read synchronously; changes made with ikon app secret set while the app runs take effect only after a restart.
    Secrets Secrets { get; }
    // Consulted only during build-time snapshot capture. Returned routes are unioned with the [BootSnapshot] Routes list from ikon-config.toml, validated, and deduped.
    Func<Task<IEnumerable<string>>>? SnapshotRoutesProvider { get; set; }
    // Gets the database that backs persistent reactive state, named by StateDatabase in the app's ikon-config toml. Empty means the built-in app database. An app whose databases carry other names sets this so its state lives in Postgres rather than falling back to asset storage.
    virtual string StateDatabase { get; }
    // Call TelephonyService.GetStatusAsync to find out whether the space has telephony, or TelephonyService.GetNumbersAsync for the numbers themselves, rather than discovering either from a failed send.
    TelephonyService Telephony { get; }
    // Enabled by default. Applies only to clients that connect after it is set; already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    // Enabled by default. Disable (e.g. in Main) for apps with no audio/video or low-latency data to save per-client peer-setup cost. Applies only to clients that connect afterward; already-connected clients are unaffected until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Persist the returned bytes as your system of record — the platform's session retention is short. Blocks until the signer completes the ceremony and the platform packages the signed PDF.
    // signerClientSessionId: The client session ID whose browser should perform the signing ceremony.
    // request: The signature order specification (documents, signer policy, purpose).
    // ct: Cancellation token. The order expires server-side after the configured TTL regardless.
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default)
    // The connection comes back unopened: open it and dispose it yourself, e.g. await using var connection = await app.DatabaseAsync(); await connection.OpenAsync();. Running a command before opening throws connection not open. Name nothing and you get the app's default database — the built-in app one, or the app's own database when it declares exactly one. Naming is only needed to pick between several, and the name is the one from the Databases list in the app's env-specific ikon-config toml, applied with ikon app config and surfaced via Databases. The built-in database is provisioned on demand: an app that never asks for one is never given one, so the first call may wait while it is created. A database the app declares itself is provisioned at activation and is already there.
    // databaseName: The database to connect to, or null for the app's default one.
    // throws ArgumentException: Thrown when a named database is not among the app's databases, or when no name was given and the app has several to choose from.
    virtual Task<DbConnection> DatabaseAsync(string? databaseName = null)
    // Provisions the app's built-in database if the space does not have one yet and returns its connection info, adding it to Databases. Concurrent callers share one provisioning attempt. DatabaseAsync calls this for you; call it directly only to pay the first-use cost somewhere other than the first query.
    // throws InvalidOperationException: Thrown when the database could not be provisioned.
    virtual Task<DatabaseConnectionInfo> EnsureDefaultDatabaseAsync()
    // Completes only when the persisted deletions have finished. Erasure is idempotent — erasing a user with no stored state is a no-op.
    // userId: The user whose persistent state to erase.
    virtual Task EraseUserStateAsync(string userId)
    // Build a shareable link to this app: PublicUrl plus a query string built from queryParams — an anonymous object (or a string dictionary), following the identity-by-anonymous-object shape of MintUrlAsync. Each readable property becomes a URL-encoded name=value pair; null-valued properties are skipped. So app.JoinUrl(new { id = sessionId }) yields {PublicUrl}?id={sessionId}. Replaces hand-assembling $"{app.ReactiveGlobalState.SpaceUrl.Value}?id={sessionId}". Passing null returns PublicUrl as-is.
    // queryParams: Anonymous object (e.g. new { id = sessionId, host = true }) or string dictionary whose entries become the query string. Null for no query string.
    virtual string JoinUrl(object? queryParams = null)
    // Identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), never by URL path — the path is what minting returns. Omitting identity (null) pins this instance's own session on an app endpoint so the URL routes back here, and pins nothing on a cell endpoint. Grants are non-expiring unless you pass expiresIn.
    // endpoint: Identifies the endpoint by its HANDLER, NOT by its URL path: pass the handler method name (e.g. nameof(GetDocument)) — or the full {Owner}_{Method} registry name when the bare name is ambiguous. Use nameof so a rename stays in sync. You never pass the path here (an endpoint's path is often derived from the method name, and may be templated) — the path is what minting RETURNS, built from this handler's EndpointInfo.PublicUrl.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // Mint working URLs for several endpoints sharing one pinned identity, in a single backend round-trip. Returns a map keyed by the endpoints you passed. See MintUrlAsync.
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
    // Revoke every URL minted under a shared group tag.
    virtual Task RevokeGroupAsync(string group, CancellationToken ct = default)
    // Revoke a single minted URL by its MintedUrl.GrantId.
    virtual Task RevokeUrlAsync(string grantId, CancellationToken ct = default)
    // Event fired when a client joins the session.
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    // Event fired when a client leaves the session.
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    // Event fired for each protocol message received from the server.
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    // Fires after app creation but before Main(). Do not subscribe from inside Main() — it has already fired by then and the handler will never run.
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    // Event fired before the plugin disconnects, allowing cleanup of resources.
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
    // At-least-once delivery — the handler must be idempotent. Throwing marks the erasure incomplete and it is redelivered on a later session start.
    event AsyncEventHandler<UserDataErasureEventArgs> UserDataErasureAsync
  // Convenience subscription helpers for the lifecycle events on IAppBase. The raw event handler shape is AsyncEventHandler<TEventArgs> which expects a single EventArgs parameter — LLM-generated code routinely reaches for app.StartingAsync += async () => ... (zero-arg) or async (sender, args) => ... (two-arg, .NET prior). Both fail to compile against the canonical one-arg delegate. These extension methods accept the LLM-natural shapes directly: app.OnStarting(async () => ...) wires the underlying event; app.OnClientJoined(async ctx => ...) passes the Context straight through so the handler doesn't need to remember to drill into the event-args wrapper.
  static class IAppEventExtensions
    // Subscribe to IAppBase.ClientJoinedAsync with a handler that receives the joining client's Context directly (SessionId, UserId, etc) — skipping the ClientJoinedEventArgs wrapper the raw event emits.
    static void OnClientJoined(this IAppBase app, Func<Context, Task> handler)
    // Subscribe to IAppBase.ClientJoinedAsync with a handler that receives both the joining client's Context AND its typed TClientParameters. Replaces the awkward app.Clients[ctx.SessionId]!.Parameters drill inside the handler body.
    static void OnClientJoined<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to IAppBase.ClientLeftAsync with a handler that receives the departing client's Context directly.
    static void OnClientLeft(this IAppBase app, Func<Context, Task> handler)
    // Subscribe to IAppBase.ClientLeftAsync with a handler that receives both the departing client's Context AND its typed TClientParameters.
    static void OnClientLeft<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to IAppBase.MessageReceivedAsync with a handler that receives the protocol message directly.
    static void OnMessageReceived(this IAppBase app, Func<ProtocolMessage, Task> handler)
    // Declare the app's dynamic public routes for build-time boot-snapshot capture (e.g. one route per store listing). The provider runs only in a snapshot-capture process; returned routes are unioned with the [BootSnapshot] Routes list from ikon-config.toml.
    static void OnSnapshotRoutes(this IAppBase app, Func<Task<IEnumerable<string>>> provider)
    // Subscribe to IAppBase.StartingAsync with a zero-arg async handler. The Starting event carries no data — there's nothing to forward.
    static void OnStarting(this IAppBase app, Func<Task> handler)
    // Subscribe to IAppBase.StoppingAsync with a zero-arg async handler.
    static void OnStopping(this IAppBase app, Func<Task> handler)
    // Subscribe to IAppBase.UserDataErasureAsync with a handler that receives the erased user's id directly. Clean APP-OWNED data here (own database tables, PII embedded in session/global values) — the platform has already erased the user's platform-managed state. Delivery is at-least-once, so the handler must be idempotent; throwing marks the erasure incomplete and it is redelivered on a later session start.
    static void OnUserDataErasure(this IAppBase app, Func<string, Task> handler)
  // Interface representing a connected client with typed parameters.
  interface IClient<out TClientParameters>
    // Gets the typed parameters for this client.
    TClientParameters Parameters { get; }
    // Gets the session id of this client — the same id used to index IClientCollection<TClientParameters> and to target client-directed APIs.
    int SessionId { get; }
  // Collection interface for accessing connected clients. Iterable for the common "broadcast / fan-out" pattern (`foreach (var client in app.Clients)`), indexable by session ID for direct lookups, and exposes Ids when only the connected-session-ids are needed.
  interface IClientCollection<out TClientParameters> : IEnumerable<IClient<TClientParameters>>
    // Gets the number of currently connected clients.
    int Count { get; }
    // Gets the connected client session IDs as an enumerable. Convenience for code that just needs the IDs without the full client objects — e.g. `foreach (var id in app.Clients.Ids) { _scores[id] = 0; }`.
    IEnumerable<int> Ids { get; }
    // Gets the client with the specified session ID, or null if not found.
    IClient<TClientParameters>? this[int clientSessionId] { get; }
  // Marker interface for custom profile attribute classes. Implement this interface on classes that define custom profile attributes.
  interface IProfileAttributes
  // The two streaming members are shaped to plug straight into Ikon.AI: ListenAsync yields what ISpeechRecognizer.RecognizeContinuousSpeechAsync consumes, and SpeakAsync takes what ISpeechGenerator.GenerateSpeechAsync produces. So a conversational loop needs no adapter between them:
  // await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new("How can I help?")));
  //
  // await foreach (var heard in ai.SpeechRecognizer.RecognizeContinuousSpeechAsync(config, call.ListenAsync()))
  // {
  //     await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new(await Reply(heard))));
  // }
  // Sample rates are handled here: the provider's telephony audio and whatever rate the model wants are resampled to meet, so an app never has to know that 8 kHz exists.
  interface IVoiceCall : IAsyncDisposable
    // The provider's id for this call, the same one its webhooks carry.
    string CallId { get; }
    // Who is calling, in E.164. Empty on a call the app placed, where there is no such person.
    string From { get; }
    // Whether the call is still up.
    bool IsConnected { get; }
    // The other end of the call, in E.164: the number they dialled on an incoming call, and the number the app asked for on one it placed.
    string To { get; }
    // Ends the call.
    Task HangUpAsync(CancellationToken ct = default)
    // Drops audio already sent but not yet heard — what barge-in needs when the caller starts talking over the agent.
    Task InterruptAsync(CancellationToken ct = default)
    // The caller's audio as it arrives, at sampleRate. Ends when the call does.
    // sampleRate: What the consumer wants, typically the recognizer's rate.
    IAsyncEnumerable<float[]> ListenAsync(int sampleRate = 16000, CancellationToken ct = default)
    // Returns once every chunk has been sent, which is before the caller has finished hearing it — the provider buffers and plays at its own rate. Use WaitForPlaybackAsync to wait for the audio to actually land, and InterruptAsync to abandon it.
    Task SpeakAsync(IAsyncEnumerable<AudioChunk> audio, CancellationToken ct = default)
    // Completes once the caller has heard everything sent so far.
    Task WaitForPlaybackAsync(CancellationToken ct = default)
  // Sibling of HttpMethodAttribute: both declare an inbound HTTP endpoint over the shared addressing + identity model (see EndpointAttribute), differing only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients. Each tool is reachable two ways: through the owner's fixed JSON-RPC multiplexer ({owner}/mcp — tools/list + tools/call, and the only surface that streams notifications/progress over SSE), and as its own directly-callable POST endpoint whose body IS the tool's arguments object. That per-tool path defaults to the kebab-cased method name and is overridable via EndpointAttribute.Path — the override adjusts only this tool's own endpoint, never the shared multiplexer. The same method may also carry a verb-named REST attribute ([HttpPost] etc.); then that route serves the REST surface and the per-tool MCP endpoint is suppressed. The governance subject id is always the structural "{Type}.{Method}". The one place it parts company with its sibling is the default EndpointAttribute.Auth, which is EndpointAuth.User here rather than EndpointAuth.Grant. A grant is a signed URL handed to something the app provisioned, and an MCP client is the opposite of that: it arrives from outside, on behalf of a person, through a flow that ends in a token. Defaulting a tool to a credential no MCP client can obtain would make every tool either unreachable or, once someone widened it to get past that, wider than intended. Set Auth explicitly for a tool that really is reachable without a user.
  sealed class McpAttribute : EndpointAttribute
    // Declares an MCP tool whose own endpoint path is the kebab-cased method name.
    ctor()
    // Declares an MCP tool whose own directly-callable endpoint is served at path.
    ctor(string path)
    // Set this explicitly; the method's XML doc summary is never used as a fallback.
    string Description { get; init; }
    // MCP-wire tool name presented to clients in tools/list. Defaults to the method name when null or empty. The governance subject id is always "{Type}.{Method}" regardless of this.
    string? Name { get; init; }
    // Scopes narrow WITHIN an authorization; they do not replace it. A tool that names a scope must also be reachable — an EndpointAuth.User tool is the case this exists for, because only a token carries scopes at all. Naming one on a Public tool would be meaningless and is ignored. A caller whose token lacks the scope gets 403 with error="insufficient_scope", which is the one refusal an MCP client will re-authorize for. That is why it is a 403 and not a 401: a bare 401 says "who are you", and the client already knows.
    string Scope { get; init; }
  // Sibling of McpAttribute — same cell-method-as-callable model, different MCP verb shape: • Static resource — method takes no arguments; the URI is the literal UriTemplate with no placeholders. Lists in resources/list. • Dynamic resource — method takes parameters that map to {placeholder} segments in the URI template by name. Lists in resources/templates/list; the client crafts a concrete URI and reads it. Read-only by spec — authors should not put side effects in resource methods (the same governance hook still fires on every read with Operation = "resource", so policy authors can distinguish read access from tool dispatch).
  sealed class McpResourceAttribute : Attribute
    ctor(string uriTemplate)
    // Description shown to MCP clients so the agent (or user, via the client UI) can decide when to fetch the resource. Empty values pass through verbatim.
    string Description { get; init; }
    // MIME type advertised to clients. Defaults to text/plain for string returns and application/octet-stream for binary; override here to be more specific (text/markdown, application/json, image/png, etc.).
    string MimeType { get; init; }
    // Display name shown to MCP clients. Defaults to the method name when null or empty.
    string? Name { get; init; }
    // Required. Placeholder names must exactly match the cell method's parameter names.
    string UriTemplate { get; }
  // Event arguments for the IAppBase.MessageReceivedAsync event.
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    // Gets the received protocol message.
    ProtocolMessage Message { get; }
  // A minted endpoint URL: the working Url (the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended), the GrantId to revoke it by, and the optional ExpiresAt when a TTL was requested (grants are non-expiring by default).
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
  // The app's browser-history surface, reached through App.Navigation: reads and drives the URL of a connected client, and reports the navigations the client makes on its own. Navigation is per client, not per app: every path the app sets or reads belongs to one client session. The parameterless overloads act on the client of the ambient ClientScope — the client whose event, function call or reactive render is currently on the stack — so they must be called from a client-scoped context; the targetId overloads name the client session explicitly and work from anywhere (a background task, a timer, another client's handler). Paths under the platform-reserved prefixes /ikon and /api are rejected: the load balancer intercepts them before they ever reach the app, so navigating there would strand the client on a backend route. SetPathAsync throws ArgumentException rather than let that happen.
  class Navigation
    // The current URL path of the client in scope (query string stripped), or null outside a client scope or before any path is known. Tracked before the client's first frame renders, so route-dependent server UI can branch on it from the very first render — unlike state set from IAppBase joined handlers, which run on a background task and can lose the race against the first frame.
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
  // Event arguments raised when a client navigates to a different URL — either through the app (Navigation.SetPathAsync) or on its own (a link, the browser's back button, a manual reload).
  class NavigationPathChangedEventArgs : EventArgs
    // Creates the event arguments, splitting url into path and query
    // url: The URL the client navigated to, query string included
    // clientContext: The client that navigated
    ctor(string url, Context clientContext)
    // The client that navigated
    Context ClientContext { get; }
    // Session id of the client that navigated
    int ClientSessionId { get; }
    // The new path without its query string (e.g. /orders for /orders?id=7)
    string Path { get; }
    // The new URL as the client reported it, query string included
    string Url { get; }
    // Id of the user the navigating client is signed in as
    string UserId { get; }
  // Content of a user-facing notification surfaced on the client device (browser notification on the web, OS notification on Flutter native apps).
  sealed record NotificationContent
    // Title: Notification title. Required.
    // Body: Optional body text shown below the title.
    // IconUrl: Optional URL of an icon image shown with the notification.
    // Tag: Optional collapse key — a later notification with the same tag replaces an existing one instead of stacking.
    // LaunchUrl: Optional in-app path the client navigates to when the user taps the notification.
    // Data: Optional opaque JSON payload the app receives back when the user taps the notification.
    ctor(string Title, string? Body = null, string? IconUrl = null, string? Tag = null, string? LaunchUrl = null, string? Data = null)
    // Optional body text shown below the title.
    string? Body { get; init; }
    // Optional opaque JSON payload the app receives back when the user taps the notification.
    string? Data { get; init; }
    // Optional URL of an icon image shown with the notification.
    string? IconUrl { get; init; }
    // Optional in-app path the client navigates to when the user taps the notification.
    string? LaunchUrl { get; init; }
    // Optional collapse key — a later notification with the same tag replaces an existing one instead of stacking.
    string? Tag { get; init; }
    // Notification title. Required.
    string Title { get; init; }
  // The notification permission state of a client, as reported by the browser / OS.
  enum NotificationPermission
    // The user has not yet been asked; permission will be requested on the first send.
    Default
    // The user granted permission; notifications are shown.
    Granted
    // The user denied permission; nothing is shown until they change it in their browser/OS.
    Denied
    // The client cannot show notifications (API unavailable, or the function is not registered).
    Unsupported
  // Outcome of sending a notification to a single client session.
  sealed record NotificationSendResult
    // SessionId: The target client session id.
    // Delivered: True when the client actually displayed the notification (permission granted).
    // Permission: The client's resulting permission state after the send attempt.
    ctor(int SessionId, bool Delivered, NotificationPermission Permission)
    // True when the client actually displayed the notification (permission granted).
    bool Delivered { get; init; }
    // The client's resulting permission state after the send attempt.
    NotificationPermission Permission { get; init; }
    // The target client session id.
    int SessionId { get; init; }
  // Accessed via app.Notifications. Client permission is requested lazily on the first actual send, not when the app opens. SendToUserAsync automatically falls back to offline OS push (Web Push / FCM) when the target user has no connected session.
  sealed class NotificationService
    // Shows a notification on all currently-connected client sessions. Returns one result per session.
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    // Reads a client's current notification permission state without sending anything.
    // sessionId: The target client session id.
    // ct: Optional cancellation token.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    // Shows a notification on a single connected client session. The client requests notification permission lazily (on this first send) before displaying. Returns the per-session delivery and permission outcome.
    // sessionId: The target client session id.
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // Returns one result per connected session for the user. An empty list means the user had no connected session and only offline push was attempted — it is not an error.
    // userId: The persistent user id to notify.
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = default)
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
  // A reactive value persisted per user, partitioned at runtime by UserScope. Each user sees their own value across all of their client sessions.
  class PersistentUserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // Erases one user's value: the in-memory value is dropped (the next read sees the initial value) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when Postgres read-through applies, so the erased value cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // Writes one user's value regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then write to it from anywhere.
    void SetFor(string userId, T value)
    // Atomically read-modify-writes one user's value, under that user's lock, regardless of which scope — if any — is active.
    void UpdateFor(string userId, Func<T, T> mutator)
    // Reads one user's value regardless of which scope — if any — is active.
    T ValueFor(string userId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // Erases one user's dictionary: the in-memory entries are dropped (the next read sees the initial entries) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when Postgres read-through applies, so the erased entries cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // Removes the entry for key from one user's dictionary regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, TKey key)
    // Adds or replaces one entry in one user's dictionary regardless of which scope — if any — is active. This is the background-task form of this[key] = value: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. One notification.
    void SetFor(string userId, TKey key, TValue value)
    // Atomically transforms one user's entries under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveDictionary<TKey, TValue>.Update.
    void UpdateFor(string userId, Action<Dictionary<TKey, TValue>> transform)
    // Reads one user's entries regardless of which scope — if any — is active.
    IReadOnlyDictionary<TKey, TValue> ValueFor(string userId)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveHashSet<T> : ReactiveHashSet<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // Adds item to one user's set regardless of which scope — if any — is active. This is the background-task form of Add(item): capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. Returns whether it was added. One notification either way.
    bool AddFor(string userId, T item)
    // Erases one user's set: the in-memory members are dropped (the next read sees the initial members) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when Postgres read-through applies, so the erased members cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // Removes item from one user's set regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, T item)
    // Atomically transforms one user's members under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveHashSet<T>.Update.
    void UpdateFor(string userId, Action<HashSet<T>> transform)
    // Reads one user's members regardless of which scope — if any — is active.
    IReadOnlyCollection<T> ValueFor(string userId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // Appends to one user's list regardless of which scope — if any — is active. This is the background-task form of ReactiveList<T>.Add: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. One notification.
    void AddFor(string userId, T item)
    // Erases one user's list: the in-memory items are dropped (the next read sees the initial items) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when Postgres read-through applies, so the erased items cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // Removes the first occurrence of item from one user's list regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, T item)
    // Atomically replaces one user's items under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveList<T>.Update.
    void UpdateFor(string userId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    // Reads one user's items regardless of which scope — if any — is active.
    IReadOnlyList<T> ValueFor(string userId)
  // Read-only view of a client's address.
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
  // Exception thrown when a required role is missing.
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
    // Connected-client capacity of one shard before the platform spills to the next one
    int MaxClientsPerShard { get; }
    // Cost ceiling on the shard family size; 0 (the default) means unlimited. When every allowed shard is at capacity, new connections still join the last shard over capacity — visitors are never turned away by sharding
    int MaxShards { get; set; }
  // Event arguments raised when a captured audio segment ended without producing a transcript.
  sealed class SpeechNotRecognizedEventArgs : EventArgs
    ctor(SpeechNotRecognizedReason reason, Context clientContext, string streamId, string? correlationId, Exception? error = null)
    // Client context of the speaker.
    Context ClientContext { get; }
    // Client session id of the speaker.
    int ClientSessionId { get; }
    // Correlation id of the originating CaptureButton (null for ad-hoc audio streams).
    string? CorrelationId { get; }
    // The failure when Reason is SpeechNotRecognizedReason.Error; otherwise null.
    Exception? Error { get; }
    // Why the segment produced no text.
    SpeechNotRecognizedReason Reason { get; }
    // Stream id from which the audio was captured.
    string StreamId { get; }
    // User id of the speaker.
    string UserId { get; }
  // Why a captured audio segment produced no transcript.
  enum SpeechNotRecognizedReason
    // The segment carried no audio — typically a press released before the microphone delivered a frame.
    NoAudio
    // The segment stayed below the configured silence threshold.
    Silence
    // The recognizer ran but returned no text.
    NoText
    // The recognizer failed; the failure is in SpeechNotRecognizedEventArgs.Error.
    Error
  // Event arguments raised when speech has been recognized from a captured audio stream.
  sealed class SpeechRecognizedEventArgs : EventArgs
    ctor(string text, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount, int turnId = 0)
    // Client context of the speaker.
    Context ClientContext { get; }
    // Client session id of the speaker.
    int ClientSessionId { get; }
    // Correlation id of the originating CaptureButton (null for ad-hoc audio streams).
    string? CorrelationId { get; }
    // Duration of the captured audio segment.
    TimeSpan Duration { get; }
    // Total sample count fed to the recognizer.
    int SampleCount { get; }
    // Stream id from which the audio was captured.
    string StreamId { get; }
    // Recognized speech text.
    string Text { get; }
    // Identifier of the detected turn when the recognition came from Audio.UseTurnDetection, shared with the matching TurnStartedEventArgs and TurnSpeculativeEventArgs. 0 for push-to-talk recognitions (Audio.UseSpeechRecognition).
    int TurnId { get; }
    // User id of the speaker.
    string UserId { get; }
  // Event arguments for the IAppBase.StartingAsync event.
  class StartingEventArgs : EventArgs
    ctor()
  // Event arguments for the IAppBase.StoppingAsync event.
  class StoppingEventArgs : EventArgs
    ctor()
  // Accessed via app.Telephony. The space needs a number first (ikon app telephony create --country se); until then every operation throws TelephonyNumberNotAvailableException, which names that command. A space may hold several numbers, in different markets and on different providers — omit from and the platform picks one, or name one to send as it. Sending is metered, so a space out of credits is suspended like any other overspend.
  sealed class TelephonyService
    // The binding outlives this process: it pins an identity, not an instance, so if this one is reaped the next message provisions a fresh instance with the same identity rather than being lost. That is what makes an app wake up when someone texts it. Running locally is the exception. There the binding also carries this machine's instance id, which is minted fresh on every run and cannot outlive it — so a local binding is reverted automatically when the app shuts down, rather than leaving the number pointed at a dead process. It applies to every number the space holds: one number cannot serve two identities, so an app wanting inbound per user needs a number per user.
    Task BindInboundToThisInstanceAsync(CancellationToken ct = default)
    // The same IVoiceCall an incoming call gives, so a conversation reads the same whichever end started it — and plugs into Ikon.AI the same way:
    // await using var call = await app.Telephony.CallAsync("+358401234567");
    // await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new("Your build finished")));
    // Returns only once the call is connected and audio can flow; it throws if nobody answers before ringTimeout. Dispose it — or call IVoiceCall.HangUpAsync — to end the call. The call is metered and bounded like any other: it counts against the space's concurrent-call limit, carries the platform duration cap, and is refused for a destination the platform does not allow.
    // from: Which of the app's numbers to call from. Omit to let the platform choose: the app's default number if it has one, else a number local to the destination's market, else the first it holds. Naming a number the app does not hold is refused rather than substituted.
    Task<IVoiceCall> CallAsync(string to, TimeSpan? ringTimeout = null, string? from = null, CancellationToken ct = default)
    // Worth reading when the app wants to choose a sender itself rather than let the platform pick one — to answer as the same number a user last saw, say. Most apps never need it: omitting from already sends from a number local to the recipient.
    Task<IReadOnlyList<TelephonyNumber>> GetNumbersAsync(CancellationToken ct = default)
    // Reports whether telephony is enabled for the app's space and which numbers it holds. Use it to decide whether to offer SMS or calling at all, rather than discovering it from a failed send.
    Task<TelephonyStatus> GetStatusAsync(CancellationToken ct = default)
    // The caller's audio reaches the handler as it is spoken and the app can speak back over the same call; see IVoiceCall for the conversational loop. Nothing else has to be configured. Calling this tells the platform that this app answers calls, which is when the provider side is wired up — so an app can start answering the phone without anyone touching a number, and a call that arrives while the app is not running starts it, exactly as an incoming message does.
    Task HandleCallsAsync(Func<IVoiceCall, Task> handler, CancellationToken ct = default)
    // Sends inbound back to the app's default shared instance, undoing BindInboundToThisInstanceAsync.
    Task ResetInboundAsync(CancellationToken ct = default)
    // Check SmsSendResult.Replyable on the result: when it is false the recipient received the message but cannot answer it, because the space holds no number local to their market and a foreign sender is stripped in transit. Long messages are split into billable segments; SmsSendResult.Parts reports how many were charged.
    // from: Which of the app's numbers to send as. Omit to let the platform choose: the app's default number if it has one, else a number local to the recipient's market — which is what keeps a message replyable — else the first it holds. Naming a number the app does not hold is refused rather than substituted, since sending as a different number reaches the recipient as a stranger.
    Task<SmsSendResult> SendSmsAsync(string to, string text, string? from = null, CancellationToken ct = default)
    // The app declares no webhook: the platform owns the endpoint the provider posts to and delivers the message here, so a message reaches whichever instance inbound is bound to — starting one if none is running. Reply by calling SendSmsAsync with SmsMessage.From. There is deliberately no "return a string to reply" shortcut: a reply the provider sends on our behalf is billed inside the provider, where nothing can meter it or refuse it for a space out of credit.
    event Func<SmsMessage, Task>? SmsReceived
  // The built-in client UI themes. The wire protocol carries the theme as a string (custom theme names are allowed via ClientFunctions.SetThemeAsync); ThemeExtensions.ToThemeName maps these values to their wire names.
  enum Theme
    Dark
    Light
  // Helpers for mapping Theme values to and from the wire strings used by the client.
  static class ThemeExtensions
    // True when the client's reported theme is the dark theme. False for the light theme, custom theme names, and clients that have not reported a theme.
    static bool IsDarkTheme(this Context clientContext)
    // Returns the wire name of the theme: "dark" or "light".
    static string ToThemeName(this Theme theme)
  // Event arguments raised when a turn has probably ended and its speculative transcript is ready (see Audio.UseTurnDetection). Start downstream work (e.g. generating a reply) with CancellationToken: it is cancelled if the user resumes speaking, and the matching SpeechRecognizedEventArgs (same TurnId) confirms the turn otherwise.
  sealed class TurnSpeculativeEventArgs : EventArgs
    ctor(int turnId, string text, TimeSpan duration, CancellationToken cancellationToken, string streamId, Context clientContext)
    // Cancelled if the user resumes speaking, invalidating this speculative transcript.
    CancellationToken CancellationToken { get; }
    // Client context of the speaker.
    Context ClientContext { get; }
    // Client session id of the speaker.
    int ClientSessionId { get; }
    // Duration of the audio the transcript was recognized from.
    TimeSpan Duration { get; }
    // Stream id the turn was detected on.
    string StreamId { get; }
    // Speculative transcript of the turn so far.
    string Text { get; }
    // Identifier of this turn, shared with the matching started and recognized events.
    int TurnId { get; }
    // User id of the speaker.
    string UserId { get; }
  // Event arguments raised when a user starts a speech turn on a turn-detected stream (see Audio.UseTurnDetection). Useful as a barge-in or listening-indicator hook.
  sealed class TurnStartedEventArgs : EventArgs
    ctor(int turnId, string streamId, Context clientContext)
    // Client context of the speaker.
    Context ClientContext { get; }
    // Client session id of the speaker.
    int ClientSessionId { get; }
    // Stream id the turn was detected on.
    string StreamId { get; }
    // Identifier of this turn, shared with the matching speculative and recognized events.
    int TurnId { get; }
    // User id of the speaker.
    string UserId { get; }
  // Event arguments for the IAppBase.UserDataErasureAsync event.
  class UserDataErasureEventArgs : EventArgs
    ctor(string userId)
    // Gets the id of the user whose data must be erased.
    string UserId { get; }
  // Built-in user roles. Maps to role strings stored in profile.
  enum UserRole
    // Anonymous/unauthenticated user (maps to "anonymous" role)
    Guest
    // Regular authenticated user (maps to "user" role)
    User
    // Moderator with elevated permissions (maps to "moderator" role)
    Moderator
    // Administrator with full permissions (maps to "admin" role)
    Admin
  // Handles video streaming for apps. Outgoing frames are transmitted immediately — call SendFrameAsync once per frame, paced by the caller at the source framerate (typically by forwarding each incoming frame as it arrives).
  class Video
    ctor(IAppBase app)
    // Closes all video streams.
    ValueTask CloseAllAsync()
    // Closes a video stream and sends the stream end message.
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    // Gets information about an output stream if it exists.
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
    // Event raised when an incoming video stream begins
    event AsyncEventHandler<VideoInputStreamBeginEventArgs> VideoInputStreamBeginAsync
    // Event raised when an incoming video stream ends
    event AsyncEventHandler<VideoInputStreamEndEventArgs> VideoInputStreamEndAsync
  // Event arguments raised when an incoming video frame is received
  class VideoInputFrameEventArgs : EventArgs
    ctor(string streamId, Context clientContext, int trackId, byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Correlation identifier inherited from the originating VideoStreamBegin (e.g., set by a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Encoded video frame data
    byte[] Data { get; }
    // Frame duration in microseconds
    uint DurationInUs { get; }
    // Frame number in the sequence
    int FrameNumber { get; }
    // Whether this is a keyframe
    bool IsKey { get; }
    // Unique identifier for the video stream
    string StreamId { get; }
    // Timestamp in microseconds
    ulong TimestampInUs { get; }
    // Track id for the video stream
    int TrackId { get; }
    // User identifier
    string UserId { get; }
  // Event arguments raised when an incoming video stream begins
  class VideoInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, VideoCodec codec, string codecDetails, int width, int height, double framerate, Context clientContext, int trackId, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Video codec used for encoding
    VideoCodec Codec { get; }
    // Codec-specific details
    string CodecDetails { get; }
    // Optional correlation identifier set by the originator (e.g., a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Description of the video stream
    string Description { get; }
    // Video framerate
    double Framerate { get; }
    // Video height in pixels
    int Height { get; }
    // Source type of the video stream (e.g., "camera", "screen")
    string SourceType { get; }
    // Unique identifier for the video stream
    string StreamId { get; }
    // Track id for the video stream
    int TrackId { get; }
    // User identifier
    string UserId { get; }
    // Video width in pixels
    int Width { get; }
  // Event arguments raised when an incoming video stream ends
  class VideoInputStreamEndEventArgs : EventArgs
    ctor(string streamId, Context clientContext, int trackId, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Correlation identifier inherited from the originating VideoStreamBegin (e.g., set by a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Unique identifier for the video stream
    string StreamId { get; }
    // Track number for the video stream
    int TrackId { get; }
    // User identifier
    string UserId { get; }
  // Information about an output video stream
  record VideoOutputStreamInfo
    ctor(string StreamId, int TrackId, VideoCodec Codec, int Width, int Height, double Framerate)
    VideoCodec Codec { get; init; }
    double Framerate { get; init; }
    int Height { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
    int Width { get; init; }

namespace Ikon.App.Cells
  // A cell is always shared by its SessionIdentity: every caller that Cells.Connects with the same identity reaches the same instance and its Reactive<T> state — the identity IS the sharing scope (parameterless = one global; keyed = one per key). The runtime picks the transport: a local run hosts every cell in-process (a direct object); in the cloud the cell lives in its own cell-host and callers reach it through a proxy ([HttpGet]/[HttpPost] over HTTP, [Function] methods and Reactive<T> members over an SDK connection). App authors never choose or think about placement — they declare [Cell] and a SessionIdentity, and get exactly what those mean.
  sealed class CellAttribute : Attribute
    ctor()
    // Number of concurrent instances per addressable key. Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin CellHost.Resolve<TInterface> across them. For globals (parameterless SessionIdentity) the N instances are eager-spawned at host construction — the load-balanced auth-cell pattern. For keyed cells the N instances are spawned together on first access; sharded keyed cells must tolerate eventual consistency between shards (cells should hold no per-instance state, or persist shared state through an external store).
    int Capacity { get; init; }
    // How long a keyed cell may remain idle before CellHost.EvictIdleAsync removes it from the directory. Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
  // Each in-process server runs in its own async-local scope, so Cells.Instance resolves to that server's own host and wiring. The framework calls Initialize once at startup; apps call Connect<TInterface> for each cell access.
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // On a CLOUD run, when TInterface is an interface backed by a [Cell] type, returns a SubstrateCellProxy<TInterface> that dispatches per member: [HttpGet]/[HttpPost] methods over stateless HTTP, [Function] methods and Reactive<T> members over a standard SDK connection to the cell-host. Otherwise — a concrete-type request, or ANY cell on a LOCAL run — returns the local cell instance from this server's CellHost. Local runs host every cell in-process (there is no deployed cell-host to proxy to, and a local run is a single process), so every cell behaves as a normal shared instance locally.
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    // Dispose every live cell-host connection. Call on app shutdown. Idempotent.
    ValueTask DisposeAsync()
    // Reserved key in an SDK connection's parameters that names the substrate cell type to route to. The cell's SessionIdentity-record fields ride alongside it. MUST stay in sync with the cloud's CELL_TYPE_PARAM in cell-routing.ts — that's what the backend's app-session start keys on to provision a cell-host session.
    const string CellTypeParam
  // Framework handle injected into a cell's primary constructor. Exposes the SessionIdentity the cell was instantiated for; future revisions add lifetime, config, etc.
  interface ICell<out TSessionIdentity>
    // The SessionIdentity record value this cell instance is keyed by.
    TSessionIdentity Identity { get; }

namespace Ikon.App.Cron
  // Per-invocation context for a CronAttribute handler currently executing. A cron handler may optionally accept one of these (and/or a CancellationToken) to learn when and why it fired; a parameterless handler is equally valid. AsyncLocal so handler code (and anything it calls) can read it without threading it through every method signature.
  sealed record CronContext
    ctor(DateTime FireTimeUtc, string Schedule)
    // The cron context for the invocation currently running on this async flow, or null.
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
    // Case-insensitive lookup of a request header. UNTRUSTED request input — read it for handler logic (e.g. endpoint signature verification), NEVER to derive the SessionIdentity. Identity is resolved upstream before the handler runs and is the only thing that picks the target instance; headers cannot move it. Returns null when the header is absent. The accessor is case-insensitive because HTTP header names are, and the two dispatch paths build the header dictionary with different comparers.
    string? Header(string name)
    static IDisposable Use(HttpCallContext context)

namespace Ikon.App.Mcp
  // Per-request context for an MCP tools/call or resources/read in flight. AsyncLocal so the bridge can read it from inside parameter binding without threading another argument through every call site. Carries: • The request's effective CancellationToken (linked to the transport CT and a per-request CTS the host can trip on notifications/cancelled). • An optional progress sink the bridge wires IProgress<T> parameters into. • SessionIdentityFields — the authenticated identity for this request (from claims merged by the transport). Bridges pass it to CellHost.ResolveByCellTypeName so keyed cells route to the right instance. Empty / null on the stdio path (single-user process).
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
  // How a PaymentEntitlement was obtained.
  enum EntitlementSource
    Unknown
    Subscription
    OneTime
  // The price for a created offer. Omit Interval for a one-time offer.
  sealed record OfferPriceSpec
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval? Interval = null, int? IntervalCount = null)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval? Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  // Defines an offer to create via PaymentsService.CreateOfferAsync.
  sealed record OfferSpec
    ctor(string OfferId, string Name, OfferPriceSpec Price)
    string Name { get; init; }
    string OfferId { get; init; }
    OfferPriceSpec Price { get; init; }
  // A single payment record (a one-off charge or a subscription renewal). OfferId is null for ad-hoc charges and records written before offer tracking.
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
  // A normalized payment event the backend pushes to the app.
  sealed record PaymentEvent
    ctor(string EventId, PaymentProvider? Provider, PaymentEventType? Type, DateTimeOffset? OccurredAt, long Sequence, string PayloadJson)
    string EventId { get; init; }
    DateTimeOffset? OccurredAt { get; init; }
    string PayloadJson { get; init; }
    PaymentProvider? Provider { get; init; }
    long Sequence { get; init; }
    PaymentEventType? Type { get; init; }
    // The normalized projection as a JSON element.
    JsonElement Payload()
  // The kind of a normalized PaymentEvent.
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
  // What a Payment paid for — a one-off charge or a subscription charge.
  enum PaymentKind
    Unknown
    OneTime
    Subscription
  // A provider-hosted page the customer is redirected to in order to pay. Send them to Url.
  sealed record PaymentLink
    ctor(string Url, string Reference, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    string Reference { get; init; }
    string Url { get; init; }
  // A purchasable offer in the app's catalog — recurring (subscription) or one-time, per its prices.
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
  // The payment provider that moves the money. A command uses the space's enabled provider unless it names one, either per call or by pinning PaymentsService.DefaultProvider.
  enum PaymentProvider
    Stripe
    Mollie
    Surfboard
  // A receipt for a completed payment. Url is a provider-hosted receipt page. Pdf holds downloadable PDF bytes only when the provider exposes one; today every provider (Stripe, Surfboard) returns a hosted URL only, so Pdf is null — the field is populated when a provider offers a PDF.
  sealed record PaymentReceipt
    ctor(string? Url, byte[]? Pdf, string? PdfContentType)
    byte[]? Pdf { get; init; }
    string? PdfContentType { get; init; }
    string? Url { get; init; }
  // Result of a PaymentsService.ReconcileAsync request. Enqueued counts the provider objects queued for re-processing; their effects arrive asynchronously as normal payment events.
  sealed record PaymentReconcileResult
    ctor(PaymentProvider? Provider, int Enqueued)
    int Enqueued { get; init; }
    PaymentProvider? Provider { get; init; }
  // Result of a refund.
  sealed record PaymentRefund
    ctor(string Reference, RefundStatus Status)
    string Reference { get; init; }
    RefundStatus Status { get; init; }
  // The outcome of a Payment.
  enum PaymentStatus
    Unknown
    Pending
    Paid
    Failed
    Canceled
  // A customer's live subscription, created by paying for a recurring offer.
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
    // Offer the entitlement is keyed to.
    string OfferId { get; }
    override IFunctionPolicy CreatePolicy()
  // Reached via app.Payments; one instance per app. Every command takes an optional per-call provider; with none given it uses DefaultProvider or, failing that, the space's enabled provider. The service holds no payment state — every read hits the backend except the synchronous IsEntitled.
  sealed class PaymentsService : AsyncLocalInstance<PaymentsService>
    ctor()
    // Off by default: a payment link for a guest throws InvalidOperationException, because the guest's device-scoped user id changes when they sign in, orphaning the payment and its entitlement. Enable only for purchases that may stay behind (e.g. anonymous tips).
    bool AllowAnonymousPayments { get; set; }
    // Default cancel URL used when a command does not specify one.
    string? DefaultCancelUrl { get; set; }
    // Leave null (the default) so each command uses the space's enabled provider; set it only to pin one provider for an app with several enabled. A per-call provider argument overrides it.
    PaymentProvider? DefaultProvider { get; set; }
    // Default success URL used when a command does not specify one.
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
    // The app's catalog of purchasable offers.
    Task<IReadOnlyList<PaymentOffer>> ListOffersAsync(CancellationToken cancellationToken = default)
    // The customer's payments. customerKey defaults to the current user.
    Task<IReadOnlyList<Payment>> ListPaymentsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // The customer's subscriptions. customerKey defaults to the current user.
    Task<IReadOnlyList<PaymentSubscription>> ListSubscriptionsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // Recovery path for a missed provider webhook or an app that was offline. Eventually consistent: pulled objects surface as ordinary PaymentEventReceived pushes and entitlement refreshes. A reference (a payment link's checkout-session reference or a subscription id) scopes the pull to one object; otherwise the customer's recent objects, or the space's recent window when no customer is in scope.
    Task<PaymentReconcileResult> ReconcileAsync(string? customerKey = null, string? reference = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Refunds in full by default, or partially via amountMinor. A refund does NOT revoke an entitlement the original payment granted.
    Task<PaymentRefund> RefundAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Remove an offer from the app's catalog (Stripe archives the Product/Price). Returns false if no such active offer existed.
    Task<bool> RemoveOfferAsync(string offerId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Fetch a receipt for a completed payment. PaymentReceipt.Url is a provider-hosted receipt page (present for Stripe and Surfboard). PaymentReceipt.Pdf carries downloadable PDF bytes only when the provider offers one; today both providers return a hosted URL only, so it is null.
    Task<PaymentReceipt> RequestReceiptAsync(string paymentId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Valid only while the subscription is cancel-at-period-end and its paid period has not ended; an immediate cancel or a fully-ended subscription needs a new checkout. Returns a SubscriptionResume whose SubscriptionResume.SubscriptionId may differ from the input when the provider recreated the subscription (Mollie).
    Task<SubscriptionResume> ResumeSubscriptionAsync(string subscriptionId, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Raised for each normalized payment event the backend pushes (paid, refunded, subscription renewed/canceled). Subscribing registers the receiver on first use.
    event Func<PaymentEvent, Task>? PaymentEventReceived
  // The direction of a subscription plan change — to a pricier (Upgrade) or cheaper/equal (Downgrade) offer.
  enum PlanChangeDirection
    Unknown
    Upgrade
    Downgrade
  // The billing interval of a recurring price.
  enum PriceInterval
    Unknown
    Day
    Week
    Month
    Year
  // Whether a price bills once or on a recurring interval.
  enum PriceKind
    Unknown
    OneTime
    Recurring
  // The state of a PaymentRefund.
  enum RefundStatus
    Unknown
    Pending
    Succeeded
    Failed
  // Result of PaymentsService.ChangeSubscriptionOfferAsync. Changed is false when the subscription was already on the requested offer (a no-op). On an upgrade ProrationAmountMinor was charged immediately and the new plan is active now; on a downgrade nothing is charged and the new plan takes over at the next renewal (Effective is "immediate" or "next_cycle").
  sealed record SubscriptionOfferChange
    ctor(bool Changed, PlanChangeDirection? Direction, long ProrationAmountMinor, string? ProratedChargeRef, string? Currency, string? Effective, PaymentProvider? Provider)
    bool Changed { get; init; }
    string? Currency { get; init; }
    PlanChangeDirection? Direction { get; init; }
    string? Effective { get; init; }
    string? ProratedChargeRef { get; init; }
    long ProrationAmountMinor { get; init; }
    PaymentProvider? Provider { get; init; }
  // Result of PaymentsService.ResumeSubscriptionAsync. SubscriptionId is the subscription reference after resume — a new one when the provider recreated the subscription (Mollie).
  sealed record SubscriptionResume
    ctor(bool Resumed, string? SubscriptionId, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    bool Resumed { get; init; }
    string? SubscriptionId { get; init; }
  // The lifecycle state of a PaymentSubscription.
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
  // Thrown when a connector operation fails: usually the remote service returned an error response, but also when a caller-supplied precondition makes the call impossible to attempt.
  sealed class ConnectorException : Exception
    ctor(string provider, string message, int? statusCode = null)
    string Provider { get; }
    // HTTP status of the failed response, when the failure was an HTTP error. Lets a caller distinguish a permanent 401/403 (reconnect required) from a transient failure.
    int? StatusCode { get; }
  // A GitHub 403 may indicate a rate limit (check X-RateLimit-Remaining / Retry-After) rather than a permanent auth failure, so callers should not unconditionally treat a 403 as a dead credential. The retry loop reacts only to 429.
  sealed class GitHub
    ctor(string token, HttpClient? http = null)
    // Comment on an issue or pull request. Returns the comment's html_url.
    Task<string> CommentAsync(string repo, int number, string body, CancellationToken ct = default)
    Task<GitHubIssue> CreateIssueAsync(string repo, string title, string body, CancellationToken ct = default)
    Task<GitHubIssue> GetIssueAsync(string repo, int number, CancellationToken ct = default)
    // Unlike the connector's JSON calls, this does NOT retry on HTTP 429 (rate limit); a 429 surfaces a ConnectorException immediately. A GitHub 403 may itself indicate a rate limit (check X-RateLimit-Remaining / Retry-After) rather than a permanent auth failure, so do not unconditionally treat a 403 as a dead credential.
    Task<string> GetPullRequestDiffAsync(string repo, int number, CancellationToken ct = default)
    // The result may be silently truncated at maxPages with no signal. Detect this by comparing the result length against the page cap (maxPages × 100): if it reaches the cap, resume by calling again with since raised to the newest GitHubIssue.UpdatedAt returned. A GitHub 403 may indicate a rate limit (check X-RateLimit-Remaining / Retry-After) rather than a permanent auth failure, so do not unconditionally treat a 403 as a dead credential. since is INCLUSIVE (returns issues updated at-or-after it) while results are ordered by update time ascending, so resuming with since set to the last item's GitHubIssue.UpdatedAt re-returns every item updated in that same second. When resuming, dedupe on GitHubIssue.Number (unlike Slack's exclusive oldest).
    Task<IReadOnlyList<GitHubIssue>> ListIssuesSinceAsync(string repo, string since, int maxPages = 50, CancellationToken ct = default)
    // Unlike the connector's JSON calls, this does NOT retry on HTTP 429 (rate limit); a 429 surfaces a ConnectorException immediately.
    Task<GitHubMergeResult> MergePullRequestAsync(string repo, int number, string? commitTitle = null, CancellationToken ct = default)
  // One issue or pull request. UpdatedAt is the raw ISO-8601 timestamp exactly as GitHub returns it — callers that page by updated use it as an opaque ordered cursor, so reformatting it would break resume-from-cursor round-trips.
  sealed record GitHubIssue
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
  // Slack messaging connector. Post and read messages with a bot token (xoxb-...). Raw — no agent coupling; the agent skill lives in Ikon.Connectors.
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
    // Open a Socket Mode connection with an app-level token (xapp-...) and return the WebSocket URL.
    Task<string> OpenSocketUrlAsync(string appToken, CancellationToken ct = default)
    // Map one message object from a history page or a Socket Mode event to a SlackMessage. Returns null when the object has no ts (not a message).
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
    // Attachments on the message; empty (never null) when the message has none, so callers can iterate or read .Count without a null check.
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
  // Google Drive connector. Upload, download and list files with Google OAuth2 credentials. Raw — the agent skill is DriveSkill.
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
  // OAuth2 credentials for Google connectors. The refresh token is long-lived; the access token is obtained and refreshed automatically by the Google client library.
  sealed record GoogleCredentials
    ctor(string ClientId, string ClientSecret, string RefreshToken)
    string ClientId { get; init; }
    string ClientSecret { get; init; }
    string RefreshToken { get; init; }

# Ikon.Connectors.Browser Public API

namespace Ikon.Connectors.Browser
  // Builds the persona an app registers on its orchestrator to operate a browser.
  static class BrowserOperatorPersona
    static Persona Create(string name = "browser-operator", string? systemPrompt = null, LLMModel visionModel = Claude46Sonnet, Reasoning? reasoning = null)
    const string DefaultName
  // Owns the browser lifecycle: start once, dispose to release the process. Resolves a WebTarget by mark first, then accessibility role+name, then selector.
  sealed class BrowserSession : IAsyncDisposable
    ctor()
    // The last ~40 console messages / page errors / failed requests from the page — the page's own account of why it is in whatever state it is in. Diagnostic gold when a page that "should" render stays blank (auth failures, websocket errors, bundle errors).
    IReadOnlyList<string> ConsoleTail { get; }
    string CurrentUrl { get; }
    ValueTask DisposeAsync()
    // Evaluate a JavaScript function-expression (e.g. "() => { ...; return 'x'; }") on the current page and return its string result. For light page-state manipulation by non-agentic callers — e.g. the codegen visual gate flipping data-theme so it can screenshot both theme states of the same view.
    Task<string?> EvaluateAsync(string script)
    Task<WebActionResult> ExecuteAsync(WebAction action)
    Task<IReadOnlyList<MarkedElement>> MarkElementsAsync()
    Task NavigateAsync(string url)
    Task<byte[]> ScreenshotAsync()
    // Screenshot as JPEG at the given quality — for callers that put the image into an LLM context, where a PNG's 3-5x larger payload rides along for every later turn.
    Task<byte[]> ScreenshotJpegAsync(int quality = 70)
    // Call once; throws InvalidOperationException if already started (dispose first). captureGrade renders at a 1440×900 2× viewport for high-fidelity single-shot screenshots — leave false for interactive driving, where the larger payload is pure token cost.
    // headless: Run the browser without a visible window.
    // captureGrade: High-fidelity capture mode for single-shot visual grading: 1440×900 viewport at 2x device scale, so small text, hairline borders, and gradients survive to the vision model. Leave false for agentic driving sessions — their screenshots ride along in every later LLM turn, where the 4x pixel payload is pure token cost.
    Task StartAsync(bool headless, bool captureGrade = false, CancellationToken ct = default)
  // The browser operator's tools. Each action runs on the per-thread BrowserSession and returns a TEXT observation (URL + numbered interactable elements). Screenshots are saved as artifacts (references), never posted into the thread; look runs an on-demand vision pass on a referenced screenshot and returns a text description.
  sealed class BrowserSkill : Skill
    ctor(LLMModel visionModel = Claude46Sonnet)
    override string Instructions { get; }
    override string Name { get; }
    // Attaches a live BrowserSession to a thread so this skill's tools can operate it, for standalone use where the persona is registered directly on a custom orchestrator rather than driven through WebAgent.OperateAsync. Without this, every tool resolves no per-run state and returns "No active browser session." The caller owns the session's lifetime (starting, navigating, and disposing it); call DetachSession when the run ends. The step trace and named outputs the tools produce accumulate against the state registered here.
    // threadId: Id of the thread whose tool calls should operate the session.
    // session: The started browser session the tools act on.
    static void AttachSession(string threadId, BrowserSession session)
    // Removes the run state attached to a thread by AttachSession and returns the WebRun the tools produced (steps + named outputs), or null if none was attached or the run never reached a finish. Does not dispose the session — the caller owns it. Safe to call for a thread that has no attached session.
    // threadId: Id of the thread whose attached session should be released.
    static WebRun? DetachSession(string threadId)
    override IEnumerable<Tool> Tools()
  // An interactable element discovered on the page, tagged for this observation.
  sealed record MarkedElement
    ctor(int Mark, string Role, string Name, string Selector)
    int Mark { get; init; }
    string Name { get; init; }
    string Role { get; init; }
    string Selector { get; init; }
  // A single browser action. A tagged union so a flow serializes losslessly and replays exactly.
  abstract record WebAction
  // Click the element Target resolves to, then wait for the page to settle.
  sealed record WebAction.Click : WebAction
    ctor(WebTarget Target)
    WebTarget Target { get; init; }
  // Read the inner text of the element Target resolves to and record it under OutputName in the run's outputs.
  sealed record WebAction.Extract : WebAction
    ctor(WebTarget Target, string OutputName)
    string OutputName { get; init; }
    WebTarget Target { get; init; }
  // Fill the element Target resolves to with Text. Set Secret for credentials: the live fill uses the value, but step traces and distilled flows store RedactedText in its place, so a replay must re-supply the value through its input slot rather than reusing the captured one. Set InputName to mark the value as a flow input slot that a replay substitutes.
  sealed record WebAction.Fill : WebAction
    ctor(WebTarget Target, string Text, bool Secret = false, string? InputName = null)
    string? InputName { get; init; }
    bool Secret { get; init; }
    WebTarget Target { get; init; }
    string Text { get; init; }
    // Placeholder stored anywhere a secret value would otherwise be persisted — the step trace, the distilled flow JSON, logs. Never used for the live fill.
    const string RedactedText
  // Go to Url and wait for the page to settle.
  sealed record WebAction.Navigate : WebAction
    ctor(string Url)
    string Url { get; init; }
  // Press a keyboard key (e.g. "Enter", "Escape") on the focused element, then wait for the page to settle.
  sealed record WebAction.Press : WebAction
    ctor(string Key)
    string Key { get; init; }
  // Scroll the page by Dx/Dy pixels (mouse wheel).
  sealed record WebAction.Scroll : WebAction
    ctor(int Dx, int Dy)
    int Dx { get; init; }
    int Dy { get; init; }
  // The result of executing one WebAction: whether it succeeded, the selector that actually resolved the target, the text an Extract produced, and a caller-actionable diagnosis when it failed.
  sealed record WebActionResult
    ctor(bool Ok, string Selector, string? Extracted = null, string? Failure = null)
    string? Extracted { get; init; }
    string? Failure { get; init; }
    bool Ok { get; init; }
    string Selector { get; init; }
  // Operates a website as an agent subthread (B1: browser actions are tools, the agent self-drives via AgentCall.RunAsync<T>), distills a successful run into a replayable WebFlow, and replays flows deterministically. The persona named personaName must be registered on the orchestrator (see BrowserOperatorPersona.Create).
  static class WebAgent
    static WebFlow Distill(WebRun run, string? name = null)
    static Task<WebRun> OperateAsync(AgentThread parent, string url, string objective, WebAgentOptions? options = null, string personaName = "browser-operator", CancellationToken ct = default)
    static Task<WebReplay> ReplayAsync(WebFlow flow, IReadOnlyDictionary<string, string> inputs, bool headless = true, CancellationToken ct = default)
  sealed record WebAgentOptions
    ctor(int MaxSteps = 25, bool Headless = true)
    bool Headless { get; init; }
    int MaxSteps { get; init; }
  // A distilled, replayable integration: ordered steps with parameterized input slots.
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
  // The result of replaying a WebFlow.
  sealed record WebReplay
    // Healed: Reserved for self-healing replay, which is not yet implemented — this is currently always false, so do not branch on it expecting a meaningful value.
    ctor(bool Ok, IReadOnlyDictionary<string, string> Outputs, bool Healed)
    // Reserved for self-healing replay, which is not yet implemented — this is currently always false, so do not branch on it expecting a meaningful value.
    bool Healed { get; init; }
    bool Ok { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
  // The result of an operate run: outcome, summary, the action trace, and any extracted outputs. Looks counts visual inspections separately — they consume agent budget without appearing in the action trace, so budget analysis needs both numbers.
  sealed record WebRun
    ctor(WebOutcome Outcome, string Summary, IReadOnlyList<WebStep> Steps, IReadOnlyDictionary<string, string> Outputs, int Looks = 0)
    int Looks { get; init; }
    WebOutcome Outcome { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
    string Summary { get; init; }
  // One executed action, the selector that actually resolved it, and whether it succeeded. A secret Fill is stored with its value redacted at construction, so the trace — and everything derived from it (distilled flow JSON, logs) — never carries the credential.
  sealed record WebStep
    ctor(WebAction action, string resolvedSelector, bool ok)
    WebAction Action { get; init; }
    bool Ok { get; init; }
    string ResolvedSelector { get; init; }
  // How to locate an element. Resolution tries the perception mark id from the current observation first, then accessibility role + name, then a CSS/XPath selector — populate whichever are known, since the later ones are what let a replay still find the element once the marks have gone stale.
  sealed record WebTarget
    ctor(string? Role = null, string? Name = null, string? Selector = null, int? Mark = null)
    int? Mark { get; init; }
    string? Name { get; init; }
    string? Role { get; init; }
    string? Selector { get; init; }

# Ikon.Resonance Public API

namespace Ikon.Resonance
  // Tracks audio stream metrics including packet counts, inter-packet delays, jitter, and encoding times. Supports tracking metrics across multiple streams. When Enabled, an AudioMetricsReport is published to Reports once per UpdateIntervalSeconds while packets are being recorded.
  class AudioMetrics
    ctor()
    bool Enabled { get; set; }
    bool LogMetrics { get; set; }
    double UpdateIntervalSeconds { get; set; }
    // Records one packet for streamId. This is a no-op unless Enabled is set to true first — while disabled, nothing is tracked and Reports never yields, so a caller expecting reports must enable the collector before recording.
    void RecordPacket(string streamId, double encodingTimeMs)
    void Remove(string streamId)
    // The interval snapshots as an async stream. A single-consumer diagnostics stream: only the latest unread report is kept, and concurrent enumerations compete for reports.
    // cancellationToken: Ends the stream when cancelled.
    IAsyncEnumerable<AudioMetricsReport> Reports(CancellationToken cancellationToken = default)
    void Reset(string streamId)
    void ResetAll()
  // One interval snapshot of audio stream metrics published by AudioMetrics.
  sealed record AudioMetricsReport
    ctor(int StreamCount, double MinIpdMs, double AvgIpdMs, double MaxIpdMs, double JitterMs, double AvgEncodeTimeMs, double CpuUsagePercent)
    double AvgEncodeTimeMs { get; init; }
    double AvgIpdMs { get; init; }
    double CpuUsagePercent { get; init; }
    double JitterMs { get; init; }
    double MaxIpdMs { get; init; }
    double MinIpdMs { get; init; }
    int StreamCount { get; init; }
  // Provides methods for resampling audio between different sample rates and channel configurations. Supports mono and stereo audio using linear interpolation for sample rate conversion.
  static class AudioResampler
    // Calculates the number of output frames after resampling.
    // inputFrameCount: The number of input frames (samples per channel).
    // inputSampleRate: The input sample rate in Hz.
    // outputSampleRate: The desired output sample rate in Hz.
    static int CalculateResampledFrameCount(int inputFrameCount, int inputSampleRate, int outputSampleRate)
    // Converts audio between mono and stereo channel configurations. Stereo to mono averages both channels; mono to stereo duplicates the channel.
    // source: The source audio samples in interleaved format.
    // destination: The destination buffer for converted samples.
    // inputChannelCount: The number of input channels (1 or 2).
    // outputChannelCount: The number of output channels (1 or 2).
    static void ConvertChannels(ReadOnlySpan<float> source, Span<float> destination, int inputChannelCount, int outputChannelCount)
    // Determines whether the specified channel count is supported.
    // channelCount: The number of channels to check.
    static bool IsSupportedChannelCount(int channelCount)
    // Resamples audio from one sample rate and channel configuration to another using linear interpolation.
    // source: The source audio samples in interleaved format.
    // destination: The destination buffer for resampled samples.
    // inputSampleRate: The input sample rate in Hz.
    // outputSampleRate: The output sample rate in Hz.
    // inputChannelCount: The number of input channels (1 or 2).
    // outputChannelCount: The number of output channels (1 or 2).
    static int Resample(ReadOnlySpan<float> source, Span<float> destination, int inputSampleRate, int outputSampleRate, int inputChannelCount, int outputChannelCount)
    // The maximum number of audio channels supported (mono or stereo).
    const int MaxSupportedChannelCount = 2
  // Provides utility methods for measuring audio levels and converting audio samples between PCM 16-bit integer and 32-bit float formats.
  static class AudioUtils
    // Output bytes are little-endian; input is clamped to [-1, 1] first. output must be at least 2 * input.Length; throws ArgumentException otherwise.
    // input: The input buffer containing float samples.
    // output: The output buffer for raw bytes. Must be at least twice the length of input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input, Span<byte> output)
    // Converts 32-bit float samples to 16-bit PCM samples as raw bytes (little-endian). Float values are clamped to [-1.0, 1.0] before conversion.
    // input: The input buffer containing float samples.
    static byte[] ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input)
    // Input is clamped to [-1, 1] first. output must be at least input.Length; throws ArgumentException otherwise.
    // input: The input buffer containing float samples.
    // output: The output buffer for 16-bit PCM samples. Must be at least as long as input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input, Span<short> output)
    // Converts 32-bit float samples to 16-bit PCM samples. Float values are clamped to [-1.0, 1.0] before conversion.
    // input: The input buffer containing float samples.
    static short[] ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input)
    // Normalizes to [-1, 1]. output must be at least input.Length; throws ArgumentException otherwise. Returns the sample count.
    // input: The input buffer containing 16-bit PCM samples.
    // output: The output buffer for float samples. Must be at least as long as input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertPcm16ToFloat(ReadOnlySpan<short> input, Span<float> output)
    // Converts 16-bit PCM samples to 32-bit float samples normalized to the range [-1.0, 1.0].
    // input: The input buffer containing 16-bit PCM samples.
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<short> input)
    // Bytes are little-endian; input length must be a multiple of 2 and output at least input.Length / 2. Normalizes to [-1, 1].
    // input: The input buffer containing raw bytes representing 16-bit PCM samples (little-endian).
    // output: The output buffer for float samples. Must be at least half the length of input.
    // throws ArgumentException: Thrown when the input length is not a multiple of 2 or output buffer is too small.
    static int ConvertPcm16ToFloat(ReadOnlySpan<byte> input, Span<float> output)
    // Converts 16-bit PCM samples (as raw bytes) to 32-bit float samples normalized to the range [-1.0, 1.0].
    // input: The input buffer containing raw bytes representing 16-bit PCM samples (little-endian).
    // throws ArgumentException: Thrown when the input length is not a multiple of 2.
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<byte> input)
    // For input normalized to [-1, 1] the result is in [0, 1]. Returns 0 for an empty span; channel layout does not matter.
    // samples: The samples to measure. Channel layout is irrelevant; all samples contribute equally.
    static float Rms(ReadOnlySpan<float> samples)
  // Decides when to interrupt the agent's speech (barge-in): the caller must produce sustained speech for a few consecutive frames, and only after a short grace period from when the agent started speaking (so the first syllables / any echo don't false-trigger). Pure logic — unit tested.
  sealed class BargeInDetector
    ctor(int sustainedFrames = 3, double graceMs = 300.0)
    void Reset()
    bool ShouldInterrupt(bool isSpeech, bool agentSpeaking, double msSinceSpeakStart)
  // Crossfade curve type.
  enum CrossfadeCurve
    // Linear crossfade (amplitude-based). Can have a perceived dip in the middle.
    Linear
    // Equal power crossfade (power-based). Maintains constant perceived loudness. Uses sine/cosine curves: fadeOut = cos(t * π/2), fadeIn = sin(t * π/2)
    EqualPower
  // Fade transition mode when new speech interrupts current speech.
  enum FadeMode
    // Fade out completes before fade in starts.
    Sequential
    // Fade out and fade in happen simultaneously.
    Crossfade
  // One personalized output frame from a GroupAudioMixer: the participant it is addressed to plus their mixed audio.
  readonly struct GroupAudioFrame
    ctor(int participantId, PcmAudioFrame frame)
    // The mixed audio frame (all other participants' streams, excluding the participant's own).
    PcmAudioFrame Frame { get; }
    // The participant this mix is addressed to.
    int ParticipantId { get; }
    void Deconstruct(out int participantId, out PcmAudioFrame frame)
  // Server-side audio mixer for group voice scenarios (meetings, conferences, multiplayer). Mixes multiple participant audio streams together, producing a personalized output stream for each participant that contains all other participants' audio mixed together but excludes the participant's own audio. Each input stream is tagged with the id of the participant it belongs to (typically a client session id) to control the exclusion. Participants must be registered with AddParticipant before they can receive mixed output. Streams are added/removed independently via AddStream and RemoveStream. A participant continues to receive output (from other participants' streams) even when they have no active streams of their own. Uses power-preserving normalization (1/sqrt(N)) and tanh soft-clipping to prevent distortion when many participants speak simultaneously.
  sealed class GroupAudioMixer : IAsyncDisposable
    ctor(GroupAudioMixerConfig? config = null)
    // Registers a participant to receive personalized mixed audio output. The participant will receive a mix of all streams except those tagged with their own id.
    void AddParticipant(int participantId)
    // Registers an input audio stream and tags it with the owning participantId so that participant never hears their own audio. Re-adding a stream id that is already registered keeps its buffered audio; if the owning participantId differs (the id was reclaimed by a reconnecting participant) the ownership tag is updated so exclusion routing follows the new owner.
    void AddStream(string streamId, int participantId)
    ValueTask DisposeAsync()
    // Unregisters a participant. They will no longer receive mixed audio output.
    void RemoveParticipant(int participantId)
    // Unregisters an input stream and discards any samples still buffered for it. Removing an unknown stream id is a no-op.
    void RemoveStream(string streamId)
    // The personalized mixes as a stream of 20 ms frames, paced at best-effort real time. Each tick yields one GroupAudioFrame per registered participant — except a participant whose tick mix would contain only their own audio (e.g. a lone speaker), who is skipped for that tick. The caller owns the loop: run await foreach over the stream and forward each frame to its participant. Single consumer: a concurrent second enumeration throws, but once an enumeration ends (including by an exception unwinding the consumer's loop) the stream may be re-entered — this is how a pump recovers after a frame-handling failure. Buffer-reuse contract: the yielded frames alias a single reused sample buffer — consume the samples fully within the loop body and copy them if you need to store them beyond it. Cancelling cancellationToken (or disposing the mixer) ends the stream gracefully: each participant that received audio gets one final empty frame marked PcmAudioFrame.IsLast so downstream consumers can close their streams, then the enumeration completes without throwing.
    // cancellationToken: Ends the stream when cancelled.
    // throws InvalidOperationException: Thrown when the mixer is already streaming.
    IAsyncEnumerable<GroupAudioFrame> StreamAsync(CancellationToken cancellationToken = default)
    // Buffers interleaved samples for a registered input stream, resampling to the mixer's native 48 kHz stereo format when needed. When the stream's buffer is full the oldest samples are dropped to make room; writes to an unknown stream are dropped with a throttled warning (stream teardown races with in-flight frames, so this is not an error).
    // throws ArgumentException: channelCount is less than 1 or sampleRate is not positive.
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  // Configuration for the GroupAudioMixer. Immutable — the mixer captures the values at construction, so construct a new config (and mixer) instead of mutating a shared instance.
  sealed record GroupAudioMixerConfig
    ctor()
    // Maximum buffer size per stream in milliseconds.
    double MaxBufferSizeMs { get; init; }
  // One in-process frame of raw PCM audio: interleaved float samples plus stream identity and optional encoding options, analysis results, and target information. This is the middle of the three audio currencies. AudioChunk is producer audio flowing INTO a mixer (TTS output, synthesized samples), identified by its speech-event id. PcmAudioFrame is the paced PCM output flowing OUT of the mixers toward the Opus encoder, identified by its output stream id. The encoded result travels on the wire as the protocol type AudioFrame.
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
  // Filters silence from an audio chunk stream so that only speech reaches downstream consumers such as speech-to-text models (which tend to hallucinate on silent input). Uses asymmetric EMA for level tracking, an adaptive noise floor, and a circular pre-buffer to ensure speech onsets are never clipped. Designed for real-time audio at typical frame sizes (20 ms). Usage — push-based: call ProcessChunk per audio chunk, forward non-null results. Usage — stream-based: wrap an IAsyncEnumerable<T> source with FilterAsync.
  sealed class SilenceRemover
    // Creates a new SilenceRemover for the given audio format.
    // sampleRate: Sample rate of the incoming audio in Hz (e.g. 48000).
    // channelCount: Number of audio channels (e.g. 1 for mono).
    // config: Optional configuration. When null, sensible defaults tuned for voice-over-IP audio are used.
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    // Wraps an async audio source, yielding only chunks that contain speech. Silence is suppressed and speech onsets include look-back audio from the pre-buffer.
    // source: The async enumerable producing audio chunks.
    // sampleRate: Sample rate of the audio in Hz.
    // channelCount: Number of audio channels.
    // config: Optional silence remover configuration.
    // ct: Cancellation token.
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = default)
    // Processes a single audio chunk and determines whether it should be forwarded downstream. Returns the samples to forward (including pre-buffered onset audio when speech begins), or null if the chunk is silence that should be suppressed.
    // chunk: The audio samples to process. Expected to be interleaved float samples in [-1, 1].
    float[]? ProcessChunk(ReadOnlySpan<float> chunk)
    // Resets all internal state (EMA level, noise floor, pre-buffer, and state machine) to initial values. Call this when starting a new audio session on the same instance.
    void Reset()
  // Configuration for SilenceRemover. The silence remover uses asymmetric EMA (exponential moving average) to track audio level, an adaptive noise floor that adjusts to the environment, and a circular pre-buffer that preserves the onset of speech so words are never clipped. The speech threshold is computed as: noiseFloor * NoiseFloorMultiplier + NoiseFloorOffset. Immutable — the remover captures the values at construction, so construct a new config (and remover) instead of mutating a shared instance.
  sealed record SilenceRemoverConfig
    ctor()
    // EMA smoothing factor for rising audio levels (0..1). Higher values respond faster to speech onset.
    float AttackAlpha { get; init; }
    // Starting noise floor estimate before any audio has been analyzed.
    float InitialNoiseFloor { get; init; }
    // Upper bound for the adaptive noise floor. Prevents the speech threshold from rising too high in very noisy environments.
    float MaxNoiseFloor { get; init; }
    // How fast the noise floor adapts during silence (0..1). Keep low to prevent speech from contaminating the noise floor estimate.
    float NoiseFloorAlpha { get; init; }
    // Speech threshold multiplier above the noise floor. Higher values are less sensitive and produce fewer false triggers from background noise.
    float NoiseFloorMultiplier { get; init; }
    // Absolute offset added to the speech threshold to prevent it from reaching zero in digital silence. Ensures a minimum sensitivity level.
    float NoiseFloorOffset { get; init; }
    // Milliseconds of recent audio kept in the circular look-back buffer. This audio is emitted on speech onset to preserve word beginnings that would otherwise be clipped.
    int PreBufferMs { get; init; }
    // EMA smoothing factor for falling audio levels (0..1). Lower values decay slower, holding through natural pauses in speech.
    float ReleaseAlpha { get; init; }
    // Number of consecutive above-threshold chunks required to confirm speech onset. Filters transient clicks and noise bursts from triggering false speech detection.
    int SpeechOnsetChunks { get; init; }
    // Milliseconds of trailing audio to include after the last speech chunk. Allows natural word endings and brief pauses to pass through before returning to silence state.
    int TrailingSilenceMs { get; init; }
  // Simplified audio mixer for speech output with precise 20ms frame timing. Handles one speech event at a time with smooth crossfade transitions.
  sealed class SpeechMixer : IAsyncDisposable
    ctor(SpeechMixerConfig? config = null)
    // Encoder options to use for audio output.
    AudioEncoderOptions? EncoderOptions { get; set; }
    // Whether output is currently paused (a pending Pause fade-out counts once it completes).
    bool IsPaused { get; }
    // Stable identifier stamped on every output frame this mixer emits.
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
    // Crossfade curve type. EqualPower maintains constant perceived loudness.
    CrossfadeCurve CrossfadeCurve { get; init; }
    // Duration of silence padding after speech and effects end (in milliseconds). This prevents fadeout from triggering at natural speech endings.
    double EndPaddingMs { get; init; }
    // Duration of fade-in when speech starts (in milliseconds).
    double FadeInMs { get; init; }
    // Fade transition mode when new speech interrupts current speech. Sequential: fade out completes before fade in starts. Crossfade: fade out and fade in happen simultaneously.
    FadeMode FadeMode { get; init; }
    // Duration of fade-out when speech ends or is interrupted (in milliseconds).
    double FadeOutMs { get; init; }
    // Upper bound only; the queue grows on demand from a small size. Samples added beyond this bound are dropped with a throttled warning, never thrown.
    double MaxBufferSizeMs { get; init; }
    // Maximum padding duration in milliseconds for effect tails. Prevents infinite padding if effects never fully decay.
    double MaxPaddingTimeMs { get; init; }
    // RMS threshold below which effect tail padding stops. Default is 0.001 (~-60dB), meaning padding continues until output is essentially silent.
    double PaddingThreshold { get; init; }
  // Detects conversational turns in a continuous (open-mic) audio stream: speech onset, probable turn end (speculative), speech resumption, and confirmed turn end — the segmentation an always-listening voice app needs between "raw mic frames" and "transcribe and respond". Deterministic: time is counted in received samples, not wall-clock, so the same frame sequence always produces the same events. This assumes the source keeps delivering frames during silence (true for platform mic capture, which streams continuously while active). Usage — push-based: call Process per audio chunk and act on the returned event. Usage — stream-based: wrap an IAsyncEnumerable<T> source with DetectAsync.
  sealed class TurnDetector
    // Creates a new TurnDetector for the given audio format.
    // sampleRate: Sample rate of the incoming audio in Hz (e.g. 48000).
    // channelCount: Number of audio channels (e.g. 1 for mono).
    // config: Optional configuration. When null, defaults tuned for conversational voice are used.
    ctor(int sampleRate, int channelCount, TurnDetectorConfig? config = null)
    // Wraps an async audio source, yielding turn events as they occur. When the source completes, a still-open turn is flushed as a final TurnEventKind.TurnEnded event.
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
    // Resets all internal state to initial values. Call this when starting a new audio session on the same instance.
    void Reset()
  // Configuration for TurnDetector. Immutable — construct a new config (and detector) instead of mutating a shared instance.
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
  // A transition reported by TurnDetector. Samples carries the utterance audio for TurnEventKind.SpeculativeTurnEnd and TurnEventKind.TurnEnded (including pre-buffered onset audio) and is empty for the other kinds.
  readonly struct TurnEvent
    // Duration of Samples; zero when no audio is carried.
    TimeSpan Duration { get; }
    // The kind of transition.
    TurnEventKind Kind { get; }
    // Utterance samples (interleaved float PCM), or empty for events that carry no audio.
    float[] Samples { get; }
  // The kind of transition reported by TurnDetector.
  enum TurnEventKind
    // The user has produced sustained speech (at least TurnDetectorConfig.MinSpeechDuration).
    SpeechStarted
    // Silence has lasted TurnDetectorConfig.SpeculativeSilence — the turn has probably ended. Carries the utterance audio so far, so downstream work (transcription, a reply) can start early. Followed by either SpeechResumed (the guess was wrong) or TurnEnded.
    SpeculativeTurnEnd
    // Speech resumed after a SpeculativeTurnEnd — discard the speculative result.
    SpeechResumed
    // The turn has ended: silence lasted TurnDetectorConfig.TurnEndSilence (or the turn hit TurnDetectorConfig.MaxTurnDuration). Carries the complete utterance audio.
    TurnEnded
  // Creates WAV audio files in memory with support for 16-bit integer or 32-bit float sample formats. Samples are written incrementally and the WAV header is finalized when the file is accessed.
  class WavFile : IDisposable
    // Initializes a new WAV file builder with the specified audio parameters.
    // sampleRate: The sample rate in Hz (e.g., 44100, 48000).
    // channelCount: The number of audio channels (1 for mono, 2 for stereo).
    // sampleFormat: The sample format to use for the WAV file.
    ctor(int sampleRate, int channelCount, WavFile.SampleFormat sampleFormat)
    // Adds 16-bit integer audio samples to the WAV file.
    // samples: The interleaved audio samples to add.
    // throws InvalidOperationException: Thrown if the file has been finalized or the sample format is not Short.
    void AddSamples(ReadOnlySpan<short> samples)
    // Adds 32-bit float audio samples to the WAV file.
    // samples: The interleaved audio samples to add.
    // throws InvalidOperationException: Thrown if the file has been finalized or the sample format is not Float.
    void AddSamples(ReadOnlySpan<float> samples)
    // Gets the WAV file as a byte array. Finalizes the WAV header if not already done.
    byte[] AsArray()
    // Gets the WAV file as a fresh readable stream over a copy of the data. Finalizes the WAV header if not already done. The returned stream is independent of this WavFile, so it survives disposal of the builder and each call returns its own stream.
    Stream AsStream()
    // Releases the resources used by the WAV file builder.
    void Dispose()
    // Saves the WAV file to disk. Finalizes the WAV header if not already done.
    // filePath: The path where the WAV file will be saved.
    void SaveToFile(string filePath)
  // Specifies the sample format used in the WAV file.
  enum WavFile.SampleFormat
    // 16-bit signed integer PCM format.
    Short
    // 32-bit IEEE floating-point format.
    Float

namespace Ikon.Resonance.Analysis
  // Result of audio analysis containing shape set values.
  readonly struct AudioAnalysisResult
    ctor(uint setId, IReadOnlyList<float> values)
    // The shape set ID this result belongs to.
    uint SetId { get; }
    // The analysis values for this shape set. Analyzers may reuse the backing storage between frames — copy the values if you need them beyond the current frame.
    IReadOnlyList<float> Values { get; }
  // Declaration of a shape set with ID and shape names.
  readonly struct AudioShapeSetDeclaration
    ctor(uint setId, string name, IReadOnlyList<string> shapeNames)
    // Human-readable name for the shape set (e.g., "Viseme", "Sentiment").
    string Name { get; }
    // Unique identifier for this shape set.
    uint SetId { get; }
    // Names of each shape in the set, in order (e.g., ["MouthOpenY", "MouthForm"]).
    IReadOnlyList<string> ShapeNames { get; }
  // Factory interface for creating audio analyzer instances. Analyzers extract data from audio without modifying it.
  interface IAudioAnalyzer
    // Gets the shape set declaration for this analyzer. Called once when setting up the audio stream.
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    // Creates a stateful analyzer instance bound to the mixer's output format.
    // sampleRate: Mixer output sample rate.
    // channelCount: Mixer output channel count.
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)
  // Stateful audio analyzer that extracts data from audio buffers without modifying them.
  interface IAudioAnalyzerInstance
    // Analyzes the provided buffer and returns shape set values. The buffer is not modified.
    // buffer: The audio buffer to analyze (interleaved samples).
    AudioAnalysisResult Analyze(ReadOnlySpan<float> buffer)
    // Resets the analyzer internal state back to its initial values.
    void Reset()
  // Audio analyzer that performs FFT-based spectral analysis for viseme (lip sync) detection. Produces MouthOpenY (0-1) from RMS and MouthForm (-1 to +1) from spectral analysis.
  sealed class VisemeAnalyzer : IAudioAnalyzer
    ctor()
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)

namespace Ikon.Resonance.Effects
  // Low-fidelity effect that reduces both bit depth and sample rate.
  sealed class BitCrusherAudioEffect : IAudioEffect
    ctor()
    ctor(int bitDepth, int downsampleFactor, float mix)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Classic chorus with modulated delay that gently widens mono or stereo sources.
  sealed class ChorusAudioEffect : IAudioEffect
    ctor()
    ctor(float baseDelayMs, float depthMs, float rateHz, float mix, float stereoPhaseOffsetDegrees = 90f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Feedback delay that adds spacious echoes with gentle high-frequency damping.
  sealed class DelayAudioEffect : IAudioEffect
    ctor()
    ctor(float delayMs, float feedback, float mix, float feedbackDamping = 0.25f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Stateless definition of an audio effect that can create mixer-ready instances.
  interface IAudioEffect
    // Creates a stateful effect instance bound to the mixer's output format.
    // sampleRate: Mixer output sample rate.
    // channelCount: Mixer output channel count.
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Stateful audio effect that can mutate audio buffers in place.
  interface IAudioEffectInstance
    // Processes the provided buffer in place.
    // buffer: The audio buffer to transform.
    void Process(Span<float> buffer)
    // Resets the effect internal state back to its initial values.
    void Reset()
  // The parameterless constructor yields a natural small-room reverb (four delay lines, 120–320 ms). For the array constructor, the feedbacks/mixes/delayTimesMs/cutoffFrequencies arrays must all be the same length (one entry per delay line): delay time sets perceived room size, feedback (< 1.0) sets tail length, mix the wet blend, and cutoff damps highs inside the feedback loop.
  sealed class ReverbAudioEffect : IAudioEffect
    // Creates a reverb with default room parameters (small room).
    ctor()
    // Creates a reverb with simplified parameters for easy room modeling.
    // roomSize: Room size from 0 (tiny) to 1 (cathedral). Scales delay times.
    // decay: Reverb tail decay from 0 (short) to 1 (long). Scales feedback.
    // damping: High-frequency damping from 0 (bright) to 1 (dark/muffled).
    // mix: Wet/dry mix from 0 (dry) to 1 (fully wet).
    ctor(float roomSize, float decay, float damping, float mix)
    // Creates a reverb with full control over all delay line parameters.
    ctor(IReadOnlyList<float> feedbacks, IReadOnlyList<float> mixes, IReadOnlyList<float> delayTimesMs, IReadOnlyList<float> cutoffFrequencies)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Metallic robot voice using ring modulation and mild saturation.
  sealed class RobotVoiceAudioEffect : IAudioEffect
    ctor()
    ctor(float carrierFrequencyHz, float mix, float drive)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Soft saturation that adds harmonic richness while keeping peaks controlled.
  sealed class SaturationAudioEffect : IAudioEffect
    ctor()
    ctor(float drive, float mix)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Narrowband telephone-style filter with gentle saturation.
  sealed class TelephoneAudioEffect : IAudioEffect
    ctor()
    ctor(float lowCutHz, float highCutHz, float mix, float drive)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Amplitude modulation (tremolo) with optional stereo phase offset for movement.
  sealed class TremoloAudioEffect : IAudioEffect
    ctor()
    ctor(float rateHz, float depth, float mix, float stereoPhaseOffsetDegrees = 90f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
