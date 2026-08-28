# Overlay Selection — Picking Modal vs Drawer vs Popover vs Toast

Five overlay shapes exist and they are not interchangeable. The recurring defect is reaching for a modal by default: a modal interrupts, so using one for a detail view destroys the list context the user was working in, and using one for a confirmation the user will hit fifty times a day is friction with no payoff.

Pick by **task depth** and **whether the parent context must survive**.

## When to use

Any time something has to appear over the current surface. Decide with the table before writing the component — the fix afterwards is a restructure, not a class change.

| Shape | Use when | Parent context |
|---|---|---|
| `AlertDialog` | Destructive or irreversible, and confirming genuinely prevents error | Interrupted, deliberately |
| `Dialog` | One short focused task that must be finished or abandoned | Interrupted |
| Slide-in panel | Inspect or edit detail *while* the list / board / canvas stays visible | **Preserved** — this is the point |
| `Popover` | Compact controls or info anchored to the thing that opened it | Preserved |
| `Tooltip` | A short hint on hover/focus. Never the only home for an instruction or an action | Preserved |
| `Toast` | Transient confirmation that needs no decision | Preserved |

## Snippet

```csharp
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
```

## Notes

- **The panel is the one most often got wrong.** If the user's next action is likely "now the next record", the list must still be there. A modal forces close → re-find → re-open on every single record.
- `AlertDialog` is for destructive and irreversible only. A confirm on an everyday reversible action trains people to click through dialogs without reading, which is exactly what breaks them when a real one appears.
- Long multi-step work does not belong in a modal. If it needs more than one screen, it needs a route or a full surface.
- A `Tooltip` must never be the only place an instruction lives — it does not exist on touch, and assistive tech may not reach it. Put the requirement in a label or helper text and let the tooltip add nuance.
- A `Toast` carrying "Undo" needs the same recovery reachable somewhere permanent (a history list, a trash view). Critical recoverability inside a disappearing pill is a data-loss bug.
- Give an icon-only close button an accessible name via `props` `aria-label` — unnamed icon buttons are invisible to assistive tech and to the app validator.
- Only one overlay should own the Escape key at a time; see `global-keyboard-shortcuts` for the Esc-stack ordering when several can be open.

## See also

- `slide-in-side-panel` — the full backdrop-plus-animation drawer when the panel should overlay rather than sit beside.
- `destructive-confirm-dialog` — the id-driven confirm modal in full.
- `toast-notifications` — tone-coded auto-dismissing pills with age-based expiry.
- `global-keyboard-shortcuts` — Esc-stack ordering across stacked overlays.
