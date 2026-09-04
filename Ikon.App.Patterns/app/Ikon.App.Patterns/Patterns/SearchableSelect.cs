namespace Ikon.App.Patterns.Patterns;

// Pattern: searchable-select — see docs/patterns/searchable-select.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class SearchableSelect : IPatternDemo
{
    public string Slug => "searchable-select";
    public string Title => "Searchable select over many options";
    public string Category => "Forms & input";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-searchable-select
    private static readonly string[] Countries =
        ["Finland", "France", "Germany", "Greece", "Iceland", "Ireland", "Italy"];

    private readonly UserReactive<string> _country = new("");
    private readonly ClientReactive<string> _search = new("");

    private void Render(IView view)
    {
        // Combobox is STATELESS about the search: it renders the options it is handed and reports
        // what was typed. Filtering is the app's job, which is what lets the same component serve
        // a fixed list, a database query or a remote search.
        var matches = string.IsNullOrWhiteSpace(_search.Value)
            ? Countries
            : Countries.Where(c => c.Contains(_search.Value, StringComparison.OrdinalIgnoreCase)).ToArray();

        view.Combobox(
            options: matches.Select(c => new SelectOption(c, c)).ToList(),
            value: _country.Value,
            onValueChange: async country => _country.Value = country,

            // searchValue/onSearchChange are a separate pair from value/onValueChange: what is
            // typed and what is chosen are different pieces of state, and conflating them is why
            // a hand-rolled version loses the selection as soon as the user types again.
            searchValue: _search.Value,
            onSearchChange: async text => _search.Value = text,

            // emptyText is what renders when the filter matches nothing -- a designed empty state
            // rather than a silently blank popover.
            emptyText: "No country matches that.",
            placeholder: "Select a country",
            searchPlaceholder: "Type to filter");
    }
    #endregion
}
