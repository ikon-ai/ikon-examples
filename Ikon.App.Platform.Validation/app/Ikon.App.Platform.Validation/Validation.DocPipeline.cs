using System.Runtime.CompilerServices;
using Ikon.Pipeline;
using Ikon.Pipeline.Items;

// The pipeline guide, as code that compiles. `SimplePipeline` lives in its own file because that
// fence carries the `using` directives, which are only legal at file scope.
//
// The guide declared its pipeline classes `private` at namespace scope, which C# does not allow
// (CS1527) — they are `internal` here and in the guide.

#region docsnippet:pipeline-example-data
internal class ExampleData
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Occupation { get; set; } = string.Empty;
}
#endregion

#region docsnippet:pipeline-advanced
// If a config object is desired, the pipeline class can take in an IPipelineHost<TConfig> parameter
// The user supplies the config either as an object or JSON when running the pipeline
// The config will be accessible via the host.Config property
[Pipeline]
internal class AdvancedPipeline(IPipelineHost<AdvancedPipeline.Config> host)
{
    // The config object is a user-defined POD class
    public class Config
    {
        public int ConfigValue1 { get; set; } = 1;
        public string ConfigValue2 { get; set; } = "ConfigValue";
    }

    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        // Filter items to only those having the "even" tag
        var evenItems = inputItems.Filter(item => item.HasTagsAsync("even"));

        // Filter items to only those having the "odd" tag
        var oddItems = inputItems.Filter(item => item.HasTagsAsync("odd"));

        // Filter items to only those that are objects of type ExampleData
        var objectItems = inputItems.Filter(item => item.IsObjectAsync<ExampleData>());

        // Filter items to only those that are images (based on MIME type)
        var imageItems = inputItems.Filter(item => item.IsImageAsync());

        // All Transform* functions take an expression; the easiest way is to pass a function with parameters
        // The variable values inside the expression are read and used to calculate a hash for the processor call
        // If any of the variable values change, then possible caching for that processor is skipped and it runs
        // If processor name, version, and expression variables are the same as a previous run, cached results are used

        // Process each item separately but in parallel
        evenItems = evenItems.Transform(item => MyProcessor(item, host.Config.ConfigValue2, cancellationToken));

        // Gather items into batches and process each batch in parallel
        // Batch size can be set with maxBatchSize parameter
        oddItems = oddItems.TransformBatch(items => MyBatchProcessor(items, host.Config.ConfigValue2, cancellationToken));

        // Process each item and produce multiple output items as a stream
        var itemToStreamItems = objectItems.TransformStream(item => MyItemToStreamProcessor(item, host.Config.ConfigValue2, cancellationToken));

        // Process multiple input items as a stream and produce multiple output items as a stream
        var streamToStreamItems = oddItems.TransformStream(items => MyStreamToStreamProcessor(items, host.Config.ConfigValue2, cancellationToken));

        // Merge multiple branches into one
        var mergedItems = evenItems.Merge(oddItems, itemToStreamItems, streamToStreamItems);

        // Group items by a key (here process ID) and process each group as a batch
        // Grouping ID can be any string value
        var groupProcessedItems = mergedItems.TransformGroup(item => item.GetProcessIdAsync(), items => MyBatchProcessor(items, host.Config.ConfigValue2, cancellationToken));

        // ForEach can be used to run code for each item without producing any output items
        imageItems.ForEach(async item =>
        {
            Log.Instance.Info($"Image item Name={item.Name}, MimeType={item.MimeType}");
        });

        // All Transform* functions also have a TransformLambda* counterpart that takes a lambda instead of an expression
        // Their use is discouraged as the lambda cannot be analyzed for variable values and thus caching is less effective
        // Also, transparent remote processor handling cannot be used with lambdas
        var doNotUseTransformLambdaItems = inputItems.TransformLambda(async item =>
        {
            return await MyProcessor(item, host.Config.ConfigValue2, cancellationToken);
        });

        // Calling output on any branch outputs those items from the pipeline
        groupProcessedItems.Output();
    }

    [Processor]
    private static async Task<List<Item>> MyProcessor(Item inputItem, string myParameter, CancellationToken cancellationToken)
    {
        var content = await inputItem.GetContentAsString();
        content = $"{content} - Single processed with parameter: {myParameter}";
        var outputItem = await Item.Create(inputItem, $"{inputItem.Name}.processed", content, MimeTypes.TextPlain);

        return [outputItem];
    }

    [Processor]
    private static async Task<List<Item>> MyBatchProcessor(List<Item> inputItems, string myParameter, CancellationToken cancellationToken)
    {
        List<Item> outputItems = [];

        foreach (var item in inputItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await item.GetContentAsString();
            content = $"{content} - Batch processed with parameter: {myParameter}";
            var outputItem = await Item.Create(item, $"{item.Name}.batch_processed", content, MimeTypes.TextPlain);
            outputItems.Add(outputItem);
        }

        return outputItems;
    }

    [Processor]
    private static async IAsyncEnumerable<Item> MyItemToStreamProcessor(Item inputItem, string myParameter, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // It is assumed that the input item is an object of type ExampleData
        var data = await inputItem.GetContentAsObject<ExampleData>();

        for (int i = 0; i < 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await inputItem.GetContentAsString();
            content = $"{content} - Streamed output {i + 1} with parameter {myParameter} for object {data.Name}, Age {data.Age}, Occupation {data.Occupation}";
            var outputItem = await Item.Create(inputItem, $"{inputItem.Name}.stream_processed{i + 1}", content, MimeTypes.TextPlain);
            yield return outputItem;
        }
    }

    [Processor]
    private static async IAsyncEnumerable<Item> MyStreamToStreamProcessor(IAsyncEnumerable<Item> inputItems, string myParameter, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in inputItems.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await item.GetContentAsString();
            content = $"{content} - Stream-to-stream processed with parameter: {myParameter}";
            var outputItem = await Item.Create(item, $"{item.Name}.stream2stream_processed", content, MimeTypes.TextPlain);
            yield return outputItem;
        }
    }
}
#endregion

file static class DocPipelineGuide
{
    public static async Task RunAsync()
    {
        #region docsnippet:pipeline-run
        using var pipelineRunner = new PipelineRunner();
        await pipelineRunner.Initialize<SimplePipeline>();

        List<Item> inputItems = [];

        for (int i = 0; i < 10; i++)
        {
            var item = await Item.CreateInitial($"item{i + 1}", $"Content of item {i + 1}", MimeTypes.TextPlain);
            inputItems.Add(item);
        }

        var outputItems = await pipelineRunner.Run(inputItems);

        foreach (var outputItem in outputItems)
        {
            var content = await outputItem.GetContentAsString();
            Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
        }
        #endregion
    }

    public static async Task RunAsEnumerableAsync()
    {
        #region docsnippet:pipeline-run-enumerable
        using var pipelineRunner = new PipelineRunner();
        await pipelineRunner.Initialize<SimplePipeline>();

        List<Item> inputItems = [];

        for (int i = 0; i < 10; i++)
        {
            var item = await Item.CreateInitial($"item{i + 1}", $"Content of item {i + 1}", MimeTypes.TextPlain);
            inputItems.Add(item);
        }

        await foreach (var outputItem in pipelineRunner.RunAsEnumerable(inputItems))
        {
            var content = await outputItem.GetContentAsString();
            Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
        }
        #endregion
    }

    public static async Task RunWithConfigAsync()
    {
        #region docsnippet:pipeline-run-config
        using var pipelineRunner = new PipelineRunner();

        var pipelineRunnerConfig = new PipelineRunner.Config
        {
            TypeName = typeof(SimplePipeline).FullName!,
            ProcessFailureThreshold = 2,
            DisableMetadataOutput = true
            // Additional options available, such as cache paths, default retry configuration, and remote execution toggles
        };

        await pipelineRunner.Initialize(pipelineRunnerConfig);

        List<Item> inputItems = [];

        for (int i = 0; i < 10; i++)
        {
            var item = await Item.CreateInitial($"item{i + 1}", $"Content of item {i + 1}", MimeTypes.TextPlain);
            inputItems.Add(item);
        }

        var outputItems = await pipelineRunner.Run(inputItems);

        foreach (var outputItem in outputItems)
        {
            var content = await outputItem.GetContentAsString();
            Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
        }
        #endregion
    }

    public static async Task RunCancellableAsync()
    {
        #region docsnippet:pipeline-run-cancel
        using var pipelineRunner = new PipelineRunner();
        await pipelineRunner.Initialize<SimplePipeline>();

        List<Item> inputItems = [];

        for (int i = 0; i < 10; i++)
        {
            var item = await Item.CreateInitial($"item{i + 1}", $"Content of item {i + 1}", MimeTypes.TextPlain);
            inputItems.Add(item);
        }

        List<Item> outputItems = [];
        var cts = new CancellationTokenSource();

        try
        {
            outputItems = await pipelineRunner.Run(inputItems, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log.Instance.Info("Pipeline run was cancelled");
        }

        foreach (var outputItem in outputItems)
        {
            var content = await outputItem.GetContentAsString();
            Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
        }
        #endregion
    }

    public static async Task CreateInitialItemsAsync()
    {
        #region docsnippet:pipeline-create-initial-items
        List<Item> inputItems = [];

        // Create an initial item from a string
        // MIME type specified for small text content as automatic detection may not work well
        string stringContent = "This is a string content";
        inputItems.Add(await Item.CreateInitial("string_item_name", stringContent, mimeTypeOverride: MimeTypes.TextPlain));

        // Create an initial item from a byte array
        // MIME type will be analyzed from the content
        byte[] byteContent = new byte[1024];
        inputItems.Add(await Item.CreateInitial("binary_item_name", byteContent));

        // Create an initial item from a stream
        // MIME type will be analyzed from the content
        await using var stream = new MemoryStream(1024);
        inputItems.Add(await Item.CreateInitial("stream_item_name", stream));

        // Create an initial item from an object (will be serialized to JSON)
        // MIME type will be set automatically
        var exampleData = new ExampleData();
        inputItems.Add(await Item.CreateInitialFromObject("object_item_name", exampleData));
        #endregion
    }

    public static async Task CreateItemsAsync(Item parentItem, Item anotherParentItem)
    {
        #region docsnippet:pipeline-create-items
        List<Item> outputItems = [];

        // Create an item from a string with single parent
        string stringContent = "This is a string content";
        outputItems.Add(await Item.Create(parentItem, $"{parentItem.Name}.name_suffix", stringContent, mimeTypeOverride: MimeTypes.TextPlain));

        // Create an item from a string with multiple parents
        outputItems.Add(await Item.Create([parentItem, anotherParentItem], "full_item_name", stringContent, mimeTypeOverride: MimeTypes.TextPlain));

        // Create an item from a string without any parents (not recommended, but possible)
        outputItems.Add(await Item.Create([], "full_item_name", stringContent, mimeTypeOverride: MimeTypes.TextPlain));

        // Create an item from a byte array
        // MIME type will be analyzed from the content
        byte[] byteContent = new byte[1024];
        outputItems.Add(await Item.Create(parentItem, $"{parentItem.Name}.name_suffix", byteContent));

        // Create an item from a stream
        // MIME type will be analyzed from the content
        await using var stream = new MemoryStream(1024);
        outputItems.Add(await Item.Create(parentItem, $"{parentItem.Name}.name_suffix", stream));

        // Create an item from an object (will be serialized to JSON)
        // MIME type will be set automatically
        var exampleData = new ExampleData();
        outputItems.Add(await Item.CreateFromObject(parentItem, $"{parentItem.Name}.name_suffix", exampleData));
        #endregion
    }

    public static async Task ReadContentAsync(Item parentItem)
    {
        #region docsnippet:pipeline-read-content
        var stringItem = await Item.Create(parentItem, $"{parentItem.Name}.string", "This is a string content", mimeTypeOverride: MimeTypes.TextPlain);
        var byteItem = await Item.Create(parentItem, $"{parentItem.Name}.bytes", new byte[1024]);
        await using var stream = new MemoryStream(1024);
        var streamItem = await Item.Create(parentItem, $"{parentItem.Name}.stream", stream);
        var exampleData = new ExampleData { Name = "John Doe", Age = 30, Occupation = "Engineer" };
        var objectItem = await Item.CreateFromObject(parentItem, $"{parentItem.Name}.object", exampleData);

        // Get item content as string
        string stringContent = await stringItem.GetContentAsString();
        Log.Instance.Info($"String content: {stringContent}");

        // Get item content as byte array
        byte[] byteContent = await byteItem.GetContentAsBytes();
        Log.Instance.Info($"Byte content length: {byteContent.Length}");

        // Get item content as stream
        await using Stream streamContent = await streamItem.GetContentAsStream();
        Log.Instance.Info($"Stream content length: {streamContent.Length}");

        // Get item content as deserialized object
        ExampleData objectContent = await objectItem.GetContentAsObject<ExampleData>();
        Log.Instance.Info($"Object content: Name={objectContent.Name}, Age={objectContent.Age}, Occupation={objectContent.Occupation}");
        #endregion
    }

    public static async Task LocalFileAsync(Item parentItem)
    {
        #region docsnippet:pipeline-local-file
        var sourceItem = await Item.Create(parentItem, $"{parentItem.Name}.bytes", new byte[1024]);

        // Copy any item to a temporary local file system file
        // Useful for external libraries that can only read from a file path
        // The local file will be automatically deleted when disposed
        using (var localFile = await sourceItem.GetLocalFile())
        {
            Log.Instance.Info($"Local file, Path={localFile.Path}, MimeType={localFile.MimeType}");
        }

        // Create a temporary local file path for writing
        // You can give this path to external libraries to write content to
        // An item can then be created from the local file
        // The file will be automatically deleted when disposed
        using (var localFile = new LocalFile(MimeTypes.TextPlain))
        {
            await File.WriteAllTextAsync(localFile.Path, "This is some text content");
            var outputItem = await Item.Create(parentItem, "my_item", localFile);
        }
        #endregion
    }

    public static async Task RunAdvancedAsync()
    {
        #region docsnippet:pipeline-run-advanced
        using var pipelineRunner = new PipelineRunner();

        var myPipelineConfig = new AdvancedPipeline.Config
        {
            ConfigValue1 = 42,
            ConfigValue2 = "The answer"
        };

        await pipelineRunner.Initialize<AdvancedPipeline>(
            userConfigInstance: myPipelineConfig, // Give the user config instance to the pipeline runner
            usePersistentCache: true // This Initialize overload has common useful options (for full control, see the overload taking PipelineRunner.Config)
        );

        List<Item> inputItems = [];

        for (int i = 0; i < 10; i++)
        {
            List<string> tags = i % 2 == 0 ? ["even"] : ["odd"];
            var item = await Item.CreateInitial($"item{i}", $"Content of item {i}", MimeTypes.TextPlain, tags);
            inputItems.Add(item);
        }

        inputItems.Add(await Item.CreateInitialFromObject("object_item", new ExampleData { Name = "Alice", Age = 28, Occupation = "Designer" }));
        inputItems.Add(await Item.CreateInitial("image_item", new byte[2048], MimeTypes.ImagePng));

        var outputItems = await pipelineRunner.Run(inputItems);

        foreach (var outputItem in outputItems)
        {
            var content = await outputItem.GetContentAsString();
            Log.Instance.Info($"Output item, Name={outputItem.Name}, MimeType={outputItem.MimeType}, Content='{content}'");
        }
        #endregion
    }
}
