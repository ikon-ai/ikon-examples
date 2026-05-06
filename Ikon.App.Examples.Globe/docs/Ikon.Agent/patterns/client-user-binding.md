<!-- mined-from: Ikon.App.Bump -->
# Client ↔ User Binding — Push State Into the Right Browser

Multi-user apps with a shared server need to push state ("you got a match", "your match accepted") into a specific user's browser even when the trigger comes from somewhere else. Two `ConcurrentDictionary` maps + `ReactiveScope.Use(new ClientScope(...))` is enough.

## When to use

A server-side event happens for user B (incoming match, invite, message) and you need to update user B's `ClientReactive` state — but the code triggering it is running in user A's request scope or a background task with no scope at all.

## Snippet

```csharp
public partial class BumpApp
{
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
        using var _ = ReactiveScope.Use(new ClientScope(clientId));
        _activeMatchId.Value = matchId;
        _revealed.Value = false;
        _screen.Value = BumpScreen.BumpPresented;
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
}
```

## Notes

- Maintain both directions: `clientId -> userId` and `userId -> clientId`. You need to look up by either.
- Wire the binding in `app.ClientJoinedAsync` and clear in `app.ClientLeftAsync`.
- Always wrap cross-user state mutations in `using var _ = ReactiveScope.Use(new ClientScope(otherClientId))` — without the scope, `ClientReactive<T>` writes go nowhere visible.
- Treat "user not online" as an expected branch (TryGetValue → false).

## See also

- `multi-user-game`
- `role-based-screen-router`
