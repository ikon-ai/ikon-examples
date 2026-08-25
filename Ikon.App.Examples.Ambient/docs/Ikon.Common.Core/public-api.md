# Ikon.Common.Core Public API

namespace Ikon.Common.Core
  // Path rules shared by everything that handles app-owned routes: navigation, boot-snapshot capture, and the bundler. One canonical form ends route-matching drift between layers — the same string must match at declaration, capture, manifest, gateway, and client.
  static class AppRoutes
    // True when path falls under a platform-reserved prefix (/ikon or /api, either exactly or as a {prefix}/… subpath). The query string is ignored and a missing leading slash is tolerated. /ikonic and other prefixes that merely share a leading substring are NOT reserved.
    static bool IsReservedPath(string path)
    // True when variantId is a valid boot-snapshot variant id: 1-32 characters of lowercase ASCII letters, digits, or non-leading -. The id doubles as a file-name component (boot-snapshot-variant-{id}-{hash}.json) and as the app-side switch key on view.SnapshotVariant.
    static bool IsValidVariantId(string? variantId)
    // True when canonicalPath matches canonicalPattern. Both must already be canonical (TryCanonicalizePattern / TryCanonicalizeRoute). Matching is segment-wise: a literal matches by ordinal equality, * matches exactly one segment, and a final ** matches zero or more remaining segments — so / matches only the root and /** matches every path including the root. This is the C# member of the lockstep matcher trio (with routePatternMatches in sdk-ui's boot-snapshot-route.ts and the inline boot-snapshot-preload-inline.js) — the three implementations must decide identically.
    static bool RoutePatternMatches(string canonicalPattern, string canonicalPath)
    // Canonicalizes a boot-snapshot seed-rule path pattern. A pattern is a canonical route whose segments may additionally be * (exactly one segment, any content) or a final ** (zero or more trailing segments). Rejects : anywhere (reserved as the seed-rule separator), ** before the final segment, and segments mixing literals with wildcards (such as a*b, reserved for the future).
    // pattern: The pattern as declared, e.g. /*/**
    // canonical: The canonical form when the pattern is valid, otherwise empty
    // error: Why the pattern was rejected, otherwise null
    static bool TryCanonicalizePattern(string? pattern, out string canonical, out string? error)
    // Canonicalizes a route declared for boot-snapshot capture: percent-decoded, no trailing slash (except the root /). Unicode composition is preserved as declared — normalization is unavailable under InvariantGlobalization, so no layer normalizes and the declared bytes are the contract. Rejects routes with a query string, fragment, backslash, control characters, or a platform-reserved prefix. The same canonical form is applied at declaration, capture, the route manifest, the gateway, and the client, so a route matches at every layer or none.
    // route: The route as declared, e.g. /live/listing/42
    // canonical: The canonical form when the route is valid, otherwise empty
    // error: Why the route was rejected, otherwise null
    static bool TryCanonicalizeRoute(string? route, out string canonical, out string? error)
    // Parses a [BootSnapshot] seed rule of the form pattern:variantId (the same colon-separated shape as the Databases entries). The pattern half is canonicalized via TryCanonicalizePattern; the variant id must satisfy IsValidVariantId.
    static bool TryParseSeedRule(string? rule, out string pattern, out string variantId, out string? error)
    // The platform reserves the entire /ikon subtree (current + future platform routes) and /api (the app/cell endpoint surface). The load balancer intercepts these before they reach the app's SPA, so an app route under them can never be served.
    static readonly string[] ReservedPathPrefixes
  // Verifies platform-signed assertions (e.g. StepUpAssertion) issued by the Ikon platform backend. Fetches the platform JWKS from {platformBaseUrl}/.well-known/jwks.json on demand and caches the keys for five minutes, so a rotated signing key is picked up without recreating the verifier.
  sealed class AssertionVerifier
    ctor(string platformBaseUrl, HttpClient? httpClient = null, Func<DateTimeOffset>? clock = null)
    // Generic JWT validation: JWKS-backed signature verification + standard iss/aud/exp checks + (when present) iat clock-skew guard. Returns the decoded claims as a JsonDocument — caller owns disposal — plus the token's exp so a caller can cache the validated result against the token lifetime. Use this for OAuth 2.1 bearer-token resource-server validation where the step-up-specific projection in VerifyAsync isn't relevant.
    // token: The encoded JWT.
    // expectedIssuer: Required iss value.
    // expectedAudience: Required aud value (matches a string aud or any entry of an array aud).
    // ct: Cancellation token.
    Task<(JsonDocument Claims, DateTimeOffset ExpiresAt)> VerifyAndExtractClaimsAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = default)
    Task<StepUpAssertion> VerifyAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = default)
  class AsyncLocalInstance<T> where T : new()
    ctor()
    // The current instance of T. Resolution depends on the mode: in global mode (the default, used by the CLI and build-time tooling) this lazily creates and returns a single process-wide singleton; in async-local mode (enabled at ikon-server launch via EnableAndInitAsyncLocalInstance) it returns the instance set on the current async flow. In async-local mode a flow that has no instance set — e.g. a Task.Run body or timer callback that did not inherit the context — makes this THROW AsyncLocalInstanceNotSetException rather than returning a shared fallback.
    static T Instance { get; }
    // Reverts this type to global mode: clears the current flow's instance and makes Instance return the process-global singleton again. The inverse of EnableAndInitAsyncLocalInstance.
    static void DisableAsyncLocalInstance()
    // Switches this type to async-local mode and seeds the current flow with a fresh instance. Call this before SetAsyncLocalInstance — a set before the mode is enabled is ignored. After this, Instance resolves per async flow instead of returning the process-global singleton.
    static void EnableAndInitAsyncLocalInstance()
    // A no-op when async-local mode has not been enabled via EnableAndInitAsyncLocalInstance — the caller then keeps reading the process-global singleton. A warning is written to the console in that case so the ineffective set is not silent; scoped code that depends on the set taking effect must enable the mode first.
    static void SetAsyncLocalInstance(T value)
  sealed class AsyncLocalInstanceAttribute : Attribute
    ctor()
  static class AsyncLocalInstanceDiagnostics
    // When true, accessing an AsyncLocalInstance<T> in global (non-async-local) mode logs a one-per-type warning with a stack trace. Enabled at ikon-server launch: there every such instance must be async-local, because a process-global singleton collides when multiple ikon-servers share one process (host + embedded preview/sandbox). Off elsewhere (CLI, build-time config generation) where global mode is fine and the warnings would be noise.
    static bool WarnOnGlobalModeAccess
  // Thrown by AsyncLocalInstance<T>.Instance when async-local mode is enabled but no instance has been set on the current flow — e.g. accessing Log.Instance from a Task.Run body or a timer callback that did not inherit the async-local context. Catch this rather than a bare Exception to handle the missing-scope case selectively.
  sealed class AsyncLocalInstanceNotSetException : Exception
    ctor(string message)
  class BackendQuotaExceededException : UserException
    ctor(string key, int current, int limit, string friendlyMessage)
    int Current { get; }
    string Key { get; }
    int Limit { get; }
  // None of it is authoritative. The client picked every value, and forging one only misdescribes or misconfigures the forger's own session — so a signature would buy nothing while making the connect token several hundred characters longer, which is exactly what it used to do. What keeps "copy the whole thing" safe is the shape of ClientEnvironment: it has no field worth forging, and cannot express UserId, IsInternal, opcode groups or anything else that authorizes. That is a property to preserve, not a coincidence.
  static class ClientEnvironmentCodec
    static string Encode(ClientEnvironment environment)
    // The environment as an older client's connect token carries it.
    static ClientEnvironment FromConnectToken(ConnectToken token)
    // The environment to build this client's Context from: the one it presented, or — for a client that predates SdkCapabilities.ClientEnvironmentOnConnect and therefore sends none — the copy the token minter baked into the connect token.
    static ClientEnvironment Resolve(string? presented, ConnectToken token)
    static bool TryDecode(string? presented, out ClientEnvironment? environment)
    // Reserved query parameter carrying the environment on a connect request.
    const string QueryParam
  // This replaced a JWT whose payload was the whole ConnectToken as JSON. The token is opaque to every SDK — they carry it in a URL and hand it back — so nothing outside this type needs to parse it, and the JSON bought nothing but size: PascalCase field names, every field present including the empty ones, then base64 over the lot. The envelope — Teleport body, truncated HMAC-SHA256, base64url — is SignedTokenCodec, shared with the route token a fleet proxy gateway verifies. The key is the caller's per-server ConnectToken secret, never the platform secret.
  static class ConnectTokenCodec
    static string Encode(ConnectToken token, byte[] key)
    // The ConnectToken.ExpiresAt value for a token minted now with this lifetime.
    static uint ExpiresIn(TimeSpan lifetime)
    static long ToUnixSeconds(DateTime utc)
    // Verify and decode a presented token. The signature is checked before the body is parsed, so a forged payload never reaches the deserializer.
    // nowUtc: Supplied rather than read here so tests can drive the expiry boundary.
    static ConnectTokenStatus TryDecode(string presented, byte[] key, DateTime nowUtc, out ConnectToken? token)
    // How long a minted token stays valid. Long rather than short on purpose: on a dropped connection the SDK retries /connect once with the token it already holds before going back to the backend for a new one, so a short lifetime turns every laptop-wakeup reconnect into an extra round trip — and possession of the token is not the only thing standing between a caller and the app.
    static readonly TimeSpan DefaultLifetime
  // Why a presented connect token was not accepted.
  enum ConnectTokenStatus
    Valid
    // Not decodable at all — truncated, corrupt, or a token layout this build predates.
    Malformed
    // Decodable, but not signed with this server's key. A forgery, or a token for another server.
    BadSignature
    // Genuinely ours and correctly signed, but past ConnectToken.ExpiresAt. Kept distinct from the other two on purpose: a client that reconnects after a long sleep should mint a fresh token rather than treat this as an authentication failure it cannot recover from.
    Expired
  // Thrown when an email send names a sender identity the platform cannot honour — the space has no verified sending domain, or the requested sender domain is not a verified sending domain of the space. Retrying without the sender fields sends from the platform's own address instead.
  class EmailSenderNotAvailableException : UserException
    ctor(string friendlyMessage, string? senderDomain = null, string? hint = null)
    string? Hint { get; }
    // The sender domain the request named, when the failure was about a specific domain.
    string? SenderDomain { get; }
  // A logical fabric address: what a message is FOR, independent of where it currently lives. The directory maps an Endpoint to a concrete route (session id + relay group); the routing engine itself keeps working on session ids. IdentityHash is the canonical SessionIdentityHash — for cells it already folds the cell type in, and CellType is carried alongside for directory lookup and provisioning.
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
  // Provides resilient conversions between loosely typed LLM/tool payloads and strongly typed function parameters/results. Handles primitives, arrays (including single-item arrays), Newtonsoft JSON tokens, and falls back to System.Text.Json when needed.
  static class ExtendedCast
    static T? Convert<T>(object? value)
    // A null value against a NON-nullable value type yields that type's default — 0 for Int32, false for Boolean — NOT null, so a missing LLM field is indistinguishable from a real zero. Make the target nullable (e.g. int?) when the caller must tell "absent" from "zero".
    static object? Convert(object? value, Type targetType)
    // Deserializes a JsonElement into targetType, tolerating the placeholders LLMs often emit when a schema marks every property required but the underlying field is nullable: "" for collections/objects becomes null, "" for bool becomes false, etc. Falls back to ExtendedCast conversion on type mismatch so callers pick up array-wrap and single-item-array behaviour for free.
    static object? FromJsonElement(JsonElement element, Type targetType)
  static class ExtendedCastExtensions
    static T? ExtendedCast<T>(this object? value)
    static object? ExtendedCast(this object? value, Type targetType)
  class FeatureFlagsStorage : AsyncLocalInstance<FeatureFlagsStorage>
    ctor()
    ImmutableDictionary<string, bool> ReadOnlyFeatureFlags { get; }
    bool Get(string featureFlagName)
    // Sets a feature flag, overwriting any existing value by default so that the last write wins. Pass shouldOverride = false to keep an already-set value and only log the refused write — the flag is then left untouched.
    void Set(string featureFlagName, bool value, bool shouldOverride = true)
  class FeatureNotEnabledException : UserException
    ctor(string featureKey, string friendlyMessage, string? hint = null)
    string FeatureKey { get; }
    string? Hint { get; }
  class HighPrecisionTimestamp : AsyncLocalInstance<HighPrecisionTimestamp>
    ctor()
    DateTime UtcNow { get; }
  static class HostUtils
    // Deletes a directory tree, clearing ReadOnly attributes along the way (git marks its pack files read-only, which makes a plain Directory.Delete fail with access denied). Continues past individual failures instead of stopping at the first one and returns the paths that could not be deleted; an empty list means the directory is completely gone.
    static IReadOnlyList<string> DeleteDirectoryBestEffort(string path)
    // Scans upward from startPort for a port free on both TCP and UDP right now. This is a racy probe: it binds and immediately closes a socket, so the port is free again the moment this returns and before the caller can bind it for real — two concurrent scanners will pick the same port and the second bind fails. usedPorts only de-duplicates within a single caller. To claim a port safely across concurrent starts in this process, go through PortLease.Take rather than calling this directly.
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
    // Clones obj by serializing it to JSON and deserializing it back to T. This is a JSON PROJECTION, not a faithful object clone: the copy is materialized as the static type T, so a polymorphic/derived runtime type collapses to T; members without a setter and members the serializer skips are dropped; and a reference cycle throws. Use it only for plain, tree-shaped, fully serializable data.
    static T DeepCopy<T>(T obj)
    static string Format(string json, JsonOptions? options = null)
    static T From<T>(string json, JsonOptions? options = null)
    static object? From(string json, Type type, JsonOptions? options = null)
    static object? From(string json, string typeName, JsonOptions? options = null)
    // Like Deserialize<T>, but tolerant of LLM responses that wrap the JSON payload in a markdown code fence (```json ... ``` or ``` ... ```) or STRINGIFY it — the document JSON-encoded as a string, bare or as an object's single property value. Tries direct deserialization first; on JsonException, looks for an embedded payload and retries with that content. The recovery only runs on the failure path, so the happy path pays no extra cost. This always deserializes with System.Text.Json and the supplied JsonSerializerOptions; it does NOT honor the Ikon JsonOptions engine switches (UseJson5, Newtonsoft). For those, use FromLLMResponse<T>.
    static T FromLLMResponse<T>(string text, JsonSerializerOptions? options)
    // Like From<T>, but tolerant of LLM responses that wrap the JSON payload in a markdown code fence or stringify it (see FromLLMResponse<T>). Tries direct deserialization first; on a JSON parse failure (from either System.Text.Json or Newtonsoft, depending on JsonOptions.UseJson5), looks for an embedded payload and retries with that content.
    static T FromLLMResponse<T>(string text, JsonOptions? options = null)
    static Type? ResolveTypeByName(string typeName)
    static string To<T>(T obj, JsonOptions? options = null)
  // Serialization toggles for Json. Immutable; construct with named arguments for the toggles that differ from the defaults, e.g. new JsonOptions(camelCase: true). The default instance (new JsonOptions()) matches the behavior of calling the Json methods without options.
  sealed class JsonOptions
    ctor(bool useJson5 = false, bool indentation = true, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false, bool caseInsensitive = false)
    // Use camelCase property names instead of the declared C# names.
    bool CamelCase { get; }
    // Match property names case-insensitively when deserializing. Ignored when serializing and when UseJson5 is set (Newtonsoft is already case-insensitive).
    bool CaseInsensitive { get; }
    // Use camelCase enum value names (only applies when EnumsAsNames is set).
    bool EnumCamelCase { get; }
    // Write enum values as their names instead of integers.
    bool EnumsAsNames { get; }
    // Include public fields in addition to properties.
    bool IncludeFields { get; }
    // Write null-valued members instead of omitting them.
    bool IncludeNull { get; }
    // Write indented, multi-line output. Ignored when deserializing.
    bool Indentation { get; }
    // Serialize/deserialize with Newtonsoft (JSON5-tolerant: comments, trailing commas) instead of System.Text.Json.
    bool UseJson5 { get; }
    // Single-line output (Indentation off), defaults otherwise.
    static readonly JsonOptions Compact
  // One legacy_usage_observed event per distinct (feature, detail, caller space) key per process, never per call: the question is whether a path is reached at all, so a busy server reporting a thousand old calls says exactly what one report says and costs a thousand times more. Every part of the key comes from a small closed set — a handful of features, per feature a payload version, capability level or type name, and the spaces calling this host — so the bookkeeping is bounded by the shims that exist rather than by traffic. Nothing is emitted when no deprecated path runs, which is the ordinary case.
  static class LegacyUsage
    // Records the first observation of feature at detail (and callerSpaceId, where the caller is a separate space) in this process, and returns whether it was in fact the first — later observations of the same key are dropped.
    // feature: One of the constants on this type.
    // detail: The dimension that decides the retirement threshold — a version, a capability level, a type name. Empty when the feature has no such dimension.
    // sessionId: The session the first observation came from, for tracing it back to a peer. Deliberately outside the dedup key: keying on it would emit one event per old client.
    // callerSpaceId: The space that made the call, for the features whose peer is a different space than the reporting server. Part of the key, because deduplicating on the detail alone would name only whichever space happened to call first. Empty — the usual case — means the peer belongs to the reporting server's own space, which the backend already stamps onto every event, so nothing is lost by leaving it out.
    static bool Report(string feature, string detail = "", int sessionId = 0, string callerSpaceId = "")
    // A plugin binary implements only the obsolete ConnectAsync, not ConnectAsync2. Detail is the plugin type name.
    const string PluginConnectAsyncV1
    // A V1 ACTION_CALL (opcode 1) arrived where ACTION_CALL2 is expected.
    const string ProtocolV1ActionCall
    // An init payload named a plugin that no longer exists and was skipped. Detail is the plugin type name.
    const string RemovedPluginRequested
    // An RPC caller's PayloadVersion. Detail is the version, reported for every caller so the fleet floor is visible, not just callers below one particular rung. The only feature whose peer can belong to a different space than the reporting server, so the only one that carries a caller space.
    const string RpcPayloadVersion
    // A client's advertised SdkCapability. Detail is the level, reported for every client for the same reason as RpcPayloadVersion.
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
    // Log a critical failure with an associated exception. Convenience overload for the .NET-conventional logger.Critical(message, exception) shape — the exception's full ToString() is appended to the message so stack traces land in the log without needing to interpolate ex into the message.
    void Critical(string message, Exception exception)
    // Log an exception with an associated message — same as Critical but with the exception first, matching the Serilog / Microsoft.Extensions.Logging idiom logger.LogCritical(ex, message).
    void Critical(Exception exception, string message)
    void Debug(string message)
    void DisableFileOutput()
    void EnableFileOutput(string filePath, bool append = false)
    void Error(string message)
    // Log an error with an associated exception. Convenience overload for the .NET-conventional logger.Error(message, exception) shape — the exception's full ToString() is appended to the message so stack traces land in the log without needing to interpolate ex into the message.
    void Error(string message, Exception exception)
    // Log an exception with an associated message — same as Error but with the exception first, matching the Serilog / Microsoft.Extensions.Logging idiom logger.LogError(ex, message).
    void Error(Exception exception, string message)
    void Event(string name, object? parameters = null)
    // Logs message at Exception level and returns it unchanged, so the same call both records and supplies the throw message in one expression: throw new SomeException(Log.Instance.Exception("what went wrong")). It does not create, wrap, or rethrow any exception — the return value is exactly the input string.
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
    // Log a warning with an associated exception. Convenience overload for the .NET-conventional logger.Warning(message, exception) shape — the exception's full ToString() is appended to the message so stack traces land in the log without needing to interpolate ex into the message.
    void Warning(string message, Exception exception)
    // Log an exception with an associated message — same as Warning but with the exception first, matching the Serilog / Microsoft.Extensions.Logging idiom logger.LogWarning(ex, message).
    void Warning(Exception exception, string message)
    static void WriteErrorToConsole(string message)
    static void WriteToConsole(string message, ConsoleColor color)
    static void WriteWarningToConsole(string message)
    bool BlockWhenFull
    LogFilter ConsoleWriterFilter
    LogFilter FileWriterFilter
    LogFilter Filter
    // Optional prefix rendered at the very start of every console/file log line (before the timestamp). Because Log is an async-local instance, each isolated server scope (e.g. an embedded preview/sandbox server vs the host app) has its own instance and can carry its own prefix, making interleaved stdout from multiple in-process servers attributable at a glance.
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
  // Startup check for the shared secrets that authenticate machine-to-machine callers: the TURN shared secret, the relay agent token, and the host-server control token. Each of those checks used to fail open when its secret was empty — an unset TURN secret meant STUN MESSAGE-INTEGRITY was not verified at all, an unset control token left POST /servers and POST /drain unauthenticated, and an unset relay token let any agent claim public port allocations. One misconfigured deploy therefore turned a public listener into an open one, and nothing about the running process said so. A server now refuses to start instead. That is deliberately louder than accepting traffic it cannot authenticate: a process that will not come up is noticed, whereas an open port is not. Local development, which has no reason to hold real secrets, sets IKON_ALLOW_INSECURE_MACHINE_AUTH=true to keep the old behaviour — an explicit choice per run rather than a silent default.
  static class MachineAuthGuard
    // Whether the explicit local-development opt-out is set.
    static bool AllowsInsecureMachineAuth()
    // Whether a server holding secretValue for configFieldName may start. Logs either the refusal or the acknowledged insecure run, so the reason a process did or did not come up is always in the log.
    // serverName: The server refusing to start, for the log line.
    // configFieldName: The config field the operator has to set.
    // secretValue: The value as configured; null or empty is the failure case.
    static bool CanStartWithSecret(string serverName, string configFieldName, string? secretValue)
    // Opt back in to accepting unauthenticated machine callers. Intended for local runs only; it cannot be set by accident and every use is logged.
    const string AllowInsecureVariable
  // Provides optimized utility methods for converting strings between different naming conventions.
  static class NameConversions
    static string ToCamelCase(string input)
    static string ToDisplayName(string input)
    static string ToKebabCase(string input)
    static string ToPascalCase(string input)
    static string ToSlug(string input, int maxLength)
    static string ToSnakeCase(string input)
  // A claim on a set of ports that lasts until the server holding them has stopped, so two servers starting in the same process can never be handed the same port. HostUtils.FindAvailableTcpAndUdpPort answers "is this port free right now" by binding a probe socket and closing it again. Its caller binds for real some time later, and in that gap the port is free to everyone: two servers scanning the same range concurrently both see the default port unused, both pick it, and whichever binds second dies with "address already in use". The scan cannot fix this on its own — a per-call usedPorts set only de-duplicates within one caller. A lease closes the gap by keeping the chosen ports out of every other lease's scan for as long as the owner needs them. Take the ports through one lease per server, and dispose it when that server has released its sockets — never at the end of configuration, which would reopen the gap.
  sealed class PortLease : IDisposable
    ctor()
    // Release every port this lease holds. Call it once the owning server's sockets are closed; calling it while the server still listens hands its ports to the next scanner.
    void Dispose()
    // Claim the first port at or above startPort that is free on both TCP and UDP and not already leased in this process. The scan runs under the process-wide gate, so a concurrent lease cannot observe the same port as free.
    int Take(int startPort)
    // Claim a port that something else already chose — a relay agent's local port, a value from config — so later scans in this process skip it. Claiming one this lease already holds, or one another lease holds, is a no-op: a lease only ever releases what it added itself.
    void TakeSpecific(int port)
  // A reactive version of the protocol GlobalState. Each property is wrapped in a ReactiveT so that any UI binding to it will update only when the value changes.
  class ReactiveGlobalState
    ctor()
    // Unique identifier of the app session this server is serving; empty outside a cloud run
    Reactive<string> AppSessionId { get; }
    // Active audio streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.AudioStreamState>> AudioStreams { get; }
    // Keyed by client session id; each Context carries that client's user id, device, viewport, and locale.
    Reactive<Dictionary<int, Context>> Clients { get; }
    // Whether debug mode is enabled, providing additional logging and development features
    Reactive<bool> DebugMode { get; }
    // The current first human user; reassigned when that user leaves. Contrast PrimaryUserId, which is fixed.
    Reactive<string> FirstUserId { get; }
    // Registry of callable functions organized by client session ID
    Reactive<Dictionary<int, List<ActionFunctionRegister>>> Functions { get; }
    // Unique identifier of the specific Ikon server instance handling this session
    Reactive<string> IkonServerId { get; }
    // Display name of the organization
    Reactive<string> OrganisationName { get; }
    // The session owner from server config, fixed for the session's lifetime; used for user-specific asset storage paths.
    Reactive<string> PrimaryUserId { get; }
    // Tells whether the app is being run through publicly accessible endpoints (in local development)
    Reactive<bool> PublicAccess { get; }
    // Tells where the server is running from
    Reactive<ServerRunType> ServerRunType { get; }
    // Hash of the session identity values that this session was joined on
    Reactive<string> SessionIdentityHash { get; }
    // Full URL with session identifier for direct access to current session
    Reactive<string> SessionUrl { get; }
    // Unique identifier for the space where this session is running
    Reactive<string> SpaceId { get; }
    // Display name of the space
    Reactive<string> SpaceName { get; }
    // URL for accessing the app through its space domain
    Reactive<string> SpaceUrl { get; }
    // Active UI streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.UIStreamState>> UIStreams { get; }
    // Active video streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.VideoStreamState>> VideoStreams { get; }
    // Returns the client context against the clientSessionId, or null when no client with that session id is connected
    Context? GetClientContext(int clientSessionId)
    // Returns the first or null client context against the userId
    Context? GetClientContext(string userId)
    // Gets a collection of all human client contexts. This includes all clients whose UserType includes the Human flag.
    IEnumerable<Context> GetHumanClients()
    // Gets a collection of client contexts grouped by unique AuthSessionId. If a user has multiple clients, only the first one (by the iteration order) is returned.
    IEnumerable<Context> GetUniqueAuthClientContexts()
    // Gets a collection of client contexts grouped by unique AuthSessionId. If a user has multiple clients, only the first one (by the iteration order) is returned.
    IEnumerable<Context> GetUniqueHumanAuthClientContexts()
    // Updates the ReactiveGlobalState from a new GlobalState. Only those reactive properties that have actually changed will trigger notifications.
    void UpdateFrom(GlobalState newState)
  // A helper comparer to compare two dictionaries for equality by checking that they have the same keys and that the corresponding values are equal.
  class ReactiveGlobalState.DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
    ctor()
    bool Equals(Dictionary<TKey, TValue>? x, Dictionary<TKey, TValue>? y)
    int GetHashCode(Dictionary<TKey, TValue> obj)
    static readonly ReactiveGlobalState.DictionaryComparer<TKey, TValue> Instance
  // Same envelope as ConnectTokenCodec — see SignedTokenCodec — but a different key and a much shorter life. The gateway verifies locally, so a dial target never costs a backend round trip and a caller-supplied targetHost never enters the picture. The token authorizes a target; it does not authenticate a session. The ikon server still validates the connect token and the auth ticket, exactly as it does behind the on-host proxy.
  static class RouteTokenCodec
    static string Encode(RouteToken token, byte[] key)
    // The RouteToken.ExpiresAt value for a token minted now with this lifetime.
    static uint ExpiresIn(TimeSpan lifetime)
    // The port range is checked here rather than at the dial site so that every caller gets it. The range is the same one the on-host proxy confines auth-ticket ports to: a token is signed by us, but a bug that minted port 22 should still not become an SSRF.
    // nowUtc: Supplied rather than read here so tests can drive the expiry boundary.
    static RouteTokenStatus TryDecode(string presented, byte[] key, DateTime nowUtc, int portRangeStart, int portRangeEnd, out RouteToken? token)
    // How long a minted route token stays valid. Short, unlike the connect token: it is presented twice within one connect sequence — once at /connect and once when the WebSocket opens — and nothing replays it later. A client that sleeps and reconnects gets a fresh one from the backend along with everything else it re-fetches.
    static readonly TimeSpan DefaultLifetime
  // Why a presented route token was not accepted.
  enum RouteTokenStatus
    Valid
    // Not decodable at all — truncated, corrupt, or a token layout this build predates.
    Malformed
    // Decodable, but not signed with the route-token key. A forgery, or a token for another fleet.
    BadSignature
    // Correctly signed but past RouteToken.ExpiresAt.
    Expired
    // Signed, unexpired, and still unusable: no host, or a port outside the range the gateway may dial. Kept distinct because it means we minted something wrong, not that a caller tampered.
    InvalidTarget
  // Read-only view of the space-scoped secrets (tokens, API keys, passwords) loaded from the Ikon backend. Apps receive a Secrets via app.Secrets; pipelines receive one via host.Secrets on IPipelineHost<TConfig>. Manage values from the CLI with ikon app secret set/list/delete. Rotating a secret while an app or pipeline is running only takes effect after a restart.
  sealed class Secrets
    // Returns the value for key, or throws InvalidOperationException if no secret with that key is set for this space.
    string this[string key] { get; }
    // Keys of all secrets available for this space. Values are intentionally not exposed in bulk.
    IReadOnlyCollection<string> Keys { get; }
    // Non-throwing lookup. Returns true and sets value when the key exists; returns false and sets value to null otherwise.
    bool TryGet(string key, out string? value)
  class Sensitive<T>
    ctor(T value, SensitivityPolicy sensitivityPolicy = Default)
    bool IsSensitive { get; }
    SensitivityPolicy Policy { get; }
    T Value { get; }
  enum SensitivityPolicy
    Default
  // Turns the service token in IKON_SERVICE_TOKEN into a short-lived access token. A service token is what automation authenticates with: minted once by a signed-in person, stored in a CI secret, and presented unchanged on every run. It is deliberately not a refresh token — presenting a refresh token rotates it, which would invalidate the stored copy and make the next run's presentation look like theft — so it goes to /tool/exchange rather than /tool/refresh, and nothing about the credential changes. The access token it yields lives in this process only. It is never written to login.json: a CI runner's filesystem is shared with whatever else the job runs, the credential is good for an hour, and the store exists to remember a *person's* sign-in on a machine they own. Caching is per-process for the same reason a single verb only needs it once.
  static class ServiceTokenExchanger
    // The service token this process was given, or null when none was set.
    static string? GetServiceToken()
    // Credentials for environment obtained by exchanging the service token, or null when no service token is set or the exchange failed. Keyed by environment because one token is only ever valid for the environment that minted it: a development token presented to production is a 401, and caching one answer for both would turn that into the wrong credential rather than a clear failure. Synchronous, like TokenRenewer and for the same reason: every caller above it is synchronous, and every host behind it is a console process with no synchronization context.
    static IkonBackend.LoginInfo? TryExchange(IkonBackend.EnvironmentType environment)
    // The environment variable a CI job sets. Named for what it holds, not for where it goes.
    const string ServiceTokenVariable
  // The canonical routing-identity hash — the one function behind the wire SessionHash and the gateway's cell routing. Byte-compatible with the TypeScript backend's getHashFromObject (shared-nest): keys sorted by UTF-16 code units, values serialized with JSON.stringify escaping, SHA256, unpadded base64url. Conformance vectors generated from the Node implementation live in the test suite; any change here must keep them green on BOTH sides or routing identity drifts between the gateway and the runtime. This is the ROUTING identity hash only. Persisted-reactive storage partitioning uses its own historical hash (StorageStatePersistence) which must never change — it addresses stored data.
  static class SessionIdentityHash
    // Hash of an app session identity (the identity fields only).
    static string Compute(IReadOnlyDictionary<string, string> sessionIdentity)
    // Hash of a cell identity: the identity fields plus CellTypeKey. Identity KEYS must be the cell's SessionIdentity record parameter names VERBATIM (the bundle manifest's IdentityFields — e.g. "ChannelId", not "channelId"): the gateway keys the hashed object by the manifest field name regardless of the query-param casing the caller used. Verified against the production gateway.
    static string ComputeForCell(string cellType, IReadOnlyDictionary<string, string> sessionIdentity)
    // The key the gateway mixes into a CELL route's hashed object (cell-routing.ts CELL_TYPE_PARAM) so the same identity fields under different cell types hash apart.
    const string CellTypeKey
  // Conventions shared between the build-time boot-snapshot capture pipeline's three processes: the bundler tool (sets the environment variables), the server runner (drives the capture clients), and the app host (answers route enumeration). The env-var gate is the security boundary: the route-enumeration function must only exist in a capture process, never in production, because Context.IsSnapshot is client-controlled and cannot gate anything.
  static class SnapshotCapture
    // True when this process was launched for boot-snapshot capture.
    static bool IsCaptureProcess { get; }
    // Total capture budget across all routes; routes not captured in time are skipped.
    const int CaptureBudgetMs = 600000
    // Set to "1" by the bundler when launching the app for snapshot capture.
    const string EnabledEnvVar
    // Maximum number of routes captured after unioning static and dynamic routes. Variant skeletons are config-bounded separately (MaxVariants) and do not consume route slots.
    const int MaxRoutes = 50
    // Maximum number of variant skeletons captured per bundle.
    const int MaxVariants = 16
    // Client-side function an app calls to signal a route's view has settled.
    const string ReadyFunctionName
    // Per-route cap on waiting for the view to settle.
    const int RouteTimeoutMs = 10000
    // Path to a JSON file containing the statically declared routes to capture.
    const string RoutesEnvVar
    // App-side function the capture client calls to enumerate dynamic routes.
    const string RoutesFunctionName
    // Quiescence window: a route is considered settled after this many ms without a UI update. The app's ready signal (ReadyFunctionName) always wins the race when it arrives first.
    const int SettleMs = 750
  // The JSON contract of the file SnapshotCapture.RoutesEnvVar points at: the bundler writes it from the app's [BootSnapshot] config, the server runner reads it to drive the per-route capture loop.
  sealed class SnapshotRoutesFile
    ctor()
    // Statically declared routes to capture, before canonicalization.
    List<string> Routes { get; set; }
    // Distinct boot-snapshot variant ids referenced by the app's seed rules. Each is captured once as a skeleton render the app keys on Context.SnapshotVariant; the seed rules' axis and pattern mapping stay on the bundler side and never reach the capture process. Variant artifacts are never prerendered to HTML and never listed in the route manifest or sitemap.
    List<string> VariantIds { get; set; }
  // A reference to a Studio project as pasted by a user: either a bare space id or a full Studio project URL (https://host/app/{spaceId}/...). A URL also carries the backend environment, inferred from its host; a bare id carries none.
  readonly struct StudioProjectRef
    ctor(string spaceId, IkonBackend.EnvironmentType? environment)
    // Environment inferred from a URL host; null when the reference was a bare id.
    IkonBackend.EnvironmentType? Environment { get; }
    string SpaceId { get; }
    static StudioProjectRef Parse(string reference)
  // A UserException so the app developer sees the sentence and not a stack trace. Before this existed the backend threw a bare error, which arrived as a 500 and surfaced as an HttpRequestException carrying a URL, a status code and a response body — none of which say what to do about it. Hint names the command that does.
  sealed class TelephonyNumberNotAvailableException : UserException
    ctor(string friendlyMessage, string? number = null, string? hint = null)
    // What to do about it, as a command the developer can run.
    string? Hint { get; }
    // The number that was asked for, when the caller named one.
    string? Number { get; }
  // Rate-limits repeated calls to the same action, keyed by the action's declaring type and method name. Its purpose is keeping a hot path (a send loop, a per-message handler) from flooding the log with the same warning: wrap the log call and only the first one per interval gets through. Because the key is derived from the action's method, all call sites inside one method share a throttle bucket — pass a distinct extraKey when a method throttles more than one action. Buckets live for the lifetime of the process and are never evicted, so extraKey must come from a bounded set, never from unbounded data like a session or message id.
  static class Throttler
    // Runs action unless it already ran within the throttle interval.
    // action: The action to run at most once per interval. Its declaring type and method name form the throttle key.
    // throttleInterval: Minimum time between two runs of the same action. Defaults to 1 second when null.
    // extraKey: Distinguishes several throttled actions that share a declaring method. Must be from a bounded set — keys are never evicted.
    static bool TryExecute(Action action, TimeSpan? throttleInterval = null, string? extraKey = null)
  static class TokenRenewer
    static DateTimeOffset GetTokenExpiry(string token)
    // Whether the stored refresh token is still worth presenting, as far as the store itself knows. An entry with no recorded expiry — written before refreshExpiresAt was stored — counts as live. Redemption is what actually settles the question, and assuming the worst here would send a user through a sign-in on the strength of a field that was simply never written.
    static bool HasLiveRefreshToken(IkonBackend.LoginInfo? login)
    static bool IsRenewalDue(DateTimeOffset expiry, DateTimeOffset now)
    static Task<TokenRenewer.RenewalOutcome> RenewIfDueAsync(IkonBackend.EnvironmentType environment, CancellationToken cancellationToken)
    // Synchronous recovery for a caller that has just found an expired access token. Returns what happened, so the caller can tell a credential the user must replace from a rotation that merely did not happen this time. Blocking is what the callers want here: IkonBackend.Login is synchronous and every host behind it — the tool, the test base class, the server — is a console process with no synchronization context to deadlock against.
    static TokenRenewer.RenewalOutcome TryRecoverExpiredToken(IkonBackend.EnvironmentType environment)
    // Best-effort rotation for a caller whose access token is still valid but inside its last quarter. Returns the rotated store, or null when nothing rotated. Separate from TryRecoverExpiredToken because the two want opposite answers to the same question. Recovery has nothing to fall back on, so it retries and reports whether the store ended up usable — a store that was already fine counts as success. This runs while the caller holds a working token, so "already fine" is exactly the case it must not report as a rotation, and failing costs nothing: the token in hand still works, and expiry recovers on the next run.
    static IkonBackend.LoginInfo? TryRenewDueToken(IkonBackend.EnvironmentType environment)
  // What a renewal attempt did, and — when it did not rotate — whose problem that is. The distinction that matters is ChainExpired against Unavailable: only the first is the user's to fix. Collapsing the two is what had the tool answer every unreachable auth service, every throttled rotation and every lost store lock with "run 'ikon login'" — a browser sign-in for a credential that was still perfectly good.
  enum TokenRenewer.RenewalOutcome
    // The access token has plenty of life left; nothing was attempted.
    NotDue
    // The stored access token is live — rotated by this process, or by one it waited behind.
    Renewed
    // No saved login for this environment at all.
    NotSignedIn
    // A login with no refresh token: a `--backend-token` pair, or one predating the rotating flow.
    NoRefreshToken
    // The refresh chain is gone — revoked, detected as reused, or past its 90 days. Only signing in again fixes it.
    ChainExpired
    // Renewal did not happen for a reason that need not repeat: offline, throttled, or another process held the store lock.
    Unavailable
  static class Toml
    static T From<T>(string toml) where T : class, new()
    static string To<T>(T obj) where T : class
  // Exception for user-facing errors that should be displayed cleanly without stack traces. Use this for expected error conditions like invalid input, missing files, or failed operations.
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
    Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken = default)
    Task<IReadOnlyList<AssetUri>> ListAsync(AssetClass assetClass, string? prefix = null, CancellationToken cancellationToken = default)
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
  // Asset class determines which storage backend is used to store/retrieve the asset.
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
  // Serializes AssetUri as its canonical URI string so it round-trips correctly. Without this, System.Text.Json cannot reconstruct the immutable get-only struct and falls back to default(AssetUri) on deserialization (losing the path, class, and scope identifiers).
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
  // Sender or recipient entry parsed from an inbound email envelope.
  sealed record EmailAddress
    ctor(string Email, string? Name, string? Subaddress)
    string Email { get; init; }
    string? Name { get; init; }
    string? Subaddress { get; init; }
  // Represents a single attachment on an outgoing app email. Bytes is the raw binary content; the platform encodes it as base64 before sending it on the wire.
  sealed record EmailAttachment
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  // A streaming attachment download. The caller owns the Content stream; dispose this object (e.g. await using) to release it.
  sealed class EmailAttachmentDownload : IAsyncDisposable
    // Decrypted attachment bytes streamed from the platform.
    Stream Content { get; }
    // The sender-supplied filename, sanitized by the platform.
    string Filename { get; }
    // The attachment's MIME type, as recorded at ingest time.
    string MimeType { get; }
    // The decrypted (plaintext) attachment size in bytes.
    long Size { get; }
    ValueTask DisposeAsync()
  // A single SMTP header preserved on an inbound email.
  sealed record EmailHeader
    ctor(string Name, string Value)
    string Name { get; init; }
    string Value { get; init; }
  // Specification for a custom email sent by an app through the platform mailer. The platform enqueues the send for asynchronous delivery and returns once the request has been accepted; transient delivery failures are retried server-side.
  sealed record EmailSendRequest
    // To: Recipient email address.
    // Subject: Email subject line.
    // HtmlBody: Pre-rendered HTML body of the email.
    // TextBody: Optional plain-text fallback for clients that do not render HTML.
    // ReplyTo: Optional Reply-To address, for directing replies away from the From address.
    // Attachments: Optional list of binary attachments. Up to 10 per email.
    // Metadata: Optional string key/value pairs forwarded to the mail provider for tracking.
    // SenderLocalPart: Optional local part of the From address — the part before the @. The platform owns the domain and only ever uses one the space has verified for sending, so this cannot send from somewhere else. Lowercase letters, digits, dot, underscore and hyphen only, starting and ending alphanumeric, at most 64 characters; names belonging to the mail infrastructure (postmaster, abuse, mailer-daemon, …) are rejected. When the space has no verified sending domain the send fails with EmailSenderNotAvailableException — catch it and resend without the sender fields to deliver from the platform's own address.
    // SenderDisplayName: Optional display name shown beside the From address. Defaults to the space's own name. At most 64 characters, with line breaks and other header-unsafe characters rejected. Like SenderLocalPart, requires a verified sending domain.
    // SenderDomain: Optional sending domain for the From address, for a space with more than one verified sending domain. Must be one of the space's own verified sending domains; anything else fails the send with EmailSenderNotAvailableException. Left null, the platform picks the space's designated or best verified sending domain.
    ctor(string To, string Subject, string HtmlBody, string? TextBody = null, string? ReplyTo = null, IReadOnlyList<EmailAttachment>? Attachments = null, IReadOnlyDictionary<string, string>? Metadata = null, string? SenderLocalPart = null, string? SenderDisplayName = null, string? SenderDomain = null)
    // Optional list of binary attachments. Up to 10 per email.
    IReadOnlyList<EmailAttachment>? Attachments { get; init; }
    // Pre-rendered HTML body of the email.
    string HtmlBody { get; init; }
    // Optional string key/value pairs forwarded to the mail provider for tracking.
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    // Optional Reply-To address, for directing replies away from the From address.
    string? ReplyTo { get; init; }
    // Optional display name shown beside the From address. Defaults to the space's own name. At most 64 characters, with line breaks and other header-unsafe characters rejected. Like SenderLocalPart, requires a verified sending domain.
    string? SenderDisplayName { get; init; }
    // Optional sending domain for the From address, for a space with more than one verified sending domain. Must be one of the space's own verified sending domains; anything else fails the send with EmailSenderNotAvailableException. Left null, the platform picks the space's designated or best verified sending domain.
    string? SenderDomain { get; init; }
    // Optional local part of the From address — the part before the @. The platform owns the domain and only ever uses one the space has verified for sending, so this cannot send from somewhere else. Lowercase letters, digits, dot, underscore and hyphen only, starting and ending alphanumeric, at most 64 characters; names belonging to the mail infrastructure (postmaster, abuse, mailer-daemon, …) are rejected. When the space has no verified sending domain the send fails with EmailSenderNotAvailableException — catch it and resend without the sender fields to deliver from the platform's own address.
    string? SenderLocalPart { get; init; }
    // Email subject line.
    string Subject { get; init; }
    // Optional plain-text fallback for clients that do not render HTML.
    string? TextBody { get; init; }
    // Recipient email address.
    string To { get; init; }
  // Rules for the sender identity fields on an outgoing app email — the local part alphabet and the names the mail infrastructure keeps for itself. Checking against these before sending turns a rejection from the platform into an immediate, local error.
  static class EmailSenderIdentity
    // Whether a normalized local part is one of the names reserved for the mail infrastructure.
    static bool IsReservedLocalPart(string localPart)
    // Whether a normalized local part matches the alphabet the platform accepts.
    static bool IsValidLocalPart(string localPart)
    // Trims and lowercases a local part the way the backend does before validating. Returns null when nothing remains.
    static string? NormalizeLocalPart(string? localPart)
    const int MaxDisplayNameCodePoints = 64
    const int MaxLocalPartLength = 64
  // Lightweight metadata for an inbound email's attachment — does not include the body bytes. Fetch the body via the email service's DownloadAttachmentAsync.
  sealed record InboundAttachmentInfo
    ctor(string Id, string Filename, string MimeType, long Size)
    string Filename { get; init; }
    string Id { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
  // Full inbound email with decrypted body and parsed envelope. Attachments expose metadata only; fetch each one via the email service's DownloadAttachmentAsync.
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
  // Inbox-listing entry. Subject is decrypted server-side; body and attachment bytes are not included here — call EmailService.GetMessageAsync for the full message.
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
  // One page of inbox results. NextCursor is null when there are no more pages.
  sealed record InboxPage
    ctor(IReadOnlyList<InboundEmailSummary> Items, string? NextCursor)
    IReadOnlyList<InboundEmailSummary> Items { get; init; }
    string? NextCursor { get; init; }
  // Filter and pagination parameters for an inbox listing.
  sealed record InboxQuery
    ctor()
    // Opaque cursor returned by a previous InboxPage.NextCursor. null requests the first page.
    string? Cursor { get; init; }
    // Filter to messages sent from this address. Case-insensitive.
    string? From { get; init; }
    // Maximum number of messages to return for this page. The platform clamps to [1, 100]; values outside that range are silently adjusted. Defaults to 25.
    int Limit { get; init; }
    // Filter to messages delivered to this recipient address. Case-insensitive.
    string? Recipient { get; init; }
    // Inclusive lower bound on the SMTP receive timestamp.
    DateTimeOffset? Since { get; init; }
    // Inclusive upper bound on the SMTP receive timestamp.
    DateTimeOffset? Until { get; init; }

namespace Ikon.Common.Core.Functions
  // The type of callback a registered function uses.
  enum CallbackType
    // Synchronous callback returning a value directly.
    Sync
    // Asynchronous callback returning Task or Task<T>.
    Async
    // Async enumerable callback returning IAsyncEnumerable<T>.
    AsyncEnumerable
  // Immutable representation of a function with metadata and optional callbacks. Consolidates FunctionInfo, RegisteredFunction, and KernelContext.Function into a single type.
  readonly struct Function
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
    Ikon.Common.Core.Functions.FunctionParameter[] Parameters { get; }
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
    override string ToString()
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
    override object TypeId { get; }
    // Whether the function should be distributed to other clients. If not set, defaults to Local for standalone functions, or inherits from [RegisterAll] for methods in a class with that attribute.
    FunctionVisibility Visibility { get; set; }
  // Per-call ambient context exposed to the body of a function dispatched by FunctionRegistry. Set by the registry's inbound dispatch path before invoking the function and cleared after.
  static class FunctionCallContext
    // The session id of the client that issued the current function call, or null when the call did not originate from a remote client (e.g. local in-process invocation).
    static int? CallerSessionId { get; }
  sealed class FunctionCallException : Exception
    ctor(string message, string remoteTypeName, string remoteStackTrace)
    ctor(string message, string remoteTypeName, string remoteStackTrace, Exception? innerException)
    string RemoteStackTrace { get; }
    string RemoteTypeName { get; }
    const string RemoteFunctionCallerNotSetTypeName
  // Metadata about a function parameter.
  readonly struct FunctionParameter
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
  class FunctionRegistry : AsyncLocalInstance<FunctionRegistry>
    ctor()
    // Optional resolver that maps a caller session id to the auth session id — a per-login correlation identifier, not an authentication flag (cloud logins, including anonymous ones, always carry one). For guest detection use IsAnonymousResolver.
    Func<int, string?>? AuthSessionIdResolver { get; set; }
    // The version of the live/current registered implementation. When set, a caller that sends no version resolves to this version's functions instead of the greatest registered version. Hosts serving multiple versions side by side (e.g. the Ikon.AI library) set this so unversioned callers always reach the current build — in a local/dev build the current version is stamped low (1.0.0) and would otherwise lose to a higher-numbered preserved snapshot. Null keeps the greatest fallback.
    string? CurrentVersion { get; set; }
    // All registered functions grouped by name.
    IReadOnlyDictionary<string, IReadOnlyList<Function>> Functions { get; }
    // Optional resolver that maps a caller session id to whether the caller is an anonymous (guest) user: true for a guest, false for an authenticated user or machine, null for unknown sessions. Wired by the host (e.g. Ikon.App.App) from Context.IsAnonymous; consumed by LoggedInPolicy.
    Func<int, bool?>? IsAnonymousResolver { get; set; }
    // True while this call stack is executing a function on behalf of a remote caller. Flow-local, so concurrent local calls are unaffected.
    static bool IsExecutingRemoteCall { get; }
    // When set, the dispatcher rejects any remote call whose restored scopes carry no BackendTokenScope with a space claim. Turned on by delegating proxy hosts (e.g. the Ikon.AI library) that make platform-key calls on behalf of a caller and must never execute for an unidentified caller. Off by default so ordinary RPC hosts are unaffected.
    bool RequireVerifiedCallerSpace { get; set; }
    // Optional resolver that maps a caller session id to the set of roles the caller holds. Wired by the host (e.g. Ikon.App.App) so that RequireRoleAttribute / RoleBasedPolicy can gate calls. Returns an empty/null collection for callers without any roles. The dispatcher copies the result into PolicyCallContext.AdditionalContext under the key RoleBasedPolicy.RolesContextKey.
    Func<int, IReadOnlyCollection<string>?>? RolesResolver { get; set; }
    // Optional resolver that maps a caller session id to the reactive scopes that should be active during the function body's execution — typically [ClientScope, UserScope] derived from the caller's Context. Wired by the host (e.g. Ikon.App.App) so that ClientReactive<T> and UserReactive<T> resolve naturally without the function body having to push scopes manually via FunctionCallContext.CallerSessionId + ReactiveScope.Use.
    Func<int, IReadOnlyList<IScopeKey>>? ScopeResolver { get; set; }
    // Optional resolver that maps a caller session id to the user id associated with that session. Wired by the host (e.g. Ikon.App.App) so that policy evaluation has access to the caller's identity. Returns null for unknown sessions or unauthenticated (guest) callers.
    Func<int, string?>? UserIdResolver { get; set; }
    void AddFunction(Function function, FunctionVisibility? visibilityOverride = null)
    // Hooks the registry to a protocol channel so that remote function calls and registrations are handled automatically.
    Task AttachProtocolAsync(IProtocolMessageChannel channel, int senderId)
    TResult Call<TResult>(string name, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    // The async overloads take cancellationToken second and args third, unlike the synchronous Call<TResult> which takes args second. Pass the arguments by name when omitting the token: CallAsync<int>("Add", args: [2, 3]).
    Task<TResult> CallAsync<TResult>(string name, CancellationToken cancellationToken = default, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    Task CallAsync(string name, CancellationToken cancellationToken = default, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    IAsyncEnumerable<TItem> CallAsyncEnumerable<TItem>(string name, CancellationToken cancellationToken = default, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
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
    Function? GetFunction(string name, IReadOnlyList<Ikon.Common.Core.Protocol.FunctionParameter> protocolParameters)
    // Gets a local function with the given name and version, using protocol parameter type names to resolve overloads. If version is non-empty, tries exact version match first, then falls back to greatest version. If version is empty, selects the greatest versioned function or falls back to unversioned.
    Function? GetFunction(string name, IReadOnlyList<Ikon.Common.Core.Protocol.FunctionParameter> protocolParameters, string version)
    // Gets a function with the given name from a specific client session.
    Function? GetFunction(string name, int clientSessionId)
    // Gets all functions with the given name.
    IReadOnlyList<Function> GetFunctions(string name)
    // Checks if a function with the given name exists.
    bool HasFunction(string name)
    // Checks if a function with the given name exists for a specific client session.
    bool HasFunction(string name, int clientSessionId)
    // Invoke an already-resolved local function with a pre-built positional argument array, bypassing the argument-type resolution that CallAsync performs. The args must already line up with the function's parameter list — used by callers that inject host-supplied parameters (e.g. a cron trigger building the array from Function.MethodInfo to inject a context object). Returns the result, if any.
    Task<object?> InvokeLocalAsync(Function function, object?[] args)
    // Scans an assembly for types with [RegisterAll] or methods with [Function] attributes and registers them.
    void RegisterFromAssembly(Assembly assembly, FunctionVisibility? visibilityOverride = null, string? version = null)
    // Scans an instance for [RegisterAll] attribute or methods with [Function] attribute and registers them.
    void RegisterFromInstance(object instance, FunctionVisibility? visibilityOverride = null, string? version = null)
    // Scans a type for [RegisterAll] attribute or methods with [Function] attribute and registers them. For instance methods, you need to use RegisterFromInstance instead.
    void RegisterFromType<T>(FunctionVisibility? visibilityOverride = null, string? version = null)
    // Scans a type for [RegisterAll] attribute or methods with [Function] attribute and registers them. For instance methods, you need to use RegisterFromInstance instead.
    void RegisterFromType(Type type, FunctionVisibility? visibilityOverride = null, string? version = null)
    // Registers a single method as a function unless one is already registered under the same name. Used by the app layer to register [Cron] methods, which are registrable like [Function] even when they carry no [Function] attribute. Idempotent: a method already registered (e.g. because it also carries [Function] under the same name) is left untouched. When name is null or empty the full member name ("{Type.FullName}.{Method}") is used.
    void RegisterFunctionMethod(object instance, MethodInfo method, string? name = null, FunctionVisibility visibility = Local)
    void RegisterFunctionsFromClientInitialization(ClientInitialization? clientInitialization)
    // Registers a remote function (from another client via protocol).
    void RegisterRemoteFunction(Guid id, string name, Ikon.Common.Core.Functions.FunctionParameter[] parameters, Type returnType, string description, FunctionVisibility visibility, bool llmInlineResult, bool llmCallOnlyOnce, CallbackType callbackType, int clientSessionId, bool requiresInstance = false)
    bool RemoveFunction(string name, FunctionVisibility visibility)
    // Removes all local functions with the given name. Remote functions with the same name are preserved. Returns true if any functions were removed.
    bool RemoveFunction(string name)
    // Removes all functions registered by a specific client session (when client disconnects).
    void RemoveFunctionsByClientSessionId(int clientSessionId)
    // TODO(frozen-abi): exists only to maintain the shim above; delete with it.
    static void RemoveRemoteCallExecutionStartingSubscribers(AssemblyLoadContext loadContext)
    // Sends registrations for all functions and processes pending registrations.
    Task StartProtocolAsync()
    // Stops protocol handling but keeps the channel attached. Pending registrations are cleared.
    Task StopProtocolAsync()
    void SyncFunctionsFromGlobalState(GlobalState globalState)
    // Tries to get the single function registered under name, returning false rather than throwing when the name is unknown or resolves ambiguously (multiple overloads or multiple remote clients). Use GetFunction to resolve an overload by argument types.
    bool TryGetFunction(string name, out Function? function)
    // Waits for a function with the given name to be registered.
    // functionName: Name of the function to wait for.
    // timeout: How long to wait before giving up. Defaults to 30 seconds when null.
    // ct: Cancellation token.
    Task<bool> WaitForFunctionAsync(string functionName, TimeSpan? timeout = null, CancellationToken ct = default)
    // Fired when an approval flow completes (approved or rejected). Use this event for audit logging of approval decisions.
    event Action<ApprovalAuditEntry>? ApprovalCompleted
    // Fired when all of a client session's functions are removed because it disconnected (RemoveFunctionsByClientSessionId). Lets services that track per-session state — e.g. ReactiveSubscriptionService's subscriber set — release it promptly instead of discovering the dead session only when a later push fails.
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
  // Determines whether a function is advertised over the protocol so remote clients can call it. This is a dispatch-scope axis only — auth gating is a separate concern declared via policy attributes ([RequireLogin], [AllowAnonymous], [RequireRole], ...).
  enum FunctionVisibility
    // Function is not advertised. Only callable within the server process.
    Local
    // Function is advertised over the protocol; remote clients can call it. External functions must declare their auth posture with [RequireLogin] or [AllowAnonymous] — a startup audit warns when neither is present.
    External
  sealed class InstanceNotFoundException : Exception
    ctor(Guid instanceId)
    Guid InstanceId { get; }
  // Marks a class for automatic registration of all public members (methods, properties, constructors). Used for auto-registration via RegisterFromInstance/RegisterFromType/RegisterFromAssembly. Function names are automatically generated using the full type name (e.g., Namespace.Class.MethodName). Individual members can use [Function] to override defaults.
  class RegisterAllAttribute : Attribute
    ctor()
    // If true, the LLM can only call each function once per generation pass. Individual members can override this with [Function].
    bool LlmCallOnlyOnce { get; set; }
    // If true, the LLM can inline function results directly without tool call overhead. Individual members can override this with [Function].
    bool LlmInlineResult { get; set; }
    // Whether the functions should be distributed to other clients. Default is Local (not distributed).
    FunctionVisibility Visibility { get; set; }

namespace Ikon.Common.Core.Functions.Policy
  // Use this on framework-shipped or genuinely public endpoints where capability is provided by something other than session auth (e.g. a stableId, a webhook signature, or the endpoint being read-only public). Pair with explicit [RateLimit] when abuse is a concern.
  sealed class AllowAnonymousAttribute : Attribute
    ctor()
  // Represents an audit log entry for an approval decision.
  sealed class ApprovalAuditEntry
    // Creates a new approval audit entry.
    ctor(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, bool approved, string? reason, string policyName, DateTimeOffset timestamp)
    // The unique identifier for this approval request.
    Guid ApprovalId { get; }
    // True if the approval was granted; false if rejected.
    bool Approved { get; }
    // The session ID of the approver who responded to the request.
    int ApproverSessionId { get; }
    // The user ID of the approver, if available.
    string? ApproverUserId { get; }
    // The unique identifier for the function call that required approval.
    Guid CallId { get; }
    // The name of the function that required approval.
    string FunctionName { get; }
    // The name of the policy that required approval.
    string PolicyName { get; }
    // The reason for rejection, if rejected.
    string? Reason { get; }
    // The timestamp when the approval decision was made.
    DateTimeOffset Timestamp { get; }
    // Creates an audit entry for an approved request.
    static ApprovalAuditEntry CreateApproved(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, string policyName)
    // Creates an audit entry for a rejected request.
    static ApprovalAuditEntry CreateRejected(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, string? reason, string policyName)
  // Context passed to approval handlers containing all information needed to process an approval request.
  sealed class ApprovalContext
    // Public identifier for this approval request. Can be shared with callers to track which approval they're waiting for.
    Guid ApprovalId { get; }
    // Hash of the secret token that must be echoed back by the approver. The raw token is only provided to the designated approver via protocol.
    string ApprovalTokenHash { get; }
    // The arguments being passed to the function.
    object?[] Args { get; }
    // Hash of the serialized arguments, used for token binding.
    string ArgsHash { get; }
    // The original policy call context.
    PolicyCallContext CallContext { get; }
    // The session ID of the original caller.
    int CallerSessionId { get; }
    // The time when this approval request expires.
    DateTimeOffset ExpiresAt { get; }
    // The name of the function requiring approval.
    string FunctionName { get; }
    // The reason why approval is required.
    string Reason { get; }
    // The timeout in seconds for the approval request. Always at least PolicyDecision.MinExpirySeconds (30 seconds).
    int TimeoutSeconds { get; }
    // Creates a new ApprovalContext with generated IDs and returns both the context and the raw token. The raw token should only be sent to the designated approver.
    // functionName: The name of the function requiring approval.
    // reason: The reason why approval is required.
    // args: The arguments being passed to the function.
    // callContext: The original policy call context.
    // timeoutSeconds: The timeout in seconds (minimum 30).
    static (ApprovalContext Context, Guid RawToken) Create(string functionName, string reason, object?[] args, PolicyCallContext callContext, int timeoutSeconds = 300)
    // Checks if this approval request has expired.
    bool IsExpired()
    // Validates that a provided token matches this context. Uses constant-time comparison of hashes to prevent timing attacks.
    // providedToken: The token GUID provided by the approver.
    bool ValidateToken(Guid providedToken)
    // Validates that a provided token string matches this context.
    // providedToken: The token string provided by the approver.
    bool ValidateToken(string providedToken)
  // Delegate type for approval handlers that process approval requests.
  delegate ApprovalHandlerDelegate
    Task<ApprovalResult> ApprovalHandlerDelegate(ApprovalContext context)
  // The result of an approval request returned by approval handlers.
  readonly struct ApprovalResult
    // True if the request was approved.
    bool IsApproved { get; }
    // The reason for rejection, if applicable.
    string? RejectionReason { get; }
    // Creates an approved result.
    static ApprovalResult Approved()
    // Creates a rejected result with an optional reason.
    // reason: The reason for rejection.
    static ApprovalResult Rejected(string? reason = null)
    override string ToString()
  // Specifies who should receive the approval request.
  enum ApproverType
    // The approval request is sent to the original caller.
    Caller
    // The approval request is sent to a specific client.
    SpecificClient
    // The approval request is sent to a specific user's active client(s).
    SpecificUser
  // Interface for function policies that can be evaluated before function execution.
  interface IFunctionPolicy
    // The name of this policy (used for logging and error messages).
    string Name { get; }
    // The priority of this policy. Lower values are evaluated first. Default priority is 100.
    virtual int Priority { get; }
    // Evaluates the policy for a function call.
    // args: The arguments being passed to the function.
    // context: The policy call context with metadata about the call.
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
  // Interface for checking usage limits before function execution.
  interface IUsageLimitChecker
    // Checks if the call should be allowed based on usage limits.
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
  // A policy that maintains separate rate limits per caller session.
  sealed class PerSessionRateLimitPolicy : IFunctionPolicy
    // Creates a new per-session rate limit policy.
    // limit: Maximum number of calls allowed per session in the window.
    // windowSeconds: The time window in seconds.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(int limit, int windowSeconds, string? name = null, int priority = 50)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a PolicyDelegate from this policy.
    PolicyDelegate ToDelegate()
  // Helper methods for extracting typed arguments from policy evaluation arguments.
  static class PolicyArgs
    // Checks if all required arguments are present at the specified indices.
    // args: The arguments array.
    // requiredIndices: The indices that must have non-null values.
    static bool HasAll(object?[] args, params int[] requiredIndices)
    // Gets an optional argument at the specified index, returning a default if missing.
    // args: The arguments array.
    // index: The zero-based index of the argument.
    // defaultValue: The default value to return if the argument is missing or null.
    static T? Optional<T>(object?[] args, int index, T? defaultValue = default)
    // Gets a required argument at the specified index, throwing if missing or wrong type.
    // args: The arguments array.
    // index: The zero-based index of the argument.
    // throws PolicyDeniedException: Thrown if the argument is missing, null, or wrong type.
    static T Required<T>(object?[] args, int index)
    // Tries to get an argument at the specified index.
    // args: The arguments array.
    // index: The zero-based index of the argument.
    // value: The output value if successful.
    static bool TryGet<T>(object?[] args, int index, out T? value)
  // Base class for policy attributes that can be applied to functions.
  abstract class PolicyAttribute : Attribute
    // The priority of this policy. Lower values are evaluated first.
    int Priority { get; set; }
    // Creates a policy instance from this attribute.
    abstract IFunctionPolicy CreatePolicy()
  // Applies a custom policy class to the function.
  sealed class PolicyAttribute<TPolicy> : PolicyAttribute where TPolicy : IFunctionPolicy, new()
    ctor()
    override IFunctionPolicy CreatePolicy()
  // Rich context object for policy evaluation providing access to all relevant information about the function call being evaluated.
  sealed class PolicyCallContext
    ctor(Guid callId, string functionName, int callerSessionId, string? userId, string? tenantId, Guid? instanceId, bool isInternal, CancellationToken cancellationToken, string? authSessionId = null, bool? isAnonymous = null, DateTime? callTimestamp = null, IReadOnlyDictionary<string, object?>? additionalContext = null)
    // Additional context data that may have been provided with the call.
    IReadOnlyDictionary<string, object?>? AdditionalContext { get; }
    // The auth session ID of the caller, if available. A per-login correlation identifier, not an authentication flag — use IsAnonymous for guest detection.
    string? AuthSessionId { get; }
    // The unique identifier for this function call.
    Guid CallId { get; }
    // The timestamp when the call was initiated.
    DateTime CallTimestamp { get; }
    // The session ID of the caller.
    int CallerSessionId { get; }
    // The cancellation token for this call.
    CancellationToken CancellationToken { get; }
    // The name of the function being called.
    string FunctionName { get; }
    // The instance ID if this is a call on a specific instance.
    Guid? InstanceId { get; }
    // Whether the caller is an anonymous (guest) user: true for a guest, false for an authenticated user or machine, null when unknown (no resolvable client context for the caller session).
    bool? IsAnonymous { get; }
    // True if this call originated from the same process (internal call).
    bool IsInternal { get; }
    // The tenant ID, if available.
    string? TenantId { get; }
    // The user ID of the caller, if available.
    string? UserId { get; }
  // Provides utilities for composing multiple policies into a single policy.
  static class PolicyChain
    // Creates a policy that requires all provided policies to allow. Policies are evaluated in priority order (lower priority = evaluated first). Evaluation stops at the first non-Allow decision.
    // policies: The policies to chain together.
    static IFunctionPolicy All(params IFunctionPolicy[] policies)
    // Creates a PolicyDelegate that requires all provided policies to allow.
    // policies: The policies to chain together.
    static PolicyDelegate AllAsDelegate(params IFunctionPolicy[] policies)
  // Represents a policy decision about whether a function call should be allowed. This is a discriminated union with three possible states: Allow, Deny, or NeedsApproval. Use pattern matching to handle the different cases.
  abstract class PolicyDecision
    // Creates an Allow decision.
    static PolicyDecision Allowed()
    // Creates a Deny decision with a reason and optional error code.
    // reason: The reason for denying the function call.
    // code: Optional error code for programmatic handling.
    static PolicyDecision Denied(string reason, string? code = null)
    // Creates a RequireApproval decision with default expiry.
    // message: The message explaining why approval is required.
    static PolicyDecision RequireApproval(string message)
    // Creates a RequireApproval decision with custom expiry.
    // message: The message explaining why approval is required.
    // expirySeconds: How long the approval request is valid (minimum 30 seconds).
    static PolicyDecision RequireApproval(string message, int expirySeconds)
    // Creates a RequireApproval decision with a custom approval handler.
    // message: The message explaining why approval is required.
    // handler: The custom handler to process the approval request.
    static PolicyDecision RequireApproval(string message, ApprovalHandlerDelegate handler)
    // Creates a RequireApproval decision with custom expiry and handler.
    // message: The message explaining why approval is required.
    // expirySeconds: How long the approval request is valid (minimum 30 seconds).
    // handler: The custom handler to process the approval request.
    static PolicyDecision RequireApproval(string message, int expirySeconds, ApprovalHandlerDelegate handler)
    // Default expiry time for approval requests in seconds.
    const int DefaultExpirySeconds = 300
    // Minimum expiry time for approval requests in seconds.
    const int MinExpirySeconds = 30
  // Represents a decision to allow the function call to proceed.
  sealed class PolicyDecision.Allow : PolicyDecision
  // Represents a decision to deny the function call.
  sealed class PolicyDecision.Deny : PolicyDecision
    // Optional error code for programmatic handling (e.g., "rate_limit_exceeded").
    string? Code { get; }
    // The reason for denying the function call.
    string Reason { get; }
  // Represents a decision that requires approval before the function can execute.
  sealed class PolicyDecision.NeedsApproval : PolicyDecision
    // How long the approval request is valid, in seconds (minimum 30, default 300).
    int ExpirySeconds { get; }
    // Optional custom handler for processing the approval request.
    ApprovalHandlerDelegate? Handler { get; }
    // The message explaining why approval is required.
    string Message { get; }
  // Delegate type for policy evaluation.
  delegate PolicyDelegate
    ValueTask<PolicyDecision> PolicyDelegate(object?[] args, PolicyCallContext context)
  // Exception thrown when a function call is denied by a policy.
  sealed class PolicyDeniedException : Exception
    // Creates a new PolicyDeniedException with just a reason.
    // reason: The reason for denying the call.
    ctor(string? reason)
    // Creates a new PolicyDeniedException with a reason and error code.
    // reason: The reason for denying the call.
    // code: Error code for programmatic handling (e.g., "rate_limit_exceeded", "bad_args").
    ctor(string? reason, string? code)
    // Creates a new PolicyDeniedException with an error code, policy name, and function name.
    // reason: The reason for denying the call.
    // code: Optional error code for programmatic handling.
    // policyName: The name of the policy that denied the call.
    // functionName: The name of the function that was denied.
    ctor(string? reason, string? code, string? policyName, string? functionName)
    // Creates a new PolicyDeniedException with an inner exception.
    ctor(string? reason, Exception innerException, string? policyName = null, string? functionName = null)
    // Creates a new PolicyDeniedException with an error code and inner exception.
    ctor(string? reason, string? code, Exception innerException, string? policyName = null, string? functionName = null)
    // Optional error code for programmatic handling (e.g., "rate_limit_exceeded", "approval_rejected").
    string? Code { get; }
    // The name of the function that was denied.
    string? FunctionName { get; }
    // The name of the policy that denied the call.
    string? PolicyName { get; }
  // Contains the complete result of evaluating a function's policy.
  sealed class PolicyEvaluationResult
    ctor(PolicyDecision decision, string functionName, Guid callId, string? decidingPolicyName, TimeSpan evaluationDuration)
    // The call ID of the function call that was evaluated.
    Guid CallId { get; }
    // The name of the policy that caused a Deny or RequireApproval decision. Null if the decision is Allow.
    string? DecidingPolicyName { get; }
    // The final policy decision.
    PolicyDecision Decision { get; }
    // Time taken to evaluate the policy.
    TimeSpan EvaluationDuration { get; }
    // The name of the function that was evaluated.
    string FunctionName { get; }
    // True if the decision allows the function call to proceed.
    bool IsAllowed { get; }
    // True if the decision denies the function call.
    bool IsDenied { get; }
    // True if the decision requires approval before proceeding.
    bool RequiresApproval { get; }
    // Creates an Allow result (used when no policy is attached to a function).
    static PolicyEvaluationResult Allowed(string functionName, Guid callId)
    // Creates a Denied result.
    static PolicyEvaluationResult Denied(string functionName, Guid callId, string? reason, string policyName, TimeSpan evaluationDuration)
    // Creates a Denied result with an error code.
    static PolicyEvaluationResult Denied(string functionName, Guid callId, string reason, string? code, string policyName, TimeSpan evaluationDuration)
    // Creates a RequiresApproval result.
    static PolicyEvaluationResult NeedsApproval(PolicyDecision decision, string functionName, Guid callId, string policyName, TimeSpan evaluationDuration)
    override string ToString()
  // Non-generic version of PolicyAttribute for use when generic attributes are not supported.
  sealed class PolicyTypeAttribute : PolicyAttribute
    // Creates a new policy type attribute.
    // policyType: The type of policy to create. Must implement IFunctionPolicy and have a parameterless constructor.
    ctor(Type policyType)
    // The type of policy to create.
    Type PolicyType { get; }
    override IFunctionPolicy CreatePolicy()
  // Applies a rate limit policy to the function.
  sealed class RateLimitAttribute : PolicyAttribute
    // Creates a new rate limit attribute.
    // limit: Maximum number of calls allowed in the window.
    // windowSeconds: The time window in seconds.
    ctor(int limit, int windowSeconds)
    // Maximum number of calls allowed in the window.
    int Limit { get; }
    // If true, rate limit is per-session. If false (default), it's global.
    bool PerSession { get; set; }
    // The time window in seconds.
    int WindowSeconds { get; }
    override IFunctionPolicy CreatePolicy()
  // A policy that limits the rate of function calls.
  sealed class RateLimitPolicy : IFunctionPolicy
    // Creates a new rate limit policy.
    // limit: Maximum number of calls allowed in the window.
    // windowSeconds: The time window in seconds.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(int limit, int windowSeconds, string? name = null, int priority = 50)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a PolicyDelegate from this policy.
    PolicyDelegate ToDelegate()
  // Requires approval before the function can execute.
  sealed class RequireApprovalAttribute : PolicyAttribute
    // Creates a new require approval attribute.
    ctor()
    // The type of approver to ask.
    ApproverType ApproverType { get; set; }
    // The client session ID to ask for approval (only used when ApproverType is SpecificClient).
    int ClientSessionId { get; set; }
    // The reason why approval is required.
    string Reason { get; set; }
    // The user ID to ask for approval (only used when ApproverType is SpecificUser).
    string? UserId { get; set; }
    override IFunctionPolicy CreatePolicy()
  // A policy that always requires approval before the function can execute.
  sealed class RequireApprovalPolicy : IFunctionPolicy
    // Creates a new require approval policy that asks the caller for approval.
    // reason: The reason why approval is required.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(string reason, string? name = null, int priority = 100)
    // Creates a new require approval policy with a custom approval handler.
    // reason: The reason why approval is required.
    // handler: The custom approval handler.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(string reason, ApprovalHandlerDelegate handler, string? name = null, int priority = 100)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a new require approval policy that asks a specific client.
    // reason: The reason why approval is required.
    // clientSessionId: The client session ID to ask for approval.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    static RequireApprovalPolicy ForClient(string reason, int clientSessionId, string? name = null, int priority = 100)
    // Creates a new require approval policy that asks a specific user.
    // reason: The reason why approval is required.
    // userId: The user ID to ask for approval.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    static RequireApprovalPolicy ForUser(string reason, string userId, string? name = null, int priority = 100)
    // Creates a PolicyDelegate from this policy.
    PolicyDelegate ToDelegate()
  // Guest (anonymous) callers are denied with the "login_required" error code. The Ikon client runtime intercepts this and triggers the deferred-login flow.
  sealed class RequireLoginAttribute : PolicyAttribute
    ctor()
    override IFunctionPolicy CreatePolicy()
  // Internal callers (PolicyCallContext.IsInternal) bypass the check — same as LoggedInPolicy — because in-process callers are already trusted.
  sealed class RequireRoleAttribute : PolicyAttribute
    ctor(params string[] roles)
    // When false (default), the caller passes if they hold ANY of the listed roles. When true, the caller must hold ALL listed roles.
    bool RequireAll { get; set; }
    // The roles the caller must hold (any or all, see RequireAll).
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
  // Applies a usage limit policy to the function.
  sealed class UsageLimitAttribute : PolicyAttribute
    // Creates a new usage limit attribute with the specified checker type.
    // checkerType: The type of checker to use. Must implement IUsageLimitChecker.
    ctor(Type checkerType)
    // The type of usage limit checker to use. Must implement IUsageLimitChecker and have a parameterless constructor.
    Type CheckerType { get; }
    override IFunctionPolicy CreatePolicy()
  // Result of a usage limit check.
  sealed class UsageLimitCheckResult
    // Whether the call is allowed.
    bool Allowed { get; }
    // The error code for denial (if not allowed).
    string? DenyCode { get; }
    // The reason for denial (if not allowed).
    string? DenyReason { get; }
    // Creates an allow result.
    static UsageLimitCheckResult Allow()
    // Creates a deny result with the specified reason and code.
    // reason: The reason for denial.
    // code: The error code (defaults to "usage_limit_exceeded").
    static UsageLimitCheckResult Deny(string reason, string? code = "usage_limit_exceeded")
  // A policy that checks for available credits/quota before execution.
  sealed class UsageLimitPolicy : IFunctionPolicy
    // Creates a new usage limit policy with the specified checker.
    // checker: The checker to use for evaluating usage limits.
    // name: Optional policy name.
    // priority: Policy evaluation priority (lower = earlier).
    ctor(IUsageLimitChecker checker, string? name = null, int priority = 10)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a PolicyDelegate from this policy.
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
    // Takes over the retired-key values of source, replacing any held here. Call it after cloning this type by any route other than Teleport: the bag is private, so a JSON round trip or a hand-written copy drops it and the clone stops emitting retired fields.
    void CopyRetiredFieldsFrom(AuthResponse source)
    // The bag to populate before writing: setting a member here makes the writer emit the retired field under its original id during its sunset window, so readers that still resolve the old name keep seeing it.
    AuthResponse.RetiredFields GetOrCreateRetiredFields()
    // The retired-key values the last read captured, or null when none were seen. A method rather than a property so TOML mapping and JSON serialization never treat the bag as data.
    AuthResponse.RetiredFields? GetRetiredFields()
    // The names of this type's retired keys — fields that no longer exist but stay readable from old data.
    static readonly IReadOnlyList<string> RetiredKeys
  // Typed bag of this type's retired keys; a null member means the source carried no value.
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
    // Alias for SessionId. The protocol surfaces this same int as ClientSessionId on event-args types like ClientJoinedEventArgs.ClientSessionId — code generated against the event-args shape naturally reaches for ctx.ClientSessionId after switching to the Context directly. Provide both names so the natural reach resolves without renaming.
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
    // Takes over the retired-key values of source, replacing any held here. Call it after cloning this type by any route other than Teleport: the bag is private, so a JSON round trip or a hand-written copy drops it and the clone stops emitting retired fields.
    void CopyRetiredFieldsFrom(GlobalState source)
    // Returns the context of the connected client with this client session id, or null when no client with that session id is connected
    Context? GetClientContext(int clientSessionId)
    // Returns the context of the first connected client of this user, or null when the user has no connected client
    Context? GetClientContext(string userId)
    int GetClientSessionId(string userId)
    int[] GetClientSessionIds()
    int[] GetClientSessionIdsByProductId(string productId)
    int[] GetClientSessionIdsExcept(int[] clientSessionIds)
    int[] GetHumanClientSessionIds()
    int[] GetMachineClientSessionIds()
    // The bag to populate before writing: setting a member here makes the writer emit the retired field under its original id during its sunset window, so readers that still resolve the old name keep seeing it.
    GlobalState.RetiredFields GetOrCreateRetiredFields()
    // The retired-key values the last read captured, or null when none were seen. A method rather than a property so TOML mapping and JSON serialization never treat the bag as data.
    GlobalState.RetiredFields? GetRetiredFields()
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
    // The names of this type's retired keys — fields that no longer exist but stay readable from old data.
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
  // Typed bag of this type's retired keys; a null member means the source carried no value.
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
    // Wraps an existing buffer as a protocol message WITHOUT copying — data is aliased, and every accessor reads straight from it. The caller must keep the buffer alive and unchanged for the lifetime of this message; if it is pooled and returned, or otherwise reused or overwritten, this message silently reads corrupted data. Use CopyFrom when the source buffer will be reused.
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
    // Creates a protocol message over a private copy of data, so the caller may reuse, return, or overwrite the source buffer immediately. Prefer this over the aliasing #ctor constructor whenever the source is a pooled or otherwise reused buffer.
    static ProtocolMessage CopyFrom(ReadOnlySpan<byte> data)
    static ProtocolMessage Create(int senderId, IProtocolMessagePayload payload, PayloadType payloadType = Unknown, int trackId = 0, int sequenceId = 0, MessageFlag flags = None, IReadOnlyList<int>? targetIds = null, bool compress = false)
    T GetPayload<T>() where T : IProtocolMessagePayload
    IProtocolMessagePayload GetPayload()
    static ProtocolMessage ModifyMessage(ProtocolMessage message, int? senderId = null, int? trackId = null, int? sequenceId = null, MessageFlag? flags = null, IReadOnlyList<int>? targetIds = null)
    static ProtocolMessage ModifyPayload(IProtocolMessagePayload payload, ProtocolMessage message, PayloadType payloadType = Unknown)
    // Register an app-local message type (an app's own schema/*.tp type, opcode in Opcode.GROUP_APP_LOCAL) at runtime. Called from the generated type's static constructor — app-local types are compiled into the app assembly and are not visible to the platform's compile-time ProtocolMessage source generator.
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
    // Client sends what it says about itself — identification, environment, wire preferences — as a ClientEnvironment on the /connect request instead of expecting the token minter to have baked it into the connect token. None of it was ever authoritative, so signing it bought nothing while making the token several hundred characters longer; a client at this level lets the minter leave the whole block out. A client below this level still has its environment carried in the token, so nothing breaks — see the ConnectToken schema and the legacy-cleanup todo for when the token side can go.
    const int ClientEnvironmentOnConnect = 5
    // Client handles the CORE_CLIENT_INITIALIZATION message — the server/app function registry the server sends out-of-band right after the joining client's GlobalState — and registers those functions during connect. When any connected client advertises less than this, the server keeps the function registry embedded in GlobalState.Functions for the whole session so the older client can still learn server functions. This is a distinct level from FunctionRegistryOutsideGlobalState because the ClientInitialization message was introduced after it: clients advertising only levels 1-3 cannot parse it and would silently receive no functions if the server stripped them from GlobalState.
    const int ClientInitializationMessage = 4
    // Client understands the batched CORE_CLIENT_LIFECYCLE_BATCH message (client joined/ready/left and user joined/left events coalesced into one payload) and unpacks it into the individual events. When all connected external clients advertise at least this, the server coalesces and debounces those broadcasts to external clients instead of one fan-out message per event; otherwise it falls back to per-event broadcasts. Internal (localhost) clients always receive the events immediately, unbatched.
    const int ClientLifecycleBatching = 3
    // Deliberately still ClientInitializationMessage: this constant is what the C# SDK and plugins advertise, and they do not yet send a ClientEnvironment on connect. Advertising a level a build does not implement is how a client talks itself out of data it needs — here it would tell the minter to omit an environment nobody then supplies. It moves when the C# side sends one.
    const int Current = 4
    // Client understands server functions delivered out-of-band (the original targeted ACTION_FUNCTION_REGISTER_BATCH on join) rather than embedded in GlobalState.Functions. Superseded by ClientInitializationMessage: the out-of-band delivery is now the CORE_CLIENT_INITIALIZATION message, which a level-1 client does NOT understand. Do not gate the functions-out-of-GlobalState decision on this level — it is too low and matches clients that predate the ClientInitialization message.
    const int FunctionRegistryOutsideGlobalState = 1
    // Client honors the keepalive watchdog timeout communicated by the server in AuthResponse.KeepaliveTimeoutMs instead of hard-coding it. When all connected clients advertise at least this, the server may stretch its keepalive send interval well beyond the legacy client's fixed watchdog; otherwise it stays within the legacy-safe cap.
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
    // The highest capability level this server build supports; advertised in AuthResponse.
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
  // Factory methods for creating ClientReactive<T> with per-client initialization.
  static class ClientReactive
    // Create a ClientReactive that initializes each client's value using a factory function. The factory receives the client session ID.
    static ClientReactive<T> Create<T>(Func<int, T> factory)
  // Same reactive contract as Reactive<T>, partitioned per client session: each connected client holds its own value, even two clients of the same user (use UserReactive<T> when the value should instead be shared across a user's sessions). .Value resolves against the active client scope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new ClientScope(...)) block — and throws when none is active. Background work carries no client scope, so name the session instead via SetFor / ValueFor.
  class ClientReactive<T> : Reactive<T, ClientScope>
    ctor(T initialValue)
    // Writes the value for one client session regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the client scope is still active (var clientSessionId = ReactiveScope.ClientId; in the UI callback, or ctx.ClientSessionId), then write to it from anywhere.
    void SetFor(int clientSessionId, T value)
    // Atomically read-modify-writes one client session's value, under that session's lock, regardless of which scope — if any — is active.
    void UpdateFor(int clientSessionId, Func<T, T> mutator)
    // Reads one client session's value regardless of which scope — if any — is active.
    T ValueFor(int clientSessionId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // Creates an empty per-client reactive dictionary whose keys are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase), preserved across every mutation.
    ctor(IEqualityComparer<TKey> comparer)
    // Creates a per-client reactive dictionary seeded with initialEntries whose keys are compared with comparer, preserved across every mutation.
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, IEqualityComparer<TKey> comparer)
    // Removes all entries from one client session's dictionary regardless of which scope — if any — is active.
    void ClearFor(int clientSessionId)
    // Removes the entry for key from one client session's dictionary regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(int clientSessionId, TKey key)
    // Adds or replaces one entry in one client session's dictionary regardless of which scope — if any — is active. This is the background-task form of this[key] = value: capture the id while the client scope is still active (var clientSessionId = ReactiveScope.ClientId;), then mutate from anywhere. One notification.
    void SetFor(int clientSessionId, TKey key, TValue value)
    // Atomically transforms one client session's entries under that session's lock, regardless of which scope — if any — is active. Same contract as ReactiveDictionary<TKey, TValue>.Update.
    void UpdateFor(int clientSessionId, Action<Dictionary<TKey, TValue>> transform)
    // Reads one client session's entries regardless of which scope — if any — is active.
    IReadOnlyDictionary<TKey, TValue> ValueFor(int clientSessionId)
  // Shorthand for ReactiveEffect<ClientScope>. Mirrors ClientReactive<T> as the per-client variant of Reactive<T>. Each connected client gets its own runner with independent cancel/queue, materialized on first dep change inside that client's scope.
  class ClientReactiveEffect : ReactiveEffect<ClientScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Creates an empty per-client reactive set whose members are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase), preserved across every mutation.
    ctor(IEqualityComparer<T> comparer)
    // Creates a per-client reactive set seeded with initialItems whose members are compared with comparer, preserved across every mutation.
    ctor(IEnumerable<T> initialItems, IEqualityComparer<T> comparer)
    // Adds item to one client session's set regardless of which scope — if any — is active. This is the background-task form of Add(item): capture the id while the client scope is still active (var clientSessionId = ReactiveScope.ClientId;), then mutate from anywhere. Returns whether it was added. One notification either way.
    bool AddFor(int clientSessionId, T item)
    // Removes all members from one client session's set regardless of which scope — if any — is active.
    void ClearFor(int clientSessionId)
    // Removes item from one client session's set regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(int clientSessionId, T item)
    // Atomically transforms one client session's members under that session's lock, regardless of which scope — if any — is active. Same contract as ReactiveHashSet<T>.Update.
    void UpdateFor(int clientSessionId, Action<HashSet<T>> transform)
    // Reads one client session's members regardless of which scope — if any — is active.
    IReadOnlyCollection<T> ValueFor(int clientSessionId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per client session exactly like ClientReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new ClientScope(...)) block. Accessing outside these contexts throws an exception.
  class ClientReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Appends to one client session's list regardless of which scope — if any — is active. This is the background-task form of ReactiveList<T>.Add: capture the id while the client scope is still active (var clientSessionId = ReactiveScope.ClientId;), then mutate from anywhere. One notification.
    void AddFor(int clientSessionId, T item)
    // Removes all items from one client session's list regardless of which scope — if any — is active.
    void ClearFor(int clientSessionId)
    // Removes the first occurrence of item from one client session's list regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(int clientSessionId, T item)
    // Atomically replaces one client session's items under that session's lock, regardless of which scope — if any — is active. Same contract as ReactiveList<T>.Update.
    void UpdateFor(int clientSessionId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    // Reads one client session's items regardless of which scope — if any — is active.
    IReadOnlyList<T> ValueFor(int clientSessionId)
  interface IReactive
    long Version { get; }
    // Fires whenever this reactive's value changes (in any scope, for scoped variants). Payload-free so a single subscription can be taken across heterogeneous reactives — handlers fetch the new value via .Value when they need it. Used by ReactiveEffect and other dependency-tracked consumers.
    event Action? Changed
    // Fires with the scope-derived session id whose value just changed. For unscoped reactives the id is always 0; for ClientReactive<T> it is the hash of ClientScope; for UserReactive<T> the hash of UserScope; etc. Lets external subscription routing fan out to only the clients whose scope matches the changed signal.
    event Action<int>? SessionChanged
  // Factory methods for creating MountReactive<T> with per-mount initialization.
  static class MountReactive
    // Create a MountReactive that initializes each mount's value using a factory function. The factory receives the mount id.
    static MountReactive<T> Create<T>(Func<string, T> factory)
  // Same reactive contract as Reactive<T>, partitioned per Parallax mount an app declares via Mounts (e.g. independent message history for an embedded "aiCanvas" mount vs the "ikon-ui" page). For state shared across a client's mounts use ClientReactive<T>; across all clients use Reactive<T>. .Value resolves against the MountScope active during a render iteration — typically anywhere inside UI.Root() — and throws otherwise. Background work carries no mount scope, so name the mount instead via SetFor / ValueFor.
  class MountReactive<T> : Reactive<T, MountScope>
    ctor(T initialValue)
    // Writes one mount's value regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the mount scope is still active (var mountId = ReactiveScope.MountId;), then write to it from anywhere.
    void SetFor(string mountId, T value)
    // Atomically read-modify-writes one mount's value, under that mount's lock, regardless of which scope — if any — is active.
    void UpdateFor(string mountId, Func<T, T> mutator)
    // Reads one mount's value regardless of which scope — if any — is active.
    T ValueFor(string mountId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per mount exactly like MountReactive<T>. Important: Must be accessed inside a render iteration where MountScope is active — typically anywhere inside UI.Root().
  class MountReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // Removes all entries from one mount's dictionary regardless of which scope — if any — is active.
    void ClearFor(string mountId)
    // Removes the entry for key from one mount's dictionary regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string mountId, TKey key)
    // Adds or replaces one entry in one mount's dictionary regardless of which scope — if any — is active. This is the background-task form of this[key] = value: capture the id while the mount scope is still active (var mountId = ReactiveScope.MountId;), then mutate from anywhere. One notification.
    void SetFor(string mountId, TKey key, TValue value)
    // Atomically transforms one mount's entries under that mount's lock, regardless of which scope — if any — is active. Same contract as ReactiveDictionary<TKey, TValue>.Update.
    void UpdateFor(string mountId, Action<Dictionary<TKey, TValue>> transform)
    // Reads one mount's entries regardless of which scope — if any — is active.
    IReadOnlyDictionary<TKey, TValue> ValueFor(string mountId)
  // Shorthand for ReactiveEffect<MountScope>. Mirrors MountReactive<T> as the per-mount variant of Reactive<T>. Each Parallax mount gets its own runner, materialized on first dep change inside that mount's scope.
  class MountReactiveEffect : ReactiveEffect<MountScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per mount exactly like MountReactive<T>. Important: Must be accessed inside a render iteration where MountScope is active — typically anywhere inside UI.Root().
  class MountReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Adds item to one mount's set regardless of which scope — if any — is active. This is the background-task form of Add(item): capture the id while the mount scope is still active (var mountId = ReactiveScope.MountId;), then mutate from anywhere. Returns whether it was added. One notification either way.
    bool AddFor(string mountId, T item)
    // Removes all members from one mount's set regardless of which scope — if any — is active.
    void ClearFor(string mountId)
    // Removes item from one mount's set regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string mountId, T item)
    // Atomically transforms one mount's members under that mount's lock, regardless of which scope — if any — is active. Same contract as ReactiveHashSet<T>.Update.
    void UpdateFor(string mountId, Action<HashSet<T>> transform)
    // Reads one mount's members regardless of which scope — if any — is active.
    IReadOnlyCollection<T> ValueFor(string mountId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per mount exactly like MountReactive<T>. Important: Must be accessed inside a render iteration where MountScope is active — typically anywhere inside UI.Root().
  class MountReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Appends to one mount's list regardless of which scope — if any — is active. This is the background-task form of ReactiveList<T>.Add: capture the id while the mount scope is still active (var mountId = ReactiveScope.MountId;), then mutate from anywhere. One notification.
    void AddFor(string mountId, T item)
    // Removes all items from one mount's list regardless of which scope — if any — is active.
    void ClearFor(string mountId)
    // Removes the first occurrence of item from one mount's list regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string mountId, T item)
    // Atomically replaces one mount's items under that mount's lock, regardless of which scope — if any — is active. Same contract as ReactiveList<T>.Update.
    void UpdateFor(string mountId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    // Reads one mount's items regardless of which scope — if any — is active.
    IReadOnlyList<T> ValueFor(string mountId)
  // Selects the backing store for a persistent reactive.
  enum PersistenceBackend
    // Asset storage on private S3-style cloud files. Explicitly opts the value out of the Default routing — pick it when a structured value must stay on asset storage even though the app has its built-in Postgres database.
    Private
    // Asset storage on public S3-style cloud files. The reactive exposes a PublicUrl accessor so the value can be linked to from the open web.
    Public
    // Postgres key-value row in a database the app declares in ikon-config.toml. Pass the database name (matching the Databases = ["name:postgres"] entry) when constructing the reactive; with a single declared database the name can be omitted.
    Postgres
    // Let the platform pick the store per its storage doctrine: structured values go to the app's built-in app database when the session has one, while binary payloads (byte[]) — and sessions without a database — use private asset storage. This is the default for every persistent reactive that does not name a backend.
    Default
  // Identifies where a reactive's value is persisted in cloud storage and how it is keyed.
  enum PersistenceScope
    // Not persisted. The value is ephemeral and lost when the app restarts.
    None
    // Persisted globally for the app within its space. Shared across all session identities and users. Use for app-wide configuration that one app instance owns.
    Global
    // Persisted per session identity (the routing key the app declares as its TSessionIdentity). Two app instances with the same session identity share the same value; different identities have separate values.
    Session
    // Persisted per user. The current primary user's id is part of the storage key, so each user has their own value.
    User
  static class Reactive
    // Runs action on a background task and assigns its result to reactiveValue when it completes, so subscribers react without the caller awaiting. Exceptions go to onError when provided and are logged otherwise; cancellation leaves the reactive value unchanged.
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
    // Runs action on a background task and assigns its result to reactiveValue when it completes, passing token to the action so it can observe cancellation. Exceptions go to onError when provided and are logged otherwise; cancellation leaves the reactive value unchanged.
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
  // Reading Value during a UI render registers a dependency; writing a changed value re-renders only the parts that read it. An unscoped Reactive<T> holds one value shared across all clients and is accessible anywhere. For per-client state use ClientReactive<T>; for per-user state (shared across a user's sessions) use UserReactive<T>. Those scoped variants resolve .Value against the active scope, so it must be read inside one — UI.Root(), an action callback, or a ReactiveScope.Use() block — and throw otherwise; background work (a Task.Run loop, a timer, an endpoint handler) has no scope and names its target instead via SetFor(id, value) / ValueFor(id).
  class Reactive<T> : IReactive
    // Creates a reactive whose initial value is default(T). Call as new Reactive<T>() — the UseDefault parameter is only an overload disambiguator and is never passed explicitly.
    ctor(UseDefault _ = default)
    // Creates a reactive with an explicit initial value: new Reactive<int>(0), new Reactive<Dictionary<int, Player>>(new()).
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
    // Unwraps to Value — this is a TRACKED read: used during render it registers a dependency (the component re-renders when the value changes), and for scoped variants it throws InvalidOperationException when no scope is active. It is not a cheap unwrap; use Peek to read the current value without tracking.
    static implicit operator T(Reactive<T> r)
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
  // Base class for scoped reactive variables: each distinct TScope instance gets its own value, resolved from the active scope. Use directly only for custom scope types — prefer ClientReactive<T> (per-client) or UserReactive<T> (per-user). The required scope must be active when accessing .Value (e.g. inside UI.Root()); otherwise it throws InvalidOperationException.
  class Reactive<T, TScope> : Reactive<T> where TScope : IScopeKey
    ctor(T initialValue)
    ctor(Func<T> initialValue)
    // Writes the value for scope regardless of which scope — if any — is active, so background work can target a scope it does not run under without re-scoping itself.
    void SetFor(TScope scope, T value)
    // Atomically read-modify-writes the value for scope, under that scope's lock, regardless of which scope — if any — is active.
    void UpdateFor(TScope scope, Func<T, T> mutator)
    // Reads the value for scope regardless of which scope — if any — is active.
    T ValueFor(TScope scope)
  // Convenience helpers on Reactive<T> for the busy-flag pattern that every async handler uses. Without these, the standard shape is verbose and easy to break:
  // _busy.Value = true;
  // try { await SlowThingAsync(); }
  // finally { _busy.Value = false; }
  // Forgetting finally leaves the flag stuck on if the call throws. AsToken collapses the shape to:
  // using var _ = _busy.AsToken();
  // await SlowThingAsync();
  // — the flag flips to true on entry, the IDisposable returns it to false on dispose (including the catch-and-rethrow path of using).
  static class ReactiveBoolExtensions
    // Set the flag to true and return an IDisposable that returns it to false on dispose. Idempotent — disposing twice is safe (the second dispose is a no-op).
    static IDisposable AsToken(this Reactive<bool> reactive)
  // Mutation helpers for a Reactive<T> that wraps a mutable collection. List state belongs in ReactiveList<T> and dictionary state in ReactiveDictionary<TKey, TValue>, not in Reactive<List<T>> / Reactive<Dictionary<TKey, TValue>>: they give the same one-call mutators (Add / Remove / indexer-set / Update) with copy-on-write snapshots and a read-only Value, so a snapshot handed to a renderer can't be mutated behind its back. These extensions are for the collections that have no reactive equivalent yet — Reactive<HashSet<T>> — and for legacy Reactive<List<T>> / Reactive<Dictionary<TKey, TValue>> code. In those cases they mutate the underlying instance AND fire the change notification in one call, so callers write _byId.Set(key, value) instead of the two-step _byId.Value[key] = value; _byId.NotifyUpdate();. The two-step form is needed because the reference-equality check at the Value setter doesn't trigger when the underlying collection is mutated in place — which is what makes a forgotten NotifyUpdate the classic "UI doesn't update after Add/Remove" bug. These helpers make the right thing the easy thing. Every helper runs its mutation through the locked Reactive<T>.Update, so concurrent mutations from parallel handlers serialize instead of racing. Reassignment (_byId.Value = new Dictionary<TKey, TValue>(_byId.Value) { [key] = value }) also notifies and remains available when callers want immutable-style updates.
  static class ReactiveCollectionExtensions
    // Append item to the underlying list and notify.
    static void Add<T>(this Reactive<List<T>> reactive, T item)
    // Add item to the underlying set and notify if it was new.
    static bool Add<T>(this Reactive<HashSet<T>> reactive, T item)
    // Append items to the underlying list and notify once.
    static void AddRange<T>(this Reactive<List<T>> reactive, IEnumerable<T> items)
    // Clear the underlying list and notify if it had items.
    static void Clear<T>(this Reactive<List<T>> reactive)
    // Clear the underlying set and notify if it had items.
    static void Clear<T>(this Reactive<HashSet<T>> reactive)
    // Clear the underlying dictionary and notify if it had entries.
    static void Clear<TKey, TValue>(this Reactive<Dictionary<TKey, TValue>> reactive)
    // Insert item at index and notify.
    static void Insert<T>(this Reactive<List<T>> reactive, int index, T item)
    // Escape hatch for mutations the typed helpers don't cover (e.g. sorting in place, swapping items, clearing+repopulating). The mutator runs on the live reference under the Reactive<T>.Update lock; the change notification fires after it returns. Use the typed helpers (Add<T>, Remove<T>, …) when one fits.
    static void Mutate<T>(this Reactive<T> reactive, Action<T> mutator)
    // Remove the first occurrence of item and notify if removed.
    static bool Remove<T>(this Reactive<List<T>> reactive, T item)
    // Remove item from the underlying set and notify if removed.
    static bool Remove<T>(this Reactive<HashSet<T>> reactive, T item)
    // Remove key and notify if removed.
    static bool Remove<TKey, TValue>(this Reactive<Dictionary<TKey, TValue>> reactive, TKey key)
    // Remove all items matching match and notify if any removed.
    static int RemoveAll<T>(this Reactive<List<T>> reactive, Predicate<T> match)
    // Remove the item at index and notify.
    static void RemoveAt<T>(this Reactive<List<T>> reactive, int index)
    // Set key to value and notify.
    static void Set<TKey, TValue>(this Reactive<Dictionary<TKey, TValue>> reactive, TKey key, TValue value)
  // Reads track a dependency exactly like Reactive<T> (reading Value, Count, the indexer, TryGetValue, or enumerating during render). Every mutation method fires exactly one notification on its own — _byId[key] = value is the whole call. Reactive<T>.NotifyUpdate is the escape hatch for the one case the mutators cannot see: mutating a stored value in place (feed.Messages.Add(m); _feeds.NotifyUpdate();). Copy-on-write: every mutation runs under the lock and replaces the backing dictionary with a fresh copy, so concurrent mutations serialize and any dictionary handed out earlier is a stable snapshot. Each mutation copies the whole dictionary, so for batches prefer the single-notify bulk ops (ReplaceAll, Update) over per-key calls in a loop.
  class ReactiveDictionary<TKey, TValue> : Reactive<Dictionary<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // Creates an empty reactive dictionary whose keys are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase). The comparer is preserved across every copy-on-write mutation, so the custom key semantics hold for the life of the dictionary.
    ctor(IEqualityComparer<TKey> comparer)
    // Creates a reactive dictionary seeded with initialEntries whose keys are compared with comparer. The comparer is preserved across every mutation.
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, IEqualityComparer<TKey> comparer)
    // The number of entries. Tracked read.
    int Count { get; }
    // The value for key. The getter is a tracked read and throws for a missing key; the setter adds or replaces the entry with one change notification.
    TValue this[TKey key] { get; set; }
    // The keys of the current entries. Tracked read.
    IEnumerable<TKey> Keys { get; }
    // The current entries without dependency tracking. See Reactive<T>.Peek.
    IReadOnlyDictionary<TKey, TValue> Peek { get; }
    // The current entries as a read-only snapshot. Reading tracks a dependency like Reactive<T>.Value; assigning replaces the whole content with a copy of the given entries (see ReplaceAll).
    IReadOnlyDictionary<TKey, TValue> Value { get; set; }
    // The values of the current entries. Tracked read.
    IEnumerable<TValue> Values { get; }
    // Add key with value; throws if the key already exists. One notification.
    void Add(TKey key, TValue value)
    // Remove all entries. One notification.
    void Clear()
    // Whether an entry for key is present. Tracked read.
    bool ContainsKey(TKey key)
    // Enumerate a snapshot of the current entries. Tracked read; the snapshot is safe to iterate while other code mutates the dictionary.
    IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    // Remove the entry for key. Returns whether it was found. One notification either way.
    bool Remove(TKey key)
    // Replace the whole content with a copy of entries. One notification.
    void ReplaceAll(IEnumerable<KeyValuePair<TKey, TValue>> entries)
    // Add key with value if the key is not present. Returns whether it was added. One notification either way.
    bool TryAdd(TKey key, TValue value)
    // Get the value for key if present. Tracked read.
    bool TryGetValue(TKey key, out TValue value)
    // Atomically transform the content: transform receives a fresh copy of the current entries and mutates it freely (add, remove, rewrite several keys). Runs under the same lock as all other mutations, so concurrent updates serialize. One notification.
    void Update(Action<Dictionary<TKey, TValue>> transform)
  // Lifecycle (global): • Constructor runs the body once immediately (initial fire). • Each tracked dep's IReactive.Changed event triggers a re-run. • If a dep changes while a previous run is still in flight, the previous run's CancellationToken is cancelled and one follow-up run is queued. Rapid-fire changes coalesce. • IDisposable.Dispose cancels any in-flight run and detaches all dep subscriptions. • Exceptions in the body (other than OperationCanceledException) are logged and do not disable the effect.
  class ReactiveEffect : IDisposable
    // Create an effect with an async body. The token cancels when a dep changes mid-run; respect it for clean cancellation.
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // This overload exists so an async () => await ... body binds here as a Task-returning delegate rather than collapsing into the Action overload as async-void — which would report the run complete at the first await and swallow later exceptions. Use the Func<CancellationToken, Task> overload to observe cancellation.
    ctor(Func<Task> body, params IReactive[] deps)
    // Create an effect with a sync body.
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
    // Creates an empty reactive set whose members are compared with comparer (e.g. StringComparer.OrdinalIgnoreCase). The comparer is preserved across every copy-on-write mutation, so the custom membership semantics hold for the life of the set.
    ctor(IEqualityComparer<T> comparer)
    // Creates a reactive set seeded with initialItems whose members are compared with comparer. The comparer is preserved across every mutation.
    ctor(IEnumerable<T> initialItems, IEqualityComparer<T> comparer)
    // The number of members. Tracked read.
    int Count { get; }
    // The current members without dependency tracking. See Reactive<T>.Peek.
    IReadOnlyCollection<T> Peek { get; }
    // The current members as a read-only snapshot. Reading tracks a dependency like Reactive<T>.Value; assigning replaces the whole content with a copy of the given members (see ReplaceAll).
    IReadOnlyCollection<T> Value { get; set; }
    // Add item. Returns whether it was added (false if already present). One notification either way.
    bool Add(T item)
    // Remove all members. One notification.
    void Clear()
    // Whether item is present. Tracked read.
    bool Contains(T item)
    // Remove every member of other. One notification for the whole batch.
    void ExceptWith(IEnumerable<T> other)
    // Enumerate a snapshot of the current members. Tracked read; the snapshot is safe to iterate while other code mutates the set.
    IEnumerator<T> GetEnumerator()
    // Remove item. Returns whether it was found. One notification either way.
    bool Remove(T item)
    // Replace the whole content with a copy of items. One notification.
    void ReplaceAll(IEnumerable<T> items)
    // Add every member of other. One notification for the whole batch.
    void UnionWith(IEnumerable<T> other)
    // Atomically transform the content: transform receives a fresh copy of the current members and mutates it freely (add, remove, several members at once). Runs under the same lock as all other mutations, so concurrent updates serialize. One notification.
    void Update(Action<HashSet<T>> transform)
  // Reads track a dependency exactly like Reactive<T> (reading Value, Count, the indexer, or enumerating during render). Every mutation method fires exactly one notification on its own — _items.Add(x) is the whole call. Reactive<T>.NotifyUpdate is the escape hatch for the one case the mutators cannot see: mutating an item in place (tracker.Progress = 47; _items.NotifyUpdate();). Copy-on-write: every mutation runs under the lock and replaces the backing list with a fresh copy, so concurrent mutations serialize and any list handed out earlier is a stable snapshot. Each mutation copies the whole list, so for batches prefer the single-notify bulk ops (AddRange, ReplaceAll, Update) over per-item calls in a loop.
  class ReactiveList<T> : Reactive<List<T>>, IReadOnlyList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // The number of items. Tracked read.
    int Count { get; }
    // The item at index. The getter is a tracked read; the setter replaces the item with one change notification.
    T this[int index] { get; set; }
    // The current items without dependency tracking. See Reactive<T>.Peek.
    IReadOnlyList<T> Peek { get; }
    // The current items as a read-only snapshot. Reading tracks a dependency like Reactive<T>.Value; assigning replaces the whole content with a copy of the given sequence (see ReplaceAll).
    IReadOnlyList<T> Value { get; set; }
    // Append item. One notification.
    void Add(T item)
    // Append items. One notification for the whole batch.
    void AddRange(IEnumerable<T> items)
    // Remove all items. One notification.
    void Clear()
    // Whether item is present. Tracked read.
    bool Contains(T item)
    // Enumerate a snapshot of the current items. Tracked read; the snapshot is safe to iterate while other code mutates the list.
    IEnumerator<T> GetEnumerator()
    // Index of the first occurrence of item, or -1. Tracked read.
    int IndexOf(T item)
    // Insert item at index. One notification.
    void Insert(int index, T item)
    // Remove the first occurrence of item. Returns whether it was found. One notification either way.
    bool Remove(T item)
    // Remove all items matching match. Returns the removed count. One notification either way.
    int RemoveAll(Predicate<T> match)
    // Remove the item at index. One notification.
    void RemoveAt(int index)
    // Replace the whole content with a copy of items. One notification.
    void ReplaceAll(IEnumerable<T> items)
    // Sort the items using comparison. One notification.
    void Sort(Comparison<T> comparison)
    // Atomically replace the content: transform sees the current items and returns the new ones, which are materialized into a fresh list. Runs under the same lock as all other mutations, so concurrent updates serialize. One notification.
    void Update(Func<IReadOnlyList<T>, IEnumerable<T>> transform)
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
  // Marker type for the default-value Reactive<T> constructor. Because every constructor carries trailing caller-info parameters, a marker parameter is what keeps the value-less overload distinct from Reactive(T initialValue, ...). Never pass it explicitly — write new Reactive<T>() and the value starts at default(T). Passing any argument at all selects the value constructor, so new Reactive<Dictionary<int, Player>>(new()) means what it reads as.
  readonly struct UseDefault
  // Same reactive contract as Reactive<T>, partitioned per user and shared across that user's client sessions (use ClientReactive<T> when each client needs its own value). .Value resolves against the active user scope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throws when none is active. Background work carries no user scope, so name the user instead via SetFor / ValueFor.
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue)
    ctor(Func<string, T> initialValue)
    // Writes one user's value regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then write to it from anywhere.
    void SetFor(string userId, T value)
    // Atomically read-modify-writes one user's value, under that user's lock, regardless of which scope — if any — is active.
    void UpdateFor(string userId, Func<T, T> mutator)
    // Reads one user's value regardless of which scope — if any — is active.
    T ValueFor(string userId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor()
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries)
    // Removes all entries from one user's dictionary regardless of which scope — if any — is active.
    void ClearFor(string userId)
    // Removes the entry for key from one user's dictionary regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, TKey key)
    // Adds or replaces one entry in one user's dictionary regardless of which scope — if any — is active. This is the background-task form of this[key] = value: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. One notification.
    void SetFor(string userId, TKey key, TValue value)
    // Atomically transforms one user's entries under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveDictionary<TKey, TValue>.Update.
    void UpdateFor(string userId, Action<Dictionary<TKey, TValue>> transform)
    // Reads one user's entries regardless of which scope — if any — is active.
    IReadOnlyDictionary<TKey, TValue> ValueFor(string userId)
  // Shorthand for ReactiveEffect<UserScope>. Mirrors UserReactive<T> as the per-user variant of Reactive<T>. Each distinct user gets its own runner; the same user across multiple sessions shares one runner.
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Binds an async () => ... body here as a Task-returning delegate instead of the async-void Action overload — constructors are not inherited, so this mirrors the base ReactiveEffect<TScope> overload.
    ctor(Func<Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveHashSet<T> : ReactiveHashSet<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Adds item to one user's set regardless of which scope — if any — is active. This is the background-task form of Add(item): capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. Returns whether it was added. One notification either way.
    bool AddFor(string userId, T item)
    // Removes all members from one user's set regardless of which scope — if any — is active.
    void ClearFor(string userId)
    // Removes item from one user's set regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, T item)
    // Atomically transforms one user's members under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveHashSet<T>.Update.
    void UpdateFor(string userId, Action<HashSet<T>> transform)
    // Reads one user's members regardless of which scope — if any — is active.
    IReadOnlyCollection<T> ValueFor(string userId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — partitioned per user exactly like UserReactive<T>. Important: Must be accessed inside UI.Root() or within a ReactiveScope.Use(new UserScope(...)) block. Accessing outside these contexts throws an exception.
  class UserReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // Appends to one user's list regardless of which scope — if any — is active. This is the background-task form of ReactiveList<T>.Add: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. One notification.
    void AddFor(string userId, T item)
    // Removes all items from one user's list regardless of which scope — if any — is active.
    void ClearFor(string userId)
    // Removes the first occurrence of item from one user's list regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, T item)
    // Atomically replaces one user's items under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveList<T>.Update.
    void UpdateFor(string userId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    // Reads one user's items regardless of which scope — if any — is active.
    IReadOnlyList<T> ValueFor(string userId)

namespace Ikon.Common.Core.Scope
  // Scope for backend token context, transports the backend token of the caller.
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
  // Scope with a user-specified name and ID, enabling dynamic scoping without needing new struct types.
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
    // The mount id every Ikon app emits today on its single Parallax stream. Apps that don't override IAppBase.Mounts render under this id.
    const string DefaultMountId
  // Scope for grouping a single logical operation (e.g., LLM generation, image generation).
  readonly struct OperationScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  // Scope for application run context, typically set at program startup in Program.cs. Used to group all log events and operations within a single application run.
  readonly struct RunScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  // Scope for tenant/customer context, an arbitrary user-specified ID for scoping AI app logic.
  readonly struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
  // Machine-triggered work has no ClientScope and no UserScope, so without this its cost lands in the space's totals attached to nothing and a schedule quietly burning credits is indistinguishable from the app's ordinary use. Every log event carries the active scopes, so the cost of an AI call made inside a trigger handler is attributed by the ambient scope alone — call sites need no change. Scoped to the invocation rather than the session on purpose: a session woken by cron goes on to serve clients, and their spend is theirs, not the schedule's. The values match the backend's AppSessionSource spelling, so the trigger a cost row carries reads the same as the source stamped on the session that ran it.
  readonly struct TriggerScope : IScopeKey
    ctor(string kind)
    string Id { get; }
    string Name { get; }
    // A [Cron] function invoked by the backend scheduler.
    const string Cron
    // An HTTP endpoint or inbound webhook request routed to the app.
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
  // Represents a successfully signed document returned by the platform signing service. The platform downloads the result from the upstream signing vendor, hashes it, and hands the signed bytes plus evidence metadata to the requesting app. Apps should persist Bytes as the system of record — the platform retention is short.
  sealed record SignedDocument
    ctor(string OrderId, byte[] Bytes, string MimeType, DateTimeOffset SignedAt, string SignedDocumentHash, string IdentityScheme, string? SignerNameHash, string? EvidenceLevel)
    byte[] Bytes { get; init; }
    string? EvidenceLevel { get; init; }
    string IdentityScheme { get; init; }
    string MimeType { get; init; }
    string OrderId { get; init; }
    DateTimeOffset SignedAt { get; init; }
    string SignedDocumentHash { get; init; }
    // The signer's legal name as the identity provider reported it, when the order requested the name attribute. Null otherwise. This is what an app shows a user and checks against the signer it expected; SignerNameHash is keyed by a platform secret and can do neither.
    string? SignerName { get; init; }
    string? SignerNameHash { get; init; }

namespace Ikon.Common.Core.Telephony
  // A message the app's number received.
  sealed record SmsMessage
    // From: Who sent it, in E.164. Pass it to app.Telephony.SendSmsAsync to reply.
    // To: The number of the app's that received it.
    // Text: The message body.
    // MessageId: The provider's id for the message.
    ctor(string From, string To, string Text, string MessageId)
    // Who sent it, in E.164. Pass it to app.Telephony.SendSmsAsync to reply.
    string From { get; init; }
    // The provider's id for the message.
    string MessageId { get; init; }
    // The message body.
    string Text { get; init; }
    // The number of the app's that received it.
    string To { get; init; }
  // Carries no price. A send is charged to the space in platform credits, which is the only cost an app is quoted — what the carrier behind it charged is not the app's to see. Read what a space has spent with ikon app costs.
  sealed record SmsSendResult
    // MessageId: The provider's id for the message, for correlating delivery reports.
    // From: The number or sender id the message was sent from.
    // Parts: Billable segments. A message using non-GSM characters fits roughly half as much per segment.
    // Status: The provider's status for the message at the moment it was accepted.
    // Replyable: Whether the recipient can reply. False when the space holds no number local to the recipient's market: a foreign number is commonly stripped in transit and shown as "Unknown", so the message arrives but nothing can be sent back.
    ctor(string MessageId, string From, int Parts, string Status, bool Replyable)
    // The number or sender id the message was sent from.
    string From { get; init; }
    // The provider's id for the message, for correlating delivery reports.
    string MessageId { get; init; }
    // Billable segments. A message using non-GSM characters fits roughly half as much per segment.
    int Parts { get; init; }
    // Whether the recipient can reply. False when the space holds no number local to the recipient's market: a foreign number is commonly stripped in transit and shown as "Unknown", so the message arrives but nothing can be sent back.
    bool Replyable { get; init; }
    // The provider's status for the message at the moment it was accepted.
    string Status { get; init; }
  // A phone number the app's space holds.
  sealed record TelephonyNumber
    // Number: The number in E.164 form, for example +358401234567.
    // Country: The ISO 3166-1 alpha-2 country the number belongs to.
    // Provider: Which carrier serves this number. Two of the app's numbers may differ.
    // Capabilities: What the number can carry, as the provider names it — sms, voice.
    // IsDefault: Whether this is the number used when a send or a call names none. At most one of the app's numbers is the default; when none is, the platform picks one local to each recipient's market.
    // SessionIdentity: Which instance this number's incoming messages and calls are delivered to. Empty means the app's shared instance. Two numbers can carry different identities, which is how one app answers as several users.
    ctor(string Number, string Country, string Provider, IReadOnlyList<string> Capabilities, bool IsDefault, IReadOnlyDictionary<string, string> SessionIdentity)
    // What the number can carry, as the provider names it — sms, voice.
    IReadOnlyList<string> Capabilities { get; init; }
    // The ISO 3166-1 alpha-2 country the number belongs to.
    string Country { get; init; }
    // Whether this is the number used when a send or a call names none. At most one of the app's numbers is the default; when none is, the platform picks one local to each recipient's market.
    bool IsDefault { get; init; }
    // The number in E.164 form, for example +358401234567.
    string Number { get; init; }
    // Which carrier serves this number. Two of the app's numbers may differ.
    string Provider { get; init; }
    // Which instance this number's incoming messages and calls are delivered to. Empty means the app's shared instance. Two numbers can carry different identities, which is how one app answers as several users.
    IReadOnlyDictionary<string, string> SessionIdentity { get; init; }
  // There is no single provider for a space: it may hold numbers on more than one at once, so each number names its own.
  sealed record TelephonyStatus
    // Enabled: Whether the space holds any number at all.
    // Numbers: The numbers the space holds. Messages and calls are sent from these.
    ctor(bool Enabled, IReadOnlyList<TelephonyNumber> Numbers)
    // Whether the space holds any number at all.
    bool Enabled { get; init; }
    // The numbers the space holds. Messages and calls are sent from these.
    IReadOnlyList<TelephonyNumber> Numbers { get; init; }
