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
  // Downloads URL-delivered generation results. When a result's ResultKind is ResultKind.Url the payload lives behind a signed download link valid for roughly one hour; GetDataAsync returns the bytes either way, downloading transparently when needed.
  static class AssetOutputs
    static Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default)
    // Returns the result's payload bytes: Data when delivered inline, otherwise downloaded from Url.
    static Task<byte[]> GetDataAsync(this IResultPayload result, CancellationToken cancellationToken = default)
  // The name selects the model in the category's string-based APIs (e.g. new LLM("my-model")). An empty ApiKey means the endpoint needs no authentication header.
  abstract class CustomModel
    // API key for the endpoint. Empty means no authentication header is sent (e.g. a local Ollama instance).
    string ApiKey { get; init; }
    // Model name sent to the endpoint in the request payload. Defaults to Name when left unset.
    string ApiModelName { get; init; }
    // Full URL of the endpoint, e.g. http://localhost:8000/v1/chat/completions.
    required string EndpointUrl { get; init; }
    // Name the model is registered and selected by. Must not collide with a built-in model name and must not contain dots or whitespace.
    required string Name { get; init; }
  // Register a model at app startup, then select it by name anywhere a model name string is accepted:
  // CustomModels.Instance.Register(new CustomLLMModel
  // {
  //     Name = "my-model",
  //     EndpointUrl = "http://localhost:8000/v1/chat/completions",
  //     Api = CustomLLMApi.OpenAICompletions,
  //     ApiKey = "sk-...",
  //     ContextWindowSize = 32768,
  // });
  //
  // var reply = await Emerge.AskAsync("Hello", "my-model");
  // Custom models always execute in the local process — calls never go through the Ikon RPC mechanism. Usage is reported with a .user suffix and billed as a flat per-request fee instead of per-token provider pricing. The registry is async-local (like CredentialStorage): register models on the main flow at startup, before spawning parallel work, so every flow sees them. Registering the same name again replaces the previous registration; instances constructed before the replacement keep the configuration they were created with.
  sealed class CustomModels : AsyncLocalInstance<CustomModels>
    ctor()
    // True when the name is registered in any category.
    bool IsRegistered(string name)
    // Registers a custom LLM endpoint, selectable with new LLM(name) and the string-model overloads of Emerge.
    void Register(CustomLLMModel model)
    // Registers a custom embedding endpoint, selectable with new EmbeddingGenerator(name).
    void Register(CustomEmbeddingModel model)
    // Registers a custom rerank endpoint, selectable with new Reranker(name).
    void Register(CustomRerankModel model)
    // Registers a custom classification endpoint, selectable with new Classifier(name).
    void Register(CustomClassificationModel model)
    // Removes the named model from every category it is registered in. Returns true when at least one registration was removed.
    bool Unregister(string name)
  // A generation result payload delivered either as inline bytes or as a short-lived download URL, as told by Kind.
  interface IResultPayload
    byte[]? Data { get; }
    ResultKind Kind { get; }
    string? Url { get; }
  // Transient (network blip, server restart, flaky link) and therefore retryable — the RPC layer retries with a forced reconnect, and exhausted attempts still surface as retryable.
  sealed class IkonServerConnectException : RetryableAIException
    ctor(string message)
    ctor(string message, Exception inner)
  // A reference clip for prompt-driven audio editing: the model preserves this clip's timing and structure while the prompt re-styles it. Supply the clip exactly one way: Data (with MimeType), Url, or AssetUri (resolved automatically).
  sealed record InputAudio
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    // End of the region to edit, in seconds. null means to the end.
    double? EndSeconds { get; init; }
    string? MimeType { get; init; }
    // Start of the region to edit, in seconds. null means from the beginning.
    double? StartSeconds { get; init; }
    // How strongly the output adheres to this reference, in [0, 1]; higher keeps the original melody/timing closer. null defaults to strong adherence.
    double? Strength { get; init; }
    string? Url { get; init; }
  // Supply the image exactly one way: inline via Data (with MimeType), by Url, or by AssetUri — all consumers resolve the asset to a URL. Type, Strength, and MaskDilution apply only to image-editing/inpainting models; depth, segmentation, mesh, and video generation ignore them.
  sealed record InputImage
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    double? MaskDilution { get; init; }
    string? MimeType { get; init; }
    double? Strength { get; init; }
    InputImageType Type { get; init; }
    string? Url { get; init; }
  enum InputImageType
    Normal
    Mask
  // A reference clip for video generation: footage the model is shown rather than asked to invent, addressed from the prompt the way reference images are. What it is *for* is the prompt's business — carrying a subject's appearance across a cut, holding a camera move, or regenerating a stretch of an existing film with one thing changed. Supply the clip exactly one way: Data (with MimeType), Url, or AssetUri (resolved automatically). Providers impose their own length and size limits.
  sealed record InputVideo
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  // Attributes a failed model call to a ModelFailureKind.
  static class ModelFailure
    static ModelFailureKind Classify(Exception exception)
  // Why a model call failed, independent of the modality that raised it. The retryable/non-retryable split answers "should this call be tried again"; it deliberately says nothing about the cause, so a removed model, an unpaid account and a model that merely answered badly all land in the same bucket. This answers "what does the failure say about the model", which is what decides whether a human has to act.
  enum ModelFailureKind
    // The failure could not be attributed. Callers that gate on this should treat it as a real failure: an unrecognised error is far more likely to be a genuine defect than a benign one.
    Unknown
    // The call never reached a verdict about the model: transport error, timeout, throttling or a provider-side fault. Says nothing about whether the model is healthy.
    Transient
    // The provider no longer serves this model id. The model has been removed, renamed or retired and the configuration has to be updated.
    Unavailable
    // The model exists but this account cannot call it: missing or rejected credentials, exhausted credits, or a quota that is not a transient rate limit. An operator has to act, but nothing is wrong with the model or the code.
    AccessDenied
    // The model answered and the answer did not meet the contract: no content, an unusable tool call, or output that failed validation. Non-deterministic by nature and often not reproducible on the next call.
    Quality
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
  // An image produced by an analysis model (depth map, segmentation mask, preview). Kind tells how it was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record OutputImage : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    int Height { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
    int Width { get; init; }
  class RegionNotSupportedException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // With Auto the payload stays inline in-process; only when the result is returned from a remotely hosted AI function is it uploaded to a short-lived asset URL, and then only if it exceeds an internal size threshold (a few MB), keeping the protocol message small. Url always uploads, in any context. Check the result's ResultKind field to see which delivery was used.
  enum ResultDelivery
    Auto
    Url
  // Data guarantees the result's Data is non-null; Url guarantees its Url is non-null. Call result.GetDataAsync() (AssetOutputs.GetDataAsync) to get the bytes either way.
  enum ResultKind
    Data
    Url
  class RetryableAIException : AIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // The URLs these clients fetch come from app code, from LLM tool arguments and from provider responses — none of which the platform controls. Checking the URL string is not enough: the name is resolved later, so a host that resolves to 169.254.169.254 passes any check made up front, and a redirect or a second DNS answer moves the target after the check. The decision therefore happens at SocketsHttpHandler.ConnectCallback, on the address actually being connected to. That covers every redirect hop, because each one connects again, and it closes DNS rebinding, because the address checked is the address used.
  static class SsrfGuard
    // A handler that resolves the host itself and connects only to a public address.
    static SocketsHttpHandler CreateHandler()
    static bool IsAllowedScheme(Uri uri)

namespace Ikon.AI.Classification
  sealed record ClassificationDetail
    ctor()
    ctor(ClassificationLabel label, string originalCategory, bool isFlagged, double score)
    bool IsFlagged { get; init; }
    ClassificationLabel Label { get; init; }
    string OriginalCategory { get; init; }
    double Score { get; init; }
  // Supply Text, Data (with MimeType), Url, or AssetUri (resolved to a URL automatically).
  sealed record ClassificationInput
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Text { get; init; }
    string? Url { get; init; }
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
    // Sentinel for user-registered custom models (see CustomModels); not directly usable — select custom models by their registered name string.
    Custom
  static class ClassificationModelExtensions
    static string DisplayName(this ClassificationModel model)
  sealed record ClassificationResult
    ctor()
    List<ClassificationDetail> Details { get; init; }
    bool IsFlagged { get; init; }
  class ClassificationResultException : NonRetryableAIException
    ctor(ClassificationResult classificationResult)
    ctor(ClassificationResult classificationResult, Exception inner)
    ClassificationResult ClassificationResult { get; }
  sealed class Classifier : IClassifier
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ClassificationModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsImageInput { get; }
    TimeSpan Timeout { get; set; }
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, CancellationToken cancellationToken = default)
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, CancellationToken cancellationToken = default)
    Task<ClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a Classifier per call. Defaults to ClassificationModel.OpenAIOmniModeration (free); override via model. Read result.IsFlagged and per-label result.Details. Use the constructor plus the instance overloads for image/message-part input, a custom Timeout, or reusing one instance across many inputs.
    static Task<ClassificationResult> ClassifyAsync(string text, ClassificationModel model = OpenAIOmniModeration, CancellationToken cancellationToken = default)
    void Dispose()
    static ClassifierCapabilities GetCapabilities(ClassificationModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ClassificationModel model)
  sealed class ClassifierCapabilities : IClassifierInfo
    ctor()
    bool SupportsImageInput { get; init; }
  class ClassifierException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // Request/response format a custom classification endpoint speaks.
  enum CustomClassificationApi
    // OpenAI moderations format (/v1/moderations).
    OpenAI
    // Mistral moderations format.
    Mistral
  // Configuration for a user-provided custom classification endpoint, registered via CustomModels.Register and selected by name with new Classifier(name).
  sealed class CustomClassificationModel : CustomModel
    ctor()
    // Request/response format the endpoint speaks.
    required CustomClassificationApi Api { get; init; }
    // True when the endpoint accepts image inputs.
    bool SupportsImageInput { get; init; }
  interface IClassifier : IClassifierInfo, IDisposable
    // Defaults to 10 seconds.
    TimeSpan Timeout { get; set; }
    Task<ClassificationResult> ClassifyAsync(IReadOnlyList<ClassificationInput> inputs, CancellationToken cancellationToken = default)
    virtual Task<ClassificationResult> ClassifyAsync(IReadOnlyList<IMessagePart> messageParts, CancellationToken cancellationToken = default)
    virtual Task<ClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken = default)
  interface IClassifierInfo
    bool SupportsImageInput { get; }
  class NonRetryableClassifierException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.Database
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
  // For app code prefer the typed factories (Trino, Postgres, Sqlite, BigQuery), passing the password from app.Secrets. CreateAsync instead reads every connection field from environment variables or space secrets, for shared pipelines.
  class DatabaseConnection : IDisposable
    string BigQueryDataset { get; set; }
    string BigQueryProjectId { get; set; }
    DatabaseType DatabaseType { get; set; }
    DbConnection DbConnection { get; set; }
    static DatabaseConnection BigQuery(string projectId, string dataset)
    static Task<DatabaseConnection> CreateAsync(DatabaseConnection.Config config)
    // Disposes the owned DbConnection — a pooled connection returns to its pool. Wrap per-request use in using; without it every construction leaks a live connection until the pool is exhausted.
    void Dispose()
    static DatabaseConnection Postgres(string host, int port, string database, string user, string password)
    static DatabaseConnection Sqlite(string path)
    static DatabaseConnection Trino(string host, int port, string catalog, string user, string password)
  class DatabaseConnection.Config
    ctor()
    string? EnvVarPrefix { get; set; }
    DatabaseConnection.SpaceSecret? SpaceSecret { get; set; }
  class DatabaseConnection.SpaceSecret
    ctor()
    string Prefix { get; set; }
    string SpaceId { get; set; }
  class DatabaseInfo
    ctor()
    DatabaseType DatabaseType { get; set; }
    List<string>? ExampleQuestions { get; set; }
    string? SqlCteCommand { get; set; }
    List<DatabaseTableInfo> Tables { get; set; }
  class DatabaseInfoExtractor
    ctor(DatabaseConnection databaseConnection)
    Task<DatabaseInfo> ExtractAsync(DatabaseInfoExtractor.Config config, CancellationToken cancellationToken)
  // Configuration for database info extraction.
  class DatabaseInfoExtractor.Config
    ctor()
    // Regex patterns matched against the three-part schema.table.column name.
    List<string>? ColumnExcludeRegex { get; set; }
    Dictionary<string, string> ColumnExtraInfo { get; set; }
    bool IncludeEmptyColumns { get; set; }
    int JsonSampleLengthLimit { get; set; }
    int JsonSampleRowLimit { get; set; }
    int NonTextSampleRowLimit { get; set; }
    // When empty the default depends on the database type (e.g. public for PostgreSQL).
    List<string>? Schemas { get; set; }
    // Regex patterns for table names to exclude.
    List<string>? TableExcludeRegex { get; set; }
    Dictionary<string, string> TableExtraInfo { get; set; }
    // Regex patterns matched against schema.table (or just table); an empty/null list includes all.
    List<string>? TableIncludeRegex { get; set; }
    int TextSampleLengthLimit { get; set; }
    int TextSampleRowLimit { get; set; }
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
  sealed class ResultCell
    ctor(string column, object? value)
    string Column { get; }
    object? Value { get; }
  sealed class ResultRow
    ctor(IReadOnlyList<ResultCell> cells)
    IReadOnlyList<ResultCell> Cells { get; }
    // Value of the named column, or null. Null is returned both for a genuine SQL NULL and for a column that is not present — use TryGetValue to tell the two apart.
    object? this[string column] { get; }
    // Looks up a column by name. Returns false only when no such column exists; a column present but holding SQL NULL returns true with value set to null.
    bool TryGetValue(string column, out object? value)
  sealed class ResultSet
    ctor(IReadOnlyList<string> columns, IReadOnlyList<ResultRow> rows, int limitedRowCount, int totalRowCount, CultureInfo culture)
    IReadOnlyList<string> Columns { get; }
    int LimitedRowCount { get; }
    IReadOnlyList<ResultRow> Rows { get; }
    int TotalRowCount { get; }
    static Task<ResultSet> Create(DbDataReader reader, int maxRows, CultureInfo? culture = null, List<string>? columnNames = null)
    string ToCsv()
    string ToJson()
    string ToMarkdown()
  static class SqlValidator
    // Best-effort guard that rejects LLM-authored SQL carrying a write/side-effect keyword or a table outside allowedTables. It is a keyword blocklist plus a FROM/JOIN allowlist, NOT a dialect-aware parser, so it does not prove the statement is side-effect free. Where the query runs against real data, back it with a read-only transaction or role.
    static void ValidateReadOnly(string sql, IReadOnlySet<string> allowedTables)

namespace Ikon.AI.DepthEstimation
  sealed class DepthEstimator : IDepthEstimator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(DepthEstimatorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // Estimate depth for one image — the instance form of the EstimateAsync one-shot, for when you already hold an estimator. Reach for EstimateDepthAsync when the request needs any other DepthEstimatorConfig field.
    Task<DepthEstimatorResult> EstimateAsync(byte[] imageData, string mimeType, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a DepthEstimator per call. Defaults to DepthEstimatorModel.DepthAnythingV2 (cheap+fast); override via model (Marigold is slower, higher quality). The depth map is in result.Depth (.Data/.MimeType). Use the constructor + EstimateDepthAsync for a URL source or the Marigold tuning fields.
    static Task<DepthEstimatorResult> EstimateAsync(byte[] imageData, string mimeType, DepthEstimatorModel model = DepthAnythingV2, CancellationToken cancellationToken = default)
    Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(DepthEstimatorModel model)
  sealed record DepthEstimatorConfig
    ctor()
    int? EnsembleSize { get; init; }
    InputImage InputImage { get; init; }
    int? NumInferenceSteps { get; init; }
    int? ProcessingResolution { get; init; }
    ResultDelivery ResultDelivery { get; init; }
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
    static string DisplayName(this DepthEstimatorModel model)
  sealed record DepthEstimatorResult
    ctor()
    OutputImage Depth { get; init; }
  interface IDepthEstimator : IDisposable
    Task<DepthEstimatorResult> EstimateDepthAsync(DepthEstimatorConfig config, CancellationToken cancellationToken = default)
  class NonRetryableDepthEstimatorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.Embeddings
  // Request/response format a custom embedding endpoint speaks.
  enum CustomEmbeddingApi
    // OpenAI embeddings format (/v1/embeddings) — also spoken by most self-hosted embedding servers.
    OpenAI
    // Cohere embed format.
    Cohere
    // Mistral embeddings format.
    Mistral
    // Google Vertex prediction format.
    Google
    // Jina embeddings format.
    Jina
    // Voyage embeddings format.
    Voyage
  // Configuration for a user-provided custom embedding endpoint, registered via CustomModels.Register and selected by name with new EmbeddingGenerator(name).
  sealed class CustomEmbeddingModel : CustomModel
    ctor()
    // Request/response format the endpoint speaks.
    required CustomEmbeddingApi Api { get; init; }
    // Dimension of the returned embedding vectors.
    required int EmbeddingVectorSize { get; init; }
    // Maximum number of inputs per request; larger batches are split automatically.
    int MaxInputCount { get; init; }
  enum EmbeddingEncoding
    Base64
    GzipBase64
  sealed class EmbeddingGenerator : IEmbeddingGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(EmbeddingModel model, IReadOnlyList<ModelRegion>? regions = null)
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    void Dispose()
    // Embed a batch of texts — the instance form of the EmbedAsync one-shot, for when you already hold a generator. Reach for GenerateEmbeddingsAsync when the request needs any other EmbeddingGeneratorConfig field (batch cap, timeout).
    Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an EmbeddingGenerator per call. Defaults to EmbeddingModel.OpenAI3Small and EmbeddingType.Generic; override the model via model, and pass an explicit EmbeddingType when embedding documents vs. queries for asymmetric retrieval. Returns one float[] per input, in input order. Use the constructor + GenerateEmbeddingsAsync for per-request batch caps or a custom timeout.
    static Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingModel model = OpenAI3Small, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    Task<List<float[]>> GenerateEmbeddingsAsync(EmbeddingGeneratorConfig config, CancellationToken cancellationToken = default)
    static EmbeddingGeneratorCapabilities GetCapabilities(EmbeddingModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(EmbeddingModel model)
  sealed class EmbeddingGeneratorCapabilities : IEmbeddingGeneratorInfo
    ctor()
    int EmbeddingVectorSize { get; init; }
    int MaxInputCount { get; init; }
  sealed record EmbeddingGeneratorConfig
    ctor()
    List<string> Inputs { get; init; }
    // Per-request batch cap; larger input lists are split into batches of this size. 0 means the model's maximum.
    int MaxInputCount { get; init; }
    // Per-request; scaled up internally with the batch size.
    TimeSpan Timeout { get; init; }
    EmbeddingType Type { get; init; }
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
    VoyageCode3
    // Sentinel for user-registered custom models (see CustomModels); not directly usable — select custom models by their registered name string.
    Custom
  static class EmbeddingModelExtensions
    static string DisplayName(this EmbeddingModel model)
  enum EmbeddingType
    Generic
    Document
    Query
    Clustering
    Classification
  interface IEmbeddingGenerator : IDisposable, IEmbeddingGeneratorInfo
    // Returns one vector per input, in input order.
    Task<List<float[]>> GenerateEmbeddingsAsync(EmbeddingGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IEmbeddingGeneratorInfo
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
  class NonRetryableEmbeddingGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  static class VectorMath
    // Calculates the element-wise average embedding from a list of embeddings. Each embedding must be a float array of the same length.
    // embeddings: List of embeddings (each as a float array)
    static float[] CalculateAverageEmbedding(IList<float[]> embeddings)
    // Calculates the cosine similarity between two vectors.
    // throws ArgumentException: The vectors differ in length, or either vector has zero magnitude (e.g. a blank or failed embedding), for which cosine similarity is undefined. Guard degenerate vectors before calling when scoring in a loop.
    static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Calculates the dot product of two vectors.
    static float CalculateDotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // Calculates the Euclidean distance between two vectors.
    static float CalculateEuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    // For each embedding in the list, finds the k nearest neighbors (using Euclidean distance).
    // embeddings: List of embeddings (each as a float array)
    // k: Number of neighbors to retrieve for each embedding
    static List<List<VectorMath.Neighbor>> CalculateKNearestNeighbors(IList<float[]> embeddings, int k)
    // Calculates the magnitude (L2 norm) of a vector.
    static float GetMagnitude(ReadOnlySpan<float> vector)
  readonly struct VectorMath.Neighbor
    ctor(int index, float distance)
    float Distance { get; }
    int Index { get; }

namespace Ikon.AI.FileConversion
  // Kind tells how the file was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record ConvertedFile : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    string? Url { get; init; }
  sealed class FileConverter : IFileConverter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(FileConverterModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
    // Convert one file's bytes to PDF — the instance form of the ConvertToPdfAsync one-shot, for when you already hold a converter. Reach for ConvertToPdfAsync when the request needs any other FileConverterConfig field.
    Task<ConvertedFile> ConvertToPdfAsync(byte[] data, string fileName, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a FileConverter per call. fileName must carry the source extension (e.g. report.docx) — it determines the input format. The PDF is in result.Data. Use the constructor + ConvertToPdfAsync for a URL or AssetUri source, or a custom timeout.
    static Task<ConvertedFile> ConvertToPdfAsync(byte[] data, string fileName, FileConverterModel model = ConvertApi, CancellationToken cancellationToken = default)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(FileConverterModel model)
  // Supply the file exactly one way: Data, Url, or AssetUri (resolved to a URL automatically). FileName must carry the source extension (e.g. report.docx) — it determines the input format.
  sealed record FileConverterConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string FileName { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    TimeSpan Timeout { get; init; }
    string? Url { get; init; }
  class FileConverterException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum FileConverterModel
    ConvertApi
  static class FileConverterModelExtensions
    static string DisplayName(this FileConverterModel model)
  interface IFileConverter : IDisposable
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
  class NonRetryableFileConverterException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.ImageGeneration
  interface IImageGenerator : IDisposable, IImageGeneratorInfo
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IImageGeneratorInfo
    // True when the model accepts reference input images (image-to-image / editing).
    bool SupportsInputImage { get; }
    // True when an InputImageType.Mask gets dedicated inpainting handling rather than being treated as a plain reference image.
    bool SupportsMask { get; }
    // True when the model can return more than one image from a single request (ImageGeneratorConfig.Count > 1).
    bool SupportsMultipleOutputs { get; }
    // True when the model honours ImageGeneratorConfig.NegativePrompt.
    bool SupportsNegativePrompt { get; }
    // True when the model can produce output with a transparent background (ImageGeneratorConfig.Background = ImageBackground.Transparent). Requesting transparency from a model without it throws instead of failing at the provider.
    bool SupportsTransparentBackground { get; }
  enum ImageBackground
    Auto
    Opaque
    Transparent
  sealed class ImageGenerator : IImageGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsInputImage { get; }
    bool SupportsMask { get; }
    bool SupportsMultipleOutputs { get; }
    bool SupportsNegativePrompt { get; }
    bool SupportsTransparentBackground { get; }
    void Dispose()
    // Generate one image from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateImageAsync when the request needs any other ImageGeneratorConfig field (input images, size, image count).
    Task<ImageGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an ImageGenerator per call. Defaults to ImageGeneratorModel.Gemini25FlashImage (cheap+fast); override via model. Never returns null — throws ImageGeneratorException on failure or empty output, so wrap in try/catch to continue without the image. Use the constructor + GenerateImageAsync for batch/size/input-image or any other ImageGeneratorConfig field.
    static Task<ImageGeneratorResult> GenerateAsync(string prompt, ImageGeneratorModel model = Gemini25FlashImage, CancellationToken cancellationToken = default)
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = default)
    static ImageGeneratorCapabilities GetCapabilities(ImageGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageGeneratorModel model)
  sealed class ImageGeneratorCapabilities : IImageGeneratorInfo
    ctor()
    bool SupportsInputImage { get; init; }
    bool SupportsMask { get; init; }
    bool SupportsMultipleOutputs { get; init; }
    bool SupportsNegativePrompt { get; init; }
    bool SupportsTransparentBackground { get; init; }
  sealed record ImageGeneratorConfig
    ctor()
    ImageBackground Background { get; init; }
    int Count { get; init; }
    // Requested pixel height; see Width for how tiered providers treat it.
    int Height { get; init; }
    List<InputImage> InputImages { get; init; }
    // Embed Ikon's imperceptible provenance watermark in the result pixels (EU AI Act Article 50 machine-readable marking, uniform across providers). The XMP metadata mark is always written regardless of this flag; disabling this skips the pixel pass — and, for JPEG results, the one high-quality re-encode it costs.
    bool InvisibleWatermark { get; init; }
    string NegativePrompt { get; init; }
    string Prompt { get; init; }
    ImageQuality Quality { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    SafetyLevel SafetyLevel { get; init; }
    string SearchPrompt { get; init; }
    int Seed { get; init; }
    int Steps { get; init; }
    string Style { get; init; }
    TimeSpan Timeout { get; init; }
    bool UpsamplePrompt { get; init; }
    // Renders a small corner badge with this text on the result (e.g. "AI"). Empty = no visible mark. Intended as a plan-tier lever, not a compliance requirement — the machine-readable marks above are what Article 50 asks for.
    string VisibleWatermark { get; init; }
    // The only way to request a size. Providers with fixed resolution tiers (e.g. Gemini 1K/2K/4K) round the longer edge up to the nearest tier and take the aspect ratio from Width:Height — ask for 2048x2048 to get a 2K image.
    int Width { get; init; }
  class ImageGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum ImageGeneratorModel
    GptImage1Mini
    GptImage15
    GptImage2
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
    static string DisplayName(this ImageGeneratorModel model)
  // Kind tells how the image was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record ImageGeneratorResult : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    int Height { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
    int Width { get; init; }
  enum ImageQuality
    Auto
    Low
    Medium
    High
  class NonRetryableImageGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // Provider-mapped moderation strength; Moderate is the default.
  enum SafetyLevel
    None
    Minimal
    Low
    Moderate
    High
    VeryHigh
    Maximum

namespace Ikon.AI.ImageSegmentation
  interface IImageSegmenter : IDisposable
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed class ImageSegmenter : IImageSegmenter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageSegmenterModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageSegmenterModel model)
    // Segment one image against a prompt — the instance form of the SegmentAsync one-shot, for when you already hold a segmenter. Reach for SegmentImageAsync when the request needs any other ImageSegmenterConfig field.
    Task<ImageSegmenterResult> SegmentAsync(byte[] imageData, string mimeType, string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an ImageSegmenter per call. Defaults to ImageSegmenterModel.Sam31; override via model. Each detected object is in result.Segments with its mask image, score, and bounding box. Use the constructor + SegmentImageAsync for a URL source, point/box prompts, multiple masks per object, or other fields.
    static Task<ImageSegmenterResult> SegmentAsync(byte[] imageData, string mimeType, string prompt, ImageSegmenterModel model = Sam31, CancellationToken cancellationToken = default)
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed record ImageSegmenterConfig
    ctor()
    List<ImageSegmenterConfig.BoxPrompt> BoxPrompts { get; init; }
    InputImage InputImage { get; init; }
    int MaxMasks { get; init; }
    List<ImageSegmenterConfig.PointPrompt> PointPrompts { get; init; }
    string? Prompt { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    bool ReturnMultipleMasks { get; init; }
    TimeSpan Timeout { get; init; }
  sealed record ImageSegmenterConfig.BoxPrompt
    ctor()
    int? ObjectId { get; init; }
    double XMax { get; init; }
    double XMin { get; init; }
    double YMax { get; init; }
    double YMin { get; init; }
  sealed record ImageSegmenterConfig.PointPrompt
    ctor()
    bool IsBackground { get; init; }
    int? ObjectId { get; init; }
    double X { get; init; }
    double Y { get; init; }
  class ImageSegmenterException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum ImageSegmenterModel
    Sam3
    Sam31
  static class ImageSegmenterModelExtensions
    static string DisplayName(this ImageSegmenterModel model)
  sealed record ImageSegmenterResult
    ctor()
    OutputImage? Preview { get; init; }
    List<ImageSegmenterResult.Segment> Segments { get; init; }
  sealed record ImageSegmenterResult.Segment
    ctor()
    List<double> Box { get; init; }
    OutputImage Mask { get; init; }
    double? Score { get; init; }
  class NonRetryableImageSegmenterException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.ImageUpscaling
  interface IImageUpscaler : IDisposable, IImageUpscalerInfo
    Task<ImageUpscalerResult> UpscaleImageAsync(ImageUpscalerConfig config, CancellationToken cancellationToken = default)
  interface IImageUpscalerInfo
    // Whether the model invents detail; see UpscaleFidelity.
    UpscaleFidelity Fidelity { get; }
    // Largest output this model will produce, or 0 when it is uncapped. A request whose input size and scale factor would exceed it is refused before the provider is called, so a model priced in steps of output size can never be charged at a step above the one we bill. Only checked when the input is supplied as bytes — a URL's size is not known up front.
    double MaxOutputMegapixels { get; }
    // The largest ImageUpscalerConfig.ScaleFactor the provider accepts, or 0 when SupportsScaleFactor is false. A high ceiling is what the API allows, not a promise the provider will render it — the output size limit still applies.
    double MaxScaleFactor { get; }
    // True when the model honours ImageUpscalerConfig.Creativity. False on every UpscaleFidelity.Faithful model.
    bool SupportsCreativity { get; }
    // True when the model honours ImageUpscalerConfig.EnhanceFaces.
    bool SupportsFaceEnhancement { get; }
    // True when the model honours ImageUpscalerConfig.OutputFormat; on the rest the provider's own encoding is returned.
    bool SupportsOutputFormat { get; }
    // True when the model honours ImageUpscalerConfig.ScaleFactor. Models with a single built-in step size report false.
    bool SupportsScaleFactor { get; }
    // True when the model honours ImageUpscalerConfig.TargetResolution.
    bool SupportsTargetResolution { get; }
  sealed class ImageUpscaler : IImageUpscaler
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageUpscalerModel model, IReadOnlyList<ModelRegion>? regions = null)
    UpscaleFidelity Fidelity { get; }
    double MaxOutputMegapixels { get; }
    double MaxScaleFactor { get; }
    bool SupportsCreativity { get; }
    bool SupportsFaceEnhancement { get; }
    bool SupportsOutputFormat { get; }
    bool SupportsScaleFactor { get; }
    bool SupportsTargetResolution { get; }
    void Dispose()
    // Read ImageUpscalerCapabilities.Fidelity before picking a model when it matters whether the result may contain detail the input never had.
    static ImageUpscalerCapabilities GetCapabilities(ImageUpscalerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageUpscalerModel model)
    // Upscale one image — the instance form of the UpscaleAsync one-shot, for when you already hold an upscaler. Reach for UpscaleImageAsync when the request needs any other ImageUpscalerConfig field.
    Task<ImageUpscalerResult> UpscaleAsync(byte[] imageData, string mimeType, double scaleFactor = 0.0, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an ImageUpscaler per call. Defaults to ImageUpscalerModel.SeedVr2, which reconstructs detail faithfully and bills per output megapixel. scaleFactor of 0 leaves the model's own default in place. Every default model is UpscaleFidelity.Faithful — reach for ImageUpscalerModel.Crystal and ImageUpscalerConfig.Creativity to let a model invent detail. The upscaled image is in result.Image (.Data/.MimeType). Use the constructor + UpscaleImageAsync for a URL source or any other config field.
    static Task<ImageUpscalerResult> UpscaleAsync(byte[] imageData, string mimeType, ImageUpscalerModel model = SeedVr2, double scaleFactor = 0.0, CancellationToken cancellationToken = default)
    Task<ImageUpscalerResult> UpscaleImageAsync(ImageUpscalerConfig config, CancellationToken cancellationToken = default)
  sealed class ImageUpscalerCapabilities : IImageUpscalerInfo
    ctor()
    UpscaleFidelity Fidelity { get; init; }
    double MaxOutputMegapixels { get; init; }
    double MaxScaleFactor { get; init; }
    bool SupportsCreativity { get; init; }
    bool SupportsFaceEnhancement { get; init; }
    bool SupportsOutputFormat { get; init; }
    bool SupportsScaleFactor { get; init; }
    bool SupportsTargetResolution { get; init; }
  sealed record ImageUpscalerConfig
    ctor()
    // 0 keeps the model as close to the input as it can get; 1 lets it invent detail freely. Only models reporting IImageUpscalerInfo.SupportsCreativity accept a non-zero value — on the rest it throws, so a faithful model can never quietly start hallucinating.
    double Creativity { get; init; }
    // Restore faces beyond what the rest of the frame gets. This invents detail even on an otherwise faithful model, so it is off unless asked for.
    bool EnhanceFaces { get; init; }
    InputImage InputImage { get; init; }
    bool InvisibleWatermark { get; init; }
    // Defaults to UpscaleOutputFormat.Png: re-encoding a freshly recovered image as JPEG throws away detail that was just paid for.
    UpscaleOutputFormat OutputFormat { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    // Linear multiplier applied to both edges; 0 leaves the model's own default in place. Requesting a factor from a model that does not expose one, or one above the model's IImageUpscalerInfo.MaxScaleFactor, throws rather than being clamped.
    double ScaleFactor { get; init; }
    // Upscale towards a fixed resolution instead of by a factor. Mutually exclusive with ScaleFactor.
    UpscaleTargetResolution TargetResolution { get; init; }
    TimeSpan Timeout { get; init; }
    string VisibleWatermark { get; init; }
  class ImageUpscalerException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum ImageUpscalerModel
    SeedVr2
    Topaz
    RecraftCrisp
    Crystal
  static class ImageUpscalerModelExtensions
    static string DisplayName(this ImageUpscalerModel model)
  sealed record ImageUpscalerResult
    ctor()
    OutputImage Image { get; init; }
  class NonRetryableImageUpscalerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  // The distinction is the whole point of picking one upscaler over another. Faithful models reconstruct only what the input supports, so the result can still be read as evidence of the original. Creative models synthesize plausible detail that was never in the input. Tunable models move between the two as ImageUpscalerConfig.Creativity rises, and sit at the faithful end when it is left at zero.
  enum UpscaleFidelity
    Faithful
    Tunable
    Creative
  enum UpscaleOutputFormat
    Png
    Jpeg
  // The longer edge is driven to the named height and the aspect ratio is preserved. Only models whose capabilities report IImageUpscalerInfo.SupportsTargetResolution accept this.
  enum UpscaleTargetResolution
    None
    Hd720
    Fhd1080
    Qhd1440
    Uhd2160

namespace Ikon.AI.Kernel
  static class AsyncEnumerableExtensions
    static Task<T1[]> AsArrayAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<T1> AsFirstAsync<T1>(this IAsyncEnumerable<LLMEvent> source)
    static Task<string> AsStringAsync(this IAsyncEnumerable<LLMEvent> source)
    static IAsyncEnumerable<LLMEvent> WithParsedTagsAsync(this IAsyncEnumerable<LLMEvent> source, List<string>? tagWhitelist = null, List<string>? tagBlacklist = null)
    static IAsyncEnumerable<LLMEvent> WithReasoningFromTagAsync(this IAsyncEnumerable<LLMEvent> source, string reasoningTagName)
    static IAsyncEnumerable<LLMEvent> WithThrottlingAsync(this IAsyncEnumerable<LLMEvent> source, int charsPerSecond, int charsPerUpdate, CancellationToken cancellationToken = default)
    static IAsyncEnumerable<LLMEvent> WithWindowedProcessingAsync(this IAsyncEnumerable<LLMEvent> source, Func<string, List<LLMEvent>, Task<(bool, List<LLMEvent>)>> processAsync, int windowSize = 0, int windowOverlap = 0)
  readonly struct AudioIdPart : IMessagePart
    ctor(string id)
    string Id { get; }
    MessagePartType Type { get; }
  readonly struct AudioPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  class BinaryDataContainer
    ctor(byte[] data, string mimeType)
    byte[] Data { get; }
    string MimeType { get; }
  class FunctionCall
    ctor(Function function, object?[] parameters, string parametersJson, string callId, string hash, string thoughtSignature = "", string reasoningContent = "")
    string CallId { get; }
    Function Function { get; }
    string Hash { get; }
    object?[] Parameters { get; }
    string ParametersJson { get; }
    string ReasoningContent { get; }
    string ThoughtSignature { get; }
  // Only providers that support media in tool results inline the media for the model to see; all other consumers fall back to ToString, which summarizes the media without emitting the bytes.
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
  readonly struct FunctionResultPart : IMessagePart
    ctor(FunctionCall functionCall, LLMEvent[] events, object result)
    LLMEvent[] Events { get; }
    FunctionCall FunctionCall { get; }
    object Result { get; }
    MessagePartType Type { get; }
  interface IMessagePart
    MessagePartType Type { get; }
  readonly struct ImagePart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  readonly struct ImageUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  readonly struct Instruction
    ctor(InstructionType type, string content)
    string Content { get; }
    InstructionType Type { get; }
  enum InstructionType
    Context
    Command
  readonly record struct KernelContext
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction>? instructions = null, ImmutableList<MessageBlock>? messages = null, ImmutableDictionary<string, Function>? functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string? audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object? jsonSchema = null, string? gbnfGrammar = null, string? toolPlan = null)
    string AudioOutputVoiceId { get; init; }
    // Threshold is measured in input tokens; clearing runs after prompt-cache lookup so cached prefixes survive. Null disables it. Only providers with server-side context editing (Anthropic context-management) act on it — others ignore it.
    int? ClearToolResultsAfterInputTokens { get; init; }
    // Tool names exempt from ClearToolResultsAfterInputTokens clearing — use for results that stay semantically load-bearing all run (verdicts, anchors).
    IReadOnlyList<string>? ClearToolResultsExcludedTools { get; init; }
    bool DisableFunctionCalling { get; init; }
    // When true (the DEFAULT — set in the constructor), any assistant text the model emits on the same turn as a tool call is DROPPED — only the tool call flows on. Set false to keep that interleaved text (e.g. a model that narrates before calling a tool). A direct Kernel/LLM consumer who does not set this loses same-turn text with no signal.
    bool DiscardTextOutputWithFunctionCalls { get; init; }
    // A fresh, blank `KernelContext` — equivalent to `new KernelContext()`. Prefer this or `new KernelContext()` over `default(KernelContext)`: `default` leaves the collections unset, though the mutation helpers below tolerate it. Provided as a named constant for code generated against frameworks that expect an `.Empty` affordance.
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
    // Travels with the context over RPC, so the process that actually talks to the provider honours it — which is the only way a remote generation can report progress at all.
    bool StreamProgress { get; init; }
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
    // Runs the model over this context AND executes registered functions in-loop: it wraps ILLM.GenerateAsync and, whenever the model calls a function whose result is produced inline, runs it and feeds the output back into the event stream (recursing until the model stops calling functions). Prefer this when the context has functions registered. Use ILLM.GenerateAsync directly for the RAW provider stream that never runs a tool.
    IAsyncEnumerable<LLMEvent> GenerateAsync(ILLM llm, CancellationToken cancellationToken = default)
    KernelContext KeepMessagesMax(int count)
    KernelContext WithFunctions(IEnumerable<Function>? functions, bool replaceExisting = false)
  // Consume by switching on the concrete record case; forward any case you do not handle unchanged so downstream consumers still receive it.
  abstract record LLMEvent
    // Name of the pipeline stage that produced this event (e.g. "generate", "generate.reasoning", "Shader.Output.AfterPass"). Combinators re-tag events they transform so the origin of each event stays visible.
    string Source { get; init; }
  // An incremental chunk of generated output audio.
  sealed record LLMEvent.AudioDelta : LLMEvent
    ctor(AudioChunk Audio)
    AudioChunk Audio { get; init; }
  // The provider-side id of the generated output audio, replayable as an AudioIdPart in a follow-up context.
  sealed record LLMEvent.AudioId : LLMEvent
    ctor(string Id)
    string Id { get; init; }
  // The transcript of generated output audio.
  sealed record LLMEvent.AudioTranscript : LLMEvent
    ctor(string Transcript)
    string Transcript { get; init; }
  // ReferStartIndex/ReferEndIndex bound the citing text span; PositionIndex is the character index of the citation marker itself.
  sealed record LLMEvent.Citation : LLMEvent
    ctor(string OriginalId, string MappedId, int ReferStartIndex, int ReferEndIndex, int PositionIndex)
    string MappedId { get; init; }
    string OriginalId { get; init; }
    int PositionIndex { get; init; }
    int ReferEndIndex { get; init; }
    int ReferStartIndex { get; init; }
  // Generation was stopped by a content-safety classifier.
  sealed record LLMEvent.ContentFiltered : LLMEvent
    ctor(ClassificationResult Classification)
    ClassificationResult Classification { get; init; }
  // The complete model message of a shader run (may differ from the text response), emitted once at the end.
  sealed record LLMEvent.FinalModelMessage : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // The complete text response of a shader run, emitted once at the end.
  sealed record LLMEvent.FinalText : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // The provider's finish reason for the generation (e.g. "stop", "max_tokens").
  sealed record LLMEvent.Finished : LLMEvent
    ctor(string Reason)
    string Reason { get; init; }
  // How much content of a given kind arrived in one chunk, emitted as the chunk is decoded. Opt-in via KernelContext.StreamProgress — off by default, because it changes the event stream every consumer sees. It exists because nothing else can answer "is the model working right now" over RPC: usage is reported once a turn has ended, Reasoning and tool arguments are only emitted after the stream drains, and text may be suppressed entirely on a tool-calling turn. Carries the SIZE and not the content, so it costs a few bytes and never puts the same text on the wire twice — the content still arrives in its own event.
  sealed record LLMEvent.GenerationProgress : LLMEvent
    ctor(LlmStreamKind Kind, int Characters)
    int Characters { get; init; }
    LlmStreamKind Kind { get; init; }
  // The model's reasoning trace for this generation.
  sealed record LLMEvent.Reasoning : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // A parsed XML-style tag extracted from the text stream by AsyncEnumerableExtensions.WithParsedTagsAsync.
  sealed record LLMEvent.Tag : LLMEvent
    ctor(string Name, string Content, IReadOnlyDictionary<string, string>? Attributes)
    IReadOnlyDictionary<string, string>? Attributes { get; init; }
    string Content { get; init; }
    string Name { get; init; }
  // An incremental chunk of generated text.
  sealed record LLMEvent.TextDelta : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // The model requested a tool invocation.
  sealed record LLMEvent.ToolCallRequested : LLMEvent
    ctor(FunctionCall Call)
    FunctionCall Call { get; init; }
  // The model's plan for upcoming tool calls (Cohere).
  sealed record LLMEvent.ToolPlan : LLMEvent
    ctor(string Text)
    string Text { get; init; }
  // ValueType is the value's runtime type name, used to rehydrate Value to its original type after a JSON round-trip (e.g. over RPC).
  sealed record LLMEvent.ToolResult : LLMEvent
    ctor(string functionName, object? value)
    ctor(string functionName, object? value, string? valueType)
    string FunctionName { get; }
    object? Value { get; }
    string? ValueType { get; }
  // The buckets are disjoint: total input = InputTokens + CachedInputTokens + CacheCreationInputTokens. A fully cached prompt reports InputTokens=0 with all input in CachedInputTokens.
  sealed record LLMEvent.Usage : LLMEvent
    ctor(int InputTokens, int CachedInputTokens, int CacheCreationInputTokens, int OutputTokens)
    int CacheCreationInputTokens { get; init; }
    int CachedInputTokens { get; init; }
    int InputTokens { get; init; }
    int OutputTokens { get; init; }
  enum MediaResolution
    Default
    Low
    Medium
    High
    UltraHigh
  readonly struct MessageBlock
    ctor(MessageBlockRole role, IMessagePart[] parts, string? userName = null)
    ctor(MessageBlockRole role, IEnumerable<IMessagePart> parts, string? userName = null)
    ctor(MessageBlockRole role, string message, string? userName = null)
    IMessagePart[] Parts { get; }
    MessageBlockRole Role { get; }
    string? UserName { get; }
    // Builds a block from a heterogeneous input list. Each input must be a string or a BinaryDataContainer whose MIME type is an image, audio, video, or PDF; any other input type or MIME type is rejected rather than silently dropped. Returns null only when inputs is empty.
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
  readonly struct PdfPart : IMessagePart
    ctor(byte[] content, string mimeType)
    byte[] Content { get; }
    string MimeType { get; }
    MessagePartType Type { get; }
  readonly struct PdfUrlPart : IMessagePart
    ctor(string url)
    MessagePartType Type { get; }
    string Url { get; }
  enum ReasoningEffort
    None
    Minimal
    Low
    Medium
    High
  // Selects which JSON-schema dialect the generator emits. All Ikon-side schema shapes (primitives, arrays, dictionaries, polymorphism) are expressible in both dialects; the two differ in how they encode nullability and how strictly they police unknown keywords.
  enum SchemaDialect
    // JSON Schema 2020-12 / OpenAPI 3.1. Nullable primitives expand their "type" into a ["X", "null"] union. Accepted by OpenAI strict structured outputs and Anthropic tool-use schemas.
    JsonSchema202012
    // OpenAPI 3.0 Schema Object. "type" is always a single string and nullability is carried on a separate "nullable": true flag. Accepted by Google's Gemini response_schema validator, which rejects the 2020-12 union-type form outright.
    OpenApi30
  readonly struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  readonly struct VideoAssetPart : IMessagePart
    ctor(AssetUri uri, string? mimeType = null, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string? MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    AssetUri Uri { get; }
  readonly struct VideoPart : IMessagePart
    ctor(byte[] content, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    byte[] Content { get; }
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
  readonly struct VideoUrlPart : IMessagePart
    ctor(string url, string mimeType, MediaResolution resolution = Default, double? fps = null, TimeSpan? startOffset = null, TimeSpan? endOffset = null)
    TimeSpan? EndOffset { get; }
    double? Fps { get; }
    string MimeType { get; }
    MediaResolution Resolution { get; }
    TimeSpan? StartOffset { get; }
    MessagePartType Type { get; }
    string Url { get; }

namespace Ikon.AI.LLM
  // Request/response format a custom LLM endpoint speaks.
  enum CustomLLMApi
    // OpenAI chat completions format (/v1/chat/completions) — spoken by most self-hosted stacks: vLLM, Ollama, llama.cpp, LM Studio, TGI, SGLang.
    OpenAICompletions
    // OpenAI responses format (/v1/responses).
    OpenAIResponses
    // Anthropic messages format.
    Anthropic
    // Google Gemini format (Vertex-style streamGenerateContent).
    Google
    // Cohere chat format.
    Cohere
  // Capability flags default to what a typical self-hosted OpenAI-compatible model supports; enable more (e.g. SupportsJsonSchema) when the endpoint provides them.
  sealed class CustomLLMModel : CustomModel
    ctor()
    // Request/response format the endpoint speaks.
    required CustomLLMApi Api { get; init; }
    // Maximum input-context window of the model, in tokens.
    required int ContextWindowSize { get; init; }
    // Largest number of tokens the endpoint will generate in one response. Leave at 0 when the endpoint has no such cap: a request asking for more than the model can produce is capped at this value instead of being sent as-is, and 0 means "send the caller's value".
    int MaxOutputTokens { get; init; }
    bool SupportsCaching { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsSingleToolCalling { get; init; }
    bool SupportsStreaming { get; init; }
    bool SupportsStrictJsonSchema { get; init; }
    bool SupportsSystemMessages { get; init; }
    bool SupportsTemperature { get; init; }
  // Returns the exact JSON schema each provider ships to the model for a Function; use it rather than re-deriving your own projection.
  static class FunctionSchema
    // Projects the function's parameter list into its provider JSON schema: an object schema with type/properties/required, including parameter descriptions and allowed-value enums.
    static string ToJson(Function function)
  interface ILLM : IDisposable, ILLMInfo
    IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext context, CancellationToken cancellationToken = default)
  interface ILLMInfo
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
    // In tokens. 0 means "no published limit", not "no output allowed".
    int MaxOutputTokens { get; }
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
  sealed class LLMCapabilities : ILLMInfo
    ctor()
    int ContextWindowSize { get; init; }
    string InlineReasoningTagName { get; init; }
    int MaxOutputTokens { get; init; }
    SchemaDialect SchemaDialect { get; init; }
    bool SupportsGbnfGrammar { get; init; }
    // Distinct from SupportsInputImages: a vision model whose tool results are JSON-only (e.g. Gemini functionResponse) accepts images in messages but not inside tool_result blocks.
    bool SupportsImagesInToolResults { get; init; }
    bool SupportsInputAudio { get; init; }
    bool SupportsInputImages { get; init; }
    bool SupportsInputPdf { get; init; }
    bool SupportsInputVideo { get; init; }
    bool SupportsJsonSchema { get; init; }
    bool SupportsOutputAudio { get; init; }
    bool SupportsParallelToolCalling { get; init; }
    bool SupportsReasoning { get; init; }
    bool SupportsSingleToolCalling { get; init; }
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
    Gpt56Sol
    Gpt56Terra
    Gpt56Luna
    O3
    O3Pro
    Claude45Haiku
    Claude45Sonnet
    Claude45Opus
    Claude46Opus
    Claude46Sonnet
    Claude47Opus
    Claude48Opus
    Claude5Sonnet
    Claude5Opus
    Claude5Fable
    Gemini25Flash
    Gemini25FlashLite
    Gemini25Pro
    Gemini3Flash
    Gemini31Pro
    Gemini31FlashLite
    Gemini35Flash
    Gemini35FlashLite
    Gemini36Flash
    Grok43
    Grok45
    GrokBuild01
    Grok420Reasoning
    Grok420NonReasoning
    MistralSmall
    MistralMedium
    MistralLarge
    Ministral14B
    Ministral8B
    Ministral3B
    MagistralSmall
    MagistralMedium
    Codestral
    Devstral2
    VoxtralSmall
    CommandR
    CommandRPlus
    CommandA
    CommandAReasoning
    CommandAPlus
    CommandAVision
    CommandR7B
    KimiK25
    KimiK26
    KimiK27Code
    KimiK3
    Qwen36
    Qwen37
    Qwen37Max
    Qwen38Max
    Qwen37Flash
    Qwen3827B
    GptOss120B
    Glm5
    Glm51
    Glm52
    Glm53
    Glm5VTurbo
    MiniMaxM25
    MiniMaxM27
    MiniMaxM3
    DeepSeekV32
    DeepSeekV4Pro
    DeepSeekV4Flash
    DeepSeekV4FlashVision
    Seed21Turbo
    Seed20Code
    Seed20Lite
    Seed20Mini
    MiMoV25
    MiMoV25Pro
    Step37Flash
    NovaPro
    NovaLite
    NovaMicro
    Nova2Lite
  static class LLMModelExtensions
    // In tokens. Returns 0 when the model can't be resolved — treat 0 as "unknown" and skip utilization math rather than dividing by zero.
    static int ContextWindowSize(this LLMModel model)
    static string DisplayName(this LLMModel model)
    // In tokens. Returns 0 when the limit is unknown (unresolvable model, or a provider that publishes none) — treat 0 as "no cap known", not as a zero budget.
    static int MaxOutputTokens(this LLMModel model)
  class ModelOutputException : RetryableLLMException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
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
    Task<MeshGeneratorResult> GenerateMeshAsync(MeshGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IMeshGeneratorInfo
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
  sealed class MeshGenerator : IMeshGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MeshGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputImages { get; }
    bool SupportsImageToMesh { get; }
    bool SupportsLowPoly { get; }
    bool SupportsPbr { get; }
    bool SupportsTextToMesh { get; }
    void Dispose()
    // Generate a mesh from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateMeshAsync when the request needs any other MeshGeneratorConfig field.
    Task<MeshGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a MeshGenerator per call. Defaults to MeshGeneratorModel.Meshy6; override via model. Returns signed per-format download URLs (.GlbUrl, .FbxUrl, …) that expire roughly three days after generation — download promptly. Use the constructor + GenerateMeshAsync for image-to-mesh, PBR textures, or topology control.
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
  sealed record MeshGeneratorConfig
    ctor()
    bool EnablePbr { get; init; }
    List<InputImage> InputImages { get; init; }
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
    static string DisplayName(this MeshGeneratorModel model)
  // The download URLs are signed and expire roughly three days after generation — fetch the model files promptly.
  sealed record MeshGeneratorResult
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
  class NonRetryableMeshGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.MusicGeneration
  interface IMusicGenerator : IDisposable, IMusicGeneratorInfo
    // Channel count of the PCM samples produced by GenerateMusicAsync.
    int ChannelCount { get; }
    // Sample rate of the PCM samples produced by GenerateMusicAsync.
    int SampleRate { get; }
    // Requires IMusicGeneratorInfo.SupportsStreaming; otherwise throws NonRetryableMusicGeneratorException. Use GenerateMusicFileAsync for a buffered encoded file.
    IAsyncEnumerable<AudioChunk> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    // Generates the music and returns it as a single buffered, encoded audio file. Supported by all models, including those that cannot stream.
    Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IMusicGeneratorInfo
    // When false the model ignores MusicGeneratorConfig.DurationSeconds, emitting a fixed-length clip or (when editing) matching the input clip's length.
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    // When false, IMusicGenerator.GenerateMusicAsync throws; use the buffered IMusicGenerator.GenerateMusicFileAsync instead.
    bool SupportsStreaming { get; }
  sealed class MusicGenerator : IMusicGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MusicGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    bool SupportsStreaming { get; }
    void Dispose()
    // Generate a music file from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateMusicFileAsync when the request needs any other MusicGeneratorConfig field (duration, input audio, seed).
    Task<MusicGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a MusicGenerator per call. Defaults to MusicGeneratorModel.ElevenLabsMusicV2 (supports duration control and editing); override via model. Returns a buffered, encoded audio file (.Data/.MimeType/.DurationSeconds). Use the constructor + GenerateMusicFileAsync for duration/input-audio/seed, or GenerateMusicAsync for streaming PCM chunks.
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
  // With an empty InputAudios the model generates from the prompt alone; with one or more it performs audio-to-audio editing (the prompt re-styles the clips, timing preserved). The underlying music model works on clips of at least 3 seconds. For shorter UI/game sound effects use SoundEffectGenerator instead.
  sealed record MusicGeneratorConfig
    ctor()
    // Seconds, clamped to the model's supported range. When editing, set it to the source clip's length to keep the original timing. Ignored unless IMusicGeneratorInfo.SupportsDurationControl is true.
    double? DurationSeconds { get; init; }
    bool ForceInstrumental { get; init; }
    List<InputAudio> InputAudios { get; init; }
    string Prompt { get; init; }
    // Applies to the buffered IMusicGenerator.GenerateMusicFileAsync result; the streaming IMusicGenerator.GenerateMusicAsync chunks are unaffected.
    ResultDelivery ResultDelivery { get; init; }
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
    // The platform provides the Suno key, so these behave like every other model here and need no per-app secret. An app may still override it with its own subscription by setting IKON_SUNO_API_KEY (ikon app secret set IKON_SUNO_API_KEY <key>), which is then billed as bring-your-own-key usage.
    SunoV5
    SunoV55
  static class MusicGeneratorModelExtensions
    static string DisplayName(this MusicGeneratorModel model)
  // Kind tells how the audio was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record MusicGeneratorResult : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    double DurationSeconds { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
  class NonRetryableMusicGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)

namespace Ikon.AI.OCR
  enum DocumentType
    General
  interface IOCR : IDisposable, IOCRInfo
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = default)
  interface IOCRInfo
    int MaxPagesSupported { get; }
  class NonRetryableOCRException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class OCR : IOCR
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxPagesSupported { get; }
    // Read one document's bytes — the instance form of the AnalyzeAsync one-shot, for when you already hold an OCR instance. Reach for AnalyzeDocumentAsync when the request needs any other OCRConfig field (asset uri, url, document type).
    Task<OCRResult> AnalyzeAsync(byte[] data, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an OCR per call. Accepts image or PDF bytes. Defaults to OCRModel.AzureDocumentIntelligence; override via model. Extracted text is in result.Text; result.Paragraphs/result.Pages carry structure. Use the constructor + AnalyzeDocumentAsync for a URL/AssetUri source or other fields, or AnalyzeDocumentStreamingAsync for page-by-page streaming.
    static Task<OCRResult> AnalyzeAsync(byte[] data, OCRModel model = AzureDocumentIntelligence, CancellationToken cancellationToken = default)
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    static OCRCapabilities GetCapabilities(OCRModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(OCRModel model)
  sealed record OCRBoundingBox
    ctor()
    int PageNumber { get; init; }
    List<float> Polygon { get; init; }
  sealed class OCRCapabilities : IOCRInfo
    ctor()
    int MaxPagesSupported { get; init; }
  // Supply the document exactly one way: Data (with MimeType; detected from the bytes when unset), Url, or AssetUri (resolved to a URL automatically).
  sealed record OCRConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    DocumentType DocumentType { get; init; }
    bool IncludeWords { get; init; }
    string? MimeType { get; init; }
    string? Pages { get; init; }
    TimeSpan Timeout { get; init; }
    string? Url { get; init; }
  class OCRException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum OCRModel
    AzureDocumentIntelligence
    MistralOCR
  static class OCRModelExtensions
    static string DisplayName(this OCRModel model)
  sealed record OCRPage
    ctor()
    float Height { get; init; }
    int PageNumber { get; init; }
    string Unit { get; init; }
    float Width { get; init; }
  sealed record OCRParagraph
    ctor()
    List<OCRBoundingBox> BoundingRegions { get; init; }
    string Content { get; init; }
  sealed record OCRResult
    ctor()
    List<OCRPage> Pages { get; init; }
    List<OCRParagraph> Paragraphs { get; init; }
    string Text { get; init; }
    List<OCRWord> Words { get; init; }
  sealed record OCRWord
    ctor()
    OCRBoundingBox BoundingBox { get; init; }
    float Confidence { get; init; }
    string Content { get; init; }

namespace Ikon.AI.Provenance
  // Ikon's uniform provenance layer for AI-generated images — the platform's EU AI Act Article 50 marking, applied identically for every provider (per-provider watermarks are deliberately opted out of upstream). Three layers behind one call: an XMP metadata mark (always; the machine-readable baseline — IPTC DigitalSourceType=trainedAlgorithmicMedia), an imperceptible tiled pixel watermark (default on; detectable via MeasureInvisibleMark), and an optional visible corner badge (the self-service trial-tier lever). PNG and JPEG take all three; WebP takes the metadata mark alone, because the bundled codec neither reads nor writes it and reaching the pixels would mean transcoding the caller's image to another format. Ask GetMarkingSupport rather than assuming. Any other encoding passes through untouched. Streamed media (WebRTC, TTS) is out of scope by design — disclosure there is interaction-level.
  static class ImageProvenance
    // Mark a generated image. Returns new bytes for PNG/JPEG input; any other format is returned unchanged (recorded roadmap gap, not an error — the caller cannot do better).
    static byte[] Apply(byte[] data, string model, bool invisibleWatermark = true, string visibleWatermark = "")
    // What Apply would achieve on these bytes, so a caller can tell a fully marked result from a metadata-only one instead of assuming.
    static ProvenanceMarking GetMarkingSupport(byte[] data)
    // Correlation score of the invisible mark. At or above DetectionThreshold the image carries Ikon's mark; unmarked images score near zero. Public so tooling (and the trial-tier pipeline) can prove our own marks.
    static double MeasureInvisibleMark(byte[] data)
    // The embedded XMP packet, or null when none is present. Format-agnostic scan (the packet carries a globally unique id), usable on PNG and JPEG alike.
    static string? ReadMetadataMark(byte[] data)
    // Detection score at or above which an image is considered marked. Scores are normal-deviates: an unmarked image scores |z| ≲ 3, a marked one scores in the tens to hundreds depending on size and recompression.
    const double DetectionThreshold = 12.0
  // How completely ImageProvenance.Apply can mark a given encoding.
  enum ProvenanceMarking
    // No mark at all — an encoding this layer does not know.
    None
    // The XMP metadata mark only. Machine-readable and standards-compliant, but strippable by anything that rewrites the file's metadata.
    MetadataOnly
    // Metadata plus the imperceptible pixel watermark, which survives a re-encode.
    Full

namespace Ikon.AI.Reranking
  // Request/response format a custom rerank endpoint speaks.
  enum CustomRerankApi
    // Cohere rerank format.
    Cohere
    // Jina rerank format.
    Jina
    // Voyage rerank format.
    Voyage
    // Together rerank format.
    Together
  // Configuration for a user-provided custom rerank endpoint, registered via CustomModels.Register and selected by name with new Reranker(name).
  sealed class CustomRerankModel : CustomModel
    ctor()
    // Request/response format the endpoint speaks.
    required CustomRerankApi Api { get; init; }
  interface IReranker : IDisposable
    // Returns items ordered most relevant first; RerankItem.Index is the document's position in RerankerConfig.Documents.
    Task<List<RerankItem>> RerankAsync(RerankerConfig config, CancellationToken cancellationToken = default)
  class NonRetryableRerankerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed record RerankItem
    ctor()
    int Index { get; init; }
    double Score { get; init; }
  enum RerankModel
    CohereRerank4Fast
    CohereRerank4Pro
    JinaReranker3
    VoyageRerank25
    VoyageRerank25Lite
    // Sentinel for user-registered custom models (see CustomModels); not directly usable — select custom models by their registered name string.
    Custom
  static class RerankModelExtensions
    static string DisplayName(this RerankModel model)
  sealed class Reranker : IReranker
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(RerankModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(RerankModel model)
    Task<List<RerankItem>> RerankAsync(RerankerConfig config, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a Reranker per call. Defaults to RerankModel.CohereRerank4Fast; override via model. Pass topN to cap returned items (0 returns all). Each RerankItem carries the document's original .Index and relevance .Score, ordered most relevant first. Use the constructor + RerankAsync for a custom timeout or reusing one instance across many queries.
    static Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, RerankModel model = CohereRerank4Fast, int topN = 0, CancellationToken cancellationToken = default)
  sealed record RerankerConfig
    ctor()
    List<string> Documents { get; init; }
    string Query { get; init; }
    // Scaled up internally with the document count.
    TimeSpan Timeout { get; init; }
    // Caps how many items are returned; 0 returns all.
    int TopN { get; init; }
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
    ctor(string link, float score = 0f)
    ctor(List<string> segments, float score = 0f)
    ctor(ContentLink parent, string secondPart, float score = 0f)
    ctor(string link, string secondPart, float score = 0f)
    ContentLink Parent { get; }
    ContentLink Root { get; }
    override bool Equals(object? obj)
    List<(string Link, string Internal)> GenerateHierarchicalSplitLinks()
    override int GetHashCode()
    override string ToString()
    static bool operator ==(ContentLink? lhs, ContentLink? rhs)
    static bool operator !=(ContentLink? lhs, ContentLink? rhs)
    readonly string Link
    readonly float Score
    readonly List<string> Segments
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
    ValueTask DisposeAsync()
    Task<ContentLink[]> ExpandAsync(ContentLink[] links)
    Task<ContentLink[]> ExpandAsync(ContentLink link)
    Task<Content?> GetContentAsync(ContentLink link)
    Retriever.ContentMetadata? GetContentMetadata(string metadataId)
    Task<string> GetContentsAsync(string query, Retriever.GetContentsOptions options)
    ContentLink? Ignore(ContentLink link, string detail)
    Task InitializeAsync(string dataDirectory, EmbeddingModel embeddingModel = OpenAI3Small, VectorStoreConfig? vectorStore = null)
    Task InitializeAsync(IReadOnlyList<AssetUri> assetUris, EmbeddingModel embeddingModel = OpenAI3Small, VectorStoreConfig? vectorStore = null)
    ContentLink[] Prefer(ContentLink link, string detail)
    ContentLink[] Prefer(ContentLink[] links, string detail)
    Task<ContentLink[]> SearchAsync(string query, int maxLinks = 25, float searchThreshold = 0.1f)
    Task<Retriever.Event[]> SearchEventsAsync(string startUtcTimestamp, string endUtcTimestamp, int maxResults = 100)
    Task<Retriever.Event[]> SearchEventsAsync(string startUtcTimestamp, string endUtcTimestamp, string searchString, int maxResults = 100)
    Task<KeywordSearchResult[]> SearchKeywordsAsync(string searchString, int maxResults = 100)
    Task StopAsync()
    Task WaitForLoadingToEndAsync()
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
    void AddFilter(string name, KernelContext context, Function function)
    void AddFunction(string name, KernelContext context, Function function)
    bool ContainsKey(string key)
    IEnumerable<string> GetKeys()
    object? GetValue(string key)
    string GetValueAsString(string key)
    void Register<T>() where T : class
    void SetValue(string key, object? value)
  interface IScriptEngine
    IScriptContext CreateContext()
    bool TryParse(string template, out IScriptTemplate? parsedTemplate, out string? errorMessage)
  interface IScriptTemplate
    Task<string> RenderAsync(IScriptContext context)
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
  class ScriptableValue<T> where T : struct
    ctor(T value)
    ctor(string script)
    string? Script { get; }
    T? Value { get; }
    Task<T> GetValueAsync(Func<string, Task<string>> renderer)
  class ScriptableValueConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
  class Shader
    ctor(string shaderConfigAsJsonString, bool enableRenderedShaderLogging = false)
    Dictionary<string, object?> Input { get; }
    static string Escape(string? text)
    IAsyncEnumerable<LLMEvent> GenerateAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, ExpandoObject? implicitJsonSchema = null, string? implicitJsonExample = null, IdMapper? idMapper = null, string modelUserName = "", string modelMessagePrefix = "", string modelMessageSuffix = "", int iteration = 0, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default)
    Task<T> GenerateObjectAsync<T>(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, JsonSerializerOptions? jsonSerializerOptions = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default) where T : new()
    Task<string> GenerateStringAsync(KernelContext? context = null, List<KernelContext>? externalContexts = null, Dictionary<string, object?>? state = null, ShaderInvocationContext? invocationContext = null, Func<Dictionary<string, object?>, Task>? stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default)
    void SetActiveState<T>(string key, T? value)
    static string Unescape(string? text)
    event EventHandler<string>? RenderedShader
  class Shader.TemplateMessage
    ctor()
    string Content { get; set; }
    string Role { get; set; }
  class ShaderCache : AsyncLocalInstance<ShaderCache>
    ctor()
    string? DefaultSpaceId { get; set; }
    ShaderCache.ImplicitShader GetImplicitShader()
  class ShaderCache.ImplicitShader
    ctor(AssetUri? shaderUri, string callerFilePath, ShaderCache outer)
    IAsyncEnumerable<LLMEvent> GenerateAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<T> GenerateObjectAsync<T>(string? cacheKey = null, List<KernelContext>? contexts = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters) where T : new()
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters)
    Task<string> GenerateStringAsync(List<KernelContext>? contexts = null, ShaderInvocationContext? invocationContext = null, Func<KernelContext, Task<KernelContext>>? preprocessContext = null, CancellationToken cancellationToken = default, params (string key, object? value)[] parameters)
    Task<Shader> GetShaderAsync()
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
    // Channel count of the PCM samples produced by GenerateSoundEffectAsync.
    int ChannelCount { get; }
    // Sample rate of the PCM samples produced by GenerateSoundEffectAsync.
    int SampleRate { get; }
    // Streams raw PCM chunks; use GenerateSoundEffectFileAsync for a buffered, encoded audio file instead.
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    // Generates the sound effect and returns it as a single buffered, encoded audio file (WAV).
    Task<SoundEffectGeneratorResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
  interface ISoundEffectGeneratorInfo
    bool SupportsLooping { get; }
  class NonRetryableSoundEffectGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class SoundEffectGenerator : ISoundEffectGenerator
    ctor(string modelName)
    ctor(SoundEffectGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SoundEffectGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsLooping { get; }
    void Dispose()
    // Generate a sound-effect file from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateSoundEffectFileAsync when the request needs any other SoundEffectGeneratorConfig field.
    Task<SoundEffectGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a SoundEffectGenerator per call. Returns a buffered WAV file (.Data/.MimeType/.DurationSeconds). Use the constructor + GenerateSoundEffectFileAsync for duration/looping/prompt-influence, or GenerateSoundEffectAsync for streaming PCM chunks.
    static Task<SoundEffectGeneratorResult> GenerateAsync(string prompt, SoundEffectGeneratorModel model = ElevenLabsV2, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<SoundEffectGeneratorResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    static SoundEffectGeneratorCapabilities GetCapabilities(SoundEffectGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SoundEffectGeneratorModel model)
  sealed class SoundEffectGeneratorCapabilities : ISoundEffectGeneratorInfo
    ctor()
    bool SupportsLooping { get; init; }
  sealed record SoundEffectGeneratorConfig
    ctor()
    double? DurationSeconds { get; init; }
    bool Loop { get; init; }
    string Prompt { get; init; }
    double PromptInfluence { get; init; }
    // Applies to the buffered ISoundEffectGenerator.GenerateSoundEffectFileAsync result; the streaming ISoundEffectGenerator.GenerateSoundEffectAsync chunks are unaffected.
    ResultDelivery ResultDelivery { get; init; }
    TimeSpan Timeout { get; init; }
  class SoundEffectGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SoundEffectGeneratorModel
    ElevenLabsV2
  static class SoundEffectGeneratorModelExtensions
    static string DisplayName(this SoundEffectGeneratorModel model)
  // Kind tells how the audio was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record SoundEffectGeneratorResult : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    double DurationSeconds { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }

namespace Ikon.AI.SpeechGeneration
  interface ISpeechGenerator : IDisposable
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
  class NonRetryableSpeechGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class SpeechGenerator : ISpeechGenerator
    ctor(string modelName)
    ctor(SpeechGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    void Dispose()
    // Speak a line of text and collect it into one audio chunk — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateSpeechAsync when you want the chunks as they stream, or any other SpeechGeneratorConfig field.
    Task<AudioChunk> GenerateAsync(string text, string? voice = null, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a SpeechGenerator per call. Defaults to SpeechGeneratorModel.ElevenFlash25; override via model. Pass voice to pick a voice (model default otherwise). Streamed chunks are concatenated into one PCM AudioChunk. Never returns null — throws SpeechGeneratorException on failure or empty output. Use the constructor + GenerateSpeechAsync for chunk-by-chunk streaming or other fields.
    static Task<AudioChunk> GenerateAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechGeneratorModel model)
    static IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> GetVoiceIdsByModel()
  sealed record SpeechGeneratorConfig
    ctor()
    string Instructions { get; init; }
    string Language { get; init; }
    // Speaking-rate multiplier (1.0 = normal); null keeps the model's own default. Honored by OpenAI and Google; ElevenLabs ignores it.
    double? Speed { get; init; }
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
    static string DisplayName(this SpeechGeneratorModel model)
  static class TextFilter
    static string Filter(string text, TextFilter.Config config)
  sealed class TextFilter.Config
    ctor()
    int MaxTextLength { get; set; }
    bool RemoveEmojis { get; set; }
    bool SimplifyUrls { get; set; }
    bool SpeakOnlyFirstParagraph { get; set; }

namespace Ikon.AI.SpeechRecognition
  sealed record AnalyzePronunciationConfig
    ctor()
    int ChannelCount { get; init; }
    string Language { get; init; }
    string ReferenceText { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    TimeSpan Timeout { get; init; }
  interface ISpeechRecognizer : IDisposable, ISpeechRecognizerInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  interface ISpeechRecognizerInfo
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
  class NonRetryableSpeechRecognizerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed record Pronunciation.Break
    ctor()
    int BreakLength { get; init; }
    List<string> ErrorTypes { get; init; }
    Pronunciation.MissingBreak MissingBreak { get; init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get; init; }
  sealed record Pronunciation.Feedback
    ctor()
    Pronunciation.Prosody Prosody { get; init; }
  sealed record Pronunciation.Intonation
    ctor()
    List<string> ErrorTypes { get; init; }
    Pronunciation.Monotone Monotone { get; init; }
  sealed record Pronunciation.MissingBreak
    ctor()
    double Confidence { get; init; }
  sealed record Pronunciation.Monotone
    ctor()
    double SyllablePitchDeltaConfidence { get; init; }
  sealed record Pronunciation.NBest
    ctor()
    double Confidence { get; init; }
    string Display { get; init; }
    string ITN { get; init; }
    string Lexical { get; init; }
    string MaskedITN { get; init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Word> Words { get; init; }
  sealed record Pronunciation.Phoneme
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed record Pronunciation.PhonemePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
  sealed record Pronunciation.PronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    double CompletenessScore { get; init; }
    double FluencyScore { get; init; }
    double PronScore { get; init; }
    double ProsodyScore { get; init; }
  sealed record Pronunciation.Prosody
    ctor()
    Pronunciation.Break Break { get; init; }
    Pronunciation.Intonation Intonation { get; init; }
  sealed record Pronunciation.Result
    ctor()
    int Channel { get; init; }
    string DisplayText { get; init; }
    long Duration { get; init; }
    string Id { get; init; }
    List<Pronunciation.NBest> NBest { get; init; }
    long Offset { get; init; }
    string RecognitionStatus { get; init; }
    double SNR { get; init; }
  sealed record Pronunciation.Syllable
    ctor()
    long Duration { get; init; }
    string Grapheme { get; init; }
    long Offset { get; init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed record Pronunciation.SyllablePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
  sealed record Pronunciation.UnexpectedBreak
    ctor()
    double Confidence { get; init; }
  sealed record Pronunciation.Word
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    List<Pronunciation.Phoneme> Phonemes { get; init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Syllable> Syllables { get; init; }
    string Text { get; init; }
  sealed record Pronunciation.WordPronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    string ErrorType { get; init; }
    Pronunciation.Feedback Feedback { get; init; }
  sealed record RecognizeContinuousSpeechConfig
    ctor()
    string[] CandidateLanguages { get; init; }
    int ChannelCount { get; init; }
    string Language { get; init; }
    int SampleRate { get; init; }
  // Supply the audio exactly one way: raw PCM via Samples or SamplesPcm16 (with SampleRate/ChannelCount), or an encoded audio file via Data (with MimeType), Url, or AssetUri (resolved automatically).
  sealed record RecognizeSpeechConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    int ChannelCount { get; init; }
    byte[]? Data { get; init; }
    string Language { get; init; }
    string? MimeType { get; init; }
    string? Prompt { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    double Temperature { get; init; }
    TimeSpan Timeout { get; init; }
    string? Url { get; init; }
  sealed class SpeechRecognizer : ISpeechRecognizer
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
    // Transcribe a buffer of samples — the instance form of the RecognizeAsync one-shot, for when you already hold a recognizer. Reach for RecognizeBatchSpeechAsync when the request needs any other RecognizeSpeechConfig field (language, prompt, timestamps).
    Task<string> RecognizeAsync(float[] samples, int sampleRate, int channelCount = 1, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a SpeechRecognizer per call. Defaults to SpeechRecognizerModel.WhisperLarge3Turbo; override via model. Returns the recognized text (empty when nothing was recognized). Use the constructor + RecognizeBatchSpeechAsync for a language hint, prompt, or other fields, or RecognizeContinuousSpeechAsync for streaming.
    static Task<string> RecognizeAsync(float[] samples, int sampleRate, SpeechRecognizerModel model = WhisperLarge3Turbo, int channelCount = 1, CancellationToken cancellationToken = default)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerAdapter : ISpeechRecognizer
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
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    // SilenceTriggered mode only: forces recognition after this much continuous speech without a pause. TimeSpan.Zero or negative disables the limit. Defaults to 30s.
    TimeSpan MaxSpeechDuration { get; set; }
    // Defaults to Mode.SilenceTriggered.
    SpeechRecognizerAdapter.Mode Mode { get; set; }
    // Used only in GrowingWindow/SlidingWindow modes (GrowingWindow recognizes all accumulated audio, SlidingWindow only audio since the last run); defaults to 5s.
    TimeSpan RecognitionInterval { get; set; }
    // The timeout for individual speech recognition API requests.
    TimeSpan RequestTimeout { get; set; }
    // SilenceTriggered mode only: a pause of this length flushes accumulated speech for recognition. Defaults to 750ms.
    TimeSpan SilenceDuration { get; set; }
    // The amplitude threshold below which audio is considered silence. Sample values with absolute amplitude below this threshold are treated as silent.
    float SilenceThreshold { get; set; }
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
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
    Gpt4OmniTranscribeDiarize
    DeepgramNova3General
    DeepgramNova3Medical
    AssemblyAIUniversal3ProStreaming
    AssemblyAIUniversalStreamingEnglish
    AssemblyAIUniversalStreamingMultilingual
    VoxtralMiniTranscribe2
  static class SpeechRecognizerModelExtensions
    static string DisplayName(this SpeechRecognizerModel model)

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
    ctor(VectorStoreConfig? config = null)
    Task CreateCollectionAsync(string collectionName, EmbeddingModel model)
    Task<int> GetDataItemCountAsync(string collectionName)
    Task RemoveAsync(string collectionName, IEnumerable<string> tags)
    Task<List<Result<object>>> SearchAsync(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<object>>> SearchAsync(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool>? tagsFilter = null)
    Task<int> SetAsync(string collectionName, int? key, string text, object value, IEnumerable<string>? tags = null)
    Task<int> SetAsync(string collectionName, int? key, float[] vector, object value, IEnumerable<string>? tags = null)
  enum VectorStoreBackend
    // Brute-force in-process store. The default — no external dependency.
    InMemory
    // Postgres + pgvector, with an HNSW index. Scales past what an in-RAM linear scan can.
    PgVector
  // Chooses the backing store for a VectorDatabase. The default (or a null config) keeps the in-memory store, so existing callers are unaffected; pass one with VectorStoreBackend.PgVector to persist and scale.
  sealed class VectorStoreConfig
    ctor()
    VectorStoreBackend Backend { get; init; }
    // Opens a fresh connection for a pgvector operation (each op opens and disposes its own, as PgVectorCorpus does), so the call belongs inside the factory: () => DatabaseConnection.Postgres(...).DbConnection. Required when Backend is VectorStoreBackend.PgVector.
    Func<DbConnection>? ConnectionFactory { get; init; }
    // Table-name prefix, so several vector databases can share one Postgres database.
    string TablePrefix { get; init; }

namespace Ikon.AI.Utils
  static class ImageUtils
    static byte[] ConvertAlphaMaskToBlackWhiteMask(byte[] maskData)
    static byte[] ConvertBlackWhiteMaskToAlphaMask(byte[] maskData)
    // Caps both dimensions at maxDimension (aspect preserved) and re-encodes as JPEG; returns the source bytes unchanged when the image already fits and is at most maxBytes.
    static (byte[] Bytes, string MimeType, int Width, int Height) EncodeJpegCapped(byte[] source, string sourceMimeType, int maxDimension = 1568, int quality = 70, int maxBytes = 204800)
    static (int width, int height) GetImageDimensions(byte[] buffer)
    static byte[] InvertMask(byte[] maskData)
    static bool IsWebP(byte[] data)

namespace Ikon.AI.VideoEnhancement
  interface IVideoEnhancer : IDisposable
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
  class NonRetryableVideoEnhancerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class VideoEnhancer : IVideoEnhancer
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoEnhancerModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // Enhance a video by URL — the instance form of the EnhanceAsync one-shot, for when you already hold an enhancer. Reach for EnhanceVideoAsync when the request needs any other VideoEnhancerConfig field.
    Task<VideoEnhancerResult> EnhanceAsync(string videoUrl, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a VideoEnhancer per call. Defaults to VideoEnhancerModel.TensorPixUpscale2xUltra41; override via model. Returns the enhanced video as a download URL in .Url plus .OutputFps/.OutputSizeBytes. Use the constructor + EnhanceVideoAsync for raw bytes (Data), frame-range trim, target FPS, or other fields.
    static Task<VideoEnhancerResult> EnhanceAsync(string videoUrl, VideoEnhancerModel model = TensorPixUpscale2xUltra41, CancellationToken cancellationToken = default)
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoEnhancerModel model)
  // Supply the video exactly one way: Data (with MimeType), Url, or AssetUri (resolved to a URL automatically).
  sealed record VideoEnhancerConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    int? EndFrame { get; init; }
    string? MimeType { get; init; }
    int? StartFrame { get; init; }
    int? TargetFps { get; init; }
    TimeSpan Timeout { get; init; }
    string? Url { get; init; }
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
    static string DisplayName(this VideoEnhancerModel model)
  sealed record VideoEnhancerResult
    ctor()
    int? OutputFps { get; init; }
    long? OutputSizeBytes { get; init; }
    string Url { get; init; }

namespace Ikon.AI.VideoGeneration
  interface IVideoGenerator : IDisposable, IVideoGeneratorInfo
    Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IVideoGeneratorInfo
    int MaxInputAudios { get; }
    int MaxInputImages { get; }
    int MaxInputVideos { get; }
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
  class NonRetryableVideoGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class VideoGenerator : IVideoGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int MaxInputAudios { get; }
    int MaxInputImages { get; }
    int MaxInputVideos { get; }
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
    // Generate a video from a plain prompt — the instance form of the GenerateAsync one-shot, for when you already hold a generator. Reach for GenerateVideoAsync when the request needs any other VideoGeneratorConfig field.
    Task<VideoGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a VideoGenerator per call. Defaults to VideoGeneratorModel.Veo31Fast; override via model. Returns the result with the generated clip's .Url. Use the constructor + GenerateVideoAsync for image-to-video, length, resolution, aspect ratio, negative prompt, audio, or other fields.
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
    int MaxInputAudios { get; init; }
    int MaxInputImages { get; init; }
    int MaxInputVideos { get; init; }
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
  sealed record VideoGeneratorConfig
    ctor()
    VideoGeneratorAspectRatio AspectRatio { get; init; }
    bool? GenerateAudio { get; init; }
    // Reference audio, for models that accept it. Addressed from the prompt as @Audio1 and so on, in prompt order.
    List<InputAudio> InputAudios { get; init; }
    List<InputImage> InputImages { get; init; }
    // Reference footage, for models that accept it. Addressed from the prompt in the provider's own notation — fal's Seedance uses @Video1, @Video2 in prompt order.
    List<InputVideo> InputVideos { get; init; }
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
    Kling26
    Kling30
    Kling30Omni
    KlingVideoO1
    LumaRay20
    LumaRay20Flash
    Pika22
    Pixverse55
    Pixverse6
    Pollo20
    RunwayGen4
    Seedance15Pro
    Seedance20
    Seedance20Fast
    Seedance20Mini
    Seedance25
    Seedance25Reference
    Sora2
    Sora2Pro
    Veo31
    Veo31Fast
    ViduQ2Pro
    ViduQ2Turbo
    ViduQ3Pro
    ViduQ3Turbo
    Wan26
    Wan27
    GrokImagineVideo
    GrokImagineVideo15
  static class VideoGeneratorModelExtensions
    static string DisplayName(this VideoGeneratorModel model)
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
  sealed record VideoGeneratorResult
    ctor()
    string Url { get; init; }

namespace Ikon.AI.WebScraping
  sealed record Cookie
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
  sealed record DownloadFileConfig
    ctor()
    string CountryCode { get; init; }
    TimeSpan Timeout { get; init; }
    string Url { get; init; }
  sealed record DownloadFileResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Url { get; init; }
  interface IWebScraper : IDisposable, IWebScraperInfo
    Task<DownloadFileResult> DownloadFileAsync(DownloadFileConfig config, CancellationToken cancellationToken = default)
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = default)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = default)
  interface IWebScraperInfo
    bool SupportsFileDownload { get; }
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
  sealed record MultiPageScrapeConfig
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
  class NonRetryableWebScraperException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed record PageResult
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string MimeType { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed record ScreenshotConfig
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
  sealed record ScreenshotResult
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
  sealed record SinglePageScrapeConfig
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
  sealed class WebScraper : IWebScraper
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
    // Scrape one page by URL — the instance form of the ScrapeAsync one-shot, for when you already hold a scraper. Reach for ScrapeSinglePageAsync when the request needs any other SinglePageScrapeConfig field.
    Task<PageResult> ScrapeAsync(string url, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a WebScraper per call. Defaults to WebScraperModel.Jina; override via model. Returns the page as Markdown in .Content plus .Title/.Url. Use the constructor + ScrapeSinglePageAsync for output format/cookies/JS or other fields, or ScrapeMultiplePagesAsync/TakeScreenshotAsync/DownloadFileAsync for crawling, screenshots, and downloads.
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
    static string DisplayName(this WebScraperModel model)
  enum WebScraperOutputFormat
    Text
    Markdown
    Html

namespace Ikon.AI.WebSearching
  interface IWebSearcher : IDisposable, IWebSearcherInfo
    Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
    Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = default)
  interface IWebSearcherInfo
    bool SupportsImageSearching { get; }
  class NonRetryableWebSearcherException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed record SearchConfig
    ctor()
    string CountryCode { get; init; }
    string InSiteUrl { get; init; }
    string Language { get; init; }
    int MaxResults { get; init; }
    WebSearcherOutputFormat OutputFormat { get; init; }
    string Query { get; init; }
    TimeSpan Timeout { get; init; }
  sealed record SearchResult
    ctor()
    string Content { get; init; }
    List<string> Keywords { get; init; }
    string MimeType { get; init; }
    string Title { get; init; }
    string Url { get; init; }
  sealed class WebSearcher : IWebSearcher
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(WebSearcherModel model, IReadOnlyList<ModelRegion>? regions = null)
    bool SupportsImageSearching { get; }
    void Dispose()
    static WebSearcherCapabilities GetCapabilities(WebSearcherModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebSearcherModel model)
    // Web page search for a plain query — the instance form of the SearchAsync one-shot, for when you already hold a searcher. Reach for SearchPagesAsync when the search needs any other SearchConfig field (site restriction, country, language).
    Task<List<SearchResult>> SearchAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a WebSearcher per call. Defaults to WebSearcherModel.Google; override via model. Each SearchResult exposes .Url/.Title/.Content. Use the constructor + SearchPagesAsync for site-restricted search, country/language targeting, or other SearchConfig fields, or SearchImagesAsync (with an image-capable model) for image search.
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
    static string DisplayName(this WebSearcherModel model)
  enum WebSearcherOutputFormat
    Text
    Markdown
    Html
