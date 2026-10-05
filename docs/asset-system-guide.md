<!-- checked-against: fd8ad941fb71dfe6073ba358 -->

# Asset System Developer Guide

## Overview

The Ikon asset system exposes a uniform abstraction for storing and retrieving files, JSON payloads, and other binary or textual artifacts without binding application code to a specific backend. Each `Asset` instance dispatches every read, write, delete, and listing request to the storage driver that corresponds to the asset class encoded in the `AssetUri`, and propagates change notifications through `AssetEventAsync` so caches can react to updates. The API is asynchronous end-to-end, providing cancellation support where appropriate and surfacing metadata on every transfer to enable optimistic concurrency and lifecycle management.

## Asset URIs

All asset identifiers use the `assets://` scheme defined by `AssetUri`. URIs are composed of optional scope segments followed by the asset class and backend-specific path:

```text
assets://space/{spaceId}/user/{userId}/{asset-class}/{path/to/resource}?{query}
```

Key rules:

- `space` and `user` segments are optional and may appear in that order. They scope the asset inside the storage backend; `space` is the platform's name for a cloud app, and `spaceId` is the app's id.
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

Each storage reports what metadata it has through `AssetMetadata` — MIME type, byte size, update timestamp and tags on the cloud classes, plus a download URL on `CloudFile`/`CloudFilePublic` and the backend-specific identifier on `CloudJson` only, just size, update timestamp and path on `LocalFile`, and just size and resource name on `EmbeddedFile` (a `ListAsync` entry carries only the resource name, so its `Size` is null) — so callers can perform fine-grained reconciliation. Storages with a canonical native addressing scheme may also expose it via `AssetMetadata.NativeUri` (for example `gs://bucket/object` on GCS-backed cloud files); downstream consumers that recognise the scheme can use it as a zero-copy fast path, and callers that do not should ignore it.

Public cloud files can additionally report `AssetMetadata.SameOriginUrl`: the same asset as a root-relative path on your app's own origin. It is present when the app's asset storage is served through the platform's own origin, which is the default, and absent when an app keeps its assets in storage the platform does not front — so treat it as optional and fall back to the absolute URL: `metadata.SameOriginUrl ?? metadata.Url`. Prefer it whenever the URL is going to a browser — being same-origin, it needs no CORS and reaches visitors on networks that allow only the origin they are already on. `view.Image` already does this for you, so an `AssetUri` rendered through the UI needs nothing extra. Anything fetching from *outside* a browser — your own app process, an external service, a webhook — has nothing to resolve a relative path against and must use `Url`.

## Asset metadata helpers

Most read and write operations accept or return an `AssetMetadata` instance. Populate `MimeType`, `Tags`, or `LastModified` when writing so that storage drivers can set headers or enforce optimistic concurrency. `Get*WithMetadataAsync` helpers pair the payload with the metadata in an `AssetContent<T>`, disposing underlying streams automatically when needed.

## Storing data

### `WriteAsync`

`WriteAsync` is the safe way to stream into an asset. It hands your callback a writable stream and commits only when the callback returns: a callback that throws or is cancelled leaves the asset exactly as it was, and the exception propagates. A failure or cancellation during the commit itself, after the store has sent the request that stores the write, may have stored it anyway, and the exception does not say which, so a retried conditional write can see its own first attempt as a conflict. A conditional write (`AssetMetadata.LastModified`) that another writer beat throws `AssetUpdateConflictException`. On a store your app registered itself with `AddStorageAsync`, which cannot abandon a write, the bytes are staged in a temp file and copied in after the callback returns, so there the guarantee holds only until that copy starts.

<!-- ikon-example: asset-guide-write-async -->
```csharp
var assets = Asset.Instance;
var reportUri = new AssetUri(AssetClass.CloudFile, "reports/latest.pdf", spaceId: "space-42");

await using var source = File.OpenRead("./report.pdf");
await assets.WriteAsync(
    reportUri,
    (target, ct) => source.CopyToAsync(target, ct),
    new AssetMetadata(mimeType: "application/pdf", size: source.Length));
```

### `GetWriteStreamAsync`

`GetWriteStreamAsync` returns a writable stream bound to the storage driver identified by the URI. The write is committed when the stream is disposed, allowing each storage to finalize uploads (for example by issuing signed PUT requests). A write that fails before dispose still commits what was written, so prefer `WriteAsync` unless you need the stream itself. On a cloud file the write streams to storage only when the metadata carries its `Size`, and the stream must then receive exactly that many bytes (the `LastModified` conflict check then runs when the stream is opened); without a size the whole object is buffered in memory and uploaded when the stream is disposed.

<!-- ikon-example: asset-guide-write-stream -->
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

Use `SetTextAsync` to persist UTF-8 encoded text to any storage class that accepts textual payloads (for example `CloudJson`). Provide `AssetMetadata.LastModified` when you need optimistic concurrency: the driver validates the value against the current revision and throws `AssetUpdateConflictException` (or returns `AssetWriteStatus.Conflict` from `TrySetTextAsync`). On `CloudFile`, `CloudFilePublic` and `LocalFile`, a `LastModified` for an asset that no longer exists throws `FileNotFoundException` instead (`AssetWriteStatus.NotFound` from `TrySetTextAsync` and `TrySetBytesAsync`), not a conflict.

<!-- ikon-example: asset-guide-set-text -->
```csharp
var settingsUri = new AssetUri(AssetClass.CloudJson, "config/app.json", spaceId: "space-42");
var payload = JsonSerializer.Serialize(settingsObject);
await assets.SetTextAsync(
    settingsUri,
    payload,
    new AssetMetadata(lastModified: cachedMetadata?.LastModified));
```

`TrySetTextAsync` mirrors the behavior but returns an `AssetWriteResult` so you can branch without exceptions:

<!-- ikon-example: asset-guide-try-set-text -->
```csharp
var write = await assets.TrySetTextAsync(settingsUri, payload);
if (write.IsConflict)
{
    // Inspect write.Metadata to decide whether to re-read and retry.
}
```

### `SetBytesAsync` / `TrySetBytesAsync`

`SetBytesAsync` uploads byte arrays that are already materialized in memory. `TrySetBytesAsync` exposes the same optimistic concurrency semantics as the text helper.

<!-- ikon-example: asset-guide-set-bytes -->
```csharp
var thumbnailUri = new AssetUri(AssetClass.CloudFile, "thumbnails/card.jpg", spaceId: "space-42");
await assets.SetBytesAsync(thumbnailUri, thumbnailBytes, new AssetMetadata(mimeType: "image/jpeg"));
```

### `SetAsync<T>`

`SetAsync<T>` serializes arbitrary reference types to JSON and writes the result using `SetTextAsync`; a `string` is written as-is through `SetTextAsync` and a `byte[]` through `SetBytesAsync`. This is a convenient way to persist strongly typed settings without manual serialization.

<!-- ikon-example: asset-guide-set-typed -->
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

<!-- ikon-example: asset-guide-exists -->
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

<!-- ikon-example: asset-guide-read-stream -->
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

`GetWithMetadataAsync<T>` deserializes JSON payloads into the requested type (with fast paths for `string` and `byte[]`) and surfaces metadata. `GetAsync<T>` and `TryGetAsync<T>` return just the content. For any other `T`, an asset that exists but holds empty or whitespace content throws `InvalidDataException` — `TryGetAsync<T>` included, which returns `null` only when the asset does not exist.

<!-- ikon-example: asset-guide-get-typed -->
```csharp
var layout = await assets.GetAsync<DashboardLayout>(
    new AssetUri(AssetClass.CloudJson, "layouts/dashboard.json", spaceId: "space-42"));
```

### Change subscriptions

`GetOrUpdateWithMetadataAsync` wires a callback to an asset. The callback is invoked immediately with the current content and again whenever the underlying storage reports an add, change, or delete event. Only `LocalFile` reports changes (`EmbeddedFile` reports each resource as added at start); the cloud classes report none, so for them the callback fires again only when code in the same process calls `Asset.NotifyUpdateAsync`, and a write from another instance is never seen. Provide `onAssetNotFound` to seed defaults before subscribing; without it, or when it does not create the asset, the call throws `FileNotFoundException`. The returned `IAsyncDisposable` unsubscribes the callback when disposed.

<!-- ikon-example: asset-guide-subscribe -->
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

Use `ListAsync` with an `AssetQuery` to enumerate a folder. Listing is currently supported by the `LocalFile` and `EmbeddedFile` backends only. Cloud backends (`CloudFile`, `CloudFilePublic`, `CloudJson`) do not yet support listing and will throw `NotSupportedException`. The folder prefix is the only filter that applies: a non-empty `Tags` throws `NotSupportedException`, `Limit` caps every listing, and listings are not paged (the query's `NextContinuationToken` is always null), so filter by tag and page the returned list yourself. A `LocalFile` listing ignores the query's space and user and matches the prefix against the file's path on disk, so only an unscoped folder URI lists as below; a space- or user-scoped one never lists its own files, only the unscoped files at the same bare path.

<!-- ikon-example: asset-guide-list -->
```csharp
var folderUri = new AssetUri(AssetClass.LocalFile, "albums/2024/");
var entries = await assets.ListAsync(new AssetQuery(folderUri));
foreach (var entry in entries)
{
    Log.Instance.Info($"{entry.AssetUri.Path} updated {entry.Metadata.LastModified:O}");
}
```

Convenience overloads accept an `AssetClass` and optional prefix or a folder URI directly when only the URIs are required.

## Optimistic concurrency workflow

When an asset must not be overwritten blindly, follow this pattern:

1. Read the asset with metadata (`GetTextWithMetadataAsync`, `GetBytesWithMetadataAsync`, or `GetWithMetadataAsync<T>`).
2. Carry `metadata.LastModified` forward into `SetTextAsync` or `SetBytesAsync` via `AssetMetadata`.
3. Handle `AssetUpdateConflictException` (or check `AssetWriteResult.IsConflict`) to trigger a re-read and retry.

This approach is supported across the `CloudFile`, `CloudFilePublic`, `CloudJson` and `LocalFile` backends. `CloudJson` passes the check to the Hub service as `ifUpdatedAt`, atomic with the write, and `LocalFile` repeats it at commit under the store's per-path lock, so only an edit made outside the store can slip in. `CloudFile` and `CloudFilePublic` compare the timestamp just before uploading, so a concurrent write can still slip between the check and the upload.
