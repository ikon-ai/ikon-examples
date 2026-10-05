namespace Ikon.App.Patterns.Examples;

internal sealed partial class AgentGuideExamples
{

    private static async Task DocAssetMetadataAsync(AssetUri uri)
    {
        #region example:asset-metadata
        bool exists = await Asset.Instance.ExistsAsync(uri);
        var metadata = await Asset.Instance.GetMetadataAsync(uri);  // .Size, .LastModified, .Url, .UrlIsTemporal, .MimeType
        #endregion

        _ = (exists, metadata);
    }

    private static async Task DocAssetExpiryAsync(string key, byte[] bytes, string spaceId)
    {
        #region example:asset-temporary-expiry
        await Asset.Instance.SetBytesAsync(
            new AssetUri(AssetClass.CloudFile, $"exports/{key}/report.csv", spaceId: spaceId),
            bytes,
            new AssetMetadata(mimeType: "text/csv", expiresAt: DateTime.UtcNow.AddHours(6)));
        #endregion
    }

    private static async Task DocAssetListingAsync(AssetUri uri, AssetUri folderUri)
    {
        #region example:asset-listing
        var entries = await Asset.Instance.ListAsync(new AssetQuery(folderUri) { Limit = 50 });
        await Asset.Instance.DeleteAsync(uri);
        #endregion

        _ = entries;
    }

    private void DocAssetUriConstruction()
    {
        #region example:asset-uri-construction
        // URIs use assets:// scheme with optional scope segments (space, user)
        var localFile = new AssetUri(AssetClass.LocalFile, "image.jpg");
        var cloudFile = new AssetUri(AssetClass.CloudFile, "path/file.jpg", spaceId: app.GlobalState.SpaceId);
        var publicFile = new AssetUri(AssetClass.CloudFilePublic, "path/file.jpg", spaceId: app.GlobalState.SpaceId);
        var cloudJson = new AssetUri(AssetClass.CloudJson, "path/data.json", spaceId: app.GlobalState.SpaceId);
        #endregion

        _ = (localFile, cloudFile, publicFile, cloudJson);
    }

    private static async Task DocAssetSubscriptionAsync(AssetUri uri, Dictionary<AssetUri, Settings> cache)
    {
        #region example:asset-change-subscription
        await Asset.Instance.GetOrUpdateWithMetadataAsync<Settings>(uri,
            async (args, content) =>
            {
                if (content is null) { cache.Remove(uri); return; }
                cache[uri] = content.Content;
            },
            async _ => await Asset.Instance.SetAsync(uri, Settings.Default));
        #endregion
    }

    private static async Task DocAssetReadWriteAsync(AssetUri uri, string jsonString)
    {
        #region example:asset-read-write
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
        #endregion

        Log.Instance.Debug($"{text} {layout} {readStream} {writeStream}");
    }
}
