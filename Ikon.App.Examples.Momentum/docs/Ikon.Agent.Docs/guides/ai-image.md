# AI Image Generation

## AI Image Generation

Generate AI images with the one-shot `ImageGenerator.GenerateAsync(prompt)`. Supports Gemini, GPT Image, Flux models.

```csharp
var image = await ImageGenerator.GenerateAsync("A neon-lit cyberpunk street");  // Gemini25FlashImage (cheap+fast) by default
var bytes = await image.GetDataAsync();  // payload bytes, downloaded transparently when delivered as a URL
// image.MimeType — never null; throws AIException on failure
```

Pass a model as the second argument to override the default: `ImageGenerator.GenerateAsync(prompt, ImageGeneratorModel.Gemini3ProImage)`.

> **Result delivery (`result.Kind`):** results carry inline bytes by default (`Kind == ResultKind.Data`, `Data` non-null). When a result is returned from a remotely hosted AI function and its payload exceeds a few MB, it is automatically uploaded and comes back as a signed download URL valid for roughly one hour (`Kind == ResultKind.Url`, `Url` non-null, `Data` null) to stay within the protocol message limit; run locally, large payloads stay inline. `await result.GetDataAsync()` returns the bytes either way — prefer it over reading `Data` directly. Set `ResultDelivery = ResultDelivery.Url` in the config to always get a URL. Music, sound-effect, file-conversion, segmentation, depth, and upscaling results follow the same pattern.

Reach for the constructor + config form only when you need width/height, batch generation, input images, or other `ImageGeneratorConfig` fields:

> **The config form always takes `ImageGeneratorConfig`, not a raw string.** The `Prompt` goes inside the config object.
> `imageGenerator.GenerateImageAsync("prompt")` will NOT compile — use `new ImageGeneratorConfig { Prompt = "..." }`.

```csharp
using var imageGenerator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);
var results = await imageGenerator.GenerateImageAsync(new ImageGeneratorConfig
{
    Prompt = "A neon-lit cyberpunk street",
    Width = 512,
    Height = 512
});
if (results.Count > 0) { var image = results[0]; /* await image.GetDataAsync(), image.MimeType */ }
```

> **A size the model cannot render throws**, rather than being clamped or bucketed to a nearby one — so ask for a size the model lists, or leave `Width` and `Height` at `0` to take the provider's own default. Set both or neither; half a size is refused. The result's `Width`/`Height` are measured from the returned bytes, not echoed from the request, because models snap to their own grids and tiers inside what they accept.

### Image Upscaling (super-resolution)

Raise an image's resolution with the one-shot `ImageUpscaler.UpscaleAsync(bytes, mimeType)`. One image in, one larger image out.

```csharp
var result = await ImageUpscaler.UpscaleAsync(imageBytes, "image/png", scaleFactor: 4);  // SeedVr2 by default
var bytes = await result.Image.GetDataAsync();
```

> **Faithful vs. creative.** Every model's `Fidelity` says whether it invents detail. `Faithful` reconstructs only what the input supports; `Creative` synthesizes detail that was never there; `Tunable` moves between the two as `Creativity` rises (0 to 1) and sits at the faithful end at 0. No model is `Creative` today, so nothing invents detail unless you raise `Creativity` on a `Tunable` model; asking a `Faithful` model for `Creativity` above 0 throws rather than being ignored. Call `ImageUpscaler.GetCapabilities(model)` when it matters.

Use the constructor + config form for URL input, a target resolution, or creative upscaling:

```csharp
using var imageUpscaler = new ImageUpscaler(ImageUpscalerModel.SeedVr2);
var result = await imageUpscaler.UpscaleImageAsync(new ImageUpscalerConfig
{
    InputImage = new InputImage { Url = "https://example.com/photo.png" },
    TargetResolution = UpscaleTargetResolution.Uhd2160
});
```

`ScaleFactor` and `TargetResolution` are mutually exclusive, and a model throws on either one it does not support rather than ignoring it. `ScaleFactor = 0` leaves the model's own default in place.

---

# Ikon.AI Public API
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
    Task<ImageUpscalerResult> UpscaleAsync(byte[] imageData, string mimeType, double scaleFactor = 0.0, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an ImageUpscaler per call. Defaults to ImageUpscalerModel.SeedVr2, which reconstructs detail faithfully and bills per output megapixel. scaleFactor of 0 leaves the model's own default in place. Reach for ImageUpscalerModel.Crystal, the UpscaleFidelity.Tunable model, with ImageUpscalerConfig.Creativity above 0 to let it invent detail. The upscaled image is in result.Image (.Data/.MimeType). Use the constructor + UpscaleImageAsync for a URL source or any other config field.
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
  enum ImageUpscalerModel
    SeedVr2
    Topaz
    RecraftCrisp
    Crystal
    // extension methods: ImageUpscalerModelExtensions{DisplayName}
  static class ImageUpscalerModelExtensions
    static string DisplayName(this ImageUpscalerModel model)
  sealed record ImageUpscalerResult
    ctor()
    OutputImage Image { get; init; }
    // Which provenance layers the upscaled bytes carry. ProvenanceMarking.Full is the norm on PNG/JPEG output; anything less means the re-mark degraded (WebP, an undecodable payload, a failed marking pass — each logged at Warning).
    ProvenanceMarking Provenance { get; init; }
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
