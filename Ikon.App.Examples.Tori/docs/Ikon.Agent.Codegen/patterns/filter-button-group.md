<!-- mined-from: Sentrix -->
# Filter Button Group — Inline Pill Filters Backed by ClientReactive

A row of small button-pills for picking one value from a fixed set, backed by a `ClientReactive<string>`. Each user has their own filter state, the rendered pill toggles colour when selected, and changing it resets pagination to page 0.

## When to use

Lists with 3-6 mutually-exclusive filter values (status, source, trust, severity, type). Pairs naturally with `DataTable` and `ScrollArea`. Use a `Select` dropdown instead when the option set is dynamic or longer than 6.

## Snippet

```csharp
private readonly ClientReactive<string> _filesStatusFilter = new("all");
private readonly ClientReactive<string> _filesSourceFilter = new("all");
private readonly ClientReactive<string> _filesTrustFilter = new("all");
private readonly ClientReactive<int> _filesPage = new(0);

private void RenderFilesTabFilters(UIView view)
{
    view.Row(["flex flex-wrap items-center gap-3"], content: view =>
    {
        RenderFilesFilterButtonGroup(view, T("Status"), _filesStatusFilter,
        [
            ("all", T("All")),
            ("in-progress", T("In progress")),
            ("done", T("Done")),
            ("error", T("Error")),
        ]);

        RenderFilesFilterButtonGroup(view, T("Source"), _filesSourceFilter,
        [
            ("all", T("All")),
            ("Client", T("Client")),
            ("Counterparty", T("Counterparty")),
            ("Regulator", T("Regulator")),
        ]);

        view.Row(["flex items-center gap-2 flex-1 min-w-[10rem] max-w-[20rem]"], content: view =>
        {
            view.TextField([Input.Default, "flex-1"],
                placeholder: T("Search by name…"),
                value: _filesSearchQuery.Value,
                onValueChange: async v =>
                {
                    _filesSearchQuery.Value = v ?? "";
                    _filesPage.Value = 0;
                });
        });
    });
}

private void RenderFilesFilterButtonGroup(UIView view, string label, ClientReactive<string> state,
    (string Value, string Label)[] options)
{
    view.Row(["flex items-center gap-1"], content: view =>
    {
        view.Text(["text-xs font-medium text-tertiary mr-1"], label);

        foreach (var (value, optionLabel) in options)
        {
            var selected = string.Equals(state.Value, value, StringComparison.OrdinalIgnoreCase);
            var style = selected
                ? new[] { "text-xs font-medium px-2.5 py-1 rounded-md bg-primary text-primary-foreground" }
                : new[] { "text-xs font-medium px-2.5 py-1 rounded-md text-tertiary hover:bg-secondary hover:text-primary cursor-pointer" };

            view.Button(style, label: optionLabel,
                onClick: async () =>
                {
                    state.Value = value;
                    _filesPage.Value = 0;
                });
        }
    });
}
```

## Notes

- `ClientReactive<string>` not `Reactive<string>` — each user filters their view independently. A shared `Reactive` would let one user's filter wipe everyone else's table.
- "all" is a sentinel string, not a nullable. The filter function checks `if (filter == "all") return source;` first.
- Always reset `_filesPage.Value = 0` when the filter changes — if the user was on page 5 and the filtered count drops below that, the table renders empty.
- `(string Value, string Label)` tuple options keep the call site terse without needing a record per filter set.
- Size: `text-xs` + `px-2.5 py-1` is the right "filter chip" weight — bigger than a tag, smaller than a button.

## See also

- `kanban-multi-column` — when you need columns instead of filter pills.
- `inline-list-cell-edit` — editing rows once they're filtered.
