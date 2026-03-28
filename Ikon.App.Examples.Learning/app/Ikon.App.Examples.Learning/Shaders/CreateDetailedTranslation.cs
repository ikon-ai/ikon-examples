using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class CreateDetailedTranslationShader
{
    public static async Task<DetailedTranslationResult> GenerateAsync(
        string modelName,
        string reasoningEffort,
        UserState userState,
        string word,
        string sentenceContext,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = $"""
            You are a language learning assistant providing detailed word-by-word translation analysis.

            The user is learning {userState.TargetLanguage} and their preferred language is {userState.PreferredLanguage}.
            Their current language level is {userState.CurrentLanguageLevel}.

            Analyze the following word in context:
            Word: {word}
            Sentence context: {sentenceContext}

            Provide detailed linguistic information including:
            1. WordTranslation: The original word, its transliteration (if the language uses non-Latin script), and translation
            2. WordInformation: The basic/dictionary form, part of speech, meaning, how it's used in this context, and pronunciation guide
            3. Examples: 2-3 example sentences using this word with translations
            4. Explanation: A brief explanation of any grammar rules, conjugations, or cultural context relevant to this word

            Make the explanation appropriate for their {userState.CurrentLanguageLevel} level.
            """;

        var (result, _) = await Emerge.Run<DetailedTranslationResult>(
            LLMModel.Gpt41,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.3f;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }
}
