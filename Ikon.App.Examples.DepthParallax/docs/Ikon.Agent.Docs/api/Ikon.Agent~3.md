namespace Ikon.Agent
  // The agent layer never names a concrete model: each pass resolves Capability × Family to a concrete LLMModel internally, which lands on the Emerge pass as EmergePass.Model.
  sealed record Reasoning
    ctor(Capability Capability = Standard, ModelFamily Family = Claude, double Temperature = 0.7, int MaxOutputTokens = 32000, int? ClearToolResultsAfterInputTokens = null, IReadOnlyList<string>? ClearToolResultsExcludedTools = null, ReasoningEffort? Effort = null)
    Capability Capability { get; init; }
    int? ClearToolResultsAfterInputTokens { get; init; }
    IReadOnlyList<string>? ClearToolResultsExcludedTools { get; init; }
    ReasoningEffort? Effort { get; init; }
    ModelFamily Family { get; init; }
    int MaxOutputTokens { get; init; }
    double Temperature { get; init; }
  static class RuntimeMessages
    // A user-role corrective the runner posts to recover a drive (the truncated-pass re-prompt). It steers the agent; it is never part of the user's conversation.
    const string NudgePayloadKind
  // Stateful (carries any service references it needs) but instances are reusable across threads. The live ToolContext is delivered per invocation; Tools itself sees no per-call context — register-time configuration only.
  abstract class Skill
    virtual string Instructions { get; }
    abstract string Name { get; }
    abstract IEnumerable<Tool> Tools()
  sealed record SkillSet
    // ReasoningOverride: When set, the runner resolves the pass's model/temperature from IT instead of the persona's Persona.Reasoning — so a stage machine can put drafting stages on a cheap fast tier while verdict stages stay on a strong one, per pass, without splitting personas.
    ctor(string Name, IReadOnlyList<Skill> Skills, string? Instructions = null, Reasoning? ReasoningOverride = null)
    string? Instructions { get; init; }
    string Name { get; init; }
    Reasoning? ReasoningOverride { get; init; }
    IReadOnlyList<Skill> Skills { get; init; }
  sealed record SpawnSpec
    ctor(string PersonaName, Content SeedTask, ThreadOptions? Options = null)
    ThreadOptions? Options { get; init; }
    string PersonaName { get; init; }
    Content SeedTask { get; init; }
  // A StatusOverride is applied before the pass's automatic GoIdle, so it wins over the default idle transition. Notes is attached to the just-completed pass's PassRecord for run analysis.
  sealed record StageTransitionResult<TState> where TState : struct, Enum
    ctor(TState? NextStage = null, ThreadTransition? StatusOverride = null, string? Notes = null)
    TState? NextStage { get; init; }
    string? Notes { get; init; }
    ThreadTransition? StatusOverride { get; init; }
  static class Storages
    static IStorage InMemory()
  readonly record struct StreamedContent
    ctor(long TextCharacters, long ReasoningCharacters, long ToolArgumentCharacters)
    long ReasoningCharacters { get; init; }
    long TextCharacters { get; init; }
    long ToolArgumentCharacters { get; init; }
  sealed record ThreadCheckpoint
    ctor(string Label, DateTime CapturedAt, ThreadSnapshot Thread, PlanSnapshot Plan, IReadOnlyList<Message> Messages, IReadOnlyList<Artifact> Artifacts)
    IReadOnlyList<Artifact> Artifacts { get; init; }
    DateTime CapturedAt { get; init; }
    string Label { get; init; }
    IReadOnlyList<Message> Messages { get; init; }
    PlanSnapshot Plan { get; init; }
    ThreadSnapshot Thread { get; init; }
  abstract record ThreadEvent
    DateTime At { get; init; }
    string ThreadId { get; init; }
  // Per-pass, NOT per-thread. Final is the thread's status at pass end — typically Idle, but can be WaitingForInput / WaitingForChildren / Done / Failed depending on what the pass triggered.
  sealed record ThreadEvent.AgentCompleted : ThreadEvent
    ctor(string ThreadId, DateTime At, string AgentName, ThreadStatus Final)
    string AgentName { get; init; }
    ThreadStatus Final { get; init; }
  sealed record ThreadEvent.ArtifactWritten : ThreadEvent
    ctor(string ThreadId, DateTime At, Artifact Artifact)
    Artifact Artifact { get; init; }
  // Fires BEFORE the runner applies the configured BudgetAction, so a consumer observing the event sees the thread's status change arrive after it. Snapshot says which cap tripped and by how much.
  sealed record ThreadEvent.BudgetExceeded : ThreadEvent
    ctor(string ThreadId, DateTime At, BudgetSnapshot Snapshot)
    BudgetSnapshot Snapshot { get; init; }
  sealed record ThreadEvent.MessagePosted : ThreadEvent
    ctor(string ThreadId, DateTime At, Message Message)
    Message Message { get; init; }
  // Fires for every pass, success or failure.
  sealed record ThreadEvent.PassCompleted : ThreadEvent
    ctor(string ThreadId, DateTime At, PassRecord Record)
    PassRecord Record { get; init; }
  // The thread has been transitioned to ThreadStatus.Failed. Retryable reflects the exception classification (a RetryableAIException surfaces here only after Emerge exhausted its internal retries).
  sealed record ThreadEvent.PassFailed : ThreadEvent
    ctor(string ThreadId, DateTime At, string AgentName, string ErrorMessage, bool Retryable)
    string AgentName { get; init; }
    string ErrorMessage { get; init; }
    bool Retryable { get; init; }
  sealed record ThreadEvent.Progress : ThreadEvent
    ctor(string ThreadId, DateTime At, string Message)
    string Message { get; init; }
  sealed record ThreadEvent.SkillEmitted : ThreadEvent
    ctor(string ThreadId, DateTime At, string SkillName, string Kind, JsonElement Payload)
    string Kind { get; init; }
    JsonElement Payload { get; init; }
    string SkillName { get; init; }
  // NOT accumulated — consumers append successive deltas to render streaming text; the final assembled text arrives as a MessagePosted once the pass ends.
  sealed record ThreadEvent.TextDelta : ThreadEvent
    ctor(string ThreadId, DateTime At, string AgentName, string Delta)
    string AgentName { get; init; }
    string Delta { get; init; }
  // Cumulative for the in-flight pass, not a delta; fired as the model reports updates mid-stream.
  sealed record ThreadEvent.TokenUsageUpdated : ThreadEvent
    ctor(string ThreadId, DateTime At, long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
  sealed record ThreadEvent.ToolCallCompleted : ThreadEvent
    ctor(string ThreadId, DateTime At, string ToolName, string Result)
    string Result { get; init; }
    string ToolName { get; init; }
  sealed record ThreadEvent.ToolCallStarted : ThreadEvent
    ctor(string ThreadId, DateTime At, string ToolName, string ParametersJson)
    string ParametersJson { get; init; }
    string ToolName { get; init; }
  sealed record ThreadOptions
    // StageMachineName: Name of a stage machine registered with Orchestrator.RegisterStageMachine. An UNREGISTERED name throws InvalidOperationException at creation — register the machine before creating the thread, and check the spelling.
    // InitialStage: Stage to seed the thread in. Requires StageMachineName to be set — supplying it alone throws InvalidOperationException.
    // InitialStatus: Starting status; defaults to Active. Only a thread created explicitly as Pending must have its first turn driven through EnsureActiveAsync/DriveAsync rather than ReactivateIfIdleAsync.
    ctor(string? StageMachineName = null, string? InitialStage = null, ThreadStatus InitialStatus = Active)
    string? InitialStage { get; init; }
    ThreadStatus InitialStatus { get; init; }
    string? StageMachineName { get; init; }
  sealed record ThreadSnapshot
    ctor(string Id, string PlanId, string AgentName, string? ParentId, ThreadStatus Status, string? Stage, string? StageMachineName, ThreadUsage Usage, DateTime CreatedAt, DateTime UpdatedAt)
    string AgentName { get; init; }
    DateTime CreatedAt { get; init; }
    string Id { get; init; }
    string? ParentId { get; init; }
    string PlanId { get; init; }
    string? Stage { get; init; }
    string? StageMachineName { get; init; }
    ThreadStatus Status { get; init; }
    DateTime UpdatedAt { get; init; }
    ThreadUsage Usage { get; init; }
  enum ThreadStatus
    Pending
    Active
    WaitingForChildren
    WaitingForInput
    Idle
    Done
    Failed
    Archived
  enum ThreadTransition
    Activate
    Yield
    WaitForChildren
    WaitForInput
    GoIdle
    Complete
    Fail
    Reactivate
    Archive
    Restore
  // Depth is the spawn-tree level from the root — a spawned child sits one level below its parent. Turns increments after each LLM pass; WallTime is the thread's own, not the tree's.
  sealed record ThreadUsage
    ctor(long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens, TimeSpan WallTime, int Depth, int Turns)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    int Depth { get; init; }
    static ThreadUsage Empty { get; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
    int Turns { get; init; }
    TimeSpan WallTime { get; init; }
  // Construct via the Tool.Of<…> factories, which infer the parameter schema from the delegate signature; use Tool.OfContext<…> when the implementation needs the live ToolContext.
  sealed record Tool
    ctor(string Name, string Description, string ParameterSchema, Func<ToolContext, JsonElement, Task<ToolResult>> Invoke, bool ReadOnly = false)
    string Description { get; init; }
    Func<ToolContext, JsonElement, Task<ToolResult>> Invoke { get; init; }
    string Name { get; init; }
    string ParameterSchema { get; init; }
    bool ReadOnly { get; init; }
    // The given JSON object schema (properties/required, optionally per-property description and enum) becomes ParameterSchema verbatim. invoke receives the raw argument object exactly as the model produced it. Like OfContext tools, dispatch through an agent pass requires the runner's ToolContext scope.
    static Tool FromSchema(string name, string description, string parameterSchemaJson, Func<ToolContext, JsonElement, Task<ToolResult>> invoke, bool readOnly = false)
    static Tool Of<TResult>(string name, string description, Func<Task<TResult>> impl)
    static Tool Of<T1, TResult>(string name, string description, Func<T1, Task<TResult>> impl)
    static Tool Of<T1, T2, TResult>(string name, string description, Func<T1, T2, Task<TResult>> impl)
    static Tool Of<T1, T2, T3, TResult>(string name, string description, Func<T1, T2, T3, Task<TResult>> impl)
    static Tool Of<T1, T2, T3, T4, TResult>(string name, string description, Func<T1, T2, T3, T4, Task<TResult>> impl)
    static Tool Of<TResult>(string name, string description, Func<TResult> impl)
    static Tool Of<T1, TResult>(string name, string description, Func<T1, TResult> impl)
    static Tool Of<T1, T2, TResult>(string name, string description, Func<T1, T2, TResult> impl)
    static Tool Of<T1, T2, T3, TResult>(string name, string description, Func<T1, T2, T3, TResult> impl)
    static Tool Of<T1, T2, T3, T4, TResult>(string name, string description, Func<T1, T2, T3, T4, TResult> impl)
    static Tool OfContext<TResult>(string name, string description, Func<ToolContext, Task<TResult>> impl)
    static Tool OfContext<T1, TResult>(string name, string description, Func<ToolContext, T1, Task<TResult>> impl)
    static Tool OfContext<T1, T2, TResult>(string name, string description, Func<ToolContext, T1, T2, Task<TResult>> impl)
    static Tool OfContext<T1, T2, T3, TResult>(string name, string description, Func<ToolContext, T1, T2, T3, Task<TResult>> impl)
    static Tool OfContext<T1, T2, T3, T4, TResult>(string name, string description, Func<ToolContext, T1, T2, T3, T4, Task<TResult>> impl)
    static Tool OfContext<TResult>(string name, string description, Func<ToolContext, TResult> impl)
    static Tool OfContext<T1, TResult>(string name, string description, Func<ToolContext, T1, TResult> impl)
    static Tool OfContext<T1, T2, TResult>(string name, string description, Func<ToolContext, T1, T2, TResult> impl)
    static Tool OfContext<T1, T2, T3, TResult>(string name, string description, Func<ToolContext, T1, T2, T3, TResult> impl)
    static Tool OfContext<T1, T2, T3, T4, TResult>(string name, string description, Func<ToolContext, T1, T2, T3, T4, TResult> impl)
    // Returns a copy; the original tool is unchanged. Unknown parameter names are ignored.
    Tool WithAllowedValues(string paramName, IReadOnlyList<string> values)
    // Returns a copy; the original tool is unchanged. Unknown parameter names are ignored.
    Tool WithParamDescription(string paramName, string description)
  // Captured when ToolCallStarted fires; the result and error flag fill in when ToolCallCompleted follows. PrecedingAgentMessages counts the agent messages already on the thread at start time, so a UI can slot the row in front of the agent message it produced.
  sealed record ToolCallEntry
    // IsError: true/false for a call completed live in this process; null when the timeline was rebuilt by replaying a thread's journal — the journal does not record the error flag, so it cannot be recovered on resume. Treat null as "unknown", not as "succeeded".
    ctor(int PrecedingAgentMessages, string ToolName, string ArgsJson, string? ResultText, bool? IsError)
    string ArgsJson { get; init; }
    bool? IsError { get; init; }
    int PrecedingAgentMessages { get; init; }
    string? ResultText { get; init; }
    string ToolName { get; init; }
  sealed record ToolContext
    ctor(AgentThread Thread, CancellationToken Cancellation)
    CancellationToken Cancellation { get; init; }
    PassMediaBudget MediaBudget { get; init; }
    // Capabilities of the model driving the current pass, or null when the runner predates capability plumbing (tests, direct Invoke). Tools use this to decide between returning media and a text description fallback.
    LLMCapabilities? ModelCapabilities { get; init; }
    BudgetRemaining RemainingBudget { get; }
    AgentThread Thread { get; init; }
    Task<Content.Reference> AttachAsArtifactAsync(string name, string mimeType, byte[] bytes, ArtifactSource source = Agent)
    Task EmitAsync(string skillName, string kind, object payload)
    // Null when the artifact is absent or its payload doesn't deserialize to T.
    Task<T?> ReadArtifactAsync<T>(string name)
    // Lands atomically in IStorage, registers on the thread's reactive AgentThread.Artifacts list, and fires ArtifactWritten.
    Task<Artifact> WriteArtifactAsync(string name, string type, IReadOnlyList<Content> parts, ArtifactSource source = Agent)
    Task<Artifact> WriteArtifactAsync<T>(string name, T value, ArtifactSource source = Agent)
  sealed record ToolInfo
    ctor(string Name, string Description, string ParameterSchema)
    string Description { get; init; }
    string Name { get; init; }
    string ParameterSchema { get; init; }
  sealed record ToolResult
    ctor(Content Output, bool IsError = false)
    bool IsError { get; init; }
    Content Output { get; init; }
  enum WaitMode
    All
    Any
