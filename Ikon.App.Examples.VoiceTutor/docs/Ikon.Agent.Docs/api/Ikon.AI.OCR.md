namespace Ikon.AI.OCR
  enum DocumentType
    General
  interface IOCR : IDisposable, IOCRInfo
    Task<OCRResult> AnalyzeDocumentAsync(OCRConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<OCRResult> AnalyzeDocumentStreamingAsync(OCRConfig config, CancellationToken cancellationToken = default)
  interface IOCRInfo
    // Largest document the model accepts, in bytes, or 0 when it publishes no limit — never read 0 as a zero budget. Only checked when the document is supplied as OCRConfig.Data; the size behind a OCRConfig.Url or OCRConfig.AssetUri is not known before the request is made.
    long MaxDocumentSizeBytes { get; }
    // Most pages the model reads in one request, or 0 when it publishes no limit. A longer document has to be split into several requests with OCRConfig.Pages.
    int MaxPagesSupported { get; }
    // Mime types the provider documents as accepted input, or empty when it publishes no list. Advisory: a type outside the list is passed to the provider rather than refused here, because the provider is the authority on what it will read.
    IReadOnlyList<string> SupportedMimeTypes { get; }
    // True when the model fills OCRResult.Words for OCRConfig.IncludeWords. A request that asks a model reporting false for words is refused rather than answered with an empty list.
    bool SupportsWordLevelText { get; }
  sealed class OCR : IOCR
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(OCRModel model, IReadOnlyList<ModelRegion>? regions = null)
    long MaxDocumentSizeBytes { get; }
    int MaxPagesSupported { get; }
    IReadOnlyList<string> SupportedMimeTypes { get; }
    bool SupportsWordLevelText { get; }
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
    long MaxDocumentSizeBytes { get; init; }
    int MaxPagesSupported { get; init; }
    IReadOnlyList<string> SupportedMimeTypes { get; init; }
    bool SupportsWordLevelText { get; init; }
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
