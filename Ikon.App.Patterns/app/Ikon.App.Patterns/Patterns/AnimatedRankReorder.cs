namespace Ikon.App.Patterns.Patterns;

// Pattern: animated-rank-reorder — see docs/patterns/animated-rank-reorder.md.
internal sealed class AnimatedRankReorder : IPatternDemo
{
    public string Slug => "animated-rank-reorder";
    public string Title => "Animated rank changes";
    public string Category => "Multi-user & games";

    // Close scores, so one press often swaps two rows and the move shows.
    private static readonly string[] SampleTeams = ["Aurora", "Borealis", "Cirrus", "Delta", "Ember", "Fjord"];

    public AnimatedRankReorder()
    {
        _teams.AddRange(SampleTeams.Select((name, index) => new Team($"t{index}", name, 90 - index * 7)));
    }

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-md"], content: col =>
        {
            col.Button([Button.OutlineSm, "self-start"], text: "Score for a random team", onClick: () =>
            {
                var id = _teams[Random.Shared.Next(_teams.Count)].Id;
                _teams.Update(list => list.Select(t => t.Id == id ? t with { Score = t.Score + Random.Shared.Next(5, 20) } : t));
            });
            RenderBoard((UIView)col);
        });
    }

    #region example:pattern-animated-rank-reorder
    private sealed record Team(string Id, string Name, int Score);

    private readonly ReactiveList<Team> _teams = new();

    private const int RowHeight = 56;

    private void RenderBoard(UIView view)
    {
        var ranked = _teams.OrderByDescending(t => t.Score).ToList();
        var rankOf = ranked.Select((team, index) => (team.Id, index)).ToDictionary(x => x.Id, x => x.index);

        // A box of fixed height holds the rows, so the page below does not move while they do.
        view.Box(["relative w-full", $"h-[{ranked.Count * RowHeight}px]"], content: view =>
        {
            // Draw the rows in a stable order (by id), never by rank. The client keeps each keyed row's
            // element and only its translate-y changes, which transition-transform animates. Drawn in
            // rank order, the elements change places instead and the rows jump.
            foreach (var team in _teams.OrderBy(t => t.Id, StringComparer.Ordinal))
            {
                var rank = rankOf[team.Id];

                view.Box(
                    ["absolute inset-x-0 top-0 transition-transform duration-700 ease-in-out", $"translate-y-[{rank * RowHeight}px]", $"h-[{RowHeight - 8}px]"],
                    key: team.Id,
                    content: view => view.Row([Card.Default, "h-full items-center gap-4 px-4"], content: row =>
                    {
                        row.Text([Text.BodyStrong, "w-8 tabular-nums"], text: $"{rank + 1}");
                        row.Text([Text.Body, "flex-1 truncate"], text: team.Name);

                        // Keyed by the score, so a new score is a new node and its keyframes play once.
                        row.Text([Text.BodyStrong, "tabular-nums motion-[0:scale-125,100:scale-100] motion-duration-500ms"],
                            key: $"{team.Id}-{team.Score}", text: $"{team.Score}");
                    }));
            }
        });
    }
    #endregion
}
