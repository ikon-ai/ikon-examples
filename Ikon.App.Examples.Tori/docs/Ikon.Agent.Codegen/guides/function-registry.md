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
  // The type of callback a registered function uses.
  enum CallbackType
    Sync
    Async
    AsyncEnumerable
  // Immutable representation of a function with metadata and optional callbacks. Consolidates FunctionInfo, RegisteredFunction, and KernelContext.Function into a single type.
  struct Function
    // JSON deserialization constructor. Resolves ReturnType from ReturnTypeName string. Creates a function without callbacks (for remote/metadata-only use).
    ctor(Guid id, string name, FunctionParameter[] parameters, string returnTypeName, string description, FunctionVisibility visibility, bool llmInlineResult, bool llmCallOnlyOnce, CallbackType callbackType, int? clientSessionId, bool requiresInstance = false, string? version = null)
    // Primary constructor for creating functions with callbacks.
    ctor(Guid id, string name, FunctionParameter[] parameters, Type returnType, string description, FunctionVisibility visibility, bool llmInlineResult, bool llmCallOnlyOnce, CallbackType callbackType, int? clientSessionId, Func<object?[], object?>? callback, Func<object?[], Task<object?>>? callbackAsync, Func<object?[], IAsyncEnumerable<object?>>? callbackAsyncEnumerable, MethodInfo? methodInfo = null, bool requiresInstance = false, PolicyDelegate? policy = null, string? version = null)
    // The type of callback (Sync, Async, or AsyncEnumerable).
    CallbackType CallbackType { get; }
    // The clientSessionId of the client who registered this function. Null means this is a local function (registered in this process).
    int? ClientSessionId { get; }
    // Description of what the function does. Passed to LLM for tool description.
    string Description { get; }
    // True if this function has a callback that can be invoked locally.
    bool HasCallback { get; }
    // True if this function has a policy attached.
    bool HasPolicy { get; }
    // Unique identifier for this function.
    Guid Id { get; }
    // True if this function is local (registered in this process).
    bool IsLocal { get; }
    // True if this function is remote (registered by another client).
    bool IsRemote { get; }
    // If true, the LLM can only call this function once per generation pass.
    bool LlmCallOnlyOnce { get; }
    // If true, the LLM can inline the function result directly without tool call overhead.
    bool LlmInlineResult { get; }
    // The MethodInfo for the underlying method. Exposed so external introspection (e.g. the startup auth-marker audit in Ikon.App) can read method-level attributes. Null for delegate-based registrations, constructors, or remote functions.
    MethodInfo? MethodInfo { get; }
    // The name of the function (used for lookup and LLM tool name).
    string Name { get; }
    // The parameters of the function.
    FunctionParameter[] Parameters { get; }
    // Optional policy delegate for evaluating whether this function can be called. If null, the function is allowed to execute without policy checks.
    PolicyDelegate? Policy { get; }
    // True if this function requires an instance to be invoked. When true and no callback is set, the function is metadata-only and can only be invoked with a provided InstanceId.
    bool RequiresInstance { get; }
    // The return type of the function. Stored directly for performance. For async functions, this is the inner type (e.g., string for Task<string>). For async enumerable functions, this is the item type.
    Type ReturnType { get; }
    // The full name of the return type. Computed from ReturnType for JSON serialization.
    string ReturnTypeName { get; }
    // The version of the library that registered this function. Empty string means unversioned (legacy or latest).
    string Version { get; }
    // Whether the function should be distributed to other clients.
    FunctionVisibility Visibility { get; }
    // Calls the function synchronously. Only valid for local sync functions.
    object? Call(object?[] args)
    // Calls the function asynchronously. Only valid for local async functions.
    Task<object?> CallAsync(object?[] args)
    // Calls the function as an async enumerable call. Only valid for local async enumerable functions.
    IAsyncEnumerable<object?> CallAsyncEnumerable(object?[] args)
    // Calls the function synchronously and returns an enumerable result. Only valid for local sync functions whose result implements IEnumerable.
    IEnumerable<object?> CallEnumerable(object?[] args)
    static Function Create<TResult>(string name, string description, Func<TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, TResult>(string name, string description, Func<T1, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, TResult>(string name, string description, Func<T1, T2, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, TResult>(string name, string description, Func<T1, T2, T3, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, TResult>(string name, string description, Func<T1, T2, T3, T4, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function, PolicyDelegate? policy = null)
    static Function Create<TResult>(string name, string description, Func<Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, TResult>(string name, string description, Func<T1, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, TResult>(string name, string description, Func<T1, T2, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, TResult>(string name, string description, Func<T1, T2, T3, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, TResult>(string name, string description, Func<T1, T2, T3, T4, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<TResult>(string name, string description, Func<IAsyncEnumerable<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, TResult>(string name, string description, Func<T1, IAsyncEnumerable<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, TResult>(string name, string description, Func<T1, T2, IAsyncEnumerable<TResult>> function, PolicyDelegate? policy = null)
    // Creates a Function definition from a delegate.
    static Function Register(Delegate function, string? name = null, FunctionAttribute? attribute = null, MethodInfo? methodInfo = null, PolicyDelegate? policy = null, Dictionary<string, string>? paramDescriptions = null)
    static Function Register<TResult>(Func<TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, TResult>(Func<T1, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, TResult>(Func<T1, T2, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<TResult>(Func<Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, TResult>(Func<T1, Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, TResult>(Func<T1, T2, Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<TResult>(Func<IAsyncEnumerable<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, TResult>(Func<T1, IAsyncEnumerable<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, TResult>(Func<T1, T2, IAsyncEnumerable<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    override string ToString()
    // Creates a new Function with modified properties. Null parameters keep existing values. Use clearClientSessionId=true to explicitly set ClientSessionId to null. Use clearPolicy=true to explicitly set Policy to null.
    Function With(Guid? id = null, string? name = null, FunctionParameter[]? parameters = null, Type? returnType = null, string? description = null, FunctionVisibility? visibility = null, bool? llmInlineResult = null, bool? llmCallOnlyOnce = null, CallbackType? callbackType = null, int? clientSessionId = null, Func<object?[], object?>? callback = null, Func<object?[], Task<object?>>? callbackAsync = null, Func<object?[], IAsyncEnumerable<object?>>? callbackAsyncEnumerable = null, MethodInfo? methodInfo = null, bool? requiresInstance = null, PolicyDelegate? policy = null, bool clearClientSessionId = false, bool clearMethodInfo = false, bool clearPolicy = false, string? version = null)
    // Returns a new Function with the specified parameter's AllowedValues set. Pass null to clear an existing override and fall back to the type-based enum (or no enum at all). Use together with WithParamDescription to ship dynamic enum + dynamic doc per pass: rebuild the Function at the start of each pass, plumb the current allowed transitions through the parameter description and the allowed-values list, and re-add to EmergePass.Tools.
    Function WithAllowedValues(string paramName, IReadOnlyList<string>? allowedValues)
    // Returns a new Function with the specified parameter's description updated.
    Function WithParamDescription(string paramName, string description)
  // Marks a method as a registerable function for the FunctionRegistry. Used for auto-registration via RegisterFromInstance/RegisterFromType/RegisterFromAssembly.
  class FunctionAttribute : Attribute
    ctor()
    ctor(string description, bool llmInlineResult = false, bool llmCallOnlyOnce = false)
    // Description of what the function does. Passed to LLM for tool description.
    string Description { get; set; }
    // If true, the LLM can only call this function once per generation pass.
    bool LlmCallOnlyOnce { get; set; }
    // If true, the LLM can inline the function result directly without tool call overhead.
    bool LlmInlineResult { get; set; }
    // Override the function name. If null, the full type name plus method name is used.
    string? Name { get; set; }
    // Override the inherited TypeId property with JsonIgnore for serialization.
    object TypeId { get; }
    // Whether the function should be distributed to other clients. If not set, defaults to Local for standalone functions, or inherits from [RegisterAll] for methods in a class with that attribute.
    FunctionVisibility Visibility { get; set; }
  // Per-call ambient context exposed to the body of a function dispatched by FunctionRegistry . Set by the registry's inbound dispatch path before invoking the function and cleared after.
  static class FunctionCallContext
    // The session id of the client that issued the current function call, or null when the call did not originate from a remote client (e.g. local in-process invocation).
    static int? CallerSessionId { get; }
  // Metadata about a function parameter.
  struct FunctionParameter
    // Primary constructor with Type directly.
    ctor(int index, string name, string description, Type type, bool hasDefaultValue, object? defaultValue, IReadOnlyList<string>? allowedValues = null)
    // JSON deserialization constructor. Resolves Type from TypeName string.
    ctor(int index, string name, string description, string typeName, bool hasDefaultValue, object? defaultValue, IReadOnlyList<string>? allowedValues = null)
    // Optional override for the JSON-schema enum field emitted to the LLM. When non-null, the schema uses these values instead of Enum.GetNames(Type). Lets callers narrow a static enum at registration time (e.g. "of these 7 enum members, only these 3 are valid right now") or attach an enum to a non-enum parameter type (e.g. a string field whose allowed values come from runtime state). Pair with Description rebuilds for dynamic per-call documentation.
    IReadOnlyList<string>? AllowedValues { get; }
    // The default value if HasDefaultValue is true.
    object? DefaultValue { get; }
    // Description of the parameter. Used by LLM for tool parameter descriptions.
    string Description { get; }
    // Whether the parameter has a default value.
    bool HasDefaultValue { get; }
    // The position of the parameter in the parameter list (0-based).
    int Index { get; }
    // Whether the parameter type is a nullable value type (e.g. int?, bool?).
    bool IsNullableValueType { get; }
    // The name of the parameter.
    string Name { get; }
    // The CLR type of the parameter. Stored directly for performance.
    Type Type { get; }
    // The full name of the parameter type. Computed from Type for JSON serialization. Nullable value types are unwrapped to their underlying type for remote schema compatibility.
    string TypeName { get; }
    override string ToString()
  // Central registry for functions that can be called locally or remotely. Supports both local and shared (distributed) function scopes.
  class FunctionRegistry : AsyncLocalInstance<FunctionRegistry>, BuiltInApprovalHandlers.IApprovalProtocolBridge
    ctor()
    // Optional resolver that maps a caller session id to the auth session id. Returns null or empty for unauthenticated (guest) callers.
    Func<int, string?>? AuthSessionIdResolver { get; set; }
    // All registered functions grouped by name.
    IReadOnlyDictionary<string, IReadOnlyList<Function>> Functions { get; }
    // Invoked at the start of a remote function call execution. Runs in the async context of the executing function, so subscribers can set AsyncLocal state.
    static Action? RemoteCallExecutionStarting { get; set; }
    // When set, the dispatcher rejects any remote call whose restored scopes carry no BackendTokenScope with a space claim. Turned on by delegating proxy hosts (e.g. the Ikon.AI library) that make platform-key calls on behalf of a caller and must never execute for an unidentified caller. Off by default so ordinary RPC hosts are unaffected.
    bool RequireVerifiedCallerSpace { get; set; }
    // Optional resolver that maps a caller session id to the set of roles the caller holds. Wired by the host (e.g. Ikon.App.App) so that RequireRoleAttribute / RoleBasedPolicy can gate calls. Returns an empty/null collection for callers without any roles. The dispatcher copies the result into AdditionalContext under the key RolesContextKey .
    Func<int, IReadOnlyCollection<string>?>? RolesResolver { get; set; }
    // Optional resolver that maps a caller session id to the reactive scopes that should be active during the function body's execution — typically [ClientScope, UserScope] derived from the caller's Context . Wired by the host (e.g. Ikon.App.App) so that ClientReactive`1 and UserReactive`1 resolve naturally without the function body having to push scopes manually via FunctionCallContext.CallerSessionId + Use .
    Func<int, IReadOnlyList<IScopeKey>>? ScopeResolver { get; set; }
    // Optional resolver that maps a caller session id to the user id associated with that session. Wired by the host (e.g. Ikon.App.App) so that policy evaluation has access to the caller's identity. Returns null for unknown sessions or unauthenticated (guest) callers.
    Func<int, string?>? UserIdResolver { get; set; }
    void AddFunction(Function function, FunctionVisibility? visibilityOverride = null)
    // Hooks the registry to a protocol channel so that remote function calls and registrations are handled automatically.
    Task AttachProtocolAsync(IProtocolMessageChannel channel, int senderId)
    TResult Call<TResult>(string name, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    Task<TResult> CallAsync<TResult>(string name, CancellationToken cancellationToken = null, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    Task CallAsync(string name, CancellationToken cancellationToken = null, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    IAsyncEnumerable<TItem> CallAsyncEnumerable<TItem>(string name, CancellationToken cancellationToken = null, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    IEnumerable<TItem> CallEnumerable<TItem>(string name, object?[]? args = null)
    // Removes all locally registered functions. Remote functions are preserved.
    void ClearLocalFunctions()
    // Removes every remote function, keeping only this registry's own local functions. Called on protocol detach (disconnect): remote functions were mirrored from the now-gone peer and are re-synced fresh from the peer's ClientInitialization/GlobalState on reconnect. Without this, reconnecting to a RESTARTED peer (new FunctionIds / new session id) leaves the pre-disconnect remote functions behind, so the same name ends up registered by two client sessions and a name-only call throws "Multiple remote clients (...) have registered function '...'". Local functions are preserved — the client re-advertises them to the peer via StartProtocolAsync.
    void ClearRemoteFunctions()
    // Stops protocol handling and detaches the registry from the channel.
    void DetachProtocol()
    // Disposes a remote instance.
    Task DisposeInstanceAsync(Guid instanceId, int? targetId = null)
    // Gets all client session IDs that have registered a function with the given name.
    IReadOnlyCollection<int> GetClientSessionsWithFunction(string name)
    // Gets the function with the given name. Throws if multiple functions with the same name are registered (use Call/CallAsync with targetId parameter instead).
    Function? GetFunction(string name)
    // Gets the function with the given name, using argument types to resolve overloads.
    Function? GetFunction(string name, object?[] args)
    // Gets the function with the given name, using protocol parameter type names to resolve overloads. Used by the protocol handler when receiving remote calls.
    Function? GetFunction(string name, IReadOnlyList<FunctionParameter> protocolParameters)
    // Gets a local function with the given name and version, using protocol parameter type names to resolve overloads. If version is non-empty, tries exact version match first, then falls back to greatest version. If version is empty, selects the greatest versioned function or falls back to unversioned.
    Function? GetFunction(string name, IReadOnlyList<FunctionParameter> protocolParameters, string version)
    // Gets a function with the given name from a specific client session.
    Function? GetFunction(string name, int clientSessionId)
    // Gets all functions with the given name.
    IReadOnlyList<Function> GetFunctions(string name)
    // Checks if a function with the given name exists.
    bool HasFunction(string name)
    // Checks if a function with the given name exists for a specific client session.
    bool HasFunction(string name, int clientSessionId)
    // Invoke an already-resolved local function with a pre-built positional argument array, bypassing the argument-type resolution that CallAsync performs. The args must already line up with the function's parameter list — used by callers that inject host-supplied parameters (e.g. a cron trigger building the array from MethodInfo to inject a context object). Returns the result, if any.
    Task<object?> InvokeLocalAsync(Function function, object?[] args)
    // Scans an assembly for types with [RegisterAll] or methods with [Function] attributes and registers them.
    void RegisterFromAssembly(Assembly assembly, FunctionVisibility? visibilityOverride = null, string? version = null)
    // Scans an instance for [RegisterAll] attribute or methods with [Function] attribute and registers them.
    void RegisterFromInstance(object instance, FunctionVisibility? visibilityOverride = null, string? version = null)
    void RegisterFromType<T>(FunctionVisibility? visibilityOverride = null, string? version = null)
    // Scans a type for [RegisterAll] attribute or methods with [Function] attribute and registers them. For instance methods, you need to use RegisterFromInstance instead.
    void RegisterFromType(Type type, FunctionVisibility? visibilityOverride = null, string? version = null)
    // Registers a single method as a function unless one is already registered under the same name. Used by the app layer to register [Cron] methods, which are registrable like [Function] even when they carry no [Function] attribute. Idempotent: a method already registered (e.g. because it also carries [Function] under the same name) is left untouched. When name is null or empty the full member name ("{Type.FullName}.{Method}") is used.
    void RegisterFunctionMethod(object instance, MethodInfo method, string? name = null, FunctionVisibility visibility = Local)
    void RegisterFunctionsFromClientInitialization(ClientInitialization? clientInitialization)
    // Registers a remote function (from another client via protocol).
    void RegisterRemoteFunction(Guid id, string name, FunctionParameter[] parameters, Type returnType, string description, FunctionVisibility visibility, bool llmInlineResult, bool llmCallOnlyOnce, CallbackType callbackType, int clientSessionId, bool requiresInstance = false)
    bool RemoveFunction(string name, FunctionVisibility visibility)
    // Removes all local functions with the given name. Remote functions with the same name are preserved. Returns true if any functions were removed.
    bool RemoveFunction(string name)
    // Removes all functions registered by a specific client session (when client disconnects).
    void RemoveFunctionsByClientSessionId(int clientSessionId)
    // Sends registrations for all functions and processes pending registrations.
    Task StartProtocolAsync()
    // Stops protocol handling but keeps the channel attached. Pending registrations are cleared.
    Task StopProtocolAsync()
    void SyncFunctionsFromGlobalState(GlobalState globalState)
    // Tries to get a function with the given name.
    bool TryGetFunction(string name, out Function? function)
    // Waits for a function with the given name to be registered.
    Task<bool> WaitForFunctionAsync(string functionName, TimeSpan timeout = null, CancellationToken ct = null)
    // Fired when an approval flow completes (approved or rejected). Use this event for audit logging of approval decisions.
    event Action<ApprovalAuditEntry>? ApprovalCompleted
    // Fired when all of a client session's functions are removed because it disconnected ( RemoveFunctionsByClientSessionId ). Lets services that track per-session state — e.g. ReactiveSubscriptionService's subscriber set — release it promptly instead of discovering the dead session only when a later push fails.
    event Action<int>? ClientSessionRemoved
    // Fired when a function is registered.
    event Action<Function>? FunctionRegistered
    // Fired when a function is unregistered.
    event Action<string>? FunctionUnregistered
    // Fired when a policy is evaluated for a function call.
    event Action<PolicyEvaluationResult>? PolicyEvaluated
  sealed class FunctionResultWithData<T>
    ctor(T value, byte[] data)
    byte[] Data { get; }
    T Value { get; }
  static class FunctionUtils
    static ValueTuple<string?, string> DecodeFunctionName(string encodedFunctionName)
    static string EncodeFunctionName(string? typeName, string functionName)
  // Determines whether a function is advertised over the protocol so remote clients can call it. This is a dispatch-scope axis only — auth gating is a separate concern declared via policy attributes ([RequireLogin], [AllowAnonymous], [RequireRole], ...).
  enum FunctionVisibility
    Local
    External
  // Marks a class for automatic registration of all public members (methods, properties, constructors). Used for auto-registration via RegisterFromInstance/RegisterFromType/RegisterFromAssembly. Function names are automatically generated using the full type name (e.g., Namespace.Class.MethodName). Individual members can use [Function] to override defaults.
  class RegisterAllAttribute : Attribute
    ctor()
    // If true, the LLM can only call each function once per generation pass. Individual members can override this with [Function].
    bool LlmCallOnlyOnce { get; set; }
    // If true, the LLM can inline function results directly without tool call overhead. Individual members can override this with [Function].
    bool LlmInlineResult { get; set; }
    // Whether the functions should be distributed to other clients. Default is Local (not distributed).
    FunctionVisibility Visibility { get; set; }
  sealed class RemoteFunctionCallRequest
    ctor(string functionName)
    CancellationToken CancellationToken { get; set; }
    string FunctionName { get; }
    Guid? InstanceId { get; set; }
    object?[]? Parameters { get; set; }
    bool PropagateScopes { get; set; }
    int? TargetId { get; set; }
    string? Version { get; set; }
  sealed class RemoteFunctionCaller
    ctor(IProtocolMessageChannel protocolMessageChannel, int senderId = 0, TimeSpan? actionAckTimeout = null, TimeSpan? callTimeout = null, int? enumerationBufferCapacity = null)
    TResult Call<TResult>(RemoteFunctionCallRequest request)
    void Call(RemoteFunctionCallRequest request)
    Task<TResult> CallAsync<TResult>(RemoteFunctionCallRequest request)
    Task CallAsync(RemoteFunctionCallRequest request)
    IAsyncEnumerable<TItem> CallAsyncEnumerable<TItem>(RemoteFunctionCallRequest request)
    // Cancels all pending calls with a connection closed exception. Called when the underlying connection is lost.
    void CancelAllPendingCalls()
    // Cancels pending calls targeting a specific client with a target-disconnected exception. Called when a target client leaves so callers fail fast instead of waiting for the ack timeout.
    void CancelPendingCallsForTarget(int targetId)
    static object CreateAsyncEnumerableParameter<T>(IAsyncEnumerable<T> source)
    static object CreateEnumerableParameter<T>(IEnumerable<T> source)
    static FunctionParameter CreateParameter<T>(T value)
    static FunctionParameter CreateParameter(Type type, object? value)
    Task DisposeInstanceAsync(Guid instanceId, int? targetId = null)
  // Records which path the version-aware function lookup took. Surfaced in failure events so the analytics tool can distinguish "no match at all" from "fell back from the requested version".
  enum VersionResolution
    None
    Exact
    Greatest
    Unversioned
    Other
