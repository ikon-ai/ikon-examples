# Ikon.AI.Emergence Public API

namespace Ikon.AI.Emergence
  sealed class AgentScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    string? Role { get; set; }
    int? Seed { get; set; }
  sealed class BestOfOptions<T> : EmergeScope<T>
    ctor()
    Func<T, ScoreBreakdown?, string>? BuildCriticFeedback { get; set; }
    Action<CandidateScope<T>>? CandidateConfig { get; set; }
    int Count { get; set; }
    bool CriticMustImprove { get; set; }
    EmergeScope<T> CriticScope { get; }
    bool EnableCritic { get; set; }
    Func<T, EmergenceTrace, double>? Score { get; set; }
    void Candidate(Action<CandidateScope<T>> configure)
    void Critic(Action<EmergeScope<T>> configure)
  sealed class CandidateScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    int? Seed { get; set; }
  sealed class Completed<T> : EmergeEvent<T>, IEquatable<Completed<T>>
    ctor(T Result, KernelContext Context, EmergenceTrace Trace)
    KernelContext Context { get; init; }
    T Result { get; init; }
    EmergenceTrace Trace { get; init; }
  static class Emerge
    // One-shot LLM completion that returns the result string. The verbose form
    // var (reply, _) = await Emerge.Run<string>(
    //     LLMModel.Claude45Haiku, new KernelContext(),
    //     pass => pass.Command = command).FinalAsync(ct);
    // becomes
    // var reply = await Emerge.AskAsync(command, ct);
    // Uses Claude45Haiku by default — cheap+fast, the right choice for short transformations (chatbot replies, reformat-as-X, classify, summarize). Override the model via the other overload when the task warrants a stronger tier. Reach for the full Run when you need tools, multi-iteration agentic loops, a populated KernelContext , or fine pass tuning.
    static Task<string> AskAsync(string command, CancellationToken ct = default)
    // Like AskAsync but with an explicit model override.
    static Task<string> AskAsync(string command, LLMModel model, CancellationToken ct = default)
    // One-shot structured-output completion. Same shape as the string overload, but the model is asked for a JSON object matching T 's schema. Throws if the model returns nothing or invalid JSON.
    static Task<T> AskAsync<T>(string command, CancellationToken ct = default) where T : class
    // Like AskAsync but with an explicit model override.
    static Task<T> AskAsync<T>(string command, LLMModel model, CancellationToken ct = default) where T : class
    static IAsyncEnumerable<EmergeEvent<T>> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<TResult>> MapReduce<TChunk, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TChunk, TResult>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<TResult>> MapReduce<TChunk, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TChunk, TResult>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, ILLM llm, CancellationToken ct = default)
    // Like Run but creates a fresh KernelContext internally — the common case where the call carries no prior conversation. Paired with ResultAsync , the verbose form
    // var (result, _) = await Emerge.Run<Recipe>(
    //     LLMModel.Claude45Sonnet, new KernelContext(),
    //     pass => pass.Command = command, ct).FinalAsync(ct);
    // becomes
    // var result = await Emerge.Run<Recipe>(
    //     LLMModel.Claude45Sonnet,
    //     pass => pass.Command = command, ct).ResultAsync(ct);
    // Pass an explicit KernelContext via the other overloads when you seed the call with input (images, prior turns) or carry conversation history across calls.
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, Action<EmergePass<T>> configure, CancellationToken ct = default)
    // Like Run but with an explicit ILLM (e.g. a mock for testing).
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<T>> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, ILLM llm, CancellationToken ct = default)
  static class EmergeEventExtensions
    // Drains the stream and returns the completed result together with the updated KernelContext . Reach for this over ResultAsync when you need the context back (conversation continuity) or want to handle a null result yourself.
    static Task<ValueTuple<T, KernelContext>> FinalAsync<T>(IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Like FinalAsync but also returns the run's EmergenceTrace . Reach for this when you need telemetry (duration, token usage, tool-call history) alongside the result.
    static Task<ValueTuple<T, KernelContext, EmergenceTrace>> FinalWithTraceAsync<T>(IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Drains the stream and returns the completed result without the tuple ceremony. The verbose form
    // var (result, _) = await Emerge.Run<Recipe>(
    //     model, pass => pass.Command = command).FinalAsync(ct);
    // becomes
    // var result = await Emerge.Run<Recipe>(
    //     model, pass => pass.Command = command).ResultAsync(ct);
    // Never returns null — if the run completes without producing a result (where FinalAsync would hand back a null result), an EmergenceStoppedException is thrown. Reach for FinalAsync instead when you need the updated KernelContext back (conversation continuity) or want to handle a missing result yourself via a nullable result.
    static Task<T> ResultAsync<T>(IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
  abstract class EmergeEvent<T> : IEquatable<EmergeEvent<T>>
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
    LLMModel? Model { get; set; }
    bool? OptimizeContext { get; set; }
    // Names of tools the caller declares SIDE-EFFECT-FREE (pure read/lookup). The executor runs consecutive calls to these from one model turn CONCURRENTLY — measured on codegen, sequential guide/read batches dominated pass latency. Results are still recorded in the model's original order. Mutating tools stay out of this set and act as barriers.
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
  class EmergeResult
    ctor(object? result = null)
    object? Result { get; }
    bool SkipReprocessing { get; init; }
  abstract class EmergeScopeBase
    string? Command { get; set; }
    bool? IncludeJsonExample { get; set; }
    int? MaxIterations { get; set; }
    int? MaxOutputTokens { get; set; }
    int? MaxRetries { get; set; }
    int? MaxToolCalls { get; set; }
    TimeSpan? MaxWallTime { get; set; }
    LLMModel? Model { get; set; }
    bool? OptimizeContext { get; set; }
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
  class EmergeScope<T> : EmergeScopeBase
    ctor()
    bool CaseInsensitiveJson { get; set; }
    string JsonExample { get; }
    string JsonSchema { get; }
    bool UseJson { get; set; }
  sealed class EmergenceCallInfo
    ctor()
    long CacheCreationInputTokens { get; set; }
    long CachedInputTokens { get; set; }
    string CallId { get; init; }
    // Resolved context-window size for this call's model, or 0 when the model can't be resolved.
    int ContextWindowSize { get; init; }
    // Fraction of the model's context window currently consumed by input tokens (0.0–1.0). Returns 0 when context window is unknown. Read by the agent runtime to decide when to surface a budget-extension prompt or self-compact.
    double ContextWindowUtilization { get; }
    TimeSpan? Duration { get; set; }
    string? Error { get; set; }
    long InputTokens { get; set; }
    string Model { get; init; }
    long OutputTokens { get; set; }
    string Pattern { get; init; }
    string ResultType { get; init; }
    DateTime StartedAt { get; init; }
    string? StopReason { get; set; }
    bool? Success { get; set; }
    Dictionary<string, string> Tags { get; init; }
  enum EmergenceStatus
    Completed
    Stopped
    Failed
  class EmergenceStoppedException : Exception
    ctor(EmergenceStatus status, string? stopReason)
    ctor(EmergenceStatus status, string? stopReason, Exception innerException)
    EmergenceStatus Status { get; }
    string? StopReason { get; }
  sealed class EmergenceTrace : IEquatable<EmergenceTrace>
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
  class FoundSection
    ctor()
    string Content { get; set; }
    string NodeId { get; set; }
    int? Page { get; set; }
    string Path { get; set; }
    string Relevance { get; set; }
  static class KernelContextExtensions
    static IReadOnlyList<FunctionCall> GetFunctionCalls(KernelContext ctx, int take = 10)
    static IReadOnlyList<FunctionResultPart> GetFunctionResults(KernelContext ctx, int take = 10)
    static bool HasFunctionResults(KernelContext ctx)
  sealed class MapReduceOptions<TChunk, TResult> : EmergeScope<TResult>
    ctor()
    IReadOnlyList<object>? Chunks { get; set; }
    object? Input { get; set; }
    EmergeScope<TChunk> MapScope { get; }
    int MaxParallel { get; set; }
    EmergeScope<TResult> ReduceScope { get; }
    Func<object, IEnumerable<object>>? Split { get; set; }
    void Map(Action<EmergeScope<TChunk>> configure)
    void Reduce(Action<EmergeScope<TResult>> configure)
  // MCP (Model Context Protocol) client using Streamable HTTP transport. Connects to an MCP server, discovers tools, and proxies tool calls.
  sealed class McpClient : IDisposable
    ctor(string endpoint, Dictionary<string, string>? headers = null)
    IReadOnlyList<McpTool> Tools { get; }
    // Calls an MCP tool by name with the given JSON arguments.
    Task<string> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = default)
    // Calls an MCP tool and returns both content and pagination cursor. Pass a cursor from a previous response to fetch the next page.
    Task<McpToolResult> CallToolRawAsync(string name, JsonElement arguments, CancellationToken ct = default, string? cursor = null)
    // Initializes the MCP session and discovers available tools.
    Task ConnectAsync(CancellationToken ct = default)
    void Dispose()
  class McpTool : IEquatable<McpTool>
    ctor(string Name, string? Description, JsonElement? InputSchema)
    string? Description { get; init; }
    JsonElement? InputSchema { get; init; }
    string Name { get; init; }
  class McpToolResult : IEquatable<McpToolResult>
    ctor(string Content, string? NextCursor)
    string Content { get; init; }
    string? NextCursor { get; init; }
  sealed class ModelText<T> : EmergeEvent<T>, IEquatable<ModelText<T>>
    ctor(string Text)
    string Text { get; init; }
  class NavigationDecision
    ctor()
    bool Complete { get; set; }
    string Reasoning { get; set; }
  sealed class Progress<T> : EmergeEvent<T>, IEquatable<Progress<T>>
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
  sealed class Retry<T> : EmergeEvent<T>, IEquatable<Retry<T>>
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
  sealed class Stage<T> : EmergeEvent<T>, IEquatable<Stage<T>>
    ctor(string Name)
    string Name { get; init; }
  sealed class Stopped<T> : EmergeEvent<T>, IEquatable<Stopped<T>>
    ctor(KernelContext Context, string? Reason)
    KernelContext Context { get; init; }
    string? Reason { get; init; }
  sealed class TokenUpdate<T> : EmergeEvent<T>, IEquatable<TokenUpdate<T>>
    ctor(long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
  sealed class ToolCallPlanned<T> : EmergeEvent<T>, IEquatable<ToolCallPlanned<T>>
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  sealed class ToolCallResult<T> : EmergeEvent<T>, IEquatable<ToolCallResult<T>>
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
  class TreeSearchResult
    ctor()
    string ReasoningTrace { get; set; }
    List<FoundSection> Sections { get; set; }

namespace Ikon.AI.Emergence.Structured
  sealed class StructuredTagParser.ParsedBlock : IEquatable<StructuredTagParser.ParsedBlock>
    ctor(string TagName, string Content, int StartIndex, int EndIndex)
    string Content { get; init; }
    int EndIndex { get; init; }
    int StartIndex { get; init; }
    string TagName { get; init; }
  sealed class StructuredTagParser.ParsedResponse : IEquatable<StructuredTagParser.ParsedResponse>
    ctor(string PlainText, IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks)
    IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks { get; init; }
    string PlainText { get; init; }
  // Generic parser for structured XML-style tags in LLM responses. Handles case mismatches, partial tags, and various formatting variations.
  static class StructuredTagParser
    // Extract the content of a specific tag (first occurrence)
    static string? GetTagContent(string content, string tagName)
    // Check if content contains a specific tag
    static bool HasTag(string content, string tagName)
    // Parse content and extract structured blocks for the specified tag names
    static StructuredTagParser.ParsedResponse Parse(string content, params string[] tagNames)

namespace Ikon.AI.Emergence.Tree
  class ContentSection : IEquatable<ContentSection>
    ctor(string Title, string Content, int? Page = null)
    string Content { get; init; }
    int? Page { get; init; }
    string Title { get; init; }
  interface IContentReader
    abstract IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = default)
  class StringContentReader : IContentReader
    ctor(string content)
    IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = default)
  class TreeIndex
    ctor()
    ctor(TreeNode root)
    TreeNode Root { get; set; }
    static IAsyncEnumerable<EmergeEvent<TreeIndex>> BuildAsync(LLMModel model, string content, TreeIndexOptions? options = null, CancellationToken ct = default)
    static IAsyncEnumerable<EmergeEvent<TreeIndex>> BuildAsync(LLMModel model, IContentReader reader, TreeIndexOptions? options = null, CancellationToken ct = default)
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
