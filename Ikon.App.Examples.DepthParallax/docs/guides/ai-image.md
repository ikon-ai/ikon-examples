# AI Image Generation

## AI Image Generation

Generate AI images with the one-shot `ImageGenerator.GenerateAsync(prompt)`. Supports Gemini, GPT Image, Flux models. Returns image bytes and mime type.

```csharp
var image = await ImageGenerator.GenerateAsync("A neon-lit cyberpunk street");  // Gemini25FlashImage (cheap+fast) by default
// image.Data, image.MimeType — never null; throws ImageGeneratorException on failure
```

Pass a model as the second argument to override the default: `ImageGenerator.GenerateAsync(prompt, ImageGeneratorModel.Gemini3ProImage)`.

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
if (results.Count > 0) { var image = results[0]; /* image.Data, image.MimeType */ }
```

---

# Ikon.AI Public API
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
    // Defaults to Gemini25FlashImage (cheap+fast). Override the model via the second parameter when the task warrants. Never returns null — throws an ImageGeneratorException when generation fails or the model produces no results, so wrap in try/catch when the app should continue without the image. Reach for the constructor + GenerateImageAsync when you need batch generation, custom width/height, an ImageBackground override, input images, or any other ImageGeneratorConfig field beyond the prompt.
    static Task<ImageGeneratorResult> GenerateAsync(string prompt, ImageGeneratorModel model = Gemini25FlashImage, CancellationToken cancellationToken = default)
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
    static string DisplayName(this ImageGeneratorModel model)
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
  class NonRetryableImageGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SafetyLevel
    Level0
    Level1
    Level2
    Level3
    Level4
    Level5
    Level6
