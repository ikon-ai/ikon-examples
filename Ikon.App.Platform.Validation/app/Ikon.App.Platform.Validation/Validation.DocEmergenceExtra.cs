using Ikon.Agent.Skills;

// Generated holder for the fences of emergence-guide.md; each region is one fence, verbatim, so the
// compiler judges exactly what a reader copies.
file static class DocEmergenceExtra
{
    // The names the guide's prose hands the reader before the block that uses them.
    private sealed record MyType(string Text);

    private sealed record Draft(string Text);

    private sealed record CoderResponse(string Text);

    private static readonly LLMModel model = LLMModel.Default;
    private static readonly KernelContext ctx = new();
    private static readonly string task = "";
    private static readonly string systemPrompt = "";
    private static readonly EmergePass<CoderResponse> pass = null!;

    private static string WriteFile(string path, string content) => path;

    private static string ReadFile(string path) => path;

    private static string ListFiles() => "";

    public static async Task EmxAwaitableAndStreaming()
    {
        #region docsnippet:emx-awaitable-and-streaming
        // Just get the result (never null; throws EmergenceStoppedException
        // if the run stops or completes without one)
        var result = await Emerge.Run<MyType>(model, pass => { pass.Command = task; });

        // Streaming - observe progress
        await foreach (var ev in Emerge.Run<MyType>(model, ctx, pass => { pass.Command = task; }))
        {
            switch (ev)
            {
                case ModelText<MyType> t: Console.Write(t.Text); break;
                case ToolCallPlanned<MyType> tc: Console.WriteLine($"Calling {tc.Call.Function.Name}"); break;
                case Completed<MyType> done: Console.WriteLine($"Result: {done.Result}"); break;
            }
        }

        // Get the (nullable) result plus the updated KernelContext
        var (withContext, context) = await Emerge.Run<MyType>(model, ctx, pass => { pass.Command = task; }).FinalAsync();

        // Get the result with trace info
        var (withTrace, tracedContext, trace) = await Emerge.Run<MyType>(model, ctx, pass => { pass.Command = task; }).FinalWithTraceAsync();
        #endregion
    }

    public static async Task EmxConfigurationInheritance()
    {
        #region docsnippet:emx-configuration-inheritance
        await Emerge.Refine<Draft>(model, ctx, opt =>
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

    public static async Task EmxToolRegistration()
    {
        #region docsnippet:emx-tool-registration
        await foreach (var ev in Emerge.Run<CoderResponse>(LLMModel.Claude45Sonnet, ctx, pass =>
        {
            pass.AddTool(Tool.Of("write_file", "Write content to a file",
                    ([Description("Workspace-relative path")] string path, string content) => WriteFile(path, content)))
                .AddTool(Tool.Of("read_file", "Read file contents",
                    (string path) => ReadFile(path)))
                .AddTool(Tool.Of("list_files", "List all files",
                    () => ListFiles()));

            pass.Command = "Complete this coding task.";
            pass.MaxIterations = 10;
            pass.MaxToolCalls = 50;
        }))
        {
            if (ev is Completed<CoderResponse> done) { Log.Instance.Info($"{done.Result}"); }
        }
        #endregion
    }

    public static async Task EmxToolRegistration2()
    {
        #region docsnippet:emx-tool-registration-2
        var mcpClient = new McpClient("https://example.com/mcp");
        await mcpClient.ConnectAsync();
        var skill = new McpSkill(mcpClient);

        // As part of a Persona's skill set:
        var persona = new Persona("Assistant", systemPrompt,
            Skills: [Built.Messaging, skill],
            Reasoning: new Reasoning());

        // Or directly on a pass (requires an AgentRunner scope):
        pass.AddTools(skill.Tools().ToArray());
        #endregion
    }

}
