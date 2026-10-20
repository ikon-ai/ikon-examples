namespace Ikon.App.Patterns.Patterns;

// Pattern: countdown-timer — see docs/patterns/countdown-timer.md.
// The demo starts a 15-second round on press; the bar shrinks in the browser and the number ticks
// from the server once a second.
internal sealed class CountdownTimer(IAppBase app) : IPatternDemo
{
    public string Slug => "countdown-timer";
    public string Title => "Countdown timer";
    public string Category => "Realtime";

    public void RenderDemo(IView view)
    {
        RenderCountdown((UIView)view);
        view.Button([Button.OutlineMd, "mt-3"], text: "Start a 15-second round",
            onClick: () => StartRound(TimeSpan.FromSeconds(15)));
    }

    #region example:pattern-countdown-timer
    // Every client of the instance sees the same round, so these are shared reactives
    private readonly Reactive<int> _round = new(0);
    private readonly Reactive<int> _roundMilliseconds = new(0);
    private readonly Reactive<int> _secondsLeft = new(0);
    private CancellationTokenSource? _roundTimer;

    private void StartRound(TimeSpan length)
    {
        _roundTimer?.Cancel();
        _roundTimer = CancellationTokenSource.CreateLinkedTokenSource(app.StoppingToken);
        var deadline = DateTime.UtcNow + length;

        _round.Value++;
        _roundMilliseconds.Value = (int)length.TotalMilliseconds;
        _secondsLeft.Value = (int)Math.Ceiling(length.TotalSeconds);

        _ = TickAsync(deadline, _roundTimer.Token);
    }

    // One small update a second; the bar moves in the browser between them
    private async Task TickAsync(DateTime deadline, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (_secondsLeft.Value > 0 && await timer.WaitForNextTickAsync(ct))
            {
                // Read from the deadline, so a late tick does not make the round longer
                _secondsLeft.Value = Math.Max(0, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds));
            }
        }
        catch (OperationCanceledException)
        {
            // A new round or the instance stopping ended this one; its own timer takes over
            return;
        }

        await OnTimeUpAsync();
    }

    private void RenderCountdown(UIView view)
    {
        view.Column([Card.Default, "gap-3 p-4 w-full max-w-md"], content: view =>
        {
            view.Text([Text.H2, "tabular-nums"], _secondsLeft.Value > 0 ? $"{_secondsLeft.Value} s" : "Time is up");

            view.Box(["h-2 w-full rounded-full bg-secondary overflow-hidden"], content: view =>
            {
                if (_secondsLeft.Value > 0)
                {
                    // A new key each round restarts the animation, which runs the round's length
                    view.Box(["h-full bg-brand-solid", $"motion-[0:w-full,100:w-0] motion-duration-{_roundMilliseconds.Value}ms motion-ease-linear"],
                        key: $"round-{_round.Value}");
                }
            });
        });
    }
    #endregion

    private static Task OnTimeUpAsync() => Task.CompletedTask;
}
