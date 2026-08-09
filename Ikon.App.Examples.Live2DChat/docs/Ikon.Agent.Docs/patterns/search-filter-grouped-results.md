<!-- mined-from: NoBrainer -->
# Search Filter With Grouped Results — One Field, Many Sections

A single search `TextField` filters a flat object collection by name/key/type substring (case-insensitive). The filtered results are partitioned into "Canonical" / "Frontier" buckets, each bucket grouped by `Type`, and rendered as separate sections. Empty filter shows everything; non-empty filter shows a friendly "No results for X" with the appropriate icon.

## When to use

You have a large mostly-flat collection (entities, files, contacts, beliefs, knowledge graph nodes) that benefits from sectioned presentation. Users want to type a name and see it bubble up wherever it lives in the hierarchy without losing the section structure. Better than a flat results list when the type/section is meaningful at the moment of choice.

## Snippet

```csharp
private void RenderWorldModelView(UIView view)
{
    var snapshot = _worldModel.GetSnapshot();
    var filter = _worldModelFilter.Value?.Trim() ?? "";
    var filteredObjects = snapshot.Objects;

    if (filter.Length > 0)
    {
        filteredObjects = filteredObjects.Where(o =>
            o.Props.Values.Any(v => v.Contains(filter, StringComparison.OrdinalIgnoreCase))
            || o.MatchKey?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true
            || o.Type.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    var canonical = filteredObjects.Where(o => o.Maturity >= Maturity.Stable).ToList();
    var frontier = filteredObjects.Where(o => o.Maturity < Maturity.Stable
        && o.RetentionState != RetentionState.Archived).ToList();

    view.ScrollArea(rootStyle: ["flex-1 min-h-0"], viewportStyle: ["px-5 py-4"], content: view =>
    {
        view.Column(["gap-4 max-w-3xl mx-auto w-full"], content: view =>
        {
            view.TextField(
                ["text-sm bg-black/[0.03] border border-black/[0.06] rounded-lg px-3 py-2 placeholder:text-black/20"],
                placeholder: "Search entities...",
                value: _worldModelFilter.Value,
                onValueChange: async v => _worldModelFilter.Value = v);

            if (filteredObjects.Count == 0)
            {
                view.Column(["items-center py-16 gap-3"], content: view =>
                {
                    view.Icon(["w-8 h-8 text-black/10"], name: filter.Length > 0 ? "search" : "globe");
                    view.Text(["text-sm text-black/20 font-light"],
                        filter.Length > 0 ? $"No results for \"{filter}\"" : "World model is empty");
                });
            }
            else
            {
                if (canonical.Count > 0)
                {
                    view.Text(["text-[10px] font-medium tracking-widest uppercase text-violet-600/40 mb-1"], "CANONICAL");
                    foreach (var group in canonical.GroupBy(o => o.Type).OrderByDescending(g => g.Count()))
                    {
                        RenderTypeGroup(view, group.Key, group.ToList());
                    }
                }

                if (frontier.Count > 0)
                {
                    view.Text(["text-[10px] font-medium tracking-widest uppercase text-black/20 mt-2 mb-1"], "FRONTIER");
                    foreach (var group in frontier.GroupBy(o => o.Type).OrderByDescending(g => g.Count()))
                    {
                        RenderTypeGroup(view, group.Key, group.ToList());
                    }
                }
            }
        });
    });
}
```

## Notes

- Filter on multiple substrings (`Props.Values`, `MatchKey`, `Type`) so a single query hits whatever facet the user happens to remember.
- Groups are ordered by `Count()` descending — the noisiest type leads, which is usually what the user wants to see first when nothing's typed.
- Empty state swaps icon + copy based on whether the user is searching or just hasn't ingested any data yet — same component, two intents.
- Wrap in `ScrollArea` with `flex-1 min-h-0` so the search bar stays put while results scroll.

## See also

- `expandable-detail-card` — what each result row in `RenderTypeGroup` opens into
