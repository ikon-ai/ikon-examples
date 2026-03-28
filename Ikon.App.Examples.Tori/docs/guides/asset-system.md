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
var cloudFile = new AssetUri(AssetClass.CloudFile, "path/file.jpg", spaceId: app.GlobalState.SpaceId);
var publicFile = new AssetUri(AssetClass.CloudFilePublic, "path/file.jpg", spaceId: app.GlobalState.SpaceId);
var cloudJson = new AssetUri(AssetClass.CloudJson, "path/data.json", spaceId: app.GlobalState.SpaceId);
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
