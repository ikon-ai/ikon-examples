namespace Ikon.App.Patterns.Patterns;

// Pattern: background-worker — see docs/patterns/background-worker.md.
internal sealed class BackgroundWorker : IPatternDemo
{
    public string Slug => "background-worker";
    public string Title => "Background worker";
    public string Category => "Platform mechanics";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern: a loop started in OnStarting runs a round every minute with nobody connected, holding BackgroundWork only while its rounds succeed, and a five-minute [Cron] starts a new instance when the last one stopped — see docs/patterns/background-worker.md.");
}

// Abstract so neither the bundle scan nor the Patterns app registers its [Cron] job or starts its
// loop: the gallery has no work to run every minute.
file abstract class BackgroundWorkerExample(IAppBase app)
{
    private Task RunOneRoundAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    #region example:pattern-background-worker
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    /// <summary>Call once from Main. Every new instance (a deploy, a hot reload, a cold start) starts its own loop.</summary>
    private void StartWorker()
    {
        // Not awaited: OnStarting must return for the instance to finish starting
        app.OnStarting(async () => _ = RunWorkerAsync(app.StoppingToken));
    }

    private async Task RunWorkerAsync(CancellationToken stopping)
    {
        // While held, the instance is not stopped as idle, so the loop runs with nobody connected.
        // It holds for three rounds and each round that succeeds extends it, so a loop that keeps
        // failing lets the instance stop instead of running, and costing, all day.
        await using var hold = await app.BackgroundWork.StartAsync(Interval * 3, "background worker");
        using var timer = new PeriodicTimer(Interval);

        try
        {
            do
            {
                try
                {
                    await RunOneRoundAsync(stopping);
                    await hold.ExtendAsync(Interval * 3);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.Instance.Warning($"Background worker round failed, next try in {Interval.TotalSeconds:0} s: {ex.Message}");
                }
            }
            while (await timer.WaitForNextTickAsync(stopping));
        }
        catch (OperationCanceledException)
        {
            // The instance is stopping; the next instance starts its own loop, so nothing is lost here
        }
    }

    // A tick starts the app's shared instance when none is running, and that start runs OnStarting,
    // so the loop is back within five minutes of an instance stopping. The tick has nothing to do.
    [Cron("*/5 * * * *")]
    public void KeepWorkerRunning()
    {
    }
    #endregion
}
