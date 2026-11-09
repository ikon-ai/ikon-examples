using Dapper;

namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: roster-email-match — see docs/patterns/roster-email-match.md.
// The demo draws the screen of a person whose sign-in email matched no roster row; the match
// needs a running app's database and a signed-in profile.
internal sealed class RosterEmailMatch(IAppBase app) : IPatternDemo
{
    public string Slug => "roster-email-match";
    public string Title => "Existing accounts carried over by email";
    public string Category => "Accounts";
    public void RenderDemo(IView view) => RenderWelcome((UIView)view);

    #region example:pattern-roster-email-match
    private ClientProfiles Profiles { get; } = new(app);

    // Per client: the roster row this person matched, or null when none did
    private readonly ClientReactive<string?> _rosterName = new((string?)null);

    /// <summary>Call once from Main.</summary>
    private void WireRosterMatch()
    {
        app.OnClientJoined(async ctx =>
        {
            // Only the email sign-in proves the address; Google or Apple may report one they did not verify
            if (ctx.IsAnonymous || ctx.AuthProvider != "email" || await Profiles.GetProfileAsync(ctx) is not { Email: { Length: > 0 } email })
            {
                return;
            }

            _rosterName.Value = await ClaimRosterRowAsync(ctx.UserId, email);
        });
    }

    // The import fills app_roster once (email, name, role) with user_id empty. The first sign-in with
    // the address claims the row; a second person with the same address, or a repeat join, claims nothing new.
    private async Task<string?> ClaimRosterRowAsync(string userId, string email)
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("CREATE TABLE IF NOT EXISTS app_roster (email text PRIMARY KEY, name text NOT NULL, role text NOT NULL, user_id text)");

        return await db.QuerySingleOrDefaultAsync<string>(
            "UPDATE app_roster SET user_id = @userId WHERE email = lower(@email) AND (user_id IS NULL OR user_id = @userId) RETURNING name",
            new { userId, email });
    }

    private void RenderWelcome(UIView view)
    {
        view.Column([Card.Default, "gap-3 p-4 max-w-md"], content: col =>
        {
            if (_rosterName.Value is { } name)
            {
                col.Text([Text.H3], $"Welcome back, {name}");
                col.Text([Text.Body], "Your account from the old system is linked");
                return;
            }

            col.Text([Text.H3], "We could not find you on the list");
            col.Text([Text.Body], "Sign in with the address your school or club has on file");
        });
    }
    #endregion
}
