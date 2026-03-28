using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class GenerateExerciseShader
{
    public static async Task<Exercise> GenerateAsync(
        string modelName,
        string reasoningEffort,
        UserState userState,
        Content? content,
        string? skillId,
        string? levelId,
        string? subSkillId,
        LearningTheme? theme,
        string? scenario,
        ExerciseType exerciseType,
        ExerciseSource exerciseSource,
        ExerciseCategory exerciseCategory,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = BuildCommand(userState, theme, scenario, exerciseType, exerciseSource, exerciseCategory);

        var (result, _) = await Emerge.Run<Exercise>(
            LLMModel.Gpt41,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.7f;
                pass.MaxOutputTokens = 18000;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }

    private static string BuildCommand(
        UserState userState,
        LearningTheme? theme,
        string? scenario,
        ExerciseType exerciseType,
        ExerciseSource exerciseSource,
        ExerciseCategory exerciseCategory)
    {
        var sb = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(scenario))
        {
            sb.AppendLine("#Task");
            sb.AppendLine($"You generate an exercise based on the scenario given by the user. The scenario is: '{scenario}'.");
        }
        else
        {
            sb.AppendLine("#Task");
            sb.AppendLine("You generate an exercise based on a random scenario.");
        }

        sb.AppendLine();

        if (theme != null)
        {
            sb.AppendLine("#TargetTheme");
            sb.AppendLine($"The theme for the exercise is '{theme.Description.En}'.");
            sb.AppendLine($"The theme description is '{theme.DetailedDescription.En}'.");
            sb.AppendLine();
        }

        sb.AppendLine("#ExerciseType");
        sb.AppendLine($"The exercise type should be {exerciseType}.");
        sb.AppendLine();

        sb.AppendLine("#Instructions");
        if (exerciseType == ExerciseType.SimpleQA)
        {
            sb.AppendLine("Clear and direct questions should be generated. Should be a minimum of 5 questions and maximum of 10 questions.");
        }
        else
        {
            sb.AppendLine("Clear and progressive sub goals should be provided. Should be a minimum of 3 and a maximum of 10 sub goals generated.");
        }
        sb.AppendLine();

        sb.AppendLine("#LanguageLevel");
        sb.AppendLine($"The user is currently at {userState.CurrentLanguageLevel} language level and is aiming to achieve {userState.TargetLanguageLevel} language level.");
        sb.AppendLine($"The target language is {userState.TargetLanguage}.");
        sb.AppendLine();

        sb.AppendLine("#Source");
        sb.AppendLine($"The exercise source is '{exerciseSource}'.");
        sb.AppendLine();

        sb.AppendLine("#Category");
        sb.AppendLine($"The exercise category is '{exerciseCategory}'.");
        sb.AppendLine();

        if (exerciseType == ExerciseType.Conversational)
        {
            sb.AppendLine("#RolesUniqueness");
            sb.AppendLine("The roles you generate for the user and AI should be very distinctive and not very similar to each other.");
            sb.AppendLine();
        }

        sb.AppendLine("#AudioPreset");
        sb.AppendLine("This refers to environment which the scenario takes place.");
        sb.AppendLine("Valid audio presets are: Normal, Cathedral, School, Airport, Cafe, Home, Bedroom.");
        sb.AppendLine("If the scenario's environment isn't found, use 'Normal'.");

        return sb.ToString();
    }
}
