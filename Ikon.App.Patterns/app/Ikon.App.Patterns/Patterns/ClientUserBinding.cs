namespace Ikon.App.Patterns.Patterns;

// Pattern: client-user-binding — see docs/patterns/client-user-binding.md.
// BumpScreen stands in for the app's real screen enum the bound state drives.
internal sealed class ClientUserBinding : IPatternDemo
{
    public string Slug => "client-user-binding";
    public string Title => "Client user binding";
    public string Category => "State";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: maintains a two-way client-id/user-id binding so per-client reactive state can be routed to a specific user from anywhere. See the source and docs/patterns/client-user-binding.md.");

    private enum BumpScreen { Register, BumpPresented }

    #region docsnippet:pattern-client-user-binding
    private readonly ConcurrentDictionary<int, string> _clientToUser = new();
    private readonly ConcurrentDictionary<string, int> _userToClient = new();

    private readonly ClientReactive<BumpScreen> _screen = new(BumpScreen.Register);
    private readonly ClientReactive<string?> _activeMatchId = new(null);
    private readonly ClientReactive<bool> _revealed = new(false);

    private void BindClientToUser(int clientId, string userId)
    {
        _clientToUser[clientId] = userId;
        _userToClient[userId] = clientId;
    }

    private string? CurrentUserId()
    {
        var clientId = ReactiveScope.ClientId;
        return _clientToUser.TryGetValue(clientId, out var userId) ? userId : null;
    }

    private void PostCardToClient(int clientId, string matchId)
    {
        // Several writes to the same client — scope the region once.
        using var _ = ReactiveScope.Use(new ClientScope(clientId));
        _activeMatchId.Value = matchId;
        _revealed.Value = false;
        _screen.Value = BumpScreen.BumpPresented;
    }

    private void RevealTo(int clientId)
    {
        // A single write needs no scope at all — name the client.
        _revealed.SetFor(clientId, true);
    }

    private void SendClientTo(string userId, string matchId, BumpScreen target)
    {
        if (!_userToClient.TryGetValue(userId, out var clientId))
        {
            return;
        }

        using var _ = ReactiveScope.Use(new ClientScope(clientId));
        _activeMatchId.Value = matchId;
        _screen.Value = target;
    }
    #endregion
}
