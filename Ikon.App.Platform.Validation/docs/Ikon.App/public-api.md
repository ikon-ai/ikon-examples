# Ikon.App Public API

namespace Ikon.App
  sealed class AppAttribute : Attribute
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, string[]? dependencies = null)
    // Each listed app must reach ready state before this app's Joined callback fires — use it to order dependent app startup.
    string[] Dependencies { get; }
    string? Description { get; }
    string? Guid { get; }
    string? Name { get; }
    string? ProductId { get; }
    Opcode ReceiveOpcodeGroups { get; }
    Opcode SendOpcodeGroups { get; }
    UserType UserType { get; }
    int Version { get; }
  // Register every route before calling StartAsync; routes added afterward are not served.
  sealed class AppEndpointHost : IAsyncDisposable
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    bool HasPublicUrl { get; }
    // Throws InvalidOperationException when read before StartAsync has completed.
    int LocalPort { get; }
    Action? OnRequest { get; set; }
    // Throws InvalidOperationException when read before the relay tunnel is allocated; guard with HasPublicUrl when the relay may be unreachable.
    string PublicUrl { get; }
    ValueTask DisposeAsync()
    void MapDelete(string pattern, Func<HttpContext, Task> handler)
    void MapGet(string pattern, Func<HttpContext, Task> handler)
    void MapMethods(string pattern, string method, Func<HttpContext, Task> handler)
    void MapPatch(string pattern, Func<HttpContext, Task> handler)
    void MapPost(string pattern, Func<HttpContext, Task> handler)
    void MapPut(string pattern, Func<HttpContext, Task> handler)
    // The framework closes and disposes the socket once the handler returns; do not dispose it or use it past the handler's completion.
    void MapWebSocket(string pattern, Func<HttpContext, WebSocket, Task> handler)
    // Returns as soon as the host is serving and keeps running in the background — it does not block for the host's lifetime. A failed relay allocation is non-fatal.
    Task StartAsync(CancellationToken cancellationToken = default)
    Task StopAsync(CancellationToken cancellationToken = default)
    // Only for an app whose endpoints are useless without their public URL, and which would rather start late than start wrong — a relay being redeployed takes a few seconds to come back. Do NOT await this on the app initialization path of an app that renders UI: it blocks first paint on something the app does not need in order to draw.
    Task<bool> WaitForPublicUrlAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    // Fires only for the background-retry allocation; not raised when the tunnel was already allocated during StartAsync.
    event Action<string>? PublicUrlAvailable
  static class AppMessaging
    // Filtered by the type's opcode; the handler receives the decoded payload and the sender's client session id. Dispose the returned handle to unsubscribe.
    static IDisposable OnMessage<T>(this IMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    // There is no implicit broadcast — you must pass the explicit recipient session IDs. Whether the type travels reliably or unreliably is declared on its .tp schema, not here.
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
  sealed class AppServices : AsyncLocalInstance<AppServices>
    ctor()
    IAppBase? HostApp { get; }
    bool IsReady { get; }
    Secrets Secrets { get; }
    DbConnection Database(string databaseName)
    Task<DbConnection> OpenDatabaseAsync(string databaseName, CancellationToken ct = default)
    Task WhenReadyAsync()
  delegate AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
  // Three ways to send audio, by pacing: SpeakAsync / SendSpeech are real-time paced by the speech mixer and new speech interrupts current speech with a fade — the default for spoken replies. StreamAsync plays a complete clip (decoded file, generated music) paced to real time, without the mixer's interruption semantics. SendImmediateAsync transmits at once with no pacing — only for audio already produced in real time or very short clips; a long clip sent this way arrives all at once and can overflow client audio buffers.
  class Audio
    ctor(IAppBase app)
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    AudioMetrics Metrics { get; }
    SpeechMixer SpeechMixer { get; }
    ValueTask CloseAllAsync()
    ValueTask CloseAsync(string? streamId = null)
    AudioOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    AudioPlaybackStatus? GetPlaybackStatus(int clientSessionId, string? streamId = null)
    // Delivery is unpaced: the client receives everything as fast as it encodes. Callers own the real-time pacing, so feed this method chunks as they are produced, not a whole clip at once.
    ValueTask SendImmediateAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Real-time paced by the speech mixer, so fast producers (typical TTS) cannot overflow client audio buffers; a chunk with a new id interrupts current playback with a fade. Returns immediately — playback happens in the background.
    void SendSpeech(AudioChunk audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Completes at end of mixer playout (pause-aware, real-time paced), not at end of generation. Long texts are backpressure-paced against the bounded mixer buffer, so any length is safe. An interruption by a newer Speak call completes the task quietly.
    Task SpeakAndWaitAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // Each call interrupts the previous one: it fades out whatever is still playing and cancels the prior call's generation, so a new utterance supersedes the old. Defaults to SpeechGeneratorModel.ElevenFlash25. Drive SpeechGenerator + SendSpeech yourself instead when you need overlapping speakers, playback that must not interrupt what is already playing, or raw access to the generated samples.
    Task SpeakAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // One call streams one whole clip on its stream id. Do not run two concurrent calls on the same stream id — the interleaved frames would corrupt client playback; use distinct stream ids or await the previous call first. Cancelling stops the clip early and closes it with a final end-of-stream frame.
    Task StreamAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, string? streamId = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // Call once during app setup. Mutually exclusive with UseTurnDetection, and calling it a second time throws — either conflict raises InvalidOperationException.
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01f, bool requireCorrelatedStream = true, string language = "", TimeSpan? timeout = null)
    // Call once during app setup. Mutually exclusive with UseSpeechRecognition, and calling it a second time throws — either conflict raises InvalidOperationException.
    void UseTurnDetection(SpeechRecognizerModel model = WhisperLarge3Turbo, string language = "", TurnDetectorConfig? config = null, bool speculative = true, bool pauseWhileAppSpeaking = true, bool requireCorrelatedStream = true, TimeSpan? timeout = null)
    // args.Samples are decoded float PCM at the sample rate from the stream's begin event; IsFirst/IsLast bracket one captured segment (e.g. one push-to-talk press).
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Handlers may set args.StreamingMode to control when the stream's frames are delivered (streamed live, or buffered until the total duration is known / until the last frame).
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    event AsyncEventHandler<AudioPlaybackReportEventArgs> PlaybackReportReceivedAsync
    // Fires only after UseSpeechRecognition or UseTurnDetection has been called once at setup; subscribing without one of those means this event never fires.
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
    event AsyncEventHandler<TurnSpeculativeEventArgs> TurnSpeculativeAsync
    event AsyncEventHandler<TurnStartedEventArgs> TurnStartedAsync
  class AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, Context clientContext, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string? CorrelationId { get; }
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
    string? CorrelationId { get; }
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
    string? CorrelationId { get; }
    string StreamId { get; }
    string UserId { get; }
  record AudioOutputStreamInfo
    ctor(string StreamId, int TrackId, AudioCodec Codec, int SampleRate, int ChannelCount)
    int ChannelCount { get; init; }
    AudioCodec Codec { get; init; }
    int SampleRate { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
  class AudioPlaybackReportEventArgs : EventArgs
    ctor(AudioPlaybackStatus status)
    AudioPlaybackStatus Status { get; }
  sealed class AudioPlaybackStatus
    ctor()
    TimeSpan BufferedDuration { get; init; }
    int ClientSessionId { get; init; }
    uint Epoch { get; init; }
    TimeSpan? PlayedDuration { get; init; }
    DateTime ReceivedAtUtc { get; init; }
    AudioPlaybackState State { get; init; }
    int TrackId { get; init; }
  class BackgroundWork
    // Calls are ref-counted: the server is notified only on the first StartAsync and the last StopAsync. Dispose the returned scope (or call StopAsync) to release — pair every Start with exactly one release or idle shutdown stays blocked.
    ValueTask<IAsyncDisposable> StartAsync()
    ValueTask StopAsync()
  sealed record ClientAudioCaptureOptions
    ctor()
    bool? AutoGainControl { get; init; }
    int? Bitrate { get; init; }
    static ClientAudioCaptureOptions Default { get; }
    string? DeviceId { get; init; }
    bool? EchoCancellation { get; init; }
    bool? NoiseSuppression { get; init; }
  sealed record ClientContact
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    IReadOnlyList<string> Emails { get; init; }
    IReadOnlyList<string> Names { get; init; }
    IReadOnlyList<string> Phones { get; init; }
  // Each method targets the calling client resolved from the current reactive scope unless a targetId is supplied. When the target client has not registered the backing function the call degrades to the failure value (false/null/empty list) rather than throwing — except the capture methods (StartVideoCaptureAsync, StartAudioCaptureAsync, CaptureImageAsync), which throw NotSupportedException.
  static class ClientFunctions
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> ExitFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<int?> GetBatteryLevelAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<string?> GetLanguageAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<ClientLocation?> GetLocationAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // The value is whatever the browser's Network Information API exposes and mixes two vocabularies: a speed class ("slow-2g", "2g", "3g", "4g") where only that is available — note a fast wifi connection commonly reports "4g" — or a connection medium ("wifi", "cellular", "ethernet", "bluetooth", "none", ...) on platforms that expose it. Treat it as an informational hint, not a reliable wifi/cellular discriminator.
    static Task<string?> GetNetworkTypeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<string?> GetTimezoneAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<string?> GetUrlAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<ClientVisibility> GetVisibilityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> KeepScreenAwakeAsync(bool enabled, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> LoginAsync(string provider, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> LoginShowAsync(string? reason = null, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> LogoutAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> OpenExternalUrlAsync(string url, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<string?> PlaySoundAsync(string url, double volume = 1.0, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Audio bytes are de-duplicated per client session by content hash: the first call uploads the data, later calls with identical bytes send only the hash reference, so a reused sound is never re-transmitted.
    static Task<string?> PlaySoundAsync(byte[] data, string mimeType, double volume = 1.0, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> RequestFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> SetThemeAsync(Theme theme, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> SetThemeAsync(string themeName, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> SnapshotReadyAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> StopCaptureAsync(string streamId, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> StopSoundAsync(string playbackId, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> VibrateAsync(int durationMs, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> VibrateAsync(IReadOnlyList<int> pattern, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> VibrateAsync(string pattern, int? targetId = null, CancellationToken cancellationToken = default)
  enum ClientHardwareAcceleration
    PreferHardware
    PreferSoftware
  sealed record ClientImageCapture
    ctor(string Mime, int Width, int Height, byte[] Data)
    byte[] Data { get; init; }
    int Height { get; init; }
    string Mime { get; init; }
    int Width { get; init; }
  enum ClientImageCaptureFormat
    Jpeg
    Png
  sealed record ClientImageCaptureOptions
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
  sealed record ClientLocation
    ctor(double Latitude, double Longitude, double Accuracy)
    double Accuracy { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
  sealed record ClientMediaDevice
    ctor(string DeviceId, ClientMediaDeviceKind Kind, string Label, string GroupId)
    string DeviceId { get; init; }
    string GroupId { get; init; }
    ClientMediaDeviceKind Kind { get; init; }
    string Label { get; init; }
  enum ClientMediaDeviceKind
    Unknown
    AudioInput
    VideoInput
  sealed class ClientProfile
    ProfileAddress? Address { get; }
    string? BirthDate { get; }
    string? Email { get; }
    string? FirstName { get; }
    string? Gender { get; }
    string Id { get; }
    string? Language { get; }
    string? LastName { get; }
    string? Name { get; }
    string? PhoneNumber { get; }
    string? PreferredName { get; }
    IReadOnlyList<string> Roles { get; }
    string UserId { get; }
    string VisibleName { get; }
    object? GetAttribute(string key)
    TAttributes GetAttributes<TAttributes>() where TAttributes : IProfileAttributes, new()
    bool HasRole(UserRole role)
    void RequireRole(UserRole role)
  // A connected client's profile is cached when it joins, so lookups for connected clients return from cache; a cache miss loads from the backend asynchronously. Lookups return null when the context carries no UserId or the backend has no matching profile.
  class ClientProfiles
    ctor(IAppBase app)
    Task AddRoleAsync(Context clientContext, UserRole role)
    Task AddRoleAsync(Context clientContext, string role)
    void ClearCache()
    Task<IReadOnlyList<ClientProfile>> FindProfilesAsync(Dictionary<string, string> filters, int maxResults = 1000)
    Task<IReadOnlyList<ClientProfile>> GetAllProfilesAsync(int maxResults = 1000)
    Task<TAttributes?> GetAttributesAsync<TAttributes>(Context clientContext) where TAttributes : IProfileAttributes, new()
    Task<ClientProfile?> GetProfileAsync(Context clientContext)
    Task<ClientProfile?> GetProfileAsync(string userId)
    Task RefreshProfileAsync(Context clientContext)
    Task RefreshProfileAsync(string userId)
    Task RemoveRoleAsync(Context clientContext, UserRole role)
    Task RemoveRoleAsync(Context clientContext, string role)
    Task SetAttributesAsync<TAttributes>(Context clientContext, TAttributes attrs) where TAttributes : IProfileAttributes
    Task SetRolesAsync(Context clientContext, IEnumerable<UserRole> roles)
    Task SetRolesAsync(Context clientContext, IEnumerable<string> roles)
    Task UpdateAsync(Context clientContext, Action<ProfileData> update)
  enum ClientVideoCaptureCodec
    H264
    Vp8
    Vp9
    Av1
  sealed record ClientVideoCaptureOptions
    ctor()
    int? Bitrate { get; init; }
    static ClientVideoCaptureOptions DefaultCamera { get; }
    static ClientVideoCaptureOptions DefaultScreen { get; }
    string? DeviceId { get; init; }
    int? Framerate { get; init; }
    ClientHardwareAcceleration? HardwareAcceleration { get; init; }
    int? Height { get; init; }
    int? KeyFrameIntervalFrames { get; init; }
    IReadOnlyList<ClientVideoCaptureCodec>? PreferredCodecs { get; init; }
    int? Width { get; init; }
  enum ClientVideoCaptureSource
    Camera
    Screen
  enum ClientVisibility
    Unknown
    Visible
    Hidden
  // Dates are inclusive and interpreted in UTC. Category filters to one usage category (e.g. llm, image-generation); EventName filters to one full usage event name (e.g. llm.openai.gpt4o.global.output-text-tokens).
  sealed record CostQuery
    ctor(DateOnly StartDate, DateOnly EndDate, string? Category = null, string? EventName = null)
    string? Category { get; init; }
    DateOnly EndDate { get; init; }
    string? EventName { get; init; }
    DateOnly StartDate { get; init; }
  // Accessed via app.Costs. Costs are reported per day and per usage event name; credits are the billing unit. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
  sealed class CostsService
    // Returns one row per day and usage event name; days without usage produce no rows. The result is ordered by date, then event name.
    Task<IReadOnlyList<DailyCost>> GetDailyCostsAsync(CostQuery query, CancellationToken ct = default)
    Task<double> GetTotalCreditsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
  // A [Cron] method behaves like a [Function] in that the trigger resolves it through the FunctionRegistry by name. Applying [Cron] is enough to register the method (as a Local function) — you do not also need [Function], though combining them is fine. The handler takes no caller-supplied arguments. It may optionally accept a host-injected CronContext (fire time + schedule) and/or a CancellationToken that signals app shutdown, in any order — mirroring how an [HttpPost] handler may accept an HttpRequest. Any other parameter fails registration at startup, since the scheduler has nothing to bind it to. Overlap is allowed: a tick fires even if the previous invocation is still running, so guard re-entrancy yourself if it matters.
  sealed class CronAttribute : Attribute
    ctor(string schedule)
    string? Name { get; init; }
    string Schedule { get; }
  // Credits is the cost in platform credits — the unit users are billed in. EventName identifies the AI model and usage kind (e.g. llm.openai.gpt4o.global.output-text-tokens) and Category is its first segment (e.g. llm). TotalUsage is the summed usage amount in the event's native unit (tokens, seconds, generations, ...). RawCostEur is the underlying provider cost in EUR and is null unless the space has raw cost visibility enabled.
  sealed record DailyCost
    ctor(DateOnly Date, string Category, string EventName, double TotalUsage, double Credits, double? RawCostEur)
    string Category { get; init; }
    double Credits { get; init; }
    DateOnly Date { get; init; }
    string EventName { get; init; }
    double? RawCostEur { get; init; }
    double TotalUsage { get; init; }
  // Accessed via app.Email. Every operation requires the app's space to have the Email feature enabled; a call against a non-entitled space throws FeatureNotEnabledException.
  sealed class EmailService
    // The backend resolves the id before deleting and rejects an unknown one, so a repeated delete throws HttpRequestException carrying a 404 rather than being treated as a no-op. Callers sweeping ids they no longer track should catch it.
    Task DeleteAsync(string id, CancellationToken ct = default)
    // The returned EmailAttachmentDownload owns the content stream; dispose it (e.g. await using) to release the underlying connection.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = default)
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = default)
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = default)
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = default)
    // The platform sets the visible From address — set EmailSendRequest.ReplyTo to redirect replies. The send is enqueued: a successful return means the platform accepted the request, not that the recipient received it (transient delivery failures are retried server-side). Total payload is capped at ~10 MB.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
  abstract class EndpointAttribute : Attribute
    // Defaults to EndpointAuth.Grant; setting AuthPolicy overrides it.
    EndpointAuth Auth { get; init; }
    // When non-empty, takes precedence over Auth.
    string? AuthPolicy { get; init; }
    // Empty = derived from the method name (kebab-cased). A {name} segment whose name matches a field of the owner's SessionIdentity record binds the routing identity; other {name} segments bind as ordinary handler parameters. Never declare a /.well-known/*, /ikon/*, or /api path — those are reserved.
    string Path { get; }
  enum EndpointAuth
    Grant
    Public
    Deny
  sealed record EndpointInfo
    ctor()
    string CellType { get; init; }
    string FunctionName { get; init; }
    // Carries no grant: a public endpoint is callable as-is, but a grant/policy endpoint needs a working, identity-bound URL minted via IApp.MintUrlAsync.
    string PublicUrl { get; init; }
  // Fired per chunk with the raw bytes for streaming (transcode/scan/forward); the platform already writes the chunk itself. Bytes are not yet verified — the SHA-256 check runs only after the last chunk and a mismatch discards the whole upload, so never act irreversibly. Data is valid only during the callback — copy it to retain it.
  sealed record FileUploadChunkArgs
    ctor(string UploadId, string FileName, string MimeType, long Size, byte[] Data, long BytesWritten)
    long BytesWritten { get; init; }
    byte[] Data { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Fires only after the byte count and recomputed SHA-256 both match. Exactly one of LocalTempFilePath and AssetUri is non-null. The temp file is deleted when the app stops — move or copy it here to keep it.
  sealed record FileUploadCompleteArgs
    ctor(string UploadId, string FileName, string MimeType, long Size, string? LocalTempFilePath, AssetUri? AssetUri)
    AssetUri? AssetUri { get; init; }
    string FileName { get; init; }
    string? LocalTempFilePath { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Terminal hook for an upload that had started (cancel, 60 s stall, out-of-sequence chunk, byte-count or SHA-256 mismatch, write failure). Uploads the app rejected from PreStart or Start never reach here. Any partial file/asset is already deleted — clean up only app-side state.
  sealed record FileUploadErrorArgs
    ctor(string UploadId, string FileName, string MimeType, long Size, string ErrorMessage)
    string ErrorMessage { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // First hook, before any bytes transfer — the cheapest place to reject (return false or a FileUploadResult and nothing is sent). Hook order: PreStart → Start → Chunk/Progress (per chunk) → Complete on success or Error on failure. Capture Cancel to abort the upload later, e.g. from a UI cancel button.
  sealed record FileUploadPreStartArgs
    ctor(string UploadId, string FileName, string MimeType, long Size, Func<string?, Task> Cancel)
    Func<string?, Task> Cancel { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed record FileUploadProgressArgs
    ctor(string UploadId, string FileName, string MimeType, long Size, double ProgressPercentage, long BytesUploaded)
    long BytesUploaded { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    double ProgressPercentage { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Accepted defaults to true; return true; works via the implicit bool conversion. Set AssetUri to write the upload straight into the asset system instead of a local temp file.
  sealed record FileUploadResult
    ctor()
    bool Accepted { get; init; }
    AssetUri? AssetUri { get; init; }
    static implicit operator FileUploadResult(bool accepted)
  // Last chance to reject the upload, and the last hook where setting FileUploadResult.AssetUri can redirect the bytes into the asset system instead of a temp file. Only hook that carries Hash — do content-duplicate checks here.
  sealed record FileUploadStartArgs
    ctor(string UploadId, string FileName, string MimeType, long Size, string Hash)
    string FileName { get; init; }
    string Hash { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class HttpDeleteAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed class HttpGetAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  abstract class HttpMethodAttribute : EndpointAttribute
    abstract string Method { get; }
  sealed class HttpPatchAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed class HttpPostAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed class HttpPutAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed record HttpRequest
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  // An endpoint method may return any serializable value for an automatic 200 + JSON response, or return an HttpResult to control status code, content type, and body.
  sealed record HttpResult
    ctor(int StatusCode, object? Body = null, string ContentType = "application/json")
    object? Body { get; init; }
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
  interface IApp<out TSessionIdentity, out TClientParameters> : IAppBase
    // Resolves the current client from the ambient reactive scope — call it only inside UI.Root() or another ReactiveScope context; outside one there is no current client and it throws.
    virtual TClientParameters ClientParameters { get; }
    IClientCollection<TClientParameters> Clients { get; }
    TSessionIdentity SessionIdentity { get; }
  interface IAppBase : IMessageChannel
    BackgroundWork BackgroundWork { get; }
    // Costs are reported per day and per usage event name; credits are the billing unit. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
    CostsService Costs { get; }
    // Resolved from the ambient reactive scope: null outside a client scope (e.g. background work, a timer). Identifies the client being served, never this plugin's own connection context.
    virtual Context? CurrentClientContext { get; }
    // Empty string when no client is in scope. This is the correct key for a payment customer key, subscription gating, and per-user state — always populated for a connected client (the real user id when authenticated, else a stable anonymous id).
    virtual string CurrentUserId { get; }
    // Read-only in the cloud — writing to it throws. Use it for reading app-bundled data files, not for runtime writes.
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // Requires the Email feature enabled on the app's organisation/space; calls from a non-entitled space throw FeatureNotEnabledException.
    EmailService Email { get; }
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    GlobalState GlobalState { get; }
    // null except in local dev on a localhost address (no --host-public), where it lets an in-process client reach this exact process over loopback. Via the relay or in the cloud it is null — connect through the normal relay/ApiKey path instead.
    virtual (string Host, int Port)? LocalLoopbackEndpoint { get; }
    // Defaults to the server's memory-derived limit; setting any value fully overrides that default and takes effect immediately. New connections are rejected once the limit is reached.
    int MaxClients { get; set; }
    int MaxMemoryLimitMb { get; }
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    NotificationService Notifications { get; }
    PaymentsService Payments { get; }
    virtual string PublicUrl { get; }
    // Values are fetched once at startup and read synchronously; changes made with ikon app secret set while the app runs take effect only after a restart.
    Secrets Secrets { get; }
    // Consulted only during build-time snapshot capture. Returned routes are unioned with the [BootSnapshot] Routes list from ikon-config.toml, validated, and deduped.
    Func<Task<IEnumerable<string>>>? SnapshotRoutesProvider { get; set; }
    // Enabled by default. Applies only to clients that connect after it is set; already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    // Enabled by default. Disable (e.g. in Main) for apps with no audio/video or low-latency data to save per-client peer-setup cost. Applies only to clients that connect afterward; already-connected clients are unaffected until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Persist the returned bytes as your system of record — the platform's session retention is short. Blocks until the signer completes the ceremony and the platform packages the signed PDF.
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default)
    // The caller owns the returned connection — open and dispose it (e.g. await using var connection = app.Database("mydb");). Throws ArgumentException when no configured database has that name.
    virtual DbConnection Database(string databaseName)
    // Completes only when the persisted deletions have finished. Erasure is idempotent — erasing a user with no stored state is a no-op.
    virtual Task EraseUserStateAsync(string userId)
    virtual string JoinUrl(object? queryParams = null)
    // Identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), never by URL path — the path is what minting returns. Omitting identity (null) pins this instance's own session on an app endpoint so the URL routes back here, and pins nothing on a cell endpoint. Grants are non-expiring unless you pass expiresIn.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    virtual Task<IReadOnlyDictionary<string, MintedUrl>> MintUrlsAsync(IEnumerable<string> endpoints, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // Bind your listener to the returned RelayEndpoint.LocalPort; the tunnel is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the endpoint to release it.
    Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", int localPort = 0, CancellationToken ct = default)
    // Verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier. Blocks until the user completes the challenge in their browser.
    Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = default)
    virtual Task RevokeGroupAsync(string group, CancellationToken ct = default)
    virtual Task RevokeUrlAsync(string grantId, CancellationToken ct = default)
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    // Fires after app creation but before Main(). Do not subscribe from inside Main() — it has already fired by then and the handler will never run.
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
    // At-least-once delivery — the handler must be idempotent. Throwing marks the erasure incomplete and it is redelivered on a later session start.
    event AsyncEventHandler<UserDataErasureEventArgs> UserDataErasureAsync
  static class IAppEventExtensions
    static void OnClientJoined(this IAppBase app, Func<Context, Task> handler)
    static void OnClientJoined<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnClientLeft(this IAppBase app, Func<Context, Task> handler)
    static void OnClientLeft<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnMessageReceived(this IAppBase app, Func<ProtocolMessage, Task> handler)
    static void OnSnapshotRoutes(this IAppBase app, Func<Task<IEnumerable<string>>> provider)
    static void OnStarting(this IAppBase app, Func<Task> handler)
    static void OnStopping(this IAppBase app, Func<Task> handler)
    static void OnUserDataErasure(this IAppBase app, Func<string, Task> handler)
  interface IClient<out TClientParameters>
    TClientParameters Parameters { get; }
    int SessionId { get; }
  interface IClientCollection<out TClientParameters> : IEnumerable<IClient<TClientParameters>>
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters>? this[int clientSessionId] { get; }
  interface IProfileAttributes
  // Sibling of HttpMethodAttribute: both declare an inbound HTTP endpoint over the shared addressing + identity model (see EndpointAttribute), differing only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients. Each tool is reachable two ways: through the owner's fixed JSON-RPC multiplexer ({owner}/mcp — tools/list + tools/call, and the only surface that streams notifications/progress over SSE), and as its own directly-callable POST endpoint whose body IS the tool's arguments object. That per-tool path defaults to the kebab-cased method name and is overridable via EndpointAttribute.Path — the override adjusts only this tool's own endpoint, never the shared multiplexer. The same method may also carry a verb-named REST attribute ([HttpPost] etc.); then that route serves the REST surface and the per-tool MCP endpoint is suppressed. The governance subject id is always the structural "{Type}.{Method}".
  sealed class McpAttribute : EndpointAttribute
    ctor()
    ctor(string path)
    // Set this explicitly; the method's XML doc summary is never used as a fallback.
    string Description { get; init; }
    string? Name { get; init; }
  // Sibling of McpAttribute — same cell-method-as-callable model, different MCP verb shape: • Static resource — method takes no arguments; the URI is the literal UriTemplate with no placeholders. Lists in resources/list. • Dynamic resource — method takes parameters that map to {placeholder} segments in the URI template by name. Lists in resources/templates/list; the client crafts a concrete URI and reads it. Read-only by spec — authors should not put side effects in resource methods (the same governance hook still fires on every read with Operation = "resource", so policy authors can distinguish read access from tool dispatch).
  sealed class McpResourceAttribute : Attribute
    ctor(string uriTemplate)
    string Description { get; init; }
    string MimeType { get; init; }
    string? Name { get; init; }
    // Required. Placeholder names must exactly match the cell method's parameter names.
    string UriTemplate { get; }
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
  sealed record MintedUrl
    ctor(string Url, string GrantId, DateTimeOffset? ExpiresAt)
    DateTimeOffset? ExpiresAt { get; init; }
    string GrantId { get; init; }
    string Url { get; init; }
  class Navigation
    string? CurrentPath { get; }
    // Round-trips to the live client over the connection rather than reading server state; returns null when the client doesn't answer or isn't connected.
    Task<string?> GetPathAsync(int targetId)
    // Acts on the client of the ambient ClientScope — call from a client-scoped context. Returns null outside a client scope or when the client doesn't answer.
    Task<string?> GetPathAsync()
    // Rejects paths under the platform-reserved /ikon and /api prefixes (throws ArgumentException) — the load balancer owns those. The client's existing query string is preserved unless path carries its own.
    Task<bool> SetPathAsync(int targetId, string path, bool replace = false)
    // Acts on the client of the ambient ClientScope — call from a client-scoped context (event handler, function call, reactive render). Rejects reserved /ikon and /api paths (throws ArgumentException), same as the targetId overload.
    Task<bool> SetPathAsync(string path, bool replace = false)
    // Fires on any client URL change — link, back button, reload, or the app's own SetPathAsync. Handlers run on a background task in the navigating client's UserScope/ClientScope, so scoped reactives resolve to that client. A handler exception is logged and swallowed, never reaching the client.
    event AsyncEventHandler<NavigationPathChangedEventArgs> PathChangedAsync
  class NavigationPathChangedEventArgs : EventArgs
    ctor(string url, Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string Path { get; }
    string Url { get; }
    string UserId { get; }
  sealed record NotificationContent
    ctor(string Title, string? Body = null, string? IconUrl = null, string? Tag = null, string? LaunchUrl = null, string? Data = null)
    string? Body { get; init; }
    string? Data { get; init; }
    string? IconUrl { get; init; }
    string? LaunchUrl { get; init; }
    string? Tag { get; init; }
    string Title { get; init; }
  enum NotificationPermission
    Default
    Granted
    Denied
    Unsupported
  sealed record NotificationSendResult
    ctor(int SessionId, bool Delivered, NotificationPermission Permission)
    bool Delivered { get; init; }
    NotificationPermission Permission { get; init; }
    int SessionId { get; init; }
  // Accessed via app.Notifications. Client permission is requested lazily on the first actual send, not when the app opens. SendToUserAsync automatically falls back to offline OS push (Web Push / FCM) when the target user has no connected session.
  sealed class NotificationService
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // Returns one result per connected session for the user. An empty list means the user had no connected session and only offline push was attempted — it is not an error.
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = default)
  // Use for app-wide configuration the app instance owns. For per-session-identity state (the typical app routing key) use PersistentSessionReactive<T>; for per-user state use PersistentUserReactive<T>.
  class PersistentReactive<T> : Reactive<T>
    ctor(T initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentReactive<T>. For per-user dictionaries use PersistentUserReactiveDictionary<TKey, TValue>.
  class PersistentReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue> where TKey : notnull
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentReactive<T>. For per-user sets use PersistentUserReactiveHashSet<T>.
  class PersistentReactiveHashSet<T> : ReactiveHashSet<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentReactive<T>. For per-user lists use PersistentUserReactiveList<T>.
  class PersistentReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // This is the natural choice for state that belongs to a specific app instance, since the session identity already determines instance routing.
  class PersistentSessionReactive<T> : Reactive<T>
    ctor(T initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentSessionReactive<T>, which is the natural choice for dictionary state belonging to a specific app instance.
  class PersistentSessionReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue> where TKey : notnull
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentSessionReactive<T>, which is the natural choice for set state belonging to a specific app instance.
  class PersistentSessionReactiveHashSet<T> : ReactiveHashSet<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentSessionReactive<T>, which is the natural choice for list state belonging to a specific app instance.
  class PersistentSessionReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  class PersistentUserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    void ClearFor(string userId)
    void SetFor(string userId, T value)
    void UpdateFor(string userId, Func<T, T> mutator)
    T ValueFor(string userId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    void ClearFor(string userId)
    bool RemoveFor(string userId, TKey key)
    void SetFor(string userId, TKey key, TValue value)
    void UpdateFor(string userId, Action<Dictionary<TKey, TValue>> transform)
    IReadOnlyDictionary<TKey, TValue> ValueFor(string userId)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveHashSet<T> : ReactiveHashSet<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    bool AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Action<HashSet<T>> transform)
    IReadOnlyCollection<T> ValueFor(string userId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    void AddFor(string userId, T item)
    void ClearFor(string userId)
    bool RemoveFor(string userId, T item)
    void UpdateFor(string userId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    IReadOnlyList<T> ValueFor(string userId)
  sealed class ProfileAddress
    string? City { get; }
    string? Country { get; }
    string? Municipality { get; }
    string? State { get; }
    string? Street { get; }
    string? Zip { get; }
  // Only properties assigned on this instance are sent; untouched properties are left unchanged. Assigning null to a property is a change too — it clears that field rather than leaving it untouched.
  sealed class ProfileData
    ctor()
    string? AddressCity { get; set; }
    string? AddressCountry { get; set; }
    string? AddressState { get; set; }
    string? AddressStreet { get; set; }
    string? AddressZip { get; set; }
    string? BirthDate { get; set; }
    string? Email { get; set; }
    string? FirstName { get; set; }
    string? Gender { get; set; }
    string? Language { get; set; }
    string? LastName { get; set; }
    string? Name { get; set; }
    string? PhoneNumber { get; set; }
    string? PreferredName { get; set; }
  class RoleRequiredException : Exception
    ctor(string role, string? userId = null)
    string RequiredRole { get; }
    string? UserId { get; }
  // Shards do NOT share reactive state — each shard is an independent instance of the same identity. Declare sharding only for surfaces designed for it: stateless or read-mostly apps (public landing pages, broadcast views), or apps that synchronize through external state (database, assets). Clients are not sticky to a shard across reconnects. Example:
  // [Sharded(2000)]
  // public record SessionIdentity(string? UserId, [property: Sharded(50)] string? Team);
  sealed class ShardedAttribute : Attribute
    ctor(int maxClientsPerShard = 100)
    int MaxClientsPerShard { get; }
    int MaxShards { get; set; }
  sealed class SpeechRecognizedEventArgs : EventArgs
    ctor(string text, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount, int turnId = 0)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string? CorrelationId { get; }
    TimeSpan Duration { get; }
    int SampleCount { get; }
    string StreamId { get; }
    string Text { get; }
    int TurnId { get; }
    string UserId { get; }
  class StartingEventArgs : EventArgs
    ctor()
  class StoppingEventArgs : EventArgs
    ctor()
  enum Theme
    Dark
    Light
  static class ThemeExtensions
    static bool IsDarkTheme(this Context clientContext)
    static string ToThemeName(this Theme theme)
  sealed class TurnSpeculativeEventArgs : EventArgs
    ctor(int turnId, string text, TimeSpan duration, CancellationToken cancellationToken, string streamId, Context clientContext)
    CancellationToken CancellationToken { get; }
    Context ClientContext { get; }
    int ClientSessionId { get; }
    TimeSpan Duration { get; }
    string StreamId { get; }
    string Text { get; }
    int TurnId { get; }
    string UserId { get; }
  sealed class TurnStartedEventArgs : EventArgs
    ctor(int turnId, string streamId, Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string StreamId { get; }
    int TurnId { get; }
    string UserId { get; }
  class UserDataErasureEventArgs : EventArgs
    ctor(string userId)
    string UserId { get; }
  enum UserRole
    Guest
    User
    Moderator
    Admin
  class Video
    ctor(IAppBase app)
    ValueTask CloseAllAsync()
    ValueTask CloseAsync(string? streamId = null)
    VideoOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Frames are transmitted immediately — the caller owns the pacing. Call once per frame at the source framerate (typically forwarding each incoming frame as it arrives); never loop over a stored clip's frames without pacing.
    ValueTask SendFrameAsync(byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, VideoCodec codec, int width, int height, double framerate, string? streamId = null, IReadOnlyList<int>? targetIds = null, int? trackId = null)
    // args.Data is encoded codec bitstream (see the codec on the stream's begin event), not decoded pixels — forward it as-is (e.g. via SendFrameAsync) or decode it before analysis.
    event AsyncEventHandler<VideoInputFrameEventArgs> VideoInputFrameAsync
    event AsyncEventHandler<VideoInputStreamBeginEventArgs> VideoInputStreamBeginAsync
    event AsyncEventHandler<VideoInputStreamEndEventArgs> VideoInputStreamEndAsync
  class VideoInputFrameEventArgs : EventArgs
    ctor(string streamId, Context clientContext, int trackId, byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string? CorrelationId { get; }
    byte[] Data { get; }
    uint DurationInUs { get; }
    int FrameNumber { get; }
    bool IsKey { get; }
    string StreamId { get; }
    ulong TimestampInUs { get; }
    int TrackId { get; }
    string UserId { get; }
  class VideoInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, VideoCodec codec, string codecDetails, int width, int height, double framerate, Context clientContext, int trackId, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    VideoCodec Codec { get; }
    string CodecDetails { get; }
    string? CorrelationId { get; }
    string Description { get; }
    double Framerate { get; }
    int Height { get; }
    string SourceType { get; }
    string StreamId { get; }
    int TrackId { get; }
    string UserId { get; }
    int Width { get; }
  class VideoInputStreamEndEventArgs : EventArgs
    ctor(string streamId, Context clientContext, int trackId, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string? CorrelationId { get; }
    string StreamId { get; }
    int TrackId { get; }
    string UserId { get; }
  record VideoOutputStreamInfo
    ctor(string StreamId, int TrackId, VideoCodec Codec, int Width, int Height, double Framerate)
    VideoCodec Codec { get; init; }
    double Framerate { get; init; }
    int Height { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
    int Width { get; init; }

namespace Ikon.App.Cells
  // A cell is always shared by its SessionIdentity: every caller that Cells.Connects with the same identity reaches the same instance and its Reactive<T> state — the identity IS the sharing scope (parameterless = one global; keyed = one per key). The runtime picks the transport: a local run hosts every cell in-process (a direct object); in the cloud the cell lives in its own cell-host and callers reach it through a proxy ([HttpGet]/[HttpPost] over HTTP, [Function] methods and Reactive<T> members over an SDK connection). App authors never choose or think about placement — they declare [Cell] and a SessionIdentity, and get exactly what those mean.
  sealed class CellAttribute : Attribute
    ctor()
    int Capacity { get; init; }
    int IdleTtlSeconds { get; init; }
  // Each in-process server runs in its own async-local scope, so Cells.Instance resolves to that server's own host and wiring. The framework calls Initialize once at startup; apps call Connect<TInterface> for each cell access.
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // On a CLOUD run, when TInterface is an interface backed by a [Cell] type, returns a SubstrateCellProxy<TInterface> that dispatches per member: [HttpGet]/[HttpPost] methods over stateless HTTP, [Function] methods and Reactive<T> members over a standard SDK connection to the cell-host. Otherwise — a concrete-type request, or ANY cell on a LOCAL run — returns the local cell instance from this server's CellHost. Local runs host every cell in-process (there is no deployed cell-host to proxy to, and a local run is a single process), so every cell behaves as a normal shared instance locally.
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    ValueTask DisposeAsync()
    const string CellTypeParam
  interface ICell<out TSessionIdentity>
    TSessionIdentity Identity { get; }

namespace Ikon.App.Cron
  sealed record CronContext
    ctor(DateTime FireTimeUtc, string Schedule)
    static CronContext? Current { get; }
    DateTime FireTimeUtc { get; init; }
    string Schedule { get; init; }
    static IDisposable Use(CronContext context)

namespace Ikon.App.Http
  // Exposes the request's resolved identity to handler code on endpoint/MCP-dispatched calls, where the connection-level context carries none. Headers and RawBody are untrusted request inputs — read them for handler logic such as inline webhook-signature verification, but never to derive identity; the target instance is already chosen from trusted sources before the handler runs.
  sealed record HttpCallContext
    ctor(IReadOnlyDictionary<string, string>? SessionIdentity = null, CancellationToken CancellationToken = default, IReadOnlyDictionary<string, string>? Headers = null, string? RawBody = null)
    CancellationToken CancellationToken { get; init; }
    static HttpCallContext? Current { get; }
    IReadOnlyDictionary<string, string>? Headers { get; init; }
    string? RawBody { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentity { get; init; }
    // Null when no HttpCallContext is current or the identity carries no userid (e.g. an anonymous endpoint).
    string? UserId { get; }
    string? Header(string name)
    static IDisposable Use(HttpCallContext context)

namespace Ikon.App.Mcp
  sealed record McpCallContext
    ctor(CancellationToken CancellationToken, Func<ProgressUpdate, Task>? OnProgress, IReadOnlyDictionary<string, string>? SessionIdentityFields = null)
    CancellationToken CancellationToken { get; init; }
    static McpCallContext? Current { get; }
    Func<ProgressUpdate, Task>? OnProgress { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentityFields { get; init; }
    // Null when no McpCallContext is current or the request's claims carry no userid.
    string? UserId { get; }
    static IDisposable Use(McpCallContext context)
  // Progress is a monotonic counter; keep Total constant across a call's updates so clients can render a stable percentage.
  sealed record ProgressUpdate
    ctor(double Progress, double? Total = null, string? Message = null)
    string? Message { get; init; }
    double Progress { get; init; }
    double? Total { get; init; }

namespace Ikon.App.Payments
  enum EntitlementSource
    Unknown
    Subscription
    OneTime
  sealed record OfferPriceSpec
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval? Interval = null, int? IntervalCount = null)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval? Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  sealed record OfferSpec
    ctor(string OfferId, string Name, OfferPriceSpec Price)
    string Name { get; init; }
    string OfferId { get; init; }
    OfferPriceSpec Price { get; init; }
  sealed record Payment
    ctor(string Id, PaymentProvider? Provider, PaymentStatus Status, PaymentKind Kind, string? OfferId, long AmountMinor, string Currency, long AmountRefundedMinor, DateTimeOffset? CreatedAt)
    long AmountMinor { get; init; }
    long AmountRefundedMinor { get; init; }
    DateTimeOffset? CreatedAt { get; init; }
    string Currency { get; init; }
    string Id { get; init; }
    PaymentKind Kind { get; init; }
    string? OfferId { get; init; }
    PaymentProvider? Provider { get; init; }
    PaymentStatus Status { get; init; }
  // The access-control answer [PaymentsRequireEntitlement] gates on. Subscription access carries ExpiresAt (period end plus a grace window) and reports Active false once it has passed; a one-time purchase never expires.
  sealed record PaymentEntitlement
    ctor(string OfferId, bool Active, DateTimeOffset? ExpiresAt, EntitlementSource Source)
    bool Active { get; init; }
    DateTimeOffset? ExpiresAt { get; init; }
    string OfferId { get; init; }
    EntitlementSource Source { get; init; }
  sealed record PaymentEvent
    ctor(string EventId, PaymentProvider? Provider, PaymentEventType? Type, DateTimeOffset? OccurredAt, long Sequence, string PayloadJson)
    string EventId { get; init; }
    DateTimeOffset? OccurredAt { get; init; }
    string PayloadJson { get; init; }
    PaymentProvider? Provider { get; init; }
    long Sequence { get; init; }
    PaymentEventType? Type { get; init; }
    JsonElement Payload()
  enum PaymentEventType
    PaymentAuthorized
    PaymentPaid
    PaymentRefunded
    PaymentCanceled
    PaymentExpired
    PaymentFailed
    SubscriptionActivated
    SubscriptionUpdated
    SubscriptionRenewed
    SubscriptionRenewalFailed
    SubscriptionCanceled
    CatalogUpdated
  enum PaymentKind
    Unknown
    OneTime
    Subscription
  sealed record PaymentLink
    ctor(string Url, string Reference, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    string Reference { get; init; }
    string Url { get; init; }
  sealed record PaymentOffer
    ctor(string OfferId, string Name, IReadOnlyList<PaymentPrice> Prices)
    string Name { get; init; }
    string OfferId { get; init; }
    IReadOnlyList<PaymentPrice> Prices { get; init; }
  // Interval and IntervalCount are meaningful only when Kind is PriceKind.Recurring; a one-time price reports PriceInterval.Unknown.
  sealed record PaymentPrice
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval Interval, int? IntervalCount)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  enum PaymentProvider
    Stripe
    Mollie
    Surfboard
  sealed record PaymentReceipt
    ctor(string? Url, byte[]? Pdf, string? PdfContentType)
    byte[]? Pdf { get; init; }
    string? PdfContentType { get; init; }
    string? Url { get; init; }
  sealed record PaymentReconcileResult
    ctor(PaymentProvider? Provider, int Enqueued)
    int Enqueued { get; init; }
    PaymentProvider? Provider { get; init; }
  sealed record PaymentRefund
    ctor(string Reference, RefundStatus Status)
    string Reference { get; init; }
    RefundStatus Status { get; init; }
  enum PaymentStatus
    Unknown
    Pending
    Paid
    Failed
    Canceled
  sealed record PaymentSubscription
    ctor(string Id, PaymentProvider? Provider, SubscriptionStatus Status, string? OfferId, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd)
    bool CancelAtPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    string Id { get; init; }
    string? OfferId { get; init; }
    PaymentProvider? Provider { get; init; }
    SubscriptionStatus Status { get; init; }
  // On missing access it DENIES with the stable code payments_entitlement_required — catch that in the UI to open a payment link. The customer is resolved from PolicyCallContext.UserId, so a call with no user denies with payments_no_user.
  sealed class PaymentsRequireEntitlementAttribute : PolicyAttribute
    ctor(string offerId)
    string OfferId { get; }
    override IFunctionPolicy CreatePolicy()
  // Reached via app.Payments; one instance per app. Every command takes an optional per-call provider; with none given it uses DefaultProvider or, failing that, the space's enabled provider. The service holds no payment state — every read hits the backend except the synchronous IsEntitled.
  sealed class PaymentsService : AsyncLocalInstance<PaymentsService>
    ctor()
    // Off by default: a payment link for a guest throws InvalidOperationException, because the guest's device-scoped user id changes when they sign in, orphaning the payment and its entitlement. Enable only for purchases that may stay behind (e.g. anonymous tips).
    bool AllowAnonymousPayments { get; set; }
    string? DefaultCancelUrl { get; set; }
    // Leave null (the default) so each command uses the space's enabled provider; set it only to pin one provider for an app with several enabled. A per-call provider argument overrides it.
    PaymentProvider? DefaultProvider { get; set; }
    string? DefaultSuccessUrl { get; set; }
    // Cancels at period end by default; pass immediate to end it now. The entitlement lapses only when the cancellation takes effect.
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Moves subscriptionId to newOfferId (another recurring offer, same currency and interval). On an upgrade (pricier offer) the prorated difference is charged now and the new offer's entitlement is granted immediately; on a downgrade nothing is charged, the current (higher) plan stays available until the next renewal, and renewals then bill the new price. The previous offer's entitlement is left to lapse at its stored expiry. immediateChargeMinor overrides the platform's computed proration for Mollie/Surfboard (developer-owned pricing); it is rejected for Stripe, which prorates natively. Returns a SubscriptionOfferChange whose SubscriptionOfferChange.Changed is false when the subscription was already on the requested offer.
    Task<SubscriptionOfferChange> ChangeSubscriptionOfferAsync(string subscriptionId, string newOfferId, long? immediateChargeMinor = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Idempotent on OfferSpec.OfferId — calling again updates the offer. Stripe provisions a Product + Price; catalog-less providers (Mollie, Surfboard) store the offer on the platform.
    Task<PaymentOffer> CreateOfferAsync(OfferSpec offer, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Paying grants the customer an entitlement for the offer; a recurring offer also starts a subscription. customerKey defaults to the current user. Throws for an anonymous (not signed-in) customer unless AllowAnonymousPayments is set. allowPromotionCodes is honored by Stripe only; other providers ignore it. amountMinorOverride charges the given amount (in minor units) instead of the offer's stored price while still granting the offer's entitlement — for developer-computed pricing such as an upgrade credit. It is supported on one-time offers only; supplying it for a recurring offer is rejected (use ChangeSubscriptionOfferAsync to change a subscription's plan).
    Task<PaymentLink> CreatePaymentLinkAsync(string offerId, string? customerKey = null, string? email = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, bool allowPromotionCodes = false, long? amountMinorOverride = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Charges an ad-hoc amount and grants NO entitlement — reach for the offer overload when a purchase should unlock access. customerKey defaults to the current user; throws for an anonymous customer unless AllowAnonymousPayments is set. allowPromotionCodes is Stripe-only.
    Task<PaymentLink> CreatePaymentLinkAsync(long amountMinor, string currency, string? customerKey = null, string? description = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, bool allowPromotionCodes = false, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Makes a backend call; customerKey defaults to the current user. For gating UI every render, prefer the synchronous IsEntitled instead.
    Task<PaymentEntitlement> GetEntitlementAsync(string offerId, string? customerKey = null, CancellationToken cancellationToken = default)
    // No backend call — safe to read every render, and reading it inside a UI lambda re-renders when the entitlement changes. The first read for an unseen offer returns false and warms the cache in the background, flipping to the real value on a later render. customerKey defaults to the current user.
    bool IsEntitled(string offerId, string? customerKey = null)
    Task<IReadOnlyList<PaymentOffer>> ListOffersAsync(CancellationToken cancellationToken = default)
    Task<IReadOnlyList<Payment>> ListPaymentsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    Task<IReadOnlyList<PaymentSubscription>> ListSubscriptionsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // Recovery path for a missed provider webhook or an app that was offline. Eventually consistent: pulled objects surface as ordinary PaymentEventReceived pushes and entitlement refreshes. A reference (a payment link's checkout-session reference or a subscription id) scopes the pull to one object; otherwise the customer's recent objects, or the space's recent window when no customer is in scope.
    Task<PaymentReconcileResult> ReconcileAsync(string? customerKey = null, string? reference = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Refunds in full by default, or partially via amountMinor. A refund does NOT revoke an entitlement the original payment granted.
    Task<PaymentRefund> RefundAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    Task<bool> RemoveOfferAsync(string offerId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    Task<PaymentReceipt> RequestReceiptAsync(string paymentId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Valid only while the subscription is cancel-at-period-end and its paid period has not ended; an immediate cancel or a fully-ended subscription needs a new checkout. Returns a SubscriptionResume whose SubscriptionResume.SubscriptionId may differ from the input when the provider recreated the subscription (Mollie).
    Task<SubscriptionResume> ResumeSubscriptionAsync(string subscriptionId, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    event Func<PaymentEvent, Task>? PaymentEventReceived
  enum PlanChangeDirection
    Unknown
    Upgrade
    Downgrade
  enum PriceInterval
    Unknown
    Day
    Week
    Month
    Year
  enum PriceKind
    Unknown
    OneTime
    Recurring
  enum RefundStatus
    Unknown
    Pending
    Succeeded
    Failed
  sealed record SubscriptionOfferChange
    ctor(bool Changed, PlanChangeDirection? Direction, long ProrationAmountMinor, string? ProratedChargeRef, string? Currency, string? Effective, PaymentProvider? Provider)
    bool Changed { get; init; }
    string? Currency { get; init; }
    PlanChangeDirection? Direction { get; init; }
    string? Effective { get; init; }
    string? ProratedChargeRef { get; init; }
    long ProrationAmountMinor { get; init; }
    PaymentProvider? Provider { get; init; }
  sealed record SubscriptionResume
    ctor(bool Resumed, string? SubscriptionId, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    bool Resumed { get; init; }
    string? SubscriptionId { get; init; }
  enum SubscriptionStatus
    Unknown
    Incomplete
    IncompleteExpired
    Trialing
    Active
    PastDue
    Unpaid
    Paused
    Canceled
