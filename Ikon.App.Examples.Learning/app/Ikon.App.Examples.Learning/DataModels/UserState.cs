namespace Ikon.App.Examples.Learning.DataModels;

public class UserState
{
    public string UserId { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = string.Empty;
    public string CurrentLanguageLevel { get; set; } = "A0";
    public string TargetLanguageLevel { get; set; } = "A1.1";
    public List<Exercise> CreatedExercises { get; set; } = [];
    public List<ExerciseReport> ExerciseHistory { get; set; } = [];
    public List<AchievementRecord> Achievements { get; set; } = [];
    public int CurrentStreak { get; set; } = 0;
    public int TotalPoints { get; set; } = 0;
    public DateTime LastActivityDate { get; set; } = DateTime.MinValue;
    public string Theme { get; set; } = "LakeBlue";

    // UI Settings
    public int SelectedCharacterIndex { get; set; } = 0;
    public int SelectedVoiceIndex { get; set; } = 0;
    public int SelectedViewModeIndex { get; set; } = 1;
}

public class ExerciseReport
{
    public string ExerciseId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public int Score { get; set; } = 0;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
}
