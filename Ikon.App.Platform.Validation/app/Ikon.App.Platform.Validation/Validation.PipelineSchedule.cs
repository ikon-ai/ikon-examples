// The scheduled counterpart to Validation.Pipeline.cs (which covers the HTTPS-endpoint mode).
// It is written ONLY by the scheduled run, never by the app process — the platform validator
// redeploys this app continuously, so an app-side write would keep the timestamp fresh even with
// a fully broken pipeline scheduler and defeat the freshness check.
[Pipeline(
    "Records the time of every scheduled run so the platform validator can prove pipeline scheduling still fires",
    name: "validation-schedule",
    executionMode: PipelineExecutionMode.Scheduled,
    schedule: ValidationSchedulePipeline.Schedule)]
public class ValidationSchedulePipeline(IPipelineHost<EmptyPipelineConfig> host)
{
    // Offset from the app's [Cron] heartbeat (minute 0) so the two scheduled paths are visibly
    // independent rather than two readings of one tick.
    public const string Schedule = "30 * * * *";
    public const string AssetPath = "validation/scheduled-pipeline.json";

    public Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        var spaceId = host.SpaceId;
        inputItems.Transform(item => RecordRun(item, spaceId, cancellationToken)).Output();
        return Task.CompletedTask;
    }

    // A scheduled fire has no inputs, so the runner injects the same constant root item every time
    // and the item hash never changes. Without skipCache every run after the first would cache-hit
    // and the timestamp would sit still while the scheduler was working perfectly.
    [Processor(skipCache: true)]
    private static async Task<List<Item>> RecordRun(Item seed, string spaceId, CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var json = JsonSerializer.Serialize(
            new ValidationScheduledPipelineRun { LastRunUtc = timestamp, Schedule = Schedule },
            new JsonSerializerOptions { WriteIndented = true });
        var uri = new AssetUri(AssetClass.CloudJson, AssetPath, spaceId: spaceId);
        await Asset.Instance.SetTextAsync(uri, json, cancellationToken: cancellationToken);
        Log.Instance.Info($"Validation scheduled pipeline ran at {timestamp} for space {spaceId}");
        return [await Item.Create(seed, "scheduled-run.json", json, MimeTypes.ApplicationJson)];
    }
}

public class ValidationScheduledPipelineRun
{
    public string LastRunUtc { get; set; } = "";
    public string Schedule { get; set; } = "";
}

public partial class Validation
{
    private static readonly TimeSpan ScheduledPipelineRefreshInterval = TimeSpan.FromSeconds(15);

    private readonly Reactive<string?> _lastScheduledPipelineRunUtc = new(null);
    private readonly Reactive<bool> _scheduledPipelineStatusLoaded = new(false);
    private DateTime _scheduledPipelineReadStartedAt = DateTime.MinValue;
    private int _scheduledPipelineReadRunning;

    private AssetUri ScheduledPipelineUri =>
        new(AssetClass.CloudJson, ValidationSchedulePipeline.AssetPath, spaceId: app.GlobalState.SpaceId);

    private void RenderScheduledPipelineCard(UIView view)
    {
        RefreshScheduledPipelineStatus();

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Scheduled pipeline");
            view.Text([Text.Body, "mb-2"],
                $"An hourly [Pipeline] (\"{ValidationSchedulePipeline.Schedule}\") writes its run time to {ValidationSchedulePipeline.AssetPath}. " +
                "It runs in its own pipeline runtime rather than in the app process, so it exercises a scheduler the cron heartbeat above does not.");

            if (!_scheduledPipelineStatusLoaded.Value)
            {
                view.Spinner();
                return;
            }

            var timestamp = _lastScheduledPipelineRunUtc.Value;
            view.Text([Text.Body, "font-mono"], timestamp ?? "never", props: TestId("pipeline-schedule-timestamp"));

            if (timestamp is not null && DateTimeOffset.TryParse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                var age = DateTimeOffset.UtcNow - parsed;
                view.Text([Text.Caption, "mt-1"], $"Age: {(int)age.TotalMinutes} min", props: TestId("pipeline-schedule-age"));
            }
        });
    }

    // The pipeline writes from a different process, so the asset is the only channel and a
    // once-per-process seed would go stale. Re-read on every render of the section instead,
    // rate-limited so the re-render each read triggers cannot feed itself.
    private void RefreshScheduledPipelineStatus()
    {
        if (DateTime.UtcNow - _scheduledPipelineReadStartedAt < ScheduledPipelineRefreshInterval)
        {
            return;
        }

        if (Interlocked.Exchange(ref _scheduledPipelineReadRunning, 1) == 1)
        {
            return;
        }

        _scheduledPipelineReadStartedAt = DateTime.UtcNow;

        _ = Task.Run(async () =>
        {
            // Started from inside a UI render (a reactive callback), whose async-local flows in via
            // ExecutionContext. Detach, or the writes below are swallowed as re-entrant and the card
            // keeps showing "never" until some unrelated change happens to re-render it.
            using var reactiveDetach = ReactiveManager.SuppressCallbackTracking();

            try
            {
                var json = await Asset.Instance.TryGetTextAsync(ScheduledPipelineUri);
                var run = json is null ? null : JsonSerializer.Deserialize<ValidationScheduledPipelineRun>(json);

                if (run is { LastRunUtc.Length: > 0 })
                {
                    _lastScheduledPipelineRunUtc.Value = run.LastRunUtc;
                }
            }
            catch (Exception ex)
            {
                // A failed read leaves the last known timestamp on screen. Staleness is what the
                // validator judges, and it is judged the same whether the asset or the read broke.
                Log.Instance.Warning($"Validation scheduled pipeline status read failed: {ex.Message}");
            }
            finally
            {
                _scheduledPipelineStatusLoaded.Value = true;
                Interlocked.Exchange(ref _scheduledPipelineReadRunning, 0);
            }
        });
    }
}
