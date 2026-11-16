namespace Ikon.App.Platform.Patterns.Patterns;

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
    // One LLM call per distinct text. The cache skips a text it has already summarised. Its key is
    // the content alone, not the item's name, and the prompt and the model are not part of it either:
    // after changing either, raise the processor's version, or the next run returns the old summaries.
    [Pipeline(name: "summaries")]
    public sealed class SummaryPipeline
    {
        public async Task Run(Pipeline<Item>.Branch documents, CancellationToken cancellationToken)
        {
            documents.Transform(document => Summarise(document, cancellationToken)).Output();
        }

        // Static and given only the item. What a processor captures is part of its cache key, so it
        // cannot capture app.Files or the app. The app reads the inputs and writes the outputs.
        [Processor(version: 2, maxParallelism: 4)]
        private static async Task<List<Item>> Summarise(Item document, CancellationToken cancellationToken)
        {
            var text = await document.GetContentAsString();

            // Billed to the app's space, like an AI call from a button handler
            var summary = await Emerge.Run<string>(LLMModel.Claude45Haiku, pass =>
            {
                pass.SystemPrompt = "Summarise the document in two sentences.";
                pass.Command = text;
            }, cancellationToken);

            // Named by the input's content hash, which a cached output shares with every input of the
        // same text, whatever that input was called
        return [await Item.Create(document, document.Hash, summary, MimeTypes.TextPlain)];
        }
    }

    // Paths in app.Files.Data: shipped in data/, or written there when a person uploads a file
    private readonly ReactiveList<string> _documents = new(["pipeline-demo/harbour.txt", "pipeline-demo/orchard.txt"]);
    private readonly ReactiveDictionary<string, string> _summaries = new();
    private readonly Reactive<int> _done = new(0);
    private readonly Reactive<int> _total = new(0);
    private readonly Reactive<bool> _running = new(false);
    private readonly Reactive<string?> _error = new(null);

    private async Task SummariseAllAsync()
    {
        if (_running.Value)
        {
            return;
        }

        using var running = _running.AsToken();
        _done.Value = 0;
        _total.Value = _documents.Count;
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
                // Local disk, shared by every instance and run in the process, which is safe because an
                // output is matched back by content hash. A restart, a deploy or a start after an idle
                // stop begins with an empty cache, so whatever must not be redone goes to app.Files.
                CachePath = Path.Combine(Path.GetTempPath(), "summary-pipeline"),
                StatusUpdateInterval = 1,
                DisableMetadataOutput = true,
            });
            runner.StatusUpdate += (_, status) => _done.Value = status.OutputItemCount;

            // Create the items after Initialize, on the runner's async flow, because they keep their content in its cache
            var paths = _documents.ToList();
            var texts = await Task.WhenAll(paths.Select(path => app.Files.Data.ReadTextAsync(path)));
            var inputs = new List<Item>();

            // One input per distinct text, with the app's own paths kept by its content hash: two
            // inputs with the same text would both get the output of whichever the cache saw first
            var pathsByHash = new Dictionary<string, List<string>>();

            for (var i = 0; i < paths.Count; i++)
            {
                var input = await Item.CreateInitial(paths[i], texts[i], MimeTypes.TextPlain);

                if (pathsByHash.TryGetValue(input.Hash, out var sameText))
                {
                    sameText.Add(paths[i]);
                    continue;
                }

                pathsByHash[input.Hash] = [paths[i]];
                inputs.Add(input);
            }

            _total.Value = inputs.Count;

            // Each summary is stored as soon as it is ready, so a run cut short keeps what it finished
            await foreach (var summary in runner.RunAsEnumerable(inputs))
            {
                var text = await summary.GetContentAsString();

                foreach (var path in pathsByHash[summary.Name])
                {
                    await app.Files.Data.WriteTextAsync($"summaries/{path}", text);
                    _summaries[path] = text;
                }
            }
        }
        catch (PipelineException ex)
        {
            Log.Instance.Warning($"Summary pipeline stopped after {_done.Value} documents: {ex.Message}");
            _error.Value = "Some documents were not summarised. Run it again to retry them.";
        }
    }

    private void RenderSummaries(UIView view)
    {
        view.Column([Card.Default, "gap-3 p-4 w-full max-w-2xl"], content: col =>
        {
            col.Row(["items-center justify-between gap-3 flex-wrap"], content: row =>
            {
                row.Text([Text.H3], "Summaries");
                row.Button([Button.PrimaryMd], text: _running.Value ? $"Summarising… {_done.Value} of {_total.Value}" : "Summarise all",
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
