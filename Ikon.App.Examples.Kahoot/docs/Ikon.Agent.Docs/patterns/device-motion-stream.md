# Device Motion Stream — Reading What GPS Cannot See
<!-- checked-against: 3a76eae3433d0c16 -->
`app.Motion` — a `MotionService` — streams a client's accelerometer, gyroscope and magnetometer to the app. Where `LocationService` answers *where*, this answers *how it is moving* — a speed trace cannot tell a collected canter from a fast trot, and it cannot see a swing, a gesture or a single step at all. Stride and rotation live in the accelerometer.

Samples arrive in **batches**, not one at a time. A round trip per sample at 50 Hz would be fifty round trips a second; `MotionOptions.BatchMilliseconds` is the knob between responsiveness and cost.

## When to use

Gait, cadence, activity classification, impact detection, rep counting, orientation — analysis over a window of samples.

**Not for a low-latency controller.** Batched function calls carry at least one batch of scheduling delay, and every sample is delivered reliably whether or not it still matters. A phone used as a pointing device wants an unreliable app-defined `.tp` message, where a dropped sample is simply superseded by the next. Use this for analysis and a `.tp` channel for control.

## Snippet

```csharp
private readonly UserReactive<double> _stepsPerMinute = new(0);
private readonly UserReactive<bool> _tracking = new(false);

/// Subscribe ONCE, in OnStarting — not per client. The handler runs on the pushing client's
/// reactive scope, so writing per-user state from inside it needs no scope juggling.
private void Wire()
{
    app.Motion.OnBatch(batch =>
    {
        // A batch interleaves every enabled sensor in device order, and Magnitude means m/s² on
        // an acceleration sample and rad/s on a gyroscope one. Split by Sensor first: one series
        // over the mixture reads as a plausible number that measures nothing.
        var accel = batch.Samples.Where(s => s.Sensor == MotionSensors.UserAcceleration).ToArray();

        // Cadence is peaks per minute in the acceleration magnitude across all three axes — the
        // usual first thing a detector wants.
        var peaks = 0;

        for (var i = 1; i < accel.Length - 1; i++)
        {
            var previous = accel[i - 1].Magnitude;
            var current = accel[i].Magnitude;
            var next = accel[i + 1].Magnitude;

            if (current > previous && current >= next && current > 1.2) { peaks++; }
        }

        var seconds = accel.Length == 0 ? 0
            : (accel[^1].AtMillis - accel[0].AtMillis) / 1000.0;

        if (seconds > 0) { _stepsPerMinute.Value = peaks / seconds * 60; }
    });
}

/// 25 Hz reads a walk; a controller wants 50+. BatchMilliseconds is the cost knob — one round
/// trip per sample at 50 Hz is fifty round trips a second, so the device buffers instead.
private async Task StartAsync(int sessionId)
{
    _tracking.Value = await app.Motion.StartTrackingAsync(sessionId, new MotionOptions(
        Hertz: 50,
        Sensors: MotionSensors.UserAcceleration | MotionSensors.Gyroscope,
        BatchMilliseconds: 200,
        Background: true));
}

private async Task StopAsync(int sessionId)
{
    await app.Motion.StopTrackingAsync(sessionId);
    _tracking.Value = false;
}

private void RenderCadence(IView view)
{
    view.Column([StatCard.Root, "max-w-xs"], content: view =>
    {
        view.Text([StatCard.Label], text: "Cadence");
        view.Text([StatCard.Value], text: $"{_stepsPerMinute.Value:0} spm");
        view.Text([Text.Caption], text: _tracking.Value ? "Reading sensors" : "Not tracking");
    });
}
```

## Notes

- **Subscribe once, in `OnStarting`** — not per client. `OnBatch` handlers run on the *pushing* client's reactive scope, so writing `UserReactive` / `ClientReactive` state from inside one needs no scope juggling and no captured ids.
- `MotionSensors` is a `[Flags]` enum — combine what you need and no more. Each sensor is battery.
- `MotionSample.Magnitude` (√(x²+y²+z²)) is the usual first thing a detector wants; the raw axes are the device's own — x across the screen, y up it, z out of it.
- **Split a batch by `MotionSample.Sensor` before you measure anything.** With more than one sensor enabled the batch interleaves them in device order, and the axes carry different units — m/s² for the acceleration sensors, rad/s for the gyroscope, µT for the magnetometer. A magnitude series taken over the mixture still produces a number, and that number means nothing.
- `AtMillis` is **device** time, not server time; `MotionBatch.At` is when the server received the batch. Derive rates from `AtMillis` deltas, never from arrival time, or a slow network reads as a slow athlete.
- `StartTrackingAsync` returns `false` when the client cannot oblige — no such sensor, or a build that does not know how to read it. Treat a false as "this device can't", not as an error.
- `Background: true` needs an already-running background mode on iOS. Motion alone does not keep an app alive, so pair it with location tracking if the app must keep reading in a pocket.
- `LiveHertz` decimates only the *live* stream while `RecordingArchiveService` keeps every sample on the device — the right shape when the live rate drives a screen and the real analysis happens afterwards. See `offline-recording-archive`.
- For the server's own side of a timing calculation, `HighPrecisionTimestamp.Instance.UtcNow` is a base time advanced by a `Stopwatch` rather than a fresh `DateTime.UtcNow` read. It does not jump when the system clock is adjusted, so an NTP correction mid-recording cannot produce a negative interval or a sample that appears to arrive before the one ahead of it.

## See also

- `offline-recording-archive` — keeping the full-rate samples on the device when the network is not to be trusted.
- `lock-screen-live-activity` — showing what the analysis produced without the app on screen.
