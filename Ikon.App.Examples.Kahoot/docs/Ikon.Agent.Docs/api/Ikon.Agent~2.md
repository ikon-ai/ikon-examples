namespace Ikon.Agent
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
