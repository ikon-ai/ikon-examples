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

        // On a cloud run the reactive is a mirror that starts at its seed (0) until the subscription
        // is live. A local run returns the cell itself, which is not a MirrorReactive.
        if (total is MirrorReactive<int> { Status: not MirrorStatus.Live } mirror)
        {
            view.Text(mirror.Status == MirrorStatus.Failed ? "Scores are unavailable right now" : "Loading scores…");
            return;
        }

        view.Text([Text.H2], $"{total.Value} points");
    }
    #endregion
}
