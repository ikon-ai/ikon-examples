using Dapper;

namespace Ikon.App.Patterns.Patterns;

// Pattern: invite-link-membership — see docs/patterns/invite-link-membership.md.
// The demo draws the owner's member screen from sample rows; its buttons write to the app's
// database, which only a running app has.
internal sealed class InviteLinkMembership : IPatternDemo
{
    public string Slug => "invite-link-membership";
    public string Title => "Invite links and members";
    public string Category => "Accounts";

    private const string SampleOwnerId = "user-aino";

    private readonly IAppBase app;

    public InviteLinkMembership(IAppBase app)
    {
        this.app = app;
        _members.AddRange(
        [
            new Member(SampleOwnerId, "Aino Virtanen", IsOwner: true),
            new Member("user-bruno", "Bruno Costa", IsOwner: false),
            new Member("user-chidi", "Chidi Okafor", IsOwner: false),
        ]);
        _invites.Add(new Invite("3f9c1a7e5b2d4c86a0e1f2b3c4d5e6f7", DateTime.UtcNow.AddDays(9)));
    }

    public void RenderDemo(IView view) => RenderMembers((UIView)view, SampleOwnerId);

    #region example:pattern-invite-link-membership
    private sealed record Member(string UserId, string Name, bool IsOwner);
    private sealed record Invite(string Code, DateTime ExpiresAt);

    // The rows are stored in two tables. These lists are what the screens render: LoadAsync fills them
    // from app.OnStarting, and every write below updates the table and then the list.
    private readonly ReactiveList<Member> _members = new();
    private readonly ReactiveList<Invite> _invites = new();
    private readonly ClientReactive<string?> _newLink = new((string?)null);

    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(14);

    private async Task LoadAsync()
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS members (user_id text PRIMARY KEY, name text NOT NULL, is_owner boolean NOT NULL);
            CREATE TABLE IF NOT EXISTS invites (code text PRIMARY KEY, expires_at timestamptz NOT NULL);
            """);
        _members.ReplaceAll(await db.QueryAsync<Member>("SELECT user_id AS UserId, name AS Name, is_owner AS IsOwner FROM members"));
        _invites.ReplaceAll(await db.QueryAsync<Invite>("SELECT code AS Code, expires_at AS ExpiresAt FROM invites WHERE expires_at > now()"));
    }

    private bool IsOwner(string userId) => _members.Any(m => m.UserId == userId && m.IsOwner);

    // The code is the secret that lets someone in, so it is random and long, never a readable name.
    // The link opens the app with ?invite=<code>, which binds to ClientParameters.Invite.
    private async Task<string> CreateInviteAsync(string userId)
    {
        if (!IsOwner(userId))
        {
            throw new UnauthorizedAccessException($"User {userId} is not the owner and cannot invite");
        }

        var invite = new Invite(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(), DateTime.UtcNow + InviteLifetime);

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("INSERT INTO invites (code, expires_at) VALUES (@Code, @ExpiresAt)", invite);
        _invites.Add(invite);
        return app.JoinUrl(new { invite = invite.Code });
    }

    // Call with the signed-in user and the code from ClientParameters.Invite. The expiry is checked
    // by the database in the same statement as the insert, so an invite cancelled or expired a moment
    // ago lets nobody in.
    private async Task<bool> JoinAsync(string userId, string name, string code)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            return true;
        }

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        var joined = await db.ExecuteAsync("""
            INSERT INTO members (user_id, name, is_owner)
            SELECT @userId, @name, false WHERE EXISTS (SELECT 1 FROM invites WHERE code = @code AND expires_at > now())
            ON CONFLICT (user_id) DO NOTHING
            """, new { userId, name, code });

        if (joined == 0)
        {
            return false;
        }

        _members.Add(new Member(userId, name, IsOwner: false));
        return true;
    }

    private async Task CancelInviteAsync(string userId, string code)
    {
        if (!IsOwner(userId))
        {
            return;
        }

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("DELETE FROM invites WHERE code = @code", new { code });
        _invites.RemoveAll(i => i.Code == code);
    }

    // Removing a member deletes their row; every screen that checks membership reads _members, so
    // the removed person loses access on their next render.
    private async Task RemoveMemberAsync(string userId, string memberId)
    {
        if (!IsOwner(userId) || memberId == userId)
        {
            return;
        }

        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("DELETE FROM members WHERE user_id = @memberId", new { memberId });
        _members.RemoveAll(m => m.UserId == memberId);
    }

    private void RenderMembers(UIView view, string userId)
    {
        var isOwner = IsOwner(userId);

        view.Column([Card.Default, "p-6 gap-4 max-w-lg"], content: view =>
        {
            view.Text([Text.H3], "Members");

            foreach (var member in _members)
            {
                view.Row([Layout.Row.SpaceBetween], key: member.UserId, content: row =>
                {
                    row.Text([Text.Body], member.IsOwner ? $"{member.Name} (owner)" : member.Name);

                    if (isOwner && !member.IsOwner)
                    {
                        row.Button([Button.GhostErrorSm], text: "Remove", onClick: async () => await RemoveMemberAsync(userId, member.UserId));
                    }
                });
            }

            if (!isOwner)
            {
                return;
            }

            view.Separator();
            view.Text([Text.Label], "Invite links");

            foreach (var invite in _invites.Where(i => i.ExpiresAt > DateTime.UtcNow))
            {
                view.Row([Layout.Row.SpaceBetween], key: invite.Code, content: row =>
                {
                    row.Text([Text.Caption], $"Expires {invite.ExpiresAt:d MMM}");
                    row.Button([Button.GhostSm], text: "Cancel link", onClick: async () => await CancelInviteAsync(userId, invite.Code));
                });
            }

            if (_newLink.Value is { } link)
            {
                view.ActionButton([Button.OutlineSm, "self-start"], text: "Copy the new link",
                    action: ActionKind.CopyToClipboard, options: new CopyToClipboardActionOptions { Text = link });
            }

            view.Button([Button.PrimarySm, "self-start"], text: "Create invite link",
                onClick: async () => _newLink.Value = await CreateInviteAsync(userId));
        });
    }
    #endregion
}
