namespace Ikon.Common.Core
  class AsyncLocalInstance<T> where T : new()
    ctor()
    // In global mode (the default) this lazily creates and returns a single process-wide singleton; in async-local mode (enabled via EnableAndInitAsyncLocalInstance) it returns the instance set on the current async flow — and THROWS AsyncLocalInstanceNotSetException when the flow has none (e.g. a Task.Run body or timer callback that did not inherit the context) rather than returning a shared fallback.
    static T Instance { get; }
    static void DisableAsyncLocalInstance()
    // Switches this type to async-local mode and seeds the current flow with a fresh instance. Call this before SetAsyncLocalInstance — a set before the mode is enabled is ignored.
    static void EnableAndInitAsyncLocalInstance()
    // A no-op when async-local mode has not been enabled via EnableAndInitAsyncLocalInstance — the caller then keeps reading the process-global singleton. A warning is written to the console in that case so the ineffective set is not silent; scoped code that depends on the set taking effect must enable the mode first.
    static void SetAsyncLocalInstance(T value)
  sealed class AsyncLocalInstanceAttribute : Attribute
    ctor()
  // Thrown by AsyncLocalInstance<T>.Instance when async-local mode is enabled but no instance has been set on the current flow — e.g. accessing Log.Instance from a Task.Run body or a timer callback that did not inherit the async-local context.
  sealed class AsyncLocalInstanceNotSetException : Exception
  class BackendQuotaExceededException : UserException
    ctor(string key, int current, int limit, string friendlyMessage)
    int Current { get; }
    string Key { get; }
    int Limit { get; }
  // Thrown when an email send names a sender identity the platform cannot honour — the space has no verified sending domain, or the requested sender domain is not a verified sending domain of the space. Retrying without the sender fields sends from the platform's own address instead.
  class EmailSenderNotAvailableException : UserException
    ctor(string friendlyMessage, string? senderDomain = null, string? hint = null)
    string? Hint { get; }
    // The sender domain the request named; null when the failure was not about a specific domain.
    string? SenderDomain { get; }
  // A logical fabric address: what a message is FOR, independent of where it currently lives. IdentityHash is the canonical SessionIdentityHash — for cells it already folds the cell type in, and CellType is carried alongside for directory lookup and provisioning.
  readonly struct Endpoint : IEquatable<Endpoint>
    ctor(EndpointKind kind, string cellType, string identityHash)
    // The [Cell] type name for EndpointKind.Cell; empty otherwise.
    string CellType { get; }
    string IdentityHash { get; }
    EndpointKind Kind { get; }
    // Identity keys must be the cell's SessionIdentity record parameter names verbatim (e.g. "ChannelId") — see SessionIdentityHash.ComputeForCell.
    static Endpoint ForCell(string cellType, IReadOnlyDictionary<string, string> sessionIdentity)
    static Endpoint ForClient(string identityHash)
    static Endpoint ForServer(string identityHash)
    static bool operator ==(Endpoint left, Endpoint right)
    static bool operator !=(Endpoint left, Endpoint right)
  enum EndpointKind
    Client
    Cell
    Server
  // The class field every failure event carries, so a query can separate "someone was told something" from "we have a defect" without matching on message text. Values are stable strings — they are read by dashboards long after the code that wrote them has changed.
  static class EventFailureClass
    // An unhandled exception reached the top of a process. This is the bucket that should be near zero.
    const string Defect
    // Something we call refused or did not answer: a model provider, the backend, a spawned toolchain.
    const string Dependency
    // Not a failure at all — a confirmation the caller has to give, or a deliberate stop. Recorded so the outcome is visible, never counted as an error.
    const string Expected
    // The caller asked for something impossible or malformed, and was told so. Not a defect.
    const string UserError
  // Resilient conversions between loosely typed LLM/tool payloads and strongly typed function parameters/results: primitives, arrays (including single-item arrays), Newtonsoft JSON tokens, with a System.Text.Json fallback.
  static class ExtendedCast
    static T? Convert<T>(object? value)
    // A null value against a NON-nullable value type yields that type's default — 0 for Int32, false for Boolean — NOT null, so a missing LLM field is indistinguishable from a real zero. Make the target nullable (e.g. int?) when the caller must tell "absent" from "zero".
    static object? Convert(object? value, Type targetType)
    // Tolerates the placeholders LLMs emit when a schema marks every property required but the field is nullable: "" for collections/objects becomes null, "" for bool becomes false, etc. Falls back to ExtendedCast conversion on type mismatch, so single-item-array wrapping applies.
    static object? FromJsonElement(JsonElement element, Type targetType)
  static class ExtendedCastExtensions
    static T? ExtendedCast<T>(this object? value)
    static object? ExtendedCast(this object? value, Type targetType)
  class FeatureNotEnabledException : UserException
    ctor(string featureKey, string friendlyMessage, string? hint = null)
    string FeatureKey { get; }
    string? Hint { get; }
  class HighPrecisionTimestamp : AsyncLocalInstance<HighPrecisionTimestamp>
    ctor()
    DateTime UtcNow { get; }
  interface ILogInfo
    object LogInfo { get; }
  interface IMessageChannel
    int SessionId { get; }
    IDisposable RegisterMessageHandler(Func<ProtocolMessage, ValueTask> handler, Opcode? opcodeGroupMask = null, Opcode[]? opcodes = null)
    ValueTask SendMessageAsync(ProtocolMessage message)
    ValueTask SendMessageAsync(IProtocolMessagePayload payload)
  interface IPlugin : IProtocolMessageChannel
    string ConnectTokenJson { get; }
    bool IsAuthTicketSent { get; }
    bool IsConnected { get; }
    // The AuthResponse from the most recent successful connect (entrypoints + auth ticket + client session). Cache it to drive a later ReconnectWithAuthResponseAsync.
    AuthResponse? LastAuthResponse { get; }
    DateTime ServerInitTime { get; set; }
    // The connect entry point: fetches the AuthResponse — entrypoints, auth ticket, and client session — via the /connect GET, then opens the transport.
    Task ConnectAsync2(string connectUrl, CancellationToken ct = default)
    Task ConnectAsync2(string host, int port, bool useTls, CancellationToken ct = default)
    void OverrideConfigValues(string overrideConfigJson)
    // Soft reconnect: reopen the transport reusing a previously-fetched AuthResponse (typically LastAuthResponse) instead of re-fetching via the /connect GET, so the server resumes the same session within its disconnect grace.
    Task ReconnectWithAuthResponseAsync(AuthResponse cachedAuthResponse, CancellationToken ct = default)
    Task StopAsync()
  interface IProtocolMessageChannel : IMessageChannel
    Context ClientContext { get; }
  static class Json
    static Dictionary<string, object> AsDict(string json)
    static Dictionary<string, object> ConvertDict(Dictionary<string, object> dict)
    // Clones via a JSON round trip — a JSON PROJECTION, not a faithful object clone: a polymorphic/derived runtime type collapses to T, members without a setter or that the serializer skips are dropped, and a reference cycle throws. Only for plain, tree-shaped, fully serializable data.
    static T DeepCopy<T>(T obj)
    static string Format(string json, JsonOptions? options = null)
    static T From<T>(string json, JsonOptions? options = null)
    static object? From(string json, Type type, JsonOptions? options = null)
    static object? From(string json, string typeName, JsonOptions? options = null)
    // Tolerant of LLM responses that wrap the JSON payload in a markdown code fence or STRINGIFY it (the document JSON-encoded as a string, bare or as an object's single property value): tries direct deserialization first and retries with the embedded payload on JsonException. Always uses System.Text.Json with the supplied JsonSerializerOptions — the Ikon JsonOptions engine switches are not honored; use FromLLMResponse<T> for those.
    static T FromLLMResponse<T>(string text, JsonSerializerOptions? options)
    // Like From<T>, but tolerant of LLM responses that wrap the JSON payload in a markdown code fence or stringify it: tries direct deserialization first and retries with the embedded payload on a parse failure.
    static T FromLLMResponse<T>(string text, JsonOptions? options = null)
    static Type? ResolveTypeByName(string typeName)
    static string To<T>(T obj, JsonOptions? options = null)
  // Serialization toggles for Json. Immutable; construct with named arguments for the toggles that differ from the defaults, e.g. new JsonOptions(camelCase: true). The default instance matches calling the Json methods without options.
  sealed class JsonOptions
    ctor(bool useJson5 = false, bool indentation = true, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false, bool caseInsensitive = false)
    bool CamelCase { get; }
    // Deserialization only. Ignored when serializing and when UseJson5 is set (Newtonsoft is already case-insensitive).
    bool CaseInsensitive { get; }
    // Only applies when EnumsAsNames is set.
    bool EnumCamelCase { get; }
    bool EnumsAsNames { get; }
    bool IncludeFields { get; }
    bool IncludeNull { get; }
    bool Indentation { get; }
    // Serialize/deserialize with Newtonsoft (JSON5-tolerant: comments, trailing commas) instead of System.Text.Json.
    bool UseJson5 { get; }
    static readonly JsonOptions Compact
  class Log : AsyncLocalInstance<Log>
    ctor()
    IList<IScopeKey> CurrentScopes { get; }
    bool ShowTimeDelta { get; set; }
    void AddDefaultLogHandlers()
    void AddLogEvent(LogEvent logEvent)
    void AddScope(IScopeKey scope)
    IDisposable? BeginTimer(string name, LogType logType = Debug)
    IDisposable CreateAsyncFlow(string? description = null)
    void Critical(string message)
    // The exception's full ToString() is appended to the message, so stack traces land in the log without interpolating the exception into the message.
    void Critical(string message, Exception exception)
    void Critical(Exception exception, string message)
    void Debug(string message)
    void DisableFileOutput()
    void EnableFileOutput(string filePath, bool append = false)
    void Error(string message)
    // The exception's full ToString() is appended to the message, so stack traces land in the log without interpolating the exception into the message.
    void Error(string message, Exception exception)
    void Error(Exception exception, string message)
    void Event(string name, object? parameters = null)
    // Logs message at Exception level and returns it unchanged, so the same call both records and supplies the throw message in one expression: throw new SomeException(Log.Instance.Exception("what went wrong")). It does not create, wrap, or rethrow any exception.
    string Exception(string message)
    TScope GetScope<TScope>() where TScope : struct, IScopeKey
    IScopeKey GetScopeByName(string name)
    void Info(string message)
    // Safe and idempotent to call. When RequireInitCall is false (the default) the queue pumps are already started by the constructor, so this returns without doing anything; it only starts them when RequireInitCall deferred that to here. Calling it a second time is a no-op.
    Task InitializeAsync()
    void LogMessage(LogType type, string message)
    void LogMessage2(LogType type, string filePath, int lineNumber, string memberName, string message)
    static LogParameter<T> Named<T>(string name, T value)
    void RemoveDefaultLogHandlers()
    static Sensitive<T> Sensitive<T>(T value, SensitivityPolicy sensitivityPolicy = Default)
    Task StopAsync()
    void Trace(string message)
    TScope? TryGetScope<TScope>() where TScope : struct, IScopeKey
    bool TryGetScope<TScope>(out TScope scope) where TScope : struct, IScopeKey
    IScopeKey? TryGetScopeByName(string name)
    void Usage(string usageName, double usage)
    void Usage(string usageName, Func<Task<double>> usageFunc)
    IDisposable UseScope(IScopeKey scope)
    IDisposable UseScopes(params IScopeKey[] scopes)
    Task WaitEmptyAsync()
    void Warning(string message)
    // The exception's full ToString() is appended to the message, so stack traces land in the log without interpolating the exception into the message.
    void Warning(string message, Exception exception)
    void Warning(Exception exception, string message)
    static void WriteErrorToConsole(string message)
    static void WriteToConsole(string message, ConsoleColor color)
    static void WriteWarningToConsole(string message)
    bool BlockWhenFull
    LogFilter ConsoleWriterFilter
    LogFilter FileWriterFilter
    LogFilter Filter
    // Rendered at the very start of every console/file log line, before the timestamp. Because Log is an async-local instance, each isolated server scope can carry its own prefix, making interleaved stdout from multiple in-process servers attributable.
    string Prefix
    static bool RequireInitCall
    bool ShowAsyncFlow
    string TraceFilter
    event Log.AsyncFlowFinishedHandler? AsyncFlowFinished
    event Log.LogEventHandler? LogEvent
  delegate Log.AsyncFlowFinishedHandler
    void AsyncFlowFinishedHandler(object sender, int asyncFlowId)
  delegate Log.LogEventHandler
    void LogEventHandler(object sender, LogEvent logEvent)
  class LogEvent
    ctor()
    // keepTokenScopeIds: false replaces the id of every token scope with the JWT placeholder. Only the usage payload passes true: the backend verifies those tokens to attribute the usage to a space.
    Dictionary<string, object?> GetParameters(bool includeExtraParameters = true, bool keepTokenScopeIds = false)
    string? GetParametersAsJson(bool includeExtraParameters = true, bool keepTokenScopeIds = false)
    int AsyncFlowId
    string EventName
    object? EventParameters
    string? EventParametersJsonRedacted
    int LineNumber
    string MemberName
    string Message
    LogEvent.Parameter[] Parameters
    string Path
    string Prefix
    int PreviousAsyncFlowId
    LogScopeEntry[] Scopes
    DateTime Time
    LogType Type
    double Usage
    string UsageName
  readonly struct LogEvent.Parameter
    ctor(string name, object? value)
    readonly string Name
    readonly object? Value
  enum LogFilter
    None
    Critical
    Error
    Warning
    Info
    Debug
    Trace
  readonly struct LogParameter<T>
    ctor(string name, T value)
    readonly string Name
    readonly T Value
  struct LogScopeEntry
    string Id { get; set; }
    string Type { get; set; }
  // A targeted send whose id list is EMPTY transmits nothing: an empty list is indistinguishable on the wire from no targets, which the server routes to everyone, so a filter matching nobody would otherwise reach exactly who it excluded. The default value names no audience and throws rather than guessing.
  readonly struct MediaTargets
    static MediaTargets Everyone { get; }
    // A targeted send that resolved to no clients. False for Everyone, and false for the default value, which throws instead.
    bool IsEmpty { get; }
    bool IsEveryone { get; }
    // null for Everyone, which is what the protocol's absent target list means. Throws on the default value.
    IReadOnlyList<int>? SessionIds { get; }
    static MediaTargets To(int sessionId)
    static MediaTargets To(IReadOnlyList<int> sessionIds)
    static MediaTargets To(params int[] sessionIds)
    override string ToString()
  static class NameConversions
    static string ToCamelCase(string input)
    static string ToDisplayName(string input)
    static string ToKebabCase(string input)
    static string ToPascalCase(string input)
    static string ToSlug(string input, int maxLength)
    static string ToSnakeCase(string input)
  // A claim on a set of ports that lasts until the server holding them has stopped, so two servers starting in the same process are never handed the same port. Take ports through one lease per server, and dispose it when that server has released its sockets — never at the end of configuration, which reopens the race.
  sealed class PortLease : IDisposable
    ctor()
    // Releases every port this lease holds. Call it once the owning server's sockets are closed; calling it while the server still listens hands its ports to the next scanner.
    void Dispose()
    // Claims the first port at or above startPort that is free on both TCP and UDP and not already leased in this process. The scan runs under the process-wide gate, so a concurrent lease cannot observe the same port as free.
    int Take(int startPort)
    // Claims a port that something else already chose — a relay agent's local port, a value from config — so later scans in this process skip it. Claiming one this lease already holds, or one another lease holds, is a no-op: a lease only ever releases what it added itself.
    void TakeSpecific(int port)
  // A reactive version of the protocol GlobalState: each property is wrapped in a Reactive so a UI binding to it updates only when the value changes.
  class ReactiveGlobalState
    ctor()
    // Empty outside a cloud run.
    Reactive<string> AppSessionId { get; }
    Reactive<Dictionary<string, GlobalState.AudioStreamState>> AudioStreams { get; }
    // Keyed by client session id; each Context carries that client's user id, device, viewport, and locale.
    Reactive<Dictionary<int, Context>> Clients { get; }
    Reactive<bool> DebugMode { get; }
    // The current first human user; reassigned when that user leaves. Contrast PrimaryUserId, which is fixed.
    Reactive<string> FirstUserId { get; }
    // Keyed by client session id.
    Reactive<Dictionary<int, List<ActionFunctionRegister>>> Functions { get; }
    Reactive<string> IkonServerId { get; }
    Reactive<string> OrganisationName { get; }
    // The session owner from server config, fixed for the session's lifetime; used for user-specific asset storage paths.
    Reactive<string> PrimaryUserId { get; }
    // True when the app is being run through publicly accessible endpoints in local development.
    Reactive<bool> PublicAccess { get; }
    Reactive<ServerRunType> ServerRunType { get; }
    Reactive<string> SessionIdentityHash { get; }
    Reactive<string> SessionUrl { get; }
    Reactive<string> SpaceId { get; }
    Reactive<string> SpaceName { get; }
    Reactive<string> SpaceUrl { get; }
    Reactive<Dictionary<string, GlobalState.UIStreamState>> UIStreams { get; }
    Reactive<Dictionary<string, GlobalState.VideoStreamState>> VideoStreams { get; }
    Context? GetClientContext(int clientSessionId)
    // The first connected client context of this user, or null when the user has none.
    Context? GetClientContext(string userId)
    IEnumerable<Context> GetHumanClients()
    // One context per distinct AuthSessionId — a user with multiple clients contributes only the first by iteration order.
    IEnumerable<Context> GetUniqueAuthClientContexts()
    // One human context per distinct AuthSessionId — a user with multiple clients contributes only the first by iteration order.
    IEnumerable<Context> GetUniqueHumanAuthClientContexts()
    // Updates from a new GlobalState; only the reactive properties whose value actually changed trigger notifications.
    void UpdateFrom(GlobalState newState)
