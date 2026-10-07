using Microsoft.EntityFrameworkCore;

namespace Ikon.App.Patterns.Examples;

// The mutation catalogue needs its own `_items` — an item type with a `Done` flag — where the
// sortable-list example needs a list of ids. Two topics, two shapes, both verbatim.
internal sealed class ValueMutationExamples
{
    private sealed record Item(bool Done);
    private sealed record Config(string Theme);

    private readonly Reactive<int> _count = new(0);
    private readonly ReactiveList<Item> _items = new();
    private readonly Reactive<Config> _config = new(new Config("light"));

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
        _items.Update(list => list.Select(i => i with { Done = true }));  // whole-list transform, one notification
        _items.Value = imported;  // assignment replaces the whole content (same as ReplaceAll)

        // Record mutation
        _config.Value = _config.Value with { Theme = "dark" };
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
                    view.TextField(bind: _input, style: ["flex-1"],
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

internal sealed partial class AgentGuideExamples
{

    #region example:persistent-reactive-scopes
    // DEFAULT for app state — one bucket per SessionIdentity (the app's routing key)
    private readonly PersistentSessionReactive<MyState> _state = new(new MyState());

    // App-wide (rare) — same value for everyone in the space
    private readonly PersistentReactive<int> _totalVisits = new(0);

    // Follows a user across all of their client sessions
    private readonly PersistentUserReactive<Prefs> _prefs = new(new Prefs());

    // Persisted lists — same mutation-notifies contract as ReactiveList<T>
    private readonly PersistentReactiveList<TodoItem> _todos = new();        // app-wide
    private readonly PersistentUserReactiveList<Bookmark> _bookmarks = new(); // per-user
    #endregion

    // Never instantiated: as a field of a class the gallery builds, the Postgres example would register
    // a live reactive against a database named "main" that the Patterns app's space does not have.
    private sealed class PersistentBackendsExamples
    {
        #region example:persistent-reactive-backends
        // Default — the app's built-in database when the session has one; binary payloads and
        // sessions without a database land on private asset storage
        private readonly PersistentSessionReactive<Prefs> _defaultBackend = new(new Prefs());

        // Public asset URL needed (never sensitive data) — the URL serves the stored JSON wrapper,
        // not the raw bytes, so it is no image src; serve media through app.Files.Public
        private readonly PersistentSessionReactive<byte[]> _logo
            = new([], backend: PersistenceBackend.Public);

        // Small, frequently-mutated value (counters, status flags). Requires a postgres DB declared
        // created with 'ikon db create --name main'. Omit postgresDatabase if there is only one.
        private readonly PersistentSessionReactive<long> _counter
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
        // Shared across all clients (global state)
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
        using var _ = ReactiveScope.UseNested(new ClientScope(clientId));
        _clientTheme.Value = "dark"; // Now targets the specified client
        #endregion

        Log.Instance.Debug($"theme {theme}");
    }
}
