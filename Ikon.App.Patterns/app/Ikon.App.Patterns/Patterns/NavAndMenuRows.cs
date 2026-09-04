namespace Ikon.App.Patterns.Patterns;

// Pattern: nav-and-menu-rows — see docs/patterns/nav-and-menu-rows.md.
// The docsnippet region is the three navigation surfaces side by side — page tabs, a sidebar rail
// and a menu row — because the point of the pattern is which token each one takes. The stubs
// outside it stand in for the sections a real app would route between.
internal sealed class NavAndMenuRows : IPatternDemo
{
    public string Slug => "nav-and-menu-rows";
    public string Title => "Nav and menu rows";
    public string Category => "Layout";

    public void RenderDemo(IView view) => RenderShell(view);

    private sealed record Section(string Key, string Label, string Icon);

    private static readonly Section[] Sections =
    [
        new("overview", "Overview", "layout-dashboard"),
        new("activity", "Activity", "activity"),
        new("files", "Files", "folder"),
    ];

    private static void RenderSectionBody(IView view, string label) =>
        view.Text([Text.Body, "p-4"], text: label);

    #region docsnippet:pattern-nav-and-menu-rows
    private readonly ClientReactive<string> _section = new("overview");
    private readonly Reactive<bool> _menuOpen = new(false);
    private readonly Reactive<string> _density = new("Comfortable");

    private void RenderShell(IView view)
    {
        // Page navigation between peer panels: content-width tabs on a shared rail. Not a
        // segmented control — Tabs.List/Tabs.Trigger would fill each label with the brand colour
        // and the row would read as three buttons.
        view.Tabs(
            value: _section.Value,
            onValueChange: async value => _section.Value = value,
            listStyle: [Tabs.NavList],
            triggerStyle: [Tabs.NavTriggerMd],
            tabs: Sections.Select(s =>
                new TabItem(s.Key, s.Label, v => RenderSectionBody(v, s.Label))));

        // The same destinations as a sidebar rail. NavItem carries the whole row: transparent at
        // rest, tinted on hover, and the active row differs in WEIGHT as well as colour.
        view.Column([NavPanel.Border, "w-56"], content: view =>
        {
            foreach (var s in Sections)
            {
                bool active = _section.Value == s.Key;

                view.Button([NavItem.Md, active ? NavItem.Active : NavItem.Default],
                    onClick: async () => _section.Value = s.Key,
                    content: v =>
                    {
                        v.Icon([NavItem.Icon], name: s.Icon);
                        v.Text([NavItem.Label], text: s.Label);
                    });
            }
        });

        // A menu row is a Button wearing Menu.Item — a full-width transparent row that highlights
        // on hover. Menu.Content must reach contentStyle: or the panel renders transparent.
        view.DropdownMenu(
            open: _menuOpen.Value,
            onOpenChange: async open => _menuOpen.Value = open,
            contentStyle: [Menu.Content],
            trigger: v => v.Button([Button.OutlineMd], text: "Density"),
            content: v =>
            {
                v.Text([Menu.Label], text: "Row density");

                string[] options = ["Comfortable", "Compact"];

                foreach (var option in options)
                {
                    v.Button([Menu.Item], text: option, onClick: async () =>
                    {
                        _density.Value = option;
                        _menuOpen.Value = false;
                    });
                }
            });

        RenderDensityToggle(view);
    }

    // A segmented control IS the right answer when the choices are parallel values of ONE setting,
    // and that is the only place the filled treatment belongs.
    private void RenderDensityToggle(IView view) =>
        view.Tabs(
            value: _density.Value,
            onValueChange: async value => _density.Value = value,
            listStyle: [Tabs.List],
            triggerStyle: [Tabs.Trigger],
            tabs:
            [
                new TabItem("Comfortable", "Comfortable", _ => { }),
                new TabItem("Compact", "Compact", _ => { }),
            ]);
    #endregion
}
