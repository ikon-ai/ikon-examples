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

// Persisted across app restarts (saved to cloud storage automatically)
private readonly PersistentReactive<int> _totalVisits = new(0);
```

`PersistentReactive<T>` values survive app restarts by persisting to cloud storage. Use for counters, settings, or any state that should not reset on redeployment.

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

### ReactiveScope Context

Inside UI callbacks, access the current client/user context:

```csharp
var clientId = ReactiveScope.ClientId;

// Manually set scope (e.g., in background tasks)
using var _ = ReactiveScope.Use(new ClientScope(clientSessionId));
_clientTheme.Value = "dark"; // Now targets the specified client
```

---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Reactive
  class ClientReactive<T> : Reactive<T, ClientScope>
    ctor(T initialValue, string file = "", string member = "")
    ctor(Func<int, T> initialValue, string file = "", string member = "")
  sealed class ReactiveManager.Handle
    ctor(ReactiveManager reactiveManager, Func<ReactiveManager.Handle, Task> callback, Guid id, int number)
    string DebugDescription { get;  set; }
    int? GroupId { get;  set; }
    Guid Id { get; }
    bool IsUpdate { get; }
    string OptimisticActionId { get; }
    DateTime? UpdatedAt { get; }
    void StopTracking(bool isUpdating)
    override string ToString()
  sealed class HotReloadStateStore : AsyncLocalInstance<HotReloadStateStore>
    ctor()
    Dictionary<string, StoredReactiveState> CaptureAllForHotReload()
    Dictionary<string, StoredReactiveState> CaptureForStorage()
    void Clear()
    void LoadHotReloadStates(Dictionary<string, StoredReactiveState> states)
    void LoadStorageStates(Dictionary<string, StoredReactiveState> states)
    void Register(string stableId, IReactiveWithState reactive, bool persistent)
  interface IReactive
  interface IReactiveWithState
    string StableId { get; }
    abstract StoredReactiveState CaptureState()
    abstract void RestoreState(StoredReactiveState state)
  sealed class OptimisticClientFunctionCallInfo
    ctor(int clientSessionId, string functionName, List<FunctionParameter> parameters, string resultTypeName, int callIndex)
    int CallIndex { get; }
    int ClientSessionId { get; }
    string FunctionName { get; }
    List<FunctionParameter> Parameters { get; }
    string ResultTypeName { get; }
  struct OptimisticClientFunctionKey : IEquatable<OptimisticClientFunctionKey>
    ctor(string functionName, int clientSessionId, int callIndex)
    int CallIndex { get; }
    int ClientSessionId { get; }
    string FunctionName { get; }
  sealed class OptimisticClientUpdateEventArgs : EventArgs
    ctor(string optimisticActionId, IReadOnlyList<OptimisticClientFunctionCallInfo> currentCalls, IReadOnlyList<OptimisticClientFunctionCallInfo> previousCalls)
    IReadOnlyList<OptimisticClientFunctionCallInfo> CurrentCalls { get; }
    string OptimisticActionId { get; }
    IReadOnlyList<OptimisticClientFunctionCallInfo> PreviousCalls { get; }
  static class Reactive
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception> onError = null, CancellationToken token = null)
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception> onError = null, CancellationToken token = null)
  class ReactiveManager : IDisposable
    ctor(string category)
    string Category { get; }
    static bool IsOptimisticUpdateInProgress { get; }
    int UpdatedHandleCount { get; }
    static IDisposable ActivateHydratedOptimisticContext(IReadOnlyList<OptimisticClientFunctionCallInfo> recordedCalls, IReadOnlyDictionary<OptimisticClientFunctionKey, object> results)
    void AddOptimisticUpdate(string optimisticActionId, Func<Task> optimisticUpdate)
    void DecrementUICreationOngoing()
    void Dispose()
    void IncrementUICreationOngoing()
    void OnDeleted(Guid id)
    void Reactive(Action<ReactiveManager.Handle> callback)
    Task ReactiveAsync(Func<ReactiveManager.Handle, Task> callback)
    void StopTrackingAll()
    static bool TryConsumeHydratedClientFunctionResult(string functionName, int clientSessionId, out object result)
    static bool TryRegisterOptimisticClientFunctionCall(string functionName, int clientSessionId, List<FunctionParameter> parameters, string resultTypeName)
    bool TryTakeOptimisticClientCalls(string optimisticActionId, out List<OptimisticClientFunctionCallInfo> calls)
    Task UpdateAsync()
    Task UpdateOptimisticAsync()
    event EventHandler<Guid> Deleted
    event EventHandler<OptimisticClientUpdateEventArgs> OptimisticClientUpdateProduced
    event EventHandler ReactiveObjectUpdated
    event EventHandler<Guid> Updating
  static class ReactiveScope
    static int ClientId { get; }
    static int? ClientIdOrNull { get; }
    static IList<IScopeKey> Current { get; }
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
  class Reactive<T> : IReactive, IReactiveWithState
    ctor(UseDefault _ = null, string file = "", string member = "")
    ctor(T initialValue, string file = "", string member = "")
    T Peek { get; }
    string StableId { get; }
    T Value { get;  set; }
    StoredReactiveState CaptureState()
    void NotifyUpdate()
    void RestoreState(StoredReactiveState state)
    override string ToString()
    event Action<T> ValueChanged
    event Func<T, Task> ValueChangedAsync
  class Reactive<T, TScope> : Reactive<T> where TScope : IScopeKey
    ctor(T initialValue, string file = "", string member = "")
    ctor(Func<T> initialValue, string file = "", string member = "")
  class Signal<T> : IReactive
    ctor(T initial)
    T Peek { get; }
    T Value { get;  set; }
    void NotifyUpdate()
    event Action<T> ValueChanged
    event Func<T, Task> ValueChangedAsync
  class StoredReactiveState
    ctor()
    ctor(string typeName, string memberName, int ordinal, Dictionary<int, string> sessionValues)
    string MemberName { get;  set; }
    int Ordinal { get;  set; }
    Dictionary<int, string> SessionValues { get;  set; }
    string TypeName { get;  set; }
  struct UseDefault
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
