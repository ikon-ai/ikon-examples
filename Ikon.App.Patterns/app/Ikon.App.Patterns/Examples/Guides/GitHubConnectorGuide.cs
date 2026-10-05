using Ikon.Connectors;

namespace Ikon.App.Patterns.Examples;

file sealed class GitHubConnectorGuideExamples
{
    private static Task ProcessIssueAsync(GitHubIssue item) => Task.CompletedTask;

    private static Task ShowAsync(string text) => Task.CompletedTask;

    public async Task GitHubAsync(string token)
    {
        #region example:connectors-github
        var gitHub = new GitHub(token);
        var issue = await gitHub.GetIssueAsync("ikon-ai/examples", 42);
        var commentUrl = await gitHub.CommentAsync("ikon-ai/examples", 42, "Reproduced on main.");
        #endregion

        Log.Instance.Debug($"{issue} {commentUrl}");
    }

    public async Task GitHubSinceAsync(GitHub gitHub, string cursor, HashSet<int> seenIssueNumbers)
    {
        #region example:connectors-github-since
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
        #endregion
    }

    public async Task GitHubMergeAsync(GitHub gitHub)
    {
        #region example:connectors-github-merge
        var reviewedHead = await gitHub.GetPullRequestHeadShaAsync("ikon-ai/examples", 42);   // before reading the diff
        var result = await gitHub.MergePullRequestAsync("ikon-ai/examples", 42, reviewedHead, commitTitle: "Add retry policy");

        if (!result.Merged)
        {
            Log.Instance.Warning($"PR #42 not merged: {result.Message}");
        }
        #endregion
    }

    public async Task GitHubFilesAsync(GitHub gitHub)
    {
        #region example:connectors-github-files
        var readme = await gitHub.GetFileAsync("ikon-ai/examples", "README.md", reference: "main");
        var text = readme is null ? "" : System.Text.Encoding.UTF8.GetString(readme.Content);

        var head = await gitHub.GetBranchHeadAsync("ikon-ai/examples", "main");
        await gitHub.CreateBranchAsync("ikon-ai/examples", "docs/intro", head!);
        await gitHub.PutFileAsync("ikon-ai/examples", "README.md", System.Text.Encoding.UTF8.GetBytes(text + "\nMore."),
            "Extend the intro", "docs/intro", expectedSha: readme?.Sha);

        var pullRequest = await gitHub.CreatePullRequestAsync("ikon-ai/examples", "Extend the intro", head: "docs/intro", baseBranch: "main");
        #endregion

        Log.Instance.Debug($"{pullRequest.Number}");
    }

    public async Task GitHubDeviceSignInAsync(string clientId)
    {
        #region example:connectors-github-device
        var code = await GitHubAuth.StartDeviceFlowAsync(clientId, scope: "repo");
        await ShowAsync($"Open {code.VerificationUri} and enter {code.UserCode}");

        var token = await GitHubAuth.WaitForDeviceTokenAsync(clientId, code);   // polls until they have
        var gitHub = new GitHub(token.AccessToken);
        #endregion

        Log.Instance.Debug($"{gitHub}");
    }

    public bool GitHubWebhook(string webhookSecret, string? signatureHeader, string body)
    {
        #region example:connectors-github-webhook
        if (!GitHubAuth.VerifyWebhook(webhookSecret, signatureHeader, body))   // X-Hub-Signature-256
        {
            return false;
        }
        #endregion

        return true;
    }
}
