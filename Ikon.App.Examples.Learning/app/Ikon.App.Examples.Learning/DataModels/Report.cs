using Ikon.Common;

namespace Ikon.App.Examples.Learning.DataModels;

public class Report
{
    [Description("Overall score for the exercise (0-100).")]
    public int OverallScore { get; set; } = 0;

    [Description("Aggregated score dimensions across all questions/subgoals.")]
    public ScoreDimensions ScoreDimensions { get; set; } = new();

    [Description("Summary feedback on the user's performance.")]
    public string Feedback { get; set; } = string.Empty;

    [Description("Key takeaways and areas for improvement.")]
    public string KeyTakeaways { get; set; } = string.Empty;

    [Description("Total time taken to complete the exercise in seconds.")]
    public int TimeTaken { get; set; } = 0;
}

public class Hint
{
    [Description("A suggested continuation or hint to help the user respond.")]
    public string Suggestion { get; set; } = string.Empty;

    [Description("Brief explanation of why this suggestion might help.")]
    public string Explanation { get; set; } = string.Empty;
}

public class ImagePrompt
{
    [Description("Enhanced prompt for image generation with detailed visual description.")]
    public string Prompt { get; set; } = string.Empty;
}
