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
new PersistentSessionReactive<Prefs>(new Prefs());

// Public asset URL needed (uploaded images, published files — never sensitive data)
private readonly PersistentSessionReactive<byte[]> _logo
    = new([], backend: PersistenceBackend.Public);
var url = _logo.PublicUrl;  // null until first save completes

// Small, frequently-mutated value (counters, status flags). Requires a postgres DB declared
// in ikon-config.toml as Databases = ["main:postgres"]. Omit postgresDatabase if there is only one.
private readonly PersistentSessionReactive<long> _counter
    = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
```

Use `key: "..."` only when constructing reactives in a loop — pass a stable identifier you own, not `Guid.NewGuid()` (which orphans data on restart). Field names already provide stable keys for normal field-initialized reactives.

For the full reference (anti-patterns, when to drop down to `Asset.Instance` directly, save semantics) see the persistent-state guide composed below.

### Scope Requirements

**Never access `ClientReactive` or `UserReactive` values via `.Value` outside a scope.** `Main()` runs before any client/user scope exists, and a background task carries none. `.Value` reads and writes must happen inside `UI.Root()` or inside event callbacks (onClick, onSubmit, etc.), which run within a scope — anywhere else `.Value` throws an `InvalidOperationException` naming the fix. From background code, name the target instead: `_clientTheme.SetFor(clientSessionId, "dark")` / `_clientTheme.ValueFor(clientSessionId)`. Use `ReactiveScope.Use()` when a whole region needs the scope.

```csharp
// WRONG — crashes at startup, no user scope active
public async Task Main()
{
    if (_hasJoined.Value) { ... }  // UserReactive — throws InvalidOperationException
    RenderTavern();
}

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
using var _ = ReactiveScope.Use(new ClientScope(clientSessionId));
_clientTheme.Value = "dark"; // Now targets the specified client
```

---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Reactive
  static class ClientReactive
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
  class ClientReactiveEffect : ReactiveEffect<ClientScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
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
    event Action? Changed
    event Action<int>? SessionChanged
  static class MountReactive
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
  class MountReactiveEffect : ReactiveEffect<MountScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
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
    Private
    Public
    Postgres
  enum PersistenceScope
    None
    Global
    Session
    User
  static class Reactive
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
  // Reading Value during a UI render registers a dependency; writing a changed value re-renders only the parts that read it. An unscoped Reactive<T> holds one value shared across all clients and is accessible anywhere. For per-client state use ClientReactive<T>; for per-user state (shared across a user's sessions) use UserReactive<T>. Those scoped variants resolve .Value against the active scope, so it must be read inside one — UI.Root(), an action callback, or a ReactiveScope.Use() block — and throw otherwise; background work (a Task.Run loop, a timer, an endpoint handler) has no scope and names its target instead via SetFor(id, value) / ValueFor(id).
  class Reactive<T> : IReactive
    ctor(UseDefault _ = default)
    ctor(T initialValue)
    // Unlike Value, does not register a dependency, so reading it inside a render never causes a re-render when the value later changes.
    T Peek { get; }
    // For the scoped variants (ClientReactive<T>, UserReactive<T>, MountReactive<T>) the scope must be active — inside UI.Root(), an action callback, or a ReactiveScope.Use block. From background work that carries no scope, name the target instead: SetFor(id, value) / ValueFor(id).
    T Value { get; set; }
    long Version { get; }
    // Fluent (returns this). Use only for runtime-only caches rebuilt from their own backing store after a reload — capturing non-serializable or cyclic graphs otherwise fails noisily. Does not affect long-term persistence, which applies only to non-None PersistenceScopes.
    Reactive<T> ExcludeFromHotReloadCapture()
    // The escape hatch for in-place mutation of a stored value (e.g. a mutable object the reactive holds), which the setter never observes. Prefer Update, which mutates and notifies under the scope's lock in one step.
    void NotifyUpdate()
    override string ToString()
    // Runs mutator under the scope's lock so concurrent read-modify-writes serialize instead of racing, and fires the change notification exactly once.
    void Update(Func<T, T> mutator)
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
    static IDisposable AsToken(this Reactive<bool> reactive)
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
    bool Remove(TKey key)
    void ReplaceAll(IEnumerable<KeyValuePair<TKey, TValue>> entries)
    bool TryAdd(TKey key, TValue value)
    bool TryGetValue(TKey key, out TValue value)
    void Update(Action<Dictionary<TKey, TValue>> transform)
  // Lifecycle (global): • Constructor runs the body once immediately (initial fire). • Each tracked dep's IReactive.Changed event triggers a re-run. • If a dep changes while a previous run is still in flight, the previous run's CancellationToken is cancelled and one follow-up run is queued. Rapid-fire changes coalesce. • IDisposable.Dispose cancels any in-flight run and detaches all dep subscriptions. • Exceptions in the body (other than OperationCanceledException) are logged and do not disable the effect.
  class ReactiveEffect : IDisposable
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  class ReactiveEffect<TScope> : IDisposable where TScope : struct, IScopeKey
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // Reads track a dependency exactly like Reactive<T> (reading Value, Count, Contains, or enumerating during render). Every mutation method fires exactly one notification on its own — _ids.Add(x) is the whole call. Reactive<T>.NotifyUpdate is the escape hatch for the one case the mutators cannot see: mutating a stored member in place. Copy-on-write: every mutation runs under the lock and replaces the backing set with a fresh copy, so concurrent mutations serialize and any set handed out earlier is a stable snapshot. Each mutation copies the whole set, so for batches prefer the single-notify bulk ops (UnionWith, ExceptWith, ReplaceAll, Update) over per-item calls in a loop.
  class ReactiveHashSet<T> : Reactive<HashSet<T>>, IReadOnlyCollection<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
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
    bool Remove(T item)
    int RemoveAll(Predicate<T> match)
    void RemoveAt(int index)
    void ReplaceAll(IEnumerable<T> items)
    void Sort(Comparison<T> comparison)
    void Update(Func<IReadOnlyList<T>, IEnumerable<T>> transform)
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
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
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
    void AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    IReadOnlyList<T> ValueFor(string userId)

namespace Ikon.Common.Core.Scope
  readonly struct BackendTokenScope : IScopeKey
    ctor(string token)
    string Id { get; }
    string Name { get; }
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
    const string DefaultMountId
  readonly struct OperationScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  readonly struct RunScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  readonly struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
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

## Three backends — pick by what you store

```csharp
// Default — small/medium structured state
new PersistentSessionReactive<Prefs>(new Prefs());

// Public asset URL needed (uploaded images, published files)
private readonly PersistentSessionReactive<byte[]> _logo
    = new([], backend: PersistenceBackend.Public);

// Then read the URL after first save:
var url = _logo.PublicUrl;  // null until first save completes

// Small, frequently-mutated value, app has a postgres DB declared
private readonly PersistentSessionReactive<long> _counter
    = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
```

Rules:

- `Private` (default) — S3-backed cloud file. Use for anything that doesn't need a public URL.
- `Public` — same backend but the asset gets a public URL on `PublicUrl`. **Only** when the value will be linked to from the open web. Don't use for anything sensitive.
- `Postgres` — one row in the `ikon_reactive_storage` table in a postgres DB the app declares with `Databases = ["name:postgres"]` in `ikon-config.toml`. **Only** for small, frequently-mutated values (counters, status flags, small JSON). Never for binary blobs, large lists, or images. If the app has only one postgres DB, omit `postgresDatabase`. If there are several, name the one you want.

Backend ≠ scope. Scope decides who sees the value; backend decides where it lives. They're chosen independently.

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

- **Load**: parallel for all persistent reactives, finishes before `Main()` runs. Your code sees persisted values from the start.
- **Save**: parallel for all persistent reactives, on `StoppingAsync` (graceful shutdown).
- **Crashes lose unsaved changes.** If a value must survive a crash, also write it through a side-channel (webhook, direct DB, …). Don't try to bolt save-on-every-change on top — for high-write durability, store it in postgres directly through `app.Databases`.

## Anti-patterns — don't do these

- ❌ `Path.Combine(app.DataDirectory, "data")` + `Directory.CreateDirectory(...)` for runtime state. `DataDirectory` is **read-only** in cloud. It's for bundled assets the app reads, not state it writes.
- ❌ Using `Postgres` backend for binary blobs, image bytes, or unbounded lists.
- ❌ Using `Public` backend for anything sensitive — assets get a real URL on the open web.
- ❌ Constructing AssetUris by hand for state that fits a `PersistentXxxReactive`.
- ❌ Using `Guid.NewGuid()` as `key:` — it changes on restart.
- ❌ Reading the postgres backend in a tight loop — every read is a roundtrip; cache locally if you need it often.

## When to drop down to `Asset.Instance` directly

Reach for `Asset.Instance` (with a hand-built `AssetUri`) only when:
- You need to list files (`Asset.Instance.ListAsync`), not just read/write a known one.
- You need streaming reads/writes for very large files (multi-GB).
- The data isn't naturally a typed reactive value (e.g., a uploaded user file you only ever fetch on demand).

For everything else, use a `PersistentXxxReactive<T>`. It's strictly less code and keeps your state model uniform.
