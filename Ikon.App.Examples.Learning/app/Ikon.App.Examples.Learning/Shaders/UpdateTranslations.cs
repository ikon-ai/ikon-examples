using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class UpdateTranslationsShader
{
    public static async Task<Translations> GenerateAsync(
        string modelName,
        string reasoningEffort,
        string language,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = $"""
            You are an expert translator. Your task is to translate text from one language to another as accurately as possible, ensuring the meaning, tone, and nuances of the original text are preserved. Avoid making any changes to the original content.

            #Task
            You have translate system texts from source language code: 'en'
            into language code: '{language}'

            The default values of the texts are available as the default values of items in the JSON response. You are to translate those default values into the specified language. If the default english value has ((VARIABLE)) in it, that variable should remain the same as it is a placeholder to be replaced later. For example, a default value of 'Hello ((NAME))' should be translated to 'Hola ((NAME))' in Spanish. These translations will be used in the menus for a language learning platform. The translations should be accurate and contextually appropriate for the menus for a language learning platform.
            """;

        var (result, _) = await Emerge.Run<Translations>(
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
