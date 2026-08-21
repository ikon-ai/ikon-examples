# Ikon Connectors Developer Guide

This guide covers the connector libraries — `Ikon.Connectors` (Slack, GitHub), `Ikon.Connectors.Google` (Drive, Gmail), and `Ikon.Connectors.Browser` (agentic and scripted web automation) — for app developers wiring external services into an Ikon app.

## Overview

Each connector is a **raw** client for one external service: a thin, typed wrapper over the service's API with no agent coupling. For agent tool use, each connector has a matching `Skill` (`SlackSkill`, `GitHubSkill`, `DriveSkill`, `GmailSkill`, `BrowserSkill`) that wraps it — construct the connector, pass it to the skill, and register the skill on a persona. This guide focuses on the raw connectors; each skill exposes a curated SUBSET of its connector's operations as tools (for example `SlackSkill` offers post + history only, `GmailSkill` send + list without message bodies) — check the skill's `Tools()` before relying on a capability, and note that registering `GitHubSkill` grants the agent authority to create issues, comment, and merge pull requests with no confirmation gate.

All connectors report failures with `ConnectorException` (from `Ikon.Connectors`). It carries `Provider` (`"slack"`, `"github"`, `"gmail"`, `"drive"`, `"browser"`) and, when the failure was an HTTP error, `StatusCode`. Branch on `StatusCode` to distinguish a permanent `401`/`403` — the credential is bad or revoked, so surface a "reconnect required" state instead of retrying — from a transient failure worth retrying:

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

The Slack and GitHub connectors honor rate limits on their JSON API calls: a `429` is retried up to three times, waiting the server's `Retry-After` (bounded at two minutes), before it surfaces as a `ConnectorException`. Three methods bypass that retry and fail immediately on a `429` — `GitHub.GetPullRequestDiffAsync`, `GitHub.MergePullRequestAsync`, and `Slack.DownloadFileAsync` — so wrap those yourself when rate limiting matters.

## Slack

Construct `Slack` with a **bot token** (`xoxb-...`). An optional `HttpClient` can be injected; otherwise a shared one is used.

```csharp
var slack = new Slack(botToken);
```

### Posting

```csharp
var posted = await slack.PostAsync("C0123456789", "Deploy finished", threadTs: rootTs);
```

The returned `SlackMessage` is **synthesized locally** from the request, not fetched back from Slack: `Ts` and `Channel` come from the response, but `User` is empty and `ThreadTs` merely echoes the argument. Use it for the `Ts` of the message you just posted — do not read server-populated fields (author, files, subtype) off it.

### Reading history

Slack timestamps (`Ts`, `ThreadTs`, `oldestTs`) are **raw Slack `ts` strings** (e.g. `"1727694230.000200"`), not `DateTime`s. Treat them as opaque ordered cursors and pass them back verbatim.

`HistoryAsync(channel, limit)` fetches one page of recent messages. `HistorySinceAsync(channel, oldestTs)` fetches every message with `ts > oldestTs`, following pagination to completion and returning the result **oldest-first**, so a caller that advances a cursor per message never leaves a gap:

```csharp
var messages = await slack.HistorySinceAsync(channelId, oldestTs: lastSeenTs);

foreach (var message in messages)
{
    await ProcessAsync(message);
    lastSeenTs = message.Ts;   // safe: oldest-first means no gap on interruption
}
```

Paging is bounded by `maxPages` (default 50 pages of `pageLimit` 200). Because Slack pages **backward in time**, when the bound trips it is the **oldest** messages that are missing — the returned list covers the most recent span only. If a backfill can exceed the bound, raise `maxPages` or advance the cursor and call again.

### Conversations and files

`ListConversationsAsync` returns every public and private channel the token can see, paged to completion; `GetConversationAsync(channelId)` fetches one. `DownloadFileAsync(url)` downloads a shared file's `url_private_download`; the bot token is attached only for Slack-owned hosts, so a URL parsed out of untrusted message text can never leak the token to another server.

### Socket Mode

`OpenSocketUrlAsync` requests a Socket Mode URL (`apps.connections.open`) and returns it — the library ships no Socket Mode client, so the WebSocket handshake, envelope acknowledgements, hello/disconnect handling, and reconnection on the URL's short expiry are yours to implement. It requires an **app-level token** (`xapp-...`) passed as its argument — the bot token compiles fine here but fails at runtime with `invalid_auth`. These are two different credentials from the same Slack app:

```csharp
var wsUrl = await slack.OpenSocketUrlAsync(appToken);   // xapp-..., not the xoxb- bot token
```

`Slack.ParseMessage(JsonElement, channel)` maps a raw message object — from a history page or a Socket Mode event payload — to a `SlackMessage`, returning `null` for non-message objects (no `ts`).

## GitHub

Construct `GitHub` with a token. The constructor **throws `ArgumentException` on an empty or whitespace token** — an empty token would otherwise degrade silently to unauthenticated requests, where private repositories answer 404 instead of 401. Every `repo` parameter is the `"owner/name"` form:

```csharp
var gitHub = new GitHub(token);
var issue = await gitHub.GetIssueAsync("ikon-ai/examples", 42);
var commentUrl = await gitHub.CommentAsync("ikon-ai/examples", 42, "Reproduced on main.");
```

### Listing by update time

`ListIssuesSinceAsync(repo, since)` returns every issue **and pull request** updated after `since` (an ISO-8601 timestamp, e.g. `"2026-01-01T00:00:00Z"`), ordered by update time ascending and paged to completion (bounded by `maxPages`). The GitHub issues API includes pull requests; `GitHubIssue.IsPullRequest` tells them apart.

`GitHubIssue.UpdatedAt` is the raw ISO-8601 string exactly as GitHub returned it. It is an **opaque cursor**: feed it back as the next `since` without parsing or reformatting it — a round-trip through `DateTime` can change the text and break resume-from-cursor paging.

```csharp
var updated = await gitHub.ListIssuesSinceAsync("ikon-ai/examples", since: cursor);

foreach (var item in updated.Where(i => !i.IsPullRequest))
{
    await ProcessIssueAsync(item);
}

if (updated.Count > 0)
{
    cursor = updated[^1].UpdatedAt;   // pass back verbatim next time
}
```

### Merging pull requests

`MergePullRequestAsync` treats a refused merge (HTTP 405/409 — not mergeable, head changed) as an **answer, not an error**: it returns `GitHubMergeResult` with `Merged: false` and GitHub's reason in `Message` instead of throwing. Always branch on `.Merged`; other HTTP failures still throw `ConnectorException`.

```csharp
var result = await gitHub.MergePullRequestAsync("ikon-ai/examples", 42, commitTitle: "Add retry policy");

if (!result.Merged)
{
    logger.LogWarning("PR #42 not merged: {Reason}", result.Message);
}
```

`GetPullRequestDiffAsync` returns the PR's unified diff as text.

## Google: Drive and Gmail

Both connectors authenticate with `GoogleCredentials(ClientId, ClientSecret, RefreshToken)` — OAuth2 refresh-token credentials; the short-lived access token is obtained and refreshed automatically by the Google client library.

`Drive` and `Gmail` are **`IDisposable` and own an `HttpClient`**: construct one instance per credential and reuse it for the credential's lifetime, rather than constructing per call.

```csharp
var credentials = new GoogleCredentials(clientId, clientSecret, refreshToken);
using var drive = new Drive(credentials);
using var gmail = new Gmail(credentials);
```

Google failures surface two ways: a failed upload, download, or Gmail metadata fetch throws `ConnectorException` (with provider `"drive"`/`"gmail"`), while lower-level API errors surface as the Google client library's own exceptions — so catch both. Use `GoogleAuth.IsAuthFailure(ex)` on the latter to decide whether to stop retrying: it is `true` only for permanent auth failures (revoked or expired refresh token, bad client), never for transient or network errors.

### Drive

```csharp
await using var content = File.OpenRead("./report.pdf");
var uploaded = await drive.UploadAsync("report.pdf", "application/pdf", content, folderId);

await using var download = await drive.DownloadAsync(uploaded.Id);
```

`ListAsync(folderId, limit)` fetches a **single page**: `limit` is a per-page maximum, not a guarantee that everything under the folder is returned, and the results **include trashed files**. Use it only for a bounded "recent files" peek. For a complete or filtered listing use `ListAllAsync`, which pages through the entire result set and accepts an extra Drive query clause:

```csharp
await foreach (var file in drive.ListAllAsync(folderId, extraQuery: "trashed = false"))
{
    Console.WriteLine($"{file.Name} ({file.MimeType}, modified {file.ModifiedTime:O})");
}
```

The `extraQuery` clause is Drive query syntax — `"trashed = false"` excludes trashed files, `"modifiedTime > '2024-01-01T00:00:00'"` bounds a historical backfill by time.

### Gmail

```csharp
var unread = await gmail.ListAsync("is:unread", limit: 10);

foreach (var email in unread)
{
    var body = await gmail.GetBodyAsync(email.Id);
    Console.WriteLine($"{email.From}: {email.Subject}");
}

var sentId = await gmail.SendAsync("someone@example.com", "Weekly summary", bodyText, cc: "team@example.com");
```

`ListAsync` fetches up to `limit` matching messages; `ListAllAsync` streams the entire result set — bound a backfill with query date operators such as `"after:2024/01/01"`. Both fetch message metadata in batches, and **if any single message fetch in a batch fails, the whole call throws `ConnectorException`** — a partially populated list is never returned.

Two field contracts to respect:

- `EmailSummary.ReceivedAt` is `DateTimeOffset.MinValue` when Gmail supplies no internal date. Check for it before sorting or displaying by date.
- `GetBodyAsync` returns the `text/plain` part when present, else the **raw HTML** of the `text/html` part, else an empty string — the fallback is not converted to text.

## Browser

`Ikon.Connectors.Browser` operates a real (Playwright-driven) browser. There are three entry points; pick by who is driving:

| Entry point | Who drives | Use when |
|---|---|---|
| `WebAgent.OperateAsync` | An LLM agent subthread | You have an objective in natural language and want the agent to figure out the clicks. Needs an `AgentThread` (from `Ikon.Agent`) and a registered browser-operator persona. |
| `BrowserSession` | Your code | You know the exact actions — scripted navigation, screenshots, page evaluation. No LLM involved. |
| `BrowserSkill` | An agent, as tools | You are composing your own persona and want browser actions available as tools alongside other skills. |

### Agentic operation

Register the persona `BrowserOperatorPersona.Create()` returns on your app's orchestrator (its default name, `"browser-operator"`, matches `OperateAsync`'s default `personaName`). Then hand the agent an objective:

```csharp
var run = await WebAgent.OperateAsync(
    thread,                                    // an AgentThread from Ikon.Agent
    "https://portal.example.com",
    "Log in with the provided credentials and extract the current account balance",
    new WebAgentOptions(MaxSteps: 25, Headless: true));

if (run.Outcome == WebOutcome.Succeeded)
{
    var balance = run.Outputs["balance"];
}
```

`WebRun` carries the `Outcome` (`Succeeded`, `Failed`, or `BudgetExhausted` when `MaxSteps` ran out), a `Summary`, the full action trace in `Steps`, any `Extract`ed `Outputs`, and `Looks` — the count of on-demand vision inspections, which consume agent budget without appearing in the trace.

### Manual driving

`BrowserSession` owns the browser lifecycle: start once, dispose to release the process. `WebTarget` resolution tries the perception mark first, then accessibility role + name, then a CSS/XPath selector — populate whichever you know.

```csharp
await using var session = new BrowserSession();
await session.StartAsync(headless: true);
await session.NavigateAsync("https://example.com/login");

var marks = await session.MarkElementsAsync();
var result = await session.ExecuteAsync(
    new WebAction.Fill(new WebTarget(Role: "textbox", Name: "Email"), "user@example.com"));

if (!result.Ok)
{
    Console.WriteLine(result.Failure);       // caller-actionable diagnosis
    Console.WriteLine(string.Join("\n", session.ConsoleTail));   // the page's own account
}
```

The action vocabulary is a tagged union: `Navigate`, `Click`, `Fill`, `Press`, `Scroll`, and `Extract` (which records the target's inner text under an output name). `ScreenshotAsync` returns a PNG; prefer `ScreenshotJpegAsync` when the image goes into an LLM context. `ConsoleTail` holds the last ~40 console messages, page errors, and failed requests — the first place to look when a page that "should" render stays blank.

### Distill and replay

A successful `WebRun` can be **distilled** into a `WebFlow` — a deterministic, replayable integration — and replayed **without an LLM**:

```csharp
var flow = WebAgent.Distill(run, name: "portal-balance");
// ... persist flow (it serializes losslessly), later:
var replay = await WebAgent.ReplayAsync(flow, new Dictionary<string, string>
{
    ["email"] = accountEmail,
    ["password"] = accountPassword,
});

if (replay.Ok)
{
    var balance = replay.Outputs["balance"];
}
```

Distillation keeps only the steps that succeeded and parameterizes each filled field into a named input slot (`WebFlow.Inputs`); slot names are slugs of the field's accessible name (`"Password"` becomes `password`). A `Fill` marked `Secret` is stored **redacted** everywhere the trace is persisted — the step trace, the distilled flow JSON, logs — so the flow never carries the credential. That means every secret slot **must** be supplied in `inputs` at replay: a missing one fails upfront with `ConnectorException` rather than typing the redaction placeholder into the field. Replay failures are ordinary results, not exceptions — check `WebReplay.Ok`.

`WebFlowDistiller.Distill` and `WebFlowPlayer.ReplayAsync` are the underlying pieces if you need to replay on a `BrowserSession` you manage yourself.
