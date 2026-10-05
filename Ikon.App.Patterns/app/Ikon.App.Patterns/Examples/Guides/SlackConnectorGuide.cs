using Ikon.Connectors;

namespace Ikon.App.Patterns.Examples;

file sealed class SlackConnectorGuideExamples
{
    private static Task ProcessAsync(SlackMessage message) => Task.CompletedTask;

    private static Task SaveBotTokenAsync(string teamId, string botToken) => Task.CompletedTask;

    public void SlackClient(string botToken)
    {
        #region example:connectors-slack-client
        var slack = new Slack(botToken);
        #endregion

        Log.Instance.Debug($"{slack}");
    }

    public async Task SlackPostAsync(Slack slack, string rootTs)
    {
        #region example:connectors-slack-post
        var posted = await slack.PostAsync("C0123456789", "Deploy finished", threadTs: rootTs);
        #endregion

        Log.Instance.Debug($"{posted}");
    }

    public async Task SlackHistoryAsync(Slack slack, string channelId, string lastSeenTs)
    {
        #region example:connectors-slack-history
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
        #region example:connectors-slack-socket
        var wsUrl = await slack.OpenSocketUrlAsync(appToken);   // xapp-..., not the xoxb- bot token
        #endregion

        Log.Instance.Debug($"{wsUrl}");
    }

    public async Task SlackInstallAsync(string clientId, string clientSecret, string redirectUri, string state, string code)
    {
        #region example:connectors-slack-install
        var installUrl = SlackAuth.AuthorizeUrl(clientId, ["channels:history", "chat:write"], redirectUri, state);

        // ... a workspace member installs the app; the redirect back carries `code`:
        var installation = await SlackAuth.ExchangeCodeAsync(clientId, clientSecret, code, redirectUri);
        await SaveBotTokenAsync(installation.TeamId, installation.BotToken);
        #endregion

        Log.Instance.Debug($"{installUrl}");
    }

    public bool SlackVerify(string signingSecret, string? timestampHeader, string? signatureHeader, string body)
    {
        #region example:connectors-slack-verify
        if (!SlackAuth.VerifyRequest(signingSecret, timestampHeader, signatureHeader, body))
        {
            return false;   // not Slack's, or replayed: answer 401 and do nothing
        }
        #endregion

        return true;
    }
}
