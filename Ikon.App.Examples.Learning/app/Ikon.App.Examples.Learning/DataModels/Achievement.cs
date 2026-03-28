namespace Ikon.App.Examples.Learning.DataModels;

public enum Achievement
{
    FirstStep,
    DailyGrind,
    Devoted,
    Perfectionist,
    Comeback,
    EarlyBird,
    NightOwl,
    SuperLearner,
    SuperSpeed,
    Master,
    Marathon,
    UpToDate,
    Reporter,
    Creator,
    MasterCreator,
    Curiosity,
    LateToTheParty,
    FirstWords,
    A1_1,
    A1_2,
    A1_3,
    A2_1,
    A2_2,
    B1_1,
    None
}

public class AchievementRecord
{
    public Achievement Achievement { get; set; } = Achievement.None;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class AchievementExtensions
{
    public static string Title(this Achievement achievement, Translations translations)
    {
        return achievement switch
        {
            Achievement.FirstStep => translations.FirstStep,
            Achievement.DailyGrind => translations.DailyGrind,
            Achievement.Devoted => translations.Devoted,
            Achievement.Perfectionist => translations.Perfectionist,
            Achievement.Comeback => translations.Comeback,
            Achievement.EarlyBird => translations.EarlyBird,
            Achievement.NightOwl => translations.NightOwl,
            Achievement.SuperLearner => translations.SuperLearner,
            Achievement.SuperSpeed => translations.SuperSpeed,
            Achievement.Master => translations.Master,
            Achievement.Marathon => translations.Marathon,
            Achievement.UpToDate => translations.UpToDate,
            Achievement.Reporter => translations.Reporter,
            Achievement.Creator => translations.Creator,
            Achievement.MasterCreator => translations.MasterCreator,
            Achievement.Curiosity => translations.Curiosity,
            Achievement.LateToTheParty => translations.LateToTheParty,
            Achievement.FirstWords => translations.FirstWords,
            Achievement.A1_1 => "A1.1",
            Achievement.A1_2 => "A1.2",
            Achievement.A1_3 => "A1.3",
            Achievement.A2_1 => "A2.1",
            Achievement.A2_2 => "A2.2",
            Achievement.B1_1 => "B1.1",
            _ => achievement.ToString()
        };
    }

    public static string Description(this Achievement achievement, Translations translations)
    {
        return achievement switch
        {
            Achievement.FirstStep => translations.FirstStepDescription,
            Achievement.DailyGrind => translations.DailyGrindDescription,
            Achievement.Devoted => translations.DevotedDescription,
            Achievement.Perfectionist => translations.PerfectionistDescription,
            Achievement.Comeback => translations.ComebackDescription,
            Achievement.EarlyBird => translations.EarlyBirdDescription,
            Achievement.NightOwl => translations.NightOwlDescription,
            Achievement.SuperLearner => translations.SuperLearnerDescription,
            Achievement.SuperSpeed => translations.SuperSpeedDescription,
            Achievement.Master => translations.MasterDescription,
            Achievement.Marathon => translations.MarathonDescription,
            Achievement.UpToDate => translations.UpToDateDescription,
            Achievement.Reporter => translations.ReporterDescription,
            Achievement.Creator => translations.CreatorDescription,
            Achievement.MasterCreator => translations.MasterCreatorDescription,
            Achievement.Curiosity => translations.CuriosityDescription,
            Achievement.LateToTheParty => translations.LateToThePartyDescription,
            Achievement.FirstWords => translations.FirstWordsDescription,
            Achievement.A1_1 => translations.A1_1Description,
            Achievement.A1_2 => translations.A1_2Description,
            Achievement.A1_3 => translations.A1_3Description,
            Achievement.A2_1 => translations.A2_1Description,
            Achievement.A2_2 => translations.A2_2Description,
            Achievement.B1_1 => translations.B1_1Description,
            _ => achievement.ToString()
        };
    }
}
