return await App.Run(args);

public record SessionIdentity(string Id);
public record ClientParams(string Id = "", bool Host = false);

[App]
public partial class Kahoot(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new Theme());

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
    private readonly Reactive<List<Player>> _players = new([]);

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

        await ClientFunctions.SetThemeAsync(args.ClientSessionId, Constants.DarkTheme);
    }

    private async Task OnClientLeftAsync(ClientLeftEventArgs args)
    {
        var players = _players.Value.ToList();
        var player = players.FirstOrDefault(p => p.ClientId == args.ClientSessionId);

        if (player != null)
        {
            players.Remove(player);
            _players.Value = players;
        }

        _playerAnswers.TryRemove(args.ClientSessionId, out _);
    }

    private bool IsHost()
    {
        var client = app.Clients[ReactiveScope.ClientId];
        return client?.Parameters.Host == true;
    }

    private Player? GetCurrentPlayer()
    {
        return _players.Value.FirstOrDefault(p => p.ClientId == ReactiveScope.ClientId);
    }

    private async Task AddOrUpdatePlayerAsync(int clientId, string name)
    {
        var players = _players.Value.ToList();
        var existingPlayer = players.FirstOrDefault(p => p.ClientId == clientId);

        if (existingPlayer != null)
        {
            var index = players.IndexOf(existingPlayer);
            players[index] = existingPlayer with { Name = name };
        }
        else
        {
            if (players.Count >= MaxPlayers)
            {
                return;
            }

            players.Add(new Player(clientId, name, 0, 0, 0, 0));
        }

        _players.Value = players;

        if (existingPlayer == null && _gameStage.Value == GameStage.Question)
        {
            var offsetMs = (int)(DateTimeOffset.UtcNow - _questionStartedAt).TotalMilliseconds;

            using var scope = ReactiveScope.Use(new ClientScope(clientId));
            _progressBarOffsetMs.Value = Math.Max(0, offsetMs);
        }
    }

    private void UpdatePlayerScore(int clientId, int points, bool correct, bool missed)
    {
        var players = _players.Value.ToList();
        var player = players.FirstOrDefault(p => p.ClientId == clientId);

        if (player == null)
        {
            return;
        }

        var index = players.IndexOf(player);

        if (correct)
        {
            players[index] = player with
            {
                Score = player.Score + points,
                CorrectCount = player.CorrectCount + 1
            };
        }
        else if (missed)
        {
            players[index] = player with
            {
                MissedCount = player.MissedCount + 1
            };
        }
        else
        {
            players[index] = player with
            {
                WrongCount = player.WrongCount + 1
            };
        }

        _players.Value = players;
    }

    private void ResetPlayerScores()
    {
        var players = _players.Value.ToList();

        for (int i = 0; i < players.Count; i++)
        {
            players[i] = players[i] with
            {
                Score = 0,
                CorrectCount = 0,
                WrongCount = 0,
                MissedCount = 0
            };
        }

        _players.Value = players;
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
        return $"{app.ReactiveGlobalState.ChannelUrl.Value}?id={app.SessionIdentity.Id}";
    }

    private string GetCreateSessionUrl()
    {
        return $"{app.ReactiveGlobalState.ChannelUrl.Value}?id={GenerateSessionId()}&host=true";
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
