namespace Ikon.App.Patterns.Patterns;

// Pattern: tree-with-expand-collapse — see docs/patterns/tree-with-expand-collapse.md.
// The record and status enum stand in for the app's real thread model; the demo hands the region
// the whole list, as an app would.
internal sealed class TreeWithExpandCollapse : IPatternDemo
{
    public string Slug => "tree-with-expand-collapse";
    public string Title => "Tree with expand-collapse";
    public string Category => "Navigation";

    public void RenderDemo(IView view)
    {
        view.Column(["gap-2 max-w-sm"], content: col =>
        {
            col.Box(["rounded-xl border border-secondary p-1.5"], content: tree => RenderThreadTree(tree, _sampleThreads));
            col.Row(["gap-2 items-center"], content: row =>
            {
                row.Button([Button.GhostSm], text: "Collapse all", onClick: async () => _expanded.Clear());
                row.Text(["text-xs text-muted-foreground"],
                    _selectedThreadId.Value is { } id ? $"Selected: {_sampleThreads.First(t => t.Id == id).Title}" : "Nothing selected");
            });
        });
    }

    private readonly List<ThreadInfo> _sampleThreads =
    [
        new("market", "Market sizing for e-bikes in Finland", null, ThreadStatus.Active),
        new("sources", "Gather sales statistics", "market", ThreadStatus.Done),
        new("customs", "Read customs import data 2023-2025", "sources", ThreadStatus.Done),
        new("retail", "Survey retailer price lists", "sources", ThreadStatus.Done),
        new("model", "Build the adoption model", "market", ThreadStatus.Active),
        new("draft", "Draft the summary", "market", ThreadStatus.Pending),
        new("pricing", "Competitor pricing review", null, ThreadStatus.Done),
        new("pricing-eu", "Compare EU list prices", "pricing", ThreadStatus.Done),
        new("pricing-us", "Compare US list prices", "pricing", ThreadStatus.Done),
        new("support", "Answer a dealer question about warranty terms", null, ThreadStatus.Done),
    ];

    private enum ThreadStatus { Pending, Active, Done }

    private sealed record ThreadInfo(string Id, string Title, string? ParentId, ThreadStatus Status);

    #region example:pattern-tree-with-expand-collapse
    // Which branches are open, per client: one tab expanding a branch leaves another tab's tree alone.
    private readonly ExpandedSet _expanded = new(["market"]);
    private readonly ClientReactive<string?> _selectedThreadId = new(null);

    private void RenderThreadTree(UIView view, IReadOnlyList<ThreadInfo> allThreads)
    {
        var childrenOf = allThreads.ToLookup(t => t.ParentId ?? "");

        view.TreeView(
            roots: childrenOf[""].ToList(),
            id: t => t.Id,
            label: t => LabelFor(t, childrenOf[t.Id].ToList()),
            children: t => childrenOf[t.Id].ToList(),
            expanded: _expanded,
            selectedId: _selectedThreadId.Value,
            icon: t => t.Status == ThreadStatus.Done ? "circle-check" : "circle-dashed",
            onSelect: async t => _selectedThreadId.Value = t.Id);
    }

    // A collapsed branch says what is inside it, so the user can tell whether it is worth opening.
    private string LabelFor(ThreadInfo thread, List<ThreadInfo> children)
    {
        if (children.Count == 0 || _expanded.IsExpanded(thread.Id))
        {
            return thread.Title;
        }

        var open = children.Count(c => c.Status != ThreadStatus.Done);
        return open == 0 ? $"{thread.Title} · all {children.Count} done" : $"{thread.Title} · {open} of {children.Count} open";
    }
    #endregion
}
