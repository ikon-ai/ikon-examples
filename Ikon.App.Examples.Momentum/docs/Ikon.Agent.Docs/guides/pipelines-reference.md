# Pipelines Reference

## Pipelines API Reference

Full Pipeline framework reference and guide.

---

# Ikon.Pipeline Public API

namespace Ikon.Pipeline
  // Empty configuration sentinel for pipelines that need a host (for IPipelineHost<TConfig>.Secrets, IPipelineHost<TConfig>.OrganisationId, IPipelineHost<TConfig>.SpaceId) but no user-defined configuration.
  sealed class EmptyPipelineConfig
    ctor()
  // Exposes a pre-existing pipeline from a framework assembly. Place it on the [App] decorated class or on empty marker classes.
  sealed class ExposePipelineAttribute : Attribute
    ctor(Type pipelineType, string? name = null, PipelineExecutionMode executionMode = None, string? schedule = null)
    // Any value other than None overrides the execution mode defined on the original [Pipeline] attribute.
    PipelineExecutionMode ExecutionMode { get; }
    // Overrides the name used for the pipeline endpoint URL; the original pipeline name is used when not set.
    string? Name { get; }
    // Must be decorated with [Pipeline].
    Type PipelineType { get; }
    // If set, overrides the schedule defined on the original [Pipeline] attribute. The same 5-minute minimum interval applies as on PipelineAttribute.Schedule.
    string? Schedule { get; }
  static class FunctionRegistryExtensions
    // registry: The function registry.
    // functionName: Name of the function to register.
    // description: Optional description for the function.
    // configInstance: Optional configuration instance for the pipeline.
    static void RegisterPipeline<TPipeline>(this FunctionRegistry registry, string functionName, string? description = null, object? configInstance = null) where TPipeline : class
  interface IPipelineHost<out TConfig>
    TConfig Config { get; }
    // Id of the organisation that owns the space this pipeline is running in. Empty when the pipeline runs without a space-scoped backend token.
    string OrganisationId { get; }
    // Secrets (API keys, tokens, passwords) configured for the space this pipeline is running in. Fetched from the Ikon backend once when the runner constructs the pipeline; rotating a secret with ikon app secret set while the pipeline is running only takes effect after a re-run.
    Secrets Secrets { get; }
    // Empty when the pipeline runs without a space-scoped backend token.
    string SpaceId { get; }
  // For the special case where a path on the local filesystem is needed — normally use the Item.GetContent* methods. Item.GetLocalFile copies an Item's content to a local file; constructing a LocalFile directly gives a temporary file path to write to (no file exists until written), from which a new Item can be created. The MIME type determines the temporary file's extension and is used when creating an Item from the LocalFile. Dispose deletes the file only when it was created as temporary; a file supplied as existingFilePath is never deleted.
  sealed class LocalFile : IDisposable
    // mimeType: MIME type of the file.
    // existingFilePath: Optional existing file path to use. If not provided, a temporary file path will be created.
    ctor(string mimeType, string? existingFilePath = null)
    string MimeType { get; }
    string Path { get; }
    void Dispose()
  sealed class Pipeline<T> where T : IItem<T>
    Task Completion { get; }
    Pipeline<T>.PipelineStatus Status { get; }
    void Complete()
    Pipeline<T>.Branch Inputs()
    // throws PipelineException: The configured maximum input item count has been exceeded.
    bool Post(T item)
    event Pipeline<T>.AsyncEventHandler<T>? Output
  delegate Pipeline<T>.AsyncEventHandler<in TEventArgs> where T : IItem<T>
    Task AsyncEventHandler<in TEventArgs>(object sender, TEventArgs e)
  // Prefer the expression-based Transform/TransformStream/TransformBatch/TransformGroup overloads over their *Lambda counterparts: only expressions can run remotely, and only expressions cache correctly. An expression's captured variable values are hashed into the processor id, so changing a captured value invalidates that step's cache. A *Lambda step is ALSO cached, but under a name-only key with no captured-value fingerprint — change a captured value and the step silently replays the old cached output. To force a lambda step to re-run, pass skipCache: true.
  sealed class Pipeline<T>.Branch where T : IItem<T>
    ctor(Pipeline<T> outer, ISourceBlock<T> sourceBlock, IDataflowBlock dataflowBlock)
    // predicate: Predicate that decides whether an item should continue through the branch.
    // maxParallelism: Optional maximum degree of parallelism for evaluating the predicate.
    Pipeline<T>.Branch Filter(Func<T, Task<bool>> predicate, int? maxParallelism = null)
    // Matching is by exact object-type name, not by assignability: only items produced via Item.CreateFromObject<TObject> with the identical concrete TObject pass. An item stored as a derived type, or one that would merely deserialize into a base class or interface of TObject, is filtered out.
    // maxParallelism: Optional maximum degree of parallelism for the filter.
    Pipeline<T>.Branch Filter<TObject>(int? maxParallelism = null)
    // Terminal: ends the branch. Use a Transform* method instead to keep processing downstream.
    // func: Action to invoke for each item.
    // maxParallelism: Optional maximum degree of parallelism.
    void ForEach(Func<T, Task> func, int? maxParallelism = null)
    // branches: Branches to merge with the current branch.
    Pipeline<T>.Branch Merge(params Pipeline<T>.Branch[] branches)
    // Terminal: ends the branch. Sends each item to the pipeline's configured output(s).
    // maxParallelism: Optional maximum degree of parallelism for the output operation.
    void Output(int? maxParallelism = null)
    // item: The item to post.
    // throws PipelineException: This branch is not the pipeline input branch, the maximum input item count has been exceeded, or the item was declined because the pipeline has been completed.
    void Post(T item)
    // items: Items to be posted.
    // throws PipelineException: This branch is not the pipeline input branch, the maximum input item count has been exceeded, or an item was declined because the pipeline has been completed.
    void Post(List<T> items)
    // Await the returned task to observe completion and surface any errors from draining stream.
    // stream: Sequence producing items to post.
    // throws PipelineException: This branch is not the pipeline input branch, the maximum input item count has been exceeded, or an item was declined because the pipeline has been completed.
    Task Post(IAsyncEnumerable<T> stream)
    // transformExpr: Expression representing the transformation.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch Transform(Expression<Func<T, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // transformExpr: Expression representing the batch transformation.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // maxBatchSize: When specified, size of the batch to trigger processing.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch TransformBatch(Expression<Func<List<T>, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, int? maxBatchSize = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // transformFunc: Function that transforms a batch of items.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // maxBatchSize: When specified, size of the batch to trigger processing.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch TransformBatchLambda(Func<List<T>, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, int? maxBatchSize = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // groupKeySelectorExpr: Expression selecting the group key from an item.
    // transformExpr: Expression that processes a group of items.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch TransformGroup(Expression<Func<T, Task<string>>> groupKeySelectorExpr, Expression<Func<List<T>, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // groupKeyFunc: Function producing the group key for an item.
    // transformFunc: Function that transforms a group of items sharing the same key.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch TransformGroupLambda(Func<T, Task<string>> groupKeyFunc, Func<List<T>, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // transformFunc: Transformation function.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch TransformLambda(Func<T, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // transformExpr: Expression representing the transformation.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch TransformStream(Expression<Func<T, IAsyncEnumerable<T>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // transformExpr: Expression representing the stream transformation.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch TransformStream(Expression<Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // transformFunc: Transformation function producing a stream of items.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch TransformStreamLambda(Func<T, IAsyncEnumerable<T>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // transformFunc: Transformation function operating on a stream of items.
    // id: Optional processor identifier override.
    // maxParallelism: Optional maximum degree of parallelism.
    // maxRetries: Optional maximum number of retries.
    // skipCache: Whether to bypass caching for this step.
    // allowDuplicates: Whether duplicate items produced by this step should be preserved.
    // tags: Processor tags.
    // retryableExceptionTypes: Exception types that should trigger a retry.
    Pipeline<T>.Branch TransformStreamLambda(Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
  sealed class Pipeline<T>.PipelineStatus where T : IItem<T>
    ctor()
    TimeSpan Duration { get; init; }
    int ErrorLogCount { get; init; }
    int ProcessFailureCount { get; init; }
    int ProcessRetryCount { get; init; }
    int ProcessedItemCacheHits { get; init; }
    int ProcessedItemCount { get; init; }
    int WarningLogCount { get; init; }
  sealed class Pipeline<T>.RemoteCall where T : IItem<T>
    ctor(Pipeline<T> pipeline, object? instance, string processorName, object?[] args)
    object?[] Args { get; }
    object? Instance { get; }
    Pipeline<T> Pipeline { get; }
    string ProcessorName { get; }
  static class Pipeline<T>.RemoteCallHelper where T : IItem<T>
    static object? BlockOnResult(Task<object?> task)
    static Task<object?> CallRemoteAsync(Pipeline<T> pipeline, object? instance, MethodInfo method, ProcessorAttribute attr, object[] args)
    static IAsyncEnumerable<TR?> CallRemoteStreamAsync<TR>(Pipeline<T> pipeline, object? instance, MethodInfo method, ProcessorAttribute attr, object[] args)
    static Task<RT?> CastTaskResult<RT>(Task<object?> task)
    static Task IgnoreTaskResult(Task<object?> task)
  sealed class PipelineAttribute : Attribute
    ctor(string description = "", int version = 1, string guid = "", Type? inputSchema = null, Type? resultSchema = null, string? name = null, int maxInputItems = 0, PipelineExecutionMode executionMode = None, string? schedule = null)
    string Description { get; }
    PipelineExecutionMode ExecutionMode { get; }
    string Guid { get; }
    // POD class modelling the expected input format for external use; not enforced by the pipeline runtime.
    Type? InputSchema { get; }
    // 0 (the default) means no limit; when exceeded the pipeline throws a PipelineException.
    int MaxInputItems { get; }
    // Defaults to the class name. The name generates the pipeline URL endpoint, converted to kebab-case.
    string? Name { get; }
    // POD class modelling the expected output format for external use; not enforced by the pipeline runtime.
    Type? ResultSchema { get; }
    // Cron schedule expression (standard 5/6-field cron syntax). Only used when ExecutionMode is Scheduled. The platform enforces a minimum interval of 5 minutes: a faster schedule is clamped to a slower equivalent when a safe one exists, and rejected at bundle time otherwise.
    string? Schedule { get; }
    int Version { get; }
  sealed class PipelineException : Exception
    ctor()
    ctor(string message)
    ctor(string message, Exception innerException)
  static class PipelineFunction
    // functionName: Name of the function to register.
    // description: Optional description for the function.
    // configInstance: Optional configuration instance for the pipeline.
    static Function Create<TPipeline>(string functionName, string? description = null, object? configInstance = null) where TPipeline : class
  // Transport-friendly representation of a pipeline item for remote function calls. Contains the actual content data, not just a cache reference like Item.
  readonly struct PipelineFunctionItem
    byte[] Content { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    List<string>? Tags { get; init; }
    static PipelineFunctionItem FromBytes(string name, byte[] content, string? mimeType = null, List<string>? tags = null)
    static PipelineFunctionItem FromString(string name, string content, string? mimeType = null, List<string>? tags = null)
    string GetContentAsString()
  sealed class PipelineRunner : IDisposable
    // Only one runner may exist per process at a time — the runner registers a process-global adapter, so constructing a second while one is still alive (even in a different async context) throws.
    ctor()
    void Dispose()
    // config: Runner configuration.
    Task Initialize(PipelineRunner.Config config)
    // userPipelineInstance: Optional user pipeline instance to use.
    // userConfigInstance: Optional user configuration instance for the pipeline.
    // usePersistentCache: Whether persistent caches should be used.
    // keepRunning: Whether the runner should keep watching for input.
    // outputPath: Optional output path that will be used instead of in-memory output.
    Task Initialize<TPipeline>(TPipeline? userPipelineInstance = default, object? userConfigInstance = null, bool usePersistentCache = false, string? cachePath = null, bool keepRunning = false, string? outputPath = null, bool allApiKeys = false) where TPipeline : class
    Task InitializeForUnitTest()
    // items: Optional set of in-memory items to feed into the pipeline.
    // cancellationToken: Token used to cancel pipeline execution.
    Task<List<Item>> Run(List<Item>? items = null, CancellationToken cancellationToken = default)
    // items: Optional set of in-memory items to feed into the pipeline.
    // cancellationToken: Token used to cancel pipeline execution.
    IAsyncEnumerable<Item> RunAsEnumerable(List<Item>? items = null, CancellationToken cancellationToken = default)
    // configJson: JSON serialized configuration.
    // onStatusUpdate: Callback to receive JSON serialized status updates.
    // cancellationToken: Token used to cancel pipeline execution.
    static Task RunInExternalAssembly(string configJson, Action<string> onStatusUpdate, CancellationToken cancellationToken)
    // config: Configuration for the pipeline runner.
    // onStatusUpdate: Callback invoked with live status updates.
    // cancellationToken: Token used to cancel remote execution.
    static Task RunRemote(PipelineRunner.Config config, Action<PipelineStatus> onStatusUpdate, CancellationToken cancellationToken = default)
    // items: Optional set of in-memory items to feed into the pipeline.
    // cancellationToken: Token used to cancel pipeline execution.
    Task RunWithoutCollecting(List<Item>? items = null, CancellationToken cancellationToken = default)
    event Pipeline<Item>.AsyncEventHandler<Item>? Output
    event EventHandler<PipelineStatus>? StatusUpdate
  sealed class PipelineRunner.Config
    ctor()
    // Requests all API keys from the backend; admin only.
    bool AllApiKeys { get; set; }
    string? CachePath { get; set; }
    bool ClearCache { get; set; }
    string? ConfigPath { get; set; }
    CacheType ContentCacheType { get; set; }
    bool DefaultDisableProcessCache { get; set; }
    // Default degree of parallelism for processors when not overridden. Defaults to Environment.ProcessorCount × 4 when left null.
    int? DefaultMaxProcessParallelism { get; set; }
    int? DefaultMaxRetries { get; set; }
    List<string>? DefaultRetryableExceptionTypes { get; set; }
    bool DisableInputCache { get; set; }
    bool DisableMetadataOutput { get; set; }
    bool DisableOutputCache { get; set; }
    string? DllPath { get; set; }
    bool EnableRemoteClient { get; set; }
    bool EnableRemoteHost { get; set; }
    bool EnableSseOutput { get; set; }
    bool EnumerateZips { get; set; }
    string? FinalStatusPath { get; set; }
    string IkonBackendToken { get; set; }
    string IkonBackendUrl { get; set; }
    List<string>? InputPaths { get; set; }
    bool IsTestRun { get; set; }
    bool KeepRunning { get; set; }
    int LogFilter { get; set; }
    int? MaxInputReadParallelism { get; set; }
    int? MaxRemoteRequestParallelism { get; set; }
    bool OutputFinalStatus { get; set; }
    List<string>? OutputPaths { get; set; }
    int ProcessFailureThreshold { get; set; }
    string? ProcessingId { get; set; }
    string? RabbitMQConnectionString { get; set; }
    bool RecursiveInput { get; set; }
    List<string>? RemoteClientProcessorWhiteList { get; set; }
    // Seconds between input scans when KeepRunning is enabled. Default 10.
    int ScanInterval { get; set; }
    StateType StateType { get; set; }
    // Seconds between status update callbacks. Default 15.
    int StatusUpdateInterval { get; set; }
    // Fully qualified name of the pipeline type to execute.
    string TypeName { get; set; }
    object? UserConfigInstance { get; set; }
    object? UserPipelineInstance { get; set; }
  // Invokes the PipelineRunner from an external assembly. For internal use only.
  sealed class PipelineRunnerInvoker
    // pipelineDllPath: Path to the pipeline executable bundle.
    static Task<PipelineRunnerInvoker> Create(string pipelineDllPath)
    // configJson: Serialized configuration for the run.
    // onStatusUpdate: Callback invoked with serialized status updates.
    // cancellationToken: Token that cancels the running pipeline.
    Task Run(string configJson, Action<string> onStatusUpdate, CancellationToken cancellationToken)
  sealed class PipelineStatus
    ctor()
    int DuplicateItemCount { get; set; }
    TimeSpan Duration { get; set; }
    int ErrorLogCount { get; set; }
    bool HasCompleted { get; set; }
    bool HasFaulted { get; set; }
    int InputItemCacheHits { get; set; }
    int InputItemCacheMiss { get; }
    int InputItemCount { get; set; }
    int InvalidItemCount { get; set; }
    int OutputItemCacheHits { get; set; }
    int OutputItemCacheMiss { get; }
    int OutputItemCount { get; set; }
    int ProcessFailureCount { get; set; }
    int ProcessRetryCount { get; set; }
    int ProcessedItemCacheHits { get; set; }
    int ProcessedItemCacheMiss { get; }
    int ProcessedItemCount { get; set; }
    string ProcessingId { get; set; }
    DateTime StartTime { get; set; }
    Dictionary<string, double> Usages { get; set; }
    int WarningLogCount { get; set; }
    bool WasCancelled { get; set; }
  sealed class ProcessorAttribute : Attribute
    ctor(string? id = null, int version = 1, int maxParallelism = 0, int maxRetries = -1, bool isRemote = false, bool skipCache = false, bool allowDuplicates = true, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Defaults to true (duplicates preserved). Set to false to enable deduplication based on content hash and group id.
    bool AllowDuplicates { get; set; }
    string? Id { get; set; }
    bool IsRemote { get; set; }
    int MaxParallelism { get; set; }
    // Maximum retries on failure. The default of -1 means "not set" and falls back to the pipeline defaults; an explicit 0 disables retries for this processor.
    int MaxRetries { get; set; }
    // When left null (not set) the pipeline defaults are used; an explicitly-empty array means retry nothing, matching the pipeline-level default semantics.
    Type[]? RetryableExceptionTypes { get; set; }
    bool SkipCache { get; set; }
    ProcessorTags[] Tags { get; set; }
    // Feeds the processor cache hash; bump the version to invalidate previously cached outputs.
    int Version { get; set; }
  enum ProcessorTags
    Gpu

namespace Ikon.Pipeline.ContentCache
  enum CacheType
    InMemory
    FileSystem

namespace Ikon.Pipeline.Items
  interface IItem<out T>
    Task<bool> IsObjectAsync<TObject>()
    // processId: Identifier associated with the processor run.
    T WithProcessId(Guid processId)
  // Immutable, lightweight pointer: it carries a content hash, not the bytes (which live in the content cache). Produce modified copies via the With* methods rather than mutating. The hash is derived from content, MIME type, parent hashes, and tags, so any of those differing yields a distinct item. MIME type is auto-detected from the content when not supplied and sets the output file extension.
  readonly struct Item : IItem<Item>
    // Do not construct directly — always create items via the static Create, CreateInitial, or CreateFromObject methods.
    ctor()
    string GroupId { get; init; }
    string Hash { get; init; }
    // For internal use.
    string? InitialPath { get; init; }
    bool IsDefault { get; }
    ItemMetadata? Metadata { get; init; }
    string MimeType { get; init; }
    // Used as the filename when outputting; the extension comes from the MIME type.
    string Name { get; init; }
    IReadOnlyList<string> ParentHashes { get; init; }
    Guid ProcessId { get; init; }
    IReadOnlyList<string>? Tags { get; init; }
    // Called from processors during the run; the parent items feed the new item's hash. To seed inputs before Run, use CreateInitial.
    // parents: Parent items used to compute the new item's hash.
    // name: Name of the new item.
    // content: Content stream.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags associated with the item.
    // metadata: Optional metadata.
    static Task<Item> Create(List<Item> parents, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: Content stream.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(Item parent, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parents: Parent items.
    // name: Name of the new item.
    // content: UTF-8 string content.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(List<Item> parents, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: UTF-8 string content.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(Item parent, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parents: Parent items.
    // name: Name of the new item.
    // content: Binary content.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(List<Item> parents, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: Binary content.
    // mimeTypeOverride: MIME type of the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(Item parent, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // parents: Parent items.
    // name: Name of the new item.
    // content: Local file containing the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(List<Item> parents, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: Local file containing the content.
    // tags: Optional tags.
    // metadata: Optional metadata.
    static Task<Item> Create(Item parent, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    // Serializes content to JSON. Use inside the pipeline; before Run use CreateInitialFromObject<T>.
    // parents: Parent items.
    // name: Name of the new item.
    // content: Object to serialize.
    // tags: Optional tags.
    // metadata: Optional metadata.
    // jsonSerializerOptions: Optional JSON serializer options.
    static Task<Item> CreateFromObject<T>(List<Item> parents, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // parent: Parent item.
    // name: Name of the new item.
    // content: Object to serialize.
    // tags: Optional tags.
    // metadata: Optional metadata.
    // jsonSerializerOptions: Optional JSON serializer options.
    static Task<Item> CreateFromObject<T>(Item parent, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // For seeding input items after the pipeline is initialized but before Run. Inside a running pipeline use Create instead.
    // name: Name of the item.
    // content: Stream containing the item content.
    // mimeTypeOverride: Optional MIME type to use instead of auto detection.
    // tags: Optional tags associated with the item.
    // metadata: Optional metadata for the item.
    static Task<Item> CreateInitial(string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // name: Name of the item.
    // content: UTF-8 string content.
    // mimeTypeOverride: Optional MIME type override.
    // tags: Optional tags associated with the item.
    // metadata: Optional metadata for the item.
    static Task<Item> CreateInitial(string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // name: Name of the item.
    // content: Binary content.
    // mimeTypeOverride: Optional MIME type override.
    // tags: Optional tags associated with the item.
    // metadata: Optional metadata for the item.
    static Task<Item> CreateInitial(string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // name: Name of the item.
    // content: Object to serialize.
    // metadata: Optional metadata for the item.
    // tags: Optional tags associated with the item.
    // jsonSerializerOptions: Optional JSON serializer options.
    static Task<Item> CreateInitialFromObject<T>(string name, T content, ItemMetadata? metadata = null, List<string>? tags = null, JsonSerializerOptions? jsonSerializerOptions = null)
    Task<byte[]> GetContentAsBytes()
    Task<TObject> GetContentAsObject<TObject>()
    Task<Stream> GetContentAsStream()
    Task<string> GetContentAsString()
    string GetGroupId()
    Task<string> GetGroupIdAsync()
    Task<LocalFile> GetLocalFile()
    string GetOriginalName()
    Task<string> GetOriginalNameAsync()
    string GetOriginalPath()
    Task<string> GetOriginalPathAsync()
    string GetPageId()
    Task<string> GetPageIdAsync()
    Task<List<Item>> GetParents()
    string GetProcessId()
    Task<string> GetProcessIdAsync()
    bool HasTags(params string[] tags)
    Task<bool> HasTagsAsync(params string[] tags)
    bool IsAudio()
    Task<bool> IsAudioAsync()
    bool IsBinary()
    Task<bool> IsBinaryAsync()
    bool IsCsv()
    Task<bool> IsCsvAsync()
    bool IsImage()
    Task<bool> IsImageAsync()
    bool IsJson()
    Task<bool> IsJsonAsync()
    bool IsMicrosoftExcel()
    Task<bool> IsMicrosoftExcelAsync()
    bool IsMicrosoftPowerpoint()
    Task<bool> IsMicrosoftPowerpointAsync()
    bool IsMicrosoftWord()
    Task<bool> IsMicrosoftWordAsync()
    // This is an exact object-type-name match against the item's MIME type, not an is-assignable check: it returns false for a base class or interface of the stored type even though GetContentAsObject<TObject> would deserialize such an item successfully. Do not use it to guard GetContentAsObject<TObject> against a base/interface TObject.
    bool IsObject<TObject>()
    bool IsObject()
    Task<bool> IsObjectAsync<TObject>()
    Task<bool> IsObjectAsync()
    bool IsPdf()
    Task<bool> IsPdfAsync()
    bool IsText()
    Task<bool> IsTextAsync()
    bool IsVideo()
    Task<bool> IsVideoAsync()
    bool IsXml()
    Task<bool> IsXmlAsync()
    // name: Optional new name.
    // mimeType: Optional MIME type override.
    // processId: Optional process identifier.
    // groupId: Optional group identifier.
    // tags: Optional tag collection.
    // metadata: Optional metadata override.
    Item With(string? name = null, string? mimeType = null, Guid? processId = null, string? groupId = null, List<string>? tags = null, ItemMetadata? metadata = null)
    Item WithProcessId(Guid processId)
    const string ObjectMimeTypePrefix
  static class ItemExtensions
    // Returns null when nothing matches — unlike FirstOrDefault, which yields a default Item struct that null checks cannot detect.
    static Item? FirstOrNull(this IEnumerable<Item> items, Func<Item, bool> predicate)
    // Returns null when the collection is empty — unlike FirstOrDefault, which yields a default Item struct that null checks cannot detect.
    static Item? FirstOrNull(this IEnumerable<Item> items)
  // When outputting an item that has metadata, the metadata is written alongside the item with a .meta.json extension. Immutable by design; use the With method to create modified copies.
  readonly struct ItemMetadata
    // Do not use. Use the constructor which takes a parent ItemMetadata instead.
    ctor()
    // Inherits values from the provided parent metadata where a parameter is not supplied.
    ctor(ItemMetadata? parent, string? previousItemName = null, string? nextItemName = null, string? originalPath = null, string? originalName = null, DateTime? createdAt = null, DateTime? updatedAt = null, string? documentType = null, string? documentTitle = null, IReadOnlyList<string>? titleHierarchy = null, int? pageNumber = null, IReadOnlyList<int>? pageNumbers = null, int? pageCount = null, IReadOnlyDictionary<string, string>? properties = null, string? customJson = null)
    DateTime? CreatedAt { get; init; }
    string? CustomJson { get; init; }
    string? DocumentTitle { get; init; }
    string? DocumentType { get; init; }
    string? NextItemName { get; init; }
    string? OriginalName { get; init; }
    string? OriginalPath { get; init; }
    int? PageCount { get; init; }
    int? PageNumber { get; init; }
    IReadOnlyList<int>? PageNumbers { get; init; }
    string? PreviousItemName { get; init; }
    IReadOnlyDictionary<string, string>? Properties { get; init; }
    IReadOnlyList<string>? TitleHierarchy { get; init; }
    DateTime? UpdatedAt { get; init; }
    ItemMetadata With(string? previousItemName = null, string? nextItemName = null, string? originalPath = null, string? originalName = null, DateTime? createdAt = null, DateTime? updatedAt = null, string? documentType = null, string? documentTitle = null, IReadOnlyList<string>? titleHierarchy = null, int? pageNumber = null, IReadOnlyList<int>? pageNumbers = null, int? pageCount = null, IReadOnlyDictionary<string, string>? properties = null, string? customJson = null)

namespace Ikon.Pipeline.Remote.Bus
  interface IRemoteCallBus
    // message: Invocation request.
    // cancellationToken: Token used to cancel the pending call.
    Task<RemoteCallResult> Client_CallHostFunction(RemoteCallMessage message, CancellationToken cancellationToken = default)
    // cancellationToken: Token used to cancel enumeration.
    virtual IAsyncEnumerable<RemoteCallResult> Client_GetFunctionCallResults(CancellationToken cancellationToken = default)
    // cancellationToken: Token used to cancel enumeration.
    IAsyncEnumerable<RemoteCallMessage> Client_GetProcessorCalls(CancellationToken cancellationToken = default)
    // result: Result generated by the host.
    Task Client_HostProcessorCallResult(RemoteCallResult result)
    // message: Invocation request.
    Task Host_CallProcessor(RemoteCallMessage message)
    // result: Result produced by the client.
    virtual Task Host_ClientFunctionCallResult(RemoteCallResult result)
    // cancellationToken: Token used to cancel enumeration.
    virtual IAsyncEnumerable<RemoteCallMessage> Host_GetFunctionCalls(CancellationToken cancellationToken = default)
    // cancellationToken: Token used to cancel enumeration.
    IAsyncEnumerable<RemoteCallResult> Host_GetProcessorCallResults(CancellationToken cancellationToken = default)
  sealed class RabbitMQRemoteCallBus : IDisposable, IRemoteCallBus
    Task<RemoteCallResult> Client_CallHostFunction(RemoteCallMessage message, CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallResult> Client_GetFunctionCallResults(CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallMessage> Client_GetProcessorCalls(CancellationToken cancellationToken = default)
    Task Client_HostProcessorCallResult(RemoteCallResult result)
    // connectionString: RabbitMQ connection string.
    // isHost: Whether the instance should accept host responsibilities.
    // isClient: Whether the instance should accept client responsibilities.
    // processorWhiteList: Optional whitelist restricting processors visible to the client.
    static Task<RabbitMQRemoteCallBus> CreateAsync(string connectionString, bool isHost, bool isClient, List<string>? processorWhiteList = null)
    void Dispose()
    Task Host_CallProcessor(RemoteCallMessage message)
    Task Host_ClientFunctionCallResult(RemoteCallResult result)
    IAsyncEnumerable<RemoteCallMessage> Host_GetFunctionCalls(CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallResult> Host_GetProcessorCallResults(CancellationToken cancellationToken = default)
    // processorNames: List of processor names to allow, or null to allow all.
    void SetWhiteList(List<string>? processorNames)
  sealed class RemoteCallMessage
    ctor()
    // processorName: Name of the processor to invoke.
    // args: Arguments passed to the processor.
    // correlationId: Correlation identifier for matching responses.
    ctor(string processorName, object?[] args, Guid correlationId)
    string[] ArgsJson { get; set; }
    Guid CorrelationId { get; set; }
    string ProcessorName { get; set; }
    // index: Position of the argument in ArgsJson.
    // throws PipelineException: index is outside the bounds of ArgsJson.
    T? GetArg<T>(int index)
  sealed class RemoteCallResult
    ctor()
    // processorName: Name of the processor that handled the request.
    // correlationId: Correlation identifier shared with the request.
    // resultJson: Serialized result payload.
    // remoteCallResultType: Completion status of the call.
    // errorMessage: Optional error description.
    ctor(string processorName, Guid correlationId, string? resultJson, RemoteCallResultType remoteCallResultType, string? errorMessage = "")
    Guid CorrelationId { get; set; }
    string? ErrorMessage { get; set; }
    string ProcessorName { get; set; }
    string? ResultJson { get; set; }
    RemoteCallResultType ResultType { get; set; }
    // Check ResultType before calling this. On RemoteCallResultType.Failed (and any other outcome that carries no payload) ResultJson is null, so this returns default(T) — for a value type that is a legitimate-looking zero, indistinguishable from a real result. Read ErrorMessage when ResultType is RemoteCallResultType.Failed.
    T? GetResult<T>()
  enum RemoteCallResultType
    Success
    Streaming
    StreamingDone
    Failed

namespace Ikon.Pipeline.Spec
  sealed class PipelineSpec
    ctor()
    object? Config { get; set; }
    string? Guid { get; set; }
    object? Input { get; set; }
    Dictionary<string, object?> OpenApiSpec { get; set; }
    object? Result { get; set; }
  static class PipelineSpecGenerator
    static PipelineSpec Generate(Type pipelineType, bool includeExamples = true)

namespace Ikon.Pipeline.State
  enum StateType
    InMemory
    Sqlite
    SqLiteBatch


---

# Ikon Pipeline Guide

## Overview

The Ikon Pipeline is a reactive asynchronous parallel data processing framework designed for high-performance workloads. It enables you to define the structure of a processing graph once while relying on an intelligent caching system to determine which steps need re-execution when the pipeline runs again.

Key capabilities:

- **Reactive scheduling**: The pipeline run specifies the structure of the processing graph. When executed, the caching system determines what needs to be re-processed based on what has changed since the last run (code, configuration, or input changes).
- **Fully asynchronous**: Every aspect of the pipeline operates asynchronously, from pipeline definition to runtime execution.
- **Parallel processing**: Processors run in parallel where dependencies allow, fully utilizing the processing power of the host machine.
- **Step-level caching**: Every processing step is cached with automatic invalidation based on processor identity, version, configuration, and input state. This avoids unnecessary re-processing and significantly speeds up subsequent runs.
- **Flexible execution**: Pipelines can be invoked directly from code or executed with the `ikon` CLI tool.
- **Distributed execution**: Support for remote host/client modes enables distributing processor execution across multiple machines.

## Defining and Running a Simple Pipeline

Create a pipeline class and annotate it with `[Pipeline]`. Implement a `Run` method with the required signature and compose processing steps using branch operations. Annotate processor methods with `[Processor]`.

```csharp
using Ikon.Common;
using Ikon.Common.Core;
using Ikon.Pipeline;
using Ikon.Pipeline.Items;

[Pipeline]
private class SimplePipeline
{
    // Pipelines must have a Run method with this signature
    // The cancellation token is optional and can be omitted
    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        // Transform one item at a time (but in parallel) using the MyProcessor function
        var outputItems = inputItems.Transform(item => MyProcessor(item, "my parameter", cancellationToken));

        // Output the processed items from the pipeline
        outputItems.Output();
    }

    // Processor input parameters are flexible - choose what you need
    [Processor]
    private static async Task<List<Item>> MyProcessor(Item inputItem, string myParameter, CancellationToken cancellationToken)
    {
        var content = await inputItem.GetContentAsString();
        content = $"{content} - Processed with parameter: {myParameter}";
        var outputItem = await Item.Create(inputItem, $"{inputItem.Name}.example", content, MimeTypes.TextPlain);

        return [outputItem]; // Can return empty list if no output is desired
    }
}
```

### Running the Pipeline

Instantiate a `PipelineRunner`, initialize it with the pipeline type, and submit items for processing.

```csharp
using Ikon.Pipeline;
using Ikon.Pipeline.Items;

using var pipelineRunner = new PipelineRunner();
await pipelineRunner.Initialize<SimplePipeline>();

List<Item> inputItems = [];

for (int i = 0; i < 10; i++)
{
    var item = await Item.CreateInitial($"item{i + 1}", $"Content of item {i + 1}", MimeTypes.TextPlain);
    inputItems.Add(item);
}

var outputItems = await pipelineRunner.Run(inputItems);

foreach (var outputItem in outputItems)
{
    var content = await outputItem.GetContentAsString();
    Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
}
```

### Streaming Results with RunAsEnumerable

`RunAsEnumerable` streams results as soon as processors emit them, which is useful for long-running workflows.

```csharp
using var pipelineRunner = new PipelineRunner();
await pipelineRunner.Initialize<SimplePipeline>();

List<Item> inputItems = [];

for (int i = 0; i < 10; i++)
{
    var item = await Item.CreateInitial($"item{i + 1}", $"Content of item {i + 1}", MimeTypes.TextPlain);
    inputItems.Add(item);
}

await foreach (var outputItem in pipelineRunner.RunAsEnumerable(inputItems))
{
    var content = await outputItem.GetContentAsString();
    Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
}
```

### Configuring the Runner

`PipelineRunner.Initialize` accepts a `PipelineRunner.Config` object for fine-grained control over processor retry limits, metadata output, type discovery, and more.

```csharp
using var pipelineRunner = new PipelineRunner();

var pipelineRunnerConfig = new PipelineRunner.Config
{
    TypeName = typeof(SimplePipeline).FullName!,
    ProcessFailureThreshold = 2,
    DisableMetadataOutput = true
    // Additional options available, such as cache paths, default retry configuration, and remote execution toggles
};

await pipelineRunner.Initialize(pipelineRunnerConfig);

List<Item> inputItems = [];

for (int i = 0; i < 10; i++)
{
    var item = await Item.CreateInitial($"item{i + 1}", $"Content of item {i + 1}", MimeTypes.TextPlain);
    inputItems.Add(item);
}

var outputItems = await pipelineRunner.Run(inputItems);

foreach (var outputItem in outputItems)
{
    var content = await outputItem.GetContentAsString();
    Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
}
```

### Cancellation Support

Pass a `CancellationToken` when invoking the pipeline to halt execution cooperatively.

```csharp
using var pipelineRunner = new PipelineRunner();
await pipelineRunner.Initialize<SimplePipeline>();

List<Item> inputItems = [];

for (int i = 0; i < 10; i++)
{
    var item = await Item.CreateInitial($"item{i + 1}", $"Content of item {i + 1}", MimeTypes.TextPlain);
    inputItems.Add(item);
}

List<Item> outputItems = [];
var cts = new CancellationTokenSource();

try
{
    outputItems = await pipelineRunner.Run(inputItems, cts.Token);
}
catch (OperationCanceledException)
{
    Log.Instance.Info("Pipeline run was cancelled");
}

foreach (var outputItem in outputItems)
{
    var content = await outputItem.GetContentAsString();
    Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
}
```

## Creating Items

Pipelines operate on immutable `Item` instances that carry content, metadata, and lineage.

### Initial Items

Initial items are created outside a pipeline run (but after pipeline initialization) and are meant to be given as input to a pipeline. Initial items do not have any parent item(s) and must not be created inside a processor.

```csharp
List<Item> inputItems = [];

// Create an initial item from a string
// MIME type specified for small text content as automatic detection may not work well
string stringContent = "This is a string content";
inputItems.Add(await Item.CreateInitial("string_item_name", stringContent, mimeTypeOverride: MimeTypes.TextPlain));

// Create an initial item from a byte array
// MIME type will be analyzed from the content
byte[] byteContent = new byte[1024];
inputItems.Add(await Item.CreateInitial("binary_item_name", byteContent));

// Create an initial item from a stream
// MIME type will be analyzed from the content
await using var stream = new MemoryStream(1024);
inputItems.Add(await Item.CreateInitial("stream_item_name", stream));

// Create an initial item from an object (will be serialized to JSON)
// MIME type will be set automatically
var exampleData = new ExampleData();
inputItems.Add(await Item.CreateInitialFromObject("object_item_name", exampleData));
```

The examples rely on a simple data transfer object for object-based items:

```csharp
private class ExampleData
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Occupation { get; set; } = string.Empty;
}
```

### Items Produced by Processors

Non-`Item.CreateInitial*` functions are meant to be used inside processors and (almost) always take in a parent item. The `name` parameter specifies the full item name. Use string interpolation to derive names from parent items. It is also possible, though uncommon, to create items without parents.

```csharp
Item parentItem = /* existing pipeline item */;
Item anotherParentItem = /* another pipeline item */;
List<Item> outputItems = [];

// Create an item from a string with single parent
string stringContent = "This is a string content";
outputItems.Add(await Item.Create(parentItem, $"{parentItem.Name}.name_suffix", stringContent, mimeTypeOverride: MimeTypes.TextPlain));

// Create an item from a string with multiple parents
outputItems.Add(await Item.Create([parentItem, anotherParentItem], "full_item_name", stringContent, mimeTypeOverride: MimeTypes.TextPlain));

// Create an item from a string without any parents (not recommended, but possible)
outputItems.Add(await Item.Create([], "full_item_name", stringContent, mimeTypeOverride: MimeTypes.TextPlain));

// Create an item from a byte array
// MIME type will be analyzed from the content
byte[] byteContent = new byte[1024];
outputItems.Add(await Item.Create(parentItem, $"{parentItem.Name}.name_suffix", byteContent));

// Create an item from a stream
// MIME type will be analyzed from the content
await using var stream = new MemoryStream(1024);
outputItems.Add(await Item.Create(parentItem, $"{parentItem.Name}.name_suffix", stream));

// Create an item from an object (will be serialized to JSON)
// MIME type will be set automatically
var exampleData = new ExampleData();
outputItems.Add(await Item.CreateFromObject(parentItem, $"{parentItem.Name}.name_suffix", exampleData));
```

## Reading Item Content

Items provide asynchronous helpers for working with content in multiple representations.

```csharp
Item parentItem = /* existing pipeline item */;

var stringItem = await Item.Create(parentItem, $"{parentItem.Name}.string", "This is a string content", mimeTypeOverride: MimeTypes.TextPlain);
var byteItem = await Item.Create(parentItem, $"{parentItem.Name}.bytes", new byte[1024]);
await using var stream = new MemoryStream(1024);
var streamItem = await Item.Create(parentItem, $"{parentItem.Name}.stream", stream);
var exampleData = new ExampleData { Name = "John Doe", Age = 30, Occupation = "Engineer" };
var objectItem = await Item.CreateFromObject(parentItem, $"{parentItem.Name}.object", exampleData);

// Get item content as string
string stringContent = await stringItem.GetContentAsString();
Log.Instance.Info($"String content: {stringContent}");

// Get item content as byte array
byte[] byteContent = await byteItem.GetContentAsBytes();
Log.Instance.Info($"Byte content length: {byteContent.Length}");

// Get item content as stream
await using Stream streamContent = await streamItem.GetContentAsStream();
Log.Instance.Info($"Stream content length: {streamContent.Length}");

// Get item content as deserialized object
ExampleData objectContent = await objectItem.GetContentAsObject<ExampleData>();
Log.Instance.Info($"Object content: Name={objectContent.Name}, Age={objectContent.Age}, Occupation={objectContent.Occupation}");
```

## Working with Local Files

Use `LocalFile` to interoperate with APIs that require filesystem access. Temporary files are cleaned up automatically when the `LocalFile` is disposed.

```csharp
Item parentItem = /* existing pipeline item */;
var sourceItem = await Item.Create(parentItem, $"{parentItem.Name}.bytes", new byte[1024]);

// Copy any item to a temporary local file system file
// Useful for external libraries that can only read from a file path
// The local file will be automatically deleted when disposed
using (var localFile = await sourceItem.GetLocalFile())
{
    Log.Instance.Info($"Local file, Path={localFile.Path}, MimeType={localFile.MimeType}");
}

// Create a temporary local file path for writing
// You can give this path to external libraries to write content to
// An item can then be created from the local file
// The file will be automatically deleted when disposed
using (var localFile = new LocalFile(MimeTypes.TextPlain))
{
    await File.WriteAllTextAsync(localFile.Path, "This is some text content");
    var outputItem = await Item.Create(parentItem, "my_item", localFile);
}
```

## Advanced Pipeline Composition

Pipelines can accept strongly typed configuration through dependency injection of `IPipelineHost<TConfig>` and provide rich branching primitives for filtering, batching, streaming, grouping, and observation.

```csharp
// If a config object is desired, the pipeline class can take in an IPipelineHost<TConfig> parameter
// The user supplies the config either as an object or JSON when running the pipeline
// The config will be accessible via the host.Config property
[Pipeline]
private class AdvancedPipeline(IPipelineHost<AdvancedPipeline.Config> host)
{
    // The config object is a user-defined POD class
    public class Config
    {
        public int ConfigValue1 { get; set; } = 1;
        public string ConfigValue2 { get; set; } = "ConfigValue";
    }

    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        // Filter items to only those having the "even" tag
        var evenItems = inputItems.Filter(item => item.HasTagsAsync("even"));

        // Filter items to only those having the "odd" tag
        var oddItems = inputItems.Filter(item => item.HasTagsAsync("odd"));

        // Filter items to only those that are objects of type ExampleData
        var objectItems = inputItems.Filter(item => item.IsObjectAsync<ExampleData>());

        // Filter items to only those that are images (based on MIME type)
        var imageItems = inputItems.Filter(item => item.IsImageAsync());

        // All Transform* functions take an expression; the easiest way is to pass a function with parameters
        // The variable values inside the expression are read and used to calculate a hash for the processor call
        // If any of the variable values change, then possible caching for that processor is skipped and it runs
        // If processor name, version, and expression variables are the same as a previous run, cached results are used

        // Process each item separately but in parallel
        evenItems = evenItems.Transform(item => MyProcessor(item, host.Config.ConfigValue2, cancellationToken));

        // Gather items into batches and process each batch in parallel
        // Batch size can be set with maxBatchSize parameter
        oddItems = oddItems.TransformBatch(items => MyBatchProcessor(items, host.Config.ConfigValue2, cancellationToken));

        // Process each item and produce multiple output items as a stream
        var itemToStreamItems = objectItems.TransformStream(item => MyItemToStreamProcessor(item, host.Config.ConfigValue2, cancellationToken));

        // Process multiple input items as a stream and produce multiple output items as a stream
        var streamToStreamItems = oddItems.TransformStream(items => MyStreamToStreamProcessor(items, host.Config.ConfigValue2, cancellationToken));

        // Merge multiple branches into one
        var mergedItems = evenItems.Merge(oddItems, itemToStreamItems, streamToStreamItems);

        // Group items by a key (here process ID) and process each group as a batch
        // Grouping ID can be any string value
        var groupProcessedItems = mergedItems.TransformGroup(item => item.GetProcessIdAsync(), items => MyBatchProcessor(items, host.Config.ConfigValue2, cancellationToken));

        // ForEach can be used to run code for each item without producing any output items
        imageItems.ForEach(async item =>
        {
            Log.Instance.Info($"Image item Name={item.Name}, MimeType={item.MimeType}");
        });

        // All Transform* functions also have a TransformLambda* counterpart that takes a lambda instead of an expression
        // Their use is discouraged as the lambda cannot be analyzed for variable values and thus caching is less effective
        // Also, transparent remote processor handling cannot be used with lambdas
        var doNotUseTransformLambdaItems = inputItems.TransformLambda(async item =>
        {
            return await MyProcessor(item, host.Config.ConfigValue2, cancellationToken);
        });

        // Calling output on any branch outputs those items from the pipeline
        groupProcessedItems.Output();
    }

    [Processor]
    private static async Task<List<Item>> MyProcessor(Item inputItem, string myParameter, CancellationToken cancellationToken)
    {
        var content = await inputItem.GetContentAsString();
        content = $"{content} - Single processed with parameter: {myParameter}";
        var outputItem = await Item.Create(inputItem, $"{inputItem.Name}.processed", content, MimeTypes.TextPlain);

        return [outputItem];
    }

    [Processor]
    private static async Task<List<Item>> MyBatchProcessor(List<Item> inputItems, string myParameter, CancellationToken cancellationToken)
    {
        List<Item> outputItems = [];

        foreach (var item in inputItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await item.GetContentAsString();
            content = $"{content} - Batch processed with parameter: {myParameter}";
            var outputItem = await Item.Create(item, $"{item.Name}.batch_processed", content, MimeTypes.TextPlain);
            outputItems.Add(outputItem);
        }

        return outputItems;
    }

    [Processor]
    private static async IAsyncEnumerable<Item> MyItemToStreamProcessor(Item inputItem, string myParameter, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // It is assumed that the input item is an object of type ExampleData
        var data = await inputItem.GetContentAsObject<ExampleData>();

        for (int i = 0; i < 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await inputItem.GetContentAsString();
            content = $"{content} - Streamed output {i + 1} with parameter {myParameter} for object {data.Name}, Age {data.Age}, Occupation {data.Occupation}";
            var outputItem = await Item.Create(inputItem, $"{inputItem.Name}.stream_processed{i + 1}", content, MimeTypes.TextPlain);
            yield return outputItem;
        }
    }

    [Processor]
    private static async IAsyncEnumerable<Item> MyStreamToStreamProcessor(IAsyncEnumerable<Item> inputItems, string myParameter, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in inputItems.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await item.GetContentAsString();
            content = $"{content} - Stream-to-stream processed with parameter: {myParameter}";
            var outputItem = await Item.Create(item, $"{item.Name}.stream2stream_processed", content, MimeTypes.TextPlain);
            yield return outputItem;
        }
    }
}
```

### Running the Advanced Pipeline

Supply a configuration instance, enable persistent caching, and provide rich input collections including tagged items and binary payloads.

```csharp
using var pipelineRunner = new PipelineRunner();

var myPipelineConfig = new AdvancedPipeline.Config
{
    ConfigValue1 = 42,
    ConfigValue2 = "The answer"
};

await pipelineRunner.Initialize<AdvancedPipeline>(
    userConfigInstance: myPipelineConfig, // Give the user config instance to the pipeline runner
    usePersistentCache: true // This Initialize overload has common useful options (for full control, see the overload taking PipelineRunner.Config)
);

List<Item> inputItems = [];

for (int i = 0; i < 10; i++)
{
    List<string> tags = i % 2 == 0 ? ["even"] : ["odd"];
    var item = await Item.CreateInitial($"item{i}", $"Content of item {i}", MimeTypes.TextPlain, tags);
    inputItems.Add(item);
}

inputItems.Add(await Item.CreateInitialFromObject("object_item", new ExampleData { Name = "Alice", Age = 28, Occupation = "Designer" }));
inputItems.Add(await Item.CreateInitial("image_item", new byte[2048], MimeTypes.ImagePng));

var outputItems = await pipelineRunner.Run(inputItems);

foreach (var outputItem in outputItems)
{
    var content = await outputItem.GetContentAsString();
    Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
}
```

## Reading Secrets and Space Context from a Pipeline

A pipeline that takes an `IPipelineHost<TConfig>` constructor parameter exposes three accessors
alongside `host.Config`:

- `host.Secrets` — secrets (API keys, tokens, passwords) for the current space. Manage values
  with `ikon app secret set/list/delete`.
- `host.OrganisationId` — id of the current organisation.
- `host.SpaceId` — id of the current space.

Use `EmptyPipelineConfig` when the pipeline has no user-defined configuration:

```csharp
[Pipeline]
public class FetchFromGithub(IPipelineHost<EmptyPipelineConfig> host)
{
    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        string token = host.Secrets["GITHUB_TOKEN"];

        if (host.Secrets.TryGet("GITHUB_API_BASE", out var apiBase))
        {
            Log.Instance.Info($"Using custom GitHub API base: {apiBase}");
        }

        Log.Instance.Info($"Running in organisation {host.OrganisationId} space {host.SpaceId}");

        // ...
        await Task.CompletedTask;
    }
}
```

Pipelines that already have a config type get the same accessors:

```csharp
[Pipeline]
public class TranscribeAudio(IPipelineHost<TranscribeAudio.Config> host)
{
    public class Config
    {
        public string Language { get; set; } = "en";
    }

    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        string apiKey = host.Secrets["OPENAI_API_KEY"];
        string lang = host.Config.Language;
        // ...
        await Task.CompletedTask;
    }
}
```

Indexer access throws if a secret is not set; use `TryGet` for optional secrets. Rotating a
value with `ikon app secret set` takes effect on the next pipeline run.

## Running Pipelines with the ikon CLI

Use `ikon pipeline run` to execute a pipeline outside your application code, or `ikon app pipeline run` from inside an Ikon AI app project for the common case where the DLL and space ID can be auto-resolved from the project.

### From an Ikon AI app project

Run from the app project root:

```bash
# Short name resolves to the matching [Pipeline] class in the app's DLL
ikon app pipeline run MyPipeline --input ./data/ --output ./output/

# Skip the rebuild step and reuse the previous build output
ikon app pipeline run MyPipeline --no-build

# Pick a non-default target config (e.g. ikon-config.production.toml)
ikon app pipeline run MyPipeline --target production
```

`ikon app pipeline run` builds the app, locates the output assembly, resolves the pipeline type, reads `Target.SpaceId` from `ikon-config.toml`, and exchanges for a space token automatically. All `ikon pipeline run` flags below pass through.

### Common Options

| Option | Description |
|--------|-------------|
| `--type-name` | Fully qualified pipeline type to execute. Required when running from pre-built assemblies or when multiple pipelines exist in the project. |
| `--dll-path` | Load the pipeline from an external assembly instead of the current project. |
| `--input` | One or more input files, directories (supports wildcards), or asset URIs. Separate multiple paths with commas. |
| `--recursive` | Recursively enumerate input directories and wildcards. |
| `--config` | Path to a JSON configuration file whose contents are provided to the pipeline host configuration model. |
| `--output` | One or more output destinations (files, directories, or asset URIs) where generated items should be written. Separate multiple paths with commas. |

### Example Usage

```bash
# Run a pipeline from a compiled DLL with input files
ikon pipeline run --dll-path ./bin/Release/MyPipeline.dll --type-name MyNamespace.MyPipeline --input ./data/*.json --output ./output/

# Run with configuration and recursive input scanning
ikon pipeline run --dll-path ./bin/Release/MyPipeline.dll --type-name MyNamespace.MyPipeline --input ./data/ --recursive --config ./pipeline-config.json --output ./output/
```

Additional parameters cover cache directories, retry configuration, remote execution flags, and status reporting. Run `ikon pipeline run --help` for a complete listing.

## Remote Host and Client Modes

The pipeline runner can operate in remote host and client modes to distribute processor execution across multiple machines. This enables scaling processor workloads horizontally using a message bus (RabbitMQ) for communication.

### Prerequisites

Before running distributed pipelines:

1. **RabbitMQ must be running**: The message bus must be operational and accessible before starting any host or client processes.

2. **Shared cache directory**: Host and all clients must have access to the same cache directory path. Items are transmitted through the message bus as lightweight metadata containing a content hash pointer. The actual content is stored in and read from the shared cache. On a single machine, use the same `--cache` path for all processes. For multi-machine deployments, use a shared network drive or NFS mount.

3. **Same pipeline DLL**: All processes (host and clients) must use the same compiled pipeline DLL.

### Defining Remote Processors

Mark processors for remote execution using the `isRemote` parameter in the `[Processor]` attribute:

```csharp
[Pipeline]
public class DistributedPipeline(IPipelineHost<DistributedPipeline.Config> host)
{
    public class Config
    {
        public int DelayMs { get; set; } = 100;
    }

    public async Task Run(Pipeline<Item>.Branch inputItems)
    {
        var stage1 = inputItems.Transform(item => ProcessorA(item, host.Config.DelayMs));
        var stage2 = stage1.Transform(item => ProcessorB(item, host.Config.DelayMs));
        stage2.Output();
    }

    // Mark processor for remote execution with isRemote: true
    // The version parameter is used for cache invalidation and processor identification
    [Processor(isRemote: true, version: 1)]
    private static async Task<List<Item>> ProcessorA(Item item, int delayMs)
    {
        await Task.Delay(delayMs);
        var content = await item.GetContentAsString();
        content += "->A";
        return [await Item.Create(item, $"{item.Name}.a", content, MimeTypes.TextPlain)];
    }

    [Processor(isRemote: true, version: 1)]
    private static async Task<List<Item>> ProcessorB(Item item, int delayMs)
    {
        await Task.Delay(delayMs);
        var content = await item.GetContentAsString();
        content += "->B";
        return [await Item.Create(item, $"{item.Name}.b", content, MimeTypes.TextPlain)];
    }
}
```

**Important limitations for remote processors:**
- `CancellationToken` parameters are **not supported** in remote processors. Remove any `CancellationToken` parameters from methods marked with `isRemote: true`.
- All processor parameters must be JSON-serializable.

### Host Mode

Enable host mode with `PipelineRunner.Config.EnableRemoteHost` or `ikon pipeline run --remote-host`. The host:

- Reads input items and orchestrates the pipeline graph
- Dispatches remote processor calls to clients via the message bus
- Maintains the shared state and content cache
- Collects results and produces output items

### Client Mode

Enable client mode with `PipelineRunner.Config.EnableRemoteClient` or `ikon pipeline run --remote-client`. The client:

- Connects to the message bus and listens for processor calls
- Executes processors locally using content from the shared cache
- Returns results to the host via the message bus
- Runs indefinitely until terminated

### Startup Order

**Critical: Clients must start before the host.** RabbitMQ discards messages if no consumer is bound to a queue. If you start the host first, processor calls may be lost before clients connect.

Recommended startup sequence:
1. Ensure RabbitMQ is running
2. Start all client processes
3. Wait a few seconds for clients to bind to queues
4. Start the host process

### Processor Name Format

Remote processors are identified by their fully qualified name in the format:

```
{Namespace}.{ClassName}.{MethodName}.{Version}
```

For example, the `ProcessorA` method above would have the name:
```
MyNamespace.DistributedPipeline.ProcessorA.1
```

This name format is used when configuring the client processor whitelist.

### Configuration Options

| Option | Description |
|--------|-------------|
| `RabbitMQConnectionString` / `--remote-rabbitmq` | RabbitMQ connection string. Format: `host=localhost;port=5672;username=guest;password=guest`. Required for distributed execution. |
| `MaxRemoteRequestParallelism` / `--max-remote-request-parallelism` | Maximum concurrent remote operations the host processes. Defaults to `ProcessorCount * 100`. |
| `RemoteClientProcessorWhiteList` / `--remote-client-processor-whitelist` | Comma-separated list of processor names this client handles. If omitted, the client handles all remote processors. |
| `CachePath` / `--cache` | Path to the shared content cache directory. Must be the same for host and all clients. |

### Example: Single Client Handling All Processors

```bash
# Terminal 1: Start the client first
ikon pipeline run \
    --dll-path ./bin/Release/MyPipeline.dll \
    --type-name MyNamespace.DistributedPipeline \
    --cache ./shared-cache \
    --remote-client \
    --remote-rabbitmq "host=localhost;port=5672;username=guest;password=guest"

# Terminal 2: Start the host after client is ready (wait a few seconds)
ikon pipeline run \
    --dll-path ./bin/Release/MyPipeline.dll \
    --type-name MyNamespace.DistributedPipeline \
    --input ./data/ \
    --output ./output/ \
    --cache ./shared-cache \
    --remote-host \
    --remote-rabbitmq "host=localhost;port=5672;username=guest;password=guest"
```

### Example: Specialized Clients

Distribute different processors to different clients using the whitelist:

```bash
# Terminal 1: Client handling only ProcessorA
ikon pipeline run \
    --dll-path ./bin/Release/MyPipeline.dll \
    --type-name MyNamespace.DistributedPipeline \
    --cache ./shared-cache \
    --remote-client \
    --remote-rabbitmq "host=localhost;port=5672;username=guest;password=guest" \
    --remote-client-processor-whitelist "MyNamespace.DistributedPipeline.ProcessorA.1"

# Terminal 2: Client handling only ProcessorB
ikon pipeline run \
    --dll-path ./bin/Release/MyPipeline.dll \
    --type-name MyNamespace.DistributedPipeline \
    --cache ./shared-cache \
    --remote-client \
    --remote-rabbitmq "host=localhost;port=5672;username=guest;password=guest" \
    --remote-client-processor-whitelist "MyNamespace.DistributedPipeline.ProcessorB.1"

# Terminal 3: Start the host after clients are ready
ikon pipeline run \
    --dll-path ./bin/Release/MyPipeline.dll \
    --type-name MyNamespace.DistributedPipeline \
    --input ./data/ \
    --output ./output/ \
    --cache ./shared-cache \
    --remote-host \
    --remote-rabbitmq "host=localhost;port=5672;username=guest;password=guest"
```

### Example: Multiple Clients for Load Distribution

Run multiple clients handling the same processors to distribute load:

```bash
# Start multiple clients (each in separate terminal)
# All clients handle all processors - work is distributed via RabbitMQ
ikon pipeline run \
    --dll-path ./bin/Release/MyPipeline.dll \
    --type-name MyNamespace.DistributedPipeline \
    --cache ./shared-cache \
    --remote-client \
    --remote-rabbitmq "host=localhost;port=5672;username=guest;password=guest"
```

### Programmatic Usage

Use `PipelineRunner.RunRemote` to orchestrate distributed execution from code:

```csharp
var config = new PipelineRunner.Config
{
    TypeName = typeof(DistributedPipeline).FullName!,
    DllPath = "./bin/Release/MyPipeline.dll",
    EnableRemoteHost = true,
    EnableRemoteClient = true, // Can run host and client in same process
    RabbitMQConnectionString = "host=localhost;port=5672;username=guest;password=guest",
    CachePath = "./shared-cache"
};

await PipelineRunner.RunRemote(config, status =>
{
    Console.WriteLine($"Processed: {status.ProcessedItemCount}, Failures: {status.ProcessFailureCount}");
}, cancellationToken);
```

When remote modes are active, `PipelineRunner.RunRemote` orchestrates the host/client lifecycle, forwards live status updates, and honors cancellation tokens for cooperative shutdown.


---

# Ikon.Pipelines.Public Public API

namespace Ikon.Pipelines.Public.Examples
  static class ExampleProcessors
    static Task<List<Item>> Run(Item inputItem)
    static Task<List<Item>> Run2(Item inputItem, CancellationToken cancellationToken)
    static Task<List<Item>> Run3(List<Item> inputItems)
    static Task<List<Item>> Run4(List<Item> inputItems, CancellationToken cancellationToken)
  // Example pipeline. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<FullExamplePipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class FullExamplePipeline
    ctor(IPipelineHost<FullExamplePipeline.Config> host)
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
  class FullExamplePipeline.Config
    ctor()
    int TestValue1 { get; set; }
    string TestValue2 { get; set; }
  class FullExamplePipeline.Input
    ctor()
    int TestValue1 { get; set; }
    string TestValue2 { get; set; }
  class FullExamplePipeline.Result
    ctor()
    int TestValue1 { get; set; }
    string TestValue2 { get; set; }
  // Example pipeline. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<MinimalExamplePipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class MinimalExamplePipeline
    ctor()
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)

namespace Ikon.Pipelines.Public.Processors.Json
  static class MergeJsonProcessor
    // Deserializes each input item's content and emits a single JSON-array item, named exactly itemName (the full name, not a suffix), whose elements are the deserialized items in input order. An item whose content fails to deserialize throws InvalidOperationException. Returns an empty result when there are no input items.
    static Task<List<Item>> Run(List<Item> items, string itemName)
  static class SplitJsonArrayProcessor
    // Splits a JSON array into one output item per element. When the content is a top-level array, its elements are emitted directly; when the content is an object, the first array-valued property in enumeration order is used. Throws InvalidOperationException when no array is found.
    static Task<List<Item>> Run(Item input)
  static class TrimJsonProcessor
    // Beyond removing any fields named in fieldsToRemove, this always performs a recursive prune of the entire document — independent of fieldsToRemove, and applied even when it is null or empty. Every null value, every empty or whitespace-only string, and every object or array that ends up empty is dropped, recursively, so a container emptied by pruning is itself pruned from its parent. If the whole document prunes away to nothing, the output is the literal null.
    static Task<List<Item>> Run(Item input, List<string>? fieldsToRemove = null)

namespace Ikon.Pipelines.Public.Processors.OCR
  static class OCRProcessor
    static Task<List<Item>> Run(Item input, OCRProcessor.Config config, CancellationToken cancellationToken)
  class OCRProcessor.Config
    ctor()
    OCRModel OCRModel { get; set; }

namespace Ikon.Pipelines.Public.Processors.Pdf
  static class ExtractPdfProcessor
    static Task<List<Item>> Run(Item input, ExtractPdfProcessor.Config config, CancellationToken cancellationToken)
  class ExtractPdfProcessor.Config
    ctor()
    // Longest side, in pixels, of each rendered page image. Default 1024; must be greater than zero.
    int MaxPageImageDimension { get; set; }
  interface IPdfDocument : IDisposable
    int PageCount { get; }
    IPdfPage GetPage(int index)
  interface IPdfPage : IDisposable
    double Height { get; }
    int Index { get; }
    double Width { get; }
    void CreateCopy(Stream output)
    // Renders the page scaled so its longest side is at most maxDimension pixels, preserving aspect ratio. Returns the page as a row-major RGBA byte buffer (4 bytes/pixel) and the resulting pixel dimensions.
    (byte[] rgba, int width, int height) GetPixels(int maxDimension)
    // Renders the page at the exact width by height pixel size. Returns the page as a row-major RGBA byte buffer (4 bytes/pixel) and the resulting pixel dimensions.
    (byte[] rgba, int width, int height) GetPixels(int width, int height, bool hasAlpha)
    string GetText()
  static class PdfDocument
    static IPdfDocument Load(byte[] bytes, string? password = null)

namespace Ikon.Pipelines.Public.UniversalRag
  // Universal RAG ingestion pipeline. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<UniversalRagPipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class UniversalRagPipeline
    ctor(IPipelineHost<UniversalRagPipeline.Config> host)
    // Items are routed by MimeType: PDFs, plain text, and JSON are each handled on their own document path. An item whose original name starts with web_scraper_config (JSON), web_scraper_sitemap (XML), or web_scraper_sitelist (plain text) instead triggers the web-scraping path. All other PDF, text, and JSON items are treated as documents. An item that matches none of these — neither a document type nor a web-scraper trigger — produces no output and is silently dropped.
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
  class UniversalRagPipeline.Config
    ctor()
    AnalyzePdfDocumentProcessor.Config AnalyzeDocumentType { get; set; }
    // Number of text chunks embedded per batch request. Default 200.
    int EmbeddingBatchSize { get; set; }
    ExtractPdfProcessor.Config ExtractPdf { get; set; }
    ExtractFullTextAndSectionsProcessor.Config ExtractSections { get; set; }
    ExtractTextProcessor.Config ExtractText { get; set; }
    FormatWebPageProcessor.Config FormatWebPage { get; set; }
    GenerateEmbeddingsProcessor.Config GenerateEmbeddings { get; set; }
    GenerateSummaryProcessor.Config GenerateSummary { get; set; }
    // Maximum concurrent LLM requests during analysis. Default 10.
    int MaxLLMParallelism { get; set; }

namespace Ikon.Pipelines.Public.UniversalRag.Processors
  static class AnalyzePdfDocumentProcessor
    static Task<List<Item>> Run(List<Item> inputItems, AnalyzePdfDocumentProcessor.Config config, CancellationToken cancellationToken)
  class AnalyzePdfDocumentProcessor.Config
    ctor()
    LLMModel LLMModel { get; set; }
    // Number of leading pages sent to the LLM for document-level analysis (title, type). Default 3.
    int PagesToAnalyze { get; set; }
  static class CombineEmbeddingsProcessor
    static Task<List<Item>> Run(List<Item> inputItems, CancellationToken cancellationToken)
  static class ExtractFullTextAndSectionsProcessor
    static Task<List<Item>> Run(Item inputItem, ExtractFullTextAndSectionsProcessor.Config config, CancellationToken cancellationToken)
    static Task<List<Item>> Run(List<Item> inputItems, ExtractFullTextAndSectionsProcessor.Config config, CancellationToken cancellationToken)
  class ExtractFullTextAndSectionsProcessor.Config
    ctor()
    string ExtraCommand { get; set; }
    string ExtraContext { get; set; }
    bool ExtractFullText { get; set; }
    bool ExtractSections { get; set; }
    LLMModel LLMModel { get; set; }
  static class ExtractTextProcessor
    static Task<List<Item>> Run(List<Item> inputItems, ExtractTextProcessor.Config config, CancellationToken cancellationToken)
  class ExtractTextProcessor.Config
    ctor()
    LLMModel LLMModel { get; set; }
  static class FormatWebPageProcessor
    static Task<List<Item>> Run(Item inputItem, FormatWebPageProcessor.Config config, CancellationToken cancellationToken)
  class FormatWebPageProcessor.Config
    ctor()
    string ExtraCommand { get; set; }
    string ExtraContext { get; set; }
    LLMModel LLMModel { get; set; }
  static class FullTextPassthroughProcessor
    static Task<List<Item>> Run(Item inputItem, CancellationToken cancellationToken)
  static class GenerateEmbeddingsProcessor
    static Task<List<Item>> Run(List<Item> inputItems, GenerateEmbeddingsProcessor.Config config, CancellationToken cancellationToken)
  class GenerateEmbeddingsProcessor.Config
    ctor()
    EmbeddingModel EmbeddingModel { get; set; }
  static class GenerateRouterProcessor
    static Task<List<Item>> Run(List<Item> inputItems, CancellationToken cancellationToken)
  static class GenerateSummaryProcessor
    static Task<List<Item>> Run(Item inputItem, GenerateSummaryProcessor.Config config, CancellationToken cancellationToken)
  class GenerateSummaryProcessor.Config
    ctor()
    LLMModel LLMModel { get; set; }
  static class WebScraperConfigProcessor
    static Task<List<Item>> Run(List<Item> inputItems, CancellationToken cancellationToken)
  static class WebScraperProcessor
    static Task<List<Item>> Run(Item inputItem, CancellationToken cancellationToken)

namespace Ikon.Pipelines.Public.UniversalRag.Shaders
  class AnalyzePdfDocument
    ctor()
    static Task<AnalyzePdfDocument.Result> Run(LLMModel llmModel, List<Item> pageImageItems, CancellationToken cancellationToken = default)
  enum AnalyzePdfDocument.DocumentType
    Document
    Presentation
  class AnalyzePdfDocument.Result
    ctor()
    string Title { get; set; }
    AnalyzePdfDocument.DocumentType Type { get; set; }
  class ExtractDocumentPageText
    ctor()
    static Task<string> Run(LLMModel llmModel, Item rawTextItem, Item imageItem, CancellationToken cancellationToken = default)
  class ExtractPresentationPageText
    ctor()
    static Task<string> Run(LLMModel llmModel, Item rawTextItem, Item imageItem, CancellationToken cancellationToken = default)
  class ExtractSections
    ctor()
    static Task<ExtractSections.Result> Run(LLMModel llmModel, string documentTextWithLineNumbers, string extraContext, string extraCommand, CancellationToken cancellationToken = default)
  class ExtractSections.Result
    ctor()
    List<ExtractSections.Section> Sections { get; set; }
  class ExtractSections.Section
    ctor()
    int EndLine { get; set; }
    int StartLine { get; set; }
    List<string> TitleHierarchy { get; set; }
  class FormatWebPage
    ctor()
    static Task<FormatWebPage.Result> Run(LLMModel llmModel, string url, string title, string content, string extraContext, string extraCommand, CancellationToken cancellationToken = default)
  class FormatWebPage.Result
    ctor()
    string Content { get; set; }
    bool HasContent { get; set; }
  class GenerateSummary
    ctor()
    static Task<string> Run(LLMModel llmModel, string content, CancellationToken cancellationToken = default)

namespace Ikon.Pipelines.Public.UniversalRag.Utils
  static class TextUtils
    static string TrimMarkdownBackticks(string input)

namespace Ikon.Pipelines.Public.VideoImageSafety
  enum CollageSelectionMode
    SceneThreshold
    FixedInterval
  // Safety analysis for images. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<ImageSafetyPipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class ImageSafetyPipeline
    ctor(IPipelineHost<ImageSafetyPipeline.Config> host)
    // Each input Item's content must deserialize to a ImageSource (the pipeline's inputSchema) — post JSON matching ImageSource as the item content; non-conforming input causes a per-item processing failure (non-JSON content fails to deserialize, and a JSON object missing the required Url field fails downloading downstream), not a silent empty result.
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
  class ImageSafetyPipeline.Config
    ctor()
    // LLM model for analyzing image content and safety
    LLMModel AnalysisModel { get; set; }
    // Maximum number of parallel analysis requests
    int MaxAnalysisParallelism { get; set; }
    // Maximum number of parallel moderation requests
    int MaxModerationParallelism { get; set; }
    // Model used for content moderation
    ClassificationModel ModerationModel { get; set; }
  class ImageSafetyResult
    ctor()
    // Primary content category classification
    string ContentCategory { get; set; }
    // Extracted factual information from the image
    string Facts { get; set; }
    // Recommended audience for this content
    string IdealAudience { get; set; }
    // Description of the image content
    string ImageDescription { get; set; }
    // Interpreted meaning and context of the image
    string ImageMeaning { get; set; }
    // Whether the image passed safety analysis
    bool IsSafe { get; set; }
    // The main safety concern identified
    string PrimaryRisk { get; set; }
    // Suggested actions based on the analysis
    string RecommendedActions { get; set; }
    // Human-readable summary of the safety evaluation
    string SafetySummary { get; set; }
    // The original image source that was analyzed
    ImageSource Source { get; set; }
    // List of safety categories that were triggered
    string[] TriggeredCategories { get; set; }
  class ImageSource
    ctor()
    // Optional description providing context about the image content
    string Description { get; set; }
    // Display name for the image
    string Name { get; set; }
    // URL of the image to analyze
    string Url { get; set; }
  // Safety analysis for video clips. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<VideoSafetyPipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class VideoSafetyPipeline
    ctor(IPipelineHost<VideoSafetyPipeline.Config> host)
    // Each input Item's content must deserialize to a VideoSource (the pipeline's inputSchema) — post JSON matching VideoSource as the item content; non-conforming input causes a per-item processing failure (non-JSON content fails to deserialize, and a JSON object missing the required Url field fails downloading downstream), not a silent empty result. ffmpeg/ffprobe must be on PATH for extraction.
    // inputItems: Branch containing video source items to analyze.
    // cancellationToken: Token to cancel the operation.
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
  class VideoSafetyPipeline.Config
    ctor()
    // LLM model for analyzing video frames
    LLMModel AnalysisModel { get; set; }
    // Fixed number of collages to generate (0 = auto-calculate based on duration)
    int CollageCount { get; set; }
    // Width in pixels for extracted frames
    int CollageFrameWidth { get; set; }
    // Time interval in minutes between collages when auto-calculating. Only consulted when CollageCount is 0; ignored when CollageCount > 0.
    double CollageIntervalMinutes { get; set; }
    // Frame selection strategy: SceneThreshold uses scene detection, FixedInterval uses even distribution
    CollageSelectionMode CollageSelection { get; set; }
    // LLM model for final safety evaluation
    LLMModel EvaluationModel { get; set; }
    // Target frame extraction rate in frames per second. Only used when CollageSelection is FixedInterval; ignored under the default SceneThreshold mode.
    double FramesPerSecond { get; set; }
    // Maximum number of parallel frame analysis requests
    int MaxAnalysisParallelism { get; set; }
    // Maximum number of frames to extract per collage
    int MaxFrames { get; set; }
    // Maximum number of parallel moderation requests
    int MaxModerationParallelism { get; set; }
    // Model used for content moderation
    ClassificationModel ModerationModel { get; set; }
    // Sensitivity threshold for scene change detection (0-1). Only used when CollageSelection is SceneThreshold (the default).
    double SceneChangeThreshold { get; set; }
    // Number of columns in the frame collage grid
    int TileColumns { get; set; }
    // Number of rows in the frame collage grid
    int TileRows { get; set; }
    // Language code for speech recognition (e.g., en-US)
    string TranscriptionLanguage { get; set; }
    // Speech recognition model to use for transcription
    SpeechRecognizerModel TranscriptionModel { get; set; }
    // Temperature for transcription (0 = deterministic, higher = more varied)
    float TranscriptionTemperature { get; set; }
  class VideoSafetyResult
    ctor()
    // Primary content category classification
    string ContentCategory { get; set; }
    // Extracted factual information from the video
    string Facts { get; set; }
    // Recommended audience for this content
    string IdealAudience { get; set; }
    // Whether the video passed safety analysis
    bool IsSafe { get; set; }
    // Interpreted meaning and context of the video
    string Meaning { get; set; }
    // The main safety concern identified
    string PrimaryRisk { get; set; }
    // Suggested actions based on the analysis
    string RecommendedActions { get; set; }
    // Description of the most representative frame
    string RepresentativeDescription { get; set; }
    // Human-readable summary of the safety evaluation
    string SafetySummary { get; set; }
    // The original video source that was analyzed
    VideoSource Source { get; set; }
    // Audio transcript from the video
    string Transcript { get; set; }
    // List of safety categories that were triggered
    string[] TriggeredCategories { get; set; }
  class VideoSource
    ctor()
    // Optional description providing context about the video content
    string Description { get; set; }
    // Display name for the video
    string Name { get; set; }
    // URL of the video to analyze
    string Url { get; set; }

namespace Ikon.Pipelines.Public.VideoImageSafety.Shaders
  static class AnalyzeImageSafety
    static Task<AnalyzeImageSafety.Result> RunAsync(LLMModel llmModel, byte[] image, string imageMimeType, string sourceName, string sourceDescription, CancellationToken cancellationToken = default)
  class AnalyzeImageSafety.Result
    ctor()
    string ContentCategory { get; set; }
    string Facts { get; set; }
    string IdealAudience { get; set; }
    string ImageDescription { get; set; }
    string ImageMeaning { get; set; }
    bool IsSafe { get; set; }
    string PrimaryRisk { get; set; }
    string RecommendedActions { get; set; }
    string SafetySummary { get; set; }
    string[] TriggeredCategories { get; set; }
  static class AnalyzeVideoFrames
    static Task<AnalyzeVideoFrames.Result> RunAsync(LLMModel llmModel, byte[] collageImage, string collageImageMimeType, CancellationToken cancellationToken = default)
  class AnalyzeVideoFrames.Result
    ctor()
    string Facts { get; set; }
    string FramesDescription { get; set; }
    string VideoMeaning { get; set; }
  static class EvaluateVideoSafety
    static Task<EvaluateVideoSafety.Result> RunAsync(LLMModel llmModel, string sourceName, string sourceDescription, string transcript, AnalyzeVideoFrames.Result combinedAnalysis, CancellationToken cancellationToken = default)
  class EvaluateVideoSafety.Result
    ctor()
    string ContentCategory { get; set; }
    string IdealAudience { get; set; }
    bool IsSafe { get; set; }
    string PrimaryRisk { get; set; }
    string RecommendedActions { get; set; }
    string SafetySummary { get; set; }
    string[] TriggeredCategories { get; set; }
