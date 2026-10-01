using System.Globalization;

public partial class Validation
{
    private const string DeviceUploadActionId = "validation.device.upload";
    private const long DeviceUploadMaxBytes = 10L * 1024 * 1024;
    private const int DeviceUploadHistory = 10;

    // The client-side wire names of the device functions. ClientFunctions keeps its name table
    // internal, and a capability matrix is the one place an app needs to ask about them directly.
    private static readonly (string Label, string Function)[] DeviceCapabilities =
    [
        ("getLocation", "ikon.client.getLocation"),
        ("locationStream", "ikon.client.startLocationUpdates"),
        ("motion", "ikon.client.startMotionUpdates"),
        ("recording", "ikon.client.startRecordingArchive"),
        ("liveActivity", "ikon.client.startLiveActivity"),
        ("vibrate", "ikon.client.vibrate"),
        ("keepAwake", "ikon.client.keepScreenAwake"),
        ("battery", "ikon.client.getBatteryLevel"),
        ("network", "ikon.client.getNetworkType"),
        ("visibility", "ikon.client.getVisibility"),
        ("mediaDevices", "ikon.client.getMediaDevices"),
        ("notifications", "ikon.client.showNotification"),
        ("pushSubscription", "ikon.client.getPushSubscription"),
        ("fullscreen", "ikon.client.requestFullscreen"),
    ];

    private readonly ClientReactive<string> _devLiveActivityResult = new("(no call yet)");
    private readonly ClientReactive<int> _devLiveActivityBumps = new(0);
    private readonly ClientReactive<bool> _devLiveActivityMuted = new(false);
    private readonly ClientReactive<DateTime?> _devLiveActivityStartedAt = new((DateTime?)null);

    private readonly Reactive<IReadOnlyList<string>> _devUploads = new([]);

    private readonly ClientReactive<int> _devCapabilitiesVersion = new(0);
    private readonly ClientReactive<string> _devProbeResult = new("(not run yet)");

    private void RenderDeviceLiveActivityCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            RenderDeviceCardHeading(view, "Live Activity", onlyOn: "iOS 16.2+");

            view.Row([Layout.Row.Md, "flex-wrap mb-2"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Start", icon: "play", props: TestId("dev-live-activity-start"),
                    onClick: () => RunLiveActivityAsync("start", async () =>
                    {
                        _devLiveActivityStartedAt.Value = DateTime.UtcNow;
                        _devLiveActivityBumps.Value = 0;
                        _devLiveActivityMuted.Value = false;
                        return await app.LiveActivity.StartAsync("Validation", "#2563eb", LiveActivityMetrics(), "Running");
                    }));
                view.Button([Button.PrimaryMd], text: "Bump metrics", icon: "plus", props: TestId("dev-live-activity-bump"),
                    onClick: () => RunLiveActivityAsync("update", async () =>
                    {
                        _devLiveActivityBumps.Value += 1;
                        return await app.LiveActivity.UpdateAsync(LiveActivityMetrics(), _devLiveActivityMuted.Value ? "Paused" : "Running", _devLiveActivityMuted.Value);
                    }));
                view.Button([Button.PrimaryMd], text: _devLiveActivityMuted.Value ? "Unmute" : "Mute", icon: "pause", props: TestId("dev-live-activity-mute"),
                    onClick: () => RunLiveActivityAsync("mute", async () =>
                    {
                        _devLiveActivityMuted.Value = !_devLiveActivityMuted.Value;
                        return await app.LiveActivity.UpdateAsync(LiveActivityMetrics(), _devLiveActivityMuted.Value ? "Paused" : "Running", _devLiveActivityMuted.Value);
                    }));
                view.Button([Button.ErrorMd], text: "End", icon: "square", props: TestId("dev-live-activity-end"),
                    onClick: () => RunLiveActivityAsync("end", () => app.LiveActivity.EndAsync()));
                view.Button([Button.ErrorMd], text: "End everywhere", icon: "x", props: TestId("dev-live-activity-end-everywhere"),
                    onClick: async () =>
                    {
                        try
                        {
                            await app.LiveActivity.EndEverywhereAsync();
                            _devLiveActivityResult.Value = "LiveActivity end everywhere: done";
                        }
                        catch (Exception ex)
                        {
                            _devLiveActivityResult.Value = $"Error: LiveActivity end everywhere failed: {ex.Message}";
                        }
                    });
            });
            view.Text([Text.Body], _devLiveActivityResult.Value, props: TestId("dev-live-activity-result"));
            view.Text([Text.Body, "mt-2"], $"Bumps: {_devLiveActivityBumps.Value}, muted: {(_devLiveActivityMuted.Value ? "yes" : "no")}");
        });
    }

    private IReadOnlyList<LiveMetric> LiveActivityMetrics()
    {
        var elapsed = _devLiveActivityStartedAt.Value is { } startedAt ? DateTime.UtcNow - startedAt : TimeSpan.Zero;

        return
        [
            new LiveMetric(_devLiveActivityBumps.Value.ToString(CultureInfo.InvariantCulture), "bumps"),
            new LiveMetric($"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:00}", "elapsed"),
            new LiveMetric(DateTime.UtcNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture), "updated utc"),
        ];
    }

    private async Task RunLiveActivityAsync(string call, Func<Task<bool>> invoke)
    {
        try
        {
            bool shown = await invoke();
            _devLiveActivityResult.Value = shown ? $"LiveActivity {call}: ok (true)" : $"LiveActivity {call}: not supported (false)";
        }
        catch (Exception ex)
        {
            _devLiveActivityResult.Value = $"Error: LiveActivity {call} failed: {ex.Message}";
        }
    }

    private void RegisterDeviceUpload()
    {
        app.Uploads.Register(
            DeviceUploadActionId,
            onStart: args => Task.FromResult(new FileUploadResult { Accepted = args.Size <= DeviceUploadMaxBytes }),
            onComplete: args =>
            {
                RecordDeviceUpload($"{DateTime.UtcNow:HH:mm:ss}Z received {args.FileName} ({args.MimeType}, {args.Size} bytes) from session {ReactiveScope.ClientIdOrNull?.ToString(CultureInfo.InvariantCulture) ?? "?"}");

                if (args.LocalTempFilePath is { } path)
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        // The bytes were only counted, never kept; a temp file the OS still holds is cleaned with the temp dir.
                    }
                }

                return Task.CompletedTask;
            },
            onError: args =>
            {
                RecordDeviceUpload($"{DateTime.UtcNow:HH:mm:ss}Z failed {args.FileName}: {args.ErrorMessage}");
                return Task.CompletedTask;
            });
    }

    private void RecordDeviceUpload(string line)
    {
        _devUploads.Value = [line, .. _devUploads.Value.Take(DeviceUploadHistory - 1)];
    }

    private void RenderDeviceUploadsCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            // app.Uploads.Register receives uploads no rendered component asked for; only client code
            // (the web SDK's uploadFile with this action id, or a native transport) can start one, so the
            // card shows the registration and the receipts it produces.
            RenderDeviceCardHeading(view, "Background uploads");
            RenderFieldGrid(view,
                ("Action id", v => v.Text([Text.Body], DeviceUploadActionId, props: TestId("dev-upload-registered"))),
                ("Accepts", v => v.Text([Text.Body], $"up to {DeviceUploadMaxBytes / (1024 * 1024)} MB, counted and discarded")));

            view.Text([Text.Label, "mt-4 mb-2"], $"Receipts ({_devUploads.Value.Count})");

            if (_devUploads.Value.Count == 0)
            {
                view.Text([Text.Caption], "None yet.");
            }

            foreach (var line in _devUploads.Value)
            {
                view.Text([Text.Caption], line, props: TestId("dev-upload-receipt"));
            }
        });
    }

    private void RenderDeviceCapabilitiesCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            RenderDeviceCardHeading(view, "Capabilities");

            _ = _devCapabilitiesVersion.Value;
            int sessionId = ReactiveScope.ClientId;

            RenderFieldGrid(view,
                ("Session", v => v.Text([Text.Body], sessionId.ToString(CultureInfo.InvariantCulture), props: TestId("dev-session"))),
                ("User", v => v.Text([Text.Body], DeviceUserLabel())));

            // A function the client does not advertise makes the matching server call answer false or
            // null without a round trip; Probe then calls the harmless ones for real.
            view.Box(["flex flex-wrap gap-2 mt-4"], props: TestId("dev-capabilities"), content: view =>
            {
                foreach (var (label, function) in DeviceCapabilities)
                {
                    view.Badge(label, FunctionRegistry.Instance.HasFunction(function, sessionId) ? SemanticTone.Success : SemanticTone.Neutral);
                }
            });

            view.Row([Layout.Row.Md, "flex-wrap mt-4 mb-2"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Refresh", icon: "refresh-cw", props: TestId("dev-capabilities-refresh"),
                    onClick: async () => _devCapabilitiesVersion.Value += 1);
                view.Button([Button.PrimaryMd], text: "Probe", icon: "radar", props: TestId("dev-capabilities-probe"),
                    onClick: ProbeDeviceCapabilitiesAsync);
            });
            view.Text([Text.Body], _devProbeResult.Value, props: TestId("dev-capabilities-probe-result"));
        });
    }

    private async Task ProbeDeviceCapabilitiesAsync()
    {
        int sessionId = ReactiveScope.ClientId;
        _devProbeResult.Value = "Probing…";
        string step = "battery";

        try
        {
            var battery = await ClientFunctions.GetBatteryLevelAsync(sessionId);
            step = "network";
            var network = await ClientFunctions.GetNetworkTypeAsync(sessionId);
            step = "visibility";
            var visibility = await ClientFunctions.GetVisibilityAsync(sessionId);
            step = "media devices";
            var devices = await ClientFunctions.GetMediaDevicesAsync(sessionId);
            step = "keep awake";
            bool awakeOn = await ClientFunctions.KeepScreenAwakeAsync(true, sessionId);
            bool awakeOff = await ClientFunctions.KeepScreenAwakeAsync(false, sessionId);
            step = "vibrate";
            bool vibrated = await ClientFunctions.VibrateAsync(30, sessionId);
            step = "language";
            var language = await ClientFunctions.GetLanguageAsync(sessionId);
            step = "timezone";
            var timezone = await ClientFunctions.GetTimezoneAsync(sessionId);

            _devProbeResult.Value =
                $"PASS probe: battery {(battery.HasValue ? $"{battery.Value}%" : "n/a")}, network {network ?? "n/a"}, visibility {visibility}, " +
                $"media devices {devices.Count} ({devices.Count(d => d.Kind == ClientMediaDeviceKind.AudioInput)} mic, {devices.Count(d => d.Kind == ClientMediaDeviceKind.VideoInput)} camera), " +
                $"keep awake on {awakeOn.ToString().ToLowerInvariant()}/off {awakeOff.ToString().ToLowerInvariant()}, vibrate {vibrated.ToString().ToLowerInvariant()}, " +
                $"language {language ?? "n/a"}, timezone {timezone ?? "n/a"}";
        }
        catch (Exception ex)
        {
            _devProbeResult.Value = $"FAIL probe: {step}: {ex.Message}";
        }
    }
}
