using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class ExtractArticlesShader
{
    public static async Task<News> GenerateAsync(
        string modelName,
        string reasoningEffort,
        string articles,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow.ToString();

        var command = $"""
            You structure markdown text of a scraped news webpage with articles into the provided JSON format which extracts and structures the articles. Do not change any of the original content. The only content that you generate on your own is the Article Id and Summary.

            #ArticlesMarkdown
            The articles markdown:
            {articles}

            #LastModified
            Current UTC time is: {utcNow}.

            #ResponseLanguage
            The response should use the same language as the one used in the articles.
            """;

        var (result, _) = await Emerge.Run<News>(
            LLMModel.Gpt41,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.2f;
                pass.MaxOutputTokens = 4000;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }
}
