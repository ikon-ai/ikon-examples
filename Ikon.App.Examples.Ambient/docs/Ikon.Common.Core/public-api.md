# Ikon.Common.Core Public API

namespace Ikon.Common.Core
  sealed class AssertionVerifier
    ctor(string platformBaseUrl, HttpClient? httpClient = null, Func<DateTimeOffset>? clock = null)
    Task<(JsonDocument Claims, DateTimeOffset ExpiresAt)> VerifyAndExtractClaimsAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = default)
    Task<StepUpAssertion> VerifyAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = default)
  class AsyncLocalInstance<T> where T : new()
    ctor()
    static T Instance { get; }
    static void DisableAsyncLocalInstance()
    static void EnableAndInitAsyncLocalInstance()
    static void SetAsyncLocalInstance(T value)
  sealed class AsyncLocalInstanceAttribute : Attribute
    ctor()
  class BackendQuotaExceededException : UserException
    ctor(string key, int current, int limit, string friendlyMessage)
    int Current { get; }
    string Key { get; }
    int Limit { get; }
  static class ExceptionFormatter
    static string FormatException(Exception ex, bool includeFilePaths = true)
  static class ExtendedCast
    static T? Convert<T>(object? value)
    static object? Convert(object? value, Type targetType)
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
  static class HostUtils
    static IReadOnlyList<string> DeleteDirectoryBestEffort(string path)
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
    AuthResponse? LastAuthResponse { get; }
    DateTime ServerInitTime { get; set; }
    // The current connect entry point (the unsuffixed ConnectAsync is obsolete): fetches the AuthResponse — entrypoints, auth ticket, and client session — via the /connect GET, then opens the transport.
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
    static T DeepCopy<T>(T obj)
    static string Format(string json, JsonOptions? options = null)
    static T From<T>(string json, JsonOptions? options = null)
    static object? From(string json, Type type, JsonOptions? options = null)
    static object? From(string json, string typeName, JsonOptions? options = null)
    static T FromLLMResponse<T>(string text, JsonSerializerOptions? options)
    static T FromLLMResponse<T>(string text, JsonOptions? options = null)
    static Type? ResolveTypeByName(string typeName)
    static string To<T>(T obj, JsonOptions? options = null)
  sealed class JsonOptions
    ctor(bool useJson5 = false, bool indentation = true, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false, bool caseInsensitive = false)
    bool CamelCase { get; }
    bool CaseInsensitive { get; }
    bool EnumCamelCase { get; }
    bool EnumsAsNames { get; }
    bool IncludeFields { get; }
    bool IncludeNull { get; }
    bool Indentation { get; }
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
    void Critical(LogCriticalHandler handler)
    void Critical(string message)
    void Critical(string message, Exception exception)
    void Critical(Exception exception, string message)
    void Debug(LogDebugHandler handler)
    void Debug(string message)
    void DisableFileOutput()
    void EnableFileOutput(string filePath, bool append = false)
    void Error(LogErrorHandler handler)
    void Error(string message)
    void Error(string message, Exception exception)
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
    void Warning(string message, Exception exception)
    void Warning(Exception exception, string message)
    static void WriteErrorToConsole(string message)
    static void WriteToConsole(string message, ConsoleColor color)
    static void WriteWarningToConsole(string message)
    bool BlockWhenFull
    LogFilter ConsoleWriterFilter
    LogFilter FileWriterFilter
    LogFilter Filter
    string Prefix
    static bool RequireInitCall
    bool ShowAsyncFlow
    string TraceFilter
    static event Log.AsyncFlowFinishedHandler? AsyncFlowFinished
    event Log.LogEventHandler? LogEvent
  delegate Log.AsyncFlowFinishedHandler
    void AsyncFlowFinishedHandler(object sender, int asyncFlowId)
  delegate Log.LogEventHandler
    void LogEventHandler(object sender, LogEvent logEvent)
  readonly ref struct LogCriticalHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  readonly ref struct LogDebugHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  readonly ref struct LogErrorHandler
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
  readonly struct LogEvent.Parameter
    ctor(string name, object? value)
    readonly string Name
    readonly object? Value
  readonly ref struct LogExceptionHandler
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
  readonly ref struct LogGeneralHandler
    ctor(int literalLength, int formattedCount, Log log, LogType logType)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  readonly ref struct LogGeneralHandler2
    ctor(int literalLength, int formattedCount, Log log, LogType logType, string filePath, int lineNumber, string memberName)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  readonly ref struct LogInfoHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  readonly struct LogParameter<T>
    ctor(string name, T value)
    readonly string Name
    readonly T Value
  struct LogScopeEntry
    string Id { get; set; }
    string Type { get; set; }
  readonly ref struct LogTraceHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  readonly ref struct LogWarningHandler
    ctor(int literalLength, int formattedCount, Log log)
    void AppendFormatted<T>(T value, string format = "", string name = "")
    void AppendFormatted<T>(LogParameter<T> p, string format = "")
    void AppendLiteral(string s)
  static class NameConversions
    static string ToCamelCase(string input)
    static string ToDisplayName(string input)
    static string ToKebabCase(string input)
    static string ToPascalCase(string input)
    static string ToSlug(string input, int maxLength)
    static string ToSnakeCase(string input)
  sealed class PublicApiDocIgnoreAttribute : Attribute
    ctor()
  class ReactiveGlobalState
    ctor()
    Reactive<AppSourceType> AppSourceType { get; }
    Reactive<Dictionary<string, GlobalState.AudioStreamState>> AudioStreams { get; }
    Reactive<string> ChannelId { get; }
    Reactive<string> ChannelName { get; }
    Reactive<string> ChannelUrl { get; }
    // Keyed by client session id; each Context carries that client's user id, device, viewport, and locale.
    Reactive<Dictionary<int, Context>> Clients { get; }
    Reactive<bool> DebugMode { get; }
    // The current first human user; reassigned when that user leaves. Contrast PrimaryUserId, which is fixed.
    Reactive<string> FirstUserId { get; }
    Reactive<Dictionary<int, List<ActionFunctionRegister>>> Functions { get; }
    Reactive<string> OrganisationName { get; }
    // The session owner from server config, fixed for the session's lifetime; used for user-specific asset storage paths.
    Reactive<string> PrimaryUserId { get; }
    Reactive<bool> PublicAccess { get; }
    Reactive<ServerRunType> ServerRunType { get; }
    Reactive<string> ServerSessionId { get; }
    Reactive<string> SessionChannelUrl { get; }
    Reactive<string> SessionHash { get; }
    Reactive<string> SpaceId { get; }
    Reactive<string> SpaceName { get; }
    Reactive<Dictionary<string, GlobalState.TrackingStreamState>> TrackingStreams { get; }
    Reactive<Dictionary<string, GlobalState.UIStreamState>> UIStreams { get; }
    Reactive<Dictionary<string, GlobalState.VideoStreamState>> VideoStreams { get; }
    Context? GetClientContext(int clientSessionId)
    Context? GetClientContext(string userId)
    IEnumerable<Context> GetHumanClients()
    IEnumerable<Context> GetUniqueAuthClientContexts()
    IEnumerable<Context> GetUniqueHumanAuthClientContexts()
    void UpdateFrom(GlobalState newState)
  class ReactiveGlobalState.DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
    ctor()
    bool Equals(Dictionary<TKey, TValue>? x, Dictionary<TKey, TValue>? y)
    int GetHashCode(Dictionary<TKey, TValue> obj)
    static readonly ReactiveGlobalState.DictionaryComparer<TKey, TValue> Instance
  sealed class Secrets
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
  static class Throttler
    static bool TryExecute(Action action, TimeSpan? throttleInterval = null, string? extraKey = null)
  static class Toml
    static T From<T>(string toml) where T : class, new()
    static string To<T>(T obj) where T : class
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
  // Grammar: assets://[space/{spaceId}/][user/{userId}/][channel/{channelId}/]{class}/{path}[?query]. {class} is the kebab-case AssetClass (local-file, embedded-file, cloud-file, cloud-file-public, cloud-json) and selects the storage backend; {path} may include subdirectories and a file name. The optional space/user/channel segments scope the asset — omit them for a global asset. Immutable; With returns a modified copy.
  readonly struct AssetUri : IEquatable<AssetUri>
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
    static bool operator ==(AssetUri left, AssetUri right)
    static bool operator !=(AssetUri left, AssetUri right)
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
  sealed record EmailAttachment
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  sealed class EmailAttachmentDownload : IAsyncDisposable
    Stream Content { get; }
    string Filename { get; }
    string MimeType { get; }
    long Size { get; }
    ValueTask DisposeAsync()
  sealed record EmailHeader
    ctor(string Name, string Value)
    string Name { get; init; }
    string Value { get; init; }
  sealed record EmailSendRequest
    ctor(string To, string Subject, string HtmlBody, string? TextBody = null, string? ReplyTo = null, IReadOnlyList<EmailAttachment>? Attachments = null, IReadOnlyDictionary<string, string>? Metadata = null)
    IReadOnlyList<EmailAttachment>? Attachments { get; init; }
    string HtmlBody { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? ReplyTo { get; init; }
    string Subject { get; init; }
    string? TextBody { get; init; }
    string To { get; init; }
  sealed record InboundAttachmentInfo
    ctor(string Id, string Filename, string MimeType, long Size)
    string Filename { get; init; }
    string Id { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
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
  sealed record InboxPage
    ctor(IReadOnlyList<InboundEmailSummary> Items, string? NextCursor)
    IReadOnlyList<InboundEmailSummary> Items { get; init; }
    string? NextCursor { get; init; }
  sealed record InboxQuery
    ctor()
    string? Cursor { get; init; }
    string? From { get; init; }
    int Limit { get; init; }
    string? Recipient { get; init; }
    DateTimeOffset? Since { get; init; }
    DateTimeOffset? Until { get; init; }

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
    string ApprovalTokenHash { get; }
    object?[] Args { get; }
    string ArgsHash { get; }
    PolicyCallContext CallContext { get; }
    int CallerSessionId { get; }
    DateTimeOffset ExpiresAt { get; }
    string FunctionName { get; }
    string Reason { get; }
    int TimeoutSeconds { get; }
    static (ApprovalContext Context, Guid RawToken) Create(string functionName, string reason, object?[] args, PolicyCallContext callContext, int timeoutSeconds = 300)
    bool IsExpired()
    bool ValidateToken(Guid providedToken)
    bool ValidateToken(string providedToken)
  delegate ApprovalHandlerDelegate
    Task<ApprovalResult> ApprovalHandlerDelegate(ApprovalContext context)
  readonly struct ApprovalResult
    bool IsApproved { get; }
    string? RejectionReason { get; }
    static ApprovalResult Approved()
    static ApprovalResult Rejected(string? reason = null)
    override string ToString()
  enum ApproverType
    Caller
    SpecificClient
    SpecificUser
  interface IFunctionPolicy
    string Name { get; }
    virtual int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
  interface IUsageLimitChecker
    ValueTask<UsageLimitCheckResult> CheckAsync(PolicyCallContext context, object?[] args)
  // Checks PolicyCallContext.AuthSessionId — guests (unauthenticated callers) have an empty auth session even though they have a valid UserId (device-scoped). Returns PolicyDecision.Denied with error code "login_required", which the Ikon client runtime catches to drive the deferred-login flow.
  sealed class LoggedInPolicy : IFunctionPolicy
    ctor()
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    const string LoginRequiredCode
  sealed class PerSessionRateLimitPolicy : IFunctionPolicy
    ctor(int limit, int windowSeconds, string? name = null, int priority = 50)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    PolicyDelegate ToDelegate()
  static class PolicyArgs
    static bool HasAll(object?[] args, params int[] requiredIndices)
    static T? Optional<T>(object?[] args, int index, T? defaultValue = default)
    static T Required<T>(object?[] args, int index)
    static bool TryGet<T>(object?[] args, int index, out T? value)
  abstract class PolicyAttribute : Attribute
    int Priority { get; set; }
    abstract IFunctionPolicy CreatePolicy()
  sealed class PolicyAttribute<TPolicy> : PolicyAttribute where TPolicy : IFunctionPolicy, new()
    ctor()
    override IFunctionPolicy CreatePolicy()
  sealed class PolicyCallContext
    ctor(Guid callId, string functionName, int callerSessionId, string? userId, string? tenantId, Guid? instanceId, bool isInternal, CancellationToken cancellationToken, string? authSessionId = null, DateTime? callTimestamp = null, IReadOnlyDictionary<string, object?>? additionalContext = null)
    IReadOnlyDictionary<string, object?>? AdditionalContext { get; }
    string? AuthSessionId { get; }
    Guid CallId { get; }
    DateTime CallTimestamp { get; }
    int CallerSessionId { get; }
    CancellationToken CancellationToken { get; }
    string FunctionName { get; }
    Guid? InstanceId { get; }
    bool IsInternal { get; }
    string? TenantId { get; }
    string? UserId { get; }
  static class PolicyChain
    static IFunctionPolicy All(params IFunctionPolicy[] policies)
    static PolicyDelegate AllAsDelegate(params IFunctionPolicy[] policies)
  abstract class PolicyDecision
    static PolicyDecision Allowed()
    static PolicyDecision Denied(string reason, string? code = null)
    static PolicyDecision RequireApproval(string message)
    static PolicyDecision RequireApproval(string message, int expirySeconds)
    static PolicyDecision RequireApproval(string message, ApprovalHandlerDelegate handler)
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
    ctor(string? reason)
    ctor(string? reason, string? code)
    ctor(string? reason, string? code, string? policyName, string? functionName)
    ctor(string? reason, Exception innerException, string? policyName = null, string? functionName = null)
    ctor(string? reason, string? code, Exception innerException, string? policyName = null, string? functionName = null)
    string? Code { get; }
    string? FunctionName { get; }
    string? PolicyName { get; }
  sealed class PolicyEvaluationResult
    ctor(PolicyDecision decision, string functionName, Guid callId, string? decidingPolicyName, TimeSpan evaluationDuration)
    Guid CallId { get; }
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
    ctor(Type policyType)
    Type PolicyType { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class RateLimitAttribute : PolicyAttribute
    ctor(int limit, int windowSeconds)
    int Limit { get; }
    bool PerSession { get; set; }
    int WindowSeconds { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class RateLimitPolicy : IFunctionPolicy
    ctor(int limit, int windowSeconds, string? name = null, int priority = 50)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    PolicyDelegate ToDelegate()
  sealed class RequireApprovalAttribute : PolicyAttribute
    ctor()
    ApproverType ApproverType { get; set; }
    int ClientSessionId { get; set; }
    string Reason { get; set; }
    string? UserId { get; set; }
    override IFunctionPolicy CreatePolicy()
  sealed class RequireApprovalPolicy : IFunctionPolicy
    ctor(string reason, string? name = null, int priority = 100)
    ctor(string reason, ApprovalHandlerDelegate handler, string? name = null, int priority = 100)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    static RequireApprovalPolicy ForClient(string reason, int clientSessionId, string? name = null, int priority = 100)
    static RequireApprovalPolicy ForUser(string reason, string userId, string? name = null, int priority = 100)
    PolicyDelegate ToDelegate()
  // Guest callers (no auth session) are denied with the "login_required" error code. The Ikon client runtime intercepts this and triggers the deferred-login flow.
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
    ctor(Type checkerType)
    Type CheckerType { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class UsageLimitCheckResult
    bool Allowed { get; }
    string? DenyCode { get; }
    string? DenyReason { get; }
    static UsageLimitCheckResult Allow()
    static UsageLimitCheckResult Deny(string reason, string? code = "usage_limit_exceeded")
  sealed class UsageLimitPolicy : IFunctionPolicy
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
  enum AppSourceType
    Bundle
    GitSource
  enum AudioCodec
    Unknown
    Opus
    Mp3
    RawPcm16
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
  sealed class Context : IProtocolMessagePayload
    ctor()
    ctor(ContextType contextType, UserType userType, PayloadType payloadType, string description, string userId, string deviceId, string productId, string versionId, string installId, string locale, int sessionId, bool isInternal, bool isSnapshot, bool isReady, bool hasInput, string channelLocale, string authSessionId, bool receiveAllMessages, ulong preciseJoinedAt, string userAgent, ClientType clientType, string uniqueSessionId, Dictionary<string, string> parameters, SdkType sdkType, int sdkCapability, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, StyleFormat styleFormat, bool supportsCompression, bool isSoftDisconnected, ulong softDisconnectAt)
    string AuthSessionId { get; set; }
    string ChannelLocale { get; set; }
    int ClientSessionId { get; }
    ClientType ClientType { get; set; }
    ContextType ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    string InstallId { get; set; }
    bool IsInternal { get; set; }
    bool IsReady { get; set; }
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
  sealed class GlobalState : ILogInfo, IProtocolMessagePayload
    ctor()
    ctor(Dictionary<int, Context> clients, Dictionary<int, List<ActionFunctionRegister>> functions, Dictionary<string, GlobalState.UIStreamState> uiStreams, Dictionary<string, GlobalState.AudioStreamState> audioStreams, Dictionary<string, GlobalState.VideoStreamState> videoStreams, Dictionary<string, GlobalState.TrackingStreamState> trackingStreams, string spaceId, string channelId, string serverSessionId, string sessionHash, string channelUrl, string sessionChannelUrl, string firstUserId, string primaryUserId, string organisationName, string spaceName, string channelName, ServerRunType serverRunType, AppSourceType appSourceType, bool publicAccess, bool debugMode)
    AppSourceType AppSourceType { get; set; }
    Dictionary<string, GlobalState.AudioStreamState> AudioStreams { get; set; }
    string ChannelId { get; set; }
    string ChannelName { get; set; }
    string ChannelUrl { get; set; }
    Dictionary<int, Context> Clients { get; set; }
    bool DebugMode { get; set; }
    string FirstUserId { get; set; }
    Dictionary<int, List<ActionFunctionRegister>> Functions { get; set; }
    object LogInfo { get; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string OrganisationName { get; set; }
    string PrimaryUserId { get; set; }
    bool PublicAccess { get; set; }
    ServerRunType ServerRunType { get; set; }
    string ServerSessionId { get; set; }
    string SessionChannelUrl { get; set; }
    string SessionHash { get; set; }
    string SpaceId { get; set; }
    string SpaceName { get; set; }
    Dictionary<string, GlobalState.TrackingStreamState> TrackingStreams { get; set; }
    Dictionary<string, GlobalState.UIStreamState> UIStreams { get; set; }
    Dictionary<string, GlobalState.VideoStreamState> VideoStreams { get; set; }
    void AddAudioStream(GlobalState.AudioStreamState audioStreamState)
    void AddClient(Context clientContext)
    void AddFunction(int clientSessionId, ActionFunctionRegister function)
    void AddTrackingStream(GlobalState.TrackingStreamState trackingStreamState)
    void AddUIStream(GlobalState.UIStreamState uiStreamState)
    void AddVideoStream(GlobalState.VideoStreamState videoStreamState)
    Context? GetClientContext(int clientSessionId)
    Context? GetClientContext(string userId)
    int GetClientSessionId(string userId)
    int[] GetClientSessionIds()
    int[] GetClientSessionIdsByProductId(string productId)
    int[] GetClientSessionIdsExcept(int[] clientSessionIds)
    int[] GetHumanClientSessionIds()
    int[] GetMachineClientSessionIds()
    List<string>? GetUserIds(IEnumerable<int> targetIds)
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
  sealed class GlobalState.AudioStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, AudioStreamBegin info)
    int ClientSessionId { get; set; }
    AudioStreamBegin Info { get; set; }
    string StreamId { get; set; }
    int TrackId { get; set; }
  sealed class GlobalState.TrackingStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, TrackingStreamBegin info)
    int ClientSessionId { get; set; }
    TrackingStreamBegin Info { get; set; }
    string StreamId { get; set; }
    int TrackId { get; set; }
  sealed class GlobalState.UIStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, UIStreamBegin info)
    int ClientSessionId { get; set; }
    UIStreamBegin Info { get; set; }
    string StreamId { get; set; }
    int TrackId { get; set; }
  sealed class GlobalState.VideoStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, VideoStreamBegin info)
    int ClientSessionId { get; set; }
    VideoStreamBegin Info { get; set; }
    string StreamId { get; set; }
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
    UI_STYLES
    UI_UPDATE
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
  static class SdkCapabilities
    const int ClientInitializationMessage = 4
    const int ClientLifecycleBatching = 3
    const int Current = 4
    const int FunctionRegistryOutsideGlobalState = 1
    const int KeepaliveTimeoutNegotiation = 2
  enum SdkType
    Unknown
    DotNet
    TypeScript
    Cpp
    Dart
    Rust
  static class ServerCapabilities
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
  sealed class TrackingStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string category, TrackingType type, List<string> faceBlendshapes)
    string Category { get; set; }
    List<string> FaceBlendshapes { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    TrackingType Type { get; set; }
  enum TrackingType
    Face
    Hands
    Pose
    All
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
  class ClientReactiveEffect : ReactiveEffect<ClientScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
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
    event Action? Changed
    event Action<int>? SessionChanged
  static class MountReactive
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
  class MountReactiveEffect : ReactiveEffect<MountScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
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
    Private
    Public
    Postgres
  enum PersistenceScope
    None
    Global
    Session
    User
  static class Reactive
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = default)
  // Reading Value during a UI render registers a dependency; writing a changed value re-renders only the parts that read it. An unscoped Reactive<T> holds one value shared across all clients and is accessible anywhere. For per-client state use ClientReactive<T>; for per-user state (shared across a user's sessions) use UserReactive<T>. Those scoped variants resolve .Value against the active scope, so it must be read inside one — UI.Root(), an action callback, or a ReactiveScope.Use() block — and throw otherwise; background work (a Task.Run loop, a timer, an endpoint handler) has no scope and names its target instead via SetFor(id, value) / ValueFor(id).
  class Reactive<T> : IReactive
    ctor(UseDefault _ = default)
    ctor(T initialValue)
    // Unlike Value, does not register a dependency, so reading it inside a render never causes a re-render when the value later changes.
    T Peek { get; }
    // For the scoped variants (ClientReactive<T>, UserReactive<T>, MountReactive<T>) the scope must be active — inside UI.Root(), an action callback, or a ReactiveScope.Use block. From background work that carries no scope, name the target instead: SetFor(id, value) / ValueFor(id).
    T Value { get; set; }
    long Version { get; }
    // Fluent (returns this). Use only for runtime-only caches rebuilt from their own backing store after a reload — capturing non-serializable or cyclic graphs otherwise fails noisily. Does not affect long-term persistence, which applies only to non-None PersistenceScopes.
    Reactive<T> ExcludeFromHotReloadCapture()
    // The escape hatch for in-place mutation of a stored value (e.g. a mutable object the reactive holds), which the setter never observes. Prefer Update, which mutates and notifies under the scope's lock in one step.
    void NotifyUpdate()
    override string ToString()
    // Runs mutator under the scope's lock so concurrent read-modify-writes serialize instead of racing, and fires the change notification exactly once.
    void Update(Func<T, T> mutator)
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
    static IDisposable AsToken(this Reactive<bool> reactive)
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
    bool Remove(TKey key)
    void ReplaceAll(IEnumerable<KeyValuePair<TKey, TValue>> entries)
    bool TryAdd(TKey key, TValue value)
    bool TryGetValue(TKey key, out TValue value)
    void Update(Action<Dictionary<TKey, TValue>> transform)
  // Lifecycle (global): • Constructor runs the body once immediately (initial fire). • Each tracked dep's IReactive.Changed event triggers a re-run. • If a dep changes while a previous run is still in flight, the previous run's CancellationToken is cancelled and one follow-up run is queued. Rapid-fire changes coalesce. • IDisposable.Dispose cancels any in-flight run and detaches all dep subscriptions. • Exceptions in the body (other than OperationCanceledException) are logged and do not disable the effect.
  class ReactiveEffect : IDisposable
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  class ReactiveEffect<TScope> : IDisposable where TScope : struct, IScopeKey
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // Reads track a dependency exactly like Reactive<T> (reading Value, Count, Contains, or enumerating during render). Every mutation method fires exactly one notification on its own — _ids.Add(x) is the whole call. Reactive<T>.NotifyUpdate is the escape hatch for the one case the mutators cannot see: mutating a stored member in place. Copy-on-write: every mutation runs under the lock and replaces the backing set with a fresh copy, so concurrent mutations serialize and any set handed out earlier is a stable snapshot. Each mutation copies the whole set, so for batches prefer the single-notify bulk ops (UnionWith, ExceptWith, ReplaceAll, Update) over per-item calls in a loop.
  class ReactiveHashSet<T> : Reactive<HashSet<T>>, IReadOnlyCollection<T>
    ctor()
    ctor(IEnumerable<T> initialItems)
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
    bool Remove(T item)
    int RemoveAll(Predicate<T> match)
    void RemoveAt(int index)
    void ReplaceAll(IEnumerable<T> items)
    void Sort(Comparison<T> comparison)
    void Update(Func<IReadOnlyList<T>, IEnumerable<T>> transform)
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
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
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
  sealed record SignedDocument
    ctor(string OrderId, byte[] Bytes, string MimeType, DateTimeOffset SignedAt, string SignedDocumentHash, string IdentityScheme, string? SignerNameHash, string? EvidenceLevel)
    byte[] Bytes { get; init; }
    string? EvidenceLevel { get; init; }
    string IdentityScheme { get; init; }
    string MimeType { get; init; }
    string OrderId { get; init; }
    DateTimeOffset SignedAt { get; init; }
    string SignedDocumentHash { get; init; }
    string? SignerNameHash { get; init; }
