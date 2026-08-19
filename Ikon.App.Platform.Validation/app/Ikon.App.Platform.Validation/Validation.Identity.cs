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

    private async Task LoadIdentityAsync(Context clientContext)
    {
        int sessionId = clientContext.SessionId;
        _identityIsAnonymous.SetFor(sessionId, clientContext.IsAnonymous);
        _identityUserId.SetFor(sessionId, clientContext.UserId);

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

    private async Task TriggerLoginAsync(string provider)
    {
        try
        {
            await ClientFunctions.LoginAsync(provider);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Sign-in trigger failed: {ex.Message}");
        }
    }

    private void RenderIdentitySection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Session Identity");
                RenderFieldGrid(view,
                    ("UserId", v => v.Text([Text.Body], app.SessionIdentity.UserId)),
                    ("Id", v => v.Text([Text.Body], app.SessionIdentity.Id)),
                    ("Session URL", v => v.Text([Text.Link, "truncate"], app.GlobalState.SessionUrl, href: app.GlobalState.SessionUrl)));
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Client Parameters");
                var clientParams = app.Clients[ReactiveScope.ClientId]?.Parameters;
                RenderFieldGrid(view,
                    ("Id", v => v.Text([Text.Body], clientParams?.Id ?? "")),
                    ("Test", v => v.Text([Text.Body], clientParams?.Test ?? "")));
            });

            RenderDeferredLoginBox(view);
        });
    }

    private void RenderDeferredLoginBox(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Deferred Login");

            bool isAnonymous = _identityIsAnonymous.Value;

            var rows = new List<(string Label, Action<UIView> Value)>
            {
                ("Anonymous (guest)", v => v.Text([Text.Body], isAnonymous.ToString())),
                ("User ID", v => v.Text([Text.Body], _identityUserId.Value)),
            };

            if (_identityLoaded.Value)
            {
                rows.Add(("Name", v => v.Text([Text.Body], Displayed(_identityVisibleName.Value))));
                rows.Add(("Email", v => v.Text([Text.Body], Displayed(_identityEmail.Value))));
                rows.Add(("Roles", v => v.Text([Text.Body], Displayed(_identityRoles.Value))));
            }

            RenderFieldGrid(view, rows.ToArray());

            if (!_identityLoaded.Value)
            {
                view.Box(["mt-3"], content: b => b.Spinner());
            }

            if (isAnonymous)
            {
                view.Text([Text.Caption, "mt-4"], "You are browsing as a guest. Sign in on demand:");
                view.Row(["flex-wrap gap-3 mt-2"], content: row =>
                {
                    row.Button([Button.PrimaryMd], text: "Sign in with Google", onClick: () => TriggerLoginAsync("google"));
                    row.Button([Button.SolidMd], text: "Sign in with Microsoft", onClick: () => TriggerLoginAsync("microsoft"));
                    row.Button([Button.OutlineMd], text: "Show login UI",
                        onClick: async () => await ClientFunctions.LoginShowAsync("Sign in to see your full profile"));
                });
            }
            else
            {
                view.Text([Text.Caption, "mt-4"], "You are signed in.");
                view.Button([Button.ErrorMd, "mt-2"], text: "Log out", onClick: async () => await ClientFunctions.LogoutAsync());
            }
        });
    }

    private static string Displayed(string value) => string.IsNullOrEmpty(value) ? "—" : value;

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
