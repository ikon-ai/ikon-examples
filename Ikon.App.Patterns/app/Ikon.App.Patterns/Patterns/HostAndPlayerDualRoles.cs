namespace Ikon.App.Patterns.Patterns;

// Pattern: host-and-player-dual-roles — see docs/patterns/host-and-player-dual-roles.md.
// The docsnippet region is the canonical body the doc extracts. The mined original decorated the
// class with [App]; here it stays a plain nested example class so it never competes with this app's
// real [App] type for runtime discovery. The stubs outside the region stand in for the two role
// views and the session-id helpers the branch logic calls.
internal sealed class HostAndPlayerDualRoles : IPatternDemo
{
    public string Slug => "host-and-player-dual-roles";
    public string Title => "Host and player dual roles";
    public string Category => "Realtime";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Multi-surface pattern with no self-contained demo: one app serves a host view and a player view from the same session, branching on a client's Host parameter. See the source and docs/patterns/host-and-player-dual-roles.md.");

    public sealed record SessionIdentity(string Id);

    private static string GenerateSessionId() => throw new NotImplementedException();
    private static bool IsValidSessionId(string? id) => throw new NotImplementedException();
    private static void RenderCreateSession(UIView view) => throw new NotImplementedException();
    private static void RenderHostView(UIView view) => throw new NotImplementedException();
    private static void RenderPlayerView(UIView view) => throw new NotImplementedException();

    #region docsnippet:pattern-host-and-player-dual-roles
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
    #endregion
}
