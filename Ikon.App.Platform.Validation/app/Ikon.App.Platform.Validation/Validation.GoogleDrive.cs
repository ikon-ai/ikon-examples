using Ikon.Connectors;
using Ikon.Connectors.Google;

public partial class Validation
{
    private const string GdClientIdSecret = "GOOGLE_CLIENT_ID";
    private const string GdClientSecretSecret = "GOOGLE_CLIENT_SECRET";
    private const string GdDriveScope = "https://www.googleapis.com/auth/drive";
    private const string GdStateParam = "googlestate";
    private const string GdCodeParam = "googlecode";
    private const string GdErrorParam = "googleerror";

    private static readonly (string Key, string Description)[] GdRequiredSecrets =
    [
        (GdClientIdSecret, "Client ID of the Google OAuth client (Web application) the Drive tab signs in with"),
        (GdClientSecretSecret, "Client secret of that Google OAuth client"),
    ];

    private readonly Reactive<string> _gdSubTab = new("files");
    private readonly Reactive<bool> _gdBusy = new(false);
    private readonly Reactive<string> _gdStatus = new("");
    private readonly Reactive<string?> _gdSignInUrl = new(null);
    private readonly Reactive<bool> _gdSignedIn = new(false);

    // The verifier of each sign-in started here, by its state, until the redirect redeems it.
    private readonly Dictionary<string, string> _gdPendingSignIns = new();

    private (GoogleTokenProvider Tokens, Drive Drive, GoogleWatches Watches)? _gdClients;

    private void RenderGoogleDriveSection(UIView view)
    {
        if (RenderSectionLocked(view, "Google Drive"))
        {
            return;
        }

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Google Drive");
            RenderGoogleDriveAccountCard(view);

            if (!_gdSignedIn.Value)
            {
                return;
            }

            view.Tabs(
                value: _gdSubTab.Value,
                onValueChange: async value => _gdSubTab.Value = value ?? "files",
                listContainerStyle: [Card.Default, "p-2"],
                listStyle: [Tabs.List, "flex-wrap bg-transparent gap-1"],
                triggerStyle: [Tabs.Trigger, "text-xs px-2 py-1"],
                contentStyle: [Tabs.Content, "flex flex-col gap-6"],
                tabs: [
                    new TabItem("files", "Files", view =>
                    {
                        RenderGoogleDriveFilesCard(view);
                        RenderGoogleDrivePreviewCard(view);
                        RenderGoogleDriveBinCard(view);
                    }),
                    new TabItem("changes", "Changes & push", view =>
                    {
                        RenderGoogleDriveChangesCard(view);
                        RenderGoogleDrivePushCard(view);
                    }),
                ]);
        });
    }

    private void RenderGoogleDriveAccountCard(UIView view)
    {
        view.Box([Card.Elevated, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Account");

            var missing = GdRequiredSecrets.Where(secret => !app.Secrets.TryGet(secret.Key, out var value) || string.IsNullOrWhiteSpace(value)).ToList();

            if (missing.Count > 0)
            {
                view.Text([Text.Body, "text-warning-primary mb-2"], $"SKIP: set the secrets {string.Join(", ", missing.Select(s => s.Key))} on this space", props: TestId("gd-setup"));

                foreach (var (key, description) in missing)
                {
                    view.Text([Text.Caption, "font-mono"], $"ikon app secret set {key} --description \"{description}\"", key: key);
                }

                return;
            }

            if (GdSignInRedirectUri() is not { } redirect)
            {
                view.Text([Text.Body, "text-warning-primary"], "SKIP: the app has no public URL to send the sign-in back to", props: TestId("gd-setup"));
                return;
            }

            view.Text([Text.Caption, "mb-1"], "Signs in as a Google account with the full Drive scope and acts as that person. Add this authorized redirect URI to the OAuth client:");
            view.Text([Text.Caption, "font-mono mb-3 break-all"], redirect, props: TestId("gd-redirect-uri"));

            view.Row([Layout.Row.Sm, "flex-wrap items-center"], content: view =>
            {
                view.Button([_gdSignedIn.Value ? Button.OutlineMd : Button.PrimaryMd], text: _gdSignedIn.Value ? "Sign in again" : "Sign in with Google",
                    disabled: _gdBusy.Value, props: TestId("gd-sign-in"),
                    onClick: async () => await GdRunAsync("start sign-in", StartGoogleSignInAsync));

                if (_gdSignInUrl.Value is { } signInUrl)
                {
                    view.Link([Button.PrimaryMd], href: signInUrl, text: "Continue to Google", props: TestId("gd-sign-in-link"));
                }

                if (_gdSignedIn.Value)
                {
                    view.Button([Button.OutlineMd], text: "Check sign-in", disabled: _gdBusy.Value, props: TestId("gd-check"),
                        onClick: async () => await GdRunAsync("check sign-in", CheckGoogleSignInAsync));
                    view.Button([Button.GhostMd], text: "Sign out", disabled: _gdBusy.Value, props: TestId("gd-sign-out"),
                        onClick: async () => SignOutOfGoogle());
                }

                if (_gdBusy.Value)
                {
                    view.Spinner();
                }
            });

            if (_gdStatus.Value.Length > 0)
            {
                view.Text([Text.Body, "mt-3", SpStatusColor(_gdStatus.Value)], _gdStatus.Value, props: TestId("gd-status"));
            }
        });
    }

    // Google's redirect carries no session identity, so it lands on the space's shared instance rather
    // than the one that started the sign-in and holds its verifier. This only sends the browser back
    // to that session, which the state names, with the code for it to redeem.
    [HttpGet("/google/signed-in", Auth = EndpointAuth.Public)]
    public Task<HttpResult> GoogleSignInCallbackAsync(HttpRequest request)
    {
        request.Query.TryGetValue("state", out var state);
        var session = state?.Split('.', 2)[0];

        if (state is null || session is null || !GdSessionHashRegex().IsMatch(session) || GdSignInRedirectUri() is not { } redirect)
        {
            return Task.FromResult(GdSignInPage(400, "This sign-in was not started from the Validation app", null));
        }

        var query = new List<string> { $"ikon-session={session}", $"{GdStateParam}={Uri.EscapeDataString(state)}" };

        if (request.Query.TryGetValue("code", out var code))
        {
            query.Add($"{GdCodeParam}={Uri.EscapeDataString(code)}");
        }

        if (request.Query.TryGetValue("error", out var error))
        {
            query.Add($"{GdErrorParam}={Uri.EscapeDataString(error)}");
        }

        var target = $"{new Uri(redirect).GetLeftPart(UriPartial.Authority)}/google-drive?{string.Join('&', query)}";
        return Task.FromResult(GdSignInPage(200, "Returning to the Validation app…", target));
    }

    private void InitGoogleDrive()
    {
        app.OnClientJoined(async (clientContext, clientParams) =>
        {
            if (clientContext.InitialPath.Split('?', 2) is not [_, var queryString])
            {
                return;
            }

            var query = System.Web.HttpUtility.ParseQueryString(queryString);

            if (query[GdStateParam] is not { } state)
            {
                return;
            }

            // The code is single-use and bound to its verifier, but it has no business staying in the
            // address bar; the session's own id stays, so a reload rejoins this session.
            var id = app.SessionIdentity.Id;
            await ClientFunctions.SetUrlAsync(string.IsNullOrEmpty(id) ? "/google-drive" : $"/google-drive?id={Uri.EscapeDataString(id)}", replace: true, targetId: clientContext.SessionId);

            // Not through GdRunAsync: it skips while another Drive action is busy, and this code cannot wait.
            try
            {
                _gdStatus.Value = await FinishGoogleSignInAsync(state, query[GdCodeParam], query[GdErrorParam]);
            }
            catch (ConnectorException ex)
            {
                _gdStatus.Value = $"FAIL finish sign-in: {ex.Message}";
            }
        });
    }

    private async Task<string> FinishGoogleSignInAsync(string state, string? code, string? error)
    {
        if (error is not null)
        {
            return $"FAIL Google refused the sign-in: {error}";
        }

        if (code is null || !_gdPendingSignIns.Remove(state, out var verifier))
        {
            return "FAIL this sign-in was not started in this session, or was already used";
        }

        var credentials = await GoogleAuth.ExchangeCodeAsync(app.Secrets[GdClientIdSecret], app.Secrets[GdClientSecretSecret], code, GdSignInRedirectUri()!, codeVerifier: verifier);
        var tokens = new GoogleTokenProvider(credentials);
        var granted = await tokens.GetGrantedScopesAsync();

        _gdClients = (tokens, new Drive(tokens), new GoogleWatches(tokens));
        _gdSignedIn.Value = true;
        _gdSignInUrl.Value = null;
        await GdOpenFolderAsync(-1);

        return granted.Contains(GdDriveScope)
            ? $"PASS signed in; granted: {string.Join(", ", granted)}"
            : $"FAIL signed in without the Drive scope, which the consent screen let the person untick; granted: {string.Join(", ", granted)}";
    }

    private static HttpResult GdSignInPage(int status, string message, string? forwardTo)
    {
        var forward = forwardTo is null
            ? ""
            : $"""<meta http-equiv="refresh" content="0;url={System.Net.WebUtility.HtmlEncode(forwardTo)}">""";
        var link = forwardTo is null
            ? ""
            : $"""<br><a href="{System.Net.WebUtility.HtmlEncode(forwardTo)}">Continue</a>""";
        var html = $"""
            <!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Google sign-in</title>{forward}</head>
            <body style="font-family: system-ui, sans-serif; display: grid; place-items: center; min-height: 100vh; margin: 0">
            <p style="max-width: 28rem; padding: 2rem; text-align: center">{System.Net.WebUtility.HtmlEncode(message)}{link}</p>
            </body></html>
            """;

        return new HttpResult(status, html, "text/html");
    }

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9_-]{1,128}$")]
    private static partial System.Text.RegularExpressions.Regex GdSessionHashRegex();

    private string? GdSignInRedirectUri()
    {
        return app.Endpoints.FirstOrDefault(e => e.FunctionName.EndsWith("_" + nameof(GoogleSignInCallbackAsync), StringComparison.Ordinal))?.PublicUrl is { Length: > 0 } url
            ? url
            : null;
    }

    private Task<string> StartGoogleSignInAsync()
    {
        var (verifier, challenge) = PkceCodes.Create();
        // The state names this session, so the redirect can be sent back to the instance holding the verifier.
        var state = $"{app.GlobalState.SessionIdentityHash}.{Guid.NewGuid():N}";
        _gdPendingSignIns[state] = verifier;
        _gdSignInUrl.Value = GoogleAuth.AuthorizeUrl(app.Secrets[GdClientIdSecret], GdSignInRedirectUri()!, [GdDriveScope], state, challenge);
        return Task.FromResult("PASS sign-in ready: press Continue to Google and sign in; Google brings you back here");
    }

    // A person can untick scopes on Google's consent screen, so the scopes granted are what decide
    // which actions on the tab can work.
    private async Task<string> CheckGoogleSignInAsync()
    {
        var granted = await GdClients().Tokens.GetGrantedScopesAsync();

        return granted.Contains(GdDriveScope)
            ? $"PASS the token works; granted: {string.Join(", ", granted)}"
            : $"FAIL the token lacks {GdDriveScope}; granted: {string.Join(", ", granted)}";
    }

    private void SignOutOfGoogle()
    {
        _gdClients = null;
        _gdSignedIn.Value = false;
        _gdItems.Clear();
        _gdFolderPath.Clear();
        _gdPreview.Value = null;
        _gdActiveItem.Value = null;
        _gdBin.Clear();
        _gdChanges.Clear();
        _gdChangesSummary.Value = null;
        _gdDeltaToken = null;
        _gdStatus.Value = "Signed out; the refresh token is forgotten, not revoked";
    }

    private (GoogleTokenProvider Tokens, Drive Drive, GoogleWatches Watches) GdClients()
    {
        return _gdClients ?? throw new InvalidOperationException("Sign in with Google first");
    }

    // Every action reports into one status line: PASS with what it did, or FAIL with Google's own
    // answer, whose reason names a missing scope or permission when that is the cause.
    private async Task GdRunAsync(string action, Func<Task<string>> run)
    {
        if (_gdBusy.Value)
        {
            return;
        }

        _gdBusy.Value = true;
        _gdStatus.Value = $"Working: {action}…";

        try
        {
            _gdStatus.Value = await run();
        }
        catch (ConnectorException ex) when (ex.IsReconnectRequired)
        {
            _gdStatus.Value = $"FAIL {action}: {ex.Message} — {(ex.StatusCode == 401 ? "Google no longer accepts the sign-in; sign in again" : "the account cannot do this, or the token lacks the scope")}";
        }
        catch (ConnectorException ex)
        {
            _gdStatus.Value = $"FAIL {action}: {ex.Message}";
        }
        catch (Exception ex)
        {
            _gdStatus.Value = $"FAIL {action}: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _gdBusy.Value = false;
        }
    }
}
