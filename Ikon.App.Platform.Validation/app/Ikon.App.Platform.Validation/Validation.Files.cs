public partial class Validation
{
    private void RenderFilesSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Basic FileUpload
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Basic File Upload");
                view.Text([Text.Caption, "mb-4"], "Click or drag & drop files. Supports paste.");

                view.FileUpload(
                    [FileUpload.Zone.Base],
                    accept: ["image/*", ".pdf", ".txt"],
                    multiple: false,
                    onUploadComplete: async args =>
                    {
                        _basicUploadStatus.Value = $"Uploaded: {args.FileName}";
                    },
                    onUploadError: async args =>
                    {
                        _basicUploadStatus.Value = $"Error: {args.ErrorMessage}";
                    },
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([Media.PlaceholderIcon], name: "upload");
                            view.Text([Text.Body], "Click to upload or drag & drop");
                            view.Text([Text.Caption], "Images, PDFs, or text files");
                        });
                    });

                if (!string.IsNullOrEmpty(_basicUploadStatus.Value))
                {
                    view.Text([Text.Caption, "mt-2 font-mono"], _basicUploadStatus.Value);
                }
            });

            // Multiple file upload
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Multiple File Upload");
                view.Text([Text.Caption, "mb-4"], "Upload multiple files at once.");

                view.FileUpload(
                    [FileUpload.Zone.Images],
                    accept: ["image/*"],
                    multiple: true,
                    maxFileSize: 5 * 1024 * 1024,
                    onUploadComplete: async args =>
                    {
                        _multiUploadStatus.Value = $"Uploaded: {args.FileName}";
                    },
                    onUploadError: async args =>
                    {
                        _multiUploadStatus.Value = $"Error: {args.ErrorMessage}";
                    },
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([FileUpload.Icon.Success], name: "images");
                            view.Text([Text.Body], "Drop images here");
                            view.Text([Text.Caption], "Max 5MB per file");
                        });
                    });

                if (!string.IsNullOrEmpty(_multiUploadStatus.Value))
                {
                    view.Text([Text.Caption, "mt-2 font-mono"], _multiUploadStatus.Value);
                }
            });

            // FileUploadZone wrapping a TextArea
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "File Upload Zone (Wrapping TextArea)");
                view.Text([Text.Caption, "mb-4"], "Drop or paste files onto the text area below.");

                view.FileUploadZone(
                    activeStyle: [FileUpload.Zone.ActiveRing],
                    accept: ["image/*", ".pdf"],
                    onUploadComplete: async args =>
                    {
                        _zoneUploadStatus.Value = $"Uploaded attachment: {args.FileName}";
                    },
                    onUploadError: async args =>
                    {
                        _zoneUploadStatus.Value = $"Error: {args.ErrorMessage}";
                    },
                    content: view =>
                    {
                        view.TextArea(
                            [Input.Default, "min-h-[120px]"],
                            value: _textAreaValue.Value,
                            placeholder: "Type a message or drop/paste a file here...",
                            onValueChange: async value => _textAreaValue.Value = value);
                    });

                if (!string.IsNullOrEmpty(_zoneUploadStatus.Value))
                {
                    view.Text([Text.Caption, "mt-2 font-mono"], _zoneUploadStatus.Value);
                }
            });

            // Disabled file upload
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Disabled File Upload");
                view.Text([Text.Caption, "mb-4"], "File upload is disabled.");

                view.FileUpload(
                    [FileUpload.Zone.Base, FileUpload.Zone.Disabled],
                    disabled: true,
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([FileUpload.Icon.Disabled], name: "upload-off");
                            view.Text([Text.Body, "text-muted-foreground"], "Upload disabled");
                        });
                    });
            });

            // File type restrictions
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "File Type Restrictions");
                view.Row([Layout.Row.Lg, "flex-wrap"], content: view =>
                {
                    view.Box(["flex-1 min-w-[200px]"], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "Images Only");

                        view.FileUpload(
                            [FileUpload.Zone.Images],
                            accept: ["image/*"],
                            onUploadComplete: async args =>
                            {
                                _imagesUploadStatus.Value = $"Uploaded image: {args.FileName}";
                            },
                            onUploadError: async args =>
                            {
                                _imagesUploadStatus.Value = $"Error: {args.ErrorMessage}";
                            },
                            content: view =>
                            {
                                view.Icon([FileUpload.Icon.Success], name: "image");
                                view.Text([Text.Caption], ".jpg, .png, .gif");
                            });

                        if (!string.IsNullOrEmpty(_imagesUploadStatus.Value))
                        {
                            view.Text([Text.Caption, "mt-2 font-mono"], _imagesUploadStatus.Value);
                        }
                    });

                    view.Box(["flex-1 min-w-[200px]"], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "PDFs Only");

                        view.FileUpload(
                            [FileUpload.Zone.Documents],
                            accept: [".pdf", "application/pdf"],
                            onUploadComplete: async args =>
                            {
                                _pdfsUploadStatus.Value = $"Uploaded PDF: {args.FileName}";
                            },
                            onUploadError: async args =>
                            {
                                _pdfsUploadStatus.Value = $"Error: {args.ErrorMessage}";
                            },
                            content: view =>
                            {
                                view.Icon([FileUpload.Icon.Error], name: "file-text");
                                view.Text([Text.Caption], ".pdf");
                            });

                        if (!string.IsNullOrEmpty(_pdfsUploadStatus.Value))
                        {
                            view.Text([Text.Caption, "mt-2 font-mono"], _pdfsUploadStatus.Value);
                        }
                    });

                    view.Box(["flex-1 min-w-[200px]"], content: view =>
                    {
                        view.Text([Text.Caption, "mb-2"], "Code Files");

                        view.FileUpload(
                            [FileUpload.Zone.Code],
                            accept: [".js", ".ts", ".cs", ".py", ".json"],
                            onUploadComplete: async args =>
                            {
                                _codeUploadStatus.Value = $"Uploaded code file: {args.FileName}";
                            },
                            onUploadError: async args =>
                            {
                                _codeUploadStatus.Value = $"Error: {args.ErrorMessage}";
                            },
                            content: view =>
                            {
                                view.Icon([FileUpload.Icon.Brand], name: "code");
                                view.Text([Text.Caption], ".js, .ts, .cs, .py");
                            });

                        if (!string.IsNullOrEmpty(_codeUploadStatus.Value))
                        {
                            view.Text([Text.Caption, "mt-2 font-mono"], _codeUploadStatus.Value);
                        }
                    });
                });
            });

            // Advanced File Upload with all callbacks
            RenderAdvancedFileUploadSection(view);
        });
    }

    private void RenderAdvancedFileUploadSection(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Advanced File Upload (All Callbacks)");
            view.Text([Text.Caption, "mb-4"], "Tests all file upload callbacks with status tracking.");

            // Controls
            view.Row([Layout.Row.Md, "mb-4 items-end"], content: view =>
            {
                view.Box(["flex-1"], content: view =>
                {
                    view.Text([FormField.Label, "mb-1"], "Upload Mode");
                    view.Select(
                        value: _advUploadMode.Value,
                        options:
                        [
                            new SelectOption("local", "Local Temp File Path"),
                            new SelectOption("asset", "Asset URI (CloudFilePublic)")
                        ],
                        onValueChange: async v =>
                        {
                            _advUploadMode.Value = v;
                            ResetAdvancedUploadStatus();
                        });
                });

                view.Row(["items-center gap-2"], content: view =>
                {
                    view.Switch([Switch.Root], @checked: _advUploadRejectAll.Value,
                        onCheckedChange: async v => _advUploadRejectAll.Value = v,
                        content: view => view.SwitchThumb([Switch.Thumb]));
                    view.Label([Label.Base], content: v => v.Text(text: "Reject all uploads"));
                });
            });

            // File upload
            view.FileUpload(
                [FileUpload.Zone.Base],
                multiple: false,
                onUploadPreStart: async args =>
                {
                    ResetAdvancedUploadStatus();
                    _advUploadInitStatus.Value = $"UploadId={args.UploadId}, File={args.FileName}, Type={args.MimeType}, Size={args.Size}";

                    if (_advUploadMode.Value == "asset")
                    {
                        var assetUri = new AssetUri(
                            AssetClass.CloudFilePublic,
                            $"validation-uploads/{args.FileName}",
                            spaceId: app.GlobalState.SpaceId);
                        return new FileUploadPreStartResult { AssetUri = assetUri.ToString() };
                    }

                    return true;
                },
                onUploadStart: async args =>
                {
                    var accepted = !_advUploadRejectAll.Value;
                    _advUploadStartStatus.Value = $"Hash={args.Hash}, Accepted={accepted}";
                    return accepted;
                },
                onUploadProgress: async args =>
                {
                    _advUploadProgress.Value = args.ProgressPercentage;
                    _advUploadProgressStatus.Value = $"{args.ProgressPercentage:F1}% ({args.BytesUploaded}/{args.Size} bytes)";
                },
                onUploadComplete: async args =>
                {
                    _advUploadProgress.Value = 100;
                    var location = args.AssetUri != null ? $"AssetUri={args.AssetUri}" : $"LocalPath={args.LocalTempFilePath}";
                    _advUploadCompleteStatus.Value = $"{args.FileName} ({args.Size} bytes) — {location}";

                    if (args.AssetUri != null)
                    {
                        try
                        {
                            var metadata = await Asset.Instance.GetMetadataAsync(new AssetUri(args.AssetUri));
                            _advUploadAssetUrl.Value = metadata.Url;
                        }
                        catch (Exception ex)
                        {
                            _advUploadAssetUrl.Value = null;
                            Log.Instance.Warning($"Failed to get asset metadata: {ex.Message}");
                        }
                    }
                },
                onUploadError: async args =>
                {
                    _advUploadErrorStatus.Value = $"{args.FileName}: {args.ErrorMessage}";
                },
                content: view =>
                {
                    view.Column([Layout.Column.Center], content: view =>
                    {
                        view.Icon([Media.PlaceholderIcon], name: "upload");
                        view.Text([Text.Body], "Upload a file to test all callbacks");
                        view.Text([Text.Caption], _advUploadMode.Value == "asset" ? "Asset URI mode — file stored in cloud" : "Local mode — file stored as temp file");
                    });
                });

            if (_advUploadProgress.Value > 0 && _advUploadProgress.Value < 100)
            {
                view.Progress(value: _advUploadProgress.Value, max: 100, rootStyle: [Progress.Root, "mt-2"]);
            }

            // Callback status display
            view.Box(["mt-4 space-y-1"], content: view =>
            {
                view.Text([Text.BodyStrong, "mb-2"], "Callback Status");
                RenderCallbackStatusLine(view, "onUploadPreStart", _advUploadInitStatus.Value);
                RenderCallbackStatusLine(view, "onUploadStart", _advUploadStartStatus.Value);
                RenderCallbackStatusLine(view, "onUploadProgress", _advUploadProgressStatus.Value);
                RenderCallbackStatusLine(view, "onUploadComplete", _advUploadCompleteStatus.Value);
                RenderCallbackStatusLine(view, "onUploadError", _advUploadErrorStatus.Value);
            });

            if (_advUploadAssetUrl.Value != null)
            {
                view.Button([Button.PrimaryMd, "mt-4"], label: "Open Asset URL",
                    href: _advUploadAssetUrl.Value,
                    target: "_blank",
                    rel: "noopener noreferrer");
            }
        });
    }

    private static void RenderCallbackStatusLine(UIView view, string label, string status)
    {
        view.Row(["items-start gap-2"], content: view =>
        {
            view.Text([Text.Caption, "font-mono min-w-[140px] shrink-0"], label + ":");
            view.Text([Text.Caption, "font-mono break-all"],
                string.IsNullOrEmpty(status) ? "—" : status);
        });
    }

    private void ResetAdvancedUploadStatus()
    {
        _advUploadInitStatus.Value = "";
        _advUploadStartStatus.Value = "";
        _advUploadProgressStatus.Value = "";
        _advUploadCompleteStatus.Value = "";
        _advUploadErrorStatus.Value = "";
        _advUploadProgress.Value = 0;
        _advUploadAssetUrl.Value = null;
    }
}
