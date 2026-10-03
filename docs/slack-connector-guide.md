# Slack Connector Guide
<!-- checked-against: 5943264067372059 -->
This guide covers `Ikon.Connectors.Slack` — posting to, reading and installing into a Slack workspace — for app developers wiring Slack into an Ikon app.

## Slack

The types are in namespace `Ikon.Connectors`, not the package's name — `using Ikon.Connectors.Slack;` names the `Slack` class and fails with CS0138. Construct `Slack` with a **bot token** (`xoxb-...`); an empty or whitespace token throws `ArgumentException` at construction. An optional `HttpClient` can be injected; otherwise a shared one is used.

<!-- ikon-example: connectors-slack-client -->
```csharp
var slack = new Slack(botToken);
```

Every failure is a `ConnectorException` (from `Ikon.Connectors`) with `Provider` `"slack"`. Slack answers an auth error with HTTP 200 and `ok:false`, and the connector reports it as `StatusCode` `401`, so `IsReconnectRequired` means the token is invalid or revoked: ask the person to reconnect rather than retry. Any other Slack API error has a null `StatusCode` and Slack's code (`channel_not_found`, `not_in_channel`, `is_archived`) in `ErrorCode`, and fails the same way again. A `429` is retried three times on Slack's `Retry-After`, bounded at two minutes, before it surfaces; `IsTransient` marks one that still did.

### Posting

<!-- ikon-example: connectors-slack-post -->
```csharp
var posted = await slack.PostAsync("C0123456789", "Deploy finished", threadTs: rootTs);
```

The returned `SlackPostResult` is what Slack echoed back: the posted message's `Ts` and the `Channel` id a name like `#general` resolved to. Pass that `Ts` as `threadTs` to reply in the thread; the post's author, subtype and files are not returned — read the message back from history for those.

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

### Conversations and files

`ListConversationsAsync` returns the public and private channels the token can see, paging up to `maxPages`; a workspace with more channels than the cap admits gets a `ConnectorPageCapException<SlackConversation>` carrying the channels read so far as `Items` and a null `ResumeFrom`, never a shortened list presented as the total — raise `maxPages` for such a workspace. Archived channels are included; filter on `SlackConversation.IsArchived` before posting, which fails on one with `is_archived`. `GetConversationAsync(channelId)` fetches one. Both hand back `SlackConversation` records — `Id`, `Name`, `IsMember`, and the three shape flags `IsPrivate`, `IsIm` and `IsMpim` that separate a channel from a DM or a group DM. A file shared into a message arrives as a `SlackFile` (`Id`, `MimeType`, and a `DownloadUrl` that is null when the token cannot fetch it). `DownloadFileAsync(url)` downloads a shared file's `url_private_download` with the bot token; when Slack answers with its HTML sign-in page instead (the token lacks `files:read` or the file is not shared with the bot), it throws `ConnectorException` rather than returning that page as the file. It fetches Slack-owned hosts only (`slack.com` and subdomains); any other URL — e.g. one parsed out of untrusted message text — throws `ArgumentException` without a request, so the token can never leak to another server.

### Socket Mode

`OpenSocketUrlAsync` requests a Socket Mode URL (`apps.connections.open`) and returns it — the library ships no Socket Mode client, so the WebSocket handshake, envelope acknowledgements, hello/disconnect handling, and reconnection on the URL's short expiry are yours to implement. It requires an **app-level token** (`xapp-...`) passed as its argument — an empty token or one without the `xapp-` prefix, such as the bot token, throws `ArgumentException` before any request. These are two different credentials from the same Slack app:

<!-- ikon-example: connectors-slack-socket -->
```csharp
var wsUrl = await slack.OpenSocketUrlAsync(appToken);   // xapp-..., not the xoxb- bot token
```

`Slack.ParseMessage(JsonElement, channel)` maps a raw message object — from a history page or a Socket Mode event payload — to a `SlackMessage`, returning `null` for non-message objects (no `ts`).

### Threads and people

`RepliesAsync(channel, threadTs)` reads a whole thread — the parent first, then its replies oldest-first — which is how a feed built on `HistorySinceAsync` picks up the replies it leaves out: for each message whose `ThreadTs` is its own `Ts`, read its thread. It pages up to `maxPages` and throws `ConnectorPageCapException<SlackMessage>` with the messages read past it. `GetUserAsync(userId)` turns the user id a message names into a `SlackUser` — `Name`, `RealName`, `DisplayName`, `IsBot`, `IsDeleted`, `TimeZone`, and `Email` when the token has `users:read.email`.

### Installing the app and checking requests

`SlackAuth` runs the OAuth v2 install: send a workspace member to `AuthorizeUrl` with the bot scopes the app needs, receive the `code` at your redirect endpoint, and redeem it with `ExchangeCodeAsync`. The `SlackInstallation` it returns carries the `BotToken` a `Slack` client takes, the workspace's `TeamId` and `TeamName`, and the `Scopes` actually granted. A reused or expired code, or a wrong client secret, is a `401` with Slack's code in `ErrorCode`:

<!-- ikon-example: connectors-slack-install -->
```csharp
var installUrl = SlackAuth.AuthorizeUrl(clientId, ["channels:history", "chat:write"], redirectUri, state);

// ... a workspace member installs the app; the redirect back carries `code`:
var installation = await SlackAuth.ExchangeCodeAsync(clientId, clientSecret, code, redirectUri);
await SaveBotTokenAsync(installation.TeamId, installation.BotToken);
```

An endpoint Slack calls — the Events API, slash commands, interactivity — is public, so anyone can call it. `SlackAuth.VerifyRequest` checks the `X-Slack-Signature` header against the raw body signed with the app's signing secret, and refuses an `X-Slack-Request-Timestamp` more than five minutes off, so a captured request cannot be replayed. Pass the body exactly as it arrived:

<!-- ikon-example: connectors-slack-verify -->
```csharp
if (!SlackAuth.VerifyRequest(signingSecret, timestampHeader, signatureHeader, body))
{
    return false;   // not Slack's, or replayed: answer 401 and do nothing
}
```
