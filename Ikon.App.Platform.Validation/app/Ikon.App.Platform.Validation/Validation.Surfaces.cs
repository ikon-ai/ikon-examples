public partial class Validation
{
    // The probe surfaces of ikon-config.toml, each at /surfaces/<name>, which validation/validate_platform_surfaces.py
    // drives. "app", the fallback, serves the tabs; "console" has a host of its own and no route.
    private const string SurfaceRoutePrefix = "/surfaces/";
    private const string SiteShellVariant = "site-shell";
    private static readonly string[] ProbeSurfaces = ["site", "static", "members", "staff", "console"];

    private readonly ClientReactive<int> _surfaceCounter = new(0);

    // The surface the client entered, kept per client so the root render can branch on it: a client
    // reactive lets the clients that entered the same surface share one render, where reading the
    // client's own context would render every client of the tabs alone
    private readonly ClientReactive<string> _enteredSurface = new("");

    [Function(Name = "SurfaceEcho", Description = "Returns the name of the surface its caller entered", Visibility = FunctionVisibility.External)]
    [Surface("site", "staff")]
    public string SurfaceEcho() => SurfaceOf(ReactiveScope.ClientId);

    private string SurfaceOf(int clientId) =>
        app.ReactiveGlobalState.Clients.Value.TryGetValue(clientId, out var context) ? context.Surface : "";

    // The probe page is chosen by the path, not by the surface the client entered: a wrong selection
    // then shows as a page whose two names differ, rather than as the tabs
    private string? ProbeSurfaceFor(string? path)
    {
        if (path != null && path.StartsWith(SurfaceRoutePrefix, StringComparison.Ordinal))
        {
            return path[SurfaceRoutePrefix.Length..].Split('/', 2)[0];
        }

        string entered = _enteredSurface.Value;
        return ProbeSurfaces.Contains(entered) ? entered : null;
    }

    private bool TryRenderSurfaceProbe(UIView view)
    {
        // The probe shows nothing private, and the surfaces phase reads its snapshots, so they render
        // for real rather than as skeletons
        if (view.SnapshotVariant == SiteShellVariant)
        {
            view.SnapshotReveal(view => view.Column([Layout.Column.Lg, Container.Xl4, "py-8 px-4"], props: TestId("site-shell"), content: view =>
            {
                view.Text([Text.H1], "Surface probe");
                view.Text([Text.Body, "text-tertiary"], "Loading this page");
            }));

            return true;
        }

        string? probeSurface = ProbeSurfaceFor(app.Navigation.CurrentPath);

        if (probeSurface == null)
        {
            return false;
        }

        view.SnapshotReveal(view => view.Column([Layout.Column.Lg, Container.Xl4, "py-8 px-4"], content: view =>
        {
            view.Text([Text.H1], "Surface probe");
            view.Text([Text.Body], "The path names:");
            view.Text([Text.Body], probeSurface, props: TestId("surface-path"));
            view.Text([Text.Body], "The client entered:");
            view.Text([Text.Body], _enteredSurface.Value, props: TestId("surface-entered"));

            // A surface that never connects may hold no control handled on the server, or the bundle refuses it
            if (probeSurface == "static")
            {
                view.Link([Button.SecondaryMd], href: "/", content: v => v.Text(text: "Open the app"));
                return;
            }

            view.Row([Layout.Row.Md, "items-center"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Add one", onClick: () =>
                {
                    _surfaceCounter.Value++;
                    return Task.CompletedTask;
                }, props: TestId("surface-counter-button"));
                view.Text([Text.Body], _surfaceCounter.Value.ToString(), props: TestId("surface-counter"));
            });

            int browsers = app.ReactiveGlobalState.Clients.Value.Values.Count(context => context.ContextType == ContextType.Browser);
            view.Text([Text.Body], "Browsers the app sees in this instance:");
            view.Text([Text.Body], browsers.ToString(), props: TestId("surface-app-clients"));
            view.AddNode("validation-surface-probe", new Dictionary<string, object?>(), style: ["w-full"]);
        }));

        return true;
    }
}
