using System.Text.Json;
using Ikon.AI.Emergence;
using Ikon.Common.Core.Functions;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    // Response types
    public sealed class SimpleResponse
    {
        public string Answer { get; set; } = "";
        public string Reasoning { get; set; } = "";
    }

    public sealed class ResearchResponse
    {
        public string Topic { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<string> KeyFindings { get; set; } = [];
    }

    // Run<T> tab configuration
    private readonly Reactive<string> _researchTopic = new("quantum computing in medicine");
    private readonly Reactive<double> _temperature = new(0.7);
    private readonly Reactive<int> _maxIterations = new(5);

    // Tool implementations
    private static object SearchWeb(string query)
    {
        return new
        {
            results = new[]
            {
                new { title = $"Research on {query}", content = $"Recent studies show {query} is advancing rapidly with applications in multiple domains." },
                new { title = $"{query} Overview", content = $"Experts predict {query} will transform industries within the next decade." },
                new { title = $"Practical {query}", content = $"Current implementations of {query} demonstrate promising results in real-world scenarios." }
            }
        };
    }

    private static object GetStatistics(string topic)
    {
        return new
        {
            topic,
            stats = new[]
            {
                $"Market growth: 25% annually",
                $"Active research papers: 15,000+",
                $"Industry adoption: 45% of Fortune 500"
            }
        };
    }

    private void RenderRunSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Tab header
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Run<T> Pattern");
                view.Text([Text.Body, "text-muted-foreground"],
                    "The basic emergence pattern. Runs a single LLM pass to generate structured output. Can include tools for multi-step reasoning.");
            });

            // Example selector
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H3, "mb-3"], "Choose Example");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    var examples = new[] { ("simple", "Simple Query"), ("research", "Research with Tools") };
                    foreach (var (key, label) in examples)
                    {
                        var isSelected = _selectedRunExample.Value == key;
                        view.Button(
                            [isSelected ? Button.PrimaryMd : Button.OutlineMd],
                            label: label,
                            onClick: async () => _selectedRunExample.Value = key);
                    }
                });
            });

            // Render selected example
            switch (_selectedRunExample.Value)
            {
                case "simple":
                    RenderSimpleExample(view);
                    break;
                case "research":
                    RenderResearchExample(view);
                    break;
            }
        });
    }

    private void RenderSimpleExample(UIView view)
    {
        RenderExamplePanel(
            view,
            exampleId: "run-simple",
            title: "Simple Query",
            description: "Generate a structured response without any tools.",
            whatItDoes: "Makes a single LLM call to generate JSON output. Completes in 1 iteration since no tools are involved. Good for simple transformations and generations.",
            runAction: RunSimpleExample);
    }

    private void RenderResearchExample(UIView view)
    {
        var state = GetOrCreateState("run-research");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Research with Tools");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "Use tools to gather information before generating a response.");

                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "What this example demonstrates:");
                        view.Text([Text.Caption, "text-blue-300"],
                            "The model calls search_web and get_statistics tools to gather information, then synthesizes findings into structured JSON. Expect 2-3 iterations: tool calls, then final response.");
                    });

                    // Configuration controls
                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-3"], "Configuration");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            // Topic input
                            view.Column([Layout.Column.Sm], content: view =>
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Research Topic");
                                view.TextField(
                                    [Input.Default, "w-full"],
                                    value: _researchTopic.Value,
                                    placeholder: "Enter a topic to research...",
                                    onValueChange: async v => _researchTopic.Value = v ?? "");
                            });

                            view.Row([Layout.Row.Lg], content: view =>
                            {
                                // Temperature slider
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Temperature: {_temperature.Value:F1}");
                                    RenderSlider(view, _temperature.Value, 0.0, 1.0, 0.1, v => _temperature.Value = v);
                                });

                                // Max iterations
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Max Iterations: {_maxIterations.Value}");
                                    RenderSlider(view, _maxIterations.Value, 1, 10, 1, v => _maxIterations.Value = (int)v);
                                });
                            });
                        });
                    });

                    // Progress indicator
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
                                        $"Iteration {state.CurrentIteration.Value} | Tool Calls: {state.ToolCallCount.Value}");
                                });
                            });
                        });
                    }

                    // Controls
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
                            view.Button([Button.PrimaryMd], label: "Run Research", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunResearchExample(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear", onClick: async () => state.Clear());
                    });
                });
            });

            // Results panels
            view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
            {
                // Event log
                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] flex flex-col"], content: view =>
                {
                    view.Row(["justify-between items-center mb-2 shrink-0"], content: view =>
                    {
                        view.Text([Text.H3], "Event Stream");
                        if (state.Logs.Value.Count > 0)
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], $"{state.Logs.Value.Count} events");
                        }
                    });

                    RenderLogList(view, state);
                });

                // Result
                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] flex flex-col"], content: view =>
                {
                    view.Text([Text.H3, "mb-2 shrink-0"], "Result");
                    view.Box(["overflow-y-auto flex-1 font-mono text-xs"], content: view =>
                    {
                        if (string.IsNullOrEmpty(state.ResultJson.Value))
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

    private void RenderLogList(UIView view, ExampleState state)
    {
        view.Box(["overflow-y-auto flex-1 font-mono text-xs"], content: view =>
        {
            if (state.Logs.Value.Count == 0)
            {
                view.Text([Text.Caption, "text-muted-foreground"], "Click 'Run' to start...");
            }
            else
            {
                foreach (var log in state.Logs.Value.TakeLast(100))
                {
                    var (bgClass, textClass) = log.Level switch
                    {
                        LogLevel.Event => ("bg-blue-500/10", "text-blue-400"),
                        LogLevel.Tool => ("bg-purple-500/10", "text-purple-400"),
                        LogLevel.Result => ("bg-green-500/10", "text-green-400"),
                        LogLevel.Error => ("bg-red-500/10", "text-red-400"),
                        LogLevel.Stage => ("bg-yellow-500/10", "text-yellow-400"),
                        LogLevel.Iteration => ("bg-cyan-500/10", "text-cyan-400"),
                        _ => ("", "text-foreground")
                    };

                    view.Box([$"py-1 px-2 mb-1 rounded {bgClass}"], content: view =>
                    {
                        view.Row([Layout.Row.Sm], content: view =>
                        {
                            view.Text([Text.Caption, "text-muted-foreground w-20 shrink-0"], log.Timestamp.ToString("HH:mm:ss.fff"));
                            view.Text([Text.Caption, $"w-20 shrink-0 font-semibold {textClass}"], $"[{log.Level}]");
                            view.Text([Text.Caption, "break-all"], log.Message);
                        });
                    });
                }
            }
        });
    }

    private async Task RunSimpleExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Generating response...";

        try
        {
            var ctx = CreateContext();
            state.Log("Starting simple Run<T> - no tools, single iteration expected", LogLevel.Info);

            await foreach (var ev in Emerge.Run<SimpleResponse>(LLMModel.Claude45Sonnet, ctx, pass =>
            {
                pass.Command = $"""
                    What are the three most important principles of software engineering?
                    Provide a concise answer.

                    Return JSON:
                    {pass.JsonSchema}
                    """;
                pass.Temperature = 0.7;
                pass.MaxIterations = 3;

                state.CurrentIteration.Value = pass.Iteration;
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Completed<SimpleResponse> completed)
                {
                    state.Log($"Completed event received, Result is null: {completed.Result == null}", LogLevel.Info);
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

    private async Task RunResearchExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Initializing...";

        try
        {
            var ctx = CreateContext();
            var topic = _researchTopic.Value;

            state.Log($"Starting research on: {topic}", LogLevel.Info);
            state.Log($"Temperature: {_temperature.Value:F1}, Max Iterations: {_maxIterations.Value}", LogLevel.Info);

            await foreach (var ev in Emerge.Run<ResearchResponse>(LLMModel.Claude45Sonnet, ctx, pass =>
            {
                pass.AddTool("search_web", "Search the web for information on a topic", (string query) => SearchWeb(query))
                    .AddTool("get_statistics", "Get statistics and data about a topic", (string t) => GetStatistics(t));

                pass.Command = $"""
                    Research the topic: "{topic}"

                    You MUST use the available tools to gather information:
                    1. Use search_web to find relevant information
                    2. Use get_statistics to get data points

                    After gathering information, compile a comprehensive summary.

                    Return JSON:
                    {pass.JsonSchema}
                    """;
                pass.Temperature = _temperature.Value;
                pass.MaxIterations = _maxIterations.Value;
                pass.MaxToolCalls = 10;

                state.CurrentIteration.Value = pass.Iteration;
                state.CurrentStage.Value = pass.Iteration == 0 ? "Making initial request..." : $"Iteration {pass.Iteration}";

                if (pass.Iteration > 0)
                {
                    state.Log($"Iteration {pass.Iteration} - processing tool results", LogLevel.Iteration);
                }
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Completed<ResearchResponse> completed)
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
