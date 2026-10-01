using System.Globalization;

public partial class Validation
{
    private readonly ClientReactive<string> _accountRemovalScheduledFor = new("not read yet");
    private readonly ClientReactive<bool> _accountRemovalConfirmOpen = new(false);
    private readonly ClientReactive<string> _accountRemovalResult = new("");

    private void RenderAccountSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Account");
            RenderSignInCard(view);
            RenderSsoConnectionsCard(view);
            RenderAccountCard(view);
        });
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

    private void RenderSignInCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Sign-in");

            bool isAnonymous = _identityIsAnonymous.Value;

            var rows = new List<(string Label, Action<UIView> Value)>
            {
                ("Anonymous (guest)", v => v.Text([Text.Body], isAnonymous.ToString())),
                ("User ID", v => v.Text([Text.Body], _identityUserId.Value)),
                ("Signed in with", v => v.Text([Text.Body], Displayed(_identityAuthProvider.Value), props: TestId("identity-auth-provider"))),
                ("SSO connection", v => v.Text([Text.Body], Displayed(_identitySsoConnectionId.Value), props: TestId("identity-sso-connection"))),
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
                view.Row(["flex-wrap gap-3 mt-4"], content: row =>
                {
                    row.Button([Button.PrimaryMd], text: "Sign in with Google", onClick: () => TriggerLoginAsync("google"));
                    row.Button([Button.PrimaryMd], text: "Sign in with Microsoft", onClick: () => TriggerLoginAsync("microsoft"));
                    row.Button([Button.PrimaryMd], text: "Show login UI",
                        onClick: async () => await ClientFunctions.LoginShowAsync("Sign in to see your full profile"));
                });
            }
            else
            {
                view.Button([Button.ErrorMd, "mt-4"], text: "Log out", onClick: async () => await ClientFunctions.LogoutAsync());
            }

            // The app's own legal pages, which the platform serves from legal/*.md at the [Legal] paths.
            // rel="external" makes the browser load the page instead of the app routing the link.
            view.Row(["flex-wrap gap-x-2 items-center mt-4"], content: row =>
            {
                row.Text([Text.Link, "text-sm"], "Privacy notice", href: "/legal/privacy", rel: "external", props: TestId("legal-link-privacy"));
                row.Text([Text.Caption], "·");
                row.Text([Text.Link, "text-sm"], "Terms", href: "/legal/terms", rel: "external", props: TestId("legal-link-terms"));
            });
        });
    }

    private void RenderSsoConnectionsCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "SSO Connections");

            // The space's tenant domains and IdP setup, so behind the same password as the other
            // tabs that read the space's own configuration.
            if (!SectionsUnlocked())
            {
                view.Text([Text.Body], "Locked", props: TestId("sso-locked"));
                return;
            }

            view.Button([Button.PrimaryMd], text: "List", onClick: ListSsoConnectionsAsync, props: TestId("sso-list-run"));

            if (!string.IsNullOrEmpty(_ssoConnectionsResult.Value))
            {
                bool passed = _ssoConnectionsResult.Value.StartsWith("PASS", StringComparison.Ordinal);
                view.Text([Text.Body, "mt-2 whitespace-pre-wrap break-words", passed ? "text-success-primary" : "text-error-primary"],
                    _ssoConnectionsResult.Value, props: TestId("sso-list-result"));
            }
        });
    }

    private async Task ListSsoConnectionsAsync()
    {
        if (!SectionsUnlocked())
        {
            _ssoConnectionsResult.Value = "FAIL locked";
            return;
        }

        try
        {
            var connections = await app.SsoConnections.ListAsync();
            var lines = connections.Select(c =>
                $"{c.Name} · {c.Preset} · {c.Status} · {c.RequiredMode} · " +
                (c.EmailDomains.Count == 0 ? "no domains" : string.Join(", ", c.EmailDomains.Select(d => $"{d.Domain} {d.State}"))));
            _ssoConnectionsResult.Value = string.Join("\n", lines.Prepend($"PASS {connections.Count} connection(s)"));
        }
        catch (Exception ex)
        {
            _ssoConnectionsResult.Value = $"FAIL {ex.GetType().Name}: {ex.Message}";
        }
    }

    private void RenderAccountCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Account and Erasure");

            view.Row([Layout.Row.Sm, "items-baseline flex-wrap"], content: view =>
            {
                view.Text([Text.Body], "Removal scheduled for:");
                view.Text([Text.Body], _accountRemovalScheduledFor.Value, props: TestId("account-removal-scheduled"));
            });

            view.Row([Layout.Row.Sm, "items-baseline flex-wrap mt-2"], content: view =>
            {
                view.Text([Text.Body], "Last erasure:");
                view.Text([Text.Body, "break-words"], _lastRuntimeErasure.Value, props: TestId("erasure-last-runtime"));
            });

            view.Row(["flex-wrap gap-3 mt-4"], content: row =>
            {
                row.Button([Button.OutlineMd], text: "Read", onClick: () => RunAccountRemovalAsync(ClientFunctions.GetAccountRemovalAsync), props: TestId("account-removal-read"));
                row.Button([Button.ErrorMd], text: "Request removal", onClick: () => _accountRemovalConfirmOpen.Value = true, props: TestId("account-removal-request"));
                row.Button([Button.OutlineMd], text: "Cancel removal", onClick: () => RunAccountRemovalAsync(ClientFunctions.CancelAccountRemovalAsync), props: TestId("account-removal-cancel"));
            });

            view.AlertDialog(
                open: _accountRemovalConfirmOpen.Value,
                onOpenChange: async open => _accountRemovalConfirmOpen.Value = open,
                title: "Erase your account?",
                description: "Your Ikon account is erased after the grace period (14 days by default). Cancel removal takes it back until then.",
                cancelLabel: "Keep my account",
                actionLabel: "Request removal",
                onAction: async () =>
                {
                    _accountRemovalConfirmOpen.Value = false;
                    await RunAccountRemovalAsync(ClientFunctions.RequestAccountRemovalAsync);
                });

            if (!string.IsNullOrEmpty(_accountRemovalResult.Value))
            {
                bool passed = _accountRemovalResult.Value.StartsWith("PASS", StringComparison.Ordinal);
                view.Text([Text.Body, "mt-2 break-words", passed ? "text-success-primary" : "text-warning-primary"],
                    _accountRemovalResult.Value, props: TestId("account-removal-result"));
            }
        });
    }

    private async Task RunAccountRemovalAsync(Func<int?, CancellationToken, Task<ClientAccountRemoval?>> call)
    {
        try
        {
            var removal = await call(null, CancellationToken.None);

            if (removal == null)
            {
                _accountRemovalScheduledFor.Value = "unknown";
                _accountRemovalResult.Value = "FAIL the client has no account functions";
                return;
            }

            if (!removal.Succeeded)
            {
                // A guest has no account to erase, so the backend refusing is the expected answer there.
                _accountRemovalScheduledFor.Value = "unknown";
                _accountRemovalResult.Value = $"REFUSED {removal.Error}";
                return;
            }

            _accountRemovalScheduledFor.Value = removal.ScheduledFor is { } scheduledFor
                ? TimeZoneInfo.ConvertTime(scheduledFor, ViewerTimeZone()).ToString("yyyy-MM-dd HH:mm:sszzz", CultureInfo.InvariantCulture)
                : "nothing scheduled";
            _accountRemovalResult.Value = "PASS";
        }
        catch (Exception ex)
        {
            _accountRemovalScheduledFor.Value = "unknown";
            _accountRemovalResult.Value = $"FAIL {ex.GetType().Name}: {ex.Message}";
        }
    }
}
