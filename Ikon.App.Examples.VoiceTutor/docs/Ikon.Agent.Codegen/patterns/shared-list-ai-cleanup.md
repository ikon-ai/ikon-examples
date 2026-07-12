# Shared List with AI Transformation

A shared collection (todo, notes, tasks, etc.) with CRUD plus an AI button that transforms the whole list (deduplicate, prioritize, summarize, group).

## When to use

Any "list of items, occasionally let AI tidy them" shape — todos, notes, tags, ideas, quotes.

## Snippet

```csharp
public sealed record TodoItem(string Id, string Text);

private readonly ReactiveList<TodoItem> _items = new();
private readonly Reactive<string> _draft = new("");
private readonly Reactive<bool> _busy = new(false);

private void Add()
{
    var text = _draft.Value.Trim();
    if (string.IsNullOrEmpty(text)) return;
    _items.Add(new TodoItem(Guid.NewGuid().ToString("N"), text));
    _draft.Value = "";
}

private void Remove(string id) =>
    _items.RemoveAll(i => i.Id == id);

private async Task AiCleanupAsync()
{
    if (_busy.Value || _items.Count == 0) return;
    using var _ = _busy.AsToken();
    var current = string.Join("\n", _items.Select(i => $"- {i.Text}"));
    var cleaned = await Emerge.AskAsync<List<string>>(
        $"Deduplicate and prioritize this todo list:\n{current}\n\nReturn the cleaned list, most important first.");
    _items.ReplaceAll(cleaned.Select(t => new TodoItem(Guid.NewGuid().ToString("N"), t)));
}

// UI:
view.Row(["gap-2 p-4"], content: v =>
{
    v.TextField(["flex-1"], value: _draft.Value, placeholder: "What needs doing?",
        onValueChange: async x => _draft.Value = x, onSubmit: async _ => Add());
    v.Button(style: [Button.Default, "transition-colors duration-150 hover:opacity-90"],
        onClick: () => Add(), content: c => c.Text(text: "Add"));
    v.Button(style: [Button.SecondaryMd, _busy.Value ? "opacity-50" : ""],
        disabled: _busy.Value || _items.Count == 0, onClick: AiCleanupAsync,
        content: c => c.Text(text: _busy.Value ? "Cleaning…" : "AI Cleanup"));
});

if (_items.Count == 0)
{
    view.Box(["text-center text-muted-foreground p-12"], content: v =>
        v.Text(text: "No items yet — add one above to get started."));
}
else
{
    view.Column(["gap-1 p-4"], content: view =>
    {
        foreach (var item in _items)
        {
            view.Row(["items-center gap-2 p-3 rounded-md hover:bg-surface transition-colors duration-150"], content: v =>
            {
                v.Text(["flex-1"], text: item.Text);
                v.Button(style: [Button.GhostMd], onClick: () => Remove(item.Id),
                    content: c => c.Text(text: "Remove"));
            });
        }
    });
}
```

## Notes

- `_items` is a `ReactiveList<TodoItem>` — call the mutation on the reactive itself (`Add`, `RemoveAll`, `ReplaceAll`); each call notifies once, and `.Value.Add` does not compile.
- Empty input is rejected at the start of `Add()`.
- Empty list state has its own visible branch (no items yet — "add one above to get started").
- AI Cleanup is gated by both `_busy` and `Count == 0` (no point cleaning empty list).
- Use `Emerge.Run<List<string>>` for **structured** output — the LLM returns a typed list directly, no JSON parsing.

## See also

- `busy-flag-loading` — generic async pattern.
- `chatbot-streaming` — similar transcript shape, single-LLM-call form.
- `kanban-multi-column` — same items shape spread across multiple columns.
