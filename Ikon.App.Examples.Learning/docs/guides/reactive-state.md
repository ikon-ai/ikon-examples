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
```

### Persistent Reactives — survive app restarts

```csharp
// DEFAULT for app state — one bucket per SessionIdentity (the app's routing key)
private readonly PersistentSessionReactive<MyState> _state = new(new MyState());

// App-wide (rare) — same value for everyone in the space
private readonly PersistentReactive<int> _totalVisits = new(0);

// Follows a user across all of their client sessions
private readonly PersistentUserReactive<Prefs> _prefs = new(new Prefs());
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

// List mutation (mutate in place and notify)
_items.Value.Add(newItem);
_items.NotifyUpdate();

// Record mutation
_config.Value = _config.Value with { Theme = "dark" };
```

### Complete Example: Shared Messages + Per-Client Input

```csharp
// Shared state — all clients see the same messages
private readonly Reactive<List<string>> _messages = new([]);

// Per-client state — each client has their own input
private readonly ClientReactive<string> _input = new("");

public async Task Main()
{
    UI.Root([Page.Default], content: view =>
    {
        view.Column(["h-screen"], content: view =>
        {
            // All clients see the same messages
            view.ScrollArea(autoScroll: true, autoScrollKey: _messages.Value.Count.ToString(),
                rootStyle: ["flex-1 min-h-0 px-4"], content: view =>
            {
                foreach (var msg in _messages.Value)
                {
                    view.Text([Text.Body, "py-1"], msg);
                }
            });

            // Each client has their own input
            view.Row(["p-4 gap-2 flex-shrink-0"], content: view =>
            {
                view.TextField([Input.Default, "flex-1"],
                    value: _input.Value,
                    onValueChange: async v => _input.Value = v,
                    onSubmit: async submitted =>
                    {
                        _messages.Value.Add(submitted);
                        _messages.NotifyUpdate(); // Required for in-place list mutation
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
  static class ClientReactive
    static ClientReactive<T> Create<T>(Func<int, T> factory, string file = "", string member = "")
  class ClientReactiveEffect : ReactiveEffect<ClientScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  class ClientReactive<T> : Reactive<T, ClientScope>
    ctor(T initialValue, string file = "", string member = "")
  sealed class ReactiveManager.Handle
    string DebugDescription { get; set; }
    int? GroupId { get; set; }
    Guid Id { get; }
    bool IsUpdate { get; }
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
    bool TryGet(string stableId, out IReactiveWithState? reactive)
  interface IPersistedReactive : IReactiveWithState
    abstract void SetPublicUrl(string? url)
  interface IReactive
    long Version { get; }
    event Action Changed
    event Action<int> SessionChanged
  interface IReactiveWithState
    int CurrentScopeSessionId { get; }
    string StableId { get; }
    abstract StoredReactiveState CaptureState()
    virtual string ReadCurrentValueAsJson()
    abstract void RestoreState(StoredReactiveState state)
  static class MountReactive
    static MountReactive<T> Create<T>(Func<string, T> factory, string file = "", string member = "")
  class MountReactive<T> : Reactive<T, MountScope>
    ctor(T initialValue, string file = "", string member = "")
  sealed class PersistedRegistration
    ctor(string stableId, IReactiveWithState reactive, PersistenceScope persistence, PersistenceBackend backend, string? postgresDatabase)
    PersistenceBackend Backend { get; }
    PersistenceScope Persistence { get; }
    string PostgresDatabase { get; }
    IReactiveWithState Reactive { get; }
    string StableId { get; }
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
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = null)
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = null)
  static class ReactiveBoolExtensions
    static IDisposable AsToken(Reactive<bool> reactive)
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
  class ReactiveEffect : IDisposable
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  class ReactiveEffect<TScope> : IDisposable where TScope : struct, IScopeKey
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  class ReactiveManager : IDisposable
    ctor(string category)
    string Category { get; }
    int UpdatedHandleCount { get; }
    void DecrementUICreationOngoing()
    void Dispose()
    void IncrementUICreationOngoing()
    void OnDeleted(Guid id)
    void Reactive(Action<ReactiveManager.Handle> callback)
    Task ReactiveAsync(Func<ReactiveManager.Handle, Task> callback)
    void StopTrackingAll()
    Task UpdateAsync()
    event EventHandler<Guid> Deleted
    event EventHandler ReactiveObjectUpdated
    event EventHandler<Guid> Updating
  static class ReactiveNameIndex
    static void Clear()
    static void Register(string memberName, string stableId)
    static bool TryGet(string memberName, out string stableId)
  static class ReactiveScope
    static int ClientId { get; }
    static int? ClientIdOrNull { get; }
    static IList<IScopeKey> Current { get; }
    static string MountId { get; }
    static string MountIdOrNull { get; }
    static string UserId { get; }
    static string UserIdOrNull { get; }
    static void Add(IScopeKey scope)
    static TScope Get<TScope>()
    static IScopeKey GetByName(string name)
    static TScope? TryGet<TScope>()
    static bool TryGet<TScope>(out TScope scope)
    static IScopeKey TryGetByName(string name)
    static IDisposable Use(IScopeKey scope)
    static IDisposable Use(params IScopeKey[] scopes)
  static class ReactiveScopeRestorer
    static IDisposable Activate(IReadOnlyList<IScopeKey> scopes)
    static IScopeKey[] CaptureCurrent()
    static IScopeKey[] CopyInRestorableOrder(IList<IScopeKey> scopes)
  sealed class ReactiveSubscriptionService : AsyncLocalInstance<ReactiveSubscriptionService>
    ctor()
    Func<int, IReadOnlyList<IScopeKey>> ScopeResolver { get; set; }
    void AttachTo(FunctionRegistry registry)
    string GetStableIdByName(string memberName)
    void RemoveSession(int sessionId)
    string Subscribe(string stableId, string mountId)
    void Unsubscribe(string stableId, string mountId)
    static string GetStableIdByNameFunctionName
    static string SubscribeFunctionName
    static string UnsubscribeFunctionName
    static string UpdateFunctionName
  class Reactive<T> : IReactive, IReactiveWithState
    ctor(UseDefault _ = null, string file = "", string member = "")
    ctor(T initialValue, string file = "", string member = "")
    int CurrentScopeSessionId { get; }
    T Peek { get; }
    string StableId { get; }
    T Value { get; set; }
    long Version { get; }
    StoredReactiveState CaptureState()
    void NotifyUpdate()
    string ReadCurrentValueAsJson()
    void RestoreState(StoredReactiveState state)
    override string ToString()
    event Action Changed
    event Action<int> SessionChanged
    event Action<T> ValueChanged
    event Func<T, Task> ValueChangedAsync
  class Reactive<T, TScope> : Reactive<T> where TScope : IScopeKey
    ctor(T initialValue, string file = "", string member = "")
    ctor(Func<T> initialValue, string file = "", string member = "")
  class Signal<T> : IReactive
    ctor(T initial)
    T Peek { get; }
    T Value { get; set; }
    long Version { get; }
    void NotifyUpdate()
    event Action Changed
    event Action<int> SessionChanged
    event Action<T> ValueChanged
    event Func<T, Task> ValueChangedAsync
  class StoredReactiveState
    ctor()
    ctor(string typeName, string memberName, int ordinal, Dictionary<int, string> sessionValues)
    string MemberName { get; set; }
    int Ordinal { get; set; }
    Dictionary<int, string> SessionValues { get; set; }
    string TypeName { get; set; }
  struct UseDefault
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, string file = "", string member = "")
    ctor(Func<string, T> initialValue, string file = "", string member = "")

namespace Ikon.Common.Core.Scope
  struct BackendTokenScope : IScopeKey
    ctor(string token)
    string Id { get; }
    string Name { get; }
  struct ClientScope : IScopeKey
    ctor(int sessionId)
    ctor(Context context)
    int Id { get; }
    string Name { get; }
  struct CustomScope : IScopeKey
    ctor(string name, string id)
    string Id { get; }
    string Name { get; }
  interface IScopeKey
    object Id { get; }
    string Name { get; }
  struct MountScope : IScopeKey
    ctor(string mountId)
    string Id { get; }
    string Name { get; }
    static string DefaultMountId
  struct OperationScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  struct RunScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  class ScopeRestorer
    ctor(ScopeStack scopeStack)
    IDisposable Activate(IReadOnlyList<IScopeKey> scopes)
    IScopeKey[] CaptureCurrent()
    static IScopeKey[] CopyInRestorableOrder(IList<IScopeKey> scopes)
  static class ScopeSerializer
    static List<ActionFunctionCall.ScopeEntry> CaptureForFunctionCall()
    static IScopeKey[] Deserialize(IReadOnlyList<ActionFunctionCall.ScopeEntry> entries)
  class ScopeStack
    ctor()
    IList<IScopeKey> Current { get; }
    void Add(IScopeKey scope)
    TScope Get<TScope>()
    IScopeKey GetByName(string name)
    TScope? TryGet<TScope>()
    bool TryGet<TScope>(out TScope scope)
    IScopeKey TryGetByName(string name)
    IDisposable Use(IScopeKey scope)
    IDisposable UseScopes(params IScopeKey[] scopes)
  struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
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
