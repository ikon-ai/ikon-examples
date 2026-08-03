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
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Cron heartbeat");
                view.Text([Text.Body, "mb-2"],
                    $"An hourly [Cron] (\"{HeartbeatSchedule}\") writes a timestamp to {HeartbeatAssetPath}. " +
                    "The platform validator fails when the timestamp goes stale, so a broken cron pipeline is caught automatically.");

                var timestamp = _lastHeartbeatUtc.Value;
                view.Text([Text.Body, "font-mono"], timestamp ?? "never", props: TestId("cron-heartbeat-timestamp"));

                if (timestamp is not null && DateTimeOffset.TryParse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                {
                    var age = DateTimeOffset.UtcNow - parsed;
                    view.Text([Text.Caption, "mt-1"], $"Age: {(int)age.TotalMinutes} min", props: TestId("cron-heartbeat-age"));
                }
            });
        });
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
