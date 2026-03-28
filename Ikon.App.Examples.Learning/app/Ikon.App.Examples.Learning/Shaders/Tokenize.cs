using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class TokenizeShader
{
    public static async Task<Tokenized> GenerateAsync(
        string modelName,
        string reasoningEffort,
        string text,
        string targetLanguage,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = $"""
            Tokenize the following {targetLanguage} sentence into individual words or meaningful tokens. For languages like Chinese, Japanese, or Thai that don't use spaces between words, properly segment the text into individual words or morphemes.

            Text to tokenize:
            {text}

            Return an array of words/tokens in their order of appearance. Keep punctuation attached to words where appropriate for the language.
            """;

        var (result, _) = await Emerge.Run<Tokenized>(
            LLMModel.Gpt41Mini,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.1f;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }
}
