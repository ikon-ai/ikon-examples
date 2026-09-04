# Ikon.Agent Public API

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
    Reactive<IReadOnlyList<ToolInfo>> ActiveTools { get; }
    Reactive<Activity> Activity { get; }
    string AgentName { get; }
    Reactive<IReadOnlyList<Artifact>> Artifacts { get; }
    Reactive<IReadOnlyList<AgentThread>> Children { get; }
    Reactive<ContextSnapshot> Context { get; }
    DateTime CreatedAt { get; }
    IAsyncEnumerable<ThreadEvent> Events { get; }
    // Why the thread last entered ThreadStatus.Failed — the exception message from a failed pass, or a budget-stop reason. Null until a failure occurs.
    Reactive<string?> FailureReason { get; }
    string Id { get; }
    // True while a drive loop currently owns this thread (the single-consumer drive lock is held) — the official "is anything advancing this thread right now" signal. Not reactive; pair it with a Status read (which is) so consumers re-evaluate on every settle.
    bool IsBeingDriven { get; }
    // Every posted message in arrival order; seeded from storage at construction and appended on every MessagePosted.
    Reactive<IReadOnlyList<Message>> Messages { get; }
    string? ParentId { get; }
    // Throws InvalidOperationException if the plan has been archived out of the live registry; reach it via App + AgentApp.GetPlan when archived-plan access is needed.
    AgentPlan Plan { get; }
    string PlanId { get; }
    // Derived from Usage and the merged host/persona Budget; recomputed on every usage change.
    Reactive<BudgetRemaining> RemainingBudget { get; }
    // Walks ParentId through the orchestrator's live registry. A parent the registry no longer holds — archived, or not yet re-hydrated — ends the walk at that parent's id rather than throwing, because attribution must never be able to fail a run. Naming the unreachable ancestor, rather than the last one still resolvable, is what keeps a run's id stable: archiving the root mid-run would otherwise re-point every descendant at the deepest surviving thread and split one run's cost across two ids. Every descendant stops at the same unreachable ancestor, so the tree stays agreed on one id either way.
    string RootId { get; }
    Reactive<string?> Stage { get; }
    Reactive<ThreadStatus> Status { get; }
    IStorage Storage { get; }
    // Unlike Usage, which providers report only once a turn has ended, these counters advance chunk by chunk while generation is in flight. Measured in characters, not tokens. Own-thread only; walk Children for a tree.
    StreamedContent Streamed { get; }
    Reactive<IReadOnlyList<ToolCallEntry>> ToolCallTimeline { get; }
    Reactive<ThreadUsage> Usage { get; }
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
    Task<bool> TransitionAsync(ThreadTransition transition)
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
  static class Checkpoints
    static Task<ThreadCheckpoint> ReadAsync(string path, CancellationToken ct = default)
    // Creates the parent directory if needed and overwrites any existing file.
    static Task WriteAsync(ThreadCheckpoint checkpoint, string path, CancellationToken ct = default)
  abstract record Content
  // Equality is by CONTENT — see Image for the reference-aliasing caveat on Bytes.
  sealed record Content.Audio : Content
    ctor(byte[] Bytes, string MimeType)
    byte[] Bytes { get; init; }
    string MimeType { get; init; }
  // Equality is by CONTENT — see Image for the reference-aliasing caveat on Bytes.
  sealed record Content.Binary : Content
    ctor(byte[] Bytes, string MimeType)
    byte[] Bytes { get; init; }
    string MimeType { get; init; }
  // Equality is by CONTENT: two Image parts with the same MimeType and byte-identical Bytes are equal and hash alike. The buffer is stored by reference, not copied: do not mutate an array after handing it to this record, or its equality and hash change with it.
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
    // Joins all Text parts in order; non-text parts are skipped.
    static string GetText(this IEnumerable<Content> parts)
    static string GetText(this Message message)
    static string GetText(this Artifact artifact)
  sealed record ContextSnapshot
    ctor(string SystemPrompt, IReadOnlyList<Message> History)
    static ContextSnapshot Empty { get; }
    IReadOnlyList<Message> History { get; init; }
    string SystemPrompt { get; init; }
  enum DriveMode
    // Stop on Idle / terminal / awaiting an external signal.
    UntilQuiescent
    // Keep going through Idle; stop on terminal, explicit wait, a stage named "Done", or no-progress.
    UntilStable
  enum DriveOutcome
    // The thread reached a settling state — terminal, awaiting an external signal, Idle (in DriveMode.UntilQuiescent), a stage named "Done", or no forward progress — or another driver already owns it.
    Settled
    // The loop hit its safety pass cap without settling. The thread is still non-terminal and was making changes each pass; treat it as stuck.
    HitSafetyLimit
  static class EmergeToolExtensions
    // First registration of a name wins — a same-named tool already on the pass is skipped. A tool built with Tool.OfContext (or Tool.FromSchema) resolves its live ToolContext through the ambient agent-run scope; invoking one on a pass driven outside an agent run throws.
    static EmergePass<T> AddTool<T>(this EmergePass<T> pass, Tool tool)
    static EmergePass<T> AddTools<T>(this EmergePass<T> pass, params Tool[] tools)
  interface IStageMachine<TState> where TState : struct, Enum
    TState InitialState { get; }
    string Name { get; }
    // Optional: indexes of thread messages to EXCLUDE from the LLM context — superseded content the machine knows is dead weight. To stay cache-friendly, implementations should compute the set ONCE per stage entry and return the same frozen set for every pass within the stage. Default: exclude nothing.
    virtual IReadOnlySet<int> ContextExclusions(AgentThread thread, TState state)
    // Runs BEFORE every pass, ahead of context assembly — the hook for once-per-run context seeding that must precede the first pass so it lives in the cached conversation prefix for the whole run. Implementations dedupe with their own per-run state. Default: no-op.
    virtual Task OnPassStartingAsync(AgentThread thread, TState state, CancellationToken ct)
    // Stage machines are registered per orchestrator and shared by every thread on it — the thread parameter is what lets an implementation keep per-run state without leaking it across runs.
    SkillSet SkillsFor(AgentThread thread, TState state)
    // Optional: advance a resumed thread WITHOUT running an LLM pass — for a purely deterministic next step, e.g. a user approval routing straight to the next stage. A driver calls this when a parked thread is reactivated, BEFORE the next pass. Return a transition to apply it (and skip the pass); return null to fall through to a normal pass. Default: no fast-forward.
    virtual Task<StageTransitionResult<TState>?> TryResumeWithoutPassAsync(AgentThread thread, TState current, CancellationToken ct)
    // Return null to stay in the current stage with no status override; otherwise the runner applies the override (if any) first, then moves to the new stage.
    Task<StageTransitionResult<TState>?> TryTransitionAsync(AgentThread thread, TState current, ThreadEvent evt, CancellationToken ct)
  // Every state mutation must land atomically so a crash mid-tool-call doesn't lose state. The core ships only the in-memory backend; production-grade backends (e.g. Ikon.Agent.Storage.Postgres) implement this interface in separate adapter packages, constructed by the app and passed to Orchestrator.
  interface IStorage
    // Appends to the thread's durable, append-only journal and returns the assigned monotonic sequence. Only JournalCodec.IsJournalable events reach here; Orchestrator.ResumeAsync replays the journal to rebuild a thread's transcript.
    Task<long> AppendJournalAsync(string threadId, ThreadEvent entry, CancellationToken ct = default)
    Task AppendMessageAsync(string threadId, Message message, CancellationToken ct = default)
    // Permanently deletes the plan and everything it owns — its working thread(s), their messages and artifacts. Irreversible (unlike archive). Idempotent: deleting an unknown id is a no-op.
    Task DeletePlanAsync(string id, CancellationToken ct = default)
    // Every app whose persisted status is not Archived.
    IAsyncEnumerable<AppSnapshot> ListAppsAsync(CancellationToken ct = default)
    // Every thread whose persisted status is not Done/Failed/Archived.
    IAsyncEnumerable<ThreadSnapshot> ListNonTerminalAsync(CancellationToken ct = default)
    // Every plan whose persisted status is not Archived.
    IAsyncEnumerable<PlanSnapshot> ListPlansAsync(CancellationToken ct = default)
    // Every finished thread (persisted status Done or Failed); Archived threads are excluded — they were dismissed. Backs Orchestrator.ResumeAsync's optional restore of completed work as history.
    IAsyncEnumerable<ThreadSnapshot> ListTerminalAsync(CancellationToken ct = default)
    // Drops the thread's journal entries and messages — the bulk of a settled thread's storage — while keeping its snapshot, plan and artifacts. For hosts that archive the log elsewhere and keep only a reference. Idempotent: pruning an unknown thread is a no-op.
    Task PruneThreadLogAsync(string threadId, CancellationToken ct = default)
    Task<AppSnapshot?> ReadAppAsync(string id, CancellationToken ct = default)
    // Returns null if no such artifact exists on the thread. When multiple rows share a name (possible only via direct WriteArtifactAsync calls — the AgentThread write path replaces by name), the LATEST write wins; every implementation must honor that.
    Task<Artifact?> ReadArtifactByNameAsync(string threadId, string name, CancellationToken ct = default)
    // Yields the thread's artifacts oldest write first.
    IAsyncEnumerable<Artifact> ReadArtifactsByThreadAsync(string threadId, CancellationToken ct = default)
    // Yields entries in sequence order, only those whose sequence is greater than fromSequence (0 = from the start).
    IAsyncEnumerable<JournalEntry> ReadJournalAsync(string threadId, long fromSequence = 0, CancellationToken ct = default)
    IAsyncEnumerable<Message> ReadMessagesAsync(string threadId, CancellationToken ct = default)
    Task<PlanSnapshot?> ReadPlanAsync(string id, CancellationToken ct = default)
    Task SaveAppAsync(AppSnapshot snapshot, CancellationToken ct = default)
    Task SavePlanAsync(PlanSnapshot snapshot, CancellationToken ct = default)
    Task SaveThreadAsync(ThreadSnapshot snapshot, CancellationToken ct = default)
    Task WriteArtifactAsync(Artifact artifact, CancellationToken ct = default)
  static class JournalCodec
    // Throws on an unknown kind rather than silently dropping a slice of history.
    static ThreadEvent Decode(string kind, string payload)
    // The payload omits the bulk the journal does not carry — binary message/artifact bodies and a pass's system prompt and context window; the message, artifact and run-log stores are the system of record for those.
    static string Encode(ThreadEvent evt)
    // The high-frequency streaming events (TextDelta, TokenUsageUpdated, Progress) are live-only and excluded; everything settled is journaled.
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
    // storage: Persistence backend for apps, plans, threads, messages, and artifacts. A null value selects the non-durable in-memory backend (Storages.InMemory): state lives only for the process, so new Orchestrator() followed by ResumeAsync after a restart re-hydrates nothing. Pass an explicit durable adapter when the tree must survive a restart.
    // hostBudget: Optional host-wide budget merged with each persona's own Budget when computing a thread's remaining budget: the tighter cap wins per kind, and Budget.OnExceeded resolves to the stricter action of the two (Stop > AskUser > Continue) so a host stop-policy is never silently loosened by a persona budget, nor vice versa.
    // llm: Optional LLM backend override; null uses the platform default.
    ctor(IStorage? storage = null, Budget? hostBudget = null, ModelStream? llm = null)
    // Active (non-archived) apps only.
    Reactive<IReadOnlyList<AgentApp>> Apps { get; }
    IAsyncEnumerable<ThreadEvent> Events { get; }
    // Applied to every tool call and every LLM call made by threads under this orchestrator. Default is null — no governance. Set once at boot, before any passes run. Implementations live in Ikon.Guvernor (PolicyGovernanceHook) and elsewhere.
    IGovernanceHook? GovernanceHook { get; set; }
    // Custom Capability × Family → concrete LLMModel routing for every pass under this orchestrator. Default is null — the built-in platform mapping applies. Set once at boot; personas keep their abstract Reasoning, only the resolution changes.
    Func<Reasoning, LLMModel>? ModelResolverHook { get; set; }
    Reactive<IReadOnlyList<Persona>> Personas { get; }
    IStorage Storage { get; }
    // When true, every pass reports generation progress as it streams — surfaced via AgentThread.Streamed — so a host can show that the model is working before the turn ends. Default false; only a host driving a live display benefits from the extra events.
    bool StreamProgress { get; set; }
    Reactive<IReadOnlyList<AgentThread>> Threads { get; }
    Orchestrator AddPersona(Persona persona)
    // Omit id and each call mints a new app; pass a host-recomputable id (a space id, a workspace key) and the same call after ResumeAsync re-uses the persisted app — same plans, threads, and artifacts.
    // name: Display name of the app.
    // brief: What the app is for; seeds the agents working in it.
    // id: Stable identity for the app. Omit it and the app gets a fresh random id, so a host that calls this again after a restart creates a second, empty app. Pass an id the host can recompute from its own state (a space id, a workspace key) and the same call after ResumeAsync re-uses the persisted app — same plans, same threads, same artifacts.
    Task<AgentApp> CreateAppAsync(string name, string brief = "", string? id = null, CancellationToken ct = default)
    // Get-or-create by name: matches the app on appName (default personaName) and the plan on planName, so a repeated call after ResumeAsync returns the SAME persisted thread. seedTask is posted only when the plan is first created, never onto an existing history. A repeated call naming a DIFFERENT personaName for an existing plan throws InvalidOperationException rather than returning the other persona's thread.
    Task<AgentThread> CreateThreadAsync(string personaName, Content seedTask, string? appName = null, string planName = "main", CancellationToken ct = default)
    ValueTask DisposeAsync()
    AgentApp? GetApp(string id)
    // A thread is evicted from the live registry once it reaches a terminal status (Done, Failed, or Archived), so this returns null for a completed run — read a finished run's history from storage instead.
    AgentThread? GetThread(string threadId)
    Orchestrator RegisterStageMachine<TState>(IStageMachine<TState> machine) where TState : struct, Enum
    // Mints fresh ids throughout, so one checkpoint can be restored many times as independent runs. The returned thread carries the checkpoint's status (typically ThreadStatus.Idle); re-engage it with AgentThread.ReactivateIfIdleAsync + RunPassAsync or AgentThread.DriveAsync. Throws if the checkpoint's persona is not registered.
    Task<AgentThread> RestoreCheckpointAsync(ThreadCheckpoint checkpoint, CancellationToken ct = default)
    // Idempotent — safe to call once at startup, a no-op for in-memory storage. Loads in dependency order (apps, then plans, then threads); an unknown persisted stage throws InvalidOperationException immediately.
    Task ResumeAsync(bool includeTerminal = false, CancellationToken ct = default)
    // Throws InvalidOperationException if the thread is still ThreadStatus.Pending, or if no persona named AgentThread.AgentName is registered.
    Task RunPassAsync(AgentThread thread, CancellationToken ct = default)
  // A tool that returns media claims a slot per artifact and degrades to text when the claim is refused. The budget is per pass: it resets on the next one.
  sealed class PassMediaBudget
    ctor()
    bool HasHeadroom { get; }
    // Returns false when the same artifact was already shown this pass (the model should scroll up instead) — alreadyShown distinguishes that from budget exhaustion.
    bool TryClaim(string artifactName, out bool alreadyShown)
    const int MaxImagesPerPass = 3
  // Role is "user" or the agent name. This is the model's INPUT — it does not include the pass's own reply (that is PassRecord.AssistantText). Non-text parts are omitted.
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
  // Result is the tool's result rendered to string (empty if the pass ended before the result came back). CallId is the LLM's stable id for the call — the identity used to fold a result back onto the right call, since a pass can fire several same-named calls and matching by name alone scrambles question/answer pairs.
  sealed record PassToolCall
    ctor(string CallId, string Name, string ParametersJson, string Result)
    string CallId { get; init; }
    string Name { get; init; }
    string ParametersJson { get; init; }
    string Result { get; init; }
  sealed record Persona
    // Reasoning: How this agent thinks — an abstract Capability × ModelFamily choice, resolved to a concrete LLMModel per pass; see Reasoning for the mapping.
    // NudgeOnAssistantStall: Set true only for agents that must keep working when the drive re-invokes a thread whose last turn was their own tool-less message (the runner appends a bounded user continuation to unstall them); leave it false for agents that legitimately conclude with a text turn, or nudging makes them loop forever.
    ctor(string Name, string SystemPrompt, IReadOnlyList<Skill> Skills, Reasoning Reasoning, Budget? Budget = null, bool NudgeOnAssistantStall = false, IReadOnlySet<string>? TranscriptOnlyPayloadKinds = null)
    Budget? Budget { get; init; }
    string Name { get; init; }
    bool NudgeOnAssistantStall { get; init; }
    Reasoning Reasoning { get; init; }
    IReadOnlyList<Skill> Skills { get; init; }
    string SystemPrompt { get; init; }
    IReadOnlySet<string>? TranscriptOnlyPayloadKinds { get; init; }
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

namespace Ikon.Agent.Skills
  static class Built
    static readonly Skill Attachments
    static readonly Skill Messaging
    static readonly Skill Termination
    static readonly Skill UserDecision
    static readonly Skill WebSearch
  // One Tool per tool the MCP server advertises, built schema-first from the server's own JSON input schema. The client must have been connected via McpClient.ConnectAsync before the skill's tools are enumerated. Tool names are made LLM-safe by replacing dots with underscores; calls go to the server under the original name.
  sealed class McpSkill : Skill
    ctor(McpClient mcpClient, string name = "mcp", string instructions = "")
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  // Kind discriminates the shape: "decision" (default) — Options is non-empty, the host renders buttons, and the user's pick posts back as a UserDecisionResponse; "clarification" — Options is empty, the host renders a free-text prompt, and the user's typed answer posts back as a plain user message.
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
  // The host posts a user Message with PayloadKind = UserDecisionProtocol.ResponsePayloadKind and Payload = JSON of this record; the agent reads it on its next pass.
  sealed record UserDecisionResponse
    ctor(string Choice)
    string Choice { get; init; }
