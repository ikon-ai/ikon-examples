using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class CreateTranslationShader
{
    public static async Task<Translation> GenerateAsync(
        string modelName,
        string reasoningEffort,
        UserState userState,
        string message,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = $"""
            Translate the following into {userState.PreferredLanguage} (This language could also be in language code form like en-US, fi, etc): {message}.

            If the language is Chinese, Japanese, Arabic or Korean or other non Latin alphabet language, add also word by word transliteration of the text.
            Set TransliteratedMessage empty in case transliteration is not needed (not needed for languages that use the latin or extended latin alphabet, like Finnish).
            """;

        var (result, _) = await Emerge.Run<Translation>(
            LLMModel.Gpt41Mini,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.2f;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }
}
