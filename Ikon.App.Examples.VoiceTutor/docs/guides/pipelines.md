# Pipelines

## Pipelines

Pipelines run background data processing jobs (ETL, batch processing, scheduled cron tasks). NOT for app timers or game loops — for those, use `PeriodicTimer` or `Task.Delay` in async methods.

```csharp
[Pipeline(name: "example")]
public class ExamplePipeline
{
    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        var outputItems = inputItems.Transform(item => Process(item, cancellationToken));
        outputItems.Output();
    }

    [Processor]
    private static async Task<List<Item>> Process(Item inputItem, CancellationToken cancellationToken)
    {
        var text = await inputItem.GetContentAsString();
        var outputItem = await Item.Create(inputItem, $"{inputItem.Name}.result", $"Processed: {text}", MimeTypes.TextPlain);
        return [outputItem];
    }
}
```

Execution modes are configured via C# attributes on pipeline classes. Modes: `HttpsEndpoint` (callable via HTTPS), `Scheduled` (cron). Example: `[Pipeline("Description", executionMode: PipelineExecutionMode.Scheduled, schedule: "0 0 * * *")]`

To run a pipeline locally during development, use `ikon app pipeline run <PipelineName>` from the project root. It builds the app, picks up the space ID from `ikon-config.toml`, and forwards any `ikon pipeline run` flags (`--input`, `--output`, `--config`, etc.).

Pipelines can read secrets and the current organisation/space by taking an `IPipelineHost<TConfig>` constructor parameter (use `EmptyPipelineConfig` when no config is needed) — see the Secrets and Credentials section above and the Pipelines API Reference for the `host.Secrets` / `host.OrganisationId` / `host.SpaceId` accessors.
