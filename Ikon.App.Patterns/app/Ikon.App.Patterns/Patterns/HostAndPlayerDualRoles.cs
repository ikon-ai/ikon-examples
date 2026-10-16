namespace Ikon.App.Patterns.Patterns;

// Pattern: host-and-player-dual-roles — see docs/patterns/host-and-player-dual-roles.md.
// The example region is the canonical body the doc extracts. In an app the class is the scaffold's
// [App] partial. Here the attribute is left off, so runtime discovery finds only this app's real
// [App] type. The stubs outside the region stand in for the two role
// views and the session-id helpers the branch logic calls.
internal sealed class HostAndPlayerDualRoles : IPatternDemo
{
    public string Slug => "host-and-player-dual-roles";
    public string Title => "Host and player dual roles";
    public string Category => "Realtime";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Multi-surface pattern with no self-contained demo: one app serves a host view and a player view from the same session, branching on whether a client's link carries the session's host code. See the source and docs/patterns/host-and-player-dual-roles.md.");

    public sealed record SessionIdentity(string Id);

    private static string GenerateSessionId() => throw new NotImplementedException();
    private static bool IsValidSessionId(string? id) => throw new NotImplementedException();
    private static void RenderCreateSession(UIView view) => throw new NotImplementedException();
    private static void RenderHostView(UIView view) => throw new NotImplementedException();
    private static void RenderPlayerView(UIView view) => throw new NotImplementedException();

    #region example:pattern-host-and-player-dual-roles
    public record ClientParams(string Id = "", string HostCode = "");

    public partial class KahootApp(IApp<SessionIdentity, ClientParams> app)
    {
        // The code the host's link carried when the session started. Players never see it, so a
        // player cannot make themselves the host by editing their link.
        private readonly Reactive<string?> _hostCode = new(null);

        /// <summary>Call once from Main.</summary>
        private void WireHostCode()
        {
            // The first client in a new session is the host who created it, because players can
            // only learn the session id from the host's screen.
            app.OnClientJoined((ctx, parameters) =>
            {
                if (_hostCode.Value is null && parameters.HostCode.Length > 0)
                {
                    _hostCode.Value = parameters.HostCode;
                }

                return Task.CompletedTask;
            });
        }

        private bool IsHost()
        {
            var code = app.Clients[ReactiveScope.ClientId]?.Parameters.HostCode;
            return !string.IsNullOrEmpty(code) && code == _hostCode.Value;
        }

        private string GetJoinUrl()
            => app.JoinUrl(new { id = app.SessionIdentity.Id });

        private string GetCreateSessionUrl()
            => app.JoinUrl(new { id = GenerateSessionId(), hostCode = Guid.NewGuid().ToString("N") });

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
