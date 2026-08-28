# Ikon.Common.Core Public API

namespace Ikon.Common.Core
  static class AppRoutes
    // True when path falls under a platform-reserved prefix (/ikon or /api, exactly or as a {prefix}/… subpath). The query string is ignored and a missing leading slash is tolerated; prefixes that merely share a leading substring (/ikonic) are NOT reserved.
    static bool IsReservedPath(string path)
    // A valid boot-snapshot variant id is 1-32 characters of lowercase ASCII letters, digits, or non-leading -.
    static bool IsValidVariantId(string? variantId)
    // Both inputs must already be canonical (TryCanonicalizePattern / TryCanonicalizeRoute). Matching is segment-wise: a literal matches by ordinal equality, * matches exactly one segment, and a final ** matches zero or more remaining segments — so / matches only the root and /** matches every path including the root.
    static bool RoutePatternMatches(string canonicalPattern, string canonicalPath)
    // A pattern is a canonical route whose segments may additionally be * (exactly one segment, any content) or a final ** (zero or more trailing segments). Rejects : anywhere (reserved as the seed-rule separator), ** before the final segment, and segments mixing literals with wildcards (such as a*b).
    // pattern: The pattern as declared, e.g. /*/**
    // canonical: The canonical form when the pattern is valid, otherwise empty
    // error: Why the pattern was rejected, otherwise null
    static bool TryCanonicalizePattern(string? pattern, out string canonical, out string? error)
    // Canonicalizes a route declared for boot-snapshot capture: percent-decoded, no trailing slash (except the root /). Rejects routes with a query string, fragment, backslash, control characters, or a platform-reserved prefix.
    // route: The route as declared, e.g. /live/listing/42
    // canonical: The canonical form when the route is valid, otherwise empty
    // error: Why the route was rejected, otherwise null
    static bool TryCanonicalizeRoute(string? route, out string canonical, out string? error)
    // A [BootSnapshot] seed rule has the form pattern:variantId. The pattern half is canonicalized via TryCanonicalizePattern; the variant id must satisfy IsValidVariantId.
    static bool TryParseSeedRule(string? rule, out string pattern, out string variantId, out string? error)
    // The platform reserves the entire /ikon subtree and /api. The load balancer intercepts these before they reach the app's SPA, so an app route under them can never be served.
    static readonly string[] ReservedPathPrefixes
  // Verifies platform-signed assertions (e.g. StepUpAssertion) issued by the Ikon platform backend. Fetches the platform JWKS from {platformBaseUrl}/.well-known/jwks.json on demand and caches the keys for five minutes, so a rotated signing key is picked up without recreating the verifier.
  sealed class AssertionVerifier
    ctor(string platformBaseUrl, HttpClient? httpClient = null, Func<DateTimeOffset>? clock = null)
    // Generic JWT validation: JWKS-backed signature verification, standard iss/aud/exp checks, and (when present) an iat clock-skew guard. The caller owns disposing the returned JsonDocument; the returned exp lets a caller cache the validated result for the token lifetime. Use this where the step-up projection in VerifyAsync isn't relevant.
    // token: The encoded JWT.
    // expectedIssuer: Required iss value.
    // expectedAudience: Required aud value (matches a string aud or any entry of an array aud).
    // ct: Cancellation token.
    Task<(JsonDocument Claims, DateTimeOffset ExpiresAt)> VerifyAndExtractClaimsAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = default)
    Task<StepUpAssertion> VerifyAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = default)
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
  static class AsyncLocalInstanceDiagnostics
    static bool WarnOnGlobalModeAccess
  // Thrown by AsyncLocalInstance<T>.Instance when async-local mode is enabled but no instance has been set on the current flow — e.g. accessing Log.Instance from a Task.Run body or a timer callback that did not inherit the async-local context.
  sealed class AsyncLocalInstanceNotSetException : Exception
    ctor(string message)
  class BackendQuotaExceededException : UserException
    ctor(string key, int current, int limit, string friendlyMessage)
    int Current { get; }
    string Key { get; }
    int Limit { get; }
  // Carries what a client says about ITSELF on the /connect request: the Teleport binary form of a ClientEnvironment, base64url-encoded. Unsigned, deliberately.
  static class ClientEnvironmentCodec
    static string Encode(ClientEnvironment environment)
    static ClientEnvironment FromConnectToken(ConnectToken token)
    // The environment to build this client's Context from: the one it presented, or — for a client that predates SdkCapabilities.ClientEnvironmentOnConnect and therefore sends none — the copy the token minter baked into the connect token.
    static ClientEnvironment Resolve(string? presented, ConnectToken token)
    static bool TryDecode(string? presented, out ClientEnvironment? environment)
    const string QueryParam
  // Encodes and verifies the connect token a client presents at /connect: the Teleport binary form of a ConnectToken with a truncated HMAC appended, base64url-encoded once. The key is the caller's per-server ConnectToken secret, never the platform secret.
  static class ConnectTokenCodec
    static string Encode(ConnectToken token, byte[] key)
    // The ConnectToken.ExpiresAt value for a token minted now with this lifetime.
    static uint ExpiresIn(TimeSpan lifetime)
    static long ToUnixSeconds(DateTime utc)
    // The signature is checked before the body is parsed, so a forged payload never reaches the deserializer.
    // nowUtc: Supplied rather than read here so tests can drive the expiry boundary.
    static ConnectTokenStatus TryDecode(string presented, byte[] key, DateTime nowUtc, out ConnectToken? token)
    static readonly TimeSpan DefaultLifetime
  enum ConnectTokenStatus
    Valid
    Malformed
    BadSignature
    // Correctly signed but past ConnectToken.ExpiresAt: the client should mint a fresh token rather than treat this as an unrecoverable authentication failure.
    Expired
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
  static class ExceptionFormatter
    static string FormatException(Exception ex, bool includeFilePaths = true)
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
  class FeatureFlagsStorage : AsyncLocalInstance<FeatureFlagsStorage>
    ctor()
    ImmutableDictionary<string, bool> ReadOnlyFeatureFlags { get; }
    bool Get(string featureFlagName)
    // The last write wins by default. Pass shouldOverride = false to keep an already-set value and only log the refused write.
    void Set(string featureFlagName, bool value, bool shouldOverride = true)
  class FeatureNotEnabledException : UserException
    ctor(string featureKey, string friendlyMessage, string? hint = null)
    string FeatureKey { get; }
    string? Hint { get; }
  class HighPrecisionTimestamp : AsyncLocalInstance<HighPrecisionTimestamp>
    ctor()
    DateTime UtcNow { get; }
  static class HostUtils
    // Clears ReadOnly attributes along the way (git marks its pack files read-only). Continues past individual failures and returns the paths that could not be deleted; an empty list means the directory is completely gone.
    static IReadOnlyList<string> DeleteDirectoryBestEffort(string path)
    // A racy probe: it binds and immediately closes a socket, so the port is free again the moment this returns — two concurrent scanners can pick the same port. usedPorts only de-duplicates within a single caller; to claim a port safely across concurrent starts in this process, go through PortLease.Take instead.
    static int FindAvailableTcpAndUdpPort(int startPort, HashSet<int>? usedPorts = null)
    static int FindAvailableUdpPortRange(int startPort, int count)
    static string GenerateDeviceId()
    static void OpenBrowser(string url)
    static bool TcpPortIsAvailable(int port)
    static bool UdpPortIsAvailable(int port)
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
    // The current connect entry point (a legacy unsuffixed ConnectAsync overload exists but is obsolete and hidden from this listing): fetches the AuthResponse — entrypoints, auth ticket, and client session — via the /connect GET, then opens the transport.
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
  // One legacy_usage_observed event per distinct (feature, detail, caller space) key per process, never per call.
  static class LegacyUsage
    // Returns whether this was in fact the first observation of the key in this process — later observations of the same key are dropped.
    // feature: One of the constants on this type.
    // detail: The dimension that decides the retirement threshold — a version, a capability level, a type name. Empty when the feature has no such dimension.
    // sessionId: The session the first observation came from, for tracing it back to a peer. Deliberately outside the dedup key: keying on it would emit one event per old client.
    // callerSpaceId: The space that made the call, for the features whose peer is a different space than the reporting server. Part of the key, because deduplicating on the detail alone would name only whichever space happened to call first. Empty — the usual case — means the peer belongs to the reporting server's own space, which the backend already stamps onto every event, so nothing is lost by leaving it out.
    static bool Report(string feature, string detail = "", int sessionId = 0, string callerSpaceId = "")
    const string PluginConnectAsyncV1
    const string ProtocolV1ActionCall
    const string RemovedPluginRequested
    const string RpcPayloadVersion
    const string SdkCapabilityLevel
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
    Dictionary<string, object?> GetParameters(bool includeExtraParameters = true)
    string? GetParametersAsJson(bool includeExtraParameters = true)
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
  // A server whose secret is unset refuses to start, unless the local-development opt-out is set.
  static class MachineAuthGuard
    static bool AllowsInsecureMachineAuth()
    // Logs either the refusal or the acknowledged insecure run, so the reason a process did or did not come up is always in the log.
    // serverName: The server refusing to start, for the log line.
    // configFieldName: The config field the operator has to set.
    // secretValue: The value as configured; null or empty is the failure case.
    static bool CanStartWithSecret(string serverName, string configFieldName, string? secretValue)
    // Opt back in to accepting unauthenticated machine callers. Intended for local runs only; every use is logged.
    const string AllowInsecureVariable
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
  class ReactiveGlobalState.DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
    ctor()
    bool Equals(Dictionary<TKey, TValue>? x, Dictionary<TKey, TValue>? y)
    int GetHashCode(Dictionary<TKey, TValue> obj)
    static readonly ReactiveGlobalState.DictionaryComparer<TKey, TValue> Instance
  // Encodes and verifies the route token a fleet proxy gateway uses to decide which ikon server a session may be forwarded to. The token authorizes a target, it does not authenticate a session: the ikon server still validates the connect token and the auth ticket.
  static class RouteTokenCodec
    static string Encode(RouteToken token, byte[] key)
    // The RouteToken.ExpiresAt value for a token minted now with this lifetime.
    static uint ExpiresIn(TimeSpan lifetime)
    // The port range is checked here rather than at the dial site so that every caller gets it. The range is the same one the on-host proxy confines auth-ticket ports to: a token is signed by us, but a bug that minted port 22 should still not become an SSRF.
    // nowUtc: Supplied rather than read here so tests can drive the expiry boundary.
    static RouteTokenStatus TryDecode(string presented, byte[] key, DateTime nowUtc, int portRangeStart, int portRangeEnd, out RouteToken? token)
    static readonly TimeSpan DefaultLifetime
  enum RouteTokenStatus
    Valid
    Malformed
    BadSignature
    Expired
    // Signed, unexpired, and still unusable: no host, or a port outside the range the gateway may dial — we minted something wrong, not a caller tampering.
    InvalidTarget
  // Read-only view of the space-scoped secrets loaded from the Ikon backend. Apps receive one via app.Secrets; pipelines via host.Secrets. Manage values with ikon app secret set/list/delete. Rotating a secret while an app or pipeline is running only takes effect after a restart.
  sealed class Secrets
    // Throws InvalidOperationException when no secret with that key is set for this space; use TryGet for a non-throwing lookup.
    string this[string key] { get; }
    IReadOnlyCollection<string> Keys { get; }
    bool TryGet(string key, out string? value)
  class Sensitive<T>
    ctor(T value, SensitivityPolicy sensitivityPolicy = Default)
    bool IsSensitive { get; }
    SensitivityPolicy Policy { get; }
    T Value { get; }
  enum SensitivityPolicy
    Default
  // Turns the service token in IKON_SERVICE_TOKEN into a short-lived access token that lives in this process only; the exchange result is cached per process.
  static class ServiceTokenExchanger
    static string? GetServiceToken()
    // Null when no service token is set or the exchange failed. Cached per environment, because one token is only ever valid for the environment that minted it.
    static IkonBackend.LoginInfo? TryExchange(IkonBackend.EnvironmentType environment)
    const string ServiceTokenVariable
  // The canonical routing-identity hash behind the wire SessionHash and the gateway's cell routing. Byte-compatible with the TypeScript backend's getHashFromObject: keys sorted by UTF-16 code units, values serialized with JSON.stringify escaping, SHA256, unpadded base64url. ROUTING only — persisted-reactive storage partitioning uses its own historical hash that must never change.
  static class SessionIdentityHash
    static string Compute(IReadOnlyDictionary<string, string> sessionIdentity)
    // Hashes the identity fields plus CellTypeKey. Identity KEYS must be the cell's SessionIdentity record parameter names VERBATIM (e.g. "ChannelId", not "channelId"): the gateway keys the hashed object by the manifest field name regardless of the query-param casing the caller used.
    static string ComputeForCell(string cellType, IReadOnlyDictionary<string, string> sessionIdentity)
    const string CellTypeKey
  // The env-var gate is the security boundary: the route-enumeration function must only exist in a capture process, never in production, because Context.IsSnapshot is client-controlled and cannot gate anything.
  static class SnapshotCapture
    static bool IsCaptureProcess { get; }
    // Total capture budget across all routes; routes not captured in time are skipped.
    const int CaptureBudgetMs = 600000
    const string EnabledEnvVar
    // Counted after unioning static and dynamic routes. Variant skeletons are bounded separately (MaxVariants) and do not consume route slots.
    const int MaxRoutes = 50
    const int MaxVariants = 16
    const string ReadyFunctionName
    const int RouteTimeoutMs = 10000
    const string RoutesEnvVar
    const string RoutesFunctionName
    // Quiescence window: a route is considered settled after this many ms without a UI update. The app's ready signal (ReadyFunctionName) always wins the race when it arrives first.
    const int SettleMs = 750
  sealed class SnapshotRoutesFile
    ctor()
    List<string> Routes { get; set; }
    List<string> VariantIds { get; set; }
  // A reference to a Studio project as pasted by a user: either a bare space id or a full Studio project URL (https://host/apps/{spaceId}/...). A URL also carries the backend environment, inferred from its host; a bare id carries none.
  readonly struct StudioProjectRef
    ctor(string spaceId, IkonBackend.EnvironmentType? environment)
    // Inferred from a URL host; null when the reference was a bare id.
    IkonBackend.EnvironmentType? Environment { get; }
    string SpaceId { get; }
    static StudioProjectRef Parse(string reference)
  sealed class TelephonyNumberNotAvailableException : UserException
    ctor(string friendlyMessage, string? number = null, string? hint = null)
    // What to do about it, as a command the developer can run.
    string? Hint { get; }
    // The number that was asked for; null when the caller named none.
    string? Number { get; }
  // Rate-limits repeated calls to the same action, keyed by the action's declaring type and method name — all call sites inside one method share a throttle bucket, so pass a distinct extraKey when a method throttles more than one action. Buckets are never evicted, so extraKey must come from a bounded set, never unbounded data like a session or message id.
  static class Throttler
    // action: The action to run at most once per interval. Its declaring type and method name form the throttle key.
    // throttleInterval: Minimum time between two runs of the same action. Defaults to 1 second when null.
    // extraKey: Distinguishes several throttled actions that share a declaring method. Must be from a bounded set — keys are never evicted.
    static bool TryExecute(Action action, TimeSpan? throttleInterval = null, string? extraKey = null)
  static class TokenRenewer
    static DateTimeOffset GetTokenExpiry(string token)
    // Whether the stored refresh token is still worth presenting, as far as the store itself knows. An entry with no recorded expiry — written before refreshExpiresAt was stored — counts as live.
    static bool HasLiveRefreshToken(IkonBackend.LoginInfo? login)
    static bool IsRenewalDue(DateTimeOffset expiry, DateTimeOffset now)
    static Task<TokenRenewer.RenewalOutcome> RenewIfDueAsync(IkonBackend.EnvironmentType environment, CancellationToken cancellationToken)
    // Synchronous recovery for a caller that has just found an expired access token. Returns what happened, so the caller can tell a credential the user must replace from a rotation that merely did not happen this time.
    static TokenRenewer.RenewalOutcome TryRecoverExpiredToken(IkonBackend.EnvironmentType environment)
    // Best-effort rotation for a caller whose access token is still valid but inside its last quarter. Returns the rotated store, or null when nothing rotated; failing costs nothing — the token in hand still works.
    static IkonBackend.LoginInfo? TryRenewDueToken(IkonBackend.EnvironmentType environment)
  // What a renewal attempt did, and — when it did not rotate — whose problem that is: only ChainExpired is the user's to fix.
  enum TokenRenewer.RenewalOutcome
    NotDue
    Renewed
    NotSignedIn
    // A login with no refresh token: a --backend-token pair, or one predating the rotating flow.
    NoRefreshToken
    // The refresh chain is gone — revoked, detected as reused, or past its 90 days. Only signing in again fixes it.
    ChainExpired
    // Renewal did not happen for a reason that need not repeat: offline, throttled, or another process held the store lock.
    Unavailable
  static class Toml
    static T From<T>(string toml) where T : class, new()
    static string To<T>(T obj) where T : class
  // User-facing errors displayed cleanly without stack traces. Use for expected error conditions like invalid input, missing files, or failed operations.
  class UserException : Exception
    ctor(string message)
    ctor(string message, Exception innerException)

namespace Ikon.Common.Core.Assets
  sealed class Asset : AsyncLocalInstance<Asset>, IAsyncDisposable
    ctor()
    Task AddStorageAsync(AssetClass assetClass, IStorage storage, bool startInBackground = false)
    Task DeleteAsync(AssetUri assetUri)
    ValueTask DisposeAsync()
    Task<bool> ExistsAsync(AssetUri assetUri)
    Task<T> GetAsync<T>(AssetUri assetUri) where T : class
    Task<byte[]> GetBytesAsync(AssetUri assetUri)
    Task<AssetContent<byte[]>> GetBytesWithMetadataAsync(AssetUri assetUri)
    Task<AssetMetadata> GetMetadataAsync(AssetUri assetUri)
    Task<IAsyncDisposable> GetOrUpdateWithMetadataAsync<T>(AssetUri assetUri, Func<AssetEventArgs, AssetContent<T>?, Task> onAsset, Func<AssetEventArgs, Task>? onAssetNotFound = null) where T : class
    Task<IAsyncDisposable> GetOrUpdateWithMetadataAsync<T>(AssetUri assetUri, Action<AssetEventArgs, AssetContent<T>?> onAsset, Func<AssetEventArgs, Task>? onAssetNotFound = null) where T : class
    Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    Task<string> GetTextAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<string>> GetTextWithMetadataAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<T>> GetWithMetadataAsync<T>(AssetUri assetUri) where T : class
    Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
    // Only AssetClass.LocalFile and AssetClass.EmbeddedFile storages can be listed today; the cloud classes throw NotSupportedException. See AssetQuery for which query fields each storage honours.
    // throws NotSupportedException: The storage for AssetQuery.Class does not support listing
    Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken = default)
    // Same storage support as ListAsync: cloud classes throw NotSupportedException.
    Task<IReadOnlyList<AssetUri>> ListAsync(AssetClass assetClass, string? prefix = null, CancellationToken cancellationToken = default)
    // Same storage support as ListAsync: cloud classes throw NotSupportedException.
    Task<IReadOnlyList<AssetUri>> ListAsync(AssetUri folderUri, CancellationToken cancellationToken = default)
    Task NotifyUpdateAsync(AssetUri assetUri)
    Task SetAsync<T>(AssetUri assetUri, T asset, AssetMetadata? metadata = null, CancellationToken cancellationToken = default) where T : class
    Task SetBytesAsync(AssetUri assetUri, byte[] bytes, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
    Task SetTextAsync(AssetUri assetUri, string text, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
    Task<T?> TryGetAsync<T>(AssetUri assetUri) where T : class
    Task<byte[]?> TryGetBytesAsync(AssetUri assetUri)
    Task<AssetContent<byte[]>?> TryGetBytesWithMetadataAsync(AssetUri assetUri)
    Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    Task<string?> TryGetTextAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<string>?> TryGetTextWithMetadataAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<T>?> TryGetWithMetadataAsync<T>(AssetUri assetUri) where T : class
    Task<AssetWriteResult> TrySetBytesAsync(AssetUri assetUri, byte[] bytes, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
    Task<AssetWriteResult> TrySetTextAsync(AssetUri assetUri, string text, AssetMetadata? metadata = null, CancellationToken cancellationToken = default)
  enum AssetClass
    // Server's local filesystem under a system-managed root; not cloud-persisted.
    LocalFile
    // Baked into the app assembly as an embedded resource; read-only at runtime.
    EmbeddedFile
    // Persistent private cloud storage for any file, small or large, binary or text.
    CloudFile
    // Persistent public cloud storage; the asset is reachable via a public URL.
    CloudFilePublic
    // Persistent private cloud storage for small JSON text values.
    CloudJson
  sealed class AssetContent<T> : IDisposable
    ctor(T content, AssetMetadata? metaData = null)
    T Content { get; }
    AssetMetadata? MetaData { get; }
    void Dispose()
  class AssetEventArgs : EventArgs
    ctor(AssetUri assetUri, AssetStatus status)
    AssetUri AssetUri { get; }
    AssetStatus Status { get; }
  readonly struct AssetListingEntry
    ctor(AssetUri assetUri, AssetMetadata metadata)
    AssetUri AssetUri { get; }
    AssetMetadata Metadata { get; }
  readonly struct AssetMetadata
    ctor(string? mimeType = null, long? size = null, DateTime? lastModified = null, string? url = null, bool? urlIsTemporal = null, string[]? tags = null, string? internalPath = null, string? storageId = null, string? nativeUri = null, bool? isAppServed = null, DateTime? expiresAt = null)
    DateTime? ExpiresAt { get; }
    string? InternalPath { get; }
    bool? IsAppServed { get; }
    DateTime? LastModified { get; }
    string? MimeType { get; }
    string? NativeUri { get; }
    string? SameOriginUrl { get; init; }
    long? Size { get; }
    string? StorageId { get; }
    string[]? Tags { get; }
    string? Url { get; }
    bool? UrlIsTemporal { get; }
  // Only the AssetClass.LocalFile and AssetClass.EmbeddedFile storages list at all, and they honour different fields: EffectiveFolderPrefix always filters; Limit caps the embedded-file listing only; Tags, ContinuationToken and NextContinuationToken are reserved for the cloud storages and are ignored today, so setting them still yields the full, unfiltered listing.
  sealed class AssetQuery
    ctor(AssetClass assetClass)
    ctor(AssetUri folderUri)
    AssetClass Class { get; }
    string? ContinuationToken { get; set; }
    string? EffectiveFolderPrefix { get; }
    string? EffectiveSpaceId { get; }
    string? EffectiveUserId { get; }
    string? FolderPrefix { get; set; }
    AssetUri? FolderUri { get; set; }
    int? Limit { get; set; }
    string? NextContinuationToken { get; set; }
    string? SpaceId { get; set; }
    string[]? Tags { get; set; }
    string? UserId { get; set; }
    AssetQuery Clone()
  enum AssetStatus
    None
    Added
    Exists
    Changed
    Deleted
  sealed class AssetUpdateConflictException : Exception
    ctor(AssetUri assetUri, AssetMetadata? metadata)
    AssetUri AssetUri { get; }
    AssetMetadata? Metadata { get; }
  // Grammar: assets://[space/{spaceId}/][user/{userId}/]{class}/{path}[?query]. {class} is the kebab-case AssetClass (local-file, embedded-file, cloud-file, cloud-file-public, cloud-json) and selects the storage backend; {path} may include subdirectories and a file name. The optional space/user segments scope the asset — omit them for a global asset. Immutable; With returns a modified copy. A legacy channel/{id}/ segment is still accepted on parse and discarded (read tolerance for pre-migration URIs) — it is never emitted.
  readonly struct AssetUri : IEquatable<AssetUri>
    ctor(string uriString)
    ctor(AssetClass assetClass, string? path = null, string? spaceId = null, string? userId = null, string? query = null)
    AssetClass Class { get; }
    string FileName { get; }
    string Path { get; }
    string? Query { get; }
    static string Scheme { get; }
    string? SpaceId { get; }
    string? UserId { get; }
    static AssetUri FromFilesystemPath(string relativePathToRoot, AssetClass defaultAssetClass = LocalFile)
    static bool IsValid(string uriString)
    static string ToFilesystemPath(AssetUri assetUri)
    static bool TryParse(string uriString, out AssetUri assetUri, out string? failureReason)
    static bool TryParse(string uriString, out AssetUri assetUri)
    AssetUri With(AssetClass? assetClass = null, string? path = null, string? spaceId = null, string? userId = null, string? query = null)
    static bool operator ==(AssetUri left, AssetUri right)
    static bool operator !=(AssetUri left, AssetUri right)
  // Serializes AssetUri as its canonical URI string so it round-trips correctly. Without this, System.Text.Json cannot reconstruct the immutable get-only struct and falls back to default(AssetUri) on deserialization.
  sealed class AssetUriJsonConverter : JsonConverter<AssetUri>
    ctor()
    override AssetUri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    override void Write(Utf8JsonWriter writer, AssetUri value, JsonSerializerOptions options)
  readonly struct AssetWriteResult
    ctor(AssetWriteStatus status, AssetMetadata? metadata = null)
    bool IsConflict { get; }
    AssetMetadata? Metadata { get; }
    AssetWriteStatus Status { get; }
    bool Succeeded { get; }
  enum AssetWriteStatus
    NotFound
    Conflict
    Skipped
    Success
  interface IHashableStream
    void SetSha256Hash(string? hash)
  interface IStorage : IAsyncDisposable
    Task DeleteAsync(AssetUri assetUri)
    Task<bool> ExistsAsync(AssetUri assetUri)
    Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata, CancellationToken cancellationToken)
    Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken)
    Task StartAsync()
    Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    Task WaitUntilQueueEmptyAsync()
    event Func<AssetEventArgs, Task> AssetEventAsync
  static class StorageExtensions
    static Task AddEmbeddedFileStorageAsync(this Asset asset, Assembly? assembly = null, string resourceNamespace = "")

namespace Ikon.Common.Core.Auth
  sealed record StepUpAssertion
    ctor(string Issuer, string Audience, string Subject, long IssuedAt, long ExpiresAt, string Jti, string UserId, string ChallengeId, string Purpose, string? SpaceId, string IdentityScheme, string? AssuranceLevel, string EidSubjectHash, string? IdentifierHash, string? VerifiedName, string? Birthdate, long VerifiedAt, string IdTokenHash, IReadOnlyDictionary<string, object?> RawClaims)
    string? AssuranceLevel { get; init; }
    string Audience { get; init; }
    string? Birthdate { get; init; }
    string ChallengeId { get; init; }
    string EidSubjectHash { get; init; }
    long ExpiresAt { get; init; }
    string IdTokenHash { get; init; }
    string? IdentifierHash { get; init; }
    string IdentityScheme { get; init; }
    long IssuedAt { get; init; }
    string Issuer { get; init; }
    string Jti { get; init; }
    string Purpose { get; init; }
    IReadOnlyDictionary<string, object?> RawClaims { get; init; }
    string? SpaceId { get; init; }
    string Subject { get; init; }
    string UserId { get; init; }
    long VerifiedAt { get; init; }
    string? VerifiedName { get; init; }

namespace Ikon.Common.Core.Email
  sealed record EmailAddress
    ctor(string Email, string? Name, string? Subaddress)
    string Email { get; init; }
    string? Name { get; init; }
    string? Subaddress { get; init; }
  // Bytes is the raw binary content; the platform encodes it as base64 on the wire.
  sealed record EmailAttachment
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  // The caller owns the Content stream; dispose this object (e.g. await using) to release it.
  sealed class EmailAttachmentDownload : IAsyncDisposable
    Stream Content { get; }
    // Sender-supplied, sanitized by the platform.
    string Filename { get; }
    string MimeType { get; }
    long Size { get; }
    ValueTask DisposeAsync()
  sealed record EmailHeader
    ctor(string Name, string Value)
    string Name { get; init; }
    string Value { get; init; }
  // The platform enqueues the send and returns once accepted; transient delivery failures are retried server-side.
  sealed record EmailSendRequest
    // Attachments: Up to 10 per email.
    // Metadata: Forwarded to the mail provider for tracking.
    // SenderLocalPart: The From-address part before the @: lowercase letters, digits, dot, underscore, hyphen; alphanumeric at both ends; max 64 chars; mail-infrastructure names (postmaster, abuse, …) rejected. Needs a verified sending domain, else the send fails with EmailSenderNotAvailableException.
    // SenderDisplayName: Defaults to the space's name. Max 64 characters; header-unsafe characters rejected. Needs a verified sending domain.
    // SenderDomain: Must be one of the space's verified sending domains, else EmailSenderNotAvailableException. Null lets the platform pick the designated or best verified domain.
    ctor(string To, string Subject, string HtmlBody, string? TextBody = null, string? ReplyTo = null, IReadOnlyList<EmailAttachment>? Attachments = null, IReadOnlyDictionary<string, string>? Metadata = null, string? SenderLocalPart = null, string? SenderDisplayName = null, string? SenderDomain = null)
    IReadOnlyList<EmailAttachment>? Attachments { get; init; }
    string HtmlBody { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? ReplyTo { get; init; }
    string? SenderDisplayName { get; init; }
    string? SenderDomain { get; init; }
    string? SenderLocalPart { get; init; }
    string Subject { get; init; }
    string? TextBody { get; init; }
    string To { get; init; }
  // Checking against these before sending turns a rejection from the platform into an immediate, local error.
  static class EmailSenderIdentity
    static bool IsReservedLocalPart(string localPart)
    static bool IsValidLocalPart(string localPart)
    // Trims and lowercases the way the backend does before validating; returns null when nothing remains.
    static string? NormalizeLocalPart(string? localPart)
    const int MaxDisplayNameCodePoints = 64
    const int MaxLocalPartLength = 64
  // Metadata only — no body bytes; fetch the body via the email service's DownloadAttachmentAsync.
  sealed record InboundAttachmentInfo
    ctor(string Id, string Filename, string MimeType, long Size)
    string Filename { get; init; }
    string Id { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
  // Attachments expose metadata only; fetch each one via the email service's DownloadAttachmentAsync.
  sealed record InboundEmailDetail
    ctor(string Id, string Recipient, string From, string Subject, string? BodyText, string? BodyHtml, IReadOnlyList<EmailAddress> To, IReadOnlyList<EmailAddress> Cc, string? ReplyTo, IReadOnlyList<EmailHeader> Headers, IReadOnlyList<InboundAttachmentInfo> Attachments, DateTimeOffset ReceivedAt, double? SpamScore, string? Tag)
    IReadOnlyList<InboundAttachmentInfo> Attachments { get; init; }
    string? BodyHtml { get; init; }
    string? BodyText { get; init; }
    IReadOnlyList<EmailAddress> Cc { get; init; }
    string From { get; init; }
    IReadOnlyList<EmailHeader> Headers { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Recipient { get; init; }
    string? ReplyTo { get; init; }
    double? SpamScore { get; init; }
    string Subject { get; init; }
    string? Tag { get; init; }
    IReadOnlyList<EmailAddress> To { get; init; }
  // Subject is decrypted server-side; body and attachment bytes are not included here — call EmailService.GetMessageAsync for the full message.
  sealed record InboundEmailSummary
    ctor(string Id, string Recipient, string From, string Subject, DateTimeOffset ReceivedAt, int AttachmentCount, double? SpamScore, string? Tag)
    int AttachmentCount { get; init; }
    string From { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Recipient { get; init; }
    double? SpamScore { get; init; }
    string Subject { get; init; }
    string? Tag { get; init; }
  // NextCursor is null when there are no more pages.
  sealed record InboxPage
    ctor(IReadOnlyList<InboundEmailSummary> Items, string? NextCursor)
    IReadOnlyList<InboundEmailSummary> Items { get; init; }
    string? NextCursor { get; init; }
  sealed record InboxQuery
    ctor()
    // Opaque cursor from a previous InboxPage.NextCursor; null requests the first page.
    string? Cursor { get; init; }
    // Case-insensitive.
    string? From { get; init; }
    // The platform clamps to [1, 100]; values outside that range are silently adjusted. Defaults to 25.
    int Limit { get; init; }
    // Case-insensitive.
    string? Recipient { get; init; }
    // Inclusive lower bound on the SMTP receive timestamp.
    DateTimeOffset? Since { get; init; }
    // Inclusive upper bound on the SMTP receive timestamp.
    DateTimeOffset? Until { get; init; }

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

namespace Ikon.Common.Core.Functions.Policy
  // Use this on framework-shipped or genuinely public endpoints where capability is provided by something other than session auth (e.g. a stableId, a webhook signature, or the endpoint being read-only public). Pair with explicit [RateLimit] when abuse is a concern.
  sealed class AllowAnonymousAttribute : Attribute
    ctor()
  sealed class ApprovalAuditEntry
    ctor(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, bool approved, string? reason, string policyName, DateTimeOffset timestamp)
    Guid ApprovalId { get; }
    bool Approved { get; }
    int ApproverSessionId { get; }
    string? ApproverUserId { get; }
    Guid CallId { get; }
    string FunctionName { get; }
    string PolicyName { get; }
    string? Reason { get; }
    DateTimeOffset Timestamp { get; }
    static ApprovalAuditEntry CreateApproved(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, string policyName)
    static ApprovalAuditEntry CreateRejected(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, string? reason, string policyName)
  sealed class ApprovalContext
    Guid ApprovalId { get; }
    // The raw token is only provided to the designated approver via protocol.
    string ApprovalTokenHash { get; }
    object?[] Args { get; }
    string ArgsHash { get; }
    PolicyCallContext CallContext { get; }
    int CallerSessionId { get; }
    DateTimeOffset ExpiresAt { get; }
    string FunctionName { get; }
    string Reason { get; }
    // Always at least PolicyDecision.MinExpirySeconds (30 seconds).
    int TimeoutSeconds { get; }
    // The raw token must only be sent to the designated approver.
    // functionName: The name of the function requiring approval.
    // reason: The reason why approval is required.
    // args: The arguments being passed to the function.
    // callContext: The original policy call context.
    // timeoutSeconds: The timeout in seconds (minimum 30).
    static (ApprovalContext Context, Guid RawToken) Create(string functionName, string reason, object?[] args, PolicyCallContext callContext, int timeoutSeconds = 300)
    bool IsExpired()
    // Uses constant-time comparison of hashes to prevent timing attacks.
    // providedToken: The token GUID provided by the approver.
    bool ValidateToken(Guid providedToken)
    // providedToken: The token string provided by the approver.
    bool ValidateToken(string providedToken)
  delegate ApprovalHandlerDelegate
    Task<ApprovalResult> ApprovalHandlerDelegate(ApprovalContext context)
  readonly struct ApprovalResult
    bool IsApproved { get; }
    string? RejectionReason { get; }
    static ApprovalResult Approved()
    // reason: The reason for rejection.
    static ApprovalResult Rejected(string? reason = null)
    override string ToString()
  enum ApproverType
    Caller
    SpecificClient
    SpecificUser
  interface IFunctionPolicy
    string Name { get; }
    // Lower values are evaluated first; the default is 100.
    virtual int Priority { get; }
    // args: The arguments being passed to the function.
    // context: The policy call context with metadata about the call.
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
  interface IUsageLimitChecker
    // context: The policy call context.
    // args: The function arguments.
    ValueTask<UsageLimitCheckResult> CheckAsync(PolicyCallContext context, object?[] args)
  // Checks PolicyCallContext.IsAnonymous — guests carry a valid UserId (device-scoped) but are marked anonymous by the backend. Callers whose anonymity cannot be resolved (no client context) are denied too. AuthSessionId is deliberately not consulted: it is a per-login correlation id that cloud guests also have. Returns PolicyDecision.Denied with error code "login_required", which the Ikon client runtime catches to drive the deferred-login flow.
  sealed class LoggedInPolicy : IFunctionPolicy
    ctor()
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    const string LoginRequiredCode
  sealed class PerSessionRateLimitPolicy : IFunctionPolicy
    // limit: Maximum number of calls allowed per session in the window.
    // windowSeconds: The time window in seconds.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(int limit, int windowSeconds, string? name = null, int priority = 50)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    PolicyDelegate ToDelegate()
  static class PolicyArgs
    // args: The arguments array.
    // requiredIndices: The indices that must have non-null values.
    static bool HasAll(object?[] args, params int[] requiredIndices)
    // args: The arguments array.
    // index: The zero-based index of the argument.
    // defaultValue: The default value to return if the argument is missing or null.
    static T? Optional<T>(object?[] args, int index, T? defaultValue = default)
    // args: The arguments array.
    // index: The zero-based index of the argument.
    // throws PolicyDeniedException: Thrown if the argument is missing, null, or wrong type.
    static T Required<T>(object?[] args, int index)
    // args: The arguments array.
    // index: The zero-based index of the argument.
    // value: The output value if successful.
    static bool TryGet<T>(object?[] args, int index, out T? value)
  abstract class PolicyAttribute : Attribute
    // Lower values are evaluated first.
    int Priority { get; set; }
    abstract IFunctionPolicy CreatePolicy()
  sealed class PolicyAttribute<TPolicy> : PolicyAttribute where TPolicy : IFunctionPolicy, new()
    ctor()
    override IFunctionPolicy CreatePolicy()
  sealed class PolicyCallContext
    ctor(Guid callId, string functionName, int callerSessionId, string? userId, string? tenantId, Guid? instanceId, bool isInternal, CancellationToken cancellationToken, string? authSessionId = null, bool? isAnonymous = null, DateTime? callTimestamp = null, IReadOnlyDictionary<string, object?>? additionalContext = null)
    IReadOnlyDictionary<string, object?>? AdditionalContext { get; }
    // A per-login correlation identifier, not an authentication flag — use IsAnonymous for guest detection.
    string? AuthSessionId { get; }
    Guid CallId { get; }
    DateTime CallTimestamp { get; }
    int CallerSessionId { get; }
    CancellationToken CancellationToken { get; }
    string FunctionName { get; }
    Guid? InstanceId { get; }
    // true for a guest, false for an authenticated user or machine, null when unknown (no resolvable client context for the caller session).
    bool? IsAnonymous { get; }
    bool IsInternal { get; }
    string? TenantId { get; }
    string? UserId { get; }
  static class PolicyChain
    // Requires all provided policies to allow. Policies are evaluated in priority order (lower first); evaluation stops at the first non-Allow decision.
    // policies: The policies to chain together.
    static IFunctionPolicy All(params IFunctionPolicy[] policies)
    // policies: The policies to chain together.
    static PolicyDelegate AllAsDelegate(params IFunctionPolicy[] policies)
  // A discriminated union with three states: Allow, Deny, or NeedsApproval — pattern match on the subtypes.
  abstract class PolicyDecision
    static PolicyDecision Allowed()
    // reason: The reason for denying the function call.
    // code: Optional error code for programmatic handling.
    static PolicyDecision Denied(string reason, string? code = null)
    // message: The message explaining why approval is required.
    static PolicyDecision RequireApproval(string message)
    // message: The message explaining why approval is required.
    // expirySeconds: How long the approval request is valid (minimum 30 seconds).
    static PolicyDecision RequireApproval(string message, int expirySeconds)
    // message: The message explaining why approval is required.
    // handler: The custom handler to process the approval request.
    static PolicyDecision RequireApproval(string message, ApprovalHandlerDelegate handler)
    // message: The message explaining why approval is required.
    // expirySeconds: How long the approval request is valid (minimum 30 seconds).
    // handler: The custom handler to process the approval request.
    static PolicyDecision RequireApproval(string message, int expirySeconds, ApprovalHandlerDelegate handler)
    const int DefaultExpirySeconds = 300
    const int MinExpirySeconds = 30
  sealed class PolicyDecision.Allow : PolicyDecision
  sealed class PolicyDecision.Deny : PolicyDecision
    string? Code { get; }
    string Reason { get; }
  sealed class PolicyDecision.NeedsApproval : PolicyDecision
    int ExpirySeconds { get; }
    ApprovalHandlerDelegate? Handler { get; }
    string Message { get; }
  delegate PolicyDelegate
    ValueTask<PolicyDecision> PolicyDelegate(object?[] args, PolicyCallContext context)
  sealed class PolicyDeniedException : Exception
    // reason: The reason for denying the call.
    ctor(string? reason)
    // reason: The reason for denying the call.
    // code: Error code for programmatic handling (e.g., "rate_limit_exceeded", "bad_args").
    ctor(string? reason, string? code)
    // reason: The reason for denying the call.
    // code: Optional error code for programmatic handling.
    // policyName: The name of the policy that denied the call.
    // functionName: The name of the function that was denied.
    ctor(string? reason, string? code, string? policyName, string? functionName)
    ctor(string? reason, Exception innerException, string? policyName = null, string? functionName = null)
    ctor(string? reason, string? code, Exception innerException, string? policyName = null, string? functionName = null)
    string? Code { get; }
    string? FunctionName { get; }
    string? PolicyName { get; }
  sealed class PolicyEvaluationResult
    ctor(PolicyDecision decision, string functionName, Guid callId, string? decidingPolicyName, TimeSpan evaluationDuration)
    Guid CallId { get; }
    // Null if the decision is Allow.
    string? DecidingPolicyName { get; }
    PolicyDecision Decision { get; }
    TimeSpan EvaluationDuration { get; }
    string FunctionName { get; }
    bool IsAllowed { get; }
    bool IsDenied { get; }
    bool RequiresApproval { get; }
    static PolicyEvaluationResult Allowed(string functionName, Guid callId)
    static PolicyEvaluationResult Denied(string functionName, Guid callId, string? reason, string policyName, TimeSpan evaluationDuration)
    static PolicyEvaluationResult Denied(string functionName, Guid callId, string reason, string? code, string policyName, TimeSpan evaluationDuration)
    static PolicyEvaluationResult NeedsApproval(PolicyDecision decision, string functionName, Guid callId, string policyName, TimeSpan evaluationDuration)
    override string ToString()
  sealed class PolicyTypeAttribute : PolicyAttribute
    // policyType: The type of policy to create. Must implement IFunctionPolicy and have a parameterless constructor.
    ctor(Type policyType)
    Type PolicyType { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class RateLimitAttribute : PolicyAttribute
    // limit: Maximum number of calls allowed in the window.
    // windowSeconds: The time window in seconds.
    ctor(int limit, int windowSeconds)
    int Limit { get; }
    // If true, the rate limit is per-session; if false (the default), it is global.
    bool PerSession { get; set; }
    int WindowSeconds { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class RateLimitPolicy : IFunctionPolicy
    // limit: Maximum number of calls allowed in the window.
    // windowSeconds: The time window in seconds.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(int limit, int windowSeconds, string? name = null, int priority = 50)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    PolicyDelegate ToDelegate()
  sealed class RequireApprovalAttribute : PolicyAttribute
    ctor()
    ApproverType ApproverType { get; set; }
    // Only used when ApproverType is SpecificClient.
    int ClientSessionId { get; set; }
    string Reason { get; set; }
    // Only used when ApproverType is SpecificUser.
    string? UserId { get; set; }
    override IFunctionPolicy CreatePolicy()
  sealed class RequireApprovalPolicy : IFunctionPolicy
    // reason: The reason why approval is required.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(string reason, string? name = null, int priority = 100)
    // reason: The reason why approval is required.
    // handler: The custom approval handler.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(string reason, ApprovalHandlerDelegate handler, string? name = null, int priority = 100)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // reason: The reason why approval is required.
    // clientSessionId: The client session ID to ask for approval.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    static RequireApprovalPolicy ForClient(string reason, int clientSessionId, string? name = null, int priority = 100)
    // reason: The reason why approval is required.
    // userId: The user ID to ask for approval.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    static RequireApprovalPolicy ForUser(string reason, string userId, string? name = null, int priority = 100)
    PolicyDelegate ToDelegate()
  // Guest (anonymous) callers are denied with the "login_required" error code. The Ikon client runtime intercepts this and triggers the deferred-login flow.
  sealed class RequireLoginAttribute : PolicyAttribute
    ctor()
    override IFunctionPolicy CreatePolicy()
  // Internal callers (PolicyCallContext.IsInternal) bypass the check — same as LoggedInPolicy — because in-process callers are already trusted.
  sealed class RequireRoleAttribute : PolicyAttribute
    ctor(params string[] roles)
    bool RequireAll { get; set; }
    string[] RequiredRoles { get; }
    override IFunctionPolicy CreatePolicy()
  // Denies with code role_required (MissingRoleCode) when the caller lacks the role(s); roles are read from the user_roles (RolesContextKey) context key. Internal callers bypass the check.
  sealed class RoleBasedPolicy : IFunctionPolicy
    ctor(string[] required, bool requireAll, int priority)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    const string MissingRoleCode
    const string RolesContextKey
  sealed class UsageLimitAttribute : PolicyAttribute
    // checkerType: The type of checker to use. Must implement IUsageLimitChecker.
    ctor(Type checkerType)
    Type CheckerType { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class UsageLimitCheckResult
    bool Allowed { get; }
    string? DenyCode { get; }
    string? DenyReason { get; }
    static UsageLimitCheckResult Allow()
    // reason: The reason for denial.
    // code: The error code (defaults to "usage_limit_exceeded").
    static UsageLimitCheckResult Deny(string reason, string? code = "usage_limit_exceeded")
  sealed class UsageLimitPolicy : IFunctionPolicy
    // checker: The checker to use for evaluating usage limits.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(IUsageLimitChecker checker, string? name = null, int priority = 10)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    PolicyDelegate ToDelegate()

namespace Ikon.Common.Core.Protocol
  sealed class ActionFunctionRegister : IProtocolMessagePayload
    ctor()
    ctor(Guid functionId, string functionName, List<ActionFunctionRegister.FunctionRegisterParameter> parameters, string resultTypeName, bool isEnumerable, string enumerableItemTypeName, bool isCancellable, string description, bool llmInlineResult, bool llmCallOnlyOnce, bool requiresInstance, List<string> versions)
    string Description { get; set; }
    string EnumerableItemTypeName { get; set; }
    Guid FunctionId { get; set; }
    string FunctionName { get; set; }
    bool IsCancellable { get; set; }
    bool IsEnumerable { get; set; }
    bool LlmCallOnlyOnce { get; set; }
    bool LlmInlineResult { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    List<ActionFunctionRegister.FunctionRegisterParameter> Parameters { get; set; }
    bool RequiresInstance { get; set; }
    string ResultTypeName { get; set; }
    List<string> Versions { get; set; }
    static ActionFunctionRegister.FunctionRegisterParameter CreateParameter(int parameterIndex, string parameterName, Type clrType, bool hasDefaultValue, object? defaultValue, bool isEnumerable, string enumerableItemTypeName, string description)
  sealed class ActionFunctionRegister.FunctionRegisterParameter
    ctor()
    ctor(int parameterIndex, string parameterName, string typeName, bool hasDefaultValue, string defaultValueJson, byte[] defaultValueData, bool isEnumerable, string enumerableItemTypeName, string description)
    byte[] DefaultValueData { get; set; }
    string DefaultValueJson { get; set; }
    string Description { get; set; }
    string EnumerableItemTypeName { get; set; }
    bool HasDefaultValue { get; set; }
    bool IsEnumerable { get; set; }
    int ParameterIndex { get; set; }
    string ParameterName { get; set; }
    string TypeName { get; set; }
  enum AudioCodec
    Unknown
    Opus
    Mp3
    RawPcm16
  enum AudioPlaybackState
    Unknown
    Playing
    Blocked
    Hidden
  sealed class AudioStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string streamId, string description, string sourceType, AudioCodec codec, string codecDetails, int sampleRate, int channels, List<AudioStreamBegin.AudioShapeSet>? shapeSets, string? correlationId)
    int Channels { get; set; }
    AudioCodec Codec { get; set; }
    string CodecDetails { get; set; }
    string? CorrelationId { get; set; }
    string Description { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SampleRate { get; set; }
    List<AudioStreamBegin.AudioShapeSet>? ShapeSets { get; set; }
    string SourceType { get; set; }
    string StreamId { get; set; }
  sealed class AudioStreamBegin.AudioShapeSet
    ctor()
    ctor(uint setId, string name, List<string> shapeNames)
    string Name { get; set; }
    uint SetId { get; set; }
    List<string> ShapeNames { get; set; }
  sealed class AuthResponse : IProtocolMessagePayload
    ctor()
    ctor(Context clientContext, Context serverContext, string certHash, List<Entrypoint> entrypoints, Dictionary<string, bool> featureFlags, string spaceId, string appSessionId, string ikonServerId, string primaryUserId, int keepaliveTimeoutMs, int serverCapability)
    // The app session this server was provisioned for — the session's business identity, stable across the servers that run it.
    string AppSessionId { get; set; }
    string CertHash { get; set; }
    Context ClientContext { get; set; }
    List<Entrypoint> Entrypoints { get; set; }
    Dictionary<string, bool> FeatureFlags { get; set; }
    // The provisioned server instance serving this connection.
    string IkonServerId { get; set; }
    int KeepaliveTimeoutMs { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PrimaryUserId { get; set; }
    int ServerCapability { get; set; }
    Context ServerContext { get; set; }
    string SpaceId { get; set; }
    void CopyRetiredFieldsFrom(AuthResponse source)
    AuthResponse.RetiredFields GetOrCreateRetiredFields()
    AuthResponse.RetiredFields? GetRetiredFields()
    static readonly IReadOnlyList<string> RetiredKeys
  sealed class AuthResponse.RetiredFields
    ctor()
    // Renamed to IkonServerId. Teleport matches fields by name hash, so the server keeps writing this via the retired bag with the IkonServerId value until no frozen app bundle or cached SDK build in circulation still reads it.
    string? ServerSessionId { get; set; }
  sealed class ClientEnvironment : IProtocolMessagePayload
    ctor()
    ctor(string description, string deviceId, string productId, string versionId, string installId, string locale, string userAgent, ClientType clientType, SdkType sdkType, int sdkCapability, int protocolVersion, PayloadType payloadType, StyleFormat styleFormat, bool supportsCompression, bool hasInput, bool receiveAllMessages, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, string initialUrl, bool isSnapshot, string snapshotVariant)
    ClientType ClientType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    // Absolute URL the browser loaded, ikon-* query params stripped; copied into Context.InitialUrl. Empty for every non-browser client.
    string InitialUrl { get; set; }
    string InstallId { get; set; }
    // True for the build-time snapshot-capture client; copied into Context.IsSnapshot. Identifies the client whose initial UI is baked into boot-snapshot.json. Inert beyond identification in v1.
    bool IsSnapshot { get; set; }
    bool IsTouchDevice { get; set; }
    string Locale { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    PayloadType PayloadType { get; set; }
    string ProductId { get; set; }
    int ProtocolVersion { get; set; }
    bool ReceiveAllMessages { get; set; }
    // Opaque, monotonically-increasing capability level advertised by the connecting SDK (companion to SdkType). 0 = legacy/unknown. A client can only misreport this about itself: claiming a level it does not have makes the server send messages it cannot handle.
    int SdkCapability { get; set; }
    SdkType SdkType { get; set; }
    // Boot-snapshot variant id the capture client asks the app to render (a skeleton keyed by the [BootSnapshot] seed rules); empty for route captures and all live clients. Copied into Context.SnapshotVariant. Client-controlled like IsSnapshot — must never gate anything security-relevant.
    string SnapshotVariant { get; set; }
    StyleFormat StyleFormat { get; set; }
    bool SupportsCompression { get; set; }
    string Theme { get; set; }
    string Timezone { get; set; }
    string UserAgent { get; set; }
    string VersionId { get; set; }
    int ViewportHeight { get; set; }
    int ViewportWidth { get; set; }
  sealed class ClientInitialization : IProtocolMessagePayload
    ctor()
    ctor(Dictionary<int, List<ActionFunctionRegister>> functions)
    Dictionary<int, List<ActionFunctionRegister>> Functions { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
  enum ClientType
    Unknown
    MobileWeb
    MobileApp
    DesktopWeb
    DesktopApp
  sealed class ConnectToken : IProtocolMessagePayload
    ctor()
    ctor(uint expiresAt, ContextType contextType, UserType userType, bool isInternal, string userId, string authSessionId, bool isAnonymous, bool isGlobal, Opcode opcodeGroupsFromServer, Opcode opcodeGroupsToServer, Dictionary<string, string> parameters, string serverSessionId, string description, string deviceId, string productId, string versionId, string installId, string locale, string userAgent, ClientType clientType, SdkType sdkType, int sdkCapability, int protocolVersion, PayloadType payloadType, StyleFormat styleFormat, bool supportsCompression, bool hasInput, bool receiveAllMessages, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, string initialUrl, bool isSnapshot, string snapshotVariant)
    string AuthSessionId { get; set; }
    ClientType ClientType { get; set; }
    ContextType ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    // Epoch seconds after which the server refuses this token. 0 means no timed expiry, which is only appropriate when the signing process also owns the verifying server and randomizes the secret at startup — the token then dies with the process. Every minted token that leaves the machine sets it.
    uint ExpiresAt { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    // Absolute URL the browser loaded, ikon-* query params stripped; copied into Context.InitialUrl. Empty for every non-browser client. Client-supplied like InitialPath — never authoritative.
    string InitialUrl { get; set; }
    string InstallId { get; set; }
    // True when the user is anonymous (guest login or no login): a device-scoped identity rather than a real account. Filled by the backend from the user's role; copied into Context.IsAnonymous. AuthSessionId cannot express this — cloud logins (including anonymous ones) always carry a session id, so "has a session" does not mean "has an account".
    bool IsAnonymous { get; set; }
    // True when the anonymous user is the space's GLOBAL communal identity (the "global" login method) — every global visitor shares one UserId. Always false for device-scoped guests and signed-in users; implies IsAnonymous. Filled by the backend from the minted flavor; copied into Context.IsGlobal.
    bool IsGlobal { get; set; }
    bool IsInternal { get; set; }
    // True for the build-time snapshot-capture client; copied into Context.IsSnapshot. Identifies the client whose initial UI is baked into boot-snapshot.json. Inert beyond identification in v1.
    bool IsSnapshot { get; set; }
    bool IsTouchDevice { get; set; }
    string Locale { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Opcode OpcodeGroupsFromServer { get; set; }
    Opcode OpcodeGroupsToServer { get; set; }
    // Launch parameters. Signed, and NOT part of the removable block below, because Studio mints the preview inspector by putting `ikon-inspect` here — a client that could assert its own parameters would hand itself the inspector.
    Dictionary<string, string> Parameters { get; set; }
    PayloadType PayloadType { get; set; }
    string ProductId { get; set; }
    int ProtocolVersion { get; set; }
    bool ReceiveAllMessages { get; set; }
    // Opaque, monotonically-increasing capability level advertised by the connecting SDK (companion to SdkType). 0 = legacy/unknown. Threaded SDK connect-request -> backend -> ConnectToken -> ikon server -> client Context.
    int SdkCapability { get; set; }
    SdkType SdkType { get; set; }
    string ServerSessionId { get; set; }
    // Boot-snapshot variant id the capture client asks the app to render (a skeleton keyed by the [BootSnapshot] seed rules); empty for route captures and all live clients. Copied into Context.SnapshotVariant. Client-controlled like IsSnapshot — must never gate anything security-relevant.
    string SnapshotVariant { get; set; }
    StyleFormat StyleFormat { get; set; }
    bool SupportsCompression { get; set; }
    string Theme { get; set; }
    string Timezone { get; set; }
    string UserAgent { get; set; }
    string UserId { get; set; }
    UserType UserType { get; set; }
    string VersionId { get; set; }
    int ViewportHeight { get; set; }
    int ViewportWidth { get; set; }
    override string ToString()
  sealed class Context : IProtocolMessagePayload
    ctor()
    ctor(ContextType contextType, UserType userType, PayloadType payloadType, string description, string userId, string deviceId, string productId, string versionId, string installId, string locale, int sessionId, bool isInternal, bool isSnapshot, string snapshotVariant, bool isReady, bool hasInput, string authSessionId, bool isAnonymous, bool isGlobal, bool receiveAllMessages, ulong preciseJoinedAt, string userAgent, ClientType clientType, string uniqueSessionId, Dictionary<string, string> parameters, SdkType sdkType, int sdkCapability, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, string initialUrl, StyleFormat styleFormat, bool supportsCompression, bool isSoftDisconnected, ulong softDisconnectAt)
    string AuthSessionId { get; set; }
    // Alias for SessionId.
    int ClientSessionId { get; }
    ClientType ClientType { get; set; }
    ContextType ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    // Copied from ConnectToken.InitialUrl — the absolute URL the browser loaded, so an app can see the host it was reached on (a custom customer domain) and not just the path. A strict superset of InitialPath: same SDK-internal ikon-* query params stripped, everything else kept, so InitialUrl.PathAndQuery reproduces InitialPath. Empty for every non-browser client, so read it as an addition to InitialPath rather than a replacement. Client-controlled like InitialPath — treat it as a hint and re-authorize server-side; it must never gate anything security-relevant on its own.
    string InitialUrl { get; set; }
    string InstallId { get; set; }
    // Copied from ConnectToken.IsAnonymous — true when the user is anonymous (guest login or no login): a device-scoped identity rather than a real account. The authoritative signal for guest detection; AuthSessionId is a login-session identifier, not an authentication flag.
    bool IsAnonymous { get; set; }
    // Copied from ConnectToken.IsGlobal — true when this anonymous user is the space's GLOBAL communal identity (the "global" login method), where every global visitor shares one UserId. Always false for device-scoped guests and signed-in users; implies IsAnonymous. Lets an app offer "continue as guest" or per-visitor features only where they make sense.
    bool IsGlobal { get; set; }
    bool IsInternal { get; set; }
    bool IsReady { get; set; }
    // Copied from ConnectToken.IsSnapshot — marks the build-time snapshot-capture client.
    bool IsSnapshot { get; set; }
    bool IsSoftDisconnected { get; set; }
    bool IsTouchDevice { get; set; }
    string Locale { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Dictionary<string, string> Parameters { get; set; }
    PayloadType PayloadType { get; set; }
    ulong PreciseJoinedAt { get; set; }
    string ProductId { get; set; }
    bool ReceiveAllMessages { get; set; }
    // Opaque, monotonically-increasing capability level advertised by the connecting SDK (companion to SdkType). 0 = legacy/unknown. Copied from ConnectToken.SdkCapability when the server builds the client Context.
    int SdkCapability { get; set; }
    SdkType SdkType { get; set; }
    int SessionId { get; set; }
    // Copied from ConnectToken.SnapshotVariant — the boot-snapshot variant id the capture client asks the app to render; empty for route captures and all live clients. Client-controlled like IsSnapshot — must never gate anything security-relevant.
    string SnapshotVariant { get; set; }
    ulong SoftDisconnectAt { get; set; }
    StyleFormat StyleFormat { get; set; }
    bool SupportsCompression { get; set; }
    string Theme { get; set; }
    string Timezone { get; set; }
    string UniqueSessionId { get; set; }
    string UserAgent { get; set; }
    string UserId { get; set; }
    UserType UserType { get; set; }
    string VersionId { get; set; }
    int ViewportHeight { get; set; }
    int ViewportWidth { get; set; }
    override string ToString()
  enum ContextType
    Unknown
    Backend
    Server
    Plugin
    Browser
    Native
  sealed class Entrypoint : IProtocolMessagePayload
    ctor()
    ctor(EntrypointType type, string uri, Opcode opcodeGroupsFromServer, Opcode opcodeGroupsToServer, int priority, string description, byte[] authTicket, bool isUnreliable)
    byte[] AuthTicket { get; set; }
    string Description { get; set; }
    bool IsUnreliable { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Opcode OpcodeGroupsFromServer { get; set; }
    Opcode OpcodeGroupsToServer { get; set; }
    int Priority { get; set; }
    EntrypointType Type { get; set; }
    string Uri { get; set; }
    override string ToString()
  enum EntrypointType
    None
    WebSocket
    WebSocketProxy
    WebTransport
    WebTransportProxy
    Tcp
    TcpProxy
    Https
    WebRTC
    TcpTls
    Udp
    UdpDtls
    HttpStream
    HttpStreamProxy
  sealed class FunctionParameter : IProtocolMessagePayload
    ctor()
    ctor(int parameterIndex, string typeName, string valueJson, byte[] valueData, bool isEnumerable, string enumerableItemTypeName, Guid enumerationId, byte[] valueTeleport)
    string EnumerableItemTypeName { get; set; }
    Guid EnumerationId { get; set; }
    bool IsEnumerable { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int ParameterIndex { get; set; }
    string TypeName { get; set; }
    byte[] ValueData { get; set; }
    string ValueJson { get; set; }
    byte[] ValueTeleport { get; set; }
  // Shared state synchronized across all clients and the server, providing access to connected clients, registered functions, active media streams, and session metadata
  sealed class GlobalState : ILogInfo, IProtocolMessagePayload
    ctor()
    ctor(Dictionary<int, Context> clients, Dictionary<int, List<ActionFunctionRegister>> functions, Dictionary<string, GlobalState.UIStreamState> uiStreams, Dictionary<string, GlobalState.AudioStreamState> audioStreams, Dictionary<string, GlobalState.VideoStreamState> videoStreams, string spaceId, string ikonServerId, string appSessionId, string sessionIdentityHash, string spaceUrl, string sessionUrl, string firstUserId, string primaryUserId, string organisationName, string spaceName, ServerRunType serverRunType, bool publicAccess, bool debugMode)
    // Unique identifier of the app session this server is serving — the session's business identity, stable across the servers that run it. Empty outside a cloud run.
    string AppSessionId { get; set; }
    // Active audio streams indexed by stream ID
    Dictionary<string, GlobalState.AudioStreamState> AudioStreams { get; set; }
    // All connected clients indexed by their client session ID, containing client metadata such as user ID, device info, viewport dimensions, and locale
    Dictionary<int, Context> Clients { get; set; }
    // Whether debug mode is enabled, providing additional logging and development features
    bool DebugMode { get; set; }
    // User ID of the first human user who joined this session, dynamically reassigned when that user leaves
    string FirstUserId { get; set; }
    // Registry of callable functions organized by client session ID
    Dictionary<int, List<ActionFunctionRegister>> Functions { get; set; }
    // Unique identifier of the specific Ikon server instance handling this session
    string IkonServerId { get; set; }
    object LogInfo { get; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    // Display name of the organization
    string OrganisationName { get; set; }
    // Static user ID of the session owner from server configuration, used for user-specific asset storage paths
    string PrimaryUserId { get; set; }
    // Tells whether the app is being run through publicly accessible endpoints (in local development)
    bool PublicAccess { get; set; }
    // Tells where the server is running from
    ServerRunType ServerRunType { get; set; }
    // Hash of the session identity values that this session was joined on
    string SessionIdentityHash { get; set; }
    // Full URL with session identifier for direct access to current session
    string SessionUrl { get; set; }
    // Unique identifier for the space where this session is running
    string SpaceId { get; set; }
    // Display name of the space
    string SpaceName { get; set; }
    // URL for accessing the app through its space domain
    string SpaceUrl { get; set; }
    // Active UI streams indexed by stream ID
    Dictionary<string, GlobalState.UIStreamState> UIStreams { get; set; }
    // Active video streams indexed by stream ID
    Dictionary<string, GlobalState.VideoStreamState> VideoStreams { get; set; }
    void AddAudioStream(GlobalState.AudioStreamState audioStreamState)
    void AddClient(Context clientContext)
    void AddFunction(int clientSessionId, ActionFunctionRegister function)
    void AddUIStream(GlobalState.UIStreamState uiStreamState)
    void AddVideoStream(GlobalState.VideoStreamState videoStreamState)
    void CopyRetiredFieldsFrom(GlobalState source)
    Context? GetClientContext(int clientSessionId)
    // Returns the context of the first connected client of this user, or null when the user has no connected client
    Context? GetClientContext(string userId)
    // Returns the session id of the first connected client of this user, or 0 when the user has no connected client. Check for 0 before targeting the result, or use GetClientContext, which returns null instead of a sentinel
    int GetClientSessionId(string userId)
    int[] GetClientSessionIds()
    int[] GetClientSessionIdsByProductId(string productId)
    int[] GetClientSessionIdsExcept(int[] clientSessionIds)
    int[] GetHumanClientSessionIds()
    int[] GetMachineClientSessionIds()
    GlobalState.RetiredFields GetOrCreateRetiredFields()
    GlobalState.RetiredFields? GetRetiredFields()
    // Returns null (not an empty list) when none of targetIds is connected; ids that name no connected client are skipped, so the result can be shorter than the input
    List<string>? GetUserIds(IEnumerable<int> targetIds)
    void RemoveAudioStream(string streamId)
    void RemoveClient(int clientSessionId)
    void RemoveFunction(Guid functionId)
    void RemoveUIStream(string streamId)
    void RemoveVideoStream(string streamId)
    void SetReady(int clientSessionId)
    void SetReconnected(int clientSessionId)
    void SetSoftDisconnected(int clientSessionId, ulong softDisconnectAt)
    override string ToString()
    static readonly IReadOnlyList<string> RetiredKeys
  sealed class GlobalState.AudioStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, AudioStreamBegin info)
    // Session ID of the client sending this stream
    int ClientSessionId { get; set; }
    // Stream configuration including codec, sample rate, and channels
    AudioStreamBegin Info { get; set; }
    // Unique identifier for this stream
    string StreamId { get; set; }
    // Track identifier within the client
    int TrackId { get; set; }
  sealed class GlobalState.RetiredFields
    ctor()
    // TODO(channel-compat): back-compat mirror of SpaceUrl for pre-channel-removal clients that still read the old field name. The server writes it via the retired bag with the SpaceUrl value; stop writing once no such old clients remain.
    string? ChannelUrl { get; set; }
    // Renamed to IkonServerId; the server writes it via the retired bag with the new field's value so clients built before the rename still resolve it. Stop writing once no frozen app bundle or cached SDK build in circulation reads it.
    string? ServerSessionId { get; set; }
    // TODO(channel-compat): back-compat mirror of SessionUrl, same lifecycle as ChannelUrl above.
    string? SessionChannelUrl { get; set; }
    // Renamed to SessionIdentityHash, same lifecycle as ServerSessionId above.
    string? SessionHash { get; set; }
  sealed class GlobalState.UIStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, UIStreamBegin info)
    // Session ID of the client sending this stream
    int ClientSessionId { get; set; }
    // Stream configuration including category and metadata
    UIStreamBegin Info { get; set; }
    // Unique identifier for this stream
    string StreamId { get; set; }
    // Track identifier within the client
    int TrackId { get; set; }
  sealed class GlobalState.VideoStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, VideoStreamBegin info)
    // Session ID of the client sending this stream
    int ClientSessionId { get; set; }
    // Stream configuration including codec and resolution
    VideoStreamBegin Info { get; set; }
    // Unique identifier for this stream
    string StreamId { get; set; }
    // Track identifier within the client
    int TrackId { get; set; }
  interface IProtocolMessagePayload
    virtual MessageFlag MessageDefaultFlags { get; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
  enum LogType
    None
    Trace
    Debug
    Info
    Warning
    Error
    Critical
    Event
    Usage
    Exception
  enum MessageFlag
    None
    SendBackToSender
    Delayable
    SendToUser
    Compressed
    Unreliable
  enum Opcode
    NONE
    CONSTANT_GROUP_BITS
    CONSTANT_GROUP_OFFSET
    GROUP_CORE
    CORE_AUTH_RESPONSE
    CORE_AUTH_TICKET
    CORE_GLOBAL_STATE
    CORE_ON_SERVER_STATUS_PING
    CORE_ON_USER_JOINED
    CORE_ON_USER_LEFT
    CORE_ON_CLIENT_JOINED
    CORE_ON_CLIENT_LEFT
    CORE_ON_SERVER_STARTED
    CORE_ON_SERVER_STOPPED
    CORE_ON_SERVER_STOPPING
    CORE_ON_CLIENT_READY
    CORE_CLIENT_READY
    CORE_SERVER_INIT
    CORE_ON_PLUGIN_RELOADED
    CORE_DYNAMIC_CONFIG
    CORE_PROXY_RPC_AUTH_TICKET
    CORE_UPDATE_CLIENT_CONTEXT
    CORE_BACKGROUND_WORK_ACTIVE
    CORE_RESET_IDLE
    CORE_CLIENT_DISCONNECTING
    CORE_ON_APP_READY
    CORE_ON_FRONTEND_RELOADED
    CORE_ON_USER_DATA_ERASED
    CORE_WEBRTC_OFFER
    CORE_WEBRTC_ANSWER
    CORE_WEBRTC_ICE_CANDIDATE
    CORE_WEBRTC_READY
    CORE_WEBRTC_AUDIO_SEGMENT
    CORE_WEBRTC_TRACK_MAP
    CORE_WEBRTC_VIDEO_CAPTURE
    CORE_WEBRTC_ICE_SERVERS_REQUEST
    CORE_WEBRTC_ICE_SERVERS_RESPONSE
    CORE_WEBRTC_CLOSE
    CORE_RELAY_AGENT_AUTH
    CORE_RELAY_AGENT_AUTH_RESULT
    CORE_RELAY_HEARTBEAT
    CORE_RELAY_TCP_CONNECTION_OPENED
    CORE_RELAY_TCP_CONNECTION_CLOSED
    CORE_RELAY_TCP_DATA
    CORE_RELAY_UDP_DATA
    CORE_RELAY_ADD_TUNNEL
    CORE_RELAY_TUNNEL_ADDED
    CORE_RELAY_REMOVE_TUNNEL
    CORE_IKON_SERVER_ENDPOINT_HOST_INFO
    CORE_CLIENT_INITIALIZATION
    CORE_CLIENT_LIFECYCLE_BATCH
    CORE_APP_CONFIG
    GROUP_KEEPALIVE
    KEEPALIVE_REQUEST
    KEEPALIVE_RESPONSE
    GROUP_EVENTS
    EVENTS_PROFILE_UPDATE
    EVENTS_SPEECH_PLAYBACK_COMPLETE
    GROUP_ANALYTICS
    ANALYTICS_LOGS
    ANALYTICS_EVENTS
    ANALYTICS_USAGES
    ANALYTICS_USAGE
    ANALYTICS_SPECIAL_LOG
    ANALYTICS_PROCESSING_UPDATE
    ANALYTICS_REACTIVE_PROCESSING_UPDATE
    ANALYTICS_IKON_PROXY_SERVER_STATS
    ANALYTICS_IKON_RELAY_SERVER_STATS
    ANALYTICS_IKON_TURN_SERVER_STATS
    ANALYTICS_IKON_HOST_SERVER_STATS
    ANALYTICS_TRAFFIC_USAGE
    GROUP_ACTIONS
    ACTION_CALL
    ACTION_ACTIVE
    ACTION_TEXT_OUTPUT
    ACTION_TEXT_OUTPUT_DELTA
    ACTION_TEXT_OUTPUT_DELTA_FULL
    ACTION_SET_STATE
    ACTION_TAP
    ACTION_PAN
    ACTION_ZOOM
    ACTION_OPEN_EXTERNAL_URL
    ACTION_FUNCTION_REGISTER
    ACTION_FUNCTION_CALL
    ACTION_FUNCTION_RESULT
    ACTION_GENERATE_ANSWER
    ACTION_REGENERATE_ANSWER
    ACTION_CLEAR_STATE
    ACTION_CLASSIFICATION_RESULT
    ACTION_AUDIO_STOP
    ACTION_CALL_TEXT
    ACTION_CANCEL_GENERATION
    ACTION_SPEECH_RECOGNIZED
    ACTION_CALL_RESULT
    ACTION_DOWNLOAD
    ACTION_PLAY_SOUND
    ACTION_STOP_SOUND
    ACTION_START_RECORDING
    ACTION_STOP_RECORDING
    ACTION_FUNCTION_ENUMERATION_ITEM
    ACTION_FUNCTION_ENUMERATION_END
    ACTION_FUNCTION_CANCEL
    ACTION_FUNCTION_DISPOSE
    ACTION_FUNCTION_ERROR
    ACTION_FUNCTION_ACK
    ACTION_FUNCTION_AWAITING_APPROVAL
    ACTION_FUNCTION_APPROVAL_REQUIRED
    ACTION_FUNCTION_APPROVAL_RESPONSE
    UI_UPDATE_ACK
    ACTION_CALL2
    ACTION_FUNCTION_REGISTER_BATCH
    ACTION_CUSTOM_USER_MESSAGE
    ACTION_URL_CHANGED
    ACTION_FILE_UPLOAD_PRE_START2
    ACTION_FILE_UPLOAD_PRE_START_RESPONSE2
    ACTION_FILE_UPLOAD_START2
    ACTION_FILE_UPLOAD_START_RESPONSE2
    ACTION_FILE_UPLOAD_DATA2
    ACTION_FILE_UPLOAD_ACK2
    ACTION_FILE_UPLOAD_END2
    ACTION_FILE_UPLOAD_COMPLETE2
    ACTION_FUNCTION_ENUMERATION_ITEM_BATCH
    ACTION_CALL_ACK
    ACTION_TRIGGER_CRON
    ACTION_RESULT
    UI_RESYNC_REQUEST
    ACTION_USER_DATA_ERASURE
    ACTION_FILE_UPLOAD_RESUME2
    ACTION_FILE_UPLOAD_RESUME_RESPONSE2
    GROUP_UI
    UI_STREAM_BEGIN
    UI_STREAM_END
    UI_STYLES
    UI_UPDATE
    UI_STYLES_BATCH
    UI_STYLES_DELETE
    GROUP_COMMON
    GROUP_AUDIO
    AUDIO_STREAM_BEGIN
    AUDIO_STREAM_END
    AUDIO_FRAME_VOLUME
    AUDIO_FRAME
    AUDIO_SHAPE_FRAME
    AUDIO_PLAYBACK_REPORT
    GROUP_VIDEO
    VIDEO_STREAM_BEGIN
    VIDEO_STREAM_END
    VIDEO_FRAME
    VIDEO_REQUEST_IDR_FRAME
    VIDEO_INVALIDATE_FRAME
    GROUP_ALL
    GROUP_APP_LOCAL
    CONSTANT_GROUP_MASK
  static class Opcodes
    static bool IsOpcodeInAnyGroup(Opcode opcode, Opcode groups)
  static class PayloadCompression
    // When non-null, Buffer is rented from the shared ArrayPool<T> and is OVERSIZED: its Length is the rented capacity, not the compressed size. Read only the first Length bytes (e.g. Buffer.AsSpan(0, Length)) — the tail is undefined. The caller OWNS the returned buffer and MUST hand it back with ReturnBuffer once done with it, or the pooled array leaks; when Buffer is null there is nothing to return.
    static (byte[]? Buffer, int Length) Compress(ReadOnlySpan<byte> data)
    // The transport bounds the COMPRESSED frame, not what it expands to — a frame of zeros inflates by orders of magnitude. This runs before the sender is authenticated, so without a ceiling a single frame can exhaust the process's memory. Buffer is rented from the shared ArrayPool<T> and is OVERSIZED: its Length is the rented capacity, not the decompressed size. Read only the first Length bytes (e.g. Buffer.AsSpan(0, Length)) — the tail is undefined. The caller OWNS the returned buffer and MUST hand it back with ReturnBuffer once done with it, or the pooled array leaks.
    static (byte[] Buffer, int Length) Decompress(ReadOnlySpan<byte> compressedData, int estimatedSize = 0, int maxDecompressedSize = 0)
    static void ReturnBuffer(byte[]? buffer)
    static bool ShouldCompress(int payloadSize)
    const int CompressionThreshold = 1024
  enum PayloadType
    Unknown
    MessagePack
    MemoryPack
    Json
    Teleport
    All
  class ProtocolMessage : AsyncLocalInstance<ProtocolMessage>
    ctor()
    // Wraps the buffer WITHOUT copying — data is aliased and every accessor reads straight from it. The caller must keep the buffer alive and unchanged for the lifetime of this message; a reused or overwritten buffer makes it silently read corrupted data. Use CopyFrom when the source buffer will be reused.
    ctor(Memory<byte> data)
    Memory<byte> Data { get; }
    MessageFlag Flags { get; }
    int Length { get; }
    Opcode Opcode { get; }
    Memory<byte> Payload { get; }
    Span<byte> PayloadSpan { get; }
    PayloadType PayloadType { get; }
    int PayloadVersion { get; }
    int SenderId { get; }
    int SequenceId { get; }
    string StreamId { get; }
    int TargetIdCount { get; }
    int[] TargetIds { get; }
    ReadOnlySpan<int> TargetIdsSpan { get; }
    int TrackId { get; }
    // Copies data, so the caller may reuse, return, or overwrite the source buffer immediately. Prefer this over the aliasing #ctor constructor whenever the source is a pooled or otherwise reused buffer.
    static ProtocolMessage CopyFrom(ReadOnlySpan<byte> data)
    static ProtocolMessage Create(int senderId, IProtocolMessagePayload payload, PayloadType payloadType = Unknown, int trackId = 0, int sequenceId = 0, MessageFlag flags = None, IReadOnlyList<int>? targetIds = null, bool compress = false)
    T GetPayload<T>() where T : IProtocolMessagePayload
    IProtocolMessagePayload GetPayload()
    static ProtocolMessage ModifyMessage(ProtocolMessage message, int? senderId = null, int? trackId = null, int? sequenceId = null, MessageFlag? flags = null, IReadOnlyList<int>? targetIds = null)
    static ProtocolMessage ModifyPayload(IProtocolMessagePayload payload, ProtocolMessage message, PayloadType payloadType = Unknown)
    // Registers an app-local message type (an app's own schema/*.tp type, opcode in Opcode.GROUP_APP_LOCAL) at runtime. Called from the generated type's static constructor — app-local types are compiled into the app assembly and are not visible to the platform's compile-time ProtocolMessage source generator.
    static void RegisterAppLocalMessageType(Type type, Opcode opcode, int version)
    override string ToString()
    static ProtocolMessage WithFlags(ProtocolMessage message, MessageFlag additionalFlags)
    PayloadType DefaultPayloadType
    const int MaxMessageSize = 20971520
    const int MinimumHeaderLength = 27
    static readonly Dictionary<Opcode, Type> OpcodeToType
    static readonly Dictionary<Type, Opcode> TypeToOpcode
    static readonly Dictionary<Type, int> TypeToVersion
  class ProtocolMessageAttribute : Attribute
    ctor(int version = 0, Opcode opcode = NONE, bool unreliable = false)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    bool Unreliable { get; }
  static class ProtocolVersion
    static int Version { get; }
  sealed class RouteToken : IProtocolMessagePayload
    ctor()
    ctor(uint expiresAt, string host, int httpsPort, int tlsPort)
    // Epoch seconds after which the gateway refuses the token. Always set — this one leaves the machine, and unlike the ConnectToken it authorizes a dial target rather than a session, so it is minted short-lived.
    uint ExpiresAt { get; set; }
    // The ikon server's hostname, as the gateway will dial and TLS-validate it. A fleet instance presents the platform wildcard certificate, so this must be the real FQDN and never an address.
    string Host { get; set; }
    // Both upstream ports, because the two legs land on different ones: /connect is HTTPS to HttpsPort, and the channel is a raw TCP socket to TlsPort. HostAgent allocates them separately from one range, so neither can be derived from the other.
    int HttpsPort { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int TlsPort { get; set; }
  // Capability levels advertised by a connecting SDK via Context.SdkCapability (companion to Context.SdkType). Opaque and monotonically increasing — bump when adding a capability the ikon server must detect per connected client. 0 means a legacy client that predates capability negotiation.
  static class SdkCapabilities
    // Client sends its ClientEnvironment on the /connect request instead of expecting the token minter to have baked it into the connect token; the minter may leave the block out. A client below this level still has its environment carried in the token.
    const int ClientEnvironmentOnConnect = 5
    // Client handles the CORE_CLIENT_INITIALIZATION message — the function registry sent out-of-band right after the joining client's GlobalState — and registers those functions during connect. When any connected client advertises less than this, the server keeps the registry embedded in GlobalState.Functions for the whole session.
    const int ClientInitializationMessage = 4
    // Client unpacks the batched CORE_CLIENT_LIFECYCLE_BATCH message (client/user lifecycle events coalesced into one payload). When all connected external clients advertise at least this, the server coalesces and debounces those broadcasts; otherwise it falls back to per-event broadcasts. Internal (localhost) clients always receive the events immediately, unbatched.
    const int ClientLifecycleBatching = 3
    // Deliberately still ClientInitializationMessage: this constant is what the C# SDK and plugins advertise, and they do not yet send a ClientEnvironment on connect. Advertising a level a build does not implement is how a client talks itself out of data it needs — here it would tell the minter to omit an environment nobody then supplies. It moves when the C# side sends one.
    const int Current = 4
    // Client understands server functions delivered out-of-band rather than embedded in GlobalState.Functions. Superseded by ClientInitializationMessage: do not gate the functions-out-of-GlobalState decision on this level — it is too low and matches clients that predate the ClientInitialization message.
    const int FunctionRegistryOutsideGlobalState = 1
    // Client honors the keepalive timeout in AuthResponse.KeepaliveTimeoutMs instead of hard-coding it. When all connected clients advertise at least this, the server may stretch its keepalive send interval beyond the legacy client's fixed watchdog; otherwise it stays within the legacy-safe cap.
    const int KeepaliveTimeoutNegotiation = 2
  enum SdkType
    Unknown
    DotNet
    TypeScript
    Cpp
    Dart
    Rust
  // Capability levels advertised by the ikon server to a connecting client via AuthResponse.ServerCapability (companion to the client's Context.SdkCapability). Opaque and monotonically increasing — bump when adding a server behavior a client must detect to alter its connect handling. 0 means a legacy server that predates capability negotiation.
  static class ServerCapabilities
    // Server sends a ClientInitialization message immediately after the joining client's GlobalState, carrying the server/app function registry out-of-band. A client that sees at least this waits for that message during connect (so server functions are registered before the connect call returns) instead of expecting functions embedded in GlobalState.
    const int ClientInitializationMessage = 1
    const int Current = 1
  enum ServerRunType
    Local
    Cloud
  enum ServerStatus
    Unknown
    Starting
    Running
    Stopping
    Stopped
  enum StyleFormat
    Css
    Flutter
  static class UIElementLabels
    const string Blur
    const string ChatMessage
    const string Disabled
    const string ImageAvatar
    const string Markdown
    const string SizeExtraSmall
    const string SizeFitContent
    const string SizeFullWidth
    const string SizeLarge
    const string SizeMedium
    const string SizeSmall
    const string Wrap
  sealed class UIStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string category)
    string Category { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
  static class UIStreamCategories
    const string App
    const string Chat
    const string Collapsed
    const string DebugOverlay
    const string Footer
    const string Header
    const string Input
    const string Menu
    const string Overlay
    const string Preview
    const string SecondScreen
  static class UIStylesKeys
    const string Common
    const string Crosswind
    const string Css
    const string Flutter
    const string ReactNative
  enum UserType
    Unknown
    Machine
    Human
  enum VideoCodec
    Unknown
    H264
    Vp8
    Vp9
    Av1
  sealed class VideoStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string streamId, string description, string sourceType, VideoCodec codec, string codecDetails, int width, int height, double framerate, string? correlationId)
    VideoCodec Codec { get; set; }
    string CodecDetails { get; set; }
    string? CorrelationId { get; set; }
    string Description { get; set; }
    double Framerate { get; set; }
    int Height { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string SourceType { get; set; }
    string StreamId { get; set; }
    int Width { get; set; }

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
  class Reactive<T> : IReactive
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
  // Marker type for the default-value Reactive<T> constructor. Never pass it explicitly — write new Reactive<T>() and the value starts at default(T); passing any argument at all selects the value constructor.
  readonly struct UseDefault
  // Same reactive contract as Reactive<T>, partitioned per user and shared across that user's client sessions (use ClientReactive<T> when each client needs its own value). .Value resolves against the active user scope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throws when none is active. Background work carries no user scope, so name the user instead via SetFor / ValueFor.
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue)
    ctor(Func<string, T> initialValue)
    void SetFor(string userId, T value)
    void UpdateFor(string userId, Func<T, T> mutator)
    T ValueFor(string userId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    void ClearFor(string userId)
    bool RemoveFor(string userId, TKey key)
    void SetFor(string userId, TKey key, TValue value)
    void UpdateFor(string userId, Action<Dictionary<TKey, TValue>> transform)
    IReadOnlyDictionary<TKey, TValue> ValueFor(string userId)
  // Shorthand for ReactiveEffect<UserScope>: each distinct user gets its own runner; the same user across multiple sessions shares one runner.
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    bool AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Action<HashSet<T>> transform)
    IReadOnlyCollection<T> ValueFor(string userId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    void AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    IReadOnlyList<T> ValueFor(string userId)

namespace Ikon.Common.Core.Scope
  readonly struct BackendTokenScope : IScopeKey
    ctor(string token)
    string Id { get; }
    string Name { get; }
  // Each time a client connects to the server, it gets a new ClientScope with a unique Id (session ID). This scope is used by ClientReactive<T> to partition state per client. Relationship to UserScope: Multiple ClientScopes can belong to the same user. For example, a user connected from two clients has two different ClientScope IDs but the same UserScope ID. Lifecycle: Active during UI rendering inside UI.Root(). Automatically established by the framework for each client iteration.
  readonly struct ClientScope : IScopeKey
    ctor(int sessionId)
    ctor(Context context)
    int Id { get; }
    string Name { get; }
  readonly struct CustomScope : IScopeKey
    ctor(string name, string id)
    string Id { get; }
    string Name { get; }
  interface IScopeKey
    object Id { get; }
    string Name { get; }
  // Pushed by the framework alongside UserScope / ClientScope during the per-(client, mount) render iteration in ReactiveRoot.RunAsync.
  readonly struct MountScope : IScopeKey
    ctor(string mountId)
    string Id { get; }
    string Name { get; }
    // The mount id every Ikon app emits today on its single Parallax stream; apps that don't override IAppBase.Mounts render under this id.
    const string DefaultMountId
  readonly struct OperationScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  readonly struct RunScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  readonly struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
  // Machine-triggered work has no ClientScope and no UserScope, so without this its cost lands in the space's totals attached to nothing and a schedule quietly burning credits is indistinguishable from the app's ordinary use. Every log event carries the active scopes, so the cost of an AI call made inside a trigger handler is attributed by the ambient scope alone — call sites need no change. Scoped to the invocation rather than the session on purpose: a session woken by cron goes on to serve clients, and their spend is theirs, not the schedule's. The values match the backend's AppSessionSource spelling, so the trigger a cost row carries reads the same as the source stamped on the session that ran it.
  readonly struct TriggerScope : IScopeKey
    ctor(string kind)
    string Id { get; }
    string Name { get; }
    const string Cron
    const string Endpoint
  // Identifies a logical user across their multiple client sessions. Used by UserReactive<T> to share state across a user's multiple connected clients. Lifecycle: Active during UI rendering inside UI.Root(). Automatically established by the framework alongside ClientScope.
  readonly struct UserScope : IScopeKey
    ctor(string userId)
    ctor(Context context)
    string Id { get; }
    string Name { get; }

namespace Ikon.Common.Core.Signing
  sealed record SignatureDocument
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  sealed record SignatureOrderRequest
    ctor(string Purpose, IReadOnlyList<SignatureDocument> Documents, SignatureSigner Signer, string? CostAttributionKey = null, string? Title = null, string? ClientReturnUrl = null)
    string? ClientReturnUrl { get; init; }
    string? CostAttributionKey { get; init; }
    IReadOnlyList<SignatureDocument> Documents { get; init; }
    string Purpose { get; init; }
    SignatureSigner Signer { get; init; }
    string? Title { get; init; }
  enum SignaturePolicy
    PkiSigning
    EidHub
  sealed record SignatureSigner
    ctor(SignaturePolicy Policy, string? Vendor = null, IReadOnlyList<string>? IdpNames = null, IReadOnlyList<string>? RequestedAttributes = null)
    IReadOnlyList<string>? IdpNames { get; init; }
    SignaturePolicy Policy { get; init; }
    IReadOnlyList<string>? RequestedAttributes { get; init; }
    string? Vendor { get; init; }
  // The platform downloads the result from the upstream signing vendor, hashes it, and hands the signed bytes plus evidence metadata to the requesting app. Apps should persist Bytes as the system of record — the platform retention is short.
  sealed record SignedDocument
    ctor(string OrderId, byte[] Bytes, string MimeType, DateTimeOffset SignedAt, string SignedDocumentHash, string IdentityScheme, string? SignerNameHash, string? EvidenceLevel)
    byte[] Bytes { get; init; }
    string? EvidenceLevel { get; init; }
    string IdentityScheme { get; init; }
    string MimeType { get; init; }
    string OrderId { get; init; }
    DateTimeOffset SignedAt { get; init; }
    string SignedDocumentHash { get; init; }
    // The signer's legal name as the identity provider reported it, when the order requested the name attribute; null otherwise. This is what an app shows a user and checks against the signer it expected — SignerNameHash is keyed by a platform secret and can do neither.
    string? SignerName { get; init; }
    string? SignerNameHash { get; init; }

namespace Ikon.Common.Core.Telephony
  sealed record SmsMessage
    // From: Who sent it, in E.164. Pass it to app.Telephony.SendSmsAsync to reply.
    // To: The number of the app's that received it.
    // Text: The message body.
    // MessageId: The provider's id for the message.
    ctor(string From, string To, string Text, string MessageId)
    string From { get; init; }
    string MessageId { get; init; }
    string Text { get; init; }
    string To { get; init; }
  // The outcome of sending an SMS. No price: a send is charged to the space in platform credits, readable with ikon app costs.
  sealed record SmsSendResult
    // MessageId: The provider's id for the message, for correlating delivery reports.
    // From: The number or sender id the message was sent from.
    // Parts: Billable segments. A message using non-GSM characters fits roughly half as much per segment.
    // Status: The provider's status for the message at the moment it was accepted.
    // Replyable: Whether the recipient can reply. False when the space holds no number local to the recipient's market: a foreign number is commonly stripped in transit and shown as "Unknown", so the message arrives but nothing can be sent back.
    ctor(string MessageId, string From, int Parts, string Status, bool Replyable)
    string From { get; init; }
    string MessageId { get; init; }
    int Parts { get; init; }
    bool Replyable { get; init; }
    string Status { get; init; }
  sealed record TelephonyNumber
    // Number: The number in E.164 form, for example +358401234567.
    // Country: The ISO 3166-1 alpha-2 country the number belongs to.
    // Provider: Which carrier serves this number. Two of the app's numbers may differ.
    // Capabilities: What the number can carry, as the provider names it — sms, voice.
    // IsDefault: Whether this is the number used when a send or a call names none. At most one of the app's numbers is the default; when none is, the platform picks one local to each recipient's market.
    // SessionIdentity: Which instance this number's incoming messages and calls are delivered to. Empty means the app's shared instance. Two numbers can carry different identities, which is how one app answers as several users.
    ctor(string Number, string Country, string Provider, IReadOnlyList<string> Capabilities, bool IsDefault, IReadOnlyDictionary<string, string> SessionIdentity)
    IReadOnlyList<string> Capabilities { get; init; }
    string Country { get; init; }
    bool IsDefault { get; init; }
    string Number { get; init; }
    string Provider { get; init; }
    IReadOnlyDictionary<string, string> SessionIdentity { get; init; }
  sealed record TelephonyStatus
    // Enabled: Whether the space holds any number at all.
    // Numbers: The numbers the space holds. Messages and calls are sent from these.
    ctor(bool Enabled, IReadOnlyList<TelephonyNumber> Numbers)
    bool Enabled { get; init; }
    IReadOnlyList<TelephonyNumber> Numbers { get; init; }
