namespace Ikon.App.Patterns.Patterns;

// Pattern: lock-screen-live-activity — see docs/patterns/lock-screen-live-activity.md.
// The docsnippet region is the start/update/end cycle plus the metrics the banner carries. Every
// call returns false rather than throwing where a banner cannot be shown, but app.LiveActivity
// itself throws on a host with no device, so the calls stay out of the gallery's render path.
internal sealed class LockScreenLiveActivity(IAppBase app) : IPatternDemo
{
    public string Slug => "lock-screen-live-activity";
    public string Title => "Lock screen live activity";
    public string Category => "Media";
    public void RenderDemo(IView view) => RenderMirror(view);

    private readonly Reactive<double> _distanceKm = new(4.2);
    private readonly Reactive<int> _movingSeconds = new(1_530);
    private readonly Reactive<bool> _held = new(false);

    #region docsnippet:pattern-lock-screen-live-activity
    /// The banner carries VALUES, never layout — one widget draws every app's. Three metrics is the
    /// ceiling; anything past that is dropped, so choose the three worth glancing at.
    private IReadOnlyList<LiveMetric> Metrics() =>
    [
        new LiveMetric($"{_distanceKm.Value:0.00} km", "distance"),
        new LiveMetric($"{_movingSeconds.Value / 60}:{_movingSeconds.Value % 60:00}", "moving"),
        new LiveMetric(Pace(), "pace"),
    ];

    private async Task StartAsync()
    {
        // Returns false — never throws — on a browser, on Android, on iOS below 16.2, or on a shell
        // that predates the bridge. A banner is a nicety; never let its absence take the app down.
        await app.LiveActivity.StartAsync(
            title: "Momentum",
            accentHex: "#db176e",
            metrics: Metrics(),
            status: "Run");
    }

    /// Called from wherever the numbers change. Starting a second activity would orphan the first
    /// with numbers that never move again, so a repeat start folds into an update — but prefer this.
    private async Task PushAsync()
    {
        await app.LiveActivity.UpdateAsync(Metrics(), status: _held.Value ? "Paused" : "Run", muted: _held.Value);
    }

    /// End it when the activity ends, and on OnClientLeft — a banner left behind outlives the app
    /// and freezes at whatever it last said.
    private async Task EndAsync()
    {
        await app.LiveActivity.EndAsync();
    }

    private string Pace()
    {
        if (_distanceKm.Value <= 0) { return "—"; }

        var secondsPerKm = _movingSeconds.Value / _distanceKm.Value;
        return $"{(int)(secondsPerKm / 60)}:{(int)(secondsPerKm % 60):00} /km";
    }

    /// The in-app mirror. Whatever the banner says, the app shows the same numbers — a lock screen
    /// disagreeing with the screen it came from reads as a bug.
    private void RenderMirror(IView view)
    {
        view.Row([Card.Default, "gap-6 p-4"], content: view =>
        {
            foreach (var metric in Metrics())
            {
                view.Column([Layout.Column.Xs], content: v =>
                {
                    v.Text([StatCard.Value, "text-xl"], text: metric.Value);
                    v.Text([StatCard.Label], text: metric.Label);
                });
            }
        });
    }
    #endregion
}
