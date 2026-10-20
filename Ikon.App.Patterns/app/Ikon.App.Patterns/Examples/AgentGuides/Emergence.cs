namespace Ikon.App.Patterns.Examples;

#region example:emerge-result-type
// Both sealed class and record work as result types
public sealed class AnalysisResult
{
    public string Summary { get; set; } = "";
    public List<string> KeyPoints { get; set; } = [];
}

// Records also work:
// public record AnalysisResult(string Summary, List<string> KeyPoints);
#endregion

file static class EmergeBasicExamples
{
    public static async Task RunAsync(string topic)
    {
        #region example:emerge-basic
        // Streaming (observe each event)
        await foreach (var ev in Emerge.Run<AnalysisResult>(LLMModel.Claude55Sonnet, new KernelContext(), pass =>
        {
            pass.SystemPrompt = "You are a helpful analyst.";
            pass.Command = $"Analyze: {topic}";
            pass.ReasoningEffort = ReasoningEffort.Low;
            pass.MaxOutputTokens = 32000;
            pass.MaxIterations = 5;
        }))
        {
            if (ev is Completed<AnalysisResult> completed)
            {
                var result = completed.Result;
            }
        }

        // Direct result (no streaming) — awaiting the run returns non-null T or throws EmergenceStoppedException
        var analysis = await Emerge.Run<AnalysisResult>(LLMModel.Claude55Sonnet, pass =>
        {
            pass.Command = $"Analyze: {topic}";
        });
        #endregion

        Log.Instance.Debug($"{analysis}");
    }
}

internal sealed partial class AgentGuideExamples
{
    // The file-local example class, reachable from the demo's button only through this file
    private static Task DocEmergeBasicAsync(string topic) => EmergeBasicExamples.RunAsync(topic);

    // The tools example configures a pass; this runs one with it
    private static async Task<string> DocEmergeToolsRunAsync() =>
        await Emerge.Run<string>(LLMModel.Claude55Sonnet, pass =>
        {
            pass.Command = "What are the latest numbers on tide pools?";
            DocEmergeTools(pass);
            DocEmergeToolsDescribed(pass);
        });

    private static async Task<AnalysisResult> DocCancellationAsync(string topic)
    {
        #region example:cancellation-timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var result = await Emerge.Run<AnalysisResult>(LLMModel.Claude55Sonnet, pass =>
        {
            pass.Command = $"Analyze: {topic}";
        }, cts.Token);
        #endregion

        return result;
    }

    private static void DocEmergeTools(EmergePass<string> pass)
    {
        #region example:emerge-tools
        pass.AddTool(Tool.Of("search", "Search the web", (string query) => SearchWeb(query)))
            .AddTool(Tool.Of("get_data", "Get statistics", (string topic) => GetData(topic)));
        pass.MaxToolCalls = 10;
        #endregion
    }

    private static void DocEmergeToolsDescribed(EmergePass<string> pass)
    {
        #region example:emerge-tools-described
        pass.AddTool(Tool.Of("find_sounds", "Find sound effects in the catalogue",
            ([Description("What the sound is, such as 'rifle shot' or 'rain on a roof'")] string query,
             [Description("How many results to return, 1 to 20; 5 when left out")] int? limit,
             [Description("Only sounds of this category; every category when left out")] string? category) =>
                FindSounds(query, limit ?? 5, category)));
        #endregion
    }

    private static string FindSounds(string query, int limit, string? category) => $"{query} {limit} {category}";

    private static async Task DocBestOfAsync(string prompt)
    {
        #region example:emerge-bestof
        await foreach (var ev in Emerge.BestOf<CreativeResponse>(LLMModel.Claude55Sonnet, new KernelContext(), bo =>
        {
            bo.Command = $"Write a tagline for: {prompt}";
            bo.Count = 3;
            bo.Score = (response, trace) => ScoreResponse(response);
            bo.Candidate(c => { c.SystemPrompt = c.Index == 0 ? "Keep it short and plain." : "Be playful."; });
        }))
        {
            if (ev is Completed<CreativeResponse> completed) { /* best candidate */ }
        }
        #endregion
    }

    private static async Task DocConversationHistoryAsync(string userMessage, string nextUserMessage)
    {
        #region example:conversation-history
        // First user message — start with a fresh KernelContext
        var (result1, context) = await Emerge.Run<ChatResponse>(LLMModel.Claude55Sonnet, new KernelContext(), pass =>
        {
            pass.SystemPrompt = "You are a friendly assistant.";
            pass.Command = userMessage;
        }).FinalAsync();

        // Second message — pass the returned context so it carries the full conversation history automatically
        var (result2, context2) = await Emerge.Run<ChatResponse>(LLMModel.Claude55Sonnet, context, pass =>
        {
            pass.Command = nextUserMessage;
        }).FinalAsync();
        #endregion

        _ = (result1, result2, context2);
    }

    public async Task<TopicBrief> Research(string topic, int depth = 2)
    {
        #region example:emerge-typed-run
        var brief = await Emerge.Run<TopicBrief>(
            LLMModel.Claude55Sonnet,
            pass =>
            {
                pass.SystemPrompt = """
                    Research the given topic. Return JSON matching the output schema.
                    Be concrete — named entities, dates, numbers. Confidence reflects
                    how grounded your facts are; lower it if you're guessing.
                    """;
                pass.Command = $"Topic: {topic}\nDepth: {depth}";
                pass.MaxIterations = depth;
            });
        #endregion

        return brief;
    }
}
