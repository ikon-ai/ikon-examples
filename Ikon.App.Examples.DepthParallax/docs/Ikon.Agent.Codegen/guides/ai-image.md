# AI Image Generation

## AI Image Generation

Generate AI images with `new ImageGenerator(model)` and `GenerateImageAsync(config)`. Supports Gemini, GPT Image, Flux models. Returns image bytes and mime type.

> **Always pass `ImageGeneratorConfig`, not a raw string.** The `Prompt` goes inside the config object.
> `imageGenerator.GenerateImageAsync("prompt")` will NOT compile — use `new ImageGeneratorConfig { Prompt = "..." }`.

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

---

# Ikon.AI Public API
namespace Ikon.AI.ImageGeneration
  interface IImageGenerator : IDisposable
    abstract Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = null)
  enum ImageBackground
    Auto
    Opaque
    Transparent
  sealed class ImageGenerator : IDisposable, IImageGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // One-shot image generation. The verbose form using var generator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage); var results = await generator.GenerateImageAsync(new ImageGeneratorConfig { Prompt = prompt }); var image = results.FirstOrDefault(); becomes var image = await ImageGenerator.GenerateAsync(prompt); Defaults to Gemini25FlashImage (cheap+fast). Override the model via the second parameter when the task warrants. Returns null if the model produces no results — caller should null-check before using .Data / .MimeType. Reach for the constructor + GenerateImageAsync when you need batch generation, custom width/height, an ImageBackground override, input images, or any other ImageGeneratorConfig field beyond the prompt.
    static Task<ImageGeneratorResult?> GenerateAsync(string prompt, ImageGeneratorModel model = Gemini25FlashImage, CancellationToken cancellationToken = null)
    Task<List<ImageGeneratorResult>> GenerateImageAsync(ImageGeneratorConfig config, CancellationToken cancellationToken = null)
    static ImageGeneratorCapabilities GetCapabilities(ImageGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageGeneratorModel model)
  sealed class ImageGeneratorCapabilities
    ctor()
  sealed class ImageGeneratorConfig
    ctor()
    ImageBackground Background { get; set; }
    int Count { get; set; }
    int Height { get; set; }
    List<InputImage> InputImages { get; set; }
    string NegativePrompt { get; set; }
    string Prompt { get; set; }
    ImageQuality Quality { get; set; }
    SafetyLevel SafetyLevel { get; set; }
    string SearchPrompt { get; set; }
    int Seed { get; set; }
    int Steps { get; set; }
    string Style { get; set; }
    TimeSpan Timeout { get; set; }
    bool UpsamplePrompt { get; set; }
    int Width { get; set; }
    static ImageGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
  sealed class ImageGeneratorResult
    ctor()
    byte[] Data { get; set; }
    int Height { get; set; }
    string MimeType { get; set; }
    int Width { get; set; }
    static ImageGeneratorResult ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ImageQuality
    Auto
    Low
    Medium
    High
  sealed class InputImage
    ctor()
    byte[] Data { get; set; }
    double? MaskDilution { get; set; }
    string MimeType { get; set; }
    double? Strength { get; set; }
    InputImageType Type { get; set; }
    static InputImage ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
