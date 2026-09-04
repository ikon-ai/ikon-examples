namespace Ikon.App.Patterns.Patterns;

// Pattern: tree-with-expand-collapse — see docs/patterns/tree-with-expand-collapse.md.
// The record, status enum, per-client selection and SelectThread stand in for the app's real thread
// model; the docsnippet region is the canonical recursive renderer the doc extracts.
internal sealed class TreeWithExpandCollapse : IPatternDemo
{
    public string Slug => "tree-with-expand-collapse";
    public string Title => "Tree with expand-collapse";
    public string Category => "Navigation";
    public void RenderDemo(IView view) => RenderThreadTree(view, _sampleThreads, _sampleThreads, depth: 0);

    private readonly List<ThreadInfo> _sampleThreads =
    [
        new("root", "Research task", null, ThreadStatus.Active),
        new("child-a", "Gather sources", "root", ThreadStatus.Done),
        new("child-b", "Draft summary", "root", ThreadStatus.Pending),
    ];

    private enum ThreadStatus { Pending, Active, Done }

    private sealed record ThreadInfo(string Id, string Title, string? ParentId, ThreadStatus Status);

    private readonly ClientReactive<string?> _selectedThreadId = new(null);

    private Task SelectThread(string threadId) => throw new NotImplementedException();

    #region docsnippet:pattern-tree-with-expand-collapse
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
