# Multi-User Game — Host + Players + Per-Client Answers

A real-time multi-user trivia/quiz game (Kahoot-style). One client is the **host** (sees question + reveals); other clients are **players** (see same question, tap an answer, see leaderboard). Mixes shared state (current question, leaderboard) with per-client state (player name, selected answer, has-joined flag).

## When to use

Live trivia, multiplayer quiz, kahoot-style games, party quizzes, classroom quizzes, multi-user voting, crowd-sourced polls. Any app where one client orchestrates and others participate with their own choices visible only to themselves.

## Snippet

```csharp
return await App.Run(args);

// Host detection lives in ClientParams. The host client connects with `?host=true` query param.
public record SessionIdentity(string Id);
public record ClientParams(string Id = "", bool Host = false);

[App]
public partial class LiveQuizApp(IApp<SessionIdentity, ClientParams> app)
{
    // ── Shared state (all clients see the same values) ──────────────────────
    private readonly Reactive<GameStage> _stage = new(GameStage.Lobby);
    private readonly Reactive<int> _questionIndex = new(0);
    private readonly Reactive<List<Question>> _questions = new([]);
    private readonly Reactive<List<Player>> _players = new([]);

    // ── Per-client state (each player has their own copy) ───────────────────
    private readonly ClientReactive<string> _playerName = new("");
    private readonly ClientReactive<bool> _hasJoined = new(false);
    private readonly ClientReactive<int?> _selectedAnswer = new((int?)null);

    // ── Server-side bookkeeping (not reactive — used in handlers only) ──────
    private readonly ConcurrentDictionary<int, int> _playerAnswers = new();  // ClientSessionId → choice

    public async Task Main()
    {
        app.ClientJoinedAsync += OnClientJoinedAsync;   // 1-arg async — never `() =>`
        app.ClientLeftAsync += OnClientLeftAsync;       // 1-arg async — never `() =>`

        UI.Root([Page.Default], content: RenderUI);
    }

    private async Task OnClientJoinedAsync(ClientJoinedEventArgs args)
    {
        // ReactiveScope inside event handlers needs an explicit ClientScope —
        // args.ClientSessionId is the int identity for this client.
        using var _ = ReactiveScope.Use(new ClientScope(args.ClientSessionId));
        // Now ClientReactive<T>.Value reads/writes for THIS specific client.
    }

    private async Task OnClientLeftAsync(ClientLeftEventArgs args)
    {
        var players = _players.Value.ToList();
        var leaver = players.FirstOrDefault(p => p.ClientId == args.ClientSessionId);
        if (leaver != null)
        {
            players.Remove(leaver);
            _players.Value = players;
        }
        _playerAnswers.TryRemove(args.ClientSessionId, out _);
    }

    // Host detection — read parameters of the CURRENT client through the indexer.
    // ReactiveScope.ClientId is int (not string).
    private bool IsHost()
    {
        var client = app.Clients[ReactiveScope.ClientId];
        return client?.Parameters.Host == true;
    }

    private Player? CurrentPlayer() =>
        _players.Value.FirstOrDefault(p => p.ClientId == ReactiveScope.ClientId);

    private async Task JoinAsync(string name)
    {
        var clientId = ReactiveScope.ClientId;
        var players = _players.Value.ToList();

        if (!players.Any(p => p.ClientId == clientId))
        {
            players.Add(new Player(clientId, name, Score: 0));
            _players.Value = players;
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
        // shared `_players` list instead, then enter each client's scope.)
        foreach (var player in _players.Value)
        {
            using var _ = ReactiveScope.Use(new ClientScope(player.ClientId));
            _selectedAnswer.Value = null;
        }
        _playerAnswers.Clear();
        _questionIndex.Value++;
        _stage.Value = GameStage.Question;
    }
}

public enum GameStage { Lobby, Question, Reveal, Leaderboard, GameOver }
public record Player(int ClientId, string Name, int Score);
public record Question(string Prompt, string[] Choices, int CorrectIndex);
```

## Notes

- **Host detection via `ClientParams.Host`** — the `Host` flag on `ClientParams` is set by query param (`?host=true`). All other clients default to `Host = false`. Don't try to make the first joiner the host; URL-driven role is robust and lets the host reload without losing the role.
- **`app.ClientJoinedAsync` and `ClientLeftAsync` are 1-arg async handlers** — `async args => { ... }`, never `async () => { ... }`. Wrong arity produces CS1593.
- **`ReactiveScope.ClientId` is `int`, not `string`.** `app.Clients[id]?.Parameters` is the read path. There is **NO** `app.ClientSessionId`, **NO** `app.ClientParameters`, **NO** `app.Clients.All`, **NO** `IClientCollection.Current`. Inside event handlers, wrap with `using var _ = ReactiveScope.Use(new ClientScope(args.ClientSessionId))` to enter the joining client's scope.
- **Mix shared + per-client state explicitly.** The current question, players list, and game stage are `Reactive<T>` (everyone sees same value). Each player's name, has-joined state, and selected answer are `ClientReactive<T>` (each client sees their own). Trying to make a single `Reactive<Dictionary<int, T>>` work for per-client state defeats the purpose — `ClientReactive<T>` auto-scopes for free.
- **Server-side bookkeeping uses `ConcurrentDictionary<int, …>`** keyed by `ClientSessionId` — for state that doesn't need to be reactive (vote tallies, timestamps, internal counters). Only push to `Reactive`/`ClientReactive` when the UI needs to refresh.
- **Don't iterate `app.Clients` to reset per-client state.** `IClientCollection<T>` exposes only `Count` and the indexer. To touch every client's `ClientReactive` value, walk your shared `_players` list and `ReactiveScope.Use(new ClientScope(p.ClientId))` per entry.
- **`Reactive<T>` constructor must take an explicit initial value** — `new Reactive<List<Player>>([])`, `new Reactive<int>(0)`, `new ClientReactive<int?>((int?)null)`. Bare `new Reactive<T>()` produces CS0121 ambiguous-call.
- **Game flow as a state machine via `Reactive<GameStage>`**: Lobby → Question → Reveal → Leaderboard → (next round or GameOver). UI branches on `_stage.Value`; transitions happen in host-only handlers.

## See also

- `chatbot-streaming` — the single-LLM-conversation single-client variant.
- `kanban-multi-column` — shared state with mutation buttons (no host role).
- `app-structure` (top-level guide) — `[App]`, partial class, `IApp<TSessionIdentity, TClientParameters>`, ClientParameters via query-param.
- `reactive-state` (top-level guide) — `Reactive<T>` vs `ClientReactive<T>` mechanics, `ReactiveScope.Use` pattern.
