# Ikon Connectors Developer Guide
<!-- checked-against: 27536c2ea258482d -->
This guide covers the connector libraries — `Ikon.Connectors` (Slack, GitHub, Microsoft Graph, Procountor), `Ikon.Connectors.Google` (Drive, Gmail), and `Ikon.Connectors.Browser` (agentic and scripted web automation) — for app developers wiring external services into an Ikon app.

## Overview

Each connector is a **raw** client for one external service: a thin, typed wrapper over the service's API with no agent coupling. The connectors' agent skills are internal, so an app cannot construct them or register them on a persona of its own; the one public route to one is `BrowserOperatorPersona.Create()`, which builds a persona around the browser skill. This guide focuses on the raw connectors.

All connectors report failures with `ConnectorException` (from `Ikon.Connectors`). It carries `Provider` (`"slack"`, `"github"`, `"microsoft-graph"`, `"procountor"`, `"gmail"`, `"drive"`, `"browser"`) and, when the failure was an HTTP error, `StatusCode`, normalized so that `401`/`403` mean the credential must be reconnected: a Slack auth error (HTTP 200 with `ok:false`) is reported as `401`, and a GitHub rate-limit `403` as `429`. Any other Slack API error has a null `StatusCode` and its code (`channel_not_found`, `not_in_channel`, `is_archived`) in `ErrorCode`. Branch on `StatusCode` to distinguish a permanent `401`/`403` — the credential is bad or revoked, so surface a "reconnect required" state instead of retrying — from a transient failure worth retrying (Google, Microsoft Graph and Procountor report the raw status, and a Google `403` can be a quota rather than a revoked grant):

<!-- ikon-example: connectors-errors -->
```csharp
try
{
    await slack.PostAsync(channelId, text);
}
catch (ConnectorException ex) when (ex.StatusCode is 401 or 403)
{
    // Permanent: the token is invalid or revoked. Ask the user to reconnect.
}
catch (ConnectorException)
{
    // Transient or service-side: safe to retry later.
}
```

The Slack and GitHub connectors honor rate limits on their JSON API calls: a `429` is retried up to three times, waiting the server's `Retry-After` (bounded at two minutes), before it surfaces as a `ConnectorException`. Four methods bypass that retry and fail immediately on a `429` — `GitHub.GetPullRequestDiffAsync`, `GitHub.MergePullRequestAsync`, `GitHub.MergePullRequestUnpinnedAsync`, and `Slack.DownloadFileAsync` — so wrap those yourself when rate limiting matters. `MicrosoftGraph` and `Procountor` retry the same way on every call they make, the token request included, and on its Graph calls and downloads `MicrosoftGraph` treats a `503` as it does a `429`, because Graph throttles with either.

GitHub answers a primary or secondary rate limit with `403` as well; the connector reports such a response (one with `X-RateLimit-Remaining: 0` or a `Retry-After`) as `429`, so a GitHub `403` that reaches you is an access failure, not a rate limit.

One more exception type exists, and it is not a failure: the paged reads (`Slack.HistorySinceAsync`, `Slack.ListConversationsAsync`, `GitHub.ListIssuesSinceAsync`) each take a `maxPages` bound, and a call that reaches it with the service still holding more throws `ConnectorPageCapException<T>` rather than handing back a shortened list as if it were complete. What it carries differs because Slack and GitHub page in opposite directions: `ResumeFrom` is set only where a cursor can continue the read (GitHub issues) and is null for Slack, and Slack history hands back no `Items` at all — each method's section below says which. Every listing of `MicrosoftGraph` and `Procountor` follows the same rule, with `ResumeFrom` set to its own cursor: the next page link for Graph, the lowest id read for Procountor. The platform's own backend listings (`IkonBackend` — spaces, databases, billing rows, release notes, everything an `ikon` verb or Studio lists) follow the same rule with `BackendPageCapException<T>`: a `maxResults` window that fills while the backend reports more throws with the `Items` read, the `TotalCount`, and the `NextCursor`, never a shortened list as the total.

## Slack

Construct `Slack` with a **bot token** (`xoxb-...`); an empty or whitespace token throws `ArgumentException` at construction. An optional `HttpClient` can be injected; otherwise a shared one is used.

<!-- ikon-example: connectors-slack-client -->
```csharp
var slack = new Slack(botToken);
```

### Posting

<!-- ikon-example: connectors-slack-post -->
```csharp
var posted = await slack.PostAsync("C0123456789", "Deploy finished", threadTs: rootTs);
```

The returned `SlackPostResult` is what Slack echoed back: the posted message's `Ts` and the `Channel` id a name like `#general` resolved to. Pass that `Ts` as `threadTs` to reply in the thread; the post's author, subtype and files are not returned — read the message back from history for those.

### Reading history

Slack timestamps (`Ts`, `ThreadTs`, `oldestTs`) are **raw Slack `ts` strings** (e.g. `"1727694230.000200"`), not `DateTime`s. Treat them as opaque ordered cursors and pass them back verbatim.

`HistoryAsync(channel, limit)` fetches one page of recent messages. `HistorySinceAsync(channel, oldestTs)` fetches every **top-level** message with `ts > oldestTs`, following pagination to completion and returning the result **oldest-first**, so a caller that advances a cursor per message never leaves a gap in the channel's own timeline:

<!-- ikon-example: connectors-slack-history -->
```csharp
var messages = await slack.HistorySinceAsync(channelId, oldestTs: lastSeenTs);

foreach (var message in messages)
{
    await ProcessAsync(message);
    lastSeenTs = message.Ts;   // safe: oldest-first means no gap on interruption
}
```

**In-thread replies are not in that result, and nothing reports their absence.** Both methods call `conversations.history`, which returns only the messages posted to the channel itself; a reply posted inside a thread is reached by `conversations.replies` on its parent's `ThreadTs`, and this connector does not call it. So a channel feed built on `HistorySinceAsync` alone silently drops every threaded reply, however far the cursor advances. A message that owns a thread carries its own `ts` as `ThreadTs` — fetch each such thread yourself if replies matter to you.

Paging is bounded by `maxPages` (default 50 pages of `pageLimit` 200), and the bound is never silent: when it trips with Slack still reporting a `next_cursor`, the call throws `ConnectorPageCapException<SlackMessage>` instead of returning, with empty `Items` and a null `ResumeFrom`. Because Slack pages **backward in time**, the messages read are the most recent ones and the unread gap sits **below** them, so none are handed out — keep your cursor at the `oldestTs` you called with, and close the gap by calling again with a larger `maxPages` or by reading narrower windows with the `HistorySinceAsync(channel, oldestTs, latestTs)` overload. `maxPages` must be positive and `pageLimit` 1–1000, or the call throws `ArgumentOutOfRangeException`. A caller that stays under the bound sees no exception at all.

### Conversations and files

`ListConversationsAsync` returns the public and private channels the token can see, paging up to `maxPages`; a workspace with more channels than the cap admits gets a `ConnectorPageCapException<SlackConversation>` carrying the channels read so far as `Items` and a null `ResumeFrom`, never a shortened list presented as the total — raise `maxPages` for such a workspace. Archived channels are included; filter on `SlackConversation.IsArchived` before posting, which fails on one with `is_archived`. `GetConversationAsync(channelId)` fetches one. Both hand back `SlackConversation` records — `Id`, `Name`, `IsMember`, and the three shape flags `IsPrivate`, `IsIm` and `IsMpim` that separate a channel from a DM or a group DM. A file shared into a message arrives as a `SlackFile` (`Id`, `MimeType`, and a `DownloadUrl` that is null when the token cannot fetch it). `DownloadFileAsync(url)` downloads a shared file's `url_private_download` with the bot token; when Slack answers with its HTML sign-in page instead (the token lacks `files:read` or the file is not shared with the bot), it throws `ConnectorException` rather than returning that page as the file. It fetches Slack-owned hosts only (`slack.com` and subdomains); any other URL — e.g. one parsed out of untrusted message text — throws `ArgumentException` without a request, so the token can never leak to another server.

### Socket Mode

`OpenSocketUrlAsync` requests a Socket Mode URL (`apps.connections.open`) and returns it — the library ships no Socket Mode client, so the WebSocket handshake, envelope acknowledgements, hello/disconnect handling, and reconnection on the URL's short expiry are yours to implement. It requires an **app-level token** (`xapp-...`) passed as its argument — an empty token or one without the `xapp-` prefix, such as the bot token, throws `ArgumentException` before any request. These are two different credentials from the same Slack app:

<!-- ikon-example: connectors-slack-socket -->
```csharp
var wsUrl = await slack.OpenSocketUrlAsync(appToken);   // xapp-..., not the xoxb- bot token
```

`Slack.ParseMessage(JsonElement, channel)` maps a raw message object — from a history page or a Socket Mode event payload — to a `SlackMessage`, returning `null` for non-message objects (no `ts`).

## GitHub

Construct `GitHub` with a token. The constructor **throws `ArgumentException` on an empty or whitespace token** — an empty token would otherwise degrade silently to unauthenticated requests, where private repositories answer 404 instead of 401. Every `repo` parameter is the `"owner/name"` form, checked before any request: anything other than two segments of letters, digits, `-`, `_` or `.` joined by one `/` (a full URL, `owner/name/extra`, a `.` or `..` segment) throws `ArgumentException`:

<!-- ikon-example: connectors-github -->
```csharp
var gitHub = new GitHub(token);
var issue = await gitHub.GetIssueAsync("ikon-ai/examples", 42);
var commentUrl = await gitHub.CommentAsync("ikon-ai/examples", 42, "Reproduced on main.");
```

### Listing by update time

`ListIssuesSinceAsync(repo, since)` returns every issue **and pull request** updated after `since` (an ISO-8601 timestamp, e.g. `"2026-01-01T00:00:00Z"`), ordered by update time ascending and paged to completion. Paging is bounded by `maxPages` (default 50 pages of 100; `maxPages <= 0` throws `ArgumentOutOfRangeException`), and reaching the bound with a full last page throws `ConnectorPageCapException<GitHubIssue>` rather than returning a shortened list: `Items` holds the pages read (ascending and gap-free, so they are safe to process) and `ResumeFrom` is the newest `UpdatedAt` among them — pass it back as the next `since` to continue. The GitHub issues API includes pull requests; `GitHubIssue.IsPullRequest` tells them apart.

`GitHubIssue.UpdatedAt` is the raw ISO-8601 string exactly as GitHub returned it. It is an **opaque cursor**: feed it back as the next `since` without parsing or reformatting it — a round-trip through `DateTime` can change the text and break resume-from-cursor paging.

`since` is boundary-**inclusive** (it returns items updated at-or-after it), so resuming with the last item's `UpdatedAt` re-returns every item that shares that exact second. Dedupe on `GitHubIssue.Number` across calls — do not assume the resumed page is all new. (This differs from Slack's `HistorySinceAsync`, whose `oldestTs` is exclusive.)

<!-- ikon-example: connectors-github-since -->
```csharp
var updated = await gitHub.ListIssuesSinceAsync("ikon-ai/examples", since: cursor);

foreach (var item in updated.Where(i => !i.IsPullRequest))
{
    if (!seenIssueNumbers.Add(item.Number))
    {
        continue;   // already processed on a previous page — since is inclusive
    }

    await ProcessIssueAsync(item);
}

if (updated.Count > 0)
{
    cursor = updated[^1].UpdatedAt;   // pass back verbatim next time
}
```

### Merging pull requests

`MergePullRequestAsync` treats a refused merge (HTTP 405/409 — not mergeable, head changed) as an **answer, not an error**: it returns `GitHubMergeResult` with `Merged: false` and GitHub's reason in `Message` instead of throwing. Always branch on `.Merged`; other HTTP failures still throw `ConnectorException`.

<!-- ikon-example: connectors-github-merge -->
```csharp
var reviewedHead = await gitHub.GetPullRequestHeadShaAsync("ikon-ai/examples", 42);   // before reading the diff
var result = await gitHub.MergePullRequestAsync("ikon-ai/examples", 42, reviewedHead, commitTitle: "Add retry policy");

if (!result.Merged)
{
    Log.Instance.Warning($"PR #42 not merged: {result.Message}");
}
```

`GetPullRequestDiffAsync` returns the PR's unified diff as text.

## Microsoft Graph: SharePoint and Entra

`MicrosoftGraph` reads a SharePoint document library together with who may read each document, and resolves a person's Entra identity to the principals those grants name. That is what an app needs to show SharePoint content only to the people SharePoint would show it to. Construct it with an app registration's tenant id, client id and client secret; an empty one throws `ArgumentException` at construction. The access token is requested on first use and again shortly before it expires.

**This is application-permission access with permission sync, not delegated access.** Every call runs as the app registration, which reads whatever its permissions admit, so Graph trims nothing per person. Your app reads each file's grants and enforces them itself. Two things follow. A permission changed in SharePoint applies in your app only once you have read that file's grants again. And a grant the connector reports as unresolved (below) names nobody you can check, so it must admit nobody.

### What to provision

In the tenant's Entra admin center: register an application, create a client secret, add these Microsoft Graph **application** permissions, and grant admin consent.

| Permission | What needs it |
|---|---|
| `Sites.Read.All` | `GetSiteAsync` and `GetGroupSiteAsync`. It also covers `ListDrivesAsync`, `ListFilesAsync`, `DownloadFileAsync` and `ListPermissionsAsync`, for which Graph names `Files.Read.All` as the least privilege. |
| `User.Read.All` | `FindUserAsync` and `ListUserGroupsAsync`. |
| `GroupMember.Read.All` | The names of the groups `ListUserGroupsAsync` returns. Without it Graph returns each group's id and an empty `DisplayName`; the ids are all that trimming needs. |

Two things to settle with the tenant's administrator rather than assume. Microsoft's guidance for scanning libraries at scale says an app needs `Sites.FullControl.All` "to process permissions correctly", while the reference for listing a file's permissions names only the read permissions above: if `ListPermissionsAsync` returns fewer grants than SharePoint shows for a file, that is the permission to add. And to confine the app registration to one site, Microsoft's `Sites.Selected` takes the place of `Sites.Read.All` once an administrator has given the application the `read` role on that site.

### Reading a library

<!-- ikon-example: connectors-graph-ingest -->
```csharp
var graph = new MicrosoftGraph(tenantId, clientId, clientSecret);
var site = await graph.GetSiteAsync("contoso.sharepoint.com", "/sites/Finance");
var library = (await graph.ListDrivesAsync(site.Id)).First(drive => drive.Name == "Documents");

var listing = await graph.ListFilesAsync(library.Id);

foreach (var file in listing.Files)
{
    var grants = await graph.ListPermissionsAsync(library.Id, file.Id);
    var content = await graph.DownloadFileAsync(library.Id, file.Id, maxBytes: 20_000_000);
    await IndexAsync(file, content, grants);
}

var deltaLink = listing.DeltaLink;   // store verbatim: it is what reads only the changes next time
```

`GetSiteAsync(hostname, sitePath)` finds a site by its address and returns it as a `MicrosoftGraphSite`; an empty path or `/` is the host's root site. `ListDrivesAsync` returns its document libraries as `MicrosoftGraphDrive` records. `ListFilesAsync` walks a library's delta feed, which is the one listing Graph guarantees complete while people keep editing, and returns a `MicrosoftGraphFileListing`: the `Files` and a `DeltaLink`. Each `MicrosoftGraphFile` has its `Id`, `Name`, `Path` inside the library, `WebUrl`, `MimeType`, `Size`, `LastModified` and `LastModifiedBy`. Only files are listed; a OneNote notebook, which Graph models as a package, is not one.

`folderPath` narrows the result to one folder, but the walk still pages the whole library, because Graph documents the delta feed for a library's root only. The walk is bounded by `maxPages` (default 50), and reaching it with more pending throws `ConnectorPageCapException<MicrosoftGraphFile>` with the files read as `Items` and the next page link as `ResumeFrom` — pass that as `deltaLink` to continue from there.

`DownloadFileAsync` takes a `maxBytes` and refuses a larger file with a `ConnectorException` instead of buffering it or handing back part of it.

### Keeping it current

<!-- ikon-example: connectors-graph-changes -->
```csharp
var changes = await graph.ListFilesAsync(driveId, deltaLink: deltaLink);

foreach (var file in changes.Files)
{
    if (file.Deleted)
    {
        await RemoveAsync(file.Id);
        continue;
    }

    var grants = await graph.ListPermissionsAsync(driveId, file.Id);
    var content = await graph.DownloadFileAsync(driveId, file.Id, maxBytes: 20_000_000);
    await IndexAsync(file, content, grants);
}

deltaLink = changes.DeltaLink;
```

Called with the `DeltaLink` of an earlier listing, `ListFilesAsync` returns only what changed since. A removed item arrives with `Deleted` set and nothing but its `Id`; a deleted folder's id can be among them, and deletions are never filtered by `folderPath`. With a `folderPath`, a changed file that is now outside the folder arrives as `Deleted` too, since it may have been moved out of it. A link too old for Graph to answer throws `ConnectorException` with `StatusCode` 410, and the remedy is a full listing. Key what you store on `Id`: a folder that is moved or renamed does not bring the files under it back through the feed, so a stored `Path` goes stale until the next full listing.

**A change to who may read is not a change to a file.** New sharing on a folder, a change to the site's permissions, a person joining or leaving a group — none of these re-reports the files they affect. Read group membership when a person asks (`ListUserGroupsAsync` is cheap), and re-read `ListPermissionsAsync` for every file you hold on the schedule that decides how stale a permission you accept.

### Who may read a file

`ListPermissionsAsync(driveId, itemId)` returns the item's effective permissions, inherited ones included, as one `MicrosoftGraphGrant` per principal. Its `Kind` says what `PrincipalId` is and whether you can check a person against it:

| `Kind` | The grant is to | Checking a person |
|---|---|---|
| `User` | an Entra user | `PrincipalId` equals the `Id` of the `MicrosoftGraphUser` from `FindUserAsync` |
| `Group` | an Entra group, security or Microsoft 365 | `PrincipalId` is among the ids from `ListUserGroupsAsync` |
| `SiteGroup` | a SharePoint site group, such as "Finance Members" | **unresolved** |
| `SiteUser` | a principal SharePoint knows and Entra does not; `LoginName` carries its claim | **unresolved** |
| `Link` | whoever holds a sharing link's URL; `LinkScope`, a `MicrosoftGraphLinkScope`, is `Anonymous`, `Organization` or `Unknown` | **unresolved** — the connector cannot know who holds it |
| `Application` | an application | not a person |
| `Unknown` | an invitation nobody has redeemed (`Email` is its address), or a shape the connector does not know | **unresolved** |

A sharing link made for named people is not a `Link` grant: it yields one grant per person named, with `LinkScope` `Users` — a `User` or `Group` grant where Entra knows them, otherwise one of the unresolved kinds. A link that only re-states access people already have yields nothing. `Roles` are Graph's own — `read`, `write` and `owner` all include reading — and `Inherited` is true when Graph named the folder a grant comes from; Graph documents that SharePoint libraries leave that out, so false does not mean the grant is set on the file itself.

<!-- ikon-example: connectors-graph-trim -->
```csharp
var user = await graph.FindUserAsync(signedInEmail);

if (user is null)
{
    return false;   // not in this tenant, so no grant can name them
}

var groupIds = (await graph.ListUserGroupsAsync(user.Id))
    .Select(group => group.Id)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

var mayRead = grants.Any(grant =>
    grant.Roles.Any(role => role is "read" or "write" or "owner")
    && grant.PrincipalId is { } principalId
    && grant.Kind switch
    {
        MicrosoftGraphPrincipalKind.User => string.Equals(principalId, user.Id, StringComparison.OrdinalIgnoreCase),
        MicrosoftGraphPrincipalKind.Group => groupIds.Contains(principalId),
        _ => false,   // site groups, links and unknown principals name nobody this app can check
    });
```

`FindUserAsync` takes an Entra object id, a user principal name or a primary mail address, returns null when nobody matches, and throws when more than one person does. `ListUserGroupsAsync` returns every group the person is in, directly or through nested groups, as `MicrosoftGraphGroup` records; owning a group is not membership of it.

**SharePoint site groups are the limit to know before you promise anything.** A site's own permissions are held by its site groups — Owners, Members, Visitors — and a library that inherits them reports its grants as `SiteGroup`. Graph gives an application no call that lists who is in a site group, so the connector reports the group by name and resolves nothing: under the rule above, a library shared only through its site's groups is readable by nobody in your app. What resolves is a grant to people or to Entra groups, so the dependable arrangement is a library, or its folders, shared with Entra groups.

For a team site connected to a Microsoft 365 group there is one more fact to use. Microsoft documents that the group's owners become site owners and its members site members, and that people can also be added to the site's groups directly. `MicrosoftGraphDrive.OwnerGroupId` names that group where Graph reports it — Graph returns it without documenting it, so confirm it by checking that `GetGroupSiteAsync` returns the same site — and `ListUserGroupsAsync` says whether a person is in it. Whether a `SiteGroup` grant called "Finance Members" is that site's members group is something the connector cannot see, and people added to the site directly are in no Entra group, so treating group membership as that grant is your app's stated rule, never the connector's.

## Procountor

`Procountor` reads customers, sales invoices and their payments from Procountor (Finago). It writes nothing.

Procountor issues credentials in two halves. The **client id, client secret and redirect URI** identify your integration and come from Procountor when you request API access. The **API key** is created in Procountor by a user of the company whose figures you read — *Basics → API client keys → New API key*, entering your client id — and binds the connector to that user's rights in that one company; a company administrator can create it for a technical user that may use the API and not the application. Keep all four in `app.Secrets`. The connector exchanges the key for an access token that lasts an hour and renews it before it expires.

<!-- ikon-example: connectors-procountor -->
```csharp
var procountor = new Procountor(clientId, clientSecret, redirectUri, apiKey);

var customers = await procountor.ListCustomersAsync();
var invoices = await procountor.ListSalesInvoicesAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

foreach (var byStatus in invoices.GroupBy(invoice => invoice.Status))
{
    Log.Instance.Info($"{byStatus.Key}: {byStatus.Count()} invoices, {byStatus.Sum(invoice => invoice.Total ?? 0)} in accounting currency");
}

var payments = await procountor.ListPaymentEventsAsync(invoices[0].Id);
```

The constructor reads production (`Procountor.ProductionBaseUrl`) unless given another `baseUrl`; `Procountor.TestBaseUrl` is Procountor's public testing server, which has its own credentials and its own data. An empty credential, or a base URL that is not `https`, throws `ArgumentException` at construction.

`ListCustomersAsync` returns the customers of the business partner register as `ProcountorBusinessPartner` records. Procountor lists active partners or deactivated ones, never both: `active: false` asks for the others.

`ListSalesInvoicesAsync(startDate, endDate)` returns the sales invoices whose invoice date falls in the range, both ends included, as `ProcountorInvoice` records: `Id`, `PartnerId` and `PartnerName`, `InvoiceNumber`, `Date`, `DueDate`, `Total`, `TotalExcludingVat`, `Currency`, `Status` and `Type`. Three contracts to respect:

- **Every status comes back**, unfinished and invalidated invoices among them. `Status` is Procountor's own word (`UNFINISHED`, `NOT_SENT`, `SENT`, `PARTLY_PAID`, `PAID`, `MARKED_PAID`, `INVALIDATED` and more); decide which ones a figure counts before you sum.
- **`Total` is in the company's accounting currency**, also for an invoice issued in another currency, so totals add up. It is null where Procountor reported no such sum.
- **There is no open amount.** A listing carries `Status` and nothing about how much has been paid. `ListPaymentEventsAsync(invoiceId)` returns the payments recorded against one invoice as `ProcountorPaymentEvent` records; what is still owed is the total less the events you count as paid, and which of them count is an accounting decision the connector does not make.

Listings page by descending id, 200 rows a page, and a row created while a listing runs is never returned twice or made to hide another. The bound is `maxPages` (default 50, so 10,000 rows; 10 for `ListPaymentEventsAsync`): reaching it with a full last page throws `ConnectorPageCapException<T>` with the rows read as `Items` and the lowest id among them as `ResumeFrom` — parse it and pass it as `previousId` to read on. Procountor documents its request limits (60 a second in production, 90 a minute on the testing server) and no throttling response; the connector waits and retries on a `429` if one comes.

## Google: Drive and Gmail

Both connectors authenticate with `GoogleCredentials(ClientId, ClientSecret, RefreshToken)` — OAuth2 refresh-token credentials; the short-lived access token is obtained and refreshed automatically by the Google client library.

`Drive` and `Gmail` are **`IDisposable` and own an `HttpClient`**: construct one instance per credential and reuse it for the credential's lifetime, rather than constructing per call.

<!-- ikon-example: connectors-google-clients -->
```csharp
var credentials = new GoogleCredentials(clientId, clientSecret, refreshToken);
using var drive = new Drive(credentials);
using var gmail = new Gmail(credentials);
```

Google failures surface two ways: a failed upload, download, or Gmail metadata fetch throws `ConnectorException` (with provider `"drive"`/`"gmail"`), while lower-level API errors surface as the Google client library's own exceptions — so catch both. Use `GoogleAuth.IsAuthFailure(ex)` on the latter to decide whether to stop retrying: it is `true` only for permanent auth failures (revoked or expired refresh token, bad client), never for transient or network errors. Listing with a `folderId` first probes the folder, so an unknown or unreadable folder throws `ConnectorException` rather than returning an empty listing; `Gmail.SendAsync` throws `ConnectorException` for a malformed recipient and `ArgumentException` for an empty `to`.

### Drive

<!-- ikon-example: connectors-drive-transfer -->
```csharp
await using var content = File.OpenRead("./report.pdf");
var uploaded = await drive.UploadAsync("report.pdf", "application/pdf", content, folderId);

await using var download = await drive.DownloadAsync(uploaded.Id);
```

`DownloadAsync` buffers the whole file in memory and works only for files with binary content: Google-native Docs, Sheets and Slides are rejected by Google with HTTP 403, surfaced as a `ConnectorException` whose `StatusCode` is null (the 403 appears only in `Message`, so the `401`/`403` rule above does not catch it), and the connector has no export.

`ListAsync(folderId, limit)` returns **at most `limit`** files, paging as needed and **excluding trashed files**; a result of exactly `limit` files means more may exist, and `limit <= 0` returns an empty list. Use it only for a bounded "recent files" peek. For a complete or filtered listing use `ListAllAsync`, which pages through the entire result set, includes trashed files unless the query excludes them, and accepts an extra Drive query clause:

<!-- ikon-example: connectors-drive-list -->
```csharp
await foreach (var file in drive.ListAllAsync(folderId, extraQuery: "trashed = false"))
{
    Log.Instance.Info($"{file.Name} ({file.MimeType}, modified {file.ModifiedTime:O})");
}
```

Either listing yields `DriveFile` records: `Id`, `Name`, `MimeType`, an optional `Size` and `ModifiedTime`, and a `WebViewLink` for opening the file in Drive. The `extraQuery` clause is Drive query syntax — `"trashed = false"` excludes trashed files, `"modifiedTime > '2024-01-01T00:00:00'"` bounds a historical backfill by time.

### Gmail

<!-- ikon-example: connectors-gmail -->
```csharp
var unread = await gmail.ListAsync("is:unread", limit: 10);

foreach (var email in unread)
{
    var body = await gmail.GetBodyAsync(email.Id);
    Log.Instance.Info($"{email.From}: {email.Subject}");
}

var sentId = await gmail.SendAsync("someone@example.com", "Weekly summary", bodyText, cc: "team@example.com");
```

`ListAsync` fetches up to `limit` matching messages; `ListAllAsync` streams the entire result set — bound a backfill with query date operators such as `"after:2024/01/01"`. Both fetch message metadata in batches, and **if any single message fetch in a batch fails, the whole call throws `ConnectorException`** — a partially populated list is never returned.

Two field contracts to respect:

- `EmailSummary.ReceivedAt` is null when Gmail supplies no internal date. Handle it before sorting or displaying by date.
- `GetBodyAsync` returns an `EmailBody`: `Text` is the `text/plain` part when present (`IsHtml` false), else the **raw HTML** of the `text/html` part (`IsHtml` true, not converted to text), else an empty string.

## Browser

`Ikon.Connectors.Browser` operates a real (Playwright-driven) browser. There are two entry points; pick by who is driving:

| Entry point | Who drives | Use when |
|---|---|---|
| `WebAgent.OperateAsync` | An LLM agent subthread | You have an objective in natural language and want the agent to figure out the clicks. Needs an `AgentThread` (from `Ikon.Agent`) and a registered browser-operator persona. |
| `BrowserSession` | Your code | You know the exact actions — scripted navigation, screenshots, page evaluation. No LLM involved. |

### Agentic operation

Register the persona `BrowserOperatorPersona.Create()` returns on your app's orchestrator (its default name, `"browser-operator"`, matches `OperateAsync`'s default `personaName`). Then hand the agent an objective:

<!-- ikon-example: connectors-web-agent -->
```csharp
var run = await WebAgent.OperateAsync(
    thread,                                    // an AgentThread from Ikon.Agent
    "https://portal.example.com",
    "Log in with the provided credentials and extract the current account balance",
    new WebAgentOptions(PublicInternetOnly: true, MaxPasses: 25, Headless: true));

if (run.Outcome == WebOutcome.Succeeded)
{
    var balance = run.Outputs["balance"];
}
```

`WebRun` carries the `Outcome` (`Succeeded`, `Failed`, or `BudgetExhausted` when `MaxPasses` ran out), a `Summary`, the full action trace in `Steps`, any `Extract`ed `Outputs`, and `Looks` — the count of on-demand vision inspections, which consume agent budget without appearing in the trace.

### Sites you do not control

A site that is not your own app decides what the browser loads next, and the agent can press anything on it. Three options make that safe to hand to a person:

- `PublicInternetOnly: true` confines the browser to public addresses. Every request is made by the platform's guarded HTTP client, so no page can reach the network your app runs in, and certificates are validated. Every `WebAgentOptions` states it; `false` is only for your own app on localhost or a private address.
- `ReviewWrite` is asked before every action that could change something on the site — a click on a submit, send, pay or delete control, Enter outside a search field, and anything the classifier does not recognise. The action runs only on `WebApproval.Allow`; `WebApproval.Deny(reason)` is reported to the agent, which does not try it again. The `WebActionReview` carries a one-line `Description` and a JPEG `Screenshot` of the page. Nobody answering must be a refusal, so bound the wait.
- `OnProgress` hands you a `WebProgress` — step number, URL, what just happened, and a JPEG `Screenshot` — after every observation, for a live view.

<!-- ikon-example: connectors-web-agent-review -->
```csharp
var run = await WebAgent.OperateAsync(
    thread,
    "https://supplier.example.com/orders",
    "Reorder last month's printer paper",
    new WebAgentOptions(
        PublicInternetOnly: true,
        MaxPasses: 40,
        ReviewWrite: async (review, ct) => await askPerson(review.Description, review.Screenshot, ct)
            ? WebApproval.Allow
            : WebApproval.Deny("the person declined it"),
        OnProgress: progress =>
        {
            liveView.Value = progress.Screenshot;
            return Task.CompletedTask;
        }));
```

Typing into a field is not a write, because on most sites nothing is committed until a submit; a field that saves as you type is the case the classifier cannot see.

### Manual driving

`BrowserSession` owns the browser lifecycle: start once, dispose to release the process. `WebTarget` resolution tries the perception mark first, then accessibility role + name, then a CSS/XPath selector — populate whichever you know.

<!-- ikon-example: connectors-browser-session -->
```csharp
await using var session = new BrowserSession();
await session.StartAsync(headless: true);
await session.NavigateAsync("https://example.com/login");

var marks = await session.MarkElementsAsync();
var result = await session.ExecuteAsync(
    new WebAction.Fill(new WebTarget(Role: "textbox", Name: "Email"), "user@example.com"));

if (!result.Ok)
{
    Log.Instance.Warning($"Action failed: {result.Failure}");            // caller-actionable diagnosis
    Log.Instance.Warning(string.Join("\n", session.ConsoleTail));       // the page's own account
}
```

The action vocabulary is a tagged union: `Navigate`, `Click`, `Fill`, `FillLogin`, `FillDetail`, `UsePasskey`, `Press`, `Scroll`, `Extract` (which records the target's inner text under an output name), `Upload`, `Select`, `Hover`, `Back`, `ClickAt`, `AnswerDialog` and `ReadVisible`. `ScreenshotAsync` returns a PNG; prefer `ScreenshotJpegAsync` when the image goes into an LLM context. `ConsoleTail` holds the last ~40 console messages, page errors, and failed requests — the first place to look when a page that "should" render stays blank.

### A browser somewhere else

The agent needs only an `IWebPage` — navigate, screenshot (PNG and JPEG), mark the elements, execute an action, the current URL, stage files and take downloads, report saved logins, and dispose — and `BrowserSession` is the one in your process. `WebAgentOptions.OpenPage` hands a run a page that lives elsewhere instead, such as a browser on a person's own computer driven over a connection of your own; the run disposes the page it opened, and `Headless` and `PublicInternetOnly` are then for the opener to honour. A page reached over a network should also implement `IWebPage.ObserveAsync`, which returns the marks and a screenshot as one `WebObservation`: the agent observes after every step, and the default asks for each in turn.

`BrowserSession.StartPersistentAsync(profileDirectory, headless)` starts on a profile kept in a directory, so its cookies, saved passwords and sign-ins survive from one session to the next: a person signs in to a site once in that profile and every later run is signed in. Only one session can hold a profile at a time; `NewTabAsync` opens another tab on the same profile, so several agents can work at once on one set of sign-ins, and disposing a tab closes only that tab. `Closed` is raised when the person closes the window, or the tab. `AddCookiesAsync` puts `BrowserCookie`s into the session's jar — into the profile, for a persistent session — which is how a host brings in the sign-ins of the browser the person already uses.

### Files in and out

A run can hand a site files and bring files back. `WebAgentOptions.Files` lists `WebFile`s — a name, a MIME type and the bytes — that the agent may put into a page's file input with `WebAction.Upload`; the agent is told their names, and an upload is a write, so `ReviewWrite` is asked first. Whatever the pages download during the run, including a url that is itself a file and a PDF the browser would only have shown, comes back in `WebRun.Downloads`. Driving a page yourself, `IWebPage.StageFileAsync` hands it a file to upload and `TakeDownloadsAsync` returns each download once.

### Saved logins

An agent signs in without ever seeing a password. Give the page an `ILoginVault` — `BrowserSession.Logins` — and each observation lists the `SavedLogin`s that cover the current page, by id and label only. The agent calls `use_login`, which is `WebAction.FillLogin(target, loginId, field)` with a `LoginField` of `Username`, `Password` or `OneTimeCode`, and the browser asks the vault for that one value at the moment it fills the field. The value never reaches the model, the step trace or a distilled flow, so a replayed flow signs in again through the vault.

A login fills only where `SavedLogin.Covers` holds for the document the field is in — https on the login's site or a subdomain of it, plain http only on loopback — so a frame from another site, or a look-alike host, gets nothing; a password fills only into a password field. `ILoginVault.RevealAsync` returns null to refuse, and a vault that holds the secret checks the page itself rather than trusting its caller. `IWebPage.SavedLoginsAsync` is what a page reports; a page on a person's computer answers from the vault there. A `SavedLogin` with `AskFirst` is filled only after `ReviewWrite` approves it, as a write would be; with no reviewer the agent is told to leave the sign-in to the person. `WebAgent.ReplayAsync` stops at a step that fills such a login, since a replay asks nobody.

To keep a sign-in rather than a password, `BrowserSession.ExportStorageStateAsync` returns the session's cookies and storage, and `BrowserSession.StartAsync` takes them back as `storageState`; treat that text as a credential. For a tab a person signs in on themselves, `BrowserSession.OfferToSaveSignInsAsync` notices the password being submitted and, on the next page, offers to save it, calling you with the site, the username and the password when they accept. `BrowserSession.OfferToSaveAuthenticatorsAsync` does the same for an authenticator being set up: it reads the `otpauth://totp/` address from the page's QR code or link and offers to keep the secret with a login already saved for that site, which you name through its first callback.

### Saved passkeys

A passkey signs in the same way, without the agent holding the key. Give the page an `IPasskeyVault` — `BrowserSession.Passkeys` — and each observation lists the `SavedPasskey`s that cover the current page. The agent calls `use_passkey`, which is `WebAction.UsePasskey(passkeyId)`: the browser takes the `PasskeyKey` from the vault into an authenticator it runs for that page in place of the computer's own, and the site's own passkey sign-in, pressed next, succeeds with nobody touching anything. After each sign-in `IPasskeyVault.UsedAsync` gets the count the site has now seen.

A passkey is used only where `SavedPasskey.Covers` holds for the page, and the browser itself gives a key to no site but the one it was made for. A `SavedPasskey` has `AskFirst` unless the vault says otherwise, so `ReviewWrite` is asked before it is made ready, and with no reviewer the agent is told to leave the sign-in to the person; `WebAgent.ReplayAsync` stops at such a step. While a passkey is ready on a page, the person's own security key or device cannot answer there.

For a tab a person signs in on themselves, `BrowserSession.OfferToSavePasskeysAsync` asks at the moment a site makes a passkey whether to keep it: on yes the browser's authenticator makes it and you get the `PasskeyKey`, on no the person's own device makes it as it would have. Treat a `PasskeyKey` as the credential it is. `IWebPage.SavedPasskeysAsync` is what a page reports.

### Saved cards and details

The same holds for a payment card and for the person's name and address. Give the page an `IDetailVault` — `BrowserSession.Details` — and an observation of a page with a form lists each `SavedDetail` by id, kind (`SavedDetail.Card` or `SavedDetail.Identity`), label and the names of its fields, never a value. The agent calls `use_detail`, which is `WebAction.FillDetail(target, detailId, field)`, and the browser asks the vault for that one field as it fills it.

<!-- ikon-example: connectors-saved-detail -->
```csharp
await using var session = new BrowserSession { Details = detailVault };
await session.StartAsync(headless: true);
await session.NavigateAsync("https://shop.example/checkout");

var filled = await session.ExecuteAsync(
    new WebAction.FillDetail(new WebTarget(Role: "textbox", Name: "Card number"), "personal-visa", "number"));
```

A detail fills only where `SavedDetail.MayFill` holds both for the page and for the document the field is in — https, or plain http on loopback — and that document may be a frame from another host, as a payment provider's card form is. A field where `SavedDetail.IsSecret` holds, a card's number and security code, is masked on the page once filled, and no filled field's value is read back into an observation. In an agent run the person is asked through `ReviewWrite` before the first fill of a detail on a site, and the rest of that detail's fields then fill there without asking again; with no reviewer the fill is refused and the agent is told to leave the form to the person. Submitting the form is a write of its own, reviewed as any other. `WebAgent.ReplayAsync` refuses a flow that holds a `WebAction.FillDetail`, since a replay asks nobody. `IWebPage.SavedDetailsAsync` is what a page reports, and a page that keeps none reports none.

### Distill and replay

A successful `WebRun` can be **distilled** into a `WebFlow` — a deterministic, replayable integration — and replayed **without an LLM**:

<!-- ikon-example: connectors-replay -->
```csharp
var flow = WebAgent.Distill(run, name: "portal-balance");
// ... persist flow (it serializes losslessly), later:
var replay = await WebAgent.ReplayAsync(flow, new Dictionary<string, string>
{
    ["email"] = accountEmail,
    ["password"] = accountPassword,
}, headless: true, publicInternetOnly: true);   // as the run it was distilled from

if (replay.Ok)
{
    var balance = replay.Outputs["balance"];
}
```

Distillation keeps only the steps that succeeded and parameterizes each filled field into a named input slot (`WebFlow.Inputs`); slot names are slugs of the field's accessible name (`"Password"` becomes `password`). A `Fill` marked `Secret` is stored **redacted** everywhere the trace is persisted — the step trace, the distilled flow JSON, logs — so the flow never carries the credential. That means every slot **must** be supplied in `inputs` at replay — a missing one, secret or not, fails upfront with `ConnectorException` rather than typing a recorded or placeholder value into the field, and a key that names no slot is rejected the same way, so a misspelt input can never be silently ignored. Replay failures are ordinary results, not exceptions — check `WebReplay.Ok`. Pass `publicInternetOnly` as the run the flow was distilled from had it: the overload without it replays with `PublicInternetOnly` off, private addresses reachable and certificates unchecked, which is for your own app only.

`WebAgent.ReplayAsync(page, flow, inputs)` replays on an `IWebPage` you opened and still own — a `BrowserSession`, or a page from the same opener you give `WebAgentOptions.OpenPage` — and leaves it open. A replay asks nobody before a step, so check `WebAgent.WritesIn(flow)` before replaying unattended: it names, in an approval's words, each step an agent run would have asked a person about, and is empty for a flow that only reads.

A fact on a page that is not a control — a heading, a price, a count — has no mark, so the agent reads it with the `read` tool by the words it shows. The recorded `Extract` keeps the element's structural path in `WebTarget.Selector`, and a replay reads that element first, so it returns next week's price rather than looking for this week's. To read a whole result list — every name and price on screen — the agent uses `read_visible`, recorded as `WebAction.ReadVisible`: the text in view in reading order, including text a shop gives only to screen readers, far cheaper than looking at a screenshot. Whatever the agent reads is also shown back to it, not only kept in `WebRun.Outputs`.

Some steps only the person can take: a code sent to their phone, a passkey, a CAPTCHA, signing in where no saved login exists. With `WebAgentOptions.HandToPerson`, the agent hands the page over — your callback gets a sentence saying what is needed, shows it to the person, and returns true once they have done it in that browser — and the run carries on from the page as they left it. Leave it null for a browser the person cannot reach; the agent then finishes and says what is left. A cloud browser can be made reachable: `BrowserSession.StartScreencastAsync` streams the page as JPEG frames while it changes, and `TapAsync`, `PressKeyAsync`, `TypeTextAsync` and `ScrollAsync` act on the page as the person watching it would, so an app can show the page live and let them sign in there themselves. `WebAgentOptions.Steering` is a `WebRunSteering` you keep: whatever you `Tell` it while the run works reaches the agent with its next observation, so a person can correct a task in flight.

To work in the browser a person already uses, signed in where they are, drive a tab of their own Chrome through the Ikon Connect extension. `ChromeExtensionRelay` listens on localhost for the extension — `ChromeExtensionRelay.WriteExtension` writes the person's copy, which carries the relay's key — and `BrowserSession.StartInChromeAsync` opens a tab there, in a tab group of its own, and drives it as any other session; disposing the session closes that tab and nothing else of theirs. Nothing of the session's is put on their browser: no user agent, no script, no network guard. A file the tab downloads goes to the person's downloads folder, as their browser does it, and comes through `TakeDownloadsAsync` and `WebRun.Downloads` as well once it is whole: the relay, on the same computer, reads it from there. `ChromeExtensionRelay.PageShared` hands you a `SharedPage` when the person sends the page they are on from the extension's button.

An address someone guessed can be wrong. With `WebAgentOptions.StartFromSiteRootOnMissingPage`, a starting address that answers 404 or 410 starts the agent on the site's home page, told why, instead of failing the run; leave it off when the address is the thing under test.

### What the browser hands back

A run's trace is a list of `WebStep` — the `WebAction` attempted, the `ResolvedSelector` it actually
landed on, and whether it was `Ok`. Resolution tries the perception mark id first, then accessibility
role and name, then a CSS or XPath selector, which is what lets a distilled flow still find an
element after the marks have gone stale. A `WebTarget` with a `Name` and no `Role` names an element
by the text it shows; it resolves by its `Selector` first, then by that text. Driving the page manually returns a `WebActionResult`
instead: `Ok`, the `Selector` used, whatever was `Extracted`, and a `Failure` string when it did not
work — a failed action is a result, not an exception. Perception returns `MarkedElement` records, one
per interactive element, each with the numeric `Mark` the model refers to it by plus the `Role`,
`Name` and `Selector` behind it.
