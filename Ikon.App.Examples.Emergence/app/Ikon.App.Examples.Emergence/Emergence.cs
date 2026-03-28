using System.Text.Json;
using Ikon.Common.Core.Functions;
using Ikon.Parallax.Components.Standard;

return await App.Run(args);

public record SessionIdentity(string UserId);
public record ClientParams;

[App]
public partial class Emergence(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new Theme());

    // Tab state
    private readonly Reactive<string> _activeTab = new("run");

    // Per-example execution state - keyed by example ID
    private readonly Dictionary<string, ExampleState> _exampleStates = new();

    // Example selection per tab
    private readonly Reactive<string> _selectedRunExample = new("simple");
    private readonly Reactive<string> _selectedBestOfExample = new("basic");
    private readonly Reactive<string> _selectedMapReduceExample = new("document");
    private readonly Reactive<string> _selectedAdvancedExample = new("solver-critic");

    // Current cancellation token
    private CancellationTokenSource? _cts;

    private ExampleState GetOrCreateState(string exampleId)
    {
        if (!_exampleStates.TryGetValue(exampleId, out var state))
        {
            state = new ExampleState();
            _exampleStates[exampleId] = state;
        }

        return state;
    }

    public async Task Main()
    {
        UI.Root([Page.Default],
            content: view =>
            {
                view.Column([Container.Xl4, "py-8 px-4"], content: view =>
                {
                    // Header
                    view.Column(["mb-6"], content: view =>
                    {
                        view.Text([Text.Display], "Emergence Patterns");
                        view.Text([Text.Body, "text-muted-foreground"], "Interactive examples demonstrating AI agent patterns with live execution visualization");
                    });

                    // Main tabs
                    view.Tabs(
                        value: _activeTab.Value,
                        onValueChange: async value => _activeTab.Value = value ?? "run",
                        listContainerStyle: [Card.Default, "p-2 mb-4"],
                        listStyle: [Tabs.List, "flex-wrap bg-transparent"],
                        triggerStyle: [Tabs.Trigger],
                        contentStyle: [Tabs.Content],
                        tabs: [
                            new TabItem("run", "Run<T>", RenderRunSection),
                            new TabItem("bestof", "BestOf", RenderBestOfSection),
                            new TabItem("mapreduce", "MapReduce", RenderMapReduceSection),
                            new TabItem("taskgraph", "TaskGraph", RenderTaskGraphSection),
                            new TabItem("advanced", "Advanced", RenderAdvancedSection),
                            new TabItem("coder", "Agentic Coder", RenderAgenticCoderSection),
                            new TabItem("treesearch", "TreeSearch", RenderTreeSearchSection),
                            new TabItem("tags", "Structured Tags", RenderTagsSection)
                        ]);
                });
            });
    }

    private void RenderExamplePanel(
        UIView view,
        string exampleId,
        string title,
        string description,
        string whatItDoes,
        Func<ExampleState, Task> runAction)
    {
        var state = GetOrCreateState(exampleId);

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Example header with description
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], title);
                    view.Text([Text.Body, "text-muted-foreground mb-4"], description);

                    // What it does explanation
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "What this example demonstrates:");
                        view.Text([Text.Caption, "text-blue-300"], whatItDoes);
                    });

                    // Progress indicator (when running)
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
                                state.CurrentStage.Value = "Stopped";
                            });
                        }
                        else
                        {
                            view.Button([Button.PrimaryMd], label: "Run Example", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await runAction(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear", onClick: async () =>
                        {
                            state.Clear();
                        });
                    });
                });
            });

            // Split view: logs and result
            view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
            {
                // Event log panel
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

                    view.Box(["overflow-y-auto h-[calc(100%-2rem)] font-mono text-xs"], content: view =>
                    {
                        if (state.Logs.Value.Count == 0)
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], "Click 'Run Example' to start...");
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
                });

                // Result panel
                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] overflow-hidden"], content: view =>
                {
                    view.Text([Text.H3, "mb-2"], "Result");
                    view.Box(["overflow-y-auto h-[calc(100%-2rem)] font-mono text-xs"], content: view =>
                    {
                        if (string.IsNullOrEmpty(state.ResultJson.Value))
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], "The final result will appear here...");
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

    private void LogEvent<T>(ExampleState state, EmergeEvent<T> ev)
    {
        switch (ev)
        {
            case ModelText<T> text:
                // Don't log every text chunk - too noisy
                break;
            case ToolCallPlanned<T> tool:
                state.ToolCallCount.Value++;
                state.Log($"Calling tool: {tool.Call.Function.Name}({TruncateJson(tool.Call.ParametersJson)})", LogLevel.Tool);
                break;
            case ToolCallResult<T> result:
                state.Log($"Tool returned: {TruncateJson(JsonSerializer.Serialize(result.Result))}", LogLevel.Tool);
                break;
            case Stage<T> stage:
                state.CurrentStage.Value = stage.Name;
                state.Log($"Entering stage: {stage.Name}", LogLevel.Stage);
                break;
            case Ikon.AI.Emergence.Progress<T> progress:
                state.Log(progress.Message, LogLevel.Info);
                break;
            case Completed<T> completed:
                state.CurrentIteration.Value = completed.Trace.Iterations;
                state.Log($"Completed! {completed.Trace.Iterations} iterations, {completed.Trace.ToolCalls} tool calls", LogLevel.Result);
                break;
            case Stopped<T> stopped:
                state.Log($"Stopped: {stopped.Reason}", LogLevel.Error);
                break;
        }
    }

    private static string TruncateJson(string json, int maxLength = 80)
    {
        if (string.IsNullOrEmpty(json))
        {
            return "{}";
        }

        if (json.Length <= maxLength)
        {
            return json;
        }

        return json[..(maxLength - 3)] + "...";
    }

    private static KernelContext CreateContext()
    {
        return new KernelContext();
    }

    private static void RenderSlider(
        UIView view,
        double value,
        double min,
        double max,
        double step,
        Action<double> onValueChange,
        [System.Runtime.CompilerServices.CallerFilePath] string file = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
    {
        view.Slider(
            [Slider.Default],
            value: [value],
            min: min,
            max: max,
            step: step,
            onValueChange: values =>
            {
                if (values.Count > 0)
                {
                    onValueChange(values[0]);
                }

                return Task.CompletedTask;
            },
            content: view =>
            {
                view.SliderTrack([Slider.Track], content: view =>
                {
                    view.SliderRange([Slider.Range]);
                });
                view.SliderThumb([Slider.Thumb]);
            },
            file: file,
            line: line);
    }
}

// Per-example state container
public class ExampleState
{
    public Reactive<bool> IsRunning { get; } = new(false);
    public Reactive<string> CurrentStage { get; } = new("Ready");
    public Reactive<int> CurrentIteration { get; } = new(0);
    public Reactive<int> ToolCallCount { get; } = new(0);
    public Reactive<List<LogEntry>> Logs { get; } = new([]);
    public Reactive<string> ResultJson { get; } = new("");

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        var logs = new List<LogEntry>(Logs.Value)
        {
            new(DateTime.Now, level, message)
        };
        Logs.Value = logs;
    }

    public void Clear()
    {
        IsRunning.Value = false;
        CurrentStage.Value = "Ready";
        CurrentIteration.Value = 0;
        ToolCallCount.Value = 0;
        Logs.Value = [];
        ResultJson.Value = "";
    }

    public void SetResult<T>(T result)
    {
        try
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            ResultJson.Value = json;
            Log($"Result set: {json.Length} chars", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Log($"Failed to serialize result: {ex.Message}", LogLevel.Error);
        }
    }
}

public enum LogLevel
{
    Info,
    Event,
    Tool,
    Result,
    Error,
    Stage,
    Iteration
}

public record LogEntry(DateTime Timestamp, LogLevel Level, string Message);
