using System.Text.Json;

public partial class Validation
{
    private void RenderAssetsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            RenderLocalFileCard(view);
            RenderCloudFileCard(view);
            RenderCloudFilePublicCard(view);
            RenderCloudJsonCard(view);
        });
    }

    private void RenderLocalFileCard(UIView view)
    {
        var uri = new AssetUri(AssetClass.LocalFile, "santa.jpg");

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "LocalFile");
            view.Text([Text.Caption, "font-mono mb-2"], $"URI: {uri}");

            view.Button([Button.PrimaryMd], label: "Load from Data", onClick: async () =>
            {
                var bytes = await Asset.Instance.GetBytesAsync(uri);
                _assetLocalFileData.Value = bytes;
                _assetLocalFileStatus.Value = $"Loaded ({bytes.Length} bytes)";
            });

            view.Text([Text.Body, "mt-2"], _assetLocalFileStatus.Value);

            if (_assetLocalFileData.Value != null)
            {
                view.Image(["max-w-xs h-auto rounded-lg mt-2"], data: _assetLocalFileData.Value, mimeType: MimeTypes.ImageJpeg);
            }
        });
    }

    private void RenderCloudFileCard(UIView view)
    {
        var uri = new AssetUri(AssetClass.CloudFile, "validation/santa.jpg", spaceId: host.GlobalState.SpaceId);

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "CloudFile");
            view.Text([Text.Caption, "font-mono mb-2"], $"URI: {uri}");

            view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
            {
                view.Button([Button.PrimaryMd], label: "Upload",
                    disabled: _assetCloudFileUploading.Value,
                    onClick: async () =>
                    {
                        _assetCloudFileUploading.Value = true;
                        try
                        {
                            var bytes = await File.ReadAllBytesAsync(Path.Combine(host.DataDirectory, "santa.jpg"));
                            await Asset.Instance.SetBytesAsync(uri, bytes, new AssetMetadata(mimeType: MimeTypes.ImageJpeg));
                            var metadata = await Asset.Instance.GetMetadataAsync(uri);
                            _assetCloudFileStatus.Value = "Uploaded";
                            _assetCloudFileMetadata.Value = $"Size: {metadata.Size}, LastModified: {metadata.LastModified}, URL temporal: {metadata.UrlIsTemporal}";
                            _assetCloudFileBackingUrl.Value = metadata.Url ?? "";
                        }
                        finally
                        {
                            _assetCloudFileUploading.Value = false;
                        }
                    });

                view.Button([Button.PrimaryMd], label: "Download & Show", onClick: async () =>
                {
                    var bytes = await Asset.Instance.GetBytesAsync(uri);
                    _assetCloudFileDownloaded.Value = bytes;
                });

                view.Button([Button.PrimaryMd], label: "Show via URL", onClick: async () =>
                {
                    var metadata = await Asset.Instance.GetMetadataAsync(uri);
                    _assetCloudFileUrl.Value = metadata.Url;
                });

                view.Button([Button.DangerMd], label: "Delete", onClick: async () =>
                {
                    await Asset.Instance.DeleteAsync(uri);
                    _assetCloudFileStatus.Value = "Not uploaded";
                    _assetCloudFileMetadata.Value = "";
                    _assetCloudFileBackingUrl.Value = "";
                    _assetCloudFileDownloaded.Value = null;
                    _assetCloudFileUrl.Value = null;
                });
            });

            view.Text([Text.Body, "mt-2"], _assetCloudFileStatus.Value);

            if (!string.IsNullOrEmpty(_assetCloudFileMetadata.Value))
            {
                view.Text([Text.Caption, "mt-1"], _assetCloudFileMetadata.Value);
            }

            if (!string.IsNullOrEmpty(_assetCloudFileBackingUrl.Value))
            {
                view.Text([Text.Caption, "font-mono mt-1 break-all"], $"URL: {_assetCloudFileBackingUrl.Value}");
            }

            if (_assetCloudFileDownloaded.Value != null)
            {
                view.Text([Text.Caption, "mt-2"], "Downloaded bytes:");
                view.Image(["max-w-xs h-auto rounded-lg mt-1"], data: _assetCloudFileDownloaded.Value, mimeType: MimeTypes.ImageJpeg);
            }

            if (_assetCloudFileUrl.Value != null)
            {
                view.Text([Text.Caption, "mt-2"], "Via URL:");
                view.Image(["max-w-xs h-auto rounded-lg mt-1"], src: _assetCloudFileUrl.Value);
            }
        });
    }

    private void RenderCloudFilePublicCard(UIView view)
    {
        var uri = new AssetUri(AssetClass.CloudFilePublic, "validation/santa-public.jpg", spaceId: host.GlobalState.SpaceId);

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "CloudFilePublic");
            view.Text([Text.Caption, "font-mono mb-2"], $"URI: {uri}");

            view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
            {
                view.Button([Button.PrimaryMd], label: "Upload",
                    disabled: _assetCloudFilePublicUploading.Value,
                    onClick: async () =>
                    {
                        _assetCloudFilePublicUploading.Value = true;
                        try
                        {
                            var bytes = await File.ReadAllBytesAsync(Path.Combine(host.DataDirectory, "santa.jpg"));
                            await Asset.Instance.SetBytesAsync(uri, bytes, new AssetMetadata(mimeType: MimeTypes.ImageJpeg));
                            var metadata = await Asset.Instance.GetMetadataAsync(uri);
                            _assetCloudFilePublicStatus.Value = "Uploaded";
                            _assetCloudFilePublicMetadata.Value = $"Size: {metadata.Size}, LastModified: {metadata.LastModified}, URL temporal: {metadata.UrlIsTemporal}";
                            _assetCloudFilePublicBackingUrl.Value = metadata.Url ?? "";
                        }
                        finally
                        {
                            _assetCloudFilePublicUploading.Value = false;
                        }
                    });

                view.Button([Button.PrimaryMd], label: "Download & Show", onClick: async () =>
                {
                    var bytes = await Asset.Instance.GetBytesAsync(uri);
                    _assetCloudFilePublicDownloaded.Value = bytes;
                });

                view.Button([Button.PrimaryMd], label: "Show via URL", onClick: async () =>
                {
                    var metadata = await Asset.Instance.GetMetadataAsync(uri);
                    _assetCloudFilePublicUrl.Value = metadata.Url;
                });

                view.Button([Button.DangerMd], label: "Delete", onClick: async () =>
                {
                    await Asset.Instance.DeleteAsync(uri);
                    _assetCloudFilePublicStatus.Value = "Not uploaded";
                    _assetCloudFilePublicMetadata.Value = "";
                    _assetCloudFilePublicBackingUrl.Value = "";
                    _assetCloudFilePublicDownloaded.Value = null;
                    _assetCloudFilePublicUrl.Value = null;
                });
            });

            view.Text([Text.Body, "mt-2"], _assetCloudFilePublicStatus.Value);

            if (!string.IsNullOrEmpty(_assetCloudFilePublicMetadata.Value))
            {
                view.Text([Text.Caption, "mt-1"], _assetCloudFilePublicMetadata.Value);
            }

            if (!string.IsNullOrEmpty(_assetCloudFilePublicBackingUrl.Value))
            {
                view.Text([Text.Caption, "font-mono mt-1 break-all"], $"URL: {_assetCloudFilePublicBackingUrl.Value}");
            }

            if (_assetCloudFilePublicDownloaded.Value != null)
            {
                view.Text([Text.Caption, "mt-2"], "Downloaded bytes:");
                view.Image(["max-w-xs h-auto rounded-lg mt-1"], data: _assetCloudFilePublicDownloaded.Value, mimeType: MimeTypes.ImageJpeg);
            }

            if (_assetCloudFilePublicUrl.Value != null)
            {
                view.Text([Text.Caption, "mt-2"], "Via URL:");
                view.Image(["max-w-xs h-auto rounded-lg mt-1"], src: _assetCloudFilePublicUrl.Value);
            }
        });
    }

    private void RenderCloudJsonCard(UIView view)
    {
        var uri = new AssetUri(AssetClass.CloudJson, "validation/test-data.json", spaceId: host.GlobalState.SpaceId);

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "CloudJson");
            view.Text([Text.Caption, "font-mono mb-2"], $"URI: {uri}");

            view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
            {
                view.Button([Button.PrimaryMd], label: "Generate & Upload", onClick: async () =>
                {
                    var data = new ValidationAssetData
                    {
                        Name = $"Test-{Random.Shared.Next(1000, 9999)}",
                        Value = Random.Shared.Next(1, 100),
                        Timestamp = DateTime.UtcNow.ToString("O")
                    };

                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    await Asset.Instance.SetTextAsync(uri, json);
                    var metadata = await Asset.Instance.GetMetadataAsync(uri);
                    _assetCloudJsonStatus.Value = "Uploaded";
                    _assetCloudJsonMetadata.Value = $"Size: {metadata.Size}, LastModified: {metadata.LastModified}";
                    _assetCloudJsonUploaded.Value = json;
                });

                view.Button([Button.PrimaryMd], label: "Download", onClick: async () =>
                {
                    var json = await Asset.Instance.GetTextAsync(uri);
                    _assetCloudJsonDownloaded.Value = json;
                });

                view.Button([Button.DangerMd], label: "Delete", onClick: async () =>
                {
                    await Asset.Instance.DeleteAsync(uri);
                    _assetCloudJsonStatus.Value = "Not uploaded";
                    _assetCloudJsonMetadata.Value = "";
                    _assetCloudJsonUploaded.Value = "";
                    _assetCloudJsonDownloaded.Value = "";
                });
            });

            view.Text([Text.Body, "mt-2"], _assetCloudJsonStatus.Value);

            if (!string.IsNullOrEmpty(_assetCloudJsonMetadata.Value))
            {
                view.Text([Text.Caption, "mt-1"], _assetCloudJsonMetadata.Value);
            }

            if (!string.IsNullOrEmpty(_assetCloudJsonUploaded.Value) || !string.IsNullOrEmpty(_assetCloudJsonDownloaded.Value))
            {
                view.Row([Layout.Row.Lg, "flex-wrap mt-2"], content: view =>
                {
                    if (!string.IsNullOrEmpty(_assetCloudJsonUploaded.Value))
                    {
                        view.Column(["flex-1 min-w-[200px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Uploaded:");
                            view.Markdown(content: "```json\n" + _assetCloudJsonUploaded.Value + "\n```");
                        });
                    }

                    if (!string.IsNullOrEmpty(_assetCloudJsonDownloaded.Value))
                    {
                        view.Column(["flex-1 min-w-[200px]"], content: view =>
                        {
                            view.Text([Text.Caption, "mb-1"], "Downloaded:");
                            view.Markdown(content: "```json\n" + _assetCloudJsonDownloaded.Value + "\n```");
                        });
                    }
                });
            }
        });
    }
}

public class ValidationAssetData
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public string Timestamp { get; set; } = "";
}
