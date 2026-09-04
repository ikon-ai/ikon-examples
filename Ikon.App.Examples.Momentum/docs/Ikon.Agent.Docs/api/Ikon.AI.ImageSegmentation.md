namespace Ikon.AI.ImageSegmentation
  interface IImageSegmenter : IDisposable
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed class ImageSegmenter : IImageSegmenter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(ImageSegmenterModel model, IReadOnlyList<ModelRegion>? regions = null)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(ImageSegmenterModel model)
    Task<ImageSegmenterResult> SegmentAsync(byte[] imageData, string mimeType, string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes an ImageSegmenter per call. Defaults to ImageSegmenterModel.Sam31; override via model. Each detected object is in result.Segments with its mask image, score, and bounding box. Use the constructor + SegmentImageAsync for a URL source, point/box prompts, multiple masks per object, or other fields.
    static Task<ImageSegmenterResult> SegmentAsync(byte[] imageData, string mimeType, string prompt, ImageSegmenterModel model = Sam31, CancellationToken cancellationToken = default)
    Task<ImageSegmenterResult> SegmentImageAsync(ImageSegmenterConfig config, CancellationToken cancellationToken = default)
  sealed record ImageSegmenterConfig
    ctor()
    List<ImageSegmenterConfig.BoxPrompt> BoxPrompts { get; init; }
    InputImage InputImage { get; init; }
    int MaxMasks { get; init; }
    List<ImageSegmenterConfig.PointPrompt> PointPrompts { get; init; }
    string? Prompt { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    bool ReturnMultipleMasks { get; init; }
    TimeSpan Timeout { get; init; }
  sealed record ImageSegmenterConfig.BoxPrompt
    ctor()
    int? ObjectId { get; init; }
    double XMax { get; init; }
    double XMin { get; init; }
    double YMax { get; init; }
    double YMin { get; init; }
  sealed record ImageSegmenterConfig.PointPrompt
    ctor()
    bool IsBackground { get; init; }
    int? ObjectId { get; init; }
    double X { get; init; }
    double Y { get; init; }
  enum ImageSegmenterModel
    Sam3
    Sam31
  static class ImageSegmenterModelExtensions
    static string DisplayName(this ImageSegmenterModel model)
  sealed record ImageSegmenterResult
    ctor()
    OutputImage? Preview { get; init; }
    List<ImageSegmenterResult.Segment> Segments { get; init; }
  sealed record ImageSegmenterResult.Segment
    ctor()
    List<double> Box { get; init; }
    OutputImage Mask { get; init; }
    double? Score { get; init; }
