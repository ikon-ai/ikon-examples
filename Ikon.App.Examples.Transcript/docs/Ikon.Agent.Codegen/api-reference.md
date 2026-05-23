# Ikon.AI.Emergence Public API

namespace Ikon.AI.Emergence
  sealed class AgentScope<T> : EmergeScope<T>
    ctor()
    int Index { get; }
    string Role { get; set; }
    int? Seed { get; set; }
  sealed class BestOfOptions<T> : EmergeScope<T>
    ctor()
    Func<T, ScoreBreakdown, string> BuildCriticFeedback { get; set; }
    Action<CandidateScope<T>> CandidateConfig { get; set; }
    int Count { get; set; }
    bool CriticMustImprove { get; set; }
    EmergeScope<T> CriticScope { get; }
    bool EnableCritic { get; set; }
    Func<T, EmergenceTrace, double> Score { get; set; }
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
    Action<AgentScope<T>> DebaterConfig { get; set; }
    int Debaters { get; set; }
    EmergeScope<T> JudgeScope { get; }
    void Debater(Action<AgentScope<T>> configure)
    void Judge(Action<EmergeScope<T>> configure)
  static class Emerge
    static Task<string> AskAsync(string command, CancellationToken ct = null)
    static Task<string> AskAsync(string command, LLMModel model, CancellationToken ct = null)
    static Task<T> AskAsync<T>(string command, CancellationToken ct = null)
    static Task<T> AskAsync<T>(string command, LLMModel model, CancellationToken ct = null)
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
  sealed class EmergeChat
    ctor()
  sealed class EmergeEventCallbacks<T>
    ctor()
    Action<T, EmergenceTrace> OnCompleted { get; set; }
    Action<string> OnStopped { get; set; }
    Action<string> OnText { get; set; }
    Action<FunctionCall> OnToolCallPlanned { get; set; }
    Action<FunctionCall, object> OnToolCallResult { get; set; }
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
    string Command { get; set; }
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
    IReadOnlyList<ModelRegion> Regions { get; set; }
    TimeSpan? RetryDelay { get; set; }
    int? SkipLastNMessages { get; set; }
    string StopReason { get; }
    string SystemPrompt { get; set; }
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
    object Result { get; }
    bool SkipReprocessing { get; init; }
  sealed class EmergeScope : EmergeScopeBase
    ctor()
  abstract class EmergeScopeBase
    string Command { get; set; }
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
    IReadOnlyList<ModelRegion> Regions { get; set; }
    TimeSpan? RetryDelay { get; set; }
    int? SkipLastNMessages { get; set; }
    string SystemPrompt { get; set; }
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
    int ContextWindowSize { get; init; }
    double ContextWindowUtilization { get; }
    TimeSpan? Duration { get; set; }
    string Error { get; set; }
    long InputTokens { get; set; }
    string Model { get; init; }
    long OutputTokens { get; set; }
    string Pattern { get; init; }
    string ResultType { get; init; }
    DateTime StartedAt { get; init; }
    string StopReason { get; set; }
    bool? Success { get; set; }
    Dictionary<string, string> Tags { get; init; }
  static class EmergenceMonitor
    static bool HasObservers { get; }
    static void AddObserver(IEmergenceObserver observer)
    static void ClearObservers()
    static void RemoveObserver(IEmergenceObserver observer)
    static void SetSoleObserver(IEmergenceObserver observer)
    static IDisposable WithTags(Dictionary<string, string> tags)
  class EmergenceMonitorState : IEmergenceObserver
    ctor()
    IReadOnlyList<EmergenceCallInfo> Calls { get; }
    void Clear()
    void OnCallCompleted(EmergenceCallInfo call)
    void OnCallStarted(EmergenceCallInfo call)
    void OnEvent(EmergenceCallInfo call, EmergenceObserverEvent evt)
    event Action Changed
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
    Exception Error { get; init; }
    string FinishReason { get; init; }
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
    Action<AgentScope<T>> SolverConfig { get; set; }
    int SolverCount { get; set; }
    void Merger(Action<EmergeScope<T>> configure)
    void Solver(Action<AgentScope<T>> configure)
  sealed class ExecutionPlan
    ctor()
    List<PlanStep> Steps { get; set; }
    string Summary { get; set; }
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
    IReadOnlyList<object> Chunks { get; set; }
    object Input { get; set; }
    EmergeScope<TChunk> MapScope { get; }
    int MaxParallel { get; set; }
    EmergeScope<TResult> ReduceScope { get; }
    Func<object, IEnumerable<object>> Split { get; set; }
    void Map(Action<EmergeScope<TChunk>> configure)
    void Reduce(Action<EmergeScope<TResult>> configure)
  sealed class McpClient : IDisposable
    ctor(string endpoint, Dictionary<string, string>? headers = null)
    IReadOnlyList<McpTool> Tools { get; }
    Task<string> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = null)
    Task<McpToolResult> CallToolRawAsync(string name, JsonElement arguments, CancellationToken ct = null, string? cursor = null)
    Task ConnectAsync(CancellationToken ct = null)
    void Dispose()
    Function[] ToFunctions()
  class McpTool : IEquatable<McpTool>
    ctor(string Name, string? Description, JsonElement? InputSchema)
    string Description { get; init; }
    JsonElement? InputSchema { get; init; }
    string Name { get; init; }
  class McpToolResult : IEquatable<McpToolResult>
    ctor(string Content, string? NextCursor)
    string Content { get; init; }
    string NextCursor { get; init; }
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
    string Reason { get; init; }
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
    Func<T, ScoreBreakdown, string> BuildCriticFeedback { get; set; }
    Action<CandidateScope<T>> CandidateConfig { get; set; }
    int Count { get; set; }
    bool CriticMustImprove { get; set; }
    EmergeScope<T> CriticScope { get; }
    bool EnableCritic { get; set; }
    int MaxParallel { get; set; }
    Func<T, EmergenceTrace, double> Score { get; set; }
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
    string ToolName { get; set; }
  sealed class Progress<T> : EmergeEvent<T>, IEquatable<Progress<T>>
    ctor(string Message)
    string Message { get; init; }
  sealed class RefineOptions<T> : EmergeScope<T>
    ctor()
    EmergeScope<T> InitialScope { get; }
    int MaxRefinements { get; set; }
    EmergeScope<T> RefinementScope { get; }
    Func<T, EmergenceTrace, Task<bool>> ShouldContinue { get; set; }
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
    Action<EmergeScopeBase> Configure { get; set; }
    string Description { get; set; }
    LLMModel? Model { get; set; }
    string Name { get; set; }
  sealed class RouterDecision
    ctor()
    string Reasoning { get; set; }
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
    ScoreMetric Weakest { get; init; }
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
    Action<CandidateScope<T>> SampleConfig { get; set; }
    int Samples { get; set; }
    Func<IReadOnlyList<T>, T> SelectMajority { get; set; }
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
    string Reason { get; init; }
  sealed class SwarmAgent<T>
    ctor()
    List<string> DependsOn { get; set; }
    string Id { get; set; }
    string Role { get; set; }
    EmergeScope<T> Scope { get; }
  sealed class SwarmOptions<T> : EmergeScope<T>
    ctor()
    List<SwarmAgent<T>> Agents { get; }
    EmergeScope<T> CoordinatorScope { get; }
    int MaxParallel { get; set; }
    int MaxRounds { get; set; }
    Func<IReadOnlyList<T>, T> Merge { get; set; }
    void AddAgent(string role, Action<EmergeScope<T>> configure)
    void Coordinator(Action<EmergeScope<T>> configure)
  sealed class TaskGraphOptions<T> : EmergeScope<T>
    ctor()
    bool EnableParallelReview { get; set; }
    int MaxParallel { get; set; }
    Func<string, Task> OnHumanFeedback { get; set; }
    Action<PlanRevision> OnPlanRevised { get; set; }
    Action<ReviewFeedback> OnReviewCompleted { get; set; }
    Action<TaskNode, object> OnTaskCompleted { get; set; }
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
    string Error { get; set; }
    string Id { get; set; }
    string Owner { get; set; }
    object Result { get; set; }
    string Status { get; set; }
  sealed class TestRefineFeedback
    ctor()
    bool Continue { get; set; }
    string Feedback { get; set; }
    ScoreBreakdown Score { get; set; }
  sealed class TestRefineOptions<T> : EmergeScope<T>
    ctor()
    Func<T, int, Task> Apply { get; set; }
    Func<T, int, Task<TestRefineFeedback>> Evaluate { get; set; }
    EmergeScope<T> InitialScope { get; }
    int MaxIterations { get; set; }
    EmergeScope<T> RefinementScope { get; }
    void Initial(Action<EmergeScope<T>> configure)
    void Refinement(Action<EmergeScope<T>> configure)
  sealed class ThoughtNode<T>
    ctor()
    List<ThoughtNode<T>> Children { get; }
    int Depth { get; set; }
    ThoughtNode<T> Parent { get; set; }
    string Reasoning { get; set; }
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
    Func<T, EmergenceTrace, double> Evaluate { get; set; }
    EmergeScope<T> EvaluatorScope { get; }
    int MaxDepth { get; set; }
    EmergeScope<T> ThoughtScope { get; }
    void Evaluator(Action<EmergeScope<T>> configure)
    void Thought(Action<EmergeScope<T>> configure)
  sealed class TreeSearchOptions<T> : EmergeScope<T>
    ctor()
    TreeIndex Index { get; set; }
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
  static class StructuredTagParser
    static string GetTagContent(string content, string tagName)
    static bool HasTag(string content, string tagName)
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
    TreeNode FindById(string id)
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
    TreeNode Parent { get; }
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
  sealed class GovernanceCall : IEquatable<GovernanceCall>
    ctor(string Operation, string Subject, IReadOnlyDictionary<string, object?> Args, IReadOnlyDictionary<string, object?> Ctx)
    IReadOnlyDictionary<string, object> Args { get; init; }
    IReadOnlyDictionary<string, object> Ctx { get; init; }
    string Operation { get; init; }
    string Subject { get; init; }
  sealed class GovernanceCallResult : IEquatable<GovernanceCallResult>
    ctor(bool Failed, string Outcome, string? ErrorMessage = null)
    string ErrorMessage { get; init; }
    bool Failed { get; init; }
    string Outcome { get; init; }
  static class GovernanceInvoker
    static Task<T> RunAsync<T>(GovernanceCall call, Func<Task<T>> invoke, CancellationToken ct = null)
  sealed class GovernanceOutcome : IEquatable<GovernanceOutcome>
    ctor(GovernanceAction Action, string DecisionId, string RuleId, string PolicyId, string Reason, string? Target = null)
    GovernanceAction Action { get; init; }
    string DecisionId { get; init; }
    string PolicyId { get; init; }
    string Reason { get; init; }
    string RuleId { get; init; }
    string Target { get; init; }
  static class GovernanceScope
    static IGovernanceHook Current { get; }
    static IDisposable Use(IGovernanceHook hook)
  interface IGovernanceHook
    abstract Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    abstract Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
  class IkonAIConnection : AsyncLocalInstance<IkonAIConnection>
    ctor()
    IkonClientConfig ConfigOverride { get; set; }
    Task ForceReconnectAsync(CancellationToken ct = null)
    Task<IkonClient> GetOrCreateClientAsync(CancellationToken ct = null)
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
    Embeddings
    FileConverter
    ImageGenerator
    LLM
    OCR
    Reranker
    SoundEffectGenerator
    SpeechGenerator
    SpeechRecognizer
    VideoEnhancer
    VideoGenerator
    WebScraper
    WebSearcher
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
    Fireworks
    Google
    Groq
    Hyperbolic
    Ikon
    Jina
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
    Task<T> GenerateObjectAsync<T>(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = null)
    Task<string> GenerateStringAsync(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = null)
    T GetState<T>(string key)
    void SetState(string key, object? value)
    void StopProcessing()
    event EventHandler<string> RenderedShader

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
    string EnvVarPrefix { get; set; }
    DatabaseConnection.SpaceSecret SpaceSecret { get; set; }
  class DatabaseInfoExtractor.Config
    ctor()
    List<string> ColumnExcludeRegex { get; set; }
    Dictionary<string, string> ColumnExtraInfo { get; set; }
    bool IncludeEmptyColumns { get; set; }
    int JsonSampleLengthLimit { get; set; }
    int JsonSampleRowLimit { get; set; }
    int NonTextSampleRowLimit { get; set; }
    List<string> Schemas { get; set; }
    List<string> TableExcludeRegex { get; set; }
    Dictionary<string, string> TableExtraInfo { get; set; }
    List<string> TableIncludeRegex { get; set; }
    int TextSampleLengthLimit { get; set; }
    int TextSampleRowLimit { get; set; }
  class DatabaseColumnInfo
    ctor()
    string ColumnName { get; set; }
    string DataType { get; set; }
    string Description { get; set; }
    string ExtraInfo { get; set; }
    string ForeignKeyColumnName { get; set; }
    string ForeignKeyTableName { get; set; }
    bool? IsForeignKey { get; set; }
    bool? IsPrimaryKey { get; set; }
    List<string> Values { get; set; }
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
    List<string> ExampleQuestions { get; set; }
    string SqlCteCommand { get; set; }
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
    string Description { get; set; }
    string ExtraInfo { get; set; }
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
    GeminiEmbedding1
    GoogleTextEmbedding5
    GoogleTextMultilingualEmbedding2
    JinaEmbeddings3
    JinaEmbeddings4
    Voyage35
    Voyage35Lite
    Voyage4
    Voyage4Lite
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
    static float[] CalculateAverageEmbedding(IList<float[]> embeddings)
    static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateDotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateEuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static List<List<VectorMath.Neighbor>> CalculateKNearestNeighbors(IList<float[]> embeddings, int k)
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
    byte[] Data { get; set; }
    string FileName { get; set; }
    TimeSpan Timeout { get; set; }
    string Url { get; set; }
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
    static Task<ImageGeneratorResult> GenerateAsync(string prompt, ImageGeneratorModel model = Gemini25FlashImage, CancellationToken cancellationToken = null)
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
    Flux1KreaDev
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
    object[] Parameters { get; }
    string ParametersJson { get; }
    string ReasoningContent { get; }
    string ThoughtSignature { get; }
  class FunctionResult
    ctor(object? result = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null)
    string ModelMessagePrefix { get; set; }
    string ModelMessageSuffix { get; set; }
    object Result { get; set; }
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
  static class JsonSchemaGenerator
    static ExpandoObject GenerateJsonSchemaExpandoObject<T>(SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    static JsonNode GenerateSchemaNode(Type type, string? description = null, SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    static string GenerateSchemaString<T>(SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    static string GenerateSchemaString(Type type, SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
  struct KernelContext : IEquatable<KernelContext>
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    static KernelContext Default { get; }
    bool DisableFunctionCalling { get; init; }
    bool DiscardTextOutputWithFunctionCalls { get; init; }
    static KernelContext Empty { get; }
    ImmutableDictionary<string, Function> Functions { get; init; }
    string GbnfGrammar { get; init; }
    ImmutableList<Instruction> Instructions { get; init; }
    object JsonSchema { get; init; }
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
    string UserName { get; }
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
  enum SchemaDialect
    JsonSchema202012
    OpenApi30
  struct StreamingResult
    ctor(object value, string sourceName, string? valueTypeName = null)
    string SourceName { get; }
    object Value { get; }
    string ValueTypeName { get; }
  class Tag
    ctor(string name, string content, Dictionary<string, string>? attributes = null)
    Dictionary<string, string> Attributes { get; }
    string Content { get; }
    string Name { get; }
  struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  class TokenUsage
    ctor(int inputTokens, int cachedInputTokens, int cacheCreationInputTokens, int outputTokens)
    int CacheCreationInputTokens { get; }
    int CachedInputTokens { get; }
    int InputTokens { get; }
    int OutputTokens { get; }
  class ToolPlan
    ctor(string text)
    string Text { get; }
  struct VideoAssetPart : IMessagePart
    ctor(AssetUri uri, string? mimeType = null)
    string MimeType { get; }
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
    Gemini25Flash
    Gemini25FlashLite
    Gemini25Pro
    Gemini3Flash
    Gemini31Pro
    Gemini31FlashLite
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
    static int ContextWindowSize(LLMModel model)
    static string DisplayName(LLMModel model)

namespace Ikon.AI.Legacy
  class Mind : IAsyncDisposable
    ctor()
    Context CurrentUserClientContext { get; }
    string CurrentUserLocale { get; }
    string DefaultModelName { get; set; }
    string DefaultSecondaryModelName { get; set; }
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
    Func<Dictionary<string, object>, Task> StateUpdate
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

namespace Ikon.AI.OCR
  enum DocumentType
    General
  interface IOCR : IDisposable
    abstract Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = null)
  sealed class OCR : IDisposable, IOCR
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    static OCRCapabilities GetCapabilities(OCRModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(OCRModel model)
  sealed class OCRBoundingBox
    ctor()
    int PageNumber { get; init; }
    List<float> Polygon { get; init; }
  sealed class OCRCapabilities
    ctor()
  sealed class OCRConfig
    ctor()
    AssetUri? AssetUri { get; set; }
    byte[] Data { get; set; }
    DocumentType DocumentType { get; set; }
    bool IncludeWords { get; set; }
    string Pages { get; set; }
    TimeSpan Timeout { get; set; }
    string Url { get; set; }
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
    Task<Content> GetContent(ContentLink link)
    Retriever.ContentMetadata GetContentMetadata(string metadataId)
    Task<string> GetContents(string query, Retriever.GetContentsOptions options)
    Task<string> GetContents2(string query, Retriever.GetContentsOptions2 options)
    ContentLink Ignore(ContentLink link, string detail)
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
  class FunctionDetailsDictionaryConverter<T> : JsonConverter where T : new(), IFunctionDetails
    ctor()
    override bool CanConvert(Type objectType)
    override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
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
    abstract object GetValue(string key)
    abstract string GetValueAsString(string key)
    abstract void Register<T>()
    abstract void SetValue(string key, object? value)
  interface IScriptEngine
    abstract IScriptContext CreateContext()
    abstract bool TryParse(string template, out IScriptTemplate? parsedTemplate, out string? errorMessage)
  interface IScriptTemplate
    abstract Task<string> RenderAsync(IScriptContext context)
  class ShaderCache.ImplicitShader
    ctor(AssetUri? shaderUri, string callerFilePath, ShaderCache outer)
    IAsyncEnumerable<StreamingResult> GenerateAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<Shader> GetShaderAsync()
  class Intent
    ctor()
    History History { get; set; }
    string Id { get; set; }
    Dictionary<string, object> Input { get; set; }
    Misc Misc { get; set; }
    Model Model { get; set; }
    List<Pass> Passes { get; set; }
    ScriptableValue<bool> Select { get; set; }
  class JTokenConverter
    ctor()
    static object ConvertJTokenToObject(JToken? token)
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
    ExpandoObject JsonSchema { get; set; }
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
    ScriptableStringValue Call { get; set; }
    ScriptableValue<bool> CallOnlyOnce { get; set; }
    ScriptableStringValue Description { get; set; }
    ScriptableValue<bool> InlineCall { get; set; }
    Dictionary<string, ParameterDetails> Parameters { get; set; }
    ScriptableStringValue Process { get; set; }
    ScriptableValue<bool> Select { get; set; }
    ScriptableStringValue Use { get; set; }
  class Output
    ctor()
    ScriptableStringValue AfterPass { get; set; }
    ScriptableStringValue AfterShader { get; set; }
    ScriptableStringValue BeforePass { get; set; }
    ScriptableStringValue BeforeShader { get; set; }
  class ParameterDetails
    ctor()
    object DefaultValue { get; set; }
    ScriptableStringValue Description { get; set; }
    ScriptableValue<bool> HasDefaultValue { get; set; }
    ScriptableStringValue Type { get; set; }
    ScriptableStringValue Use { get; set; }
  class Pass
    ctor()
    Actions Actions { get; set; }
    ScriptableStringValue Command { get; set; }
    ScriptableStringValue Context { get; set; }
    History History { get; set; }
    string Id { get; set; }
    Dictionary<string, object> Input { get; set; }
    Misc Misc { get; set; }
    Model Model { get; set; }
    Dictionary<string, ModelFunctionDetails> ModelFunctions { get; set; }
    Output Output { get; set; }
    ScriptableValue<bool> Select { get; set; }
    Dictionary<string, TemplateFunctionDetails> TemplateFunctions { get; set; }
  class ScriptableStringDictionaryConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class ScriptableStringValue
    ctor(string? value = "")
    bool IsScript { get; }
    string Value { get; }
    Task<string> GetValueAsync(Func<string, Task<string>> renderer)
  class ScriptableStringValueConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class ScriptableValueConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class ScriptableValue<T> where T : struct
    ctor(T value)
    ctor(string script)
    string Script { get; }
    T? Value { get; }
    Task<T> GetValueAsync(Func<string, Task<string>> renderer)
  class Shader
    ctor(string shaderConfigAsJsonString, bool enableRenderedShaderLogging = false)
    Dictionary<string, object> Input { get; }
    static string Escape(string? text)
    IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, ExpandoObject? implicitJsonSchema = null, string? implicitJsonExample = null, IdMapper? idMapper = null, string modelUserName = "", string modelMessagePrefix = "", string modelMessageSuffix = "", int iteration = 0, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null)
    Task<T> GenerateObjectAsync<T>(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, JsonSerializerOptions? jsonSerializerOptions = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null)
    Task<string> GenerateStringAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null)
    void SetActiveState<T>(string key, T value)
    static string Unescape(string? text)
    event EventHandler<string> RenderedShader
  class ShaderCache : AsyncLocalInstance<ShaderCache>
    ctor()
    string DefaultSpaceId { get; set; }
    ShaderCache.ImplicitShader GetImplicitShader(string callerFilePath = "")
  class ShaderConfig
    ctor()
    static object Default { get; }
    History History { get; set; }
    Dictionary<string, object> Input { get; set; }
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
    Dictionary<string, object> Config { get; set; }
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
    ctor(string modelName, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(SoundEffectGeneratorModel model, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(SoundEffectGeneratorModel model, IReadOnlyList<ModelRegion>? regions, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
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
    ctor(string modelName, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(SpeechGeneratorModel model, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
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
    string MimeType { get; set; }
    int? StartFrame { get; set; }
    int? TargetFps { get; set; }
    TimeSpan Timeout { get; set; }
    byte[] VideoData { get; set; }
    string VideoUrl { get; set; }
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
    byte[] Data { get; set; }
    string MimeType { get; set; }
    string Url { get; set; }
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
    string NegativePrompt { get; set; }
    string Prompt { get; set; }
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
  interface IWebScraper : IDisposable, IWebScraperInfo
    abstract Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = null)
    abstract Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = null)
    abstract Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = null)
  interface IWebScraperInfo
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
    WebScraperModel WebScraperModel { get; set; }
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
    WebScraperModel WebScraperModel { get; set; }
    static SinglePageScrapeConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebScraper : IDisposable, IWebScraper, IWebScraperInfo
    ctor(string modelName, bool useLocalCache = false)
    ctor(WebScraperModel model, bool useLocalCache = false)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions, bool useLocalCache = false)
    ctor(WebScraperModel model, IReadOnlyList<ModelRegion>? regions, bool useLocalCache = false)
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
    void Dispose()
    static WebScraperCapabilities GetCapabilities(WebScraperModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebScraperModel model)
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = null)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = null)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = null)
  sealed class WebScraperCapabilities : IWebScraperInfo
    ctor()
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
  // Per-app theme configuration. Composes the platform's Ikon CSS baseline with per-token CSS-variable overrides addressed by name. One uniform syntax: an indexer keyed by CSS variable name (without the leading --) or by Tailwind utility token. The renderer dispatches by key shape: Tailwind palette step (amber-400) → --color-amber-400rounded-{rung} → --radius-{rung}shadow-{rung} → --shadow-{rung}font-{role} → --font-{role}ease-{kind} → --ease-{kind}Anything else → --{key} (free CSS variable) Values are Crosswind / Tailwind class names (resolved via ) or raw CSS values (hex, rem, family stacks, gradients) — the resolver passes raw values through. Example: private UI UI { get; } = new(app, new IkonTheme { // Brand commitment — set the semantic vars that components consume. ["primary"] = "amber-400", ["bg-brand-solid"] = "amber-400", ["bg-brand-solid-hover"] = "amber-400", ["text-brand"] = "amber-400", ["border-brand"] = "amber-400", ["primary-foreground"] = "#0A0A0A", // pick contrast yourself // Background + foreground. ["background"] = "zinc-950", ["text-primary"] = "amber-50", ["text-foreground"] = "amber-50", // Surfaces. ["card"] = "zinc-900", ["popover"] = "zinc-900", // Type + shape. ["font-heading"] = "Crimson Pro", ["font-body"] = "Inter", ["radius-base"] = "rounded-lg", // Motion. ["motion-duration-base"] = "200ms", ["ease-default"] = "ease-out", // Per-token Tailwind palette / radius / shadow overrides. ["amber-400"] = "#F5A524", ["rounded-lg"] = "1.25rem", ["shadow-lg"] = "0 8px 16px rgba(0,0,0,.18)", // Bespoke decorative tokens. ["hero-glow"] = "radial-gradient(circle, #F5A52488, transparent 70%)", DarkMode = new IkonTheme { ["background"] = "zinc-50", ["text-primary"] = "zinc-950", }, }); The indexer is the only configurable surface — there are no magic property fan-outs and no auto-derived contrast text. What you write IS what lands in the override block.
  sealed class IkonTheme : ITheme
    ctor()
    // Paired dark-mode theme. Pass another ; its overrides are emitted under [data-theme="dark"], .dark, and prefers-color-scheme: dark.
    IkonTheme DarkMode { get; init; }
    string Item { get; set; }
  // Accumulates profiling samples over multiple render passes, providing aggregate statistics (avg, min, max, p95, p99).
  sealed class ProfileHistory
    // Creates a new history buffer that retains the last render sessions.
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
  // Disposable timing scope that records elapsed time into the current when disposed.
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
    static ProfileSession Current { get; }
    // Historical profiling data, or null if history is not enabled.
    static ProfileHistory History { get; }
    // Whether history recording is currently paused.
    static bool IsHistoryPaused { get; }
    // Disables profiling history collection and discards accumulated data.
    static void DisableHistory()
    // Enables profiling history collection, keeping up to render sessions.
    static void EnableHistory(int maxSamples = 1000)
    // Starts a named timing measurement within the current profiling session. Dispose the returned scope to record the elapsed time.
    static ProfileScope Measure(string name)
    // Pauses history recording. New render sessions are not recorded until is called.
    static void PauseHistory()
    // Clears all accumulated profiling history samples.
    static void ResetHistory()
    // Resumes history recording after a pause.
    static void ResumeHistory()
  // Main entry point for the Ikon Parallax reactive UI system. Manages client connections, render cycles, style distribution, and action handling for server-driven UI.
  class UI
    // Creates a new UI instance bound to the given app and theme.
    ctor(IAppBase app, ITheme theme)
    // When true, each render cycle logs timing breakdowns. See for history.
    bool EnableProfiling { get; set; }
    // When true, caches subtrees with unchanged reactive dependencies to skip redundant re-renders.
    bool EnableSubtreeCaching { get; set; }
    // Adds a global CSS block that is sent to all connected clients. Idempotent: identical CSS returns the same style ID.
    string AddGlobalCss(string css)
    // Defines the root UI view tree. Call this in a reactive context to re-render when dependencies change.
    void Root(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
  // Represents a UI view scope for building the component tree. Extension methods on this type provide the component API (e.g. Text, Button, Input).
  class UIView
    // The default icon library name used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }
    // Adds a child node with the given type and props to the current view.
    void AddNode(string type, Dictionary<string, object?> props, List<UIViewNode>? children = null, string? key = null, string[]? style = null, string? styleId = null, string file = "", int line = 0)
    string CreateAction<T>(Func<ActionArgs<T>, Task>? callback)
    // Registers binary data as a payload and returns a reference string for use as an image src.
    string RegisterPayload(byte[] data, string mimeType)
  // Represents a single node in the UI view tree, with identity, props, children, and style information.
  sealed class UIViewNode
    // Creates a new view node with the given type, props, and optional children.
    ctor(string type, Guid viewId, Dictionary<string, object?> props, List<UIViewNode>? children = null, string? key = null, IReadOnlyList<string>? styleIds = null, ulong parentPathHash = 0, IReadOnlyCollection<IReadOnlyList<string>>? styleIdProps = null, string file = "", int line = 0)
    // Ordered child nodes.
    List<UIViewNode> Children { get; }
    // Lazily computed content signature used for subtree caching and diffing.
    string ContentFingerprint { get; }
    // True when came from an explicit key argument, not from a prop such as value or text.
    bool HasExplicitKey { get; }
    // Stable unique identifier for this node.
    string Id { get; }
    // Precomputed hash of for fast lookups.
    int IdHash { get; }
    // When true, nodes include source file and line markers for debugging.
    static bool IncludeSourceMarkers { get; set; }
    // Component properties passed to the frontend renderer.
    Dictionary<string, object> Props { get; }
    // Source file and line marker for debugging, included only when is true.
    string SourceMarker { get; }
    // Hint string used by the stable ID generator to produce deterministic IDs.
    string StableHint { get; }
    // Resolved Crosswind style class identifiers.
    IReadOnlyList<string> StyleIds { get; }
    // The component type name (e.g. "div", "button").
    string Type { get; }

namespace Ikon.Parallax.Components.Charts
  // Configuration for a chart axis including legend text, tick appearance, and label truncation.
  class AxisConfig
    ctor()
    // Format string for tick labels. For time scales, use d3-time-format tokens (e.g. "%H:%M", "%m/%d %H:%M").
    string Format { get; set; }
    string Legend { get; set; }
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
    string DomainColor { get; init; }
    ChartTextStyle Legend { get; init; }
    string TickColor { get; init; }
    ChartTextStyle TickLabel { get; init; }
  // Event arguments for chart click interactions.
  class ChartClickArgs
    ctor()
    string Id { get; set; }
    string IndexValue { get; set; }
    string SerieId { get; set; }
    object Value { get; set; }
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
    string LineColor { get; init; }
    string LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Extension methods for rendering interactive chart components (bar, line, pie).
  static class ChartExtensions
    // Renders an interactive bar chart with configurable grouping, layout, axes, and theming.
    static void BarChart(UIView view, string[]? style = null, IEnumerable<Dictionary<string, object>>? data = null, IEnumerable<string>? keys = null, string? indexBy = null, BarGroupMode? groupMode = null, BarLayout? layout = null, ScaleType? valueScale = null, ScaleType? indexScale = null, bool? reverse = null, double? minValue = null, double? maxValue = null, double? padding = null, double? innerPadding = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enableLabel = null, int? labelSkipWidth = null, int? labelSkipHeight = null, string? labelTextColor = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderRadius = null, double? borderWidth = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null, string file = "", int line = 0)
    // Renders an interactive line chart with configurable curves, points, areas, and crosshairs.
    static void LineChart(UIView view, string[]? style = null, IEnumerable<LineChartSeries>? data = null, ScaleType? xScaleType = null, ScaleType? yScaleType = null, double? xScaleMin = null, double? xScaleMax = null, double? yScaleMin = null, double? yScaleMax = null, bool? yScaleStacked = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enablePoints = null, int? pointSize = null, string? pointColor = null, string? pointBorderColor = null, int? pointBorderWidth = null, bool? enableArea = null, double? areaOpacity = null, double? areaBaselineValue = null, bool? enableCrosshair = null, CrosshairType? crosshairType = null, LineCurve? curve = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, double? lineWidth = null, bool? isInteractive = null, bool? useMesh = null, string? gradientFromColor = null, string? gradientToColor = null, IEnumerable<double>? gridXValues = null, IEnumerable<double>? gridYValues = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null, string file = "", int line = 0)
    // Renders an interactive pie/donut chart with configurable arc labels, link labels, and legends.
    static void PieChart(UIView view, string[]? style = null, IEnumerable<PieChartDatum>? data = null, double? innerRadius = null, double? padAngle = null, double? cornerRadius = null, double? startAngle = null, double? endAngle = null, bool? sortByValue = null, ChartMargin? margin = null, bool? enableArcLabels = null, string? arcLabelsTextColor = null, double? arcLabelsSkipAngle = null, bool? enableArcLinkLabels = null, string? arcLinkLabelsTextColor = null, double? arcLinkLabelsSkipAngle = null, double? arcLinkLabelsThickness = null, string? arcLinkLabelsColor = null, double? activeOuterRadiusOffset = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderWidth = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null, string file = "", int line = 0)
  // Styling for chart grid lines.
  class ChartGridStyle : IEquatable<ChartGridStyle>
    ctor()
    string LineColor { get; init; }
    string LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Styling for chart data labels.
  class ChartLabelsStyle : IEquatable<ChartLabelsStyle>
    ctor()
    ChartTextStyle Text { get; init; }
  // Styling for chart legend text and title.
  class ChartLegendStyle : IEquatable<ChartLegendStyle>
    ctor()
    ChartTextStyle Text { get; init; }
    ChartTextStyle Title { get; init; }
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
    string Color { get; init; }
    string FontFamily { get; init; }
    int? FontSize { get; init; }
  // Complete theme configuration for chart components, combining all styling aspects.
  class ChartTheme : IEquatable<ChartTheme>
    ctor()
    ChartAxisStyle Axis { get; init; }
    ChartColorScheme? ColorScheme { get; init; }
    string[] Colors { get; init; }
    ChartCrosshairStyle Crosshair { get; init; }
    ChartGridStyle Grid { get; init; }
    ChartLabelsStyle Labels { get; init; }
    ChartLegendStyle Legends { get; init; }
    ChartTextStyle Text { get; init; }
    ChartTooltipStyle Tooltip { get; init; }
  // Built-in chart theme presets for light and dark backgrounds.
  static class ChartThemes
    // Chart theme optimized for dark backgrounds with muted but saturated series colors.
    static ChartTheme DefaultDark { get; }
    // Chart theme optimized for light backgrounds with soft, pastel-like series colors.
    static ChartTheme DefaultLight { get; }
  // Styling for chart tooltips.
  class ChartTooltipStyle : IEquatable<ChartTooltipStyle>
    ctor()
    string BackgroundColor { get; init; }
    string BorderColor { get; init; }
    int? BorderRadius { get; init; }
    ChartTextStyle Text { get; init; }
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
    string Anchor { get; set; }
    string Direction { get; set; }
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
    string Color { get; set; }
    IEnumerable<LineChartPoint> Data { get; set; }
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
    string Color { get; set; }
    string Id { get; set; }
    string Label { get; set; }
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
    string ActionId { get; init; }
    // Action buttons for "actions" type cells.
    CellAction[] Actions { get; init; }
    // When true, the cell's interactive element is disabled.
    bool? Disabled { get; init; }
    // Button label for action cells.
    string Label { get; init; }
    // Crosswind style classes for the cell.
    string[] Style { get; init; }
    // Cell type: "text", "badge", "action", "actions", or "checkbox".
    string Type { get; init; }
    // Display value or checkbox state ("true"/"false").
    string Value { get; init; }
    // Visual variant for badge cells.
    string Variant { get; init; }
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
    string Icon { get; init; }
    string Label { get; init; }
    string[] Style { get; init; }
  // Defines a column in a data table including header text, width, and alignment.
  class DataTableColumn : IEquatable<DataTableColumn>
    // Defines a column in a data table including header text, width, and alignment.
    ctor(string Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left, string? MinWidth = null, bool Wrap = false)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string Header { get; init; }
    string MinWidth { get; init; }
    string Width { get; init; }
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
    Dictionary<string, JsonElement> Properties { get; init; }
    // Type-safe accessor for the event's custom properties.
    RiveEventProperties Props { get; }
    // Target identifier for the event.
    string Target { get; init; }
    // The Rive event type identifier.
    int? Type { get; init; }
    // URL associated with the event, if any.
    string Url { get; init; }
  // Helper class for accessing Rive event properties with type-safe methods.
  sealed class RiveEventProperties
    // Helper class for accessing Rive event properties with type-safe methods.
    ctor(Dictionary<string, JsonElement>? properties)
    // Gets a boolean property value, or if not found.
    bool GetBool(string key, bool defaultValue = false)
    // Gets a double property value, or if not found.
    double GetDouble(string key, double defaultValue = 0)
    // Gets an integer property value, or if not found.
    int GetInt(string key, int defaultValue = 0)
    // Gets a string property value, or if not found.
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
  // JSON converter that deserializes into the correct derived type based on the ActionType field.
  class ActionEventConverter : JsonConverter<ActionEvent>
    ctor()
    override ActionEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
  // One row in the charges list.
  sealed class BillingChargeView : IEquatable<BillingChargeView>
    // One row in the charges list.
    ctor(string Id, string AmountLabel, string Status, DateTimeOffset Created, bool Paid, bool Refunded, string? PaymentIntentId, string? ReceiptUrl, string? Description = null)
    string AmountLabel { get; init; }
    DateTimeOffset Created { get; init; }
    string Description { get; init; }
    string Id { get; init; }
    bool Paid { get; init; }
    string PaymentIntentId { get; init; }
    string ReceiptUrl { get; init; }
    bool Refunded { get; init; }
    string Status { get; init; }
  // Composed Parallax components for billing UIs — pricing tables, checkout actions, customer-portal entry points, payment-method and invoice lists, and subscription status. Pair with for end-to-end flows. All components are pure compositions of existing primitives (Box / Text / Button / Icon / Column / Row), so they participate in the standard theming, motion, and validation rules just like the rest of the Parallax surface.
  static class BillingExtensions
    // Dual-mode billing-management entry point. BYOK mode (default): renders a button that invokes , expected to call BillingService.CreatePortalAsync, and redirects to the returned Stripe-hosted Customer Portal URL.Connect mode: pass a non-empty and the component renders an embedded instead — Stripe doesn't expose a hosted Customer Portal for connected accounts, so the embedded management surface is the only equivalent.
    static void BillingPortalButton(UIView view, Func<Task<string?>>? onOpenPortal = null, string? connectAccountSessionClientSecret = null, string? publishableKey = null, string? text = null, string[]? style = null, bool? disabled = null, string? icon = "settings", string? key = null, string file = "", int line = 0)
    // Vertical list of charge / receipt rows. Each row shows formatted amount, status, optional refund button (when is supplied and the charge is paid + non-refunded), and a "Receipt" link when present.
    static void ChargeList(UIView view, IReadOnlyList<BillingChargeView> charges, Func<string, Task>? onRefund = null, string[]? style = null, string? emptyText = null, string? key = null, string file = "", int line = 0)
    // Button that initiates a redirect-to-Stripe checkout. The handler is expected to call BillingService.CreateCheckoutAsync(...) and return the session url; the component then redirects the current client via ClientFunctions.SetUrlAsync. Returning null from the handler disables the redirect (e.g. for guest validation).
    static void CheckoutButton(UIView view, Func<Task<string?>> onCheckout, string? text = null, string[]? style = null, bool? disabled = null, string? icon = "credit-card", string? key = null, string file = "", int line = 0)
    // Mount Stripe Connect Embedded "Account Management" inline. Lets the connected-account holder update bank account, business details and KYC info after onboarding. Server enables the account_management component on the account session.
    static void ConnectAccountManagementFrame(UIView view, string? accountSessionClientSecret, string? publishableKey = null, string[]? style = null, string? minHeightClass = null, string? key = null, string file = "", int line = 0)
    // Mount Stripe Connect Embedded "Balances" inline. Shows available and pending balance per currency. Server enables the balances component on the account session.
    static void ConnectBalancesFrame(UIView view, string? accountSessionClientSecret, string? publishableKey = null, string[]? style = null, string? minHeightClass = null, string? key = null, string file = "", int line = 0)
    // Mount Stripe Connect Embedded "Documents" inline. Shows tax-form documents for the connected account.
    static void ConnectDocumentsFrame(UIView view, string? accountSessionClientSecret, string? publishableKey = null, string[]? style = null, string? minHeightClass = null, string? key = null, string file = "", int line = 0)
    // Mount Stripe Connect Embedded "Notification Banner" inline. Surfaces Stripe-issued action items (e.g. "verify your ID"). Server enables the notification_banner component on the account session. Renders compactly (no min-height by default).
    static void ConnectNotificationBanner(UIView view, string? accountSessionClientSecret, string? publishableKey = null, string[]? style = null, string? key = null, string file = "", int line = 0)
    // Mount Stripe Connect Embedded "Account Onboarding" inline. Server supplies an account_sessions client secret with the account_onboarding component enabled. Frontend resolver loads Stripe Connect.js and mounts <ConnectAccountOnboarding> inside the host node.
    static void ConnectOnboardingFrame(UIView view, string? accountSessionClientSecret, string? publishableKey = null, string[]? style = null, string? minHeightClass = null, string? key = null, string file = "", int line = 0)
    // Mount Stripe Connect Embedded "Payments" inline. Lists charges with refund / dispute / capture controls. Server enables the payments component on the account session.
    static void ConnectPaymentsFrame(UIView view, string? accountSessionClientSecret, string? publishableKey = null, string[]? style = null, string? minHeightClass = null, string? key = null, string file = "", int line = 0)
    // Mount Stripe Connect Embedded "Payouts" inline. Lists payouts and (when enabled) lets the holder edit payout schedule. Server enables the payouts component on the account session.
    static void ConnectPayoutsFrame(UIView view, string? accountSessionClientSecret, string? publishableKey = null, string[]? style = null, string? minHeightClass = null, string? key = null, string file = "", int line = 0)
    // Mount point for Stripe's Embedded Checkout. Renders a host element with data-stripe-client-secret that the frontend's EmbeddedCheckoutProvider mounts into. Pass the obtained from BillingService.CreateEmbeddedCheckoutAsync. When is null/empty (e.g. the user hasn't picked a plan yet) the component renders a placeholder so callers can drop it into a layout unconditionally.
    static void EmbeddedCheckoutFrame(UIView view, string? clientSecret, string? publishableKey = null, string? connectedAccountId = null, string[]? style = null, string? minHeightClass = null, string? key = null, string file = "", int line = 0)
    // Vertical list of past invoices. Each row links to the hosted invoice url when present, and to the PDF when present.
    static void InvoiceList(UIView view, IReadOnlyList<BillingInvoiceView> invoices, string[]? style = null, string? emptyText = null, string? key = null, string file = "", int line = 0)
    // Mount Stripe Elements bound to a PaymentIntent for confirming a one-shot payment (e.g., capturing a saved card, completing a manual capture flow). Frontend resolver mounts <PaymentElement /> inside <Elements> and confirms via stripe.confirmPayment. Symmetric to — that one saves a card without charging; this one charges (possibly using a saved card on file).
    static void PaymentIntentFrame(UIView view, string? clientSecret, string? publishableKey = null, string? returnUrl = null, string? connectedAccountId = null, string[]? style = null, string? minHeightClass = null, string? key = null, string file = "", int line = 0)
    // Vertical list of saved payment methods. Each row shows brand, last four, and expiry. Optional renders a remove action.
    static void PaymentMethodList(UIView view, IReadOnlyList<BillingPaymentMethodView> methods, Func<string, Task>? onDetach = null, Func<Task>? onAddCard = null, string? setupIntentClientSecret = null, string? publishableKey = null, string? connectedAccountId = null, string[]? style = null, string? emptyText = null, string? key = null, string file = "", int line = 0)
    // Single pricing plan card with name, price, optional badge, feature bullet list and CTA. Use directly when laying plans out by hand, or via for the common grid case.
    static void PlanCard(UIView view, BillingPlanView plan, Func<string, Task>? onSelect = null, string[]? style = null, string? key = null, string file = "", int line = 0)
    // Render a grid of pricing plan cards. Each card invokes with the plan's id when the CTA is pressed. The card whose is true gets the brand-emphasis treatment (one card max).
    static void PricingTable(UIView view, IReadOnlyList<BillingPlanView> plans, Func<string, Task>? onSelect = null, string[]? style = null, int? columns = null, string? key = null, string file = "", int line = 0)
    // Mount Stripe Elements bound to a SetupIntent for saving a card without an immediate charge. Frontend resolver mounts <PaymentElement /> inside <Elements> and confirms via stripe.confirmSetup. The SetupIntent's payment_method is auto-attached to the customer it was created for; refresh PaymentMethodList afterwards.
    static void SetupIntentFrame(UIView view, string? clientSecret, string? publishableKey = null, string? returnUrl = null, string? connectedAccountId = null, string[]? style = null, string? minHeightClass = null, string? key = null, string file = "", int line = 0)
    // Renders a vertical list of cards, one per subscription. Pass the same callback set you'd pass to a single ; each callback receives the subscription id of the row that fired it.
    static void SubscriptionList(UIView view, IReadOnlyList<BillingSubscription> subscriptions, Func<BillingSubscription, BillingSubscriptionView>? projector = null, Func<string, Task>? onResume = null, Func<string, Task>? onCancel = null, Func<string, Task>? onCancelImmediate = null, Func<string, Task>? onPause = null, Func<string, Task>? onResumeFromPause = null, Action<UIView, BillingSubscription>? footer = null, string[]? style = null, string? emptyText = null, string? key = null, string file = "", int line = 0)
    // Compact subscription status card showing plan name, status pill and renewal/expiry date. Slot a BillingPortalButton in the footer to give the user a manage entry point.
    static void SubscriptionStatus(UIView view, BillingSubscriptionView subscription, string[]? style = null, Action<UIView>? footer = null, Func<Task>? onResume = null, Func<Task>? onCancel = null, Func<Task>? onCancelImmediate = null, Func<Task>? onPause = null, Func<Task>? onResumeFromPause = null, string? key = null, string file = "", int line = 0)
    // Grid of one-tap tip preset amounts. Each preset renders as a rounded button showing the currency-formatted amount; clicking invokes with the chosen minor-unit amount. App handler typically passes the amount to BillingService.CreateTipCheckoutAsync and redirects.
    static void TipPresetGrid(UIView view, IReadOnlyList<long> presetsMinor, string currencySymbol, Func<long, Task> onTip, string[]? style = null, string? key = null, string file = "", int line = 0)
    // Display-only preview card for the next-billing-cycle invoice. Pair with BillingService.PreviewUpcomingInvoiceAsync: call before committing a plan change so the user sees "next bill = €X · €Y proration".
    static void UpcomingInvoicePreview(UIView view, BillingUpcomingInvoice preview, string[]? style = null, string? key = null, string file = "", int line = 0)
  // One row in the invoice / receipt list.
  sealed class BillingInvoiceView : IEquatable<BillingInvoiceView>
    // One row in the invoice / receipt list.
    ctor(string Id, DateTimeOffset Date, string AmountLabel, string Status, string? HostedUrl = null, string? PdfUrl = null)
    string AmountLabel { get; init; }
    DateTimeOffset Date { get; init; }
    string HostedUrl { get; init; }
    string Id { get; init; }
    string PdfUrl { get; init; }
    string Status { get; init; }
  // Parallax node-type strings emitted by for the Stripe-embedded surfaces. The frontend resolver in @ikonai/sdk-react-ui-billing matches against these exact strings — they form a cross-language contract. If a constant value changes here, update the matching constant in platform-typescript/sdk/sdk-react-ui-billing/src/node-types.ts.
  static class BillingNodeTypes
    // Node type for Stripe Connect Account Management ().
    static string ConnectAccountManagement
    // Node type for Stripe Connect Balances ().
    static string ConnectBalances
    // Node type for Stripe Connect Documents ().
    static string ConnectDocuments
    // Node type for Stripe Connect Notification Banner ().
    static string ConnectNotificationBanner
    // Node type for Stripe Connect Account Onboarding ().
    static string ConnectOnboarding
    // Node type for Stripe Connect Payments ().
    static string ConnectPayments
    // Node type for Stripe Connect Payouts ().
    static string ConnectPayouts
    // Node type for Stripe Embedded Checkout ().
    static string EmbeddedCheckout
    // Node type for Stripe Elements PaymentElement bound to a PaymentIntent ().
    static string PaymentIntent
    // Node type for Stripe Elements PaymentElement bound to a SetupIntent ().
    static string SetupIntent
  // One saved card / payment method.
  sealed class BillingPaymentMethodView : IEquatable<BillingPaymentMethodView>
    // One saved card / payment method.
    ctor(string Id, string Brand, string Last4, int ExpMonth, int ExpYear, bool IsDefault = false)
    string Brand { get; init; }
    int ExpMonth { get; init; }
    int ExpYear { get; init; }
    string Id { get; init; }
    bool IsDefault { get; init; }
    string Last4 { get; init; }
  // View-model records for the Parallax billing components. They are intentionally lightweight and decoupled from the Stripe-shaped records so the components can be driven from any source — a live , a fake in-memory list, or static catalog data.
  sealed class BillingPlanView : IEquatable<BillingPlanView>
    // View-model records for the Parallax billing components. They are intentionally lightweight and decoupled from the Stripe-shaped records so the components can be driven from any source — a live , a fake in-memory list, or static catalog data.
    ctor(string PlanId, string Name, string PriceLabel, string? IntervalLabel = null, IReadOnlyList<string>? Features = null, string? Badge = null, string? CtaLabel = null, bool Highlighted = false, bool Disabled = false)
    string Badge { get; init; }
    string CtaLabel { get; init; }
    bool Disabled { get; init; }
    IReadOnlyList<string> Features { get; init; }
    bool Highlighted { get; init; }
    string IntervalLabel { get; init; }
    string Name { get; init; }
    string PlanId { get; init; }
    string PriceLabel { get; init; }
  // Subscription header / status card model.
  sealed class BillingSubscriptionView : IEquatable<BillingSubscriptionView>
    // Subscription header / status card model.
    ctor(string PlanName, string Status, DateTimeOffset? CurrentPeriodEnd = null, bool CancelAtPeriodEnd = false, string? PriceLabel = null)
    bool CancelAtPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    string PlanName { get; init; }
    string PriceLabel { get; init; }
    string Status { get; init; }
  // Extension methods for Calendar and DatePicker components.
  static class CalendarExtensions
    // Month-grid date selector. Renders a single month with day cells. Dates are ISO yyyy-MM-dd strings.
    static void Calendar(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? month = null, string? defaultMonth = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, string? locale = null, bool? disabled = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onMonthChange = null, string file = "", int line = 0)
    // Button that opens a popover containing a .
    static void DatePicker(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, string? format = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? calendarStyle = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
  // Which physical camera to prefer when starting the capture. Maps to the W3C MediaStream facingMode constraint and is treated as an "ideal" hint — the browser falls back to whatever camera is available if the requested side does not exist (e.g. desktops without a rear camera).
  enum CameraFacing
    User
    Environment
  // Options for capturing an image from the client's camera.
  sealed class CaptureImageActionOptions : ActionOptions, IEquatable<CaptureImageActionOptions>
    ctor()
    // Hardware constraints for camera selection.
    CaptureImageConstraints Constraints { get; init; }
    // Output image format.
    ClientImageCaptureFormat? Format { get; init; }
    // Desired image height in pixels.
    int? Height { get; init; }
    // How the capture is presented (native OS camera UI vs. headless silent grab). Defaults to — silent webcam capture via getUserMedia, which works uniformly on desktop and mobile. Set to to opt in to the OS camera app on phones (preview + shutter + front/back toggle); on desktop browsers Native transparently falls back to the headless path because the web platform doesn't expose a camera-app launch.
    CaptureImageMode? Mode { get; init; }
    // Image quality (0.0 to 1.0) for lossy formats.
    double? Quality { get; init; }
    // Desired image width in pixels.
    int? Width { get; init; }
  // Hardware constraints for image capture. Applied directly when is . In mode only is honored (mapped to the file input's capture attribute); the OS camera UI ignores other constraints.
  sealed class CaptureImageConstraints : IEquatable<CaptureImageConstraints>
    ctor()
    // Preferred camera device ID. Headless mode only.
    string DeviceId { get; init; }
    // Preferred camera side (front vs. rear). Most useful on phones where opens the rear camera by default. On desktops with only a webcam this is ignored.
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
    // Number of slides advanced per navigation step at this breakpoint. Defaults to when null.
    int? SlidesPerGroup { get; init; }
    // Number of slides visible in the viewport at this breakpoint.
    int SlidesPerView { get; init; }
  // Extension methods for Carousel components.
  static class CarouselExtensions
    // Horizontal or vertical carousel with optional navigation arrows and indicator dots.
    static void Carousel(UIView view, string[]? style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, int? slidesPerView = null, int? slidesPerGroup = null, int? slideGapPx = null, IEnumerable<CarouselBreakpoint>? breakpoints = null, IEnumerable<CarouselSlideItem>? slides = null, bool? showArrows = null, bool? showIndicators = null, string? previousLabel = null, string? nextLabel = null, string? previousIconName = null, string? nextIconName = null, string[]? rootStyle = null, string[]? viewportStyle = null, string[]? slideStyle = null, string[]? previousStyle = null, string[]? nextStyle = null, string[]? indicatorsStyle = null, string[]? indicatorStyle = null, string[]? indicatorActiveStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<double, Task>? onIndexChange = null, string file = "", int line = 0)
    // A single slide inside a . Use when rendering slides manually.
    static void Slide(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Declarative slide definition for .
  sealed class CarouselSlideItem : IEquatable<CarouselSlideItem>
    // Declarative slide definition for .
    ctor(Action<UIView> Content, string? Key = null)
    // Builder function for rendering the slide.
    Action<UIView> Content { get; init; }
    // Optional stable key used for diffing.
    string Key { get; init; }
  // Extension methods for the ChatLog primitive — the canonical chat-bubble layout shape: header + scrolling auto-scrolled body + composer. Wraps with chat-friendly defaults so callers don't have to remember to set autoScroll: true.
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
  // Output string format for .
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
    IReadOnlyList<ClientContact> Contacts { get; init; }
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
    // Row — positional (style, children) overload (see ).
    static void Row(UIView view, string[]? style, Action<UIView> children, string file = "", int line = 0)
    // Container for layering children on top of each other. Use with Layer components as children.
    static void Stack(UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Defines a column in a content grid including optional header, width, flex, and alignment.
  class ContentGridColumn : IEquatable<ContentGridColumn>
    // Defines a column in a content grid including optional header, width, flex, and alignment.
    ctor(string? Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string Header { get; init; }
    string Width { get; init; }
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
    static void Link(UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, string? iconPosition = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
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
    byte[] Data { get; init; }
    // Suggested filename for the downloaded file.
    string Filename { get; init; }
    // MIME type for binary data (e.g. "image/png"). Optional — defaults to "application/octet-stream" when is set without a MIME type.
    string MimeType { get; init; }
    // URL to download. Can be a regular URL or a data URL. If Data is provided, this is auto-generated from the binary data using , falling back to "application/octet-stream" when MimeType is unset so the download still fires.
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
    string OverId { get; init; }
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
    string OverId { get; init; }
  // Event args for drag start in @dnd-kit.
  sealed class DragStartArgs : IEquatable<DragStartArgs>
    // Event args for drag start in @dnd-kit.
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for escape key down events on overlays.
  sealed class EscapeKeyDownArgs : IEquatable<EscapeKeyDownArgs>
    // Event args for escape key down events on overlays.
    ctor()
  // Hint used by to preload the slide's primary media asset.
  enum FeedMediaKind
    None
    Image
    Video
    VideoFull
  // Extension methods for the FeedScroller component — a vertically-snapping, full-viewport feed optimized for media-heavy content (TikTok / Reels / Shorts-style).
  static class FeedScrollerExtensions
    // Renders a TikTok-style vertical feed: each slide occupies the viewport and snaps into place.
    static void FeedScroller(UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[]? style = null, string[]? slideStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onActiveChange = null, Func<double, Task>? onScrollNearEnd = null, Func<bool, Task>? onMuteChange = null, string file = "", int line = 0)
    // A single slide inside a . Use when rendering slides manually rather than via the declarative API.
    static void FeedSlide(UIView view, int index, string[]? style = null, FeedMediaKind mediaKind = None, string? mediaUrl = null, string? mediaPoster = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
  // A single slide in a .
  sealed class FeedSlide : IEquatable<FeedSlide>
    // A single slide in a .
    ctor(Action<UIView> Content, string? Key = null, FeedMediaKind MediaKind = None, string? MediaUrl = null, string? MediaPoster = null)
    // Builder invoked to render the slide. Only slides inside the render window are realized.
    Action<UIView> Content { get; init; }
    // Stable key used for diffing and preload identity. Defaults to slide index.
    string Key { get; init; }
    // Kind of media the slide needs preloaded.
    FeedMediaKind MediaKind { get; init; }
    // Optional poster image URL for video slides.
    string MediaPoster { get; init; }
    // URL of the media asset matching .
    string MediaUrl { get; init; }
  // Extension methods for file picker components. Unlike , a FilePicker only opens the native file picker and reports selected file metadata to the server — it does not transfer bytes. The picked File handles are cached on the client and uploaded later by a rendered with a matching seedSelectionIds prop.
  static class FilePickerExtensions
    // Native file picker. Emits once per selected file with its metadata (name, mime, size, client-generated selection id). The File bytes stay on the client and are not transferred until a FileUpload with matching seedSelectionIds is mounted.
    static void FilePicker(UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FilePickerSelectedArgs, Task>? onFileSelected = null, Func<FilePickerValidationErrorArgs, Task>? onValidationError = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Metadata for a file chosen in a . The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
  sealed class FilePickerSelectedArgs : IEquatable<FilePickerSelectedArgs>
    // Metadata for a file chosen in a . The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  // Reported when client-side validation rejects a picked file (e.g. file too large for maxFileSize). Host UIs should surface to the user — without a handler the rejection is silent and the user just sees "nothing happened" after clicking the picker.
  sealed class FilePickerValidationErrorArgs : IEquatable<FilePickerValidationErrorArgs>
    // Reported when client-side validation rejects a picked file (e.g. file too large for maxFileSize). Host UIs should surface to the user — without a handler the rejection is silent and the user just sees "nothing happened" after clicking the picker.
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
    string TargetId { get; init; }
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
  // Hour display format for .
  enum HourFormat
    Hour24
    Hour12
  // Event returned from an image capture action with the captured image data.
  sealed class ImageCaptureActionEvent : ActionEvent, IEquatable<ImageCaptureActionEvent>
    // Event returned from an image capture action with the captured image data.
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string Data { get; init; }
    int Height { get; init; }
    string Mime { get; init; }
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
    static void OtpField(UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Individual input slot for OTP.
    static void OtpFieldInput(UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Password input with visibility toggle.
    static void PasswordToggleField(UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Icon that changes based on visibility state.
    static void PasswordToggleFieldIcon(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null, string file = "", int line = 0)
    // The password input element.
    static void PasswordToggleFieldInput(UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, string file = "", int line = 0)
    // Button to toggle password visibility.
    static void PasswordToggleFieldToggle(UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Two-way bind a TextArea to a in one call. Same shape as the TextField bind overload.
    static void TextArea(UIView view, Reactive<string> bind, string[]? style = null, string? placeholder = null, bool? disabled = null, int? rows = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Multi-line text input area.
    static void TextArea(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Two-way bind a TextField to a in one call — reads bind.Value for the controlled value and writes bind.Value = v on every keystroke. Use this instead of pairing value: bind.Value with a manual onValueChange.
    static void TextField(UIView view, Reactive<string> bind, string[]? style = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, bool? multiline = null, int? rows = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, string file = "", int line = 0)
    // Single-line text input field.
    static void TextField(UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, string file = "", int line = 0)
  // Event args for interact outside events on overlays (combines pointer and focus).
  sealed class InteractOutsideArgs : IEquatable<InteractOutsideArgs>
    // Event args for interact outside events on overlays (combines pointer and focus).
    ctor(string? TargetId)
    string TargetId { get; init; }
  // String constants for common keyboard key names, matching the browser KeyboardEvent.key specification. Use these with for type-safe key filtering. Raw strings can also be used for uncommon keys not listed here.
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
    // Resizable split panel with a drag handle between two panes. Resize is handled entirely on the client — only the final size is sent to the server via .
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
  // Event data for media capture start/stop callbacks, containing the stream identifier and capture kind. identifies the user who initiated the capture and is populated for all capture kinds (audio, camera, screen). Prefer reading / rather than tracking streamId-to-client mappings yourself.
  sealed class MediaCaptureEvent : IEquatable<MediaCaptureEvent>
    // Event data for media capture start/stop callbacks, containing the stream identifier and capture kind. identifies the user who initiated the capture and is populated for all capture kinds (audio, camera, screen). Prefer reading / rather than tracking streamId-to-client mappings yourself.
    ctor(string StreamId, string Kind)
    // Client context of the user who initiated the capture.
    Context ClientContext { get; init; }
    // Client session id of the user who initiated the capture.
    int? ClientSessionId { get; }
    string Kind { get; init; }
    string StreamId { get; init; }
    // User id of the user who initiated the capture.
    string UserId { get; }
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
    // Push-to-talk microphone button: a CaptureButton(kind: Audio, mode: Hold) that integrates with . After enabling speech recognition once (Audio.UseSpeechRecognition(...)), subscribe to Audio.SpeechRecognizedAsync to receive transcriptions when the user releases the button. The user's client context is carried on the event args — no streamId-to-client plumbing needed in the app.
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
    static void AlertDialog(UIView view, bool? open = null, bool? defaultOpen = null, string? title = null, string? description = null, string? cancelLabel = null, string? actionLabel = null, Func<Task>? onAction = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? cancelStyle = null, string[]? actionStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
    // Modal dialog window.
    static void Dialog(UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
    // Rich content card that appears on hover with configurable delays.
    static void HoverCard(UIView view, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
    // Floating content panel that appears next to a trigger element.
    static void Popover(UIView view, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
    // Toast notification with built-in provider and viewport.
    static void Toast(UIView view, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string? title = null, string? description = null, bool? showClose = null, string? closeLabel = null, Action<UIView>? content = null, string[]? toastStyle = null, string[]? viewportStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, Func<Task>? onEscapeKeyDown = null, Func<Task>? onPause = null, Func<Task>? onResume = null, Func<ToastSwipeArgs, Task>? onSwipeStart = null, Func<ToastSwipeArgs, Task>? onSwipeMove = null, Func<ToastSwipeArgs, Task>? onSwipeEnd = null, Func<ToastSwipeArgs, Task>? onSwipeCancel = null, string file = "", int line = 0)
    // Brief informational message that appears on hover. Includes built-in provider.
    static void Tooltip(UIView view, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
  // One page of items plus the controls needed to render prev/next buttons. Returned by .
  sealed class Page<T> : IEquatable<Page<T>>
    // One page of items plus the controls needed to render prev/next buttons. Returned by .
    ctor(IReadOnlyList<T> Items, int Index, int TotalPages, int PageSize, bool CanPrev, bool CanNext, Func<Task> Prev, Func<Task> Next, Func<int, Task> JumpTo, Func<Task> First, Func<Task> Last, IReadOnlyList<T> Source)
    // True if there is a next page.
    bool CanNext { get; init; }
    // True if there is a previous page.
    bool CanPrev { get; init; }
    // Action that jumps to page 0.
    Func<Task> First { get; init; }
    // Zero-based current page index.
    int Index { get; init; }
    // The slice of for the current page.
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
    // Total number of pages (always >= 1, even when is empty).
    int TotalPages { get; init; }
  // Bounded-cursor primitive on top of . Slices an in-memory list, returns the slice + bound actions (Prev/Next/JumpTo/First/Last) the caller binds to whatever UI fits. Holds zero rendering opinion — no tab bars, no default control rows, no opinionated layout. Most Ikon apps don't need pagination at all (live feeds, autoscroll, virtualization handle the common cases via Reactive<List<T>> + ScrollArea(autoScroll: true)). Use this when you have a static list large enough to warrant explicit page navigation. For DB-backed pagination (load only the current page from a backend), drive directly and observe its value in your data-loading code — same per-client semantics, no special helper needed.
  static class PaginationExtensions
    static Page<T> Paginate<T>(UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  // Options for the Contact Picker API action.
  sealed class PickContactsActionOptions : ActionOptions, IEquatable<PickContactsActionOptions>
    ctor()
    // When true, allows selecting multiple contacts.
    bool Multiple { get; init; }
  // Event args for pointer down outside events on overlays.
  sealed class PointerDownOutsideArgs : IEquatable<PointerDownOutsideArgs>
    // Event args for pointer down outside events on overlays.
    ctor(string? TargetId)
    string TargetId { get; init; }
  // Extension methods for QR code generation.
  static class QrCodeExtensions
    // QR code image. Generates a QR code server-side and renders it as an image.
    static void QR(UIView view, string[]? style = null, string? value = null, int size = 256, string? key = null, string file = "", int line = 0)
  // Extension methods for the RichTextEditor component.
  static class RichTextEditorExtensions
    // Inline rich-text editor with a configurable toolbar. Values are HTML strings.
    static void RichTextEditor(UIView view, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, IReadOnlyList<RichTextTool>? tools = null, bool? showToolbar = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? toolbarStyle = null, string[]? toolbarButtonStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, string file = "", int line = 0)
  // Formatting action available in the toolbar.
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
  // Tiny primitives for using as a signal the app reads to decide what to render. Routes, tabs, modes, panel selections, "which dialog is open" — same shape, same primitives. Intentionally minimal: no opinionated tab bars, no URL coupling, no rendering bias. The signal is the building block; the app decides how to consume it. For URL ↔ signal sync (browser bar, deep links, back/forward), use on the host app — keeps URL concerns in one place instead of forking them through this layer.
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
  // Extension methods for the ScrollColumn primitive — a header/body/footer dialog pattern where the body scrolls. Wraps a with the correct flex sizing so scrolling engages without ceremony.
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
    static void Select(UIView view, string[]? style = null, IReadOnlyList<SelectOption>? options = null, IReadOnlyList<SelectOptionGroup>? groups = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, bool? required = null, bool? open = null, string? name = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? itemStyle = null, string[]? itemIndicatorStyle = null, string? indicatorIconName = "check", string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool?, Task>? onOpenChange = null, string file = "", int line = 0)
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
    string Label { get; init; }
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
    string Text { get; init; }
    // Title for the shared content.
    string Title { get; init; }
    // URL to share.
    string Url { get; init; }
  // Options for showing a browser notification.
  sealed class ShowNotificationActionOptions : ActionOptions, IEquatable<ShowNotificationActionOptions>
    // Notification body text.
    string Body { get; init; }
    // URL of the notification icon image.
    string Icon { get; init; }
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
  // Smallest time unit shown by a .
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
  // Extension methods for the DOM-virtualized scroll containers and . Items outside the visible window plus an overscan buffer have their content children skipped at the React layer (the wrapper still occupies space via fixed dimensions), so DOM size scales with viewport, not itemCount.
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
  // Defines a UI theme providing base CSS and a default icon library.
  interface ITheme
    // Global CSS injected into the client as the theme baseline.
    string Css { get; }
    // The default icon library name (e.g. "lucide") used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }

namespace Ikon.Parallax.Themes.Ikon
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
  // Resolves a Crosswind class name (color step, radius rung, font role, motion duration / easing) to the underlying CSS value the IkonTheme variables expect. Hex / rem / family-stack passthrough — values that don't look like Crosswind tokens are returned as-is so users can mix in raw hex when they need a custom palette.
  static class CrosswindResolver
    // True when a Tailwind palette token represents a "light" step (50-500 inclusive). Used by to pick the auto-derived primary-foreground (dark text on light brand vs. white text on dark brand). Returns null when we can't infer (raw hex, non-palette tokens) — caller falls back to luminance computation.
    static bool? IsLightPaletteStep(string token)
    // True when the token is a recognized Tailwind palette step (e.g. "amber-400", "zinc-950"). Used by to dispatch indexer overrides to the right CSS-variable target.
    static bool IsTailwindPaletteToken(string token)
    // Resolve a color token (e.g. "amber-400", "zinc-950") to a CSS color expression referencing the corresponding Tailwind palette CSS variable shipped by TailwindCssBaseline. Raw colors (hex, oklch, hsl, rgb, named) pass through unchanged.
    static string ResolveColor(string token)
    // Resolve a motion duration token (e.g. "duration-150", "150ms", "0.2s") to a CSS duration literal.
    static string ResolveDuration(string token)
    // Resolve an easing token (e.g. "ease-out", "linear") to a CSS easing value. Cubic-bezier expressions and raw keywords pass through unchanged.
    static string ResolveEasing(string token)
    // Resolve a font-family token (e.g. "font-sans", "font-serif", or a literal family name) to a quoted CSS font-family stack. Custom family names get a sensible system fallback chain.
    static string ResolveFontFamily(string token)
    // Resolve a radius token (e.g. "rounded-lg", "rounded-2xl") to its rem value. Raw rem / px values pass through unchanged.
    static string ResolveRadius(string token)
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
  static class Icon
    static string Default
    static string Lg
    static string Md
    static string Sm
    static string Spinner
    static string SpinnerLg
    static string SpinnerSm
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
    // Backwards-compatible alias for the tri-color decorative overlay. New code should prefer Showcase for clarity.
    static string Gradient
    static string Plain
    static string Showcase
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
  sealed class Theme : ITheme
    ctor()
    string Css { get; }
    string DefaultIconLibrary { get; }
  // One named override on an app's theme: a role + Crosswind value, or a free-form custom CSS variable. The codegen Styling Oracle emits a list of these; the CSS renderer walks them.
  sealed class ThemeIntent : IEquatable<ThemeIntent>
    // One named override on an app's theme: a role + Crosswind value, or a free-form custom CSS variable. The codegen Styling Oracle emits a list of these; the CSS renderer walks them.
    ctor(string Role, string Value, string? CustomName = null)
    string CustomName { get; init; }
    string Role { get; init; }
    string Value { get; init; }
    // Theme roles recognized by the renderer. Anything else passes through as a custom variable (in which case must be set and matches the variable name without the leading --).
    static IReadOnlyList<string> Roles
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
    ColorToken Color { get; init; }
    double? Width { get; init; }
    BorderSideToken MergeOver(BorderSideToken? other)
  sealed class BorderToken : IEquatable<BorderToken>
    ctor(BorderSideToken? Left, BorderSideToken? Top, BorderSideToken? Right, BorderSideToken? Bottom)
    BorderSideToken Bottom { get; init; }
    BorderSideToken Left { get; init; }
    BorderSideToken Right { get; init; }
    BorderSideToken Top { get; init; }
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
    string Version { get; init; }
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
    string Description { get; init; }
    T Value { get; init; }
  sealed class CanvasTypographyScale
    ctor()
    string Description { get; init; }
    string FontFamily { get; init; }
    string FontSize { get; init; }
    string LetterSpacing { get; init; }
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
    string Raw { get; init; }
    string Ref { get; init; }
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
    FlutterStyleTokens Flutter { get; init; }
    IReadOnlyList<MotionBindingMetadata> MotionBindings { get; init; }
  class ContainerVariant : IEquatable<ContainerVariant>
    ctor(string? Name, string? Query, string? Breakpoint)
    string Breakpoint { get; init; }
    string Name { get; init; }
    string Query { get; init; }
    bool WantsBreakpoint { get; }
    ContainerVariant WithBreakpoint(string breakpoint)
  static class CssEmitter
    static string Emit(CompileResult result)
  static class CssProcessor
    static string GetCss(string tailwindDeclaration, string classId)
    static CompiledStyle GetStyle(string tailwindDeclaration, string classId)
  class CssRule : IEquatable<CssRule>
    ctor(string? AtRule, string Selector, Dictionary<string, string> Decls)
    string AtRule { get; init; }
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
    ctor(EdgeInsetsToken? Padding, EdgeInsetsToken? Margin, ColorToken? BackgroundColor, BorderToken? Border, BorderRadiusToken? BorderRadius, SizeToken? Size, TextStyleToken? Text, FlexToken? Flex, double? Opacity, IReadOnlyList<ShadowToken>? Shadow, OverflowToken? Overflow, TransformToken? Transform, PositionToken? Position, GradientToken? Gradient, MotionToken? Motion, bool? Hidden, bool? Visible, CursorToken? Cursor, double? AspectRatio, int? ZIndex)
    double? AspectRatio { get; init; }
    ColorToken BackgroundColor { get; init; }
    BorderToken Border { get; init; }
    BorderRadiusToken BorderRadius { get; init; }
    CursorToken? Cursor { get; init; }
    static FlutterStyleTokens Empty { get; }
    FlexToken Flex { get; init; }
    GradientToken Gradient { get; init; }
    bool? Hidden { get; init; }
    bool IsEmpty { get; }
    EdgeInsetsToken Margin { get; init; }
    MotionToken Motion { get; init; }
    double? Opacity { get; init; }
    OverflowToken? Overflow { get; init; }
    EdgeInsetsToken Padding { get; init; }
    PositionToken Position { get; init; }
    IReadOnlyList<ShadowToken> Shadow { get; init; }
    SizeToken Size { get; init; }
    TextStyleToken Text { get; init; }
    TransformToken Transform { get; init; }
    bool? Visible { get; init; }
    int? ZIndex { get; init; }
  sealed class GradientToken : IEquatable<GradientToken>
    ctor(string Direction, ColorToken? From, ColorToken? Via, ColorToken? To)
    string Direction { get; init; }
    ColorToken From { get; init; }
    ColorToken To { get; init; }
    ColorToken Via { get; init; }
  sealed class MotionBindingMetadata : IEquatable<MotionBindingMetadata>
    ctor(string Source, string? Min, string? Max, string? Clamp, bool Reverse, string? Ease, string? Map, string? TargetId)
    string Clamp { get; init; }
    string Ease { get; init; }
    string Map { get; init; }
    string Max { get; init; }
    string Min { get; init; }
    bool Reverse { get; init; }
    string Source { get; init; }
    string TargetId { get; init; }
  sealed class MotionToken : IEquatable<MotionToken>
    ctor(string? Type, double? Duration, string? Ease, double? Delay, string? IterationMode)
    double? Delay { get; init; }
    double? Duration { get; init; }
    string Ease { get; init; }
    string IterationMode { get; init; }
    string Type { get; init; }
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
    static ValueTuple<string, string> ComposeTemplate(IReadOnlyList<string> variants, string? track, ContainerVariant? container = null)
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
    static ValueTuple<string, string> ResolveTextSize(string tokenOrLength)
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
    static List<TailwindDescription> Deduplicate(List<TailwindDescription> classes)
  class TailwindDescription : IEquatable<TailwindDescription>
    ctor(List<string> Variants, string? Track, string Utility, List<ArgValue> Args, bool Important, bool Negative, ContainerVariant? Container = null, bool HasBracketArg = false, bool IsArbitraryProperty = false)
    List<ArgValue> Args { get; init; }
    ContainerVariant Container { get; init; }
    bool HasBracketArg { get; init; }
    bool Important { get; init; }
    bool IsArbitraryProperty { get; init; }
    bool Negative { get; init; }
    string Track { get; init; }
    string TrackKey { get; }
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
    string LetterSpacing { get; }
    string LineHeight { get; }
    string Size { get; }
  static class TailwindNormalizer
    static TailwindDescription Normalize(TailwindDescription tw)
  static class TailwindParser
    static List<TailwindDescription> ParseManyRaw(string inputLine)
    static TailwindDescription ParseRaw(string input)
    static ValueTuple<List<string>, string, ContainerVariant> SplitVariants(List<string> variants)
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
    ColorToken Color { get; init; }
    TextDecorationToken? Decoration { get; init; }
    string FontFamily { get; init; }
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
    static List<TailwindDescription> Combine(List<TailwindDescription> classes)
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
  sealed class AppAttribute : Attribute
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL, Opcode sendOpcodeGroups = GROUP_ALL, string[]? dependencies = null)
    int AppVersion { get; }
    string[] Dependencies { get; }
    string Description { get; }
    string Guid { get; }
    string Name { get; }
    string ProductId { get; }
    Opcode ReceiveOpcodeGroups { get; }
    Opcode SendOpcodeGroups { get; }
    UserType UserType { get; }
    int Version { get; }
    PluginAttribute ToPluginAttribute(Type owner)
  static class AppDatabaseConnection
    static DbConnection Create(IAppBase app, string databaseName)
    static DbConnection Create(DatabaseConnectionInfo dbInfo)
  sealed class AppEndpointHost : IAsyncDisposable
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    int LocalPort { get; }
    string PublicUrl { get; }
    ValueTask DisposeAsync()
    void MapGet(string pattern, Func<HttpContext, Task> handler)
    void MapPost(string pattern, Func<HttpContext, Task> handler)
    void MapWebSocket(string pattern, Func<HttpContext, WebSocket, Task> handler)
    Task StartAsync(CancellationToken cancellationToken = null)
    Task StopAsync(CancellationToken cancellationToken = null)
  class App<TConfig> : BasePlugin<App<TConfig>, WrapperConfig<TConfig>>, IAppBase, IApp<TConfig>, IUserAppInstanceHost
    ctor(Type appInstanceType, WrapperConfig<TConfig> userConfig, PluginAttribute pluginAttribute, string argsJson)
    BackgroundWork BackgroundWork { get; }
    TConfig Config { get; }
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    EmailService Email { get; }
    int MaxMemoryLimitMb { get; }
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    ReactiveGlobalState ReactiveGlobalState { get; }
    ReactiveRoot ReactiveRoot { get; }
    Secrets Secrets { get; }
    object UserAppInstance { get; }
    IReadOnlyList<WebhookInfo> Webhooks { get; }
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = null)
    Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", CancellationToken ct = null)
    Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = null)
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
  class App<TSessionIdentity, TClientParameters> : BasePlugin<App<TSessionIdentity, TClientParameters>, BasePluginConfig>, IAppBase, IApp<TSessionIdentity, TClientParameters>, IUserAppInstanceHost
    ctor(Type appInstanceType, PluginAttribute pluginAttribute, string argsJson)
    BackgroundWork BackgroundWork { get; }
    IClientCollection<TClientParameters> Clients { get; }
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    EmailService Email { get; }
    int MaxMemoryLimitMb { get; }
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    ReactiveGlobalState ReactiveGlobalState { get; }
    ReactiveRoot ReactiveRoot { get; }
    Secrets Secrets { get; }
    TSessionIdentity SessionIdentity { get; }
    object UserAppInstance { get; }
    IReadOnlyList<WebhookInfo> Webhooks { get; }
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = null)
    Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", CancellationToken ct = null)
    Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = null)
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
  delegate AsyncEventHandler<TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler`1<TEventArgs>(TEventArgs e)
  class Audio
    ctor(IAppBase app)
    AudioEncoderOptions DefaultEncoderOptions { get; set; }
    AudioMetrics Metrics { get; }
    SpeechMixer SpeechMixer { get; }
    ValueTask CloseAllAsync()
    ValueTask CloseAsync(string? streamId = null)
    AudioOutputStreamInfo GetOutputStreamInfo(string? streamId = null)
    ValueTask SendAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    void SendSpeech(AudioContainer audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    void SendSpeech(string speechEventId, ReadOnlySpan<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01, bool requireCorrelatedStream = true, string language = "", TimeSpan? timeout = null)
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
  class AudioInputFrameEventArgs : EventArgs, ICaptureCorrelationArgs
    ctor(string streamId, Context clientContext, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    float[] Samples { get; }
    string StreamId { get; }
    TimeSpan TotalDuration { get; set; }
    string UserId { get; }
  class AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, int sampleRate, int channelCount, Context clientContext, int trackId, string? correlationId)
    int ChannelCount { get; }
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
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
    string CorrelationId { get; }
    string StreamId { get; }
    string UserId { get; }
  enum AudioInputStreamingMode
    Streaming
    DelayUntilTotalDurationKnown
    DelayUntilIsLast
  class AudioOutputStreamInfo : IEquatable<AudioOutputStreamInfo>
    ctor(string StreamId, int TrackId, AudioCodec Codec, int SampleRate, int ChannelCount)
    int ChannelCount { get; init; }
    AudioCodec Codec { get; init; }
    int SampleRate { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
  sealed class AuthOutcome : IEquatable<AuthOutcome>
    ctor(HttpResult? Reject, IReadOnlyDictionary<string, string>? Claims = null)
    IReadOnlyDictionary<string, string> Claims { get; init; }
    HttpResult Reject { get; init; }
    static AuthOutcome Pass(IReadOnlyDictionary<string, string>? claims = null)
    static AuthOutcome RejectWith(HttpResult result)
  class BackgroundWork
    ValueTask<IAsyncDisposable> StartAsync()
    ValueTask StopAsync()
  static class CaptureCorrelationBridge
    static void RegisterStart(string correlationId, Func<ICaptureCorrelationArgs, Task> handler)
    static void RegisterStop(string correlationId, Func<ICaptureCorrelationArgs, Task> handler)
    static void Unregister(string correlationId)
  sealed class ClientAudioCaptureOptions : IEquatable<ClientAudioCaptureOptions>
    ctor()
    bool? AutoGainControl { get; init; }
    int? Bitrate { get; init; }
    static ClientAudioCaptureOptions Default { get; }
    string DeviceId { get; init; }
    bool? EchoCancellation { get; init; }
    bool? NoiseSuppression { get; init; }
    IReadOnlyList<int> TargetIds { get; init; }
  class ClientCollection<TClientParameters> : IClientCollection<TClientParameters>, IEnumerable, IEnumerable<IClient<TClientParameters>>
    ctor()
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters> Item { get; }
    IEnumerator<IClient<TClientParameters>> GetEnumerator()
  sealed class ClientContact : IEquatable<ClientContact>
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    IReadOnlyList<string> Emails { get; init; }
    IReadOnlyList<string> Names { get; init; }
    IReadOnlyList<string> Phones { get; init; }
  static class ClientFunctions
    static Task<ClientImageCapture> CaptureImageAsync(int targetId, ClientImageCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<bool> ExitFullscreenAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> ExitFullscreenAsync(CancellationToken cancellationToken = null)
    static Task<int?> GetBatteryLevelAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<int?> GetBatteryLevelAsync(CancellationToken cancellationToken = null)
    static Task<string> GetLanguageAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetLanguageAsync(CancellationToken cancellationToken = null)
    static Task<ClientLocation> GetLocationAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<ClientLocation> GetLocationAsync(CancellationToken cancellationToken = null)
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(CancellationToken cancellationToken = null)
    static Task<string> GetNetworkTypeAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetNetworkTypeAsync(CancellationToken cancellationToken = null)
    static Task<string> GetThemeAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetThemeAsync(CancellationToken cancellationToken = null)
    static Task<string> GetTimezoneAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetTimezoneAsync(CancellationToken cancellationToken = null)
    static Task<string> GetUrlAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetUrlAsync(CancellationToken cancellationToken = null)
    static Task<string> GetVisibilityAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetVisibilityAsync(CancellationToken cancellationToken = null)
    static Task<bool> KeepScreenAwakeAsync(int targetId, bool enabled, CancellationToken cancellationToken = null)
    static Task<bool> KeepScreenAwakeAsync(bool enabled, CancellationToken cancellationToken = null)
    static Task<bool> LoginShowAsync(int targetId, string? reason = null, CancellationToken cancellationToken = null)
    static Task<bool> LogoutAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> LogoutAsync(CancellationToken cancellationToken = null)
    static Task<bool> OpenExternalUrlAsync(int targetId, string url, CancellationToken cancellationToken = null)
    static Task<bool> OpenExternalUrlAsync(string url, CancellationToken cancellationToken = null)
    static Task<string> PlaySoundAsync(int targetId, string url, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string> PlaySoundAsync(int targetId, byte[] data, string mimeType, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string> PlaySoundAsync(string url, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string> PlaySoundAsync(byte[] data, string mimeType, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<bool> RequestFullscreenAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> RequestFullscreenAsync(CancellationToken cancellationToken = null)
    static Task<bool> ScrollToAsync(int targetId, double x, double y, bool smooth = false, CancellationToken cancellationToken = null)
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, CancellationToken cancellationToken = null)
    static Task<bool> SetThemeAsync(int targetId, string theme, bool persist = true, CancellationToken cancellationToken = null)
    static Task<bool> SetThemeAsync(string theme, bool persist = true, CancellationToken cancellationToken = null)
    static Task<bool> SetUrlAsync(int targetId, string url, bool replace = false, bool preserveQueryParams = false, CancellationToken cancellationToken = null)
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, CancellationToken cancellationToken = null)
    static Task<string> StartAudioCaptureAsync(int targetId, ClientAudioCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartVideoCaptureAsync(int targetId, ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<bool> StopCaptureAsync(int targetId, string streamId, CancellationToken cancellationToken = null)
    static Task<bool> StopCaptureAsync(string streamId, CancellationToken cancellationToken = null)
    static Task<bool> StopSoundAsync(int targetId, string playbackId, CancellationToken cancellationToken = null)
    static Task<bool> StopSoundAsync(string playbackId, CancellationToken cancellationToken = null)
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
  sealed class ClientLocation : IEquatable<ClientLocation>
    ctor(double Latitude, double Longitude, double Accuracy)
    double Accuracy { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
  static class ClientMediaCaptureSerializer
    static string SerializeAudioOptions(ClientAudioCaptureOptions? options)
    static string SerializeImageOptions(ClientImageCaptureOptions? options)
    static string SerializeVideoOptions(ClientVideoCaptureOptions? options)
  sealed class ClientMediaDevice : IEquatable<ClientMediaDevice>
    ctor(string DeviceId, string Kind, string Label, string GroupId)
    string DeviceId { get; init; }
    string GroupId { get; init; }
    string Kind { get; init; }
    string Label { get; init; }
  sealed class ClientProfile
    ProfileAddress Address { get; }
    string BirthDate { get; }
    string Email { get; }
    string FirstName { get; }
    string Gender { get; }
    string Id { get; }
    bool IsAdmin { get; }
    bool IsGuest { get; }
    bool IsModerator { get; }
    string Language { get; }
    string LastName { get; }
    string Name { get; }
    string PhoneNumber { get; }
    string PreferredName { get; }
    IReadOnlyList<string> Roles { get; }
    string UserId { get; }
    string VisibleName { get; }
    object GetAttribute(string key)
    TAttributes GetAttributes<TAttributes>()
    bool HasRole(UserRole role)
    bool HasRole(string role)
    bool HasRole<TRole>(TRole role)
  class ClientProfiles
    ctor(IAppBase app)
    Task AddRoleAsync(Context clientContext, UserRole role)
    Task AddRoleAsync(Context clientContext, string role)
    void ClearCache()
    Task<IReadOnlyList<ClientProfile>> FindProfilesAsync(Dictionary<string, string> filters, int maxResults = 1000)
    Task<IReadOnlyList<ClientProfile>> GetAllProfilesAsync(int maxResults = 1000)
    TAttributes GetAttributes<TAttributes>(Context clientContext)
    ClientProfile GetProfile(Context clientContext)
    bool HasRole(Context clientContext, UserRole role)
    bool HasRole(Context clientContext, string role)
    bool HasRole<TRole>(Context clientContext, TRole role)
    bool IsAdmin(Context clientContext)
    bool IsGuest(Context clientContext)
    bool IsModerator(Context clientContext)
    Task RefreshProfileAsync(Context clientContext)
    Task RefreshProfileAsync(string userId)
    Task RemoveRoleAsync(Context clientContext, UserRole role)
    Task RemoveRoleAsync(Context clientContext, string role)
    void RequireAdmin(Context clientContext)
    void RequireModerator(Context clientContext)
    void RequireRole(Context clientContext, UserRole role)
    void RequireRole(Context clientContext, string role)
    Task SetAttributesAsync<TAttributes>(Context clientContext, TAttributes attrs)
    Task SetRolesAsync(Context clientContext, IEnumerable<UserRole> roles)
    Task SetRolesAsync(Context clientContext, IEnumerable<string> roles)
    ClientProfile TryGetProfile(Context clientContext)
    ClientProfile TryGetProfile(string userId)
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
    string DeviceId { get; init; }
    int? Framerate { get; init; }
    ClientHardwareAcceleration? HardwareAcceleration { get; init; }
    int? Height { get; init; }
    int? KeyFrameIntervalFrames { get; init; }
    IReadOnlyList<ClientVideoCaptureCodec> PreferredCodecs { get; init; }
    IReadOnlyList<int> TargetIds { get; init; }
    int? Width { get; init; }
  enum ClientVideoCaptureSource
    Camera
    Screen
  class Client<TClientParameters> : IClient<TClientParameters>
    ctor(TClientParameters parameters)
    TClientParameters Parameters { get; }
  static class Constants
    static string DarkTheme
    static string LightTheme
  sealed class EmailService
    Task DeleteAsync(string id, CancellationToken ct = null)
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = null)
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = null)
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = null)
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = null)
    Task SendAsync(EmailSendRequest request, CancellationToken ct = null)
  sealed class FileUploadCallbackSet
    ctor()
    Func<FileUploadChunkArgs, Task> OnChunkReceived
    Func<FileUploadCompleteArgs, Task> OnUploadComplete
    Func<FileUploadErrorArgs, Task> OnUploadError
    Func<FileUploadPreStartArgs, Task<FileUploadPreStartResult>> OnUploadPreStart
    Func<FileUploadProgressArgs, Task> OnUploadProgress
    Func<FileUploadStartArgs, Task<FileUploadStartResult>> OnUploadStart
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
    string AssetUri { get; init; }
    string FileName { get; init; }
    string LocalTempFilePath { get; init; }
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
    Func<string, Task> Cancel { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadPreStartResult : IEquatable<FileUploadPreStartResult>
    ctor()
    ctor(string? assetUri)
    ctor(bool accepted, string? assetUri = null)
    bool Accepted { get; set; }
    string AssetUri { get; set; }
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
    string AssetUri { get; set; }
  static class HttpDispatchGovernance
    static Task<object> InvokeAsync(MethodInfo handler, Type ownerType, IReadOnlyDictionary<string, object?> args, Func<Task<object?>> invoke, CancellationToken ct = null)
  sealed class HttpEndpointAttribute : Attribute
    ctor(string method, string path)
    bool Absolute { get; init; }
    Type Auth { get; init; }
    string Method { get; }
    string Path { get; }
  static class HttpEndpointDiscovery
    static IReadOnlyList<HttpEndpointInfo> ForType(Type ownerType)
    static IReadOnlyList<HttpEndpointInfo> ForTypes(IEnumerable<Type> types)
  sealed class HttpEndpointEnvelope : IEquatable<HttpEndpointEnvelope>
    ctor(int StatusCode, string? Body, string ContentType)
    string Body { get; init; }
    string ContentType { get; init; }
    int StatusCode { get; init; }
  sealed class HttpEndpointInfo : IEquatable<HttpEndpointInfo>
    ctor(string Method, string Path, Type? Auth, bool Absolute, MethodInfo Handler, Type OwnerType)
    bool Absolute { get; init; }
    Type Auth { get; init; }
    MethodInfo Handler { get; init; }
    string Method { get; init; }
    Type OwnerType { get; init; }
    string Path { get; init; }
  sealed class HttpRequest : IEquatable<HttpRequest>
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  sealed class HttpResult : IEquatable<HttpResult>
    ctor(int StatusCode, object? Body = null, string ContentType = "application/json")
    object Body { get; init; }
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
  interface IAppBase : IProtocolMessageChannel
    BackgroundWork BackgroundWork { get; }
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    EmailService Email { get; }
    GlobalState GlobalState { get; }
    int MaxMemoryLimitMb { get; }
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    ReactiveGlobalState ReactiveGlobalState { get; }
    ReactiveRoot ReactiveRoot { get; }
    Secrets Secrets { get; }
    IReadOnlyList<WebhookInfo> Webhooks { get; }
    abstract Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = null)
    abstract Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", CancellationToken ct = null)
    abstract Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = null)
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
  static class IAppEventExtensions
    static void OnClientJoined(IAppBase app, Func<Context, Task> handler)
    static void OnClientJoined<TSessionIdentity, TClientParameters>(IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnClientLeft(IAppBase app, Func<Context, Task> handler)
    static void OnClientLeft<TSessionIdentity, TClientParameters>(IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnMessageReceived(IAppBase app, Func<ProtocolMessage, Task> handler)
    static void OnStarting(IAppBase app, Func<Task> handler)
    static void OnStopping(IAppBase app, Func<Task> handler)
  interface IApp<TConfig> : IAppBase, IProtocolMessageChannel
    TConfig Config { get; }
  interface IApp<TSessionIdentity, TClientParameters> : IAppBase, IProtocolMessageChannel
    TClientParameters ClientParameters { get; }
    IClientCollection<TClientParameters> Clients { get; }
    TSessionIdentity SessionIdentity { get; }
  interface ICaptureCorrelationArgs
    Context ClientContext { get; }
    string CorrelationId { get; }
    string StreamId { get; }
  interface IClientCollection<TClientParameters> : IEnumerable, IEnumerable<IClient<TClientParameters>>
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters> Item { get; }
    IEnumerable<int> Keys { get; }
  interface IClient<TClientParameters>
    TClientParameters Parameters { get; }
  interface IProfileAttributes
  interface IUserAppInstanceHost
    object UserAppInstance { get; }
  static class JsonSchemaBuilder
    static JsonElement BuildObjectSchema(IReadOnlyList<ParameterInfo> parameters)
    static JsonElement BuildObjectSchema(IReadOnlyList<ParameterInfo> parameters, IReadOnlyList<ValueTuple<string, Type, string?>> extraRequired)
  static class LoginPrompt
    static Task ShowAsync(int targetClientSessionId, string? reason = null)
    static Task ShowAsync(string? reason = null)
    static string HandoffParameterKey
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
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
    Task<string> GetPathAsync(int targetId)
    Task<string> GetPathAsync()
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
  class PersistentReactive<T> : Reactive<T>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string PostgresDatabase { get; }
    string PublicUrl { get; }
  class PersistentSessionReactive<T> : Reactive<T>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string PostgresDatabase { get; }
    string PublicUrl { get; }
  class PersistentUserReactive<T> : Reactive<T, UserScope>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string PostgresDatabase { get; }
    string PublicUrl { get; }
  sealed class ProfileAddress
    string City { get; }
    string Country { get; }
    string Municipality { get; }
    string State { get; }
    string Street { get; }
    string Zip { get; }
  sealed class ProfileData
    ctor()
    string AddressCity { get; set; }
    string AddressCountry { get; set; }
    string AddressState { get; set; }
    string AddressStreet { get; set; }
    string AddressZip { get; set; }
    string BirthDate { get; set; }
    string Email { get; set; }
    string FirstName { get; set; }
    string Gender { get; set; }
    string Language { get; set; }
    string LastName { get; set; }
    string Name { get; set; }
    string PhoneNumber { get; set; }
    string PreferredName { get; set; }
  class ReactiveRoot
    ctor(IAppBase app, int updateIntervalMs = 1000)
    ReactiveManager ReactiveManager { get; }
    Task RunAsync(Func<Task> render, Func<Context, bool>? filter = null)
  sealed class RouteTemplate
    IReadOnlyList<string> CaptureNames { get; }
    string Pattern { get; }
    static RouteTemplate Parse(string template)
    bool TryMatch(string path, out IReadOnlyDictionary<string, string> captures)
  sealed class SpeechRecognizedEventArgs : EventArgs
    ctor(string text, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    TimeSpan Duration { get; }
    int SampleCount { get; }
    string StreamId { get; }
    string Text { get; }
    string UserId { get; }
  class StartingEventArgs : EventArgs
    ctor()
  class StoppingEventArgs : EventArgs
    ctor()
  sealed class UriTemplate
    bool IsStatic { get; }
    IReadOnlyList<string> PlaceholderNames { get; }
    string Template { get; }
    IReadOnlyDictionary<string, string> Match(string uri)
    static UriTemplate Parse(string template)
  enum UserRole
    Guest
    User
    Moderator
    Admin
  class Video
    ctor(IAppBase app)
    ValueTask CloseAllAsync()
    ValueTask CloseAsync(string? streamKey = null)
    VideoOutputStreamInfo GetOutputStreamInfo(string? streamId = null)
    ValueTask SendAsync(byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, VideoCodec codec, int width, int height, double framerate, string? streamId = null, IReadOnlyList<int>? targetIds = null, int? trackId = null)
    event AsyncEventHandler<VideoInputFrameEventArgs> VideoInputFrameAsync
    event AsyncEventHandler<VideoInputStreamBeginEventArgs> VideoInputStreamBeginAsync
    event AsyncEventHandler<VideoInputStreamEndEventArgs> VideoInputStreamEndAsync
  class VideoInputFrameEventArgs : EventArgs, ICaptureCorrelationArgs
    ctor(string streamId, Context clientContext, int trackId, byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    byte[] Data { get; }
    uint DurationInUs { get; }
    int FrameNumber { get; }
    bool IsKey { get; }
    string StreamId { get; }
    ulong TimestampInUs { get; }
    int TrackId { get; }
    string UserId { get; }
  class VideoInputStreamBeginEventArgs : EventArgs, ICaptureCorrelationArgs
    ctor(string streamId, string description, string sourceType, VideoCodec codec, string codecDetails, int width, int height, double framerate, Context clientContext, int trackId, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    VideoCodec Codec { get; }
    string CodecDetails { get; }
    string CorrelationId { get; }
    string Description { get; }
    double Framerate { get; }
    int Height { get; }
    string SourceType { get; }
    string StreamId { get; }
    int TrackId { get; }
    string UserId { get; }
    int Width { get; }
  class VideoInputStreamEndEventArgs : EventArgs, ICaptureCorrelationArgs
    ctor(string streamId, Context clientContext, int trackId, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    string StreamId { get; }
    int TrackId { get; }
    string UserId { get; }
  class VideoOutputStreamInfo : IEquatable<VideoOutputStreamInfo>
    ctor(string StreamId, int TrackId, VideoCodec Codec, int Width, int Height, double Framerate)
    VideoCodec Codec { get; init; }
    double Framerate { get; init; }
    int Height { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
    int Width { get; init; }
  sealed class WebhookInfo
    ctor()
    string CellType { get; set; }
    string FunctionName { get; set; }
    string PublicUrl { get; set; }
  class WrapperConfig<TConfig> : BasePluginConfig
    ctor()
    ctor(TConfig userConfig)
    TConfig AppConfig { get; set; }

namespace Ikon.App.Auth
  sealed class AnonymousAuth
    ctor(ICell<AnonymousAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  sealed class ApiKeyAuth
    ctor(ICell<ApiKeyAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  sealed class AuthTicketAuth
    ctor(ICell<AuthTicketAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  sealed class EdgeTrustedHeaderAuth
    ctor(ICell<EdgeTrustedHeaderAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  sealed class OAuthAuth
    ctor(ICell<OAuthAuth.SessionIdentity> ctx)
    static string ConfiguredIssuer { get; }
    Task<AuthOutcome> Authenticate(HttpRequest request)
  class AnonymousAuth.SessionIdentity : IEquatable<AnonymousAuth.SessionIdentity>
    ctor()
  class ApiKeyAuth.SessionIdentity : IEquatable<ApiKeyAuth.SessionIdentity>
    ctor()
  class AuthTicketAuth.SessionIdentity : IEquatable<AuthTicketAuth.SessionIdentity>
    ctor()
  class EdgeTrustedHeaderAuth.SessionIdentity : IEquatable<EdgeTrustedHeaderAuth.SessionIdentity>
    ctor()
  class OAuthAuth.SessionIdentity : IEquatable<OAuthAuth.SessionIdentity>
    ctor()
  class SessionTokenAuth.SessionIdentity : IEquatable<SessionTokenAuth.SessionIdentity>
    ctor()
  sealed class SessionTokenAuth
    ctor(ICell<SessionTokenAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)

namespace Ikon.App.Billing
  sealed class AssetBillingConnectAccountStore : IBillingConnectAccountStore
    ctor(string assetPath = "billing/connect-account-id.json")
    Task ClearAsync(CancellationToken cancellationToken = null)
    Task<string> GetAsync(CancellationToken cancellationToken = null)
    Task SetAsync(string connectedAccountId, CancellationToken cancellationToken = null)
  sealed class BillingAccountSession : IEquatable<BillingAccountSession>
    ctor(string ClientSecret, DateTimeOffset ExpiresAt)
    string ClientSecret { get; init; }
    DateTimeOffset ExpiresAt { get; init; }
  sealed class BillingAccountSessionRequest : IEquatable<BillingAccountSessionRequest>
    bool AccountManagement { get; init; }
    bool AccountOnboarding { get; init; }
    bool Balances { get; init; }
    string ConnectedAccountId { get; init; }
    bool DisableStripeUserAuth { get; init; }
    bool Documents { get; init; }
    bool ExternalAccountCollection { get; init; }
    bool NotificationBanner { get; init; }
    bool Payments { get; init; }
    bool PaymentsCapturePayments { get; init; }
    bool PaymentsDisputeManagement { get; init; }
    bool PaymentsRefundManagement { get; init; }
    bool Payouts { get; init; }
    bool PayoutsEditPayoutSchedule { get; init; }
    bool PayoutsStandardPayouts { get; init; }
  static class BillingAppHelpers
    static BillingOptions AutoDetectFromApp(IAppBase app, string defaultAppId = "app")
    static string GetSecretOrEnv(IAppBase app, string key)
  sealed class BillingCatalogSync
    ctor(BillingService billing)
    Task<BillingPlanCatalogMap> SyncAsync(IReadOnlyList<BillingPlanSpec> plans, CancellationToken cancellationToken = null)
    Task<BillingPlanCatalogMap> SyncFromCatalogClassAsync(Type catalogClass, CancellationToken cancellationToken = null)
  sealed class BillingCharge : IEquatable<BillingCharge>
    ctor(string Id, string? PaymentIntentId, string? CustomerId, long AmountMinor, long AmountRefundedMinor, string Currency, string Status, bool Paid, bool Refunded, DateTimeOffset Created, string? Description, string? ReceiptUrl)
    long AmountMinor { get; init; }
    long AmountRefundedMinor { get; init; }
    DateTimeOffset Created { get; init; }
    string Currency { get; init; }
    string CustomerId { get; init; }
    string Description { get; init; }
    string Id { get; init; }
    bool Paid { get; init; }
    string PaymentIntentId { get; init; }
    string ReceiptUrl { get; init; }
    bool Refunded { get; init; }
    string Status { get; init; }
  sealed class BillingChargeCreditsAttribute : PolicyAttribute
    ctor(string sku, int credits = 1)
    int Credits { get; }
    string Sku { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class BillingCheckoutOffer : IEquatable<BillingCheckoutOffer>
    ctor(bool AlreadyEntitled, string? SessionId, string? Url)
    bool AlreadyEntitled { get; init; }
    string SessionId { get; init; }
    string Url { get; init; }
  sealed class BillingCheckoutResult : IEquatable<BillingCheckoutResult>
    ctor(string SessionId, string Url)
    string SessionId { get; init; }
    string Url { get; init; }
  sealed class BillingConnectAccount : IEquatable<BillingConnectAccount>
    ctor(string Id, bool DetailsSubmitted, bool ChargesEnabled, bool PayoutsEnabled, IReadOnlyList<string> RequirementsCurrentlyDue, IReadOnlyList<string> RequirementsEventuallyDue, string? RequirementsDisabledReason, string? Country = null)
    bool ChargesEnabled { get; init; }
    string Country { get; init; }
    bool DetailsSubmitted { get; init; }
    string Id { get; init; }
    bool PayoutsEnabled { get; init; }
    IReadOnlyList<string> RequirementsCurrentlyDue { get; init; }
    string RequirementsDisabledReason { get; init; }
    IReadOnlyList<string> RequirementsEventuallyDue { get; init; }
  sealed class BillingConnectFunctionHost
    ctor(BillingConnectService connect, Func<string?> connectedAccountIdGetter, Func<BillingConnectAccount, Task>? onStatusRefresh = null)
    Task<string> FetchConnectManagementSecretAsync()
    Task<string> FetchConnectOnboardingSecretAsync()
    Task OnConnectOnboardingExitAsync()
  sealed class BillingConnectService
    ctor(BillingOptions options)
    static BillingConnectService Current { get; }
    Task<BillingAccountSession> CreateAccountSessionAsync(BillingAccountSessionRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingWebhookEndpoint> CreateConnectWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateExpressAccountAsync(string email, string country, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, IEnumerable<string>? requestedCapabilities = null, CancellationToken cancellationToken = null)
    Task<string> CreateLoginLinkAsync(string connectedAccountId, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateOnboardingLinkAsync(string connectedAccountId, string refreshUrl, string returnUrl, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingConnectAccount> RetrieveAccountAsync(string connectedAccountId, CancellationToken cancellationToken = null)
    Task<string> TransferAsync(string connectedAccountId, long amountMinor, string currency, string idempotencyKey, CancellationToken cancellationToken = null)
  enum BillingCouponDuration
    Once
    Forever
    Repeating
  sealed class BillingCouponInfo : IEquatable<BillingCouponInfo>
    ctor()
    long? AmountOffMinor { get; init; }
    string Currency { get; init; }
    BillingCouponDuration Duration { get; init; }
    int? DurationInMonths { get; init; }
    string Id { get; init; }
    int? MaxRedemptions { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Name { get; init; }
    decimal? PercentOff { get; init; }
    DateTimeOffset? RedeemBy { get; init; }
  sealed class BillingCreditNote : IEquatable<BillingCreditNote>
    ctor(string Id, string Number, string Status, long AmountMinor, string? PdfUrl)
    long AmountMinor { get; init; }
    string Id { get; init; }
    string Number { get; init; }
    string PdfUrl { get; init; }
    string Status { get; init; }
  sealed class BillingCreditNoteInfo : IEquatable<BillingCreditNoteInfo>
    long? AmountMinor { get; init; }
    long? CreditAmountMinor { get; init; }
    string InvoiceId { get; init; }
    string Memo { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Reason { get; init; }
    long? RefundAmountMinor { get; init; }
  sealed class BillingCustomerInfo : IEquatable<BillingCustomerInfo>
    ctor()
    string AddressCity { get; init; }
    string AddressCountry { get; init; }
    string AddressLine1 { get; init; }
    string AddressLine2 { get; init; }
    string AddressPostalCode { get; init; }
    string AddressState { get; init; }
    string Description { get; init; }
    string Email { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Name { get; init; }
    string Phone { get; init; }
    string PreferredLocales { get; init; }
    BillingTaxExempt? TaxExempt { get; init; }
  sealed class BillingDestination : IEquatable<BillingDestination>
    ctor(string ConnectedAccountId, long? ApplicationFeeAmountMinor = null, decimal? ApplicationFeePercent = null)
    long? ApplicationFeeAmountMinor { get; init; }
    decimal? ApplicationFeePercent { get; init; }
    string ConnectedAccountId { get; init; }
  sealed class BillingEmbeddedCheckout : IEquatable<BillingEmbeddedCheckout>
    ctor(string SessionId, string ClientSecret)
    string ClientSecret { get; init; }
    string SessionId { get; init; }
  sealed class BillingEntitlement : IEquatable<BillingEntitlement>
    ctor(string PlanId, bool SubscriptionActive, DateTimeOffset? SubscriptionEndsAt, bool CancelAtPeriodEnd, string? SubscriptionStatus, bool UnlockGranted, DateTimeOffset? UnlockGrantedAt, int CreditsRemaining, DateTimeOffset? LastPurchaseAt)
    bool CancelAtPeriodEnd { get; init; }
    int CreditsRemaining { get; init; }
    DateTimeOffset? LastPurchaseAt { get; init; }
    string PlanId { get; init; }
    bool SubscriptionActive { get; init; }
    DateTimeOffset? SubscriptionEndsAt { get; init; }
    string SubscriptionStatus { get; init; }
    bool UnlockGranted { get; init; }
    DateTimeOffset? UnlockGrantedAt { get; init; }
  sealed class BillingEvent : IEquatable<BillingEvent>
    ctor(string EventId, BillingEventType Type, string? CustomerId, string? SubscriptionId, string? ClientReferenceId, string? PlanId, string? Status, DateTimeOffset? CurrentPeriodStart, DateTimeOffset? CurrentPeriodEnd, long? AmountPaid, string? Currency, JsonElement RawPayload)
    long? AmountPaid { get; init; }
    string ClientReferenceId { get; init; }
    string Currency { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodStart { get; init; }
    string CustomerId { get; init; }
    string EventId { get; init; }
    string PlanId { get; init; }
    JsonElement RawPayload { get; init; }
    string Status { get; init; }
    string SubscriptionId { get; init; }
    BillingEventType Type { get; init; }
  enum BillingEventType
    Unknown
    CheckoutCompleted
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
  sealed class BillingInvoice : IEquatable<BillingInvoice>
    ctor(string Id, string? HostedInvoiceUrl, string? InvoicePdfUrl, string Status)
    string HostedInvoiceUrl { get; init; }
    string Id { get; init; }
    string InvoicePdfUrl { get; init; }
    string Status { get; init; }
  sealed class BillingInvoiceSummary : IEquatable<BillingInvoiceSummary>
    ctor(string Id, string? CustomerId, string? SubscriptionId, long AmountDueMinor, long AmountPaidMinor, string Currency, string Status, DateTimeOffset Created, DateTimeOffset? DueDate, string? HostedInvoiceUrl, string? InvoicePdfUrl)
    long AmountDueMinor { get; init; }
    long AmountPaidMinor { get; init; }
    DateTimeOffset Created { get; init; }
    string Currency { get; init; }
    string CustomerId { get; init; }
    DateTimeOffset? DueDate { get; init; }
    string HostedInvoiceUrl { get; init; }
    string Id { get; init; }
    string InvoicePdfUrl { get; init; }
    string Status { get; init; }
    string SubscriptionId { get; init; }
  sealed class BillingLineItem : IEquatable<BillingLineItem>
    ctor()
    long? AdHocAmountMinor { get; init; }
    string AdHocCurrency { get; init; }
    string AdHocProductName { get; init; }
    bool AdHocRecurring { get; init; }
    string AdHocRecurringInterval { get; init; }
    string PriceId { get; init; }
    long Quantity { get; init; }
    static BillingLineItem Dynamic(long amountMinor, string currency, string productName, long quantity = 1)
    static BillingLineItem ForPrice(string priceId, long quantity = 1)
  enum BillingMode
    Subscription
    OneTime
  sealed class BillingOptions : IEquatable<BillingOptions>
    ctor()
    string ApiKey { get; init; }
    string ApiVersion { get; init; }
    bool AutomaticTax { get; init; }
    bool CollectTaxId { get; init; }
    string ConnectedAccountId { get; init; }
    string DefaultCancelUrl { get; init; }
    IReadOnlyDictionary<string, string> DefaultMetadata { get; init; }
    string DefaultPortalReturnUrl { get; init; }
    string DefaultSuccessUrl { get; init; }
    string IkonAppId { get; init; }
    string IkonBackendUrl { get; init; }
    string IkonWebhookSecret { get; init; }
    int MaxRetryAttempts { get; init; }
    long? PlatformApplicationFeeAmountMinor { get; init; }
    decimal? PlatformApplicationFeePercent { get; init; }
    BillingProvider Provider { get; init; }
    TimeSpan? RequestTimeout { get; init; }
    TimeSpan RetryBaseDelay { get; init; }
    string WebhookSecret { get; init; }
  sealed class BillingPage<T> : IEquatable<BillingPage<T>>
    ctor(IReadOnlyList<T> Items, bool HasMore, string? LastId)
    bool HasMore { get; init; }
    IReadOnlyList<T> Items { get; init; }
    string LastId { get; init; }
  sealed class BillingPaymentIntent : IEquatable<BillingPaymentIntent>
    ctor(string Id, string ClientSecret, string Status)
    string ClientSecret { get; init; }
    string Id { get; init; }
    string Status { get; init; }
  sealed class BillingPaymentLink : IEquatable<BillingPaymentLink>
    ctor(string Id, string Url)
    string Id { get; init; }
    string Url { get; init; }
  sealed class BillingPaymentMethod : IEquatable<BillingPaymentMethod>
    ctor(string Id, string Type, string? CardBrand, string? CardLast4, int? CardExpMonth, int? CardExpYear)
    string CardBrand { get; init; }
    int? CardExpMonth { get; init; }
    int? CardExpYear { get; init; }
    string CardLast4 { get; init; }
    string Id { get; init; }
    string Type { get; init; }
  sealed class BillingPlanCatalogMap
    IEnumerable<string> AppPlanIds { get; }
    int Count { get; }
    bool Contains(string appPlanId)
    string GetPriceId(string appPlanId)
    IReadOnlyDictionary<string, string> ToDictionary()
    bool TryGetPriceId(string appPlanId, out string priceId)
  sealed class BillingPlanDescriptor : IEquatable<BillingPlanDescriptor>
    ctor(string PlanId, string StripePriceId, BillingMode Mode, string? MeteredPriceId = null, long Quantity = 1, IReadOnlyDictionary<string, string>? Metadata = null, int? TrialPeriodDays = null, bool AllowPromotionCodes = false)
    bool AllowPromotionCodes { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string MeteredPriceId { get; init; }
    BillingMode Mode { get; init; }
    string PlanId { get; init; }
    long Quantity { get; init; }
    string StripePriceId { get; init; }
    int? TrialPeriodDays { get; init; }
    static BillingPlanDescriptor Credits(string planId, string stripePriceId, int creditsGranted, IReadOnlyDictionary<string, string>? metadata = null)
    static BillingPlanDescriptor Subscription(string planId, string stripePriceId, int trialPeriodDays = 0, bool allowPromotionCodes = false, long quantity = 1, string? meteredPriceId = null, IReadOnlyDictionary<string, string>? metadata = null)
    static BillingPlanDescriptor Unlock(string planId, string stripePriceId, long quantity = 1, IReadOnlyDictionary<string, string>? metadata = null)
  sealed class BillingPlanSpec : IEquatable<BillingPlanSpec>
    ctor(string AppPlanId, string ProductName, long UnitAmountMinor, string Currency, string? Interval, int? IntervalCount = null, string? Description = null, string? Nickname = null, IReadOnlyDictionary<string, string>? Metadata = null, string? LookupKeyOverride = null)
    string AppPlanId { get; init; }
    string Currency { get; init; }
    string Description { get; init; }
    string Interval { get; init; }
    int? IntervalCount { get; init; }
    string LookupKeyOverride { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Nickname { get; init; }
    string ProductName { get; init; }
    long UnitAmountMinor { get; init; }
    static BillingPlanSpec Credits(string appPlanId, string productName, long unitAmountMinor, string currency, int creditsGranted, string? description = null)
    static BillingPlanSpec Subscription(string appPlanId, string productName, long unitAmountMinor, string currency, string interval, int? intervalCount = null, string? description = null)
    static BillingPlanSpec Unlock(string appPlanId, string productName, long unitAmountMinor, string currency, string? description = null)
  sealed class BillingPortalConfigurationInfo : IEquatable<BillingPortalConfigurationInfo>
    ctor()
    bool AllowCustomerUpdate { get; init; }
    bool AllowInvoiceHistory { get; init; }
    bool AllowPaymentMethodUpdate { get; init; }
    bool AllowSubscriptionCancel { get; init; }
    bool AllowSubscriptionPause { get; init; }
    string BusinessProfileHeadline { get; init; }
    string PrivacyPolicyUrl { get; init; }
    string SubscriptionCancelMode { get; init; }
    string TermsOfServiceUrl { get; init; }
  sealed class BillingPortalResult : IEquatable<BillingPortalResult>
    ctor(string SessionId, string Url)
    string SessionId { get; init; }
    string Url { get; init; }
  sealed class BillingPrice : IEquatable<BillingPrice>
    ctor(string Id, string ProductId, long UnitAmountMinor, string Currency, string? RecurringInterval, bool Active, string? LookupKey = null)
    bool Active { get; init; }
    string Currency { get; init; }
    string Id { get; init; }
    string LookupKey { get; init; }
    string ProductId { get; init; }
    string RecurringInterval { get; init; }
    long UnitAmountMinor { get; init; }
  sealed class BillingPriceInfo : IEquatable<BillingPriceInfo>
    bool Active { get; init; }
    string Currency { get; init; }
    string LookupKey { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Nickname { get; init; }
    string ProductId { get; init; }
    string RecurringInterval { get; init; }
    int? RecurringIntervalCount { get; init; }
    bool TransferLookupKey { get; init; }
    long UnitAmountMinor { get; init; }
  sealed class BillingProduct : IEquatable<BillingProduct>
    ctor(string Id, string Name, bool Active, string? Description)
    bool Active { get; init; }
    string Description { get; init; }
    string Id { get; init; }
    string Name { get; init; }
  sealed class BillingProductInfo : IEquatable<BillingProductInfo>
    bool Active { get; init; }
    string Description { get; init; }
    string Id { get; init; }
    IReadOnlyList<string> Images { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Name { get; init; }
    string StatementDescriptor { get; init; }
  enum BillingProvider
    Disabled
    Byok
    IkonConnect
  sealed class BillingRequireSubscriptionAttribute : PolicyAttribute
    ctor(string planId)
    string PlanId { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class BillingRequireUnlockAttribute : PolicyAttribute
    ctor(string planId)
    string PlanId { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class BillingService
    ctor(BillingOptions options, IBillingAppAdapter adapter)
    IBillingCreditStore CreditStore { get; set; }
    static BillingService Current { get; }
    Task AddInvoiceItemAsync(string stripeCustomerId, long amountMinor, string currency, string description, string? subscriptionId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task AdjustCustomerBalanceAsync(string stripeCustomerId, long amountMinorDelta, string currency, string description, string idempotencyKey, CancellationToken cancellationToken = null)
    Task<BillingPaymentIntent> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = null)
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, CancellationToken cancellationToken = null)
    Task CancelSubscriptionScheduleAsync(string scheduleId, CancellationToken cancellationToken = null)
    Task<BillingPaymentIntent> CapturePaymentIntentAsync(string paymentIntentId, long? amountToCaptureMinor = null, CancellationToken cancellationToken = null)
    Task<BillingCheckoutResult> CreateCartCheckoutAsync(IEnumerable<BillingLineItem> lines, BillingMode mode, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingCheckoutResult> CreateCheckoutAsync(string planId, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateCouponAsync(BillingCouponInfo coupon, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingCreditNote> CreateCreditNoteAsync(BillingCreditNoteInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateCustomerAsync(BillingCustomerInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingTaxId> CreateCustomerTaxIdAsync(string stripeCustomerId, string type, string value, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingEmbeddedCheckout> CreateEmbeddedCheckoutAsync(string planId, string? appCustomerKey, string? email, string returnUrl, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingInvoice> CreateHostedInvoiceAsync(string stripeCustomerId, IEnumerable<BillingLineItem> lines, int daysUntilDue, bool autoSend = true, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingPaymentIntent> CreatePaymentIntentAsync(long amountMinor, string currency, string? stripeCustomerId = null, string captureMethod = "automatic", string? paymentMethodId = null, bool confirm = false, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingPaymentLink> CreatePaymentLinkAsync(IEnumerable<BillingLineItem> lines, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingPortalResult> CreatePortalAsync(string stripeCustomerId, string? returnUrl = null, CancellationToken cancellationToken = null)
    Task<string> CreatePortalConfigurationAsync(BillingPortalConfigurationInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreatePriceAsync(BillingPriceInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateProductAsync(BillingProductInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreatePromotionCodeAsync(string couponId, string code, DateTimeOffset? expiresAt = null, long? maxRedemptions = null, string? restrictedToCustomerId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingSetupIntent> CreateSetupIntentAsync(string stripeCustomerId, string usage = "off_session", string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateSubscriptionScheduleAsync(string stripeCustomerId, IEnumerable<BillingSubscriptionPhase> phases, DateTimeOffset? startDate = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingCheckoutResult> CreateTipCheckoutAsync(long amountMinor, string currency, string? title = null, string? message = null, string? appCustomerKey = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingWebhookEndpoint> CreateWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task DeleteCustomerTaxIdAsync(string stripeCustomerId, string taxIdId, CancellationToken cancellationToken = null)
    Task DeleteWebhookEndpointAsync(string webhookEndpointId, CancellationToken cancellationToken = null)
    Task DetachPaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = null)
    Task<BillingEntitlement> GetEntitlementAsync(string planId, string appCustomerKey, IBillingCreditStore? creditStore = null, CancellationToken cancellationToken = null)
    Task<BillingWebhookResult> HandleIkonWebhookAsync(string? signatureHeader, string body, TimeSpan? tolerance = null, CancellationToken cancellationToken = null)
    Task<BillingWebhookResult> HandleWebhookAsync(string? signatureHeader, string body, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListApplePayDomainsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingCharge>> ListChargesAsync(string? stripeCustomerId = null, int limit = 100, DateTimeOffset? createdAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCouponsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCreditNotesAsync(string? invoiceId = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCustomerTaxIdsAsync(string stripeCustomerId, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListEventIdsAsync(string? type = null, DateTimeOffset? createdAfter = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingInvoiceSummary>> ListInvoicesAsync(string? stripeCustomerId = null, string? subscriptionId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPaymentLinksAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingPaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card", int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingPrice>> ListPricesAsync(string? productId = null, bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    Task<BillingPage<BillingPrice>> ListPricesPageAsync(string? productId = null, bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingProduct>> ListProductsAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    Task<BillingPage<BillingProduct>> ListProductsPageAsync(bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPromotionCodesAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingSubscription>> ListSubscriptionsAsync(string? stripeCustomerId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListWebhookEndpointsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<BillingCheckoutOffer> OfferCheckoutAsync(string planId, string appCustomerKey, string? email = null, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task PauseSubscriptionAsync(string subscriptionId, string behavior = "void", CancellationToken cancellationToken = null)
    Task<BillingUpcomingInvoice> PreviewUpcomingInvoiceAsync(string stripeCustomerId, string? subscriptionId = null, string? newPriceId = null, long? newQuantity = null, string? couponId = null, CancellationToken cancellationToken = null)
    Task RefundAsync(string paymentIntentId, long? amountMinor, string? reason, string idempotencyKey, CancellationToken cancellationToken = null)
    Task<string> RegisterApplePayDomainAsync(string domainName, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task ReportUsageAsync(string meterEventName, string stripeCustomerId, long value, string idempotencyKey, DateTimeOffset? timestamp = null, CancellationToken cancellationToken = null)
    Task ResumeCanceledSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    Task ResumeSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    Task<BillingEvent> RetrieveEventAsync(string eventId, CancellationToken cancellationToken = null)
    Task<BillingPrice> RetrievePriceByLookupKeyAsync(string lookupKey, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersByAppKeyAsync(string appCustomerKey, string metadataKey = "app_customer_key", int limit = 1, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersDetailedAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    Task UpdateCustomerAsync(string stripeCustomerId, BillingCustomerInfo info, CancellationToken cancellationToken = null)
    Task UpdatePriceAsync(string priceId, bool? active = null, string? nickname = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    Task UpdateProductAsync(string productId, bool? active = null, string? name = null, string? description = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    Task UpdateSubscriptionItemQuantityAsync(string subscriptionItemId, long quantity, bool prorate = true, CancellationToken cancellationToken = null)
    Task UpdateSubscriptionPriceAsync(string subscriptionItemId, string newPriceId, bool prorate = true, CancellationToken cancellationToken = null)
    Task UpdateSubscriptionScheduleAsync(string scheduleId, IEnumerable<BillingSubscriptionPhase> phases, CancellationToken cancellationToken = null)
    Task VoidCreditNoteAsync(string creditNoteId, CancellationToken cancellationToken = null)
  sealed class BillingSetupIntent : IEquatable<BillingSetupIntent>
    ctor(string Id, string ClientSecret)
    string ClientSecret { get; init; }
    string Id { get; init; }
  sealed class BillingSubscription : IEquatable<BillingSubscription>
    ctor(string Id, string CustomerId, string Status, DateTimeOffset? CurrentPeriodStart, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd, string? DefaultPaymentMethodId, string? LatestInvoiceId, IReadOnlyList<string> ItemIds)
    bool CancelAtPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodStart { get; init; }
    string CustomerId { get; init; }
    string DefaultPaymentMethodId { get; init; }
    string Id { get; init; }
    IReadOnlyList<string> ItemIds { get; init; }
    string LatestInvoiceId { get; init; }
    string Status { get; init; }
  sealed class BillingSubscriptionPhase : IEquatable<BillingSubscriptionPhase>
    ctor(string StripePriceId, long Quantity = 1, int? Iterations = null)
    int? Iterations { get; init; }
    long Quantity { get; init; }
    string StripePriceId { get; init; }
  enum BillingTaxExempt
    None
    Exempt
    Reverse
  sealed class BillingTaxId : IEquatable<BillingTaxId>
    ctor(string Id, string Type, string Value, string? Country)
    string Country { get; init; }
    string Id { get; init; }
    string Type { get; init; }
    string Value { get; init; }
  sealed class BillingUpcomingInvoice : IEquatable<BillingUpcomingInvoice>
    ctor(long AmountDueMinor, long AmountPaidMinor, long SubtotalMinor, long TotalMinor, long? TotalDiscountAmountMinor, long? TaxMinor, string Currency, DateTimeOffset? PeriodStart, DateTimeOffset? PeriodEnd, DateTimeOffset? NextPaymentAttempt, IReadOnlyList<BillingUpcomingInvoiceLine> Lines)
    long AmountDueMinor { get; init; }
    long AmountPaidMinor { get; init; }
    string Currency { get; init; }
    IReadOnlyList<BillingUpcomingInvoiceLine> Lines { get; init; }
    DateTimeOffset? NextPaymentAttempt { get; init; }
    DateTimeOffset? PeriodEnd { get; init; }
    DateTimeOffset? PeriodStart { get; init; }
    long SubtotalMinor { get; init; }
    long? TaxMinor { get; init; }
    long? TotalDiscountAmountMinor { get; init; }
    long TotalMinor { get; init; }
  sealed class BillingUpcomingInvoiceLine : IEquatable<BillingUpcomingInvoiceLine>
    ctor(string? PriceId, string Description, long AmountMinor, string Currency, long Quantity, bool Proration)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    string Description { get; init; }
    string PriceId { get; init; }
    bool Proration { get; init; }
    long Quantity { get; init; }
  sealed class BillingWebhookEndpoint : IEquatable<BillingWebhookEndpoint>
    ctor(string Id, string Url, string? Secret, string Status)
    string Id { get; init; }
    string Secret { get; init; }
    string Status { get; init; }
    string Url { get; init; }
  sealed class BillingWebhookFunctionHost
    ctor(BillingService billing)
    Task<string> StripeWebhook(Dictionary<string, string> queryParams, Dictionary<string, string> headers, string body)
  sealed class BillingWebhookResult : IEquatable<BillingWebhookResult>
    ctor(bool Verified, string? Reason, BillingEvent? Event, string? AdapterError = null)
    string AdapterError { get; init; }
    BillingEvent Event { get; init; }
    string Reason { get; init; }
    bool Verified { get; init; }
  interface IBillingAppAdapter
    abstract Task ApplyEventAsync(BillingEvent evt, CancellationToken cancellationToken)
    abstract Task<BillingPlanDescriptor> GetPlanAsync(string planId, CancellationToken cancellationToken)
    abstract Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken cancellationToken)
  interface IBillingConnectAccountStore
    abstract Task ClearAsync(CancellationToken cancellationToken = null)
    abstract Task<string> GetAsync(CancellationToken cancellationToken = null)
    abstract Task SetAsync(string connectedAccountId, CancellationToken cancellationToken = null)
  interface IBillingCreditStore
    abstract Task<int> DeductAsync(string appCustomerKey, string sku, int credits, string idempotencyKey, CancellationToken cancellationToken = null)
    abstract Task<int> GetCreditsAsync(string appCustomerKey, string sku, CancellationToken cancellationToken = null)
    abstract Task<int> GrantAsync(string appCustomerKey, string sku, int credits, string idempotencyKey, CancellationToken cancellationToken = null)

namespace Ikon.App.Cells
  sealed class CellAttribute : Attribute
    ctor()
    int Capacity { get; init; }
    int IdleTtlSeconds { get; init; }
    CellProcessScope ProcessScope { get; init; }
  sealed class CellConnectRequest : IEquatable<CellConnectRequest>
    ctor(string CellTypeName, IReadOnlyDictionary<string, string> Identity)
    string CellTypeName { get; init; }
    IReadOnlyDictionary<string, string> Identity { get; init; }
  sealed class CellConnection : IAsyncDisposable
    IkonClient Client { get; }
    ReactiveRegistry Reactive { get; }
    ValueTask DisposeAsync()
  sealed class CellHost : IAsyncDisposable
    ctor(IEnumerable<Assembly>? assemblies = null)
    IReadOnlyCollection<Type> CellTypes { get; }
    ValueTask DisposeAsync()
    Task<int> EvictIdleAsync()
    Task<int> EvictIdleOlderThanAsync(DateTime cutoffUtc)
    static Type GetSessionIdentityType(Type cellType)
    static bool HasIdentityParameters(Type identityType)
    void RegisterSingleton(object instance)
    TInterface Resolve<TInterface>(object sessionIdentity)
    object ResolveByCellTypeName(string cellTypeName, IReadOnlyDictionary<string, string> sessionIdentityFields)
    bool TryGetCellTypeForInterface(Type iface, out Type cellType)
    event Action TopologyChanged
  static class CellNaming
    static string SdkFunctionName(Type cellType, string methodName)
    static string WebhookFunctionName(Type cellType, string methodName)
    static string CellEndpointBaseUrlFunctionName
  enum CellProcessScope
    AppProcess
    Substrate
  static class Cells
    static CellHost Current { get; }
    static TInterface Connect<TInterface>(object sessionIdentity)
    static ValueTask DisposeAsync()
    static void Initialize(CellHost host)
    static void SetCellClientFactory(Func<CellConnectRequest, Task<IkonClient>> factory)
    static void SetWebhookUrlResolver(Func<string, string?> resolver)
    static string CellTypeParam
  interface ICell<TSessionIdentity>
    TSessionIdentity Identity { get; }
  sealed class McpResourceAttribute : Attribute
    ctor(string uriTemplate)
    string Description { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    string UriTemplate { get; }
  sealed class McpToolAttribute : Attribute
    ctor()
    string Description { get; init; }
    string Name { get; init; }
  class SubstrateCellProxy<TInterface> : DispatchProxy where TInterface : class
    ctor()
    static TInterface Create(Type cellType, object sessionIdentity, Func<string, string?> webhookUrlResolver)

namespace Ikon.App.Mcp
  sealed class CallToolParams : IEquatable<CallToolParams>
    ctor()
    JsonElement Arguments { get; init; }
    string Name { get; init; }
  sealed class CallToolResult : IEquatable<CallToolResult>
    ctor(IReadOnlyList<ToolContent> Content, bool IsError)
    IReadOnlyList<ToolContent> Content { get; init; }
    bool IsError { get; init; }
  sealed class CancelledNotificationParams : IEquatable<CancelledNotificationParams>
    ctor(JsonElement RequestId, string? Reason = null)
    string Reason { get; init; }
    JsonElement RequestId { get; init; }
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
  sealed class JsonRpcRequest : IEquatable<JsonRpcRequest>
    ctor()
    JsonElement? Id { get; init; }
    bool IsNotification { get; }
    string JsonRpc { get; init; }
    string Method { get; init; }
    JsonElement? Params { get; init; }
  sealed class JsonRpcResponse : IEquatable<JsonRpcResponse>
    ctor()
    JsonRpcError Error { get; init; }
    JsonElement? Id { get; init; }
    string JsonRpc { get; init; }
    object Result { get; init; }
    static JsonRpcResponse Fail(JsonElement? id, int code, string message)
    static JsonRpcResponse Ok(JsonElement? id, object? result)
  sealed class ListResourceTemplatesResult : IEquatable<ListResourceTemplatesResult>
    ctor(IReadOnlyList<ResourceTemplate> ResourceTemplates)
    string NextCursor { get; init; }
    IReadOnlyList<ResourceTemplate> ResourceTemplates { get; init; }
  sealed class ListResourcesResult : IEquatable<ListResourcesResult>
    ctor(IReadOnlyList<Resource> Resources)
    string NextCursor { get; init; }
    IReadOnlyList<Resource> Resources { get; init; }
  sealed class ListToolsParams : IEquatable<ListToolsParams>
    ctor()
    string Cursor { get; init; }
  sealed class ListToolsResult : IEquatable<ListToolsResult>
    ctor(IReadOnlyList<ToolDefinition> Tools)
    string NextCursor { get; init; }
    IReadOnlyList<ToolDefinition> Tools { get; init; }
  sealed class McpCallContext : IEquatable<McpCallContext>
    ctor(CancellationToken CancellationToken, Func<ProgressUpdate, Task>? OnProgress, IReadOnlyDictionary<string, string>? SessionIdentityFields = null)
    CancellationToken CancellationToken { get; init; }
    static McpCallContext Current { get; }
    Func<ProgressUpdate, Task> OnProgress { get; init; }
    IReadOnlyDictionary<string, string> SessionIdentityFields { get; init; }
    static IDisposable Use(McpCallContext context)
  sealed class McpCapabilities : IEquatable<McpCapabilities>
    ctor(McpToolsCapability? Tools = null, McpResourcesCapability? Resources = null)
    McpResourcesCapability Resources { get; init; }
    McpToolsCapability Tools { get; init; }
  static class McpErrorCode
    static int GovernanceDenied
    static int GovernanceEscalated
    static int InternalError
    static int InvalidParams
    static int InvalidRequest
    static int MethodNotFound
    static int ParseError
  sealed class McpHost
    ctor(string serverName = "ikon-mcp", string serverVersion = "0.1.0", string protocolVersion = "2024-11-05")
    IReadOnlyCollection<McpResourceHandler> Resources { get; }
    McpServerInfo ServerInfo { get; }
    IReadOnlyCollection<McpToolHandler> Tools { get; }
    Task<JsonRpcResponse> HandleRequestAsync(JsonRpcRequest request, CancellationToken ct = null, IReadOnlyDictionary<string, string>? sessionIdentityFields = null, IMcpNotificationSink? perRequestSink = null)
    McpHost RegisterResource(McpResourceHandler resource)
    McpHost RegisterTool(McpToolHandler handler)
    void SetNotificationSink(IMcpNotificationSink sink)
  static class McpHttpTransport
    static Task HandlePostAsync(HttpContext context, McpHost mcp, IReadOnlyDictionary<string, string>? sessionIdentityFields = null)
    static Task HandleProtectedResourceDiscoveryAsync(HttpContext context)
  static class McpJson
    static T Deserialize<T>(string json)
    static T DeserializeParams<T>(JsonElement? element)
    static string Serialize<T>(T value)
    static JsonSerializerOptions Options
  static class McpResourceBridge
    static McpResourceHandler BuildHandler(CellHost cellHost, McpResourceInfo info)
  static class McpResourceDiscovery
    static IReadOnlyList<McpResourceInfo> ForType(Type ownerType)
    static IReadOnlyList<McpResourceInfo> ForTypes(IEnumerable<Type> types)
  sealed class McpResourceHandler : IEquatable<McpResourceHandler>
    ctor(string DisplayName, string Description, string MimeType, string UriTemplate, bool IsStatic, Func<string, IReadOnlyDictionary<string, string>?> TryMatch, Func<string, IReadOnlyDictionary<string, string>, CancellationToken, Task<ResourceContents>> Read)
    string Description { get; init; }
    string DisplayName { get; init; }
    bool IsStatic { get; init; }
    string MimeType { get; init; }
    Func<string, IReadOnlyDictionary<string, string>, CancellationToken, Task<ResourceContents>> Read { get; init; }
    string SubjectId { get; init; }
    Func<string, IReadOnlyDictionary<string, string>> TryMatch { get; init; }
    string UriTemplate { get; init; }
  sealed class McpResourceInfo : IEquatable<McpResourceInfo>
    ctor(string DisplayName, string Description, string MimeType, UriTemplate UriTemplate, MethodInfo Handler, Type OwnerCellType)
    string Description { get; init; }
    string DisplayName { get; init; }
    MethodInfo Handler { get; init; }
    bool IsStatic { get; }
    string MimeType { get; init; }
    Type OwnerCellType { get; init; }
    string SubjectId { get; }
    UriTemplate UriTemplate { get; init; }
  sealed class McpResourcesCapability : IEquatable<McpResourcesCapability>
    ctor()
  sealed class McpServerInfo : IEquatable<McpServerInfo>
    ctor(string Name, string Version)
    string Name { get; init; }
    string Version { get; init; }
  static class McpToolBridge
    static McpToolHandler BuildHandler(CellHost cellHost, McpToolInfo info)
  static class McpToolDiscovery
    static IReadOnlyList<McpToolInfo> ForType(Type ownerType)
    static IReadOnlyList<McpToolInfo> ForTypes(IEnumerable<Type> types)
  sealed class McpToolHandler : IEquatable<McpToolHandler>
    ctor(string Name, string Description, JsonElement InputSchema, Func<JsonElement, CancellationToken, Task<string>> Invoke)
    string Description { get; init; }
    JsonElement InputSchema { get; init; }
    Func<JsonElement, CancellationToken, Task<string>> Invoke { get; init; }
    string Name { get; init; }
    JsonElement? OutputSchema { get; init; }
    string SubjectId { get; init; }
  sealed class McpToolInfo : IEquatable<McpToolInfo>
    ctor(string Name, string Description, MethodInfo Handler, Type OwnerCellType)
    string Description { get; init; }
    MethodInfo Handler { get; init; }
    string Name { get; init; }
    Type OwnerCellType { get; init; }
    string SubjectId { get; }
  sealed class McpToolsCapability : IEquatable<McpToolsCapability>
    ctor()
  sealed class ProgressNotificationParams : IEquatable<ProgressNotificationParams>
    ctor(JsonElement ProgressToken, double Progress, double? Total = null, string? Message = null)
    string Message { get; init; }
    double Progress { get; init; }
    JsonElement ProgressToken { get; init; }
    double? Total { get; init; }
  sealed class ProgressUpdate : IEquatable<ProgressUpdate>
    ctor(double Progress, double? Total = null, string? Message = null)
    string Message { get; init; }
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
    string Description { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    string Uri { get; init; }
  sealed class ResourceContents : IEquatable<ResourceContents>
    ctor(string Uri, string? MimeType = null, string? Text = null, string? Blob = null)
    string Blob { get; init; }
    string MimeType { get; init; }
    string Text { get; init; }
    string Uri { get; init; }
  sealed class ResourceTemplate : IEquatable<ResourceTemplate>
    ctor(string UriTemplate, string Name, string? Description = null, string? MimeType = null)
    string Description { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    string UriTemplate { get; init; }
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
    JsonElement? OutputSchema { get; init; }

# Ikon.Resonance Public API

namespace Ikon.Resonance
  struct AudioFrameEx
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId, TimeSpan totalDuration = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, IReadOnlyList<AudioAnalysisResult>? analysisResults = null, IReadOnlyList<AudioShapeSetDeclaration>? shapeSetDeclarations = null)
    IReadOnlyList<AudioAnalysisResult> AnalysisResults { get; }
    int ChannelCount { get; }
    AudioEncoderOptions EncoderOptions { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    int SampleRate { get; }
    ReadOnlyMemory<float> Samples { get; }
    IReadOnlyList<AudioShapeSetDeclaration> ShapeSetDeclarations { get; }
    string StreamId { get; }
    IReadOnlyList<int> TargetIds { get; }
    TimeSpan TotalDuration { get; }
  sealed class AudioGenerator
    ctor()
    bool IsRunning { get; }
    AudioGeneratorOptions Options { get; }
    void AddEffect(IAudioEffect effect)
    string AddSource(IAudioSource source)
    void ClearEffects()
    T GetSource<T>(string streamId)
    IEnumerable<ValueTuple<string, T>> GetSourcesOfType<T>()
    void RemoveEffectAt(int index)
    bool RemoveSource(string streamId)
    void ReplaceEffect(int index, IAudioEffect newEffect)
    Task StartAsync(Func<AudioGeneratorFrame, ValueTask> onFrame, Func<string, ValueTask>? onStreamEnd = null, CancellationToken cancellationToken = null)
    Task StopAsync()
    void UpdateOptions(Action<AudioGeneratorOptions> configure)
  struct AudioGeneratorFrame
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId)
    int ChannelCount { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    int SampleRate { get; }
    ReadOnlyMemory<float> Samples { get; }
    string StreamId { get; }
  sealed class AudioGeneratorOptions
    ctor()
    int BurstPacketCount { get; set; }
    double DriftFactor { get; set; }
    bool EnableBurstMode { get; set; }
    bool EnableDrift { get; set; }
    bool EnableJitter { get; set; }
    bool EnablePause { get; set; }
    int JitterMs { get; set; }
    int PauseDurationMs { get; set; }
    int PauseIntervalMs { get; set; }
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
    event Action Updated
  static class AudioResampler
    static int CalculateResampledFrameCount(int inputFrameCount, int inputSampleRate, int outputSampleRate)
    static void ConvertChannels(ReadOnlySpan<float> source, Span<float> destination, int inputChannelCount, int outputChannelCount)
    static bool IsSupportedChannelCount(int channelCount)
    static void Resample(ReadOnlySpan<float> source, Span<float> destination, int inputSampleRate, int outputSampleRate, int inputChannelCount, int outputChannelCount)
    static int MaxSupportedChannelCount
  sealed class AudioTimer
    ctor()
    void Reset()
    void WaitUntil(long targetTicks, CancellationToken token)
    Task WaitUntilAsync(long targetTicks, CancellationToken token)
  static class AudioUtils
    static int ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input, Span<byte> output)
    static byte[] ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input)
    static int ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input, Span<short> output)
    static short[] ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input)
    static int ConvertPcm16ToFloat(ReadOnlySpan<short> input, Span<float> output)
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<short> input)
    static int ConvertPcm16ToFloat(ReadOnlySpan<byte> input, Span<float> output)
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<byte> input)
  enum CrossfadeCurve
    Linear
    EqualPower
  enum FadeMode
    Sequential
    Crossfade
  sealed class GroupAudioMixer : IAsyncDisposable
    ctor(GroupAudioMixerConfig? config = null)
    void AddParticipant(string excludeKey)
    void AddStream(string streamId, string excludeKey)
    ValueTask DisposeAsync()
    void RemoveParticipant(string excludeKey)
    void RemoveStream(string streamId)
    Task StartAsync(Func<string, AudioFrameEx, ValueTask> onFrame, CancellationToken cancellationToken = null)
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  sealed class GroupAudioMixerConfig
    ctor()
    double MaxBufferSizeMs { get; set; }
  interface IAudioSource
    abstract void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
  enum WavFile.SampleFormat
    Short
    Float
  sealed class SilenceRemover
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = null)
    float[] ProcessChunk(ReadOnlySpan<float> chunk)
    void Reset()
  sealed class SilenceRemoverConfig
    ctor()
    float AttackAlpha { get; set; }
    float InitialNoiseFloor { get; set; }
    float MaxNoiseFloor { get; set; }
    float NoiseFloorAlpha { get; set; }
    float NoiseFloorMultiplier { get; set; }
    float NoiseFloorOffset { get; set; }
    int PreBufferMs { get; set; }
    float ReleaseAlpha { get; set; }
    int SpeechOnsetChunks { get; set; }
    int TrailingSilenceMs { get; set; }
  sealed class SpeechMixer : IAsyncDisposable
    ctor(SpeechMixerConfig? config = null)
    AudioEncoderOptions EncoderOptions { get; set; }
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
  sealed class SpeechMixerConfig
    ctor()
    CrossfadeCurve CrossfadeCurve { get; set; }
    double EndPaddingMs { get; set; }
    double FadeInMs { get; set; }
    FadeMode FadeMode { get; set; }
    double FadeOutMs { get; set; }
    double MaxBufferSizeMs { get; set; }
    double MaxPaddingTimeMs { get; set; }
    double PaddingThreshold { get; set; }
  class WavFile : IDisposable
    ctor(int sampleRate, int channelCount, WavFile.SampleFormat sampleFormat)
    void AddSamples(ReadOnlySpan<short> samples)
    void AddSamples(ReadOnlySpan<float> samples)
    byte[] AsArray()
    Stream AsStream()
    void Dispose()
    void SaveToFile(string filePath)

namespace Ikon.Resonance.Analysis
  struct AudioAnalysisResult
    uint SetId { get; set; }
    float[] Values { get; set; }
  struct AudioShapeSetDeclaration
    string Name { get; set; }
    uint SetId { get; set; }
    string[] ShapeNames { get; set; }
  interface IAudioAnalyzer
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    abstract IAudioAnalyzerInstance Create(int sampleRate, int channelCount)
  interface IAudioAnalyzerInstance
    abstract AudioAnalysisResult Analyze(ReadOnlySpan<float> buffer)
    abstract void Reset()
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
    ctor(float baseDelayMs, float depthMs, float rateHz, float mix, float stereoPhaseOffsetDegrees = 90)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class DelayAudioEffect : IAudioEffect
    ctor()
    ctor(float delayMs, float feedback, float mix, float feedbackDamping = 0.25)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  interface IAudioEffect
    abstract IAudioEffectInstance Create(int sampleRate, int channelCount)
  interface IAudioEffectInstance
    abstract void Process(Span<float> buffer)
    abstract void Reset()
  sealed class ReverbAudioEffect : IAudioEffect
    ctor()
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
    ctor(float rateHz, float depth, float mix, float stereoPhaseOffsetDegrees = 90)
    IAudioEffectInstance Create(int sampleRate, int channelCount)

namespace Ikon.Resonance.Synth
  sealed class DrumMachineSource : IAudioSource
    ctor(double bpm)
    double Bpm { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
  sealed class SineWaveSource : IAudioSource
    ctor(int frequencyIndex)
    int FrequencyIndex { get; }
    double FrequencyLeft { get; }
    double FrequencyRight { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)

namespace Ikon.Resonance.Synth.Envelopes
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
  enum EnvelopeStage
    Idle
    Attack
    Decay
    Sustain
    Release

namespace Ikon.Resonance.Synth.Filters
  sealed class MoogLadderFilter
    ctor()
    double Cutoff { get; set; }
    double Drive { get; set; }
    double Resonance { get; set; }
    double Process(double input)
    void Reset()
    void SetSampleRate(double sampleRate)

namespace Ikon.Resonance.Synth.Modulation
  sealed class Lfo
    ctor()
    double Phase { get; }
    double Rate { get; set; }
    LfoWaveform Waveform { get; set; }
    double Process()
    void Reset()
    void SetSampleRate(double sampleRate)
    void Sync()
  enum LfoWaveform
    Sine
    Triangle
    Saw
    Square
    SampleAndHold

namespace Ikon.Resonance.Synth.Moog
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
  static class MoogSynthPresets
    static MoogSynthPatch AcidLead()
    static MoogSynthPatch[] All()
    static MoogSynthPatch Brass()
    static MoogSynthPatch FatBass()
    static MoogSynthPatch FilterSweep()
    static MoogSynthPatch LushPad()
    static MoogSynthPatch Pluck()
  sealed class MoogSynthSource : IAudioSource
    ctor(MoogSynthPatch? patch = null)
    Sequencer Sequencer { get; }
    MoogSynth Synth { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
    void NextPattern()
    void SetPatch(MoogSynthPatch patch)
    void SetSequencerMode(SequencerMode mode)

namespace Ikon.Resonance.Synth.Oscillators
  interface IOscillator
    double Phase { get; }
    abstract double Process(double frequency, double sampleRate)
    abstract void Reset()
    abstract void Sync()
  enum OscillatorType
    Saw
    Square
    Triangle
    Pulse
    Sine
  static class PolyBlep
    static double Compute(double t, double dt)
  sealed class PulseOscillator : IOscillator
    ctor()
    double Phase { get; }
    double PulseWidth { get; set; }
    double Process(double frequency, double sampleRate, double pulseWidth)
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  sealed class SawOscillator : IOscillator
    ctor()
    double Phase { get; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  sealed class SquareOscillator : IOscillator
    ctor()
    double Phase { get; }
    double PulseWidth { get; set; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  sealed class SubOscillator : IOscillator
    ctor()
    int OctaveDown { get; set; }
    double Phase { get; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  sealed class TriangleOscillator : IOscillator
    ctor()
    double Phase { get; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()

namespace Ikon.Resonance.Synth.Sequencer
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
  enum SequencerMode
    Pattern
    Generative
  struct SequencerNote
    ctor(int noteNumber, double velocity, double duration)
    double Duration { get; }
    int NoteNumber { get; }
    double Velocity { get; }
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
  sealed class Song
    ctor()
    double Bpm { get; set; }
    int LoopLengthBeats { get; set; }
    string Name { get; set; }
    List<SongTrack> Tracks { get; set; }
  static class SongLibrary
    static Song[] All()
    static Song BinaryHorizon()
    static Song CyberChase()
    static Song DigitalDreams()
    static Song LostPatrol()
    static Song NeonPatrol()
    static Song Parallax()
    static Song ShadowRunner()
  struct SongNote
    ctor(int noteNumber, double velocity, double duration, double startBeat)
    double Duration { get; }
    int NoteNumber { get; }
    double StartBeat { get; }
    double Velocity { get; }
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
  sealed class SongTrack
    ctor()
    string Name { get; set; }
    List<SongNote> Notes { get; set; }
    MoogSynthPatch Patch { get; set; }

namespace Ikon.Resonance.Synth.Voice
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
  sealed class VoiceAllocator
    ctor(int voiceCount = 8)
    int VoiceCount { get; }
    IReadOnlyList<SynthVoice> Voices { get; }
    void AllNotesOff()
    void NoteOff(int noteNumber)
    SynthVoice NoteOn(int noteNumber, double velocity)
    void Reset()
    void SetSampleRate(double sampleRate)
