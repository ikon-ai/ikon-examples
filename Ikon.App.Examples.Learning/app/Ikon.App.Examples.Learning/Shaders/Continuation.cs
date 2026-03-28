using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class ContinuationShader
{
    public static async Task<Hint> GenerateAsync(
        string modelName,
        string reasoningEffort,
        UserState userState,
        Exercise exercise,
        string conversationHistory,
        string currentAiQuestion,
        string previousHint,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = BuildCommand(userState, exercise, conversationHistory, currentAiQuestion, previousHint);

        var (result, _) = await Emerge.Run<Hint>(
            LLMModel.Gpt41Mini,
            new KernelContext(),
            pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.5f;
            },
            cancellationToken
        ).FinalAsync();

        return result;
    }

    private static string BuildCommand(
        UserState userState,
        Exercise exercise,
        string conversationHistory,
        string currentAiQuestion,
        string previousHint)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("You are a helpful language learning assistant providing hints to a student.");
        sb.AppendLine();

        sb.AppendLine("User Information:");
        sb.AppendLine($"- Target Language: {userState.TargetLanguage}");
        sb.AppendLine($"- Current Level: {userState.CurrentLanguageLevel}");
        sb.AppendLine($"- Preferred Language: {userState.PreferredLanguage}");
        sb.AppendLine();

        sb.AppendLine("Exercise Information:");
        sb.AppendLine($"- Exercise Name: {exercise.Name}");
        sb.AppendLine($"- Scenario: {exercise.Scenario}");
        sb.AppendLine($"- User's Role: {exercise.Roles?.User?.Role}");

        if (exercise.Roles?.User?.SubGoals?.Count > 0)
        {
            sb.AppendLine("- Current Sub-Goals:");
            foreach (var sg in exercise.Roles.User.SubGoals)
            {
                var optional = sg.Optional ? " (Optional)" : "";
                sb.AppendLine($"  {sg.SubGoalId}. {sg.Description}{optional}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Conversation So Far:");
        sb.AppendLine(conversationHistory);
        sb.AppendLine();

        sb.AppendLine($"Current AI Question/Prompt: {currentAiQuestion}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(previousHint))
        {
            sb.AppendLine($"Previous Hint Given: {previousHint}");
            sb.AppendLine("The user is asking for additional help, provide a more detailed hint or alternative approach.");
            sb.AppendLine();
        }

        sb.AppendLine("Generate a helpful hint that:");
        sb.AppendLine($"1. Is in {userState.TargetLanguage} with {userState.PreferredLanguage} explanation");
        sb.AppendLine("2. Doesn't give away the complete answer");
        sb.AppendLine($"3. Provides a starting phrase or vocabulary help appropriate for {userState.CurrentLanguageLevel} level");
        sb.AppendLine("4. Includes a brief explanation of why this approach might work");

        return sb.ToString();
    }
}
