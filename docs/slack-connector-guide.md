# Slack Connector Guide
<!-- checked-against: 32b2da6946407040 -->
This guide covers `Ikon.Connectors.Slack` — installing into a Slack workspace, posting Block Kit messages and files, reading conversations and people, receiving events, interactions and slash commands over HTTP or Socket Mode, modals and App Home, and streaming agent replies — for app developers wiring Slack into an Ikon app.

## Slack

The types are in namespace `Ikon.Connectors`, not the package's name — `using Ikon.Connectors.Slack;` names the `Slack` class and fails with CS0138. Construct `Slack` with a **bot token** (`xoxb-...`), or a user token (`xoxp-...`) to act as the person who installed the app; an empty or whitespace token throws `ArgumentException` at construction. A token that rotates goes in through a `SlackTokenProvider` instead (see "Rotating tokens"). An optional `HttpClient` can be injected; otherwise a shared one is used.

<!-- ikon-example: connectors-slack-client -->
```csharp
var slack = new Slack(botToken);
```

Every failure is a `ConnectorException` (from `Ikon.Connectors`) with `Provider` `"slack"`. Slack answers every API error with HTTP 200 and `ok:false`, and the connector stamps the ones that mean something about the credential: an invalid, revoked or expired token is `StatusCode` `401`, and `missing_scope` and `not_allowed_token_type` are `403` — so `IsReconnectRequired` means ask the person to reconnect, or reinstall with the scope the message names, rather than retry. `ratelimited` is `429` and `internal_error` or `fatal_error` `500`. Any other Slack API error has a null `StatusCode` and Slack's code (`channel_not_found`, `not_in_channel`, `is_archived`) in `ErrorCode`, and fails the same way again; for `invalid_blocks` and `invalid_arguments` the message carries Slack's explanation. A `429` is retried three times on Slack's `Retry-After`, bounded at two minutes, before it surfaces; `IsTransient` marks one that still did.

### Posting

<!-- ikon-example: connectors-slack-post -->
```csharp
var posted = await slack.PostAsync("C0123456789", "Deploy finished", threadTs: rootTs);
```

The returned `SlackPostResult` is what Slack echoed back: the posted message's `Ts` and the `Channel` id a name like `#general` resolved to. Pass that `Ts` as `threadTs` to reply in the thread; the post's author, subtype and files are not returned — read the message back from history for those.

A richer message is a `SlackMessageContent` — its `Text`, which notifications and screen readers show, plus `Blocks`, legacy `Attachments` as raw JSON, and `SlackMessageMetadata` (an `EventType` and a JSON `EventPayload` the app can read back later) — posted with `SlackPostOptions` for the thread, `ReplyBroadcast`, unfurling, and a custom `Username` and icon (which need `chat:write.customize`). A string converts to a text-only `SlackMessageContent`. Slack paces posting at about one message a second per channel; a burst past it comes back as a `429` and is retried.

<!-- ikon-example: connectors-slack-blocks -->
```csharp
var content = new SlackMessageContent("Deploy 412 finished")
{
    Blocks =
    [
        SlackBlock.Header("Deploy 412 finished"),
        new SlackSectionBlock(SlackText.Markdown("*api* is live on `prod`"))
        {
            Accessory = new SlackButton("Roll back") { ActionId = "rollback", Value = "412", Style = SlackButtonStyle.Danger },
        },
        SlackBlock.Context(SlackText.Markdown("Started by <@U0123456789>")),
    ],
};

var posted = await slack.PostAsync(channelId, content, new SlackPostOptions { UnfurlLinks = false });
```

The rest of a message's life:

- `UpdateAsync(channel, ts, content)` replaces what a message the app posted says — content without `Blocks` clears the old ones — and `DeleteAsync(channel, ts)` removes it; another's message is `cant_update_message` or `cant_delete_message`.
- `PostEphemeralAsync(channel, user, content)` shows a message to one person in the channel until they reload Slack. It cannot be updated or deleted through the API afterwards — only through the `response_url` of an interaction on it.
- `ScheduleAsync(channel, postAt, content)` has Slack post a message up to 120 days ahead and returns a `SlackScheduledMessage`; `ListScheduledAsync` lists those not yet posted and `DeleteScheduledAsync(channel, id)` cancels one that is not about to be posted.
- `GetPermalinkAsync(channel, ts)` is a link that opens the message in Slack.
- `AddReactionAsync`/`RemoveReactionAsync(channel, ts, name)` react with an emoji code such as `thumbsup`, and `GetReactionsAsync` reads every `SlackReaction` with its `Count` and `Users`. `PinAsync`/`UnpinAsync` pin, and `ListPinsAsync(channel)` returns the pinned messages.
- `UnfurlAsync(channel, ts, unfurls)` answers a `link_shared` event with a `SlackUnfurl` of blocks for each URL of the app's own domains; `UnfurlByIdAsync(unfurlId, source, unfurls)` does the same for a link still in the composer.

`SlackResponseUrl.SendAsync(url, content, responseType, replaceOriginal, deleteOriginal)` answers through the `response_url` of a slash command, button or shortcut — `SlackResponseType.InChannel` for everyone, `Ephemeral` for the person who acted — and posts to an incoming webhook's URL the same way. Neither needs a token, the URL itself is the credential, and a `response_url` takes five messages within 30 minutes. It posts only to `https://hooks.slack.com`; any other URL throws `ArgumentException` without a request, so one taken from an untrusted payload cannot aim it elsewhere.

<!-- ikon-example: connectors-slack-response-url -->
```csharp
await SlackResponseUrl.SendAsync(responseUrl, "Rolled back to 411", SlackResponseType.InChannel, replaceOriginal: true);
```

### Block Kit

Every Block Kit block, element and text object is a record serialized to Slack's JSON by its type, with Slack's field names in PascalCase: `SlackSectionBlock(text) { Accessory = ... }` is `{"type":"section","text":...,"accessory":...}`. `SlackText.Plain` and `SlackText.Markdown` build the two text objects — `SlackPlainText` for labels, headers and placeholders, to which a string converts, and `SlackMarkdownText` for Slack's own mrkdwn (`*bold*`, `<url|label>`, `<@U123>`). The `markdown` block (`SlackBlock.Markdown`) takes CommonMark instead, as an AI reply is written.

| Kind | Types |
|---|---|
| Layout blocks | `SlackSectionBlock`, `SlackHeaderBlock`, `SlackDividerBlock`, `SlackContextBlock` (`ISlackContextElement`: text and images), `SlackActionsBlock`, `SlackImageBlock`, `SlackInputBlock`, `SlackMarkdownBlock`, `SlackVideoBlock` |
| Rich text | `SlackRichTextBlock` of `SlackRichTextElement`s — `SlackRichTextSection`, `SlackRichTextList` (`SlackListStyle`), `SlackRichTextPreformatted`, `SlackRichTextQuote` — holding `SlackRichTextLeaf`s: `SlackRichTextRun` with a `SlackTextStyle`, `SlackRichTextLink`, `SlackRichTextUser`, `SlackRichTextUsergroup`, `SlackRichTextChannel`, `SlackRichTextEmoji`, `SlackRichTextBroadcast` (`SlackBroadcastRange`), `SlackRichTextDate`, `SlackRichTextColor` |
| Tables and charts | `SlackTableBlock` with `SlackColumnSetting` (`SlackColumnAlign`); `SlackDataTableBlock`, sortable and paged; cells are `ISlackTableCell`: `SlackTextCell`, `SlackNumberCell`, a `SlackRichTextBlock`, or a `SlackActionCell` button in a data table; `SlackDataVisualizationBlock` of a `SlackChart` — `SlackPieChart`, or a `SlackSeriesChart` (`SlackBarChart`, `SlackAreaChart`, `SlackLineChart`) of `SlackChartSeries` and `SlackChartPoint` on a `SlackChartAxis` |
| Cards and groups | `SlackCardBlock` (with a `SlackIcon` or an image), `SlackCarouselBlock` of cards, `SlackContainerBlock` (`SlackContainerWidth`), `SlackAlertBlock` (`SlackAlertLevel`, modals only) |
| Agent replies | `SlackTaskCardBlock` and `SlackPlanBlock` of `SlackPlanTask`s, each with a `SlackTaskStatus` and `SlackUrlSource`s; `SlackContextActionsBlock` holding `SlackFeedbackButtons` (two `SlackFeedbackButton`s) and `SlackIconButton` |
| Buttons and menus | `SlackButton` (`SlackButtonStyle`, `SlackConfirm`), `SlackOverflow`, `SlackCheckboxes`, `SlackRadioButtons`, `SlackWorkflowButton` (`SlackWorkflow`, `SlackWorkflowTrigger`, `SlackWorkflowInput`), `SlackImageElement` |
| Selects | `SlackStaticSelect`, `SlackMultiStaticSelect` (`SlackOption`, `SlackOptionGroup`), `SlackExternalSelect`, `SlackMultiExternalSelect`, `SlackUsersSelect`, `SlackMultiUsersSelect`, `SlackConversationsSelect`, `SlackMultiConversationsSelect` (`SlackConversationFilter`), `SlackChannelsSelect`, `SlackMultiChannelsSelect` |
| Inputs | `SlackPlainTextInput`, `SlackEmailInput`, `SlackUrlInput`, `SlackNumberInput`, `SlackRichTextInput`, `SlackFileInput`, `SlackDatePicker`, `SlackTimePicker`, `SlackDateTimePicker`; `SlackDispatchActionConfig` makes one report as it changes |

A report reads best as tables and charts. A `SlackTableBlock` is a plain grid; a `SlackDataTableBlock` sorts and pages, and a `SlackActionCell` puts a button in a row:

<!-- ikon-example: connectors-slack-report -->
```csharp
var content = new SlackMessageContent("Weekly report")
{
    Blocks =
    [
        new SlackContainerBlock(
        [
            new SlackTableBlock(
            [
                [new SlackTextCell("Team"), new SlackTextCell("Closed")],
                [new SlackTextCell("Platform"), new SlackTextCell("42")],
            ])
            {
                ColumnSettings = [null, new SlackColumnSetting { Align = SlackColumnAlign.Right }],
            },
        ])
        {
            Title = "Tickets this week",
            Width = SlackContainerWidth.Wide,
            IsCollapsible = true,
        },
        new SlackDataTableBlock("Open tickets",
        [
            [new SlackTextCell("Ticket"), new SlackTextCell("Age (days)"), new SlackTextCell("")],
            [new SlackTextCell("DCW-1024"), new SlackNumberCell(3, "3"), new SlackActionCell(new SlackButton("Close") { ActionId = "close", Value = "DCW-1024" })],
        ])
        { PageSize = 10 },
        // SlackLineChart and SlackAreaChart take the same series and axis.
        new SlackDataVisualizationBlock("Tickets by day", new SlackBarChart(
            [new SlackChartSeries("Opened", [new("Mon", 4), new("Tue", 7)]), new SlackChartSeries("Closed", [new("Mon", 3), new("Tue", 9)])],
            new SlackChartAxis(["Mon", "Tue"]) { YLabel = "Tickets" })),
        new SlackDataVisualizationBlock("By severity", new SlackPieChart([new("High", 3), new("Low", 11)])),
        new SlackCarouselBlock(
        [
            new SlackCardBlock
            {
                SlackIcon = new SlackIcon("bug"),
                HeroImage = new SlackImageElement("https://ikon.live/screens/1024.png", "Screenshot of the failure"),
                Title = SlackText.Plain("DCW-1024"),
                Body = SlackText.Markdown("Login fails on *Safari*"),
                Actions = [new SlackButton("Open") { ActionId = "open", Value = "DCW-1024", Style = SlackButtonStyle.Primary }],
            },
        ]),
    ],
};
```

Rich text is how Slack's own composer writes, and what a `SlackRichTextInput` hands back:

<!-- ikon-example: connectors-slack-rich-text -->
```csharp
var notes = new SlackRichTextBlock(
[
    new SlackRichTextSection(
    [
        new SlackRichTextRun("Release ") { Style = new SlackTextStyle { Bold = true } },
        new SlackRichTextLink("https://ikon.live/releases/412", "412"),
        new SlackRichTextRun(" is out, "),
        new SlackRichTextBroadcast(SlackBroadcastRange.Here),
        new SlackRichTextEmoji("tada"),
    ]),
    new SlackRichTextList(SlackListStyle.Bullet,
    [
        new SlackRichTextSection([new SlackRichTextRun("Reviewed by "), new SlackRichTextUser("U0123456789")]),
        new SlackRichTextSection([new SlackRichTextRun("Owned by "), new SlackRichTextUsergroup("S0123456789")]),
        new SlackRichTextSection([new SlackRichTextRun("Discussed in "), new SlackRichTextChannel("C0123456789")]),
    ]),
    new SlackRichTextQuote([new SlackRichTextRun("Ships at "), new SlackRichTextDate(1790000000, "{date_short_pretty} at {time}", "soon")]),
    new SlackRichTextPreformatted([new SlackRichTextRun("ikon deploy --target prod")]) { Language = "bash" },
    new SlackRichTextSection([new SlackRichTextRun("Brand colour "), new SlackRichTextColor("#F405B3")]),
]);
```

A form is a run of `SlackInputBlock`s, each with a `BlockId` and an element `ActionId` to look its value up by once submitted (see Modals below). An external select asks the app for its options as the person types:

<!-- ikon-example: connectors-slack-form -->
```csharp
var priority = new SlackOption(SlackText.Plain("High"), "high");

SlackBlock[] form =
[
    new SlackInputBlock("Title", new SlackPlainTextInput { ActionId = "title", MaxLength = 120 }) { BlockId = "title" },
    new SlackInputBlock("Details", new SlackRichTextInput { ActionId = "details", MaxLines = 6 }) { BlockId = "details", Optional = true },
    new SlackInputBlock("Priority", new SlackStaticSelect("Pick one")
    {
        ActionId = "priority",
        OptionGroups = [new SlackOptionGroup("Urgent", [priority]), new SlackOptionGroup("Later", [new SlackOption(SlackText.Plain("Low"), "low")])],
        InitialOption = priority,
    })
    { BlockId = "priority" },
    new SlackInputBlock("Labels", new SlackMultiStaticSelect { ActionId = "labels", Options = [new SlackOption(SlackText.Plain("Bug"), "bug")] }) { BlockId = "labels", Optional = true },
    new SlackInputBlock("Customer", new SlackExternalSelect("Search customers") { ActionId = "customer", MinQueryLength = 2 }) { BlockId = "customer" },
    new SlackInputBlock("Related", new SlackMultiExternalSelect { ActionId = "related" }) { BlockId = "related", Optional = true },
    new SlackInputBlock("Assignee", new SlackUsersSelect { ActionId = "assignee" }) { BlockId = "assignee" },
    new SlackInputBlock("Watchers", new SlackMultiUsersSelect { ActionId = "watchers", MaxSelectedItems = 5 }) { BlockId = "watchers", Optional = true },
    new SlackInputBlock("Post to", new SlackConversationsSelect
    {
        ActionId = "post_to",
        DefaultToCurrentConversation = true,
        ResponseUrlEnabled = true,
        Filter = new SlackConversationFilter(["public", "private"]) { ExcludeBotUsers = true },
    })
    { BlockId = "post_to" },
    new SlackInputBlock("Also notify", new SlackMultiConversationsSelect { ActionId = "notify" }) { BlockId = "notify", Optional = true },
    new SlackInputBlock("Channel", new SlackChannelsSelect { ActionId = "channel" }) { BlockId = "channel", Optional = true },
    new SlackInputBlock("Channels", new SlackMultiChannelsSelect { ActionId = "channels" }) { BlockId = "channels", Optional = true },
    new SlackInputBlock("Due", new SlackDatePicker { ActionId = "due" }) { BlockId = "due" },
    new SlackInputBlock("At", new SlackTimePicker("09:00") { ActionId = "at" }) { BlockId = "at", Optional = true },
    new SlackInputBlock("Remind", new SlackDateTimePicker { ActionId = "remind" }) { BlockId = "remind", Optional = true },
    new SlackInputBlock("Estimate (h)", new SlackNumberInput(IsDecimalAllowed: true) { ActionId = "estimate", MinValue = "0" }) { BlockId = "estimate", Optional = true },
    new SlackInputBlock("Reporter email", new SlackEmailInput { ActionId = "email" }) { BlockId = "email", Optional = true },
    new SlackInputBlock("Link", new SlackUrlInput { ActionId = "link" }) { BlockId = "link", Optional = true },
    new SlackInputBlock("Screenshots", new SlackFileInput(["png", "jpg"]) { ActionId = "screenshots", MaxFiles = 3 }) { BlockId = "screenshots", Optional = true },
    new SlackInputBlock("Notify", new SlackCheckboxes([new SlackOption(SlackText.Markdown("*Email* me"), "email")]) { ActionId = "notify_me" }) { BlockId = "notify_me", Optional = true },
    new SlackInputBlock("Visibility", new SlackRadioButtons([new SlackOption(SlackText.Plain("Team"), "team")]) { ActionId = "visibility" }) { BlockId = "visibility" },
    new SlackInputBlock("Search", new SlackPlainTextInput
    {
        ActionId = "search",
        DispatchActionConfig = new SlackDispatchActionConfig(["on_character_entered"]),
    })
    { BlockId = "search", DispatchAction = true, Optional = true },
    new SlackSectionBlock(SlackText.Markdown("More"))
    {
        Accessory = new SlackOverflow([new SlackOption(SlackText.Plain("Open docs"), "docs") { Url = "https://ikon.live/docs" }]) { ActionId = "more" },
    },
    new SlackImageBlock("https://ikon.live/logo.png", "Ikon logo"),
    new SlackVideoBlock("Walkthrough", "https://ikon.live/embed/intro", "https://ikon.live/intro.jpg", "Intro video"),
    new SlackActionsBlock(
    [
        new SlackWorkflowButton("Escalate", new SlackWorkflow(new SlackWorkflowTrigger("https://slack.com/shortcuts/Ft0123ABC456/xyz")
        {
            CustomizableInputParameters = [new SlackWorkflowInput("ticket", "DCW-1024")],
        }))
        { ActionId = "escalate" },
    ]),
];
```

An AI reply shows its working with a plan and task cards, answers in a markdown block, and offers feedback beneath:

<!-- ikon-example: connectors-slack-agent-blocks -->
```csharp
var details = new SlackRichTextBlock([new SlackRichTextSection([new SlackRichTextRun("Read 14 tickets")])]);

SlackBlock[] reply =
[
    new SlackPlanBlock("Looking into the outage",
    [
        new SlackPlanTask("search", "Searched the incident channel", SlackTaskStatus.Complete) { Output = details },
        new SlackPlanTask("draft", "Drafting a summary", SlackTaskStatus.InProgress),
    ]),
    new SlackTaskCardBlock("sources", "Checked the status page", SlackTaskStatus.Complete)
    {
        Sources = [new SlackUrlSource("https://status.ikon.live", "status.ikon.live")],
    },
    SlackBlock.Markdown(answer),
    new SlackContextActionsBlock(
    [
        new SlackFeedbackButtons(new SlackFeedbackButton("Good", "good"), new SlackFeedbackButton("Bad", "bad")) { ActionId = "feedback" },
        new SlackIconButton("trash", "Delete") { ActionId = "delete" },
    ]),
];

// In a modal, an alert block draws attention to a notice.
SlackBlock notice = new SlackAlertBlock(SlackText.Plain("This answer was drafted by an agent")) { Level = SlackAlertLevel.Warning };
```

A block or element this library does not model goes in as JSON with `SlackBlock.Raw(json)` or `SlackElement.Raw(json)` and is sent exactly as given. Slack checks the limits — 50 blocks a message, 100 a view, 3000 characters of section text — and refuses a message over them with `invalid_blocks`, its message naming the offending field. For blocks built from data, such as a model's output, `ValidateBlocksAsync(blocks)` asks Slack first and returns each `SlackBlockProblem` with a JSON `Pointer` to the field, or nothing when they are valid.

Reading a message back, `SlackMessage` carries its `Blocks` as Slack's raw JSON, with `Edited` (a `SlackEdit` naming who and when), the thread's `ReplyCount`, `ReplyUsers` and `LatestReply`, its `Reactions`, `Metadata`, and the `BotId` and `AppId` that mark a post by an app.

### Reading history

Slack timestamps (`Ts`, `ThreadTs`, `oldestTs`) are **raw Slack `ts` strings** (e.g. `"1727694230.000200"`), not `DateTime`s. Treat them as opaque ordered cursors and pass them back verbatim.

`HistoryAsync(channel, limit)` fetches one page of recent messages. `HistoryPageAsync(channel, limit, cursor)` fetches the same page as a `SlackHistoryPage` that also carries Slack's own `HasMore` and the `NextCursor` that reads the next, older page. `HistorySinceAsync(channel, oldestTs)` fetches every **top-level** message with `ts > oldestTs`, following pagination to completion and returning the result **oldest-first**, so a caller that advances a cursor per message never leaves a gap in the channel's own timeline:

<!-- ikon-example: connectors-slack-history -->
```csharp
var messages = await slack.HistorySinceAsync(channelId, oldestTs: lastSeenTs);

foreach (var message in messages)
{
    await ProcessAsync(message);
    lastSeenTs = message.Ts;   // safe: oldest-first means no gap on interruption
}
```

**In-thread replies are not in that result, and nothing reports their absence.** Both methods call `conversations.history`, which returns only the messages posted to the channel itself; a reply posted inside a thread is reached by `RepliesAsync` on its parent's `ThreadTs`. So a channel feed built on `HistorySinceAsync` alone silently drops every threaded reply, however far the cursor advances. A message that owns a thread carries its own `ts` as `ThreadTs` — read each such thread with `RepliesAsync` if replies matter to you.

Paging is bounded by `maxPages` (default 50 pages of `pageLimit` 200), and the bound is never silent: when it trips with Slack still reporting a `next_cursor`, the call throws `ConnectorPageCapException<SlackMessage>` instead of returning, with empty `Items` and a null `ResumeFrom`. Because Slack pages **backward in time**, the messages read are the most recent ones and the unread gap sits **below** them, so none are handed out — keep your cursor at the `oldestTs` you called with, and close the gap by calling again with a larger `maxPages` or by reading narrower windows with the `HistorySinceAsync(channel, oldestTs, latestTs)` overload. `maxPages` must be positive and `pageLimit` 1–1000, or the call throws `ArgumentOutOfRangeException`. A caller that stays under the bound sees no exception at all.

### Conversations

`ListConversationsAsync` returns the public and private channels the token can see, paging up to `maxPages`; a workspace with more channels than the cap admits gets a `ConnectorPageCapException<SlackConversation>` carrying the channels read so far as `Items` and a null `ResumeFrom`, never a shortened list presented as the total — raise `maxPages` for such a workspace. Archived channels are included; filter on `SlackConversation.IsArchived` before posting, which fails on one with `is_archived`. `ListConversationsAsync(types, excludeArchived)` lists other kinds too — `SlackConversationTypes` flags `Public`, `Private`, `Im` and `Mpim`, each needing its own read scope — and `ListUserConversationsAsync(userId, types)` only those one person (or, with no id, the app itself) is in. `GetConversationAsync(channelId)` fetches one. All hand back `SlackConversation` records — `Id`, `Name`, `IsMember`, the three shape flags `IsPrivate`, `IsIm` and `IsMpim` that separate a channel from a DM or a group DM, its `Topic`, `Purpose`, `MemberCount` and `Creator`, `IsShared` and `IsExtShared` for one shared with another workspace or organisation, and for a DM the `UserId` on the other side. `ListMembersAsync(channelId)` returns the user ids in one.

The app joins a public channel with `JoinAsync` (`channels:join`); a private one has to invite it. `CreateConversationAsync(name, isPrivate)`, `RenameAsync`, `SetTopicAsync`, `SetPurposeAsync`, `InviteAsync(channelId, userIds)`, `KickAsync`, `LeaveAsync`, `ArchiveAsync` and `UnarchiveAsync` manage channels, each needing the `:manage` or `:write` scope of its kind. To write to a person, `OpenDirectMessageAsync([userId])` opens (or finds) the app's DM with them — up to eight ids make a group DM — and returns the id to post to; `CloseAsync` takes it out of the sidebar again and `MarkAsync(channelId, ts)` moves a user token's read cursor.

<!-- ikon-example: connectors-slack-dm -->
```csharp
if (await slack.LookupUserByEmailAsync(email) is { IsDeleted: false } person)
{
    var dm = await slack.OpenDirectMessageAsync([person.Id]);
    await slack.PostAsync(dm, $"Hi {person.DisplayName ?? person.RealName}, your report is ready");
}
```

### Files

`UploadFileAsync(upload, share)` uploads a `SlackFileUpload` — a file name, the content as a `Stream` with its exact `Length` (Slack needs the size before the upload starts), and an optional `Title`, `AltText` and `SnippetType` — and shares it into the channel or thread a `SlackFileShare` names, with an `InitialComment` posted alongside. `SlackFileUpload.FromBytes(name, bytes)` wraps a byte array. Without a share the file stays private to the app until it is shared. `UploadFilesAsync` uploads several and shares them as one message. A seekable stream is rewound and sent again if Slack throttles the upload; one that cannot seek is sent once. The caller's stream is never closed. Uploading needs `files:write`.

<!-- ikon-example: connectors-slack-upload -->
```csharp
await using var content = File.OpenRead(path);

var file = await slack.UploadFileAsync(
    new SlackFileUpload(Path.GetFileName(path), content, content.Length) { Title = "Monthly report" },
    new SlackFileShare(channelId) { ThreadTs = threadTs, InitialComment = "Here is this month's report" });
```

`GetFileAsync(fileId)` reads one file, `ListFilesAsync(channel, user, types, from, to)` lists them newest first — throwing `ConnectorPageCapException<SlackFile>` with the files read when `maxPages` pages of 100 are not enough — and `DeleteFileAsync` deletes one the app owns.

A file shared into a message arrives as a `SlackFile` (`Id`, `MimeType`, and a `DownloadUrl` that is null when Slack sends no `url_private_download` for it, plus `Name`, `Title`, `Size`, `FileType`, `Permalink`, `Created` and the sharing `User`). `DownloadFileAsync(url)` streams a shared file's `url_private_download` with the bot token — dispose the stream to release the connection — and `DownloadFileBytesAsync(url, maxBytes)` reads it whole, refusing rather than truncating a file larger than `maxBytes`; when Slack answers with its HTML sign-in page instead (the token lacks `files:read` or the file is not shared with the bot), it throws `ConnectorException` rather than returning that page as the file. It fetches Slack-owned hosts only (`slack.com` and subdomains); any other URL — e.g. one parsed out of untrusted message text — throws `ArgumentException` without a request, so the token can never leak to another server.

### Receiving events

Slack tells an app what happens in a workspace — a message, a mention, a reaction, a person opening the app's Home — through the Events API: it posts each event to a URL the app gives in its settings, or sends it down a Socket Mode connection (below). Over HTTP the endpoint is an `[HttpPost]` with `Auth = EndpointAuth.Public`, because Slack carries no Ikon credential: check every request with `SlackAuth.VerifyRequest` (see "Installing the app and checking requests") and read it with `SlackEvents.Parse(body)`.

<!-- ikon-example: connectors-slack-events -->
```csharp
[HttpPost("/slack/events", Auth = EndpointAuth.Public)]
public HttpResult OnSlackEvent(Ikon.App.HttpRequest request)
{
    if (!SlackAuth.VerifyRequest(_signingSecret, request.Headers.GetValueOrDefault("X-Slack-Request-Timestamp"),
            request.Headers.GetValueOrDefault("X-Slack-Signature"), request.Body))
    {
        return HttpResult.Unauthorized();
    }

    switch (SlackEvents.Parse(request.Body))
    {
        case SlackUrlVerification verification:
            return HttpResult.Text(verification.Challenge);

        case SlackEventCallback { Event: SlackAppMentionEvent mention }:
            // Slack wants its answer within three seconds, so the reply is posted after it.
            _ = Task.Run(() => _slack.PostAsync(mention.Message.Channel, "On it", mention.Message.ThreadTs ?? mention.Message.Ts));
            break;

        case SlackEventCallback { Event: SlackAppHomeOpenedEvent { Tab: "home" } home }:
            _ = Task.Run(() => _slack.PublishHomeAsync(home.User, SlackView.Home([SlackBlock.Header("Your tickets")])));
            break;
    }

    return HttpResult.Ok();
}
```

`SlackEvents.Parse` returns a `SlackEnvelope`: a `SlackUrlVerification` when Slack checks the URL — answer with its `Challenge` — a `SlackEventCallback` for an event, `SlackAppRateLimited` when Slack has stopped sending the workspace's events for the rest of the minute, and null for an envelope type Slack adds later. A callback names the `TeamId`, its `Authorizations` (each a `SlackAuthorization`: which install it concerns), and an `EventId` that is the same on every retry of one event. Its `Event` is a `SlackEvent`:

| Event | Record |
|---|---|
| a message posted, edited or deleted where the app listens | `SlackMessageEvent` (the `SlackMessage` and its `ChannelType`), `SlackMessageChangedEvent`, `SlackMessageDeletedEvent` |
| the app @-mentioned | `SlackAppMentionEvent` |
| a reaction or pin added or removed | `SlackReactionEvent`, `SlackPinEvent` |
| someone joined or left a channel | `SlackMemberChannelEvent` |
| a channel created, renamed, archived, deleted, shared | `SlackChannelEvent` |
| the app's Home, Messages or About tab opened | `SlackAppHomeOpenedEvent` |
| a file shared, created, changed or deleted | `SlackFileEvent` |
| a link to the app's domain shared | `SlackLinkSharedEvent` with its `SlackSharedLink`s, answered with `UnfurlAsync` |
| a person joined the workspace or changed their profile | `SlackUserEvent` |
| the app uninstalled, or its tokens revoked | `SlackAppUninstalledEvent`, `SlackTokensRevokedEvent` |
| a thread with the app's assistant started or changed context | `SlackAssistantThreadEvent` |
| what an agent app's person is looking at changed | `SlackAppContextChangedEvent` of `SlackContextEntity`s |
| a person stopped an agent's reply, or renamed its session | `SlackAgentSessionStoppedEvent`, `SlackAgentSessionTitleChangedEvent` |

Any other type arrives as `SlackUnknownEvent`, and every event keeps Slack's JSON in `Raw` for a field no record names. What each one carries, and the interactions below beside them:

<!-- ikon-example: connectors-slack-event-kinds -->
```csharp
private static string Describe(SlackEnvelope? envelope) => envelope switch
{
    SlackAppRateLimited limited => $"Slack paused events for {limited.TeamId} this minute",
    SlackEventCallback { Event: var slackEvent } => slackEvent switch
    {
        SlackMessageEvent { Message.BotId: not null } => "a bot's post, perhaps the app's own",
        SlackMessageEvent { ChannelType: "im" } message => $"DM from {message.Message.User}: {message.Message.Text}",
        SlackMessageEvent message => $"{message.Message.User} in {message.Message.Channel}",
        SlackMessageChangedEvent edit => $"edited from '{edit.Previous?.Text}' to '{edit.Message.Text}'",
        SlackMessageDeletedEvent deleted => $"{deleted.DeletedTs} deleted",
        SlackReactionEvent { Added: true } reaction => $":{reaction.Reaction}: on {reaction.Ts}",
        SlackPinEvent pin => $"{pin.Ts} {(pin.Added ? "pinned" : "unpinned")}",
        SlackMemberChannelEvent member => $"{member.User} {(member.Joined ? "joined" : "left")} {member.Channel}",
        SlackChannelEvent channel => $"{channel.Type}: {channel.Name ?? channel.Channel}",
        SlackFileEvent file => $"{file.Type} {file.FileId}",
        SlackLinkSharedEvent link => $"unfurl {string.Join(", ", link.Links.Select(shared => shared.Url))}",
        SlackUserEvent person => $"{person.Type}: {person.User.Name}",
        SlackAppUninstalledEvent or SlackTokensRevokedEvent => "forget this workspace's tokens",
        SlackAssistantThreadEvent thread => $"assistant thread {thread.ThreadTs}, looking at {thread.ContextChannel}",
        SlackAppContextChangedEvent context => $"viewing {context.Entities.FirstOrDefault()?.Value}",
        SlackAgentSessionStoppedEvent stopped => $"stop streaming {string.Join(", ", stopped.StreamingMessageTs)}",
        SlackAgentSessionTitleChangedEvent renamed => $"session renamed to {renamed.Title}",
        SlackUnknownEvent other => $"{other.Type}, read from Raw",
        _ => slackEvent.Type,
    },
    _ => "nothing to do",
};

private static string Describe(SlackInteraction interaction) => interaction switch
{
    SlackShortcut shortcut => $"global shortcut {shortcut.CallbackId}",
    SlackMessageShortcut shortcut => $"{shortcut.CallbackId} on '{shortcut.Message.Text}'",
    SlackViewClosed closed => $"{closed.View.CallbackId} dismissed",
    SlackUnknownInteraction other => $"{other.Type}, read from Raw",
    _ => interaction.Type,
};

private static string Describe(SlackSocketRequest request) => request switch
{
    SlackSocketEvent socketEvent => Describe(socketEvent.Callback),
    SlackSocketInteraction socketInteraction => Describe(socketInteraction.Interaction),
    SlackSocketSlashCommand socketCommand => socketCommand.Command.Command,
    _ => request.EnvelopeId,
};
```

Slack wants a 2xx within three seconds, and resends an event it got no answer for — up to three times, the retry carrying an `X-Slack-Retry-Num` header — so answer first and work after, and deduplicate on `EventId` when a repeat would do harm. An app's own posts come back as events too: skip a `SlackMessage` whose `BotId` is set, or whose `User` is the app's bot user from `AuthTestAsync`.

### Interactivity and slash commands

A person's buttons, menus, form submissions and shortcuts arrive at the app's interactivity URL; `SlackInteractions.Parse(body)` reads one into a `SlackInteraction` carrying the `User` (a `SlackInteractionUser`), the team, and the `TriggerId` that can open a modal within three seconds:

- `SlackBlockActions` — a button pressed or a value picked: each `SlackAction` with its `ActionId`, `BlockId` and a `SlackInputValue`, plus the `Message` or `View` it happened in, the surface's whole `State`, and a `ResponseUrl` for answering in a message's conversation.
- `SlackViewSubmission` — a modal submitted: its `View` (a `SlackViewInfo`) holds the `State`, the `CallbackId` and `PrivateMetadata` the app gave it, and `ResponseUrls` holds a `SlackResponseUrlTarget` per conversation picker that asked for one.
- `SlackViewClosed`, `SlackShortcut` (a global shortcut), `SlackMessageShortcut` (a shortcut on a message), and `SlackBlockSuggestion` — an external select asking for options as the person types.
- `SlackUnknownInteraction` for anything else, with `Raw`.

A view's `State` is a `SlackViewState`: `SlackViewState.Get(blockId, actionId)` returns the `SlackInputValue`, whose fields follow the element — `Value` for text, number, email and URL inputs and buttons; a `SlackSelectedOption` or several for selects, checkboxes and radio buttons; `SelectedUser(s)`, `SelectedConversation(s)`, `SelectedChannel(s)`, `SelectedDate`, `SelectedTime` and `SelectedDateTime` for the pickers; `RichTextValue` and `Files` for those inputs. An optional field the person left empty may be missing, so `Get` returns null for it.

The answer to an interaction is its HTTP body, and `SlackAck` builds each as JSON for `HttpResult.Json`: `ViewErrors` keeps a modal open with an error under each named input, `UpdateView` and `PushView` replace or stack it, `ClearViews` closes the whole stack, `Options` and `OptionGroups` answer a block suggestion, and `Message` replies to a slash command, a shortcut or a button. An empty 200 closes a submitted modal.

<!-- ikon-example: connectors-slack-interactivity -->
```csharp
[HttpPost("/slack/interactivity", Auth = EndpointAuth.Public)]
public HttpResult OnSlackInteraction(Ikon.App.HttpRequest request)
{
    if (!Verified(request))
    {
        return HttpResult.Unauthorized();
    }

    switch (SlackInteractions.Parse(request.Body))
    {
        case SlackViewSubmission { View.CallbackId: "new_ticket" } submission:
            var title = submission.View.State.Get("title", "title")?.Value;

            if (string.IsNullOrWhiteSpace(title))
            {
                return HttpResult.Json(SlackAck.ViewErrors(new Dictionary<string, string> { ["title"] = "Give the ticket a title" }));
            }

            _ = Task.Run(() => CreateTicketAsync(title, submission.User.Id));
            return HttpResult.Ok();   // an empty 200 closes the modal

        case SlackBlockActions { Actions: [{ ActionId: "rollback" } action], ResponseUrl: { } responseUrl }:
            _ = Task.Run(() => SlackResponseUrl.SendAsync(responseUrl, $"Rolling back {action.Value.Value}", replaceOriginal: true));
            return HttpResult.Ok();

        case SlackBlockSuggestion { ActionId: "customer" } suggestion:
            return HttpResult.Json(SlackAck.Options(FindCustomers(suggestion.Value)));

        default:
            return HttpResult.Ok();
    }
}
```

A slash command posts a form to its own URL; `SlackSlashCommand.Parse(body)` reads its `Command`, `Text`, `UserId`, `ChannelId`, `ResponseUrl` and `TriggerId`. Answer within three seconds with an empty 200 or `SlackAck.Message`, which only the person sees unless it is `InChannel`; anything later goes through the `ResponseUrl`.

<!-- ikon-example: connectors-slack-command -->
```csharp
[HttpPost("/slack/commands", Auth = EndpointAuth.Public)]
public async Task<HttpResult> OnSlackCommandAsync(Ikon.App.HttpRequest request)
{
    if (!Verified(request))
    {
        return HttpResult.Unauthorized();
    }

    var command = SlackSlashCommand.Parse(request.Body);

    if (command.Command != "/ticket")
    {
        return HttpResult.Json(SlackAck.Message($"I don't know {command.Command}"));
    }

    // A trigger id works once, within three seconds: open the modal before anything slow.
    var form = new SlackInputBlock("Title", new SlackPlainTextInput { ActionId = "title" }) { BlockId = "title" };
    await _slack.OpenViewAsync(command.TriggerId, SlackView.Modal("New ticket", [form], submit: "Create") with { CallbackId = "new_ticket" });
    return HttpResult.Ok();
}
```

### Modals and App Home

A `SlackView` is a modal (`SlackView.Modal(title, blocks, submit)`) or a person's Home tab (`SlackView.Home(blocks)`) — its `SlackViewType` — of up to 100 blocks, with the `CallbackId`, `PrivateMetadata` (3000 characters the app gets back with every interaction on it) and `ExternalId` the app wants, and `NotifyOnClose` for a `SlackViewClosed`. `OpenViewAsync(triggerId, view)` opens a modal and `PushViewAsync` stacks another on it, three deep at most; both need a trigger id from the interaction, which works once and within three seconds — open a modal before slow work, and fill it in afterwards. `UpdateViewAsync(view, viewId or externalId, hash)` replaces an open one at any time; with the `Hash` of the `SlackViewInfo` last read, a change made since fails with `hash_conflict` instead of being overwritten. `PublishHomeAsync(userId, view)` sets what a person's Home shows — publish it when a `SlackAppHomeOpenedEvent` arrives, and again whenever its content changes.

### Streaming agent replies

An AI reply can grow in Slack as the model writes it, as Slack's own agents do. `StreamAsync(channel, deltas, options, stop)` takes any `IAsyncEnumerable<string>` of text — an `Emerge.Run`'s `ModelText` tokens, say — and streams it into one message, stopping the stream when the text ends or the source throws:

<!-- ikon-example: connectors-slack-stream -->
```csharp
var run = Emerge.Run<string>(LLMModel.Claude46Sonnet, pass => pass.Command = mention.Message.Text);
var feedback = new SlackContextActionsBlock(
    [new SlackFeedbackButtons(new SlackFeedbackButton("Good", "good"), new SlackFeedbackButton("Bad", "bad")) { ActionId = "feedback" }]);

await slack.StreamAsync(
    mention.Message.Channel,
    run.OfType<ModelText<string>>().Select(token => token.Text),
    new SlackStreamOptions { ThreadTs = mention.Message.ThreadTs ?? mention.Message.Ts },
    new SlackStreamStop { Blocks = [feedback] });
```

A run that retries streams the new attempt's `ModelText` after the abandoned one's, and this filter posts both into the message; where that matters, post the `Completed` result instead, or start a new message at each `Retry` or `Stage`.

`SlackStreamOptions` names the `ThreadTs` to reply in, and in a channel rather than a DM the `RecipientUserId` and `RecipientTeamId` the reply is for. For more than text, `StartStreamAsync` returns a `SlackMessageStream` to append to: `AppendMarkdownAsync` for text, and `AppendAsync` for a `SlackStreamChunk` — a `SlackMarkdownChunk`, a `SlackTaskUpdateChunk` showing a step of the work with its `SlackTaskStatus` (a later chunk with the same `Id` updates it), a `SlackPlanUpdateChunk` titling the tasks when `TaskDisplayMode` is `SlackTaskDisplayMode.Plan`, or a `SlackBlocksChunk` of blocks. `StopAsync` ends the reply with a `SlackStreamStop`: final `Blocks` such as feedback buttons, `Metadata`, and the `SlackSessionStatus` to leave the session in. Disposing a stream not yet stopped stops it.

<!-- ikon-example: connectors-slack-stream-steps -->
```csharp
await using var stream = await slack.StartStreamAsync(channel, new SlackStreamOptions { ThreadTs = threadTs, TaskDisplayMode = SlackTaskDisplayMode.Plan });

await stream.AppendAsync(SlackStreamChunk.Plan("Looking into the outage"));
await stream.AppendAsync(new SlackTaskUpdateChunk("logs", "Reading the error logs", SlackTaskStatus.InProgress));
// ... the work ...
await stream.AppendAsync(new SlackTaskUpdateChunk("logs", "Read the error logs", SlackTaskStatus.Complete) { Output = "412 errors since 09:14" });
await stream.AppendAsync(new SlackBlocksChunk([SlackBlock.Divider()]));
await stream.AppendMarkdownAsync("The **api** deploy at 09:12 broke sign-in; rolling back fixes it.");

await stream.StopAsync(new SlackStreamStop { SessionStatus = SlackSessionStatus.Active });
```

Appends are buffered and sent at most once per `FlushInterval` (a second by default) and whenever the buffer reaches Slack's limits, so a token-by-token feed costs one request a second, not one per token — Slack allows about a hundred appends a minute per workspace, shared by every stream. A send that fails keeps its text buffered: a background one throws once from the next append or flush, and `StopAsync` still sends what was kept; a stop that fails can be tried again.

An agent app also tells Slack where its session stands — `SetAgentStatusAsync(SlackSessionStatus.Processing, ...)` while it works, which falls back to `Active` after an hour unless set again — renames the session with `RenameAgentSessionAsync`, and offers up to four `SlackSuggestedPrompt`s to a person who opens it with `SetSuggestedPromptsAsync`. A person who presses stop sends a `SlackAgentSessionStoppedEvent` naming the messages still streaming: stop them, since Slack leaves the session's status to the app.

<!-- ikon-example: connectors-slack-agent-session -->
```csharp
await slack.SetSuggestedPromptsAsync(thread.Channel,
[
    new SlackSuggestedPrompt("Summarise", "Summarise this channel"),
    new SlackSuggestedPrompt("Open tickets", "Which tickets are still open?"),
]);

await slack.SetAgentStatusAsync(SlackSessionStatus.Processing, thread.Channel, thread.ThreadTs, title: "Ticket review");
await slack.RenameAgentSessionAsync("Ticket review: platform", thread.Channel, thread.ThreadTs);
```

### Socket Mode

An app that should not, or cannot, take requests at a public URL receives the same events, interactions and slash commands over Socket Mode: a WebSocket it opens to Slack. Turn Socket Mode on in the Slack app's settings and create an **app-level token** (`xapp-...`) with `connections:write` — a credential separate from the bot token. `SlackSocketClient(appToken, handler)` runs it: `RunAsync(ct)` connects, hands each request to the handler as a `SlackSocketRequest` — a `SlackSocketEvent` (its `Callback` is the `SlackEventCallback` the HTTP endpoint would have read), a `SlackSocketInteraction` or a `SlackSocketSlashCommand` — and acknowledges it when the handler returns. For an interaction or slash command, the JSON the handler returns, a `SlackAck` body, is the answer; null acknowledges with none.

<!-- ikon-example: connectors-slack-socket -->
```csharp
var socket = new SlackSocketClient(appToken, async (request, ct) =>   // xapp-..., not the xoxb- bot token
{
    switch (request)
    {
        case SlackSocketEvent { Callback.Event: SlackAppMentionEvent mention }:
            await slack.PostAsync(mention.Message.Channel, "On it", mention.Message.ThreadTs ?? mention.Message.Ts, ct);
            return null;

        case SlackSocketSlashCommand { Command.Command: "/status" }:
            return SlackAck.Message("All systems go");

        default:
            return null;
    }
});

await socket.RunAsync(stoppingToken);
```

The handler has `SlackSocketOptions.AckTimeout` (2.5 seconds) before Slack is acknowledged without its answer while it keeps running; a handler that throws is logged and acknowledged empty, so Slack does not resend it. When Slack asks for a reconnect, the client opens the next connection before the old one closes, so nothing sent in between is lost; a connection that drops is reopened after a growing delay, up to `MaxReconnectDelay`. `RunAsync` throws `ConnectorException` with `StatusCode` `401` when Slack refuses the app token or Socket Mode is turned off for the app. Socket Mode suits an app installed in its own workspaces: Slack does not list a Socket Mode app in its Marketplace, so an app distributed there takes requests over HTTP. `OpenSocketUrlAsync(appToken)` returns a single connection URL, for an app running its own client.

`Slack.ParseMessage(JsonElement, channel)` maps a raw message object — from a history page or an event payload — to a `SlackMessage`, returning `null` for non-message objects (no `ts`).

### Threads and people

`RepliesAsync(channel, threadTs)` reads a whole thread — the parent first, then its replies oldest-first — which is how a feed built on `HistorySinceAsync` picks up the replies it leaves out: for each message whose `ThreadTs` is its own `Ts`, read its thread. It pages up to `maxPages` and throws `ConnectorPageCapException<SlackMessage>` with the messages read past it. `GetUserAsync(userId)` turns the user id a message names into a `SlackUser` — `Name`, `RealName`, `DisplayName`, `IsBot`, `IsDeleted`, `TimeZone`, `Title`, `AvatarUrl`, the `IsAdmin`, `IsOwner` and guest (`IsRestricted`, `IsUltraRestricted`) flags, and `Email` when the token has `users:read.email`. `ListUsersAsync` lists the whole workspace, bots and deactivated accounts included, and `LookupUserByEmailAsync(email)` finds one person by address, or null. `GetUserProfileAsync(userId)` reads the `SlackUserProfile` — status text, emoji and expiry, title, phone and pronouns — and `GetPresenceAsync` the `SlackPresence`. `GetTeamAsync()` describes the workspace as a `SlackTeam`, and `ListEmojiAsync()` its custom emoji.

### Installing the app and checking requests

`SlackAuth` runs the OAuth v2 install: send a workspace member to `AuthorizeUrl` with the bot scopes the app needs, receive the `code` at your redirect endpoint, and redeem it with `ExchangeCodeAsync`. The `SlackInstallation` it returns carries the `BotToken` a `Slack` client takes, the workspace's `TeamId` and `TeamName`, and the `Scopes` actually granted. A reused or expired code, or a wrong client secret, is a `401` with Slack's code in `ErrorCode`:

<!-- ikon-example: connectors-slack-install -->
```csharp
var installUrl = SlackAuth.AuthorizeUrl(clientId, ["channels:history", "chat:write"], redirectUri, state);

// ... a workspace member installs the app; the redirect back carries `code`:
var installation = await SlackAuth.ExchangeCodeAsync(clientId, clientSecret, code, redirectUri);
await SaveBotTokenAsync(installation.TeamId, installation.BotToken);
```

Pass `userScopes` as well and the installer's own token comes back in `installation.AuthedUser` — a `SlackAuthedUser` with their `Id`, the `Scopes` they granted and `Tokens` for a second `Slack` client that acts as them; an install that asks for user scopes only has an empty `BotToken`. An app that asked for `incoming-webhook` also gets the `SlackIncomingWebhook` the installer picked, whose `Url` posts to that channel without a token and is a secret to keep. An app installed with PKCE has no client secret: put `PkceCodes.Create()`'s `Challenge` in `AuthorizeUrl` as `codeChallenge`, keep its `Verifier` with the state, and pass `null` as the secret and the verifier as `codeVerifier` to `ExchangeCodeAsync`. `SlackAuth.RevokeAsync(token)` revokes a token when a person disconnects, and treats one that already stopped working as revoked.

`slack.AuthTestAsync()` answers who a token acts as — a `SlackIdentity` with the workspace's `TeamId`, the token's `UserId` (the bot user's id, which is how an app recognises its own messages among the ones it reads) and its `BotId` — and is the cheapest way to check that a stored token still works.

#### Rotating tokens

With token rotation on in the Slack app's settings — Slack turns it on for every PKCE app, and it cannot be turned off again — an access token lasts 12 hours and `SlackInstallation` carries the `BotRefreshToken` and `BotExpiresAt` that renew it, together as `BotTokens`. Hand them to a `SlackTokenProvider`, and construct the client with it instead of a token string: it refreshes five minutes before expiry, and once more if Slack still calls the token expired, and calls `onTokensRotated` with every new `SlackTokens` pair. Slack retires the old pair at each refresh, so that callback must save the new one in place of the stored one — a restart that reads the old pair back has to install again. A refresh token Slack refuses is a `401`.

<!-- ikon-example: connectors-slack-rotation -->
```csharp
var tokens = new SlackTokenProvider(installation.BotTokens, clientId, clientSecret,
    onTokensRotated: (rotated, ct) => SaveBotTokensAsync(installation.TeamId, rotated, ct));
var slack = new Slack(tokens);
```

An endpoint Slack calls — the Events API, slash commands, interactivity — is public, so anyone can call it. `SlackAuth.VerifyRequest` checks the `X-Slack-Signature` header against the raw body signed with the app's signing secret, and refuses an `X-Slack-Request-Timestamp` more than five minutes off, so a captured request cannot be replayed. Pass the body exactly as it arrived:

<!-- ikon-example: connectors-slack-verify -->
```csharp
if (!SlackAuth.VerifyRequest(signingSecret, timestampHeader, signatureHeader, body))
{
    return false;   // not Slack's, or replayed: answer 401 and do nothing
}
```

### Rate limits

Slack limits each Web API method per app and workspace, and answers a call past its limit with a `429` that the connector retries on Slack's `Retry-After` (see the top of this guide). Posting runs at about one message a second per channel. A commercially distributed app that is not listed in Slack's Marketplace — as opposed to an app built for its own workspaces — may read `conversations.history` and `conversations.replies` (`HistoryAsync`, `HistorySinceAsync`, `RepliesAsync`) only once a minute, at most 15 messages a page; for such an app, keep a channel feed on events rather than polling history. Streaming replies share about a hundred appends a minute per workspace (see "Streaming agent replies"), and the Events API stops a workspace's events for the rest of the minute past 30000 an hour, which arrives as `SlackAppRateLimited`.

### What it does not reach

The connector covers what an app does in conversations. It does not wrap Slack's Enterprise Grid `admin.*` methods, canvases, Lists, Work Objects, search, user groups, bookmarks, reminders, calls, Do Not Disturb, custom workflow steps, the app manifest API, Sign in with Slack, the legacy RTM API, `dialog.open` or stars. An event of theirs still arrives, as a `SlackUnknownEvent` with Slack's JSON in `Raw`.
