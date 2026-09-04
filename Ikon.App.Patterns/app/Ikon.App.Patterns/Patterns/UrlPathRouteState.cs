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
    private readonly Reactive<string> _activePage = new("dashboards");

    public async Task Main()
    {
        app.Navigation.PathChangedAsync += async args =>
        {
            var path = args.Path.TrimStart('/');

            if (path.StartsWith("dashboard/"))
            {
                var id = path["dashboard/".Length..];
                _activePage.Value = $"dashboard:{id}";
                await LoadDashboardDataAsync(id);
            }
            else if (path == "explore")
            {
                _activePage.Value = "explore";
            }
            else if (path == "settings")
            {
                _activePage.Value = "settings";
            }
            else
            {
                _activePage.Value = "dashboards";
            }
        };
    }

    private async Task NavigateToDashboardAsync(string id)
    {
        _activePage.Value = $"dashboard:{id}";
        await app.Navigation.SetPathAsync($"/dashboard/{id}");
        await LoadDashboardDataAsync(id);
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
