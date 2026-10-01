# Ikon Connectors Developer Guide
<!-- checked-against: 871ba7e4267cbddd -->
This guide covers the connector libraries — `Ikon.Connectors` (Slack, GitHub), `Ikon.Connectors.Google` (Drive, Gmail), and `Ikon.Connectors.Browser` (agentic and scripted web automation) — for app developers wiring external services into an Ikon app.

## Overview

Each connector is a **raw** client for one external service: a thin, typed wrapper over the service's API with no agent coupling. The connectors' agent skills are internal, so an app cannot construct them or register them on a persona of its own; the one public route to one is `BrowserOperatorPersona.Create()`, which builds a persona around the browser skill. This guide focuses on the raw connectors.

All connectors report failures with `ConnectorException` (from `Ikon.Connectors`). It carries `Provider` (`"slack"`, `"github"`, `"gmail"`, `"drive"`, `"browser"`) and, when the failure was an HTTP error, `StatusCode`. Branch on `StatusCode` to distinguish a permanent `401`/`403` — the credential is bad or revoked, so surface a "reconnect required" state instead of retrying — from a transient failure worth retrying (GitHub `403` needs one more check — see below):

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

The Slack and GitHub connectors honor rate limits on their JSON API calls: a `429` is retried up to three times, waiting the server's `Retry-After` (bounded at two minutes), before it surfaces as a `ConnectorException`. Three methods bypass that retry and fail immediately on a `429` — `GitHub.GetPullRequestDiffAsync`, `GitHub.MergePullRequestAsync`, and `Slack.DownloadFileAsync` — so wrap those yourself when rate limiting matters.

GitHub is the exception to the `403` rule above: it answers a primary or secondary rate limit with `403` as well, and the connector does not retry those. The response body is in `Message` (`"API rate limit exceeded"`, `"secondary rate limit"`), so for `Provider == "github"` treat a `403` as a dead credential only when the message does not name a rate limit; otherwise retry after the limit resets.

One more exception type exists, and it is not a failure: the paged reads (`Slack.HistorySinceAsync`, `Slack.ListConversationsAsync`, `GitHub.ListIssuesSinceAsync`) each take a `maxPages` bound, and a call that reaches it with the service still holding more throws `ConnectorPageCapException<T>` — carrying the `Items` it did read and a `ResumeFrom` point — rather than handing back a shortened list as if it were complete. Each method's section below says what `ResumeFrom` means for it, because Slack and GitHub page in opposite directions. The platform's own backend listings (`IkonBackend` — spaces, databases, billing rows, release notes, everything an `ikon` verb or Studio lists) follow the same rule with `BackendPageCapException<T>`: a `maxResults` window that fills while the backend reports more throws with the `Items` read, the `TotalCount`, and the `NextCursor`, never a shortened list as the total.

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

The returned `SlackMessage` is **synthesized locally** from the request, not fetched back from Slack: `Ts` and `Channel` come from the response, but `User` is empty and `ThreadTs` merely echoes the argument. Use it for the `Ts` of the message you just posted — do not read server-populated fields (author, files, subtype) off it.

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

Paging is bounded by `maxPages` (default 50 pages of `pageLimit` 200), and the bound is never silent: when it trips with Slack still reporting a `next_cursor`, the call throws `ConnectorPageCapException<SlackMessage>` instead of returning. The exception carries the messages it did read as `Items` (oldest-first, same as a normal result) and the oldest of their `ts` values as `ResumeFrom`. Because Slack pages **backward in time**, the span in hand is the most recent one and the unread messages sit **below** `ResumeFrom` — so ingest `Items` if they are useful, but do not move your cursor past the `oldestTs` you called with; the only way to close the gap is to call again with a larger `maxPages`. A caller that stays under the bound sees no exception at all.

### Conversations and files

`ListConversationsAsync` returns the public and private channels the token can see, paging up to `maxPages`; a workspace with more channels than the cap admits gets a `ConnectorPageCapException<SlackConversation>` carrying the channels read so far as `Items` and the next page cursor as `ResumeFrom`, never a shortened list presented as the total — raise `maxPages` for such a workspace. `GetConversationAsync(channelId)` fetches one. Both hand back `SlackConversation` records — `Id`, `Name`, `IsMember`, and the three shape flags `IsPrivate`, `IsIm` and `IsMpim` that separate a channel from a DM or a group DM. A file shared into a message arrives as a `SlackFile` (`Id`, `MimeType`, and a `DownloadUrl` that is null when the token cannot fetch it). `DownloadFileAsync(url)` downloads a shared file's `url_private_download` with the bot token. It fetches Slack-owned hosts only (`slack.com` and subdomains); any other URL — e.g. one parsed out of untrusted message text — throws `ArgumentException` without a request, so the token can never leak to another server.

### Socket Mode

`OpenSocketUrlAsync` requests a Socket Mode URL (`apps.connections.open`) and returns it — the library ships no Socket Mode client, so the WebSocket handshake, envelope acknowledgements, hello/disconnect handling, and reconnection on the URL's short expiry are yours to implement. It requires an **app-level token** (`xapp-...`) passed as its argument — an empty token or one without the `xapp-` prefix, such as the bot token, throws `ArgumentException` before any request. These are two different credentials from the same Slack app:

<!-- ikon-example: connectors-slack-socket -->
```csharp
var wsUrl = await slack.OpenSocketUrlAsync(appToken);   // xapp-..., not the xoxb- bot token
```

`Slack.ParseMessage(JsonElement, channel)` maps a raw message object — from a history page or a Socket Mode event payload — to a `SlackMessage`, returning `null` for non-message objects (no `ts`).

## GitHub

Construct `GitHub` with a token. The constructor **throws `ArgumentException` on an empty or whitespace token** — an empty token would otherwise degrade silently to unauthenticated requests, where private repositories answer 404 instead of 401. Every `repo` parameter is the `"owner/name"` form:

<!-- ikon-example: connectors-github -->
```csharp
var gitHub = new GitHub(token);
var issue = await gitHub.GetIssueAsync("ikon-ai/examples", 42);
var commentUrl = await gitHub.CommentAsync("ikon-ai/examples", 42, "Reproduced on main.");
```

### Listing by update time

`ListIssuesSinceAsync(repo, since)` returns every issue **and pull request** updated after `since` (an ISO-8601 timestamp, e.g. `"2026-01-01T00:00:00Z"`), ordered by update time ascending and paged to completion. Paging is bounded by `maxPages` (default 50 pages of 100), and reaching the bound with a full last page throws `ConnectorPageCapException<GitHubIssue>` rather than returning a shortened list: `Items` holds the pages read (ascending and gap-free, so they are safe to process) and `ResumeFrom` is the newest `UpdatedAt` among them — pass it back as the next `since` to continue. The GitHub issues API includes pull requests; `GitHubIssue.IsPullRequest` tells them apart.

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
var result = await gitHub.MergePullRequestAsync("ikon-ai/examples", 42, commitTitle: "Add retry policy");

if (!result.Merged)
{
    Log.Instance.Warning($"PR #42 not merged: {result.Message}");
}
```

`GetPullRequestDiffAsync` returns the PR's unified diff as text.

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

`DownloadAsync` buffers the whole file in memory and works only for files with binary content: Google-native Docs, Sheets and Slides are rejected with HTTP 403 as a `ConnectorException`, and the connector has no export.

`ListAsync(folderId, limit)` fetches a **single page**: `limit` is a per-page maximum, not a guarantee that everything under the folder is returned, and the results **include trashed files**. Use it only for a bounded "recent files" peek. For a complete or filtered listing use `ListAllAsync`, which pages through the entire result set and accepts an extra Drive query clause:

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

- `EmailSummary.ReceivedAt` is `DateTimeOffset.MinValue` when Gmail supplies no internal date. Check for it before sorting or displaying by date.
- `GetBodyAsync` returns the `text/plain` part when present, else the **raw HTML** of the `text/html` part, else an empty string — the fallback is not converted to text.

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
    new WebAgentOptions(MaxSteps: 25, Headless: true));

if (run.Outcome == WebOutcome.Succeeded)
{
    var balance = run.Outputs["balance"];
}
```

`WebRun` carries the `Outcome` (`Succeeded`, `Failed`, or `BudgetExhausted` when `MaxSteps` ran out), a `Summary`, the full action trace in `Steps`, any `Extract`ed `Outputs`, and `Looks` — the count of on-demand vision inspections, which consume agent budget without appearing in the trace.

### Sites you do not control

A site that is not your own app decides what the browser loads next, and the agent can press anything on it. Three options make that safe to hand to a person:

- `PublicInternetOnly: true` confines the browser to public addresses. Every request is made by the platform's guarded HTTP client, so no page can reach the network your app runs in, and certificates are validated.
- `ReviewWrite` is asked before every action that could change something on the site — a click on a submit, send, pay or delete control, Enter outside a search field, and anything the classifier does not recognise. The action runs only on `WebApproval.Allow`; `WebApproval.Deny(reason)` is reported to the agent, which does not try it again. The `WebActionReview` carries a one-line `Description` and a JPEG `Screenshot` of the page. Nobody answering must be a refusal, so bound the wait.
- `OnProgress` hands you a `WebProgress` — step number, URL, what just happened, and a JPEG `Screenshot` — after every observation, for a live view.

<!-- ikon-example: connectors-web-agent-review -->
```csharp
var run = await WebAgent.OperateAsync(
    thread,
    "https://supplier.example.com/orders",
    "Reorder last month's printer paper",
    new WebAgentOptions(
        MaxSteps: 40,
        PublicInternetOnly: true,
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
});

if (replay.Ok)
{
    var balance = replay.Outputs["balance"];
}
```

Distillation keeps only the steps that succeeded and parameterizes each filled field into a named input slot (`WebFlow.Inputs`); slot names are slugs of the field's accessible name (`"Password"` becomes `password`). A `Fill` marked `Secret` is stored **redacted** everywhere the trace is persisted — the step trace, the distilled flow JSON, logs — so the flow never carries the credential. That means every slot **must** be supplied in `inputs` at replay — a missing one, secret or not, fails upfront with `ConnectorException` rather than typing a recorded or placeholder value into the field, and a key that names no slot is rejected the same way, so a misspelt input can never be silently ignored. Replay failures are ordinary results, not exceptions — check `WebReplay.Ok`.

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
