using System.Text;
using Ikon.Connectors;
using Ikon.Connectors.Google;

public partial class Validation
{
    private const int GdPageSize = 100;

    // A file opened for preview is held in memory and handed to the browser as a data URL, so a
    // larger one is offered only through Drive's own link.
    private const long GdPreviewLimitBytes = 10 * 1024 * 1024;
    private const int GdTextPreviewChars = 20_000;
    private const string GdNativePrefix = "application/vnd.google-apps.";

    // What a native file previews as: its text where Drive exports one, otherwise nothing here.
    private static readonly Dictionary<string, string> GdPreviewExports = new(StringComparer.Ordinal)
    {
        [$"{GdNativePrefix}document"] = "text/markdown",
        [$"{GdNativePrefix}spreadsheet"] = "text/csv",
        [$"{GdNativePrefix}presentation"] = "text/plain",
        [$"{GdNativePrefix}drawing"] = "image/svg+xml",
    };

    // What an upload becomes when "Convert to Google format" is on, by the uploaded file's extension.
    private static readonly Dictionary<string, string> GdConversions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".docx"] = $"{GdNativePrefix}document",
        [".doc"] = $"{GdNativePrefix}document",
        [".txt"] = $"{GdNativePrefix}document",
        [".md"] = $"{GdNativePrefix}document",
        [".rtf"] = $"{GdNativePrefix}document",
        [".odt"] = $"{GdNativePrefix}document",
        [".xlsx"] = $"{GdNativePrefix}spreadsheet",
        [".xls"] = $"{GdNativePrefix}spreadsheet",
        [".csv"] = $"{GdNativePrefix}spreadsheet",
        [".ods"] = $"{GdNativePrefix}spreadsheet",
        [".pptx"] = $"{GdNativePrefix}presentation",
        [".ppt"] = $"{GdNativePrefix}presentation",
        [".odp"] = $"{GdNativePrefix}presentation",
    };

    private static readonly DriveRole[] GdShareRoles = [DriveRole.Reader, DriveRole.Commenter, DriveRole.Writer];

    private static readonly string GdRecentQuery = DriveQuery.And(DriveQuery.NotTrashed, DriveQuery.IsNotFolder);

    private readonly Reactive<string> _gdListing = new("folder");
    private readonly Reactive<string> _gdListingTitle = new("");
    private readonly Reactive<string> _gdSort = new("drive");
    private readonly Reactive<SharedDrive?> _gdSharedDrive = new(null);
    private readonly ReactiveList<SharedDrive> _gdSharedDrives = new();
    private readonly ReactiveList<DriveFile> _gdFolderPath = new();
    private readonly ReactiveList<DriveFile> _gdItems = new();
    private readonly Reactive<string?> _gdNextCursor = new(null);
    private readonly Reactive<string> _gdSearchQuery = new("");
    private readonly Reactive<string> _gdNewFolderName = new("");
    private readonly Reactive<bool> _gdConvertUploads = new(false);
    private readonly Reactive<GdPreview?> _gdPreview = new(null);
    private readonly ReactiveList<DriveFile> _gdBin = new();
    private readonly Reactive<bool> _gdBinShown = new(false);
    private readonly Reactive<DriveFile?> _gdPermanentDeleteTarget = new(null);

    private readonly Reactive<DriveFile?> _gdActiveItem = new(null);
    private readonly Reactive<string> _gdPanelView = new("");
    private readonly Reactive<string> _gdRenameInput = new("");
    private readonly Reactive<string> _gdMoveTarget = new("");
    private readonly ReactiveList<string> _gdExportTargets = new();
    private readonly ReactiveList<DrivePermission> _gdPermissions = new();
    private readonly ReactiveList<DriveAccessProposal> _gdProposals = new();
    private readonly Reactive<string> _gdShareEmail = new("");
    private readonly Reactive<string> _gdShareRole = new(nameof(DriveRole.Reader));
    private readonly ReactiveList<DriveComment> _gdComments = new();
    private readonly Reactive<string> _gdCommentInput = new("");
    private readonly Reactive<string> _gdReplyInput = new("");
    private readonly ReactiveList<DriveRevision> _gdRevisions = new();

    // Only what this tab granted is offered for removal, so a test on a real file cannot withdraw
    // someone's actual access.
    private readonly HashSet<string> _gdPermissionsCreatedHere = [];

    private IReadOnlyDictionary<string, IReadOnlyList<string>>? _gdExportFormats;

    private sealed record GdPreview(DriveFile File, string? Text, byte[]? Bytes, string MimeType, string FileName, string Note);

    private void RenderGoogleDriveFilesCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-3"], "Files");

            view.Row([Layout.Row.Sm, "items-center flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.GhostSm], text: _gdSharedDrive.Value?.Name ?? "My Drive", props: TestId("gd-crumb-root"),
                    onClick: async () => await GdRunAsync("open the root", () => GdOpenFolderAsync(-1)));

                for (var i = 0; i < _gdFolderPath.Count; i++)
                {
                    var depth = i;
                    view.Text([Text.Caption], "/");
                    view.Button([Button.GhostSm], key: _gdFolderPath[i].Id, text: _gdFolderPath[i].Name,
                        onClick: async () => await GdRunAsync("open the folder", () => GdOpenFolderAsync(depth)));
                }

                view.Box(["flex-1"], content: _ => { });
                view.Select(value: _gdSort.Value, ariaLabel: "Order", props: TestId("gd-sort"),
                    options: [new SelectOption("drive", "Drive's order"), new SelectOption("name", "By name"), new SelectOption("modified", "Newest changed"), new SelectOption("created", "Newest created")],
                    onValueChange: async value =>
                    {
                        _gdSort.Value = value ?? "drive";
                        await GdRunAsync("reorder the folder", () => GdOpenFolderAsync(_gdFolderPath.Count - 1));
                    });
            });

            view.Row([Layout.Row.Sm, "flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.OutlineSm], text: "Recent", disabled: _gdBusy.Value, props: TestId("gd-recent"),
                    onClick: async () => await GdRunAsync("list recent files", GdListRecentAsync));
                view.Button([Button.OutlineSm], text: "Shared drives", disabled: _gdBusy.Value, props: TestId("gd-shared-drives"),
                    onClick: async () => await GdRunAsync("list shared drives", GdListSharedDrivesAsync));
                view.Button([Button.OutlineSm], text: "Bin", disabled: _gdBusy.Value, props: TestId("gd-bin"),
                    onClick: async () => await GdRunAsync("list the bin", GdListBinAsync));
            });

            if (_gdSharedDrives.Count > 0)
            {
                view.Row([Layout.Row.Sm, "flex-wrap mb-3"], content: view =>
                {
                    view.Button([_gdSharedDrive.Value == null ? Button.PrimarySm : Button.OutlineSm], text: "My Drive",
                        onClick: async () => await GdRunAsync("open My Drive", () => GdOpenSharedDriveAsync(null)));

                    foreach (var sharedDrive in _gdSharedDrives)
                    {
                        view.Button([_gdSharedDrive.Value?.Id == sharedDrive.Id ? Button.PrimarySm : Button.OutlineSm], key: sharedDrive.Id, text: sharedDrive.Name,
                            props: TestId("gd-shared-drive"),
                            onClick: async () => await GdRunAsync($"open {sharedDrive.Name}", () => GdOpenSharedDriveAsync(sharedDrive)));
                    }
                });
            }

            view.Row([Layout.Row.Md, "items-end flex-wrap mb-4"], content: view =>
            {
                view.TextField([Input.Default, "flex-1 min-w-48"], value: _gdSearchQuery.Value, placeholder: "Search names and content", props: TestId("gd-search-input"),
                    onValueChange: async v => _gdSearchQuery.Value = v ?? "");
                view.Button([Button.OutlineMd], text: "Search", disabled: _gdBusy.Value || _gdSearchQuery.Value.Trim().Length == 0, props: TestId("gd-search"),
                    onClick: async () => await GdRunAsync("search", GdSearchAsync));
                view.TextField([Input.Default, "w-48"], value: _gdNewFolderName.Value, placeholder: "New folder name", props: TestId("gd-folder-input"),
                    onValueChange: async v => _gdNewFolderName.Value = v ?? "");
                view.Button([Button.OutlineMd], text: "Create folder", disabled: _gdBusy.Value || _gdNewFolderName.Value.Trim().Length == 0 || _gdListing.Value != "folder",
                    props: TestId("gd-folder-create"),
                    onClick: async () => await GdRunAsync("create the folder", GdCreateFolderAsync));
            });

            if (_gdListing.Value != "folder")
            {
                view.Row([Layout.Row.Sm, "items-center mb-2"], content: view =>
                {
                    view.Text([Text.Caption], _gdListingTitle.Value, props: TestId("gd-listing-title"));
                    view.Button([Button.GhostSm], text: "Back to the folder",
                        onClick: async () => await GdRunAsync("open the folder", () => GdOpenFolderAsync(_gdFolderPath.Count - 1)));
                });
            }

            if (_gdItems.Count == 0)
            {
                view.Text([Text.Caption, "py-4"], _gdListing.Value == "folder" ? "This folder is empty" : "Nothing found", props: TestId("gd-files-empty"));
            }

            foreach (var file in _gdItems)
            {
                view.Row([Layout.Row.Md, "items-center py-2 border-b border-secondary"], key: file.Id, content: view =>
                {
                    view.Icon([Icon.Default], name: file.IsFolder ? "folder" : file.IsGoogleNative ? "file-text" : "file");
                    view.Column(["flex-1 min-w-0"], content: view =>
                    {
                        view.Text([Text.Body, "truncate"], $"{(file.Starred ? "★ " : "")}{file.Name}", props: TestId("gd-file"));
                        view.Text([Text.Caption, "truncate"], GdDescribe(file));
                    });

                    view.Button([Button.OutlineSm], text: file.IsFolder ? "Open" : "View", disabled: _gdBusy.Value, props: TestId(file.IsFolder ? "gd-file-open" : "gd-file-view"),
                        onClick: async () => await GdRunAsync(file.IsFolder ? "open the folder" : $"read {file.Name}",
                            () => file.IsFolder ? GdEnterFolderAsync(file) : GdPreviewAsync(file)));
                    view.Button([_gdActiveItem.Value?.Id == file.Id ? Button.SecondarySm : Button.GhostSm], text: "More", disabled: _gdBusy.Value,
                        props: new Dictionary<string, object> { ["aria-label"] = $"More actions for {file.Name}", ["data-testid"] = "gd-file-more" },
                        onClick: async () => GdToggleActiveItem(file));
                    view.Button([Button.GhostErrorSm], text: "Bin", disabled: _gdBusy.Value,
                        props: new Dictionary<string, object> { ["aria-label"] = $"Move {file.Name} to the bin", ["data-testid"] = "gd-file-bin", ["title"] = "Drive keeps it in the bin for 30 days" },
                        onClick: async () => await GdRunAsync($"move {file.Name} to the bin", () => GdBinAsync(file)));
                });

                if (_gdActiveItem.Value?.Id == file.Id)
                {
                    RenderGoogleDriveItemPanel(view, file);
                }
            }

            if (_gdNextCursor.Value != null)
            {
                view.Button([Button.OutlineSm, "mt-3"], text: "Load more", disabled: _gdBusy.Value, props: TestId("gd-load-more"),
                    onClick: async () => await GdRunAsync("load the next page", GdLoadMoreAsync));
            }

            if (_gdListing.Value == "folder")
            {
                view.Switch([Switch.Default, "mt-4"], value: _gdConvertUploads.Value, label: "Convert Office, text and CSV uploads to Google format",
                    onValueChange: async value => _gdConvertUploads.Value = value);

                view.FileUpload(
                    [FileUpload.Zone.Base, "mt-2"],
                    multiple: false,
                    onUploadComplete: async args => await GdRunAsync($"upload {args.FileName}", () => GdUploadAsync(args)),
                    onUploadError: async args => _gdStatus.Value = $"FAIL upload: {args.ErrorMessage}",
                    content: view =>
                    {
                        view.Column([Layout.Column.Center], content: view =>
                        {
                            view.Icon([Media.PlaceholderIcon], name: "upload");
                            view.Text([Text.Body], "Upload into this folder");
                            view.Text([Text.Caption], "Up to 5 MiB in one request, larger files through a resumable session");
                        });
                    });
            }
        });
    }

    private void RenderGoogleDriveItemPanel(UIView view, DriveFile file)
    {
        view.Column(["pl-6 py-3 border-b border-secondary bg-secondary/40 rounded-md"], content: view =>
        {
            view.Row([Layout.Row.Sm, "items-center flex-wrap mb-2"], content: view =>
            {
                view.TextField([Input.Default, "w-56"], value: _gdRenameInput.Value, placeholder: file.Name, props: TestId("gd-rename-input"),
                    onValueChange: async v => _gdRenameInput.Value = v ?? "");
                view.Button([Button.OutlineSm], text: "Rename", disabled: _gdBusy.Value || _gdRenameInput.Value.Trim().Length == 0, props: TestId("gd-rename"),
                    onClick: async () => await GdRunAsync($"rename {file.Name}", () => GdRenameAsync(file)));
                view.Button([Button.OutlineSm], text: file.Starred ? "Unstar" : "Star", disabled: _gdBusy.Value, props: TestId("gd-star"),
                    onClick: async () => await GdRunAsync($"star {file.Name}", () => GdStarAsync(file)));

                if (!file.IsFolder)
                {
                    view.Button([Button.OutlineSm], text: "Copy", disabled: _gdBusy.Value, props: TestId("gd-copy"),
                        onClick: async () => await GdRunAsync($"copy {file.Name}", () => GdCopyAsync(file)));
                }

                view.Button([Button.OutlineSm], text: "Shortcut", disabled: _gdBusy.Value, props: TestId("gd-shortcut"),
                    onClick: async () => await GdRunAsync($"create a shortcut to {file.Name}", () => GdShortcutAsync(file)));
            });

            var folders = _gdItems.Where(f => f.IsFolder && f.Id != file.Id).Select(f => new SelectOption(f.Id, f.Name)).ToList();

            if (_gdFolderPath.Count > 0)
            {
                folders.Insert(0, new SelectOption(_gdFolderPath.Count > 1 ? _gdFolderPath[^2].Id : _gdSharedDrive.Value?.Id ?? "root", "↑ the folder above"));
            }

            if (folders.Count > 0)
            {
                view.Row([Layout.Row.Sm, "items-center flex-wrap mb-2"], content: view =>
                {
                    view.Select(value: _gdMoveTarget.Value.Length > 0 ? _gdMoveTarget.Value : folders[0].Value, options: folders, ariaLabel: "Move to folder",
                        props: TestId("gd-move-target"), onValueChange: async v => _gdMoveTarget.Value = v ?? "");
                    view.Button([Button.OutlineSm], text: "Move", disabled: _gdBusy.Value, props: TestId("gd-move"),
                        onClick: async () => await GdRunAsync($"move {file.Name}", () => GdMoveAsync(file, _gdMoveTarget.Value.Length > 0 ? _gdMoveTarget.Value : folders[0].Value)));
                });
            }

            view.Row([Layout.Row.Sm, "flex-wrap mb-2"], content: view =>
            {
                view.Button([Button.OutlineSm], text: "Sharing", disabled: _gdBusy.Value, props: TestId("gd-sharing"),
                    onClick: async () => await GdRunAsync($"list who can open {file.Name}", () => GdLoadSharingAsync(file)));

                if (!file.IsFolder)
                {
                    view.Button([Button.OutlineSm], text: "Comments", disabled: _gdBusy.Value, props: TestId("gd-comments"),
                        onClick: async () => await GdRunAsync($"list comments on {file.Name}", () => GdLoadCommentsAsync(file)));
                    view.Button([Button.OutlineSm], text: "Revisions", disabled: _gdBusy.Value, props: TestId("gd-revisions"),
                        onClick: async () => await GdRunAsync($"list revisions of {file.Name}", () => GdLoadRevisionsAsync(file)));
                }

                if (file.IsGoogleNative && !file.IsFolder)
                {
                    view.Button([Button.OutlineSm], text: "Export", disabled: _gdBusy.Value, props: TestId("gd-export-formats"),
                        onClick: async () => await GdRunAsync($"list export formats of {file.Name}", () => GdLoadExportFormatsAsync(file)));
                }

                if (file.WebViewLink is { } link)
                {
                    view.Link([Button.GhostSm], href: link, text: "Open in Drive", target: "_blank");
                }

                view.Button([Button.GhostErrorSm], text: "Delete permanently", disabled: _gdBusy.Value, props: TestId("gd-permanent-delete"),
                    onClick: async () => _gdPermanentDeleteTarget.Value = file);
            });

            switch (_gdPanelView.Value)
            {
                case "sharing":
                    RenderGoogleDriveSharing(view, file);
                    break;

                case "comments":
                    RenderGoogleDriveComments(view, file);
                    break;

                case "revisions":
                    foreach (var revision in _gdRevisions)
                    {
                        view.Row([Layout.Row.Sm, "items-center"], key: revision.Id, content: view =>
                        {
                            view.Text([Text.Caption, "flex-1"],
                                $"{revision.Id} · {revision.ModifiedTime:yyyy-MM-dd HH:mm} · {revision.LastModifiedBy ?? "?"} · {SpSize(revision.Size)}{(revision.KeepForever ? " · kept forever" : "")}",
                                props: TestId("gd-revision"));

                            if (!file.IsGoogleNative)
                            {
                                view.Button([Button.GhostSm], text: "Download", disabled: _gdBusy.Value,
                                    onClick: async () => await GdRunAsync($"download revision {revision.Id}", () => GdPreviewRevisionAsync(file, revision)));
                                view.Button([Button.GhostSm], text: revision.KeepForever ? "Let Drive prune" : "Keep forever", disabled: _gdBusy.Value,
                                    onClick: async () => await GdRunAsync($"change revision {revision.Id}", () => GdKeepRevisionAsync(file, revision)));
                            }
                        });
                    }

                    break;

                case "export":
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        foreach (var target in _gdExportTargets)
                        {
                            view.Button([Button.OutlineSm], key: target, text: target, disabled: _gdBusy.Value, props: TestId("gd-export"),
                                onClick: async () => await GdRunAsync($"export {file.Name} as {target}", () => GdExportAsync(file, target)));
                        }
                    });
                    break;
            }
        });

        view.AlertDialog(
            open: _gdPermanentDeleteTarget.Value?.Id == file.Id,
            onOpenChange: async open => _gdPermanentDeleteTarget.Value = open ? file : null,
            title: $"Delete {file.Name} for good?",
            description: file.IsFolder ? "This skips the bin, takes everything in the folder with it, and cannot be undone." : "This skips the bin and cannot be undone.",
            cancelLabel: "Keep it",
            actionLabel: "Delete permanently",
            onAction: async () =>
            {
                _gdPermanentDeleteTarget.Value = null;
                await GdRunAsync($"permanently delete {file.Name}", () => GdPermanentDeleteAsync(file));
            });
    }

    private void RenderGoogleDriveSharing(UIView view, DriveFile file)
    {
        foreach (var permission in _gdPermissions)
        {
            view.Row([Layout.Row.Sm, "items-center"], key: permission.Id, content: view =>
            {
                var who = permission.Type switch
                {
                    DrivePermissionType.Anyone => "anyone with the link",
                    DrivePermissionType.Domain => $"everyone at {permission.Domain}",
                    _ => permission.DisplayName is { } name ? $"{name} <{permission.EmailAddress}>" : permission.EmailAddress ?? permission.Id,
                };

                view.Text([Text.Caption, "flex-1"], $"{who} · {permission.Role}{(permission.Inherited ? " · inherited" : "")}{(permission.ExpiresAt is { } at ? $" · until {at:yyyy-MM-dd}" : "")}",
                    props: TestId("gd-permission"));

                if (_gdPermissionsCreatedHere.Contains(permission.Id))
                {
                    view.Button([Button.GhostErrorSm], text: "Remove", disabled: _gdBusy.Value, props: TestId("gd-permission-remove"),
                        onClick: async () => await GdRunAsync("remove the permission", () => GdRemovePermissionAsync(file, permission)));
                }
            });
        }

        foreach (var proposal in _gdProposals)
        {
            view.Text([Text.Caption], $"Access requested by {proposal.RequesterEmail}: {string.Join(", ", proposal.RequestedRoles)}{(proposal.Message is { } message ? $" — “{message}”" : "")}",
                key: proposal.Id, props: TestId("gd-access-proposal"));
        }

        view.Row([Layout.Row.Sm, "items-center flex-wrap mt-2"], content: view =>
        {
            view.TextField([Input.Default, "w-64"], value: _gdShareEmail.Value, placeholder: "person@example.com", props: TestId("gd-share-email"),
                onValueChange: async v => _gdShareEmail.Value = v ?? "");
            view.Select(value: _gdShareRole.Value, ariaLabel: "Role", props: TestId("gd-share-role"),
                options: GdShareRoles.Select(r => new SelectOption(r.ToString(), r.ToString())).ToList(),
                onValueChange: async v => _gdShareRole.Value = v ?? nameof(DriveRole.Reader));
            view.Button([Button.OutlineSm], text: "Share", disabled: _gdBusy.Value || !_gdShareEmail.Value.Contains('@'), props: TestId("gd-share"),
                onClick: async () => await GdRunAsync($"share {file.Name}", () => GdShareAsync(file, NewDrivePermission.User(_gdShareEmail.Value.Trim(), Enum.Parse<DriveRole>(_gdShareRole.Value)) with
                {
                    SendNotificationEmail = false,
                })));
            view.Button([Button.OutlineSm], text: "Anyone with the link can view", disabled: _gdBusy.Value, props: TestId("gd-share-link"),
                onClick: async () => await GdRunAsync($"open {file.Name} to anyone with the link", () => GdShareAsync(file, NewDrivePermission.Anyone(DriveRole.Reader))));
        });
    }

    private void RenderGoogleDriveComments(UIView view, DriveFile file)
    {
        foreach (var comment in _gdComments)
        {
            view.Column(["py-1 border-b border-secondary"], key: comment.Id, content: view =>
            {
                view.Row([Layout.Row.Sm, "items-center"], content: view =>
                {
                    view.Text([Text.Caption, "flex-1"], $"{comment.AuthorName}: {comment.Content}{(comment.Resolved ? " · resolved" : "")}{(comment.QuotedContent is { } quoted ? $" · on “{quoted}”" : "")}",
                        props: TestId("gd-comment"));
                    view.Button([Button.GhostSm], text: "Reply", disabled: _gdBusy.Value || _gdReplyInput.Value.Trim().Length == 0,
                        onClick: async () => await GdRunAsync("reply", () => GdReplyAsync(file, comment)));
                    view.Button([Button.GhostSm], text: comment.Resolved ? "Reopen" : "Resolve", disabled: _gdBusy.Value, props: TestId("gd-comment-resolve"),
                        onClick: async () => await GdRunAsync(comment.Resolved ? "reopen the comment" : "resolve the comment", () => GdResolveAsync(file, comment)));
                    view.Button([Button.GhostErrorSm], text: "Delete", disabled: _gdBusy.Value,
                        onClick: async () => await GdRunAsync("delete the comment", () => GdDeleteCommentAsync(file, comment)));
                });

                foreach (var reply in comment.Replies)
                {
                    view.Text([Text.Caption, "pl-4"], $"↳ {reply.AuthorName}: {(reply.Action is { } action ? $"[{action}] " : "")}{reply.Content}", key: reply.Id);
                }
            });
        }

        view.Row([Layout.Row.Sm, "items-center flex-wrap mt-2"], content: view =>
        {
            view.TextField([Input.Default, "flex-1 min-w-48"], value: _gdCommentInput.Value, placeholder: "New comment", props: TestId("gd-comment-input"),
                onValueChange: async v => _gdCommentInput.Value = v ?? "");
            view.Button([Button.OutlineSm], text: "Comment", disabled: _gdBusy.Value || _gdCommentInput.Value.Trim().Length == 0, props: TestId("gd-comment-add"),
                onClick: async () => await GdRunAsync("comment", () => GdCommentAsync(file)));
            view.TextField([Input.Default, "flex-1 min-w-48"], value: _gdReplyInput.Value, placeholder: "Reply text, then press Reply on a comment", props: TestId("gd-reply-input"),
                onValueChange: async v => _gdReplyInput.Value = v ?? "");
        });
    }

    private void RenderGoogleDrivePreviewCard(UIView view)
    {
        if (_gdPreview.Value is not { } preview)
        {
            return;
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Row([Layout.Row.Md, "items-center mb-3"], content: view =>
            {
                view.Text([Text.H3, "flex-1"], preview.FileName, props: TestId("gd-preview-name"));

                if (preview.Bytes != null)
                {
                    view.ActionButton([Button.OutlineSm],
                        action: ActionKind.DownloadFile,
                        options: new DownloadFileActionOptions { Data = preview.Bytes, MimeType = preview.MimeType, Filename = preview.FileName },
                        content: v => v.Text(text: "Download"));
                }

                if (preview.File.WebViewLink is { } link)
                {
                    view.Link([Button.GhostSm], href: link, text: "Open in Drive", target: "_blank");
                }

                view.Button([Button.GhostSm], text: "Close", onClick: async () => _gdPreview.Value = null);
            });

            view.Text([Text.Caption, "mb-3"], preview.Note, props: TestId("gd-preview-note"));

            if (preview.Text != null)
            {
                view.Box(["bg-secondary rounded-md p-4 max-h-96 overflow-auto"], content: view =>
                {
                    view.Text([Text.Body, "font-mono whitespace-pre-wrap text-sm"], preview.Text, props: TestId("gd-preview-text"));
                });
            }
            else if (preview.Bytes != null && preview.MimeType.StartsWith("image/", StringComparison.Ordinal))
            {
                view.Image(["max-h-96 rounded-md"], data: preview.Bytes, mimeType: preview.MimeType, alt: preview.FileName);
            }
        });
    }

    private void RenderGoogleDriveBinCard(UIView view)
    {
        if (!_gdBinShown.Value)
        {
            return;
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Row([Layout.Row.Md, "items-center mb-3"], content: view =>
            {
                view.Text([Text.H3, "flex-1"], "Bin");
                view.Button([Button.GhostSm], text: "Close", onClick: async () => _gdBinShown.Value = false);
            });

            if (_gdBin.Count == 0)
            {
                view.Text([Text.Caption], "The bin is empty", props: TestId("gd-bin-empty"));
            }

            foreach (var file in _gdBin)
            {
                view.Row([Layout.Row.Md, "items-center py-2 border-b border-secondary"], key: file.Id, content: view =>
                {
                    view.Text([Text.Body, "flex-1 truncate"], file.Name, props: TestId("gd-bin-file"));
                    view.Text([Text.Caption], GdDescribe(file));
                    view.Button([Button.OutlineSm], text: "Restore", disabled: _gdBusy.Value, props: TestId("gd-restore"),
                        onClick: async () => await GdRunAsync($"restore {file.Name}", () => GdRestoreAsync(file)));
                    view.Button([Button.GhostErrorSm], text: "Delete permanently", disabled: _gdBusy.Value,
                        onClick: async () => _gdPermanentDeleteTarget.Value = file);
                });

                view.AlertDialog(
                    open: _gdPermanentDeleteTarget.Value?.Id == file.Id && _gdActiveItem.Value?.Id != file.Id,
                    onOpenChange: async open => _gdPermanentDeleteTarget.Value = open ? file : null,
                    title: $"Delete {file.Name} for good?",
                    description: "It leaves the bin for good; this cannot be undone.",
                    cancelLabel: "Keep it",
                    actionLabel: "Delete permanently",
                    onAction: async () =>
                    {
                        _gdPermanentDeleteTarget.Value = null;
                        await GdRunAsync($"permanently delete {file.Name}", () => GdPermanentDeleteAsync(file));
                    });
            }
        });
    }

    private void GdToggleActiveItem(DriveFile file)
    {
        _gdActiveItem.Value = _gdActiveItem.Value?.Id == file.Id ? null : file;
        _gdPanelView.Value = "";
        _gdRenameInput.Value = "";
        _gdMoveTarget.Value = "";
    }

    private string GdCurrentFolderId => _gdFolderPath.Count > 0 ? _gdFolderPath[^1].Id : _gdSharedDrive.Value?.Id ?? "root";

    private DriveSort? GdSortOrder => _gdSort.Value switch
    {
        "name" => DriveSort.Name,
        "modified" => DriveSort.NewestModified,
        "created" => DriveSort.NewestCreated,
        _ => null,
    };

    // -1 is the root; otherwise the index of the folder in the breadcrumb.
    private async Task<string> GdOpenFolderAsync(int depth)
    {
        while (_gdFolderPath.Count > depth + 1)
        {
            _gdFolderPath.RemoveAt(_gdFolderPath.Count - 1);
        }

        return await GdLoadFolderAsync();
    }

    private async Task<string> GdEnterFolderAsync(DriveFile folder)
    {
        _gdFolderPath.Add(folder);
        return await GdLoadFolderAsync();
    }

    // Drive's own order lists through the folder endpoint, which also proves the folder readable; a
    // chosen order goes through a search, which is what takes one.
    private async Task<string> GdLoadFolderAsync()
    {
        var drive = GdClients().Drive;
        var folderId = GdCurrentFolderId;
        var page = GdSortOrder is { } sort
            ? await drive.SearchPageAsync(DriveQuery.And(DriveQuery.InFolder(folderId), DriveQuery.NotTrashed), _gdSharedDrive.Value?.Id, GdPageSize, sort: sort)
            : await drive.ListChildrenPageAsync(folderId, GdPageSize);

        _gdListing.Value = "folder";
        _gdActiveItem.Value = null;
        _gdItems.ReplaceAll(page.Items);
        _gdNextCursor.Value = page.NextCursor;
        var name = _gdFolderPath.Count > 0 ? _gdFolderPath[^1].Name : _gdSharedDrive.Value?.Name ?? "My Drive";
        return $"PASS listed {name}: {page.Items.Count(f => f.IsFolder)} folders, {page.Items.Count(f => !f.IsFolder)} files{(page.HasMore ? ", more on the next page" : "")}";
    }

    private async Task<string> GdLoadMoreAsync()
    {
        var drive = GdClients().Drive;
        var cursor = _gdNextCursor.Value;
        var page = _gdListing.Value switch
        {
            "recent" => await drive.SearchPageAsync(GdRecentQuery, limit: GdPageSize, cursor: cursor, sort: DriveSort.NewestModified),
            "search" => await drive.SearchPageAsync(GdSearchQuery(_gdSearchQuery.Value.Trim()), limit: GdPageSize, cursor: cursor),
            _ when GdSortOrder is { } sort => await drive.SearchPageAsync(DriveQuery.And(DriveQuery.InFolder(GdCurrentFolderId), DriveQuery.NotTrashed), _gdSharedDrive.Value?.Id, GdPageSize, cursor, sort),
            _ => await drive.ListChildrenPageAsync(GdCurrentFolderId, GdPageSize, cursor),
        };

        _gdItems.AddRange(page.Items);
        _gdNextCursor.Value = page.NextCursor;
        return $"PASS read {page.Items.Count} more{(page.HasMore ? "; still more" : "; that was the last page")}";
    }

    private static string GdSearchQuery(string text) => DriveQuery.And(DriveQuery.FullTextContains(text), DriveQuery.NotTrashed);

    private async Task<string> GdListRecentAsync()
    {
        var page = await GdClients().Drive.SearchPageAsync(GdRecentQuery, limit: GdPageSize, sort: DriveSort.NewestModified);

        _gdListing.Value = "recent";
        _gdListingTitle.Value = "Recently changed files across My Drive and what is shared with you, newest first";
        _gdItems.ReplaceAll(page.Items);
        _gdNextCursor.Value = page.NextCursor;
        return $"PASS listed {page.Items.Count} recent files, newest changed {page.Items.FirstOrDefault()?.ModifiedTime:yyyy-MM-dd HH:mm}";
    }

    private async Task<string> GdSearchAsync()
    {
        var text = _gdSearchQuery.Value.Trim();
        var page = await GdClients().Drive.SearchPageAsync(GdSearchQuery(text), limit: GdPageSize);

        _gdListing.Value = "search";
        _gdListingTitle.Value = $"Files whose name or content matches “{text}” — Drive's index can lag a fresh upload by minutes";
        _gdItems.ReplaceAll(page.Items);
        _gdNextCursor.Value = page.NextCursor;
        return $"PASS search for “{text}” found {page.Items.Count}{(page.HasMore ? "+" : "")}";
    }

    private async Task<string> GdListSharedDrivesAsync()
    {
        var drives = await GdClients().Drive.ListSharedDrivesAsync();
        _gdSharedDrives.ReplaceAll(drives);
        return drives.Count == 0 ? "PASS the account is in no shared drive" : $"PASS the account is in {drives.Count} shared drives";
    }

    private async Task<string> GdOpenSharedDriveAsync(SharedDrive? sharedDrive)
    {
        _gdSharedDrive.Value = sharedDrive;
        _gdFolderPath.Clear();
        return await GdLoadFolderAsync();
    }

    private async Task<string> GdCreateFolderAsync()
    {
        var folder = await GdClients().Drive.CreateFolderAsync(GdCurrentFolderId, _gdNewFolderName.Value.Trim());
        _gdNewFolderName.Value = "";
        await GdLoadFolderAsync();
        return $"PASS created folder {folder.Name} ({folder.Id})";
    }

    private async Task<string> GdUploadAsync(FileUploadCompleteArgs args)
    {
        if (args.LocalTempFilePath is not { } path)
        {
            return $"FAIL upload {args.FileName}: the file never reached the server";
        }

        var convertTo = _gdConvertUploads.Value && GdConversions.TryGetValue(Path.GetExtension(args.FileName), out var native) ? native : null;
        var mimeType = string.IsNullOrEmpty(args.MimeType) ? "application/octet-stream" : args.MimeType;

        await using var content = File.OpenRead(path);
        var uploaded = await GdClients().Drive.UploadAsync(GdCurrentFolderId, args.FileName, mimeType, content, content.Length, new DriveUploadOptions { ConvertTo = convertTo });

        await GdLoadFolderAsync();
        return $"PASS uploaded {uploaded.Name} ({SpSize(content.Length)}{(content.Length > 5 * 1024 * 1024 ? ", resumable" : "")}){(convertTo != null ? $" as {uploaded.MimeType}" : "")}";
    }

    private async Task<string> GdRenameAsync(DriveFile file)
    {
        var renamed = await GdClients().Drive.RenameAsync(file.Id, _gdRenameInput.Value.Trim());
        _gdRenameInput.Value = "";
        GdReplaceItem(renamed);
        return $"PASS renamed {file.Name} to {renamed.Name}";
    }

    private async Task<string> GdStarAsync(DriveFile file)
    {
        var updated = await GdClients().Drive.UpdateAsync(file.Id, starred: !file.Starred);
        GdReplaceItem(updated);
        return $"PASS {(updated.Starred ? "starred" : "unstarred")} {updated.Name}";
    }

    private async Task<string> GdCopyAsync(DriveFile file)
    {
        var copy = await GdClients().Drive.CopyAsync(file.Id);
        await GdLoadFolderAsync();
        return $"PASS copied {file.Name} to {copy.Name}";
    }

    private async Task<string> GdShortcutAsync(DriveFile file)
    {
        var shortcut = await GdClients().Drive.CreateShortcutAsync(GdCurrentFolderId, file.Id);
        await GdLoadFolderAsync();
        return $"PASS created shortcut {shortcut.Name} pointing at {shortcut.ShortcutTargetId}";
    }

    private async Task<string> GdMoveAsync(DriveFile file, string targetFolderId)
    {
        await GdClients().Drive.MoveAsync(file.Id, targetFolderId);
        _gdItems.RemoveAll(f => f.Id == file.Id);
        _gdActiveItem.Value = null;
        return $"PASS moved {file.Name} into {_gdItems.FirstOrDefault(f => f.Id == targetFolderId)?.Name ?? "the folder above"}";
    }

    private async Task<string> GdBinAsync(DriveFile file)
    {
        await GdClients().Drive.DeleteAsync(file.Id);
        _gdItems.RemoveAll(f => f.Id == file.Id);

        if (_gdPreview.Value?.File.Id == file.Id)
        {
            _gdPreview.Value = null;
        }

        return $"PASS moved {file.Name} to the bin; Restore brings it back from Bin";
    }

    private async Task<string> GdListBinAsync()
    {
        var page = await GdClients().Drive.SearchPageAsync("trashed = true", limit: GdPageSize, sort: DriveSort.NewestModified);
        _gdBin.ReplaceAll(page.Items);
        _gdBinShown.Value = true;
        return $"PASS the bin holds {page.Items.Count}{(page.HasMore ? "+" : "")} files";
    }

    private async Task<string> GdRestoreAsync(DriveFile file)
    {
        var restored = await GdClients().Drive.RestoreAsync(file.Id);
        _gdBin.RemoveAll(f => f.Id == file.Id);
        await GdLoadFolderAsync();
        return $"PASS restored {restored.Name} to {(restored.Parents.Count > 0 ? restored.Parents[0] : "its folder")}";
    }

    private async Task<string> GdPermanentDeleteAsync(DriveFile file)
    {
        await GdClients().Drive.PermanentDeleteAsync(file.Id);
        _gdItems.RemoveAll(f => f.Id == file.Id);
        _gdBin.RemoveAll(f => f.Id == file.Id);
        _gdActiveItem.Value = null;
        return $"PASS deleted {file.Name} for good";
    }

    private async Task<string> GdPreviewAsync(DriveFile file)
    {
        var drive = GdClients().Drive;
        var started = DateTime.UtcNow;

        if (file.IsGoogleNative)
        {
            if (!GdPreviewExports.TryGetValue(file.MimeType, out var target))
            {
                _gdPreview.Value = new GdPreview(file, null, null, file.MimeType, file.Name, $"{file.MimeType} has no text export; open it in Drive");
                return $"PASS {file.Name} is a {file.MimeType} with nothing to preview here";
            }

            return await GdExportAsync(file, target);
        }

        if (file.Size > GdPreviewLimitBytes)
        {
            _gdPreview.Value = new GdPreview(file, null, null, file.MimeType, file.Name, $"{SpSize(file.Size)} is over the {SpSize(GdPreviewLimitBytes)} preview limit — open it in Drive");
            return $"PASS {file.Name} is too large to preview here";
        }

        var bytes = await drive.DownloadBytesAsync(file.Id, GdPreviewLimitBytes);
        var ms = (DateTime.UtcNow - started).TotalMilliseconds;
        _gdPreview.Value = new GdPreview(file, GdTextOf(file.MimeType, file.Name, bytes), bytes, file.MimeType, file.Name,
            $"{SpSize(bytes.Length)} {file.MimeType}, downloaded in {ms:0} ms{(file.Md5Checksum is { } md5 ? $", Drive's MD5 {md5}" : "")}");
        return $"PASS read {file.Name} ({SpSize(bytes.Length)})";
    }

    // An export over 10 MB is refused by the export endpoint and read from the file's export link
    // instead, so a large Doc here exercises that fallback too.
    private async Task<string> GdExportAsync(DriveFile file, string targetMimeType)
    {
        var started = DateTime.UtcNow;
        await using var exported = await GdClients().Drive.ExportAsync(file.Id, targetMimeType);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;

        while ((read = await exported.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > GdPreviewLimitBytes)
            {
                _gdPreview.Value = new GdPreview(file, null, null, targetMimeType, file.Name, $"The {targetMimeType} export is over the {SpSize(GdPreviewLimitBytes)} preview limit — open it in Drive");
                return $"PASS the {targetMimeType} export of {file.Name} is too large to preview here";
            }

            buffer.Write(chunk, 0, read);
        }

        var bytes = buffer.ToArray();
        var ms = (DateTime.UtcNow - started).TotalMilliseconds;
        var fileName = $"{file.Name}{GdExtensionOf(targetMimeType)}";
        var note = $"Exported by Drive as {targetMimeType}: {SpSize(bytes.Length)} in {ms:0} ms{(file.MimeType.EndsWith("spreadsheet", StringComparison.Ordinal) && targetMimeType == "text/csv" ? " — a CSV export holds the first sheet only" : "")}";

        _gdPreview.Value = new GdPreview(file, GdTextOf(targetMimeType, fileName, bytes), bytes, targetMimeType, fileName, note);
        return $"PASS exported {file.Name} as {targetMimeType} ({SpSize(bytes.Length)})";
    }

    private async Task<string> GdLoadExportFormatsAsync(DriveFile file)
    {
        _gdExportFormats ??= await GdClients().Drive.GetExportFormatsAsync();
        var targets = _gdExportFormats.TryGetValue(file.MimeType, out var formats) ? formats : [];

        _gdExportTargets.ReplaceAll(targets);
        _gdPanelView.Value = "export";
        return $"PASS {file.MimeType} exports to {targets.Count} formats";
    }

    private async Task<string> GdLoadSharingAsync(DriveFile file)
    {
        var drive = GdClients().Drive;
        var permissions = await drive.ListPermissionsAsync(file.Id);
        IReadOnlyList<DriveAccessProposal> proposals = [];
        var proposalNote = "";

        try
        {
            proposals = await drive.ListAccessProposalsAsync(file.Id);
        }
        catch (ConnectorException ex) when (ex.StatusCode is 403 or 404)
        {
            // Only the file's owner or an editor sees its access requests; the permissions read above
            // is the check that matters here.
            proposalNote = "; access requests are visible only to the owner or an editor";
        }

        _gdPermissions.ReplaceAll(permissions);
        _gdProposals.ReplaceAll(proposals);
        _gdPanelView.Value = "sharing";
        return $"PASS {file.Name} has {permissions.Count} permissions, {permissions.Count(p => p.Inherited)} inherited, and {proposals.Count} access requests{proposalNote}";
    }

    private async Task<string> GdShareAsync(DriveFile file, NewDrivePermission permission)
    {
        var added = await GdClients().Drive.AddPermissionAsync(file.Id, permission);
        _gdPermissionsCreatedHere.Add(added.Id);
        _gdShareEmail.Value = "";
        await GdLoadSharingAsync(file);
        return $"PASS gave {(added.Type == DrivePermissionType.Anyone ? "anyone with the link" : added.EmailAddress)} {added.Role} access to {file.Name}";
    }

    private async Task<string> GdRemovePermissionAsync(DriveFile file, DrivePermission permission)
    {
        await GdClients().Drive.RemovePermissionAsync(file.Id, permission.Id);
        _gdPermissionsCreatedHere.Remove(permission.Id);
        await GdLoadSharingAsync(file);
        return $"PASS removed {permission.EmailAddress ?? permission.Type.ToString()}'s access to {file.Name}";
    }

    private async Task<string> GdLoadCommentsAsync(DriveFile file)
    {
        var comments = await GdClients().Drive.ListCommentsAsync(file.Id);
        _gdComments.ReplaceAll(comments);
        _gdPanelView.Value = "comments";
        return $"PASS {file.Name} has {comments.Count} comments, {comments.Count(c => c.Resolved)} resolved";
    }

    private async Task<string> GdCommentAsync(DriveFile file)
    {
        var comment = await GdClients().Drive.CreateCommentAsync(file.Id, _gdCommentInput.Value.Trim());
        _gdCommentInput.Value = "";
        await GdLoadCommentsAsync(file);
        return $"PASS commented on {file.Name} ({comment.Id})";
    }

    private async Task<string> GdReplyAsync(DriveFile file, DriveComment comment)
    {
        await GdClients().Drive.AddReplyAsync(file.Id, comment.Id, _gdReplyInput.Value.Trim());
        _gdReplyInput.Value = "";
        await GdLoadCommentsAsync(file);
        return "PASS replied";
    }

    private async Task<string> GdResolveAsync(DriveFile file, DriveComment comment)
    {
        var drive = GdClients().Drive;
        _ = comment.Resolved ? await drive.ReopenCommentAsync(file.Id, comment.Id) : await drive.ResolveCommentAsync(file.Id, comment.Id);
        await GdLoadCommentsAsync(file);
        return $"PASS {(comment.Resolved ? "reopened" : "resolved")} the comment";
    }

    private async Task<string> GdDeleteCommentAsync(DriveFile file, DriveComment comment)
    {
        await GdClients().Drive.DeleteCommentAsync(file.Id, comment.Id);
        await GdLoadCommentsAsync(file);
        return "PASS deleted the comment";
    }

    private async Task<string> GdLoadRevisionsAsync(DriveFile file)
    {
        var revisions = await GdClients().Drive.ListRevisionsAsync(file.Id);
        _gdRevisions.ReplaceAll(revisions);
        _gdPanelView.Value = "revisions";
        return $"PASS {file.Name} has {revisions.Count} revisions";
    }

    private async Task<string> GdPreviewRevisionAsync(DriveFile file, DriveRevision revision)
    {
        if (revision.Size > GdPreviewLimitBytes)
        {
            return $"SKIP revision {revision.Id} is {SpSize(revision.Size)}, over the preview limit";
        }

        await using var content = await GdClients().Drive.DownloadRevisionAsync(file.Id, revision.Id);
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        var mimeType = revision.MimeType ?? file.MimeType;
        var fileName = revision.OriginalFilename ?? file.Name;

        _gdPreview.Value = new GdPreview(file, GdTextOf(mimeType, fileName, bytes), bytes, mimeType, fileName,
            $"Revision {revision.Id} of {revision.ModifiedTime:yyyy-MM-dd HH:mm}: {SpSize(bytes.Length)}");
        return $"PASS read revision {revision.Id} ({SpSize(bytes.Length)})";
    }

    private async Task<string> GdKeepRevisionAsync(DriveFile file, DriveRevision revision)
    {
        var updated = await GdClients().Drive.KeepRevisionAsync(file.Id, revision.Id, !revision.KeepForever);
        await GdLoadRevisionsAsync(file);
        return $"PASS revision {revision.Id} is {(updated.KeepForever ? "kept forever" : "left for Drive to prune")}";
    }

    private void GdReplaceItem(DriveFile file)
    {
        _gdItems.ReplaceAll(_gdItems.Select(f => f.Id == file.Id ? file : f).ToList());

        if (_gdActiveItem.Value?.Id == file.Id)
        {
            _gdActiveItem.Value = file;
        }
    }

    private static string GdDescribe(DriveFile file)
    {
        var kind = file.IsFolder ? "Folder" : file.IsGoogleNative ? file.MimeType[GdNativePrefix.Length..] : $"{SpSize(file.Size)} {file.MimeType}";
        return $"{kind} · {file.ModifiedTime:yyyy-MM-dd HH:mm}{(file.OwnerEmails.Count > 0 ? $" · {file.OwnerEmails[0]}" : "")}";
    }

    private static string? GdTextOf(string mimeType, string fileName, byte[] bytes)
    {
        var isText = mimeType.StartsWith("text/", StringComparison.Ordinal) || mimeType is "application/json" or "image/svg+xml"
            || SpTextExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());

        if (!isText)
        {
            return null;
        }

        var text = Encoding.UTF8.GetString(bytes);
        return text.Length > GdTextPreviewChars ? text[..GdTextPreviewChars] + $"\n… ({text.Length - GdTextPreviewChars} more characters)" : text;
    }

    private static string GdExtensionOf(string mimeType) => mimeType switch
    {
        "text/markdown" => ".md",
        "text/csv" => ".csv",
        "text/plain" => ".txt",
        "text/html" => ".html",
        "application/pdf" => ".pdf",
        "image/svg+xml" => ".svg",
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
        "application/vnd.oasis.opendocument.text" => ".odt",
        "application/rtf" => ".rtf",
        "application/epub+zip" => ".epub",
        "application/zip" => ".zip",
        _ => "",
    };
}
