<!-- mined-from: Ikon.App.Examples.Kahoot -->
# Host And Player Dual Roles — One App, Two Completely Different UIs

A single Ikon app serves two visually distinct roles by branching off a `host=true` query param in `ClientParams`. Host clients see the big-screen game master view (QR code, leaderboard, projector-friendly text); players see the small-screen tap-to-answer view. Both share the same `Reactive` state but render different trees.

## When to use

Multiplayer experiences where one device is a "stage" and others are controllers (party games, classroom quizzes, conference apps, presenter/audience tools). One app instance handles both sides — no separate frontend needed.

## Snippet

```csharp
public record ClientParams(string Id = "", bool Host = false);

public class Kahoot(IApp<SessionIdentity, ClientParams> app)
{
    private bool IsHost()
    {
        var client = app.Clients[ReactiveScope.ClientId];
        return client?.Parameters.Host == true;
    }

    private string GetJoinUrl()
        => app.JoinUrl(new { id = app.SessionIdentity.Id });

    private string GetCreateSessionUrl()
        => app.JoinUrl(new { id = GenerateSessionId(), host = "true" });

    private void RenderUI(UIView view)
    {
        var clientId = ReactiveScope.ClientId;
        var client = app.Clients[clientId];

        if (!IsValidSessionId(client?.Parameters.Id))
        {
            RenderCreateSession(view);
            return;
        }

        if (IsHost())
        {
            RenderHostView(view);
        }
        else
        {
            RenderPlayerView(view);
        }
    }

    private void RenderHostLobby(UIView view)
    {
        view.QR(style: ["w-64 h-64 bg-white p-3 rounded-2xl"], value: GetJoinUrl(), size: 400);
        view.Text(style: ["text-2xl font-bold"], text: "Scan to join!");
    }
}
```

## Notes

- Only the host clicks "Start Game" — same `Reactive<GameStage>` updates both views simultaneously.
- The session id (`?id=abcdef12`) is just an 8-char hex string; same id on host + player URL keeps them in the same app instance.
- `ReactiveScope.ClientId` lets per-client state (like `_selectedAnswer`) stay scoped to the player who tapped.
- Hosts often want fullscreen — pair with `ActionKind.RequestFullscreen` on the player "Let's go" button.

## See also

- `multi-user-game`
- `typical-app-structure`
