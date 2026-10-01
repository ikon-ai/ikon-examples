using System.Text.Json;

public partial class Validation
{
    private const string HeartbeatSchedule = "0 * * * *";
    private const string HeartbeatAssetPath = "validation/heartbeat.json";

    private readonly Reactive<string?> _lastHeartbeatUtc = new(null);
    private int _heartbeatSeedStarted;

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
            _lastHeartbeatUtc.Value = timestamp;
            Log.Instance.Info($"Validation heartbeat written at {timestamp}");
        }
        catch (Exception ex)
        {
            Log.Instance.Error(ex, $"Validation heartbeat write failed");
        }
    }

    private void RenderCronSection(UIView view)
    {
        SeedHeartbeatFromAsset();

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Cron");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-3"], "Scheduled function");
                RenderLastRun(view, _lastHeartbeatUtc.Value, "cron-heartbeat-timestamp");
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
        var reported = app.GlobalState.Clients.TryGetValue(ReactiveScope.ClientId, out var client) ? client.Timezone : null;

        if (!string.IsNullOrEmpty(reported) && TimeZoneInfo.TryFindSystemTimeZoneById(reported, out var zone))
        {
            return zone;
        }

        return TimeZoneInfo.Utc;
    }

    // Seed the UI mirror from the persisted asset once per process, so the last heartbeat is
    // visible after a restart without waiting for the next tick. Read-only — see WriteHeartbeatAsync.
    private void SeedHeartbeatFromAsset()
    {
        if (Interlocked.Exchange(ref _heartbeatSeedStarted, 1) == 1)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            // Started from inside a UI render (a reactive callback), whose async-local flows in via
            // ExecutionContext. Detach, or the seed write below is swallowed as re-entrant.
            using var reactiveDetach = ReactiveManager.SuppressCallbackTracking();

            try
            {
                var json = await Asset.Instance.GetTextAsync(HeartbeatUri);
                var heartbeat = JsonSerializer.Deserialize<ValidationHeartbeat>(json);

                if (!string.IsNullOrEmpty(heartbeat?.LastHeartbeatUtc))
                {
                    // Never move the timestamp backwards: a cron tick may have written between
                    // the read starting and this update landing.
                    if (_lastHeartbeatUtc.Value is null)
                    {
                        _lastHeartbeatUtc.Value = heartbeat.LastHeartbeatUtc;
                    }
                }
            }
            catch
            {
                // No heartbeat asset yet — the UI shows "never" until the first tick writes it.
            }
        });
    }
}

public class ValidationHeartbeat
{
    public string LastHeartbeatUtc { get; set; } = "";
    public string Schedule { get; set; } = "";
}
