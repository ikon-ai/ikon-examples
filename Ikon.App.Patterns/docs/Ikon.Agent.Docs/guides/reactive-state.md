# Reactive State

## Reactive State Management

### Basic Reactive Types

```csharp
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
```

### Persistent Reactives — survive app restarts

```csharp
// DEFAULT for app state — one bucket per SessionIdentity (the app's routing key)
private readonly PersistentSessionReactive<MyState> _state = new(new MyState());

// App-wide (rare) — same value for everyone in the space
private readonly PersistentReactive<int> _totalVisits = new(0);

// Follows a user across all of their client sessions
private readonly PersistentUserReactive<Prefs> _prefs = new(new Prefs());

// Persisted lists — same mutation-notifies contract as ReactiveList<T>
private readonly PersistentReactiveList<TodoItem> _todos = new();        // app-wide
private readonly PersistentUserReactiveList<Bookmark> _bookmarks = new(); // per-user
```

Heuristic: if you can't articulate why it should be `Global` or `User`, use `PersistentSessionReactive<T>`. Persisted values load in parallel before `Main()` runs and save on graceful shutdown — read and write them like any other reactive. Never write runtime state to `app.DataDirectory`; it is read-only in cloud.

Backends (passed as `backend:`):

```csharp
// Default — Private S3-backed cloud asset
private readonly PersistentSessionReactive<Prefs> _defaultBackend = new(new Prefs());

// Public asset URL needed (uploaded images, published files — never sensitive data)
private readonly PersistentSessionReactive<byte[]> _logo
    = new([], backend: PersistenceBackend.Public);

// Small, frequently-mutated value (counters, status flags). Requires a postgres DB declared
// created with 'ikon app db create --name main'. Omit postgresDatabase if there is only one.
private readonly PersistentSessionReactive<long> _counter
    = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
```

`_logo.PublicUrl` is null until the first save completes.

Use `key: "..."` only when constructing reactives in a loop — pass a stable identifier you own, not `Guid.NewGuid()` (which orphans data on restart). Field names already provide stable keys for normal field-initialized reactives.

For the full reference (anti-patterns, when to drop down to `Asset.Instance` directly, save semantics) see the persistent-state guide composed below.

### Scope Requirements

**Never access `ClientReactive` or `UserReactive` values via `.Value` outside a scope.** `Main()` runs before any client/user scope exists, and a background task carries none. `.Value` reads and writes must happen inside `UI.Root()` or inside event callbacks (onClick, onSubmit, etc.), which run within a scope — anywhere else `.Value` throws an `InvalidOperationException` naming the fix. From background code, name the target instead: `_clientTheme.SetFor(clientSessionId, "dark")` / `_clientTheme.ValueFor(clientSessionId)`. Use `ReactiveScope.Use()` when a whole region needs the scope.

```csharp
// WRONG — crashes at startup, no user scope active
public async Task Main()
{
    if (_hasJoined.Value) { /* ... */ }  // UserReactive — throws InvalidOperationException
    RenderTavern();
}
```

```csharp
// CORRECT — branch inside UI.Root() where scopes are active
public async Task Main()
{
    UI.Root([Page.Default], content: view =>
    {
        if (_hasJoined.Value) { RenderTavern(view); }  // OK — inside UI.Root()
        else { RenderEntry(view); }
    });
}
```

### Value Mutation

```csharp
// Simple assignment
_count.Value = 42;

// List mutation — call the method on the ReactiveList itself; each call notifies once
_items.Add(newItem);
_items.RemoveAll(i => i.Done);
_items.Update(list => list.Select(i => i with { Done = true }));  // whole-list transform, one notification
_items.Value = imported;  // assignment replaces the whole content (same as ReplaceAll)

// Record mutation
_config.Value = _config.Value with { Theme = "dark" };
```

On a `ReactiveList<T>`, reads (`Value`, `Count`, indexer, enumeration) are `IReadOnlyList<T>` — `_items.Value.Add(x)` does not compile; `_items.Add(x)` is the spelling. Every mutator (`Add`, `Remove`, `RemoveAll`, indexer-set, `Update`) fires one notification on its own, so there is no `NotifyUpdate` to remember for them. `NotifyUpdate()` is the escape hatch for the one case the mutators can't see: mutating an item in place (`tracker.Progress = 47; _items.NotifyUpdate();` for a mutable POCO stored in the list). Each mutation copies the list, so batch with `AddRange` / `ReplaceAll` / `Update` instead of per-item calls in a loop.

### Complete Example: Shared Messages + Per-Client Input

```csharp
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
```

### ReactiveScope Context

Inside UI callbacks, access the current client/user context. `app` does not have a `ClientId` property. Always use `ReactiveScope.ClientId` inside UI callbacks.

```csharp
var clientId = ReactiveScope.ClientId;

// From background code (or to reach another client): name the target, no scope needed
_clientTheme.SetFor(clientId, "dark");
var theme = _clientTheme.ValueFor(clientId);

// Scope a whole region instead when several reads/writes belong to the same client
using var _ = ReactiveScope.Use(new ClientScope(clientId));
_clientTheme.Value = "dark"; // Now targets the specified client
```

---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Reactive
  static class ClientReactive
    // Each client's value is initialized by the factory, which receives the client session id.
    static ClientReactive<T> Create<T>(Func<int, T> factory)
  // Same reactive contract as Reactive<T>, partitioned per client session: each connected client holds its own value, even two clients of the same user (use UserReactive<T> when the value should instead be shared across a user's sessions). .Value resolves against the active client scope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new ClientScope(...)) block — and throws when none is active. Background work carries no client scope, so name the session instead via SetFor / ValueFor.
  class ClientReactive<T> : Reactive<T, ClientScope>
    ctor(T initialValue)
    void SetFor(int clientSessionId, T value)
    void UpdateFor(int clientSessionId, Func<T, T> mutator)
    T ValueFor(int clientSessionId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    ctor(IEqualityComparer<TKey> comparer)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, IEqualityComparer<TKey> comparer)
    void ClearFor(int clientSessionId)
    bool RemoveFor(int clientSessionId, TKey key)
    void SetFor(int clientSessionId, TKey key, TValue value)
    void UpdateFor(int clientSessionId, Action<Dictionary<TKey, TValue>> transform)
    IReadOnlyDictionary<TKey, TValue> ValueFor(int clientSessionId)
  // Shorthand for ReactiveEffect<ClientScope>: each connected client gets its own runner with independent cancel/queue, materialized on first dep change inside that client's scope.
  class ClientReactiveEffect : ReactiveEffect<ClientScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    ctor(IEqualityComparer<T> comparer)
    ctor(IEnumerable<T> initialItems, IEqualityComparer<T> comparer)
    bool AddFor(int clientSessionId, T item)
    void ClearFor(int clientSessionId)
    bool RemoveFor(int clientSessionId, T item)
    void UpdateFor(int clientSessionId, Action<HashSet<T>> transform)
    IReadOnlyCollection<T> ValueFor(int clientSessionId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    void AddFor(int clientSessionId, T item)
    void ClearFor(int clientSessionId)
    bool RemoveFor(int clientSessionId, T item)
    void UpdateFor(int clientSessionId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    IReadOnlyList<T> ValueFor(int clientSessionId)
  interface IReactive
    long Version { get; }
    // Fires whenever this reactive's value changes (in any scope, for scoped variants). Payload-free so a single subscription can be taken across heterogeneous reactives — handlers fetch the new value via .Value when they need it.
    event Action? Changed
    // Fires with the scope-derived session id whose value just changed: always 0 for unscoped reactives, the hash of the scope for scoped variants. Lets external subscription routing fan out to only the clients whose scope matches the changed signal.
    event Action<int>? SessionChanged
  static class MountReactive
    // Each mount's value is initialized by the factory, which receives the mount id.
    static MountReactive<T> Create<T>(Func<string, T> factory)
  // Same reactive contract as Reactive<T>, partitioned per Parallax mount an app declares via Mounts (e.g. independent message history for an embedded "aiCanvas" mount vs the "ikon-ui" page). For state shared across a client's mounts use ClientReactive<T>; across all clients use Reactive<T>. .Value resolves against the MountScope active during a render iteration — typically anywhere inside UI.Root() — and throws otherwise. Background work carries no mount scope, so name the mount instead via SetFor / ValueFor.
  class MountReactive<T> : Reactive<T, MountScope>
    ctor(T initialValue)
    void SetFor(string mountId, T value)
    void UpdateFor(string mountId, Func<T, T> mutator)
    T ValueFor(string mountId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per mount exactly like MountReactive<T>. Important: Must be accessed inside a render iteration where MountScope is active — typically anywhere inside UI.Root().
  class MountReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    void ClearFor(string mountId)
    bool RemoveFor(string mountId, TKey key)
    void SetFor(string mountId, TKey key, TValue value)
    void UpdateFor(string mountId, Action<Dictionary<TKey, TValue>> transform)
    IReadOnlyDictionary<TKey, TValue> ValueFor(string mountId)
  // Shorthand for ReactiveEffect<MountScope>: each Parallax mount gets its own runner, materialized on first dep change inside that mount's scope.
  class MountReactiveEffect : ReactiveEffect<MountScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per mount exactly like MountReactive<T>. Important: Must be accessed inside a render iteration where MountScope is active — typically anywhere inside UI.Root().
  class MountReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    bool AddFor(string mountId, T item)
    void ClearFor(string mountId)
    bool RemoveFor(string mountId, T item)
    void UpdateFor(string mountId, Action<HashSet<T>> transform)
    IReadOnlyCollection<T> ValueFor(string mountId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per mount exactly like MountReactive<T>. Important: Must be accessed inside a render iteration where MountScope is active — typically anywhere inside UI.Root().
  class MountReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    void AddFor(string mountId, T item)
    void ClearFor(string mountId)
    bool RemoveFor(string mountId, T item)
    void UpdateFor(string mountId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    IReadOnlyList<T> ValueFor(string mountId)
  enum PersistenceBackend
    // Asset storage on private S3-style cloud files. Explicitly opts the value out of the Default routing — pick it when a structured value must stay on asset storage even though the app has its built-in Postgres database.
    Private
    // Asset storage on public S3-style cloud files. The reactive exposes a PublicUrl accessor so the value can be linked to from the open web.
    Public
    // Postgres key-value row in a database the app declares in ikon-config.toml. Pass the database name (matching the Databases = ["name:postgres"] entry) when constructing the reactive; with a single declared database the name can be omitted.
    Postgres
    // The platform picks the store: structured values go to the app's built-in app database when the session has one, while binary payloads (byte[]) — and sessions without a database — use private asset storage. The default for every persistent reactive that does not name a backend.
    Default
  enum PersistenceScope
    None
    // Persisted for the app within its space, shared across all session identities and users. Use for app-wide configuration that one app instance owns.
    Global
    // Persisted per session identity (the routing key the app declares as its TSessionIdentity): the same identity shares one value, different identities have separate values.
    Session
    // The current primary user's id is part of the storage key, so each user has their own value.
    User
  static class Reactive
    // Runs action on a background task and assigns its result to reactiveValue when it completes, so subscribers react without the caller awaiting. Exceptions go to onError when provided and are logged otherwise; cancellation leaves the reactive value unchanged.
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
    // Like Run<T>, additionally passing token to the action so it can observe cancellation.
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
  // Reading Value during a UI render registers a dependency; writing a changed value re-renders only the parts that read it. An unscoped Reactive<T> holds one value shared across all clients and is accessible anywhere. For per-client state use ClientReactive<T>; for per-user state (shared across a user's sessions) use UserReactive<T>. Those scoped variants resolve .Value against the active scope, so it must be read inside one — UI.Root(), an action callback, or a ReactiveScope.Use() block — and throw otherwise; background work (a Task.Run loop, a timer, an endpoint handler) has no scope and names its target instead via SetFor(id, value) / ValueFor(id).
  class Reactive<T> : IReactive
    // Creates a reactive whose initial value is default(T). Call as new Reactive<T>() — the UseDefault parameter is only an overload disambiguator and is never passed explicitly.
    ctor(UseDefault _ = default)
    ctor(T initialValue)
    // Unlike Value, does not register a dependency, so reading it inside a render never causes a re-render when the value later changes.
    T Peek { get; }
    // For the scoped variants (ClientReactive<T>, UserReactive<T>, MountReactive<T>) the scope must be active — inside UI.Root(), an action callback, or a ReactiveScope.Use block. From background work that carries no scope, name the target instead: SetFor(id, value) / ValueFor(id).
    T Value { get; set; }
    // Monotonic change counter for the currently-active scope's value, incremented on every write or NotifyUpdate. Lets consumers detect changes cheaply without comparing values.
    long Version { get; }
    // Fluent (returns this). Use only for runtime-only caches rebuilt from their own backing store after a reload — capturing non-serializable or cyclic graphs otherwise fails noisily. Does not affect long-term persistence, which applies only to non-None PersistenceScopes.
    Reactive<T> ExcludeFromHotReloadCapture()
    // The escape hatch for in-place mutation of a stored value (e.g. a mutable object the reactive holds), which the setter never observes. Prefer Update, which mutates and notifies under the scope's lock in one step.
    void NotifyUpdate()
    override string ToString()
    // Runs mutator under the scope's lock so concurrent read-modify-writes serialize instead of racing, and fires the change notification exactly once.
    void Update(Func<T, T> mutator)
    // Unwraps to Value — a TRACKED read: used during render it registers a dependency, and for scoped variants it throws InvalidOperationException when no scope is active. Not a cheap unwrap; use Peek to read without tracking.
    static implicit operator T(Reactive<T> r)
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
  // Base class for scoped reactive variables: each distinct TScope instance gets its own value, resolved from the active scope. Use directly only for custom scope types — prefer ClientReactive<T> (per-client) or UserReactive<T> (per-user). The required scope must be active when accessing .Value (e.g. inside UI.Root()); otherwise it throws InvalidOperationException.
  class Reactive<T, TScope> : Reactive<T> where TScope : IScopeKey
    ctor(T initialValue)
    ctor(Func<T> initialValue)
    void SetFor(TScope scope, T value)
    void UpdateFor(TScope scope, Func<T, T> mutator)
    T ValueFor(TScope scope)
  static class ReactiveBoolExtensions
    // Sets the flag to true and returns an IDisposable that returns it to false on dispose — the busy-flag pattern without the try/finally. Idempotent: disposing twice is safe.
    static IDisposable AsToken(this Reactive<bool> reactive)
  // Mutation helpers for a Reactive<T> that wraps a mutable collection: they mutate the underlying instance AND fire the change notification in one call, running through the locked Reactive<T>.Update so concurrent mutations serialize. Meant only for the collections with no reactive equivalent yet (Reactive<HashSet<T>>) and legacy Reactive<List<T>> / Reactive<Dictionary<TKey, TValue>> code — prefer ReactiveList<T> / ReactiveDictionary<TKey, TValue>, on which these same spellings bind to the copy-on-write instance members instead.
  static class ReactiveCollectionExtensions
    static void Add<T>(this Reactive<List<T>> reactive, T item)
    static bool Add<T>(this Reactive<HashSet<T>> reactive, T item)
    static void AddRange<T>(this Reactive<List<T>> reactive, IEnumerable<T> items)
    static void Clear<T>(this Reactive<List<T>> reactive)
    static void Clear<T>(this Reactive<HashSet<T>> reactive)
    static void Clear<TKey, TValue>(this Reactive<Dictionary<TKey, TValue>> reactive)
    static void Insert<T>(this Reactive<List<T>> reactive, int index, T item)
    // Escape hatch for mutations the typed helpers don't cover (e.g. sorting in place, swapping items, clearing+repopulating). The mutator runs on the live reference under the Reactive<T>.Update lock; the change notification fires after it returns. Use the typed helpers (Add<T>, Remove<T>, …) when one fits.
    static void Mutate<T>(this Reactive<T> reactive, Action<T> mutator)
    static bool Remove<T>(this Reactive<List<T>> reactive, T item)
    static bool Remove<T>(this Reactive<HashSet<T>> reactive, T item)
    static bool Remove<TKey, TValue>(this Reactive<Dictionary<TKey, TValue>> reactive, TKey key)
    static int RemoveAll<T>(this Reactive<List<T>> reactive, Predicate<T> match)
    static void RemoveAt<T>(this Reactive<List<T>> reactive, int index)
    static void Set<TKey, TValue>(this Reactive<Dictionary<TKey, TValue>> reactive, TKey key, TValue value)
  // Reads track a dependency exactly like Reactive<T> (reading Value, Count, the indexer, TryGetValue, or enumerating during render). Every mutation method fires exactly one notification on its own — _byId[key] = value is the whole call. Reactive<T>.NotifyUpdate is the escape hatch for the one case the mutators cannot see: mutating a stored value in place (feed.Messages.Add(m); _feeds.NotifyUpdate();). Copy-on-write: every mutation runs under the lock and replaces the backing dictionary with a fresh copy, so concurrent mutations serialize and any dictionary handed out earlier is a stable snapshot. Each mutation copies the whole dictionary, so for batches prefer the single-notify bulk ops (ReplaceAll, Update) over per-key calls in a loop.
  class ReactiveDictionary<TKey, TValue> : Reactive<Dictionary<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // Keys are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase); the comparer is preserved across every copy-on-write mutation, so the custom key semantics hold for the life of the dictionary.
    ctor(IEqualityComparer<TKey> comparer)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, IEqualityComparer<TKey> comparer)
    int Count { get; }
    TValue this[TKey key] { get; set; }
    IEnumerable<TKey> Keys { get; }
    IReadOnlyDictionary<TKey, TValue> Peek { get; }
    IReadOnlyDictionary<TKey, TValue> Value { get; set; }
    IEnumerable<TValue> Values { get; }
    void Add(TKey key, TValue value)
    void Clear()
    bool ContainsKey(TKey key)
    IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    void Mutate(Action<Dictionary<TKey, TValue>> mutator)
    bool Remove(TKey key)
    void ReplaceAll(IEnumerable<KeyValuePair<TKey, TValue>> entries)
    void Set(TKey key, TValue value)
    bool TryAdd(TKey key, TValue value)
    bool TryGetValue(TKey key, out TValue value)
    void Update(Action<Dictionary<TKey, TValue>> transform)
  // Lifecycle (global): • Constructor runs the body once immediately (initial fire). • Each tracked dep's IReactive.Changed event triggers a re-run. • If a dep changes while a previous run is still in flight, the previous run's CancellationToken is cancelled and one follow-up run is queued. Rapid-fire changes coalesce. • IDisposable.Dispose cancels any in-flight run and detaches all dep subscriptions. • Exceptions in the body (other than OperationCanceledException) are logged and do not disable the effect.
  class ReactiveEffect : IDisposable
    // The token cancels when a dep changes mid-run; respect it for clean cancellation.
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // This overload exists so an async () => await ... body binds here as a Task-returning delegate rather than collapsing into the Action overload as async-void — which would report the run complete at the first await and swallow later exceptions. Use the Func<CancellationToken, Task> overload to observe cancellation.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // Unlike the global ReactiveEffect, this variant does NOT fire eagerly at construction — there is no active scope yet. The first dep change observed inside a scope of type TScope instantiates that scope's runner and fires the body for the first time. TScope must be a value type (struct, IScopeKey), a tighter constraint than Reactive<T, TScope>'s IScopeKey; the built-in scopes (ClientScope, UserScope, MountScope) are structs, but a class-based custom scope works with the reactive and not with this effect.
  class ReactiveEffect<TScope> : IDisposable where TScope : struct, IScopeKey
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // Reads track a dependency exactly like Reactive<T> (reading Value, Count, Contains, or enumerating during render). Every mutation method fires exactly one notification on its own — _ids.Add(x) is the whole call. Reactive<T>.NotifyUpdate is the escape hatch for the one case the mutators cannot see: mutating a stored member in place. Copy-on-write: every mutation runs under the lock and replaces the backing set with a fresh copy, so concurrent mutations serialize and any set handed out earlier is a stable snapshot. Each mutation copies the whole set, so for batches prefer the single-notify bulk ops (UnionWith, ExceptWith, ReplaceAll, Update) over per-item calls in a loop.
  class ReactiveHashSet<T> : Reactive<HashSet<T>>, IReadOnlyCollection<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Members are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase); the comparer is preserved across every copy-on-write mutation, so the custom membership semantics hold for the life of the set.
    ctor(IEqualityComparer<T> comparer)
    ctor(IEnumerable<T> initialItems, IEqualityComparer<T> comparer)
    int Count { get; }
    IReadOnlyCollection<T> Peek { get; }
    IReadOnlyCollection<T> Value { get; set; }
    bool Add(T item)
    void Clear()
    bool Contains(T item)
    void ExceptWith(IEnumerable<T> other)
    IEnumerator<T> GetEnumerator()
    void Mutate(Action<HashSet<T>> mutator)
    bool Remove(T item)
    void ReplaceAll(IEnumerable<T> items)
    void UnionWith(IEnumerable<T> other)
    void Update(Action<HashSet<T>> transform)
  // Reads track a dependency exactly like Reactive<T> (reading Value, Count, the indexer, or enumerating during render). Every mutation method fires exactly one notification on its own — _items.Add(x) is the whole call. Reactive<T>.NotifyUpdate is the escape hatch for the one case the mutators cannot see: mutating an item in place (tracker.Progress = 47; _items.NotifyUpdate();). Copy-on-write: every mutation runs under the lock and replaces the backing list with a fresh copy, so concurrent mutations serialize and any list handed out earlier is a stable snapshot. Each mutation copies the whole list, so for batches prefer the single-notify bulk ops (AddRange, ReplaceAll, Update) over per-item calls in a loop.
  class ReactiveList<T> : Reactive<List<T>>, IReadOnlyList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    int Count { get; }
    T this[int index] { get; set; }
    IReadOnlyList<T> Peek { get; }
    IReadOnlyList<T> Value { get; set; }
    void Add(T item)
    void AddRange(IEnumerable<T> items)
    void Clear()
    bool Contains(T item)
    IEnumerator<T> GetEnumerator()
    int IndexOf(T item)
    void Insert(int index, T item)
    void Mutate(Action<List<T>> mutator)
    bool Remove(T item)
    int RemoveAll(Predicate<T> match)
    void RemoveAt(int index)
    void ReplaceAll(IEnumerable<T> items)
    void Sort(Comparison<T> comparison)
    void Update(Func<IReadOnlyList<T>, IEnumerable<T>> transform)
  // A scope stack supporting multiple overlapping scope types (Client, User, Tenant, etc.), each tracked independently. Scope changes are automatically mirrored to Log.Instance.
  static class ReactiveScope
    static int ClientId { get; }
    static int? ClientIdOrNull { get; }
    static IList<IScopeKey> Current { get; }
    static string MountId { get; }
    static string? MountIdOrNull { get; }
    static string UserId { get; }
    static string? UserIdOrNull { get; }
    static void Add(IScopeKey scope)
    static TScope Get<TScope>() where TScope : struct, IScopeKey
    static IScopeKey GetByName(string name)
    static TScope? TryGet<TScope>() where TScope : struct, IScopeKey
    static bool TryGet<TScope>(out TScope scope) where TScope : struct, IScopeKey
    static IScopeKey? TryGetByName(string name)
    static IDisposable Use(IScopeKey scope)
    static IDisposable Use(params IScopeKey[] scopes)
  // Marker type for the default-value Reactive<T> constructor. Never pass it explicitly — write new Reactive<T>() and the value starts at default(T); passing any argument at all selects the value constructor.
  readonly struct UseDefault
  // Same reactive contract as Reactive<T>, partitioned per user and shared across that user's client sessions (use ClientReactive<T> when each client needs its own value). .Value resolves against the active user scope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throws when none is active. Background work carries no user scope, so name the user instead via SetFor / ValueFor.
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue)
    ctor(Func<string, T> initialValue)
    void SetFor(string userId, T value)
    void UpdateFor(string userId, Func<T, T> mutator)
    T ValueFor(string userId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    void ClearFor(string userId)
    bool RemoveFor(string userId, TKey key)
    void SetFor(string userId, TKey key, TValue value)
    void UpdateFor(string userId, Action<Dictionary<TKey, TValue>> transform)
    IReadOnlyDictionary<TKey, TValue> ValueFor(string userId)
  // Shorthand for ReactiveEffect<UserScope>: each distinct user gets its own runner; the same user across multiple sessions shares one runner.
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    bool AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Action<HashSet<T>> transform)
    IReadOnlyCollection<T> ValueFor(string userId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Seeds each user's list from their id the first time that user's scope resolves — the list counterpart of UserReactive<T>'s factory constructor. Without it the only way to give a per-user list a computed starting point was UserReactive<List<T>>, which is build error IKON002: a reactive wrapping a mutable collection notifies on assignment only, so a caller mutating the inner list silently updates nothing.
    ctor(Func<string, IEnumerable<T>> initialItems)
    void AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    IReadOnlyList<T> ValueFor(string userId)

namespace Ikon.Common.Core.Scope
  // Each time a client connects to the server, it gets a new ClientScope with a unique Id (session ID). This scope is used by ClientReactive<T> to partition state per client. Relationship to UserScope: Multiple ClientScopes can belong to the same user. For example, a user connected from two clients has two different ClientScope IDs but the same UserScope ID. Lifecycle: Active during UI rendering inside UI.Root(). Automatically established by the framework for each client iteration.
  readonly struct ClientScope : IScopeKey
    ctor(int sessionId)
    ctor(Context context)
    int Id { get; }
    string Name { get; }
  readonly struct CustomScope : IScopeKey
    ctor(string name, string id)
    string Id { get; }
    string Name { get; }
  interface IScopeKey
    object Id { get; }
    string Name { get; }
  // Pushed by the framework alongside UserScope / ClientScope during the per-(client, mount) render iteration in ReactiveRoot.RunAsync.
  readonly struct MountScope : IScopeKey
    ctor(string mountId)
    string Id { get; }
    string Name { get; }
    // The mount id every Ikon app emits today on its single Parallax stream; apps that don't override IAppBase.Mounts render under this id.
    const string DefaultMountId
  readonly struct OperationScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  readonly struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
  // Machine-triggered work has no ClientScope and no UserScope, so without this its cost lands in the space's totals attached to nothing and a schedule quietly burning credits is indistinguishable from the app's ordinary use. Every log event carries the active scopes, so the cost of an AI call made inside a trigger handler is attributed by the ambient scope alone — call sites need no change. Scoped to the invocation rather than the session on purpose: a session woken by cron goes on to serve clients, and their spend is theirs, not the schedule's. The values match the backend's AppSessionSource spelling, so the trigger a cost row carries reads the same as the source stamped on the session that ran it.
  readonly struct TriggerScope : IScopeKey
    ctor(string kind)
    string Id { get; }
    string Name { get; }
    const string Cron
    const string Endpoint
  // Identifies a logical user across their multiple client sessions. Used by UserReactive<T> to share state across a user's multiple connected clients. Lifecycle: Active during UI rendering inside UI.Root(). Automatically established by the framework alongside ClientScope.
  readonly struct UserScope : IScopeKey
    ctor(string userId)
    ctor(Context context)
    string Id { get; }
    string Name { get; }

---

# Ikon Persistent State Guide

How to persist app state across restarts. Read this before reaching for files or hand-rolled storage.

## TL;DR — what to pick

```csharp
// Default for almost everything you want to persist:
private readonly PersistentSessionReactive<MyState> _state = new(new MyState());
```

Then from any method:

```csharp
// Read and write like any reactive:
_state.Value = next;
var current = _state.Value;
```

That's it. The framework loads from cloud storage before `Main()` runs and saves on graceful shutdown. No file paths, no `Directory.CreateDirectory`, no AssetUri ceremony.

## Three scopes — pick by who shares the value

| Class | Same value seen by | When to pick |
|---|---|---|
| `PersistentReactive<T>` | All session identities, all users (app-wide within space) | Truly app-wide config. Rare — most apps don't need this. |
| **`PersistentSessionReactive<T>`** | The same SessionIdentity (the app's routing key) | **Default.** One app instance = one bucket of state. |
| `PersistentUserReactive<T>` | The same UserId, across all of their sessions | State that follows a user (preferences, history, profile). |

Heuristic: if you can't articulate why it should be `Global` or `User`, it's `Session`. The session identity is what the app already declared as its routing key — using the same key for storage means your data partitions match how your app instances partition.

**Composer drafts and other "the user's words in progress" belong in USER scope** (`UserReactive<string>`, or `PersistentUserReactive<string>` to survive app restarts) — never `ClientReactive`. A page reload mints a NEW client session, so per-client state evaporates with it by design: a draft bound to `ClientReactive` is lost on every reload. User scope survives reloads and follows the user across tabs. Do NOT reach for browser storage (localStorage/sessionStorage in a custom component) to patch this — draft persistence is server state's job on this platform.

Each scope also has persistent collection variants — the durable counterparts of `ReactiveList<T>`, `ReactiveHashSet<T>` and `ReactiveDictionary<TKey, TValue>`. Use these instead of wrapping a collection in a `Persistent...Reactive<List<T>>`:

| Collection | App-wide | Per session identity | Per user |
|---|---|---|---|
| list | `PersistentReactiveList<T>` | `PersistentSessionReactiveList<T>` | `PersistentUserReactiveList<T>` |
| set | `PersistentReactiveHashSet<T>` | `PersistentSessionReactiveHashSet<T>` | `PersistentUserReactiveHashSet<T>` |
| dictionary | `PersistentReactiveDictionary<TKey, TValue>` | `PersistentSessionReactiveDictionary<TKey, TValue>` | `PersistentUserReactiveDictionary<TKey, TValue>` |

The user-scoped classes additionally expose per-user accessors usable outside an active user scope (background tasks): `ValueFor(userId)`, `SetFor(userId, value)`, and `UpdateFor(userId, ...)` on `PersistentUserReactive<T>`; the collection variants have equivalents like `AddFor` / `RemoveFor` / `ClearFor`.

## Backends — the default does the right thing

```csharp
// Default — structured state lands in the app's built-in postgres database
private readonly PersistentSessionReactive<Prefs> _prefs = new(new Prefs());

// byte[] payloads stay on asset storage automatically — no backend parameter needed
private readonly PersistentSessionReactive<byte[]> _snapshot = new([]);

// Public asset URL needed (uploaded images, published files)
private readonly PersistentSessionReactive<byte[]> _logo
    = new([], backend: PersistenceBackend.Public);

// Explicitly target a postgres DB of the app's own
private readonly PersistentSessionReactive<long> _counter
    = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
```

Read the public URL from a method, once a save has happened:

```csharp
var url = _logo.PublicUrl;  // null until first save completes
```

Every app gets a built-in Postgres database named `app`. Nothing declares it and nothing has to
create it: the platform provisions it the first time the app asks for a database, and it is
quota-free. The default backend routes on the storage doctrine — structured state belongs in
Postgres, asset storage is for binaries and public files:

- `Default` (what you get when you name no backend) — structured values (`T` is not `byte[]`) are
  stored as one row in the `ikon_reactive_storage` table of the built-in `app` database.
  `byte[]` payloads go to private asset storage. If the session has no database (older backend,
  degraded provisioning, plain local run), everything falls back to asset storage — same behavior,
  different shelf — and the app logs one warning naming the fallback.
- `Private` — S3-backed private cloud file, explicitly. Pick it only when a structured value must
  stay on asset storage despite the default.
- `Public` — asset storage with a public URL on `PublicUrl`. **Only** when the value will be
  linked to from the open web. Don't use for anything sensitive.
- `Postgres` — a row in a postgres DB of the app's own, created with
  `ikon app db create --name <name>`. If the space holds only one such database, omit
  `postgresDatabase`; with several, name the one you want.

Existing data migrates by itself: when a structured value first loads from the `app` database and finds
no row, the old asset location is read and the value is copied into Postgres, so the next load hits
the row. The old asset blob is left in place. Apps already using a declared DB with
`backend: PersistenceBackend.Postgres` are untouched by all of this.

Every load and save logs its destination at debug level (member name, scope, and the postgres
database or asset path), so "where is my data?" is answered by the app's logs.

Backend ≠ scope. Scope decides who sees the value; backend decides where it lives. They're chosen independently.

### Database tiers

Every provisioned Postgres database lives on a tier — `shared-dense`, `shared`, or
`dedicated-small` — which decides how densely it is packed onto an instance and how many
connections it gets. Your plan maps the default tier; you never pick one at declaration time.

A live database can move to another tier without redeploying:

```
ikon app db tier set dedicated-small
```

With several declared databases, name the one to move with `--database <name>`. The platform
copies the data to an instance of the new tier, verifies it, and switches connections over —
the database keeps its name and credentials, but expect open connections to drop briefly while
the data moves (sessions reconnect automatically). `ikon app db list` shows each database's tier
and the state of an in-flight migration.

## The `key:` parameter — only for loops

```csharp
// WRONG — every loop iteration creates a reactive with the SAME stable id.
foreach (var camera in cameras)
{
    var baseline = new PersistentSessionReactive<byte[]>([]);  // collisions!
}

// RIGHT — explicit stable key derived from the dynamic identity.
foreach (var camera in cameras)
{
    var baseline = new PersistentSessionReactive<byte[]>(
        [],
        key: $"baseline:{camera.Id}");
}
```

Pass a stable identifier the app owns. Not `Guid.NewGuid()` — that changes on every restart and orphans the old data. Don't reuse the same key across different types or scopes.

You almost never need `key:` for fields. Field names are already stable. Only reach for it when constructing reactives in a loop or based on runtime identity.

## Save semantics

- **Load**: parallel for all persistent reactives, finishes before `Main()` runs. Your code sees persisted values from the start. (User-scoped reactives load per user: the primary user's partition is preloaded before `Main()`; other users' partitions load lazily the first time their scope is touched.)
- **Save**: parallel for all persistent reactives, on `StoppingAsync` (graceful shutdown).
- **Crashes lose unsaved changes.** If a value must survive a crash, also write it through a side-channel (webhook, direct DB, …). Don't try to bolt save-on-every-change on top — for high-write durability, store it in postgres directly through `app.Databases`.

## Erasing a user's state

`ClearFor(userId)` on any user-scoped persistent reactive erases that user's partition: the in-memory value is dropped (the next read sees the initial value) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when the value lives in Postgres with read-through, so an erased value cannot resurrect on the next load. The deletion runs in the background; the user leaves the shutdown save immediately, so a cleared user is never re-persisted.

To erase everything the app has persisted about a user — a user-data-erasure (GDPR) request — use the app-level helper:

```csharp
await app.EraseUserStateAsync(userId);
```

It applies `ClearFor` semantics to every registered user-scoped persistent reactive and additionally deletes every stored row under the user's storage scope key — including rows left behind by fields that no longer exist in the code — and completes only when the stored state is gone. Global- and session-scoped values are untouched: they are shared state, not per-user data.

## Schema-versioned state (`.tp` types)

A plain record works fine — until you rename a property. The stored JSON still carries the old
name, the deserializer doesn't recognize it, and the value silently resets. If a persisted type is
worth evolving, define it as a Teleport data schema instead of a record and the `.tp` compat
contract comes with it:

```toml
# schema/PlayerProfile.tp
type = "PlayerProfile"
version = 2
data = true

[fields]
DisplayName = "string"
Score = "int32"

# Renamed to DisplayName in v2.
[obsolete]
Nickname = "string"
```

Bumping `version` obliges you to say what the old data means — the generated code calls a
migration you write in your partial class, so forgetting it is a compile error, not a data loss:

```csharp
public sealed partial class PlayerProfile
{
    static void UpgradeFrom1(PlayerProfile value, PlayerProfile.RetiredFields? retiredFields)
    {
        if (string.IsNullOrEmpty(value.DisplayName) && retiredFields?.Nickname is { } nickname)
        {
            value.DisplayName = nickname;
        }
    }
}
```

Then use it like any other persisted value — `new PersistentSessionReactive<PlayerProfile>(new())`.
Nothing else changes; the contract rides inside the stored payload. Three guarantees come with it:

- **Renames survive.** A removed property moves to the schema's `[obsolete]` ledger; on load its
  stored value arrives typed in `RetiredFields` and your `UpgradeFrom` step decides what it means.
- **Old branches can't destroy new fields.** Keys this build doesn't know — at the top level and
  inside nested section objects — are carried through a load→save cycle untouched, so a rollback
  build saving over a newer build's data keeps the newer fields. Honest scope: the preservation
  window is one process's load→save cycle, and saves are whole-blob last-writer-wins across
  instances — with `[Sharded]`/multi-instance saves, a stale instance's save can still resurrect
  values another instance changed (merge-on-save is future work). Arrays are not merged either;
  the last saved list wins whole.
- **Newer data is never downgraded — or destroyed.** If the stored payload was written by a newer
  schema version than this build understands (or the envelope is malformed, or the value fails to
  deserialize), the app sees the default value and the save re-emits the stored payload verbatim.
  One warning names the reactive and both versions at load, and the first write to such a reactive
  warns once more: **writes made while holding a newer payload update memory but do not persist**
  (`Reactive '<StableId>' holds stored state from a newer schema version; writes ... will not
  persist`).

One asymmetry to know: keys listed in the `[obsolete]` ledger are *consumed* — captured into the
bag, handed to your migration, and stripped from the next save — while unledgered unknown keys are
preserved forever. Retire a key only when this build owns deciding what it means.

Boundaries: the contract applies to single-value `Persistent…Reactive<T>` where `T` is a data-`.tp`
root — the collection variants keep plain behavior for now. Nested `[obsolete.Section]` entries are
neither migrated nor preserved by the state contract yet: **don't retire nested fields on persisted
types**; keep ledger entries at the root. Plain records keep today's behavior exactly; nothing opts
in until the type is a Teleport data schema.

## Anti-patterns — don't do these

- ❌ `Path.Combine(app.DataDirectory, "data")` + `Directory.CreateDirectory(...)` for runtime state. `DataDirectory` is **read-only** in cloud. It's for bundled assets the app reads, not state it writes.
- ❌ Using the `Postgres` backend for binary blobs or image bytes — `byte[]` belongs on asset storage, which the default already does for you.
- ❌ Using `Public` backend for anything sensitive — assets get a real URL on the open web.
- ❌ Constructing AssetUris by hand for state that fits a `PersistentXxxReactive`.
- ❌ Using `Guid.NewGuid()` as `key:` — it changes on restart.
- ❌ Assuming the postgres-backed storage reads or writes through on every access — like the asset backends, the row is only read at load and written at save; in between, the value lives in memory. For read-your-writes durability, go through `app.Databases` directly.

## When to drop down to `Asset.Instance` directly

Reach for `Asset.Instance` (with a hand-built `AssetUri`) only when:
- You need to list files (`Asset.Instance.ListAsync`), not just read/write a known one.
- You need streaming reads/writes for very large files (multi-GB).
- The data isn't naturally a typed reactive value (e.g., a uploaded user file you only ever fetch on demand).

For everything else, use a `PersistentXxxReactive<T>`. It's strictly less code and keeps your state model uniform.
