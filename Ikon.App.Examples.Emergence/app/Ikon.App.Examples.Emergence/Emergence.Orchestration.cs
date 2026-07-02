using Ikon.AI.Emergence;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    public sealed class ItineraryPlan
    {
        public string Destination { get; set; } = "";
        public List<string> Days { get; set; } = [];
        public string Summary { get; set; } = "";
    }

    public sealed class RoutedAnswer
    {
        public string Approach { get; set; } = "";
        public string Answer { get; set; } = "";
    }

    public sealed class MergedIdeas
    {
        public List<string> Names { get; set; } = [];
        public string Rationale { get; set; } = "";
        public string TopPick { get; set; } = "";
    }

    public sealed class SwarmDeliverable
    {
        public string Title { get; set; } = "";
        public string Brief { get; set; } = "";
        public List<string> KeyPoints { get; set; } = [];
    }

    private readonly Reactive<string> _selectedOrchExample = new("plan");

    private readonly Reactive<string> _planDestination = new("Kyoto, Japan for 3 days");
    private readonly Reactive<int> _planMaxSteps = new(6);

    private readonly Reactive<string> _routerRequest = new("Write a recursive C# function that returns the nth Fibonacci number.");

    private readonly Reactive<string> _ensembleTopic = new("a subscription app that turns your grocery receipts into recipes");
    private readonly Reactive<int> _ensembleSolvers = new(3);

    private readonly Reactive<string> _swarmTopic = new("the case for four-day work weeks at a mid-size software company");
    private readonly Reactive<int> _swarmRounds = new(1);

    private void RenderOrchestrationSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Orchestration Patterns");
                view.Text([Text.Body, "text-muted-foreground"],
                    "Multi-agent orchestration: plan-then-execute, dynamic routing to the best approach, ensemble merging of " +
                    "diverse solvers, and role-based swarms with a coordinator.");
            });

            RenderExampleSelector(view, _selectedOrchExample,
                ("plan", "PlanAndExecute"),
                ("router", "Router"),
                ("ensemble", "EnsembleMerge"),
                ("swarm", "Swarm"));

            switch (_selectedOrchExample.Value)
            {
                case "plan":
                    RenderPlanAndExecuteExample(view);
                    break;
                case "router":
                    RenderRouterExample(view);
                    break;
                case "ensemble":
                    RenderEnsembleMergeExample(view);
                    break;
                case "swarm":
                    RenderSwarmExample(view);
                    break;
            }
        });
    }

    private void RenderPlanAndExecuteExample(UIView view)
    {
        RenderPatternExample(
            view,
            exampleId: "orch-plan",
            title: "PlanAndExecute — Strategic Execution",
            description: "First a planner breaks the goal into steps, then an executor works through them to produce the final result.",
            howItWorks:
                $"1. Planner: produces an ExecutionPlan (a list of steps) for the goal.\n" +
                $"2. Executor: works through the steps (up to {_planMaxSteps.Value}) and assembles the result.\n" +
                "Watch the Stage events switch from planning to execution.",
            runLabel: "Plan & Execute",
            resultLabel: "Itinerary",
            config: cfg =>
            {
                cfg.Column([Layout.Column.Sm], content: c =>
                {
                    c.Text([Text.Caption, "text-muted-foreground"], "Trip Goal");
                    c.TextField(
                        [Input.Default, "w-full"],
                        value: _planDestination.Value,
                        placeholder: "Where to and for how long?",
                        onValueChange: async v => _planDestination.Value = v ?? "");
                });
                cfg.Row([Layout.Row.Lg], content: r =>
                {
                    RenderLabeledSlider(r, $"Max Steps: {_planMaxSteps.Value}", _planMaxSteps.Value, 2, 10, 1, v => _planMaxSteps.Value = (int)v);
                });
            },
            runAction: RunPlanAndExecuteExample);
    }

    private void RenderRouterExample(UIView view)
    {
        RenderPatternExample(
            view,
            exampleId: "orch-router",
            title: "Router — Dynamic Routing",
            description: "A router picks the most appropriate approach for the request, then that approach handles it.",
            howItWorks:
                "1. Router: classifies the request into one of the registered routes (code / creative / analysis).\n" +
                "2. The selected route's configuration handles the request.\n" +
                "The chosen approach is reported in the result and the event log.",
            runLabel: "Route & Answer",
            resultLabel: "Answer",
            config: cfg =>
            {
                cfg.Column([Layout.Column.Sm], content: c =>
                {
                    c.Text([Text.Caption, "text-muted-foreground"], "Request");
                    c.TextArea(
                        [Textarea.Default, "w-full h-16 font-mono text-xs"],
                        value: _routerRequest.Value,
                        placeholder: "Ask for code, a creative piece, or an analysis...",
                        onValueChange: async v => _routerRequest.Value = v ?? "");
                });
            },
            runAction: RunRouterExample);
    }

    private void RenderEnsembleMergeExample(UIView view)
    {
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
    }

    private void RenderSwarmExample(UIView view)
    {
        RenderPatternExample(
            view,
            exampleId: "orch-swarm",
            title: "Swarm — Role-Based Orchestration",
            description: "Multiple agents with distinct roles work across rounds, then a coordinator synthesizes the final deliverable.",
            howItWorks:
                $"1. Agents: researcher, analyst, and writer each contribute their part ({_swarmRounds.Value} round(s)).\n" +
                "2. Coordinator: merges all agent outputs into the final brief.\n" +
                "All agents run every round with isolated contexts; the coordinator sees them all.",
            runLabel: "Run Swarm",
            resultLabel: "Deliverable",
            config: cfg =>
            {
                cfg.Column([Layout.Column.Sm], content: c =>
                {
                    c.Text([Text.Caption, "text-muted-foreground"], "Topic");
                    c.TextArea(
                        [Textarea.Default, "w-full h-16 font-mono text-xs"],
                        value: _swarmTopic.Value,
                        placeholder: "What should the swarm produce a brief on?",
                        onValueChange: async v => _swarmTopic.Value = v ?? "");
                });
                cfg.Row([Layout.Row.Lg], content: r =>
                {
                    RenderLabeledSlider(r, $"Rounds: {_swarmRounds.Value}", _swarmRounds.Value, 1, 3, 1, v => _swarmRounds.Value = (int)v);
                });
            },
            runAction: RunSwarmExample);
    }

    private async Task RunPlanAndExecuteExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Planning...";

        try
        {
            var ctx = CreateContext();
            var goal = _planDestination.Value;
            var maxSteps = _planMaxSteps.Value;

            state.Log($"Starting PlanAndExecute: goal=\"{goal}\", maxSteps={maxSteps}", LogLevel.Info);

            await foreach (var ev in Emerge.PlanAndExecute<ItineraryPlan>(LLMModel.Claude45Sonnet, ctx, opt =>
            {
                opt.MaxSteps = maxSteps;

                opt.Planner(p =>
                {
                    p.Command = $"""
                        Create a concise step-by-step plan to build a great travel itinerary for: {goal}
                        Each step should be one clear action (e.g. pick neighborhoods, choose sights, plan meals).
                        """;
                });

                opt.Executor(e =>
                {
                    e.Command = $"""
                        Execute the plan and produce a day-by-day itinerary for: {goal}
                        Return JSON:
                        {e.JsonSchema}
                        """;
                });
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<ItineraryPlan> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                    state.CurrentIteration.Value++;
                }

                if (ev is Completed<ItineraryPlan> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = $"Complete — {completed.Result.Days.Count} day(s)";
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

    private async Task RunRouterExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Routing...";

        try
        {
            var ctx = CreateContext();
            var request = _routerRequest.Value;

            state.Log($"Starting Router for request: \"{TruncateJson(request, 60)}\"", LogLevel.Info);

            await foreach (var ev in Emerge.Router<RoutedAnswer>(LLMModel.Claude45Sonnet, ctx, opt =>
            {
                opt.AddRoute("code", "Programming, algorithms, and technical implementation questions");
                opt.AddRoute("creative", "Creative writing, brainstorming, taglines, and storytelling");
                opt.AddRoute("analysis", "Data analysis, comparisons, reasoning, and structured evaluation");

                opt.Router(r =>
                {
                    r.Temperature = 0.2;
                    r.Command = "Analyze the user's request and select the single most appropriate route.";
                });

                opt.Command = $"""
                    Handle the user's request using the selected approach. In the Approach field, name the route you took.

                    Request: {request}

                    Return JSON:
                    {opt.JsonSchema}
                    """;
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<RoutedAnswer> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                    state.Log($"Stage: {stage.Name}", LogLevel.Stage);
                }

                if (ev is Completed<RoutedAnswer> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = $"Complete — via {completed.Result.Approach}";
                    state.Log($"Routed via: {completed.Result.Approach}", LogLevel.Result);
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

                if (ev is Completed<MergedIdeas> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = $"Complete — top pick: {completed.Result.TopPick}";
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

    private async Task RunSwarmExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Assembling swarm...";

        try
        {
            var ctx = CreateContext();
            var topic = _swarmTopic.Value;
            var rounds = _swarmRounds.Value;

            state.Log($"Starting Swarm on: \"{topic}\" ({rounds} round(s))", LogLevel.Info);

            await foreach (var ev in Emerge.Swarm<SwarmDeliverable>(LLMModel.Claude45Sonnet, ctx, opt =>
            {
                opt.MaxRounds = rounds;
                opt.MaxParallel = 3;

                opt.AddAgent("researcher", s =>
                {
                    s.Command = $"Research and gather the strongest facts and examples about: {topic}";
                });

                opt.AddAgent("analyst", s =>
                {
                    s.Command = $"Analyze the trade-offs and identify the most compelling points about: {topic}";
                });

                opt.AddAgent("writer", s =>
                {
                    s.Command = $"Draft crisp, persuasive prose about: {topic}";
                });

                opt.Coordinator(c =>
                {
                    c.Command = $"""
                        Synthesize the researcher, analyst, and writer outputs into a single tight brief on: {topic}
                        Return JSON:
                        {c.JsonSchema}
                        """;
                });
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<SwarmDeliverable> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                }

                if (ev is Completed<SwarmDeliverable> completed)
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
