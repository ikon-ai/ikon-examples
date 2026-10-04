# GitHub Connector Guide
<!-- checked-against: b0e1c26b39abc08c -->
This guide covers `Ikon.Connectors.GitHub` — issues, pull requests, repository files, sign-in and webhooks — for app developers wiring GitHub into an Ikon app.

## GitHub

The types are in namespace `Ikon.Connectors`, not the package's name — `using Ikon.Connectors.GitHub;` names the `GitHub` class and fails with CS0138. Construct `GitHub` with a token. The constructor **throws `ArgumentException` on an empty or whitespace token** — an empty token would otherwise degrade silently to unauthenticated requests, where private repositories answer 404 instead of 401. Every `repo` parameter is the `"owner/name"` form, checked before any request: anything other than two segments of letters, digits, `-`, `_` or `.` joined by one `/` (a full URL, `owner/name/extra`, a `.` or `..` segment) throws `ArgumentException`:

<!-- ikon-example: connectors-github -->
```csharp
var gitHub = new GitHub(token);
var issue = await gitHub.GetIssueAsync("ikon-ai/examples", 42);
var commentUrl = await gitHub.CommentAsync("ikon-ai/examples", 42, "Reproduced on main.");
```

Every failure is a `ConnectorException` (from `Ikon.Connectors`) with `Provider` `"github"`. `IsReconnectRequired` (`401`/`403`) means the token is invalid, revoked or lacks the access: ask the person to reconnect rather than retry. GitHub answers a primary or secondary rate limit with `403` as well; the connector reports such a response (one with `X-RateLimit-Remaining: 0` or a `Retry-After`) as `429` and retries it three times, waiting the `Retry-After`, or for the rate-limit window to reset when GitHub names none — a window that resets more than two minutes out surfaces the `429` at once instead — so a GitHub `403` that reaches you is an access failure, not a rate limit. `IsTransient` (`408`, `429`, `5xx`) marks a call that may succeed later.

### Listing by update time

`ListIssuesSinceAsync(repo, since)` returns every issue **and pull request** updated after `since` (an ISO-8601 timestamp, e.g. `"2026-01-01T00:00:00Z"`), ordered by update time ascending and paged to completion. Paging is bounded by `maxPages` (default 50 pages of 100; `maxPages <= 0` throws `ArgumentOutOfRangeException`), and reaching the bound with a full last page throws `ConnectorPageCapException<GitHubIssue>` rather than returning a shortened list: `Items` holds the pages read (ascending and gap-free, so they are safe to process) and `ResumeFrom` is the newest `UpdatedAt` among them — pass it back as the next `since` to continue. The GitHub issues API includes pull requests; `GitHubIssue.IsPullRequest` tells them apart.

`GitHubIssue.UpdatedAt` is the raw ISO-8601 string exactly as GitHub returned it. It is an **opaque cursor**: feed it back as the next `since` without parsing or reformatting it — a round-trip through `DateTime` can change the text and break resume-from-cursor paging.

`since` is boundary-**inclusive** (it returns items updated at-or-after it), so resuming with the last item's `UpdatedAt` re-returns every item that shares that exact second. Dedupe on `GitHubIssue.Number` across calls — do not assume the resumed page is all new.

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

`GetPullRequestDiffAsync` returns the PR's unified diff as text. Both merge calls take a `GitHubMergeMethod` — `Merge` (the default), `Squash` or `Rebase`, where the repository allows it.

### Repositories, files and pull requests

The connector also works a repository the way an editor does, without a clone: `GetUserAsync` (a `GitHubUser`), `ListRepositoriesAsync` and `GetRepositoryAsync` (each `GitHubRepository` says whether the token `CanPush`), `ListBranchesAsync` (`GitHubBranch` records), `GetBranchHeadAsync` (null for a missing branch) and `CreateBranchAsync`, `ListTreeAsync` for every file path at a reference (a `GitHubTree`, whose `IsTruncated` says GitHub stopped short on a very large tree), `GetFileAsync` and `PutFileAsync`, and `ListCommitsAsync` (`GitHubCommit` records, newest first). Pull requests have `ListPullRequestsAsync` (filtered by a `GitHubPullRequestState`), `GetPullRequestAsync`, `CreatePullRequestAsync` and `ClosePullRequestAsync`, each returning a `GitHubPullRequest`, and `ReviewPullRequestAsync` with a `GitHubReviewEvent` of `Approve`, `RequestChanges` or `Comment` (the last two need a body).

`GetFileAsync` returns a `GitHubFile` with the `Content` bytes, or null when no file is at the path (nothing, or a directory), and reads a file over one megabyte through its blob; `PutFileAsync` returns the new blob and commit SHAs as a `GitHubWriteResult`. `PutFileAsync` writes one file as one commit; replacing a file needs the `Sha` it was read at as `expectedSha`, and a file that changed since throws `ConnectorException` with `409` — as does another commit reaching the branch first, which only a fresh read of the file tells apart. Listings page by 100 up to `maxPages` and throw `ConnectorPageCapException<T>` past it. A failure's message carries GitHub's own reason (`Validation Failed: A pull request already exists for …`) rather than its JSON:

<!-- ikon-example: connectors-github-files -->
```csharp
var readme = await gitHub.GetFileAsync("ikon-ai/examples", "README.md", reference: "main");
var text = readme is null ? "" : System.Text.Encoding.UTF8.GetString(readme.Content);

var head = await gitHub.GetBranchHeadAsync("ikon-ai/examples", "main");
await gitHub.CreateBranchAsync("ikon-ai/examples", "docs/intro", head!);
await gitHub.PutFileAsync("ikon-ai/examples", "README.md", System.Text.Encoding.UTF8.GetBytes(text + "\nMore."),
    "Extend the intro", "docs/intro", expectedSha: readme?.Sha);

var pullRequest = await gitHub.CreatePullRequestAsync("ikon-ai/examples", "Extend the intro", head: "docs/intro", baseBranch: "main");
```

### Signing in and webhooks

`GitHubAuth` signs a person in with GitHub's device flow, which needs no redirect address: they type a short code at github.com, so it works the same from a laptop and from a deployed app. Device flow must be enabled on the OAuth app or GitHub App. `StartDeviceFlowAsync` returns a `GitHubDeviceCode` whose `UserCode` and `VerificationUri` you show the person, and `WaitForDeviceTokenAsync` polls at the pace GitHub asks for until it can return their `GitHubUserToken`; a code that expires or a person who declines is a `401` with GitHub's code (`expired_token`, `access_denied`) in `ErrorCode`. A GitHub App's token expires and comes with a `RefreshToken` that `RefreshAsync` exchanges for the next one:

<!-- ikon-example: connectors-github-device -->
```csharp
var code = await GitHubAuth.StartDeviceFlowAsync(clientId, scope: "repo");
await ShowAsync($"Open {code.VerificationUri} and enter {code.UserCode}");

var token = await GitHubAuth.WaitForDeviceTokenAsync(clientId, code);   // polls until they have
var gitHub = new GitHub(token.AccessToken);
```

A webhook endpoint is public too. `GitHubAuth.VerifyWebhook` checks `X-Hub-Signature-256` against the raw body signed with the webhook's secret:

<!-- ikon-example: connectors-github-webhook -->
```csharp
if (!GitHubAuth.VerifyWebhook(webhookSecret, signatureHeader, body))   // X-Hub-Signature-256
{
    return false;
}
```
