# Ikon.Pipelines.Public Public API

namespace Ikon.Pipelines.Public.Examples
  static class ExampleProcessors
    static Task<List<Item>> Run(Item inputItem)
    static Task<List<Item>> Run2(Item inputItem, CancellationToken cancellationToken)
    static Task<List<Item>> Run3(List<Item> inputItems)
    static Task<List<Item>> Run4(List<Item> inputItems, CancellationToken cancellationToken)
  // Example pipeline. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<FullExamplePipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class FullExamplePipeline
    ctor(IPipelineHost<FullExamplePipeline.Config> host)
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
  class FullExamplePipeline.Config
    ctor()
    int TestValue1 { get; set; }
    string TestValue2 { get; set; }
  class FullExamplePipeline.Input
    ctor()
    int TestValue1 { get; set; }
    string TestValue2 { get; set; }
  class FullExamplePipeline.Result
    ctor()
    int TestValue1 { get; set; }
    string TestValue2 { get; set; }
  // Example pipeline. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<MinimalExamplePipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class MinimalExamplePipeline
    ctor()
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)

namespace Ikon.Pipelines.Public.Processors.Json
  static class MergeJsonProcessor
    // Deserializes each input item's content and emits a single JSON-array item, named exactly itemName (the full name, not a suffix), whose elements are the deserialized items in input order. An item whose content fails to deserialize throws InvalidOperationException. Returns an empty result when there are no input items.
    static Task<List<Item>> Run(List<Item> items, string itemName)
  static class SplitJsonArrayProcessor
    // Splits a JSON array into one output item per element. When the content is a top-level array, its elements are emitted directly; when the content is an object, the first array-valued property in enumeration order is used. Throws InvalidOperationException when no array is found.
    static Task<List<Item>> Run(Item input)
  static class TrimJsonProcessor
    // Beyond removing any fields named in fieldsToRemove, this always performs a recursive prune of the entire document — independent of fieldsToRemove, and applied even when it is null or empty. Every null value, every empty or whitespace-only string, and every object or array that ends up empty is dropped, recursively, so a container emptied by pruning is itself pruned from its parent. If the whole document prunes away to nothing, the output is the literal null.
    static Task<List<Item>> Run(Item input, List<string>? fieldsToRemove = null)

namespace Ikon.Pipelines.Public.Processors.OCR
  static class OCRProcessor
    static Task<List<Item>> Run(Item input, OCRProcessor.Config config, CancellationToken cancellationToken)
  class OCRProcessor.Config
    ctor()
    OCRModel OCRModel { get; set; }

namespace Ikon.Pipelines.Public.Processors.Pdf
  static class ExtractPdfProcessor
    static Task<List<Item>> Run(Item input, ExtractPdfProcessor.Config config, CancellationToken cancellationToken)
  class ExtractPdfProcessor.Config
    ctor()
    // Longest side, in PIXELS, of each rendered page image. Default 1024; must be greater than zero.
    int MaxPageImageDimension { get; set; }
  interface IPdfDocument : IDisposable
    int PageCount { get; }
    IPdfPage GetPage(int index)
  interface IPdfPage : IDisposable
    double Height { get; }
    int Index { get; }
    double Width { get; }
    void CreateCopy(Stream output)
    // Renders the page scaled so its longest side is at most maxDimension pixels, preserving aspect ratio. Returns the page as a row-major RGBA byte buffer (4 bytes/pixel) and the resulting pixel dimensions.
    (byte[] rgba, int width, int height) GetPixels(int maxDimension)
    // Renders the page at the exact width by height pixel size. Returns the page as a row-major RGBA byte buffer (4 bytes/pixel) and the resulting pixel dimensions.
    (byte[] rgba, int width, int height) GetPixels(int width, int height, bool hasAlpha)
    string GetText()
  static class PdfDocument
    static IPdfDocument Load(byte[] bytes, string? password = null)

namespace Ikon.Pipelines.Public.UniversalRag
  // Universal RAG ingestion pipeline. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<UniversalRagPipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class UniversalRagPipeline
    ctor(IPipelineHost<UniversalRagPipeline.Config> host)
    // Items are routed by MimeType: PDFs, plain text, and JSON are each handled on their own document path. An item whose original name starts with web_scraper_config (JSON), web_scraper_sitemap (XML), or web_scraper_sitelist (plain text) instead triggers the web-scraping path. All other PDF, text, and JSON items are treated as documents. An item that matches none of these — neither a document type nor a web-scraper trigger — produces no output and is silently dropped.
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
  class UniversalRagPipeline.Config
    ctor()
    AnalyzePdfDocumentProcessor.Config AnalyzeDocumentType { get; set; }
    // Number of text chunks embedded per batch request. Default 200.
    int EmbeddingBatchSize { get; set; }
    ExtractPdfProcessor.Config ExtractPdf { get; set; }
    ExtractFullTextAndSectionsProcessor.Config ExtractSections { get; set; }
    ExtractTextProcessor.Config ExtractText { get; set; }
    FormatWebPageProcessor.Config FormatWebPage { get; set; }
    GenerateEmbeddingsProcessor.Config GenerateEmbeddings { get; set; }
    GenerateSummaryProcessor.Config GenerateSummary { get; set; }
    // Maximum concurrent LLM requests during analysis. Default 10.
    int MaxLLMParallelism { get; set; }

namespace Ikon.Pipelines.Public.UniversalRag.Processors
  static class AnalyzePdfDocumentProcessor
    static Task<List<Item>> Run(List<Item> inputItems, AnalyzePdfDocumentProcessor.Config config, CancellationToken cancellationToken)
  class AnalyzePdfDocumentProcessor.Config
    ctor()
    LLMModel LLMModel { get; set; }
    // Number of leading pages sent to the LLM for document-level analysis (title, type). Default 3.
    int PagesToAnalyze { get; set; }
  static class CombineEmbeddingsProcessor
    static Task<List<Item>> Run(List<Item> inputItems, CancellationToken cancellationToken)
  static class ExtractFullTextAndSectionsProcessor
    static Task<List<Item>> Run(Item inputItem, ExtractFullTextAndSectionsProcessor.Config config, CancellationToken cancellationToken)
    static Task<List<Item>> Run(List<Item> inputItems, ExtractFullTextAndSectionsProcessor.Config config, CancellationToken cancellationToken)
  class ExtractFullTextAndSectionsProcessor.Config
    ctor()
    string ExtraCommand { get; set; }
    string ExtraContext { get; set; }
    bool ExtractFullText { get; set; }
    bool ExtractSections { get; set; }
    LLMModel LLMModel { get; set; }
  static class ExtractTextProcessor
    static Task<List<Item>> Run(List<Item> inputItems, ExtractTextProcessor.Config config, CancellationToken cancellationToken)
  class ExtractTextProcessor.Config
    ctor()
    LLMModel LLMModel { get; set; }
  static class FormatWebPageProcessor
    static Task<List<Item>> Run(Item inputItem, FormatWebPageProcessor.Config config, CancellationToken cancellationToken)
  class FormatWebPageProcessor.Config
    ctor()
    string ExtraCommand { get; set; }
    string ExtraContext { get; set; }
    LLMModel LLMModel { get; set; }
  static class FullTextPassthroughProcessor
    static Task<List<Item>> Run(Item inputItem, CancellationToken cancellationToken)
  static class GenerateEmbeddingsProcessor
    static Task<List<Item>> Run(List<Item> inputItems, GenerateEmbeddingsProcessor.Config config, CancellationToken cancellationToken)
  class GenerateEmbeddingsProcessor.Config
    ctor()
    EmbeddingModel EmbeddingModel { get; set; }
  static class GenerateRouterProcessor
    static Task<List<Item>> Run(List<Item> inputItems, CancellationToken cancellationToken)
  static class GenerateSummaryProcessor
    static Task<List<Item>> Run(Item inputItem, GenerateSummaryProcessor.Config config, CancellationToken cancellationToken)
  class GenerateSummaryProcessor.Config
    ctor()
    LLMModel LLMModel { get; set; }
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
  class AnalyzePdfDocument.Result
    ctor()
    string Title { get; set; }
    AnalyzePdfDocument.DocumentType Type { get; set; }
  class ExtractDocumentPageText
    ctor()
    static Task<string> Run(LLMModel llmModel, Item rawTextItem, Item imageItem, CancellationToken cancellationToken = default)
  class ExtractPresentationPageText
    ctor()
    static Task<string> Run(LLMModel llmModel, Item rawTextItem, Item imageItem, CancellationToken cancellationToken = default)
  class ExtractSections
    ctor()
    static Task<ExtractSections.Result> Run(LLMModel llmModel, string documentTextWithLineNumbers, string extraContext, string extraCommand, CancellationToken cancellationToken = default)
  class ExtractSections.Result
    ctor()
    List<ExtractSections.Section> Sections { get; set; }
  class ExtractSections.Section
    ctor()
    int EndLine { get; set; }
    int StartLine { get; set; }
    List<string> TitleHierarchy { get; set; }
  class FormatWebPage
    ctor()
    static Task<FormatWebPage.Result> Run(LLMModel llmModel, string url, string title, string content, string extraContext, string extraCommand, CancellationToken cancellationToken = default)
  class FormatWebPage.Result
    ctor()
    string Content { get; set; }
    bool HasContent { get; set; }
  class GenerateSummary
    ctor()
    static Task<string> Run(LLMModel llmModel, string content, CancellationToken cancellationToken = default)

namespace Ikon.Pipelines.Public.UniversalRag.Utils
  static class TextUtils
    static string TrimMarkdownBackticks(string input)

namespace Ikon.Pipelines.Public.VideoImageSafety
  enum CollageSelectionMode
    SceneThreshold
    FixedInterval
  // Safety analysis for images. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<ImageSafetyPipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class ImageSafetyPipeline
    ctor(IPipelineHost<ImageSafetyPipeline.Config> host)
    // Runs the pipeline. Each input Item's content must deserialize to a ImageSource (the pipeline's inputSchema) — post JSON matching ImageSource as the item content; non-conforming input causes a per-item processing failure (non-JSON content fails to deserialize, and a JSON object missing the required Url field fails downloading downstream), not a silent empty result.
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
  class ImageSafetyPipeline.Config
    ctor()
    // LLM model for analyzing image content and safety
    LLMModel AnalysisModel { get; set; }
    // Maximum number of parallel analysis requests
    int MaxAnalysisParallelism { get; set; }
    // Maximum number of parallel moderation requests
    int MaxModerationParallelism { get; set; }
    // Model used for content moderation
    ClassificationModel ModerationModel { get; set; }
  class ImageSafetyResult
    ctor()
    // Primary content category classification
    string ContentCategory { get; set; }
    // Extracted factual information from the image
    string Facts { get; set; }
    // Recommended audience for this content
    string IdealAudience { get; set; }
    // Description of the image content
    string ImageDescription { get; set; }
    // Interpreted meaning and context of the image
    string ImageMeaning { get; set; }
    // Whether the image passed safety analysis
    bool IsSafe { get; set; }
    // The main safety concern identified
    string PrimaryRisk { get; set; }
    // Suggested actions based on the analysis
    string RecommendedActions { get; set; }
    // Human-readable summary of the safety evaluation
    string SafetySummary { get; set; }
    // The original image source that was analyzed
    ImageSource Source { get; set; }
    // List of safety categories that were triggered
    string[] TriggeredCategories { get; set; }
  class ImageSource
    ctor()
    // Optional description providing context about the image content
    string Description { get; set; }
    // Display name for the image
    string Name { get; set; }
    // URL of the image to analyze
    string Url { get; set; }
  // Safety analysis for video clips. Do not construct this pipeline or call Run directly — run it through PipelineRunner.Initialize<VideoSafetyPipeline> or the ikon pipeline run CLI, which supplies the input branch.
  class VideoSafetyPipeline
    ctor(IPipelineHost<VideoSafetyPipeline.Config> host)
    // Runs the pipeline, orchestrating parallel extraction of audio and frames, transcription, moderation, and analysis. Each input Item's content must deserialize to a VideoSource (the pipeline's inputSchema) — post JSON matching VideoSource as the item content; non-conforming input causes a per-item processing failure (non-JSON content fails to deserialize, and a JSON object missing the required Url field fails downloading downstream), not a silent empty result. ffmpeg/ffprobe must be on PATH for extraction.
    // inputItems: Branch containing video source items to analyze.
    // cancellationToken: Token to cancel the operation.
    Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
  class VideoSafetyPipeline.Config
    ctor()
    // LLM model for analyzing video frames
    LLMModel AnalysisModel { get; set; }
    // Fixed number of collages to generate (0 = auto-calculate based on duration)
    int CollageCount { get; set; }
    // Width in pixels for extracted frames
    int CollageFrameWidth { get; set; }
    // Time interval in minutes between collages when auto-calculating. Only consulted when CollageCount is 0; ignored when CollageCount > 0.
    double CollageIntervalMinutes { get; set; }
    // Frame selection strategy: SceneThreshold uses scene detection, FixedInterval uses even distribution
    CollageSelectionMode CollageSelection { get; set; }
    // LLM model for final safety evaluation
    LLMModel EvaluationModel { get; set; }
    // Target frame extraction rate in frames per second. Only used when CollageSelection is FixedInterval; ignored under the default SceneThreshold mode.
    double FramesPerSecond { get; set; }
    // Maximum number of parallel frame analysis requests
    int MaxAnalysisParallelism { get; set; }
    // Maximum number of frames to extract per collage
    int MaxFrames { get; set; }
    // Maximum number of parallel moderation requests
    int MaxModerationParallelism { get; set; }
    // Model used for content moderation
    ClassificationModel ModerationModel { get; set; }
    // Sensitivity threshold for scene change detection (0-1). Only used when CollageSelection is SceneThreshold (the default).
    double SceneChangeThreshold { get; set; }
    // Number of columns in the frame collage grid
    int TileColumns { get; set; }
    // Number of rows in the frame collage grid
    int TileRows { get; set; }
    // Language code for speech recognition (e.g., en-US)
    string TranscriptionLanguage { get; set; }
    // Speech recognition model to use for transcription
    SpeechRecognizerModel TranscriptionModel { get; set; }
    // Temperature for transcription (0 = deterministic, higher = more varied)
    float TranscriptionTemperature { get; set; }
  class VideoSafetyResult
    ctor()
    // Primary content category classification
    string ContentCategory { get; set; }
    // Extracted factual information from the video
    string Facts { get; set; }
    // Recommended audience for this content
    string IdealAudience { get; set; }
    // Whether the video passed safety analysis
    bool IsSafe { get; set; }
    // Interpreted meaning and context of the video
    string Meaning { get; set; }
    // The main safety concern identified
    string PrimaryRisk { get; set; }
    // Suggested actions based on the analysis
    string RecommendedActions { get; set; }
    // Description of the most representative frame
    string RepresentativeDescription { get; set; }
    // Human-readable summary of the safety evaluation
    string SafetySummary { get; set; }
    // The original video source that was analyzed
    VideoSource Source { get; set; }
    // Audio transcript from the video
    string Transcript { get; set; }
    // List of safety categories that were triggered
    string[] TriggeredCategories { get; set; }
  class VideoSource
    ctor()
    // Optional description providing context about the video content
    string Description { get; set; }
    // Display name for the video
    string Name { get; set; }
    // URL of the video to analyze
    string Url { get; set; }

namespace Ikon.Pipelines.Public.VideoImageSafety.Shaders
  static class AnalyzeImageSafety
    static Task<AnalyzeImageSafety.Result> RunAsync(LLMModel llmModel, byte[] image, string imageMimeType, string sourceName, string sourceDescription, CancellationToken cancellationToken = default)
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
  static class AnalyzeVideoFrames
    static Task<AnalyzeVideoFrames.Result> RunAsync(LLMModel llmModel, byte[] collageImage, string collageImageMimeType, CancellationToken cancellationToken = default)
  class AnalyzeVideoFrames.Result
    ctor()
    string Facts { get; set; }
    string FramesDescription { get; set; }
    string VideoMeaning { get; set; }
  static class EvaluateVideoSafety
    static Task<EvaluateVideoSafety.Result> RunAsync(LLMModel llmModel, string sourceName, string sourceDescription, string transcript, AnalyzeVideoFrames.Result combinedAnalysis, CancellationToken cancellationToken = default)
  class EvaluateVideoSafety.Result
    ctor()
    string ContentCategory { get; set; }
    string IdealAudience { get; set; }
    bool IsSafe { get; set; }
    string PrimaryRisk { get; set; }
    string RecommendedActions { get; set; }
    string SafetySummary { get; set; }
    string[] TriggeredCategories { get; set; }
