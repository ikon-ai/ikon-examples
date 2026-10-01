using System.Globalization;

public partial class Validation
{
    private const int DeviceArchiveHistory = 10;

    private readonly ClientReactive<string> _devMotionStatus = new("Motion: off");
    private readonly ClientReactive<int> _devMotionBatches = new(0);
    private readonly ClientReactive<int> _devMotionSamples = new(0);
    private readonly ClientReactive<IReadOnlyDictionary<MotionSensors, MotionSample>> _devMotionLast = new(new Dictionary<MotionSensors, MotionSample>());
    private readonly ClientReactive<string> _devMotionHertz = new("25");
    private readonly ClientReactive<string> _devMotionBatchMs = new("200");
    private readonly ClientReactive<string> _devMotionLiveHertz = new("0");
    private readonly ClientReactive<bool> _devMotionUserAcceleration = new(true);
    private readonly ClientReactive<bool> _devMotionAcceleration = new(false);
    private readonly ClientReactive<bool> _devMotionGyroscope = new(false);
    private readonly ClientReactive<bool> _devMotionMagnetometer = new(false);

    private readonly ClientReactive<string> _devRecordingId = new("");
    private readonly ClientReactive<string> _devRecordingStatus = new("Recording: off");
    private readonly ClientReactive<bool> _devRecordingFixes = new(true);
    private readonly ClientReactive<bool> _devRecordingMotion = new(true);
    private readonly ClientReactive<string> _devRecordingCodec = new("(not run yet)");
    private readonly ClientReactive<string> _devRecordingRedecode = new("");
    private readonly Reactive<IReadOnlyList<DeviceArchiveSummary>> _devRecordingArchives = new([]);

    private void RenderDeviceMotionCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            RenderDeviceCardHeading(view, "Motion", onlyOn: "Flutter app");

            view.Row([Layout.Row.Md, "flex-wrap items-end mb-3"], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Hertz");
                    view.Select(options: [new("10", "10"), new("25", "25"), new("50", "50"), new("100", "100")], bind: _devMotionHertz, ariaLabel: "Motion sample rate");
                });
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Batch (ms)");
                    view.Select(options: [new("100", "100"), new("200", "200"), new("500", "500"), new("1000", "1000")], bind: _devMotionBatchMs, ariaLabel: "Motion batch milliseconds");
                });
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Live Hz (0 = all)");
                    view.Select(options: [new("0", "0"), new("5", "5"), new("10", "10")], bind: _devMotionLiveHertz, ariaLabel: "Motion live hertz");
                });
            });

            view.Row([Layout.Row.Md, "flex-wrap items-center mb-3"], content: view =>
            {
                RenderDeviceToggle(view, _devMotionUserAcceleration, "User acceleration", "dev-motion-user-acceleration");
                RenderDeviceToggle(view, _devMotionAcceleration, "Acceleration", "dev-motion-acceleration");
                RenderDeviceToggle(view, _devMotionGyroscope, "Gyroscope", "dev-motion-gyroscope");
                RenderDeviceToggle(view, _devMotionMagnetometer, "Magnetometer", "dev-motion-magnetometer");
            });

            view.Row([Layout.Row.Md, "flex-wrap mb-2"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Start motion", icon: "activity", props: TestId("dev-motion-start"),
                    onClick: StartDeviceMotionAsync);
                view.Button([Button.ErrorMd], text: "Stop motion", icon: "circle-stop", props: TestId("dev-motion-stop"),
                    onClick: StopDeviceMotionAsync);
            });
            view.Text([Text.Body], _devMotionStatus.Value, props: TestId("dev-motion-status"));
            view.Text([Text.Body, "mt-2"], $"Batches: {_devMotionBatches.Value}, samples: {_devMotionSamples.Value}", props: TestId("dev-motion-counts"));

            foreach (var (sensor, sample) in _devMotionLast.Value.OrderBy(p => (int)p.Key))
            {
                view.Text([Text.Caption],
                    $"{sensor}: x {Inv(sample.X, "F3")}, y {Inv(sample.Y, "F3")}, z {Inv(sample.Z, "F3")}, |v| {Inv(sample.Magnitude, "F3")} at {DateTimeOffset.FromUnixTimeMilliseconds((long)sample.AtMillis):HH:mm:ss.fff}Z");
            }
        });
    }

    private void RenderDeviceRecordingCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            RenderDeviceCardHeading(view, "Recordings", onlyOn: "Flutter app");

            view.Row([Layout.Row.Md, "flex-wrap items-end mb-3"], content: view =>
            {
                view.TextField([Input.Default, "w-64"], bind: _devRecordingId, label: "Archive id", placeholder: "empty = generated", props: TestId("dev-recording-id"));
                RenderDeviceToggle(view, _devRecordingFixes, "Fixes", "dev-recording-fixes");
                RenderDeviceToggle(view, _devRecordingMotion, "Motion", "dev-recording-motion");
            });

            view.Row([Layout.Row.Md, "flex-wrap mb-2"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Start recording", icon: "circle-dot", props: TestId("dev-recording-start"),
                    onClick: StartDeviceRecordingAsync);
                view.Button([Button.PrimaryMd], text: "Stop and upload", icon: "upload", props: TestId("dev-recording-stop"),
                    onClick: StopDeviceRecordingAsync);
                view.Button([Button.PrimaryMd], text: "Request pending", icon: "inbox", props: TestId("dev-recording-pending"),
                    onClick: RequestDeviceRecordingsAsync);
            });
            view.Text([Text.Body], _devRecordingStatus.Value, props: TestId("dev-recording-status"));

            view.Text([Text.Label, "mt-4 mb-2"], $"Archives received ({_devRecordingArchives.Value.Count})");

            if (_devRecordingArchives.Value.Count == 0)
            {
                view.Text([Text.Caption], "None yet.", props: TestId("dev-recording-archives-empty"));
            }

            foreach (var archive in _devRecordingArchives.Value)
            {
                view.Row([Layout.Row.Md, "flex-wrap items-center"], content: view =>
                {
                    view.Text([Text.Caption],
                        $"{archive.ArchiveId}: {archive.Fixes} fixes, {archive.MotionSamples} motion samples, ≥{archive.RecordBytes} bytes, started {archive.StartedAt:HH:mm:ss}Z, session {archive.SessionId}, user {archive.UserLabel}, received {archive.ReceivedAt:HH:mm:ss}Z",
                        props: TestId("dev-recording-archive"));
                    view.Button([Button.PrimarySm], text: "Re-decode", onClick: () => RedecodeDeviceArchiveAsync(archive));
                });
            }

            if (!string.IsNullOrEmpty(_devRecordingRedecode.Value))
            {
                view.Text([Text.Body, "mt-2"], _devRecordingRedecode.Value, props: TestId("dev-recording-redecode"));
            }

            // Encodes a synthetic archive as a device writes one, appends a torn record and decodes it
            // back, so the format is checked on any client.
            view.Text([Text.Label, "mt-4 mb-2"], "Archive format self-test");
            view.Row([Layout.Row.Md, "flex-wrap mb-2"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Run codec self-test", icon: "binary", props: TestId("dev-recording-codec-run"),
                    onClick: async () => _devRecordingCodec.Value = RunRecordingCodecSelfTest());
            });
            view.Text([Text.Body], _devRecordingCodec.Value, props: TestId("dev-recording-codec"));
        });
    }

    private static void RenderDeviceToggle(UIView view, ClientReactive<bool> bind, string label, string testId)
    {
        view.Row(["items-center gap-2"], content: view =>
        {
            view.Switch([Switch.Default], bind: bind, props: AriaLabel(label, testId: testId));
            view.Text([Text.Caption], label);
        });
    }

    private async Task StartDeviceMotionAsync()
    {
        var sensors = (_devMotionUserAcceleration.Value ? MotionSensors.UserAcceleration : 0)
            | (_devMotionAcceleration.Value ? MotionSensors.Acceleration : 0)
            | (_devMotionGyroscope.Value ? MotionSensors.Gyroscope : 0)
            | (_devMotionMagnetometer.Value ? MotionSensors.Magnetometer : 0);

        if (sensors == 0)
        {
            _devMotionStatus.Value = "Motion start: pick at least one sensor";
            return;
        }

        var options = new MotionOptions(
            Hertz: int.Parse(_devMotionHertz.Value, CultureInfo.InvariantCulture),
            Sensors: sensors,
            BatchMilliseconds: int.Parse(_devMotionBatchMs.Value, CultureInfo.InvariantCulture),
            LiveHertz: int.Parse(_devMotionLiveHertz.Value, CultureInfo.InvariantCulture));

        try
        {
            _devMotionBatches.Value = 0;
            _devMotionSamples.Value = 0;
            _devMotionLast.Value = new Dictionary<MotionSensors, MotionSample>();
            bool started = await app.Motion.StartTrackingAsync(ReactiveScope.ClientId, options);
            _devMotionStatus.Value = started
                ? $"Motion start: streaming (true) — {sensors} at {options.Hertz} Hz, {options.BatchMilliseconds} ms batches"
                : "Motion start: not supported on this client (false)";
        }
        catch (Exception ex)
        {
            _devMotionStatus.Value = $"Error: motion start failed: {ex.Message}";
        }
    }

    private async Task StopDeviceMotionAsync()
    {
        try
        {
            bool stopped = await app.Motion.StopTrackingAsync(ReactiveScope.ClientId);
            _devMotionStatus.Value = stopped ? "Motion stop: stopped (true)" : "Motion stop: not supported on this client (false)";
        }
        catch (Exception ex)
        {
            _devMotionStatus.Value = $"Error: motion stop failed: {ex.Message}";
        }
    }

    // Runs in the pushing client's scope, so the ClientReactive writes land on that client.
    private void OnDeviceMotionBatch(MotionBatch batch)
    {
        _devMotionBatches.Value += 1;
        _devMotionSamples.Value += batch.Samples.Count;

        var last = new Dictionary<MotionSensors, MotionSample>(_devMotionLast.Value);

        foreach (var sample in batch.Samples)
        {
            last[sample.Sensor] = sample;
        }

        _devMotionLast.Value = last;
    }

    private async Task StartDeviceRecordingAsync()
    {
        string archiveId = _devRecordingId.Value.Trim();

        if (archiveId.Length == 0)
        {
            archiveId = $"validation-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            _devRecordingId.Value = archiveId;
        }

        try
        {
            bool started = await app.Recordings.StartAsync(ReactiveScope.ClientId, archiveId,
                new RecordingOptions(Fixes: _devRecordingFixes.Value, Motion: _devRecordingMotion.Value, MaxBytes: 32L * 1024 * 1024));
            _devRecordingStatus.Value = started
                ? $"Recording start: recording {archiveId} (true)"
                : "Recording start: not supported on this client (false)";
        }
        catch (Exception ex)
        {
            _devRecordingStatus.Value = $"Error: recording start failed: {ex.Message}";
        }
    }

    private async Task StopDeviceRecordingAsync()
    {
        string archiveId = _devRecordingId.Value.Trim();

        if (archiveId.Length == 0)
        {
            _devRecordingStatus.Value = "Recording stop: no archive id — start a recording first";
            return;
        }

        try
        {
            bool stopped = await app.Recordings.StopAsync(ReactiveScope.ClientId, archiveId);
            _devRecordingStatus.Value = stopped
                ? $"Recording stop: {archiveId} stopped, upload requested (true) — the archive appears below when it lands"
                : "Recording stop: not supported on this client (false)";
        }
        catch (Exception ex)
        {
            _devRecordingStatus.Value = $"Error: recording stop failed: {ex.Message}";
        }
    }

    private async Task RequestDeviceRecordingsAsync()
    {
        try
        {
            bool asked = await app.Recordings.RequestPendingAsync(ReactiveScope.ClientId);
            _devRecordingStatus.Value = asked
                ? "Request pending: asked (true) — anything the device still holds uploads now"
                : "Request pending: not supported on this client (false)";
        }
        catch (Exception ex)
        {
            _devRecordingStatus.Value = $"Error: request pending failed: {ex.Message}";
        }
    }

    // Runs on the app's scope, not the uploader's: the client that uploads is often not the one that
    // recorded, so the list is shared rather than per client.
    private void OnDeviceRecordingArchive(RecordingArchive archive)
    {
        long recordBytes = RecordingArchiveCodec.HeaderBytes
            + (long)archive.Fixes.Count * RecordingArchiveCodec.FixBytes
            + (long)archive.Motion.Count * RecordingArchiveCodec.MotionBytes;

        var summary = new DeviceArchiveSummary(
            archive.ArchiveId,
            archive.SessionId,
            string.IsNullOrEmpty(archive.UserId) ? "anonymous" : archive.UserId,
            archive.StartedAt,
            archive.Fixes.Count,
            archive.Motion.Count,
            recordBytes,
            archive.Asset,
            DateTime.UtcNow);

        _devRecordingArchives.Value = [summary, .. _devRecordingArchives.Value.Take(DeviceArchiveHistory - 1)];
    }

    private async Task RedecodeDeviceArchiveAsync(DeviceArchiveSummary archive)
    {
        _devRecordingRedecode.Value = $"Re-decoding {archive.ArchiveId}…";

        try
        {
            var bytes = await Asset.Instance.GetBytesAsync(archive.Asset);
            var (startedAt, fixes, motion) = RecordingArchiveCodec.Decode(bytes);
            bool matches = fixes.Count == archive.Fixes && motion.Count == archive.MotionSamples;
            _devRecordingRedecode.Value =
                $"{(matches ? "PASS" : "FAIL")} re-decode {archive.ArchiveId}: {bytes.Length} bytes, {fixes.Count} fixes, {motion.Count} motion samples, started {startedAt:HH:mm:ss}Z";
        }
        catch (Exception ex)
        {
            _devRecordingRedecode.Value = $"Error: re-decode {archive.ArchiveId} failed: {ex.Message}";
        }
    }

    private static string RunRecordingCodecSelfTest()
    {
        try
        {
            var startedAt = new DateTime(2026, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);
            double baseAt = new DateTimeOffset(startedAt).ToUnixTimeMilliseconds();

            RecordedFix[] fixes =
            [
                new(baseAt + 1000, 60.169856, 24.938379, 5, 1.5, 90, 12.5),
                new(baseAt + 2000, 60.169900, 24.938500, 4, 1.6, 92, double.NaN),
            ];
            MotionSample[] samples =
            [
                new(baseAt + 1020, 0.1, -0.2, 9.8, MotionSensors.Acceleration),
                new(baseAt + 1040, 0.01, 0.02, 0.03, MotionSensors.Gyroscope),
                new(baseAt + 1060, -1.5, 0.5, 0.25, MotionSensors.UserAcceleration),
            ];

            using var stream = new MemoryStream();
            stream.Write(RecordingArchiveCodec.EncodeHeader(startedAt, baseAt));
            stream.Write(RecordingArchiveCodec.EncodeFix(fixes[0], baseAt));
            stream.Write(RecordingArchiveCodec.EncodeMotion(samples[0], baseAt));
            stream.Write(RecordingArchiveCodec.EncodeMotion(samples[1], baseAt));
            stream.Write(RecordingArchiveCodec.EncodeFix(fixes[1], baseAt));
            stream.Write(RecordingArchiveCodec.EncodeMotion(samples[2], baseAt));

            // A device killed mid-write leaves a partial record; the decoder must keep everything before it.
            var tornTail = RecordingArchiveCodec.EncodeFix(fixes[0], baseAt);
            stream.Write(tornTail, 0, 10);

            var bytes = stream.ToArray();
            var (decodedStart, decodedFixes, decodedMotion) = RecordingArchiveCodec.Decode(bytes);

            if (decodedStart != startedAt)
            {
                return $"FAIL codec: started {decodedStart:O}, expected {startedAt:O}";
            }

            if (decodedFixes.Count != fixes.Length || decodedMotion.Count != samples.Length)
            {
                return $"FAIL codec: decoded {decodedFixes.Count} fixes and {decodedMotion.Count} motion samples, expected {fixes.Length} and {samples.Length}";
            }

            for (int i = 0; i < fixes.Length; i++)
            {
                var expected = fixes[i];
                var actual = decodedFixes[i];
                bool altitudeOk = double.IsNaN(expected.AltitudeMeters) ? double.IsNaN(actual.AltitudeMeters) : Math.Abs(actual.AltitudeMeters - expected.AltitudeMeters) < 0.01;

                if (actual.AtMillis != expected.AtMillis || actual.Latitude != expected.Latitude || actual.Longitude != expected.Longitude
                    || Math.Abs(actual.AccuracyMeters - expected.AccuracyMeters) > 0.01 || Math.Abs(actual.Heading - expected.Heading) > 0.01 || !altitudeOk)
                {
                    return $"FAIL codec: fix {i} decoded as {actual}, expected {expected}";
                }
            }

            for (int i = 0; i < samples.Length; i++)
            {
                var expected = samples[i];
                var actual = decodedMotion[i];

                if (actual.Sensor != expected.Sensor || actual.AtMillis != expected.AtMillis
                    || Math.Abs(actual.X - expected.X) > 1e-5 || Math.Abs(actual.Y - expected.Y) > 1e-5 || Math.Abs(actual.Z - expected.Z) > 1e-5)
                {
                    return $"FAIL codec: motion {i} decoded as {actual}, expected {expected}";
                }
            }

            return $"PASS codec: {bytes.Length} bytes → {decodedFixes.Count} fixes, {decodedMotion.Count} motion samples, torn tail ignored";
        }
        catch (Exception ex)
        {
            return $"FAIL codec: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private sealed record DeviceArchiveSummary(
        string ArchiveId,
        int SessionId,
        string UserLabel,
        DateTime StartedAt,
        int Fixes,
        int MotionSamples,
        long RecordBytes,
        AssetUri Asset,
        DateTime ReceivedAt);
}
