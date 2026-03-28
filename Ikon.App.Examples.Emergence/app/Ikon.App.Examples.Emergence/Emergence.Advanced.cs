using System.Text.Json;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    // Advanced pattern types
    public sealed class PuzzleSolution
    {
        public string Reasoning { get; set; } = "";
        public string Answer { get; set; } = "";
        public int Confidence { get; set; }
    }

    public sealed class CodeSolution
    {
        public string Code { get; set; } = "";
        public string Explanation { get; set; } = "";
        public List<string> TestCases { get; set; } = [];
    }

    public sealed class DebatePosition
    {
        public string Position { get; set; } = "";
        public List<string> Arguments { get; set; } = [];
        public List<string> Evidence { get; set; } = [];
        public string Conclusion { get; set; } = "";
    }

    public sealed class JudgeVerdict
    {
        public string WinningPosition { get; set; } = "";
        public string Reasoning { get; set; } = "";
        public List<string> StrongestArguments { get; set; } = [];
        public string FinalVerdict { get; set; } = "";
    }

    // Advanced pattern configuration
    private readonly Reactive<string> _puzzleText = new("""
Three friends (Alice, Bob, Carol) each have a different pet (cat, dog, fish) and live in different colored houses (red, blue, green).
- Alice doesn't live in the red house.
- The person with the cat lives in the blue house.
- Bob has the dog.
- Carol lives in the green house.
Who lives where and has which pet?
""");

    private readonly Reactive<string> _codingTask = new("Write a Python function that checks if a string is a valid palindrome, ignoring spaces and punctuation.");
    private readonly Reactive<string> _debateTopic = new("Remote work should become the default for knowledge workers");

    private readonly Reactive<int> _treeDepth = new(3);
    private readonly Reactive<int> _treeBranching = new(2);
    private readonly Reactive<int> _treeBeamWidth = new(2);

    private readonly Reactive<int> _scvMaxRounds = new(2);
    private readonly Reactive<double> _solverTemperature = new(0.7);
    private readonly Reactive<double> _criticTemperature = new(0.3);

    private readonly Reactive<int> _debaterCount = new(2);
    private readonly Reactive<double> _debateTemperature = new(0.8);
    private readonly Reactive<double> _judgeTemperature = new(0.3);

    private void RenderAdvancedSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Pattern description
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Advanced Patterns");
                view.Text([Text.Body, "text-muted-foreground"],
                    "Multi-agent patterns for complex reasoning tasks: Tree of Thought for exploration, Solver-Critic-Verifier for refinement, and Debate Then Judge for diverse perspectives.");
            });

            // Example selector
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H3, "mb-3"], "Choose Pattern");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    var examples = new[]
                    {
                        ("solver-critic", "Solver-Critic-Verifier"),
                        ("debate", "Debate Then Judge"),
                        ("tree-of-thought", "Tree of Thought")
                    };
                    foreach (var (key, label) in examples)
                    {
                        var isSelected = _selectedAdvancedExample.Value == key;
                        view.Button(
                            [isSelected ? Button.PrimaryMd : Button.OutlineMd],
                            label: label,
                            onClick: async () => _selectedAdvancedExample.Value = key);
                    }
                });
            });

            // Render selected example
            switch (_selectedAdvancedExample.Value)
            {
                case "solver-critic":
                    RenderSolverCriticExample(view);
                    break;
                case "debate":
                    RenderDebateExample(view);
                    break;
                case "tree-of-thought":
                    RenderTreeOfThoughtExample(view);
                    break;
            }
        });
    }

    private void RenderSolverCriticExample(UIView view)
    {
        var state = GetOrCreateState("advanced-scv");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Solver-Critic-Verifier");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "A three-role refinement loop: Solver creates initial solution, Critic reviews it, Verifier improves based on criticism.");

                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "How Solver-Critic-Verifier works:");
                        view.Text([Text.Caption, "text-blue-300"],
                            $"1. Solver (temp={_solverTemperature.Value:F1}): Creates initial solution creatively\n" +
                            $"2. Critic (temp={_criticTemperature.Value:F1}): Reviews and identifies issues\n" +
                            $"3. Verifier (temp=0.2): Fixes issues based on criticism\n" +
                            $"Runs up to {_scvMaxRounds.Value} rounds. You'll see Stage events for each role.");
                    });

                    // Configuration
                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-3"], "Configuration");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            // Task input
                            view.Column([Layout.Column.Sm], content: view =>
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Coding Task");
                                view.TextArea(
                                    ["w-full h-20 font-mono text-xs"],
                                    value: _codingTask.Value,
                                    placeholder: "Describe the coding task...",
                                    onValueChange: async v => _codingTask.Value = v ?? "");
                            });

                            view.Row([Layout.Row.Lg], content: view =>
                            {
                                // Max rounds
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Max Rounds: {_scvMaxRounds.Value}");
                                    RenderSlider(view, _scvMaxRounds.Value, 1, 4, 1, v => _scvMaxRounds.Value = (int)v);
                                });

                                // Solver temperature
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Solver Temp: {_solverTemperature.Value:F1}");
                                    RenderSlider(view, _solverTemperature.Value, 0.0, 1.0, 0.1, v => _solverTemperature.Value = v);
                                });

                                // Critic temperature
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Critic Temp: {_criticTemperature.Value:F1}");
                                    RenderSlider(view, _criticTemperature.Value, 0.0, 1.0, 0.1, v => _criticTemperature.Value = v);
                                });
                            });
                        });
                    });

                    // Progress
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
                                        $"Round {state.CurrentIteration.Value + 1} of {_scvMaxRounds.Value}");
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
                            view.Button([Button.PrimaryMd], label: "Solve & Refine", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunSolverCriticVerifierExample(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear", onClick: async () => state.Clear());
                    });
                });
            });

            // Results
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
                    view.Text([Text.H3, "mb-2"], "Final Solution");
                    view.Box(["overflow-y-auto h-[calc(100%-2rem)] font-mono text-xs"], content: view =>
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

    private void RenderDebateExample(UIView view)
    {
        var state = GetOrCreateState("advanced-debate");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Debate Then Judge");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "Multiple debaters argue different positions, then a judge synthesizes the best arguments.");

                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "How Debate Then Judge works:");
                        view.Text([Text.Caption, "text-blue-300"],
                            $"1. {_debaterCount.Value} Debaters (temp={_debateTemperature.Value:F1}): Each argues a different position\n" +
                            $"2. Judge (temp={_judgeTemperature.Value:F1}): Evaluates arguments and synthesizes conclusion\n" +
                            $"Debaters are assigned opposing stances automatically.\n" +
                            $"You'll see Stage events for each debater and the judge.");
                    });

                    // Configuration
                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-3"], "Configuration");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            // Topic input
                            view.Column([Layout.Column.Sm], content: view =>
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Debate Topic");
                                view.TextField(
                                    [Input.Default, "w-full"],
                                    value: _debateTopic.Value,
                                    placeholder: "Enter a debate topic...",
                                    onValueChange: async v => _debateTopic.Value = v ?? "");
                            });

                            view.Row([Layout.Row.Lg], content: view =>
                            {
                                // Debater count
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Debaters: {_debaterCount.Value}");
                                    RenderSlider(view, _debaterCount.Value, 2, 4, 1, v => _debaterCount.Value = (int)v);
                                });

                                // Debate temperature
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Debate Temp: {_debateTemperature.Value:F1}");
                                    RenderSlider(view, _debateTemperature.Value, 0.0, 1.0, 0.1, v => _debateTemperature.Value = v);
                                });

                                // Judge temperature
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Judge Temp: {_judgeTemperature.Value:F1}");
                                    RenderSlider(view, _judgeTemperature.Value, 0.0, 1.0, 0.1, v => _judgeTemperature.Value = v);
                                });
                            });
                        });
                    });

                    // Progress
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
                                    var progressText = state.CurrentStage.Value.Contains("Judge")
                                        ? "Evaluating all positions..."
                                        : $"Heard {state.CurrentIteration.Value} of {_debaterCount.Value} debaters";
                                    view.Text([Text.Caption, "text-yellow-300"], progressText);
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
                            view.Button([Button.PrimaryMd], label: "Start Debate", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunDebateExample(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear", onClick: async () => state.Clear());
                    });
                });
            });

            // Results
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
                    view.Text([Text.H3, "mb-2"], "Judge's Verdict");
                    view.Box(["overflow-y-auto h-[calc(100%-2rem)] font-mono text-xs"], content: view =>
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

    private void RenderTreeOfThoughtExample(UIView view)
    {
        var state = GetOrCreateState("advanced-tot");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Tree of Thought");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "Explores multiple reasoning paths using beam search, evaluating and pruning branches to find the best solution.");

                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "How Tree of Thought works:");
                        view.Text([Text.Caption, "text-blue-300"],
                            $"1. Depth {_treeDepth.Value}: Explores {_treeDepth.Value} levels of reasoning\n" +
                            $"2. Branching {_treeBranching.Value}: Generates {_treeBranching.Value} alternatives at each step\n" +
                            $"3. Beam Width {_treeBeamWidth.Value}: Keeps best {_treeBeamWidth.Value} paths per level\n" +
                            $"Total nodes explored: up to {Math.Pow(_treeBranching.Value, _treeDepth.Value) * _treeBeamWidth.Value:F0}");
                    });

                    // Configuration
                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-3"], "Configuration");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            // Puzzle input
                            view.Column([Layout.Column.Sm], content: view =>
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Logic Puzzle");
                                view.TextArea(
                                    ["w-full h-24 font-mono text-xs"],
                                    value: _puzzleText.Value,
                                    placeholder: "Enter a logic puzzle...",
                                    onValueChange: async v => _puzzleText.Value = v ?? "");
                            });

                            view.Row([Layout.Row.Lg], content: view =>
                            {
                                // Max depth
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Max Depth: {_treeDepth.Value}");
                                    RenderSlider(view, _treeDepth.Value, 2, 5, 1, v => _treeDepth.Value = (int)v);
                                });

                                // Branching factor
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Branching: {_treeBranching.Value}");
                                    RenderSlider(view, _treeBranching.Value, 2, 4, 1, v => _treeBranching.Value = (int)v);
                                });

                                // Beam width
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Beam Width: {_treeBeamWidth.Value}");
                                    RenderSlider(view, _treeBeamWidth.Value, 1, 4, 1, v => _treeBeamWidth.Value = (int)v);
                                });
                            });
                        });
                    });

                    // Progress
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
                                        $"Depth {state.CurrentIteration.Value} | Nodes evaluated: {state.ToolCallCount.Value}");
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
                            view.Button([Button.PrimaryMd], label: "Explore Solutions", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunTreeOfThoughtExample(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear", onClick: async () => state.Clear());
                    });
                });
            });

            // Results
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
                    view.Text([Text.H3, "mb-2"], "Best Solution");
                    view.Box(["overflow-y-auto h-[calc(100%-2rem)] font-mono text-xs"], content: view =>
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

    private async Task RunTreeOfThoughtExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Initializing search tree...";

        try
        {
            var ctx = CreateContext();
            var puzzle = _puzzleText.Value;
            var depth = _treeDepth.Value;
            var branching = _treeBranching.Value;
            var beamWidth = _treeBeamWidth.Value;

            state.Log($"Starting TreeOfThought: depth={depth}, branching={branching}, beam={beamWidth}", LogLevel.Info);
            state.Log($"Puzzle: {TruncateJson(puzzle, 60)}", LogLevel.Info);

            await foreach (var ev in Emerge.TreeOfThought<PuzzleSolution>(LLMModel.Claude45Sonnet, ctx, tot =>
            {
                tot.MaxDepth = depth;
                tot.BranchingFactor = branching;
                tot.BeamWidth = beamWidth;

                tot.Thought(thought =>
                {
                    thought.Temperature = 0.8;
                    thought.Command = $"""
                        Solve this logic puzzle step by step:

                        {puzzle}

                        Think through one step of reasoning. Build on any previous thoughts provided.
                        Return JSON:
                        {thought.JsonSchema}
                        """;
                });

                tot.Evaluate = (solution, trace) =>
                {
                    var score = 0.0;

                    if (solution.Reasoning.Contains("therefore", StringComparison.OrdinalIgnoreCase) ||
                        solution.Reasoning.Contains("because", StringComparison.OrdinalIgnoreCase) ||
                        solution.Reasoning.Contains("since", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.3;
                    }

                    score += solution.Confidence / 100.0 * 0.3;

                    if (solution.Answer.Length > 20)
                    {
                        score += 0.2;
                    }

                    if (solution.Reasoning.Length < 50)
                    {
                        score -= 0.2;
                    }

                    state.ToolCallCount.Value++;
                    state.Log($"Evaluated: score={Math.Max(0, score):F2}, confidence={solution.Confidence}", LogLevel.Result);

                    return Math.Max(0, score);
                };
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<PuzzleSolution> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                    if (stage.Name.Contains("Depth"))
                    {
                        state.CurrentIteration.Value++;
                    }
                }

                if (ev is Completed<PuzzleSolution> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = $"Complete - Confidence: {completed.Result.Confidence}%";
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

    private async Task RunSolverCriticVerifierExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Starting solver...";

        try
        {
            var ctx = CreateContext();
            var task = _codingTask.Value;
            var maxRounds = _scvMaxRounds.Value;
            var solverTemp = _solverTemperature.Value;
            var criticTemp = _criticTemperature.Value;

            state.Log($"Starting SolverCriticVerifier: maxRounds={maxRounds}", LogLevel.Info);
            state.Log($"Task: {TruncateJson(task, 60)}", LogLevel.Info);

            await foreach (var ev in Emerge.SolverCriticVerifier<CodeSolution>(LLMModel.Claude45Sonnet, ctx, scv =>
            {
                scv.MaxRounds = maxRounds;

                scv.Solver(solver =>
                {
                    solver.Temperature = solverTemp;
                    solver.Command = $"""
                        {task}
                        Include test cases.
                        Return JSON:
                        {solver.JsonSchema}
                        """;
                });

                scv.Critic(critic =>
                {
                    critic.Temperature = criticTemp;
                    critic.Command = """
                        Review this code solution critically. Look for:
                        - Edge cases not handled
                        - Potential bugs
                        - Missing test cases
                        - Performance issues
                        List all issues found.
                        """;
                });

                scv.Verifier(verifier =>
                {
                    verifier.Temperature = 0.2;
                    verifier.Command = $"""
                        Based on the criticism, improve the solution.
                        Fix all identified issues and add any missing test cases.
                        Return the improved JSON:
                        {verifier.JsonSchema}
                        """;
                });
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<CodeSolution> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                    if (stage.Name.Contains("Round"))
                    {
                        state.CurrentIteration.Value++;
                    }
                }

                if (ev is Completed<CodeSolution> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = $"Complete - {completed.Result.TestCases.Count} test cases";
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

    private async Task RunDebateExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Starting debate...";

        try
        {
            var ctx = CreateContext();
            var topic = _debateTopic.Value;
            var debaterCount = _debaterCount.Value;
            var debateTemp = _debateTemperature.Value;
            var judgeTemp = _judgeTemperature.Value;

            var stances = new[] { "strongly in favor of", "against", "cautiously supportive of", "skeptical about" };

            state.Log($"Starting DebateThenJudge with {debaterCount} debaters", LogLevel.Info);
            state.Log($"Topic: \"{topic}\"", LogLevel.Info);
            for (int i = 0; i < debaterCount; i++)
            {
                state.Log($"  Debater {i}: {stances[i % stances.Length]}", LogLevel.Info);
            }

            await foreach (var ev in Emerge.DebateThenJudge<JudgeVerdict>(LLMModel.Claude45Sonnet, ctx, debate =>
            {
                debate.Debaters = debaterCount;

                debate.Debater(d =>
                {
                    d.Temperature = debateTemp;
                    var stance = stances[d.Index % stances.Length];
                    d.Command = $"You are Debater {d.Index}, arguing {stance}: \"{topic}\"\n\n" +
                        "Present your strongest arguments with evidence.\n" +
                        "Be persuasive and cite specific examples or data where possible.\n\n" +
                        "Return JSON with Position, Arguments (array), Evidence (array), and Conclusion fields.";
                });

                debate.Judge(judge =>
                {
                    judge.Temperature = judgeTemp;
                    judge.Command = $"""
                        You are a neutral judge evaluating a debate on: "{topic}"

                        Review all positions and:
                        1. Identify which position presented the strongest overall case
                        2. List the most compelling arguments from the debate
                        3. Provide your reasoned verdict

                        Return JSON:
                        {judge.JsonSchema}
                        """;
                });
            }).WithCancellation(_cts!.Token))
            {
                LogDebateEvent(state, ev, stances);

                if (ev is Stage<JudgeVerdict> stage)
                {
                    state.CurrentStage.Value = FormatStageName(stage.Name, stances);

                    if (stage.Name.StartsWith("Debater:"))
                    {
                        var parts = stage.Name.Split(':');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out var debaterIndex))
                        {
                            state.CurrentIteration.Value = debaterIndex + 1;
                        }
                    }
                }

                if (ev is Completed<JudgeVerdict> completed)
                {
                    state.SetResult(completed.Result);
                    var winner = !string.IsNullOrEmpty(completed.Result.WinningPosition)
                        ? $"Winner: {completed.Result.WinningPosition}"
                        : "Verdict reached";
                    state.CurrentStage.Value = $"Complete - {winner}";
                    state.Log($"Judge's verdict: {completed.Result.WinningPosition}", LogLevel.Result);
                    state.Log($"Reasoning: {TruncateJson(completed.Result.Reasoning, 120)}", LogLevel.Result);
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

    private static string FormatStageName(string stageName, string[] stances)
    {
        if (stageName.StartsWith("Debater:"))
        {
            var parts = stageName.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var debaterIndex))
            {
                var stance = stances[debaterIndex % stances.Length];
                return $"Debater {debaterIndex} ({stance})";
            }
        }

        if (stageName == "Judge")
        {
            return "Judge evaluating positions...";
        }

        return stageName;
    }

    private void LogDebateEvent<T>(ExampleState state, EmergeEvent<T> ev, string[] stances)
    {
        switch (ev)
        {
            case Stage<T> stage:
                if (stage.Name.StartsWith("Debater:"))
                {
                    var parts = stage.Name.Split(':');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var debaterIndex))
                    {
                        var stance = stances[debaterIndex % stances.Length];
                        state.Log($"Debater {debaterIndex} ({stance}) presenting arguments...", LogLevel.Stage);
                    }
                }
                else if (stage.Name == "Judge")
                {
                    state.Log("Judge is now evaluating all positions...", LogLevel.Stage);
                }
                else if (stage.Name.StartsWith("DebateRound:"))
                {
                    var round = stage.Name.Split(':')[1];
                    state.Log($"Starting debate round {round}", LogLevel.Stage);
                }
                else
                {
                    state.Log($"Stage: {stage.Name}", LogLevel.Stage);
                }

                break;
            case Ikon.AI.Emergence.Progress<T> progress:
                state.Log(progress.Message, LogLevel.Info);
                break;
            case Completed<T> completed:
                state.Log($"Completed! Iterations: {completed.Trace.Iterations}", LogLevel.Result);
                break;
            case Stopped<T> stopped:
                state.Log($"Stopped: {stopped.Reason}", LogLevel.Error);
                break;
        }
    }
}
