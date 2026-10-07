<!-- checked-against: 5e5e04f17155805081ddc8fe -->

# GitHub Connector Guide

This guide covers `Ikon.Connectors.GitHub` — repositories, their files and history, issues, pull requests and reviews, Actions, checks, releases, deployments, discussions, projects, search, organizations, notifications, signing in, GitHub Apps and webhooks — for app developers wiring GitHub into an Ikon app.

## GitHub

The types are in namespace `Ikon.Connectors`, not the package's name — `using Ikon.Connectors.GitHub;` names the `GitHub` class and fails with CS0138. Each GitHub product has its own client: `GitHub` (repositories, branches, files, commits, people), `GitHubIssues`, `GitHubPullRequests`, `GitHubActions`, `GitHubChecks`, `GitHubReleases`, `GitHubDeployments`, `GitHubDiscussions`, `GitHubProjects`, `GitHubSearch`, `GitHubOrganizations`, `GitHubNotifications`, `GitHubHooks` and `GitHubApp`. Construct any of them but `GitHubApp` with a token; `GitHubApp` takes the app's `GitHubAppCredentials`. Ids GitHub numbers — comments, runs, jobs, checks, releases, hooks, installations — are `long`; issue and pull request numbers are `int`; GraphQL ids are strings. The constructor **throws `ArgumentException` on an empty or whitespace token** — an empty token would otherwise degrade silently to unauthenticated requests, where private repositories answer 404 instead of 401. Every `repo` parameter is the `"owner/name"` form, checked before any request: anything other than two segments of letters, digits, `-`, `_` or `.` joined by one `/` (a full URL, `owner/name/extra`, a `.` or `..` segment) throws `ArgumentException`:

<!-- ikon-example: connectors-github -->
```csharp
var issues = new GitHubIssues(token);
var issue = await issues.GetAsync("ikon-ai/examples", 42);
var comment = await issues.AddCommentAsync("ikon-ai/examples", 42, "Reproduced on main.");
```

Every failure is a `ConnectorException` (from `Ikon.Connectors`) with `Provider` `"github"`, and its message reads `{operation} failed with HTTP {status} {code}: {GitHub's reason}`. `ErrorCode` is GitHub's own code for the first validation error (`already_exists`, `missing_field`, `invalid`, …) when it names one. `IsReconnectRequired` (`401`/`403`) means the token is invalid, revoked or lacks the access: ask the person to reconnect rather than retry. GitHub answers a primary or secondary rate limit with `403` as well; the connector reports such a response — one with `X-RateLimit-Remaining: 0` or a `Retry-After`, or whose message names a secondary rate limit — as `429` and retries it — three times in all for one call, across its redirects, a renewed token and GraphQL's `RATE_LIMITED` — waiting the `Retry-After`, for the rate-limit window to reset, or a minute for a secondary limit that names neither. A wait of more than two minutes surfaces the `429` at once instead. So a `403` that reaches you is an access failure, not a rate limit. A `503` or `504` is retried the same way on a read only, since either may come after GitHub carried out a write. `IsTransient` (`408`, `429`, `5xx`) marks a call that may succeed later. A 401 or 403 from a storage host that was never sent the token, such as an expired download link, has no `StatusCode`.

A renamed or transferred repository answers its old name with a redirect (`301` for a read, `307` for a write) to its new address on the same API. The connector follows it with the token, sending a write again with the same method and body. A redirect to any other host, such as a download's signed link, is followed without the token. An `HttpClient` you pass in may follow redirects itself and drop the token on the way. The connector asks the new address again with the token when that happens, at the cost of a second request.

A listing that reads every page pages by 100 up to `maxPages` (default 10; 50 for `ListFilesAsync` and the delta methods) and past it throws `ConnectorPageCapException<T>` with the items read, never a shortened list. Its `ResumeFrom` is set only where a method takes it back: `GitHubPullRequests.ListAsync` and `GitHubActions.ListRunsAsync` hand out the cursor `ListPageAsync` and `ListRunsPageAsync` take, and the delta methods the cursor to pass back. Elsewhere it is null, and a larger `maxPages` reads the rest.

Every request pins `X-GitHub-Api-Version` — `2026-03-10` on github.com and GHE.com, and `2022-11-28` on an Enterprise Server, which every release accepts — so GitHub's answers keep the shape the records were written against.

### Sharing a token, and renewing it

A `GitHubTokenProvider` is what every client but `GitHubApp` takes in place of a bare token. Give the clients of one credential the same provider, so they share one renewal:

- `GitHubTokenProvider.ForToken(token)` — a personal access token, an OAuth app's token, or an installation token minted elsewhere, used as it is.
- `GitHubTokenProvider.Anonymous()` — no token: public data only, at 60 requests an hour per address.
- `new GitHubTokenProvider(userToken, clientId, clientSecret, onTokensRotated)` — a GitHub App user token. It lasts eight hours and is renewed five minutes before it expires, and once more when GitHub answers `401`; concurrent callers wait on one renewal. **GitHub rotates the refresh token on every renewal and the old pair stops working**, so persist the pair `onTokensRotated` receives. A renewal GitHub refuses is a `401`: the person signs in again.
- `new GitHubTokenProvider(appCredentials, installationId)` — a GitHub App installation: it signs a JWT with the app's private key, mints the hour-long installation token and mints the next one before it runs out. `GitHubInstallationTokenOptions` narrows it to some repositories or permissions.

<!-- ikon-example: connectors-github-provider -->
```csharp
var tokens = new GitHubTokenProvider(signedIn, clientId, onTokensRotated: (rotated, ct) => SaveTokensAsync(rotated));

var gitHub = new GitHub(tokens);
var issues = new GitHubIssues(tokens);
var pullRequests = new GitHubPullRequests(tokens);
```

<!-- ikon-example: connectors-github-app -->
```csharp
var app = new GitHubAppCredentials(appClientId, privateKeyPem);
var tokens = new GitHubTokenProvider(app, installationId);   // mints and renews installation tokens

var issues = new GitHubIssues(tokens);
```

`GitHubAppCredentials` takes the app's client id (or numeric app id) and the private key PEM text as downloaded from the app's settings; a key that is not RSA PEM throws `ArgumentException` from the `GitHubTokenProvider` or `GitHubApp` constructor given it. Printing the record shows `***` for the key.

A provider names its `GitHubServer`: `GitHubServer.Cloud` (the default), `GitHubServer.Enterprise(new Uri("https://github.example.com"))` for an Enterprise Server, or `GitHubServer.DataResidency("acme")` for a GHE.com tenant, which answers nothing anonymously. Every client of the provider talks to that server, and a paging link or stored cursor naming another host is refused before the token is sent to it.

### Reading only what changed

`GitHubIssues.DeltaAsync(repo, since)` returns a `GitHubIssueDelta`: every issue **and pull request** updated since `since`, ordered by update time ascending, each once in its latest state, and the `Since` cursor to pass back next time. With no `since` it reads them all; `fromNow: true` reads nothing and returns a cursor that reports changes from now. Paging is bounded by `maxPages` (default 50 pages of 100; `maxPages <= 0` throws `ArgumentOutOfRangeException`), and reaching the bound with a full last page throws `ConnectorPageCapException<GitHubIssue>` rather than returning a shortened list: `Items` holds the pages read (ascending and gap-free, so they are safe to process) and `ResumeFrom` is the cursor to pass back as `since`. GitHub reports no deletions. `GitHubIssue.IsPullRequest` tells pull requests apart.

`GitHubIssue.UpdatedAt` and the cursor are the raw ISO-8601 strings exactly as GitHub returned them. They are **opaque**: pass them back without parsing or reformatting — a round-trip through `DateTime` can change the text and break the walk.

`since` is boundary-**inclusive**, so a resumed read re-returns every item that shares the cursor's exact second. Dedupe on `GitHubIssue.Number` across calls:

<!-- ikon-example: connectors-github-since -->
```csharp
var delta = await issues.DeltaAsync("ikon-ai/examples", since: cursor);

foreach (var item in delta.Items.Where(i => !i.IsPullRequest))
{
    if (!seenIssueNumbers.Add(item.Number))
    {
        continue;   // already processed last time — since is inclusive
    }

    await ProcessIssueAsync(item);
}

await SaveCursorAsync(delta.Since);   // pass back verbatim next time
```

### Working issues

`GitHubIssues.ListAsync` lists a repository's issues (and pull requests) by a `GitHubIssueQuery` — state, labels, milestone, assignee, creator, mention, issue type, updated since — and `CreateAsync` takes a `NewGitHubIssue` with labels, assignees, a milestone and an issue type; GitHub drops those silently unless the token can push. `UpdateAsync` changes only the arguments given: a `GitHubIssueState` with a `GitHubIssueStateReason` closes or reopens it, labels and assignees replace the whole set, a `milestone` of 0 removes it and an empty `type` clears it. `LockAsync` (with a `GitHubLockReason`) and `UnlockAsync` stop and allow comments from people without push access.

<!-- ikon-example: connectors-github-triage -->
```csharp
var untriaged = await issues.ListAsync("ikon-ai/examples", new GitHubIssueQuery(Labels: ["needs-triage"], Assignee: "none"));

foreach (var issue in untriaged.Where(i => !i.IsPullRequest))
{
    await issues.SetLabelsAsync("ikon-ai/examples", issue.Number, ["bug"]);
    await issues.UpdateAsync("ikon-ai/examples", issue.Number, assignees: ["octocat"], type: "Bug");
}

var epic = await issues.CreateAsync("ikon-ai/examples", new NewGitHubIssue("Release 2.0", Labels: ["epic"]));
await issues.AddSubIssueAsync("ikon-ai/examples", epic.Number, untriaged[0].Id);   // the sub-issue's id, not its number
```

Conversation comments: `ListCommentsAsync`, `GetCommentAsync`, `UpdateCommentAsync`, `DeleteCommentAsync`, and `CommentsDeltaAsync`, which reads every comment in the repository updated since a cursor into a `GitHubCommentDelta` with the same inclusive-cursor rules as `DeltaAsync` (dedupe on `GitHubComment.Id`). Labels (`GitHubLabel`): `ListLabelsAsync`, `CreateLabelAsync`, `UpdateLabelAsync`, `DeleteLabelAsync`, and on one issue `AddLabelsAsync`, `SetLabelsAsync` and `RemoveLabelAsync`. Milestones (`GitHubMilestone`): `ListMilestonesAsync`, `CreateMilestoneAsync`, `UpdateMilestoneAsync`, `DeleteMilestoneAsync`. Assignees: `ListAssigneesAsync`, `AddAssigneesAsync`, `RemoveAssigneesAsync`. `ListIssueTypesAsync` lists an organization's issue types (`GitHubIssueType`).

Sub-issues take the child's `GitHubIssue.Id`, not its number, since it may live in another repository of the same owner: `ListSubIssuesAsync`, `AddSubIssueAsync` (`replaceParent` moves one that has a parent), `RemoveSubIssueAsync`, `ReprioritizeSubIssueAsync` (exactly one of `afterId` or `beforeId`) and `FindParentAsync`. `ListTimelineAsync` returns everything that happened to an issue as `GitHubTimelineEvent` records whose `Raw` keeps each kind's own fields. Reactions use a `GitHubReaction`: `AddReactionAsync` and `AddCommentReactionAsync` return the reaction's id, `ListReactionsAsync` lists `GitHubReactionEntry` records, and `DeleteReactionAsync` and `DeleteCommentReactionAsync` remove one.

### Merging pull requests

`GitHubPullRequests.MergeAsync` merges only if the head is still the SHA you reviewed, and treats a refused merge (HTTP 405/409 — not mergeable, head changed) as an **answer, not an error**: it returns `GitHubMergeResult` with `Merged: false` and GitHub's reason in `Message` instead of throwing. Always branch on `.Merged`; other HTTP failures still throw `ConnectorException`. `MergeUnpinnedAsync` merges whatever the head is when the request lands.

<!-- ikon-example: connectors-github-merge -->
```csharp
var reviewedHead = (await pullRequests.GetAsync("ikon-ai/examples", 42)).HeadSha;   // before reading the diff
var result = await pullRequests.MergeAsync("ikon-ai/examples", 42, reviewedHead, commitTitle: "Add retry policy");

if (!result.Merged)
{
    Log.Instance.Warning($"PR #42 not merged: {result.Message}");
}
```

`GetDiffAsync` returns the PR's unified diff as text. Both merge calls take a `GitHubMergeMethod` — `Merge` (the default), `Squash` or `Rebase`, where the repository allows it.

### Reviewing pull requests

`GitHubPullRequests.ListAsync` also takes a `GitHubPullRequestQuery` (state, head, base, a `GitHubPullRequestSort`), and `ListPageAsync` reads one page of up to 100 as a `GitHubPage<GitHubPullRequest>` whose `NextCursor` it takes back; a listing past its page cap throws with that same cursor as `ResumeFrom`. `ListFilesAsync` (`GitHubChangedFile` records) and `ListCommitsAsync` read what a pull request changes — GitHub stops at 3,000 files and 250 commits without saying so — `IsMergedAsync` asks whether it merged, `UpdateBranchAsync` merges the base into its head, and `RequestReviewersAsync` and `RemoveRequestedReviewersAsync` manage reviewers by login and team.

A review can comment on lines of the diff: `CreateReviewAsync` takes a `NewGitHubReview` whose `NewGitHubReviewComment` records name a path, a `Line` (and a `StartLine` for a range) and a `GitHubDiffSide`. Leave its `Event` null to keep the review pending, then `SubmitReviewAsync` it. `ListReviewsAsync`, `GetReviewAsync`, `UpdateReviewAsync`, `DeletePendingReviewAsync` and `DismissReviewAsync` manage reviews; `ListReviewCommentsAsync`, `GetReviewCommentAsync`, `CreateReviewCommentAsync`, `ReplyToReviewCommentAsync`, `UpdateReviewCommentAsync`, `DeleteReviewCommentAsync` and `AddReviewCommentReactionAsync` the line comments (`GitHubReviewComment` records). A pull request's conversation comments, labels and assignees are `GitHubIssues`'s.

<!-- ikon-example: connectors-github-review -->
```csharp
var files = await pullRequests.ListFilesAsync("ikon-ai/examples", 42);
var changed = files.First(f => f.Status == "modified");

await pullRequests.CreateReviewAsync("ikon-ai/examples", 42, new NewGitHubReview(
    GitHubReviewEvent.RequestChanges,
    "Two things before this merges.",
    [new NewGitHubReviewComment(changed.Path, "This loop never ends when the list is empty.", Line: 18)]));
```

Some pull request features are GraphQL-only on GitHub; `GitHubPullRequests` sends them through GraphQL for you, looking up the pull request's node id from its number: `EnableAutoMergeAsync` (merges by itself once reviews and checks pass; the repository must allow auto-merge) and `DisableAutoMergeAsync`, `EnqueueAsync` and `DequeueAsync` for a merge queue, `MarkReadyForReviewAsync` and `ConvertToDraftAsync`, `ListReviewThreadsAsync` (`GitHubReviewThread` with every one of its `GitHubThreadComment` records) with `ResolveReviewThreadAsync` (a `GitHubThreadResolution`) and `UnresolveReviewThreadAsync`, and `MinimizeCommentAsync`, which hides a comment by its `NodeId` behind a `GitHubMinimizeReason`.

<!-- ikon-example: connectors-github-automerge -->
```csharp
var pullRequest = await pullRequests.GetAsync("ikon-ai/examples", 42);

if (pullRequest.IsDraft)
{
    await pullRequests.MarkReadyForReviewAsync("ikon-ai/examples", 42);
}

// Merges by itself once the required reviews and checks pass, unless someone pushes first.
await pullRequests.EnableAutoMergeAsync("ikon-ai/examples", 42, GitHubMergeMethod.Squash, expectedHeadSha: pullRequest.HeadSha);

foreach (var thread in (await pullRequests.ListReviewThreadsAsync("ikon-ai/examples", 42)).Where(t => t.IsOutdated && !t.IsResolved))
{
    await pullRequests.ResolveReviewThreadAsync(thread.Id, GitHubThreadResolution.Addressed);
}
```

### Repositories, files and the Git database

The `GitHub` client works a repository the way an editor does, without a clone. Repositories: `ListRepositoriesAsync`, `ListOrganizationRepositoriesAsync`, `ListUserRepositoriesAsync` and `GetRepositoryAsync` (each `GitHubRepository` says whether the token `CanPush`); `CreateRepositoryAsync` (a name, or a `NewGitHubRepository` for an organization, a first commit, a license or a `.gitignore`), `CreateRepositoryFromTemplateAsync`, `UpdateRepositoryAsync` (only the arguments given change), `DeleteRepositoryAsync` (needs `delete_repo`), `ForkRepositoryAsync` and `ListForksAsync`, `TransferRepositoryAsync`; `SetTopicsAsync`, `GetLanguagesAsync` and `ListContributorsAsync` (`GitHubContributor` records); `ListCollaboratorsAsync` (`GitHubCollaborator` records), `GetCollaboratorPermissionAsync`, `AddCollaboratorAsync` with a `GitHubRepositoryPermission` (true when GitHub sent an invitation the person must accept) and `RemoveCollaboratorAsync`; and `CreateDispatchEventAsync`, which triggers the repository's `repository_dispatch` workflows.

Files: `GetFileAsync` returns a `GitHubFile` with the `Content` bytes, or null when no file is at the path (nothing, a directory, a symlink or a submodule), and reads a file over one megabyte through its blob. `DownloadFileAsync` streams the raw content instead, and `DownloadFileBytesAsync` reads it whole but refuses one over `maxBytes` rather than truncating it. `SetFileAsync` writes one file as one commit and returns the new blob and commit SHAs as a `GitHubWriteResult`; replacing a file needs the `Sha` it was read at as `expectedSha`, and a file that changed since throws `ConnectorException` with `StatusCode` `409` — as does another commit reaching the branch first, which only a fresh read of the file tells apart. `ListTreeAsync` gives every file path at a reference (a `GitHubTree`, whose `IsTruncated` says GitHub stopped short on a very large tree), `ListDirectoryAsync` one directory (`GitHubContentEntry` records, at most 1,000), and `DeleteFileAsync`, `GetReadmeAsync` and `DownloadArchiveAsync` (the whole repository as a zip or tarball, `GitHubArchiveFormat`) the rest.

History: `ListCommitsAsync` (`GitHubCommit` records, newest first) is one request for `limit` commits (default 40, 1 to 100, else `ArgumentOutOfRangeException`) and filters by `since`, `until` and `author`; `GetCommitAsync` (a `GitHubCommit` whose `Files` are `GitHubChangedFile` records), `GetCommitDiffAsync`, `CompareAsync` (a `GitHubComparison` of two references) and `ListCommitPullRequestsAsync`. People: `GetUserAsync` (a `GitHubUser`). `GitHubPullRequests` has `ListAsync` (filtered by a `GitHubPullRequestState`), `GetAsync`, `CreateAsync` and `UpdateAsync` (`state: GitHubPullRequestState.Closed` closes one without merging), each returning a `GitHubPullRequest`, and `CreateReviewAsync` with a `GitHubReviewEvent` of `Approve`, `RequestChanges` or `Comment` (the last two need a body), which returns the `GitHubReview` it made. `GitHubIssues.AddCommentAsync` returns the `GitHubComment` it made.

<!-- ikon-example: connectors-github-files -->
```csharp
var readme = await gitHub.GetFileAsync("ikon-ai/examples", "README.md", reference: "main");
var text = readme is null ? "" : System.Text.Encoding.UTF8.GetString(readme.Content);

var head = await gitHub.GetBranchHeadAsync("ikon-ai/examples", "main");
await gitHub.CreateBranchAsync("ikon-ai/examples", "docs/intro", head!);
await gitHub.SetFileAsync("ikon-ai/examples", "README.md", System.Text.Encoding.UTF8.GetBytes(text + "\nMore."),
    "Extend the intro", "docs/intro", expectedSha: readme?.Sha);

var pullRequest = await pullRequests.CreateAsync("ikon-ai/examples", "Extend the intro", head: "docs/intro", baseBranch: "main");
```

Branches: `ListBranchesAsync` (`GitHubBranch` records), `GetBranchAsync`, `GetBranchHeadAsync` (null for a missing branch), `CreateBranchAsync`, `RenameBranchAsync`, `DeleteBranchAsync`, `MergeUpstreamAsync` for a fork (a `GitHubMergeResult`, refused merges as an answer) and `MergeBranchAsync` (a merge commit, null when there was nothing to merge, `409` on a conflict). `GetBranchProtectionAsync` reads classic protection (a `GitHubBranchProtection`, null when there is none) and `ListBranchRulesAsync` the ruleset rules that apply (`GitHubBranchRule` records). Tags and refs: `ListTagsAsync` (`GitHubTag`), `ListMatchingRefsAsync`, `FindRefAsync`, `CreateRefAsync`, `UpdateRefAsync` and `DeleteRefAsync` (each a `GitHubRef`), and `CreateTagAsync` for an annotated tag, whose returned ref points at the tag object as `FindRefAsync` reads it, not at the commit.

The Git database is there for what the contents API cannot do: `GetTreeAsync` (a `GitHubTreeListing` of `GitHubTreeEntry` records), `CreateBlobAsync`, `DownloadBlobBytesAsync`, `CreateTreeAsync`, `GetGitCommitAsync` and `CreateGitCommitAsync` (`GitHubGitCommit` records). `CommitFilesAsync` puts them together: it writes and deletes several files (`GitHubFileChange` records, null content to delete) as **one** commit, so they change together or not at all. A head that is not `expectedHeadSha`, or another commit reaching the branch first, is a `409` and the branch is left as it was; a branch protection rule that refuses the commit is its own `422`:

<!-- ikon-example: connectors-github-commit -->
```csharp
var commit = await gitHub.CommitFilesAsync("ikon-ai/examples", "main",
[
    new GitHubFileChange("docs/intro.md", System.Text.Encoding.UTF8.GetBytes("# Intro\n")),
    new GitHubFileChange("scripts/build.sh", System.Text.Encoding.UTF8.GetBytes("#!/bin/sh\n"), IsExecutable: true),
    new GitHubFileChange("docs/old.md", null),   // deletes it
], "Restructure the docs", expectedHeadSha: expectedHead);
```

Writes that create content — files, blobs, commits — run one at a time: GitHub refuses bursts of them as a secondary rate limit.

### Checks and statuses

`GitHubChecks` is what CI reports against a commit. Commit statuses work with any token that can write them: `CreateStatusAsync` sets a context's `GitHubStatusState`, `ListStatusesAsync` lists them (`GitHubStatus`), and `GetCombinedStatusAsync` adds them up (`GitHubCombinedStatus`). Check runs are richer and **only a GitHub App can write them**, with an installation token: `CreateRunAsync` takes a `NewGitHubCheckRun`, `UpdateRunAsync` moves it through `GitHubCheckStatus` to a `GitHubCheckConclusion`, and a `GitHubCheckOutput` carries a Markdown summary and `GitHubCheckAnnotation` records pinned to lines (`GitHubAnnotationLevel`). GitHub takes 50 annotations a request; the connector sends more in batches of 50. A summary or text over 65,535 characters is refused with `ArgumentException`. `GetRunAsync`, `ListRunsAsync` (`GitHubCheckRun`), `ListRunAnnotationsAsync`, `RerequestRunAsync`, `ListSuitesAsync` (`GitHubCheckSuite`) and `RerequestSuiteAsync` read and re-run them.

<!-- ikon-example: connectors-github-checks -->
```csharp
var run = await checks.CreateRunAsync("ikon-ai/examples", new NewGitHubCheckRun("lint", headSha, GitHubCheckStatus.InProgress));

await checks.UpdateRunAsync("ikon-ai/examples", run.Id,
    conclusion: findings.Count == 0 ? GitHubCheckConclusion.Success : GitHubCheckConclusion.Failure,
    output: new GitHubCheckOutput("Lint", $"{findings.Count} findings", Annotations: findings));   // sent 50 to a request
```

### Actions

`GitHubActions` drives workflows (`GitHubWorkflow`): `ListWorkflowsAsync`, `GetWorkflowAsync`, `EnableWorkflowAsync`, `DisableWorkflowAsync`, and `DispatchWorkflowAsync`, which runs a workflow with a `workflow_dispatch` trigger and returns the id of the run GitHub started (null when it names none) for `GetRunAsync`. Runs (`GitHubWorkflowRun`) are listed by a `GitHubRunQuery` with `ListRunsAsync`, or a page at a time with `ListRunsPageAsync` (GitHub returns at most 1,000 for a filtered query), and handled with `GetRunAsync`, `RerunAsync`, `RerunFailedJobsAsync`, `CancelRunAsync` (`force` stops `always()` steps too), `ApproveRunAsync` for a first-time contributor's fork and `DeleteRunAsync`. Jobs (`GitHubJob` with `GitHubJobStep` records): `ListJobsAsync`, `GetJobAsync`, `RerunJobAsync`.

Logs and artifacts are redirects to signed links that live about a minute; the connector follows them at once, without the token: `DownloadRunLogsAsync` streams a run's logs as a zip, `DownloadJobLogAsync` reads one job's log as text up to `maxBytes`, and `ListArtifactsAsync` (`GitHubArtifact`), `DownloadArtifactAsync`, `DownloadArtifactBytesAsync` and `DeleteArtifactAsync` handle artifacts. A link that has expired is refused with no `StatusCode`, since it says nothing about the token.

Variables and secrets live in a `GitHubActionsScope` — `Repository`, `Environment` or `Organization`: `ListVariablesAsync`, `FindVariableAsync`, `SetVariableAsync` (creates or changes; an organization's new variable is for its private repositories), `DeleteVariableAsync` (`GitHubVariable`), and for secrets `ListSecretsAsync` (names and dates only, `GitHubSecret`) and `DeleteSecretAsync`. A name is letters, digits and underscores, not starting with a digit or `GITHUB_`; any other throws `ArgumentException` before a request. Writing a secret's value is not here: GitHub needs it sealed with libsodium first. Caches (`GitHubActionsCache`): `ListCachesAsync`, `DeleteCacheAsync`, `DeleteCachesByKeyAsync`.

<!-- ikon-example: connectors-github-actions -->
```csharp
var runId = await actions.DispatchWorkflowAsync("ikon-ai/examples", "nightly.yml", "main", new Dictionary<string, string> { ["suite"] = "full" });

var failed = await actions.ListRunsAsync("ikon-ai/examples", new GitHubRunQuery(Workflow: "ci.yml", Branch: "main", Status: "failure"), maxPages: 1);

foreach (var job in failed.Count == 0 ? [] : await actions.ListJobsAsync("ikon-ai/examples", failed[0].Id))
{
    if (job.Conclusion == "failure")
    {
        var log = await actions.DownloadJobLogAsync("ikon-ai/examples", job.Id, maxBytes: 1_000_000);
        await ShowAsync(log[^Math.Min(log.Length, 2000)..]);   // the end of the log, where the failure is
    }
}

await actions.SetVariableAsync(GitHubActionsScope.Environment("ikon-ai/examples", "staging"), "REGION", "eu-north");
```

`maxPages: 1` caps the listing rather than reading its first page: with more than 100 matching runs it throws `ConnectorPageCapException<GitHubWorkflowRun>`, so read one page with `ListRunsPageAsync` instead.

### Releases and deployments

`GitHubReleases` publishes versions as `GitHubRelease` records: `ListAsync`, `GetAsync`, `FindLatestAsync` and `FindByTagAsync` (null when there is none), `CreateAsync` with a `NewGitHubRelease` (a tag, notes, draft, prerelease, notes GitHub generates), `UpdateAsync` (`isDraft: false` publishes a draft), `DeleteAsync`, and `GenerateNotesAsync` for the notes alone (`GitHubReleaseNotes`). Assets (`GitHubReleaseAsset`): `ListAssetsAsync`, `UploadAssetAsync` — a stream with its length and media type, sent to GitHub's uploads host; a throttled upload is sent again only from a stream that can seek — `UpdateAssetAsync`, `DeleteAssetAsync`, and `DownloadAssetAsync` and `DownloadAssetBytesAsync`, which follow GitHub's redirect to its storage host without the token.

<!-- ikon-example: connectors-github-release -->
```csharp
var release = await releases.CreateAsync("ikon-ai/examples", new NewGitHubRelease("v2.0.0", GenerateNotes: true, IsDraft: true));

await using (var package = File.OpenRead(packagePath))
{
    await releases.UploadAssetAsync("ikon-ai/examples", release.Id, "examples-2.0.0.zip", package, package.Length, "application/zip");
}

await releases.UpdateAsync("ikon-ai/examples", release.Id, isDraft: false);   // publish
```

`GitHubDeployments` records what was deployed where: `ListAsync`, `GetAsync`, `CreateAsync` with a `NewGitHubDeployment` (it never merges the default branch into the ref first; a required status that has not passed is a `409`), `DeleteAsync` for an inactive one, `CreateStatusAsync` with a `GitHubDeploymentState` plus the environment and log addresses, and `ListStatusesAsync` (`GitHubDeploymentStatus`). `ListEnvironmentsAsync` and `FindEnvironmentAsync` read the repository's environments (`GitHubEnvironment`) and the kinds of rule protecting them.

<!-- ikon-example: connectors-github-deploy -->
```csharp
GitHubDeployment deployment = await deployments.CreateAsync("ikon-ai/examples", new NewGitHubDeployment("main", "staging"));
await deployments.CreateStatusAsync("ikon-ai/examples", deployment.Id, GitHubDeploymentState.InProgress);

try
{
    var address = await deployAsync();
    await deployments.CreateStatusAsync("ikon-ai/examples", deployment.Id, GitHubDeploymentState.Success, environmentUrl: address.ToString());
}
catch (Exception)
{
    await deployments.CreateStatusAsync("ikon-ai/examples", deployment.Id, GitHubDeploymentState.Failure);
    throw;
}
```

### Discussions, projects and GraphQL

Discussions and Projects live in GitHub's GraphQL API, and their clients use it; they spend GitHub's GraphQL point budget rather than the REST one, and name things by GraphQL ids. `GitHubDiscussions` (Discussions must be on for the repository): `ListCategoriesAsync` (`GitHubDiscussionCategory`; an `IsAnswerable` one takes answers), `ListAsync`, `FindAsync` (null when there is none) and `CreateAsync` (each a `GitHubDiscussion`), `ListCommentsAsync` (`GitHubDiscussionComment` records with their replies), `AddCommentAsync` (a reply with `replyToId`) and `MarkAnswerAsync`.

<!-- ikon-example: connectors-github-discussions -->
```csharp
var questions = (await discussions.ListCategoriesAsync("ikon-ai/examples")).First(c => c.IsAnswerable);
var discussion = await discussions.CreateAsync("ikon-ai/examples", questions.Id, "How do I page a listing?", "Is there a cursor?");

var reply = await discussions.AddCommentAsync(discussion.Id, "Use ListPageAsync and pass back NextCursor.");
await discussions.MarkAnswerAsync(reply.Id);
```

`GitHubProjects` works an organization's or a person's project boards: `ListAsync` (`GitHubProject`), `ListFieldsAsync` (`GitHubProjectField` with its `GitHubProjectFieldOption` options and iterations), `ListItemsAsync` (`GitHubProjectItem` with each field's value as text by field name), `AddItemAsync` with an issue's or pull request's `NodeId`, `UpdateItemFieldAsync` with a `GitHubProjectFieldValue` (`Text`, `Number`, `Date`, `SingleSelect` or `Iteration`), and `DeleteItemAsync`.

<!-- ikon-example: connectors-github-projects -->
```csharp
var roadmap = (await projects.ListAsync("ikon-ai")).First(p => p.Title == "Roadmap");
var status = (await projects.ListFieldsAsync(roadmap.Id)).First(f => f.Name == "Status");

var itemId = await projects.AddItemAsync(roadmap.Id, issue.NodeId);
await projects.UpdateItemFieldAsync(roadmap.Id, itemId, status.Id, GitHubProjectFieldValue.SingleSelect(status.Options.First(o => o.Name == "In progress").Id));
```

For anything else GraphQL reaches, `GitHub.QueryAsync` runs a query or mutation and returns its `data`. GitHub answers a failed query with HTTP 200 and an `errors` list; the connector throws for any error, a partial answer included, with the error's `type` as `ErrorCode` (`NOT_FOUND` is a `404`, `FORBIDDEN` a `403`). `RATE_LIMITED`, which GitHub answers before running the query, is sent again as a rate-limited REST call is and surfaces as a `429` only after that. `GitHub.ListFolderTextsAsync` uses it to read the text of every file in a folder in one request (`GitHubFolderTexts` of `GitHubTextFile` records; `IsComplete` is false when a binary or too-large file was left out).

<!-- ikon-example: connectors-github-graphql -->
```csharp
var data = await gitHub.QueryAsync(
    "query($owner: String!, $name: String!) { repository(owner: $owner, name: $name) { stargazerCount forkCount } }",
    new Dictionary<string, object?> { ["owner"] = "ikon-ai", ["name"] = "examples" });

var stars = data.GetProperty("repository").GetProperty("stargazerCount").GetInt32();
```

### Search

`GitHubSearch` runs GitHub's search syntax across `SearchRepositoriesAsync`, `SearchIssuesAsync` (the query must say `is:issue` or `is:pr`), `SearchCodeAsync` (default branches only, files under 384 KB, with the matching fragments), `SearchCommitsAsync` and `SearchUsersAsync`. Each reads at most `limit` results, 1 to 1,000 — GitHub returns no more than 1,000 of any query — into a `GitHubSearchResults<T>` with GitHub's `TotalCount` and `IncompleteResults`, set when its search timed out; a code match is a `GitHubCodeMatch`. Search has its own limits: 30 a minute, 10 for code.

<!-- ikon-example: connectors-github-search -->
```csharp
var stale = await search.SearchIssuesAsync("repo:ikon-ai/examples is:pr is:open review:required updated:<2026-09-01", limit: 50);

foreach (var pullRequest in stale.Items)
{
    await ShowAsync($"#{pullRequest.Number} {pullRequest.Title} waits for review");
}
```

### People, organizations and notifications

`GitHub` also reads people: `GetUserAsync(login)`, `ListEmailsAsync` (`GitHubEmail`, needs `user:email`), stars (`StarAsync`, `UnstarAsync`, `IsStarredAsync`, `ListStarredAsync`, `ListStargazersAsync`), watching (`WatchAsync`, `UnwatchAsync`), `GetRateLimitAsync` (a `GitHubRateLimit` of `GitHubRateLimitBucket` windows, free to call) and `RenderMarkdownAsync`.

`GitHubOrganizations` covers an organization and its teams: `ListMineAsync`, `GetAsync` (`GitHubOrganization`), `ListMembersAsync` by `GitHubOrganizationRole`, `FindMembershipAsync`, `SetMembershipAsync` (invites someone not yet a member; `GitHubMembership` stays `pending` until they accept), `RemoveMemberAsync`, `ListOutsideCollaboratorsAsync`, `ListInvitationsAsync` (`GitHubInvitation`) and `CancelInvitationAsync`; teams (`GitHubTeam`) with `ListTeamsAsync`, `ListMyTeamsAsync`, `GetTeamAsync`, `CreateTeamAsync`, `UpdateTeamAsync`, `DeleteTeamAsync`, `ListTeamMembersAsync`, `SetTeamMembershipAsync` with a `GitHubTeamRole`, `RemoveTeamMembershipAsync`, `ListTeamRepositoriesAsync`, `SetTeamRepositoryPermissionAsync` and `RemoveTeamRepositoryAsync`.

<!-- ikon-example: connectors-github-teams -->
```csharp
var team = await organizations.CreateTeamAsync("ikon-ai", "docs-reviewers", "Reviews every docs change");
await organizations.SetTeamMembershipAsync("ikon-ai", team.Slug, "octocat", GitHubTeamRole.Maintainer);
await organizations.SetTeamRepositoryPermissionAsync("ikon-ai", team.Slug, "ikon-ai/examples", GitHubRepositoryPermission.Write);
```

`GitHubNotifications` is the account's inbox, and **needs a classic personal access token or an OAuth app token** (`notifications` or `repo`); GitHub refuses fine-grained and GitHub App tokens with `403`. `ListAsync` lists threads (`GitHubNotification`), and `DeltaAsync` polls for changes with `If-Modified-Since`, so a poll that finds nothing costs no rate limit, and returns only the threads updated since the cursor when something changed; its `GitHubNotificationDelta` carries the `LastModified` cursor to pass back and the `PollInterval` GitHub asks clients to keep. Threads come newest first, so a delta past its `maxPages` hands back the cursor it was given as `ResumeFrom`: no later one covers the threads left unread. `MarkReadAsync`, `MarkThreadReadAsync`, `MarkThreadDoneAsync` and `SetThreadIgnoredAsync` act on them.

<!-- ikon-example: connectors-github-inbox -->
```csharp
var delta = await notifications.DeltaAsync(cursor);   // a 304 when nothing changed, at no rate-limit cost

foreach (var thread in delta.Items.Where(t => t.IsUnread && t.Reason == "review_requested"))
{
    await ShowAsync($"{thread.Repository}: {thread.SubjectTitle}");
}

await SaveCursorAsync(delta.LastModified ?? "");
await Task.Delay(delta.PollInterval ?? TimeSpan.FromMinutes(1));   // GitHub's asked-for pace
```

### Signing in

`GitHubAuth` signs a person in two ways. The device flow needs no redirect address: they type a short code at github.com, so it works the same from a laptop and from a deployed app. Device flow must be enabled on the OAuth app or GitHub App. `StartDeviceFlowAsync` returns a `GitHubDeviceCode` whose `UserCode` and `VerificationUri` you show the person (printing it shows `***` for the secret `DeviceCode`), and `WaitForDeviceTokenAsync` polls at the pace GitHub asks for until it can return their `GitHubUserToken`; a code that expires or a person who declines is a `401` with GitHub's code (`expired_token`, `access_denied`) in `ErrorCode`.

<!-- ikon-example: connectors-github-device -->
```csharp
var code = await GitHubAuth.StartDeviceFlowAsync(clientId, scope: "repo");
await ShowAsync($"Open {code.VerificationUri} and enter {code.UserCode}");

var token = await GitHubAuth.WaitForDeviceTokenAsync(clientId, code);   // polls until they have
var gitHub = new GitHub(new GitHubTokenProvider(token, clientId));
```

The web flow sends the person to `AuthorizeUrl` and exchanges the code GitHub redirects back with. Pass a `PkceCodes` to both halves, and check the returned `state` before exchanging:

<!-- ikon-example: connectors-github-web -->
```csharp
var pkce = PkceCodes.Create();
var state = Guid.NewGuid().ToString("N");
var signInUrl = GitHubAuth.AuthorizeUrl(clientId, redirectUri, state, scope: "repo", pkce: pkce);

// …the person signs in and GitHub redirects back with ?code=…&state=…
if (returnedState == state)
{
    var token = await GitHubAuth.ExchangeCodeAsync(clientId, clientSecret, returnedCode, redirectUri, pkce.Verifier);
}
```

A GitHub App's user token expires after eight hours and comes with a `RefreshToken`; a `GitHubTokenProvider` renews it for you, and `RefreshAsync` does it by hand. With the app's client id and secret, `CheckTokenAsync` reads what GitHub knows about a token the app issued — a `GitHubTokenInfo` with its scopes, expiry and person, or null when it is no longer honoured —, `RevokeTokenAsync` ends one token and `RevokeGrantAsync` ends the person's whole grant to the app.

### GitHub Apps

`GitHubApp` acts as the app itself, with a JWT signed by its private key: `GetAppAsync` (a `GitHubAppDetails` with the app's permissions and events), `ListInstallationsAsync`, `GetInstallationAsync`, `FindRepositoryInstallationAsync`, `FindOrganizationInstallationAsync` and `FindUserInstallationAsync` (each a `GitHubInstallation`, null where the app is not installed), `DeleteInstallationAsync`, `SuspendInstallationAsync` and `UnsuspendInstallationAsync`. `CreateInstallationTokenAsync` mints a `GitHubInstallationToken` for something outside the process, such as a git push; its options may narrow what the installation reaches, and asking for more is a `422`. Printing one shows `***` for the token. To act inside an installation, give a client a `GitHubTokenProvider` built from the app credentials and the installation id instead.

<!-- ikon-example: connectors-github-installations -->
```csharp
var app = new GitHubApp(new GitHubAppCredentials(appClientId, privateKeyPem));
GitHubAppDetails details = await app.GetAppAsync();
var installation = await app.FindRepositoryInstallationAsync("ikon-ai/examples");

if (installation is not null)
{
    GitHubInstallationToken pushToken = await app.CreateInstallationTokenAsync(installation.Id, new GitHubInstallationTokenOptions(Repositories: ["examples"]));
    await ShowAsync($"{details.Name} can push to ikon-ai/examples until {pushToken.ExpiresAt:t}");
}
```

With an installation token, `GitHub.ListInstallationRepositoriesAsync` lists what the installation reaches; with an app's user token, `ListUserInstallationsAsync` and `ListUserInstallationRepositoriesAsync` list what the person can reach through it.

### Webhooks

A webhook endpoint is public, so check each delivery first: `GitHubAuth.VerifyWebhook` checks `X-Hub-Signature-256` against the raw body signed with the webhook's secret. `GitHubWebhookEvents.Parse` then reads the delivery from its `X-GitHub-Event` and `X-GitHub-Delivery` headers into a `GitHubWebhookEvent`, which carries the `Action`, `Repository`, `Sender`, `InstallationId` and the whole `Raw` JSON. The events apps act on have their own records — `GitHubPushEvent` (with `GitHubPushedCommit` records), `GitHubPullRequestEvent`, `GitHubPullRequestReviewEvent`, `GitHubPullRequestReviewCommentEvent`, `GitHubIssuesEvent`, `GitHubIssueCommentEvent`, `GitHubSubIssuesEvent`, `GitHubCheckRunEvent`, `GitHubCheckSuiteEvent`, `GitHubStatusEvent`, `GitHubWorkflowRunEvent`, `GitHubWorkflowJobEvent`, `GitHubReleaseEvent`, `GitHubRefEvent` (a branch or tag created or deleted), `GitHubDeploymentStatusEvent`, `GitHubDiscussionEvent`, `GitHubDiscussionCommentEvent`, `GitHubInstallationEvent`, `GitHubInstallationRepositoriesEvent` and `GitHubPingEvent` — and every other arrives as `GitHubUnknownEvent`. `Parse` never throws: a body that is not a JSON object is null, and one that lacks or garbles what its record needs arrives as `GitHubUnknownEvent`. GitHub wants an answer within ten seconds, and sends a redelivery with the same delivery id, so dedupe on `DeliveryId`.

<!-- ikon-example: connectors-github-webhook -->
```csharp
[HttpPost("/github/events", Auth = EndpointAuth.Public)]
public HttpResult OnGitHubEvent(Ikon.App.HttpRequest request)
{
    if (!GitHubAuth.VerifyWebhook(_webhookSecret, request.Headers.GetValueOrDefault("X-Hub-Signature-256"), request.Body))
    {
        return HttpResult.Unauthorized();
    }

    switch (GitHubWebhookEvents.Parse(request.Headers.GetValueOrDefault("X-GitHub-Event"), request.Body, request.Headers.GetValueOrDefault("X-GitHub-Delivery")))
    {
        case GitHubIssuesEvent { Action: "opened" } opened:
            // GitHub wants its answer within ten seconds, so the work runs after it.
            _ = Task.Run(() => _issues.AddLabelsAsync(opened.Repository!, opened.Issue.Number, ["needs-triage"]));
            break;

        case GitHubPushEvent { Ref: "refs/heads/main" } push:
            _ = Task.Run(() => ShowAsync($"{push.Sender} pushed {push.Commits.Count} commits to {push.Repository}"));
            break;
    }

    return HttpResult.Ok();
}
```

What each record carries:

<!-- ikon-example: connectors-github-events -->
```csharp
return delivery switch
{
    GitHubPingEvent ping => $"webhook {ping.HookId} is connected",
    GitHubPushEvent push => $"{push.Commits.Count} commits to {push.Ref}",
    GitHubRefEvent created when created.IsCreated => $"{created.RefType} {created.Ref} created",
    GitHubRefEvent deleted => $"{deleted.RefType} {deleted.Ref} deleted",
    GitHubPullRequestEvent pr => $"pull request #{pr.PullRequest.Number} {pr.Action}",
    GitHubPullRequestReviewEvent review => $"#{review.Number} reviewed: {review.Review.State}",
    GitHubPullRequestReviewCommentEvent line => $"#{line.Number} {line.Comment.Path}:{line.Comment.Line}",
    GitHubIssuesEvent issue => $"issue #{issue.Issue.Number} {issue.Action}",
    GitHubIssueCommentEvent comment => $"#{comment.Issue.Number}: {comment.Comment.Body}",
    GitHubSubIssuesEvent sub => $"#{sub.SubIssue.Number} under #{sub.Parent.Number}",
    GitHubCheckRunEvent run => $"{run.CheckRun.Name}: {run.CheckRun.Conclusion ?? run.CheckRun.Status}",
    GitHubCheckSuiteEvent suite => $"checks {suite.Action} for {suite.CheckSuite.HeadSha}",
    GitHubStatusEvent status => $"{status.Context}: {status.State}",
    GitHubWorkflowRunEvent workflow => $"{workflow.Run.Name} {workflow.Run.Conclusion ?? workflow.Run.Status}",
    GitHubWorkflowJobEvent job => $"job {job.Job.Name} {job.Job.Status}",
    GitHubReleaseEvent release => $"release {release.Release.TagName} {release.Action}",
    GitHubDeploymentStatusEvent deploy => $"{deploy.Deployment.Environment}: {deploy.Status.State}",
    GitHubDiscussionEvent discussion => $"discussion #{discussion.Number} in {discussion.Category}",
    GitHubDiscussionCommentEvent reply => $"reply on discussion #{reply.DiscussionNumber}",
    GitHubInstallationEvent installed => $"app {installed.Action} on {installed.Installation.Account}",
    GitHubInstallationRepositoriesEvent repos => $"{repos.Added.Count} repositories added, {repos.Removed.Count} removed",
    GitHubUnknownEvent other => $"{other.Name} {other.Action}",   // other.Raw holds GitHub's JSON
    _ => delivery.Name,
};
```

`GitHubHooks` manages the webhooks of a repository or an organization (a `GitHubHookScope`): `ListAsync`, `GetAsync` (`GitHubHook`), `CreateAsync` with a `NewGitHubHook` — an https address, the secret and the events; deliveries are JSON — `UpdateAsync` (only the arguments given change; a new address keeps the secret), `DeleteAsync` and `PingAsync`. **GitHub never retries a failed delivery by itself**: `ListDeliveriesAsync` (`GitHubHookDelivery`) finds the failures, `GetDeliveryAsync` reads one with its request and response, and `RedeliverAsync` sends it again within GitHub's 30 days. A GitHub App's own webhook, set in its settings, has the same on `GitHubApp`: `GetHookUrlAsync`, `UpdateHookAsync`, `ListHookDeliveriesAsync` and `RedeliverHookDeliveryAsync`.

<!-- ikon-example: connectors-github-hooks -->
```csharp
var hook = await hooks.CreateAsync(GitHubHookScope.Repository("ikon-ai/examples"),
    new NewGitHubHook("https://my-app.ikon.live/github/events", webhookSecret, ["push", "issues", "pull_request"]));

// GitHub never retries a failed delivery itself.
foreach (var failed in (await hooks.ListDeliveriesAsync(GitHubHookScope.Repository("ikon-ai/examples"), hook.Id, maxPages: 1)).Where(d => d.StatusCode >= 500))
{
    await hooks.RedeliverAsync(GitHubHookScope.Repository("ikon-ai/examples"), hook.Id, failed.Id);
}
```

`maxPages: 1` is a cap, not the newest page: once the hook has more than 100 deliveries it throws `ConnectorPageCapException<GitHubHookDelivery>`.

### Rate limits

GitHub allows 5,000 REST requests an hour for a person's token (15,000 on GitHub Enterprise Cloud), 5,000 for an installation plus 50 for each repository and user past 20 (at most 12,500), and 60 without a token; GraphQL has its own 5,000-point hourly budget, and search 30 a minute (10 for code). On top of those, secondary limits refuse bursts — more than 100 concurrent requests, or more than about 80 content-creating requests a minute. The connector retries a rate-limited request three times as described under **GitHub** and then throws `429`; `GitHub.GetRateLimitAsync` reads where a token stands without spending any. Make writes one at a time, poll notifications no faster than `PollInterval`, and prefer the delta methods and webhooks to re-reading whole listings.

### What it does not reach

- **Actions secret values.** Secrets are listed and deleted, but writing one needs it sealed with libsodium against the repository's key first, which the connector does not do.
- **Security alerts.** Code scanning, secret scanning, Dependabot alerts and security advisories.
- **Copilot.** Seat management, usage metrics and assigning the coding agent.
- **Packages, Codespaces, Pages and Git LFS objects.**
- **Administration.** Rulesets and branch protection are read, not written; self-hosted runners, organization settings, custom roles and enterprise APIs are out.
- **Gists, followers, keys and the public events timeline.**
- **Project views, workflows and status updates**, and creating or deleting projects themselves; `GitHub.QueryAsync` reaches anything else GraphQL offers.
