using System.Globalization;

public partial class Validation
{
    private readonly ClientReactive<string> _devLocationOneShot = new("(not requested yet)");
    private readonly ClientReactive<string> _devLocationTracking = new("Tracking: off");
    private readonly ClientReactive<string> _devLocationLastFix = new("(no fix received yet)");
    private readonly ClientReactive<int> _devLocationFixCount = new(0);
    private readonly ClientReactive<string> _devLocationInterval = new("5");
    private readonly ClientReactive<string> _devLocationDistance = new("0");
    private readonly ClientReactive<bool> _devLocationBackground = new(false);

    private void InitDevice()
    {
        app.Locations.OnUpdate(OnDeviceLocationUpdate);
        app.Motion.OnBatch(OnDeviceMotionBatch);
        app.Recordings.OnArchive(OnDeviceRecordingArchive);
        RegisterDeviceUpload();
    }

    private void RenderDeviceSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Device");

            RenderDeviceCapabilitiesCard(view);
            RenderDeviceLocationCard(view);
            RenderDeviceMotionCard(view);
            RenderDeviceRecordingCard(view);
            RenderDeviceLiveActivityCard(view);
            RenderDeviceUploadsCard(view);
        });
    }

    // Which client a card needs, shown beside its title, because a browser answers false to most of them.
    private static void RenderDeviceCardHeading(UIView view, string title, string? onlyOn = null)
    {
        view.Row([Layout.Row.Sm, "items-center flex-wrap mb-4"], content: view =>
        {
            view.Text([Text.H3], title);

            if (onlyOn != null)
            {
                view.Badge(onlyOn, SemanticTone.Info);
            }
        });
    }

    private void RenderDeviceLocationCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            RenderDeviceCardHeading(view, "Location");

            view.Text([Text.Label, "mb-2"], "One-shot");
            view.Row([Layout.Row.Md, "flex-wrap items-center mb-2"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Get location", icon: "map-pin", props: TestId("dev-location-oneshot-run"),
                    onClick: GetDeviceLocationOnceAsync);
            });
            view.Text([Text.Body, "mb-6"], _devLocationOneShot.Value, props: TestId("dev-location-oneshot"));

            view.Text([Text.Label, "mb-2"], "Continuous tracking");
            view.Row([Layout.Row.Md, "flex-wrap items-end mb-3"], content: view =>
            {
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Interval (s)");
                    view.Select(options: [new("1", "1"), new("5", "5"), new("10", "10"), new("30", "30")], bind: _devLocationInterval, ariaLabel: "Location interval seconds");
                });
                view.Box([FormField.Root], content: view =>
                {
                    view.Text([FormField.Label], "Distance filter (m)");
                    view.Select(options: [new("0", "0"), new("10", "10"), new("50", "50")], bind: _devLocationDistance, ariaLabel: "Location distance filter metres");
                });
                view.Row(["items-center gap-2"], content: view =>
                {
                    view.Switch([Switch.Default], bind: _devLocationBackground, props: AriaLabel("Track in background", testId: "dev-location-background"));
                    view.Text([Text.Caption], "Background (Flutter)");
                });
            });
            view.Row([Layout.Row.Md, "flex-wrap mb-2"], content: view =>
            {
                view.Button([Button.PrimaryMd], text: "Start tracking", icon: "locate", props: TestId("dev-location-start"),
                    onClick: StartDeviceLocationTrackingAsync);
                view.Button([Button.ErrorMd], text: "Stop tracking", icon: "locate-off", props: TestId("dev-location-stop"),
                    onClick: StopDeviceLocationTrackingAsync);
            });
            view.Text([Text.Body], _devLocationTracking.Value, props: TestId("dev-location-tracking"));
            view.Text([Text.Body, "mt-2"], $"Fixes received: {_devLocationFixCount.Value}", props: TestId("dev-location-count"));
            view.Text([Text.Body, "mt-1"], _devLocationLastFix.Value, props: TestId("dev-location-last"));
        });
    }

    private async Task GetDeviceLocationOnceAsync()
    {
        _devLocationOneShot.Value = "Requesting…";

        try
        {
            var location = await ClientFunctions.GetLocationAsync(ReactiveScope.ClientId);
            _devLocationOneShot.Value = location == null
                ? "FAIL one-shot: no location (unsupported client, permission denied or timed out)"
                : $"PASS one-shot: lat {Inv(location.Latitude, "F6")}, lon {Inv(location.Longitude, "F6")}, accuracy {Inv(location.Accuracy, "F0")} m";
        }
        catch (Exception ex)
        {
            _devLocationOneShot.Value = $"Error: one-shot location failed: {ex.Message}";
        }
    }

    private async Task StartDeviceLocationTrackingAsync()
    {
        var options = new LocationTrackingOptions(
            IntervalSeconds: int.Parse(_devLocationInterval.Value, CultureInfo.InvariantCulture),
            DistanceFilterMeters: int.Parse(_devLocationDistance.Value, CultureInfo.InvariantCulture),
            Background: _devLocationBackground.Value,
            NotificationTitle: "Validation is tracking",
            NotificationBody: "Location is streamed to the Validation app until you press Stop.");

        try
        {
            _devLocationFixCount.Value = 0;
            _devLocationLastFix.Value = "(waiting for the first fix)";
            bool started = await app.Locations.StartTrackingAsync(ReactiveScope.ClientId, options);
            _devLocationTracking.Value = started ? "Tracking: started (true)" : "Tracking: not supported on this client (false)";
        }
        catch (Exception ex)
        {
            _devLocationTracking.Value = $"Error: start tracking failed: {ex.Message}";
        }
    }

    private async Task StopDeviceLocationTrackingAsync()
    {
        try
        {
            bool stopped = await app.Locations.StopTrackingAsync(ReactiveScope.ClientId);
            _devLocationTracking.Value = stopped ? "Tracking: stopped (true)" : "Tracking: stop not supported on this client (false)";
        }
        catch (Exception ex)
        {
            _devLocationTracking.Value = $"Error: stop tracking failed: {ex.Message}";
        }
    }

    // Runs in the pushing client's scope, so the ClientReactive writes land on that client.
    private void OnDeviceLocationUpdate(LocationUpdate update)
    {
        int? scopeSessionId = ReactiveScope.ClientIdOrNull;
        int count = _devLocationFixCount.Value + 1;
        _devLocationFixCount.Value = count;

        string altitude = double.IsNaN(update.AltitudeMeters) ? "n/a" : $"{Inv(update.AltitudeMeters, "F1")} m";
        string heading = update.Heading < 0 ? "n/a" : $"{Inv(update.Heading, "F0")}°";
        string verdict = scopeSessionId == update.SessionId ? "PASS" : "FAIL";

        _devLocationLastFix.Value =
            $"{verdict} fix #{count}: lat {Inv(update.Latitude, "F6")}, lon {Inv(update.Longitude, "F6")}, " +
            $"accuracy {Inv(update.AccuracyMeters, "F0")} m, speed {Inv(update.SpeedMps, "F1")} m/s, heading {heading}, altitude {altitude}, " +
            $"measured {update.MeasuredAt:HH:mm:ss.fff}Z, received {update.At:HH:mm:ss.fff}Z, " +
            $"session {update.SessionId} (scope {scopeSessionId?.ToString(CultureInfo.InvariantCulture) ?? "none"}), user {(string.IsNullOrEmpty(update.UserId) ? "anonymous" : update.UserId)}";
    }

    private static string DeviceUserLabel()
    {
        string? userId = ReactiveScope.UserIdOrNull;
        return string.IsNullOrEmpty(userId) ? "(none)" : userId;
    }

    private static string Inv(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
}
