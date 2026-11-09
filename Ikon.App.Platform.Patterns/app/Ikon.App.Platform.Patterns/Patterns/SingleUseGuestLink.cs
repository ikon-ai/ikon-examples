using Dapper;

namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: single-use-guest-link — see docs/patterns/single-use-guest-link.md.
// The demo draws the staff screen from sample rows. A running app writes the same rows to its
// guest_links table at start, but only while the table is empty, so "Create a link" has a table to
// insert into and a link someone claimed stays claimed. The render smoke test never starts the app
// and draws the rows from memory.
internal sealed class SingleUseGuestLink : IPatternDemo
{
    public string Slug => "single-use-guest-link";
    public string Title => "Single-use guest link";
    public string Category => "Accounts";

    private readonly IAppBase app;

    private static GuestLink[] SampleLinks() =>
    [
        new GuestLink("5d0c7b1e9a3f4e28b6c1d2e3f4a5b6c7", "Maija Korhonen", DateTime.UtcNow.AddDays(2), UserId: null),
        new GuestLink("a1b2c3d4e5f60718293a4b5c6d7e8f90", "Tomas Weber", DateTime.UtcNow.AddDays(1), UserId: "user-guest-7"),
    ];

    public SingleUseGuestLink(IAppBase app)
    {
        this.app = app;
        _links.AddRange(SampleLinks());
        app.OnStarting(SeedOnceAsync);
    }

    public void RenderDemo(IView view) => RenderLinks((UIView)view);

    // Every visitor shares the gallery's database, so nothing here overwrites a row already there.
    // ON CONFLICT keeps two instances starting at once from failing.
    private async Task SeedOnceAsync()
    {
        if (SnapshotCapture.IsCaptureProcess)
        {
            return;
        }

        try
        {
            await LoadAsync();

            var links = SampleLinks();

            await using (var db = await app.DatabaseAsync())
            {
                await db.OpenAsync();
                await db.ExecuteAsync("""
                    INSERT INTO guest_links (code, name, expires_at, user_id)
                    SELECT * FROM unnest(@codes, @names, @expiresAts, @userIds)
                    WHERE NOT EXISTS (SELECT 1 FROM guest_links)
                    ON CONFLICT (code) DO NOTHING
                    """,
                    new
                    {
                        codes = links.Select(l => l.Code).ToArray(),
                        names = links.Select(l => l.Name).ToArray(),
                        expiresAts = links.Select(l => l.ExpiresAt).ToArray(),
                        userIds = links.Select(l => l.UserId).ToArray(),
                    });
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            // The staff screen still draws from the sample rows; only "Create a link" needs the
            // table. Rows a load already read are the real ones and stay.
            if (_links.Count == 0)
            {
                _links.ReplaceAll(SampleLinks());
            }

            Log.Instance.Warning($"Seeding the single-use-guest-link demo's guest_links table failed, so its sample rows show and \"Create a link\" fails: {ex.Message}");
        }
    }

    #region example:pattern-single-use-guest-link
    private sealed record GuestLink(string Code, string Name, DateTime ExpiresAt, string? UserId);

    private readonly ReactiveList<GuestLink> _links = new();
    private readonly ClientReactive<string?> _newLink = new((string?)null);

    private static readonly TimeSpan LinkLifetime = TimeSpan.FromDays(3);

    private async Task LoadAsync()
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS guest_links (
                code text PRIMARY KEY, name text NOT NULL, expires_at timestamptz NOT NULL, user_id text NULL);
            """);
        _links.ReplaceAll(await db.QueryAsync<GuestLink>("SELECT code AS Code, name AS Name, expires_at AS ExpiresAt, user_id AS UserId FROM guest_links"));
    }

    // Only a staff screen calls this; check the staff role there (profiles-and-roles).
    // The link opens the app with ?guest=<code>, which binds to ClientParameters.Guest.
    private async Task<string> CreateLinkAsync(string guestName)
    {
        var link = new GuestLink(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(), guestName, DateTime.UtcNow + LinkLifetime, UserId: null);

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("INSERT INTO guest_links (code, name, expires_at) VALUES (@Code, @Name, @ExpiresAt)", link);
        _links.Add(link);
        return app.JoinUrl(new { guest = link.Code });
    }

    // Call with ReactiveScope.UserId and the code from ClientParameters.Guest. The one UPDATE
    // claims the link only while nobody holds it and it has not expired, so two browsers opening
    // it together cannot both get in. A guest keeps the same user id in one browser, so the
    // claimant keeps access on a reload when the instance it reaches holds the claim in _links:
    // the instance that took it, or one that has run LoadAsync since.
    private async Task<bool> RedeemAsync(string userId, string code)
    {
        if (HasAccess(userId))
        {
            return true;
        }

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        var claimed = await db.ExecuteAsync("""
            UPDATE guest_links SET user_id = @userId
            WHERE code = @code AND user_id IS NULL AND expires_at > now()
            """, new { userId, code });

        if (claimed == 0)
        {
            return false;
        }

        _links.ReplaceWhere(l => l.Code == code, l => l with { UserId = userId });
        return true;
    }

    // Every screen and handler the guest may use asks this, not only the page the link opens.
    private bool HasAccess(string userId) => _links.Any(l => l.UserId == userId);

    private static string StatusOf(GuestLink link) =>
        link.UserId is not null ? "Used" : link.ExpiresAt <= DateTime.UtcNow ? "Expired" : "Waiting";

    private void RenderLinks(UIView view)
    {
        view.Column([Card.Default, "p-6 gap-4 max-w-lg"], content: view =>
        {
            view.Text([Text.H3], "Guest links");

            foreach (var link in _links)
            {
                view.Row([Layout.Row.SpaceBetween], key: link.Code, content: row =>
                {
                    row.Text([Text.Body], link.Name);
                    row.Badge(StatusOf(link));
                });
            }

            if (_newLink.Value is { } url)
            {
                view.ActionButton([Button.OutlineSm, "self-start"], text: "Copy the new link",
                    action: ActionKind.CopyToClipboard, options: new CopyToClipboardActionOptions { Text = url });
            }

            view.Button([Button.PrimarySm, "self-start"], text: "Create a link",
                onClick: async () => _newLink.Value = await CreateLinkAsync("New guest"));
        });
    }
    #endregion
}
