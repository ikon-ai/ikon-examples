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
    static Task<(T Result, KernelContext Context)> FinalAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Like FinalAsync but also returns the run's EmergenceTrace . Reach for this when you need telemetry (duration, token usage, tool-call history) alongside the result.
    static Task<(T Result, KernelContext Context, EmergenceTrace Trace)> FinalWithTraceAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
    // Drains the stream and returns the completed result without the tuple ceremony. The verbose form
    // var (result, _) = await Emerge.Run<Recipe>(
    //     model, pass => pass.Command = command).FinalAsync(ct);
    // becomes
    // var result = await Emerge.Run<Recipe>(
    //     model, pass => pass.Command = command).ResultAsync(ct);
    // Never returns null — if the run completes without producing a result (where FinalAsync would hand back a null result), an EmergenceStoppedException is thrown. Reach for FinalAsync instead when you need the updated KernelContext back (conversation continuity) or want to handle a missing result yourself via a nullable result.
    static Task<T> ResultAsync<T>(this IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = default)
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
    // Concrete model for this pass. Callers that sit above the agent layer (Ikon.Agent) usually don't set this directly — there a persona declares an abstract Reasoning (Capability × ModelFamily) and the agent runtime resolves it to the LLMModel placed here.
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
  // Optional wrapper a tool body can return to control how Emerge feeds the result back to the model. The executor unwraps Result into the tool-result message (so the model never sees the wrapper), and SkipReprocessing = true completes the run right after the current batch of tool calls instead of sending the results back for another model turn — for tools whose side effect IS the answer (e.g. a UI action the model triggered on the user's behalf); the run then completes with a default result. Plain return values behave as if wrapped with SkipReprocessing = false.
  sealed class EmergeResult
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
  // One tree section the navigator marked relevant, with the reason it gave.
  sealed class FoundSection : IEquatable<FoundSection>
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
  // The navigator's structured verdict at the end of a TreeSearch run.
  sealed class NavigationDecision : IEquatable<NavigationDecision>
    ctor(string Reasoning = "", bool Complete = false)
    bool Complete { get; init; }
    string Reasoning { get; init; }
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
  // Result of a TreeSearch run: the sections the navigator marked relevant, plus its final reasoning.
  sealed class TreeSearchResult : IEquatable<TreeSearchResult>
    ctor(List<FoundSection> Sections, string ReasoningTrace = "")
    string ReasoningTrace { get; init; }
    List<FoundSection> Sections { get; init; }

namespace Ikon.AI.Emergence.Structured
  // A parsed block from the content
  sealed class StructuredTagParser.ParsedBlock : IEquatable<StructuredTagParser.ParsedBlock>
    ctor(string TagName, string Content, int StartIndex, int EndIndex)
    string Content { get; init; }
    int EndIndex { get; init; }
    int StartIndex { get; init; }
    string TagName { get; init; }
  // Complete parsed response with plain text and extracted blocks
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
  enum GovernanceAction
    Allow
    Deny
    Escalate
    Obfuscate
    Delay
  // The pending AI operation presented to the hook. Operation discriminates surface ("ai_call", "tool", "ingest"); Subject is the thing being acted on (model name, tool name, corpus name); Args are call-specific parameters; Ctx carries host-supplied identity / mission / runtime context (mission_id, agent_id, thread_id, user, tenant, time, etc.).
  sealed class GovernanceCall : IEquatable<GovernanceCall>
    ctor(string Operation, string Subject, IReadOnlyDictionary<string, object?> Args, IReadOnlyDictionary<string, object?> Ctx)
    IReadOnlyDictionary<string, object?> Args { get; init; }
    IReadOnlyDictionary<string, object?> Ctx { get; init; }
    string Operation { get; init; }
    string Subject { get; init; }
  // What happened after the operation ran (or didn't). Hooks use this in AfterAsync to close out the audit record.
  sealed class GovernanceCallResult : IEquatable<GovernanceCallResult>
    ctor(bool Failed, string Outcome, string? ErrorMessage = null)
    string? ErrorMessage { get; init; }
    bool Failed { get; init; }
    string Outcome { get; init; }
  // Thrown by AI primitives when an active IGovernanceHook returns Deny . Carries the decision id so callers can correlate the failure to the audit record.
  sealed class GovernanceDeniedException : Exception
    ctor(string decisionId, string ruleId, string policyId, string reason)
    string DecisionId { get; }
    string PolicyId { get; }
    string Reason { get; }
    string RuleId { get; }
  // Thrown by AI primitives when an active hook returns Escalate . The host runtime is expected to catch this and route to the escalation target rather than retry — the operation is paused, not failed.
  sealed class GovernanceEscalatedException : Exception
    ctor(string decisionId, string target, string reason)
    string DecisionId { get; }
    string Reason { get; }
    string Target { get; }
  // Shared invocation wrapper used by every transport that gates a call through GovernanceScope . Builds the standard Before / Deny / Escalate / invoke / After flow once so HTTP, MCP, and any future transport stay symmetric — the only thing each transport supplies is the GovernanceCall shape and the inner invocation. With no hook active the wrap is a pass-through.
  static class GovernanceInvoker
    static Task<T> RunAsync<T>(GovernanceCall call, Func<Task<T>> invoke, CancellationToken ct = default)
  // What the hook decided. The host must honour Action : Allow → invoke the operationDeny → throw GovernanceDeniedException Escalate → suspend / route to Target Obfuscate → apply the named transformDelay → wait the named duration then proceed DecisionId is the audit identifier the host can attach to any subsequent telemetry tied to this operation.
  sealed class GovernanceOutcome : IEquatable<GovernanceOutcome>
    ctor(GovernanceAction Action, string DecisionId, string RuleId, string PolicyId, string Reason, string? Target = null)
    GovernanceAction Action { get; init; }
    string DecisionId { get; init; }
    string PolicyId { get; init; }
    string Reason { get; init; }
    string RuleId { get; init; }
    string? Target { get; init; }
  // AsyncLocal scope carrying the active IGovernanceHook for the duration of an AI-touched operation. Host code wraps work in using var _ = GovernanceScope.Use(hook);; downstream Ikon AI primitives read Current and apply the hook if present. The scope crosses await boundaries naturally; it does NOT cross Task.Run or manually-started threads. Capture the hook into a local before any fork if you need to.
  static class GovernanceScope
    static IGovernanceHook? Current { get; }
    static IDisposable Use(IGovernanceHook hook)
  // Single hook surface called by every AI-touched primitive in the Ikon platform — LLM calls (Emerge.Run<T>), agent tool dispatch (Ikon.Agent), data ingest steps — before they act. One contract, three surfaces. Host code activates a hook by entering a GovernanceScope ; downstream primitives read Current and consult the hook if it is set. The default — no scope active — is a no-op pass-through and the AI primitives behave exactly as they do without governance.
  interface IGovernanceHook
    abstract Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    abstract Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
  // Connecting to the Ikon server timed out or failed. TRANSIENT by nature — a network blip, a server restart, a flaky link — so it is retryable: the RPC layer retries with a forced reconnect, and one that exhausts those attempts still lands as retryable so Emerge's bounded retry (and a host's re-drive) get their shot. A single 15s blip killing a 40-minute codegen run (observed repeatedly on a flaky uplink) is exactly what this classification prevents.
  sealed class IkonServerConnectException : RetryableAIException
    ctor(string message)
    ctor(string message, Exception inner)
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
  // Default no-op hook. Allows every call, records nothing. Lets primitives treat the hook contract as non-nullable downstream.
  sealed class NullGovernanceHook : IGovernanceHook
    ctor()
    Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
    static NullGovernanceHook Instance
  class RegionNotSupportedException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class RetryableAIException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.Chat
  sealed class BasicChat : IAsyncDisposable
    ctor(AssetUri shaderUri)
    KernelContext BaseContext { get; set; }
    int MaxHistoryLength { get; set; }
    IReadOnlyList<MessageBlock> Messages { get; }
    void AddModelMessage(string text)
    void AddModelMessage(params object?[] parts)
    void AddUserMessage(string text)
    void AddUserMessage(params object?[] parts)
    void ClearMessages()
    void Continue()
    KernelContext CreateKernelContext()
    ValueTask DisposeAsync()
    IAsyncEnumerable<LLMEvent> GenerateAsync(IEnumerable<(string Key, object? Value)>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = default)
    Task<T> GenerateObjectAsync<T>(IEnumerable<(string Key, object? Value)>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = default) where T : new()
    Task<string> GenerateStringAsync(IEnumerable<(string Key, object? Value)>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = default)
    T? GetState<T>(string key)
    void SetState(string key, object? value)
    void StopProcessing()
    event EventHandler<string>? RenderedShader

namespace Ikon.AI.Classification
  sealed class ClassificationDetail : IEquatable<ClassificationDetail>
    ctor()
    ctor(ClassificationLabel label, string originalCategory, bool isFlagged, double score)
    bool IsFlagged { get; init; }
    ClassificationLabel Label { get; init; }
    string OriginalCategory { get; init; }
    double Score { get; init; }
  sealed class ClassificationInput : IEquatable<ClassificationInput>
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Text { get; init; }
    string Url { get; init; }
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
  static class ClassificationModelExtensions
    static string DisplayName(this ClassificationModel model)
  sealed class ClassificationResult : IEquatable<ClassificationResult>
    ctor()
    List<ClassificationDetail> Details { get; init; }
    bool IsFlagged { get; init; }
  class ClassificationResultException : NonRetryableAIException
    ctor(ClassificationResult classificationResult)
    ctor(ClassificationResult classificationResult, Exception inner)
    ClassificationResult ClassificationResult { get; }
  sealed class Classifier : IClassifier, IDisposable
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ClassificationModel model, IReadOnlyList<ModelRegion>? regions = null)
    TimeSpan Timeout { get; set; }
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, CancellationToken cancellationToken = default)
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, CancellationToken cancellationToken = default)
    Task<ClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken = default)
    // One-shot text moderation. The verbose form
    // using var classifier = new Classifier(ClassificationModel.OpenAIOmniModeration);
    // var result = await classifier.ClassifyAsync(text);
    // becomes
    // var result = await Classifier.ClassifyAsync(text);
    // Defaults to OpenAIOmniModeration (free to use, the standard moderation model). Override the model via the second parameter when the task warrants. Check result.IsFlagged and the per-label result.Details. Reach for the constructor + the instance ClassifyAsync overloads when you need to classify images or message parts ( ClassificationInput ), set a custom Timeout , or classify many inputs with the same classifier instance.
    static Task<ClassificationResult> ClassifyAsync(string text, ClassificationModel model = OpenAIOmniModeration, CancellationToken cancellationToken = default)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ClassificationModel model)
  class ClassifierException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  interface IClassifier : IDisposable
    // Maximum duration of a single classification request. Defaults to 10 seconds.
    TimeSpan Timeout { get; set; }
    abstract Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, CancellationToken cancellationToken = default)
    virtual Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, CancellationToken cancellationToken = default)
    virtual Task<ClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken = default)
  class NonRetryableClassifierException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.Database
  sealed class BigQueryDbConnection : DbConnection
    ctor(string projectId, string datasetId)
    string ConnectionString { get; set; }
    string DataSource { get; }
    string Database { get; }
    string ServerVersion { get; }
    ConnectionState State { get; }
    override void ChangeDatabase(string databaseName)
    override void Close()
    override DataTable GetSchema()
    override DataTable GetSchema(string collectionName)
    override DataTable GetSchema(string collectionName, string?[]? restrictionValues)
    override void Open()
  class DatabaseConnection.Config
    ctor()
    string? EnvVarPrefix { get; set; }
    DatabaseConnection.SpaceSecret? SpaceSecret { get; set; }
  // Configuration for database info extraction.
  class DatabaseInfoExtractor.Config
    ctor()
    // Regex patterns for column names to exclude (format: "schema.table.column").
    List<string>? ColumnExcludeRegex { get; set; }
    Dictionary<string, string> ColumnExtraInfo { get; set; }
    bool IncludeEmptyColumns { get; set; }
    int JsonSampleLengthLimit { get; set; }
    int JsonSampleRowLimit { get; set; }
    int NonTextSampleRowLimit { get; set; }
    // Exact schema names to include. If empty, defaults depend on database type (e.g., "public" for PostgreSQL).
    List<string>? Schemas { get; set; }
    // Regex patterns for table names to exclude.
    List<string>? TableExcludeRegex { get; set; }
    Dictionary<string, string> TableExtraInfo { get; set; }
    // Regex patterns for table names to include (format: "schema.table" or just "table").
    List<string>? TableIncludeRegex { get; set; }
    int TextSampleLengthLimit { get; set; }
    int TextSampleRowLimit { get; set; }
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
  // Creates database connections. Prefer the typed factory methods ( Trino , Postgres , Sqlite , BigQuery ) for app code — host, port, and catalog are not secrets, only the password is. Pass that password from app.Secrets:
  // DatabaseConnection.Trino(host: "trino.example.com", port: 443, catalog: "hive",
  //                      user: "ikon", password: app.Secrets["TRINO_PASSWORD"])
  // CreateAsync remains for shared pipelines that read all of host/port/user/password/etc. from environment variables or space secrets.
  class DatabaseConnection
    ctor()
    string BigQueryDataset { get; set; }
    string BigQueryProjectId { get; set; }
    DatabaseType DatabaseType { get; set; }
    DbConnection DbConnection { get; set; }
    static DatabaseConnection BigQuery(string projectId, string dataset)
    static Task<DatabaseConnection> CreateAsync(DatabaseConnection.Config config)
    static DatabaseConnection Postgres(string host, int port, string database, string user, string password)
    static DatabaseConnection Sqlite(string path)
    static DatabaseConnection Trino(string host, int port, string catalog, string user, string password)
  class DatabaseInfo
    ctor()
    DatabaseType DatabaseType { get; set; }
    List<string>? ExampleQuestions { get; set; }
    string? SqlCteCommand { get; set; }
    List<DatabaseTableInfo> Tables { get; set; }
  class DatabaseInfoExtractor
    ctor(DatabaseConnection databaseConnection)
    Task<DatabaseInfo> ExtractAsync(DatabaseInfoExtractor.Config config, CancellationToken cancellationToken)
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
  class DatabaseConnection.SpaceSecret
    ctor()
    string Prefix { get; set; }
    string SpaceId { get; set; }
  static class SqlValidator
    static void ValidateReadOnly(string sql, HashSet<string> allowedTables)

namespace Ikon.AI.DepthEstimation
  sealed class DepthEstimator : IDepthEstimator, IDisposable
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(DepthEstimatorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // One-shot depth estimation from raw image bytes. The verbose form
    // using var depthEstimator = new DepthEstimator(DepthEstimatorModel.DepthAnythingV2);
    // var result = await depthEstimator.EstimateDepthAsync(new DepthEstimatorConfig
    // {
    //     Image = new DepthEstimatorConfig.InputImage { Data = imageData, MimeType = mimeType }
    // });
    // becomes
    // var result = await DepthEstimator.EstimateAsync(imageData, "image/png");
    // Defaults to DepthAnythingV2 (cheap+fast). Override the model via the third parameter when the task warrants (Marigold is slower but higher quality). The depth map image is in result.Depth (.Data / .MimeType). Reach for the constructor + EstimateDepthAsync when the image is a URL instead of bytes, or when you need the Marigold tuning fields on DepthEstimatorConfig .
    static Task<DepthEstimatorResult> EstimateAsync(byte[] imageData, string mimeType, DepthEstimatorModel model = DepthAnythingV2, CancellationToken cancellationToken = default)
    Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(DepthEstimatorModel model)
  sealed class DepthEstimatorConfig : IEquatable<DepthEstimatorConfig>
    ctor()
    int? EnsembleSize { get; init; }
    DepthEstimatorConfig.InputImage Image { get; init; }
    int? NumInferenceSteps { get; init; }
    int? ProcessingResolution { get; init; }
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
  sealed class DepthEstimatorResult : IEquatable<DepthEstimatorResult>
    ctor()
    DepthEstimatorResult.OutputImage Depth { get; init; }
  interface IDepthEstimator : IDisposable
    abstract Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = default)
  sealed class DepthEstimatorConfig.InputImage : IEquatable<DepthEstimatorConfig.InputImage>
    ctor()
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  class NonRetryableDepthEstimatorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class DepthEstimatorResult.OutputImage : IEquatable<DepthEstimatorResult.OutputImage>
    ctor()
    byte[] Data { get; init; }
    int Height { get; init; }
    string MimeType { get; init; }
    int Width { get; init; }

namespace Ikon.AI.Embeddings
  enum EmbeddingEncoding
    Base64
    GzipBase64
  sealed class EmbeddingGenerator : IDisposable, IEmbeddingGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(EmbeddingModel model, IReadOnlyList<ModelRegion>? regions = null)
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    TimeSpan Timeout { get; set; }
    void Dispose()
    // One-shot embedding generation. The verbose form
    // using var embeddingGenerator = new EmbeddingGenerator(EmbeddingModel.OpenAI3Small);
    // var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(texts, EmbeddingType.Generic);
    // becomes
    // var embeddings = await EmbeddingGenerator.EmbedAsync(texts);
    // Defaults to OpenAI3Small (cheap+fast) and Generic . Override the model via the second parameter when the task warrants; pass an explicit EmbeddingType when embedding documents and queries for asymmetric retrieval. Returns one float[] vector per input, in input order. Reach for the constructor + GenerateEmbeddingsAsync when you need batching control (maxInputCount), a custom Timeout , or the generator's MaxInputCount / EmbeddingVectorSize properties.
    static Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingModel model = OpenAI3Small, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, CancellationToken cancellationToken = default)
    static EmbeddingGeneratorCapabilities GetCapabilities(EmbeddingModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(EmbeddingModel model)
  sealed class EmbeddingGeneratorCapabilities
    ctor()
    int EmbeddingVectorSize { get; init; }
    int MaxInputCount { get; init; }
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
  static class EmbeddingModelExtensions
    static string DisplayName(this EmbeddingModel model)
  enum EmbeddingType
    Generic
    Document
    Query
    Clustering
    Classification
  interface IEmbeddingGenerator : IDisposable
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    // Maximum duration of a single embedding request, scaled up internally with the batch size. Defaults to 10 seconds.
    TimeSpan Timeout { get; set; }
    abstract Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, CancellationToken cancellationToken = default)
  struct VectorMath.Neighbor
    ctor(int index, float distance)
    float Distance { get; }
    int Index { get; }
  class NonRetryableEmbeddingGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  static class VectorMath
    // Calculates the element-wise average embedding from a list of embeddings. Each embedding must be a float array of the same length.
    static float[] CalculateAverageEmbedding(IList<float[]> embeddings)
    // Calculates the cosine similarity between two vectors.
    static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Calculates the dot product of two vectors.
    static float CalculateDotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Calculates the Euclidean distance between two vectors.
    static float CalculateEuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // For each embedding in the list, finds the k nearest neighbors (using Euclidean distance).
    static List<List<VectorMath.Neighbor>> CalculateKNearestNeighbors(IList<float[]> embeddings, int k)
    // Calculates the magnitude (L2 norm) of a vector.
    static float GetMagnitude(ReadOnlySpan<float> vector)

namespace Ikon.AI.FileConversion
  sealed class ConvertedFile
    ctor()
    byte[] Data { get; init; }
    string Mimetype { get; init; }
    string Name { get; init; }
  sealed class FileConverter : IDisposable, IFileConverter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(FileConverterModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
    // One-shot PDF conversion from raw file bytes. The verbose form
    // using var fileConverter = new FileConverter(FileConverterModel.ConvertApi);
    // var pdf = await fileConverter.ConvertToPdfAsync(new FileConverterConfig { Data = data, FileName = fileName });
    // becomes
    // var pdf = await FileConverter.ConvertToPdfAsync(data, fileName);
    // Defaults to ConvertApi (the only conversion model). fileName must carry the source extension (e.g. report.docx) — it determines the input format. The converted PDF is in pdf.Data. Reach for the constructor + ConvertToPdfAsync when the source is a URL or AssetUri instead of bytes, or when you need a custom timeout.
    static Task<ConvertedFile> ConvertToPdfAsync(byte[] data, string fileName, FileConverterModel model = ConvertApi, CancellationToken cancellationToken = default)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(FileConverterModel model)
  sealed class FileConverterConfig
    ctor()
    AssetUri? AssetUri { get; set; }
    byte[]? Data { get; set; }
    string FileName { get; set; }
    TimeSpan Timeout { get; set; }
    string? Url { get; set; }
  class FileConverterException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum FileConverterModel
    ConvertApi
  static class FileConverterModelExtensions
    static string DisplayName(this FileConverterModel model)
  interface IFileConverter : IDisposable
    abstract Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
  class NonRetryableFileConverterException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.ImageGeneration
  interface IImageGenerator : IDisposable
    abstract Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = default)
  enum ImageBackground
    Auto
    Opaque
    Transparent
  sealed class ImageGenerator : IDisposable, IImageGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // One-shot image generation. The verbose form
    // using var generator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);
    // var results = await generator.GenerateImageAsync(new ImageGeneratorConfig { Prompt = prompt });
    // var image = results.FirstOrDefault();
    // becomes
    // var image = await ImageGenerator.GenerateAsync(prompt);
    // Defaults to Gemini25FlashImage (cheap+fast). Override the model via the second parameter when the task warrants. Never returns null — throws an ImageGeneratorException when generation fails or the model produces no results, so wrap in try/catch when the app should continue without the image. Reach for the constructor + GenerateImageAsync when you need batch generation, custom width/height, an ImageBackground override, input images, or any other ImageGeneratorConfig field beyond the prompt.
    static Task<ImageGeneratorResult> GenerateAsync(string prompt, ImageGeneratorModel model = Gemini25FlashImage, CancellationToken cancellationToken = default)
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageGeneratorModel model)
  sealed class ImageGeneratorConfig : IEquatable<ImageGeneratorConfig>
    ctor()
    ImageBackground Background { get; init; }
    int Count { get; init; }
    int Height { get; init; }
    string ImageSize { get; init; }
    List<InputImage> InputImages { get; init; }
    string NegativePrompt { get; init; }
    string Prompt { get; init; }
    ImageQuality Quality { get; init; }
    ImageResultDelivery ResultDelivery { get; init; }
    SafetyLevel SafetyLevel { get; init; }
    string SearchPrompt { get; init; }
    int Seed { get; init; }
    int Steps { get; init; }
    string Style { get; init; }
    TimeSpan Timeout { get; init; }
    bool UpsamplePrompt { get; init; }
    int Width { get; init; }
  class ImageGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum ImageGeneratorModel
    GptImage1Mini
    GptImage15
    GptImage2
    Imagen3
    Imagen4Fast
    Imagen4Standard
    Imagen4Ultra
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
  sealed class ImageGeneratorResult : IEquatable<ImageGeneratorResult>
    ctor()
    byte[] Data { get; init; }
    int Height { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
    int Width { get; init; }
  enum ImageQuality
    Auto
    Low
    Medium
    High
  enum ImageResultDelivery
    Data
    Url
  sealed class InputImage : IEquatable<InputImage>
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[] Data { get; init; }
    double? MaskDilution { get; init; }
    string MimeType { get; init; }
    double? Strength { get; init; }
    InputImageType Type { get; init; }
    string? Url { get; init; }
  enum InputImageType
    Normal
    Mask
  class NonRetryableImageGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SafetyLevel
    Level0
    Level1
    Level2
    Level3
    Level4
    Level5
    Level6

namespace Ikon.AI.ImageSegmentation
  sealed class ImageSegmenterConfig.BoxPrompt : IEquatable<ImageSegmenterConfig.BoxPrompt>
    ctor()
    int? ObjectId { get; init; }
    double XMax { get; init; }
    double XMin { get; init; }
    double YMax { get; init; }
    double YMin { get; init; }
  interface IImageSegmenter : IDisposable
    abstract Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed class ImageSegmenter : IDisposable, IImageSegmenter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageSegmenterModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageSegmenterModel model)
    // One-shot text-prompted segmentation from raw image bytes. The verbose form
    // using var segmenter = new ImageSegmenter(ImageSegmenterModel.Sam31);
    // var result = await segmenter.SegmentImageAsync(new ImageSegmenterConfig
    // {
    //     Image = new ImageSegmenterConfig.InputImage { Data = imageData, MimeType = mimeType },
    //     Prompt = prompt
    // });
    // becomes
    // var result = await ImageSegmenter.SegmentAsync(imageData, "image/png", "person");
    // Defaults to Sam31 (the latest SAM revision at the same price as SAM 3). Override the model via the fourth parameter when the task warrants. Each detected object is in result.Segments with its mask image, score, and bounding box. Reach for the constructor + SegmentImageAsync when the image is a URL instead of bytes, or when you need point/box prompts, multiple masks per object, or any other ImageSegmenterConfig field.
    static Task<ImageSegmenterResult> SegmentAsync(byte[] imageData, string mimeType, string prompt, ImageSegmenterModel model = Sam31, CancellationToken cancellationToken = default)
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed class ImageSegmenterConfig : IEquatable<ImageSegmenterConfig>
    ctor()
    List<ImageSegmenterConfig.BoxPrompt> BoxPrompts { get; init; }
    ImageSegmenterConfig.InputImage Image { get; init; }
    int MaxMasks { get; init; }
    List<ImageSegmenterConfig.PointPrompt> PointPrompts { get; init; }
    string? Prompt { get; init; }
    bool ReturnMultipleMasks { get; init; }
    TimeSpan Timeout { get; init; }
  class ImageSegmenterException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum ImageSegmenterModel
    Sam3
    Sam31
  static class ImageSegmenterModelExtensions
    static string DisplayName(this ImageSegmenterModel model)
  sealed class ImageSegmenterResult : IEquatable<ImageSegmenterResult>
    ctor()
    ImageSegmenterResult.OutputImage? Preview { get; init; }
    List<ImageSegmenterResult.Segment> Segments { get; init; }
  sealed class ImageSegmenterConfig.InputImage : IEquatable<ImageSegmenterConfig.InputImage>
    ctor()
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  class NonRetryableImageSegmenterException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class ImageSegmenterResult.OutputImage : IEquatable<ImageSegmenterResult.OutputImage>
    ctor()
    byte[] Data { get; init; }
    int Height { get; init; }
    string MimeType { get; init; }
    int Width { get; init; }
  sealed class ImageSegmenterConfig.PointPrompt : IEquatable<ImageSegmenterConfig.PointPrompt>
    ctor()
    bool IsBackground { get; init; }
    int? ObjectId { get; init; }
    double X { get; init; }
    double Y { get; init; }
  sealed class ImageSegmenterResult.Segment : IEquatable<ImageSegmenterResult.Segment>
    ctor()
    List<double> Box { get; init; }
    ImageSegmenterResult.OutputImage Mask { get; init; }
    double? Score { get; init; }

namespace Ikon.AI.Kernel
  static class AsyncEnumerableExtensions
    static Task<T1[]> AsArrayAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<T1> AsFirstAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<string> AsStringAsync(this IAsyncEnumerable<LLMEvent> source)
    static IAsyncEnumerable<LLMEvent> WithParsedTagsAsync(this IAsyncEnumerable<LLMEvent> source, List<string>? tagWhitelist = null, List<string>? tagBlacklist = null)
    static IAsyncEnumerable<LLMEvent> WithReasoningFromTagAsync(this IAsyncEnumerable<LLMEvent> source, string reasoningTagName)
    static IAsyncEnumerable<LLMEvent> WithThrottlingAsync(this IAsyncEnumerable<LLMEvent> source, int charsPerSecond, int charsPerUpdate, CancellationToken cancellationToken = default)
    static IAsyncEnumerable<LLMEvent> WithWindowedProcessingAsync(this IAsyncEnumerable<LLMEvent> source, Func<string, List<LLMEvent>, Task<(bool, List<LLMEvent>)>> processAsync, int windowSize = 0, int windowOverlap = 0)
  // An incremental chunk of generated output audio.
  sealed class LLMEvent.AudioDelta : LLMEvent, IEquatable<LLMEvent.AudioDelta>
    ctor(AudioChunk Audio)
    AudioChunk Audio { get; init; }
  // The provider-side id of the generated output audio, replayable as an AudioIdPart in a follow-up context.
  sealed class LLMEvent.AudioId : LLMEvent, IEquatable<LLMEvent.AudioId>
    ctor(string Id)
    string Id { get; init; }
  struct AudioIdPart : IMessagePart
    ctor(string id)
    string Id { get; }
    MessagePartType Type { get; }
  struct AudioPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  // The transcript of generated output audio.
  sealed class LLMEvent.AudioTranscript : LLMEvent, IEquatable<LLMEvent.AudioTranscript>
    ctor(string Transcript)
    string Transcript { get; init; }
  class BinaryDataContainer
    ctor(byte[] data, string mimeType)
    byte[] Data { get; }
    string MimeType { get; }
  // A citation reference detected in the generated text. The refer indices bound the text span that refers to the citation; PositionIndex is the character index of the citation marker itself.
  sealed class LLMEvent.Citation : LLMEvent, IEquatable<LLMEvent.Citation>
    ctor(string OriginalId, string MappedId, int ReferStartIndex, int ReferEndIndex, int PositionIndex)
    string MappedId { get; init; }
    string OriginalId { get; init; }
    int PositionIndex { get; init; }
    int ReferEndIndex { get; init; }
    int ReferStartIndex { get; init; }
  // Generation was stopped by a content-safety classifier.
  sealed class LLMEvent.ContentFiltered : LLMEvent, IEquatable<LLMEvent.ContentFiltered>
    ctor(ClassificationResult Classification)
    ClassificationResult Classification { get; init; }
  // The complete model message of a shader run (may differ from the text response), emitted once at the end.
  sealed class LLMEvent.FinalModelMessage : LLMEvent, IEquatable<LLMEvent.FinalModelMessage>
    ctor(string Text)
    string Text { get; init; }
  // The complete text response of a shader run, emitted once at the end.
  sealed class LLMEvent.FinalText : LLMEvent, IEquatable<LLMEvent.FinalText>
    ctor(string Text)
    string Text { get; init; }
  // The provider's finish reason for the generation (e.g. "stop", "max_tokens").
  sealed class LLMEvent.Finished : LLMEvent, IEquatable<LLMEvent.Finished>
    ctor(string Reason)
    string Reason { get; init; }
  class FunctionCall
    ctor(Function function, object?[] parameters, string parametersJson, string callId, string hash, string thoughtSignature = "", string reasoningContent = "")
    string CallId { get; }
    Function Function { get; }
    string Hash { get; }
    object?[] Parameters { get; }
    string ParametersJson { get; }
    string ReasoningContent { get; }
    string ThoughtSignature { get; }
  // Function/tool result carrying media alongside text. Providers that support media inside tool results (Anthropic tool_result image blocks) inline the media so the model actually SEES it; every other consumer degrades to ToString , which summarizes the media without dumping bytes.
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
  struct FunctionResultPart : IMessagePart
    ctor(FunctionCall functionCall, LLMEvent[] events, object result)
    LLMEvent[] Events { get; }
    FunctionCall FunctionCall { get; }
    object Result { get; }
    MessagePartType Type { get; }
  interface IMessagePart
    MessagePartType Type { get; }
  struct ImagePart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct ImageUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  struct Instruction
    ctor(InstructionType type, string content)
    string Content { get; }
    InstructionType Type { get; }
  enum InstructionType
    Context
    Command
  struct KernelContext : IEquatable<KernelContext>
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    // When set, providers that support server-side context editing (Anthropic context-management beta) clear OLD tool results once the request's input exceeds this many tokens — after prompt-cache lookup, so cached prefixes survive. The single biggest context sink in long tool-using loops is superseded tool results being re-sent every round; server-side clearing removes them without the cache-busting a client-side history rewrite causes. Null = off. Providers without support ignore it.
    int? ClearToolResultsAfterInputTokens { get; init; }
    // Tool names whose results are NEVER cleared by ClearToolResultsAfterInputTokens (semantic anchors like verdicts).
    IReadOnlyList<string>? ClearToolResultsExcludedTools { get; init; }
    bool DisableFunctionCalling { get; init; }
    bool DiscardTextOutputWithFunctionCalls { get; init; }
    // A fresh, blank `KernelContext` — equivalent to `new KernelContext()` or `default`. Provided as a named constant for code generated against frameworks that expect an `.Empty` affordance on context-like types.
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
    IAsyncEnumerable<LLMEvent> GenerateAsync(ILLM llm, CancellationToken cancellationToken = default)
    KernelContext KeepMessagesMax(int count)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
  // One event in the typed stream produced by GenerateAsync and its combinators. Consume the stream by switching on the concrete case: TextDelta for incremental text, ToolCallRequested when the model asks for a tool, ToolResult for a tool's output, Usage and Finished for end-of-generation accounting, and so on. Events not relevant to a consumer should be passed through unchanged so downstream consumers still see them.
  abstract class LLMEvent : IEquatable<LLMEvent>
    // Name of the pipeline stage that produced this event (e.g. "generate", "generate.reasoning", "Shader.Output.AfterPass"). Combinators re-tag events they transform so the origin of each event stays visible.
    string Source { get; init; }
  enum MediaResolution
    Default
    Low
    Medium
    High
    UltraHigh
  struct MessageBlock
    ctor(MessageBlockRole role, IMessagePart[] parts, string? userName = null)
    ctor(MessageBlockRole role, IEnumerable<IMessagePart> parts, string? userName = null)
    ctor(MessageBlockRole role, string message, string? userName = null)
    IMessagePart[] Parts { get; }
    MessageBlockRole Role { get; }
    string? UserName { get; }
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
  struct PdfPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct PdfUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  // The model's reasoning trace for this generation.
  sealed class LLMEvent.Reasoning : LLMEvent, IEquatable<LLMEvent.Reasoning>
    ctor(string Text)
    string Text { get; init; }
  enum ReasoningEffort
    None
    Minimal
    Low
    Medium
    High
  // Selects which JSON-schema dialect the generator emits. All Ikon-side schema shapes (primitives, arrays, dictionaries, polymorphism) are expressible in both dialects; the two differ in how they encode nullability and how strictly they police unknown keywords.
  enum SchemaDialect
    JsonSchema202012
    OpenApi30
  // A parsed XML-style tag extracted from the text stream by WithParsedTagsAsync .
  sealed class LLMEvent.Tag : LLMEvent, IEquatable<LLMEvent.Tag>
    ctor(string Name, string Content, IReadOnlyDictionary<string, string>? Attributes)
    IReadOnlyDictionary<string, string>? Attributes { get; init; }
    string Content { get; init; }
    string Name { get; init; }
  // An incremental chunk of generated text.
  sealed class LLMEvent.TextDelta : LLMEvent, IEquatable<LLMEvent.TextDelta>
    ctor(string Text)
    string Text { get; init; }
  struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  // The model requested a tool invocation.
  sealed class LLMEvent.ToolCallRequested : LLMEvent, IEquatable<LLMEvent.ToolCallRequested>
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  // The model's plan for upcoming tool calls (Cohere).
  sealed class LLMEvent.ToolPlan : LLMEvent, IEquatable<LLMEvent.ToolPlan>
    ctor(string Text)
    string Text { get; init; }
  // The output of an executed tool. Value holds the tool's return value; ValueType records its runtime type so the value can be rehydrated to the original type after a JSON round-trip (e.g. over RPC).
  sealed class LLMEvent.ToolResult : LLMEvent, IEquatable<LLMEvent.ToolResult>
    ctor(string functionName, object? value)
    ctor(string functionName, object? value, string? valueType)
    string FunctionName { get; }
    object? Value { get; }
    string? ValueType { get; }
  // Token accounting for one generation. CachedInputTokens is the subset of InputTokens served from the provider's prompt cache (Anthropic cache_read_input_tokens, OpenAI cached_tokens, Bedrock CacheReadInputTokens).
  sealed class LLMEvent.Usage : LLMEvent, IEquatable<LLMEvent.Usage>
    ctor(int InputTokens, int CachedInputTokens, int CacheCreationInputTokens, int OutputTokens)
    int CacheCreationInputTokens { get; init; }
    int CachedInputTokens { get; init; }
    int InputTokens { get; init; }
    int OutputTokens { get; init; }
  struct VideoAssetPart : IMessagePart
    ctor(AssetUri uri, string? mimeType = null, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string? MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    AssetUri Uri { get; }
  struct VideoPart : IMessagePart
    ctor(byte[] content, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    byte[] Content { get; }
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
  struct VideoUrlPart : IMessagePart
    ctor(string url, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    string Url { get; }

namespace Ikon.AI.LLM
  // Public seam over the provider-facing JSON schema generator. This is the exact projection every LLM provider applies when it ships a Function to the model (Anthropic input_schema, OpenAI parameters, …). Callers that need to display, persist, or compare "the schema the LLM will see" should use this instead of re-deriving their own — any drift between a home-grown projection and the wire is a bug this seam exists to prevent.
  static class FunctionSchema
    // Projects the function's parameter list into its provider JSON schema: an object schema with type/properties/required, including parameter descriptions and allowed-value enums.
    static string ToJson(Function function)
  interface ILLM : IDisposable, ILLMInfo
    abstract IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext context, CancellationToken cancellationToken = default)
  interface ILLMInfo
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
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
    bool SupportsStreaming { get; }
    bool SupportsZeroDataRetention { get; }
    bool UsesInlineReasoning { get; }
  sealed class LLM : IDisposable, ILLM, ILLMInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(LLMModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
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
    void Dispose()
    IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext context, CancellationToken cancellationToken = default)
    static LLMCapabilities GetCapabilities(LLMModel model)
    static LLMCapabilities GetCapabilities(LLMModel model, IReadOnlyList<ModelRegion>? regions)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(LLMModel model)
  sealed class LLMCapabilities : ILLMInfo
    ctor()
    int ContextWindowSize { get; init; }
    string InlineReasoningTagName { get; init; }
    SchemaDialect SchemaDialect { get; init; }
    bool SupportsGbnfGrammar { get; init; }
    // True when the provider binding can inline images INSIDE tool results (Anthropic tool_result image blocks). Distinct from SupportsInputImages : a vision model whose tool results are JSON-only (e.g. Gemini functionResponse) sees images in messages but not in tool results.
    bool SupportsImagesInToolResults { get; init; }
    bool SupportsInputAudio { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsInputPdf { get; init; }
    bool SupportsInputVideo { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsOutputAudio { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
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
    O3
    O3Pro
    Claude41Opus
    Claude45Haiku
    Claude45Sonnet
    Claude45Opus
    Claude46Opus
    Claude46Sonnet
    Claude47Opus
    Claude48Opus
    Claude5Sonnet
    Gemini25Flash
    Gemini25FlashLite
    Gemini25Pro
    Gemini3Flash
    Gemini31Pro
    Gemini31FlashLite
    Gemini35Flash
    Grok43
    Grok420Reasoning
    Grok420NonReasoning
    MistralSmall
    MistralMedium
    MistralLarge
    MagistralSmall
    MagistralMedium
    Codestral
    Devstral2
    VoxtralSmall
    CommandR
    CommandA
    CommandAReasoning
    CommandAPlus
    CommandAVision
    CommandR7B
    KimiK25
    KimiK26
    KimiK27Code
    Qwen36
    Qwen37
    Qwen37Max
    GptOss120B
    Glm5
    Glm51
    Glm52
    MiniMaxM25
    MiniMaxM27
    MiniMaxM3
    DeepSeekV32
    DeepSeekV4Pro
    DeepSeekV4Flash
    NovaPro
    NovaLite
    NovaMicro
    Nova2Lite
  static class LLMModelExtensions
    // Maximum input-context window for the model, in tokens (e.g. 200_000 for Claude 4.x base, 1_000_000 for the 1M-context tier). Returns 0 when the model can't be resolved — callers should treat 0 as "unknown" and skip utilization computation rather than dividing by zero.
    static int ContextWindowSize(this LLMModel model)
    static string DisplayName(this LLMModel model)
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
    abstract Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IMeshGeneratorInfo
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
  sealed class MeshGeneratorConfig.InputImage : IEquatable<MeshGeneratorConfig.InputImage>
    ctor()
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  sealed class MeshGenerator : IDisposable, IMeshGenerator, IMeshGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MeshGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
    void Dispose()
    // One-shot text-to-mesh. The verbose form
    // using var generator = new MeshGenerator(MeshGeneratorModel.Meshy6);
    // var result = await generator.GenerateMeshAsync(new MeshGeneratorConfig { Prompt = prompt });
    // becomes
    // var mesh = await MeshGenerator.GenerateAsync(prompt);
    // Defaults to Meshy6 (the current Meshy generation at the same per-credit price as Meshy 5). Override the model via the second parameter when the task warrants. Returns signed download URLs per format (.GlbUrl, .FbxUrl, …) that expire roughly three days after generation — download promptly. Reach for the constructor + GenerateMeshAsync when you need image-to-mesh (input images), PBR textures, polycount/topology control, or any other MeshGeneratorConfig field beyond the prompt.
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
  sealed class MeshGeneratorConfig : IEquatable<MeshGeneratorConfig>
    ctor()
    bool EnablePbr { get; init; }
    List<MeshGeneratorConfig.InputImage> InputImages { get; init; }
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
  // Result of a mesh generation. The URLs are signed and expire roughly three days after generation, so download the model files promptly.
  sealed class MeshGeneratorResult : IEquatable<MeshGeneratorResult>
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
    // Channel count of the PCM samples produced by GenerateMusicAsync .
    int ChannelCount { get; }
    // Sample rate of the PCM samples produced by GenerateMusicAsync .
    int SampleRate { get; }
    // Streams the generated music as PCM AudioChunk chunks as they are produced. Only supported when SupportsStreaming is true; other models throw a MusicGeneratorException . Use GenerateMusicFileAsync for a buffered, encoded audio file instead.
    abstract IAsyncEnumerable<AudioChunk> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    // Generates the music and returns it as a single buffered, encoded audio file. Supported by all models, including those that cannot stream.
    abstract Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IMusicGeneratorInfo
    // Whether DurationSeconds controls the length of the output. When false the model ignores it: it emits a fixed-length clip (e.g. Lyria 2 is always ~30s) or, for audio-to-audio editing, the output length follows the input clip.
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    // Whether the model can stream generated audio as it is produced via GenerateMusicAsync . Models without streaming support only expose the buffered GenerateMusicFileAsync result.
    bool SupportsStreaming { get; }
  // A reference clip fed into a prompt-driven music edit. The model preserves the timing and structure of this audio while the prompt re-styles it (timbre, instrumentation, mood). Mirrors the image-to-image InputImage shape used by the image generator.
  sealed class InputAudio : IEquatable<InputAudio>
    ctor()
    byte[] Data { get; init; }
    // End of the region to edit, in seconds. null means to the end.
    double? EndSeconds { get; init; }
    string MimeType { get; init; }
    // Start of the region to edit, in seconds. null means from the beginning.
    double? StartSeconds { get; init; }
    // How strongly the output should adhere to this reference, in [0, 1]. Higher keeps the original melody and timing closer. null defaults to strong adherence.
    double? Strength { get; init; }
  sealed class MusicGenerator : IDisposable, IMusicGenerator, IMusicGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MusicGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    bool SupportsStreaming { get; }
    void Dispose()
    // One-shot music generation. The verbose form
    // using var generator = new MusicGenerator(MusicGeneratorModel.ElevenLabsMusicV2);
    // var result = await generator.GenerateMusicFileAsync(new MusicGeneratorConfig { Prompt = prompt });
    // becomes
    // var music = await MusicGenerator.GenerateAsync(prompt);
    // Defaults to ElevenLabsMusicV2 (cheap+fast, supports duration control and editing). Override the model via the second parameter when the task warrants. Returns a buffered, encoded audio file (.AudioData / .ContentType / .DurationSeconds). Reach for the constructor + GenerateMusicFileAsync when you need a target duration, input audio (prompt-driven editing), seed, or any other MusicGeneratorConfig field beyond the prompt; use GenerateMusicAsync for streaming PCM chunks.
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
  // Configuration for prompt-driven music generation and editing. With an empty InputAudios the model generates from the prompt alone. With one or more InputAudios it performs audio-to-audio editing: the prompt re-styles the reference clips while their timing and structure are preserved.
  sealed class MusicGeneratorConfig : IEquatable<MusicGeneratorConfig>
    ctor()
    // Target length in seconds (clamped to the model's supported range). When editing, set this to the source clip's length so the output keeps the original timing.
    double? DurationSeconds { get; init; }
    bool ForceInstrumental { get; init; }
    List<InputAudio> InputAudios { get; init; }
    string Prompt { get; init; }
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
  static class MusicGeneratorModelExtensions
    static string DisplayName(this MusicGeneratorModel model)
  sealed class MusicGeneratorResult : IEquatable<MusicGeneratorResult>
    ctor()
    byte[] AudioData { get; init; }
    string ContentType { get; init; }
    double DurationSeconds { get; init; }
  class NonRetryableMusicGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.OCR
  enum DocumentType
    General
  interface IOCR : IDisposable, IOCRInfo
    abstract Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = default)
    abstract IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = default)
  interface IOCRInfo
    int MaxPagesSupported { get; }
  class NonRetryableOCRException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class OCR : IDisposable, IOCR, IOCRInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxPagesSupported { get; }
    // One-shot document OCR from raw file bytes (image or PDF). The verbose form
    // using var ocr = new OCR(OCRModel.AzureDocumentIntelligence);
    // var result = await ocr.AnalyzeDocumentAsync(new OCRConfig { Data = data });
    // becomes
    // var result = await OCR.AnalyzeAsync(data);
    // Defaults to AzureDocumentIntelligence (cheap+robust general document OCR). Override the model via the second parameter when the task warrants. Read the extracted text from result.Text; result.Paragraphs and result.Pages carry the structure. Reach for the constructor + AnalyzeDocumentAsync when the document is a URL or AssetUri instead of bytes, or when you need page selection, word-level bounding boxes, or any other OCRConfig field; use AnalyzeDocumentStreamingAsync for page-by-page streaming.
    static Task<OCRResult> AnalyzeAsync(byte[] data, OCRModel model = AzureDocumentIntelligence, CancellationToken cancellationToken = default)
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    static OCRCapabilities GetCapabilities(OCRModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(OCRModel model)
  sealed class OCRBoundingBox
    ctor()
    int PageNumber { get; init; }
    List<float> Polygon { get; init; }
  sealed class OCRCapabilities : IOCRInfo
    ctor()
    int MaxPagesSupported { get; init; }
  sealed class OCRConfig
    ctor()
    AssetUri? AssetUri { get; set; }
    byte[]? Data { get; set; }
    DocumentType DocumentType { get; set; }
    bool IncludeWords { get; set; }
    string? Pages { get; set; }
    TimeSpan Timeout { get; set; }
    string? Url { get; set; }
  class OCRException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum OCRModel
    AzureDocumentIntelligence
    MistralOCR
  static class OCRModelExtensions
    static string DisplayName(this OCRModel model)
  sealed class OCRPage
    ctor()
    float Height { get; init; }
    int PageNumber { get; init; }
    string Unit { get; init; }
    float Width { get; init; }
  sealed class OCRParagraph
    ctor()
    List<OCRBoundingBox> BoundingRegions { get; init; }
    string Content { get; init; }
  sealed class OCRResult
    ctor()
    List<OCRPage> Pages { get; init; }
    List<OCRParagraph> Paragraphs { get; init; }
    string Text { get; init; }
    List<OCRWord> Words { get; init; }
  sealed class OCRWord
    ctor()
    OCRBoundingBox BoundingBox { get; init; }
    float Confidence { get; init; }
    string Content { get; init; }

namespace Ikon.AI.Reranking
  interface IReranker : IDisposable
    // Maximum duration of a single rerank request, scaled up internally with the document count. Defaults to 10 seconds.
    TimeSpan Timeout { get; set; }
    abstract Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, CancellationToken cancellationToken = default)
  class NonRetryableRerankerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class RerankItem : IEquatable<RerankItem>
    ctor()
    int Index { get; init; }
    double Score { get; init; }
  enum RerankModel
    CohereRerank4Fast
    CohereRerank4Pro
    JinaReranker3
    VoyageRerank25
    VoyageRerank25Lite
  static class RerankModelExtensions
    static string DisplayName(this RerankModel model)
  sealed class Reranker : IDisposable, IReranker
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(RerankModel model, IReadOnlyList<ModelRegion>? regions = null)
    TimeSpan Timeout { get; set; }
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(RerankModel model)
    Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, CancellationToken cancellationToken = default)
    // One-shot reranking. The verbose form
    // using var reranker = new Reranker(RerankModel.CohereRerank4Fast);
    // var items = await reranker.RerankAsync(documents, query);
    // becomes
    // var items = await Reranker.RerankAsync(documents, query);
    // Defaults to CohereRerank4Fast (cheap+fast). Override the model via the third parameter when the task warrants; pass topN to cap how many items are returned (0 returns all). Each RerankItem carries the document's original .Index and its relevance .Score, ordered most relevant first. Reach for the constructor + the instance RerankAsync when you need a custom Timeout or rerank many queries against the same reranker instance.
    static Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, RerankModel model = CohereRerank4Fast, int topN = 0, CancellationToken cancellationToken = default)
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
    ctor(string link, float score = 0)
    ctor(List<string> segments, float score = 0)
    ctor(ContentLink parent, string secondPart, float score = 0)
    ctor(string link, string secondPart, float score = 0)
    ContentLink Parent { get; }
    ContentLink Root { get; }
    override bool Equals(object? obj)
    List<(string Link, string Internal)> GenerateHierarchicalSplitLinks()
    override int GetHashCode()
    override string ToString()
    string Link
    float Score
    List<string> Segments
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
    Task InitializeAsync(string dataDirectory, EmbeddingModel embeddingModel = OpenAI3Small)
    Task InitializeAsync(IReadOnlyList<AssetUri> assetUris, EmbeddingModel embeddingModel = OpenAI3Small)
    ContentLink[] Prefer(ContentLink link, string detail)
    ContentLink[] Prefer(ContentLink[] links, string detail)
    Task<ContentLink[]> SearchAsync(string query, int maxLinks = 25, float searchThreshold = 0.1)
    Task<Retriever.Event[]> SearchEventsAsync(string startUtcTimestamp, string endUtcTimestamp, int maxResults = 100)
    Task<Retriever.Event[]> SearchEventsAsync(string startUtcTimestamp, string endUtcTimestamp, string searchString, int maxResults = 100)
    Task<KeywordSearchResult[]> SearchKeywordsAsync(string searchString, int maxResults = 100)
    Task StopAsync()
    Task WaitForLoadingToEndAsync()

namespace Ikon.AI.SoundEffectGeneration
  interface ISoundEffectGenerator : IDisposable, ISoundEffectGeneratorInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    abstract IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
  interface ISoundEffectGeneratorInfo
    bool SupportsLooping { get; }
  class NonRetryableSoundEffectGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class SoundEffectFileResult
    byte[] AudioData { get; init; }
    string ContentType { get; init; }
    double DurationSeconds { get; init; }
  sealed class SoundEffectGenerator : IDisposable, ISoundEffectGenerator, ISoundEffectGeneratorInfo
    ctor(string modelName)
    ctor(SoundEffectGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SoundEffectGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsLooping { get; }
    void Dispose()
    // One-shot sound effect generation. The verbose form
    // using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);
    // var result = await generator.GenerateSoundEffectFileAsync(new SoundEffectGeneratorConfig { Prompt = prompt });
    // becomes
    // var effect = await SoundEffectGenerator.GenerateAsync(prompt);
    // Defaults to ElevenLabsV2 (the only sound effect model). Returns a buffered WAV file (.AudioData / .ContentType / .DurationSeconds). Reach for the constructor + GenerateSoundEffectFileAsync when you need a target duration, looping, prompt influence, or any other SoundEffectGeneratorConfig field beyond the prompt; use GenerateSoundEffectAsync for streaming PCM chunks.
    static Task<SoundEffectFileResult> GenerateAsync(string prompt, SoundEffectGeneratorModel model = ElevenLabsV2, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<SoundEffectFileResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    static SoundEffectGeneratorCapabilities GetCapabilities(SoundEffectGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SoundEffectGeneratorModel model)
  sealed class SoundEffectGeneratorCapabilities : ISoundEffectGeneratorInfo
    ctor()
    bool SupportsLooping { get; init; }
  sealed class SoundEffectGeneratorConfig : IEquatable<SoundEffectGeneratorConfig>
    ctor()
    double? DurationSeconds { get; init; }
    bool Loop { get; init; }
    string Prompt { get; init; }
    double PromptInfluence { get; init; }
    TimeSpan Timeout { get; init; }
  class SoundEffectGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SoundEffectGeneratorModel
    ElevenLabsV2
  static class SoundEffectGeneratorModelExtensions
    static string DisplayName(this SoundEffectGeneratorModel model)

namespace Ikon.AI.SpeechGeneration
  sealed class TextFilter.Config
    ctor()
    int MaxTextLength { get; set; }
    bool RemoveEmojis { get; set; }
    bool SimplifyUrls { get; set; }
    bool SpeakOnlyFirstParagraph { get; set; }
  interface ISpeechGenerator : IDisposable
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    abstract IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
  class NonRetryableSpeechGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class SpeechGenerator : IDisposable, ISpeechGenerator
    ctor(string modelName)
    ctor(SpeechGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    void Dispose()
    // One-shot text-to-speech. The verbose form
    // using var generator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
    // await foreach (var chunk in generator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = text }))
    // {
    //     // collect chunk.Samples
    // }
    // becomes
    // var audio = await SpeechGenerator.GenerateAsync(text);
    // Defaults to ElevenFlash25 (cheap+fast). Override the model via the second parameter when the task warrants; pass voice to pick a voice (the model's default voice otherwise). The streamed chunks are concatenated into a single PCM AudioChunk (.Samples / .SampleRate / .ChannelCount). Never returns null — throws a SpeechGeneratorException when generation fails or the model produces no audio, so wrap in try/catch when the app should continue without the audio. Reach for the constructor + GenerateSpeechAsync when you need chunk-by-chunk streaming playback while generation runs, or any other SpeechGeneratorConfig field beyond text+voice (language, instructions, speed).
    static Task<AudioChunk> GenerateAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechGeneratorModel model)
    static IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> GetVoiceIdsByModel()
  sealed class SpeechGeneratorConfig : IEquatable<SpeechGeneratorConfig>
    ctor()
    string Instructions { get; init; }
    string Language { get; init; }
    string Speed { get; init; }
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

namespace Ikon.AI.SpeechRecognition
  sealed class AnalyzePronunciationConfig : IEquatable<AnalyzePronunciationConfig>
    ctor()
    int ChannelCount { get; init; }
    string Language { get; init; }
    string ReferenceText { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    TimeSpan Timeout { get; init; }
  sealed class Pronunciation.Break : IEquatable<Pronunciation.Break>
    ctor()
    int BreakLength { get; init; }
    List<string> ErrorTypes { get; init; }
    Pronunciation.MissingBreak MissingBreak { get; init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get; init; }
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    // The maximum duration of continuous speech before recognition is forced in SilenceTriggered mode. This prevents indefinite buffering when the speaker doesn't pause. Set to Zero or negative to disable the limit.
    TimeSpan MaxSpeechDuration { get; set; }
    // The recognition mode that determines how audio is segmented and when recognition is triggered.
    SpeechRecognizerAdapter.Mode Mode { get; set; }
    // The interval at which speech recognition is triggered in GrowingWindow and SlidingWindow modes. In GrowingWindow mode, recognition runs on all accumulated audio at this interval. In SlidingWindow mode, recognition runs on the audio collected since the last recognition.
    TimeSpan RecognitionInterval { get; set; }
    // The timeout for individual speech recognition API requests.
    TimeSpan RequestTimeout { get; set; }
    // The duration of continuous silence required to trigger recognition in SilenceTriggered mode. When the speaker pauses for this duration, the accumulated speech is sent for recognition.
    TimeSpan SilenceDuration { get; set; }
    // The amplitude threshold below which audio is considered silence. Sample values with absolute amplitude below this threshold are treated as silent.
    float SilenceThreshold { get; set; }
  sealed class Pronunciation.Feedback : IEquatable<Pronunciation.Feedback>
    ctor()
    Pronunciation.Prosody Prosody { get; init; }
  interface ISpeechRecognizer : IDisposable, ISpeechRecognizerInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    abstract Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    abstract Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    abstract IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  interface ISpeechRecognizerInfo
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
  sealed class Pronunciation.Intonation : IEquatable<Pronunciation.Intonation>
    ctor()
    List<string> ErrorTypes { get; init; }
    Pronunciation.Monotone Monotone { get; init; }
  sealed class Pronunciation.MissingBreak : IEquatable<Pronunciation.MissingBreak>
    ctor()
    double Confidence { get; init; }
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
  sealed class Pronunciation.Monotone : IEquatable<Pronunciation.Monotone>
    ctor()
    double SyllablePitchDeltaConfidence { get; init; }
  sealed class Pronunciation.NBest : IEquatable<Pronunciation.NBest>
    ctor()
    double Confidence { get; init; }
    string Display { get; init; }
    string ITN { get; init; }
    string Lexical { get; init; }
    string MaskedITN { get; init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Word> Words { get; init; }
  class NonRetryableSpeechRecognizerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class Pronunciation.Phoneme : IEquatable<Pronunciation.Phoneme>
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed class Pronunciation.PhonemePronunciationAssessment : IEquatable<Pronunciation.PhonemePronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
  static class Pronunciation
  sealed class Pronunciation.PronunciationAssessment : IEquatable<Pronunciation.PronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
    double CompletenessScore { get; init; }
    double FluencyScore { get; init; }
    double PronScore { get; init; }
    double ProsodyScore { get; init; }
  sealed class Pronunciation.Prosody : IEquatable<Pronunciation.Prosody>
    ctor()
    Pronunciation.Break Break { get; init; }
    Pronunciation.Intonation Intonation { get; init; }
  sealed class RecognizeContinuousSpeechConfig : IEquatable<RecognizeContinuousSpeechConfig>
    ctor()
    string[] CandidateLanguages { get; init; }
    int ChannelCount { get; init; }
    string Language { get; init; }
    int SampleRate { get; init; }
  sealed class RecognizeSpeechConfig : IEquatable<RecognizeSpeechConfig>
    ctor()
    int ChannelCount { get; init; }
    string Language { get; init; }
    string Prompt { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    double Temperature { get; init; }
    TimeSpan Timeout { get; init; }
  sealed class Pronunciation.Result : IEquatable<Pronunciation.Result>
    ctor()
    int Channel { get; init; }
    string DisplayText { get; init; }
    long Duration { get; init; }
    string Id { get; init; }
    List<Pronunciation.NBest> NBest { get; init; }
    long Offset { get; init; }
    string RecognitionStatus { get; init; }
    double SNR { get; init; }
  sealed class SpeechRecognizer : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
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
    // One-shot batch transcription. The verbose form
    // using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
    // var text = await recognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
    // {
    //     Samples = samples,
    //     SampleRate = 16000,
    //     ChannelCount = 1
    // });
    // becomes
    // var text = await SpeechRecognizer.RecognizeAsync(samples, 16000);
    // Defaults to WhisperLarge3Turbo (cheap+fast). Override the model via the third parameter when the task warrants. Returns the recognized text (empty when nothing was recognized). Reach for the constructor + RecognizeBatchSpeechAsync when you need PCM16 byte input, a language hint, a prompt, or any other RecognizeSpeechConfig field; use RecognizeContinuousSpeechAsync for streaming recognition.
    static Task<string> RecognizeAsync(float[] samples, int sampleRate, SpeechRecognizerModel model = WhisperLarge3Turbo, int channelCount = 1, CancellationToken cancellationToken = default)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerAdapter : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
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
    DeepgramNova3General
    AssemblyAIUniversal3ProStreaming
    AssemblyAIUniversalStreamingEnglish
    AssemblyAIUniversalStreamingMultilingual
    VoxtralMiniTranscribe2
  static class SpeechRecognizerModelExtensions
    static string DisplayName(this SpeechRecognizerModel model)
  sealed class Pronunciation.Syllable : IEquatable<Pronunciation.Syllable>
    ctor()
    long Duration { get; init; }
    string Grapheme { get; init; }
    long Offset { get; init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed class Pronunciation.SyllablePronunciationAssessment : IEquatable<Pronunciation.SyllablePronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
  sealed class Pronunciation.UnexpectedBreak : IEquatable<Pronunciation.UnexpectedBreak>
    ctor()
    double Confidence { get; init; }
  sealed class Pronunciation.Word : IEquatable<Pronunciation.Word>
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    List<Pronunciation.Phoneme> Phonemes { get; init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Syllable> Syllables { get; init; }
    string Text { get; init; }
  sealed class Pronunciation.WordPronunciationAssessment : IEquatable<Pronunciation.WordPronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
    string ErrorType { get; init; }
    Pronunciation.Feedback Feedback { get; init; }

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
    ctor()
    Task CreateCollectionAsync(string collectionName, EmbeddingModel model)
    Task<int> GetDataItemCountAsync(string collectionName)
    Task RemoveAsync(string collectionName, IEnumerable<string> tags)
    Task<List<Result<object>>> SearchAsync(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<object>>> SearchAsync(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<int> SetAsync(string collectionName, int? key, string text, object value, IEnumerable<string>? tags = null)
    Task<int> SetAsync(string collectionName, int? key, float[] vector, object value, IEnumerable<string>? tags = null)

namespace Ikon.AI.Utils
  static class ImageUtils
    static byte[] ConvertAlphaMaskToBlackWhiteMask(byte[] maskData)
    static byte[] ConvertBlackWhiteMaskToAlphaMask(byte[] maskData)
    // Re-encodes an image as JPEG with both dimensions capped at maxDimension (aspect preserved). Returns the original bytes untouched when the image already fits AND is at most maxBytes — small screenshots pass through without a decode cost. Intended for images going into LLM context, where anything above ~1568px is downscaled by the provider anyway and only costs tokens.
    static (byte[] Bytes, string MimeType, int Width, int Height) EncodeJpegCapped(byte[] source, string sourceMimeType, int maxDimension = 1568, int quality = 70, int maxBytes = 204800)
    static (int width, int height) GetImageDimensions(byte[] buffer)
    static byte[] InvertMask(byte[] maskData)

namespace Ikon.AI.VideoEnhancement
  interface IVideoEnhancer : IDisposable
    abstract Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
  class NonRetryableVideoEnhancerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class VideoEnhancer : IDisposable, IVideoEnhancer
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoEnhancerModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // One-shot video enhancement from a video URL. The verbose form
    // using var enhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixUpscale2xUltra41);
    // var result = await enhancer.EnhanceVideoAsync(new VideoEnhancerConfig { VideoUrl = url });
    // becomes
    // var enhanced = await VideoEnhancer.EnhanceAsync(url);
    // Defaults to TensorPixUpscale2xUltra41 (the current 2x upscale generation — cheaper than the 4x filter). Override the model via the second parameter when the task warrants. Returns the enhanced video as a download URL in .Url along with .OutputFps and .OutputSizeBytes. Reach for the constructor + EnhanceVideoAsync when you need to enhance raw video bytes (VideoData), trim to a frame range, set a target FPS for TensorPixFpsBoost , or any other VideoEnhancerConfig field beyond the URL.
    static Task<VideoEnhancerResult> EnhanceAsync(string videoUrl, VideoEnhancerModel model = TensorPixUpscale2xUltra41, CancellationToken cancellationToken = default)
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoEnhancerModel model)
  sealed class VideoEnhancerConfig : IEquatable<VideoEnhancerConfig>
    ctor()
    int? EndFrame { get; init; }
    string? MimeType { get; init; }
    int? StartFrame { get; init; }
    int? TargetFps { get; init; }
    TimeSpan Timeout { get; init; }
    byte[]? VideoData { get; init; }
    string? VideoUrl { get; init; }
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
  sealed class VideoEnhancerResult : IEquatable<VideoEnhancerResult>
    ctor()
    int? OutputFps { get; init; }
    long? OutputSizeBytes { get; init; }
    string Url { get; init; }

namespace Ikon.AI.VideoGeneration
  interface IVideoGenerator : IDisposable, IVideoGeneratorInfo
    abstract Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IVideoGeneratorInfo
    int MaxInputImages { get; }
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
  sealed class VideoGeneratorConfig.InputImage : IEquatable<VideoGeneratorConfig.InputImage>
    ctor()
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  class NonRetryableVideoGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class VideoGenerator : IDisposable, IVideoGenerator, IVideoGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputImages { get; }
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
    // One-shot text-to-video. The verbose form
    // using var generator = new VideoGenerator(VideoGeneratorModel.Veo31Fast);
    // var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig { Prompt = prompt });
    // becomes
    // var video = await VideoGenerator.GenerateAsync(prompt);
    // Defaults to Veo31Fast (the cheap+fast tier of the strongest general-purpose family). Override the model via the second parameter when the task warrants. Returns the result with the generated clip's .Url. Reach for the constructor + GenerateVideoAsync when you need input images (image-to-video), a specific length, resolution, aspect ratio, negative prompt, audio, or any other VideoGeneratorConfig field beyond the prompt.
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
    int MaxInputImages { get; init; }
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
  sealed class VideoGeneratorConfig : IEquatable<VideoGeneratorConfig>
    ctor()
    VideoGeneratorAspectRatio AspectRatio { get; init; }
    bool? GenerateAudio { get; init; }
    List<VideoGeneratorConfig.InputImage> InputImages { get; init; }
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
    Hailuo23
    Hailuo23Fast
    Kling26
    Kling30
    Kling30Omni
    KlingVideoO1
    LumaRay20
    LumaRay20Flash
    Pika22
    Pixverse55
    Pollo20
    Pollo30
    Pollodance20
    Pollodance20Fast
    RunwayGen4
    Seedance15Pro
    Sora2
    Sora2Pro
    Veo31
    Veo31Fast
    ViduQ2Pro
    ViduQ2Turbo
    ViduQ3Pro
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
  sealed class VideoGeneratorResult : IEquatable<VideoGeneratorResult>
    ctor()
    string Url { get; init; }

namespace Ikon.AI.WebScraping
  sealed class Cookie : IEquatable<Cookie>
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
  sealed class DownloadFileConfig : IEquatable<DownloadFileConfig>
    ctor()
    string CountryCode { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
  sealed class DownloadFileResult : IEquatable<DownloadFileResult>
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Url { get; init; }
  interface IWebScraper : IDisposable, IWebScraperInfo
    abstract Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = default)
    abstract Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = default)
    abstract Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = default)
    abstract Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = default)
  interface IWebScraperInfo
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
  sealed class MultiPageScrapeConfig : IEquatable<MultiPageScrapeConfig>
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
  sealed class PageResult : IEquatable<PageResult>
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string Mimetype { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed class ScreenshotConfig : IEquatable<ScreenshotConfig>
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
  sealed class ScreenshotResult : IEquatable<ScreenshotResult>
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
  sealed class SinglePageScrapeConfig : IEquatable<SinglePageScrapeConfig>
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
  sealed class WebScraper : IDisposable, IWebScraper, IWebScraperInfo
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
    // One-shot single page scrape. The verbose form
    // using var scraper = new WebScraper(WebScraperModel.Jina);
    // var page = await scraper.ScrapeSinglePageAsync(new SinglePageScrapeConfig { Url = url });
    // becomes
    // var page = await WebScraper.ScrapeAsync(url);
    // Defaults to Jina (cheap+fast hosted reader). Override the model via the second parameter when the task warrants. Returns the page as Markdown in .Content along with .Title and .Url. Reach for the constructor + ScrapeSinglePageAsync when you need a different output format, cookies, custom JavaScript, or any other SinglePageScrapeConfig field beyond the URL; use ScrapeMultiplePagesAsync , TakeScreenshotAsync , or DownloadFileAsync for crawling, screenshots, and file downloads.
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
    abstract Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
    abstract Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
  interface IWebSearcherInfo
    bool SupportsImageSearching { get; }
  class NonRetryableWebSearcherException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class SearchConfig : IEquatable<SearchConfig>
    ctor()
    string CountryCode { get; init; }
    string InSiteUrl { get; init; }
    string Language { get; init; }
    int MaxResults { get; init; }
    WebSearcherOutputFormat OutputFormat { get; init; }
    string Query { get; init; }
    TimeSpan Timeout { get; init; }
  sealed class SearchResult : IEquatable<SearchResult>
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string Mimetype { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed class WebSearcher : IDisposable, IWebSearcher, IWebSearcherInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(WebSearcherModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsImageSearching { get; }
    void Dispose()
    static WebSearcherCapabilities GetCapabilities(WebSearcherModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebSearcherModel model)
    // One-shot web page search. The verbose form
    // using var searcher = new WebSearcher(WebSearcherModel.Google);
    // var results = await searcher.SearchPagesAsync(new SearchConfig { Query = query });
    // becomes
    // var results = await WebSearcher.SearchAsync(query);
    // Defaults to Google (cheap+fast general web search). Override the model via the second parameter when the task warrants. Each SearchResult exposes .Url, .Title, and .Content. Reach for the constructor + SearchPagesAsync when you need site-restricted search, country/language targeting, or any other SearchConfig field beyond query+max results; use SearchImagesAsync (with an image-capable model such as GoogleImages ) for image search.
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
  // Per-client theme state created by UseTheme . Holds each client's active theme and switches it: Current is bindable in views, and ToggleAsync can be bound directly to a button's onClick.
  sealed class ThemeControl
    // The calling client's active theme as its wire string, "dark" or "light" (see ToThemeName ). Bindable in views, e.g. name: theme.Current.Value == Theme.Dark.ToThemeName() ? "sun" : "moon".
    ClientReactive<string> Current { get; }
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
    // When true (the default), a re-render reuses the cached output of any subtree whose tracked reactive dependencies are unchanged, skipping re-execution of its content lambda — so an update costs O(changed subtree) rather than O(whole tree). Wire output is identical to the uncached path (proven by the differential oracle in Ikon.Parallax.Test); the only behavioural change is that a subtree reading NON-reactive data no longer refreshes until one of its reactive dependencies changes, which is the intended reactive contract. Set false to force a full re-render every cycle.
    bool EnableSubtreeCaching { get; set; }
    // Defines the root UI view tree. Call this in a reactive context to re-render when dependencies change.
    void Root(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
    // One-call per-client theme handling: syncs each joining client's theme, exposes the active theme as a bindable Current , and provides dark/light switching. The verbose form
    // private readonly ClientReactive<string> _theme = new(Theme.Dark.ToThemeName());
    //
    // // In Main:
    // app.ClientJoinedAsync += async args =>
    // {
    //     if (string.IsNullOrEmpty(args.ClientContext.Theme))
    //     {
    //         _theme.Value = Theme.Dark.ToThemeName();
    //         await ClientFunctions.SetThemeAsync(Theme.Dark);
    //     }
    //     else
    //     {
    //         _theme.Value = (args.ClientContext.IsDarkTheme() ? Theme.Dark : Theme.Light).ToThemeName();
    //     }
    // };
    //
    // private async Task ToggleThemeAsync()
    // {
    //     var next = _theme.Value == Theme.Dark.ToThemeName() ? Theme.Light : Theme.Dark;
    //     await ClientFunctions.SetThemeAsync(next);
    //     _theme.Value = next.ToThemeName();
    // }
    // becomes
    // _theme = UI.UseTheme();
    // with _theme.Current bindable in views (e.g. a sun/moon icon) and _theme.ToggleAsync bindable to a button's onClick. When followClient is true (the default), a joining client that already has a theme keeps it and clients without one get defaultTheme ; when false, every joining client is forced to defaultTheme . Call once in Main, before clients join.
    ThemeControl UseTheme(Theme defaultTheme = Dark, bool followClient = true)
  // Represents a UI view scope for building the component tree. Extension methods on this type provide the component API (e.g. Text, Button, Input).
  class UIView
    // The default icon library name used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }
    // True when this render is capturing the build-time boot snapshot (the client's Context.IsSnapshot is set). The snapshot is a public asset shown to everyone before the live UI connects, so gate per-user or sensitive content on this — typically via the SnapshotReveal / SnapshotHide / SnapshotOnly wrappers rather than reading this directly. Always false on the normal live render path.
    bool IsSnapshot { get; }
    // Adds a child node with the given type and props. The props parameter is the non-generic IDictionary on purpose: it's the ONLY type that cleanly accepts BOTH a `Dictionary<string, object>` (the natural non-null shape a model builds) AND a `Dictionary<string, object?>` (props that carry null values) with no nullability warning and no suppression. A generic `Dictionary<string, object?>` param warns CS8620 on the non-null form (identity-modulo-nullability), and no PAIR of generic overloads works either — nullability annotations are erased for overload resolution, so two such overloads are CS0111 (same signature) or CS0121 (ambiguous).
    void AddNode(string type, IDictionary? props = null, List<UIViewNode>? children = null, string? key = null, string[]? style = null, string? styleId = null)
    // Registers a callback as a UI action and returns its ID for use in component props.
    string? CreateAction<T>(Func<ActionArgs<T>, Task>? callback)
    // Registers binary data as a payload and returns a reference string for use as an image src.
    string RegisterPayload(byte[] data, string mimeType)
  // Represents a single node in the UI view tree, with identity, props, children, and style information.
  sealed class UIViewNode
    // Creates a new view node with the given type, props, and optional children.
    ctor(string type, Guid viewId, Dictionary<string, object?> props, List<UIViewNode>? children = null, string? key = null, IReadOnlyList<string>? styleIds = null, ulong parentPathHash = 0, IReadOnlyCollection<IReadOnlyList<string>>? styleIdProps = null)
    // Ordered child nodes.
    List<UIViewNode> Children { get; }
    // Lazily computed content signature used for subtree caching and diffing.
    string? ContentFingerprint { get; }
    // True when StableHint came from an explicit key argument, not from a prop such as value or text.
    bool HasExplicitKey { get; }
    // Stable unique identifier for this node.
    string Id { get; }
    // Precomputed hash of Id for fast lookups.
    int IdHash { get; }
    // When true, nodes include source file and line markers for debugging.
    static bool IncludeSourceMarkers { get; set; }
    // Component properties passed to the frontend renderer.
    Dictionary<string, object?> Props { get; }
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
  sealed class AxisConfig : IEquatable<AxisConfig>
    ctor()
    // Format string for tick labels. For time scales, use d3-time-format tokens (e.g. "%H:%M", "%m/%d %H:%M").
    string? Format { get; init; }
    string? Legend { get; init; }
    int? LegendOffset { get; init; }
    int? TickPadding { get; init; }
    int? TickRotation { get; init; }
    int? TickSize { get; init; }
    // Number of ticks to display. When set, the axis will show approximately this many evenly-spaced ticks instead of one per data point.
    int? TickValues { get; init; }
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
  class ChartAxisStyle : IEquatable<ChartAxisStyle>
    ctor()
    string? DomainColor { get; init; }
    ChartTextStyle? Legend { get; init; }
    string? TickColor { get; init; }
    ChartTextStyle? TickLabel { get; init; }
  // Event arguments for chart click interactions.
  sealed class ChartClickArgs : IEquatable<ChartClickArgs>
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
  class ChartCrosshairStyle : IEquatable<ChartCrosshairStyle>
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Extension methods for rendering interactive chart components (bar, line, pie).
  static class ChartExtensions
    // Renders an interactive bar chart with configurable grouping, layout, axes, and theming. Pass valueUnit to render tooltip values and value-axis ticks in a human-scaled unit — well-known units are "milliseconds", "seconds", "bytes", "percent", and "usd" (e.g. 1333.9 milliseconds renders as "1.33 s"); any other string is appended as a plain suffix.
    static void BarChart(this UIView view, string[]? style = null, IEnumerable<Dictionary<string, object>>? data = null, IEnumerable<string>? keys = null, string? indexBy = null, BarGroupMode? groupMode = null, BarLayout? layout = null, ScaleType? valueScale = null, ScaleType? indexScale = null, bool? reverse = null, double? minValue = null, double? maxValue = null, double? padding = null, double? innerPadding = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enableLabel = null, int? labelSkipWidth = null, int? labelSkipHeight = null, string? labelTextColor = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderRadius = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Renders an interactive line chart with configurable curves, points, areas, and crosshairs. Pass valueUnit to render tooltip Y values and left-axis ticks in a human-scaled unit — well-known units are "milliseconds", "seconds", "bytes", "percent", and "usd" (e.g. 1333.9 milliseconds renders as "1.33 s"); any other string is appended as a plain suffix.
    static void LineChart(this UIView view, string[]? style = null, IEnumerable<LineChartSeries>? data = null, ScaleType? xScaleType = null, ScaleType? yScaleType = null, double? xScaleMin = null, double? xScaleMax = null, double? yScaleMin = null, double? yScaleMax = null, bool? yScaleStacked = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enablePoints = null, int? pointSize = null, string? pointColor = null, string? pointBorderColor = null, int? pointBorderWidth = null, bool? enableArea = null, double? areaOpacity = null, double? areaBaselineValue = null, bool? enableCrosshair = null, CrosshairType? crosshairType = null, LineCurve? curve = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, double? lineWidth = null, bool? isInteractive = null, bool? useMesh = null, bool? enableSlices = null, string? xFormat = null, string? yFormat = null, string? valueUnit = null, string? gradientFromColor = null, string? gradientToColor = null, IEnumerable<double>? gridXValues = null, IEnumerable<double>? gridYValues = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Renders an interactive pie/donut chart with configurable arc labels, link labels, and legends. Pass valueUnit to render tooltip values in a human-scaled unit — well-known units are "milliseconds", "seconds", "bytes", "percent", and "usd" (e.g. 1333.9 milliseconds renders as "1.33 s"); any other string is appended as a plain suffix.
    static void PieChart(this UIView view, string[]? style = null, IEnumerable<PieChartDatum>? data = null, double? innerRadius = null, double? padAngle = null, double? cornerRadius = null, double? startAngle = null, double? endAngle = null, bool? sortByValue = null, ChartMargin? margin = null, bool? enableArcLabels = null, string? arcLabelsTextColor = null, double? arcLabelsSkipAngle = null, bool? enableArcLinkLabels = null, string? arcLinkLabelsTextColor = null, double? arcLinkLabelsSkipAngle = null, double? arcLinkLabelsThickness = null, string? arcLinkLabelsColor = null, double? activeOuterRadiusOffset = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? arcLabelAsPercentage = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
  // Styling for chart grid lines.
  class ChartGridStyle : IEquatable<ChartGridStyle>
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Styling for chart data labels.
  class ChartLabelsStyle : IEquatable<ChartLabelsStyle>
    ctor()
    ChartTextStyle? Text { get; init; }
  // Styling for chart legend text and title.
  class ChartLegendStyle : IEquatable<ChartLegendStyle>
    ctor()
    ChartTextStyle? Text { get; init; }
    ChartTextStyle? Title { get; init; }
  // Margin configuration for chart containers.
  sealed class ChartMargin : IEquatable<ChartMargin>
    ctor()
    int? Bottom { get; init; }
    int? Left { get; init; }
    int? Right { get; init; }
    int? Top { get; init; }
  // Text styling for chart elements.
  class ChartTextStyle : IEquatable<ChartTextStyle>
    ctor()
    string? Color { get; init; }
    string? FontFamily { get; init; }
    int? FontSize { get; init; }
  // Complete theme configuration for chart components, combining all styling aspects.
  class ChartTheme : IEquatable<ChartTheme>
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
  class ChartTooltipStyle : IEquatable<ChartTooltipStyle>
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
  sealed class LegendConfig : IEquatable<LegendConfig>
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
  sealed class LineChartPoint : IEquatable<LineChartPoint>
    // X value — a string label for point scales, or a number for linear/time scales, so the type is genuinely mixed.
    object X { get; init; }
    double Y { get; init; }
  // A named data series for a line chart, containing an ordered collection of points.
  sealed class LineChartSeries : IEquatable<LineChartSeries>
    string? Color { get; init; }
    IEnumerable<LineChartPoint>? Data { get; init; }
    string Id { get; init; }
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
  sealed class PieChartDatum : IEquatable<PieChartDatum>
    string? Color { get; init; }
    string Id { get; init; }
    string? Label { get; init; }
    double Value { get; init; }
  // Scale type for chart axes.
  enum ScaleType
    Point
    Linear
    Time
    Log

namespace Ikon.Parallax.Components.DataTable
  // A single cell in a data table row. Use the static factory methods to create typed cells.
  class Cell : IEquatable<Cell>
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
    // Creates a badge cell. The tone renders through the themed badge recipe (Theming.Badge.*); style classes merge on top of the tone token, and the literal "unstyled" class opts out of the tone token entirely.
    static Cell Badge(string value, SemanticTone? tone = null, string[]? style = null)
    // Creates a checkbox cell.
    static Cell Checkbox(bool value, string actionId, string[]? style = null, bool disabled = false)
    // Creates a text cell.
    static Cell Text(string? value, string[]? style = null)
  // An action button that can be displayed within a data table cell.
  class CellAction : IEquatable<CellAction>
    ctor(string Label, string ActionId, string[]? Style = null, string? Icon = null)
    string ActionId { get; init; }
    string? Icon { get; init; }
    string Label { get; init; }
    string[]? Style { get; init; }
  // The kind of content a data table cell renders.
  enum CellType
    Text
    Badge
    Action
    Actions
    Checkbox
  // Defines a column in a data table including header text, width, and alignment.
  class DataTableColumn : IEquatable<DataTableColumn>
    ctor(string Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left, string? MinWidth = null, bool Wrap = false)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string Header { get; init; }
    string? MinWidth { get; init; }
    string? Width { get; init; }
    bool Wrap { get; init; }
  // Extension methods for rendering paginated data tables.
  static class DataTableExtensions
    // Renders a paginated data table with configurable columns, rows, actions, and styling.
    static void DataTable(this UIView view, DataTableColumn[] columns, DataTableRow[] rows, int totalCount, int pageIndex, int pageSize, Func<int, Task>? onPageChange = null, Func<string, Task>? onRowClick = null, Func<string, Task>? onActionClick = null, Action<UIView>? emptyContent = null, int[]? columnWidths = null, Func<string, Task>? onColumnResize = null, string[]? style = null, string[]? headerStyle = null, string[]? rowStyle = null, string[]? cellStyle = null, string[]? headerCellStyle = null, string[]? dataCellStyle = null, string[]? paginationStyle = null, string[]? paginationButtonStyle = null, string[]? pageNumberStyle = null, string[]? pageNumberActiveStyle = null, string[]? emptyStyle = null, string[]? actionButtonStyle = null, string[]? resizeHandleStyle = null, string[]? tooltipStyle = null, string? prevLabel = null, string? nextLabel = null, string? pageLabel = null, string? key = null)
  // A single row in a data table, identified by a unique ID and containing an array of cells.
  class DataTableRow : IEquatable<DataTableRow>
    ctor(string Id, Cell[] Cells)
    Cell[] Cells { get; init; }
    string Id { get; init; }

namespace Ikon.Parallax.Components.ImageEditor
  // Extension methods for the image editor canvas component.
  static class ImageEditorExtensions
    // Canvas for editing images with brush and eraser tools.
    static void ImageEditorCanvas(this UIView view, string[]? style = null, string? src = null, int? brushWidth = null, string? brushColor = null, ImageEditorTool? tool = null, double? zoom = null, bool? highResolution = null, int? textMaxLength = null, int? textFontSize = null, int? textPadding = null, Func<ImageEditorSaveArgs, Task>? onSave = null, Func<ImageEditorHistoryArgs, Task>? onHistoryChange = null, int? triggerSave = null, int? triggerUndo = null, int? triggerRedo = null, string? styleId = null, string? key = null)
  // Event args for when the undo/redo history state changes.
  sealed class ImageEditorHistoryArgs : IEquatable<ImageEditorHistoryArgs>
    ctor(bool CanUndo, bool CanRedo)
    bool CanRedo { get; init; }
    bool CanUndo { get; init; }
  // Event args for when the image editor saves the edited image.
  sealed class ImageEditorSaveArgs : IEquatable<ImageEditorSaveArgs>
    ctor(string ImageData)
    string ImageData { get; init; }
  // Drawing tool active on an ImageEditorCanvas.
  enum ImageEditorTool
    Brush
    Eraser
    Text
    Arrow
    Region

namespace Ikon.Parallax.Components.Rive
  // Layout alignment options for Rive animations.
  enum RiveAlignment
    Center
    TopLeft
    TopCenter
    TopRight
    CenterLeft
    CenterRight
    BottomLeft
    BottomCenter
    BottomRight
  // Represents a color value for Rive animations.
  sealed class RiveColor
    ctor()
    // Blue channel (0-255).
    int B { get; init; }
    // Green channel (0-255).
    int G { get; init; }
    // Red channel (0-255).
    int R { get; init; }
  // Data received from a Rive event.
  sealed class RiveEventData
    ctor()
    // Delay in seconds before the event fires.
    double? Delay { get; init; }
    // The name of the Rive event.
    string Name { get; init; }
    // Custom properties attached to the event as JSON elements.
    Dictionary<string, JsonElement>? Properties { get; init; }
    // Type-safe accessor for the event's custom properties.
    RiveEventProperties Props { get; }
    // Target identifier for the event.
    string? Target { get; init; }
    // The Rive event type identifier.
    int? Type { get; init; }
    // URL associated with the event, if any.
    string? Url { get; init; }
  // Helper class for accessing Rive event properties with type-safe methods.
  sealed class RiveEventProperties
    ctor(Dictionary<string, JsonElement>? properties)
    // Gets a boolean property value, or defaultValue if not found.
    bool GetBool(string key, bool defaultValue = false)
    // Gets a double property value, or defaultValue if not found.
    double GetDouble(string key, double defaultValue = 0)
    // Gets an integer property value, or defaultValue if not found.
    int GetInt(string key, int defaultValue = 0)
    // Gets a string property value, or defaultValue if not found.
    string GetString(string key, string defaultValue = "")
  // Extension methods for Rive animation components.
  static class RiveExtensions
    // Canvas for rendering Rive animations with state machine support.
    static void RiveCanvas(this UIView view, string[]? style = null, string? source = null, IEnumerable<string>? stateMachines = null, RiveViewModel? viewModel = null, IEnumerable<RiveTrigger>? triggers = null, Func<RiveEventData, Task>? onEvent = null, RiveFit? layoutFit = null, RiveAlignment? layoutAlignment = null, bool? autoplay = null, bool? useOffscreenRenderer = null, bool? autoBind = null, bool? enableMultiTouch = null, bool? dispatchPointerExit = null, bool? isTouchScrollEnabled = null, bool? shouldDisableRiveListeners = null, IEnumerable<RiveKeyboardBinding>? keyboardBindings = null, string? backgroundColor = null, string? width = null, string? height = null, string? styleId = null, string? key = null)
  // Layout fit options for Rive animations.
  enum RiveFit
    Contain
    Cover
    Fill
    FitWidth
    FitHeight
    None
    ScaleDown
    Layout
  // Static helpers for creating keyboard bindings.
  static class RiveKeyboard
    // Creates a boolean binding that sets a Rive boolean input to true while the key is held.
    static RiveKeyboardBinding Boolean(RiveKeyboardKey key, string inputName)
    // Creates a trigger binding that fires a Rive trigger input when the key is pressed.
    static RiveKeyboardBinding Trigger(RiveKeyboardKey key, string inputName)
  // Represents a keyboard binding for a Rive animation input.
  sealed class RiveKeyboardBinding
    ctor()
    // The Rive state machine input name to bind to.
    string InputName { get; init; }
    // The keyboard key that triggers this binding.
    RiveKeyboardKey Key { get; init; }
    // Whether this binding is a boolean (held) or trigger (pressed) type.
    RiveKeyboardBindingKind Kind { get; init; }
  // Types of keyboard bindings for Rive inputs.
  enum RiveKeyboardBindingKind
    Boolean
    Trigger
  // Keyboard keys that can be bound to Rive inputs.
  enum RiveKeyboardKey
    ArrowUp
    ArrowDown
    ArrowLeft
    ArrowRight
  // Represents a trigger that can be fired in a Rive animation. Calling Fire() increments the sequence and triggers a UI re-render.
  sealed class RiveTrigger
    ctor(string name)
    // The name of this trigger, matching the Rive input name.
    string Name { get; }
    // Current trigger sequence number, incremented on each fire.
    long Sequence { get; }
    // Fires the trigger, causing the Rive animation to respond on the next render.
    void Fire()
  // Fluent builder for constructing Rive view model data.
  sealed class RiveViewModel
    ctor()
    // Sets a boolean input on the Rive state machine.
    RiveViewModel Boolean(string name, bool? value)
    // Sets an RGB color input on the Rive state machine.
    RiveViewModel Color(string name, int r, int g, int b)
    // Sets an enum input on the Rive state machine by integer value.
    RiveViewModel Enum(string name, int? value)
    // Sets a number input on the Rive state machine.
    RiveViewModel Number(string name, double? value)
    // Sets a string input on the Rive state machine.
    RiveViewModel String(string name, string? value)

namespace Ikon.Parallax.Components.Standard
  // Extension methods for accessibility components.
  static class AccessibilityExtensions
    // Wraps an icon with accessible label for screen readers.
    static void AccessibleIcon(this UIView view, string[]? style = null, string? label = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Hides content visually while keeping it accessible to screen readers.
    static void VisuallyHidden(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Base event returned from a client-side action, indicating the action type and whether it succeeded.
  class ActionEvent : IEquatable<ActionEvent>
    ctor(string ActionType, bool Success)
    string ActionType { get; init; }
    bool Success { get; init; }
  // Types of client-side actions that can be triggered from the server.
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
  // Base class for client-side action configuration.
  abstract class ActionOptions : IEquatable<ActionOptions>
  // Represents activation mode for Tabs.
  enum ActivationMode
    Automatic
    Manual
  // Inline alert banner composite over the theme's Alert token recipe.
  static class AlertExtensions
    // Inline alert banner — tone-tinted surface with an icon, title, optional description, and an optional dismiss button. The icon defaults per tone (success check, warning triangle, error alert, info circle). Caller styles merge on top of the tone's Theming.Alert token; include the literal "unstyled" class to opt out.
    static void Alert(this UIView view, string title, SemanticTone tone = Neutral, string[]? style = null, string? description = null, string? icon = null, bool showIcon = true, Func<Task>? onDismiss = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? iconStyle = null, string[]? dismissStyle = null, Action<UIView>? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Represents alignment for overlay positioning.
  enum Align
    Start
    Center
    End
  // Status pill composite over the theme's Badge token recipe. Replaces the hand-rolled inline-flex/rounded-full/px-2 pill pattern with a one-liner: view.Badge("Live", SemanticTone.Success).
  static class BadgeExtensions
    // Small status pill. With zero style args it renders the themed pill for the tone (Theming.Badge.*); caller styles merge on top of the base token, and the literal "unstyled" class opts out of the base entirely.
    static void Badge(this UIView view, string text, SemanticTone tone = Neutral, string[]? style = null, BadgeSize size = Md, bool outline = false, bool dot = false, string[]? dotStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Size of a Badge .
  enum BadgeSize
    Sm
    Md
    Lg
  // Breadcrumb trail composite over the theme's Breadcrumb token recipe.
  static class BreadcrumbExtensions
    // Breadcrumb navigation trail. Items with an OnClick render as clickable links; the last item always renders as the non-clickable current page (with aria-current="page"). A chevron separator is placed between items.
    static void Breadcrumb(this UIView view, IReadOnlyList<BreadcrumbItem> items, string[]? style = null, string? separatorIcon = null, string[]? linkStyle = null, string[]? itemStyle = null, string[]? pageStyle = null, string[]? separatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // One entry in a Breadcrumb trail.
  sealed class BreadcrumbItem : IEquatable<BreadcrumbItem>
    ctor(string Label, Func<Task>? OnClick = null)
    // Visible text of the crumb.
    string Label { get; init; }
    // Navigation callback. Ignored for the last item, which always renders as the non-clickable current page.
    Func<Task>? OnClick { get; init; }
  // Extension methods for Calendar and DatePicker components.
  static class CalendarExtensions
    // Month-grid date selector. Renders a single month with day cells. Dates are ISO yyyy-MM-dd strings.
    static void Calendar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? month = null, string? defaultMonth = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, string? locale = null, bool? disabled = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onMonthChange = null)
    // Button that opens a popover containing a Calendar .
    static void DatePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, string? format = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? calendarStyle = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Which physical camera to prefer when starting the capture. Maps to the W3C MediaStream facingMode constraint and is treated as an "ideal" hint — the browser falls back to whatever camera is available if the requested side does not exist (e.g. desktops without a rear camera).
  enum CameraFacing
    User
    Environment
  // Options for capturing an image from the client's camera.
  sealed class CaptureImageActionOptions : ActionOptions, IEquatable<CaptureImageActionOptions>
    ctor()
    // Hardware constraints for camera selection.
    CaptureImageConstraints? Constraints { get; init; }
    // Output image format.
    ClientImageCaptureFormat? Format { get; init; }
    // Desired image height in pixels.
    int? Height { get; init; }
    // How the capture is presented (native OS camera UI vs. headless silent grab). Defaults to Headless — silent webcam capture via getUserMedia, which works uniformly on desktop and mobile. Set to Native to opt in to the OS camera app on phones (preview + shutter + front/back toggle); on desktop browsers Native transparently falls back to the headless path because the web platform doesn't expose a camera-app launch.
    CaptureImageMode? Mode { get; init; }
    // Image quality (0.0 to 1.0) for lossy formats.
    double? Quality { get; init; }
    // Desired image width in pixels.
    int? Width { get; init; }
  // Hardware constraints for image capture. Applied directly when Mode is Headless . In Native mode only FacingMode is honored (mapped to the file input's capture attribute); the OS camera UI ignores other constraints.
  sealed class CaptureImageConstraints : IEquatable<CaptureImageConstraints>
    ctor()
    // Preferred camera device ID. Headless mode only.
    string? DeviceId { get; init; }
    // Preferred camera side (front vs. rear). Most useful on phones where Environment opens the rear camera by default. On desktops with only a webcam this is ignored.
    CameraFacing? FacingMode { get; init; }
  // How the image capture is presented to the user. Controls whether the OS camera UI is invoked or whether the capture happens silently.
  enum CaptureImageMode
    Native
    Headless
  // Card-family composites: Card, StatCard, and EmptyState. All are server-side compositions over the container/text primitives styled by the Theming.Card / Theming.StatCard / Theming.EmptyState token recipes — beautiful by default, every part overridable.
  static class CardExtensions
    // Surface container with optional header (title + description), body content, and footer, per the theme's Card recipe. With zero style args it renders the themed card (Theming.Card.Default; Theming.Card.Interactive when onClick is set). Caller styles merge on top of the base token; include the literal "unstyled" class to opt out of the base entirely.
    static void Card(this UIView view, string[]? style = null, string? title = null, string? description = null, Action<UIView>? header = null, Action<UIView>? content = null, Action<UIView>? footer = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? contentStyle = null, string[]? footerStyle = null, Delegate? onClick = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Card — positional (style, children) overload so view.Card([style], v => {...}) binds the lambda to the body instead of tripping on the title parameter.
    static void Card(this UIView view, string[]? style, Action<UIView> children)
    // Centered placeholder for empty lists/pages — optional icon, title, optional description, and an optional action row — per the theme's EmptyState recipe.
    static void EmptyState(this UIView view, string title, string[]? style = null, string? description = null, string? icon = null, Action<UIView>? action = null, string[]? iconWrapStyle = null, string[]? iconStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? actionsStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Dashboard statistic card — label, large value, optional delta with trend arrow, and an optional icon box — per the theme's StatCard recipe.
    static void StatCard(this UIView view, string label, string value, string[]? style = null, string? delta = null, StatTrend trend = Flat, string? trendLabel = null, string? icon = null, SemanticTone iconTone = Neutral, string[]? labelStyle = null, string[]? valueStyle = null, string[]? trendStyle = null, string[]? iconBoxStyle = null, string[]? iconStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Alignment of slides relative to the carousel viewport.
  enum CarouselAlign
    Start
    Center
    End
  // Responsive carousel configuration applied above a container-width threshold.
  sealed class CarouselBreakpoint : IEquatable<CarouselBreakpoint>
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
    // Horizontal or vertical carousel with optional navigation arrows and indicator dots.
    static void Carousel(this UIView view, string[]? style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, int? slidesPerView = null, int? slidesPerGroup = null, int? slideGapPx = null, IEnumerable<CarouselBreakpoint>? breakpoints = null, IEnumerable<CarouselSlideItem>? slides = null, bool? showArrows = null, bool? showIndicators = null, string? previousLabel = null, string? nextLabel = null, string? previousIconName = null, string? nextIconName = null, string[]? rootStyle = null, string[]? viewportStyle = null, string[]? slideStyle = null, string[]? previousStyle = null, string[]? nextStyle = null, string[]? indicatorsStyle = null, string[]? indicatorStyle = null, string[]? indicatorActiveStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<int, Task>? onIndexChange = null)
    // A single slide inside a Carousel . Use when rendering slides manually.
    static void Slide(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Declarative slide definition for Carousel .
  sealed class CarouselSlideItem : IEquatable<CarouselSlideItem>
    ctor(Action<UIView> Content, string? Key = null)
    // Builder function for rendering the slide.
    Action<UIView> Content { get; init; }
    // Optional stable key used for diffing.
    string? Key { get; init; }
  // Extension methods for the ChatLog primitive — the canonical chat-bubble layout shape: header + scrolling auto-scrolled body + composer. Wraps ScrollColumn with chat-friendly defaults so callers don't have to remember to set autoScroll: true.
  static class ChatLogExtensions
    // Renders a chat-style scrolling region: an optional pinned header (e.g. "Conversation"), a scrollable body that auto-scrolls to the bottom on change, and an optional pinned footer (typically the input row).
    static void ChatLog(this UIView view, string[]? style = null, int messageCount = 0, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, string? styleId = null, string? key = null)
  // Represents the checked state for checkbox-like components.
  enum CheckedState
    Unchecked
    Checked
    Indeterminate
  // Extension methods for the CodeEditor component.
  static class CodeEditorExtensions
    // Monospace code editor with an optional line-number gutter.
    static void CodeEditor(this UIView view, string? value = null, string? defaultValue = null, string? language = null, string? placeholder = null, bool? readOnly = null, bool? disabled = null, bool? showLineNumbers = null, int? tabSize = null, bool? insertSpaces = null, bool? wrap = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? gutterStyle = null, string[]? contentStyle = null, string[]? languageBadgeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  // Represents collision detection strategy for @dnd-kit.
  enum CollisionDetection
    ClosestCenter
    ClosestCorners
    RectIntersection
    PointerWithin
  // Output string format for ColorPicker .
  enum ColorFormat
    Hex
    Rgb
    Hsl
  // Extension methods for ColorPicker components.
  static class ColorPickerExtensions
    // Swatch-triggered color picker with hue slider, saturation/lightness square, and hex input.
    static void ColorPicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, ColorFormat format = Hex, bool? showAlpha = null, IReadOnlyList<string>? presets = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onValueCommit = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Horizontal alignment for a content grid or data table column.
  enum ColumnAlign
    Left
    Center
    Right
  // Event returned from a contact picker action with the selected contacts.
  sealed class ContactsActionEvent : ActionEvent, IEquatable<ContactsActionEvent>
    ctor(bool Success, IReadOnlyList<ClientContact>? Contacts)
    IReadOnlyList<ClientContact>? Contacts { get; init; }
  // Extension methods for container components.
  static class ContainerExtensions
    // Generic container element.
    static void Box(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null)
    // Box — positional (style, children) overload. Models reach for view.Box([style], v => {...}) with the lambda as the 2nd positional; without this overload it tries to bind to styleId (string?) and trips CS1660. The lambda parameter is named children (not content) so existing callers that use content: by name unambiguously match the original.
    static void Box(this UIView view, string[]? style, Action<UIView> children)
    // Container with vertical flexbox layout (flex-col).
    static void Column(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Column — positional (style, children) overload.
    static void Column(this UIView view, string[]? style, Action<UIView> children)
    // Container with flexbox layout enabled.
    static void Flex(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Flex — positional (style, children) overload (see Box ).
    static void Flex(this UIView view, string[]? style, Action<UIView> children)
    // Container with CSS grid layout enabled.
    static void Grid(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Grid — positional (style, children) overload.
    static void Grid(this UIView view, string[]? style, Action<UIView> children)
    // Absolutely positioned layer within a Stack container.
    static void Layer(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Layer — positional (style, children) overload (see Box ).
    static void Layer(this UIView view, string[]? style, Action<UIView> children)
    // Container with horizontal flexbox layout (flex-row).
    static void Row(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Row — positional (style, children) overload (see Box ).
    static void Row(this UIView view, string[]? style, Action<UIView> children)
    // Loading spinner — an animated circular indicator for async/pending states. A typed convenience over the spin utility classes (equivalent to a div with the Theming.Icon.Spinner style): render it while waiting on data, e.g. if (_loading.Value) { view.Spinner(); }. Override colour/size via the style array; the default tracks the theme's muted foreground.
    static void Spinner(this UIView view, string[]? style = null, SpinnerSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Container for layering children on top of each other. Use with Layer components as children.
    static void Stack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Stack — positional (style, children) overload (see Box ).
    static void Stack(this UIView view, string[]? style, Action<UIView> children)
  // Defines a column in a content grid including optional header, width, flex, and alignment.
  class ContentGridColumn : IEquatable<ContentGridColumn>
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
  sealed class CopyToClipboardActionOptions : ActionOptions, IEquatable<CopyToClipboardActionOptions>
    // The text to copy.
    string Text { get; init; }
  // Extension methods for core UI components including buttons, toggles, text inputs, dialogs, and typography.
  static class CoreExtensions
    // Button that triggers a client-side action (e.g., clipboard, download). Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    static void ActionButton(this UIView view, string[]? style = null, ActionKind action = Unknown, string? text = null, ActionOptions? options = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<ActionEvent, Task>? onActionComplete = null, Action<UIView>? content = null)
    // Clickable button that triggers an action. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    static void Button(this UIView view, string[]? style = null, string? text = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null)
    // Button — positional-text-first overload. Same rationale as the matching Text overload — avoids CS1744 when models write view.Button("Sign in", onClick: …). First parameter is named buttonText to avoid ambiguity with callers using Button(text: "...") by name.
    static void Button(this UIView view, string buttonText, string[]? style = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null)
    // Semantic heading element for titles and section headers.
    static void Heading(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Heading — positional-text-first overload, same rationale as the matching Text overload: view.Heading("Settings", style: [Text.H2]) is the shape models reach for. Parameter is named headingText to avoid ambiguity with callers using text: by name.
    static void Heading(this UIView view, string headingText, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Renders an icon from an icon library.
    static void Icon(this UIView view, string[]? style = null, string? name = null, string? library = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Icon — positional-name-first overload. Same rationale as the matching Text overload: view.Icon("check", style: [Icon.Sm]) is the shape models reach for. Parameter is named iconName to avoid ambiguity with callers using name: by name.
    static void Icon(this UIView view, string iconName, string[]? style = null, string? library = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Inline anchor link — sugar for a `Button` styled like a hyperlink with an `href`. Mirrors HTML anchor semantics. By default opens in the same tab; pass target: "_blank" to open in a new tab (we automatically add `rel="noopener noreferrer"` for `_blank` if no other `rel` is provided). Generated code naturally reaches for `view.Link(text:, href:)`; this gives it the canonical shape rather than forcing every link into `view.Button(href:, …)`.
    static void Link(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Link — positional-text-first overload. Same rationale as the matching Text overload: view.Link("Docs", href: "https://…") is the shape models reach for. Parameter is named linkText to avoid ambiguity with callers using text: by name.
    static void Link(this UIView view, string linkText, string[]? style = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Renders markdown content with formatting support.
    static void Markdown(this UIView view, string[]? style = null, string? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Markdown — positional-content-first overload: view.Markdown("# Hello"). Parameter is named markdownContent to avoid ambiguity with callers using content: by name.
    static void Markdown(this UIView view, string markdownContent, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Text element for displaying content.
    static void Text(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Text element — positional-text-first overload. Models trained on shadcn / Radix / React conventions reach for view.Text("Hello", style: ["text-xl"]) rather than the view.Text(["text-xl"], "Hello") ordering. Without this overload, the positional string argument fails to bind to the original signature's first parameter (string[]? style), producing CS1744 / CS1503 — the most common compile error in the codegen benchmark. Parameter is named textContent (not text) to avoid ambiguity with existing callers that use Text(text: "...") by name.
    static void Text(this UIView view, string textContent, string[]? style = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Single toggle button.
    static void Toggle(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null)
    // Item within a toggle group.
    static void ToggleGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggle group with multiple selection.
    static void ToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggle group with single selection.
    static void ToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
  // Represents the text direction for DirectionProvider.
  enum Dir
    Ltr
    Rtl
  // Extension methods for Accordion and Collapsible components.
  static class DisclosureExtensions
    // Content for an accordion item, collapsed or expanded.
    static void AccordionContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Wraps an AccordionTrigger.
    static void AccordionHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for an accordion item.
    static void AccordionItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Accordion with multiple items open at a time.
    static void AccordionMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Accordion with single item open at a time.
    static void AccordionSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? collapsible = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggles the collapsed state of an accordion item.
    static void AccordionTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Expandable/collapsible container.
    static void Collapsible(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Content that is shown or hidden.
    static void CollapsibleContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggles the collapsed state.
    static void CollapsibleTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Options for downloading a file to the client.
  sealed class DownloadFileActionOptions : ActionOptions, IEquatable<DownloadFileActionOptions>
    ctor()
    // Binary data to download. When set, Url is auto-generated as a data URL.
    byte[]? Data { get; init; }
    // Suggested filename for the downloaded file.
    string? Filename { get; init; }
    // MIME type for binary data (e.g. "image/png"). Optional — defaults to "application/octet-stream" when Data is set without a MIME type.
    string? MimeType { get; init; }
    // URL to download. Can be a regular URL or a data URL. If Data is provided, this is auto-generated from the binary data using MimeType , falling back to "application/octet-stream" when MimeType is unset so the download still fires.
    string Url { get; init; }
  // Extension methods for drag and drop components.
  static class DragAndDropExtensions
    // Root context for drag and drop operations.
    static void DndContext(this UIView view, string[]? style = null, CollisionDetection collisionDetection = ClosestCenter, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<DragStartArgs, Task>? onDragStart = null, Func<DragMoveArgs, Task>? onDragMove = null, Func<DragOverArgs, Task>? onDragOver = null, Func<DragEndArgs, Task>? onDragEnd = null, Func<Task>? onDragCancel = null, int? activationDistance = null, Action<UIView>? content = null)
    // Overlay shown while dragging.
    static void DragOverlay(this UIView view, string[]? style = null, bool? dropAnimation = true, string? activeDragId = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Element that can be dragged.
    static void Draggable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, bool? hideOnDrag = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Drop target area.
    static void Droppable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Context for sortable list operations.
    static void SortableContext(this UIView view, string[]? style = null, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Drag handle for a SortableItem. When a SortableHandle descendant is present, only pointerdown on the handle starts a drag; the rest of the item remains free for inner clickable elements like buttons. Place inside a SortableItem (or a SortableList itemContent). Outside a SortableItem the handle renders as a plain container.
    static void SortableHandle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Sortable item within a SortableContext.
    static void SortableItem(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // SortableList component that auto-handles reordering.
    static void SortableList(this UIView view, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, CollisionDetection collisionDetection = ClosestCenter, Func<SortableReorderArgs, Task>? onReorder = null, Func<DragStartArgs, Task>? onDragStart = null, Action<UIView, string>? itemContent = null, string[]? listStyle = null, string[]? itemStyle = null, int? activationDistance = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Event args for drag cancel in @dnd-kit.
  sealed class DragCancelArgs : IEquatable<DragCancelArgs>
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for drag end in @dnd-kit.
  sealed class DragEndArgs : IEquatable<DragEndArgs>
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag move in @dnd-kit.
  sealed class DragMoveArgs : IEquatable<DragMoveArgs>
    ctor(string ActiveId, double DeltaX, double DeltaY)
    string ActiveId { get; init; }
    double DeltaX { get; init; }
    double DeltaY { get; init; }
  // Event args for drag over in @dnd-kit.
  sealed class DragOverArgs : IEquatable<DragOverArgs>
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag start in @dnd-kit.
  sealed class DragStartArgs : IEquatable<DragStartArgs>
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for escape key down events on overlays.
  sealed class EscapeKeyDownArgs : IEquatable<EscapeKeyDownArgs>
    ctor()
  // Per-client expanded-node state for TreeView — a reactive set of expanded node ids, so apps don't hand-roll revision counters or per-node booleans.
  sealed class ExpandedSet
    // Create the set, optionally pre-expanding the given node ids for every client.
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
    None
    Image
    Video
    VideoFull
  // Extension methods for the FeedScroller component — a vertically-snapping, full-viewport feed optimized for media-heavy content (TikTok / Reels / Shorts-style).
  static class FeedScrollerExtensions
    // Renders a TikTok-style vertical feed: each slide occupies the viewport and snaps into place.
    static void FeedScroller(this UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[]? style = null, string[]? slideStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onActiveChange = null, Func<double, Task>? onScrollNearEnd = null, Func<bool, Task>? onMuteChange = null)
    // A single slide inside a FeedScroller . Use when rendering slides manually rather than via the FeedSlide declarative API.
    static void FeedSlide(this UIView view, int index, string[]? style = null, FeedMediaKind mediaKind = None, string? mediaUrl = null, string? mediaPoster = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // A single slide in a FeedScroller .
  sealed class FeedSlide : IEquatable<FeedSlide>
    ctor(Action<UIView> Content, string? Key = null, FeedMediaKind MediaKind = None, string? MediaUrl = null, string? MediaPoster = null)
    // Builder invoked to render the slide. Only slides inside the render window are realized.
    Action<UIView> Content { get; init; }
    // Stable key used for diffing and preload identity. Defaults to slide index.
    string? Key { get; init; }
    // Kind of media the slide needs preloaded.
    FeedMediaKind MediaKind { get; init; }
    // Optional poster image URL for video slides.
    string? MediaPoster { get; init; }
    // URL of the media asset matching MediaKind .
    string? MediaUrl { get; init; }
  // Extension methods for file picker components. Unlike FileUpload , a FilePicker only opens the native file picker and reports selected file metadata to the server — it does not transfer bytes. The picked File handles are cached on the client and uploaded later by a FileUpload rendered with a matching seedSelectionIds prop.
  static class FilePickerExtensions
    // Native file picker. Emits onFileSelected once per selected file with its metadata (name, mime, size, client-generated selection id). The File bytes stay on the client and are not transferred until a FileUpload with matching seedSelectionIds is mounted.
    static void FilePicker(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FilePickerSelectedArgs, Task>? onFileSelected = null, Func<FilePickerValidationErrorArgs, Task>? onValidationError = null, Action<UIView>? content = null)
  // Metadata for a file chosen in a FilePicker . The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
  sealed class FilePickerSelectedArgs : IEquatable<FilePickerSelectedArgs>
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  // Reported when client-side validation rejects a picked file (e.g. file too large for maxFileSize). Host UIs should surface Reason to the user — without a handler the rejection is silent and the user just sees "nothing happened" after clicking the picker.
  sealed class FilePickerValidationErrorArgs : IEquatable<FilePickerValidationErrorArgs>
    ctor(string FileName, string MimeType, long Size, string Reason)
    string FileName { get; init; }
    string MimeType { get; init; }
    string Reason { get; init; }
    long Size { get; init; }
  // Extension methods for file upload components.
  static class FileUploadExtensions
    // File upload component with explicit upload area, button click, drag-drop, and paste support.
    static void FileUpload(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string? capture = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, string[]? seedSelectionIds = null, Action<UIView>? content = null)
    // Wrapper component that adds file upload capability (drag-drop + paste) to any content. Children define the visual appearance.
    static void FileUploadZone(this UIView view, string[]? style = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string[]? accept = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, Func<bool, Task>? onDragActiveChange = null, Action<UIView>? content = null, string[]? zoneStyle = null, string[]? activeStyle = null, string? activeStyleId = null, string? styleId = null, string? key = null, string[]? seedSelectionIds = null, IReadOnlyDictionary<string, object>? props = null)
  // Extension methods for focus hint management.
  static class FocusHintExtensions
    // Requests focus attention for a UI element, typically for accessibility announcements.
    static void FocusHint(this UIView view, FocusHintProps props, string? key = null, Guid? targetViewId = null)
  // Configuration for a focus hint request including priority, ranking, and cooldown behavior.
  sealed class FocusHintProps : IEquatable<FocusHintProps>
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
  sealed class FocusOutsideArgs : IEquatable<FocusOutsideArgs>
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Priority level for focus hint announcements, matching ARIA live region politeness.
  enum FocusPriority
    Polite
    Assertive
  // Extension methods for Form, Checkbox, RadioGroup, Switch, Slider, and Label components.
  static class FormExtensions
    // Checkbox control with simple boolean state. For tri-state support (indeterminate), use TriStateCheckbox .
    static void Checkbox(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null)
    // Visual indicator for the checkbox state.
    static void CheckboxIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Form container with validation support.
    static void Form(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClearServerErrors = null, Action<UIView>? content = null)
    // Wraps the input control.
    static void FormControl(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for a form field with label and validation.
    static void FormField(this UIView view, string[]? style = null, string? name = null, bool? serverInvalid = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Label for a form field.
    static void FormLabel(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Validation message for a form field.
    static void FormMessage(this UIView view, string[]? style = null, FormMessageMatch? match = null, bool? forceMatch = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Submit button for the form.
    static void FormSubmit(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Accessible label for form controls.
    static void Label(this UIView view, string[]? style = null, string? htmlFor = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for radio buttons.
    static void RadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<string>? bind = null)
    // Visual indicator for the selected radio.
    static void RadioGroupIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual radio button.
    static void RadioGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, bool? required = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Range slider control.
    static void Slider(this UIView view, string[]? style = null, IReadOnlyList<double>? value = null, IReadOnlyList<double>? defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<double>, Task>? onValueChange = null, Func<IReadOnlyList<double>, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null, Reactive<double>? bind = null)
    // Single-thumb slider with a scalar value — the common case. Sugar over the list form so callers write Slider(value: 50, onValueChange: async v => …) without the one-element-list dance. Use the list form for multi-thumb ranges.
    static void Slider(this UIView view, double value, string[]? style = null, double? min = null, double? max = null, double? step = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onValueChange = null, Func<double, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null)
    // Filled range portion of the slider.
    static void SliderRange(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Draggable thumb on the slider.
    static void SliderThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Track for the slider.
    static void SliderTrack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggle switch control.
    static void Switch(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null)
    // The thumb that moves when the switch is toggled.
    static void SwitchThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Checkbox control with tri-state support (checked, unchecked, indeterminate).
    static void TriStateCheckbox(this UIView view, string[]? style = null, CheckedState? value = null, CheckedState? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onValueChange = null, Action<UIView>? content = null)
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
  // Hour display format for TimePicker .
  enum HourFormat
    Hour24
    Hour12
  // Event returned from an image capture action with the captured image data.
  sealed class ImageCaptureActionEvent : ActionEvent, IEquatable<ImageCaptureActionEvent>
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string? Data { get; init; }
    int Height { get; init; }
    string? Mime { get; init; }
    int Width { get; init; }
  // Extension methods for image and avatar components.
  static class ImageExtensions
    // Avatar container with image and fallback.
    static void Avatar(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Fallback content shown when image fails to load.
    static void AvatarFallback(this UIView view, string[]? style = null, int? delayMs = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Image element for the avatar.
    static void AvatarImage(this UIView view, string[]? style = null, string? src = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onLoadingStatusChange = null)
    // Image element.
    static void Image(this UIView view, string[]? style = null, string? src = null, AssetUri? assetUri = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
    // Image element with binary data payload.
    static void Image(this UIView view, string[]? style = null, byte[]? data = null, string? mimeType = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
  // Extension methods for input components (TextField, TextArea, OTP, Password).
  static class InputExtensions
    // One-time password input field.
    static void OtpField(this UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string? label = null)
    // Individual input slot for OTP.
    static void OtpFieldInput(this UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Password input with visibility toggle.
    static void PasswordToggleField(this UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string? label = null)
    // Icon that changes based on visibility state.
    static void PasswordToggleFieldIcon(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null)
    // The password input element.
    static void PasswordToggleFieldInput(this UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Button to toggle password visibility.
    static void PasswordToggleFieldToggle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Multi-line text input area.
    static void TextArea(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null)
    // Single-line text input field.
    static void TextField(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, bool? multiline = null, int? rows = null)
  // Event args for interact outside events on overlays (combines pointer and focus).
  sealed class InteractOutsideArgs : IEquatable<InteractOutsideArgs>
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // String constants for common keyboard key names, matching the browser KeyboardEvent.key specification. Use these with KeyboardListener for type-safe key filtering. Raw strings can also be used for uncommon keys not listed here.
  static class Key
    static string Alt
    static string ArrowDown
    static string ArrowLeft
    static string ArrowRight
    static string ArrowUp
    static string Backspace
    static string Control
    static string Delete
    static string End
    static string Enter
    static string Escape
    static string F1
    static string F10
    static string F11
    static string F12
    static string F2
    static string F3
    static string F4
    static string F5
    static string F6
    static string F7
    static string F8
    static string F9
    static string Home
    static string Meta
    static string PageDown
    static string PageUp
    static string Shift
    static string Space
    static string Tab
  // Event args for keyboard events, matching the browser KeyboardEvent properties.
  sealed class KeyboardEventArgs : IEquatable<KeyboardEventArgs>
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
    static void KeyboardListener(this UIView view, Func<KeyboardEventArgs, Task>? onKeyDown = null, Func<KeyboardEventArgs, Task>? onKeyUp = null, IReadOnlyList<string>? keys = null, bool? global = true, bool? preventDefault = null, bool? stopPropagation = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Extension methods for scroll area and layout components.
  static class LayoutExtensions
    // Maintains a specific aspect ratio for content.
    static void AspectRatio(this UIView view, string[]? style = null, double ratio = 1, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Provides text direction context (ltr/rtl) to descendants.
    static void DirectionProvider(this UIView view, string[]? style = null, Dir dir = Ltr, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Infinite scroll view that fires callbacks when user scrolls near the end.
    static void InfiniteScrollView(this UIView view, string[]? style = null, int threshold = 200, int debounceMs = 100, bool loading = false, bool hasMore = true, ScrollDirection direction = Down, ScrollAreaScrollbars scrollbars = Vertical, Action<UIView>? loadingIndicator = null, Func<ScrollNearEndArgs, Task>? onNearEnd = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Progress component that auto-renders the indicator with transform. Success , Warning , and Error tones map to the matching Theming.Progress.Variant tokens; other tones use the default (brand) fill.
    static void Progress(this UIView view, string[]? style = null, double? value = null, double? max = null, SemanticTone tone = Neutral, bool indeterminate = false, Func<double?, string>? getValueLabel = null, string[]? rootStyle = null, string[]? indicatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Resizable split panel with a drag handle between two panes. Resize is handled entirely on the client — only the final size is sent to the server via onResized .
    static void ResizableSplit(this UIView view, Orientation orientation = Horizontal, double initialSize = 200, double minSize = 100, double maxSize = 500, bool reversed = false, Func<double, Task>? onResized = null, Action<UIView>? first = null, Action<UIView>? second = null, string[]? style = null, string[]? handleStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // ScrollArea component that auto-renders viewport and scrollbars.
    static void ScrollArea(this UIView view, string[]? style = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType type = Hover, int? scrollHideDelay = null, Dir dir = Ltr, bool autoScroll = false, string? autoScrollKey = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? cornerStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // ScrollArea — positional (style, children) overload (see Box ).
    static void ScrollArea(this UIView view, string[]? style, Action<UIView> children)
    // Visual separator between content.
    static void Separator(this UIView view, string[]? style = null, Orientation orientation = Horizontal, bool decorative = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Event returned from a geolocation action with latitude/longitude coordinates.
  sealed class LocationActionEvent : ActionEvent, IEquatable<LocationActionEvent>
    ctor(bool Success, double? Latitude, double? Longitude, double? Accuracy)
    double? Accuracy { get; init; }
    double? Latitude { get; init; }
    double? Longitude { get; init; }
  // Specifies the behavior of a CaptureButton when pressed.
  enum MediaCaptureButtonMode
    Hold
    Toggle
  // Event data for media capture start/stop callbacks, containing the stream identifier and capture kind. ClientContext identifies the user who initiated the capture and is populated for all capture kinds (audio, camera, screen). Prefer reading ClientSessionId / UserId rather than tracking streamId-to-client mappings yourself.
  sealed class MediaCaptureEvent : IEquatable<MediaCaptureEvent>
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
    Audio
    Camera
    Screen
  // Extension methods for media playback components.
  static class MediaExtensions
    // Audio player for URL-based audio content.
    static void AudioUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, string? preload = null, string? styleId = null, string? key = null)
    // Button that captures media (audio, camera, or screen) based on the specified kind. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    static void CaptureButton(this UIView view, string[]? style = null, MediaCaptureKind kind = Audio, string? text = null, MediaCaptureButtonMode captureMode = Hold, ClientAudioCaptureOptions? audioOptions = null, ClientVideoCaptureOptions? videoOptions = null, int? holdReleaseDelayMs = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Push-to-talk microphone button: a CaptureButton(kind: Audio, captureMode: Hold) that integrates with SpeechRecognizedAsync . After enabling speech recognition once (Audio.UseSpeechRecognition(...)), subscribe to Audio.SpeechRecognizedAsync to receive transcriptions when the user releases the button. The user's client context is carried on the event args — no streamId-to-client plumbing needed in the app.
    static void PushToTalkButton(this UIView view, string[]? style = null, string? text = "⏺", int holdReleaseDelayMs = 500, ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Canvas element for rendering a live video stream.
    static void VideoStreamCanvas(this UIView view, string[]? style = null, string? streamId = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
    // Video player for URL-based video content.
    static void VideoUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, bool? playsInline = null, string? poster = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  // Extension methods for NavigationMenu, Menubar, and Toolbar components.
  static class NavigationExtensions
    // Menubar root container.
    static void Menubar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Checkbox item in menu.
    static void MenubarCheckboxItem(this UIView view, string[]? style = null, CheckedState isChecked = Unchecked, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onCheckedChange = null, Action<UIView>? content = null)
    // Dropdown content for the menu.
    static void MenubarContent(this UIView view, string[]? style = null, bool loop = true, Side side = Bottom, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Clickable menu item.
    static void MenubarItem(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // Visual indicator for checkbox/radio state.
    static void MenubarItemIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual menu in the menubar.
    static void MenubarMenu(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Radio group in menu.
    static void MenubarRadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Radio item in menu.
    static void MenubarRadioItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Separator between menu items.
    static void MenubarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Submenu container.
    static void MenubarSub(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Content for submenu.
    static void MenubarSubContent(this UIView view, string[]? style = null, bool loop = true, Side side = Right, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Trigger for submenu.
    static void MenubarSubTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Button that opens a menu.
    static void MenubarTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Navigation menu root.
    static void NavigationMenu(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, int? delayDuration = null, int? skipDelayDuration = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Content shown when navigation item is active.
    static void NavigationMenuContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Visual indicator for active navigation item.
    static void NavigationMenuIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual navigation menu item.
    static void NavigationMenuItem(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Link within navigation menu.
    static void NavigationMenuLink(this UIView view, string[]? style = null, bool? active = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // List of navigation menu items.
    static void NavigationMenuList(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Trigger that opens navigation content.
    static void NavigationMenuTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Viewport for navigation menu content.
    static void NavigationMenuViewport(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toolbar container.
    static void Toolbar(this UIView view, string[]? style = null, Orientation orientation = Horizontal, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Button in the toolbar.
    static void ToolbarButton(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null)
    // Link in the toolbar.
    static void ToolbarLink(this UIView view, string[]? style = null, string? href = null, string? target = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Separator in the toolbar.
    static void ToolbarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Multi-select toggle group in toolbar.
    static void ToolbarToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Single-select toggle group in toolbar.
    static void ToolbarToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggle item in toolbar toggle group.
    static void ToolbarToggleItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Represents the orientation for components like Tabs, Slider, etc.
  enum Orientation
    Horizontal
    Vertical
  // Overlay components (Dialog, AlertDialog, Popover, Tooltip, HoverCard, Toast). Each handles Portal/Overlay management automatically.
  static class OverlayExtensions
    // Alert dialog that requires explicit user acknowledgment. Cannot be dismissed by clicking outside.
    static void AlertDialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? title = null, string? description = null, string? cancelLabel = null, string? actionLabel = null, Func<Task>? onAction = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? cancelStyle = null, string[]? actionStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Modal dialog window.
    static void Dialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? headerStyle = null)
    // Rich content card that appears on hover with configurable delays.
    static void HoverCard(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Floating content panel that appears next to a trigger element.
    static void Popover(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Toast notification with built-in provider and viewport.
    static void Toast(this UIView view, string[]? style = null, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string? title = null, string? description = null, bool? showClose = null, string? closeLabel = null, Action<UIView>? content = null, string[]? toastStyle = null, string[]? viewportStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Func<Task>? onEscapeKeyDown = null, Func<Task>? onPause = null, Func<Task>? onResume = null, Func<ToastSwipeArgs, Task>? onSwipeStart = null, Func<ToastSwipeArgs, Task>? onSwipeMove = null, Func<ToastSwipeArgs, Task>? onSwipeEnd = null, Func<ToastSwipeArgs, Task>? onSwipeCancel = null)
    // Brief informational message that appears on hover. Includes built-in provider.
    static void Tooltip(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
  // One page of items plus the controls needed to render prev/next buttons. Returned by Paginate .
  sealed class Page<T> : IEquatable<Page<T>>
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
  // Bounded-cursor primitive on top of ClientReactive . Slices an in-memory list, returns the slice + bound actions (Prev/Next/JumpTo/First/Last) the caller binds to whatever UI fits. Holds zero rendering opinion — no tab bars, no default control rows, no opinionated layout. Most Ikon apps don't need pagination at all (live feeds, autoscroll, virtualization handle the common cases via Reactive<List<T>> + ScrollArea(autoScroll: true)). Use this when you have a static list large enough to warrant explicit page navigation. For DB-backed pagination (load only the current page from a backend), drive ClientReactive directly and observe its value in your data-loading code — same per-client semantics, no special helper needed.
  static class PaginationExtensions
    // Slice items by pageSize using page as per-client current-page state. Each connected client sees its own page; setting page from one client doesn't shift another client's view.
    static Page<T> Paginate<T>(this UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  // Options for the Contact Picker API action.
  sealed class PickContactsActionOptions : ActionOptions, IEquatable<PickContactsActionOptions>
    ctor()
    // When true, allows selecting multiple contacts.
    bool Multiple { get; init; }
  // Event args for pointer down outside events on overlays.
  sealed class PointerDownOutsideArgs : IEquatable<PointerDownOutsideArgs>
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Extension methods for QR code generation.
  static class QrCodeExtensions
    // QR code image. Generates a QR code server-side and renders it as an image.
    static void QR(this UIView view, string[]? style = null, string? value = null, int size = 256, string? key = null)
  // Extension methods for the RichTextEditor component.
  static class RichTextEditorExtensions
    // Inline rich-text editor with a configurable toolbar. Values are HTML strings.
    static void RichTextEditor(this UIView view, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, IReadOnlyList<RichTextTool>? tools = null, bool? showToolbar = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? toolbarStyle = null, string[]? toolbarButtonStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  // Formatting action available in the RichTextEditor toolbar.
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
  // Tiny primitives for using ClientReactive as a signal the app reads to decide what to render. Routes, tabs, modes, panel selections, "which dialog is open" — same shape, same primitives. Intentionally minimal: no opinionated tab bars, no URL coupling, no rendering bias. The signal is the building block; the app decides how to consume it. For URL ↔ signal sync (browser bar, deep links, back/forward), use Navigation on the host app — keeps URL concerns in one place instead of forking them through this layer.
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
  // Extension methods for the ScrollColumn primitive — a header/body/footer dialog pattern where the body scrolls. Wraps a ScrollArea with the correct flex sizing so scrolling engages without ceremony.
  static class ScrollColumnExtensions
    // Renders a flex column with an optional header, a scrollable body, and an optional footer. The header and footer stay pinned; the body scrolls.
    static void ScrollColumn(this UIView view, string[]? style = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType scrollType = Hover, bool autoScroll = false, string? autoScrollKey = null, string[]? bodyStyle = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string? styleId = null, string? key = null)
  // Direction for infinite scroll loading.
  enum ScrollDirection
    Down
    Up
  // Event args for when user scrolls near the end of content.
  sealed class ScrollNearEndArgs : IEquatable<ScrollNearEndArgs>
    ctor(double ScrollTop, double ScrollHeight, double ClientHeight, ScrollDirection Direction)
    double ClientHeight { get; init; }
    ScrollDirection Direction { get; init; }
    double ScrollHeight { get; init; }
    double ScrollTop { get; init; }
  // Extension methods for Select components.
  static class SelectExtensions
    // Select dropdown component that auto-renders the full structure with trigger button, dropdown content, and items. Use either options (flat list) or groups (grouped items) - not both.
    static void Select(this UIView view, string[]? style = null, IReadOnlyList<SelectOption>? options = null, IReadOnlyList<SelectOptionGroup>? groups = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, bool? required = null, bool? open = null, string? name = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? itemStyle = null, string[]? itemIndicatorStyle = null, string? indicatorIconName = "check", string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null, Reactive<string>? bind = null)
  // Represents a selectable option in a Select component.
  sealed class SelectOption : IEquatable<SelectOption>
    ctor(string Value, string Label, bool Disabled = false)
    bool Disabled { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  // Represents a group of selectable options in a Select component.
  sealed class SelectOptionGroup : IEquatable<SelectOptionGroup>
    ctor(string? Label, IReadOnlyList<SelectOption> Options)
    string? Label { get; init; }
    IReadOnlyList<SelectOption> Options { get; init; }
  // Semantic tone shared by the app-chrome components (Badge, Alert, Toasts, StatCard icon box). Tones map to the theme's semantic color tokens, so they are correct in both light and dark mode.
  enum SemanticTone
    Neutral
    Brand
    Success
    Warning
    Error
    Info
  // A typed uniform value to pass to a WebGL shader. Use the static factory methods to create instances.
  struct ShaderUniform
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
    // Shadertoy-compatible WebGL fragment shader canvas.
    static void ShadertoyCanvas(this UIView view, string[]? style = null, string? shaderSource = null, int? fps = null, IReadOnlyDictionary<string, ShaderUniform>? uniforms = null, IReadOnlyList<string>? channels = null, bool? enableMouse = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  // Options for the Web Share API action.
  sealed class ShareActionOptions : ActionOptions, IEquatable<ShareActionOptions>
    ctor()
    // Text body for the shared content.
    string? Text { get; init; }
    // Title for the shared content.
    string? Title { get; init; }
    // URL to share.
    string? Url { get; init; }
  // Slide-over panel composites (Sheet, Drawer) built on the Dialog primitive. The dialog's portal + content styling is repositioned per side via the Theming.Sheet / Theming.Drawer token recipes, including Crosswind slide-in/out motion classes driven by the panel's data-state attribute.
  static class SheetExtensions
    // Bottom drawer on top of the Dialog primitive — mobile-style rounded panel with a drag handle, per the Theming.Drawer token recipe. Same open/close model as Sheet : in controlled mode pass onOpenChange to actually close.
    static void Drawer(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showHandle = true, string[]? style = null, string[]? overlayStyle = null, string[]? handleStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string? key = null)
    // Side-anchored slide-over panel on top of the Dialog primitive. With zero style args the panel uses Theming.Sheet.Base plus the side token (position, border, slide animation); caller styles merge on top, and the literal "unstyled" class opts out. In controlled mode ( open set) pass onOpenChange and flip your state to false there, or the built-in close button and outside clicks cannot dismiss the sheet.
    static void Sheet(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, Side side = Right, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showClose = true, string[]? style = null, string[]? overlayStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? closeStyle = null, string? key = null)
  // Represents the side for positioning overlays.
  enum Side
    Top
    Right
    Bottom
    Left
  // Extension methods for the Skeleton component.
  static class SkeletonExtensions
    // Pulsing placeholder block for loading / not-yet-available content — the visual stand-in used while real content is pending, and the default fill for content redacted from the build-time boot snapshot (see SnapshotReveal). A typed convenience over the Skeleton.* theme tokens (a div with animate-pulse styling); size and shape via size / shape , or override freely through style .
    static void Skeleton(this UIView view, string[]? style = null, SkeletonShape shape = Rectangle, SkeletonSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Outline shape of a Skeleton placeholder.
  enum SkeletonShape
    Rectangle
    Circle
    Square
  // Height preset for a Skeleton placeholder.
  enum SkeletonSize
    Xs
    Sm
    Md
    Lg
    Xl
  // Wrappers for controlling how the UI renders into the build-time boot snapshot. The boot snapshot is a public asset painted to everyone before the live connection, so by default the snapshot render replaces every content leaf with a skeleton — per-user content can never leak. These wrappers let the app override that default for specific regions, branching on IsSnapshot at build time so it keeps a single UI.Root definition instead of two separate UIs. On the normal live render path every wrapper is a single bool check plus the content the developer already wrote.
  static class SnapshotExtensions
    // Renders content live, but omits it entirely from the boot snapshot — use to keep a region out of the public snapshot without leaving even a skeleton (e.g. interactive controls that are dead before the live connection).
    static void SnapshotHide(this UIView view, Action<UIView> content)
    // Renders content only in the boot snapshot, never live — use for snapshot-specific filler (e.g. a curated first-paint placeholder) that should disappear once the live UI takes over. The filler is rendered as authored (not auto-skeletonized), since it is the developer's own snapshot stand-in.
    static void SnapshotOnly(this UIView view, Action<UIView> content)
    // Opts content out of automatic skeletonization: it renders as real content in the boot snapshot instead of being replaced with skeletons. Use only for content that is safe to bake into the public snapshot (logos, static chrome, marketing copy). The opt-out applies to the whole subtree — nested containers and leaves all render their real content. IsSnapshot stays true inside the region, so this means "show real content here", not "render as if live".
    static void SnapshotReveal(this UIView view, Action<UIView> content)
  // Represents sort strategy for @dnd-kit SortableContext.
  enum SortStrategy
    VerticalList
    HorizontalList
  // Contains information about a reorder operation in SortableList.
  sealed class SortableReorderArgs : IEquatable<SortableReorderArgs>
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
  // Trend direction for a StatCard delta.
  enum StatTrend
    Flat
    Up
    Down
  // Represents sticky behavior for Select/DropdownMenu.
  enum Sticky
    Partial
    Always
  // Defines a tab for use with the Tabs component.
  class TabItem : IEquatable<TabItem>
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
    // Table container (CSS display: table). Compose with TableHeader , TableBody , TableRow , TableHead , and TableCell . Caller styles merge on top of the base token; include the literal "unstyled" class to opt out.
    static void Table(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Table — positional (style, children) overload.
    static void Table(this UIView view, string[]? style, Action<UIView> children)
    // Body row group (CSS display: table-row-group).
    static void TableBody(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Data cell (CSS display: table-cell).
    static void TableCell(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // TableCell — positional-text-first overload: r.TableCell(user.Name).
    static void TableCell(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // Header cell (CSS display: table-cell) with muted uppercase column-label styling.
    static void TableHead(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // TableHead — positional-text-first overload: r.TableHead("Name").
    static void TableHead(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // Header row group (CSS display: table-header-group). Put one TableRow of TableHead cells inside.
    static void TableHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Table row (CSS display: table-row) with a bottom border. Rows with onClick also get hover highlight + pointer cursor.
    static void TableRow(this UIView view, string[]? style = null, bool striped = false, Delegate? onClick = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
  // Extension methods for Tabs components.
  static class TabsExtensions
    // Container for Tabs components. Use the 'tabs' parameter to define tab content.
    static void Tabs(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, ActivationMode activationMode = Automatic, IEnumerable<TabItem>? tabs = null, string[]? listContainerStyle = null, string[]? listStyle = null, string[]? triggerStyle = null, string[]? disabledTriggerStyle = null, string[]? contentContainerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null)
  // Smallest time unit shown by a TimePicker .
  enum TimeGranularity
    Hour
    Minute
    Second
  // Extension methods for TimePicker components.
  static class TimePickerExtensions
    // Picker for a time of day. Values are ISO-8601 HH:mm or HH:mm:ss strings.
    static void TimePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, HourFormat hourFormat = Hour24, TimeGranularity granularity = Minute, int? minuteStep = null, int? secondStep = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string? placeholder = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? columnStyle = null, string[]? itemStyle = null, string[]? itemSelectedStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // One notification held by a Toasts queue.
  sealed class ToastItem : IEquatable<ToastItem>
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
  sealed class ToastSwipeArgs : IEquatable<ToastSwipeArgs>
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
  // Imperative per-client toast queue so app code never owns notification state.
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
    long Show(string title, string? description = null, SemanticTone tone = Neutral, int durationMs = 5000)
    // Enqueue a success toast.
    long Success(string title, string? description = null, int durationMs = 5000)
    // Enqueue a warning toast.
    long Warning(string title, string? description = null, int durationMs = 5000)
    // Default auto-dismiss duration in milliseconds.
    static int DefaultDurationMs
  // Host composite that renders a Toasts queue with the toast primitives.
  static class ToastsExtensions
    // Render the toast viewport for a Toasts queue. Mount exactly once in the root UI; every queued toast renders as a themed toast (tone icon, title, description, close button) that the client auto-dismisses after its duration. Both auto-dismiss and the close button report back and remove the item from the queue.
    static void ToastHost(this UIView view, Toasts toasts, string[]? viewportStyle = null, string[]? toastStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, bool showClose = true)
  // Recursive tree composite over the Collapsible primitive, styled with the NavPanel/NavItem token recipes.
  static class TreeViewExtensions
    // Hierarchical tree view. Branch nodes render as Collapsibles whose trigger row toggles expansion (tracked in expanded ) and reports selection; leaf nodes are plain clickable rows. The row matching selectedId renders with the active item style.
    static void TreeView<T>(this UIView view, IReadOnlyList<T> roots, Func<T, string> id, Func<T, string> label, Func<T, IReadOnlyList<T>?> children, ExpandedSet expanded, string[]? style = null, Func<T, Task>? onSelect = null, string? selectedId = null, Func<T, string?>? icon = null, string[]? itemStyle = null, string[]? selectedItemStyle = null, string[]? labelStyle = null, string[]? childrenStyle = null, string? styleId = null, string? key = null)
  // Extension methods for the DOM-virtualized scroll containers VirtualList and VirtualGrid . Items outside the visible window plus an overscan buffer have their content children skipped at the React layer (the wrapper still occupies space via fixed dimensions), so DOM size scales with viewport, not itemCount.
  static class VirtualListExtensions
    // DOM-virtualized scrollable grid. Items are laid out in a fixed number of columns and rows outside the visible window are not mounted in the DOM.
    static void VirtualGrid(this UIView view, int itemCount, int columns, double rowHeight, Action<UIView, int> onRenderItem, int overscan = 2, int gap = 12, int? minItemWidthPx = null, int? maxColumns = null, double? aspectRatio = null, string? resetScrollKey = null, Func<int, Task>? onNearEnd = null, int nearEndThresholdRows = 2, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // DOM-virtualized vertical list with fixed item height. Renders only items inside the visible window plus an overscan buffer.
    static void VirtualList(this UIView view, int itemCount, double itemHeight, Action<UIView, int> onRenderItem, int overscan = 4, Func<int, Task>? onNearEnd = null, int nearEndThreshold = 5, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Day of the week used as the first column in the calendar grid.
  enum WeekStart
    Sunday
    Monday

namespace Ikon.Parallax.Theming
  static class Accessibility
    static string RequiredLabel(string baseLabel)
    static string NotScreenReaderOnly
    static string ScreenReaderOnly
    static string SkipLink
  static class Accordion
    static string ChevronIcon
    static string Content
    static string ContentInner
    static string Default
    static string Header
    static string Item
    static string Root
    static string Trigger
  static class Alert
    static string Base
    static string Default
    static string Description
    static string Error
    static string Info
    static string Success
    static string Title
    static string Warning
  static class AlertDialog
    static string Action
    static string Cancel
    static string Content
    static string Default
    static string Description
    static string Footer
    static string Header
    static string Overlay
    static string Title
  // One vocabulary entry: an accepted theme key and the canonical variable keys it commits. Targets are always canonical (never other aliases), so expansion is one step.
  sealed class ThemeVocabulary.Alias : IEquatable<ThemeVocabulary.Alias>
    ctor(string Name, IReadOnlyList<string> Targets, ThemeVocabulary.ValueKind Kind)
    ThemeVocabulary.ValueKind Kind { get; init; }
    string Name { get; init; }
    IReadOnlyList<string> Targets { get; init; }
  static class Accessibility.Aria
    static string Busy
    static string Checked
    static string CurrentPage
    static string CurrentStep
    static string Disabled
    static string Expanded
    static string Invalid
    static string Required
    static string Selected
  static class AspectRatio
    static string Base
    static string Default
    static string PlaceholderContent
  static class Avatar
    static string Base
    static string Default
    static string Fallback
    static string Image
    static string Root
  static class Badge
    static string Base
    static string Brand
    static string BrandLg
    static string BrandMd
    static string BrandSm
    static string Default
    static string DefaultLg
    static string DefaultMd
    static string DefaultSm
    static string Error
    static string ErrorLg
    static string ErrorMd
    static string ErrorSm
    static string IconLeft
    static string IconRight
    static string Info
    static string InfoLg
    static string InfoMd
    static string InfoSm
    static string Neutral
    static string NeutralLg
    static string NeutralMd
    static string NeutralSm
    static string OutlineBrand
    static string OutlineBrandLg
    static string OutlineBrandMd
    static string OutlineBrandSm
    static string OutlineError
    static string OutlineErrorLg
    static string OutlineErrorMd
    static string OutlineErrorSm
    static string OutlineInfo
    static string OutlineInfoLg
    static string OutlineInfoMd
    static string OutlineInfoSm
    static string OutlineNeutral
    static string OutlineNeutralLg
    static string OutlineNeutralMd
    static string OutlineNeutralSm
    static string OutlineSuccess
    static string OutlineSuccessLg
    static string OutlineSuccessMd
    static string OutlineSuccessSm
    static string OutlineWarning
    static string OutlineWarningLg
    static string OutlineWarningMd
    static string OutlineWarningSm
    static string Success
    static string SuccessLg
    static string SuccessMd
    static string SuccessSm
    static string Warning
    static string WarningLg
    static string WarningMd
    static string WarningSm
  static class Tokens.Blur
    static string Lg
    static string Md
    static string Sm
  static class Breadcrumb
    static string Ellipsis
    static string Item
    static string Link
    static string List
    static string Page
    static string Root
    static string Separator
  static class Button
    static string Base
    static string Default
    static string DefaultLg
    static string DefaultMd
    static string DefaultSm
    static string Error
    static string ErrorLg
    static string ErrorMd
    static string ErrorSm
    static string Ghost
    static string GhostLg
    static string GhostMd
    static string GhostSm
    static string Icon
    static string IconLeft
    static string IconRight
    static string Info
    static string InfoLg
    static string InfoMd
    static string InfoSm
    static string Link
    static string LinkLg
    static string LinkMd
    static string LinkSm
    static string Neutral
    static string NeutralLg
    static string NeutralMd
    static string NeutralSm
    static string Outline
    static string OutlineLg
    static string OutlineMd
    static string OutlineSm
    static string Primary
    static string PrimaryLg
    static string PrimaryMd
    static string PrimarySm
    static string Secondary
    static string SecondaryLg
    static string SecondaryMd
    static string SecondarySm
    static string SolidLg
    static string SolidMd
    static string SolidSm
    static string Success
    static string SuccessLg
    static string SuccessMd
    static string SuccessSm
    static string Warning
    static string WarningLg
    static string WarningMd
    static string WarningSm
  static class Calendar
    static string Day
    static string DayDisabled
    static string DayOutside
    static string DaySelected
    static string DayToday
    static string Default
    static string Grid
    static string Header
    static string HeaderTitle
    static string NavButton
    static string Root
    static string Row
    static string Weekday
  static class Card
    static string Base
    static string Content
    static string Default
    static string Description
    static string Elevated
    static string Flat
    static string Footer
    static string Ghost
    static string Glass
    static string GlassSubtle
    static string Header
    static string HeaderRow
    static string Interactive
    static string InteractiveFill
    static string Outline
    static string Selected
    static string Strong
    static string Subtle
    static string Title
  static class OnSurface.Card
    static string Caption
    static string Muted
    static string Subtle
    static string Text
  static class Carousel
    static string Default
    static string Indicator
    static string IndicatorActive
    static string Indicators
    static string NavButton
    static string Next
    static string Previous
    static string Root
    static string Slide
    static string Track
    static string TrackVertical
    static string Viewport
  static class Chart
    static string Container
    static string ContainerLg
    static string ContainerMd
    static string ContainerSm
    static string ContainerXl
    static string Default
  static class Checkbox
    static string Default
    static string Indicator
    static string Root
  static class CodeEditor
    static string Body
    static string Content
    static string Default
    static string Gutter
    static string Header
    static string LanguageBadge
    static string Line
    static string Root
  static class Collapsible
    static string Content
    static string Default
    static string Root
    static string Trigger
    static string TriggerIcon
  static class ColorPicker
    static string AlphaTrack
    static string Content
    static string Default
    static string HexInput
    static string HueThumb
    static string HueTrack
    static string PresetSwatch
    static string PresetsGrid
    static string SaturationArea
    static string Swatch
    static string SwatchLg
    static string SwatchSm
    static string Thumb
    static string Trigger
  static class Layout.Column
    static string Center
    static string Default
    static string Lg
    static string Md
    static string Sm
    static string Xl
    static string Xs
  static class Command
    static string Default
    static string Dialog
    static string Empty
    static string Group
    static string GroupHeading
    static string Input
    static string InputWrapper
    static string Item
    static string List
    static string Root
    static string Separator
    static string Shortcut
  static class Container
    static string Full
    static string Lg
    static string Md
    static string Prose
    static string Screen
    static string Sm
    static string Xl
    static string Xl2
    static string Xl3
    static string Xl4
    static string Xl5
    static string Xl6
    static string Xl7
    static string Xs
  static class ContentGrid
    static string Bordered
    static string Cell
    static string CellMuted
    static string Default
    static string Header
  static class DataTable
    static string Cell
    static string DataCell
    static string Default
    static string EmptyState
    static string Header
    static string HeaderCell
    static string PageNumber
    static string PageNumberActive
    static string Pagination
    static string PaginationButton
    static string ResizeHandle
    static string Row
    static string RowClickable
  static class DatePicker
    static string Content
    static string Default
    static string Trigger
    static string TriggerLg
    static string TriggerSm
  static class OnSurface.Default
    static string Caption
    static string Muted
    static string Subtle
    static string Text
  static class Dialog
    static string CloseButton
    static string Content
    static string Description
    static string Footer
    static string Header
    static string Overlay
    static string Title
  static class DragDrop
    static string Container
    static string ContainerHorizontal
    static string DropZone
    static string DropZoneActive
    static string Overlay
    static string OverlayContent
  static class Drawer
    static string Content
    static string Default
    static string Description
    static string Footer
    static string Handle
    static string Header
    static string Overlay
    static string Title
  static class DropdownMenu
    static string CheckboxItem
    static string Content
    static string Group
    static string Item
    static string Label
    static string RadioItem
    static string Separator
    static string Shortcut
    static string SubContent
    static string SubTrigger
  static class DragDrop.Droppable
    static string Base
    static string Default
    static string Disabled
    static string Info
    static string Success
  static class Tokens.Duration
    static string Fast
    static string Instant
    static string Normal
    static string Slow
    static string Slower
  static class Transition.Ease
    static string In
    static string InOut
    static string Linear
    static string Out
  static class EmptyState
    static string Actions
    static string Description
    static string IconSize
    static string IconSizeSm
    static string IconWrap
    static string IconWrapSm
    static string IllustrationSize
    static string IllustrationWrap
    static string IllustrationWrapSm
    static string Root
    static string RootFull
    static string RootSm
    static string Title
  static class FeedScroller
    static string Default
    static string MuteToggle
    static string Root
    static string Slide
    static string SlideMedia
    static string SlideOverlay
  static class FileUpload
    static string FileItem
    static string FileList
    static string FileName
    static string FileSize
    static string RemoveButton
    static string TypeIcon
  static class Accessibility.Focus
    static string HighContrast
    static string None
    static string Sentinel
    static string Within
  static class FormField
    static string ErrorText
    static string HelpText
    static string Label
    static string LabelRequired
    static string ParamRow
    static string Root
    static string SuccessText
    static string WarningText
  static class Layout.Grid
    static string Cols2
    static string Cols3
    static string Cols4
  static class Select.Group
    static string Label
    static string Root
  static class ImageCard.Hover
    static string Dim
    static string Zoom
  static class HoverCard
    static string Content
    static string Default
  // Defines a UI theme providing base CSS and a default icon library.
  interface ITheme
    // Global CSS injected into the client as the theme baseline.
    string Css { get; }
    // The default icon library name (e.g. "lucide") used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }
  static class Icon
    static string Default
    static string Lg
    static string Md
    static string Sm
    static string Spinner
    static string SpinnerLg
    static string SpinnerSm
    static string Xl
    static string Xs
  static class FileUpload.Icon
    static string Base
    static string Brand
    static string Disabled
    static string Error
    static string Info
    static string Success
  static class Toggle.Size.Icon
    static string Lg
    static string Md
    static string Sm
  // Per-app theme configuration. Composes the platform's Ikon CSS baseline with per-token CSS-variable overrides addressed by name. One uniform syntax: an indexer keyed by a vocabulary alias ( ThemeVocabulary ), a CSS variable name (without the leading --), or a Tailwind utility token. The renderer dispatches by key shape: Vocabulary alias (primary, card, radius, density) → its canonical variable clusterTailwind palette step (amber-400) → --color-amber-400 (Ikon scales like neutral-900 also set the bare var)rounded-{rung} → --radius-{rung}shadow-{rung} → --shadow-{rung}font-{role} → --font-{role}spacing → the --spacing density unitAnything else → --{key} (free CSS variable) Values are Crosswind / Tailwind class names, which are resolved to CSS, or raw CSS values (hex, rem, family stacks, gradients), which pass through unchanged. Example — the structural core is a small committed set; expressive decoration (gradients, textures) stays concrete at use points:
  // private UI UI { get; } = new(app, new IkonTheme
  // {
  //     ["primary"]    = "amber-400",   // whole brand cluster: fills, CTA, focus ring, brand icons/text
  //     ["background"] = "zinc-950",
  //     ["card"]       = "zinc-900",
  //     ["foreground"] = "amber-50",
  //     ["muted-foreground"] = "zinc-400",
  //     ["border"]     = "zinc-800",
  //
  //     ["font-heading"] = "Crimson Pro",
  //     ["font-body"]    = "Inter",
  //     ["radius"]       = "rounded-lg",
  //     ["density"]      = "airy",
  //     ["motion-duration-base"] = "200ms",
  //     ["ease-default"]         = "ease-out",
  //
  //     // Per-token palette / radius / shadow overrides and free decorative vars.
  //     ["amber-400"]  = "#F5A524",
  //     ["shadow-lg"]  = "0 8px 16px rgba(0,0,0,.18)",
  //     ["hero-glow"]  = "radial-gradient(circle, #F5A52488, transparent 70%)",
  //
  //     DarkMode = new IkonTheme
  //     {
  //         ["background"] = "zinc-50",
  //         ["foreground"] = "zinc-950",
  //     },
  // });
  // Aliases expand to exactly their documented cluster — beyond that there is no magic fan-out and no auto-derived contrast text. A later explicit entry overrides an alias-expanded one (["primary"] then ["bg-brand-button"] re-pins just the CTA).
  sealed class IkonTheme : ITheme
    ctor()
    // Paired dark-mode theme. Pass another IkonTheme ; its overrides are emitted under [data-theme="dark"], .dark, and prefers-color-scheme: dark. Only meaningful in Adaptive mode.
    IkonTheme? DarkMode { get; init; }
    // Per-token override addressed by CSS variable name (without the leading --) or by Tailwind utility token. Set during object initialization.
    string this[string token] { get; set; }
    // How the app relates to light/dark switching. Adaptive (the default) keeps today's behavior: overrides restyle the light theme, DarkMode restyles the dark one, and the client's theme preference picks between them. Fixed commits to ONE scheme: every override is also emitted under the dark selectors, so a client-side theme flip cannot pull the platform's dark palette in under the app's committed colors. For atmospheric, game, or brand-locked looks that should never light/dark switch.
    ThemeMode Mode { get; init; }
  static class ImageCard
    static string Caption
    static string Image
    static string Root
    static string Title
  static class Input
    static string Base
    static string Default
    static string DefaultLg
    static string DefaultSm
    static string Error
    static string ErrorLg
    static string ErrorSm
    static string Ghost
    static string GhostLg
    static string GhostSm
    static string Invalid
    static string InvalidLg
    static string InvalidSm
    static string Success
    static string SuccessLg
    static string SuccessSm
    static string Warning
    static string WarningLg
    static string WarningSm
  static class Interaction
    static string HoverCard
    static string HoverGlow
    static string HoverLift
  static class DragDrop.Item
    static string Base
    static string Dashed
    static string Default
    static string Disabled
    static string Dragging
  static class Label
    static string Base
    static string Default
    static string Error
    static string Optional
    static string Required
  static class Layout
    static string Center
    static string Page
    static string RowWrap
    static string Section
    static string SectionBody
    static string SectionHeader
    static string Stretch
  static class Media
    static string CanvasFill
    static string Default
    static string EmptyState
    static string Fill
    static string ImageEmptyState
    static string Mirror
    static string PlaceholderHint
    static string PlaceholderIcon
    static string PlaceholderText
    static string VideoContainer
  static class Menubar
    static string Content
    static string Default
    static string Item
    static string Root
    static string Separator
    static string Trigger
  static class Accessibility.Motion
    static string Reduce
    static string ReduceFade
    static string Respectful
    static string Safe
  static class NavItem
    static string Active
    static string ActiveAccent
    static string ActiveBrand
    static string ActiveSubtle
    static string Count
    static string Default
    static string Icon
    static string Label
    static string Lg
    static string Md
    static string Sm
    static string Subtle
  static class NavPanel
    static string Base
    static string Border
    static string Divided
    static string Filled
    static string Ghost
  static class NavSection
    static string Divider
    static string Label
    static string Root
  static class NavigationMenu
    static string Content
    static string ContentNarrow
    static string ContentPopover
    static string ContentPopoverSide
    static string ContentWide
    static string Default
    static string Indicator
    static string Link
    static string LinkCompact
    static string List
    static string ListVertical
    static string Root
    static string Trigger
    static string TriggerDisabled
    static string TriggerIcon
    static string TriggerIconRotate180
    static string TriggerIconRotate90
    static string TriggerVertical
    static string Viewport
  static class OnSurface
  static class Tokens.Opacity
    static string GlassLg
    static string GlassMd
    static string GlassSm
    static string O10
    static string O15
    static string O20
    static string O25
    static string O30
    static string O40
    static string O5
    static string O50
  static class Separator.Orientation
    static string Horizontal
    static string Vertical
  static class OtpField
    static string Default
    static string Input
    static string Root
  static class ImageCard.Overlay
    static string Center
    static string Dim
    static string Reveal
  static class Page
    static string Base
    static string Default
    static string Plain
  static class Pagination
    static string Active
    static string Disabled
    static string Ellipsis
    static string Item
    static string List
    static string Next
    static string Previous
    static string Root
  static class Panel
    static string Fill
    static string Side
    static string Sidebar
    static string SidebarNarrow
    static string Wide
  static class Input.Password
    static string Input
    static string Toggle
    static string Wrapper
  static class Popover
    static string Content
    static string Default
  static class OnSurface.Popover
    static string Caption
    static string Muted
    static string Subtle
    static string Text
  static class Progress
    // Composes the indicator class list from the base recipe, a fill variant ( Variant , defaulting to the brand fill), the optional indeterminate shimmer, and caller overrides appended last so they win.
    static string ComposeIndicator(string? variant = null, bool indeterminate = false, params string?[] overrides)
    // Arbitrary-value transform class that fills the indicator to value percent (clamped to 0–100) by translating it left from the fully-filled position.
    static string IndicatorTransform(double value)
    static string Base
    static string Default
    static string Indeterminate
    static string Indicator
    static string IndicatorBase
    static string Label
    static string Root
    static string Value
  static class Transition.Property
    static string All
    static string Colors
    static string Opacity
    static string Shadow
    static string Transform
  static class RadioGroup
    static string Default
    static string Indicator
    static string Item
    static string Root
    static string RootHorizontal
  static class Tokens.Radius
    static string Full
    static string Lg
    static string Md
    static string None
    static string Sm
    static string Xl
    static string Xl2
  static class AspectRatio.Ratio
    static string Photo
    static string Portrait
    static string Square
    static string Video
    static string Wide
  static class ResizableSplit
    static string FirstPane
    static string FirstPaneVertical
    static string Handle
    static string HandleVertical
    static string Root
    static string SecondPane
    static string SecondPaneVertical
  static class Responsive
    static string CenterToEnd
    static string CenterToLeft
    static string CenterToSpaceBetween
    static string CenterToStart
    static string ColToRow
    static string ColToRowMd
    static string HiddenDesktop
    static string HiddenMobile
    static string HiddenTablet
    static string LeftToCenter
    static string RowToCol
    static string VisibleMobile
    static string VisibleTablet
  static class RichTextEditor
    static string Content
    static string Default
    static string Root
    static string Toolbar
    static string ToolbarButton
    static string ToolbarSeparator
  static class Layout.Row
    static string Default
    static string InlineCenter
    static string Lg
    static string Md
    static string Sm
    static string SpaceBetween
    static string Xl
    static string Xs
  static class ScrollArea
    static string Bordered
    static string Default
    static string Root
    static string Scrollbar
    static string Thumb
    static string Viewport
  static class Select
    static string Content
    static string Default
    static string Item
    static string ItemIndicator
    static string Label
    static string ScrollButton
    static string Separator
    static string Trigger
    static string TriggerBase
  static class Separator
    static string Base
    static string Horizontal
    static string Vertical
  static class Tokens.Shadow
    static string Lg
    static string Md
    static string None
    static string Sm
    static string Xl
    static string Xl2
  static class Avatar.Shape
    static string Circle
    static string Square
  static class Skeleton.Shape
    static string Circle
    static string Rectangle
    static string Square
  static class Sheet
    static string Base
    static string CloseButton
    static string Default
    static string Description
    static string Footer
    static string Header
    static string Overlay
    static string Title
  static class Sheet.Side
    static string Bottom
    static string Left
    static string Right
    static string Top
  static class Button.Size
    static string Lg
    static string Md
    static string Sm
  static class Toggle.Size
    static string Lg
    static string Md
    static string Sm
  static class Select.Size
    static string Lg
    static string Md
    static string Sm
  static class Progress.Size
    static string Lg
    static string Md
    static string Sm
    static string Xs
  static class Avatar.Size
    static string Lg
    static string Md
    static string Sm
    static string Xl
    static string Xl2
    static string Xs
  static class Skeleton.Size
    static string Lg
    static string Md
    static string Sm
    static string Xl
    static string Xs
  static class Skeleton
    static string Avatar
    static string AvatarLg
    static string AvatarSm
    static string Base
    static string Button
    static string Card
    static string Default
    static string Input
    static string Text
    static string TextLg
    static string TextSm
  static class Slider
    static string Default
    static string Range
    static string Root
    static string RootVertical
    static string Thumb
    static string Track
    static string TrackVertical
  static class Drawer.Snap
    static string Full
    static string Half
    static string Quarter
    static string ThreeQuarter
  static class Layout.Split
    static string Detail
    static string DetailLg
    static string Gapped
    static string Main
    static string Root
    static string Sidebar
    static string SidebarLg
    static string SidebarSm
  static class StatCard
    static string Header
    static string IconBox
    static string IconBoxBrand
    static string IconBoxError
    static string IconBoxInfo
    static string IconBoxSuccess
    static string IconBoxWarning
    static string IconSize
    static string Label
    static string Root
    static string Trend
    static string TrendIcon
    static string TrendLabel
    static string TrendValue
    static string Value
    static string ValueRow
  static class State
    static string Checked
    static string Disabled
    static string Empty
    static string Focusable
    static string Indeterminate
    static string Invalid
    static string Loading
    static string Pending
    static string Pressable
    static string Readonly
    static string Selected
    static string Success
    static string Validating
    static string Warning
  static class Switch
    static string Default
    static string Root
    static string Thumb
  static class Tabs
    static string Content
    static string List
    static string ListVertical
    static string Trigger
    static string TriggerDisabled
  static class Text
    static string Body
    static string BodySm
    static string BodyStrong
    static string Caption
    static string Code
    static string Display
    static string DisplaySm
    static string H1
    static string H2
    static string H3
    static string H4
    static string H5
    static string H6
    static string Label
    static string Link
    static string Muted
    static string Numeric
    static string Overline
    static string Small
    static string Tabular
  static class Textarea
    static string Base
    static string Default
    static string DefaultLg
    static string DefaultSm
    static string Invalid
  // How an app's IkonTheme relates to the client's light/dark preference. Not every app wants two themes: a productivity tool should adapt, but a game, an atmospheric experience, or a brand-locked look is designed as ONE palette — and letting a theme toggle pull the platform's dark (or light) defaults in underneath that palette produces a broken half-switched hybrid.
  enum ThemeMode
    Adaptive
    Fixed
  // The canonical theming vocabulary: shadcn-style theme keys and what they commit. Each alias expands to the canonical CSS variables that make its intent real across every consumer (components, focus rings, native clients). This table is the single source of truth — the theme renderer expands aliases through it, the codegen styling tools fan roles out through it, and the docs drift tests lock the published reference tables to it. Collision policy: `primary` as a THEME KEY means brand (the shadcn reading; the Untitled-UI tiered reading only ever existed on the prefixed utility classes, which are untouched). Bare `accent` and `secondary` are deliberately NOT aliases — their shadcn and Ikon meanings genuinely conflict, so they stay unknown-key warnings instead of guessing.
  static class ThemeVocabulary
    // Every accepted alias, keyed by name.
    static IReadOnlyDictionary<string, ThemeVocabulary.Alias> Aliases { get; }
  static class TimePicker
    static string Column
    static string ColumnSeparator
    static string Content
    static string Default
    static string Item
    static string ItemSelected
    static string Trigger
  static class Toast
    static string Action
    static string Base
    static string Close
    static string Default
    static string Description
    static string Title
    static string Viewport
    static string ViewportBottomCenter
  static class Toggle
    static string Base
    static string Default
    static string DefaultLg
    static string DefaultMd
    static string DefaultSm
    static string Group
    static string GroupVertical
    static string IconDefault
    static string IconDefaultLg
    static string IconDefaultMd
    static string IconDefaultSm
  static class Tokens
  static class Tone
    static string Error
    static string Ghost
    static string Info
    static string Link
    static string Muted
    static string Neutral
    static string Outline
    static string Primary
    static string Solid
    static string Subtle
    static string Success
    static string Warning
  static class Toolbar
    static string Button
    static string Default
    static string IconStyle
    static string Root
    static string Separator
    static string ToggleGroup
    static string ToggleItem
  static class Tooltip
    static string Content
    static string Default
  static class Transition
    static string Fast
    static string None
    static string Normal
    static string Slow
    static string Slower
  static class StatCard.TrendVariant
    static string Negative
    static string Neutral
    static string Positive
  // What value shape an alias expects, for docs and tooling.
  enum ThemeVocabulary.ValueKind
    Color
    FontFamily
    Radius
    Duration
    Easing
    Spacing
  static class Separator.Variant
    static string Default
    static string Strong
    static string Subtle
  static class Alert.Variant
    static string Default
    static string Error
    static string Info
    static string Success
    static string Warning
  static class Toggle.Variant
    static string Default
  static class Progress.Variant
    static string Default
    static string Error
    static string Success
    static string Warning
  static class Tokens.Width
    static string Dialog
    static string DialogLg
    static string DialogMd
    static string DialogSm
    static string DialogXl
    static string Drawer
    static string Popover
    static string Sheet
    static string Toast
  static class ZIndex
    static string Dropdown
    static string Modal
    static string Overlay
    static string Popover
    static string Sticky
    static string Toast
    static string Tooltip
  static class FileUpload.Zone
    static string Active
    static string ActiveRing
    static string Base
    static string Code
    static string Compact
    static string Default
    static string Disabled
    static string Documents
    static string DragOverlay
    static string Images
    static string Wrapper

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
    static CanvasDesignTokenDocument Load(string json)
    static CanvasDesignTokenDocument LoadFromFile(string path)
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
    T Value { get; init; }
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
  // Injectable theme data for the Flutter style resolver. When set (via ThemeSource ) the resolver resolves colour scales and semantic tokens against the app's own theme instead of the hardcoded platform baseline snapshot, so custom brand themes render correctly on native clients. Lookup values may be concrete colours ("#0c0e12", "oklch(...)"), scale references ("neutral-800"), or other semantic tokens ("text-secondary"); the resolver chases references and normalizes concrete colours to hex.
  sealed class FlutterThemeSource
    ctor(IReadOnlyDictionary<string, string> scaleHex, IReadOnlyDictionary<string, string> darkSemantic, IReadOnlyDictionary<string, string> lightSemantic, double? radiusBasePx = null, IReadOnlyDictionary<string, double>? radiusPx = null, IReadOnlyDictionary<string, string>? fontFamilies = null, double? spacingUnitPx = null)
    IReadOnlyDictionary<string, string> DarkSemantic { get; }
    // Themed font families keyed by role ("body", "display", "heading", …), values are plain family names ("Fraunces") the Flutter client can load.
    IReadOnlyDictionary<string, string> FontFamilies { get; }
    IReadOnlyDictionary<string, string> LightSemantic { get; }
    // Themed radius base in logical px; rung values derive from it unless RadiusPx pins a rung explicitly. Null = platform default.
    double? RadiusBasePx { get; }
    // Explicit per-rung radius overrides in logical px, keyed by rung name ("lg").
    IReadOnlyDictionary<string, double> RadiusPx { get; }
    IReadOnlyDictionary<string, string> ScaleHex { get; }
    // Themed spacing unit in logical px (the density knob — web multiplies --spacing the same way). Null = platform default (4px).
    double? SpacingUnitPx { get; }
    static FlutterThemeSource FromDesignTokens(CanvasDesignTokenDocument document)
  enum TailwindColorContext
    Generic
    Background
    Foreground
    Text
    Border
  sealed class TailwindColorDefinitions
    ctor()
    ctor(IReadOnlyDictionary<string, string>? background, IReadOnlyDictionary<string, string>? foreground, IReadOnlyDictionary<string, string>? text, IReadOnlyDictionary<string, string>? border)
    IReadOnlyDictionary<string, string> Background { get; init; }
    IReadOnlyDictionary<string, string> Border { get; init; }
    IReadOnlyDictionary<string, string> Foreground { get; init; }
    IReadOnlyDictionary<string, string> Text { get; init; }
    void Validate()
  static class TailwindCssBaseline
    static string AdditionalCss { get; }
    static IReadOnlyDictionary<string, string> DarkVariables { get; }
    static IReadOnlyDictionary<string, string> LightVariables { get; }
    // The stock Tailwind colour palette parsed once from the authored baseline: every --color-{name}-{step} entry keyed as "{name}-{step}" → its OKLCH value. This is the single source the palette name/step views below derive from, so a token dropped from (or added to) the baseline can never silently disagree with them.
    static IReadOnlyDictionary<string, string> PaletteColors { get; }
    // Palette family names present in the baseline (red, …, stone), first-seen order.
    static IReadOnlyList<string> PaletteNames { get; }
    // Palette steps present in the baseline (50, …, 950), ascending.
    static IReadOnlyList<string> PaletteSteps { get; }
    static string GetFullBaseline()
  sealed class TailwindCssVariables
    ctor(IDictionary<string, string> light, IDictionary<string, string> dark, string darkThemeName = "dark")
    IReadOnlyDictionary<string, string> Dark { get; }
    string DarkThemeName { get; }
    IReadOnlyDictionary<string, string> Light { get; }
    string EmitDark()
    string EmitLight()
  // Static facade the Crosswind compiler resolves custom aliases through. Definitions live in a TailwindCustomStyleScope that the caller pins with PushScope around each compile, so several apps hosted in one process each resolve against their own theme. Lookups fall back to a process-wide scope kept for legacy single-app hosts that still write it via SetDefinitions / MergeDefinitions .
  static class TailwindCustomStyleRegistry
    // Flutter theme data of the scope active for the current compile, preferring the ambient scope like the alias lookups do.
    static FlutterThemeSource? CurrentFlutterTheme { get; }
    static bool IsFontFamilyToken(string name)
    static bool IsFontWeightToken(string name)
    // Compat write path for legacy single-app hosts: merges definitions into the process-wide fallback scope. New code should own a TailwindCustomStyleScope and pin it with PushScope instead.
    static void MergeDefinitions(TailwindStyleDefinitions definitions)
    // Makes the given scope the ambient alias source for the current async flow until the returned handle is disposed. Compilation call sites stay static, but each caller can pin its own scope for the duration of a compile.
    static IDisposable PushScope(TailwindCustomStyleScope scope)
    // Compat write path for legacy single-app hosts: replaces the process-wide fallback scope's definitions (null clears them). New code should own a TailwindCustomStyleScope and pin it with PushScope instead.
    static void SetDefinitions(TailwindStyleDefinitions? definitions)
    static bool TryResolve(string name, TailwindColorContext context, out string value)
    static bool TryResolveFontFamily(string name, out string value)
    static bool TryResolveFontWeight(string name, out string value)
  // One isolated set of custom color and font alias definitions. Style compilation reads aliases through TailwindCustomStyleRegistry , which prefers the ambient scope pushed via PushScope and falls back to the process-wide scope, so several apps hosted in one process can each compile against their own theme without contaminating the others.
  sealed class TailwindCustomStyleScope
    ctor()
    // Optional Flutter theme data derived from the same app theme as the alias definitions. The Flutter style resolver reads it through the ambient scope so each app in a shared process renders its own brand colors on native clients.
    FlutterThemeSource? FlutterTheme { get; set; }
    bool IsFontFamilyToken(string name)
    bool IsFontWeightToken(string name)
    // Merges definitions into this scope. Returns true when the merge added or changed at least one alias, so callers know whether previously compiled styles may now resolve differently and need recompilation.
    bool MergeDefinitions(TailwindStyleDefinitions definitions)
    void SetDefinitions(TailwindStyleDefinitions? definitions)
    bool TryResolve(string name, TailwindColorContext context, out string value)
    bool TryResolveFontFamily(string name, out string value)
    bool TryResolveFontWeight(string name, out string value)
  sealed class TailwindFontDefinitions
    ctor()
    ctor(IReadOnlyDictionary<string, string>? family, IReadOnlyDictionary<string, string>? weight)
    IReadOnlyDictionary<string, string> Family { get; init; }
    IReadOnlyDictionary<string, string> Weight { get; init; }
    void Validate()
  sealed class TailwindFontSize
    ctor(string size, string lineHeight, string? letterSpacing)
    string? LetterSpacing { get; }
    string LineHeight { get; }
    string Size { get; }
  sealed class TailwindStyleDefinitions
    ctor()
    ctor(TailwindColorDefinitions colors, TailwindFontDefinitions? fonts = null)
    TailwindColorDefinitions Colors { get; init; }
    TailwindFontDefinitions Fonts { get; init; }
    void Validate()
  sealed class TailwindThemeDefinition
    ctor(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> colorScales, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> colors, IReadOnlyDictionary<string, string> boxShadow, IReadOnlyDictionary<string, string> shadowPalette, IReadOnlyDictionary<string, string> focusRing, IReadOnlyDictionary<string, string> borderRadius, IReadOnlyDictionary<string, string> backdropBlur, IReadOnlyDictionary<string, string> fontFamily, IReadOnlyDictionary<string, string> fontWeight, IReadOnlyDictionary<string, TailwindFontSize> fontSize)
    IReadOnlyDictionary<string, string> BackdropBlur { get; }
    IReadOnlyDictionary<string, string> BorderRadius { get; }
    IReadOnlyDictionary<string, string> BoxShadow { get; }
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ColorScales { get; }
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Colors { get; }
    IReadOnlyDictionary<string, string> FocusRing { get; }
    IReadOnlyDictionary<string, string> FontFamily { get; }
    IReadOnlyDictionary<string, TailwindFontSize> FontSize { get; }
    IReadOnlyDictionary<string, string> FontWeight { get; }
    IReadOnlyDictionary<string, string> ShadowPalette { get; }
  // Target-scoping Crosswind variants. A class prefixed with flutter: applies only on the Flutter renderer, web: only on the web/CSS renderer, and an unprefixed class applies to both. This lets a single Crosswind class list carry per-target styling — e.g. ["px-3 py-2 rounded-md", "web:bg-background web:text-secondary", "flutter:bg-slate-900 flutter:text-slate-100"] — instead of maintaining a parallel token catalogue. Works with the variant-group syntax too: flutter:(bg-slate-900 text-slate-100) applies the marker to every grouped class. The marker is consumed by whichever renderer is active: the CSS compiler drops flutter: classes and strips the web: marker (emitting the class as base); the Flutter resolver drops web: classes and strips the flutter: marker.
  static class TargetVariant
    // True when variants contains the given target marker.
    static bool Has(IReadOnlyList<string> variants, string target)
    // Returns a copy of variants with the given target marker removed. The marker has been satisfied by the active renderer and must not become a CSS selector or block Flutter resolution. Returns the original reference unchanged when the marker is absent, to avoid an allocation on the common path.
    static IReadOnlyList<string> Without(IReadOnlyList<string> variants, string target)
    static string Flutter
    static string Web

# Ikon.App Public API

namespace Ikon.App
  // Attribute that decorates app classes to configure their connection and messaging behavior
  sealed class AppAttribute : Attribute
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, string[]? dependencies = null)
    // Product IDs of other apps that must be ready before this app's Joined callback is invoked
    string[] Dependencies { get; }
    // Human-readable description of the app. Defaults to "{ClassName} App" if not specified
    string? Description { get; }
    // Stable identifier for the app that persists across class renames. Used by external systems to identify apps independently of their type name
    string? Guid { get; }
    // Display name of the app. Defaults to the class name if not specified
    string? Name { get; }
    // Unique identifier for the app. Defaults to the full type name if not specified
    string? ProductId { get; }
    // Opcode groups this app subscribes to receive messages from
    Opcode ReceiveOpcodeGroups { get; }
    // Opcode groups this app is allowed to send messages to
    Opcode SendOpcodeGroups { get; }
    // Indicates whether the app operates autonomously (Machine) or represents a human user connecting through it (Human). Defaults to Machine
    UserType UserType { get; }
    // Version number of the app
    int Version { get; }
  // A lightweight HTTP and WebSocket endpoint host built on ASP.NET Core. Construct the host, register routes with MapGet / MapPost / MapWebSocket , and call StartAsync to allocate the relay tunnel and begin serving requests.
  sealed class AppEndpointHost : IAsyncDisposable
    // Creates a new HTTP/WebSocket endpoint host. The relay tunnel is not allocated until StartAsync is called.
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    // True once the relay tunnel is allocated and PublicUrl can be read. False before StartAsync , and after it when the relay was unreachable — the host then serves on LocalPort only and retries the allocation in the background; subscribe to PublicUrlAvailable to learn when the tunnel comes up.
    bool HasPublicUrl { get; }
    // The local port Kestrel binds to. Available after StartAsync completes.
    int LocalPort { get; }
    // Invoked once per inbound HTTP/WebSocket request before it is routed. Used to mark external activity (e.g. reset the server's idle timer) so an endpoint-served instance isn't reaped while it is serving traffic. Null = no hook.
    Action? OnRequest { get; set; }
    // The public URL for this endpoint. Available once the relay tunnel is allocated — normally when StartAsync completes; check HasPublicUrl when the relay may be down.
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
    // Registers a handler for WebSocket connections matching the specified route pattern. The socket is automatically closed and disposed after the handler completes.
    void MapWebSocket(string pattern, Func<HttpContext, WebSocket, Task> handler)
    // Allocates the relay tunnel, starts Kestrel with the registered routes, and returns immediately while the host continues to run in the background. When the relay tunnel cannot be allocated (relay not configured, backend unreachable), Kestrel still starts on a locally picked port and the tunnel allocation is retried in the background — local traffic keeps working, and PublicUrlAvailable fires once the tunnel comes up.
    Task StartAsync(CancellationToken cancellationToken = default)
    // Stops the endpoint host gracefully. Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = default)
    // Raised with the public URL when the background retry allocates the relay tunnel after StartAsync completed without one. Not raised when the tunnel was allocated during StartAsync itself — read PublicUrl directly in that case.
    event Action<string>? PublicUrlAvailable
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own schema/*.tp files (compiled by ikon app teleport build); each carries its own GROUP_APP_LOCAL opcode and is sent/received as a native type — no JSON marshalling. Delivery is server-controlled and explicit: SendMessageAsync always takes the recipient client session IDs — there is no implicit broadcast to every client. Whether a type travels reliably or unreliably is declared on the .tp schema (unreliable = true), not here.
  static class AppMessaging
    // Subscribe to inbound app messages of type T (filtered by the type's opcode). The handler receives the decoded native payload and the sender's client session ID. Dispose the returned handle to unsubscribe.
    static IDisposable OnMessage<T>(this IMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    // Send a typed app message to the given client session IDs. The server decides the recipients — pass the explicit target list (e.g. every current client, everyone-but-the-sender, or a single client).
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    // Send a typed app message to a single client.
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
  // Delegate for async event handlers in the app lifecycle.
  delegate AsyncEventHandler<TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<TEventArgs>(TEventArgs e)
  // Handles audio streaming, encoding, and decoding for apps
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
    ValueTask CloseAsync(string? streamId = null)
    // Gets information about an output stream if it exists.
    AudioOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Sends audio data to the Ikon server.
    ValueTask SendAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Sends audio data through the default speech mixer.
    void SendSpeech(AudioChunk audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Generate speech for text and play it to listeners. The verbose form
    // _speechCts?.Cancel();
    // _speechCts = new CancellationTokenSource();
    // Audio.SpeechMixer.FadeOut();
    // using var generator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
    // var config = new SpeechGeneratorConfig { Text = text, VoiceId = voiceId };
    // await foreach (var audio in generator.GenerateSpeechAsync(config, _speechCts.Token))
    // {
    //     Audio.SendSpeech(audio);
    // }
    // becomes
    // await Audio.SpeakAsync(text);
    // Each call interrupts the previous one — it fades out whatever is still playing and cancels the previous call's generation, which is what a voice app almost always wants (a new reply supersedes the old one). Uses ElevenFlash25 by default — cheap+fast, the platform's go-to tier for conversational TTS. Hand-roll the SpeechGenerator + SendSpeech loop instead when you need custom mixing (overlapping speakers), speech that must not interrupt what is already playing, raw access to the generated samples (duration math, waveform analysis), or generator config beyond text, voice, instructions, and speed (e.g. language).
    Task SpeakAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, string? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // Enable speech-to-text on captured audio. After calling this, every captured audio segment (typically initiated by a CaptureButton or PushToTalkButton) is transcribed when the segment ends, and SpeechRecognizedAsync fires with the recognized text and originating client context.
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01, bool requireCorrelatedStream = true, string language = "", TimeSpan? timeout = null)
    // Event raised when an incoming audio frame is received and decoded
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Event raised when an incoming audio stream begins
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    // Event raised when an incoming audio stream ends
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Event raised when speech-to-text recognition completes for a captured audio segment. Requires UseSpeechRecognition to be called once during app setup. Each press of a PushToTalkButton (or any other capture-button-initiated stream) produces one recognition event when the user releases. Args carry the recognized text plus the originating client context — no streamId-to-client plumbing needed.
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
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
  class AudioOutputStreamInfo : IEquatable<AudioOutputStreamInfo>
    ctor(string StreamId, int TrackId, AudioCodec Codec, int SampleRate, int ChannelCount)
    int ChannelCount { get; init; }
    AudioCodec Codec { get; init; }
    int SampleRate { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
  // Signals the server that the plugin is doing background work, preventing the idle shutdown timer from advancing. Supports ref counting for multiple concurrent background work scopes.
  class BackgroundWork
    // Signals that background work has started. Returns an IAsyncDisposable that calls StopAsync() on dispose. Multiple calls are ref counted; the server is only notified on the first Start and last Stop.
    ValueTask<IAsyncDisposable> StartAsync()
    // Signals that one unit of background work has completed. The server is only notified when the last active scope is stopped.
    ValueTask StopAsync()
  sealed class ClientAudioCaptureOptions : IEquatable<ClientAudioCaptureOptions>
    ctor()
    bool? AutoGainControl { get; init; }
    int? Bitrate { get; init; }
    static ClientAudioCaptureOptions Default { get; }
    string? DeviceId { get; init; }
    bool? EchoCancellation { get; init; }
    bool? NoiseSuppression { get; init; }
    IReadOnlyList<int>? TargetIds { get; init; }
  // Represents a contact picked from the client's contact list.
  sealed class ClientContact : IEquatable<ClientContact>
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    // The contact's email addresses.
    IReadOnlyList<string> Emails { get; init; }
    // The contact's names.
    IReadOnlyList<string> Names { get; init; }
    // The contact's phone numbers.
    IReadOnlyList<string> Phones { get; init; }
  // Provides convenient access to pre-agreed client-side functions. These functions are registered by clients (e.g., TypeScript SDK) and can be called from the server. Every function targets the calling client resolved from the current reactive scope by default; pass targetId to address another client session.
  static class ClientFunctions
    // Captures a single image from the client's camera.
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Requests the client to exit fullscreen mode.
    static Task<bool> ExitFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current battery level on the client.
    static Task<int?> GetBatteryLevelAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the browser language preference from the client.
    static Task<string?> GetLanguageAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current GPS location from the client.
    static Task<ClientLocation?> GetLocationAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the list of available media input devices on the client.
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current network connection type on the client.
    static Task<string?> GetNetworkTypeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Reads the client's current notification permission state.
    static Task<NotificationPermission> GetNotificationPermissionAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the browser timezone from the client.
    static Task<string?> GetTimezoneAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current browser URL path and query string from the client.
    static Task<string?> GetUrlAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current page visibility state on the client.
    static Task<string?> GetVisibilityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Prevents or allows the screen to sleep on the client.
    static Task<bool> KeepScreenAwakeAsync(bool enabled, int? targetId = null, CancellationToken cancellationToken = default)
    // Prompts the client to show its login UI (deferred login flow).
    static Task<bool> LoginShowAsync(string? reason = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Clears the auth session and reloads the page, returning the client to the login screen.
    static Task<bool> LogoutAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Opens an external URL in a new browser tab on the client.
    static Task<bool> OpenExternalUrlAsync(string url, int? targetId = null, CancellationToken cancellationToken = default)
    // Plays a sound on the client from a URL.
    static Task<string?> PlaySoundAsync(string url, double volume = 1, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Plays a sound on the client from a byte array. The sound data is cached per session, so subsequent calls with the same data will not re-transmit the audio.
    static Task<string?> PlaySoundAsync(byte[] data, string mimeType, double volume = 1, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Requests the client to enter fullscreen mode.
    static Task<bool> RequestFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Scrolls the page to a specific position on the client.
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the UI theme on the client.
    static Task<bool> SetThemeAsync(Theme theme, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the UI theme on the client by its wire name. Prefer SetThemeAsync for the built-in dark and light themes; this overload exists for custom theme names.
    static Task<bool> SetThemeAsync(string themeName, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the browser URL without triggering a page reload.
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Shows a notification on the client. The client requests notification permission lazily on the first send before displaying. Returns the client's resulting permission state.
    static Task<NotificationPermission> ShowNotificationAsync(NotificationContent content, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts audio capture on the client from the microphone.
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts video capture on the client from camera or screen.
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a media capture on the client by its stream ID.
    static Task<bool> StopCaptureAsync(string streamId, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a playing sound on the client.
    static Task<bool> StopSoundAsync(string playbackId, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices.
    static Task<bool> VibrateAsync(string pattern, int? targetId = null, CancellationToken cancellationToken = default)
  enum ClientHardwareAcceleration
    PreferHardware
    PreferSoftware
  sealed class ClientImageCapture : IEquatable<ClientImageCapture>
    ctor(string Mime, int Width, int Height, byte[] Data)
    byte[] Data { get; init; }
    int Height { get; init; }
    string Mime { get; init; }
    int Width { get; init; }
  enum ClientImageCaptureFormat
    Jpeg
    Png
  sealed class ClientImageCaptureOptions : IEquatable<ClientImageCaptureOptions>
    ctor()
    ClientImageCaptureFormat? Format { get; init; }
    int? Height { get; init; }
    double? Quality { get; init; }
    int? Width { get; init; }
  // Event arguments for the ClientJoinedAsync event.
  class ClientJoinedEventArgs : EventArgs
    ctor(Context clientContext)
    // Gets the context of the client that joined.
    Context ClientContext { get; }
    // Gets the session ID of the client that joined.
    int ClientSessionId { get; }
    // Gets the user ID of the client that joined, or an empty string if not authenticated.
    string UserId { get; }
  // Event arguments for the ClientLeftAsync event.
  class ClientLeftEventArgs : EventArgs
    ctor(Context clientContext)
    // Gets the context of the client that left.
    Context ClientContext { get; }
    // Gets the session ID of the client that left.
    int ClientSessionId { get; }
    // Gets the user ID of the client that left, or an empty string if not authenticated.
    string UserId { get; }
  // Represents a geolocation with latitude, longitude, and accuracy in meters.
  sealed class ClientLocation : IEquatable<ClientLocation>
    ctor(double Latitude, double Longitude, double Accuracy)
    // The accuracy of the coordinates in meters.
    double Accuracy { get; init; }
    // The latitude coordinate.
    double Latitude { get; init; }
    // The longitude coordinate.
    double Longitude { get; init; }
  // Represents a media input device available on the client.
  sealed class ClientMediaDevice : IEquatable<ClientMediaDevice>
    ctor(string DeviceId, string Kind, string Label, string GroupId)
    // The unique identifier for the device.
    string DeviceId { get; init; }
    // The group identifier for devices that share the same physical device.
    string GroupId { get; init; }
    // The type of device (audioinput or videoinput).
    string Kind { get; init; }
    // A human-readable label for the device.
    string Label { get; init; }
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
    // Check if user has a specific built-in role. For roles outside UserRole , check Roles directly.
    bool HasRole(UserRole role)
    // Require that the user has the specified role. Throws RoleRequiredException if not.
    void RequireRole(UserRole role)
  // Manages client profiles for an AI app. Profiles are loaded and cached when clients join, and GetProfileAsync loads any uncached profile from the backend on demand.
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
  enum ClientVideoCaptureCodec
    H264
    Vp8
    Vp9
    Av1
  sealed class ClientVideoCaptureOptions : IEquatable<ClientVideoCaptureOptions>
    ctor()
    int? Bitrate { get; init; }
    static ClientVideoCaptureOptions DefaultCamera { get; }
    static ClientVideoCaptureOptions DefaultScreen { get; }
    string? DeviceId { get; init; }
    int? Framerate { get; init; }
    ClientHardwareAcceleration? HardwareAcceleration { get; init; }
    int? Height { get; init; }
    int? KeyFrameIntervalFrames { get; init; }
    IReadOnlyList<ClientVideoCaptureCodec>? PreferredCodecs { get; init; }
    IReadOnlyList<int>? TargetIds { get; init; }
    int? Width { get; init; }
  enum ClientVideoCaptureSource
    Camera
    Screen
  // Marks a method to run on a cron schedule. Unlike HttpMethodAttribute / [Mcp], a cron job is not externally addressable — it has no path and no edge authorization. The platform discovers [Cron] methods at build time, records each in the app bundle manifest, and the backend schedules them; when a tick fires the app is run under the global (empty) session identity and the target function is invoked through the FunctionRegistry.
  sealed class CronAttribute : Attribute
    // Declares a cron job that runs on schedule .
    ctor(string schedule)
    // Optional registry-name override. When null or empty the function is registered (and triggered) under its full member name "{Type.FullName}.{Method}".
    string? Name { get; init; }
    // The cron expression that schedules this method (standard 5/6-field cron syntax, e.g. "0 * * * *" for hourly). Evaluated by the backend scheduler.
    string Schedule { get; }
  // Platform email surface for an Ikon app — sending custom emails through the platform mailer and reading inbound emails delivered to the app's space. Accessed via app.Email. All operations require the app's organisation/space to have the Email feature enabled; calls against a non-entitled space throw FeatureNotEnabledException .
  sealed class EmailService
    // Removes an inbound email and frees its attachment storage. Idempotent — deleting a missing message succeeds silently.
    Task DeleteAsync(string id, CancellationToken ct = default)
    // Streams a decrypted attachment from the platform. The returned EmailAttachmentDownload owns the content stream — dispose it (e.g. await using) when done.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = default)
    // Lazily enumerates all received emails matching query , transparently following pages until exhausted. Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single page of received emails for the app's space. Paginate by passing the returned NextCursor back as Cursor .
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single inbound email with decrypted body and parsed envelope.
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = default)
    // Sends a custom HTML email through the platform mailer. The platform sets the visible From address; pass ReplyTo to direct replies elsewhere. The send is enqueued for asynchronous delivery — a successful return means the platform has accepted the request, not that the recipient has received the message. Transient delivery failures are retried server-side. The total payload size (subject, body, attachments, metadata) is capped at roughly 10 MB.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
  // Shared base for the two developer-facing inbound HTTP surfaces, [Rest] and [Mcp]. They differ only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients; addressing, path templating, identity binding, auth, and abuse-control are identical and live here so there is exactly one place to reason about them.
  abstract class EndpointAttribute : Attribute
    // Built-in authorization for this endpoint, resolved at the gateway edge before (and without) provisioning the app. Defaults to Grant (a signed grant URL). Set AuthPolicy instead to name a custom /router/ policy.
    EndpointAuth Auth { get; init; }
    // Name of a custom /router/ edge policy that authorizes this endpoint (an apiKey/hmac/ipAllow helper you defined in router/index.ts). When set (non-empty) it takes precedence over Auth . Authorization lives in /router/, the single auth surface — not in C#.
    string? AuthPolicy { get; init; }
    // External path under the space domain (after {space}.ikonai.app/api). Optional: when omitted (empty) the path is derived from the method name (kebab-cased) — /{method} on the app class, /{cell-type}/{method} on a cell. A leading-slash path is absolute; a relative form ("bump") is resolved against the owner's auto-derived mount point at build time. Route params use {name} syntax. A {name} whose name matches a field of the owner's SessionIdentity record binds into the routing identity (the extrinsic resource the caller names); other {name} segments bind as ordinary handler parameters. Reserved paths the developer must NOT declare: /.well-known/* (RFC), and the /ikon/* + /api subtrees (platform-owned).
    string Path { get; }
  // The built-in authorization for an endpoint — the discoverable, no-/router/-needed options. For a custom edge policy (an apiKey/hmac/ipAllow helper you defined in /router/), set AuthPolicy to its name instead.
  enum EndpointAuth
    Grant
    Public
    Deny
  // Information about an HTTP endpoint exposed by the app — an [HttpGet]/[HttpPost]/[Mcp] surface. Returned by Endpoints for developer convenience.
  sealed class EndpointInfo : IEquatable<EndpointInfo>
    ctor()
    // The cell type for a substrate-cell endpoint (empty for app + AppProcess-cell endpoints). When non-empty, the gateway cell-routes the request to that cell's partitioned instance, keyed by the cell's IdentityFields in the URL; empty means the endpoint resolves to the app instance.
    string CellType { get; init; }
    // The endpoint's registry name — {Owner}_{Method} for typed endpoints (or the explicit FunctionAttribute.Name override). The backend resolves this name when routing.
    string FunctionName { get; init; }
    // The bare public URL for this endpoint under the space domain ({space}.ikonai.app/api/{path}), templated where the path has open {segment}s. It carries NO grant: a public endpoint is callable as-is; a grant/policy endpoint needs a working, identity-bound URL from IApp.MintUrl. The backend reverse-proxies to this instance — cold-starting it in the cloud, or routing to a registered local run.
    string PublicUrl { get; init; }
  sealed class FileUploadChunkArgs : IEquatable<FileUploadChunkArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, byte[] Data, long BytesWritten)
    long BytesWritten { get; init; }
    byte[] Data { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadCompleteArgs : IEquatable<FileUploadCompleteArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string? LocalTempFilePath, string? AssetUri)
    string? AssetUri { get; init; }
    string FileName { get; init; }
    string? LocalTempFilePath { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadErrorArgs : IEquatable<FileUploadErrorArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string ErrorMessage)
    string ErrorMessage { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadPreStartArgs : IEquatable<FileUploadPreStartArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, Func<string?, Task> Cancel)
    Func<string?, Task> Cancel { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadProgressArgs : IEquatable<FileUploadProgressArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, double ProgressPercentage, long BytesUploaded)
    long BytesUploaded { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    double ProgressPercentage { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Accept/reject decision returned from the onUploadPreStart and onUploadStart callbacks. Accepted defaults to true; return true; works via the implicit bool conversion. Set AssetUri to write the upload straight into the asset system instead of a local temp file.
  sealed class FileUploadResult : IEquatable<FileUploadResult>
    ctor()
    bool Accepted { get; init; }
    string? AssetUri { get; init; }
  sealed class FileUploadStartArgs : IEquatable<FileUploadStartArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string Hash)
    string FileName { get; init; }
    string Hash { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Marks a method as a DELETE REST endpoint. See EndpointAttribute .
  sealed class HttpDeleteAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Marks a method on an app or cell as a GET REST endpoint. The framework mounts a route on the owner's AppEndpointHost, binds the request, invokes the method, and serializes the return value; authorization runs at the gateway edge (the endpoint's Auth/router/ policy), not in-process. See EndpointAttribute for path templating and URL-supplied identity.
  sealed class HttpGetAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Shared base for the verb-named REST attributes ([HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch]). The verb is baked into the attribute type — there is no verb enum — which mirrors the ASP.NET Core idiom and so generates reliably from LLMs. All of them share the addressing + identity model on EndpointAttribute ; only the HTTP method differs.
  abstract class HttpMethodAttribute : EndpointAttribute
    // HTTP verb as an uppercase string (GET / POST / PUT / DELETE / PATCH).
    string Method { get; }
  // Marks a method as a PATCH REST endpoint. See EndpointAttribute .
  sealed class HttpPatchAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Marks a method as a POST REST endpoint — the common case (third-party webhooks included; verify the signature from the injected request context). See EndpointAttribute .
  sealed class HttpPostAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Marks a method as a PUT REST endpoint. See EndpointAttribute .
  sealed class HttpPutAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Serializable view of an inbound HTTP request — its method, path, query, headers, and raw body. The dispatcher constructs one per inbound request; a handler reads it (e.g. via HttpCallContext) for the untrusted inputs the typed binding doesn't surface, such as verifying a webhook signature inline.
  sealed class HttpRequest : IEquatable<HttpRequest>
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  // Typed return value from an HttpMethodAttribute -annotated method. Endpoints can return any serializable type for an automatic 200 + JSON response, or return an HttpResult when they need control over status code, content type, or custom body serialization.
  sealed class HttpResult : IEquatable<HttpResult>
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
  // Base interface for Ikon app hosts providing access to shared state, reactive infrastructure, and lifecycle events.
  interface IAppBase : IMessageChannel
    // Gets the background work tracker that prevents server idle shutdown while work is in progress.
    BackgroundWork BackgroundWork { get; }
    // The Context of the client currently being served — the one rendering the UI or firing the current handler, resolved from the active reactive scope. null when no client is in scope (e.g. background work). Use this to identify the current client — never a plugin's own connection context. For the joining client's context use the ClientJoined event args instead.
    Context? CurrentClientContext { get; }
    // The user id of the client currently being served, or an empty string when no client is in scope. Always populated for a connected client — the real user id for authenticated users, a stable anonymous id otherwise. This is the correct source for a payment customer key, subscription gating, per-user state, etc.
    string CurrentUserId { get; }
    // Gets the path to the Data directory for this app. Files placed in the Data folder of the app project can be accessed at runtime using this path. Note: in cloud, this directory is read-only and writing to it will throw an exception.
    string DataDirectory { get; }
    // Gets the database connection configurations for this app instance.
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // Gets the email service for this app — sending custom emails through the platform mailer and reading inbound emails delivered to this app's space. Requires the Email feature to be enabled on the app's organisation/space; calls against a non-entitled space throw FeatureNotEnabledException .
    EmailService Email { get; }
    // Gets the HTTP endpoints ([HttpGet]/[HttpPost]/[Mcp] surfaces) exposed by this app instance, including ready-to-use public URLs with the current session identity and signed token prefilled. The list is built once before Main() runs, from the endpoints declared on the app class and on loaded [Cell] types.
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    // Gets the platform-wide shared state from the server containing clients, streams, and space/channel info.
    GlobalState GlobalState { get; }
    // The loopback endpoint (host + HTTPS port) of THIS instance's own local server, but ONLY when the server's own URL is a localhost address — i.e. local dev WITHOUT --public-access. This lets an in-process client (e.g. a simulated player, a self-test harness) connect directly over loopback to this exact process instead of routing through the relay. It returns null when the instance is exposed via the relay (--public-access) or runs in the cloud — there the server's own URL is the relay/space URL, a direct socket can't (and shouldn't) reach it, and callers should use the normal relay/ApiKey connect path (which routes to this registered serving instance) instead. The default is null for hosts that don't run a local server; IApp overrides it.
    (string Host, int Port)? LocalLoopbackEndpoint { get; }
    // The maximum number of clients this app instance accepts. Initialized to the server's memory-derived limit (computed from the instance's memory budget), so reading it tells you the default ceiling for this instance. You may set it lower to cap the instance below that default, or higher if you know your app's per-client cost is small enough to support more — once the app sets a value it fully overrides the memory-derived default. Once the limit is reached the server rejects further connections. Changes take effect immediately; the new limit is sent to the server.
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
    // The app's public URL — the address a browser opens to join this app instance's channel. Replaces the app.ReactiveGlobalState.ChannelUrl.Value incantation; reading it inside UI code subscribes to changes the same way. For a URL with query parameters (e.g. a session join link) use JoinUrl .
    string PublicUrl { get; }
    // Gets the reactive wrapper around GlobalState that provides change notifications.
    ReactiveGlobalState ReactiveGlobalState { get; }
    // Gets the secrets (tokens, API keys, passwords) configured for this app. Values are fetched from the Ikon backend once at app startup and exposed synchronously; changes made via ikon app secret set while the app is running only take effect after a restart.
    Secrets Secrets { get; }
    // Whether this app instance offers the raw UDP / UDP-DTLS transports to connecting clients. Enabled by default. Set to false to disable them. Like WebRtcEnabled this takes effect for clients that connect after it is set (the transports are no longer advertised); already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    // Whether this app instance offers WebRTC transport to connecting clients. Enabled by default. Set to false (e.g. in Main) to disable WebRTC for apps that don't use audio/video or low-latency data — WebRTC peer setup (ICE candidate gathering, DTLS) is a notable per-client memory and allocation cost. Takes effect for clients that connect after it is set: the server stops advertising WebRTC and ignores WebRTC signaling, so no per-client peer state is created. Already-connected clients keep their channels until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Creates a platform-managed eID-backed PAdES signature order for the supplied document(s). The platform navigates the signer's browser to the signing-ceremony URL through the existing client UI surface, awaits the asynchronous packaging completion, and resolves the returned task with the signed PDF and evidence metadata. The returned bytes are the long-term-validation PAdES PDF when the chosen scheme produces it; apps should persist them as the system of record because the platform's session retention is short.
    abstract Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default)
    // Creates a DbConnection for one of the app's configured databases (the Databases list in the app's env-specific ikon-config toml, applied with ikon app config and surfaced via Databases ) by name; the caller opens and disposes it: await using var connection = app.Database("mydb");.
    virtual DbConnection Database(string databaseName)
    // Build a shareable link to this app: PublicUrl plus a query string built from queryParams — an anonymous object (or a string dictionary), following the identity-by-anonymous-object shape of MintUrlAsync . Each readable property becomes a URL-encoded name=value pair; null-valued properties are skipped. So app.JoinUrl(new { id = sessionId }) yields {PublicUrl}?id={sessionId}. Replaces hand-assembling $"{app.ReactiveGlobalState.ChannelUrl.Value}?id={sessionId}". Passing null returns PublicUrl as-is.
    virtual string JoinUrl(object? queryParams = null)
    // Mint a working, identity-bound URL for one endpoint — the single way to get a callable URL for a grant (default) or policy endpoint. You identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), NOT by its URL path — the path is often derived from the method name (and may be templated), so the path is what minting RETURNS, not what you pass in. The returned URL is the endpoint's PublicUrl with any pinned {placeholder} path segments substituted and a signed ?ikon-grant= appended. identity (an anonymous object, e.g. new { DocumentId = "doc-42" }, or a string dictionary) PINS those identity fields into the grant; fields you omit stay open {captures} for the caller to fill. Omitting identity entirely ( null ) pins THIS instance's own session identity, so the URL routes back to this app instance — the common case. Grants are non-expiring by default — pass expiresIn only for an ephemeral link, and an optional group to revoke a batch together via RevokeGroupAsync . Re-minting the same stable (non-expiring) URL returns an identical URL, so it survives restarts.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // Mint working URLs for several endpoints sharing one pinned identity, in a single backend round-trip. Returns a map keyed by the endpoints you passed. See MintUrlAsync .
    virtual Task<IReadOnlyDictionary<string, MintedUrl>> MintUrlsAsync(IEnumerable<string> endpoints, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // Dynamically requests a raw TCP/TLS/UDP endpoint. Returns a RelayEndpoint whose LocalPort a listener should bind to; the endpoint is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the returned endpoint to release it. For HTTP/HTTPS endpoints use AppEndpointHost .
    abstract Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", int localPort = 0, CancellationToken ct = default)
    // Requests a fresh strong-authentication step-up challenge for the current user. Navigates the client browser to the platform's configured identity provider through the existing client UI surface, waits for the user to complete the challenge, and returns the platform-signed step-up assertion JWT. Apps must verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier .
    abstract Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = default)
    // Revoke every URL minted under a shared group tag.
    virtual Task RevokeGroupAsync(string group, CancellationToken ct = default)
    // Revoke a single minted URL by its GrantId .
    virtual Task RevokeUrlAsync(string grantId, CancellationToken ct = default)
    // Event fired when a client joins the session.
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    // Event fired when a client leaves the session.
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    // Event fired for each protocol message received from the server.
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    // Event fired after app instance creation but before Main() is called. Do not subscribe to this event inside Main() as it will not be called after Main. Primarily used by app extensions that receive the host as a constructor parameter.
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    // Event fired before the plugin disconnects, allowing cleanup of resources.
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
  // Convenience subscription helpers for the lifecycle events on IAppBase . The raw event handler shape is AsyncEventHandler<TEventArgs> which expects a single EventArgs parameter — LLM-generated code routinely reaches for app.StartingAsync += async () => ... (zero-arg) or async (sender, args) => ... (two-arg, .NET prior). Both fail to compile against the canonical one-arg delegate. These extension methods accept the LLM-natural shapes directly: app.OnStarting(async () => ...) wires the underlying event; app.OnClientJoined(async ctx => ...) passes the Context straight through so the handler doesn't need to remember to drill into the event-args wrapper.
  static class IAppEventExtensions
    // Subscribe to ClientJoinedAsync with a handler that receives the joining client's Context directly (SessionId, UserId, etc) — skipping the ClientJoinedEventArgs wrapper the raw event emits.
    static void OnClientJoined(this IAppBase app, Func<Context, Task> handler)
    // Subscribe to ClientJoinedAsync with a handler that receives both the joining client's Context AND its typed TClientParameters . Replaces the awkward app.Clients[ctx.SessionId]!.Parameters drill inside the handler body.
    static void OnClientJoined<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to ClientLeftAsync with a handler that receives the departing client's Context directly.
    static void OnClientLeft(this IAppBase app, Func<Context, Task> handler)
    // Subscribe to ClientLeftAsync with a handler that receives both the departing client's Context AND its typed TClientParameters .
    static void OnClientLeft<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to MessageReceivedAsync with a handler that receives the protocol message directly.
    static void OnMessageReceived(this IAppBase app, Func<ProtocolMessage, Task> handler)
    // Subscribe to StartingAsync with a zero-arg async handler. The Starting event carries no data — there's nothing to forward.
    static void OnStarting(this IAppBase app, Func<Task> handler)
    // Subscribe to StoppingAsync with a zero-arg async handler.
    static void OnStopping(this IAppBase app, Func<Task> handler)
  // App host interface providing typed session identity and client parameters.
  interface IApp<TSessionIdentity, TClientParameters> : IAppBase, IMessageChannel
    // Gets the typed parameters for the current client (determined by ReactiveScope). Must be called inside UI.Root() or a ReactiveScope context.
    TClientParameters ClientParameters { get; }
    // Gets the collection of connected clients with typed parameters. Automatically synced with GlobalState .
    IClientCollection<TClientParameters> Clients { get; }
    // Gets the typed session identity used to determine app instance routing.
    TSessionIdentity SessionIdentity { get; }
  // Collection interface for accessing connected clients. Iterable for the common "broadcast / fan-out" pattern (`foreach (var client in app.Clients)`), indexable by session ID for direct lookups, and exposes Ids when only the connected-session-ids are needed.
  interface IClientCollection<TClientParameters> : IEnumerable, IEnumerable<IClient<TClientParameters>>
    // Gets the number of currently connected clients.
    int Count { get; }
    // Gets the connected client session IDs as an enumerable. Convenience for code that just needs the IDs without the full client objects — e.g. `foreach (var id in app.Clients.Ids) { _scores[id] = 0; }`.
    IEnumerable<int> Ids { get; }
    // Gets the client with the specified session ID, or null if not found.
    IClient<TClientParameters>? this[int clientSessionId] { get; }
  // Interface representing a connected client with typed parameters.
  interface IClient<TClientParameters>
    // Gets the typed parameters for this client.
    TClientParameters Parameters { get; }
    // Gets the session id of this client — the same id used to index IClientCollection and to target client-directed APIs.
    int SessionId { get; }
  // Marker interface for custom profile attribute classes. Implement this interface on classes that define custom profile attributes.
  interface IProfileAttributes
  // Marks a method on an app or cell as an MCP tool. The framework discovers these at startup, reflects the method's parameters into a JSON Schema, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP tools/call requests to it.
  sealed class McpAttribute : EndpointAttribute
    // Declares an MCP tool whose own endpoint path is the kebab-cased method name.
    ctor()
    // Declares an MCP tool whose own directly-callable endpoint is served at path .
    ctor(string path)
    // Description shown to MCP clients so the agent's LLM can decide when to invoke the tool. Empty values pass through verbatim — there is no XML-summary fallback.
    string Description { get; init; }
    // MCP-wire tool name presented to clients in tools/list. Defaults to the method name when null or empty. The governance subject id is always "{Type}.{Method}" regardless of this.
    string? Name { get; init; }
  // Marks a method on a cell as an MCP-exposed resource — read-only data addressed by a URI. The framework reflects the method's parameters into a URI template, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP resources/read requests against the matching URI.
  sealed class McpResourceAttribute : Attribute
    ctor(string uriTemplate)
    // Description shown to MCP clients so the agent (or user, via the client UI) can decide when to fetch the resource. Empty values pass through verbatim.
    string Description { get; init; }
    // MIME type advertised to clients. Defaults to text/plain for string returns and application/octet-stream for binary; override here to be more specific (text/markdown, application/json, image/png, etc.).
    string MimeType { get; init; }
    // Display name shown to MCP clients. Defaults to the method name when null or empty.
    string? Name { get; init; }
    // URI or URI template (RFC-6570 Level 1: {name} placeholders only). Required. Placeholder names must match the cell method's parameter names exactly. The scheme is author-chosen — common conventions are file:///, {cellname}://, or domain-specific scheme like order://, policy://.
    string UriTemplate { get; }
  // Event arguments for the MessageReceivedAsync event.
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    // Gets the received protocol message.
    ProtocolMessage Message { get; }
  // A minted endpoint URL: the working Url (the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended), the GrantId to revoke it by, and the optional ExpiresAt when a TTL was requested (grants are non-expiring by default).
  sealed class MintedUrl : IEquatable<MintedUrl>
    ctor(string Url, string GrantId, DateTimeOffset? ExpiresAt)
    DateTimeOffset? ExpiresAt { get; init; }
    string GrantId { get; init; }
    string Url { get; init; }
  class Navigation
    Task<string?> GetPathAsync(int targetId)
    Task<string?> GetPathAsync()
    Task<bool> SetPathAsync(int targetId, string path, bool replace = false)
    Task<bool> SetPathAsync(string path, bool replace = false)
    event AsyncEventHandler<NavigationPathChangedEventArgs> PathChangedAsync
  class NavigationPathChangedEventArgs : EventArgs
    ctor(string url, Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string Path { get; }
    string Url { get; }
    string UserId { get; }
  // Content of a user-facing notification surfaced on the client device (browser notification on the web, OS notification on Flutter native apps).
  sealed class NotificationContent : IEquatable<NotificationContent>
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
    Default
    Granted
    Denied
    Unsupported
  // Outcome of sending a notification to a single client session.
  sealed class NotificationSendResult : IEquatable<NotificationSendResult>
    ctor(int SessionId, bool Delivered, NotificationPermission Permission)
    // True when the client actually displayed the notification (permission granted).
    bool Delivered { get; init; }
    // The client's resulting permission state after the send attempt.
    NotificationPermission Permission { get; init; }
    // The target client session id.
    int SessionId { get; init; }
  // Platform notification surface for an Ikon app — shows user-facing notifications on connected clients. Accessed via app.Notifications. Connected clients receive the notification immediately (foreground). Permission is requested lazily on the client the first time a notification is actually sent, not when the app opens. SendToUserAsync fans out to every connected session for that user; if the user has no connected session it falls back to offline push (an OS notification) through the backend push hub. Offline push is server-orchestrated: when a foreground send is granted, the client's push subscription is fetched and registered with the backend, which then delivers via Web Push / FCM while the user is disconnected.
  sealed class NotificationService
    // Shows a notification on all currently-connected client sessions. Returns one result per session.
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    // Reads a client's current notification permission state without sending anything.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    // Shows a notification on a single connected client session. The client requests notification permission lazily (on this first send) before displaying. Returns the per-session delivery and permission outcome.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // Shows a notification on every currently-connected session belonging to userId (a user may be connected from several devices). When the user has no connected session, falls back to offline push — an OS notification delivered through the backend push hub. Returns one result per targeted session (empty when the user was offline and only push was attempted).
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = default)
  // A ReactiveList persisted globally for the app within its space. Shared across all session identities and users; one list per app deployment.
  class PersistentReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A reactive value persisted globally for the app within its space. Shared across all session identities and users; one value per app deployment.
  class PersistentReactive<T> : Reactive<T>
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A ReactiveList persisted per session identity. Apps with the same routing key share the same list; different routing keys have isolated lists.
  class PersistentSessionReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A reactive value persisted per session identity. Apps with the same routing key share the same value; different routing keys have isolated values.
  class PersistentSessionReactive<T> : Reactive<T>
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A ReactiveList persisted per user, partitioned at runtime by UserScope . Each user sees their own list across all of their client sessions.
  class PersistentUserReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A reactive value persisted per user, partitioned at runtime by UserScope . Each user sees their own value across all of their client sessions.
  class PersistentUserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Read-only view of a client's address.
  sealed class ProfileAddress
    string? City { get; }
    string? Country { get; }
    string? Municipality { get; }
    string? State { get; }
    string? Street { get; }
    string? Zip { get; }
  // Mutable class for updating profile fields. Only properties that are set will be sent to the backend.
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
  // Event arguments raised when speech has been recognized from a captured audio stream.
  sealed class SpeechRecognizedEventArgs : EventArgs
    ctor(string text, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount)
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
    // User id of the speaker.
    string UserId { get; }
  // Event arguments for the StartingAsync event.
  class StartingEventArgs : EventArgs
    ctor()
  // Event arguments for the StoppingAsync event.
  class StoppingEventArgs : EventArgs
    ctor()
  // The built-in client UI themes. The wire protocol carries the theme as a string (custom theme names are allowed via SetThemeAsync ); ToThemeName maps these values to their wire names.
  enum Theme
    Dark
    Light
  // Helpers for mapping Theme values to and from the wire strings used by the client.
  static class ThemeExtensions
    // True when the client's reported theme is the dark theme. False for the light theme, custom theme names, and clients that have not reported a theme.
    static bool IsDarkTheme(this Context clientContext)
    // Returns the wire name of the theme: "dark" or "light".
    static string ToThemeName(this Theme theme)
  // Built-in user roles. Maps to role strings stored in profile.
  enum UserRole
    Guest
    User
    Moderator
    Admin
  // Handles video streaming for apps
  class Video
    ctor(IAppBase app)
    // Closes all video streams.
    ValueTask CloseAllAsync()
    // Closes a video stream and sends the stream end message.
    ValueTask CloseAsync(string? streamKey = null)
    // Gets information about an output stream if it exists.
    VideoOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Sends a video frame to the Ikon server.
    ValueTask SendAsync(byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, VideoCodec codec, int width, int height, double framerate, string? streamId = null, IReadOnlyList<int>? targetIds = null, int? trackId = null)
    // Event raised when an incoming video frame is received
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
  class VideoOutputStreamInfo : IEquatable<VideoOutputStreamInfo>
    ctor(string StreamId, int TrackId, VideoCodec Codec, int Width, int Height, double Framerate)
    VideoCodec Codec { get; init; }
    double Framerate { get; init; }
    int Height { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
    int Width { get; init; }

namespace Ikon.App.Cells
  // Marks a class as a cell — a headless app addressed by a SessionIdentity record declared inside the class. Discovered by CellHost at startup via reflection over loaded assemblies.
  sealed class CellAttribute : Attribute
    ctor()
    // Number of concurrent instances per addressable key. Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin Resolve across them. For globals (parameterless SessionIdentity) the N instances are eager-spawned at host construction — the load-balanced auth-cell pattern. For keyed cells the N instances are spawned together on first access; sharded keyed cells must tolerate eventual consistency between shards (cells should hold no per-instance state, or persist shared state through an external store).
    int Capacity { get; init; }
    // How long a keyed cell may remain idle before EvictIdle removes it from the directory. Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
    // Where this cell type is hosted. AppProcess (the default) keeps the cell in the app's own `CellHost` — every app process has its own copies, state is not shared across processes. Substrate declares that the cell should be hosted on the platform's cell-deployment substrate, where one instance per (cell-type, SessionIdentity) is shared across all app processes that connect.
    CellProcessScope ProcessScope { get; init; }
  // Where a CellAttribute -decorated type's instances live.
  enum CellProcessScope
    AppProcess
    Substrate
  // Per-server-scoped accessor (via AsyncLocalInstance — use Cells.Instance) for that server's CellHost plus the wiring substrate-cell proxies need: the endpoint-URL resolver (for [HttpGet]/[HttpPost] methods) and the cell-client factory (for [Function] methods and Reactive<T> state, which ride a standard IkonClient SDK connection to the cell-host).
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // Resolve (or spawn on first call) the cell implementation for TInterface keyed by sessionIdentity . Subsequent calls with an equal SessionIdentity return the same instance.
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    // Dispose every live cell-host connection. Call on app shutdown. Idempotent.
    ValueTask DisposeAsync()
    // Reserved key in an SDK connection's parameters that names the substrate cell type to route to. The cell's SessionIdentity-record fields ride alongside it. MUST stay in sync with the cloud's CELL_TYPE_PARAM in cell-routing.ts — that's what ChannelInstanceService.create keys on to provision a cell-host channel-instance.
    static string CellTypeParam
  // Framework handle injected into a cell's primary constructor. Exposes the SessionIdentity the cell was instantiated for; future revisions add lifetime, config, etc.
  interface ICell<TSessionIdentity>
    // The SessionIdentity record value this cell instance is keyed by.
    TSessionIdentity Identity { get; }

namespace Ikon.App.Connectors
  // Thrown when a connector's remote service returns an error response.
  sealed class ConnectorException : Exception
    ctor(string provider, string message)
    string Provider { get; }
  // Google Drive connector. Upload, download and list files with Google OAuth2 credentials. Raw — the agent skill lives in Ikon.Agent.Connectors.
  sealed class Drive
    ctor(GoogleCredentials credentials)
    Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    // Stream every file under a folder (or the whole drive), paging through the full result set. Pass an extra query clause such as "modifiedTime > '2024-01-01T00:00:00'" to bound a historical backfill by time.
    IAsyncEnumerable<DriveFile> ListAllAsync(string? folderId = null, string? extraQuery = null, CancellationToken ct = default)
    Task<IReadOnlyList<DriveFile>> ListAsync(string? folderId = null, int limit = 50, CancellationToken ct = default)
    Task<DriveFile> UploadAsync(string name, string mimeType, Stream content, string? folderId = null, CancellationToken ct = default)
  sealed class DriveFile : IEquatable<DriveFile>
    ctor(string Id, string Name, string MimeType, long? Size, string? WebViewLink, DateTimeOffset? ModifiedTime = null)
    string Id { get; init; }
    string MimeType { get; init; }
    DateTimeOffset? ModifiedTime { get; init; }
    string Name { get; init; }
    long? Size { get; init; }
    string? WebViewLink { get; init; }
  static class GoogleAuth
    static UserCredential CredentialFor(GoogleCredentials credentials, IEnumerable<string> scopes)
    // True when ex is a PERMANENT OAuth failure (revoked/expired refresh token, bad client) that retrying won't fix — the account must be reconnected. Lets connectors stop and surface a distinct "reconnect required" state instead of hammering the token endpoint forever.
    static bool IsAuthFailure(Exception ex)
  // OAuth2 credentials for Google connectors. The refresh token is long-lived; the access token is obtained and refreshed automatically by the Google client library.
  sealed class GoogleCredentials : IEquatable<GoogleCredentials>
    ctor(string ClientId, string ClientSecret, string RefreshToken)
    string ClientId { get; init; }
    string ClientSecret { get; init; }
    string RefreshToken { get; init; }
  // Slack messaging connector. Post and read messages with a bot token (xoxb-...). Raw — no agent coupling; the agent skill lives in Ikon.Agent.Connectors.
  sealed class Slack
    ctor(string botToken, HttpClient? http = null)
    Task<IReadOnlyList<SlackMessage>> HistoryAsync(string channel, int limit = 20, CancellationToken ct = default)
    Task<SlackMessage> PostAsync(string channel, string text, string? threadTs = null, CancellationToken ct = default)
  sealed class SlackMessage : IEquatable<SlackMessage>
    ctor(string Channel, string User, string Text, string Ts, string? ThreadTs = null)
    string Channel { get; init; }
    string Text { get; init; }
    string? ThreadTs { get; init; }
    string Ts { get; init; }
    string User { get; init; }
  // WhatsApp messaging connector (WhatsApp Business Cloud API via Meta Graph). Send with a system-user access token and the sender's phone number id. Raw — the agent skill lives in Ikon.Agent.Connectors.
  sealed class WhatsApp
    ctor(string accessToken, string phoneNumberId, HttpClient? http = null)
    Task<string> SendAsync(string to, string text, CancellationToken ct = default)

namespace Ikon.App.Cron
  // Per-invocation context for a CronAttribute handler currently executing. A cron handler may optionally accept one of these (and/or a CancellationToken ) to learn when and why it fired; a parameterless handler is equally valid. AsyncLocal so handler code (and anything it calls) can read it without threading it through every method signature.
  sealed class CronContext : IEquatable<CronContext>
    ctor(DateTime FireTimeUtc, string Schedule)
    // The cron context for the invocation currently running on this async flow, or null.
    static CronContext? Current { get; }
    DateTime FireTimeUtc { get; init; }
    string Schedule { get; init; }
    static IDisposable Use(CronContext context)

namespace Ikon.App.Http
  // Per-request context for an HttpMethodAttribute handler currently executing. AsyncLocal so handler code (and anything it calls) can read the request's resolved identity without threading the dict through every method signature. Relationship to other "context" concepts on the platform: SessionIdentity (the typed app/cell record): the routing / instance-partition key. Always present — it's what was used to address the channel-instance this handler runs in. Stable across the cell instance's lifetime.Context (Ikon protocol Context for WS clients): the live client *connection* — sessionId, deviceId, AuthSessionId, UserId from the connect-token. Absent for endpoint/MCP dispatches because there is no live client connection.HttpCallContext.Current (this) and McpCallContext .Current: the *request-scoped overlay* that exposes the per-call resolved identity for handler code to read. Set by the wrapper before the handler runs, cleared after. The point is that handlers reading "who is this call for?" get a non-empty answer on endpoint/MCP-dispatched calls, where the connection-level Context.UserId would be empty. The handler's SessionIdentity record (resolved by CellHost.ResolveByCellTypeName before this context is set) and HttpCallContext.Current.SessionIdentity carry the same information in different shapes: the former is typed and tied to the cell's lifetime; the latter is the raw wire dict tied to the call's lifetime. Headers and RawBody are the UNTRUSTED request inputs, exposed so a handler can do its own logic inline (e.g. verify a Stripe-Signature against the raw body) without a separate auth cell. They must never feed identity resolution — the target instance is already chosen from trusted sources (a signed ikon-grant / policy claims / platform-controlled path+query) before the handler runs, so reading a header cannot retarget the call.
  sealed class HttpCallContext : IEquatable<HttpCallContext>
    ctor(IReadOnlyDictionary<string, string>? SessionIdentity = null, CancellationToken CancellationToken = default, IReadOnlyDictionary<string, string>? Headers = null, string? RawBody = null)
    CancellationToken CancellationToken { get; init; }
    static HttpCallContext? Current { get; }
    IReadOnlyDictionary<string, string>? Headers { get; init; }
    string? RawBody { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentity { get; init; }
    // Convenience accessor for the conventional userid field of the request's SessionIdentity. Returns null when no HttpCallContext is current or when the identity dict has no userid key (e.g. an anonymous endpoint with no identity-bearing fields). Case-insensitive lookup — the same dict is built by the backend funnel from open `{userid}` path captures, policy claims, and a signed `ikon-grant`'s pinned identity.
    string? UserId { get; }
    // Case-insensitive lookup of a request header. UNTRUSTED request input — read it for handler logic (e.g. endpoint signature verification), NEVER to derive the SessionIdentity. Identity is resolved upstream before the handler runs and is the only thing that picks the target instance; headers cannot move it. Returns null when the header is absent. The accessor is case-insensitive because HTTP header names are, and the two dispatch paths build the header dictionary with different comparers.
    string? Header(string name)
    static IDisposable Use(HttpCallContext context)

namespace Ikon.App.Mcp
  // Per-request context for an MCP tools/call or resources/read in flight. AsyncLocal so the bridge can read it from inside parameter binding without threading another argument through every call site. Carries: The request's effective CancellationToken (linked to the transport CT and a per-request CTS the host can trip on notifications/cancelled).An optional progress sink the bridge wires IProgress parameters into. SessionIdentityFields — the authenticated identity for this request (from claims merged by the transport). Bridges pass it to CellHost.ResolveByCellTypeName so keyed cells route to the right instance. Empty / null on the stdio path (single-user process).
  sealed class McpCallContext : IEquatable<McpCallContext>
    ctor(CancellationToken CancellationToken, Func<ProgressUpdate, Task>? OnProgress, IReadOnlyDictionary<string, string>? SessionIdentityFields = null)
    CancellationToken CancellationToken { get; init; }
    static McpCallContext? Current { get; }
    Func<ProgressUpdate, Task>? OnProgress { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentityFields { get; init; }
    // Convenience accessor for the conventional userid field of the request's SessionIdentity. Returns null when no McpCallContext is current or when claims carried no userid. Mirror of UserId — same semantics across both request-scoped contexts.
    string? UserId { get; }
    static IDisposable Use(McpCallContext context)
  // One progress update emitted by a long-running tool. Progress is a monotonic counter; Total is optional but expected to stay constant across updates so clients can render a percentage. Message is freeform display text.
  sealed class ProgressUpdate : IEquatable<ProgressUpdate>
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
  sealed class OfferPriceSpec : IEquatable<OfferPriceSpec>
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval? Interval = null, int? IntervalCount = null)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval? Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  // Defines an offer to create via CreateOfferAsync .
  sealed class OfferSpec : IEquatable<OfferSpec>
    ctor(string OfferId, string Name, OfferPriceSpec Price)
    string Name { get; init; }
    string OfferId { get; init; }
    OfferPriceSpec Price { get; init; }
  // A single payment record (a one-off charge or a subscription renewal). OfferId is null for ad-hoc charges and records written before offer tracking.
  sealed class Payment : IEquatable<Payment>
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
  // A customer's access to an offer, whether from an active subscription or a one-time purchase. This is the access-control answer the [PaymentsRequireEntitlement] policy gates on. Subscription access carries ExpiresAt (period end plus a grace window) and reports inactive once it has passed; a one-time purchase has no expiry.
  sealed class PaymentEntitlement : IEquatable<PaymentEntitlement>
    ctor(string OfferId, bool Active, DateTimeOffset? ExpiresAt, EntitlementSource Source)
    bool Active { get; init; }
    DateTimeOffset? ExpiresAt { get; init; }
    string OfferId { get; init; }
    EntitlementSource Source { get; init; }
  // A normalized payment event the backend pushes to the app.
  sealed class PaymentEvent : IEquatable<PaymentEvent>
    ctor(string EventId, PaymentProvider? Provider, PaymentEventType? Type, DateTimeOffset? OccurredAt, long Sequence, string PayloadJson)
    string EventId { get; init; }
    DateTimeOffset? OccurredAt { get; init; }
    string PayloadJson { get; init; }
    PaymentProvider? Provider { get; init; }
    long Sequence { get; init; }
    PaymentEventType? Type { get; init; }
    // The normalized projection as a JSON element.
    JsonElement Payload()
  // The kind of a normalized PaymentEvent .
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
  // A provider-hosted page the customer is redirected to in order to pay. Send them to Url .
  sealed class PaymentLink : IEquatable<PaymentLink>
    ctor(string Url, string Reference, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    string Reference { get; init; }
    string Url { get; init; }
  // A purchasable offer in the app's catalog — recurring (subscription) or one-time, per its prices.
  sealed class PaymentOffer : IEquatable<PaymentOffer>
    ctor(string OfferId, string Name, IReadOnlyList<PaymentPrice> Prices)
    string Name { get; init; }
    string OfferId { get; init; }
    IReadOnlyList<PaymentPrice> Prices { get; init; }
  // One price on an offer. Interval and IntervalCount are meaningful only when Kind is Recurring ; a one-time price reports Unknown .
  sealed class PaymentPrice : IEquatable<PaymentPrice>
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval Interval, int? IntervalCount)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  // The payment provider that moves the money. A command uses the space's enabled provider unless it names one, either per call or by pinning DefaultProvider .
  enum PaymentProvider
    Stripe
    Mollie
    Surfboard
  // A receipt for a completed payment. Url is a provider-hosted receipt page. Pdf holds downloadable PDF bytes only when the provider exposes one; today every provider (Stripe, Surfboard) returns a hosted URL only, so Pdf is null — the field is populated when a provider offers a PDF.
  sealed class PaymentReceipt : IEquatable<PaymentReceipt>
    ctor(string? Url, byte[]? Pdf, string? PdfContentType)
    byte[]? Pdf { get; init; }
    string? PdfContentType { get; init; }
    string? Url { get; init; }
  // Result of a ReconcileAsync request. Enqueued counts the provider objects queued for re-processing; their effects arrive asynchronously as normal payment events.
  sealed class PaymentReconcileResult : IEquatable<PaymentReconcileResult>
    ctor(PaymentProvider? Provider, int Enqueued)
    int Enqueued { get; init; }
    PaymentProvider? Provider { get; init; }
  // Result of a refund.
  sealed class PaymentRefund : IEquatable<PaymentRefund>
    ctor(string Reference, RefundStatus Status)
    string Reference { get; init; }
    RefundStatus Status { get; init; }
  // The outcome of a Payment .
  enum PaymentStatus
    Unknown
    Pending
    Paid
    Failed
    Canceled
  // A customer's live subscription, created by paying for a recurring offer.
  sealed class PaymentSubscription : IEquatable<PaymentSubscription>
    ctor(string Id, PaymentProvider? Provider, SubscriptionStatus Status, string? OfferId, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd)
    bool CancelAtPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    string Id { get; init; }
    string? OfferId { get; init; }
    PaymentProvider? Provider { get; init; }
    SubscriptionStatus Status { get; init; }
  // Declares the function requires the current customer to hold an active entitlement for offerId — access granted by an active subscription or a one-time purchase. Resolves the customer from UserId and reads the entitlement from Instance . On missing access it DENIES with a stable code (payments_entitlement_required); the app's UI catches it and opens a payment link via CreatePaymentLinkAsync . The provider webhook then flips the entitlement and the user retries.
  sealed class PaymentsRequireEntitlementAttribute : PolicyAttribute
    ctor(string offerId)
    // Offer the entitlement is keyed to.
    string OfferId { get; }
    override IFunctionPolicy CreatePolicy()
  // App-level entry point for payments, reached via app.Payments. The app creates payment links (for an offer or an ad-hoc amount) and reacts to PaymentEventReceived events. Every command accepts an optional per-call provider override; when none is given the backend uses the space's enabled provider. The app holds no payment state. One instance per app (an AsyncLocalInstance singleton).
  sealed class PaymentsService : AsyncLocalInstance<PaymentsService>
    ctor()
    // Default cancel URL used when a command does not specify one.
    string? DefaultCancelUrl { get; set; }
    // Optional provider to use when a command does not specify one. Left null by default: the SDK then sends no provider and the backend charges with the space's enabled (default) provider. Set this only to pin a specific provider for an app that has more than one enabled.
    PaymentProvider? DefaultProvider { get; set; }
    // Default success URL used when a command does not specify one.
    string? DefaultSuccessUrl { get; set; }
    // Cancel a subscription at the period end (default) or right away with immediate . The entitlement lapses when the cancellation takes effect.
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Create (or update) an offer in the app's catalog so customers can pay for it by id. For Stripe this provisions a Product + Price; for providers without a catalog (Mollie, Surfboard) the offer is stored by the platform. Idempotent on OfferId .
    Task<PaymentOffer> CreateOfferAsync(OfferSpec offer, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Create a provider-hosted payment link for an offer. Recurring offers start a subscription; paying grants an entitlement. customerKey defaults to the current user.
    Task<PaymentLink> CreatePaymentLinkAsync(string offerId, string? customerKey = null, string? email = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Create a provider-hosted payment link for an ad-hoc amount (tip, one-off charge). Grants no entitlement — use an offer for that. customerKey defaults to the current user.
    Task<PaymentLink> CreatePaymentLinkAsync(long amountMinor, string currency, string? customerKey = null, string? description = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // The customer's access to an offer (a backend call). Used by the [PaymentsRequireEntitlement] policy. customerKey defaults to the current user. For gating UI, prefer the synchronous IsEntitled .
    Task<PaymentEntitlement> GetEntitlementAsync(string offerId, string? customerKey = null, CancellationToken cancellationToken = default)
    // Synchronous, cache-backed access check for gating UI — no backend call, safe to read every render. Reading it inside a UI lambda re-renders when the entitlement changes (after a purchase or a pushed event). customerKey defaults to the current user. The first read for an unseen offer returns false and warms the cache in the background, flipping to the real value on the next render.
    bool IsEntitled(string offerId, string? customerKey = null)
    // The app's catalog of purchasable offers.
    Task<IReadOnlyList<PaymentOffer>> ListOffersAsync(CancellationToken cancellationToken = default)
    // The customer's payments. customerKey defaults to the current user.
    Task<IReadOnlyList<Payment>> ListPaymentsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // The customer's subscriptions. customerKey defaults to the current user.
    Task<IReadOnlyList<PaymentSubscription>> ListSubscriptionsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // Ask the backend to re-pull live provider state — the recovery path when a provider webhook was missed or the app was offline when an event was pushed. Eventually consistent: the pulled objects flow through the normal pipeline and surface as ordinary PaymentEventReceived pushes and entitlement refreshes within seconds. With a reference (a payment link's checkout-session reference or a subscription id) only that object is pulled; otherwise the customer's recent objects; with neither and no current user in scope, the space's recent window.
    Task<PaymentReconcileResult> ReconcileAsync(string? customerKey = null, string? reference = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Refund a payment, in full by default or partially via amountMinor . Refunding does not revoke an entitlement the payment granted.
    Task<PaymentRefund> RefundAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Remove an offer from the app's catalog (Stripe archives the Product/Price). Returns false if no such active offer existed.
    Task<bool> RemoveOfferAsync(string offerId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Fetch a receipt for a completed payment. Url is a provider-hosted receipt page (present for Stripe and Surfboard). Pdf carries downloadable PDF bytes only when the provider offers one; today both providers return a hosted URL only, so it is null.
    Task<PaymentReceipt> RequestReceiptAsync(string paymentId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Raised for each normalized payment event the backend pushes (paid, refunded, subscription renewed/canceled). Subscribing registers the receiver on first use.
    event Func<PaymentEvent, Task>? PaymentEventReceived
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
  // The state of a PaymentRefund .
  enum RefundStatus
    Unknown
    Pending
    Succeeded
    Failed
  // The lifecycle state of a PaymentSubscription .
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

# Ikon.App.Extra Public API

namespace Ikon.App.Connectors
  sealed class EmailSummary : IEquatable<EmailSummary>
    ctor(string Id, string ThreadId, string From, string Subject, string Snippet, DateTimeOffset ReceivedAt)
    string From { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Snippet { get; init; }
    string Subject { get; init; }
    string ThreadId { get; init; }
  // Gmail connector. Send and list mail with Google OAuth2 credentials (refresh token). Raw — the agent skill lives in Ikon.Agent.Connectors.
  sealed class Gmail
    ctor(GoogleCredentials credentials)
    // Fetch the full plain-text body of a message. Returns the text/plain part when present, falling back to the text extracted from the HTML part, then to an empty string.
    Task<string> GetBodyAsync(string id, CancellationToken ct = default)
    // Stream every message matching the query, paging through the whole result set. Use a query with date operators (e.g. "after:2024/01/01") to bound a historical backfill by time.
    IAsyncEnumerable<EmailSummary> ListAllAsync(string? query = null, CancellationToken ct = default)
    Task<IReadOnlyList<EmailSummary>> ListAsync(string? query = null, int limit = 20, CancellationToken ct = default)
    Task<string> SendAsync(string to, string subject, string body, string? cc = null, CancellationToken ct = default)

namespace Ikon.App.Connectors.Browser
  // A long-lived Playwright page driven across many turns. Owns the browser lifecycle; resolves a WebTarget by mark, then accessibility role+name, then selector. Raw — no agent logic; the agent layer (Ikon.Agent.Browser) exposes these actions as tools.
  sealed class BrowserSession : IAsyncDisposable
    ctor()
    // The last ~40 console messages / page errors / failed requests from the page — the page's own account of why it is in whatever state it is in. Diagnostic gold when a page that "should" render stays blank (auth failures, websocket errors, bundle errors).
    IReadOnlyList<string> ConsoleTail { get; }
    string CurrentUrl { get; }
    ValueTask DisposeAsync()
    // Evaluate a JavaScript function-expression (e.g. "() => { ...; return 'x'; }") on the current page and return its string result. For light page-state manipulation by non-agentic callers — e.g. the codegen visual gate flipping data-theme so it can screenshot both theme states of the same view.
    Task<string?> EvaluateAsync(string script)
    Task<(bool Ok, string Selector, string? Extracted, string? Failure)> ExecuteAsync(WebAction action)
    Task<IReadOnlyList<MarkedElement>> MarkElementsAsync()
    Task NavigateAsync(string url)
    Task<byte[]> ScreenshotAsync()
    // Screenshot as JPEG at the given quality — for callers that put the image into an LLM context, where a PNG's 3-5x larger payload rides along for every later turn.
    Task<byte[]> ScreenshotJpegAsync(int quality = 70)
    Task StartAsync(bool headless, bool captureGrade = false, CancellationToken ct = default)
  sealed class WebAction.Click : WebAction, IEquatable<WebAction.Click>
    ctor(WebTarget Target)
    WebTarget Target { get; init; }
  sealed class WebAction.Extract : WebAction, IEquatable<WebAction.Extract>
    ctor(WebTarget Target, string OutputName)
    string OutputName { get; init; }
    WebTarget Target { get; init; }
  sealed class WebAction.Fill : WebAction, IEquatable<WebAction.Fill>
    ctor(WebTarget Target, string Text, bool Secret = false, string? InputName = null)
    string? InputName { get; init; }
    bool Secret { get; init; }
    WebTarget Target { get; init; }
    string Text { get; init; }
  // An interactable element discovered on the page, tagged for this observation.
  sealed class MarkedElement : IEquatable<MarkedElement>
    ctor(int Mark, string Role, string Name, string Selector)
    int Mark { get; init; }
    string Name { get; init; }
    string Role { get; init; }
    string Selector { get; init; }
  sealed class WebAction.Navigate : WebAction, IEquatable<WebAction.Navigate>
    ctor(string Url)
    string Url { get; init; }
  sealed class WebAction.Press : WebAction, IEquatable<WebAction.Press>
    ctor(string Key)
    string Key { get; init; }
  sealed class WebAction.Scroll : WebAction, IEquatable<WebAction.Scroll>
    ctor(int Dx, int Dy)
    int Dx { get; init; }
    int Dy { get; init; }
  // A single browser action. A tagged union so a flow serializes losslessly and replays exactly.
  abstract class WebAction : IEquatable<WebAction>
  // A distilled, replayable integration: ordered steps with parameterized input slots.
  sealed class WebFlow : IEquatable<WebFlow>
    ctor(string Name, string Origin, IReadOnlyList<WebStep> Steps, IReadOnlyList<string> Inputs)
    IReadOnlyList<string> Inputs { get; init; }
    string Name { get; init; }
    string Origin { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
  // Turns a successful WebRun into a replayable WebFlow : keeps the steps that worked and parameterizes each filled field into a named input slot. Pure and deterministic.
  static class WebFlowDistiller
    static WebFlow Distill(WebRun run, string? name = null)
  // Deterministically replays a distilled WebFlow on a browser session — no LLM — substituting input slots with supplied values.
  static class WebFlowPlayer
    static Task<WebReplay> ReplayAsync(BrowserSession session, WebFlow flow, IReadOnlyDictionary<string, string> inputs, CancellationToken ct = default)
  enum WebOutcome
    Succeeded
    Failed
    BudgetExhausted
  // The result of replaying a WebFlow .
  sealed class WebReplay : IEquatable<WebReplay>
    ctor(bool Ok, IReadOnlyDictionary<string, string> Outputs, bool Healed)
    bool Healed { get; init; }
    bool Ok { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
  // The result of an operate run: outcome, summary, the action trace, and any extracted outputs. Looks counts visual inspections separately — they consume agent budget without appearing in the action trace, so budget analysis needs both numbers.
  sealed class WebRun : IEquatable<WebRun>
    ctor(WebOutcome Outcome, string Summary, IReadOnlyList<WebStep> Steps, IReadOnlyDictionary<string, string> Outputs, int Looks = 0)
    int Looks { get; init; }
    WebOutcome Outcome { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
    string Summary { get; init; }
  // One executed action, the selector that actually resolved it, and whether it succeeded.
  sealed class WebStep : IEquatable<WebStep>
    ctor(WebAction Action, string ResolvedSelector, bool Ok)
    WebAction Action { get; init; }
    bool Ok { get; init; }
    string ResolvedSelector { get; init; }
  // How to locate an element. Prefer accessibility role + name; fall back to a CSS/XPath selector or a perception mark id from the current observation.
  sealed class WebTarget : IEquatable<WebTarget>
    ctor(string? Role = null, string? Name = null, string? Selector = null, int? Mark = null)
    int? Mark { get; init; }
    string? Name { get; init; }
    string? Role { get; init; }
    string? Selector { get; init; }

namespace Ikon.App.Connectors.Telephony
  // Raw call tuning: the TTS voice, spoken language, and a hard duration cap. Model/agent choices live in the agent layer (Ikon.Agent.Telephony), not here.
  sealed class CallOptions : IEquatable<CallOptions>
    ctor(string VoiceId = "", string Language = "en-US", TimeSpan? MaxDuration = null)
    string Language { get; init; }
    TimeSpan? MaxDuration { get; init; }
    string VoiceId { get; init; }
  enum CallOutcome
    Completed
    NoAnswer
    Busy
    Failed
  sealed class CallResult : IEquatable<CallResult>
    ctor(string Transcript, CallOutcome Outcome, TimeSpan Duration)
    TimeSpan Duration { get; init; }
    CallOutcome Outcome { get; init; }
    string Transcript { get; init; }
  // A completed caller utterance: its transcript plus the raw mu-law audio.
  sealed class CallTurn : IEquatable<CallTurn>
    ctor(string Transcript, byte[] AudioMuLaw)
    byte[] AudioMuLaw { get; init; }
    string Transcript { get; init; }
  // G.711 mu-law codec for telephony audio (8-bit, 8kHz), the encoding Twilio Media Streams uses on the wire. Converts between mu-law bytes and normalized float samples.
  static class MuLawCodec
    // Decodes mu-law bytes to float samples normalized to [-1.0, 1.0].
    static float[] Decode(ReadOnlySpan<byte> muLaw)
    // Encodes float samples (normalized to [-1.0, 1.0]) to mu-law bytes.
    static byte[] Encode(ReadOnlySpan<float> samples)
  // A live phone call — the real-time audio engine. Segments caller speech into turns ( Turns ), speaks replies ( SpeakAsync ), and hangs up. No agent logic: the brain is supplied by the consumer (Ikon.Agent.Connectors.Telephony binds a call to a subthread). Supports barge-in: sustained caller speech during a reply cancels TTS and flushes Twilio's buffer. Speech detection uses Silero VAD (falls back to an RMS gate if the model can't load).
  sealed class PhoneCall : IAsyncDisposable
    TimeSpan Duration { get; }
    CallOutcome Outcome { get; }
    ValueTask DisposeAsync()
    Task HangupAsync()
    // Speak a reply to the caller (TTS → 8kHz mu-law → Media Streams). Interruptible by barge-in; returns true if the caller barged in (so the consumer can stop voicing the rest of the reply).
    Task<bool> SpeakAsync(string text, CancellationToken ct = default)
    // Caller utterances as they complete, until the call ends.
    IAsyncEnumerable<CallTurn> Turns(CancellationToken ct = default)
  // Places outbound Twilio calls and hosts the Media Streams WebSocket. Each placed call yields a live PhoneCall once the audio stream connects. Raw — no agent logic; credentials come from app.Secrets.
  sealed class Telephone : IAsyncDisposable
    ctor(IAppBase app, TwilioCredentials credentials, CallOptions? options = null)
    // Place a call to an E.164 number; resolves to the live call once audio connects.
    Task<PhoneCall> CallAsync(string number, CancellationToken ct = default)
    ValueTask DisposeAsync()
  // Twilio credentials. Supplied from app.Secrets at construction; never hardcoded.
  sealed class TwilioCredentials : IEquatable<TwilioCredentials>
    ctor(string AccountSid, string AuthToken, string FromNumber)
    string AccountSid { get; init; }
    string AuthToken { get; init; }
    string FromNumber { get; init; }

# Ikon.Resonance Public API

namespace Ikon.Resonance
  // Audio frame with samples, stream identity, and optional encoding options, analysis results, and target information.
  struct AudioFrame
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, IReadOnlyList<AudioAnalysisResult>? analysisResults = null, IReadOnlyList<AudioShapeSetDeclaration>? shapeSetDeclarations = null)
    IReadOnlyList<AudioAnalysisResult>? AnalysisResults { get; }
    int ChannelCount { get; }
    AudioEncoderOptions? EncoderOptions { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    int SampleRate { get; }
    ReadOnlyMemory<float> Samples { get; }
    IReadOnlyList<AudioShapeSetDeclaration>? ShapeSetDeclarations { get; }
    string StreamId { get; }
    IReadOnlyList<int>? TargetIds { get; }
    TimeSpan TotalDuration { get; }
  // Tracks audio stream metrics including packet counts, inter-packet delays, jitter, and encoding times. Supports tracking metrics across multiple streams.
  class AudioMetrics
    ctor()
    double AvgEncodeTimeMs { get; }
    double AvgIpdMs { get; }
    double CpuUsagePercent { get; }
    bool Enabled { get; set; }
    double JitterMs { get; }
    bool LogMetrics { get; set; }
    double MaxIpdMs { get; }
    double MinIpdMs { get; }
    int StreamCount { get; }
    double UpdateIntervalSeconds { get; set; }
    void RecordPacket(string streamId, double encodingTimeMs)
    void Remove(string streamId)
    void Reset(string streamId)
    void ResetAll()
    event Action? Updated
  // Provides methods for resampling audio between different sample rates and channel configurations. Supports mono and stereo audio using linear interpolation for sample rate conversion.
  static class AudioResampler
    // Calculates the number of output frames after resampling.
    static int CalculateResampledFrameCount(int inputFrameCount, int inputSampleRate, int outputSampleRate)
    // Converts audio between mono and stereo channel configurations. Stereo to mono averages both channels; mono to stereo duplicates the channel.
    static void ConvertChannels(ReadOnlySpan<float> source, Span<float> destination, int inputChannelCount, int outputChannelCount)
    // Determines whether the specified channel count is supported.
    static bool IsSupportedChannelCount(int channelCount)
    // Resamples audio from one sample rate and channel configuration to another using linear interpolation.
    static void Resample(ReadOnlySpan<float> source, Span<float> destination, int inputSampleRate, int outputSampleRate, int inputChannelCount, int outputChannelCount)
    // The maximum number of audio channels supported (mono or stereo).
    static int MaxSupportedChannelCount
  // Provides utility methods for measuring audio levels and converting audio samples between PCM 16-bit integer and 32-bit float formats.
  static class AudioUtils
    // Converts 32-bit float samples to 16-bit PCM samples as raw bytes (little-endian). Float values are clamped to [-1.0, 1.0] before conversion.
    static int ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input, Span<byte> output)
    // Converts 32-bit float samples to 16-bit PCM samples as raw bytes (little-endian). Float values are clamped to [-1.0, 1.0] before conversion.
    static byte[] ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input)
    // Converts 32-bit float samples to 16-bit PCM samples. Float values are clamped to [-1.0, 1.0] before conversion.
    static int ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input, Span<short> output)
    // Converts 32-bit float samples to 16-bit PCM samples. Float values are clamped to [-1.0, 1.0] before conversion.
    static short[] ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input)
    // Converts 16-bit PCM samples to 32-bit float samples normalized to the range [-1.0, 1.0].
    static int ConvertPcm16ToFloat(ReadOnlySpan<short> input, Span<float> output)
    // Converts 16-bit PCM samples to 32-bit float samples normalized to the range [-1.0, 1.0].
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<short> input)
    // Converts 16-bit PCM samples (as raw bytes) to 32-bit float samples normalized to the range [-1.0, 1.0].
    static int ConvertPcm16ToFloat(ReadOnlySpan<byte> input, Span<float> output)
    // Converts 16-bit PCM samples (as raw bytes) to 32-bit float samples normalized to the range [-1.0, 1.0].
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<byte> input)
    // Computes the root mean square (RMS) level of the samples. For normalized float audio in [-1.0, 1.0] the result is in [0.0, 1.0] and is the standard measure of perceived loudness (e.g. for silence detection thresholds).
    static float Rms(ReadOnlySpan<float> samples)
  // Crossfade curve type.
  enum CrossfadeCurve
    Linear
    EqualPower
  // Fade transition mode when new speech interrupts current speech.
  enum FadeMode
    Sequential
    Crossfade
  // Server-side audio mixer for group voice scenarios (meetings, conferences, multiplayer). Mixes multiple participant audio streams together, producing a personalized output stream for each participant that contains all other participants' audio mixed together but excludes the participant's own audio. Each input stream is tagged with an excludeKey (typically a participant/session ID) to control the exclusion. Participants must be registered with AddParticipant before they can receive mixed output. Streams are added/removed independently via AddStream and RemoveStream . A participant continues to receive output (from other participants' streams) even when they have no active streams of their own. Uses power-preserving normalization (1/sqrt(N)) and tanh soft-clipping to prevent distortion when many participants speak simultaneously.
  sealed class GroupAudioMixer : IAsyncDisposable
    ctor(GroupAudioMixerConfig? config = null)
    // Registers a participant to receive personalized mixed audio output. The participant will receive a mix of all streams except those tagged with their excludeKey.
    void AddParticipant(string excludeKey)
    // Registers an input audio stream and tags it with excludeKey so the owning participant never hears their own audio. Adding an already-registered stream id is a no-op.
    void AddStream(string streamId, string excludeKey)
    ValueTask DisposeAsync()
    // Unregisters a participant. They will no longer receive mixed audio output.
    void RemoveParticipant(string excludeKey)
    // Unregisters an input stream and discards any samples still buffered for it. Removing an unknown stream id is a no-op.
    void RemoveStream(string streamId)
    // Starts the output loop that paces personalized mixes into onFrame as 20 ms frames (called once per registered participant per tick, with the participant's excludeKey as the first argument). May be called only once per mixer instance; a second call throws so a silently dropped onFrame can never go unnoticed. Buffer-reuse contract: the frames passed to onFrame alias a single reused sample buffer — consume the samples fully before returning from the callback and copy them if you need to store them beyond the call.
    Task StartAsync(Func<string, AudioFrame, ValueTask> onFrame, CancellationToken cancellationToken = default)
    // Buffers interleaved samples for a registered input stream, resampling to the mixer's native 48 kHz stereo format when needed. When the stream's buffer is full the oldest samples are dropped to make room; writes to an unknown stream are dropped with a throttled warning (stream teardown races with in-flight frames, so this is not an error).
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  // Configuration for the GroupAudioMixer . Immutable — the mixer captures the values at construction, so construct a new config (and mixer) instead of mutating a shared instance.
  sealed class GroupAudioMixerConfig : IEquatable<GroupAudioMixerConfig>
    ctor()
    // Maximum buffer size per stream in milliseconds.
    double MaxBufferSizeMs { get; init; }
  // Represents a source that generates audio frames.
  interface IAudioSource
    // Generates a frame of audio into the provided buffer.
    abstract void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
  // Specifies the sample format used in the WAV file.
  enum WavFile.SampleFormat
    Short
    Float
  // Filters silence from an audio chunk stream so that only speech reaches downstream consumers such as speech-to-text models (which tend to hallucinate on silent input). Uses asymmetric EMA for level tracking, an adaptive noise floor, and a circular pre-buffer to ensure speech onsets are never clipped. Designed for real-time audio at typical frame sizes (20 ms). Usage — push-based: call ProcessChunk per audio chunk, forward non-null results. Usage — stream-based: wrap an IAsyncEnumerable source with FilterAsync .
  sealed class SilenceRemover
    // Creates a new SilenceRemover for the given audio format.
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    // Wraps an async audio source, yielding only chunks that contain speech. Silence is suppressed and speech onsets include look-back audio from the pre-buffer.
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = default)
    // Processes a single audio chunk and determines whether it should be forwarded downstream. Returns the samples to forward (including pre-buffered onset audio when speech begins), or null if the chunk is silence that should be suppressed.
    float[]? ProcessChunk(ReadOnlySpan<float> chunk)
    // Resets all internal state (EMA level, noise floor, pre-buffer, and state machine) to initial values. Call this when starting a new audio session on the same instance.
    void Reset()
  // Configuration for SilenceRemover . The silence remover uses asymmetric EMA (exponential moving average) to track audio level, an adaptive noise floor that adjusts to the environment, and a circular pre-buffer that preserves the onset of speech so words are never clipped. The speech threshold is computed as: noiseFloor * NoiseFloorMultiplier + NoiseFloorOffset . Immutable — the remover captures the values at construction, so construct a new config (and remover) instead of mutating a shared instance.
  sealed class SilenceRemoverConfig : IEquatable<SilenceRemoverConfig>
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
    // Feeds a chunk of speech audio into the mixer, resampling to 48 kHz stereo when needed. The chunk's id identifies the speech event: chunks with the current event's id append to it, while a new id interrupts the current event with the configured fade transition. Effects, analyzers, and target ids are captured from the event's first chunk.
    void AddSamples(AudioChunk chunk, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Immediately discards all speech state — current, pending, and paused — without fading. Use for hard resets (e.g. conversation restart); prefer FadeOut for a graceful stop.
    void Clear()
    ValueTask DisposeAsync()
    // Starts fading out the current speech event over the configured fade-out duration. The event completes when the fade reaches silence. No-op when nothing is playing or a fade-out is already in progress.
    void FadeOut()
    // Pauses output by fading the current speech out, then holding it (buffered samples are kept) until Resume . No-op when already paused or pausing.
    void Pause()
    // Resumes paused output, fading the held speech event back in from where it stopped. No-op when not paused.
    void Resume()
    // Starts the output loop that paces mixed audio into onFrame as 20 ms frames. May be called only once per mixer instance; a second call throws so a silently dropped onFrame can never go unnoticed. Buffer-reuse contract: the frames passed to onFrame alias a single reused sample buffer — consume the samples fully before returning from the callback and copy them if you need to store them beyond the call.
    Task StartAsync(Func<AudioFrame, ValueTask> onFrame, CancellationToken cancellationToken = default)
  // Configuration options for the SpeechMixer. Immutable — the mixer captures the values at construction, so construct a new config (and mixer) instead of mutating a shared instance.
  sealed class SpeechMixerConfig : IEquatable<SpeechMixerConfig>
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
    // Maximum buffer size in milliseconds for incoming speech samples. This is an upper bound only; the queue grows from a small initial size on demand. Keep this generous enough to absorb production-faster-than-playback bursts (typical for non-streaming TTS) but tight enough that a runaway producer can't consume excessive memory. Samples added beyond this bound are dropped (with a throttled warning) rather than throwing; the backing buffer is released once the event drains, so this only caps the transient in-flight footprint.
    double MaxBufferSizeMs { get; init; }
    // Maximum padding duration in milliseconds for effect tails. Prevents infinite padding if effects never fully decay.
    double MaxPaddingTimeMs { get; init; }
    // RMS threshold below which effect tail padding stops. Default is 0.001 (~-60dB), meaning padding continues until output is essentially silent.
    double PaddingThreshold { get; init; }
  // Creates WAV audio files in memory with support for 16-bit integer or 32-bit float sample formats. Samples are written incrementally and the WAV header is finalized when the file is accessed.
  class WavFile : IDisposable
    // Initializes a new WAV file builder with the specified audio parameters.
    ctor(int sampleRate, int channelCount, WavFile.SampleFormat sampleFormat)
    // Adds 16-bit integer audio samples to the WAV file.
    void AddSamples(ReadOnlySpan<short> samples)
    // Adds 32-bit float audio samples to the WAV file.
    void AddSamples(ReadOnlySpan<float> samples)
    // Gets the WAV file as a byte array. Finalizes the WAV header if not already done.
    byte[] AsArray()
    // Gets the WAV file as a readable stream. Finalizes the WAV header if not already done.
    Stream AsStream()
    // Releases the resources used by the WAV file builder.
    void Dispose()
    // Saves the WAV file to disk. Finalizes the WAV header if not already done.
    void SaveToFile(string filePath)

namespace Ikon.Resonance.Analysis
  // Result of audio analysis containing shape set values.
  struct AudioAnalysisResult
    ctor(uint setId, IReadOnlyList<float> values)
    // The shape set ID this result belongs to.
    uint SetId { get; }
    // The analysis values for this shape set. Analyzers may reuse the backing storage between frames — copy the values if you need them beyond the current frame.
    IReadOnlyList<float> Values { get; }
  // Declaration of a shape set with ID and shape names.
  struct AudioShapeSetDeclaration
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
    abstract IAudioAnalyzerInstance Create(int sampleRate, int channelCount)
  // Stateful audio analyzer that extracts data from audio buffers without modifying them.
  interface IAudioAnalyzerInstance
    // Analyzes the provided buffer and returns shape set values. The buffer is not modified.
    abstract AudioAnalysisResult Analyze(ReadOnlySpan<float> buffer)
    // Resets the analyzer internal state back to its initial values.
    abstract void Reset()
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
    ctor(float baseDelayMs, float depthMs, float rateHz, float mix, float stereoPhaseOffsetDegrees = 90)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Feedback delay that adds spacious echoes with gentle high-frequency damping.
  sealed class DelayAudioEffect : IAudioEffect
    ctor()
    ctor(float delayMs, float feedback, float mix, float feedbackDamping = 0.25)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Stateless definition of an audio effect that can create mixer-ready instances.
  interface IAudioEffect
    // Creates a stateful effect instance bound to the mixer's output format.
    abstract IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Stateful audio effect that can mutate audio buffers in place.
  interface IAudioEffectInstance
    // Processes the provided buffer in place.
    abstract void Process(Span<float> buffer)
    // Resets the effect internal state back to its initial values.
    abstract void Reset()
  // Factory for creating reverb effects with configurable delay lines, feedback, mix, and damping.
  sealed class ReverbAudioEffect : IAudioEffect
    // Creates a reverb with default room parameters (small room).
    ctor()
    // Creates a reverb with simplified parameters for easy room modeling.
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
    ctor(float rateHz, float depth, float mix, float stereoPhaseOffsetDegrees = 90)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
