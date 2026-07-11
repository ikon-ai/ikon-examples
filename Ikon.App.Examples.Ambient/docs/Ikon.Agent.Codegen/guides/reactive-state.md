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

**Never access `ClientReactive` or `UserReactive` values outside `UI.Root()`.** `Main()` runs before any client/user scope exists. All reads and writes of scoped reactive values must happen inside `UI.Root()` or inside event callbacks (onClick, onSubmit, etc.) which run within a scope. For background tasks, use `ReactiveScope.Use()` to enter a scope explicitly.

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

On a `ReactiveList<T>`, reads (`Value`, `Count`, indexer, enumeration) are `IReadOnlyList<T>` — `_items.Value.Add(x)` does not compile; `_items.Add(x)` is the spelling and there is no `NotifyUpdate` to remember. Each mutation copies the list, so batch with `AddRange` / `ReplaceAll` / `Update` instead of per-item calls in a loop.

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
            view.ScrollArea(autoScroll: true, autoScrollKey: _messages.Count.ToString(),
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

// Manually set scope (e.g., in background tasks)
using var _ = ReactiveScope.Use(new ClientScope(clientSessionId));
_clientTheme.Value = "dark"; // Now targets the specified client
```

---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Reactive
  // Factory methods for creating ClientReactive`1 with per-client initialization.
  static class ClientReactive
    static ClientReactive<T> Create<T>(Func<int, T> factory, string file = "", string member = "")
  // Shorthand for ReactiveEffect<ClientScope>. Mirrors ClientReactive<T> as the per-client variant of Reactive<T>. Each connected client gets its own runner with independent cancel/queue, materialized on first dep change inside that client's scope.
  class ClientReactiveEffect : ReactiveEffect<ClientScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // A ReactiveList`1 with a separate list for each client session.
  class ClientReactiveList<T> : ReactiveList<T>
    ctor(string file = "", string member = "")
    ctor(IEnumerable<T> initialItems, string file = "", string member = "")
  // A reactive variable with a separate value for each client session.
  class ClientReactive<T> : Reactive<T, ClientScope>
    ctor(T initialValue, string file = "", string member = "")
  sealed class ReactiveManager.Handle
    string DebugDescription { get; set; }
    int? GroupId { get; set; }
    Guid Id { get; }
    bool IsUpdate { get; }
    ReactiveManager.Handle? Parent { get; }
    DateTime? UpdatedAt { get; }
    void StopTracking(bool isUpdating)
    override string ToString()
  sealed class HotReloadStateStore : AsyncLocalInstance<HotReloadStateStore>
    ctor()
    Dictionary<string, StoredReactiveState> CaptureAllForHotReload()
    void Clear()
    IReadOnlyList<PersistedRegistration> GetPersistedRegistrations()
    void LoadHotReloadStates(Dictionary<string, StoredReactiveState> states)
    void Register(string stableId, IReactiveWithState reactive, PersistenceScope persistence, PersistenceBackend backend = Private, string? postgresDatabase = null)
    // Look up a registered reactive by its stable id. Returns false if no reactive is registered under stableId or if the underlying reactive has been garbage-collected.
    bool TryGet(string stableId, out IReactiveWithState? reactive)
  interface IPersistedReactive : IReactiveWithState
    abstract void SetPublicUrl(string? url)
  interface IReactive
    long Version { get; }
    // Fires whenever this reactive's value changes (in any scope, for scoped variants). Payload-free so a single subscription can be taken across heterogeneous reactives — handlers fetch the new value via .Value when they need it. Used by ReactiveEffect and other dependency-tracked consumers.
    event Action? Changed
    // Fires with the scope-derived session id whose Signal<T> value just changed. For unscoped reactives the id is always 0; for ClientReactive<T> it is the hash of ClientScope; for UserReactive<T> the hash of UserScope; etc. Lets external subscription routing fan out to only the clients whose scope matches the changed signal.
    event Action<int>? SessionChanged
  interface IReactiveWithState
    // Whether this reactive's value is captured for hot-reload state preservation. Default true. Runtime-only caches that hold non-serializable or cyclic object graphs — and that rehydrate from their own backing store after a reload — opt out by returning false, so the hot-reload capture pass skips them instead of logging a (harmless) serialization warning every reload. Does not affect long-term persistence (which only ever touches non-None PersistenceScope s).
    bool CaptureForHotReload { get; }
    // Hash-derived session id that this reactive's .Value would resolve to under the currently-active ReactiveScope . Used by the subscription service to key per-scope subscriber routing. Default implementation returns 0 — override on per-scope reactives.
    int CurrentScopeSessionId { get; }
    string StableId { get; }
    abstract StoredReactiveState CaptureState()
    // Read this reactive's value for the currently-active scope, serialize to JSON, and trigger per-scope initialization if needed. Default implementation returns the session-0 value from CaptureState .
    virtual string ReadCurrentValueAsJson()
    abstract void RestoreState(StoredReactiveState state)
  // Factory methods for creating MountReactive`1 with per-mount initialization.
  static class MountReactive
    static MountReactive<T> Create<T>(Func<string, T> factory, string file = "", string member = "")
  // A ReactiveList`1 with a separate list for each Parallax mount in the active render iteration.
  class MountReactiveList<T> : ReactiveList<T>
    ctor(string file = "", string member = "")
    ctor(IEnumerable<T> initialItems, string file = "", string member = "")
  // A reactive variable with a separate value for each Parallax mount in the active render iteration.
  class MountReactive<T> : Reactive<T, MountScope>
    ctor(T initialValue, string file = "", string member = "")
  sealed class PersistedRegistration
    ctor(string stableId, IReactiveWithState reactive, PersistenceScope persistence, PersistenceBackend backend, string? postgresDatabase)
    PersistenceBackend Backend { get; }
    PersistenceScope Persistence { get; }
    string? PostgresDatabase { get; }
    IReactiveWithState Reactive { get; }
    string StableId { get; }
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
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = null)
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = null)
  // Convenience helpers on Reactive`1 for the busy-flag pattern that every async handler uses. Without these, the standard shape is verbose and easy to break: _busy.Value = true; try { await SlowThingAsync(); } finally { _busy.Value = false; } Forgetting finally leaves the flag stuck on if the call throws. AsToken collapses the shape to: using var _ = _busy.AsToken(); await SlowThingAsync(); — the flag flips to true on entry, the IDisposable returns it to false on dispose (including the catch-and-rethrow path of using).
  static class ReactiveBoolExtensions
    // Set the flag to true and return an IDisposable that returns it to false on dispose. Idempotent — disposing twice is safe (the second dispose is a no-op).
    static IDisposable AsToken(Reactive<bool> reactive)
  // Mutation helpers for Reactive`1 wrapping a collection. They mutate the underlying instance AND fire the change notification in one call so callers can write _items.Add(x) instead of the two-step _items.Value.Add(x); _items.NotifyUpdate();. Why these exist on a Reactive wrapping a mutable collection: the reference-equality check at the Value setter doesn't trigger when the underlying list is mutated in-place. Forgetting NotifyUpdate is the dominant "UI doesn't update after Add/Remove" bug class. These helpers make the right thing the easy thing. Every helper runs its mutation through the locked Update , so concurrent mutations from parallel handlers serialize instead of racing. Reassignment (_items.Value = [.. _items.Value, x]) still works and stays the right form when callers want immutable-style updates; these helpers are the in-place alternative for the common case. For list state, ReactiveList`1 offers the same one-call surface with copy-on-write snapshots and a read-only Value.
  static class ReactiveCollectionExtensions
    static void Add<T>(Reactive<List<T>> reactive, T item)
    static bool Add<T>(Reactive<HashSet<T>> reactive, T item)
    static void AddRange<T>(Reactive<List<T>> reactive, IEnumerable<T> items)
    static void Clear<T>(Reactive<List<T>> reactive)
    static void Clear<T>(Reactive<HashSet<T>> reactive)
    static void Clear<TKey, TValue>(Reactive<Dictionary<TKey, TValue>> reactive)
    static void Insert<T>(Reactive<List<T>> reactive, int index, T item)
    static void Mutate<T>(Reactive<T> reactive, Action<T> mutator)
    static bool Remove<T>(Reactive<List<T>> reactive, T item)
    static bool Remove<T>(Reactive<HashSet<T>> reactive, T item)
    static bool Remove<TKey, TValue>(Reactive<Dictionary<TKey, TValue>> reactive, TKey key)
    static int RemoveAll<T>(Reactive<List<T>> reactive, Predicate<T> match)
    static void RemoveAt<T>(Reactive<List<T>> reactive, int index)
    static void Set<TKey, TValue>(Reactive<Dictionary<TKey, TValue>> reactive, TKey key, TValue value)
  // Side-effect primitive that runs on tracked IReactive dependency changes. Mirrors the shape of Reactive`1 / Reactive`2 : this class is the unscoped (global) variant; ReactiveEffect`1 binds to a single scope type; further generic variants (forthcoming) compose multiple scopes the same way Reactive<T, TScope1, TScope2> does.
  class ReactiveEffect : IDisposable
    // Create an effect with an async body. The token cancels when a dep changes mid-run; respect it for clean cancellation.
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Create an effect with a sync body.
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // Side-effect primitive bound to a single scope type. Mirrors Reactive<T, TScope>: each instance of TScope gets its own per-scope effect runner with independent cancel/queue state, materialized lazily on first dep change in that scope. Unlike the global ReactiveEffect , this variant does NOT fire eagerly at construction — there's no scope active yet. The first dep change observed inside a scope of type TScope instantiates that scope's runner and fires the body for the first time. For "fire when scope first opens regardless of deps" lifecycle hooks (e.g. preload data on client connect), use the host app's existing scope-creation events directly.
  class ReactiveEffect<TScope> : IDisposable where TScope : struct, IScopeKey
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // A reactive list that automatically triggers UI updates on every mutation.
  class ReactiveList<T> : Reactive<List<T>>, IEnumerable, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    ctor(string file = "", string member = "")
    ctor(IEnumerable<T> initialItems, string file = "", string member = "")
    // The number of items. Tracked read.
    int Count { get; }
    T Item { get; set; }
    // The current items without dependency tracking. See Peek .
    IReadOnlyList<T> Peek { get; }
    // The current items as a read-only snapshot. Reading tracks a dependency like Value ; assigning replaces the whole content with a copy of the given sequence (see ReplaceAll ).
    IReadOnlyList<T> Value { get; set; }
    // Append item . One notification.
    void Add(T item)
    // Append items . One notification for the whole batch.
    void AddRange(IEnumerable<T> items)
    // Remove all items. One notification.
    void Clear()
    // Whether item is present. Tracked read.
    bool Contains(T item)
    // Enumerate a snapshot of the current items. Tracked read; the snapshot is safe to iterate while other code mutates the list.
    IEnumerator<T> GetEnumerator()
    // Index of the first occurrence of item , or -1. Tracked read.
    int IndexOf(T item)
    // Insert item at index . One notification.
    void Insert(int index, T item)
    // Remove the first occurrence of item . Returns whether it was found. One notification either way.
    bool Remove(T item)
    // Remove all items matching match . Returns the removed count. One notification either way.
    int RemoveAll(Predicate<T> match)
    // Remove the item at index . One notification.
    void RemoveAt(int index)
    // Replace the whole content with a copy of items . One notification.
    void ReplaceAll(IEnumerable<T> items)
    // Sort the items using comparison . One notification.
    void Sort(Comparison<T> comparison)
    // Atomically replace the content: transform sees the current items and returns the new ones, which are materialized into a fresh list. Runs under the same lock as all other mutations, so concurrent updates serialize. One notification.
    void Update(Func<IReadOnlyList<T>, IEnumerable<T>> transform)
  class ReactiveManager : IDisposable
    ctor(string category)
    string Category { get; }
    // Live-handle count above which Dispose logs a leak warning. The default suits the classic one-render-handle-per-client shape; per-container subtree rendering legitimately holds one handle per UI container, so its owner raises this to keep the warning meaningful.
    int DisposeHandleWarningThreshold { get; set; }
    // Number of currently registered (dispatchable) handles. Diagnostic counterpart of DisposeHandleWarningThreshold .
    int LiveHandleCount { get; }
    int UpdatedHandleCount { get; }
    void DecrementUICreationOngoing()
    void Dispose()
    void IncrementUICreationOngoing()
    void OnDeleted(Guid id)
    void Reactive(Action<ReactiveManager.Handle> callback)
    Task ReactiveAsync(Func<ReactiveManager.Handle, Task> callback)
    void StopTrackingAll()
    // Detach the current execution flow from any in-progress reactive callback so that reactive reads and writes made here are treated as ordinary access instead of being attributed to — or, for writes, swallowed by the re-entrancy guard of — the enclosing callback. This is needed when background work (a Run , a continuation, a timer) is started from INSIDE a reactive callback, e.g. a UI render that, while rendering, kicks off a fire-and-forget task to resolve an image and then bumps a reactive to re-render once it arrives. ExecutionContext flows the callback's async-local into that task, so without detaching the bump is misclassified as happening "within a reactive callback", dropped, and the UI never refreshes. Unlike SuppressFlow , this leaves every other ambient value (reactive scopes, async-local singletons) intact, so code inside still resolves the session's services correctly. Wrap the detached work in a using block (or hold the returned handle for the lifetime of the background task) and the original tracking is restored on dispose.
    static IDisposable SuppressCallbackTracking()
    Task UpdateAsync()
    event EventHandler<Guid>? Deleted
    event EventHandler? ReactiveObjectUpdated
    event EventHandler<Guid>? Updating
  // App-scoped lookup mapping a reactive's source-code member name (the C# field or property declared on the App class) to its StableId . Built by reflection at App startup; consumed by the Ikon.Reactive.GetStableIdByName framework function so frontends can subscribe by member name instead of needing a per-app helper RPC to fetch the hashed id.
  static class ReactiveNameIndex
    // Drop every registered mapping. Used by hot-reload to clear stale entries from the previous App instance before the next instance re-indexes.
    static void Clear()
    // Register a reactive member-name → stableId mapping. Idempotent; re-registering the same name with the same stableId is a no-op.
    static void Register(string memberName, string stableId)
    // Look up a stableId by reactive member name. Returns false when the name was not indexed at startup (e.g. reactive declared in a helper class outside the App).
    static bool TryGet(string memberName, out string stableId)
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
  static class ReactiveScopeRestorer
    static IDisposable? Activate(IReadOnlyList<IScopeKey> scopes)
    static IScopeKey[] CaptureCurrent()
    static IScopeKey[] CopyInRestorableOrder(IList<IScopeKey> scopes)
  // Bridges Reactive`1 change notifications to remote clients over the existing function-call wire. Exposes three framework-shipped shared functions — Ikon.Reactive.Subscribe, Ikon.Reactive.Unsubscribe, and Ikon.Reactive.Update — so any FunctionRegistry -connected client can observe a server-side reactive value without registering a Parallax UI tree.
  sealed class ReactiveSubscriptionService : AsyncLocalInstance<ReactiveSubscriptionService>
    ctor()
    // Optional resolver: given a calling session id, returns the scopes that should be active during Subscribe/Unsubscribe so per-scope reactives resolve to the caller's natural session/user. Typically wired in app startup as sid => { var ctx = app.GlobalState.GetClientContext(sid); return [new ClientScope(ctx), new UserScope(ctx)]; }. When unset, the service falls back to [new ClientScope(sessionId)] only — ClientReactive`1 works, UserReactive`1 throws.
    Func<int, IReadOnlyList<IScopeKey>>? ScopeResolver { get; set; }
    // Wires this service's framework functions into the given registry. Call once during app/server startup, after the registry has its protocol channel attached.
    void AttachTo(FunctionRegistry registry)
    // Resolves a reactive's StableId by the C# field or property name on the App class. Used by the JS-side useReactive(client, name, ...) hook so frontends don't need a per-app helper RPC just to learn the stable id.
    string GetStableIdByName(string memberName)
    // Drop all subscriptions belonging to a session — call when a client disconnects to release subscriber-state without waiting for explicit unsubscribes.
    void RemoveSession(int sessionId)
    // Subscribe the calling session to changes on the reactive identified by stableId . Returns the current value as JSON. The caller receives subsequent updates via Ikon.Reactive.Update calls routed only to subscribers whose scope hash matches the changed signal.
    string Subscribe(string stableId, string mountId)
    // Unsubscribe the calling session from a reactive. Idempotent; calling for an unsubscribed reactive is a no-op. The mountId must match the value passed to Subscribe so the same scope hash is computed for symmetric removal.
    void Unsubscribe(string stableId, string mountId)
    static string GetStableIdByNameFunctionName
    static string SubscribeFunctionName
    static string UnsubscribeFunctionName
    static string UpdateFunctionName
  // A reactive variable that automatically triggers UI updates when its value changes.
  class Reactive<T> : IReactive, IReactiveWithState
    ctor(UseDefault _ = null, string file = "", string member = "")
    ctor(T initialValue, string file = "", string member = "")
    T Peek { get; }
    T Value { get; set; }
    long Version { get; }
    // Opt this reactive out of hot-reload state capture. Use for runtime-only caches that hold non-serializable or cyclic object graphs and are rebuilt from their own backing store after a reload (e.g. orchestrator caches of live domain objects) — capturing them only fails noisily. Fluent: returns this so it can be chained onto a field initializer. Has no effect on long-term persistence, which only applies to non-None PersistenceScope s.
    Reactive<T> ExcludeFromHotReloadCapture()
    void NotifyUpdate()
    override string ToString()
    // Atomically read-modify-write the value for the currently-active scope. The transform runs under a per-scope lock, so concurrent mutations (e.g. appending to a shared list from parallel action handlers) serialize instead of racing — replacing the ad-hoc external locks that callers previously needed. Fires the change notification once. See Update .
    void Update(Func<T, T> mutator)
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
  // A reactive variable scoped to a specific scope type, providing isolated values per scope instance.
  class Reactive<T, TScope> : Reactive<T> where TScope : IScopeKey
    ctor(T initialValue, string file = "", string member = "")
    ctor(Func<T> initialValue, string file = "", string member = "")
  class Signal<T> : IReactive
    ctor(T initial)
    T Peek { get; }
    T Value { get; set; }
    long Version { get; }
    void NotifyUpdate()
    // Atomically read-modify-write the value: mutator runs under a lock, so concurrent updates serialize and each sees the latest value — the read and the write cannot interleave. Fires the change notification exactly once. Unlike the setter this never skips on equality: a mutator that edits a mutable value in place and returns the same reference still notifies, which is the whole point of the primitive.
    void Update(Func<T, T> mutator)
    event Action? Changed
    event Action<int>? SessionChanged
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
  class StoredReactiveState
    ctor()
    ctor(string typeName, string memberName, int ordinal, Dictionary<int, string> sessionValues)
    string MemberName { get; set; }
    int Ordinal { get; set; }
    Dictionary<int, string> SessionValues { get; set; }
    string TypeName { get; set; }
  struct UseDefault
  // Shorthand for ReactiveEffect<UserScope>. Mirrors UserReactive<T> as the per-user variant of Reactive<T>. Each distinct user gets its own runner; the same user across multiple sessions shares one runner.
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // A ReactiveList`1 with a separate list for each user, shared across their client sessions.
  class UserReactiveList<T> : ReactiveList<T>
    ctor(string file = "", string member = "")
    ctor(IEnumerable<T> initialItems, string file = "", string member = "")
  // A reactive variable with a separate value for each user, shared across their client sessions.
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, string file = "", string member = "")
    ctor(Func<string, T> initialValue, string file = "", string member = "")

namespace Ikon.Common.Core.Scope
  // Scope for backend token context, transports the backend token of the caller.
  struct BackendTokenScope : IScopeKey
    ctor(string token)
    string Id { get; }
    string Name { get; }
  // Scope for client session context, providing unique identity for each connected client.
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
  // Identifies the Parallax render target ("mount") an app is currently producing UI for. An app may declare multiple mounts via Mounts ; each ( ClientScope , MountScope ) pair gets its own per-render UI tree and an independent stream on the wire. Default mount id is "ikon-ui" — the value every app emits today on its single stream.
  struct MountScope : IScopeKey
    ctor(string mountId)
    string Id { get; }
    string Name { get; }
    // The mount id every Ikon app emits today on its single Parallax stream. Apps that don't override IAppBase.Mounts render under this id.
    static string DefaultMountId
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
  class ScopeRestorer
    ctor(ScopeStack scopeStack)
    IDisposable? Activate(IReadOnlyList<IScopeKey> scopes)
    IScopeKey[] CaptureCurrent()
    static IScopeKey[] CopyInRestorableOrder(IList<IScopeKey> scopes)
  // Serializes and deserializes scopes for function call propagation.
  static class ScopeSerializer
    // Captures current scopes for inclusion in a function call. Excludes RunScope since each process has its own run context.
    static List<ActionFunctionCall.ScopeEntry> CaptureForFunctionCall()
    static IScopeKey[] Deserialize(IReadOnlyList<ActionFunctionCall.ScopeEntry> entries)
  class ScopeStack
    ctor()
    IList<IScopeKey> Current { get; }
    void Add(IScopeKey scope)
    TScope Get<TScope>() where TScope : struct, IScopeKey
    IScopeKey GetByName(string name)
    TScope? TryGet<TScope>() where TScope : struct, IScopeKey
    bool TryGet<TScope>(out TScope scope) where TScope : struct, IScopeKey
    IScopeKey? TryGetByName(string name)
    IDisposable Use(IScopeKey scope)
    IDisposable UseScopes(params IScopeKey[] scopes)
  // Scope for tenant/customer context, an arbitrary user-specified ID for scoping AI app logic.
  struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
  // Scope for end user identity context, providing unique identity for each user.
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
