public enum GamePhase
{
    Intro,
    Accusation,
    Teleportation,
    Investigation,
    Proposal,
    Judgment,
    FinalVerdict
}

public enum CommandType
{
    Examine,
    Ask,
    Reflect,
    Propose,
    Look
}

public enum TranscriptRole
{
    System,
    Q,
    Player,
    Narrator,
    Witness
}

public record ParsedCommand(CommandType Type, string Argument);

public record TranscriptEntry(TranscriptRole Role, string Speaker, string Text);

public class Witness
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class SceneDescription
{
    public string LocationName { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImagePrompt { get; set; } = "";
    public List<string> ExaminableElements { get; set; } = [];
    public List<Witness> Witnesses { get; set; } = [];
    public string HiddenConnection { get; set; } = "";
}

public class Accusation
{
    public string Crime { get; set; } = "";
    public string QSpeech { get; set; } = "";
    public string HiddenLaw { get; set; } = "";
    public SceneDescription Scene { get; set; } = new();
}

public class InvestigationResponse
{
    public string Narration { get; set; } = "";
    public string QComment { get; set; } = "";
    public float ProximityToTruth { get; set; }
    public string ImageEvolutionHint { get; set; } = "";
}

public class JudgmentResponse
{
    public string QSpeech { get; set; } = "";
    public bool Correct { get; set; }
    public float Accuracy { get; set; }
    public string Explanation { get; set; } = "";
}

public class RoundResult
{
    public int RoundNumber { get; set; }
    public string Crime { get; set; } = "";
    public string HiddenLaw { get; set; } = "";
    public string PlayerTheory { get; set; } = "";
    public float Accuracy { get; set; }
    public bool Correct { get; set; }
}
