using Ikon.AI.Emergence;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    public sealed class RefinedText
    {
        public string Text { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    public sealed class CommitMessage
    {
        public string Message { get; set; } = "";
        public string Rationale { get; set; } = "";
    }

    private readonly Reactive<string> _selectedRefineExample = new("refine");

    private readonly Reactive<string> _refineProduct = new("A stainless-steel insulated water bottle that keeps drinks cold for 24 hours");
    private readonly Reactive<int> _refineMaxRefinements = new(3);
    private readonly Reactive<int> _refineTargetLength = new(140);

    private readonly Reactive<string> _testRefineChange = new("Added a null check in the user-avatar renderer so it falls back to initials when no image is set");
    private readonly Reactive<int> _testRefineMaxIterations = new(4);

    private void RenderRefineSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Refine Patterns");
                view.Text([Text.Body, "text-muted-foreground"],
                    "Produce an initial answer, then iteratively improve it. Refine loops on the serialized result with a " +
                    "stop predicate; TestRefine additionally applies and tests each iteration in the real world before refining.");
            });

            RenderExampleSelector(view, _selectedRefineExample,
                ("refine", "Refine"),
                ("testrefine", "TestRefine"));

            switch (_selectedRefineExample.Value)
            {
                case "refine":
                    RenderRefineExample(view);
                    break;
                case "testrefine":
                    RenderTestRefineExample(view);
                    break;
            }
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

    private void RenderTestRefineExample(UIView view)
    {
        RenderPatternExample(
            view,
            exampleId: "refine-test",
            title: "TestRefine — Generate, Test, Refine",
            description: "An agentic loop that evaluates each attempt against real rules and feeds the failures back into the next refinement.",
            howItWorks:
                $"1. Initial: write a conventional-commit message for the change.\n" +
                $"2. Evaluate: a real validator checks subject length ≤ 72, no trailing period, and a 'type: summary' prefix.\n" +
                $"3. Refinement: fix exactly the reported issues (up to {_testRefineMaxIterations.Value} iterations).\n" +
                "Unlike Refine, the Evaluate callback grounds each iteration in actual test feedback.",
            runLabel: "Generate & Test",
            resultLabel: "Commit Message",
            config: cfg =>
            {
                cfg.Column([Layout.Column.Sm], content: c =>
                {
                    c.Text([Text.Caption, "text-muted-foreground"], "Change Description");
                    c.TextArea(
                        [Textarea.Default, "w-full h-16 font-mono text-xs"],
                        value: _testRefineChange.Value,
                        placeholder: "Describe the code change...",
                        onValueChange: async v => _testRefineChange.Value = v ?? "");
                });
                cfg.Row([Layout.Row.Lg], content: r =>
                {
                    RenderLabeledSlider(r, $"Max Iterations: {_testRefineMaxIterations.Value}", _testRefineMaxIterations.Value, 1, 6, 1, v => _testRefineMaxIterations.Value = (int)v);
                });
            },
            runAction: RunTestRefineExample);
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

                if (ev is Completed<RefinedText> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = $"Complete — {completed.Result.Text.Length} chars";
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

    private async Task RunTestRefineExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Generating...";

        try
        {
            var ctx = CreateContext();
            var change = _testRefineChange.Value;
            var maxIterations = _testRefineMaxIterations.Value;

            state.Log($"Starting TestRefine: maxIterations={maxIterations}", LogLevel.Info);

            await foreach (var ev in Emerge.TestRefine<CommitMessage>(LLMModel.Claude45Sonnet, ctx, opt =>
            {
                opt.MaxIterations = maxIterations;

                opt.Initial(s =>
                {
                    s.Command = $"""
                        Write a conventional-commit message for this change:
                        {change}

                        Return JSON:
                        {s.JsonSchema}
                        """;
                });

                opt.Refinement(s =>
                {
                    s.Command = $"""
                        Fix the commit message so it satisfies all reported issues. Keep the meaning.

                        Return JSON:
                        {s.JsonSchema}
                        """;
                });

                opt.Evaluate = (result, iteration) =>
                {
                    var issues = new List<string>();
                    var message = result.Message ?? "";
                    var subject = message.Split('\n', 2)[0].Trim();

                    if (subject.Length == 0)
                    {
                        issues.Add("Message is empty.");
                    }

                    if (subject.Length > 72)
                    {
                        issues.Add($"Subject is {subject.Length} chars; keep it ≤ 72.");
                    }

                    if (subject.EndsWith('.'))
                    {
                        issues.Add("Subject must not end with a period.");
                    }

                    if (!subject.Contains(':'))
                    {
                        issues.Add("Use a 'type: summary' prefix (e.g. 'fix: ...').");
                    }

                    state.CurrentIteration.Value = iteration + 1;
                    state.Log($"Test iteration {iteration}: subject=\"{TruncateJson(subject, 50)}\" → {issues.Count} issue(s)",
                        issues.Count > 0 ? LogLevel.Stage : LogLevel.Result);

                    foreach (var issue in issues)
                    {
                        state.Log($"  - {issue}", LogLevel.Stage);
                    }

                    return Task.FromResult(new TestRefineFeedback
                    {
                        Continue = issues.Count > 0,
                        Feedback = issues.Count > 0
                            ? "Fix these issues:\n" + string.Join("\n", issues.Select(i => $"- {i}"))
                            : "All checks passed.",
                    });
                };
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<CommitMessage> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                }

                if (ev is Completed<CommitMessage> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = "Complete";
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
