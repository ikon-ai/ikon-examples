using Ikon.AI.Emergence;
using Ikon.AI.Kernel;
using Ikon.App.Examples.Learning.DataModels;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class GenerateScenariosShader
{
    public static async Task<GeneratedScenarios> GenerateAsync(
        string modelName,
        string reasoningEffort,
        string themeName,
        string themeDescription,
        string targetLanguage,
        string languageLevel,
        List<Scenario>? existingScenarios = null,
        int count = 5,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = BuildCommand(themeName, themeDescription, targetLanguage, languageLevel, existingScenarios, count);

        var (result, _) = await Emerge.Run<GeneratedScenarios>(
            LLMModel.Gpt41,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.7f;
                pass.MaxOutputTokens = 4000;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }

    private static string BuildCommand(
        string themeName,
        string themeDescription,
        string targetLanguage,
        string languageLevel,
        List<Scenario>? existingScenarios,
        int count)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"You are a language learning exercise designer creating practice scenarios for {targetLanguage} learners at the {languageLevel} level.");
        sb.AppendLine();

        sb.AppendLine("#Theme");
        sb.AppendLine($"Name: {themeName}");
        sb.AppendLine($"Description: {themeDescription}");
        sb.AppendLine();

        if (existingScenarios?.Count > 0)
        {
            sb.AppendLine("#Existing Scenarios (do NOT repeat these)");
            foreach (var scenario in existingScenarios)
            {
                sb.AppendLine($"- {scenario.Name}: {scenario.Description}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("#Task");
        sb.AppendLine($"Generate {count} unique, practical conversation scenarios for the theme above. Each scenario should:");
        sb.AppendLine();
        sb.AppendLine("1. Be realistic and likely to occur in everyday life");
        sb.AppendLine($"2. Be appropriate for {languageLevel} level learners");
        sb.AppendLine("3. Focus on practical communication skills");
        sb.AppendLine("4. Be clearly different from existing scenarios");
        sb.AppendLine("5. Include a specific setting and context");
        sb.AppendLine();

        sb.AppendLine($"#Guidelines for {languageLevel} level:");
        if (languageLevel is "A1" or "A1.1" or "A1.2" or "A1.3")
        {
            sb.AppendLine("- Use very basic language with simple greetings, questions, and answers");
            sb.AppendLine("- Focus on highly familiar situations with predictable exchanges");
            sb.AppendLine("- Keep scenarios short and straightforward");
        }
        else if (languageLevel is "A2" or "A2.1" or "A2.2")
        {
            sb.AppendLine("- Use simple, routine language for everyday tasks");
            sb.AppendLine("- Include basic vocabulary and clear, structured dialogue");
            sb.AppendLine("- Allow for slightly more complex interactions");
        }
        else if (languageLevel is "B1" or "B1.1")
        {
            sb.AppendLine("- Design conversations with moderate complexity");
            sb.AppendLine("- Encourage asking and answering questions beyond rehearsed phrases");
            sb.AppendLine("- Include some unexpected elements in the conversation");
        }
        else
        {
            sb.AppendLine("- Use natural language with broader vocabulary");
            sb.AppendLine("- Include nuanced expressions and spontaneous exchanges");
            sb.AppendLine("- Allow for more complex conversational dynamics");
        }

        return sb.ToString();
    }
}
