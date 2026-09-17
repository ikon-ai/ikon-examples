namespace Ikon.Common.Core.Reactive
  // Mutation helpers for a Reactive<T> that wraps a mutable collection: they mutate the underlying instance AND fire the change notification in one call, running through the locked Reactive<T>.Update so concurrent mutations serialize. Meant only for legacy Reactive<List<T>> / Reactive<Dictionary<TKey, TValue>> / Reactive<HashSet<T>> code — declaring one of those in an app project is build error IKON002. Prefer ReactiveList<T> / ReactiveDictionary<TKey, TValue> / ReactiveHashSet<T>, on which these same spellings bind to the copy-on-write instance members instead.
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
    // extension methods on Reactive<Dictionary<TKey, TValue>>: ReactiveCollectionExtensions{Clear, Remove, Set}
    // extension methods on Reactive<T>: ReactiveCollectionExtensions{Mutate}
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
    // extension methods on Reactive<HashSet<T>>: ReactiveCollectionExtensions{Add, Clear, Remove}
    // extension methods on Reactive<T>: ReactiveCollectionExtensions{Mutate}
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
    bool Exists(Predicate<T> match)
    T? Find(Predicate<T> match)
    List<T> FindAll(Predicate<T> match)
    int FindIndex(Predicate<T> match)
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
    // extension methods on IReadOnlyList<T>, using Ikon.Common.Core: ReadOnlyListExtensions{FindIndex, FindLastIndex, IndexOf}
    // extension methods on Reactive<List<T>>: ReactiveCollectionExtensions{Add, AddRange, Clear, Insert, Remove, RemoveAll, RemoveAt}
    // extension methods on Reactive<T>: ReactiveCollectionExtensions{Mutate}
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
    // Restores the caller's own scope of the same name on dispose, unlike Use, which drops it. Reactive scope only: nothing is mirrored to the log, because this is a value-routing hop rather than a unit of work.
    static IDisposable UseNested(IScopeKey scope)
  // Marker type for the default-value Reactive<T> constructor. Never pass it explicitly — write new Reactive<T>() and the value starts at default(T); passing any argument at all selects the value constructor.
  readonly struct UseDefault
  // Same reactive contract as Reactive<T>, partitioned per user and shared across that user's client sessions (use ClientReactive<T> when each client needs its own value). .Value resolves against the active user scope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throws when none is active. Background work carries no user scope, so name the user instead via SetFor / ValueFor.
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue)
    ctor(Func<string, T> initialValue)
    void SetFor(string userId, T value)
    void UpdateFor(string userId, Func<T, T> mutator)
    T ValueFor(string userId)
    // extension methods on Reactive<Dictionary<TKey, TValue>>: ReactiveCollectionExtensions{Clear, Remove, Set}
    // extension methods on Reactive<HashSet<T>>: ReactiveCollectionExtensions{Add, Clear, Remove}
    // extension methods on Reactive<List<T>>: ReactiveCollectionExtensions{Add, AddRange, Clear, Insert, Remove, RemoveAll, RemoveAt}
    // extension methods on Reactive<T>: ReactiveCollectionExtensions{Mutate}
    // extension methods on Reactive<bool>: ReactiveBoolExtensions{AsToken}
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Reads and mutations resolve against the active UserScope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throw when none is active: Main(), the constructor, Task.Run loops, timers and endpoint handlers carry no user scope, so reach one user's partition from there through the …For(userId, …) accessors with an id captured where the scope existed.
  class UserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    void ClearFor(string userId)
    bool RemoveFor(string userId, TKey key)
    void SetFor(string userId, TKey key, TValue value)
    void UpdateFor(string userId, Action<Dictionary<TKey, TValue>> transform)
    IReadOnlyDictionary<TKey, TValue> ValueFor(string userId)
    // extension methods on Reactive<Dictionary<TKey, TValue>>: ReactiveCollectionExtensions{Clear, Remove, Set}
    // extension methods on Reactive<T>: ReactiveCollectionExtensions{Mutate}
  // Shorthand for ReactiveEffect<UserScope>: each distinct user gets its own runner; the same user across multiple sessions shares one runner.
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Reads and mutations resolve against the active UserScope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throw when none is active: Main(), the constructor, Task.Run loops, timers and endpoint handlers carry no user scope, so reach one user's partition from there through the …For(userId, …) accessors with an id captured where the scope existed.
  class UserReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    bool AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Action<HashSet<T>> transform)
    IReadOnlyCollection<T> ValueFor(string userId)
    // extension methods on Reactive<HashSet<T>>: ReactiveCollectionExtensions{Add, Clear, Remove}
    // extension methods on Reactive<T>: ReactiveCollectionExtensions{Mutate}
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Reads and mutations resolve against the active UserScope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throw when none is active: Main(), the constructor, Task.Run loops, timers and endpoint handlers carry no user scope, so reach one user's partition from there through the …For(userId, …) accessors with an id captured where the scope existed.
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
    // extension methods on IReadOnlyList<T>, using Ikon.Common.Core: ReadOnlyListExtensions{FindIndex, FindLastIndex, IndexOf}
    // extension methods on Reactive<List<T>>: ReactiveCollectionExtensions{Add, AddRange, Clear, Insert, Remove, RemoveAll, RemoveAt}
    // extension methods on Reactive<T>: ReactiveCollectionExtensions{Mutate}
