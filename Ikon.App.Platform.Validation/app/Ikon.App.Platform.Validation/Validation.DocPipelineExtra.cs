using Ikon.Pipeline;
using Ikon.Pipeline.Items;

// The pipeline guide's remaining worked classes.
//
// `FetchFromGithub` is deliberately absent here: the AGENTS.md template and this guide were showing
// two different versions of the same class, so both now pin the one region in Validation.DocExamples.cs.

#region docsnippet:pipeline-config-secret
[Pipeline]
public class TranscribeAudio(IPipelineHost<TranscribeAudio.Config> host)
{
    public class Config
    {
        public string Language { get; set; } = "en";
    }

    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        string apiKey = host.Secrets["OPENAI_API_KEY"];
        string lang = host.Config.Language;
        // ...
        await Task.CompletedTask;
    }
}
#endregion

#region docsnippet:pipeline-distributed
[Pipeline]
public class DistributedPipeline(IPipelineHost<DistributedPipeline.Config> host)
{
    public class Config
    {
        public int DelayMs { get; set; } = 100;
    }

    public async Task Run(Pipeline<Item>.Branch inputItems)
    {
        var stage1 = inputItems.Transform(item => ProcessorA(item, host.Config.DelayMs));
        var stage2 = stage1.Transform(item => ProcessorB(item, host.Config.DelayMs));
        stage2.Output();
    }

    // Mark processor for remote execution with isRemote: true
    // The version parameter is used for cache invalidation and processor identification
    [Processor(isRemote: true, version: 1)]
    private static async Task<List<Item>> ProcessorA(Item item, int delayMs)
    {
        await Task.Delay(delayMs);
        var content = await item.GetContentAsString();
        content += "->A";
        return [await Item.Create(item, $"{item.Name}.a", content, MimeTypes.TextPlain)];
    }

    [Processor(isRemote: true, version: 1)]
    private static async Task<List<Item>> ProcessorB(Item item, int delayMs)
    {
        await Task.Delay(delayMs);
        var content = await item.GetContentAsString();
        content += "->B";
        return [await Item.Create(item, $"{item.Name}.b", content, MimeTypes.TextPlain)];
    }
}
#endregion

file static class DocPipelineRemote
{
    public static async Task RunRemoteAsync(CancellationToken cancellationToken)
    {
        #region docsnippet:pipeline-run-remote
        var config = new PipelineRunner.Config
        {
            TypeName = typeof(DistributedPipeline).FullName!,
            DllPath = "./bin/Release/MyPipeline.dll",
            EnableRemoteHost = true,
            EnableRemoteClient = true, // Can run host and client in same process
            RabbitMQConnectionString = "host=localhost;port=5672;username=guest;password=guest",
            CachePath = "./shared-cache"
        };

        await PipelineRunner.RunRemote(config, status =>
        {
            Log.Instance.Info($"Processed: {status.ProcessedItemCount}, Failures: {status.ProcessFailureCount}");
        }, cancellationToken);
        #endregion
    }
}

file static class DocPipelineItems
{
    public static void FindJsonOutput(IReadOnlyList<Item> outputItems)
    {
        #region docsnippet:pipeline-first-or-null
        Item? match = outputItems.FirstOrNull(item => item.MimeType == MimeTypes.ApplicationJson);

        if (match is { } found)
        {
            Log.Instance.Info($"Found {found.Name}");
        }
        #endregion
    }
}
