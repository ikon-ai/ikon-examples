namespace Ikon.App.Patterns.Patterns;

// Pattern: live-quiz-countdown-loop — see docs/patterns/live-quiz-countdown-loop.md.
// The docsnippet region drives one round per iteration: pull the next pre-generated question, tick a
// per-second countdown, break early when everyone answered. The stubs outside it hold the game state
// the loop reads and stand in for the per-round answer bookkeeping.
internal sealed class LiveQuizCountdownLoop : IPatternDemo
{
    public string Slug => "live-quiz-countdown-loop";
    public string Title => "Live quiz countdown loop";
    public string Category => "Realtime";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: one game loop drives each round — pull the next question, tick a per-second countdown, and break early once everyone has answered. See the source and docs/patterns/live-quiz-countdown-loop.md.");

    private sealed record Question(string Text, IReadOnlyList<string> Answers);
    private enum GameStage { Lobby, Question, Feedback, Leaderboard, GameOver }

    private const int QuestionGenerationTimeoutSeconds = 30;

    private readonly Reactive<int> _totalQuestions = new(10);
    private readonly Reactive<int> _questionNumber = new(0);
    private readonly Reactive<int> _countdown = new(0);
    private readonly Reactive<int> _countdownSeconds = new(20);
    private readonly Reactive<int> _feedbackSeconds = new(3);
    private readonly Reactive<int> _leaderboardSeconds = new(5);
    private readonly Reactive<GameStage> _gameStage = new(GameStage.Lobby);
    private readonly Reactive<Question?> _currentQuestion = new((Question?)null);
    private readonly System.Threading.Channels.Channel<Question> _questionChannel =
        System.Threading.Channels.Channel.CreateUnbounded<Question>();
    private readonly ConcurrentDictionary<int, int> _playerAnswers = new();
    private DateTimeOffset _questionStartedAt;

    private void ResetPlayerAnswerState() => throw new NotImplementedException();
    private bool AllPlayersAnswered() => throw new NotImplementedException();

    // Time-bonus scoring reads the per-question start timestamp the loop stamps each round.
    private void ProcessAnswers()
    {
        _ = DateTimeOffset.UtcNow - _questionStartedAt;
        throw new NotImplementedException();
    }

    #region docsnippet:pattern-live-quiz-countdown-loop
    private async Task RunGameLoopAsync(CancellationToken ct)
    {
        for (int questionIndex = 0; questionIndex < _totalQuestions.Value; questionIndex++)
        {
            if (ct.IsCancellationRequested) { break; }

            _questionNumber.Value = questionIndex + 1;

            using var questionTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            questionTimeoutCts.CancelAfter(TimeSpan.FromSeconds(QuestionGenerationTimeoutSeconds));

            try
            {
                _currentQuestion.Value = await _questionChannel.Reader.ReadAsync(questionTimeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                Log.Instance.Warning($"Timed out waiting for question {questionIndex + 1}, ending game");
                break;
            }

            _playerAnswers.Clear();
            ResetPlayerAnswerState();
            _questionStartedAt = DateTimeOffset.UtcNow;
            _gameStage.Value = GameStage.Question;
            _countdown.Value = _countdownSeconds.Value;

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (_countdown.Value > 0 && !ct.IsCancellationRequested)
            {
                if (AllPlayersAnswered()) { break; }

                await timer.WaitForNextTickAsync(ct);
                _countdown.Value--;
            }

            ProcessAnswers();
            _gameStage.Value = GameStage.Feedback;
            await Task.Delay(TimeSpan.FromSeconds(_feedbackSeconds.Value), ct);

            if (questionIndex < _totalQuestions.Value - 1)
            {
                _gameStage.Value = GameStage.Leaderboard;
                await Task.Delay(TimeSpan.FromSeconds(_leaderboardSeconds.Value), ct);
            }
        }

        _gameStage.Value = GameStage.GameOver;
    }
    #endregion
}
