namespace Ikon.App.Patterns.Patterns;

// Pattern: overlay-selection — see docs/patterns/overlay-selection.md.
// The docsnippet region shows all four overlay shapes side by side so the choice between them is
// legible in one screen; the stubs outside it stand in for the row model, the store, and the detail
// and filter bodies each overlay hosts.
internal sealed class OverlaySelection : IPatternDemo
{
    public string Slug => "overlay-selection";
    public string Title => "Overlay selection";
    public string Category => "Layout";
    public void RenderDemo(IView view) => Render(view);

    private sealed record Row(string Id, string Name);

    private readonly ReactiveList<Row> _rows = new();

    public OverlaySelection()
    {
        _rows.AddRange([new Row("1", "Northwind Freight"), new Row("2", "Lumen Health")]);
    }

    private static void RenderDetail(IView view, Row row) =>
        view.Text([Text.Body], text: $"Everything known about {row.Name}.");

    private static void RenderFilterControls(IView view) =>
        view.Text([Text.Caption], text: "Status, owner, stage…");

    private void DeleteRow(string id) => _rows.RemoveAll(r => r.Id == id);

    #region docsnippet:pattern-overlay-selection
    private readonly Reactive<Row?> _inspecting = new(null);
    private readonly Reactive<string?> _pendingDelete = new(null);
    private readonly Reactive<bool> _filterOpen = new(false);
    private readonly Reactive<bool> _saved = new(false);

    private void Render(IView view)
    {
        // DESTRUCTIVE → AlertDialog. Confirmation earns its interruption only here.
        view.AlertDialog(
            open: _pendingDelete.Value is not null,
            onOpenChange: async open => { if (!open) { _pendingDelete.Value = null; } },
            overlayStyle: [AlertDialog.Overlay], contentStyle: [AlertDialog.Content],
            title: "Delete this row?",
            titleStyle: [AlertDialog.Title],
            description: "This cannot be undone.",
            descriptionStyle: [AlertDialog.Description],
            footerStyle: [AlertDialog.Footer],
            cancelLabel: "Keep", cancelStyle: [AlertDialog.Cancel],
            actionLabel: "Delete", actionStyle: [Button.ErrorMd],
            onAction: async () =>
            {
                if (_pendingDelete.Value is { } id) { DeleteRow(id); }
                _pendingDelete.Value = null;
            });

        // DETAIL BESIDE THE LIST → a panel, NOT a modal. The list stays on screen and keeps its
        // scroll position, so the user can move to the next record without re-finding it.
        view.Row(["flex w-full gap-4"], content: view =>
        {
            view.Column([Layout.Column.Sm, "flex-1 min-w-0"], content: view =>
            {
                foreach (var row in _rows)
                {
                    var inspected = row;
                    view.Box([Card.Interactive, "p-3"], onClick: async () => _inspecting.Value = inspected,
                        content: v => v.Text([Text.Body], text: row.Name));
                }
            });

            if (_inspecting.Value is { } open)
            {
                view.Column([Card.Elevated, "w-80 shrink-0 p-4"], content: view =>
                {
                    view.Row([Layout.Row.SpaceBetween], content: v =>
                    {
                        v.Text([Text.H3], text: open.Name);
                        v.Button([Button.GhostMd, Button.Icon],
                            onClick: async () => _inspecting.Value = null,
                            props: new Dictionary<string, object> { ["aria-label"] = "Close detail" },
                            content: inner => inner.Icon([Icon.Default], name: "x"));
                    });
                    RenderDetail(view, open);
                });
            }
        });

        // COMPACT CONTROLS ANCHORED TO THEIR TRIGGER → Popover. A modal here would be theatre.
        view.Popover(
            open: _filterOpen.Value,
            onOpenChange: async open => _filterOpen.Value = open,
            contentStyle: [Popover.Content],
            trigger: v => v.Button([Button.OutlineMd], text: "Filters"),
            contentSlot: RenderFilterControls);

        // CONFIRMATION THAT NEEDS NO DECISION → Toast. Never put a recovery path here alone; it
        // disappears, and an Undo the user missed is an Undo that does not exist.
        view.Toast(
            open: _saved.Value,
            onOpenChange: async open => _saved.Value = open,
            viewportStyle: [Toast.ViewportBottomCenter], toastStyle: [Toast.Base],
            title: "Saved", titleStyle: [Toast.Title],
            durationMs: 2500, showClose: true, closeStyle: [Toast.Close]);
    }
    #endregion
}
