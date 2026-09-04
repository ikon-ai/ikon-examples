namespace Ikon.App.Patterns.Patterns;

// Pattern: multi-user-game — see docs/patterns/multi-user-game.md.
// The real app hosts LiveQuizApp under `[App]` and boots it with `App.Run(args)`; here it is a nested
// example class so the pattern's shared/per-client state and host-driven flow compile self-contained.
internal sealed class MultiUserGame : IPatternDemo
{
    public string Slug => "multi-user-game";
    public string Title => "Multi-user game";
    public string Category => "Realtime";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Multi-user pattern with no standalone demo surface: shared vs per-client reactive state driven by a host client that advances the stage for everyone. See the source and docs/patterns/multi-user-game.md.");

    #region docsnippet:pattern-multi-user-game
    // Host detection lives in ClientParams. The host client connects with `?host=true` query param.
    public record SessionIdentity(string Id);
    public record ClientParams(string Id = "", bool Host = false);

    public partial class LiveQuizApp(IApp<SessionIdentity, ClientParams> app)
    {
        private UI UI { get; } = new(app, new IkonTheme());

        // ── Shared state (all clients see the same values) ──────────────────────
        private readonly Reactive<GameStage> _stage = new(GameStage.Lobby);
        private readonly Reactive<int> _questionIndex = new(0);
        private readonly ReactiveList<Question> _questions = new();
        private readonly ReactiveList<Player> _players = new();

        // ── Per-client state (each player has their own copy) ───────────────────
        private readonly ClientReactive<string> _playerName = new("");
        private readonly ClientReactive<bool> _hasJoined = new(false);
        private readonly ClientReactive<int?> _selectedAnswer = new((int?)null);

        // ── Server-side bookkeeping (not reactive — used in handlers only) ──────
        private readonly ConcurrentDictionary<int, int> _playerAnswers = new();  // ClientSessionId → choice

        public async Task Main()
        {
            app.OnClientJoined(OnClientJoinedAsync);   // friendly extension — Func<Context, Task>
            app.OnClientLeft(OnClientLeftAsync);       // never raw `app.ClientJoinedAsync += ...`

            UI.Root([Page.Default], content: RenderUI);
        }

        // One screen per stage. Everyone renders from the same `_stage` reactive, so the host advancing
        // it moves every client at once.
        private void RenderUI(UIView view) =>
            view.Text([Text.H1], text: $"Stage: {_stage.Value}");

        private async Task OnClientJoinedAsync(Ikon.Common.Core.Protocol.Context ctx)
        {
            // ReactiveScope inside event handlers needs an explicit ClientScope —
            // ctx.ClientSessionId is the int identity for this client.
            using var _ = ReactiveScope.Use(new ClientScope(ctx.ClientSessionId));
            // Now ClientReactive<T>.Value reads/writes for THIS specific client.
        }

        private async Task OnClientLeftAsync(Ikon.Common.Core.Protocol.Context ctx)
        {
            _players.RemoveAll(p => p.ClientId == ctx.ClientSessionId);   // one notification
            _playerAnswers.TryRemove(ctx.ClientSessionId, out _);
        }

        // Host detection — read parameters of the CURRENT client through the indexer.
        // ReactiveScope.ClientId is int (not string).
        private bool IsHost()
        {
            var client = app.Clients[ReactiveScope.ClientId];
            return client?.Parameters.Host == true;
        }

        private Player? CurrentPlayer() =>
            _players.FirstOrDefault(p => p.ClientId == ReactiveScope.ClientId);

        private async Task JoinAsync(string name)
        {
            var clientId = ReactiveScope.ClientId;

            if (!_players.Any(p => p.ClientId == clientId))
            {
                _players.Add(new Player(clientId, name, Score: 0));   // mutator notifies; no list rebuild
            }
            _hasJoined.Value = true;        // per-client reactive — only THIS client's UI flips
            _playerName.Value = name;
        }

        private async Task SelectAnswerAsync(int choice)
        {
            if (_selectedAnswer.Value != null) return;     // per-client guard — already answered
            _selectedAnswer.Value = choice;
            _playerAnswers[ReactiveScope.ClientId] = choice;
        }

        private async Task HostStartNextQuestionAsync()
        {
            // Reset per-client state for everyone by walking the player list.
            // (Don't iterate `app.Clients` — there is no .All / .Current; use the
            // shared `_players` list instead and write to each client by id.)
            foreach (var player in _players)
            {
                _selectedAnswer.SetFor(player.ClientId, null);
            }
            _playerAnswers.Clear();
            _questionIndex.Value++;
            _stage.Value = GameStage.Question;
        }
    }

    public enum GameStage { Lobby, Question, Reveal, Leaderboard, GameOver }
    public record Player(int ClientId, string Name, int Score);
    public record Question(string Prompt, string[] Choices, int CorrectIndex);
    #endregion

    // Outside the docsnippet: the question bank is part of the pattern's shared state, but the trimmed
    // example advances the stage without rendering questions. Reference it so the warnings-as-errors
    // build does not flag the field no method above reads.
    partial class LiveQuizApp
    {
        private void KeepPatternFieldsReferenced() => _ = _questions.Count;
    }
}
