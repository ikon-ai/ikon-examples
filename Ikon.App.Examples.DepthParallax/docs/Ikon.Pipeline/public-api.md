# Ikon.Pipeline Public API

namespace Ikon.Pipeline
  sealed class EmptyPipelineConfig
    ctor()
  sealed class ExposePipelineAttribute : Attribute
    ctor(Type pipelineType, string? name = null, PipelineExecutionMode executionMode = None, string? schedule = null)
    PipelineExecutionMode ExecutionMode { get; }
    string? Name { get; }
    Type PipelineType { get; }
    string? Schedule { get; }
  static class FunctionRegistryExtensions
    static void RegisterPipeline<TPipeline>(this FunctionRegistry registry, string functionName, string? description = null, object? configInstance = null) where TPipeline : class
  interface IPipelineHost<out TConfig>
    TConfig Config { get; }
    string OrganisationId { get; }
    Secrets Secrets { get; }
    string SpaceId { get; }
  sealed class LocalFile : IDisposable
    ctor(string mimeType, string? existingFilePath = null)
    string MimeType { get; }
    string Path { get; }
    void Dispose()
  sealed class Pipeline<T> where T : IItem<T>
    Task Completion { get; }
    Pipeline<T>.PipelineStatus Status { get; }
    void Complete()
    Pipeline<T>.Branch Inputs()
    bool Post(T item)
    event Pipeline<T>.AsyncEventHandler<T>? Output
  delegate Pipeline<T>.AsyncEventHandler<in TEventArgs> where T : IItem<T>
    Task AsyncEventHandler<in TEventArgs>(object sender, TEventArgs e)
  // Prefer the expression-based Transform/TransformStream/TransformBatch/TransformGroup overloads over their *Lambda counterparts: only expressions can be cached and run remotely. An expression's captured variable values are hashed into the processor id, so changing a captured value invalidates that step's cache.
  sealed class Pipeline<T>.Branch where T : IItem<T>
    ctor(Pipeline<T> outer, ISourceBlock<T> sourceBlock, IDataflowBlock dataflowBlock)
    Pipeline<T>.Branch Filter(Func<T, Task<bool>> predicate, int? maxParallelism = null)
    Pipeline<T>.Branch Filter<TObject>(int? maxParallelism = null)
    // Terminal: ends the branch. Use a Transform* method instead to keep processing downstream.
    void ForEach(Func<T, Task> func, int? maxParallelism = null)
    Pipeline<T>.Branch Merge(params Pipeline<T>.Branch[] branches)
    // Terminal: ends the branch. Sends each item to the pipeline's configured output(s).
    void Output(int? maxParallelism = null)
    void Post(T item)
    void Post(List<T> items)
    void Post(IAsyncEnumerable<T> stream)
    Pipeline<T>.Branch Transform(Expression<Func<T, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch TransformBatch(Expression<Func<List<T>, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, int? maxBatchSize = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch TransformBatchLambda(Func<List<T>, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, int? maxBatchSize = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch TransformGroup(Expression<Func<T, Task<string>>> groupKeySelectorExpr, Expression<Func<List<T>, Task<List<T>>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch TransformGroupLambda(Func<T, Task<string>> groupKeyFunc, Func<List<T>, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch TransformLambda(Func<T, Task<List<T>>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch TransformStream(Expression<Func<T, IAsyncEnumerable<T>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch TransformStream(Expression<Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>>> transformExpr, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch TransformStreamLambda(Func<T, IAsyncEnumerable<T>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    Pipeline<T>.Branch TransformStreamLambda(Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> transformFunc, string? id = null, int? maxParallelism = null, int? maxRetries = null, bool? skipCache = null, bool? allowDuplicates = null, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
  sealed class Pipeline<T>.PipelineStatus where T : IItem<T>
    ctor()
    TimeSpan Duration { get; set; }
    int ErrorLogCount { get; set; }
    int ProcessFailureCount { get; set; }
    int ProcessRetryCount { get; set; }
    int ProcessedItemCacheHits { get; set; }
    int ProcessedItemCount { get; set; }
    int WarningLogCount { get; set; }
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
    Type? InputSchema { get; }
    int MaxInputItems { get; }
    string? Name { get; }
    Type? ResultSchema { get; }
    string? Schedule { get; }
    int Version { get; }
  sealed class PipelineException : Exception
    ctor()
    ctor(string message)
    ctor(string message, Exception innerException)
  static class PipelineFunction
    static Function Create<TPipeline>(string functionName, string? description = null, object? configInstance = null) where TPipeline : class
  readonly struct PipelineFunctionItem
    byte[] Content { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    List<string>? Tags { get; init; }
    static PipelineFunctionItem FromBytes(string name, byte[] content, string? mimeType = null, List<string>? tags = null)
    static PipelineFunctionItem FromString(string name, string content, string? mimeType = null, List<string>? tags = null)
    string GetContentAsString()
  sealed class PipelineRunner : IDisposable
    ctor()
    void Dispose()
    Task Initialize(PipelineRunner.Config config)
    Task Initialize<TPipeline>(TPipeline? userPipelineInstance = default, object? userConfigInstance = null, bool usePersistentCache = false, string? cachePath = null, bool keepRunning = false, string? outputPath = null, bool allApiKeys = false) where TPipeline : class
    Task InitializeForUnitTest()
    Task<List<Item>> Run(List<Item>? items = null, CancellationToken cancellationToken = default)
    IAsyncEnumerable<Item> RunAsEnumerable(List<Item>? items = null, CancellationToken cancellationToken = default)
    static Task RunInExternalAssembly(string configJson, Action<string> onStatusUpdate, CancellationToken cancellationToken)
    static Task RunRemote(PipelineRunner.Config config, Action<PipelineStatus> onStatusUpdate, CancellationToken cancellationToken = default)
    Task RunWithoutCollecting(List<Item>? items = null, CancellationToken cancellationToken = default)
    event Pipeline<Item>.AsyncEventHandler<Item>? Output
    event EventHandler<PipelineStatus>? StatusUpdate
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
  sealed class PipelineRunnerInvoker
    static Task<PipelineRunnerInvoker> Create(string pipelineDllPath)
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
    ctor(string? id = null, int version = 1, int maxParallelism = 0, int maxRetries = 0, bool isRemote = false, bool skipCache = false, bool allowDuplicates = true, ProcessorTags[]? tags = null, Type[]? retryableExceptionTypes = null)
    bool AllowDuplicates { get; set; }
    string? Id { get; set; }
    bool IsRemote { get; set; }
    int MaxParallelism { get; set; }
    int MaxRetries { get; set; }
    Type[] RetryableExceptionTypes { get; set; }
    bool SkipCache { get; set; }
    ProcessorTags[] Tags { get; set; }
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
    T WithProcessId(Guid processId)
  // Immutable, lightweight pointer: it carries a content hash, not the bytes (which live in the content cache). Produce modified copies via the With* methods rather than mutating. The hash is derived from content, MIME type, parent hashes, and tags, so any of those differing yields a distinct item. MIME type is auto-detected from the content when not supplied and sets the output file extension.
  readonly struct Item : IItem<Item>
    // Do not construct directly — always create items via the static Create, CreateInitial, or CreateFromObject methods.
    ctor()
    string GroupId { get; init; }
    string Hash { get; init; }
    string? InitialPath { get; init; }
    bool IsDefault { get; }
    ItemMetadata? Metadata { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    IReadOnlyList<string> ParentHashes { get; init; }
    Guid ProcessId { get; init; }
    IReadOnlyList<string>? Tags { get; init; }
    // Called from processors during the run; the parent items feed the new item's hash. To seed inputs before Run, use CreateInitial.
    static Task<Item> Create(List<Item> parents, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    static Task<Item> Create(Item parent, string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    static Task<Item> Create(List<Item> parents, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    static Task<Item> Create(Item parent, string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    static Task<Item> Create(List<Item> parents, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    static Task<Item> Create(Item parent, string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    static Task<Item> Create(List<Item> parents, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    static Task<Item> Create(Item parent, string name, LocalFile content, List<string>? tags = null, ItemMetadata? metadata = null)
    // Serializes content to JSON. Use inside the pipeline; before Run use CreateInitialFromObject<T>.
    static Task<Item> CreateFromObject<T>(List<Item> parents, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    static Task<Item> CreateFromObject<T>(Item parent, string name, T content, List<string>? tags = null, ItemMetadata? metadata = null, JsonSerializerOptions? jsonSerializerOptions = null)
    // For seeding input items after the pipeline is initialized but before Run. Inside a running pipeline use Create instead.
    static Task<Item> CreateInitial(string name, Stream content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    static Task<Item> CreateInitial(string name, string content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
    static Task<Item> CreateInitial(string name, byte[] content, string? mimeTypeOverride = null, List<string>? tags = null, ItemMetadata? metadata = null)
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
    Item With(string? name = null, string? mimeType = null, Guid? processId = null, string? groupId = null, List<string>? tags = null, ItemMetadata? metadata = null)
    Item WithProcessId(Guid processId)
    const string ObjectMimeTypePrefix
  static class ItemExtensions
    static Item? FirstOrNull(this IEnumerable<Item> items, Func<Item, bool> predicate)
    static Item? FirstOrNull(this IEnumerable<Item> items)
  readonly struct ItemMetadata
    ctor()
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
    Task<RemoteCallResult> Client_CallHostFunction(RemoteCallMessage message, CancellationToken cancellationToken = default)
    virtual IAsyncEnumerable<RemoteCallResult> Client_GetFunctionCallResults(CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallMessage> Client_GetProcessorCalls(CancellationToken cancellationToken = default)
    Task Client_HostProcessorCallResult(RemoteCallResult result)
    Task Host_CallProcessor(RemoteCallMessage message)
    virtual Task Host_ClientFunctionCallResult(RemoteCallResult result)
    virtual IAsyncEnumerable<RemoteCallMessage> Host_GetFunctionCalls(CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallResult> Host_GetProcessorCallResults(CancellationToken cancellationToken = default)
  sealed class RabbitMQRemoteCallBus : IDisposable, IRemoteCallBus
    Task<RemoteCallResult> Client_CallHostFunction(RemoteCallMessage message, CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallResult> Client_GetFunctionCallResults(CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallMessage> Client_GetProcessorCalls(CancellationToken cancellationToken = default)
    Task Client_HostProcessorCallResult(RemoteCallResult result)
    static Task<RabbitMQRemoteCallBus> CreateAsync(string connectionString, bool isHost, bool isClient, List<string>? processorWhiteList = null)
    void Dispose()
    Task Host_CallProcessor(RemoteCallMessage message)
    Task Host_ClientFunctionCallResult(RemoteCallResult result)
    IAsyncEnumerable<RemoteCallMessage> Host_GetFunctionCalls(CancellationToken cancellationToken = default)
    IAsyncEnumerable<RemoteCallResult> Host_GetProcessorCallResults(CancellationToken cancellationToken = default)
    void SetWhiteList(List<string>? processorNames)
  sealed class RemoteCallMessage
    ctor()
    ctor(string processorName, object?[] args, Guid correlationId)
    string[] ArgsJson { get; set; }
    Guid CorrelationId { get; set; }
    string ProcessorName { get; set; }
    T? GetArg<T>(int index)
  sealed class RemoteCallResult
    ctor()
    ctor(string processorName, Guid correlationId, string? resultJson, RemoteCallResultType remoteCallResultType, string? errorMessage = "")
    Guid CorrelationId { get; set; }
    string? ErrorMessage { get; set; }
    string ProcessorName { get; set; }
    string? ResultJson { get; set; }
    RemoteCallResultType ResultType { get; set; }
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
