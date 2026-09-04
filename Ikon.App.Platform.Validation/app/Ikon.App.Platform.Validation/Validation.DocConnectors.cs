using Ikon.Connectors;
using Ikon.Connectors.Browser;
using Ikon.Connectors.Google;

// The connectors guide, as one file that compiles. The validation app now references
// Ikon.Connectors and Ikon.Connectors.Browser so these examples can be pinned to real calls
// rather than transcribed.
file sealed class DocConnectorsGuide
{
    private static Task ProcessAsync(SlackMessage message) => Task.CompletedTask;

    private static Task ProcessIssueAsync(GitHubIssue item) => Task.CompletedTask;

    public async Task ErrorsAsync(Slack slack, string channelId, string text)
    {
        #region docsnippet:connectors-errors
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
        #endregion
    }

    public void SlackClient(string botToken)
    {
        #region docsnippet:connectors-slack-client
        var slack = new Slack(botToken);
        #endregion

        Log.Instance.Debug($"{slack}");
    }

    public async Task SlackPostAsync(Slack slack, string rootTs)
    {
        #region docsnippet:connectors-slack-post
        var posted = await slack.PostAsync("C0123456789", "Deploy finished", threadTs: rootTs);
        #endregion

        Log.Instance.Debug($"{posted}");
    }

    public async Task SlackHistoryAsync(Slack slack, string channelId, string lastSeenTs)
    {
        #region docsnippet:connectors-slack-history
        var messages = await slack.HistorySinceAsync(channelId, oldestTs: lastSeenTs);

        foreach (var message in messages)
        {
            await ProcessAsync(message);
            lastSeenTs = message.Ts;   // safe: oldest-first means no gap on interruption
        }
        #endregion
    }

    public async Task SlackSocketAsync(Slack slack, string appToken)
    {
        #region docsnippet:connectors-slack-socket
        var wsUrl = await slack.OpenSocketUrlAsync(appToken);   // xapp-..., not the xoxb- bot token
        #endregion

        Log.Instance.Debug($"{wsUrl}");
    }

    public async Task GitHubAsync(string token)
    {
        #region docsnippet:connectors-github
        var gitHub = new GitHub(token);
        var issue = await gitHub.GetIssueAsync("ikon-ai/examples", 42);
        var commentUrl = await gitHub.CommentAsync("ikon-ai/examples", 42, "Reproduced on main.");
        #endregion

        Log.Instance.Debug($"{issue} {commentUrl}");
    }

    public async Task GitHubSinceAsync(GitHub gitHub, string cursor, HashSet<int> seenIssueNumbers)
    {
        #region docsnippet:connectors-github-since
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
        #region docsnippet:connectors-github-merge
        var result = await gitHub.MergePullRequestAsync("ikon-ai/examples", 42, commitTitle: "Add retry policy");

        if (!result.Merged)
        {
            Log.Instance.Warning($"PR #42 not merged: {result.Message}");
        }
        #endregion
    }

    public void GoogleClients(string clientId, string clientSecret, string refreshToken)
    {
        #region docsnippet:connectors-google-clients
        var credentials = new GoogleCredentials(clientId, clientSecret, refreshToken);
        using var drive = new Drive(credentials);
        using var gmail = new Gmail(credentials);
        #endregion
    }

    public async Task DriveTransferAsync(Drive drive, string folderId)
    {
        #region docsnippet:connectors-drive-transfer
        await using var content = File.OpenRead("./report.pdf");
        var uploaded = await drive.UploadAsync("report.pdf", "application/pdf", content, folderId);

        await using var download = await drive.DownloadAsync(uploaded.Id);
        #endregion
    }

    public async Task DriveListAsync(Drive drive, string folderId)
    {
        #region docsnippet:connectors-drive-list
        await foreach (var file in drive.ListAllAsync(folderId, extraQuery: "trashed = false"))
        {
            Log.Instance.Info($"{file.Name} ({file.MimeType}, modified {file.ModifiedTime:O})");
        }
        #endregion
    }

    public async Task GmailAsync(Gmail gmail, string bodyText)
    {
        #region docsnippet:connectors-gmail
        var unread = await gmail.ListAsync("is:unread", limit: 10);

        foreach (var email in unread)
        {
            var body = await gmail.GetBodyAsync(email.Id);
            Log.Instance.Info($"{email.From}: {email.Subject}");
        }

        var sentId = await gmail.SendAsync("someone@example.com", "Weekly summary", bodyText, cc: "team@example.com");
        #endregion

        Log.Instance.Debug($"{sentId}");
    }

    public async Task WebAgentAsync(AgentThread thread)
    {
        #region docsnippet:connectors-web-agent
        var run = await WebAgent.OperateAsync(
            thread,                                    // an AgentThread from Ikon.Agent
            "https://portal.example.com",
            "Log in with the provided credentials and extract the current account balance",
            new WebAgentOptions(MaxSteps: 25, Headless: true));

        if (run.Outcome == WebOutcome.Succeeded)
        {
            var balance = run.Outputs["balance"];
        }
        #endregion
    }

    public async Task BrowserSessionAsync()
    {
        #region docsnippet:connectors-browser-session
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
        #endregion

        Log.Instance.Debug($"{marks}");
    }

    public async Task ReplayAsync(WebRun run, string accountEmail, string accountPassword)
    {
        #region docsnippet:connectors-replay
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
        #endregion
    }
}
