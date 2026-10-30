<!-- checked-against: 6d81feecd5ae66cbcf02cb07 -->

# Microsoft Connector Guide

This guide covers `Ikon.Connectors.Microsoft` — SharePoint, OneDrive, the Entra directory, Outlook mail and Microsoft Teams through Microsoft Graph, and Teams bots through the Bot Connector — for app developers wiring Microsoft 365 into an IKON app.

## Microsoft: SharePoint and OneDrive

`Ikon.Connectors.Microsoft` reaches Microsoft 365 files and lists through Microsoft Graph. `SharePoint` covers sites, their document libraries and their lists. `OneDrive` covers the files in any drive, and in Graph a SharePoint document library *is* a drive: the same file calls serve a site's libraries and a person's own OneDrive, keyed by the library's `Id`. `EntraDirectory` finds people and their groups, for checking a person against the grants on a file. `Outlook` reads, sends and organises mail. `Teams`, `TeamsMessages`, `TeamsMeetings` and `TeamsPresence` reach Microsoft Teams, and `TeamsBot` is a Teams bot that answers as itself.

### Setting it up

An administrator of the customer's Microsoft 365 tenant does this once, in the Microsoft Entra admin center under App registrations:

1. **Register the app** and note its tenant id and client id.
2. **Give it a credential.** For app-only access, a certificate: upload its public part (`.cer`) to the registration and keep the `.pfx` for the app. A client secret serves delegated access, and app-only calls that stay on Graph.
3. **Add API permissions and grant admin consent**, choosing them by who the app acts as. The SharePoint permissions are listed under SharePoint, not Microsoft Graph, and only the calls whose remarks say they use SharePoint's own REST API need them:

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

Delegated access is an OAuth sign-in your app runs: send the person to `AuthorizeUrl`, receive the `code` at your redirect endpoint (an `[HttpGet]` endpoint — see the endpoints guide), and redeem it with `ExchangeCodeAsync`. `PkceCodes.Create()` (from `Ikon.Connectors`) adds PKCE: pass the `Challenge` of the `PkceCodes` it returns to `AuthorizeUrl` and keep its `Verifier` with the sign-in's state for `ExchangeCodeAsync`, so a code intercepted on its way back is useless to whoever took it. `offline_access` is always requested, so the result carries a refresh token. The account chooser is always shown, so the person picks an account rather than getting whichever one the browser is signed in to. **Entra issues a new refresh token every time it refreshes**; keep the newest one, through `onRefreshTokenRotated`, or the stored one expires on its own lifetime however often it was used:

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

`GetGrantedPermissionsAsync` returns a `TokenPermissions` read from the current token: the application permissions an admin consented to, or the delegated scopes a person granted. An app-only token with no `ApplicationPermissions` is the usual reason Graph answers every call with a bare `401`, and this call shows that faster than guessing at consent.

Every failure is a `ConnectorException` (from `Ikon.Connectors`) with the provider `"microsoft"`, sign-in, SharePoint and OneDrive alike, and its message names the operation that failed. A credential Entra rejects for good — a revoked or expired refresh token, a wrong or expired client secret or certificate, consent never given — throws it with `StatusCode` 401, so `IsReconnectRequired` is true: have the person sign in again, or the administrator fix the registration, rather than retry. A `403` from Graph means the token is valid but lacks the permission or the site grant. A `401` from Graph is retried once with a fresh token, so one that surfaces means the credential itself is refused. A download or upload host refusing its pre-authenticated URL throws with no `StatusCode`: the URL expired, and the credential is fine. A successful answer whose body is not the JSON the call expects, such as a proxy's sign-in page, also throws with no `StatusCode`, not with a parser's exception. A listing past its `maxPages` throws `ConnectorPageCapException<T>` with the items read, never a shortened list; only a change feed's cap carries `ResumeFrom`, the delta link to continue from, and for any other listing the answer is a larger `maxPages`.

### Sites, libraries and files

`GetSiteAsync` takes the site's URL (up to the site itself, not a page or library under it), the `host:/sites/path` form, a composite site id, or `root`, and returns a `SharePointSite` whose `Id` every other site call takes. `ListLibrariesAsync` returns a `DocumentLibrary` for every document library of the site, not only the default "Documents" one; `OneDrive.GetUserDriveAsync` returns one for a person's OneDrive. `DownloadAsync` streams a file; `DownloadAsPdfAsync` has Microsoft convert a Word, Excel, PowerPoint or other office file to PDF first, which is how to read one without an Office parser:

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

`UploadAsync` needs the content's exact size: up to 4 MiB is one request, anything larger goes through an upload session in 10 MiB ranges, and a range the upload host fails is sent again where the session says it still waits for it. An upload that fails or is cancelled cancels its session with one request of at most ten seconds, and then surfaces its own failure. The name is a file name, never a path: one holding `/`, `\` or `:` throws `ArgumentException` before any request — pass the folder's id instead. `UploadConflict` decides what happens when the name is taken — `Fail` (the default, `409`), `Replace` or `Rename`. `CreateFolderAsync`, `RenameAsync`, `MoveAsync` (within one drive) and `DeleteAsync` (to the recycle bin) cover the rest:

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

Polling delta works, but Graph can also call your app when a library, OneDrive, list or mailbox changes. `GraphSubscriptions.CreateAsync` subscribes a `SubscriptionResource` — a drive's root (`SubscriptionResource.DriveRoot`; Graph does not watch one folder), a list (`SubscriptionResource.List`) or a mailbox (`SubscriptionResource.Messages`, see "Being told when mail arrives") — to your app's public https endpoint, an `[HttpPost]` endpoint with `Auth = EndpointAuth.Public` (see the endpoints guide), and returns a `GraphSubscription`. Graph checks the URL on the spot by calling it with a token to echo, so the endpoint must be up before you subscribe. A drive or list subscription lives at most `GraphSubscriptions.MaxLifetime` (just under 30 days), a mail one a week: `RenewAsync` extends it, `ReauthorizeAsync` answers Graph's `reauthorizationRequired`, and `DeleteAsync` and `ListAsync` manage the rest:

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

**SharePoint site groups need SharePoint's own API.** A site's own permissions are held by its site groups — Owners, Members, Visitors — and a library that inherits them reports its grants as `SiteGroup`. Graph gives an application no call that lists who is in a site group, so on Graph alone a library shared only through its site's groups is readable by nobody in your app. `SharePoint.ResolveGrantsAsync` reads the groups' members through SharePoint's REST API and turns each `SiteGroup` and `SiteUser` grant into the `User`, `Group`, `EveryoneInTenant`, `Everyone` or `Application` grants it covers, with the original roles and permission id; a member it cannot classify becomes `Unknown`, and a group whose membership is hidden, a person Entra does not find, or a Microsoft 365 group's owners (whom no Entra group stands for) keeps an unresolved kind, so trimming stays fail-closed. It needs `MicrosoftCertificateCredentials` (or delegated credentials) and `User.Read.All`. Without them, the dependable arrangement is a library, or its folders, shared with Entra groups.

For a team site connected to a Microsoft 365 group there is one more fact to use. Microsoft documents that the group's owners become site owners and its members site members, and that people can also be added to the site's groups directly. `DocumentLibrary.OwnerGroupId` names that group where Graph reports it — Graph returns it without documenting it, so confirm it by checking that `SharePoint.GetGroupSiteAsync` returns the same site — and `ListUserGroupsAsync` says whether a person is in it. Whether a `SiteGroup` grant called "Finance Members" is that site's members group is something the connector cannot see, and people added to the site directly are in no Entra group, so treating group membership as that grant is your app's stated rule, never the connector's.

### Site groups, role assignments, attachments and the recycle bin

Some of SharePoint is reachable only through its own REST API, never Graph. The calls that use it say so in their remarks, need `MicrosoftCertificateCredentials` or delegated credentials, and throw `InvalidOperationException` before any request when given an app-only secret. `ListSiteGroupsAsync` and `ListSiteGroupMembersAsync` return `SharePointPrincipal`s, each of a `SharePointPrincipalKind` read from its SharePoint claim — a `User` with its `UserPrincipalName`, an `EntraGroup` or `EntraGroupOwners` with the group's `EntraId`, `EveryoneInTenant`, `Everyone`, an `Application` for an app principal, a `SiteGroup`, or `Unknown` for any other claim.

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

`SearchContentAsync` searches SharePoint and OneDrive through Microsoft Search with a `SharePointSearchQuery`: a KQL query string (`budget filetype:xlsx`, `path:"https://contoso.sharepoint.com/sites/Finance"`), the `SearchEntityType`s to look for, the managed properties to return as `Fields`, and a page (`From` up to 1000, `Size` up to 500). Each `SharePointSearchHit` in the `SharePointSearchResults` says what it is and carries the ids the other calls take — site, drive, list and list item — and a `Summary` with matched terms in `<c0>` tags. It needs `Files.Read.All` or `Sites.Read.All` and does not honour `Sites.Selected` grants; an app-only search covers one geography, which the connector reads from the tenant's root site unless `Region` names it, and sees only content shared beyond its owner unless the tenant has built Microsoft's full index for apps. Results lag uploads and edits by minutes. `OneDrive.SearchAsync` is the narrower search of one drive, and does not work under `Sites.Selected` either.

### Pages

`ListPagesAsync` returns a site's modern pages as `SitePageInfo`, news posts included, and `GetPageTextAsync` a page's title and text without markup — what an index of a site's pages needs. `CreatePageAsync` makes a page from `PageSection`s of `PageWebPart`s, each rich text (`PageWebPart.Text`) or one of Microsoft's standard web parts (`PageWebPart.Standard`); a section's `Layout` decides how many columns it has. A new or updated page is a draft until `PublishPageAsync`; `UpdatePageAsync` and `DeletePageAsync` complete the set. Creating and changing pages needs `Sites.ReadWrite.All`.

### Mail

`Outlook` reads, sends and organises mail in Exchange Online. Every call names the mailbox first: a user principal name or object id with app-only credentials, which reach every mailbox in the tenant unless RBAC for Applications in Exchange Online narrows them to some, or `me` with delegated credentials. A folder is an id or one of Outlook's well-known names: `inbox`, `drafts`, `sentitems`, `deleteditems`, `archive`, `junkemail`, `msgfolderroot` and others. The permissions, application or delegated:

| What | Graph permission |
|---|---|
| Reading mail, attachments and folders | `Mail.Read` |
| Drafts, attachments on drafts, moving, flagging, deleting, folders | `Mail.ReadWrite` |
| Sending, replying, forwarding | `Mail.Send` (plus `Mail.ReadWrite`, since a message is drafted first) |
| Mailbox settings, categories, inbox rules | `MailboxSettings.Read` / `MailboxSettings.ReadWrite` |

A mailbox someone else shared with the signed-in person is reached with the delegated `Mail.Read.Shared` family, which cannot subscribe to notifications.

Message ids are Graph's *immutable* ids: every request asks for them, so an id stays the same when the message moves to another folder or the archive, and an id stored yesterday still finds the message today. An `OutlookMessage` carries its sender and recipients as `OutlookRecipient`s, `SentAt` and `ReceivedAt`, `BodyPreview`, the `Body` as Outlook stores it (`BodyIsHtml` says which), the `ConversationId` every message of its thread shares, and the RFC 5322 threading: `InternetMessageId`, `InReplyTo` and `References`, all without angle brackets. `GetMessageAsync` reads one, null when it is gone, and `GetMessagesAsync` reads many through `$batch`, four at a time, leaving out the ones gone or refused on their own; `OutlookReadOptions` leaves the body out, asks for it as plain text, or adds every internet header in `Headers`. `ListMessagesAsync` reads a folder, or the whole mailbox, narrowed by an `OutlookMessageQuery` (received between two dates, read or unread, with attachments, from one sender, one conversation); `ListMessagesPageAsync` reads one `OutlookMessagePage` at a time, its `NextCursor` handed back for the next. `SearchMessagesAsync` searches the way Outlook's search box does, words or KQL such as `from:ben hasattachment:true`, at most 1,000 results. `ListConversationAsync` reads a whole thread oldest first, and `FindMessageByInternetMessageIdAsync` finds the message a reply names in its `In-Reply-To`. `DownloadMessageAsync` streams the whole message as MIME (an `.eml`), and `DownloadMessageBytesAsync` reads it into memory up to a limit:

<!-- ikon-example: connectors-outlook-read -->
```csharp
var thread = await outlook.ListConversationAsync("ada@contoso.com", message.ConversationId!);

foreach (var earlier in thread)
{
    Log.Instance.Info($"{earlier.SentAt}: {earlier.From?.Address} {earlier.Subject}");
}

await using var mime = await outlook.DownloadMessageAsync("ada@contoso.com", message.Id);
await KeepAsync($"{message.InternetMessageId}.eml", mime);
```

### Sending, replying and drafts

`SendAsync` sends a `NewOutlookMessage`: `To`, `Cc`, `Bcc` and `ReplyTo` (each an address, or `Name <address>`), `Text` or `Html`, `Attachments`, `Importance`, custom `x-` `Headers`, and `From` to send as a shared mailbox the owner holds Send As rights on. The message is made as a draft and then sent, which is what lets `SendAsync` hand back an `OutlookSentMessage` with the `InternetMessageId` it went out with — the id a reply will name — and a copy lands in Sent Items. A failure before the send removes the draft; a failed send leaves it in Drafts. `ReplyAsync` and `ForwardAsync` start from the draft Outlook would make: the reply is threaded, its subject is the original's with `RE:` unless `Subject` is set, its recipients are the original's sender (everyone, with `replyAll`) unless `To` is set, and `Cc` adds to whom Outlook copies; the text goes above the quoted original. `CreateDraftAsync`, `UpdateDraftAsync`, `SendDraftAsync` and `DeleteDraftAsync` leave the sending to a person. `SendMimeAsync` sends a message an app wrote as MIME itself, up to 3 MB:

<!-- ikon-example: connectors-outlook-send -->
```csharp
await using var ledger = File.OpenRead("./ledger.pdf");

var sent = await outlook.SendAsync("ada@contoso.com", new NewOutlookMessage("Documents for case RTD-11")
{
    To = ["Ben Bitdiddle <ben@acme.com>"],
    Bcc = ["case-rtd11@mail.example.com"],
    Html = "<p>Please find the ledger attached.</p>",
    Attachments = [new NewOutlookAttachment("ledger.pdf", "application/pdf", ledger, ledger.Length)],
});

await RememberSentAsync(sent.InternetMessageId);   // a reply names it in In-Reply-To
```

### Attachments

`ListAttachmentsAsync` lists a message's attachments without their content: each `OutlookAttachment` has its `Kind` — a `File`, an attached Outlook `Item` such as a forwarded email, or a `Reference` link to a file in OneDrive — and `IsInline` with the `ContentId` the HTML body names it by as `cid:`. `DownloadAttachmentAsync` streams one (an attached email as MIME), `DownloadAttachmentBytesAsync` reads it up to a limit, and a `Reference` has no content to download. `AddAttachmentAsync` attaches a stream to a draft: under 3 MB in one request, larger (up to 150 MB) through an upload session in 3 MiB ranges that never carry the token:

<!-- ikon-example: connectors-outlook-attachments -->
```csharp
foreach (var attachment in await outlook.ListAttachmentsAsync("ada@contoso.com", message.Id))
{
    if (attachment.Kind == OutlookAttachmentKind.Reference || attachment.IsInline)
    {
        continue;
    }

    await using var content = await outlook.DownloadAttachmentAsync("ada@contoso.com", message.Id, attachment.Id);
    await KeepAsync(attachment.Name, content);
}
```

### Organising mail

`UpdateMessageAsync` changes only what it is given: read state, `Categories` (the names `ListCategoriesAsync` lists), the follow-up `OutlookFlag`, `OutlookImportance`, and Focused Inbox's `OutlookClassification`; `MarkReadAsync` and `MarkUnreadAsync` are the common case. `MoveMessageAsync` and `CopyMessageAsync` take a destination folder, `ArchiveAsync` moves to the Archive folder, `DeleteMessageAsync` moves to Deleted Items, where the owner can still restore it, and `PermanentDeleteMessageAsync` cannot be undone. The plural forms — `UpdateMessagesAsync`, `MoveMessagesAsync`, `DeleteMessagesAsync`, `PermanentDeleteMessagesAsync` — change many messages through Graph's `$batch`, twenty to a request; every message is tried, then any that failed throw with the first failure's `StatusCode` and the count. Each is safe to repeat, and a message already gone counts as deleted:

<!-- ikon-example: connectors-outlook-organise -->
```csharp
await outlook.UpdateMessagesAsync("ada@contoso.com", selectedIds, isRead: true, categories: ["Case RTD-11"]);
await outlook.MoveMessagesAsync("ada@contoso.com", selectedIds, "archive");
```

### Folders

`FoldersDeltaAsync` reads every folder of the mailbox, at every depth, as one flat list of `OutlookFolder`s whose `ParentFolderId` rebuilds the tree, each with its item and unread counts; passed the `DeltaLink` of that `OutlookFolderDelta` again, it returns only the folders created, renamed, moved or removed since. `ListFoldersAsync` reads one level (hidden folders only when asked), `GetFolderAsync` one folder by id or well-known name, and `CreateFolderAsync`, `RenameFolderAsync`, `MoveFolderAsync` and `DeleteFolderAsync` change the tree.

### Reading only what changed in a mailbox

`MessagesDeltaAsync(user, folder)` works like `OneDrive.DeltaAsync`, one folder at a time: every message of the folder on the first call (`inbox` by default), then only what changed since the `DeltaLink` of the `OutlookDelta` it hands back. A new, read, flagged or moved-in message appears in its latest state; a removed message, or one moved out of the folder, has `Deleted` set. Graph will not read the threading on a change feed, so its messages carry no `InReplyTo` or `References`, and a message whose read state alone changed comes with only its `Id` and `IsRead`: read them in full with `GetMessagesAsync` before storing them. Subfolders are not included, so a mailbox is synced folder by folder, each with its own link, from the list `FoldersDeltaAsync` keeps. It reads pages of 100 messages, so the default `maxPages` of 50 covers a folder of 5,000; a larger folder throws `ConnectorPageCapException<OutlookMessage>` whose `ResumeFrom` is passed back as the delta link to read on. `receivedSince` starts the feed with recent mail only — Graph returns at most 5,000 messages to a feed started that way, so older history is read with `ListMessagesAsync` and `ReceivedBefore`. A link Graph no longer honours throws with `IsResyncRequired` (a `410`): start again without one:

<!-- ikon-example: connectors-outlook-delta -->
```csharp
var delta = await outlook.MessagesDeltaAsync("ada@contoso.com", "inbox", deltaLink);

foreach (var message in delta.Items.Where(m => !m.Deleted))
{
    Log.Instance.Info($"{message.From?.Address}: {message.Subject}");
}

deltaLink = delta.DeltaLink;   // store it for the next call
```

### Being told when mail arrives

`SubscriptionResource.Messages(user, folder)` subscribes `GraphSubscriptions` to a mailbox, or one folder of it, for messages created, updated and deleted (see "Being told when something changed"). Its notification names the message in `ResourceId`; read the change with `MessagesDeltaAsync` on the folder. A mail subscription lives at most a week (`SubscriptionResource.MaxLifetime`), so renew it from its record, `RenewAsync(subscription)`, which keeps within that. Microsoft advises naming the mailbox by its object id here. Besides `reauthorizationRequired`, a mail subscription's lifecycle URL receives `missed` when Graph dropped notifications — read the folders with delta — and `subscriptionRemoved`, when it must be created again:

<!-- ikon-example: connectors-outlook-subscribe -->
```csharp
var subscription = await subscriptions.CreateAsync(
    SubscriptionResource.Messages(mailboxObjectId), notificationUrl, clientState, lifecycleNotificationUrl: notificationUrl);

// Later, before it ends: renewing from the record keeps within the mail maximum.
subscription = await subscriptions.RenewAsync(subscription);
```

### Mailbox settings

`GetMailboxSettingsAsync` reads `OutlookMailboxSettings`: the mailbox's time zone, language, archive folder, purpose (a person's, shared, a room) and `OutlookAutomaticReplies`. `SetAutomaticRepliesAsync` turns the out-of-office reply on, off, or on between two times (`OutlookAutomaticRepliesStatus`), for whom outside the organisation `OutlookExternalAudience` says. `ListCategoriesAsync`, `CreateCategoryAsync`, `UpdateCategoryAsync` and `DeleteCategoryAsync` manage the `OutlookCategory`s messages are tagged with, each in one of Outlook's 25 preset colours (`OutlookCategoryColor`). `ListRulesAsync`, `CreateRuleAsync` (a `NewOutlookRule`), `UpdateRuleAsync` and `DeleteRuleAsync` manage inbox rules: each `OutlookRule`'s `OutlookRuleConditions` (sender, recipient, subject or body words, attachments, importance) choose the mail its `OutlookRuleActions` move, copy, mark read, categorise, forward or delete.

### Teams and channels

`Teams` reads and manages Microsoft Teams: teams, channels, chats and their members. `TeamsMessages` reads and posts messages, `TeamsMeetings` reaches online meetings and their transcripts and recordings, `TeamsPresence` reads and sets presence, and `TeamsBot` is a bot that answers as itself. They take the same credentials and token provider as the other clients. A call that takes a `user` takes an object id or user principal name, or `me` with delegated credentials. The permissions:

| What | Delegated | Application |
|---|---|---|
| Reading teams and channels | `Team.ReadBasic.All`, `Channel.ReadBasic.All` | the same |
| Creating and changing teams and channels | `Team.Create`, `TeamSettings.ReadWrite.All`, `Channel.ReadWrite.All`; deleting a team `Group.ReadWrite.All` | the same |
| Team and channel members | `TeamMember.ReadWrite.All`, `ChannelMember.ReadWrite.All` | the same |
| Chats and their members | `Chat.ReadWrite`, `ChatMember.ReadWrite` | `Chat.ReadWrite.All`, `ChatMember.ReadWrite.All` |
| Channel messages | `ChannelMessage.Read.All`; posting `ChannelMessage.Send` | `ChannelMessage.Read.All`; no posting |
| Chat messages | `Chat.Read`; posting `ChatMessage.Send` | `Chat.Read.All`; no posting |
| Meetings | `OnlineMeetings.ReadWrite`, `OnlineMeetingArtifact.Read.All`, `OnlineMeetingTranscript.Read.All`, `OnlineMeetingRecording.Read.All` | the same as `.All`, plus an application access policy (see "Meetings, transcripts and recordings") |
| Presence | `Presence.Read.All`; setting `Presence.ReadWrite` | `Presence.Read.All`, `Presence.ReadWrite.All` |

Teams allows an app to read a resource for changes only once a day. To hear about changes sooner, subscribe (see "Being told when Teams messages arrive") or read a change feed.

`ListJoinedTeamsAsync` lists the teams a person is in, `ListTeamsAsync` every team in the tenant, and `GetTeamAsync` reads one `TeamsTeam`, with its `TeamsVisibility`. `CreateTeamAsync` returns a `TeamsPendingTeam` as soon as Graph accepts it, because Teams sets the team up in the background, which can take minutes. Subscribe to `SubscriptionResource.Teams` to be told when it appears, or read `GetTeamOperationAsync` once when the team is next needed: its `TeamsOperation` has a `TeamsOperationStatus` and, once done, the team's id. With app-only credentials, name an owner in `NewTeamsTeam.OwnerUserIds`, since Graph refuses a team without one. `UpdateTeamAsync` changes only what it is given, `ArchiveTeamAsync` makes a team read-only and `UnarchiveTeamAsync` reverses it, and `DeleteTeamAsync` deletes the team with its group, which can be restored for 30 days. `ListChannelsAsync`, `GetChannelAsync` and `GetPrimaryChannelAsync` (the General channel) read `TeamsChannel`s. `CreateChannelAsync` makes a standard, private or shared channel (`NewTeamsChannel` with its `TeamsChannelMembership`), and `UpdateChannelAsync` and `DeleteChannelAsync` change and remove one. A channel's files are a folder in the team's SharePoint site: `GetChannelFolderAsync` returns it as a `DriveItem`, and `OneDrive` reads and writes the files in it, which needs `Files.Read.All` as well:

<!-- ikon-example: connectors-teams-channels -->
```csharp
var fileCount = 0;

foreach (var team in await teams.ListJoinedTeamsAsync("me"))
{
    foreach (var channel in await teams.ListChannelsAsync(team.Id))
    {
        // A channel's files are a SharePoint folder, read with OneDrive.
        var folder = await teams.GetChannelFolderAsync(team.Id, channel.Id);
        fileCount += (await oneDrive.ListChildrenAsync(folder.DriveId, folder.Id)).Count;
    }
}
```

### Chats and members

`ListChatsAsync` lists a person's one-on-one, group and meeting chats as `TeamsChat`s, each with its `TeamsChatType`, and `GetChatAsync` reads one. `CreateChatAsync` takes every member's id, the signed-in person's included with delegated credentials: two members and no topic make a one-on-one chat, which Graph returns as it is when the two already have one, and anything else makes a group chat. `RenameChatAsync` changes a group chat's topic. `HideChatAsync`, `UnhideChatAsync`, `MarkChatReadAsync` and `MarkChatUnreadAsync` act for one person, named by user id and tenant id.

`ListTeamMembersAsync`, `ListChannelMembersAsync` and `ListChatMembersAsync` return `TeamsMember`s. A member's `Id` is the membership's id, which `SetTeamMemberRoleAsync` and the `Remove…MemberAsync` methods take; `UserId` is the person's. `AddTeamMemberAsync` adds one person, as an owner if asked. `AddTeamMembersAsync` adds many, 200 to a request; every one is tried, then any that failed throw one `ConnectorException` naming how many, so the call is safe to repeat. A standard channel's members are the team's, so channel members are for private and shared channels. `AddChatMemberAsync` adds someone to a group chat, with or without the earlier messages (`shareHistory`), and returns the membership id.

### Posting and reading Teams messages

`TeamsMessages` names where messages are with a `TeamsConversation`: `TeamsConversation.Channel(teamId, channelId)` for a channel's own messages, `TeamsConversation.Thread(teamId, channelId, messageId)` for the replies to one of them, and `TeamsConversation.Chat(chatId)` for a chat. It serializes with System.Text.Json, so an app can store one. Every `TeamsMessage` carries its `Conversation`, and its `Thread` is where replies to it go; a chat message has no `Thread`, since chats have no threads.

Posting is done as the signed-in person. Graph lets an app post as itself only to migrate messages from another system, so `PostAsync` and `UpdateAsync` throw `InvalidOperationException` with app-only credentials: a message from the app goes through `TeamsBot`. A `NewTeamsMessage` has `Text` or `Html`, a `Subject` (on a channel's root message only), a `TeamsImportance` (`Urgent` in a chat notifies again every two minutes for 20 minutes), `Mentions`, `Files`, `Cards` and `Images`. A mention is written in the HTML as `<at id="0">Grace</at>` and named in `Mentions` with the same number and a `TeamsMentionTarget`: a user, channel, team or tag (`TeamsMentionKind`). A tag or mention without its partner throws `ArgumentException` before any request. `TeamsFileReference.From` attaches a SharePoint or OneDrive file by link, so the people who open the message need access to it. `TeamsCard.Adaptive` attaches an Adaptive Card, but Graph shows only its `Action.OpenUrl` buttons; a card whose buttons the app answers needs the bot. `TeamsImage` puts an image inside the message, and a message carries at most 4 MB:

<!-- ikon-example: connectors-teams-post -->
```csharp
var channel = TeamsConversation.Channel(teamId, channelId);

var posted = await messages.PostAsync(channel, new NewTeamsMessage
{
    Html = "The release notes are ready, <at id=\"0\">Grace</at>.",
    Mentions = [new TeamsMention(0, "Grace", TeamsMentionTarget.User(graceUserId))],
});

// A message's Thread is where its replies go.
await messages.PostAsync(posted.Thread!, "Reply here with anything missing.");
await messages.AddReactionAsync(channel, posted.Id, "🚀");
```

`GetMessageAsync` reads one message, null when it does not exist. `ListMessagesAsync` reads a channel's root messages, a thread's replies or a chat's messages, newest first; a chat can be narrowed to messages changed between two times (`modifiedSince`, `modifiedBefore`), which Graph does not offer for channels. `ListMessagesPageAsync` reads one `TeamsMessagePage` of up to 50 at a time. Teams stores a message's `Body` as HTML. A message carries who sent it (`TeamsSender`, a person or an app), its `Mentions`, its `Reactions` (`TeamsReaction`s, with who reacted and when), and its `Attachments` (`TeamsAttachment`s): a file is a link, which `OneDrive.ResolveSharingUrlAsync` turns into a `DriveItem` to download, and a card holds its JSON in `Content`. The images inside a message are in `HostedContentUrls`, read with `DownloadHostedContentAsync` or `DownloadHostedContentBytesAsync`. `UpdateAsync` replaces a message the signed-in person posted. `DeleteAsync` deletes one the way Teams does, leaving it in place marked deleted, and `RestoreAsync` brings it back. `AddReactionAsync` and `RemoveReactionAsync` take any emoji and, like posting, throw `InvalidOperationException` with app-only credentials, since a bot cannot react. `ListPinsAsync`, `PinAsync` and `UnpinAsync` manage a chat's pinned messages; Graph pins only in chats, and `UnpinAsync` takes the pin's own `TeamsPin.Id`.

### Reading only what changed in Teams

`ChannelMessagesDeltaAsync(teamId, channelId)` works like `OneDrive.DeltaAsync`: the channel's root messages on the first call, then only the ones new or changed since the `DeltaLink` of the `TeamsMessageDelta` it hands back, each in its latest state, with `Deleted` set on a deleted one. It does not report replies, so read a thread with `ListMessagesAsync` on the message's `Thread`. `ChatMessagesDeltaAsync(user)` reads every chat a person is in as one feed, with application permissions only, and each message names its chat in `Conversation`. Both reach eight months back. Graph cannot start these feeds from now, so `modifiedSince` starts one from a recent time instead of the beginning. A link Graph no longer honours throws with `IsResyncRequired` (a `410`): start again without one. A feed past its `maxPages` throws `ConnectorPageCapException<TeamsMessage>` whose `ResumeFrom` is the link to read on with:

<!-- ikon-example: connectors-teams-delta -->
```csharp
var delta = await messages.ChannelMessagesDeltaAsync(teamId, channelId, storedLink, modifiedSince: DateTimeOffset.UtcNow.AddDays(-7));

foreach (var message in delta.Items)
{
    if (message.Deleted)
    {
        await ForgetAsync(message.Id);
    }
    else
    {
        await IndexAsync(message);
    }
}

await SaveDeltaLinkAsync(delta.DeltaLink);
```

### Being told when Teams messages arrive

`GraphSubscriptions` subscribes to Teams the same way as to a drive (see "Being told when something changed"). `SubscriptionResource.ChannelMessages`, `ChatMessages` and `UserChatMessages` watch one channel, one chat or every chat a person is in, and `AllChannelMessages` and `AllChatMessages` watch the whole tenant with application permissions. `SubscriptionResource.Teams`, `Channels`, `TeamMembers` and `ChatMembers` report teams, channels and members coming and going, `Presences` reports changes to up to 650 people's presence, and `MeetingTranscripts` and `MeetingRecordings` report new transcripts and recordings. A Teams subscription lives at most three days and a presence one an hour, so renew one from its record with `RenewAsync(subscription)`, which keeps within its maximum. Graph allows a Teams subscription without a `lifecycleNotificationUrl` to live an hour at most, so `CreateAsync` and `RenewAsync` keep one within that; give it a lifecycle URL for the three days.

A Teams notification names the message or member that changed in `ResourceId`, to read with `GetMessageAsync`. It can also carry the message itself: give `CreateAsync` a `GraphResourceDataKey`, a certificate with its private key and a name of your choosing, and Graph encrypts each message to the certificate. `GraphResourceDataReader` reads such a delivery: it checks that Microsoft signed the delivery's validation tokens for your app and for each notification's tenant, decrypts each message, and returns it in `GraphNotification.ResourceData`, for `TeamsMessages.ParseMessage`. A delivery that fails those checks reads as nothing. To change certificates, subscribe with the new one and give the reader both until the old subscriptions end:

<!-- ikon-example: connectors-teams-subscribe -->
```csharp
var key = new GraphResourceDataKey(certificate, "teams-2026");

var subscription = await subscriptions.CreateAsync(
    SubscriptionResource.AllChannelMessages(), notificationUrl, clientState,
    lifecycleNotificationUrl: notificationUrl, resourceData: key);

// One reader for the app's lifetime, used by the notification endpoint: it caches Microsoft's signing keys.
_reader = new GraphResourceDataReader(clientId, [key]);
```

<!-- ikon-example: connectors-teams-notifications -->
```csharp
if (GraphNotifications.ValidationToken(request.Query) is { } token)
{
    return new HttpResult(200, token, "text/plain");
}

foreach (var notification in await _reader!.ReadAsync(request.Body, clientState))
{
    if (notification.ResourceData is { } resource && TeamsMessages.ParseMessage(resource) is { } message)
    {
        await IndexAsync(message);
    }
    else if (notification.LifecycleEvent is not null)
    {
        QueueRenewal(notification.SubscriptionId);
    }
}

return new HttpResult(202, "", "text/plain");
```

### Meetings, transcripts and recordings

`TeamsMeetings` takes the meeting's organizer first. `CreateMeetingAsync` makes an online meeting from a `NewTeamsMeeting`, with who skips the lobby (`TeamsLobbyBypass`), and returns its `TeamsMeeting` with the `JoinUrl` to share; Graph does not put it in anyone's calendar. `GetMeetingAsync` reads one, `FindMeetingByJoinUrlAsync` finds the meeting a join link opens, such as one in a calendar invitation, and `UpdateMeetingAsync` and `DeleteMeetingAsync` change and remove one. A meeting's chat is `TeamsMeeting.ChatId`, for `TeamsMessages`. `ListAttendanceReportsAsync` lists a `TeamsAttendanceReport` for each time the meeting ran, and `ListAttendanceRecordsAsync` a `TeamsAttendanceRecord` for each person who attended, with how long they stayed and a `TeamsAttendanceInterval` for each time they joined and left. `ListTranscriptsAsync` lists a meeting's `TeamsTranscript`s, and `DownloadTranscriptAsync` and `DownloadTranscriptBytesAsync` read one as WebVTT, with each caption's speaker in a `<v>` tag. `ListRecordingsAsync` lists its `TeamsRecording`s, and `DownloadRecordingAsync` streams one as MP4. To hear about new ones, subscribe to `SubscriptionResource.MeetingTranscripts` or `MeetingRecordings`.

With app-only credentials, a tenant administrator also has to let the app act for the organizers it reaches, with an application access policy in Teams PowerShell: `New-CsApplicationAccessPolicy` naming the app's client id, then `Grant-CsApplicationAccessPolicy` for the people or the whole tenant. Until then Graph answers `403`:

<!-- ikon-example: connectors-teams-transcripts -->
```csharp
var meeting = await meetings.FindMeetingByJoinUrlAsync(organizerId, joinUrl)
    ?? throw new InvalidOperationException("The organizer has no meeting with that link");

foreach (var transcript in await meetings.ListTranscriptsAsync(organizerId, meeting.Id))
{
    var vtt = await meetings.DownloadTranscriptBytesAsync(organizerId, meeting.Id, transcript.Id, maxBytes: 10_000_000);
    await SummariseAsync(meeting.Id, vtt);
}
```

### Presence

`TeamsPresence.GetPresenceAsync` reads a person's `TeamsPresenceInfo`: their `TeamsAvailability`, what they are doing (`Activity`), their status message and whether they are out of office. `GetPresencesAsync` reads many people's at once, 650 to a request. `SetPresenceAsync` sets the presence the app reports for a person, such as busy while it is on a call with them, and `ClearPresenceAsync` removes it. Teams combines it with the person's other sessions, and the app's session is its client id. `SetPreferredPresenceAsync` sets the presence the person chose, which wins over every session's, and `ClearPreferredPresenceAsync` removes it. `SetStatusMessageAsync` sets or clears the note others see. To follow changes, subscribe to `SubscriptionResource.Presences` rather than reading again and again:

<!-- ikon-example: connectors-teams-presence -->
```csharp
var people = await presence.GetPresencesAsync(memberIds);
var free = people.Where(p => p.Availability == TeamsAvailability.Available && !p.IsOutOfOffice).Select(p => p.UserId).ToList();

await presence.SetStatusMessageAsync("me", "Reviewing the launch plan", DateTimeOffset.UtcNow.AddHours(2));
```

### A Teams bot: setting it up

A bot is the app's own identity in Teams: people message it, mention it in channels, press its card buttons, and it answers as itself. Setting one up is done once:

1. **Register the bot.** In the Azure portal, create an Azure Bot resource for an app registration, single-tenant, and give it a client secret or certificate. Set its messaging endpoint to the public URL of your app's `[HttpPost]` bot endpoint, such as `/teams/messages`, and turn on the Microsoft Teams channel.
2. **Make the Teams app.** A Teams app package is a `manifest.json` naming the bot's client id, the scopes it works in (`personal`, `team`, `groupChat`), and two icons, zipped together. Upload it in the Teams admin center for the organisation, or in Teams itself to try it.
3. **Give the app the credential.** `TeamsBot` takes `MicrosoftAppCredentials` or `MicrosoftCertificateCredentials` for the bot's app registration, in the bot's own tenant. A bot registered before Microsoft stopped making multi-tenant bots uses `botframework.com` as its tenant, so its credential is separate from the one the Graph clients share. The bot needs no Graph permission to answer.

### Answering in Teams

Teams posts an activity to the bot's endpoint for each thing that happens. `TeamsActivities.Parse` reads it as a `TeamsActivity`, and `TeamsBotVerifier.VerifyAsync` checks the request's bearer token: that the Bot Connector signed it for this bot, for the activity's service URL and for Teams. Answer one that fails with `401`. Keep one verifier for the app's lifetime, since it caches the signing keys. The activities are:

- `TeamsMessageActivity`: a person messaged the bot, or mentioned it in a channel or group chat. `TextWithoutBotMention` is the text without the bot's own mention, and `Value` holds what an Adaptive Card's `Action.Submit` button sent.
- `TeamsMessageUpdateActivity`, `TeamsMessageDeleteActivity` and `TeamsReactionActivity`: a message edited, deleted, or reacted to.
- `TeamsConversationUpdateActivity`: members joining or leaving, the bot among them, or a channel or team changing (`EventType`).
- `TeamsInstallationActivity`: the bot installed or removed.
- `TeamsCardActionActivity`: a person pressed an Adaptive Card's `Action.Execute` button.
- `TeamsFeedbackActivity`: a thumbs up or down from the bot's feedback buttons.
- `TeamsInvokeActivity` and `TeamsUnknownActivity`: anything else, with Teams' JSON in `Raw`.

<!-- ikon-example: connectors-teams-activity-kinds -->
```csharp
var line = TeamsActivities.Parse(body) switch
{
    TeamsMessageActivity message => $"{message.From.Name}: {message.TextWithoutBotMention}",
    TeamsMessageUpdateActivity edit => $"edited {edit.Id}: {edit.Text}",
    TeamsMessageDeleteActivity deleted => $"deleted {deleted.Id}",
    TeamsReactionActivity reaction => $"{(reaction.Added ? "added" : "removed")} {string.Join(" ", reaction.Reactions)} on {reaction.ReplyToId}",
    TeamsConversationUpdateActivity update => $"{update.EventType}: {update.MembersAdded.Count} joined, {update.MembersRemoved.Count} left",
    TeamsInstallationActivity installation => installation.Added ? "installed" : "removed",
    TeamsCardActionActivity action => $"card button {action.Verb}",
    TeamsFeedbackActivity feedback => $"vote {feedback.Reaction}",
    TeamsInvokeActivity invoke => $"invoke {invoke.Name}",
    TeamsUnknownActivity unknown => $"not read: {unknown.Type}",
    _ => "not an activity",
};
```

Answer every activity with `200` within 15 seconds and do slow work after answering. An invoke, such as a card action or a vote, is answered in the same response with a body from `TeamsInvokeResponse`. Every activity names its `Conversation`, a `TeamsBotConversation` to answer it in, whose `TeamsBotConversationType` says whether it is a one-on-one chat, a group chat or a channel. `PostAsync` sends a `TeamsBotMessage` and returns its activity id. In a channel thread, pass the activity's `ReplyToId` to keep the answer in the thread. `UpdateAsync` and `DeleteAsync` change the bot's own messages, and `SendTypingAsync` shows it writing. A `TeamsBotMessage` has `Text` in markdown by default (`TeamsBotTextFormat`), `Cards` (`TeamsCard.Adaptive`, laid out by `TeamsCardLayout` as a list or a carousel), `Mentions` (`TeamsBotMention`s, written in the text as `<at>Name</at>`), `SuggestedActions` offered as buttons in a one-on-one chat, a `Summary` for notifications, and for an AI agent `AIGenerated`, `Citations` (`TeamsCitation`s, shown where the text names `[1]`) and `FeedbackButtons`. `ListMembersAsync` and `GetMemberAsync` read the people in a conversation as `TeamsBotUser`s, whose `AadObjectId` is the id the Graph clients take, and `GetTeamAsync` and `ListTeamChannelsAsync` read the team a channel belongs to as a `TeamsBotTeam` and its `TeamsBotChannel`s. A conversation the bot cannot reach, because it was removed, never installed or blocked, throws with `StatusCode` 404:

<!-- ikon-example: connectors-teams-bot-endpoint -->
```csharp
[HttpPost("/teams/messages", Auth = EndpointAuth.Public)]
public async Task<HttpResult> OnTeamsActivityAsync(Ikon.App.HttpRequest request)
{
    if (TeamsActivities.Parse(request.Body) is not { } activity
        || !await _verifier.VerifyAsync(request.Headers.GetValueOrDefault("Authorization"), activity))
    {
        return HttpResult.Unauthorized();
    }

    switch (activity)
    {
        case TeamsMessageActivity message:
            // Teams wants its answer within 15 seconds, so the reply is sent after it.
            _ = Task.Run(() => _bot.PostAsync(message.Conversation, $"You said: {message.TextWithoutBotMention}", message.ReplyToId));
            break;

        case TeamsInstallationActivity { Added: true } installed:
            await SaveConversationAsync(installed.From.Id, installed.Conversation);
            break;

        case TeamsCardActionActivity { Verb: "approve" } action:
            await ApproveAsync(action.Data);
            return HttpResult.Json(TeamsInvokeResponse.Message("Approved"));

        case TeamsFeedbackActivity feedback:
            await RecordVoteAsync(feedback.ReplyToId, feedback.Reaction, feedback.Feedback);
            return HttpResult.Json(TeamsInvokeResponse.Ok());
    }

    return HttpResult.Ok();
}
```

### Streaming a bot's reply

`StreamAsync` streams a sequence of text, such as an AI model's, as one reply that Teams shows growing as it is written. `StartStream` returns a `TeamsBotMessageStream` to drive by hand: `InformAsync` says what the bot is doing until the text starts, `AppendAsync` adds text, and `StopAsync` finishes the message, adding a final `TeamsBotMessage`'s cards, citations, AI label and feedback buttons. Text is sent as it arrives. An append goes out at once when no request is under way and a second has passed since the last, which is Teams' limit; otherwise it goes with the next request, so a quiet stream sends nothing. Teams ends a stream at two minutes, so the stream finishes its message by `TeamsBotStreamOptions.MaxDuration` and posts any later text as a message of its own. When the person presses Stop, `StoppedByUser` is set, later text is dropped, and `StreamAsync` stops reading. Teams streams in one-on-one chats only; in a group chat or channel the stream shows the bot typing and posts the reply once it stops:

<!-- ikon-example: connectors-teams-bot-stream -->
```csharp
var run = Emerge.Run<string>(LLMModel.Claude55Sonnet, pass => pass.Command = message.TextWithoutBotMention);

await _bot.StreamAsync(
    message.Conversation,
    run.OfType<ModelText<string>>().Select(token => token.Text),
    final: new TeamsBotMessage { AIGenerated = true, FeedbackButtons = true });
```

### Messaging people first

A bot can message a conversation it was part of at any time: store the `TeamsBotConversation` from an activity, such as `TeamsInstallationActivity`, and post to it later. `CreatePersonalConversationAsync` opens the one-on-one chat with a person the bot has not talked to yet, and can send a first message with it; the bot must already be installed for the person, or it throws with `StatusCode` 404. `CreateChannelThreadAsync` starts a new thread in a channel the bot was added to and returns a `TeamsBotThread`:

<!-- ikon-example: connectors-teams-bot-proactive -->
```csharp
// A conversation stored from an earlier activity is answered at any time.
await _bot.PostAsync(stored, "Your report is ready.");

// A person the bot has not talked to yet: open the chat first.
var chat = await _bot.CreatePersonalConversationAsync(serviceUrl, tenantId, userId, "Welcome aboard!");
await SaveConversationAsync(userId, chat);
```

### Rate limits

Graph and SharePoint throttle per app and per tenant, and a throttled call is retried before it surfaces: a `429` or `503` on any method and a `504` on a read (a gateway timeout on a write may hide a write that happened, so it is never repeated) are sent again up to three times, each after the `Retry-After` the service named, bounded at two minutes. A throttled answer that names a longer wait surfaces at once, since every retry inside it would be refused again. The one retry with a fresh token after a `401` does not count among the three. A call that still fails throws with `IsTransient` set: back off and try later rather than at once. Every request carries the `User-Agent` SharePoint's throttling guidance asks integrations to send, which it favours over anonymous traffic when it throttles. Outlook allows one app at most four requests at a time and 10,000 every ten minutes against one mailbox, so sync a mailbox's folders one after another rather than all at once; a request in a `$batch` is throttled on its own, and the bulk methods send a throttled one again after its `Retry-After`. `CopyAsync` polls SharePoint every two seconds while it works, or as its `Retry-After` asks, and `CreateSiteAsync` every three, each until its `maxWait`. Teams has its own limits per app and tenant on top of Graph's, and allows reading a resource for changes only once a day. A bot may send about seven messages a second to one conversation, and the Bot Connector throttles with `429` alone, so a bot's message is never sent twice after a `503`. A streamed reply sends at most one request a second.

### What it does not reach

The library uses Graph v1.0 and nothing from Graph's beta, so creating a site through `POST /sites`, the site recycle bin in Graph, page templates and archiving sites are not here (`CreateSiteAsync` and `ListRecycleBinAsync` reach the first two through SharePoint's own API). SharePoint Embedded containers, hub sites, site designs and scripts, list views and tenant administration settings are not covered, and change notifications arrive only by webhook, not over Graph's socket.io channel.

Mail reaches a mailbox's primary folders: the in-place archive mailbox, which Graph's v1.0 mail API does not open, is not reached, nor are mail signatures, which Graph does not expose, MailTips, Focused Inbox overrides, or mail notifications that carry the message itself (resource data is for Teams subscriptions only). A sent or received message's subject and body cannot be changed, only a draft's. Nor are Calendar, Contacts, To Do, Planner, OneNote or Excel workbooks. `EntraDirectory` finds one person and a person's groups; it does not list the directory's users or groups, a group's members, or photos. Only Microsoft's global cloud is reached — not the national clouds (US Government, China), nor Teams bots in them — and SharePoint only on `*.sharepoint.com` hosts, not a vanity domain.

Teams is reached for teams, channels, chats, members, messages, meetings, presence and bots. Calls and their real-time media, Shifts, tabs and app installation, tags, the activity feed, channel email addresses beyond reading them, the export and retained-message APIs, and importing messages from another system are not covered. A bot does not cover message extensions, task modules, meeting apps or file uploads that ask the person's consent.
