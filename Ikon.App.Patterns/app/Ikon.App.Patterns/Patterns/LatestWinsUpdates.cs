using Ikon.App.Patterns.Protocol;

namespace Ikon.App.Patterns.Patterns;

// Pattern: latest-wins-updates — see docs/patterns/latest-wins-updates.md.
// No tablet joins the gallery, so the demo's button writes positions the way the handler does, and
// the board shows them once the publishing loop copies them.
internal sealed class LatestWinsUpdates(IAppBase app) : IPatternDemo
{
    public string Slug => "latest-wins-updates";
    public string Title => "Fast updates where a lost one does not matter";
    public string Category => "Device & sensors";

    public void RenderDemo(IView view)
    {
        RenderBoard((UIView)view);
        view.Button([Button.OutlineMd, "mt-3"], text: "Simulate a position", onClick: async () =>
        {
            var workerId = $"tablet-{Random.Shared.Next(1, 4)}";
            var position = new Position(workerId, Random.Shared.Next(0, 100), Random.Shared.Next(0, 100), DateTime.UtcNow);
            _latest[workerId] = (_sequence++, position);
            _shown[workerId] = position;
        });
    }

    private int _sequence;

    #region example:pattern-latest-wins-updates
    private sealed record Position(string WorkerId, double X, double Y, DateTime ReceivedAt);

    // The newest position of each tablet; a plain dictionary, so a message changes no UI
    private readonly ConcurrentDictionary<string, (int Sequence, Position Position)> _latest = new();

    // What the screens draw, copied from _latest four times a second
    private readonly ReactiveDictionary<string, Position> _shown = new();

    /// <summary>Call once from Main.</summary>
    private void StartPositions()
    {
        app.OnMessage<WorkerPosition>(OnPositionAsync);
        _ = PublishPositionsAsync(app.StoppingToken);
    }

    // An unreliable message can be lost or arrive after a newer one, so an older sequence is ignored
    private Task OnPositionAsync(WorkerPosition message, int senderId)
    {
        var position = new Position(message.WorkerId, message.X, message.Y, DateTime.UtcNow);
        _latest.AddOrUpdate(message.WorkerId, (message.Sequence, position),
            (_, current) => message.Sequence > current.Sequence ? (message.Sequence, position) : current);

        return Task.CompletedTask;
    }

    private async Task PublishPositionsAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                foreach (var (workerId, latest) in _latest)
                {
                    // Only a changed position is written, so a still tablet sends nothing to the screens
                    if (!_shown.TryGetValue(workerId, out var shown) || shown != latest.Position)
                    {
                        _shown[workerId] = latest.Position;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The instance is stopping; the next instance starts its own loop from Main
        }
    }

    private void RenderBoard(UIView view)
    {
        view.Column([Card.Default, "gap-2 p-4 w-full max-w-2xl"], content: view =>
        {
            view.Text([Text.H3], "Tablets on the floor");

            if (_shown.Count == 0)
            {
                view.EmptyState([EmptyState.RootSm], title: "No tablet has reported yet", description: "A tablet shows up here once it sends a position.");
                return;
            }

            foreach (var position in _shown.Values.OrderBy(p => p.WorkerId, StringComparer.Ordinal))
            {
                view.Row(["items-center justify-between gap-3"], key: position.WorkerId, content: view =>
                {
                    view.Text([Text.Body, "font-medium"], position.WorkerId);
                    view.Text([Text.Body, "tabular-nums"], $"{position.X:0} m, {position.Y:0} m");
                    view.Text([Text.Caption], $"{position.ReceivedAt:HH:mm:ss} UTC");
                });
            }
        });
    }
    #endregion
}
