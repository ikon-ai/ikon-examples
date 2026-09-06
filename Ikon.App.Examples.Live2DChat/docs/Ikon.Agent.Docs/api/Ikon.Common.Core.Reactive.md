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
  // Tracks dependencies exactly as Reactive<T> does, so bound UI re-renders normally. For state an owner derives and publishes, where assigning would desync it. No implicit unwrap to T — read Value or Peek.
  interface IReadOnlyReactive<T> : IReactive
    // Reads without registering a dependency, so a render reading it does not re-run when the value later changes.
    T Peek { get; }
    // A TRACKED read: used during render it registers a dependency, and for scoped variants it throws InvalidOperationException when no scope is active.
    T Value { get; }
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
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
  class Reactive<T> : IReadOnlyReactive<T>
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
