<!-- mined-from: Ikon.App.Examples.Kahoot -->
# Live Quiz Countdown Loop — Per-Question Timer With Early Finish

A Kahoot-style game loop that pulls the next question from an unbounded channel, ticks a per-second countdown via `PeriodicTimer`, and breaks early when every player has answered. Question generation runs in parallel (a separate task fills the channel) so the player never waits between rounds.

## When to use

You're building a turn-based or round-based multiplayer game where each round has a fixed time budget but should end early if all participants act. The producer/consumer split lets the LLM generate the next round while the current one is being played.

## Snippet

```csharp
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
```

## Notes

- `Channel.CreateUnbounded` holds the pre-generated questions; the generator task runs ahead with `_totalQuestions + 2` items. Don't reach for `CreateBounded` with `DropOldest` here — the producer is bounded by the question count already, and dropping would silently lose a round.
- Linked CTS gives a per-question generation timeout that doesn't kill the whole game.
- `_playerAnswers` is a `ConcurrentDictionary` keyed by `ClientId` — `ProcessAnswers` reads it once after the loop exits.
- Time-bonus scoring: `points = 500 + floor(speedFactor * 500)` where `speedFactor = timeRemaining / totalTime`.

## See also

- `multi-user-game`
- `single-processor-channel-queue`
