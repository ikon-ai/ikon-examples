namespace Ikon.AI.FileConversion
  // Kind tells how the file was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record ConvertedFile : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string Name { get; init; }
    string? Url { get; init; }
  sealed class FileConverter : IFileConverter
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(FileConverterModel model, IReadOnlyList<ModelRegion>? regions = null)
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
    Task<ConvertedFile> ConvertToPdfAsync(byte[] data, string fileName, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a FileConverter per call. fileName must carry the source extension (e.g. report.docx) — it determines the input format. The PDF is in result.Data. Use the constructor + ConvertToPdfAsync for a URL or AssetUri source, or a custom timeout.
    static Task<ConvertedFile> ConvertToPdfAsync(byte[] data, string fileName, FileConverterModel model = ConvertApi, CancellationToken cancellationToken = default)
    void Dispose()
    static IReadOnlyList<ModelRegion> GetSupportedRegions(FileConverterModel model)
  // Supply the file exactly one way: Data, Url, or AssetUri (resolved to a URL automatically). FileName must carry the source extension (e.g. report.docx) — it determines the input format.
  sealed record FileConverterConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    byte[]? Data { get; init; }
    string FileName { get; init; }
    ResultDelivery ResultDelivery { get; init; }
    TimeSpan Timeout { get; init; }
    string? Url { get; init; }
  enum FileConverterModel
    ConvertApi
  static class FileConverterModelExtensions
    static string DisplayName(this FileConverterModel model)
  interface IFileConverter : IDisposable
    Task<ConvertedFile> ConvertToPdfAsync(FileConverterConfig config, CancellationToken cancellationToken = default)
