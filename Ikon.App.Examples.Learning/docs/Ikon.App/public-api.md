# Ikon.App Public API

namespace Ikon.App
  sealed class AppAttribute : Attribute
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL, Opcode sendOpcodeGroups = GROUP_ALL, string[]? dependencies = null)
    int AppVersion { get; }
    string[] Dependencies { get; }
    string Description { get; }
    string Guid { get; }
    string Name { get; }
    string ProductId { get; }
    Opcode ReceiveOpcodeGroups { get; }
    Opcode SendOpcodeGroups { get; }
    UserType UserType { get; }
    int Version { get; }
    PluginAttribute ToPluginAttribute(Type owner)
  static class AppDatabaseConnection
    static DbConnection Create(IAppBase app, string databaseName)
    static DbConnection Create(DatabaseConnectionInfo dbInfo)
  sealed class AppEndpointHost : IAsyncDisposable
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    int LocalPort { get; }
    string PublicUrl { get; }
    ValueTask DisposeAsync()
    void MapGet(string pattern, Func<HttpContext, Task> handler)
    void MapPost(string pattern, Func<HttpContext, Task> handler)
    void MapWebSocket(string pattern, Func<HttpContext, WebSocket, Task> handler)
    Task StartAsync(CancellationToken cancellationToken = null)
    Task StopAsync(CancellationToken cancellationToken = null)
  class App<TConfig> : BasePlugin<App<TConfig>, WrapperConfig<TConfig>>, IAppBase, IApp<TConfig>
    ctor(Type appInstanceType, WrapperConfig<TConfig> userConfig, PluginAttribute pluginAttribute, string argsJson)
    BackgroundWork BackgroundWork { get; }
    TConfig Config { get; }
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    EmailService Email { get; }
    int MaxMemoryLimitMb { get; }
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    ReactiveGlobalState ReactiveGlobalState { get; }
    ReactiveRoot ReactiveRoot { get; }
    Secrets Secrets { get; }
    IReadOnlyList<WebhookInfo> Webhooks { get; }
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = null)
    Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", CancellationToken ct = null)
    Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = null)
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
  class App<TSessionIdentity, TClientParameters> : BasePlugin<App<TSessionIdentity, TClientParameters>, BasePluginConfig>, IAppBase, IApp<TSessionIdentity, TClientParameters>
    ctor(Type appInstanceType, PluginAttribute pluginAttribute, string argsJson)
    BackgroundWork BackgroundWork { get; }
    IClientCollection<TClientParameters> Clients { get; }
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    EmailService Email { get; }
    int MaxMemoryLimitMb { get; }
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    ReactiveGlobalState ReactiveGlobalState { get; }
    ReactiveRoot ReactiveRoot { get; }
    Secrets Secrets { get; }
    TSessionIdentity SessionIdentity { get; }
    IReadOnlyList<WebhookInfo> Webhooks { get; }
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = null)
    Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", CancellationToken ct = null)
    Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = null)
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
  delegate AsyncEventHandler<TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler`1<TEventArgs>(TEventArgs e)
  class Audio
    ctor(IAppBase app)
    AudioEncoderOptions DefaultEncoderOptions { get; set; }
    AudioMetrics Metrics { get; }
    SpeechMixer SpeechMixer { get; }
    ValueTask CloseAllAsync()
    ValueTask CloseAsync(string? streamId = null)
    AudioOutputStreamInfo GetOutputStreamInfo(string? streamId = null)
    ValueTask SendAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    void SendSpeech(AudioContainer audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    void SendSpeech(string speechEventId, ReadOnlySpan<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01, bool requireCorrelatedStream = true, string language = "", TimeSpan? timeout = null)
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
  class AudioInputFrameEventArgs : EventArgs, ICaptureCorrelationArgs
    ctor(string streamId, Context clientContext, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    float[] Samples { get; }
    string StreamId { get; }
    TimeSpan TotalDuration { get; set; }
    string UserId { get; }
  class AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, int sampleRate, int channelCount, Context clientContext, int trackId, string? correlationId)
    int ChannelCount { get; }
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    string Description { get; }
    int SampleRate { get; }
    string SourceType { get; }
    string StreamId { get; }
    AudioInputStreamingMode StreamingMode { get; set; }
    int TrackId { get; }
    string UserId { get; }
  class AudioInputStreamEndEventArgs : EventArgs
    ctor(string streamId, Context clientContext, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    string StreamId { get; }
    string UserId { get; }
  enum AudioInputStreamingMode
    Streaming
    DelayUntilTotalDurationKnown
    DelayUntilIsLast
  class AudioOutputStreamInfo : IEquatable<AudioOutputStreamInfo>
    ctor(string StreamId, int TrackId, AudioCodec Codec, int SampleRate, int ChannelCount)
    int ChannelCount { get; init; }
    AudioCodec Codec { get; init; }
    int SampleRate { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
  sealed class AuthOutcome : IEquatable<AuthOutcome>
    ctor(HttpResult? Reject, IReadOnlyDictionary<string, string>? Claims = null)
    IReadOnlyDictionary<string, string> Claims { get; init; }
    HttpResult Reject { get; init; }
    static AuthOutcome Pass(IReadOnlyDictionary<string, string>? claims = null)
    static AuthOutcome RejectWith(HttpResult result)
  class BackgroundWork
    ValueTask<IAsyncDisposable> StartAsync()
    ValueTask StopAsync()
  static class CaptureCorrelationBridge
    static void RegisterStart(string correlationId, Func<ICaptureCorrelationArgs, Task> handler)
    static void RegisterStop(string correlationId, Func<ICaptureCorrelationArgs, Task> handler)
    static void Unregister(string correlationId)
  sealed class ClientAudioCaptureOptions : IEquatable<ClientAudioCaptureOptions>
    ctor()
    bool? AutoGainControl { get; init; }
    int? Bitrate { get; init; }
    static ClientAudioCaptureOptions Default { get; }
    string DeviceId { get; init; }
    bool? EchoCancellation { get; init; }
    bool? NoiseSuppression { get; init; }
    IReadOnlyList<int> TargetIds { get; init; }
  class ClientCollection<TClientParameters> : IClientCollection<TClientParameters>, IEnumerable, IEnumerable<IClient<TClientParameters>>
    ctor()
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters> Item { get; }
    IEnumerator<IClient<TClientParameters>> GetEnumerator()
  sealed class ClientContact : IEquatable<ClientContact>
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    IReadOnlyList<string> Emails { get; init; }
    IReadOnlyList<string> Names { get; init; }
    IReadOnlyList<string> Phones { get; init; }
  static class ClientFunctions
    static Task<ClientImageCapture> CaptureImageAsync(int targetId, ClientImageCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<bool> ExitFullscreenAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> ExitFullscreenAsync(CancellationToken cancellationToken = null)
    static Task<int?> GetBatteryLevelAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<int?> GetBatteryLevelAsync(CancellationToken cancellationToken = null)
    static Task<string> GetLanguageAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetLanguageAsync(CancellationToken cancellationToken = null)
    static Task<ClientLocation> GetLocationAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<ClientLocation> GetLocationAsync(CancellationToken cancellationToken = null)
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(CancellationToken cancellationToken = null)
    static Task<string> GetNetworkTypeAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetNetworkTypeAsync(CancellationToken cancellationToken = null)
    static Task<string> GetThemeAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetThemeAsync(CancellationToken cancellationToken = null)
    static Task<string> GetTimezoneAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetTimezoneAsync(CancellationToken cancellationToken = null)
    static Task<string> GetUrlAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetUrlAsync(CancellationToken cancellationToken = null)
    static Task<string> GetVisibilityAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string> GetVisibilityAsync(CancellationToken cancellationToken = null)
    static Task<bool> KeepScreenAwakeAsync(int targetId, bool enabled, CancellationToken cancellationToken = null)
    static Task<bool> KeepScreenAwakeAsync(bool enabled, CancellationToken cancellationToken = null)
    static Task<bool> LoginShowAsync(int targetId, string? reason = null, CancellationToken cancellationToken = null)
    static Task<bool> LogoutAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> LogoutAsync(CancellationToken cancellationToken = null)
    static Task<bool> OpenExternalUrlAsync(int targetId, string url, CancellationToken cancellationToken = null)
    static Task<bool> OpenExternalUrlAsync(string url, CancellationToken cancellationToken = null)
    static Task<string> PlaySoundAsync(int targetId, string url, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string> PlaySoundAsync(int targetId, byte[] data, string mimeType, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string> PlaySoundAsync(string url, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string> PlaySoundAsync(byte[] data, string mimeType, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<bool> RequestFullscreenAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> RequestFullscreenAsync(CancellationToken cancellationToken = null)
    static Task<bool> ScrollToAsync(int targetId, double x, double y, bool smooth = false, CancellationToken cancellationToken = null)
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, CancellationToken cancellationToken = null)
    static Task<bool> SetThemeAsync(int targetId, string theme, bool persist = true, CancellationToken cancellationToken = null)
    static Task<bool> SetThemeAsync(string theme, bool persist = true, CancellationToken cancellationToken = null)
    static Task<bool> SetUrlAsync(int targetId, string url, bool replace = false, bool preserveQueryParams = false, CancellationToken cancellationToken = null)
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, CancellationToken cancellationToken = null)
    static Task<string> StartAudioCaptureAsync(int targetId, ClientAudioCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartVideoCaptureAsync(int targetId, ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<bool> StopCaptureAsync(int targetId, string streamId, CancellationToken cancellationToken = null)
    static Task<bool> StopCaptureAsync(string streamId, CancellationToken cancellationToken = null)
    static Task<bool> StopSoundAsync(int targetId, string playbackId, CancellationToken cancellationToken = null)
    static Task<bool> StopSoundAsync(string playbackId, CancellationToken cancellationToken = null)
    static Task<bool> VibrateAsync(int targetId, string pattern, CancellationToken cancellationToken = null)
    static Task<bool> VibrateAsync(string pattern, CancellationToken cancellationToken = null)
  enum ClientHardwareAcceleration
    PreferHardware
    PreferSoftware
  sealed class ClientImageCapture : IEquatable<ClientImageCapture>
    ctor(string Mime, int Width, int Height, byte[] Data)
    byte[] Data { get; init; }
    int Height { get; init; }
    string Mime { get; init; }
    int Width { get; init; }
  enum ClientImageCaptureFormat
    Jpeg
    Png
  sealed class ClientImageCaptureOptions : IEquatable<ClientImageCaptureOptions>
    ctor()
    ClientImageCaptureFormat? Format { get; init; }
    int? Height { get; init; }
    double? Quality { get; init; }
    int? Width { get; init; }
  class ClientJoinedEventArgs : EventArgs
    ctor(Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string UserId { get; }
  class ClientLeftEventArgs : EventArgs
    ctor(Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string UserId { get; }
  sealed class ClientLocation : IEquatable<ClientLocation>
    ctor(double Latitude, double Longitude, double Accuracy)
    double Accuracy { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
  static class ClientMediaCaptureSerializer
    static string SerializeAudioOptions(ClientAudioCaptureOptions? options)
    static string SerializeImageOptions(ClientImageCaptureOptions? options)
    static string SerializeVideoOptions(ClientVideoCaptureOptions? options)
  sealed class ClientMediaDevice : IEquatable<ClientMediaDevice>
    ctor(string DeviceId, string Kind, string Label, string GroupId)
    string DeviceId { get; init; }
    string GroupId { get; init; }
    string Kind { get; init; }
    string Label { get; init; }
  sealed class ClientProfile
    ProfileAddress Address { get; }
    string BirthDate { get; }
    string Email { get; }
    string FirstName { get; }
    string Gender { get; }
    string Id { get; }
    bool IsAdmin { get; }
    bool IsGuest { get; }
    bool IsModerator { get; }
    string Language { get; }
    string LastName { get; }
    string Name { get; }
    string PhoneNumber { get; }
    string PreferredName { get; }
    IReadOnlyList<string> Roles { get; }
    string UserId { get; }
    string VisibleName { get; }
    object GetAttribute(string key)
    TAttributes GetAttributes<TAttributes>()
    bool HasRole(UserRole role)
    bool HasRole(string role)
    bool HasRole<TRole>(TRole role)
  class ClientProfiles
    ctor(IAppBase app)
    Task AddRoleAsync(Context clientContext, UserRole role)
    Task AddRoleAsync(Context clientContext, string role)
    void ClearCache()
    Task<IReadOnlyList<ClientProfile>> FindProfilesAsync(Dictionary<string, string> filters, int maxResults = 1000)
    Task<IReadOnlyList<ClientProfile>> GetAllProfilesAsync(int maxResults = 1000)
    TAttributes GetAttributes<TAttributes>(Context clientContext)
    ClientProfile GetProfile(Context clientContext)
    bool HasRole(Context clientContext, UserRole role)
    bool HasRole(Context clientContext, string role)
    bool HasRole<TRole>(Context clientContext, TRole role)
    bool IsAdmin(Context clientContext)
    bool IsGuest(Context clientContext)
    bool IsModerator(Context clientContext)
    Task RefreshProfileAsync(Context clientContext)
    Task RefreshProfileAsync(string userId)
    Task RemoveRoleAsync(Context clientContext, UserRole role)
    Task RemoveRoleAsync(Context clientContext, string role)
    void RequireAdmin(Context clientContext)
    void RequireModerator(Context clientContext)
    void RequireRole(Context clientContext, UserRole role)
    void RequireRole(Context clientContext, string role)
    Task SetAttributesAsync<TAttributes>(Context clientContext, TAttributes attrs)
    Task SetRolesAsync(Context clientContext, IEnumerable<UserRole> roles)
    Task SetRolesAsync(Context clientContext, IEnumerable<string> roles)
    ClientProfile TryGetProfile(Context clientContext)
    ClientProfile TryGetProfile(string userId)
    Task UpdateAsync(Context clientContext, Action<ProfileData> update)
  enum ClientVideoCaptureCodec
    H264
    Vp8
    Vp9
    Av1
  sealed class ClientVideoCaptureOptions : IEquatable<ClientVideoCaptureOptions>
    ctor()
    int? Bitrate { get; init; }
    static ClientVideoCaptureOptions DefaultCamera { get; }
    static ClientVideoCaptureOptions DefaultScreen { get; }
    string DeviceId { get; init; }
    int? Framerate { get; init; }
    ClientHardwareAcceleration? HardwareAcceleration { get; init; }
    int? Height { get; init; }
    int? KeyFrameIntervalFrames { get; init; }
    IReadOnlyList<ClientVideoCaptureCodec> PreferredCodecs { get; init; }
    IReadOnlyList<int> TargetIds { get; init; }
    int? Width { get; init; }
  enum ClientVideoCaptureSource
    Camera
    Screen
  class Client<TClientParameters> : IClient<TClientParameters>
    ctor(TClientParameters parameters)
    TClientParameters Parameters { get; }
  static class Constants
    static string DarkTheme
    static string LightTheme
  sealed class EmailService
    Task DeleteAsync(string id, CancellationToken ct = null)
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = null)
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = null)
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = null)
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = null)
    Task SendAsync(EmailSendRequest request, CancellationToken ct = null)
  sealed class FileUploadCallbackSet
    ctor()
    Func<FileUploadChunkArgs, Task> OnChunkReceived
    Func<FileUploadCompleteArgs, Task> OnUploadComplete
    Func<FileUploadErrorArgs, Task> OnUploadError
    Func<FileUploadPreStartArgs, Task<FileUploadPreStartResult>> OnUploadPreStart
    Func<FileUploadProgressArgs, Task> OnUploadProgress
    Func<FileUploadStartArgs, Task<FileUploadStartResult>> OnUploadStart
  sealed class FileUploadChunkArgs : IEquatable<FileUploadChunkArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, byte[] Data, long BytesWritten)
    long BytesWritten { get; init; }
    byte[] Data { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadCompleteArgs : IEquatable<FileUploadCompleteArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string? LocalTempFilePath, string? AssetUri)
    string AssetUri { get; init; }
    string FileName { get; init; }
    string LocalTempFilePath { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadErrorArgs : IEquatable<FileUploadErrorArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string ErrorMessage)
    string ErrorMessage { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadHandler : IDisposable
    ctor(IAppBase app)
    void Dispose()
    void RegisterCallbacks(string uploadActionId, FileUploadCallbackSet callbackSet)
  sealed class FileUploadPreStartArgs : IEquatable<FileUploadPreStartArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, Func<string?, Task> Cancel)
    Func<string, Task> Cancel { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadPreStartResult : IEquatable<FileUploadPreStartResult>
    ctor()
    bool Accepted { get; set; }
    string AssetUri { get; set; }
  sealed class FileUploadProgressArgs : IEquatable<FileUploadProgressArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, double ProgressPercentage, long BytesUploaded)
    long BytesUploaded { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    double ProgressPercentage { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadStartArgs : IEquatable<FileUploadStartArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string Hash)
    string FileName { get; init; }
    string Hash { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadStartResult : IEquatable<FileUploadStartResult>
    ctor()
    bool Accepted { get; set; }
    string AssetUri { get; set; }
  static class HttpDispatchGovernance
    static Task<object> InvokeAsync(MethodInfo handler, Type ownerType, IReadOnlyDictionary<string, object?> args, Func<Task<object?>> invoke, CancellationToken ct = null)
  sealed class HttpEndpointAttribute : Attribute
    ctor(string method, string path)
    bool Absolute { get; init; }
    Type Auth { get; init; }
    string Method { get; }
    string Path { get; }
  static class HttpEndpointDiscovery
    static IReadOnlyList<HttpEndpointInfo> ForType(Type ownerType)
    static IReadOnlyList<HttpEndpointInfo> ForTypes(IEnumerable<Type> types)
  sealed class HttpEndpointEnvelope : IEquatable<HttpEndpointEnvelope>
    ctor(int StatusCode, string? Body, string ContentType)
    string Body { get; init; }
    string ContentType { get; init; }
    int StatusCode { get; init; }
  sealed class HttpEndpointInfo : IEquatable<HttpEndpointInfo>
    ctor(string Method, string Path, Type? Auth, bool Absolute, MethodInfo Handler, Type OwnerType)
    bool Absolute { get; init; }
    Type Auth { get; init; }
    MethodInfo Handler { get; init; }
    string Method { get; init; }
    Type OwnerType { get; init; }
    string Path { get; init; }
  sealed class HttpRequest : IEquatable<HttpRequest>
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  sealed class HttpResult : IEquatable<HttpResult>
    ctor(int StatusCode, object? Body = null, string ContentType = "application/json")
    object Body { get; init; }
    string ContentType { get; init; }
    int StatusCode { get; init; }
    static HttpResult Accepted(object? body = null)
    static HttpResult BadRequest(string? reason = null)
    static HttpResult Conflict(string? reason = null)
    static HttpResult Created(object? body = null)
    static HttpResult Forbidden(string? reason = null)
    static HttpResult Json(object body, int statusCode = 200)
    static HttpResult NoContent()
    static HttpResult NotFound(string? reason = null)
    static HttpResult Ok(object? body = null)
    static HttpResult Text(string body, int statusCode = 200)
    static HttpResult Unauthorized(string? reason = null)
  interface IAppBase : IProtocolMessageChannel
    BackgroundWork BackgroundWork { get; }
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    EmailService Email { get; }
    GlobalState GlobalState { get; }
    int MaxMemoryLimitMb { get; }
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    ReactiveGlobalState ReactiveGlobalState { get; }
    ReactiveRoot ReactiveRoot { get; }
    Secrets Secrets { get; }
    IReadOnlyList<WebhookInfo> Webhooks { get; }
    abstract Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = null)
    abstract Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", CancellationToken ct = null)
    abstract Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = null)
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
  static class IAppEventExtensions
    static void OnClientJoined(IAppBase app, Func<Context, Task> handler)
    static void OnClientJoined<TSessionIdentity, TClientParameters>(IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnClientLeft(IAppBase app, Func<Context, Task> handler)
    static void OnClientLeft<TSessionIdentity, TClientParameters>(IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnMessageReceived(IAppBase app, Func<ProtocolMessage, Task> handler)
    static void OnStarting(IAppBase app, Func<Task> handler)
    static void OnStopping(IAppBase app, Func<Task> handler)
  interface IApp<TConfig> : IAppBase, IProtocolMessageChannel
    TConfig Config { get; }
  interface IApp<TSessionIdentity, TClientParameters> : IAppBase, IProtocolMessageChannel
    TClientParameters ClientParameters { get; }
    IClientCollection<TClientParameters> Clients { get; }
    TSessionIdentity SessionIdentity { get; }
  interface ICaptureCorrelationArgs
    Context ClientContext { get; }
    string CorrelationId { get; }
    string StreamId { get; }
  interface IClientCollection<TClientParameters> : IEnumerable, IEnumerable<IClient<TClientParameters>>
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters> Item { get; }
    IEnumerable<int> Keys { get; }
  interface IClient<TClientParameters>
    TClientParameters Parameters { get; }
  interface IProfileAttributes
  static class JsonSchemaBuilder
    static JsonElement BuildObjectSchema(IReadOnlyList<ParameterInfo> parameters)
    static JsonElement BuildObjectSchema(IReadOnlyList<ParameterInfo> parameters, IReadOnlyList<ValueTuple<string, Type, string?>> extraRequired)
  static class LoginPrompt
    static Task ShowAsync(int targetClientSessionId, string? reason = null)
    static Task ShowAsync(string? reason = null)
    static string HandoffParameterKey
  static class McpHttpTransport
    static Task HandlePostAsync(HttpContext context, McpHost mcp, IReadOnlyDictionary<string, string>? sessionIdentityFields = null)
    static Task HandleProtectedResourceDiscoveryAsync(HttpContext context)
  static class McpResourceBridge
    static McpResourceHandler BuildHandler(CellHost cellHost, McpResourceInfo info)
  static class McpResourceDiscovery
    static IReadOnlyList<McpResourceInfo> ForType(Type ownerType)
    static IReadOnlyList<McpResourceInfo> ForTypes(IEnumerable<Type> types)
  sealed class McpResourceInfo : IEquatable<McpResourceInfo>
    ctor(string DisplayName, string Description, string MimeType, UriTemplate UriTemplate, MethodInfo Handler, Type OwnerCellType)
    string Description { get; init; }
    string DisplayName { get; init; }
    MethodInfo Handler { get; init; }
    bool IsStatic { get; }
    string MimeType { get; init; }
    Type OwnerCellType { get; init; }
    string SubjectId { get; }
    UriTemplate UriTemplate { get; init; }
  static class McpToolBridge
    static McpToolHandler BuildHandler(CellHost cellHost, McpToolInfo info)
  static class McpToolDiscovery
    static IReadOnlyList<McpToolInfo> ForType(Type ownerType)
    static IReadOnlyList<McpToolInfo> ForTypes(IEnumerable<Type> types)
  sealed class McpToolInfo : IEquatable<McpToolInfo>
    ctor(string Name, string Description, MethodInfo Handler, Type OwnerCellType)
    string Description { get; init; }
    MethodInfo Handler { get; init; }
    string Name { get; init; }
    Type OwnerCellType { get; init; }
    string SubjectId { get; }
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
  static class ClientFunctions.Names
    static string CaptureImage
    static string ExitFullscreen
    static string GetBatteryLevel
    static string GetLanguage
    static string GetLocation
    static string GetMediaDevices
    static string GetNetworkType
    static string GetTheme
    static string GetTimezone
    static string GetUrl
    static string GetVisibility
    static string KeepScreenAwake
    static string LoginShow
    static string Logout
    static string OpenExternalUrl
    static string PlaySound
    static string RequestFullscreen
    static string ScrollTo
    static string SetTheme
    static string SetUrl
    static string StartAudioCapture
    static string StartVideoCapture
    static string StopCapture
    static string StopSound
    static string Vibrate
  class Navigation : IReactiveWithState
    Task<string> GetPathAsync(int targetId)
    Task<string> GetPathAsync()
    Task<bool> SetPathAsync(int targetId, string path, bool replace = false)
    Task<bool> SetPathAsync(string path, bool replace = false)
    event AsyncEventHandler<NavigationPathChangedEventArgs> PathChangedAsync
  class NavigationPathChangedEventArgs : EventArgs
    ctor(string url, Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string Path { get; }
    string Url { get; }
    string UserId { get; }
  class PersistentReactive<T> : Reactive<T>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string PostgresDatabase { get; }
    string PublicUrl { get; }
  class PersistentSessionReactive<T> : Reactive<T>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string PostgresDatabase { get; }
    string PublicUrl { get; }
  class PersistentUserReactive<T> : Reactive<T, UserScope>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string PostgresDatabase { get; }
    string PublicUrl { get; }
  sealed class ProfileAddress
    string City { get; }
    string Country { get; }
    string Municipality { get; }
    string State { get; }
    string Street { get; }
    string Zip { get; }
  sealed class ProfileData
    ctor()
    string AddressCity { get; set; }
    string AddressCountry { get; set; }
    string AddressState { get; set; }
    string AddressStreet { get; set; }
    string AddressZip { get; set; }
    string BirthDate { get; set; }
    string Email { get; set; }
    string FirstName { get; set; }
    string Gender { get; set; }
    string Language { get; set; }
    string LastName { get; set; }
    string Name { get; set; }
    string PhoneNumber { get; set; }
    string PreferredName { get; set; }
  class ReactiveRoot
    ctor(IAppBase app, int updateIntervalMs = 1000)
    ReactiveManager ReactiveManager { get; }
    Task RunAsync(Func<Task> render, Func<Context, bool>? filter = null)
  sealed class RouteTemplate
    IReadOnlyList<string> CaptureNames { get; }
    string Pattern { get; }
    static RouteTemplate Parse(string template)
    bool TryMatch(string path, out IReadOnlyDictionary<string, string> captures)
  sealed class SpeechRecognizedEventArgs : EventArgs
    ctor(string text, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    TimeSpan Duration { get; }
    int SampleCount { get; }
    string StreamId { get; }
    string Text { get; }
    string UserId { get; }
  class StartingEventArgs : EventArgs
    ctor()
  class StoppingEventArgs : EventArgs
    ctor()
  sealed class UriTemplate
    bool IsStatic { get; }
    IReadOnlyList<string> PlaceholderNames { get; }
    string Template { get; }
    IReadOnlyDictionary<string, string> Match(string uri)
    static UriTemplate Parse(string template)
  enum UserRole
    Guest
    User
    Moderator
    Admin
  class Video
    ctor(IAppBase app)
    ValueTask CloseAllAsync()
    ValueTask CloseAsync(string? streamKey = null)
    VideoOutputStreamInfo GetOutputStreamInfo(string? streamId = null)
    ValueTask SendAsync(byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, VideoCodec codec, int width, int height, double framerate, string? streamId = null, IReadOnlyList<int>? targetIds = null, int? trackId = null)
    event AsyncEventHandler<VideoInputFrameEventArgs> VideoInputFrameAsync
    event AsyncEventHandler<VideoInputStreamBeginEventArgs> VideoInputStreamBeginAsync
    event AsyncEventHandler<VideoInputStreamEndEventArgs> VideoInputStreamEndAsync
  class VideoInputFrameEventArgs : EventArgs, ICaptureCorrelationArgs
    ctor(string streamId, Context clientContext, int trackId, byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    byte[] Data { get; }
    uint DurationInUs { get; }
    int FrameNumber { get; }
    bool IsKey { get; }
    string StreamId { get; }
    ulong TimestampInUs { get; }
    int TrackId { get; }
    string UserId { get; }
  class VideoInputStreamBeginEventArgs : EventArgs, ICaptureCorrelationArgs
    ctor(string streamId, string description, string sourceType, VideoCodec codec, string codecDetails, int width, int height, double framerate, Context clientContext, int trackId, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    VideoCodec Codec { get; }
    string CodecDetails { get; }
    string CorrelationId { get; }
    string Description { get; }
    double Framerate { get; }
    int Height { get; }
    string SourceType { get; }
    string StreamId { get; }
    int TrackId { get; }
    string UserId { get; }
    int Width { get; }
  class VideoInputStreamEndEventArgs : EventArgs, ICaptureCorrelationArgs
    ctor(string streamId, Context clientContext, int trackId, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string CorrelationId { get; }
    string StreamId { get; }
    int TrackId { get; }
    string UserId { get; }
  class VideoOutputStreamInfo : IEquatable<VideoOutputStreamInfo>
    ctor(string StreamId, int TrackId, VideoCodec Codec, int Width, int Height, double Framerate)
    VideoCodec Codec { get; init; }
    double Framerate { get; init; }
    int Height { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
    int Width { get; init; }
  sealed class WebhookInfo
    ctor()
    string CellType { get; set; }
    string FunctionName { get; set; }
    string PublicUrl { get; set; }
  class WrapperConfig<TConfig> : BasePluginConfig
    ctor()
    ctor(TConfig userConfig)
    TConfig AppConfig { get; set; }

namespace Ikon.App.Auth
  sealed class AnonymousAuth
    ctor(ICell<AnonymousAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  sealed class ApiKeyAuth
    ctor(ICell<ApiKeyAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  sealed class AuthTicketAuth
    ctor(ICell<AuthTicketAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  sealed class EdgeTrustedHeaderAuth
    ctor(ICell<EdgeTrustedHeaderAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  sealed class OAuthAuth
    ctor(ICell<OAuthAuth.SessionIdentity> ctx)
    static string ConfiguredIssuer { get; }
    Task<AuthOutcome> Authenticate(HttpRequest request)
  class AnonymousAuth.SessionIdentity : IEquatable<AnonymousAuth.SessionIdentity>
    ctor()
  class ApiKeyAuth.SessionIdentity : IEquatable<ApiKeyAuth.SessionIdentity>
    ctor()
  class AuthTicketAuth.SessionIdentity : IEquatable<AuthTicketAuth.SessionIdentity>
    ctor()
  class EdgeTrustedHeaderAuth.SessionIdentity : IEquatable<EdgeTrustedHeaderAuth.SessionIdentity>
    ctor()
  class OAuthAuth.SessionIdentity : IEquatable<OAuthAuth.SessionIdentity>
    ctor()
  class SessionTokenAuth.SessionIdentity : IEquatable<SessionTokenAuth.SessionIdentity>
    ctor()
  sealed class SessionTokenAuth
    ctor(ICell<SessionTokenAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)

namespace Ikon.App.Billing
  sealed class AssetBillingConnectAccountStore : IBillingConnectAccountStore
    ctor(string assetPath = "billing/connect-account-id.json")
    Task ClearAsync(CancellationToken cancellationToken = null)
    Task<string> GetAsync(CancellationToken cancellationToken = null)
    Task SetAsync(string connectedAccountId, CancellationToken cancellationToken = null)
  sealed class BillingAccountSession : IEquatable<BillingAccountSession>
    ctor(string ClientSecret, DateTimeOffset ExpiresAt)
    string ClientSecret { get; init; }
    DateTimeOffset ExpiresAt { get; init; }
  sealed class BillingAccountSessionRequest : IEquatable<BillingAccountSessionRequest>
    bool AccountManagement { get; init; }
    bool AccountOnboarding { get; init; }
    bool Balances { get; init; }
    string ConnectedAccountId { get; init; }
    bool DisableStripeUserAuth { get; init; }
    bool Documents { get; init; }
    bool ExternalAccountCollection { get; init; }
    bool NotificationBanner { get; init; }
    bool Payments { get; init; }
    bool PaymentsCapturePayments { get; init; }
    bool PaymentsDisputeManagement { get; init; }
    bool PaymentsRefundManagement { get; init; }
    bool Payouts { get; init; }
    bool PayoutsEditPayoutSchedule { get; init; }
    bool PayoutsStandardPayouts { get; init; }
  static class BillingAppHelpers
    static BillingOptions AutoDetectFromApp(IAppBase app, string defaultAppId = "app")
    static string GetSecretOrEnv(IAppBase app, string key)
  sealed class BillingCatalogSync
    ctor(BillingService billing)
    Task<BillingPlanCatalogMap> SyncAsync(IReadOnlyList<BillingPlanSpec> plans, CancellationToken cancellationToken = null)
    Task<BillingPlanCatalogMap> SyncFromCatalogClassAsync(Type catalogClass, CancellationToken cancellationToken = null)
  sealed class BillingCharge : IEquatable<BillingCharge>
    ctor(string Id, string? PaymentIntentId, string? CustomerId, long AmountMinor, long AmountRefundedMinor, string Currency, string Status, bool Paid, bool Refunded, DateTimeOffset Created, string? Description, string? ReceiptUrl)
    long AmountMinor { get; init; }
    long AmountRefundedMinor { get; init; }
    DateTimeOffset Created { get; init; }
    string Currency { get; init; }
    string CustomerId { get; init; }
    string Description { get; init; }
    string Id { get; init; }
    bool Paid { get; init; }
    string PaymentIntentId { get; init; }
    string ReceiptUrl { get; init; }
    bool Refunded { get; init; }
    string Status { get; init; }
  sealed class BillingChargeCreditsAttribute : PolicyAttribute
    ctor(string sku, int credits = 1)
    int Credits { get; }
    string Sku { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class BillingCheckoutOffer : IEquatable<BillingCheckoutOffer>
    ctor(bool AlreadyEntitled, string? SessionId, string? Url)
    bool AlreadyEntitled { get; init; }
    string SessionId { get; init; }
    string Url { get; init; }
  sealed class BillingCheckoutResult : IEquatable<BillingCheckoutResult>
    ctor(string SessionId, string Url)
    string SessionId { get; init; }
    string Url { get; init; }
  sealed class BillingConnectAccount : IEquatable<BillingConnectAccount>
    ctor(string Id, bool DetailsSubmitted, bool ChargesEnabled, bool PayoutsEnabled, IReadOnlyList<string> RequirementsCurrentlyDue, IReadOnlyList<string> RequirementsEventuallyDue, string? RequirementsDisabledReason, string? Country = null)
    bool ChargesEnabled { get; init; }
    string Country { get; init; }
    bool DetailsSubmitted { get; init; }
    string Id { get; init; }
    bool PayoutsEnabled { get; init; }
    IReadOnlyList<string> RequirementsCurrentlyDue { get; init; }
    string RequirementsDisabledReason { get; init; }
    IReadOnlyList<string> RequirementsEventuallyDue { get; init; }
  sealed class BillingConnectFunctionHost
    ctor(BillingConnectService connect, Func<string?> connectedAccountIdGetter, Func<BillingConnectAccount, Task>? onStatusRefresh = null)
    Task<string> FetchConnectManagementSecretAsync()
    Task<string> FetchConnectOnboardingSecretAsync()
    Task OnConnectOnboardingExitAsync()
  sealed class BillingConnectService
    ctor(BillingOptions options)
    static BillingConnectService Current { get; }
    Task<BillingAccountSession> CreateAccountSessionAsync(BillingAccountSessionRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingWebhookEndpoint> CreateConnectWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateExpressAccountAsync(string email, string country, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, IEnumerable<string>? requestedCapabilities = null, CancellationToken cancellationToken = null)
    Task<string> CreateLoginLinkAsync(string connectedAccountId, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateOnboardingLinkAsync(string connectedAccountId, string refreshUrl, string returnUrl, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingConnectAccount> RetrieveAccountAsync(string connectedAccountId, CancellationToken cancellationToken = null)
    Task<string> TransferAsync(string connectedAccountId, long amountMinor, string currency, string idempotencyKey, CancellationToken cancellationToken = null)
  enum BillingCouponDuration
    Once
    Forever
    Repeating
  sealed class BillingCouponInfo : IEquatable<BillingCouponInfo>
    ctor()
    long? AmountOffMinor { get; init; }
    string Currency { get; init; }
    BillingCouponDuration Duration { get; init; }
    int? DurationInMonths { get; init; }
    string Id { get; init; }
    int? MaxRedemptions { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Name { get; init; }
    decimal? PercentOff { get; init; }
    DateTimeOffset? RedeemBy { get; init; }
  sealed class BillingCreditNote : IEquatable<BillingCreditNote>
    ctor(string Id, string Number, string Status, long AmountMinor, string? PdfUrl)
    long AmountMinor { get; init; }
    string Id { get; init; }
    string Number { get; init; }
    string PdfUrl { get; init; }
    string Status { get; init; }
  sealed class BillingCreditNoteInfo : IEquatable<BillingCreditNoteInfo>
    long? AmountMinor { get; init; }
    long? CreditAmountMinor { get; init; }
    string InvoiceId { get; init; }
    string Memo { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Reason { get; init; }
    long? RefundAmountMinor { get; init; }
  sealed class BillingCustomerInfo : IEquatable<BillingCustomerInfo>
    ctor()
    string AddressCity { get; init; }
    string AddressCountry { get; init; }
    string AddressLine1 { get; init; }
    string AddressLine2 { get; init; }
    string AddressPostalCode { get; init; }
    string AddressState { get; init; }
    string Description { get; init; }
    string Email { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Name { get; init; }
    string Phone { get; init; }
    string PreferredLocales { get; init; }
    BillingTaxExempt? TaxExempt { get; init; }
  sealed class BillingDestination : IEquatable<BillingDestination>
    ctor(string ConnectedAccountId, long? ApplicationFeeAmountMinor = null, decimal? ApplicationFeePercent = null)
    long? ApplicationFeeAmountMinor { get; init; }
    decimal? ApplicationFeePercent { get; init; }
    string ConnectedAccountId { get; init; }
  sealed class BillingEmbeddedCheckout : IEquatable<BillingEmbeddedCheckout>
    ctor(string SessionId, string ClientSecret)
    string ClientSecret { get; init; }
    string SessionId { get; init; }
  sealed class BillingEntitlement : IEquatable<BillingEntitlement>
    ctor(string PlanId, bool SubscriptionActive, DateTimeOffset? SubscriptionEndsAt, bool CancelAtPeriodEnd, string? SubscriptionStatus, bool UnlockGranted, DateTimeOffset? UnlockGrantedAt, int CreditsRemaining, DateTimeOffset? LastPurchaseAt)
    bool CancelAtPeriodEnd { get; init; }
    int CreditsRemaining { get; init; }
    DateTimeOffset? LastPurchaseAt { get; init; }
    string PlanId { get; init; }
    bool SubscriptionActive { get; init; }
    DateTimeOffset? SubscriptionEndsAt { get; init; }
    string SubscriptionStatus { get; init; }
    bool UnlockGranted { get; init; }
    DateTimeOffset? UnlockGrantedAt { get; init; }
  sealed class BillingEvent : IEquatable<BillingEvent>
    ctor(string EventId, BillingEventType Type, string? CustomerId, string? SubscriptionId, string? ClientReferenceId, string? PlanId, string? Status, DateTimeOffset? CurrentPeriodStart, DateTimeOffset? CurrentPeriodEnd, long? AmountPaid, string? Currency, JsonElement RawPayload)
    long? AmountPaid { get; init; }
    string ClientReferenceId { get; init; }
    string Currency { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodStart { get; init; }
    string CustomerId { get; init; }
    string EventId { get; init; }
    string PlanId { get; init; }
    JsonElement RawPayload { get; init; }
    string Status { get; init; }
    string SubscriptionId { get; init; }
    BillingEventType Type { get; init; }
  enum BillingEventType
    Unknown
    CheckoutCompleted
    InvoicePaid
    InvoicePaymentFailed
    InvoiceFinalized
    PaymentActionRequired
    SubscriptionUpdated
    SubscriptionDeleted
    ChargeRefunded
    ChargeDisputed
    ChargeDisputeClosed
    SetupIntentSucceeded
    PaymentMethodAttached
    CreditNoteCreated
    CreditNoteVoided
    SubscriptionTrialWillEnd
  sealed class BillingInvoice : IEquatable<BillingInvoice>
    ctor(string Id, string? HostedInvoiceUrl, string? InvoicePdfUrl, string Status)
    string HostedInvoiceUrl { get; init; }
    string Id { get; init; }
    string InvoicePdfUrl { get; init; }
    string Status { get; init; }
  sealed class BillingInvoiceSummary : IEquatable<BillingInvoiceSummary>
    ctor(string Id, string? CustomerId, string? SubscriptionId, long AmountDueMinor, long AmountPaidMinor, string Currency, string Status, DateTimeOffset Created, DateTimeOffset? DueDate, string? HostedInvoiceUrl, string? InvoicePdfUrl)
    long AmountDueMinor { get; init; }
    long AmountPaidMinor { get; init; }
    DateTimeOffset Created { get; init; }
    string Currency { get; init; }
    string CustomerId { get; init; }
    DateTimeOffset? DueDate { get; init; }
    string HostedInvoiceUrl { get; init; }
    string Id { get; init; }
    string InvoicePdfUrl { get; init; }
    string Status { get; init; }
    string SubscriptionId { get; init; }
  sealed class BillingLineItem : IEquatable<BillingLineItem>
    ctor()
    long? AdHocAmountMinor { get; init; }
    string AdHocCurrency { get; init; }
    string AdHocProductName { get; init; }
    bool AdHocRecurring { get; init; }
    string AdHocRecurringInterval { get; init; }
    string PriceId { get; init; }
    long Quantity { get; init; }
    static BillingLineItem Dynamic(long amountMinor, string currency, string productName, long quantity = 1)
    static BillingLineItem ForPrice(string priceId, long quantity = 1)
  enum BillingMode
    Subscription
    OneTime
  sealed class BillingOptions : IEquatable<BillingOptions>
    ctor()
    string ApiKey { get; init; }
    string ApiVersion { get; init; }
    bool AutomaticTax { get; init; }
    bool CollectTaxId { get; init; }
    string ConnectedAccountId { get; init; }
    string DefaultCancelUrl { get; init; }
    IReadOnlyDictionary<string, string> DefaultMetadata { get; init; }
    string DefaultPortalReturnUrl { get; init; }
    string DefaultSuccessUrl { get; init; }
    string IkonAppId { get; init; }
    string IkonAppToken { get; init; }
    string IkonBackendUrl { get; init; }
    string IkonWebhookSecret { get; init; }
    int MaxRetryAttempts { get; init; }
    long? PlatformApplicationFeeAmountMinor { get; init; }
    decimal? PlatformApplicationFeePercent { get; init; }
    BillingProvider Provider { get; init; }
    TimeSpan? RequestTimeout { get; init; }
    TimeSpan RetryBaseDelay { get; init; }
    string WebhookSecret { get; init; }
  sealed class BillingPage<T> : IEquatable<BillingPage<T>>
    ctor(IReadOnlyList<T> Items, bool HasMore, string? LastId)
    bool HasMore { get; init; }
    IReadOnlyList<T> Items { get; init; }
    string LastId { get; init; }
  sealed class BillingPaymentIntent : IEquatable<BillingPaymentIntent>
    ctor(string Id, string ClientSecret, string Status)
    string ClientSecret { get; init; }
    string Id { get; init; }
    string Status { get; init; }
  sealed class BillingPaymentLink : IEquatable<BillingPaymentLink>
    ctor(string Id, string Url)
    string Id { get; init; }
    string Url { get; init; }
  sealed class BillingPaymentMethod : IEquatable<BillingPaymentMethod>
    ctor(string Id, string Type, string? CardBrand, string? CardLast4, int? CardExpMonth, int? CardExpYear)
    string CardBrand { get; init; }
    int? CardExpMonth { get; init; }
    int? CardExpYear { get; init; }
    string CardLast4 { get; init; }
    string Id { get; init; }
    string Type { get; init; }
  sealed class BillingPlanCatalogMap
    IEnumerable<string> AppPlanIds { get; }
    int Count { get; }
    bool Contains(string appPlanId)
    string GetPriceId(string appPlanId)
    IReadOnlyDictionary<string, string> ToDictionary()
    bool TryGetPriceId(string appPlanId, out string priceId)
  sealed class BillingPlanDescriptor : IEquatable<BillingPlanDescriptor>
    ctor(string PlanId, string StripePriceId, BillingMode Mode, string? MeteredPriceId = null, long Quantity = 1, IReadOnlyDictionary<string, string>? Metadata = null, int? TrialPeriodDays = null, bool AllowPromotionCodes = false)
    bool AllowPromotionCodes { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string MeteredPriceId { get; init; }
    BillingMode Mode { get; init; }
    string PlanId { get; init; }
    long Quantity { get; init; }
    string StripePriceId { get; init; }
    int? TrialPeriodDays { get; init; }
    static BillingPlanDescriptor Credits(string planId, string stripePriceId, int creditsGranted, IReadOnlyDictionary<string, string>? metadata = null)
    static BillingPlanDescriptor Subscription(string planId, string stripePriceId, int trialPeriodDays = 0, bool allowPromotionCodes = false, long quantity = 1, string? meteredPriceId = null, IReadOnlyDictionary<string, string>? metadata = null)
    static BillingPlanDescriptor Unlock(string planId, string stripePriceId, long quantity = 1, IReadOnlyDictionary<string, string>? metadata = null)
  sealed class BillingPlanSpec : IEquatable<BillingPlanSpec>
    ctor(string AppPlanId, string ProductName, long UnitAmountMinor, string Currency, string? Interval, int? IntervalCount = null, string? Description = null, string? Nickname = null, IReadOnlyDictionary<string, string>? Metadata = null, string? LookupKeyOverride = null)
    string AppPlanId { get; init; }
    string Currency { get; init; }
    string Description { get; init; }
    string Interval { get; init; }
    int? IntervalCount { get; init; }
    string LookupKeyOverride { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Nickname { get; init; }
    string ProductName { get; init; }
    long UnitAmountMinor { get; init; }
    static BillingPlanSpec Credits(string appPlanId, string productName, long unitAmountMinor, string currency, int creditsGranted, string? description = null)
    static BillingPlanSpec Subscription(string appPlanId, string productName, long unitAmountMinor, string currency, string interval, int? intervalCount = null, string? description = null)
    static BillingPlanSpec Unlock(string appPlanId, string productName, long unitAmountMinor, string currency, string? description = null)
  sealed class BillingPortalConfigurationInfo : IEquatable<BillingPortalConfigurationInfo>
    ctor()
    bool AllowCustomerUpdate { get; init; }
    bool AllowInvoiceHistory { get; init; }
    bool AllowPaymentMethodUpdate { get; init; }
    bool AllowSubscriptionCancel { get; init; }
    bool AllowSubscriptionPause { get; init; }
    string BusinessProfileHeadline { get; init; }
    string PrivacyPolicyUrl { get; init; }
    string SubscriptionCancelMode { get; init; }
    string TermsOfServiceUrl { get; init; }
  sealed class BillingPortalResult : IEquatable<BillingPortalResult>
    ctor(string SessionId, string Url)
    string SessionId { get; init; }
    string Url { get; init; }
  sealed class BillingPrice : IEquatable<BillingPrice>
    ctor(string Id, string ProductId, long UnitAmountMinor, string Currency, string? RecurringInterval, bool Active, string? LookupKey = null)
    bool Active { get; init; }
    string Currency { get; init; }
    string Id { get; init; }
    string LookupKey { get; init; }
    string ProductId { get; init; }
    string RecurringInterval { get; init; }
    long UnitAmountMinor { get; init; }
  sealed class BillingPriceInfo : IEquatable<BillingPriceInfo>
    bool Active { get; init; }
    string Currency { get; init; }
    string LookupKey { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Nickname { get; init; }
    string ProductId { get; init; }
    string RecurringInterval { get; init; }
    int? RecurringIntervalCount { get; init; }
    bool TransferLookupKey { get; init; }
    long UnitAmountMinor { get; init; }
  sealed class BillingProduct : IEquatable<BillingProduct>
    ctor(string Id, string Name, bool Active, string? Description)
    bool Active { get; init; }
    string Description { get; init; }
    string Id { get; init; }
    string Name { get; init; }
  sealed class BillingProductInfo : IEquatable<BillingProductInfo>
    bool Active { get; init; }
    string Description { get; init; }
    string Id { get; init; }
    IReadOnlyList<string> Images { get; init; }
    IReadOnlyDictionary<string, string> Metadata { get; init; }
    string Name { get; init; }
    string StatementDescriptor { get; init; }
  enum BillingProvider
    Disabled
    Byok
    IkonConnect
  sealed class BillingRequireSubscriptionAttribute : PolicyAttribute
    ctor(string planId)
    string PlanId { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class BillingRequireUnlockAttribute : PolicyAttribute
    ctor(string planId)
    string PlanId { get; }
    override IFunctionPolicy CreatePolicy()
  sealed class BillingService
    ctor(BillingOptions options, IBillingAppAdapter adapter)
    IBillingCreditStore CreditStore { get; set; }
    static BillingService Current { get; }
    Task AddInvoiceItemAsync(string stripeCustomerId, long amountMinor, string currency, string description, string? subscriptionId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task AdjustCustomerBalanceAsync(string stripeCustomerId, long amountMinorDelta, string currency, string description, string idempotencyKey, CancellationToken cancellationToken = null)
    Task<BillingPaymentIntent> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = null)
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, CancellationToken cancellationToken = null)
    Task CancelSubscriptionScheduleAsync(string scheduleId, CancellationToken cancellationToken = null)
    Task<BillingPaymentIntent> CapturePaymentIntentAsync(string paymentIntentId, long? amountToCaptureMinor = null, CancellationToken cancellationToken = null)
    Task<BillingCheckoutResult> CreateCartCheckoutAsync(IEnumerable<BillingLineItem> lines, BillingMode mode, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingCheckoutResult> CreateCheckoutAsync(string planId, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateCouponAsync(BillingCouponInfo coupon, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingCreditNote> CreateCreditNoteAsync(BillingCreditNoteInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateCustomerAsync(BillingCustomerInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingTaxId> CreateCustomerTaxIdAsync(string stripeCustomerId, string type, string value, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingEmbeddedCheckout> CreateEmbeddedCheckoutAsync(string planId, string? appCustomerKey, string? email, string returnUrl, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingInvoice> CreateHostedInvoiceAsync(string stripeCustomerId, IEnumerable<BillingLineItem> lines, int daysUntilDue, bool autoSend = true, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingPaymentIntent> CreatePaymentIntentAsync(long amountMinor, string currency, string? stripeCustomerId = null, string captureMethod = "automatic", string? paymentMethodId = null, bool confirm = false, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingPaymentLink> CreatePaymentLinkAsync(IEnumerable<BillingLineItem> lines, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingPortalResult> CreatePortalAsync(string stripeCustomerId, string? returnUrl = null, CancellationToken cancellationToken = null)
    Task<string> CreatePortalConfigurationAsync(BillingPortalConfigurationInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreatePriceAsync(BillingPriceInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateProductAsync(BillingProductInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreatePromotionCodeAsync(string couponId, string code, DateTimeOffset? expiresAt = null, long? maxRedemptions = null, string? restrictedToCustomerId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingSetupIntent> CreateSetupIntentAsync(string stripeCustomerId, string usage = "off_session", string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<string> CreateSubscriptionScheduleAsync(string stripeCustomerId, IEnumerable<BillingSubscriptionPhase> phases, DateTimeOffset? startDate = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingCheckoutResult> CreateTipCheckoutAsync(long amountMinor, string currency, string? title = null, string? message = null, string? appCustomerKey = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task<BillingWebhookEndpoint> CreateWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task DeleteCustomerTaxIdAsync(string stripeCustomerId, string taxIdId, CancellationToken cancellationToken = null)
    Task DeleteWebhookEndpointAsync(string webhookEndpointId, CancellationToken cancellationToken = null)
    Task DetachPaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = null)
    Task<BillingEntitlement> GetEntitlementAsync(string planId, string appCustomerKey, IBillingCreditStore? creditStore = null, CancellationToken cancellationToken = null)
    Task<BillingWebhookResult> HandleIkonWebhookAsync(string? signatureHeader, string body, TimeSpan? tolerance = null, CancellationToken cancellationToken = null)
    Task<BillingWebhookResult> HandleWebhookAsync(string? signatureHeader, string body, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListApplePayDomainsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingCharge>> ListChargesAsync(string? stripeCustomerId = null, int limit = 100, DateTimeOffset? createdAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCouponsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCreditNotesAsync(string? invoiceId = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCustomerTaxIdsAsync(string stripeCustomerId, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListEventIdsAsync(string? type = null, DateTimeOffset? createdAfter = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingInvoiceSummary>> ListInvoicesAsync(string? stripeCustomerId = null, string? subscriptionId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPaymentLinksAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingPaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card", int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingPrice>> ListPricesAsync(string? productId = null, bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    Task<BillingPage<BillingPrice>> ListPricesPageAsync(string? productId = null, bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingProduct>> ListProductsAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    Task<BillingPage<BillingProduct>> ListProductsPageAsync(bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPromotionCodesAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<BillingSubscription>> ListSubscriptionsAsync(string? stripeCustomerId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListWebhookEndpointsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<BillingCheckoutOffer> OfferCheckoutAsync(string planId, string appCustomerKey, string? email = null, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task PauseSubscriptionAsync(string subscriptionId, string behavior = "void", CancellationToken cancellationToken = null)
    Task<BillingUpcomingInvoice> PreviewUpcomingInvoiceAsync(string stripeCustomerId, string? subscriptionId = null, string? newPriceId = null, long? newQuantity = null, string? couponId = null, CancellationToken cancellationToken = null)
    Task RefundAsync(string paymentIntentId, long? amountMinor, string? reason, string idempotencyKey, CancellationToken cancellationToken = null)
    Task<string> RegisterApplePayDomainAsync(string domainName, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    Task ReportUsageAsync(string meterEventName, string stripeCustomerId, long value, string idempotencyKey, DateTimeOffset? timestamp = null, CancellationToken cancellationToken = null)
    Task ResumeCanceledSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    Task ResumeSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    Task<BillingEvent> RetrieveEventAsync(string eventId, CancellationToken cancellationToken = null)
    Task<BillingPrice> RetrievePriceByLookupKeyAsync(string lookupKey, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersByAppKeyAsync(string appCustomerKey, string metadataKey = "app_customer_key", int limit = 1, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersDetailedAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    Task UpdateCustomerAsync(string stripeCustomerId, BillingCustomerInfo info, CancellationToken cancellationToken = null)
    Task UpdatePriceAsync(string priceId, bool? active = null, string? nickname = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    Task UpdateProductAsync(string productId, bool? active = null, string? name = null, string? description = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    Task UpdateSubscriptionItemQuantityAsync(string subscriptionItemId, long quantity, bool prorate = true, CancellationToken cancellationToken = null)
    Task UpdateSubscriptionPriceAsync(string subscriptionItemId, string newPriceId, bool prorate = true, CancellationToken cancellationToken = null)
    Task UpdateSubscriptionScheduleAsync(string scheduleId, IEnumerable<BillingSubscriptionPhase> phases, CancellationToken cancellationToken = null)
    Task VoidCreditNoteAsync(string creditNoteId, CancellationToken cancellationToken = null)
  sealed class BillingSetupIntent : IEquatable<BillingSetupIntent>
    ctor(string Id, string ClientSecret)
    string ClientSecret { get; init; }
    string Id { get; init; }
  sealed class BillingSubscription : IEquatable<BillingSubscription>
    ctor(string Id, string CustomerId, string Status, DateTimeOffset? CurrentPeriodStart, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd, string? DefaultPaymentMethodId, string? LatestInvoiceId, IReadOnlyList<string> ItemIds)
    bool CancelAtPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodStart { get; init; }
    string CustomerId { get; init; }
    string DefaultPaymentMethodId { get; init; }
    string Id { get; init; }
    IReadOnlyList<string> ItemIds { get; init; }
    string LatestInvoiceId { get; init; }
    string Status { get; init; }
  sealed class BillingSubscriptionPhase : IEquatable<BillingSubscriptionPhase>
    ctor(string StripePriceId, long Quantity = 1, int? Iterations = null)
    int? Iterations { get; init; }
    long Quantity { get; init; }
    string StripePriceId { get; init; }
  enum BillingTaxExempt
    None
    Exempt
    Reverse
  sealed class BillingTaxId : IEquatable<BillingTaxId>
    ctor(string Id, string Type, string Value, string? Country)
    string Country { get; init; }
    string Id { get; init; }
    string Type { get; init; }
    string Value { get; init; }
  sealed class BillingUpcomingInvoice : IEquatable<BillingUpcomingInvoice>
    ctor(long AmountDueMinor, long AmountPaidMinor, long SubtotalMinor, long TotalMinor, long? TotalDiscountAmountMinor, long? TaxMinor, string Currency, DateTimeOffset? PeriodStart, DateTimeOffset? PeriodEnd, DateTimeOffset? NextPaymentAttempt, IReadOnlyList<BillingUpcomingInvoiceLine> Lines)
    long AmountDueMinor { get; init; }
    long AmountPaidMinor { get; init; }
    string Currency { get; init; }
    IReadOnlyList<BillingUpcomingInvoiceLine> Lines { get; init; }
    DateTimeOffset? NextPaymentAttempt { get; init; }
    DateTimeOffset? PeriodEnd { get; init; }
    DateTimeOffset? PeriodStart { get; init; }
    long SubtotalMinor { get; init; }
    long? TaxMinor { get; init; }
    long? TotalDiscountAmountMinor { get; init; }
    long TotalMinor { get; init; }
  sealed class BillingUpcomingInvoiceLine : IEquatable<BillingUpcomingInvoiceLine>
    ctor(string? PriceId, string Description, long AmountMinor, string Currency, long Quantity, bool Proration)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    string Description { get; init; }
    string PriceId { get; init; }
    bool Proration { get; init; }
    long Quantity { get; init; }
  sealed class BillingWebhookEndpoint : IEquatable<BillingWebhookEndpoint>
    ctor(string Id, string Url, string? Secret, string Status)
    string Id { get; init; }
    string Secret { get; init; }
    string Status { get; init; }
    string Url { get; init; }
  sealed class BillingWebhookFunctionHost
    ctor(BillingService billing)
    Task<string> StripeWebhook(Dictionary<string, string> queryParams, Dictionary<string, string> headers, string body)
  sealed class BillingWebhookResult : IEquatable<BillingWebhookResult>
    ctor(bool Verified, string? Reason, BillingEvent? Event, string? AdapterError = null)
    string AdapterError { get; init; }
    BillingEvent Event { get; init; }
    string Reason { get; init; }
    bool Verified { get; init; }
  interface IBillingAppAdapter
    abstract Task ApplyEventAsync(BillingEvent evt, CancellationToken cancellationToken)
    abstract Task<BillingPlanDescriptor> GetPlanAsync(string planId, CancellationToken cancellationToken)
    abstract Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken cancellationToken)
  interface IBillingConnectAccountStore
    abstract Task ClearAsync(CancellationToken cancellationToken = null)
    abstract Task<string> GetAsync(CancellationToken cancellationToken = null)
    abstract Task SetAsync(string connectedAccountId, CancellationToken cancellationToken = null)
  interface IBillingCreditStore
    abstract Task<int> DeductAsync(string appCustomerKey, string sku, int credits, string idempotencyKey, CancellationToken cancellationToken = null)
    abstract Task<int> GetCreditsAsync(string appCustomerKey, string sku, CancellationToken cancellationToken = null)
    abstract Task<int> GrantAsync(string appCustomerKey, string sku, int credits, string idempotencyKey, CancellationToken cancellationToken = null)
