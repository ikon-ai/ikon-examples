using Dapper;

namespace Ikon.App.Patterns.Patterns;

// Pattern: first-user-becomes-admin — see docs/patterns/first-user-becomes-admin.md.
// The demo draws the admin page as a person who is not an admin sees it; the claim needs a
// running app's database and profiles.
internal sealed class FirstUserBecomesAdmin(IAppBase app) : IPatternDemo
{
    public string Slug => "first-user-becomes-admin";
    public string Title => "Admin-only screens and the first admin";
    public string Category => "Accounts";
    public void RenderDemo(IView view) => RenderAdminPage((UIView)view);

    #region example:pattern-first-user-becomes-admin
    private ClientProfiles Profiles { get; } = new(app);

    private readonly ClientReactive<bool> _isAdmin = new(false);
    // Per user, so a reload keeps the typed address
    private readonly UserReactive<string> _newAdminEmail = new("");
    private readonly ClientReactive<string?> _adminMessage = new((string?)null);

    // Once the app has an owner nobody can claim it again, so later joins skip the database
    private volatile bool _ownerClaimed;

    /// <summary>Call once from Main.</summary>
    private void WireAdminRole()
    {
        app.OnClientJoined(async ctx =>
        {
            // A guest, or a local run nobody signed in to, has no profile and never claims the app
            if (ctx.IsAnonymous || await Profiles.GetProfileAsync(ctx) is not { } profile)
            {
                return;
            }

            var isAdmin = profile.HasRole(UserRole.Admin);

            // An admin who arrives first claims the app too, so nobody after them can
            if (!_ownerClaimed && await ClaimOwnershipAsync(ctx.UserId) && !isAdmin)
            {
                await Profiles.AddRoleAsync(ctx, UserRole.Admin);
                isAdmin = true;
            }

            // The join handler runs in the joining client's scope, so this is that client's flag
            _isAdmin.Value = isAdmin;
        });
    }

    // The first signed-in person to open the app claims it. The table holds one row under a fixed
    // key, so a second insert, even one racing the first, conflicts and claims nothing.
    private async Task<bool> ClaimOwnershipAsync(string userId)
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("CREATE TABLE IF NOT EXISTS app_owner (id int PRIMARY KEY CHECK (id = 1), user_id text NOT NULL, claimed_at timestamptz NOT NULL DEFAULT now())");
        var claimed = await db.ExecuteAsync("INSERT INTO app_owner (id, user_id) VALUES (1, @userId) ON CONFLICT (id) DO NOTHING", new { userId }) == 1;
        _ownerClaimed = true;
        return claimed;
    }

    private async Task AddAdminAsync()
    {
        // The screen hides the form from everyone else, but this is the check that counts:
        // a handler can be reached without the screen that draws its button
        if (!_isAdmin.Value || _newAdminEmail.Value.Trim() is not { Length: > 0 } email)
        {
            return;
        }

        // Works before the person has opened the app: they become an admin when they first sign in
        var holdsItNow = await Profiles.AddRoleByEmailAsync(email, UserRole.Admin);
        _adminMessage.Value = holdsItNow ? $"{email} is an admin" : $"{email} becomes an admin on first sign-in";
        _newAdminEmail.Value = "";
    }

    private void RenderAdminPage(UIView view)
    {
        view.Column([Card.Default, "gap-3 p-4 max-w-md"], content: col =>
        {
            col.Text([Text.H3], "Admins");

            if (!_isAdmin.Value)
            {
                col.Text([Text.Body], "Only the app's admins can open this page");
                return;
            }

            col.TextField(type: "email", label: "Make someone an admin", placeholder: "name@example.com", bind: _newAdminEmail);
            col.Button([Button.PrimaryMd, "self-start"], text: "Add admin", onClick: AddAdminAsync);

            if (_adminMessage.Value is { } message)
            {
                col.Text([Text.Caption], message);
            }
        });
    }
    #endregion
}
