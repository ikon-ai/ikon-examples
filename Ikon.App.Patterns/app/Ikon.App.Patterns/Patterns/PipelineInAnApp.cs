namespace Ikon.App.Patterns.Patterns;

// Pattern: pipeline-in-an-app — see docs/patterns/pipeline-in-an-app.md.
// The two documents are shipped in the Patterns app's data/pipeline-demo/; the summaries are the
// gallery's mock model output.
internal sealed class PipelineInAnApp(IAppBase app) : IPatternDemo
{
    public string Slug => "pipeline-in-an-app";
    public string Title => "A pipeline run from inside the app";
    public string Category => "Performance & async";
    public void RenderDemo(IView view) => RenderSummaries((UIView)view);

    #region example:pattern-pipeline-in-an-app
    // One LLM call per document. The cache skips a document whose text, prompt and model have not
    // changed since the last run.
    [Pipeline(name: "summaries")]
    public sealed class SummaryPipeline
    {
        public async Task Run(Pipeline<Item>.Branch documents, CancellationToken cancellationToken)
        {
            documents.Transform(document => Summarise(document, cancellationToken)).Output();
        }

        // Static and given only the item. What a processor captures is part of its cache key, so it
        // cannot capture app.Files or the app. The app reads the inputs and writes the outputs.
        [Processor(version: 1, maxParallelism: 4)]
        private static async Task<List<Item>> Summarise(Item document, CancellationToken cancellationToken)
        {
            var text = await document.GetContentAsString();

            // Billed to the app's space, like an AI call from a button handler
            var summary = await Emerge.Run<string>(LLMModel.Claude45Haiku, pass =>
            {
                pass.SystemPrompt = "Summarise the document in two sentences.";
                pass.Command = text;
            }, cancellationToken);

            return [await Item.Create(document, document.Name, summary, MimeTypes.TextPlain)];
        }
    }

    // Paths in app.Files.Data: shipped in data/, or written there when a person uploads a file
    private readonly ReactiveList<string> _documents = new(["pipeline-demo/harbour.txt", "pipeline-demo/orchard.txt"]);
    private readonly ReactiveDictionary<string, string> _summaries = new();
    private readonly Reactive<int> _done = new(0);
    private readonly Reactive<bool> _running = new(false);
    private readonly Reactive<string?> _error = new(null);

    private async Task SummariseAllAsync()
    {
        if (_running.Value)
        {
            return;
        }

        _running.Value = true;
        _done.Value = 0;
        _error.Value = null;

        try
        {
            // Held up to the 30 minutes passed here, so the run finishes even if the person who started it leaves
            await using var hold = await app.BackgroundWork.StartAsync(TimeSpan.FromMinutes(30), "summary pipeline");

            await using var runner = new PipelineRunner();

            // Use Initialize(Config), not Initialize<TPipeline>. Initialize<TPipeline> is for a standalone
            // program: it signs in and starts its own credential refresh in place of the app's.
            await runner.Initialize(new PipelineRunner.Config
            {
                TypeName = typeof(SummaryPipeline).FullName!,
                // Local disk, kept while this instance runs. A restart, a deploy or a start after an
                // idle stop begins with an empty cache, so whatever must not be redone goes to app.Files.
                CachePath = Path.Combine(Path.GetTempPath(), "summary-pipeline"),
                StatusUpdateInterval = 1,
                DisableMetadataOutput = true,
            });
            runner.StatusUpdate += (_, status) => _done.Value = status.OutputItemCount;

            // Create the items after Initialize, on the runner's async flow, because they keep their content in its cache
            var paths = _documents.ToList();
            var texts = await Task.WhenAll(paths.Select(path => app.Files.Data.ReadTextAsync(path)));
            var inputs = new List<Item>();

            for (var i = 0; i < paths.Count; i++)
            {
                inputs.Add(await Item.CreateInitial(paths[i], texts[i], MimeTypes.TextPlain));
            }

            // Each summary is stored as soon as it is ready, so a run cut short keeps what it finished
            await foreach (var summary in runner.RunAsEnumerable(inputs))
            {
                var text = await summary.GetContentAsString();
                await app.Files.Data.WriteTextAsync($"summaries/{summary.Name}", text);
                _summaries[summary.Name] = text;
            }
        }
        catch (PipelineException ex)
        {
            Log.Instance.Warning($"Summary pipeline stopped after {_done.Value} documents: {ex.Message}");
            _error.Value = "Some documents were not summarised. Run it again to retry them.";
        }
        finally
        {
            _running.Value = false;
        }
    }

    private void RenderSummaries(UIView view)
    {
        view.Column([Card.Default, "gap-3 p-4 w-full max-w-2xl"], content: col =>
        {
            col.Row(["items-center justify-between gap-3 flex-wrap"], content: row =>
            {
                row.Text([Text.H3], "Summaries");
                row.Button([Button.PrimaryMd], text: _running.Value ? $"Summarising… {_done.Value} of {_documents.Count}" : "Summarise all",
                    disabled: _running.Value, onClick: SummariseAllAsync);
            });

            if (_error.Value is { } error)
            {
                col.Text([Text.Caption, "text-destructive"], error);
            }

            foreach (var path in _documents)
            {
                col.Column(["gap-1"], key: path, content: item =>
                {
                    item.Text([Text.Body, "font-medium"], path);
                    item.Text([Text.Caption], _summaries.TryGetValue(path, out var summary) ? summary : "Not summarised yet");
                });
            }
        });
    }
    #endregion
}
