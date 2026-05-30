# Kanban — Multi-Column Board

Three columns (Todo / Doing / Done), cards flow between them via Move buttons. Plus an "AI Plan" button that fills the board from a project description.

## When to use

Project board, sprint planning, content pipeline (idea → drafting → published), recruiting funnel, sales stages. Any "items move through ordered states" shape.

## Snippet

```csharp
public sealed record Card(string Id, string Title, string? Description, int ColumnIndex);

private static readonly string[] Columns = ["Todo", "In Progress", "Done"];
private readonly Reactive<List<Card>> _cards = new([]);
private readonly Reactive<string> _newTitle = new("");
private readonly Reactive<bool> _busy = new(false);

private void AddCard()
{
    var title = _newTitle.Value.Trim();
    if (string.IsNullOrEmpty(title)) return;
    _cards.Value = [.. _cards.Value, new Card(Guid.NewGuid().ToString("N"), title, null, 0)];
    _newTitle.Value = "";
}

private void Move(string id, int delta)
{
    _cards.Value = _cards.Value
        .Select(c => c.Id == id
            ? c with { ColumnIndex = Math.Clamp(c.ColumnIndex + delta, 0, Columns.Length - 1) }
            : c)
        .ToList();
}

private async Task AiPlanAsync(string description)
{
    if (_busy.Value) return;
    using var _ = _busy.AsToken();
    var (generated, _) = await Emerge.Run<List<Card>>(LLMModel.Claude45Haiku, new KernelContext(),
        pass => { pass.Command = $"Project description: {description}\n\nGenerate 6-10 starter kanban cards. Distribute across columns 0=Todo, 1=In Progress, 2=Done based on what's actionable now vs blocked vs done."; })
        .FinalAsync();
    _cards.Value = [.. _cards.Value, .. (generated ?? []).Select(g => g with { Id = Guid.NewGuid().ToString("N") })];
}

// UI:
view.Row(["gap-4 p-4 items-start"], content: view =>
{
    for (int colIdx = 0; colIdx < Columns.Length; colIdx++)
    {
        var idx = colIdx;
        var cardsInCol = _cards.Value.Where(c => c.ColumnIndex == idx).ToList();
        view.Column(["flex-1 bg-surface rounded-lg p-3 gap-2 min-w-0"], content: view =>
        {
            view.Row(["items-center justify-between"], content: v =>
            {
                v.Text(["text-sm font-semibold uppercase tracking-wider"], text: Columns[idx]);
                v.Text(["text-xs text-muted-foreground"], text: $"{cardsInCol.Count}");
            });

            if (idx == 0)
            {
                view.Row(["gap-2"], content: v =>
                {
                    v.TextField(["flex-1"], value: _newTitle.Value, placeholder: "+ New card",
                        onValueChange: async x => _newTitle.Value = x, onSubmit: async _ => AddCard());
                });
            }

            if (cardsInCol.Count == 0)
            {
                view.Box(["text-center text-xs text-muted-foreground p-6 border border-dashed rounded-md"], content: v =>
                    v.Text(text: idx == 0 ? "Drop a card here" : "Nothing here yet"));
            }
            foreach (var card in cardsInCol)
            {
                view.Box(["bg-background rounded-md p-3 gap-1 hover:shadow-md transition-shadow duration-150 cursor-grab"], content: v =>
                {
                    v.Text(["text-sm font-medium"], text: card.Title);
                    if (!string.IsNullOrEmpty(card.Description))
                        v.Text(["text-xs text-muted-foreground"], text: card.Description);
                    v.Row(["gap-1 mt-2"], content: vv =>
                    {
                        vv.Button(style: [Button.GhostMd, "text-xs"], disabled: idx == 0,
                            onClick: () => Move(card.Id, -1), content: c => c.Text(text: "←"));
                        vv.Button(style: [Button.GhostMd, "text-xs"], disabled: idx == Columns.Length - 1,
                            onClick: () => Move(card.Id, +1), content: c => c.Text(text: "→"));
                    });
                });
            }
        });
    }
});
```

## Notes

- Columns are computed from `_cards`'s `ColumnIndex`, not stored separately. One source of truth.
- Move uses `c with { ColumnIndex = ... }` (record `with` expression) and reassigns the whole list.
- Multi-user safe: shared `Reactive<List<Card>>`, every client sees the same board updates.
- AI Plan uses **structured output** (`Emerge.Run<List<Card>>`) — the LLM returns typed cards directly, no JSON parsing.
- Drag-and-drop: this snippet uses Move buttons for clarity; for true HTML5 drag-and-drop, see ui-components guide's drag-and-drop section. Buttons are accessible and work on mobile, drag-and-drop is nice-to-have on top.
- Each column has its own empty-state branch.

## See also

- `shared-list-ai-cleanup` — single-list version of the same idea.
- `multi-user-game` — different shared/per-client mix for game flow.
- `ui-components` (top-level) — `view.DragSource` / `view.DropTarget` for actual drag-and-drop.
