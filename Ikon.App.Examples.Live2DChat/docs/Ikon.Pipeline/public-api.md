# Ikon.Pipeline Public API

namespace Ikon.Pipeline
  // Delegate signature used for asynchronous event notifications.
  delegate Pipeline<T>.AsyncEventHandler<T, TEventArgs> where T : IItem<T>
    Task AsyncEventHandler<T, TEventArgs>(object sender, TEventArgs e)
  // Represents a branch in the pipeline for processing items.
  sealed class Pipeline<T>.Branch<T> where T : IItem<T>
    ctor(Pipeline<T> outer, ISourceBlock<T> sourceBlock, IDataflowBlock dataflowBlock)
    // Filters items in the branch using an asynchronous predicate.
    Pipeline<T>.Branch<T> Filter(Func<T, Task<bool>> predicate, int? maxParallelism = null)
    // Filters the branch to items that can be treated as TObject.
    Pipeline<T>.Branch<T> Filter<TObject>(int? maxParallelism = null)
    // Executes an asynchronous action for every item in the branch. Note: this ends the branch. Prefer using any of the Transform methods for further processing.
    void ForEach(Func<T, Task> func, int? maxParallelism = null)
    Pipeline<T>.Branch<T> Merge(params Pipeline<T>.Branch<T>[] branches)
    // Outputs all the items from this branch. Note: this ends the branch.
    void Output(int? maxParallelism = null)
    // Sends a single item directly into this branch.
    void Post(T item)
    // Sends a collection of items into this branch.
    void Post(List<T> items)
    // Streams items from an asynchronous sequence into the branch.
    void Post(IAsyncEnumerable<T> stream)
    // Applies an asynchronous transformation to each item. Note: The expression will be analyzed and all variable values will be used to calculate the processor ID (which will invalidate caches if changed).
    Pipeline<T>.Branch<T> Transform(Expression<Func<T, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Batches items and processes each batch with an asynchronous function. Note: The expression will be analyzed and all variable values will be used to calculate the processor ID (which will invalidate caches if changed).
    Pipeline<T>.Branch<T> TransformBatch(Expression<Func<List<T>, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, int? maxBatchSize = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Collects items into batches and transforms each batch with an asynchronous function. Note: Please prefer using the Branch.TransformBatch method that takes in an expression for better cache and remote call handling.
    Pipeline<T>.Branch<T> TransformBatchLambda(Func<List<T>, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, int? maxBatchSize = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Groups items by a key and processes each group with an asynchronous function. Note: The expression will be analyzed and all variable values will be used to calculate the processor ID (which will invalidate caches if changed).
    Pipeline<T>.Branch<T> TransformGroup(Expression<Func<T, Task<string>>> groupKeySelectorExpr, Expression<Func<List<T>, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Groups items by a key and processes each group with an asynchronous function. Note: Please prefer using the Branch.TransformGroup method that takes in an expression for better cache and remote call handling.
    Pipeline<T>.Branch<T> TransformGroupLambda(Func<T, Task<string>> groupKeyFunc, Func<List<T>, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Applies an asynchronous transformation to each item. The function can return zero or more output items per input item. Note: Please prefer using the Branch.Transform method that takes in an expression for better cache and remote call handling.
    Pipeline<T>.Branch<T> TransformLambda(Func<T, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Transforms each item into an asynchronous stream of items. Note: The expression will be analyzed and all variable values will be used to calculate the processor ID (which will invalidate caches if changed).
    Pipeline<T>.Branch<T> TransformStream(Expression<Func<T, IAsyncEnumerable<T>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Applies a stream-to-stream transformation. Note: The expression will be analyzed and all variable values will be used to calculate the processor ID (which will invalidate caches if changed).
    Pipeline<T>.Branch<T> TransformStream(Expression<Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Transforms each item into an asynchronous stream of items. Note: Please prefer using the Branch.TransformStream method that takes in an expression for better cache and remote call handling.
    Pipeline<T>.Branch<T> TransformStreamLambda(Func<T, IAsyncEnumerable<T>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Applies a stream-to-stream asynchronous transformation. Note: Please prefer using the Branch.TransformStream method that takes in an expression for better cache and remote call handling.
    Pipeline<T>.Branch<T> TransformStreamLambda(Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
  // Configuration settings for the PipelineRunner.
  sealed class PipelineRunner.Config
    ctor()
    // Whether to request all API keys from the backend (admin only).
    bool AllApiKeys { get; set; }
    // Base directory for persistent cache and state data.
    string? CachePath { get; set; }
    // Indicates whether existing cache data should be cleared before execution.
    bool ClearCache { get; set; }
    // Optional path to a JSON configuration file.
    string? ConfigPath { get; set; }
    // Content cache implementation backing item storage.
    CacheType ContentCacheType { get; set; }
    // Disables caching of processor outputs unless explicitly re-enabled.
    bool DefaultDisableProcessCache { get; set; }
    // Optional default degree of parallelism for processors when not overridden.
    int? DefaultMaxProcessParallelism { get; set; }
    // Optional default retry count for processors when not overridden.
    int? DefaultMaxRetries { get; set; }
    // Type names of exceptions that should be treated as retryable by default.
    List<string>? DefaultRetryableExceptionTypes { get; set; }
    // Disables caching of input items.
    bool DisableInputCache { get; set; }
    // Disables writing metadata files for output items.
    bool DisableMetadataOutput { get; set; }
    // Disables caching of output items.
    bool DisableOutputCache { get; set; }
    // Optional path to the assembly containing the pipeline type when loading dynamically.
    string? DllPath { get; set; }
    // Enables remote client functionality that offloads processors to a host.
    bool EnableRemoteClient { get; set; }
    // Enables remote host functionality that serves processor calls to clients.
    bool EnableRemoteHost { get; set; }
    // Enables SSE streaming of output content.
    bool EnableSseOutput { get; set; }
    // Enumerates ZIP archives for input content when enabled.
    bool EnumerateZips { get; set; }
    // Optional file path to persist the final status JSON payload.
    string? FinalStatusPath { get; set; }
    // Ikon backend access token propagated to the hosted pipeline.
    string IkonBackendToken { get; set; }
    // Ikon backend URL propagated to the hosted pipeline.
    string IkonBackendUrl { get; set; }
    // Locations that should be scanned for input content.
    List<string>? InputPaths { get; set; }
    // Indicates that the run is executed in a test context with minimal side effects.
    bool IsTestRun { get; set; }
    // Keeps the runner active after initial processing to watch for new inputs.
    bool KeepRunning { get; set; }
    // Log filter level that should be applied when running in an external context.
    int LogFilter { get; set; }
    // Optional maximum degree of parallelism for reading inputs.
    int? MaxInputReadParallelism { get; set; }
    // Optional limit for concurrent remote requests.
    int? MaxRemoteRequestParallelism { get; set; }
    // Emits the final status snapshot after completion.
    bool OutputFinalStatus { get; set; }
    // Destination paths for persisted output content.
    List<string>? OutputPaths { get; set; }
    // Maximum number of processor failures tolerated before aborting the run.
    int ProcessFailureThreshold { get; set; }
    // Identifier used to correlate logs and status updates for this run.
    string? ProcessingId { get; set; }
    // RabbitMQ connection string used when remote execution is enabled.
    string? RabbitMQConnectionString { get; set; }
    // Indicates whether directory inputs should be enumerated recursively.
    bool RecursiveInput { get; set; }
    // Optional whitelist restricting which processors a remote client may execute.
    List<string>? RemoteClientProcessorWhiteList { get; set; }
    // Interval, in seconds, between input scans when Config.KeepRunning is enabled.
    int ScanInterval { get; set; }
    // State storage implementation used by the pipeline.
    StateType StateType { get; set; }
    // Interval, in seconds, between status update callbacks.
    int StatusUpdateInterval { get; set; }
    // Fully qualified name of the pipeline type to execute.
    string TypeName { get; set; }
    // Optional pipeline configuration instance supplied by the caller.
    object? UserConfigInstance { get; set; }
    // Optional user-provided pipeline instance that overrides automatic construction.
    object? UserPipelineInstance { get; set; }
  // Empty configuration sentinel for pipelines that need a host (for IPipelineHost<TConfig>.Secrets, IPipelineHost<TConfig>.OrganisationId, IPipelineHost<TConfig>.SpaceId) but no user-defined configuration.
  sealed class EmptyPipelineConfig
    ctor()
  // Attribute for exposing a pre-existing pipeline from a framework assembly. Can be placed on the [App] decorated class or on empty marker classes.
  sealed class ExposePipelineAttribute : Attribute
    ctor(Type pipelineType, string? name = null, PipelineExecutionMode executionMode = None, string? schedule = null)
    // Execution mode override for the exposed pipeline. If set to a value other than None, overrides the execution mode defined on the original [Pipeline] attribute.
    PipelineExecutionMode ExecutionMode { get; }
    // Optional name override for the pipeline endpoint URL. If not set, the original pipeline name will be used.
    string? Name { get; }
    // The type of the pipeline class to expose. Must be decorated with [Pipeline].
    Type PipelineType { get; }
    // Schedule override for the exposed pipeline. If set, overrides the schedule defined on the original [Pipeline] attribute.
    string? Schedule { get; }
  // Extension methods for registering pipelines with the FunctionRegistry.
  static class FunctionRegistryExtensions
    // Registers a pipeline as a callable function in the registry.
    static void RegisterPipeline<TPipeline>(this FunctionRegistry registry, string functionName, string? description = null, object? configInstance = null) where TPipeline : class
  // Provides access to the configuration and platform context (secrets, space, organisation) available while a pipeline runs.
  interface IPipelineHost<TConfig>
    // Configuration associated with the host.
    TConfig Config { get; }
    // Id of the organisation that owns the space this pipeline is running in. Empty when the pipeline runs without a space-scoped backend token.
    string OrganisationId { get; }
    // Secrets (API keys, tokens, passwords) configured for the space this pipeline is running in. Fetched from the Ikon backend once when the runner constructs the pipeline; rotating a secret with ikon app secret set while the pipeline is running only takes effect after a re-run.
    Secrets Secrets { get; }
    // Id of the space this pipeline is running in. Empty when the pipeline runs without a space-scoped backend token.
    string SpaceId { get; }
  // Helper class for managing local files used as input or output in the pipeline. Normally one should just use the Item.GetContent* methods. This is for special cases where a path to a file on a local filesystem is needed. Use the Item.GetLocalFile method to copy the content of an Item to a local file and get a path to it. Remember to use 'using' or dispose the LocalFile after use to clean up temporary files. One can also create a LocalFile instance which will give you a temporary file path to write to (no file is created until you write to it). One can then create a new item with this LocalFile instance. Remember to dispose the LocalFile after use to clean up the temporary file. Mimetype is used to determine the file extension for temporary files and also when creating a new Item from a LocalFile.
  sealed class LocalFile : IDisposable
    // Creates a LocalFile instance. If existingFilePath is provided, it will use that file and not delete it on dispose.
    ctor(string mimeType, string? existingFilePath = null)
    // MIME type associated with the file contents.
    string MimeType { get; }
    // Absolute path to the underlying file on disk.
    string Path { get; }
    // Disposes the LocalFile instance and deletes the temporary file if it was created by this instance.
    void Dispose()
  // Class attribute for defining a pipeline. When running a pipeline with the pipeline runner, the target class must be decorated with this attribute.
  sealed class PipelineAttribute : Attribute
    ctor(string description = "", int version = 1, string guid = "", Type? inputSchema = null, Type? resultSchema = null, string? name = null, int maxInputItems = 0, PipelineExecutionMode executionMode = None, string? schedule = null)
    // Optional description of the pipeline.
    string Description { get; }
    // Execution mode for the pipeline. Determines how the pipeline is triggered.
    PipelineExecutionMode ExecutionMode { get; }
    // Optional unique identifier (GUID) for the pipeline.
    string Guid { get; }
    // Optional type of POD class defining the input schema for the pipeline. Input schema can be used to model the expected format of input data. This is for external use and not enforced by the pipeline runtime.
    Type? InputSchema { get; }
    // Maximum number of input items allowed for this pipeline. If set to 0 (default), there is no limit. When exceeded, the pipeline will throw a PipelineException.
    int MaxInputItems { get; }
    // Optional name override for the pipeline. If not set, the class name will be used. This name is used to generate the pipeline URL endpoint (converted to kebab-case).
    string? Name { get; }
    // Optional type of POD class defining the result schema for the pipeline. Result schema can be used to model the expected format of output data. This is for external use and not enforced by the pipeline runtime.
    Type? ResultSchema { get; }
    // Cron schedule expression for the pipeline. Only used when ExecutionMode is Scheduled.
    string? Schedule { get; }
    // Version of the pipeline.
    int Version { get; }
  // Represents errors raised by the pipeline infrastructure.
  sealed class PipelineException : Exception
    ctor()
    ctor(string message)
    ctor(string message, Exception innerException)
  // Helper class for creating functions from pipeline types.
  static class PipelineFunction
    // Creates a function that runs the specified pipeline type.
    static Function Create<TPipeline>(string functionName, string? description = null, object? configInstance = null) where TPipeline : class
  // Transport-friendly representation of a pipeline item for remote function calls. Contains the actual content data (not just a cache reference).
  struct PipelineFunctionItem
    byte[] Content { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    List<string>? Tags { get; init; }
    static PipelineFunctionItem FromBytes(string name, byte[] content, string? mimeType = null, List<string>? tags = null)
    static PipelineFunctionItem FromString(string name, string content, string? mimeType = null, List<string>? tags = null)
    string GetContentAsString()
  // Helper class to run pipelines with various configurations.
  sealed class PipelineRunner : IDisposable
    // Creates a new instance of PipelineRunner. Only one runner should exist in a given async context.
    ctor()
    // Releases resources associated with the runner.
    void Dispose()
    // Initializes the runner with the given configuration. Note: Use the PipelineRunner.Initialize<TPipeline> method for a simplified initialization.
    Task Initialize(PipelineRunner.Config config)
    // Convenience method that initializes the runner using sensible defaults.
    Task Initialize<TPipeline>(TPipeline? userPipelineInstance = default, object? userConfigInstance = null, bool usePersistentCache = false, string? cachePath = null, bool keepRunning = false, string? outputPath = null, bool allApiKeys = false) where TPipeline : class
    // Simplified initialization used by unit tests.
    Task InitializeForUnitTest()
    // Runs the pipeline with optional in-memory input items and collects all output items into a list. Will return only after the pipeline has completed.
    Task<List<Item>> Run(List<Item>? items = null, CancellationToken cancellationToken = default)
    // Runs the pipeline with optional in-memory input items and returns an asynchronous stream of output items.
    IAsyncEnumerable<Item> RunAsEnumerable(List<Item>? items = null, CancellationToken cancellationToken = default)
    // This method is meant to be used when running the pipeline loaded in an external assembly load context.
    static Task RunInExternalAssembly(string configJson, Action<string> onStatusUpdate, CancellationToken cancellationToken)
    // Runs the pipeline in remote host and/or client mode.
    static Task RunRemote(PipelineRunner.Config config, Action<PipelineStatus> onStatusUpdate, CancellationToken cancellationToken = default)
    // Runs the pipeline with optional in-memory input items without collecting output items. Will return only after the pipeline has completed.
    Task RunWithoutCollecting(List<Item>? items = null, CancellationToken cancellationToken = default)
    // Raised whenever the pipeline produces an output item.
    event Pipeline<T>.AsyncEventHandler<Item, Item>? Output
    // Raised periodically with updated pipeline status metrics.
    event EventHandler<PipelineStatus>? StatusUpdate
  // Used to invoke the PipelineRunner from an external assembly. For internal use only.
  sealed class PipelineRunnerInvoker
    // Loads the pipeline bundle located next to the provided DLL and prepares an invoker.
    static Task<PipelineRunnerInvoker> Create(string pipelineDllPath)
    // Executes the pipeline inside its isolated context using the provided configuration.
    Task Run(string configJson, Action<string> onStatusUpdate, CancellationToken cancellationToken)
  // Represents the live status of a pipeline execution.
  sealed class PipelineStatus
    ctor()
    // Number of items detected as duplicates.
    int DuplicateItemCount { get; set; }
    // Time elapsed since PipelineStatus.StartTime.
    TimeSpan Duration { get; set; }
    // Count of error-level log entries emitted during the run.
    int ErrorLogCount { get; set; }
    // Indicates whether the pipeline has finished processing.
    bool HasCompleted { get; set; }
    // Indicates whether the pipeline ended due to an unhandled error.
    bool HasFaulted { get; set; }
    // Number of input items served from the input cache.
    int InputItemCacheHits { get; set; }
    // Number of input items that required fresh processing.
    int InputItemCacheMiss { get; }
    // Number of items observed by the inputs.
    int InputItemCount { get; set; }
    // Count of items marked invalid by validation.
    int InvalidItemCount { get; set; }
    // Number of output items reused from cache.
    int OutputItemCacheHits { get; set; }
    // Number of output items generated without cache hits.
    int OutputItemCacheMiss { get; }
    // Number of items produced by the outputs.
    int OutputItemCount { get; set; }
    // Number of processor executions that ultimately failed.
    int ProcessFailureCount { get; set; }
    // Number of processor retries triggered by failures.
    int ProcessRetryCount { get; set; }
    // Number of processed items obtained from the processor cache.
    int ProcessedItemCacheHits { get; set; }
    // Number of processed items that required re-computation.
    int ProcessedItemCacheMiss { get; }
    // Number of items processed by the pipeline.
    int ProcessedItemCount { get; set; }
    // Identifier of the currently running pipeline execution.
    string ProcessingId { get; set; }
    // Coordinated universal time when the pipeline started processing.
    DateTime StartTime { get; set; }
    // Aggregated usage metrics reported by the pipeline execution.
    Dictionary<string, double> Usages { get; set; }
    // Count of warning-level log entries emitted during the run.
    int WarningLogCount { get; set; }
    // Indicates whether the pipeline was cancelled.
    bool WasCancelled { get; set; }
  // Snapshot of the current processing statistics for a pipeline instance.
  sealed class Pipeline<T>.PipelineStatus<T> where T : IItem<T>
    ctor()
    // Total time elapsed since the pipeline started.
    TimeSpan Duration { get; set; }
    // Count of error log entries produced during execution.
    int ErrorLogCount { get; set; }
    // Number of processor executions that resulted in failure.
    int ProcessFailureCount { get; set; }
    // Number of times processors retried execution.
    int ProcessRetryCount { get; set; }
    // Number of items served from the processor cache.
    int ProcessedItemCacheHits { get; set; }
    // Number of items processed by the pipeline.
    int ProcessedItemCount { get; set; }
    // Count of warning log entries produced during execution.
    int WarningLogCount { get; set; }
  // Reactive asynchronous parallel data processing pipeline.
  sealed class Pipeline<T> where T : IItem<T>
    // Task that completes when all registered pipeline branches finish.
    Task Completion { get; }
    // Returns real-time status metrics for the pipeline.
    Pipeline<T>.PipelineStatus<T> Status { get; }
    // Marks the pipeline as complete and stops accepting input.
    void Complete()
    // Entry point to the pipeline where processors can be chained.
    Pipeline<T>.Branch<T> Inputs()
    // Posts an item into the pipeline.
    bool Post(T item)
    // Event that fires whenever a final output item is produced.
    event Pipeline<T>.AsyncEventHandler<T, T>? Output
  // Method attribute for defining a processor. When using processor methods in a pipeline, the method must be decorated with this attribute.
  sealed class ProcessorAttribute : Attribute
    ctor(string? id = null, int version = 1, int maxParallelism = 0, int maxRetries = 0, bool isRemote = false, bool skipCache = false, bool allowDuplicates = true, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    // Indicates whether duplicate items produced by the processor should be preserved. Defaults to true. Set to false to enable deduplication based on content hash and group ID.
    bool AllowDuplicates { get; set; }
    // Optional override identifier for the processor. If not provided, the ID will be calculated automatically.
    string? Id { get; set; }
    // Indicates whether the processor should be available for remote execution.
    bool IsRemote { get; set; }
    // Maximum degree of parallelism for the processor. If not given, the pipeline defaults will be used.
    int MaxParallelism { get; set; }
    // Maximum number of retries for the processor in case of failure. If not given, the pipeline defaults will be used.
    int MaxRetries { get; set; }
    // Exception types that are considered retryable for this processor. If not given, the pipeline defaults will be used.
    Type[] RetryableExceptionTypes { get; set; }
    // Indicates whether the pipeline should ignore cache for this processor.
    bool SkipCache { get; set; }
    // Tags associated with the processor for categorization or special handling.
    ProcessorTags[] Tags { get; set; }
    // Version of the processor. Used for calculating the processor hash for caching purposes.
    int Version { get; set; }
  // Describes additional capabilities or requirements for a processor.
  enum ProcessorTags
    Gpu
  // Represents a remote processor invocation request emitted by the pipeline.
  sealed class Pipeline<T>.RemoteCall<T> where T : IItem<T>
    ctor(Pipeline<T> pipeline, object? instance, string processorName, object?[] args)
    // Arguments supplied to the processor method.
    object?[] Args { get; }
    // Optional processor instance used to execute the call.
    object? Instance { get; }
    // Pipeline issuing the remote call.
    Pipeline<T> Pipeline { get; }
    // Name of the processor method being invoked remotely.
    string ProcessorName { get; }
  static class Pipeline<T>.RemoteCallHelper<T> where T : IItem<T>
    static object? BlockOnResult(Task<object?> task)
    static Task<object?> CallRemoteAsync(Pipeline<T> pipeline, object? instance, MethodInfo method, ProcessorAttribute attr, object[] args)
    static IAsyncEnumerable<TR?> CallRemoteStreamAsync<TR>(Pipeline<T> pipeline, object? instance, MethodInfo method, ProcessorAttribute attr, object[] args)
    static Task<RT?> CastTaskResult<RT>(Task<object?> task)
    static Task IgnoreTaskResult(Task<object?> task)

namespace Ikon.Pipeline.ContentCache
  enum CacheType
    InMemory
    FileSystem

namespace Ikon.Pipeline.Items
  // Minimal interface for items processed by the pipeline.
  interface IItem<T>
    // Determines whether the underlying content can be treated as the specified object type.
    Task<bool> IsObjectAsync<TObject>()
    // Returns a copy of the item with the supplied process identifier.
    T WithProcessId(Guid processId)
  // Represents an item processed by the pipeline. Items consist of their name, MIME type, and content. Items themselves do not store the content but carry a hash that points to the content in the content cache. Items are lightweight pointers to content stored in the content cache. They are immutable by design to avoid accidental modifications. If item properties need to be modified, use the With method to create a new item with the desired changes. When an item is created with content, its hash is automatically computed based on the content, MIME type, parent item hashes, and tags. If no MIME type is provided, it will be detected automatically. The content is then stored in the content cache. Item names do not need to contain any extension; the MIME type is used to determine the content type and the file extension when outputting.
  struct Item : IItem<Item>
    // Do not use. Always use the static Create or CreateInitial methods to create new items.
    ctor()
    // Optional user-defined group identifier for the item. This can be used to group related items together.
    string GroupId { get; init; }
    // Hash of the content the item points to in the content cache. This is automatically computed when creating the item.
    string Hash { get; init; }
    // For internal use.
    string? InitialPath { get; init; }
    // Returns true if this is an empty/default Item (not created via Create methods).
    bool IsDefault { get; }
    // Optional metadata associated with the item.
    ItemMetadata? Metadata { get; init; }
    // MIME type of the item's content. This is automatically detected when creating the item if not provided. This will determine the file extension when outputting.
    string MimeType { get; init; }
    // The name of the item. When outputting, this is used as the filename (with appropriate extension based on the mimetype).
    string Name { get; init; }
    // Hashes of the parent items of this item. This is automatically set when creating the item.
    IReadOnlyList<string> ParentHashes { get; init; }
    // Unique identifier for the processor that created this item. This is automatically set by the pipeline. This can be used to group items created by the same processor.
    Guid ProcessId { get; init; }
    // Optional user-defined tags associated with the item. Tags can be used filtering or grouping items.
    IReadOnlyList<string>? Tags { get; init; }
    // Creates a new Item inside the pipeline based on parent items. This should be used by processors during pipeline execution. To supply initial inputs before running the pipeline, use Item.CreateInitial instead.
    static Task<Item> Create(List<Item> parents, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create for a single parent item. Use Create within the pipeline and Item.CreateInitial for items created before Run.
    static Task<Item> Create(Item parent, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that accepts string content. Use Item.CreateInitial before running the pipeline and Create from processors.
    static Task<Item> Create(List<Item> parents, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that accepts string content. Use Item.CreateInitial before the pipeline runs and Create for in-pipeline items.
    static Task<Item> Create(Item parent, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that accepts binary content. Use Item.CreateInitial for pre-run items and this method within the pipeline.
    static Task<Item> Create(List<Item> parents, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that accepts binary content. Use Item.CreateInitial to create initial input items before the pipeline runs.
    static Task<Item> Create(Item parent, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that reads content from a LocalFile. Use this when a tool requires a local path. For creating inputs before the pipeline runs, call Item.CreateInitial.
    static Task<Item> Create(List<Item> parents, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that reads content from a LocalFile. Use when a tool needs a path on disk. For pre-run items use Item.CreateInitial.
    static Task<Item> Create(Item parent, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    // Serializes an object as JSON and creates a new item within the pipeline. Use this from processors. To generate initial items before running the pipeline, call Item.CreateInitialFromObject<T>.
    static Task<Item> CreateFromObject<T>(List<Item> parents, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // Overload of Item.CreateFromObject<T> for a single parent item. Use Item.CreateInitialFromObject<T> before running the pipeline and this overload inside the pipeline.
    static Task<Item> CreateFromObject<T>(Item parent, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // Creates an initial Item before the pipeline is started. Use this to generate input items outside of the pipeline after it has been initialized but before Run is called. Inside the pipeline, use Item.Create instead of CreateInitial.
    static Task<Item> CreateInitial(string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Convenience overload of CreateInitial that accepts the content as a string. Call Item.Create inside the pipeline; use CreateInitial only before Run.
    static Task<Item> CreateInitial(string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Convenience overload of CreateInitial that accepts the content as a byte array. Call Item.Create inside the pipeline; use CreateInitial beforehand.
    static Task<Item> CreateInitial(string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Serializes an object as JSON and creates an initial item from it. Use this before the pipeline runs; processors should call Item.CreateFromObject<T> during pipeline execution instead.
    static Task<Item> CreateInitialFromObject<T>(string name, T content, ItemMetadata? metadata = null, List<string>? tags = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // Retrieves the item's content as a byte array.
    Task<byte[]> GetContentAsBytes()
    // Deserializes the item's JSON content into an object.
    Task<TObject> GetContentAsObject<TObject>()
    // Retrieves the item's content as a Stream.
    Task<Stream> GetContentAsStream()
    // Retrieves the item's content as a UTF-8 string.
    Task<string> GetContentAsString()
    string GetGroupId()
    Task<string> GetGroupIdAsync()
    // Materializes the item's content as a temporary LocalFile. A LocalFile allows easily proxying the item through any tool that expects a path to an on-disk file.
    Task<LocalFile> GetLocalFile()
    string GetOriginalName()
    Task<string> GetOriginalNameAsync()
    string GetOriginalPath()
    Task<string> GetOriginalPathAsync()
    string GetPageId()
    Task<string> GetPageIdAsync()
    // Retrieves the parent items of this item.
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
    // Creates a copy of this item with optional property overrides.
    Item With(string? name = null, string? mimeType = null, Guid? processId = null, string? groupId = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Creates a copy of this item with the specified process identifier.
    Item WithProcessId(Guid processId)
    const string ObjectMimeTypePrefix
  // Extension methods for Item collections.
  static class ItemExtensions
    // Returns the first item matching the predicate, or null if none found. Use this instead of FirstOrDefault when you need null-checking semantics for Item structs.
    static Item? FirstOrNull(this IEnumerable<Item> items, Func<Item, bool> predicate)
    // Returns the first item, or null if the collection is empty. Use this instead of FirstOrDefault when you need null-checking semantics for Item structs.
    static Item? FirstOrNull(this IEnumerable<Item> items)
  // Optional metadata that can be associated with an item in the pipeline. When outputting an item that has metadata, the metadata will be output alongside the item with .meta.json extension. ItemMetadata is immutable by design to avoid accidental modifications during processing. Use the With method to create modified copies.
  struct ItemMetadata
    // Do not use. Use the constructor which takes a parent ItemMetadata instead.
    ctor()
    // Creates a new ItemMetadata instance, inheriting values from the provided parent metadata where applicable.
    ctor(ItemMetadata? parent, string? previousItemName = null, string? nextItemName = null, string? originalPath = null, string? originalName = null, DateTime? createdAt = null, DateTime? updatedAt = null, string? documentType = null, string? documentTitle = null, IReadOnlyList<string>? titleHierarchy = null, int? pageNumber = null, IReadOnlyList<int>? pageNumbers = null, int? pageCount = null, IReadOnlyDictionary<string, string>? properties = null, string? customJson = null)
    // Creation timestamp of the original file item, if applicable.
    DateTime? CreatedAt { get; init; }
    // User-defined JSON string for custom serialized data.
    string? CustomJson { get; init; }
    // User-defined document title.
    string? DocumentTitle { get; init; }
    // User-defined document type.
    string? DocumentType { get; init; }
    // Name of the next item in a sequence, if applicable.
    string? NextItemName { get; init; }
    // Original file name from which the item was created from, if applicable.
    string? OriginalName { get; init; }
    // Original file path from which the item was created from, if applicable.
    string? OriginalPath { get; init; }
    // Total page count of the original file which this item was created from, if applicable.
    int? PageCount { get; init; }
    // Page number within the original file which this item was created from, if applicable.
    int? PageNumber { get; init; }
    // List of page numbers within a document this item corresponds to, if applicable.
    IReadOnlyList<int>? PageNumbers { get; init; }
    // Name of the previous item in a sequence, if applicable.
    string? PreviousItemName { get; init; }
    // User-defined string key-value pairs for arbitrary metadata.
    IReadOnlyDictionary<string, string>? Properties { get; init; }
    // User-defined title hierarchy.
    IReadOnlyList<string>? TitleHierarchy { get; init; }
    // Last updated timestamp of the original file item, if applicable.
    DateTime? UpdatedAt { get; init; }
    // Because ItemMetadata is immutable, this method allows creating a new instance with modified properties.
    ItemMetadata With(string? previousItemName = null, string? nextItemName = null, string? originalPath = null, string? originalName = null, DateTime? createdAt = null, DateTime? updatedAt = null, string? documentType = null, string? documentTitle = null, IReadOnlyList<string>? titleHierarchy = null, int? pageNumber = null, IReadOnlyList<int>? pageNumbers = null, int? pageCount = null, IReadOnlyDictionary<string, string>? properties = null, string? customJson = null)

namespace Ikon.Pipeline.Remote.Bus
  // Abstraction for transporting remote pipeline processor calls between hosts and clients.
  interface IRemoteCallBus
    // Sends a function call from a client to the host and awaits the response.
    Task<RemoteCallResult> Client_CallHostFunction(RemoteCallMessage message, CancellationToken cancellationToken = default)
    // Streams host function call results back to clients.
    virtual IAsyncEnumerable<RemoteCallResult> Client_GetFunctionCallResults(CancellationToken cancellationToken = default)
    // Retrieves processor calls that the host has dispatched to clients.
    IAsyncEnumerable<RemoteCallMessage> Client_GetProcessorCalls(CancellationToken cancellationToken = default)
    // Sends the outcome of a host-executed processor back to a client.
    Task Client_HostProcessorCallResult(RemoteCallResult result)
    // Sends a processor invocation from the host to clients.
    Task Host_CallProcessor(RemoteCallMessage message)
    // Sends the outcome of a client-executed processor back to the host.
    virtual Task Host_ClientFunctionCallResult(RemoteCallResult result)
    // Retrieves remote function calls destined for the host.
    virtual IAsyncEnumerable<RemoteCallMessage> Host_GetFunctionCalls(CancellationToken cancellationToken = default)
    // Streams processor results generated by clients back to the host.
    IAsyncEnumerable<RemoteCallResult> Host_GetProcessorCallResults(CancellationToken cancellationToken = default)
  // RabbitMQ-backed implementation of IRemoteCallBus supporting host and client roles.
  sealed class RabbitMQRemoteCallBus : IDisposable, IRemoteCallBus
    Task<RemoteCallResult> Client_CallHostFunction(RemoteCallMessage message, CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallResult> Client_GetFunctionCallResults(CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallMessage> Client_GetProcessorCalls(CancellationToken cancellationToken = default)
    Task Client_HostProcessorCallResult(RemoteCallResult result)
    // Creates a new RabbitMQRemoteCallBus configured for the requested roles.
    static Task<RabbitMQRemoteCallBus> CreateAsync(string connectionString, bool isHost, bool isClient, List<string>? processorWhiteList = null)
    void Dispose()
    Task Host_CallProcessor(RemoteCallMessage message)
    Task Host_ClientFunctionCallResult(RemoteCallResult result)
    IAsyncEnumerable<RemoteCallMessage> Host_GetFunctionCalls(CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallResult> Host_GetProcessorCallResults(CancellationToken cancellationToken = default)
    // Configures the client-side whitelist of processors to consume.
    void SetWhiteList(List<string>? processorNames)
  // Represents a remote invocation request exchanged between pipeline hosts and clients.
  sealed class RemoteCallMessage
    // Initializes a new RemoteCallMessage instance for serialization.
    ctor()
    // Initializes a new RemoteCallMessage with the provided processor information.
    ctor(string processorName, object?[] args, Guid correlationId)
    // Serialized arguments for the processor invocation.
    string[] ArgsJson { get; set; }
    // Identifier used to correlate requests with responses.
    Guid CorrelationId { get; set; }
    // Name of the processor that should handle the call.
    string ProcessorName { get; set; }
    // Deserializes the argument at the specified index.
    T? GetArg<T>(int index)
  // Represents the outcome of a remote processor invocation.
  sealed class RemoteCallResult
    // Initializes a new RemoteCallResult for serialization.
    ctor()
    // Initializes a new RemoteCallResult with the provided result information.
    ctor(string processorName, Guid correlationId, string? resultJson, RemoteCallResultType remoteCallResultType, string? errorMessage = "")
    // Correlation identifier shared with the originating request.
    Guid CorrelationId { get; set; }
    // Optional error message describing failure details.
    string? ErrorMessage { get; set; }
    // Name of the processor that produced the result.
    string ProcessorName { get; set; }
    // Serialized result payload.
    string? ResultJson { get; set; }
    // Indicates whether the call succeeded, failed, or produced streaming output.
    RemoteCallResultType ResultType { get; set; }
    // Deserializes the result payload to the requested type.
    T? GetResult<T>()
  // Indicates how a remote call completed and whether additional messages follow.
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
