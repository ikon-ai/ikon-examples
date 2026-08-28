# Function Registry

## Function Registry

Register callable functions that can be invoked by LLMs, clients, and pipelines.

### Direct Registration

```csharp
FunctionRegistry.Instance.AddFunction(
    Function.Register(MyMethod, "my_function",
        new FunctionAttribute { Description = "Description of what it does" }),
    FunctionVisibility.External);
```

### Attribute-Based Registration (Static Methods)

```csharp
public class MathFunctions
{
    [Function(Name = "Add", Description = "Adds two numbers", Visibility = FunctionVisibility.External)]
    public static int Add(int a, int b) => a + b;
}

FunctionRegistry.Instance.RegisterFromType(typeof(MathFunctions));
```

### Attribute-Based Registration (Instance Methods)

```csharp
[RegisterAll(Visibility = FunctionVisibility.External)]
public class GreetingFunctions(string greeting)
{
    [Function(Name = "Greet", Description = "Greets a person")]
    public string Greet(string name) => $"{greeting}, {name}!";
}

FunctionRegistry.Instance.RegisterFromInstance(new GreetingFunctions("Hello"));
```

### Pipeline Registration

```csharp
FunctionRegistry.Instance.RegisterPipeline<MyPipeline>("run_my_pipeline");
```

### Function Visibility

- `FunctionVisibility.External` - Advertised over the protocol; remote clients and LLMs can call it
- `FunctionVisibility.Local` - Only callable within the server process (the default)

`External` functions must declare auth intent or the substrate emits a startup WARN:

- `[RequireLogin]` - caller must have an authenticated session
- `[AllowAnonymous]` - explicit opt-in for genuinely open endpoints
- `[RequireRole("admin")]` - caller must hold a specific role (composes with `[RequireLogin]`)

```csharp
[Function(Visibility = FunctionVisibility.External)]
[RequireLogin]
public string GetUserSecret() => "for logged-in users only";

[Function(Visibility = FunctionVisibility.External)]
[AllowAnonymous]
public string GetPublicStatus() => "anyone can call this";
```

### Calling Functions

```csharp
var result = FunctionRegistry.Instance.Call<int>("Add", [2, 3]);
var result = await FunctionRegistry.Instance.CallAsync<string>("Greet", args: ["World"]);
```

### Exposing a function over HTTP

To expose a method as a public HTTP endpoint (a REST route or a third-party webhook), mark it `[HttpPost("/path")]` rather than `[Function]` — it is served under `https://{space}.ikonai.app/api/{path}`. (`[Function]` is for SDK/in-app calls and LLM tools, not inbound HTTP.) See the **HTTP endpoints & MCP tools** section under **Endpoints & Webhooks** for the handler-binding rules and URL details.

---

# Ikon.Common.Core Public API
namespace Ikon.Common.Core.Functions
  enum CallbackType
    Sync
    Async
    AsyncEnumerable
  readonly struct Function
    CallbackType CallbackType { get; }
    // Null means this is a local function (registered in this process).
    int? ClientSessionId { get; }
    string Description { get; }
    bool HasCallback { get; }
    bool HasPolicy { get; }
    Guid Id { get; }
    bool IsLocal { get; }
    bool IsRemote { get; }
    bool LlmCallOnlyOnce { get; }
    bool LlmInlineResult { get; }
    // Null for delegate-based registrations, constructors, or remote functions.
    MethodInfo? MethodInfo { get; }
    string Name { get; }
    Ikon.Common.Core.Functions.FunctionParameter[] Parameters { get; }
    // Null means the function is allowed to execute without policy checks.
    PolicyDelegate? Policy { get; }
    // When true and no callback is set, the function is metadata-only and can only be invoked with a provided InstanceId.
    bool RequiresInstance { get; }
    // For async functions this is the inner type (e.g. string for Task<string>); for async enumerable functions, the item type.
    Type ReturnType { get; }
    string ReturnTypeName { get; }
    // Empty string means unversioned (legacy or latest).
    string Version { get; }
    FunctionVisibility Visibility { get; }
    // Only valid for local sync functions.
    object? Call(object?[] args)
    // Only valid for local async functions.
    Task<object?> CallAsync(object?[] args)
    // Only valid for local async enumerable functions.
    IAsyncEnumerable<object?> CallAsyncEnumerable(object?[] args)
    // Only valid for local sync functions whose result implements IEnumerable.
    IEnumerable<object?> CallEnumerable(object?[] args)
    override string ToString()
  class FunctionAttribute : Attribute
    ctor()
    ctor(string description, bool llmInlineResult = false, bool llmCallOnlyOnce = false)
    string Description { get; set; }
    bool LlmCallOnlyOnce { get; set; }
    bool LlmInlineResult { get; set; }
    // Null uses the full type name plus method name.
    string? Name { get; set; }
    override object TypeId { get; }
    // If not set, defaults to Local for standalone functions, or inherits from [RegisterAll] for methods in a class with that attribute.
    FunctionVisibility Visibility { get; set; }
  static class FunctionCallContext
    // The session id of the client that issued the current function call, or null when the call did not originate from a remote client (e.g. local in-process invocation).
    static int? CallerSessionId { get; }
  sealed class FunctionCallException : Exception
    ctor(string message, string remoteTypeName, string remoteStackTrace)
    ctor(string message, string remoteTypeName, string remoteStackTrace, Exception? innerException)
    string RemoteStackTrace { get; }
    string RemoteTypeName { get; }
    const string RemoteFunctionCallerNotSetTypeName
  readonly struct FunctionParameter
    ctor(int index, string name, string description, Type type, bool hasDefaultValue, object? defaultValue, IReadOnlyList<string>? allowedValues = null)
    ctor(int index, string name, string description, string typeName, bool hasDefaultValue, object? defaultValue, IReadOnlyList<string>? allowedValues = null)
    // Optional override for the JSON-schema enum field emitted to the LLM. When non-null, the schema uses these values instead of Enum.GetNames(Type): narrow a static enum at registration time, or attach an enum to a non-enum parameter type whose allowed values come from runtime state. Pair with Description rebuilds for dynamic per-call documentation.
    IReadOnlyList<string>? AllowedValues { get; }
    object? DefaultValue { get; }
    string Description { get; }
    bool HasDefaultValue { get; }
    int Index { get; }
    bool IsNullableValueType { get; }
    string Name { get; }
    Type Type { get; }
    // Nullable value types are unwrapped to their underlying type for remote schema compatibility.
    string TypeName { get; }
    override string ToString()
  class FunctionRegistry : AsyncLocalInstance<FunctionRegistry>
    ctor()
    // Maps a caller session id to the auth session id — a per-login correlation identifier, not an authentication flag (cloud logins, including anonymous ones, always carry one). For guest detection use IsAnonymousResolver.
    Func<int, string?>? AuthSessionIdResolver { get; set; }
    // The version of the live/current registered implementation. When set, a caller that sends no version resolves to this version's functions instead of the greatest registered version; null keeps the greatest-version fallback.
    string? CurrentVersion { get; set; }
    IReadOnlyDictionary<string, IReadOnlyList<Function>> Functions { get; }
    // Maps a caller session id to whether the caller is an anonymous (guest) user: true for a guest, false for an authenticated user or machine, null for unknown sessions.
    Func<int, bool?>? IsAnonymousResolver { get; set; }
    // True while this call stack is executing a function on behalf of a remote caller. Flow-local, so concurrent local calls are unaffected.
    static bool IsExecutingRemoteCall { get; }
    // When set, the dispatcher rejects any remote call whose restored scopes carry no BackendTokenScope with a space claim. Off by default, so ordinary RPC hosts are unaffected.
    bool RequireVerifiedCallerSpace { get; set; }
    // Returns an empty/null collection for callers without roles. The dispatcher copies the result into PolicyCallContext.AdditionalContext under RoleBasedPolicy.RolesContextKey.
    Func<int, IReadOnlyCollection<string>?>? RolesResolver { get; set; }
    // Maps a caller session id to the reactive scopes active during the function body's execution — typically [ClientScope, UserScope] from the caller's Context. Wired by the host so ClientReactive<T> and UserReactive<T> resolve without the function body pushing scopes manually.
    Func<int, IReadOnlyList<IScopeKey>>? ScopeResolver { get; set; }
    // Returns null for unknown sessions or unauthenticated (guest) callers.
    Func<int, string?>? UserIdResolver { get; set; }
    void AddFunction(Function function, FunctionVisibility? visibilityOverride = null)
    Task AttachProtocolAsync(IProtocolMessageChannel channel, int senderId)
    TResult Call<TResult>(string name, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    // The async overloads take cancellationToken second and args third, unlike the synchronous Call<TResult> which takes args second. Pass the arguments by name when omitting the token: CallAsync<int>("Add", args: [2, 3]).
    Task<TResult> CallAsync<TResult>(string name, CancellationToken cancellationToken = default, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    Task CallAsync(string name, CancellationToken cancellationToken = default, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    IAsyncEnumerable<TItem> CallAsyncEnumerable<TItem>(string name, CancellationToken cancellationToken = default, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    IEnumerable<TItem> CallEnumerable<TItem>(string name, object?[]? args = null)
    // Remote functions are preserved.
    void ClearLocalFunctions()
    // Removes every remote function, keeping this registry's own local functions (the client re-advertises them via StartProtocolAsync). Called on protocol detach: remote functions were mirrored from the now-gone peer and are re-synced fresh on reconnect.
    void ClearRemoteFunctions()
    void DetachProtocol()
    Task DisposeInstanceAsync(Guid instanceId, int? targetId = null)
    IReadOnlyCollection<int> GetClientSessionsWithFunction(string name)
    // Throws if multiple functions with the same name are registered (use Call/CallAsync with the targetId parameter instead).
    Function? GetFunction(string name)
    Function? GetFunction(string name, object?[] args)
    Function? GetFunction(string name, IReadOnlyList<Ikon.Common.Core.Protocol.FunctionParameter> protocolParameters)
    // A non-empty version tries an exact version match first, then falls back to the greatest version; an empty version selects the greatest versioned function or falls back to unversioned.
    Function? GetFunction(string name, IReadOnlyList<Ikon.Common.Core.Protocol.FunctionParameter> protocolParameters, string version)
    Function? GetFunction(string name, int clientSessionId)
    IReadOnlyList<Function> GetFunctions(string name)
    bool HasFunction(string name)
    bool HasFunction(string name, int clientSessionId)
    // Bypasses the argument-type resolution that CallAsync performs: args must already line up with the function's parameter list. For callers that inject host-supplied parameters (e.g. a cron trigger building the array from Function.MethodInfo).
    Task<object?> InvokeLocalAsync(Function function, object?[] args)
    void RegisterFromAssembly(Assembly assembly, FunctionVisibility? visibilityOverride = null, string? version = null)
    void RegisterFromInstance(object instance, FunctionVisibility? visibilityOverride = null, string? version = null)
    // Instance methods require RegisterFromInstance instead.
    void RegisterFromType<T>(FunctionVisibility? visibilityOverride = null, string? version = null)
    // Instance methods require RegisterFromInstance instead.
    void RegisterFromType(Type type, FunctionVisibility? visibilityOverride = null, string? version = null)
    // Idempotent: a method already registered under the same name is left untouched. When name is null or empty the full member name ("{Type.FullName}.{Method}") is used.
    void RegisterFunctionMethod(object instance, MethodInfo method, string? name = null, FunctionVisibility visibility = Local)
    void RegisterFunctionsFromClientInitialization(ClientInitialization? clientInitialization)
    void RegisterRemoteFunction(Guid id, string name, Ikon.Common.Core.Functions.FunctionParameter[] parameters, Type returnType, string description, FunctionVisibility visibility, bool llmInlineResult, bool llmCallOnlyOnce, CallbackType callbackType, int clientSessionId, bool requiresInstance = false)
    bool RemoveFunction(string name, FunctionVisibility visibility)
    // Removes local functions only — remote functions with the same name are preserved. Returns true if any were removed.
    bool RemoveFunction(string name)
    void RemoveFunctionsByClientSessionId(int clientSessionId)
    // TODO(frozen-abi): exists only to maintain the shim above; delete with it.
    static void RemoveRemoteCallExecutionStartingSubscribers(AssemblyLoadContext loadContext)
    Task StartProtocolAsync()
    // Keeps the channel attached, unlike DetachProtocol; pending registrations are cleared.
    Task StopProtocolAsync()
    void SyncFunctionsFromGlobalState(GlobalState globalState)
    // Returns false rather than throwing when the name is unknown or resolves ambiguously (multiple overloads or multiple remote clients). Use GetFunction to resolve an overload by argument types.
    bool TryGetFunction(string name, out Function? function)
    // functionName: Name of the function to wait for.
    // timeout: How long to wait before giving up. Defaults to 30 seconds when null.
    // ct: Cancellation token.
    Task<bool> WaitForFunctionAsync(string functionName, TimeSpan? timeout = null, CancellationToken ct = default)
    event Action<ApprovalAuditEntry>? ApprovalCompleted
    // Fired when all of a client session's functions are removed because it disconnected (RemoveFunctionsByClientSessionId), so services tracking per-session state can release it promptly instead of discovering the dead session when a later push fails.
    event Action<int>? ClientSessionRemoved
    event Action<Function>? FunctionRegistered
    event Action<string>? FunctionUnregistered
    event Action<PolicyEvaluationResult>? PolicyEvaluated
  sealed class FunctionResultWithData<T>
    ctor(T value, byte[] data)
    byte[] Data { get; }
    T Value { get; }
  // A dispatch-scope axis only — auth gating is a separate concern declared via policy attributes ([RequireLogin], [AllowAnonymous], [RequireRole], ...).
  enum FunctionVisibility
    Local
    // External functions must declare their auth posture with [RequireLogin] or [AllowAnonymous] — a startup audit warns when neither is present.
    External
  sealed class InstanceNotFoundException : Exception
    ctor(Guid instanceId)
    Guid InstanceId { get; }
  // Function names are generated from the full type name (e.g. Namespace.Class.MethodName); individual members can use [Function] to override defaults.
  class RegisterAllAttribute : Attribute
    ctor()
    bool LlmCallOnlyOnce { get; set; }
    bool LlmInlineResult { get; set; }
    FunctionVisibility Visibility { get; set; }
