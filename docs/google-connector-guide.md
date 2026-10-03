# Google Connector Guide
<!-- checked-against: ba6242f0cf67d64f -->
This guide covers `Ikon.Connectors.Google` — Google Drive and Gmail — for app developers wiring a person's Google account into an Ikon app.

## Google: Drive and Gmail

Both connectors authenticate with `GoogleCredentials(ClientId, ClientSecret, RefreshToken)` — OAuth2 refresh-token credentials; the short-lived access token is obtained and refreshed automatically by the Google client library.

`Drive` and `Gmail` are **`IDisposable` and own an `HttpClient`**: construct one instance per credential and reuse it for the credential's lifetime, rather than constructing per call.

<!-- ikon-example: connectors-google-clients -->
```csharp
var credentials = new GoogleCredentials(clientId, clientSecret, refreshToken);
using var drive = new Drive(credentials);
using var gmail = new Gmail(credentials);
```

Both take an optional `HttpMessageHandler` as their second argument, for a proxy or a test double. The token refresh goes through it too, and disposing a client leaves it as it was, so one handler can serve several clients.

Every Google failure is a `ConnectorException` (from `Ikon.Connectors`) with provider `"google"`, the Google client library's own exception as its `InnerException` and Google's error reason (`notFound`, `insufficientPermissions`, `fileNotDownloadable`) as `ErrorCode`. `IsReconnectRequired` (`401`/`403`) means the person has to reconnect rather than retry, and `IsTransient` (`408`, `429`, `5xx`) that the same call may succeed later. A refresh token Google refuses for good (`invalid_grant`, a bad client) is a `401`. Google answers a rate limit with `403` as well as `429`; both are retried three times, waiting the `Retry-After` or backing off, and one that outlasts the retries surfaces as `429`, so a Google `403` that reaches you is an access failure. `GoogleAuth.IsAuthFailure(ex)` remains for code that catches broader exceptions: it is `true` only for permanent auth failures, never for transient or network errors. Listing with a `folderId` first probes the folder, so an unknown or unreadable folder throws `ConnectorException` rather than returning an empty listing; `Gmail.SendAsync` throws `ConnectorException` for a malformed recipient and `ArgumentException` for an empty `to`.

### Signing in

`GoogleAuth.AuthorizeUrl` builds the consent URL and `GoogleAuth.ExchangeCodeAsync` redeems the `code` for `GoogleCredentials`. The URL always asks for offline access with the consent screen, because Google issues a refresh token only on a consent; an exchange that still gets none throws rather than hand back credentials nothing could renew. `PkceCodes.Create()` (from `Ikon.Connectors`) adds PKCE: pass its `Challenge` to `AuthorizeUrl` and keep its `Verifier` with the sign-in's state for `ExchangeCodeAsync`, so a code intercepted on its way back is useless to whoever took it:

<!-- ikon-example: connectors-google-signin -->
```csharp
var pkce = PkceCodes.Create();
await SaveSignInAsync(state, pkce);
var signInUrl = GoogleAuth.AuthorizeUrl(clientId, redirectUri, ["https://www.googleapis.com/auth/gmail.readonly"], state, pkce.Challenge);

// ... the person consents; the redirect back carries `code` and `state`:
var verifier = (await LoadSignInAsync(state)).Verifier;
var credentials = await GoogleAuth.ExchangeCodeAsync(clientId, clientSecret, code, redirectUri, codeVerifier: verifier);
using var gmail = new Gmail(credentials);
```

### Drive

<!-- ikon-example: connectors-drive-transfer -->
```csharp
await using var content = File.OpenRead("./report.pdf");
var uploaded = await drive.UploadAsync("report.pdf", "application/pdf", content, folderId);

await using var download = await drive.DownloadAsync(uploaded.Id);
```

`DownloadAsync` buffers the whole file in memory and works only for files with binary content. A Google-native Doc, Sheet or Slides deck (`MimeType` starting `application/vnd.google-apps.`) has none: downloading one throws a `ConnectorException` with a null `StatusCode` whose message names `ExportAsync`. Convert those with `ExportAsync(fileId, targetMimeType)`, which is buffered the same way and takes any export format Google offers for the file's type — `text/plain` for Docs and Slides, `text/csv` for Sheets (first sheet only), `application/pdf` for any of them. Google refuses an export over 10 MB.

<!-- ikon-example: connectors-drive-export -->
```csharp
if (file.MimeType == "application/vnd.google-apps.document")
{
    await using var exported = await drive.ExportAsync(file.Id, "text/plain");
    using var reader = new StreamReader(exported);
    var text = await reader.ReadToEndAsync();
}
```

`ListAsync(folderId, limit)` returns **at most `limit`** files, paging as needed and **excluding trashed files**; a result of exactly `limit` files means more may exist, and `limit <= 0` returns an empty list. Use it only for a bounded "recent files" peek. For a complete or filtered listing use `ListAllAsync`, which pages through the entire result set, includes trashed files unless the query excludes them, and accepts an extra Drive query clause:

<!-- ikon-example: connectors-drive-list -->
```csharp
await foreach (var file in drive.ListAllAsync(folderId, extraQuery: "trashed = false"))
{
    Log.Instance.Info($"{file.Name} ({file.MimeType}, modified {file.ModifiedTime:O})");
}
```

Either listing yields `DriveFile` records: `Id`, `Name`, `MimeType`, an optional `Size` and `ModifiedTime`, and a `WebViewLink` for opening the file in Drive. The `extraQuery` clause is Drive query syntax — `"trashed = false"` excludes trashed files, `"modifiedTime > '2024-01-01T00:00:00'"` bounds a historical backfill by time.

To follow a drive without listing it again, `GetStartPageTokenAsync` marks now, and `ChangesAsync(pageToken)` returns `DriveChanges`: every `DriveChange` since, each file once in its current state — including its `Parents` and whether it is `Trashed`, so filter on a folder yourself — or `Removed` when the credential lost it. Store `NewStartPageToken` for the next call. A shared drive's changes are in its own feed: pass the id `GetSharedDriveIdAsync` returns for a folder there to both calls. Past `maxPages` the `ConnectorPageCapException<DriveChange>` carries the page token to continue from as `ResumeFrom`:

<!-- ikon-example: connectors-drive-changes -->
```csharp
var changes = await drive.ChangesAsync(storedToken ?? await drive.GetStartPageTokenAsync());

foreach (var change in changes.Items)
{
    if (change.File is { Trashed: false } file && file.Parents?.Contains(folderId) == true)
    {
        Log.Instance.Info($"changed: {file.Name}");
    }
}

storedToken = changes.NewStartPageToken;
```

Shared drives work like My Drive. A `folderId` inside a shared drive is listed from that drive, and `ListSharedDrivesAsync` returns a `SharedDrive` for each shared drive the credential belongs to, whose `Id` is also its root folder's id — pass it as `folderId` to list the drive. A listing with no `folderId` is Google's user corpus: My Drive and what is shared with the credential, not every shared drive's files.

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

`ListAsync` fetches up to `limit` matching messages; `ListPageAsync` returns the same page as an `EmailPage` that also says whether more match (`HasMore`) and carries the `NextPageToken` to pass back for the next page; `ListAllAsync` streams the entire result set — bound a backfill with query date operators such as `"after:2024/01/01"`. All of them fetch message metadata in batches of 50, Gmail's safe size, and a message Gmail throttled inside a batch is fetched again in a later one. **If any message still cannot be fetched, the whole call throws `ConnectorException`** — a partially populated list is never returned.

To read only what arrived since the last look, `GetHistoryIdAsync` marks now and `HistoryAsync(startHistoryId)` returns a `GmailHistory`: the `AddedMessageIds` and `DeletedMessageIds` since, and the `HistoryId` to start from next. Gmail keeps history for about a week, and a start older than that is a `404`:

<!-- ikon-example: connectors-gmail-history -->
```csharp
var start = storedHistoryId ?? await gmail.GetHistoryIdAsync();   // first run: from now on

try
{
    var history = await gmail.HistoryAsync(start);

    foreach (var messageId in history.AddedMessageIds)
    {
        await IngestMailAsync(messageId);
    }

    storedHistoryId = history.HistoryId;
}
catch (ConnectorException ex) when (ex.StatusCode == 404)
{
    storedHistoryId = null;   // older than Gmail keeps: read again with a query, then start fresh
}
```

Two field contracts to respect:

- `EmailSummary.ReceivedAt` is null when Gmail supplies no internal date. Handle it before sorting or displaying by date.
- `GetBodyAsync` returns an `EmailBody`: `Text` is the `text/plain` part when present (`IsHtml` false), else the **raw HTML** of the `text/html` part (`IsHtml` true, not converted to text), else an empty string. It also carries the message's own `From`, `Subject` and `Date` headers.
