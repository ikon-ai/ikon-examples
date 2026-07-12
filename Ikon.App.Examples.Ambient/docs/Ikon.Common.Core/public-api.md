# Ikon.Common.Core Public API

namespace Ikon.Common.Core
  // Verifies platform-signed assertions (e.g. StepUpAssertion ) issued by the Ikon platform backend. Fetches the platform JWKS from {platformBaseUrl}/.well-known/jwks.json on demand and caches the keys for the lifetime of the verifier instance.
  sealed class AssertionVerifier
    ctor(string platformBaseUrl, HttpClient? httpClient = null, Func<DateTimeOffset>? clock = null)
    // Generic JWT validation: JWKS-backed signature verification + standard iss/aud/exp checks + (when present) iat clock-skew guard. Returns the decoded claims as a JsonDocument — caller owns disposal — plus the token's exp so a caller can cache the validated result against the token lifetime. Use this for OAuth 2.1 bearer-token resource-server validation where the step-up-specific projection in VerifyAsync isn't relevant.
    Task<(JsonDocument Claims, DateTimeOffset ExpiresAt)> VerifyAndExtractClaimsAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = default)
    Task<StepUpAssertion> VerifyAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = default)
  delegate Log.AsyncFlowFinishedHandler
    void AsyncFlowFinishedHandler(object sender, int asyncFlowId)
  sealed class AsyncLocalInstanceAttribute : Attribute
    ctor()
  class AsyncLocalInstance<T> where T : new()
    ctor()
    static T Instance { get; }
    static void DisableAsyncLocalInstance()
    static void EnableAndInitAsyncLocalInstance()
    static void SetAsyncLocalInstance(T value)
  class BackendQuotaExceededException : UserException
    ctor(string key, int current, int limit, string friendlyMessage)
    int Current { get; }
    string Key { get; }
    int Limit { get; }
  // A helper comparer to compare two dictionaries for equality by checking that they have the same keys and that the corresponding values are equal.
  class ReactiveGlobalState.DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
    ctor()
    bool Equals(Dictionary<TKey, TValue>? x, Dictionary<TKey, TValue>? y)
    int GetHashCode(Dictionary<TKey, TValue> obj)
    static ReactiveGlobalState.DictionaryComparer<TKey, TValue> Instance
  static class ExceptionFormatter
    static string FormatException(Exception ex, bool includeFilePaths = true)
  // Provides resilient conversions between loosely typed LLM/tool payloads and strongly typed function parameters/results. Handles primitives, arrays (including single-item arrays), Newtonsoft JSON tokens, and falls back to System.Text.Json when needed.
  static class ExtendedCast
    static T? Convert<T>(object? value)
    static object? Convert(object? value, Type targetType)
    // Deserializes a JsonElement into targetType , tolerating the placeholders LLMs often emit when a schema marks every property required but the underlying field is nullable: "" for collections/objects becomes null, "" for bool becomes false, etc. Falls back to ExtendedCast conversion on type mismatch so callers pick up array-wrap and single-item-array behaviour for free.
    static object? FromJsonElement(JsonElement element, Type targetType)
  static class ExtendedCastExtensions
    static T? ExtendedCast<T>(this object? value)
    static object? ExtendedCast(this object? value, Type targetType)
  class FeatureFlagsStorage : AsyncLocalInstance<FeatureFlagsStorage>
    ctor()
    ImmutableDictionary<string, bool> ReadOnlyFeatureFlags { get; }
    bool Get(string featureFlagName)
    void Set(string featureFlagName, bool value, bool shouldOverride = false)
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
    abstract IDisposable RegisterMessageHandler(Func<ProtocolMessage, ValueTask> handler, Opcode? opcodeGroupMask = null, Opcode[]? opcodes = null)
    abstract ValueTask SendMessageAsync(ProtocolMessage message)
    abstract ValueTask SendMessageAsync(IProtocolMessagePayload payload)
  interface IPlugin : IMessageChannel, IProtocolMessageChannel
    string ConnectTokenJson { get; }
    bool IsAuthTicketSent { get; }
    bool IsConnected { get; }
    // The AuthResponse from the most recent successful connect (entrypoints + auth ticket + client session). Cache it to drive a later ReconnectWithAuthResponseAsync .
    AuthResponse? LastAuthResponse { get; }
    DateTime ServerInitTime { get; set; }
    abstract Task ConnectAsync2(string connectUrl, CancellationToken ct = default)
    abstract Task ConnectAsync2(string host, int port, bool useTls, CancellationToken ct = default)
    abstract void OverrideConfigValues(string overrideConfigJson)
    // Soft reconnect: reopen the transport reusing a previously-fetched AuthResponse (its entrypoints, auth ticket, and client session) WITHOUT re-fetching it via the /connect GET. Lets the server resume the same session within its disconnect grace. Use LastAuthResponse from the prior connection.
    abstract Task ReconnectWithAuthResponseAsync(AuthResponse cachedAuthResponse, CancellationToken ct = default)
    abstract Task StopAsync()
  interface IProtocolMessageChannel : IMessageChannel
    Context ClientContext { get; }
  static class Json
    static Dictionary<string, object> AsDict(string json)
    static Dictionary<string, object> ConvertDict(Dictionary<string, object> dict)
    static T DeepCopy<T>(T obj)
    static string Format(string json, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false)
    static T From<T>(string json, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false, bool caseInsensitive = false)
    static object? From(string json, Type type, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false)
    static object? From(string json, string typeName, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false)
    // Like Deserialize<T> , but tolerant of LLM responses that wrap the JSON payload in a markdown code fence (```json ... ``` or ``` ... ```). Tries direct deserialization first; on JsonException , looks for the first fenced block in the input and retries with that content. The regex is only constructed and matched on the failure path, so the happy path pays no extra cost.
    static T FromLLMResponse<T>(string text, JsonSerializerOptions? options)
    // Like From , but tolerant of LLM responses that wrap the JSON payload in a markdown code fence. Tries direct deserialization first; on a JSON parse failure (from either System.Text.Json or Newtonsoft, depending on useJson5 ), looks for the first fenced block in the input and retries with that content.
    static T FromLLMResponse<T>(string text, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false, bool caseInsensitive = false)
    static Type? ResolveTypeByName(string typeName)
    static string To<T>(T obj, bool useJson5 = false, bool indentation = true, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false)
  class Log : AsyncLocalInstance<Log>
    ctor()
    IList<IScopeKey> CurrentScopes { get; }
    bool ShowTimeDelta { get; set; }
    void AddDefaultLogHandlers()
    void AddLogEvent(LogEvent logEvent)
    void AddScope(IScopeKey scope)
    IDisposable? BeginTimer(string name, LogType logType = Debug)
    IDisposable CreateAsyncFlow(string? description = null)
    void Critical(LogCriticalHandler handler)
    void Critical(string message)
    // Log a critical failure with an associated exception. Convenience overload for the .NET-conventional logger.Critical(message, exception) shape — the exception's full ToString() is appended to the message so stack traces land in the log without needing to interpolate ex into the message.
    void Critical(string message, Exception exception)
    // Log an exception with an associated message — same as Critical but with the exception first, matching the Serilog / Microsoft.Extensions.Logging idiom logger.LogCritical(ex, message).
    void Critical(Exception exception, string message)
    void Debug(LogDebugHandler handler)
    void Debug(string message)
    void DisableFileOutput()
    void EnableFileOutput(string filePath, bool append = false)
    void Error(LogErrorHandler handler)
    void Error(string message)
    // Log an error with an associated exception. Convenience overload for the .NET-conventional logger.Error(message, exception) shape — the exception's full ToString() is appended to the message so stack traces land in the log without needing to interpolate ex into the message.
    void Error(string message, Exception exception)
    // Log an exception with an associated message — same as Error but with the exception first, matching the Serilog / Microsoft.Extensions.Logging idiom logger.LogError(ex, message).
    void Error(Exception exception, string message)
    void Event(string name, object? parameters = null)
    string Exception(LogExceptionHandler handler)
    string Exception(string message)
    TScope GetScope<TScope>() where TScope : struct, IScopeKey
    IScopeKey GetScopeByName(string name)
    void Info(LogInfoHandler handler)
    void Info(string message)
    Task InitializeAsync()
    void LogMessage(LogType type, LogGeneralHandler handler)
    void LogMessage(LogType type, string message)
    void LogMessage2(LogType type, string filePath, int lineNumber, string memberName, LogGeneralHandler2 handler)
    void LogMessage2(LogType type, string filePath, int lineNumber, string memberName, string message)
    static LogParameter<T> Named<T>(string name, T value)
    void RemoveDefaultLogHandlers()
    static Sensitive<T> Sensitive<T>(T value, SensitivityPolicy sensitivityPolicy = Default)
    Task StopAsync()
    void Trace(LogTraceHandler handler)
    void Trace(string message)
    TScope? TryGetScope<TScope>() where TScope : struct, IScopeKey
    bool TryGetScope<TScope>(out TScope scope) where TScope : struct, IScopeKey
    IScopeKey? TryGetScopeByName(string name)
    void Usage(string usageName, double usage)
    void Usage(string usageName, Func<Task<double>> usageFunc)
    IDisposable UseScope(IScopeKey scope)
    IDisposable UseScopes(params IScopeKey[] scopes)
    Task WaitEmptyAsync()
    void Warning(LogWarningHandler handler)
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
    static event Log.AsyncFlowFinishedHandler? AsyncFlowFinished
    event Log.LogEventHandler? LogEvent
  // Interpolated string handler for Critical — pass an interpolated string at the call site; do not construct directly.
  ref struct LogCriticalHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  // Interpolated string handler for Debug — pass an interpolated string at the call site; do not construct directly.
  ref struct LogDebugHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  // Interpolated string handler for Error — pass an interpolated string at the call site; do not construct directly.
  ref struct LogErrorHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
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
  delegate Log.LogEventHandler
    void LogEventHandler(object sender, LogEvent logEvent)
  // Interpolated string handler for Exception — pass an interpolated string at the call site; do not construct directly.
  ref struct LogExceptionHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  enum LogFilter
    None
    Critical
    Error
    Warning
    Info
    Debug
    Trace
  // Interpolated string handler for LogMessage — pass an interpolated string at the call site; do not construct directly.
  ref struct LogGeneralHandler
    ctor(int literalLength, int formattedCount, Log log, LogType logType)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  // Interpolated string handler for LogMessage2 , the overload that takes explicit caller-site info — pass an interpolated string at the call site; do not construct directly.
  ref struct LogGeneralHandler2
    ctor(int literalLength, int formattedCount, Log log, LogType logType, string filePath, int lineNumber, string memberName)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  // Interpolated string handler for Info — pass an interpolated string at the call site; do not construct directly.
  ref struct LogInfoHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  struct LogParameter<T>
    ctor(string name, T value)
    string Name
    T Value
  struct LogScopeEntry
    string Id { get; set; }
    string Type { get; set; }
  // Interpolated string handler for Trace — pass an interpolated string at the call site; do not construct directly.
  ref struct LogTraceHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  // Interpolated string handler for Warning — pass an interpolated string at the call site; do not construct directly.
  ref struct LogWarningHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  // Provides optimized utility methods for converting strings between different naming conventions.
  static class NameConversions
    static string ToCamelCase(string input)
    static string ToDisplayName(string input)
    static string ToKebabCase(string input)
    static string ToPascalCase(string input)
    static string ToSlug(string input, int maxLength)
    static string ToSnakeCase(string input)
  struct LogEvent.Parameter
    ctor(string name, object? value)
    string Name
    object? Value
  sealed class PublicApiDocIgnoreAttribute : Attribute
    ctor()
  // A reactive version of the protocol GlobalState. Each property is wrapped in a ReactiveT so that any UI binding to it will update only when the value changes.
  class ReactiveGlobalState
    ctor()
    // Tells the source where the app is being run from
    Reactive<AppSourceType> AppSourceType { get; }
    // Active audio streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.AudioStreamState>> AudioStreams { get; }
    // Unique identifier for the channel within the space
    Reactive<string> ChannelId { get; }
    // Display name of the channel
    Reactive<string> ChannelName { get; }
    // URL for accessing the channel
    Reactive<string> ChannelUrl { get; }
    // All connected clients indexed by their client session ID, containing client metadata such as user ID, device info, viewport dimensions, and locale
    Reactive<Dictionary<int, Context>> Clients { get; }
    // Whether debug mode is enabled, providing additional logging and development features
    Reactive<bool> DebugMode { get; }
    // User ID of the first human user who joined this session, dynamically reassigned when that user leaves
    Reactive<string> FirstUserId { get; }
    // Registry of callable functions organized by client session ID
    Reactive<Dictionary<int, List<ActionFunctionRegister>>> Functions { get; }
    // Display name of the organization
    Reactive<string> OrganisationName { get; }
    // Static user ID of the session owner from server configuration, used for user-specific asset storage paths
    Reactive<string> PrimaryUserId { get; }
    // Tells whether the app is being run through publicly accessible endpoints (in local development)
    Reactive<bool> PublicAccess { get; }
    // Tells where the server is running from
    Reactive<ServerRunType> ServerRunType { get; }
    // Unique identifier of the specific Ikon server instance handling this session
    Reactive<string> ServerSessionId { get; }
    // Full URL with session identifier for direct access to current session
    Reactive<string> SessionChannelUrl { get; }
    // Hash derived from the session identity parameters
    Reactive<string> SessionHash { get; }
    // Unique identifier for the space where this session is running
    Reactive<string> SpaceId { get; }
    // Display name of the space
    Reactive<string> SpaceName { get; }
    // Active tracking streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.TrackingStreamState>> TrackingStreams { get; }
    // Active UI streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.UIStreamState>> UIStreams { get; }
    // Active video streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.VideoStreamState>> VideoStreams { get; }
    // Returns the client context against the clientSesssionId
    Context GetClientContext(int clientSessionId)
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
  // Read-only view of the space-scoped secrets (tokens, API keys, passwords) loaded from the Ikon backend. Apps receive a Secrets via app.Secrets; pipelines receive one via host.Secrets on IPipelineHost<TConfig>. Manage values from the CLI with ikon app secret set/list/delete. Rotating a secret while an app or pipeline is running only takes effect after a restart.
  sealed class Secrets
    // Returns the value for key , or throws InvalidOperationException if no secret with that key is set for this space.
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
  // String-distance utilities. Single home for primitives that otherwise breed private copies in every caller (Levenshtein had three implementations across Ikon.Code, Ikon.Agent.Codegen, and the MiniAgent app before being consolidated here).
  static class StringDistance
    // Standard-shape Levenshtein edit distance. Returns the minimum number of single-character insertions, deletions, or substitutions to turn a into b . Empty / null inputs return the length of the other side. O(|a|·|b|) time and memory; fine for the sub-100-char identifiers and filenames the platform compares.
    static int Levenshtein(string? a, string? b)
  static class Throttler
    static bool TryExecute(Action action, TimeSpan throttleInterval = default, string? extraKey = null)
  static class Toml
    static T From<T>(string toml) where T : class, new()
    static string To<T>(T obj) where T : class
  // Exception for user-facing errors that should be displayed cleanly without stack traces. Use this for expected error conditions like invalid input, missing files, or failed operations.
  class UserException : Exception
    ctor(string message)
    ctor(string message, Exception innerException)
  static class Utils
    // Deletes a directory tree, clearing ReadOnly attributes along the way (git marks its pack files read-only, which makes a plain Directory.Delete fail with access denied). Continues past individual failures instead of stopping at the first one and returns the paths that could not be deleted; an empty list means the directory is completely gone.
    static IReadOnlyList<string> DeleteDirectoryBestEffort(string path)
    static int FindAvailableTcpAndUdpPort(int startPort, HashSet<int>? usedPorts = null)
    static int FindAvailableUdpPortRange(int startPort, int count)
    static string GenerateDeviceId()
    static void OpenBrowser(string url)
    static bool TcpPortIsAvailable(int port)
    static bool UdpPortIsAvailable(int port)

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
    LocalFile
    EmbeddedFile
    CloudFile
    CloudFilePublic
    CloudJson
    CloudProfile
  sealed class AssetContent<T> : IDisposable
    ctor(T content, AssetMetadata? metaData = null)
    T Content { get; }
    AssetMetadata? MetaData { get; }
    void Dispose()
  class AssetEventArgs : EventArgs
    ctor(AssetUri assetUri, AssetStatus status)
    AssetUri AssetUri { get; }
    AssetStatus Status { get; }
  struct AssetListingEntry
    ctor(AssetUri assetUri, AssetMetadata metadata)
    AssetUri AssetUri { get; }
    AssetMetadata Metadata { get; }
  struct AssetMetadata
    ctor(string? mimeType = null, long? size = null, DateTime? lastModified = null, string? url = null, bool? urlIsTemporal = null, string[]? tags = null, string? internalPath = null, string? storageId = null, string? nativeUri = null, bool? isAppServed = null, DateTime? expiresAt = null)
    DateTime? ExpiresAt { get; }
    string? InternalPath { get; }
    bool? IsAppServed { get; }
    DateTime? LastModified { get; }
    string? MimeType { get; }
    string? NativeUri { get; }
    long? Size { get; }
    string? StorageId { get; }
    string[]? Tags { get; }
    string? Url { get; }
    bool? UrlIsTemporal { get; }
  sealed class AssetQuery
    ctor(AssetClass assetClass)
    ctor(AssetUri folderUri)
    string? ChannelId { get; set; }
    AssetClass Class { get; }
    string? ContinuationToken { get; set; }
    string? EffectiveChannelId { get; }
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
  // AssetUris are used to store and retrieve data on the Ikon platform. Use the asset class to select the storage backend. Space ID, User ID, and Channel ID are optional identifiers to scope the asset. Path is the location of the asset within the storage backend. It may include subdirectories and/or a file name. Query is optional and is not used for now. Example asset URIs: assets://space/12345/user/67890/channel/12345/cloud-file/images/photos/pic1.jpg assets://cloud-json/config/settings.json assets://space/12345/local-file/documents/report.pdf assets://embedded-file/logo.png
  struct AssetUri : IEquatable<AssetUri>
    ctor(string uriString)
    ctor(AssetClass assetClass, string? path = null, string? spaceId = null, string? userId = null, string? channelId = null, string? query = null)
    string? ChannelId { get; }
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
    AssetUri With(AssetClass? assetClass = null, string? path = null, string? spaceId = null, string? userId = null, string? channelId = null, string? query = null)
  // Serializes AssetUri as its canonical URI string so it round-trips correctly. Without this, System.Text.Json cannot reconstruct the immutable get-only struct and falls back to default(AssetUri) on deserialization (losing the path, class, and scope identifiers).
  sealed class AssetUriJsonConverter : JsonConverter<AssetUri>
    ctor()
    override AssetUri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    override void Write(Utf8JsonWriter writer, AssetUri value, JsonSerializerOptions options)
  struct AssetWriteResult
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
    abstract void SetSha256Hash(string? hash)
  interface IStorage : IAsyncDisposable
    abstract Task DeleteAsync(AssetUri assetUri)
    abstract Task<bool> ExistsAsync(AssetUri assetUri)
    abstract Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    abstract Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata, CancellationToken cancellationToken)
    abstract Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken)
    abstract Task StartAsync()
    abstract Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    abstract Task WaitUntilQueueEmptyAsync()
    event Func<AssetEventArgs, Task> AssetEventAsync
  static class StorageExtensions
    static Task AddEmbeddedFileStorageAsync(this Asset asset, Assembly? assembly = null, string resourceNamespace = "")

namespace Ikon.Common.Core.Auth
  sealed class StepUpAssertion : IEquatable<StepUpAssertion>
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
  sealed class EmailAddress : IEquatable<EmailAddress>
    ctor(string Email, string? Name, string? Subaddress)
    string Email { get; init; }
    string? Name { get; init; }
    string? Subaddress { get; init; }
  // Represents a single attachment on an outgoing app email. Bytes is the raw binary content; the platform encodes it as base64 before sending it on the wire.
  sealed class EmailAttachment : IEquatable<EmailAttachment>
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
  sealed class EmailHeader : IEquatable<EmailHeader>
    ctor(string Name, string Value)
    string Name { get; init; }
    string Value { get; init; }
  // Specification for a custom email sent by an app through the platform mailer. The platform enqueues the send for asynchronous delivery and returns once the request has been accepted; transient delivery failures are retried server-side.
  sealed class EmailSendRequest : IEquatable<EmailSendRequest>
    ctor(string To, string Subject, string HtmlBody, string? TextBody = null, string? ReplyTo = null, IReadOnlyList<EmailAttachment>? Attachments = null, IReadOnlyDictionary<string, string>? Metadata = null)
    // Optional list of binary attachments. Up to 10 per email.
    IReadOnlyList<EmailAttachment>? Attachments { get; init; }
    // Pre-rendered HTML body of the email.
    string HtmlBody { get; init; }
    // Optional string key/value pairs forwarded to the mail provider for tracking.
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    // Optional Reply-To address. The visible From address is set by the platform.
    string? ReplyTo { get; init; }
    // Email subject line.
    string Subject { get; init; }
    // Optional plain-text fallback for clients that do not render HTML.
    string? TextBody { get; init; }
    // Recipient email address.
    string To { get; init; }
  // Lightweight metadata for an inbound email's attachment — does not include the body bytes. Fetch the body via the email service's DownloadAttachmentAsync.
  sealed class InboundAttachmentInfo : IEquatable<InboundAttachmentInfo>
    ctor(string Id, string Filename, string MimeType, long Size)
    string Filename { get; init; }
    string Id { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
  // Full inbound email with decrypted body and parsed envelope. Attachments expose metadata only; fetch each one via the email service's DownloadAttachmentAsync.
  sealed class InboundEmailDetail : IEquatable<InboundEmailDetail>
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
  sealed class InboundEmailSummary : IEquatable<InboundEmailSummary>
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
  sealed class InboxPage : IEquatable<InboxPage>
    ctor(IReadOnlyList<InboundEmailSummary> Items, string? NextCursor)
    IReadOnlyList<InboundEmailSummary> Items { get; init; }
    string? NextCursor { get; init; }
  // Filter and pagination parameters for an inbox listing.
  sealed class InboxQuery : IEquatable<InboxQuery>
    ctor()
    // Opaque cursor returned by a previous NextCursor . null requests the first page.
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
    Sync
    Async
    AsyncEnumerable
  // Immutable representation of a function with metadata and optional callbacks. Consolidates FunctionInfo, RegisteredFunction, and KernelContext.Function into a single type.
  struct Function
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
    object TypeId { get; }
    // Whether the function should be distributed to other clients. If not set, defaults to Local for standalone functions, or inherits from [RegisterAll] for methods in a class with that attribute.
    FunctionVisibility Visibility { get; set; }
  // Per-call ambient context exposed to the body of a function dispatched by FunctionRegistry . Set by the registry's inbound dispatch path before invoking the function and cleared after.
  static class FunctionCallContext
    // The session id of the client that issued the current function call, or null when the call did not originate from a remote client (e.g. local in-process invocation).
    static int? CallerSessionId { get; }
  sealed class FunctionCallException : Exception
    ctor(string message, string remoteTypeName, string remoteStackTrace)
    ctor(string message, string remoteTypeName, string remoteStackTrace, Exception? innerException)
    string RemoteStackTrace { get; }
    string RemoteTypeName { get; }
    static string RemoteFunctionCallerNotSetTypeName
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
  class FunctionRegistry : AsyncLocalInstance<FunctionRegistry>
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
    // Optional resolver that maps a caller session id to the reactive scopes that should be active during the function body's execution — typically [ClientScope, UserScope] derived from the caller's Context . Wired by the host (e.g. Ikon.App.App) so that ClientReactive and UserReactive resolve naturally without the function body having to push scopes manually via FunctionCallContext.CallerSessionId + Use .
    Func<int, IReadOnlyList<IScopeKey>>? ScopeResolver { get; set; }
    // Optional resolver that maps a caller session id to the user id associated with that session. Wired by the host (e.g. Ikon.App.App) so that policy evaluation has access to the caller's identity. Returns null for unknown sessions or unauthenticated (guest) callers.
    Func<int, string?>? UserIdResolver { get; set; }
    void AddFunction(Function function, FunctionVisibility? visibilityOverride = null)
    // Hooks the registry to a protocol channel so that remote function calls and registrations are handled automatically.
    Task AttachProtocolAsync(IProtocolMessageChannel channel, int senderId)
    TResult Call<TResult>(string name, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
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
    // Invoke an already-resolved local function with a pre-built positional argument array, bypassing the argument-type resolution that CallAsync performs. The args must already line up with the function's parameter list — used by callers that inject host-supplied parameters (e.g. a cron trigger building the array from MethodInfo to inject a context object). Returns the result, if any.
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
    // Sends registrations for all functions and processes pending registrations.
    Task StartProtocolAsync()
    // Stops protocol handling but keeps the channel attached. Pending registrations are cleared.
    Task StopProtocolAsync()
    void SyncFunctionsFromGlobalState(GlobalState globalState)
    // Tries to get a function with the given name.
    bool TryGetFunction(string name, out Function? function)
    // Waits for a function with the given name to be registered.
    Task<bool> WaitForFunctionAsync(string functionName, TimeSpan timeout = default, CancellationToken ct = default)
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
  // Determines whether a function is advertised over the protocol so remote clients can call it. This is a dispatch-scope axis only — auth gating is a separate concern declared via policy attributes ([RequireLogin], [AllowAnonymous], [RequireRole], ...).
  enum FunctionVisibility
    Local
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
  // Represents a decision to allow the function call to proceed.
  sealed class PolicyDecision.Allow : PolicyDecision
  // Marks an External function as deliberately callable without authentication. Pure marker — does not inject a policy, only documents intent and silences the startup audit warning for External functions that have no auth policy attached.
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
    static (ApprovalContext Context, Guid RawToken) Create(string functionName, string reason, object?[] args, PolicyCallContext callContext, int timeoutSeconds = 300)
    // Checks if this approval request has expired.
    bool IsExpired()
    // Validates that a provided token matches this context. Uses constant-time comparison of hashes to prevent timing attacks.
    bool ValidateToken(Guid providedToken)
    // Validates that a provided token string matches this context.
    bool ValidateToken(string providedToken)
  // Delegate type for approval handlers that process approval requests.
  delegate ApprovalHandlerDelegate
    Task<ApprovalResult> ApprovalHandlerDelegate(ApprovalContext context)
  // The result of an approval request returned by approval handlers.
  struct ApprovalResult
    // True if the request was approved.
    bool IsApproved { get; }
    // The reason for rejection, if applicable.
    string? RejectionReason { get; }
    // Creates an approved result.
    static ApprovalResult Approved()
    // Creates a rejected result with an optional reason.
    static ApprovalResult Rejected(string? reason = null)
    override string ToString()
  // Specifies who should receive the approval request.
  enum ApproverType
    Caller
    SpecificClient
    SpecificUser
  // Represents a decision to deny the function call.
  sealed class PolicyDecision.Deny : PolicyDecision
    // Optional error code for programmatic handling (e.g., "rate_limit_exceeded").
    string? Code { get; }
    // The reason for denying the function call.
    string Reason { get; }
  // Interface for function policies that can be evaluated before function execution.
  interface IFunctionPolicy
    // The name of this policy (used for logging and error messages).
    string Name { get; }
    // The priority of this policy. Lower values are evaluated first. Default priority is 100.
    int Priority { get; }
    // Evaluates the policy for a function call.
    abstract ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
  // Interface for checking usage limits before function execution.
  interface IUsageLimitChecker
    // Checks if the call should be allowed based on usage limits.
    abstract ValueTask<UsageLimitCheckResult> CheckAsync(PolicyCallContext context, object?[] args)
  // Denies a function call when the caller has no authenticated session.
  sealed class LoggedInPolicy : IFunctionPolicy
    ctor()
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    static string LoginRequiredCode
  // Represents a decision that requires approval before the function can execute.
  sealed class PolicyDecision.NeedsApproval : PolicyDecision
    // How long the approval request is valid, in seconds (minimum 30, default 300).
    int ExpirySeconds { get; }
    // Optional custom handler for processing the approval request.
    ApprovalHandlerDelegate? Handler { get; }
    // The message explaining why approval is required.
    string Message { get; }
  // A policy that maintains separate rate limits per caller session.
  sealed class PerSessionRateLimitPolicy : IFunctionPolicy
    // Creates a new per-session rate limit policy.
    ctor(int limit, int windowSeconds, string? name = null, int priority = 50)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a PolicyDelegate from this policy.
    PolicyDelegate ToDelegate()
  // Helper methods for extracting typed arguments from policy evaluation arguments.
  static class PolicyArgs
    // Checks if all required arguments are present at the specified indices.
    static bool HasAll(object?[] args, params int[] requiredIndices)
    // Gets an optional argument at the specified index, returning a default if missing.
    static T? Optional<T>(object?[] args, int index, T? defaultValue = default)
    // Gets a required argument at the specified index, throwing if missing or wrong type.
    static T Required<T>(object?[] args, int index)
    // Tries to get an argument at the specified index.
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
    ctor(Guid callId, string functionName, int callerSessionId, string? userId, string? tenantId, Guid? instanceId, bool isInternal, CancellationToken cancellationToken, string? authSessionId = null, DateTime? callTimestamp = null, IReadOnlyDictionary<string, object?>? additionalContext = null)
    // Additional context data that may have been provided with the call.
    IReadOnlyDictionary<string, object?>? AdditionalContext { get; }
    // The auth session ID of the caller, if available. Empty or null for unauthenticated (guest) callers.
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
    // True if this call originated from the same process (internal call).
    bool IsInternal { get; }
    // The tenant ID, if available.
    string? TenantId { get; }
    // The user ID of the caller, if available.
    string? UserId { get; }
  // Provides utilities for composing multiple policies into a single policy.
  static class PolicyChain
    // Creates a policy that requires all provided policies to allow. Policies are evaluated in priority order (lower priority = evaluated first). Evaluation stops at the first non-Allow decision.
    static IFunctionPolicy All(params IFunctionPolicy[] policies)
    // Creates a PolicyDelegate that requires all provided policies to allow.
    static PolicyDelegate AllAsDelegate(params IFunctionPolicy[] policies)
  // Represents a policy decision about whether a function call should be allowed. This is a discriminated union with three possible states: Allow, Deny, or NeedsApproval. Use pattern matching to handle the different cases.
  abstract class PolicyDecision
    // Creates an Allow decision.
    static PolicyDecision Allowed()
    // Creates a Deny decision with a reason and optional error code.
    static PolicyDecision Denied(string reason, string? code = null)
    // Creates a RequireApproval decision with default expiry.
    static PolicyDecision RequireApproval(string message)
    // Creates a RequireApproval decision with custom expiry.
    static PolicyDecision RequireApproval(string message, int expirySeconds)
    // Creates a RequireApproval decision with a custom approval handler.
    static PolicyDecision RequireApproval(string message, ApprovalHandlerDelegate handler)
    // Creates a RequireApproval decision with custom expiry and handler.
    static PolicyDecision RequireApproval(string message, int expirySeconds, ApprovalHandlerDelegate handler)
    // Default expiry time for approval requests in seconds.
    static int DefaultExpirySeconds
    // Minimum expiry time for approval requests in seconds.
    static int MinExpirySeconds
  // Delegate type for policy evaluation.
  delegate PolicyDelegate
    ValueTask<PolicyDecision> PolicyDelegate(object?[] args, PolicyCallContext context)
  // Exception thrown when a function call is denied by a policy.
  sealed class PolicyDeniedException : Exception
    // Creates a new PolicyDeniedException with just a reason.
    ctor(string? reason)
    // Creates a new PolicyDeniedException with a reason and error code.
    ctor(string? reason, string? code)
    // Creates a new PolicyDeniedException with an error code, policy name, and function name.
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
    ctor(Type policyType)
    // The type of policy to create.
    Type PolicyType { get; }
    override IFunctionPolicy CreatePolicy()
  // Applies a rate limit policy to the function.
  sealed class RateLimitAttribute : PolicyAttribute
    // Creates a new rate limit attribute.
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
    ctor(string reason, string? name = null, int priority = 100)
    // Creates a new require approval policy with a custom approval handler.
    ctor(string reason, ApprovalHandlerDelegate handler, string? name = null, int priority = 100)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a new require approval policy that asks a specific client.
    static RequireApprovalPolicy ForClient(string reason, int clientSessionId, string? name = null, int priority = 100)
    // Creates a new require approval policy that asks a specific user.
    static RequireApprovalPolicy ForUser(string reason, string userId, string? name = null, int priority = 100)
    // Creates a PolicyDelegate from this policy.
    PolicyDelegate ToDelegate()
  // Marks a function as requiring an authenticated user session.
  sealed class RequireLoginAttribute : PolicyAttribute
    ctor()
    override IFunctionPolicy CreatePolicy()
  // Requires the caller to hold one (or all, when RequireAll is true) of the specified roles. Roles are sourced from PolicyCallContext.AdditionalContext["user_roles"], which the dispatcher populates via RolesResolver .
  sealed class RequireRoleAttribute : PolicyAttribute
    ctor(params string[] roles)
    // When false (default), the caller passes if they hold ANY of the listed roles. When true, the caller must hold ALL listed roles.
    bool RequireAll { get; set; }
    // The roles the caller must hold (any or all, see RequireAll ).
    string[] RequiredRoles { get; }
    override IFunctionPolicy CreatePolicy()
  // Policy that denies the call unless the caller has the required role(s). Roles are read from PolicyCallContext.AdditionalContext["user_roles"].
  sealed class RoleBasedPolicy : IFunctionPolicy
    ctor(string[] required, bool requireAll, int priority)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    static string MissingRoleCode
    static string RolesContextKey
  // Applies a usage limit policy to the function.
  sealed class UsageLimitAttribute : PolicyAttribute
    // Creates a new usage limit attribute with the specified checker type.
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
    static UsageLimitCheckResult Deny(string reason, string? code = "usage_limit_exceeded")
  // A policy that checks for available credits/quota before execution.
  sealed class UsageLimitPolicy : IFunctionPolicy
    // Creates a new usage limit policy with the specified checker.
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
    static ActionFunctionRegister ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionRegister ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionRegister? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum AppSourceType
    Bundle
    GitSource
  enum AudioCodec
    Unknown
    Opus
    Mp3
    RawPcm16
  sealed class AudioStreamBegin.AudioShapeSet
    ctor()
    ctor(uint setId, string name, List<string> shapeNames)
    string Name { get; set; }
    uint SetId { get; set; }
    List<string> ShapeNames { get; set; }
    static AudioStreamBegin.AudioShapeSet ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioStreamBegin.AudioShapeSet ReadFromTeleport(ReadOnlySpan<byte> data, AudioStreamBegin.AudioShapeSet? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static AudioStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data, AudioStreamBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static GlobalState.AudioStreamState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState.AudioStreamState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState.AudioStreamState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AuthResponse : IProtocolMessagePayload
    ctor()
    ctor(Context clientContext, Context serverContext, string certHash, List<Entrypoint> entrypoints, Dictionary<string, bool> featureFlags, string spaceId, string channelId, string channelInstanceId, string primaryUserId, string serverSessionId, int keepaliveTimeoutMs, int serverCapability)
    string CertHash { get; set; }
    string ChannelId { get; set; }
    string ChannelInstanceId { get; set; }
    Context ClientContext { get; set; }
    List<Entrypoint> Entrypoints { get; set; }
    Dictionary<string, bool> FeatureFlags { get; set; }
    int KeepaliveTimeoutMs { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PrimaryUserId { get; set; }
    int ServerCapability { get; set; }
    Context ServerContext { get; set; }
    string ServerSessionId { get; set; }
    string SpaceId { get; set; }
    static AuthResponse ReadFromTeleport(ReadOnlySpan<byte> data)
    static AuthResponse ReadFromTeleport(ReadOnlySpan<byte> data, AuthResponse? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ClientInitialization : IProtocolMessagePayload
    ctor()
    ctor(Dictionary<int, List<ActionFunctionRegister>> functions)
    Dictionary<int, List<ActionFunctionRegister>> Functions { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ClientInitialization ReadFromTeleport(ReadOnlySpan<byte> data)
    static ClientInitialization ReadFromTeleport(ReadOnlySpan<byte> data, ClientInitialization? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ClientType
    Unknown
    MobileWeb
    MobileApp
    DesktopWeb
    DesktopApp
  sealed class Context : IProtocolMessagePayload
    ctor()
    ctor(ContextType contextType, UserType userType, PayloadType payloadType, string description, string userId, string deviceId, string productId, string versionId, string installId, string locale, int sessionId, bool isInternal, bool isSnapshot, bool isReady, bool hasInput, string channelLocale, string embeddedSpaceId, string authSessionId, bool receiveAllMessages, ulong preciseJoinedAt, string userAgent, ClientType clientType, string uniqueSessionId, Dictionary<string, string> parameters, SdkType sdkType, int sdkCapability, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, StyleFormat styleFormat, bool supportsCompression, bool isSoftDisconnected, ulong softDisconnectAt)
    string AuthSessionId { get; set; }
    string ChannelLocale { get; set; }
    // Alias for SessionId . The protocol surfaces this same int as ClientSessionId on event-args types like ClientJoinedEventArgs.ClientSessionId — code generated against the event-args shape naturally reaches for ctx.ClientSessionId after switching to the Context directly. Provide both names so the natural reach resolves without renaming.
    int ClientSessionId { get; }
    ClientType ClientType { get; set; }
    ContextType ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    string EmbeddedSpaceId { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    string InstallId { get; set; }
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
    static Context ReadFromTeleport(ReadOnlySpan<byte> data)
    static Context ReadFromTeleport(ReadOnlySpan<byte> data, Context? destination)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static Entrypoint ReadFromTeleport(ReadOnlySpan<byte> data)
    static Entrypoint ReadFromTeleport(ReadOnlySpan<byte> data, Entrypoint? destination)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static Ikon.Common.Core.Protocol.FunctionParameter ReadFromTeleport(ReadOnlySpan<byte> data)
    static Ikon.Common.Core.Protocol.FunctionParameter ReadFromTeleport(ReadOnlySpan<byte> data, Ikon.Common.Core.Protocol.FunctionParameter? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static ActionFunctionRegister.FunctionRegisterParameter ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionRegister.FunctionRegisterParameter ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionRegister.FunctionRegisterParameter? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  // Shared state synchronized across all clients and the server, providing access to connected clients, registered functions, active media streams, and session metadata
  sealed class GlobalState : ILogInfo, IProtocolMessagePayload
    ctor()
    ctor(Dictionary<int, Context> clients, Dictionary<int, List<ActionFunctionRegister>> functions, Dictionary<string, GlobalState.UIStreamState> uiStreams, Dictionary<string, GlobalState.AudioStreamState> audioStreams, Dictionary<string, GlobalState.VideoStreamState> videoStreams, Dictionary<string, GlobalState.TrackingStreamState> trackingStreams, string spaceId, string channelId, string serverSessionId, string sessionHash, string channelUrl, string sessionChannelUrl, string firstUserId, string primaryUserId, string organisationName, string spaceName, string channelName, ServerRunType serverRunType, AppSourceType appSourceType, bool publicAccess, bool debugMode)
    // Tells the source where the app is being run from
    AppSourceType AppSourceType { get; set; }
    // Active audio streams indexed by stream ID
    Dictionary<string, GlobalState.AudioStreamState> AudioStreams { get; set; }
    // Unique identifier for the channel within the space
    string ChannelId { get; set; }
    // Display name of the channel
    string ChannelName { get; set; }
    // URL for accessing the channel
    string ChannelUrl { get; set; }
    // All connected clients indexed by their client session ID, containing client metadata such as user ID, device info, viewport dimensions, and locale
    Dictionary<int, Context> Clients { get; set; }
    // Whether debug mode is enabled, providing additional logging and development features
    bool DebugMode { get; set; }
    // User ID of the first human user who joined this session, dynamically reassigned when that user leaves
    string FirstUserId { get; set; }
    // Registry of callable functions organized by client session ID
    Dictionary<int, List<ActionFunctionRegister>> Functions { get; set; }
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
    // Unique identifier of the specific Ikon server instance handling this session
    string ServerSessionId { get; set; }
    // Full URL with session identifier for direct access to current session
    string SessionChannelUrl { get; set; }
    // Hash derived from the session identity parameters
    string SessionHash { get; set; }
    // Unique identifier for the space where this session is running
    string SpaceId { get; set; }
    // Display name of the space
    string SpaceName { get; set; }
    // Active tracking streams indexed by stream ID
    Dictionary<string, GlobalState.TrackingStreamState> TrackingStreams { get; set; }
    // Active UI streams indexed by stream ID
    Dictionary<string, GlobalState.UIStreamState> UIStreams { get; set; }
    // Active video streams indexed by stream ID
    Dictionary<string, GlobalState.VideoStreamState> VideoStreams { get; set; }
    void AddAudioStream(GlobalState.AudioStreamState audioStreamState)
    void AddClient(Context clientContext)
    void AddFunction(int clientSessionId, ActionFunctionRegister function)
    void AddTrackingStream(GlobalState.TrackingStreamState trackingStreamState)
    void AddUIStream(GlobalState.UIStreamState uiStreamState)
    void AddVideoStream(GlobalState.VideoStreamState videoStreamState)
    Context GetClientContext(int clientSessionId)
    Context GetClientContext(string userId)
    int GetClientSessionId(string userId)
    int[] GetClientSessionIds()
    int[] GetClientSessionIdsByProductId(string productId)
    int[] GetClientSessionIdsExcept(int[] clientSessionIds)
    int[] GetHumanClientSessionIds()
    int[] GetMachineClientSessionIds()
    List<string>? GetUserIds(IEnumerable<int> targetIds)
    static GlobalState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState? destination)
    void RemoveAudioStream(string streamId)
    void RemoveClient(int clientSessionId)
    void RemoveFunction(Guid functionId)
    void RemoveTrackingStream(string streamId)
    void RemoveUIStream(string streamId)
    void RemoveVideoStream(string streamId)
    void SetReady(int clientSessionId)
    void SetReconnected(int clientSessionId)
    void SetSoftDisconnected(int clientSessionId, ulong softDisconnectAt)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface IProtocolMessagePayload
    MessageFlag MessageDefaultFlags { get; }
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
    CORE_SERVER_START
    CORE_SERVER_STOP
    CORE_ON_HOSTED_SERVER_EXIT
    CORE_DYNAMIC_CONFIG
    CORE_PROXY_RPC_AUTH_TICKET
    CORE_SERVER_INIT2
    CORE_UPDATE_CLIENT_CONTEXT
    CORE_BACKGROUND_WORK_ACTIVE
    CORE_RESET_IDLE
    CORE_CLIENT_DISCONNECTING
    CORE_ON_APP_READY
    CORE_ON_FRONTEND_RELOADED
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
    EVENTS_CHANNEL_COMPLETE
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
    ACTION_FILE_UPLOAD_BEGIN
    ACTION_FILE_UPLOAD_DATA
    ACTION_FILE_UPLOAD_ACK
    ACTION_FILE_UPLOAD_END
    ACTION_FILE_UPLOAD_RESULT
    ACTION_OPEN_CHANNEL
    ACTION_OPEN_EXTERNAL_URL
    ACTION_UI_OPEN_VIEW
    ACTION_UI_CLOSE_VIEW
    ACTION_UI_BLOCKING_BEGIN
    ACTION_UI_BLOCKING_END
    ACTION_UI_UPDATE_TEXT_DELTA
    ACTION_UI_DELETE_CONTAINER
    ACTION_UPDATE_GFX_SHADER
    ACTION_FUNCTION_REGISTER
    ACTION_FUNCTION_CALL
    ACTION_FUNCTION_RESULT
    ACTION_GENERATE_ANSWER
    ACTION_REGENERATE_ANSWER
    ACTION_CLEAR_CHAT_MESSAGE_HISTORY
    ACTION_CLEAR_STATE
    ACTION_RELOAD_CHANNELS
    ACTION_RELOAD_PROFILE
    ACTION_CLASSIFICATION_RESULT
    ACTION_AUDIO_STOP
    ACTION_CALL_TEXT
    ACTION_RELOAD_APPLICATION
    ACTION_CANCEL_GENERATION
    ACTION_UI_SET_CONTAINER_STABLE
    ACTION_SPEECH_RECOGNIZED
    ACTION_CALL_RESULT
    ACTION_RELOAD_PROVIDER
    ACTION_DOWNLOAD
    ACTION_SCROLL_TO_CONTAINER
    ACTION_UI_CLEAR_STREAM
    ACTION_PLAY_SOUND
    ACTION_ENTER_FULLSCREEN
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
    ACTION_TRIGGER_GIT_PULL
    ACTION_FILE_UPLOAD_CALLBACK
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
    GROUP_UI
    UI_STREAM_BEGIN
    UI_STREAM_END
    UI_CONTAINER_BEGIN
    UI_CONTAINER_END
    UI_SECTION_BEGIN
    UI_SECTION_END
    UI_LIST_BEGIN
    UI_LIST_ITEM
    UI_LIST_END
    UI_TEXT
    UI_HEADER
    UI_SEPARATOR
    UI_BUTTON
    UI_ICON_BUTTON
    UI_IMAGE
    UI_FILE
    UI_BADGE
    UI_CONTENT_LINK
    UI_MAP
    UI_VEGA_CHART
    UI_ICON
    UI_FILE_UPLOAD_SECTION_BEGIN
    UI_FILE_UPLOAD_SECTION_END
    UI_MATERIAL_SYMBOL
    UI_BUTTON_BEGIN
    UI_BUTTON_END
    UI_CONTAINER_DELETE
    UI_INPUT_TEXT
    UI_PROGRESS_BAR
    UI_UPDATE_BEGIN
    UI_UPDATE_END
    UI_AUTOCOMPLETE
    UI_CHECKBOX
    UI_QS
    UI_ELEMENT
    UI_STYLES
    UI_SVG
    UI_UPDATE
    UI_INIT
    UI_STYLES_BATCH
    UI_STYLES_DELETE
    GROUP_COMMON
    GROUP_AUDIO
    AUDIO_STREAM_BEGIN
    AUDIO_STREAM_END
    AUDIO_FRAME
    AUDIO_FRAME_VOLUME
    AUDIO_FRAME2
    AUDIO_SHAPE_FRAME
    GROUP_VIDEO
    VIDEO_STREAM_BEGIN
    VIDEO_STREAM_END
    VIDEO_FRAME
    VIDEO_REQUEST_IDR_FRAME
    VIDEO_INVALIDATE_FRAME
    GROUP_TRACKING
    TRACKING_STREAM_BEGIN
    TRACKING_STREAM_END
    TRACKING_FRAME
    GROUP_SCENE
    SCENE_MESH
    SCENE_ARRAY
    GROUP_ALL
    GROUP_APP_LOCAL
    CONSTANT_GROUP_MASK
  static class Opcodes
    static bool IsOpcodeInAnyGroup(Opcode opcode, Opcode groups)
  static class PayloadCompression
    static (byte[]? Buffer, int Length) Compress(ReadOnlySpan<byte> data)
    static (byte[] Buffer, int Length) Decompress(ReadOnlySpan<byte> compressedData, int estimatedSize = 0)
    static void ReturnBuffer(byte[]? buffer)
    static bool ShouldCompress(int payloadSize)
    static int CompressionThreshold
  enum PayloadType
    Unknown
    MessagePack
    MemoryPack
    Json
    Teleport
    All
  class ProtocolMessage : AsyncLocalInstance<ProtocolMessage>
    ctor()
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
    static ProtocolMessage Create(int senderId, IProtocolMessagePayload payload, PayloadType payloadType = Unknown, int trackId = 0, int sequenceId = 0, MessageFlag flags = None, IReadOnlyList<int>? targetIds = null, bool compress = false)
    T GetPayload<T>() where T : IProtocolMessagePayload
    IProtocolMessagePayload GetPayload()
    static ProtocolMessage ModifyMessage(ProtocolMessage message, int? senderId = null, int? trackId = null, int? sequenceId = null, MessageFlag? flags = null, IReadOnlyList<int>? targetIds = null)
    static ProtocolMessage ModifyPayload(IProtocolMessagePayload payload, ProtocolMessage message, PayloadType payloadType = Unknown)
    // Register an app-local message type (an app's own schema/*.tp type, opcode in GROUP_APP_LOCAL ) at runtime. Called from the generated type's static constructor — app-local types are compiled into the app assembly and are not visible to the platform's compile-time ProtocolMessage source generator.
    static void RegisterAppLocalMessageType(Type type, Opcode opcode, int version)
    override string ToString()
    static ProtocolMessage WithFlags(ProtocolMessage message, MessageFlag additionalFlags)
    PayloadType DefaultPayloadType
    static int MaxMessageSize
    static int MinimumHeaderLength
    static Dictionary<Opcode, Type> OpcodeToType
    static Dictionary<Type, Opcode> TypeToOpcode
    static Dictionary<Type, int> TypeToVersion
  class ProtocolMessageAttribute : Attribute
    ctor(int version = 0, Opcode opcode = NONE, bool unreliable = false)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    bool Unreliable { get; }
  static class ProtocolVersion
    static int Version { get; }
  // Capability levels advertised by a connecting SDK via SdkCapability (companion to SdkType ). Opaque and monotonically increasing — bump when adding a capability the ikon server must detect per connected client. 0 means a legacy client that predates capability negotiation.
  static class SdkCapabilities
    // Client handles the CORE_CLIENT_INITIALIZATION message — the server/app function registry the server sends out-of-band right after the joining client's GlobalState — and registers those functions during connect. When any connected client advertises less than this, the server keeps the function registry embedded in GlobalState.Functions for the whole session so the older client can still learn server functions. This is a distinct level from FunctionRegistryOutsideGlobalState because the ClientInitialization message was introduced after it: clients advertising only levels 1-3 cannot parse it and would silently receive no functions if the server stripped them from GlobalState.
    static int ClientInitializationMessage
    // Client understands the batched CORE_CLIENT_LIFECYCLE_BATCH message (client joined/ready/left and user joined/left events coalesced into one payload) and unpacks it into the individual events. When all connected external clients advertise at least this, the server coalesces and debounces those broadcasts to external clients instead of one fan-out message per event; otherwise it falls back to per-event broadcasts. Internal (localhost) clients always receive the events immediately, unbatched.
    static int ClientLifecycleBatching
    // The highest capability level this build supports; advertised by first-party SDKs and the server itself.
    static int Current
    // Client understands server functions delivered out-of-band (the original targeted ACTION_FUNCTION_REGISTER_BATCH on join) rather than embedded in GlobalState.Functions. Superseded by ClientInitializationMessage : the out-of-band delivery is now the CORE_CLIENT_INITIALIZATION message, which a level-1 client does NOT understand. Do not gate the functions-out-of-GlobalState decision on this level — it is too low and matches clients that predate the ClientInitialization message.
    static int FunctionRegistryOutsideGlobalState
    // Client honors the keepalive watchdog timeout communicated by the server in AuthResponse.KeepaliveTimeoutMs instead of hard-coding it. When all connected clients advertise at least this, the server may stretch its keepalive send interval well beyond the legacy client's fixed watchdog; otherwise it stays within the legacy-safe cap.
    static int KeepaliveTimeoutNegotiation
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
    static int ClientInitializationMessage
    // The highest capability level this server build supports; advertised in AuthResponse.
    static int Current
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
  sealed class TrackingStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string category, TrackingType type, List<string> faceBlendshapes)
    string Category { get; set; }
    List<string> FaceBlendshapes { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    TrackingType Type { get; set; }
    static TrackingStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static TrackingStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data, TrackingStreamBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class GlobalState.TrackingStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, TrackingStreamBegin info)
    // Session ID of the client sending this stream
    int ClientSessionId { get; set; }
    // Stream configuration including tracking type (face, hands, pose)
    TrackingStreamBegin Info { get; set; }
    // Unique identifier for this stream
    string StreamId { get; set; }
    // Track identifier within the client
    int TrackId { get; set; }
    static GlobalState.TrackingStreamState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState.TrackingStreamState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState.TrackingStreamState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum TrackingType
    Face
    Hands
    Pose
    All
  static class UIElementLabels
    static string Blur
    static string ChatMessage
    static string Disabled
    static string ImageAvatar
    static string Markdown
    static string SizeExtraSmall
    static string SizeFitContent
    static string SizeFullWidth
    static string SizeLarge
    static string SizeMedium
    static string SizeSmall
    static string Wrap
  sealed class UIStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string category)
    string Category { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data, UIStreamBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class UIStreamCategories
    static string App
    static string Chat
    static string Collapsed
    static string DebugOverlay
    static string Footer
    static string Header
    static string Input
    static string Menu
    static string Overlay
    static string Preview
    static string SecondScreen
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
    static GlobalState.UIStreamState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState.UIStreamState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState.UIStreamState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class UIStylesKeys
    static string Common
    static string Crosswind
    static string Css
    static string Flutter
    static string ReactNative
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
    static VideoStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static VideoStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data, VideoStreamBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static GlobalState.VideoStreamState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState.VideoStreamState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState.VideoStreamState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.Common.Core.Reactive
  // Factory methods for creating ClientReactive with per-client initialization.
  static class ClientReactive
    // Create a ClientReactive that initializes each client's value using a factory function. The factory receives the client session ID.
    static ClientReactive<T> Create<T>(Func<int, T> factory)
  // Shorthand for ReactiveEffect<ClientScope>. Mirrors ClientReactive<T> as the per-client variant of Reactive<T>. Each connected client gets its own runner with independent cancel/queue, materialized on first dep change inside that client's scope.
  class ClientReactiveEffect : ReactiveEffect<ClientScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // A ReactiveList with a separate list for each client session.
  class ClientReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
  // A reactive variable with a separate value for each client session.
  class ClientReactive<T> : Reactive<T, ClientScope>
    ctor(T initialValue)
  interface IReactive
    long Version { get; }
    // Fires whenever this reactive's value changes (in any scope, for scoped variants). Payload-free so a single subscription can be taken across heterogeneous reactives — handlers fetch the new value via .Value when they need it. Used by ReactiveEffect and other dependency-tracked consumers.
    event Action? Changed
    // Fires with the scope-derived session id whose Signal<T> value just changed. For unscoped reactives the id is always 0; for ClientReactive<T> it is the hash of ClientScope; for UserReactive<T> the hash of UserScope; etc. Lets external subscription routing fan out to only the clients whose scope matches the changed signal.
    event Action<int>? SessionChanged
  // Factory methods for creating MountReactive with per-mount initialization.
  static class MountReactive
    // Create a MountReactive that initializes each mount's value using a factory function. The factory receives the mount id.
    static MountReactive<T> Create<T>(Func<string, T> factory)
  // A ReactiveList with a separate list for each Parallax mount in the active render iteration.
  class MountReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
  // A reactive variable with a separate value for each Parallax mount in the active render iteration.
  class MountReactive<T> : Reactive<T, MountScope>
    ctor(T initialValue)
  // Selects the backing store for a persistent reactive.
  enum PersistenceBackend
    Private
    Public
    Postgres
  // Identifies where a reactive's value is persisted in cloud storage and how it is keyed.
  enum PersistenceScope
    None
    Global
    Session
    User
  static class Reactive
    // Runs action on a background task and assigns its result to reactiveValue when it completes, so subscribers react without the caller awaiting. Exceptions go to onError when provided and are logged otherwise; cancellation leaves the reactive value unchanged.
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
    // Runs action on a background task and assigns its result to reactiveValue when it completes, passing token to the action so it can observe cancellation. Exceptions go to onError when provided and are logged otherwise; cancellation leaves the reactive value unchanged.
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
  // Convenience helpers on Reactive for the busy-flag pattern that every async handler uses. Without these, the standard shape is verbose and easy to break:
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
  // Mutation helpers for Reactive wrapping a collection. They mutate the underlying instance AND fire the change notification in one call so callers can write _items.Add(x) instead of the two-step _items.Value.Add(x); _items.NotifyUpdate();. Why these exist on a Reactive wrapping a mutable collection: the reference-equality check at the Value setter doesn't trigger when the underlying list is mutated in-place. Forgetting NotifyUpdate is the dominant "UI doesn't update after Add/Remove" bug class. These helpers make the right thing the easy thing. Every helper runs its mutation through the locked Update , so concurrent mutations from parallel handlers serialize instead of racing. Reassignment (_items.Value = [.. _items.Value, x]) still works and stays the right form when callers want immutable-style updates; these helpers are the in-place alternative for the common case. For list state, ReactiveList offers the same one-call surface with copy-on-write snapshots and a read-only Value.
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
    // Mutate the underlying value via mutator and notify.
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
  // Side-effect primitive that runs on tracked IReactive dependency changes. Mirrors the shape of Reactive / Reactive : this class is the unscoped (global) variant; ReactiveEffect binds to a single scope type; further generic variants (forthcoming) compose multiple scopes the same way Reactive<T, TScope1, TScope2> does.
  class ReactiveEffect : IDisposable
    // Create an effect with an async body. The token cancels when a dep changes mid-run; respect it for clean cancellation.
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Create an effect with a sync body.
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // Side-effect primitive bound to a single scope type. Mirrors Reactive<T, TScope>: each instance of TScope gets its own per-scope effect runner with independent cancel/queue state, materialized lazily on first dep change in that scope. Unlike the global ReactiveEffect , this variant does NOT fire eagerly at construction — there's no scope active yet. The first dep change observed inside a scope of type TScope instantiates that scope's runner and fires the body for the first time. For "fire when scope first opens regardless of deps" lifecycle hooks (e.g. preload data on client connect), use the host app's existing scope-creation events directly.
  class ReactiveEffect<TScope> : IDisposable where TScope : struct, IScopeKey
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // A reactive list that automatically triggers UI updates on every mutation.
  class ReactiveList<T> : Reactive<List<T>>, IEnumerable, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
    // The number of items. Tracked read.
    int Count { get; }
    // The item at index . The getter is a tracked read; the setter replaces the item with one change notification.
    T this[int index] { get; set; }
    // The current items without dependency tracking. See Peek .
    IReadOnlyList<T> Peek { get; }
    // The current items as a read-only snapshot. Reading tracks a dependency like Value ; assigning replaces the whole content with a copy of the given sequence (see ReplaceAll ).
    IReadOnlyList<T> Value { get; set; }
    // Append item . One notification.
    void Add(T item)
    // Append items . One notification for the whole batch.
    void AddRange(IEnumerable<T> items)
    // Remove all items. One notification.
    void Clear()
    // Whether item is present. Tracked read.
    bool Contains(T item)
    // Enumerate a snapshot of the current items. Tracked read; the snapshot is safe to iterate while other code mutates the list.
    IEnumerator<T> GetEnumerator()
    // Index of the first occurrence of item , or -1. Tracked read.
    int IndexOf(T item)
    // Insert item at index . One notification.
    void Insert(int index, T item)
    // Remove the first occurrence of item . Returns whether it was found. One notification either way.
    bool Remove(T item)
    // Remove all items matching match . Returns the removed count. One notification either way.
    int RemoveAll(Predicate<T> match)
    // Remove the item at index . One notification.
    void RemoveAt(int index)
    // Replace the whole content with a copy of items . One notification.
    void ReplaceAll(IEnumerable<T> items)
    // Sort the items using comparison . One notification.
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
  // A reactive variable that automatically triggers UI updates when its value changes.
  class Reactive<T> : IReactive
    // Creates a reactive whose initial value is default(T). Call as new Reactive<T>() — the UseDefault parameter is only an overload disambiguator and is never passed explicitly.
    ctor(UseDefault _ = default)
    ctor(T initialValue)
    // Reads the value for the currently-active scope without subscribing the current reactive computation to changes. Use inside renders for values that should not trigger re-renders.
    T Peek { get; }
    // The value for the currently-active scope. Reading inside a reactive computation (e.g. a UI render) subscribes it to changes; writing notifies subscribers when the value changed.
    T Value { get; set; }
    // Monotonic change counter for the currently-active scope's value, incremented on every write or NotifyUpdate . Lets consumers detect changes cheaply without comparing values.
    long Version { get; }
    // Opt this reactive out of hot-reload state capture. Use for runtime-only caches that hold non-serializable or cyclic object graphs and are rebuilt from their own backing store after a reload (e.g. orchestrator caches of live domain objects) — capturing them only fails noisily. Fluent: returns this so it can be chained onto a field initializer. Has no effect on long-term persistence, which only applies to non-None PersistenceScope s.
    Reactive<T> ExcludeFromHotReloadCapture()
    // Notifies subscribers that the current value changed without assigning it, for in-place mutations the setter cannot see (e.g. adding to a stored collection). Prefer Update , which mutates and notifies atomically.
    void NotifyUpdate()
    override string ToString()
    // Atomically read-modify-write the value for the currently-active scope. The transform runs under a per-scope lock, so concurrent mutations (e.g. appending to a shared list from parallel action handlers) serialize instead of racing — replacing the ad-hoc external locks that callers previously needed. Fires the change notification once. See Update .
    void Update(Func<T, T> mutator)
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
  // A reactive variable scoped to a specific scope type, providing isolated values per scope instance.
  class Reactive<T, TScope> : Reactive<T> where TScope : IScopeKey
    ctor(T initialValue)
    ctor(Func<T> initialValue)
  // Marker type for the default-value Reactive constructor. Because every constructor carries trailing caller-info parameters, a marker parameter is what keeps the value-less overload distinct from Reactive(T initialValue, ...). Never pass it explicitly — write new Reactive<T>() and the value starts at default(T).
  struct UseDefault
  // Shorthand for ReactiveEffect<UserScope>. Mirrors UserReactive<T> as the per-user variant of Reactive<T>. Each distinct user gets its own runner; the same user across multiple sessions shares one runner.
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // A ReactiveList with a separate list for each user, shared across their client sessions.
  class UserReactiveList<T> : ReactiveList<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
  // A reactive variable with a separate value for each user, shared across their client sessions.
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue)
    ctor(Func<string, T> initialValue)

namespace Ikon.Common.Core.Scope
  // Scope for backend token context, transports the backend token of the caller.
  struct BackendTokenScope : IScopeKey
    ctor(string token)
    string Id { get; }
    string Name { get; }
  // Scope for client session context, providing unique identity for each connected client.
  struct ClientScope : IScopeKey
    ctor(int sessionId)
    ctor(Context context)
    int Id { get; }
    string Name { get; }
  // Scope with a user-specified name and ID, enabling dynamic scoping without needing new struct types.
  struct CustomScope : IScopeKey
    ctor(string name, string id)
    string Id { get; }
    string Name { get; }
  interface IScopeKey
    object Id { get; }
    string Name { get; }
  // Identifies the Parallax render target ("mount") an app is currently producing UI for. An app may declare multiple mounts via Mounts ; each ( ClientScope , MountScope ) pair gets its own per-render UI tree and an independent stream on the wire. Default mount id is "ikon-ui" — the value every app emits today on its single stream.
  struct MountScope : IScopeKey
    ctor(string mountId)
    string Id { get; }
    string Name { get; }
    // The mount id every Ikon app emits today on its single Parallax stream. Apps that don't override IAppBase.Mounts render under this id.
    static string DefaultMountId
  // Scope for grouping a single logical operation (e.g., LLM generation, image generation).
  struct OperationScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  // Scope for application run context, typically set at program startup in Program.cs. Used to group all log events and operations within a single application run.
  struct RunScope : IScopeKey
    ctor()
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  // Scope for tenant/customer context, an arbitrary user-specified ID for scoping AI app logic.
  struct TenantScope : IScopeKey
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
  // Scope for end user identity context, providing unique identity for each user.
  struct UserScope : IScopeKey
    ctor(string userId)
    ctor(Context context)
    string Id { get; }
    string Name { get; }

namespace Ikon.Common.Core.Signing
  sealed class SignatureDocument : IEquatable<SignatureDocument>
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  sealed class SignatureOrderRequest : IEquatable<SignatureOrderRequest>
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
  sealed class SignatureSigner : IEquatable<SignatureSigner>
    ctor(SignaturePolicy Policy, string? Vendor = null, IReadOnlyList<string>? IdpNames = null, IReadOnlyList<string>? RequestedAttributes = null)
    IReadOnlyList<string>? IdpNames { get; init; }
    SignaturePolicy Policy { get; init; }
    IReadOnlyList<string>? RequestedAttributes { get; init; }
    string? Vendor { get; init; }
  // Represents a successfully signed document returned by the platform signing service. The platform downloads the result from the upstream signing vendor, hashes it, and hands the signed bytes plus evidence metadata to the requesting app. Apps should persist Bytes as the system of record — the platform retention is short.
  sealed class SignedDocument : IEquatable<SignedDocument>
    ctor(string OrderId, byte[] Bytes, string MimeType, DateTimeOffset SignedAt, string SignedDocumentHash, string IdentityScheme, string? SignerNameHash, string? EvidenceLevel)
    byte[] Bytes { get; init; }
    string? EvidenceLevel { get; init; }
    string IdentityScheme { get; init; }
    string MimeType { get; init; }
    string OrderId { get; init; }
    DateTimeOffset SignedAt { get; init; }
    string SignedDocumentHash { get; init; }
    string? SignerNameHash { get; init; }
