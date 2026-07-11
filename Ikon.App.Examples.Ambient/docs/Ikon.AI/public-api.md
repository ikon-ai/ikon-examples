# Ikon.AI Public API

namespace Ikon.AI
  class AIException : Exception
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class AITimeoutException : RetryableAIException
    ctor(string message)
    ctor(TimeSpan configuredTimeout, string targetName)
    TimeSpan ConfiguredTimeout { get; }
    string TargetName { get; }
  enum GovernanceAction
    Allow
    Deny
    Escalate
    Obfuscate
    Delay
  // The pending AI operation presented to the hook. Operation discriminates surface ("ai_call", "tool", "ingest"); Subject is the thing being acted on (model name, tool name, corpus name); Args are call-specific parameters; Ctx carries host-supplied identity / mission / runtime context (mission_id, agent_id, thread_id, user, tenant, time, etc.).
  sealed class GovernanceCall : IEquatable<GovernanceCall>
    ctor(string Operation, string Subject, IReadOnlyDictionary<string, object?> Args, IReadOnlyDictionary<string, object?> Ctx)
    IReadOnlyDictionary<string, object?> Args { get; init; }
    IReadOnlyDictionary<string, object?> Ctx { get; init; }
    string Operation { get; init; }
    string Subject { get; init; }
  // What happened after the operation ran (or didn't). Hooks use this in AfterAsync to close out the audit record.
  sealed class GovernanceCallResult : IEquatable<GovernanceCallResult>
    ctor(bool Failed, string Outcome, string? ErrorMessage = null)
    string? ErrorMessage { get; init; }
    bool Failed { get; init; }
    string Outcome { get; init; }
  // Thrown by AI primitives when an active IGovernanceHook returns Deny . Carries the decision id so callers can correlate the failure to the audit record.
  sealed class GovernanceDeniedException : Exception
    ctor(string decisionId, string ruleId, string policyId, string reason)
    string DecisionId { get; }
    string PolicyId { get; }
    string Reason { get; }
    string RuleId { get; }
  // Thrown by AI primitives when an active hook returns Escalate . The host runtime is expected to catch this and route to the escalation target rather than retry — the operation is paused, not failed.
  sealed class GovernanceEscalatedException : Exception
    ctor(string decisionId, string target, string reason)
    string DecisionId { get; }
    string Reason { get; }
    string Target { get; }
  // Shared invocation wrapper used by every transport that gates a call through GovernanceScope . Builds the standard Before / Deny / Escalate / invoke / After flow once so HTTP, MCP, and any future transport stay symmetric — the only thing each transport supplies is the GovernanceCall shape and the inner invocation. With no hook active the wrap is a pass-through.
  static class GovernanceInvoker
    static Task<T> RunAsync<T>(GovernanceCall call, Func<Task<T>> invoke, CancellationToken ct = default)
  // What the hook decided. The host must honour Action : Allow → invoke the operationDeny → throw GovernanceDeniedException Escalate → suspend / route to Target Obfuscate → apply the named transformDelay → wait the named duration then proceed DecisionId is the audit identifier the host can attach to any subsequent telemetry tied to this operation.
  sealed class GovernanceOutcome : IEquatable<GovernanceOutcome>
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
  // Single hook surface called by every AI-touched primitive in the Ikon platform — LLM calls (Emerge.Run<T>), agent tool dispatch (Ikon.Agent), data ingest steps — before they act. One contract, three surfaces. Host code activates a hook by entering a GovernanceScope ; downstream primitives read Current and consult the hook if it is set. The default — no scope active — is a no-op pass-through and the AI primitives behave exactly as they do without governance.
  interface IGovernanceHook
    abstract Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    abstract Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
  // Connecting to the Ikon server timed out or failed. TRANSIENT by nature — a network blip, a server restart, a flaky link — so it is retryable: the RPC layer retries with a forced reconnect, and one that exhausts those attempts still lands as retryable so Emerge's bounded retry (and a host's re-drive) get their shot. A single 15s blip killing a 40-minute codegen run (observed repeatedly on a flaky uplink) is exactly what this classification prevents.
  sealed class IkonServerConnectException : RetryableAIException
    ctor(string message)
    ctor(string message, Exception inner)
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
  class NonRetryableAIException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // Default no-op hook. Allows every call, records nothing. Lets primitives treat the hook contract as non-nullable downstream.
  sealed class NullGovernanceHook : IGovernanceHook
    ctor()
    Task AfterAsync(GovernanceCall call, GovernanceCallResult result, CancellationToken ct)
    Task<GovernanceOutcome> BeforeAsync(GovernanceCall call, CancellationToken ct)
    static NullGovernanceHook Instance
  class RegionNotSupportedException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class RetryableAIException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

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
    IAsyncEnumerable<LLMEvent> GenerateAsync(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = default)
    Task<T> GenerateObjectAsync<T>(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = default) where T : new()
    Task<string> GenerateStringAsync(IEnumerable<ValueTuple<string, object?>>? parameters = null, IEnumerable<KernelContext>? contexts = null, CancellationToken cancellationToken = default)
    T GetState<T>(string key)
    void SetState(string key, object? value)
    void StopProcessing()
    event EventHandler<string>? RenderedShader

namespace Ikon.AI.Classification
  sealed class ClassificationDetail : IEquatable<ClassificationDetail>
    ctor()
    ctor(ClassificationLabel label, string originalCategory, bool isFlagged, double score)
    bool IsFlagged { get; init; }
    ClassificationLabel Label { get; init; }
    string OriginalCategory { get; init; }
    double Score { get; init; }
  sealed class ClassificationInput : IEquatable<ClassificationInput>
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Text { get; init; }
    string Url { get; init; }
    static ClassificationInput FromMessagePart(IMessagePart messagePart)
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
  sealed class ClassificationResult : IEquatable<ClassificationResult>
    ctor()
    List<ClassificationDetail> Details { get; init; }
    bool IsFlagged { get; init; }
  class ClassificationResultException : NonRetryableAIException
    ctor(ClassificationResult classificationResult)
    ctor(ClassificationResult classificationResult, Exception inner)
    ClassificationResult ClassificationResult { get; }
  sealed class Classifier : IClassifier, IDisposable
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ClassificationModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    Task<ClassificationResult> ClassifyAsync(string text, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    // One-shot text moderation. The verbose form
    // using var classifier = new Classifier(ClassificationModel.OpenAIOmniModeration);
    // var result = await classifier.ClassifyAsync(text);
    // becomes
    // var result = await Classifier.ClassifyAsync(text);
    // Defaults to OpenAIOmniModeration (free to use, the standard moderation model). Override the model via the second parameter when the task warrants. Check result.IsFlagged and the per-label result.Details. Reach for the constructor + the instance ClassifyAsync overloads when you need to classify images or message parts ( ClassificationInput ), set a custom timeout, or classify many inputs with the same generator instance.
    static Task<ClassificationResult> ClassifyAsync(string text, ClassificationModel model = OpenAIOmniModeration, CancellationToken cancellationToken = default)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ClassificationModel model)
  class ClassifierException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  interface IClassifier : IDisposable
    abstract Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    virtual Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    virtual Task<ClassificationResult> ClassifyAsync(string text, TimeSpan? timeout = null, CancellationToken cancellationToken = default)

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
  // Creates database connections. Prefer the typed factory methods ( Trino , Postgres , Sqlite , BigQuery ) for app code — host, port, and catalog are not secrets, only the password is. Pass that password from app.Secrets:
  // DatabaseConnection.Trino(host: "trino.example.com", port: 443, catalog: "hive",
  //                      user: "ikon", password: app.Secrets["TRINO_PASSWORD"])
  // CreateAsync remains for shared pipelines that read all of host/port/user/password/etc. from environment variables or space secrets.
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
    // One-shot depth estimation from raw image bytes. The verbose form
    // using var depthEstimator = new DepthEstimator(DepthEstimatorModel.DepthAnythingV2);
    // var result = await depthEstimator.EstimateDepthAsync(new DepthEstimatorConfig
    // {
    //     Image = new DepthEstimatorConfig.InputImage { Data = imageData, MimeType = mimeType }
    // });
    // becomes
    // var result = await DepthEstimator.EstimateAsync(imageData, "image/png");
    // Defaults to DepthAnythingV2 (cheap+fast). Override the model via the third parameter when the task warrants (Marigold is slower but higher quality). The depth map image is in result.Depth (.Data / .MimeType). Reach for the constructor + EstimateDepthAsync when the image is a URL instead of bytes, or when you need the Marigold tuning fields on DepthEstimatorConfig .
    static Task<DepthEstimatorResult> EstimateAsync(byte[] imageData, string mimeType, DepthEstimatorModel model = DepthAnythingV2, CancellationToken cancellationToken = default)
    Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(DepthEstimatorModel model)
  sealed class DepthEstimatorConfig : IEquatable<DepthEstimatorConfig>
    ctor()
    int? EnsembleSize { get; init; }
    DepthEstimatorConfig.InputImage Image { get; init; }
    int? NumInferenceSteps { get; init; }
    int? ProcessingResolution { get; init; }
    TimeSpan Timeout { get; init; }
  class DepthEstimatorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum DepthEstimatorModel
    DepthAnythingV2
    Marigold
    Midas
  static class DepthEstimatorModelExtensions
    static string DisplayName(DepthEstimatorModel model)
  sealed class DepthEstimatorResult : IEquatable<DepthEstimatorResult>
    ctor()
    DepthEstimatorResult.OutputImage Depth { get; init; }
  interface IDepthEstimator : IDisposable
    abstract Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = default)
  sealed class DepthEstimatorConfig.InputImage : IEquatable<DepthEstimatorConfig.InputImage>
    ctor()
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  sealed class DepthEstimatorResult.OutputImage : IEquatable<DepthEstimatorResult.OutputImage>
    ctor()
    byte[] Data { get; init; }
    int Height { get; init; }
    string MimeType { get; init; }
    int Width { get; init; }

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
    // One-shot embedding generation. The verbose form
    // using var embeddingGenerator = new EmbeddingGenerator(EmbeddingModel.OpenAI3Small);
    // var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(texts, EmbeddingType.Generic);
    // becomes
    // var embeddings = await EmbeddingGenerator.EmbedAsync(texts);
    // Defaults to OpenAI3Small (cheap+fast) and Generic . Override the model via the second parameter when the task warrants; pass an explicit EmbeddingType when embedding documents and queries for asymmetric retrieval. Returns one float[] vector per input, in input order. Reach for the constructor + GenerateEmbeddingsAsync when you need batching control (maxInputCount), a custom timeout, or the generator's MaxInputCount / EmbeddingVectorSize properties.
    static Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingModel model = OpenAI3Small, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    static EmbeddingGeneratorCapabilities GetCapabilities(EmbeddingModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(EmbeddingModel model)
  sealed class EmbeddingGeneratorCapabilities
    ctor()
    int EmbeddingVectorSize { get; init; }
    int MaxInputCount { get; init; }
  class EmbeddingGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class EmbeddingItem
    ctor(string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, string embedding)
    string Context { get; init; }
    string Embedding { get; init; }
    float[] EmbeddingArray { get; }
    EmbeddingEncoding Encoding { get; init; }
    EmbeddingModel Model { get; init; }
    EmbeddingType Type { get; init; }
    static Task<EmbeddingItem> CreateAsync(string input, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, CancellationToken cancellationToken = default)
    static Task<EmbeddingItem> CreateAsync(float[] embedding, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding)
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
    abstract Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
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
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
    // One-shot PDF conversion from raw file bytes. The verbose form
    // using var fileConverter = new FileConverter(FileConverterModel.ConvertApi);
    // var pdf = await fileConverter.ConvertToPdfAsync(new FileConverterConfig { Data = data, FileName = fileName });
    // becomes
    // var pdf = await FileConverter.ConvertToPdfAsync(data, fileName);
    // Defaults to ConvertApi (the only conversion model). fileName must carry the source extension (e.g. report.docx) — it determines the input format. The converted PDF is in pdf.Data. Reach for the constructor + ConvertToPdfAsync when the source is a URL or AssetUri instead of bytes, or when you need a custom timeout.
    static Task<ConvertedFile> ConvertToPdfAsync(byte[] data, string fileName, FileConverterModel model = ConvertApi, CancellationToken cancellationToken = default)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(FileConverterModel model)
  sealed class FileConverterConfig
    ctor()
    AssetUri? AssetUri { get; set; }
    byte[]? Data { get; set; }
    string FileName { get; set; }
    TimeSpan Timeout { get; set; }
    string? Url { get; set; }
  class FileConverterException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum FileConverterModel
    ConvertApi
  static class FileConverterModelExtensions
    static string DisplayName(FileConverterModel model)
  interface IFileConverter : IDisposable
    abstract Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)

namespace Ikon.AI.ImageGeneration
  interface IImageGenerator : IDisposable
    abstract Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = default)
  enum ImageBackground
    Auto
    Opaque
    Transparent
  sealed class ImageGenerator : IDisposable, IImageGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // One-shot image generation. The verbose form
    // using var generator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);
    // var results = await generator.GenerateImageAsync(new ImageGeneratorConfig { Prompt = prompt });
    // var image = results.FirstOrDefault();
    // becomes
    // var image = await ImageGenerator.GenerateAsync(prompt);
    // Defaults to Gemini25FlashImage (cheap+fast). Override the model via the second parameter when the task warrants. Returns null if the model produces no results — caller should null-check before using .Data / .MimeType. Reach for the constructor + GenerateImageAsync when you need batch generation, custom width/height, an ImageBackground override, input images, or any other ImageGeneratorConfig field beyond the prompt.
    static Task<ImageGeneratorResult?> GenerateAsync(string prompt, ImageGeneratorModel model = Gemini25FlashImage, CancellationToken cancellationToken = default)
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageGeneratorModel model)
  sealed class ImageGeneratorConfig : IEquatable<ImageGeneratorConfig>
    ctor()
    ImageBackground Background { get; init; }
    int Count { get; init; }
    int Height { get; init; }
    string ImageSize { get; init; }
    List<InputImage> InputImages { get; init; }
    string NegativePrompt { get; init; }
    string Prompt { get; init; }
    ImageQuality Quality { get; init; }
    ImageResultDelivery ResultDelivery { get; init; }
    SafetyLevel SafetyLevel { get; init; }
    string SearchPrompt { get; init; }
    int Seed { get; init; }
    int Steps { get; init; }
    string Style { get; init; }
    TimeSpan Timeout { get; init; }
    bool UpsamplePrompt { get; init; }
    int Width { get; init; }
  class ImageGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
  sealed class ImageGeneratorResult : IEquatable<ImageGeneratorResult>
    ctor()
    byte[] Data { get; init; }
    int Height { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
    int Width { get; init; }
  enum ImageQuality
    Auto
    Low
    Medium
    High
  enum ImageResultDelivery
    Data
    Url
  sealed class InputImage : IEquatable<InputImage>
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[] Data { get; init; }
    double? MaskDilution { get; init; }
    string MimeType { get; init; }
    double? Strength { get; init; }
    InputImageType Type { get; init; }
    string? Url { get; init; }
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
  sealed class ImageSegmenterConfig.BoxPrompt : IEquatable<ImageSegmenterConfig.BoxPrompt>
    ctor()
    int? ObjectId { get; init; }
    double XMax { get; init; }
    double XMin { get; init; }
    double YMax { get; init; }
    double YMin { get; init; }
  interface IImageSegmenter : IDisposable
    abstract Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed class ImageSegmenter : IDisposable, IImageSegmenter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageSegmenterModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageSegmenterModel model)
    // One-shot text-prompted segmentation from raw image bytes. The verbose form
    // using var segmenter = new ImageSegmenter(ImageSegmenterModel.Sam31);
    // var result = await segmenter.SegmentImageAsync(new ImageSegmenterConfig
    // {
    //     Image = new ImageSegmenterConfig.InputImage { Data = imageData, MimeType = mimeType },
    //     Prompt = prompt
    // });
    // becomes
    // var result = await ImageSegmenter.SegmentAsync(imageData, "image/png", "person");
    // Defaults to Sam31 (the latest SAM revision at the same price as SAM 3). Override the model via the fourth parameter when the task warrants. Each detected object is in result.Segments with its mask image, score, and bounding box. Reach for the constructor + SegmentImageAsync when the image is a URL instead of bytes, or when you need point/box prompts, multiple masks per object, or any other ImageSegmenterConfig field.
    static Task<ImageSegmenterResult> SegmentAsync(byte[] imageData, string mimeType, string prompt, ImageSegmenterModel model = Sam31, CancellationToken cancellationToken = default)
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed class ImageSegmenterConfig : IEquatable<ImageSegmenterConfig>
    ctor()
    List<ImageSegmenterConfig.BoxPrompt> BoxPrompts { get; init; }
    ImageSegmenterConfig.InputImage Image { get; init; }
    int MaxMasks { get; init; }
    List<ImageSegmenterConfig.PointPrompt> PointPrompts { get; init; }
    string? Prompt { get; init; }
    bool ReturnMultipleMasks { get; init; }
    TimeSpan Timeout { get; init; }
  class ImageSegmenterException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum ImageSegmenterModel
    Sam3
    Sam31
  static class ImageSegmenterModelExtensions
    static string DisplayName(ImageSegmenterModel model)
  sealed class ImageSegmenterResult : IEquatable<ImageSegmenterResult>
    ctor()
    ImageSegmenterResult.OutputImage? Preview { get; init; }
    List<ImageSegmenterResult.Segment> Segments { get; init; }
  sealed class ImageSegmenterConfig.InputImage : IEquatable<ImageSegmenterConfig.InputImage>
    ctor()
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  sealed class ImageSegmenterResult.OutputImage : IEquatable<ImageSegmenterResult.OutputImage>
    ctor()
    byte[] Data { get; init; }
    int Height { get; init; }
    string MimeType { get; init; }
    int Width { get; init; }
  sealed class ImageSegmenterConfig.PointPrompt : IEquatable<ImageSegmenterConfig.PointPrompt>
    ctor()
    bool IsBackground { get; init; }
    int? ObjectId { get; init; }
    double X { get; init; }
    double Y { get; init; }
  sealed class ImageSegmenterResult.Segment : IEquatable<ImageSegmenterResult.Segment>
    ctor()
    List<double> Box { get; init; }
    ImageSegmenterResult.OutputImage Mask { get; init; }
    double? Score { get; init; }

namespace Ikon.AI.Kernel
  static class AsyncEnumerableExtensions
    static Task<T1[]> AsArrayAsync<T1>(IAsyncEnumerable<LLMEvent> source)
    static Task<T1> AsFirstAsync<T1>(IAsyncEnumerable<LLMEvent> source)
    static Task<string> AsStringAsync(IAsyncEnumerable<LLMEvent> source)
    static IAsyncEnumerable<LLMEvent> WithCitationsAsync(IAsyncEnumerable<LLMEvent> source, IdMapper idMapper)
    static IAsyncEnumerable<LLMEvent> WithParsedTagsAsync(IAsyncEnumerable<LLMEvent> source, List<string>? tagWhitelist = null, List<string>? tagBlacklist = null)
    static IAsyncEnumerable<LLMEvent> WithReasoningFromTagAsync(IAsyncEnumerable<LLMEvent> source, string reasoningTagName)
    static IAsyncEnumerable<LLMEvent> WithThrottlingAsync(IAsyncEnumerable<LLMEvent> source, int charsPerSecond, int charsPerUpdate, CancellationToken cancellationToken = default)
    static IAsyncEnumerable<LLMEvent> WithWindowedProcessingAsync(IAsyncEnumerable<LLMEvent> source, Func<string, List<LLMEvent>, Task<ValueTuple<bool, List<LLMEvent>>>> processAsync, int windowSize = 0, int windowOverlap = 0)
  sealed class LLMEvent.AudioDelta : LLMEvent, IEquatable<LLMEvent.AudioDelta>
    ctor(AudioChunk Audio)
    AudioChunk Audio { get; init; }
  sealed class LLMEvent.AudioId : LLMEvent, IEquatable<LLMEvent.AudioId>
    ctor(string Id)
    string Id { get; init; }
  struct AudioIdPart : IMessagePart
    ctor(string id)
    string Id { get; }
    MessagePartType Type { get; }
  struct AudioPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  sealed class LLMEvent.AudioTranscript : LLMEvent, IEquatable<LLMEvent.AudioTranscript>
    ctor(string Transcript)
    string Transcript { get; init; }
  class BinaryDataContainer
    ctor(byte[] data, string mimeType)
    byte[] Data { get; }
    string MimeType { get; }
  sealed class LLMEvent.Citation : LLMEvent, IEquatable<LLMEvent.Citation>
    ctor(string OriginalId, string MappedId, int ReferStartIndex, int ReferEndIndex, int PositionIndex)
    string MappedId { get; init; }
    string OriginalId { get; init; }
    int PositionIndex { get; init; }
    int ReferEndIndex { get; init; }
    int ReferStartIndex { get; init; }
  sealed class LLMEvent.ContentFiltered : LLMEvent, IEquatable<LLMEvent.ContentFiltered>
    ctor(ClassificationResult Classification)
    ClassificationResult Classification { get; init; }
  sealed class LLMEvent.FinalModelMessage : LLMEvent, IEquatable<LLMEvent.FinalModelMessage>
    ctor(string Text)
    string Text { get; init; }
  sealed class LLMEvent.FinalText : LLMEvent, IEquatable<LLMEvent.FinalText>
    ctor(string Text)
    string Text { get; init; }
  sealed class LLMEvent.Finished : LLMEvent, IEquatable<LLMEvent.Finished>
    ctor(string Reason)
    string Reason { get; init; }
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
    ctor(FunctionCall functionCall, LLMEvent[] events, object result)
    LLMEvent[] Events { get; }
    FunctionCall FunctionCall { get; }
    object Result { get; }
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
  struct KernelContext : IEquatable<KernelContext>
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    // When set, providers that support server-side context editing (Anthropic context-management beta) clear OLD tool results once the request's input exceeds this many tokens — after prompt-cache lookup, so cached prefixes survive. The single biggest context sink in long tool-using loops is superseded tool results being re-sent every round; server-side clearing removes them without the cache-busting a client-side history rewrite causes. Null = off. Providers without support ignore it.
    int? ClearToolResultsAfterInputTokens { get; init; }
    // Tool names whose results are NEVER cleared by ClearToolResultsAfterInputTokens (semantic anchors like verdicts).
    IReadOnlyList<string>? ClearToolResultsExcludedTools { get; init; }
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
    IAsyncEnumerable<LLMEvent> GenerateAsync(ILLM llm, CancellationToken cancellationToken = default)
    KernelContext KeepMessagesMax(int count)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
  // One event in the typed stream produced by GenerateAsync and its combinators. Consume the stream by switching on the concrete case: TextDelta for incremental text, ToolCallRequested when the model asks for a tool, ToolResult for a tool's output, Usage and Finished for end-of-generation accounting, and so on. Events not relevant to a consumer should be passed through unchanged so downstream consumers still see them.
  abstract class LLMEvent : IEquatable<LLMEvent>
    // Name of the pipeline stage that produced this event (e.g. "generate", "generate.reasoning", "Shader.Output.AfterPass"). Combinators re-tag events they transform so the origin of each event stays visible.
    string Source { get; init; }
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
  struct PdfPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  struct PdfUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  sealed class LLMEvent.Reasoning : LLMEvent, IEquatable<LLMEvent.Reasoning>
    ctor(string Text)
    string Text { get; init; }
  enum ReasoningEffort
    None
    Minimal
    Low
    Medium
    High
  sealed class LLMEvent.Tag : LLMEvent, IEquatable<LLMEvent.Tag>
    ctor(string Name, string Content, IReadOnlyDictionary<string, string>? Attributes)
    IReadOnlyDictionary<string, string>? Attributes { get; init; }
    string Content { get; init; }
    string Name { get; init; }
  sealed class LLMEvent.TextDelta : LLMEvent, IEquatable<LLMEvent.TextDelta>
    ctor(string Text)
    string Text { get; init; }
  struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  sealed class LLMEvent.ToolCallRequested : LLMEvent, IEquatable<LLMEvent.ToolCallRequested>
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  sealed class LLMEvent.ToolPlan : LLMEvent, IEquatable<LLMEvent.ToolPlan>
    ctor(string Text)
    string Text { get; init; }
  sealed class LLMEvent.ToolResult : LLMEvent, IEquatable<LLMEvent.ToolResult>
    ctor(string functionName, object? value)
    ctor(string functionName, object? value, string? valueType)
    string FunctionName { get; }
    object? Value { get; }
    string? ValueType { get; }
  sealed class LLMEvent.Usage : LLMEvent, IEquatable<LLMEvent.Usage>
    ctor(int InputTokens, int CachedInputTokens, int CacheCreationInputTokens, int OutputTokens)
    int CacheCreationInputTokens { get; init; }
    int CachedInputTokens { get; init; }
    int InputTokens { get; init; }
    int OutputTokens { get; init; }
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
  // Public seam over the provider-facing JSON schema generator. This is the exact projection every LLM provider applies when it ships a Function to the model (Anthropic input_schema, OpenAI parameters, …). Callers that need to display, persist, or compare "the schema the LLM will see" should use this instead of re-deriving their own — any drift between a home-grown projection and the wire is a bug this seam exists to prevent.
  static class FunctionSchema
    // Projects the function's parameter list into its provider JSON schema: an object schema with type/properties/required, including parameter descriptions and allowed-value enums.
    static string ToJson(Function function)
  interface ILLM : IDisposable, ILLMInfo
    abstract IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext context, CancellationToken cancellationToken = default)
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
    IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext context, CancellationToken cancellationToken = default)
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
  class LLMMaxOutputTokensException : NonRetryableLLMException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
  class NonRetryableLLMException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  class RetryableLLMException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.MeshGeneration
  interface IMeshGenerator : IDisposable, IMeshGeneratorInfo
    abstract Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IMeshGeneratorInfo
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
  sealed class MeshGeneratorConfig.InputImage : IEquatable<MeshGeneratorConfig.InputImage>
    ctor()
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  sealed class MeshGenerator : IDisposable, IMeshGenerator, IMeshGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MeshGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
    void Dispose()
    // One-shot text-to-mesh. The verbose form
    // using var generator = new MeshGenerator(MeshGeneratorModel.Meshy6);
    // var result = await generator.GenerateMeshAsync(new MeshGeneratorConfig { Prompt = prompt });
    // becomes
    // var mesh = await MeshGenerator.GenerateAsync(prompt);
    // Defaults to Meshy6 (the current Meshy generation at the same per-credit price as Meshy 5). Override the model via the second parameter when the task warrants. Returns signed download URLs per format (.GlbUrl, .FbxUrl, …) that expire roughly three days after generation — download promptly. Reach for the constructor + GenerateMeshAsync when you need image-to-mesh (input images), PBR textures, polycount/topology control, or any other MeshGeneratorConfig field beyond the prompt.
    static Task<MeshGeneratorResult> GenerateAsync(string prompt, MeshGeneratorModel model = Meshy6, CancellationToken cancellationToken = default)
    Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = default)
    static MeshGeneratorCapabilities GetCapabilities(MeshGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(MeshGeneratorModel model)
  sealed class MeshGeneratorCapabilities : IMeshGeneratorInfo
    ctor()
    int MaxInputImages { get; init; }
    bool SupportsImageToMesh { get; init; }
    bool SupportsLowPoly { get; init; }
    bool SupportsPbr { get; init; }
    bool SupportsTextToMesh { get; init; }
  sealed class MeshGeneratorConfig : IEquatable<MeshGeneratorConfig>
    ctor()
    bool EnablePbr { get; init; }
    List<MeshGeneratorConfig.InputImage> InputImages { get; init; }
    MeshGeneratorMeshStyle MeshStyle { get; init; }
    string? Prompt { get; init; }
    bool Remesh { get; init; }
    int TargetPolycount { get; init; }
    bool Texture { get; init; }
    string? TexturePrompt { get; init; }
    TimeSpan Timeout { get; init; }
    MeshGeneratorTopology Topology { get; init; }
  class MeshGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum MeshGeneratorMeshStyle
    Standard
    LowPoly
  enum MeshGeneratorModel
    Meshy5
    Meshy6
  static class MeshGeneratorModelExtensions
    static string DisplayName(MeshGeneratorModel model)
  // Result of a mesh generation. The URLs are signed and expire roughly three days after generation, so download the model files promptly.
  sealed class MeshGeneratorResult : IEquatable<MeshGeneratorResult>
    ctor()
    DateTimeOffset? ExpiresAt { get; init; }
    string? FbxUrl { get; init; }
    string? GlbUrl { get; init; }
    string? MtlUrl { get; init; }
    string? ObjUrl { get; init; }
    string? ThumbnailUrl { get; init; }
    string? UsdzUrl { get; init; }
  enum MeshGeneratorTopology
    Triangle
    Quad

namespace Ikon.AI.MusicGeneration
  interface IMusicGenerator : IDisposable, IMusicGeneratorInfo
    // Channel count of the PCM samples produced by GenerateMusicAsync .
    int ChannelCount { get; }
    // Sample rate of the PCM samples produced by GenerateMusicAsync .
    int SampleRate { get; }
    // Streams the generated music as PCM AudioChunk chunks as they are produced. Only supported when SupportsStreaming is true; other models throw a MusicGeneratorException . Use GenerateMusicFileAsync for a buffered, encoded audio file instead.
    abstract IAsyncEnumerable<AudioChunk> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    // Generates the music and returns it as a single buffered, encoded audio file. Supported by all models, including those that cannot stream.
    abstract Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IMusicGeneratorInfo
    // Whether DurationSeconds controls the length of the output. When false the model ignores it: it emits a fixed-length clip (e.g. Lyria 2 is always ~30s) or, for audio-to-audio editing, the output length follows the input clip.
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    // Whether the model can stream generated audio as it is produced via GenerateMusicAsync . Models without streaming support only expose the buffered GenerateMusicFileAsync result.
    bool SupportsStreaming { get; }
  // A reference clip fed into a prompt-driven music edit. The model preserves the timing and structure of this audio while the prompt re-styles it (timbre, instrumentation, mood). Mirrors the image-to-image InputImage shape used by the image generator.
  sealed class InputAudio : IEquatable<InputAudio>
    ctor()
    byte[] Data { get; init; }
    // End of the region to edit, in seconds. null means to the end.
    double? EndSeconds { get; init; }
    string MimeType { get; init; }
    // Start of the region to edit, in seconds. null means from the beginning.
    double? StartSeconds { get; init; }
    // How strongly the output should adhere to this reference, in [0, 1]. Higher keeps the original melody and timing closer. null defaults to strong adherence.
    double? Strength { get; init; }
  sealed class MusicGenerator : IDisposable, IMusicGenerator, IMusicGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MusicGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    bool SupportsStreaming { get; }
    void Dispose()
    // One-shot music generation. The verbose form
    // using var generator = new MusicGenerator(MusicGeneratorModel.ElevenLabsMusicV2);
    // var result = await generator.GenerateMusicFileAsync(new MusicGeneratorConfig { Prompt = prompt });
    // becomes
    // var music = await MusicGenerator.GenerateAsync(prompt);
    // Defaults to ElevenLabsMusicV2 (cheap+fast, supports duration control and editing). Override the model via the second parameter when the task warrants. Returns a buffered, encoded audio file (.AudioData / .ContentType / .DurationSeconds). Reach for the constructor + GenerateMusicFileAsync when you need a target duration, input audio (prompt-driven editing), seed, or any other MusicGeneratorConfig field beyond the prompt; use GenerateMusicAsync for streaming PCM chunks.
    static Task<MusicGeneratorResult> GenerateAsync(string prompt, MusicGeneratorModel model = ElevenLabsMusicV2, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    static MusicGeneratorCapabilities GetCapabilities(MusicGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(MusicGeneratorModel model)
  sealed class MusicGeneratorCapabilities : IMusicGeneratorInfo
    ctor()
    bool SupportsDurationControl { get; init; }
    bool SupportsEditing { get; init; }
    bool SupportsStreaming { get; init; }
  // Configuration for prompt-driven music generation and editing. With an empty InputAudios the model generates from the prompt alone. With one or more InputAudios it performs audio-to-audio editing: the prompt re-styles the reference clips while their timing and structure are preserved.
  sealed class MusicGeneratorConfig : IEquatable<MusicGeneratorConfig>
    ctor()
    // Target length in seconds (clamped to the model's supported range). When editing, set this to the source clip's length so the output keeps the original timing.
    double? DurationSeconds { get; init; }
    bool ForceInstrumental { get; init; }
    List<InputAudio> InputAudios { get; init; }
    string Prompt { get; init; }
    int Seed { get; init; }
    TimeSpan Timeout { get; init; }
  class MusicGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum MusicGeneratorModel
    ElevenLabsMusicV2
    FalStableAudio
    FalLyria2
  static class MusicGeneratorModelExtensions
    static string DisplayName(MusicGeneratorModel model)
  sealed class MusicGeneratorResult : IEquatable<MusicGeneratorResult>
    ctor()
    byte[] AudioData { get; init; }
    string ContentType { get; init; }
    double DurationSeconds { get; init; }

namespace Ikon.AI.OCR
  enum DocumentType
    General
  interface IOCR : IDisposable, IOCRInfo
    abstract Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = default)
    abstract IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = default)
  interface IOCRInfo
    int MaxPagesSupported { get; }
  sealed class OCR : IDisposable, IOCR, IOCRInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxPagesSupported { get; }
    // One-shot document OCR from raw file bytes (image or PDF). The verbose form
    // using var ocr = new OCR(OCRModel.AzureDocumentIntelligence);
    // var result = await ocr.AnalyzeDocumentAsync(new OCRConfig { Data = data });
    // becomes
    // var result = await OCR.AnalyzeAsync(data);
    // Defaults to AzureDocumentIntelligence (cheap+robust general document OCR). Override the model via the second parameter when the task warrants. Read the extracted text from result.Text; result.Paragraphs and result.Pages carry the structure. Reach for the constructor + AnalyzeDocumentAsync when the document is a URL or AssetUri instead of bytes, or when you need page selection, word-level bounding boxes, or any other OCRConfig field; use AnalyzeDocumentStreamingAsync for page-by-page streaming.
    static Task<OCRResult> AnalyzeAsync(byte[] data, OCRModel model = AzureDocumentIntelligence, CancellationToken cancellationToken = default)
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = default)
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
  class OCRException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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

namespace Ikon.AI.Reranking
  interface IReranker : IDisposable
    abstract Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
  sealed class RerankItem : IEquatable<RerankItem>
    ctor()
    int Index { get; init; }
    double Score { get; init; }
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
    static IReadOnlyList<ModelRegion> GetSupportedRegions(RerankModel model)
    Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    // One-shot reranking. The verbose form
    // using var reranker = new Reranker(RerankModel.CohereRerank4Fast);
    // var items = await reranker.RerankAsync(documents, query);
    // becomes
    // var items = await Reranker.RerankAsync(documents, query);
    // Defaults to CohereRerank4Fast (cheap+fast). Override the model via the third parameter when the task warrants; pass topN to cap how many items are returned (0 returns all). Each RerankItem carries the document's original .Index and its relevance .Score, ordered most relevant first. Reach for the constructor + the instance RerankAsync when you need a custom timeout or rerank many queries against the same generator instance.
    static Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, RerankModel model = CohereRerank4Fast, int topN = 0, CancellationToken cancellationToken = default)
  class RerankerException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

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
  class IdMapperException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
    Task<ContentLink[]> ExpandAsync(ContentLink[] links)
    Task<ContentLink[]> ExpandAsync(ContentLink link)
    Task<Content?> GetContentAsync(ContentLink link)
    Retriever.ContentMetadata? GetContentMetadata(string metadataId)
    Task<string> GetContentsAsync(string query, Retriever.GetContentsOptions options)
    ContentLink? Ignore(ContentLink link, string detail)
    Task InitializeAsync(string dataDirectory, EmbeddingModel embeddingModel = OpenAI3Small)
    Task InitializeAsync(IReadOnlyList<AssetUri> assetUris, EmbeddingModel embeddingModel = OpenAI3Small)
    ContentLink[] Prefer(ContentLink link, string detail)
    ContentLink[] Prefer(ContentLink[] links, string detail)
    Task<ContentLink[]> SearchAsync(string query, int maxLinks = 25, float searchThreshold = 0.1)
    Task<Retriever.Event[]> SearchEventsAsync(string startUtcTimestamp, string endUtcTimestamp, int maxResults = 100)
    Task<Retriever.Event[]> SearchEventsAsync(string startUtcTimestamp, string endUtcTimestamp, string searchString, int maxResults = 100)
    Task<KeywordSearchResult[]> SearchKeywordsAsync(string searchString, int maxResults = 100)
    Task StopAsync()
    Task WaitForLoadingToEndAsync()

namespace Ikon.AI.SoundEffectGeneration
  interface ISoundEffectGenerator : IDisposable, ISoundEffectGeneratorInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    abstract IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
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
    // One-shot sound effect generation. The verbose form
    // using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);
    // var result = await generator.GenerateSoundEffectFileAsync(new SoundEffectGeneratorConfig { Prompt = prompt });
    // becomes
    // var effect = await SoundEffectGenerator.GenerateAsync(prompt);
    // Defaults to ElevenLabsV2 (the only sound effect model). Returns a buffered WAV file (.AudioData / .ContentType / .DurationSeconds). Reach for the constructor + GenerateSoundEffectFileAsync when you need a target duration, looping, prompt influence, or any other SoundEffectGeneratorConfig field beyond the prompt; use GenerateSoundEffectAsync for streaming PCM chunks.
    static Task<SoundEffectFileResult> GenerateAsync(string prompt, SoundEffectGeneratorModel model = ElevenLabsV2, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<SoundEffectFileResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    static SoundEffectGeneratorCapabilities GetCapabilities(SoundEffectGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SoundEffectGeneratorModel model)
  sealed class SoundEffectGeneratorCapabilities : ISoundEffectGeneratorInfo
    ctor()
    bool SupportsLooping { get; init; }
  sealed class SoundEffectGeneratorConfig : IEquatable<SoundEffectGeneratorConfig>
    ctor()
    double? DurationSeconds { get; init; }
    bool Loop { get; init; }
    string Prompt { get; init; }
    double PromptInfluence { get; init; }
    TimeSpan Timeout { get; init; }
  class SoundEffectGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
    abstract IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
  sealed class SpeechGenerator : IDisposable, ISpeechGenerator
    ctor(string modelName)
    ctor(SpeechGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    void Dispose()
    // One-shot text-to-speech. The verbose form
    // using var generator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
    // await foreach (var chunk in generator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = text }))
    // {
    //     // collect chunk.Samples
    // }
    // becomes
    // var audio = await SpeechGenerator.GenerateAsync(text);
    // Defaults to ElevenFlash25 (cheap+fast). Override the model via the second parameter when the task warrants; pass voice to pick a voice (the model's default voice otherwise). The streamed chunks are concatenated into a single PCM AudioChunk (.Samples / .SampleRate / .ChannelCount). Returns null if the model produces no audio — caller should null-check before using the samples. Reach for the constructor + GenerateSpeechAsync when you need chunk-by-chunk streaming playback while generation runs, or any other SpeechGeneratorConfig field beyond text+voice (language, instructions, speed).
    static Task<AudioChunk?> GenerateAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechGeneratorModel model)
    static IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> GetVoiceIdsByModel()
  sealed class SpeechGeneratorConfig : IEquatable<SpeechGeneratorConfig>
    ctor()
    string Instructions { get; init; }
    string Language { get; init; }
    string Speed { get; init; }
    string Text { get; init; }
    TimeSpan Timeout { get; init; }
    string VoiceId { get; init; }
  class SpeechGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
  sealed class AnalyzePronunciationConfig : IEquatable<AnalyzePronunciationConfig>
    ctor()
    int ChannelCount { get; init; }
    string Language { get; init; }
    string ReferenceText { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    TimeSpan Timeout { get; init; }
  sealed class Pronunciation.Break : IEquatable<Pronunciation.Break>
    ctor()
    int BreakLength { get; init; }
    List<string> ErrorTypes { get; init; }
    Pronunciation.MissingBreak MissingBreak { get; init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get; init; }
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    TimeSpan MaxSpeechDuration { get; set; }
    SpeechRecognizerAdapter.Mode Mode { get; set; }
    TimeSpan RecognitionInterval { get; set; }
    TimeSpan RequestTimeout { get; set; }
    TimeSpan SilenceDuration { get; set; }
    float SilenceThreshold { get; set; }
  sealed class Pronunciation.Feedback : IEquatable<Pronunciation.Feedback>
    ctor()
    Pronunciation.Prosody Prosody { get; init; }
  interface ISpeechRecognizer : IDisposable, ISpeechRecognizerInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    abstract Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    abstract Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    abstract IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  interface ISpeechRecognizerInfo
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
  sealed class Pronunciation.Intonation : IEquatable<Pronunciation.Intonation>
    ctor()
    List<string> ErrorTypes { get; init; }
    Pronunciation.Monotone Monotone { get; init; }
  sealed class Pronunciation.MissingBreak : IEquatable<Pronunciation.MissingBreak>
    ctor()
    double Confidence { get; init; }
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
  sealed class Pronunciation.Monotone : IEquatable<Pronunciation.Monotone>
    ctor()
    double SyllablePitchDeltaConfidence { get; init; }
  sealed class Pronunciation.NBest : IEquatable<Pronunciation.NBest>
    ctor()
    double Confidence { get; init; }
    string Display { get; init; }
    string ITN { get; init; }
    string Lexical { get; init; }
    string MaskedITN { get; init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Word> Words { get; init; }
  sealed class Pronunciation.Phoneme : IEquatable<Pronunciation.Phoneme>
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed class Pronunciation.PhonemePronunciationAssessment : IEquatable<Pronunciation.PhonemePronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
  static class Pronunciation
  sealed class Pronunciation.PronunciationAssessment : IEquatable<Pronunciation.PronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
    double CompletenessScore { get; init; }
    double FluencyScore { get; init; }
    double PronScore { get; init; }
    double ProsodyScore { get; init; }
  sealed class Pronunciation.Prosody : IEquatable<Pronunciation.Prosody>
    ctor()
    Pronunciation.Break Break { get; init; }
    Pronunciation.Intonation Intonation { get; init; }
  sealed class RecognizeContinuousSpeechConfig : IEquatable<RecognizeContinuousSpeechConfig>
    ctor()
    string[] CandidateLanguages { get; init; }
    int ChannelCount { get; init; }
    string Language { get; init; }
    int SampleRate { get; init; }
  sealed class RecognizeSpeechConfig : IEquatable<RecognizeSpeechConfig>
    ctor()
    int ChannelCount { get; init; }
    string Language { get; init; }
    string Prompt { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    double Temperature { get; init; }
    TimeSpan Timeout { get; init; }
  sealed class Pronunciation.Result : IEquatable<Pronunciation.Result>
    ctor()
    int Channel { get; init; }
    string DisplayText { get; init; }
    long Duration { get; init; }
    string Id { get; init; }
    List<Pronunciation.NBest> NBest { get; init; }
    long Offset { get; init; }
    string RecognitionStatus { get; init; }
    double SNR { get; init; }
  sealed class SpeechRecognizer : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(SpeechRecognizerModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    static SpeechRecognizerCapabilities GetCapabilities(SpeechRecognizerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechRecognizerModel model)
    // One-shot batch transcription. The verbose form
    // using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
    // var text = await recognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
    // {
    //     Samples = samples,
    //     SampleRate = 16000,
    //     ChannelCount = 1
    // });
    // becomes
    // var text = await SpeechRecognizer.RecognizeAsync(samples, 16000);
    // Defaults to WhisperLarge3Turbo (cheap+fast). Override the model via the third parameter when the task warrants. Returns the recognized text (empty when nothing was recognized). Reach for the constructor + RecognizeBatchSpeechAsync when you need PCM16 byte input, a language hint, a prompt, or any other RecognizeSpeechConfig field; use RecognizeContinuousSpeechAsync for streaming recognition.
    static Task<string> RecognizeAsync(float[] samples, int sampleRate, SpeechRecognizerModel model = WhisperLarge3Turbo, int channelCount = 1, CancellationToken cancellationToken = default)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerAdapter : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(ISpeechRecognizer speechRecognizer, SpeechRecognizerAdapter.Config? config = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerCapabilities : ISpeechRecognizerInfo
    ctor()
    bool SupportsBatchRecognition { get; init; }
    bool SupportsContinuousRecognition { get; init; }
    bool SupportsPronunciationAnalysis { get; init; }
  class SpeechRecognizerException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
  sealed class Pronunciation.Syllable : IEquatable<Pronunciation.Syllable>
    ctor()
    long Duration { get; init; }
    string Grapheme { get; init; }
    long Offset { get; init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed class Pronunciation.SyllablePronunciationAssessment : IEquatable<Pronunciation.SyllablePronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
  sealed class Pronunciation.UnexpectedBreak : IEquatable<Pronunciation.UnexpectedBreak>
    ctor()
    double Confidence { get; init; }
  sealed class Pronunciation.Word : IEquatable<Pronunciation.Word>
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    List<Pronunciation.Phoneme> Phonemes { get; init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Syllable> Syllables { get; init; }
    string Text { get; init; }
  sealed class Pronunciation.WordPronunciationAssessment : IEquatable<Pronunciation.WordPronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
    string ErrorType { get; init; }
    Pronunciation.Feedback Feedback { get; init; }

namespace Ikon.AI.Storage
  class KeywordIndex
    ctor()
    Task AddAsync(string word, string link)
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
    Task<int> GetDataItemCountAsync(string collectionName)
    Task RemoveAsync(string collectionName, IEnumerable<string> tags)
    Task<List<Result<object>>> SearchAsync(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<object>>> SearchAsync(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<int> SetAsync(string collectionName, int? key, string text, object value, IEnumerable<string>? tags = null)
    Task<int> SetAsync(string collectionName, int? key, float[] vector, object value, IEnumerable<string>? tags = null)

namespace Ikon.AI.Utils
  static class ImageUtils
    static byte[] ConvertAlphaMaskToBlackWhiteMask(byte[] maskData)
    static byte[] ConvertBlackWhiteMaskToAlphaMask(byte[] maskData)
    // Re-encodes an image as JPEG with both dimensions capped at maxDimension (aspect preserved). Returns the original bytes untouched when the image already fits AND is at most maxBytes — small screenshots pass through without a decode cost. Intended for images going into LLM context, where anything above ~1568px is downscaled by the provider anyway and only costs tokens.
    static ValueTuple<byte[], string, int, int> EncodeJpegCapped(byte[] source, string sourceMimeType, int maxDimension = 1568, int quality = 70, int maxBytes = 204800)
    static ValueTuple<int, int> GetImageDimensions(byte[] buffer)
    static byte[] InvertMask(byte[] maskData)

namespace Ikon.AI.VideoEnhancement
  interface IVideoEnhancer : IDisposable
    abstract Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
  sealed class VideoEnhancer : IDisposable, IVideoEnhancer
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoEnhancerModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoEnhancerModel model)
  sealed class VideoEnhancerConfig : IEquatable<VideoEnhancerConfig>
    ctor()
    int? EndFrame { get; init; }
    string? MimeType { get; init; }
    int? StartFrame { get; init; }
    int? TargetFps { get; init; }
    TimeSpan Timeout { get; init; }
    byte[]? VideoData { get; init; }
    string? VideoUrl { get; init; }
  class VideoEnhancerException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum VideoEnhancerModel
    TensorPixFpsBoost
    TensorPixUpscale2xUltra4
    TensorPixUpscale2xUltra41
    TensorPixUpscale4xUltra4
  static class VideoEnhancerModelExtensions
    static string DisplayName(VideoEnhancerModel model)
  sealed class VideoEnhancerResult : IEquatable<VideoEnhancerResult>
    ctor()
    int? OutputFps { get; init; }
    long? OutputSizeBytes { get; init; }
    string Url { get; init; }

namespace Ikon.AI.VideoGeneration
  interface IVideoGenerator : IDisposable, IVideoGeneratorInfo
    abstract Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = default)
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
  sealed class VideoGeneratorConfig.InputImage : IEquatable<VideoGeneratorConfig.InputImage>
    ctor()
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
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
    // One-shot text-to-video. The verbose form
    // using var generator = new VideoGenerator(VideoGeneratorModel.Veo31Fast);
    // var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig { Prompt = prompt });
    // becomes
    // var video = await VideoGenerator.GenerateAsync(prompt);
    // Defaults to Veo31Fast (the cheap+fast tier of the strongest general-purpose family). Override the model via the second parameter when the task warrants. Returns the result with the generated clip's .Url. Reach for the constructor + GenerateVideoAsync when you need input images (image-to-video), a specific length, resolution, aspect ratio, negative prompt, audio, or any other VideoGeneratorConfig field beyond the prompt.
    static Task<VideoGeneratorResult> GenerateAsync(string prompt, VideoGeneratorModel model = Veo31Fast, CancellationToken cancellationToken = default)
    Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = default)
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
  sealed class VideoGeneratorConfig : IEquatable<VideoGeneratorConfig>
    ctor()
    VideoGeneratorAspectRatio AspectRatio { get; init; }
    bool? GenerateAudio { get; init; }
    List<VideoGeneratorConfig.InputImage> InputImages { get; init; }
    int Length { get; init; }
    string? NegativePrompt { get; init; }
    string? Prompt { get; init; }
    VideoGeneratorResolution Resolution { get; init; }
    int? Seed { get; init; }
    TimeSpan Timeout { get; init; }
  class VideoGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
  sealed class VideoGeneratorResult : IEquatable<VideoGeneratorResult>
    ctor()
    string Url { get; init; }

namespace Ikon.AI.WebScraping
  sealed class Cookie : IEquatable<Cookie>
    ctor()
    string Domain { get; init; }
    double ExpirationDate { get; init; }
    bool HostOnly { get; init; }
    bool HttpOnly { get; init; }
    int Id { get; init; }
    string Name { get; init; }
    string Path { get; init; }
    string SameSite { get; init; }
    bool Secure { get; init; }
    bool Session { get; init; }
    string StoreId { get; init; }
    string Value { get; init; }
  sealed class DownloadFileConfig : IEquatable<DownloadFileConfig>
    ctor()
    string CountryCode { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
  sealed class DownloadFileResult : IEquatable<DownloadFileResult>
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Url { get; init; }
  interface IWebScraper : IDisposable, IWebScraperInfo
    abstract Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = default)
    abstract Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = default)
    abstract Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = default)
    abstract Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = default)
  interface IWebScraperInfo
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
  sealed class MultiPageScrapeConfig : IEquatable<MultiPageScrapeConfig>
    ctor()
    bool AddGivenUrlsToWhitelist { get; init; }
    bool AllowOnlyGivenUrls { get; init; }
    List<Cookie> Cookies { get; init; }
    string CountryCode { get; init; }
    int DelayMs { get; init; }
    string ExcludedCSSElements { get; init; }
    List<string> ExcludedLineStarts { get; init; }
    List<string> ExcludedWholeLines { get; init; }
    bool Headless { get; init; }
    bool IgnoreRobotsTxt { get; init; }
    bool IncludeLinkedFiles { get; init; }
    string IncludedCSSElements { get; init; }
    string JavaScript { get; init; }
    bool LoadResources { get; init; }
    string Locale { get; init; }
    int MaxDepth { get; init; }
    int MaxPages { get; init; }
    WebScraperOutputFormat OutputFormat { get; init; }
    string PlaywrightScript { get; init; }
    bool RerunIfGivenUrlsMissing { get; init; }
    TimeSpan SinglePageTimeout { get; init; }
    TimeSpan Timeout { get; init; }
    List<string> UrlBlacklist { get; init; }
    List<string> UrlWhitelist { get; init; }
    List<string> Urls { get; init; }
    bool UseReadability { get; init; }
    bool UseSitemap { get; init; }
    bool UseSitemapOnly { get; init; }
    bool UseStreaming { get; init; }
    TimeSpan WaitAfter { get; init; }
  sealed class PageResult : IEquatable<PageResult>
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string Mimetype { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed class ScreenshotConfig : IEquatable<ScreenshotConfig>
    ctor()
    List<Cookie> Cookies { get; init; }
    string CountryCode { get; init; }
    bool FullPage { get; init; }
    bool Headless { get; init; }
    int Height { get; init; }
    string JavaScript { get; init; }
    string Locale { get; init; }
    string PlaywrightScript { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
    bool UseCaptchaSolver { get; init; }
    TimeSpan WaitAfter { get; init; }
    int Width { get; init; }
  sealed class ScreenshotResult : IEquatable<ScreenshotResult>
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
  sealed class SinglePageScrapeConfig : IEquatable<SinglePageScrapeConfig>
    ctor()
    List<Cookie> Cookies { get; init; }
    string CountryCode { get; init; }
    string ExcludedCSSElements { get; init; }
    List<string> ExcludedLineStarts { get; init; }
    List<string> ExcludedWholeLines { get; init; }
    bool Headless { get; init; }
    bool IncludeLinkedFiles { get; init; }
    string IncludedCSSElements { get; init; }
    string JavaScript { get; init; }
    bool LoadResources { get; init; }
    string Locale { get; init; }
    WebScraperOutputFormat OutputFormat { get; init; }
    string PlaywrightScript { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
    bool UseCaptchaSolver { get; init; }
    bool UseReadability { get; init; }
    TimeSpan WaitAfter { get; init; }
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
    Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = default)
    static WebScraperCapabilities GetCapabilities(WebScraperModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebScraperModel model)
    // One-shot single page scrape. The verbose form
    // using var scraper = new WebScraper(WebScraperModel.Jina);
    // var page = await scraper.ScrapeSinglePageAsync(new SinglePageScrapeConfig { Url = url });
    // becomes
    // var page = await WebScraper.ScrapeAsync(url);
    // Defaults to Jina (cheap+fast hosted reader). Override the model via the second parameter when the task warrants. Returns the page as Markdown in .Content along with .Title and .Url. Reach for the constructor + ScrapeSinglePageAsync when you need a different output format, cookies, custom JavaScript, or any other SinglePageScrapeConfig field beyond the URL; use ScrapeMultiplePagesAsync , TakeScreenshotAsync , or DownloadFileAsync for crawling, screenshots, and file downloads.
    static Task<PageResult> ScrapeAsync(string url, WebScraperModel model = Jina, CancellationToken cancellationToken = default)
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = default)
  sealed class WebScraperCapabilities : IWebScraperInfo
    ctor()
    bool SupportsFileDownload { get; init; }
    bool SupportsMultiPageScraping { get; init; }
    bool SupportsScreenshotting { get; init; }
    bool SupportsSinglePageScraping { get; init; }
  class WebScraperException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
    abstract Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
    abstract Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
  interface IWebSearcherInfo
    bool SupportsImageSearching { get; }
  sealed class SearchConfig : IEquatable<SearchConfig>
    ctor()
    string CountryCode { get; init; }
    string InSiteUrl { get; init; }
    string Language { get; init; }
    int MaxResults { get; init; }
    WebSearcherOutputFormat OutputFormat { get; init; }
    string Query { get; init; }
    TimeSpan Timeout { get; init; }
  sealed class SearchResult : IEquatable<SearchResult>
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string Mimetype { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed class WebSearcher : IDisposable, IWebSearcher, IWebSearcherInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(WebSearcherModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsImageSearching { get; }
    void Dispose()
    static WebSearcherCapabilities GetCapabilities(WebSearcherModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebSearcherModel model)
    // One-shot web page search. The verbose form
    // using var searcher = new WebSearcher(WebSearcherModel.Google);
    // var results = await searcher.SearchPagesAsync(new SearchConfig { Query = query });
    // becomes
    // var results = await WebSearcher.SearchAsync(query);
    // Defaults to Google (cheap+fast general web search). Override the model via the second parameter when the task warrants. Each SearchResult exposes .Url, .Title, and .Content. Reach for the constructor + SearchPagesAsync when you need site-restricted search, country/language targeting, or any other SearchConfig field beyond query+max results; use SearchImagesAsync (with an image-capable model such as GoogleImages ) for image search.
    static Task<List<SearchResult>> SearchAsync(string query, WebSearcherModel model = Google, int maxResults = 10, CancellationToken cancellationToken = default)
    Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
    Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
  sealed class WebSearcherCapabilities : IWebSearcherInfo
    ctor()
    bool SupportsImageSearching { get; init; }
  class WebSearcherException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
