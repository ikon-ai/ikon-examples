using Ikon.AI.Emergence;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    public sealed class Explanation
    {
        public string Text { get; set; } = "";
        public int SentenceCount { get; set; }
    }

    public sealed class MathAnswer
    {
        public string FinalAnswer { get; set; } = "";
        public string Reasoning { get; set; } = "";
    }

    private readonly Reactive<string> _parallelConcept = new("how a hash map achieves average O(1) lookups");
    private readonly Reactive<int> _parallelCount = new(4);
    private readonly Reactive<int> _parallelMaxParallel = new(4);

    private readonly Reactive<string> _selfConsistencyProblem = new("A shirt costs $40 after a 20% discount. There is then 10% sales tax added. What is the final price? Give the number only in FinalAnswer.");
    private readonly Reactive<int> _selfConsistencySamples = new(5);

    private void RenderParallelBestOfExample(UIView view)
    {
        RenderPatternExample(
            view,
            exampleId: "sampling-parallel",
            title: "ParallelBestOf — Parallel Score & Select",
            description: "Like BestOf, but candidates run concurrently with an explicit concurrency limit. Best when you want many samples fast.",
            howItWorks:
                $"1. Generate {_parallelCount.Value} one-sentence explanations, up to {_parallelMaxParallel.Value} at a time.\n" +
                "2. Score each: reward a single tight sentence that names the concept.\n" +
                "3. Return the highest-scoring candidate.",
            runLabel: "Generate & Compare",
            resultLabel: "Best Explanation",
            config: cfg =>
            {
                cfg.Column([Layout.Column.Sm], content: c =>
                {
                    c.Text([Text.Caption, "text-muted-foreground"], "Concept to Explain");
                    c.TextField(
                        [Input.Default, "w-full"],
                        value: _parallelConcept.Value,
                        placeholder: "Explain in one sentence...",
                        onValueChange: async v => _parallelConcept.Value = v ?? "");
                });
                cfg.Row([Layout.Row.Lg], content: r =>
                {
                    RenderLabeledSlider(r, $"Candidates: {_parallelCount.Value}", _parallelCount.Value, 2, 8, 1, v => _parallelCount.Value = (int)v);
                    RenderLabeledSlider(r, $"Max Parallel: {_parallelMaxParallel.Value}", _parallelMaxParallel.Value, 1, 8, 1, v => _parallelMaxParallel.Value = (int)v);
                });
            },
            runAction: RunParallelBestOfExample);
    }

    private void RenderSelfConsistencyExample(UIView view)
    {
        RenderPatternExample(
            view,
            exampleId: "sampling-selfconsistency",
            title: "SelfConsistency — Majority Voting",
            description: "Sample the same reasoning problem several times at high temperature, then keep the answer the samples agree on most.",
            howItWorks:
                $"1. Draw {_selfConsistencySamples.Value} independent samples of the reasoning problem.\n" +
                "2. Group by final answer and pick the majority.\n" +
                "This smooths out one-off reasoning slips that any single sample might make.",
            runLabel: "Sample & Vote",
            resultLabel: "Majority Answer",
            config: cfg =>
            {
                cfg.Column([Layout.Column.Sm], content: c =>
                {
                    c.Text([Text.Caption, "text-muted-foreground"], "Reasoning Problem");
                    c.TextArea(
                        [Textarea.Default, "w-full h-20 font-mono text-xs"],
                        value: _selfConsistencyProblem.Value,
                        placeholder: "A word problem with a single numeric answer...",
                        onValueChange: async v => _selfConsistencyProblem.Value = v ?? "");
                });
                cfg.Row([Layout.Row.Lg], content: r =>
                {
                    RenderLabeledSlider(r, $"Samples: {_selfConsistencySamples.Value}", _selfConsistencySamples.Value, 3, 9, 1, v => _selfConsistencySamples.Value = (int)v);
                });
            },
            runAction: RunSelfConsistencyExample);
    }

    private async Task RunParallelBestOfExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Generating candidates...";

        try
        {
            var ctx = CreateContext();
            var concept = _parallelConcept.Value;
            var count = _parallelCount.Value;
            var maxParallel = _parallelMaxParallel.Value;

            state.Log($"Starting ParallelBestOf: count={count}, maxParallel={maxParallel}", LogLevel.Info);

            await foreach (var ev in Emerge.ParallelBestOf<Explanation>(LLMModel.Claude45Sonnet, ctx, opt =>
            {
                opt.Count = count;
                opt.MaxParallel = maxParallel;
                opt.Command = $"""
                    Explain in exactly one clear sentence: {concept}
                    Return JSON:
                    {opt.JsonSchema}
                    """;

                opt.Score = (explanation, trace) =>
                {
                    var text = explanation.Text ?? "";
                    var score = 0.0;

                    var sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries).Length;

                    if (sentences == 1)
                    {
                        score += 0.5;
                    }
                    else if (sentences == 2)
                    {
                        score += 0.2;
                    }

                    if (text.Length is >= 40 and <= 220)
                    {
                        score += 0.3;
                    }

                    var firstWord = concept.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

                    if (firstWord.Length > 0 && text.Contains(firstWord, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.2;
                    }

                    state.Log($"Scored {score:F2} ({sentences} sentence(s), {text.Length} chars): \"{TruncateJson(text, 60)}\"", LogLevel.Result);
                    return Math.Min(score, 1.0);
                };
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Completed<Explanation> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = "Complete — best candidate selected";
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

    private async Task RunSelfConsistencyExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Sampling...";

        try
        {
            var ctx = CreateContext();
            var problem = _selfConsistencyProblem.Value;
            var samples = _selfConsistencySamples.Value;

            state.Log($"Starting SelfConsistency with {samples} samples", LogLevel.Info);

            await foreach (var ev in Emerge.SelfConsistency<MathAnswer>(LLMModel.Claude45Sonnet, ctx, opt =>
            {
                opt.Samples = samples;
                opt.MaxParallel = samples;

                opt.Sample(s =>
                {
                    s.Temperature = 0.8;
                    s.Seed = s.Index * 1000;
                    s.Command = $"""
                        Solve this problem step by step. Put ONLY the final answer in FinalAnswer.

                        {problem}

                        Return JSON:
                        {s.JsonSchema}
                        """;
                });

                opt.SelectMajority = answers =>
                {
                    var groups = answers
                        .GroupBy(a => (a.FinalAnswer ?? "").Trim())
                        .OrderByDescending(g => g.Count())
                        .ToList();

                    foreach (var group in groups)
                    {
                        state.Log($"Vote: \"{group.Key}\" ×{group.Count()}", LogLevel.Result);
                    }

                    return groups.First().First();
                };
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Completed<MathAnswer> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = $"Complete — majority: {completed.Result.FinalAnswer}";
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
