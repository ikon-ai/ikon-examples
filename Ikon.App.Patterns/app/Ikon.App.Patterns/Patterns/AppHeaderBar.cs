namespace Ikon.App.Patterns.Patterns;

// Pattern: app-header-bar — see docs/patterns/app-header-bar.md.
// The demo draws the bar twice, the phone layout (the menu button) and the desktop layout (the links
// inline), so both can be compared without resizing the page.
internal sealed class AppHeaderBar : IPatternDemo
{
    public string Slug => "app-header-bar";
    public string Title => "App header bar";
    public string Category => "Navigation";

    public void RenderDemo(IView view)
    {
        view.Column(["gap-4 w-full max-w-2xl"], content: col =>
        {
            col.Text([Text.Caption], "Wide screen (md and up)");
            col.Box(["rounded-lg border overflow-hidden"], content: box => RenderAppHeader(box));
            col.Text([Text.Caption], "Phone (below md): resize the window to see the menu button replace the links");
        });
    }

    private readonly ClientReactive<bool> _menuOpen = new(false);
    private readonly Reactive<string> _page = new("Projects");

    #region example:pattern-app-header-bar
    private void RenderAppHeader(UIView view)
    {
        // One row that never wraps: the title shrinks and truncates, the actions keep their size
        view.Row(["flex items-center justify-between gap-2 px-4 py-2"], content: bar =>
        {
            bar.Text([Text.H4, "min-w-0 truncate"], "Quarterly planning workspace");

            // md and up: the links sit in the bar
            bar.Row(["hidden md:flex items-center gap-1 shrink-0"], content: links =>
            {
                foreach (var page in new[] { "Projects", "Reports", "Settings" })
                {
                    links.Button([Button.GhostSm], text: page, onClick: async () => _page.Value = page);
                }
            });

            // Below md: the same actions in a menu behind one icon button
            bar.DropdownMenu(open: _menuOpen.Value, onOpenChange: async o => _menuOpen.Value = o,
                contentStyle: [Menu.Content],
                trigger: trigger => trigger.Button([Button.GhostMd, Button.Icon, "md:hidden shrink-0"], ariaLabel: "Open menu", content: icon => icon.Icon(name: "menu")),
                content: menu =>
                {
                    foreach (var page in new[] { "Projects", "Reports", "Settings" })
                    {
                        menu.Button([Menu.Item], text: page, onClick: async () =>
                        {
                            _page.Value = page;
                            _menuOpen.Value = false;
                        });
                    }
                });
        });
    }
    #endregion
}
