using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    public sealed class MergedIdeas
    {
        public List<string> Names { get; set; } = [];
        public string Rationale { get; set; } = "";
        public string TopPick { get; set; } = "";
    }

    private readonly Reactive<string> _ensembleTopic = new("a subscription app that turns your grocery receipts into recipes");
    private readonly Reactive<int> _ensembleSolvers = new(3);

    private void RenderEnsembleMergeSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "EnsembleMerge");
                view.Text([Text.Body, "text-muted-foreground"],
                    "Several solvers tackle the same problem from different angles in parallel, then a merger synthesizes them into one result.");
            });

            RenderPatternExample(
                view,
                exampleId: "orch-ensemble",
                title: "EnsembleMerge — Diverse Solutions Merged",
                description: "Several solvers tackle the same problem from different angles in parallel, then a merger synthesizes them into one result.",
                howItWorks:
                    $"1. Solvers: {_ensembleSolvers.Value} solvers each brainstorm names with a different temperature.\n" +
                    "2. Merger: dedupes, picks the strongest, and explains the top choice.\n" +
                    "Solvers run with isolated contexts for deterministic parallel execution.",
                runLabel: "Brainstorm & Merge",
                resultLabel: "Merged Ideas",
                config: cfg =>
                {
                    cfg.Column([Layout.Column.Sm], content: c =>
                    {
                        c.Text([Text.Caption, "text-muted-foreground"], "Product Concept");
                        c.TextArea(
                            [Textarea.Default, "w-full h-16 font-mono text-xs"],
                            value: _ensembleTopic.Value,
                            placeholder: "Describe the product to name...",
                            onValueChange: async v => _ensembleTopic.Value = v ?? "");
                    });
                    cfg.Row([Layout.Row.Lg], content: r =>
                    {
                        RenderLabeledSlider(r, $"Solvers: {_ensembleSolvers.Value}", _ensembleSolvers.Value, 2, 5, 1, v => _ensembleSolvers.Value = (int)v);
                    });
                },
                runAction: RunEnsembleMergeExample);
        });
    }

    private async Task RunEnsembleMergeExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Brainstorming...";

        try
        {
            var ctx = CreateContext();
            var topic = _ensembleTopic.Value;
            var solverCount = _ensembleSolvers.Value;

            state.Log($"Starting EnsembleMerge with {solverCount} solvers", LogLevel.Info);

            await foreach (var ev in Emerge.EnsembleMerge<MergedIdeas>(LLMModel.Claude45Sonnet, ctx, opt =>
            {
                opt.SolverCount = solverCount;
                opt.MaxParallel = solverCount;

                opt.Solver(s =>
                {
                    s.Temperature = 0.6 + 0.15 * s.Index;
                    s.Command = $"""
                        Brainstorm 4-6 memorable product names for: {topic}
                        Approach it from your own distinct angle (playful, literal, evocative, etc.).
                        Return JSON:
                        {s.JsonSchema}
                        """;
                });

                opt.Merger(m =>
                {
                    m.Temperature = 0.3;
                    m.Command = $"""
                        Combine all the brainstormed name lists. Remove duplicates and weak options,
                        keep the strongest, pick a single top choice, and explain why.
                        Return JSON:
                        {m.JsonSchema}
                        """;
                });
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<MergedIdeas> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                }

                if (ev is Completed<MergedIdeas> { Result: { } result })
                {
                    state.SetResult(result);
                    state.CurrentStage.Value = $"Complete — top pick: {result.TopPick}";
                }
            }
        }
        catch (OperationCanceledException)
        {
            state.Log("Cancelled", LogLevel.Error);
        }
        catch (Exception ex)
        {
            state.Log($"Error: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            state.IsRunning.Value = false;
        }
    }
}
