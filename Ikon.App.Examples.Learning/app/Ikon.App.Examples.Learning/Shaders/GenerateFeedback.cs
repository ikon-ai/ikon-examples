using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class GenerateFeedbackShader
{
    public static async Task<UserScore> GenerateAsync(
        string modelName,
        string reasoningEffort,
        UserState userState,
        Exercise exercise,
        string aiQuestion,
        string userAnswer,
        int subGoalId,
        int questionId,
        int helpCount,
        bool audioInput,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = BuildCommand(userState, exercise, aiQuestion, userAnswer, subGoalId, questionId, helpCount, audioInput);

        var (result, _) = await Emerge.Run<UserScore>(
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

    private static string BuildCommand(
        UserState userState,
        Exercise exercise,
        string aiQuestion,
        string userAnswer,
        int subGoalId,
        int questionId,
        int helpCount,
        bool audioInput)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("You are a language learning feedback generator. Evaluate the user's response to a language exercise.");
        sb.AppendLine();

        sb.AppendLine("User Information:");
        sb.AppendLine($"- Target Language: {userState.TargetLanguage}");
        sb.AppendLine($"- Current Level: {userState.CurrentLanguageLevel}");
        sb.AppendLine($"- Preferred Language: {userState.PreferredLanguage}");
        sb.AppendLine();

        sb.AppendLine("Exercise Information:");
        sb.AppendLine($"- Exercise Name: {exercise.Name}");
        sb.AppendLine($"- Exercise Type: {exercise.Type}");
        sb.AppendLine($"- Scenario: {exercise.Scenario}");

        if (exercise.Roles?.User?.SubGoals?.Count > 0)
        {
            sb.AppendLine("- User's Sub-Goals:");
            foreach (var sg in exercise.Roles.User.SubGoals)
            {
                var optional = sg.Optional ? " (Optional)" : "";
                sb.AppendLine($"  {sg.SubGoalId}. {sg.Description}{optional}");
            }
        }

        if (exercise.Questions?.Count > 0)
        {
            sb.AppendLine("- Questions:");
            foreach (var q in exercise.Questions)
            {
                sb.AppendLine($"  {q.QuestionId}. {q.Question}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"AI's Question/Prompt: {aiQuestion}");
        sb.AppendLine($"User's Answer: {userAnswer}");

        if (subGoalId > 0)
        {
            sb.AppendLine($"Current Sub-Goal ID: {subGoalId}");
        }

        if (questionId > 0)
        {
            sb.AppendLine($"Current Question ID: {questionId}");
        }

        sb.AppendLine($"Hints Used: {helpCount}");
        sb.AppendLine($"Audio Input: {audioInput}");
        sb.AppendLine();

        sb.AppendLine("Evaluate the user's response and provide:");
        sb.AppendLine("1. Score on three dimensions (0-3 each, where 3 is excellent):");
        sb.AppendLine("   - TaskFulfillment: How well does the answer address the question/sub-goal?");
        sb.AppendLine("   - OrganizationAndStructure: How well is the response organized and structured?");
        sb.AppendLine("   - LinguisticResourceAndAccuracy: How accurate is the grammar, vocabulary, and language usage?");
        sb.AppendLine();
        sb.AppendLine($"2. Constructive feedback in {userState.PreferredLanguage} that:");
        sb.AppendLine("   - Acknowledges what the user did well");
        sb.AppendLine("   - Points out specific areas for improvement");
        sb.AppendLine($"   - Is encouraging and appropriate for their {userState.CurrentLanguageLevel} level");
        sb.AppendLine();
        sb.AppendLine("Note: Be lenient for audio input as it may contain transcription errors.");

        return sb.ToString();
    }
}
