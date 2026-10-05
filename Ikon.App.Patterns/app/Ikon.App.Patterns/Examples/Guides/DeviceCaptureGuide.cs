namespace Ikon.App.Patterns.Examples;

file sealed class DeviceCaptureExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private readonly Reactive<double> _peak = new(0);

    private static void Repair(object archive) { }

    private static Task ProcessAsync(AssetUri uri) => Task.CompletedTask;

    public async Task MotionAsync(int sessionId)
    {
        #region example:device-motion
        app.Motion.OnBatch(batch =>
        {
            foreach (var sample in batch.Samples)
            {
                _peak.Value = Math.Max(_peak.Value, sample.Magnitude);
            }
        });

        await app.Motion.StartTrackingAsync(sessionId, new MotionOptions(
            Hertz: 50,
            Sensors: MotionSensors.UserAcceleration | MotionSensors.Gyroscope,
            BatchMilliseconds: 200));
        #endregion
    }

    public async Task RecordingsAsync(int sessionId, string outingId)
    {
        #region example:device-recordings
        app.Recordings.OnArchive(archive => Repair(archive));

        await app.Recordings.StartAsync(sessionId, outingId, new RecordingOptions(
            Fixes: true, Motion: true, MaxBytes: 128L * 1024 * 1024));
        #endregion
    }

    public async Task LiveActivityAsync(IReadOnlyList<LiveMetric> metrics)
    {
        #region example:device-live-activity
        await app.LiveActivity.StartAsync("Momentum", "#db176e",
            [new LiveMetric("0.00 km", "distance"), new LiveMetric("0:00", "moving")], "Run");

        await app.LiveActivity.UpdateAsync(metrics, status: "Run");
        await app.LiveActivity.EndEverywhereAsync();
        #endregion
    }

    public void Uploads()
    {
        #region example:device-uploads
        app.Uploads.Register("my-app.telemetry",
            onStart: args => Task.FromResult(new FileUploadResult
            {
                AssetUri = new AssetUri(AssetClass.CloudFile, $"telemetry/{args.FileName}", app.GlobalState.SpaceId),
            }),
            onComplete: async args =>
            {
                if (args.AssetUri is { } uri) { await ProcessAsync(uri); }
            });
        #endregion
    }
}
