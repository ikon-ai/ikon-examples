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
