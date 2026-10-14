using Ikon.Common.Core.Protocol;

namespace Ikon.App.Patterns.Examples;

public sealed class ProfilesAndCostsExamples(IAppBase app)
{
    private ClientProfiles Profiles { get; } = new(app);

    #region example:profiles-custom-role
    private readonly ClientReactive<bool> _isReferee = new(false);

    public void ShowRefereeControls()
    {
        app.OnClientJoined(async ctx =>
        {
            // The join handler runs in the joining client's scope, so this sets the flag for that client alone
            var profile = await Profiles.GetProfileAsync(ctx);
            _isReferee.Value = profile?.HasRole("referee") == true;
        });
    }
    #endregion

    #region example:profiles-grant-by-email
    public async Task<string> MakeRefereeAsync(string email)
    {
        // Works before the person has ever opened the app: they get the role when they first sign in
        bool holdsItNow = await Profiles.AddRoleByEmailAsync(email, "referee");

        return holdsItNow ? $"{email} is a referee" : $"{email} becomes a referee on first sign-in";
    }
    #endregion

    #region example:profiles-read
    public async Task<string> GreetAsync(Context clientContext)
    {
        var profile = await Profiles.GetProfileAsync(clientContext);

        if (profile is null)
        {
            return "Welcome";
        }

        // VisibleName is empty until the person sets a preferred or first name
        var name = profile.VisibleName is { Length: > 0 } visibleName ? visibleName : profile.Name ?? profile.Email ?? "there";

        return profile.HasRole(UserRole.Admin)
            ? $"Welcome back, {name} (admin)"
            : $"Welcome back, {name}";
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

    #region example:costs-team-budget
    public async Task<string> AskForTeamAsync(string teamId, double monthlyCredits, string question, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        // What the team spent this month: the usage of every call made under its scope.
        var spent = await app.Costs.GetCreditsForScopeAsync("team", teamId, firstOfMonth, today, ct);

        // Checked here because a Budget denies a call only when the platform's spend mode is enforce.
        if (spent >= monthlyCredits)
        {
            return "Your team has used this month's AI budget.";
        }

        // Everything below is attributed to the team, and this one operation is capped at what is left.
        using var team = Log.Instance.UseScope(new CustomScope("team", teamId));
        using var budget = app.Costs.Budget("team-question", monthlyCredits - spent);

        return await Emerge.AskAsync(question, ct);
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
