namespace Ikon.App.Patterns.Examples;

#region example:pipeline-example
[Pipeline(name: "example")]
public class ExamplePipeline
{
    public async Task Run(Pipeline<Item>.Branch inputItems, CancellationToken cancellationToken)
    {
        var outputItems = inputItems.Transform(item => Process(item, cancellationToken));
        outputItems.Output();
    }

    [Processor]
    private static async Task<List<Item>> Process(Item inputItem, CancellationToken cancellationToken)
    {
        var text = await inputItem.GetContentAsString();
        var outputItem = await Item.Create(inputItem, $"{inputItem.Name}.result", $"Processed: {text}", MimeTypes.TextPlain);
        return [outputItem];
    }
}
#endregion
