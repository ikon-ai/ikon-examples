# Record List Toolbar — Search, Sort, Range, Export Over a Collection

The control strip that makes a collection of records *operable* rather than merely displayed. Any app whose main view is rows a user accumulates — contacts, invoices, applications, tickets, saved runs, tracked habits — needs some subset of search, sort, a date range, filters and export. Users notice these missing far faster than they notice a missing feature, because every product they have used has them.

One reactive per control, one derived `Visible()` the list renders from. The derived query is the whole trick: controls never mutate the stored collection, so state survives filtering and nothing is lost when a filter clears.

## When to use

The primary surface is a list or table of records the user builds up over time and will later need to find things in. Skip it entirely for a fixed handful of items that all fit on screen — a five-row settings list needs no search box.

Take only the controls the workflow needs. Sorting matters when order carries meaning; a date range matters when time changes which records are relevant; export matters when carrying the data elsewhere is a real task and this build actually writes the file.

## Snippet

```csharp
private readonly Reactive<string> _search = new("");
private readonly Reactive<string> _sort = new("recent");
private readonly Reactive<string> _range = new("all");

/// The single derived query every surface reads. Controls filter the VIEW, never the store.
private IReadOnlyList<Contact> Visible()
{
    IEnumerable<Contact> rows = _contacts;

    if (_search.Value.Length > 0)
    {
        rows = rows.Where(c =>
            c.Name.Contains(_search.Value, StringComparison.OrdinalIgnoreCase)
            || c.Company.Contains(_search.Value, StringComparison.OrdinalIgnoreCase));
    }

    if (_range.Value != "all" && int.TryParse(_range.Value, out var days))
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        rows = rows.Where(c => c.LastTouched >= cutoff);
    }

    rows = _sort.Value == "name"
        ? rows.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
        : rows.OrderByDescending(c => c.LastTouched);

    return rows.ToList();
}

private void RenderToolbar(IView view)
{
    var visible = Visible();

    view.Row([Layout.Row.SpaceBetween, "flex-wrap gap-3"], content: view =>
    {
        view.Row(["flex flex-wrap items-center gap-2"], content: view =>
        {
            view.TextField([Input.DefaultSm, "w-56"], placeholder: "Search name or company", bind: _search);

            view.Select([Input.DefaultSm, "w-40"], bind: _sort, ariaLabel: "Sort by", options:
            [
                new SelectOption("recent", "Most recent"),
                new SelectOption("name", "Name"),
            ]);

            view.Select([Input.DefaultSm, "w-40"], bind: _range, ariaLabel: "Date range", options:
            [
                new SelectOption("all", "All time"),
                new SelectOption("30", "Last 30 days"),
                new SelectOption("90", "Last 3 months"),
                new SelectOption("365", "Last year"),
            ]);
        });

        // Export exists here ONLY because this build really writes the file.
        view.ActionButton([Button.OutlineMd], action: ActionKind.DownloadFile,
            options: new DownloadFileActionOptions
            {
                Filename = "contacts.csv",
                Data = Encoding.UTF8.GetBytes(BuildCsv(visible)),
            },
            content: v => v.Row([Layout.Row.Xs], content: inner =>
            {
                inner.Icon([Icon.Sm], name: "download");
                inner.Text([Text.Caption], text: $"Export {visible.Count}");
            }));
    });

    // An active filter the user cannot see is a bug report waiting to happen: show each one
    // as a chip that removes itself.
    if (_search.Value.Length > 0 || _range.Value != "all")
    {
        view.Row(["flex flex-wrap gap-2 pt-2"], content: view =>
        {
            if (_search.Value.Length > 0)
            {
                RenderFilterChip(view, $"Search: {_search.Value}", () => _search.Value = "");
            }

            if (_range.Value != "all")
            {
                RenderFilterChip(view, "Date range", () => _range.Value = "all");
            }
        });
    }
}

private static void RenderFilterChip(IView view, string label, Action clear)
{
    view.Button([Badge.NeutralSm, "gap-1.5"], onClick: async () => clear(), content: v =>
    {
        v.Text(["text-xs font-semibold"], text: label);
        v.Icon([Icon.Sm], name: "x");
    });
}
```

## Notes

- `Visible()` is a plain method, not cached state. Reading `_search.Value` / `_sort.Value` / `_range.Value` inside it registers the dependency wherever it is called from, so the list re-renders on every control change with no wiring.
- Enumerate the reactive collection directly (`IEnumerable<Contact> rows = _contacts;`) — enumeration is a tracked read. Never filter by mutating the store; `RemoveAll` on a search keystroke destroys the user's data.
- `bind:` on TextField and Select is the whole two-way binding. Don't pair `value:` with a manual `onValueChange` here.
- Give every unlabelled control an `ariaLabel:` — a bare Select is unreachable for assistive tech and invisible to the app validator.
- **Add a control only if it works.** An Export button whose handler is empty, or a Sort that reorders nothing, is worse than no button: it is a promise the app breaks, and the functional gate drives the app and fails on exactly that.
- Keep the whole strip in one wrapping `Row` (`flex-wrap`) so a narrow viewport reflows it rather than clipping it. Standard actions may move into a compact surface on mobile; they must not disappear.
- **Do not let a change reorder the row the user is touching.** A status edit under a "most recent" sort moves that row out from under the pointer mid-click. Re-sort on an explicit action, or keep the row in place until the surface is idle — the same rule covers renaming options and moving controls while someone is interacting with them.
- Sorting by "most recent" is the right default for anything with recency semantics. Sorting by name is what people reach for when they are hunting a specific known record.
- For a large collection, bound the rendered window (a page size, or `view.InfiniteScrollView`) rather than rendering every row — and say so in the UI, so the user knows more exist.
- **The option set comes from the data, not from a list you typed.** Every value present in the collection is selectable, a value with no current items shows as empty with a count rather than being omitted, and clearing every filter always returns the whole collection. Where the set must be fixed, rows carrying an unlisted value go under an explicit `Other` instead of vanishing. This failure is the mirror image of a dead control and much harder to see: the control works, results render, and the missing rows produce no state anyone can notice.
- **One filter surface, not two.** The chips summarise what is applied and remove it; the toolbar offers the choices. A sidebar that only repeats the chips adds nothing — delete it. And a facet list is not navigation: styling Project / Platform / Type as full-width high-emphasis rows gives the app two competing navigation systems, neither of which is the real one (`nav-and-menu-rows` has the tokens that keep them apart).
- **Name the dimension when a value is ambiguous.** `Project: Not set` and `Month: August 2026`, never three unlabelled `Not set` chips in a row.

## See also

- `zero-results-state` — what the list renders when the filters match nothing.
- `inline-list-cell-edit` — editing a field directly in the row this toolbar filters.
- `persistent-user-preferences` — when a chosen sort or range should survive a reload.
