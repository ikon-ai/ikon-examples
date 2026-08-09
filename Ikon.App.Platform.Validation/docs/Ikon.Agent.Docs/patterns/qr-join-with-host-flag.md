<!-- mined-from: Ikon.App.Examples.Kahoot -->
# QR Join With Host Flag — Big-Screen QR, Tap-To-Join Players

A landing page (no session id) shows a "Start Game!" button that links to `?id=<random8hex>&host=true`. The host's screen then renders a QR code containing the same id without `host=true`; phones that scan join as players. One URL space, one app instance, two roles disambiguated entirely by query params.

## When to use

Conference demos, classroom interactives, party games — anywhere a TV/projector is the controller and phones are the participants. The QR sticker pattern means players never type a URL.

## Snippet

```csharp
public record ClientParams(string Id = "", bool Host = false);

private static bool IsValidSessionId(string? id)
{
    if (string.IsNullOrEmpty(id) || id.Length != 8) return false;
    return id.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
}

private static string GenerateSessionId() => Guid.NewGuid().ToString("N")[..8];

private string GetJoinUrl()
    => app.JoinUrl(new { id = app.SessionIdentity.Id });

private string GetCreateSessionUrl()
    => app.JoinUrl(new { id = GenerateSessionId(), host = "true" });

private void RenderCreateSession(UIView view)
{
    view.Column(style: ["w-full h-screen items-center justify-center gap-6"], content: view =>
    {
        view.Text(style: ["text-7xl font-bold"], text: "Ikon Kahoot");
        view.Button(
            style: ["px-8 py-4 text-xl rounded-full border border-white/30"],
            href: GetCreateSessionUrl(),
            content: v =>
            {
                v.Row(style: ["items-center gap-3"], content: v =>
                {
                    v.Icon(["w-5 h-5"], name: "play");
                    v.Text(text: "Start Game!");
                });
            });
    });
}

private void RenderHostLobby(UIView view)
{
    view.QR(
        style: ["w-64 h-64 bg-white p-3 rounded-2xl"],
        value: GetJoinUrl(),
        size: 400);

    view.Text(style: ["text-2xl font-bold mt-2"], text: "Scan to join!");
    view.Button(
        style: [Button.GhostMd, "text-sm"],
        text: "Open player view",
        href: GetJoinUrl(),
        target: "_blank");
}
```

## Notes

- 8-char hex (first 8 of a GUID) is short enough for fast QR scanning, long enough to avoid collisions in practice.
- Validating the session id before routing prevents stray hits to `/?id=foo` from joining real games.
- Add a "Open player view" link next to the QR for testing without a phone.
- The session identity (`app.SessionIdentity.Id`) is the *same* id; only the `host=true` flag differs between the two URLs.

## See also

- `host-and-player-dual-roles`
- `multi-user-game`
