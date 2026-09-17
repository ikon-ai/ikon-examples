namespace Ikon.App.Patterns.Patterns;

// Pattern: device-motion-stream — see docs/patterns/device-motion-stream.md.
// The docsnippet region wires app.Motion and renders what the batches produced. The service calls
// live in Wire/Start/Stop, which the gallery never invokes: app.Motion throws NotSupportedException
// on a host without a device attached, and a demo must render anywhere.
internal sealed class DeviceMotionStream(IAppBase app) : IPatternDemo
{
    public string Slug => "device-motion-stream";
    public string Title => "Device motion stream";
    public string Category => "Realtime";
    public void RenderDemo(IView view) => RenderCadence(view);

    #region docsnippet:pattern-device-motion-stream
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
    #endregion
}
