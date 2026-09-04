<!-- mined-from: Ikon.App.Patterns -->
# Live Leaderboard Table — DataTable With Derived Rank

`view.DataTable` renders the rows it is handed and reports page changes; it does not sort, rank or
page anything itself. The caller owns all three, which is what makes it a leaderboard rather than a
grid: sort on read, derive the rank from that order, and keep the page index in state so paging
survives a re-render.

## When to use

Any ranked or tabular collection that changes under the viewer — scores, standings, queue
positions, live counts. For a handful of rows a `Column` of `Row`s is simpler; reach for
`DataTable` once you want aligned columns, paging or per-row actions.

Never store a rank on the record. A stored rank goes stale the instant any score changes, and
nothing will tell you.

## Notes

- Columns are `DataTableColumn(Header, Width, Flex, Align, MinWidth, Wrap)` — `Width` for a fixed
  column, `Flex` for the one that should absorb the remaining space, `ColumnAlign.Right` for
  numbers so digits line up.
- Cells are built by factory: `Cell.Text`, `Cell.Badge(value, tone)`, `Cell.Checkbox`,
  `Cell.Action(label, actionId)` and `Cell.ActionGroup`. Row actions arrive in `onActionClick` as
  the `actionId` you gave them.
- `emptyContent:` is a lambda, not a string — an empty leaderboard is a designed state, not a
  blank table.
- Per-slot styling goes through `styles: new DataTableStyles { … }`; each slot merges on top of the
  themed default.

## Snippet

```csharp
private sealed record Player(string Id, string Name, int Score);

// Shared, not per-client: every player watches the same board.
private readonly ReactiveList<Player> _players = new();

// DataTable is paged by the CALLER — it renders the rows it is handed and reports page
// changes. Keeping the page index in state is what makes paging survive a re-render.
private readonly ClientReactive<int> _page = new(0);

private const int PageSize = 10;

private static readonly DataTableColumn[] Columns =
[
    new("#", Width: "3rem", Align: ColumnAlign.Right),
    new("Player", Flex: 1),
    new("Score", Width: "6rem", Align: ColumnAlign.Right),
];

private void Render(IView view)
{
    // Rank is derived at render time from the sort, never stored on the player -- a stored
    // rank goes stale the moment any score changes.
    var ranked = _players.OrderByDescending(p => p.Score).ToList();

    var rows = ranked
        .Skip(_page.Value * PageSize)
        .Take(PageSize)
        .Select((player, index) => new DataTableRow(player.Id,
        [
            Cell.Text($"{_page.Value * PageSize + index + 1}"),
            Cell.Text(player.Name),
            // A leader badge reads at a glance where a number does not.
            ranked[0].Id == player.Id
                ? Cell.Badge($"{player.Score}", SemanticTone.Success)
                : Cell.Text($"{player.Score}"),
        ]))
        .ToArray();

    view.DataTable(
        columns: Columns,
        rows: rows,
        totalCount: ranked.Count,
        pageIndex: _page.Value,
        pageSize: PageSize,
        onPageChange: async page => _page.Value = page,
        emptyContent: v => v.Text(["text-muted-foreground p-4"], text: "No players yet"));
}
```

## See also

- `multi-user-game` — the shared vs per-client state split a leaderboard sits on top of.
- `record-list-toolbar` — search / sort / filter over a collection when the table needs controls.
