# Ikon.AI Public API

namespace Ikon.AI
  enum GovernanceAction
    Allow
    Deny
    Escalate
    Obfuscate
    Delay
  // The pending AI operation presented to the hook. Operation discriminates surface ("ai_call", "tool", "ingest"); Subject is the thing being acted on (model name, tool name, corpus name); Args are call-specific parameters; Ctx carries host-supplied identity / mission / runtime context (mission_id, agent_id, thread_id, user, tenant, time, etc.).
  sealed class GovernanceCall : IEquatable<GovernanceCall>
    // The pending AI operation presented to the hook. Operation discriminates surface ("ai_call", "tool", "ingest"); Subject is the thing being acted on (model name, tool name, corpus name); Args are call-specific parameters; Ctx carries host-supplied identity / mission / runtime context (mission_id, agent_id, thread_id, user, tenant, time, etc.).
    ctor(string Operation, string Subject, IReadOnlyDictionary<string, object?> Args, IReadOnlyDictionary<string, object?> Ctx)
    IReadOnlyDictionary<string, object?> Args { get; init; }
    IReadOnlyDictionary<string, object?> Ctx { get; init; }
    string Operation { get; init; }
    string Subject { get; init; }
  // What happened after the operation ran (or didn't). Hooks use this in AfterAsync to close out the audit record.
  sealed class GovernanceCallResult : IEquatable<GovernanceCallResult>
    // What happened after the operation ran (or didn't). Hooks use this in AfterAsync to close out the audit record.
    ctor(bool Failed, string Outcome, string? ErrorMessage = null)
    string? ErrorMessage { get; init; }
    bool Failed { get; init; }
    string Outcome { get; init; }
  // Shared invocation wrapper used by every transport that gates a call through GovernanceScope . Builds the standard Before / Deny / Escalate / invoke / After flow once so HTTP, MCP, and any future transport stay symmetric — the only thing each transport supplies is the GovernanceCall shape and the inner invocation. With no hook active the wrap is a pass-through.
  static class GovernanceInvoker
    static Task<T> RunAsync<T>(GovernanceCall call, Func<Task<T>> invoke, CancellationToken ct = null)
  // What the hook decided. The host must honour Action : Allow → invoke the operationDeny → throw GovernanceDeniedException Escalate → suspend / route to Target Obfuscate → apply the named transformDelay → wait the named duration then proceed DecisionId is the audit identifier the host can attach to any subsequent telemetry tied to this operation.
  sealed class GovernanceOutcome : IEquatable<GovernanceOutcome>
    // What the hook decided. The host must honour Action : Allow → invoke the operationDeny → throw GovernanceDeniedException Escalate → suspend / route to Target Obfuscate → apply the named transformDelay → wait the named duration then proceed DecisionId is the audit identifier the host can attach to any subsequent telemetry tied to this operation.
    ctor(GovernanceAction Action, string DecisionId, string RuleId, string PolicyId, string Reason, string? Target = null)
    GovernanceAction Action { get; init; }
    string DecisionId { get; init; }
    string PolicyId { get; init; }
    string Reason { get; init; }
    string RuleId { get; init; }
    string? Target { get; init; }
  // AsyncLocal scope carrying the active IGovernanceHook for the duration of an AI-touched operation. Host code wraps work in using var _ = GovernanceScope.Use(hook);; downstream Ikon AI primitives read Current and apply the hook if present. The scope crosses await boundaries naturally; it does NOT cross Task.Run or manually-started threads. Capture the hook into a local before any fork if you need to.
  static class GovernanceScope
    static IGovernanceHook? Current { get; }
    static IDisposable Use(IGovernanceHook hook)
  // Single hook surface called by every AI-touched primitive in the Ikon platform — LLM calls (Emerge.Run<T>), agent tool dispatch (Ikon.Agent2), data ingest steps — before they act. One contract, three surfaces. Host code activates a hook by entering a GovernanceScope ; downstream primitives read Current and consult the hook if it is set. The default — no scope active — is a no-op pass-through and the AI primitives behave exactly as they do without governance.
  interface IGovernanceHook
    abstract Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    abstract Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
  // Central configuration for SDK connection to the Ikon.AI function host. Uses BackendConfig mode (IkonBackend.Instance token) for authentication. Inherits from AsyncLocalInstance to support proper async local flow in tests and apps.
  class IkonAIConnection : AsyncLocalInstance<IkonAIConnection>
    ctor()
    IkonClientConfig? ConfigOverride { get; set; }
    Task ForceReconnectAsync(CancellationToken ct = null)
    // Gets or creates an IkonClient connected to the Ikon.AI function host. The client is cached per instance to avoid connection overhead on each call. If the client is reconnecting, waits for reconnection to complete.
    Task<IkonClient> GetOrCreateClientAsync(CancellationToken ct = null)
    // Pre-establishes the connection to the host app so that subsequent function calls do not incur connection setup latency.
    Task WarmupAsync(CancellationToken ct = null)
    static string ChannelKey
    static string DevelopmentSpaceId
    static string ExternalUserId
    static string ProductionSpaceId
  class ImplementationSelector : AsyncLocalInstance<ImplementationSelector>
    ctor()
    bool ForceLocal { get; set; }
    bool ForceRemote { get; set; }
  enum ModelCategory
    Classifier
    DepthEstimator
    Embeddings
    FileConverter
    ImageGenerator
    ImageSegmenter
    LLM
    MeshGenerator
    MusicGenerator
    OCR
    Reranker
    SoundEffectGenerator
    SpeechGenerator
    SpeechRecognizer
    VideoEnhancer
    VideoGenerator
    WebScraper
    WebSearcher
  // JSON converter factory that handles deserialization of legacy model enum formats. Supports both the current enum names (e.g., "OpenAI3Small") and legacy canonical names (e.g., "OpenAI_3Small").
  class ModelEnumConverterFactory : JsonConverterFactory
    ctor()
    override bool CanConvert(Type typeToConvert)
    override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  enum ModelRegion
    Global
    Eu
    EuNorth
    EuWest
    EuCentral
    EuSouth
    Us
    UsEast
    UsWest
  struct ModelRegionPriorityKey : IEquatable<ModelRegionPriorityKey>
    ctor(ModelCategory category, Organization organization, string modelFamilyName)
    ModelCategory Category { get; }
    string ModelFamilyName { get; }
    Organization Organization { get; }
  static class ModelRegionSelector
    static void SetPriorityList(ModelRegionPriorityKey key, IReadOnlyList<ModelRegion> priorities)
    static bool TryGetPriorityList(ModelRegionPriorityKey key, out IReadOnlyList<ModelRegion> priorities)
  // Default no-op hook. Allows every call, records nothing. Lets primitives treat the hook contract as non-nullable downstream.
  sealed class NullGovernanceHook : IGovernanceHook
    ctor()
    Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
    static NullGovernanceHook Instance
  enum Organization
    None
    AI21
    Anthropic
    AssemblyAI
    Aws
    Azure
    BlackForestLabs
    Cerebras
    Cohere
    ConvertApi
    DeepInfra
    Deepgram
    ElevenLabs
    Fal
    Fireworks
    Google
    Groq
    Hyperbolic
    Ikon
    Jina
    Meshy
    Mistral
    OpenAI
    OpenRouter
    Pollo
    SerpApi
    Spider
    Stability
    TensorPix
    Together
    Voyage
    XAI

namespace Ikon.AI.Chat
  sealed class BasicChat : IAsyncDisposable
    ctor(AssetUri shaderUri)
    KernelContext BaseContext { get; set; }
    int MaxHistoryLength { get; set; }
    IReadOnlyList<MessageBlock> Messages { get; }
    void AddModelMessage(string text)
    void AddModelMessage(params object?[] parts)
    void AddUserMessage(string text)
    void AddUserMessage(params object?[] parts)
    void ClearMessages()
    void Continue()
    KernelContext CreateKernelContext()
    ValueTask DisposeAsync()
    IAsyncEnumerable<StreamingResult> GenerateAsync(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = null)
    Task<T> GenerateObjectAsync<T>(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = null) where T : new()
    Task<string> GenerateStringAsync(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = null)
    T GetState<T>(string key)
    void SetState(string key, object? value)
    void StopProcessing()
    event EventHandler<string>? RenderedShader

namespace Ikon.AI.Classification
  sealed class ClassificationDetail
    ctor()
    ctor(ClassificationLabel label, string originalCategory, bool isFlagged, double score)
    bool IsFlagged { get; init; }
    ClassificationLabel Label { get; init; }
    string OriginalCategory { get; init; }
    double Score { get; init; }
    static ClassificationDetail ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ClassificationInput
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Text { get; init; }
    string Url { get; init; }
    static ClassificationInput FromMessagePart(IMessagePart messagePart)
    static ClassificationInput ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ClassificationLabel
    Unknown
    SafetyHateSpeech
    SafetyHarassment
    SafetySelfHarm
    SafetySexualContent
    SafetyChildAbuse
    SafetyViolence
    SafetyJailbreak
    SafetyCopyright
    SafetyDangerousContent
    SafetyHealth
    SafetyFinancial
    SafetyLegal
    SafetyPII
  enum ClassificationModel
    OpenAIOmniModeration
    MistralModeration
  static class ClassificationModelExtensions
    static string DisplayName(ClassificationModel model)
  sealed class ClassificationResult
    ctor()
    List<ClassificationDetail> Details { get; init; }
    bool IsFlagged { get; init; }
    static ClassificationResult ReadFromTeleport(ReadOnlySpan<byte> data)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Classifier : IClassifier, IDisposable
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ClassificationModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    Task<ClassificationResult> ClassifyAsync(string text, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    void Dispose()
    static ClassifierCapabilities GetCapabilities(ClassificationModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ClassificationModel model)
  sealed class ClassifierCapabilities
    ctor()
  interface IClassifier : IDisposable
    abstract Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    virtual Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    virtual Task<ClassificationResult> ClassifyAsync(string text, TimeSpan? timeout = null, CancellationToken cancellationToken = null)

namespace Ikon.AI.Database
  sealed class BigQueryDbConnection : DbConnection
    ctor(string projectId, string datasetId)
    string ConnectionString { get; set; }
    string DataSource { get; }
    string Database { get; }
    string ServerVersion { get; }
    ConnectionState State { get; }
    override void ChangeDatabase(string databaseName)
    override void Close()
    override DataTable GetSchema()
    override DataTable GetSchema(string collectionName)
    override DataTable GetSchema(string collectionName, string?[]? restrictionValues)
    override void Open()
  class DatabaseConnection.Config
    ctor()
    string? EnvVarPrefix { get; set; }
    DatabaseConnection.SpaceSecret? SpaceSecret { get; set; }
  class DatabaseInfoExtractor.Config
    ctor()
    List<string>? ColumnExcludeRegex { get; set; }
    Dictionary<string, string> ColumnExtraInfo { get; set; }
    bool IncludeEmptyColumns { get; set; }
    int JsonSampleLengthLimit { get; set; }
    int JsonSampleRowLimit { get; set; }
    int NonTextSampleRowLimit { get; set; }
    List<string>? Schemas { get; set; }
    List<string>? TableExcludeRegex { get; set; }
    Dictionary<string, string> TableExtraInfo { get; set; }
    List<string>? TableIncludeRegex { get; set; }
    int TextSampleLengthLimit { get; set; }
    int TextSampleRowLimit { get; set; }
  class DatabaseColumnInfo
    ctor()
    string ColumnName { get; set; }
    string DataType { get; set; }
    string? Description { get; set; }
    string? ExtraInfo { get; set; }
    string? ForeignKeyColumnName { get; set; }
    string? ForeignKeyTableName { get; set; }
    bool? IsForeignKey { get; set; }
    bool? IsPrimaryKey { get; set; }
    List<string>? Values { get; set; }
  // Creates database connections. Prefer the typed factory methods ( Trino , Postgres , Sqlite , BigQuery ) for app code — host, port, and catalog are not secrets, only the password is. Pass that password from app.Secrets: DatabaseConnection.Trino(host: "trino.example.com", port: 443, catalog: "hive", user: "ikon", password: app.Secrets["TRINO_PASSWORD"]) CreateAsync remains for shared pipelines that read all of host/port/user/password/etc. from environment variables or space secrets.
  class DatabaseConnection
    ctor()
    string BigQueryDataset { get; set; }
    string BigQueryProjectId { get; set; }
    DatabaseType DatabaseType { get; set; }
    DbConnection DbConnection { get; set; }
    static DatabaseConnection BigQuery(string projectId, string dataset)
    static Task<DatabaseConnection> CreateAsync(DatabaseConnection.Config config)
    static DatabaseConnection Postgres(string host, int port, string database, string user, string password)
    static DatabaseConnection Sqlite(string path)
    static DatabaseConnection Trino(string host, int port, string catalog, string user, string password)
  class DatabaseInfo
    ctor()
    DatabaseType DatabaseType { get; set; }
    List<string>? ExampleQuestions { get; set; }
    string? SqlCteCommand { get; set; }
    List<DatabaseTableInfo> Tables { get; set; }
  class DatabaseInfoExtractor
    ctor(DatabaseConnection databaseConnection)
    Task<DatabaseInfo> ExtractAsync(DatabaseInfoExtractor.Config config, CancellationToken cancellationToken)
    Task<ResultSet> GetCteDatabaseInfoAllValuesAsync(DatabaseInfo cteDatabaseInfo, int maxRows)
    static bool IsText(string dataType)
    Task<DatabaseInfo> ValidateAndFillCteDatabaseInfoAsync(DatabaseInfo cteDatabaseInfo, int maxRowsFilter)
  class DatabaseTableInfo
    ctor()
    List<DatabaseColumnInfo> Columns { get; set; }
    string? Description { get; set; }
    string? ExtraInfo { get; set; }
    string TableName { get; set; }
  enum DatabaseType
    Unknown
    PostgreSql
    Sqlite
    BigQuery
    Trino
  class DatabaseConnection.SpaceSecret
    ctor()
    string Prefix { get; set; }
    string SpaceId { get; set; }
  static class SqlValidator
    static void ValidateReadOnly(string sql, HashSet<string> allowedTables)

namespace Ikon.AI.DepthEstimation
  sealed class DepthEstimator : IDepthEstimator, IDisposable
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(DepthEstimatorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = null)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(DepthEstimatorModel model)
  sealed class DepthEstimatorConfig
    ctor()
    int? EnsembleSize { get; set; }
    DepthEstimatorConfig.InputImage Image { get; set; }
    int? NumInferenceSteps { get; set; }
    int? ProcessingResolution { get; set; }
    TimeSpan Timeout { get; set; }
    static DepthEstimatorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum DepthEstimatorModel
    DepthAnythingV2
    Marigold
    Midas
  static class DepthEstimatorModelExtensions
    static string DisplayName(DepthEstimatorModel model)
  sealed class DepthEstimatorResult
    ctor()
    DepthEstimatorResult.OutputImage Depth { get; set; }
    static DepthEstimatorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface IDepthEstimator : IDisposable
    abstract Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = null)
  sealed class DepthEstimatorConfig.InputImage
    ctor()
    byte[]? Data { get; set; }
    string? MimeType { get; set; }
    string? Url { get; set; }
    static DepthEstimatorConfig.InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class DepthEstimatorResult.OutputImage
    ctor()
    byte[] Data { get; set; }
    int Height { get; set; }
    string MimeType { get; set; }
    int Width { get; set; }
    static DepthEstimatorResult.OutputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.Embeddings
  enum EmbeddingEncoding
    Base64
    GzipBase64
  sealed class EmbeddingGenerator : IDisposable, IEmbeddingGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(EmbeddingModel model, IReadOnlyList<ModelRegion>? regions = null)
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    void Dispose()
    Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    static EmbeddingGeneratorCapabilities GetCapabilities(EmbeddingModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(EmbeddingModel model)
  sealed class EmbeddingGeneratorCapabilities
    ctor()
    int EmbeddingVectorSize { get; init; }
    int MaxInputCount { get; init; }
  sealed class EmbeddingItem
    ctor(string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, string embedding)
    string Context { get; init; }
    string Embedding { get; init; }
    float[] EmbeddingArray { get; }
    EmbeddingEncoding Encoding { get; init; }
    EmbeddingModel Model { get; init; }
    EmbeddingType Type { get; init; }
    static Task<EmbeddingItem> Create(string input, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, CancellationToken cancellationToken = null)
    static Task<EmbeddingItem> Create(float[] embedding, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding)
  enum EmbeddingModel
    OpenAIAda2
    OpenAI3Small
    OpenAI3Large
    CohereEmbed4
    MistralEmbed
    CodestralEmbed
    GeminiEmbedding1
    GoogleTextEmbedding5
    GoogleTextMultilingualEmbedding2
    JinaEmbeddings3
    JinaEmbeddings4
    JinaEmbeddings5TextSmall
    JinaEmbeddings5TextNano
    JinaEmbeddings5OmniSmall
    JinaEmbeddings5OmniNano
    Voyage35
    Voyage35Lite
    Voyage4
    Voyage4Lite
    Voyage4Large
  static class EmbeddingModelExtensions
    static string DisplayName(EmbeddingModel model)
  enum EmbeddingType
    Generic
    Document
    Query
    Clustering
    Classification
  interface IEmbeddingGenerator : IDisposable
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    abstract Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
  struct VectorMath.Neighbor
    ctor(int index, float distance)
    float Distance { get; }
    int Index { get; }
  static class VectorMath
    // Calculates the element-wise average embedding from a list of embeddings. Each embedding must be a float array of the same length.
    static float[] CalculateAverageEmbedding(IList<float[]> embeddings)
    // Calculates the cosine similarity between two vectors.
    static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Calculates the dot product of two vectors.
    static float CalculateDotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Calculates the Euclidean distance between two vectors.
    static float CalculateEuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // For each embedding in the list, finds the k nearest neighbors (using Euclidean distance).
    static List<List<VectorMath.Neighbor>> CalculateKNearestNeighbors(IList<float[]> embeddings, int k)
    // Calculates the magnitude (L2 norm) of a vector.
    static float GetMagnitude(ReadOnlySpan<float> vector)

namespace Ikon.AI.FileConversion
  sealed class ConvertedFile
    ctor()
    byte[] Data { get; init; }
    string Mimetype { get; init; }
    string Name { get; init; }
  sealed class FileConverter : IDisposable, IFileConverter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(FileConverterModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    static FileConverterCapabilities GetCapabilities(FileConverterModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(FileConverterModel model)
  sealed class FileConverterCapabilities
    ctor()
  sealed class FileConverterConfig
    ctor()
    AssetUri? AssetUri { get; set; }
    byte[]? Data { get; set; }
    string FileName { get; set; }
    TimeSpan Timeout { get; set; }
    string? Url { get; set; }
  enum FileConverterModel
    ConvertApi
  static class FileConverterModelExtensions
    static string DisplayName(FileConverterModel model)
  interface IFileConverter : IDisposable
    abstract Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = null)

namespace Ikon.AI.ImageGeneration
  interface IImageGenerator : IDisposable
    abstract Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = null)
  enum ImageBackground
    Auto
    Opaque
    Transparent
  sealed class ImageGenerator : IDisposable, IImageGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // One-shot image generation. The verbose form using var generator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage); var results = await generator.GenerateImageAsync(new ImageGeneratorConfig { Prompt = prompt }); var image = results.FirstOrDefault(); becomes var image = await ImageGenerator.GenerateAsync(prompt); Defaults to Gemini25FlashImage (cheap+fast). Override the model via the second parameter when the task warrants. Returns null if the model produces no results — caller should null-check before using .Data / .MimeType. Reach for the constructor + GenerateImageAsync when you need batch generation, custom width/height, an ImageBackground override, input images, or any other ImageGeneratorConfig field beyond the prompt.
    static Task<ImageGeneratorResult?> GenerateAsync(string prompt, ImageGeneratorModel model = Gemini25FlashImage, CancellationToken cancellationToken = null)
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = null)
    static ImageGeneratorCapabilities GetCapabilities(ImageGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageGeneratorModel model)
  sealed class ImageGeneratorCapabilities
    ctor()
  sealed class ImageGeneratorConfig
    ctor()
    ImageBackground Background { get; set; }
    int Count { get; set; }
    int Height { get; set; }
    string ImageSize { get; set; }
    List<InputImage> InputImages { get; set; }
    string NegativePrompt { get; set; }
    string Prompt { get; set; }
    ImageQuality Quality { get; set; }
    SafetyLevel SafetyLevel { get; set; }
    string SearchPrompt { get; set; }
    int Seed { get; set; }
    int Steps { get; set; }
    string Style { get; set; }
    TimeSpan Timeout { get; set; }
    bool UpsamplePrompt { get; set; }
    int Width { get; set; }
    static ImageGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ImageGeneratorModel
    GptImage1Mini
    GptImage15
    GptImage2
    Imagen3
    Imagen4Fast
    Imagen4Standard
    Imagen4Ultra
    Gemini25FlashImage
    Gemini3ProImage
    Gemini31FlashImage
    Gemini31FlashLiteImage
    Flux1Dev
    Flux1Schnell
    Flux11Pro
    Flux11ProUltra
    Flux11ProUltraRaw
    Flux1Fill
    Flux1KontextPro
    Flux1KontextMax
    Flux2Dev
    Flux2Flex
    Flux2Pro
    Flux2Max
    Flux2Klein9B
    Flux2Klein4B
    GrokImagineImage
    GrokImagineImageQuality
  static class ImageGeneratorModelExtensions
    static string DisplayName(ImageGeneratorModel model)
  sealed class ImageGeneratorResult
    ctor()
    byte[] Data { get; set; }
    int Height { get; set; }
    string MimeType { get; set; }
    int Width { get; set; }
    static ImageGeneratorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ImageQuality
    Auto
    Low
    Medium
    High
  sealed class InputImage
    ctor()
    byte[] Data { get; set; }
    double? MaskDilution { get; set; }
    string MimeType { get; set; }
    double? Strength { get; set; }
    InputImageType Type { get; set; }
    static InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum InputImageType
    Normal
    Mask
  enum SafetyLevel
    Level0
    Level1
    Level2
    Level3
    Level4
    Level5
    Level6

namespace Ikon.AI.ImageSegmentation
  sealed class ImageSegmenterConfig.BoxPrompt
    ctor()
    int? ObjectId { get; set; }
    double XMax { get; set; }
    double XMin { get; set; }
    double YMax { get; set; }
    double YMin { get; set; }
    static ImageSegmenterConfig.BoxPrompt ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface IImageSegmenter : IDisposable
    abstract Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = null)
  sealed class ImageSegmenter : IDisposable, IImageSegmenter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageSegmenterModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageSegmenterModel model)
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = null)
  sealed class ImageSegmenterConfig
    ctor()
    List<ImageSegmenterConfig.BoxPrompt> BoxPrompts { get; set; }
    ImageSegmenterConfig.InputImage Image { get; set; }
    int MaxMasks { get; set; }
    List<ImageSegmenterConfig.PointPrompt> PointPrompts { get; set; }
    string? Prompt { get; set; }
    bool ReturnMultipleMasks { get; set; }
    TimeSpan Timeout { get; set; }
    static ImageSegmenterConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ImageSegmenterModel
    Sam3
    Sam31
  static class ImageSegmenterModelExtensions
    static string DisplayName(ImageSegmenterModel model)
  sealed class ImageSegmenterResult
    ctor()
    ImageSegmenterResult.OutputImage? Preview { get; set; }
    List<ImageSegmenterResult.Segment> Segments { get; set; }
    static ImageSegmenterResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ImageSegmenterConfig.InputImage
    ctor()
    byte[]? Data { get; set; }
    string? MimeType { get; set; }
    string? Url { get; set; }
    static ImageSegmenterConfig.InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ImageSegmenterResult.OutputImage
    ctor()
    byte[] Data { get; set; }
    int Height { get; set; }
    string MimeType { get; set; }
    int Width { get; set; }
    static ImageSegmenterResult.OutputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ImageSegmenterConfig.PointPrompt
    ctor()
    bool IsBackground { get; set; }
    int? ObjectId { get; set; }
    double X { get; set; }
    double Y { get; set; }
    static ImageSegmenterConfig.PointPrompt ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ImageSegmenterResult.Segment
    ctor()
    List<double> Box { get; set; }
    ImageSegmenterResult.OutputImage Mask { get; set; }
    double? Score { get; set; }
    static ImageSegmenterResult.Segment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.Kernel
  sealed class AsyncEnumerableExtensions.<G>$CA58BA95B4ED5DE0AC5F384160329049
    Task<T1[]> AsArrayAsync<T1>()
    Task<T1> AsFirstAsync<T1>()
    Task<string> AsStringAsync()
    IAsyncEnumerable<StreamingResult> WithWindowedProcessingAsync(Func<string, List<StreamingResult>, Task<ValueTuple<bool, List<StreamingResult>>>> processAsync, int windowSize = 0, int windowOverlap = 0)
  static class AsyncEnumerableExtensions.<G>$CA58BA95B4ED5DE0AC5F384160329049.<M>$7325656A85ACD35A95DB91A9468B406C
  static class AsyncEnumerableExtensions
    static Task<T1[]> AsArrayAsync<T1>(IAsyncEnumerable<StreamingResult> source)
    static Task<T1> AsFirstAsync<T1>(IAsyncEnumerable<StreamingResult> source)
    static Task<string> AsStringAsync(IAsyncEnumerable<StreamingResult> source)
    static IAsyncEnumerable<StreamingResult> WithCitationsAsync(IAsyncEnumerable<StreamingResult> source, IdMapper idMapper)
    static IAsyncEnumerable<StreamingResult> WithParsedTagsAsync(IAsyncEnumerable<StreamingResult> source, List<string>? tagWhitelist = null, List<string>? tagBlacklist = null)
    static IAsyncEnumerable<StreamingResult> WithReasoningFromTagAsync(IAsyncEnumerable<StreamingResult> source, string reasoningTagName)
    static IAsyncEnumerable<StreamingResult> WithThrottlingAsync(IAsyncEnumerable<StreamingResult> source, int charsPerSecond, int charsPerUpdate, CancellationToken cancellationToken = null)
    static IAsyncEnumerable<StreamingResult> WithWindowedProcessingAsync(IAsyncEnumerable<StreamingResult> source, Func<string, List<StreamingResult>, Task<ValueTuple<bool, List<StreamingResult>>>> processAsync, int windowSize = 0, int windowOverlap = 0)
  struct AudioIdPart : IMessagePart
    ctor(string id)
    string Id { get; }
    MessagePartType Type { get; }
  struct AudioPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  class BinaryDataContainer
    ctor(byte[] data, string mimeType)
    byte[] Data { get; }
    string MimeType { get; }
  class Citation
    ctor(string originalId, string mappedId, int referStartIndex, int referEndIndex, int positionIndex)
    string MappedId { get; }
    string OriginalId { get; }
    int PositionIndex { get; }
    int ReferEndIndex { get; }
    int ReferStartIndex { get; }
  class FinalModelMessage
    ctor(string text)
    string Text { get; }
  class FinalTextResponse
    ctor(string text)
    string Text { get; }
  class FinishReason
    ctor(string reason)
    string Reason { get; }
  class FunctionCall
    ctor(Function function, object?[] parameters, string parametersJson, string callId, string hash, string thoughtSignature = "", string reasoningContent = "")
    string CallId { get; }
    Function Function { get; }
    string Hash { get; }
    object?[] Parameters { get; }
    string ParametersJson { get; }
    string ReasoningContent { get; }
    string ThoughtSignature { get; }
  // Function/tool result carrying media alongside text. Providers that support media inside tool results (Anthropic tool_result image blocks) inline the media so the model actually SEES it; every other consumer degrades to ToString , which summarizes the media without dumping bytes.
  sealed class FunctionMediaResult
    // Function/tool result carrying media alongside text. Providers that support media inside tool results (Anthropic tool_result image blocks) inline the media so the model actually SEES it; every other consumer degrades to ToString , which summarizes the media without dumping bytes.
    ctor(string text, params BinaryDataContainer[] media)
    IReadOnlyList<BinaryDataContainer> Media { get; }
    string Text { get; }
    override string ToString()
  class FunctionResult
    ctor(object? result = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null)
    string? ModelMessagePrefix { get; set; }
    string? ModelMessageSuffix { get; set; }
    object? Result { get; set; }
  struct FunctionResultPart : IMessagePart
    ctor(FunctionCall functionCall, StreamingResult[] streamingResults, object result)
    FunctionCall FunctionCall { get; }
    object Result { get; }
    StreamingResult[] StreamingResults { get; }
    MessagePartType Type { get; }
  interface IMessagePart
    MessagePartType Type { get; }
  struct ImagePart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct ImageUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  struct Instruction
    ctor(InstructionType type, string content)
    string Content { get; }
    InstructionType Type { get; }
  enum InstructionType
    Context
    Command
  class JsonExampleGenerator
    ctor()
    static JsonNode DeepSerialize(object? obj)
    static T GenerateExampleInstance<T>()
    static string GenerateExampleJson<T>()
  // Generates JSON Schema definitions from .NET types. To satisfy the OpenAI spec, every object schema’s "required" array must exactly equal the keys in "properties", and every object schema must have a "type": "object" key. Properties that are allowed to be null are marked according to the target dialect: the 2020-12 dialect expands "type" into a ["X", "null"] union, while the OpenAPI 3.0 dialect adds a sibling "nullable": true.
  static class JsonSchemaGenerator
    static ExpandoObject GenerateJsonSchemaExpandoObject<T>(SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    // Generate the schema as a JsonNode tree rather than a serialised string. Handles primitives (string, int, bool, ...), enums, arrays, dictionaries, and complex types — i.e. valid as a root for any callable shape, not just records. Useful when the caller wants to embed the schema into a larger JSON structure without the round-trip of string→parse.
    static JsonNode GenerateSchemaNode(Type type, string? description = null, SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    static string GenerateSchemaString<T>(SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
    // Non-generic overload for callers that have a Type at runtime (reflection, dynamic dispatch, MCP tool-schema generation). Same semantics as the generic version.
    static string GenerateSchemaString(Type type, SchemaDialect dialect = JsonSchema202012, bool supersetCompatibilityMode = false)
  struct KernelContext : IEquatable<KernelContext>
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    // Alias for Empty . Some generated code reaches for `Default` first (common shadcn / .NET pattern).
    static KernelContext Default { get; }
    bool DisableFunctionCalling { get; init; }
    bool DiscardTextOutputWithFunctionCalls { get; init; }
    // A fresh, blank `KernelContext` — equivalent to `new KernelContext()` or `default`. Provided as a named constant for code generated against frameworks that expect an `.Empty` / `.Default` affordance on context-like types.
    static KernelContext Empty { get; }
    ImmutableDictionary<string, Function> Functions { get; init; }
    string GbnfGrammar { get; init; }
    ImmutableList<Instruction> Instructions { get; init; }
    object? JsonSchema { get; init; }
    bool LogFullRequest { get; init; }
    bool LogFullResponse { get; init; }
    int MaxOutputTokens { get; init; }
    ImmutableList<MessageBlock> Messages { get; init; }
    ReasoningEffort ReasoningEffort { get; init; }
    int ReasoningTokenBudget { get; init; }
    double Temperature { get; init; }
    TimeSpan Timeout { get; init; }
    string ToolPlan { get; init; }
    bool UseAudioOutput { get; init; }
    bool UseCaching { get; init; }
    bool UseCitations { get; init; }
    bool UseJson { get; init; }
    bool UseStreaming { get; init; }
    bool UseUserNames { get; init; }
    KernelContext Add(Instruction instruction)
    KernelContext Add(MessageBlock message)
    static KernelContext Create(IEnumerable<Instruction>? instructions = null, IEnumerable<MessageBlock>? messages = null, IEnumerable<Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    IAsyncEnumerable<StreamingResult> GenerateAsync(ILLM llm, CancellationToken cancellationToken = null)
    KernelContext KeepMessagesMax(int count)
    IAsyncEnumerable<StreamingResult> RecurseAsync(IAsyncEnumerable<StreamingResult> generator, HashSet<string> alreadyCalledFunctions, CancellationToken cancellationToken = null)
    IAsyncEnumerable<StreamingResult> ReturnFunctionCallAsync(string name, string parametersJson, string callId, string thoughtSignature = "", string reasoningContent = "")
    IAsyncEnumerable<StreamingResult> RunFunctionAsync(string functionName, object?[] parameters, CancellationToken cancellationToken = null)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
  enum MediaResolution
    Default
    Low
    Medium
    High
    UltraHigh
  struct MessageBlock
    ctor(MessageBlockRole role, IMessagePart[] parts, string? userName = null)
    ctor(MessageBlockRole role, IEnumerable<IMessagePart> parts, string? userName = null)
    ctor(MessageBlockRole role, string message, string? userName = null)
    IMessagePart[] Parts { get; }
    MessageBlockRole Role { get; }
    string? UserName { get; }
    static MessageBlock? CreateFromObjects(IReadOnlyList<object?> inputs, MessageBlockRole role)
    override string ToString()
  enum MessageBlockRole
    User
    Model
    FunctionResult
  enum MessagePartType
    Text
    Image
    ImageUrl
    Audio
    AudioId
    Video
    VideoUrl
    VideoAsset
    Pdf
    PdfUrl
    FunctionResult
  class OutputAudioId
    ctor(string id)
    string Id { get; }
  class OutputAudioTranscript
    ctor(string transcript)
    string Transcript { get; }
  struct PdfPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct PdfUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  class ReasoningBlock
    ctor(string text)
    string Text { get; }
  enum ReasoningEffort
    None
    Minimal
    Low
    Medium
    High
  // Selects which JSON-schema dialect the generator emits. All Ikon-side schema shapes (primitives, arrays, dictionaries, polymorphism) are expressible in both dialects; the two differ in how they encode nullability and how strictly they police unknown keywords.
  enum SchemaDialect
    JsonSchema202012
    OpenApi30
  struct StreamingResult
    ctor(object value, string sourceName, string? valueTypeName = null)
    string SourceName { get; }
    object Value { get; }
    string? ValueTypeName { get; }
  class Tag
    ctor(string name, string content, Dictionary<string, string>? attributes = null)
    Dictionary<string, string>? Attributes { get; }
    string Content { get; }
    string Name { get; }
  struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  class TokenUsage
    ctor(int inputTokens, int cachedInputTokens, int cacheCreationInputTokens, int outputTokens)
    int CacheCreationInputTokens { get; }
    // Subset of InputTokens served from the provider's prompt cache (Anthropic cache_read_input_tokens, OpenAI cached_tokens, Bedrock CacheReadInputTokens). Always included in InputTokens; this is the cache-attributable portion.
    int CachedInputTokens { get; }
    int InputTokens { get; }
    int OutputTokens { get; }
  class ToolPlan
    ctor(string text)
    string Text { get; }
  struct VideoAssetPart : IMessagePart
    ctor(AssetUri uri, string? mimeType = null, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string? MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    AssetUri Uri { get; }
  struct VideoPart : IMessagePart
    ctor(byte[] content, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    byte[] Content { get; }
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
  struct VideoUrlPart : IMessagePart
    ctor(string url, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    string Url { get; }

namespace Ikon.AI.LLM
  interface ILLM : IDisposable, ILLMInfo
    abstract IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext context, CancellationToken cancellationToken = null)
  interface ILLMInfo
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
    SchemaDialect SchemaDialect { get; }
    bool SupportsGbnfGrammar { get; }
    bool SupportsInputAudio { get; }
    bool SupportsInputImages { get; }
    bool SupportsInputPdf { get; }
    bool SupportsInputVideo { get; }
    bool SupportsJsonSchema { get; }
    bool SupportsOutputAudio { get; }
    bool SupportsParallelToolCalling { get; }
    bool SupportsReasoning { get; }
    bool SupportsStreaming { get; }
    bool SupportsZeroDataRetention { get; }
    bool UsesInlineReasoning { get; }
  sealed class LLM : IDisposable, ILLM, ILLMInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(LLMModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
    SchemaDialect SchemaDialect { get; }
    bool SupportsGbnfGrammar { get; }
    bool SupportsInputAudio { get; }
    bool SupportsInputImages { get; }
    bool SupportsInputPdf { get; }
    bool SupportsInputVideo { get; }
    bool SupportsJsonSchema { get; }
    bool SupportsOutputAudio { get; }
    bool SupportsParallelToolCalling { get; }
    bool SupportsReasoning { get; }
    bool SupportsSingleToolCalling { get; }
    bool SupportsStreaming { get; }
    bool SupportsZeroDataRetention { get; }
    bool UsesInlineReasoning { get; }
    void Dispose()
    IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext context, CancellationToken cancellationToken = null)
    static LLMCapabilities GetCapabilities(LLMModel model)
    static LLMCapabilities GetCapabilities(LLMModel model, IReadOnlyList<ModelRegion>? regions)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(LLMModel model)
  sealed class LLMCapabilities : ILLMInfo
    ctor()
    int ContextWindowSize { get; init; }
    string InlineReasoningTagName { get; init; }
    SchemaDialect SchemaDialect { get; init; }
    bool SupportsGbnfGrammar { get; init; }
    // True when the provider binding can inline images INSIDE tool results (Anthropic tool_result image blocks). Distinct from SupportsInputImages : a vision model whose tool results are JSON-only (e.g. Gemini functionResponse) sees images in messages but not in tool results.
    bool SupportsImagesInToolResults { get; init; }
    bool SupportsInputAudio { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsInputPdf { get; init; }
    bool SupportsInputVideo { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsOutputAudio { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsStreaming { get; init; }
    bool SupportsZeroDataRetention { get; init; }
    bool UsesInlineReasoning { get; init; }
  enum LLMModel
    Gpt4OmniMini
    Gpt41
    Gpt41Mini
    Gpt5
    Gpt5Mini
    Gpt5Nano
    Gpt51
    Gpt52
    Gpt5Pro
    Gpt52Pro
    Gpt53Codex
    Gpt54
    Gpt54Mini
    Gpt54Nano
    Gpt54Pro
    Gpt55
    Gpt55Pro
    O3
    O3Pro
    Claude41Opus
    Claude45Haiku
    Claude45Sonnet
    Claude45Opus
    Claude46Opus
    Claude46Sonnet
    Claude47Opus
    Claude48Opus
    Claude5Sonnet
    Gemini25Flash
    Gemini25FlashLite
    Gemini25Pro
    Gemini3Flash
    Gemini31Pro
    Gemini31FlashLite
    Gemini35Flash
    Grok43
    Grok420Reasoning
    Grok420NonReasoning
    MistralSmall
    MistralMedium
    MistralLarge
    MagistralSmall
    MagistralMedium
    Codestral
    Devstral2
    VoxtralSmall
    CommandR
    CommandA
    CommandAReasoning
    CommandAPlus
    CommandAVision
    CommandR7B
    KimiK25
    KimiK26
    KimiK27Code
    Qwen36
    Qwen37
    Qwen37Max
    GptOss120B
    Glm5
    Glm51
    Glm52
    MiniMaxM25
    MiniMaxM27
    MiniMaxM3
    DeepSeekV32
    DeepSeekV4Pro
    DeepSeekV4Flash
    NovaPro
    NovaLite
    NovaMicro
    Nova2Lite
  static class LLMModelExtensions
    // Maximum input-context window for the model, in tokens (e.g. 200_000 for Claude 4.x base, 1_000_000 for the 1M-context tier). Returns 0 when the model can't be resolved — callers should treat 0 as "unknown" and skip utilization computation rather than dividing by zero.
    static int ContextWindowSize(LLMModel model)
    static string DisplayName(LLMModel model)

namespace Ikon.AI.Legacy
  class Mind : IAsyncDisposable
    ctor()
    Context CurrentUserClientContext { get; }
    string CurrentUserLocale { get; }
    string? DefaultModelName { get; set; }
    string? DefaultSecondaryModelName { get; set; }
    string DefaultUserLocale { get; set; }
    string DefaultUserName { get; set; }
    KernelContext KernelContext { get; }
    Task AddModelInput(string text, bool isHistory = false)
    Task AddUserInput(Context clientContext, string userName, string userLocale, IReadOnlyList<object> inputs, bool isHistory = false)
    Task CancelGenerateAnswer()
    void ClearMessageHistory()
    void ClearState()
    ValueTask DisposeAsync()
    Task GenerateAnswer(string? command = null, string? context = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null, Context? clientContext = null, List<ValueTuple<string, object?>>? variables = null)
    T GetState<T>(string key)
    T GetState<T>(string key, T defaultValue)
    Task InitializeAsync(MindConfig config, Retriever retriever, string mindUserName, Context hostClientContext, AssetUri? shaderUri = null)
    Mind.ShaderLoadResult LoadShader(string shaderContent)
    Task PostMessage(string text)
    Task RegenerateAnswer(Context? clientContext = null)
    Task RequestGenerateAnswer(string? command = null, string? context = null, string? modelMessagePrefix = null, string? modelMessageSuffix = null, Context? clientContext = null, List<ValueTuple<string, object?>>? variables = null)
    Task RequestRegenerateAnswer(Context? clientContext = null)
    void SetState<T>(string key, T value)
    Task StopAsync()
    Task WaitGenerateAnswer()
    Func<Task> Activity
    Func<Task> Cancel
    Func<MindResult, Task> Finish
    Func<List<KernelContext>> GetContexts
    Func<StreamingResult, Task> Output
    Action PreStart
    Action<string> RenderedShader
    Func<Task> Retry
    Func<Task> Start
    Func<Dictionary<string, object?>, Task> StateUpdate
  class MindConfig
    ctor()
    int ActivityIntervalMs
    string BackupFailureMessage
    bool ClipLongUserMessagesInsteadOfError
    bool EnableRenderedShaderLogging
    bool IncludeReasonInFailureMessage
    int MaxHistoryLength
    int MaxRetryCount
    int MaxUserMessageLength
    int MaxUserMessagesRateLimit
    double MaxUserMessagesRateWindow
  class MindResult
    ctor()
    string AudioId { get; set; }
    string ModelMessage { get; set; }
    string TextResponse { get; set; }
  class Mind.ShaderLoadResult
    ctor()
    string ErrorMessage
    bool IsSuccess

namespace Ikon.AI.MeshGeneration
  interface IMeshGenerator : IDisposable, IMeshGeneratorInfo
    abstract Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = null)
  interface IMeshGeneratorInfo
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
  sealed class MeshGeneratorConfig.InputImage
    ctor()
    byte[]? Data { get; set; }
    string? MimeType { get; set; }
    string? Url { get; set; }
    static MeshGeneratorConfig.InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class MeshGenerator : IDisposable, IMeshGenerator, IMeshGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MeshGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
    void Dispose()
    Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = null)
    static MeshGeneratorCapabilities GetCapabilities(MeshGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(MeshGeneratorModel model)
  sealed class MeshGeneratorCapabilities : IMeshGeneratorInfo
    ctor()
    int MaxInputImages { get; init; }
    bool SupportsImageToMesh { get; init; }
    bool SupportsLowPoly { get; init; }
    bool SupportsPbr { get; init; }
    bool SupportsTextToMesh { get; init; }
  sealed class MeshGeneratorConfig
    ctor()
    bool EnablePbr { get; set; }
    List<MeshGeneratorConfig.InputImage> InputImages { get; set; }
    MeshGeneratorMeshStyle MeshStyle { get; set; }
    string? Prompt { get; set; }
    bool Remesh { get; set; }
    int TargetPolycount { get; set; }
    bool Texture { get; set; }
    string? TexturePrompt { get; set; }
    TimeSpan Timeout { get; set; }
    MeshGeneratorTopology Topology { get; set; }
    static MeshGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum MeshGeneratorMeshStyle
    Standard
    LowPoly
  enum MeshGeneratorModel
    Meshy5
    Meshy6
  static class MeshGeneratorModelExtensions
    static string DisplayName(MeshGeneratorModel model)
  // Result of a mesh generation. The URLs are signed and expire roughly three days after generation, so download the model files promptly.
  sealed class MeshGeneratorResult
    ctor()
    DateTimeOffset? ExpiresAt { get; set; }
    string? FbxUrl { get; set; }
    string? GlbUrl { get; set; }
    string? MtlUrl { get; set; }
    string? ObjUrl { get; set; }
    string? ThumbnailUrl { get; set; }
    string? UsdzUrl { get; set; }
    static MeshGeneratorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum MeshGeneratorTopology
    Triangle
    Quad

namespace Ikon.AI.MusicGeneration
  interface IMusicGenerator : IDisposable, IMusicGeneratorInfo
    // Channel count of the PCM samples produced by GenerateMusicAsync .
    int ChannelCount { get; }
    // Sample rate of the PCM samples produced by GenerateMusicAsync .
    int SampleRate { get; }
    // Streams the generated music as PCM AudioContainer chunks as they are produced. Only supported when SupportsStreaming is true; other models throw a MusicGeneratorException . Use GenerateMusicFileAsync for a buffered, encoded audio file instead.
    abstract IAsyncEnumerable<AudioContainer> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = null)
    // Generates the music and returns it as a single buffered, encoded audio file. Supported by all models, including those that cannot stream.
    abstract Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = null)
  interface IMusicGeneratorInfo
    // Whether DurationSeconds controls the length of the output. When false the model ignores it: it emits a fixed-length clip (e.g. Lyria 2 is always ~30s) or, for audio-to-audio editing, the output length follows the input clip.
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    // Whether the model can stream generated audio as it is produced via GenerateMusicAsync . Models without streaming support only expose the buffered GenerateMusicFileAsync result.
    bool SupportsStreaming { get; }
  // A reference clip fed into a prompt-driven music edit. The model preserves the timing and structure of this audio while the prompt re-styles it (timbre, instrumentation, mood). Mirrors the image-to-image InputImage shape used by the image generator.
  sealed class InputAudio
    ctor()
    byte[] Data { get; set; }
    // End of the region to edit, in seconds. null means to the end.
    double? EndSeconds { get; set; }
    string MimeType { get; set; }
    // Start of the region to edit, in seconds. null means from the beginning.
    double? StartSeconds { get; set; }
    // How strongly the output should adhere to this reference, in [0, 1]. Higher keeps the original melody and timing closer. null defaults to strong adherence.
    double? Strength { get; set; }
    static InputAudio ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class MusicGenerator : IDisposable, IMusicGenerator, IMusicGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MusicGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    bool SupportsStreaming { get; }
    void Dispose()
    IAsyncEnumerable<AudioContainer> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = null)
    Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = null)
    static MusicGeneratorCapabilities GetCapabilities(MusicGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(MusicGeneratorModel model)
  sealed class MusicGeneratorCapabilities : IMusicGeneratorInfo
    ctor()
    bool SupportsDurationControl { get; init; }
    bool SupportsEditing { get; init; }
    bool SupportsStreaming { get; init; }
  // Configuration for prompt-driven music generation and editing. With an empty InputAudios the model generates from the prompt alone. With one or more InputAudios it performs audio-to-audio editing: the prompt re-styles the reference clips while their timing and structure are preserved.
  sealed class MusicGeneratorConfig
    ctor()
    // Target length in seconds (clamped to the model's supported range). When editing, set this to the source clip's length so the output keeps the original timing.
    double? DurationSeconds { get; set; }
    bool ForceInstrumental { get; set; }
    List<InputAudio> InputAudios { get; set; }
    string Prompt { get; set; }
    int Seed { get; set; }
    TimeSpan Timeout { get; set; }
    static MusicGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum MusicGeneratorModel
    ElevenLabsMusicV2
    FalStableAudio
    FalLyria2
  static class MusicGeneratorModelExtensions
    static string DisplayName(MusicGeneratorModel model)
  sealed class MusicGeneratorResult
    ctor()
    byte[] AudioData { get; set; }
    string ContentType { get; set; }
    double DurationSeconds { get; set; }
    static MusicGeneratorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.OCR
  enum DocumentType
    General
  interface IOCR : IDisposable, IOCRInfo
    abstract Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = null)
    abstract IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = null)
  interface IOCRInfo
    int MaxPagesSupported { get; }
  sealed class OCR : IDisposable, IOCR, IOCRInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxPagesSupported { get; }
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = null)
    IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    static OCRCapabilities GetCapabilities(OCRModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(OCRModel model)
  sealed class OCRBoundingBox
    ctor()
    int PageNumber { get; init; }
    List<float> Polygon { get; init; }
  sealed class OCRCapabilities : IOCRInfo
    ctor()
    int MaxPagesSupported { get; init; }
  sealed class OCRConfig
    ctor()
    AssetUri? AssetUri { get; set; }
    byte[]? Data { get; set; }
    DocumentType DocumentType { get; set; }
    bool IncludeWords { get; set; }
    string? Pages { get; set; }
    TimeSpan Timeout { get; set; }
    string? Url { get; set; }
  enum OCRModel
    AzureDocumentIntelligence
    MistralOCR
  static class OCRModelExtensions
    static string DisplayName(OCRModel model)
  sealed class OCRPage
    ctor()
    float Height { get; init; }
    int PageNumber { get; init; }
    string Unit { get; init; }
    float Width { get; init; }
  sealed class OCRParagraph
    ctor()
    List<OCRBoundingBox> BoundingRegions { get; init; }
    string Content { get; init; }
  sealed class OCRResult
    ctor()
    List<OCRPage> Pages { get; init; }
    List<OCRParagraph> Paragraphs { get; init; }
    string Text { get; init; }
    List<OCRWord> Words { get; init; }
  sealed class OCRWord
    ctor()
    OCRBoundingBox BoundingBox { get; init; }
    float Confidence { get; init; }
    string Content { get; init; }

namespace Ikon.AI.Policy
  sealed class CreditLimitChecker : IUsageLimitChecker
    ctor()
    ValueTask<UsageLimitCheckResult> CheckAsync(PolicyCallContext context, object?[] args)

namespace Ikon.AI.Reranking
  interface IReranker : IDisposable
    abstract Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
  sealed class RerankItem
    ctor()
    int Index { get; init; }
    double Score { get; init; }
    static RerankItem ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum RerankModel
    CohereRerank4Fast
    CohereRerank4Pro
    JinaReranker3
    VoyageRerank25
    VoyageRerank25Lite
  static class RerankModelExtensions
    static string DisplayName(RerankModel model)
  sealed class Reranker : IDisposable, IReranker
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(RerankModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static RerankerCapabilities GetCapabilities(RerankModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(RerankModel model)
    Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
  sealed class RerankerCapabilities
    ctor()

namespace Ikon.AI.Retrieving
  class Content
    ctor(object value, ContentLink link, string mimeType)
    override string ToString()
    ContentLink Link
    string MimeType
    object Value
  class ContentLink
    ctor(string link, float score = 0)
    ctor(List<string> segments, float score = 0)
    ctor(ContentLink parent, string secondPart, float score = 0)
    ctor(string link, string secondPart, float score = 0)
    ContentLink Parent { get; }
    ContentLink Root { get; }
    override bool Equals(object? obj)
    List<ValueTuple<string, string>> GenerateHierarchicalSplitLinks()
    override int GetHashCode()
    override string ToString()
    string Link
    float Score
    List<string> Segments
  class Retriever.ContentMetadata
    ctor()
    DateTime CreatedAt { get; set; }
    string DocumentTitle { get; set; }
    string OriginalName { get; set; }
    string OriginalPath { get; set; }
    int PageNumber { get; set; }
    List<int> PageNumbers { get; set; }
    List<string> TitleHierarchy { get; set; }
    DateTime UpdatedAt { get; set; }
  class Retriever.Event
    ctor()
    DateTime Date { get; set; }
    string Description { get; set; }
    override string ToString()
    ContentLink Source
  class Retriever.GetContentsOptions
    ctor()
    string ContentPostfixes { get; set; }
    float CumulativeScoreThreshold { get; set; }
    int HitCountThreshold { get; set; }
    int MaxContentCount { get; set; }
    int MaxSearchResults { get; set; }
    int MinContentCount { get; set; }
    float SearchThreshold { get; set; }
    bool UseCumulativeScore { get; set; }
    bool UseIdMapper { get; set; }
  class Retriever.GetContentsOptions2
    ctor()
    bool IncludeFullTexts { get; set; }
    int MaxRerankResults { get; set; }
    int MaxSearchResults { get; set; }
    double RerankThreshold { get; set; }
    float SearchThreshold { get; set; }
  class IdMapper
    ctor(IdMappingType mappingType = None, int randomHexLength = 8, int randomLettersLength = 8, int integerCounter = 0, int? seed = null)
    string ToMapped(string original)
    string ToOriginal(string mapped)
    bool TryToOriginal(string mapped, out string original)
    ConcurrentDictionary<string, string> Mapping
    ConcurrentDictionary<string, string> ReverseMapping
  enum IdMappingType
    None
    RandomHex
    RandomLetters
    IncreasingInteger
  class JsonAsset
    ctor(string content)
    IEnumerable<string> GetAllKeys()
    string[] GetKeys()
    bool TryGetValue(string keyPath, out object? value)
    bool TryGetValueAsObject(string keyPath, out object? value)
  class Retriever : IAsyncDisposable
    ctor()
    KernelContext Context { get; }
    IdMapper IdMapper { get; }
    ValueTask DisposeAsync()
    Task<ContentLink[]> Expand(ContentLink[] links)
    Task<ContentLink[]> Expand(ContentLink link)
    Task<Content?> GetContent(ContentLink link)
    Retriever.ContentMetadata? GetContentMetadata(string metadataId)
    Task<string> GetContents(string query, Retriever.GetContentsOptions options)
    Task<string> GetContents2(string query, Retriever.GetContentsOptions2 options)
    ContentLink? Ignore(ContentLink link, string detail)
    Task InitializeAsync(string dataDirectory, EmbeddingModel embeddingModel = OpenAI3Small)
    Task InitializeAsync(IReadOnlyList<AssetUri> assetUris, EmbeddingModel embeddingModel = OpenAI3Small)
    ContentLink[] Prefer(ContentLink link, string detail)
    ContentLink[] Prefer(ContentLink[] links, string detail)
    Task<ContentLink[]> Search(string query, int maxLinks = 25, float searchThreshold = 0.1)
    Task<Retriever.Event[]> SearchEvents(string startUtcTimestamp, string endUtcTimestamp, int maxResults = 100)
    Task<Retriever.Event[]> SearchEvents(string startUtcTimestamp, string endUtcTimestamp, string searchString, int maxResults = 100)
    Task<KeywordSearchResult[]> SearchKeywords(string searchString, int maxResults = 100)
    Task StopAsync()
    Task WaitForLoadingToEnd()

namespace Ikon.AI.Shader
  class Actions
    ctor()
    ScriptableStringValue AfterPass { get; set; }
    ScriptableStringValue AfterShader { get; set; }
    ScriptableStringValue BeforePass { get; set; }
    ScriptableStringValue BeforeShader { get; set; }
    Dictionary<string, ScriptableStringValue> Listeners
  class FunctionDetailsDictionaryConverter<T> : JsonConverter where T : IFunctionDetails, new()
    ctor()
    override bool CanConvert(Type objectType)
    override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class History
    ctor()
    ScriptableValue<int> Max { get; set; }
    ScriptableValue<int> Skip { get; set; }
  interface IFunctionDetails
    ScriptableValue<bool> Select { get; set; }
  interface IScriptContext
    abstract void AddFilter(string name, KernelContext context, Function function)
    abstract void AddFunction(string name, KernelContext context, Function function)
    abstract bool ContainsKey(string key)
    abstract IEnumerable<string> GetKeys()
    abstract object? GetValue(string key)
    abstract string GetValueAsString(string key)
    abstract void Register<T>() where T : class
    abstract void SetValue(string key, object? value)
  interface IScriptEngine
    abstract IScriptContext CreateContext()
    abstract bool TryParse(string template, out IScriptTemplate? parsedTemplate, out string? errorMessage)
  interface IScriptTemplate
    abstract Task<string> RenderAsync(IScriptContext context)
  class ShaderCache.ImplicitShader
    ctor(AssetUri? shaderUri, string callerFilePath, ShaderCache outer)
    IAsyncEnumerable<StreamingResult> GenerateAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters) where T : new()
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object?>[] parameters)
    Task<Shader> GetShaderAsync()
  class Intent
    ctor()
    History? History { get; set; }
    string Id { get; set; }
    Dictionary<string, object?>? Input { get; set; }
    Misc? Misc { get; set; }
    Model? Model { get; set; }
    List<Pass> Passes { get; set; }
    ScriptableValue<bool> Select { get; set; }
  class JTokenConverter
    ctor()
    static object? ConvertJTokenToObject(JToken? token)
  class Misc
    ctor()
    ScriptableStringValue CitationInsertionCommand { get; set; }
    ScriptableStringValue CitationUserMessageExtension { get; set; }
    List<string> FailClassificationLabels { get; set; }
    ScriptableStringValue FailureMessage { get; set; }
    ScriptableValue<bool> InsertCitationsBackToModelMessage { get; set; }
    ScriptableValue<bool> UseTrimming { get; set; }
  class Model
    ctor()
    ScriptableStringValue AudioOutputVoiceId { get; set; }
    ScriptableValue<int> CharsPerSecond { get; set; }
    ScriptableValue<int> CharsPerUpdate { get; set; }
    ScriptableValue<bool> DisableFunctionCalling { get; set; }
    ScriptableValue<bool> DiscardTextOutputWithFunctionCalls { get; set; }
    ScriptableValue<bool> ForceCitations { get; set; }
    ScriptableStringValue GbnfGrammar { get; set; }
    ExpandoObject? JsonSchema { get; set; }
    ScriptableStringValue JsonSchemaString { get; set; }
    ScriptableValue<bool> LogFullRequest { get; set; }
    ScriptableValue<bool> LogRenderedShader { get; set; }
    ScriptableValue<int> MaxOutputTokens { get; set; }
    ScriptableValue<int> MaxRecursionDepth { get; set; }
    ScriptableStringValue Name { get; set; }
    ScriptableStringValue ReasoningEffort { get; set; }
    ScriptableValue<int> ReasoningTokenBudget { get; set; }
    List<ModelRegion> Regions { get; set; }
    ScriptableValue<int> RequestTimeoutSeconds { get; set; }
    ScriptableValue<double> Temperature { get; set; }
    List<Transform> Transforms { get; set; }
    ScriptableValue<bool> UseAudioOutput { get; set; }
    ScriptableValue<bool> UseCaching { get; set; }
    ScriptableValue<bool> UseCitations { get; set; }
    ScriptableValue<bool> UseJson { get; set; }
    ScriptableValue<bool> UseStreaming { get; set; }
    ScriptableValue<bool> UseThrottling { get; set; }
    ScriptableValue<bool> UseUserNames { get; set; }
  class ModelFunctionDetails : IFunctionDetails
    ctor()
    ScriptableStringValue? Call { get; set; }
    ScriptableValue<bool>? CallOnlyOnce { get; set; }
    ScriptableStringValue Description { get; set; }
    ScriptableValue<bool>? InlineCall { get; set; }
    Dictionary<string, ParameterDetails> Parameters { get; set; }
    ScriptableStringValue Process { get; set; }
    ScriptableValue<bool> Select { get; set; }
    ScriptableStringValue? Use { get; set; }
  class Output
    ctor()
    ScriptableStringValue AfterPass { get; set; }
    ScriptableStringValue AfterShader { get; set; }
    ScriptableStringValue BeforePass { get; set; }
    ScriptableStringValue BeforeShader { get; set; }
  class ParameterDetails
    ctor()
    object? DefaultValue { get; set; }
    ScriptableStringValue? Description { get; set; }
    ScriptableValue<bool>? HasDefaultValue { get; set; }
    ScriptableStringValue? Type { get; set; }
    ScriptableStringValue? Use { get; set; }
  class Pass
    ctor()
    Actions Actions { get; set; }
    ScriptableStringValue Command { get; set; }
    ScriptableStringValue Context { get; set; }
    History? History { get; set; }
    string Id { get; set; }
    Dictionary<string, object?>? Input { get; set; }
    Misc? Misc { get; set; }
    Model? Model { get; set; }
    Dictionary<string, ModelFunctionDetails> ModelFunctions { get; set; }
    Output Output { get; set; }
    ScriptableValue<bool> Select { get; set; }
    Dictionary<string, TemplateFunctionDetails> TemplateFunctions { get; set; }
  class ScriptableStringDictionaryConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class ScriptableStringValue
    ctor(string? value = "")
    bool IsScript { get; }
    string? Value { get; }
    Task<string?> GetValueAsync(Func<string, Task<string>> renderer)
  class ScriptableStringValueConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class ScriptableValueConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class ScriptableValue<T> where T : struct
    ctor(T value)
    ctor(string script)
    string? Script { get; }
    T? Value { get; }
    Task<T> GetValueAsync(Func<string, Task<string>> renderer)
  class Shader
    ctor(string shaderConfigAsJsonString, bool enableRenderedShaderLogging = false)
    Dictionary<string, object?> Input { get; }
    static string Escape(string? text)
    IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, ExpandoObject? implicitJsonSchema = null, string? implicitJsonExample = null, IdMapper? idMapper = null, string modelUserName = "", string modelMessagePrefix = "", string modelMessageSuffix = "", int iteration = 0, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null)
    Task<T> GenerateObjectAsync<T>(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, JsonSerializerOptions? jsonSerializerOptions = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null) where T : new()
    Task<string> GenerateStringAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = null)
    void SetActiveState<T>(string key, T value)
    static string Unescape(string? text)
    event EventHandler<string>? RenderedShader
  class ShaderCache : AsyncLocalInstance<ShaderCache>
    ctor()
    string? DefaultSpaceId { get; set; }
    ShaderCache.ImplicitShader GetImplicitShader(string callerFilePath = "")
  class ShaderConfig
    ctor()
    static object Default { get; }
    History History { get; set; }
    Dictionary<string, object?> Input { get; set; }
    List<Intent> Intents { get; set; }
    ScriptableValue<int> MaxLogLineLength { get; set; }
    ScriptableValue<int> MaxLogSectionLineCount { get; set; }
    Misc Misc { get; set; }
    Model Model { get; set; }
    string ShaderLanguage { get; set; }
    int? ShaderVersion { get; set; }
  class ShaderInvocationContext
    ctor()
    string FailureMessage { get; }
    string Reasoning { get; }
  class StyleInvariantComparer : IEqualityComparer<string>
    ctor()
    bool Equals(string? x, string? y)
    int GetHashCode(string obj)
  class TemplateFunctionDetails : IFunctionDetails
    ctor()
    ScriptableStringValue Name { get; set; }
    ScriptableValue<bool> Select { get; set; }
  class Shader.TemplateMessage
    ctor()
    string Content { get; set; }
    string Role { get; set; }
  class Transform
    ctor()
    Dictionary<string, object?> Config { get; set; }
    ScriptableStringValue Name { get; set; }
    ScriptableValue<bool> ProcessInput { get; set; }
    ScriptableValue<bool> ProcessOutput { get; set; }
    ScriptableValue<int> WindowOverlap { get; set; }
    ScriptableValue<int> WindowSize { get; set; }

namespace Ikon.AI.Shader.Scriban
  class ScribanScriptEngine : IScriptEngine
    ctor()
    IScriptContext CreateContext()
    bool TryParse(string template, out IScriptTemplate? parsedTemplate, out string? errorMessage)

namespace Ikon.AI.SoundEffectGeneration
  interface ISoundEffectGenerator : IDisposable, ISoundEffectGeneratorInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    abstract IAsyncEnumerable<AudioContainer> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
  interface ISoundEffectGeneratorInfo
    bool SupportsLooping { get; }
  sealed class SoundEffectFileResult
    byte[] AudioData { get; init; }
    string ContentType { get; init; }
    double DurationSeconds { get; init; }
  sealed class SoundEffectGenerator : IDisposable, ISoundEffectGenerator, ISoundEffectGeneratorInfo
    ctor(string modelName)
    ctor(SoundEffectGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SoundEffectGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsLooping { get; }
    void Dispose()
    IAsyncEnumerable<AudioContainer> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
    Task<SoundEffectFileResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
    static SoundEffectGeneratorCapabilities GetCapabilities(SoundEffectGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SoundEffectGeneratorModel model)
  sealed class SoundEffectGeneratorCapabilities : ISoundEffectGeneratorInfo
    ctor()
    bool SupportsLooping { get; init; }
  sealed class SoundEffectGeneratorConfig
    ctor()
    double? DurationSeconds { get; set; }
    bool Loop { get; set; }
    string Prompt { get; set; }
    double PromptInfluence { get; set; }
    TimeSpan Timeout { get; set; }
    static SoundEffectGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum SoundEffectGeneratorModel
    ElevenLabsV2
  static class SoundEffectGeneratorModelExtensions
    static string DisplayName(SoundEffectGeneratorModel model)

namespace Ikon.AI.SpeechGeneration
  sealed class TextFilter.Config
    ctor()
    int MaxTextLength { get; set; }
    bool RemoveEmojis { get; set; }
    bool SimplifyUrls { get; set; }
    bool SpeakOnlyFirstParagraph { get; set; }
  interface ISpeechGenerator : IDisposable
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    abstract IAsyncEnumerable<AudioContainer> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = null)
  sealed class SpeechGenerator : IDisposable, ISpeechGenerator
    ctor(string modelName)
    ctor(SpeechGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    void Dispose()
    IAsyncEnumerable<AudioContainer> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = null)
    static SpeechGeneratorCapabilities GetCapabilities(SpeechGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechGeneratorModel model)
    static IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> GetVoiceIdsByModel()
  sealed class SpeechGeneratorCapabilities
    ctor()
  sealed class SpeechGeneratorConfig
    ctor()
    string Instructions { get; set; }
    string Language { get; set; }
    string Speed { get; set; }
    string Text { get; set; }
    TimeSpan Timeout { get; set; }
    string VoiceId { get; set; }
    static SpeechGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class SpeechGeneratorExtensions
    static Task StreamSpeechAsync(ISpeechGenerator speechGenerator, SpeechGeneratorConfig config, Func<AudioContainer, Task> onAudio, CancellationToken cancellationToken = null)
  enum SpeechGeneratorModel
    AzureSpeechService
    OpenAITts1
    OpenAITts1Hd
    Gpt4OmniMiniTts
    ElevenFlash2
    ElevenMultilingual2
    ElevenFlash25
    Eleven3
    GoogleChirp3
    Gemini25FlashTts
    Gemini25ProTts
    Gemini31FlashTts
  static class SpeechGeneratorModelExtensions
    static string DisplayName(SpeechGeneratorModel model)
  static class TextFilter
    static string Filter(string text, TextFilter.Config config)

namespace Ikon.AI.SpeechRecognition
  sealed class AnalyzePronunciationConfig
    ctor()
    int ChannelCount { get; set; }
    string Language { get; set; }
    string ReferenceText { get; set; }
    int SampleRate { get; set; }
    float[] Samples { get; set; }
    byte[] SamplesPcm16 { get; set; }
    TimeSpan Timeout { get; set; }
    static AnalyzePronunciationConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Break
    ctor()
    int BreakLength { get; init; }
    List<string> ErrorTypes { get; init; }
    Pronunciation.MissingBreak MissingBreak { get; init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get; init; }
    static Pronunciation.Break ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    TimeSpan MaxSpeechDuration { get; set; }
    SpeechRecognizerAdapter.Mode Mode { get; set; }
    TimeSpan RecognitionInterval { get; set; }
    TimeSpan RequestTimeout { get; set; }
    TimeSpan SilenceDuration { get; set; }
    float SilenceThreshold { get; set; }
  sealed class Pronunciation.Feedback
    ctor()
    Pronunciation.Prosody Prosody { get; init; }
    static Pronunciation.Feedback ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface ISpeechRecognizer : IDisposable, ISpeechRecognizerInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    abstract Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = null)
    abstract Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = null)
    abstract IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = null)
  interface ISpeechRecognizerInfo
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
  sealed class Pronunciation.Intonation
    ctor()
    List<string> ErrorTypes { get; init; }
    Pronunciation.Monotone Monotone { get; init; }
    static Pronunciation.Intonation ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.MissingBreak
    ctor()
    double Confidence { get; init; }
    static Pronunciation.MissingBreak ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
  sealed class Pronunciation.Monotone
    ctor()
    double SyllablePitchDeltaConfidence { get; init; }
    static Pronunciation.Monotone ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.NBest
    ctor()
    double Confidence { get; init; }
    string Display { get; init; }
    string ITN { get; init; }
    string Lexical { get; init; }
    string MaskedITN { get; init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Word> Words { get; init; }
    static Pronunciation.NBest ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Phoneme
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
    static Pronunciation.Phoneme ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.PhonemePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    static Pronunciation.PhonemePronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class Pronunciation
  sealed class Pronunciation.PronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    double CompletenessScore { get; init; }
    double FluencyScore { get; init; }
    double PronScore { get; init; }
    double ProsodyScore { get; init; }
    static Pronunciation.PronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Prosody
    ctor()
    Pronunciation.Break Break { get; init; }
    Pronunciation.Intonation Intonation { get; init; }
    static Pronunciation.Prosody ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RecognizeContinuousSpeechConfig
    ctor()
    string[] CandidateLanguages { get; set; }
    int ChannelCount { get; set; }
    string Language { get; set; }
    int SampleRate { get; set; }
    static RecognizeContinuousSpeechConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RecognizeSpeechConfig
    ctor()
    int ChannelCount { get; set; }
    string Language { get; set; }
    string Prompt { get; set; }
    int SampleRate { get; set; }
    float[] Samples { get; set; }
    byte[] SamplesPcm16 { get; set; }
    double Temperature { get; set; }
    TimeSpan Timeout { get; set; }
    static RecognizeSpeechConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Result
    ctor()
    int Channel { get; init; }
    string DisplayText { get; init; }
    long Duration { get; init; }
    string Id { get; init; }
    List<Pronunciation.NBest> NBest { get; init; }
    long Offset { get; init; }
    string RecognitionStatus { get; init; }
    double SNR { get; init; }
    static Pronunciation.Result ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SpeechRecognizer : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(SpeechRecognizerModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    static SpeechRecognizerCapabilities GetCapabilities(SpeechRecognizerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechRecognizerModel model)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = null)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = null)
  sealed class SpeechRecognizerAdapter : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(ISpeechRecognizer speechRecognizer, SpeechRecognizerAdapter.Config? config = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = null)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = null)
  sealed class SpeechRecognizerCapabilities : ISpeechRecognizerInfo
    ctor()
    bool SupportsBatchRecognition { get; init; }
    bool SupportsContinuousRecognition { get; init; }
    bool SupportsPronunciationAnalysis { get; init; }
  enum SpeechRecognizerModel
    AzureSpeechService
    Whisper2
    WhisperLarge3
    WhisperLarge3Turbo
    Gpt4OmniTranscribe
    Gpt4OmniMiniTranscribe
    DeepgramNova3General
    AssemblyAIUniversal3ProStreaming
    AssemblyAIUniversalStreamingEnglish
    AssemblyAIUniversalStreamingMultilingual
    VoxtralMiniTranscribe2
  static class SpeechRecognizerModelExtensions
    static string DisplayName(SpeechRecognizerModel model)
  sealed class Pronunciation.Syllable
    ctor()
    long Duration { get; init; }
    string Grapheme { get; init; }
    long Offset { get; init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
    static Pronunciation.Syllable ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.SyllablePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    static Pronunciation.SyllablePronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.UnexpectedBreak
    ctor()
    double Confidence { get; init; }
    static Pronunciation.UnexpectedBreak ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Word
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    List<Pronunciation.Phoneme> Phonemes { get; init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Syllable> Syllables { get; init; }
    string Text { get; init; }
    static Pronunciation.Word ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.WordPronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    string ErrorType { get; init; }
    Pronunciation.Feedback Feedback { get; init; }
    static Pronunciation.WordPronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.Storage
  class KeywordIndex
    ctor()
    Task Add(string word, string link)
    static KeywordIndex Deserialize(Stream stream)
    Task InitializeAsync()
    void RemoveTooCommonTerms(double threshold = 0.5, int minDocumentCount = 5)
    List<KeywordSearchResult> Search(string words)
    void Serialize(Stream stream)
  struct KeywordSearchResult
    ctor(string link, float score)
    string Link
    float Score
  enum Metric
    DotProduct
    CosineSimilarity
    EuclideanDistance
  struct Result<T>
    ctor(int key, float score, T value)
    int Key
    float Score
    T Value
  class VectorDatabase
    ctor()
    Task CreateCollectionAsync(string collectionName, EmbeddingModel model)
    Task<int> GetDataItemCount(string collectionName)
    Task RemoveAsync(string collectionName, IEnumerable<string> tags)
    Task<List<Result<object>>> SearchAsync(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<object>>> SearchAsync(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<int> SetAsync(string collectionName, int? key, string text, object value, IEnumerable<string>? tags = null)
    Task<int> SetAsync(string collectionName, int? key, float[] vector, object value, IEnumerable<string>? tags = null)

namespace Ikon.AI.Utils
  static class HttpUtils
    static Task<string> DumpHttpRequest(HttpRequestMessage request)
    static Task<string> GetErrorMessage(HttpRequestException exception, HttpResponseMessage? response, string modelName)
    static Task<int> GetHttpRequestSize(HttpRequestMessage request)
  static class ImageUtils
    static byte[] ConvertAlphaMaskToBlackWhiteMask(byte[] maskData)
    static byte[] ConvertBlackWhiteMaskToAlphaMask(byte[] maskData)
    // Re-encodes an image as JPEG with both dimensions capped at maxDimension (aspect preserved). Returns the original bytes untouched when the image already fits AND is at most maxBytes — small screenshots pass through without a decode cost. Intended for images going into LLM context, where anything above ~1568px is downscaled by the provider anyway and only costs tokens.
    static ValueTuple<byte[], string, int, int> EncodeJpegCapped(byte[] source, string sourceMimeType, int maxDimension = 1568, int quality = 70, int maxBytes = 204800)
    static ValueTuple<int, int> GetImageDimensions(byte[] buffer)
    static byte[] InvertMask(byte[] maskData)

namespace Ikon.AI.VideoEnhancement
  interface IVideoEnhancer : IDisposable
    abstract Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = null)
  sealed class VideoEnhancer : IDisposable, IVideoEnhancer
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoEnhancerModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = null)
    static VideoEnhancerCapabilities GetCapabilities(VideoEnhancerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoEnhancerModel model)
  sealed class VideoEnhancerCapabilities
    ctor()
  sealed class VideoEnhancerConfig
    ctor()
    int? EndFrame { get; set; }
    string? MimeType { get; set; }
    int? StartFrame { get; set; }
    int? TargetFps { get; set; }
    TimeSpan Timeout { get; set; }
    byte[]? VideoData { get; set; }
    string? VideoUrl { get; set; }
    static VideoEnhancerConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum VideoEnhancerModel
    TensorPixFpsBoost
    TensorPixUpscale2xUltra4
    TensorPixUpscale2xUltra41
    TensorPixUpscale4xUltra4
  static class VideoEnhancerModelExtensions
    static string DisplayName(VideoEnhancerModel model)
  sealed class VideoEnhancerResult
    ctor()
    int? OutputFps { get; init; }
    long? OutputSizeBytes { get; init; }
    string Url { get; init; }
    static VideoEnhancerResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.VideoGeneration
  interface IVideoGenerator : IDisposable, IVideoGeneratorInfo
    abstract Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = null)
  interface IVideoGeneratorInfo
    int MaxInputImages { get; }
    VideoGeneratorResolutionMode ResolutionMode { get; }
    IReadOnlyList<int> SupportedLengths { get; }
    IReadOnlyList<VideoGeneratorResolution> SupportedResolutions { get; }
    bool SupportsAudio { get; }
    bool SupportsImageToVideo { get; }
    bool SupportsMultipleImages { get; }
    bool SupportsNegativePrompt { get; }
    bool SupportsSeed { get; }
    bool SupportsTailImage { get; }
    bool SupportsTextToVideo { get; }
  sealed class VideoGeneratorConfig.InputImage
    ctor()
    byte[]? Data { get; set; }
    string? MimeType { get; set; }
    string? Url { get; set; }
    static VideoGeneratorConfig.InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class VideoGenerator : IDisposable, IVideoGenerator, IVideoGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputImages { get; }
    VideoGeneratorResolutionMode ResolutionMode { get; }
    IReadOnlyList<int> SupportedLengths { get; }
    IReadOnlyList<VideoGeneratorResolution> SupportedResolutions { get; }
    bool SupportsAudio { get; }
    bool SupportsImageToVideo { get; }
    bool SupportsMultipleImages { get; }
    bool SupportsNegativePrompt { get; }
    bool SupportsSeed { get; }
    bool SupportsTailImage { get; }
    bool SupportsTextToVideo { get; }
    void Dispose()
    Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = null)
    static VideoGeneratorCapabilities GetCapabilities(VideoGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoGeneratorModel model)
  enum VideoGeneratorAspectRatio
    Ratio16x9
    Ratio9x16
    Ratio4x3
    Ratio3x4
    Ratio1x1
  sealed class VideoGeneratorCapabilities : IVideoGeneratorInfo
    ctor()
    int MaxInputImages { get; init; }
    VideoGeneratorResolutionMode ResolutionMode { get; init; }
    IReadOnlyList<int> SupportedLengths { get; init; }
    IReadOnlyList<VideoGeneratorResolution> SupportedResolutions { get; init; }
    bool SupportsAudio { get; init; }
    bool SupportsImageToVideo { get; init; }
    bool SupportsMultipleImages { get; init; }
    bool SupportsNegativePrompt { get; init; }
    bool SupportsSeed { get; init; }
    bool SupportsTailImage { get; init; }
    bool SupportsTextToVideo { get; init; }
  sealed class VideoGeneratorConfig
    ctor()
    VideoGeneratorAspectRatio AspectRatio { get; set; }
    bool? GenerateAudio { get; set; }
    List<VideoGeneratorConfig.InputImage> InputImages { get; set; }
    int Length { get; set; }
    string? NegativePrompt { get; set; }
    string? Prompt { get; set; }
    VideoGeneratorResolution Resolution { get; set; }
    int? Seed { get; set; }
    TimeSpan Timeout { get; set; }
    static VideoGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum VideoGeneratorModel
    Hailuo23
    Hailuo23Fast
    Kling26
    Kling30
    Kling30Omni
    KlingVideoO1
    LumaRay20
    LumaRay20Flash
    Pika22
    Pixverse55
    Pollo20
    Pollo30
    Pollodance20
    Pollodance20Fast
    RunwayGen4
    Seedance15Pro
    Sora2
    Sora2Pro
    Veo31
    Veo31Fast
    ViduQ2Pro
    ViduQ2Turbo
    ViduQ3Pro
    Wan26
    Wan27
    GrokImagineVideo
    GrokImagineVideo15
  static class VideoGeneratorModelExtensions
    static string DisplayName(VideoGeneratorModel model)
  enum VideoGeneratorResolution
    Resolution360p
    Resolution480p
    Resolution540p
    Resolution720p
    Resolution768p
    Resolution1080p
    Resolution4K
  enum VideoGeneratorResolutionMode
    Discrete
    AspectRatio
  sealed class VideoGeneratorResult
    ctor()
    string Url { get; init; }
    static VideoGeneratorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.AI.WebScraping
  sealed class Cookie
    ctor()
    string Domain { get; set; }
    double ExpirationDate { get; set; }
    bool HostOnly { get; set; }
    bool HttpOnly { get; set; }
    int Id { get; set; }
    string Name { get; set; }
    string Path { get; set; }
    string SameSite { get; set; }
    bool Secure { get; set; }
    bool Session { get; set; }
    string StoreId { get; set; }
    string Value { get; set; }
    static Cookie ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class DownloadFileConfig
    ctor()
    string CountryCode { get; set; }
    TimeSpan Timeout { get; set; }
    string Url { get; set; }
    static DownloadFileConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class DownloadFileResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Url { get; init; }
    static DownloadFileResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface IWebScraper : IDisposable, IWebScraperInfo
    abstract Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = null)
    abstract Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = null)
    abstract Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = null)
    abstract Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = null)
  interface IWebScraperInfo
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
  sealed class MultiPageScrapeConfig
    ctor()
    bool AddGivenUrlsToWhitelist { get; set; }
    bool AllowOnlyGivenUrls { get; set; }
    List<Cookie> Cookies { get; set; }
    string CountryCode { get; set; }
    int DelayMs { get; set; }
    string ExcludedCSSElements { get; set; }
    List<string> ExcludedLineStarts { get; set; }
    List<string> ExcludedWholeLines { get; set; }
    bool Headless { get; set; }
    bool IgnoreRobotsTxt { get; set; }
    bool IncludeLinkedFiles { get; set; }
    string IncludedCSSElements { get; set; }
    string JavaScript { get; set; }
    bool LoadResources { get; set; }
    string Locale { get; set; }
    int MaxDepth { get; set; }
    int MaxPages { get; set; }
    WebScraperOutputFormat OutputFormat { get; set; }
    string PlaywrightScript { get; set; }
    bool RerunIfGivenUrlsMissing { get; set; }
    TimeSpan SinglePageTimeout { get; set; }
    TimeSpan Timeout { get; set; }
    List<string> UrlBlacklist { get; set; }
    List<string> UrlWhitelist { get; set; }
    List<string> Urls { get; set; }
    bool UseReadability { get; set; }
    bool UseSitemap { get; set; }
    bool UseSitemapOnly { get; set; }
    bool UseStreaming { get; set; }
    TimeSpan WaitAfter { get; set; }
    static MultiPageScrapeConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class PageResult
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string Mimetype { get; init; }
    string Title { get; init; }
    string Url { get; init; }
    static PageResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ScreenshotConfig
    ctor()
    List<Cookie> Cookies { get; set; }
    string CountryCode { get; set; }
    bool FullPage { get; set; }
    bool Headless { get; set; }
    int Height { get; set; }
    string JavaScript { get; set; }
    string Locale { get; set; }
    string PlaywrightScript { get; set; }
    TimeSpan Timeout { get; set; }
    string Url { get; set; }
    bool UseCaptchaSolver { get; set; }
    TimeSpan WaitAfter { get; set; }
    int Width { get; set; }
    static ScreenshotConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ScreenshotResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    static ScreenshotResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SinglePageScrapeConfig
    ctor()
    List<Cookie> Cookies { get; set; }
    string CountryCode { get; set; }
    string ExcludedCSSElements { get; set; }
    List<string> ExcludedLineStarts { get; set; }
    List<string> ExcludedWholeLines { get; set; }
    bool Headless { get; set; }
    bool IncludeLinkedFiles { get; set; }
    string IncludedCSSElements { get; set; }
    string JavaScript { get; set; }
    bool LoadResources { get; set; }
    string Locale { get; set; }
    WebScraperOutputFormat OutputFormat { get; set; }
    string PlaywrightScript { get; set; }
    TimeSpan Timeout { get; set; }
    string Url { get; set; }
    bool UseCaptchaSolver { get; set; }
    bool UseReadability { get; set; }
    TimeSpan WaitAfter { get; set; }
    static SinglePageScrapeConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebScraper : IDisposable, IWebScraper, IWebScraperInfo
    ctor(string modelName)
    ctor(WebScraperModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(WebScraperModel model, IReadOnlyList<ModelRegion>? regions)
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
    void Dispose()
    Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = null)
    static WebScraperCapabilities GetCapabilities(WebScraperModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebScraperModel model)
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = null)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = null)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = null)
  sealed class WebScraperCapabilities : IWebScraperInfo
    ctor()
    bool SupportsFileDownload { get; init; }
    bool SupportsMultiPageScraping { get; init; }
    bool SupportsScreenshotting { get; init; }
    bool SupportsSinglePageScraping { get; init; }
  enum WebScraperModel
    Spider
    Jina
    LocalPuppeteer
    LocalNodriver
    LocalPlaywright
  static class WebScraperModelExtensions
    static string DisplayName(WebScraperModel model)
  enum WebScraperOutputFormat
    Text
    Markdown
    Html

namespace Ikon.AI.WebSearching
  interface IWebSearcher : IDisposable, IWebSearcherInfo
    abstract Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
    abstract Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
  interface IWebSearcherInfo
    bool SupportsImageSearching { get; }
  sealed class SearchConfig
    ctor()
    string CountryCode { get; set; }
    string InSiteUrl { get; set; }
    string Language { get; set; }
    int MaxResults { get; set; }
    WebSearcherOutputFormat OutputFormat { get; set; }
    string Query { get; set; }
    TimeSpan Timeout { get; set; }
    static SearchConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SearchResult
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string Mimetype { get; init; }
    string Title { get; init; }
    string Url { get; init; }
    static SearchResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebSearcher : IDisposable, IWebSearcher, IWebSearcherInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(WebSearcherModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsImageSearching { get; }
    void Dispose()
    static WebSearcherCapabilities GetCapabilities(WebSearcherModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebSearcherModel model)
    Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
    Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
  sealed class WebSearcherCapabilities : IWebSearcherInfo
    ctor()
    bool SupportsImageSearching { get; init; }
  enum WebSearcherModel
    Spider
    Jina
    Google
    GoogleImages
    Amazon
    Bing
    BingImages
    Youtube
  static class WebSearcherModelExtensions
    static string DisplayName(WebSearcherModel model)
  enum WebSearcherOutputFormat
    Text
    Markdown
    Html
