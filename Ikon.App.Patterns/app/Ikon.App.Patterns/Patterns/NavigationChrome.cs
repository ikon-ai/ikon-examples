namespace Ikon.App.Patterns.Patterns;

// Pattern: navigation-chrome — see docs/patterns/navigation-chrome.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class NavigationChrome : IPatternDemo
{
    public string Slug => "navigation-chrome";
    public string Title => "Breadcrumbs and a navigation menu";
    public string Category => "Layout & navigation";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-navigation-chrome
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
    #endregion
}
