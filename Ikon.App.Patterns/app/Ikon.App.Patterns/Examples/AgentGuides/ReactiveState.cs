using Microsoft.EntityFrameworkCore;

namespace Ikon.App.Patterns.Examples;

// The mutation catalogue needs its own `_items` — an item type with a `Done` flag — where the
// sortable-list example needs a list of ids. Two topics, two shapes, both verbatim.
internal sealed class ValueMutationExamples
{
    private sealed record Item(bool Done);
    private sealed record Config(string Theme);

    private sealed class Profile
    {
        public string Theme { get; set; } = "light";
    }

    private readonly Reactive<int> _count = new(0);
    private readonly ReactiveList<Item> _items = new();
    private readonly Reactive<Config> _config = new(new Config("light"));
    private readonly Reactive<int> _lastOrderNumber = new(0);
    private readonly Reactive<Profile> _profile = new(new Profile());

    // Reactive writes during a render are discarded, so the example runs from a button and reports
    // the values it left behind — proof it executed, not only that it compiled.
    public static string Run()
    {
        var example = new ValueMutationExamples();
        example.Mutate(new Item(false), [new Item(false), new Item(true)]);

        return example._count.Value == 42 && example._items.Count == 2 && example._config.Value.Theme == "dark"
            ? $"PASS value mutation: count={example._count.Value}, items={example._items.Count}, theme={example._config.Value.Theme}"
            : $"FAIL value mutation: count={example._count.Value}, items={example._items.Count}, theme={example._config.Value.Theme}";
    }

    private void Mutate(Item newItem, List<Item> imported)
    {
        #region example:value-mutation
        // Simple assignment
        _count.Value = 42;

        // List mutation — call the method on the ReactiveList itself; each call notifies once
        _items.Add(newItem);
        _items.RemoveAll(i => i.Done);
        _items.ReplaceWhere(i => !i.Done, i => i with { Done = true });  // swap the matching items, one notification
        _items.Update(list => list.Select(i => i with { Done = true }));  // whole-list transform, one notification
        _items.Value = imported;  // assignment replaces the whole content (same as ReplaceAll)

        // Record mutation
        _config.Value = _config.Value with { Theme = "dark" };

        // Read-modify-write in one step: the lambda runs under the reactive's lock. Update returns
        // nothing, so keep the new value from inside the lambda
        int orderNumber = 0;
        _lastOrderNumber.Update(n => orderNumber = n + 1);

        // A mutable object (a generated .tp class has no with): assigning the same instance back
        // is skipped as unchanged, so change it inside Update, which always notifies
        _profile.Update(p => { p.Theme = "dark"; return p; });
        #endregion
    }
}

// `UI` is an instance member of the app class, so an example that builds a whole screen needs an app
// to hang it off. The holder is that app — the example inside is what a reader writes.
file sealed class ChatExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    #region example:shared-messages-example
    // Shared state — all clients see the same messages
    private readonly ReactiveList<string> _messages = new();

    // Per-client state — each client has their own input
    private readonly ClientReactive<string> _input = new("");

    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column(["h-screen"], content: view =>
            {
                // All clients see the same messages
                view.ScrollArea(autoScroll: true, autoScrollKey: _messages,
                    rootStyle: ["flex-1 min-h-0 px-4"], content: view =>
                {
                    foreach (var msg in _messages)
                    {
                        view.Text([Text.Body, "py-1"], msg);
                    }
                });

                // Each client has their own input
                view.Row(["p-4 gap-2 flex-shrink-0"], content: view =>
                {
                    view.TextField(bind: _input, style: ["default", "flex-1"], placeholder: "Message",
                        onSubmit: async submitted =>
                        {
                            _messages.Add(submitted); // Mutation methods notify on their own
                        },
                        clearOnSubmit: true);
                });
            });
        });
    }
    #endregion
}

// The scope contrast is the same method written two ways, so each way needs a class of its own.
// The wrong way is the one IKON050 exists to flag
#pragma warning disable IKON050
file sealed class ScopeWrongExamples
{
    private readonly UserReactive<bool> _hasJoined = new(false);

    private static void RenderTavern() { }

    #region example:scope-requirements-wrong
    // WRONG — crashes at startup, no user scope active
    public async Task Main()
    {
        if (_hasJoined.Value) { /* ... */ }  // UserReactive — throws InvalidOperationException
        RenderTavern();
    }
    #endregion
}
#pragma warning restore IKON050

file sealed class ScopeRightExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    private readonly UserReactive<bool> _hasJoined = new(false);

    private static void RenderTavern(UIView view) => view.Text(text: "The tavern: the joined player's table and chat");

    private static void RenderEntry(UIView view) => view.Text(text: "The entry screen: pick a name and join the tavern");

    #region example:scope-requirements-right
    // CORRECT — branch inside UI.Root() where scopes are active
    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            if (_hasJoined.Value) { RenderTavern(view); }  // OK — inside UI.Root()
            else { RenderEntry(view); }
        });
    }
    #endregion
}

file sealed class ScreenLoadExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private sealed record Quiz(string Id, string Title);

    private static Task<IReadOnlyList<Quiz>> LoadQuizzesAsync(string userId) =>
        Task.FromResult<IReadOnlyList<Quiz>>([new Quiz("q1", $"Fractions for {userId}")]);

    #region example:load-screen-data
    private readonly ClientReactive<string> _screen = new("home");
    private readonly ClientReactiveList<Quiz> _quizzes = new();
    private readonly ClientReactive<bool> _loadingQuizzes = new(false);

    public void WireUp()
    {
        // The screen a client lands on loads when it joins: the handler runs in that client's scope.
        app.OnClientJoined(async ctx => await OpenQuizzesAsync());
    }

    // Every other screen loads in the handler that opens it.
    private async Task OpenQuizzesAsync()
    {
        _screen.Value = "quizzes";
        _loadingQuizzes.Value = true;

        try
        {
            _quizzes.ReplaceAll(await LoadQuizzesAsync(ReactiveScope.UserId));
        }
        finally
        {
            _loadingQuizzes.Value = false;
        }
    }

    // The render only reads what the handler loaded.
    private void RenderQuizzes(UIView view)
    {
        view.Button([Button.GhostMd], text: "Reload", onClick: async () => await OpenQuizzesAsync());

        if (_loadingQuizzes.Value)
        {
            view.Box([Skeleton.Card]);
            return;
        }

        foreach (var quiz in _quizzes)
        {
            view.Text([Text.Body], quiz.Title);
        }
    }
    #endregion
}

file sealed class ClockExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private sealed record Instrument(string Name, DateTime LastSeenUtc);

    private readonly ReactiveList<Instrument> _instruments = new([new("Rain gauge", DateTime.UtcNow), new("Wind vane", DateTime.UtcNow.AddMinutes(-7))]);

    #region example:time-driven-render
    // One clock for the app. Every render that reads it runs again on each tick.
    private readonly Reactive<DateTime> _now = new(DateTime.UtcNow);

    // Call it once from Main.
    public void StartClock() => _ = TickAsync();

    private async Task TickAsync()
    {
        // Tick only as often as the screen needs: each tick re-renders every client that reads _now.
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        try
        {
            while (await timer.WaitForNextTickAsync(app.StoppingToken))
            {
                _now.Value = DateTime.UtcNow;
            }
        }
        catch (OperationCanceledException)
        {
            // The app instance is stopping, which is how the clock is meant to end
        }
    }

    private void RenderInstruments(UIView view)
    {
        var now = _now.Value;

        foreach (var instrument in _instruments)
        {
            var silent = now - instrument.LastSeenUtc;
            view.Text([Text.Body], silent > TimeSpan.FromMinutes(1) ? $"{instrument.Name}: silent for {silent.TotalMinutes:0} min" : $"{instrument.Name}: live");
        }
    }
    #endregion
}

// Abstract so neither the bundle scan nor the Patterns app registers its [Cron] job, and so the
// gallery declares none of its global values in the Patterns app's space.
file abstract class GlobalValuesExamples
{
    private static Task<IReadOnlyList<string>> LoadLapsedUserIdsAsync() => Task.FromResult<IReadOnlyList<string>>(["user-1"]);

    #region example:global-reactive-values
    // One flag for every instance of the app: each room shows the banner, and the staff screen sets it.
    private readonly PersistentGlobalReactive<bool> _maintenance = new(false, key: "maintenance");

    // A count every instance adds to, kept across deploys.
    private readonly PersistentGlobalReactive<int> _gamesPlayed = new(0, key: "games-played");

    // Players waiting for a match in any room. It starts empty again after a deploy or 7 days without a write.
    private readonly GlobalReactiveList<string> _lobby = new(key: "lobby");

    // One value per user, the same in every instance, so a [Cron] job can write it for the user's own instance.
    private readonly PersistentGlobalUserReactive<int> _streak = new(0, key: "streak");

    // An assignment: the last write the backend accepts wins.
    private void SetMaintenance(bool on) => _maintenance.Value = on;

    // Update and the collection operations run again over a newer value when another instance wrote
    // first, so no game and no player is lost. Keep the function free of side effects.
    private void FinishGame() => _gamesPlayed.Update(count => count + 1);

    private void JoinLobby(string playerName) => _lobby.Add(playerName);

    // The job runs in the instance with an empty identity, where no user scope is active, so it names the user.
    [Cron("0 3 * * *")]
    public async Task ResetLapsedStreaksAsync()
    {
        foreach (var userId in await LoadLapsedUserIdsAsync())
        {
            _streak.SetFor(userId, 0);
        }
    }
    #endregion

    public string Describe() => $"{_maintenance.Value} {_lobby.Count}";
}

internal sealed partial class AgentGuideExamples
{

    #region example:persistent-reactive-scopes
    // Everyone in this instance, kept across restarts and deploys: the default for app state
    private readonly PersistentReactive<MyState> _state = new(new MyState());

    // One user in this instance, across all of their client sessions
    private readonly PersistentUserReactive<Prefs> _prefs = new(new Prefs());

    // Every instance of the app, kept across deploys: a setting, a flag, a counter
    private readonly PersistentGlobalReactive<int> _totalVisits = new(0);

    // One user in every instance of the app: a preference that follows them into every room
    private readonly PersistentGlobalUserReactive<Prefs> _sharedPrefs = new(new Prefs());

    // Collections come in the same scopes. As with ReactiveList<T>, a change to one reruns the renders that read it
    private readonly PersistentReactiveList<TodoItem> _todos = new();
    private readonly PersistentUserReactiveList<Bookmark> _bookmarks = new();
    #endregion

    // Never instantiated: as a field of a class the gallery builds, the Postgres example would register
    // a live reactive against a database named "main" that the Patterns app's space does not have.
    private sealed class PersistentBackendsExamples
    {
        #region example:persistent-reactive-backends
        // Default — the app's built-in database when the session has one; binary payloads and
        // sessions without a database land on private asset storage
        private readonly PersistentReactive<Prefs> _defaultBackend = new(new Prefs());

        // Public asset URL needed (never sensitive data) — the URL serves the stored JSON wrapper,
        // not the raw bytes, so it is no image src; serve media through app.Files.Public
        private readonly PersistentReactive<byte[]> _logo
            = new([], backend: PersistenceBackend.Public);

        // Small, frequently-mutated value (counters, status flags). Requires a postgres DB declared
        // created with 'ikon db create --name main'. Omit postgresDatabase if there is only one.
        private readonly PersistentReactive<long> _counter
            = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
        #endregion

        public string? LogoPublicUrl() => _logo.PublicUrl;

        public long Total() => _counter.Value + (_defaultBackend.Value.DarkMode ? 1 : 0);
    }

    // Field-declaration examples live in their own holder, because two guide topics legitimately
    // use the same field name for different things — `_todos` is a PersistentReactiveList in the
    // persistence example and a ClientReactiveList in the reactive-types one. One class per topic
    // keeps both verbatim.
    private sealed class DocReactiveTypes
    {
        #region example:basic-reactive-types
        // Shared by every client of this instance
        private readonly Reactive<int> _count = new(0);

        // Per-client state (each connected client sees their own value)
        private readonly ClientReactive<string> _theme = new("light");

        // Per-user state (shared across a user's multiple client sessions)
        // If a user connects from phone and desktop, both clients share the same UserReactive values
        private readonly UserReactive<string> _userPref = new("");

        // List state — ReactiveList<T> (shared) / ClientReactiveList<T> / UserReactiveList<T>.
        // Never Reactive<List<T>> in new code; ReactiveList<T> is the list type.
        private readonly ReactiveList<string> _messages = new();
        private readonly ClientReactiveList<TodoItem> _todos = new();
        #endregion

        public int Total => _count.Value + _theme.Value.Length + _userPref.Value.Length
            + _messages.Count + _todos.Count;
    }

    private void DocReactiveScope()
    {
        #region example:reactive-scope
        var clientId = ReactiveScope.ClientId;

        // From background code (or to reach another client): name the target, no scope needed
        _clientTheme.SetFor(clientId, "dark");
        var theme = _clientTheme.ValueFor(clientId);

        // Scope a whole region instead when several reads/writes belong to the same client.
        // UseNested restores the exact previous scopes on dispose and stays out of the log scope
        using var scope = ReactiveScope.UseNested(new ClientScope(clientId));
        _clientTheme.Value = "dark"; // Now targets the specified client
        #endregion

        Log.Instance.Debug($"theme {theme}");
    }
}
