namespace Ikon.App.Platform.Validation.Docs;

// The profiles-and-roles and costs guide sections.

#region docsnippet:profiles-attributes-type
public sealed class GameAttributes : IProfileAttributes
{
    public int HighScore { get; set; }
    public string FavouriteTrack { get; set; } = "";
}
#endregion

public sealed class ProfilesAndCostsDocs(IAppBase app)
{
    private ClientProfiles Profiles { get; } = new(app);

    #region docsnippet:profiles-read
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

    #region docsnippet:profiles-write
    public async Task SetPreferredNameAsync(Context clientContext, string preferredName)
    {
        // Only PreferredName is sent; every other field is left as it was.
        await Profiles.UpdateAsync(clientContext, data => data.PreferredName = preferredName);
    }
    #endregion

    #region docsnippet:profiles-attributes
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

    #region docsnippet:costs-total
    public async Task<double> CreditsThisMonthAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        return await app.Costs.GetTotalCreditsAsync(firstOfMonth, today, ct);
    }
    #endregion

    #region docsnippet:costs-daily
    public async Task<IReadOnlyList<DailyCost>> ImageCostsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var query = new CostQuery(from, to, Category: "image-generation");

        return await app.Costs.GetDailyCostsAsync(query, ct);
    }
    #endregion
}
