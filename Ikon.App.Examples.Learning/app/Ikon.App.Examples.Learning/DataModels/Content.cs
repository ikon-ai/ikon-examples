using System.Text.Json.Serialization;

namespace Ikon.App.Examples.Learning.DataModels;

public class Skill
{
    public List<string> KNNIds { get; set; } = [];
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public Dictionary<string, LevelEntry> Levels { get; set; } = new();
}

public class SubSkill
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string ContextForExercise { get; set; } = string.Empty;
}

public class LevelEntry
{
    public string Common { get; set; } = "";
    public List<SubSkill> SubSkills { get; set; } = [];
}

public class SkillGroup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> SkillIds { get; set; } = [];
}

public class LearningTheme
{
    public string Id { get; set; } = string.Empty;
    public ThemeMultiLanguageText Description { get; set; } = new();
    public ThemeMultiLanguageText DetailedDescription { get; set; } = new();
    public string StartingSkillGroupId { get; set; } = string.Empty;
    public List<string> ExampleScenarios { get; set; } = [];
}

public class ThemeMultiLanguageText
{
    [JsonPropertyName("en")]
    public string En { get; set; } = string.Empty;

    [JsonPropertyName("fi")]
    public string Fi { get; set; } = string.Empty;
}

public class Exercise
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExerciseType Type { get; set; } = ExerciseType.None;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExerciseSource Source { get; set; } = ExerciseSource.None;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExerciseCategory Category { get; set; } = ExerciseCategory.None;

    public string SkillId { get; set; } = string.Empty;
    public string LevelId { get; set; } = string.Empty;
    public string SubSkillId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string Scenario { get; set; } = string.Empty;
    public Roles Roles { get; set; } = new();
    public List<QuestionData> Questions { get; set; } = [];
    public string AudioPreset { get; set; } = string.Empty;
}

public enum ExerciseType
{
    SimpleQA,
    Conversational,
    None
}

public enum ExerciseSource
{
    Content,
    Custom,
    News,
    None
}

public enum ExerciseCategory
{
    Assignment,
    ThemeChallenge,
    None
}

public class Roles
{
    public string AI { get; set; } = string.Empty;
    public UserRole User { get; set; } = new();
}

public class UserRole
{
    public string Role { get; set; } = string.Empty;
    public List<SubGoal> SubGoals { get; set; } = [];
}

public class SubGoal
{
    public int SubGoalId { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public bool Optional { get; set; } = false;
}

public class QuestionData
{
    public int QuestionId { get; set; } = 0;
    public string Question { get; set; } = string.Empty;
    public string? CorrectAnswer { get; set; } = null;
}

public class AudioPreset
{
    public string Name { get; set; } = string.Empty;
    public float[] ReverbFeedbacks { get; set; } = [];
    public float[] ReverbMixes { get; set; } = [];
    public float[] ReverbDelayTimesMs { get; set; } = [];
    public float[] ReverbCutoffFrequencies { get; set; } = [];
}

public class Content(List<Skill> skills, List<SkillGroup> skillGroups, List<LearningTheme> themes, List<AudioPreset> audioPresets)
{
    public List<Skill> Skills { get; set; } = skills;
    public List<SkillGroup> SkillGroups { get; set; } = skillGroups;
    public List<LearningTheme> Themes { get; set; } = themes;
    public List<AudioPreset> AudioPresets { get; set; } = audioPresets;
}
