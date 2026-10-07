using System.Text;
using System.Text.Json;
using Ikon.Connectors;
using Ikon.Connectors.Microsoft;

public partial class Validation
{
    private const string SpTenantSecret = "MICROSOFT_TENANT_ID";
    private const string SpClientSecret = "MICROSOFT_CLIENT_ID";
    private const string SpClientSecretSecret = "MICROSOFT_CLIENT_SECRET";
    private const string SpSiteSecret = "SHAREPOINT_SITE_URL";
    private const string SpCertificateSecret = "MICROSOFT_CLIENT_CERTIFICATE";
    private const string SpCertificatePasswordSecret = "MICROSOFT_CLIENT_CERTIFICATE_PASSWORD";

    private static readonly (string Key, string Description)[] SpRequiredSecrets =
    [
        (SpTenantSecret, "Entra tenant the SharePoint test app is registered in"),
        (SpClientSecret, "Application (client) ID of the SharePoint test app"),
    ];

    // A file opened for preview is held in memory and handed to the browser as a data URL, so a
    // larger one is offered only through SharePoint's own link.
    private const long SpPreviewLimitBytes = 10 * 1024 * 1024;
    private const int SpTextPreviewChars = 20_000;
    private const int SpListPages = 5;

    private static readonly string[] SpOfficeExtensions = [".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".odt", ".ods", ".odp", ".rtf"];
    private static readonly string[] SpTextExtensions = [".txt", ".md", ".json", ".csv", ".xml", ".log", ".yaml", ".yml"];

    private readonly Reactive<string> _spSiteInput = new("");
    private readonly Reactive<string> _spSubTab = new("files");
    private readonly Reactive<bool> _spBusy = new(false);
    private readonly Reactive<string> _spStatus = new("");
    private readonly Reactive<SharePointSite?> _spSite = new(null);
    private readonly ReactiveList<DocumentLibrary> _spLibraries = new();
    private readonly ReactiveList<SharePointList> _spLists = new();
    private readonly Reactive<DocumentLibrary?> _spLibrary = new(null);
    private readonly ReactiveList<DriveItem> _spFolderPath = new();
    private readonly ReactiveList<DriveItem> _spItems = new();
    private readonly Reactive<string> _spSearchQuery = new("");
    private readonly Reactive<string?> _spSearchShown = new(null);
    private readonly Reactive<string> _spNewFolderName = new("");
    private readonly Reactive<SpPreview?> _spPreview = new(null);
    private readonly ReactiveDictionary<string, string> _spDeltaLinks = new();
    private readonly Reactive<bool> _spSharingChanges = new(false);
    private readonly ReactiveList<DriveItem> _spChanges = new();
    private readonly Reactive<string?> _spChangesSummary = new(null);
    private readonly Reactive<SharePointList?> _spList = new(null);
    private readonly ReactiveList<ListItem> _spListItems = new();
    private readonly Reactive<string> _spNewItemTitle = new("");

    private (MicrosoftTokenProvider Tokens, SharePoint SharePoint, OneDrive OneDrive)? _spClients;

    private sealed record SpPreview(DriveItem Item, string? Text, byte[]? Bytes, string MimeType, string FileName, string Note);

    private void RenderSharePointSection(UIView view)
    {
        if (RenderSectionLocked(view, "SharePoint"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "SharePoint");
            RenderSharePointSiteCard(view);

            if (SpMissingSecrets().Count > 0)
            {
                return;
            }

            RenderSharePointDelegatedCard(view);

            if (_spSite.Value == null)
            {
                RenderSharePointDiscoveryCard(view);
                return;
            }

            view.Tabs(
                value: _spSubTab.Value,
                onValueChange: async value => _spSubTab.Value = value ?? "files",
                listContainerStyle: [Card.Default, "p-2"],
                listStyle: [Tabs.List, "flex-wrap bg-transparent gap-1"],
                triggerStyle: [Tabs.Trigger, "text-xs px-2 py-1"],
                contentStyle: [Tabs.Content, "flex flex-col gap-6"],
                tabs: [
                    new TabItem("files", "Files", view =>
                    {
                        RenderSharePointFilesCard(view);
                        RenderSharePointPreviewCard(view);
                        RenderSharePointChangesCard(view);
                        RenderSharePointRecycleBinCard(view);
                    }),
                    new TabItem("lists", "Lists", RenderSharePointListsCard),
                    new TabItem("pages", "Pages", RenderSharePointPagesCard),
                    new TabItem("search", "Search", RenderSharePointSearchCard),
                    new TabItem("access", "Access", view =>
                    {
                        RenderSharePointDiscoveryCard(view);
                        RenderSharePointAccessCard(view);
                    }),
                    new TabItem("notifications", "Notifications", RenderSharePointNotificationsCard),
                ]);
        });
    }

    private void RenderSharePointSiteCard(UIView view)
    {
        view.Box([Card.Elevated, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Site");

            var missing = SpMissingSecrets();

            if (missing.Count > 0)
            {
                view.Text([Text.Body, "text-warning-primary mb-2"], $"SKIP: set the secrets {string.Join(", ", missing.Select(s => s.Key))} on this space", props: TestId("sp-setup"));

                foreach (var (key, description) in missing)
                {
                    view.Text([Text.Caption, "font-mono"], $"ikon secret set {key} --description \"{description}\"", key: key);
                }

                view.Text([Text.Caption, "font-mono"], $"ikon secret set {SpSiteSecret} --description \"SharePoint site the tab opens by default\"   (optional)");
                return;
            }

            app.Secrets.TryGet(SpSiteSecret, out var defaultSite);
            var kind = SpHasCertificate() ? "a certificate" : "a client secret";
            view.Text([Text.Caption, "mb-3"], $"Signs in app-only with {kind} as the app registered in tenant {app.Secrets[SpTenantSecret]} (client {app.Secrets[SpClientSecret]})", props: TestId("sp-setup"));

            if (!SpHasCertificate())
            {
                view.Text([Text.Caption, "mb-3"], $"Actions that use SharePoint's own REST API need a certificate: set {SpCertificateSecret} (base64 PFX) and, if it has one, {SpCertificatePasswordSecret}");
            }

            view.Row([Layout.Row.Md, "items-end flex-wrap"], content: view =>
            {
                view.Box([FormField.Root, "flex-1 min-w-64"], content: view =>
                {
                    view.Text([FormField.Label], "Site URL");
                    view.TextField(
                        [Input.Default],
                        value: _spSiteInput.Value,
                        placeholder: string.IsNullOrWhiteSpace(defaultSite) ? "https://contoso.sharepoint.com/sites/Team" : defaultSite,
                        props: TestId("sp-site-input"),
                        onValueChange: async v => _spSiteInput.Value = v ?? "");
                });

                view.Button([Button.PrimaryMd],
                    text: "Open site",
                    disabled: _spBusy.Value,
                    props: TestId("sp-open"),
                    onClick: async () => await SpRunAsync("open site", OpenSharePointSiteAsync));

                view.Button([Button.OutlineMd],
                    text: "Check sign-in",
                    disabled: _spBusy.Value,
                    props: TestId("sp-check-token"),
                    onClick: async () => await SpRunAsync("check sign-in", CheckSharePointSignInAsync));

                if (_spBusy.Value)
                {
                    view.Spinner();
                }
            });

            if (_spStatus.Value.Length > 0)
            {
                view.Text([Text.Body, "mt-3", SpStatusColor(_spStatus.Value)], _spStatus.Value, props: TestId("sp-status"));
            }

            if (_spSite.Value is { } site)
            {
                view.Text([Text.Caption, "mt-2"], $"{site.DisplayName} · {site.WebUrl} · {site.Id}", props: TestId("sp-site"));
                view.Text([Text.Label, "mt-4 mb-2"], "Document libraries");

                view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                {
                    foreach (var library in _spLibraries)
                    {
                        var selected = _spLibrary.Value?.Id == library.Id;
                        view.Button([selected ? Button.PrimarySm : Button.OutlineSm],
                            key: library.Id,
                            text: library.Name,
                            props: TestId($"sp-library-{library.Name}"),
                            onClick: async () => await SpRunAsync($"open {library.Name}", () => OpenSharePointLibraryAsync(library)));
                    }
                });
            }
        });
    }

    private void RenderSharePointFilesCard(UIView view)
    {
        if (_spLibrary.Value is not { } library)
        {
            return;
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-3"], "Files");

            view.Row([Layout.Row.Sm, "items-center flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.GhostSm], text: library.Name, props: TestId("sp-crumb-root"),
                    onClick: async () => await SpRunAsync("open the library root", () => OpenSharePointFolderAsync(-1)));

                for (var i = 0; i < _spFolderPath.Count; i++)
                {
                    var depth = i;
                    view.Text([Text.Caption], "/");
                    view.Button([Button.GhostSm], key: _spFolderPath[i].Id, text: _spFolderPath[i].Name,
                        onClick: async () => await SpRunAsync("open the folder", () => OpenSharePointFolderAsync(depth)));
                }
            });

            view.Row([Layout.Row.Md, "items-end flex-wrap mb-4"], content: view =>
            {
                view.TextField([Input.Default, "flex-1 min-w-48"],
                    value: _spSearchQuery.Value,
                    placeholder: "Search this library",
                    props: TestId("sp-search-input"),
                    onValueChange: async v => _spSearchQuery.Value = v ?? "");

                view.Button([Button.OutlineMd], text: "Search", disabled: _spBusy.Value || _spSearchQuery.Value.Trim().Length == 0, props: TestId("sp-search"),
                    onClick: async () => await SpRunAsync("search", SearchSharePointAsync));

                view.TextField([Input.Default, "w-48"],
                    value: _spNewFolderName.Value,
                    placeholder: "New folder name",
                    props: TestId("sp-folder-input"),
                    onValueChange: async v => _spNewFolderName.Value = v ?? "");

                view.Button([Button.OutlineMd], text: "Create folder", disabled: _spBusy.Value || _spNewFolderName.Value.Trim().Length == 0, props: TestId("sp-folder-create"),
                    onClick: async () => await SpRunAsync("create the folder", CreateSharePointFolderAsync));
            });

            if (_spSearchShown.Value is { } shown)
            {
                view.Row([Layout.Row.Sm, "items-center mb-2"], content: view =>
                {
                    view.Text([Text.Caption], $"Search results for “{shown}” — the search index can lag a fresh upload by minutes");
                    view.Button([Button.GhostSm], text: "Back to the folder", onClick: async () => await SpRunAsync("open the folder", () => OpenSharePointFolderAsync(_spFolderPath.Count - 1)));
                });
            }

            if (_spItems.Count == 0)
            {
                view.Text([Text.Caption, "py-4"], _spSearchShown.Value != null ? "Nothing found" : "This folder is empty", props: TestId("sp-files-empty"));
            }

            foreach (var item in _spItems)
            {
                view.Row([Layout.Row.Md, "items-center py-2 border-b border-secondary"], key: item.Id, content: view =>
                {
                    view.Icon([Icon.Default], name: item.IsFolder ? "folder" : "file");
                    view.Column(["flex-1 min-w-0"], content: view =>
                    {
                        view.Text([Text.Body, "truncate"], item.Name, props: TestId("sp-file"));
                        view.Text([Text.Caption], item.IsFolder ? "Folder" : $"{SpSize(item.Size)} · {item.LastModified:yyyy-MM-dd HH:mm}");
                    });

                    view.Button([Button.OutlineSm], text: item.IsFolder ? "Open" : "View", disabled: _spBusy.Value,
                        onClick: async () => await SpRunAsync(item.IsFolder ? "open the folder" : $"read {item.Name}",
                            () => item.IsFolder ? EnterSharePointFolderAsync(item) : PreviewSharePointItemAsync(item, asPdf: false)));

                    if (!item.IsFolder && SpOfficeExtensions.Contains(Path.GetExtension(item.Name).ToLowerInvariant()))
                    {
                        view.Button([Button.OutlineSm], text: "As PDF", disabled: _spBusy.Value,
                            onClick: async () => await SpRunAsync($"convert {item.Name}", () => PreviewSharePointItemAsync(item, asPdf: true)));
                    }

                    view.Button([_spActiveItem.Value?.Id == item.Id ? Button.SecondarySm : Button.GhostSm], text: "More", disabled: _spBusy.Value,
                        props: new Dictionary<string, object> { ["aria-label"] = $"More actions for {item.Name}", ["data-testid"] = "sp-file-more" },
                        onClick: async () => SpToggleActiveItem(item));

                    view.Button([Button.GhostErrorSm], text: "Delete", disabled: _spBusy.Value,
                        props: new Dictionary<string, object> { ["aria-label"] = $"Delete {item.Name}", ["title"] = "Moves it to the site's recycle bin" },
                        onClick: async () => await SpRunAsync($"delete {item.Name}", () => DeleteSharePointItemAsync(item)));
                });

                if (_spActiveItem.Value?.Id == item.Id)
                {
                    RenderSharePointItemPanel(view, item);
                }
            }

            RenderSharePointOpenByLink(view);

            view.FileUpload(
                [FileUpload.Zone.Base, "mt-4"],
                multiple: false,
                onUploadComplete: async args => await SpRunAsync($"upload {args.FileName}", () => UploadToSharePointAsync(args)),
                onUploadError: async args => _spStatus.Value = $"FAIL upload: {args.ErrorMessage}",
                content: view =>
                {
                    view.Column([Layout.Column.Center], content: view =>
                    {
                        view.Icon([Media.PlaceholderIcon], name: "upload");
                        view.Text([Text.Body], "Upload into this folder");
                        view.Text([Text.Caption], "A taken name gets a numbered copy; files over 4 MiB go up in chunks");
                    });
                });
        });
    }

    private void RenderSharePointPreviewCard(UIView view)
    {
        if (_spPreview.Value is not { } preview)
        {
            return;
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Row([Layout.Row.Md, "items-center mb-3"], content: view =>
            {
                view.Text([Text.H3, "flex-1"], preview.FileName, props: TestId("sp-preview-name"));

                if (preview.Bytes != null)
                {
                    view.ActionButton([Button.OutlineSm],
                        action: ActionKind.DownloadFile,
                        options: new DownloadFileActionOptions { Data = preview.Bytes, MimeType = preview.MimeType, Filename = preview.FileName },
                        content: v => v.Text(text: "Download"));
                }

                if (preview.Item.WebUrl is { } webUrl)
                {
                    view.Link([Button.GhostSm], href: webUrl, text: "Open in SharePoint", target: "_blank");
                }

                view.Button([Button.GhostSm], text: "Close", onClick: async () => _spPreview.Value = null);
            });

            view.Text([Text.Caption, "mb-3"], preview.Note, props: TestId("sp-preview-note"));

            if (preview.Text != null)
            {
                view.Box(["bg-secondary rounded-md p-4 max-h-96 overflow-auto"], content: view =>
                {
                    view.Text([Text.Body, "font-mono whitespace-pre-wrap text-sm"], preview.Text, props: TestId("sp-preview-text"));
                });
            }
            else if (preview.Bytes != null && preview.MimeType.StartsWith("image/", StringComparison.Ordinal))
            {
                view.Image(["max-h-96 rounded-md"], data: preview.Bytes, mimeType: preview.MimeType, alt: preview.FileName);
            }
        });
    }

    private void RenderSharePointChangesCard(UIView view)
    {
        if (_spLibrary.Value is not { } library)
        {
            return;
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Changes");
            view.Text([Text.Caption, "mb-3"], _spDeltaLinks.ContainsKey(library.Id)
                ? "Shows what changed in this library since the last check"
                : "The first check reads the whole library and remembers where it stopped; the next shows only what changed");

            view.Row([Layout.Row.Md, "items-center flex-wrap"], content: view =>
            {
                view.Button([Button.OutlineMd], text: "Check for changes", disabled: _spBusy.Value, props: TestId("sp-changes"),
                    onClick: async () => await SpRunAsync("check for changes", () => ReadSharePointChangesAsync(fromNow: false)));
                view.Button([Button.GhostMd], text: "Start from now", disabled: _spBusy.Value, props: TestId("sp-changes-now"),
                    onClick: async () => await SpRunAsync("start changes from now", () => ReadSharePointChangesAsync(fromNow: true)));
                view.Switch([Switch.Default], value: _spSharingChanges.Value, label: "Report sharing changes (needs Sites.FullControl.All)",
                    onValueChange: value =>
                    {
                        _spSharingChanges.Value = value;
                        _spDeltaLinks.Clear();
                        return Task.CompletedTask;
                    });
            });

            if (_spChangesSummary.Value is { } summary)
            {
                view.Text([Text.Body, "mt-3"], summary, props: TestId("sp-changes-summary"));
            }

            foreach (var change in _spChanges.Take(50))
            {
                var kind = change.Deleted ? "removed" : change.SharingChanged ? "sharing changed" : change.IsFolder ? "folder" : "changed";
                view.Text([Text.Caption], $"{kind} · {(change.Deleted ? change.Id : change.Name)}", key: change.Id, props: TestId("sp-change"));
            }
        });
    }

    private List<(string Key, string Description)> SpMissingSecrets()
    {
        var missing = SpRequiredSecrets.Where(secret => !SpHasSecret(secret.Key)).ToList();

        if (!SpHasSecret(SpClientSecretSecret) && !SpHasCertificate())
        {
            missing.Add((SpClientSecretSecret, "Client secret of the SharePoint test app (or set MICROSOFT_CLIENT_CERTIFICATE instead)"));
        }

        return missing;
    }

    private bool SpHasSecret(string key)
    {
        return app.Secrets.TryGet(key, out var value) && !string.IsNullOrWhiteSpace(value);
    }

    private bool SpHasCertificate()
    {
        return SpHasSecret(SpCertificateSecret);
    }

    private MicrosoftCredentials SpCredentials()
    {
        if (!SpHasCertificate())
        {
            return new MicrosoftAppCredentials(app.Secrets[SpTenantSecret], app.Secrets[SpClientSecret], app.Secrets[SpClientSecretSecret]);
        }

        app.Secrets.TryGet(SpCertificatePasswordSecret, out var password);
        var certificate = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12(
            Convert.FromBase64String(app.Secrets[SpCertificateSecret]), string.IsNullOrEmpty(password) ? null : password);

        return new MicrosoftCertificateCredentials(app.Secrets[SpTenantSecret], app.Secrets[SpClientSecret], certificate);
    }

    private (SharePoint SharePoint, OneDrive OneDrive) SpClients()
    {
        var clients = SpConnection();
        return (clients.SharePoint, clients.OneDrive);
    }

    private (MicrosoftTokenProvider Tokens, SharePoint SharePoint, OneDrive OneDrive) SpConnection()
    {
        if (_spClients is { } clients)
        {
            return clients;
        }

        var tokens = new MicrosoftTokenProvider(_spUseDelegated.Value && _spDelegatedCredentials is { } delegated ? delegated : SpCredentials());
        clients = (tokens, new SharePoint(tokens), new OneDrive(tokens));
        _spClients = clients;
        return clients;
    }

    // The token's roles claim is what Graph authorizes app-only calls by. Without one, Graph answers
    // every call with a bare 401 that names no permission, so this is where a missing application
    // permission or admin consent becomes visible.
    private async Task<string> CheckSharePointSignInAsync()
    {
        // A fresh sign-in, so a permission or consent changed since the last token is what it reports.
        _spClients = null;
        var tokens = SpConnection().Tokens;
        var graph = await tokens.GetGrantedPermissionsAsync();

        if (_spUseDelegated.Value)
        {
            return graph.DelegatedScopes.Count == 0
                ? $"FAIL check sign-in: the delegated token for {graph.Audience} carries no scopes"
                : $"PASS signed in as the person for {graph.Audience}, delegated scopes: {string.Join(", ", graph.DelegatedScopes)}";
        }

        if (graph.ApplicationPermissions.Count == 0)
        {
            return $"FAIL check sign-in: Entra issued a token for {graph.Audience} but it carries no application permissions — add Microsoft Graph Application permission Sites.Selected (or Sites.ReadWrite.All) to the app and grant admin consent";
        }

        var result = $"PASS signed in for {graph.Audience}, application permissions: {string.Join(", ", graph.ApplicationPermissions)}{(graph.ExpiresAt is { } at ? $", token valid until {at:HH:mm} UTC" : "")}";

        if (!SpHasCertificate())
        {
            return result + "; SharePoint REST not checked (needs a certificate)";
        }

        if (_spSite.Value?.WebUrl is not { } webUrl)
        {
            return result + "; open a site to check the SharePoint REST token too";
        }

        var sharePoint = await tokens.GetGrantedPermissionsAsync($"https://{new Uri(webUrl).Host}");

        return sharePoint.ApplicationPermissions.Count == 0
            ? $"FAIL check sign-in: the Graph token is fine but the SharePoint token for {new Uri(webUrl).Host} carries no application permissions — add the SharePoint application permission Sites.Selected (or Sites.FullControl.All) and grant admin consent"
            : $"{result}; SharePoint REST: {string.Join(", ", sharePoint.ApplicationPermissions)}";
    }

    // Every action reports into one status line: PASS with what it did, or FAIL with Graph's own
    // answer, which names the missing permission or site grant when that is the cause.
    private async Task SpRunAsync(string action, Func<Task<string>> run)
    {
        if (_spBusy.Value)
        {
            return;
        }

        _spBusy.Value = true;
        _spStatus.Value = $"Working: {action}…";

        try
        {
            _spStatus.Value = await run();
        }
        catch (ConnectorException ex) when (ex.StatusCode == 401)
        {
            // The rejected token stays cached for its hour otherwise, so a permission granted to fix
            // this would not be seen until then.
            _spClients = null;
            _spStatus.Value = $"FAIL {action}: {ex.Message} — either Entra refused the secrets or the token carries no application permission: press Check sign-in to see which";
        }
        catch (ConnectorException ex) when (ex.StatusCode == 403)
        {
            _spStatus.Value = $"FAIL {action}: {ex.Message} — the app has no grant for this site (Sites.Selected) or lacks the permission for this action";
        }
        catch (Exception ex)
        {
            _spStatus.Value = $"FAIL {action}: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _spBusy.Value = false;
        }
    }

    private async Task<string> OpenSharePointSiteAsync()
    {
        var address = _spSiteInput.Value.Trim();

        if (address.Length == 0 && (!app.Secrets.TryGet(SpSiteSecret, out address) || string.IsNullOrWhiteSpace(address)))
        {
            return $"SKIP: type a site URL or set the secret {SpSiteSecret}";
        }

        var (sharePoint, _) = SpClients();
        var site = await sharePoint.GetSiteAsync(address);
        var libraries = await sharePoint.ListLibrariesAsync(site.Id);
        var lists = await sharePoint.ListListsAsync(site.Id);

        _spSite.Value = site;
        _spLibraries.ReplaceAll(libraries);
        _spLists.ReplaceAll(lists);
        _spLibrary.Value = null;
        _spFolderPath.Clear();
        _spItems.Clear();
        _spPreview.Value = null;
        _spList.Value = null;
        _spListItems.Clear();
        _spChanges.Clear();
        _spChangesSummary.Value = null;

        return $"PASS opened {site.DisplayName}: {libraries.Count} document libraries, {lists.Count(l => !l.Hidden && l.Template != "documentLibrary")} lists";
    }

    private async Task<string> OpenSharePointLibraryAsync(DocumentLibrary library)
    {
        _spLibrary.Value = library;
        _spFolderPath.Clear();
        _spChanges.Clear();
        _spChangesSummary.Value = null;
        return await LoadSharePointFolderAsync();
    }

    private async Task<string> EnterSharePointFolderAsync(DriveItem folder)
    {
        _spFolderPath.Add(folder);
        return await LoadSharePointFolderAsync();
    }

    // -1 is the library root; otherwise the index of the folder in the breadcrumb.
    private async Task<string> OpenSharePointFolderAsync(int depth)
    {
        while (_spFolderPath.Count > depth + 1)
        {
            _spFolderPath.RemoveAt(_spFolderPath.Count - 1);
        }

        return await LoadSharePointFolderAsync();
    }

    private async Task<string> LoadSharePointFolderAsync()
    {
        var library = _spLibrary.Value!;
        var folder = _spFolderPath.Count > 0 ? _spFolderPath[^1] : null;
        _spSearchShown.Value = null;
        var (_, oneDrive) = SpClients();
        IReadOnlyList<DriveItem> items;
        var capped = "";

        try
        {
            items = await oneDrive.ListChildrenAsync(library.Id, folder?.Id, maxPages: SpListPages);
        }
        catch (ConnectorPageCapException<DriveItem> cap)
        {
            items = cap.Items;
            capped = $" (the first {cap.Items.Count}; the folder holds more)";
        }

        _spItems.ReplaceAll(items.OrderByDescending(i => i.IsFolder).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase));
        return $"PASS listed {folder?.Name ?? library.Name}: {items.Count(i => i.IsFolder)} folders, {items.Count(i => !i.IsFolder)} files{capped}";
    }

    private async Task<string> SearchSharePointAsync()
    {
        var query = _spSearchQuery.Value.Trim();
        var (_, oneDrive) = SpClients();
        var results = await oneDrive.SearchAsync(_spLibrary.Value!.Id, query, maxPages: 2);

        _spItems.ReplaceAll(results);
        _spSearchShown.Value = query;
        return $"PASS search for “{query}” found {results.Count}";
    }

    private async Task<string> CreateSharePointFolderAsync()
    {
        var name = _spNewFolderName.Value.Trim();
        var (_, oneDrive) = SpClients();
        var folder = await oneDrive.CreateFolderAsync(_spLibrary.Value!.Id, _spFolderPath.Count > 0 ? _spFolderPath[^1].Id : null, name, UploadConflict.Rename);

        _spNewFolderName.Value = "";
        await LoadSharePointFolderAsync();
        return $"PASS created folder {folder.Name}";
    }

    private async Task<string> UploadToSharePointAsync(FileUploadCompleteArgs args)
    {
        if (args.LocalTempFilePath is not { } path)
        {
            return $"FAIL upload {args.FileName}: the file never reached the server";
        }

        var (_, oneDrive) = SpClients();
        await using var content = File.OpenRead(path);
        var uploaded = await oneDrive.UploadAsync(_spLibrary.Value!.Id, _spFolderPath.Count > 0 ? _spFolderPath[^1].Id : null, args.FileName, content, content.Length, UploadConflict.Rename);

        await LoadSharePointFolderAsync();
        return $"PASS uploaded {uploaded.Name} ({SpSize(uploaded.Size)}{(content.Length > 4 * 1024 * 1024 ? ", in chunks" : "")})";
    }

    private async Task<string> DeleteSharePointItemAsync(DriveItem item)
    {
        var (_, oneDrive) = SpClients();
        await oneDrive.DeleteAsync(_spLibrary.Value!.Id, item.Id, item.ETag);

        if (_spPreview.Value?.Item.Id == item.Id)
        {
            _spPreview.Value = null;
        }

        _spItems.RemoveAll(i => i.Id == item.Id);
        return $"PASS moved {item.Name} to the recycle bin";
    }

    private async Task<string> PreviewSharePointItemAsync(DriveItem item, bool asPdf)
    {
        if (!asPdf && item.Size > SpPreviewLimitBytes)
        {
            _spPreview.Value = new SpPreview(item, null, null, item.MimeType ?? "application/octet-stream", item.Name,
                $"{SpSize(item.Size)} is over the {SpSize(SpPreviewLimitBytes)} preview limit — open it in SharePoint");
            return $"PASS {item.Name} is too large to preview here";
        }

        var (_, oneDrive) = SpClients();
        var started = DateTime.UtcNow;
        byte[] bytes;

        if (asPdf)
        {
            // The converted PDF's size is known only as it arrives, so the limit is held while reading.
            await using var content = await oneDrive.DownloadAsPdfAsync(_spLibrary.Value!.Id, item.Id);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;

            while ((read = await content.ReadAsync(chunk)) > 0)
            {
                if (buffer.Length + read > SpPreviewLimitBytes)
                {
                    _spPreview.Value = new SpPreview(item, null, null, "application/pdf", Path.ChangeExtension(item.Name, ".pdf"),
                        $"The PDF is over the {SpSize(SpPreviewLimitBytes)} preview limit — open it in SharePoint");
                    return $"PASS the PDF of {item.Name} is too large to preview here";
                }

                buffer.Write(chunk, 0, read);
            }

            bytes = buffer.ToArray();
        }
        else
        {
            bytes = await oneDrive.DownloadBytesAsync(_spLibrary.Value!.Id, item.Id, SpPreviewLimitBytes);
        }

        var ms = (DateTime.UtcNow - started).TotalMilliseconds;
        var fileName = asPdf ? Path.ChangeExtension(item.Name, ".pdf") : item.Name;
        var mime = asPdf ? "application/pdf" : item.MimeType ?? "application/octet-stream";
        var isText = !asPdf && (mime.StartsWith("text/", StringComparison.Ordinal) || mime == "application/json" || SpTextExtensions.Contains(Path.GetExtension(item.Name).ToLowerInvariant()));
        string? text = null;

        if (isText)
        {
            text = Encoding.UTF8.GetString(bytes);

            if (text.Length > SpTextPreviewChars)
            {
                text = text[..SpTextPreviewChars] + $"\n… ({text.Length - SpTextPreviewChars} more characters)";
            }
        }

        var note = asPdf
            ? $"Converted to PDF by Microsoft: {SpSize(bytes.Length)}, starts with {(bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8) ? "%PDF-" : "something that is not a PDF")}, {ms:0} ms"
            : $"{SpSize(bytes.Length)} {mime}, downloaded in {ms:0} ms";

        _spPreview.Value = new SpPreview(item, text, bytes, mime, fileName, note);
        return $"PASS {(asPdf ? "converted" : "read")} {item.Name} ({SpSize(bytes.Length)})";
    }

    private async Task<string> ReadSharePointChangesAsync(bool fromNow)
    {
        var library = _spLibrary.Value!;
        var (_, oneDrive) = SpClients();
        string? deltaLink = null;
        var first = fromNow || !_spDeltaLinks.TryGetValue(library.Id, out deltaLink);
        var options = new DriveDeltaOptions(FromNow: fromNow, IncludeSharingChanges: _spSharingChanges.Value, MaxPages: 20);
        DriveDelta delta;
        bool capped;

        try
        {
            (delta, capped) = await ReadSharePointDeltaAsync(oneDrive, library.Id, deltaLink, options);
        }
        catch (ConnectorException ex) when (ex.StatusCode == 410)
        {
            (delta, capped) = await ReadSharePointDeltaAsync(oneDrive, library.Id, null, options with { FromNow = false });
            first = true;
        }

        _spDeltaLinks[library.Id] = delta.DeltaLink;
        var more = capped ? " — more pages to read; check again to continue" : "";
        var items = delta.Items.Where(i => i.Name != "root").ToList();
        _spChanges.ReplaceAll(items);
        _spChangesSummary.Value = fromNow
            ? "Watching from now on: change something in SharePoint, then Check for changes"
            : first
                ? $"Read the whole library: {items.Count} items{more}. Change something and check again."
                : $"{items.Count} changed since the last check ({items.Count(i => i.Deleted)} removed, {items.Count(i => i.SharingChanged)} with sharing changes){more}";

        return $"PASS {(fromNow ? "started from now" : first ? "read the library" : "read the changes")}: {items.Count} items{more}";
    }

    // A large library takes more pages than one check reads; the capped read's next-page link is a
    // delta link like any other, so the next check continues from it instead of starting over.
    private static async Task<(DriveDelta Delta, bool Capped)> ReadSharePointDeltaAsync(OneDrive oneDrive, string driveId, string? deltaLink, DriveDeltaOptions options)
    {
        try
        {
            return (await oneDrive.DeltaAsync(driveId, deltaLink, options), false);
        }
        catch (ConnectorPageCapException<DriveItem> cap) when (cap.ResumeFrom is not null)
        {
            return (new DriveDelta(cap.Items, cap.ResumeFrom), true);
        }
    }

    private static string? SpField(ListItem item, string name)
    {
        return item.Fields.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static string SpSize(long? bytes)
    {
        return bytes switch
        {
            null => "?",
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KiB",
            _ => $"{bytes / (1024.0 * 1024):0.#} MiB",
        };
    }

    private static string SpStatusColor(string status)
    {
        return status.StartsWith("PASS", StringComparison.Ordinal) ? "text-success-primary"
            : status.StartsWith("FAIL", StringComparison.Ordinal) ? "text-error-primary"
            : status.StartsWith("SKIP", StringComparison.Ordinal) ? "text-warning-primary"
            : "text-secondary";
    }
}
