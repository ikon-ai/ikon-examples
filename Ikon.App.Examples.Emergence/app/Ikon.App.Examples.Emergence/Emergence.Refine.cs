using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    public sealed class RefinedText
    {
        public string Text { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    private readonly Reactive<string> _refineProduct = new("A stainless-steel insulated water bottle that keeps drinks cold for 24 hours");
    private readonly Reactive<int> _refineMaxRefinements = new(3);
    private readonly Reactive<int> _refineTargetLength = new(140);

    private void RenderRefineSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Refine");
                view.Text([Text.Body, "text-muted-foreground"],
                    "Produce an initial answer, then iteratively improve it. Refine loops on the serialized result with a stop predicate.");
            });

            RenderRefineExample(view);
        });
    }

    private void RenderRefineExample(UIView view)
    {
        RenderPatternExample(
            view,
            exampleId: "refine-main",
            title: "Refine — Iterative Improvement",
            description: "Generate a first draft, then keep improving it until a stop condition is met or the refinement budget runs out.",
            howItWorks:
                $"1. Initial: write a first marketing description.\n" +
                $"2. Refinement: tighten and sharpen it (up to {_refineMaxRefinements.Value} passes).\n" +
                $"3. ShouldContinue: keep refining while the text is longer than {_refineTargetLength.Value} chars.\n" +
                "Each refinement automatically receives the previous attempt in context.",
            runLabel: "Refine",
            resultLabel: "Refined Text",
            config: cfg =>
            {
                cfg.Column([Layout.Column.Sm], content: c =>
                {
                    c.Text([Text.Caption, "text-muted-foreground"], "Product");
                    c.TextArea(
                        [Textarea.Default, "w-full h-16 font-mono text-xs"],
                        value: _refineProduct.Value,
                        placeholder: "Describe the product...",
                        onValueChange: async v => _refineProduct.Value = v ?? "");
                });
                cfg.Row([Layout.Row.Lg], content: r =>
                {
                    RenderLabeledSlider(r, $"Max Refinements: {_refineMaxRefinements.Value}", _refineMaxRefinements.Value, 1, 6, 1, v => _refineMaxRefinements.Value = (int)v);
                    RenderLabeledSlider(r, $"Target Length ≤ {_refineTargetLength.Value}", _refineTargetLength.Value, 60, 240, 10, v => _refineTargetLength.Value = (int)v);
                });
            },
            runAction: RunRefineExample);
    }

    private async Task RunRefineExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Drafting...";

        try
        {
            var ctx = CreateContext();
            var product = _refineProduct.Value;
            var maxRefinements = _refineMaxRefinements.Value;
            var targetLength = _refineTargetLength.Value;

            state.Log($"Starting Refine: maxRefinements={maxRefinements}, targetLength<={targetLength}", LogLevel.Info);

            await foreach (var ev in Emerge.Refine<RefinedText>(LLMModel.Claude45Sonnet, ctx, opt =>
            {
                opt.MaxRefinements = maxRefinements;

                opt.Initial(s =>
                {
                    s.Temperature = 0.8;
                    s.Command = $"""
                        Write a punchy marketing description for this product:
                        {product}

                        Return JSON:
                        {s.JsonSchema}
                        """;
                });

                opt.Refinement(s =>
                {
                    s.Temperature = 0.5;
                    s.Command = $"""
                        Improve the description: tighten the wording, make it more vivid and specific,
                        and bring it under {targetLength} characters without losing the hook.

                        Return JSON:
                        {s.JsonSchema}
                        """;
                });

                opt.ShouldContinue = (result, trace) =>
                {
                    var tooLong = result.Text.Length > targetLength;
                    state.CurrentIteration.Value++;
                    state.Log($"ShouldContinue: length={result.Text.Length} (target ≤ {targetLength}) → {(tooLong ? "refine again" : "stop")}", LogLevel.Result);
                    return Task.FromResult(tooLong);
                };
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<RefinedText> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                }

                if (ev is Completed<RefinedText> { Result: { } result })
                {
                    state.SetResult(result);
                    state.CurrentStage.Value = $"Complete — {result.Text.Length} chars";
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
