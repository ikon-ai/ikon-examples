using System.Text.Json;
using Ikon.App.Examples.Globe.WebGLGlobe;

return await App.Run(args);

public record SessionIdentity(string UserId);

public record ClientParams;

[App]
public partial class Globe(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new Theme());

    private readonly Reactive<string> _queryText = new("");
    private readonly Reactive<bool> _isProcessing = new(false);
    private readonly Reactive<GlobeDataSet?> _currentData = new(null);
    private readonly Reactive<string> _currentDataLabel = new("");
    private readonly Reactive<string> _errorMessage = new("");
    private readonly Reactive<List<QueryHistoryItem>> _queryHistory = new([]);
    private readonly Reactive<SelectedSpikeData?> _selectedSpike = new(null);

    public Task Main()
    {
        RenderUI();
        return Task.CompletedTask;
    }

    private async Task ProcessQueryAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || _isProcessing.Value)
        {
            return;
        }

        _isProcessing.Value = true;
        _errorMessage.Value = "";

        try
        {
            var queryResult = await ProcessDataQueryShader.GenerateAsync(query);
            var globeData = await GenerateGlobeDataShader.GenerateAsync(
                queryResult.InterpretedQuery,
                queryResult.DataSourceHint,
                queryResult.SuggestedColor);

            _currentData.Value = globeData;
            _currentDataLabel.Value = queryResult.DisplayLabel;

            var history = _queryHistory.Value;
            history.Insert(0, new QueryHistoryItem
            {
                Query = query,
                Label = queryResult.DisplayLabel,
                Timestamp = DateTime.UtcNow
            });

            if (history.Count > 10)
            {
                history.RemoveAt(history.Count - 1);
            }

            _queryHistory.Value = history;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Query processing failed: {ex.Message}");
            _errorMessage.Value = "Failed to process query. Please try again.";
        }
        finally
        {
            _isProcessing.Value = false;
        }
    }

    private static string SerializeDataPoints(List<GlobeDataPoint> points)
    {
        var data = points.Select(p => new object[] { p.Latitude, p.Longitude, p.Magnitude, p.Label ?? "" }).ToArray();
        return JsonSerializer.Serialize(data);
    }

    private void RenderUI()
    {
        UI.Root(style: [Page.Default, "font-sans bg-slate-950 h-screen w-screen overflow-hidden"], content: view =>
        {
            view.Box(["absolute inset-0 z-0"], content: globeContainer =>
            {
                globeContainer.WebGLGlobe(
                    data: _currentData.Value != null ? SerializeDataPoints(_currentData.Value.Points) : null,
                    seriesName: _currentData.Value?.SeriesName,
                    seriesColor: _currentData.Value?.Color,
                    autoRotate: true,
                    rotationSpeed: 0.5f,
                    globeColor: "#1a1a2e",
                    atmosphereColor: "#3b82f6",
                    onSpikeClick: async spike => { _selectedSpike.Value = spike; },
                    style: ["w-full h-full"]);
            });

            view.Box(["absolute right-0 inset-y-0 w-[400px] p-6 z-10 flex flex-col gap-4 pointer-events-none"], content: panel =>
            {
                panel.Column([Styles.GlassCard, "p-4 pointer-events-auto"], content: queryCard =>
                {
                    queryCard.Text([Text.H3, "text-slate-800 mb-3"], "Ask a Question");
                    queryCard.Column(["gap-2"], content: inputGroup =>
                    {
                        inputGroup.TextField(
                            [Input.Default, "bg-white/80"],
                            placeholder: "e.g., Show population by country",
                            value: _queryText.Value,
                            onValueChange: async value => { _queryText.Value = value; },
                            onSubmit: async _ => await ProcessQueryAsync(_queryText.Value));

                        inputGroup.Button(
                            [Button.PrimaryMd, "w-full"],
                            _isProcessing.Value ? "Processing..." : "Visualize",
                            disabled: _isProcessing.Value || string.IsNullOrWhiteSpace(_queryText.Value),
                            onClick: async () => await ProcessQueryAsync(_queryText.Value));
                    });

                    if (!string.IsNullOrEmpty(_errorMessage.Value))
                    {
                        queryCard.Text(["text-red-500 text-sm mt-2"], _errorMessage.Value);
                    }
                });

                if (_currentData.Value != null)
                {
                    panel.Column([Styles.GlassCard, "p-4 pointer-events-auto"], content: infoCard =>
                    {
                        infoCard.Text([Text.H3, "text-slate-800 mb-2"], "Current Visualization");
                        infoCard.Text(["text-slate-600 font-medium"], _currentDataLabel.Value);
                        infoCard.Row(["gap-4 mt-2"], content: stats =>
                        {
                            stats.Column(["flex-1"], content: stat =>
                            {
                                stat.Text(["text-slate-500 text-xs uppercase"], "Data Points");
                                stat.Text(["text-slate-800 text-xl font-bold"], _currentData.Value.Points.Count.ToString());
                            });
                            stats.Column(["flex-1"], content: stat =>
                            {
                                stat.Text(["text-slate-500 text-xs uppercase"], "Series");
                                stat.Text(["text-slate-800 text-sm font-medium truncate"], _currentData.Value.SeriesName);
                            });
                        });
                    });
                }

                if (_selectedSpike.Value != null)
                {
                    panel.Column([Styles.GlassCard, "p-4 pointer-events-auto"], content: spikeCard =>
                    {
                        spikeCard.Row(["justify-between items-center mb-3"], content: header =>
                        {
                            header.Text([Text.H3, "text-slate-800"], "Selected Location");
                            header.Box(["cursor-pointer text-slate-400 hover:text-slate-600"],
                                onClick: async () => { _selectedSpike.Value = null; },
                                content: x => x.Text(text: "\u2715"));
                        });

                        var spike = _selectedSpike.Value;
                        spikeCard.Text(["text-slate-700 font-medium text-lg"], spike.Label);
                        spikeCard.Row(["gap-4 mt-2"], content: coords =>
                        {
                            coords.Column(["flex-1"], content: c =>
                            {
                                c.Text(["text-slate-500 text-xs uppercase"], "Latitude");
                                c.Text(["text-slate-800 font-mono"], $"{spike.Lat:F2}\u00b0");
                            });
                            coords.Column(["flex-1"], content: c =>
                            {
                                c.Text(["text-slate-500 text-xs uppercase"], "Longitude");
                                c.Text(["text-slate-800 font-mono"], $"{spike.Lon:F2}\u00b0");
                            });
                        });
                        spikeCard.Column(["mt-2"], content: mag =>
                        {
                            mag.Text(["text-slate-500 text-xs uppercase"], "Magnitude");
                            mag.Text(["text-slate-800 text-xl font-bold"], $"{spike.Magnitude:P0}");
                        });
                    });
                }
                else if (_queryHistory.Value.Count > 0)
                {
                    panel.Column([Styles.GlassCard, "p-4 flex-1 overflow-hidden pointer-events-auto"], content: historyCard =>
                    {
                        historyCard.Text([Text.H3, "text-slate-800 mb-3"], "Recent Queries");
                        historyCard.Column(["gap-2 overflow-y-auto flex-1"], content: historyList =>
                        {
                            foreach (var item in _queryHistory.Value.Take(5))
                            {
                                var capturedItem = item;
                                historyList.Box([
                                    "p-2 rounded-lg bg-white/50 hover:bg-white/70 cursor-pointer",
                                    "transition-colors duration-200"
                                ], onClick: async () =>
                                {
                                    _queryText.Value = capturedItem.Query;
                                    await ProcessQueryAsync(capturedItem.Query);
                                }, content: historyRow =>
                                {
                                    historyRow.Text(["text-slate-700 text-sm font-medium truncate"], capturedItem.Label);
                                    historyRow.Text(["text-slate-500 text-xs truncate"], capturedItem.Query);
                                });
                            }
                        });
                    });
                }

                panel.Column([Styles.GlassCardSubtle, "p-3 pointer-events-auto"], content: suggestions =>
                {
                    suggestions.Text(["text-slate-600 text-xs mb-2"], "Try asking:");
                    suggestions.Row(["flex-wrap gap-1"], content: chips =>
                    {
                        var examples = new[] { "Energy usage", "GDP per capita", "Internet users", "CO2 emissions" };
                        foreach (var example in examples)
                        {
                            var capturedExample = example;
                            chips.Box([
                                "px-2 py-1 rounded-full bg-slate-200/70 hover:bg-slate-300/70",
                                "text-slate-600 text-xs cursor-pointer transition-colors"
                            ], onClick: async () =>
                            {
                                _queryText.Value = capturedExample;
                                await ProcessQueryAsync(capturedExample);
                            }, content: chip =>
                            {
                                chip.Text(text: capturedExample);
                            });
                        }
                    });
                });
            });

            view.Box(["absolute left-6 bottom-6 z-10 pointer-events-none"], content: branding =>
            {
                branding.Column([Styles.GlassCardSubtle, "p-3 pointer-events-auto"], content: brand =>
                {
                    brand.Text(["text-white/90 font-bold text-lg"], "Globe");
                    brand.Text(["text-white/60 text-xs"], "AI-Powered Data Visualization");
                });
            });
        });
    }
}

public class QueryHistoryItem
{
    public string Query { get; set; } = "";
    public string Label { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

internal static class Styles
{
    public const string GlassCard = "bg-white/80 backdrop-blur-md border border-white/40 rounded-xl shadow-lg";
    public const string GlassCardSubtle = "bg-white/60 backdrop-blur-sm border border-white/30 rounded-lg";
}
