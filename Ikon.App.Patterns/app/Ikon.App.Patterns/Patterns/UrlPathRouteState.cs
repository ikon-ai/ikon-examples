namespace Ikon.App.Patterns.Patterns;

// Pattern: url-path-route-state — see docs/patterns/url-path-route-state.md.
// `app` is the App's primary-constructor handle; the Render* / LoadDashboardData stubs stand in for the
// per-page views the router switches between. The docsnippet region is the canonical routing round-trip.
internal sealed class UrlPathRouteState(IAppBase app) : IPatternDemo
{
    public string Slug => "url-path-route-state";
    public string Title => "URL path route state";
    public string Category => "Navigation";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title, "Mirrors the browser URL path into a single reactive so nav highlight and page switch stay in sync — see docs/patterns/url-path-route-state.md.");

    private Task LoadDashboardDataAsync(string id) => throw new NotImplementedException();

    private void RenderExplorePage(UIView view) => throw new NotImplementedException();

    private void RenderSettingsPage(UIView view) => throw new NotImplementedException();

    private void RenderDashboardView(UIView view, string id) => throw new NotImplementedException();

    private void RenderDashboardList(UIView view) => throw new NotImplementedException();

    #region docsnippet:pattern-url-path-route-state
    // Per client: the URL is one client's, so the page it selects is too. The factory runs on the
    // client's first read -- its first frame -- and CurrentPath is already known by then, so a deep
    // link or reload paints the right page at once. A joined handler runs on a background task and
    // can lose that race, so it is not what the first frame depends on.
    private readonly ClientReactive<string> _activePage =
        ClientReactive.Create(_ => PageFor(app.Navigation.CurrentPath ?? "/"));

    public async Task Main()
    {
        // PathChangedAsync fires only for a move the client makes on its own -- a link click or
        // back/forward. The path a client lands on (deep link, reload) raises nothing: the factory
        // above routes it, and the joined handler only loads the data that page needs. The app's
        // own SetPathAsync is not echoed back either, which is why NavigateToDashboardAsync routes
        // itself.
        app.Navigation.PathChangedAsync += async args => await RouteAsync(args.Path);
        app.OnClientJoined(async _ => await RouteAsync(app.Navigation.CurrentPath ?? "/"));
    }

    // Pure: the same mapping serves the first frame and every later navigation.
    private static string PageFor(string path)
    {
        path = path.TrimStart('/');

        if (path.StartsWith("dashboard/"))
        {
            return $"dashboard:{path["dashboard/".Length..]}";
        }

        return path is "explore" or "settings" ? path : "dashboards";
    }

    private async Task RouteAsync(string path)
    {
        var page = PageFor(path);
        _activePage.Value = page;

        if (page.StartsWith("dashboard:"))
        {
            await LoadDashboardDataAsync(page["dashboard:".Length..]);
        }
    }

    private async Task NavigateToDashboardAsync(string id)
    {
        var path = $"/dashboard/{id}";

        // SetPathAsync returns false when the client never acknowledged the move -- CurrentPath is
        // then rolled back to where it was. Routing anyway would render the dashboard behind a URL
        // that still says the old page, and nothing would ever report the mismatch.
        if (!await app.Navigation.SetPathAsync(path))
        {
            return;
        }

        await RouteAsync(path);
    }

    // Render switch: one reactive holds both "which page" and "which entity".
    private void Render(IView view)
    {
        if (_activePage.Value == "explore")
        {
            RenderExplorePage(view);
        }
        else if (_activePage.Value == "settings")
        {
            RenderSettingsPage(view);
        }
        else if (_activePage.Value.StartsWith("dashboard:"))
        {
            var id = _activePage.Value["dashboard:".Length..];
            RenderDashboardView(view, id);
        }
        else
        {
            RenderDashboardList(view);
        }
    }

    // Sidebar nav highlight: all dashboard sub-pages light up the same item.
    private bool IsNavItemActive(string page)
    {
        return _activePage.Value == page
            || (page == "dashboards" && _activePage.Value.StartsWith("dashboard:"));
    }
    #endregion
}
