# Media File Analysis (ffmpeg)

## Media File Analysis (ffmpeg)

How to accept user-uploaded video/audio files and analyze them WITHOUT copying the full file into the app container. Containers have LIMITED disk and RAM — far too small for video work — so the golden path keeps the bytes in cloud asset storage end to end:

1. `view.FileUpload` returning an `AssetUri` from `onUploadStart` — the upload streams DIRECTLY into asset storage, never touching container disk.
2. `Asset.Instance.GetMetadataAsync(uri)` — its `.Url` is a temporary signed URL to the stored file.
3. `ffprobe` / `ffmpeg` take that URL as input directly — they stream over HTTP (with range requests for seeking), so metadata probing, frame extraction, audio extraction, and transcoding all run without ever materializing the file locally.

**ffmpeg and ffprobe are preinstalled in the standard app containers** (the default runtime plus the workflow/creator images; the browser-scraping runtime is the exception) — launch them with `Process.Start`, no packages or setup needed. (`Process`/`ProcessStartInfo` live in `System.Diagnostics` — not an Ikon namespace and not in `GlobalUsings.cs`, so add it there; `JsonDocument`'s `System.Text.Json` is already in the managed usings block.)

### Step 1 — upload directly to asset storage

Return an `AssetUri` from `onUploadStart` and the platform routes the upload bytes straight into asset storage instead of a local temp file (`view.FileUploadZone` takes the same callbacks for drag-and-drop):

```csharp
private readonly Reactive<AssetUri?> _mediaAssetUri = new(null);
```

Then in the UI lambda:

```csharp
view.FileUpload(
    accept: ["video/*", "audio/*"],
    maxFileSize: 2L * 1024 * 1024 * 1024,
    onUploadStart: async args =>
    {
        var assetUri = new AssetUri(AssetClass.CloudFile, $"uploads/{args.Hash}/{args.FileName}", spaceId: app.GlobalState.SpaceId);
        return new FileUploadResult { Accepted = true, AssetUri = assetUri };
    },
    onUploadComplete: async args =>
    {
        if (args.AssetUri is not { } assetUri)
        {
            return;
        }

        _mediaAssetUri.Value = assetUri;
        await AnalyzeMediaAsync(assetUri);
    });
```

The callbacks hand back the `AssetUri` struct itself (`AssetUri?`), the same type every `Asset.Instance.*` call takes — pattern-match the null away and pass it straight on.

For a progress bar, add `onUploadChunk`. It fires per chunk with a `FileUploadChunkArgs` — the
`UploadId`, the client-supplied `FileName` and `MimeType`, the `Size` the client announced, this
chunk's `Data`, and `BytesWritten` including this chunk. The `Data` is valid only for the duration
of the callback, so copy it if you keep it. `onUploadComplete` fires only after the byte count and a
recomputed SHA-256 both match, so a truncated or corrupted upload never reaches it.

### Step 2 — get a temp URL and probe with ffprobe

Add `using System.Diagnostics;` — the scaffold's global usings do not carry it.

```csharp
private static async Task<JsonDocument?> ProbeMediaAsync(AssetUri assetUri)
{
    // The signed URL is temporary (.UrlIsTemporal) — fetch it fresh
    // right before each ffprobe/ffmpeg invocation, never persist it
    var metadata = await Asset.Instance.GetMetadataAsync(assetUri);

    if (metadata.Url is null)
    {
        return null;
    }

    var startInfo = new ProcessStartInfo
    {
        FileName = "ffprobe",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    // ArgumentList, never an interpolated Arguments string — URL characters can
    // otherwise be misparsed as ffprobe option flags
    foreach (var arg in new[] { "-v", "quiet", "-print_format", "json", "-show_format", "-show_streams", metadata.Url })
    {
        startInfo.ArgumentList.Add(arg);
    }

    using var process = Process.Start(startInfo);

    if (process is null)
    {
        return null;
    }

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

    try
    {
        string output = await process.StandardOutput.ReadToEndAsync(cts.Token);
        await process.WaitForExitAsync(cts.Token);
        return process.ExitCode == 0 ? JsonDocument.Parse(output) : null;
    }
    catch (OperationCanceledException)
    {
        try { process.Kill(); } catch { /* the probe is already being abandoned; a kill that fails changes nothing */ }

        return null;
    }
}
```

The JSON carries `format.duration`, `format.bit_rate`, and per-stream `codec_name` / `width` / `height` / `sample_rate` / `channels` — enough for duration display, validation, and routing to further analysis.

### Step 3 — heavier analysis: stream ffmpeg output through pipes

Give ffmpeg the same signed URL as `-i` and pipe its output to stdout; read it incrementally. Neither the input media nor the output ever lands on container disk:

```csharp
// Extract mono 16 kHz PCM audio (e.g. for speech recognition); for thumbnail
// frames instead, swap the output args for "-f", "image2pipe", "-vcodec", "mjpeg"
// with a select/fps filter
var startInfo = new ProcessStartInfo
{
    FileName = "ffmpeg",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

foreach (var arg in new[] { "-loglevel", "quiet", "-i", url, "-vn", "-f", "f32le", "-ac", "1", "-ar", "16000", "pipe:1" })
{
    startInfo.ArgumentList.Add(arg);
}

using var process = Process.Start(startInfo);

if (process is null)
{
    return;
}

await using var audioStream = process.StandardOutput.BaseStream;
// read fixed-size chunks from audioStream — do NOT ReadToEnd a long file
```

For byte-level access from C# without ffmpeg, `Asset.Instance.GetReadStreamAsync(uri)` returns an `AssetContent<Stream>` whose `.Content` is a `Stream` over the stored object — process it incrementally the same way.

### Anti-patterns (each one exhausts container disk or RAM on real-world media)

- Do NOT skip the `AssetUri` in `onUploadStart` and then read `args.LocalTempFilePath` for video/audio — that lands the whole file on container disk. (Temp-file uploads are fine for small images/documents.)
- Do NOT `Asset.Instance.GetBytesAsync(uri)` a video/audio asset — that loads the whole file into RAM. Bytes are fine for images and documents; media streams.
- Do NOT download the asset to a local file just to hand ffmpeg a path — ffmpeg reads URLs natively.

---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Assets
  sealed class Asset : AsyncLocalInstance<Asset>, IAsyncDisposable
    ctor()
    Task AddStorageAsync(AssetClass assetClass, IStorage storage, bool startInBackground = false)
    Task DeleteAsync(AssetUri assetUri)
    ValueTask DisposeAsync()
    Task<bool> ExistsAsync(AssetUri assetUri)
    Task<T> GetAsync<T>(AssetUri assetUri) where T : class
    Task<byte[]> GetBytesAsync(AssetUri assetUri)
    Task<AssetContent<byte[]>> GetBytesWithMetadataAsync(AssetUri assetUri)
    Task<AssetMetadata> GetMetadataAsync(AssetUri assetUri)
    Task<IAsyncDisposable> GetOrUpdateWithMetadataAsync<T>(AssetUri assetUri, Func<AssetEventArgs, AssetContent<T>?, Task> onAsset, Func<AssetEventArgs, Task>? onAssetNotFound = null) where T : class
    Task<IAsyncDisposable> GetOrUpdateWithMetadataAsync<T>(AssetUri assetUri, Action<AssetEventArgs, AssetContent<T>?> onAsset, Func<AssetEventArgs, Task>? onAssetNotFound = null) where T : class
    Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    Task<string> GetTextAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<string>> GetTextWithMetadataAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<T>> GetWithMetadataAsync<T>(AssetUri assetUri) where T : class
    Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
    // Only AssetClass.LocalFile and AssetClass.EmbeddedFile storages can be listed today; the cloud classes throw NotSupportedException. See AssetQuery for which query fields each storage honours.
    // throws NotSupportedException: The storage for AssetQuery.Class does not support listing
    Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken = default)
    // Same storage support as ListAsync: cloud classes throw NotSupportedException.
    Task<IReadOnlyList<AssetUri>> ListAsync(AssetClass assetClass, string? prefix = null, CancellationToken cancellationToken = default)
    // Same storage support as ListAsync: cloud classes throw NotSupportedException.
    Task<IReadOnlyList<AssetUri>> ListAsync(AssetUri folderUri, CancellationToken cancellationToken = default)
    Task NotifyUpdateAsync(AssetUri assetUri)
    Task SetAsync<T>(AssetUri assetUri, T asset, AssetMetadata? metadata = null, CancellationToken cancellationToken = default) where T : class
    Task SetBytesAsync(AssetUri assetUri, byte[] bytes, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
    Task SetTextAsync(AssetUri assetUri, string text, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
    Task<T?> TryGetAsync<T>(AssetUri assetUri) where T : class
    Task<byte[]?> TryGetBytesAsync(AssetUri assetUri)
    Task<AssetContent<byte[]>?> TryGetBytesWithMetadataAsync(AssetUri assetUri)
    Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    Task<string?> TryGetTextAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<string>?> TryGetTextWithMetadataAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<T>?> TryGetWithMetadataAsync<T>(AssetUri assetUri) where T : class
    Task<AssetWriteResult> TrySetBytesAsync(AssetUri assetUri, byte[] bytes, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
    Task<AssetWriteResult> TrySetTextAsync(AssetUri assetUri, string text, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
  enum AssetClass
    // Server's local filesystem under a system-managed root; not cloud-persisted.
    LocalFile
    // Baked into the app assembly as an embedded resource; read-only at runtime.
    EmbeddedFile
    // Persistent private cloud storage for any file, small or large, binary or text.
    CloudFile
    // Persistent public cloud storage; the asset is reachable via a public URL.
    CloudFilePublic
    // Persistent private cloud storage for small JSON text values.
    CloudJson
  sealed class AssetContent<T> : IDisposable
    ctor(T content, AssetMetadata? metaData = null)
    T Content { get; }
    AssetMetadata? MetaData { get; }
    void Dispose()
  class AssetEventArgs : EventArgs
    ctor(AssetUri assetUri, AssetStatus status)
    AssetUri AssetUri { get; }
    AssetStatus Status { get; }
  readonly struct AssetListingEntry
    ctor(AssetUri assetUri, AssetMetadata metadata)
    AssetUri AssetUri { get; }
    AssetMetadata Metadata { get; }
  readonly struct AssetMetadata
    ctor(string? mimeType = null, long? size = null, DateTime? lastModified = null, string? url = null, bool? urlIsTemporal = null, string[]? tags = null, string? internalPath = null, string? storageId = null, string? nativeUri = null, bool? isAppServed = null, DateTime? expiresAt = null)
    DateTime? ExpiresAt { get; }
    string? InternalPath { get; }
    bool? IsAppServed { get; }
    DateTime? LastModified { get; }
    string? MimeType { get; }
    string? NativeUri { get; }
    string? SameOriginUrl { get; init; }
    long? Size { get; }
    string? StorageId { get; }
    string[]? Tags { get; }
    string? Url { get; }
    bool? UrlIsTemporal { get; }
  // Only the AssetClass.LocalFile and AssetClass.EmbeddedFile storages list at all, and they honour different fields: EffectiveFolderPrefix always filters; Limit caps the embedded-file listing only; Tags, ContinuationToken and NextContinuationToken are reserved for the cloud storages and are ignored today, so setting them still yields the full, unfiltered listing.
  sealed class AssetQuery
    ctor(AssetClass assetClass)
    ctor(AssetUri folderUri)
    AssetClass Class { get; }
    string? ContinuationToken { get; set; }
    string? EffectiveFolderPrefix { get; }
    string? EffectiveSpaceId { get; }
    string? EffectiveUserId { get; }
    string? FolderPrefix { get; set; }
    AssetUri? FolderUri { get; set; }
    int? Limit { get; set; }
    string? NextContinuationToken { get; set; }
    string? SpaceId { get; set; }
    string[]? Tags { get; set; }
    string? UserId { get; set; }
    AssetQuery Clone()
  enum AssetStatus
    None
    Added
    Exists
    Changed
    Deleted
  sealed class AssetUpdateConflictException : Exception
    ctor(AssetUri assetUri, AssetMetadata? metadata)
    AssetUri AssetUri { get; }
    AssetMetadata? Metadata { get; }
  // Grammar: assets://[space/{spaceId}/][user/{userId}/]{class}/{path}[?query]. {class} is the kebab-case AssetClass (local-file, embedded-file, cloud-file, cloud-file-public, cloud-json) and selects the storage backend; {path} may include subdirectories and a file name. The optional space/user segments scope the asset — omit them for a global asset. Immutable; With returns a modified copy. A legacy channel/{id}/ segment is still accepted on parse and discarded (read tolerance for pre-migration URIs) — it is never emitted.
  readonly struct AssetUri : IEquatable<AssetUri>
    ctor(string uriString)
    ctor(AssetClass assetClass, string? path = null, string? spaceId = null, string? userId = null, string? query = null)
    AssetClass Class { get; }
    string FileName { get; }
    string Path { get; }
    string? Query { get; }
    static string Scheme { get; }
    string? SpaceId { get; }
    string? UserId { get; }
    static AssetUri FromFilesystemPath(string relativePathToRoot, AssetClass defaultAssetClass = LocalFile)
    static bool IsValid(string uriString)
    static string ToFilesystemPath(AssetUri assetUri)
    static bool TryParse(string uriString, out AssetUri assetUri, out string? failureReason)
    static bool TryParse(string uriString, out AssetUri assetUri)
    AssetUri With(AssetClass? assetClass = null, string? path = null, string? spaceId = null, string? userId = null, string? query = null)
    static bool operator ==(AssetUri left, AssetUri right)
    static bool operator !=(AssetUri left, AssetUri right)
  readonly struct AssetWriteResult
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
    Task DeleteAsync(AssetUri assetUri)
    Task<bool> ExistsAsync(AssetUri assetUri)
    Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata, CancellationToken cancellationToken)
    Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken)
    Task StartAsync()
    Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    Task WaitUntilQueueEmptyAsync()
    event Func<AssetEventArgs, Task> AssetEventAsync
  static class StorageExtensions
    static Task AddEmbeddedFileStorageAsync(this Asset asset, Assembly? assembly = null, string resourceNamespace = "")

---

# Asset System Developer Guide

## Overview

The Ikon asset system exposes a uniform abstraction for storing and retrieving files, JSON payloads, and other binary or textual artifacts without binding application code to a specific backend. Each `Asset` instance dispatches every read, write, delete, and listing request to the storage driver that corresponds to the asset class encoded in the `AssetUri`, and propagates change notifications through `AssetEventAsync` so caches can react to updates. The API is asynchronous end-to-end, providing cancellation support where appropriate and surfacing metadata on every transfer to enable optimistic concurrency and lifecycle management.

## Asset URIs

All asset identifiers use the `assets://` scheme defined by `AssetUri`. URIs are composed of optional scope segments followed by the asset class and backend-specific path:

```
assets://space/{spaceId}/user/{userId}/{asset-class}/{path/to/resource}?{query}
```

Key rules:

- `space` and `user` segments are optional and may appear in that order. They scope the asset inside the storage backend.
- The asset class segment must match one of the values defined in `AssetClass` (for example `cloud-file`, `cloud-json`, or `embedded-file`).
- The remaining path is interpreted by the storage driver and can include nested folders.
- `AssetUri` instances normalize the file name, expose `With` helpers for cloning with modified components, and provide converters for filesystem paths when assets need to be mirrored locally.

## Storage classes

`AssetClass` maps human-readable URI segments to the available backend implementations. Use the class that best matches the data profile:

| Asset class | URI segment | Characteristics |
|-------------|-------------|-----------------|
| `LocalFile` | `local-file` | File-system backed, primarily for local development and tooling. Paths are rooted under a system-managed directory. |
| `EmbeddedFile` | `embedded-file` | Read-only assets embedded into an assembly. Ideal for shipping seed data and scripts. |
| `CloudFile` | `cloud-file` | Private cloud object storage optimized for arbitrary binary payloads. Supports signed URLs, metadata, and optimistic concurrency tokens. |
| `CloudFilePublic` | `cloud-file-public` | Same backing service as `CloudFile` but exposes public URLs for assets meant to be shared openly. |
| `CloudJson` | `cloud-json` | JSON documents persisted through the Hub API, suited for low-latency configuration payloads. Supports optimistic concurrency via the `LastModified` timestamp. |

Each storage reports metadata such as MIME type, byte size, update timestamp, tags, download URL (when applicable), and the backend-specific identifier through `AssetMetadata` so callers can perform fine-grained reconciliation. Storages with a canonical native addressing scheme may also expose it via `AssetMetadata.NativeUri` (for example `gs://bucket/object` on GCS-backed cloud files); downstream consumers that recognise the scheme can use it as a zero-copy fast path, and callers that do not should ignore it.

Public cloud files additionally report `AssetMetadata.SameOriginUrl`: the same asset as a root-relative path on your app's own origin. Prefer it whenever the URL is going to a browser — being same-origin, it needs no CORS and reaches visitors on networks that allow only the origin they are already on. `view.Image` already prefers it for you, so an `AssetUri` rendered through the UI needs nothing extra. Anything fetching from *outside* a browser — your own app process, an external service, a webhook — has nothing to resolve a relative path against and must use `Url`.

## Asset metadata helpers

Most read and write operations accept or return an `AssetMetadata` instance. Populate `MimeType`, `Tags`, or `LastModified` when writing so that storage drivers can set headers or enforce optimistic concurrency. `Get*WithMetadataAsync` helpers pair the payload with the metadata in an `AssetContent<T>`, disposing underlying streams automatically when needed.

## Storing data

### `GetWriteStreamAsync`

`GetWriteStreamAsync` returns a writable stream bound to the storage driver identified by the URI. The write is committed when the stream is disposed, allowing each storage to finalize uploads (for example by issuing signed PUT requests).

```csharp
var assets = Asset.Instance;
var photoUri = new AssetUri(
    assetClass: AssetClass.CloudFile,
    path: "images/hero.png",
    spaceId: "space-42");

await using var writeStream = await assets.GetWriteStreamAsync(
    photoUri,
    metadata: new AssetMetadata(mimeType: "image/png"));
await using var fileStream = File.OpenRead("./hero.png");
await fileStream.CopyToAsync(writeStream);
```

### `SetTextAsync` / `TrySetTextAsync`

Use `SetTextAsync` to persist UTF-8 encoded text to any storage class that accepts textual payloads (for example `CloudJson`). Provide `AssetMetadata.LastModified` when you need optimistic concurrency: the driver validates the value against the current revision and throws `AssetUpdateConflictException` (or returns `AssetWriteStatus.Conflict` from `TrySetTextAsync`).

```csharp
var settingsUri = new AssetUri(AssetClass.CloudJson, "config/app.json", spaceId: "space-42");
var payload = JsonSerializer.Serialize(settingsObject);
await assets.SetTextAsync(
    settingsUri,
    payload,
    new AssetMetadata(lastModified: cachedMetadata?.LastModified));
```

`TrySetTextAsync` mirrors the behavior but returns an `AssetWriteResult` so you can branch without exceptions:

```csharp
var write = await assets.TrySetTextAsync(settingsUri, payload);
if (write.IsConflict)
{
    // Inspect write.Metadata to decide whether to re-read and retry.
}
```

### `SetBytesAsync` / `TrySetBytesAsync`

`SetBytesAsync` uploads byte arrays that are already materialized in memory. `TrySetBytesAsync` exposes the same optimistic concurrency semantics as the text helper.

```csharp
var thumbnailUri = new AssetUri(AssetClass.CloudFile, "thumbnails/card.jpg", spaceId: "space-42");
await assets.SetBytesAsync(thumbnailUri, thumbnailBytes, new AssetMetadata(mimeType: "image/jpeg"));
```

### `SetAsync<T>`

`SetAsync<T>` serializes arbitrary reference types to JSON (unless the value is already `string` or `byte[]`) and writes the result using `SetTextAsync`. This is a convenient way to persist strongly typed settings without manual serialization.

```csharp
await assets.SetAsync(
    new AssetUri(AssetClass.CloudJson, "layouts/dashboard.json", spaceId: "space-42"),
    new DashboardLayout { Columns = 3, Widgets = widgets });
```

## Loading data

### Existence and metadata

- `ExistsAsync` checks whether an asset is present.
- `GetMetadataAsync` returns metadata or throws if the asset is missing.
- `TryGetMetadataAsync` returns `null` when metadata is unavailable.

```csharp
if (!await assets.ExistsAsync(settingsUri))
{
    throw new InvalidOperationException("Missing configuration asset.");
}

var metadata = await assets.GetMetadataAsync(settingsUri);
Log.Instance.Info($"Last updated {metadata.LastModified:O}");
```

### Streams and primitives

- `GetReadStreamAsync` returns `AssetContent<Stream>` so callers can stream large files while inspecting metadata.
- `GetTextWithMetadataAsync` / `GetTextAsync` read UTF-8 text by default and support explicit encodings. `TryGet*` variants avoid throwing.
- `GetBytesWithMetadataAsync` / `GetBytesAsync` materialize the asset into memory as a byte array.

```csharp
var download = await assets.GetReadStreamAsync(photoUri);
using (download)
{
    await using var destination = File.Create("./downloaded.png");
    await download.Content.CopyToAsync(destination);
}

var script = await assets.GetTextAsync(new AssetUri(AssetClass.EmbeddedFile, "Scripts/init.sql"));
```

### Structured objects

`GetWithMetadataAsync<T>` deserializes JSON payloads into the requested type (with fast paths for `string` and `byte[]`) and surfaces metadata. `GetAsync<T>` and `TryGetAsync<T>` return just the content.

```csharp
var layout = await assets.GetAsync<DashboardLayout>(
    new AssetUri(AssetClass.CloudJson, "layouts/dashboard.json", spaceId: "space-42"));
```

### Change subscriptions

`GetOrUpdateWithMetadataAsync` wires a callback to an asset. The callback is invoked immediately with the current content and again whenever the underlying storage reports an add, change, or delete event. Provide `onAssetNotFound` to seed defaults before subscribing.

```csharp
await assets.GetOrUpdateWithMetadataAsync<Settings>(
    settingsUri,
    async (args, content) =>
    {
        if (content is null)
        {
            cache.Remove(settingsUri);
            return;
        }

        cache[settingsUri] = content.Content;
    },
    async _ => await assets.SetAsync(settingsUri, Settings.Default));
```

## Listing assets

Use `ListAsync` with an `AssetQuery` to enumerate folders, filter by tags, and paginate through large collections. Listing is currently supported by the `LocalFile` and `EmbeddedFile` backends only. Cloud backends (`CloudFile`, `CloudFilePublic`, `CloudJson`) do not yet support listing and will throw `NotSupportedException`.

```csharp
var folderUri = new AssetUri(AssetClass.LocalFile, "albums/2024/");
var query = new AssetQuery(folderUri)
{
    Tags = new[] { "cover" },
    Limit = 50,
};

var entries = await assets.ListAsync(query);
foreach (var entry in entries)
{
    Log.Instance.Info($"{entry.AssetUri.Path} updated {entry.Metadata.LastModified:O}");
}

var nextPageToken = query.NextContinuationToken;
```

Convenience overloads accept an `AssetClass` and optional prefix or a folder URI directly when only the URIs are required.

## Optimistic concurrency workflow

When an asset must not be overwritten blindly, follow this pattern:

1. Read the asset with metadata (`GetTextWithMetadataAsync`, `GetBytesWithMetadataAsync`, or `GetWithMetadataAsync<T>`).
2. Carry `metadata.LastModified` forward into `SetTextAsync` or `SetBytesAsync` via `AssetMetadata`.
3. Handle `AssetUpdateConflictException` (or check `AssetWriteResult.IsConflict`) to trigger a re-read and retry.

This approach is supported consistently across `CloudFile` and `CloudJson` backends and aligns with the Hub service’s `ifUpdatedAt` semantics.
