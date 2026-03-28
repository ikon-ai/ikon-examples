using Ikon.AI.Emergence;
using Ikon.AI.Emergence.Tree;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    // TreeSearch tab state
    private readonly Reactive<string> _selectedTreeSearchExample = new("search");
    private readonly Reactive<string> _treeSearchQuery = new("What are the key features?");
    private readonly Reactive<bool> _showSourceDocument = new(false);
    private readonly Reactive<bool> _useCustomDocument = new(false);
    private readonly Reactive<string> _customDocument = new("");

    // Sample document content for demos
    private const string SampleMarkdownDocument = """
        # Product Documentation

        Welcome to our product documentation. This guide covers all aspects of using our platform.

        ## Getting Started

        This section helps new users get up and running quickly.

        ### Installation

        To install the product, follow these steps:
        1. Download the installer from our website
        2. Run the installer with administrator privileges
        3. Follow the on-screen instructions
        4. Restart your computer when prompted

        System requirements:
        - Windows 10 or later, macOS 12+, or Linux (Ubuntu 20.04+)
        - 8 GB RAM minimum (16 GB recommended)
        - 500 MB disk space
        - Internet connection for activation

        ### Configuration

        After installation, configure the product:
        1. Open the settings panel
        2. Enter your license key
        3. Configure your preferences
        4. Set up integrations with other tools

        ## Core Features

        Our platform provides several key features for productivity.

        ### Feature A: Data Processing

        The data processing engine handles large datasets efficiently:
        - Supports CSV, JSON, XML, and Parquet formats
        - Parallel processing for improved performance
        - Automatic schema detection
        - Built-in data validation

        ### Feature B: Analytics Dashboard

        The analytics dashboard provides real-time insights:
        - Customizable widgets and layouts
        - Interactive charts and graphs
        - Export to PDF and Excel
        - Scheduled report generation

        ### Feature C: API Integration

        Connect with external services through our API:
        - RESTful API with OpenAPI documentation
        - Webhook support for event-driven workflows
        - OAuth 2.0 authentication
        - Rate limiting and quotas

        ## Troubleshooting

        Common issues and their solutions.

        ### Connection Problems

        If you experience connection issues:
        1. Check your internet connection
        2. Verify firewall settings
        3. Ensure the service is running
        4. Contact support if issues persist

        ### Performance Issues

        To improve performance:
        1. Close unnecessary applications
        2. Increase allocated memory
        3. Clear the cache
        4. Update to the latest version

        ## Support

        For additional help, contact our support team at support@example.com or visit our community forums.
        """;

    private string GetActiveDocument() => _useCustomDocument.Value && !string.IsNullOrWhiteSpace(_customDocument.Value)
        ? _customDocument.Value
        : SampleMarkdownDocument;

    private void RenderTreeSearchSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Tab header
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "TreeSearch Pattern");
                view.Text([Text.Body, "text-muted-foreground"],
                    "Vectorless RAG using hierarchical document indexing and LLM reasoning. Navigates document structure like a human expert rather than using vector similarity.");
            });

            // Example selector
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H3, "mb-3"], "Choose Example");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    var examples = new[] { ("basic", "Build Tree Index"), ("search", "Search & Navigate") };
                    foreach (var (key, label) in examples)
                    {
                        var isSelected = _selectedTreeSearchExample.Value == key;
                        view.Button(
                            [isSelected ? Button.PrimaryMd : Button.OutlineMd],
                            label: label,
                            onClick: async () => _selectedTreeSearchExample.Value = key);
                    }
                });
            });

            // Render selected example
            switch (_selectedTreeSearchExample.Value)
            {
                case "basic":
                    RenderBasicTreeIndexExample(view);
                    break;
                case "search":
                    RenderTreeSearchExample(view);
                    break;
            }
        });
    }

    private void RenderBasicTreeIndexExample(UIView view)
    {
        var state = GetOrCreateState("treesearch-basic");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Build Tree Index");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "Build a hierarchical tree index from a markdown document.");

                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "What this example demonstrates:");
                        view.Text([Text.Caption, "text-blue-300"],
                            "Uses LLM to analyze document structure and create a table-of-contents-like tree. Each node gets an ID, title, and summary for navigation.");
                    });

                    // Document source toggle
                    RenderDocumentSourceSelector(view);

                    // Progress indicator
                    if (state.IsRunning.Value)
                    {
                        view.Box(["bg-yellow-500/10 border border-yellow-500/20 rounded-lg p-4 mb-4"], content: view =>
                        {
                            view.Row([Layout.Row.Sm, "items-center"], content: view =>
                            {
                                view.Box([Icon.Spinner, "mr-2 text-yellow-400"]);
                                view.Text([Text.Caption, "text-yellow-400 font-semibold"], state.CurrentStage.Value);
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
                            view.Button([Button.PrimaryMd], label: "Build Index", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunBasicTreeIndexExample(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear", onClick: async () => state.Clear());
                    });
                });
            });

            // Results: Source document and Tree index side by side
            view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
            {
                // Source document panel
                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] flex flex-col"], content: view =>
                {
                    view.Row(["justify-between items-center mb-2 shrink-0"], content: view =>
                    {
                        view.Text([Text.H3], "Source Document");
                        var doc = GetActiveDocument();
                        view.Text([Text.Caption, "text-muted-foreground"], $"{doc.Length} chars");
                    });

                    view.Box(["overflow-y-auto flex-1 font-mono text-xs bg-muted/30 rounded p-3"], content: view =>
                    {
                        view.Text([Text.Caption, "whitespace-pre-wrap"], GetActiveDocument());
                    });
                });

                // Tree index result
                view.Box([Card.Default, "p-4 flex-1 min-h-[400px] max-h-[600px] flex flex-col"], content: view =>
                {
                    view.Text([Text.H3, "mb-2 shrink-0"], "Tree Index (Table of Contents)");
                    view.Box(["overflow-y-auto flex-1 font-mono text-xs"], content: view =>
                    {
                        if (string.IsNullOrEmpty(state.ResultJson.Value))
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], "Click 'Build Index' to generate the tree structure...");
                        }
                        else
                        {
                            view.Box(["bg-green-500/10 p-3 rounded"], content: view =>
                            {
                                view.Text([Text.Caption, "text-green-400 whitespace-pre-wrap"], state.ResultJson.Value);
                            });
                        }
                    });
                });
            });

            // Event log at bottom
            view.Box([Card.Default, "p-4 min-h-[150px] max-h-[250px] flex flex-col"], content: view =>
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
        });
    }

    private void RenderTreeSearchExample(UIView view)
    {
        var state = GetOrCreateState("treesearch-search");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Search & Navigate");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "Build a tree index and then search it using LLM reasoning.");

                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "What this example demonstrates:");
                        view.Text([Text.Caption, "text-blue-300"],
                            "Two-phase process: First builds a tree index from the document, then uses tools (tree_get_children, tree_get_content, tree_mark_relevant) to navigate and find relevant sections.");
                    });

                    // Document source toggle
                    RenderDocumentSourceSelector(view);

                    // Search query input
                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-2"], "Search Query");
                        view.TextField(
                            [Input.Default, "w-full"],
                            value: _treeSearchQuery.Value,
                            placeholder: "What would you like to find?",
                            onValueChange: async v => _treeSearchQuery.Value = v ?? "");

                        view.Text([Text.Caption, "text-muted-foreground mt-2"],
                            "Try: \"What are the system requirements?\", \"How do I configure?\", \"What features are available?\"");
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
                            view.Button([Button.PrimaryMd], label: "Build & Search", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunTreeSearchExample(state);
                            });
                        }

                        view.Button([Button.OutlineMd], label: "Clear", onClick: async () => state.Clear());

                        // Toggle source document view
                        view.Button(
                            [_showSourceDocument.Value ? Button.SecondaryMd : Button.OutlineMd],
                            label: _showSourceDocument.Value ? "Hide Source" : "Show Source",
                            onClick: async () => _showSourceDocument.Value = !_showSourceDocument.Value);
                    });
                });
            });

            // Source document panel (collapsible)
            if (_showSourceDocument.Value)
            {
                view.Box([Card.Default, "p-4 mb-4 max-h-[300px] flex flex-col"], content: view =>
                {
                    view.Row(["justify-between items-center mb-2 shrink-0"], content: view =>
                    {
                        view.Text([Text.H3], "Source Document");
                        var doc = GetActiveDocument();
                        view.Text([Text.Caption, "text-muted-foreground"], $"{doc.Length} chars");
                    });

                    view.Box(["overflow-y-auto flex-1 font-mono text-xs bg-muted/30 rounded p-3"], content: view =>
                    {
                        view.Text([Text.Caption, "whitespace-pre-wrap"], GetActiveDocument());
                    });
                });
            }

            // Results panels: Event stream and Found sections
            view.Row([Layout.Row.Lg, "flex-col lg:flex-row"], content: view =>
            {
                // Event log
                view.Box([Card.Default, "p-4 flex-1 min-h-[350px] max-h-[500px] flex flex-col"], content: view =>
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

                // Found sections result
                view.Box([Card.Default, "p-4 flex-1 min-h-[350px] max-h-[500px] flex flex-col"], content: view =>
                {
                    view.Text([Text.H3, "mb-2 shrink-0"], "Found Sections");
                    view.Box(["overflow-y-auto flex-1 font-mono text-xs"], content: view =>
                    {
                        if (string.IsNullOrEmpty(state.ResultJson.Value))
                        {
                            view.Text([Text.Caption, "text-muted-foreground"], "Relevant sections will appear here...");
                        }
                        else
                        {
                            view.Box(["bg-green-500/10 p-3 rounded"], content: view =>
                            {
                                view.Text([Text.Caption, "text-green-400 whitespace-pre-wrap"], state.ResultJson.Value);
                            });
                        }
                    });
                });
            });
        });
    }

    private void RenderDocumentSourceSelector(UIView view)
    {
        view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
        {
            view.Row(["justify-between items-center mb-3"], content: view =>
            {
                view.Text([Text.Caption, "font-semibold"], "Document Source");
                view.Row([Layout.Row.Sm], content: view =>
                {
                    view.Button(
                        [!_useCustomDocument.Value ? Button.PrimarySm : Button.OutlineSm],
                        label: "Sample Doc",
                        onClick: async () => _useCustomDocument.Value = false);
                    view.Button(
                        [_useCustomDocument.Value ? Button.PrimarySm : Button.OutlineSm],
                        label: "Custom",
                        onClick: async () => _useCustomDocument.Value = true);
                });
            });

            if (_useCustomDocument.Value)
            {
                view.TextArea(
                    ["w-full h-32 font-mono text-xs"],
                    value: _customDocument.Value,
                    placeholder: "Paste your markdown document here...\n\n# Title\n\n## Section 1\nContent...\n\n## Section 2\nMore content...",
                    onValueChange: async v => _customDocument.Value = v ?? "");

                if (string.IsNullOrWhiteSpace(_customDocument.Value))
                {
                    view.Text([Text.Caption, "text-yellow-500 mt-2"], "Enter a document above, or switch to Sample Doc");
                }
            }
            else
            {
                view.Text([Text.Caption, "text-muted-foreground"],
                    "Using sample product documentation (installation, features, troubleshooting)");
            }
        });
    }

    private async Task RunBasicTreeIndexExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Building tree index...";

        try
        {
            var document = GetActiveDocument();
            state.Log("Starting tree index build from document", LogLevel.Info);
            state.Log($"Document size: {document.Length} characters", LogLevel.Info);

            TreeIndex? index = null;

            await foreach (var ev in TreeIndex.BuildAsync(LLMModel.Claude45Sonnet, document, new TreeIndexOptions
            {
                MaxDepth = 4,
                GenerateSummaries = true
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<TreeIndex> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                }
                else if (ev is Ikon.AI.Emergence.Progress<TreeIndex> progress)
                {
                    state.Log(progress.Message, LogLevel.Info);
                }
                else if (ev is Completed<TreeIndex> completed)
                {
                    index = completed.Result;
                    state.CurrentStage.Value = "Complete";

                    var nodeCount = index.Traverse().Count();
                    state.Log($"Tree built with {nodeCount} nodes", LogLevel.Result);

                    // Show the table of contents as result
                    var toc = index.ToTableOfContents();
                    state.ResultJson.Value = toc;
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

    private async Task RunTreeSearchExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Building tree index...";

        try
        {
            var query = _treeSearchQuery.Value;
            var document = GetActiveDocument();

            state.Log($"Query: {query}", LogLevel.Info);
            state.Log($"Document: {document.Length} characters", LogLevel.Info);
            state.Log("Phase 1: Building tree index...", LogLevel.Stage);

            // Phase 1: Build tree index
            TreeIndex? index = null;

            await foreach (var ev in TreeIndex.BuildAsync(LLMModel.Claude45Sonnet, document, new TreeIndexOptions
            {
                MaxDepth = 4,
                GenerateSummaries = true
            }).WithCancellation(_cts!.Token))
            {
                if (ev is Completed<TreeIndex> completed)
                {
                    index = completed.Result;
                    var nodeCount = index.Traverse().Count();
                    state.Log($"Tree index built with {nodeCount} nodes", LogLevel.Result);
                }
            }

            if (index == null)
            {
                state.Log("Failed to build tree index", LogLevel.Error);
                return;
            }

            // Phase 2: Search the tree
            state.CurrentStage.Value = "Searching tree...";
            state.Log("Phase 2: Searching tree with LLM navigation...", LogLevel.Stage);

            var ctx = CreateContext();

            await foreach (var ev in Emerge.TreeSearch<TreeSearchResult>(LLMModel.Claude45Sonnet, ctx, options =>
            {
                options.Index = index;
                options.Query = query;
                options.MaxSteps = 10;
                options.MaxResults = 3;

                state.CurrentIteration.Value = 0;
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Completed<TreeSearchResult> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = "Complete";

                    state.Log($"Found {completed.Result.Sections.Count} relevant sections", LogLevel.Result);
                    foreach (var section in completed.Result.Sections)
                    {
                        state.Log($"  - {section.Path}: {section.Relevance}", LogLevel.Info);
                    }
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
