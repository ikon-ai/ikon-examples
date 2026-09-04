namespace Ikon.App.Patterns.Patterns;

// Pattern: board-move-without-drag — see docs/patterns/board-move-without-drag.md.
// The docsnippet region is the board plus the per-card Move menu that makes it operable without a
// pointer; the drag wiring itself is the drag-and-drop guide's, and wraps this unchanged.
internal sealed class BoardMoveWithoutDrag : IPatternDemo
{
    public string Slug => "board-move-without-drag";
    public string Title => "Board move without drag";
    public string Category => "Data";
    public void RenderDemo(IView view) => RenderBoard(view);

    private sealed record BoardCard(string Id, string Title, string ColumnId);

    private static readonly (string Id, string Name)[] Columns =
    [
        ("todo", "To do"), ("doing", "In progress"), ("done", "Done"),
    ];

    private readonly ReactiveList<BoardCard> _cards = new();

    public BoardMoveWithoutDrag()
    {
        _cards.AddRange(
        [
            new BoardCard("1", "Draft the launch note", "todo"),
            new BoardCard("2", "Wire the export button", "doing"),
            new BoardCard("3", "Fix the empty state", "done"),
        ]);
    }

    #region docsnippet:pattern-board-move-without-drag
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
    #endregion
}
