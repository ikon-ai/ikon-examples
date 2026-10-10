using System.Security.Cryptography.X509Certificates;
using Ikon.Connectors;
using Ikon.Connectors.Microsoft;

namespace Ikon.App.Patterns.Examples;

file sealed class MicrosoftConnectorGuideAppExamples(IApp<SessionIdentity, ClientParameters> app)
{
    public SharePoint MicrosoftCertificateFromSecrets()
    {
        #region example:connectors-microsoft-certificate
        app.Secrets.TryGet("MICROSOFT_CLIENT_CERTIFICATE_PASSWORD", out var password);

        var certificate = X509CertificateLoader.LoadPkcs12(
            Convert.FromBase64String(app.Secrets["MICROSOFT_CLIENT_CERTIFICATE"]), password);

        var tokens = new MicrosoftTokenProvider(new MicrosoftCertificateCredentials(
            app.Secrets["MICROSOFT_TENANT_ID"], app.Secrets["MICROSOFT_CLIENT_ID"], certificate));
        var sharePoint = new SharePoint(tokens);
        #endregion

        return sharePoint;
    }
}

file sealed class MicrosoftConnectorGuideExamples
{
    private static Task SaveRefreshTokenAsync(string refreshToken, CancellationToken ct) => Task.CompletedTask;

    private static Task IngestAsync(DriveItem item, Stream content) => Task.CompletedTask;

    private static Task IndexAsync(DriveItem item, byte[] content, IReadOnlyList<DriveItemGrant> grants) => Task.CompletedTask;

    private static Task IndexAsync(string itemId, IReadOnlyDictionary<string, System.Text.Json.JsonElement> fields) => Task.CompletedTask;

    private static Task RemoveAsync(string fileId) => Task.CompletedTask;

    private static Task ForgetAsync(string itemId) => Task.CompletedTask;

    private static Task SaveDeltaLinkAsync(string deltaLink) => Task.CompletedTask;

    private static Task KeepAsync(string name, Stream content) => Task.CompletedTask;

    private static Task RememberSentAsync(string internetMessageId) => Task.CompletedTask;

    private static void QueueReauthorize(string subscriptionId)
    {
    }

    private static void QueueDeltaRead(string subscriptionId)
    {
    }

    public void MicrosoftAppClients(string tenantId, string clientId, string clientSecret)
    {
        #region example:connectors-microsoft-app
        var tokens = new MicrosoftTokenProvider(new MicrosoftAppCredentials(tenantId, clientId, clientSecret));
        var sharePoint = new SharePoint(tokens);
        var oneDrive = new OneDrive(tokens);
        var directory = new EntraDirectory(tokens);
        #endregion

        Log.Instance.Debug($"{sharePoint} {oneDrive} {directory}");
    }

    public async Task MicrosoftUserClientsAsync(string clientId, string clientSecret, string redirectUri, string state, string code)
    {
        #region example:connectors-microsoft-user
        string[] scopes = ["Sites.ReadWrite.All"];
        var signInUrl = MicrosoftAuth.AuthorizeUrl("organizations", clientId, redirectUri, scopes, state);

        // ... the person signs in; the redirect back carries `code`:
        var credentials = await MicrosoftAuth.ExchangeCodeAsync("organizations", clientId, clientSecret, code, redirectUri, scopes);
        await SaveRefreshTokenAsync(credentials.RefreshToken, CancellationToken.None);

        var tokens = new MicrosoftTokenProvider(credentials, onRefreshTokenRotated: SaveRefreshTokenAsync);
        var sharePoint = new SharePoint(tokens);
        #endregion

        Log.Instance.Debug($"{signInUrl} {sharePoint}");
    }

    public async Task SharePointFilesAsync(SharePoint sharePoint, OneDrive oneDrive)
    {
        #region example:connectors-sharepoint-files
        var site = await sharePoint.GetSiteAsync("https://contoso.sharepoint.com/sites/Engineering");

        foreach (var library in await sharePoint.ListLibrariesAsync(site.Id))
        {
            foreach (var item in await oneDrive.ListChildrenAsync(library.Id))
            {
                if (item.IsFolder)
                {
                    continue;
                }

                await using var content = item.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)
                    ? await oneDrive.DownloadAsPdfAsync(library.Id, item.Id)
                    : await oneDrive.DownloadAsync(library.Id, item.Id);

                await IngestAsync(item, content);
            }
        }
        #endregion
    }

    public async Task OutlookDeltaAsync(Outlook outlook, string? deltaLink)
    {
        #region example:connectors-outlook-delta
        var delta = await outlook.MessagesDeltaAsync("ada@contoso.com", "inbox", deltaLink);

        foreach (var message in delta.Items.Where(m => !m.Deleted))
        {
            Log.Instance.Info($"{message.From?.Address}: {message.Subject}");
        }

        deltaLink = delta.DeltaLink;   // store it for the next call
        #endregion
    }

    public async Task OutlookReadAsync(Outlook outlook, OutlookMessage message)
    {
        #region example:connectors-outlook-read
        var thread = await outlook.ListConversationAsync("ada@contoso.com", message.ConversationId!);

        foreach (var earlier in thread)
        {
            Log.Instance.Info($"{earlier.SentAt}: {earlier.From?.Address} {earlier.Subject}");
        }

        await using var mime = await outlook.DownloadMessageAsync("ada@contoso.com", message.Id);
        await KeepAsync($"{message.InternetMessageId}.eml", mime);
        #endregion
    }

    public async Task OutlookSendAsync(Outlook outlook)
    {
        #region example:connectors-outlook-send
        await using var ledger = File.OpenRead("./ledger.pdf");

        var sent = await outlook.SendAsync("ada@contoso.com", new NewOutlookMessage("Documents for case RTD-11")
        {
            To = ["Ben Bitdiddle <ben@acme.com>"],
            Bcc = ["case-rtd11@mail.example.com"],
            Html = "<p>Please find the ledger attached.</p>",
            Attachments = [new NewOutlookAttachment("ledger.pdf", "application/pdf", ledger, ledger.Length)],
        });

        await RememberSentAsync(sent.InternetMessageId);   // a reply names it in In-Reply-To
        #endregion
    }

    public async Task OutlookAttachmentsAsync(Outlook outlook, OutlookMessage message)
    {
        #region example:connectors-outlook-attachments
        foreach (var attachment in await outlook.ListAttachmentsAsync("ada@contoso.com", message.Id))
        {
            if (attachment.Kind == OutlookAttachmentKind.Reference || attachment.IsInline)
            {
                continue;
            }

            await using var content = await outlook.DownloadAttachmentAsync("ada@contoso.com", message.Id, attachment.Id);
            await KeepAsync(attachment.Name, content);
        }
        #endregion
    }

    public async Task OutlookOrganiseAsync(Outlook outlook, IReadOnlyList<string> selectedIds)
    {
        #region example:connectors-outlook-organise
        await outlook.UpdateMessagesAsync("ada@contoso.com", selectedIds, isRead: true, categories: ["Case RTD-11"]);
        await outlook.MoveMessagesAsync("ada@contoso.com", selectedIds, "archive");
        #endregion
    }

    public async Task<GraphSubscription> OutlookSubscribeAsync(GraphSubscriptions subscriptions, string mailboxObjectId, string notificationUrl, string clientState)
    {
        #region example:connectors-outlook-subscribe
        var subscription = await subscriptions.CreateAsync(
            SubscriptionResource.Messages(mailboxObjectId), notificationUrl, clientState, lifecycleNotificationUrl: notificationUrl);

        // Later, before it ends: renewing from the record keeps within the mail maximum.
        subscription = await subscriptions.RenewAsync(subscription);
        #endregion

        return subscription;
    }

    public async Task SharePointUploadAsync(OneDrive oneDrive, string driveId, string folderId)
    {
        #region example:connectors-sharepoint-upload
        await using var report = File.OpenRead("./report.pdf");
        var uploaded = await oneDrive.UploadAsync(driveId, folderId, "report.pdf", report, report.Length, UploadConflict.Replace);
        #endregion

        Log.Instance.Debug($"{uploaded.WebUrl}");
    }

    public async Task SharePointDeltaAsync(OneDrive oneDrive, string driveId, string? deltaLink)
    {
        #region example:connectors-sharepoint-delta
        DriveDelta delta;

        try
        {
            delta = await oneDrive.DeltaAsync(driveId, deltaLink);
        }
        catch (ConnectorException ex) when (ex.IsResyncRequired)
        {
            delta = await oneDrive.DeltaAsync(driveId);   // the link expired: read everything again
        }

        foreach (var item in delta.Items.Where(i => !i.IsFolder))
        {
            Log.Instance.Info(item.Deleted ? $"removed {item.Id}" : $"changed {item.Name}");
        }

        deltaLink = delta.DeltaLink;   // store it for the next call
        #endregion
    }

    public async Task SharePointGrantsAsync(OneDrive oneDrive, string driveId, string? deltaLink)
    {
        #region example:connectors-sharepoint-grants
        var delta = await oneDrive.DeltaAsync(driveId, deltaLink);

        foreach (var item in delta.Items.Where(i => !i.IsFolder))
        {
            if (item.Deleted)
            {
                await RemoveAsync(item.Id);
                continue;
            }

            var grants = await oneDrive.ListPermissionsAsync(driveId, item.Id);
            var content = await oneDrive.DownloadBytesAsync(driveId, item.Id, maxBytes: 20_000_000);
            await IndexAsync(item, content, grants);
        }
        #endregion
    }

    public async Task<bool> SharePointTrimAsync(EntraDirectory directory, string signedInEmail, IReadOnlyList<DriveItemGrant> grants)
    {
        #region example:connectors-sharepoint-trim
        var user = await directory.FindUserAsync(signedInEmail);

        if (user is null)
        {
            return false;   // not in this tenant, so no grant can name them
        }

        var groupIds = (await directory.ListUserGroupsAsync(user.Id))
            .Select(group => group.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mayRead = grants.Any(grant =>
            grant.Roles.Any(role => role is "read" or "write" or "owner")
            && grant.PrincipalId is { } principalId
            && grant.Kind switch
            {
                GrantPrincipalKind.User => string.Equals(principalId, user.Id, StringComparison.OrdinalIgnoreCase),
                GrantPrincipalKind.Group => groupIds.Contains(principalId),
                _ => false,   // site groups, links and unknown principals name nobody this app can check
            });
        #endregion

        return mayRead;
    }

    public async Task SharePointListsAsync(SharePoint sharePoint, string siteId, string listId)
    {
        #region example:connectors-sharepoint-lists
        var columns = await sharePoint.ListColumnsAsync(siteId, listId);
        var open = await sharePoint.ListItemsAsync(siteId, listId, filter: "fields/Status eq 'Open'");

        var created = await sharePoint.CreateItemAsync(siteId, listId, new Dictionary<string, object?>
        {
            ["Title"] = "Renew the support contract",
            ["Status"] = "Open",
        });

        try
        {
            await sharePoint.UpdateItemFieldsAsync(siteId, listId, created.Id,
                new Dictionary<string, object?> { ["Status"] = "Done" }, eTag: created.ETag);
        }
        catch (ConnectorException ex) when (ex.StatusCode == 412)
        {
            // Someone changed the item since it was read: read it again before deciding.
        }
        #endregion

        Log.Instance.Debug($"{columns.Count} {open.Count}");
    }

    public async Task SharePointAuthoringAsync(SharePoint sharePoint, string siteId, string projectsListId)
    {
        #region example:connectors-sharepoint-authoring
        var tasks = await sharePoint.CreateListAsync(siteId, "Launch tasks", columns:
        [
            new ColumnSpec("Status", ColumnKind.Choice, Choices: ["Open", "Done"], Indexed: true),
            new ColumnSpec("Due", ColumnKind.DateOnly, DisplayName: "Due date"),
            new ColumnSpec("Owner", ColumnKind.PersonOrGroup),
            new ColumnSpec("Project", ColumnKind.Lookup, LookupListId: projectsListId),
        ]);

        var owner = await sharePoint.EnsureSiteUserAsync(siteId, "alex@contoso.com");
        var fields = new Dictionary<string, object?> { ["Title"] = "Book the venue", ["Status"] = "Open", ["Due"] = "2026-11-02" };
        fields.SetLookup("Owner", owner).SetLookup("Project", 3);

        await sharePoint.CreateItemAsync(siteId, tasks.Id, fields);

        var overdue = await sharePoint.ListItemsAsync(siteId, tasks.Id,
            new ListItemQuery("fields/Status eq 'Open'", Select: ["Title", "Due"], OrderBy: "fields/Due"));
        #endregion

        Log.Instance.Debug($"{overdue.Count}");
    }

    public async Task<string> SubscribeAsync(GraphSubscriptions subscriptions, string driveId, string notificationUrl, string clientState)
    {
        #region example:connectors-graph-subscribe
        var subscription = await subscriptions.CreateAsync(
            SubscriptionResource.DriveRoot(driveId), notificationUrl, clientState, lifecycleNotificationUrl: notificationUrl);

        // Renew before it ends; RenewAsync extends it to the longest Graph allows.
        var renewAt = subscription.ExpiresAt - GraphSubscriptions.MaxLifetime / 3;
        #endregion

        return $"{subscription.Id} {renewAt}";
    }

    public HttpResult ReceiveNotification(HttpRequest request, string clientState)
    {
        #region example:connectors-graph-notifications
        if (GraphNotifications.ValidationToken(request.Query) is { } token)
        {
            return new HttpResult(200, token, "text/plain");   // Graph checking the URL
        }

        foreach (var notification in GraphNotifications.Parse(request.Body, clientState))
        {
            if (notification.LifecycleEvent == "reauthorizationRequired")
            {
                QueueReauthorize(notification.SubscriptionId);
            }
            else
            {
                QueueDeltaRead(notification.SubscriptionId);   // answer first, read the changes after
            }
        }

        return new HttpResult(202, "", "text/plain");
        #endregion
    }

    public async Task SharePointListChangesAsync(SharePoint sharePoint, string siteId, string listId, string? storedLink)
    {
        #region example:connectors-sharepoint-list-delta
        var delta = await sharePoint.ItemsDeltaAsync(siteId, listId, storedLink);

        foreach (var item in delta.Items)
        {
            if (item.Deleted)
            {
                await ForgetAsync(item.Id);
            }
            else
            {
                await IndexAsync(item.Id, item.Fields);
            }
        }

        await SaveDeltaLinkAsync(delta.DeltaLink);
        #endregion
    }
}
