<!-- mined-from: Threads -->
# Tree With Expand/Collapse — Recursive list with ancestry

A recursive renderer for parent/child hierarchical data (threads with sub-threads, files with sub-folders). Each node renders its own row, then conditionally recurses into children if its id is in an `expanded` set; otherwise it shows a compact `"N children — M active"` summary that the user can click to expand. Indentation is computed from depth (`depth * 12`) and applied as `pl-[{n}px]`.

## When to use

Parent-child structures where the depth is unbounded but most users only care about the top 1-2 levels at any given time. Better than a flat list when the relationship matters; better than fully-expanded everywhere when the tree is large. Pair with the dynamic-indent technique to render hundreds of nodes cheaply — no wrapper element per level.

## Snippet

```csharp
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
        if (children.Count == 0) continue;

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
```

## Notes

- Expanded ids live in a `ClientReactiveList<string>` — `_expandedChildrenIds.Add(id)` mutates and notifies in one call (`.Value.Add` does not compile), `Contains` is a tracked read, and the value is per-client so two browser tabs can have different rows expanded. To collapse, `_expandedChildrenIds.Remove(id)`.
- The collapsed-summary row gives users useful information without the noise of N nested items: "5 children — 2 active" is more actionable than a chevron alone. Compute the summary from the unflattened list so it stays accurate as children update.
- Indent via `pl-[{n}px]` arbitrary value; this is one of the few cases where it's idiomatic to compute the value at render time. Crosswind handles the dynamic class string.
- For very deep trees (depth >5), cap the indent at a max so the rightmost nodes don't disappear off-screen: `Math.Min(depth, 5) * 12`.
- Pass the *full* `allThreads` list down so each level can find its own children with `Where(t => t.ParentId == ...)` — don't pre-build a parent→children dictionary; the linear scan is fast enough for normal hierarchies and avoids stale-cache bugs.

## See also

- `kanban-multi-column` — the flat list counterpart when hierarchy doesn't matter
- `expandable-detail-card` — single row that opens to show its detail (no recursion)
