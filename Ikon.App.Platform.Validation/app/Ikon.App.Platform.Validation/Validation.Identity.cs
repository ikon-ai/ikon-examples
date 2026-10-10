using System.Reflection;

public partial class Validation
{
    private ClientProfiles ClientProfiles { get; } = new(app);

    // Per-client identity, resolved from the backend profile when a client joins. Deferred login means
    // every visitor starts anonymous; these reflect that until (and after) they sign in on demand.
    private readonly ClientReactive<bool> _identityLoaded = new(false);
    private readonly ClientReactive<bool> _identityIsAnonymous = new(true);
    private readonly ClientReactive<string> _identityUserId = new("");
    private readonly ClientReactive<string> _identityEmail = new("");
    private readonly ClientReactive<string> _identityVisibleName = new("");
    private readonly ClientReactive<string> _identityRoles = new("");
    private readonly ClientReactive<bool> _identityIsSharedSession = new(false);
    private readonly ClientReactive<string> _identityAuthProvider = new("");
    private readonly ClientReactive<string> _identitySsoConnectionId = new("");
    private readonly ClientReactive<string> _ssoConnectionsResult = new("");

    // The message envelope's version and opcode are the same for every Context, so they say nothing about the client
    private static readonly PropertyInfo[] ContextProperties = typeof(Context)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetIndexParameters().Length == 0 && p.Name is not ("MessageVersion" or "MessageOpcode"))
        .ToArray();

    private async Task LoadIdentityAsync(Context clientContext)
    {
        int sessionId = clientContext.SessionId;
        _identityIsAnonymous.SetFor(sessionId, clientContext.IsAnonymous);
        _identityUserId.SetFor(sessionId, clientContext.UserId);
        _identityIsSharedSession.SetFor(sessionId, clientContext.IsSharedSession);
        _identityAuthProvider.SetFor(sessionId, clientContext.AuthProvider);
        _identitySsoConnectionId.SetFor(sessionId, clientContext.SsoConnectionId);

        try
        {
            var profile = await ClientProfiles.GetProfileAsync(clientContext);

            if (profile != null)
            {
                _identityEmail.SetFor(sessionId, profile.Email ?? "");
                _identityVisibleName.SetFor(sessionId, profile.VisibleName);
                _identityRoles.SetFor(sessionId, string.Join(", ", profile.Roles));
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Identity profile load failed: {ex.Message}");
        }
        finally
        {
            _identityLoaded.SetFor(sessionId, true);
        }
    }

    private void RenderSessionIdentitySection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Session Identity");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Session");
                RenderFieldGrid(view,
                    ("UserId", v => v.Text([Text.Body], app.SessionIdentity.UserId)),
                    ("Id", v => v.Text([Text.Body], app.SessionIdentity.Id)),
                    ("Session URL", v => v.Text([Text.Link, "truncate"], app.GlobalState.SessionUrl, href: app.GlobalState.SessionUrl)),
                    ("Shared session", v => v.Text([Text.Body], _identityIsSharedSession.Value.ToString(), props: TestId("identity-shared-session"))));
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Client Parameters");
                var clientParams = app.Clients[ReactiveScope.ClientId]?.Parameters;
                RenderFieldGrid(view,
                    ("Id", v => v.Text([Text.Body], clientParams?.Id ?? "")),
                    ("Test", v => v.Text([Text.Body], clientParams?.Test ?? "")));
            });

            // Every property, read by reflection so a field the platform adds shows up without an edit
            // here, and shown as-is on every run: comparing a local run with a deployment is the point.
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Client Context");
                app.ReactiveGlobalState.Clients.Value.TryGetValue(ReactiveScope.ClientId, out var clientContext);

                if (clientContext == null)
                {
                    view.Text([Text.Body], "—");
                    return;
                }

                RenderFieldGrid(view, ContextProperties
                    .Select(p => (p.Name, (Action<UIView>)(v => v.Text([Text.Body], FormatContextValue(p.GetValue(clientContext)),
                        props: TestId($"context-{NameConversions.ToKebabCase(p.Name)}")))))
                    .ToArray());
            });

        });
    }

    private static string Displayed(string value) => string.IsNullOrEmpty(value) ? "—" : value;

    private static string FormatContextValue(object? value) => value switch
    {
        null => "—",
        IDictionary<string, string> map => map.Count == 0 ? "—" : string.Join(", ", map.Select(kv => $"{kv.Key}={kv.Value}")),
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => Displayed(value.ToString() ?? ""),
    };

    private static void RenderFieldGrid(UIView view, params (string Label, Action<UIView> Value)[] rows)
    {
        view.ContentGrid(
            style: [ContentGrid.Bordered],
            columns:
            [
                new ContentGridColumn(null, "8rem"),
                new ContentGridColumn(null, Flex: 1),
            ],
            content: grid =>
            {
                foreach (var (label, value) in rows)
                {
                    grid.Box([ContentGrid.CellMuted], content: cell => cell.Text([Text.Body], label));
                    grid.Box([ContentGrid.Cell, "min-w-0 break-words"], content: value);
                }
            });
    }
}
