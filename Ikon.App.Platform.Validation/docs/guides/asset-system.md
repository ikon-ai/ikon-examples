# Asset System

## Asset System

Uniform abstraction for storing and retrieving files, JSON payloads, and binary artifacts across multiple storage backends. All operations go through `Asset.Instance`.

### Asset classes

| Class | URI segment | Use case |
|-------|-------------|----------|
| `LocalFile` | `local-file` | File-system backed, local development. Files stored under the app Data directory. |
| `EmbeddedFile` | `embedded-file` | Read-only assets embedded into an assembly (seed data, scripts). |
| `CloudFile` | `cloud-file` | Private cloud object storage for arbitrary binary payloads. Supports signed URLs and optimistic concurrency. |
| `CloudFilePublic` | `cloud-file-public` | Same as CloudFile but exposes public URLs. |
| `CloudJson` | `cloud-json` | JSON documents via Hub API, suited for low-latency configuration payloads. Supports optimistic concurrency. |

### URI construction

```csharp
// URIs use assets:// scheme with optional scope segments (space, user, channel)
var localFile = new AssetUri(AssetClass.LocalFile, "image.jpg");
var cloudFile = new AssetUri(AssetClass.CloudFile, "path/file.jpg", spaceId: host.GlobalState.SpaceId);
var publicFile = new AssetUri(AssetClass.CloudFilePublic, "path/file.jpg", spaceId: host.GlobalState.SpaceId);
var cloudJson = new AssetUri(AssetClass.CloudJson, "path/data.json", spaceId: host.GlobalState.SpaceId);
```

### Read/Write

```csharp
// Bytes
var bytes = await Asset.Instance.GetBytesAsync(uri);
await Asset.Instance.SetBytesAsync(uri, bytes, new AssetMetadata(mimeType: MimeTypes.ImageJpeg));

// Text
var text = await Asset.Instance.GetTextAsync(uri);
await Asset.Instance.SetTextAsync(uri, jsonString);

// Typed objects (JSON serialization)
var layout = await Asset.Instance.GetAsync<DashboardLayout>(uri);
await Asset.Instance.SetAsync(uri, new DashboardLayout { Columns = 3 });

// Streams
await using var readStream = (await Asset.Instance.GetReadStreamAsync(uri)).Content;
await using var writeStream = await Asset.Instance.GetWriteStreamAsync(uri, new AssetMetadata(mimeType: "image/png"));
```

### Metadata & existence

```csharp
bool exists = await Asset.Instance.ExistsAsync(uri);
var metadata = await Asset.Instance.GetMetadataAsync(uri);  // .Size, .LastModified, .Url, .UrlIsTemporal, .MimeType
```

### Optimistic concurrency

Read with metadata, carry `LastModified` forward into writes to prevent blind overwrites:

```csharp
var content = await Asset.Instance.GetTextWithMetadataAsync(uri);
// ... modify content.Content ...
var result = await Asset.Instance.TrySetTextAsync(uri, modified, new AssetMetadata(lastModified: content.Metadata.LastModified));
if (result.IsConflict) { /* re-read and retry */ }
```

### Change subscriptions

```csharp
await Asset.Instance.GetOrUpdateWithMetadataAsync(uri,
    async (args, content) =>
    {
        if (content is null) { cache.Remove(uri); return; }
        cache[uri] = content.Content;
    },
    async _ => await Asset.Instance.SetAsync(uri, Settings.Default));
```

### Listing & deletion

```csharp
var entries = await Asset.Instance.ListAsync(new AssetQuery(folderUri) { Limit = 50 });
await Asset.Instance.DeleteAsync(uri);
```

---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Assets
  sealed class Asset : AsyncLocalInstance<Asset>, IAsyncDisposable
    ctor()
    IkonBackend Backend { get;  set; }
    Task AddStorageAsync(AssetClass assetClass, IStorage storage, bool startInBackground = false)
    Task DeleteAsync(AssetUri assetUri)
    ValueTask DisposeAsync()
    Task<bool> ExistsAsync(AssetUri assetUri)
    Task<T> GetAsync<T>(AssetUri assetUri)
    Task<byte[]> GetBytesAsync(AssetUri assetUri)
    Task<AssetContent<byte[]>> GetBytesWithMetadataAsync(AssetUri assetUri)
    Task<AssetMetadata> GetMetadataAsync(AssetUri assetUri)
    Task<IAsyncDisposable> GetOrUpdateWithMetadataAsync<T>(AssetUri assetUri, Func<AssetEventArgs, AssetContent<T>, Task> onAsset, Func<AssetEventArgs, Task> onAssetNotFound = null)
    Task<IAsyncDisposable> GetOrUpdateWithMetadataAsync<T>(AssetUri assetUri, Action<AssetEventArgs, AssetContent<T>> onAsset, Func<AssetEventArgs, Task> onAssetNotFound = null)
    Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    Task<string> GetTextAsync(AssetUri assetUri, Encoding encoding = null)
    Task<AssetContent<string>> GetTextWithMetadataAsync(AssetUri assetUri, Encoding encoding = null)
    Task<AssetContent<T>> GetWithMetadataAsync<T>(AssetUri assetUri)
    Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<AssetUri>> ListAsync(AssetClass assetClass, string prefix = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<AssetUri>> ListAsync(AssetUri folderUri, CancellationToken cancellationToken = null)
    Task NotifyUpdateAsync(AssetUri assetUri)
    Task SetAsync<T>(AssetUri assetUri, T asset, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
    Task SetBytesAsync(AssetUri assetUri, byte[] bytes, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
    Task SetTextAsync(AssetUri assetUri, string text, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
    Task<T> TryGetAsync<T>(AssetUri assetUri)
    Task<byte[]> TryGetBytesAsync(AssetUri assetUri)
    Task<AssetContent<byte[]>> TryGetBytesWithMetadataAsync(AssetUri assetUri)
    Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    Task<string> TryGetTextAsync(AssetUri assetUri, Encoding encoding = null)
    Task<AssetContent<string>> TryGetTextWithMetadataAsync(AssetUri assetUri, Encoding encoding = null)
    Task<AssetContent<T>> TryGetWithMetadataAsync<T>(AssetUri assetUri)
    Task<AssetWriteResult> TrySetBytesAsync(AssetUri assetUri, byte[] bytes, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
    Task<AssetWriteResult> TrySetTextAsync(AssetUri assetUri, string text, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
  enum AssetClass
    LocalFile
    EmbeddedFile
    CloudFile
    CloudFilePublic
    CloudJson
    CloudProfile
  sealed class AssetContent<T> : IDisposable
    ctor(T content, AssetMetadata? metaData = null)
    T Content { get; }
    AssetMetadata? MetaData { get; }
    void Dispose()
  class AssetEventArgs : EventArgs
    ctor(AssetUri assetUri, AssetStatus status)
    AssetUri AssetUri { get; }
    AssetStatus Status { get; }
  struct AssetListingEntry
    ctor(AssetUri assetUri, AssetMetadata metadata)
    AssetUri AssetUri { get; }
    AssetMetadata Metadata { get; }
  struct AssetMetadata
    ctor(string mimeType = null, long? size = null, DateTime? lastModified = null, string url = null, bool? urlIsTemporal = null, string[] tags = null, string internalPath = null, string storageId = null)
    string InternalPath { get; }
    DateTime? LastModified { get; }
    string MimeType { get; }
    long? Size { get; }
    string StorageId { get; }
    string[] Tags { get; }
    string Url { get; }
    bool? UrlIsTemporal { get; }
  sealed class AssetQuery
    ctor(AssetClass assetClass)
    ctor(AssetUri folderUri)
    string ChannelId { get;  set; }
    AssetClass Class { get; }
    string ContinuationToken { get;  set; }
    string EffectiveChannelId { get; }
    string EffectiveFolderPrefix { get; }
    string EffectiveSpaceId { get; }
    string EffectiveUserId { get; }
    string FolderPrefix { get;  set; }
    AssetUri? FolderUri { get;  set; }
    int? Limit { get;  set; }
    string NextContinuationToken { get;  set; }
    string SpaceId { get;  set; }
    string[] Tags { get;  set; }
    string UserId { get;  set; }
    AssetQuery Clone()
  enum AssetStatus
    None
    Added
    Exists
    Changed
    Deleted
  struct AssetUri : IEquatable<AssetUri>
    ctor(string uriString)
    ctor(AssetClass assetClass, string path = null, string spaceId = null, string userId = null, string channelId = null, string query = null)
    string ChannelId { get; }
    AssetClass Class { get; }
    string FileName { get; }
    string Path { get; }
    string Query { get; }
    static string Scheme { get; }
    string SpaceId { get; }
    string UserId { get; }
    static AssetUri FromFilesystemPath(string relativePathToRoot, AssetClass defaultAssetClass = LocalFile)
    static bool IsValid(string uriString)
    static string ToFilesystemPath(AssetUri assetUri)
    static bool TryParse(string uriString, out AssetUri assetUri, out string failureReason)
    static bool TryParse(string uriString, out AssetUri assetUri)
    AssetUri With(AssetClass? assetClass = null, string path = null, string spaceId = null, string userId = null, string channelId = null, string query = null)
  struct AssetWriteResult
    ctor(AssetWriteStatus status, AssetMetadata? metadata = null)
    bool IsConflict { get; }
    AssetMetadata? Metadata { get; }
    AssetWriteStatus Status { get; }
    bool Succeeded { get; }
  enum AssetWriteStatus
    NotFound
    Conflict
    Skipped
    Success
  interface IStorage : IAsyncDisposable
    abstract Task DeleteAsync(AssetUri assetUri)
    abstract Task<bool> ExistsAsync(AssetUri assetUri)
    abstract Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    abstract Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata, CancellationToken cancellationToken)
    abstract Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken)
    abstract Task StartAsync()
    abstract Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    abstract Task WaitUntilQueueEmptyAsync()
    event Func<AssetEventArgs, Task> AssetEventAsync
  static class StorageExtensions
    static Task AddEmbeddedFileStorageAsync(Asset asset, Assembly assembly = null, string resourceNamespace = "")
