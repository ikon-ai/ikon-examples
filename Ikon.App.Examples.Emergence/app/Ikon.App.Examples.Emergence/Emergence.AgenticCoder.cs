using System.Text.Json;
using Ikon.AI.Emergence;
using Ikon.Common.Core.Functions;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    // Agentic coder types
    public sealed class CoderResponse
    {
        public string Status { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<string> FilesCreated { get; set; } = [];
        public List<string> FilesModified { get; set; } = [];
        public string NextStep { get; set; } = "";
    }

    // Simulated file system for the coder
    private readonly Dictionary<string, string> _virtualFiles = new();
    private readonly List<string> _coderLogs = [];

    // Coder configuration
    private readonly Reactive<string> _coderTask = new("Create a simple calculator class in C# with Add, Subtract, Multiply, and Divide methods. Include XML documentation and unit tests.");
    private readonly Reactive<int> _coderMaxIterations = new(10);
    private readonly Reactive<double> _coderTemperature = new(0.3);

    private void RenderAgenticCoderSection(UIView view)
    {
        var state = GetOrCreateState("agentic-coder");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Header
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Agentic Coder");
                view.Text([Text.Body, "text-muted-foreground"],
                    "A mini coding agent that can write, read, and modify files to complete programming tasks. Uses tools to iteratively build a solution.");
            });

            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "How the Agentic Coder works:");
                        view.Text([Text.Caption, "text-blue-300"],
                            "The agent has access to file system tools (write_file, read_file, list_files) and can iteratively:\n" +
                            "1. Plan the implementation approach\n" +
                            "2. Create files with code\n" +
                            "3. Review and refine the code\n" +
                            "4. Add tests and documentation\n\n" +
                            "Files are stored in a virtual file system shown below.");
                    });

                    // Task input
                    view.Column([Layout.Column.Sm, "mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-muted-foreground font-semibold"], "Coding Task");
                        view.TextArea(
                            ["w-full h-24 font-mono text-sm"],
                            value: _coderTask.Value,
                            placeholder: "Describe what you want the agent to build...",
                            onValueChange: async v => _coderTask.Value = v ?? "");
                    });

                    // Settings row
                    view.Row([Layout.Row.Lg, "mb-4"], content: view =>
                    {
                        view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], $"Max Iterations: {_coderMaxIterations.Value}");
                            RenderSlider(view, _coderMaxIterations.Value, 3, 15, 1, v => _coderMaxIterations.Value = (int)v);
                        });

                        view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], $"Temperature: {_coderTemperature.Value:F1}");
                            RenderSlider(view, _coderTemperature.Value, 0.0, 1.0, 0.1, v => _coderTemperature.Value = v);
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
                                        $"Iteration {state.CurrentIteration.Value} | Tool Calls: {state.ToolCallCount.Value} | Files: {_virtualFiles.Count}");
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
                            view.Button([Button.PrimaryMd], label: "Start Coding", onClick: async () =>
                            {
                                state.Clear();
                                _virtualFiles.Clear();
                                _coderLogs.Clear();
                                _cts = new CancellationTokenSource();
                                await RunAgenticCoder(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear All", onClick: async () =>
                        {
                            state.Clear();
                            _virtualFiles.Clear();
                            _coderLogs.Clear();
                        });
                    });
                });
            });

            // Two-row layout: Top row (Activity + Files), Bottom row (Code preview)
            view.Column([Layout.Column.Lg], content: view =>
            {
                // Top row: Activity log and file list side by side
                view.Row([Layout.Row.Lg, "flex-col md:flex-row"], content: view =>
                {
                    // Event log - takes most space
                    view.Box([Card.Default, "p-4 flex-1 min-h-[250px] max-h-[350px] flex flex-col"], content: view =>
                    {
                        view.Row(["justify-between items-center mb-2 shrink-0"], content: view =>
                        {
                            view.Text([Text.H3], "Agent Activity");
                            if (state.Logs.Value.Count > 0)
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], $"{state.Logs.Value.Count} events");
                            }
                        });

                        RenderLogList(view, state);
                    });

                    // File explorer - fixed width sidebar
                    view.Box([Card.Default, "p-4 w-full md:w-56 shrink-0 min-h-[250px] max-h-[350px] flex flex-col"], content: view =>
                    {
                        view.Text([Text.H3, "mb-2 shrink-0"], "Files");
                        view.Box(["overflow-y-auto flex-1"], content: view =>
                        {
                            if (_virtualFiles.Count == 0)
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "No files yet...");
                            }
                            else
                            {
                                foreach (var file in _virtualFiles.Keys.OrderBy(f => f))
                                {
                                    var isSelected = _selectedFile.Value == file;
                                    var bgStyle = isSelected ? "bg-primary/20" : "bg-transparent hover:bg-muted";

                                    view.Button([$"w-full justify-start py-1.5 px-2 rounded text-left {bgStyle}"],
                                        label: $"{GetFileIcon(file)} {file}",
                                        onClick: async () => _selectedFile.Value = file);
                                }
                            }
                        });
                    });
                });

                // Bottom row: Code preview - full width
                view.Box([Card.Default, "p-4 min-h-[300px] max-h-[450px] flex flex-col"], content: view =>
                {
                    view.Row(["justify-between items-center mb-2 shrink-0"], content: view =>
                    {
                        view.Text([Text.H3], string.IsNullOrEmpty(_selectedFile.Value) ? "Code Preview" : _selectedFile.Value);
                        if (!string.IsNullOrEmpty(_selectedFile.Value) && _virtualFiles.ContainsKey(_selectedFile.Value))
                        {
                            var lines = _virtualFiles[_selectedFile.Value].Split('\n').Length;
                            view.Text([Text.Caption, "text-muted-foreground"], $"{lines} lines");
                        }
                    });

                    view.Box(["overflow-y-auto flex-1 font-mono text-xs bg-muted/30 rounded p-3"], content: view =>
                    {
                        if (string.IsNullOrEmpty(_selectedFile.Value))
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], "Select a file to view its contents...");
                        }
                        else if (_virtualFiles.TryGetValue(_selectedFile.Value, out var content))
                        {
                            view.Text([Text.Caption, "whitespace-pre-wrap"], content);
                        }
                        else
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], "File not found");
                        }
                    });
                });
            });

            // Result summary
            if (!string.IsNullOrEmpty(state.ResultJson.Value))
            {
                view.Box([Card.Default, "p-4 mt-4"], content: view =>
                {
                    view.Text([Text.H3, "mb-2"], "Completion Summary");
                    view.Box(["bg-green-500/10 p-3 rounded font-mono text-xs"], content: view =>
                    {
                        view.Text([Text.Caption, "text-green-400 whitespace-pre-wrap"], state.ResultJson.Value);
                    });
                });
            }
        });
    }

    private readonly Reactive<string> _selectedFile = new("");

    private static string GetFileIcon(string filename)
    {
        if (filename.EndsWith(".cs")) return "[C#]";
        if (filename.EndsWith(".json")) return "[JSON]";
        if (filename.EndsWith(".md")) return "[MD]";
        if (filename.EndsWith(".txt")) return "[TXT]";
        if (filename.EndsWith(".xml")) return "[XML]";
        return "[FILE]";
    }

    private async Task RunAgenticCoder(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Initializing agent...";

        try
        {
            var ctx = CreateContext();
            var task = _coderTask.Value;
            var maxIterations = _coderMaxIterations.Value;
            var temperature = _coderTemperature.Value;

            state.Log($"Starting agentic coder with task: {TruncateJson(task, 80)}", LogLevel.Info);
            state.Log($"Max iterations: {maxIterations}, Temperature: {temperature:F1}", LogLevel.Info);

            await foreach (var ev in Emerge.Run<CoderResponse>(LLMModel.Claude45Sonnet, ctx, pass =>
            {
                // Add file system tools
                pass.AddTool("write_file", "Write content to a file. Creates the file if it doesn't exist, overwrites if it does.",
                    (string path, string content) => WriteFile(state, path, content))
                   .AddTool("read_file", "Read the contents of a file",
                    (string path) => ReadFile(state, path))
                   .AddTool("list_files", "List all files in the virtual file system",
                    () => ListFiles(state))
                   .AddTool("delete_file", "Delete a file from the virtual file system",
                    (string path) => DeleteFile(state, path))
                   .AddTool("search_in_files", "Search for a pattern in all files",
                    (string pattern) => SearchInFiles(state, pattern));

                var filesList = _virtualFiles.Count > 0
                    ? $"\n\nCurrent files:\n{string.Join("\n", _virtualFiles.Keys.Select(f => $"- {f}"))}"
                    : "\n\nNo files created yet.";

                pass.Command = $"""
                    You are an expert software developer. Complete the following task:

                    {task}

                    Use the available tools to:
                    1. Create well-structured, clean code
                    2. Add appropriate documentation
                    3. Include unit tests if applicable
                    4. Follow best practices for the language/framework

                    Current workspace state:{filesList}

                    When you have completed the task, return a JSON summary:
                    {pass.JsonSchema}

                    Think step by step. Create files as needed. Review your work before finishing.
                    """;

                pass.Temperature = temperature;
                pass.MaxIterations = maxIterations;
                pass.MaxToolCalls = 50;

                state.CurrentIteration.Value = pass.Iteration;
                state.CurrentStage.Value = pass.Iteration == 0 ? "Planning approach..." : $"Iteration {pass.Iteration}";

                if (pass.Iteration > 0 && pass.HasNewFunctionResults)
                {
                    state.Log($"Iteration {pass.Iteration} - processing tool results", LogLevel.Iteration);
                }
            }).WithCancellation(_cts!.Token))
            {
                LogCoderEvent(state, ev);

                if (ev is Completed<CoderResponse> completed)
                {
                    state.SetResult(completed.Result);
                    var filesCount = _virtualFiles.Count;
                    state.CurrentStage.Value = $"Complete - {filesCount} files created";
                    state.Log($"Task completed: {completed.Result.Summary}", LogLevel.Result);

                    if (completed.Result.FilesCreated.Count > 0)
                    {
                        state.Log($"Files created: {string.Join(", ", completed.Result.FilesCreated)}", LogLevel.Result);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            state.Log("Cancelled by user", LogLevel.Error);
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

    private void LogCoderEvent<T>(ExampleState state, EmergeEvent<T> ev)
    {
        switch (ev)
        {
            case ToolCallPlanned<T> tool:
                state.ToolCallCount.Value++;
                var args = TruncateJson(tool.Call.ParametersJson, 60);
                state.Log($"Tool: {tool.Call.Function.Name}({args})", LogLevel.Tool);
                break;
            case ToolCallResult<T> result:
                var resultStr = TruncateJson(JsonSerializer.Serialize(result.Result), 80);
                state.Log($"Result: {resultStr}", LogLevel.Tool);
                break;
            case Completed<T> completed:
                state.Log($"Completed! {completed.Trace.Iterations} iterations, {completed.Trace.ToolCalls} tool calls", LogLevel.Result);
                break;
            case Stopped<T> stopped:
                state.Log($"Stopped: {stopped.Reason}", LogLevel.Error);
                break;
        }
    }

    // Tool implementations
    private object WriteFile(ExampleState state, string path, string content)
    {
        var isNew = !_virtualFiles.ContainsKey(path);
        _virtualFiles[path] = content;

        var lines = content.Split('\n').Length;
        var action = isNew ? "Created" : "Updated";
        state.Log($"{action} file: {path} ({lines} lines)", LogLevel.Event);

        if (string.IsNullOrEmpty(_selectedFile.Value))
        {
            _selectedFile.Value = path;
        }

        return new { success = true, action, path, lines };
    }

    private object ReadFile(ExampleState state, string path)
    {
        if (_virtualFiles.TryGetValue(path, out var content))
        {
            state.Log($"Read file: {path}", LogLevel.Event);
            return new { success = true, path, content };
        }

        state.Log($"File not found: {path}", LogLevel.Error);
        return new { success = false, error = $"File not found: {path}" };
    }

    private object ListFiles(ExampleState state)
    {
        var files = _virtualFiles.Keys.OrderBy(f => f).ToList();
        state.Log($"Listed {files.Count} files", LogLevel.Event);
        return new { success = true, files, count = files.Count };
    }

    private object DeleteFile(ExampleState state, string path)
    {
        if (_virtualFiles.Remove(path))
        {
            state.Log($"Deleted file: {path}", LogLevel.Event);

            if (_selectedFile.Value == path)
            {
                _selectedFile.Value = _virtualFiles.Keys.FirstOrDefault() ?? "";
            }

            return new { success = true, deleted = path };
        }

        return new { success = false, error = $"File not found: {path}" };
    }

    private object SearchInFiles(ExampleState state, string pattern)
    {
        var results = new List<object>();

        foreach (var (path, content) in _virtualFiles)
        {
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new { file = path, line = i + 1, content = lines[i].Trim() });
                }
            }
        }

        state.Log($"Search '{pattern}': {results.Count} matches", LogLevel.Event);
        return new { success = true, pattern, matches = results, count = results.Count };
    }
}
