using Ikon.Common;

namespace Ikon.App.Examples.Learning.DataModels;

public class UserScore
{
    [Description("The Sub Goal ID related to the question.")]
    public int SubGoalId { get; set; } = 0;

    [Description("Question ID from the given data.")]
    public int QuestionId { get; set; } = 0;

    [Description("The AI generated message.")]
    public string AiResponse { get; set; } = string.Empty;

    [Description("The user response that was provided.")]
    public string ProvidedAnswer { get; set; } = string.Empty;

    [Description("The score of the user response.")]
    public ScoreDimensions Score { get; set; } = new();

    [Description("Feedback that justifies the score given.")]
    public string Feedback { get; set; } = string.Empty;

    [Description("Whether hints were asked. 'Yes' or 'No'.")]
    public string HintsAsked { get; set; } = "No";

    [Description("Whether the user provided the answer verbally. 'Yes' or 'No'.")]
    public string AudioInput { get; set; } = "No";

    [Description("UTC timestamp of when the AI asked the question.")]
    public string AiResponseTimestamp { get; set; } = string.Empty;

    [Description("UTC timestamp of when the user answered.")]
    public string ProvidedAnswerTimestamp { get; set; } = string.Empty;
}
