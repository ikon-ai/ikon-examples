namespace Ikon.App.Patterns.Patterns;

// Pattern: keyboard-list-and-detail — see docs/patterns/keyboard-list-and-detail.md.
// The example region below is the canonical body the doc extracts.
internal sealed class KeyboardListAndDetail : IPatternDemo
{
    public string Slug => "keyboard-list-and-detail";
    public string Title => "Keyboard list and detail";
    public string Category => "Navigation";

    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-keyboard-list-and-detail
    private sealed record Clause(string Id, string Title, string Text);

    private static readonly Clause[] Clauses = Enumerable.Range(1, 30)
        .Select(n => new Clause($"clause-{n}", $"{n}. Clause {n}",
            $"Clause {n} of the supply agreement. The supplier delivers the goods within {n + 4} working days " +
            "of the order, and the buyer pays within 30 days of the invoice."))
        .ToArray();

    private readonly ClientReactive<int> _selected = new(0);

    private void Render(IView view)
    {
        view.KeyboardListener(global: true, keys: ["j", "k", "J", "K"], onKeyDown: args =>
        {
            var step = args.Key is "j" or "J" ? 1 : -1;
            _selected.Value = Math.Clamp(_selected.Value + step, 0, Clauses.Length - 1);
            return Task.CompletedTask;
        });

        var selected = Clauses[_selected.Value];

        view.Row(["h-[420px] gap-4"], content: row =>
        {
            row.ScrollArea(["w-56 shrink-0 rounded-lg border border-secondary bg-card"], content: list =>
            {
                for (var i = 0; i < Clauses.Length; i++)
                {
                    var index = i;
                    var clause = Clauses[index];
                    var isSelected = clause == selected;

                    list.Box([isSelected ? "px-3 py-2 bg-brand-secondary text-brand-primary" : "px-3 py-2 text-secondary"],
                        key: clause.Id, onClick: () => _selected.Value = index, content: item =>
                        {
                            item.Text([Text.Body], clause.Title);

                            // Scrolls this row into view when the selection moves to it.
                            if (isSelected)
                            {
                                item.ScrollIntoView();
                            }
                        });
                }
            });

            row.ScrollArea(["flex-1 rounded-lg border border-secondary bg-card"], viewportStyle: ["px-5 py-3"], content: pane =>
            {
                foreach (var clause in Clauses)
                {
                    pane.Column([clause == selected ? "gap-1 py-3 border-l-2 border-brand ps-3" : "gap-1 py-3 ps-3"], key: clause.Id, content: section =>
                    {
                        section.Heading([Text.H4], clause.Title, level: 3);
                        section.Text([Text.Body, "text-secondary"], clause.Text);

                        // The detail pane follows the selection the same way.
                        if (clause == selected)
                        {
                            section.ScrollIntoView();
                        }
                    });
                }
            });
        });
    }
    #endregion
}
