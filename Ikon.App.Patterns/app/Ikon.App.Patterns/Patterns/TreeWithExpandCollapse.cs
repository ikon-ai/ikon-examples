namespace Ikon.App.Patterns.Patterns;

// Pattern: tree-with-expand-collapse — see docs/patterns/tree-with-expand-collapse.md.
// The record, status enum, per-client selection and SelectThread stand in for the app's real thread
// model; the example region is the canonical recursive renderer the doc extracts. The demo hands it
// the root threads and the whole list, as an app would.
internal sealed class TreeWithExpandCollapse : IPatternDemo
{
    public string Slug => "tree-with-expand-collapse";
    public string Title => "Tree with expand-collapse";
    public string Category => "Navigation";

    public void RenderDemo(IView view)
    {
        view.Column(["gap-2 max-w-sm"], content: col =>
        {
            col.Column(["rounded-xl border border-secondary py-1.5"], content: tree =>
                RenderThreadTree(tree, _sampleThreads.Where(t => t.ParentId is null).ToList(), _sampleThreads, depth: 0));
            col.Row(["gap-2 items-center"], content: row =>
            {
                row.Button([Button.GhostSm], text: "Collapse all", onClick: async () => _expandedChildrenIds.Clear());
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

    private readonly ClientReactive<string?> _selectedThreadId = new(null);

    private Task SelectThread(string threadId)
    {
        _selectedThreadId.Value = threadId;
        return Task.CompletedTask;
    }

    #region example:pattern-tree-with-expand-collapse
    private readonly ClientReactiveList<string> _expandedChildrenIds = new();

    private void RenderThreadTree(UIView view, List<ThreadInfo> threads, List<ThreadInfo> allThreads, int depth)
    {
        foreach (var thread in threads)
        {
            var isSelected = thread.Id == _selectedThreadId.Value;
            var capturedThread = thread;
            var indent = depth * 12;

            view.Box([$"py-2 rounded-lg mx-1.5 mb-px {(isSelected ? "bg-accent" : "hover:bg-accent/40")}"], content: view =>
            {
                view.Box([$"pl-[{indent + 12}px] pr-3"], content: view =>
                {
                    view.Row(["items-center gap-2"], content: view =>
                    {
                        view.Box(["flex-1 cursor-pointer"],
                            onClick: async () => await SelectThread(capturedThread.Id),
                            content: view =>
                            {
                                view.Text(["text-xs font-medium truncate"], capturedThread.Title);
                            });
                    });
                });
            });

            var children = allThreads.Where(t => t.ParentId == capturedThread.Id).ToList();

            if (children.Count == 0)
            {
                continue;
            }

            var childrenExpanded = _expandedChildrenIds.Contains(capturedThread.Id);

            if (childrenExpanded)
            {
                RenderThreadTree(view, children, allThreads, depth + 1);
            }
            else
            {
                var childIndent = (depth + 1) * 12;
                var doneCount = children.Count(c => c.Status == ThreadStatus.Done);
                var activeCount = children.Count(c => c.Status is ThreadStatus.Active or ThreadStatus.Pending);
                var summary = doneCount == children.Count
                    ? $"{children.Count} children — all done"
                    : activeCount > 0
                        ? $"{children.Count} children — {activeCount} active"
                        : $"{children.Count} children";

                view.Box(["py-0.5 cursor-pointer hover:bg-accent/40 rounded-md mx-1.5"],
                    onClick: async () => _expandedChildrenIds.Add(capturedThread.Id),
                    content: view =>
                    {
                        view.Box([$"pl-[{childIndent + 12}px] pr-3"], content: view =>
                        {
                            view.Row(["items-center gap-1"], content: view =>
                            {
                                view.Icon(["w-2.5 h-2.5 text-muted-foreground/50"], name: "chevron-right");
                                view.Text(["text-[10px] text-muted-foreground/50"], summary);
                            });
                        });
                    });
            }
        }
    }
    #endregion
}
