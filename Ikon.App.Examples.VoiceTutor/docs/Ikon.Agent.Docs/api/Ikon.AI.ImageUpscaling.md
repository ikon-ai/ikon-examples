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
