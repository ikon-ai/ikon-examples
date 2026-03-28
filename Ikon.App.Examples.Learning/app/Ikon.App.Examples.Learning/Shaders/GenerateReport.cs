using Ikon.AI.Emergence;
using Ikon.AI.Kernel;

namespace Ikon.App.Examples.Learning.Shaders;

internal static class GenerateReportShader
{
    public static async Task<Report> GenerateAsync(
        string modelName,
        string reasoningEffort,
        UserState userState,
        Exercise exercise,
        List<UserScore> userScores,
        int timeTakenSeconds,
        List<KernelContext>? contexts = null,
        CancellationToken cancellationToken = default)
    {
        var command = BuildCommand(userState, exercise, userScores, timeTakenSeconds);

        var (result, _) = await Emerge.Run<Report>(
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
        List<UserScore> userScores,
        int timeTakenSeconds)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("You are generating a completion report for a language learning exercise.");
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
        sb.AppendLine();

        sb.AppendLine("User's Performance:");
        foreach (var score in userScores)
        {
            sb.AppendLine("---");

            if (score.SubGoalId > 0)
            {
                sb.AppendLine($"Sub-Goal ID: {score.SubGoalId}");
            }

            if (score.QuestionId > 0)
            {
                sb.AppendLine($"Question ID: {score.QuestionId}");
            }

            sb.AppendLine($"AI Question: {score.AiResponse}");
            sb.AppendLine($"User Answer: {score.ProvidedAnswer}");
            sb.AppendLine($"Task Fulfillment: {score.Score.TaskFulfillment}/3");
            sb.AppendLine($"Organization: {score.Score.OrganizationAndStructure}/3");
            sb.AppendLine($"Linguistic Accuracy: {score.Score.LinguisticResourceAndAccuracy}/3");
            sb.AppendLine($"Hints Used: {score.HintsAsked}");
        }

        sb.AppendLine();
        sb.AppendLine($"Time Taken: {timeTakenSeconds} seconds");
        sb.AppendLine();

        sb.AppendLine("Generate a comprehensive report with:");
        sb.AppendLine("1. OverallScore: A score from 0-100 based on the average performance across all dimensions");
        sb.AppendLine("2. ScoreDimensions: Aggregated averages for each dimension (0-3 scale)");
        sb.AppendLine($"3. Feedback: A summary of the user's overall performance in {userState.PreferredLanguage}");
        sb.AppendLine("4. KeyTakeaways: 2-3 specific areas the user should focus on for improvement");
        sb.AppendLine($"5. TimeTaken: {timeTakenSeconds}");

        return sb.ToString();
    }
}
