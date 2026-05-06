<!-- mined-from: Ikon.App.Monitor -->
# URL Path As State — `app.Navigation` Round-Trip With Composite Keys

Drive the active page from a single `Reactive<string> _activePage`, sync changes both ways with `app.Navigation`, and encode entity IDs with a colon prefix (`dashboard:abc123`) so one reactive holds both "which view" and "which item".

## When to use

Multi-page apps where you want browser back/forward and shareable URLs without React Router. One reactive, one path-changed handler, one `SetPathAsync` call per click.

## Snippet

```csharp
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
        else if (path == "explore")    { _activePage.Value = "explore"; }
        else if (path == "settings")   { _activePage.Value = "settings"; }
        else                           { _activePage.Value = "dashboards"; }
    };
}

private async Task NavigateToDashboardAsync(string id)
{
    _activePage.Value = $"dashboard:{id}";
    await app.Navigation.SetPathAsync($"/dashboard/{id}");
    await LoadDashboardDataAsync(id);
}

// Render switch:
if (_activePage.Value == "explore")
    RenderExplorePage(view);
else if (_activePage.Value == "settings")
    RenderSettingsPage(view);
else if (_activePage.Value.StartsWith("dashboard:"))
{
    var id = _activePage.Value["dashboard:".Length..];
    RenderDashboardView(view, id);
}
else
    RenderDashboardList(view);

// Sidebar nav highlight:
var isActive = _activePage.Value == page
    || (page == "dashboards" && _activePage.Value.StartsWith("dashboard:"));
```

## Notes

- The `prefix:id` encoding (`dashboard:abc`) keeps "which page" and "which entity" in one reactive. Cheap pattern-match in the renderer.
- Always update both `_activePage.Value` AND `app.Navigation.SetPathAsync` on click. The `PathChangedAsync` handler covers external nav (back button, deep link).
- Sidebar active-state checks use `StartsWith("dashboard:")` so all dashboard sub-pages light up the same nav item.
- Default to a fallback page (`"dashboards"`) for any unknown path.

## See also

- `role-based-screen-router`
- `bottom-tab-bar-nav`
- `collapsible-sidebar-nav`
