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
  // Factory methods for creating ClientReactive<T> with per-client initialization.
  static class ClientReactive
    // Create a ClientReactive that initializes each client's value using a factory function. The factory receives the client session ID.
    static ClientReactive<T> Create<T>(Func<int, T> factory)
  // A ReactiveDictionary<TKey, TValue> with a separate dictionary for each client session.
  // Remarks:
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
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
    ctor(Action body, params IReactive[] deps)
  // A ReactiveList<T> with a separate list for each client session.
  // Remarks:
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
  // A reactive variable with a separate value for each client session.
  // Remarks:
  // Use ClientReactive when you need state that is independent for each connected client. Each client connection gets its own value, even if they belong to the same user. Example use cases: • Form input text (each client types independently) • UI state like selected tab, scroll position, expanded panels • Client-specific preferences or temporary state When NOT to use: If you want state shared across a user's multiple client sessions, use UserReactive<T> instead. Where .Value works: anywhere the client scope is active — inside UI.Root(), an action callback, or a ReactiveScope.Use(new ClientScope(...)) block. Background work carries no client scope, so .Value there throws rather than writing to nowhere; name the client instead with ClientReactive<T>.SetFor / ClientReactive<T>.ValueFor.
  class ClientReactive<T> : Reactive<T, ClientScope>
    ctor(T initialValue)
    // Writes the value for one client session regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the client scope is still active (var clientSessionId = ReactiveScope.ClientId; in the UI callback, or ctx.ClientSessionId), then write to it from anywhere.
    void SetFor(int clientSessionId, T value)
    // Atomically read-modify-writes one client session's value, under that session's lock, regardless of which scope — if any — is active.
    void UpdateFor(int clientSessionId, Func<T, T> mutator)
    // Reads one client session's value regardless of which scope — if any — is active.
    T ValueFor(int clientSessionId)
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
  // A ReactiveDictionary<TKey, TValue> with a separate dictionary for each Parallax mount in the active render iteration.
  // Remarks:
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
    ctor(Action body, params IReactive[] deps)
  // A ReactiveList<T> with a separate list for each Parallax mount in the active render iteration.
  // Remarks:
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
  // A reactive variable with a separate value for each Parallax mount in the active render iteration.
  // Remarks:
  // Use MountReactive when you need state that is independent for each mount an app declares via Mounts. For example, an app that renders both an "ikon-ui" page mount and an embeddable "aiCanvas" mount can store independent message-history-per-mount state. Example use cases: • Per-mount message history (a chat panel embedded as a sub-tree in multiple host pages, each with its own conversation thread) • Per-mount UI state isolated from other mounts of the same app When NOT to use: If state should be shared across all mounts of a client, use ClientReactive<T>. If shared across all clients, use Reactive<T>. Where .Value works: inside a render iteration where MountScope is active — typically anywhere inside UI.Root(). Background work carries no mount scope, so .Value there throws rather than writing to nowhere; name the mount instead with MountReactive<T>.SetFor / MountReactive<T>.ValueFor.
  class MountReactive<T> : Reactive<T, MountScope>
    ctor(T initialValue)
    // Writes one mount's value regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the mount scope is still active (var mountId = ReactiveScope.MountId;), then write to it from anywhere.
    void SetFor(string mountId, T value)
    // Atomically read-modify-writes one mount's value, under that mount's lock, regardless of which scope — if any — is active.
    void UpdateFor(string mountId, Func<T, T> mutator)
    // Reads one mount's value regardless of which scope — if any — is active.
    T ValueFor(string mountId)
  // Selects the backing store for a persistent reactive.
  enum PersistenceBackend
    Private
    Public
    Postgres
  // Identifies where a reactive's value is persisted in cloud storage and how it is keyed.
  enum PersistenceScope
    None
    Global
    Session
    User
  static class Reactive
    // Runs action on a background task and assigns its result to reactiveValue when it completes, so subscribers react without the caller awaiting. Exceptions go to onError when provided and are logged otherwise; cancellation leaves the reactive value unchanged.
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
    // Runs action on a background task and assigns its result to reactiveValue when it completes, passing token to the action so it can observe cancellation. Exceptions go to onError when provided and are logged otherwise; cancellation leaves the reactive value unchanged.
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
  // Convenience helpers on Reactive<T> for the busy-flag pattern that every async handler uses. Without these, the standard shape is verbose and easy to break:
  // _busy.Value = true;
  // try { await SlowThingAsync(); }
  // finally { _busy.Value = false; }
  // Forgetting finally leaves the flag stuck on if the call throws. ReactiveBoolExtensions.AsToken collapses the shape to:
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
    // Mutate the underlying value via mutator and notify.
    // Remarks:
    // Escape hatch for mutations the typed helpers don't cover (e.g. sorting in place, swapping items, clearing+repopulating). The mutator runs on the live reference under the Reactive<T>.Update lock; the change notification fires after it returns. Use the typed helpers (ReactiveCollectionExtensions.Add<T>, ReactiveCollectionExtensions.Remove<T>, …) when one fits.
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
  // A reactive dictionary that automatically triggers UI updates on every mutation.
  // Remarks:
  // Reads are tracked exactly like Reactive<T>: reading ReactiveDictionary<TKey, TValue>.Value, ReactiveDictionary<TKey, TValue>.Count, the indexer, ReactiveDictionary<TKey, TValue>.TryGetValue, or enumerating the entries during UI rendering registers a dependency. Every mutation method (the indexer setter, ReactiveDictionary<TKey, TValue>.Add, ReactiveDictionary<TKey, TValue>.Remove, …) fires exactly one change notification on its own, so there is no NotifyUpdate to remember for them — _byId[key] = value is the whole call. Reactive<T>.NotifyUpdate remains the escape hatch for the one case the mutators cannot see: mutating a stored value in place (feed.Messages.Add(m); _feeds.NotifyUpdate(); for a mutable POCO in the map). Copy-on-write: every mutation runs inside the locked Reactive<T>.Update and replaces the backing dictionary with a fresh copy plus the operation. Concurrent mutations serialize instead of racing, and any dictionary handed out earlier (a ReactiveDictionary<TKey, TValue>.Value read, a live enumeration) is a stable snapshot that never changes underneath the reader. Read-only surface: ReactiveDictionary<TKey, TValue>.Value and ReactiveDictionary<TKey, TValue>.Peek are typed IReadOnlyDictionary<TKey, TValue>, so _byId.Value[key] = v does not compile — _byId[key] = v is the spelling. Assigning ReactiveDictionary<TKey, TValue>.Value replaces the whole content with a copy, same as ReactiveDictionary<TKey, TValue>.ReplaceAll. Cost: each mutation copies the whole dictionary. For batches, prefer the single-notify bulk operations — ReactiveDictionary<TKey, TValue>.ReplaceAll or ReactiveDictionary<TKey, TValue>.Update — over per-key calls in a loop.
  class ReactiveDictionary<TKey, TValue> : Reactive<Dictionary<TKey, TValue>>, IEnumerable, IEnumerable<KeyValuePair<TKey, TValue>>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // The number of entries. Tracked read.
    int Count { get; }
    // The value for key. The getter is a tracked read and throws for a missing key; the setter adds or replaces the entry with one change notification.
    TValue this[TKey key] { get; set; }
    // The keys of the current entries. Tracked read.
    IEnumerable<TKey> Keys { get; }
    // The current entries without dependency tracking. See Reactive<T>.Peek.
    IReadOnlyDictionary<TKey, TValue> Peek { get; }
    // The current entries as a read-only snapshot. Reading tracks a dependency like Reactive<T>.Value; assigning replaces the whole content with a copy of the given entries (see ReactiveDictionary<TKey, TValue>.ReplaceAll).
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
  // Side-effect primitive that runs on tracked IReactive dependency changes. Mirrors the shape of Reactive<T> / Reactive<T, TScope>: this class is the unscoped (global) variant, and ReactiveEffect<TScope> binds to a single scope type.
  // Remarks:
  // Lifecycle (global): • Constructor runs the body once immediately (initial fire). • Each tracked dep's IReactive.Changed event triggers a re-run. • If a dep changes while a previous run is still in flight, the previous run's CancellationToken is cancelled and one follow-up run is queued. Rapid-fire changes coalesce. • IDisposable.Dispose cancels any in-flight run and detaches all dep subscriptions. • Exceptions in the body (other than OperationCanceledException) are logged and do not disable the effect.
  class ReactiveEffect : IDisposable
    // Create an effect with an async body. The token cancels when a dep changes mid-run; respect it for clean cancellation.
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Create an effect with a sync body.
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // Side-effect primitive bound to a single scope type. Mirrors Reactive<T, TScope>: each instance of TScope gets its own per-scope effect runner with independent cancel/queue state, materialized lazily on first dep change in that scope. Unlike the global ReactiveEffect, this variant does NOT fire eagerly at construction — there's no scope active yet. The first dep change observed inside a scope of type TScope instantiates that scope's runner and fires the body for the first time. For "fire when scope first opens regardless of deps" lifecycle hooks (e.g. preload data on client connect), use the host app's existing scope-creation events directly.
  class ReactiveEffect<TScope> : IDisposable where TScope : struct, IScopeKey
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // A reactive list that automatically triggers UI updates on every mutation.
  // Remarks:
  // Reads are tracked exactly like Reactive<T>: reading ReactiveList<T>.Value, ReactiveList<T>.Count, the indexer, or enumerating the list during UI rendering registers a dependency. Every mutation method (ReactiveList<T>.Add, ReactiveList<T>.Remove, ReactiveList<T>.Sort, …) fires exactly one change notification on its own, so there is no NotifyUpdate to remember for them — _items.Add(x) is the whole call. Reactive<T>.NotifyUpdate remains the escape hatch for the one case the mutators cannot see: mutating an item in place (tracker.Progress = 47; _items.NotifyUpdate(); for a mutable POCO in the list). Copy-on-write: every mutation runs inside the locked Reactive<T>.Update and replaces the backing list with a fresh copy plus the operation. Concurrent mutations serialize instead of racing, and any list handed out earlier (a ReactiveList<T>.Value read, a live enumeration) is a stable snapshot that never changes underneath the reader. Read-only surface: ReactiveList<T>.Value and ReactiveList<T>.Peek are typed IReadOnlyList<T>, so _items.Value.Add(x) does not compile — _items.Add(x) is the spelling. Assigning ReactiveList<T>.Value replaces the whole content with a copy, same as ReactiveList<T>.ReplaceAll. Cost: each mutation copies the whole list. For batches, prefer the single-notify bulk operations — ReactiveList<T>.AddRange, ReactiveList<T>.ReplaceAll, or ReactiveList<T>.Update — over per-item calls in a loop.
  class ReactiveList<T> : Reactive<List<T>>, IEnumerable, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // The number of items. Tracked read.
    int Count { get; }
    // The item at index. The getter is a tracked read; the setter replaces the item with one change notification.
    T this[int index] { get; set; }
    // The current items without dependency tracking. See Reactive<T>.Peek.
    IReadOnlyList<T> Peek { get; }
    // The current items as a read-only snapshot. Reading tracks a dependency like Reactive<T>.Value; assigning replaces the whole content with a copy of the given sequence (see ReactiveList<T>.ReplaceAll).
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
  // A reactive variable that automatically triggers UI updates when its value changes.
  // Remarks:
  // Reactive variables are the foundation of Ikon's reactive UI system. When a reactive value is read during UI rendering, the framework tracks this dependency. When the value changes, only the affected parts of the UI are sent to user. When to use: Use Reactive<T> for global state shared across all clients. For per-client state, use ClientReactive<T>. For per-user state (shared across a user's multiple sessions), use UserReactive<T>. Where to access: an unscoped Reactive<T> can be accessed anywhere. The scoped variants (ClientReactive, UserReactive, MountReactive) resolve .Value against the active scope, so they need one — UI.Root(), an action callback, or a ReactiveScope.Use() block. Accessing one with no such scope active throws instead of silently reading or writing some other partition. Background work (a Task.Run loop, a timer, an endpoint handler) has no scope, so it names its target instead: SetFor(id, value) / ValueFor(id).
  class Reactive<T> : IReactive
    // Creates a reactive whose initial value is default(T). Call as new Reactive<T>() — the UseDefault parameter is only an overload disambiguator and is never passed explicitly.
    ctor(UseDefault _ = default)
    // Creates a reactive with an explicit initial value: new Reactive<int>(0), new Reactive<Dictionary<int, Player>>(new()).
    ctor(T initialValue)
    // Reads the value for the currently-active scope without subscribing the current reactive computation to changes. Use inside renders for values that should not trigger re-renders.
    T Peek { get; }
    // The value for the currently-active scope. Reading inside a reactive computation (e.g. a UI render) subscribes it to changes; writing notifies subscribers when the value changed.
    // Remarks:
    // For the scoped variants (ClientReactive<T>, UserReactive<T>, MountReactive<T>) the scope must be active — inside UI.Root(), an action callback, or a ReactiveScope.Use block. From background work that carries no scope, name the target instead: SetFor(id, value) / ValueFor(id).
    T Value { get; set; }
    // Monotonic change counter for the currently-active scope's value, incremented on every write or Reactive<T>.NotifyUpdate. Lets consumers detect changes cheaply without comparing values.
    long Version { get; }
    // Opt this reactive out of hot-reload state capture. Use for runtime-only caches that hold non-serializable or cyclic object graphs and are rebuilt from their own backing store after a reload (e.g. orchestrator caches of live domain objects) — capturing them only fails noisily. Fluent: returns this so it can be chained onto a field initializer. Has no effect on long-term persistence, which only applies to non-None PersistenceScopes.
    Reactive<T> ExcludeFromHotReloadCapture()
    // Notifies subscribers that the current value changed without assigning it, for in-place mutations the setter cannot see (e.g. adding to a stored collection). Prefer Reactive<T>.Update, which mutates and notifies atomically.
    void NotifyUpdate()
    override string ToString()
    // Atomically read-modify-write the value for the currently-active scope. The transform runs under a per-scope lock, so concurrent mutations (e.g. appending to a shared list from parallel action handlers) serialize instead of racing — replacing the ad-hoc external locks that callers previously needed. Fires the change notification once.
    void Update(Func<T, T> mutator)
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
  // A reactive variable scoped to a specific scope type, providing isolated values per scope instance.
  // Remarks:
  // This is the base class for scoped reactive variables. Each unique scope instance (e.g., each client session or user) gets its own independent value. The framework automatically resolves the correct value based on the currently active scope. When to use: Use this directly when you need custom scope types. For common cases, prefer ClientReactive<T> (per-client) or UserReactive<T> (per-user). Important: The required scope must be active when accessing the value. Accessing outside the scope (e.g., outside UI.Root()) throws an InvalidOperationException.
  class Reactive<T, TScope> : Reactive<T> where TScope : IScopeKey
    ctor(T initialValue)
    ctor(Func<T> initialValue)
    // Writes the value for scope regardless of which scope — if any — is active, so background work can target a scope it does not run under without re-scoping itself.
    void SetFor(TScope scope, T value)
    // Atomically read-modify-writes the value for scope, under that scope's lock, regardless of which scope — if any — is active.
    void UpdateFor(TScope scope, Func<T, T> mutator)
    // Reads the value for scope regardless of which scope — if any — is active.
    T ValueFor(TScope scope)
  // Marker type for the default-value Reactive<T> constructor. Because every constructor carries trailing caller-info parameters, a marker parameter is what keeps the value-less overload distinct from Reactive(T initialValue, ...). Never pass it explicitly — write new Reactive<T>() and the value starts at default(T). Passing any argument at all selects the value constructor, so new Reactive<Dictionary<int, Player>>(new()) means what it reads as.
  struct UseDefault
  // A ReactiveDictionary<TKey, TValue> with a separate dictionary for each user, shared across their client sessions.
  // Remarks:
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
    ctor(Action body, params IReactive[] deps)
  // A ReactiveList<T> with a separate list for each user, shared across their client sessions.
  // Remarks:
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
  // A reactive variable with a separate value for each user, shared across their client sessions.
  // Remarks:
  // Use UserReactive when you need state that follows a user across their multiple client sessions. If a user connects from multiple clients, all clients share the same UserReactive values. Example use cases: • User preferences (theme, language) that should sync across clients • Shopping cart that persists across sessions • User-specific data that should be consistent everywhere When NOT to use: If you need state independent per client (e.g., form input, scroll position), use ClientReactive<T> instead. Where .Value works: anywhere the user scope is active — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block. Background work carries no user scope, so .Value there throws rather than writing to nowhere; name the user instead with UserReactive<T>.SetFor / UserReactive<T>.ValueFor.
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue)
    ctor(Func<string, T> initialValue)
    // Writes one user's value regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then write to it from anywhere.
    void SetFor(string userId, T value)
    // Atomically read-modify-writes one user's value, under that user's lock, regardless of which scope — if any — is active.
    void UpdateFor(string userId, Func<T, T> mutator)
    // Reads one user's value regardless of which scope — if any — is active.
    T ValueFor(string userId)

namespace Ikon.Common.Core.Scope
  // Scope for backend token context, transports the backend token of the caller.
  struct BackendTokenScope : IScopeKey
    ctor(string token)
    string Id { get; }
    string Name { get; }
  // Scope for client session context, providing unique identity for each connected client.
  // Remarks:
  // Each time a client connects to the server, it gets a new ClientScope with a unique ClientScope.Id (session ID). This scope is used by ClientReactive<T> to partition state per client. Relationship to UserScope: Multiple ClientScopes can belong to the same user. For example, a user connected from two clients has two different ClientScope IDs but the same UserScope ID. Lifecycle: Active during UI rendering inside UI.Root(). Automatically established by the framework for each client iteration.
  struct ClientScope : IScopeKey
    ctor(int sessionId)
    ctor(Context context)
    int Id { get; }
    string Name { get; }
  // Scope with a user-specified name and ID, enabling dynamic scoping without needing new struct types.
  struct CustomScope : IScopeKey
    ctor(string name, string id)
    string Id { get; }
    string Name { get; }
  interface IScopeKey
    object Id { get; }
    string Name { get; }
  // Identifies the Parallax render target ("mount") an app is currently producing UI for. An app may declare multiple mounts via Mounts; each (ClientScope, MountScope) pair gets its own per-render UI tree and an independent stream on the wire. Default mount id is "ikon-ui" — the value every app emits today on its single stream.
  // Remarks:
  // Pushed by the framework alongside UserScope / ClientScope during the per-(client, mount) render iteration in ReactiveRoot.RunAsync.
  struct MountScope : IScopeKey
    ctor(string mountId)
    string Id { get; }
    string Name { get; }
    // The mount id every Ikon app emits today on its single Parallax stream. Apps that don't override IAppBase.Mounts render under this id.
    const string DefaultMountId
  // Scope for grouping a single logical operation (e.g., LLM generation, image generation).
  struct OperationScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  // Scope for application run context, typically set at program startup in Program.cs. Used to group all log events and operations within a single application run.
  struct RunScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  // Scope for tenant/customer context, an arbitrary user-specified ID for scoping AI app logic.
  struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
  // Scope for end user identity context, providing unique identity for each user.
  // Remarks:
  // Identifies a logical user across their multiple client sessions. Used by UserReactive<T> to share state across a user's multiple connected clients. Lifecycle: Active during UI rendering inside UI.Root(). Automatically established by the framework alongside ClientScope.
  struct UserScope : IScopeKey
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
