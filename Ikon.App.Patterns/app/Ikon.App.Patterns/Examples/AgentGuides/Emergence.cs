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
        await foreach (var ev in Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, new KernelContext(), pass =>
        {
            pass.SystemPrompt = "You are a helpful analyst.";
            pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
            pass.Temperature = 0.7;
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
        var analysis = await Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, pass =>
        {
            pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
            pass.Temperature = 0.3;
        });
        #endregion

        Log.Instance.Debug($"{analysis}");
    }
}

internal sealed partial class AgentGuideExamples
{

    private static async Task<AnalysisResult> DocCancellationAsync(string topic)
    {
        #region example:cancellation-timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var result = await Emerge.Run<AnalysisResult>(LLMModel.Claude46Sonnet, pass =>
        {
            pass.Command = $"Analyze: {topic}\n\nReturn JSON:\n{pass.JsonSchema}";
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

    private static async Task DocBestOfAsync(string prompt)
    {
        #region example:emerge-bestof
        await foreach (var ev in Emerge.BestOf<CreativeResponse>(LLMModel.Claude46Sonnet, new KernelContext(), bo =>
        {
            bo.Command = $"Write a tagline for: {prompt}\n\nReturn JSON:\n{bo.JsonSchema}";
            bo.Count = 3;
            bo.Score = (response, trace) => ScoreResponse(response);
            bo.Candidate(c => { c.Temperature = 0.5 + c.Index * 0.2; });
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
        var (result1, context) = await Emerge.Run<ChatResponse>(LLMModel.Claude46Sonnet, new KernelContext(), pass =>
        {
            pass.SystemPrompt = "You are a friendly assistant.";
            pass.Command = userMessage;
        }).FinalAsync();

        // Second message — pass the returned context so it carries the full conversation history automatically
        var (result2, context2) = await Emerge.Run<ChatResponse>(LLMModel.Claude46Sonnet, context, pass =>
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
            LLMModel.Claude45Sonnet,
            pass =>
            {
                pass.SystemPrompt = """
                    Research the given topic. Return JSON matching the output schema.
                    Be concrete — named entities, dates, numbers. Confidence reflects
                    how grounded your facts are; lower it if you're guessing.
                    """;
                pass.Command = $"Topic: {topic}\nDepth: {depth}";
                pass.Temperature = 0.2;
                pass.MaxIterations = depth;
            }).ResultAsync();
        #endregion

        return brief;
    }
}
