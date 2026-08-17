# Ikon.Agent Public API

namespace Ikon.Agent
  sealed record Activity
    ctor(ActivityKind Kind, string? Tool = null)
    ActivityKind Kind { get; init; }
    string? Tool { get; init; }
    static Activity RunningTool(string tool)
    static readonly Activity Idle
    static readonly Activity Streaming
    static readonly Activity Thinking
  enum ActivityKind
    Idle
    Thinking
    Streaming
    RunningTool
  sealed class AgentApp : IAsyncDisposable
    IReadOnlyList<AgentPlan> AllPlans { get; }
    Reactive<string> Brief { get; }
    DateTime CreatedAt { get; }
    string Id { get; }
    Reactive<string> Name { get; }
    Reactive<IReadOnlyList<AgentPlan>> Plans { get; }
    Reactive<ThreadStatus> Status { get; }
    Task ArchiveAsync()
    Task<AgentPlan> CreatePlanAsync(string name, string personaName, Content seedTask, ThreadOptions? options = null, CancellationToken ct = default)
    ValueTask DisposeAsync()
    AgentPlan? GetPlan(string id)
    Task RemovePlanAsync(AgentPlan plan)
    Task RenameAsync(string name)
    Task RestoreAsync()
    Task UpdateBriefAsync(string brief)
  static class AgentCall
    // The orchestrator drives the child until AgentCallSpec<T>.ExtractResult returns non-null; throws InvalidOperationException if it produces no result within AgentCallSpec<T>.MaxPasses passes. The persona's own Budget may stop the child sooner.
    static Task<T> RunAsync<T>(AgentThread parent, string personaName, AgentCallSpec<T> spec, CancellationToken ct = default) where T : class
    // The inline persona is registered only for this call and evicted afterwards, so no Orchestrator.AddPersona is needed. Inline personas are in-memory only — use a name-registered persona for sub-agents that must survive a process restart.
    static Task<T> RunAsync<T>(AgentThread parent, Persona persona, AgentCallSpec<T> spec, CancellationToken ct = default) where T : class
    // Each call nests one level deeper in the spawn tree; bound recursion with budget (Budget.MaxDepth). Safe to fan out in parallel via Task.WhenAll — each call uses a distinct inline persona name.
    static Task<T> RunSubAgentAsync<T>(AgentThread parent, string instructions, IReadOnlyList<Skill> skills, Content inputs, Func<AgentThread, Task<T?>> extract, Reasoning? reasoning = null, string name = "subagent", int maxPasses = 16, Budget? budget = null, CancellationToken ct = default) where T : class
  sealed record AgentCallRecord
    ctor(string PersonaName, string ResultType, bool Succeeded, string ChildThreadId, int Turns, long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens, TimeSpan WallTime, DateTime StartedAt)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    string ChildThreadId { get; init; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
    string PersonaName { get; init; }
    string ResultType { get; init; }
    DateTime StartedAt { get; init; }
    bool Succeeded { get; init; }
    int Turns { get; init; }
    TimeSpan WallTime { get; init; }
  sealed record AgentCallSpec<T> where T : class
    ctor(Content SeedTask, Func<AgentThread, CancellationToken, Task> PrimeAsync, Func<AgentThread, Task<T?>> ExtractResult, int MaxPasses = 16, ThreadOptions? ThreadOptions = null, Action<AgentCallRecord>? OnComplete = null)
    Func<AgentThread, Task<T?>> ExtractResult { get; init; }
    int MaxPasses { get; init; }
    Action<AgentCallRecord>? OnComplete { get; init; }
    Func<AgentThread, CancellationToken, Task> PrimeAsync { get; init; }
    Content SeedTask { get; init; }
    ThreadOptions? ThreadOptions { get; init; }
  sealed class AgentPlan : IAsyncDisposable
    AgentApp App { get; }
    string AppId { get; }
    DateTime CreatedAt { get; }
    string Id { get; }
    Reactive<string> Name { get; }
    Reactive<double?> Score { get; }
    Reactive<IReadOnlyDictionary<string, PlanSection>> Sections { get; }
    Reactive<ThreadStatus> Status { get; }
    AgentThread Thread { get; }
    Task ArchiveAsync()
    ValueTask DisposeAsync()
    PlanSection? ReadSection(string name)
    Task RenameAsync(string name)
    Task RestoreAsync()
    Task<PlanSection> UpdateContentAsync(string name, string content)
    Task<PlanSection> UpdateScoreAsync(string name, double score)
  // Every usage row an Ikon.AI call reports carries the ambient scope stack, and the platform's cost reporting can filter and group on it. Stamping the run here is what lets a host ask the platform what one agent run actually cost in credits, instead of reconstructing a number from token counts and a private price table. The value is always the root thread id, so a run's sub-threads — a validator, a browser operator, an ad-hoc sub-agent — fall under the run that spawned them rather than costing themselves separately.
  static class AgentScopes
    const string AgentRun
  sealed class AgentThread : IAsyncDisposable
    Reactive<IReadOnlyList<ToolInfo>> ActiveTools { get; }
    Reactive<Activity> Activity { get; }
    string AgentName { get; }
    Reactive<IReadOnlyList<Artifact>> Artifacts { get; }
    Reactive<IReadOnlyList<AgentThread>> Children { get; }
    Reactive<ContextSnapshot> Context { get; }
    DateTime CreatedAt { get; }
    IAsyncEnumerable<ThreadEvent> Events { get; }
    Reactive<string?> FailureReason { get; }
    string Id { get; }
    Reactive<IReadOnlyList<Message>> Messages { get; }
    string? ParentId { get; }
    // Throws InvalidOperationException if the plan has been archived out of the live registry; reach it via App + AgentApp.GetPlan when archived-plan access is needed.
    AgentPlan Plan { get; }
    string PlanId { get; }
    Reactive<BudgetRemaining> RemainingBudget { get; }
    // Walks ParentId through the orchestrator's live registry. A parent the registry no longer holds — archived, or not yet re-hydrated — ends the walk at that parent's id rather than throwing, because attribution must never be able to fail a run. Naming the unreachable ancestor, rather than the last one still resolvable, is what keeps a run's id stable: archiving the root mid-run would otherwise re-point every descendant at the deepest surviving thread and split one run's cost across two ids. Every descendant stops at the same unreachable ancestor, so the tree stays agreed on one id either way.
    string RootId { get; }
    Reactive<string?> Stage { get; }
    Reactive<ThreadStatus> Status { get; }
    IStorage Storage { get; }
    StreamedContent Streamed { get; }
    Reactive<IReadOnlyList<ToolCallEntry>> ToolCallTimeline { get; }
    Reactive<ThreadUsage> Usage { get; }
    // Returns a Reference to embed in a Message in place of the raw bytes, keeping subsequent LLM prompts small; the agent fetches the data via tool calls when it needs it.
    Task<Content.Reference> AttachAsArtifactAsync(string name, string mimeType, byte[] bytes, ArtifactSource source = User)
    // Attachments larger than inlineThresholdBytes are promoted to thread artifacts and replaced with a Reference; smaller items embed inline so the model sees them directly.
    Task<Message> BuildUserMessageAsync(string text, IReadOnlyList<AttachmentInput> attachments, long inlineThresholdBytes = 262144, CancellationToken ct = default)
    Task<ThreadCheckpoint> CheckpointAsync(string label, CancellationToken ct = default)
    ValueTask DisposeAsync()
    // Use DriveMode.UntilStable whenever a stage machine is registered; otherwise DriveMode.UntilQuiescent.
    Task DriveAsync(DriveMode mode = UntilQuiescent, Func<AgentThread, Task>? onPass = null, int safety = 256, CancellationToken ct = default)
    Task<bool> EnsureActiveAsync()
    Task EnsureDrivenAsync(Func<CancellationToken, Task> driveLoop, CancellationToken ct = default)
    Task<Artifact?> GetArtifactAsync(string name)
    Task<bool> HasArtifactAsync(string name)
    Task PostAsync(Message msg, CancellationToken ct = default)
    Task<bool> ReactivateIfIdleAsync()
    Task<T?> ReadArtifactAsync<T>(string name)
    // This thread transitions to ThreadStatus.WaitingForChildren for the duration and reactivates once wait is satisfied.
    Task<IReadOnlyList<AgentThread>> SpawnAllAsync(IReadOnlyList<SpawnSpec> children, WaitMode wait = All, CancellationToken ct = default)
    // Fire-and-forget: returns immediately with the child handle and this thread keeps running. Use SpawnAllAsync to wait for children.
    Task<AgentThread> SpawnAsync(string personaName, Content task, ThreadOptions? options = null, CancellationToken ct = default)
    // Returns false (no-op) if the transition is invalid from the current status. The new status reaches consumers via the Status reactive — there is no separate transition event.
    Task<bool> TransitionAsync(ThreadTransition transition)
    // Replace-by-name: writing a name that already exists updates that artifact's storage row in place rather than adding a duplicate. Use for structured outputs; for raw bytes use AttachAsArtifactAsync.
    Task<Artifact> WriteArtifactAsync(string name, string type, IReadOnlyList<Content> parts, ArtifactSource source = Agent)
    Task<Artifact> WriteArtifactAsync<T>(string name, T value, ArtifactSource source = Agent)
    const long DefaultInlineThresholdBytes = 262144
  sealed record AppSnapshot
    ctor(string Id, string Name, string Brief, ThreadStatus Status, DateTime CreatedAt, DateTime UpdatedAt)
    string Brief { get; init; }
    DateTime CreatedAt { get; init; }
    string Id { get; init; }
    string Name { get; init; }
    ThreadStatus Status { get; init; }
    DateTime UpdatedAt { get; init; }
  sealed record Artifact
    ctor(string Id, string Name, string Type, IReadOnlyList<Content> Parts, string ThreadId, ArtifactSource Source = Agent)
    string Id { get; init; }
    string Name { get; init; }
    IReadOnlyList<Content> Parts { get; init; }
    ArtifactSource Source { get; init; }
    string ThreadId { get; init; }
    string Type { get; init; }
  enum ArtifactSource
    Agent
    User
    System
  sealed record AttachmentInput
    ctor(byte[] Bytes, string MimeType, string? Name = null)
    byte[] Bytes { get; init; }
    string MimeType { get; init; }
    string? Name { get; init; }
  readonly record struct Author
    ctor(AuthorKind Kind, string? Name = null)
    AuthorKind Kind { get; init; }
    string? Name { get; init; }
    static Author Agent(string name)
    static readonly Author User
  enum AuthorKind
    User
    Agent
  // Each set field is an independent cap evaluated against the thread's own usage, never summed across kinds. MaxDepth is the exception: it counts spawn-tree levels from the root (set at construction, never mutated). Settable host-wide on Orchestrator or per-agent via Persona.Budget; the two merge with the tighter cap winning per kind.
  sealed record Budget
    ctor(long? MaxInputTokens = null, long? MaxCachedInputTokens = null, long? MaxCacheCreationInputTokens = null, long? MaxOutputTokens = null, TimeSpan? MaxWallTime = null, int? MaxDepth = null, int? MaxTurns = null, BudgetAction OnExceeded = AskUser)
    long? MaxCacheCreationInputTokens { get; init; }
    long? MaxCachedInputTokens { get; init; }
    int? MaxDepth { get; init; }
    long? MaxInputTokens { get; init; }
    long? MaxOutputTokens { get; init; }
    int? MaxTurns { get; init; }
    TimeSpan? MaxWallTime { get; init; }
    BudgetAction OnExceeded { get; init; }
  enum BudgetAction
    AskUser
    Stop
    Continue
  sealed record BudgetRemaining
    ctor(long? InputTokensRemaining, long? CachedInputTokensRemaining, long? CacheCreationInputTokensRemaining, long? OutputTokensRemaining, TimeSpan? WallTimeRemaining, int? TurnsRemaining, Budget? Effective)
    long? CacheCreationInputTokensRemaining { get; init; }
    long? CachedInputTokensRemaining { get; init; }
    Budget? Effective { get; init; }
    long? InputTokensRemaining { get; init; }
    long? OutputTokensRemaining { get; init; }
    int? TurnsRemaining { get; init; }
    TimeSpan? WallTimeRemaining { get; init; }
  sealed record BudgetSnapshot
    ctor(string TrippedField, long ActualValue, long LimitValue)
    long ActualValue { get; init; }
    long LimitValue { get; init; }
    string TrippedField { get; init; }
  enum Capability
    Quick
    Standard
    Deep
  static class Checkpoints
    static Task<ThreadCheckpoint> ReadAsync(string path, CancellationToken ct = default)
    static Task WriteAsync(ThreadCheckpoint checkpoint, string path, CancellationToken ct = default)
  abstract record Content
  sealed record Content.Audio : Content
    ctor(byte[] Bytes, string MimeType)
    byte[] Bytes { get; init; }
    string MimeType { get; init; }
  sealed record Content.Binary : Content
    ctor(byte[] Bytes, string MimeType)
    byte[] Bytes { get; init; }
    string MimeType { get; init; }
  sealed record Content.Image : Content
    ctor(byte[] Bytes, string MimeType)
    byte[] Bytes { get; init; }
    string MimeType { get; init; }
  sealed record Content.Reference : Content
    ctor(string Uri, string MimeType)
    string MimeType { get; init; }
    string Uri { get; init; }
  sealed record Content.Text : Content
    ctor(string Value)
    string Value { get; init; }
  static class ContentExtensions
    static string GetText(this IEnumerable<Content> parts)
    static string GetText(this Message message)
    static string GetText(this Artifact artifact)
  sealed record ContextSnapshot
    ctor(string SystemPrompt, IReadOnlyList<Message> History)
    static ContextSnapshot Empty { get; }
    IReadOnlyList<Message> History { get; init; }
    string SystemPrompt { get; init; }
  enum DriveMode
    UntilQuiescent
    UntilStable
  static class EmergeToolExtensions
    static EmergePass<T> AddTool<T>(this EmergePass<T> pass, Tool tool)
    static EmergePass<T> AddTools<T>(this EmergePass<T> pass, params Tool[] tools)
  interface IStageMachine<TState> where TState : struct, Enum
    TState InitialState { get; }
    string Name { get; }
    virtual IReadOnlySet<int> ContextExclusions(AgentThread thread, TState state)
    virtual Task OnPassStartingAsync(AgentThread thread, TState state, CancellationToken ct)
    SkillSet SkillsFor(AgentThread thread, TState state)
    virtual Task<StageTransitionResult<TState>?> TryResumeWithoutPassAsync(AgentThread thread, TState current, CancellationToken ct)
    Task<StageTransitionResult<TState>?> TryTransitionAsync(AgentThread thread, TState current, ThreadEvent evt, CancellationToken ct)
  interface IStorage
    Task<long> AppendJournalAsync(string threadId, ThreadEvent entry, CancellationToken ct = default)
    Task AppendMessageAsync(string threadId, Message message, CancellationToken ct = default)
    Task DeletePlanAsync(string id, CancellationToken ct = default)
    IAsyncEnumerable<AppSnapshot> ListAppsAsync(CancellationToken ct = default)
    IAsyncEnumerable<ThreadSnapshot> ListNonTerminalAsync(CancellationToken ct = default)
    IAsyncEnumerable<PlanSnapshot> ListPlansAsync(CancellationToken ct = default)
    IAsyncEnumerable<ThreadSnapshot> ListTerminalAsync(CancellationToken ct = default)
    Task PruneThreadLogAsync(string threadId, CancellationToken ct = default)
    Task<AppSnapshot?> ReadAppAsync(string id, CancellationToken ct = default)
    Task<Artifact?> ReadArtifactByNameAsync(string threadId, string name, CancellationToken ct = default)
    IAsyncEnumerable<Artifact> ReadArtifactsByThreadAsync(string threadId, CancellationToken ct = default)
    IAsyncEnumerable<JournalEntry> ReadJournalAsync(string threadId, long fromSequence = 0, CancellationToken ct = default)
    IAsyncEnumerable<Message> ReadMessagesAsync(string threadId, CancellationToken ct = default)
    Task<PlanSnapshot?> ReadPlanAsync(string id, CancellationToken ct = default)
    Task SaveAppAsync(AppSnapshot snapshot, CancellationToken ct = default)
    Task SavePlanAsync(PlanSnapshot snapshot, CancellationToken ct = default)
    Task SaveThreadAsync(ThreadSnapshot snapshot, CancellationToken ct = default)
    Task WriteArtifactAsync(Artifact artifact, CancellationToken ct = default)
  static class JournalCodec
    static ThreadEvent Decode(string kind, string payload)
    static string Encode(ThreadEvent evt)
    static bool IsJournalable(ThreadEvent evt)
    static string Kind(ThreadEvent evt)
  sealed record JournalEntry
    ctor(long Sequence, string ThreadId, DateTime At, string Kind, ThreadEvent Event)
    DateTime At { get; init; }
    ThreadEvent Event { get; init; }
    string Kind { get; init; }
    long Sequence { get; init; }
    string ThreadId { get; init; }
  sealed record Message
    ctor(Author Author, IReadOnlyList<Content> Parts, string? PayloadKind = null, JsonElement? Payload = null, string? ReplyToMessageId = null)
    Author Author { get; init; }
    IReadOnlyList<Content> Parts { get; init; }
    JsonElement? Payload { get; init; }
    string? PayloadKind { get; init; }
    string? ReplyToMessageId { get; init; }
  enum ModelFamily
    Claude
    Gpt
    Gemini
    Kimi
    Grok
    DeepSeek
    Glm
  static class ModelResolver
    static LLMModel Resolve(Reasoning reasoning)
    static LLMModel Resolve(Capability capability, ModelFamily family)
  // Constructed synchronously; call ResumeAsync once at startup to re-hydrate persisted apps, plans, and threads before driving any thread.
  sealed class Orchestrator : IAsyncDisposable
    ctor(IStorage? storage = null, Budget? hostBudget = null, ILLM? llm = null)
    Reactive<IReadOnlyList<AgentApp>> Apps { get; }
    IAsyncEnumerable<ThreadEvent> Events { get; }
    IGovernanceHook? GovernanceHook { get; set; }
    Func<Reasoning, LLMModel>? ModelResolverHook { get; set; }
    Reactive<IReadOnlyList<Persona>> Personas { get; }
    IStorage Storage { get; }
    bool StreamProgress { get; set; }
    Reactive<IReadOnlyList<AgentThread>> Threads { get; }
    Orchestrator AddPersona(Persona persona)
    // Omit id and each call mints a new app; pass a host-recomputable id (a space id, a workspace key) and the same call after ResumeAsync re-uses the persisted app — same plans, threads, and artifacts.
    Task<AgentApp> CreateAppAsync(string name, string brief = "", string? id = null, CancellationToken ct = default)
    // Get-or-create by name: matches the app on appName (default personaName) and the plan on planName, so a repeated call after ResumeAsync returns the SAME persisted thread. seedTask is posted only when the plan is first created, never onto an existing history.
    Task<AgentThread> CreateThreadAsync(string personaName, Content seedTask, string? appName = null, string planName = "main", CancellationToken ct = default)
    ValueTask DisposeAsync()
    AgentApp? GetApp(string id)
    AgentThread? GetThread(string threadId)
    Orchestrator RegisterStageMachine<TState>(IStageMachine<TState> machine) where TState : struct, Enum
    // Mints fresh ids throughout, so one checkpoint can be restored many times as independent runs. The returned thread carries the checkpoint's status (typically ThreadStatus.Idle); re-engage it with AgentThread.ReactivateIfIdleAsync + RunPassAsync or AgentThread.DriveAsync. Throws if the checkpoint's persona is not registered.
    Task<AgentThread> RestoreCheckpointAsync(ThreadCheckpoint checkpoint, CancellationToken ct = default)
    // Idempotent — safe to call once at startup, a no-op for in-memory storage. Loads in dependency order (apps, then plans, then threads); an unknown persisted stage throws InvalidOperationException immediately.
    Task ResumeAsync(bool includeTerminal = false, CancellationToken ct = default)
    // Throws InvalidOperationException if the thread is still ThreadStatus.Pending, or if no persona named AgentThread.AgentName is registered.
    Task RunPassAsync(AgentThread thread, CancellationToken ct = default)
  sealed class PassMediaBudget
    ctor()
    bool HasHeadroom { get; }
    bool TryClaim(string artifactName, out bool alreadyShown)
    const int MaxImagesPerPass = 3
  sealed record PassMessage
    ctor(string Role, string Text)
    string Role { get; init; }
    string Text { get; init; }
  // A record is the complete, self-contained account of one pass: model + sampling, the exact SystemPrompt and Context given to the model, its AssistantText reply, and every tool call with full arguments and results. StageBefore / StageAfter carry the FSM stage for stage-machine runs and are null otherwise; non-text content (images/audio) is omitted.
  sealed record PassRecord
    ctor(string ThreadId, string AgentName, int PassNumber, string? StageBefore, string? StageAfter, ThreadStatus FinalStatus, DateTime StartedAt, DateTime CompletedAt, long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens, int MessagesPosted, IReadOnlyList<PassToolCall> ToolCalls, string? Error, string? Notes = null, string Model = "", double Temperature = 0.0, int MaxOutputTokens = 0, string SystemPrompt = "", string AssistantText = "", IReadOnlyList<PassMessage>? Context = null, string FinishReason = "", int Retries = 0)
    string AgentName { get; init; }
    string AssistantText { get; init; }
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    DateTime CompletedAt { get; init; }
    IReadOnlyList<PassMessage>? Context { get; init; }
    TimeSpan Duration { get; }
    string? Error { get; init; }
    ThreadStatus FinalStatus { get; init; }
    string FinishReason { get; init; }
    long InputTokens { get; init; }
    int MaxOutputTokens { get; init; }
    int MessagesPosted { get; init; }
    string Model { get; init; }
    string? Notes { get; init; }
    long OutputTokens { get; init; }
    int PassNumber { get; init; }
    int Retries { get; init; }
    string? StageAfter { get; init; }
    string? StageBefore { get; init; }
    DateTime StartedAt { get; init; }
    string SystemPrompt { get; init; }
    double Temperature { get; init; }
    string ThreadId { get; init; }
    IReadOnlyList<PassToolCall> ToolCalls { get; init; }
  sealed record PassToolCall
    ctor(string CallId, string Name, string ParametersJson, string Result)
    string CallId { get; init; }
    string Name { get; init; }
    string ParametersJson { get; init; }
    string Result { get; init; }
  // Set NudgeOnAssistantStall true only for agents that must keep working when the drive re-invokes a thread whose last turn was their own tool-less message (the runner appends a bounded user continuation to unstall them); leave it false for agents that legitimately conclude with a text turn, or nudging makes them loop forever.
  sealed record Persona
    ctor(string Name, string SystemPrompt, IReadOnlyList<Skill> Skills, Reasoning Reasoning, Budget? Budget = null, bool NudgeOnAssistantStall = false)
    Budget? Budget { get; init; }
    string Name { get; init; }
    bool NudgeOnAssistantStall { get; init; }
    Reasoning Reasoning { get; init; }
    IReadOnlyList<Skill> Skills { get; init; }
    string SystemPrompt { get; init; }
  sealed record PlanSection
    ctor(string Content, double? Score = null)
    string Content { get; init; }
    double? Score { get; init; }
  sealed record PlanSnapshot
    ctor(string Id, string AppId, string Name, IReadOnlyDictionary<string, PlanSection> Sections, double? Score, ThreadStatus Status, DateTime CreatedAt, DateTime UpdatedAt)
    string AppId { get; init; }
    DateTime CreatedAt { get; init; }
    string Id { get; init; }
    string Name { get; init; }
    double? Score { get; init; }
    IReadOnlyDictionary<string, PlanSection> Sections { get; init; }
    ThreadStatus Status { get; init; }
    DateTime UpdatedAt { get; init; }
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
  sealed record RunAnalysis
    ctor(string ThreadId, int TotalPasses, int FailedPasses, long TotalInputTokens, long TotalCachedInputTokens, long TotalCacheCreationInputTokens, long TotalOutputTokens, TimeSpan TotalWallTime, IReadOnlyList<StageRollup> Stages)
    int FailedPasses { get; init; }
    IReadOnlyList<StageRollup> Stages { get; init; }
    string ThreadId { get; init; }
    long TotalCacheCreationInputTokens { get; init; }
    long TotalCachedInputTokens { get; init; }
    long TotalInputTokens { get; init; }
    long TotalOutputTokens { get; init; }
    int TotalPasses { get; init; }
    TimeSpan TotalWallTime { get; init; }
    static RunAnalysis From(IReadOnlyList<PassRecord> records)
    static Task<RunAnalysis> FromLogAsync(string jsonlPath, CancellationToken ct = default)
  sealed class RunLog : IAsyncDisposable
    void Append(PassRecord record)
    static RunLog Attach(Orchestrator orchestrator, string jsonlPath)
    ValueTask DisposeAsync()
    static IAsyncEnumerable<PassRecord> ReadAsync(string jsonlPath, CancellationToken ct = default)
  static class RuntimeMessages
    const string NudgePayloadKind
  abstract class Skill
    virtual string Instructions { get; }
    abstract string Name { get; }
    abstract IEnumerable<Tool> Tools()
  sealed record SkillSet
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
  sealed record StageRollup
    ctor(string Stage, int Passes, int FailedPasses, long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens, TimeSpan WallTime, int ToolCalls)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    int FailedPasses { get; init; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
    int Passes { get; init; }
    string Stage { get; init; }
    int ToolCalls { get; init; }
    TimeSpan WallTime { get; init; }
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
  sealed record ThreadEvent.AgentCompleted : ThreadEvent
    ctor(string ThreadId, DateTime At, string AgentName, ThreadStatus Final)
    string AgentName { get; init; }
    ThreadStatus Final { get; init; }
  sealed record ThreadEvent.ArtifactWritten : ThreadEvent
    ctor(string ThreadId, DateTime At, Artifact Artifact)
    Artifact Artifact { get; init; }
  sealed record ThreadEvent.BudgetExceeded : ThreadEvent
    ctor(string ThreadId, DateTime At, BudgetSnapshot Snapshot)
    BudgetSnapshot Snapshot { get; init; }
  sealed record ThreadEvent.MessagePosted : ThreadEvent
    ctor(string ThreadId, DateTime At, Message Message)
    Message Message { get; init; }
  sealed record ThreadEvent.PassCompleted : ThreadEvent
    ctor(string ThreadId, DateTime At, PassRecord Record)
    PassRecord Record { get; init; }
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
  sealed record ThreadEvent.TextDelta : ThreadEvent
    ctor(string ThreadId, DateTime At, string AgentName, string Delta)
    string AgentName { get; init; }
    string Delta { get; init; }
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
  sealed record ToolCallEntry
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
    LLMCapabilities? ModelCapabilities { get; init; }
    BudgetRemaining RemainingBudget { get; }
    AgentThread Thread { get; init; }
    Task<Content.Reference> AttachAsArtifactAsync(string name, string mimeType, byte[] bytes, ArtifactSource source = Agent)
    Task EmitAsync(string skillName, string kind, object payload)
    Task<T?> ReadArtifactAsync<T>(string name)
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

namespace Ikon.Agent.Skills
  static class Built
    static readonly Skill Attachments
    static readonly Skill Messaging
    static readonly Skill Termination
    static readonly Skill UserDecision
    static readonly Skill WebSearch
  sealed class McpSkill : Skill
    ctor(McpClient mcpClient, string name = "mcp", string instructions = "")
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  sealed record UserDecisionPrompt
    ctor(string Question, IReadOnlyList<string> Options, string Kind = "decision")
    string Kind { get; init; }
    IReadOnlyList<string> Options { get; init; }
    string Question { get; init; }
  static class UserDecisionProtocol
    static Message BuildResponse(string choice)
    static Task<UserDecisionPrompt?> TryReadPromptAsync(AgentThread thread)
    static UserDecisionResponse? TryReadResponse(Message message)
    const string ArtifactMimeType
    const string ArtifactName
    const string ResponsePayloadKind
  sealed record UserDecisionResponse
    ctor(string Choice)
    string Choice { get; init; }
