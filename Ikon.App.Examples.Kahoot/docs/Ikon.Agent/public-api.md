# Ikon.Agent Public API

namespace Ikon.Agent
  // What the agent is doing in this moment within an Active status. Distinct from ThreadStatus, which is the lifecycle state. Flat record + enum so consumers assign and pattern-match the same way: set with Activity.Idle / Activity.RunningTool("name"); match with activity.Kind == ActivityKind.Idle or activity is { Kind: ActivityKind.RunningTool, Tool: var t }. Tool is set only when Kind is ActivityKind.RunningTool; null otherwise.
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
  // A project container. Owns many AgentPlans, each a focused work unit with its own working thread. App is the user-facing concept ("Todo app", "Voice notes app"). Plans within an app evolve independently and can be created, archived, restored.
  sealed class AgentApp : IAsyncDisposable
    // All plans on this app, INCLUDING archived ones — for an "archived" view that can restore them. The reactive Plans is the active subset; this is a point-in-time snapshot.
    IReadOnlyList<AgentPlan> AllPlans { get; }
    // User's brief / pitch for the app; reactive for live UI binding.
    Reactive<string> Brief { get; }
    DateTime CreatedAt { get; }
    string Id { get; }
    // User-facing app name; reactive so UI rename surfaces live.
    Reactive<string> Name { get; }
    // Active (non-archived) plans on this app, in creation order.
    Reactive<IReadOnlyList<AgentPlan>> Plans { get; }
    // ThreadStatus.Active or ThreadStatus.Archived. Apps don't have the rich FSM that Threads do — only the two states.
    Reactive<ThreadStatus> Status { get; }
    // Archive this app — cascades to every owned plan (which archives every owning thread). Visible removal from Orchestrator.Apps.
    Task ArchiveAsync()
    // Create a new plan on this app with its 1:1 working thread. The thread runs personaName (must be registered on the orchestrator) and is seeded with seedTask.
    Task<AgentPlan> CreatePlanAsync(string name, string personaName, Content seedTask, ThreadOptions? options = null, CancellationToken ct = default)
    ValueTask DisposeAsync()
    AgentPlan? GetPlan(string id)
    // Permanently remove a plan from this app — drop it from the in-memory list, delete it from durable storage, and dispose its working thread. Irreversible (unlike AgentPlan.ArchiveAsync).
    Task RemovePlanAsync(AgentPlan plan)
    Task RenameAsync(string name)
    Task RestoreAsync()
    Task UpdateBriefAsync(string brief)
  // Typed agent call — spawns a child thread off parent, primes it with caller-supplied inputs, drives it to completion, and returns a typed T extracted from the child's final state. Models a "stage" as a function with a required return value, parallel to Emerge.Run<T> but with multi-turn tool use and stage skills.
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
  // Per-call telemetry captured at AgentCall.RunAsync<T> exit. Surfaces the structural signals — per-call tokens, wall time, pass count — that outcome-only metrics (pass count, didItConverge) hide. Forwarded to the caller via AgentCallSpec<T>.OnComplete; the caller is responsible for aggregation, JSONL output, or whatever shape the run record takes downstream.
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
  // Specification for one AgentCall.RunAsync<T>. The caller supplies (a) the seed task that opens the child thread, (b) a primer that posts typed inputs into the child's context, (c) a result extractor that reads child state and returns T when ready, and (d) the upper bound on passes before bail.
  sealed record AgentCallSpec<T> where T : class
    // SeedTask: First user-message content posted to the child thread on spawn. Typically the high-level task statement ("Grade the code against the plan and call verdict() exactly once").
    // PrimeAsync: Posts the typed inputs after spawn (current file content, build output, plan sections). Anything the agent needs that isn't already in the seed task. Called once, before any agent pass runs.
    // ExtractResult: Returns the typed result when the agent has produced it, or null if not ready. Called after each pass. Common shapes: read a named artifact, parse a structured JSON in the last agent message, check a custom thread predicate.
    // MaxPasses: Hard ceiling on passes for this call. Each pass is one LLM turn (possibly multi-step internally). Budget from the persona's own Budget applies on top.
    // ThreadOptions: Optional ThreadOptions to override per-call (e.g. stage-machine name, kernel context).
    // OnComplete: Optional completion/telemetry callback invoked once as the call exits — on both the success and the failure path — with the AgentCallRecord of per-call tokens, wall time, turns, and whether a result was produced. The caller owns any aggregation or output; leave null to capture nothing.
    ctor(Content SeedTask, Func<AgentThread, CancellationToken, Task> PrimeAsync, Func<AgentThread, Task<T?>> ExtractResult, int MaxPasses = 16, ThreadOptions? ThreadOptions = null, Action<AgentCallRecord>? OnComplete = null)
    // Returns the typed result when the agent has produced it, or null if not ready. Called after each pass. Common shapes: read a named artifact, parse a structured JSON in the last agent message, check a custom thread predicate.
    Func<AgentThread, Task<T?>> ExtractResult { get; init; }
    // Hard ceiling on passes for this call. Each pass is one LLM turn (possibly multi-step internally). Budget from the persona's own Budget applies on top.
    int MaxPasses { get; init; }
    // Optional completion/telemetry callback invoked once as the call exits — on both the success and the failure path — with the AgentCallRecord of per-call tokens, wall time, turns, and whether a result was produced. The caller owns any aggregation or output; leave null to capture nothing.
    Action<AgentCallRecord>? OnComplete { get; init; }
    // Posts the typed inputs after spawn (current file content, build output, plan sections). Anything the agent needs that isn't already in the seed task. Called once, before any agent pass runs.
    Func<AgentThread, CancellationToken, Task> PrimeAsync { get; init; }
    // First user-message content posted to the child thread on spawn. Typically the high-level task statement ("Grade the code against the plan and call verdict() exactly once").
    Content SeedTask { get; init; }
    // Optional ThreadOptions to override per-call (e.g. stage-machine name, kernel context).
    ThreadOptions? ThreadOptions { get; init; }
  // A focused work unit within an AgentApp. Owns exactly one working AgentThread (1:1). Holds a sectioned document via Sections — the plan content the agent is building toward. Plans evolve independently; multiple plans on one app run in parallel.
  sealed class AgentPlan : IAsyncDisposable
    AgentApp App { get; }
    string AppId { get; }
    DateTime CreatedAt { get; }
    string Id { get; }
    // Plan name; reactive for live UI binding.
    Reactive<string> Name { get; }
    // Aggregate score across sections: min of all sections with a score, or null if none have one. A plan is only as solid as its weakest scored axis.
    Reactive<double?> Score { get; }
    // The plan's document — named sections of content.
    Reactive<IReadOnlyDictionary<string, PlanSection>> Sections { get; }
    // ThreadStatus.Active or ThreadStatus.Archived. Reflects the plan's own lifecycle. Mirrors archival from the underlying thread automatically; the rich FSM (Idle/WaitingForInput/etc.) is on Thread.AgentThread.Status.
    Reactive<ThreadStatus> Status { get; }
    AgentThread Thread { get; }
    Task ArchiveAsync()
    ValueTask DisposeAsync()
    PlanSection? ReadSection(string name)
    Task RenameAsync(string name)
    Task RestoreAsync()
    // Set the prose content on name, preserving any existing score on that section. Used by the Planner's content writes; orthogonal to UpdateScoreAsync.
    Task<PlanSection> UpdateContentAsync(string name, string content)
    // Set the convergence score on name, preserving the section's existing content. Creates the section with empty content if it didn't exist (rare; usually the Scorer scores existing sections).
    Task<PlanSection> UpdateScoreAsync(string name, double score)
  // Every usage row an Ikon.AI call reports carries the ambient scope stack, and the platform's cost reporting can filter and group on it. Stamping the run here is what lets a host ask the platform what one agent run actually cost in credits, instead of reconstructing a number from token counts and a private price table. The value is always the root thread id, so a run's sub-threads — a validator, a browser operator, an ad-hoc sub-agent — fall under the run that spawned them rather than costing themselves separately.
  static class AgentScopes
    // Scope type carrying the root thread id of the run that emitted the usage.
    const string AgentRun
  // One conversation/task unit. Owns its own budget, message history, artifacts, sub-tree, and event stream — the thread is the scope unit. All observable state is exposed as Reactive<T> for direct UI binding. Lifecycle is driven through TransitionAsync; the FSM is the single source of truth (Idle and WaitingFor* serve as pause states).
  sealed class AgentThread : IAsyncDisposable
    Reactive<IReadOnlyList<ToolInfo>> ActiveTools { get; }
    Reactive<Activity> Activity { get; }
    string AgentName { get; }
    Reactive<IReadOnlyList<Artifact>> Artifacts { get; }
    Reactive<IReadOnlyList<AgentThread>> Children { get; }
    Reactive<ContextSnapshot> Context { get; }
    DateTime CreatedAt { get; }
    IAsyncEnumerable<ThreadEvent> Events { get; }
    // Why the thread last entered ThreadStatus.Failed — the exception message from a failed pass, or a budget-stop reason. Null until a failure occurs. Set by the runner; read by hosts/drivers so a failed run can report a concrete reason instead of just "failed".
    Reactive<string?> FailureReason { get; }
    string Id { get; }
    // True while a drive loop currently OWNS this thread (the single-consumer drive lock is held). The official "is anything advancing this thread right now" signal — hosts that latched their own "orphaned by restart" flags got it wrong in both directions across reloads. Not reactive; pair it with a Status read (which is) so consumers re-evaluate on every settle.
    bool IsBeingDriven { get; }
    // Reactive view of every Message posted to this thread, in arrival order. Seeded from storage at construction and appended to in place on every MessagePosted; UI consumers bind directly instead of maintaining their own per-thread bag.
    Reactive<IReadOnlyList<Message>> Messages { get; }
    string? ParentId { get; }
    // Throws InvalidOperationException if the plan has been archived out of the live registry; reach it via App + AgentApp.GetPlan when archived-plan access is needed.
    AgentPlan Plan { get; }
    string PlanId { get; }
    // Per-kind remaining budget, derived from Usage + the merged host/persona Budget. Recomputed on every usage change so UI consumers (and budget-aware skills) can bind directly instead of polling the orchestrator.
    Reactive<BudgetRemaining> RemainingBudget { get; }
    // Walks ParentId through the orchestrator's live registry. A parent the registry no longer holds — archived, or not yet re-hydrated — ends the walk at that parent's id rather than throwing, because attribution must never be able to fail a run. Naming the unreachable ancestor, rather than the last one still resolvable, is what keeps a run's id stable: archiving the root mid-run would otherwise re-point every descendant at the deepest surviving thread and split one run's cost across two ids. Every descendant stops at the same unreachable ancestor, so the tree stays agreed on one id either way.
    string RootId { get; }
    Reactive<string?> Stage { get; }
    Reactive<ThreadStatus> Status { get; }
    // Convenience accessor for the orchestrator's persistence backend.
    IStorage Storage { get; }
    // How much content has streamed off the model on this thread so far, split by kind. Unlike Usage — which providers only report once a turn has ended, and which therefore cannot say whether the model is working *now* — these counters advance chunk by chunk while generation is in flight. Measured in characters, not tokens: the wire carries text, and converting to tokens would mean guessing. Intended for progress indicators, which want a rate rather than an exact count. Own-thread only, like Usage; walk Children for a tree.
    StreamedContent Streamed { get; }
    // Reactive timeline of every ToolCallStarted + ToolCallCompleted pair on this thread. Each entry remembers how many agent messages had landed at the time it started, which lets UI consumers slot the tool-call rows into the conversation in front of the message they produced (Claude-Code-style transparency).
    Reactive<IReadOnlyList<ToolCallEntry>> ToolCallTimeline { get; }
    Reactive<ThreadUsage> Usage { get; }
    // Returns a Reference to embed in a Message in place of the raw bytes, keeping subsequent LLM prompts small; the agent fetches the data via tool calls when it needs it.
    Task<Content.Reference> AttachAsArtifactAsync(string name, string mimeType, byte[] bytes, ArtifactSource source = User)
    // Attachments larger than inlineThresholdBytes are promoted to thread artifacts and replaced with a Reference; smaller items embed inline so the model sees them directly.
    Task<Message> BuildUserMessageAsync(string text, IReadOnlyList<AttachmentInput> attachments, long inlineThresholdBytes = 262144, CancellationToken ct = default)
    // Capture this thread's full state — its persisted snapshot, the plan it is working, every message, and every artifact — into a restorable ThreadCheckpoint. Read-only; the live thread keeps running. Restore later (into this or any orchestrator) with Orchestrator.RestoreCheckpointAsync — the basis for running one stage once and then iterating on the next stage from a fixed upstream snapshot.
    Task<ThreadCheckpoint> CheckpointAsync(string label, CancellationToken ct = default)
    ValueTask DisposeAsync()
    // Use DriveMode.UntilStable whenever a stage machine is registered; otherwise DriveMode.UntilQuiescent.
    // onPass: Optional hook invoked after each successful pass (e.g. to refresh host-side UI lists). Receives the thread.
    // safety: Maximum passes before bail-out. Defaults to 256.
    Task<DriveOutcome> DriveAsync(DriveMode mode = UntilQuiescent, Func<AgentThread, Task>? onPass = null, int safety = 256, CancellationToken ct = default)
    // Make the thread Active from whatever non-running state it is in: Activate from Pending (a thread that has never run), Reactivate from Idle / WaitingForInput / WaitingForChildren / Done / Failed. Returns false only when the thread is already Active or archived. Use this, not ReactivateIfIdleAsync, when resuming a thread whose state you don't control. Reactivate is rejected from Pending — the matrix has no such arc — so a host that reaches for the obvious "re-engage" call silently fails to start a never-run thread, and Orchestrator.RunPassAsync then throws because it is still Pending. Only DriveAsync auto-activates, and drivers that own their own pass loop (codegen) deliberately bypass it.
    Task<bool> EnsureActiveAsync()
    // Run a drive loop under exclusive per-thread ownership, coalescing re-entrant calls. If a drive is already advancing this thread, the call flags a re-drive and returns immediately — the active loop re-runs driveLoop before exiting, so freshly-posted input is never stranded and two drives can never run at once. This is the single serialisation point for a thread's execution; every drive (the built-in DriveAsync, codegen drivers, host loops) should route through here rather than guarding concurrency itself.
    Task EnsureDrivenAsync(Func<CancellationToken, Task> driveLoop, CancellationToken ct = default)
    Task<Artifact?> GetArtifactAsync(string name)
    // True iff an artifact with this name exists on the thread. Equivalent to await GetArtifactAsync(name) is not null but reads cleaner in stage- machine transition guards ("don't advance until the stage produced X").
    Task<bool> HasArtifactAsync(string name)
    // Append a message to the thread (user input, seed task, or an agent-generated reply from the runner). Persists to storage and fires MessagePosted; the thread's Messages reactive picks the new message up before the event fans out to subscribers.
    Task PostAsync(Message msg, CancellationToken ct = default)
    // If the thread is paused in a state that accepts ThreadTransition.Reactivate (Idle, WaitingForInput, WaitingForChildren, Done, Failed), reactivate it. No-op (returns false) when the thread is already Active, in a terminal-archive state, or still Pending (created with InitialStatus: Pending and never run) — the transition matrix has no Reactivate arc from those states; a Pending thread's first turn goes through EnsureActiveAsync or DriveAsync. Idiomatic "user posted a follow-up message, re-engage the agent" call.
    Task<bool> ReactivateIfIdleAsync()
    // Read a typed artifact written by WriteArtifactAsync<T>. Returns null when it's absent or its payload doesn't deserialize to T.
    Task<T?> ReadArtifactAsync<T>(string name)
    // This waits for the children to reach a terminal state (Done / Failed); it does NOT run them. Spawning creates each child Active with its seed task posted, but no library background loop advances it — so the caller MUST drive every spawned child to completion CONCURRENTLY, on its own task, or this call blocks until ct fires (WaitMode.All) or until a child that was already terminal at spawn satisfies WaitMode.Any. Because this method itself blocks the current thread, the drives must already be in flight — e.g. start child.DriveAsync(...) (and a terminating step such as TransitionAsync(ThreadTransition.Complete)) on background tasks before or as the children are spawned, or spawn from a host loop that owns the driving. A driver that only reaches Idle never satisfies the wait: the children must be driven all the way to a terminal status.
    Task<IReadOnlyList<AgentThread>> SpawnAllAsync(IReadOnlyList<SpawnSpec> children, WaitMode wait = All, CancellationToken ct = default)
    // The child is created Active with its seed task posted, but it is NOT driven — no library background loop advances it. Nothing runs until the caller drives it: await child.DriveAsync(...) (fire-and-forget on a separate task if this thread should not block), or a host pass loop. Read the child's state (artifacts, messages) only after a drive has advanced it. Left undriven the child sits idle forever. AgentCall.RunAsync<T> is the spawn-drive-extract convenience when a typed result is wanted.
    Task<AgentThread> SpawnAsync(string personaName, Content task, ThreadOptions? options = null, CancellationToken ct = default)
    // Returns false (no-op) if the transition is invalid from the current status. The new status reaches consumers via the Status reactive — there is no separate transition event.
    Task<bool> TransitionAsync(ThreadTransition transition)
    // Replace-by-name: writing a name that already exists updates that artifact's storage row in place rather than adding a duplicate. Use for structured outputs; for raw bytes use AttachAsArtifactAsync.
    Task<Artifact> WriteArtifactAsync(string name, string type, IReadOnlyList<Content> parts, ArtifactSource source = Agent)
    // Write a typed value as a durable artifact — JSON-serialized into one text part. The typed counterpart to WriteArtifactAsync: structured data one stage produces and a later stage reads back with ReadArtifactAsync<T>, instead of hand-rolling JSON. Mark it ArtifactSource.System for control data that shouldn't show in the user-facing artifact list. Persists, rehydrates on resume, and is captured in checkpoints like any artifact.
    Task<Artifact> WriteArtifactAsync<T>(string name, T value, ArtifactSource source = Agent)
    // Default inline-vs-artifact cutoff for BuildUserMessageAsync: 256 KB.
    const long DefaultInlineThresholdBytes = 262144
  // Persisted shape of an AgentApp.
  sealed record AppSnapshot
    ctor(string Id, string Name, string Brief, ThreadStatus Status, DateTime CreatedAt, DateTime UpdatedAt)
    string Brief { get; init; }
    DateTime CreatedAt { get; init; }
    string Id { get; init; }
    string Name { get; init; }
    ThreadStatus Status { get; init; }
    DateTime UpdatedAt { get; init; }
  // Durable, addressable, named output. Multimodal via the same Content union as messages.
  sealed record Artifact
    ctor(string Id, string Name, string Type, IReadOnlyList<Content> Parts, string ThreadId, ArtifactSource Source = Agent)
    string Id { get; init; }
    string Name { get; init; }
    IReadOnlyList<Content> Parts { get; init; }
    ArtifactSource Source { get; init; }
    string ThreadId { get; init; }
    string Type { get; init; }
  // Who created an artifact. Lets skills tell user uploads apart from agent-written outputs (plan / design / etc.) without having to blacklist names.
  enum ArtifactSource
    // Agent wrote it (plan, design, generated code).
    Agent
    // User uploaded it through the host UI.
    User
    // System-owned (cached read_attachment results, derived bundles).
    System
  // One attachment to be turned into a multimodal Content part. Used by AgentThread.BuildUserMessageAsync to auto-promote large items to Artifact storage while keeping small items inline.
  sealed record AttachmentInput
    ctor(byte[] Bytes, string MimeType, string? Name = null)
    byte[] Bytes { get; init; }
    string MimeType { get; init; }
    string? Name { get; init; }
  // Who authored a message. Flat struct; equality is by (Kind, Name).
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
  // Per-kind remaining budget snapshot. Each field is the cap minus current usage for that kind, or null if no cap is set. Effective is the merged cap (host + per-persona) the runtime is enforcing.
  sealed record BudgetRemaining
    ctor(long? InputTokensRemaining, long? CachedInputTokensRemaining, long? CacheCreationInputTokensRemaining, long? OutputTokensRemaining, TimeSpan? WallTimeRemaining, int? TurnsRemaining, Budget? Effective)
    long? CacheCreationInputTokensRemaining { get; init; }
    long? CachedInputTokensRemaining { get; init; }
    Budget? Effective { get; init; }
    long? InputTokensRemaining { get; init; }
    long? OutputTokensRemaining { get; init; }
    int? TurnsRemaining { get; init; }
    TimeSpan? WallTimeRemaining { get; init; }
  // Detail of which budget cap tripped. Carried by BudgetExceeded on the thread's (and the orchestrator's) event stream.
  sealed record BudgetSnapshot
    ctor(string TrippedField, long ActualValue, long LimitValue)
    long ActualValue { get; init; }
    long LimitValue { get; init; }
    string TrippedField { get; init; }
  enum Capability
    Quick
    Standard
    Deep
  // Read/write ThreadCheckpoints as JSON files. Capture a checkpoint with AgentThread.CheckpointAsync, persist it here, and bring it back into any orchestrator with Orchestrator.RestoreCheckpointAsync.
  static class Checkpoints
    // Read a checkpoint back from a JSON file.
    static Task<ThreadCheckpoint> ReadAsync(string path, CancellationToken ct = default)
    // Write a checkpoint to path as JSON, creating the parent directory if needed and overwriting any existing file.
    static Task WriteAsync(ThreadCheckpoint checkpoint, string path, CancellationToken ct = default)
  // Multimodal content union. Used by both Message.Parts and Artifact.Parts. Vision-capable models receive Image cases directly without wrapper code.
  abstract record Content
  // Inline audio bytes plus mime type. Equality is by CONTENT — see Image for the reference-aliasing caveat on Bytes.
  sealed record Content.Audio : Content
    ctor(byte[] Bytes, string MimeType)
    byte[] Bytes { get; init; }
    string MimeType { get; init; }
  // Inline opaque bytes plus mime type. Equality is by CONTENT — see Image for the reference-aliasing caveat on Bytes.
  sealed record Content.Binary : Content
    ctor(byte[] Bytes, string MimeType)
    byte[] Bytes { get; init; }
    string MimeType { get; init; }
  // Inline image bytes plus mime type. Equality is by CONTENT: two Image parts with the same MimeType and byte-identical Bytes are equal and hash alike — the default record equality would compare the array by reference and so break dedup, caching and with-diffing over identical content. The buffer is stored by reference, not copied: do not mutate an array after handing it to this record, or its equality and hash change with it.
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
  // Shared helpers over multimodal content. GetText joins every Text part in order; non-text parts are skipped.
  static class ContentExtensions
    // Join all Text parts in order; ignores non-text parts.
    static string GetText(this IEnumerable<Content> parts)
    // Convenience: GetText over a Message's parts.
    static string GetText(this Message message)
    // Convenience: GetText over an Artifact's parts.
    static string GetText(this Artifact artifact)
  // What the LLM saw on the last (or in-flight) turn — the resolved system prompt and the message history fed to the kernel. Updates each turn; bind to AgentThread.Context for the reactive view. Tool schemas are exposed separately via AgentThread.ActiveTools.
  sealed record ContextSnapshot
    ctor(string SystemPrompt, IReadOnlyList<Message> History)
    static ContextSnapshot Empty { get; }
    IReadOnlyList<Message> History { get; init; }
    string SystemPrompt { get; init; }
  // Loop discipline for AgentThread.DriveAsync. Pick UntilQuiescent for one-shot agents that should yield to the user on first Idle; pick UntilStable for stage-machine pipelines that produce multiple passes per host call.
  enum DriveMode
    // Stop on Idle / terminal / awaiting an external signal.
    UntilQuiescent
    // Keep going through Idle; stop on terminal, explicit wait, a stage named "Done", or no-progress.
    UntilStable
  // Why a AgentThread.DriveAsync loop stopped. Lets a caller tell a genuinely settled run apart from one that ran out its per-drive safety cap — the latter is a stuck/unproductive thread that looped to the ceiling, not a finished one, and would otherwise look identical (both just return).
  enum DriveOutcome
    // The thread reached a settling state — terminal, awaiting an external signal, Idle (in DriveMode.UntilQuiescent), a stage named "Done", or no forward progress — or another driver already owns it.
    Settled
    // The loop hit its safety pass cap without settling. The thread is still non-terminal and was making changes each pass; treat it as stuck.
    HitSafetyLimit
  // Registers Tools directly on an EmergePass, so raw Emerge.Run callers get the same registration path (governance policy, user-source parameter names, description/allowed-value overrides, read-only marking) as an agent run. Pre-built Functions go straight onto pass.Tools instead — this surface is for the Tool vocabulary.
  static class EmergeToolExtensions
    // Adds the tool to the pass unless a tool with the same name is already registered — first registration of a name wins. A tool built with Tool.OfContext (or Tool.FromSchema) resolves its live ToolContext through the ambient agent-run scope; invoking one on a pass driven outside an agent run throws with a diagnostic explaining the missing scope.
    static EmergePass<T> AddTool<T>(this EmergePass<T> pass, Tool tool)
    // Adds each tool via AddTool<T>, keeping its skip-if-name-already-present dedupe semantics per tool.
    static EmergePass<T> AddTools<T>(this EmergePass<T> pass, params Tool[] tools)
  // Drives which SkillSet is active per stage. TState is the user-defined stage enum (e.g. enum CodegenStage { Plan, Code, Critic }).
  interface IStageMachine<TState> where TState : struct, Enum
    TState InitialState { get; }
    string Name { get; }
    // Optional: indexes of thread messages to EXCLUDE from the LLM context — superseded gate content the machine knows is dead weight (an old build report replaced by a newer one, a consumed rework directive). To stay cache-friendly, implementations should compute the set ONCE per stage entry and return the same frozen set for every pass within the stage — a stage swap invalidates the provider's cached prefix anyway, so that is the free moment to prune. Default: exclude nothing.
    virtual IReadOnlySet<int> ContextExclusions(AgentThread thread, TState state)
    // Runs BEFORE every pass, ahead of context assembly — the hook for once-per-run context seeding (e.g. posting a large reference message that must precede the FIRST pass so it lives in the cached conversation prefix for the whole run). Implementations dedupe with their own per-run state. Default: no-op.
    virtual Task OnPassStartingAsync(AgentThread thread, TState state, CancellationToken ct)
    // Skills active in the given stage for the given thread. Stage machines are registered per orchestrator and shared by every thread on it — the thread parameter is what lets an implementation keep per-run state without leaking it across runs.
    SkillSet SkillsFor(AgentThread thread, TState state)
    // Optional: advance a resumed thread WITHOUT running an LLM pass. A driver calls this when a parked thread is reactivated, BEFORE the next pass. Return a transition to apply it (and skip the pass); return null to fall through to a normal pass. Use this when the next step is purely deterministic — e.g. a user approval that should route straight to the next stage instead of burning a waiting-room pass first. The directive for the resulting stage (if any) is the machine's responsibility, exactly as in TryTransitionAsync. Default: no fast-forward.
    virtual Task<StageTransitionResult<TState>?> TryResumeWithoutPassAsync(AgentThread thread, TState current, CancellationToken ct)
    // Inspect the latest event and decide the next stage + optional status transition. Return null to stay in the current stage with no status override; otherwise the runner applies the override (if any) first, then moves to the new stage.
    Task<StageTransitionResult<TState>?> TryTransitionAsync(AgentThread thread, TState current, ThreadEvent evt, CancellationToken ct)
  // Transactional durability seam. Every state mutation lands atomically so a crash mid-tool-call doesn't lose state. The core ships only the in-memory backend (no external dependencies — natural default for tests and ephemeral apps). Production-grade backends ship as separate adapter packages (e.g. Ikon.Agent.Storage.Postgres) that implement this interface; apps construct them directly and pass to Orchestrator. The core never references a specific backend.
  interface IStorage
    // Append entry to the thread's durable, append-only journal and return its assigned monotonic sequence. The journal is the sequenced record of settled thread events — tool calls, messages, artifacts, pass telemetry — that Orchestrator.ResumeAsync replays to rebuild a thread's transcript. Only JournalCodec.IsJournalable events reach here.
    Task<long> AppendJournalAsync(string threadId, ThreadEvent entry, CancellationToken ct = default)
    Task AppendMessageAsync(string threadId, Message message, CancellationToken ct = default)
    // Permanently delete a plan and everything it owns — its working thread(s), their messages and artifacts. Irreversible (unlike archive). Idempotent: deleting an unknown id is a no-op.
    Task DeletePlanAsync(string id, CancellationToken ct = default)
    // Returns every app whose persisted status is not Archived.
    IAsyncEnumerable<AppSnapshot> ListAppsAsync(CancellationToken ct = default)
    // Returns every thread whose persisted status is not Done/Failed/Archived.
    IAsyncEnumerable<ThreadSnapshot> ListNonTerminalAsync(CancellationToken ct = default)
    // Returns every plan whose persisted status is not Archived.
    IAsyncEnumerable<PlanSnapshot> ListPlansAsync(CancellationToken ct = default)
    // Returns every finished thread (persisted status Done or Failed). Archived threads are excluded — they were dismissed. Used by Orchestrator.ResumeAsync to optionally restore completed work as history (a host that shows a change log, e.g. Studio), which the default resume path skips for speed.
    IAsyncEnumerable<ThreadSnapshot> ListTerminalAsync(CancellationToken ct = default)
    // Drop the thread's journal entries and messages — the bulk of a settled thread's storage — while keeping its snapshot, plan and artifacts. For hosts that archive the log elsewhere (e.g. a compressed export in asset storage) and keep only a reference; re-importing goes through AppendMessageAsync and AppendJournalAsync. Idempotent: pruning an unknown thread is a no-op.
    Task PruneThreadLogAsync(string threadId, CancellationToken ct = default)
    Task<AppSnapshot?> ReadAppAsync(string id, CancellationToken ct = default)
    // Look up an artifact by (threadId, name). Returns null if no such artifact exists on the thread. When multiple rows share a name (possible only via direct WriteArtifactAsync calls — the AgentThread write path replaces by name), the LATEST write wins; every implementation must honor that so "the current one" semantics hold across backends. Used by AgentThread.GetArtifactAsync as the storage fallback when the in-memory cache misses (e.g. a sub-thread wrote it before the parent's notification propagated).
    Task<Artifact?> ReadArtifactByNameAsync(string threadId, string name, CancellationToken ct = default)
    // Enumerate every artifact stored for the given thread, oldest write first.
    IAsyncEnumerable<Artifact> ReadArtifactsByThreadAsync(string threadId, CancellationToken ct = default)
    // Replay a thread's journal in sequence order, yielding only entries whose sequence is greater than fromSequence (0 = from the start).
    IAsyncEnumerable<JournalEntry> ReadJournalAsync(string threadId, long fromSequence = 0, CancellationToken ct = default)
    IAsyncEnumerable<Message> ReadMessagesAsync(string threadId, CancellationToken ct = default)
    Task<PlanSnapshot?> ReadPlanAsync(string id, CancellationToken ct = default)
    Task SaveAppAsync(AppSnapshot snapshot, CancellationToken ct = default)
    Task SavePlanAsync(PlanSnapshot snapshot, CancellationToken ct = default)
    Task SaveThreadAsync(ThreadSnapshot snapshot, CancellationToken ct = default)
    Task WriteArtifactAsync(Artifact artifact, CancellationToken ct = default)
  // Serializes ThreadEvents to and from their journal payloads and decides which events are durable. Public so out-of-assembly IStorage backends (e.g. the Postgres store) encode/decode identically to the in-memory one. Reuses the checkpoint serializer options so the message/artifact content union and enums round-trip the same way they do in ThreadCheckpoint.
  static class JournalCodec
    // Rebuild an event from its stored kind + payload. Throws on an unknown kind rather than silently dropping a slice of history.
    static ThreadEvent Decode(string kind, string payload)
    // Serialize an event to its JSON payload, minus the bulk the journal does not carry — see WithoutBulk.
    static string Encode(ThreadEvent evt)
    // True when evt carries durable state worth journaling. The high-frequency streaming events (TextDelta, TokenUsageUpdated, Progress) are live-only — journaling them per token would flood the log — so they are excluded. Everything settled (messages, artifacts, tool calls, pass telemetry, budget trips, skill events, completion) is journaled.
    static bool IsJournalable(ThreadEvent evt)
    // Discriminator for evt — its concrete subtype name.
    static string Kind(ThreadEvent evt)
  // One sequenced entry in a thread's append-only journal — the durable, replayable record of a settled ThreadEvent. Sequence is monotonic (assigned by IStorage.AppendJournalAsync); replaying a thread's entries in sequence order reconstructs its history — tool calls, messages, artifacts, pass telemetry. The sequence lives on this envelope, not on ThreadEvent: an event is produced before it is sequenced, so ordering is a storage concern, not intrinsic to the event.
  sealed record JournalEntry
    ctor(long Sequence, string ThreadId, DateTime At, string Kind, ThreadEvent Event)
    DateTime At { get; init; }
    ThreadEvent Event { get; init; }
    string Kind { get; init; }
    long Sequence { get; init; }
    string ThreadId { get; init; }
  // A turn in the conversation. Parts is the multimodal carrier (no separate Attachments). Optional structured payload for machine-to-machine messages.
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
  // Maps a Reasoning (Capability + ModelFamily) to a concrete LLMModel. Apps that need custom routing (e.g. pinned newer models, local-first, region-specific) substitute their own mapping via Orchestrator.ModelResolverHook; this is the fallback.
  static class ModelResolver
    static LLMModel Resolve(Reasoning reasoning)
    static LLMModel Resolve(Capability capability, ModelFamily family)
  // Constructed synchronously; call ResumeAsync once at startup to re-hydrate persisted apps, plans, and threads before driving any thread.
  sealed class Orchestrator : IAsyncDisposable
    // storage: Persistence backend for apps, plans, threads, messages, and artifacts. A null value selects the non-durable in-memory backend (Storages.InMemory): state lives only for the process, so new Orchestrator() followed by ResumeAsync after a restart re-hydrates nothing. Pass an explicit durable adapter when the tree must survive a restart.
    // hostBudget: Optional host-wide budget merged with each persona's own Budget when computing a thread's remaining budget: the tighter cap wins per kind, and Budget.OnExceeded resolves to the stricter action of the two (Stop > AskUser > Continue) so a host stop-policy is never silently loosened by a persona budget, nor vice versa.
    // llm: Optional LLM backend override; null uses the platform default.
    ctor(IStorage? storage = null, Budget? hostBudget = null, ILLM? llm = null)
    // Active (non-archived) apps registered on this orchestrator. Each app owns its own plans; create plans via AgentApp.CreatePlanAsync.
    Reactive<IReadOnlyList<AgentApp>> Apps { get; }
    // Global stream of every event from every thread.
    IAsyncEnumerable<ThreadEvent> Events { get; }
    // The governance hook applied to every tool call and every LLM call made by threads under this orchestrator. Default is null — no hook, identical behaviour to a pre-Guvernor world. Set once at boot (typically from ikon-config.toml's [governance] section) before any passes run. Implementations live in Ikon.Guvernor (PolicyGovernanceHook) and elsewhere.
    IGovernanceHook? GovernanceHook { get; set; }
    // Custom Capability × Family → concrete LLMModel routing for every pass under this orchestrator. Default is null — the built-in platform mapping applies. Set once at boot to give an app its own model family (pin newer models, route regionally, go local-first) without touching the platform-wide defaults. Personas keep their abstract Reasoning; only the resolution changes.
    Func<Reasoning, LLMModel>? ModelResolverHook { get; set; }
    Reactive<IReadOnlyList<Persona>> Personas { get; }
    IStorage Storage { get; }
    // Ask every pass under this orchestrator to report generation progress as it streams, so a host can show that the model is working before the turn ends. Default false. Opt-in because it adds events to the LLM stream and only a host driving a live display benefits; a batch or evaluation run pays for events it will discard. Threads expose the result as AgentThread.Streamed.
    bool StreamProgress { get; set; }
    Reactive<IReadOnlyList<AgentThread>> Threads { get; }
    Orchestrator AddPersona(Persona persona)
    // Omit id and each call mints a new app; pass a host-recomputable id (a space id, a workspace key) and the same call after ResumeAsync re-uses the persisted app — same plans, threads, and artifacts.
    // name: Display name of the app.
    // brief: What the app is for; seeds the agents working in it.
    // id: Stable identity for the app. Omit it and the app gets a fresh random id, so a host that calls this again after a restart creates a second, empty app. Pass an id the host can recompute from its own state (a space id, a workspace key) and the same call after ResumeAsync re-uses the persisted app — same plans, same threads, same artifacts.
    // ct: Cancellation token.
    Task<AgentApp> CreateAppAsync(string name, string brief = "", string? id = null, CancellationToken ct = default)
    // Get-or-create by name: matches the app on appName (default personaName) and the plan on planName, so a repeated call after ResumeAsync returns the SAME persisted thread. seedTask is posted only when the plan is first created, never onto an existing history.
    Task<AgentThread> CreateThreadAsync(string personaName, Content seedTask, string? appName = null, string planName = "main", CancellationToken ct = default)
    ValueTask DisposeAsync()
    AgentApp? GetApp(string id)
    // The live thread for threadId, or null if none is active. A thread is EVICTED from the live registry once it reaches a terminal status (Done, Failed, or Archived), so this returns null for a completed run — read a finished run's history from storage rather than expecting GetThread to return it.
    AgentThread? GetThread(string threadId)
    Orchestrator RegisterStageMachine<TState>(IStageMachine<TState> machine) where TState : struct, Enum
    // Mints fresh ids throughout, so one checkpoint can be restored many times as independent runs. The returned thread carries the checkpoint's status (typically ThreadStatus.Idle); re-engage it with AgentThread.ReactivateIfIdleAsync + RunPassAsync or AgentThread.DriveAsync. Throws if the checkpoint's persona is not registered.
    Task<AgentThread> RestoreCheckpointAsync(ThreadCheckpoint checkpoint, CancellationToken ct = default)
    // Idempotent — safe to call once at startup, a no-op for in-memory storage. Loads in dependency order (apps, then plans, then threads); an unknown persisted stage throws InvalidOperationException immediately.
    Task ResumeAsync(bool includeTerminal = false, CancellationToken ct = default)
    // Throws InvalidOperationException if the thread is still ThreadStatus.Pending, or if no persona named AgentThread.AgentName is registered.
    Task RunPassAsync(AgentThread thread, CancellationToken ct = default)
  // How many images a tool may hand back to the model in one pass. A tool that returns media claims a slot per artifact and degrades to text (a description, a "you already saw this" note) when the claim is refused — that keeps one greedy tool loop from filling the context window with pixels. The budget is per pass: it resets on the next one.
  sealed class PassMediaBudget
    ctor()
    // True when another image may be returned this pass; false = degrade to text.
    bool HasHeadroom { get; }
    // Try to claim an image slot for the named artifact. Returns false when the same artifact was already shown this pass (the model should scroll up instead) — alreadyShown distinguishes that from budget exhaustion.
    bool TryClaim(string artifactName, out bool alreadyShown)
    const int MaxImagesPerPass = 3
  // One message in the exact context window the model saw entering a pass: its Role ("user", or the agent name) and the Text it carried. This is the model's INPUT — it does not include the pass's own reply (that is PassRecord.AssistantText). Non-text parts are omitted. Also the shape of the priming messages captured in an agent-call snapshot.
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
    // Wall time of the pass.
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
  // One tool invocation within a pass. Result is the tool's result rendered to string (empty if the pass ended before the result came back). CallId is the LLM's stable id for the call — the identity used to fold a result back onto the right call, since a pass can fire several same-named calls (e.g. parallel guide()s) and matching by name alone scrambles question/answer pairs.
  sealed record PassToolCall
    ctor(string CallId, string Name, string ParametersJson, string Result)
    string CallId { get; init; }
    string Name { get; init; }
    string ParametersJson { get; init; }
    string Result { get; init; }
  // Spec for an agent — pure value, no behaviour. The runtime instantiates the running agent (an AgentThread binds to a Persona by name) each time the persona runs in a thread. The Persona / Agent split: Persona is the static blueprint; the agent is the dynamic runtime. No per-persona Tools field: define a one-tool Skill if you need a quick tool — uniformity beats sugar.
  sealed record Persona
    // Reasoning: How this agent thinks — an abstract Capability × ModelFamily choice, resolved to a concrete LLMModel per pass; see Reasoning for the mapping.
    // NudgeOnAssistantStall: Set true only for agents that must keep working when the drive re-invokes a thread whose last turn was their own tool-less message (the runner appends a bounded user continuation to unstall them); leave it false for agents that legitimately conclude with a text turn, or nudging makes them loop forever.
    ctor(string Name, string SystemPrompt, IReadOnlyList<Skill> Skills, Reasoning Reasoning, Budget? Budget = null, bool NudgeOnAssistantStall = false, IReadOnlySet<string>? TranscriptOnlyPayloadKinds = null)
    Budget? Budget { get; init; }
    string Name { get; init; }
    // Set true only for agents that must keep working when the drive re-invokes a thread whose last turn was their own tool-less message (the runner appends a bounded user continuation to unstall them); leave it false for agents that legitimately conclude with a text turn, or nudging makes them loop forever.
    bool NudgeOnAssistantStall { get; init; }
    // How this agent thinks — an abstract Capability × ModelFamily choice, resolved to a concrete LLMModel per pass; see Reasoning for the mapping.
    Reasoning Reasoning { get; init; }
    IReadOnlyList<Skill> Skills { get; init; }
    string SystemPrompt { get; init; }
    IReadOnlySet<string>? TranscriptOnlyPayloadKinds { get; init; }
  // One labelled segment of an AgentPlan's document.
  sealed record PlanSection
    ctor(string Content, double? Score = null)
    string Content { get; init; }
    double? Score { get; init; }
  // Persisted shape of an AgentPlan.
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
  // Aggregated view of a run log — the "analyze" half of run-record logging. Rolls a flat sequence of PassRecords up per FSM stage (collapsed to a single "(no stage)" bucket for runs without a stage machine) so the cost and shape of each stage is visible at a glance: which stage burned the tokens, which stage took the passes, where a run failed. Generic — works for any skill or stage machine.
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
    // Analyze an in-memory sequence of pass records.
    static RunAnalysis From(IReadOnlyList<PassRecord> records)
    // Read a JSONL run log and analyze it. A log holding passes from more than one thread is aggregated together; filter the records and use From for per-thread analysis.
    static Task<RunAnalysis> FromLogAsync(string jsonlPath, CancellationToken ct = default)
  // Appends every PassCompleted seen on an Orchestrator to a JSONL file — one PassRecord per line. The generic run log: any skill or stage machine produces an analyzable record without bespoke telemetry plumbing. Attach once near the start of a run; DisposeAsync to detach and flush. ReadAsync reads records back for analysis.
  sealed class RunLog : IAsyncDisposable
    // Append one record directly, bypassing the event pump. For terminal failures that happen OUTSIDE an LLM pass — in a driver loop, an interaction hook, result assembly — which never produce a PassCompleted of their own. Without this a run that crashes outside a pass would leave a log that simply *ends* with no failure entry. The write is synchronous + locked against the pump so the record is on disk before the caller returns — no channel-drain race at dispose.
    void Append(PassRecord record)
    // Subscribe to orchestrator and append each completed pass to jsonlPath (created if absent, appended to if present). The parent directory is created if needed.
    static RunLog Attach(Orchestrator orchestrator, string jsonlPath)
    ValueTask DisposeAsync()
    // Read every PassRecord back from a JSONL run log, in file order. Skips blank lines and unparseable entries rather than throwing — a truncated log from a crashed run is still useful.
    static IAsyncEnumerable<PassRecord> ReadAsync(string jsonlPath, CancellationToken ct = default)
  // Payload kinds for the runtime's own machine-posted messages, so hosts can filter them from user-facing conversation views structurally instead of by string match.
  static class RuntimeMessages
    // A user-role corrective the runner posts to recover a drive (the truncated-pass re-prompt). It steers the agent; it is never part of the user's conversation.
    const string NudgePayloadKind
  // One capability bundle: instructions + tools. Stateful (carries any service references it needs) but instances are reusable across threads. The live ToolContext is delivered per invocation; Tools itself sees no per-call context — register-time configuration only.
  abstract class Skill
    virtual string Instructions { get; }
    abstract string Name { get; }
    // Returns the tools this skill contributes when active.
    abstract IEnumerable<Tool> Tools()
  // Named, reusable bundle of skills. Returned by IStageMachine<TState>.SkillsFor; also useful as a pre-defined collection apps can share across agents.
  sealed record SkillSet
    // ReasoningOverride: When set, the runner resolves the pass's model/temperature from IT instead of the persona's Persona.Reasoning — so a stage machine can put drafting stages on a cheap fast tier while verdict stages stay on a strong one, per pass, without splitting personas.
    ctor(string Name, IReadOnlyList<Skill> Skills, string? Instructions = null, Reasoning? ReasoningOverride = null)
    string? Instructions { get; init; }
    string Name { get; init; }
    // When set, the runner resolves the pass's model/temperature from IT instead of the persona's Persona.Reasoning — so a stage machine can put drafting stages on a cheap fast tier while verdict stages stay on a strong one, per pass, without splitting personas.
    Reasoning? ReasoningOverride { get; init; }
    IReadOnlyList<Skill> Skills { get; init; }
  // One child thread to spawn via AgentThread.SpawnAllAsync: the persona to run, the seed task that opens the child thread, and optional per-thread options.
  sealed record SpawnSpec
    ctor(string PersonaName, Content SeedTask, ThreadOptions? Options = null)
    ThreadOptions? Options { get; init; }
    string PersonaName { get; init; }
    Content SeedTask { get; init; }
  // Per-stage aggregation within a RunAnalysis.
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
  // Built-in IStorage factories. Production-grade adapters (Postgres, etc.) ship as separate packages.
  static class Storages
    // In-memory backend. Not durable across process restarts.
    static IStorage InMemory()
  // Cumulative characters a thread has streamed off the model, split by what the model was producing. The three move at genuinely different times — a turn may deliberate at length before writing anything, or write tool arguments without a word of prose — so a consumer showing progress can distinguish them instead of collapsing everything into one "busy".
  readonly record struct StreamedContent
    ctor(long TextCharacters, long ReasoningCharacters, long ToolArgumentCharacters)
    long ReasoningCharacters { get; init; }
    long TextCharacters { get; init; }
    long ToolArgumentCharacters { get; init; }
  // A self-contained, restorable capture of one thread at a point in time: its persisted state, the plan it is working, its full message history, and its artifacts. Unlike Orchestrator.ResumeAsync — which re-hydrates whatever IStorage currently holds — a ThreadCheckpoint is an explicit, labeled capture you keep and restore from later. Run a stage once, checkpoint it, then iterate on the next stage from that fixed starting point as many times as you like without re-running the earlier stages. Generic: works for any skill or stage machine.
  sealed record ThreadCheckpoint
    ctor(string Label, DateTime CapturedAt, ThreadSnapshot Thread, PlanSnapshot Plan, IReadOnlyList<Message> Messages, IReadOnlyList<Artifact> Artifacts)
    IReadOnlyList<Artifact> Artifacts { get; init; }
    DateTime CapturedAt { get; init; }
    string Label { get; init; }
    IReadOnlyList<Message> Messages { get; init; }
    PlanSnapshot Plan { get; init; }
    ThreadSnapshot Thread { get; init; }
  // Audit record fan-out for the observable transitions on a thread. A single rooted hierarchy so consumers can switch on the concrete case: switch (evt) { case ThreadEvent.MessagePosted m: ... }. Two tiers share the stream: settled markers (MessagePosted, ArtifactWritten, AgentCompleted) and live streaming events (TextDelta, ToolCallStarted, ToolCallCompleted, TokenUsageUpdated, Progress). A consumer can drive a whole UI from one await foreach over AgentThread.Events: reconstruct live text from TextDelta, then treat MessagePosted as the settled value.
  abstract record ThreadEvent
    DateTime At { get; init; }
    string ThreadId { get; init; }
  // One LLM-pass completed for this thread. Per-pass, NOT per-thread. Final is the thread's status at pass end (typically Idle, but can be WaitingForInput / WaitingForChildren / Done / Failed depending on what the pass triggered). Stage machines listen for this to run IStageMachine<TState>.TryTransitionAsync.
  sealed record ThreadEvent.AgentCompleted : ThreadEvent
    ctor(string ThreadId, DateTime At, string AgentName, ThreadStatus Final)
    string AgentName { get; init; }
    ThreadStatus Final { get; init; }
  sealed record ThreadEvent.ArtifactWritten : ThreadEvent
    ctor(string ThreadId, DateTime At, Artifact Artifact)
    Artifact Artifact { get; init; }
  // A budget cap tripped for this thread. Fires BEFORE the runner applies the configured BudgetAction, so a consumer observing the event sees the thread's status change (WaitingForInput / Failed / none for Continue) arrive after it. Snapshot says which cap tripped and by how much.
  sealed record ThreadEvent.BudgetExceeded : ThreadEvent
    ctor(string ThreadId, DateTime At, BudgetSnapshot Snapshot)
    BudgetSnapshot Snapshot { get; init; }
  sealed record ThreadEvent.MessagePosted : ThreadEvent
    ctor(string ThreadId, DateTime At, Message Message)
    Message Message { get; init; }
  // One LLM pass finished — carries the full PassRecord telemetry (tokens, stage transition, tool calls, outcome). Fires for every pass, success or failure. RunLog appends these to a JSONL run log so any skill or stage machine can be analyzed and tuned offline.
  sealed record ThreadEvent.PassCompleted : ThreadEvent
    ctor(string ThreadId, DateTime At, PassRecord Record)
    PassRecord Record { get; init; }
  // An LLM pass failed. The thread has been transitioned to ThreadStatus.Failed. Retryable reflects the exception classification (a RetryableAIException surfaces here only after Emerge exhausted its internal retries).
  sealed record ThreadEvent.PassFailed : ThreadEvent
    ctor(string ThreadId, DateTime At, string AgentName, string ErrorMessage, bool Retryable)
    string AgentName { get; init; }
    string ErrorMessage { get; init; }
    bool Retryable { get; init; }
  // Human-readable progress note surfaced from the model pass (Emerge's Progress / Retry events) — e.g. "retrying (2/3)".
  sealed record ThreadEvent.Progress : ThreadEvent
    ctor(string ThreadId, DateTime At, string Message)
    string Message { get; init; }
  // Custom event emitted by a Skill via ToolContext.EmitAsync. Lets domains extend the event vocabulary (e.g. "human-handoff-requested", "model-swap-suggested") without bloating ThreadEvent for every domain need. Kind is a free-form string the emitting skill chose; Payload is opaque JSON.
  sealed record ThreadEvent.SkillEmitted : ThreadEvent
    ctor(string ThreadId, DateTime At, string SkillName, string Kind, JsonElement Payload)
    string Kind { get; init; }
    JsonElement Payload { get; init; }
    string SkillName { get; init; }
  // One incremental LLM text chunk — NOT accumulated. Consumers append successive deltas to render streaming text; the final assembled text arrives as a MessagePosted once the pass ends.
  sealed record ThreadEvent.TextDelta : ThreadEvent
    ctor(string ThreadId, DateTime At, string AgentName, string Delta)
    string AgentName { get; init; }
    string Delta { get; init; }
  // Cumulative token usage for the in-flight pass, fired as the model reports updates mid-stream.
  sealed record ThreadEvent.TokenUsageUpdated : ThreadEvent
    ctor(string ThreadId, DateTime At, long InputTokens, long CachedInputTokens, long CacheCreationInputTokens, long OutputTokens)
    long CacheCreationInputTokens { get; init; }
    long CachedInputTokens { get; init; }
    long InputTokens { get; init; }
    long OutputTokens { get; init; }
  // A tool call finished. Result is the tool's result rendered to string.
  sealed record ThreadEvent.ToolCallCompleted : ThreadEvent
    ctor(string ThreadId, DateTime At, string ToolName, string Result)
    string Result { get; init; }
    string ToolName { get; init; }
  // A tool call is about to execute. ParametersJson is the call's argument object as JSON ("{}" when absent).
  sealed record ThreadEvent.ToolCallStarted : ThreadEvent
    ctor(string ThreadId, DateTime At, string ToolName, string ParametersJson)
    string ParametersJson { get; init; }
    string ToolName { get; init; }
  // Options for a thread created via Orchestrator.
  sealed record ThreadOptions
    // StageMachineName: Name of a stage machine registered with Orchestrator.RegisterStageMachine. An UNREGISTERED name is silently ignored at creation (the thread runs with no stage machine) but throws on resume — register the machine before creating the thread, and check the spelling.
    // InitialStage: Stage to seed the thread in. Requires StageMachineName to be set — supplying it alone throws InvalidOperationException.
    // InitialStatus: Starting status; defaults to Active. Only a thread created explicitly as Pending must have its first turn driven through EnsureActiveAsync/DriveAsync rather than ReactivateIfIdleAsync.
    ctor(string? StageMachineName = null, string? InitialStage = null, ThreadStatus InitialStatus = Active)
    // Stage to seed the thread in. Requires StageMachineName to be set — supplying it alone throws InvalidOperationException.
    string? InitialStage { get; init; }
    // Starting status; defaults to Active. Only a thread created explicitly as Pending must have its first turn driven through EnsureActiveAsync/DriveAsync rather than ReactivateIfIdleAsync.
    ThreadStatus InitialStatus { get; init; }
    // Name of a stage machine registered with Orchestrator.RegisterStageMachine. An UNREGISTERED name is silently ignored at creation (the thread runs with no stage machine) but throws on resume — register the machine before creating the thread, and check the spelling.
    string? StageMachineName { get; init; }
  // Persisted thread state — the durability snapshot the runtime reads on Orchestrator.ResumeAsync and writes after each turn boundary.
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
  // Explicit transition verbs. AgentThread.TransitionAsync takes one of these; direct status mutation is not part of the API.
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
  // Cumulative usage for a thread. Depth is the spawn-tree level from the root (set at construction; a spawned child sits one level below its parent). Turns increments after each LLM pass; WallTime is the thread's own.
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
    // Schema-first factory for tools whose parameter shape is not expressible as a typed delegate — MCP-discovered tools, dynamically generated schemas, or schemas authored by hand. The given JSON schema (an object schema with properties/required, optionally per-property description and enum) becomes ParameterSchema verbatim and is parsed by the runtime bridge into the wire-facing function parameters. invoke receives the raw argument object exactly as the model produced it. Like OfContext tools, dispatch through an agent pass requires the runner's ToolContext scope.
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
  // One entry on the per-thread AgentThread.ToolCallTimeline. Captured when ToolCallStarted fires; updated with the result + error flag when ToolCallCompleted follows. PrecedingAgentMessages is the number of agent messages already on the thread at start time — UI consumers slot the row in front of the agent message it produced (the turn-grouping signal).
  sealed record ToolCallEntry
    // IsError: true/false for a call completed live in this process; null when the timeline was rebuilt by replaying a thread's journal — the journal does not record the error flag, so it cannot be recovered on resume. Treat null as "unknown", not as "succeeded".
    ctor(int PrecedingAgentMessages, string ToolName, string ArgsJson, string? ResultText, bool? IsError)
    string ArgsJson { get; init; }
    // true/false for a call completed live in this process; null when the timeline was rebuilt by replaying a thread's journal — the journal does not record the error flag, so it cannot be recovered on resume. Treat null as "unknown", not as "succeeded".
    bool? IsError { get; init; }
    int PrecedingAgentMessages { get; init; }
    string? ResultText { get; init; }
    string ToolName { get; init; }
  // What tool delegates see when invoked. Read-only handle on the active thread plus a CancellationToken that the runner threads through; everything else (storage, orchestrator state) is reachable via Thread.
  sealed record ToolContext
    ctor(AgentThread Thread, CancellationToken Cancellation)
    CancellationToken Cancellation { get; init; }
    // Shared per-pass budget for media-returning tools. Caps how many images enter one pass's context and dedupes repeat views of the same artifact — the cap lives at the tool layer because rewriting earlier tool results mid-pass would invalidate the rolling prompt cache.
    PassMediaBudget MediaBudget { get; init; }
    // Capabilities of the model driving the current pass, or null when the runner predates capability plumbing (tests, direct Invoke). Tools use this to decide between returning media (model can see it) and a text description fallback.
    LLMCapabilities? ModelCapabilities { get; init; }
    // Snapshot of how much of the effective budget remains for this thread; null in a kind means no cap on that kind. Skills read this to be cost-aware (e.g. summarize aggressively when output tokens are running low).
    BudgetRemaining RemainingBudget { get; }
    AgentThread Thread { get; init; }
    // Promote raw bytes to a durable Artifact and return a Reference pointing at it. Thin delegation to AgentThread.AttachAsArtifactAsync; defaults source to ArtifactSource.Agent because this entry point is invoked from within a tool body (i.e. the agent is the writer). Use the AgentThread overload from a host UI when the user is the writer.
    Task<Content.Reference> AttachAsArtifactAsync(string name, string mimeType, byte[] bytes, ArtifactSource source = Agent)
    // Emit a custom SkillEmitted event with a skill-defined kind and an opaque payload. Lets skills extend the event vocabulary (e.g. "human-handoff-requested", "model-swap-suggested") without bloating the core ThreadEvent hierarchy.
    Task EmitAsync(string skillName, string kind, object payload)
    // Read a typed artifact written by WriteArtifactAsync<T>; null when absent.
    Task<T?> ReadArtifactAsync<T>(string name)
    // Write a durable, addressable artifact tied to this thread. The artifact lands atomically in IStorage and is registered on the thread's reactive AgentThread.Artifacts list, plus an ArtifactWritten fires.
    Task<Artifact> WriteArtifactAsync(string name, string type, IReadOnlyList<Content> parts, ArtifactSource source = Agent)
    // Write a typed value as a durable artifact (JSON). Typed counterpart to WriteArtifactAsync — read it back with ReadArtifactAsync<T>.
    Task<Artifact> WriteArtifactAsync<T>(string name, T value, ArtifactSource source = Agent)
  // Introspection-safe view of a Tool (no executable delegate exposed).
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
  // Built-in skills the runtime ships out of the box. Singletons because the tools are stateless; the live context comes via ToolContext.
  static class Built
    static readonly Skill Attachments
    static readonly Skill Messaging
    static readonly Skill Termination
    static readonly Skill UserDecision
    static readonly Skill WebSearch
  // Exposes the tools of a connected McpClient as a Skill: one Tool per tool the MCP server advertises, built schema-first via Tool.FromSchema from the server's own JSON input schema, invoking back through McpClient.CallToolAsync. The client must have been connected via McpClient.ConnectAsync before the skill's tools are enumerated. Tool names are made LLM-safe by replacing dots with underscores; calls go to the server under the original name.
  // var mcpClient = new McpClient("https://example.com/mcp");
  // await mcpClient.ConnectAsync();
  // var skill = new McpSkill(mcpClient);
  //
  // // As part of a Persona's skill set:
  // var persona = new Persona("Assistant", systemPrompt,
  //     Skills: [Built.Messaging, skill],
  //     Reasoning: new Reasoning());
  //
  // // Or directly on a raw Emerge pass (context-bound tools need a live agent run):
  // pass.AddTools(skill.Tools().ToArray());
  sealed class McpSkill : Skill
    ctor(McpClient mcpClient, string name = "mcp", string instructions = "")
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  // Wire format for a user-input gate written by the Built.UserDecision skill. Kind discriminates between the two shapes: • "decision" (default, from await_user_decision): Options is non-empty; the host renders buttons; the user's pick posts back as a UserDecisionResponse. • "clarification" (from await_user_clarification): Options is empty; the host renders a free-text prompt; the user's typed answer posts back as a plain user message — no structured response, since the agent reads it as ordinary context.
  sealed record UserDecisionPrompt
    ctor(string Question, IReadOnlyList<string> Options, string Kind = "decision")
    string Kind { get; init; }
    IReadOnlyList<string> Options { get; init; }
    string Question { get; init; }
  // Constants describing the user-decision artifact + message shape.
  static class UserDecisionProtocol
    // Build a user Message that answers a pending decision with choice. Hosts call this in response to a button click; the message's PayloadKind is the substrate protocol that the agent reads on its next pass.
    static Message BuildResponse(string choice)
    // Read the latest UserDecisionPrompt from the thread's artifacts, if any. Returns null when no pending decision exists.
    static Task<UserDecisionPrompt?> TryReadPromptAsync(AgentThread thread)
    // Read the user's choice from the latest user message, if it carries the ResponsePayloadKind envelope. Returns null otherwise.
    static UserDecisionResponse? TryReadResponse(Message message)
    // Artifact MIME type.
    const string ArtifactMimeType
    // Artifact name the await_user_decision tool writes.
    const string ArtifactName
    // Message Message.PayloadKind for the user's response.
    const string ResponsePayloadKind
  // Wire format for the user's reply to a UserDecisionPrompt. Host posts a user Message with PayloadKind = UserDecisionProtocol.ResponsePayloadKind and Payload = JSON of this record. The agent reads it on its next pass.
  sealed record UserDecisionResponse
    ctor(string Choice)
    string Choice { get; init; }
