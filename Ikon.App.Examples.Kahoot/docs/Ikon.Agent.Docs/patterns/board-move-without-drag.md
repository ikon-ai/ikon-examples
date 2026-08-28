# Board Move Without Drag — the Path That Is Not the Pointer

A board is one of the most-generated app shapes, and drag is the obvious way to move a card. Drag is also unavailable to keyboard users and assistive tech, and unreliable on touch — so a board whose *only* move is drag is a board a large share of people cannot operate at all.

The fix is small: a per-card menu listing the other columns. Drag stays the fast path for a mouse; the menu is the one that always works. Both call the same move method.

## When to use

Any board, pipeline or kanban — and the same shape covers any reorder or reassign that a pointer gesture would otherwise own: sortable lists, priority ordering, moving an item between groups.

The drag wiring itself is not repeated here — `DndContext` / `Droppable` / `Draggable` / `DragOverlay` are in the drag-and-drop section of the app guide, and wrap this board unchanged. What that guide does not show is the half below.

## Snippet

```csharp
private readonly Reactive<string?> _menuOpenFor = new(null);

/// The ONE move operation. Drag calls it from onDragEnd; the menu calls it from a click. Both
/// paths must go through the same method, or they drift and only one of them stays correct.
private void MoveCard(string cardId, string toColumnId)
{
    var index = -1;

    for (var i = 0; i < _cards.Count; i++)
    {
        if (_cards[i].Id == cardId) { index = i; break; }
    }

    if (index >= 0 && _cards[index].ColumnId != toColumnId)
    {
        _cards[index] = _cards[index] with { ColumnId = toColumnId };
    }
}

private void RenderBoard(IView view)
{
    view.Row(["flex w-full items-start gap-4 overflow-x-auto"], content: view =>
    {
        foreach (var (columnId, columnName) in Columns)
        {
            var column = columnId;

            view.Column([Card.Default, "w-64 shrink-0 p-3", Layout.Column.Sm], content: v =>
            {
                v.Text([Text.Caption], text: columnName.ToUpperInvariant());

                foreach (var card in _cards)
                {
                    if (card.ColumnId == column) { RenderCard(v, card); }
                }
            });
        }
    });
}

private void RenderCard(IView view, BoardCard card)
{
    view.Row([Card.Subtle, "items-start justify-between gap-2 p-2"], content: view =>
    {
        view.Text([Text.Body, "text-sm flex-1 min-w-0"], text: card.Title);

        // The non-drag path. Drag stays the fast way for a mouse; this is the one that works
        // for keyboard, screen readers and every touch device where drag is unreliable.
        view.DropdownMenu(
            open: _menuOpenFor.Value == card.Id,
            onOpenChange: async open => _menuOpenFor.Value = open ? card.Id : null,
            contentStyle: [Menu.Content],
            trigger: v => v.Button([Button.GhostMd, Button.IconXs],
                props: new Dictionary<string, object> { ["aria-label"] = $"Move {card.Title}" },
                content: t => t.Icon([Icon.Xs], name: "ellipsis-vertical")),
            content: v =>
            {
                v.Text([Menu.Label], text: "Move to");

                foreach (var (columnId, columnName) in Columns)
                {
                    if (columnId == card.ColumnId) { continue; }

                    var target = columnId;
                    v.Button([Menu.Item], text: columnName, onClick: async () =>
                    {
                        MoveCard(card.Id, target);
                        _menuOpenFor.Value = null;
                    });
                }
            });
    });
}
```

## Notes

- **One `MoveCard` method, two callers.** Drag calls it from `onDragEnd`, the menu calls it from a click. Give each path its own mutation and they drift within a week — one gets the reorder-within-column case, the other doesn't.
- Mutate the item back through the `ReactiveList` indexer (`_cards[index] = _cards[index] with { ColumnId = target }`) — the list notifies on ITS mutators, not on a field write inside an item.
- The menu button is icon-only, so it needs an accessible name through `props` `aria-label`, and the name should say which card it acts on ("Move Draft the launch note") — a board of identical "Move" buttons is no better than none.
- Omit the current column from the menu. A "move to where it already is" row is a dead control.
- `Menu.Content` / `Menu.Label` / `Menu.Item` carry the whole menu surface; `Menu.Content` must be passed to `contentStyle:` or the panel renders transparent.
- Keep one open-menu reactive for the whole board (`_menuOpenFor`), not one per card — a bool per card leaks state as cards move and lets two menus open at once.
- The same reasoning covers reorder-only lists: `view.SortableList` gives you drag for free, and still needs Move up / Move down beside it.
- Don't add a drag hint ("drag me") as the discoverability fix. The menu IS the discoverable path; drag is the shortcut.

## See also

- `overlay-selection` — why this is a menu anchored to its trigger and not a dialog.
- `record-list-toolbar` — the table view of the same records, and the note on not reordering a row under the pointer.
- `inline-list-cell-edit` — editing a card's text in place once it has arrived.
