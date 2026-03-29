# Ikon.Agent Public API

namespace Ikon.Agent.Batching
  sealed class BatchProcessor
    ctor(BatchingOptions options = null)
    bool HasPending { get; }
    int PendingCount { get; }
    void Add(string formattedMessage)
    void AdvanceOverlap()
    void Clear()
    ValueTuple<IReadOnlyList<string>, IReadOnlyList<string>> GetBatch()
    bool ShouldTrigger(bool isDirty, DateTime lastAnalysisAt)
  class BatchingOptions : IEquatable<BatchingOptions>
    ctor(int BatchSize = 100, int OverlapSize = 20, TimeSpan? MaxInterval = null)
    int BatchSize { get;  init; }
    TimeSpan EffectiveMaxInterval { get; }
    TimeSpan? MaxInterval { get;  init; }
    int OverlapSize { get;  init; }

namespace Ikon.Agent.Coordinators
  sealed class AgentStreamCoordinator : IAsyncDisposable
    ctor(BatchingOptions batchingOptions = null, IStreamSummarizer summarizer = null)
    string History { get; }
    IReadOnlyList<IStreamProcessor> Processors { get; }
    IAsyncEnumerable<StreamSummary> AnalyzeAsync(IAsyncEnumerable<ProtocolMessage> stream, BatchingOptions options = null, CancellationToken cancellationToken = null)
    ValueTask DisposeAsync()
    T GetProcessor<T>()
    AgentStreamCoordinator RegisterProcessor(IStreamProcessor processor)
  sealed class StreamRouter
    ctor()
    bool IsAnyDirty { get; }
    IReadOnlyList<IStreamProcessor> Processors { get; }
    void ClearAllDirty()
    Dictionary<string, string> GetAllSnapshots(SnapshotOptions options = null)
    T GetProcessor<T>()
    void Register(IStreamProcessor processor)
    bool Route(ProtocolMessage message, IProtocolMessagePayload payload)

namespace Ikon.Agent.Core
  sealed class AgentContextRequest : IEquatable<AgentContextRequest>
    ctor(AgentState State, string Phase, int MaxTokens, IReadOnlyDictionary<string, object> Options = null)
    int MaxTokens { get;  init; }
    IReadOnlyDictionary<string, object> Options { get;  init; }
    string Phase { get;  init; }
    AgentState State { get;  init; }
  abstract class AgentEvent : IEquatable<AgentEvent>
    string AgentId { get;  init; }
    Guid EventId { get;  init; }
    long SequenceNumber { get;  init; }
    DateTime Timestamp { get;  init; }
  sealed class AgentRole : IEquatable<AgentRole>
    ctor(string RoleId, string SystemPrompt, string[] ToolAllowlist, string[] ToolDenylist, string PreferredModel = null, double? Temperature = null)
    bool AllowsAllTools { get; }
    string PreferredModel { get;  init; }
    string RoleId { get;  init; }
    string SystemPrompt { get;  init; }
    double? Temperature { get;  init; }
    string[] ToolAllowlist { get;  init; }
    string[] ToolDenylist { get;  init; }
    bool IsToolAllowed(string toolName)
  sealed class AgentTask : IEquatable<AgentTask>
    ctor(string TaskId, string Subject, string Description, AgentTaskStatus Status = Pending, ImmutableList<string> Dependencies = null, ImmutableList<string> Blockers = null, string AssignedTo = null, DateTime? StartedAt = null, DateTime? CompletedAt = null, string Result = null, string ParentTaskId = null, ImmutableList<string> ChildTaskIds = null, ImmutableList<string> Capabilities = null, ImmutableDictionary<string, object> LocalContext = null, ImmutableList<TaskLearning> Learnings = null, ImmutableDictionary<string, object> Metadata = null, string ActiveForm = null, string Error = null)
    string ActiveForm { get;  init; }
    string AssignedTo { get;  init; }
    ImmutableList<string> Blockers { get;  init; }
    bool CanOrchestrate { get; }
    ImmutableList<string> Capabilities { get;  init; }
    ImmutableList<string> ChildTaskIds { get;  init; }
    DateTime? CompletedAt { get;  init; }
    ImmutableList<string> Dependencies { get;  init; }
    string Description { get;  init; }
    string Error { get;  init; }
    bool IsLeaf { get; }
    bool IsRoot { get; }
    bool IsRunnable { get; }
    bool IsTerminal { get; }
    ImmutableList<TaskLearning> Learnings { get;  init; }
    ImmutableDictionary<string, object> LocalContext { get;  init; }
    ImmutableDictionary<string, object> Metadata { get;  init; }
    string ParentTaskId { get;  init; }
    string Result { get;  init; }
    DateTime? StartedAt { get;  init; }
    AgentTaskStatus Status { get;  init; }
    string Subject { get;  init; }
    string TaskId { get;  init; }
  enum AgentTaskStatus
    Pending
    InProgress
    Completed
    Failed
    Blocked
    Cancelled
  sealed class ArtifactCreated : AgentEvent, IEquatable<ArtifactCreated>
    ctor(string ArtifactId, string MimeType, long SizeBytes)
    string ArtifactId { get;  init; }
    string MimeType { get;  init; }
    long SizeBytes { get;  init; }
  sealed class ArtifactRef : IEquatable<ArtifactRef>
    ctor(string ArtifactId, string MimeType, long SizeBytes, DateTime CreatedAt, string StoragePath = null, IReadOnlyDictionary<string, string> Metadata = null)
    string ArtifactId { get;  init; }
    DateTime CreatedAt { get;  init; }
    IReadOnlyDictionary<string, string> Metadata { get;  init; }
    string MimeType { get;  init; }
    long SizeBytes { get;  init; }
    string StoragePath { get;  init; }
  sealed class ArtifactStoreOptions : IEquatable<ArtifactStoreOptions>
    ctor(string CustomPath = null, bool Compress = false, IReadOnlyDictionary<string, string> Metadata = null)
    bool Compress { get;  init; }
    string CustomPath { get;  init; }
    IReadOnlyDictionary<string, string> Metadata { get;  init; }
  sealed class AskUserDecision : OrchestrationDecision, IEquatable<AskUserDecision>
    ctor(string Question, string[] Options = null)
    string[] Options { get;  init; }
    string Question { get;  init; }
  sealed class BlackboardUpdated : AgentEvent, IEquatable<BlackboardUpdated>
    ctor(string Key, string ArtifactId)
    string ArtifactId { get;  init; }
    string Key { get;  init; }
  sealed class CapabilityResult : IEquatable<CapabilityResult>
    ctor(IReadOnlyList<IMessagePart> Output, IReadOnlyList<AgentTask> SpawnedTasks = null, IReadOnlyList<TaskLearning> Learnings = null, bool Success = true, string ErrorMessage = null)
    string ErrorMessage { get;  init; }
    IReadOnlyList<TaskLearning> Learnings { get;  init; }
    IReadOnlyList<IMessagePart> Output { get;  init; }
    IReadOnlyList<AgentTask> SpawnedTasks { get;  init; }
    bool Success { get;  init; }
  sealed class CapabilityRun : IEquatable<CapabilityRun>
    ctor(string CapabilityId, string Focus, int Attempt, DateTime StartedAt, DateTime? CompletedAt = null, bool Success = true)
    int Attempt { get;  init; }
    string CapabilityId { get;  init; }
    DateTime? CompletedAt { get;  init; }
    string Focus { get;  init; }
    DateTime StartedAt { get;  init; }
    bool Success { get;  init; }
  sealed class CheckpointCreated : AgentEvent, IEquatable<CheckpointCreated>
    ctor(long AtSequence, string Reason)
    long AtSequence { get;  init; }
    string Reason { get;  init; }
  sealed class ContextPackResult : IEquatable<ContextPackResult>
    ctor(string Content, int EstimatedTokens, bool IsCritical = false)
    string Content { get;  init; }
    int EstimatedTokens { get;  init; }
    bool IsCritical { get;  init; }
  sealed class DefaultCapabilityRegistry : ICapabilityRegistry
    ctor()
    IReadOnlyDictionary<string, ICapabilityExecutor> GetAllExecutors()
    IReadOnlyList<string> GetCapabilityIds()
    ICapabilityExecutor GetExecutor(string capabilityId)
    void Register(ICapabilityExecutor executor)
  enum ExecutionPattern
    Single
    BestOf
    Consensus
    Debate
  sealed class GoalSet : AgentEvent, IEquatable<GoalSet>
    ctor(string Goal)
    string Goal { get;  init; }
  interface IArtifactStore
    abstract Task DeleteAsync(string artifactId, CancellationToken ct = null)
    abstract Task<bool> ExistsAsync(string artifactId, CancellationToken ct = null)
    abstract Task<byte[]> GetAsync(string artifactId, CancellationToken ct = null)
    abstract Task<ArtifactRef> GetMetadataAsync(string artifactId, CancellationToken ct = null)
    abstract IAsyncEnumerable<ArtifactRef> ListAsync(string prefix = null, CancellationToken ct = null)
    abstract Task<Stream> OpenReadAsync(string artifactId, CancellationToken ct = null)
    abstract Task<ArtifactRef> StoreAsync(byte[] data, string mimeType, ArtifactStoreOptions options = null, CancellationToken ct = null)
    abstract Task<ArtifactRef> StoreAsync(Stream stream, string mimeType, ArtifactStoreOptions options = null, CancellationToken ct = null)
  interface IBlackboard
    abstract Task ClearAsync(CancellationToken ct = null)
    abstract Task<bool> ContainsAsync(string key, CancellationToken ct = null)
    abstract IAsyncEnumerable<KeyValuePair<string, object>> GetAllAsync(CancellationToken ct = null)
    abstract Task<T> GetAsync<T>(string key, CancellationToken ct = null)
    abstract IAsyncEnumerable<string> ListKeysAsync(string prefix = "", CancellationToken ct = null)
    abstract Task RemoveAsync(string key, CancellationToken ct = null)
    abstract Task<string> ResolveArtifactPointerAsync(string key, CancellationToken ct = null)
    abstract Task SetAsync<T>(string key, T value, CancellationToken ct = null)
    abstract Task<string> StoreArtifactPointerAsync(string key, string artifactId, CancellationToken ct = null)
  interface ICapabilityExecutor
    string CapabilityId { get; }
    string Description { get; }
    abstract Task<CapabilityResult> ExecuteAsync(AgentTask task, string focus, IBlackboard blackboard, CancellationToken ct = null)
  interface ICapabilityRegistry
    abstract IReadOnlyDictionary<string, ICapabilityExecutor> GetAllExecutors()
    abstract IReadOnlyList<string> GetCapabilityIds()
    abstract ICapabilityExecutor GetExecutor(string capabilityId)
    abstract void Register(ICapabilityExecutor executor)
  interface IContextBuilder
    abstract Task<KernelContext> BuildContextAsync(AgentContextRequest request, CancellationToken ct = null)
    abstract Task<KernelContext> BuildContextAsync(AgentContextRequest request, IEnumerable<string> packNames, CancellationToken ct = null)
    abstract IReadOnlyList<IContextPack> GetPacks()
    abstract void RegisterPack(IContextPack pack)
    abstract void UnregisterPack(string packName)
  interface IContextPack
    string Name { get; }
    int Priority { get; }
    abstract Task<ContextPackResult> BuildAsync(AgentContextRequest request, CancellationToken ct = null)
  interface IEventStore
    abstract Task AppendAsync(AgentEvent evt, CancellationToken ct = null)
    abstract Task AppendBatchAsync(IEnumerable<AgentEvent> events, CancellationToken ct = null)
    abstract Task<long> GetEventCountAsync(string agentId, CancellationToken ct = null)
    abstract Task<long> GetLatestSequenceAsync(string agentId, CancellationToken ct = null)
    abstract IAsyncEnumerable<AgentEvent> ReadFromAsync(string agentId, long fromSequence = 0, CancellationToken ct = null)
  interface ILearningStore
    abstract Task AddLearningAsync(TaskLearning learning, CancellationToken ct = null)
    abstract Task ClearAsync(CancellationToken ct = null)
    abstract IAsyncEnumerable<TaskLearning> GetAllLearningsAsync(CancellationToken ct = null)
    abstract Task<IReadOnlyList<TaskLearning>> GetRelevantLearningsAsync(string context, int maxCount = 10, CancellationToken ct = null)
    abstract Task RecordFailureAsync(string learningId, CancellationToken ct = null)
    abstract Task ReinforceLearningAsync(string learningId, CancellationToken ct = null)
  interface IOrchestrationStrategy
    abstract Task<OrchestrationDecision> DecideAsync(AgentTask currentTask, string focus, IBlackboard blackboard, IReadOnlyList<TaskLearning> learnings, CancellationToken ct = null)
  interface IStreamProcessor : IAsyncDisposable
    bool IsDirty { get; }
    string ProcessorId { get; }
    Opcode SupportedOpcodeGroup { get; }
    abstract void ClearDirty()
    abstract string GetSnapshot(SnapshotOptions options = null)
    abstract void Process(ProtocolMessage message, IProtocolMessagePayload payload)
  interface IWorkerBus
    abstract Task BroadcastAsync(WorkerMessageEnvelope envelope, CancellationToken ct = null)
    abstract Task<IReadOnlyList<string>> GetRegisteredAgentsAsync(CancellationToken ct = null)
    abstract IAsyncEnumerable<WorkerMessageEnvelope> ReceiveAsync(string agentId, CancellationToken ct = null)
    abstract Task RegisterAgentAsync(string agentId, CancellationToken ct = null)
    abstract Task SendAsync(WorkerMessageEnvelope envelope, CancellationToken ct = null)
    abstract Task UnregisterAgentAsync(string agentId, CancellationToken ct = null)
  enum LearningScope
    Task
    Subtree
    Global
  sealed class MessageReceived : AgentEvent, IEquatable<MessageReceived>
    ctor(string ChannelId, IMessagePart[] Parts)
    string ChannelId { get;  init; }
    IMessagePart[] Parts { get;  init; }
  sealed class OrchestrationContext
    List<IMessagePart> Context { get; }
    List<CapabilityRun> History { get; }
    string TaskDescription { get;  init; }
    int TokenBudget { get;  init; }
    int TokensUsed { get;  set; }
  abstract class OrchestrationDecision : IEquatable<OrchestrationDecision>
  sealed class PhaseCompleted : AgentEvent, IEquatable<PhaseCompleted>
    ctor(string Phase, bool Success)
    string Phase { get;  init; }
    bool Success { get;  init; }
  sealed class PhaseStarted : AgentEvent, IEquatable<PhaseStarted>
    ctor(string Phase)
    string Phase { get;  init; }
  sealed class PlanUpdated : AgentEvent, IEquatable<PlanUpdated>
    ctor(string[] Steps)
    string[] Steps { get;  init; }
  sealed class RoleChanged : AgentEvent, IEquatable<RoleChanged>
    ctor(string RoleId)
    string RoleId { get;  init; }
  static class Roles
    static AgentRole Coordinator { get; }
    static AgentRole Implementer { get; }
    static AgentRole Planner { get; }
    static AgentRole Researcher { get; }
    static AgentRole Reviewer { get; }
  sealed class RunCapabilityDecision : OrchestrationDecision, IEquatable<RunCapabilityDecision>
    ctor(string CapabilityId, string Focus, ExecutionPattern Pattern = Single, int Attempts = 3)
    int Attempts { get;  init; }
    string CapabilityId { get;  init; }
    string Focus { get;  init; }
    ExecutionPattern Pattern { get;  init; }
  class SnapshotOptions : IEquatable<SnapshotOptions>
    ctor(bool MarkUpdates = true, StyleVerbosity StyleVerbosity = None)
    bool MarkUpdates { get;  init; }
    StyleVerbosity StyleVerbosity { get;  init; }
  sealed class SpawnTasksDecision : OrchestrationDecision, IEquatable<SpawnTasksDecision>
    ctor(IReadOnlyList<AgentTask> Tasks, bool ExecuteInParallel = false)
    bool ExecuteInParallel { get;  init; }
    IReadOnlyList<AgentTask> Tasks { get;  init; }
  abstract class StreamProcessorBase : IAsyncDisposable, IStreamProcessor
    bool IsDirty { get; }
    string ProcessorId { get; }
    Opcode SupportedOpcodeGroup { get; }
    void ClearDirty()
    virtual ValueTask DisposeAsync()
    abstract string GetSnapshot(SnapshotOptions options = null)
    abstract void Process(ProtocolMessage message, IProtocolMessagePayload payload)
  enum StyleVerbosity
    None
    ClassNames
  sealed class TaskCompleteDecision : OrchestrationDecision, IEquatable<TaskCompleteDecision>
    ctor(string Summary)
    string Summary { get;  init; }
  sealed class TaskCreated : AgentEvent, IEquatable<TaskCreated>
    ctor(string TaskId, string Subject, string Description)
    string Description { get;  init; }
    string Subject { get;  init; }
    string TaskId { get;  init; }
  sealed class TaskFailedDecision : OrchestrationDecision, IEquatable<TaskFailedDecision>
    ctor(string Reason)
    string Reason { get;  init; }
  sealed class TaskGraph
    ctor()
    int Count { get; }
    IEnumerable<AgentTask> Tasks { get; }
    TaskGraph AddChildTask(string parentId, AgentTask child)
    TaskGraph AddOrUpdateTask(AgentTask task)
    TaskGraph AddTask(AgentTask task)
    IEnumerable<TaskLearning> GetAccumulatedLearnings(string taskId)
    IEnumerable<AgentTask> GetAncestors(string taskId)
    int GetDepth(string taskId)
    IEnumerable<AgentTask> GetDescendants(string taskId)
    IEnumerable<AgentTask> GetRootTasks()
    IEnumerable<AgentTask> GetRunnableTasks()
    ValueTuple<int, int, int, int, int> GetStats()
    AgentTask GetTask(string taskId)
    IEnumerable<AgentTask> GetTasksWithStatus(AgentTaskStatus status)
    bool HasCycle()
    TaskGraph MarkCancelled(string taskId)
    TaskGraph MarkCompleted(string taskId, string result = null)
    TaskGraph MarkFailed(string taskId, string reason = null)
    TaskGraph MarkInProgress(string taskId)
    TaskGraph PropagateLearning(string taskId, TaskLearning learning)
    TaskGraph RemoveTask(string taskId)
    TaskGraph UpdateTask(AgentTask task)
  sealed class TaskLearning : IEquatable<TaskLearning>
    ctor(string Category, string Context, string Insight, DateTime CreatedAt, LearningScope Scope = Task, int SuccessCount = 0, int FailureCount = 0)
    string Category { get;  init; }
    string Context { get;  init; }
    DateTime CreatedAt { get;  init; }
    int FailureCount { get;  init; }
    string Insight { get;  init; }
    string LearningId { get;  init; }
    LearningScope Scope { get;  init; }
    int SuccessCount { get;  init; }
  sealed class TaskStatusChanged : AgentEvent, IEquatable<TaskStatusChanged>
    ctor(string TaskId, string OldStatus, string NewStatus)
    string NewStatus { get;  init; }
    string OldStatus { get;  init; }
    string TaskId { get;  init; }
  sealed class ToolCompleted : AgentEvent, IEquatable<ToolCompleted>
    ctor(string CorrelationId, string ResultJson, bool IsError)
    string CorrelationId { get;  init; }
    bool IsError { get;  init; }
    string ResultJson { get;  init; }
  sealed class ToolInvoked : AgentEvent, IEquatable<ToolInvoked>
    ctor(string ToolName, string ParametersJson, string CorrelationId)
    string CorrelationId { get;  init; }
    string ParametersJson { get;  init; }
    string ToolName { get;  init; }
  sealed class WorkerMessageEnvelope : IEquatable<WorkerMessageEnvelope>
    ctor(string FromAgentId, string ToAgentId, string MessageType, string PayloadJson, DateTime Timestamp, Guid CorrelationId)
    Guid CorrelationId { get;  init; }
    string FromAgentId { get;  init; }
    string MessageType { get;  init; }
    string PayloadJson { get;  init; }
    DateTime Timestamp { get;  init; }
    string ToAgentId { get;  init; }
    static WorkerMessageEnvelope Create(string from, string to, string messageType, string payload)
    static WorkerMessageEnvelope CreateBroadcast(string from, string messageType, string payload)
  sealed class WorkerMessageSent : AgentEvent, IEquatable<WorkerMessageSent>
    ctor(string From, string To, string Type, string Payload)
    string From { get;  init; }
    string Payload { get;  init; }
    string To { get;  init; }
    string Type { get;  init; }
  static class WorkerMessageTypes
    static string DataRequest
    static string DataResponse
    static string Heartbeat
    static string Shutdown
    static string StatusUpdate
    static string TaskAssignment
    static string TaskCompleted
    static string TaskFailed

namespace Ikon.Agent.Kernel
  static class AgentReducer
    static AgentState Apply(AgentState state, AgentEvent evt)
    static AgentState ApplyAll(AgentState state, IEnumerable<AgentEvent> events)
    static Task<AgentState> RebuildAsync(string agentId, IEventStore eventStore, long fromSequence = 0, CancellationToken ct = null)
  sealed class AgentState : IEquatable<AgentState>
    ctor()
    string AgentId { get;  init; }
    ImmutableDictionary<string, string> BlackboardState { get;  init; }
    string CurrentRoleId { get;  init; }
    int ExecutionStepCount { get;  init; }
    string Goal { get;  init; }
    bool HasPendingWork { get; }
    bool IsTerminal { get; }
    DateTime? LastCheckpoint { get;  init; }
    string LastError { get;  init; }
    long LastSequenceNumber { get;  init; }
    ImmutableList<MessageBlock> Messages { get;  init; }
    ImmutableDictionary<string, PendingToolCall> PendingToolCalls { get;  init; }
    WorkflowPhase Phase { get;  init; }
    ImmutableList<string> Plan { get;  init; }
    TaskGraph TaskGraph { get;  init; }
    ImmutableList<WorkerMessageEnvelope> UnprocessedMessages { get;  init; }
    static AgentState Initial(string agentId)
  sealed class PendingToolCall : IEquatable<PendingToolCall>
    ctor(string CorrelationId, string ToolName, string ParametersJson, DateTime StartedAt)
    string CorrelationId { get;  init; }
    string ParametersJson { get;  init; }
    DateTime StartedAt { get;  init; }
    string ToolName { get;  init; }
  sealed class TickResult : IEquatable<TickResult>
    ctor(AgentState State, IReadOnlyList<AgentEvent> EmittedEvents, WorkflowPhase NextPhase, bool ShouldContinue, string Message = null)
    IReadOnlyList<AgentEvent> EmittedEvents { get;  init; }
    string Message { get;  init; }
    WorkflowPhase NextPhase { get;  init; }
    bool ShouldContinue { get;  init; }
    AgentState State { get;  init; }
  sealed class WorkflowEngine
    ctor(IEventStore eventStore, IContextBuilder contextBuilder, IBlackboard blackboard, IWorkerBus workerBus, WorkflowEngineOptions options = null)
    IAsyncEnumerable<TickResult> RunAsync(AgentState state, CancellationToken ct = null)
    Task<TickResult> TickAsync(AgentState state, CancellationToken ct = null)
  sealed class WorkflowEngineOptions : IEquatable<WorkflowEngineOptions>
    ctor()
    TimeSpan CheckpointInterval { get;  init; }
    bool EnableVerification { get;  init; }
    TimeSpan MaxExecutionTime { get;  init; }
    int MaxStepsPerTick { get;  init; }
    int MaxToolCallsPerStep { get;  init; }
  enum WorkflowPhase
    Idle
    IngestEvents
    MaybePlan
    SelectRunnableTasks
    AcquireLease
    BuildContext
    ExecuteStep
    Checkpoint
    Verify
    CommitEvents
    ScheduleNext
    Blocked
    Completed
    Failed

namespace Ikon.Agent.Processors.Analytics
  sealed class AnalyticsStreamProcessor : StreamProcessorBase
    ctor(int maxEntries = 100)
    string ProcessorId { get; }
    Opcode SupportedOpcodeGroup { get; }
    override string GetSnapshot(SnapshotOptions options = null)
    override void Process(ProtocolMessage message, IProtocolMessagePayload payload)

namespace Ikon.Agent.Processors.Audio
  sealed class AudioStreamProcessor : StreamProcessorBase
    ctor()
    string ProcessorId { get; }
    Opcode SupportedOpcodeGroup { get; }
    IReadOnlyList<string> GetActiveStreamIds()
    override string GetSnapshot(SnapshotOptions options = null)
    override void Process(ProtocolMessage message, IProtocolMessagePayload payload)

namespace Ikon.Agent.Processors.UI
  class VirtualUIRenderer.ActionInfo : IEquatable<VirtualUIRenderer.ActionInfo>
    ctor(string ActionId, string ActionType, string NodeId, string NodeType)
    string ActionId { get;  init; }
    string ActionType { get;  init; }
    string NodeId { get;  init; }
    string NodeType { get;  init; }
  class VirtualUIRenderer.PayloadInfo : IEquatable<VirtualUIRenderer.PayloadInfo>
    ctor(string Id, string MimeType, int SizeBytes)
    string Id { get;  init; }
    string MimeType { get;  init; }
    int SizeBytes { get;  init; }
  sealed class UIStreamProcessor : StreamProcessorBase
    ctor(Func<string, string, Task> onActionInvoke = null)
    string CurrentViewId { get; }
    string ProcessorId { get; }
    Opcode SupportedOpcodeGroup { get; }
    long Version { get; }
    void ClearChanges()
    IReadOnlyList<VirtualUIRenderer.ActionInfo> GetAvailableActions()
    override string GetSnapshot(SnapshotOptions options = null)
    Task<bool> InvokeActionAsync(string actionId, string argumentsJson = null)
    override void Process(ProtocolMessage message, IProtocolMessagePayload payload)
  sealed class VirtualUIRenderer
    ctor(Func<string, string, Task> onActionInvoke = null)
    string CurrentViewId { get; }
    long Version { get; }
    void ApplyStyles(UIStyles styles)
    void ApplyStyles(UIStylesBatch batch)
    bool ApplyUpdate(UIUpdate update)
    bool ApplyUpdateJson(string json)
    void ClearChanges()
    IReadOnlyList<VirtualUIRenderer.ActionInfo> GetAvailableActions()
    string GetSnapshot(bool markUpdates = true)
    string GetSnapshot(SnapshotOptions options)
    Task<bool> InvokeActionAsync(string actionId, string argumentsJson = null)

namespace Ikon.Agent.Services
  sealed class ContextBuilder : IContextBuilder
    ctor()
    Task<KernelContext> BuildContextAsync(AgentContextRequest request, CancellationToken ct = null)
    Task<KernelContext> BuildContextAsync(AgentContextRequest request, IEnumerable<string> packNames, CancellationToken ct = null)
    IReadOnlyList<IContextPack> GetPacks()
    void RegisterPack(IContextPack pack)
    void UnregisterPack(string packName)
  sealed class FileArtifactStore : IArtifactStore
    ctor(string basePath)
    Task DeleteAsync(string artifactId, CancellationToken ct = null)
    Task<bool> ExistsAsync(string artifactId, CancellationToken ct = null)
    Task<byte[]> GetAsync(string artifactId, CancellationToken ct = null)
    Task<ArtifactRef> GetMetadataAsync(string artifactId, CancellationToken ct = null)
    IAsyncEnumerable<ArtifactRef> ListAsync(string prefix = null, CancellationToken ct = null)
    Task<Stream> OpenReadAsync(string artifactId, CancellationToken ct = null)
    Task<ArtifactRef> StoreAsync(byte[] data, string mimeType, ArtifactStoreOptions options = null, CancellationToken ct = null)
    Task<ArtifactRef> StoreAsync(Stream stream, string mimeType, ArtifactStoreOptions options = null, CancellationToken ct = null)
  sealed class FileBackedLearningStore : ILearningStore
    ctor(string basePath)
    Task AddLearningAsync(TaskLearning learning, CancellationToken ct = null)
    Task ClearAsync(CancellationToken ct = null)
    IAsyncEnumerable<TaskLearning> GetAllLearningsAsync(CancellationToken ct = null)
    Task<IReadOnlyList<TaskLearning>> GetRelevantLearningsAsync(string context, int maxCount = 10, CancellationToken ct = null)
    Task RecordFailureAsync(string learningId, CancellationToken ct = null)
    Task ReinforceLearningAsync(string learningId, CancellationToken ct = null)
  sealed class InMemoryBlackboard : IBlackboard
    ctor()
    Task ClearAsync(CancellationToken ct = null)
    Task<bool> ContainsAsync(string key, CancellationToken ct = null)
    IAsyncEnumerable<KeyValuePair<string, object>> GetAllAsync(CancellationToken ct = null)
    Task<T> GetAsync<T>(string key, CancellationToken ct = null)
    IAsyncEnumerable<string> ListKeysAsync(string prefix = "", CancellationToken ct = null)
    Task RemoveAsync(string key, CancellationToken ct = null)
    Task<string> ResolveArtifactPointerAsync(string key, CancellationToken ct = null)
    Task SetAsync<T>(string key, T value, CancellationToken ct = null)
    Task<string> StoreArtifactPointerAsync(string key, string artifactId, CancellationToken ct = null)
  sealed class InMemoryLearningStore : ILearningStore
    ctor()
    Task AddLearningAsync(TaskLearning learning, CancellationToken ct = null)
    Task ClearAsync(CancellationToken ct = null)
    IAsyncEnumerable<TaskLearning> GetAllLearningsAsync(CancellationToken ct = null)
    Task<IReadOnlyList<TaskLearning>> GetRelevantLearningsAsync(string context, int maxCount = 10, CancellationToken ct = null)
    Task RecordFailureAsync(string learningId, CancellationToken ct = null)
    Task ReinforceLearningAsync(string learningId, CancellationToken ct = null)
  sealed class InProcessWorkerBus : IAsyncDisposable, IWorkerBus
    ctor()
    Task BroadcastAsync(WorkerMessageEnvelope envelope, CancellationToken ct = null)
    ValueTask DisposeAsync()
    Task<IReadOnlyList<string>> GetRegisteredAgentsAsync(CancellationToken ct = null)
    IAsyncEnumerable<WorkerMessageEnvelope> ReceiveAsync(string agentId, CancellationToken ct = null)
    Task RegisterAgentAsync(string agentId, CancellationToken ct = null)
    Task SendAsync(WorkerMessageEnvelope envelope, CancellationToken ct = null)
    Task UnregisterAgentAsync(string agentId, CancellationToken ct = null)
  sealed class JsonlEventStore : IAsyncDisposable, IEventStore
    ctor(string basePath)
    Task AppendAsync(AgentEvent evt, CancellationToken ct = null)
    Task AppendBatchAsync(IEnumerable<AgentEvent> events, CancellationToken ct = null)
    ValueTask DisposeAsync()
    Task<long> GetEventCountAsync(string agentId, CancellationToken ct = null)
    Task<long> GetLatestSequenceAsync(string agentId, CancellationToken ct = null)
    IAsyncEnumerable<AgentEvent> ReadFromAsync(string agentId, long fromSequence = 0, CancellationToken ct = null)

namespace Ikon.Agent.Services.Packs
  sealed class GoalPack : IContextPack
    ctor()
    string Name { get; }
    int Priority { get; }
    Task<ContextPackResult> BuildAsync(AgentContextRequest request, CancellationToken ct = null)
  sealed class PlanPack : IContextPack
    ctor()
    string Name { get; }
    int Priority { get; }
    Task<ContextPackResult> BuildAsync(AgentContextRequest request, CancellationToken ct = null)
  sealed class RecentPack : IContextPack
    ctor(int maxMessages = 20)
    string Name { get; }
    int Priority { get; }
    Task<ContextPackResult> BuildAsync(AgentContextRequest request, CancellationToken ct = null)
  sealed class ScratchpadPack : IContextPack
    ctor(IBlackboard blackboard, int maxEntries = 50)
    string Name { get; }
    int Priority { get; }
    Task<ContextPackResult> BuildAsync(AgentContextRequest request, CancellationToken ct = null)
  sealed class TasksPack : IContextPack
    ctor()
    string Name { get; }
    int Priority { get; }
    Task<ContextPackResult> BuildAsync(AgentContextRequest request, CancellationToken ct = null)
  sealed class WorkerThreadPack : IContextPack
    ctor(int maxMessages = 20)
    string Name { get; }
    int Priority { get; }
    Task<ContextPackResult> BuildAsync(AgentContextRequest request, CancellationToken ct = null)

namespace Ikon.Agent.Summarization
  sealed class AgentSummarizer : IStreamSummarizer
    ctor()
    Task<StreamSummary> SummarizeAsync(string history, string uiSnapshot, IReadOnlyList<string> overlapEvents, IReadOnlyList<string> newEvents, IReadOnlyList<string> availableActions, CancellationToken cancellationToken = null)
  interface IStreamSummarizer
    abstract Task<StreamSummary> SummarizeAsync(string history, string uiSnapshot, IReadOnlyList<string> overlapEvents, IReadOnlyList<string> newEvents, IReadOnlyList<string> availableActions, CancellationToken cancellationToken = null)
  class StreamSummary
    ctor()
    string History { get;  set; }
    string Important { get;  set; }
    string RecommendedActions { get;  set; }
    string Summary { get;  set; }
