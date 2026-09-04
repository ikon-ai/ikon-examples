// Generated holder for the fences of ikon-ai-library-overview.md; each region is one fence, verbatim, so the
// compiler judges exactly what a reader copies.
file static class DocAiDirect
{

    public static async Task AidLlm()
    {
        #region docsnippet:aid-llm
        var context = new KernelContext();
        context = context.Add(new Instruction(InstructionType.Context, "You are a helpful assistant that helps to summarize product release notes."));
        context = context.Add(new MessageBlock(MessageBlockRole.User, "Summarise the latest release highlights. Here are the notes: ..."));

        await foreach (var llmEvent in Emerge.Generate(LLMModel.Gpt5Mini, context, regions: [ModelRegion.Eu]))
        {
            Log.Instance.Info($"{llmEvent.Source} | {llmEvent}");
        }

        var stringResult = await Emerge.Generate(LLMModel.Gpt5Mini, context).AsStringAsync();
        Log.Instance.Info($"String result: {stringResult}");
        #endregion
    }

    public static async Task AidCustomModelEndpoints()
    {
        #region docsnippet:aid-custom-model-endpoints
        CustomModels.Instance.Register(new CustomLLMModel
        {
            Name = "my-model",
            EndpointUrl = "http://gpu-box:8000/v1/chat/completions",
            Api = CustomLLMApi.OpenAICompletions,
            ApiModelName = "Qwen/Qwen2.5-32B-Instruct",
            ApiKey = "sk-local-123",           // omit for keyless endpoints (e.g. local Ollama)
            ContextWindowSize = 32768,
            MaxOutputTokens = 8192,            // omit when the endpoint caps nothing
            SupportsJsonSchema = true,
        });

        var reply = await Emerge.AskAsync("Hello", "my-model");

        await foreach (var llmEvent in Emerge.Generate("my-model", new KernelContext()))
        {
            Log.Instance.Info($"{llmEvent}");
        }
        #endregion
    }
}
