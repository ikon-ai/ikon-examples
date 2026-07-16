# AI Web & Data

## AI Web & Data Services

### Web Search

```csharp
var results = await WebSearcher.SearchAsync("latest AI news", maxResults: 5);  // Google by default
foreach (var result in results) { /* result.Title, result.Url, result.Content */ }
```

Use the constructor + config form for site-restricted search or country/language targeting:

```csharp
using var searcher = new WebSearcher(WebSearcherModel.Google);
var results = await searcher.SearchPagesAsync(new SearchConfig { Query = "latest AI news", InSiteUrl = "https://example.com" });
```

### Embeddings

```csharp
var embeddings = await EmbeddingGenerator.EmbedAsync(["Hello world", "Goodbye"]);  // OpenAI3Small (cheap+fast) by default
// embeddings[0] is float[] vector
```

### Other Data Services

Each has a static one-shot with a sensible default model:

```csharp
var page = await WebScraper.ScrapeAsync("https://example.com");          // page.Content is Markdown
var moderation = await Classifier.ClassifyAsync(userText);               // moderation.IsFlagged
var ocr = await OCR.AnalyzeAsync(documentBytes);                         // ocr.Text
var pdf = await FileConverter.ConvertToPdfAsync(docxBytes, "report.docx");
var ranked = await Reranker.RerankAsync(documents, query);               // ranked[0].Index into documents
```

Refer to generated API docs for model listings and the constructor + config forms (multi-page crawling, screenshots, image moderation, OCR from URL, custom timeouts). `Retriever` provides RAG primitives.

---

# Ikon.AI Public API
namespace Ikon.AI.Classification
  sealed record ClassificationDetail
    ctor()
    ctor(ClassificationLabel label, string originalCategory, bool isFlagged, double score)
    bool IsFlagged { get; init; }
    ClassificationLabel Label { get; init; }
    string OriginalCategory { get; init; }
    double Score { get; init; }
  sealed record ClassificationInput
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

namespace Ikon.AI.Embeddings
  enum EmbeddingEncoding
    Base64
    GzipBase64
  sealed class EmbeddingGenerator : IEmbeddingGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(EmbeddingModel model, IReadOnlyList<ModelRegion>? regions = null)
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
    TimeSpan Timeout { get; set; }
    void Dispose()
    Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an EmbeddingGenerator per call. Defaults to EmbeddingModel.OpenAI3Small and EmbeddingType.Generic; override the model via model, and pass an explicit EmbeddingType when embedding documents vs. queries for asymmetric retrieval. Returns one float[] per input, in input order. Use the constructor + GenerateEmbeddingsAsync for per-request batch caps (maxInputCount) or the size properties.
    static Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, EmbeddingModel model = OpenAI3Small, EmbeddingType type = Generic, CancellationToken cancellationToken = default)
    Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, CancellationToken cancellationToken = default)
    static EmbeddingGeneratorCapabilities GetCapabilities(EmbeddingModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(EmbeddingModel model)
  sealed class EmbeddingGeneratorCapabilities : IEmbeddingGeneratorInfo
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
    VoyageCode3
  static class EmbeddingModelExtensions
    static string DisplayName(this EmbeddingModel model)
  enum EmbeddingType
    Generic
    Document
    Query
    Clustering
    Classification
  interface IEmbeddingGenerator : IDisposable, IEmbeddingGeneratorInfo
    // Scaled up internally with the batch size; defaults to 10 seconds.
    TimeSpan Timeout { get; set; }
    Task<List<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> inputs, EmbeddingType type, int maxInputCount = 0, CancellationToken cancellationToken = default)
  interface IEmbeddingGeneratorInfo
    int EmbeddingVectorSize { get; }
    int MaxInputCount { get; }
  class NonRetryableEmbeddingGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  static class VectorMath
    static float[] CalculateAverageEmbedding(IList<float[]> embeddings)
    static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateDotProduct(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static float CalculateEuclideanDistance(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    static List<List<VectorMath.Neighbor>> CalculateKNearestNeighbors(IList<float[]> embeddings, int k)
    static float GetMagnitude(ReadOnlySpan<float> vector)
  readonly struct VectorMath.Neighbor
    ctor(int index, float distance)
    float Distance { get; }
    int Index { get; }

namespace Ikon.AI.FileConversion
  sealed class ConvertedFile
    ctor()
    byte[] Data { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
  sealed class FileConverter : IFileConverter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(FileConverterModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
    Task<ConvertedFile> ConvertToPdfAsync(byte[] data, string fileName, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a FileConverter per call. fileName must carry the source extension (e.g. report.docx) — it determines the input format. The PDF is in result.Data. Use the constructor + ConvertToPdfAsync for a URL or AssetUri source, or a custom timeout.
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
    static string DisplayName(this FileConverterModel model)
  interface IFileConverter : IDisposable
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
  class NonRetryableFileConverterException : NonRetryableAIException
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
    Task<OCRResult> AnalyzeAsync(byte[] data, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an OCR per call. Accepts image or PDF bytes. Defaults to OCRModel.AzureDocumentIntelligence; override via model. Extracted text is in result.Text; result.Paragraphs/result.Pages carry structure. Use the constructor + AnalyzeDocumentAsync for a URL/AssetUri source or other fields, or AnalyzeDocumentStreamingAsync for page-by-page streaming.
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
    static string DisplayName(this OCRModel model)
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
    // Scaled up internally with the document count; defaults to 10 seconds.
    TimeSpan Timeout { get; set; }
    Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, CancellationToken cancellationToken = default)
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
  static class RerankModelExtensions
    static string DisplayName(this RerankModel model)
  sealed class Reranker : IReranker
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(RerankModel model, IReadOnlyList<ModelRegion>? regions = null)
    TimeSpan Timeout { get; set; }
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(RerankModel model)
    Task<List<RerankItem>> RerankAsync(IReadOnlyList<string> documents, string query, int topN = 0, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a Reranker per call. Defaults to RerankModel.CohereRerank4Fast; override via model. Pass topN to cap returned items (0 returns all). Each RerankItem carries the document's original .Index and relevance .Score, ordered most relevant first. Use the constructor + the instance overload for a custom Timeout or reusing one instance across many queries.
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
    static bool operator !=(ContentLink lhs, ContentLink rhs)
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
    Task<List<SearchResult>> SearchAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
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
