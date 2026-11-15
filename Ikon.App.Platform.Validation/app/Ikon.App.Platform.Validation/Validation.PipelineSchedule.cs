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

    // The pipeline writes from a different process, so the asset is the only channel and a
    // once-per-process read would go stale. While the card is on screen it is read again every interval,
    // and a failed read leaves the last known timestamp on screen. The validator judges how old that
    // timestamp is, the same way whether the asset or the read failed.
    private readonly Loader<AssetUri, string?> _lastScheduledPipelineRunUtc = new((uri, _) => ReadLastScheduledPipelineRunAsync(uri),
        new LoaderOptions { RefreshInterval = ScheduledPipelineRefreshInterval }, name: "LastScheduledPipelineRun");

    private AssetUri ScheduledPipelineUri =>
        new(AssetClass.CloudJson, ValidationSchedulePipeline.AssetPath, spaceId: app.GlobalState.SpaceId);

    private void RenderScheduledPipelineCard(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-3"], "Scheduled pipeline");

            var lastRun = _lastScheduledPipelineRunUtc.Get(ScheduledPipelineUri);

            if (lastRun.IsLoading)
            {
                view.Spinner();
                return;
            }

            RenderLastRun(view, lastRun.Value, "pipeline-schedule-timestamp");
        });
    }

    private static async Task<string?> ReadLastScheduledPipelineRunAsync(AssetUri uri)
    {
        var json = await Asset.Instance.TryGetTextAsync(uri);
        var run = json is null ? null : JsonSerializer.Deserialize<ValidationScheduledPipelineRun>(json);
        return run is { LastRunUtc.Length: > 0 } ? run.LastRunUtc : null;
    }
}
