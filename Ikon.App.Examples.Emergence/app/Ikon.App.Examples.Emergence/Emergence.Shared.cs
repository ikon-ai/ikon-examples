using System.Runtime.CompilerServices;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    // Renders the standard example scaffold shared by the pattern tabs: a description card with a
    // "how it works" note, an optional configuration block, a live-progress indicator, run/stop/clear
    // controls, and the event-stream + result split. New pattern examples build on this so each one is
    // just its config + run logic rather than a full copy of the layout.
    private void RenderPatternExample(
        UIView view,
        string exampleId,
        string title,
        string description,
        string howItWorks,
        Func<ExampleState, Task> runAction,
        string runLabel = "Run Example",
        string resultLabel = "Result",
        Action<UIView>? config = null,
        Func<ExampleState, string>? liveDetail = null)
    {
        var state = GetOrCreateState(exampleId);

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], title);
                    view.Text([Text.Body, "text-muted-foreground mb-4"], description);

                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "How it works:");
                        view.Text([Text.Caption, "text-blue-300 whitespace-pre-wrap"], howItWorks);
                    });

                    if (config != null)
                    {
                        view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: cfg =>
                        {
                            cfg.Text([Text.Caption, "font-semibold mb-3"], "Configuration");
                            cfg.Column([Layout.Column.Md], content: config);
                        });
                    }

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
                                        liveDetail?.Invoke(state) ?? $"Iteration {state.CurrentIteration.Value} | Tool Calls: {state.ToolCallCount.Value}");
                                });
                            });
                        });
                    }

                    view.Row([Layout.Row.Md], content: view =>
                    {
                        if (state.IsRunning.Value)
                        {
                            view.Button([Button.ErrorMd], text: "Stop", onClick: async () =>
                            {
                                _cts?.Cancel();
                                state.IsRunning.Value = false;
                            });
                        }
                        else
                        {
                            view.Button([Button.PrimaryMd], text: runLabel, onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await runAction(state);
                            });
                        }

                        view.Button([Button.OutlineMd], text: "Clear", onClick: async () => state.Clear());
                    });
                });
            });

            view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
            {
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

                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] flex flex-col"], content: view =>
                {
                    view.Text([Text.H3, "mb-2 shrink-0"], resultLabel);
                    var hasResult = !string.IsNullOrEmpty(state.ResultJson.Value);
                    view.Box(["overflow-y-auto flex-1 font-mono text-xs"], key: hasResult ? "has-result" : "no-result", content: view =>
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

    // A labeled slider column for use inside a Configuration block. Forwards caller file/line so each
    // call site gets a stable, distinct reactive key (RenderSlider keys off those).
    private void RenderLabeledSlider(
        UIView view,
        string label,
        double value,
        double min,
        double max,
        double step,
        Action<double> onChange,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        view.Column([Layout.Column.Sm, "flex-1"], content: view =>
        {
            view.Text([Text.Caption, "text-muted-foreground"], label);
            RenderSlider(view, value, min, max, step, onChange, file: file, line: line);
        });
    }

    // A simple example-selector row: renders a button per (key, label); the selected one is highlighted.
    private static void RenderExampleSelector(
        UIView view,
        Reactive<string> selected,
        params (string Key, string Label)[] examples)
    {
        view.Box([Card.Default, "p-4 mb-4"], content: view =>
        {
            view.Text([Text.H3, "mb-3"], "Choose Example");
            view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
            {
                foreach (var (key, label) in examples)
                {
                    var isSelected = selected.Value == key;
                    view.Button(
                        [isSelected ? Button.PrimaryMd : Button.OutlineMd],
                        text: label,
                        onClick: async () => selected.Value = key);
                }
            });
        });
    }
}
