<!-- mined-from: Ikon.App.Monitor -->
# URL Path As State — `app.Navigation` Round-Trip With Composite Keys
<!-- checked-against: 191339b3b9d2157a -->
Drive the active page from one `ClientReactive<string> _activePage` (the URL is one client's, so the page is too), seeded per client from `app.Navigation.CurrentPath` so the first frame already shows the landing page; sync changes both ways with `app.Navigation`, and encode entity IDs with a colon prefix (`dashboard:abc123`) so one reactive holds both "which view" and "which item".

## When to use

Multi-page apps where you want browser back/forward and shareable URLs without React Router. One reactive, one pure `PageFor(path)` shared by its factory and by `RouteAsync`, one `SetPathAsync` call per click.

## Snippet

```csharp
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
```

## Notes

- The `prefix:id` encoding (`dashboard:abc`) keeps "which page" and "which entity" in one reactive. Cheap pattern-match in the renderer.
- **`SetPathAsync` returns a `bool` and you have to read it.** `false` means the client never acknowledged the move, and `CurrentPath` is rolled back to where it was — so routing on regardless leaves the app rendering one page behind a URL that names another, with nothing anywhere reporting it. Return on `false` (the platform has already logged a warning naming the client and path) and only route once it is `true`.
- A `view.Button` that navigates calls `app.Navigation.SetPathAsync` AND routes itself — the app's own `SetPathAsync` is not echoed back through `PathChangedAsync`, so nothing else would update the reactive. A `view.Link` with an internal `href` needs neither: the client turns the click into a path change without reloading, and the `PathChangedAsync` handler already routes it. Give a link an `onClick` only for a side effect (analytics, closing a menu) — never to restate where the `href` already points.
- `PathChangedAsync` fires only for a move the client makes on its own within the loaded document: link clicks and back/forward. The path a client lands on — a deep link, a reload — raises nothing; `app.Navigation.CurrentPath` holds it from before the first render, which is why `_activePage` is created with `ClientReactive.Create(_ => PageFor(app.Navigation.CurrentPath ?? "/"))`: the factory runs on the client's first read, inside its scope, so the first frame is already routed. A joined handler runs on a background task and can lose the race against that frame — here it only loads the data the landing page needs.
- Sidebar active-state checks use `StartsWith("dashboard:")` so all dashboard sub-pages light up the same nav item.
- Default to a fallback page (`"dashboards"`) for any unknown path.

## See also

- `collapsible-sidebar-nav`
