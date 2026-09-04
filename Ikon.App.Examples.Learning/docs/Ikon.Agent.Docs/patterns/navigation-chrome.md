<!-- mined-from: Ikon.App.Patterns -->
# Navigation Chrome — Breadcrumbs And A Navigation Menu

Two components that both look like "navigation" and do different jobs. A **breadcrumb** says where
you are and lets you go back up; a **navigation menu** is the hover-and-focus menubar that reveals
panels of destinations.

Neither is a tab strip. `Tabs` switches the content of the page you are on; these take you
somewhere else.

## When to use

A breadcrumb once the app is more than two levels deep. A navigation menu when there are enough
destinations that a flat row of links stops working — for a sidebar, `collapsible-sidebar-nav`;
for a flat set of page tabs, `nav-and-menu-rows`.

## Notes

- **Derive the breadcrumb from where the user is; never store it beside that.** Two sources of
  truth for the same fact drift, and the crumb is the one that goes stale silently.
- **The last `BreadcrumbItem` always renders as the non-clickable current page** and its `OnClick`
  is ignored — so there is no need to special-case the tail when building the list.
- **The reactive list's mutation set is specific**: `Add`, `AddRange`, `Insert`, `Remove`,
  `RemoveAt`, `RemoveAll`, `Clear`, `ReplaceAll`, `Sort`, `Update`. There is no `RemoveRange`, so
  truncating a path is a `ReplaceAll`.
- `NavigationMenuItem`'s `value:` is its identity, matched against the menu's `value` — the same
  contract as an accordion item.
- `NavigationMenuLink`'s `active:` marks the current destination for assistive technology;
  `onSelect:` is the navigation itself. Setting only the second leaves a screen reader unable to
  say where the user is.
- `delayDuration`/`skipDelayDuration` tune how eagerly panels open on hover. Hover does not exist
  on touch, so anything reachable only by opening a panel needs another route.

## Snippet

```csharp
private readonly ClientReactive<string> _section = new("");
private readonly ClientReactiveList<string> _path = new();

private void Render(IView view)
{
    view.Column(["gap-3"], content: col =>
    {
        // A breadcrumb is DERIVED from where the user is, never stored beside it. The LAST
        // item always renders as the non-clickable current page and its OnClick is ignored,
        // so there is no need to special-case the tail.
        col.Breadcrumb(items: _path
            .Select((label, index) => new BreadcrumbItem(
                label,
                // The reactive list's mutation set is specific -- Add, AddRange, Insert,
                // Remove, RemoveAt, RemoveAll, Clear, ReplaceAll, Sort, Update. There is no
                // RemoveRange, so truncating is a ReplaceAll.
                OnClick: async () => _path.ReplaceAll(_path.Take(index + 1).ToList())))
            .ToList());

        // NavigationMenu is the hover/focus menubar: triggers that reveal panels. It is not a
        // tab strip -- a Tabs component switches the page's content, while this navigates
        // away from it.
        col.NavigationMenu(
            value: _section.Value,
            onValueChange: async section => _section.Value = section,
            content: menu => menu.NavigationMenuList(content: list =>
            {
                list.NavigationMenuItem(value: "products", content: item =>
                {
                    item.NavigationMenuTrigger(content: t => t.Text(text: "Products"));

                    item.NavigationMenuContent(content: panel =>
                    {
                        // active: marks the current destination for assistive tech; onSelect
                        // is the navigation itself.
                        panel.NavigationMenuLink(
                            active: _path.Count > 0 && _path[^1] == "Widgets",
                            onSelect: async () => _path.ReplaceAll(["Home", "Products", "Widgets"]),
                            content: v => v.Text(text: "Widgets"));

                        panel.NavigationMenuLink(
                            onSelect: async () => _path.ReplaceAll(["Home", "Products", "Gadgets"]),
                            content: v => v.Text(text: "Gadgets"));
                    });
                });
            }));
    });
}
```

## See also

- `nav-and-menu-rows` — page tabs, sidebar rows and menu rows, and which token each takes.
- `url-path-route-state` — keeping the location in the URL so a crumb survives a reload.
