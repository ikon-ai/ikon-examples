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
    // True when the model can produce output with a transparent background (ImageGeneratorConfig.Background = ImageBackground.Transparent). Check this first: only the OpenAI implementation rejects an unsupported request, and the others ignore ImageGeneratorConfig.Background outright, so the image comes back opaque with no error.
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
    Task<ImageGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an ImageGenerator per call. Defaults to ImageGeneratorModel.Gemini25FlashImage (cheap+fast); override via model. Never returns null — throws an AIException on failure (RetryableAIException for a transient failure or empty output, NonRetryableAIException for a rejected request), so catch AIException to continue without the image. Use the constructor + GenerateImageAsync for batch/size/input-image or any other ImageGeneratorConfig field.
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
    // Images per request. The call fails when the provider returns fewer than asked, so a batch is never short without a word; above 1 needs IImageGeneratorInfo.SupportsMultipleOutputs.
    int Count { get; init; }
    // Requested pixel height; see Width for how tiered providers treat it.
    int Height { get; init; }
    // Each model has a ceiling (Together and xAI take one, Kontext four, FLUX.2 eight or four); more than that throws rather than dropping the extras.
    List<InputImage> InputImages { get; init; }
    // Embed Ikon's imperceptible provenance watermark in the result pixels (EU AI Act Article 50 machine-readable marking, uniform across providers). The XMP metadata mark is always written regardless of this flag; disabling this skips the pixel pass — and, for JPEG results, the one high-quality re-encode it costs.
    bool InvisibleWatermark { get; init; }
    string NegativePrompt { get; init; }
    string Prompt { get; init; }
    // Honoured by the OpenAI gpt-image models only; anything but ImageQuality.Auto on another model throws rather than being dropped.
    ImageQuality Quality { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    SafetyLevel SafetyLevel { get; init; }
    // 0 leaves the provider's own choice. Set on a model that takes no seed (OpenAI, xAI) it throws rather than being dropped.
    int Seed { get; init; }
    // 0 leaves the provider's own choice. Honoured by the Together models, flux-2-flex (1-50) and flux-1-fill (15-50); set on any other model, or outside the model's range, it throws rather than being dropped or clamped.
    int Steps { get; init; }
    TimeSpan Timeout { get; init; }
    // Honoured by the FLUX.1 models only; set on any other model it throws rather than being dropped.
    bool UpsamplePrompt { get; init; }
    // Renders a small corner badge with this text on the result (e.g. "AI"). Empty = no visible mark. Intended as a plan-tier lever, not a compliance requirement — the machine-readable marks above are what Article 50 asks for.
    string VisibleWatermark { get; init; }
    // The only way to request a size, and set together with Height or not at all — 0x0 leaves the size to the provider. A size the model cannot render is refused, not substituted: outside its pixel range, or, on the OpenAI models, not one of the three sizes they list. Inside the range it snaps to the provider's own grid or tier (32 pixels on the FLUX models, the nearest tier at or above the request on Gemini's 1K/2K/4K and xAI's 1k/2k), and the size that came back is on ImageGeneratorResult.Width.
    int Width { get; init; }
  enum ImageGeneratorModel
    GptImage1Mini
    GptImage15
    GptImage2
    Gemini25FlashImage
    Gemini3ProImage
    Gemini31FlashImage
    Gemini31FlashLiteImage
    Flux1Dev
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
    GrokImagineImage2
    GrokImagineImageQuality
    // extension methods: ImageGeneratorModelExtensions{DisplayName}
  static class ImageGeneratorModelExtensions
    static string DisplayName(this ImageGeneratorModel model)
  // Kind tells how the image was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record ImageGeneratorResult : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    // Measured like Width.
    int Height { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    // Which provenance layers the delivered bytes carry. ProvenanceMarking.Full is the norm; anything less means the mark degraded (undecodable payload, WebP, a failed marking pass — each logged at Warning) and the image ships without the Article 50 pixel mark the config asked for.
    ProvenanceMarking Provenance { get; init; }
    string? Url { get; init; }
    // Read from the returned bytes, not echoed from the request: providers snap to their own tiers and caps, so this can differ from ImageGeneratorConfig.Width. 0 when the header could not be read (logged at Warning).
    int Width { get; init; }
    // extension methods on IResultPayload, using Ikon.AI: AssetOutputs{GetDataAsync}
  enum ImageQuality
    Auto
    Low
    Medium
    High
  // Provider-mapped moderation strength; Moderate is the default.
  enum SafetyLevel
    None
    Minimal
    Low
    Moderate
    High
    VeryHigh
    Maximum
