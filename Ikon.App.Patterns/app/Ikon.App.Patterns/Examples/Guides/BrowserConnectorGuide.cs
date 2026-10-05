using Ikon.Connectors.Browser;

namespace Ikon.App.Patterns.Examples;

file sealed class BrowserConnectorGuideExamples
{
    public async Task WebAgentAsync(AgentThread thread)
    {
        #region example:connectors-web-agent
        var run = await WebAgent.OperateAsync(
            thread,                                    // an AgentThread from Ikon.Agent
            "https://portal.example.com",
            "Log in with the provided credentials and extract the current account balance",
            new WebAgentOptions(PublicInternetOnly: true, MaxPasses: 25, Headless: true));

        if (run.Outcome == WebOutcome.Succeeded)
        {
            var balance = run.Outputs["balance"];
        }
        #endregion
    }

    public async Task WebAgentReviewAsync(AgentThread thread, Func<string, byte[], CancellationToken, Task<bool>> askPerson, Reactive<byte[]?> liveView)
    {
        #region example:connectors-web-agent-review
        var run = await WebAgent.OperateAsync(
            thread,
            "https://supplier.example.com/orders",
            "Reorder last month's printer paper",
            new WebAgentOptions(
                PublicInternetOnly: true,
                MaxPasses: 40,
                ReviewWrite: async (review, ct) => await askPerson(review.Description, review.Screenshot, ct)
                    ? WebApproval.Allow
                    : WebApproval.Deny("the person declined it"),
                OnProgress: progress =>
                {
                    liveView.Value = progress.Screenshot;
                    return Task.CompletedTask;
                }));
        #endregion

        Log.Instance.Debug($"{run.Outcome}");
    }

    public async Task BrowserSessionAsync()
    {
        #region example:connectors-browser-session
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

    public async Task SavedDetailAsync(IDetailVault detailVault)
    {
        #region example:connectors-saved-detail
        await using var session = new BrowserSession { Details = detailVault };
        await session.StartAsync(headless: true);
        await session.NavigateAsync("https://shop.example/checkout");

        var filled = await session.ExecuteAsync(
            new WebAction.FillDetail(new WebTarget(Role: "textbox", Name: "Card number"), "personal-visa", "number"));
        #endregion

        Log.Instance.Debug($"{filled.Ok}");
    }

    public async Task ReplayAsync(WebRun run, string accountEmail, string accountPassword)
    {
        #region example:connectors-replay
        var flow = WebAgent.Distill(run, name: "portal-balance");
        // ... persist flow (it serializes losslessly), later:
        var replay = await WebAgent.ReplayAsync(flow, new Dictionary<string, string>
        {
            ["email"] = accountEmail,
            ["password"] = accountPassword,
        }, headless: true, publicInternetOnly: true);   // as the run it was distilled from

        if (replay.Ok)
        {
            var balance = replay.Outputs["balance"];
        }
        #endregion
    }
}
