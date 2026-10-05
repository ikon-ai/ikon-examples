<!-- checked-against: a8d8f6c74e706976593af41b -->

# Google Connector Guide

This guide covers `Ikon.Connectors.Google` — Google Workspace's Drive, Gmail, Calendar, Docs, Sheets, Slides, Contacts, Tasks, Meet, Chat and Forms, over Google's REST APIs — for app developers wiring a person's Google account into an Ikon app.

## Google Workspace

The types are in namespace `Ikon.Connectors.Google`. Each client takes the person's `GoogleCredentials(ClientId, ClientSecret, RefreshToken)` and an optional `HttpClient` (a shared one otherwise), and needs no disposing. Access tokens come from a `GoogleTokenProvider`: pass one provider to every client of a credential so they share its tokens, and give it an `onRefreshTokenRotated` callback to persist the refresh token should Google issue a new one. `GoogleTokenProvider.ForAccessToken` wraps an access token obtained elsewhere, such as from Google's OAuth Playground while developing; it never refreshes, so calls fail with `401` once the token expires, about an hour after it was issued.

<!-- ikon-example: connectors-google-clients -->
```csharp
var tokens = new GoogleTokenProvider(new GoogleCredentials(clientId, clientSecret, refreshToken), onRefreshTokenRotated: SaveRefreshTokenAsync);
var drive = new Drive(tokens);
var gmail = new Gmail(tokens);
```

Every Google failure is a `ConnectorException` (from `Ikon.Connectors`) with provider `"google"` and Google's error reason (`notFound`, `insufficientPermissions`, `fileNotDownloadable`) as `ErrorCode`. `IsReconnectRequired` (`401`/`403`) means the person has to reconnect or be granted access rather than retry, and `IsTransient` (`408`, `429`, `5xx`) that the same call may succeed later. A refresh token Google refuses for good (`invalid_grant`, a bad client) is a `401`. Google answers a rate limit with `403` as well as `429`; both are retried three times, waiting the `Retry-After` or a doubling wait, and one that outlasts the retries surfaces as `429`, so a Google `403` that reaches you is an access failure. Ids are passed as Google returned them; one holding `/`, `?`, `#`, `%` or `\` throws `ArgumentException` before any request, except that a calendar id or sharing rule id may hold `#` and `%`.

### Signing in

`GoogleAuth.AuthorizeUrl` builds the consent URL and `GoogleAuth.ExchangeCodeAsync` redeems the `code` for `GoogleCredentials`. The URL always asks for offline access with the consent screen, because Google issues a refresh token only on a consent; an exchange that still gets none throws rather than hand back credentials nothing could renew. `PkceCodes.Create()` (from `Ikon.Connectors`) adds PKCE: pass its `Challenge` to `AuthorizeUrl` and keep its `Verifier` with the sign-in's state for `ExchangeCodeAsync`, so a code intercepted on its way back is useless to whoever took it. Google lets the person untick scopes on the consent screen, so check what they granted with `GetGrantedScopesAsync`:

<!-- ikon-example: connectors-google-signin -->
```csharp
var pkce = PkceCodes.Create();
await SaveSignInAsync(state, pkce);
var signInUrl = GoogleAuth.AuthorizeUrl(clientId, redirectUri, ["https://www.googleapis.com/auth/gmail.readonly"], state, pkce.Challenge);

// ... the person consents; the redirect back carries `code` and `state`:
var verifier = (await LoadSignInAsync(state)).Verifier;
var credentials = await GoogleAuth.ExchangeCodeAsync(clientId, clientSecret, code, redirectUri, codeVerifier: verifier);
var tokens = new GoogleTokenProvider(credentials);

if (!(await tokens.GetGrantedScopesAsync()).Contains("https://www.googleapis.com/auth/gmail.readonly"))
{
    Log.Instance.Info("Mail access was not granted; ask again or carry on without it");
}
```

`GoogleAuth.RevokeAsync(token)` withdraws the grant when the person disconnects; it takes the refresh token or an access token of the grant, and a token Google no longer knows counts as revoked.

### Drive files

`GetFileAsync` reads one file's `DriveFile`: `Id`, `Name`, `MimeType`, and where Drive names them `Size`, `ModifiedTime`, `CreatedTime`, `Parents`, `DriveId`, `Md5Checksum`, `OwnerEmails` and a `WebViewLink` for opening it in Drive; `IsFolder` and `IsGoogleNative` say what kind of file it is.

`UploadAsync(folderId, name, mimeType, content, size)` creates a new file — Drive allows duplicate names, so a name already in the folder gets a second file beside it. Up to 5 MB goes in one request and anything larger through a resumable session. `DownloadAsync` streams a file's content; dispose the stream to release the connection. `DownloadBytesAsync(fileId, maxBytes)` reads it whole and refuses rather than truncates a file larger than `maxBytes`.

<!-- ikon-example: connectors-drive-transfer -->
```csharp
await using var content = File.OpenRead("./report.pdf");
var uploaded = await drive.UploadAsync(folderId, "report.pdf", "application/pdf", content, content.Length);

await using var download = await drive.DownloadAsync(uploaded.Id);
```

A Google-native Doc, Sheet or Slides deck (`MimeType` starting `application/vnd.google-apps.`) has no binary content: downloading one throws with `ErrorCode` `fileNotDownloadable`. Convert those with `ExportAsync(fileId, targetMimeType)`, streamed the same way, to any format Google offers for the file's type — `text/plain`, `text/markdown` or docx for Docs, `text/csv` (first sheet only) or xlsx for Sheets, `text/plain` or pptx for Slides, `application/pdf` for any of them; `GetExportFormatsAsync` lists them all. An export Google's endpoint refuses as over 10 MB is fetched through the file's own export link instead.

<!-- ikon-example: connectors-drive-export -->
```csharp
if (file.MimeType == "application/vnd.google-apps.document")
{
    await using var exported = await drive.ExportAsync(file.Id, "text/markdown");
    using var reader = new StreamReader(exported);
    var markdown = await reader.ReadToEndAsync();
}
```

The overload of `UploadAsync` taking `DriveUploadOptions` converts an upload into a Google-native file with `ConvertTo` — a docx into a Doc, an xlsx or csv into a Sheet, a pptx into Slides. `UpdateContentAsync` replaces a file's content with a new revision, keeping its id, place and sharing.

### Organising files

`CreateFolderAsync`, `CreateShortcutAsync`, `RenameAsync`, `MoveAsync` (out of every folder the file was in, into one) and `CopyAsync` (`copyComments: true` brings a native file's comments along) return the resulting `DriveFile`. `UpdateAsync` changes the name, description, star and the app's private `appProperties`, sending only the arguments given. `DeleteAsync` moves a file to the bin, `RestoreAsync` brings it back, and `PermanentDeleteAsync` deletes it for good. A shared drive is created with `CreateSharedDriveAsync` and managed with `RenameSharedDriveAsync`, `HideSharedDriveAsync`, `UnhideSharedDriveAsync` and `DeleteSharedDriveAsync`, which Drive allows only on an empty drive.

### Sharing

`ListPermissionsAsync` returns who may access a file, folder or shared drive as `DrivePermission` records: the `DrivePermissionType` (a user, group, domain or anyone with the link), the `DriveRole`, and whether it is `Inherited` from a folder above, where it is changed instead. `AddPermissionAsync` grants a `NewDrivePermission` — `User`, `Group`, `ForDomain` or `Anyone` — with an optional expiry and notification message; `UpdatePermissionAsync` and `RemovePermissionAsync` change and withdraw one, and `TransferOwnershipAsync` hands a file to someone else. Requests for access arrive as `DriveAccessProposal` records from `ListAccessProposalsAsync`, which `ResolveAccessProposalAsync` accepts or denies.

<!-- ikon-example: connectors-drive-share -->
```csharp
await drive.AddPermissionAsync(fileId, NewDrivePermission.User("ada@example.com", DriveRole.Commenter) with { EmailMessage = "Your thoughts by Friday?" });

foreach (var permission in await drive.ListPermissionsAsync(fileId))
{
    if (permission.Type == DrivePermissionType.Anyone && !permission.Inherited)
    {
        await drive.RemovePermissionAsync(fileId, permission.Id);   // close the public link
    }
}
```

### Comments, revisions and labels

`ListCommentsAsync` returns a file's `DriveComment` records with their `DriveReply` replies and the `QuotedContent` each is about; `CreateCommentAsync`, `UpdateCommentAsync`, `DeleteCommentAsync`, `AddReplyAsync`, `ResolveCommentAsync` and `ReopenCommentAsync` take part in the discussion. A comment created through the API sits on the whole Doc, Sheet or Slides deck, since Google keeps those editors' anchors private.

`ListRevisionsAsync` returns a file's `DriveRevision` history; `DownloadRevisionAsync` streams an old binary version, `KeepRevisionAsync` keeps one from being pruned and `DeleteRevisionAsync` removes one.

An organisation's labels classify files: `ListLabelsAsync` returns the `DriveLabel` definitions with their `DriveLabelField` fields and `DriveLabelChoice` choices (it needs the `drive.labels.readonly` scope), `ListFileLabelsAsync` the `DriveFileLabel` values on a file, and `ModifyLabelsAsync` applies `DriveLabelChange` edits — `Apply`, `Remove`, `SetText`, `SetInteger`, `SetDate`, `SetSelection`, `SetUser`, `Unset` — all in one request.

`DriveActivity` reads what was done to a file (`QueryItemAsync`) or to anything under a folder (`QueryFolderAsync`): each `DriveActivityEvent` names its action, who did it and the files it touched. It needs the restricted `drive.activity.readonly` scope.

<!-- ikon-example: connectors-drive-activity -->
```csharp
foreach (var activityEvent in await activity.QueryFolderAsync(folderId, since: DateTimeOffset.UtcNow.AddDays(-7)))
{
    Log.Instance.Info($"{activityEvent.Time:u} {activityEvent.Action}: {string.Join(", ", activityEvent.TargetTitles)}");
}
```

### Listing and searching

`ListChildrenAsync(folderId)` returns every file directly in a folder, trashed ones left out; `root` names My Drive's root, and a shared drive's id its root. An unknown or unreadable folder throws with its `StatusCode` rather than listing nothing, which Drive's own answer cannot tell apart from an empty folder. `SearchAsync(query)` returns every file matching a Drive query; `DriveQuery` builds the common clauses with their quoting (`FullTextContains`, `NameContains`, `InFolder`, `MimeTypeIs`, `ModifiedAfter`, `NotTrashed`), `DriveQuery.And` joins them and `DriveQuery.Or` offers alternatives, such as several folders' `InFolder` (Drive has no clause for a whole folder tree). `SearchPageAsync` can read a page in a `DriveSort` order, such as the newest first. A search covers My Drive and what is shared with the credential; pass a `sharedDriveId` to search one shared drive, each of which `ListSharedDrivesAsync` lists as a `SharedDrive`.

<!-- ikon-example: connectors-drive-list -->
```csharp
foreach (var file in await drive.ListChildrenAsync(folderId))
{
    Log.Instance.Info($"{file.Name} ({file.MimeType}, modified {file.ModifiedTime:O})");
}

var budgets = await drive.SearchAsync(DriveQuery.And(
    DriveQuery.FullTextContains("budget"),
    DriveQuery.ModifiedAfter(DateTimeOffset.UtcNow.AddDays(-30)),
    DriveQuery.NotTrashed));
```

Both read every page up to `maxPages` and throw `ConnectorPageCapException<DriveFile>` with the files read past it, never a shortened list. For one page at a time, `ListChildrenPageAsync` and `SearchPageAsync` take a `limit` (1 to 1000) and a `cursor`, and return a `DriveFilePage` whose `NextCursor` reads the next page.

### Reading only what changed in Drive

`DeltaAsync` returns every file on the first call and only the changes on later ones, in a `DriveFileDelta` keyed by the `DeltaToken` it hands back — store it. `fromNow: true` skips the first full read and only marks now. Each changed file appears once, in its latest state, wherever it is, so filter on `Parents` to follow one folder; a file moved to the bin has `Trashed` set, and one deleted for good or no longer shared with the credential has `Deleted` set. A shared drive's changes are a feed of their own: pass the drive's id — `GetSharedDriveIdAsync` names a folder's — as `sharedDriveId` with every call of that feed. Past `maxPages` it throws `ConnectorPageCapException<DriveFile>` whose `ResumeFrom` is passed back as the token to continue.

<!-- ikon-example: connectors-drive-delta -->
```csharp
var delta = await drive.DeltaAsync(storedToken, fromNow: storedToken is null);

foreach (var file in delta.Items)
{
    if (file.Deleted || file.Trashed)
    {
        Log.Instance.Info($"gone: {file.Id}");
    }
    else if (file.Parents.Contains(folderId))
    {
        Log.Instance.Info($"changed: {file.Name}");
    }
}

storedToken = delta.DeltaToken;   // store it for the next call
```

### Being told when Drive changes

Polling `DeltaAsync` works, but Google can also post to your app when something changes. `GoogleWatches.CreateAsync` opens a `GoogleChannel` on a `WatchTarget` — a drive's changes feed (`WatchTarget.DriveChanges`, from a delta token) or one file (`WatchTarget.DriveFile`) — that posts to your app's public https endpoint, an `[HttpPost]` with `Auth = EndpointAuth.Public` (see the endpoints guide). Give it a secret token: `GoogleNotifications.ParseChannel` reads a `GoogleChannelNotification` from the request's headers and returns null for one that does not carry the token. A notification only says that something changed; read what with `DeltaAsync`. Google cannot extend a channel — a file's lives at most a day and a changes feed's a week — so `RenewAsync` opens a new channel before stopping the old, and `StopAsync` stops one.

<!-- ikon-example: connectors-drive-watch -->
```csharp
var start = (await drive.DeltaAsync(fromNow: true)).DeltaToken;
var channel = await watches.CreateAsync(WatchTarget.DriveChanges(start), notificationUrl, channelToken);

// In the [HttpPost] endpoint at notificationUrl:
if (GoogleNotifications.ParseChannel(headers, channelToken) is { ResourceState: "change" })
{
    var delta = await drive.DeltaAsync(storedToken);   // the notification only says that something changed
}
```

### Gmail

`ListMessagesAsync(query)` returns every message matching a Gmail search — the syntax of Gmail's search box, `is:unread` or `from:ada@example.com after:2026/01/01` — newest first, as `GmailMessageSummary` records with `From`, `Subject`, `Snippet`, `ReceivedAt` and `LabelIds`. It fetches the summaries 50 to a batch, and any message that cannot be read fails the whole call rather than leaving a hole. `ListMessagesPageAsync(query, limit, cursor)` reads one page into a `GmailMessagePage`. `GetMessageAsync` reads a whole message into a `GmailMessage`: `To`, `Cc`, `Date`, `Text` (the `text/plain` part) and `Html` (the `text/html` part as sent, not converted), its `Attachments` as `GmailAttachment` records with their name, type and size, and the `RfcMessageId` a reply names. It reads the bodies but not the attachments' bytes: `DownloadAttachmentAsync(messageId, attachmentId)` fetches one, and `GetRawMessageAsync` streams the whole MIME source. `GetProfileAsync` returns a `GmailProfile`: the address the credential reads and the mailbox's totals.

<!-- ikon-example: connectors-gmail -->
```csharp
var unread = await gmail.ListMessagesPageAsync("is:unread", limit: 10);

foreach (var summary in unread.Items)
{
    var message = await gmail.GetMessageAsync(summary.Id);
    Log.Instance.Info($"{message.From}: {message.Subject} ({message.Attachments.Count} attachments)");
}

var sentId = await gmail.SendAsync("someone@example.com", "Weekly summary", bodyText, cc: "team@example.com");
```

`SendAsync` sends plain text, or HTML with `isHtml: true`, to comma- or semicolon-separated addresses and returns the sent message's id; it needs the `gmail.send` scope. An empty `to` throws `ArgumentException` and a malformed address `ConnectorException`, both before anything is sent.

### Sending, replying and drafts

For more than plain text, `SendAsync` takes a `NewGmailMessage`: `To`, `Cc` and `Bcc`, a `ReplyTo`, a send-as `From` address (`ListSendAsAsync` lists them as `GmailSendAs` records), a `Text` and an `Html` body — both go, as alternatives — and `NewGmailAttachment` files, an inline image given a `ContentId` its HTML names as `cid:`. `ReplyAsync` answers a message in its thread, to its Reply-To or sender, with the threading headers mail programs rely on; `replyAll: true` copies everyone else it went to. `ForwardAsync` sends a message on, attached whole. An empty `Subject` on a reply or forward takes the original's.

<!-- ikon-example: connectors-gmail-send -->
```csharp
await gmail.SendAsync(new NewGmailMessage("Q3 report")
{
    To = ["grace@example.com"],
    Cc = ["finance@example.com"],
    Html = "<p>The Q3 report is attached.</p>",
    Attachments = [new NewGmailAttachment("q3.pdf", "application/pdf", reportPdf)],
});

// Into the original's thread, to its sender, with "Re:" before its subject:
await gmail.ReplyAsync(messageId, new NewGmailMessage("") { Text = "Thanks, received." });
```

A draft waits for a person to send it: `CreateDraftAsync` saves a `NewGmailMessage` — as a reply in a thread with `replyToMessageId` — and returns a `GmailDraft`; `UpdateDraftAsync`, `GetDraftAsync`, `ListDraftsAsync`, `SendDraftAsync` and `DeleteDraftAsync` do the rest. Drafts need the restricted `gmail.compose` scope.

### Organising mail

Labels are how Gmail files mail. `ListLabelsAsync` returns the mailbox's `GmailLabel`s, Gmail's own (`INBOX`, `UNREAD`, `STARRED`) and the person's; `GetLabelAsync` adds their message and thread counts, and `CreateLabelAsync`, `UpdateLabelAsync` and `DeleteLabelAsync` manage the person's. `AddLabelsAsync` and `RemoveLabelsAsync` change one message's labels and `ModifyLabelsAsync` many messages' in one request; `MarkReadAsync`, `MarkUnreadAsync` and `ArchiveAsync` are the common cases. `DeleteAsync` moves a message to the bin, `RestoreAsync` brings it back, and `PermanentDeleteAsync` deletes it for good with the full `https://mail.google.com/` scope.

<!-- ikon-example: connectors-gmail-organise -->
```csharp
var clients = (await gmail.ListLabelsAsync()).FirstOrDefault(label => label.Name == "Clients")
    ?? await gmail.CreateLabelAsync("Clients");

var fromClients = await gmail.ListMessagesAsync("from:@client.example is:unread");
await gmail.ModifyLabelsAsync([.. fromClients.Select(message => message.Id)], addLabelIds: [clients.Id], removeLabelIds: ["UNREAD"]);
```

Conversations work the same way: `ListThreadsAsync` and `ListThreadsPageAsync` return `GmailThreadSummary` records in a `GmailThreadPage`, `GetThreadAsync` a `GmailThread` with every message, and the thread calls label, bin and restore a whole conversation.

The account's settings — with the restricted `gmail.settings.basic` scope — are reached too: `ListFiltersAsync`, `CreateFilterAsync` and `DeleteFilterAsync` manage `GmailFilter`s, each a `GmailFilterCriteria` and a `GmailFilterAction`; `GetVacationAsync` and `SetVacationAsync` the automatic `GmailVacation` reply; `SetSignatureAsync` a send-as address's signature; and `ListForwardingAddressesAsync` and `GetAutoForwardingAsync` read the `GmailForwardingAddress` and `GmailAutoForwarding` setup.

### Reading only what changed in Gmail

`MessagesDeltaAsync` follows a mailbox the way `Drive.DeltaAsync` follows a drive: every message on the first call (or only a token with `fromNow: true`), then each message that changed since the stored `DeltaToken`, once, as a `GmailMessageChange` — `Added` when it arrived, `Deleted` when it is gone for good, and the `AddedLabelIds` and `RemovedLabelIds` (a message moved to the bin gains `TRASH`). Read a changed message with `GetMessageAsync`. Gmail keeps history for about a week: an older token throws with `IsResyncRequired` set, so catch up with a search and start again.

<!-- ikon-example: connectors-gmail-delta -->
```csharp
GmailDelta delta;

try
{
    delta = await gmail.MessagesDeltaAsync(storedToken, fromNow: storedToken is null);
}
catch (ConnectorException ex) when (ex.IsResyncRequired)
{
    delta = await gmail.MessagesDeltaAsync(fromNow: true);   // older than Gmail keeps: catch up with a search, then follow from now
}

foreach (var change in delta.Items)
{
    if (change.Deleted)
    {
        await ForgetMailAsync(change.Id);
    }
    else if (change.Added)
    {
        await IngestMailAsync(change.Id);
    }
}

storedToken = delta.DeltaToken;   // store it for the next call
```

### Being told when Gmail changes

Gmail does not call an app directly: it publishes to a Google Cloud Pub/Sub topic, and a push subscription on the topic posts to your app. Create the topic in the Google Cloud project of your OAuth client, let `gmail-api-push@system.gserviceaccount.com` publish to it, and give the push subscription authentication — a service account and an audience — and your endpoint, an `[HttpPost]` with `Auth = EndpointAuth.Public`. `WatchAsync(topicName)` starts the mailbox publishing and returns a `GmailWatch`; it lasts a week, so call it again daily, and `StopWatchAsync` ends it. In the endpoint, `GooglePubSubVerifier.VerifyAsync` checks the token Pub/Sub signed into the Authorization header against that audience and service account (it takes both, since any Google service account can obtain a token for any audience), `GoogleNotifications.ParsePubSub` reads the `GooglePubSubMessage`, and `GoogleNotifications.ParseGmail` the `GmailNotification` in it: the address and a history id, no more — read what changed with `MessagesDeltaAsync`. Gmail publishes at most one notification a second per mailbox.

<!-- ikon-example: connectors-gmail-watch -->
```csharp
await gmail.WatchAsync("projects/my-project/topics/gmail");   // again daily: a watch lasts a week

// In the [HttpPost] endpoint the Pub/Sub push subscription posts to:
if (await verifier.VerifyAsync(authorizationHeader)
    && GoogleNotifications.ParsePubSub(body) is { } message
    && GoogleNotifications.ParseGmail(message) is { } notification)
{
    var delta = await gmail.MessagesDeltaAsync(storedToken);   // read what changed
}
```

### Calendar

`GoogleCalendar` reaches the person's calendars. `ListCalendarsAsync` returns their list as `CalendarEntry` records, each with the `AccessRole` the credential has; `primary` names their own calendar wherever an id is taken. `ListEventsAsync(calendarId, from, to)` returns the `CalendarEvent`s in a range, each occurrence of a recurring event on its own in start order, and the overload taking a `CalendarEventQuery` adds free text, event types and recurring series as written. An event's `Start` and `End` are `CalendarTime`s — a moment (`CalendarTime.At`) or a whole day (`CalendarTime.AllDay`) — and it carries its `CalendarAttendee`s, `Recurrence`, `MeetLink`, `CalendarReminder`s and `CalendarAttachment`s.

`CreateEventAsync` creates a `NewCalendarEvent` — guests, optional guests, a recurrence rule, reminders, Drive attachments, `WithMeet` for a new Meet link. Invitations are emailed only when `notify` asks, a `CalendarNotify` of `All` or `ExternalOnly`; the event appears in the guests' calendars either way. `UpdateEventAsync` changes what it is given, refusing with `412` an update over a change made since the `ETag` it is handed; `DeleteEventAsync`, `MoveEventAsync`, `QuickAddAsync` (from a sentence, `Lunch with Ada tomorrow 12:30`) and `RespondAsync` (a `CalendarResponse` to an invitation) complete the set, with `CreateOutOfOfficeAsync`, `CreateFocusTimeAsync` and `CreateWorkingLocationAsync` for Workspace's special events.

`GetFreeBusyAsync` returns each calendar's `CalendarFreeBusy` — its `CalendarBusy` spans, without what fills them, so it works on calendars shared for free/busy alone — and `FindFreeTimeAsync` turns several people's into the `CalendarFreeTime` stretches every one of them is free, within working hours:

<!-- ikon-example: connectors-calendar-meeting -->
```csharp
var helsinki = TimeZoneInfo.FindSystemTimeZoneById("Europe/Helsinki");
var free = await calendar.FindFreeTimeAsync(
    ["primary", "grace@example.com"], TimeSpan.FromMinutes(45),
    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7),
    workdayStart: new TimeOnly(9, 0), workdayEnd: new TimeOnly(17, 0), timeZone: helsinki);

if (free.FirstOrDefault() is { } slot)
{
    var meeting = await calendar.CreateEventAsync("primary", new NewCalendarEvent("Plan review", CalendarTime.At(slot.Start), CalendarTime.At(slot.Start.AddMinutes(45)))
    {
        Attendees = ["grace@example.com"],
        WithMeet = true,
    }, notify: CalendarNotify.All);

    Log.Instance.Info($"Booked {meeting.Start?.DateTime:u}, join at {meeting.MeetLink}");
}
```

Calendars themselves are created, changed, deleted and cleared with `CreateCalendarAsync`, `UpdateCalendarAsync`, `DeleteCalendarAsync` and `ClearCalendarAsync`; `AddCalendarAsync`, `RemoveCalendarAsync` and `UpdateCalendarEntryAsync` change the person's list. `ListAclAsync`, `AddAclAsync`, `UpdateAclAsync` and `RemoveAclAsync` share a calendar as `CalendarAcl` rules, `GetColorsAsync` returns the `CalendarColors` palette of `CalendarColor` pairs and `GetSettingsAsync` the person's Calendar settings.

`EventsDeltaAsync` follows a calendar as `Drive.DeltaAsync` follows a drive, returning a `CalendarEventDelta`: every event on the first call, then each that changed since the stored token, a cancelled one with `Deleted` set. An expired token throws with `IsResyncRequired`. `WatchTarget.CalendarEvents` and `WatchTarget.CalendarList` give `GoogleWatches` channels that say when to read it.

<!-- ikon-example: connectors-calendar-delta -->
```csharp
CalendarEventDelta delta;

try
{
    delta = await calendar.EventsDeltaAsync("primary", storedToken);
}
catch (ConnectorException ex) when (ex.IsResyncRequired)
{
    delta = await calendar.EventsDeltaAsync("primary");   // the token expired: read the calendar again
}

foreach (var changed in delta.Items)
{
    Log.Instance.Info(changed.Deleted ? $"cancelled: {changed.Id}" : $"{changed.Summary} at {changed.Start?.DateTime:u}");
}

storedToken = delta.DeltaToken;   // store it for the next call
```

### Docs, Sheets and Slides

`Drive` reaches these files as files — their sharing, comments, revisions and exports. `GoogleDocs`, `GoogleSheets` and `GoogleSlides` reach what is in them. Each has a raw `BatchUpdateAsync` that sends the API's own edit requests, applied in order, all or none, for what has no method here.

`GoogleDocs.GetAsync` reads a `GoogleDocument`: its `DocsTab`s and in each the `DocsBlock` paragraphs and tables, with the positions the Docs API's edits take. `GetMarkdownAsync` reads a tab as Markdown — headings, lists, tables, bold, italic, code and links. `AppendMarkdownAsync` and `InsertMarkdownAsync` write Markdown back with its headings, lists and text styles, `ReplaceAllTextAsync` fills a template, and `InsertTableAsync` and `InsertImageAsync` add a filled table and an image from a public URL. A `RevisionId` handed to `BatchUpdateAsync` as `requiredRevisionId` refuses an edit over a change made since it was read.

<!-- ikon-example: connectors-docs-markdown -->
```csharp
var markdown = await docs.GetMarkdownAsync(documentId);

await docs.AppendMarkdownAsync(documentId, """
    ## Decisions

    - Ship the **new API** in August
    - Review [the spec](https://example.com/spec) first
    """);
```

`GoogleSheets` reads and writes cells by range, in A1 notation (`Budget!A1:C50`), a sheet's title or a named range. `GetValuesAsync` and `BatchGetValuesAsync` return `SheetValues` — every cell as text, rendered as the sheet shows it unless a `SheetValueRender` asks for the raw value or the formula. `UpdateValuesAsync`, `BatchUpdateValuesAsync` and `AppendRowsAsync` write rows and report a `SheetUpdate`; by default values are read as a person typing them would be, so `=SUM(B2:B9)` is a formula. `ClearValuesAsync` empties a range. `GetAsync` returns a `GoogleSpreadsheet` with its `SheetInfo` sheets and `SheetNamedRange`s, and `AddSheetAsync`, `RenameSheetAsync`, `DuplicateSheetAsync` and `DeleteSheetAsync` manage the sheets. `GetSheetsAsCsvAsync` reads every sheet as CSV, where a Drive export gives the first alone.

<!-- ikon-example: connectors-sheets-values -->
```csharp
var budget = await sheets.GetValuesAsync(spreadsheetId, "Budget!A1:C50");

foreach (var row in budget.Rows.Skip(1))
{
    Log.Instance.Info($"{row[0]}: {row.ElementAtOrDefault(1)}");
}

await sheets.AppendRowsAsync(spreadsheetId, "Budget", [["Laptop", 1200, "=B2*1.24"]]);
```

`GoogleSlides.GetAsync` reads a `GooglePresentation`'s `SlideInfo` slides — the text on each and its speaker notes — and `GetTextAsync` the whole deck as text. A deck is built from a template by copying it with `Drive.CopyAsync` and filling the copy: `ReplaceAllTextAsync` replaces text, and `ReplaceShapesWithImageAsync` puts an image where a placeholder shape is. `CreateSlideAsync`, `DuplicateSlideAsync` and `DeleteObjectAsync` add and remove slides, and `GetThumbnailAsync` renders one as a `SlideThumbnail`.

<!-- ikon-example: connectors-slides-template -->
```csharp
var copy = await drive.CopyAsync(templateId, newName: $"Proposal for {customer}");

await slides.ReplaceAllTextAsync(copy.Id, new Dictionary<string, string>
{
    ["{{customer}}"] = customer,
    ["{{date}}"] = DateTime.UtcNow.ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
});
```

### Contacts and Tasks

`GoogleContacts` reaches the person's contacts through Google's People API. Each is a `GoogleContact` named by its `ResourceName` (`people/c123`). `GetContactAsync`, `GetContactsAsync` and `ListContactsAsync` read them; `CreateContactAsync` takes a `NewGoogleContact`; `UpdateContactAsync` takes the contact's `ETag` as last read and refuses a change made since; and `DeleteContactAsync` deletes one. `SearchContactsAsync` matches the start of names, addresses, phone numbers and organisations, up to 30 at a time. Google answers it from a cache, so a contact just created may take a few minutes to be found. `ListGroupsAsync`, `CreateGroupAsync`, `DeleteGroupAsync` and `UpdateGroupMembersAsync` manage `GoogleContactGroup`s.

"Other contacts" are the people the person emailed without saving. `ListOtherContactsAsync` and `SearchOtherContactsAsync` read them, and `CopyOtherContactAsync` saves one. In a Workspace account, `SearchDirectoryAsync` and `ListDirectoryAsync` read the organisation's directory. `GetMeAsync` reads the person's own profile.

`ContactsDeltaAsync` is a change feed returning a `GoogleContactDelta`: a deleted contact has `Deleted` set, and Google honours a token for seven days.

<!-- ikon-example: connectors-contacts-search -->
```csharp
var matches = await contacts.SearchContactsAsync(typed, limit: 5);

if (matches.Count == 0)
{
    matches = await contacts.SearchOtherContactsAsync(typed, limit: 5);   // people emailed, never saved
}

foreach (var person in matches)
{
    Log.Instance.Info($"{person.DisplayName} <{person.Emails.FirstOrDefault()}>");
}
```

`GoogleTasks` reaches Google Tasks. A list is a `GoogleTaskList`; the id `@default` names the person's default list wherever a list id is taken. Its tasks are `GoogleTask`s: a title, notes, a due day with no time of day, and a `ParentId` that makes one a subtask. `ListTasksAsync` filters with a `GoogleTaskQuery`, and `CreateTaskAsync` takes a `NewGoogleTask`. `UpdateTaskAsync`, `CompleteTaskAsync`, `ReopenTaskAsync`, `MoveTaskAsync` and `DeleteTaskAsync` change one. `ClearCompletedAsync` hides a list's completed tasks.

Google Tasks has no change feed of its own. `TasksDeltaAsync` instead asks for what was updated since its token's moment, deleted tasks included, and returns a `GoogleTaskDelta`, so a task can come back once more than it changed.

<!-- ikon-example: connectors-tasks -->
```csharp
var parent = await tasks.CreateTaskAsync("@default", new NewGoogleTask("Prepare the launch") { Due = new DateOnly(2026, 11, 2) });
await tasks.CreateTaskAsync("@default", new NewGoogleTask("Book the venue") { ParentId = parent.Id });

foreach (var task in await tasks.ListTasksAsync("@default", new GoogleTaskQuery { ShowCompleted = false }))
{
    Log.Instance.Info($"{task.Title} due {task.Due}");
}

await tasks.CompleteTaskAsync("@default", parent.Id);
```

### Meet, Chat and Forms

These three name things by Google's resource names — `spaces/jQCFfuBOdN5z`, `conferenceRecords/abc/transcripts/t1`, `spaces/AAAAxyz/messages/m1` — which each record carries as its `Name` and each method takes back as it was returned. A name of the wrong shape throws `ArgumentException` before any request.

`GoogleMeet.CreateSpaceAsync` makes a `MeetSpace` with a link to join it. `MeetSpaceSettings` sets who joins without knocking (`MeetAccess`), moderation, and whether every conference is recorded, transcribed or summarised from its start. `GetSpaceAsync` also takes a meeting code, and `UpdateSpaceAsync` changes only the settings given. `AddMemberAsync` invites people ahead of time, as `MeetMember`s, optionally as co-hosts. `EndConferenceAsync` ends a meeting in progress.

What a meeting leaves behind is read from its `MeetConference`: `ListParticipantsAsync` returns `MeetParticipant`s, each with a `MeetParticipantKind` (signed in, anonymous or by phone), and `ListParticipantSessionsAsync` takes one's `Name` and returns its `MeetParticipantSession`s. `ListRecordingsAsync`, `ListTranscriptsAsync` and `ListSmartNotesAsync` return `MeetArtifact`s naming the Drive file or Google Doc each was written to. `ListTranscriptEntriesAsync` reads a transcript as `MeetTranscriptEntry` lines with speaker and time. Google keeps conferences and transcript entries for 30 days.

<!-- ikon-example: connectors-meet -->
```csharp
var space = await meet.CreateSpaceAsync(new MeetSpaceSettings { Access = MeetAccess.Trusted, AutoTranscription = true });
Log.Instance.Info($"Join at {space.MeetingUri}");

// Later, once the meeting is over:
var conference = (await meet.ListConferencesAsync(space.Name)).First();   // newest first

foreach (var transcript in await meet.ListTranscriptsAsync(conference.Name))
{
    foreach (var entry in await meet.ListTranscriptEntriesAsync(transcript.Name))
    {
        Log.Instance.Info($"{entry.Participant}: {entry.Text}");
    }
}
```

`GoogleChat` acts as the signed-in person, in Workspace accounts.

- **Spaces.** `ListSpacesAsync` returns their spaces, group chats and direct messages as `GoogleChatSpace`s of a `GoogleChatSpaceType`. `FindDirectMessageAsync` finds the conversation with one user, and `CreateSpaceAsync` sets up a space with its members.
- **Messages.** `ListMessagesAsync` reads a space's `GoogleChatMessage`s oldest first, and `ListMessagesPageAsync` the recent ones newest first as a `GoogleChatMessagePage`. `PostMessageAsync` posts, or replies in a message's `Thread`. `UpdateMessageAsync` and `DeleteMessageAsync` change the person's own messages.
- **Reactions and members.** `AddReactionAsync`, `ListReactionsAsync` and `RemoveReactionAsync` handle `GoogleChatReaction`s; a message also carries `GoogleChatReactionCount` totals. `ListMembersAsync`, `AddMemberAsync` and `RemoveMemberAsync` handle `GoogleChatMember`s.
- **Attachments.** A `GoogleChatAttachment` with a `MediaToken` is read with `DownloadAttachmentAsync`; one with a `DriveFileId` is read through `Drive`.

Chat's text uses its own markup — `*bold*`, `_italic_`, `~strike~`, `` `code` `` — not Markdown.

<!-- ikon-example: connectors-chat -->
```csharp
var recent = await chat.ListMessagesPageAsync(spaceName, limit: 20);

if (recent.Items.FirstOrDefault(m => m.Text.Contains("deploy?", StringComparison.OrdinalIgnoreCase)) is { } question)
{
    await chat.PostMessageAsync(spaceName, "Deploy finished *without errors*", thread: question.Thread);
    await chat.AddReactionAsync(question.Name, "✅");
}
```

`GoogleForms.CreateAsync` makes an unpublished `GoogleForm`. `AddQuestionAsync` adds a `NewGoogleFormQuestion` of a `GoogleFormQuestionKind` and returns its `GoogleFormItem`, and `SetPublishedAsync` publishes the form when it is ready. `UpdateInfoAsync` and `DeleteItemAsync` edit it, and `BatchUpdateAsync` sends the Forms API's own requests for anything else.

`ListResponsesAsync` and `GetResponseAsync` read `GoogleFormResponse`s, each with its `GoogleFormAnswer`s keyed by the item's `QuestionId`. `CreateWatchAsync` has Google publish to a Pub/Sub topic when a response arrives or the form changes. A `GoogleFormWatch` lasts seven days and is extended with `RenewWatchAsync`. `GoogleNotifications.ParseForm` reads what it publishes as a `GoogleFormNotification` with its `GoogleFormWatchEvent`.

<!-- ikon-example: connectors-forms -->
```csharp
var form = await forms.CreateAsync("Workshop feedback");
var rating = await forms.AddQuestionAsync(form.FormId, new NewGoogleFormQuestion("How was it?", GoogleFormQuestionKind.Scale) { ScaleHigh = 5, Required = true });
await forms.AddQuestionAsync(form.FormId, new NewGoogleFormQuestion("What should change?", GoogleFormQuestionKind.Paragraph));
await forms.SetPublishedAsync(form.FormId, published: true);

// Later:
foreach (var response in await forms.ListResponsesAsync(form.FormId))
{
    if (response.Answers.TryGetValue(rating.QuestionId!, out var answer))
    {
        Log.Instance.Info($"Rated {answer.Values.Single()}");
    }
}
```

### Workspace Events

`WorkspaceEvents` subscribes to what happens in Drive, Chat and Meet, and Google publishes each event to a Cloud Pub/Sub topic as it happens.

- **Targets.** A `WorkspaceEventTarget` names what to watch: a Drive file, or a folder or shared drive with everything in it; a Chat space; a Meet space, or every conference a user organises.
- **Event types.** These are Google's own names, such as `google.workspace.drive.file.v3.contentChanged`, `google.workspace.chat.message.v1.created` or `google.workspace.meet.transcript.v2.fileGenerated`.
- **Lifetime.** A `WorkspaceSubscription` lives for up to `MaxLifetime` — a week, or four hours when its events carry the changed resource — and `RenewAsync` extends it.
- **Suspension.** A subscription Google suspended, after a revoked scope or a topic it cannot publish to, has `WorkspaceSubscriptionState.Suspended` and is restarted with `ReactivateAsync`.
- **Setup.** The topic grants the Pub/Sub Publisher role to Google's publisher for each app: `drive-api-event-push@system.gserviceaccount.com` for Drive, `meet-api-event-push@system.gserviceaccount.com` for Meet, and `chat-api-push@system.gserviceaccount.com` for Chat.

`GoogleNotifications.ParseWorkspaceEvent` reads what the push subscription delivers as a `WorkspaceEvent`:

- a `WorkspaceResourceEvent` for a change, with the resource in its `Subject` and the payload in `Data`;
- a `WorkspaceExpirationReminder` 12 hours and an hour before the subscription expires;
- a `WorkspaceSubscriptionSuspended` naming why it stopped;
- a `WorkspaceSubscriptionExpired` once it is gone.

For Drive, prefer Workspace Events when the app has a Pub/Sub topic: one subscription covers a whole folder tree and says what changed. `GoogleWatches` channels need only an HTTPS endpoint, but each one only says that something changed.

<!-- ikon-example: connectors-workspace-events -->
```csharp
await events.CreateAsync(
    WorkspaceEventTarget.DriveFile(folderId, includeDescendants: true),
    ["google.workspace.drive.file.v3.created", "google.workspace.drive.file.v3.contentChanged"],
    "projects/my-project/topics/drive-events");

// In the Pub/Sub push endpoint:
if (await verifier.VerifyAsync(authorizationHeader) && GoogleNotifications.ParsePubSub(body) is { } message)
{
    switch (GoogleNotifications.ParseWorkspaceEvent(message))
    {
        case WorkspaceResourceEvent change:
            Log.Instance.Info($"{change.Type} on {change.Subject}");
            break;
        case WorkspaceExpirationReminder reminder:
            await events.RenewAsync(reminder.Subscription);
            break;
        case WorkspaceSubscriptionSuspended suspended:
            Log.Instance.Warning($"Subscription {suspended.Subscription} suspended: {suspended.Reason}");
            break;
        case WorkspaceSubscriptionExpired expired:
            Log.Instance.Warning($"Subscription {expired.Subscription} expired; create it again");
            break;
    }
}
```

### Scopes

Google classes every scope, and the class decides what publishing an app that asks for it takes: a **sensitive** scope needs Google's app verification, and a **restricted** one also needs a yearly third-party security assessment (CASA) when the data passes through your servers.

| Scope | Class | What it reaches |
|---|---|---|
| `drive.file` | non-sensitive | the files the app created or the person opened with it |
| `gmail.labels` | non-sensitive | creating and listing labels, not labelling mail |
| `gmail.send` | sensitive | sending only |
| `calendar`, `calendar.readonly`, `calendar.events` | sensitive | calendars and events |
| `documents`, `spreadsheets`, `presentations` | sensitive | every Doc, Sheet or Slides deck the person can open |
| `contacts`, `contacts.readonly`, `contacts.other.readonly`, `directory.readonly` | sensitive | contacts, other contacts, the Workspace directory |
| `tasks`, `tasks.readonly` | sensitive | task lists and tasks |
| `meetings.space.settings` | non-sensitive | changing a Meet space's settings |
| `meetings.space.created`, `meetings.space.readonly` | sensitive | the spaces the app made; every space and conference record |
| `chat.spaces`, `chat.memberships`, `chat.messages.create`, `chat.messages.reactions` | sensitive | Chat spaces, members, posting and reactions |
| `chat.messages`, `chat.messages.readonly` | restricted | reading Chat messages |
| `forms.body`, `forms.body.readonly`, `forms.responses.readonly` | sensitive | forms and their responses |
| `drive.meet.readonly` | restricted | Meet's recordings and transcripts in Drive |
| `drive.readonly`, `drive` | restricted | every file |
| `gmail.readonly`, `gmail.modify`, `gmail.compose`, `gmail.settings.basic` | restricted | the whole mailbox, or its settings |
| `https://mail.google.com/` | restricted | everything, deleting for good included |

Ask for the narrowest scope that does the job, and ask for more only when the person reaches the feature that needs it: `AuthorizeUrl` always asks with `include_granted_scopes`, so a second consent adds to the first.

### Rate limits

Google meters each API per minute, per project and per user, in units that differ by call: a Gmail message read costs more than a list, a Drive download more than a metadata read. Cloud projects created since May 2026 get tiered daily quotas on Gmail, Drive and Calendar. A `429` that outlasts the three retries is a quota signal: slow the caller rather than retry at once. Gmail's own advice keeps a batch to at most 50 calls, which the connector does. A refresh token issued to an OAuth client still in Google's "Testing" status expires after seven days.

### What it does not reach

This package covers Drive — files, sharing, comments, revisions, labels, activity and change notifications — and Gmail — reading, sending, threads, drafts, labels, filters, the vacation reply, signatures and Pub/Sub notifications — and Calendar — calendars, events, invitations, free time, sharing and change feeds — and the content of Docs, Sheets and Slides — and Contacts, Tasks, Meet's spaces and what its conferences leave behind, Chat as the person, Forms, and Workspace Events for Drive, Chat and Meet. It does not reach Calendar's appointment schedules, which have no API; Docs' suggestions; Sheets' formatting, charts and filters, or Slides' layout beyond whole slides, except through `BatchUpdateAsync`; Meet's live media; a Chat app (bot) of your own, which signs in as a service account; Chat's cards and message search; Workspace Events for Gmail and Calendar, which Google does not offer (their own push stands in); Drive approvals; Gmail's IMAP, POP, language, S/MIME and client-side encryption settings; or anything that needs a service account with domain-wide delegation, such as Gmail delegates, adding forwarding addresses, Keep, the Admin SDK or Vault. Everything it does goes through Google's REST APIs with the person's own consent.
