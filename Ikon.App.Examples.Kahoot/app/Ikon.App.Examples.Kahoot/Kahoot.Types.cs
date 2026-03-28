namespace Ikon.App.Examples.Kahoot;

public enum GameStage
{
    Lobby,
    CountdownToStart,
    Question,
    Feedback,
    Leaderboard,
    GameOver
}

public record KahootQuestion(
    string Question,
    string[] AnswerOptions,
    int CorrectIndex,
    string Explanation,
    string Category);

public record Player(
    int ClientId,
    string Name,
    int Score,
    int CorrectCount,
    int WrongCount,
    int MissedCount);
