return await App.Run(args);

public record SessionIdentity(string Id);
public record ClientParams(string Id = "", bool Host = false);

[App]
public partial class Kahoot(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new AppTheme());

    private const int QuestionChannelCapacity = 3;
    private const int MaxQuestionHistory = 20;
    private const int QuestionGenerationTimeoutSeconds = 60;
    private const int MaxPlayers = 50;

    private readonly Reactive<GameStage> _gameStage = new(GameStage.Lobby);
    private readonly Reactive<KahootQuestion?> _currentQuestion = new(null);
    private readonly Reactive<int> _questionNumber = new(0);
    private readonly Reactive<int> _totalQuestions = new(10);
    private readonly Reactive<int> _countdown = new(20);
    private readonly Reactive<int> _countdownSeconds = new(20);
    private readonly Reactive<int> _feedbackSeconds = new(5);
    private readonly Reactive<int> _leaderboardSeconds = new(5);
    private readonly ReactiveList<Player> _players = new();

    private readonly ClientReactive<string> _playerName = new("");
    private readonly ClientReactive<bool> _hasJoined = new(false);
    private readonly ClientReactive<bool> _hasStarted = new(false);
    private readonly ClientReactive<int?> _selectedAnswer = new((int?)null);
    private readonly ClientReactive<bool> _hasAnswered = new(false);
    private readonly ClientReactive<int> _progressBarOffsetMs = new(0);

    private CancellationTokenSource _gameCts = new();
    private Channel<KahootQuestion> _questionChannel = Channel.CreateBounded<KahootQuestion>(new BoundedChannelOptions(QuestionChannelCapacity) { FullMode = BoundedChannelFullMode.DropOldest });
    private readonly ConcurrentDictionary<int, (int Answer, DateTimeOffset Timestamp)> _playerAnswers = new();
    private readonly List<string> _questionHistory = [];
    private DateTimeOffset _questionStartedAt;
    private string? _knowledgeBase;

    public async Task Main()
    {
        app.ClientJoinedAsync += OnClientJoinedAsync;
        app.ClientLeftAsync += OnClientLeftAsync;
        app.StoppingAsync += OnStoppingAsync;

        _knowledgeBase = LoadKnowledgeBase();

        UI.Root([Page.Default, "font-sans min-h-screen bg-black"], content: RenderUI);
    }

    private async Task OnStoppingAsync(StoppingEventArgs args)
    {
        await _gameCts.CancelAsync();
    }

    private async Task OnClientJoinedAsync(ClientJoinedEventArgs args)
    {
        {
            using var _ = ReactiveScope.Use(new ClientScope(args.ClientSessionId));

            if (_gameStage.Value == GameStage.Question)
            {
                var offsetMs = (int)(DateTimeOffset.UtcNow - _questionStartedAt).TotalMilliseconds;
                _progressBarOffsetMs.Value = Math.Max(0, offsetMs);
            }
        }

        await ClientFunctions.SetThemeAsync(Theme.Dark, targetId: args.ClientSessionId);
    }

    private async Task OnClientLeftAsync(ClientLeftEventArgs args)
    {
        _players.RemoveAll(p => p.ClientId == args.ClientSessionId);
        _playerAnswers.TryRemove(args.ClientSessionId, out _);
    }

    private bool IsHost()
    {
        var client = app.Clients[ReactiveScope.ClientId];
        return client?.Parameters.Host == true;
    }

    private Player? GetCurrentPlayer()
    {
        return _players.FirstOrDefault(p => p.ClientId == ReactiveScope.ClientId);
    }

    private async Task AddOrUpdatePlayerAsync(int clientId, string name)
    {
        var existingPlayer = _players.FirstOrDefault(p => p.ClientId == clientId);

        if (existingPlayer != null)
        {
            _players.Update(players => players.Select(p => p.ClientId == clientId ? p with { Name = name } : p));
        }
        else
        {
            if (_players.Count >= MaxPlayers)
            {
                return;
            }

            _players.Add(new Player(clientId, name, 0, 0, 0, 0));
        }

        if (existingPlayer == null && _gameStage.Value == GameStage.Question)
        {
            var offsetMs = (int)(DateTimeOffset.UtcNow - _questionStartedAt).TotalMilliseconds;

            using var scope = ReactiveScope.Use(new ClientScope(clientId));
            _progressBarOffsetMs.Value = Math.Max(0, offsetMs);
        }
    }

    private void UpdatePlayerScore(int clientId, int points, bool correct, bool missed)
    {
        if (_players.All(p => p.ClientId != clientId))
        {
            return;
        }

        _players.Update(players => players.Select(p =>
        {
            if (p.ClientId != clientId)
            {
                return p;
            }

            if (correct)
            {
                return p with
                {
                    Score = p.Score + points,
                    CorrectCount = p.CorrectCount + 1
                };
            }

            if (missed)
            {
                return p with
                {
                    MissedCount = p.MissedCount + 1
                };
            }

            return p with
            {
                WrongCount = p.WrongCount + 1
            };
        }));
    }

    private void ResetPlayerScores()
    {
        _players.Update(players => players.Select(p => p with
        {
            Score = 0,
            CorrectCount = 0,
            WrongCount = 0,
            MissedCount = 0
        }));
    }

    private static bool IsValidSessionId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length != 8)
        {
            return false;
        }

        return id.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
    }

    private string GetJoinUrl()
    {
        var sessionId = app.Clients[ReactiveScope.ClientId]?.Parameters.Id ?? "";
        return app.JoinUrl(new { id = sessionId });
    }

    private string GetCreateSessionUrl()
    {
        return app.JoinUrl(new { id = GenerateSessionId(), host = "true" });
    }

    private static string GenerateSessionId()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    private string? LoadKnowledgeBase()
    {
        try
        {
            var path = Path.Combine(app.DataDirectory, "ikon-platform-knowledge.txt");

            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            Log.Instance.Warning("Knowledge base file not found");
            return null;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to load knowledge base: {ex.Message}");
            return null;
        }
    }
}
