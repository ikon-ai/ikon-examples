namespace Ikon.App.Patterns.Patterns;

// Pattern: live-leaderboard-table — see docs/patterns/live-leaderboard-table.md.
// The example region below is the canonical body the doc extracts.
internal sealed class LiveLeaderboardTable : IPatternDemo
{
    public string Slug => "live-leaderboard-table";
    public string Title => "Live leaderboard as a DataTable";
    public string Category => "Multi-user & games";

    // More players than one page, with distinct scores, so the ranks, the leader badge and
    // paging all show.
    private static readonly string[] SampleNames =
    [
        "Aino", "Bruno", "Chidi", "Dagny", "Emeka", "Freya", "Goran", "Hana",
        "Ilkka", "Juno", "Kaito", "Lumi", "Mateo", "Noor",
    ];

    public LiveLeaderboardTable()
    {
        _players.AddRange(SampleNames.Select((name, index) =>
            new Player($"p{index}", name, 1200 - index * 37 - (index % 3) * 11)));
    }

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Button([Button.OutlineSm, "self-start"], text: "Score for a random player",
                onClick: () => AddScore(
                    _players[Random.Shared.Next(_players.Count)].Id, Random.Shared.Next(50, 300)));
            Render(col);
        });
    }

    #region example:pattern-live-leaderboard-table
    private sealed record Player(string Id, string Name, int Score);

    // Shared, not per-client: every player watches the same board.
    private readonly ReactiveList<Player> _players = new();

    // DataTable is paged by the CALLER — it renders the rows it is handed and reports page
    // changes. Keeping the page index in state is what makes paging survive a re-render.
    private readonly ClientReactive<int> _page = new(0);

    private const int PageSize = 10;

    // Every client scores on the same list, so the read and the write happen in one Update --
    // reading _players[i] and then assigning it loses a score that lands in between.
    private void AddScore(string playerId, int points) =>
        _players.Update(list => list.Select(p => p.Id == playerId ? p with { Score = p.Score + points } : p));

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
