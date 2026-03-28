using System.Text.Json;
using Ikon.Parallax.Components.Standard;

public partial class Emergence
{
    // BestOf response type
    public sealed class CreativeResponse
    {
        public string Content { get; set; } = "";
        public string Style { get; set; } = "";
        public int WordCount { get; set; }
    }

    // BestOf configuration
    private readonly Reactive<string> _bestOfPrompt = new("Write a tagline for a sustainable coffee brand");
    private readonly Reactive<int> _candidateCount = new(3);
    private readonly Reactive<double> _temperatureVariance = new(0.4);

    private void RenderBestOfSection(UIView view)
    {
        var state = GetOrCreateState("bestof-main");

        view.Column([Layout.Column.Lg], content: view =>
        {
            // Pattern description
            view.Box([Card.Default, "p-4 mb-4"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "BestOf Pattern");
                view.Text([Text.Body, "text-muted-foreground"],
                    "Generates multiple candidate responses with varying temperatures, scores each one, and returns the best. Great for creative tasks where you want diversity.");
            });

            // Configuration panel
            view.Box([Card.Default, "p-6 mb-4"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    // What it does
                    view.Box(["bg-blue-500/10 border border-blue-500/20 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "text-blue-400 font-semibold mb-1"], "How BestOf works:");
                        view.Text([Text.Caption, "text-blue-300"],
                            $"1. Generates {_candidateCount.Value} candidates with different temperatures\n" +
                            $"2. Scores each on length, lexical richness, structure, and style\n" +
                            $"3. Returns the highest-scoring candidate\n" +
                            $"You'll see Stage events for each candidate generation.");
                    });

                    // Configuration
                    view.Box(["bg-muted/50 rounded-lg p-4 mb-4"], content: view =>
                    {
                        view.Text([Text.Caption, "font-semibold mb-3"], "Configuration");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            // Prompt input
                            view.Column([Layout.Column.Sm], content: view =>
                            {
                                view.Text([Text.Caption, "text-muted-foreground"], "Creative Prompt");
                                view.TextField(
                                    [Input.Default, "w-full"],
                                    value: _bestOfPrompt.Value,
                                    placeholder: "Enter a creative prompt...",
                                    onValueChange: async v => _bestOfPrompt.Value = v ?? "");
                            });

                            view.Row([Layout.Row.Lg], content: view =>
                            {
                                // Candidate count
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Candidates: {_candidateCount.Value}");
                                    RenderSlider(view, _candidateCount.Value, 2, 5, 1, v => _candidateCount.Value = (int)v);
                                });

                                // Temperature variance
                                view.Column([Layout.Column.Sm, "flex-1"], content: view =>
                                {
                                    view.Text([Text.Caption, "text-muted-foreground"], $"Temp Variance: {_temperatureVariance.Value:F1}");
                                    RenderSlider(view, _temperatureVariance.Value, 0.0, 0.5, 0.1, v => _temperatureVariance.Value = v);
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
                                        $"Candidate {state.CurrentIteration.Value + 1} of {_candidateCount.Value}");
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
                            view.Button([Button.PrimaryMd], label: "Generate & Compare", onClick: async () =>
                            {
                                state.Clear();
                                _cts = new CancellationTokenSource();
                                await RunBestOfExample(state);
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
                    view.Text([Text.H3, "mb-2"], "Best Result");
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

    private async Task RunBestOfExample(ExampleState state)
    {
        state.IsRunning.Value = true;
        state.CurrentStage.Value = "Starting BestOf...";

        try
        {
            var ctx = CreateContext();
            var prompt = _bestOfPrompt.Value;
            var count = _candidateCount.Value;
            var variance = _temperatureVariance.Value;

            state.Log($"Generating {count} candidates for: \"{prompt}\"", LogLevel.Info);
            state.Log($"Temperature range: {0.7 - variance:F1} to {0.7 + variance:F1}", LogLevel.Info);

            await foreach (var ev in Emerge.BestOf<CreativeResponse>(LLMModel.Claude45Sonnet, ctx, bo =>
            {
                bo.Command = $"""
                    {prompt}

                    Be creative and original. Aim for something memorable.

                    Return JSON:
                    {bo.JsonSchema}
                    """;
                bo.Count = count;
                bo.Score = (response, trace) =>
                {
                    var text = response.Content;
                    var score = 0.0;

                    // Length sweet spot: 30-120 chars is ideal for a tagline (0-0.25)
                    var len = text.Length;
                    var lengthScore = len switch
                    {
                        >= 30 and <= 80 => 0.25,
                        >= 20 and <= 120 => 0.15,
                        > 0 => 0.05,
                        _ => 0.0
                    };
                    score += lengthScore;

                    // Lexical richness: unique words / total words (0-0.25)
                    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (words.Length > 0)
                    {
                        var uniqueRatio = (double)words.Select(w => w.ToLowerInvariant().Trim(',', '.', '!', '?', '"', '\'')).Distinct().Count() / words.Length;
                        score += uniqueRatio * 0.25;
                    }

                    // Punctuation and structure: questions, alliteration, wordplay signals (0-0.2)
                    if (text.Contains('—') || text.Contains('–') || text.Contains(':'))
                    {
                        score += 0.07;
                    }

                    if (text.Contains('?') || text.Contains('!'))
                    {
                        score += 0.05;
                    }

                    // Check for alliteration (consecutive words starting with same letter)
                    for (var wi = 0; wi < words.Length - 1; wi++)
                    {
                        if (words[wi].Length > 0 && words[wi + 1].Length > 0 &&
                            char.ToLowerInvariant(words[wi][0]) == char.ToLowerInvariant(words[wi + 1][0]))
                        {
                            score += 0.04;
                            break;
                        }
                    }

                    // Uncommon word bonus: longer words suggest richer vocabulary (0-0.15)
                    var avgWordLen = words.Length > 0 ? words.Average(w => w.Length) : 0;
                    score += Math.Min(avgWordLen / 40.0, 0.15);

                    // Style field filled in meaningfully (0-0.15)
                    if (!string.IsNullOrWhiteSpace(response.Style) && response.Style.Length > 5)
                    {
                        score += 0.15;
                    }

                    var finalScore = Math.Min(score, 1.0);
                    state.Log($"Scored candidate: {finalScore:F2} (len={len}, words={words.Length}, richness={score - lengthScore:F2}) - \"{TruncateJson(text, 60)}\"", LogLevel.Result);
                    return finalScore;
                };

                bo.Candidate(c =>
                {
                    c.Temperature = 0.7 + (c.Index - count / 2.0) * variance / (count / 2.0);
                    state.CurrentIteration.Value = c.Index;
                    state.CurrentStage.Value = $"Generating candidate {c.Index + 1}";
                });
            }).WithCancellation(_cts!.Token))
            {
                LogEvent(state, ev);

                if (ev is Completed<CreativeResponse> completed)
                {
                    state.Log($"Completed event received, Result is null: {completed.Result == null}", LogLevel.Info);
                    state.SetResult(completed.Result);
                    state.CurrentStage.Value = "Complete - Best candidate selected";
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
