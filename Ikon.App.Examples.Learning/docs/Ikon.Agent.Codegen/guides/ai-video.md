# AI Video

## AI Video

### Video Generation

One-shot:

```csharp
var video = await VideoGenerator.GenerateAsync("A timelapse of a flower blooming");  // Veo31Fast (cheap+fast) by default
// video.Url (string)
```

Use the constructor + config form for input images (image-to-video), length, resolution, or aspect ratio:

```csharp
using var generator = new VideoGenerator(VideoGeneratorModel.Veo31);
var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig
{
    Prompt = "A timelapse of a flower blooming",
    AspectRatio = VideoGeneratorAspectRatio.Ratio16x9,
    Length = 5
});
// result.Url (string)
```

### Video Playback

To DISPLAY/play a video (e.g. the `result.Url` from generation) inline, use the
`view.VideoUrlPlayer` component — there is no `view.Video`, no raw HTML `<video>`
tag, and no custom React component needed.

```csharp
view.VideoUrlPlayer(
    ["w-full rounded-xl"],
    url: clip.Url,
    controls: true,
    autoplay: false,
    loop: false,
    muted: false,
    poster: clip.PosterUrl);  // optional still-frame shown before play
```

### Video Enhancement

Enhance a hosted clip with the one-shot `VideoEnhancer.EnhanceAsync(videoUrl)` (defaults to `TensorPixUpscale2xUltra41`):

```csharp
var enhanced = await VideoEnhancer.EnhanceAsync(clipUrl);
// enhanced.Url (string), enhanced.OutputFps, enhanced.OutputSizeBytes
```

Reach for the constructor + config form for raw video bytes, frame ranges, or a target FPS:

```csharp
using var enhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixUpscale2xUltra41);
var result = await enhancer.EnhanceVideoAsync(new VideoEnhancerConfig
{
    VideoData = videoBytes,
    MimeType = "video/mp4"
});
// result.Url (string), result.OutputFps, result.OutputSizeBytes
```

---

# Ikon.AI Public API
namespace Ikon.AI.VideoEnhancement
  interface IVideoEnhancer : IDisposable
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
  class NonRetryableVideoEnhancerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class VideoEnhancer : IDisposable, IVideoEnhancer
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoEnhancerModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    // Enhance a video by URL — the instance form of the VideoEnhancer.EnhanceAsync one-shot, for when you already hold an enhancer. Reach for VideoEnhancer.EnhanceVideoAsync when the request needs any other VideoEnhancerConfig field.
    Task<VideoEnhancerResult> EnhanceAsync(string videoUrl, CancellationToken cancellationToken = default)
    // One-shot video enhancement from a video URL. The verbose form
    // using var enhancer = new VideoEnhancer(VideoEnhancerModel.TensorPixUpscale2xUltra41);
    // var result = await enhancer.EnhanceVideoAsync(new VideoEnhancerConfig { VideoUrl = url });
    // becomes
    // var enhanced = await VideoEnhancer.EnhanceAsync(url);
    // Defaults to VideoEnhancerModel.TensorPixUpscale2xUltra41 (the current 2x upscale generation — cheaper than the 4x filter). Override the model via the second parameter when the task warrants. Returns the enhanced video as a download URL in .Url along with .OutputFps and .OutputSizeBytes. Reach for the constructor + VideoEnhancer.EnhanceVideoAsync when you need to enhance raw video bytes (VideoData), trim to a frame range, set a target FPS for VideoEnhancerModel.TensorPixFpsBoost, or any other VideoEnhancerConfig field beyond the URL.
    static Task<VideoEnhancerResult> EnhanceAsync(string videoUrl, VideoEnhancerModel model = TensorPixUpscale2xUltra41, CancellationToken cancellationToken = default)
    Task<VideoEnhancerResult> EnhanceVideoAsync(VideoEnhancerConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(VideoEnhancerModel model)
  sealed class VideoEnhancerConfig : IEquatable<VideoEnhancerConfig>
    ctor()
    int? EndFrame { get; init; }
    string? MimeType { get; init; }
    int? StartFrame { get; init; }
    int? TargetFps { get; init; }
    TimeSpan Timeout { get; init; }
    byte[]? VideoData { get; init; }
    string? VideoUrl { get; init; }
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
  sealed class VideoEnhancerResult : IEquatable<VideoEnhancerResult>
    ctor()
    int? OutputFps { get; init; }
    long? OutputSizeBytes { get; init; }
    string Url { get; init; }

namespace Ikon.AI.VideoGeneration
  interface IVideoGenerator : IDisposable, IVideoGeneratorInfo
    Task<VideoGeneratorResult> GenerateVideoAsync(VideoGeneratorConfig config, CancellationToken cancellationToken = default)
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
  sealed class VideoGeneratorConfig.InputImage : IEquatable<VideoGeneratorConfig.InputImage>
    ctor()
    byte[]? Data { get; init; }
    string? MimeType { get; init; }
    string? Url { get; init; }
  class NonRetryableVideoGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class VideoGenerator : IDisposable, IVideoGenerator, IVideoGeneratorInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(VideoGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
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
    // Generate a video from a plain prompt — the instance form of the VideoGenerator.GenerateAsync one-shot, for when you already hold a generator. Reach for VideoGenerator.GenerateVideoAsync when the request needs any other VideoGeneratorConfig field.
    Task<VideoGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // One-shot text-to-video. The verbose form
    // using var generator = new VideoGenerator(VideoGeneratorModel.Veo31Fast);
    // var result = await generator.GenerateVideoAsync(new VideoGeneratorConfig { Prompt = prompt });
    // becomes
    // var video = await VideoGenerator.GenerateAsync(prompt);
    // Defaults to VideoGeneratorModel.Veo31Fast (the cheap+fast tier of the strongest general-purpose family). Override the model via the second parameter when the task warrants. Returns the result with the generated clip's .Url. Reach for the constructor + VideoGenerator.GenerateVideoAsync when you need input images (image-to-video), a specific length, resolution, aspect ratio, negative prompt, audio, or any other VideoGeneratorConfig field beyond the prompt.
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
    int MaxInputImages { get; init; }
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
  sealed class VideoGeneratorConfig : IEquatable<VideoGeneratorConfig>
    ctor()
    VideoGeneratorAspectRatio AspectRatio { get; init; }
    bool? GenerateAudio { get; init; }
    List<VideoGeneratorConfig.InputImage> InputImages { get; init; }
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
    Hailuo23
    Hailuo23Fast
    Kling26
    Kling30
    Kling30Omni
    KlingVideoO1
    LumaRay20
    LumaRay20Flash
    Pika22
    Pixverse55
    Pollo20
    Pollo30
    Pollodance20
    Pollodance20Fast
    RunwayGen4
    Seedance15Pro
    Sora2
    Sora2Pro
    Veo31
    Veo31Fast
    ViduQ2Pro
    ViduQ2Turbo
    ViduQ3Pro
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
  sealed class VideoGeneratorResult : IEquatable<VideoGeneratorResult>
    ctor()
    string Url { get; init; }
