# Lock Screen Live Activity — the Banner That Updates With Nobody Looking

`app.LiveActivity` — a `LiveActivityService` — puts a banner on the iOS lock screen and in the Dynamic Island while something is running, updating in place with the app closed. A tracker sends distance, time and pace; a delivery app sends stops and an ETA; a build sends a step and a count.

It carries **values, never layout**. One widget draws all of them, which is why this needs no per-app native code even though the banner itself cannot be Flutter — iOS renders it through WidgetKit from SwiftUI archived at update time.

## When to use

Anything with a running state a person wants to glance at without opening the app: an activity in progress, a timer, a job with a queue, an arrival.

Not for notifications — those are `app.Notifications`. A live activity is one continuously-updating thing, not a stream of events.

## Snippet

```csharp
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
```

## Notes

- **Every call returns `false` rather than throwing** where a banner cannot be shown — a browser, an Android device, iOS below 16.2, a shell that predates the bridge. A banner is a nicety and its absence must never take an app down. Never branch the app's own behaviour on the result; just don't assume it worked.
- **Three metrics maximum.** Anything past the third is dropped, so choose the three worth glancing at rather than sending everything and hoping.
- `LiveMetric` is `(Value, Label)` and `Value` is a **formatted string**, not a number — the widget does no formatting, so units, precision and padding are yours.
- Prefer `UpdateAsync` over repeated `StartAsync`. A second start would orphan the first banner with numbers that never move again; the client folds a repeat start into an update, but calling the right one says what you meant.
- **End it on `OnClientLeft` as well as when the activity finishes.** A banner left behind outlives the app and freezes at whatever it last said, which reads as a broken app on a lock screen the person cannot dismiss from.
- `muted: true` is the paused/held look — it desaturates the accent. Use it for a real hold, not for "nothing changed recently".
- Mirror the same numbers in the app. A lock screen disagreeing with the screen it came from reads as a bug even when the banner is the stale one.

## See also

- `device-motion-stream` — a common source of the numbers this shows.
- `offline-recording-archive` — what keeps the underlying record honest while the screen is off.
- `connection-status-pill` — the in-app equivalent for state that must always be visible.
