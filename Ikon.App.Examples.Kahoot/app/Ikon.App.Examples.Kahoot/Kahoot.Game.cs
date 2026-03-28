public partial class Kahoot
{
    private static readonly string[] QuestionCategories =
    [
        "Architecture & Server-Driven UI",
        "Emergence Patterns & AI Orchestration",
        "Teleport Protocol & Communication",
        "Reactive State & Multiplayer",
        "Security & Thin Client",
        "Cross-Platform & SDKs",
        "Crosswind Styling & Motion",
        "Persistence & Background Work",
        "Threads & AI Development",
        "Real-World App Examples"
    ];

    private class GeneratedKahootQuestion
    {
        public string Question { get; set; } = "";
        public string[] AnswerOptions { get; set; } = [];
        public string CorrectAnswer { get; set; } = "";
        public string Explanation { get; set; } = "";
        public string Category { get; set; } = "";
    }

    private async Task StartGameAsync()
    {
        if (_gameStage.Value != GameStage.Lobby && _gameStage.Value != GameStage.GameOver)
        {
            return;
        }

        if (_players.Value.Count == 0)
        {
            return;
        }

        await _gameCts.CancelAsync();
        _gameCts = new CancellationTokenSource();
        _questionChannel = Channel.CreateBounded<KahootQuestion>(new BoundedChannelOptions(QuestionChannelCapacity) { FullMode = BoundedChannelFullMode.DropOldest });

        ResetPlayerScores();
        _questionNumber.Value = 0;
        _playerAnswers.Clear();
        _questionHistory.Clear();
        _currentQuestion.Value = null;
        _countdown.Value = _countdownSeconds.Value;

        _ = RunQuestionGenerationLoopAsync(_gameCts.Token);

        _gameStage.Value = GameStage.CountdownToStart;
        await RunCountdownToStartAsync(_gameCts.Token);
    }

    private async Task StopGameAsync()
    {
        await _gameCts.CancelAsync();
        _gameCts = new CancellationTokenSource();

        _currentQuestion.Value = null;
        _questionNumber.Value = 0;
        _countdown.Value = _countdownSeconds.Value;
        _gameStage.Value = GameStage.Lobby;
    }

    private async Task RunCountdownToStartAsync(CancellationToken ct)
    {
        try
        {
            _countdown.Value = 3;

            for (int i = 3; i >= 1; i--)
            {
                _countdown.Value = i;
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }

            await RunGameLoopAsync(ct);
        }
        catch (OperationCanceledException)
        {
            Log.Instance.Info("Countdown cancelled");
        }
    }

    private async Task RunGameLoopAsync(CancellationToken ct)
    {
        try
        {
            for (int questionIndex = 0; questionIndex < _totalQuestions.Value; questionIndex++)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

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
                    if (AllPlayersAnswered())
                    {
                        break;
                    }

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
        catch (OperationCanceledException)
        {
            Log.Instance.Info("Game loop cancelled");
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error in game loop: {ex}");
            _gameStage.Value = GameStage.GameOver;
        }
    }

    private async Task RunQuestionGenerationLoopAsync(CancellationToken ct)
    {
        try
        {
            for (int i = 0; i < _totalQuestions.Value + 2 && !ct.IsCancellationRequested; i++)
            {
                var question = await GenerateQuestionAsync(ct);

                if (question != null)
                {
                    await _questionChannel.Writer.WriteAsync(question, ct);
                }
                else
                {
                    i--;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.Instance.Info("Question generation loop cancelled");
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error in question generation loop: {ex}");
        }
    }

    private async Task<KahootQuestion?> GenerateQuestionAsync(CancellationToken ct)
    {
        try
        {
            var category = QuestionCategories[Random.Shared.Next(QuestionCategories.Length)];

            var questionHistoryText = _questionHistory.Count > 0
                ? string.Join("\n", _questionHistory.Select(q => $" - {q}"))
                : "None yet.";

            string command = $"""
                              You are generating a trivia question about the Ikon platform for a Kahoot-style quiz game.

                              # Knowledge Base
                              {_knowledgeBase ?? "No knowledge base available."}

                              # Category
                              Focus on this category: {category}

                              # Command
                              Generate a multiple-choice trivia question about the Ikon platform.
                              The question must have exactly 4 answer options, with only one correct answer.

                              IMPORTANT: Focus on architecture, design philosophy, and conceptual understanding.
                              Ask about WHY things work the way they do, what makes Ikon different, and what the platform enables.
                              Good questions test whether someone understands the platform's core ideas and design decisions.
                              Use real facts and stats from the knowledge base (e.g., number of models, protocol details, app examples).
                              Do NOT ask about specific API syntax, method signatures, or class names.

                              Examples of good question styles:
                              - "Why doesn't Ikon need a separate WebSocket server for real-time updates?"
                              - "How many AI providers does Ikon support through Emergence?"
                              - "What Emergence pattern runs multiple attempts and picks the best result?"
                              - "How many lines of code was the animated voice chat app?"
                              - "What keeps lip sync from drifting in the animated voice chat?"

                              Make the wrong answers plausible but clearly incorrect to someone who knows the platform.
                              Keep answer options short (1-5 words each).
                              Provide a brief, informative explanation of why the correct answer is right.

                              # Previous Questions (do not repeat)
                              {questionHistoryText}

                              Generate a fresh, unique question that differs from all previous ones.
                              """;

            var (result, _) = await Emerge.Run<GeneratedKahootQuestion>(LLMModel.Gemini25Flash, new KernelContext(), pass =>
            {
                pass.Command = command;
                pass.Temperature = 0.8f;
                pass.MaxOutputTokens = 4000;
            }).FinalAsync(ct);

            if (string.IsNullOrWhiteSpace(result.Question) || result.AnswerOptions.Length < 4)
            {
                return null;
            }

            var correctAnswer = result.CorrectAnswer;
            var shuffledOptions = result.AnswerOptions.OrderBy(_ => Random.Shared.Next()).ToArray();
            var correctIndex = Array.FindIndex(shuffledOptions, o => string.Equals(o.Trim(), correctAnswer?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (correctIndex < 0)
            {
                Log.Instance.Warning($"Correct answer '{correctAnswer}' not found in options, discarding question");
                return null;
            }

            _questionHistory.Add(result.Question);

            if (_questionHistory.Count > MaxQuestionHistory)
            {
                _questionHistory.RemoveAt(0);
            }

            return new KahootQuestion(
                result.Question,
                shuffledOptions,
                correctIndex,
                result.Explanation,
                result.Category);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error generating question: {ex}");
            return null;
        }
    }

    private void ResetPlayerAnswerState()
    {
        foreach (var player in _players.Value)
        {
            using var _ = ReactiveScope.Use(new ClientScope(player.ClientId));
            _selectedAnswer.Value = null;
            _hasAnswered.Value = false;
            _progressBarOffsetMs.Value = 0;
        }
    }

    private bool AllPlayersAnswered()
    {
        var playerCount = _players.Value.Count;
        return playerCount > 0 && _playerAnswers.Count >= playerCount;
    }

    private Task SubmitAnswer(int answerIndex)
    {
        if (_currentQuestion.Value == null || _gameStage.Value != GameStage.Question || _countdown.Value <= 0)
        {
            return Task.CompletedTask;
        }

        var clientId = ReactiveScope.ClientId;

        if (_playerAnswers.TryAdd(clientId, (answerIndex, DateTimeOffset.UtcNow)))
        {
            Log.Instance.Info($"Player {clientId} submitted answer: {answerIndex}");
        }

        return Task.CompletedTask;
    }

    private void ProcessAnswers()
    {
        if (_currentQuestion.Value == null)
        {
            return;
        }

        foreach (var player in _players.Value)
        {
            if (_playerAnswers.TryGetValue(player.ClientId, out var entry))
            {
                var isCorrect = entry.Answer == _currentQuestion.Value.CorrectIndex;

                if (isCorrect)
                {
                    var elapsed = (entry.Timestamp - _questionStartedAt).TotalSeconds;
                    var totalTime = _countdownSeconds.Value;
                    var timeRemaining = Math.Max(0, totalTime - elapsed);
                    var speedFactor = timeRemaining / totalTime;
                    var points = 500 + (int)Math.Floor(speedFactor * 500);
                    UpdatePlayerScore(player.ClientId, points, true, false);
                }
                else
                {
                    UpdatePlayerScore(player.ClientId, 0, false, false);
                }
            }
            else
            {
                UpdatePlayerScore(player.ClientId, 0, false, true);
            }
        }
    }
}
