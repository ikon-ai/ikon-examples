namespace Ikon.App.Patterns.Patterns;

// Pattern: shared-list-ai-cleanup — see docs/patterns/shared-list-ai-cleanup.md.
// The whole example is self-contained, so the docsnippet region carries CRUD, the AI transform and the
// UI together — no stubs are needed outside it.
internal sealed class SharedListAiCleanup : IPatternDemo
{
    public string Slug => "shared-list-ai-cleanup";
    public string Title => "Shared list AI cleanup";
    public string Category => "AI";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-shared-list-ai-cleanup
    public sealed record TodoItem(string Id, string Text);

    private readonly ReactiveList<TodoItem> _items = new();
    private readonly Reactive<string> _draft = new("");
    private readonly Reactive<bool> _busy = new(false);

    private void Add()
    {
        var text = _draft.Value.Trim();

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _items.Add(new TodoItem(Guid.NewGuid().ToString("N"), text));
        _draft.Value = "";
    }

    private void Remove(string id) =>
        _items.RemoveAll(i => i.Id == id);

    private async Task AiCleanupAsync()
    {
        if (_busy.Value || _items.Count == 0)
        {
            return;
        }

        using var _ = _busy.AsToken();
        var current = string.Join("\n", _items.Select(i => $"- {i.Text}"));
        var cleaned = await Emerge.AskAsync<List<string>>(
            $"Deduplicate and prioritize this todo list:\n{current}\n\nReturn the cleaned list, most important first.");
        _items.ReplaceAll(cleaned.Select(t => new TodoItem(Guid.NewGuid().ToString("N"), t)));
    }

    private void Render(IView view)
    {
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
    }
    #endregion
}
