namespace Ikon.Agent
  // Distinct from ThreadStatus, which is the lifecycle state. Tool is set only when Kind is ActivityKind.RunningTool; null otherwise.
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
    // Includes archived plans; the reactive Plans is the active subset, and this is a point-in-time snapshot.
    IReadOnlyList<AgentPlan> AllPlans { get; }
    Reactive<string> Brief { get; }
    DateTime CreatedAt { get; }
    string Id { get; }
    Reactive<string> Name { get; }
    // Active (non-archived) plans only, in creation order.
    Reactive<IReadOnlyList<AgentPlan>> Plans { get; }
    // Only ThreadStatus.Active or ThreadStatus.Archived; apps don't have the rich FSM that threads do.
    Reactive<ThreadStatus> Status { get; }
    // Cascades to every owned plan (which archives its working thread) and removes the app from Orchestrator.Apps.
    Task ArchiveAsync()
    // Also creates the plan's 1:1 working thread, which runs personaName (must be registered on the orchestrator) seeded with seedTask.
    Task<AgentPlan> CreatePlanAsync(string name, string personaName, Content seedTask, ThreadOptions? options = null, CancellationToken ct = default)
    ValueTask DisposeAsync()
    AgentPlan? GetPlan(string id)
    // Deletes the plan from durable storage and disposes its working thread. Irreversible, unlike AgentPlan.ArchiveAsync.
    Task RemovePlanAsync(AgentPlan plan)
    Task RenameAsync(string name)
    Task RestoreAsync()
    Task UpdateBriefAsync(string brief)
  static class AgentCall
    // The orchestrator drives the child until AgentCallSpec<T>.ExtractResult returns non-null; throws InvalidOperationException if it produces no result within AgentCallSpec<T>.MaxPasses passes. The persona's own Budget may stop the child sooner.
    // parent: Parent thread the child is spawned under.
    // personaName: Persona registered on the orchestrator that defines the child's system prompt, tools, and budget.
    // spec: Typed call specification — seed task, primer, result extractor, pass cap.
    static Task<T> RunAsync<T>(AgentThread parent, string personaName, AgentCallSpec<T> spec, CancellationToken ct = default) where T : class
    // The inline persona is registered only for this call and evicted afterwards, so no Orchestrator.AddPersona is needed. Inline personas are in-memory only — use a name-registered persona for sub-agents that must survive a process restart.
    static Task<T> RunAsync<T>(AgentThread parent, Persona persona, AgentCallSpec<T> spec, CancellationToken ct = default) where T : class
    // Each call nests one level deeper in the spawn tree; bound recursion with budget (Budget.MaxDepth). Safe to fan out in parallel via Task.WhenAll — each call uses a distinct inline persona name.
    // parent: Parent thread the sub-agent is spawned under.
    // instructions: The sub-agent's system prompt.
    // skills: Capabilities the sub-agent may use. Plain Skill instances — pass [] for a pure-reasoning agent.
    // inputs: Seed content posted to the sub-agent on spawn (the task).
    // extract: Reads the child's state (artifacts, last message) and returns the typed result when ready, or null to keep driving.
    // reasoning: How the sub-agent thinks. Defaults to Reasoning (Standard Claude).
    // name: Friendly label for the inline persona; a unique suffix is appended so parallel calls don't clash.
    // maxPasses: Upper bound on passes before bailing.
    // budget: Optional resource limits (e.g. Budget.MaxDepth to bound recursion).
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
    // SeedTask: First user-message content posted to the child thread on spawn — typically the high-level task statement.
    // PrimeAsync: Posts typed inputs the agent needs that are not already in the seed task. Called once, before any agent pass runs.
    // ExtractResult: Returns the typed result once the agent has produced it, or null if not ready. Called after each pass.
    // MaxPasses: Hard ceiling on passes for this call; each pass is one LLM turn. The persona's own Budget applies on top.
    // OnComplete: Invoked once as the call exits — on both the success and the failure path — with the AgentCallRecord for the call. The caller owns any aggregation or output; leave null to capture nothing.
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
    // Min of all sections that have a score, or null when none do.
    Reactive<double?> Score { get; }
    Reactive<IReadOnlyDictionary<string, PlanSection>> Sections { get; }
    // Only ThreadStatus.Active or ThreadStatus.Archived; mirrors archival from the underlying thread automatically. The rich FSM (Idle/WaitingForInput/etc.) is on Thread.AgentThread.Status.
    Reactive<ThreadStatus> Status { get; }
    AgentThread Thread { get; }
    Task ArchiveAsync()
    ValueTask DisposeAsync()
    PlanSection? ReadSection(string name)
    Task RenameAsync(string name)
    Task RestoreAsync()
    // Preserves any existing score on the section; orthogonal to UpdateScoreAsync.
    Task<PlanSection> UpdateContentAsync(string name, string content)
    // Preserves the section's existing content; creates the section with empty content if it didn't exist.
    Task<PlanSection> UpdateScoreAsync(string name, double score)
  // Every usage row an Ikon.AI call reports carries the ambient scope stack, and the platform's cost reporting can filter and group on it. Stamping the run here is what lets a host ask the platform what one agent run actually cost in credits, instead of reconstructing a number from token counts and a private price table. The value is always the root thread id, so a run's sub-threads — a validator, a browser operator, an ad-hoc sub-agent — fall under the run that spawned them rather than costing themselves separately.
  static class AgentScopes
    const string AgentRun
  sealed class AgentThread : IAsyncDisposable
    IReadOnlyReactive<IReadOnlyList<ToolInfo>> ActiveTools { get; }
    IReadOnlyReactive<Activity> Activity { get; }
    string AgentName { get; }
    IReadOnlyReactive<IReadOnlyList<Artifact>> Artifacts { get; }
    IReadOnlyReactive<IReadOnlyList<AgentThread>> Children { get; }
    IReadOnlyReactive<ContextSnapshot> Context { get; }
    DateTime CreatedAt { get; }
    IAsyncEnumerable<ThreadEvent> Events { get; }
    // Why the thread last entered ThreadStatus.Failed — the exception message from a failed pass, or a budget-stop reason. Null until a failure occurs.
    IReadOnlyReactive<string?> FailureReason { get; }
    string Id { get; }
    // True while a drive loop currently owns this thread (the single-consumer drive lock is held) — the official "is anything advancing this thread right now" signal. Not reactive; pair it with a Status read (which is) so consumers re-evaluate on every settle.
    bool IsBeingDriven { get; }
    // Every posted message in arrival order; seeded from storage at construction and appended on every MessagePosted.
    IReadOnlyReactive<IReadOnlyList<Message>> Messages { get; }
    string? ParentId { get; }
    // Throws InvalidOperationException if the plan has been archived out of the live registry; reach it via App + AgentApp.GetPlan when archived-plan access is needed.
    AgentPlan Plan { get; }
    string PlanId { get; }
    // Derived from Usage and the merged host/persona Budget; recomputed on every usage change.
    IReadOnlyReactive<BudgetRemaining> RemainingBudget { get; }
    // Walks ParentId through the orchestrator's live registry. A parent the registry no longer holds — archived, or not yet re-hydrated — ends the walk at that parent's id rather than throwing, because attribution must never be able to fail a run. Naming the unreachable ancestor, rather than the last one still resolvable, is what keeps a run's id stable: archiving the root mid-run would otherwise re-point every descendant at the deepest surviving thread and split one run's cost across two ids. Every descendant stops at the same unreachable ancestor, so the tree stays agreed on one id either way.
    string RootId { get; }
    IReadOnlyReactive<string?> Stage { get; }
    IReadOnlyReactive<ThreadStatus> Status { get; }
    IStorage Storage { get; }
    // Unlike Usage, which providers report only once a turn has ended, these counters advance chunk by chunk while generation is in flight. Measured in characters, not tokens. Own-thread only; walk Children for a tree.
    StreamedContent Streamed { get; }
    IReadOnlyReactive<IReadOnlyList<ToolCallEntry>> ToolCallTimeline { get; }
    IReadOnlyReactive<ThreadUsage> Usage { get; }
    // Returns a Reference to embed in a Message in place of the raw bytes, keeping subsequent LLM prompts small; the agent fetches the data via tool calls when it needs it.
    Task<Content.Reference> AttachAsArtifactAsync(string name, string mimeType, byte[] bytes, ArtifactSource source = User)
    // Attachments larger than inlineThresholdBytes are promoted to thread artifacts and replaced with a Reference; smaller items embed inline so the model sees them directly.
    Task<Message> BuildUserMessageAsync(string text, IReadOnlyList<AttachmentInput> attachments, long inlineThresholdBytes = 262144, CancellationToken ct = default)
    // Read-only — the live thread keeps running. Restore later, into this or any orchestrator, with Orchestrator.RestoreCheckpointAsync.
    Task<ThreadCheckpoint> CheckpointAsync(string label, CancellationToken ct = default)
    ValueTask DisposeAsync()
    // Use DriveMode.UntilStable whenever a stage machine is registered; otherwise DriveMode.UntilQuiescent.
    // onPass: Optional hook invoked after each successful pass (e.g. to refresh host-side UI lists). Receives the thread.
    // safety: Maximum passes before bail-out. Defaults to 256.
    Task<DriveOutcome> DriveAsync(DriveMode mode = UntilQuiescent, Func<AgentThread, Task>? onPass = null, int safety = 256, CancellationToken ct = default)
    // Activates from Pending, reactivates from Idle / WaitingForInput / WaitingForChildren / Done / Failed; returns false only when the thread is already Active or archived. Use this, not ReactivateIfIdleAsync, when resuming a thread whose state you don't control — Reactivate is rejected from Pending, so the obvious "re-engage" call silently fails to start a never-run thread. Only DriveAsync auto-activates.
    Task<bool> EnsureActiveAsync()
    // Runs driveLoop under exclusive per-thread ownership, coalescing re-entrant calls. If a drive is already advancing this thread, the call flags a re-drive and returns immediately — the active loop re-runs driveLoop before exiting, so freshly-posted input is never stranded and two drives can never run at once. Every drive should route through here rather than guarding concurrency itself.
    Task EnsureDrivenAsync(Func<CancellationToken, Task> driveLoop, CancellationToken ct = default)
    Task<Artifact?> GetArtifactAsync(string name)
    Task<bool> HasArtifactAsync(string name)
    // Persists to storage and fires MessagePosted; the Messages reactive picks the new message up before the event fans out to subscribers.
    Task PostAsync(Message msg, CancellationToken ct = default)
    // Reactivates from Idle, WaitingForInput, WaitingForChildren, Done, or Failed. No-op (returns false) when the thread is already Active, archived, or still Pending — the transition matrix has no Reactivate arc from those states; a Pending thread's first turn goes through EnsureActiveAsync or DriveAsync.
    Task<bool> ReactivateIfIdleAsync()
    // Returns null when the artifact is absent or its payload doesn't deserialize to T.
    Task<T?> ReadArtifactAsync<T>(string name)
    // This waits for the children to reach a terminal state (Done / Failed); it does NOT run them. Spawning creates each child Active with its seed task posted, but no library background loop advances it — so the caller MUST drive every spawned child to completion CONCURRENTLY, on its own task, or this call blocks until ct fires (WaitMode.All) or until a child that was already terminal at spawn satisfies WaitMode.Any. Because this method itself blocks the current thread, the drives must already be in flight — e.g. start child.DriveAsync(...) (and a terminating step such as TransitionAsync(ThreadTransition.Complete)) on background tasks before or as the children are spawned, or spawn from a host loop that owns the driving. A driver that only reaches Idle never satisfies the wait: the children must be driven all the way to a terminal status.
    Task<IReadOnlyList<AgentThread>> SpawnAllAsync(IReadOnlyList<SpawnSpec> children, WaitMode wait = All, CancellationToken ct = default)
    // The child is created Active with its seed task posted, but it is NOT driven — no library background loop advances it. Nothing runs until the caller drives it: await child.DriveAsync(...) (fire-and-forget on a separate task if this thread should not block), or a host pass loop. Read the child's state (artifacts, messages) only after a drive has advanced it. Left undriven the child sits idle forever. AgentCall.RunAsync<T> is the spawn-drive-extract convenience when a typed result is wanted.
    Task<AgentThread> SpawnAsync(string personaName, Content task, ThreadOptions? options = null, CancellationToken ct = default)
    // Returns false (no-op) if the transition is invalid from the current status. The new status reaches consumers via the Status reactive — there is no separate transition event.
    // reason: Recorded on FailureReason before the status changes, so a consumer woken by the status already sees why. Applies to ThreadTransition.Fail; ignored otherwise, and null leaves any existing reason in place.
    Task<bool> TransitionAsync(ThreadTransition transition, string? reason = null)
    // Replace-by-name: writing a name that already exists updates that artifact's storage row in place rather than adding a duplicate. Use for structured outputs; for raw bytes use AttachAsArtifactAsync.
    Task<Artifact> WriteArtifactAsync(string name, string type, IReadOnlyList<Content> parts, ArtifactSource source = Agent)
    // JSON-serializes value into one text part; read it back with ReadArtifactAsync<T>. Mark it ArtifactSource.System for control data that shouldn't show in the user-facing artifact list.
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
  // Each set field is an independent cap evaluated against the thread's own usage, never summed across kinds. MaxDepth is the exception: it counts spawn-tree levels from the root (set at construction, never mutated). Settable host-wide on Orchestrator or per-agent via Persona.Budget; the two merge with the tighter cap winning per kind. OnExceeded defaults to BudgetAction.AskUser, which PARKS the thread in ThreadStatus.WaitingForInput rather than terminating it — a hard stop for a driver, so a headless run leaves the thread parked and never finishes on its own. Pass BudgetAction.Stop for headless/batch runs that must terminate on the cap; new Budget(MaxTurns: 10) alone does not stop such a run.
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
  // Each field is the cap minus current usage for that kind, or null if no cap is set. Effective is the merged cap (host + per-persona) the runtime is enforcing.
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
