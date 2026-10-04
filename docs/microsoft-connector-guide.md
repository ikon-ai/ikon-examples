# Microsoft Connector Guide
<!-- checked-against: 6fb16b4feea11d5b -->
This guide covers `Ikon.Connectors.Microsoft` — SharePoint, OneDrive, the Entra directory and Outlook mail through Microsoft Graph — for app developers wiring Microsoft 365 into an Ikon app.

## Microsoft: SharePoint and OneDrive

`Ikon.Connectors.Microsoft` reaches Microsoft 365 files and lists through Microsoft Graph. `SharePoint` covers sites, their document libraries and their lists. `OneDrive` covers the files in any drive, and in Graph a SharePoint document library *is* a drive: the same file calls serve a site's libraries and a person's own OneDrive, keyed by the library's `Id`. `EntraDirectory` finds people and their groups, for checking a person against the grants on a file. `Outlook` reads a mailbox.

### Setting it up

An administrator of the customer's Microsoft 365 tenant does this once, in the Microsoft Entra admin center under App registrations:

1. **Register the app** and note its tenant id and client id.
2. **Give it a credential.** For app-only access, a certificate: upload its public part (`.cer`) to the registration and keep the `.pfx` for the app. A client secret serves delegated access, and app-only calls that stay on Graph.
3. **Add API permissions and grant admin consent**, by whom the app acts as. The SharePoint permissions are listed under SharePoint, not Microsoft Graph, and only the calls whose remarks say they use SharePoint's own REST API need them:

| | App-only, on the sites granted to it | Delegated, as the person signed in |
|---|---|---|
| Microsoft Graph | application `Sites.Selected` | delegated `Sites.ReadWrite.All` (or `Sites.Read.All`) |
| SharePoint | application `Sites.Selected` | delegated `AllSites.Write` (or `AllSites.Read`) |
| Then | grant the app each site (step 4) | add your app's `[HttpGet]` sign-in endpoint as a Web redirect URI |

4. **Grant the app each site it may use**, with the least role that covers what it does: `read` to browse and download, `write` to upload and edit items, `owner` to create lists and columns, `fullcontrol` for content types and to change who has access. Until then the app gets `403` on the site. The grant is made with `Sites.FullControl.All`: by `SharePoint.GrantAppAccessAsync` from the administrator's own tool, or without code through Graph's `POST /sites/{site-id}/permissions` (Graph Explorer) or PnP PowerShell's site permission commands.

Some features need more, named where they are described: Microsoft Search needs `Files.Read.All` or `Sites.Read.All`, checking people against a file's grants needs `User.Read.All`, reporting sharing changes needs `Sites.FullControl.All`, and change notifications need your app reachable on a public https URL.

The app takes the credential from its secrets — set each with `ikon secret set`, the `.pfx` as base64 (`[Convert]::ToBase64String([IO.File]::ReadAllBytes("app.pfx"))` in PowerShell, `base64 -w0 app.pfx` elsewhere) — and loads the certificate with `X509CertificateLoader` from `System.Security.Cryptography.X509Certificates`:

<!-- ikon-example: connectors-microsoft-certificate -->
```csharp
app.Secrets.TryGet("MICROSOFT_CLIENT_CERTIFICATE_PASSWORD", out var password);

var certificate = X509CertificateLoader.LoadPkcs12(
    Convert.FromBase64String(app.Secrets["MICROSOFT_CLIENT_CERTIFICATE"]), password);

var tokens = new MicrosoftTokenProvider(new MicrosoftCertificateCredentials(
    app.Secrets["MICROSOFT_TENANT_ID"], app.Secrets["MICROSOFT_CLIENT_ID"], certificate));
var sharePoint = new SharePoint(tokens);
```

### Credentials

Every kind of `MicrosoftCredentials` starts from that app registration. They differ in who the app acts as:

| | `MicrosoftAppCredentials` — app-only | `MicrosoftUserCredentials` — delegated |
|---|---|---|
| Acts as | the app itself, across the tenant or the sites granted to it | the person who signed in, seeing only what they can see |
| Consent | a tenant admin, once (`MicrosoftAuth.AdminConsentUrl`) | each person at sign-in (`MicrosoftAuth.AuthorizeUrl`) |
| Graph permissions (application / delegated) | `Sites.Selected`, then an admin grants each site a role (above); or `Sites.Read.All` / `Sites.ReadWrite.All`; `Files.Read.All` / `Files.ReadWrite.All` for people's OneDrives | `Sites.Read.All` / `Sites.ReadWrite.All`, `Files.Read.All` / `Files.ReadWrite.All` |
| Writes are attributed to | the app | the person |

App-only access can prove the app's identity with a certificate instead of a secret: `MicrosoftCertificateCredentials` takes an `X509Certificate2` holding an RSA private key, whose public part is uploaded to the app registration. Microsoft recommends it over a secret, and it is the only app-only credential SharePoint's own REST API accepts, so the `SharePoint` calls built on that API need it (or delegated credentials); their remarks say which they are.

App-only needs the customer's tenant id (or a verified domain such as `contoso.onmicrosoft.com`). Share one `MicrosoftTokenProvider` between the clients of a credential, so they share its access tokens:

<!-- ikon-example: connectors-microsoft-app -->
```csharp
var tokens = new MicrosoftTokenProvider(new MicrosoftAppCredentials(tenantId, clientId, clientSecret));
var sharePoint = new SharePoint(tokens);
var oneDrive = new OneDrive(tokens);
var directory = new EntraDirectory(tokens);
```

Delegated access is an OAuth sign-in your app runs: send the person to `AuthorizeUrl`, receive the `code` at your redirect endpoint (an `[HttpGet]` endpoint — see the endpoints guide), and redeem it with `ExchangeCodeAsync`. `PkceCodes.Create()` (from `Ikon.Connectors`) adds PKCE: pass the `Challenge` of the `PkceCodes` it returns to `AuthorizeUrl` and keep its `Verifier` with the sign-in's state for `ExchangeCodeAsync`, so a code intercepted on its way back is useless to whoever took it. `offline_access` is always requested, so the result carries a refresh token. **Entra issues a new refresh token every time it refreshes**; keep the newest one, through `onRefreshTokenRotated`, or the stored one expires on its own lifetime however often it was used:

<!-- ikon-example: connectors-microsoft-user -->
```csharp
string[] scopes = ["Sites.ReadWrite.All"];
var signInUrl = MicrosoftAuth.AuthorizeUrl("organizations", clientId, redirectUri, scopes, state);

// ... the person signs in; the redirect back carries `code`:
var credentials = await MicrosoftAuth.ExchangeCodeAsync("organizations", clientId, clientSecret, code, redirectUri, scopes);
await SaveRefreshTokenAsync(credentials.RefreshToken, CancellationToken.None);

var tokens = new MicrosoftTokenProvider(credentials, onRefreshTokenRotated: SaveRefreshTokenAsync);
var sharePoint = new SharePoint(tokens);
```

`GetGrantedPermissionsAsync` returns a `TokenPermissions` read from the current token — the application permissions an admin consented, or the delegated scopes a person granted: an app-only token with no `ApplicationPermissions` is the usual cause of Graph answering every call with a bare `401`, and seeing that is quicker than guessing at consent.

Every failure is a `ConnectorException` (from `Ikon.Connectors`) with the provider `"microsoft"`, sign-in, SharePoint and OneDrive alike, and its message names the operation that failed. A credential Entra rejects for good — a revoked or expired refresh token, a wrong or expired client secret or certificate, consent never given — throws it with `StatusCode` 401, so `IsReconnectRequired` is true: have the person sign in again, or the administrator fix the registration, rather than retry. A `403` from Graph means the token is valid but lacks the permission or the site grant. Throttled calls (`429`, `503`, and `504` on a read) are retried three times on Graph's `Retry-After`, bounded at two minutes, before they surface; `IsTransient` marks one that still did. A `401` from Graph is retried once with a fresh token, so one that surfaces means the credential itself is refused. A download or upload host refusing its pre-authenticated URL throws with no `StatusCode`: the URL expired, and the credential is fine. A listing past its `maxPages` throws `ConnectorPageCapException<T>` with the items read and the link to continue from as `ResumeFrom`, never a shortened list.

### Sites, libraries and files

`GetSiteAsync` takes the site's URL as people copy it from the browser, the `host:/sites/path` form, a composite site id, or `root`, and returns a `SharePointSite` whose `Id` every other site call takes. `ListLibrariesAsync` returns a `DocumentLibrary` for every document library of the site, not only the default "Documents" one; `OneDrive.GetUserDriveAsync` returns one for a person's OneDrive. `DownloadAsync` streams a file; `DownloadAsPdfAsync` has Microsoft convert a Word, Excel, PowerPoint or other office file to PDF first, which is how to read one without an Office parser:

<!-- ikon-example: connectors-sharepoint-files -->
```csharp
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
```

`ListChildrenAsync` lists one folder (the root when no folder id is given); `GetItemByPathAsync` finds an item by its path in the drive, and `SearchAsync` searches names and content through Microsoft's search index, so a file uploaded a moment ago may not be found yet. App-only search needs `Files.Read.All`, `Sites.Read.All` or a ReadWrite form of either: Microsoft documents that it does not support `Sites.Selected`, and Graph answers it with a `500` rather than a `403`. A listing past its `maxPages` throws `ConnectorPageCapException<DriveItem>` with the items read, never a shortened list.

`UploadAsync` needs the content's exact size: up to 4 MiB is one request, anything larger goes through an upload session in 10 MiB ranges, and a range the upload host fails is sent again where the session says it still waits for it. `UploadConflict` decides what happens when the name is taken — `Fail` (the default, `409`), `Replace` or `Rename`. `CreateFolderAsync`, `RenameAsync`, `MoveAsync` (within one drive) and `DeleteAsync` (to the recycle bin) cover the rest:

<!-- ikon-example: connectors-sharepoint-upload -->
```csharp
await using var report = File.OpenRead("./report.pdf");
var uploaded = await oneDrive.UploadAsync(driveId, folderId, "report.pdf", report, report.Length, UploadConflict.Replace);
```

### Sharing, versions and copies

`CreateSharingLinkAsync` makes a `SharingLink` of a `SharingLinkType` (`View` or `Edit`) and a `SharingLinkScope` (`Anonymous`, `Organization` or `Users`; the tenant's default kind when omitted), optionally expiring; asking again for the same type and scope returns the same link. `InviteAsync` gives named people `read` or `write` access, silently unless `sendInvitation` is set. `UpdatePermissionRolesAsync` and `RemovePermissionAsync` change or withdraw a permission set on the item itself by its id — a link's `PermissionId` or a grant's. `ResolveSharingUrlAsync` turns a sharing link, or any SharePoint or OneDrive URL to a file, into its `DriveItem`, whose `DriveId` every other call takes.

`ListVersionsAsync` returns a file's `DriveItemVersion`s, newest first; `DownloadVersionAsync` streams one and `RestoreVersionAsync` makes it current again as a new version. `CopyAsync` copies a file or folder within a drive or into another one, waits for SharePoint to finish in the background and returns the copy — moving between libraries is a copy then a `DeleteAsync`. `CheckOutAsync`, `CheckInAsync` and `DiscardCheckOutAsync` work in libraries that use check-out; `GetItemAsync` reports `CheckedOut`. A library file's own columns are its list item's fields: `GetItemFieldsAsync` reads them and `UpdateItemFieldsAsync` writes them, with an optional `ETag` as for list items, and `SharePointIds` names the list item for the `SharePoint` calls. `GetThumbnailAsync` streams a rendered image of the file, `CreatePreviewAsync` returns a short-lived viewer URL for an iframe — it acts with the caller's own access, so never hand one made with app-only read-write credentials to someone who should only read — and `PermanentDeleteAsync` skips the recycle bin.

Labels from Microsoft Purview are there too: `ExtractSensitivityLabelsAsync` returns the `SensitivityLabelAssignment`s a file carries, `AssignSensitivityLabelAsync` applies one (a metered Microsoft API that needs Azure billing set up for the app), and `GetRetentionLabelAsync`, `SetRetentionLabelAsync` and `RemoveRetentionLabelAsync` work with a retention label a Purview policy publishes to the site.

### Finding sites, and granting an app access to one

`SearchSitesAsync` finds sites by name for either kind of credential. App-only credentials can also enumerate the whole tenant with `ListAllSitesAsync`, or follow it with `SitesDeltaAsync`, whose `SiteDelta` reports created, changed and removed sites (`Deleted` set) and the `DeltaLink` to store, exactly as the drive delta below does; `fromNow: true` skips the first full read. Delegated credentials have `ListFollowedSitesAsync`, the sites the signed-in person follows. `ListSubsitesAsync` lists a site's direct subsites. `SharePointSite` carries the site collection's `HostName` and the `DataLocationCode` of the geography its content is stored in, where Graph names them.

Under `Sites.Selected` an app sees only what it has been granted. `GrantAppAccessAsync` grants an app `read`, `write`, `owner` or `fullcontrol` on a `SharePointTarget` — a whole site (`SharePointTarget.Site`), one list (`SharePointTarget.List`, for `Lists.SelectedOperations.Selected`) or one item, file or folder (`SharePointTarget.Item`, for `ListItems.SelectedOperations.Selected`). It is an admin's action: the calling credential needs `Sites.FullControl.All` (or, for a list or item, owner rights on the site), and a list or item grant breaks the permission inheritance of what it is set on. `ListAppGrantsAsync`, `UpdateAppGrantAsync` and `RevokeAppGrantAsync` read and change grants by their `PermissionId`.

`CreateSiteAsync` takes a `NewSite` — title, URL, owner and `SiteTemplate` — and creates a communication site or a team site without a Microsoft 365 group, waits for SharePoint to provision it and returns it. It goes through SharePoint's own REST API, so app-only use needs `MicrosoftCertificateCredentials`; a URL already taken throws with `StatusCode` `409`.

### Reading only what changed

`DeltaAsync` returns every item of a drive on the first call and only the changes on the next, keyed by the `DeltaLink` it hands back — store it per drive. The delta feed is the one listing Graph guarantees complete while people keep editing. Each item appears once, in its latest state, folders and the drive root included, with its `Path` inside the drive (`/Policies/ISMS scope.docx`) and `LastModifiedBy`. Removed items arrive with `Deleted` set and little but their `Id`. Key what you store on `Id`: a folder that is moved or renamed does not bring the items under it back through the feed, so a stored `Path` goes stale until the next full read. A link Graph no longer honours fails with `IsResyncRequired` set (`StatusCode` `410`): start again without one. Past `maxPages` (default 50) it throws `ConnectorPageCapException<DriveItem>` whose `ResumeFrom` is passed back as the delta link to continue. The overload taking `DriveDeltaOptions` adds two things: `FromNow` starts watching without the first full read, and `IncludeSharingChanges` brings back items whose access changed, with `SharingChanged` set (below).

<!-- ikon-example: connectors-sharepoint-delta -->
```csharp
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
```

### Being told when something changed

Polling delta works, but Graph can also call your app when a library, OneDrive or list changes. `GraphSubscriptions.CreateAsync` subscribes a `SubscriptionResource` — a drive's root (`SubscriptionResource.DriveRoot`; Graph does not watch one folder) or a list (`SubscriptionResource.List`) — to your app's public https endpoint, an `[HttpPost]` endpoint with `Auth = EndpointAuth.Public` (see the endpoints guide), and returns a `GraphSubscription`. Graph checks the URL on the spot by calling it with a token to echo, so the endpoint must be up before you subscribe. A subscription lives at most `GraphSubscriptions.MaxLifetime` (just under 30 days): `RenewAsync` extends it, `ReauthorizeAsync` answers Graph's `reauthorizationRequired`, and `DeleteAsync` and `ListAsync` manage the rest:

<!-- ikon-example: connectors-graph-subscribe -->
```csharp
var subscription = await subscriptions.CreateAsync(
    SubscriptionResource.DriveRoot(driveId), notificationUrl, clientState, lifecycleNotificationUrl: notificationUrl);

// Renew before it ends; RenewAsync extends it to the longest Graph allows.
var renewAt = subscription.ExpiresAt - GraphSubscriptions.MaxLifetime / 3;
```

`GraphNotifications` reads what arrives. `ValidationToken` returns the token of Graph's URL check, to send back as plain text. `Parse` returns the `GraphNotification`s in a delivery, dropping any whose `clientState` is not the secret you subscribed with, so a forged request changes nothing. A notification says only that something changed: answer within three seconds — Graph slows, then stops, deliveries to an endpoint that takes longer — and read the change afterwards with `DeltaAsync` or `ItemsDeltaAsync`. Delivery is usually under a minute but can take hours, so keep a slow delta poll behind it:

<!-- ikon-example: connectors-graph-notifications -->
```csharp
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
```

### Who may read a file

App-only credentials read whatever the app registration's permissions admit, so Graph trims nothing per person. To show SharePoint content only to the people SharePoint would show it to, an app reads each file's grants and enforces them itself. A permission changed in SharePoint then applies in your app only once you have read that file's grants again, and a grant reported as unresolved (below) names nobody you can check, so it must admit nobody.

The directory calls need Microsoft Graph application permissions of their own, beside the site ones above:

| Permission | What needs it |
|---|---|
| `User.Read.All` | `EntraDirectory.FindUserAsync` and `EntraDirectory.ListUserGroupsAsync`. |
| `GroupMember.Read.All` | The names of the groups `ListUserGroupsAsync` returns. Without it Graph returns each group's id and an empty `DisplayName`; the ids are all that trimming needs. |

Microsoft's guidance for scanning libraries at scale says an app needs `Sites.FullControl.All` "to process permissions correctly", while the reference for listing a file's permissions names only read permissions: if `ListPermissionsAsync` returns fewer grants than SharePoint shows for a file, settle with the tenant's administrator whether that is the permission to add.

<!-- ikon-example: connectors-sharepoint-grants -->
```csharp
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
```

`DownloadBytesAsync` takes a `maxBytes` and refuses a larger file with a `ConnectorException` instead of buffering it or handing back part of it. `ListPermissionsAsync(driveId, itemId)` returns the item's effective permissions, inherited ones included, as one `DriveItemGrant` per principal. Its `Kind`, a `GrantPrincipalKind`, says what `PrincipalId` is and whether you can check a person against it:

| `Kind` | The grant is to | Checking a person |
|---|---|---|
| `User` | an Entra user | `PrincipalId` equals the `Id` of the `DirectoryUser` from `FindUserAsync` |
| `Group` | an Entra group, security or Microsoft 365 | `PrincipalId` is among the ids from `ListUserGroupsAsync` |
| `SiteGroup` | a SharePoint site group, such as "Finance Members" | **unresolved** |
| `SiteUser` | a principal SharePoint knows and Entra does not; `LoginName` carries its claim | **unresolved** |
| `Link` | whoever holds a sharing link's URL; `LinkScope`, a `SharingLinkScope`, is `Anonymous`, `Organization` or `Unknown` | **unresolved** — the connector cannot know who holds it |
| `Application` | an application | not a person |
| `Unknown` | an invitation nobody has redeemed (`Email` is its address), or a shape the connector does not know | **unresolved** |
| `EveryoneInTenant` | "Everyone except external users": the tenant's members, its guests excluded; only `SharePoint.ResolveGrantsAsync` produces it | a person of the tenant for whom `GetUserTypeAsync` returns `Member` (it needs `User.Read.All`) |
| `Everyone` | "Everyone": every person signed in to the tenant, guests included; only `SharePoint.ResolveGrantsAsync` produces it | any person of the tenant |

A sharing link made for named people is not a `Link` grant: it yields one grant per person named, with `LinkScope` `Users` — a `User` or `Group` grant where Entra knows them, otherwise one of the unresolved kinds. A link that only re-states access people already have yields nothing. `Roles` are Graph's own — `read`, `write` and `owner` all include reading — and `Inherited` is true when Graph named the folder a grant comes from; Graph documents that SharePoint libraries leave that out, so false does not mean the grant is set on the file itself.

<!-- ikon-example: connectors-sharepoint-trim -->
```csharp
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
```

`FindUserAsync` takes an Entra object id, a user principal name, a primary mail address or an alias address, returns null when nobody matches, and throws when more than one person does. `ListUserGroupsAsync` returns every group the person is in, directly or through nested groups, as `DirectoryGroup` records; owning a group is not membership of it.

**A change to who may read is not a change to a file.** New sharing on a folder, a change to the site's permissions, a person joining or leaving a group — by default none of these re-reports the files they affect through `DeltaAsync`. Reading the feed with `DriveDeltaOptions.IncludeSharingChanges` on every read brings back the items whose sharing changed, with `SharingChanged` set, so only those need `ListPermissionsAsync` again; Microsoft documents that the app needs `Sites.FullControl.All` for it to be complete. A person joining or leaving a group changes no item at all: read group membership when a person asks. Without that permission, re-read `ListPermissionsAsync` for every file you hold on the schedule that decides how stale a permission you accept.

**SharePoint site groups need SharePoint's own API.** A site's own permissions are held by its site groups — Owners, Members, Visitors — and a library that inherits them reports its grants as `SiteGroup`. Graph gives an application no call that lists who is in a site group, so on Graph alone a library shared only through its site's groups is readable by nobody in your app. `SharePoint.ResolveGrantsAsync` reads the groups' members through SharePoint's REST API and turns each `SiteGroup` and `SiteUser` grant into the `User`, `Group`, `EveryoneInTenant` or `Everyone` grants it covers, with the original roles and permission id; a group whose membership is hidden, a person Entra does not find, or a Microsoft 365 group's owners (whom no Entra group stands for) keeps an unresolved kind, so trimming stays fail-closed. It needs `MicrosoftCertificateCredentials` (or delegated credentials) and `User.Read.All`. Without them, the dependable arrangement is a library, or its folders, shared with Entra groups.

For a team site connected to a Microsoft 365 group there is one more fact to use. Microsoft documents that the group's owners become site owners and its members site members, and that people can also be added to the site's groups directly. `DocumentLibrary.OwnerGroupId` names that group where Graph reports it — Graph returns it without documenting it, so confirm it by checking that `SharePoint.GetGroupSiteAsync` returns the same site — and `ListUserGroupsAsync` says whether a person is in it. Whether a `SiteGroup` grant called "Finance Members" is that site's members group is something the connector cannot see, and people added to the site directly are in no Entra group, so treating group membership as that grant is your app's stated rule, never the connector's.

### Site groups, role assignments, attachments and the recycle bin

Some of SharePoint is reachable only through its own REST API, never Graph. The calls that use it say so in their remarks, need `MicrosoftCertificateCredentials` or delegated credentials, and throw `InvalidOperationException` before any request when given an app-only secret. `ListSiteGroupsAsync` and `ListSiteGroupMembersAsync` return `SharePointPrincipal`s, each of a `SharePointPrincipalKind` read from its SharePoint claim — a `User` with its `UserPrincipalName`, an `EntraGroup` or `EntraGroupOwners` with the group's `EntraId`, `EveryoneInTenant`, `Everyone` or a `SiteGroup`.

`ListRoleAssignmentsAsync` returns `SharePointRoleAssignments` for a `SharePointTarget` — whether the target has permissions of its own or inherits them, and a `SharePointRoleAssignment` per principal with its `SharePointRoleDefinition`s (the permission levels, from `ListRoleDefinitionsAsync`). `BreakInheritanceAsync` gives a list or item permissions of its own, copied from its parent's unless asked otherwise, `ResetInheritanceAsync` returns it to its parent's, and `AddRoleAssignmentAsync` and `RemoveRoleAssignmentAsync` change who has which level by site user or site group id. A library file is its list item here: its `SharePointIds` give the list and item ids.

`ListAttachmentsAsync`, `AddAttachmentAsync`, `DownloadAttachmentAsync` and `DeleteAttachmentAsync` work with a list item's `ListItemAttachment`s. `ListRecycleBinAsync` returns what was deleted from the site as `RecycleBinItem`s and `RestoreFromRecycleBinAsync` puts one back where it was.

### Lists

`ListListsAsync` returns a `SharePointList` for each of a site's lists, document libraries and hidden system lists included — filter on `Template` and `Hidden`. A `ListItem`'s `Fields` are keyed by each column's **internal** name, which can differ from the name people see (a column shown as "Due date" may be `Due_x0020_date`); each `ListColumn` from `ListColumnsAsync` gives both. `ListItemsAsync` takes an OData filter on fields. A filter on a column SharePoint has not indexed is sent anyway, and may fail on a list of more than 5000 items. Pass an item's `ETag` to `UpdateItemFieldsAsync` or `DeleteItemAsync` to act only on the version you read, and the call fails with `412` when someone changed it in between:

<!-- ikon-example: connectors-sharepoint-lists -->
```csharp
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
```

`ListItemsAsync` also takes a `ListItemQuery`, which adds the columns to return (`Select`), an order (`OrderBy`, reliable only on an indexed column) and the page size. `GetItemAsync` reads one item; `GetListAsync` finds a list by its id or its display name.

Lists, columns and content types can be made as well as read. `CreateListAsync` takes a template (`genericList`, `documentLibrary`, `events`, …) and the columns to start with, each a `ColumnSpec` of a `ColumnKind`; a column the kinds do not cover is described through its `Extra` Graph properties. `CreateColumnAsync`, `UpdateColumnAsync` and `DeleteColumnAsync` work on a list's columns, or on the site's when the list id is null — and `UpdateColumnAsync` with `["indexed"] = true` is how a column becomes filterable in a list past 5000 items. `CreateContentTypeAsync` makes a site `SharePointContentType` from a parent (`0x01` an item, `0x0101` a document), `AddColumnToContentTypeAsync` gives it a site column, `AddContentTypeToListAsync` puts it on a list, and `AddContentTypeFromHubAsync` copies one published from the tenant's content type hub. `UpdateListAsync`, `DeleteListAsync` and `DeleteContentTypeAsync` complete the set.

A lookup or person column is written as the id of what it points to, in the column's `LookupId` field: `SetLookup` and `SetLookups` (the extension methods of `ListFieldValues`) write it in the form Graph expects. A person is an item of the site's own user list, not an Entra object: `EnsureSiteUserAsync` returns that id for an email or sign-in name, adding the person to the site first when they have never visited it, which goes through SharePoint's REST API:

<!-- ikon-example: connectors-sharepoint-authoring -->
```csharp
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
```

`ItemsDeltaAsync` is the drive delta for a list: the first call returns every item with its fields, later ones only what changed since the stored `DeltaLink`, each item once in its latest state and removed ones with `Deleted` set, in a `ListItemDelta`. `fromNow: true` starts watching without the first full read, and `IsResyncRequired` (a `410`) means start again without a link:

<!-- ikon-example: connectors-sharepoint-list-delta -->
```csharp
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
```

`ListItemVersionsAsync` lists an item's `ListItemVersion`s when the list keeps versions, and `RestoreItemVersionAsync` makes one current again as a new version. `UpdateItemFormValuesAsync` writes fields the way SharePoint's own edit form does, from their text, for the columns Graph cannot write — managed metadata above all, as `Label|TermGuid`; it goes through SharePoint's REST API and names every value SharePoint rejected. A managed metadata column's terms come from the term store: `ListTermGroupsAsync`, `ListTermSetsAsync` and `ListTermsAsync` return its `TermGroup`, `TermSet` and `Term` entries, for delegated credentials only — Graph does not open the term store to an app acting as itself.

### Searching everything

`SearchContentAsync` searches SharePoint and OneDrive through Microsoft Search with a `SharePointSearchQuery`: a KQL query string (`budget filetype:xlsx`, `path:"https://contoso.sharepoint.com/sites/Finance"`), the `SearchEntityType`s to look for, the managed properties to return as `Fields`, and a page (`From` up to 1000, `Size` up to 500). Each `SharePointSearchHit` in the `SharePointSearchResults` says what it is and carries the ids the other calls take — site, drive, list and list item — and a `Summary` with matched terms in `<c0>` tags. It needs `Files.Read.All` or `Sites.Read.All` and does not honour `Sites.Selected` grants; an app-only search covers one geography, which the connector reads from the tenant's root site unless `Region` names it, and sees only content shared beyond its owner unless the tenant has built Microsoft's full index for apps. Results lag uploads and edits by minutes. `OneDrive.SearchAsync` is the narrower search of one drive, and works with `Sites.Selected`.

### Pages

`ListPagesAsync` returns a site's modern pages as `SitePageInfo`, news posts included, and `GetPageTextAsync` a page's title and text without markup — what an index of a site's pages needs. `CreatePageAsync` makes a page from `PageSection`s of `PageWebPart`s, each rich text (`PageWebPart.Text`) or one of Microsoft's standard web parts (`PageWebPart.Standard`); a section's `Layout` decides how many columns it has. A new or updated page is a draft until `PublishPageAsync`; `UpdatePageAsync` and `DeletePageAsync` complete the set. Creating and changing pages needs `Sites.ReadWrite.All`.

### Mail

`Outlook` reads a mailbox in Exchange Online. It needs Graph's `Mail.Read` permission: application, for app-only credentials, reaches every mailbox in the tenant unless an Exchange application access policy narrows it; delegated reaches the signed-in person's own, named `me`. `MessagesDeltaAsync(user, folder)` works like `OneDrive.DeltaAsync`: every message of the folder on the first call (`inbox` by default, or another well-known name or folder id; subfolders are not included), then only what changed since the `DeltaLink` of the `MailDelta` it hands back, a removed message with `Deleted` set. A link Graph no longer honours throws with `IsResyncRequired` (a `410`): start again without one. Each `MailMessage` carries the sender's `From` address, `ReceivedAt`, the `Body` as Outlook stores it (`BodyIsHtml` says which), and the `ConversationId` its thread shares. `GetMessageAsync` reads one, null when it is gone:

<!-- ikon-example: connectors-outlook-delta -->
```csharp
var delta = await outlook.MessagesDeltaAsync("ada@contoso.com", "inbox", deltaLink);

foreach (var message in delta.Items.Where(m => !m.Deleted))
{
    Log.Instance.Info($"{message.From}: {message.Subject}");
}

deltaLink = delta.DeltaLink;   // store it for the next call
```

### What it does not reach

The library uses Graph v1.0 and nothing from Graph's beta, so creating a site through `POST /sites`, the site recycle bin in Graph, page templates and archiving sites are not here (`CreateSiteAsync` and `ListRecycleBinAsync` reach the first two through SharePoint's own API). SharePoint Embedded containers, hub sites, site designs and scripts, list views and tenant administration settings are not covered, and change notifications arrive only by webhook, not over Graph's socket.io channel.
