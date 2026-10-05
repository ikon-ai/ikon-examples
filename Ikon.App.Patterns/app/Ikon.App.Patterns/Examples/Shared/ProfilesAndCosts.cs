using Ikon.Common.Core.Protocol;

namespace Ikon.App.Patterns.Examples;

public sealed class ProfilesAndCostsExamples(IAppBase app)
{
    private ClientProfiles Profiles { get; } = new(app);

    #region example:profiles-read
    public async Task<string> GreetAsync(Context clientContext)
    {
        var profile = await Profiles.GetProfileAsync(clientContext);

        if (profile is null)
        {
            return "Welcome, guest";
        }

        return profile.HasRole(UserRole.Admin)
            ? $"Welcome back, {profile.VisibleName} (admin)"
            : $"Welcome back, {profile.VisibleName}";
    }
    #endregion

    #region example:profiles-write
    public async Task SetPreferredNameAsync(Context clientContext, string preferredName)
    {
        // Only PreferredName is sent; every other field is left as it was.
        await Profiles.UpdateAsync(clientContext, data => data.PreferredName = preferredName);
    }
    #endregion

    #region example:profiles-attributes
    public async Task RecordScoreAsync(Context clientContext, int score)
    {
        var attributes = await Profiles.GetAttributesAsync<GameAttributes>(clientContext)
            ?? new GameAttributes();

        if (score > attributes.HighScore)
        {
            attributes.HighScore = score;
            await Profiles.SetAttributesAsync(clientContext, attributes);
        }
    }
    #endregion

    #region example:costs-total
    public async Task<double> CreditsThisMonthAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        return await app.Costs.GetTotalCreditsAsync(firstOfMonth, today, ct);
    }
    #endregion

    #region example:costs-daily
    public async Task<IReadOnlyList<DailyCost>> ImageCostsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var query = new CostQuery(from, to, Category: "image-generation");

        return await app.Costs.GetDailyCostsAsync(query, ct);
    }
    #endregion

    #region example:costs-budget
    public async Task<IReadOnlyList<string>> SummariseAsync(IReadOnlyList<string> documents, CancellationToken ct)
    {
        // The loop's bound: the whole batch stops at 5 credits, however long the documents are.
        using var budget = app.Costs.Budget("summaries", maxCredits: 5);
        var summaries = new List<string>();

        try
        {
            foreach (var document in documents)
            {
                summaries.Add(await Emerge.AskAsync($"Summarise in one sentence:\n{document}", ct));
            }
        }
        catch (Exception ex) when (SpendLimitExceededException.Find(ex) is { } denied)
        {
            // A denied call is a budget state to show, never one to retry into.
            summaries.Add(denied.Breach?.ResetsAt is { } resetsAt
                ? $"Paused until {resetsAt:HH:mm} UTC: spend limit reached"
                : "Paused: this batch's credit budget is spent");
        }

        return summaries;
    }
    #endregion

    #region example:costs-spend-status
    public async Task<bool> AiAvailableAsync(CancellationToken ct)
    {
        var status = await app.Costs.GetSpendStatusAsync(ct);

        return status.Mode != "enforce" || status.Breaches.Count == 0;
    }
    #endregion
}
