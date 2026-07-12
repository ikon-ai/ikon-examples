# Pipelines Reference

## Pipelines API Reference

Full Pipeline framework reference and guide.

---

# Ikon.Pipeline Public API

namespace Ikon.Pipeline
  delegate Pipeline<T>.AsyncEventHandler<T, TEventArgs> where T : IItem<T>
    Task AsyncEventHandler<T, TEventArgs>(object sender, TEventArgs e)
  sealed class Pipeline<T>.Branch<T> where T : IItem<T>
    ctor(Pipeline<T> outer, ISourceBlock<T> sourceBlock, IDataflowBlock dataflowBlock)
    Pipeline<T>.Branch<T> Filter(Func<T, Task<bool>> predicate, int? maxParallelism = null)
    Pipeline<T>.Branch<T> Filter<TObject>(int? maxParallelism = null)
    void ForEach(Func<T, Task> func, int? maxParallelism = null)
    Pipeline<T>.Branch<T> Merge(params Pipeline<T>.Branch<T>[] branches)
    void Output(int? maxParallelism = null)
    void Post(T item)
    void Post(List<T> items)
    void Post(IAsyncEnumerable<T> stream)
    Pipeline<T>.Branch<T> Transform(Expression<Func<T, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch<T> TransformBatch(Expression<Func<List<T>, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, int? maxBatchSize = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch<T> TransformBatchLambda(Func<List<T>, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, int? maxBatchSize = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch<T> TransformGroup(Expression<Func<T, Task<string>>> groupKeySelectorExpr, Expression<Func<List<T>, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch<T> TransformGroupLambda(Func<T, Task<string>> groupKeyFunc, Func<List<T>, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch<T> TransformLambda(Func<T, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch<T> TransformStream(Expression<Func<T, IAsyncEnumerable<T>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch<T> TransformStream(Expression<Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch<T> TransformStreamLambda(Func<T, IAsyncEnumerable<T>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch<T> TransformStreamLambda(Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
  sealed class PipelineRunner.Config
    ctor()
    bool AllApiKeys { get; set; }
    string? CachePath { get; set; }
    bool ClearCache { get; set; }
    string? ConfigPath { get; set; }
    CacheType ContentCacheType { get; set; }
    bool DefaultDisableProcessCache { get; set; }
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
    int ScanInterval { get; set; }
    StateType StateType { get; set; }
    int StatusUpdateInterval { get; set; }
    string TypeName { get; set; }
    object? UserConfigInstance { get; set; }
    object? UserPipelineInstance { get; set; }
  // Empty configuration sentinel for pipelines that need a host (for Secrets , OrganisationId , SpaceId ) but no user-defined configuration.
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
    // Creates a new instance of PipelineRunner . Only one runner should exist in a given async context.
    ctor()
    // Releases resources associated with the runner.
    void Dispose()
    Task Initialize(PipelineRunner.Config config)
    // Convenience method that initializes the runner using sensible defaults.
    Task Initialize<TPipeline>(TPipeline? userPipelineInstance = null, object? userConfigInstance = null, bool usePersistentCache = false, string? cachePath = null, bool keepRunning = false, string? outputPath = null, bool allApiKeys = false) where TPipeline : class
    // Simplified initialization used by unit tests.
    Task InitializeForUnitTest()
    // Runs the pipeline with optional in-memory input items and collects all output items into a list. Will return only after the pipeline has completed.
    Task<List<Item>> Run(List<Item>? items = null, CancellationToken cancellationToken = default)
    // Runs the pipeline with optional in-memory input items and returns an asynchronous stream of output items.
    IAsyncEnumerable<Item> RunAsEnumerable(List<Item>? items = null, CancellationToken cancellationToken = default)
    // This method is meant to be used when running the pipeline loaded in an external assembly load context.
    static Task RunInExternalAssembly(string configJson, Action<string> onStatusUpdate, CancellationToken cancellationToken)
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
    // Time elapsed since StartTime .
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
  sealed class Pipeline<T>.PipelineStatus<T> where T : IItem<T>
    ctor()
    TimeSpan Duration { get; set; }
    int ErrorLogCount { get; set; }
    int ProcessFailureCount { get; set; }
    int ProcessRetryCount { get; set; }
    int ProcessedItemCacheHits { get; set; }
    int ProcessedItemCount { get; set; }
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
  sealed class Pipeline<T>.RemoteCall<T> where T : IItem<T>
    ctor(Pipeline<T> pipeline, object? instance, string processorName, object?[] args)
    object?[] Args { get; }
    object? Instance { get; }
    Pipeline<T> Pipeline { get; }
    string ProcessorName { get; }
  static class Pipeline<T>.RemoteCallHelper<T> where T : IItem<T>
    static object? BlockOnResult(Task<object?> task)
    static Task<object?> CallRemoteAsync(Pipeline<T> pipeline, object? instance, MethodInfo method, ProcessorAttribute attr, object[] args)
    static IAsyncEnumerable<TR> CallRemoteStreamAsync<TR>(Pipeline<T> pipeline, object? instance, MethodInfo method, ProcessorAttribute attr, object[] args)
    static Task<RT> CastTaskResult<RT>(Task<object?> task)
    static Task IgnoreTaskResult(Task<object?> task)

namespace Ikon.Pipeline.ContentCache
  enum CacheType
    InMemory
    FileSystem

namespace Ikon.Pipeline.Items
  // Minimal interface for items processed by the pipeline.
  interface IItem<T>
    // Determines whether the underlying content can be treated as the specified object type.
    abstract Task<bool> IsObjectAsync<TObject>()
    // Returns a copy of the item with the supplied process identifier.
    abstract T WithProcessId(Guid processId)
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
    // Creates a new Item inside the pipeline based on parent items. This should be used by processors during pipeline execution. To supply initial inputs before running the pipeline, use CreateInitial instead.
    static Task<Item> Create(List<Item> parents, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create for a single parent item. Use Create within the pipeline and CreateInitial for items created before Run.
    static Task<Item> Create(Item parent, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that accepts string content. Use CreateInitial before running the pipeline and Create from processors.
    static Task<Item> Create(List<Item> parents, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that accepts string content. Use CreateInitial before the pipeline runs and Create for in-pipeline items.
    static Task<Item> Create(Item parent, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that accepts binary content. Use CreateInitial for pre-run items and this method within the pipeline.
    static Task<Item> Create(List<Item> parents, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that accepts binary content. Use CreateInitial to create initial input items before the pipeline runs.
    static Task<Item> Create(Item parent, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that reads content from a LocalFile . Use this when a tool requires a local path. For creating inputs before the pipeline runs, call CreateInitial .
    static Task<Item> Create(List<Item> parents, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    // Overload of Create that reads content from a LocalFile . Use when a tool needs a path on disk. For pre-run items use CreateInitial .
    static Task<Item> Create(Item parent, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    // Serializes an object as JSON and creates a new item within the pipeline. Use this from processors. To generate initial items before running the pipeline, call CreateInitialFromObject .
    static Task<Item> CreateFromObject<T>(List<Item> parents, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // Overload of CreateFromObject for a single parent item. Use CreateInitialFromObject before running the pipeline and this overload inside the pipeline.
    static Task<Item> CreateFromObject<T>(Item parent, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // Creates an initial Item before the pipeline is started. Use this to generate input items outside of the pipeline after it has been initialized but before Run is called. Inside the pipeline, use Create instead of CreateInitial.
    static Task<Item> CreateInitial(string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Convenience overload of CreateInitial that accepts the content as a string. Call Create inside the pipeline; use CreateInitial only before Run.
    static Task<Item> CreateInitial(string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Convenience overload of CreateInitial that accepts the content as a byte array. Call Create inside the pipeline; use CreateInitial beforehand.
    static Task<Item> CreateInitial(string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    // Serializes an object as JSON and creates an initial item from it. Use this before the pipeline runs; processors should call CreateFromObject during pipeline execution instead.
    static Task<Item> CreateInitialFromObject<T>(string name, T content, ItemMetadata? metadata = null, List<string>? tags = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // Retrieves the item's content as a byte array.
    Task<byte[]> GetContentAsBytes()
    // Deserializes the item's JSON content into an object.
    Task<TObject> GetContentAsObject<TObject>()
    // Retrieves the item's content as a Stream .
    Task<Stream> GetContentAsStream()
    // Retrieves the item's content as a UTF-8 string.
    Task<string> GetContentAsString()
    string GetGroupId()
    Task<string> GetGroupIdAsync()
    // Materializes the item's content as a temporary LocalFile . A LocalFile allows easily proxying the item through any tool that expects a path to an on-disk file.
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
    static string ObjectMimeTypePrefix
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
    abstract Task<RemoteCallResult> Client_CallHostFunction(RemoteCallMessage message, CancellationToken cancellationToken = default)
    // Streams host function call results back to clients.
    virtual IAsyncEnumerable<RemoteCallResult> Client_GetFunctionCallResults(CancellationToken cancellationToken = default)
    // Retrieves processor calls that the host has dispatched to clients.
    abstract IAsyncEnumerable<RemoteCallMessage> Client_GetProcessorCalls(CancellationToken cancellationToken = default)
    // Sends the outcome of a host-executed processor back to a client.
    abstract Task Client_HostProcessorCallResult(RemoteCallResult result)
    // Sends a processor invocation from the host to clients.
    abstract Task Host_CallProcessor(RemoteCallMessage message)
    // Sends the outcome of a client-executed processor back to the host.
    virtual Task Host_ClientFunctionCallResult(RemoteCallResult result)
    // Retrieves remote function calls destined for the host.
    virtual IAsyncEnumerable<RemoteCallMessage> Host_GetFunctionCalls(CancellationToken cancellationToken = default)
    // Streams processor results generated by clients back to the host.
    abstract IAsyncEnumerable<RemoteCallResult> Host_GetProcessorCallResults(CancellationToken cancellationToken = default)
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
    T GetArg<T>(int index)
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
    T GetResult<T>()
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
  class FullExamplePipeline.Config
    ctor()
    int TestValue1 { get; set; }
    string TestValue2
  static class ExampleProcessors
    static Task<List<Item>> Run(Item inputItem)
    static Task<List<Item>> Run2(Item inputItem, CancellationToken cancellationToken)
    static Task<List<Item>> Run3(List<Item> inputItems)
    static Task<List<Item>> Run4(List<Item> inputItems, CancellationToken cancellationToken)
  class FullExamplePipeline
    ctor(IPipelineHost<FullExamplePipeline.Config> host)
    Task Run(Pipeline<T>.Branch<Item> inputItems, CancellationToken cancellationToken)
  class FullExamplePipeline.Input
    ctor()
    int TestValue1 { get; set; }
    string TestValue2
  class MinimalExamplePipeline
    ctor()
    Task Run(Pipeline<T>.Branch<Item> inputItems, CancellationToken cancellationToken)
  class FullExamplePipeline.Result
    ctor()
    int TestValue1 { get; set; }
    string TestValue2

namespace Ikon.Pipelines.Public.Processors.Json
  static class MergeJsonProcessor
    static Task<List<Item>> Run(List<Item> items, string itemName)
  static class SplitJsonArrayProcessor
    static Task<List<Item>> Run(Item input)
  static class TrimJsonProcessor
    static Task<List<Item>> Run(Item input, List<string>? fieldsToRemove = null)

namespace Ikon.Pipelines.Public.Processors.OCR
  class OCRProcessor.Config
    ctor()
    OCRModel OCRModel { get; set; }
  static class OCRProcessor
    static Task<List<Item>> Run(Item input, OCRProcessor.Config config, CancellationToken cancellationToken)

namespace Ikon.Pipelines.Public.Processors.Pdf
  class ExtractPdfProcessor.Config
    ctor()
    int MaxPageImageDimension { get; set; }
  static class ExtractPdfProcessor
    static Task<List<Item>> Run(Item input, ExtractPdfProcessor.Config config, CancellationToken cancellationToken)
  interface IPdfDocument : IDisposable
    int PageCount { get; }
    abstract IPdfPage GetPage(int index)
  interface IPdfPage : IDisposable
    double Height { get; }
    int Index { get; }
    double Width { get; }
    abstract void CreateCopy(Stream output)
    abstract (byte[] rgba, byte[] rgbaForHash, int width, int height) GetPixels(int maxDimension)
    abstract (byte[] rgba, byte[] rgbaForHash, int width, int height) GetPixels(int width, int height, bool hasAlpha)
    abstract string GetText()
  static class PdfDocument
    static IPdfDocument Load(byte[] bytes, string? password = null)

namespace Ikon.Pipelines.Public.UniversalRag
  class UniversalRagPipeline.Config
    ctor()
    AnalyzePdfDocumentProcessor.Config AnalyzeDocumentType { get; set; }
    int EmbeddingBatchSize { get; set; }
    ExtractPdfProcessor.Config ExtractPdf { get; set; }
    ExtractFullTextAndSectionsProcessor.Config ExtractSections { get; set; }
    ExtractTextProcessor.Config ExtractText { get; set; }
    FormatWebPageProcessor.Config FormatWebPage { get; set; }
    GenerateEmbeddingsProcessor.Config GenerateEmbeddings { get; set; }
    GenerateSummaryProcessor.Config GenerateSummary { get; set; }
    int MaxLLMParallelism { get; set; }
  class UniversalRagPipeline
    ctor(IPipelineHost<UniversalRagPipeline.Config> host)
    Task Run(Pipeline<T>.Branch<Item> inputItems, CancellationToken cancellationToken)

namespace Ikon.Pipelines.Public.UniversalRag.Processors
  static class AnalyzePdfDocumentProcessor
    static Task<List<Item>> Run(List<Item> inputItems, AnalyzePdfDocumentProcessor.Config config, CancellationToken cancellationToken)
  static class CombineEmbeddingsProcessor
    static Task<List<Item>> Run(List<Item> inputItems, CancellationToken cancellationToken)
  class AnalyzePdfDocumentProcessor.Config
    ctor()
    LLMModel LLMModel { get; set; }
    int PagesToAnalyze { get; set; }
  class ExtractFullTextAndSectionsProcessor.Config
    ctor()
    string ExtraCommand { get; set; }
    string ExtraContext { get; set; }
    bool ExtractFullText { get; set; }
    bool ExtractSections { get; set; }
    LLMModel LLMModel { get; set; }
  class ExtractTextProcessor.Config
    ctor()
    LLMModel LLMModel { get; set; }
  class FormatWebPageProcessor.Config
    ctor()
    string ExtraCommand { get; set; }
    string ExtraContext { get; set; }
    LLMModel LLMModel { get; set; }
  class GenerateEmbeddingsProcessor.Config
    ctor()
    EmbeddingModel EmbeddingModel { get; set; }
  class GenerateSummaryProcessor.Config
    ctor()
    LLMModel LLMModel { get; set; }
  static class ExtractFullTextAndSectionsProcessor
    static Task<List<Item>> Run(Item inputItem, ExtractFullTextAndSectionsProcessor.Config config, CancellationToken cancellationToken)
    static Task<List<Item>> Run(List<Item> inputItems, ExtractFullTextAndSectionsProcessor.Config config, CancellationToken cancellationToken)
  static class ExtractTextProcessor
    static Task<List<Item>> Run(List<Item> inputItems, ExtractTextProcessor.Config config, CancellationToken cancellationToken)
  static class FormatWebPageProcessor
    static Task<List<Item>> Run(Item inputItem, FormatWebPageProcessor.Config config, CancellationToken cancellationToken)
  static class FullTextPassthroughProcessor
    static Task<List<Item>> Run(Item inputItem, CancellationToken cancellationToken)
  static class GenerateEmbeddingsProcessor
    static Task<List<Item>> Run(List<Item> inputItems, GenerateEmbeddingsProcessor.Config config, CancellationToken cancellationToken)
  static class GenerateRouterProcessor
    static Task<List<Item>> Run(List<Item> inputItems, CancellationToken cancellationToken)
  static class GenerateSummaryProcessor
    static Task<List<Item>> Run(Item inputItem, GenerateSummaryProcessor.Config config, CancellationToken cancellationToken)
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
  class ExtractDocumentPageText
    ctor()
    static Task<string> Run(LLMModel llmModel, Item rawTextItem, Item imageItem, CancellationToken cancellationToken = default)
  class ExtractPresentationPageText
    ctor()
    static Task<string> Run(LLMModel llmModel, Item rawTextItem, Item imageItem, CancellationToken cancellationToken = default)
  class ExtractSections
    ctor()
    static Task<ExtractSections.Result> Run(LLMModel llmModel, string documentTextWithLineNumbers, string extraContext, string extraCommand, CancellationToken cancellationToken = default)
  class FormatWebPage
    ctor()
    static Task<FormatWebPage.Result> Run(LLMModel llmModel, string url, string title, string content, string extraContext, string extraCommand, CancellationToken cancellationToken = default)
  class GenerateSummary
    ctor()
    static Task<string> Run(LLMModel llmModel, string content, CancellationToken cancellationToken = default)
  class AnalyzePdfDocument.Result
    ctor()
    string Title { get; set; }
    AnalyzePdfDocument.DocumentType Type { get; set; }
  class ExtractSections.Result
    ctor()
    List<ExtractSections.Section> Sections { get; set; }
  class FormatWebPage.Result
    ctor()
    string Content { get; set; }
    bool HasContent { get; set; }
  class ExtractSections.Section
    ctor()
    int EndLine { get; set; }
    int StartLine { get; set; }
    List<string> TitleHierarchy { get; set; }

namespace Ikon.Pipelines.Public.UniversalRag.Utils
  static class TextUtils
    static string TrimMarkdownBackticks(string input)

namespace Ikon.Pipelines.Public.VideoImageSafety
  enum CollageSelectionMode
    SceneThreshold
    FixedInterval
  class ImageSafetyPipeline.Config
    ctor()
    LLMModel AnalysisModel { get; set; }
    int MaxAnalysisParallelism { get; set; }
    int MaxModerationParallelism { get; set; }
    ClassificationModel ModerationModel { get; set; }
  class VideoSafetyPipeline.Config
    ctor()
    LLMModel AnalysisModel { get; set; }
    int CollageCount { get; set; }
    int CollageFrameWidth { get; set; }
    double CollageIntervalMinutes { get; set; }
    CollageSelectionMode CollageSelection { get; set; }
    LLMModel EvaluationModel { get; set; }
    double FramesPerSecond { get; set; }
    int MaxAnalysisParallelism { get; set; }
    int MaxFrames { get; set; }
    int MaxModerationParallelism { get; set; }
    ClassificationModel ModerationModel { get; set; }
    double SceneChangeThreshold { get; set; }
    int TileColumns { get; set; }
    int TileRows { get; set; }
    string TranscriptionLanguage { get; set; }
    SpeechRecognizerModel TranscriptionModel { get; set; }
    float TranscriptionTemperature { get; set; }
  class ImageSafetyPipeline
    ctor(IPipelineHost<ImageSafetyPipeline.Config> host)
    Task Run(Pipeline<T>.Branch<Item> inputItems, CancellationToken cancellationToken)
  class ImageSafetyResult
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
    ImageSource Source { get; set; }
    string[] TriggeredCategories { get; set; }
  class ImageSource
    ctor()
    string Description { get; set; }
    string Name { get; set; }
    string Url { get; set; }
  class VideoSafetyPipeline
    ctor(IPipelineHost<VideoSafetyPipeline.Config> host)
    Task Run(Pipeline<T>.Branch<Item> inputItems, CancellationToken cancellationToken)
  class VideoSafetyResult
    ctor()
    string ContentCategory { get; set; }
    string Facts { get; set; }
    string IdealAudience { get; set; }
    bool IsSafe { get; set; }
    string Meaning { get; set; }
    string PrimaryRisk { get; set; }
    string RecommendedActions { get; set; }
    string RepresentativeDescription { get; set; }
    string SafetySummary { get; set; }
    VideoSource Source { get; set; }
    string Transcript { get; set; }
    string[] TriggeredCategories { get; set; }
  class VideoSource
    ctor()
    string Description { get; set; }
    string Name { get; set; }
    string Url { get; set; }

namespace Ikon.Pipelines.Public.VideoImageSafety.Shaders
  static class AnalyzeImageSafety
    static Task<AnalyzeImageSafety.Result> RunAsync(LLMModel llmModel, byte[] image, string imageMimeType, string sourceName, string sourceDescription, CancellationToken cancellationToken = default)
  static class AnalyzeVideoFrames
    static Task<AnalyzeVideoFrames.Result> RunAsync(LLMModel llmModel, byte[] collageImage, string collageImageMimeType, CancellationToken cancellationToken = default)
  static class EvaluateVideoSafety
    static Task<EvaluateVideoSafety.Result> RunAsync(LLMModel llmModel, string sourceName, string sourceDescription, string transcript, AnalyzeVideoFrames.Result combinedAnalysis, CancellationToken cancellationToken = default)
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
  class AnalyzeVideoFrames.Result
    ctor()
    string Facts { get; set; }
    string FramesDescription { get; set; }
    string VideoMeaning { get; set; }
  class EvaluateVideoSafety.Result
    ctor()
    string ContentCategory { get; set; }
    string IdealAudience { get; set; }
    bool IsSafe { get; set; }
    string PrimaryRisk { get; set; }
    string RecommendedActions { get; set; }
    string SafetySummary { get; set; }
    string[] TriggeredCategories { get; set; }
