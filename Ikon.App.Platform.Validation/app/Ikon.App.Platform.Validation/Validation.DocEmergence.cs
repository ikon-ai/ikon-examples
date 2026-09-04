using Ikon.AI.Emergence.Tree;
using Ikon.AI.Emergence.Structured;

// The Emergence guide, as code that compiles.
//
// Three of its fences are deliberate synopses — `pass => { ... }` and `{ ... }` stand for a body the
// surrounding prose is not about — and stay hand-written; the rest are real calls pinned here.

file static class DocEmergenceGuide
{
    #region docsnippet:emergence-analysis-result
    public class AnalysisResult
    {
        public string Summary { get; set; } = "";
        public List<string> KeyPoints { get; set; } = [];
        public float Confidence { get; set; }
    }
    #endregion

    #region docsnippet:emergence-classification
    public class Classification
    {
        public string Category { get; set; } = "";
        public float Confidence { get; set; }
    }
    #endregion

    #region docsnippet:emergence-tool-request
    public sealed record CreateEventRequest(
        [property: Description("Event title shown in the calendar")] string Title,
        [property: Description("ISO-8601 start time")] string Start,
        [property: Description("ISO-8601 end time")] string End,
        [property: Description("Optional location")] string? Location,
        [property: Description("Attendee emails")] string[]? Attendees);
    #endregion

    private sealed record ChatResponse(string Reply);

    private sealed record Answer(float Confidence, float Correctness, float Brevity);

    private sealed record ChunkSummary(string Text);

    private sealed record FinalReport(string Text);

    private sealed record Implementation(string Code);

    private sealed record Analysis(string Text);

    private sealed record CoderResponse(string Text);

    private static string SearchWeb(string query) => query;

    private static Task<string?> ValidateCodeAsync(string code) => Task.FromResult<string?>(null);

    private static string WriteFile(string path, string content) => path;

    private static string ReadFile(string path) => path;

    private static string ListFiles() => "";

    private static string CreateEvent(CreateEventRequest request) => request.Title;

    public static async Task StructuredAsync(LLMModel model)
    {
        #region docsnippet:emergence-structured
        var result = await Emerge.Run<AnalysisResult>(model, pass =>
        {
            pass.Command = "Analyze the following text and provide structured output.";
        });

        // result.Summary, result.KeyPoints, result.Confidence are typed
        #endregion

        Log.Instance.Debug($"{result}");
    }

    public static async Task ScopedOptionsAsync(LLMModel model, KernelContext ctx)
    {
        #region docsnippet:emergence-scoped-options
        await Emerge.Refine<Implementation>(model, ctx, opt =>
        {
            // Parent settings - inherited by all scopes
            opt.Temperature = 0.3f;
            opt.SystemPrompt = "You are an expert...";

            opt.Initial(s =>
            {
                // Only set what's different
                s.Command = "Generate initial draft.";
            });

            opt.Refinement(s =>
            {
                s.Temperature = 0.2f;  // Override for refinement
                s.Command = "Improve the draft.";
            });
        });
        #endregion
    }

    public static async Task AskAsync()
    {
        #region docsnippet:emergence-ask
        // String response
        string reply = await Emerge.AskAsync("Summarize this in one sentence: ...");

        // Structured response (T must be a reference type)
        Classification result = await Emerge.AskAsync<Classification>(
            "Classify this support ticket: \"My laptop won't turn on\"");

        // Explicit model override
        string harder = await Emerge.AskAsync("Hard reasoning question", LLMModel.Claude45Sonnet);
        #endregion

        Log.Instance.Debug($"{reply} {result} {harder}");
    }

    public static async Task WithToolsAsync()
    {
        #region docsnippet:emergence-with-tools
        var result = await Emerge.Run<ChatResponse>(LLMModel.Claude45Sonnet, pass =>
        {
            pass.SystemPrompt = "You are a helpful assistant.";
            pass.Command = "Answer the user's question.";
            pass.Temperature = 0.7;
            pass.MaxIterations = 5;
            pass.AddTool(Tool.Of("search_web", "Search the web for information",
                (string query) => SearchWeb(query)));
        });
        #endregion

        Log.Instance.Debug($"{result}");
    }

    public static async Task FinalAsync(KernelContext context)
    {
        #region docsnippet:emergence-final
        var (result, ctx) = await Emerge.Run<ChatResponse>(LLMModel.Claude45Sonnet, context, pass =>
        {
            pass.Command = "Answer the user's question.";
        }).FinalAsync();
        #endregion

        Log.Instance.Debug($"{result} {ctx}");
    }

    public static async Task BestOfAsync(KernelContext ctx)
    {
        #region docsnippet:emergence-bestof
        var best = await Emerge.BestOf<Answer>(LLMModel.Claude45Sonnet, ctx, opt =>
        {
            opt.Count = 5;
            opt.Command = "Solve this problem step by step.";
            opt.Score = (answer, trace) => answer.Confidence * (1f / trace.Duration.TotalSeconds);

            opt.Candidate(c =>
            {
                c.Temperature = 0.7 + 0.1 * c.Index;  // Vary temperature per candidate
                c.Seed = 1000 + c.Index;
            });
        });
        #endregion

        Log.Instance.Debug($"{best}");
    }

    public static async Task RubricAsync(KernelContext ctx)
    {
        #region docsnippet:emergence-rubric
        var rubric = new ScoreBreakdownBuilder<Answer>()
            .Metric("correctness", 3, a => a.Correctness)
            .Metric("brevity", 1, a => a.Brevity);

        var best = await Emerge.BestOf<Answer>(LLMModel.Claude45Sonnet, ctx, opt =>
        {
            opt.Command = "Solve this problem step by step.";
            opt.ScoreDetailed = (answer, _) => rubric.Score(answer);

            opt.EnableCritic = true;
            opt.BuildCriticFeedback = (answer, breakdown) =>
                $"Weakest axis: {breakdown!.Weakest!.Name}. Improve it:\n{breakdown.FormatBreakdown()}";
        });
        #endregion

        Log.Instance.Debug($"{best}");
    }

    public static async Task MapReduceAsync(KernelContext ctx, IReadOnlyList<string> documents)
    {
        #region docsnippet:emergence-mapreduce
        var report = await Emerge.MapReduce<string, ChunkSummary, FinalReport>(LLMModel.Claude45Sonnet, ctx, opt =>
        {
            opt.Chunks = documents;
            opt.MaxParallel = 8;

            opt.Map(m =>
            {
                m.Temperature = 0.5;
                m.Command = "Summarize the key points from this document chunk.";
            });

            opt.Reduce(r =>
            {
                r.Temperature = 0.3;
                r.Command = "Combine all chunk summaries into a comprehensive final report.";
            });
        });
        #endregion

        Log.Instance.Debug($"{report}");
    }

    public static async Task RefineAsync(KernelContext ctx)
    {
        #region docsnippet:emergence-refine
        var final = await Emerge.Refine<Implementation>(LLMModel.Claude45Sonnet, ctx, opt =>
        {
            opt.MaxRefinements = 3;

            opt.Initial(s =>
            {
                s.Command = "Write initial implementation of the feature.";
            });

            opt.Refinement(s =>
            {
                s.Command = "Improve the code based on the issues found.";
            });

            // Async validation - continue refining while there are errors
            opt.ShouldContinue = async (result, trace) =>
            {
                var error = await ValidateCodeAsync(result.Code);
                return error != null;
            };
        });
        #endregion

        Log.Instance.Debug($"{final}");
    }

    public static async Task EnsembleAsync(KernelContext ctx)
    {
        #region docsnippet:emergence-ensemble
        var merged = await Emerge.EnsembleMerge<Analysis>(LLMModel.Claude45Sonnet, ctx, opt =>
        {
            opt.SolverCount = 4;
            opt.MaxParallel = 4;

            opt.Solver(s =>
            {
                s.Temperature = 0.6 + 0.15 * s.Index;  // Varying temperatures
                s.Command = "Analyze this data from your unique perspective.";
            });

            opt.Merger(m =>
            {
                m.Temperature = 0.3;
                m.Command = "Synthesize all analyses into a comprehensive unified result.";
            });
        });
        #endregion

        Log.Instance.Debug($"{merged}");
    }

    public static async Task TreeSearchAsync(KernelContext ctx, string documentContent)
    {
        #region docsnippet:emergence-tree-search
        // Step 1: Build a tree index from content
        TreeIndex? index = null;
        await foreach (var ev in TreeIndex.BuildAsync(LLMModel.Claude45Sonnet, documentContent,
            new TreeIndexOptions { MaxDepth = 4, GenerateSummaries = true }))
        {
            if (ev is Completed<TreeIndex> done)
            {
                index = done.Result;
            }
        }

        // Step 2: Search the tree
        TreeSearchResult result = await Emerge.TreeSearch(LLMModel.Claude45Sonnet, ctx, opt =>
        {
            opt.Index = index;
            opt.Query = "How does authentication work?";
            opt.MaxSteps = 10;
            opt.MaxResults = 3;

            // The executor owns the navigator's Command and MaxIterations and overwrites them
            // every step — configure only model-level knobs (Model, Temperature, MaxOutputTokens)
            opt.Navigator(n =>
            {
                n.Temperature = 0.2;
            });
        });

        // result.Sections is a List<FoundSection>, each with NodeId, Path, Content, Relevance, Page;
        // result.ReasoningTrace carries the navigator's final reasoning
        #endregion

        Log.Instance.Debug($"{result}");
    }

    private static void AddRequestTool(EmergePass<CoderResponse> pass)
    {
        #region docsnippet:emergence-tool-request-use
        pass.AddTool(Tool.Of("create_event", "Create a calendar event",
            (CreateEventRequest request) => CreateEvent(request)));
        #endregion
    }

    public static void StructuredTags(string content)
    {
        #region docsnippet:emergence-structured-tags
        var parsed = StructuredTagParser.Parse(content, "reasoning", "answer");

        // parsed.PlainText — text outside tags
        // parsed.Blocks — list of ParsedBlock (TagName, Content, StartIndex, EndIndex)

        // Utility methods
        bool has = StructuredTagParser.HasTag(content, "reasoning");
        string? text = StructuredTagParser.GetTagContent(content, "answer");
        #endregion

        Log.Instance.Debug($"{parsed} {has} {text}");
    }

    public static void ContextHelpers(KernelContext ctx)
    {
        #region docsnippet:emergence-context-helpers
        bool hasFn = ctx.HasFunctionResults();
        var results = ctx.GetFunctionResults(take: 10);  // IReadOnlyList<FunctionResultPart>
        var calls = ctx.GetFunctionCalls(take: 10);       // IReadOnlyList<FunctionCall>

        // Keep only the last N message blocks (never starting on an orphan Model
        // or FunctionResult turn); instructions and other fields are preserved
        var trimmed = ctx.TrimToLastMessages(take: 20, skipLast: 0);
        #endregion

        Log.Instance.Debug($"{hasFn} {results.Count} {calls.Count} {trimmed}");
    }
}
