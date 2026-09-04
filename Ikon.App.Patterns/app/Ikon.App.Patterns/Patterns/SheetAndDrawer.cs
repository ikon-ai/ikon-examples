namespace Ikon.App.Patterns.Patterns;

// Pattern: sheet-and-drawer — see docs/patterns/sheet-and-drawer.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class SheetAndDrawer : IPatternDemo
{
    public string Slug => "sheet-and-drawer";
    public string Title => "Sheet and drawer: edge-anchored overlays";
    public string Category => "Modals & overlays";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-sheet-and-drawer
    private readonly ClientReactive<bool> _filtersOpen = new(false);
    private readonly ClientReactive<bool> _actionsOpen = new(false);

    private void Render(IView view)
    {
        view.Row(["gap-2"], content: row =>
        {
            // Sheet slides from an EDGE you choose and is the desktop shape: filters, details, a
            // settings panel beside the content it belongs to. Both Sheet and Drawer own their
            // header, so title:/description: are parameters, not something to render inside.
            row.Sheet(
                open: _filtersOpen.Value,
                onOpenChange: async open => _filtersOpen.Value = open,
                side: Side.Right,
                title: "Filters",
                description: "Narrow the list",
                trigger: t => t.Button(content: v => v.Text(text: "Filters")),
                content: panel => panel.Text(text: "…filter controls…"),
                footer: f => f.Button([Button.PrimaryMd],
                    onClick: () => _filtersOpen.Value = false,
                    content: v => v.Text(text: "Apply")));

            // Drawer comes from the BOTTOM with a drag handle, which is the touch idiom: an
            // action sheet, a picker, a confirm the thumb can reach. showHandle is what tells a
            // user it can be dragged away.
            row.Drawer(
                open: _actionsOpen.Value,
                onOpenChange: async open => _actionsOpen.Value = open,
                title: "Actions",
                showHandle: true,
                trigger: t => t.Button(content: v => v.Text(text: "Actions")),
                content: panel => panel.Column(["gap-2"], content: list =>
                {
                    list.Button(onClick: () => _actionsOpen.Value = false, content: v => v.Text(text: "Share"));
                    list.Button(onClick: () => _actionsOpen.Value = false, content: v => v.Text(text: "Duplicate"));
                }));

            // modal: false lets the page behind stay interactive -- right for an inspector the
            // user consults while working, wrong for anything that must be answered.
        });
    }
    #endregion
}
