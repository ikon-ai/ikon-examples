using Dapper;
using Ikon.App.Cells;

// No namespace: inside Ikon.App.*, `Cells` would name the Ikon.App.Cells namespace, not the class an
// app (which has no namespace) reaches through its using.

internal static class ScalingSharedInstanceExamples
{
    #region example:scaling-sharded-shared-instance
    // Everyone shares one instance; past 150 connected clients new ones go to a second shard.
    [Sharded(150)]
    public record SessionIdentity();
    #endregion
}

internal static class ScalingShardedRoomExamples
{
    #region example:scaling-sharded-room
    // One instance per room. A room with more than 50 clients spills into shards of its own,
    // at most four of them.
    public record SessionIdentity([property: Sharded(50, MaxShards = 4)] string? RoomId);
    #endregion
}

#region example:scaling-cell
// The cell's surface is an interface: on a cloud run Connect returns a proxy that implements it.
public interface IScoreboardCell
{
    // Exactly Reactive<T>: the proxy mirrors only that type, so a ReactiveList here would not update.
    Reactive<int> Total { get; }

    Task AddAsync(int points);
}

// [RegisterAll] makes the methods callable over the network, which is how the cloud proxy calls them.
[RegisterAll]
[Cell]
public sealed class ScoreboardCell(ICell<ScoreboardCell.SessionIdentity> cell) : IScoreboardCell
{
    // One cell per game id, the same instance for every app instance and shard that asks for it.
    public record SessionIdentity(string GameId);

    public string GameId => cell.Identity.GameId;

    public Reactive<int> Total { get; } = new(0);

    public async Task AddAsync(int points) => Total.Update(total => total + points);
}
#endregion

#region example:scaling-cell-loop
// A cell that updates a value on a timer: one wave position for every screen in the stadium.
public interface IWaveCell
{
    Reactive<int> Stand { get; }
}

[RegisterAll]
[Cell]
public sealed class WaveCell : IWaveCell, IAsyncDisposable
{
    public record SessionIdentity();

    private readonly CancellationTokenSource _stopping = new();
    private readonly Task _loop;

    public WaveCell(ICell<SessionIdentity> cell)
    {
        _loop = RunAsync(_stopping.Token);
    }

    public Reactive<int> Stand { get; } = new(0);

    // The platform calls this when the cell goes away: a hot reload, or its process stopping.
    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();

        try
        {
            await _loop;
        }
        catch (OperationCanceledException)
        {
            // The cancellation above is how the loop ends
        }

        _stopping.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            Stand.Update(stand => (stand + 1) % 12);
        }
    }
}
#endregion

internal sealed class ScalingCellClientExamples
{
    #region example:scaling-cell-connect
    private static IScoreboardCell Board(string gameId) =>
        // Connect returns the same cell (or proxy) for an equal identity, so calling it per use is fine.
        Cells.Instance.Connect<IScoreboardCell>(new ScoreboardCell.SessionIdentity(gameId));

    private async Task ScoreAsync(string gameId, int points) => await Board(gameId).AddAsync(points);

    private void RenderScore(UIView view, string gameId)
    {
        var total = Board(gameId).Total;

        // The reactive is a mirror that starts at its seed (0) until the subscription is live.
        if (total is MirrorReactive<int> { Status: not MirrorStatus.Live } mirror)
        {
            view.Text(mirror.Status == MirrorStatus.Failed ? "Scores are unavailable right now" : "Loading scores…");
            return;
        }

        view.Text([Text.H2], $"{total.Value} points");
    }
    #endregion
}

#region example:scaling-leaderboard-cell
public sealed record LeaderboardEntry(string Name, int Score);

// The top ten, read after Version was taken. A publish with a higher version replaces one with a lower one,
// never the reverse.
public sealed record Leaderboard(long Version, IReadOnlyList<LeaderboardEntry> Entries);

public interface ILeaderboardCell
{
    Reactive<Leaderboard> Top { get; }

    Task PublishAsync(Leaderboard top);
}

[RegisterAll]
[Cell]
public sealed class LeaderboardCell : ILeaderboardCell
{
    public record SessionIdentity();

    public LeaderboardCell(ICell<SessionIdentity> cell)
    {
    }

    // Empty until an app instance publishes, because a cell has no database of its own.
    public Reactive<Leaderboard> Top { get; } = new(new Leaderboard(0, []));

    // Two instances can publish at once. The one that read the table later wins.
    public Task PublishAsync(Leaderboard top)
    {
        Top.Update(current => top.Version > current.Version ? top : current);
        return Task.CompletedTask;
    }
}
#endregion

internal sealed class ScalingLeaderboardExamples(IAppBase app)
{
    #region example:scaling-leaderboard-instance
    private static ILeaderboardCell Board() =>
        Cells.Instance.Connect<ILeaderboardCell>(new LeaderboardCell.SessionIdentity());

    // Run once from app.OnStarting. Publishing here also refills the cell after its host stopped,
    // because the next instance to start connects to a new, empty cell. The publish is not awaited:
    // a local run may not answer the cell until OnStarting returns, so awaiting it there can hang.
    private async Task CreateScoresTableAsync()
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS scores (
                id bigserial PRIMARY KEY,
                name text NOT NULL,
                score int NOT NULL,
                created_at timestamptz NOT NULL DEFAULT now());
            CREATE SEQUENCE IF NOT EXISTS leaderboard_version;
            """);
        _ = Task.Run(PublishTopAsync);
    }

    // The database row is the record. The cell only sends the current top ten to every instance.
    private async Task AddScoreAsync(string name, int score)
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();
        await db.ExecuteAsync("INSERT INTO scores (name, score) VALUES (@name, @score)", new { name, score });
        await PublishTopAsync();
    }

    private async Task PublishTopAsync()
    {
        await using var db = await app.DatabaseAsync();
        await db.OpenAsync();

        // Take the version first, so a higher version always comes from a later read of the table.
        // A sequence, not max(id): an edited, voided or deleted score adds no row, but every publish
        // after it takes a new number.
        var version = await db.ExecuteScalarAsync<long>("SELECT nextval('leaderboard_version')");
        var entries = await db.QueryAsync<LeaderboardEntry>("""
            SELECT name AS Name, max(score) AS Score FROM scores
            GROUP BY name ORDER BY max(score) DESC LIMIT 10
            """);
        await Board().PublishAsync(new Leaderboard(version, entries.ToList()));
    }

    // Called as RenderLeaderboard(view, Board()) from the screen that shows it.
    private static void RenderLeaderboard(UIView view, ILeaderboardCell board)
    {
        var top = board.Top;

        if (top is MirrorReactive<Leaderboard> { Status: not MirrorStatus.Live } mirror)
        {
            view.Text(mirror.Status == MirrorStatus.Failed ? "The leaderboard is unavailable right now" : "Loading the leaderboard…");
            return;
        }

        foreach (var (entry, rank) in top.Value.Entries.Select((entry, index) => (entry, index + 1)))
        {
            view.Row(["justify-between gap-3"], key: entry.Name, content: row =>
            {
                row.Text([Text.Body], $"{rank}. {entry.Name}");
                row.Text([Text.Body, "tabular-nums"], $"{entry.Score}");
            });
        }
    }
    #endregion
}
