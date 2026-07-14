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
    int? ClientSessionId { get; }
    string Description { get; }
    bool HasCallback { get; }
    bool HasPolicy { get; }
    Guid Id { get; }
    bool IsLocal { get; }
    bool IsRemote { get; }
    bool LlmCallOnlyOnce { get; }
    bool LlmInlineResult { get; }
    MethodInfo? MethodInfo { get; }
    string Name { get; }
    Ikon.Common.Core.Functions.FunctionParameter[] Parameters { get; }
    PolicyDelegate? Policy { get; }
    bool RequiresInstance { get; }
    Type ReturnType { get; }
    string ReturnTypeName { get; }
    string Version { get; }
    FunctionVisibility Visibility { get; }
    object? Call(object?[] args)
    Task<object?> CallAsync(object?[] args)
    IAsyncEnumerable<object?> CallAsyncEnumerable(object?[] args)
    IEnumerable<object?> CallEnumerable(object?[] args)
    override string ToString()
  class FunctionAttribute : Attribute
    ctor()
    ctor(string description, bool llmInlineResult = false, bool llmCallOnlyOnce = false)
    string Description { get; set; }
    bool LlmCallOnlyOnce { get; set; }
    bool LlmInlineResult { get; set; }
    string? Name { get; set; }
    override object TypeId { get; }
    FunctionVisibility Visibility { get; set; }
  static class FunctionCallContext
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
    IReadOnlyList<string>? AllowedValues { get; }
    object? DefaultValue { get; }
    string Description { get; }
    bool HasDefaultValue { get; }
    int Index { get; }
    bool IsNullableValueType { get; }
    string Name { get; }
    Type Type { get; }
    string TypeName { get; }
    override string ToString()
  class FunctionRegistry : AsyncLocalInstance<FunctionRegistry>
    ctor()
    Func<int, string?>? AuthSessionIdResolver { get; set; }
    string? CurrentVersion { get; set; }
    IReadOnlyDictionary<string, IReadOnlyList<Function>> Functions { get; }
    static Action? RemoteCallExecutionStarting { get; set; }
    bool RequireVerifiedCallerSpace { get; set; }
    Func<int, IReadOnlyCollection<string>?>? RolesResolver { get; set; }
    Func<int, IReadOnlyList<IScopeKey>>? ScopeResolver { get; set; }
    Func<int, string?>? UserIdResolver { get; set; }
    void AddFunction(Function function, FunctionVisibility? visibilityOverride = null)
    Task AttachProtocolAsync(IProtocolMessageChannel channel, int senderId)
    TResult Call<TResult>(string name, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    Task<TResult> CallAsync<TResult>(string name, CancellationToken cancellationToken = default, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    Task CallAsync(string name, CancellationToken cancellationToken = default, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    IAsyncEnumerable<TItem> CallAsyncEnumerable<TItem>(string name, CancellationToken cancellationToken = default, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    IEnumerable<TItem> CallEnumerable<TItem>(string name, object?[]? args = null)
    void ClearLocalFunctions()
    void ClearRemoteFunctions()
    void DetachProtocol()
    Task DisposeInstanceAsync(Guid instanceId, int? targetId = null)
    IReadOnlyCollection<int> GetClientSessionsWithFunction(string name)
    Function? GetFunction(string name)
    Function? GetFunction(string name, object?[] args)
    Function? GetFunction(string name, IReadOnlyList<Ikon.Common.Core.Protocol.FunctionParameter> protocolParameters)
    Function? GetFunction(string name, IReadOnlyList<Ikon.Common.Core.Protocol.FunctionParameter> protocolParameters, string version)
    Function? GetFunction(string name, int clientSessionId)
    IReadOnlyList<Function> GetFunctions(string name)
    bool HasFunction(string name)
    bool HasFunction(string name, int clientSessionId)
    Task<object?> InvokeLocalAsync(Function function, object?[] args)
    void RegisterFromAssembly(Assembly assembly, FunctionVisibility? visibilityOverride = null, string? version = null)
    void RegisterFromInstance(object instance, FunctionVisibility? visibilityOverride = null, string? version = null)
    void RegisterFromType<T>(FunctionVisibility? visibilityOverride = null, string? version = null)
    void RegisterFromType(Type type, FunctionVisibility? visibilityOverride = null, string? version = null)
    void RegisterFunctionMethod(object instance, MethodInfo method, string? name = null, FunctionVisibility visibility = Local)
    void RegisterFunctionsFromClientInitialization(ClientInitialization? clientInitialization)
    void RegisterRemoteFunction(Guid id, string name, Ikon.Common.Core.Functions.FunctionParameter[] parameters, Type returnType, string description, FunctionVisibility visibility, bool llmInlineResult, bool llmCallOnlyOnce, CallbackType callbackType, int clientSessionId, bool requiresInstance = false)
    bool RemoveFunction(string name, FunctionVisibility visibility)
    bool RemoveFunction(string name)
    void RemoveFunctionsByClientSessionId(int clientSessionId)
    Task StartProtocolAsync()
    Task StopProtocolAsync()
    void SyncFunctionsFromGlobalState(GlobalState globalState)
    bool TryGetFunction(string name, out Function? function)
    Task<bool> WaitForFunctionAsync(string functionName, TimeSpan? timeout = null, CancellationToken ct = default)
    event Action<ApprovalAuditEntry>? ApprovalCompleted
    event Action<int>? ClientSessionRemoved
    event Action<Function>? FunctionRegistered
    event Action<string>? FunctionUnregistered
    event Action<PolicyEvaluationResult>? PolicyEvaluated
  sealed class FunctionResultWithData<T>
    ctor(T value, byte[] data)
    byte[] Data { get; }
    T Value { get; }
  enum FunctionVisibility
    Local
    External
  sealed class InstanceNotFoundException : Exception
    ctor(Guid instanceId)
    Guid InstanceId { get; }
  class RegisterAllAttribute : Attribute
    ctor()
    bool LlmCallOnlyOnce { get; set; }
    bool LlmInlineResult { get; set; }
    FunctionVisibility Visibility { get; set; }
