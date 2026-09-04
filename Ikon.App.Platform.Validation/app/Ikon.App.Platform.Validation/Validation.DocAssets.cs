using System.Text.Json;

// The asset system guide, as one file that compiles.
//
// docs/public/asset-system-developer-guide.md reads top to bottom into a single source file: it hands
// the reader `assets` and `settingsUri` in the first two blocks and writes against them for the rest
// of the document. So the holder below is that file — the shared names are its fields, and each
// `#region docsnippet:` is one fence, regenerated into the guide by CodegenPatternGenerator.
//
// A published guide keeps literal code in its fence rather than a marker, because it is the artifact
// itself: embedded into NuGet packages, mirrored to the examples repo, read by the canvas frontend.
// The marker sits above the fence and the build rewrites the fence body.
file sealed class DocAssetGuide
{
    private sealed record DashboardLayout
    {
        public int Columns { get; init; }
        public IReadOnlyList<string> Widgets { get; init; } = [];
    }

    private sealed record Settings(string Theme)
    {
        public static Settings Default => new("light");
    }

    private readonly Asset assets = Asset.Instance;
    private readonly AssetUri photoUri = new(AssetClass.CloudFile, "images/hero.png", spaceId: "space-42");
    private readonly AssetUri settingsUri = new(AssetClass.CloudJson, "config/app.json", spaceId: "space-42");
    private readonly Dictionary<AssetUri, Settings> cache = [];
    private readonly string payload = "{}";
    private readonly object settingsObject = new();
    private readonly AssetMetadata? cachedMetadata = new();
    private readonly byte[] thumbnailBytes = [];
    private readonly IReadOnlyList<string> widgets = [];

    public async Task WriteStreamAsync()
    {
        #region docsnippet:asset-guide-write-stream
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
        #endregion
    }

    public async Task SetTextAsync()
    {
        #region docsnippet:asset-guide-set-text
        var settingsUri = new AssetUri(AssetClass.CloudJson, "config/app.json", spaceId: "space-42");
        var payload = JsonSerializer.Serialize(settingsObject);
        await assets.SetTextAsync(
            settingsUri,
            payload,
            new AssetMetadata(lastModified: cachedMetadata?.LastModified));
        #endregion
    }

    public async Task TrySetTextAsync()
    {
        #region docsnippet:asset-guide-try-set-text
        var write = await assets.TrySetTextAsync(settingsUri, payload);
        if (write.IsConflict)
        {
            // Inspect write.Metadata to decide whether to re-read and retry.
        }
        #endregion
    }

    public async Task SetBytesAsync()
    {
        #region docsnippet:asset-guide-set-bytes
        var thumbnailUri = new AssetUri(AssetClass.CloudFile, "thumbnails/card.jpg", spaceId: "space-42");
        await assets.SetBytesAsync(thumbnailUri, thumbnailBytes, new AssetMetadata(mimeType: "image/jpeg"));
        #endregion
    }

    public async Task SetTypedAsync()
    {
        #region docsnippet:asset-guide-set-typed
        await assets.SetAsync(
            new AssetUri(AssetClass.CloudJson, "layouts/dashboard.json", spaceId: "space-42"),
            new DashboardLayout { Columns = 3, Widgets = widgets });
        #endregion
    }

    public async Task ExistsAndMetadataAsync()
    {
        #region docsnippet:asset-guide-exists
        if (!await assets.ExistsAsync(settingsUri))
        {
            throw new InvalidOperationException("Missing configuration asset.");
        }

        var metadata = await assets.GetMetadataAsync(settingsUri);
        Log.Instance.Info($"Last updated {metadata.LastModified:O}");
        #endregion
    }

    public async Task ReadStreamAsync()
    {
        #region docsnippet:asset-guide-read-stream
        var download = await assets.GetReadStreamAsync(photoUri);
        using (download)
        {
            await using var destination = File.Create("./downloaded.png");
            await download.Content.CopyToAsync(destination);
        }

        var script = await assets.GetTextAsync(new AssetUri(AssetClass.EmbeddedFile, "Scripts/init.sql"));
        #endregion

        Log.Instance.Debug($"{script}");
    }

    public async Task GetTypedAsync()
    {
        #region docsnippet:asset-guide-get-typed
        var layout = await assets.GetAsync<DashboardLayout>(
            new AssetUri(AssetClass.CloudJson, "layouts/dashboard.json", spaceId: "space-42"));
        #endregion

        Log.Instance.Debug($"{layout}");
    }

    public async Task SubscribeAsync()
    {
        #region docsnippet:asset-guide-subscribe
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
        #endregion
    }

    public async Task ListAsync()
    {
        #region docsnippet:asset-guide-list
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
        #endregion

        Log.Instance.Debug($"{nextPageToken}");
    }
}
