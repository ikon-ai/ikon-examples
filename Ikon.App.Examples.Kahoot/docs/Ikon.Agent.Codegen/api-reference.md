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
  sealed class DebateThenJudgeOptions<T> : EmergeScope<T>
    ctor()
    int DebateRounds { get; set; }
    Action<AgentScope<T>>? DebaterConfig { get; set; }
    int Debaters { get; set; }
    EmergeScope<T> JudgeScope { get; }
    void Debater(Action<AgentScope<T>> configure)
    void Judge(Action<EmergeScope<T>> configure)
  static class Emerge
    // One-shot LLM completion that returns the result string. The verbose form var (reply, _) = await Emerge.Run<string>( LLMModel.Claude45Haiku, new KernelContext(), pass => pass.Command = command).FinalAsync(ct); becomes var reply = await Emerge.AskAsync(command, ct); Uses Claude45Haiku by default — cheap+fast, the right choice for short transformations (chatbot replies, reformat-as-X, classify, summarize). Override the model via the other overload when the task warrants a stronger tier. Reach for the full Run``1 when you need tools, multi-iteration agentic loops, a populated KernelContext , or fine pass tuning.
    static Task<string> AskAsync(string command, CancellationToken ct = null)
    // Like AskAsync but with an explicit model override.
    static Task<string> AskAsync(string command, LLMModel model, CancellationToken ct = null)
    // One-shot LLM completion that returns the result string. The verbose form var (reply, _) = await Emerge.Run<string>( LLMModel.Claude45Haiku, new KernelContext(), pass => pass.Command = command).FinalAsync(ct); becomes var reply = await Emerge.AskAsync(command, ct); Uses Claude45Haiku by default — cheap+fast, the right choice for short transformations (chatbot replies, reformat-as-X, classify, summarize). Override the model via the other overload when the task warrants a stronger tier. Reach for the full Run``1 when you need tools, multi-iteration agentic loops, a populated KernelContext , or fine pass tuning.
    static Task<T> AskAsync<T>(string command, CancellationToken ct = null) where T : class
    // Like AskAsync but with an explicit model override.
    static Task<T> AskAsync<T>(string command, LLMModel model, CancellationToken ct = null) where T : class
    static IAsyncEnumerable<EmergeEvent<T>> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> DebateThenJudge<T>(LLMModel model, KernelContext context, Action<DebateThenJudgeOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> DebateThenJudge<T>(LLMModel model, KernelContext context, Action<DebateThenJudgeOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<TResult>> MapReduce<TChunk, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TChunk, TResult>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<TResult>> MapReduce<TChunk, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TChunk, TResult>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> ParallelBestOf<T>(LLMModel model, KernelContext context, Action<ParallelBestOfOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> ParallelBestOf<T>(LLMModel model, KernelContext context, Action<ParallelBestOfOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> PlanAndExecute<T>(LLMModel model, KernelContext context, Action<PlanAndExecuteOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> PlanAndExecute<T>(LLMModel model, KernelContext context, Action<PlanAndExecuteOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Router<T>(LLMModel model, KernelContext context, Action<RouterOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Router<T>(LLMModel model, KernelContext context, Action<RouterOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> SelfConsistency<T>(LLMModel model, KernelContext context, Action<SelfConsistencyOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> SelfConsistency<T>(LLMModel model, KernelContext context, Action<SelfConsistencyOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> SolverCriticVerifier<T>(LLMModel model, KernelContext context, Action<SolverCriticVerifierOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> SolverCriticVerifier<T>(LLMModel model, KernelContext context, Action<SolverCriticVerifierOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Swarm<T>(LLMModel model, KernelContext context, Action<SwarmOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Swarm<T>(LLMModel model, KernelContext context, Action<SwarmOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TaskGraph<T>(LLMModel model, KernelContext context, Action<TaskGraphOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TaskGraph<T>(LLMModel model, KernelContext context, Action<TaskGraphOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TestRefine<T>(LLMModel model, KernelContext context, Action<TestRefineOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TestRefine<T>(LLMModel model, KernelContext context, Action<TestRefineOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TreeOfThought<T>(LLMModel model, KernelContext context, Action<TreeOfThoughtOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TreeOfThought<T>(LLMModel model, KernelContext context, Action<TreeOfThoughtOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, ILLM llm, CancellationToken ct = null)
  // Marker type for chat-mode Emerge.Run where no structured output is needed. Replaces app-specific empty classes like NanobotResponse, AgentLoopResponse, etc.
  sealed class EmergeChat
    ctor()
  sealed class EmergeEventCallbacks<T>
    ctor()
    Action<T, EmergenceTrace>? OnCompleted { get; set; }
    Action<string?>? OnStopped { get; set; }
    Action<string>? OnText { get; set; }
    Action<FunctionCall>? OnToolCallPlanned { get; set; }
    Action<FunctionCall, object>? OnToolCallResult { get; set; }
  static class EmergeEventExtensions
    static IAsyncEnumerable<RunnerEvent> AsRunnerEvents<T>(IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = null)
    static Task<string> DispatchEventsAsync<T>(IAsyncEnumerable<EmergeEvent<T>> events, EmergeEventCallbacks<T> callbacks, CancellationToken ct = null)
    static Task<ValueTuple<T, KernelContext>> FinalAsync<T>(IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = null)
    static Task<ValueTuple<T, KernelContext, EmergenceTrace>> FinalWithTraceAsync<T>(IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = null)
  abstract class EmergeEvent<T> : IEquatable<EmergeEvent<T>>
  static class EmergePassExtensions
    static EmergePass<T> AddMcpTools<T>(EmergePass<T> pass, McpClient mcpClient)
    static EmergePass<T> AddTool<T>(EmergePass<T> pass, Function function)
    static EmergePass<T> AddTool<T, TResult>(EmergePass<T> pass, string name, string description, Func<TResult> function)
    static EmergePass<T> AddTool<T, T1, TResult>(EmergePass<T> pass, string name, string description, Func<T1, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, T7, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
    static EmergePass<T> AddTool<T, TResult>(EmergePass<T> pass, string name, string description, Func<Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, TResult>(EmergePass<T> pass, string name, string description, Func<T1, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, T7, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> function)
    static EmergePass<T> AddTools<T>(EmergePass<T> pass, params Function[] functions)
    static EmergePass<T> AddToolsFrom<T>(EmergePass<T> pass, object instance)
    static EmergePass<T> DescribeParams<T>(EmergePass<T> pass, string toolName, Dictionary<string, string> paramDescriptions)
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
  sealed class EmergeScope : EmergeScopeBase
    ctor()
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
  struct EmergenceBudget : IEquatable<EmergenceBudget>
    ctor()
    ctor(int maxIterations, int maxToolCalls, TimeSpan maxWallTime)
    static EmergenceBudget Default { get; }
    int MaxIterations { get; init; }
    int MaxToolCalls { get; init; }
    TimeSpan MaxWallTime { get; init; }
    static EmergenceBudget Unlimited { get; }
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
  static class EmergenceMonitor
    static bool HasObservers { get; }
    static void AddObserver(IEmergenceObserver observer)
    static void ClearObservers()
    static void RemoveObserver(IEmergenceObserver observer)
    static void SetSoleObserver(IEmergenceObserver observer)
    // Sets tags that will be attached to any EmergenceCallInfo created within this async scope. Returns a disposable that clears the tags when disposed.
    static IDisposable WithTags(Dictionary<string, string> tags)
  class EmergenceMonitorState : IEmergenceObserver
    ctor()
    IReadOnlyList<EmergenceCallInfo> Calls { get; }
    void Clear()
    void OnCallCompleted(EmergenceCallInfo call)
    void OnCallStarted(EmergenceCallInfo call)
    void OnEvent(EmergenceCallInfo call, EmergenceObserverEvent evt)
    event Action? Changed
  abstract class EmergenceObserverEvent : IEquatable<EmergenceObserverEvent>
  enum EmergenceStatus
    Completed
    Stopped
    Failed
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
  sealed class ExecutionPlan
    ctor()
    List<PlanStep> Steps { get; set; }
    string? Summary { get; set; }
  class FoundSection
    ctor()
    string Content { get; set; }
    string NodeId { get; set; }
    int? Page { get; set; }
    string Path { get; set; }
    string Relevance { get; set; }
  interface IEmergenceObserver
    abstract void OnCallCompleted(EmergenceCallInfo call)
    abstract void OnCallStarted(EmergenceCallInfo call)
    abstract void OnEvent(EmergenceCallInfo call, EmergenceObserverEvent evt)
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
    Task<string> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = null)
    // Calls an MCP tool and returns both content and pagination cursor. Pass a cursor from a previous response to fetch the next page.
    Task<McpToolResult> CallToolRawAsync(string name, JsonElement arguments, CancellationToken ct = null, string? cursor = null)
    // Initializes the MCP session and discovers available tools.
    Task ConnectAsync(CancellationToken ct = null)
    void Dispose()
    // Converts discovered MCP tools into Ikon Function objects that can be added to an EmergePass.
    Function[] ToFunctions()
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
  sealed class ObserverCompletedEvent : EmergenceObserverEvent, IEquatable<ObserverCompletedEvent>
    ctor(EmergenceTrace Trace)
    EmergenceTrace Trace { get; init; }
  sealed class ObserverProgressEvent : EmergenceObserverEvent, IEquatable<ObserverProgressEvent>
    ctor(string Message)
    string Message { get; init; }
  sealed class ObserverRetryEvent : EmergenceObserverEvent, IEquatable<ObserverRetryEvent>
    ctor(string Reason, int Attempt, int MaxAttempts)
    int Attempt { get; init; }
    int MaxAttempts { get; init; }
    string Reason { get; init; }
  sealed class ObserverStageEvent : EmergenceObserverEvent, IEquatable<ObserverStageEvent>
    ctor(string Name)
    string Name { get; init; }
  sealed class ObserverStoppedEvent : EmergenceObserverEvent, IEquatable<ObserverStoppedEvent>
    ctor(string? Reason)
    string? Reason { get; init; }
  sealed class ObserverTextEvent : EmergenceObserverEvent, IEquatable<ObserverTextEvent>
    ctor(string Text)
    string Text { get; init; }
  sealed class ObserverTokenEvent : EmergenceObserverEvent, IEquatable<ObserverTokenEvent>
    ctor(long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens, int ContextWindowSize, double ContextWindowUtilization)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    int ContextWindowSize { get; init; }
    double ContextWindowUtilization { get; init; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
  sealed class ObserverToolCallPlannedEvent : EmergenceObserverEvent, IEquatable<ObserverToolCallPlannedEvent>
    ctor(string FunctionName, string ParametersJson)
    string FunctionName { get; init; }
    string ParametersJson { get; init; }
  sealed class ObserverToolCallResultEvent : EmergenceObserverEvent, IEquatable<ObserverToolCallResultEvent>
    ctor(string FunctionName, string ResultSummary)
    string FunctionName { get; init; }
    string ResultSummary { get; init; }
  sealed class ParallelBestOfOptions<T> : EmergeScope<T>
    ctor()
    Func<T, ScoreBreakdown?, string>? BuildCriticFeedback { get; set; }
    Action<CandidateScope<T>>? CandidateConfig { get; set; }
    int Count { get; set; }
    bool CriticMustImprove { get; set; }
    EmergeScope<T> CriticScope { get; }
    bool EnableCritic { get; set; }
    int MaxParallel { get; set; }
    Func<T, EmergenceTrace, double>? Score { get; set; }
    void Candidate(Action<CandidateScope<T>> configure)
    void Critic(Action<EmergeScope<T>> configure)
  sealed class PlanAndExecuteOptions<T> : EmergeScope<T>
    ctor()
    EmergeScope<T> ExecutorScope { get; }
    int MaxSteps { get; set; }
    EmergeScope<ExecutionPlan> PlannerScope { get; }
    void Executor(Action<EmergeScope<T>> configure)
    void Planner(Action<EmergeScope<ExecutionPlan>> configure)
  sealed class PlanRevision
    ctor()
    List<TaskNode> NewTasks { get; set; }
    string Reasoning { get; set; }
    Dictionary<string, string> TaskUpdates { get; set; }
    List<string> TasksToCancel { get; set; }
  sealed class PlanStep
    ctor()
    string Description { get; set; }
    bool RequiresTool { get; set; }
    string? ToolName { get; set; }
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
  sealed class ReviewFeedback
    ctor()
    double FitnessScore { get; set; }
    List<string> Insights { get; set; }
    List<string> Issues { get; set; }
    string Reasoning { get; set; }
    bool SuggestPlanRevision { get; set; }
  sealed class Route
    ctor()
    Action<EmergeScopeBase>? Configure { get; set; }
    string Description { get; set; }
    LLMModel? Model { get; set; }
    string Name { get; set; }
  sealed class RouterDecision
    ctor()
    string? Reasoning { get; set; }
    string SelectedRoute { get; set; }
  sealed class RouterOptions<T> : EmergeScope<T>
    ctor()
    EmergeScope<RouterDecision> RouterScope { get; }
    List<Route> Routes { get; }
    void AddRoute(string name, string description, LLMModel? model = null, Action<EmergeScopeBase>? configure = null)
    void Router(Action<EmergeScope<RouterDecision>> configure)
  sealed class RunnerCompletedEvent : RunnerEvent, IEquatable<RunnerCompletedEvent>
    ctor(string FinalText)
    string FinalText { get; init; }
  sealed class RunnerErrorEvent : RunnerEvent, IEquatable<RunnerErrorEvent>
    ctor(string Error)
    string Error { get; init; }
  abstract class RunnerEvent : IEquatable<RunnerEvent>
  sealed class RunnerTextEvent : RunnerEvent, IEquatable<RunnerTextEvent>
    ctor(string Text)
    string Text { get; init; }
  sealed class RunnerToolPlannedEvent : RunnerEvent, IEquatable<RunnerToolPlannedEvent>
    ctor(string ToolName, string ParametersJson)
    string ParametersJson { get; init; }
    string ToolName { get; init; }
  sealed class RunnerToolResultEvent : RunnerEvent, IEquatable<RunnerToolResultEvent>
    ctor(string ToolName, string Result, bool IsError)
    bool IsError { get; init; }
    string Result { get; init; }
    string ToolName { get; init; }
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
  sealed class SelfConsistencyOptions<T> : EmergeScope<T>
    ctor()
    int MaxParallel { get; set; }
    Action<CandidateScope<T>>? SampleConfig { get; set; }
    int Samples { get; set; }
    Func<IReadOnlyList<T>, T>? SelectMajority { get; set; }
    void Sample(Action<CandidateScope<T>> configure)
  sealed class SolverCriticVerifierOptions<T> : EmergeScope<T>
    ctor()
    EmergeScope CriticScope { get; }
    int MaxRounds { get; set; }
    EmergeScope<T> SolverScope { get; }
    EmergeScope<T> VerifierScope { get; }
    void Critic(Action<EmergeScope> configure)
    void Solver(Action<EmergeScope<T>> configure)
    void Verifier(Action<EmergeScope<T>> configure)
  sealed class Stage<T> : EmergeEvent<T>, IEquatable<Stage<T>>
    ctor(string Name)
    string Name { get; init; }
  sealed class Stopped<T> : EmergeEvent<T>, IEquatable<Stopped<T>>
    ctor(KernelContext Context, string? Reason)
    KernelContext Context { get; init; }
    string? Reason { get; init; }
  sealed class SwarmAgent<T>
    ctor()
    List<string> DependsOn { get; set; }
    string? Id { get; set; }
    string Role { get; set; }
    EmergeScope<T> Scope { get; }
  sealed class SwarmOptions<T> : EmergeScope<T>
    ctor()
    List<SwarmAgent<T>> Agents { get; }
    EmergeScope<T> CoordinatorScope { get; }
    int MaxParallel { get; set; }
    int MaxRounds { get; set; }
    Func<IReadOnlyList<T>, T>? Merge { get; set; }
    void AddAgent(string role, Action<EmergeScope<T>> configure)
    void Coordinator(Action<EmergeScope<T>> configure)
  sealed class TaskGraphOptions<T> : EmergeScope<T>
    ctor()
    bool EnableParallelReview { get; set; }
    int MaxParallel { get; set; }
    Func<string, Task>? OnHumanFeedback { get; set; }
    Action<PlanRevision>? OnPlanRevised { get; set; }
    Action<ReviewFeedback>? OnReviewCompleted { get; set; }
    Action<TaskNode, object?>? OnTaskCompleted { get; set; }
    EmergeScope<PlanRevision> PlanReviserScope { get; }
    int ReviewIntervalTasks { get; set; }
    EmergeScope<ReviewFeedback> ReviewerScope { get; }
    EmergeScope<T> SynthesizerScope { get; }
    List<TaskNode> Tasks { get; }
    EmergeScope<T> WorkerScope { get; }
    void AddTask(string id, string description, params string[] blockedBy)
    void PlanReviser(Action<EmergeScope<PlanRevision>> configure)
    void Reviewer(Action<EmergeScope<ReviewFeedback>> configure)
    void Synthesizer(Action<EmergeScope<T>> configure)
    void Worker(Action<EmergeScope<T>> configure)
  sealed class TaskNode
    ctor()
    List<string> BlockedBy { get; set; }
    List<string> Blocks { get; set; }
    string Description { get; set; }
    string? Error { get; set; }
    string Id { get; set; }
    string? Owner { get; set; }
    object? Result { get; set; }
    string Status { get; set; }
  sealed class TestRefineFeedback
    ctor()
    bool Continue { get; set; }
    string? Feedback { get; set; }
    ScoreBreakdown? Score { get; set; }
  sealed class TestRefineOptions<T> : EmergeScope<T>
    ctor()
    Func<T, int, Task>? Apply { get; set; }
    Func<T, int, Task<TestRefineFeedback>>? Evaluate { get; set; }
    EmergeScope<T> InitialScope { get; }
    int MaxIterations { get; set; }
    EmergeScope<T> RefinementScope { get; }
    void Initial(Action<EmergeScope<T>> configure)
    void Refinement(Action<EmergeScope<T>> configure)
  sealed class ThoughtNode<T>
    ctor()
    List<ThoughtNode<T>> Children { get; }
    int Depth { get; set; }
    ThoughtNode<T>? Parent { get; set; }
    string? Reasoning { get; set; }
    double Score { get; set; }
    T Value { get; set; }
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
    ctor(FunctionCall Call, StreamingResult[] StreamingResults, object Result)
    FunctionCall Call { get; init; }
    object Result { get; init; }
    StreamingResult[] StreamingResults { get; init; }
  sealed class TreeOfThoughtOptions<T> : EmergeScope<T>
    ctor()
    int BeamWidth { get; set; }
    int BranchingFactor { get; set; }
    Func<T, EmergenceTrace, double>? Evaluate { get; set; }
    EmergeScope<T> EvaluatorScope { get; }
    int MaxDepth { get; set; }
    EmergeScope<T> ThoughtScope { get; }
    void Evaluator(Action<EmergeScope<T>> configure)
    void Thought(Action<EmergeScope<T>> configure)
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
    abstract IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = null)
  class StringContentReader : IContentReader
    ctor(string content)
    IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = null)
  class TreeIndex
    ctor()
    ctor(TreeNode root)
    TreeNode Root { get; set; }
    static IAsyncEnumerable<EmergeEvent<TreeIndex>> BuildAsync(LLMModel model, string content, TreeIndexOptions? options = null, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<TreeIndex>> BuildAsync(LLMModel model, IContentReader reader, TreeIndexOptions? options = null, CancellationToken ct = null)
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
  enum GovernanceAction
    Allow
    Deny
    Escalate
    Obfuscate
    Delay
  // The pending AI operation presented to the hook. Operation discriminates surface ("ai_call", "tool", "ingest"); Subject is the thing being acted on (model name, tool name, corpus name); Args are call-specific parameters; Ctx carries host-supplied identity / mission / runtime context (mission_id, agent_id, thread_id, user, tenant, time, etc.).
  sealed class GovernanceCall : IEquatable<GovernanceCall>
    // The pending AI operation presented to the hook. Operation discriminates surface ("ai_call", "tool", "ingest"); Subject is the thing being acted on (model name, tool name, corpus name); Args are call-specific parameters; Ctx carries host-supplied identity / mission / runtime context (mission_id, agent_id, thread_id, user, tenant, time, etc.).
    ctor(string Operation, string Subject, IReadOnlyDictionary<string, object?> Args, IReadOnlyDictionary<string, object?> Ctx)
    IReadOnlyDictionary<string, object?> Args { get; init; }
    IReadOnlyDictionary<string, object?> Ctx { get; init; }
    string Operation { get; init; }
    string Subject { get; init; }
  // What happened after the operation ran (or didn't). Hooks use this in AfterAsync to close out the audit record.
  sealed class GovernanceCallResult : IEquatable<GovernanceCallResult>
    // What happened after the operation ran (or didn't). Hooks use this in AfterAsync to close out the audit record.
    ctor(bool Failed, string Outcome, string? ErrorMessage = null)
    string? ErrorMessage { get; init; }
    bool Failed { get; init; }
    string Outcome { get; init; }
  // Shared invocation wrapper used by every transport that gates a call through GovernanceScope . Builds the standard Before / Deny / Escalate / invoke / After flow once so HTTP, MCP, and any future transport stay symmetric — the only thing each transport supplies is the GovernanceCall shape and the inner invocation. With no hook active the wrap is a pass-through.
  static class GovernanceInvoker
    static Task<T> RunAsync<T>(GovernanceCall call, Func<Task<T>> invoke, CancellationToken ct = null)
  // What the hook decided. The host must honour Action : Allow → invoke the operationDeny → throw GovernanceDeniedException Escalate → suspend / route to Target Obfuscate → apply the named transformDelay → wait the named duration then proceed DecisionId is the audit identifier the host can attach to any subsequent telemetry tied to this operation.
  sealed class GovernanceOutcome : IEquatable<GovernanceOutcome>
    // What the hook decided. The host must honour Action : Allow → invoke the operationDeny → throw GovernanceDeniedException Escalate → suspend / route to Target Obfuscate → apply the named transformDelay → wait the named duration then proceed DecisionId is the audit identifier the host can attach to any subsequent telemetry tied to this operation.
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
  // Single hook surface called by every AI-touched primitive in the Ikon platform — LLM calls (Emerge.Run<T>), agent tool dispatch (Ikon.Agent2), data ingest steps — before they act. One contract, three surfaces. Host code activates a hook by entering a GovernanceScope ; downstream primitives read Current and consult the hook if it is set. The default — no scope active — is a no-op pass-through and the AI primitives behave exactly as they do without governance.
  interface IGovernanceHook
    abstract Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    abstract Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
  // Central configuration for SDK connection to the Ikon.AI function host. Uses BackendConfig mode (IkonBackend.Instance token) for authentication. Inherits from AsyncLocalInstance to support proper async local flow in tests and apps.
  class IkonAIConnection : AsyncLocalInstance<IkonAIConnection>
    ctor()
    IkonClientConfig? ConfigOverride { get; set; }
    Task ForceReconnectAsync(CancellationToken ct = null)
    // Gets or creates an IkonClient connected to the Ikon.AI function host. The client is cached per instance to avoid connection overhead on each call. If the client is reconnecting, waits for reconnection to complete.
    Task<IkonClient> GetOrCreateClientAsync(CancellationToken ct = null)
    // Pre-establishes the connection to the host app so that subsequent function calls do not incur connection setup latency.
    Task WarmupAsync(CancellationToken ct = null)
    static string ChannelKey
    static string DevelopmentSpaceId
    static string ExternalUserId
    static string ProductionSpaceId
  class ImplementationSelector : AsyncLocalInstance<ImplementationSelector>
    ctor()
    bool ForceLocal { get; set; }
    bool ForceRemote { get; set; }
  enum ModelCategory
    Classifier
    DepthEstimator
    Embeddings
    FileConverter
    ImageGenerator
    ImageSegmenter
    LLM
    MeshGenerator
    OCR
    Reranker
    SoundEffectGenerator
    SpeechGenerator
    SpeechRecognizer
    VideoEnhancer
    VideoGenerator
    WebScraper
    WebSearcher
  // JSON converter factory that handles deserialization of legacy model enum formats. Supports both the current enum names (e.g., "OpenAI3Small") and legacy canonical names (e.g., "OpenAI_3Small").
  class ModelEnumConverterFactory : JsonConverterFactory
    ctor()
    override bool CanConvert(Type typeToConvert)
    override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
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
  struct ModelRegionPriorityKey : IEquatable<ModelRegionPriorityKey>
    ctor(ModelCategory category, Organization organization, string modelFamilyName)
    ModelCategory Category { get; }
    string ModelFamilyName { get; }
    Organization Organization { get; }
  static class ModelRegionSelector
    static void SetPriorityList(ModelRegionPriorityKey key, IReadOnlyList<ModelRegion> priorities)
    static bool TryGetPriorityList(ModelRegionPriorityKey key, out IReadOnlyList<ModelRegion> priorities)
  // Default no-op hook. Allows every call, records nothing. Lets primitives treat the hook contract as non-nullable downstream.
  sealed class NullGovernanceHook : IGovernanceHook
    ctor()
    Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
    static NullGovernanceHook Instance
  enum Organization
    None
    AI21
    Anthropic
    AssemblyAI
    Aws
    Azure
    BlackForestLabs
    Cerebras
    Cohere
    ConvertApi
    DeepInfra
    Deepgram
    ElevenLabs
    Fal
    Fireworks
    Google
    Groq
    Hyperbolic
    Ikon
    Jina
    Meshy
    Mistral
    OpenAI
    OpenRouter
    Pollo
    SerpApi
    Spider
    Stability
    TensorPix
    Together
    Voyage
    XAI

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
    IAsyncEnumerable<StreamingResult> GenerateAsync(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = null)
    Task<T> GenerateObjectAsync<T>(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = null) where T : new()
    Task<string> GenerateStringAsync(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = null)
    T GetState<T>(string key)
    void SetState(string key, object? value)
    void StopProcessing()
    event EventHandler<string>? RenderedShader

namespace Ikon.AI.Classification
  sealed class ClassificationDetail
    ctor()
    ctor(ClassificationLabel label, string originalCategory, bool isFlagged, double score)
    bool IsFlagged { get; init; }
    ClassificationLabel Label { get; init; }
    string OriginalCategory { get; init; }
    double Score { get; init; }
    static ClassificationDetail ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ClassificationInput
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Text { get; init; }
    string Url { get; init; }
    static ClassificationInput FromMessagePart(IMessagePart messagePart)
    static ClassificationInput ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static string DisplayName(ClassificationModel model)
  sealed class ClassificationResult
    ctor()
    List<ClassificationDetail> Details { get; init; }
    bool IsFlagged { get; init; }
    static ClassificationResult ReadFromTeleport(ReadOnlySpan<byte> data)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Classifier : IClassifier, IDisposable
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ClassificationModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    Task<ClassificationResult> ClassifyAsync(string text, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    void Dispose()
    static ClassifierCapabilities GetCapabilities(ClassificationModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ClassificationModel model)
  sealed class ClassifierCapabilities
    ctor()
  interface IClassifier : IDisposable
    abstract Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    virtual Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    virtual Task<ClassificationResult> ClassifyAsync(string text, TimeSpan? timeout = null, CancellationToken cancellationToken = null)

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
  class DatabaseInfoExtractor.Config
    ctor()
    List<string>? ColumnExcludeRegex { get; set; }
    Dictionary<string, string> ColumnExtraInfo { get; set; }
    bool IncludeEmptyColumns { get; set; }
    int JsonSampleLengthLimit { get; set; }
    int JsonSampleRowLimit { get; set; }
    int NonTextSampleRowLimit { get; set; }
    List<string>? Schemas { get; set; }
    List<string>? TableExcludeRegex { get; set; }
    Dictionary<string, string> TableExtraInfo { get; set; }
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
  // Creates database connections. Prefer the typed factory methods ( Trino , Postgres , Sqlite , BigQuery ) for app code — host, port, and catalog are not secrets, only the password is. Pass that password from app.Secrets: DatabaseConnection.Trino(host: "trino.example.com", port: 443, catalog: "hive", user: "ikon", password: app.Secrets["TRINO_PASSWORD"]) CreateAsync remains for shared pipelines that read all of host/port/user/password/etc. from environment variables or space secrets.
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
    Task<ResultSet> GetCteDatabaseInfoAllValuesAsync(DatabaseInfo cteDatabaseInfo, int maxRows)
    static bool IsText(string dataType)
    Task<DatabaseInfo> ValidateAndFillCteDatabaseInfoAsync(DatabaseInfo cteDatabaseInfo, int maxRowsFilter)
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
    Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = null)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(DepthEstimatorModel model)
  sealed class DepthEstimatorConfig
    ctor()
    int? EnsembleSize { get; set; }
    DepthEstimatorConfig.InputImage Image { get; set; }
    int? NumInferenceSteps { get; set; }
    int? ProcessingResolution { get; set; }
    TimeSpan Timeout { get; set; }
    static DepthEstimatorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum DepthEstimatorModel
    DepthAnythingV2
    Marigold
    Midas
  static class DepthEstimatorModelExtensions
    static string DisplayName(DepthEstimatorModel model)
  sealed class DepthEstimatorResult
    ctor()
    DepthEstimatorResult.OutputImage Depth { get; set; }
    static DepthEstimatorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface IDepthEstimator : IDisposable
    abstract Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = null)
  sealed class DepthEstimatorConfig.InputImage
    ctor()
    byte[]? Data { get; set; }
    string? MimeType { get; set; }
    string? Url { get; set; }
    static DepthEstimatorConfig.InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class DepthEstimatorResult.OutputImage
    ctor()
    byte[] Data { get; set; }
    int Height { get; set; }
    string MimeType { get; set; }
    int Width { get; set; }
    static DepthEstimatorResult.OutputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.Embeddings
  enum EmbeddingEncoding
    Base64
    GzipBase64
  sealed class EmbeddingGenerator : IDisposable, IEmbeddingGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(EmbeddingModel model, IReadOnlyList<ModelRegion>? regions = null)
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    void Dispose()
    Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    static EmbeddingGeneratorCapabilities GetCapabilities(EmbeddingModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(EmbeddingModel model)
  sealed class EmbeddingGeneratorCapabilities
    ctor()
    int EmbeddingVectorSize { get; init; }
    int MaxInputCount { get; init; }
  sealed class EmbeddingItem
    ctor(string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, string embedding)
    string Context { get; init; }
    string Embedding { get; init; }
    float[] EmbeddingArray { get; }
    EmbeddingEncoding Encoding { get; init; }
    EmbeddingModel Model { get; init; }
    EmbeddingType Type { get; init; }
    static Task<EmbeddingItem> Create(string input, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, CancellationToken cancellationToken = null)
    static Task<EmbeddingItem> Create(float[] embedding, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding)
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
    static string DisplayName(EmbeddingModel model)
  enum EmbeddingType
    Generic
    Document
    Query
    Clustering
    Classification
  interface IEmbeddingGenerator : IDisposable
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    abstract Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
  struct VectorMath.Neighbor
    ctor(int index, float distance)
    float Distance { get; }
    int Index { get; }
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
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    static FileConverterCapabilities GetCapabilities(FileConverterModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(FileConverterModel model)
  sealed class FileConverterCapabilities
    ctor()
  sealed class FileConverterConfig
    ctor()
    AssetUri? AssetUri { get; set; }
    byte[]? Data { get; set; }
    string FileName { get; set; }
    TimeSpan Timeout { get; set; }
    string? Url { get; set; }
  enum FileConverterModel
    ConvertApi
  static class FileConverterModelExtensions
    static string DisplayName(FileConverterModel model)
  interface IFileConverter : IDisposable
    abstract Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = null)

namespace Ikon.AI.ImageGeneration
  interface IImageGenerator : IDisposable
    abstract Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = null)
  enum ImageBackground
    Auto
    Opaque
    Transparent
  sealed class ImageGenerator : IDisposable, IImageGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // One-shot image generation. The verbose form using var generator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage); var results = await generator.GenerateImageAsync(new ImageGeneratorConfig { Prompt = prompt }); var image = results.FirstOrDefault(); becomes var image = await ImageGenerator.GenerateAsync(prompt); Defaults to Gemini25FlashImage (cheap+fast). Override the model via the second parameter when the task warrants. Returns null if the model produces no results — caller should null-check before using .Data / .MimeType. Reach for the constructor + GenerateImageAsync when you need batch generation, custom width/height, an ImageBackground override, input images, or any other ImageGeneratorConfig field beyond the prompt.
    static Task<ImageGeneratorResult?> GenerateAsync(string prompt, ImageGeneratorModel model = Gemini25FlashImage, CancellationToken cancellationToken = null)
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = null)
    static ImageGeneratorCapabilities GetCapabilities(ImageGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageGeneratorModel model)
  sealed class ImageGeneratorCapabilities
    ctor()
  sealed class ImageGeneratorConfig
    ctor()
    ImageBackground Background { get; set; }
    int Count { get; set; }
    int Height { get; set; }
    List<InputImage> InputImages { get; set; }
    string NegativePrompt { get; set; }
    string Prompt { get; set; }
    ImageQuality Quality { get; set; }
    SafetyLevel SafetyLevel { get; set; }
    string SearchPrompt { get; set; }
    int Seed { get; set; }
    int Steps { get; set; }
    string Style { get; set; }
    TimeSpan Timeout { get; set; }
    bool UpsamplePrompt { get; set; }
    int Width { get; set; }
    static ImageGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static string DisplayName(ImageGeneratorModel model)
  sealed class ImageGeneratorResult
    ctor()
    byte[] Data { get; set; }
    int Height { get; set; }
    string MimeType { get; set; }
    int Width { get; set; }
    static ImageGeneratorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ImageQuality
    Auto
    Low
    Medium
    High
  sealed class InputImage
    ctor()
    byte[] Data { get; set; }
    double? MaskDilution { get; set; }
    string MimeType { get; set; }
    double? Strength { get; set; }
    InputImageType Type { get; set; }
    static InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum InputImageType
    Normal
    Mask
  enum SafetyLevel
    Level0
    Level1
    Level2
    Level3
    Level4
    Level5
    Level6

namespace Ikon.AI.ImageSegmentation
  sealed class ImageSegmenterConfig.BoxPrompt
    ctor()
    int? ObjectId { get; set; }
    double XMax { get; set; }
    double XMin { get; set; }
    double YMax { get; set; }
    double YMin { get; set; }
    static ImageSegmenterConfig.BoxPrompt ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface IImageSegmenter : IDisposable
    abstract Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = null)
  sealed class ImageSegmenter : IDisposable, IImageSegmenter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageSegmenterModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageSegmenterModel model)
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = null)
  sealed class ImageSegmenterConfig
    ctor()
    List<ImageSegmenterConfig.BoxPrompt> BoxPrompts { get; set; }
    ImageSegmenterConfig.InputImage Image { get; set; }
    int MaxMasks { get; set; }
    List<ImageSegmenterConfig.PointPrompt> PointPrompts { get; set; }
    string? Prompt { get; set; }
    bool ReturnMultipleMasks { get; set; }
    TimeSpan Timeout { get; set; }
    static ImageSegmenterConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ImageSegmenterModel
    Sam3
    Sam31
  static class ImageSegmenterModelExtensions
    static string DisplayName(ImageSegmenterModel model)
  sealed class ImageSegmenterResult
    ctor()
    ImageSegmenterResult.OutputImage? Preview { get; set; }
    List<ImageSegmenterResult.Segment> Segments { get; set; }
    static ImageSegmenterResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ImageSegmenterConfig.InputImage
    ctor()
    byte[]? Data { get; set; }
    string? MimeType { get; set; }
    string? Url { get; set; }
    static ImageSegmenterConfig.InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ImageSegmenterResult.OutputImage
    ctor()
    byte[] Data { get; set; }
    int Height { get; set; }
    string MimeType { get; set; }
    int Width { get; set; }
    static ImageSegmenterResult.OutputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ImageSegmenterConfig.PointPrompt
    ctor()
    bool IsBackground { get; set; }
    int? ObjectId { get; set; }
    double X { get; set; }
    double Y { get; set; }
    static ImageSegmenterConfig.PointPrompt ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ImageSegmenterResult.Segment
    ctor()
    List<double> Box { get; set; }
    ImageSegmenterResult.OutputImage Mask { get; set; }
    double? Score { get; set; }
    static ImageSegmenterResult.Segment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.Kernel
  sealed class AsyncEnumerableExtensions.<G>$CA58BA95B4ED5DE0AC5F384160329049
    Task<T1[]> AsArrayAsync<T1>()
    Task<T1> AsFirstAsync<T1>()
    Task<string> AsStringAsync()
    IAsyncEnumerable<StreamingResult> WithWindowedProcessingAsync(Func<string, List<StreamingResult>, Task<ValueTuple<bool, List<StreamingResult>>>> processAsync, int windowSize = 0, int windowOverlap = 0)
  static class AsyncEnumerableExtensions.<G>$CA58BA95B4ED5DE0AC5F384160329049.<M>$7325656A85ACD35A95DB91A9468B406C
  static class AsyncEnumerableExtensions
    static Task<T1[]> AsArrayAsync<T1>(IAsyncEnumerable<StreamingResult> source)
    static Task<T1> AsFirstAsync<T1>(IAsyncEnumerable<StreamingResult> source)
    static Task<string> AsStringAsync(IAsyncEnumerable<StreamingResult> source)
    static IAsyncEnumerable<StreamingResult> WithCitationsAsync(IAsyncEnumerable<StreamingResult> source, IdMapper idMapper)
    static IAsyncEnumerable<StreamingResult> WithParsedTagsAsync(IAsyncEnumerable<StreamingResult> source, List<string>? tagWhitelist = null, List<string>? tagBlacklist = null)
    static IAsyncEnumerable<StreamingResult> WithReasoningFromTagAsync(IAsyncEnumerable<StreamingResult> source, string reasoningTagName)
    static IAsyncEnumerable<StreamingResult> WithThrottlingAsync(IAsyncEnumerable<StreamingResult> source, int charsPerSecond, int charsPerUpdate, CancellationToken cancellationToken = null)
    static IAsyncEnumerable<StreamingResult> WithWindowedProcessingAsync(IAsyncEnumerable<StreamingResult> source, Func<string, List<StreamingResult>, Task<ValueTuple<bool, List<StreamingResult>>>> processAsync, int windowSize = 0, int windowOverlap = 0)
  struct AudioIdPart : IMessagePart
    ctor(string id)
    string Id { get; }
    MessagePartType Type { get; }
  struct AudioPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  class BinaryDataContainer
    ctor(byte[] data, string mimeType)
    byte[] Data { get; }
    string MimeType { get; }
  class Citation
    ctor(string originalId, string mappedId, int referStartIndex, int referEndIndex, int positionIndex)
    string MappedId { get; }
    string OriginalId { get; }
    int PositionIndex { get; }
    int ReferEndIndex { get; }
    int ReferStartIndex { get; }
  class FinalModelMessage
    ctor(string text)
    string Text { get; }
  class FinalTextResponse
    ctor(string text)
    string Text { get; }
  class FinishReason
    ctor(string reason)
    string Reason { get; }
  class FunctionCall
    ctor(Function function, object?[] parameters, string parametersJson, string callId, string hash, string thoughtSignature = "", string reasoningContent = "")
    string CallId { get; }
    Function Function { get; }
    string Hash { get; }
    object?[] Parameters { get; }
    string ParametersJson { get; }
    string ReasoningContent { get; }
    string ThoughtSignature { get; }
  class FunctionResult
    ctor(object? result = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null)
    string? ModelMessagePrefix { get; set; }
    string? ModelMessageSuffix { get; set; }
    object? Result { get; set; }
  struct FunctionResultPart : IMessagePart
    ctor(FunctionCall functionCall, StreamingResult[] streamingResults, object result)
    FunctionCall FunctionCall { get; }
    object Result { get; }
    StreamingResult[] StreamingResults { get; }
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
  class JsonExampleGenerator
    ctor()
    static JsonNode DeepSerialize(object? obj)
    static T GenerateExampleInstance<T>()
    static string GenerateExampleJson<T>()
  // Generates JSON Schema definitions from .NET types. To satisfy the OpenAI spec, every object schema’s "required" array must exactly equal the keys in "properties", and every object schema must have a "type": "object" key. Properties that are allowed to be null are marked according to the target dialect: the 2020-12 dialect expands "type" into a ["X", "null"] union, while the OpenAPI 3.0 dialect adds a sibling "nullable": true.
  static class JsonSchemaGenerator
    static ExpandoObject GenerateJsonSchemaExpandoObject<T>(SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    // Generate the schema as a JsonNode tree rather than a serialised string. Handles primitives (string, int, bool, ...), enums, arrays, dictionaries, and complex types — i.e. valid as a root for any callable shape, not just records. Useful when the caller wants to embed the schema into a larger JSON structure without the round-trip of string→parse.
    static JsonNode GenerateSchemaNode(Type type, string? description = null, SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    static string GenerateSchemaString<T>(SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    // Non-generic overload for callers that have a Type at runtime (reflection, dynamic dispatch, MCP tool-schema generation). Same semantics as the generic version.
    static string GenerateSchemaString(Type type, SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
  struct KernelContext : IEquatable<KernelContext>
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    // Alias for Empty . Some generated code reaches for `Default` first (common shadcn / .NET pattern).
    static KernelContext Default { get; }
    bool DisableFunctionCalling { get; init; }
    bool DiscardTextOutputWithFunctionCalls { get; init; }
    // A fresh, blank `KernelContext` — equivalent to `new KernelContext()` or `default`. Provided as a named constant for code generated against frameworks that expect an `.Empty` / `.Default` affordance on context-like types.
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
    static KernelContext Create(IEnumerable<Instruction>? instructions = null, IEnumerable<MessageBlock>? messages = null, IEnumerable<Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    IAsyncEnumerable<StreamingResult> GenerateAsync(ILLM llm, CancellationToken cancellationToken = null)
    KernelContext KeepMessagesMax(int count)
    IAsyncEnumerable<StreamingResult> RecurseAsync(IAsyncEnumerable<StreamingResult> generator, HashSet<string> alreadyCalledFunctions, CancellationToken cancellationToken = null)
    IAsyncEnumerable<StreamingResult> ReturnFunctionCallAsync(string name, string parametersJson, string callId, string thoughtSignature = "", string reasoningContent = "")
    IAsyncEnumerable<StreamingResult> RunFunctionAsync(string functionName, object?[] parameters, CancellationToken cancellationToken = null)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
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
  class OutputAudioId
    ctor(string id)
    string Id { get; }
  class OutputAudioTranscript
    ctor(string transcript)
    string Transcript { get; }
  struct PdfPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct PdfUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  class ReasoningBlock
    ctor(string text)
    string Text { get; }
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
  struct StreamingResult
    ctor(object value, string sourceName, string? valueTypeName = null)
    string SourceName { get; }
    object Value { get; }
    string? ValueTypeName { get; }
  class Tag
    ctor(string name, string content, Dictionary<string, string>? attributes = null)
    Dictionary<string, string>? Attributes { get; }
    string Content { get; }
    string Name { get; }
  struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  class TokenUsage
    ctor(int inputTokens, int cachedInputTokens, int cacheCreationInputTokens, int outputTokens)
    int CacheCreationInputTokens { get; }
    // Subset of InputTokens served from the provider's prompt cache (Anthropic cache_read_input_tokens, OpenAI cached_tokens, Bedrock CacheReadInputTokens). Always included in InputTokens; this is the cache-attributable portion.
    int CachedInputTokens { get; }
    int InputTokens { get; }
    int OutputTokens { get; }
  class ToolPlan
    ctor(string text)
    string Text { get; }
  struct VideoAssetPart : IMessagePart
    ctor(AssetUri uri, string? mimeType = null)
    string? MimeType { get; }
    MessagePartType Type { get; }
    AssetUri Uri { get; }
  struct VideoPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct VideoUrlPart : IMessagePart
    ctor(string url, string mimeType)
    string MimeType { get; }
    MessagePartType Type { get; }
    string Url { get; }

namespace Ikon.AI.LLM
  interface ILLM : IDisposable, ILLMInfo
    abstract IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext context, CancellationToken cancellationToken = null)
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
    IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext context, CancellationToken cancellationToken = null)
    static LLMCapabilities GetCapabilities(LLMModel model)
    static LLMCapabilities GetCapabilities(LLMModel model, IReadOnlyList<ModelRegion>? regions)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(LLMModel model)
  sealed class LLMCapabilities : ILLMInfo
    ctor()
    int ContextWindowSize { get; init; }
    string InlineReasoningTagName { get; init; }
    SchemaDialect SchemaDialect { get; init; }
    bool SupportsGbnfGrammar { get; init; }
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
    CommandR
    CommandA
    CommandAReasoning
    KimiK25
    KimiK26
    Qwen36
    GptOss120B
    Glm5
    Glm51
    MiniMaxM25
    MiniMaxM27
    DeepSeekV32
    DeepSeekV4Pro
    DeepSeekV4Flash
    NovaPro
    NovaLite
    NovaMicro
    Nova2Lite
  static class LLMModelExtensions
    // Maximum input-context window for the model, in tokens (e.g. 200_000 for Claude 4.x base, 1_000_000 for the 1M-context tier). Returns 0 when the model can't be resolved — callers should treat 0 as "unknown" and skip utilization computation rather than dividing by zero.
    static int ContextWindowSize(LLMModel model)
    static string DisplayName(LLMModel model)

namespace Ikon.AI.Legacy
  class Mind : IAsyncDisposable
    ctor()
    Context CurrentUserClientContext { get; }
    string CurrentUserLocale { get; }
    string? DefaultModelName { get; set; }
    string? DefaultSecondaryModelName { get; set; }
    string DefaultUserLocale { get; set; }
    string DefaultUserName { get; set; }
    KernelContext KernelContext { get; }
    Task AddModelInput(string text, bool isHistory = false)
    Task AddUserInput(Context clientContext, string userName, string userLocale, IReadOnlyList<object> inputs, bool isHistory = false)
    Task CancelGenerateAnswer()
    void ClearMessageHistory()
    void ClearState()
    ValueTask DisposeAsync()
    Task GenerateAnswer(string? command = null, string? context = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null, Context? clientContext = null, List<ValueTuple<string, object?>>? variables = null)
    T GetState<T>(string key)
    T GetState<T>(string key, T defaultValue)
    Task InitializeAsync(MindConfig config, Retriever retriever, string mindUserName, Context hostClientContext, AssetUri? shaderUri = null)
    Mind.ShaderLoadResult LoadShader(string shaderContent)
    Task PostMessage(string text)
    Task RegenerateAnswer(Context? clientContext = null)
    Task RequestGenerateAnswer(string? command = null, string? context = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null, Context? clientContext = null, List<ValueTuple<string, object?>>? variables = null)
    Task RequestRegenerateAnswer(Context? clientContext = null)
    void SetState<T>(string key, T value)
    Task StopAsync()
    Task WaitGenerateAnswer()
    Func<Task> Activity
    Func<Task> Cancel
    Func<MindResult, Task> Finish
    Func<List<KernelContext>> GetContexts
    Func<StreamingResult, Task> Output
    Action PreStart
    Action<string> RenderedShader
    Func<Task> Retry
    Func<Task> Start
    Func<Dictionary<string, object?>, Task> StateUpdate
  class MindConfig
    ctor()
    int ActivityIntervalMs
    string BackupFailureMessage
    bool ClipLongUserMessagesInsteadOfError
    bool EnableRenderedShaderLogging
    bool IncludeReasonInFailureMessage
    int MaxHistoryLength
    int MaxRetryCount
    int MaxUserMessageLength
    int MaxUserMessagesRateLimit
    double MaxUserMessagesRateWindow
  class MindResult
    ctor()
    string AudioId { get; set; }
    string ModelMessage { get; set; }
    string TextResponse { get; set; }
  class Mind.ShaderLoadResult
    ctor()
    string ErrorMessage
    bool IsSuccess

namespace Ikon.AI.MeshGeneration
  interface IMeshGenerator : IDisposable, IMeshGeneratorInfo
    abstract Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = null)
  interface IMeshGeneratorInfo
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
  sealed class MeshGeneratorConfig.InputImage
    ctor()
    byte[]? Data { get; set; }
    string? MimeType { get; set; }
    string? Url { get; set; }
    static MeshGeneratorConfig.InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class MeshGenerator : IDisposable, IMeshGenerator, IMeshGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MeshGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
    void Dispose()
    Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = null)
    static MeshGeneratorCapabilities GetCapabilities(MeshGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(MeshGeneratorModel model)
  sealed class MeshGeneratorCapabilities : IMeshGeneratorInfo
    ctor()
    int MaxInputImages { get; init; }
    bool SupportsImageToMesh { get; init; }
    bool SupportsLowPoly { get; init; }
    bool SupportsPbr { get; init; }
    bool SupportsTextToMesh { get; init; }
  sealed class MeshGeneratorConfig
    ctor()
    bool EnablePbr { get; set; }
    List<MeshGeneratorConfig.InputImage> InputImages { get; set; }
    MeshGeneratorMeshStyle MeshStyle { get; set; }
    string? Prompt { get; set; }
    bool Remesh { get; set; }
    int TargetPolycount { get; set; }
    bool Texture { get; set; }
    string? TexturePrompt { get; set; }
    TimeSpan Timeout { get; set; }
    MeshGeneratorTopology Topology { get; set; }
    static MeshGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum MeshGeneratorMeshStyle
    Standard
    LowPoly
  enum MeshGeneratorModel
    Meshy5
    Meshy6
  static class MeshGeneratorModelExtensions
    static string DisplayName(MeshGeneratorModel model)
  // Result of a mesh generation. The URLs are signed and expire roughly three days after generation, so download the model files promptly.
  sealed class MeshGeneratorResult
    ctor()
    DateTimeOffset? ExpiresAt { get; set; }
    string? FbxUrl { get; set; }
    string? GlbUrl { get; set; }
    string? MtlUrl { get; set; }
    string? ObjUrl { get; set; }
    string? ThumbnailUrl { get; set; }
    string? UsdzUrl { get; set; }
    static MeshGeneratorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum MeshGeneratorTopology
    Triangle
    Quad

namespace Ikon.AI.OCR
  enum DocumentType
    General
  interface IOCR : IDisposable, IOCRInfo
    abstract Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = null)
    abstract IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = null)
  interface IOCRInfo
    int MaxPagesSupported { get; }
  sealed class OCR : IDisposable, IOCR, IOCRInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxPagesSupported { get; }
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = null)
    IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = null)
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
  enum OCRModel
    AzureDocumentIntelligence
    MistralOCR
  static class OCRModelExtensions
    static string DisplayName(OCRModel model)
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

namespace Ikon.AI.Policy
  sealed class CreditLimitChecker : IUsageLimitChecker
    ctor()
    ValueTask<UsageLimitCheckResult> CheckAsync(PolicyCallContext context, object?[] args)

namespace Ikon.AI.Reranking
  interface IReranker : IDisposable
    abstract Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
  sealed class RerankItem
    ctor()
    int Index { get; init; }
    double Score { get; init; }
    static RerankItem ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum RerankModel
    CohereRerank4Fast
    CohereRerank4Pro
    JinaReranker3
    VoyageRerank25
    VoyageRerank25Lite
  static class RerankModelExtensions
    static string DisplayName(RerankModel model)
  sealed class Reranker : IDisposable, IReranker
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(RerankModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static RerankerCapabilities GetCapabilities(RerankModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(RerankModel model)
    Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
  sealed class RerankerCapabilities
    ctor()

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
    List<ValueTuple<string, string>> GenerateHierarchicalSplitLinks()
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
  class Retriever.GetContentsOptions2
    ctor()
    bool IncludeFullTexts { get; set; }
    int MaxRerankResults { get; set; }
    int MaxSearchResults { get; set; }
    double RerankThreshold { get; set; }
    float SearchThreshold { get; set; }
  class IdMapper
    ctor(IdMappingType mappingType = None, int randomHexLength = 8, int randomLettersLength = 8, int integerCounter = 0, int? seed = null)
    string ToMapped(string original)
    string ToOriginal(string mapped)
    bool TryToOriginal(string mapped, out string original)
    ConcurrentDictionary<string, string> Mapping
    ConcurrentDictionary<string, string> ReverseMapping
  enum IdMappingType
    None
    RandomHex
    RandomLetters
    IncreasingInteger
  class JsonAsset
    ctor(string content)
    IEnumerable<string> GetAllKeys()
    string[] GetKeys()
    bool TryGetValue(string keyPath, out object? value)
    bool TryGetValueAsObject(string keyPath, out object? value)
  class Retriever : IAsyncDisposable
    ctor()
    KernelContext Context { get; }
    IdMapper IdMapper { get; }
    ValueTask DisposeAsync()
    Task<ContentLink[]> Expand(ContentLink[] links)
    Task<ContentLink[]> Expand(ContentLink link)
    Task<Content?> GetContent(ContentLink link)
    Retriever.ContentMetadata? GetContentMetadata(string metadataId)
    Task<string> GetContents(string query, Retriever.GetContentsOptions options)
    Task<string> GetContents2(string query, Retriever.GetContentsOptions2 options)
    ContentLink? Ignore(ContentLink link, string detail)
    Task InitializeAsync(string dataDirectory, EmbeddingModel embeddingModel = OpenAI3Small)
    Task InitializeAsync(IReadOnlyList<AssetUri> assetUris, EmbeddingModel embeddingModel = OpenAI3Small)
    ContentLink[] Prefer(ContentLink link, string detail)
    ContentLink[] Prefer(ContentLink[] links, string detail)
    Task<ContentLink[]> Search(string query, int maxLinks = 25, float searchThreshold = 0.1)
    Task<Retriever.Event[]> SearchEvents(string startUtcTimestamp, string endUtcTimestamp, int maxResults = 100)
    Task<Retriever.Event[]> SearchEvents(string startUtcTimestamp, string endUtcTimestamp, string searchString, int maxResults = 100)
    Task<KeywordSearchResult[]> SearchKeywords(string searchString, int maxResults = 100)
    Task StopAsync()
    Task WaitForLoadingToEnd()

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
    abstract void AddFilter(string name, KernelContext context, Function function)
    abstract void AddFunction(string name, KernelContext context, Function function)
    abstract bool ContainsKey(string key)
    abstract IEnumerable<string> GetKeys()
    abstract object? GetValue(string key)
    abstract string GetValueAsString(string key)
    abstract void Register<T>() where T : class
    abstract void SetValue(string key, object? value)
  interface IScriptEngine
    abstract IScriptContext CreateContext()
    abstract bool TryParse(string template, out IScriptTemplate? parsedTemplate, out string? errorMessage)
  interface IScriptTemplate
    abstract Task<string> RenderAsync(IScriptContext context)
  class ShaderCache.ImplicitShader
    ctor(AssetUri? shaderUri, string callerFilePath, ShaderCache outer)
    IAsyncEnumerable<StreamingResult> GenerateAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<Shader> GetShaderAsync()
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
  class ScriptableValueConverter : JsonConverter
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
  class Shader
    ctor(string shaderConfigAsJsonString, bool enableRenderedShaderLogging = false)
    Dictionary<string, object?> Input { get; }
    static string Escape(string? text)
    IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, ExpandoObject? implicitJsonSchema = null, string? implicitJsonExample = null, IdMapper? idMapper = null, string modelUserName = "", string modelMessagePrefix = "", string modelMessageSuffix = "", int iteration = 0, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null)
    Task<T> GenerateObjectAsync<T>(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, JsonSerializerOptions? jsonSerializerOptions = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null) where T : new()
    Task<string> GenerateStringAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null)
    void SetActiveState<T>(string key, T value)
    static string Unescape(string? text)
    event EventHandler<string>? RenderedShader
  class ShaderCache : AsyncLocalInstance<ShaderCache>
    ctor()
    string? DefaultSpaceId { get; set; }
    ShaderCache.ImplicitShader GetImplicitShader(string callerFilePath = "")
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
  class Shader.TemplateMessage
    ctor()
    string Content { get; set; }
    string Role { get; set; }
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
    abstract IAsyncEnumerable<AudioContainer> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
  interface ISoundEffectGeneratorInfo
    bool SupportsLooping { get; }
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
    IAsyncEnumerable<AudioContainer> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
    Task<SoundEffectFileResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
    static SoundEffectGeneratorCapabilities GetCapabilities(SoundEffectGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SoundEffectGeneratorModel model)
  sealed class SoundEffectGeneratorCapabilities : ISoundEffectGeneratorInfo
    ctor()
    bool SupportsLooping { get; init; }
  sealed class SoundEffectGeneratorConfig
    ctor()
    double? DurationSeconds { get; set; }
    bool Loop { get; set; }
    string Prompt { get; set; }
    double PromptInfluence { get; set; }
    TimeSpan Timeout { get; set; }
    static SoundEffectGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum SoundEffectGeneratorModel
    ElevenLabsV2
  static class SoundEffectGeneratorModelExtensions
    static string DisplayName(SoundEffectGeneratorModel model)

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
    abstract IAsyncEnumerable<AudioContainer> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = null)
  sealed class SpeechGenerator : IDisposable, ISpeechGenerator
    ctor(string modelName)
    ctor(SpeechGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    void Dispose()
    IAsyncEnumerable<AudioContainer> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = null)
    static SpeechGeneratorCapabilities GetCapabilities(SpeechGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechGeneratorModel model)
    static IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> GetVoiceIdsByModel()
  sealed class SpeechGeneratorCapabilities
    ctor()
  sealed class SpeechGeneratorConfig
    ctor()
    string Instructions { get; set; }
    string Language { get; set; }
    string Speed { get; set; }
    string Text { get; set; }
    TimeSpan Timeout { get; set; }
    string VoiceId { get; set; }
    static SpeechGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class SpeechGeneratorExtensions
    static Task StreamSpeechAsync(ISpeechGenerator speechGenerator, SpeechGeneratorConfig config, Func<AudioContainer, Task> onAudio, CancellationToken cancellationToken = null)
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
    static string DisplayName(SpeechGeneratorModel model)
  static class TextFilter
    static string Filter(string text, TextFilter.Config config)

namespace Ikon.AI.SpeechRecognition
  sealed class AnalyzePronunciationConfig
    ctor()
    int ChannelCount { get; set; }
    string Language { get; set; }
    string ReferenceText { get; set; }
    int SampleRate { get; set; }
    float[] Samples { get; set; }
    byte[] SamplesPcm16 { get; set; }
    TimeSpan Timeout { get; set; }
    static AnalyzePronunciationConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Break
    ctor()
    int BreakLength { get; init; }
    List<string> ErrorTypes { get; init; }
    Pronunciation.MissingBreak MissingBreak { get; init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get; init; }
    static Pronunciation.Break ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    TimeSpan MaxSpeechDuration { get; set; }
    SpeechRecognizerAdapter.Mode Mode { get; set; }
    TimeSpan RecognitionInterval { get; set; }
    TimeSpan RequestTimeout { get; set; }
    TimeSpan SilenceDuration { get; set; }
    float SilenceThreshold { get; set; }
  sealed class Pronunciation.Feedback
    ctor()
    Pronunciation.Prosody Prosody { get; init; }
    static Pronunciation.Feedback ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface ISpeechRecognizer : IDisposable, ISpeechRecognizerInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    abstract Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = null)
    abstract Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = null)
    abstract IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = null)
  interface ISpeechRecognizerInfo
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
  sealed class Pronunciation.Intonation
    ctor()
    List<string> ErrorTypes { get; init; }
    Pronunciation.Monotone Monotone { get; init; }
    static Pronunciation.Intonation ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.MissingBreak
    ctor()
    double Confidence { get; init; }
    static Pronunciation.MissingBreak ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
  sealed class Pronunciation.Monotone
    ctor()
    double SyllablePitchDeltaConfidence { get; init; }
    static Pronunciation.Monotone ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.NBest
    ctor()
    double Confidence { get; init; }
    string Display { get; init; }
    string ITN { get; init; }
    string Lexical { get; init; }
    string MaskedITN { get; init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Word> Words { get; init; }
    static Pronunciation.NBest ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Phoneme
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
    static Pronunciation.Phoneme ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.PhonemePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    static Pronunciation.PhonemePronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class Pronunciation
  sealed class Pronunciation.PronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    double CompletenessScore { get; init; }
    double FluencyScore { get; init; }
    double PronScore { get; init; }
    double ProsodyScore { get; init; }
    static Pronunciation.PronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Prosody
    ctor()
    Pronunciation.Break Break { get; init; }
    Pronunciation.Intonation Intonation { get; init; }
    static Pronunciation.Prosody ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RecognizeContinuousSpeechConfig
    ctor()
    string[] CandidateLanguages { get; set; }
    int ChannelCount { get; set; }
    string Language { get; set; }
    int SampleRate { get; set; }
    static RecognizeContinuousSpeechConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RecognizeSpeechConfig
    ctor()
    int ChannelCount { get; set; }
    string Language { get; set; }
    string Prompt { get; set; }
    int SampleRate { get; set; }
    float[] Samples { get; set; }
    byte[] SamplesPcm16 { get; set; }
    double Temperature { get; set; }
    TimeSpan Timeout { get; set; }
    static RecognizeSpeechConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Result
    ctor()
    int Channel { get; init; }
    string DisplayText { get; init; }
    long Duration { get; init; }
    string Id { get; init; }
    List<Pronunciation.NBest> NBest { get; init; }
    long Offset { get; init; }
    string RecognitionStatus { get; init; }
    double SNR { get; init; }
    static Pronunciation.Result ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SpeechRecognizer : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(SpeechRecognizerModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    static SpeechRecognizerCapabilities GetCapabilities(SpeechRecognizerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechRecognizerModel model)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = null)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = null)
  sealed class SpeechRecognizerAdapter : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(ISpeechRecognizer speechRecognizer, SpeechRecognizerAdapter.Config? config = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = null)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = null)
  sealed class SpeechRecognizerCapabilities : ISpeechRecognizerInfo
    ctor()
    bool SupportsBatchRecognition { get; init; }
    bool SupportsContinuousRecognition { get; init; }
    bool SupportsPronunciationAnalysis { get; init; }
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
    static string DisplayName(SpeechRecognizerModel model)
  sealed class Pronunciation.Syllable
    ctor()
    long Duration { get; init; }
    string Grapheme { get; init; }
    long Offset { get; init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
    static Pronunciation.Syllable ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.SyllablePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    static Pronunciation.SyllablePronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.UnexpectedBreak
    ctor()
    double Confidence { get; init; }
    static Pronunciation.UnexpectedBreak ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Word
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    List<Pronunciation.Phoneme> Phonemes { get; init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Syllable> Syllables { get; init; }
    string Text { get; init; }
    static Pronunciation.Word ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.WordPronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    string ErrorType { get; init; }
    Pronunciation.Feedback Feedback { get; init; }
    static Pronunciation.WordPronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.Storage
  class KeywordIndex
    ctor()
    Task Add(string word, string link)
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
    Task<int> GetDataItemCount(string collectionName)
    Task RemoveAsync(string collectionName, IEnumerable<string> tags)
    Task<List<Result<object>>> SearchAsync(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<object>>> SearchAsync(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<int> SetAsync(string collectionName, int? key, string text, object value, IEnumerable<string>? tags = null)
    Task<int> SetAsync(string collectionName, int? key, float[] vector, object value, IEnumerable<string>? tags = null)

namespace Ikon.AI.Utils
  static class HttpUtils
    static Task<string> DumpHttpRequest(HttpRequestMessage request)
    static Task<string> GetErrorMessage(HttpRequestException exception, HttpResponseMessage? response, string modelName)
    static Task<int> GetHttpRequestSize(HttpRequestMessage request)
  static class ImageUtils
    static byte[] ConvertAlphaMaskToBlackWhiteMask(byte[] maskData)
    static byte[] ConvertBlackWhiteMaskToAlphaMask(byte[] maskData)
    static ValueTuple<int, int> GetImageDimensions(byte[] buffer)
    static byte[] InvertMask(byte[] maskData)

namespace Ikon.AI.VideoEnhancement
  interface IVideoEnhancer : IDisposable
    abstract Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = null)
  sealed class VideoEnhancer : IDisposable, IVideoEnhancer
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoEnhancerModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = null)
    static VideoEnhancerCapabilities GetCapabilities(VideoEnhancerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoEnhancerModel model)
  sealed class VideoEnhancerCapabilities
    ctor()
  sealed class VideoEnhancerConfig
    ctor()
    int? EndFrame { get; set; }
    string? MimeType { get; set; }
    int? StartFrame { get; set; }
    int? TargetFps { get; set; }
    TimeSpan Timeout { get; set; }
    byte[]? VideoData { get; set; }
    string? VideoUrl { get; set; }
    static VideoEnhancerConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum VideoEnhancerModel
    TensorPixFpsBoost
    TensorPixUpscale2xUltra4
    TensorPixUpscale2xUltra41
    TensorPixUpscale4xUltra4
  static class VideoEnhancerModelExtensions
    static string DisplayName(VideoEnhancerModel model)
  sealed class VideoEnhancerResult
    ctor()
    int? OutputFps { get; init; }
    long? OutputSizeBytes { get; init; }
    string Url { get; init; }
    static VideoEnhancerResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.VideoGeneration
  interface IVideoGenerator : IDisposable, IVideoGeneratorInfo
    abstract Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = null)
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
  sealed class VideoGeneratorConfig.InputImage
    ctor()
    byte[]? Data { get; set; }
    string? MimeType { get; set; }
    string? Url { get; set; }
    static VideoGeneratorConfig.InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = null)
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
  sealed class VideoGeneratorConfig
    ctor()
    VideoGeneratorAspectRatio AspectRatio { get; set; }
    bool? GenerateAudio { get; set; }
    List<VideoGeneratorConfig.InputImage> InputImages { get; set; }
    int Length { get; set; }
    string? NegativePrompt { get; set; }
    string? Prompt { get; set; }
    VideoGeneratorResolution Resolution { get; set; }
    int? Seed { get; set; }
    TimeSpan Timeout { get; set; }
    static VideoGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum VideoGeneratorModel
    Hailuo23
    Hailuo23Fast
    Kling26
    Kling30
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
    GrokImagineVideo
  static class VideoGeneratorModelExtensions
    static string DisplayName(VideoGeneratorModel model)
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
  sealed class VideoGeneratorResult
    ctor()
    string Url { get; init; }
    static VideoGeneratorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.WebScraping
  sealed class Cookie
    ctor()
    string Domain { get; set; }
    double ExpirationDate { get; set; }
    bool HostOnly { get; set; }
    bool HttpOnly { get; set; }
    int Id { get; set; }
    string Name { get; set; }
    string Path { get; set; }
    string SameSite { get; set; }
    bool Secure { get; set; }
    bool Session { get; set; }
    string StoreId { get; set; }
    string Value { get; set; }
    static Cookie ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class DownloadFileConfig
    ctor()
    string CountryCode { get; set; }
    TimeSpan Timeout { get; set; }
    string Url { get; set; }
    static DownloadFileConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class DownloadFileResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Url { get; init; }
    static DownloadFileResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface IWebScraper : IDisposable, IWebScraperInfo
    abstract Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = null)
    abstract Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = null)
    abstract Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = null)
    abstract Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = null)
  interface IWebScraperInfo
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
  sealed class MultiPageScrapeConfig
    ctor()
    bool AddGivenUrlsToWhitelist { get; set; }
    bool AllowOnlyGivenUrls { get; set; }
    List<Cookie> Cookies { get; set; }
    string CountryCode { get; set; }
    int DelayMs { get; set; }
    string ExcludedCSSElements { get; set; }
    List<string> ExcludedLineStarts { get; set; }
    List<string> ExcludedWholeLines { get; set; }
    bool Headless { get; set; }
    bool IgnoreRobotsTxt { get; set; }
    bool IncludeLinkedFiles { get; set; }
    string IncludedCSSElements { get; set; }
    string JavaScript { get; set; }
    bool LoadResources { get; set; }
    string Locale { get; set; }
    int MaxDepth { get; set; }
    int MaxPages { get; set; }
    WebScraperOutputFormat OutputFormat { get; set; }
    string PlaywrightScript { get; set; }
    bool RerunIfGivenUrlsMissing { get; set; }
    TimeSpan SinglePageTimeout { get; set; }
    TimeSpan Timeout { get; set; }
    List<string> UrlBlacklist { get; set; }
    List<string> UrlWhitelist { get; set; }
    List<string> Urls { get; set; }
    bool UseReadability { get; set; }
    bool UseSitemap { get; set; }
    bool UseSitemapOnly { get; set; }
    bool UseStreaming { get; set; }
    TimeSpan WaitAfter { get; set; }
    static MultiPageScrapeConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class PageResult
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string Mimetype { get; init; }
    string Title { get; init; }
    string Url { get; init; }
    static PageResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ScreenshotConfig
    ctor()
    List<Cookie> Cookies { get; set; }
    string CountryCode { get; set; }
    bool FullPage { get; set; }
    bool Headless { get; set; }
    int Height { get; set; }
    string JavaScript { get; set; }
    string Locale { get; set; }
    string PlaywrightScript { get; set; }
    TimeSpan Timeout { get; set; }
    string Url { get; set; }
    bool UseCaptchaSolver { get; set; }
    TimeSpan WaitAfter { get; set; }
    int Width { get; set; }
    static ScreenshotConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ScreenshotResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    static ScreenshotResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SinglePageScrapeConfig
    ctor()
    List<Cookie> Cookies { get; set; }
    string CountryCode { get; set; }
    string ExcludedCSSElements { get; set; }
    List<string> ExcludedLineStarts { get; set; }
    List<string> ExcludedWholeLines { get; set; }
    bool Headless { get; set; }
    bool IncludeLinkedFiles { get; set; }
    string IncludedCSSElements { get; set; }
    string JavaScript { get; set; }
    bool LoadResources { get; set; }
    string Locale { get; set; }
    WebScraperOutputFormat OutputFormat { get; set; }
    string PlaywrightScript { get; set; }
    TimeSpan Timeout { get; set; }
    string Url { get; set; }
    bool UseCaptchaSolver { get; set; }
    bool UseReadability { get; set; }
    TimeSpan WaitAfter { get; set; }
    static SinglePageScrapeConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = null)
    static WebScraperCapabilities GetCapabilities(WebScraperModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebScraperModel model)
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = null)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = null)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = null)
  sealed class WebScraperCapabilities : IWebScraperInfo
    ctor()
    bool SupportsFileDownload { get; init; }
    bool SupportsMultiPageScraping { get; init; }
    bool SupportsScreenshotting { get; init; }
    bool SupportsSinglePageScraping { get; init; }
  enum WebScraperModel
    Spider
    Jina
    LocalPuppeteer
    LocalNodriver
    LocalPlaywright
  static class WebScraperModelExtensions
    static string DisplayName(WebScraperModel model)
  enum WebScraperOutputFormat
    Text
    Markdown
    Html

namespace Ikon.AI.WebSearching
  interface IWebSearcher : IDisposable, IWebSearcherInfo
    abstract Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
    abstract Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
  interface IWebSearcherInfo
    bool SupportsImageSearching { get; }
  sealed class SearchConfig
    ctor()
    string CountryCode { get; set; }
    string InSiteUrl { get; set; }
    string Language { get; set; }
    int MaxResults { get; set; }
    WebSearcherOutputFormat OutputFormat { get; set; }
    string Query { get; set; }
    TimeSpan Timeout { get; set; }
    static SearchConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SearchResult
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string Mimetype { get; init; }
    string Title { get; init; }
    string Url { get; init; }
    static SearchResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebSearcher : IDisposable, IWebSearcher, IWebSearcherInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(WebSearcherModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsImageSearching { get; }
    void Dispose()
    static WebSearcherCapabilities GetCapabilities(WebSearcherModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebSearcherModel model)
    Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
    Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
  sealed class WebSearcherCapabilities : IWebSearcherInfo
    ctor()
    bool SupportsImageSearching { get; init; }
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
    static string DisplayName(WebSearcherModel model)
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
    Context ClientContext
    // The deserialized action payload.
    T Value
  // Accumulates profiling samples over multiple render passes, providing aggregate statistics (avg, min, max, p95, p99).
  sealed class ProfileHistory
    // Creates a new history buffer that retains the last maxSamples render sessions.
    ctor(int maxSamples)
    // Ordered list of distinct measurement names seen across all recorded sessions.
    IReadOnlyList<string> Names { get; }
    // Total number of render sessions recorded (including those evicted from the rolling window).
    long SampleCount { get; }
    // Returns aggregate statistics for a specific named measurement.
    ProfileStats GetStats(string name)
    // Returns a multi-line summary with aggregate stats for total time and each measurement.
    string GetSummary()
    // Returns aggregate statistics for total render time across all sampled sessions.
    ProfileStats GetTotalStats()
    // Clears all accumulated samples and resets the sample count.
    void Reset()
  // Disposable timing scope that records elapsed time into the current ProfileSession when disposed.
  struct ProfileScope : IDisposable
    // Records the elapsed time into the profiling session.
    void Dispose()
  // Records named timing measurements for a single UI render pass.
  sealed class ProfileSession
    ctor()
    // Ordered list of measurement names recorded in this session.
    IReadOnlyList<string> Names { get; }
    // All recorded timings keyed by measurement name.
    IReadOnlyDictionary<string, double> Timings { get; }
    // Total elapsed time for this session in milliseconds.
    double TotalMs { get; }
    // Returns a formatted string with total time and per-measurement breakdown.
    string GetBreakdown()
    // Returns the recorded timing for the given measurement name, or 0 if not found.
    double GetTiming(string name)
    // Stops the session timer and logs the timing breakdown.
    void LogResults()
  // Aggregate profiling statistics for a named measurement or total render time.
  struct ProfileStats : IEquatable<ProfileStats>
    // Aggregate profiling statistics for a named measurement or total render time.
    ctor(double Avg, double Min, double Max, double Median, double P95, double P99)
    double Avg { get; init; }
    double Max { get; init; }
    double Median { get; init; }
    double Min { get; init; }
    double P95 { get; init; }
    double P99 { get; init; }
  // Provides UI render profiling with per-frame timing breakdowns and optional historical statistics.
  static class Profiler
    // Current profiling session for this async context, or null if not profiling.
    static ProfileSession? Current { get; }
    // Historical profiling data, or null if history is not enabled.
    static ProfileHistory? History { get; }
    // Whether history recording is currently paused.
    static bool IsHistoryPaused { get; }
    // Disables profiling history collection and discards accumulated data.
    static void DisableHistory()
    // Enables profiling history collection, keeping up to maxSamples render sessions.
    static void EnableHistory(int maxSamples = 1000)
    // Starts a named timing measurement within the current profiling session. Dispose the returned scope to record the elapsed time.
    static ProfileScope Measure(string name)
    // Pauses history recording. New render sessions are not recorded until ResumeHistory is called.
    static void PauseHistory()
    // Clears all accumulated profiling history samples.
    static void ResetHistory()
    // Resumes history recording after a pause.
    static void ResumeHistory()
  // Main entry point for the Ikon Parallax reactive UI system. Manages client connections, render cycles, style distribution, and action handling for server-driven UI.
  class UI
    // Creates a new UI instance bound to the given app and theme.
    ctor(IAppBase app, ITheme theme)
    // When true, each render cycle logs timing breakdowns. See Profiler for history.
    bool EnableProfiling { get; set; }
    // When true, caches subtrees with unchanged reactive dependencies to skip redundant re-renders.
    bool EnableSubtreeCaching { get; set; }
    // Assigns a CSS block to a single client (e.g. a per-tenant theme overlay). Subsequent calls for the same client replace the previous assignment and queue a delete for the prior styleId on that client. Other clients are unaffected.
    string AddClientCss(int clientId, string css)
    // Adds a global CSS block that is sent to all connected clients. Idempotent: identical CSS returns the same style ID.
    string AddGlobalCss(string css)
    // Drops the per-client CSS assignment for the given client. Use on disconnect.
    void RemoveClientCss(int clientId)
    // Defines the root UI view tree. Call this in a reactive context to re-render when dependencies change.
    void Root(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
  // Represents a UI view scope for building the component tree. Extension methods on this type provide the component API (e.g. Text, Button, Input).
  class UIView
    // The default icon library name used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }
    // Adds a child node with the given type and props. The props parameter is the non-generic IDictionary on purpose: it's the ONLY type that cleanly accepts BOTH a `Dictionary<string, object>` (the natural non-null shape a model builds) AND a `Dictionary<string, object?>` (props that carry null values) with no nullability warning and no suppression. A generic `Dictionary<string, object?>` param warns CS8620 on the non-null form (identity-modulo-nullability), and no PAIR of generic overloads works either — nullability annotations are erased for overload resolution, so two such overloads are CS0111 (same signature) or CS0121 (ambiguous).
    void AddNode(string type, IDictionary? props = null, List<UIViewNode>? children = null, string? key = null, string[]? style = null, string? styleId = null, string file = "", int line = 0)
    string? CreateAction<T>(Func<ActionArgs<T>, Task>? callback)
    // Registers binary data as a payload and returns a reference string for use as an image src.
    string RegisterPayload(byte[] data, string mimeType)
  // Represents a single node in the UI view tree, with identity, props, children, and style information.
  sealed class UIViewNode
    // Creates a new view node with the given type, props, and optional children.
    ctor(string type, Guid viewId, Dictionary<string, object?> props, List<UIViewNode>? children = null, string? key = null, IReadOnlyList<string>? styleIds = null, ulong parentPathHash = 0, IReadOnlyCollection<IReadOnlyList<string>>? styleIdProps = null, string file = "", int line = 0)
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
  class AxisConfig
    ctor()
    // Format string for tick labels. For time scales, use d3-time-format tokens (e.g. "%H:%M", "%m/%d %H:%M").
    string? Format { get; set; }
    string? Legend { get; set; }
    int? LegendOffset { get; set; }
    int? TickPadding { get; set; }
    int? TickRotation { get; set; }
    int? TickSize { get; set; }
    // Number of ticks to display. When set, the axis will show approximately this many evenly-spaced ticks instead of one per data point.
    int? TickValues { get; set; }
    // Truncate tick label text at this character length.
    int? TruncateTickAt { get; set; }
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
  class ChartClickArgs
    ctor()
    string? Id { get; set; }
    string? IndexValue { get; set; }
    string? SerieId { get; set; }
    object? Value { get; set; }
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
    // Renders an interactive bar chart with configurable grouping, layout, axes, and theming.
    static void BarChart(UIView view, string[]? style = null, IEnumerable<Dictionary<string, object>>? data = null, IEnumerable<string>? keys = null, string? indexBy = null, BarGroupMode? groupMode = null, BarLayout? layout = null, ScaleType? valueScale = null, ScaleType? indexScale = null, bool? reverse = null, double? minValue = null, double? maxValue = null, double? padding = null, double? innerPadding = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enableLabel = null, int? labelSkipWidth = null, int? labelSkipHeight = null, string? labelTextColor = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderRadius = null, double? borderWidth = null, string? valueFormat = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null, string file = "", int line = 0)
    // Renders an interactive line chart with configurable curves, points, areas, and crosshairs.
    static void LineChart(UIView view, string[]? style = null, IEnumerable<LineChartSeries>? data = null, ScaleType? xScaleType = null, ScaleType? yScaleType = null, double? xScaleMin = null, double? xScaleMax = null, double? yScaleMin = null, double? yScaleMax = null, bool? yScaleStacked = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enablePoints = null, int? pointSize = null, string? pointColor = null, string? pointBorderColor = null, int? pointBorderWidth = null, bool? enableArea = null, double? areaOpacity = null, double? areaBaselineValue = null, bool? enableCrosshair = null, CrosshairType? crosshairType = null, LineCurve? curve = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, double? lineWidth = null, bool? isInteractive = null, bool? useMesh = null, bool? enableSlices = null, string? xFormat = null, string? yFormat = null, string? gradientFromColor = null, string? gradientToColor = null, IEnumerable<double>? gridXValues = null, IEnumerable<double>? gridYValues = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null, string file = "", int line = 0)
    // Renders an interactive pie/donut chart with configurable arc labels, link labels, and legends.
    static void PieChart(UIView view, string[]? style = null, IEnumerable<PieChartDatum>? data = null, double? innerRadius = null, double? padAngle = null, double? cornerRadius = null, double? startAngle = null, double? endAngle = null, bool? sortByValue = null, ChartMargin? margin = null, bool? enableArcLabels = null, string? arcLabelsTextColor = null, double? arcLabelsSkipAngle = null, bool? enableArcLinkLabels = null, string? arcLinkLabelsTextColor = null, double? arcLinkLabelsSkipAngle = null, double? arcLinkLabelsThickness = null, string? arcLinkLabelsColor = null, double? activeOuterRadiusOffset = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderWidth = null, string? valueFormat = null, bool? arcLabelAsPercentage = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null, string file = "", int line = 0)
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
  class ChartMargin
    ctor()
    int? Bottom { get; set; }
    int? Left { get; set; }
    int? Right { get; set; }
    int? Top { get; set; }
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
  // Configuration for a chart legend including positioning, layout direction, and item sizing.
  class LegendConfig
    ctor()
    string? Anchor { get; set; }
    string? Direction { get; set; }
    int? ItemHeight { get; set; }
    int? ItemWidth { get; set; }
    int? ItemsSpacing { get; set; }
    int? SymbolSize { get; set; }
    int? TranslateX { get; set; }
    int? TranslateY { get; set; }
  // A single data point in a line chart series.
  class LineChartPoint
    object X { get; set; }
    object Y { get; set; }
  // A named data series for a line chart, containing an ordered collection of points.
  class LineChartSeries
    string? Color { get; set; }
    IEnumerable<LineChartPoint>? Data { get; set; }
    string Id { get; set; }
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
  class PieChartDatum
    string? Color { get; set; }
    string Id { get; set; }
    string? Label { get; set; }
    double Value { get; set; }
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
    // Cell type: "text", "badge", "action", "actions", or "checkbox".
    string Type { get; init; }
    // Display value or checkbox state ("true"/"false").
    string? Value { get; init; }
    // Visual variant for badge cells.
    string? Variant { get; init; }
    // Creates an action button cell.
    static Cell Action(string label, string actionId, string[]? style = null)
    // Creates a cell containing multiple action buttons.
    static Cell ActionGroup(CellAction[] actions)
    // Creates a badge cell with an optional variant.
    static Cell Badge(string value, string? variant = null, string[]? style = null)
    // Creates a checkbox cell.
    static Cell Checkbox(bool checked, string actionId, string[]? style = null, bool disabled = false)
    // Creates a text cell.
    static Cell Text(string? value, string[]? style = null)
  // An action button that can be displayed within a data table cell.
  class CellAction : IEquatable<CellAction>
    // An action button that can be displayed within a data table cell.
    ctor(string Label, string ActionId, string[]? Style = null, string? Icon = null)
    string ActionId { get; init; }
    string? Icon { get; init; }
    string Label { get; init; }
    string[]? Style { get; init; }
  // Defines a column in a data table including header text, width, and alignment.
  class DataTableColumn : IEquatable<DataTableColumn>
    // Defines a column in a data table including header text, width, and alignment.
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
    static void DataTable(UIView view, DataTableColumn[] columns, DataTableRow[] rows, int totalCount, int pageIndex, int pageSize, Func<int, Task>? onPageChange = null, Func<string, Task>? onRowClick = null, Func<string, Task>? onActionClick = null, Action<UIView>? emptyContent = null, int[]? columnWidths = null, Func<string, Task>? onColumnResize = null, string[]? style = null, string[]? headerStyle = null, string[]? rowStyle = null, string[]? cellStyle = null, string[]? headerCellStyle = null, string[]? dataCellStyle = null, string[]? paginationStyle = null, string[]? paginationButtonStyle = null, string[]? pageNumberStyle = null, string[]? pageNumberActiveStyle = null, string[]? emptyStyle = null, string[]? actionButtonStyle = null, string[]? resizeHandleStyle = null, string[]? tooltipStyle = null, string? prevLabel = null, string? nextLabel = null, string? pageLabel = null, string? key = null, string file = "", int line = 0)
  // A single row in a data table, identified by a unique ID and containing an array of cells.
  class DataTableRow : IEquatable<DataTableRow>
    // A single row in a data table, identified by a unique ID and containing an array of cells.
    ctor(string Id, Cell[] Cells)
    Cell[] Cells { get; init; }
    string Id { get; init; }

namespace Ikon.Parallax.Components.ImageEditor
  // Extension methods for the image editor canvas component.
  static class ImageEditorExtensions
    // Canvas for editing images with brush and eraser tools.
    static void ImageEditorCanvas(UIView view, string[]? style = null, string? src = null, int? brushWidth = null, string? brushColor = null, string? tool = null, double? zoom = null, int? textMaxLength = null, int? textFontSize = null, int? textPadding = null, Func<ImageEditorSaveArgs, Task>? onSave = null, Func<ImageEditorHistoryArgs, Task>? onHistoryChange = null, int? triggerSave = null, int? triggerUndo = null, int? triggerRedo = null, string? styleId = null, string? key = null, string file = "", int line = 0)
  // Event args for when the undo/redo history state changes.
  sealed class ImageEditorHistoryArgs : IEquatable<ImageEditorHistoryArgs>
    // Event args for when the undo/redo history state changes.
    ctor(bool CanUndo, bool CanRedo)
    bool CanRedo { get; init; }
    bool CanUndo { get; init; }
  // Event args for when the image editor saves the edited image.
  sealed class ImageEditorSaveArgs : IEquatable<ImageEditorSaveArgs>
    // Event args for when the image editor saves the edited image.
    ctor(string ImageData)
    string ImageData { get; init; }

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
    // Helper class for accessing Rive event properties with type-safe methods.
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
    static void RiveCanvas(UIView view, string[]? style = null, string? source = null, IEnumerable<string>? stateMachines = null, RiveViewModel? viewModel = null, IEnumerable<RiveTrigger>? triggers = null, Func<RiveEventData, Task>? onEvent = null, RiveFit? layoutFit = null, RiveAlignment? layoutAlignment = null, bool? autoplay = null, bool? useOffscreenRenderer = null, bool? autoBind = null, bool? enableMultiTouch = null, bool? dispatchPointerExit = null, bool? isTouchScrollEnabled = null, bool? shouldDisableRiveListeners = null, IEnumerable<RiveKeyboardBinding>? keyboardBindings = null, string? backgroundColor = null, string? width = null, string? height = null, string? styleId = null, string? key = null, string file = "", int line = 0)
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
    // Represents a trigger that can be fired in a Rive animation. Calling Fire() increments the sequence and triggers a UI re-render.
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
    static void AccessibleIcon(UIView view, string[]? style = null, string? label = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Hides content visually while keeping it accessible to screen readers.
    static void VisuallyHidden(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Base event returned from a client-side action, indicating the action type and whether it succeeded.
  class ActionEvent : IEquatable<ActionEvent>
    // Base event returned from a client-side action, indicating the action type and whether it succeeded.
    ctor(string ActionType, bool Success)
    string ActionType { get; init; }
    bool Success { get; init; }
  // JSON converter that deserializes ActionEvent into the correct derived type based on the ActionType field.
  class ActionEventConverter : JsonConverter<ActionEvent>
    ctor()
    override ActionEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    override void Write(Utf8JsonWriter writer, ActionEvent value, JsonSerializerOptions options)
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
    ShowNotification
  // Base class for client-side action configuration.
  abstract class ActionOptions : IEquatable<ActionOptions>
  // Represents activation mode for Tabs.
  enum ActivationMode
    Automatic
    Manual
  // Represents alignment for overlay positioning.
  enum Align
    Start
    Center
    End
  // Extension methods for Calendar and DatePicker components.
  static class CalendarExtensions
    // Month-grid date selector. Renders a single month with day cells. Dates are ISO yyyy-MM-dd strings.
    static void Calendar(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? month = null, string? defaultMonth = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, string? locale = null, bool? disabled = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onMonthChange = null, string file = "", int line = 0)
    // Button that opens a popover containing a Calendar .
    static void DatePicker(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, string? format = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? calendarStyle = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool?, Task>? onOpenChange = null, string? label = null, string file = "", int line = 0)
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
  // Alignment of slides relative to the carousel viewport.
  enum CarouselAlign
    Start
    Center
    End
  // Responsive carousel configuration applied above a container-width threshold.
  sealed class CarouselBreakpoint : IEquatable<CarouselBreakpoint>
    // Responsive carousel configuration applied above a container-width threshold.
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
    static void Carousel(UIView view, string[]? style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, int? slidesPerView = null, int? slidesPerGroup = null, int? slideGapPx = null, IEnumerable<CarouselBreakpoint>? breakpoints = null, IEnumerable<CarouselSlideItem>? slides = null, bool? showArrows = null, bool? showIndicators = null, string? previousLabel = null, string? nextLabel = null, string? previousIconName = null, string? nextIconName = null, string[]? rootStyle = null, string[]? viewportStyle = null, string[]? slideStyle = null, string[]? previousStyle = null, string[]? nextStyle = null, string[]? indicatorsStyle = null, string[]? indicatorStyle = null, string[]? indicatorActiveStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<double, Task>? onIndexChange = null, string file = "", int line = 0)
    // A single slide inside a Carousel . Use when rendering slides manually.
    static void Slide(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Declarative slide definition for Carousel .
  sealed class CarouselSlideItem : IEquatable<CarouselSlideItem>
    // Declarative slide definition for Carousel .
    ctor(Action<UIView> Content, string? Key = null)
    // Builder function for rendering the slide.
    Action<UIView> Content { get; init; }
    // Optional stable key used for diffing.
    string? Key { get; init; }
  // Extension methods for the ChatLog primitive — the canonical chat-bubble layout shape: header + scrolling auto-scrolled body + composer. Wraps ScrollColumn with chat-friendly defaults so callers don't have to remember to set autoScroll: true.
  static class ChatLogExtensions
    // Renders a chat-style scrolling region: an optional pinned header (e.g. "Conversation"), a scrollable body that auto-scrolls to the bottom on change, and an optional pinned footer (typically the input row).
    static void ChatLog(UIView view, int messageCount, string[]? style = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, string? styleId = null, string? key = null, string file = "", int line = 0)
  // Represents the checked state for checkbox-like components.
  enum CheckedState
    Unchecked
    Checked
    Indeterminate
  // Extension methods for the CodeEditor component.
  static class CodeEditorExtensions
    // Monospace code editor with an optional line-number gutter.
    static void CodeEditor(UIView view, string? value = null, string? defaultValue = null, string? language = null, string? placeholder = null, bool? readOnly = null, bool? disabled = null, bool? showLineNumbers = null, int? tabSize = null, bool? insertSpaces = null, bool? wrap = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? gutterStyle = null, string[]? contentStyle = null, string[]? languageBadgeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, string file = "", int line = 0)
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
    static void ColorPicker(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, ColorFormat format = Hex, bool? showAlpha = null, IReadOnlyList<string>? presets = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onValueCommit = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
  // Horizontal alignment for a content grid or data table column.
  enum ColumnAlign
    Left
    Center
    Right
  // Event returned from a contact picker action with the selected contacts.
  sealed class ContactsActionEvent : ActionEvent, IEquatable<ContactsActionEvent>
    // Event returned from a contact picker action with the selected contacts.
    ctor(bool Success, IReadOnlyList<ClientContact>? Contacts)
    IReadOnlyList<ClientContact>? Contacts { get; init; }
  // Extension methods for container components.
  static class ContainerExtensions
    // Generic container element.
    static void Box(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClick = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Box — positional (style, children) overload. Models reach for view.Box([style], v => {...}) with the lambda as the 2nd positional; without this overload it tries to bind to styleId (string?) and trips CS1660. The lambda parameter is named children (not content) so existing callers that use content: by name unambiguously match the original.
    static void Box(UIView view, string[]? style, Action<UIView> children, string file = "", int line = 0)
    // Container with vertical flexbox layout (flex-col).
    static void Column(UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Column — positional (style, children) overload.
    static void Column(UIView view, string[]? style, Action<UIView> children, string file = "", int line = 0)
    // Container with flexbox layout enabled.
    static void Flex(UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Container with CSS grid layout enabled.
    static void Grid(UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Grid — positional (style, children) overload.
    static void Grid(UIView view, string[]? style, Action<UIView> children, string file = "", int line = 0)
    // Absolutely positioned layer within a Stack container.
    static void Layer(UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Container with horizontal flexbox layout (flex-row).
    static void Row(UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Row — positional (style, children) overload (see Box ).
    static void Row(UIView view, string[]? style, Action<UIView> children, string file = "", int line = 0)
    // Loading spinner — an animated circular indicator for async/pending states. A typed convenience over the spin utility classes (equivalent to a div with the Default.Icon.Spinner style): render it while waiting on data, e.g. if (_loading.Value) { view.Spinner(); }. Override colour/size via the style array; the default tracks the theme's muted foreground.
    static void Spinner(UIView view, string[]? style = null, SpinnerSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Container for layering children on top of each other. Use with Layer components as children.
    static void Stack(UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Defines a column in a content grid including optional header, width, flex, and alignment.
  class ContentGridColumn : IEquatable<ContentGridColumn>
    // Defines a column in a content grid including optional header, width, flex, and alignment.
    ctor(string? Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string? Header { get; init; }
    string? Width { get; init; }
  // Extension methods for CSS grid-based content layout.
  static class ContentGridExtensions
    // Renders a CSS grid layout with configurable columns, optional headers, and child content.
    static void ContentGrid(UIView view, ContentGridColumn[] columns, Action<UIView>? content = null, string[]? style = null, string[]? headerStyle = null, string? key = null, string file = "", int line = 0)
  // Options for copying text to the clipboard.
  sealed class CopyToClipboardActionOptions : ActionOptions, IEquatable<CopyToClipboardActionOptions>
    // The text to copy.
    string Text { get; init; }
  // Extension methods for core UI components including buttons, toggles, text inputs, dialogs, and typography.
  static class CoreExtensions
    // Button that triggers a client-side action (e.g., clipboard, download). Supports both text mode and icon mode. In text mode (content is null or label is null), label is displayed as visible text. In icon mode (content and label are both provided), label becomes the accessible aria-label and content is displayed.
    static void ActionButton(UIView view, string[]? style = null, ActionKind action = Unknown, string? label = null, ActionOptions? options = null, bool? disabled = null, string? className = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<ActionEvent, Task>? onActionComplete = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Clickable button that triggers an action. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    static void Button(UIView view, string[]? style = null, string? text = null, string? label = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, string? iconPosition = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Button — positional-text-first overload. Same rationale as the matching Text overload — avoids CS1744 when models write view.Button("Sign in", onClick: …). First parameter is named buttonText to avoid ambiguity with callers using Button(text: "...") by name.
    static void Button(UIView view, string buttonText, string[]? style = null, string? label = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, string? iconPosition = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Semantic heading element for titles and section headers.
    static void Heading(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Renders an icon from an icon library.
    static void Icon(UIView view, string[]? style = null, string? name = null, string? library = null, string? className = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Inline anchor link — sugar for a `Button` styled like a hyperlink with an `href`. Mirrors HTML anchor semantics. By default opens in the same tab; pass target: "_blank" to open in a new tab (we automatically add `rel="noopener noreferrer"` for `_blank` if no other `rel` is provided). Generated code naturally reaches for `view.Link(text:, href:)`; this gives it the canonical shape rather than forcing every link into `view.Button(href:, …)`.
    static void Link(UIView view, string[]? style = null, string? text = null, string? label = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, string? iconPosition = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Renders markdown content with formatting support.
    static void Markdown(UIView view, string[]? style = null, string? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Text element for displaying content.
    static void Text(UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Text element — positional-text-first overload. Models trained on shadcn / Radix / React conventions reach for view.Text("Hello", style: ["text-xl"]) rather than the view.Text(["text-xl"], "Hello") ordering. Without this overload, the positional string argument fails to bind to the original signature's first parameter (string[]? style), producing CS1744 / CS1503 — the most common compile error in the codegen benchmark. Parameter is named textContent (not text) to avoid ambiguity with existing callers that use Text(text: "...") by name.
    static void Text(UIView view, string textContent, string[]? style = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Single toggle button.
    static void Toggle(UIView view, string[]? style = null, bool? pressed = null, bool? defaultPressed = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onPressedChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Item within a toggle group.
    static void ToggleGroupItem(UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Toggle group with multiple selection.
    static void ToggleGroupMultiple(UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Toggle group with single selection.
    static void ToggleGroupSingle(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Represents the text direction for DirectionProvider.
  enum Dir
    Ltr
    Rtl
  // Extension methods for Accordion and Collapsible components.
  static class DisclosureExtensions
    // Content for an accordion item, collapsed or expanded.
    static void AccordionContent(UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Wraps an AccordionTrigger.
    static void AccordionHeader(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Container for an accordion item.
    static void AccordionItem(UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Accordion with multiple items open at a time.
    static void AccordionMultiple(UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Accordion with single item open at a time.
    static void AccordionSingle(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? collapsible = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Toggles the collapsed state of an accordion item.
    static void AccordionTrigger(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Expandable/collapsible container.
    static void Collapsible(UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Content that is shown or hidden.
    static void CollapsibleContent(UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Toggles the collapsed state.
    static void CollapsibleTrigger(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
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
    static void DndContext(UIView view, string[]? style = null, CollisionDetection collisionDetection = ClosestCenter, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<DragStartArgs, Task>? onDragStart = null, Func<DragMoveArgs, Task>? onDragMove = null, Func<DragOverArgs, Task>? onDragOver = null, Func<DragEndArgs, Task>? onDragEnd = null, Func<Task>? onDragCancel = null, int? activationDistance = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Overlay shown while dragging.
    static void DragOverlay(UIView view, string[]? style = null, bool? dropAnimation = true, string? activeDragId = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Element that can be dragged.
    static void Draggable(UIView view, string[]? style = null, string? id = null, bool? disabled = null, bool? hideOnDrag = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Drop target area.
    static void Droppable(UIView view, string[]? style = null, string? id = null, bool? disabled = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Context for sortable list operations.
    static void SortableContext(UIView view, string[]? style = null, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Drag handle for a SortableItem. When a SortableHandle descendant is present, only pointerdown on the handle starts a drag; the rest of the item remains free for inner clickable elements like buttons. Place inside a SortableItem (or a SortableList itemContent). Outside a SortableItem the handle renders as a plain container.
    static void SortableHandle(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Sortable item within a SortableContext.
    static void SortableItem(UIView view, string[]? style = null, string? id = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // SortableList component that auto-handles reordering.
    static void SortableList(UIView view, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, CollisionDetection collisionDetection = ClosestCenter, Func<SortableReorderArgs, Task>? onReorder = null, Func<DragStartArgs, Task>? onDragStart = null, Action<UIView, string>? itemContent = null, string[]? listStyle = null, string[]? itemStyle = null, int? activationDistance = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
  // Event args for drag cancel in @dnd-kit.
  sealed class DragCancelArgs : IEquatable<DragCancelArgs>
    // Event args for drag cancel in @dnd-kit.
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for drag end in @dnd-kit.
  sealed class DragEndArgs : IEquatable<DragEndArgs>
    // Event args for drag end in @dnd-kit.
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag move in @dnd-kit.
  sealed class DragMoveArgs : IEquatable<DragMoveArgs>
    // Event args for drag move in @dnd-kit.
    ctor(string ActiveId, double DeltaX, double DeltaY)
    string ActiveId { get; init; }
    double DeltaX { get; init; }
    double DeltaY { get; init; }
  // Event args for drag over in @dnd-kit.
  sealed class DragOverArgs : IEquatable<DragOverArgs>
    // Event args for drag over in @dnd-kit.
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag start in @dnd-kit.
  sealed class DragStartArgs : IEquatable<DragStartArgs>
    // Event args for drag start in @dnd-kit.
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for escape key down events on overlays.
  sealed class EscapeKeyDownArgs : IEquatable<EscapeKeyDownArgs>
    // Event args for escape key down events on overlays.
    ctor()
  // Hint used by FeedSlide to preload the slide's primary media asset.
  enum FeedMediaKind
    None
    Image
    Video
    VideoFull
  // Extension methods for the FeedScroller component — a vertically-snapping, full-viewport feed optimized for media-heavy content (TikTok / Reels / Shorts-style).
  static class FeedScrollerExtensions
    // Renders a TikTok-style vertical feed: each slide occupies the viewport and snaps into place.
    static void FeedScroller(UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[]? style = null, string[]? slideStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onActiveChange = null, Func<double, Task>? onScrollNearEnd = null, Func<bool, Task>? onMuteChange = null, string file = "", int line = 0)
    // A single slide inside a FeedScroller . Use when rendering slides manually rather than via the FeedSlide declarative API.
    static void FeedSlide(UIView view, int index, string[]? style = null, FeedMediaKind mediaKind = None, string? mediaUrl = null, string? mediaPoster = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
  // A single slide in a FeedScroller .
  sealed class FeedSlide : IEquatable<FeedSlide>
    // A single slide in a FeedScroller .
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
    static void FilePicker(UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FilePickerSelectedArgs, Task>? onFileSelected = null, Func<FilePickerValidationErrorArgs, Task>? onValidationError = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Metadata for a file chosen in a FilePicker . The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
  sealed class FilePickerSelectedArgs : IEquatable<FilePickerSelectedArgs>
    // Metadata for a file chosen in a FilePicker . The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  // Reported when client-side validation rejects a picked file (e.g. file too large for maxFileSize). Host UIs should surface Reason to the user — without a handler the rejection is silent and the user just sees "nothing happened" after clicking the picker.
  sealed class FilePickerValidationErrorArgs : IEquatable<FilePickerValidationErrorArgs>
    // Reported when client-side validation rejects a picked file (e.g. file too large for maxFileSize). Host UIs should surface Reason to the user — without a handler the rejection is silent and the user just sees "nothing happened" after clicking the picker.
    ctor(string FileName, string MimeType, long Size, string Reason)
    string FileName { get; init; }
    string MimeType { get; init; }
    string Reason { get; init; }
    long Size { get; init; }
  // Extension methods for file upload components.
  static class FileUploadExtensions
    // File upload component with explicit upload area, button click, drag-drop, and paste support.
    static void FileUpload(UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string? capture = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FileUploadPreStartArgs, Task<FileUploadPreStartResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadStartResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, string[]? seedSelectionIds = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Wrapper component that adds file upload capability (drag-drop + paste) to any content. Children define the visual appearance.
    static void FileUploadZone(UIView view, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, Func<FileUploadPreStartArgs, Task<FileUploadPreStartResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadStartResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, Func<bool, Task>? onDragActiveChange = null, Action<UIView>? content = null, string[]? zoneStyle = null, string[]? activeStyle = null, string? activeStyleId = null, string? styleId = null, string? key = null, string[]? seedSelectionIds = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
  // Extension methods for focus hint management.
  static class FocusHintExtensions
    // Requests focus attention for a UI element, typically for accessibility announcements.
    static void FocusHint(UIView view, FocusHintProps props, string? key = null, Guid? targetViewId = null)
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
    // Event args for focus outside events on overlays.
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Priority level for focus hint announcements, matching ARIA live region politeness.
  enum FocusPriority
    Polite
    Assertive
  // Extension methods for Form, Checkbox, RadioGroup, Switch, Slider, and Label components.
  static class FormExtensions
    // Checkbox control with simple boolean state. For tri-state support (indeterminate), use the CheckedState overload.
    static void Checkbox(UIView view, string[]? style = null, bool? isChecked = null, bool? defaultChecked = null, bool? required = null, bool? disabled = null, string? name = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onCheckedChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Checkbox control with tri-state support (checked, unchecked, indeterminate).
    static void Checkbox(UIView view, string[]? style = null, CheckedState? checkedState = null, CheckedState? defaultCheckedState = null, bool? required = null, bool? disabled = null, string? name = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onCheckedStateChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Visual indicator for the checkbox state.
    static void CheckboxIndicator(UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Form container with validation support.
    static void Form(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClearServerErrors = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Wraps the input control.
    static void FormControl(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Container for a form field with label and validation.
    static void FormField(UIView view, string[]? style = null, string? name = null, bool? serverInvalid = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Label for a form field.
    static void FormLabel(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Validation message for a form field.
    static void FormMessage(UIView view, string[]? style = null, FormMessageMatch? match = null, bool? forceMatch = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Submit button for the form.
    static void FormSubmit(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Accessible label for form controls.
    static void Label(UIView view, string[]? style = null, string? htmlFor = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Container for radio buttons.
    static void RadioGroup(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Visual indicator for the selected radio.
    static void RadioGroupIndicator(UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Individual radio button.
    static void RadioGroupItem(UIView view, string[]? style = null, string? value = null, bool? disabled = null, bool? required = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Range slider control.
    static void Slider(UIView view, string[]? style = null, IReadOnlyList<double>? value = null, IReadOnlyList<double>? defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<double>, Task>? onValueChange = null, Func<IReadOnlyList<double>, Task>? onValueCommit = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Filled range portion of the slider.
    static void SliderRange(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Draggable thumb on the slider.
    static void SliderThumb(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Track for the slider.
    static void SliderTrack(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Toggle switch control.
    static void Switch(UIView view, string[]? style = null, bool? isChecked = null, bool? defaultChecked = null, bool? required = null, bool? disabled = null, string? name = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onCheckedChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // The thumb that moves when the switch is toggled.
    static void SwitchThumb(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
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
    // Event returned from an image capture action with the captured image data.
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string? Data { get; init; }
    int Height { get; init; }
    string? Mime { get; init; }
    int Width { get; init; }
  // Extension methods for image and avatar components.
  static class ImageExtensions
    // Avatar container with image and fallback.
    static void Avatar(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Fallback content shown when image fails to load.
    static void AvatarFallback(UIView view, string[]? style = null, int? delayMs = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Image element for the avatar.
    static void AvatarImage(UIView view, string[]? style = null, string? src = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onLoadingStatusChange = null, string file = "", int line = 0)
    // Image element.
    static void Image(UIView view, string[]? style = null, string? src = null, AssetUri? assetUri = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClick = null, string file = "", int line = 0)
    // Image element with binary data payload.
    static void Image(UIView view, string[]? style = null, byte[]? data = null, string? mimeType = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClick = null, string file = "", int line = 0)
  // Extension methods for input components (TextField, TextArea, OTP, Password).
  static class InputExtensions
    // One-time password input field.
    static void OtpField(UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string? label = null, string file = "", int line = 0)
    // Individual input slot for OTP.
    static void OtpFieldInput(UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Password input with visibility toggle.
    static void PasswordToggleField(UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string? label = null, string file = "", int line = 0)
    // Icon that changes based on visibility state.
    static void PasswordToggleFieldIcon(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null, string file = "", int line = 0)
    // The password input element.
    static void PasswordToggleFieldInput(UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Button to toggle password visibility.
    static void PasswordToggleFieldToggle(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Two-way bind a TextArea to a Reactive`1 in one call. Same shape as the TextField bind overload.
    static void TextArea(UIView view, Reactive<string> bind, string[]? style = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, string file = "", int line = 0)
    // Multi-line text input area.
    static void TextArea(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, string file = "", int line = 0)
    // Two-way bind a TextField to a Reactive`1 in one call — reads bind.Value for the controlled value and writes bind.Value = v on every keystroke. Use this instead of pairing value: bind.Value with a manual onValueChange.
    static void TextField(UIView view, Reactive<string> bind, string[]? style = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, bool? multiline = null, int? rows = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, string file = "", int line = 0)
    // Single-line text input field.
    static void TextField(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, string file = "", int line = 0)
  // Event args for interact outside events on overlays (combines pointer and focus).
  sealed class InteractOutsideArgs : IEquatable<InteractOutsideArgs>
    // Event args for interact outside events on overlays (combines pointer and focus).
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
    // Event args for keyboard events, matching the browser KeyboardEvent properties.
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
    static void KeyboardListener(UIView view, Func<KeyboardEventArgs, Task>? onKeyDown = null, Func<KeyboardEventArgs, Task>? onKeyUp = null, IReadOnlyList<string>? keys = null, bool? global = true, bool? preventDefault = null, bool? stopPropagation = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Extension methods for scroll area and layout components.
  static class LayoutExtensions
    // Maintains a specific aspect ratio for content.
    static void AspectRatio(UIView view, string[]? style = null, double ratio = 1, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Provides text direction context (ltr/rtl) to descendants.
    static void DirectionProvider(UIView view, string[]? style = null, Dir dir = Ltr, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Infinite scroll view that fires callbacks when user scrolls near the end.
    static void InfiniteScrollView(UIView view, string[]? style = null, int threshold = 200, int debounceMs = 100, bool loading = false, bool hasMore = true, ScrollDirection direction = Down, ScrollAreaScrollbars scrollbars = Vertical, Action<UIView>? loadingIndicator = null, Func<ScrollNearEndArgs, Task>? onNearEnd = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Progress component that auto-renders the indicator with transform.
    static void Progress(UIView view, string[]? style = null, double? value = null, double? max = null, string? variant = null, bool indeterminate = false, Func<double?, string>? getValueLabel = null, string[]? rootStyle = null, string[]? indicatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Resizable split panel with a drag handle between two panes. Resize is handled entirely on the client — only the final size is sent to the server via onResized .
    static void ResizableSplit(UIView view, Orientation orientation = Horizontal, double initialSize = 200, double minSize = 100, double maxSize = 500, bool reversed = false, Func<double, Task>? onResized = null, Action<UIView>? first = null, Action<UIView>? second = null, string[]? style = null, string[]? handleStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // ScrollArea component that auto-renders viewport and scrollbars.
    static void ScrollArea(UIView view, string[]? style = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType type = Hover, int? scrollHideDelay = null, Dir dir = Ltr, bool autoScroll = false, string? autoScrollKey = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? cornerStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Visual separator between content.
    static void Separator(UIView view, string[]? style = null, Orientation orientation = Horizontal, bool decorative = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
  // Event returned from a geolocation action with latitude/longitude coordinates.
  sealed class LocationActionEvent : ActionEvent, IEquatable<LocationActionEvent>
    // Event returned from a geolocation action with latitude/longitude coordinates.
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
    // Event data for media capture start/stop callbacks, containing the stream identifier and capture kind. ClientContext identifies the user who initiated the capture and is populated for all capture kinds (audio, camera, screen). Prefer reading ClientSessionId / UserId rather than tracking streamId-to-client mappings yourself.
    ctor(string StreamId, string Kind)
    // Client context of the user who initiated the capture.
    Context? ClientContext { get; init; }
    // Client session id of the user who initiated the capture.
    int? ClientSessionId { get; }
    string Kind { get; init; }
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
    static void AudioUrlPlayer(UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, string? preload = null, string? className = null, string? styleId = null, string? key = null, string file = "", int line = 0)
    // Button that captures media (audio, camera, or screen) based on the specified kind. Supports both text mode and icon mode. In text mode (content is null), label is displayed as visible text. In icon mode (content is provided), label becomes the accessible aria-label and content is displayed.
    static void CaptureButton(UIView view, string[]? style = null, MediaCaptureKind kind = Audio, string? label = null, MediaCaptureButtonMode captureMode = Hold, ClientAudioCaptureOptions? audioOptions = null, ClientVideoCaptureOptions? videoOptions = null, int? holdReleaseDelayMs = null, bool? disabled = null, string? className = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Push-to-talk microphone button: a CaptureButton(kind: Audio, mode: Hold) that integrates with SpeechRecognizedAsync . After enabling speech recognition once (Audio.UseSpeechRecognition(...)), subscribe to Audio.SpeechRecognizedAsync to receive transcriptions when the user releases the button. The user's client context is carried on the event args — no streamId-to-client plumbing needed in the app.
    static void PushToTalkButton(UIView view, string[]? style = null, string? label = "⏺", int holdReleaseDelayMs = 500, ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? className = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Canvas element for rendering a live video stream.
    static void VideoStreamCanvas(UIView view, string[]? style = null, string? streamId = null, int? width = null, int? height = null, string? className = null, string? styleId = null, string? key = null, string file = "", int line = 0)
    // Video player for URL-based video content.
    static void VideoUrlPlayer(UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, bool? playsInline = null, string? poster = null, int? width = null, int? height = null, string? className = null, string? styleId = null, string? key = null, string file = "", int line = 0)
  // Extension methods for NavigationMenu, Menubar, and Toolbar components.
  static class NavigationExtensions
    // Menubar root container.
    static void Menubar(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Checkbox item in menu.
    static void MenubarCheckboxItem(UIView view, string[]? style = null, CheckedState isChecked = Unchecked, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onCheckedChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Dropdown content for the menu.
    static void MenubarContent(UIView view, string[]? style = null, bool loop = true, Side side = Bottom, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Clickable menu item.
    static void MenubarItem(UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Visual indicator for checkbox/radio state.
    static void MenubarItemIndicator(UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Individual menu in the menubar.
    static void MenubarMenu(UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Radio group in menu.
    static void MenubarRadioGroup(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Radio item in menu.
    static void MenubarRadioItem(UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Separator between menu items.
    static void MenubarSeparator(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Submenu container.
    static void MenubarSub(UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Content for submenu.
    static void MenubarSubContent(UIView view, string[]? style = null, bool loop = true, Side side = Right, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Trigger for submenu.
    static void MenubarSubTrigger(UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Button that opens a menu.
    static void MenubarTrigger(UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Navigation menu root.
    static void NavigationMenu(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, int? delayDuration = null, int? skipDelayDuration = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Content shown when navigation item is active.
    static void NavigationMenuContent(UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Visual indicator for active navigation item.
    static void NavigationMenuIndicator(UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Individual navigation menu item.
    static void NavigationMenuItem(UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Link within navigation menu.
    static void NavigationMenuLink(UIView view, string[]? style = null, bool? active = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null, string file = "", int line = 0)
    // List of navigation menu items.
    static void NavigationMenuList(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Trigger that opens navigation content.
    static void NavigationMenuTrigger(UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Viewport for navigation menu content.
    static void NavigationMenuViewport(UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Toolbar container.
    static void Toolbar(UIView view, string[]? style = null, Orientation orientation = Horizontal, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Button in the toolbar.
    static void ToolbarButton(UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClick = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Link in the toolbar.
    static void ToolbarLink(UIView view, string[]? style = null, string? href = null, string? target = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Separator in the toolbar.
    static void ToolbarSeparator(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Multi-select toggle group in toolbar.
    static void ToolbarToggleGroupMultiple(UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Single-select toggle group in toolbar.
    static void ToolbarToggleGroupSingle(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Toggle item in toolbar toggle group.
    static void ToolbarToggleItem(UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Represents the orientation for components like Tabs, Slider, etc.
  enum Orientation
    Horizontal
    Vertical
  // Extension methods for overlay component child elements. For the main overlay components (Dialog, AlertDialog, Popover, Tooltip, HoverCard), use the simplified APIs in CoreExtensions.cs which handle Portal/Overlay management automatically.
  static class OverlayExtensions
    // Alert dialog that requires explicit user acknowledgment. Cannot be dismissed by clicking outside.
    static void AlertDialog(UIView view, bool? open = null, bool? defaultOpen = null, string? title = null, string? description = null, string? cancelLabel = null, string? actionLabel = null, Func<Task>? onAction = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? cancelStyle = null, string[]? actionStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Modal dialog window.
    static void Dialog(UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, string? title = null, string? description = null, string file = "", int line = 0)
    // Rich content card that appears on hover with configurable delays.
    static void HoverCard(UIView view, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
    // Floating content panel that appears next to a trigger element.
    static void Popover(UIView view, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
    // Toast notification with built-in provider and viewport.
    static void Toast(UIView view, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string? title = null, string? description = null, bool? showClose = null, string? closeLabel = null, Action<UIView>? content = null, string[]? toastStyle = null, string[]? viewportStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, Func<Task>? onEscapeKeyDown = null, Func<Task>? onPause = null, Func<Task>? onResume = null, Func<ToastSwipeArgs, Task>? onSwipeStart = null, Func<ToastSwipeArgs, Task>? onSwipeMove = null, Func<ToastSwipeArgs, Task>? onSwipeEnd = null, Func<ToastSwipeArgs, Task>? onSwipeCancel = null, string file = "", int line = 0)
    // Brief informational message that appears on hover. Includes built-in provider.
    static void Tooltip(UIView view, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
  // One page of items plus the controls needed to render prev/next buttons. Returned by Paginate``1 .
  sealed class Page<T> : IEquatable<Page<T>>
    // One page of items plus the controls needed to render prev/next buttons. Returned by Paginate``1 .
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
  // Bounded-cursor primitive on top of ClientReactive`1 . Slices an in-memory list, returns the slice + bound actions (Prev/Next/JumpTo/First/Last) the caller binds to whatever UI fits. Holds zero rendering opinion — no tab bars, no default control rows, no opinionated layout. Most Ikon apps don't need pagination at all (live feeds, autoscroll, virtualization handle the common cases via Reactive<List<T>> + ScrollArea(autoScroll: true)). Use this when you have a static list large enough to warrant explicit page navigation. For DB-backed pagination (load only the current page from a backend), drive ClientReactive`1 directly and observe its value in your data-loading code — same per-client semantics, no special helper needed.
  static class PaginationExtensions
    static Page<T> Paginate<T>(UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  // One row in the charges list.
  sealed class PaymentsChargeView : IEquatable<PaymentsChargeView>
    // One row in the charges list.
    ctor(string Id, string AmountLabel, string Status, DateTimeOffset Created, bool Paid, bool Refunded, string? PaymentIntentId, string? ReceiptUrl, string? Description = null)
    string AmountLabel { get; init; }
    DateTimeOffset Created { get; init; }
    string? Description { get; init; }
    string Id { get; init; }
    bool Paid { get; init; }
    string? PaymentIntentId { get; init; }
    string? ReceiptUrl { get; init; }
    bool Refunded { get; init; }
    string Status { get; init; }
  // Composed Parallax components for billing UIs — pricing tables, checkout actions, customer-portal entry points, payment-method and invoice lists, and subscription status. Pair with PaymentsService for end-to-end flows. All components are pure compositions of existing primitives (Box / Text / Button / Icon / Column / Row), so they participate in the standard theming, motion, and validation rules just like the rest of the Parallax surface.
  static class PaymentsExtensions
    // Vertical list of charge / receipt rows. Each row shows formatted amount, status, optional refund button (when onRefund is supplied and the charge is paid + non-refunded), and a "Receipt" link when present.
    static void ChargeList(UIView view, IReadOnlyList<PaymentsChargeView> charges, Func<string, Task>? onRefund = null, string[]? style = null, string? emptyText = null, string? key = null, string file = "", int line = 0)
    // Button that initiates a redirect-to-Stripe checkout. The onCheckout handler is expected to call PaymentsService.CreateCheckoutAsync(...) and return the session url; the component then opens the url in a new tab via ClientFunctions.OpenExternalUrlAsync. Returning null from the handler disables the redirect (e.g. for guest validation).
    static void CheckoutButton(UIView view, Func<Task<string?>> onCheckout, string? text = null, string[]? style = null, bool? disabled = null, string? icon = "credit-card", string? key = null, string file = "", int line = 0)
    // Vertical list of past invoices. Each row links to the hosted invoice url when present, and to the PDF when present.
    static void InvoiceList(UIView view, IReadOnlyList<PaymentsInvoiceView> invoices, string[]? style = null, string? emptyText = null, string? key = null, string file = "", int line = 0)
    // Vertical list of saved payment methods. Each row shows brand, last four, and expiry. Optional onDetach renders a remove action. Optional onAddCard renders a button at the bottom; typical handler creates a Stripe Checkout Session in setup mode and redirects.
    static void PaymentMethodList(UIView view, IReadOnlyList<PaymentsPaymentMethodView> methods, Func<string, Task>? onDetach = null, Func<Task>? onAddCard = null, string[]? style = null, string? emptyText = null, string? key = null, string file = "", int line = 0)
    // Renders a button that opens the Stripe-hosted Customer Portal in a new tab. The onOpenPortal handler is expected to call PaymentsService.CreatePortalAsync and return the portal url. Returning null suppresses the redirect.
    static void PaymentsPortalButton(UIView view, Func<Task<string?>>? onOpenPortal = null, string? text = null, string[]? style = null, bool? disabled = null, string? icon = "settings", string? key = null, string file = "", int line = 0)
    // Single pricing plan card with name, price, optional badge, feature bullet list and CTA. Use directly when laying plans out by hand, or via PricingTable for the common grid case.
    static void PlanCard(UIView view, PaymentsPlanView plan, Func<string, Task>? onSelect = null, string[]? style = null, string? key = null, string file = "", int line = 0)
    // Render a grid of pricing plan cards. Each card invokes onSelect with the plan's id when the CTA is pressed. The card whose Highlighted is true gets the brand-emphasis treatment (one card max).
    static void PricingTable(UIView view, IReadOnlyList<PaymentsPlanView> plans, Func<string, Task>? onSelect = null, string[]? style = null, int? columns = null, string? key = null, string file = "", int line = 0)
    // Renders a vertical list of SubscriptionStatus cards, one per subscription. Pass the same callback set you'd pass to a single SubscriptionStatus ; each callback receives the subscription id of the row that fired it.
    static void SubscriptionList(UIView view, IReadOnlyList<PaymentsSubscription> subscriptions, Func<PaymentsSubscription, PaymentsSubscriptionView>? projector = null, Func<string, Task>? onResume = null, Func<string, Task>? onCancel = null, Func<string, Task>? onCancelImmediate = null, Func<string, Task>? onPause = null, Func<string, Task>? onResumeFromPause = null, Action<UIView, PaymentsSubscription>? footer = null, string[]? style = null, string? emptyText = null, string? key = null, string file = "", int line = 0)
    // Compact subscription status card showing plan name, status pill and renewal/expiry date. Slot a PaymentsPortalButton in the footer to give the user a manage entry point.
    static void SubscriptionStatus(UIView view, PaymentsSubscriptionView subscription, string[]? style = null, Action<UIView>? footer = null, Func<Task>? onResume = null, Func<Task>? onCancel = null, Func<Task>? onCancelImmediate = null, Func<Task>? onPause = null, Func<Task>? onResumeFromPause = null, string? key = null, string file = "", int line = 0)
    // Grid of one-tap tip preset amounts. Each preset renders as a rounded button showing the currency-formatted amount; clicking invokes onTip with the chosen minor-unit amount. App handler typically passes the amount to PaymentsService.CreateTipCheckoutAsync and redirects.
    static void TipPresetGrid(UIView view, IReadOnlyList<long> presetsMinor, string currencySymbol, Func<long, Task> onTip, string[]? style = null, string? key = null, string file = "", int line = 0)
    // Display-only preview card for the next-billing-cycle invoice. Pair with PaymentsService.PreviewUpcomingInvoiceAsync: call before committing a plan change so the user sees "next bill = €X · €Y proration".
    static void UpcomingInvoicePreview(UIView view, PaymentsUpcomingInvoice preview, string[]? style = null, string? key = null, string file = "", int line = 0)
  // One row in the invoice / receipt list.
  sealed class PaymentsInvoiceView : IEquatable<PaymentsInvoiceView>
    // One row in the invoice / receipt list.
    ctor(string Id, DateTimeOffset Date, string AmountLabel, string Status, string? HostedUrl = null, string? PdfUrl = null)
    string AmountLabel { get; init; }
    DateTimeOffset Date { get; init; }
    string? HostedUrl { get; init; }
    string Id { get; init; }
    string? PdfUrl { get; init; }
    string Status { get; init; }
  // One saved card / payment method.
  sealed class PaymentsPaymentMethodView : IEquatable<PaymentsPaymentMethodView>
    // One saved card / payment method.
    ctor(string Id, string Brand, string Last4, int ExpMonth, int ExpYear, bool IsDefault = false)
    string Brand { get; init; }
    int ExpMonth { get; init; }
    int ExpYear { get; init; }
    string Id { get; init; }
    bool IsDefault { get; init; }
    string Last4 { get; init; }
  // View-model records for the Parallax billing components. They are intentionally lightweight and decoupled from the Stripe-shaped Payments records so the components can be driven from any source — a live PaymentsService , a fake in-memory list, or static catalog data.
  sealed class PaymentsPlanView : IEquatable<PaymentsPlanView>
    // View-model records for the Parallax billing components. They are intentionally lightweight and decoupled from the Stripe-shaped Payments records so the components can be driven from any source — a live PaymentsService , a fake in-memory list, or static catalog data.
    ctor(string PlanId, string Name, string PriceLabel, string? IntervalLabel = null, IReadOnlyList<string>? Features = null, string? Badge = null, string? CtaLabel = null, bool Highlighted = false, bool Disabled = false)
    string? Badge { get; init; }
    string? CtaLabel { get; init; }
    bool Disabled { get; init; }
    IReadOnlyList<string>? Features { get; init; }
    bool Highlighted { get; init; }
    string? IntervalLabel { get; init; }
    string Name { get; init; }
    string PlanId { get; init; }
    string PriceLabel { get; init; }
  // Subscription header / status card model.
  sealed class PaymentsSubscriptionView : IEquatable<PaymentsSubscriptionView>
    // Subscription header / status card model.
    ctor(string PlanName, string Status, DateTimeOffset? CurrentPeriodEnd = null, bool CancelAtPeriodEnd = false, string? PriceLabel = null)
    bool CancelAtPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    string PlanName { get; init; }
    string? PriceLabel { get; init; }
    string Status { get; init; }
  // Options for the Contact Picker API action.
  sealed class PickContactsActionOptions : ActionOptions, IEquatable<PickContactsActionOptions>
    ctor()
    // When true, allows selecting multiple contacts.
    bool Multiple { get; init; }
  // Event args for pointer down outside events on overlays.
  sealed class PointerDownOutsideArgs : IEquatable<PointerDownOutsideArgs>
    // Event args for pointer down outside events on overlays.
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Extension methods for QR code generation.
  static class QrCodeExtensions
    // QR code image. Generates a QR code server-side and renders it as an image.
    static void QR(UIView view, string[]? style = null, string? value = null, int size = 256, string? key = null, string file = "", int line = 0)
  // Extension methods for the RichTextEditor component.
  static class RichTextEditorExtensions
    // Inline rich-text editor with a configurable toolbar. Values are HTML strings.
    static void RichTextEditor(UIView view, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, IReadOnlyList<RichTextTool>? tools = null, bool? showToolbar = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? toolbarStyle = null, string[]? toolbarButtonStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, string file = "", int line = 0)
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
    BulletList
    NumberedList
    Blockquote
    Code
    Link
    ClearFormatting
    Undo
    Redo
  // Extension methods for conditionally rendering UI based on user roles.
  static class RoleGatedExtensions
    // Renders content only for admin users.
    static void ForAdmin(UIView view, ClientProfiles profiles, Context clientContext, Action<UIView> content)
    // Renders content only for moderator users.
    static void ForModerator(UIView view, ClientProfiles profiles, Context clientContext, Action<UIView> content)
    // Renders content only if the client has the specified role.
    static void ForRole(UIView view, ClientProfiles profiles, Context clientContext, UserRole role, Action<UIView> content)
    // Renders content only if the client has the specified role (by name).
    static void ForRole(UIView view, ClientProfiles profiles, Context clientContext, string role, Action<UIView> content)
    // Renders content only if the client has any of the specified roles.
    static void ForRoles(UIView view, ClientProfiles profiles, Context clientContext, IEnumerable<UserRole> roles, Action<UIView> content)
  // Tiny primitives for using ClientReactive`1 as a signal the app reads to decide what to render. Routes, tabs, modes, panel selections, "which dialog is open" — same shape, same primitives. Intentionally minimal: no opinionated tab bars, no URL coupling, no rendering bias. The signal is the building block; the app decides how to consume it. For URL ↔ signal sync (browser bar, deep links, back/forward), use Navigation on the host app — keeps URL concerns in one place instead of forking them through this layer.
  static class RoutingExtensions
    static void Routed<T>(UIView view, ClientReactive<T> signal, Dictionary<T, Action<UIView>> cases, Action<UIView>? fallback = null, string file = "", int line = 0)
    static Func<Task> Set<T>(UIView view, ClientReactive<T> signal, T value)
  // Represents which scrollbars to show in ScrollAreaSimple.
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
    static void ScrollColumn(UIView view, string[]? style = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType scrollType = Hover, bool autoScroll = false, string? autoScrollKey = null, string[]? bodyStyle = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string? styleId = null, string? key = null, string file = "", int line = 0)
  // Direction for infinite scroll loading.
  enum ScrollDirection
    Down
    Up
  // Event args for when user scrolls near the end of content.
  sealed class ScrollNearEndArgs : IEquatable<ScrollNearEndArgs>
    // Event args for when user scrolls near the end of content.
    ctor(double ScrollTop, double ScrollHeight, double ClientHeight, string Direction)
    double ClientHeight { get; init; }
    string Direction { get; init; }
    double ScrollHeight { get; init; }
    double ScrollTop { get; init; }
  // Extension methods for Select components.
  static class SelectExtensions
    // Select dropdown component that auto-renders the full structure with trigger button, dropdown content, and items. Use either options (flat list) or groups (grouped items) - not both.
    static void Select(UIView view, string[]? style = null, IReadOnlyList<SelectOption>? options = null, IReadOnlyList<SelectOptionGroup>? groups = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, bool? required = null, bool? open = null, string? name = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? itemStyle = null, string[]? itemIndicatorStyle = null, string? indicatorIconName = "check", string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool?, Task>? onOpenChange = null, string? label = null, string file = "", int line = 0)
  // Represents a selectable option in a Select component.
  sealed class SelectOption : IEquatable<SelectOption>
    // Represents a selectable option in a Select component.
    ctor(string Value, string Label, bool Disabled = false)
    bool Disabled { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  // Represents a group of selectable options in a Select component.
  sealed class SelectOptionGroup : IEquatable<SelectOptionGroup>
    // Represents a group of selectable options in a Select component.
    ctor(string? Label, IReadOnlyList<SelectOption> Options)
    string? Label { get; init; }
    IReadOnlyList<SelectOption> Options { get; init; }
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
    static void ShadertoyCanvas(UIView view, string[]? style = null, string? shaderSource = null, int? fps = null, IReadOnlyDictionary<string, ShaderUniform>? uniforms = null, bool? enableMouse = null, int? width = null, int? height = null, string? styleId = null, string? key = null, string file = "", int line = 0)
  // Options for the Web Share API action.
  sealed class ShareActionOptions : ActionOptions, IEquatable<ShareActionOptions>
    ctor()
    // Text body for the shared content.
    string? Text { get; init; }
    // Title for the shared content.
    string? Title { get; init; }
    // URL to share.
    string? Url { get; init; }
  // Options for showing a browser notification.
  sealed class ShowNotificationActionOptions : ActionOptions, IEquatable<ShowNotificationActionOptions>
    // Notification body text.
    string? Body { get; init; }
    // URL of the notification icon image.
    string? Icon { get; init; }
    // Notification title text.
    string Title { get; init; }
  // Represents the side for positioning overlays.
  enum Side
    Top
    Right
    Bottom
    Left
  // Represents sort strategy for @dnd-kit SortableContext.
  enum SortStrategy
    VerticalList
    HorizontalList
  // Contains information about a reorder operation in SortableList.
  sealed class SortableReorderArgs : IEquatable<SortableReorderArgs>
    // Contains information about a reorder operation in SortableList.
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
  // Represents sticky behavior for Select/DropdownMenu.
  enum Sticky
    Partial
    Always
  // Defines a tab for use with the Tabs component.
  class TabItem : IEquatable<TabItem>
    // Defines a tab for use with the Tabs component.
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
  // Extension methods for Tabs components.
  static class TabsExtensions
    // Container for Tabs components. Use the 'tabs' parameter to define tab content.
    static void Tabs(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, ActivationMode activationMode = Automatic, IEnumerable<TabItem>? tabs = null, string[]? listContainerStyle = null, string[]? listStyle = null, string[]? triggerStyle = null, string[]? disabledTriggerStyle = null, string[]? contentContainerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, string file = "", int line = 0)
  // Smallest time unit shown by a TimePicker .
  enum TimeGranularity
    Hour
    Minute
    Second
  // Extension methods for TimePicker components.
  static class TimePickerExtensions
    // Picker for a time of day. Values are ISO-8601 HH:mm or HH:mm:ss strings.
    static void TimePicker(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, HourFormat hourFormat = Hour24, TimeGranularity granularity = Minute, int? minuteStep = null, int? secondStep = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string? placeholder = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? columnStyle = null, string[]? itemStyle = null, string[]? itemSelectedStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
  // Event args for toast swipe events.
  sealed class ToastSwipeArgs : IEquatable<ToastSwipeArgs>
    // Event args for toast swipe events.
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
  // Extension methods for the DOM-virtualized scroll containers VirtualList and VirtualGrid . Items outside the visible window plus an overscan buffer have their content children skipped at the React layer (the wrapper still occupies space via fixed dimensions), so DOM size scales with viewport, not itemCount.
  static class VirtualListExtensions
    // DOM-virtualized scrollable grid. Items are laid out in a fixed number of columns and rows outside the visible window are not mounted in the DOM.
    static void VirtualGrid(UIView view, int itemCount, int columns, double rowHeight, Action<UIView, int> onRenderItem, int overscan = 2, int gap = 12, int? minItemWidthPx = null, int? maxColumns = null, double? aspectRatio = null, string? resetScrollKey = null, Func<double, Task>? onNearEnd = null, int nearEndThresholdRows = 2, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // DOM-virtualized vertical list with fixed item height. Renders only items inside the visible window plus an overscan buffer.
    static void VirtualList(UIView view, int itemCount, double itemHeight, Action<UIView, int> onRenderItem, int overscan = 4, Func<double, Task>? onNearEnd = null, int nearEndThreshold = 5, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
  // Day of the week used as the first column in the calendar grid.
  enum WeekStart
    Sunday
    Monday

namespace Ikon.Parallax.Themes
  // Legacy alias for ITheme . Apps scaffolded before the refactor still ship a local `Theme : ITheme` with `global using Ikon.Parallax.Themes;` — this keeps that resolve to a real interface whose contract matches the new one. New code should reference ITheme directly.
  interface ITheme : ITheme

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
    static string Grey
    static string GreyLg
    static string GreyMd
    static string GreySm
    static string IconLeft
    static string IconRight
    static string Info
    static string InfoLg
    static string InfoMd
    static string InfoSm
    static string OutlineBrand
    static string OutlineBrandLg
    static string OutlineBrandMd
    static string OutlineBrandSm
    static string OutlineError
    static string OutlineErrorLg
    static string OutlineErrorMd
    static string OutlineErrorSm
    static string OutlineGrey
    static string OutlineGreyLg
    static string OutlineGreyMd
    static string OutlineGreySm
    static string OutlineInfo
    static string OutlineInfoLg
    static string OutlineInfoMd
    static string OutlineInfoSm
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
  static class DragDrop.Draggable
    static string Base
    static string Dashed
    static string Default
    static string Disabled
    static string Dragging
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
  static class Tokens.FocusRing
    static string Default
    static string Strong
    static string Subtle
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
  static class Helper
    static string Join(params string?[] parts)
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
  // Per-app theme configuration. Composes the platform's Ikon CSS baseline with per-token CSS-variable overrides addressed by name. One uniform syntax: an indexer keyed by CSS variable name (without the leading --) or by Tailwind utility token. The renderer dispatches by key shape: Tailwind palette step (amber-400) → --color-amber-400rounded-{rung} → --radius-{rung}shadow-{rung} → --shadow-{rung}font-{role} → --font-{role}ease-{kind} → --ease-{kind}Anything else → --{key} (free CSS variable) Values are Crosswind / Tailwind class names (resolved via CrosswindResolver ) or raw CSS values (hex, rem, family stacks, gradients) — the resolver passes raw values through. Example: private UI UI { get; } = new(app, new IkonTheme { // Brand commitment — set the semantic vars that components consume. ["primary"] = "amber-400", ["bg-brand-solid"] = "amber-400", ["bg-brand-solid-hover"] = "amber-400", ["text-brand"] = "amber-400", ["border-brand"] = "amber-400", ["primary-foreground"] = "#0A0A0A", // pick contrast yourself // Background + foreground. ["background"] = "zinc-950", ["text-primary"] = "amber-50", ["text-foreground"] = "amber-50", // Surfaces. ["card"] = "zinc-900", ["popover"] = "zinc-900", // Type + shape. ["font-heading"] = "Crimson Pro", ["font-body"] = "Inter", ["radius-base"] = "rounded-lg", // Motion. ["motion-duration-base"] = "200ms", ["ease-default"] = "ease-out", // Per-token Tailwind palette / radius / shadow overrides. ["amber-400"] = "#F5A524", ["rounded-lg"] = "1.25rem", ["shadow-lg"] = "0 8px 16px rgba(0,0,0,.18)", // Bespoke decorative tokens. ["hero-glow"] = "radial-gradient(circle, #F5A52488, transparent 70%)", DarkMode = new IkonTheme { ["background"] = "zinc-50", ["text-primary"] = "zinc-950", }, }); The indexer is the only configurable surface — there are no magic property fan-outs and no auto-derived contrast text. What you write IS what lands in the override block.
  sealed class IkonTheme : ITheme
    ctor()
    // Paired dark-mode theme. Pass another IkonTheme ; its overrides are emitted under [data-theme="dark"], .dark, and prefers-color-scheme: dark.
    IkonTheme? DarkMode { get; init; }
    string Item { get; set; }
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
    static string ComposeIndicator(string? variant = null, bool indeterminate = false, params string?[] overrides)
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
  // Legacy alias for the non-configurable default theme — equivalent to new IkonTheme() with no overrides. Apps scaffolded against the older platform shape used new Theme() to get the baseline; the indexer-driven IkonTheme is the new API and should be used in new code.
  sealed class Theme : ITheme
    ctor()
    string Css { get; }
    string DefaultIconLibrary { get; }
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

namespace Ikon.Parallax.Theming.Flutter
  static class FlutterTokens.Badge
    static string Brand
    static string Neutral
  static class FlutterTokens.Button
    static string Danger
    static string Ghost
    static string Icon
    static string Neutral
    static string Outline
    static string Primary
  static class FlutterTokens.Divider
    static string Horizontal
    static string Line
  static class FlutterTokens
  static class FlutterTokens.Icon
    static string Default
    static string Lg
    static string Md
    static string Sm
    static string Xs
  static class FlutterTokens.Input
    static string Area
    static string Default
  static class FlutterTokens.Layout
    static string Center
    static string Column
    static string Row
    static string RowWrap
    static string Screen
  static class FlutterTokens.Surface
    static string Card
    static string Panel
    static string Screen
  static class FlutterTokens.Text
    static string Body
    static string BodyStrong
    static string Caption
    static string H1
    static string H2
    static string H3
    static string Hero
    static string Label
    static string Link
    static string Muted

# Ikon.Crosswind Public API

namespace Ikon.Crosswind
  enum ArgType
    Length
    Color
    Time
    Angle
    Fraction
    Keyword
    Unknown
  class ArgValue : IEquatable<ArgValue>
    ctor(string Raw, string Value, Unit Unit, ArgType Type)
    string Raw { get; init; }
    ArgType Type { get; init; }
    Unit Unit { get; init; }
    string Value { get; init; }
    bool IsColor()
    bool IsKeyword()
    bool IsLength()
    bool IsUnit()
    bool IsUnknown()
  sealed class BorderRadiusToken : IEquatable<BorderRadiusToken>
    ctor(double? TopLeft, double? TopRight, double? BottomLeft, double? BottomRight)
    double? BottomLeft { get; init; }
    double? BottomRight { get; init; }
    double? TopLeft { get; init; }
    double? TopRight { get; init; }
    static BorderRadiusToken All(double value)
    BorderRadiusToken MergeOver(BorderRadiusToken? other)
  sealed class BorderSideToken : IEquatable<BorderSideToken>
    ctor(double? Width, ColorToken? Color)
    ColorToken? Color { get; init; }
    double? Width { get; init; }
    BorderSideToken MergeOver(BorderSideToken? other)
  sealed class BorderToken : IEquatable<BorderToken>
    ctor(BorderSideToken? Left, BorderSideToken? Top, BorderSideToken? Right, BorderSideToken? Bottom)
    BorderSideToken? Bottom { get; init; }
    BorderSideToken? Left { get; init; }
    BorderSideToken? Right { get; init; }
    BorderSideToken? Top { get; init; }
    static BorderToken All(BorderSideToken side)
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
  sealed class ColorToken : IEquatable<ColorToken>
    ctor(string? Ref, string? Raw, double? Alpha)
    double? Alpha { get; init; }
    string? Raw { get; init; }
    string? Ref { get; init; }
    static ColorToken Literal(string raw, double? alpha = null)
    static ColorToken ThemeRef(string token, double? alpha = null)
  class CompileResult : IEquatable<CompileResult>
    ctor(List<CssRule> Rules, List<string> ExtraCss, List<MotionBindingMetadata> MotionBindings)
    List<string> ExtraCss { get; init; }
    List<MotionBindingMetadata> MotionBindings { get; init; }
    List<CssRule> Rules { get; init; }
  sealed class CompiledStyle : IEquatable<CompiledStyle>
    ctor(string Css, IReadOnlyList<MotionBindingMetadata> MotionBindings, FlutterStyleTokens? Flutter = null)
    string Css { get; init; }
    FlutterStyleTokens? Flutter { get; init; }
    IReadOnlyList<MotionBindingMetadata> MotionBindings { get; init; }
  class ContainerVariant : IEquatable<ContainerVariant>
    ctor(string? Name, string? Query, string? Breakpoint)
    string? Breakpoint { get; init; }
    string? Name { get; init; }
    string? Query { get; init; }
    bool WantsBreakpoint { get; }
    ContainerVariant WithBreakpoint(string breakpoint)
  static class CssEmitter
    // Emits compact CSS text from compiled rules.
    static string Emit(CompileResult result)
  static class CssProcessor
    static string GetCss(string tailwindDeclaration, string classId)
    static CompiledStyle GetStyle(string tailwindDeclaration, string classId)
  class CssRule : IEquatable<CssRule>
    ctor(string? AtRule, string Selector, Dictionary<string, string> Decls)
    string? AtRule { get; init; }
    Dictionary<string, string> Decls { get; init; }
    string Selector { get; init; }
  enum CursorToken
    Default
    Pointer
    Text
    NotAllowed
    Grab
    Grabbing
    Crosshair
    Move
    None
  enum DarkModeStrategy
    Media
    Class
  sealed class EdgeInsetsToken : IEquatable<EdgeInsetsToken>
    ctor(double? Left, double? Top, double? Right, double? Bottom)
    double? Bottom { get; init; }
    double? Left { get; init; }
    double? Right { get; init; }
    double? Top { get; init; }
    static EdgeInsetsToken All(double value)
    EdgeInsetsToken MergeOver(EdgeInsetsToken? other)
    static EdgeInsetsToken Symmetric(double? horizontal, double? vertical)
  enum FlexAlignToken
    Start
    End
    Center
    Stretch
    Baseline
  enum FlexDirectionToken
    Row
    Column
    RowReverse
    ColumnReverse
  enum FlexJustifyToken
    Start
    End
    Center
    SpaceBetween
    SpaceAround
    SpaceEvenly
  sealed class FlexToken : IEquatable<FlexToken>
    ctor(FlexDirectionToken? Direction, FlexAlignToken? AlignItems, FlexJustifyToken? JustifyContent, double? Gap, double? RowGap, double? ColumnGap, bool? Wrap)
    FlexAlignToken? AlignItems { get; init; }
    double? ColumnGap { get; init; }
    FlexDirectionToken? Direction { get; init; }
    double? Gap { get; init; }
    FlexJustifyToken? JustifyContent { get; init; }
    double? RowGap { get; init; }
    bool? Wrap { get; init; }
    FlexToken MergeOver(FlexToken? other)
  static class FlutterStyleResolver
    static FlutterStyleTokens Resolve(string tailwindDeclaration)
  sealed class FlutterStyleTokens : IEquatable<FlutterStyleTokens>
    ctor(EdgeInsetsToken? Padding, EdgeInsetsToken? Margin, ColorToken? BackgroundColor, BorderToken? Border, BorderRadiusToken? BorderRadius, SizeToken? Size, TextStyleToken? Text, FlexToken? Flex, double? Opacity, IReadOnlyList<ShadowToken>? Shadow, OverflowToken? Overflow, TransformToken? Transform, PositionToken? Position, GradientToken? Gradient, MotionToken? Motion, bool? Hidden, bool? Visible, CursorToken? Cursor, double? AspectRatio, int? ZIndex, int? GridColumns = null, bool? Pulse = null, bool? Spin = null)
    double? AspectRatio { get; init; }
    ColorToken? BackgroundColor { get; init; }
    BorderToken? Border { get; init; }
    BorderRadiusToken? BorderRadius { get; init; }
    CursorToken? Cursor { get; init; }
    static FlutterStyleTokens Empty { get; }
    FlexToken? Flex { get; init; }
    GradientToken? Gradient { get; init; }
    int? GridColumns { get; init; }
    bool? Hidden { get; init; }
    bool IsEmpty { get; }
    EdgeInsetsToken? Margin { get; init; }
    MotionToken? Motion { get; init; }
    double? Opacity { get; init; }
    OverflowToken? Overflow { get; init; }
    EdgeInsetsToken? Padding { get; init; }
    PositionToken? Position { get; init; }
    bool? Pulse { get; init; }
    IReadOnlyList<ShadowToken>? Shadow { get; init; }
    SizeToken? Size { get; init; }
    bool? Spin { get; init; }
    TextStyleToken? Text { get; init; }
    TransformToken? Transform { get; init; }
    bool? Visible { get; init; }
    int? ZIndex { get; init; }
  sealed class GradientToken : IEquatable<GradientToken>
    ctor(string Direction, ColorToken? From, ColorToken? Via, ColorToken? To)
    string Direction { get; init; }
    ColorToken? From { get; init; }
    ColorToken? To { get; init; }
    ColorToken? Via { get; init; }
  sealed class MotionBindingMetadata : IEquatable<MotionBindingMetadata>
    ctor(string Source, string? Min, string? Max, string? Clamp, bool Reverse, string? Ease, string? Map, string? TargetId)
    string? Clamp { get; init; }
    string? Ease { get; init; }
    string? Map { get; init; }
    string? Max { get; init; }
    string? Min { get; init; }
    bool Reverse { get; init; }
    string Source { get; init; }
    string? TargetId { get; init; }
  sealed class MotionToken : IEquatable<MotionToken>
    ctor(string? Type, double? Duration, string? Ease, double? Delay, string? IterationMode)
    double? Delay { get; init; }
    double? Duration { get; init; }
    string? Ease { get; init; }
    string? IterationMode { get; init; }
    string? Type { get; init; }
  enum OverflowToken
    Visible
    Hidden
    Scroll
    Auto
  sealed class PositionToken : IEquatable<PositionToken>
    ctor(PositionTypeToken Type, double? Top, double? Right, double? Bottom, double? Left)
    double? Bottom { get; init; }
    double? Left { get; init; }
    double? Right { get; init; }
    double? Top { get; init; }
    PositionTypeToken Type { get; init; }
  enum PositionTypeToken
    Static
    Relative
    Absolute
    Fixed
    Sticky
  static class SelectorComposer
    static IReadOnlyDictionary<string, string> BreakpointMap { get; }
    static string DarkClassSelector { get; set; }
    static DarkModeStrategy DarkMode { get; set; }
    static bool EnableThemeVariant { get; set; }
    static string GroupClassSelector { get; set; }
    static string PeerClassSelector { get; set; }
    static ThemeSelectorStrategy ThemeStrategy { get; set; }
    static string Compose(string baseSelector, IReadOnlyList<string> variants, string? track, ContainerVariant? container = null)
    static ValueTuple<string?, string> ComposeTemplate(IReadOnlyList<string> variants, string? track, ContainerVariant? container = null)
  sealed class ShadowToken : IEquatable<ShadowToken>
    ctor(double OffsetX, double OffsetY, double BlurRadius, double SpreadRadius, ColorToken Color)
    double BlurRadius { get; init; }
    ColorToken Color { get; init; }
    double OffsetX { get; init; }
    double OffsetY { get; init; }
    double SpreadRadius { get; init; }
  sealed class SizeToken : IEquatable<SizeToken>
    ctor(double? Width, double? Height, double? MinWidth, double? MinHeight, double? MaxWidth, double? MaxHeight)
    double? Height { get; init; }
    double? MaxHeight { get; init; }
    double? MaxWidth { get; init; }
    double? MinHeight { get; init; }
    double? MinWidth { get; init; }
    double? Width { get; init; }
    SizeToken MergeOver(SizeToken? other)
  static class TW
    static string FormatLength(ArgValue a)
    static string FractionToPercent(string frac)
    static string MaybeNegate(bool negative, string val)
    static string ResolveColor(string raw, TailwindColorContext context = Generic)
    static string ResolveFontFamily(string token)
    static string ResolveFontWeight(string token)
    static string ResolveLetterSpacing(string tokenOrLength)
    static string ResolveLineHeight(string tokenOrLength)
    static string ResolveOpacity(string token)
    static string ResolveRadius(string tokenOrLength)
    static string ResolveShadow(string token)
    static string ResolveTextAlign(string token)
    static ValueTuple<string, string?> ResolveTextSize(string tokenOrLength)
    static string SpacingTokenToLength(string token)
    static string UnitToSuffix(Unit u)
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
  static class TailwindCompiler
    // Compiles a class list into CSS rules scoped to a base selector. Use when you want real CSS (supports :hover, @media, group-hover, dark, etc).
    static CompileResult CompileRules(string baseSelector, string classAttr)
  static class TailwindCssBaseline
    static string AdditionalCss { get; }
    static IReadOnlyDictionary<string, string> DarkVariables { get; }
    static IReadOnlyDictionary<string, string> LightVariables { get; }
    static string GetFullBaseline()
  sealed class TailwindCssVariables
    ctor(IDictionary<string, string> light, IDictionary<string, string> dark, string darkThemeName = "dark")
    IReadOnlyDictionary<string, string> Dark { get; }
    string DarkThemeName { get; }
    IReadOnlyDictionary<string, string> Light { get; }
    string EmitDark()
    string EmitLight()
  static class TailwindCustomStyleRegistry
    static bool IsFontFamilyToken(string name)
    static bool IsFontWeightToken(string name)
    static void MergeDefinitions(TailwindStyleDefinitions definitions)
    static void SetDefinitions(TailwindStyleDefinitions? definitions)
    static bool TryResolve(string name, TailwindColorContext context, out string value)
    static bool TryResolveFontFamily(string name, out string value)
    static bool TryResolveFontWeight(string name, out string value)
  static class TailwindDedup
    // Last-one-wins de-duplication by (Variants, Track, Utility).
    static List<TailwindDescription> Deduplicate(List<TailwindDescription> classes)
  class TailwindDescription : IEquatable<TailwindDescription>
    ctor(List<string> Variants, string? Track, string Utility, List<ArgValue> Args, bool Important, bool Negative, ContainerVariant? Container = null, bool HasBracketArg = false, bool IsArbitraryProperty = false)
    List<ArgValue> Args { get; init; }
    ContainerVariant? Container { get; init; }
    bool HasBracketArg { get; init; }
    bool Important { get; init; }
    bool IsArbitraryProperty { get; init; }
    bool Negative { get; init; }
    string? Track { get; init; }
    string? TrackKey { get; }
    string Utility { get; init; }
    List<string> Variants { get; init; }
  sealed class TailwindDesignTokenResult
    ctor(TailwindCssVariables cssVariables, TailwindThemeDefinition theme, TailwindStyleDefinitions styleDefinitions)
    TailwindCssVariables CssVariables { get; }
    TailwindStyleDefinitions StyleDefinitions { get; }
    TailwindThemeDefinition Theme { get; }
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
  static class TailwindNormalizer
    static TailwindDescription Normalize(TailwindDescription tw)
  static class TailwindParser
    static List<TailwindDescription> ParseManyRaw(string inputLine)
    static TailwindDescription ParseRaw(string input)
    static ValueTuple<List<string>, string?, ContainerVariant?> SplitVariants(List<string> variants)
  static class TailwindStyleDefinitionLoader
    static Task<TailwindStyleDefinitions> Load(AssetUri assetUri)
    static TailwindStyleDefinitions LoadFromCss(string css)
    static TailwindStyleDefinitions LoadFromFile(string path)
    static TailwindStyleDefinitions LoadFromJson(string json)
    static TailwindStyleDefinitions LoadFromStream(Stream stream)
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
  enum TextAlignToken
    Start
    End
    Center
    Justify
    Left
    Right
  enum TextDecorationToken
    None
    Underline
    LineThrough
    Overline
  enum TextOverflowToken
    Clip
    Ellipsis
    Fade
  sealed class TextStyleToken : IEquatable<TextStyleToken>
    ctor(double? FontSize, int? FontWeight, ColorToken? Color, string? FontFamily, double? LineHeight, double? LetterSpacing, TextAlignToken? Align, TextDecorationToken? Decoration, TextOverflowToken? TextOverflow, int? MaxLines, bool? Italic, TextTransformToken? TextTransform, WhiteSpaceToken? WhiteSpace)
    TextAlignToken? Align { get; init; }
    ColorToken? Color { get; init; }
    TextDecorationToken? Decoration { get; init; }
    string? FontFamily { get; init; }
    double? FontSize { get; init; }
    int? FontWeight { get; init; }
    bool? Italic { get; init; }
    double? LetterSpacing { get; init; }
    double? LineHeight { get; init; }
    int? MaxLines { get; init; }
    TextOverflowToken? TextOverflow { get; init; }
    TextTransformToken? TextTransform { get; init; }
    WhiteSpaceToken? WhiteSpace { get; init; }
    TextStyleToken MergeOver(TextStyleToken? other)
  enum TextTransformToken
    None
    Uppercase
    Lowercase
    Capitalize
  static class ThemeEmitter
    static string Emit(IDictionary<string, string> vars, string? themeName = null)
  enum ThemeSelectorStrategy
    Attribute
    Class
  static class ThemeToTailwindConverter
    static TailwindDesignTokenResult Convert(CanvasDesignTokenDocument document)
  static class ThemeVars
    static bool VariableFallbacksEnabled { get; set; }
    static string Var(string name, string? fallback = null)
  static class TransformCombiner
    // Merges transform utilities by (Variants, Track) into a single "transform" utility. Call after TailwindDedup.Deduplicate.
    static List<TailwindDescription> Combine(List<TailwindDescription> classes)
    // Composes transform utilities for motion context, outputting individual CSS variables instead of a monolithic transform property. This allows independent animation tracks to blend without overriding each other.
    static Dictionary<string, string> ComposeForMotion(List<TailwindDescription> classes)
  sealed class TransformToken : IEquatable<TransformToken>
    ctor(double? Rotate, double? ScaleX, double? ScaleY, double? TranslateX, double? TranslateY, double? SkewX, double? SkewY)
    double? Rotate { get; init; }
    double? ScaleX { get; init; }
    double? ScaleY { get; init; }
    double? SkewX { get; init; }
    double? SkewY { get; init; }
    double? TranslateX { get; init; }
    double? TranslateY { get; init; }
  enum Unit
    Px
    Rem
    Em
    Percent
    Vw
    Vh
    Vmin
    Vmax
    Svw
    Svh
    Lvw
    Lvh
    Dvw
    Dvh
    Cqw
    Cqh
    Cqi
    Cqb
    Cqmin
    Cqmax
    Svb
    Svi
    Lvb
    Lvi
    Dvb
    Dvi
    Svmin
    Svmax
    Lvmin
    Lvmax
    Dvmin
    Dvmax
    Ch
    Ex
    Cm
    Mm
    In
    Pt
    Pc
    Ms
    S
    Deg
    Rad
    Turn
    None
    Unknown
  static class Utilities
    static Dictionary<string, Dictionary<string, string>> Accent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AccentColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Align(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AlignContent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AlignItems(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AlignSelf(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AlignTracks(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AllPetiteCaps(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> AllSmallCaps(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> AnchorName(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AnchorScope(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Animate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AnimationDelay(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AnimationDirection(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AnimationDuration(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AnimationFillMode(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AnimationIterationCount(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AnimationPlayState(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AnimationTimingFunction(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Antialiased(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Appearance(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Aspect(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AtContainer(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AutoCols(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AutoRows(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropBlur(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropBrightness(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropContrast(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropFilter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropFilterNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BackdropGrayscale(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropHueRotate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropInvert(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropOpacity(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropSaturate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropSepia(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropShorthand(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Backface(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackgroundBlend(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackgroundColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Basis(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgAuto(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgBlend(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgBottom(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgCenter(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgClipBorder(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgClipContent(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgClipPadding(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgClipText(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgConic(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgContain(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgCover(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgFixed(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgGradientToB(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToBl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToBr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToL(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToR(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToT(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToTl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToTr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgImage(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgLeft(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLeftBottom(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLeftTop(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinear(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgLinearToB(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToBl(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToBr(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToL(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToR(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToT(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToTl(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToTr(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLocal(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgNoRepeat(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgOriginBorder(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgOriginContent(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgOriginPadding(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgPosition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgRadial(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgRepeat(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRepeatRound(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRepeatSpace(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRepeatX(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRepeatY(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRight(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRightBottom(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRightTop(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgScroll(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgTop(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Block(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Blur(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Border(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderB(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderBColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderBStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderBlock(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderBlockEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderBlockStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderCollapse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BorderColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderE(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderEColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderEStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderImage(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderInline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderInlineEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderInlineStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderL(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderLColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderLStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderR(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderRColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderRStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderRadius(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderS(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderSColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderSStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderSeparate(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BorderSpacing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderSpacingX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderSpacingY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderT(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderTColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderTStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderWidth(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderXColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderXStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderYColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderYStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Bottom(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BoxDecoration(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BoxSizing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Break(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BreakAfter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BreakBefore(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BreakInside(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Brightness(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Capitalize(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> CaptionBottom(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> CaptionSide(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> CaptionTop(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Caret(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> CaretColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> CaretShape(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Clear(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ClipPath(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Collapse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> ColorInterpolationFilters(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ColorScheme(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Columns(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Contain(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ContainIntrinsicHeight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ContainIntrinsicSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ContainIntrinsicWidth(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Container(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> ContainerName(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ContainerType(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Content(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ContentVisibility(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Contents(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Contrast(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> CounterIncrement(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> CounterReset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> CounterSet(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Cursor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Decoration(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DecorationColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DecorationNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> DecorationSkipInk(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DecorationStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DecorationThickness(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Delay(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DiagonalFractions(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Direction(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DivideColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DivideStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DivideX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DivideXReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> DivideY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DivideYReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> DropShadow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Duration(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ease(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> EmptyCells(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> End(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FadeIn(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FadeOut(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FieldSizing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Fill(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Filter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FilterNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Flex1(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexAuto(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexCol(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexColReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexCombined(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FlexInitial(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexNoWrap(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexRow(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexRowReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexWrap(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexWrapReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Float(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FlowRoot(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FontFamily(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontFeature(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontKerning(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontOpticalSizing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontPalette(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontPaletteValues(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontSizeAdjust(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontStretch(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontSynthesis(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontVariantShorthand(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontVariation(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontWeight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ForcedColorAdjust(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Gap(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GapX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GapY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GradientFrom(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GradientTo(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GradientVia(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GradientViaNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Grayscale(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridArea(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridCols(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridColumn(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridColumnEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridColumnSpan(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridColumnStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridDisplay(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> GridFlow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRowEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRowSpan(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRowStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRows(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Grow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Grow0(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> HangingPunctuation(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Height(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Hidden(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> HueRotate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Hyphens(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ImageRendering(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ImageResolution(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Indent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InitialLetter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Inline(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> InlineBlock(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> InlineFlex(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> InlineGrid(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> InlineTable(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Inset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetArea(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetBlock(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetBlockEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetBlockStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetInline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetInlineEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetInlineStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetRing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InterpolateSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Invert(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Invisible(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Isolate(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> IsolationAuto(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Italic(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> JustifyContent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> JustifyItems(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> JustifySelf(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> JustifyTracks(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Left(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> LetterSpacing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> LineBreak(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> LineClamp(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> LineHeight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> LineThrough(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> LiningNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> List(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ListImage(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ListItem(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Lowercase(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> M(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskClip(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskComposite(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskImage(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskMode(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskNoRepeat(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> MaskOrigin(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskPosition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskRepeat(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskRepeatRound(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskRepeatSpace(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskRepeatX(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> MaskRepeatY(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> MaskShorthand(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MathStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaxHeight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaxWidth(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Mb(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Me(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MinHeight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MinWidth(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MixBlend(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ml(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Motion(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionBind(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionBindEase(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionBindReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> MotionComposition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionDelay(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionDuration(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionEase(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionFill(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionLetterDelay(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionMap(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionNoPromote(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionOnce(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerChildren(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetterLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetterPingPong(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetterReverse(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetterReverseLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLine(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLineLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLinePingPong(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLineReverse(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLineReverseLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerParagraph(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWord(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWordLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWordPingPong(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWordReverse(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWordReverseLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPingPong(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPlayState(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPriority(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPromote(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionRange(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionRangeEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionRangeStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionRate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionTimeline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Mr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ms(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Mt(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Mx(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> My(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> NoUnderline(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> NormalCaps(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> NormalCase(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> NormalNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> NotItalic(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> NotSrOnly(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Object(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OffsetAnchor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OffsetDistance(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OffsetPath(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OffsetPosition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OffsetRotate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OldstyleNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Opacity(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Order(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ordinal(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Orphans(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Outline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OutlineColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OutlineHidden(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> OutlineNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> OutlineOffset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OutlineStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OutlineWidth(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Overflow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverflowAnchor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverflowBlock(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverflowInline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverflowX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverflowY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Overline(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Overscroll(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverscrollBlock(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverscrollInline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverscrollX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverscrollY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> P(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PageBreakAfter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PageBreakBefore(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PageBreakInside(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PaintOrder(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Pb(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Pe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Perspective(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PerspectiveOrigin(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PetiteCaps(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Pl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PlaceContent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PlaceItems(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PlaceSelf(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PointerEvents(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PosAbsolute(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> PosFixed(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> PosRelative(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> PosStatic(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> PosSticky(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> PositionAnchor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PositionArea(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PositionTry(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PositionTryOptions(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PositionTryOrder(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PositionVisibility(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Pr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PrintColorAdjust(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ProportionalNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Ps(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Pt(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Px(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Py(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Quotes(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ReadingFlow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ReadingOrder(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Resize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Right(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ring(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RingInset(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> RingOffset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Rotate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RotateX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RotateY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RotateZ(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedB(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedBl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedBr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedE(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedEe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedEs(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedL(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedR(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedS(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedSe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedSs(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedT(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedTl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedTr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RubyAlign(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RubyPosition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Saturate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Scale(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScaleX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScaleY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScaleZ(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollBehavior(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollM(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMb(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMs(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMt(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMx(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMy(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollP(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPb(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPs(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPt(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPx(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPy(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollTimeline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollTimelineAxis(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollTimelineName(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollbarAuto(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> ScrollbarColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollbarGutter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollbarNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> ScrollbarThin(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Select(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Sepia(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Shadow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ShapeImageThreshold(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ShapeMargin(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ShapeOutside(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Shrink(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Shrink0(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Size(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SkewX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SkewY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SlashedZero(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> SlideInFromBottom(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SlideInFromLeft(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SlideInFromRight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SlideInFromTop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SlideOutToBottom(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SlideOutToLeft(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SlideOutToRight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SlideOutToTop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SmallCaps(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> SnapAlign(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SnapAxis(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SnapStop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SnapStrictness(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SpaceX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SpaceXReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> SpaceY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SpaceYReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Speak(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SpinIn(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SpinOut(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SrOnly(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> StackedFractions(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Start(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Stroke(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> StrokeDasharray(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> StrokeDashoffset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> StrokeJoin(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> StrokeLinecap(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SubpixelAntialiased(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TabSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Table(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableAuto(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableCaption(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableCell(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableColumn(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableColumnGroup(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableFixed(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableFooterGroup(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableHeaderGroup(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableRow(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableRowGroup(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TabularNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TextAlign(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextAutospace(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextBalance(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TextBoxEdge(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextBoxTrim(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextClip(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TextColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextCombineUpright(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextEllipsis(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TextEmphasis(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextNowrap(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TextOrientation(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextPretty(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TextShadow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextSpacingTrim(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextStroke(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextWrap(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TimelineScope(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TitlingCaps(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Top(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Touch(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TouchPan(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TouchPinch(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Transform(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TransformBox(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TransformNone(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TransformOrigin(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Transition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TransitionBehavior(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TransitionNone(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Translate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TranslateX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TranslateY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TranslateZ(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Truncate(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Underline(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> UnderlineOffset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> UnderlinePosition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Unicase(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Uppercase(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> VectorEffect(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ViewTimelineAxis(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ViewTimelineInset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ViewTimelineName(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ViewTransitionClass(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ViewTransitionGroup(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ViewTransitionName(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Visible(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Whitespace(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Widows(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Width(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> WillChange(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> WordSpacing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> WritingMode(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ZIndex(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Zoom(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ZoomIn(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ZoomOut(TailwindDescription cls)
  class UtilityAttribute : Attribute
    ctor(string prefix)
    string Prefix { get; }
  static class UtilityExec
    static bool HasUtility(string prefix)
    static Dictionary<string, Dictionary<string, string>> Run(TailwindDescription tw)
  enum WhiteSpaceToken
    Normal
    Nowrap
    Pre
    PreWrap
    PreLine

# Ikon.App Public API

namespace Ikon.App
  // Attribute that decorates app classes to configure their connection and messaging behavior
  sealed class AppAttribute : Attribute
    // Attribute that decorates app classes to configure their connection and messaging behavior
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL, GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL, GROUP_APP_LOCAL, string[]? dependencies = null)
    // Internal version constant for the attribute schema itself, used for versioning the App constructor calls if new parameters are added
    int AppVersion { get; }
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
    PluginAttribute ToPluginAttribute(Type owner)
  // Helper class for creating database connections from app configuration.
  static class AppDatabaseConnection
    // Creates a database connection for the specified database name from the app's configured databases.
    static DbConnection Create(IAppBase app, string databaseName)
    // Creates a database connection from a database connection info.
    static DbConnection Create(DatabaseConnectionInfo dbInfo)
  // A lightweight HTTP and WebSocket endpoint host built on ASP.NET Core. Construct the host, register routes with MapGet / MapPost / MapWebSocket , and call StartAsync to allocate the relay tunnel and begin serving requests.
  sealed class AppEndpointHost : IAsyncDisposable
    // Creates a new HTTP/WebSocket endpoint host. The relay tunnel is not allocated until StartAsync is called.
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    // The local port Kestrel binds to. Available after StartAsync completes.
    int LocalPort { get; }
    // The public URL for this endpoint. Available after StartAsync completes.
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
    // Allocates the relay tunnel, starts Kestrel with the registered routes, and returns immediately while the host continues to run in the background.
    Task StartAsync(CancellationToken cancellationToken = null)
    // Stops the endpoint host gracefully. Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = null)
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own Schema/*.tp files (compiled by ikon app teleport build); each carries its own GROUP_APP_LOCAL opcode and is sent/received as a native type — no JSON marshalling. Delivery is server-controlled and explicit: SendMessageAsync``1 always takes the recipient client session IDs — there is no implicit broadcast to every client. Whether a type travels reliably or unreliably is declared on the .tp schema (unreliable = true), not here.
  static class AppMessaging
    static IDisposable OnMessage<T>(IProtocolMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    static ValueTask SendMessageAsync<T>(IProtocolMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    static ValueTask SendMessageAsync<T>(IProtocolMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
  // Delegate for async event handlers in the app lifecycle.
  delegate AsyncEventHandler<TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler`1<TEventArgs>(TEventArgs e)
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
    ValueTask SendAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Sends audio data through the default speech mixer.
    void SendSpeech(AudioContainer audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Sends audio data through the default speech mixer.
    void SendSpeech(string speechEventId, ReadOnlySpan<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
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
  class AudioInputFrameEventArgs : EventArgs, ICaptureCorrelationArgs
    // Event arguments raised when an incoming audio frame is received
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
    // Event arguments raised when an incoming audio stream begins
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
    // Event arguments raised when an incoming audio stream ends
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
  // Controls when incoming audio frames are output to listeners
  enum AudioInputStreamingMode
    Streaming
    DelayUntilTotalDurationKnown
    DelayUntilIsLast
  // Information about an output audio stream
  class AudioOutputStreamInfo : IEquatable<AudioOutputStreamInfo>
    // Information about an output audio stream
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
  // Bridge between a media stream's CorrelationId and a higher-level handler (typically a UI component such as CaptureButton). For audio it dispatches from frame edges (IsFirst/IsLast) so registered callbacks always run before any subsequent AudioInputFrameAsync handler sees a frame from that segment. For video it dispatches from stream begin/end events. In both cases this eliminates the race that previously existed between the UI action dispatch path and the media transport path.
  static class CaptureCorrelationBridge
    // Register a handler that fires when a stream/segment with the given correlation id starts.
    static void RegisterStart(string correlationId, Func<ICaptureCorrelationArgs, Task> handler)
    // Register a handler that fires when a stream/segment with the given correlation id ends.
    static void RegisterStop(string correlationId, Func<ICaptureCorrelationArgs, Task> handler)
    // Remove handlers registered for the given correlation id.
    static void Unregister(string correlationId)
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
    // Represents a contact picked from the client's contact list.
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    // The contact's email addresses.
    IReadOnlyList<string> Emails { get; init; }
    // The contact's names.
    IReadOnlyList<string> Names { get; init; }
    // The contact's phone numbers.
    IReadOnlyList<string> Phones { get; init; }
  // Provides convenient access to pre-agreed client-side functions. These functions are registered by clients (e.g., TypeScript SDK) and can be called from the server.
  static class ClientFunctions
    // Captures a single image from the client's camera.
    static Task<ClientImageCapture> CaptureImageAsync(int targetId, ClientImageCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, CancellationToken cancellationToken = null)
    // Requests the client to exit fullscreen mode.
    static Task<bool> ExitFullscreenAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> ExitFullscreenAsync(CancellationToken cancellationToken = null)
    // Gets the current battery level on the client.
    static Task<int?> GetBatteryLevelAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<int?> GetBatteryLevelAsync(CancellationToken cancellationToken = null)
    // Gets the browser language preference from the client.
    static Task<string?> GetLanguageAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetLanguageAsync(CancellationToken cancellationToken = null)
    // Gets the current GPS location from the client.
    static Task<ClientLocation?> GetLocationAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<ClientLocation?> GetLocationAsync(CancellationToken cancellationToken = null)
    // Gets the list of available media input devices on the client.
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(CancellationToken cancellationToken = null)
    // Gets the current network connection type on the client.
    static Task<string?> GetNetworkTypeAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetNetworkTypeAsync(CancellationToken cancellationToken = null)
    // Gets the currently selected UI theme from the client.
    static Task<string?> GetThemeAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetThemeAsync(CancellationToken cancellationToken = null)
    // Gets the browser timezone from the client.
    static Task<string?> GetTimezoneAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetTimezoneAsync(CancellationToken cancellationToken = null)
    // Gets the current browser URL path and query string from the client.
    static Task<string?> GetUrlAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetUrlAsync(CancellationToken cancellationToken = null)
    // Gets the current page visibility state on the client.
    static Task<string?> GetVisibilityAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetVisibilityAsync(CancellationToken cancellationToken = null)
    // Prevents or allows the screen to sleep on the client.
    static Task<bool> KeepScreenAwakeAsync(int targetId, bool enabled, CancellationToken cancellationToken = null)
    static Task<bool> KeepScreenAwakeAsync(bool enabled, CancellationToken cancellationToken = null)
    // Prompts the client to show its login UI (deferred login flow).
    static Task<bool> LoginShowAsync(int targetId, string? reason = null, CancellationToken cancellationToken = null)
    // Clears the auth session and reloads the page, returning the client to the login screen.
    static Task<bool> LogoutAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> LogoutAsync(CancellationToken cancellationToken = null)
    // Opens an external URL in a new browser tab on the client.
    static Task<bool> OpenExternalUrlAsync(int targetId, string url, CancellationToken cancellationToken = null)
    static Task<bool> OpenExternalUrlAsync(string url, CancellationToken cancellationToken = null)
    // Plays a sound on the client from a URL.
    static Task<string?> PlaySoundAsync(int targetId, string url, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    // Plays a sound on the client from a byte array. The sound data is cached per session, so subsequent calls with the same data will not re-transmit the audio.
    static Task<string?> PlaySoundAsync(int targetId, byte[] data, string mimeType, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string?> PlaySoundAsync(string url, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string?> PlaySoundAsync(byte[] data, string mimeType, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    // Requests the client to enter fullscreen mode.
    static Task<bool> RequestFullscreenAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> RequestFullscreenAsync(CancellationToken cancellationToken = null)
    // Scrolls the page to a specific position on the client.
    static Task<bool> ScrollToAsync(int targetId, double x, double y, bool smooth = false, CancellationToken cancellationToken = null)
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, CancellationToken cancellationToken = null)
    // Updates the UI theme on the client.
    static Task<bool> SetThemeAsync(int targetId, string theme, bool persist = true, CancellationToken cancellationToken = null)
    static Task<bool> SetThemeAsync(string theme, bool persist = true, CancellationToken cancellationToken = null)
    // Updates the browser URL without triggering a page reload.
    static Task<bool> SetUrlAsync(int targetId, string url, bool replace = false, bool preserveQueryParams = false, CancellationToken cancellationToken = null)
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, CancellationToken cancellationToken = null)
    // Starts audio capture on the client from the microphone.
    static Task<string> StartAudioCaptureAsync(int targetId, ClientAudioCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, CancellationToken cancellationToken = null)
    // Starts video capture on the client from camera or screen.
    static Task<string> StartVideoCaptureAsync(int targetId, ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, CancellationToken cancellationToken = null)
    // Stops a media capture on the client by its stream ID.
    static Task<bool> StopCaptureAsync(int targetId, string streamId, CancellationToken cancellationToken = null)
    static Task<bool> StopCaptureAsync(string streamId, CancellationToken cancellationToken = null)
    // Stops a playing sound on the client.
    static Task<bool> StopSoundAsync(int targetId, string playbackId, CancellationToken cancellationToken = null)
    static Task<bool> StopSoundAsync(string playbackId, CancellationToken cancellationToken = null)
    // Triggers haptic feedback on supported devices.
    static Task<bool> VibrateAsync(int targetId, string pattern, CancellationToken cancellationToken = null)
    static Task<bool> VibrateAsync(string pattern, CancellationToken cancellationToken = null)
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
    // Event arguments for the ClientJoinedAsync event.
    ctor(Context clientContext)
    // Gets the context of the client that joined.
    Context ClientContext { get; }
    // Gets the session ID of the client that joined.
    int ClientSessionId { get; }
    // Gets the user ID of the client that joined, or an empty string if not authenticated.
    string UserId { get; }
  // Event arguments for the ClientLeftAsync event.
  class ClientLeftEventArgs : EventArgs
    // Event arguments for the ClientLeftAsync event.
    ctor(Context clientContext)
    // Gets the context of the client that left.
    Context ClientContext { get; }
    // Gets the session ID of the client that left.
    int ClientSessionId { get; }
    // Gets the user ID of the client that left, or an empty string if not authenticated.
    string UserId { get; }
  // Represents a geolocation with latitude, longitude, and accuracy in meters.
  sealed class ClientLocation : IEquatable<ClientLocation>
    // Represents a geolocation with latitude, longitude, and accuracy in meters.
    ctor(double Latitude, double Longitude, double Accuracy)
    // The accuracy of the coordinates in meters.
    double Accuracy { get; init; }
    // The latitude coordinate.
    double Latitude { get; init; }
    // The longitude coordinate.
    double Longitude { get; init; }
  static class ClientMediaCaptureSerializer
    static string? SerializeAudioOptions(ClientAudioCaptureOptions? options)
    static string? SerializeImageOptions(ClientImageCaptureOptions? options)
    static string? SerializeVideoOptions(ClientVideoCaptureOptions? options)
  // Represents a media input device available on the client.
  sealed class ClientMediaDevice : IEquatable<ClientMediaDevice>
    // Represents a media input device available on the client.
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
    // True if user has admin role
    bool IsAdmin { get; }
    // True if user is a guest (anonymous/unauthenticated)
    bool IsGuest { get; }
    // True if user has moderator role
    bool IsModerator { get; }
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
    TAttributes GetAttributes<TAttributes>() where TAttributes : IProfileAttributes, new()
    // Check if user has a specific built-in role
    bool HasRole(UserRole role)
    // Check if user has a specific role by string name
    bool HasRole(string role)
    bool HasRole<TRole>(TRole role) where TRole : Enum
  // Manages client profiles for an AI app. Automatically loads profiles when clients join and provides sync access to cached profile data.
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
    TAttributes GetAttributes<TAttributes>(Context clientContext) where TAttributes : IProfileAttributes, new()
    // Get profile for a connected client. Returns cached profile (guaranteed available after client joined).
    ClientProfile GetProfile(Context clientContext)
    // Check if client has a specific built-in role
    bool HasRole(Context clientContext, UserRole role)
    // Check if client has a specific role by string name
    bool HasRole(Context clientContext, string role)
    bool HasRole<TRole>(Context clientContext, TRole role) where TRole : Enum
    // Check if client is an admin
    bool IsAdmin(Context clientContext)
    // Check if client is a guest (anonymous/unauthenticated)
    bool IsGuest(Context clientContext)
    // Check if client is a moderator
    bool IsModerator(Context clientContext)
    // Refresh a client's profile from the backend
    Task RefreshProfileAsync(Context clientContext)
    // Refresh a profile from the backend by userId
    Task RefreshProfileAsync(string userId)
    // Remove a role from a client
    Task RemoveRoleAsync(Context clientContext, UserRole role)
    // Remove a role from a client using string role name
    Task RemoveRoleAsync(Context clientContext, string role)
    // Require admin role. Throws if not.
    void RequireAdmin(Context clientContext)
    // Require moderator role. Throws if not.
    void RequireModerator(Context clientContext)
    // Require that the client has the specified role. Throws if not.
    void RequireRole(Context clientContext, UserRole role)
    // Require that the client has the specified role. Throws if not.
    void RequireRole(Context clientContext, string role)
    Task SetAttributesAsync<TAttributes>(Context clientContext, TAttributes attrs) where TAttributes : IProfileAttributes
    // Set roles for a client
    Task SetRolesAsync(Context clientContext, IEnumerable<UserRole> roles)
    // Set roles for a client using string role names
    Task SetRolesAsync(Context clientContext, IEnumerable<string> roles)
    // Try to get profile from cache. Returns null if not loaded.
    ClientProfile? TryGetProfile(Context clientContext)
    // Try to get profile from cache by userId. Returns null if not loaded.
    ClientProfile? TryGetProfile(string userId)
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
  static class Constants
    static string DarkTheme
    static string LightTheme
  // Platform email surface for an Ikon app — sending custom emails through the platform mailer and reading inbound emails delivered to the app's space. Accessed via app.Email. All operations require the app's organisation/space to have the Email feature enabled; calls against a non-entitled space throw FeatureNotEnabledException .
  sealed class EmailService
    // Removes an inbound email and frees its attachment storage. Idempotent — deleting a missing message succeeds silently.
    Task DeleteAsync(string id, CancellationToken ct = null)
    // Streams a decrypted attachment from the platform. The returned EmailAttachmentDownload owns the content stream — dispose it (e.g. await using) when done.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = null)
    // Lazily enumerates all received emails matching query , transparently following pages until exhausted. Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = null)
    // Fetches a single page of received emails for the app's space. Paginate by passing the returned NextCursor back as Cursor .
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = null)
    // Fetches a single inbound email with decrypted body and parsed envelope.
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = null)
    // Sends a custom HTML email through the platform mailer. The platform sets the visible From address; pass ReplyTo to direct replies elsewhere. The send is enqueued for asynchronous delivery — a successful return means the platform has accepted the request, not that the recipient has received the message. Transient delivery failures are retried server-side. The total payload size (subject, body, attachments, metadata) is capped at roughly 10 MB.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = null)
  // Shared base for the two developer-facing inbound HTTP surfaces, [Rest] and [Mcp]. They differ only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients; addressing, path templating, identity binding, auth, and abuse-control are identical and live here so there is exactly one place to reason about them.
  abstract class EndpointAttribute : Attribute
    // Built-in authorization for this endpoint, resolved at the gateway edge before (and without) provisioning the app. Defaults to Grant (a signed grant URL). Set AuthPolicy instead to name a custom /router/ policy.
    EndpointAuth Auth { get; init; }
    // Name of a custom /router/ edge policy that authorizes this endpoint (an apiKey/hmac/ipAllow helper you defined in router/index.ts). When set (non-empty) it takes precedence over Auth . Authorization lives in /router/, the single auth surface — not in C#.
    string? AuthPolicy { get; init; }
    // External path under the space domain (after {space}.ikonai.app/api). Optional: when omitted (empty) the path is derived from the method name (kebab-cased) — /{method} on the app class, /{cell-type}/{method} on a cell. A leading-slash path is absolute; a relative form ("bump") is resolved against the owner's auto-derived mount point at build time. Route params use {name} syntax. A {name} whose name matches a field of the owner's SessionIdentity record binds into the routing identity (the extrinsic resource the caller names); other {name} segments bind as ordinary handler parameters. Reserved paths the developer must NOT declare: /.well-known/* (RFC), and the /ikon/* + /api subtrees (platform-owned).
    string Path { get; }
    // The effective /router/ policy name this endpoint authorizes with: AuthPolicy when set, otherwise the lower-cased Auth built-in (grant/public/deny). Mirrors the manifest's resolution so runtime discovery and the bundle manifest agree.
    string ResolveAuthPolicy()
  // The built-in authorization for an endpoint — the discoverable, no-/router/-needed options. For a custom edge policy (an apiKey/hmac/ipAllow helper you defined in /router/), set AuthPolicy to its name instead.
  enum EndpointAuth
    Grant
    Public
    Deny
  // Information about an HTTP endpoint exposed by the app — an [HttpGet]/[HttpPost]/[Mcp] surface. Returned by Endpoints for developer convenience.
  sealed class EndpointInfo
    ctor()
    // The cell type for a substrate-cell endpoint (empty for app + AppProcess-cell endpoints). When non-empty, the gateway cell-routes the request to that cell's partitioned instance, keyed by the cell's IdentityFields in the URL; empty means the endpoint resolves to the app instance.
    string CellType { get; set; }
    // The endpoint's registry name — {Owner}_{Method} for typed endpoints (or the explicit FunctionAttribute.Name override). The backend resolves this name when routing.
    string FunctionName { get; set; }
    // The bare public URL for this endpoint under the space domain ({space}.ikonai.app/api/{path}), templated where the path has open {segment}s. It carries NO grant: a public endpoint is callable as-is; a grant/policy endpoint needs a working, identity-bound URL from IApp.MintUrl. The backend reverse-proxies to this instance — cold-starting it in the cloud, or routing to a registered local run.
    string PublicUrl { get; set; }
  sealed class FileUploadCallbackSet
    ctor()
    Func<FileUploadChunkArgs, Task>? OnChunkReceived
    Func<FileUploadCompleteArgs, Task>? OnUploadComplete
    Func<FileUploadErrorArgs, Task>? OnUploadError
    Func<FileUploadPreStartArgs, Task<FileUploadPreStartResult>>? OnUploadPreStart
    Func<FileUploadProgressArgs, Task>? OnUploadProgress
    Func<FileUploadStartArgs, Task<FileUploadStartResult>>? OnUploadStart
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
  sealed class FileUploadHandler : IDisposable
    ctor(IAppBase app)
    void Dispose()
    void RegisterCallbacks(string uploadActionId, FileUploadCallbackSet callbackSet)
  sealed class FileUploadPreStartArgs : IEquatable<FileUploadPreStartArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, Func<string?, Task> Cancel)
    Func<string?, Task> Cancel { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadPreStartResult : IEquatable<FileUploadPreStartResult>
    ctor()
    ctor(string? assetUri)
    ctor(bool accepted, string? assetUri = null)
    bool Accepted { get; set; }
    string? AssetUri { get; set; }
  sealed class FileUploadProgressArgs : IEquatable<FileUploadProgressArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, double ProgressPercentage, long BytesUploaded)
    long BytesUploaded { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    double ProgressPercentage { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadStartArgs : IEquatable<FileUploadStartArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string Hash)
    string FileName { get; init; }
    string Hash { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadStartResult : IEquatable<FileUploadStartResult>
    ctor()
    ctor(string? assetUri)
    ctor(bool accepted, string? assetUri = null)
    bool Accepted { get; set; }
    string? AssetUri { get; set; }
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
    // Serializable view of an inbound HTTP request — its method, path, query, headers, and raw body. The dispatcher constructs one per inbound request; a handler reads it (e.g. via HttpCallContext) for the untrusted inputs the typed binding doesn't surface, such as verifying a webhook signature inline.
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  // Typed return value from an HttpMethodAttribute -annotated method. Endpoints can return any serializable type for an automatic 200 + JSON response, or return an HttpResult when they need control over status code, content type, or custom body serialization.
  sealed class HttpResult : IEquatable<HttpResult>
    // Typed return value from an HttpMethodAttribute -annotated method. Endpoints can return any serializable type for an automatic 200 + JSON response, or return an HttpResult when they need control over status code, content type, or custom body serialization.
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
  interface IAppBase : IProtocolMessageChannel
    // Gets the background work tracker that prevents server idle shutdown while work is in progress.
    BackgroundWork BackgroundWork { get; }
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
    // Gets the configured maximum memory limit in megabytes for this server instance.
    int MaxMemoryLimitMb { get; }
    // The Parallax mounts this app renders. Each mount produces an independent UI stream addressable from a host UI as <ParallaxView mount="..." />. Defaults to a single mount named "ikon-ui" — the wire-identical shape of every Ikon app today. Apps with multiple panels or mixed Parallax/external regions can replace the value with a longer list at any time; the render loop reacts and emits UIStreamBegin/UIStreamEnd for additions and removals.
    Reactive<IReadOnlyList<string>> Mounts { get; }
    // Gets the navigation helper for managing URL paths and listening to URL changes.
    Navigation Navigation { get; }
    // Gets the reactive wrapper around GlobalState that provides change notifications.
    ReactiveGlobalState ReactiveGlobalState { get; }
    // Gets the reactive root that manages per-client reactive graphs and update cycles.
    ReactiveRoot ReactiveRoot { get; }
    // Gets the secrets (tokens, API keys, passwords) configured for this app. Values are fetched from the Ikon backend once at app startup and exposed synchronously; changes made via ikon app secret set while the app is running only take effect after a restart.
    Secrets Secrets { get; }
    // Creates a platform-managed eID-backed PAdES signature order for the supplied document(s). The platform navigates the signer's browser to the signing-ceremony URL through the existing client UI surface, awaits the asynchronous packaging completion, and resolves the returned task with the signed PDF and evidence metadata. The returned bytes are the long-term-validation PAdES PDF when the chosen scheme produces it; apps should persist them as the system of record because the platform's session retention is short.
    abstract Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = null)
    // Mint a working, identity-bound URL for one endpoint — the single way to get a callable URL for a grant (default) or policy endpoint. The returned URL is the endpoint's PublicUrl with any pinned {placeholder} path segments substituted and a signed ?ikon-grant= appended. identity (an anonymous object, e.g. new { DocumentId = "doc-42" }, or a string dictionary) PINS those identity fields into the grant; fields you omit stay open {captures} for the caller to fill. Omitting identity entirely ( null ) pins THIS instance's own session identity, so the URL routes back to this app instance — the common case. Grants are non-expiring by default — pass expiresIn only for an ephemeral link, and an optional group to revoke a batch together via RevokeGroupAsync . Re-minting the same stable (non-expiring) URL returns an identical URL, so it survives restarts.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = null)
    // Mint working URLs for several endpoints sharing one pinned identity, in a single backend round-trip. Returns a map keyed by the endpoints you passed. See MintUrlAsync .
    virtual Task<IReadOnlyDictionary<string, MintedUrl>> MintUrlsAsync(IEnumerable<string> endpoints, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = null)
    // Dynamically requests a raw TCP/TLS/UDP endpoint. Returns a RelayEndpoint whose LocalPort a listener should bind to; the endpoint is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the returned endpoint to release it. For HTTP/HTTPS endpoints use AppEndpointHost .
    abstract Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", CancellationToken ct = null)
    // Requests a fresh strong-authentication step-up challenge for the current user. Navigates the client browser to the platform's configured identity provider through the existing client UI surface, waits for the user to complete the challenge, and returns the platform-signed step-up assertion JWT. Apps must verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier .
    abstract Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = null)
    // Revoke every URL minted under a shared group tag.
    virtual Task RevokeGroupAsync(string group, CancellationToken ct = null)
    // Revoke a single minted URL by its GrantId .
    virtual Task RevokeUrlAsync(string grantId, CancellationToken ct = null)
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
    static void OnClientJoined(IAppBase app, Func<Context, Task> handler)
    static void OnClientJoined<TSessionIdentity, TClientParameters>(IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to ClientLeftAsync with a handler that receives the departing client's Context directly.
    static void OnClientLeft(IAppBase app, Func<Context, Task> handler)
    static void OnClientLeft<TSessionIdentity, TClientParameters>(IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to MessageReceivedAsync with a handler that receives the protocol message directly.
    static void OnMessageReceived(IAppBase app, Func<ProtocolMessage, Task> handler)
    // Subscribe to StartingAsync with a zero-arg async handler. The Starting event carries no data — there's nothing to forward.
    static void OnStarting(IAppBase app, Func<Task> handler)
    // Subscribe to StoppingAsync with a zero-arg async handler.
    static void OnStopping(IAppBase app, Func<Task> handler)
  // Legacy app host interface providing access to app configuration (appVersion=1).
  interface IApp<TConfig> : IAppBase, IProtocolMessageChannel
    // Gets the app configuration provided by the developer.
    TConfig Config { get; }
  // App host interface providing typed session identity and client parameters.
  interface IApp<TSessionIdentity, TClientParameters> : IAppBase, IProtocolMessageChannel
    // Gets the typed parameters for the current client (determined by ReactiveScope). Must be called inside UI.Root() or a ReactiveScope context.
    TClientParameters ClientParameters { get; }
    // Gets the collection of connected clients with typed parameters. Automatically synced with GlobalState .
    IClientCollection<TClientParameters> Clients { get; }
    // Gets the typed session identity used to determine app instance routing.
    TSessionIdentity SessionIdentity { get; }
  // Common shape used by CaptureCorrelationBridge to dispatch capture start/stop callbacks. Implemented by audio frame args (used for per-segment dispatch) and video stream begin/end args (used for per-stream dispatch).
  interface ICaptureCorrelationArgs
    Context ClientContext { get; }
    string? CorrelationId { get; }
    string StreamId { get; }
  // Collection interface for accessing connected clients. Iterable for the common "broadcast / fan-out" pattern (`foreach (var client in app.Clients)`), indexable by session ID for direct lookups, and exposes Ids when only the connected-session-ids are needed.
  interface IClientCollection<TClientParameters> : IEnumerable, IEnumerable<IClient<TClientParameters>>
    // Gets the number of currently connected clients.
    int Count { get; }
    // Gets the connected client session IDs as an enumerable. Convenience for code that just needs the IDs without the full client objects — e.g. `foreach (var id in app.Clients.Ids) { _scores[id] = 0; }`.
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters>? Item { get; }
    // Alias for Ids — dictionary-shaped mental model. Generated code reaches for both interchangeably.
    IEnumerable<int> Keys { get; }
  // Interface representing a connected client with typed parameters.
  interface IClient<TClientParameters>
    // Gets the typed parameters for this client.
    TClientParameters Parameters { get; }
  // Marker interface for custom profile attribute classes. Implement this interface on classes that define custom profile attributes.
  interface IProfileAttributes
  // Marks a method on an app or cell as an MCP tool. The framework discovers these at startup, reflects the method's parameters into a JSON Schema, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP tools/call requests to it.
  class McpAttribute : EndpointAttribute
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
    // Marks a method on a cell as an MCP-exposed resource — read-only data addressed by a URI. The framework reflects the method's parameters into a URI template, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP resources/read requests against the matching URI.
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
    // Event arguments for the MessageReceivedAsync event.
    ctor(ProtocolMessage message)
    // Gets the received protocol message.
    ProtocolMessage Message { get; }
  // A minted endpoint URL: the working Url (the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended), the GrantId to revoke it by, and the optional ExpiresAt when a TTL was requested (grants are non-expiring by default).
  sealed class MintedUrl : IEquatable<MintedUrl>
    // A minted endpoint URL: the working Url (the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended), the GrantId to revoke it by, and the optional ExpiresAt when a TTL was requested (grants are non-expiring by default).
    ctor(string Url, string GrantId, DateTimeOffset? ExpiresAt)
    DateTimeOffset? ExpiresAt { get; init; }
    string GrantId { get; init; }
    string Url { get; init; }
  static class ClientFunctions.Names
    static string CaptureImage
    static string ExitFullscreen
    static string GetBatteryLevel
    static string GetLanguage
    static string GetLocation
    static string GetMediaDevices
    static string GetNetworkType
    static string GetTheme
    static string GetTimezone
    static string GetUrl
    static string GetVisibility
    static string KeepScreenAwake
    static string LoginShow
    static string Logout
    static string OpenExternalUrl
    static string PlaySound
    static string RequestFullscreen
    static string ScrollTo
    static string SetTheme
    static string SetUrl
    static string StartAudioCapture
    static string StartVideoCapture
    static string StopCapture
    static string StopSound
    static string Vibrate
  class Navigation : IReactiveWithState
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
  // A reactive value persisted globally for the app within its space. Shared across all session identities and users; one value per app deployment.
  class PersistentReactive<T> : Reactive<T>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A reactive value persisted per session identity. Apps with the same routing key share the same value; different routing keys have isolated values.
  class PersistentSessionReactive<T> : Reactive<T>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A reactive value persisted per user, partitioned at runtime by UserScope . Each user sees their own value across all of their client sessions.
  class PersistentUserReactive<T> : Reactive<T, UserScope>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
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
  // Manages per-client reactive graphs and update cycles for an Ikon app. Automatically stops when the app's StoppingAsync event fires.
  class ReactiveRoot
    // Creates a new reactive root for the specified app host.
    ctor(IAppBase app, int updateIntervalMs = 1000)
    // Gets the reactive manager that coordinates all reactive objects in the app.
    ReactiveManager ReactiveManager { get; }
    Task RunAsync(Func<Task> render, Func<Context, bool>? filter = null)
  // Event arguments raised when speech has been recognized from a captured audio stream.
  sealed class SpeechRecognizedEventArgs : EventArgs
    // Event arguments raised when speech has been recognized from a captured audio stream.
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
  class VideoInputFrameEventArgs : EventArgs, ICaptureCorrelationArgs
    // Event arguments raised when an incoming video frame is received
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
  class VideoInputStreamBeginEventArgs : EventArgs, ICaptureCorrelationArgs
    // Event arguments raised when an incoming video stream begins
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
  class VideoInputStreamEndEventArgs : EventArgs, ICaptureCorrelationArgs
    // Event arguments raised when an incoming video stream ends
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
    // Information about an output video stream
    ctor(string StreamId, int TrackId, VideoCodec Codec, int Width, int Height, double Framerate)
    VideoCodec Codec { get; init; }
    double Framerate { get; init; }
    int Height { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
    int Width { get; init; }
  // A thin wrapper that holds the user’s configuration. This wrapper derives from BasePluginConfig so that it can be used internally by BasePlugin. Plugin developers only see the wrapped TConfig.
  class WrapperConfig<TConfig> : BasePluginConfig
    ctor()
    // A thin wrapper that holds the user’s configuration. This wrapper derives from BasePluginConfig so that it can be used internally by BasePlugin. Plugin developers only see the wrapped TConfig.
    ctor(TConfig userConfig)
    TConfig AppConfig { get; set; }

namespace Ikon.App.Auth
  // OAuth resource-server configuration the platform reads to advertise the protected-resource discovery document (RFC 9728), so an MCP client knows which authorization server to obtain a bearer token from. Bearer-token validation itself would be an edge /router/ bearer policy evaluated at the gateway before provisioning — not an in-process cell — but no such policy is implemented yet (the fail-closed oauth helper was removed).
  static class OAuthAuth
    // Configured issuer URL (IKON_OAUTH_ISSUER) — returned by the protected-resource discovery document. Null when unconfigured.
    static string? ConfiguredIssuer { get; }

namespace Ikon.App.Cells
  // Marks a class as a cell — a headless app addressed by a SessionIdentity record declared inside the class. Discovered by CellHost at startup via reflection over loaded assemblies.
  sealed class CellAttribute : Attribute
    ctor()
    // Number of concurrent instances per addressable key. Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin Resolve``1 across them. For globals (parameterless SessionIdentity) the N instances are eager-spawned at host construction — the load-balanced auth-cell pattern. For keyed cells the N instances are spawned together on first access; sharded keyed cells must tolerate eventual consistency between shards (cells should hold no per-instance state, or persist shared state through an external store).
    int Capacity { get; init; }
    // How long a keyed cell may remain idle before EvictIdle removes it from the directory. Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
    // Where this cell type is hosted. AppProcess (the default) keeps the cell in the app's own `CellHost` — every app process has its own copies, state is not shared across processes. Substrate declares that the cell should be hosted on the platform's cell-deployment substrate, where one instance per (cell-type, SessionIdentity) is shared across all app processes that connect.
    CellProcessScope ProcessScope { get; init; }
  // What the cell-client factory needs to open a standard-SDK connection to a substrate cell-host: the cell type's simple name and its SessionIdentity-record field values.
  sealed class CellConnectRequest : IEquatable<CellConnectRequest>
    // What the cell-client factory needs to open a standard-SDK connection to a substrate cell-host: the cell type's simple name and its SessionIdentity-record field values.
    ctor(string CellTypeName, IReadOnlyDictionary<string, string> Identity)
    string CellTypeName { get; init; }
    IReadOnlyDictionary<string, string> Identity { get; init; }
  // A live standard-SDK connection from an app process to a substrate cell-host IkonServer, paired with the ReactiveRegistry that mirrors the cell's Reactive<T> state. Created lazily by Cells on first need and shared by every SubstrateCellProxy`1 for the same (CellType, SessionIdentity).
  sealed class CellConnection : IAsyncDisposable
    // The connected SDK client to the cell-host IkonServer.
    IkonClient Client { get; }
    // Reactive-subscription layer over Client 's function registry.
    ReactiveRegistry Reactive { get; }
    ValueTask DisposeAsync()
  // In-process directory + spawn substrate for CellAttribute -decorated types. Maps wire interfaces to cell types at startup, then resolves (cellType, SessionIdentity) to a single shared instance per key.
  sealed class CellHost : IAsyncDisposable
    // Construct a host that scans the supplied assemblies for CellAttribute -decorated types. When assemblies is null, scans every loaded assembly in the current AppDomain. Cells whose SessionIdentity record is parameterless (= global) are eager-spawned at construction so they are always-already-provisioned by the time a request lands.
    ctor(IEnumerable<Assembly>? assemblies = null)
    // Every CellAttribute -decorated type the host discovered during construction. Read-only enumeration used by higher layers (e.g. typed-HTTP-endpoint discovery) that need to iterate cells without owning the directory.
    IReadOnlyCollection<Type> CellTypes { get; }
    // Dispose every cell instance held by the host. Async disposal is preferred per BCL precedence; IDisposable is honored as a fallback. After disposal, Resolve``1 throws ObjectDisposedException .
    ValueTask DisposeAsync()
    // Evict every keyed cell instance whose idle time exceeds its cell type's IdleTtlSeconds . Cells without a configured TTL are never evicted by this method. Awaits DisposeAsync on each evicted instance that implements it; IDisposable is honored as a fallback. Returns the number of instances removed.
    Task<int> EvictIdleAsync()
    // Evict every keyed cell instance whose last access is strictly before cutoffUtc . Globals are never evicted. Awaits DisposeAsync on each evicted instance that implements it; IDisposable is honored as a fallback. Returns the number of instances removed.
    Task<int> EvictIdleOlderThanAsync(DateTime cutoffUtc)
    // The TSessionIdentity type a CellAttribute -decorated cell binds to, inferred from its primary-constructor's ICell`1 parameter. Returns null if the cell doesn't declare an ICell`1 ctor parameter at all.
    static Type? GetSessionIdentityType(Type cellType)
    // True when the identity record has at least one constructor parameter — i.e. the cell is keyed (different instances per identity value). False for parameterless / global identity types whose only constructor is the synthesised record copy-ctor.
    static bool HasIdentityParameters(Type identityType)
    // Register an externally-constructed instance (typically the running App<TSessionIdentity, TClientParameters> plugin) as a singleton cell. The host treats it like any other [Cell] for discovery + dispatch — its public methods show up in CellTypes , HttpEndpointDiscovery, McpToolDiscovery, and McpResourceDiscovery; ResolveByCellTypeName and Resolve``1 return the registered instance directly. The host does NOT construct, evict, or dispose singletons — lifecycle stays with the external owner.
    void RegisterSingleton(object instance)
    TInterface Resolve<TInterface>(object sessionIdentity) where TInterface : class
    // Resolve (or spawn) a cell instance by the cell type's simple name and a SessionIdentity field dict (typically the URL query params from an inbound endpoint). The host constructs the SessionIdentity record from the dict by matching the record's primary-constructor parameter names; missing nullable/default-valued fields use null/their default; missing required fields throw. Returns the cell instance as Object — callers cast to the wire interface they expect or use reflection to invoke methods.
    object ResolveByCellTypeName(string cellTypeName, IReadOnlyDictionary<string, string> sessionIdentityFields)
    // Look up the registered [Cell] concrete type whose wire-interface mapping matches iface . Returns the same type that Resolve``1 would dispatch to. Used by Cells.Connect<TInterface> to consult the cell's CellAttribute (e.g. for ProcessScope ) before deciding between local resolution and substrate-proxy routing.
    bool TryGetCellTypeForInterface(Type iface, out Type cellType)
    // Raised when a NEW cell type appears in the host after construction — specifically when RegisterSingleton registers an instance whose type wasn't already known. Higher layers (IkonServer) that snapshot the topology at build time — e.g. the discovered MCP-tool host and typed-HTTP-endpoint list — subscribe to rebuild those snapshots. This is load-bearing for app-level [Mcp]: the user's [App] instance is registered lazily on first client join (via HttpEndpointRouting.EnsureCellHost), long after the host's initial discovery walk.
    event Action? TopologyChanged
  // The wire-name conventions for cell members. Both the substrate-cell proxy (the caller) and the cell-host's endpoint-wrapper registration (the producer) build these names; keeping the format in one place stops the two sides from drifting apart.
  static class CellNaming
    // The endpoint registry name for a cell's [HttpGet]/[HttpPost] method: {CellType}_{Method}. The manifest carries this flat name as the endpoint's Name; the backend derives the upstream route /{Owner}/{Method} from it.
    static string EndpointFunctionName(Type cellType, string methodName)
    // The SDK function name for a cell's [Function] method: {CellType.FullName}.{Method}. Matches how FunctionRegistry.RegisterFromInstance names instance methods, so a substrate-cell proxy can call them over its SDK connection to the cell-host.
    static string SdkFunctionName(Type cellType, string methodName)
    // The SDK function name a cell-host exposes to advertise the base URL of its AppEndpointHost — the relay tunnel serving the cell's [HttpGet]/[HttpPost] + [Mcp] routes. A SubstrateCellProxy calls it over the cell-host SDK connection to learn where to POST [HttpGet]/[HttpPost] requests directly, instead of going through the cloud endpoint gateway. Producer (the cell-host startup path) and consumer (SubstrateCellProxy) must agree on this name.
    static string CellEndpointBaseUrlFunctionName
  // Where a CellAttribute -decorated type's instances live.
  enum CellProcessScope
    AppProcess
    Substrate
  // Static accessor for the process-wide CellHost plus the wiring substrate-cell proxies need: the endpoint-URL resolver (for [HttpGet]/[HttpPost] methods) and the cell-client factory (for [Function] methods and Reactive<T> state, which ride a standard IkonClient SDK connection to the cell-host).
  static class Cells
    // The currently installed process-wide cell host, or null if none has been installed yet. Use this when you want to reuse the shared host with a graceful fallback. For fail-fast access prefer Connect``1 .
    static CellHost? Current { get; }
    static TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    // Dispose every live cell-host connection. Call on app shutdown. Idempotent.
    static ValueTask DisposeAsync()
    // Install the process-wide cell host. Replaces any previous host (last-call-wins) so tests can swap freely. Also clears the endpoint-URL resolver and the cell-client factory, and drops the connection registry — apps re-register the resolver/factory after each Initialize. Production calls Initialize once at startup, so this only matters in tests that re-run Initialize between scenarios.
    static void Initialize(CellHost host)
    // Register the factory that opens a standard-SDK IkonClient connection to a substrate cell-host. Called by the app host at startup — the app process has the backend context (space id, login) the factory needs. SubstrateCellProxy`1 uses it for [Function]-marked methods and Reactive<T> members; without it, those throw a clear error while [HttpGet]/[HttpPost] methods still work.
    static void SetCellClientFactory(Func<CellConnectRequest, Task<IkonClient>> factory)
    // Register the function that maps a endpoint function name (e.g. "LabCell_IncrementHttp") to its public URL. Called by the app host at startup so SubstrateCellProxy`1 can dispatch a substrate cell's [HttpGet]/[HttpPost] methods over stateless HTTP. Methods the resolver returns no URL for fall through to the SDK connection.
    static void SetEndpointUrlResolver(Func<string, string?> resolver)
    // Reserved key in an SDK connection's parameters that names the substrate cell type to route to. The cell's SessionIdentity-record fields ride alongside it. MUST stay in sync with the cloud's CELL_TYPE_PARAM in cell-routing.ts — that's what ChannelInstanceService.create keys on to provision a cell-host channel-instance.
    static string CellTypeParam
  // Framework handle injected into a cell's primary constructor. Exposes the SessionIdentity the cell was instantiated for; future revisions add lifetime, config, etc.
  interface ICell<TSessionIdentity>
    // The SessionIdentity record value this cell instance is keyed by.
    TSessionIdentity Identity { get; }
  // Runtime DispatchProxy for a [Cell(ProcessScope = Substrate)] cell type. App processes call the cell as if it were local; the proxy hides the network hop and picks a transport per member: [HttpGet]/[HttpPost] methods — dispatched as stateless HTTP POST. The target is the cell-host's own IkonClient -discovered endpoint base URL when available, falling back to the cloud endpoint-gateway URL otherwise.other methods — dispatched over a standard IkonClient SDK connection to the cell-host (the cell must expose them via [Function] / [RegisterAll] so they are callable on the wire).Reactive<T> members — return a cached local read-only mirror fed by an SDK subscription; reads and Changed events work locally, mutations flow through cell methods. The SDK connection is opened lazily on first need. Even a cell reached only through [HttpGet]/[HttpPost] methods opens one once, to discover the cell-host's endpoint base URL.
  class SubstrateCellProxy<TInterface> : DispatchProxy where TInterface : class
    ctor()
    // Build a proxy implementing TInterface for the given substrate cell.
    static TInterface Create(Type cellType, object sessionIdentity, Func<string, string?> endpointUrlResolver)

namespace Ikon.App.Client
  // Thread-safe implementation of IClientCollection`1 that synchronizes with GlobalState .
  class ClientCollection<TClientParameters> : IClientCollection<TClientParameters>, IEnumerable, IEnumerable<IClient<TClientParameters>>
    ctor()
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters>? Item { get; }
    IEnumerator<IClient<TClientParameters>> GetEnumerator()
  // Implementation of IClient`1 representing a connected client with typed parameters.
  class Client<TClientParameters> : IClient<TClientParameters>
    // Implementation of IClient`1 representing a connected client with typed parameters.
    ctor(TClientParameters parameters)
    TClientParameters Parameters { get; }

namespace Ikon.App.Http
  // Per-request context for an HttpMethodAttribute handler currently executing. AsyncLocal so handler code (and anything it calls) can read the request's resolved identity without threading the dict through every method signature. Relationship to other "context" concepts on the platform: SessionIdentity (the typed app/cell record): the routing / instance-partition key. Always present — it's what was used to address the channel-instance this handler runs in. Stable across the cell instance's lifetime.Context (Ikon protocol Context for WS clients): the live client *connection* — sessionId, deviceId, AuthSessionId, UserId from the connect-token. Absent for endpoint/MCP dispatches because there is no live client connection.HttpCallContext.Current (this) and McpCallContext .Current: the *request-scoped overlay* that exposes the per-call resolved identity for handler code to read. Set by the wrapper before the handler runs, cleared after. The point is that handlers reading "who is this call for?" get a non-empty answer on endpoint/MCP-dispatched calls, where the connection-level Context.UserId would be empty. The handler's SessionIdentity record (resolved by CellHost.ResolveByCellTypeName before this context is set) and HttpCallContext.Current.SessionIdentity carry the same information in different shapes: the former is typed and tied to the cell's lifetime; the latter is the raw wire dict tied to the call's lifetime. Headers and RawBody are the UNTRUSTED request inputs, exposed so a handler can do its own logic inline (e.g. verify a Stripe-Signature against the raw body) without a separate auth cell. They must never feed identity resolution — the target instance is already chosen from trusted sources (a signed ikon-grant / policy claims / platform-controlled path+query) before the handler runs, so reading a header cannot retarget the call.
  sealed class HttpCallContext : IEquatable<HttpCallContext>
    // Per-request context for an HttpMethodAttribute handler currently executing. AsyncLocal so handler code (and anything it calls) can read the request's resolved identity without threading the dict through every method signature. Relationship to other "context" concepts on the platform: SessionIdentity (the typed app/cell record): the routing / instance-partition key. Always present — it's what was used to address the channel-instance this handler runs in. Stable across the cell instance's lifetime.Context (Ikon protocol Context for WS clients): the live client *connection* — sessionId, deviceId, AuthSessionId, UserId from the connect-token. Absent for endpoint/MCP dispatches because there is no live client connection.HttpCallContext.Current (this) and McpCallContext .Current: the *request-scoped overlay* that exposes the per-call resolved identity for handler code to read. Set by the wrapper before the handler runs, cleared after. The point is that handlers reading "who is this call for?" get a non-empty answer on endpoint/MCP-dispatched calls, where the connection-level Context.UserId would be empty. The handler's SessionIdentity record (resolved by CellHost.ResolveByCellTypeName before this context is set) and HttpCallContext.Current.SessionIdentity carry the same information in different shapes: the former is typed and tied to the cell's lifetime; the latter is the raw wire dict tied to the call's lifetime. Headers and RawBody are the UNTRUSTED request inputs, exposed so a handler can do its own logic inline (e.g. verify a Stripe-Signature against the raw body) without a separate auth cell. They must never feed identity resolution — the target instance is already chosen from trusted sources (a signed ikon-grant / policy claims / platform-controlled path+query) before the handler runs, so reading a header cannot retarget the call.
    ctor(IReadOnlyDictionary<string, string>? SessionIdentity = null, CancellationToken CancellationToken = null, IReadOnlyDictionary<string, string>? Headers = null, string? RawBody = null)
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
  // Bridges in-process HTTP cell-method dispatch through the active GovernanceScope hook. With no hook active this is a pass-through; with one set, the invocation flows through RunAsync``1 with the structural {CellType}.{Method} subject id so the same Mission gates HTTP and MCP symmetrically.
  static class HttpDispatchGovernance
    static Task<object?> InvokeAsync(MethodInfo handler, Type ownerType, IReadOnlyDictionary<string, object?> args, Func<Task<object?>> invoke, CancellationToken ct = null)
  // Reflective discovery of the typed HTTP surface on a given type: every HttpMethodAttribute method. McpAttribute methods are NOT surfaced here — they are discovered separately by McpToolDiscovery and mounted by the framework both on the /{Type}/mcp multiplexer and as their own per-tool endpoints. Used at startup by the framework to enumerate the typed-HTTP surface of an app class and of every cell type.
  static class HttpEndpointDiscovery
    // Discover every typed HTTP endpoint on ownerType . Methods inherited from base classes are included; static methods and non-public methods are skipped (endpoints must be invokable on a specific instance). Requires an explicit [HttpGet]/[HttpPost].
    static IReadOnlyList<HttpEndpointInfo> ForType(Type ownerType)
    // Discover endpoints across every type in types . Convenience overload for the startup path that has already filtered an assembly's loaded types down to apps and cells.
    static IReadOnlyList<HttpEndpointInfo> ForTypes(IEnumerable<Type> types)
  // Metadata for a single HttpMethodAttribute -annotated method discovered at startup. Carries everything the dispatcher needs at request time: the HTTP method, path template, the name of the /router/ auth policy, the reflected MethodInfo , and the owner Type (an app class or a [Cell] class). Authorization itself runs at the gateway edge (the /router/ policy), not in-process — Auth is metadata carried into the manifest.
  sealed class HttpEndpointInfo : IEquatable<HttpEndpointInfo>
    // Metadata for a single HttpMethodAttribute -annotated method discovered at startup. Carries everything the dispatcher needs at request time: the HTTP method, path template, the name of the /router/ auth policy, the reflected MethodInfo , and the owner Type (an app class or a [Cell] class). Authorization itself runs at the gateway edge (the /router/ policy), not in-process — Auth is metadata carried into the manifest.
    ctor(string Method, string Path, string? Auth, MethodInfo Handler, Type OwnerType)
    string? Auth { get; init; }
    MethodInfo Handler { get; init; }
    string Method { get; init; }
    Type OwnerType { get; init; }
    string Path { get; init; }
  // Which wire protocol an HTTP-class endpoint speaks. Addressing, path templating, identity binding, auth, and abuse-control are identical across the kinds — only the handler stack (typed bind vs MCP JSON-RPC) and the schema advertised to clients differ. [Rest] maps to Rest and [Mcp] to Mcp ; both ride the same AppEndpointHost .
  enum HttpEndpointKind
    Rest
    Mcp
  // Compiled representation of a Path template. Each segment is either a literal or a {name} capture; matching is exact on segment count, ordinal on literals, case-insensitive on capture names. No wildcard / catch-all support; that's a deliberate simplification — the typed-endpoint surface is meant to be explicit.
  sealed class RouteTemplate
    // Names of every {capture} segment, in path order.
    IReadOnlyList<string> CaptureNames { get; }
    // The literal path with capture syntax preserved (e.g. spaces/{spaceId}/messages).
    string Pattern { get; }
    static RouteTemplate Parse(string template)
    // Try to match path against this template. On success, returns true and populates captures with the captured values keyed by name. On failure, returns false and captures is empty.
    bool TryMatch(string path, out IReadOnlyDictionary<string, string> captures)
  // RFC-6570 Level-1 URI template — {name} placeholders only, no list/operator modifiers. Compile once at registration time; match incoming URIs back to placeholder values. Used by McpResourceBridge to route resources/read URIs to the owning cell method.
  sealed class UriTemplate
    bool IsStatic { get; }
    IReadOnlyList<string> PlaceholderNames { get; }
    string Template { get; }
    // Match an incoming URI against the template. Returns the placeholder bindings on success, or null if the URI doesn't fit the template shape. Placeholder values are non-empty and do not cross the next literal segment.
    IReadOnlyDictionary<string, string>? Match(string uri)
    static UriTemplate Parse(string template)

namespace Ikon.App.Mcp
  sealed class CallToolParams : IEquatable<CallToolParams>
    ctor()
    JsonElement Arguments { get; init; }
    string Name { get; init; }
  sealed class CallToolResult : IEquatable<CallToolResult>
    ctor(IReadOnlyList<ToolContent> Content, bool IsError)
    IReadOnlyList<ToolContent> Content { get; init; }
    bool IsError { get; init; }
  // Params of a notifications/cancelled notification. RequestId identifies the in-flight call the client wants to abort.
  sealed class CancelledNotificationParams : IEquatable<CancelledNotificationParams>
    // Params of a notifications/cancelled notification. RequestId identifies the in-flight call the client wants to abort.
    ctor(JsonElement RequestId, string? Reason = null)
    string? Reason { get; init; }
    JsonElement RequestId { get; init; }
  // Transport-facing sink for server-initiated JSON-RPC notifications. McpHost calls this to push progress updates and similar events that aren't the response to a specific request.
  interface IMcpNotificationSink
    abstract Task SendNotificationAsync(string method, object params, CancellationToken ct)
  sealed class InitializeResult : IEquatable<InitializeResult>
    ctor(string ProtocolVersion, McpCapabilities Capabilities, McpServerInfo ServerInfo)
    McpCapabilities Capabilities { get; init; }
    string ProtocolVersion { get; init; }
    McpServerInfo ServerInfo { get; init; }
  sealed class JsonRpcError : IEquatable<JsonRpcError>
    ctor(int Code, string Message, JsonElement? Data = null)
    int Code { get; init; }
    JsonElement? Data { get; init; }
    string Message { get; init; }
  // JSON-RPC 2.0 + MCP message types. Minimal subset for an MCP server that answers initialize, tools/list, and tools/call. Reads / writes are routed through McpJson .
  sealed class JsonRpcRequest : IEquatable<JsonRpcRequest>
    ctor()
    JsonElement? Id { get; init; }
    bool IsNotification { get; }
    string JsonRpc { get; init; }
    string Method { get; init; }
    JsonElement? Params { get; init; }
  sealed class JsonRpcResponse : IEquatable<JsonRpcResponse>
    ctor()
    JsonRpcError? Error { get; init; }
    JsonElement? Id { get; init; }
    string JsonRpc { get; init; }
    object? Result { get; init; }
    static JsonRpcResponse Fail(JsonElement? id, int code, string message)
    static JsonRpcResponse Ok(JsonElement? id, object? result)
  // Builds JSON Schema objects from .NET reflection metadata (parameter lists, property bags). Used by McpToolBridge to derive an MCP tool's inputSchema from the method's parameter list. Defers per-type schema generation to JsonSchemaGenerator so MCP tools, Emerge.Run response schemas, and Ikon.AI tool definitions all speak the same dialect (currently OpenAI/Anthropic-strict 2020-12).
  static class JsonSchemaBuilder
    // Build an object-shaped JSON Schema describing the named property bag implied by a method's parameter list. Each non-optional parameter becomes a required property whose schema is derived from its type via JsonSchemaGenerator ; parameters with a default value are optional. [Description] attributes on parameters are surfaced as the property's description.
    static JsonElement BuildObjectSchema(IReadOnlyList<ParameterInfo> parameters)
    // As BuildObjectSchema but with an extra set of always-required properties prepended (used by the MCP bridge to inject a keyed cell's identity fields).
    static JsonElement BuildObjectSchema(IReadOnlyList<ParameterInfo> parameters, IReadOnlyList<ValueTuple<string, Type, string?>> extraRequired)
  sealed class ListResourceTemplatesResult : IEquatable<ListResourceTemplatesResult>
    ctor(IReadOnlyList<ResourceTemplate> ResourceTemplates)
    string? NextCursor { get; init; }
    IReadOnlyList<ResourceTemplate> ResourceTemplates { get; init; }
  sealed class ListResourcesResult : IEquatable<ListResourcesResult>
    ctor(IReadOnlyList<Resource> Resources)
    string? NextCursor { get; init; }
    IReadOnlyList<Resource> Resources { get; init; }
  sealed class ListToolsParams : IEquatable<ListToolsParams>
    ctor()
    // Opaque pagination cursor returned in a previous NextCursor . Clients pass it back verbatim to fetch the next page; first page omits it.
    string? Cursor { get; init; }
  sealed class ListToolsResult : IEquatable<ListToolsResult>
    ctor(IReadOnlyList<ToolDefinition> Tools)
    // Set when more tools remain. Clients echo this back in Cursor to get the next page. null when this is the last page.
    string? NextCursor { get; init; }
    IReadOnlyList<ToolDefinition> Tools { get; init; }
  // Per-request context for an MCP tools/call or resources/read in flight. AsyncLocal so the bridge can read it from inside parameter binding without threading another argument through every call site. Carries: The request's effective CancellationToken (linked to the transport CT and a per-request CTS the host can trip on notifications/cancelled).An optional progress sink the bridge wires IProgress`1 parameters into. SessionIdentityFields — the authenticated identity for this request (from claims merged by the transport). Bridges pass it to CellHost.ResolveByCellTypeName so keyed cells route to the right instance. Empty / null on the stdio path (single-user process).
  sealed class McpCallContext : IEquatable<McpCallContext>
    // Per-request context for an MCP tools/call or resources/read in flight. AsyncLocal so the bridge can read it from inside parameter binding without threading another argument through every call site. Carries: The request's effective CancellationToken (linked to the transport CT and a per-request CTS the host can trip on notifications/cancelled).An optional progress sink the bridge wires IProgress`1 parameters into. SessionIdentityFields — the authenticated identity for this request (from claims merged by the transport). Bridges pass it to CellHost.ResolveByCellTypeName so keyed cells route to the right instance. Empty / null on the stdio path (single-user process).
    ctor(CancellationToken CancellationToken, Func<ProgressUpdate, Task>? OnProgress, IReadOnlyDictionary<string, string>? SessionIdentityFields = null)
    CancellationToken CancellationToken { get; init; }
    static McpCallContext? Current { get; }
    Func<ProgressUpdate, Task>? OnProgress { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentityFields { get; init; }
    // Convenience accessor for the conventional userid field of the request's SessionIdentity. Returns null when no McpCallContext is current or when claims carried no userid. Mirror of UserId — same semantics across both request-scoped contexts.
    string? UserId { get; }
    static IDisposable Use(McpCallContext context)
  sealed class McpCapabilities : IEquatable<McpCapabilities>
    ctor(McpToolsCapability? Tools = null, McpResourcesCapability? Resources = null)
    McpResourcesCapability? Resources { get; init; }
    McpToolsCapability? Tools { get; init; }
  // Standard JSON-RPC error codes plus MCP additions. The MCP spec uses InvalidRequest for malformed envelopes and MethodNotFound for unknown methods.
  static class McpErrorCode
    static int GovernanceDenied
    static int GovernanceEscalated
    static int InternalError
    static int InvalidParams
    static int InvalidRequest
    static int MethodNotFound
    static int ParseError
  // MCP server core — owns a tool registry and routes JSON-RPC requests (initialize, tools/list, tools/call) to their handlers. Tool invocations are routed through Current so the same hook that governs in-process Ikon agents governs MCP-exposed tools — one mission, two transports, one audit chain.
  sealed class McpHost
    ctor(string serverName = "ikon-mcp", string serverVersion = "0.1.0", string protocolVersion = "2024-11-05")
    IReadOnlyCollection<McpResourceHandler> Resources { get; }
    McpServerInfo ServerInfo { get; }
    IReadOnlyCollection<McpToolHandler> Tools { get; }
    // Invoke a single registered tool by name with the given arguments object — the shared core behind both the JSON-RPC tools/call path and the per-tool HTTP endpoint ( HandleToolPostAsync ). Sets up the McpCallContext (identity + cancellation + optional progress) and runs the invoke through governance, so both transports gate and bind identically. Returns an error CallToolResult for an unknown tool; governance denials/escalations propagate as exceptions for the caller to map.
    Task<CallToolResult> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = null, IReadOnlyDictionary<string, string>? sessionIdentityFields = null, Func<ProgressUpdate, Task>? onProgress = null)
    Task<JsonRpcResponse?> HandleRequestAsync(JsonRpcRequest request, CancellationToken ct = null, IReadOnlyDictionary<string, string>? sessionIdentityFields = null, IMcpNotificationSink? perRequestSink = null)
    McpHost RegisterResource(McpResourceHandler resource)
    McpHost RegisterTool(McpToolHandler handler)
    // Wire a transport's outbound notification sink. The host calls it to push notifications/progress events from in-flight tools. Optional — without a sink, progress emitted by handlers is silently dropped.
    void SetNotificationSink(IMcpNotificationSink sink)
  // MCP Streamable-HTTP entry point. The host (an AppEndpointHost map call or any ASP.NET WebApplication) wires HandlePostAsync at the MCP route — typically /mcp. The transport parses the JSON-RPC body, dispatches through the supplied McpHost with the caller-supplied sessionIdentityFields (so keyed cells resolve to the right per-identity instance), and writes the response back as application/json.
  static class McpHttpTransport
    static Task HandlePostAsync(HttpContext context, McpHost mcp, IReadOnlyDictionary<string, string>? sessionIdentityFields = null)
    // OAuth 2.1 Protected Resource Metadata discovery (RFC 9728). MCP clients GET /.well-known/oauth-protected-resource to discover which authorization server they should obtain tokens from before retrying a 401-rejected MCP request.
    static Task HandleProtectedResourceDiscoveryAsync(HttpContext context)
    // Invoke a single MCP tool over plain HTTP — the per-tool endpoint at /{Owner}/{Method} that sits alongside the /{Owner}/mcp multiplexer. The request body IS the tool's arguments object, bound exactly as tools/call binds it (record / named mode), so a multi-arg tool like Add(int a, int b) is callable as a direct POST {"a":1,"b":2}. Returns the tool's raw result (not the MCP content envelope): JSON when the tool returns an object/number, plain text when it returns a string. Goes through CallToolAsync so identity routing and governance are identical to the multiplexer.
    static Task HandleToolPostAsync(HttpContext context, McpHost mcp, string toolName, IReadOnlyDictionary<string, string>? sessionIdentityFields = null)
  static class McpJson
    static T Deserialize<T>(string json)
    static T DeserializeParams<T>(JsonElement? element)
    static string Serialize<T>(T value)
    static JsonSerializerOptions Options
  // Converts an McpResourceInfo (a discovered McpResourceAttribute -annotated cell method) into an McpResourceHandler that Ikon.Mcp.McpHost can register. On read, the handler matches the incoming URI against the template, binds placeholders to method parameters by name, resolves the owning cell, invokes the method, and packages the return value as ResourceContents — text for strings/JSON-serialisable types, base64 blob for byte[].
  static class McpResourceBridge
    static McpResourceHandler BuildHandler(CellHost cellHost, McpResourceInfo info)
  // Reflective discovery of McpResourceAttribute -decorated methods on cell types. Mirror of McpToolDiscovery .
  static class McpResourceDiscovery
    static IReadOnlyList<McpResourceInfo> ForType(Type ownerType)
    static IReadOnlyList<McpResourceInfo> ForTypes(IEnumerable<Type> types)
  // MCP resource handler — the bridge builds one per [McpResource] cell method. The host iterates handlers to answer resources/list + resources/templates/list and, on resources/read, picks the first handler whose TryMatch binds the incoming URI.
  sealed class McpResourceHandler : IEquatable<McpResourceHandler>
    // MCP resource handler — the bridge builds one per [McpResource] cell method. The host iterates handlers to answer resources/list + resources/templates/list and, on resources/read, picks the first handler whose TryMatch binds the incoming URI.
    ctor(string DisplayName, string Description, string MimeType, string UriTemplate, bool IsStatic, Func<string, IReadOnlyDictionary<string, string>?> TryMatch, Func<string, IReadOnlyDictionary<string, string>, CancellationToken, Task<ResourceContents>> Read)
    string Description { get; init; }
    string DisplayName { get; init; }
    bool IsStatic { get; init; }
    string MimeType { get; init; }
    Func<string, IReadOnlyDictionary<string, string>, CancellationToken, Task<ResourceContents>> Read { get; init; }
    // Stable governance subject id (e.g. "CatalogCell.GetItem"). Used as GovernanceCall.Subject on resources/read; the bridge always sets it explicitly.
    string SubjectId { get; init; }
    Func<string, IReadOnlyDictionary<string, string>?> TryMatch { get; init; }
    string UriTemplate { get; init; }
  // Discovered metadata for a single McpResourceAttribute -annotated cell method. Carries the parsed URI template + reflected MethodInfo so the bridge can match incoming reads and invoke without re-parsing per request.
  sealed class McpResourceInfo : IEquatable<McpResourceInfo>
    // Discovered metadata for a single McpResourceAttribute -annotated cell method. Carries the parsed URI template + reflected MethodInfo so the bridge can match incoming reads and invoke without re-parsing per request.
    ctor(string DisplayName, string Description, string MimeType, UriTemplate UriTemplate, MethodInfo Handler, Type OwnerCellType)
    string Description { get; init; }
    string DisplayName { get; init; }
    MethodInfo Handler { get; init; }
    // True when the URI template has no placeholders — the resource has a single concrete URI and is published in resources/list rather than resources/templates/list.
    bool IsStatic { get; }
    string MimeType { get; init; }
    Type OwnerCellType { get; init; }
    // Structural id used for governance subject + audit. Stable regardless of the MCP-wire display name.
    string SubjectId { get; }
    UriTemplate UriTemplate { get; init; }
  sealed class McpResourcesCapability : IEquatable<McpResourcesCapability>
    ctor()
  sealed class McpServerInfo : IEquatable<McpServerInfo>
    ctor(string Name, string Version)
    string Name { get; init; }
    string Version { get; init; }
  // Converts an McpToolInfo (a discovered McpAttribute -annotated cell method) into an McpToolHandler that Ikon.Mcp.McpHost can register. The handler resolves the cell instance via CellHost , deserialises method parameters from the incoming JSON-RPC arguments object, invokes the method, awaits a possible Task`1 / ValueTask`1 , and normalises the return value to a string MCP can ship as a "text" tool content. Two binding modes, picked by signature shape: Record mode — the method has exactly one parameter whose type serialises as a JSON object (a record, POCO, dictionary, or JsonElement ). The MCP inputSchema is the record's schema, derived top-level via JsonSchemaExporter . The whole arguments object is deserialised into that single parameter — no wrapper property name.Named mode — anything else (multiple parameters, or a single primitive parameter). Each parameter becomes a top-level property of the schema; at call time the bridge binds by parameter name. Authors don't write JSON schema strings — the C# signature is the schema.
  static class McpToolBridge
    static McpToolHandler BuildHandler(CellHost cellHost, McpToolInfo info)
  // Reflective discovery of McpAttribute -decorated methods on a cell type. Used at startup by the framework to enumerate the MCP-exposed surface of every registered cell type. Mirrors HttpEndpointDiscovery .
  static class McpToolDiscovery
    // Discover every McpAttribute -decorated public instance method on ownerType . Methods inherited from base classes are included; static methods and non-public methods are skipped (tools must be invokable on a specific cell instance).
    static IReadOnlyList<McpToolInfo> ForType(Type ownerType)
    // Discover tools across every type in types . Convenience overload for the startup path that has already filtered an assembly's loaded types down to cells.
    static IReadOnlyList<McpToolInfo> ForTypes(IEnumerable<Type> types)
  sealed class McpToolHandler : IEquatable<McpToolHandler>
    ctor(string Name, string Description, JsonElement InputSchema, Func<JsonElement, CancellationToken, Task<string>> Invoke)
    string Description { get; init; }
    JsonElement InputSchema { get; init; }
    Func<JsonElement, CancellationToken, Task<string>> Invoke { get; init; }
    string Name { get; init; }
    // Optional JSON schema for the tool's return value. Auto-derived from the method's return type by Ikon.App.McpToolBridge. Surfaced to MCP clients via OutputSchema .
    JsonElement? OutputSchema { get; init; }
    // Stable governance subject id, decoupled from the MCP-wire Name . When non-empty, the host uses this as GovernanceCall.Subject so missions can address the tool by a structural id (e.g. "RefundsCell.Refund") regardless of any client-facing name override. Defaults to Name .
    string SubjectId { get; init; }
  // Metadata for a single McpAttribute -annotated method discovered at startup. Carries everything the bridge needs at request time: the MCP-wire name, description, the reflected MethodInfo , and the owner cell Type .
  sealed class McpToolInfo : IEquatable<McpToolInfo>
    // Metadata for a single McpAttribute -annotated method discovered at startup. Carries everything the bridge needs at request time: the MCP-wire name, description, the reflected MethodInfo , and the owner cell Type .
    ctor(string Name, string Description, MethodInfo Handler, Type OwnerCellType)
    string Description { get; init; }
    MethodInfo Handler { get; init; }
    string Name { get; init; }
    Type OwnerCellType { get; init; }
    // Optional override for the tool's standalone HTTP endpoint path (from Path ). Empty → the path is derived from the method name. Does not affect the MCP wire Name .
    string Path { get; init; }
    // Structural identifier used for governance and audit. Stable regardless of the Name override — missions and policies always reference tools by this id.
    string SubjectId { get; }
  sealed class McpToolsCapability : IEquatable<McpToolsCapability>
    ctor()
  // Params of a notifications/progress notification. ProgressToken echoes the request id (or a client-supplied token) so clients can match progress events back to the call they kicked off.
  sealed class ProgressNotificationParams : IEquatable<ProgressNotificationParams>
    // Params of a notifications/progress notification. ProgressToken echoes the request id (or a client-supplied token) so clients can match progress events back to the call they kicked off.
    ctor(JsonElement ProgressToken, double Progress, double? Total = null, string? Message = null)
    string? Message { get; init; }
    double Progress { get; init; }
    JsonElement ProgressToken { get; init; }
    double? Total { get; init; }
  // One progress update emitted by a long-running tool. Progress is a monotonic counter; Total is optional but expected to stay constant across updates so clients can render a percentage. Message is freeform display text.
  sealed class ProgressUpdate : IEquatable<ProgressUpdate>
    // One progress update emitted by a long-running tool. Progress is a monotonic counter; Total is optional but expected to stay constant across updates so clients can render a percentage. Message is freeform display text.
    ctor(double Progress, double? Total = null, string? Message = null)
    string? Message { get; init; }
    double Progress { get; init; }
    double? Total { get; init; }
  sealed class ReadResourceParams : IEquatable<ReadResourceParams>
    ctor(string Uri)
    string Uri { get; init; }
  sealed class ReadResourceResult : IEquatable<ReadResourceResult>
    ctor(IReadOnlyList<ResourceContents> Contents)
    IReadOnlyList<ResourceContents> Contents { get; init; }
  sealed class Resource : IEquatable<Resource>
    ctor(string Uri, string Name, string? Description = null, string? MimeType = null)
    string? Description { get; init; }
    string? MimeType { get; init; }
    string Name { get; init; }
    string Uri { get; init; }
  sealed class ResourceContents : IEquatable<ResourceContents>
    ctor(string Uri, string? MimeType = null, string? Text = null, string? Blob = null)
    string? Blob { get; init; }
    string? MimeType { get; init; }
    string? Text { get; init; }
    string Uri { get; init; }
  sealed class ResourceTemplate : IEquatable<ResourceTemplate>
    ctor(string UriTemplate, string Name, string? Description = null, string? MimeType = null)
    string? Description { get; init; }
    string? MimeType { get; init; }
    string Name { get; init; }
    string UriTemplate { get; init; }
  // Newline-delimited JSON-RPC over stdin / stdout — the transport Claude Desktop and other MCP clients use to talk to local servers. One line per message; malformed input yields a JSON-RPC parse-error response (rather than killing the loop) so a flaky client can't poison the server. Also acts as the outbound IMcpNotificationSink for the host: in-flight tools that emit progress write notifications/progress lines back through the same stdout pipe. Writes are serialised on a per-transport lock so request-response and server-push don't interleave.
  sealed class StdioTransport : IMcpNotificationSink
    ctor(McpHost host, TextReader? input = null, TextWriter? output = null)
    Task RunAsync(CancellationToken ct = null)
    Task SendNotificationAsync(string method, object params, CancellationToken ct)
  sealed class ToolContent : IEquatable<ToolContent>
    ctor(string Type, string Text)
    string Text { get; init; }
    string Type { get; init; }
  sealed class ToolDefinition : IEquatable<ToolDefinition>
    ctor(string Name, string Description, JsonElement InputSchema)
    string Description { get; init; }
    JsonElement InputSchema { get; init; }
    string Name { get; init; }
    // Optional JSON schema for the tool's return value. Derived from the method's return type (after Task/ValueTask unwrap) by Ikon.App.McpToolBridge; authors never specify it directly. Helps MCP clients validate / type-check what they get back.
    JsonElement? OutputSchema { get; init; }

namespace Ikon.App.Payments
  sealed class AssetStripeMerchantStore : IStripeMerchantStore
    ctor(string assetPath = "payments/merchant-account.json")
    Task ClearAsync(CancellationToken cancellationToken = null)
    Task<string?> GetAsync(CancellationToken cancellationToken = null)
    Task SetAsync(string connectedAccountId, CancellationToken cancellationToken = null)
  // Bridge between the library and an app's domain model. The library calls back into the adapter to look up plans, resolve customers, and to deliver verified webhook events. Apps own all persistence — the library never touches an app database directly.
  interface IPaymentsAppAdapter
    // Apply a verified billing event to the app's domain. The library calls this from HandleWebhookAsync after signature verification. Apps must implement idempotency using EventId .
    abstract Task ApplyEventAsync(PaymentsEvent evt, CancellationToken cancellationToken)
    // Resolve a plan by its app-side id. Return null if the plan is unknown or archived.
    abstract Task<PaymentsPlanDescriptor?> GetPlanAsync(string planId, CancellationToken cancellationToken)
    // Return a Stripe customer id for the given app-side customer key, creating one if it does not yet exist. Apps should persist the mapping so subsequent calls return the same Stripe customer id.
    abstract Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken cancellationToken)
  // App-owned credit ledger contract. The library never persists credit balances itself — credits are an app concern (wallet table in app DB, KV store, etc.). Apps implement this interface and pass it to GetEntitlementAsync and to [Payments.ChargeCredits] policy attributes. All methods are scoped by (appCustomerKey, sku). The library supplies a stable idempotencyKey so apps can dedupe concurrent deductions on the same charge event (e.g. a webhook replaying the same checkout.session.completed).
  interface IPaymentsCreditStore
    // Atomically deduct credits from the customer's balance. Returns the new balance. Throws or returns negative balance when insufficient — implementations choose; the policy-attribute layer treats < 0 as denial. idempotencyKey dedupes replays.
    abstract Task<int> DeductAsync(string appCustomerKey, string sku, int credits, string idempotencyKey, CancellationToken cancellationToken = null)
    // Current balance for the given customer + SKU. Returns 0 when no row exists.
    abstract Task<int> GetCreditsAsync(string appCustomerKey, string sku, CancellationToken cancellationToken = null)
    // Atomically grant credits to the customer's balance. Returns the new balance. Called from the adapter's ApplyEventAsync when a top-up checkout completes. idempotencyKey dedupes replays (typically the Stripe EventId ).
    abstract Task<int> GrantAsync(string appCustomerKey, string sku, int credits, string idempotencyKey, CancellationToken cancellationToken = null)
  // Operation-level abstraction over a payment provider. The neutral Payments* DTOs are the contract; each provider maps them to/from its own wire format and auth model. Stripe is fully implemented (StripePaymentsProvider); Worldpay and Vipps are stubs that only declare GetCapabilities . This mirrors the Ikon.AI provider pattern (a neutral interface + per-provider implementations selected by a factory + capability flags). The seam is at the operation level — not the HTTP transport level — because provider APIs differ fundamentally (Stripe form-encoded /v1/ vs Worldpay JSON+HATEOAS vs Vipps JSON+OAuth+MSN wallet redirect), so a shared "post a Stripe form to a path" transport cannot express them all.Most operations carry a default body that throws PaymentsNotSupportedException , so a provider implements only what it supports; GetCapabilities tells apps which.
  interface IPaymentsProvider
    // Optional app-supplied credit ledger used by GetEntitlementAsync and credit-charge policies.
    IPaymentsCreditStore? CreditStore { get; set; }
    // Provider identifier (stripe / worldpay / vipps).
    string Name { get; }
    virtual Task AddInvoiceItemAsync(string stripeCustomerId, long amountMinor, string currency, string description, string? subscriptionId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task AdjustCustomerBalanceAsync(string stripeCustomerId, long amountMinorDelta, string currency, string description, string idempotencyKey, CancellationToken cancellationToken = null)
    virtual Task<PaymentsPaymentIntent> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = null)
    virtual Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, CancellationToken cancellationToken = null)
    virtual Task CancelSubscriptionScheduleAsync(string scheduleId, CancellationToken cancellationToken = null)
    virtual Task<PaymentsPaymentIntent> CapturePaymentIntentAsync(string paymentIntentId, long? amountToCaptureMinor = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsCheckoutResult> CreateCartCheckoutAsync(IEnumerable<PaymentsLineItem> lines, PaymentsMode mode, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, PaymentsDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsCheckoutResult> CreateCheckoutAsync(string planId, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, PaymentsDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<string> CreateCouponAsync(PaymentsCouponInfo coupon, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsCreditNote> CreateCreditNoteAsync(PaymentsCreditNoteInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<string> CreateCustomerAsync(PaymentsCustomerInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsTaxId> CreateCustomerTaxIdAsync(string stripeCustomerId, string type, string value, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsInvoice> CreateHostedInvoiceAsync(string stripeCustomerId, IEnumerable<PaymentsLineItem> lines, int daysUntilDue, bool autoSend = true, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsPaymentIntent> CreatePaymentIntentAsync(long amountMinor, string currency, string? stripeCustomerId = null, string captureMethod = "automatic", string? paymentMethodId = null, bool confirm = false, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsPaymentLink> CreatePaymentLinkAsync(IEnumerable<PaymentsLineItem> lines, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsPortalResult> CreatePortalAsync(string stripeCustomerId, string? returnUrl = null, string? configurationId = null, string? onBehalfOf = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<string> CreatePortalConfigurationAsync(PaymentsPortalConfigurationInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<string> CreatePriceAsync(PaymentsPriceInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<string> CreateProductAsync(PaymentsProductInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<string> CreatePromotionCodeAsync(string couponId, string code, DateTimeOffset? expiresAt = null, long? maxRedemptions = null, string? restrictedToCustomerId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<string> CreateSubscriptionScheduleAsync(string stripeCustomerId, IEnumerable<PaymentsSubscriptionPhase> phases, DateTimeOffset? startDate = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsCheckoutResult> CreateTipCheckoutAsync(long amountMinor, string currency, string? title = null, string? message = null, string? appCustomerKey = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsWebhookEndpoint> CreateWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, PaymentsWebhookPayloadShape payloadShape = Snapshot, CancellationToken cancellationToken = null)
    virtual Task DeleteCustomerTaxIdAsync(string stripeCustomerId, string taxIdId, CancellationToken cancellationToken = null)
    virtual Task DeleteWebhookEndpointAsync(string webhookEndpointId, CancellationToken cancellationToken = null)
    virtual Task DetachPaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = null)
    // Static capability flags. Query before driving an optional operation.
    abstract ProviderCapabilities GetCapabilities()
    virtual Task<PaymentsEntitlement> GetEntitlementAsync(string planId, string appCustomerKey, IPaymentsCreditStore? creditStore = null, CancellationToken cancellationToken = null)
    virtual Task<PaymentsWebhookResult> HandleWebhookAsync(string? signatureHeader, string body, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> ListApplePayDomainsAsync(int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<PaymentsCharge>> ListChargesAsync(string? stripeCustomerId = null, int limit = 100, DateTimeOffset? createdAfter = null, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> ListCouponsAsync(int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> ListCreditNotesAsync(string? invoiceId = null, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> ListCustomerTaxIdsAsync(string stripeCustomerId, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> ListEventIdsAsync(string? type = null, DateTimeOffset? createdAfter = null, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<PaymentsInvoiceSummary>> ListInvoicesAsync(string? stripeCustomerId = null, string? subscriptionId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> ListPaymentLinksAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<PaymentsPaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card", int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<PaymentsPrice>> ListPricesAsync(string? productId = null, bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<PaymentsPage<PaymentsPrice>> ListPricesPageAsync(string? productId = null, bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<PaymentsProduct>> ListProductsAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<PaymentsPage<PaymentsProduct>> ListProductsPageAsync(bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> ListPromotionCodesAsync(int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<PaymentsSubscription>> ListSubscriptionsAsync(string? stripeCustomerId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> ListWebhookEndpointsAsync(int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<PaymentsCheckoutOffer> OfferCheckoutAsync(string planId, string appCustomerKey, string? email = null, PaymentsDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task PauseSubscriptionAsync(string subscriptionId, string behavior = "void", CancellationToken cancellationToken = null)
    virtual Task PingWebhookEndpointAsync(string webhookEndpointId, CancellationToken cancellationToken = null)
    virtual Task<PaymentsUpcomingInvoice> PreviewUpcomingInvoiceAsync(string stripeCustomerId, string? subscriptionId = null, string? newPriceId = null, long? newQuantity = null, string? couponId = null, CancellationToken cancellationToken = null)
    virtual Task RefundAsync(string paymentIntentId, long? amountMinor, string? reason, string idempotencyKey, bool refundApplicationFee = false, bool reverseTransfer = false, CancellationToken cancellationToken = null)
    virtual Task<string> RegisterApplePayDomainAsync(string domainName, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    virtual Task ReportUsageAsync(string meterEventName, string stripeCustomerId, long value, string idempotencyKey, DateTimeOffset? timestamp = null, CancellationToken cancellationToken = null)
    virtual Task ResumeCanceledSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    virtual Task ResumeSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    virtual Task<PaymentsEvent> RetrieveEventAsync(string eventId, CancellationToken cancellationToken = null)
    virtual Task<PaymentsPrice?> RetrievePriceByLookupKeyAsync(string lookupKey, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> SearchCustomersAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> SearchCustomersByAppKeyAsync(string appCustomerKey, string metadataKey = "app_customer_key", int limit = 1, CancellationToken cancellationToken = null)
    virtual Task<IReadOnlyList<string>> SearchCustomersDetailedAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    virtual Task UpdateCustomerAsync(string stripeCustomerId, PaymentsCustomerInfo info, CancellationToken cancellationToken = null)
    virtual Task UpdatePriceAsync(string priceId, bool? active = null, string? nickname = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    virtual Task UpdateProductAsync(string productId, bool? active = null, string? name = null, string? description = null, IReadOnlyList<string>? marketingFeatures = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    virtual Task UpdateSubscriptionItemQuantityAsync(string subscriptionItemId, long quantity, bool prorate = true, CancellationToken cancellationToken = null)
    virtual Task UpdateSubscriptionPriceAsync(string subscriptionItemId, string newPriceId, bool prorate = true, CancellationToken cancellationToken = null)
    virtual Task UpdateSubscriptionScheduleAsync(string scheduleId, IEnumerable<PaymentsSubscriptionPhase> phases, CancellationToken cancellationToken = null)
    virtual Task VoidCreditNoteAsync(string creditNoteId, CancellationToken cancellationToken = null)
  interface IStripeMerchantStore
    abstract Task ClearAsync(CancellationToken cancellationToken = null)
    abstract Task<string?> GetAsync(CancellationToken cancellationToken = null)
    abstract Task SetAsync(string connectedAccountId, CancellationToken cancellationToken = null)
  static class PaymentsAppHelpers
    static PaymentsOptions AutoDetectFromApp(IAppBase app, string defaultSpaceId = "")
    static string? GetSecretOrEnv(IAppBase app, string key)
  // Pulls the live product + price catalog from Stripe and projects it into a per-app catalog ( PaymentsPlanCatalog ) that pricing-table UIs can render and adapters can resolve plan ids against. Push vs pull. PaymentsCatalogSync goes the other direction — app declares plans in code and the library makes Stripe match. Use Sync when pricing lives in code (deploy-time provisioning); use PaymentsCatalogProjector when Stripe (or an admin UI on top of Stripe) is the source of truth and the app needs to mirror whatever's there.Apps that need both — e.g. seed defaults from code and let operators add more via Stripe Dashboard — call Sync once at startup, then ProjectAsync at runtime / on webhook events.
  sealed class PaymentsCatalogProjector
    ctor(PaymentsService payments)
    // List active Stripe products + their recurring prices, filter to the app's slice, and project each (product, default-price) pair to a PaymentsPlanProjection .
    Task<PaymentsPlanCatalog> ProjectAsync(Func<PaymentsProduct, bool>? productFilter = null, Func<PaymentsPrice, bool>? priceFilter = null, CancellationToken cancellationToken = null)
  // Idempotent provisioning of a Stripe product+price catalog from an app-defined plan list. Apps declare plans in code (or config); this service makes sure each plan has a matching Stripe product + price, reusing existing rows by name and exact (amount, currency, interval) match. Returns a PaymentsPlanCatalogMap mapping app-side plan ids to Stripe price ids that adapters use in GetPlanAsync . Run once at app startup (it's network-bound but idempotent and short), or persist the map after first sync to skip the API hop on warm boots. Stripe is the source of truth for the price ids — they differ per account, so the map must be re-resolved per environment.
  sealed class PaymentsCatalogSync
    ctor(PaymentsService payments)
    Task<PaymentsPlanCatalogMap> SyncAsync(IReadOnlyList<PaymentsPlanSpec> plans, CancellationToken cancellationToken = null)
    // Ensure each plans entry has a matching Stripe product + price. Returns a map from app plan id to Stripe price id. Matching strategy: 1. Find an existing product whose Name matches ProductName . 2. If absent, create one (with Description and metadata.app_plan_id set). 3. Find an existing price under that product whose UnitAmountMinor, Currency, and RecurringInterval all match. 4. If absent, create one (Stripe prices are immutable, so changing a plan's price creates a new price; existing subscribers stay on the old one).
    Task<PaymentsPlanCatalogMap> SyncFromCatalogClassAsync(Type catalogClass, CancellationToken cancellationToken = null)
  // Slim view of a Stripe charge record. Returned by ListChargesAsync .
  sealed class PaymentsCharge : IEquatable<PaymentsCharge>
    // Slim view of a Stripe charge record. Returned by ListChargesAsync .
    ctor(string Id, string? PaymentIntentId, string? CustomerId, long AmountMinor, long AmountRefundedMinor, string Currency, string Status, bool Paid, bool Refunded, DateTimeOffset Created, string? Description, string? ReceiptUrl)
    // Charged amount in minor units.
    long AmountMinor { get; init; }
    // Refunded amount in minor units.
    long AmountRefundedMinor { get; init; }
    // When Stripe created the charge.
    DateTimeOffset Created { get; init; }
    // ISO 4217 currency code in lowercase.
    string Currency { get; init; }
    // Customer id, when present.
    string? CustomerId { get; init; }
    // Free-form description on the charge.
    string? Description { get; init; }
    // Stripe charge id (ch_...).
    string Id { get; init; }
    // True when the charge has been collected.
    bool Paid { get; init; }
    // Payment intent id, when present.
    string? PaymentIntentId { get; init; }
    // URL to the hosted receipt, when available.
    string? ReceiptUrl { get; init; }
    // True when the charge is fully refunded.
    bool Refunded { get; init; }
    // succeeded, pending, or failed.
    string Status { get; init; }
  // Declares the function deducts credits from the current customer's wallet for sku . Requires CreditStore wired on the ambient instance. Deduction happens inside the policy via DeductAsync with an idempotency key composed of the function name + caller id, so the same call evaluated twice (e.g. interrupted then retried) charges only once. Deny code: payments_credits_insufficient.
  sealed class PaymentsChargeCreditsAttribute : PolicyAttribute
    ctor(string sku, int credits = 1)
    int Credits { get; }
    string Sku { get; }
    override IFunctionPolicy CreatePolicy()
  // Result of OfferCheckoutAsync . Either the customer already holds the entitlement (no checkout needed — show the app's post-purchase UX directly) or a fresh Stripe Checkout session was minted and the app should redirect.
  sealed class PaymentsCheckoutOffer : IEquatable<PaymentsCheckoutOffer>
    // Result of OfferCheckoutAsync . Either the customer already holds the entitlement (no checkout needed — show the app's post-purchase UX directly) or a fresh Stripe Checkout session was minted and the app should redirect.
    ctor(bool AlreadyEntitled, string? SessionId, string? Url)
    // True when the customer already had an active subscription / unlock for the plan and no Stripe call was made.
    bool AlreadyEntitled { get; init; }
    // Stripe Checkout session id (only when AlreadyEntitled is false).
    string? SessionId { get; init; }
    // Stripe hosted-checkout URL (only when AlreadyEntitled is false). App passes to ClientFunctions.SetUrlAsync.
    string? Url { get; init; }
  // Result of creating a Stripe Checkout session. Apps redirect the user to Url .
  sealed class PaymentsCheckoutResult : IEquatable<PaymentsCheckoutResult>
    // Result of creating a Stripe Checkout session. Apps redirect the user to Url .
    ctor(string SessionId, string Url)
    string SessionId { get; init; }
    string Url { get; init; }
  enum PaymentsCouponDuration
    Once
    Forever
    Repeating
  // Coupon definition for CreateCouponAsync . Set exactly one of PercentOff or AmountOffMinor .
  sealed class PaymentsCouponInfo : IEquatable<PaymentsCouponInfo>
    ctor()
    long? AmountOffMinor { get; init; }
    string? Currency { get; init; }
    PaymentsCouponDuration Duration { get; init; }
    int? DurationInMonths { get; init; }
    string? Id { get; init; }
    int? MaxRedemptions { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? Name { get; init; }
    decimal? PercentOff { get; init; }
    DateTimeOffset? RedeemBy { get; init; }
  // Result of issuing a credit note.
  sealed class PaymentsCreditNote : IEquatable<PaymentsCreditNote>
    // Result of issuing a credit note.
    ctor(string Id, string Number, string Status, long AmountMinor, string? PdfUrl)
    // Total of the credit note.
    long AmountMinor { get; init; }
    // Credit note id (cn_...).
    string Id { get; init; }
    // Human-readable credit note number.
    string Number { get; init; }
    // URL of the generated PDF, when present.
    string? PdfUrl { get; init; }
    // issued or void.
    string Status { get; init; }
  // Inputs for CreateCreditNoteAsync . A credit note is the formal way to issue a partial refund or credit against a finalized Stripe invoice — Stripe handles the tax adjustment and regenerates the invoice PDF, which a plain Refund does not.
  sealed class PaymentsCreditNoteInfo : IEquatable<PaymentsCreditNoteInfo>
    // Amount of the credit note in minor units. Defaults to a full credit.
    long? AmountMinor { get; init; }
    // Amount to credit to the customer's balance, in minor units.
    long? CreditAmountMinor { get; init; }
    string InvoiceId { get; init; }
    string? Memo { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? Reason { get; init; }
    // Amount to refund to the original payment method, in minor units. Null = no out-of-pocket refund (credit only).
    long? RefundAmountMinor { get; init; }
  // Subset of Stripe customer fields the library reads or writes. Apps build one of these to call CreateCustomerAsync or UpdateCustomerAsync .
  sealed class PaymentsCustomerInfo : IEquatable<PaymentsCustomerInfo>
    ctor()
    string? AddressCity { get; init; }
    string? AddressCountry { get; init; }
    string? AddressLine1 { get; init; }
    string? AddressLine2 { get; init; }
    string? AddressPostalCode { get; init; }
    string? AddressState { get; init; }
    string? Description { get; init; }
    string? Email { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? Name { get; init; }
    string? Phone { get; init; }
    string? PreferredLocales { get; init; }
    // Stripe Tax exemption status. None = standard (default), Exempt = no tax charged, Reverse = EU B2B reverse-charge (customer self-accounts for VAT). Required for B2B SaaS in EU when the buyer carries a valid VAT id.
    PaymentsTaxExempt? TaxExempt { get; init; }
  // Marketplace / Stripe Connect destination for a charge. Use to route a checkout payment to a connected account while the platform takes an application fee.
  sealed class PaymentsDestination : IEquatable<PaymentsDestination>
    ctor(string ConnectedAccountId, long? ApplicationFeeAmountMinor = null, decimal? ApplicationFeePercent = null)
    long? ApplicationFeeAmountMinor { get; init; }
    decimal? ApplicationFeePercent { get; init; }
    string ConnectedAccountId { get; init; }
  // One-stop "does this customer have access to this plan" snapshot. Composed by GetEntitlementAsync from Stripe subscription state, customer metadata, and an optional app-side credit store. Apps read this single record instead of orchestrating three Stripe roundtrips themselves.
  sealed class PaymentsEntitlement : IEquatable<PaymentsEntitlement>
    // One-stop "does this customer have access to this plan" snapshot. Composed by GetEntitlementAsync from Stripe subscription state, customer metadata, and an optional app-side credit store. Apps read this single record instead of orchestrating three Stripe roundtrips themselves.
    ctor(string PlanId, bool SubscriptionActive, DateTimeOffset? SubscriptionEndsAt, bool CancelAtPeriodEnd, string? SubscriptionStatus, bool UnlockGranted, DateTimeOffset? UnlockGrantedAt, int CreditsRemaining, DateTimeOffset? LastPurchaseAt)
    // True when the subscription is scheduled to cancel at SubscriptionEndsAt .
    bool CancelAtPeriodEnd { get; init; }
    // Wallet balance for credit-based products. Populated only when an IPaymentsCreditStore is supplied; otherwise 0.
    int CreditsRemaining { get; init; }
    // Customer-metadata-stamped last-purchase timestamp; nullable.
    DateTimeOffset? LastPurchaseAt { get; init; }
    // App-side plan identifier this snapshot describes.
    string PlanId { get; init; }
    // True when an active or trialing subscription for this plan exists on the customer.
    bool SubscriptionActive { get; init; }
    // Current period end when the subscription is active. Null when there's no subscription.
    DateTimeOffset? SubscriptionEndsAt { get; init; }
    // Raw Stripe status (active, trialing, past_due, etc.) when a subscription exists; null otherwise.
    string? SubscriptionStatus { get; init; }
    // True when the customer holds a one-time unlock for this plan. Sourced from customer metadata key unlock_{planId}; apps stamp it from their ApplyEventAsync on CheckoutCompleted .
    bool UnlockGranted { get; init; }
    // Timestamp parsed from the metadata stamp. Null when not held.
    DateTimeOffset? UnlockGrantedAt { get; init; }
  // Typed billing event surfaced by HandleWebhookAsync . Apps switch on Type and read the relevant fields. Unknown event types are surfaced as Unknown with the raw payload preserved for the app to inspect.
  sealed class PaymentsEvent : IEquatable<PaymentsEvent>
    // Typed billing event surfaced by HandleWebhookAsync . Apps switch on Type and read the relevant fields. Unknown event types are surfaced as Unknown with the raw payload preserved for the app to inspect.
    ctor(string EventId, PaymentsEventType Type, string? CustomerId, string? SubscriptionId, string? ClientReferenceId, string? PlanId, string? Status, DateTimeOffset? CurrentPeriodStart, DateTimeOffset? CurrentPeriodEnd, long? AmountPaid, string? Currency, JsonElement RawPayload, string RawEventName = "", bool IsLegacyEventName = false, bool IsThinEvent = false, string? RelatedObjectId = null, string? RelatedObjectType = null, string? RelatedObjectUrl = null)
    // Amount paid in minor units (cents), when relevant.
    long? AmountPaid { get; init; }
    // The client_reference_id set when creating checkout, when present. Apps use this to map the event back to their own entity.
    string? ClientReferenceId { get; init; }
    // ISO 4217 currency code in lowercase, when relevant.
    string? Currency { get; init; }
    // UTC period end, when present.
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    // UTC period start, when present on invoice/subscription events.
    DateTimeOffset? CurrentPeriodStart { get; init; }
    // Stripe customer id, when present on the payload.
    string? CustomerId { get; init; }
    // Stripe event id (evt_...). Use for idempotency.
    string EventId { get; init; }
    // True when RawEventName is a v1 alias that will be dropped in the next major (e.g. "account.updated" superseded by "v2.core.account.updated"). Apps can warn / migrate registrations on the strength of this flag.
    bool IsLegacyEventName { get; init; }
    // True when the payload is a v2 thin event (object: "v2.core.event"). Thin events omit the embedded object snapshot; apps must fetch the underlying object via RelatedObjectUrl if they need its current state. False for the legacy v1 snapshot shape with data.object.
    bool IsThinEvent { get; init; }
    // Plan id from session metadata, when present.
    string? PlanId { get; init; }
    // Original Stripe event name as received ("v2.core.account.updated", "checkout.session.completed", …). Useful for debugging and for legacy-event detection.
    string RawEventName { get; init; }
    // Raw Stripe event JSON for app-side escape hatches.
    JsonElement RawPayload { get; init; }
    // Id of the object the thin event refers to (from related_object.id). Populated only when IsThinEvent is true.
    string? RelatedObjectId { get; init; }
    // Type of the related object (e.g. "v2.core.account"). Populated only when IsThinEvent is true.
    string? RelatedObjectType { get; init; }
    // Stripe API path that returns the current state of the related object (e.g. "/v2/core/accounts/acct_…"). Populated only when IsThinEvent is true. Apps that need the full object call HTTP GET on this path.
    string? RelatedObjectUrl { get; init; }
    // Subscription status when relevant (active, past_due, canceled, ...).
    string? Status { get; init; }
    // Stripe subscription id, when present.
    string? SubscriptionId { get; init; }
    // Typed event kind.
    PaymentsEventType Type { get; init; }
  enum PaymentsEventType
    Unknown
    CheckoutCompleted
    CheckoutAsyncPaymentSucceeded
    CheckoutAsyncPaymentFailed
    InvoicePaid
    InvoicePaymentFailed
    InvoiceFinalized
    PaymentActionRequired
    SubscriptionUpdated
    SubscriptionDeleted
    ChargeRefunded
    ChargeDisputed
    ChargeDisputeClosed
    SetupIntentSucceeded
    PaymentMethodAttached
    CreditNoteCreated
    CreditNoteVoided
    SubscriptionTrialWillEnd
    ConnectAccountUpdated
    ConnectAccountRequirementsUpdated
    ConnectAccountCapabilityUpdated
    PayoutCreated
    PayoutUpdated
    PayoutPaid
    PayoutFailed
    ConnectOAuthAuthorized
    ConnectOAuthDeauthorized
    SubscriptionScheduleUpdated
    ProductUpdated
    PriceUpdated
  // Hosted Stripe invoice — for B2B net-30 flows where the customer pays via an emailed link rather than going through Checkout.
  sealed class PaymentsInvoice : IEquatable<PaymentsInvoice>
    // Hosted Stripe invoice — for B2B net-30 flows where the customer pays via an emailed link rather than going through Checkout.
    ctor(string Id, string? HostedInvoiceUrl, string? InvoicePdfUrl, string Status)
    string? HostedInvoiceUrl { get; init; }
    string Id { get; init; }
    string? InvoicePdfUrl { get; init; }
    string Status { get; init; }
  // Slim view of a Stripe invoice. Returned by ListInvoicesAsync .
  sealed class PaymentsInvoiceSummary : IEquatable<PaymentsInvoiceSummary>
    // Slim view of a Stripe invoice. Returned by ListInvoicesAsync .
    ctor(string Id, string? CustomerId, string? SubscriptionId, long AmountDueMinor, long AmountPaidMinor, string Currency, string Status, DateTimeOffset Created, DateTimeOffset? DueDate, string? HostedInvoiceUrl, string? InvoicePdfUrl)
    long AmountDueMinor { get; init; }
    long AmountPaidMinor { get; init; }
    DateTimeOffset Created { get; init; }
    string Currency { get; init; }
    string? CustomerId { get; init; }
    DateTimeOffset? DueDate { get; init; }
    string? HostedInvoiceUrl { get; init; }
    string Id { get; init; }
    string? InvoicePdfUrl { get; init; }
    string Status { get; init; }
    string? SubscriptionId { get; init; }
  // Single line item on a multi-line checkout. Use ForPrice for a preconfigured Stripe price, Dynamic for ad-hoc amounts (tipping, donations, custom-priced cart items).
  sealed class PaymentsLineItem : IEquatable<PaymentsLineItem>
    ctor()
    long? AdHocAmountMinor { get; init; }
    string? AdHocCurrency { get; init; }
    string? AdHocProductName { get; init; }
    bool AdHocRecurring { get; init; }
    string? AdHocRecurringInterval { get; init; }
    string? PriceId { get; init; }
    long Quantity { get; init; }
    static PaymentsLineItem Dynamic(long amountMinor, string currency, string productName, long quantity = 1)
    static PaymentsLineItem ForPrice(string priceId, long quantity = 1)
  enum PaymentsMode
    Subscription
    OneTime
  // Options needed by PaymentsService . Apps load secrets from their own configuration source (Ikon secrets, environment variables, vault) and pass them in here. The library never reads configuration directly.
  sealed class PaymentsOptions : IEquatable<PaymentsOptions>
    ctor()
    // Stripe API key. Accepts both unrestricted secret keys (sk_test_ / sk_live_) and restricted keys (rk_test_ / rk_live_); the library treats them identically. Restricted keys are recommended for least-privilege deployments — see the billing guide for the suggested permission set. Required for Byok ; unused for IkonConnect (Ikon backend holds the platform key).
    string ApiKey { get; init; }
    // Stripe API version to pin (sent as Stripe-Version header). Defaults to 2026-04-22.dahlia — the version this library is tested against, which is required for Accounts v2 (/v2/core/accounts) and Payments v2 event payloads. Set to null to fall back to the connected account's default version (only do this if you must interoperate with code that depends on an older payload shape).
    string? ApiVersion { get; init; }
    // Enable Stripe automatic tax calculation on Checkout sessions. Requires Tax to be configured in the Stripe Dashboard.
    bool AutomaticTax { get; init; }
    // Collect VAT / tax IDs at Checkout. When true, the Checkout session asks for a tax ID.
    bool CollectTaxId { get; init; }
    // Stripe Connect connected-account id (acct_...). When set, every Stripe API call is sent with the Stripe-Account header so charges, customers, prices etc. live on the connected account, not the platform account. Use this for the platform-managed Connect mode where one platform key serves many connected orgs/apps.
    string? ConnectedAccountId { get; init; }
    // Default cancel URL used when a checkout call does not specify one.
    string? DefaultCancelUrl { get; init; }
    // Free-form metadata merged into every Stripe object the library creates (customers, prices, products, checkout sessions, subscriptions, ...). Use to tag every record with the originating Ikon app id so a single connected account shared by multiple apps stays separable in reporting.
    IReadOnlyDictionary<string, string>? DefaultMetadata { get; init; }
    // Default Customer Portal return URL used when a portal call does not specify one.
    string? DefaultPortalReturnUrl { get; init; }
    // Default success URL used when a checkout call does not specify one.
    string? DefaultSuccessUrl { get; init; }
    // Per-call payment-method exclusion list (e.g. ["affirm", "afterpay_clearpay"]). Stripe shows every dynamically-enabled method except the listed ones. Use when an app wants code-managed control over async methods without maintaining a dashboard configuration. Mutually exclusive with PaymentMethodConfigurationId . Apple Pay / Google Pay / Link cannot be excluded per-call — manage those at dashboard level.
    IReadOnlyList<string>? ExcludedPaymentMethodTypes { get; init; }
    string? IkonBackendUrl { get; init; }
    // Maximum number of retry attempts on transient failures (HTTP 429 / 5xx / network faults). 0 disables retries.
    int MaxRetryAttempts { get; init; }
    // Stripe Dashboard-managed Payment Method Configuration id (pmc_…). When set, the library passes payment_method_configuration on every Checkout / PaymentIntent / SetupIntent create call so the app shows exactly the methods enabled in the configuration. Preferred over ExcludedPaymentMethodTypes for stable per-app surfaces. Mutually exclusive with ExcludedPaymentMethodTypes .
    string? PaymentMethodConfigurationId { get; init; }
    // Optional platform application fee in minor units applied to every one-time charge (Checkout in payment mode) when ConnectedAccountId is set. 0 disables.
    long? PlatformApplicationFeeAmountMinor { get; init; }
    // Optional platform application fee percent applied to every recurring charge (subscriptions / Checkout in subscription mode) when ConnectedAccountId is set. 0 disables. Range 0-100.
    decimal? PlatformApplicationFeePercent { get; init; }
    PaymentsProvider Provider { get; init; }
    // HTTP request timeout per Stripe call. Null = HttpClient default.
    TimeSpan? RequestTimeout { get; init; }
    // Base delay between retry attempts. Exponential backoff with jitter is layered on top.
    TimeSpan RetryBaseDelay { get; init; }
    string? Space { get; init; }
    // Stripe webhook signing secret (starts with whsec_). Required for webhook verification.
    string? WebhookSecret { get; init; }
  // One page of Stripe list results plus the cursor ( LastId ) to pass back to the next page call. HasMore reflects Stripe's has_more flag — true means at least one more page.
  sealed class PaymentsPage<T> : IEquatable<PaymentsPage<T>>
    // One page of Stripe list results plus the cursor ( LastId ) to pass back to the next page call. HasMore reflects Stripe's has_more flag — true means at least one more page.
    ctor(IReadOnlyList<T> Items, bool HasMore, string? LastId)
    bool HasMore { get; init; }
    IReadOnlyList<T> Items { get; init; }
    string? LastId { get; init; }
  // Result of creating a Stripe payment intent — used for custom payment flows outside of Checkout (in-app card forms, deferred capture, etc.).
  sealed class PaymentsPaymentIntent : IEquatable<PaymentsPaymentIntent>
    // Result of creating a Stripe payment intent — used for custom payment flows outside of Checkout (in-app card forms, deferred capture, etc.).
    ctor(string Id, string ClientSecret, string Status)
    // Client secret for confirmation via Stripe.js / Elements.
    string ClientSecret { get; init; }
    // Payment intent id (pi_...).
    string Id { get; init; }
    // Current status (requires_payment_method, requires_confirmation, requires_action, processing, succeeded, canceled).
    string Status { get; init; }
  // Result of creating a Stripe Payment Link — a shareable URL that opens a Stripe-hosted checkout for a fixed line item.
  sealed class PaymentsPaymentLink : IEquatable<PaymentsPaymentLink>
    // Result of creating a Stripe Payment Link — a shareable URL that opens a Stripe-hosted checkout for a fixed line item.
    ctor(string Id, string Url)
    string Id { get; init; }
    string Url { get; init; }
  // Slim view of a Stripe payment method. Returned by ListPaymentMethodsAsync .
  sealed class PaymentsPaymentMethod : IEquatable<PaymentsPaymentMethod>
    // Slim view of a Stripe payment method. Returned by ListPaymentMethodsAsync .
    ctor(string Id, string Type, string? CardBrand, string? CardLast4, int? CardExpMonth, int? CardExpYear)
    // Card brand when Type is card (e.g. visa).
    string? CardBrand { get; init; }
    // Card expiry month, when applicable.
    int? CardExpMonth { get; init; }
    // Card expiry year, when applicable.
    int? CardExpYear { get; init; }
    // Last four digits of the card, when applicable.
    string? CardLast4 { get; init; }
    // Stripe payment method id (pm_...).
    string Id { get; init; }
    // Stripe type (card, sepa_debit, etc.).
    string Type { get; init; }
  // Cached catalog projection returned by ProjectAsync . PlanIdToPriceId is the lookup adapters use in GetPlanAsync ; Plans is the list apps surface to end users in pricing tables.
  sealed class PaymentsPlanCatalog : IEquatable<PaymentsPlanCatalog>
    // Cached catalog projection returned by ProjectAsync . PlanIdToPriceId is the lookup adapters use in GetPlanAsync ; Plans is the list apps surface to end users in pricing tables.
    ctor(IReadOnlyList<PaymentsPlanProjection> Plans, IReadOnlyDictionary<string, string> PlanIdToPriceId)
    IReadOnlyDictionary<string, string> PlanIdToPriceId { get; init; }
    IReadOnlyList<PaymentsPlanProjection> Plans { get; init; }
  // App-plan-id → Stripe-price-id map produced by SyncAsync . Cache this in the app (memory or DB) and have your GetPlanAsync look up the price id from it.
  sealed class PaymentsPlanCatalogMap
    // App plan ids in the map.
    IEnumerable<string> AppPlanIds { get; }
    // Number of plans in the map.
    int Count { get; }
    // True when the map has a Stripe price id for this app plan.
    bool Contains(string appPlanId)
    // Look up the Stripe price id for an app plan. Throws when missing.
    string GetPriceId(string appPlanId)
    // Snapshot the map as a plain dictionary (for serialization, persistence).
    IReadOnlyDictionary<string, string> ToDictionary()
    // Try to look up a Stripe price id without throwing.
    bool TryGetPriceId(string appPlanId, out string priceId)
  // Describes a billable plan as the app sees it. Apps map their internal plan model onto this record before handing it to PaymentsService .
  sealed class PaymentsPlanDescriptor : IEquatable<PaymentsPlanDescriptor>
    ctor(string PlanId, string StripePriceId, PaymentsMode Mode, string? MeteredPriceId = null, long Quantity = 1, IReadOnlyDictionary<string, string>? Metadata = null, int? TrialPeriodDays = null, bool AllowPromotionCodes = false)
    bool AllowPromotionCodes { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? MeteredPriceId { get; init; }
    PaymentsMode Mode { get; init; }
    string PlanId { get; init; }
    long Quantity { get; init; }
    string StripePriceId { get; init; }
    int? TrialPeriodDays { get; init; }
    // Named factory for a credit-bundle top-up plan. Customer pays for a fixed bundle of credits; the app's IPaymentsCreditStore is granted the credits when the webhook completes. Same Stripe-side shape as Unlock — one-time charge against a fixed price — but semantically distinct because the entitlement is the granted credit balance, not a metadata stamp.
    static PaymentsPlanDescriptor Credits(string planId, string stripePriceId, int creditsGranted, IReadOnlyDictionary<string, string>? metadata = null)
    // Named factory for a recurring subscription plan. Sugar over the generic constructor that hides the Mode enum value and surfaces the most common subscription knobs explicitly.
    static PaymentsPlanDescriptor Subscription(string planId, string stripePriceId, int trialPeriodDays = 0, bool allowPromotionCodes = false, long quantity = 1, string? meteredPriceId = null, IReadOnlyDictionary<string, string>? metadata = null)
    // Named factory for a one-time unlock plan. The customer pays once and the entitlement is permanent (apps stamp customer metadata unlock_{planId} from ApplyEventAsync when the checkout completes; GetEntitlementAsync reads it back).
    static PaymentsPlanDescriptor Unlock(string planId, string stripePriceId, long quantity = 1, IReadOnlyDictionary<string, string>? metadata = null)
  // Joined snapshot of a Stripe product + its active default price, projected for app-side display. Returned by ProjectAsync ; apps map this to their own view-model (e.g. PaymentsPlanView).
  sealed class PaymentsPlanProjection : IEquatable<PaymentsPlanProjection>
    // Joined snapshot of a Stripe product + its active default price, projected for app-side display. Returned by ProjectAsync ; apps map this to their own view-model (e.g. PaymentsPlanView).
    ctor(string PlanId, string ProductId, string ProductName, string? ProductDescription, string StripePriceId, long UnitAmountMinor, string Currency, string? RecurringInterval, IReadOnlyList<string>? MarketingFeatures, IReadOnlyDictionary<string, string>? ProductMetadata)
    // ISO 4217 lowercase.
    string Currency { get; init; }
    // Feature bullets defined on the product.
    IReadOnlyList<string>? MarketingFeatures { get; init; }
    // Stable identifier used by GetPlanAsync . Defaults to the price LookupKey when set, otherwise the Stripe price id.
    string PlanId { get; init; }
    // Free-text description from Stripe.
    string? ProductDescription { get; init; }
    // Stripe product id (prod_...).
    string ProductId { get; init; }
    // Free-form metadata stamped on the product. Useful for app filters (app_id, tenant_id, ...).
    IReadOnlyDictionary<string, string>? ProductMetadata { get; init; }
    // Stripe product name.
    string ProductName { get; init; }
    // Payments interval (month, year, ...). Null for one-time prices.
    string? RecurringInterval { get; init; }
    // Stripe price id (price_...).
    string StripePriceId { get; init; }
    // Price in minor units (cents).
    long UnitAmountMinor { get; init; }
  // One row in an app's plan catalog. Apps declare these in code (or load from config) and hand the list to SyncAsync .
  sealed class PaymentsPlanSpec : IEquatable<PaymentsPlanSpec>
    // One row in an app's plan catalog. Apps declare these in code (or load from config) and hand the list to SyncAsync .
    ctor(string AppPlanId, string ProductName, long UnitAmountMinor, string Currency, string? Interval, int? IntervalCount = null, string? Description = null, string? Nickname = null, IReadOnlyDictionary<string, string>? Metadata = null, string? LookupKeyOverride = null)
    // App-side plan id (e.g. "pro"). Stable across environments — the platform key resolves to a different Stripe price per account.
    string AppPlanId { get; init; }
    // ISO 4217 currency, lowercase.
    string Currency { get; init; }
    // Optional product description.
    string? Description { get; init; }
    // Recurring interval (day, week, month, year) for subscriptions. Pass null for one-time prices — but typical SaaS catalogs are recurring.
    string? Interval { get; init; }
    // Multiplier on Interval (e.g. 3 with month = quarterly). Defaults to 1.
    int? IntervalCount { get; init; }
    string? LookupKeyOverride { get; init; }
    // Free-form metadata stamped on both the product (when first created) and the price.
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    // Optional price nickname (Stripe Dashboard label).
    string? Nickname { get; init; }
    // Stripe product name. Used as the idempotency lookup key — keep stable.
    string ProductName { get; init; }
    // Price in minor units (e.g. cents).
    long UnitAmountMinor { get; init; }
    // Credit-bundle spec. Metadata is stamped with credits_granted so the webhook handler knows how many credits to grant via GrantAsync .
    static PaymentsPlanSpec Credits(string appPlanId, string productName, long unitAmountMinor, string currency, int creditsGranted, string? description = null)
    // Recurring subscription spec. Sets Interval from interval .
    static PaymentsPlanSpec Subscription(string appPlanId, string productName, long unitAmountMinor, string currency, string interval, int? intervalCount = null, string? description = null)
    // One-time unlock spec. Interval is null.
    static PaymentsPlanSpec Unlock(string appPlanId, string productName, long unitAmountMinor, string currency, string? description = null)
  // Customer Portal feature toggles. When apps create their own portal configuration via CreatePortalConfigurationAsync they pass one of these and reference the returned id when creating portal sessions.
  sealed class PaymentsPortalConfigurationInfo : IEquatable<PaymentsPortalConfigurationInfo>
    ctor()
    bool AllowCustomerUpdate { get; init; }
    bool AllowInvoiceHistory { get; init; }
    bool AllowPaymentMethodUpdate { get; init; }
    bool AllowSubscriptionCancel { get; init; }
    bool AllowSubscriptionPause { get; init; }
    string? BusinessProfileHeadline { get; init; }
    string? PrivacyPolicyUrl { get; init; }
    string? SubscriptionCancelMode { get; init; }
    string? TermsOfServiceUrl { get; init; }
  // Result of creating a Customer Portal session.
  sealed class PaymentsPortalResult : IEquatable<PaymentsPortalResult>
    // Result of creating a Customer Portal session.
    ctor(string SessionId, string Url)
    string SessionId { get; init; }
    string Url { get; init; }
  // Slim view of a Stripe price.
  sealed class PaymentsPrice : IEquatable<PaymentsPrice>
    // Slim view of a Stripe price.
    ctor(string Id, string ProductId, long UnitAmountMinor, string Currency, string? RecurringInterval, bool Active, string? LookupKey = null)
    bool Active { get; init; }
    string Currency { get; init; }
    string Id { get; init; }
    string? LookupKey { get; init; }
    string ProductId { get; init; }
    string? RecurringInterval { get; init; }
    long UnitAmountMinor { get; init; }
  // Definition of a Stripe price. Use with CreatePriceAsync .
  sealed class PaymentsPriceInfo : IEquatable<PaymentsPriceInfo>
    bool Active { get; init; }
    string Currency { get; init; }
    // Stable Stripe-side lookup key (alphanumeric + underscores). Stripe price ids are opaque; setting LookupKey lets apps resolve a price via RetrievePriceByLookupKeyAsync without listing or storing the price id. Recommended pattern for app-owned plan catalogs.
    string? LookupKey { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? Nickname { get; init; }
    string ProductId { get; init; }
    // Set for recurring prices (subscriptions). Null = one-time price.
    string? RecurringInterval { get; init; }
    int? RecurringIntervalCount { get; init; }
    // When true, if a price with the same LookupKey already exists, Stripe transfers the lookup key to the new price (silently detaching from the previous one). Use when replacing a price (since Stripe prices are immutable) so the lookup-key handle stays stable.
    bool TransferLookupKey { get; init; }
    long UnitAmountMinor { get; init; }
  // Slim view of a Stripe product.
  sealed class PaymentsProduct : IEquatable<PaymentsProduct>
    // Slim view of a Stripe product.
    ctor(string Id, string Name, bool Active, string? Description, IReadOnlyList<string>? MarketingFeatures = null, IReadOnlyDictionary<string, string>? Metadata = null)
    bool Active { get; init; }
    string? Description { get; init; }
    string Id { get; init; }
    IReadOnlyList<string>? MarketingFeatures { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string Name { get; init; }
  // Definition of a Stripe product. Use with CreateProductAsync .
  sealed class PaymentsProductInfo : IEquatable<PaymentsProductInfo>
    bool Active { get; init; }
    string? Description { get; init; }
    string? Id { get; init; }
    IReadOnlyList<string>? Images { get; init; }
    // Marketing-feature bullets shown on Stripe-hosted Pricing Tables and adaptive Checkout UIs (e.g. "Unlimited workshops", "Priority support"). Stripe caps each entry at 80 characters and the array at 15 entries. Maps to the v1 marketing_features array on the Product object.
    IReadOnlyList<string>? MarketingFeatures { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string Name { get; init; }
    string? StatementDescriptor { get; init; }
  // Selects the Stripe transport used by PaymentsService .
  enum PaymentsProvider
    Disabled
    Byok
    IkonConnect
    Worldpay
    Vipps
  sealed class PaymentsPushEvent : IEquatable<PaymentsPushEvent>
    ctor(string EventId, string Space, string Provider, string Type, string OccurredAt, long Sequence, string PayloadJson)
    string EventId { get; init; }
    string OccurredAt { get; init; }
    string PayloadJson { get; init; }
    string Provider { get; init; }
    long Sequence { get; init; }
    string Space { get; init; }
    string Type { get; init; }
    JsonElement Payload()
  // Declares the function requires the current customer to hold an active subscription for planId . Resolves via the ambient Current instance and reads the customer from UserId . The policy is webhook-driven, not polling-driven: on missing entitlement it DENIES with a stable code (payments_subscription_required), and the app's UI catches it and opens checkout via CreateCheckoutAsync . Stripe's webhook then flips the entitlement and the user retries.
  sealed class PaymentsRequireSubscriptionAttribute : PolicyAttribute
    ctor(string planId)
    // App-side plan id the subscription is keyed to.
    string PlanId { get; }
    override IFunctionPolicy CreatePolicy()
  // Declares the function requires the current customer to hold a one-time unlock for planId . Reads UnlockGranted from the ambient Current . Deny code: payments_unlock_required. App UI handles checkout offer + retry.
  sealed class PaymentsRequireUnlockAttribute : PolicyAttribute
    ctor(string planId)
    string PlanId { get; }
    override IFunctionPolicy CreatePolicy()
  // Single entry point for app-level payments operations: hosted Checkout, Customer Portal, webhook verification + dispatch, metered usage reporting, subscription management, catalog, and refunds. Apps construct one instance per process and reuse it. This is a thin façade over an IPaymentsProvider selected from Provider — Stripe today ( StripePaymentsProvider ), with Worldpay/Vipps stubs wired for future providers. Mirrors the Ikon.AI pattern (neutral façade + per-provider implementation + capability flags). Operations a provider doesn't support throw PaymentsNotSupportedException ; query GetCapabilities first.
  sealed class PaymentsService
    ctor(PaymentsOptions options, IPaymentsAppAdapter adapter)
    // Optional app-supplied credit ledger. When set, GetEntitlementAsync uses it as the default credit-store unless caller passes their own, and the ChargeCreditsAttribute policy can locate it without extra plumbing. Mutable so apps can wire it after construction.
    IPaymentsCreditStore? CreditStore { get; set; }
    // Most recently constructed PaymentsService instance observable from the current execution flow. Set as a side effect of the constructor so ambient consumers (policy attributes like [PaymentsRequireSubscription], Parallax components that want a default) can resolve without DI. Backed by AsyncLocal`1 so per-flow values are isolated.
    static PaymentsService Current { get; }
    // The active payments provider behind this façade. Exposes GetCapabilities + Name .
    IPaymentsProvider Provider { get; }
    Task AddInvoiceItemAsync(string stripeCustomerId, long amountMinor, string currency, string description, string? subscriptionId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task AdjustCustomerBalanceAsync(string stripeCustomerId, long amountMinorDelta, string currency, string description, string idempotencyKey, CancellationToken cancellationToken = null)
    Task<string> CancelBackendSubscriptionAsync(string subscriptionId, bool immediate = false, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsPaymentIntent> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = null)
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, CancellationToken cancellationToken = null)
    Task CancelSubscriptionScheduleAsync(string scheduleId, CancellationToken cancellationToken = null)
    Task<PaymentsPaymentIntent> CapturePaymentIntentAsync(string paymentIntentId, long? amountToCaptureMinor = null, CancellationToken cancellationToken = null)
    Task<string> CreateBackendCheckoutAsync(string planId, string appCustomerKey, string? email = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateBackendOrderAsync(long amountMinor, string currency, string appCustomerKey, string? description = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsCheckoutResult> CreateCartCheckoutAsync(IEnumerable<PaymentsLineItem> lines, PaymentsMode mode, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, PaymentsDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsCheckoutResult> CreateCheckoutAsync(string planId, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, PaymentsDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateCouponAsync(PaymentsCouponInfo coupon, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsCreditNote> CreateCreditNoteAsync(PaymentsCreditNoteInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateCustomerAsync(PaymentsCustomerInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsTaxId> CreateCustomerTaxIdAsync(string stripeCustomerId, string type, string value, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsInvoice> CreateHostedInvoiceAsync(string stripeCustomerId, IEnumerable<PaymentsLineItem> lines, int daysUntilDue, bool autoSend = true, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsPaymentIntent> CreatePaymentIntentAsync(long amountMinor, string currency, string? stripeCustomerId = null, string captureMethod = "automatic", string? paymentMethodId = null, bool confirm = false, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsPaymentLink> CreatePaymentLinkAsync(IEnumerable<PaymentsLineItem> lines, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsPortalResult> CreatePortalAsync(string stripeCustomerId, string? returnUrl = null, string? configurationId = null, string? onBehalfOf = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreatePortalConfigurationAsync(PaymentsPortalConfigurationInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreatePriceAsync(PaymentsPriceInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateProductAsync(PaymentsProductInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreatePromotionCodeAsync(string couponId, string code, DateTimeOffset? expiresAt = null, long? maxRedemptions = null, string? restrictedToCustomerId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateSubscriptionScheduleAsync(string stripeCustomerId, IEnumerable<PaymentsSubscriptionPhase> phases, DateTimeOffset? startDate = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsCheckoutResult> CreateTipCheckoutAsync(long amountMinor, string currency, string? title = null, string? message = null, string? appCustomerKey = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<PaymentsWebhookEndpoint> CreateWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, PaymentsWebhookPayloadShape payloadShape = Snapshot, CancellationToken cancellationToken = null)
    Task DeleteCustomerTaxIdAsync(string stripeCustomerId, string taxIdId, CancellationToken cancellationToken = null)
    Task DeleteWebhookEndpointAsync(string webhookEndpointId, CancellationToken cancellationToken = null)
    Task DetachPaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = null)
    Task<string> GetBackendCapabilitiesAsync(CancellationToken cancellationToken = null)
    Task<string> GetBackendCatalogAsync(CancellationToken cancellationToken = null)
    Task<string> GetBackendEntitlementAsync(string featureKey, string appCustomerKey, CancellationToken cancellationToken = null)
    // Static capability flags for the active provider — query before driving an optional operation.
    ProviderCapabilities GetCapabilities()
    Task<PaymentsEntitlement> GetEntitlementAsync(string planId, string appCustomerKey, IPaymentsCreditStore? creditStore = null, CancellationToken cancellationToken = null)
    Task<PaymentsWebhookResult> HandleWebhookAsync(string? signatureHeader, string body, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListApplePayDomainsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<PaymentsCharge>> ListChargesAsync(string? stripeCustomerId = null, int limit = 100, DateTimeOffset? createdAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCouponsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCreditNotesAsync(string? invoiceId = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCustomerTaxIdsAsync(string stripeCustomerId, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListEventIdsAsync(string? type = null, DateTimeOffset? createdAfter = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<PaymentsInvoiceSummary>> ListInvoicesAsync(string? stripeCustomerId = null, string? subscriptionId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPaymentLinksAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<PaymentsPaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card", int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<PaymentsPrice>> ListPricesAsync(string? productId = null, bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    Task<PaymentsPage<PaymentsPrice>> ListPricesPageAsync(string? productId = null, bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<PaymentsProduct>> ListProductsAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    Task<PaymentsPage<PaymentsProduct>> ListProductsPageAsync(bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPromotionCodesAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<PaymentsSubscription>> ListSubscriptionsAsync(string? stripeCustomerId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListWebhookEndpointsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<PaymentsCheckoutOffer> OfferCheckoutAsync(string planId, string appCustomerKey, string? email = null, PaymentsDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task PauseSubscriptionAsync(string subscriptionId, string behavior = "void", CancellationToken cancellationToken = null)
    Task PingWebhookEndpointAsync(string webhookEndpointId, CancellationToken cancellationToken = null)
    Task<PaymentsUpcomingInvoice> PreviewUpcomingInvoiceAsync(string stripeCustomerId, string? subscriptionId = null, string? newPriceId = null, long? newQuantity = null, string? couponId = null, CancellationToken cancellationToken = null)
    Task RefundAsync(string paymentIntentId, long? amountMinor, string? reason, string idempotencyKey, bool refundApplicationFee = false, bool reverseTransfer = false, CancellationToken cancellationToken = null)
    Task<string> RefundBackendOrderAsync(string orderId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> RegisterApplePayDomainAsync(string domainName, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task ReportUsageAsync(string meterEventName, string stripeCustomerId, long value, string idempotencyKey, DateTimeOffset? timestamp = null, CancellationToken cancellationToken = null)
    Task ResumeCanceledSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    Task ResumeSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    Task<PaymentsEvent> RetrieveEventAsync(string eventId, CancellationToken cancellationToken = null)
    Task<PaymentsPrice?> RetrievePriceByLookupKeyAsync(string lookupKey, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersByAppKeyAsync(string appCustomerKey, string metadataKey = "app_customer_key", int limit = 1, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersDetailedAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    Task UpdateCustomerAsync(string stripeCustomerId, PaymentsCustomerInfo info, CancellationToken cancellationToken = null)
    Task UpdatePriceAsync(string priceId, bool? active = null, string? nickname = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    Task UpdateProductAsync(string productId, bool? active = null, string? name = null, string? description = null, IReadOnlyList<string>? marketingFeatures = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    Task UpdateSubscriptionItemQuantityAsync(string subscriptionItemId, long quantity, bool prorate = true, CancellationToken cancellationToken = null)
    Task UpdateSubscriptionPriceAsync(string subscriptionItemId, string newPriceId, bool prorate = true, CancellationToken cancellationToken = null)
    Task UpdateSubscriptionScheduleAsync(string scheduleId, IEnumerable<PaymentsSubscriptionPhase> phases, CancellationToken cancellationToken = null)
    Task VoidCreditNoteAsync(string creditNoteId, CancellationToken cancellationToken = null)
    event Func<PaymentsPushEvent, Task>? PaymentReceived
  // Slim view of a Stripe subscription. Returned by ListSubscriptionsAsync .
  sealed class PaymentsSubscription : IEquatable<PaymentsSubscription>
    // Slim view of a Stripe subscription. Returned by ListSubscriptionsAsync .
    ctor(string Id, string CustomerId, string Status, DateTimeOffset? CurrentPeriodStart, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd, string? DefaultPaymentMethodId, string? LatestInvoiceId, IReadOnlyList<string> ItemIds, string? FirstPriceId = null, string? FirstProductId = null)
    // True when subscription is scheduled to cancel at period end.
    bool CancelAtPeriodEnd { get; init; }
    // Current billing period end.
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    // Current billing period start.
    DateTimeOffset? CurrentPeriodStart { get; init; }
    // Customer id.
    string CustomerId { get; init; }
    // Saved payment method used for renewals.
    string? DefaultPaymentMethodId { get; init; }
    // Stripe price id (price_…) of the first item, when present. Use to resolve the plan via the catalog (reverse lookup against PlanIdToPriceId ).
    string? FirstPriceId { get; init; }
    // Stripe product id (prod_…) of the first item's price, when present. Use to resolve the plan name when prices are expanded server-side.
    string? FirstProductId { get; init; }
    // Subscription id (sub_...).
    string Id { get; init; }
    // Subscription item ids — pass to UpdateSubscriptionItemQuantityAsync .
    IReadOnlyList<string> ItemIds { get; init; }
    // Most recent invoice id, when present.
    string? LatestInvoiceId { get; init; }
    // active, trialing, past_due, canceled, incomplete, etc.
    string Status { get; init; }
  // One phase of a subscription schedule — a price + duration pair. Used by CreateSubscriptionScheduleAsync for multi-phase billing (e.g. discounted intro then full price).
  sealed class PaymentsSubscriptionPhase : IEquatable<PaymentsSubscriptionPhase>
    // One phase of a subscription schedule — a price + duration pair. Used by CreateSubscriptionScheduleAsync for multi-phase billing (e.g. discounted intro then full price).
    ctor(string StripePriceId, long Quantity = 1, int? Iterations = null)
    // How many billing cycles this phase lasts. Final phase may be open-ended (omit iterations on the last phase to make it run forever).
    int? Iterations { get; init; }
    // Quantity of the subscription line item.
    long Quantity { get; init; }
    // Stripe Price id active during this phase.
    string StripePriceId { get; init; }
  // Stripe Tax exemption modes. Maps to tax_exempt on the Stripe customer object.
  enum PaymentsTaxExempt
    None
    Exempt
    Reverse
  // Slim view of a customer's tax id record (VAT, GST, etc.).
  sealed class PaymentsTaxId : IEquatable<PaymentsTaxId>
    // Slim view of a customer's tax id record (VAT, GST, etc.).
    ctor(string Id, string Type, string Value, string? Country)
    // ISO country code, when present.
    string? Country { get; init; }
    // Stripe tax id object id (txi_...).
    string Id { get; init; }
    // Stripe tax id type (e.g. eu_vat, gb_vat, us_ein).
    string Type { get; init; }
    // The tax id value as the customer entered it.
    string Value { get; init; }
  // Preview of a customer's next invoice — used to show "your next bill will be X" UI before a plan change is committed. Returned by PreviewUpcomingInvoiceAsync .
  sealed class PaymentsUpcomingInvoice : IEquatable<PaymentsUpcomingInvoice>
    // Preview of a customer's next invoice — used to show "your next bill will be X" UI before a plan change is committed. Returned by PreviewUpcomingInvoiceAsync .
    ctor(long AmountDueMinor, long AmountPaidMinor, long SubtotalMinor, long TotalMinor, long? TotalDiscountAmountMinor, long? TaxMinor, string Currency, DateTimeOffset? PeriodStart, DateTimeOffset? PeriodEnd, DateTimeOffset? NextPaymentAttempt, IReadOnlyList<PaymentsUpcomingInvoiceLine> Lines)
    long AmountDueMinor { get; init; }
    long AmountPaidMinor { get; init; }
    string Currency { get; init; }
    IReadOnlyList<PaymentsUpcomingInvoiceLine> Lines { get; init; }
    DateTimeOffset? NextPaymentAttempt { get; init; }
    DateTimeOffset? PeriodEnd { get; init; }
    DateTimeOffset? PeriodStart { get; init; }
    long SubtotalMinor { get; init; }
    long? TaxMinor { get; init; }
    long? TotalDiscountAmountMinor { get; init; }
    long TotalMinor { get; init; }
  sealed class PaymentsUpcomingInvoiceLine : IEquatable<PaymentsUpcomingInvoiceLine>
    ctor(string? PriceId, string Description, long AmountMinor, string Currency, long Quantity, bool Proration)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    string Description { get; init; }
    string? PriceId { get; init; }
    bool Proration { get; init; }
    long Quantity { get; init; }
  // Result of registering or fetching a Stripe webhook endpoint.
  sealed class PaymentsWebhookEndpoint : IEquatable<PaymentsWebhookEndpoint>
    // Result of registering or fetching a Stripe webhook endpoint.
    ctor(string Id, string Url, string? Secret, string Status)
    // Endpoint id (we_...).
    string Id { get; init; }
    // Webhook signing secret. Stripe returns this only on creation; subsequent fetches return null.
    string? Secret { get; init; }
    // enabled or disabled.
    string Status { get; init; }
    // URL Stripe posts events to.
    string Url { get; init; }
  // Payload shape requested when registering a v2 event destination (POST /v2/core/event_destinations) — Stripe ships every event in one of these two shapes.
  enum PaymentsWebhookPayloadShape
    Snapshot
    Thin
  // Outcome of HandleWebhookAsync . Surfaces signature verification status without throwing — apps return HTTP 200 either way to avoid Stripe retry storms, but log unverified deliveries.
  sealed class PaymentsWebhookResult : IEquatable<PaymentsWebhookResult>
    // Outcome of HandleWebhookAsync . Surfaces signature verification status without throwing — apps return HTTP 200 either way to avoid Stripe retry storms, but log unverified deliveries.
    ctor(bool Verified, string? Reason, PaymentsEvent? Event, string? AdapterError = null, string? BackendIngestError = null)
    // Set when the signature verified and event parsed cleanly but ApplyEventAsync threw. Apps decide whether to return 200 (acknowledge, retry isn't useful) or 500 (let Stripe retry). Null when the adapter call succeeded or wasn't reached.
    string? AdapterError { get; init; }
    // Set on a BYOK app when the signature verified but forwarding the raw provider event to the Ikon backend's normalized payments store failed. The local adapter has already been called; this only signals that the backend mirror is out of date for this event and Stripe should be allowed to retry. Null when forwarding succeeded or wasn't attempted.
    string? BackendIngestError { get; init; }
    // Parsed event when Verified is true; null otherwise.
    PaymentsEvent? Event { get; init; }
    // Reason for failure when Verified is false; null on success.
    string? Reason { get; init; }
    // True when the Stripe signature was validated against the configured webhook secret.
    bool Verified { get; init; }
  // Static capability flags for a payments provider. Apps query these (via GetCapabilities ) before driving an operation a provider may not support — mirrors how Ikon.AI's ILLMInfo exposes per-model feature flags. Operations a provider lacks throw PaymentsNotSupportedException .
  sealed class ProviderCapabilities : IEquatable<ProviderCapabilities>
    ctor()
    // Provider exposes a products/prices/plans catalog (Stripe/PayPal). False for providers that take amounts per-payment with no catalog (Vipps).
    bool SupportsCatalog { get; init; }
    // Provider supports a marketplace/connect model with application fees.
    bool SupportsConnect { get; init; }
    // Provider supports tax-aware credit notes against invoices.
    bool SupportsCreditNotes { get; init; }
    // Provider has a first-class customer object that can be created/updated/searched (Stripe). False where identity is the wallet/app user (Vipps).
    bool SupportsCustomerObjects { get; init; }
    // Provider offers a hosted self-serve customer portal (Stripe Billing Portal).
    bool SupportsCustomerPortal { get; init; }
    // Provider can mint a hosted checkout / redirect URL the customer completes (Stripe Checkout, Vipps wallet redirect, PayPal approve link).
    bool SupportsHostedCheckout { get; init; }
    // Provider auto-bills a native subscription object (Stripe/PayPal). False where recurring is stored-credential / agreement + app-scheduled charges (Worldpay/Vipps).
    bool SupportsNativeSubscriptions { get; init; }
    // Provider supports shareable hosted payment links.
    bool SupportsPaymentLinks { get; init; }
    // Platform can provision a sub-merchant programmatically (Stripe Connect accounts, Worldpay Onboarding, PayPal Partner Referrals). False where onboarding is contractual (Vipps MSN).
    bool SupportsProgrammaticOnboarding { get; init; }
    // Provider supports programmatic refunds.
    bool SupportsRefunds { get; init; }
  // Result of RetrieveAccountAsync .
  sealed class StripeMerchantAccount : IEquatable<StripeMerchantAccount>
    // Result of RetrieveAccountAsync .
    ctor(string Id, bool DetailsSubmitted, bool ChargesEnabled, bool PayoutsEnabled, IReadOnlyList<string> RequirementsCurrentlyDue, IReadOnlyList<string> RequirementsEventuallyDue, string? RequirementsDisabledReason, string? Country = null, IReadOnlyDictionary<string, string>? CapabilityStatuses = null, string? EntityType = null, string? Dashboard = null)
    IReadOnlyDictionary<string, string>? CapabilityStatuses { get; init; }
    bool ChargesEnabled { get; init; }
    string? Country { get; init; }
    string? Dashboard { get; init; }
    bool DetailsSubmitted { get; init; }
    string? EntityType { get; init; }
    string Id { get; init; }
    bool PayoutsEnabled { get; init; }
    IReadOnlyList<string> RequirementsCurrentlyDue { get; init; }
    string? RequirementsDisabledReason { get; init; }
    IReadOnlyList<string> RequirementsEventuallyDue { get; init; }
  // Read-only inspector for Stripe Connect accounts and platform-Connect webhook destinations. In the redirect-only / Stripe-managed posture the platform backend is the sole driver of write operations on connected accounts (create, onboarding-link mint, status refresh). This client-side service exposes: retrieve a connected account's live state, fetch a v2 thin-event related object, and create the platform's Connect webhook endpoint (one per app).
  sealed class StripeMerchantService
    ctor(PaymentsOptions options)
    // Most recently constructed StripeMerchantService instance observable from the current execution flow.
    static StripeMerchantService Current { get; }
    // Create a platform webhook endpoint that receives events from every connected account (one endpoint serves all).
    Task<PaymentsWebhookEndpoint> CreateConnectWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, PaymentsWebhookPayloadShape payloadShape = Snapshot, CancellationToken cancellationToken = null)
    // Fetch the current state of the object a v2 thin event refers to.
    Task<string> FetchRelatedObjectAsync(string apiPath, CancellationToken cancellationToken = null)
    // Retrieve a connected account to inspect onboarding and capability status.
    Task<StripeMerchantAccount> RetrieveAccountAsync(string connectedAccountId, CancellationToken cancellationToken = null)
  // Stripe implementation of IPaymentsProvider : hosted Stripe Checkout, Customer Portal, webhook verification + dispatch, metered usage reporting, subscription management, catalog, and refunds. Talks to Stripe through an IStripeTransport (BYOK direct, or ikon-connect proxy) — that transport choice is internal to this provider and orthogonal to which payment provider is active. Constructed by PaymentsService (the public façade) via PaymentsProviderFactory.
  sealed class StripePaymentsProvider : IPaymentsProvider
    // Optional app-supplied credit ledger. When set, GetEntitlementAsync uses it as the default credit-store unless caller passes their own.
    IPaymentsCreditStore? CreditStore { get; set; }
    string Name { get; }
    // Add a one-off line item to a customer's next invoice. Used for B2B usage true-ups, mid-cycle add-ons, or arbitrary chargebacks.
    Task AddInvoiceItemAsync(string stripeCustomerId, long amountMinor, string currency, string description, string? subscriptionId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Adjust a customer's balance, in minor units. Negative values credit the customer (reduce future invoice amounts); positive values debit. Useful for refund-as-credit, goodwill credits, or service-failure credits.
    Task AdjustCustomerBalanceAsync(string stripeCustomerId, long amountMinorDelta, string currency, string description, string idempotencyKey, CancellationToken cancellationToken = null)
    // Cancel a payment intent that hasn't been captured.
    Task<PaymentsPaymentIntent> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = null)
    // Cancel a Stripe subscription. immediate false = cancel at period end (Stripe keeps the subscription active until then); true = end now and prorate.
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, CancellationToken cancellationToken = null)
    // Cancel a subscription schedule. The current phase ends; no further phases run.
    Task CancelSubscriptionScheduleAsync(string scheduleId, CancellationToken cancellationToken = null)
    // Capture a previously authorized (manual capture) payment intent.
    Task<PaymentsPaymentIntent> CapturePaymentIntentAsync(string paymentIntentId, long? amountToCaptureMinor = null, CancellationToken cancellationToken = null)
    // Create a Stripe Checkout session with arbitrary line items — preconfigured prices, dynamic per-call amounts (donations, tipping, custom carts), or a mix. Use ForPrice and Dynamic .
    Task<PaymentsCheckoutResult> CreateCartCheckoutAsync(IEnumerable<PaymentsLineItem> lines, PaymentsMode mode, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, PaymentsDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe Checkout session for a single plan. Pass appCustomerKey to bind the session to an existing app entity (the adapter resolves a Stripe customer); pass null for guest checkout (Stripe creates a customer from the supplied email ).
    Task<PaymentsCheckoutResult> CreateCheckoutAsync(string planId, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, PaymentsDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe coupon. Set exactly one of PercentOff or AmountOffMinor . For repeating coupons supply DurationInMonths .
    Task<string> CreateCouponAsync(PaymentsCouponInfo coupon, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Issue a credit note against a finalized invoice. Use credit notes — not raw refunds — when tax was charged on the invoice; Stripe handles the tax reversal and regenerates the PDF. Apps split the credit between an out-of-pocket refund ( info . RefundAmountMinor ) and a customer-balance credit ( CreditAmountMinor ).
    Task<PaymentsCreditNote> CreateCreditNoteAsync(PaymentsCreditNoteInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a new Stripe customer directly (independent of checkout). Useful for B2B flows where the customer record needs to exist before any payment, or for invoice-only billing.
    Task<string> CreateCustomerAsync(PaymentsCustomerInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Attach a tax id (VAT, GST, etc.) to an existing customer.
    Task<PaymentsTaxId> CreateCustomerTaxIdAsync(string stripeCustomerId, string type, string value, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create, finalize, and (optionally) send a hosted Stripe invoice. Used for B2B net-30 flows: the customer receives a payable invoice URL by email and pays without a Checkout session.
    Task<PaymentsInvoice> CreateHostedInvoiceAsync(string stripeCustomerId, IEnumerable<PaymentsLineItem> lines, int daysUntilDue, bool autoSend = true, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe payment intent — the building block for custom in-app payment flows that don't use Checkout. Apps pass the returned ClientSecret to Stripe.js / Elements on the frontend.
    Task<PaymentsPaymentIntent> CreatePaymentIntentAsync(long amountMinor, string currency, string? stripeCustomerId = null, string captureMethod = "automatic", string? paymentMethodId = null, bool confirm = false, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe Payment Link — a shareable hosted-checkout URL for a fixed line item. Useful for "pay link" flows in chat, email, QR codes.
    Task<PaymentsPaymentLink> CreatePaymentLinkAsync(IEnumerable<PaymentsLineItem> lines, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Customer Portal session so the customer can manage their subscription.
    Task<PaymentsPortalResult> CreatePortalAsync(string stripeCustomerId, string? returnUrl = null, string? configurationId = null, string? onBehalfOf = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Customer Portal configuration. Apps that want to control which self-serve features the portal exposes (cancel, update payment method, view invoices, etc.) call this once and reuse the returned id when opening portal sessions via CreatePortalAsync .
    Task<string> CreatePortalConfigurationAsync(PaymentsPortalConfigurationInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe price (one-time or recurring) attached to a product.
    Task<string> CreatePriceAsync(PaymentsPriceInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe product. Apps that build catalogs programmatically can call this instead of clicking through the Dashboard.
    Task<string> CreateProductAsync(PaymentsProductInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a promotion code attached to a Stripe coupon. Apps create promotion codes for marketing campaigns, partner deals, etc. The couponId must already exist in Stripe (managed in the Dashboard or via Stripe API or CreateCouponAsync ).
    Task<string> CreatePromotionCodeAsync(string couponId, string code, DateTimeOffset? expiresAt = null, long? maxRedemptions = null, string? restrictedToCustomerId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe subscription schedule with multiple phases — useful for discounted intro phases that transition to standard pricing, or annual commitments built from a sequence of monthly phases.
    Task<string> CreateSubscriptionScheduleAsync(string stripeCustomerId, IEnumerable<PaymentsSubscriptionPhase> phases, DateTimeOffset? startDate = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a one-time hosted checkout for a tip / voluntary payment. Confers no entitlement — apps record the transaction for attribution / reporting and (optionally) ack it in the UI. Wraps CreateCartCheckoutAsync with a dynamic line item; metadata is stamped with tip_amount_minor for downstream reporting.
    Task<PaymentsCheckoutResult> CreateTipCheckoutAsync(long amountMinor, string currency, string? title = null, string? message = null, string? appCustomerKey = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Register a webhook endpoint with Stripe programmatically. The returned Secret is the signing secret — store it securely; Stripe will not return it again on subsequent reads.
    Task<PaymentsWebhookEndpoint> CreateWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, PaymentsWebhookPayloadShape payloadShape = Snapshot, CancellationToken cancellationToken = null)
    // Delete a tax id from a customer.
    Task DeleteCustomerTaxIdAsync(string stripeCustomerId, string taxIdId, CancellationToken cancellationToken = null)
    // Delete a webhook endpoint by id. Uses the v2 DELETE /v2/core/event_destinations/{id} verb.
    Task DeleteWebhookEndpointAsync(string webhookEndpointId, CancellationToken cancellationToken = null)
    // Detach a saved payment method from its customer.
    Task DetachPaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = null)
    ProviderCapabilities GetCapabilities()
    // One-shot "does this customer have access to this plan" snapshot — composes the adapter customer resolution, a filtered subscription list, a customer-metadata read, and (optionally) a credit-store lookup into a single PaymentsEntitlement record. Subscription gate: filters Stripe subscriptions by the plan's StripePriceId + status in active|trialing. Cancel-at-period-end subscriptions stay SubscriptionActive =true until the period ends (mirrors Stripe semantics).Unlock gate: reads customer metadata key unlock_{planId}. Apps stamp this key (ISO-8601 timestamp value) from ApplyEventAsync when CheckoutCompleted arrives for a one-time plan.Credit gate: when creditStore is supplied, queries the customer's wallet for the SKU. Pass null when the plan is subscription-or-unlock only.
    Task<PaymentsEntitlement> GetEntitlementAsync(string planId, string appCustomerKey, IPaymentsCreditStore? creditStore = null, CancellationToken cancellationToken = null)
    // Verify and dispatch a Stripe webhook delivery. Returns a structured result; never throws on signature failure. When Verified is true the parsed event has already been delivered to ApplyEventAsync .
    Task<PaymentsWebhookResult> HandleWebhookAsync(string? signatureHeader, string body, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListApplePayDomainsAsync(int limit = 100, CancellationToken cancellationToken = null)
    // List charges, optionally filtered to one customer. Used for app-side receipts and admin reporting screens.
    Task<IReadOnlyList<PaymentsCharge>> ListChargesAsync(string? stripeCustomerId = null, int limit = 100, DateTimeOffset? createdAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCouponsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCreditNotesAsync(string? invoiceId = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCustomerTaxIdsAsync(string stripeCustomerId, int limit = 100, CancellationToken cancellationToken = null)
    // List Stripe events for replay or audit. Apps that missed a webhook delivery (downtime) refetch via this and feed the events back through HandleWebhookAsync -equivalent dispatch.
    Task<IReadOnlyList<string>> ListEventIdsAsync(string? type = null, DateTimeOffset? createdAfter = null, int limit = 100, CancellationToken cancellationToken = null)
    // List invoices, optionally filtered to one customer or subscription.
    Task<IReadOnlyList<PaymentsInvoiceSummary>> ListInvoicesAsync(string? stripeCustomerId = null, string? subscriptionId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPaymentLinksAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    // List a customer's saved payment methods. Apps display these on a "manage payment methods" screen.
    Task<IReadOnlyList<PaymentsPaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card", int limit = 100, CancellationToken cancellationToken = null)
    // List prices, optionally filtered to a single product (single page). For catalogs > 100 prices use ListPricesPageAsync to paginate.
    Task<IReadOnlyList<PaymentsPrice>> ListPricesAsync(string? productId = null, bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    // One page of prices with cursor. Pass the returned LastId back as startingAfter on the next call to walk the full price set. Loop until HasMore is false.
    Task<PaymentsPage<PaymentsPrice>> ListPricesPageAsync(string? productId = null, bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    // List products in the catalog (single page). For catalogs > 100 products use ListProductsPageAsync to paginate.
    Task<IReadOnlyList<PaymentsProduct>> ListProductsAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    // One page of products with cursor. Pass the returned LastId back as startingAfter on the next call to walk the full catalog. Loop until HasMore is false.
    Task<PaymentsPage<PaymentsProduct>> ListProductsPageAsync(bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPromotionCodesAsync(int limit = 100, CancellationToken cancellationToken = null)
    // List Stripe subscriptions, optionally filtered by customer or status.
    Task<IReadOnlyList<PaymentsSubscription>> ListSubscriptionsAsync(string? stripeCustomerId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListWebhookEndpointsAsync(int limit = 100, CancellationToken cancellationToken = null)
    // Convenience: check entitlement first, then mint a checkout session only if the customer doesn't already have access. Returns a PaymentsCheckoutOffer describing which branch fired. App pattern: var offer = await billing.OfferCheckoutAsync("pro", appCustomerKey); if (offer.AlreadyEntitled) { } else { await ClientFunctions.SetUrlAsync(offer.Url!); } Subscription mode counts active+trialing as "entitled"; one-time mode counts a customer-metadata unlock stamp as "entitled".
    Task<PaymentsCheckoutOffer> OfferCheckoutAsync(string planId, string appCustomerKey, string? email = null, PaymentsDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Pause collection on a subscription. The subscription remains active for access purposes; Stripe just stops creating invoices until resumed.
    Task PauseSubscriptionAsync(string subscriptionId, string behavior = "void", CancellationToken cancellationToken = null)
    // Send a test ping to a registered webhook endpoint (POST /v2/core/event_destinations/{id}/ping). Stripe delivers a synthetic v2.core.event_destination.ping event to verify the endpoint's HTTP plumbing + signature verification before going live.
    Task PingWebhookEndpointAsync(string webhookEndpointId, CancellationToken cancellationToken = null)
    // Preview a customer's upcoming invoice. Use to show "your next bill" before committing a plan change, seat-count change, or coupon.
    Task<PaymentsUpcomingInvoice> PreviewUpcomingInvoiceAsync(string stripeCustomerId, string? subscriptionId = null, string? newPriceId = null, long? newQuantity = null, string? couponId = null, CancellationToken cancellationToken = null)
    // Refund a charge or payment intent, in full or partially. Use a stable idempotencyKey (typically the app's refund record id).
    Task RefundAsync(string paymentIntentId, long? amountMinor, string? reason, string idempotencyKey, bool refundApplicationFee = false, bool reverseTransfer = false, CancellationToken cancellationToken = null)
    // Register an Apple Pay domain so the domain can host Apple Pay buttons.
    Task<string> RegisterApplePayDomainAsync(string domainName, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Report a meter event for metered usage billing. Apps call this whenever a billable usage unit is consumed.
    Task ReportUsageAsync(string meterEventName, string stripeCustomerId, long value, string idempotencyKey, DateTimeOffset? timestamp = null, CancellationToken cancellationToken = null)
    // Un-cancel a subscription that was scheduled to cancel at period end. Clears cancel_at_period_end. The subscription continues normally. Has no effect if the subscription is already fully canceled.
    Task ResumeCanceledSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    // Resume collection on a paused subscription.
    Task ResumeSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    // Retrieve a single Stripe event by id, parsed into a typed PaymentsEvent . Apps use this for webhook replay: fetch the event and feed it into the same handler that ApplyEventAsync runs, but skip signature checks since the body came from Stripe directly.
    Task<PaymentsEvent> RetrieveEventAsync(string eventId, CancellationToken cancellationToken = null)
    // Resolve a price by its app-set LookupKey . Returns null when no active price has that lookup key. O(1) on the Stripe side; no listing or pagination needed.
    Task<PaymentsPrice?> RetrievePriceByLookupKeyAsync(string lookupKey, CancellationToken cancellationToken = null)
    // Search Stripe customers using Stripe's search query syntax (e.g. email:'biz@example.com', metadata['app_id']:'abc').
    Task<IReadOnlyList<string>> SearchCustomersAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    // Convenience wrapper over SearchCustomersAsync that builds the Stripe Search query metadata['app_customer_key']:'X' — the recommended idiom for resolving Stripe customer ids from an app's stable user key. Returns matched customer ids (typically 0 or 1).
    Task<IReadOnlyList<string>> SearchCustomersByAppKeyAsync(string appCustomerKey, string metadataKey = "app_customer_key", int limit = 1, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersDetailedAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    // Update mutable fields on an existing Stripe customer.
    Task UpdateCustomerAsync(string stripeCustomerId, PaymentsCustomerInfo info, CancellationToken cancellationToken = null)
    // Update mutable fields on an existing price. Stripe prices are immutable in their amount/currency/recurring shape, but active, nickname and metadata can change. Use active = false to archive an old price after migrating subscribers off it.
    Task UpdatePriceAsync(string priceId, bool? active = null, string? nickname = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    // Update mutable fields on an existing product. Use active = false to archive a product (and its prices) when retiring a plan.
    Task UpdateProductAsync(string productId, bool? active = null, string? name = null, string? description = null, IReadOnlyList<string>? marketingFeatures = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    // Update the quantity of a subscription item — typically used for seat-based billing where a customer adds or removes editor seats mid-cycle.
    Task UpdateSubscriptionItemQuantityAsync(string subscriptionItemId, long quantity, bool prorate = true, CancellationToken cancellationToken = null)
    // Swap the price on a subscription item (e.g. migrate an existing subscriber to a new price after a plan change, since Stripe prices are immutable). Stripe prorates by default — pass prorate = false for clean cycle boundaries. Typical flow after a plan-price bump: // 1. Sync catalog → new price id under same lookup_key var map = await catalogSync.SyncAsync(plans); // 2. Migrate active subscribers foreach (var sub in await billing.ListSubscriptionsAsync(status: "active")) { await billing.UpdateSubscriptionPriceAsync(sub.ItemIds[0], map.GetPriceId("pro")); }
    Task UpdateSubscriptionPriceAsync(string subscriptionItemId, string newPriceId, bool prorate = true, CancellationToken cancellationToken = null)
    // Replace the phases on an existing subscription schedule. Used when a schedule needs to be re-planned mid-flight (e.g. customer renegotiated).
    Task UpdateSubscriptionScheduleAsync(string scheduleId, IEnumerable<PaymentsSubscriptionPhase> phases, CancellationToken cancellationToken = null)
    // Void a previously issued credit note.
    Task VoidCreditNoteAsync(string creditNoteId, CancellationToken cancellationToken = null)
  // Vipps MobilePay provider — stub. Wired into PaymentsService so a future PR can implement the operations without changing the abstraction. Only GetCapabilities is real today; every operation inherits the throwing default from IPaymentsProvider . When implemented, this maps the neutral operations onto Vipps' (verified against developer.vippsmobilepay.com): Auth: token exchange — POST /accesstoken/get with client_id + client_secret + Ocp-Apim-Subscription-Key + Merchant-Serial-Number → bearer token on every call. Its own transport.Checkout: ePayment POST /epayment/v1/payments (JSON { amount:{currency,value}, paymentMethod:{type:WALLET}, returnUrl, userFlow:"WEB_REDIRECT", reference }) → a wallet app-redirect URL the customer completes; then capture/cancel. Fits the neutral PaymentsCheckoutResult { Url }.Recurring: the Recurring API — agreement (mandate) + app-scheduled charges. NOT an auto-billed subscription object.No products/prices catalog, no first-class customer object, no programmatic merchant onboarding (merchant identified by MSN + partner keys; onboarding is contractual). The merchant binding stores the MSN as its merchant id.
  sealed class VippsPaymentsProvider : IPaymentsProvider
    ctor()
    IPaymentsCreditStore? CreditStore { get; set; }
    string Name { get; }
    ProviderCapabilities GetCapabilities()
  // Worldpay (Access) provider — stub. Wired into PaymentsService so a future PR can implement the operations without changing the abstraction. Only GetCapabilities is real today; every operation inherits the throwing default from IPaymentsProvider . When implemented, this maps the neutral operations onto Worldpay's Access API model, which differs sharply from Stripe: JSON request/response (not form-encoded), HATEOAS _links the client follows (refund/cancel/settle a payment via the link returned on it, rather than fixed paths), versioned media types (application/vnd.worldpay…+json) instead of a Stripe-Version header, HTTP Basic auth, sub-merchant onboarding via the Onboarding API, and recurring via stored-credential merchant-initiated transactions (no native auto-billed subscription object). It needs its own transport — Worldpay cannot ride the Stripe form transport.
  sealed class WorldpayPaymentsProvider : IPaymentsProvider
    ctor()
    IPaymentsCreditStore? CreditStore { get; set; }
    string Name { get; }
    ProviderCapabilities GetCapabilities()

# Ikon.Resonance Public API

namespace Ikon.Resonance
  // Extended audio frame with encoding options, analysis results, and target information.
  struct AudioFrameEx
    // Extended audio frame with encoding options, analysis results, and target information.
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId, TimeSpan totalDuration = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, IReadOnlyList<AudioAnalysisResult>? analysisResults = null, IReadOnlyList<AudioShapeSetDeclaration>? shapeSetDeclarations = null)
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
  // Manages multiple audio sources and generates audio frames at a fixed rate (20ms at 48kHz stereo). Supports adding/removing sources dynamically, applying audio effects, and simulating network conditions. All options, sources, and effects can be changed while the generator is running without restart.
  sealed class AudioGenerator
    ctor()
    // Gets a value indicating whether the audio generator is currently running.
    bool IsRunning { get; }
    // Gets the current options. To modify options, use UpdateOptions .
    AudioGeneratorOptions Options { get; }
    // Adds an audio effect to the effects chain. Effects are applied in order to all audio output.
    void AddEffect(IAudioEffect effect)
    // Adds an audio source to the generator.
    string AddSource(IAudioSource source)
    // Removes all audio effects from the effects chain.
    void ClearEffects()
    T GetSource<T>(string streamId) where T : class, IAudioSource
    IEnumerable<ValueTuple<string, T>> GetSourcesOfType<T>() where T : class, IAudioSource
    // Removes an audio effect at the specified index from the effects chain.
    void RemoveEffectAt(int index)
    // Marks an audio source for removal. The source will be removed after its final frame is sent.
    bool RemoveSource(string streamId)
    // Replaces an audio effect at the specified index with a new effect.
    void ReplaceEffect(int index, IAudioEffect newEffect)
    // Starts the audio generation loop asynchronously.
    Task StartAsync(Func<AudioGeneratorFrame, ValueTask> onFrame, Func<string, ValueTask>? onStreamEnd = null, CancellationToken cancellationToken = null)
    // Stops the audio generation loop and waits for it to complete.
    Task StopAsync()
    // Updates the generator options dynamically. Changes take effect on the next frame.
    void UpdateOptions(Action<AudioGeneratorOptions> configure)
  // Output frame from the AudioGenerator.
  struct AudioGeneratorFrame
    // Output frame from the AudioGenerator.
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId)
    int ChannelCount { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    int SampleRate { get; }
    ReadOnlyMemory<float> Samples { get; }
    string StreamId { get; }
  // Configuration options for the AudioGenerator to simulate various network conditions such as jitter, drift, burst transmission, and periodic pauses. All options can be changed dynamically while the generator is running.
  sealed class AudioGeneratorOptions
    ctor()
    // Number of packets to send in each burst.
    int BurstPacketCount { get; set; }
    // Drift factor: 1.0 = realtime, 1.1 = 10% faster, 0.9 = 10% slower.
    double DriftFactor { get; set; }
    // Enable burst mode - sends multiple packets at once, then waits. Exercises buffer overflow handling on the receiver.
    bool EnableBurstMode { get; set; }
    // Enable drift simulation - sends audio faster or slower than real-time. Exercises driftCorrection on the receiver.
    bool EnableDrift { get; set; }
    // Enable jitter simulation - adds random timing variation to each packet. Exercises jitterTracking and adaptiveBuffering on the receiver.
    bool EnableJitter { get; set; }
    // Enable periodic pauses in packet sending. Exercises buffer underrun handling on the receiver.
    bool EnablePause { get; set; }
    // Maximum jitter magnitude in milliseconds. Actual jitter varies from -JitterMs to +JitterMs.
    int JitterMs { get; set; }
    // Duration of each pause in milliseconds.
    int PauseDurationMs { get; set; }
    // Interval between pauses in milliseconds (time of active sending before each pause).
    int PauseIntervalMs { get; set; }
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
  // High-performance adaptive timer for audio frame pacing. Learns the actual sleep behavior of the OS and adjusts dynamically to minimize CPU usage while maintaining precise timing for audio frame delivery.
  sealed class AudioTimer
    ctor()
    // Resets the timer state. Call when timing context changes significantly (e.g., after pausing/resuming audio, changing audio sources).
    void Reset()
    // Synchronous version for scenarios where async is not available. Uses Thread.Sleep instead of Task.Delay.
    void WaitUntil(long targetTicks, CancellationToken token)
    // Waits until the target time, using adaptive sleeping to minimize CPU usage.
    Task WaitUntilAsync(long targetTicks, CancellationToken token)
  // Provides utility methods for converting audio samples between PCM 16-bit integer and 32-bit float formats.
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
    void AddStream(string streamId, string excludeKey)
    ValueTask DisposeAsync()
    // Unregisters a participant. They will no longer receive mixed audio output.
    void RemoveParticipant(string excludeKey)
    void RemoveStream(string streamId)
    Task StartAsync(Func<string, AudioFrameEx, ValueTask> onFrame, CancellationToken cancellationToken = null)
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  // Configuration for the GroupAudioMixer .
  sealed class GroupAudioMixerConfig
    ctor()
    // Maximum buffer size per stream in milliseconds.
    double MaxBufferSizeMs { get; set; }
  // Represents a source that generates audio frames.
  interface IAudioSource
    // Generates a frame of audio into the provided buffer.
    abstract void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
  enum WavFile.SampleFormat
    Short
    Float
  // Filters silence from an audio chunk stream so that only speech reaches downstream consumers such as speech-to-text models (which tend to hallucinate on silent input). Uses asymmetric EMA for level tracking, an adaptive noise floor, and a circular pre-buffer to ensure speech onsets are never clipped. Designed for real-time audio at typical frame sizes (20 ms). Usage — push-based: call ProcessChunk per audio chunk, forward non-null results. Usage — stream-based: wrap an IAsyncEnumerable`1 source with FilterAsync .
  sealed class SilenceRemover
    // Creates a new SilenceRemover for the given audio format.
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    // Wraps an async audio source, yielding only chunks that contain speech. Silence is suppressed and speech onsets include look-back audio from the pre-buffer.
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = null)
    // Processes a single audio chunk and determines whether it should be forwarded downstream. Returns the samples to forward (including pre-buffered onset audio when speech begins), or null if the chunk is silence that should be suppressed.
    float[]? ProcessChunk(ReadOnlySpan<float> chunk)
    // Resets all internal state (EMA level, noise floor, pre-buffer, and state machine) to initial values. Call this when starting a new audio session on the same instance.
    void Reset()
  // Configuration for SilenceRemover . The silence remover uses asymmetric EMA (exponential moving average) to track audio level, an adaptive noise floor that adjusts to the environment, and a circular pre-buffer that preserves the onset of speech so words are never clipped. The speech threshold is computed as: noiseFloor * NoiseFloorMultiplier + NoiseFloorOffset .
  sealed class SilenceRemoverConfig
    ctor()
    // EMA smoothing factor for rising audio levels (0..1). Higher values respond faster to speech onset.
    float AttackAlpha { get; set; }
    // Starting noise floor estimate before any audio has been analyzed.
    float InitialNoiseFloor { get; set; }
    // Upper bound for the adaptive noise floor. Prevents the speech threshold from rising too high in very noisy environments.
    float MaxNoiseFloor { get; set; }
    // How fast the noise floor adapts during silence (0..1). Keep low to prevent speech from contaminating the noise floor estimate.
    float NoiseFloorAlpha { get; set; }
    // Speech threshold multiplier above the noise floor. Higher values are less sensitive and produce fewer false triggers from background noise.
    float NoiseFloorMultiplier { get; set; }
    // Absolute offset added to the speech threshold to prevent it from reaching zero in digital silence. Ensures a minimum sensitivity level.
    float NoiseFloorOffset { get; set; }
    // Milliseconds of recent audio kept in the circular look-back buffer. This audio is emitted on speech onset to preserve word beginnings that would otherwise be clipped.
    int PreBufferMs { get; set; }
    // EMA smoothing factor for falling audio levels (0..1). Lower values decay slower, holding through natural pauses in speech.
    float ReleaseAlpha { get; set; }
    // Number of consecutive above-threshold chunks required to confirm speech onset. Filters transient clicks and noise bursts from triggering false speech detection.
    int SpeechOnsetChunks { get; set; }
    // Milliseconds of trailing audio to include after the last speech chunk. Allows natural word endings and brief pauses to pass through before returning to silence state.
    int TrailingSilenceMs { get; set; }
  // Simplified audio mixer for speech output with precise 20ms frame timing. Handles one speech event at a time with smooth crossfade transitions.
  sealed class SpeechMixer : IAsyncDisposable
    ctor(SpeechMixerConfig? config = null)
    // Encoder options to use for audio output.
    AudioEncoderOptions? EncoderOptions { get; set; }
    bool IsPaused { get; }
    string StreamId { get; }
    void AddSamples(AudioContainer container, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    void AddSamples(string speechEventId, ReadOnlySpan<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    void Clear()
    ValueTask DisposeAsync()
    void FadeOut()
    void Pause()
    void Resume()
    Task StartAsync(Func<AudioFrameEx, ValueTask> onFrame, CancellationToken cancellationToken = null)
  // Configuration options for the SpeechMixer.
  sealed class SpeechMixerConfig
    ctor()
    // Crossfade curve type. EqualPower maintains constant perceived loudness.
    CrossfadeCurve CrossfadeCurve { get; set; }
    // Duration of silence padding after speech and effects end (in milliseconds). This prevents fadeout from triggering at natural speech endings.
    double EndPaddingMs { get; set; }
    // Duration of fade-in when speech starts (in milliseconds).
    double FadeInMs { get; set; }
    // Fade transition mode when new speech interrupts current speech. Sequential: fade out completes before fade in starts. Crossfade: fade out and fade in happen simultaneously.
    FadeMode FadeMode { get; set; }
    // Duration of fade-out when speech ends or is interrupted (in milliseconds).
    double FadeOutMs { get; set; }
    // Maximum buffer size in milliseconds for incoming speech samples. This is an upper bound only; the queue grows from a small initial size on demand. Keep this generous enough to absorb production-faster-than-playback bursts (typical for non-streaming TTS) but tight enough that a runaway producer can't consume excessive memory. Samples added beyond this bound are dropped (with a throttled warning) rather than throwing; the backing buffer is released once the event drains, so this only caps the transient in-flight footprint.
    double MaxBufferSizeMs { get; set; }
    // Maximum padding duration in milliseconds for effect tails. Prevents infinite padding if effects never fully decay.
    double MaxPaddingTimeMs { get; set; }
    // RMS threshold below which effect tail padding stops. Default is 0.001 (~-60dB), meaning padding continues until output is essentially silent.
    double PaddingThreshold { get; set; }
  // Creates WAV audio files in memory with support for 16-bit integer or 32-bit float sample formats. Samples are written incrementally and the WAV header is finalized when the file is accessed.
  class WavFile : IDisposable
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
    // The shape set ID this result belongs to.
    uint SetId { get; set; }
    // The analysis values for this shape set.
    float[] Values { get; set; }
  // Declaration of a shape set with ID and shape names.
  struct AudioShapeSetDeclaration
    // Human-readable name for the shape set (e.g., "Viseme", "Sentiment").
    string Name { get; set; }
    // Unique identifier for this shape set.
    uint SetId { get; set; }
    // Names of each shape in the set, in order (e.g., ["MouthOpenY", "MouthForm"]).
    string[] ShapeNames { get; set; }
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

namespace Ikon.Resonance.Synth
  // A synthesized drum machine IAudioSource that generates kick, hi-hat, and melody patterns at a specified BPM. Uses synthesis rather than samples for all drum sounds.
  sealed class DrumMachineSource : IAudioSource
    // A synthesized drum machine IAudioSource that generates kick, hi-hat, and melody patterns at a specified BPM. Uses synthesis rather than samples for all drum sounds.
    ctor(double bpm)
    double Bpm { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
  // A simple IAudioSource that generates stereo sine waves from a pentatonic scale. Features slight stereo detuning for a wider sound.
  sealed class SineWaveSource : IAudioSource
    ctor(int frequencyIndex)
    int FrequencyIndex { get; }
    double FrequencyLeft { get; }
    double FrequencyRight { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)

namespace Ikon.Resonance.Synth.Envelopes
  // Implements an Attack-Decay-Sustain-Release (ADSR) envelope generator for amplitude and filter modulation. Uses exponential curves for natural-sounding transitions between stages.
  sealed class AdsrEnvelope
    ctor()
    double Attack { get; set; }
    double Decay { get; set; }
    bool IsActive { get; }
    double Output { get; }
    double Release { get; set; }
    EnvelopeStage Stage { get; }
    double Sustain { get; set; }
    void Gate(bool gate)
    void NoteOff()
    void NoteOn()
    double Process()
    void Reset()
    void SetSampleRate(double sampleRate)
  // Represents the current stage of an ADSR envelope.
  enum EnvelopeStage
    Idle
    Attack
    Decay
    Sustain
    Release

namespace Ikon.Resonance.Synth.Filters
  // Emulates the classic Moog ladder filter, a 4-pole (24dB/octave) low-pass filter with resonance. Features non-linear saturation for analog-style warmth.
  sealed class MoogLadderFilter
    ctor()
    double Cutoff { get; set; }
    double Drive { get; set; }
    double Resonance { get; set; }
    double Process(double input)
    void Reset()
    void SetSampleRate(double sampleRate)

namespace Ikon.Resonance.Synth.Modulation
  // Low Frequency Oscillator (LFO) for modulating synthesizer parameters such as pitch, filter cutoff, and pulse width. Supports multiple waveform shapes and configurable rate.
  sealed class Lfo
    ctor()
    double Phase { get; }
    double Rate { get; set; }
    LfoWaveform Waveform { get; set; }
    double Process()
    void Reset()
    void SetSampleRate(double sampleRate)
    void Sync()
  // Defines the waveform shapes available for the LFO.
  enum LfoWaveform
    Sine
    Triangle
    Saw
    Square
    SampleAndHold

namespace Ikon.Resonance.Synth.Moog
  // A polyphonic virtual analog synthesizer inspired by classic Moog synthesizers. Features dual oscillators, sub-oscillator, Moog ladder filter, dual envelopes, and LFO modulation.
  sealed class MoogSynth
    ctor(int voiceCount = 8)
    Lfo Lfo { get; }
    double NoiseFloor { get; set; }
    MoogSynthPatch Patch { get; set; }
    VoiceAllocator VoiceAllocator { get; }
    void AllNotesOff()
    void ApplyPatch()
    void NoteOff(int noteNumber)
    void NoteOn(int noteNumber, double velocity = 1)
    double Process()
    void Process(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
    void Reset()
    void SetSampleRate(double sampleRate)
  // Defines all configurable parameters for the Moog synthesizer including oscillator levels, filter settings, envelope times, LFO modulation, and master volume.
  sealed class MoogSynthPatch
    ctor()
    double AmpAttack { get; set; }
    double AmpDecay { get; set; }
    double AmpRelease { get; set; }
    double AmpSustain { get; set; }
    double DriftAmount { get; set; }
    double FilterAttack { get; set; }
    double FilterCutoff { get; set; }
    double FilterDecay { get; set; }
    double FilterEnvAmount { get; set; }
    double FilterKeyTrack { get; set; }
    double FilterRelease { get; set; }
    double FilterResonance { get; set; }
    double FilterSustain { get; set; }
    double LfoRate { get; set; }
    double LfoToFilter { get; set; }
    double LfoToPitch { get; set; }
    double LfoToPwm { get; set; }
    LfoWaveform LfoWaveform { get; set; }
    double MasterVolume { get; set; }
    string Name { get; set; }
    double NoiseLevel { get; set; }
    double Osc1Level { get; set; }
    double Osc2Level { get; set; }
    double Osc2PulseWidth { get; set; }
    double SubLevel { get; set; }
  // Provides a collection of preset patches for the Moog synthesizer including basses, leads, pads, and brass sounds.
  static class MoogSynthPresets
    static MoogSynthPatch AcidLead()
    static MoogSynthPatch[] All()
    static MoogSynthPatch Brass()
    static MoogSynthPatch FatBass()
    static MoogSynthPatch FilterSweep()
    static MoogSynthPatch LushPad()
    static MoogSynthPatch Pluck()
  // An IAudioSource implementation that wraps the Moog synthesizer and sequencer for use with the audio generator system.
  sealed class MoogSynthSource : IAudioSource
    ctor(MoogSynthPatch? patch = null)
    Sequencer Sequencer { get; }
    MoogSynth Synth { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
    void NextPattern()
    void SetPatch(MoogSynthPatch patch)
    void SetSequencerMode(SequencerMode mode)

namespace Ikon.Resonance.Synth.Oscillators
  // Defines the interface for audio oscillators that generate periodic waveforms.
  interface IOscillator
    double Phase { get; }
    abstract double Process(double frequency, double sampleRate)
    abstract void Reset()
    abstract void Sync()
  // Defines the available oscillator waveform types.
  enum OscillatorType
    Saw
    Square
    Triangle
    Pulse
    Sine
  // Provides PolyBLEP (Polynomial Band-Limited Step) anti-aliasing for oscillator discontinuities. Reduces aliasing artifacts in sawtooth and square waveforms.
  static class PolyBlep
    static double Compute(double t, double dt)
  // Generates a pulse wave with variable pulse width, using PolyBLEP anti-aliasing. Pulse width can be modulated for PWM (Pulse Width Modulation) effects.
  sealed class PulseOscillator : IOscillator
    ctor()
    double Phase { get; }
    double PulseWidth { get; set; }
    double Process(double frequency, double sampleRate, double pulseWidth)
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  // Generates a sawtooth waveform using PolyBLEP anti-aliasing to reduce aliasing artifacts.
  sealed class SawOscillator : IOscillator
    ctor()
    double Phase { get; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  // Generates a square wave with variable pulse width, using PolyBLEP anti-aliasing.
  sealed class SquareOscillator : IOscillator
    ctor()
    double Phase { get; }
    double PulseWidth { get; set; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  // Generates a sub-oscillator square wave one or two octaves below the main oscillator frequency. Adds bass depth and weight to the synthesizer sound.
  sealed class SubOscillator : IOscillator
    ctor()
    int OctaveDown { get; set; }
    double Phase { get; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  // Generates a triangle waveform. Naturally band-limited due to its smooth shape.
  sealed class TriangleOscillator : IOscillator
    ctor()
    double Phase { get; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()

namespace Ikon.Resonance.Synth.Sequencer
  // Configuration settings for the generative sequencer mode, controlling scale, probability, and velocity parameters.
  sealed class GenerativeSettings
    ctor()
    double Bpm { get; set; }
    double ChordProbability { get; set; }
    double MaxVelocity { get; set; }
    double MinVelocity { get; set; }
    double NoteProbability { get; set; }
    int OctaveRange { get; set; }
    double RestProbability { get; set; }
    int RootNote { get; set; }
    int[] Scale { get; set; }
  // Controls note playback timing for the synthesizer, supporting both pattern-based and generative sequencing modes.
  sealed class Sequencer
    ctor(MoogSynth synth)
    double Bpm { get; }
    GenerativeSettings GenerativeSettings { get; set; }
    SequencerMode Mode { get; set; }
    SequencerPattern Pattern { get; set; }
    void NextPattern()
    void Process(int sampleCount)
    void Reset()
    void SetSampleRate(double sampleRate)
  // Defines the operating mode of the sequencer.
  enum SequencerMode
    Pattern
    Generative
  // Represents a single note in a sequencer pattern with timing and expression data.
  struct SequencerNote
    // Represents a single note in a sequencer pattern with timing and expression data.
    ctor(int noteNumber, double velocity, double duration)
    double Duration { get; }
    int NoteNumber { get; }
    double Velocity { get; }
  // Defines a step-based sequencer pattern with preset patterns for various musical styles.
  sealed class SequencerPattern
    ctor()
    double Bpm { get; set; }
    string Name { get; set; }
    List<SequencerNote?> Steps { get; set; }
    int StepsPerBeat { get; set; }
    static SequencerPattern AcidBass()
    static SequencerPattern Arpeggio()
    static SequencerPattern FilterSweep()
    static SequencerPattern Pad()

namespace Ikon.Resonance.Synth.Songs
  // Represents a complete song with multiple tracks, tempo, and loop length configuration.
  sealed class Song
    ctor()
    double Bpm { get; set; }
    int LoopLengthBeats { get; set; }
    string Name { get; set; }
    List<SongTrack> Tracks { get; set; }
  // Provides a collection of pre-composed demo songs in various synth styles including C64-inspired covers and original compositions.
  static class SongLibrary
    static Song[] All()
    static Song BinaryHorizon()
    static Song CyberChase()
    static Song DigitalDreams()
    static Song LostPatrol()
    static Song NeonPatrol()
    static Song Parallax()
    static Song ShadowRunner()
  // Represents a single note in a song with timing, velocity, and duration information.
  struct SongNote
    // Represents a single note in a song with timing, velocity, and duration information.
    ctor(int noteNumber, double velocity, double duration, double startBeat)
    double Duration { get; }
    int NoteNumber { get; }
    double StartBeat { get; }
    double Velocity { get; }
  // Plays back multi-track songs using multiple Moog synthesizers, handling note timing, looping, and mixing.
  sealed class SongPlayer
    ctor()
    double BeatPosition { get; }
    string CurrentSongName { get; }
    bool IsPlaying { get; }
    Song Song { get; set; }
    void Play()
    void Process(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
    void Reset()
    void SetSampleRate(double sampleRate)
    void Stop()
  // An IAudioSource implementation that wraps the song player for use with the audio generator system. Supports song switching and playback control.
  sealed class SongPlayerSource : IAudioSource
    ctor(Song? song = null)
    string CurrentSongName { get; }
    SongPlayer Player { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
    void NextSong()
    void Play()
    void Reset()
    void SetSong(Song song)
    void Stop()
  // Represents a track within a song, containing a synthesizer patch and a sequence of notes.
  sealed class SongTrack
    ctor()
    string Name { get; set; }
    List<SongNote> Notes { get; set; }
    MoogSynthPatch Patch { get; set; }

namespace Ikon.Resonance.Synth.Voice
  // Represents a single synthesizer voice with dual oscillators, sub-oscillator, noise, filter, and envelopes. Handles note-on/off events and generates audio samples for one polyphonic voice.
  sealed class SynthVoice
    ctor()
    AdsrEnvelope AmpEnvelope { get; }
    double DriftAmount { get; set; }
    double FilterCutoff { get; set; }
    double FilterEnvAmount { get; set; }
    AdsrEnvelope FilterEnvelope { get; }
    double FilterKeyTrack { get; set; }
    double FilterResonance { get; set; }
    bool IsActive { get; }
    double NoiseLevel { get; set; }
    int NoteNumber { get; }
    double Osc1Level { get; set; }
    double Osc2Level { get; set; }
    double Osc2PulseWidth { get; set; }
    double SubLevel { get; set; }
    double Velocity { get; }
    void NoteOff()
    void NoteOn(int noteNumber, double velocity)
    double Process(double lfoFilterMod, double lfoPitchMod, double lfoPwmMod)
    void Reset()
    void SetSampleRate(double sampleRate)
  // Manages polyphonic voice allocation for the synthesizer. Implements voice stealing with LRU (Least Recently Used) policy when all voices are active.
  sealed class VoiceAllocator
    ctor(int voiceCount = 8)
    int VoiceCount { get; }
    IReadOnlyList<SynthVoice> Voices { get; }
    void AllNotesOff()
    void NoteOff(int noteNumber)
    SynthVoice? NoteOn(int noteNumber, double velocity)
    void Reset()
    void SetSampleRate(double sampleRate)
