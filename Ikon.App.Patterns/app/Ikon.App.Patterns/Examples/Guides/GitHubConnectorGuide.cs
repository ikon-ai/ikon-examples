using Ikon.Connectors;

namespace Ikon.App.Patterns.Examples;

file sealed class GitHubConnectorGuideExamples
{
    private static Task ProcessIssueAsync(GitHubIssue item) => Task.CompletedTask;

    private static Task ShowAsync(string text) => Task.CompletedTask;

    private static Task SaveCursorAsync(string cursor) => Task.CompletedTask;

    private static Task SaveTokensAsync(GitHubUserToken tokens) => Task.CompletedTask;

    public async Task GitHubAsync(string token)
    {
        #region example:connectors-github
        var issues = new GitHubIssues(token);
        var issue = await issues.GetAsync("ikon-ai/examples", 42);
        var comment = await issues.AddCommentAsync("ikon-ai/examples", 42, "Reproduced on main.");
        #endregion

        Log.Instance.Debug($"{issue} {comment.HtmlUrl}");
    }

    public void GitHubShared(GitHubUserToken signedIn, string clientId)
    {
        #region example:connectors-github-provider
        var tokens = new GitHubTokenProvider(signedIn, clientId, onTokensRotated: (rotated, ct) => SaveTokensAsync(rotated));

        var gitHub = new GitHub(tokens);
        var issues = new GitHubIssues(tokens);
        var pullRequests = new GitHubPullRequests(tokens);
        #endregion

        Log.Instance.Debug($"{gitHub} {issues} {pullRequests}");
    }

    public void GitHubInstallation(string appClientId, string privateKeyPem, long installationId)
    {
        #region example:connectors-github-app
        var app = new GitHubAppCredentials(appClientId, privateKeyPem);
        var tokens = new GitHubTokenProvider(app, installationId);   // mints and renews installation tokens

        var issues = new GitHubIssues(tokens);
        #endregion

        Log.Instance.Debug($"{issues}");
    }

    public async Task GitHubAppInstallationsAsync(string appClientId, string privateKeyPem)
    {
        #region example:connectors-github-installations
        var app = new GitHubApp(new GitHubAppCredentials(appClientId, privateKeyPem));
        GitHubAppDetails details = await app.GetAppAsync();
        var installation = await app.FindRepositoryInstallationAsync("ikon-ai/examples");

        if (installation is not null)
        {
            GitHubInstallationToken pushToken = await app.CreateInstallationTokenAsync(installation.Id, new GitHubInstallationTokenOptions(Repositories: ["examples"]));
            await ShowAsync($"{details.Name} can push to ikon-ai/examples until {pushToken.ExpiresAt:t}");
        }
        #endregion
    }

    public async Task GitHubDeltaAsync(GitHubIssues issues, string? cursor, HashSet<int> seenIssueNumbers)
    {
        #region example:connectors-github-since
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
        #endregion
    }

    public async Task GitHubTriageAsync(GitHubIssues issues)
    {
        #region example:connectors-github-triage
        var untriaged = await issues.ListAsync("ikon-ai/examples", new GitHubIssueQuery(Labels: ["needs-triage"], Assignee: "none"));

        foreach (var issue in untriaged.Where(i => !i.IsPullRequest))
        {
            await issues.SetLabelsAsync("ikon-ai/examples", issue.Number, ["bug"]);
            await issues.UpdateAsync("ikon-ai/examples", issue.Number, assignees: ["octocat"], type: "Bug");
        }

        var epic = await issues.CreateAsync("ikon-ai/examples", new NewGitHubIssue("Release 2.0", Labels: ["epic"]));
        await issues.AddSubIssueAsync("ikon-ai/examples", epic.Number, untriaged[0].Id);   // the sub-issue's id, not its number
        #endregion
    }

    public async Task GitHubMergeAsync(GitHubPullRequests pullRequests)
    {
        #region example:connectors-github-merge
        var reviewedHead = (await pullRequests.GetAsync("ikon-ai/examples", 42)).HeadSha;   // before reading the diff
        var result = await pullRequests.MergeAsync("ikon-ai/examples", 42, reviewedHead, commitTitle: "Add retry policy");

        if (!result.Merged)
        {
            Log.Instance.Warning($"PR #42 not merged: {result.Message}");
        }
        #endregion
    }

    public async Task GitHubReviewAsync(GitHubPullRequests pullRequests)
    {
        #region example:connectors-github-review
        var files = await pullRequests.ListFilesAsync("ikon-ai/examples", 42);
        var changed = files.First(f => f.Status == "modified");

        await pullRequests.CreateReviewAsync("ikon-ai/examples", 42, new NewGitHubReview(
            GitHubReviewEvent.RequestChanges,
            "Two things before this merges.",
            [new NewGitHubReviewComment(changed.Path, "This loop never ends when the list is empty.", Line: 18)]));
        #endregion
    }

    public async Task GitHubFilesAsync(GitHub gitHub, GitHubPullRequests pullRequests)
    {
        #region example:connectors-github-files
        var readme = await gitHub.GetFileAsync("ikon-ai/examples", "README.md", reference: "main");
        var text = readme is null ? "" : System.Text.Encoding.UTF8.GetString(readme.Content);

        var head = await gitHub.GetBranchHeadAsync("ikon-ai/examples", "main");
        await gitHub.CreateBranchAsync("ikon-ai/examples", "docs/intro", head!);
        await gitHub.SetFileAsync("ikon-ai/examples", "README.md", System.Text.Encoding.UTF8.GetBytes(text + "\nMore."),
            "Extend the intro", "docs/intro", expectedSha: readme?.Sha);

        var pullRequest = await pullRequests.CreateAsync("ikon-ai/examples", "Extend the intro", head: "docs/intro", baseBranch: "main");
        #endregion

        Log.Instance.Debug($"{pullRequest.Number}");
    }

    public async Task GitHubCommitFilesAsync(GitHub gitHub, string expectedHead)
    {
        #region example:connectors-github-commit
        var commit = await gitHub.CommitFilesAsync("ikon-ai/examples", "main",
        [
            new GitHubFileChange("docs/intro.md", System.Text.Encoding.UTF8.GetBytes("# Intro\n")),
            new GitHubFileChange("scripts/build.sh", System.Text.Encoding.UTF8.GetBytes("#!/bin/sh\n"), IsExecutable: true),
            new GitHubFileChange("docs/old.md", null),   // deletes it
        ], "Restructure the docs", expectedHeadSha: expectedHead);
        #endregion

        Log.Instance.Debug($"{commit.Sha}");
    }

    public async Task GitHubCheckRunAsync(GitHubChecks checks, string headSha, IReadOnlyList<GitHubCheckAnnotation> findings)
    {
        #region example:connectors-github-checks
        var run = await checks.CreateRunAsync("ikon-ai/examples", new NewGitHubCheckRun("lint", headSha, GitHubCheckStatus.InProgress));

        await checks.UpdateRunAsync("ikon-ai/examples", run.Id,
            conclusion: findings.Count == 0 ? GitHubCheckConclusion.Success : GitHubCheckConclusion.Failure,
            output: new GitHubCheckOutput("Lint", $"{findings.Count} findings", Annotations: findings));   // sent 50 to a request
        #endregion
    }

    public async Task GitHubReleaseAsync(GitHubReleases releases, string packagePath)
    {
        #region example:connectors-github-release
        var release = await releases.CreateAsync("ikon-ai/examples", new NewGitHubRelease("v2.0.0", GenerateNotes: true, IsDraft: true));

        await using (var package = File.OpenRead(packagePath))
        {
            await releases.UploadAssetAsync("ikon-ai/examples", release.Id, "examples-2.0.0.zip", package, package.Length, "application/zip");
        }

        await releases.UpdateAsync("ikon-ai/examples", release.Id, isDraft: false);   // publish
        #endregion
    }

    public async Task GitHubDeployAsync(GitHubDeployments deployments, Func<Task<Uri>> deployAsync)
    {
        #region example:connectors-github-deploy
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
        #endregion
    }

    public async Task GitHubActionsAsync(GitHubActions actions)
    {
        #region example:connectors-github-actions
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
        #endregion

        Log.Instance.Debug($"{runId}");
    }

    public async Task GitHubSearchAsync(GitHubSearch search)
    {
        #region example:connectors-github-search
        var stale = await search.SearchIssuesAsync("repo:ikon-ai/examples is:pr is:open review:required updated:<2026-09-01", limit: 50);

        foreach (var pullRequest in stale.Items)
        {
            await ShowAsync($"#{pullRequest.Number} {pullRequest.Title} waits for review");
        }
        #endregion

        Log.Instance.Debug($"{stale.TotalCount}");
    }

    public async Task GitHubInboxAsync(GitHubNotifications notifications, string? cursor)
    {
        #region example:connectors-github-inbox
        var delta = await notifications.DeltaAsync(cursor);   // a 304 when nothing changed, at no rate-limit cost

        foreach (var thread in delta.Items.Where(t => t.IsUnread && t.Reason == "review_requested"))
        {
            await ShowAsync($"{thread.Repository}: {thread.SubjectTitle}");
        }

        await SaveCursorAsync(delta.LastModified ?? "");
        await Task.Delay(delta.PollInterval ?? TimeSpan.FromMinutes(1));   // GitHub's asked-for pace
        #endregion
    }

    public async Task GitHubTeamsAsync(GitHubOrganizations organizations)
    {
        #region example:connectors-github-teams
        var team = await organizations.CreateTeamAsync("ikon-ai", "docs-reviewers", "Reviews every docs change");
        await organizations.SetTeamMembershipAsync("ikon-ai", team.Slug, "octocat", GitHubTeamRole.Maintainer);
        await organizations.SetTeamRepositoryPermissionAsync("ikon-ai", team.Slug, "ikon-ai/examples", GitHubRepositoryPermission.Write);
        #endregion
    }

    public async Task GitHubAutoMergeAsync(GitHubPullRequests pullRequests)
    {
        #region example:connectors-github-automerge
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
        #endregion
    }

    public async Task GitHubDiscussionsAsync(GitHubDiscussions discussions)
    {
        #region example:connectors-github-discussions
        var questions = (await discussions.ListCategoriesAsync("ikon-ai/examples")).First(c => c.IsAnswerable);
        var discussion = await discussions.CreateAsync("ikon-ai/examples", questions.Id, "How do I page a listing?", "Is there a cursor?");

        var reply = await discussions.AddCommentAsync(discussion.Id, "Use ListPageAsync and pass back NextCursor.");
        await discussions.MarkAnswerAsync(reply.Id);
        #endregion
    }

    public async Task GitHubProjectsAsync(GitHubProjects projects, GitHubIssue issue)
    {
        #region example:connectors-github-projects
        var roadmap = (await projects.ListAsync("ikon-ai")).First(p => p.Title == "Roadmap");
        var status = (await projects.ListFieldsAsync(roadmap.Id)).First(f => f.Name == "Status");

        var itemId = await projects.AddItemAsync(roadmap.Id, issue.NodeId);
        await projects.UpdateItemFieldAsync(roadmap.Id, itemId, status.Id, GitHubProjectFieldValue.SingleSelect(status.Options.First(o => o.Name == "In progress").Id));
        #endregion
    }

    public async Task GitHubGraphQLAsync(GitHub gitHub)
    {
        #region example:connectors-github-graphql
        var data = await gitHub.QueryAsync(
            "query($owner: String!, $name: String!) { repository(owner: $owner, name: $name) { stargazerCount forkCount } }",
            new Dictionary<string, object?> { ["owner"] = "ikon-ai", ["name"] = "examples" });

        var stars = data.GetProperty("repository").GetProperty("stargazerCount").GetInt32();
        #endregion

        Log.Instance.Debug($"{stars}");
    }

    public async Task GitHubDeviceSignInAsync(string clientId)
    {
        #region example:connectors-github-device
        var code = await GitHubAuth.StartDeviceFlowAsync(clientId, scope: "repo");
        await ShowAsync($"Open {code.VerificationUri} and enter {code.UserCode}");

        var token = await GitHubAuth.WaitForDeviceTokenAsync(clientId, code);   // polls until they have
        var gitHub = new GitHub(new GitHubTokenProvider(token, clientId));
        #endregion

        Log.Instance.Debug($"{gitHub}");
    }

    public async Task GitHubWebSignInAsync(string clientId, string clientSecret, string redirectUri, string returnedCode, string returnedState)
    {
        #region example:connectors-github-web
        var pkce = PkceCodes.Create();
        var state = Guid.NewGuid().ToString("N");
        var signInUrl = GitHubAuth.AuthorizeUrl(clientId, redirectUri, state, scope: "repo", pkce: pkce);

        // …the person signs in and GitHub redirects back with ?code=…&state=…
        if (returnedState == state)
        {
            var token = await GitHubAuth.ExchangeCodeAsync(clientId, clientSecret, returnedCode, redirectUri, pkce.Verifier);
        }
        #endregion

        Log.Instance.Debug($"{signInUrl}");
    }

    private readonly string _webhookSecret = "";
    private readonly GitHubIssues _issues = new("ghp_example");

    #region example:connectors-github-webhook
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
    #endregion

    public static string DescribeGitHubEvent(GitHubWebhookEvent delivery)
    {
        #region example:connectors-github-events
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
        #endregion
    }

    public async Task GitHubHookAsync(GitHubHooks hooks, string webhookSecret)
    {
        #region example:connectors-github-hooks
        var hook = await hooks.CreateAsync(GitHubHookScope.Repository("ikon-ai/examples"),
            new NewGitHubHook("https://my-app.ikon.live/github/events", webhookSecret, ["push", "issues", "pull_request"]));

        // GitHub never retries a failed delivery itself.
        foreach (var failed in (await hooks.ListDeliveriesAsync(GitHubHookScope.Repository("ikon-ai/examples"), hook.Id, maxPages: 1)).Where(d => d.StatusCode >= 500))
        {
            await hooks.RedeliverAsync(GitHubHookScope.Repository("ikon-ai/examples"), hook.Id, failed.Id);
        }
        #endregion
    }
}
