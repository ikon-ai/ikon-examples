using System.Text.Json;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    // MapReduce types
    public sealed class ChunkSummary
    {
        public string MainTopic { get; set; } = "";
        public List<string> KeyFacts { get; set; } = [];
        public string Sentiment { get; set; } = "";
    }

    public sealed class DocumentSummary
    {
        public string Title { get; set; } = "";
        public string ExecutiveSummary { get; set; } = "";
        public List<string> AllKeyFacts { get; set; } = [];
        public string OverallSentiment { get; set; } = "";
        public int ChunksProcessed { get; set; }
    }

    public sealed class ReviewAnalysis
    {
        public string ProductAspect { get; set; } = "";
        public List<string> Positives { get; set; } = [];
        public List<string> Negatives { get; set; } = [];
        public double SentimentScore { get; set; }
    }

    public sealed class ProductReport
    {
        public string ProductName { get; set; } = "";
        public double OverallRating { get; set; }
        public List<string> TopStrengths { get; set; } = [];
        public List<string> TopWeaknesses { get; set; } = [];
        public string Recommendation { get; set; } = "";
    }

    // MapReduce configuration
    private readonly Reactive<int> _mapReduceParallel = new(3);
    private readonly Reactive<double> _mapTemperature = new(0.3);
    private readonly Reactive<double> _reduceTemperature = new(0.2);

    private readonly Reactive<string> _documentText = new("""
Chapter 1: The Rise of Artificial Intelligence
The field of artificial intelligence has seen remarkable growth in the past decade.
Machine learning algorithms have become increasingly sophisticated, enabling computers
to perform tasks that were once thought to be exclusively human domains.

Chapter 2: Challenges and Concerns
Despite the impressive advances, AI development faces significant challenges.
Issues of bias in training data, lack of interpretability in deep learning models,
and concerns about job displacement have sparked important debates.

Chapter 3: The Future Outlook
Looking ahead, experts predict continued rapid advancement in AI capabilities.
Quantum computing may accelerate machine learning, while new architectures
continue to push the boundaries of what's possible.
""");

    private readonly Reactive<string> _reviewsText = new("""
Great product! Battery life is amazing and the camera quality exceeded my expectations.
---
Decent phone but the software has some bugs. Customer support was helpful though.
---
Best phone I've ever owned. Fast, reliable, and the design is sleek.
---
Disappointed with the battery - doesn't last a full day for me.
""");

    private void RenderMapReduceSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Pattern description
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "MapReduce Pattern");
                view.Text([Text.Body, "text-muted-foreground"],
                    "Splits data into chunks, processes each in parallel (Map), then combines results (Reduce). Great for analyzing large documents or datasets.");
            });

            // Example selector
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H3, "mb-3"], "Choose Example");
                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    var examples = new[] { ("document", "Document Summarization"), ("reviews", "Review Analysis") };
                    foreach (var (key, label) in examples)
                    {
                        var isSelected = _selectedMapReduceExample.Value == key;
                        view.Button(
                            [isSelected ? Button.PrimaryMd : Button.OutlineMd],
                            label: label,
                            onClick: async () => _selectedMapReduceExample.Value = key);
                    }
                });
            });

            // Render selected example
            switch (_selectedMapReduceExample.Value)
            {
                case "document":
                    RenderDocumentExample(view);
                    break;
                case "reviews":
                    RenderReviewsExample(view);
                    break;
            }
        });
    }

    private void RenderDocumentExample(UIView view)
    {
        var state = GetOrCreateState("mapreduce-document");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Document Summarization");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "Splits a document into chapters/sections, summarizes each in parallel, then merges into a final summary.");

                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "How MapReduce works:");
                        view.Text([Text.Caption, "text-blue-300"],
                            $"1. Split: Document is divided into {CountChunks(_documentText.Value)} chunks (by '---' or chapters)\n" +
                            $"2. Map: Each chunk is summarized in parallel (up to {_mapReduceParallel.Value} at once)\n" +
                            $"3. Reduce: All summaries are merged into a final comprehensive summary\n" +
                            $"You'll see Stage events for Map and Reduce phases.");
                    });

                    // Configuration
                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-3"], "Configuration");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            // Document input
                            view.Column([Layout.Column.Sm], content: view =>
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Document (separate sections with '---' or 'Chapter')");
                                view.TextArea(
                                    ["w-full h-32 font-mono text-xs"],
                                    value: _documentText.Value,
                                    placeholder: "Paste document text here...",
                                    onValueChange: async v => _documentText.Value = v ?? "");
                            });

                            view.Row([Layout.Row.Lg], content: view =>
                            {
                                // Parallel count
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Parallel Workers: {_mapReduceParallel.Value}");
                                    RenderSlider(view, _mapReduceParallel.Value, 1, 5, 1, v => _mapReduceParallel.Value = (int)v);
                                });

                                // Map temperature
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Map Temp: {_mapTemperature.Value:F1}");
                                    RenderSlider(view, _mapTemperature.Value, 0.0, 1.0, 0.1, v => _mapTemperature.Value = v);
                                });

                                // Reduce temperature
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Reduce Temp: {_reduceTemperature.Value:F1}");
                                    RenderSlider(view, _reduceTemperature.Value, 0.0, 1.0, 0.1, v => _reduceTemperature.Value = v);
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
                                        $"Chunks processed: {state.CurrentIteration.Value}");
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
                            view.Button([Button.PrimaryMd], label: "Summarize Document", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunDocumentMapReduceExample(state);
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
                    view.Text([Text.H3, "mb-2"], "Final Summary");
                    // Use key to force re-render when result changes
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

    private void RenderReviewsExample(UIView view)
    {
        var state = GetOrCreateState("mapreduce-reviews");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H2], "Review Analysis");
                    view.Text([Text.Body, "text-muted-foreground mb-4"],
                        "Analyzes multiple product reviews in parallel, then synthesizes a comprehensive product report.");

                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "How this example works:");
                        view.Text([Text.Caption, "text-blue-300"],
                            $"1. Split: Reviews are separated by '---' ({CountChunks(_reviewsText.Value)} reviews)\n" +
                            $"2. Map: Each review is analyzed for sentiment, positives, negatives\n" +
                            $"3. Reduce: All analyses merged into overall rating and recommendations\n" +
                            $"Great for processing customer feedback at scale.");
                    });

                    // Configuration
                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-3"], "Configuration");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            // Reviews input
                            view.Column([Layout.Column.Sm], content: view =>
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Reviews (separate with '---')");
                                view.TextArea(
                                    ["w-full h-32 font-mono text-xs"],
                                    value: _reviewsText.Value,
                                    placeholder: "Paste reviews here, separated by ---...",
                                    onValueChange: async v => _reviewsText.Value = v ?? "");
                            });

                            view.Row([Layout.Row.Lg], content: view =>
                            {
                                // Parallel count
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Parallel Workers: {_mapReduceParallel.Value}");
                                    RenderSlider(view, _mapReduceParallel.Value, 1, 5, 1, v => _mapReduceParallel.Value = (int)v);
                                });

                                // Map temperature
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Map Temp: {_mapTemperature.Value:F1}");
                                    RenderSlider(view, _mapTemperature.Value, 0.0, 1.0, 0.1, v => _mapTemperature.Value = v);
                                });

                                // Reduce temperature
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Reduce Temp: {_reduceTemperature.Value:F1}");
                                    RenderSlider(view, _reduceTemperature.Value, 0.0, 1.0, 0.1, v => _reduceTemperature.Value = v);
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
                                        $"Reviews analyzed: {state.CurrentIteration.Value}");
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
                            view.Button([Button.PrimaryMd], label: "Analyze Reviews", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunReviewsMapReduceExample(state);
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
                    view.Text([Text.H3, "mb-2"], "Product Report");
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

    private static int CountChunks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        // Count by separator or chapter headers
        var chunks = SplitIntoChunks(text);
        return chunks.Length;
    }

    private static string[] SplitIntoChunks(string text)
    {
        // Split by --- or Chapter headers
        var separatorChunks = text.Split(["---", "Chapter "], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return separatorChunks.Where(c => c.Length > 20).ToArray();
    }

    private async Task RunDocumentMapReduceExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Splitting document...";

        try
        {
            var ctx = CreateContext();
            var documentChunks = SplitIntoChunks(_documentText.Value);

            state.Log($"Starting MapReduce with {documentChunks.Length} document chunks", LogLevel.Info);
            state.Log($"Parallel workers: {_mapReduceParallel.Value}, Map temp: {_mapTemperature.Value:F1}, Reduce temp: {_reduceTemperature.Value:F1}", LogLevel.Info);

            var parallel = _mapReduceParallel.Value;
            var mapTemp = _mapTemperature.Value;
            var reduceTemp = _reduceTemperature.Value;

            await foreach (var ev in Emerge.MapReduce<ChunkSummary, DocumentSummary>(LLMModel.Claude45Sonnet, ctx, mr =>
            {
                mr.Input = documentChunks;
                mr.MaxParallel = parallel;

                mr.Map(map =>
                {
                    map.Temperature = mapTemp;
                    map.Command = $"""
                        Summarize this text chunk and extract key information.
                        Return JSON:
                        {map.JsonSchema}
                        """;
                });

                mr.Reduce(reduce =>
                {
                    reduce.Temperature = reduceTemp;
                    reduce.Command = $"""
                        Combine these chunk summaries into a comprehensive document summary.
                        Merge all key facts, identify the overall sentiment, and create an executive summary.
                        Return JSON:
                        {reduce.JsonSchema}
                        """;
                });
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<DocumentSummary> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                }

                if (ev is Ikon.AI.Emergence.Progress<DocumentSummary> progress)
                {
                    state.CurrentIteration.Value++;
                }

                if (ev is Completed<DocumentSummary> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = $"Complete - {completed.Result.ChunksProcessed} chunks summarized";
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

    private async Task RunReviewsMapReduceExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Splitting reviews...";

        try
        {
            var ctx = CreateContext();
            var reviews = _reviewsText.Value.Split("---", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(r => r.Length > 10)
                .ToArray();

            state.Log($"Starting MapReduce analysis of {reviews.Length} product reviews", LogLevel.Info);
            state.Log($"Parallel workers: {_mapReduceParallel.Value}", LogLevel.Info);

            var parallel = _mapReduceParallel.Value;
            var mapTemp = _mapTemperature.Value;
            var reduceTemp = _reduceTemperature.Value;

            await foreach (var ev in Emerge.MapReduce<ReviewAnalysis, ProductReport>(LLMModel.Claude45Sonnet, ctx, mr =>
            {
                mr.Input = reviews;
                mr.MaxParallel = parallel;

                mr.Map(map =>
                {
                    map.Temperature = mapTemp;
                    map.Command = $"""
                        Analyze this product review. Extract the product aspects mentioned,
                        list positives and negatives, and calculate a sentiment score (-1 to 1).
                        Return JSON:
                        {map.JsonSchema}
                        """;
                });

                mr.Reduce(reduce =>
                {
                    reduce.Temperature = reduceTemp;
                    reduce.Command = $"""
                        Synthesize these individual review analyses into a comprehensive product report.
                        Identify top 3 strengths and weaknesses, calculate overall rating (1-5),
                        and provide a recommendation.
                        Return JSON:
                        {reduce.JsonSchema}
                        """;
                });
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Stage<ProductReport> stage)
                {
                    state.CurrentStage.Value = stage.Name;
                }

                if (ev is Ikon.AI.Emergence.Progress<ProductReport> progress)
                {
                    state.CurrentIteration.Value++;
                }

                if (ev is Completed<ProductReport> completed)
                {
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = $"Complete - Rating: {completed.Result.OverallRating:F1}/5";
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
