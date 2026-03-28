using System.Text.Json;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    public sealed class ResearchFinding
    {
        public string Topic { get; set; } = "";
        public List<string> KeyPoints { get; set; } = [];
        public string Summary { get; set; } = "";
    }

    public sealed class SynthesizedSection
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public List<string> Sources { get; set; } = [];
    }

    public sealed class FinalReport
    {
        public string Title { get; set; } = "";
        public string ExecutiveSummary { get; set; } = "";
        public List<SynthesizedSection> Sections { get; set; } = [];
        public string Conclusion { get; set; } = "";
    }

    private readonly Reactive<string> _taskGraphTopic = new("Impact of artificial intelligence on modern software development");
    private readonly Reactive<int> _taskGraphParallel = new(3);
    private readonly Reactive<double> _taskGraphTemperature = new(0.5);
    private readonly Reactive<bool> _taskGraphEnableReview = new(true);
    private readonly Reactive<string> _selectedTaskGraphExample = new("research");

    private void RenderTaskGraphSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "TaskGraph Pattern");
                view.Text([Text.Body, "text-muted-foreground"],
                    "Executes tasks with dependencies in the optimal order. Tasks without dependencies run in parallel, " +
                    "while dependent tasks wait for their prerequisites. Includes optional parallel review and plan revision.");
            });

            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H3, "mb-3"], "Choose Example");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    var examples = new[] { ("research", "Research Report"), ("analysis", "Multi-Step Analysis") };
                    foreach (var (key, label) in examples)
                    {
                        var isSelected = _selectedTaskGraphExample.Value == key;
                        view.Button(
                            [isSelected ? Button.PrimaryMd : Button.OutlineMd],
                            label: label,
                            onClick: async () => _selectedTaskGraphExample.Value = key);
                    }
                });
            });

            switch (_selectedTaskGraphExample.Value)
            {
                case "research":
                    RenderResearchReportExample(view);
                    break;
                case "analysis":
                    RenderMultiStepAnalysisExample(view);
                    break;
            }
        });
    }

    private void RenderResearchReportExample(UIView view)
    {
        var state = GetOrCreateState("taskgraph-research");

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Research Report Generation");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "Demonstrates dependency-aware parallel execution: research tasks run in parallel, then synthesis tasks " +
                        "execute when their dependencies complete, followed by final report generation.");

                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "Task dependency graph:");
                        view.Text([Text.Caption, "text-blue-300"],
                            "Task 1: Research benefits (no deps) ──┐\n" +
                            "Task 2: Research challenges (no deps) ├─→ Task 4: Synthesize benefits+challenges ──┐\n" +
                            "Task 3: Research future trends (no deps) ├─→ Task 5: Synthesize challenges+trends ──┼─→ Task 6: Write final report\n" +
                            "                                       └────────────────────────────────────────────┘\n\n" +
                            "Tasks 1, 2, 3 execute in parallel. Task 4 waits for 1+2. Task 5 waits for 2+3. Task 6 waits for 4+5.");
                    });

                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-3"], "Configuration");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            view.Column([Layout.Column.Sm], content: view =>
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Research Topic");
                                view.TextField(
                                    [Input.Default, "w-full"],
                                    value: _taskGraphTopic.Value,
                                    placeholder: "Enter research topic...",
                                    onValueChange: async v => _taskGraphTopic.Value = v ?? "");
                            });

                            view.Row([Layout.Row.Lg], content: view =>
                            {
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Parallel Workers: {_taskGraphParallel.Value}");
                                    RenderSlider(view, _taskGraphParallel.Value, 1, 5, 1, v => _taskGraphParallel.Value = (int)v);
                                });

                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Temperature: {_taskGraphTemperature.Value:F1}");
                                    RenderSlider(view, _taskGraphTemperature.Value, 0.0, 1.0, 0.1, v => _taskGraphTemperature.Value = v);
                                });

                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Row([Layout.Row.Sm, "items-center"], content: view =>
                                    {
                                        view.Checkbox(
                                            [Checkbox.Root],
                                            @checked: _taskGraphEnableReview.Value,
                                            onCheckedChange: async value => _taskGraphEnableReview.Value = value,
                                            content: v => v.CheckboxIndicator([Checkbox.Indicator], content: i => i.Icon([Icon.Default], name: "check")));
                                        view.Text([Text.Caption, "text-muted-foreground ml-2"], "Enable parallel review");
                                    });
                                });
                            });
                        });
                    });

                    if (state.IsRunning.Value)
                    {
                        view.Box(["bg-yellow-500/10 border border-yellow-500/20 rounded-lg p-4 mb-4"], content: view =>
                        {
                            view.Row([Layout.Row.Sm, "items-center"], content: view =>
                            {
                                view.Box([Icon.Spinner, "mr-2 text-yellow-400"]);
                                view.Column([Layout.Column.Sm], content: view =>
                                {
                                    view.Text([Text.Caption, "text-yellow-400 font-semibold"], state.CurrentStage.Value);
                                    view.Text([Text.Caption, "text-yellow-300"],
                                        $"Tasks completed: {state.CurrentIteration.Value}");
                                });
                            });
                        });
                    }

                    view.Row([Layout.Row.Md], content: view =>
                    {
                        if (state.IsRunning.Value)
                        {
                            view.Button([Button.DangerMd], label: "Stop", onClick: async () =>
                            {
                                _cts?.Cancel();
                                state.IsRunning.Value = false;
                            });
                        }
                        else
                        {
                            view.Button([Button.PrimaryMd], label: "Generate Report", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunResearchReportExample(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear", onClick: async () => state.Clear());
                    });
                });
            });

            view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
            {
                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] overflow-hidden"], content: view =>
                {
                    view.Row(["justify-between items-center mb-2"], content: view =>
                    {
                        view.Text([Text.H3], "Event Stream");
                        if (state.Logs.Value.Count > 0)
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], $"{state.Logs.Value.Count} events");
                        }
                    });

                    RenderLogList(view, state);
                });

                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] overflow-hidden"], content: view =>
                {
                    view.Text([Text.H3, "mb-2"], "Final Report");
                    var hasResult = !string.IsNullOrEmpty(state.ResultJson.Value);
                    view.Box(["overflow-y-auto h-[calc(100%-2rem)] font-mono text-xs"], key: hasResult ? "has-result" : "no-result", content: view =>
                    {
                        if (!hasResult)
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], "Result will appear here...");
                        }
                        else
                        {
                            view.Box(["bg-green-500/10 p-3 rounded"], content: view =>
                            {
                                view.Text([Text.Caption, "text-green-400 whitespace-pre-wrap break-all"], state.ResultJson.Value);
                            });
                        }
                    });
                });
            });
        });
    }

    private void RenderMultiStepAnalysisExample(UIView view)
    {
        var state = GetOrCreateState("taskgraph-analysis");

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Multi-Step Analysis");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "A simpler example with linear dependencies: gather data → analyze → synthesize → conclude.");

                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "Task dependency graph:");
                        view.Text([Text.Caption, "text-blue-300"],
                            "Task 1: Gather data points ──→ Task 2: Analyze patterns ──→ Task 3: Draw conclusions\n\n" +
                            "Each task must complete before the next can begin (linear dependency chain).");
                    });

                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-3"], "Configuration");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            view.Column([Layout.Column.Sm], content: view =>
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Analysis Topic");
                                view.TextField(
                                    [Input.Default, "w-full"],
                                    value: _taskGraphTopic.Value,
                                    placeholder: "Enter analysis topic...",
                                    onValueChange: async v => _taskGraphTopic.Value = v ?? "");
                            });

                            view.Row([Layout.Row.Lg], content: view =>
                            {
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Temperature: {_taskGraphTemperature.Value:F1}");
                                    RenderSlider(view, _taskGraphTemperature.Value, 0.0, 1.0, 0.1, v => _taskGraphTemperature.Value = v);
                                });
                            });
                        });
                    });

                    if (state.IsRunning.Value)
                    {
                        view.Box(["bg-yellow-500/10 border border-yellow-500/20 rounded-lg p-4 mb-4"], content: view =>
                        {
                            view.Row([Layout.Row.Sm, "items-center"], content: view =>
                            {
                                view.Box([Icon.Spinner, "mr-2 text-yellow-400"]);
                                view.Column([Layout.Column.Sm], content: view =>
                                {
                                    view.Text([Text.Caption, "text-yellow-400 font-semibold"], state.CurrentStage.Value);
                                    view.Text([Text.Caption, "text-yellow-300"],
                                        $"Tasks completed: {state.CurrentIteration.Value}/3");
                                });
                            });
                        });
                    }

                    view.Row([Layout.Row.Md], content: view =>
                    {
                        if (state.IsRunning.Value)
                        {
                            view.Button([Button.DangerMd], label: "Stop", onClick: async () =>
                            {
                                _cts?.Cancel();
                                state.IsRunning.Value = false;
                            });
                        }
                        else
                        {
                            view.Button([Button.PrimaryMd], label: "Run Analysis", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunMultiStepAnalysisExample(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear", onClick: async () => state.Clear());
                    });
                });
            });

            view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
            {
                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] overflow-hidden"], content: view =>
                {
                    view.Row(["justify-between items-center mb-2"], content: view =>
                    {
                        view.Text([Text.H3], "Event Stream");
                        if (state.Logs.Value.Count > 0)
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], $"{state.Logs.Value.Count} events");
                        }
                    });

                    RenderLogList(view, state);
                });

                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] overflow-hidden"], content: view =>
                {
                    view.Text([Text.H3, "mb-2"], "Analysis Result");
                    var hasResult = !string.IsNullOrEmpty(state.ResultJson.Value);
                    view.Box(["overflow-y-auto h-[calc(100%-2rem)] font-mono text-xs"], key: hasResult ? "has-result" : "no-result", content: view =>
                    {
                        if (!hasResult)
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], "Result will appear here...");
                        }
                        else
                        {
                            view.Box(["bg-green-500/10 p-3 rounded"], content: view =>
                            {
                                view.Text([Text.Caption, "text-green-400 whitespace-pre-wrap break-all"], state.ResultJson.Value);
                            });
                        }
                    });
                });
            });
        });
    }

    private async Task RunResearchReportExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Initializing task graph...";

        try
        {
            var ctx = CreateContext();
            var topic = _taskGraphTopic.Value;
            var parallel = _taskGraphParallel.Value;
            var temperature = _taskGraphTemperature.Value;
            var enableReview = _taskGraphEnableReview.Value;

            state.Log($"Starting TaskGraph for research report on: {topic}", LogLevel.Info);
            state.Log($"Parallel workers: {parallel}, Temperature: {temperature:F1}, Review: {enableReview}", LogLevel.Info);

            await foreach (var ev in Emerge.TaskGraph<FinalReport>(LLMModel.Claude45Sonnet, ctx, tg =>
            {
                tg.MaxParallel = parallel;
                tg.EnableParallelReview = enableReview;
                tg.ReviewIntervalTasks = 2;

                tg.AddTask("research-benefits", $"Research the key benefits and advantages of {topic}. Focus on practical applications and positive impacts.");
                tg.AddTask("research-challenges", $"Research the main challenges and problems related to {topic}. Identify obstacles and limitations.");
                tg.AddTask("research-future", $"Research future trends and predictions for {topic}. Consider technological evolution and emerging developments.");
                tg.AddTask("synthesize-benefits-challenges", "Synthesize the findings about benefits and challenges. Identify how benefits can address challenges.", "research-benefits", "research-challenges");
                tg.AddTask("synthesize-challenges-future", "Synthesize the challenges with future trends. How might future developments address current challenges?", "research-challenges", "research-future");
                tg.AddTask("final-report", "Write a comprehensive final report combining all synthesized sections.", "synthesize-benefits-challenges", "synthesize-challenges-future");

                tg.Worker(worker =>
                {
                    worker.Temperature = temperature;
                    worker.Command = $"""
                        You are a research assistant. Execute the assigned task thoroughly.
                        Topic context: {topic}

                        Return JSON:
                        {worker.JsonSchema}
                        """;
                });

                if (enableReview)
                {
                    tg.Reviewer(reviewer =>
                    {
                        reviewer.Temperature = 0.3;
                        reviewer.Command = $"""
                            Review the progress of the research tasks. Assess:
                            1. Are findings accurate and relevant to the topic?
                            2. Are there any gaps or inconsistencies?
                            3. Should the plan be revised?

                            Return JSON:
                            {reviewer.JsonSchema}
                            """;
                    });
                }

                tg.Synthesizer(synth =>
                {
                    synth.Temperature = temperature;
                    synth.Command = $"""
                        Create a comprehensive final report on: {topic}

                        Combine all research findings into a cohesive report with:
                        - An executive summary
                        - Multiple sections covering benefits, challenges, and future outlook
                        - A clear conclusion with recommendations

                        Return JSON:
                        {synth.JsonSchema}
                        """;
                });

                tg.OnTaskCompleted = (task, result) =>
                {
                    state.CurrentIteration.Value++;
                };
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<FinalReport> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                }

                if (ev is Completed<FinalReport> completed)
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

    private async Task RunMultiStepAnalysisExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Initializing analysis...";

        try
        {
            var ctx = CreateContext();
            var topic = _taskGraphTopic.Value;
            var temperature = _taskGraphTemperature.Value;

            state.Log($"Starting linear TaskGraph analysis on: {topic}", LogLevel.Info);

            await foreach (var ev in Emerge.TaskGraph<FinalReport>(LLMModel.Claude45Sonnet, ctx, tg =>
            {
                tg.MaxParallel = 1;
                tg.EnableParallelReview = false;

                tg.AddTask("gather-data", $"Gather key data points and facts about {topic}. List at least 5 important metrics or observations.");
                tg.AddTask("analyze-patterns", "Analyze the gathered data to identify patterns, trends, and correlations.", "gather-data");
                tg.AddTask("draw-conclusions", "Based on the analysis, draw conclusions and provide actionable recommendations.", "analyze-patterns");

                tg.Worker(worker =>
                {
                    worker.Temperature = temperature;
                    worker.Command = $"""
                        You are an analyst. Execute the assigned task based on previous results.
                        Topic: {topic}

                        Return JSON:
                        {worker.JsonSchema}
                        """;
                });

                tg.Synthesizer(synth =>
                {
                    synth.Temperature = temperature;
                    synth.Command = $"""
                        Create an analysis report on: {topic}

                        Summarize the data gathering, pattern analysis, and conclusions into a final report.

                        Return JSON:
                        {synth.JsonSchema}
                        """;
                });

                tg.OnTaskCompleted = (task, result) =>
                {
                    state.CurrentIteration.Value++;
                };
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<FinalReport> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                }

                if (ev is Completed<FinalReport> completed)
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
