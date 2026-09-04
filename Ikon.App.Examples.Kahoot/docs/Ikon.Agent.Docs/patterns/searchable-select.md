<!-- mined-from: Ikon.App.Patterns -->
# Searchable Select — Combobox With The Filtering Left To You

`Combobox` renders the options it is handed and reports what was typed. It does **not** filter —
which is exactly what lets the same component serve a fixed list, a database query and a remote
search without three different components.

The important part is that **what is typed and what is chosen are separate state**.
`searchValue`/`onSearchChange` are a different pair from `value`/`onValueChange`. Conflating them
is why a hand-rolled version loses the selection the moment the user types again.

## When to use

A select with more options than a person will scroll — countries, users, tags, SKUs. Under about a
dozen options, `view.Select` is simpler and needs no filtering. For a command palette over
*actions* rather than values, `command-palette-jump`.

## Notes

- **Filtering is the app's job.** For a remote source, debounce `onSearchChange` and hold the
  results in state; the component re-renders with whatever list it is given.
- Bind the chosen value to **user** scope (`UserReactive`) and the search text to **client** scope:
  the selection should survive a reload, the half-typed filter should not.
- `emptyText` is what renders when the filter matches nothing — a designed empty state instead of a
  silently blank popover. `placeholder` and `searchPlaceholder` are separate strings.
- Options are `SelectOption(value, label)`, the same shape `view.Select` takes.
- Keep the selected option in the list you pass even when it does not match the current filter, or
  the trigger has nothing to render the selection from.

## Snippet

```csharp
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
```

## See also

- `choice-controls-bound` — the controls that do take `bind:`, and the Slider overload that does not.
- `command-palette-jump` — the same interaction over actions rather than values.
