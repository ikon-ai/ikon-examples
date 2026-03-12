# AI & Emergence

## Ikon.AI - Emergence

Emergence provides structured AI agent patterns with typed outputs.

### Emerge.Run<T> - Basic Pattern

```csharp
public sealed class AnalysisResult
{
    public string Summary { get; set; } = "";
    public List<string> KeyPoints { get; set; } = [];
}

// Streaming (observe each event)
await foreach (var ev in Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, new KernelContext(), pass =>
{
    pass.SystemPrompt = "You are a helpful analyst.";
    pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
    pass.Temperature = 0.7;
    pass.MaxOutputTokens = 32000;
    pass.MaxIterations = 5;
}))
{
    if (ev is Completed<AnalysisResult> completed)
    {
        var result = completed.Result;
    }
}

// Direct result (no streaming)
var (result, context) = await Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, new KernelContext(), pass =>
{
    pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
    pass.Temperature = 0.3;
}).FinalAsync();
```

### Tools

```csharp
pass.AddTool("search", "Search the web", (string query) => SearchWeb(query))
    .AddTool("get_data", "Get statistics", (string topic) => GetData(topic));
pass.MaxToolCalls = 10;
```

### Emerge.BestOf<T> - Generate Multiple Candidates

```csharp
await foreach (var ev in Emerge.BestOf<CreativeResponse>(LLMModel.Claude46Sonnet, new KernelContext(), bo =>
{
    bo.Command = $"Write a tagline for: {prompt}\n\nReturn JSON:\n{bo.JsonSchema}";
    bo.Count = 3;
    bo.Score = (response, trace) => ScoreResponse(response);
    bo.Candidate(c => { c.Temperature = 0.5 + c.Index * 0.2; });
}))
{
    if (ev is Completed<CreativeResponse> completed) { /* best candidate */ }
}
```

Refer to generated docs for full Emergence API (MapReduce, TaskGraph, TreeSearch, TreeOfThought, SolverCriticVerifier, DebateThenJudge, Refine, PlanAndExecute, Router, EnsembleMerge, SelfConsistency, Swarm, TestRefine, ParallelBestOf) and model listings.

## Ikon.AI - Other Capabilities

### Image Generation

```csharp
var imageGenerator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);
var results = await imageGenerator.GenerateImageAsync(new ImageGeneratorConfig
{
    Prompt = "A neon-lit cyberpunk street",
    Width = 512,
    Height = 512
});
if (results.Count > 0) { var image = results[0]; /* image.Data, image.MimeType */ }
```

### Speech Generation (TTS)

```csharp
using var speechGenerator = new SpeechGenerator(SpeechGeneratorModel.ElevenTurbo25);
await foreach (var audio in speechGenerator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = "Hello world" }))
{
    Audio.SendSpeech(audio);
}
```

### Speech Recognition

```csharp
var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
var adapter = new SpeechRecognizerAdapter(recognizer, new SpeechRecognizerAdapter.Config
{
    Mode = SpeechRecognizerAdapter.Mode.SilenceTriggered,
    SilenceDuration = TimeSpan.FromMilliseconds(750),
    SilenceThreshold = 0.01f,
    MaxSpeechDuration = TimeSpan.FromSeconds(30)
});
```

### Other Services

Available: `Classifier`, `EmbeddingGenerator`, `WebSearcher`, `WebScraper`, `Reranker`, `Retriever`, `OCR`, `FileConverter`, `VideoGenerator`, `VideoEnhancer`, `SoundEffectGenerator`. Refer to generated docs for model listings and API details.

---

# Ikon.AI Public API

namespace Ikon.AI
  class IkonAIConnection : AsyncLocalInstance<IkonAIConnection>
    ctor()
    IkonClientConfig ConfigOverride { get;  set; }
    Task ForceReconnectAsync(CancellationToken ct = null)
    Task<IkonClient> GetOrCreateClientAsync(CancellationToken ct = null)
    Task WarmupAsync(CancellationToken ct = null)
    static string ChannelKey
    static string DevelopmentSpaceId
    static string ExternalUserId
    static string ProductionSpaceId
  class ImplementationSelector : AsyncLocalInstance<ImplementationSelector>
    ctor()
    bool ForceLocal { get;  set; }
    bool ForceRemote { get;  set; }
  enum ModelCategory
    Classifier
    Embeddings
    FileConverter
    ImageGenerator
    LLM
    OCR
    Reranker
    SoundEffectGenerator
    SpeechGenerator
    SpeechRecognizer
    VideoEnhancer
    VideoGenerator
    WebScraper
    WebSearcher
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
    Fireworks
    Google
    Groq
    Hyperbolic
    Ikon
    Jina
    Mistral
    Ngrok
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
    ZhipuAI

namespace Ikon.AI.Chat
  sealed class BasicChat : IAsyncDisposable
    ctor(AssetUri shaderUri)
    KernelContext BaseContext { get;  set; }
    int MaxHistoryLength { get;  set; }
    IReadOnlyList<MessageBlock> Messages { get; }
    void AddModelMessage(string text)
    void AddModelMessage(params object[] parts)
    void AddUserMessage(string text)
    void AddUserMessage(params object[] parts)
    void ClearMessages()
    void Continue()
    KernelContext CreateKernelContext()
    ValueTask DisposeAsync()
    IAsyncEnumerable<StreamingResult> GenerateAsync(IEnumerable<ValueTuple<string, object>> parameters = null, IEnumerable<KernelContext> contexts = null, CancellationToken cancellationToken = null)
    Task<T> GenerateObjectAsync<T>(IEnumerable<ValueTuple<string, object>> parameters = null, IEnumerable<KernelContext> contexts = null, CancellationToken cancellationToken = null)
    Task<string> GenerateStringAsync(IEnumerable<ValueTuple<string, object>> parameters = null, IEnumerable<KernelContext> contexts = null, CancellationToken cancellationToken = null)
    T GetState<T>(string key)
    void SetState(string key, object value)
    void StopProcessing()
    event EventHandler<string> RenderedShader

namespace Ikon.AI.Classification
  sealed class ClassificationDetail
    ctor(ClassificationLabel label, string originalCategory, bool isFlagged, double score)
    bool IsFlagged { get;  init; }
    ClassificationLabel Label { get;  init; }
    string OriginalCategory { get;  init; }
    double Score { get;  init; }
  sealed class ClassificationInput
    ctor()
    byte[] Data { get;  init; }
    string MimeType { get;  init; }
    string Text { get;  init; }
    string Url { get;  init; }
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
  sealed class ClassificationResult
    ctor()
    List<ClassificationDetail> Details { get;  init; }
    bool IsFlagged { get;  init; }
    override string ToString()
  sealed class Classifier : IClassifier, IDisposable
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(ClassificationModel model, IReadOnlyList<ModelRegion> regions = null)
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
    string ConnectionString { get;  set; }
    string DataSource { get; }
    string Database { get; }
    string ServerVersion { get; }
    ConnectionState State { get; }
    override void ChangeDatabase(string databaseName)
    override void Close()
    override DataTable GetSchema()
    override DataTable GetSchema(string collectionName)
    override DataTable GetSchema(string collectionName, string[] restrictionValues)
    override void Open()
  class DatabaseConnection.Config
    ctor()
    string EnvVarPrefix { get;  set; }
    DatabaseConnection.SpaceSecret SpaceSecret { get;  set; }
  class DatabaseInfoExtractor.Config
    ctor()
    List<string> ColumnExcludeRegex { get;  set; }
    Dictionary<string, string> ColumnExtraInfo { get;  set; }
    bool IncludeEmptyColumns { get;  set; }
    int JsonSampleLengthLimit { get;  set; }
    int JsonSampleRowLimit { get;  set; }
    int NonTextSampleRowLimit { get;  set; }
    List<string> Schemas { get;  set; }
    List<string> TableExcludeRegex { get;  set; }
    Dictionary<string, string> TableExtraInfo { get;  set; }
    List<string> TableIncludeRegex { get;  set; }
    int TextSampleLengthLimit { get;  set; }
    int TextSampleRowLimit { get;  set; }
  class DatabaseColumnInfo
    ctor()
    string ColumnName { get;  set; }
    string DataType { get;  set; }
    string Description { get;  set; }
    string ExtraInfo { get;  set; }
    string ForeignKeyColumnName { get;  set; }
    string ForeignKeyTableName { get;  set; }
    bool? IsForeignKey { get;  set; }
    bool? IsPrimaryKey { get;  set; }
    List<string> Values { get;  set; }
  class DatabaseConnection
    ctor()
    string BigQueryDataset { get;  set; }
    string BigQueryProjectId { get;  set; }
    DatabaseType DatabaseType { get;  set; }
    DbConnection DbConnection { get;  set; }
    static Task<DatabaseConnection> CreateAsync(DatabaseConnection.Config config)
  class DatabaseInfo
    ctor()
    DatabaseType DatabaseType { get;  set; }
    List<string> ExampleQuestions { get;  set; }
    string SqlCteCommand { get;  set; }
    List<DatabaseTableInfo> Tables { get;  set; }
  class DatabaseInfoExtractor
    ctor(DatabaseConnection databaseConnection)
    Task<DatabaseInfo> ExtractAsync(DatabaseInfoExtractor.Config config, CancellationToken cancellationToken)
    Task<ResultSet> GetCteDatabaseInfoAllValuesAsync(DatabaseInfo cteDatabaseInfo, int maxRows)
    static bool IsText(string dataType)
    Task<DatabaseInfo> ValidateAndFillCteDatabaseInfoAsync(DatabaseInfo cteDatabaseInfo, int maxRowsFilter)
  class DatabaseTableInfo
    ctor()
    List<DatabaseColumnInfo> Columns { get;  set; }
    string Description { get;  set; }
    string ExtraInfo { get;  set; }
    string TableName { get;  set; }
  enum DatabaseType
    Unknown
    PostgreSql
    Sqlite
    BigQuery
    Trino
  class DatabaseConnection.SpaceSecret
    ctor()
    string Prefix { get;  set; }
    string SpaceId { get;  set; }
  static class SqlValidator
    static void ValidateReadOnly(string sql, HashSet<string> allowedTables)

namespace Ikon.AI.Embeddings
  enum EmbeddingEncoding
    Base64
    GzipBase64
  sealed class EmbeddingGenerator : IDisposable, IEmbeddingGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(EmbeddingModel model, IReadOnlyList<ModelRegion> regions = null)
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    void Dispose()
    Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
    static EmbeddingGeneratorCapabilities GetCapabilities(EmbeddingModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(EmbeddingModel model)
  sealed class EmbeddingGeneratorCapabilities
    ctor()
    int EmbeddingVectorSize { get;  init; }
    int MaxInputCount { get;  init; }
  sealed class EmbeddingItem
    ctor(string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, string embedding)
    string Context { get;  init; }
    string Embedding { get;  init; }
    float[] EmbeddingArray { get; }
    EmbeddingEncoding Encoding { get;  init; }
    EmbeddingModel Model { get;  init; }
    EmbeddingType Type { get;  init; }
    static Task<EmbeddingItem> Create(string input, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding, CancellationToken cancellationToken = null)
    static Task<EmbeddingItem> Create(float[] embedding, string context, EmbeddingModel model, EmbeddingType type, EmbeddingEncoding encoding)
  enum EmbeddingModel
    OpenAIAda2
    OpenAI3Small
    OpenAI3Large
    CohereEmbed4
    MistralEmbed
    GeminiEmbedding1
    GoogleTextEmbedding5
    GoogleTextMultilingualEmbedding2
    JinaEmbeddings3
    Voyage35
    Voyage35Lite
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
    static float[] CalculateAverageEmbedding(IList<float[]> embeddings)
    static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateDotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateEuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static List<List<VectorMath.Neighbor>> CalculateKNearestNeighbors(IList<float[]> embeddings, int k)
    static float GetMagnitude(ReadOnlySpan<float> vector)

namespace Ikon.AI.FileConversion
  sealed class ConvertedFile
    ctor()
    byte[] Data { get;  init; }
    string Mimetype { get;  init; }
    string Name { get;  init; }
  sealed class FileConverter : IDisposable, IFileConverter
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(FileConverterModel model, IReadOnlyList<ModelRegion> regions = null)
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    static FileConverterCapabilities GetCapabilities(FileConverterModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(FileConverterModel model)
  sealed class FileConverterCapabilities
    ctor()
  sealed class FileConverterConfig
    ctor()
    AssetUri? AssetUri { get;  set; }
    byte[] Data { get;  set; }
    string FileName { get;  set; }
    TimeSpan Timeout { get;  set; }
    string Url { get;  set; }
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
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(ImageGeneratorModel model, IReadOnlyList<ModelRegion> regions = null)
    void Dispose()
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = null)
    static ImageGeneratorCapabilities GetCapabilities(ImageGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageGeneratorModel model)
  sealed class ImageGeneratorCapabilities
    ctor()
  sealed class ImageGeneratorConfig
    ctor()
    ImageBackground Background { get;  set; }
    int Count { get;  set; }
    int Height { get;  set; }
    List<InputImage> InputImages { get;  set; }
    string NegativePrompt { get;  set; }
    string Prompt { get;  set; }
    ImageQuality Quality { get;  set; }
    SafetyLevel SafetyLevel { get;  set; }
    string SearchPrompt { get;  set; }
    int Seed { get;  set; }
    int Steps { get;  set; }
    string Style { get;  set; }
    TimeSpan Timeout { get;  set; }
    bool UpsamplePrompt { get;  set; }
    int Width { get;  set; }
  enum ImageGeneratorModel
    Dalle3
    GptImage1
    GptImage15
    Imagen3
    Imagen4Fast
    Imagen4Standard
    Imagen4Ultra
    Gemini25FlashImage
    Gemini3ProImage
    Gemini31FlashImage
    Flux1Dev
    Flux1Schnell
    Flux11Pro
    Flux11ProUltra
    Flux11ProUltraRaw
    Flux1KontextPro
    Flux1KontextMax
    Flux1KreaDev
    Flux2Dev
    Flux2Flex
    Flux2Pro
  static class ImageGeneratorModelExtensions
    static string DisplayName(ImageGeneratorModel model)
  sealed class ImageGeneratorResult
    ctor()
    byte[] Data { get;  set; }
    int Height { get;  set; }
    string MimeType { get;  set; }
    int Width { get;  set; }
  enum ImageQuality
    Auto
    Low
    Medium
    High
  sealed class InputImage
    ctor()
    byte[] Data { get;  set; }
    double? MaskDilution { get;  set; }
    string MimeType { get;  set; }
    double? Strength { get;  set; }
    InputImageType Type { get;  set; }
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
    static IAsyncEnumerable<StreamingResult> WithParsedTagsAsync(IAsyncEnumerable<StreamingResult> source, List<string> tagWhitelist = null, List<string> tagBlacklist = null)
    static IAsyncEnumerable<StreamingResult> WithReasoningFromTagAsync(IAsyncEnumerable<StreamingResult> source, string reasoningTagName)
    static IAsyncEnumerable<StreamingResult> WithThrottlingAsync(IAsyncEnumerable<StreamingResult> source, int charsPerSecond, int charsPerUpdate)
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
  class CompletionTokenUsage
    ctor(int inputTokens, int outputTokens)
    int InputTokens { get; }
    int OutputTokens { get; }
  class Document
    ctor(string id, object content, bool isJson = false, Dictionary<string, object> metadata = null)
    object Content { get; }
    string Id { get; }
    bool IsJson { get; }
    Dictionary<string, object> Metadata { get; }
  struct DocumentPart : IMessagePart
    ctor(Document document)
    Document Document { get; }
    MessagePartType Type { get; }
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
    ctor(Function function, object[] parameters, string parametersJson, string callId, string hash, string thoughtSignature = "")
    string CallId { get; }
    Function Function { get; }
    string Hash { get; }
    object[] Parameters { get; }
    string ParametersJson { get; }
    string ThoughtSignature { get; }
  class FunctionResult
    ctor(object result = null, string modelMessagePrefix = null, string modelMessageSuffix = null)
    string ModelMessagePrefix { get;  set; }
    string ModelMessageSuffix { get;  set; }
    object Result { get;  set; }
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
    static JsonNode DeepSerialize(object obj)
    static T GenerateExampleInstance<T>()
    static string GenerateExampleJson<T>()
  static class JsonSchemaGenerator
    static ExpandoObject GenerateJsonSchemaExpandoObject<T>(bool supersetCompatibilityMode = false)
    static string GenerateSchemaString<T>(bool supersetCompatibilityMode = false)
  struct KernelContext : IEquatable<KernelContext>
    ctor()
    ctor(KernelContext? baseContext = null, ImmutableList<Instruction> instructions = null, ImmutableList<MessageBlock> messages = null, ImmutableDictionary<string, Function> functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object jsonSchema = null, string gbnfGrammar = null, string toolPlan = null)
    string AudioOutputVoiceId { get;  init; }
    bool DisableFunctionCalling { get;  init; }
    bool DiscardTextOutputWithFunctionCalls { get;  init; }
    ImmutableDictionary<string, Function> Functions { get;  init; }
    string GbnfGrammar { get;  init; }
    ImmutableList<Instruction> Instructions { get;  init; }
    object JsonSchema { get;  init; }
    bool LogFullRequest { get;  init; }
    bool LogFullResponse { get;  init; }
    int MaxOutputTokens { get;  init; }
    ImmutableList<MessageBlock> Messages { get;  init; }
    ReasoningEffort ReasoningEffort { get;  init; }
    int ReasoningTokenBudget { get;  init; }
    double Temperature { get;  init; }
    TimeSpan Timeout { get;  init; }
    string ToolPlan { get;  init; }
    bool UseAudioOutput { get;  init; }
    bool UseCaching { get;  init; }
    bool UseCitations { get;  init; }
    bool UseJson { get;  init; }
    bool UseStreaming { get;  init; }
    bool UseUserNames { get;  init; }
    KernelContext Add(Instruction instruction)
    KernelContext Add(MessageBlock message)
    static KernelContext Create(IEnumerable<Instruction> instructions = null, IEnumerable<MessageBlock> messages = null, IEnumerable<Function> functions = null, TimeSpan? timeout = null, double? temperature = null, int? maxOutputTokens = null, ReasoningEffort? reasoningEffort = null, int? reasoningTokenBudget = null, bool? useStreaming = null, bool? useJson = null, bool? useCitations = null, bool? useUserNames = null, bool? useAudioOutput = null, string audioOutputVoiceId = null, bool? useCaching = null, bool? disableFunctionCalling = null, bool? discardTextOutputWithFunctionCalls = null, bool? logFullRequest = null, bool? logFullResponse = null, object jsonSchema = null, string gbnfGrammar = null, string toolPlan = null)
    IAsyncEnumerable<StreamingResult> GenerateAsync(ILLM llm, CancellationToken cancellationToken = null)
    KernelContext KeepMessagesMax(int count)
    IAsyncEnumerable<StreamingResult> RecurseAsync(IAsyncEnumerable<StreamingResult> generator, HashSet<string> alreadyCalledFunctions)
    IAsyncEnumerable<StreamingResult> ReturnFunctionCallAsync(string name, string parametersJson, string callId, string thoughtSignature = "")
    IAsyncEnumerable<StreamingResult> RunFunctionAsync(string functionName, object[] parameters, CancellationToken cancellationToken = null)
    KernelContext WithFunctions(IEnumerable<Function> functions, bool replaceExisting = false)
  struct MessageBlock
    ctor(MessageBlockRole role, IMessagePart[] parts, string userName = null)
    ctor(MessageBlockRole role, IEnumerable<IMessagePart> parts, string userName = null)
    ctor(MessageBlockRole role, string message, string userName = null)
    IMessagePart[] Parts { get; }
    MessageBlockRole Role { get; }
    string UserName { get; }
    static MessageBlock? CreateFromObjects(IReadOnlyList<object> inputs, MessageBlockRole role)
    override string ToString()
  enum MessageBlockRole
    User
    Model
    Document
    FunctionResult
  enum MessagePartType
    Text
    Image
    ImageUrl
    Audio
    AudioId
    Document
    FunctionResult
  class OutputAudioId
    ctor(string id)
    string Id { get; }
  class OutputAudioTranscript
    ctor(string transcript)
    string Transcript { get; }
  class Reasoning
    ctor(string text)
    string Text { get; }
  enum ReasoningEffort
    None
    Minimal
    Low
    Medium
    High
  struct StreamingResult
    ctor(object value, string sourceName, string valueTypeName = null)
    string SourceName { get; }
    object Value { get; }
    string ValueTypeName { get; }
  class Tag
    ctor(string name, string content, Dictionary<string, string> attributes = null)
    Dictionary<string, string> Attributes { get; }
    string Content { get; }
    string Name { get; }
  struct TextPart : IMessagePart
    ctor(string content)
    string Content { get; }
    MessagePartType Type { get; }
  class ToolPlan
    ctor(string text)
    string Text { get; }

namespace Ikon.AI.LLM
  interface ILLM : IDisposable, ILLMInfo
    abstract IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext context, CancellationToken cancellationToken = null)
  interface ILLMInfo
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
    bool SupportsDocuments { get; }
    bool SupportsGbnfGrammar { get; }
    bool SupportsInputAudio { get; }
    bool SupportsInputImages { get; }
    bool SupportsJsonSchema { get; }
    bool SupportsOutputAudio { get; }
    bool SupportsParallelToolCalling { get; }
    bool SupportsReasoning { get; }
    bool SupportsStreaming { get; }
    bool UsesInlineReasoning { get; }
  sealed class LLM : IDisposable, ILLM, ILLMInfo
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(LLMModel model, IReadOnlyList<ModelRegion> regions = null)
    int ContextWindowSize { get; }
    string InlineReasoningTagName { get; }
    bool SupportsDocuments { get; }
    bool SupportsGbnfGrammar { get; }
    bool SupportsInputAudio { get; }
    bool SupportsInputImages { get; }
    bool SupportsJsonSchema { get; }
    bool SupportsOutputAudio { get; }
    bool SupportsParallelToolCalling { get; }
    bool SupportsReasoning { get; }
    bool SupportsSingleToolCalling { get; }
    bool SupportsStreaming { get; }
    bool UsesInlineReasoning { get; }
    void Dispose()
    IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext context, CancellationToken cancellationToken = null)
    static LLMCapabilities GetCapabilities(LLMModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(LLMModel model)
  sealed class LLMCapabilities : ILLMInfo
    ctor()
    int ContextWindowSize { get;  init; }
    string InlineReasoningTagName { get;  init; }
    bool SupportsDocuments { get;  init; }
    bool SupportsGbnfGrammar { get;  init; }
    bool SupportsInputAudio { get;  init; }
    bool SupportsInputImages { get;  init; }
    bool SupportsJsonSchema { get;  init; }
    bool SupportsOutputAudio { get;  init; }
    bool SupportsParallelToolCalling { get;  init; }
    bool SupportsReasoning { get;  init; }
    bool SupportsStreaming { get;  init; }
    bool UsesInlineReasoning { get;  init; }
  enum LLMModel
    Gpt4Omni
    Gpt4OmniMini
    Gpt4OmniAudio
    Gpt41
    Gpt41Mini
    Gpt41Nano
    Gpt5
    Gpt5Mini
    Gpt5Nano
    Gpt51
    Gpt52
    Gpt5Codex
    Gpt51Codex
    Gpt51CodexMax
    Gpt52Codex
    Gpt5Pro
    Gpt52Pro
    O3
    O3Mini
    O3Pro
    O4Mini
    Claude41Opus
    Claude45Haiku
    Claude45Sonnet
    Claude45Opus
    Claude46Opus
    Claude46Sonnet
    Gemini20Flash
    Gemini20FlashLite
    Gemini25Flash
    Gemini25Pro
    Gemini3Flash
    Gemini3Pro
    Grok3
    Grok3Mini
    Grok4
    Grok4Fast
    GrokCodeFast1
    MistralSmall
    MistralMedium
    MistralLarge
    MagistralSmall
    MagistralMedium
    PixtralLarge
    Codestral
    DevstralSmall
    DevstralMedium
    CommandR
    CommandA
    KimiK2Thinking
    KimiK2
    KimiK25
    GptOss120B
    Glm47
    Glm5
    NovaPro
    NovaLite
    NovaMicro
    Nova2Lite
  static class LLMModelExtensions
    static string DisplayName(LLMModel model)

namespace Ikon.AI.Legacy
  class Mind : IAsyncDisposable
    ctor()
    Context CurrentUserClientContext { get; }
    string CurrentUserLocale { get; }
    string DefaultModelName { get;  set; }
    string DefaultSecondaryModelName { get;  set; }
    string DefaultUserLocale { get;  set; }
    string DefaultUserName { get;  set; }
    KernelContext KernelContext { get; }
    Task AddModelInput(string text, bool isHistory = false)
    Task AddUserInput(Context clientContext, string userName, string userLocale, IReadOnlyList<object> inputs, bool isHistory = false)
    Task CancelGenerateAnswer()
    void ClearMessageHistory()
    void ClearState()
    ValueTask DisposeAsync()
    Task GenerateAnswer(string command = null, string context = null, string modelMessagePrefix = null, string modelMessageSuffix = null, Context clientContext = null, List<ValueTuple<string, object>> variables = null)
    T GetState<T>(string key)
    T GetState<T>(string key, T defaultValue)
    Task InitializeAsync(MindConfig config, Retriever retriever, string mindUserName, Context hostClientContext, AssetUri? shaderUri = null)
    Mind.ShaderLoadResult LoadShader(string shaderContent)
    Task PostMessage(string text)
    Task RegenerateAnswer(Context clientContext = null)
    Task RequestGenerateAnswer(string command = null, string context = null, string modelMessagePrefix = null, string modelMessageSuffix = null, Context clientContext = null, List<ValueTuple<string, object>> variables = null)
    Task RequestRegenerateAnswer(Context clientContext = null)
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
    Func<Dictionary<string, object>, Task> StateUpdate
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
    string AudioId { get;  set; }
    string ModelMessage { get;  set; }
    string TextResponse { get;  set; }
  class Mind.ShaderLoadResult
    ctor()
    string ErrorMessage
    bool IsSuccess

namespace Ikon.AI.OCR
  enum DocumentType
    General
  interface IOCR : IDisposable
    abstract Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = null)
  sealed class OCR : IDisposable, IOCR
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion> regions = null)
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    static OCRCapabilities GetCapabilities(OCRModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(OCRModel model)
  sealed class OCRBoundingBox
    ctor()
    int PageNumber { get;  init; }
    List<float> Polygon { get;  init; }
  sealed class OCRCapabilities
    ctor()
  sealed class OCRConfig
    ctor()
    AssetUri? AssetUri { get;  set; }
    byte[] Data { get;  set; }
    DocumentType DocumentType { get;  set; }
    TimeSpan Timeout { get;  set; }
    string Url { get;  set; }
  enum OCRModel
    AzureDocumentIntelligence
    MistralOCR
  static class OCRModelExtensions
    static string DisplayName(OCRModel model)
  sealed class OCRPage
    ctor()
    float Height { get;  init; }
    int PageNumber { get;  init; }
    string Unit { get;  init; }
    float Width { get;  init; }
  sealed class OCRParagraph
    ctor()
    List<OCRBoundingBox> BoundingRegions { get;  init; }
    string Content { get;  init; }
  sealed class OCRResult
    ctor()
    List<OCRPage> Pages { get;  init; }
    List<OCRParagraph> Paragraphs { get;  init; }
    string Text { get;  init; }
    List<OCRWord> Words { get;  init; }
  sealed class OCRWord
    ctor()
    OCRBoundingBox BoundingBox { get;  init; }
    float Confidence { get;  init; }
    string Content { get;  init; }

namespace Ikon.AI.Policy
  sealed class CreditLimitChecker : IUsageLimitChecker
    ctor()
    ValueTask<UsageLimitCheckResult> CheckAsync(PolicyCallContext context, object[] args)

namespace Ikon.AI.Reranking
  interface IReranker : IDisposable
    abstract Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = null)
  sealed class RerankItem
    ctor()
    int Index { get;  init; }
    double Score { get;  init; }
  enum RerankModel
    CohereRerank4Fast
    CohereRerank4Pro
    JinaReranker3
    VoyageRerank25
    VoyageRerank25Lite
  static class RerankModelExtensions
    static string DisplayName(RerankModel model)
  sealed class Reranker : IDisposable, IReranker
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(RerankModel model, IReadOnlyList<ModelRegion> regions = null)
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
    override bool Equals(object obj)
    List<ValueTuple<string, string>> GenerateHierarchicalSplitLinks()
    override int GetHashCode()
    override string ToString()
    string Link
    float Score
    List<string> Segments
  class Retriever.ContentMetadata
    ctor()
    DateTime CreatedAt { get;  set; }
    string DocumentTitle { get;  set; }
    string OriginalName { get;  set; }
    string OriginalPath { get;  set; }
    int PageNumber { get;  set; }
    List<int> PageNumbers { get;  set; }
    List<string> TitleHierarchy { get;  set; }
    DateTime UpdatedAt { get;  set; }
  class Retriever.Event
    ctor()
    DateTime Date { get;  set; }
    string Description { get;  set; }
    override string ToString()
    ContentLink Source
  class Retriever.GetContentsOptions
    ctor()
    string ContentPostfixes { get;  set; }
    float CumulativeScoreThreshold { get;  set; }
    int HitCountThreshold { get;  set; }
    int MaxContentCount { get;  set; }
    int MaxSearchResults { get;  set; }
    int MinContentCount { get;  set; }
    float SearchThreshold { get;  set; }
    bool UseCumulativeScore { get;  set; }
    bool UseIdMapper { get;  set; }
  class Retriever.GetContentsOptions2
    ctor()
    bool IncludeFullTexts { get;  set; }
    int MaxRerankResults { get;  set; }
    int MaxSearchResults { get;  set; }
    double RerankThreshold { get;  set; }
    float SearchThreshold { get;  set; }
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
    bool TryGetValue(string keyPath, out object value)
    bool TryGetValueAsObject(string keyPath, out object value)
  class Retriever : IAsyncDisposable
    ctor()
    KernelContext Context { get; }
    IdMapper IdMapper { get; }
    ValueTask DisposeAsync()
    Task<ContentLink[]> Expand(ContentLink[] links)
    Task<ContentLink[]> Expand(ContentLink link)
    Task<Content> GetContent(ContentLink link)
    Retriever.ContentMetadata GetContentMetadata(string metadataId)
    Task<string> GetContents(string query, Retriever.GetContentsOptions options)
    Task<string> GetContents2(string query, Retriever.GetContentsOptions2 options)
    ContentLink Ignore(ContentLink link, string detail)
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
    ScriptableStringValue AfterPass { get;  set; }
    ScriptableStringValue AfterShader { get;  set; }
    ScriptableStringValue BeforePass { get;  set; }
    ScriptableStringValue BeforeShader { get;  set; }
    Dictionary<string, ScriptableStringValue> Listeners
  class FunctionDetailsDictionaryConverter<T> : JsonConverter where T : new(), IFunctionDetails
    ctor()
    override bool CanConvert(Type objectType)
    override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  class History
    ctor()
    ScriptableValue<int> Max { get;  set; }
    ScriptableValue<int> Skip { get;  set; }
  interface IFunctionDetails
    ScriptableValue<bool> Select { get;  set; }
  interface IScriptContext
    abstract void AddFilter(string name, KernelContext context, Function function)
    abstract void AddFunction(string name, KernelContext context, Function function)
    abstract bool ContainsKey(string key)
    abstract IEnumerable<string> GetKeys()
    abstract object GetValue(string key)
    abstract string GetValueAsString(string key)
    abstract void Register<T>()
    abstract void SetValue(string key, object value)
  interface IScriptEngine
    abstract IScriptContext CreateContext()
    abstract bool TryParse(string template, out IScriptTemplate parsedTemplate, out string errorMessage)
  interface IScriptTemplate
    abstract Task<string> RenderAsync(IScriptContext context)
  class ShaderCache.ImplicitShader
    ctor(AssetUri? shaderUri, string callerFilePath, ShaderCache outer)
    IAsyncEnumerable<StreamingResult> GenerateAsync(List<KernelContext> contexts = null, ShaderInvocationContext invocationContext = null, Func<KernelContext, Task<KernelContext>> preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext> contexts = null, ShaderInvocationContext invocationContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext> contexts = null, CancellationToken cancellationToken = null, params ValueTuple<string, object>[] parameters)
    Task<T> GenerateObjectAsync<T>(string cacheKey = null, List<KernelContext> contexts = null, CancellationToken cancellationToken = null, params ValueTuple<string, object>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext> contexts = null, ShaderInvocationContext invocationContext = null, Func<KernelContext, Task<KernelContext>> preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object>[] parameters)
    Task<T> GenerateObjectAsync<T>(List<KernelContext> contexts = null, Func<KernelContext, Task<KernelContext>> preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object>[] parameters)
    Task<T> GenerateObjectAsync<T>(string cacheKey = null, List<KernelContext> contexts = null, Func<KernelContext, Task<KernelContext>> preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object>[] parameters)
    Task<string> GenerateStringAsync(List<KernelContext> contexts = null, ShaderInvocationContext invocationContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object>[] parameters)
    Task<string> GenerateStringAsync(List<KernelContext> contexts = null, ShaderInvocationContext invocationContext = null, Func<KernelContext, Task<KernelContext>> preprocessContext = null, CancellationToken cancellationToken = null, params ValueTuple<string, object>[] parameters)
    Task<Shader> GetShaderAsync()
  class Intent
    ctor()
    History History { get;  set; }
    string Id { get;  set; }
    Dictionary<string, object> Input { get;  set; }
    Misc Misc { get;  set; }
    Model Model { get;  set; }
    List<Pass> Passes { get;  set; }
    ScriptableValue<bool> Select { get;  set; }
  class JTokenConverter
    ctor()
    static object ConvertJTokenToObject(JToken token)
  class Misc
    ctor()
    ScriptableStringValue CitationInsertionCommand { get;  set; }
    ScriptableStringValue CitationUserMessageExtension { get;  set; }
    List<string> FailClassificationLabels { get;  set; }
    ScriptableStringValue FailureMessage { get;  set; }
    ScriptableValue<bool> InsertCitationsBackToModelMessage { get;  set; }
    ScriptableValue<bool> UseTrimming { get;  set; }
  class Model
    ctor()
    ScriptableStringValue AudioOutputVoiceId { get;  set; }
    ScriptableValue<int> CharsPerSecond { get;  set; }
    ScriptableValue<int> CharsPerUpdate { get;  set; }
    ScriptableValue<bool> DisableFunctionCalling { get;  set; }
    ScriptableValue<bool> DiscardTextOutputWithFunctionCalls { get;  set; }
    ScriptableValue<bool> ForceCitations { get;  set; }
    ScriptableStringValue GbnfGrammar { get;  set; }
    ExpandoObject JsonSchema { get;  set; }
    ScriptableStringValue JsonSchemaString { get;  set; }
    ScriptableValue<bool> LogFullRequest { get;  set; }
    ScriptableValue<bool> LogRenderedShader { get;  set; }
    ScriptableValue<int> MaxOutputTokens { get;  set; }
    ScriptableValue<int> MaxRecursionDepth { get;  set; }
    ScriptableStringValue Name { get;  set; }
    ScriptableStringValue ReasoningEffort { get;  set; }
    ScriptableValue<int> ReasoningTokenBudget { get;  set; }
    List<ModelRegion> Regions { get;  set; }
    ScriptableValue<int> RequestTimeoutSeconds { get;  set; }
    ScriptableValue<double> Temperature { get;  set; }
    List<Transform> Transforms { get;  set; }
    ScriptableValue<bool> UseAudioOutput { get;  set; }
    ScriptableValue<bool> UseCaching { get;  set; }
    ScriptableValue<bool> UseCitations { get;  set; }
    ScriptableValue<bool> UseJson { get;  set; }
    ScriptableValue<bool> UseStreaming { get;  set; }
    ScriptableValue<bool> UseThrottling { get;  set; }
    ScriptableValue<bool> UseUserNames { get;  set; }
  class ModelFunctionDetails : IFunctionDetails
    ctor()
    ScriptableStringValue Call { get;  set; }
    ScriptableValue<bool> CallOnlyOnce { get;  set; }
    ScriptableStringValue Description { get;  set; }
    ScriptableValue<bool> InlineCall { get;  set; }
    Dictionary<string, ParameterDetails> Parameters { get;  set; }
    ScriptableStringValue Process { get;  set; }
    ScriptableValue<bool> Select { get;  set; }
    ScriptableStringValue Use { get;  set; }
  class Output
    ctor()
    ScriptableStringValue AfterPass { get;  set; }
    ScriptableStringValue AfterShader { get;  set; }
    ScriptableStringValue BeforePass { get;  set; }
    ScriptableStringValue BeforeShader { get;  set; }
  class ParameterDetails
    ctor()
    object DefaultValue { get;  set; }
    ScriptableStringValue Description { get;  set; }
    ScriptableValue<bool> HasDefaultValue { get;  set; }
    ScriptableStringValue Type { get;  set; }
    ScriptableStringValue Use { get;  set; }
  class Pass
    ctor()
    Actions Actions { get;  set; }
    ScriptableStringValue Command { get;  set; }
    ScriptableStringValue Context { get;  set; }
    History History { get;  set; }
    string Id { get;  set; }
    Dictionary<string, object> Input { get;  set; }
    Misc Misc { get;  set; }
    Model Model { get;  set; }
    Dictionary<string, ModelFunctionDetails> ModelFunctions { get;  set; }
    Output Output { get;  set; }
    ScriptableValue<bool> Select { get;  set; }
    Dictionary<string, TemplateFunctionDetails> TemplateFunctions { get;  set; }
  class ScriptableStringDictionaryConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  class ScriptableStringValue
    ctor(string value = "")
    bool IsScript { get; }
    string Value { get; }
    Task<string> GetValueAsync(Func<string, Task<string>> renderer)
  class ScriptableStringValueConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  class ScriptableValueConverter : JsonConverter
    ctor()
    override bool CanConvert(Type objectType)
    override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  class ScriptableValue<T> where T : struct
    ctor(T value)
    ctor(string script)
    string Script { get; }
    T? Value { get; }
    Task<T> GetValueAsync(Func<string, Task<string>> renderer)
  class Shader
    ctor(string shaderConfigAsJsonString, bool enableRenderedShaderLogging = false)
    Dictionary<string, object> Input { get; }
    static string Escape(string text)
    IAsyncEnumerable<StreamingResult> GenerateAsync(KernelContext? context = null, List<KernelContext> externalContexts = null, Dictionary<string, object> state = null, ShaderInvocationContext invocationContext = null, ExpandoObject implicitJsonSchema = null, string implicitJsonExample = null, IdMapper idMapper = null, string modelUserName = "", string modelMessagePrefix = "", string modelMessageSuffix = "", int iteration = 0, Func<Dictionary<string, object>, Task> stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>> preprocessContext = null, CancellationToken cancellationToken = null)
    Task<T> GenerateObjectAsync<T>(KernelContext? context = null, List<KernelContext> externalContexts = null, Dictionary<string, object> state = null, ShaderInvocationContext invocationContext = null, Func<Dictionary<string, object>, Task> stateUpdateCallback = null, JsonSerializerOptions jsonSerializerOptions = null, Func<KernelContext, Task<KernelContext>> preprocessContext = null, CancellationToken cancellationToken = null)
    Task<string> GenerateStringAsync(KernelContext? context = null, List<KernelContext> externalContexts = null, Dictionary<string, object> state = null, ShaderInvocationContext invocationContext = null, Func<Dictionary<string, object>, Task> stateUpdateCallback = null, Func<KernelContext, Task<KernelContext>> preprocessContext = null, CancellationToken cancellationToken = null)
    void SetActiveState<T>(string key, T value)
    static string Unescape(string text)
    event EventHandler<string> RenderedShader
  class ShaderCache : AsyncLocalInstance<ShaderCache>
    ctor()
    string DefaultSpaceId { get;  set; }
    ShaderCache.ImplicitShader GetImplicitShader(string callerFilePath = "")
  class ShaderConfig
    ctor()
    static object Default { get; }
    History History { get;  set; }
    Dictionary<string, object> Input { get;  set; }
    List<Intent> Intents { get;  set; }
    ScriptableValue<int> MaxLogLineLength { get;  set; }
    ScriptableValue<int> MaxLogSectionLineCount { get;  set; }
    Misc Misc { get;  set; }
    Model Model { get;  set; }
    string ShaderLanguage { get;  set; }
    int? ShaderVersion { get;  set; }
  class ShaderInvocationContext
    ctor()
    string FailureMessage { get; }
    string Reasoning { get; }
  class StyleInvariantComparer : IEqualityComparer<string>
    ctor()
    bool Equals(string x, string y)
    int GetHashCode(string obj)
  class TemplateFunctionDetails : IFunctionDetails
    ctor()
    ScriptableStringValue Name { get;  set; }
    ScriptableValue<bool> Select { get;  set; }
  class Shader.TemplateMessage
    ctor()
    string Content { get;  set; }
    string Role { get;  set; }
  class Transform
    ctor()
    Dictionary<string, object> Config { get;  set; }
    ScriptableStringValue Name { get;  set; }
    ScriptableValue<bool> ProcessInput { get;  set; }
    ScriptableValue<bool> ProcessOutput { get;  set; }
    ScriptableValue<int> WindowOverlap { get;  set; }
    ScriptableValue<int> WindowSize { get;  set; }

namespace Ikon.AI.Shader.Scriban
  class ScribanScriptEngine : IScriptEngine
    ctor()
    IScriptContext CreateContext()
    bool TryParse(string template, out IScriptTemplate parsedTemplate, out string errorMessage)

namespace Ikon.AI.SoundEffectGeneration
  interface ISoundEffectGenerator : IDisposable, ISoundEffectGeneratorInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    abstract IAsyncEnumerable<AudioContainer> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
  interface ISoundEffectGeneratorInfo
    bool SupportsLooping { get; }
  sealed class SoundEffectFileResult
    byte[] AudioData { get;  init; }
    string ContentType { get;  init; }
    double DurationSeconds { get;  init; }
  sealed class SoundEffectGenerator : IDisposable, ISoundEffectGenerator, ISoundEffectGeneratorInfo
    ctor(string modelName, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(SoundEffectGeneratorModel model, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(string modelName, IReadOnlyList<ModelRegion> regions, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(SoundEffectGeneratorModel model, IReadOnlyList<ModelRegion> regions, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
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
    bool SupportsLooping { get;  init; }
  sealed class SoundEffectGeneratorConfig
    double? DurationSeconds { get;  set; }
    bool Loop { get;  set; }
    string Prompt { get;  set; }
    double PromptInfluence { get;  set; }
    TimeSpan Timeout { get;  set; }
  enum SoundEffectGeneratorModel
    ElevenLabsV2
  static class SoundEffectGeneratorModelExtensions
    static string DisplayName(SoundEffectGeneratorModel model)

namespace Ikon.AI.SpeechGeneration
  sealed class TextFilter.Config
    ctor()
    int MaxTextLength { get;  set; }
    bool RemoveEmojis { get;  set; }
    bool SimplifyUrls { get;  set; }
    bool SpeakOnlyFirstParagraph { get;  set; }
  interface ISpeechGenerator : IDisposable
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    abstract IAsyncEnumerable<AudioContainer> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = null)
  sealed class SpeechGenerator : IDisposable, ISpeechGenerator
    ctor(string modelName, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(SpeechGeneratorModel model, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(string modelName, IReadOnlyList<ModelRegion> regions, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion> regions, bool useLocalCache = true, TimeSpan? localCacheExpirationTime = null)
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
    string Instructions { get;  set; }
    string Language { get;  set; }
    string Speed { get;  set; }
    string Text { get;  set; }
    TimeSpan Timeout { get;  set; }
    string VoiceId { get;  set; }
  static class SpeechGeneratorExtensions
    static Task StreamSpeechAsync(ISpeechGenerator speechGenerator, SpeechGeneratorConfig config, Func<AudioContainer, Task> onAudio, CancellationToken cancellationToken = null)
  enum SpeechGeneratorModel
    AzureSpeechService
    OpenAITts1
    OpenAITts1Hd
    Gpt4OmniMiniTts
    ElevenFlash2
    ElevenTurbo2
    ElevenMultilingual2
    ElevenFlash25
    ElevenTurbo25
    Eleven3
    GoogleChirp3
    Gemini25FlashTts
    Gemini25ProTts
  static class SpeechGeneratorModelExtensions
    static string DisplayName(SpeechGeneratorModel model)
  static class TextFilter
    static string Filter(string text, TextFilter.Config config)

namespace Ikon.AI.SpeechRecognition
  sealed class AnalyzePronunciationConfig
    ctor()
    int ChannelCount { get;  set; }
    string Language { get;  set; }
    string ReferenceText { get;  set; }
    int SampleRate { get;  set; }
    float[] Samples { get;  set; }
    TimeSpan Timeout { get;  set; }
  sealed class Pronunciation.Break
    ctor()
    int BreakLength { get;  init; }
    List<string> ErrorTypes { get;  init; }
    Pronunciation.MissingBreak MissingBreak { get;  init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get;  init; }
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    TimeSpan MaxSpeechDuration { get;  set; }
    SpeechRecognizerAdapter.Mode Mode { get;  set; }
    TimeSpan RecognitionInterval { get;  set; }
    TimeSpan RequestTimeout { get;  set; }
    TimeSpan SilenceDuration { get;  set; }
    float SilenceThreshold { get;  set; }
  sealed class Pronunciation.Feedback
    ctor()
    Pronunciation.Prosody Prosody { get;  init; }
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
    List<string> ErrorTypes { get;  init; }
    Pronunciation.Monotone Monotone { get;  init; }
  sealed class Pronunciation.MissingBreak
    ctor()
    double Confidence { get;  init; }
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
  sealed class Pronunciation.Monotone
    ctor()
    double SyllablePitchDeltaConfidence { get;  init; }
  sealed class Pronunciation.NBest
    ctor()
    double Confidence { get;  init; }
    string Display { get;  init; }
    string ITN { get;  init; }
    string Lexical { get;  init; }
    string MaskedITN { get;  init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get;  init; }
    List<Pronunciation.Word> Words { get;  init; }
  sealed class Pronunciation.Phoneme
    ctor()
    long Duration { get;  init; }
    long Offset { get;  init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get;  init; }
    string Text { get;  init; }
  sealed class Pronunciation.PhonemePronunciationAssessment
    ctor()
    double AccuracyScore { get;  init; }
  static class Pronunciation
  sealed class Pronunciation.PronunciationAssessment
    ctor()
    double AccuracyScore { get;  init; }
    double CompletenessScore { get;  init; }
    double FluencyScore { get;  init; }
    double PronScore { get;  init; }
    double ProsodyScore { get;  init; }
  sealed class Pronunciation.Prosody
    ctor()
    Pronunciation.Break Break { get;  init; }
    Pronunciation.Intonation Intonation { get;  init; }
  sealed class RecognizeContinuousSpeechConfig
    ctor()
    string[] CandidateLanguages { get;  set; }
    int ChannelCount { get;  set; }
    string Language { get;  set; }
    int SampleRate { get;  set; }
  sealed class RecognizeSpeechConfig
    ctor()
    int ChannelCount { get;  set; }
    string Language { get;  set; }
    string Prompt { get;  set; }
    int SampleRate { get;  set; }
    float[] Samples { get;  set; }
    double Temperature { get;  set; }
    TimeSpan Timeout { get;  set; }
  sealed class Pronunciation.Result
    ctor()
    int Channel { get;  init; }
    string DisplayText { get;  init; }
    long Duration { get;  init; }
    string Id { get;  init; }
    List<Pronunciation.NBest> NBest { get;  init; }
    long Offset { get;  init; }
    string RecognitionStatus { get;  init; }
    double SNR { get;  init; }
  sealed class SpeechRecognizer : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(SpeechRecognizerModel model, IReadOnlyList<ModelRegion> regions = null)
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
    ctor(ISpeechRecognizer speechRecognizer, SpeechRecognizerAdapter.Config config = null)
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
    bool SupportsBatchRecognition { get;  init; }
    bool SupportsContinuousRecognition { get;  init; }
    bool SupportsPronunciationAnalysis { get;  init; }
  enum SpeechRecognizerModel
    AzureSpeechService
    Whisper2
    WhisperLarge3
    WhisperLarge3Turbo
    Gpt4OmniTranscribe
    Gpt4OmniMiniTranscribe
    DeepgramNova3General
    AssemblyAIUniversalStreaming
    VoxtralMiniTranscribe2
  static class SpeechRecognizerModelExtensions
    static string DisplayName(SpeechRecognizerModel model)
  sealed class Pronunciation.Syllable
    ctor()
    long Duration { get;  init; }
    string Grapheme { get;  init; }
    long Offset { get;  init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get;  init; }
    string Text { get;  init; }
  sealed class Pronunciation.SyllablePronunciationAssessment
    ctor()
    double AccuracyScore { get;  init; }
  sealed class Pronunciation.UnexpectedBreak
    ctor()
    double Confidence { get;  init; }
  sealed class Pronunciation.Word
    ctor()
    long Duration { get;  init; }
    long Offset { get;  init; }
    List<Pronunciation.Phoneme> Phonemes { get;  init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get;  init; }
    List<Pronunciation.Syllable> Syllables { get;  init; }
    string Text { get;  init; }
  sealed class Pronunciation.WordPronunciationAssessment
    ctor()
    double AccuracyScore { get;  init; }
    string ErrorType { get;  init; }
    Pronunciation.Feedback Feedback { get;  init; }

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
    Task<List<Result<object>>> SearchAsync(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool> tagsFilter = null)
    Task<List<Result<object>>> SearchAsync(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool> tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, string query, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool> tagsFilter = null)
    Task<List<Result<T>>> SearchAsync<T>(string collectionName, float[] queryVector, int maxItems, float threshold, Metric metric, Func<IEnumerable<string>, bool> tagsFilter = null)
    Task<int> SetAsync(string collectionName, int? key, string text, object value, IEnumerable<string> tags = null)
    Task<int> SetAsync(string collectionName, int? key, float[] vector, object value, IEnumerable<string> tags = null)

namespace Ikon.AI.Utils
  static class HttpUtils
    static Task<string> DumpHttpRequest(HttpRequestMessage request)
    static Task<string> GetErrorMessage(HttpRequestException exception, HttpResponseMessage response, string modelName)
    static Task<int> GetHttpRequestSize(HttpRequestMessage request)
  static class ImageUtils
    static ValueTuple<int, int> GetImageDimensions(byte[] buffer)

namespace Ikon.AI.VideoEnhancement
  interface IVideoEnhancer : IDisposable
    abstract Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = null)
  sealed class VideoEnhancer : IDisposable, IVideoEnhancer
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(VideoEnhancerModel model, IReadOnlyList<ModelRegion> regions = null)
    void Dispose()
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = null)
    static VideoEnhancerCapabilities GetCapabilities(VideoEnhancerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoEnhancerModel model)
  sealed class VideoEnhancerCapabilities
    ctor()
  sealed class VideoEnhancerConfig
    ctor()
    int? EndFrame { get;  set; }
    string MimeType { get;  set; }
    int? StartFrame { get;  set; }
    int? TargetFps { get;  set; }
    TimeSpan Timeout { get;  set; }
    byte[] VideoData { get;  set; }
    string VideoUrl { get;  set; }
  enum VideoEnhancerModel
    TensorPixFpsBoost
    TensorPixUpscale2xUltra4
    TensorPixUpscale2xUltra41
    TensorPixUpscale4xUltra4
  static class VideoEnhancerModelExtensions
    static string DisplayName(VideoEnhancerModel model)
  sealed class VideoEnhancerResult
    ctor()
    int? OutputFps { get;  init; }
    long? OutputSizeBytes { get;  init; }
    string Url { get;  init; }

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
    byte[] Data { get;  set; }
    string MimeType { get;  set; }
    string Url { get;  set; }
  sealed class VideoGenerator : IDisposable, IVideoGenerator, IVideoGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(VideoGeneratorModel model, IReadOnlyList<ModelRegion> regions = null)
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
    int MaxInputImages { get;  init; }
    VideoGeneratorResolutionMode ResolutionMode { get;  init; }
    IReadOnlyList<int> SupportedLengths { get;  init; }
    IReadOnlyList<VideoGeneratorResolution> SupportedResolutions { get;  init; }
    bool SupportsAudio { get;  init; }
    bool SupportsImageToVideo { get;  init; }
    bool SupportsMultipleImages { get;  init; }
    bool SupportsNegativePrompt { get;  init; }
    bool SupportsSeed { get;  init; }
    bool SupportsTailImage { get;  init; }
    bool SupportsTextToVideo { get;  init; }
  sealed class VideoGeneratorConfig
    ctor()
    VideoGeneratorAspectRatio AspectRatio { get;  set; }
    bool? GenerateAudio { get;  set; }
    List<VideoGeneratorConfig.InputImage> InputImages { get;  set; }
    int Length { get;  set; }
    string NegativePrompt { get;  set; }
    string Prompt { get;  set; }
    VideoGeneratorResolution Resolution { get;  set; }
    int? Seed { get;  set; }
    TimeSpan Timeout { get;  set; }
  enum VideoGeneratorModel
    Hailuo23
    Hailuo23Fast
    Kling26
    KlingVideoO1
    LumaRay20
    LumaRay20Flash
    Pika22
    Pixverse55
    Pollo20
    RunwayGen4
    Seedance15Pro
    Sora2
    Sora2Pro
    Veo31
    Veo31Fast
    ViduQ2Pro
    ViduQ2Turbo
    Wan26
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
    string Url { get;  init; }

namespace Ikon.AI.WebScraping
  sealed class Cookie
    ctor()
    string Domain { get;  set; }
    double ExpirationDate { get;  set; }
    bool HostOnly { get;  set; }
    bool HttpOnly { get;  set; }
    int Id { get;  set; }
    string Name { get;  set; }
    string Path { get;  set; }
    string SameSite { get;  set; }
    bool Secure { get;  set; }
    bool Session { get;  set; }
    string StoreId { get;  set; }
    string Value { get;  set; }
  interface IWebScraper : IDisposable, IWebScraperInfo
    abstract Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = null)
    abstract Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = null)
    abstract Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = null)
  interface IWebScraperInfo
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
  sealed class MultiPageScrapeConfig
    ctor()
    bool AddGivenUrlsToWhitelist { get;  set; }
    bool AllowOnlyGivenUrls { get;  set; }
    List<Cookie> Cookies { get;  set; }
    string CountryCode { get;  set; }
    int DelayMs { get;  set; }
    string ExcludedCSSElements { get;  set; }
    List<string> ExcludedLineStarts { get;  set; }
    List<string> ExcludedWholeLines { get;  set; }
    bool Headless { get;  set; }
    bool IgnoreRobotsTxt { get;  set; }
    bool IncludeLinkedFiles { get;  set; }
    string IncludedCSSElements { get;  set; }
    string JavaScript { get;  set; }
    bool LoadResources { get;  set; }
    string Locale { get;  set; }
    int MaxDepth { get;  set; }
    int MaxPages { get;  set; }
    WebScraperOutputFormat OutputFormat { get;  set; }
    string PlaywrightScript { get;  set; }
    bool RerunIfGivenUrlsMissing { get;  set; }
    TimeSpan SinglePageTimeout { get;  set; }
    TimeSpan Timeout { get;  set; }
    List<string> UrlBlacklist { get;  set; }
    List<string> UrlWhitelist { get;  set; }
    List<string> Urls { get;  set; }
    bool UseReadability { get;  set; }
    bool UseSitemap { get;  set; }
    bool UseSitemapOnly { get;  set; }
    bool UseStreaming { get;  set; }
    TimeSpan WaitAfter { get;  set; }
    WebScraperModel WebScraperModel { get;  set; }
  sealed class PageResult
    ctor()
    string Content { get;  init; }
    List<string> Keywords { get;  init; }
    string Mimetype { get;  init; }
    string Title { get;  init; }
    string Url { get;  init; }
  sealed class ScreenshotConfig
    ctor()
    List<Cookie> Cookies { get;  set; }
    string CountryCode { get;  set; }
    bool FullPage { get;  set; }
    bool Headless { get;  set; }
    int Height { get;  set; }
    string JavaScript { get;  set; }
    string Locale { get;  set; }
    string PlaywrightScript { get;  set; }
    TimeSpan Timeout { get;  set; }
    string Url { get;  set; }
    bool UseCaptchaSolver { get;  set; }
    TimeSpan WaitAfter { get;  set; }
    int Width { get;  set; }
  sealed class ScreenshotResult
    ctor()
    byte[] Data { get;  init; }
    string MimeType { get;  init; }
  sealed class SinglePageScrapeConfig
    ctor()
    List<Cookie> Cookies { get;  set; }
    string CountryCode { get;  set; }
    string ExcludedCSSElements { get;  set; }
    List<string> ExcludedLineStarts { get;  set; }
    List<string> ExcludedWholeLines { get;  set; }
    bool Headless { get;  set; }
    bool IncludeLinkedFiles { get;  set; }
    string IncludedCSSElements { get;  set; }
    string JavaScript { get;  set; }
    bool LoadResources { get;  set; }
    string Locale { get;  set; }
    WebScraperOutputFormat OutputFormat { get;  set; }
    string PlaywrightScript { get;  set; }
    TimeSpan Timeout { get;  set; }
    string Url { get;  set; }
    bool UseCaptchaSolver { get;  set; }
    bool UseReadability { get;  set; }
    TimeSpan WaitAfter { get;  set; }
    WebScraperModel WebScraperModel { get;  set; }
  sealed class WebScraper : IDisposable, IWebScraper, IWebScraperInfo
    ctor(string modelName, bool useLocalCache = false)
    ctor(WebScraperModel model, bool useLocalCache = false)
    ctor(string modelName, IReadOnlyList<ModelRegion> regions, bool useLocalCache = false)
    ctor(WebScraperModel model, IReadOnlyList<ModelRegion> regions, bool useLocalCache = false)
    bool SupportsMultiPageScraping { get; }
    bool SupportsScreenshotting { get; }
    bool SupportsSinglePageScraping { get; }
    void Dispose()
    static WebScraperCapabilities GetCapabilities(WebScraperModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebScraperModel model)
    Task<List<PageResult>> ScrapeMultiplePagesAsync(MultiPageScrapeConfig config, CancellationToken cancellationToken = null)
    Task<PageResult> ScrapeSinglePageAsync(SinglePageScrapeConfig config, CancellationToken cancellationToken = null)
    Task<ScreenshotResult> TakeScreenshotAsync(ScreenshotConfig config, CancellationToken cancellationToken = null)
  sealed class WebScraperCapabilities : IWebScraperInfo
    ctor()
    bool SupportsMultiPageScraping { get;  init; }
    bool SupportsScreenshotting { get;  init; }
    bool SupportsSinglePageScraping { get;  init; }
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
    string CountryCode { get;  set; }
    string InSiteUrl { get;  set; }
    string Language { get;  set; }
    int MaxResults { get;  set; }
    WebSearcherOutputFormat OutputFormat { get;  set; }
    string Query { get;  set; }
    TimeSpan Timeout { get;  set; }
  sealed class SearchResult
    ctor()
    string Content { get;  init; }
    List<string> Keywords { get;  init; }
    string Mimetype { get;  init; }
    string Title { get;  init; }
    string Url { get;  init; }
  sealed class WebSearcher : IDisposable, IWebSearcher, IWebSearcherInfo
    ctor(string modelName, IReadOnlyList<ModelRegion> regions = null)
    ctor(WebSearcherModel model, IReadOnlyList<ModelRegion> regions = null)
    bool SupportsImageSearching { get; }
    void Dispose()
    static WebSearcherCapabilities GetCapabilities(WebSearcherModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(WebSearcherModel model)
    Task<List<SearchResult>> SearchImagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
    Task<List<SearchResult>> SearchPagesAsync(SearchConfig config, CancellationToken cancellationToken = null)
  sealed class WebSearcherCapabilities : IWebSearcherInfo
    ctor()
    bool SupportsImageSearching { get;  init; }
  enum WebSearcherModel
    Spider
    Jina
    Google
    GoogleLight
    GoogleLightImages
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


---

# Ikon.AI Library Overview

This guide summarizes the principal namespaces in the Ikon.AI .NET library for developers building AI-enabled solutions. Each section outlines module responsibilities, supported models, and usage patterns verified by automated tests.

## Emergence

`Ikon.AI.Emergence` is the recommended way to build AI workflows with typed outputs. It provides a streaming-first, C#-idiomatic API for structured object generation, tool calling, and advanced multi-agent patterns. All APIs return `IAsyncEnumerable<EmergeEvent<T>>` and non-streaming usage is achieved via the `.FinalAsync()` extension method. Emergence can target any model listed in the [LLM](#llm) section. See the [Emergence Guide](emergence-guide.md) for the full documentation.

### Object Generation

```csharp
using Ikon.AI.Emergence;
using Ikon.AI.Kernel;
using Ikon.AI.LLM;
using Ikon.Common;

var context = new KernelContext();
context = context.Add(new MessageBlock(MessageBlockRole.User, "Tell me about John Smith."));

var (result, _) = await Emerge.Run<PersonDetails>(LLMModel.Gpt5Mini, context, pass =>
{
    pass.Command = "Return invented personal details about the person the user asked about.";
}).FinalAsync();

Log.Instance.Info($"Result: {Json.To(result)}");

public class PersonDetails
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Occupation { get; set; } = string.Empty;
}
```

Emergence supports 15 multi-agent patterns including `BestOf`, `ParallelBestOf`, `SolverCriticVerifier`, `DebateThenJudge`, `MapReduce`, `Refine`, `PlanAndExecute`, `Router`, `EnsembleMerge`, `TreeOfThought`, `SelfConsistency`, `Swarm`, `TaskGraph`, `TestRefine`, and `TreeSearch`. See the [Emergence Guide](emergence-guide.md) for full documentation on all patterns.

Region support is available via `pass.Regions`:

```csharp
pass.Regions = [ModelRegion.Eu, ModelRegion.Global];
```

## Shaders

> **Note:** For new development, prefer using [Emergence](#emergence) which provides a simpler, code-first API for structured AI outputs.

`Ikon.AI.Shader` provides declarative orchestration for prompt-driven automation. Shaders encapsulate model selection, context policies, and schema expectations while allowing reuse across applications. Shaders can target any model listed in the [LLM](#llm) section.

### Text Generation

Generate structured text using a shader definition stored in code, files, or embedded resources.

```csharp
using Ikon.AI.Kernel;
using Ikon.AI.Shader;
using Ikon.Common;

string shaderSource = @"
{
  ShaderVersion: 2,
  Model: {
    Name: 'Gpt5Mini',
    RequestTimeoutSeconds: 60,
    MaxOutputTokens: 4000,
    ReasoningEffort: 'Medium',
  },
  History: {
    Max: 10,
  },
  Input: {
    AssistantName: 'IkonBot',
  },
  Intents: [
    {
      Id: 'ExampleIntent',
      Passes: [
        {
          Id: 'ExamplePass',
          Context: 'You are a helpful assistant. Your name is {{ AssistantName }}.',
          Command: 'Please answer the user question.',
        }
      ]
    }
  ]
}";

var shader = new Shader.Shader(shaderSource);
var context = new KernelContext();
context = context.Add(new MessageBlock(MessageBlockRole.User, "Hello! What is your name?"));

var stringResult = await shader.GenerateStringAsync(context);
Log.Instance.Info($"Shader string result: {stringResult}");
```

### Object Generation

Emit strongly typed results when the shader is configured for JSON output.

```csharp
using Ikon.AI.Shader;
using Ikon.Common;
using Ikon.Common.Core;

string shaderSource = @"
{
  ShaderVersion: 2,
  Model: {
    Name: 'OpenAI_GPT5Mini',
    RequestTimeoutSeconds: 60,
    MaxOutputTokens: 4000,
    ReasoningEffort: 'Medium',
    LogRenderedShader: true,
    UseJson: true,
  },
  History: {
    Max: 10,
  },
  Input: {
    RequestedName: null,
  },
  Intents: [
    {
      Id: 'ExampleIntent',
      Passes: [
        {
          Id: 'ExamplePass',
          Command: 'Return a JSON object with invented personal details about {{ RequestedName }}. Please give the output in JSON format like this:\n{{ ImplicitJsonExample }}',
        }
      ]
    }
  ]
}";

var shader = new Shader.Shader(shaderSource);
var state = new Dictionary<string, object?>
{
    ["RequestedName"] = "John Smith"
};

var result = await shader.GenerateObjectAsync<ExampleResponse>(state: state);
Log.Instance.Info($"Shader object result: {Json.To(result)}");

private class ExampleResponse
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Occupation { get; set; } = string.Empty;
}
```

### Implicit Shaders

Implicit shaders load their source from embedded resources that share the class name. Save the shader used in `ShaderObjectExampleTest` as `<ClassName>.shader` alongside the corresponding `<ClassName>.cs` file, set the build action to **Embedded Resource**, and access it through `ShaderCache`.

```csharp
var result = await ShaderCache.Instance.GetImplicitShader().GenerateObjectAsync<ExampleResponse>(
    contexts: null,
    cancellationToken: CancellationToken.None,
    ("RequestedName", "John Smith")
);

Log.Instance.Info($"Implicit shader object result: {Json.To(result)}");
```

## LLM

> **Note:** For most use cases, prefer using [Emergence](#emergence) which provides structured outputs and higher-level patterns on top of the LLM layer.

`Ikon.AI.LLM` offers direct, streaming-level access to language models when higher-level orchestration is unnecessary.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

Pass preferred regions as an ordered list to keep inference within a geography. If omitted, the default region is `Global`.

```csharp
using Ikon.AI;
using Ikon.AI.Kernel;
using Ikon.AI.LLM;

var context = new KernelContext();
context = context.Add(new Instruction(InstructionType.Context, "You are a helpful assistant that helps to summarize product release notes."));
context = context.Add(new MessageBlock(MessageBlockRole.User, "Summarise the latest release highlights. Here are the notes: ..."));

using var llm = new LLM.LLM(LLMModel.Gpt5Mini, regions: [ModelRegion.Eu]);

await foreach (var streamingResult in llm.GenerateAsync(context))
{
    Log.Instance.Info($"{streamingResult.SourceName} | {streamingResult.Value.GetType()} | {streamingResult.Value}");
}

var stringResult = await llm.GenerateAsync(context).AsStringAsync();
Log.Instance.Info($"String result: {stringResult}");
```

## ImageGeneration

`Ikon.AI.ImageGeneration.ImageGenerator` creates images with negative prompts, seeding, and resolution controls.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.ImageGeneration;

using var imageGenerator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);

var result = (await imageGenerator.GenerateImageAsync(new ImageGeneratorConfig
{
    Prompt = "A santa dancing in the snow",
    NegativePrompt = "summer",
    Width = 1024,
    Height = 1024,
    Seed = 42
})).First();

using MemoryStream ms = new MemoryStream(result.Data);
using SixLabors.ImageSharp.Image image = await SixLabors.ImageSharp.Image.LoadAsync(ms);
await image.SaveAsPngAsync("santa.png");
```

## VideoGeneration

`Ikon.AI.VideoGeneration.VideoGenerator` renders video clips with configurable length, resolution, and aspect ratio.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.VideoGeneration;

using var generator = new VideoGenerator(VideoGeneratorModel.Pollo20);

var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig
{
    Prompt = "A santa dancing in the snow",
    Resolution = VideoGeneratorResolution.Resolution1080p,
    AspectRatio = VideoGeneratorAspectRatio.Ratio16x9,
    Length = 5
});

Log.Instance.Info($"Video URL: {result.Url}");
```

## VideoEnhancement

`Ikon.AI.VideoEnhancement.VideoEnhancer` upscales and frame-interpolates existing video clips.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.VideoEnhancement;

using var enhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixUpscale4xUltra4);

var result = await enhancer.EnhanceVideoAsync(new VideoEnhancerConfig
{
    VideoUrl = "https://example.com/input.mp4"
});

Log.Instance.Info($"Enhanced video URL: {result.Url}");
```

## SpeechGeneration

`Ikon.AI.SpeechGeneration.SpeechGenerator` streams synthesized speech while exposing supported voice IDs per model.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.SpeechGeneration;
using Ikon.Resonance;

using var speechGenerator = new SpeechGenerator(SpeechGeneratorModel.Gpt4OmniMiniTts);

foreach (var voiceId in speechGenerator.VoiceIds)
{
    Log.Instance.Info($"Voice ID: {voiceId}");
}

List<float> samples = [];

var config = new SpeechGeneratorConfig
{
    VoiceId = "ballad",
    Language = "en-US",
    Instructions = "Speak like a angry pirate.",
    Text = "There once was a ship that put to sea. The name of that ship was a Billy of Tea."
};

await foreach (var audio in speechGenerator.GenerateSpeechAsync(config))
{
    samples.AddRange(audio.Samples);
}

using var wavFile = new WavFile(speechGenerator.SampleRate, speechGenerator.ChannelCount, WavFile.SampleFormat.Float);
wavFile.AddSamples(samples.ToArray());
wavFile.SaveToFile("speech.wav");
```

## SpeechRecognition

`Ikon.AI.SpeechRecognition.SpeechRecognizer` converts audio streams into text with configurable sample rates and languages.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.SpeechRecognition;
using Ikon.Resonance;

var speechRecognizer = new SpeechRecognizer(SpeechRecognizerModel.Whisper2);

var audioBytes = await File.ReadAllBytesAsync("audio.raw");

string text = await speechRecognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
{
    Language = "en-US",
    SampleRate = 16000,
    ChannelCount = 1,
    Samples = AudioUtils.ConvertPcm16ToFloat(audioBytes)
});

Log.Instance.Info($"Recognized speech: '{text}'");
```

## SoundEffectGeneration

`Ikon.AI.SoundEffectGeneration.SoundEffectGenerator` generates sound effects from text prompts.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.SoundEffectGeneration;

using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);

var result = await generator.GenerateSoundEffectFileAsync(new SoundEffectGeneratorConfig
{
    Prompt = "A thunderstorm with heavy rain"
});

await File.WriteAllBytesAsync("thunder.wav", result.AudioData);
```

## WebScraping

`Ikon.AI.WebScraping.WebScraper` fetches and normalizes website content, with options for Markdown extraction and screenshots.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.WebScraping;

var scraper = new WebScraper(WebScraperModel.Jina, useLocalCache: true);

var page = await scraper.ScrapeSinglePageAsync(new SinglePageScrapeConfig
{
    Url = "https://example.com",
    OutputFormat = WebScraperOutputFormat.Markdown
});

Log.Instance.Info($"{page.Title}: {page.Content}...");

var screenshot = await scraper.TakeScreenshotAsync(new ScreenshotConfig
{
    Url = "https://example.com",
    Width = 800,
    Height = 600
});

await File.WriteAllBytesAsync("screenshot.png", screenshot.Data);
```

## WebSearching

`Ikon.AI.WebSearching.WebSearcher` wraps search providers for page and image discovery.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.WebSearching;

var searcher = new WebSearcher(WebSearcherModel.Google);

var results = await searcher.SearchPagesAsync(new SearchConfig
{
    Query = "Finnish ice hockey teams",
    MaxResults = 5
});

foreach (var result in results)
{
    Log.Instance.Info($"{result.Title}: {result.Url}");
}

results = await searcher.SearchImagesAsync(new SearchConfig
{
    Query = "Coffee beans",
    MaxResults = 5
});

foreach (var result in results)
{
    Log.Instance.Info($"{result.Title}: {result.Url}");
}
```

## FileConversion

`Ikon.AI.FileConversion.FileConverter` batches binary document conversions and handles long-running jobs transparently.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.FileConversion;

var fileConverter = new FileConverter(FileConverterModel.ConvertApi);
var convertedFile = await fileConverter.ConvertToPdfAsync(new FileConverterConfig
{
    Data = await File.ReadAllBytesAsync("brochure.docx"),
    FileName = "brochure.docx"
});
await File.WriteAllBytesAsync("brochure.pdf", convertedFile.Data);
```

## OCR

`Ikon.AI.OCR.OCR` extracts selectable text and structural metadata from images or PDFs.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.OCR;

var ocr = new OCR(OCRModel.AzureDocumentIntelligence);
var result = await ocr.AnalyzeDocumentAsync(new OCRConfig
{
    Data = await File.ReadAllBytesAsync("invoice.pdf")
});

Log.Instance.Info(result.Text);
```

## Reranking

`Ikon.AI.Reranking.Reranker` orders candidate documents for relevance to a query to improve retrieval pipelines.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.Reranking;

using var reranker = new Reranker(RerankModel.CohereRerank4Fast);

var items = await reranker.RerankAsync(
    ["Document about AI", "Document about cooking", "Document about space exploration"],
    query: "What is the latest in artificial intelligence?"
);

foreach (var item in items)
{
    Log.Instance.Info($"Index: {item.Index}, Score: {item.Score}");
}
```

## Classification

`Ikon.AI.Classification.Classifier` performs moderation and category detection with score-level transparency per safety label.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.Classification;

using var classifier = new Classifier(ClassificationModel.OpenAIOmniModeration);

var result = await classifier.ClassifyAsync("What a nice weather!");
Log.Instance.Info($"Flagged: {result.IsFlagged}");

result = await classifier.ClassifyAsync("How to kill kittens? (not really!)");
Log.Instance.Info($"Flagged: {result.IsFlagged}");

foreach (var detail in result.Details)
{
    if (detail.IsFlagged)
    {
        Log.Instance.Info($"{detail.Label} ({detail.OriginalCategory}): {detail.Score}");
    }
}
```

## Embeddings

`Ikon.AI.Embeddings.EmbeddingGenerator` creates vector representations for similarity search, clustering, or semantic scoring.

**Supported models:** See the model enum in the auto-generated Ikon.AI Public API reference for the current list (`docs/Ikon.AI/public-api.md` in AI apps).

```csharp
using Ikon.AI.Embeddings;

using var embeddingGenerator = new EmbeddingGenerator(EmbeddingModel.OpenAI3Small);

var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(
    ["Example sentence 1", "Example sentence 2", "Example sentence 3"],
    EmbeddingType.Document
);

foreach (var embedding in embeddings)
{
    Log.Instance.Info($"Embedding length: {embedding.Length}");
}
```

## Kernel

`Ikon.AI.Kernel` supplies shared primitives such as `KernelContext`, `MessageBlock`, and `Instruction` that underpin shaders and direct LLM calls.

## Chat

`Ikon.AI.Chat` provides abstractions for orchestrating multi-turn assistant conversations, including channel routing and history policies.

## Retrieving

`Ikon.AI.Retrieving` includes connectors, caches, and vector store helpers for retrieval-augmented generation flows.

## Database

`Ikon.AI.Database` offers persistence helpers for metadata, embeddings, and job tracking across AI workflows.


---

# Ikon.AI.Emergence Public API

namespace Ikon.AI.Emergence
  sealed class AgentScope<T> : EmergeScope<T> where T : new()
    ctor()
    int Index { get; }
    string Role { get;  set; }
    int? Seed { get;  set; }
  sealed class BestOfOptions<T> : EmergeScope<T> where T : new()
    ctor()
    Func<T, ScoreBreakdown, string> BuildCriticFeedback { get;  set; }
    Action<CandidateScope<T>> CandidateConfig { get;  set; }
    int Count { get;  set; }
    bool CriticMustImprove { get;  set; }
    EmergeScope<T> CriticScope { get; }
    bool EnableCritic { get;  set; }
    Func<T, EmergenceTrace, double> Score { get;  set; }
    void Candidate(Action<CandidateScope<T>> configure)
    void Critic(Action<EmergeScope<T>> configure)
  sealed class CandidateScope<T> : EmergeScope<T> where T : new()
    ctor()
    int Index { get; }
    int? Seed { get;  set; }
  sealed class Completed<T> : EmergeEvent<T>, IEquatable<Completed<T>>
    ctor(T Result, KernelContext Context, EmergenceTrace Trace)
    KernelContext Context { get;  init; }
    T Result { get;  init; }
    EmergenceTrace Trace { get;  init; }
  sealed class DebateThenJudgeOptions<T> : EmergeScope<T> where T : new()
    ctor()
    int DebateRounds { get;  set; }
    Action<AgentScope<T>> DebaterConfig { get;  set; }
    int Debaters { get;  set; }
    EmergeScope<T> JudgeScope { get; }
    void Debater(Action<AgentScope<T>> configure)
    void Judge(Action<EmergeScope<T>> configure)
  static class Emerge
    static IAsyncEnumerable<EmergeEvent<T>> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> BestOf<T>(LLMModel model, KernelContext context, Action<BestOfOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> DebateThenJudge<T>(LLMModel model, KernelContext context, Action<DebateThenJudgeOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> DebateThenJudge<T>(LLMModel model, KernelContext context, Action<DebateThenJudgeOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> EnsembleMerge<T>(LLMModel model, KernelContext context, Action<EnsembleMergeOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<TResult>> MapReduce<TChunk, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TChunk, TResult>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<TResult>> MapReduce<TChunk, TResult>(LLMModel model, KernelContext context, Action<MapReduceOptions<TChunk, TResult>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> ParallelBestOf<T>(LLMModel model, KernelContext context, Action<ParallelBestOfOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> ParallelBestOf<T>(LLMModel model, KernelContext context, Action<ParallelBestOfOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> PlanAndExecute<T>(LLMModel model, KernelContext context, Action<PlanAndExecuteOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> PlanAndExecute<T>(LLMModel model, KernelContext context, Action<PlanAndExecuteOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Refine<T>(LLMModel model, KernelContext context, Action<RefineOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Router<T>(LLMModel model, KernelContext context, Action<RouterOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Router<T>(LLMModel model, KernelContext context, Action<RouterOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Run<T>(LLMModel model, KernelContext context, Action<EmergePass<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> SelfConsistency<T>(LLMModel model, KernelContext context, Action<SelfConsistencyOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> SelfConsistency<T>(LLMModel model, KernelContext context, Action<SelfConsistencyOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> SolverCriticVerifier<T>(LLMModel model, KernelContext context, Action<SolverCriticVerifierOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> SolverCriticVerifier<T>(LLMModel model, KernelContext context, Action<SolverCriticVerifierOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Swarm<T>(LLMModel model, KernelContext context, Action<SwarmOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> Swarm<T>(LLMModel model, KernelContext context, Action<SwarmOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TaskGraph<T>(LLMModel model, KernelContext context, Action<TaskGraphOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TaskGraph<T>(LLMModel model, KernelContext context, Action<TaskGraphOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TestRefine<T>(LLMModel model, KernelContext context, Action<TestRefineOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TestRefine<T>(LLMModel model, KernelContext context, Action<TestRefineOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TreeOfThought<T>(LLMModel model, KernelContext context, Action<TreeOfThoughtOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TreeOfThought<T>(LLMModel model, KernelContext context, Action<TreeOfThoughtOptions<T>> configure, ILLM llm, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<T>> TreeSearch<T>(LLMModel model, KernelContext context, Action<TreeSearchOptions<T>> configure, ILLM llm, CancellationToken ct = null)
  sealed class EmergeChat
    ctor()
  sealed class EmergeEventCallbacks<T>
    ctor()
    Action<T, EmergenceTrace> OnCompleted { get;  set; }
    Action<string> OnStopped { get;  set; }
    Action<string> OnText { get;  set; }
    Action<FunctionCall> OnToolCallPlanned { get;  set; }
    Action<FunctionCall, object> OnToolCallResult { get;  set; }
  static class EmergeEventExtensions
    static IAsyncEnumerable<RunnerEvent> AsRunnerEvents<T>(IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = null)
    static Task<string> DispatchEventsAsync<T>(IAsyncEnumerable<EmergeEvent<T>> events, EmergeEventCallbacks<T> callbacks, CancellationToken ct = null)
    static Task<ValueTuple<T, KernelContext>> FinalAsync<T>(IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = null)
    static Task<ValueTuple<T, KernelContext, EmergenceTrace>> FinalWithTraceAsync<T>(IAsyncEnumerable<EmergeEvent<T>> events, CancellationToken ct = null)
  abstract class EmergeEvent<T> : IEquatable<EmergeEvent<T>>
  static class EmergePassExtensions
    static EmergePass<T> AddTool<T>(EmergePass<T> pass, Function function)
    static EmergePass<T> AddTool<T, TResult>(EmergePass<T> pass, string name, string description, Func<TResult> function)
    static EmergePass<T> AddTool<T, T1, TResult>(EmergePass<T> pass, string name, string description, Func<T1, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, T7, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
    static EmergePass<T> AddTool<T, TResult>(EmergePass<T> pass, string name, string description, Func<Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, TResult>(EmergePass<T> pass, string name, string description, Func<T1, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, T7, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> function)
    static EmergePass<T> AddTool<T, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(EmergePass<T> pass, string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> function)
    static EmergePass<T> AddTools<T>(EmergePass<T> pass, params Function[] functions)
    static EmergePass<T> AddToolsFrom<T>(EmergePass<T> pass, object instance)
    static EmergePass<T> DescribeParams<T>(EmergePass<T> pass, string toolName, Dictionary<string, string> paramDescriptions)
  sealed class EmergePass<T> where T : new()
    ctor()
    bool CaseInsensitiveJson { get;  set; }
    string Command { get;  set; }
    KernelContext Context { get; }
    bool HasFunctionResults { get; }
    bool HasNewFunctionResults { get; }
    bool? IncludeJsonExample { get;  set; }
    bool IsStopped { get; }
    int Iteration { get; }
    string JsonExample { get; }
    string JsonSchema { get; }
    int? MaxIterations { get;  set; }
    int? MaxOutputTokens { get;  set; }
    int? MaxRetries { get;  set; }
    int? MaxToolCalls { get;  set; }
    TimeSpan? MaxWallTime { get;  set; }
    LLMModel? Model { get;  set; }
    bool? OptimizeContext { get;  set; }
    ReasoningEffort? ReasoningEffort { get;  set; }
    int? ReasoningTokenBudget { get;  set; }
    IReadOnlyList<ModelRegion> Regions { get;  set; }
    TimeSpan? RetryDelay { get;  set; }
    int? SkipLastNMessages { get;  set; }
    string StopReason { get; }
    string SystemPrompt { get;  set; }
    double? Temperature { get;  set; }
    TimeSpan? Timeout { get;  set; }
    IList<Function> Tools { get; }
    bool? UseCitations { get;  set; }
    bool UseJson { get;  set; }
    int? UseLastNMessages { get;  set; }
    void Stop(string reason = null)
    void UseLastMessages(int count, int skipLast = 0)
  sealed class EmergeScope : EmergeScopeBase
    ctor()
  abstract class EmergeScopeBase
    string Command { get;  set; }
    bool? IncludeJsonExample { get;  set; }
    int? MaxIterations { get;  set; }
    int? MaxOutputTokens { get;  set; }
    int? MaxRetries { get;  set; }
    int? MaxToolCalls { get;  set; }
    TimeSpan? MaxWallTime { get;  set; }
    LLMModel? Model { get;  set; }
    bool? OptimizeContext { get;  set; }
    ReasoningEffort? ReasoningEffort { get;  set; }
    int? ReasoningTokenBudget { get;  set; }
    IReadOnlyList<ModelRegion> Regions { get;  set; }
    TimeSpan? RetryDelay { get;  set; }
    int? SkipLastNMessages { get;  set; }
    string SystemPrompt { get;  set; }
    double? Temperature { get;  set; }
    TimeSpan? Timeout { get;  set; }
    IList<Function> Tools { get; }
    bool? UseCitations { get;  set; }
    int? UseLastNMessages { get;  set; }
    void UseLastMessages(int count, int skipLast = 0)
  class EmergeScope<T> : EmergeScopeBase where T : new()
    ctor()
    bool CaseInsensitiveJson { get;  set; }
    string JsonExample { get; }
    string JsonSchema { get; }
    bool UseJson { get;  set; }
  struct EmergenceBudget : IEquatable<EmergenceBudget>
    ctor()
    ctor(int maxIterations, int maxToolCalls, TimeSpan maxWallTime)
    static EmergenceBudget Default { get; }
    int MaxIterations { get;  init; }
    int MaxToolCalls { get;  init; }
    TimeSpan MaxWallTime { get;  init; }
    static EmergenceBudget Unlimited { get; }
  sealed class EmergenceCallInfo
    ctor()
    string CallId { get;  init; }
    TimeSpan? Duration { get;  set; }
    string Error { get;  set; }
    long InputTokens { get;  set; }
    string Model { get;  init; }
    long OutputTokens { get;  set; }
    string Pattern { get;  init; }
    string ResultType { get;  init; }
    DateTime StartedAt { get;  init; }
    string StopReason { get;  set; }
    bool? Success { get;  set; }
    Dictionary<string, string> Tags { get;  init; }
  static class EmergenceMonitor
    static bool HasObservers { get; }
    static void AddObserver(IEmergenceObserver observer)
    static void ClearObservers()
    static void RemoveObserver(IEmergenceObserver observer)
    static void SetSoleObserver(IEmergenceObserver observer)
    static IDisposable WithTags(Dictionary<string, string> tags)
  class EmergenceMonitorState : IEmergenceObserver
    ctor()
    IReadOnlyList<EmergenceCallInfo> Calls { get; }
    void Clear()
    void OnCallCompleted(EmergenceCallInfo call)
    void OnCallStarted(EmergenceCallInfo call)
    void OnEvent(EmergenceCallInfo call, EmergenceObserverEvent evt)
    event Action Changed
  abstract class EmergenceObserverEvent : IEquatable<EmergenceObserverEvent>
  enum EmergenceStatus
    Completed
    Stopped
    Failed
  sealed class EmergenceTrace : IEquatable<EmergenceTrace>
    ctor()
    ctor(int iterations, int toolCalls, TimeSpan duration, IReadOnlyList<FunctionCall> toolCallHistory = null, string finishReason = null, Exception error = null, long inputTokens = 0, long outputTokens = 0)
    TimeSpan Duration { get;  init; }
    Exception Error { get;  init; }
    string FinishReason { get;  init; }
    long InputTokens { get;  init; }
    bool IsTruncated { get; }
    int Iterations { get;  init; }
    long OutputTokens { get;  init; }
    IReadOnlyList<FunctionCall> ToolCallHistory { get;  init; }
    int ToolCalls { get;  init; }
  sealed class EnsembleMergeOptions<T> : EmergeScope<T> where T : new()
    ctor()
    int MaxParallel { get;  set; }
    EmergeScope<T> MergerScope { get; }
    Action<AgentScope<T>> SolverConfig { get;  set; }
    int SolverCount { get;  set; }
    void Merger(Action<EmergeScope<T>> configure)
    void Solver(Action<AgentScope<T>> configure)
  sealed class ExecutionPlan
    ctor()
    List<PlanStep> Steps { get;  set; }
    string Summary { get;  set; }
  class FoundSection
    ctor()
    string Content { get;  set; }
    string NodeId { get;  set; }
    int? Page { get;  set; }
    string Path { get;  set; }
    string Relevance { get;  set; }
  interface IEmergenceObserver
    abstract void OnCallCompleted(EmergenceCallInfo call)
    abstract void OnCallStarted(EmergenceCallInfo call)
    abstract void OnEvent(EmergenceCallInfo call, EmergenceObserverEvent evt)
  static class KernelContextExtensions
    static IReadOnlyList<FunctionCall> GetFunctionCalls(KernelContext ctx, int take = 10)
    static IReadOnlyList<FunctionResultPart> GetFunctionResults(KernelContext ctx, int take = 10)
    static bool HasFunctionResults(KernelContext ctx)
  sealed class MapReduceOptions<TChunk, TResult> : EmergeScope<TResult> where TChunk : new() where TResult : new()
    ctor()
    IReadOnlyList<object> Chunks { get;  set; }
    object Input { get;  set; }
    EmergeScope<TChunk> MapScope { get; }
    int MaxParallel { get;  set; }
    EmergeScope<TResult> ReduceScope { get; }
    Func<object, IEnumerable<object>> Split { get;  set; }
    void Map(Action<EmergeScope<TChunk>> configure)
    void Reduce(Action<EmergeScope<TResult>> configure)
  sealed class ModelText<T> : EmergeEvent<T>, IEquatable<ModelText<T>>
    ctor(string Text)
    string Text { get;  init; }
  class NavigationDecision
    ctor()
    bool Complete { get;  set; }
    string Reasoning { get;  set; }
  sealed class ObserverCompletedEvent : EmergenceObserverEvent, IEquatable<ObserverCompletedEvent>
    ctor(EmergenceTrace Trace)
    EmergenceTrace Trace { get;  init; }
  sealed class ObserverProgressEvent : EmergenceObserverEvent, IEquatable<ObserverProgressEvent>
    ctor(string Message)
    string Message { get;  init; }
  sealed class ObserverRetryEvent : EmergenceObserverEvent, IEquatable<ObserverRetryEvent>
    ctor(string Reason, int Attempt, int MaxAttempts)
    int Attempt { get;  init; }
    int MaxAttempts { get;  init; }
    string Reason { get;  init; }
  sealed class ObserverStageEvent : EmergenceObserverEvent, IEquatable<ObserverStageEvent>
    ctor(string Name)
    string Name { get;  init; }
  sealed class ObserverStoppedEvent : EmergenceObserverEvent, IEquatable<ObserverStoppedEvent>
    ctor(string Reason)
    string Reason { get;  init; }
  sealed class ObserverTextEvent : EmergenceObserverEvent, IEquatable<ObserverTextEvent>
    ctor(string Text)
    string Text { get;  init; }
  sealed class ObserverTokenEvent : EmergenceObserverEvent, IEquatable<ObserverTokenEvent>
    ctor(long InputTokens, long OutputTokens)
    long InputTokens { get;  init; }
    long OutputTokens { get;  init; }
  sealed class ObserverToolCallPlannedEvent : EmergenceObserverEvent, IEquatable<ObserverToolCallPlannedEvent>
    ctor(string FunctionName, string ParametersJson)
    string FunctionName { get;  init; }
    string ParametersJson { get;  init; }
  sealed class ObserverToolCallResultEvent : EmergenceObserverEvent, IEquatable<ObserverToolCallResultEvent>
    ctor(string FunctionName, string ResultSummary)
    string FunctionName { get;  init; }
    string ResultSummary { get;  init; }
  sealed class ParallelBestOfOptions<T> : EmergeScope<T> where T : new()
    ctor()
    Func<T, ScoreBreakdown, string> BuildCriticFeedback { get;  set; }
    Action<CandidateScope<T>> CandidateConfig { get;  set; }
    int Count { get;  set; }
    bool CriticMustImprove { get;  set; }
    EmergeScope<T> CriticScope { get; }
    bool EnableCritic { get;  set; }
    int MaxParallel { get;  set; }
    Func<T, EmergenceTrace, double> Score { get;  set; }
    void Candidate(Action<CandidateScope<T>> configure)
    void Critic(Action<EmergeScope<T>> configure)
  sealed class PlanAndExecuteOptions<T> : EmergeScope<T> where T : new()
    ctor()
    EmergeScope<T> ExecutorScope { get; }
    int MaxSteps { get;  set; }
    EmergeScope<ExecutionPlan> PlannerScope { get; }
    void Executor(Action<EmergeScope<T>> configure)
    void Planner(Action<EmergeScope<ExecutionPlan>> configure)
  sealed class PlanRevision
    ctor()
    List<TaskNode> NewTasks { get;  set; }
    string Reasoning { get;  set; }
    Dictionary<string, string> TaskUpdates { get;  set; }
    List<string> TasksToCancel { get;  set; }
  sealed class PlanStep
    ctor()
    string Description { get;  set; }
    bool RequiresTool { get;  set; }
    string ToolName { get;  set; }
  sealed class Progress<T> : EmergeEvent<T>, IEquatable<Progress<T>>
    ctor(string Message)
    string Message { get;  init; }
  sealed class RefineOptions<T> : EmergeScope<T> where T : new()
    ctor()
    EmergeScope<T> InitialScope { get; }
    int MaxRefinements { get;  set; }
    EmergeScope<T> RefinementScope { get; }
    Func<T, EmergenceTrace, Task<bool>> ShouldContinue { get;  set; }
    void Initial(Action<EmergeScope<T>> configure)
    void Refinement(Action<EmergeScope<T>> configure)
  sealed class Retry<T> : EmergeEvent<T>, IEquatable<Retry<T>>
    ctor(string Reason, int AttemptNumber, int MaxAttempts)
    int AttemptNumber { get;  init; }
    int MaxAttempts { get;  init; }
    string Reason { get;  init; }
  sealed class ReviewFeedback
    ctor()
    double FitnessScore { get;  set; }
    List<string> Insights { get;  set; }
    List<string> Issues { get;  set; }
    string Reasoning { get;  set; }
    bool SuggestPlanRevision { get;  set; }
  sealed class Route
    ctor()
    Action<EmergeScopeBase> Configure { get;  set; }
    string Description { get;  set; }
    LLMModel? Model { get;  set; }
    string Name { get;  set; }
  sealed class RouterDecision
    ctor()
    string Reasoning { get;  set; }
    string SelectedRoute { get;  set; }
  sealed class RouterOptions<T> : EmergeScope<T> where T : new()
    ctor()
    EmergeScope<RouterDecision> RouterScope { get; }
    List<Route> Routes { get; }
    void AddRoute(string name, string description, LLMModel? model = null, Action<EmergeScopeBase> configure = null)
    void Router(Action<EmergeScope<RouterDecision>> configure)
  sealed class RunnerCompletedEvent : RunnerEvent, IEquatable<RunnerCompletedEvent>
    ctor(string FinalText)
    string FinalText { get;  init; }
  sealed class RunnerErrorEvent : RunnerEvent, IEquatable<RunnerErrorEvent>
    ctor(string Error)
    string Error { get;  init; }
  abstract class RunnerEvent : IEquatable<RunnerEvent>
  sealed class RunnerTextEvent : RunnerEvent, IEquatable<RunnerTextEvent>
    ctor(string Text)
    string Text { get;  init; }
  sealed class RunnerToolPlannedEvent : RunnerEvent, IEquatable<RunnerToolPlannedEvent>
    ctor(string ToolName, string ParametersJson)
    string ParametersJson { get;  init; }
    string ToolName { get;  init; }
  sealed class RunnerToolResultEvent : RunnerEvent, IEquatable<RunnerToolResultEvent>
    ctor(string ToolName, string Result, bool IsError)
    bool IsError { get;  init; }
    string Result { get;  init; }
    string ToolName { get;  init; }
  sealed class ScoreBreakdown
    ctor()
    IReadOnlyList<ScoreMetric> Metrics { get;  init; }
    double TotalScore { get;  init; }
    ScoreMetric Weakest { get;  init; }
    string FormatBreakdown()
  sealed class ScoreBreakdownBuilder<T>
    ctor()
    ScoreBreakdownBuilder<T> Metric(string name, double weight, Func<T, double> evaluate)
    ScoreBreakdown Score(T value)
  sealed class ScoreMetric
    ctor()
    string Name { get;  init; }
    double Score { get;  init; }
    double Weight { get;  init; }
    double WeightedScore { get; }
  sealed class SelfConsistencyOptions<T> : EmergeScope<T> where T : new()
    ctor()
    int MaxParallel { get;  set; }
    Action<CandidateScope<T>> SampleConfig { get;  set; }
    int Samples { get;  set; }
    Func<IReadOnlyList<T>, T> SelectMajority { get;  set; }
    void Sample(Action<CandidateScope<T>> configure)
  sealed class SolverCriticVerifierOptions<T> : EmergeScope<T> where T : new()
    ctor()
    EmergeScope CriticScope { get; }
    int MaxRounds { get;  set; }
    EmergeScope<T> SolverScope { get; }
    EmergeScope<T> VerifierScope { get; }
    void Critic(Action<EmergeScope> configure)
    void Solver(Action<EmergeScope<T>> configure)
    void Verifier(Action<EmergeScope<T>> configure)
  sealed class Stage<T> : EmergeEvent<T>, IEquatable<Stage<T>>
    ctor(string Name)
    string Name { get;  init; }
  sealed class Stopped<T> : EmergeEvent<T>, IEquatable<Stopped<T>>
    ctor(KernelContext Context, string Reason)
    KernelContext Context { get;  init; }
    string Reason { get;  init; }
  sealed class SwarmAgent<T> where T : new()
    ctor()
    List<string> DependsOn { get;  set; }
    string Id { get;  set; }
    string Role { get;  set; }
    EmergeScope<T> Scope { get; }
  sealed class SwarmOptions<T> : EmergeScope<T> where T : new()
    ctor()
    List<SwarmAgent<T>> Agents { get; }
    EmergeScope<T> CoordinatorScope { get; }
    int MaxParallel { get;  set; }
    int MaxRounds { get;  set; }
    Func<IReadOnlyList<T>, T> Merge { get;  set; }
    void AddAgent(string role, Action<EmergeScope<T>> configure)
    void Coordinator(Action<EmergeScope<T>> configure)
  sealed class TaskGraphOptions<T> : EmergeScope<T> where T : new()
    ctor()
    bool EnableParallelReview { get;  set; }
    int MaxParallel { get;  set; }
    Func<string, Task> OnHumanFeedback { get;  set; }
    Action<PlanRevision> OnPlanRevised { get;  set; }
    Action<ReviewFeedback> OnReviewCompleted { get;  set; }
    Action<TaskNode, object> OnTaskCompleted { get;  set; }
    EmergeScope<PlanRevision> PlanReviserScope { get; }
    int ReviewIntervalTasks { get;  set; }
    EmergeScope<ReviewFeedback> ReviewerScope { get; }
    EmergeScope<T> SynthesizerScope { get; }
    List<TaskNode> Tasks { get; }
    EmergeScope<T> WorkerScope { get; }
    void AddTask(string id, string description, params string[] blockedBy)
    void PlanReviser(Action<EmergeScope<PlanRevision>> configure)
    void Reviewer(Action<EmergeScope<ReviewFeedback>> configure)
    void Synthesizer(Action<EmergeScope<T>> configure)
    void Worker(Action<EmergeScope<T>> configure)
  sealed class TaskNode
    ctor()
    List<string> BlockedBy { get;  set; }
    List<string> Blocks { get;  set; }
    string Description { get;  set; }
    string Error { get;  set; }
    string Id { get;  set; }
    string Owner { get;  set; }
    object Result { get;  set; }
    string Status { get;  set; }
  sealed class TestRefineFeedback
    ctor()
    bool Continue { get;  set; }
    string Feedback { get;  set; }
    ScoreBreakdown Score { get;  set; }
  sealed class TestRefineOptions<T> : EmergeScope<T> where T : new()
    ctor()
    Func<T, int, Task> Apply { get;  set; }
    Func<T, int, Task<TestRefineFeedback>> Evaluate { get;  set; }
    EmergeScope<T> InitialScope { get; }
    int MaxIterations { get;  set; }
    EmergeScope<T> RefinementScope { get; }
    void Initial(Action<EmergeScope<T>> configure)
    void Refinement(Action<EmergeScope<T>> configure)
  sealed class ThoughtNode<T> where T : new()
    ctor()
    List<ThoughtNode<T>> Children { get; }
    int Depth { get;  set; }
    ThoughtNode<T> Parent { get;  set; }
    string Reasoning { get;  set; }
    double Score { get;  set; }
    T Value { get;  set; }
  sealed class TokenUpdate<T> : EmergeEvent<T>, IEquatable<TokenUpdate<T>>
    ctor(long InputTokens, long OutputTokens)
    long InputTokens { get;  init; }
    long OutputTokens { get;  init; }
  sealed class ToolCallPlanned<T> : EmergeEvent<T>, IEquatable<ToolCallPlanned<T>>
    ctor(FunctionCall Call)
    FunctionCall Call { get;  init; }
  sealed class ToolCallResult<T> : EmergeEvent<T>, IEquatable<ToolCallResult<T>>
    ctor(FunctionCall Call, StreamingResult[] StreamingResults, object Result)
    FunctionCall Call { get;  init; }
    object Result { get;  init; }
    StreamingResult[] StreamingResults { get;  init; }
  sealed class TreeOfThoughtOptions<T> : EmergeScope<T> where T : new()
    ctor()
    int BeamWidth { get;  set; }
    int BranchingFactor { get;  set; }
    Func<T, EmergenceTrace, double> Evaluate { get;  set; }
    EmergeScope<T> EvaluatorScope { get; }
    int MaxDepth { get;  set; }
    EmergeScope<T> ThoughtScope { get; }
    void Evaluator(Action<EmergeScope<T>> configure)
    void Thought(Action<EmergeScope<T>> configure)
  sealed class TreeSearchOptions<T> : EmergeScope<T> where T : new()
    ctor()
    TreeIndex Index { get;  set; }
    int MaxResults { get;  set; }
    int MaxSteps { get;  set; }
    EmergeScope<NavigationDecision> NavigatorScope { get; }
    string Query { get;  set; }
    void Navigator(Action<EmergeScope<NavigationDecision>> configure)
  class TreeSearchResult
    ctor()
    string ReasoningTrace { get;  set; }
    List<FoundSection> Sections { get;  set; }

namespace Ikon.AI.Emergence.Structured
  sealed class StructuredTagParser.ParsedBlock : IEquatable<StructuredTagParser.ParsedBlock>
    ctor(string TagName, string Content, int StartIndex, int EndIndex)
    string Content { get;  init; }
    int EndIndex { get;  init; }
    int StartIndex { get;  init; }
    string TagName { get;  init; }
  sealed class StructuredTagParser.ParsedResponse : IEquatable<StructuredTagParser.ParsedResponse>
    ctor(string PlainText, IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks)
    IReadOnlyList<StructuredTagParser.ParsedBlock> Blocks { get;  init; }
    string PlainText { get;  init; }
  static class StructuredTagParser
    static string GetTagContent(string content, string tagName)
    static bool HasTag(string content, string tagName)
    static StructuredTagParser.ParsedResponse Parse(string content, params string[] tagNames)

namespace Ikon.AI.Emergence.Tree
  class ContentSection : IEquatable<ContentSection>
    ctor(string Title, string Content, int? Page = null)
    string Content { get;  init; }
    int? Page { get;  init; }
    string Title { get;  init; }
  interface IContentReader
    abstract IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = null)
  class StringContentReader : IContentReader
    ctor(string content)
    IAsyncEnumerable<ContentSection> ReadSectionsAsync(CancellationToken ct = null)
  class TreeIndex
    ctor()
    ctor(TreeNode root)
    TreeNode Root { get;  set; }
    static IAsyncEnumerable<EmergeEvent<TreeIndex>> BuildAsync(LLMModel model, string content, TreeIndexOptions options = null, CancellationToken ct = null)
    static IAsyncEnumerable<EmergeEvent<TreeIndex>> BuildAsync(LLMModel model, IContentReader reader, TreeIndexOptions options = null, CancellationToken ct = null)
    TreeNode FindById(string id)
    void RebuildIndex()
    string ToTableOfContents(int maxDepth = -1)
    IEnumerable<TreeNode> Traverse()
  class TreeIndexOptions
    ctor()
    bool GenerateSummaries { get;  set; }
    int MaxDepth { get;  set; }
    int MaxSummaryTokens { get;  set; }
  class TreeNode
    ctor()
    ctor(string id, string title, string content = "")
    List<TreeNode> Children { get; }
    string Content { get;  set; }
    int Depth { get; }
    string Id { get;  set; }
    int? Page { get;  set; }
    TreeNode Parent { get; }
    string Summary { get;  set; }
    string Title { get;  set; }
    void AddChild(TreeNode child)
    string GetPath()
    IEnumerable<TreeNode> Traverse()


---

# Ikon.AI.Emergence Guide

Ikon.AI.Emergence is a streaming-first, C#-idiomatic library for building AI workflows with typed JSON outputs. It provides a collection of patterns for common AI tasks, from simple single-shot generation to complex multi-agent orchestration.

## Core Concepts

### Streaming-First Design

All APIs return `IAsyncEnumerable<EmergeEvent<T>>`. Non-streaming usage is achieved via the `.FinalAsync()` extension method.

```csharp
// Streaming - observe progress
await foreach (var ev in Emerge.Run<MyType>(model, ctx, pass => { ... }))
{
    switch (ev)
    {
        case ModelText<MyType> t: Console.Write(t.Text); break;
        case ToolCallPlanned<MyType> tc: Console.WriteLine($"Calling {tc.Call.Function.Name}"); break;
        case Completed<MyType> done: Console.WriteLine($"Result: {done.Result}"); break;
    }
}

// Non-streaming - just get the result
var (result, context) = await Emerge.Run<MyType>(model, ctx, pass => { ... }).FinalAsync();
```

### Event Types

| Event | Description |
|-------|-------------|
| `ModelText<T>` | Streaming text chunk from the model |
| `ToolCallPlanned<T>` | Tool call detected (contains `FunctionCall`) |
| `ToolCallResult<T>` | Tool execution completed (contains `FunctionCall`, `StreamingResult[]`, result) |
| `Stage<T>` | Pattern stage boundary (e.g., "Solver", "Critic") |
| `Progress<T>` | Progress message |
| `Retry<T>` | Retry attempt (contains `Reason`, `AttemptNumber`, `MaxAttempts`) |
| `TokenUpdate<T>` | Token usage update (contains `InputTokens`, `OutputTokens`) |
| `Completed<T>` | Final result with `Result`, `Context`, and `Trace` |
| `Stopped<T>` | Execution stopped (budget exceeded, user stop, etc.) with optional `Reason` |

### Typed JSON Output

All patterns produce typed results. The library automatically generates JSON schemas and examples for your types:

```csharp
public class AnalysisResult
{
    public string Summary { get; set; } = "";
    public List<string> KeyPoints { get; set; } = [];
    public float Confidence { get; set; }
}

var (result, _) = await Emerge.Run<AnalysisResult>(model, ctx, pass =>
{
    pass.Command = "Analyze the following text and provide structured output.";
}).FinalAsync();

// result.Summary, result.KeyPoints, result.Confidence are typed
```

### Configuration Inheritance

Pattern options inherit from `EmergeScopeBase`. Child scopes (like `InitialScope`, `RefinementScope`) inherit settings from the parent unless overridden:

```csharp
await Emerge.Refine<T>(model, ctx, opt =>
{
    // Parent settings - inherited by all scopes
    opt.Temperature = 0.3f;
    opt.SystemPrompt = "You are an expert...";

    opt.Initial(s =>
    {
        // Only set what's different
        s.Command = "Generate initial draft.";
    });

    opt.Refinement(s =>
    {
        s.Temperature = 0.2f;  // Override for refinement
        s.Command = "Improve the draft.";
    });
}).FinalAsync();
```

### Context Behavior

Patterns handle context in two ways:

- **Shared context**: Sequential stages (Solver→Critic→Verifier, Refine iterations) share context. Each stage's output is automatically added to context before the next stage runs.
- **Isolated context**: Parallel runs (BestOf candidates, MapReduce chunks, Swarm agents) use isolated derived contexts to ensure deterministic parallel execution.

---

## Patterns

### Run — Single Agent Loop

The core pattern. Generates a typed JSON result with optional tool use.

```csharp
var (result, ctx) = await Emerge.Run<ChatResponse>(LLMModel.Claude45Sonnet, context, pass =>
{
    pass.SystemPrompt = "You are a helpful assistant.";
    pass.Command = "Answer the user's question.";
    pass.Temperature = 0.7;
    pass.MaxIterations = 5;
    pass.AddTool("search_web", "Search the web for information",
        (string query) => SearchWeb(query));
}).FinalAsync();
```

The `EmergePass<T>` configure callback is invoked on every iteration, giving access to runtime state:

- `pass.Iteration` — current iteration number
- `pass.HasFunctionResults` / `pass.HasNewFunctionResults` — whether tool results exist in context
- `pass.Stop(reason?)` — early termination from within the callback

**Options:**
- `SystemPrompt` - System instruction
- `Command` - User command/prompt
- `Temperature`, `MaxOutputTokens`, `ReasoningEffort`, `ReasoningTokenBudget` - Model parameters
- `MaxIterations`, `MaxToolCalls`, `MaxWallTime` - Budget limits
- `MaxRetries`, `RetryDelay` - Automatic retry on transient failures
- `Tools` - Available tools (see [Inline Tool Registration](#inline-tool-registration))

---

### BestOf — Score and Select Best

Run N independent attempts and select the best result based on a scoring function.

```csharp
var (best, _) = await Emerge.BestOf<Answer>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.Count = 5;
    opt.Command = "Solve this problem step by step.";
    opt.Score = (answer, trace) => answer.Confidence * (1f / trace.Duration.TotalSeconds);

    opt.Candidate(c =>
    {
        c.Temperature = 0.7 + 0.1 * c.Index;  // Vary temperature per candidate
        c.Seed = 1000 + c.Index;
    });
}).FinalAsync();
```

**Options:**
- `Count` - Number of candidates (default: 3)
- `Score` - Scoring function `Func<T, EmergenceTrace, double>`
- `Candidate(Action<CandidateScope<T>>)` - Configure each candidate (has `Index`, `Seed`)

**Context flow:** Each candidate runs with an isolated derived context.

---

### ParallelBestOf — Parallel Score and Select

Like BestOf but explicitly parallelized with concurrency control.

```csharp
var (best, _) = await Emerge.ParallelBestOf<Answer>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.Count = 10;
    opt.MaxParallel = 4;  // Run 4 at a time
    opt.Command = "Generate a creative solution.";
    opt.Score = (answer, _) => ScoreAnswer(answer);
}).FinalAsync();
```

**Options:**
- `Count` - Number of candidates (default: 3)
- `MaxParallel` - Concurrency limit (default: 4)
- `Score` - Scoring function

---

### SolverCriticVerifier — Draft, Critique, Verify

Three-stage pattern: generate a draft, critique it, then produce a verified final version.

```csharp
var (final, _) = await Emerge.SolverCriticVerifier<Report>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.MaxRounds = 2;

    opt.Solver(s =>
    {
        s.Temperature = 0.8;
        s.Command = "Draft a comprehensive report on the topic.";
    });

    opt.Critic(c =>
    {
        c.Temperature = 0.3;
        c.Command = "Review this draft. List factual errors, logical gaps, and areas for improvement.";
    });

    opt.Verifier(v =>
    {
        v.Temperature = 0.4;
        v.Command = "Produce the final report addressing all critique points.";
    });
}).FinalAsync();
```

**Options:**
- `MaxRounds` - Number of solver→critic→verifier cycles (default: 1)
- `Solver(Action<EmergeScope<T>>)` - Configure the draft generator
- `Critic(Action<EmergeScope>)` - Configure the critic (untyped output)
- `Verifier(Action<EmergeScope<T>>)` - Configure the final verifier

**Context flow:** Solver output is automatically added to context before Critic runs. Critic output is added before Verifier runs.

---

### DebateThenJudge — Multiple Perspectives

Multiple debaters generate competing proposals, then a judge selects or synthesizes the best.

```csharp
var (final, _) = await Emerge.DebateThenJudge<Decision>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.Debaters = 3;
    opt.DebateRounds = 1;

    opt.Debater(d =>
    {
        d.Temperature = 0.9;
        d.Command = $"As debater {d.Index}, argue for your position on this issue.";
    });

    opt.Judge(j =>
    {
        j.Temperature = 0.3;
        j.Command = "Review all arguments. Synthesize the best points into a final decision.";
    });
}).FinalAsync();
```

**Options:**
- `Debaters` - Number of debaters (default: 2)
- `DebateRounds` - Rounds of debate (default: 1). Each round, debaters receive previous round arguments in context.
- `Debater(Action<AgentScope<T>>)` - Configure debaters (has `Index`, `Role`, `Seed`)
- `Judge(Action<EmergeScope<T>>)` - Configure the judge

**Context flow:** Debaters run with isolated contexts. After all debaters complete, the Judge receives all arguments in context.

---

### MapReduce — Chunk Processing

Split input into chunks, process each in parallel, then reduce to a final result.

```csharp
var (report, _) = await Emerge.MapReduce<ChunkSummary, FinalReport>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.Chunks = documents.Select(d => (object)d).ToList();
    opt.MaxParallel = 8;

    opt.Map(m =>
    {
        m.Temperature = 0.5;
        m.Command = "Summarize the key points from this document chunk.";
    });

    opt.Reduce(r =>
    {
        r.Temperature = 0.3;
        r.Command = "Combine all chunk summaries into a comprehensive final report.";
    });
}).FinalAsync();
```

**Options:**
- `Chunks` - Pre-split input chunks (takes precedence if set)
- `Input` + `Split` - Or provide input with a split function (used only if `Chunks` is null)
- `MaxParallel` - Concurrency for map phase (default: 4)
- `Map(Action<EmergeScope<TChunk>>)` - Configure chunk processing
- `Reduce(Action<EmergeScope<TResult>>)` - Configure reduction

**Context flow:** Map runs use isolated contexts. All map outputs are collected and provided to Reduce in context.

---

### Refine — Iterative Improvement

Generate an initial result, then iteratively improve it based on feedback.

```csharp
var (final, _) = await Emerge.Refine<Code>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.MaxRefinements = 3;

    opt.Initial(s =>
    {
        s.Command = "Write initial implementation of the feature.";
    });

    opt.Refinement(s =>
    {
        s.Command = "Improve the code based on the issues found.";
    });

    // Async validation - continue refining while there are errors
    opt.ShouldContinue = async (result, trace) =>
    {
        var error = await ValidateCodeAsync(result.Code);
        return error != null;
    };
}).FinalAsync();
```

**Options:**
- `MaxRefinements` - Maximum improvement iterations (default: 3)
- `ShouldContinue` - Async callback `Func<T, EmergenceTrace, Task<bool>>` to control refinement
- `Initial(Action<EmergeScope<T>>)` - Configure initial generation
- `Refinement(Action<EmergeScope<T>>)` - Configure refinement passes

**Context flow:** Each refinement automatically receives the previous attempt's JSON output in context.

---

### TestRefine — Agentic Loop with Real-World Testing

Generate an initial result, then iteratively apply it to the real environment, evaluate via testing, and refine based on feedback. Unlike Refine which operates purely on the serialized result, TestRefine includes caller-provided side effects for grounding each iteration in real-world testing (screenshots, browser tests, compilation, etc.).

```csharp
var (final, _) = await Emerge.TestRefine<UIComponent>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.MaxIterations = 5;

    opt.Initial(s =>
    {
        s.Command = "Generate a responsive card component.";
    });

    opt.Refinement(s =>
    {
        s.Command = "Fix the issues found during testing.";
    });

    // Apply the result to the real environment
    opt.Apply = async (result, iteration) =>
    {
        await RenderToPreview(result.Code);
    };

    // Test the environment, score, decide whether to continue
    opt.Evaluate = async (result, iteration) =>
    {
        var screenshots = await CaptureScreenshots();
        var critique = await RunVisualCritique(screenshots);
        return new TestRefineFeedback
        {
            Continue = critique.Issues.Count > 0,
            Feedback = string.Join("\n", critique.Issues.Select(i => $"Fix: {i}")),
            Score = critique.Score
        };
    };
}).FinalAsync();
```

**Options:**
- `MaxIterations` - Maximum apply→evaluate→refine cycles (default: 5)
- `Initial(Action<EmergeScope<T>>)` - Configure initial generation
- `Refinement(Action<EmergeScope<T>>)` - Configure refinement passes
- `Apply` - Async callback `Func<T, int, Task>` to apply the result to the real environment (e.g., render UI, serve HTML, deploy to sandbox). Receives the result and iteration index. Optional.
- `Evaluate` - Async callback `Func<T, int, Task<TestRefineFeedback>>` to test the environment and decide whether to continue. Returns `TestRefineFeedback` with `Continue`, `Feedback`, and optional `Score`. Optional.

**Callback behavior:**
- Without `Apply`, generates and evaluates without side effects
- Without `Evaluate`, generates, applies, and loops to `MaxIterations` (degenerates to Refine with Apply side effects)
- `TestRefineFeedback.Feedback` is injected into the next refinement prompt alongside the current result JSON

**Context flow:** Each refinement receives the current result JSON and evaluation feedback in context. The Apply and Evaluate callbacks run between LLM calls.

---

### PlanAndExecute — Strategic Execution

First create a plan, then execute each step to produce the final result.

```csharp
var (result, _) = await Emerge.PlanAndExecute<ProjectPlan>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.MaxSteps = 10;
    opt.Tools.Add(fileReadTool);
    opt.Tools.Add(searchTool);

    opt.Planner(p =>
    {
        p.Command = "Create a step-by-step plan to complete this task.";
    });

    opt.Executor(e =>
    {
        e.Command = "Execute the current step and report results.";
    });
}).FinalAsync();
```

**Options:**
- `MaxSteps` - Maximum execution steps (default: 10)
- `Planner(Action<EmergeScope<ExecutionPlan>>)` - Configure plan generation
- `Executor(Action<EmergeScope<T>>)` - Configure step execution

The `ExecutionPlan` type contains `List<PlanStep> Steps` and optional `Summary`. Each `PlanStep` has `Description`, `RequiresTool`, and optional `ToolName`.

---

### Router — Dynamic Routing

Select the best route/model/approach for the task, then execute it.

```csharp
var (result, _) = await Emerge.Router<Response>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.AddRoute("code", "Programming and technical questions", LLMModel.Claude45Sonnet);
    opt.AddRoute("creative", "Creative writing and brainstorming", LLMModel.Claude45Sonnet);
    opt.AddRoute("analysis", "Data analysis and reasoning", LLMModel.Claude45Sonnet);

    opt.Router(r =>
    {
        r.Command = "Analyze this request and select the most appropriate route.";
    });

    opt.Command = "Handle the user's request.";
}).FinalAsync();
```

**Options:**
- `AddRoute(name, description, model?, configure?)` - Define available routes
- `Router(Action<EmergeScope<RouterDecision>>)` - Configure route selection

The `RouterDecision` type contains `SelectedRoute` and optional `Reasoning`.

---

### EnsembleMerge — Diverse Solutions Merged

Run multiple diverse solvers in parallel, then merge their outputs into a coherent result.

```csharp
var (merged, _) = await Emerge.EnsembleMerge<Analysis>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.SolverCount = 4;
    opt.MaxParallel = 4;

    opt.Solver(s =>
    {
        s.Temperature = 0.6 + 0.15 * s.Index;  // Varying temperatures
        s.Command = "Analyze this data from your unique perspective.";
    });

    opt.Merger(m =>
    {
        m.Temperature = 0.3;
        m.Command = "Synthesize all analyses into a comprehensive unified result.";
    });
}).FinalAsync();
```

**Options:**
- `SolverCount` - Number of parallel solvers (default: 3)
- `MaxParallel` - Concurrency limit (default: 3)
- `Solver(Action<AgentScope<T>>)` - Configure each solver (has `Index`, `Role`, `Seed`)
- `Merger(Action<EmergeScope<T>>)` - Configure the merger

**Context flow:** Solvers run with isolated contexts for deterministic parallel execution. Merger receives all solver outputs in context.

---

### TreeOfThought — Branching Reasoning

Explore multiple reasoning paths with beam search, evaluating and pruning to find the best solution.

```csharp
var (best, _) = await Emerge.TreeOfThought<Solution>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.MaxDepth = 4;
    opt.BranchingFactor = 3;
    opt.BeamWidth = 2;

    opt.Evaluate = (thought, trace) => ScoreThought(thought);

    opt.Thought(t =>
    {
        t.Command = "Generate the next reasoning step.";
    });
}).FinalAsync();
```

**Options:**
- `MaxDepth` - Maximum tree depth (default: 3)
- `BranchingFactor` - Branches per node (default: 3)
- `BeamWidth` - Best paths to keep at each level (default: 2)
- `Evaluate` - Scoring function `Func<T, EmergenceTrace, double>`
- `Thought(Action<EmergeScope<T>>)` - Configure thought generation
- `Evaluator(Action<EmergeScope<T>>)` - Configure evaluator scope

**Context flow:** Each branch runs with an isolated derived context containing its parent thought path.

---

### SelfConsistency — Majority Voting

Sample multiple completions and select the most consistent/majority answer.

```csharp
var (answer, _) = await Emerge.SelfConsistency<MathAnswer>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.Samples = 7;
    opt.MaxParallel = 7;

    opt.Sample(s =>
    {
        s.Temperature = 0.8;
        s.Seed = s.Index * 1000;
        s.Command = "Solve this math problem step by step.";
    });

    // Optional custom majority selection
    opt.SelectMajority = answers => answers
        .GroupBy(a => a.FinalAnswer)
        .OrderByDescending(g => g.Count())
        .First()
        .First();
}).FinalAsync();
```

**Options:**
- `Samples` - Number of samples (default: 5)
- `MaxParallel` - Concurrency limit (default: 5)
- `SelectMajority` - Custom selection function (default: JSON equality grouping)
- `Sample(Action<CandidateScope<T>>)` - Configure sampling (has `Index`, `Seed`)

**Context flow:** Each sample runs with an isolated derived context to ensure independent sampling.

---

### Swarm — Multi-Agent Orchestration

Coordinate multiple agents with different roles across rounds.

```csharp
var (result, _) = await Emerge.Swarm<ProjectOutput>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.MaxRounds = 2;
    opt.MaxParallel = 3;

    opt.AddAgent("researcher", s =>
    {
        s.Command = "Research and gather relevant information.";
    });

    opt.AddAgent("analyst", s =>
    {
        s.Command = "Analyze the gathered information.";
    });

    opt.AddAgent("writer", s =>
    {
        s.Command = "Write the final output based on analysis.";
    });

    opt.Coordinator(c =>
    {
        c.Command = "Synthesize all agent outputs into the final deliverable.";
    });

    // Or use a custom merge function
    // opt.Merge = results => MergeResults(results);
}).FinalAsync();
```

**Options:**
- `MaxRounds` - Number of orchestration rounds (default: 1)
- `MaxParallel` - Agents running concurrently (default: 4)
- `AddAgent(role, configure)` - Add an agent with a role and configuration
- `Coordinator(Action<EmergeScope<T>>)` - Configure final coordination
- `Merge` - Custom merge function instead of coordinator

Each `SwarmAgent<T>` has `Role`, optional `Id`, and `DependsOn` list for declaring inter-agent dependencies.

**Context flow:** Agents run with isolated contexts per round. All agents run every round. Coordinator receives all agent outputs in context.

---

### TaskGraph — Dependency-Aware Task Execution

Define a graph of tasks with dependencies, execute them in parallel where possible, with periodic review and plan revision.

```csharp
var (result, _) = await Emerge.TaskGraph<FinalReport>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.MaxParallel = 4;
    opt.EnableParallelReview = true;
    opt.ReviewIntervalTasks = 2;

    opt.AddTask("research-benefits", "Research the benefits of the approach");
    opt.AddTask("research-challenges", "Research the challenges and risks");
    opt.AddTask("synthesize", "Synthesize findings into a report",
        "research-benefits", "research-challenges");  // blocked by both

    opt.Worker(w =>
    {
        w.Temperature = 0.7;
        w.Command = "Complete the assigned task thoroughly.";
    });

    opt.Reviewer(r =>
    {
        r.Temperature = 0.3;
        r.Command = "Review completed tasks for quality and suggest improvements.";
    });

    opt.Synthesizer(s =>
    {
        s.Temperature = 0.4;
        s.Command = "Synthesize all task results into the final deliverable.";
    });

    opt.OnTaskCompleted = (task, result) => Console.WriteLine($"Task {task.Id} done");
}).FinalAsync();
```

**Options:**
- `AddTask(id, description, params blockedBy)` - Add a task with optional dependencies
- `MaxParallel` - Concurrent task limit (default: 4)
- `EnableParallelReview` - Run reviews alongside execution (default: true)
- `ReviewIntervalTasks` - Review after every N completed tasks (default: 2)
- `Worker(Action<EmergeScope<T>>)` - Configure task executor
- `Reviewer(Action<EmergeScope<ReviewFeedback>>)` - Configure reviewer
- `PlanReviser(Action<EmergeScope<PlanRevision>>)` - Configure plan reviser
- `Synthesizer(Action<EmergeScope<T>>)` - Configure final synthesis
- `OnTaskCompleted`, `OnReviewCompleted`, `OnPlanRevised` - Progress callbacks
- `OnHumanFeedback` - Async callback for human-in-the-loop

Each `TaskNode` has `Id`, `Description`, `BlockedBy`, `Blocks`, `Status`, optional `Owner`, `Result`, and `Error`.

---

### TreeSearch — Document Tree Navigation

Navigate a hierarchical document index to find relevant sections without vector embeddings.

```csharp
// Step 1: Build a tree index from content
TreeIndex index = null;
await foreach (var ev in TreeIndex.BuildAsync(LLMModel.Claude45Sonnet, documentContent,
    new TreeIndexOptions { MaxDepth = 4, GenerateSummaries = true }))
{
    if (ev is Completed<TreeIndex> done)
    {
        index = done.Result;
    }
}

// Step 2: Search the tree
var (result, _) = await Emerge.TreeSearch<TreeSearchResult>(LLMModel.Claude45Sonnet, ctx, opt =>
{
    opt.Index = index;
    opt.Query = "How does authentication work?";
    opt.MaxSteps = 10;
    opt.MaxResults = 3;

    opt.Navigator(n =>
    {
        n.Command = "Navigate the document tree to find sections relevant to the query.";
    });
}).FinalAsync();

// result.Sections contains found sections with NodeId, Path, Content, Relevance, Page
```

**Options:**
- `Index` - The `TreeIndex` to search
- `Query` - Search query
- `MaxSteps` - Maximum navigation steps (default: 10)
- `MaxResults` - Maximum sections to return (default: 5)
- `Navigator(Action<EmergeScope<NavigationDecision>>)` - Configure navigator

**Tree indexing types:**

`TreeIndex` builds a hierarchical document structure:
- `BuildAsync(model, string content, options?)` - Build from raw text
- `BuildAsync(model, IContentReader reader, options?)` - Build from custom reader
- `ToTableOfContents(maxDepth)` - Generate table of contents
- `FindById(id)` - Look up a node by ID

`TreeIndexOptions`: `MaxDepth` (default: 4), `MaxSummaryTokens` (default: 100), `GenerateSummaries` (default: true)

`TreeNode`: `Id`, `Title`, `Summary`, `Content`, `Page`, `Children`, `Parent`, `Depth`

`IContentReader` / `ContentSection`: Interface for custom content sources. `StringContentReader` wraps a plain string.

---

## Inline Tool Registration

The `EmergePass<T>` provides fluent `AddTool` extension methods for registering tools inline with lambda functions. Tools are deduplicated by name.

```csharp
await foreach (var ev in Emerge.Run<CoderResponse>(LLMModel.Claude45Sonnet, ctx, pass =>
{
    pass.AddTool("write_file", "Write content to a file",
            (string path, string content) => WriteFile(path, content))
        .AddTool("read_file", "Read file contents",
            (string path) => ReadFile(path))
        .AddTool("list_files", "List all files",
            () => ListFiles());

    pass.Command = "Complete this coding task.";
    pass.MaxIterations = 10;
    pass.MaxToolCalls = 50;
}))
{ ... }
```

**Methods:**
- `AddTool(Function)` - Add a pre-built `Function` object
- `AddTools(params Function[])` - Add multiple pre-built functions
- `AddToolsFrom(object instance)` - Auto-discover methods with `[Function]` attributes
- `AddTool(name, description, Func<..., TResult>)` - Inline sync function (0-8 parameters)
- `AddTool(name, description, Func<..., Task<TResult>>)` - Inline async function (0-8 parameters)

All `AddTool` overloads return `EmergePass<T>` for chaining.

---

## Structured Tag Parser

`StructuredTagParser` extracts XML-style tags from LLM responses, useful for structured output outside of JSON mode.

```csharp
using Ikon.AI.Emergence.Structured;

var parsed = StructuredTagParser.Parse(content, "reasoning", "answer");

// parsed.PlainText — text outside tags
// parsed.Blocks — list of ParsedBlock (TagName, Content, StartIndex, EndIndex)

// Utility methods
bool has = StructuredTagParser.HasTag(content, "reasoning");
string? text = StructuredTagParser.GetTagContent(content, "answer");
```

---

## KernelContext Extensions

Extension methods for inspecting tool call history in a `KernelContext`:

```csharp
bool hasFn = ctx.HasFunctionResults();
var results = ctx.GetFunctionResults(take: 10);  // IReadOnlyList<FunctionResultPart>
var calls = ctx.GetFunctionCalls(take: 10);       // IReadOnlyList<FunctionCall>
```

---

## Common Options Reference

All pattern options inherit these from `EmergeScopeBase`:

| Option | Type | Description |
|--------|------|-------------|
| `Model` | `LLMModel?` | Override the model |
| `Temperature` | `double?` | Sampling temperature |
| `MaxOutputTokens` | `int?` | Maximum output tokens |
| `ReasoningEffort` | `ReasoningEffort?` | Reasoning effort level |
| `ReasoningTokenBudget` | `int?` | Token budget for reasoning |
| `Timeout` | `TimeSpan?` | Request timeout |
| `Regions` | `IReadOnlyList<ModelRegion>?` | Model region preferences |
| `MaxIterations` | `int?` | Max agentic iterations |
| `MaxToolCalls` | `int?` | Max tool calls |
| `MaxWallTime` | `TimeSpan?` | Max wall clock time |
| `MaxRetries` | `int?` | Max retries on transient failures |
| `RetryDelay` | `TimeSpan?` | Delay between retries |
| `SystemPrompt` | `string?` | System instruction |
| `Command` | `string?` | User command |
| `Tools` | `IList<Function>` | Available tools |
| `UseLastNMessages` | `int?` | Context window limit |
| `SkipLastNMessages` | `int?` | Skip N most recent messages |
| `OptimizeContext` | `bool?` | Enable context optimization |
| `UseCitations` | `bool?` | Enable citations |
| `IncludeJsonExample` | `bool?` | Include JSON example in prompt (default: true) |

`UseLastMessages(count, skipLast)` is a convenience method for setting both `UseLastNMessages` and `SkipLastNMessages`.

`EmergeScope<T>` adds `UseJson` (default: true), `CaseInsensitiveJson` (default: true), `JsonSchema`, and `JsonExample` (both read-only, auto-generated from `T`).

### EmergenceBudget

Pre-defined budget configurations:

```csharp
var budget = EmergenceBudget.Default;    // 10 iterations, 50 tool calls, 5 min
var budget = EmergenceBudget.Unlimited;  // No limits
var budget = new EmergenceBudget(maxIterations: 20, maxToolCalls: 100, maxWallTime: TimeSpan.FromMinutes(10));
```

### EmergenceTrace

Returned with `Completed<T>` events:

| Property | Type | Description |
|----------|------|-------------|
| `Iterations` | `int` | Number of LLM iterations |
| `ToolCalls` | `int` | Number of tool calls made |
| `InputTokens` | `long` | Total input tokens consumed |
| `OutputTokens` | `long` | Total output tokens generated |
| `Duration` | `TimeSpan` | Total wall time |
| `ToolCallHistory` | `IReadOnlyList<FunctionCall>` | Full tool call history |
| `FinishReason` | `string?` | Model finish reason (e.g., "length", "max_tokens") |
| `Error` | `Exception?` | Error if one occurred |
| `IsTruncated` | `bool` | True when `FinishReason` indicates output was cut short |

## Testing with Mock LLM

All pattern methods have an overload accepting `ILLM` for testing:

```csharp
var mockLlm = new MockLLM(responses);

var (result, _) = await Emerge.Run<MyType>(
    LLMModel.Claude45Sonnet,
    ctx,
    pass => { ... },
    mockLlm  // Injected for testing
).FinalAsync();
```
