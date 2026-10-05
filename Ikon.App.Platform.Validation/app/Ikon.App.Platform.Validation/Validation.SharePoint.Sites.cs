using Ikon.Connectors;
using Ikon.Connectors.Microsoft;

public partial class Validation
{
    private static readonly string[] SpDelegatedScopes = ["Sites.ReadWrite.All", "Files.ReadWrite.All", "User.Read"];

    private readonly ReactiveList<SharePointSite> _spFoundSites = new();
    private readonly Reactive<string?> _spFoundSitesTitle = new(null);
    private readonly ReactiveList<DriveItemGrant> _spSiteGrants = new();
    private readonly Reactive<bool> _spSiteGrantsShown = new(false);
    private readonly Reactive<string?> _spSignInUrl = new(null);
    private readonly Reactive<string?> _spDelegatedUser = new(null);
    private readonly Reactive<bool> _spUseDelegated = new(false);

    // The verifier of each sign-in started here, by its state, until the redirect redeems it.
    private readonly Dictionary<string, string> _spPendingSignIns = new();

    private MicrosoftUserCredentials? _spDelegatedCredentials;

    private void RenderSharePointDiscoveryCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Sites");
            view.Text([Text.Caption, "mb-3"], "All sites needs app-only Sites.Read.All; Followed sites needs a delegated sign-in; Site access needs Sites.FullControl.All or a fullcontrol site grant");

            view.Row([Layout.Row.Sm, "flex-wrap mb-3"], content: view =>
            {
                view.Button([Button.OutlineSm], text: "Subsites", disabled: _spBusy.Value || _spSite.Value == null, props: TestId("sp-subsites"),
                    onClick: async () => await SpRunAsync("list subsites", ListSharePointSubsitesAsync));
                view.Button([Button.OutlineSm], text: "All sites", disabled: _spBusy.Value || _spUseDelegated.Value, props: TestId("sp-all-sites"),
                    onClick: async () => await SpRunAsync("list all sites", ListAllSharePointSitesAsync));
                view.Button([Button.OutlineSm], text: "Followed sites", disabled: _spBusy.Value || !_spUseDelegated.Value, props: TestId("sp-followed-sites"),
                    onClick: async () => await SpRunAsync("list followed sites", ListFollowedSharePointSitesAsync));
                view.Button([Button.OutlineSm], text: "Site access", disabled: _spBusy.Value || _spSite.Value == null, props: TestId("sp-site-access"),
                    onClick: async () => await SpRunAsync("list site access", ListSharePointSiteGrantsAsync));
            });

            if (_spFoundSitesTitle.Value is { } title)
            {
                view.Text([Text.Label, "mb-2"], title);

                foreach (var site in _spFoundSites.Take(50))
                {
                    view.Row([Layout.Row.Md, "items-center py-1 border-b border-secondary"], key: site.Id, content: view =>
                    {
                        view.Column(["flex-1 min-w-0"], content: view =>
                        {
                            view.Text([Text.Body, "truncate"], site.DisplayName.Length > 0 ? site.DisplayName : site.Name, props: TestId("sp-found-site"));
                            view.Text([Text.Caption, "truncate"], $"{site.WebUrl}{(site.IsPersonalSite ? " · personal" : "")}{(site.DataLocationCode is { } geo ? $" · {geo}" : "")}");
                        });

                        if (site.WebUrl is { } webUrl)
                        {
                            view.Button([Button.GhostSm], text: "Open", disabled: _spBusy.Value, onClick: async () =>
                            {
                                _spSiteInput.Value = webUrl;
                                await SpRunAsync("open site", OpenSharePointSiteAsync);
                            });
                        }
                    });
                }

                if (_spFoundSites.Count > 50)
                {
                    view.Text([Text.Caption, "mt-2"], $"… and {_spFoundSites.Count - 50} more");
                }
            }

            if (_spSiteGrantsShown.Value)
            {
                view.Text([Text.Label, "mt-4 mb-2"], "Apps granted access to this site");

                if (_spSiteGrants.Count == 0)
                {
                    view.Text([Text.Caption], "No app holds a site grant");
                }

                foreach (var grant in _spSiteGrants)
                {
                    view.Text([Text.Caption], $"{grant.DisplayName ?? grant.PrincipalId ?? "?"} ({grant.Kind}) · {string.Join(", ", grant.Roles)}", key: grant.PermissionId + grant.PrincipalId, props: TestId("sp-site-grant"));
                }
            }
        });
    }

    private void RenderSharePointDelegatedCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-2"], "Delegated sign-in");

            if (!SpHasSecret(SpClientSecretSecret))
            {
                view.Text([Text.Body, "text-warning-primary"], $"SKIP: a delegated sign-in needs the client secret {SpClientSecretSecret}", props: TestId("sp-delegated-status"));
                return;
            }

            var redirect = SpSignInRedirectUri();

            if (redirect is null)
            {
                view.Text([Text.Body, "text-warning-primary"], "SKIP: the app has no public URL to send the sign-in back to", props: TestId("sp-delegated-status"));
                return;
            }

            view.Text([Text.Caption, "mb-1"], "Acts as the person who signs in, seeing only what they can see. Add this redirect URI to the app registration (Web platform):");
            view.Text([Text.Caption, "font-mono mb-3 break-all"], redirect);

            view.Row([Layout.Row.Sm, "flex-wrap items-center"], content: view =>
            {
                view.Button([Button.OutlineMd], text: "Sign in with Microsoft", disabled: _spBusy.Value, props: TestId("sp-delegated-start"),
                    onClick: async () => await SpRunAsync("start sign-in", StartSharePointSignInAsync));

                if (_spSignInUrl.Value is { } signInUrl)
                {
                    view.Link([Button.PrimaryMd], href: signInUrl, text: "Continue to Microsoft", target: "_blank", props: TestId("sp-delegated-link"));
                }

                if (_spDelegatedUser.Value != null)
                {
                    view.Button([_spUseDelegated.Value ? Button.PrimarySm : Button.OutlineSm], text: _spUseDelegated.Value ? "Using delegated" : "Use delegated",
                        props: TestId("sp-delegated-use"),
                        onClick: async () => SpSwitchCredentials(delegated: !_spUseDelegated.Value));
                }
            });

            if (_spDelegatedUser.Value is { } user)
            {
                view.Text([Text.Body, "mt-3 text-success-primary"], $"PASS signed in as {user}{(_spUseDelegated.Value ? "; the tab now acts as this person" : "; press Use delegated to act as this person")}", props: TestId("sp-delegated-status"));
            }
        });
    }

    [HttpGet("/sharepoint/signed-in", Auth = EndpointAuth.Public)]
    public async Task<HttpResult> SharePointSignInCallbackAsync(HttpRequest request)
    {
        string message;
        var ok = false;

        if (request.Query.TryGetValue("error", out var error))
        {
            message = $"Microsoft refused the sign-in: {error} {request.Query.GetValueOrDefault("error_description")}";
        }
        else if (!request.Query.TryGetValue("state", out var state) || !request.Query.TryGetValue("code", out var code) || !_spPendingSignIns.Remove(state, out var verifier))
        {
            message = "This sign-in was not started here, or was already used";
        }
        else
        {
            try
            {
                var credentials = await MicrosoftAuth.ExchangeCodeAsync(app.Secrets[SpTenantSecret], app.Secrets[SpClientSecret], app.Secrets[SpClientSecretSecret],
                    code, SpSignInRedirectUri()!, SpDelegatedScopes, codeVerifier: verifier);

                var tokens = new MicrosoftTokenProvider(credentials);
                var granted = await tokens.GetGrantedPermissionsAsync();

                _spDelegatedCredentials = credentials;
                _spDelegatedUser.Value = $"a person granted {string.Join(", ", granted.DelegatedScopes)}";
                _spSignInUrl.Value = null;
                message = "Signed in. Return to the Validation app.";
                ok = true;
            }
            catch (ConnectorException ex)
            {
                message = $"The code could not be redeemed: {ex.Message}";
            }
        }

        var html = $"""
            <!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>SharePoint sign-in</title></head>
            <body style="font-family: system-ui, sans-serif; display: grid; place-items: center; min-height: 100vh; margin: 0">
            <p style="max-width: 28rem; padding: 2rem; text-align: center">{System.Net.WebUtility.HtmlEncode(message)}</p>
            </body></html>
            """;

        return new HttpResult(ok ? 200 : 400, html, "text/html");
    }

    private string? SpSignInRedirectUri()
    {
        return app.Endpoints.FirstOrDefault(e => e.FunctionName.EndsWith("_" + nameof(SharePointSignInCallbackAsync), StringComparison.Ordinal))?.PublicUrl is { Length: > 0 } url
            ? url
            : null;
    }

    private Task<string> StartSharePointSignInAsync()
    {
        var (verifier, challenge) = PkceCodes.Create();
        var state = Guid.NewGuid().ToString("N");
        _spPendingSignIns[state] = verifier;
        _spSignInUrl.Value = MicrosoftAuth.AuthorizeUrl(app.Secrets[SpTenantSecret], app.Secrets[SpClientSecret], SpSignInRedirectUri()!, SpDelegatedScopes, state, challenge);
        return Task.FromResult("PASS sign-in ready: press Continue to Microsoft, sign in, then come back here");
    }

    private void SpSwitchCredentials(bool delegated)
    {
        _spUseDelegated.Value = delegated && _spDelegatedCredentials != null;
        _spClients = null;
        _spStatus.Value = _spUseDelegated.Value ? "Acting as the signed-in person" : "Acting as the app";
    }

    private async Task<string> ListSharePointSubsitesAsync()
    {
        var (sharePoint, _) = SpClients();
        var sites = await sharePoint.ListSubsitesAsync(_spSite.Value!.Id);
        SpShowSites($"Subsites of {_spSite.Value!.DisplayName}", sites);
        return $"PASS {_spSite.Value!.DisplayName} has {sites.Count} subsites";
    }

    private async Task<string> ListAllSharePointSitesAsync()
    {
        var (sharePoint, _) = SpClients();
        IReadOnlyList<SharePointSite> sites;
        var capped = "";

        try
        {
            sites = await sharePoint.ListAllSitesAsync(maxPages: SpListPages);
        }
        catch (ConnectorPageCapException<SharePointSite> cap)
        {
            sites = cap.Items;
            capped = " (the first pages; the tenant holds more)";
        }

        SpShowSites("Every site the app can see", sites);
        return $"PASS listed {sites.Count} sites, {sites.Count(s => s.IsPersonalSite)} of them personal{capped}";
    }

    private async Task<string> ListFollowedSharePointSitesAsync()
    {
        var (sharePoint, _) = SpClients();
        var sites = await sharePoint.ListFollowedSitesAsync();
        SpShowSites("Sites the signed-in person follows", sites);
        return $"PASS the signed-in person follows {sites.Count} sites";
    }

    private async Task<string> ListSharePointSiteGrantsAsync()
    {
        var (sharePoint, _) = SpClients();
        var grants = await sharePoint.ListAppGrantsAsync(SharePointTarget.Site(_spSite.Value!.Id));
        _spSiteGrants.ReplaceAll(grants);
        _spSiteGrantsShown.Value = true;
        return $"PASS {_spSite.Value!.DisplayName} grants {grants.Count} apps access";
    }

    private void SpShowSites(string title, IReadOnlyList<SharePointSite> sites)
    {
        _spFoundSites.ReplaceAll(sites.OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase));
        _spFoundSitesTitle.Value = title;
    }
}
