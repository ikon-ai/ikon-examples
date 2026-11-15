using System.Text.Json;

public partial class Validation
{
    private const string HeartbeatSchedule = "0 * * * *";
    private const string HeartbeatAssetPath = "validation/heartbeat.json";

    // Read from the persisted asset once per process, so the last heartbeat is visible after a restart
    // without waiting for the next tick
    private readonly Loader<AssetUri, string?> _lastHeartbeatUtc = new((uri, _) => ReadLastHeartbeatAsync(uri), name: "LastHeartbeat");

    private AssetUri HeartbeatUri => new(AssetClass.CloudJson, HeartbeatAssetPath, spaceId: app.GlobalState.SpaceId);

    // The heartbeat is written ONLY by the cron tick, never at startup — the platform validator
    // redeploys this app continuously, so a startup write would keep the timestamp fresh even with
    // a fully broken cron pipeline and defeat the freshness check.
    [Cron(HeartbeatSchedule, Name = "validation.heartbeat")]
    internal async Task WriteHeartbeatAsync(CancellationToken ct = default)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("O");
            var json = JsonSerializer.Serialize(
                new ValidationHeartbeat { LastHeartbeatUtc = timestamp, Schedule = HeartbeatSchedule },
                new JsonSerializerOptions { WriteIndented = true });
            await Asset.Instance.SetTextAsync(HeartbeatUri, json);
            Log.Instance.Info($"Validation heartbeat written at {timestamp}");
        }
        catch (Exception ex)
        {
            Log.Instance.Error(ex, $"Validation heartbeat write failed");
        }
    }

    private void RenderCronSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Cron");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-3"], "Scheduled function");
                RenderLastRun(view, _lastHeartbeatUtc.Get(HeartbeatUri).Value, "cron-heartbeat-timestamp");
            });

            RenderScheduledPipelineCard(view);
        });
    }

    // In the viewer's own time zone, from the one their client reported when it connected. The value
    // keeps its UTC offset because the validation script reads it back as an ISO timestamp.
    private void RenderLastRun(UIView view, string? timestamp, string testId, string label = "Last run:")
    {
        if (timestamp is null || !DateTimeOffset.TryParse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
        {
            view.Row([Layout.Row.Sm, "items-baseline"], content: view =>
            {
                view.Text([Text.Body], label);
                view.Text([Text.Body], timestamp ?? "never", props: TestId(testId));
            });
            return;
        }

        var zone = ViewerTimeZone();
        var local = TimeZoneInfo.ConvertTime(parsed, zone);

        view.Row([Layout.Row.Sm, "items-baseline flex-wrap"], content: view =>
        {
            view.Text([Text.Body], label);
            view.Text([Text.Body], local.ToString("yyyy-MM-dd HH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture), props: TestId(testId));
            view.Text([Text.Caption], zone.Id);
        });
    }

    private TimeZoneInfo ViewerTimeZone()
    {
        var reported = app.GlobalState.GetClientContext(ReactiveScope.ClientId)?.Timezone;

        if (!string.IsNullOrEmpty(reported) && TimeZoneInfo.TryFindSystemTimeZoneById(reported, out var zone))
        {
            return zone;
        }

        return TimeZoneInfo.Utc;
    }

    // Read-only — see WriteHeartbeatAsync. No heartbeat asset yet reads as none, and the UI shows
    // "never" until the first tick writes it.
    private static async Task<string?> ReadLastHeartbeatAsync(AssetUri uri)
    {
        var json = await Asset.Instance.TryGetTextAsync(uri);
        var heartbeat = json is null ? null : JsonSerializer.Deserialize<ValidationHeartbeat>(json);
        return string.IsNullOrEmpty(heartbeat?.LastHeartbeatUtc) ? null : heartbeat.LastHeartbeatUtc;
    }
}

public class ValidationHeartbeat
{
    public string LastHeartbeatUtc { get; set; } = "";
    public string Schedule { get; set; } = "";
}
