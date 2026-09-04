namespace Ikon.App.Patterns.Patterns;

// Pattern: record-list-toolbar — see docs/patterns/record-list-toolbar.md.
// The docsnippet region is the control strip plus the derived query it drives; the stubs outside it
// stand in for the record model, the store the strip filters, and the CSV the export button writes.
internal sealed class RecordListToolbar : IPatternDemo
{
    public string Slug => "record-list-toolbar";
    public string Title => "Record list toolbar";
    public string Category => "Data";
    public void RenderDemo(IView view) => RenderToolbar(view);

    private sealed record Contact(string Id, string Name, string Company, DateTime LastTouched);

    private readonly ReactiveList<Contact> _contacts = new();

    public RecordListToolbar()
    {
        _contacts.AddRange(
        [
            new Contact("1", "Aino Virtanen", "Kolme Studios", DateTime.UtcNow.AddDays(-2)),
            new Contact("2", "Marcus Reed", "Northwind Freight", DateTime.UtcNow.AddDays(-11)),
            new Contact("3", "Priya Raman", "Lumen Health", DateTime.UtcNow.AddDays(-40)),
        ]);
    }

    private static string BuildCsv(IReadOnlyList<Contact> rows) =>
        string.Join("\n", rows.Select(c => $"{c.Name},{c.Company},{c.LastTouched:yyyy-MM-dd}"));

    #region docsnippet:pattern-record-list-toolbar
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
    #endregion
}
