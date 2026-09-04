namespace Ikon.App.Patterns.Patterns;

// Pattern: live-leaderboard-table — see docs/patterns/live-leaderboard-table.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class LiveLeaderboardTable : IPatternDemo
{
    public string Slug => "live-leaderboard-table";
    public string Title => "Live leaderboard as a DataTable";
    public string Category => "Multi-user & games";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-live-leaderboard-table
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
    #endregion
}
