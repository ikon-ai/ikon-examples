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
// created with 'ikon app db create --name main'. Omit postgresDatabase if there is only one.
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
using var _ = ReactiveScope.Use(new ClientScope(clientId));
_clientTheme.Value = "dark"; // Now targets the specified client
```

---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Reactive
  // Factory methods for creating ClientReactive<T> with per-client initialization.
  static class ClientReactive
    // Create a ClientReactive that initializes each client's value using a factory function. The factory receives the client session ID.
    static ClientReactive<T> Create<T>(Func<int, T> factory)
  // Same reactive contract as Reactive<T>, partitioned per client session: each connected client holds its own value, even two clients of the same user (use UserReactive<T> when the value should instead be shared across a user's sessions). .Value resolves against the active client scope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new ClientScope(...)) block — and throws when none is active. Background work carries no client scope, so name the session instead via SetFor / ValueFor.
  class ClientReactive<T> : Reactive<T, ClientScope>
    ctor(T initialValue)
    // Writes the value for one client session regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the client scope is still active (var clientSessionId = ReactiveScope.ClientId; in the UI callback, or ctx.ClientSessionId), then write to it from anywhere.
    void SetFor(int clientSessionId, T value)
    // Atomically read-modify-writes one client session's value, under that session's lock, regardless of which scope — if any — is active.
    void UpdateFor(int clientSessionId, Func<T, T> mutator)
    // Reads one client session's value regardless of which scope — if any — is active.
    T ValueFor(int clientSessionId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // Creates an empty per-client reactive dictionary whose keys are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase), preserved across every mutation.
    ctor(IEqualityComparer<TKey> comparer)
    // Creates a per-client reactive dictionary seeded with initialEntries whose keys are compared with comparer, preserved across every mutation.
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, IEqualityComparer<TKey> comparer)
    // Removes all entries from one client session's dictionary regardless of which scope — if any — is active.
    void ClearFor(int clientSessionId)
    // Removes the entry for key from one client session's dictionary regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(int clientSessionId, TKey key)
    // Adds or replaces one entry in one client session's dictionary regardless of which scope — if any — is active. This is the background-task form of this[key] = value: capture the id while the client scope is still active (var clientSessionId = ReactiveScope.ClientId;), then mutate from anywhere. One notification.
    void SetFor(int clientSessionId, TKey key, TValue value)
    // Atomically transforms one client session's entries under that session's lock, regardless of which scope — if any — is active. Same contract as ReactiveDictionary<TKey, TValue>.Update.
    void UpdateFor(int clientSessionId, Action<Dictionary<TKey, TValue>> transform)
    // Reads one client session's entries regardless of which scope — if any — is active.
    IReadOnlyDictionary<TKey, TValue> ValueFor(int clientSessionId)
  // Shorthand for ReactiveEffect<ClientScope>. Mirrors ClientReactive<T> as the per-client variant of Reactive<T>. Each connected client gets its own runner with independent cancel/queue, materialized on first dep change inside that client's scope.
  class ClientReactiveEffect : ReactiveEffect<ClientScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Creates an empty per-client reactive set whose members are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase), preserved across every mutation.
    ctor(IEqualityComparer<T> comparer)
    // Creates a per-client reactive set seeded with initialItems whose members are compared with comparer, preserved across every mutation.
    ctor(IEnumerable<T> initialItems, IEqualityComparer<T> comparer)
    // Adds item to one client session's set regardless of which scope — if any — is active. This is the background-task form of Add(item): capture the id while the client scope is still active (var clientSessionId = ReactiveScope.ClientId;), then mutate from anywhere. Returns whether it was added. One notification either way.
    bool AddFor(int clientSessionId, T item)
    // Removes all members from one client session's set regardless of which scope — if any — is active.
    void ClearFor(int clientSessionId)
    // Removes item from one client session's set regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(int clientSessionId, T item)
    // Atomically transforms one client session's members under that session's lock, regardless of which scope — if any — is active. Same contract as ReactiveHashSet<T>.Update.
    void UpdateFor(int clientSessionId, Action<HashSet<T>> transform)
    // Reads one client session's members regardless of which scope — if any — is active.
    IReadOnlyCollection<T> ValueFor(int clientSessionId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Appends to one client session's list regardless of which scope — if any — is active. This is the background-task form of ReactiveList<T>.Add: capture the id while the client scope is still active (var clientSessionId = ReactiveScope.ClientId;), then mutate from anywhere. One notification.
    void AddFor(int clientSessionId, T item)
    // Removes all items from one client session's list regardless of which scope — if any — is active.
    void ClearFor(int clientSessionId)
    // Removes the first occurrence of item from one client session's list regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(int clientSessionId, T item)
    // Atomically replaces one client session's items under that session's lock, regardless of which scope — if any — is active. Same contract as ReactiveList<T>.Update.
    void UpdateFor(int clientSessionId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    // Reads one client session's items regardless of which scope — if any — is active.
    IReadOnlyList<T> ValueFor(int clientSessionId)
  interface IReactive
    long Version { get; }
    // Fires whenever this reactive's value changes (in any scope, for scoped variants). Payload-free so a single subscription can be taken across heterogeneous reactives — handlers fetch the new value via .Value when they need it. Used by ReactiveEffect and other dependency-tracked consumers.
    event Action? Changed
    // Fires with the scope-derived session id whose value just changed. For unscoped reactives the id is always 0; for ClientReactive<T> it is the hash of ClientScope; for UserReactive<T> the hash of UserScope; etc. Lets external subscription routing fan out to only the clients whose scope matches the changed signal.
    event Action<int>? SessionChanged
  // Factory methods for creating MountReactive<T> with per-mount initialization.
  static class MountReactive
    // Create a MountReactive that initializes each mount's value using a factory function. The factory receives the mount id.
    static MountReactive<T> Create<T>(Func<string, T> factory)
  // Same reactive contract as Reactive<T>, partitioned per Parallax mount an app declares via Mounts (e.g. independent message history for an embedded "aiCanvas" mount vs the "ikon-ui" page). For state shared across a client's mounts use ClientReactive<T>; across all clients use Reactive<T>. .Value resolves against the MountScope active during a render iteration — typically anywhere inside UI.Root() — and throws otherwise. Background work carries no mount scope, so name the mount instead via SetFor / ValueFor.
  class MountReactive<T> : Reactive<T, MountScope>
    ctor(T initialValue)
    // Writes one mount's value regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the mount scope is still active (var mountId = ReactiveScope.MountId;), then write to it from anywhere.
    void SetFor(string mountId, T value)
    // Atomically read-modify-writes one mount's value, under that mount's lock, regardless of which scope — if any — is active.
    void UpdateFor(string mountId, Func<T, T> mutator)
    // Reads one mount's value regardless of which scope — if any — is active.
    T ValueFor(string mountId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per mount exactly like MountReactive<T>. Important: Must be accessed inside a render iteration where MountScope is active — typically anywhere inside UI.Root().
  class MountReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // Removes all entries from one mount's dictionary regardless of which scope — if any — is active.
    void ClearFor(string mountId)
    // Removes the entry for key from one mount's dictionary regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string mountId, TKey key)
    // Adds or replaces one entry in one mount's dictionary regardless of which scope — if any — is active. This is the background-task form of this[key] = value: capture the id while the mount scope is still active (var mountId = ReactiveScope.MountId;), then mutate from anywhere. One notification.
    void SetFor(string mountId, TKey key, TValue value)
    // Atomically transforms one mount's entries under that mount's lock, regardless of which scope — if any — is active. Same contract as ReactiveDictionary<TKey, TValue>.Update.
    void UpdateFor(string mountId, Action<Dictionary<TKey, TValue>> transform)
    // Reads one mount's entries regardless of which scope — if any — is active.
    IReadOnlyDictionary<TKey, TValue> ValueFor(string mountId)
  // Shorthand for ReactiveEffect<MountScope>. Mirrors MountReactive<T> as the per-mount variant of Reactive<T>. Each Parallax mount gets its own runner, materialized on first dep change inside that mount's scope.
  class MountReactiveEffect : ReactiveEffect<MountScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per mount exactly like MountReactive<T>. Important: Must be accessed inside a render iteration where MountScope is active — typically anywhere inside UI.Root().
  class MountReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Adds item to one mount's set regardless of which scope — if any — is active. This is the background-task form of Add(item): capture the id while the mount scope is still active (var mountId = ReactiveScope.MountId;), then mutate from anywhere. Returns whether it was added. One notification either way.
    bool AddFor(string mountId, T item)
    // Removes all members from one mount's set regardless of which scope — if any — is active.
    void ClearFor(string mountId)
    // Removes item from one mount's set regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string mountId, T item)
    // Atomically transforms one mount's members under that mount's lock, regardless of which scope — if any — is active. Same contract as ReactiveHashSet<T>.Update.
    void UpdateFor(string mountId, Action<HashSet<T>> transform)
    // Reads one mount's members regardless of which scope — if any — is active.
    IReadOnlyCollection<T> ValueFor(string mountId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per mount exactly like MountReactive<T>. Important: Must be accessed inside a render iteration where MountScope is active — typically anywhere inside UI.Root().
  class MountReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Appends to one mount's list regardless of which scope — if any — is active. This is the background-task form of ReactiveList<T>.Add: capture the id while the mount scope is still active (var mountId = ReactiveScope.MountId;), then mutate from anywhere. One notification.
    void AddFor(string mountId, T item)
    // Removes all items from one mount's list regardless of which scope — if any — is active.
    void ClearFor(string mountId)
    // Removes the first occurrence of item from one mount's list regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string mountId, T item)
    // Atomically replaces one mount's items under that mount's lock, regardless of which scope — if any — is active. Same contract as ReactiveList<T>.Update.
    void UpdateFor(string mountId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    // Reads one mount's items regardless of which scope — if any — is active.
    IReadOnlyList<T> ValueFor(string mountId)
  // Selects the backing store for a persistent reactive.
  enum PersistenceBackend
    // Asset storage on private S3-style cloud files. Explicitly opts the value out of the Default routing — pick it when a structured value must stay on asset storage even though the app has its built-in Postgres database.
    Private
    // Asset storage on public S3-style cloud files. The reactive exposes a PublicUrl accessor so the value can be linked to from the open web.
    Public
    // Postgres key-value row in a database the app declares in ikon-config.toml. Pass the database name (matching the Databases = ["name:postgres"] entry) when constructing the reactive; with a single declared database the name can be omitted.
    Postgres
    // Let the platform pick the store per its storage doctrine: structured values go to the app's built-in app database when the session has one, while binary payloads (byte[]) — and sessions without a database — use private asset storage. This is the default for every persistent reactive that does not name a backend.
    Default
  // Identifies where a reactive's value is persisted in cloud storage and how it is keyed.
  enum PersistenceScope
    // Not persisted. The value is ephemeral and lost when the app restarts.
    None
    // Persisted globally for the app within its space. Shared across all session identities and users. Use for app-wide configuration that one app instance owns.
    Global
    // Persisted per session identity (the routing key the app declares as its TSessionIdentity). Two app instances with the same session identity share the same value; different identities have separate values.
    Session
    // Persisted per user. The current primary user's id is part of the storage key, so each user has their own value.
    User
  static class Reactive
    // Runs action on a background task and assigns its result to reactiveValue when it completes, so subscribers react without the caller awaiting. Exceptions go to onError when provided and are logged otherwise; cancellation leaves the reactive value unchanged.
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
    // Runs action on a background task and assigns its result to reactiveValue when it completes, passing token to the action so it can observe cancellation. Exceptions go to onError when provided and are logged otherwise; cancellation leaves the reactive value unchanged.
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
  // Reading Value during a UI render registers a dependency; writing a changed value re-renders only the parts that read it. An unscoped Reactive<T> holds one value shared across all clients and is accessible anywhere. For per-client state use ClientReactive<T>; for per-user state (shared across a user's sessions) use UserReactive<T>. Those scoped variants resolve .Value against the active scope, so it must be read inside one — UI.Root(), an action callback, or a ReactiveScope.Use() block — and throw otherwise; background work (a Task.Run loop, a timer, an endpoint handler) has no scope and names its target instead via SetFor(id, value) / ValueFor(id).
  class Reactive<T> : IReactive
    // Creates a reactive whose initial value is default(T). Call as new Reactive<T>() — the UseDefault parameter is only an overload disambiguator and is never passed explicitly.
    ctor(UseDefault _ = default)
    // Creates a reactive with an explicit initial value: new Reactive<int>(0), new Reactive<Dictionary<int, Player>>(new()).
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
    // Unwraps to Value — this is a TRACKED read: used during render it registers a dependency (the component re-renders when the value changes), and for scoped variants it throws InvalidOperationException when no scope is active. It is not a cheap unwrap; use Peek to read the current value without tracking.
    static implicit operator T(Reactive<T> r)
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
  // Base class for scoped reactive variables: each distinct TScope instance gets its own value, resolved from the active scope. Use directly only for custom scope types — prefer ClientReactive<T> (per-client) or UserReactive<T> (per-user). The required scope must be active when accessing .Value (e.g. inside UI.Root()); otherwise it throws InvalidOperationException.
  class Reactive<T, TScope> : Reactive<T> where TScope : IScopeKey
    ctor(T initialValue)
    ctor(Func<T> initialValue)
    // Writes the value for scope regardless of which scope — if any — is active, so background work can target a scope it does not run under without re-scoping itself.
    void SetFor(TScope scope, T value)
    // Atomically read-modify-writes the value for scope, under that scope's lock, regardless of which scope — if any — is active.
    void UpdateFor(TScope scope, Func<T, T> mutator)
    // Reads the value for scope regardless of which scope — if any — is active.
    T ValueFor(TScope scope)
  // Convenience helpers on Reactive<T> for the busy-flag pattern that every async handler uses. Without these, the standard shape is verbose and easy to break:
  // _busy.Value = true;
  // try { await SlowThingAsync(); }
  // finally { _busy.Value = false; }
  // Forgetting finally leaves the flag stuck on if the call throws. AsToken collapses the shape to:
  // using var _ = _busy.AsToken();
  // await SlowThingAsync();
  // — the flag flips to true on entry, the IDisposable returns it to false on dispose (including the catch-and-rethrow path of using).
  static class ReactiveBoolExtensions
    // Set the flag to true and return an IDisposable that returns it to false on dispose. Idempotent — disposing twice is safe (the second dispose is a no-op).
    static IDisposable AsToken(this Reactive<bool> reactive)
  // Mutation helpers for a Reactive<T> that wraps a mutable collection. List state belongs in ReactiveList<T> and dictionary state in ReactiveDictionary<TKey, TValue>, not in Reactive<List<T>> / Reactive<Dictionary<TKey, TValue>>: they give the same one-call mutators (Add / Remove / indexer-set / Update) with copy-on-write snapshots and a read-only Value, so a snapshot handed to a renderer can't be mutated behind its back. These extensions are for the collections that have no reactive equivalent yet — Reactive<HashSet<T>> — and for legacy Reactive<List<T>> / Reactive<Dictionary<TKey, TValue>> code. In those cases they mutate the underlying instance AND fire the change notification in one call, so callers write _byId.Set(key, value) instead of the two-step _byId.Value[key] = value; _byId.NotifyUpdate();. The two-step form is needed because the reference-equality check at the Value setter doesn't trigger when the underlying collection is mutated in place — which is what makes a forgotten NotifyUpdate the classic "UI doesn't update after Add/Remove" bug. These helpers make the right thing the easy thing. Every helper runs its mutation through the locked Reactive<T>.Update, so concurrent mutations from parallel handlers serialize instead of racing. Reassignment (_byId.Value = new Dictionary<TKey, TValue>(_byId.Value) { [key] = value }) also notifies and remains available when callers want immutable-style updates.
  static class ReactiveCollectionExtensions
    // Append item to the underlying list and notify.
    static void Add<T>(this Reactive<List<T>> reactive, T item)
    // Add item to the underlying set and notify if it was new.
    static bool Add<T>(this Reactive<HashSet<T>> reactive, T item)
    // Append items to the underlying list and notify once.
    static void AddRange<T>(this Reactive<List<T>> reactive, IEnumerable<T> items)
    // Clear the underlying list and notify if it had items.
    static void Clear<T>(this Reactive<List<T>> reactive)
    // Clear the underlying set and notify if it had items.
    static void Clear<T>(this Reactive<HashSet<T>> reactive)
    // Clear the underlying dictionary and notify if it had entries.
    static void Clear<TKey, TValue>(this Reactive<Dictionary<TKey, TValue>> reactive)
    // Insert item at index and notify.
    static void Insert<T>(this Reactive<List<T>> reactive, int index, T item)
    // Escape hatch for mutations the typed helpers don't cover (e.g. sorting in place, swapping items, clearing+repopulating). The mutator runs on the live reference under the Reactive<T>.Update lock; the change notification fires after it returns. Use the typed helpers (Add<T>, Remove<T>, …) when one fits.
    static void Mutate<T>(this Reactive<T> reactive, Action<T> mutator)
    // Remove the first occurrence of item and notify if removed.
    static bool Remove<T>(this Reactive<List<T>> reactive, T item)
    // Remove item from the underlying set and notify if removed.
    static bool Remove<T>(this Reactive<HashSet<T>> reactive, T item)
    // Remove key and notify if removed.
    static bool Remove<TKey, TValue>(this Reactive<Dictionary<TKey, TValue>> reactive, TKey key)
    // Remove all items matching match and notify if any removed.
    static int RemoveAll<T>(this Reactive<List<T>> reactive, Predicate<T> match)
    // Remove the item at index and notify.
    static void RemoveAt<T>(this Reactive<List<T>> reactive, int index)
    // Set key to value and notify.
    static void Set<TKey, TValue>(this Reactive<Dictionary<TKey, TValue>> reactive, TKey key, TValue value)
  // Reads track a dependency exactly like Reactive<T> (reading Value, Count, the indexer, TryGetValue, or enumerating during render). Every mutation method fires exactly one notification on its own — _byId[key] = value is the whole call. Reactive<T>.NotifyUpdate is the escape hatch for the one case the mutators cannot see: mutating a stored value in place (feed.Messages.Add(m); _feeds.NotifyUpdate();). Copy-on-write: every mutation runs under the lock and replaces the backing dictionary with a fresh copy, so concurrent mutations serialize and any dictionary handed out earlier is a stable snapshot. Each mutation copies the whole dictionary, so for batches prefer the single-notify bulk ops (ReplaceAll, Update) over per-key calls in a loop.
  class ReactiveDictionary<TKey, TValue> : Reactive<Dictionary<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // Creates an empty reactive dictionary whose keys are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase). The comparer is preserved across every copy-on-write mutation, so the custom key semantics hold for the life of the dictionary.
    ctor(IEqualityComparer<TKey> comparer)
    // Creates a reactive dictionary seeded with initialEntries whose keys are compared with comparer. The comparer is preserved across every mutation.
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, IEqualityComparer<TKey> comparer)
    // The number of entries. Tracked read.
    int Count { get; }
    // The value for key. The getter is a tracked read and throws for a missing key; the setter adds or replaces the entry with one change notification.
    TValue this[TKey key] { get; set; }
    // The keys of the current entries. Tracked read.
    IEnumerable<TKey> Keys { get; }
    // The current entries without dependency tracking. See Reactive<T>.Peek.
    IReadOnlyDictionary<TKey, TValue> Peek { get; }
    // The current entries as a read-only snapshot. Reading tracks a dependency like Reactive<T>.Value; assigning replaces the whole content with a copy of the given entries (see ReplaceAll).
    IReadOnlyDictionary<TKey, TValue> Value { get; set; }
    // The values of the current entries. Tracked read.
    IEnumerable<TValue> Values { get; }
    // Add key with value; throws if the key already exists. One notification.
    void Add(TKey key, TValue value)
    // Remove all entries. One notification.
    void Clear()
    // Whether an entry for key is present. Tracked read.
    bool ContainsKey(TKey key)
    // Enumerate a snapshot of the current entries. Tracked read; the snapshot is safe to iterate while other code mutates the dictionary.
    IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    // Remove the entry for key. Returns whether it was found. One notification either way.
    bool Remove(TKey key)
    // Replace the whole content with a copy of entries. One notification.
    void ReplaceAll(IEnumerable<KeyValuePair<TKey, TValue>> entries)
    // Add key with value if the key is not present. Returns whether it was added. One notification either way.
    bool TryAdd(TKey key, TValue value)
    // Get the value for key if present. Tracked read.
    bool TryGetValue(TKey key, out TValue value)
    // Atomically transform the content: transform receives a fresh copy of the current entries and mutates it freely (add, remove, rewrite several keys). Runs under the same lock as all other mutations, so concurrent updates serialize. One notification.
    void Update(Action<Dictionary<TKey, TValue>> transform)
  // Lifecycle (global): • Constructor runs the body once immediately (initial fire). • Each tracked dep's IReactive.Changed event triggers a re-run. • If a dep changes while a previous run is still in flight, the previous run's CancellationToken is cancelled and one follow-up run is queued. Rapid-fire changes coalesce. • IDisposable.Dispose cancels any in-flight run and detaches all dep subscriptions. • Exceptions in the body (other than OperationCanceledException) are logged and do not disable the effect.
  class ReactiveEffect : IDisposable
    // Create an effect with an async body. The token cancels when a dep changes mid-run; respect it for clean cancellation.
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // This overload exists so an async () => await ... body binds here as a Task-returning delegate rather than collapsing into the Action overload as async-void — which would report the run complete at the first await and swallow later exceptions. Use the Func<CancellationToken, Task> overload to observe cancellation.
    ctor(Func<Task> body, params IReactive[] deps)
    // Create an effect with a sync body.
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
    // Creates an empty reactive set whose members are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase). The comparer is preserved across every copy-on-write mutation, so the custom membership semantics hold for the life of the set.
    ctor(IEqualityComparer<T> comparer)
    // Creates a reactive set seeded with initialItems whose members are compared with comparer. The comparer is preserved across every mutation.
    ctor(IEnumerable<T> initialItems, IEqualityComparer<T> comparer)
    // The number of members. Tracked read.
    int Count { get; }
    // The current members without dependency tracking. See Reactive<T>.Peek.
    IReadOnlyCollection<T> Peek { get; }
    // The current members as a read-only snapshot. Reading tracks a dependency like Reactive<T>.Value; assigning replaces the whole content with a copy of the given members (see ReplaceAll).
    IReadOnlyCollection<T> Value { get; set; }
    // Add item. Returns whether it was added (false if already present). One notification either way.
    bool Add(T item)
    // Remove all members. One notification.
    void Clear()
    // Whether item is present. Tracked read.
    bool Contains(T item)
    // Remove every member of other. One notification for the whole batch.
    void ExceptWith(IEnumerable<T> other)
    // Enumerate a snapshot of the current members. Tracked read; the snapshot is safe to iterate while other code mutates the set.
    IEnumerator<T> GetEnumerator()
    // Remove item. Returns whether it was found. One notification either way.
    bool Remove(T item)
    // Replace the whole content with a copy of items. One notification.
    void ReplaceAll(IEnumerable<T> items)
    // Add every member of other. One notification for the whole batch.
    void UnionWith(IEnumerable<T> other)
    // Atomically transform the content: transform receives a fresh copy of the current members and mutates it freely (add, remove, several members at once). Runs under the same lock as all other mutations, so concurrent updates serialize. One notification.
    void Update(Action<HashSet<T>> transform)
  // Reads track a dependency exactly like Reactive<T> (reading Value, Count, the indexer, or enumerating during render). Every mutation method fires exactly one notification on its own — _items.Add(x) is the whole call. Reactive<T>.NotifyUpdate is the escape hatch for the one case the mutators cannot see: mutating an item in place (tracker.Progress = 47; _items.NotifyUpdate();). Copy-on-write: every mutation runs under the lock and replaces the backing list with a fresh copy, so concurrent mutations serialize and any list handed out earlier is a stable snapshot. Each mutation copies the whole list, so for batches prefer the single-notify bulk ops (AddRange, ReplaceAll, Update) over per-item calls in a loop.
  class ReactiveList<T> : Reactive<List<T>>, IReadOnlyList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // The number of items. Tracked read.
    int Count { get; }
    // The item at index. The getter is a tracked read; the setter replaces the item with one change notification.
    T this[int index] { get; set; }
    // The current items without dependency tracking. See Reactive<T>.Peek.
    IReadOnlyList<T> Peek { get; }
    // The current items as a read-only snapshot. Reading tracks a dependency like Reactive<T>.Value; assigning replaces the whole content with a copy of the given sequence (see ReplaceAll).
    IReadOnlyList<T> Value { get; set; }
    // Append item. One notification.
    void Add(T item)
    // Append items. One notification for the whole batch.
    void AddRange(IEnumerable<T> items)
    // Remove all items. One notification.
    void Clear()
    // Whether item is present. Tracked read.
    bool Contains(T item)
    // Enumerate a snapshot of the current items. Tracked read; the snapshot is safe to iterate while other code mutates the list.
    IEnumerator<T> GetEnumerator()
    // Index of the first occurrence of item, or -1. Tracked read.
    int IndexOf(T item)
    // Insert item at index. One notification.
    void Insert(int index, T item)
    // Remove the first occurrence of item. Returns whether it was found. One notification either way.
    bool Remove(T item)
    // Remove all items matching match. Returns the removed count. One notification either way.
    int RemoveAll(Predicate<T> match)
    // Remove the item at index. One notification.
    void RemoveAt(int index)
    // Replace the whole content with a copy of items. One notification.
    void ReplaceAll(IEnumerable<T> items)
    // Sort the items using comparison. One notification.
    void Sort(Comparison<T> comparison)
    // Atomically replace the content: transform sees the current items and returns the new ones, which are materialized into a fresh list. Runs under the same lock as all other mutations, so concurrent updates serialize. One notification.
    void Update(Func<IReadOnlyList<T>, IEnumerable<T>> transform)
  // A general-purpose scope stack that supports multiple overlapping scope types (Client, User, Tenant, etc.), each tracked independently. This is a static wrapper around a shared ScopeStack instance for the reactive system. Scope changes are automatically mirrored to Log.Instance for logging purposes.
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
  // Marker type for the default-value Reactive<T> constructor. Because every constructor carries trailing caller-info parameters, a marker parameter is what keeps the value-less overload distinct from Reactive(T initialValue, ...). Never pass it explicitly — write new Reactive<T>() and the value starts at default(T). Passing any argument at all selects the value constructor, so new Reactive<Dictionary<int, Player>>(new()) means what it reads as.
  readonly struct UseDefault
  // Same reactive contract as Reactive<T>, partitioned per user and shared across that user's client sessions (use ClientReactive<T> when each client needs its own value). .Value resolves against the active user scope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throws when none is active. Background work carries no user scope, so name the user instead via SetFor / ValueFor.
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue)
    ctor(Func<string, T> initialValue)
    // Writes one user's value regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then write to it from anywhere.
    void SetFor(string userId, T value)
    // Atomically read-modify-writes one user's value, under that user's lock, regardless of which scope — if any — is active.
    void UpdateFor(string userId, Func<T, T> mutator)
    // Reads one user's value regardless of which scope — if any — is active.
    T ValueFor(string userId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // Removes all entries from one user's dictionary regardless of which scope — if any — is active.
    void ClearFor(string userId)
    // Removes the entry for key from one user's dictionary regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, TKey key)
    // Adds or replaces one entry in one user's dictionary regardless of which scope — if any — is active. This is the background-task form of this[key] = value: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. One notification.
    void SetFor(string userId, TKey key, TValue value)
    // Atomically transforms one user's entries under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveDictionary<TKey, TValue>.Update.
    void UpdateFor(string userId, Action<Dictionary<TKey, TValue>> transform)
    // Reads one user's entries regardless of which scope — if any — is active.
    IReadOnlyDictionary<TKey, TValue> ValueFor(string userId)
  // Shorthand for ReactiveEffect<UserScope>. Mirrors UserReactive<T> as the per-user variant of Reactive<T>. Each distinct user gets its own runner; the same user across multiple sessions shares one runner.
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Adds item to one user's set regardless of which scope — if any — is active. This is the background-task form of Add(item): capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. Returns whether it was added. One notification either way.
    bool AddFor(string userId, T item)
    // Removes all members from one user's set regardless of which scope — if any — is active.
    void ClearFor(string userId)
    // Removes item from one user's set regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, T item)
    // Atomically transforms one user's members under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveHashSet<T>.Update.
    void UpdateFor(string userId, Action<HashSet<T>> transform)
    // Reads one user's members regardless of which scope — if any — is active.
    IReadOnlyCollection<T> ValueFor(string userId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Appends to one user's list regardless of which scope — if any — is active. This is the background-task form of ReactiveList<T>.Add: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. One notification.
    void AddFor(string userId, T item)
    // Removes all items from one user's list regardless of which scope — if any — is active.
    void ClearFor(string userId)
    // Removes the first occurrence of item from one user's list regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, T item)
    // Atomically replaces one user's items under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveList<T>.Update.
    void UpdateFor(string userId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    // Reads one user's items regardless of which scope — if any — is active.
    IReadOnlyList<T> ValueFor(string userId)

namespace Ikon.Common.Core.Scope
  // Scope for backend token context, transports the backend token of the caller.
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
  // Scope with a user-specified name and ID, enabling dynamic scoping without needing new struct types.
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
    // The mount id every Ikon app emits today on its single Parallax stream. Apps that don't override IAppBase.Mounts render under this id.
    const string DefaultMountId
  // Scope for grouping a single logical operation (e.g., LLM generation, image generation).
  readonly struct OperationScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  // Scope for application run context, typically set at program startup in Program.cs. Used to group all log events and operations within a single application run.
  readonly struct RunScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  // Scope for tenant/customer context, an arbitrary user-specified ID for scoping AI app logic.
  readonly struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
  // Machine-triggered work has no ClientScope and no UserScope, so without this its cost lands in the space's totals attached to nothing and a schedule quietly burning credits is indistinguishable from the app's ordinary use. Every log event carries the active scopes, so the cost of an AI call made inside a trigger handler is attributed by the ambient scope alone — call sites need no change. Scoped to the invocation rather than the session on purpose: a session woken by cron goes on to serve clients, and their spend is theirs, not the schedule's. The values match the backend's AppSessionSource spelling, so the trigger a cost row carries reads the same as the source stamped on the session that ran it.
  readonly struct TriggerScope : IScopeKey
    ctor(string kind)
    string Id { get; }
    string Name { get; }
    // A [Cron] function invoked by the backend scheduler.
    const string Cron
    // An HTTP endpoint or inbound webhook request routed to the app.
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

Each scope also has persistent collection variants — `PersistentReactiveList<T>` / `PersistentSessionReactiveList<T>` / `PersistentUserReactiveList<T>`, and the same trio for `ReactiveHashSet<T>` and `ReactiveDictionary<TKey, TValue>`. Use these instead of wrapping a collection in a `Persistent...Reactive<List<T>>`.

The user-scoped classes additionally expose per-user accessors usable outside an active user scope (background tasks): `ValueFor(userId)`, `SetFor(userId, value)`, and `UpdateFor(userId, ...)` on `PersistentUserReactive<T>`; the collection variants have equivalents like `AddFor` / `RemoveFor` / `ClearFor`.

## Backends — the default does the right thing

```csharp
// Default — structured state lands in the app's built-in postgres database
new PersistentSessionReactive<Prefs>(new Prefs());

// byte[] payloads stay on asset storage automatically — no backend parameter needed
private readonly PersistentSessionReactive<byte[]> _snapshot = new([]);

// Public asset URL needed (uploaded images, published files)
private readonly PersistentSessionReactive<byte[]> _logo
    = new([], backend: PersistenceBackend.Public);

// Then read the URL after first save:
var url = _logo.PublicUrl;  // null until first save completes

// Explicitly target a postgres DB of the app's own
private readonly PersistentSessionReactive<long> _counter
    = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
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
