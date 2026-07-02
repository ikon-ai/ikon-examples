# Ikon.App Public API

namespace Ikon.App
  // Attribute that decorates app classes to configure their connection and messaging behavior
  sealed class AppAttribute : Attribute
    // Attribute that decorates app classes to configure their connection and messaging behavior
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL, GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL, GROUP_APP_LOCAL, string[]? dependencies = null)
    // Internal version constant for the attribute schema itself, used for versioning the App constructor calls if new parameters are added
    int AppVersion { get; }
    // Product IDs of other apps that must be ready before this app's Joined callback is invoked
    string[] Dependencies { get; }
    // Human-readable description of the app. Defaults to "{ClassName} App" if not specified
    string? Description { get; }
    // Stable identifier for the app that persists across class renames. Used by external systems to identify apps independently of their type name
    string? Guid { get; }
    // Display name of the app. Defaults to the class name if not specified
    string? Name { get; }
    // Unique identifier for the app. Defaults to the full type name if not specified
    string? ProductId { get; }
    // Opcode groups this app subscribes to receive messages from
    Opcode ReceiveOpcodeGroups { get; }
    // Opcode groups this app is allowed to send messages to
    Opcode SendOpcodeGroups { get; }
    // Indicates whether the app operates autonomously (Machine) or represents a human user connecting through it (Human). Defaults to Machine
    UserType UserType { get; }
    // Version number of the app
    int Version { get; }
    PluginAttribute ToPluginAttribute(Type owner)
  // Helper class for creating database connections from app configuration.
  static class AppDatabaseConnection
    // Creates a database connection for the specified database name from the app's configured databases.
    static DbConnection Create(IAppBase app, string databaseName)
    // Creates a database connection from a database connection info.
    static DbConnection Create(DatabaseConnectionInfo dbInfo)
  // A lightweight HTTP and WebSocket endpoint host built on ASP.NET Core. Construct the host, register routes with MapGet / MapPost / MapWebSocket , and call StartAsync to allocate the relay tunnel and begin serving requests.
  sealed class AppEndpointHost : IAsyncDisposable
    // Creates a new HTTP/WebSocket endpoint host. The relay tunnel is not allocated until StartAsync is called.
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    // The local port Kestrel binds to. Available after StartAsync completes.
    int LocalPort { get; }
    // Invoked once per inbound HTTP/WebSocket request before it is routed. Used to mark external activity (e.g. reset the server's idle timer) so an endpoint-served instance isn't reaped while it is serving traffic. Null = no hook.
    Action? OnRequest { get; set; }
    // The public URL for this endpoint. Available after StartAsync completes.
    string PublicUrl { get; }
    // Stops the host, releases the relay tunnel, and releases all resources.
    ValueTask DisposeAsync()
    // Registers a handler for HTTP DELETE requests matching the specified route pattern.
    void MapDelete(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for HTTP GET requests matching the specified route pattern.
    void MapGet(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for the given HTTP verb(s) matching the specified route pattern.
    void MapMethods(string pattern, string method, Func<HttpContext, Task> handler)
    // Registers a handler for HTTP PATCH requests matching the specified route pattern.
    void MapPatch(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for HTTP POST requests matching the specified route pattern.
    void MapPost(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for HTTP PUT requests matching the specified route pattern.
    void MapPut(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for WebSocket connections matching the specified route pattern. The socket is automatically closed and disposed after the handler completes.
    void MapWebSocket(string pattern, Func<HttpContext, WebSocket, Task> handler)
    // Allocates the relay tunnel, starts Kestrel with the registered routes, and returns immediately while the host continues to run in the background.
    Task StartAsync(CancellationToken cancellationToken = null)
    // Stops the endpoint host gracefully. Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = null)
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own schema/*.tp files (compiled by ikon app teleport build); each carries its own GROUP_APP_LOCAL opcode and is sent/received as a native type — no JSON marshalling. Delivery is server-controlled and explicit: SendMessageAsync``1 always takes the recipient client session IDs — there is no implicit broadcast to every client. Whether a type travels reliably or unreliably is declared on the .tp schema (unreliable = true), not here.
  static class AppMessaging
    static IDisposable OnMessage<T>(IMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    static ValueTask SendMessageAsync<T>(IMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    static ValueTask SendMessageAsync<T>(IMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
  // Delegate for async event handlers in the app lifecycle.
  delegate AsyncEventHandler<TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler`1<TEventArgs>(TEventArgs e)
  // Handles audio streaming, encoding, and decoding for apps
  class Audio
    ctor(IAppBase app)
    // Default encoder options for audio output
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    // Audio stream metrics
    AudioMetrics Metrics { get; }
    // The default speech mixer
    SpeechMixer SpeechMixer { get; }
    // Closes all audio streams.
    ValueTask CloseAllAsync()
    // Closes an audio stream and sends the stream end message.
    ValueTask CloseAsync(string? streamId = null)
    // Gets information about an output stream if it exists.
    AudioOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Sends audio data to the Ikon server.
    ValueTask SendAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Sends audio data through the default speech mixer.
    void SendSpeech(AudioContainer audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Sends audio data through the default speech mixer.
    void SendSpeech(string speechEventId, ReadOnlySpan<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Enable speech-to-text on captured audio. After calling this, every captured audio segment (typically initiated by a CaptureButton or PushToTalkButton) is transcribed when the segment ends, and SpeechRecognizedAsync fires with the recognized text and originating client context.
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01, bool requireCorrelatedStream = true, string language = "", TimeSpan? timeout = null)
    // Event raised when an incoming audio frame is received and decoded
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Event raised when an incoming audio stream begins
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    // Event raised when an incoming audio stream ends
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Event raised when speech-to-text recognition completes for a captured audio segment. Requires UseSpeechRecognition to be called once during app setup. Each press of a PushToTalkButton (or any other capture-button-initiated stream) produces one recognition event when the user releases. Args carry the recognized text plus the originating client context — no streamId-to-client plumbing needed.
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
  // Event arguments raised when an incoming audio frame is received
  class AudioInputFrameEventArgs : EventArgs, ICaptureCorrelationArgs
    // Event arguments raised when an incoming audio frame is received
    ctor(string streamId, Context clientContext, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Correlation identifier inherited from the AudioStreamBegin (e.g., set by a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Whether this is the first frame in a sequence
    bool IsFirst { get; }
    // Whether this is the last frame in a sequence
    bool IsLast { get; }
    // Decoded floating point PCM samples in range [-1.0, 1.0]
    float[] Samples { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // Total duration of the audio if known, otherwise zero
    TimeSpan TotalDuration { get; set; }
    // User identifier
    string UserId { get; }
  // Event arguments raised when an incoming audio stream begins
  class AudioInputStreamBeginEventArgs : EventArgs
    // Event arguments raised when an incoming audio stream begins
    ctor(string streamId, string description, string sourceType, int sampleRate, int channelCount, Context clientContext, int trackId, string? correlationId)
    // Number of audio channels
    int ChannelCount { get; }
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Optional correlation identifier set by the originator (e.g., a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Description of the audio stream
    string Description { get; }
    // Sample rate in Hz
    int SampleRate { get; }
    // Source type of the audio stream (e.g., "microphone")
    string SourceType { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // Controls when frames are output (can be modified by event handler)
    AudioInputStreamingMode StreamingMode { get; set; }
    // Client- and audio-specific track number for the audio stream
    int TrackId { get; }
    // User identifier
    string UserId { get; }
  // Event arguments raised when an incoming audio stream ends
  class AudioInputStreamEndEventArgs : EventArgs
    // Event arguments raised when an incoming audio stream ends
    ctor(string streamId, Context clientContext, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Correlation identifier inherited from the AudioStreamBegin (e.g., set by a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // User identifier
    string UserId { get; }
  // Controls when incoming audio frames are output to listeners
  enum AudioInputStreamingMode
    Streaming
    DelayUntilTotalDurationKnown
    DelayUntilIsLast
  // Information about an output audio stream
  class AudioOutputStreamInfo : IEquatable<AudioOutputStreamInfo>
    // Information about an output audio stream
    ctor(string StreamId, int TrackId, AudioCodec Codec, int SampleRate, int ChannelCount)
    int ChannelCount { get; init; }
    AudioCodec Codec { get; init; }
    int SampleRate { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
  // Signals the server that the plugin is doing background work, preventing the idle shutdown timer from advancing. Supports ref counting for multiple concurrent background work scopes.
  class BackgroundWork
    // Signals that background work has started. Returns an IAsyncDisposable that calls StopAsync() on dispose. Multiple calls are ref counted; the server is only notified on the first Start and last Stop.
    ValueTask<IAsyncDisposable> StartAsync()
    // Signals that one unit of background work has completed. The server is only notified when the last active scope is stopped.
    ValueTask StopAsync()
  // Bridge between a media stream's CorrelationId and a higher-level handler (typically a UI component such as CaptureButton). For audio it dispatches from frame edges (IsFirst/IsLast) so registered callbacks always run before any subsequent AudioInputFrameAsync handler sees a frame from that segment. For video it dispatches from stream begin/end events. In both cases this eliminates the race that previously existed between the UI action dispatch path and the media transport path.
  static class CaptureCorrelationBridge
    // Register a handler that fires when a stream/segment with the given correlation id starts.
    static void RegisterStart(string correlationId, Func<ICaptureCorrelationArgs, Task> handler)
    // Register a handler that fires when a stream/segment with the given correlation id ends.
    static void RegisterStop(string correlationId, Func<ICaptureCorrelationArgs, Task> handler)
    // Remove handlers registered for the given correlation id.
    static void Unregister(string correlationId)
  sealed class ClientAudioCaptureOptions : IEquatable<ClientAudioCaptureOptions>
    ctor()
    bool? AutoGainControl { get; init; }
    int? Bitrate { get; init; }
    static ClientAudioCaptureOptions Default { get; }
    string? DeviceId { get; init; }
    bool? EchoCancellation { get; init; }
    bool? NoiseSuppression { get; init; }
    IReadOnlyList<int>? TargetIds { get; init; }
  // Represents a contact picked from the client's contact list.
  sealed class ClientContact : IEquatable<ClientContact>
    // Represents a contact picked from the client's contact list.
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    // The contact's email addresses.
    IReadOnlyList<string> Emails { get; init; }
    // The contact's names.
    IReadOnlyList<string> Names { get; init; }
    // The contact's phone numbers.
    IReadOnlyList<string> Phones { get; init; }
  // Provides convenient access to pre-agreed client-side functions. These functions are registered by clients (e.g., TypeScript SDK) and can be called from the server.
  static class ClientFunctions
    // Captures a single image from the client's camera.
    static Task<ClientImageCapture> CaptureImageAsync(int targetId, ClientImageCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, CancellationToken cancellationToken = null)
    // Requests the client to exit fullscreen mode.
    static Task<bool> ExitFullscreenAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> ExitFullscreenAsync(CancellationToken cancellationToken = null)
    // Gets the current battery level on the client.
    static Task<int?> GetBatteryLevelAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<int?> GetBatteryLevelAsync(CancellationToken cancellationToken = null)
    // Gets the browser language preference from the client.
    static Task<string?> GetLanguageAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetLanguageAsync(CancellationToken cancellationToken = null)
    // Gets the current GPS location from the client.
    static Task<ClientLocation?> GetLocationAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<ClientLocation?> GetLocationAsync(CancellationToken cancellationToken = null)
    // Gets the list of available media input devices on the client.
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(CancellationToken cancellationToken = null)
    // Gets the current network connection type on the client.
    static Task<string?> GetNetworkTypeAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetNetworkTypeAsync(CancellationToken cancellationToken = null)
    // Reads the client's current notification permission state.
    static Task<NotificationPermission> GetNotificationPermissionAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<NotificationPermission> GetNotificationPermissionAsync(CancellationToken cancellationToken = null)
    // Fetches the client's push subscription so the device can be registered for offline push. Returns null when the client has no subscription (push disabled, permission not granted, or the client cannot subscribe).
    static Task<PushSubscriptionInfo?> GetPushSubscriptionAsync(int targetId, CancellationToken cancellationToken = null)
    // Gets the currently selected UI theme from the client.
    static Task<string?> GetThemeAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetThemeAsync(CancellationToken cancellationToken = null)
    // Gets the browser timezone from the client.
    static Task<string?> GetTimezoneAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetTimezoneAsync(CancellationToken cancellationToken = null)
    // Gets the current browser URL path and query string from the client.
    static Task<string?> GetUrlAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetUrlAsync(CancellationToken cancellationToken = null)
    // Gets the current page visibility state on the client.
    static Task<string?> GetVisibilityAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<string?> GetVisibilityAsync(CancellationToken cancellationToken = null)
    // Prevents or allows the screen to sleep on the client.
    static Task<bool> KeepScreenAwakeAsync(int targetId, bool enabled, CancellationToken cancellationToken = null)
    static Task<bool> KeepScreenAwakeAsync(bool enabled, CancellationToken cancellationToken = null)
    // Prompts the client to show its login UI (deferred login flow).
    static Task<bool> LoginShowAsync(int targetId, string? reason = null, CancellationToken cancellationToken = null)
    // Clears the auth session and reloads the page, returning the client to the login screen.
    static Task<bool> LogoutAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> LogoutAsync(CancellationToken cancellationToken = null)
    // Opens an external URL in a new browser tab on the client.
    static Task<bool> OpenExternalUrlAsync(int targetId, string url, CancellationToken cancellationToken = null)
    static Task<bool> OpenExternalUrlAsync(string url, CancellationToken cancellationToken = null)
    // Plays a sound on the client from a URL.
    static Task<string?> PlaySoundAsync(int targetId, string url, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    // Plays a sound on the client from a byte array. The sound data is cached per session, so subsequent calls with the same data will not re-transmit the audio.
    static Task<string?> PlaySoundAsync(int targetId, byte[] data, string mimeType, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string?> PlaySoundAsync(string url, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    static Task<string?> PlaySoundAsync(byte[] data, string mimeType, double volume = 1, bool loop = false, CancellationToken cancellationToken = null)
    // Requests the client to enter fullscreen mode.
    static Task<bool> RequestFullscreenAsync(int targetId, CancellationToken cancellationToken = null)
    static Task<bool> RequestFullscreenAsync(CancellationToken cancellationToken = null)
    // Scrolls the page to a specific position on the client.
    static Task<bool> ScrollToAsync(int targetId, double x, double y, bool smooth = false, CancellationToken cancellationToken = null)
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, CancellationToken cancellationToken = null)
    // Updates the UI theme on the client.
    static Task<bool> SetThemeAsync(int targetId, string theme, bool persist = true, CancellationToken cancellationToken = null)
    static Task<bool> SetThemeAsync(string theme, bool persist = true, CancellationToken cancellationToken = null)
    // Updates the browser URL without triggering a page reload.
    static Task<bool> SetUrlAsync(int targetId, string url, bool replace = false, bool preserveQueryParams = false, CancellationToken cancellationToken = null)
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, CancellationToken cancellationToken = null)
    // Shows a notification on the client. The client requests notification permission lazily on the first send before displaying. Returns the client's resulting permission state.
    static Task<NotificationPermission> ShowNotificationAsync(int targetId, NotificationContent content, CancellationToken cancellationToken = null)
    static Task<NotificationPermission> ShowNotificationAsync(NotificationContent content, CancellationToken cancellationToken = null)
    // Starts audio capture on the client from the microphone.
    static Task<string> StartAudioCaptureAsync(int targetId, ClientAudioCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, CancellationToken cancellationToken = null)
    // Starts video capture on the client from camera or screen.
    static Task<string> StartVideoCaptureAsync(int targetId, ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, CancellationToken cancellationToken = null)
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, CancellationToken cancellationToken = null)
    // Stops a media capture on the client by its stream ID.
    static Task<bool> StopCaptureAsync(int targetId, string streamId, CancellationToken cancellationToken = null)
    static Task<bool> StopCaptureAsync(string streamId, CancellationToken cancellationToken = null)
    // Stops a playing sound on the client.
    static Task<bool> StopSoundAsync(int targetId, string playbackId, CancellationToken cancellationToken = null)
    static Task<bool> StopSoundAsync(string playbackId, CancellationToken cancellationToken = null)
    // Triggers haptic feedback on supported devices.
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
  // Event arguments for the ClientJoinedAsync event.
  class ClientJoinedEventArgs : EventArgs
    // Event arguments for the ClientJoinedAsync event.
    ctor(Context clientContext)
    // Gets the context of the client that joined.
    Context ClientContext { get; }
    // Gets the session ID of the client that joined.
    int ClientSessionId { get; }
    // Gets the user ID of the client that joined, or an empty string if not authenticated.
    string UserId { get; }
  // Event arguments for the ClientLeftAsync event.
  class ClientLeftEventArgs : EventArgs
    // Event arguments for the ClientLeftAsync event.
    ctor(Context clientContext)
    // Gets the context of the client that left.
    Context ClientContext { get; }
    // Gets the session ID of the client that left.
    int ClientSessionId { get; }
    // Gets the user ID of the client that left, or an empty string if not authenticated.
    string UserId { get; }
  // Represents a geolocation with latitude, longitude, and accuracy in meters.
  sealed class ClientLocation : IEquatable<ClientLocation>
    // Represents a geolocation with latitude, longitude, and accuracy in meters.
    ctor(double Latitude, double Longitude, double Accuracy)
    // The accuracy of the coordinates in meters.
    double Accuracy { get; init; }
    // The latitude coordinate.
    double Latitude { get; init; }
    // The longitude coordinate.
    double Longitude { get; init; }
  static class ClientMediaCaptureSerializer
    static string? SerializeAudioOptions(ClientAudioCaptureOptions? options)
    static string? SerializeImageOptions(ClientImageCaptureOptions? options)
    static string? SerializeVideoOptions(ClientVideoCaptureOptions? options)
  // Represents a media input device available on the client.
  sealed class ClientMediaDevice : IEquatable<ClientMediaDevice>
    // Represents a media input device available on the client.
    ctor(string DeviceId, string Kind, string Label, string GroupId)
    // The unique identifier for the device.
    string DeviceId { get; init; }
    // The group identifier for devices that share the same physical device.
    string GroupId { get; init; }
    // The type of device (audioinput or videoinput).
    string Kind { get; init; }
    // A human-readable label for the device.
    string Label { get; init; }
  // Read-only view of a client's profile. Use ClientProfiles.UpdateAsync to modify profile data.
  sealed class ClientProfile
    // Address information
    ProfileAddress? Address { get; }
    // Birth date
    string? BirthDate { get; }
    // Email address
    string? Email { get; }
    // First name
    string? FirstName { get; }
    // Gender
    string? Gender { get; }
    // Profile ID
    string Id { get; }
    // True if user has admin role
    bool IsAdmin { get; }
    // True if user is a guest (anonymous/unauthenticated)
    bool IsGuest { get; }
    // True if user has moderator role
    bool IsModerator { get; }
    // Preferred language code
    string? Language { get; }
    // Last name
    string? LastName { get; }
    // Display name
    string? Name { get; }
    // Phone number
    string? PhoneNumber { get; }
    // Preferred display name
    string? PreferredName { get; }
    // Raw roles list from backend
    IReadOnlyList<string> Roles { get; }
    // User ID (from Context.UserId)
    string UserId { get; }
    // Computed visible name (PreferredName ?? FirstName ?? empty)
    string VisibleName { get; }
    // Get a specific attribute value by key
    object? GetAttribute(string key)
    TAttributes GetAttributes<TAttributes>() where TAttributes : IProfileAttributes, new()
    // Check if user has a specific built-in role
    bool HasRole(UserRole role)
    // Check if user has a specific role by string name
    bool HasRole(string role)
    bool HasRole<TRole>(TRole role) where TRole : Enum
  // Manages client profiles for an AI app. Profiles are loaded and cached when clients join, and GetProfileAsync loads any uncached profile from the backend on demand.
  class ClientProfiles
    ctor(IAppBase app)
    // Add a role to a client
    Task AddRoleAsync(Context clientContext, UserRole role)
    // Add a role to a client using string role name
    Task AddRoleAsync(Context clientContext, string role)
    // Clear all cached profiles
    void ClearCache()
    // Find profiles by filter criteria
    Task<IReadOnlyList<ClientProfile>> FindProfilesAsync(Dictionary<string, string> filters, int maxResults = 1000)
    // Get all profiles in the space
    Task<IReadOnlyList<ClientProfile>> GetAllProfilesAsync(int maxResults = 1000)
    Task<TAttributes> GetAttributesAsync<TAttributes>(Context clientContext) where TAttributes : IProfileAttributes, new()
    // Get a client's profile, loading it from the backend on a cache miss and caching the result. Connected clients are normally already cached (their profile is loaded when they join), so this usually returns instantly and only hits the backend for an uncached user. Returns null when the context carries no UserId or the backend has no profile for it.
    Task<ClientProfile?> GetProfileAsync(Context clientContext)
    // Get a profile by userId, loading it from the backend on a cache miss.
    Task<ClientProfile?> GetProfileAsync(string userId)
    // Check if client has a specific built-in role
    bool HasRole(Context clientContext, UserRole role)
    // Check if client has a specific role by string name
    bool HasRole(Context clientContext, string role)
    bool HasRole<TRole>(Context clientContext, TRole role) where TRole : Enum
    // Check if client is an admin
    bool IsAdmin(Context clientContext)
    // Check if client is a guest (anonymous/unauthenticated)
    bool IsGuest(Context clientContext)
    // Check if client is a moderator
    bool IsModerator(Context clientContext)
    // Refresh a client's profile from the backend
    Task RefreshProfileAsync(Context clientContext)
    // Refresh a profile from the backend by userId
    Task RefreshProfileAsync(string userId)
    // Remove a role from a client
    Task RemoveRoleAsync(Context clientContext, UserRole role)
    // Remove a role from a client using string role name
    Task RemoveRoleAsync(Context clientContext, string role)
    // Require admin role. Throws if not.
    void RequireAdmin(Context clientContext)
    // Require moderator role. Throws if not.
    void RequireModerator(Context clientContext)
    // Require that the client has the specified role. Throws if not.
    void RequireRole(Context clientContext, UserRole role)
    // Require that the client has the specified role. Throws if not.
    void RequireRole(Context clientContext, string role)
    Task SetAttributesAsync<TAttributes>(Context clientContext, TAttributes attrs) where TAttributes : IProfileAttributes
    // Set roles for a client
    Task SetRolesAsync(Context clientContext, IEnumerable<UserRole> roles)
    // Set roles for a client using string role names
    Task SetRolesAsync(Context clientContext, IEnumerable<string> roles)
    // Update profile fields using a typed ProfileData object
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
    string? DeviceId { get; init; }
    int? Framerate { get; init; }
    ClientHardwareAcceleration? HardwareAcceleration { get; init; }
    int? Height { get; init; }
    int? KeyFrameIntervalFrames { get; init; }
    IReadOnlyList<ClientVideoCaptureCodec>? PreferredCodecs { get; init; }
    IReadOnlyList<int>? TargetIds { get; init; }
    int? Width { get; init; }
  enum ClientVideoCaptureSource
    Camera
    Screen
  static class Constants
    static string DarkTheme
    static string LightTheme
  // Marks a method to run on a cron schedule. Unlike HttpMethodAttribute / [Mcp], a cron job is not externally addressable — it has no path and no edge authorization. The platform discovers [Cron] methods at build time, records each in the app bundle manifest, and the backend schedules them; when a tick fires the app is run under the global (empty) session identity and the target function is invoked through the FunctionRegistry.
  sealed class CronAttribute : Attribute
    // Declares a cron job that runs on schedule .
    ctor(string schedule)
    // Optional registry-name override. When null or empty the function is registered (and triggered) under its full member name "{Type.FullName}.{Method}".
    string? Name { get; init; }
    // The cron expression that schedules this method (standard 5/6-field cron syntax, e.g. "0 * * * *" for hourly). Evaluated by the backend scheduler.
    string Schedule { get; }
  // Platform email surface for an Ikon app — sending custom emails through the platform mailer and reading inbound emails delivered to the app's space. Accessed via app.Email. All operations require the app's organisation/space to have the Email feature enabled; calls against a non-entitled space throw FeatureNotEnabledException .
  sealed class EmailService
    // Removes an inbound email and frees its attachment storage. Idempotent — deleting a missing message succeeds silently.
    Task DeleteAsync(string id, CancellationToken ct = null)
    // Streams a decrypted attachment from the platform. The returned EmailAttachmentDownload owns the content stream — dispose it (e.g. await using) when done.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = null)
    // Lazily enumerates all received emails matching query , transparently following pages until exhausted. Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = null)
    // Fetches a single page of received emails for the app's space. Paginate by passing the returned NextCursor back as Cursor .
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = null)
    // Fetches a single inbound email with decrypted body and parsed envelope.
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = null)
    // Sends a custom HTML email through the platform mailer. The platform sets the visible From address; pass ReplyTo to direct replies elsewhere. The send is enqueued for asynchronous delivery — a successful return means the platform has accepted the request, not that the recipient has received the message. Transient delivery failures are retried server-side. The total payload size (subject, body, attachments, metadata) is capped at roughly 10 MB.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = null)
  // Shared base for the two developer-facing inbound HTTP surfaces, [Rest] and [Mcp]. They differ only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients; addressing, path templating, identity binding, auth, and abuse-control are identical and live here so there is exactly one place to reason about them.
  abstract class EndpointAttribute : Attribute
    // Built-in authorization for this endpoint, resolved at the gateway edge before (and without) provisioning the app. Defaults to Grant (a signed grant URL). Set AuthPolicy instead to name a custom /router/ policy.
    EndpointAuth Auth { get; init; }
    // Name of a custom /router/ edge policy that authorizes this endpoint (an apiKey/hmac/ipAllow helper you defined in router/index.ts). When set (non-empty) it takes precedence over Auth . Authorization lives in /router/, the single auth surface — not in C#.
    string? AuthPolicy { get; init; }
    // External path under the space domain (after {space}.ikonai.app/api). Optional: when omitted (empty) the path is derived from the method name (kebab-cased) — /{method} on the app class, /{cell-type}/{method} on a cell. A leading-slash path is absolute; a relative form ("bump") is resolved against the owner's auto-derived mount point at build time. Route params use {name} syntax. A {name} whose name matches a field of the owner's SessionIdentity record binds into the routing identity (the extrinsic resource the caller names); other {name} segments bind as ordinary handler parameters. Reserved paths the developer must NOT declare: /.well-known/* (RFC), and the /ikon/* + /api subtrees (platform-owned).
    string Path { get; }
    // The effective /router/ policy name this endpoint authorizes with: AuthPolicy when set, otherwise the lower-cased Auth built-in (grant/public/deny). Mirrors the manifest's resolution so runtime discovery and the bundle manifest agree.
    string ResolveAuthPolicy()
  // The built-in authorization for an endpoint — the discoverable, no-/router/-needed options. For a custom edge policy (an apiKey/hmac/ipAllow helper you defined in /router/), set AuthPolicy to its name instead.
  enum EndpointAuth
    Grant
    Public
    Deny
  // Information about an HTTP endpoint exposed by the app — an [HttpGet]/[HttpPost]/[Mcp] surface. Returned by Endpoints for developer convenience.
  sealed class EndpointInfo
    ctor()
    // The cell type for a substrate-cell endpoint (empty for app + AppProcess-cell endpoints). When non-empty, the gateway cell-routes the request to that cell's partitioned instance, keyed by the cell's IdentityFields in the URL; empty means the endpoint resolves to the app instance.
    string CellType { get; set; }
    // The endpoint's registry name — {Owner}_{Method} for typed endpoints (or the explicit FunctionAttribute.Name override). The backend resolves this name when routing.
    string FunctionName { get; set; }
    // The bare public URL for this endpoint under the space domain ({space}.ikonai.app/api/{path}), templated where the path has open {segment}s. It carries NO grant: a public endpoint is callable as-is; a grant/policy endpoint needs a working, identity-bound URL from IApp.MintUrl. The backend reverse-proxies to this instance — cold-starting it in the cloud, or routing to a registered local run.
    string PublicUrl { get; set; }
  sealed class FileUploadCallbackSet
    ctor()
    Func<FileUploadChunkArgs, Task>? OnChunkReceived
    Func<FileUploadCompleteArgs, Task>? OnUploadComplete
    Func<FileUploadErrorArgs, Task>? OnUploadError
    Func<FileUploadPreStartArgs, Task<FileUploadPreStartResult>>? OnUploadPreStart
    Func<FileUploadProgressArgs, Task>? OnUploadProgress
    Func<FileUploadStartArgs, Task<FileUploadStartResult>>? OnUploadStart
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
    string? AssetUri { get; init; }
    string FileName { get; init; }
    string? LocalTempFilePath { get; init; }
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
    Func<string?, Task> Cancel { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class FileUploadPreStartResult : IEquatable<FileUploadPreStartResult>
    ctor()
    ctor(string? assetUri)
    ctor(bool accepted, string? assetUri = null)
    bool Accepted { get; set; }
    string? AssetUri { get; set; }
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
    ctor(string? assetUri)
    ctor(bool accepted, string? assetUri = null)
    bool Accepted { get; set; }
    string? AssetUri { get; set; }
  // Marks a method as a DELETE REST endpoint. See EndpointAttribute .
  sealed class HttpDeleteAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Marks a method on an app or cell as a GET REST endpoint. The framework mounts a route on the owner's AppEndpointHost, binds the request, invokes the method, and serializes the return value; authorization runs at the gateway edge (the endpoint's Auth/router/ policy), not in-process. See EndpointAttribute for path templating and URL-supplied identity.
  sealed class HttpGetAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Shared base for the verb-named REST attributes ([HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch]). The verb is baked into the attribute type — there is no verb enum — which mirrors the ASP.NET Core idiom and so generates reliably from LLMs. All of them share the addressing + identity model on EndpointAttribute ; only the HTTP method differs.
  abstract class HttpMethodAttribute : EndpointAttribute
    // HTTP verb as an uppercase string (GET / POST / PUT / DELETE / PATCH).
    string Method { get; }
  // Marks a method as a PATCH REST endpoint. See EndpointAttribute .
  sealed class HttpPatchAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Marks a method as a POST REST endpoint — the common case (third-party webhooks included; verify the signature from the injected request context). See EndpointAttribute .
  sealed class HttpPostAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Marks a method as a PUT REST endpoint. See EndpointAttribute .
  sealed class HttpPutAttribute : HttpMethodAttribute
    ctor(string path = "")
    string Method { get; }
  // Serializable view of an inbound HTTP request — its method, path, query, headers, and raw body. The dispatcher constructs one per inbound request; a handler reads it (e.g. via HttpCallContext) for the untrusted inputs the typed binding doesn't surface, such as verifying a webhook signature inline.
  sealed class HttpRequest : IEquatable<HttpRequest>
    // Serializable view of an inbound HTTP request — its method, path, query, headers, and raw body. The dispatcher constructs one per inbound request; a handler reads it (e.g. via HttpCallContext) for the untrusted inputs the typed binding doesn't surface, such as verifying a webhook signature inline.
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  // Typed return value from an HttpMethodAttribute -annotated method. Endpoints can return any serializable type for an automatic 200 + JSON response, or return an HttpResult when they need control over status code, content type, or custom body serialization.
  sealed class HttpResult : IEquatable<HttpResult>
    // Typed return value from an HttpMethodAttribute -annotated method. Endpoints can return any serializable type for an automatic 200 + JSON response, or return an HttpResult when they need control over status code, content type, or custom body serialization.
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
  // Base interface for Ikon app hosts providing access to shared state, reactive infrastructure, and lifecycle events.
  interface IAppBase : IMessageChannel
    // Gets the background work tracker that prevents server idle shutdown while work is in progress.
    BackgroundWork BackgroundWork { get; }
    // The Context of the client currently being served — the one rendering the UI or firing the current handler, resolved from the active reactive scope. null when no client is in scope (e.g. background work). Use this to identify the current client — never a plugin's own connection context. For the joining client's context use the ClientJoined event args instead.
    Context? CurrentClientContext { get; }
    // The user id of the client currently being served, or an empty string when no client is in scope. Always populated for a connected client — the real user id for authenticated users, a stable anonymous id otherwise. This is the correct source for a payment customer key, subscription gating, per-user state, etc.
    string CurrentUserId { get; }
    // Gets the path to the Data directory for this app. Files placed in the Data folder of the app project can be accessed at runtime using this path. Note: in cloud, this directory is read-only and writing to it will throw an exception.
    string DataDirectory { get; }
    // Gets the database connection configurations for this app instance.
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // Gets the email service for this app — sending custom emails through the platform mailer and reading inbound emails delivered to this app's space. Requires the Email feature to be enabled on the app's organisation/space; calls against a non-entitled space throw FeatureNotEnabledException .
    EmailService Email { get; }
    // Gets the HTTP endpoints ([HttpGet]/[HttpPost]/[Mcp] surfaces) exposed by this app instance, including ready-to-use public URLs with the current session identity and signed token prefilled. The list is built once before Main() runs, from the endpoints declared on the app class and on loaded [Cell] types.
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    // Gets the platform-wide shared state from the server containing clients, streams, and space/channel info.
    GlobalState GlobalState { get; }
    // The loopback endpoint (host + HTTPS port) of THIS instance's own local server, but ONLY when the server's own URL is a localhost address — i.e. local dev WITHOUT --public-access. This lets an in-process client (e.g. a simulated player, a self-test harness) connect directly over loopback to this exact process instead of routing through the relay. It returns null when the instance is exposed via the relay (--public-access) or runs in the cloud — there the server's own URL is the relay/space URL, a direct socket can't (and shouldn't) reach it, and callers should use the normal relay/ApiKey connect path (which routes to this registered serving instance) instead. The default is null for hosts that don't run a local server; IApp`2 overrides it.
    ValueTuple<string, int>? LocalLoopbackEndpoint { get; }
    // The maximum number of clients this app instance accepts. Initialized to the server's memory-derived limit (computed from the instance's memory budget), so reading it tells you the default ceiling for this instance. You may set it lower to cap the instance below that default, or higher if you know your app's per-client cost is small enough to support more — once the app sets a value it fully overrides the memory-derived default. Once the limit is reached the server rejects further connections. Changes take effect immediately; the new limit is sent to the server.
    int MaxClients { get; set; }
    // Gets the configured maximum memory limit in megabytes for this server instance.
    int MaxMemoryLimitMb { get; }
    // The Parallax mounts this app renders. Each mount produces an independent UI stream addressable from a host UI as <ParallaxView mount="..." />. Defaults to a single mount named "ikon-ui" — the wire-identical shape of every Ikon app today. Apps with multiple panels or mixed Parallax/external regions can replace the value with a longer list at any time; the render loop reacts and emits UIStreamBegin/UIStreamEnd for additions and removals.
    Reactive<IReadOnlyList<string>> Mounts { get; }
    // Gets the navigation helper for managing URL paths and listening to URL changes.
    Navigation Navigation { get; }
    // Gets the notification service for this app — shows user-facing notifications on connected clients (browser notifications on the web, OS notifications on Flutter native apps). Permission is requested on the client lazily, the first time a notification is actually sent.
    NotificationService Notifications { get; }
    // Gets the payments service for this app — offer plans, take one-off and recurring payments, and react to PaymentReceived events. Set up a provider with ikon app payments enable; the backend drives it and the app holds no payment state.
    PaymentsService Payments { get; }
    // Gets the reactive wrapper around GlobalState that provides change notifications.
    ReactiveGlobalState ReactiveGlobalState { get; }
    // Gets the reactive root that manages per-client reactive graphs and update cycles.
    ReactiveRoot ReactiveRoot { get; }
    // Gets the secrets (tokens, API keys, passwords) configured for this app. Values are fetched from the Ikon backend once at app startup and exposed synchronously; changes made via ikon app secret set while the app is running only take effect after a restart.
    Secrets Secrets { get; }
    // Whether this app instance offers the raw UDP / UDP-DTLS transports to connecting clients. Enabled by default. Set to false to disable them. Like WebRtcEnabled this takes effect for clients that connect after it is set (the transports are no longer advertised); already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    // Whether this app instance offers WebRTC transport to connecting clients. Enabled by default. Set to false (e.g. in Main) to disable WebRTC for apps that don't use audio/video or low-latency data — WebRTC peer setup (ICE candidate gathering, DTLS) is a notable per-client memory and allocation cost. Takes effect for clients that connect after it is set: the server stops advertising WebRTC and ignores WebRTC signaling, so no per-client peer state is created. Already-connected clients keep their channels until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Creates a platform-managed eID-backed PAdES signature order for the supplied document(s). The platform navigates the signer's browser to the signing-ceremony URL through the existing client UI surface, awaits the asynchronous packaging completion, and resolves the returned task with the signed PDF and evidence metadata. The returned bytes are the long-term-validation PAdES PDF when the chosen scheme produces it; apps should persist them as the system of record because the platform's session retention is short.
    abstract Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = null)
    // Mint a working, identity-bound URL for one endpoint — the single way to get a callable URL for a grant (default) or policy endpoint. You identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), NOT by its URL path — the path is often derived from the method name (and may be templated), so the path is what minting RETURNS, not what you pass in. The returned URL is the endpoint's PublicUrl with any pinned {placeholder} path segments substituted and a signed ?ikon-grant= appended. identity (an anonymous object, e.g. new { DocumentId = "doc-42" }, or a string dictionary) PINS those identity fields into the grant; fields you omit stay open {captures} for the caller to fill. Omitting identity entirely ( null ) pins THIS instance's own session identity, so the URL routes back to this app instance — the common case. Grants are non-expiring by default — pass expiresIn only for an ephemeral link, and an optional group to revoke a batch together via RevokeGroupAsync . Re-minting the same stable (non-expiring) URL returns an identical URL, so it survives restarts.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = null)
    // Mint working URLs for several endpoints sharing one pinned identity, in a single backend round-trip. Returns a map keyed by the endpoints you passed. See MintUrlAsync .
    virtual Task<IReadOnlyDictionary<string, MintedUrl>> MintUrlsAsync(IEnumerable<string> endpoints, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = null)
    // Dynamically requests a raw TCP/TLS/UDP endpoint. Returns a RelayEndpoint whose LocalPort a listener should bind to; the endpoint is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the returned endpoint to release it. For HTTP/HTTPS endpoints use AppEndpointHost .
    abstract Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", CancellationToken ct = null)
    // Requests a fresh strong-authentication step-up challenge for the current user. Navigates the client browser to the platform's configured identity provider through the existing client UI surface, waits for the user to complete the challenge, and returns the platform-signed step-up assertion JWT. Apps must verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier .
    abstract Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = null)
    // Revoke every URL minted under a shared group tag.
    virtual Task RevokeGroupAsync(string group, CancellationToken ct = null)
    // Revoke a single minted URL by its GrantId .
    virtual Task RevokeUrlAsync(string grantId, CancellationToken ct = null)
    // Event fired when a client joins the session.
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    // Event fired when a client leaves the session.
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    // Event fired for each protocol message received from the server.
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    // Event fired after app instance creation but before Main() is called. Do not subscribe to this event inside Main() as it will not be called after Main. Primarily used by app extensions that receive the host as a constructor parameter.
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    // Event fired before the plugin disconnects, allowing cleanup of resources.
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
  // Convenience subscription helpers for the lifecycle events on IAppBase . The raw event handler shape is AsyncEventHandler<TEventArgs> which expects a single EventArgs parameter — LLM-generated code routinely reaches for app.StartingAsync += async () => ... (zero-arg) or async (sender, args) => ... (two-arg, .NET prior). Both fail to compile against the canonical one-arg delegate. These extension methods accept the LLM-natural shapes directly: app.OnStarting(async () => ...) wires the underlying event; app.OnClientJoined(async ctx => ...) passes the Context straight through so the handler doesn't need to remember to drill into the event-args wrapper.
  static class IAppEventExtensions
    // Subscribe to ClientJoinedAsync with a handler that receives the joining client's Context directly (SessionId, UserId, etc) — skipping the ClientJoinedEventArgs wrapper the raw event emits.
    static void OnClientJoined(IAppBase app, Func<Context, Task> handler)
    static void OnClientJoined<TSessionIdentity, TClientParameters>(IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to ClientLeftAsync with a handler that receives the departing client's Context directly.
    static void OnClientLeft(IAppBase app, Func<Context, Task> handler)
    static void OnClientLeft<TSessionIdentity, TClientParameters>(IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to MessageReceivedAsync with a handler that receives the protocol message directly.
    static void OnMessageReceived(IAppBase app, Func<ProtocolMessage, Task> handler)
    // Subscribe to StartingAsync with a zero-arg async handler. The Starting event carries no data — there's nothing to forward.
    static void OnStarting(IAppBase app, Func<Task> handler)
    // Subscribe to StoppingAsync with a zero-arg async handler.
    static void OnStopping(IAppBase app, Func<Task> handler)
  // App host interface providing typed session identity and client parameters.
  interface IApp<TSessionIdentity, TClientParameters> : IAppBase, IMessageChannel
    // Gets the typed parameters for the current client (determined by ReactiveScope). Must be called inside UI.Root() or a ReactiveScope context.
    TClientParameters ClientParameters { get; }
    // Gets the collection of connected clients with typed parameters. Automatically synced with GlobalState .
    IClientCollection<TClientParameters> Clients { get; }
    // Gets the typed session identity used to determine app instance routing.
    TSessionIdentity SessionIdentity { get; }
  // Common shape used by CaptureCorrelationBridge to dispatch capture start/stop callbacks. Implemented by audio frame args (used for per-segment dispatch) and video stream begin/end args (used for per-stream dispatch).
  interface ICaptureCorrelationArgs
    Context ClientContext { get; }
    string? CorrelationId { get; }
    string StreamId { get; }
  // Collection interface for accessing connected clients. Iterable for the common "broadcast / fan-out" pattern (`foreach (var client in app.Clients)`), indexable by session ID for direct lookups, and exposes Ids when only the connected-session-ids are needed.
  interface IClientCollection<TClientParameters> : IEnumerable, IEnumerable<IClient<TClientParameters>>
    // Gets the number of currently connected clients.
    int Count { get; }
    // Gets the connected client session IDs as an enumerable. Convenience for code that just needs the IDs without the full client objects — e.g. `foreach (var id in app.Clients.Ids) { _scores[id] = 0; }`.
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters>? Item { get; }
    // Alias for Ids — dictionary-shaped mental model. Generated code reaches for both interchangeably.
    IEnumerable<int> Keys { get; }
  // Interface representing a connected client with typed parameters.
  interface IClient<TClientParameters>
    // Gets the typed parameters for this client.
    TClientParameters Parameters { get; }
    // Gets the session id of this client — the same id used to index IClientCollection`1 and to target client-directed APIs.
    int SessionId { get; }
  // Marker interface for custom profile attribute classes. Implement this interface on classes that define custom profile attributes.
  interface IProfileAttributes
  // Marks a method on an app or cell as an MCP tool. The framework discovers these at startup, reflects the method's parameters into a JSON Schema, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP tools/call requests to it.
  class McpAttribute : EndpointAttribute
    // Declares an MCP tool whose own endpoint path is the kebab-cased method name.
    ctor()
    // Declares an MCP tool whose own directly-callable endpoint is served at path .
    ctor(string path)
    // Description shown to MCP clients so the agent's LLM can decide when to invoke the tool. Empty values pass through verbatim — there is no XML-summary fallback.
    string Description { get; init; }
    // MCP-wire tool name presented to clients in tools/list. Defaults to the method name when null or empty. The governance subject id is always "{Type}.{Method}" regardless of this.
    string? Name { get; init; }
  // Marks a method on a cell as an MCP-exposed resource — read-only data addressed by a URI. The framework reflects the method's parameters into a URI template, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP resources/read requests against the matching URI.
  sealed class McpResourceAttribute : Attribute
    // Marks a method on a cell as an MCP-exposed resource — read-only data addressed by a URI. The framework reflects the method's parameters into a URI template, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP resources/read requests against the matching URI.
    ctor(string uriTemplate)
    // Description shown to MCP clients so the agent (or user, via the client UI) can decide when to fetch the resource. Empty values pass through verbatim.
    string Description { get; init; }
    // MIME type advertised to clients. Defaults to text/plain for string returns and application/octet-stream for binary; override here to be more specific (text/markdown, application/json, image/png, etc.).
    string MimeType { get; init; }
    // Display name shown to MCP clients. Defaults to the method name when null or empty.
    string? Name { get; init; }
    // URI or URI template (RFC-6570 Level 1: {name} placeholders only). Required. Placeholder names must match the cell method's parameter names exactly. The scheme is author-chosen — common conventions are file:///, {cellname}://, or domain-specific scheme like order://, policy://.
    string UriTemplate { get; }
  // Event arguments for the MessageReceivedAsync event.
  class MessageReceivedEventArgs : EventArgs
    // Event arguments for the MessageReceivedAsync event.
    ctor(ProtocolMessage message)
    // Gets the received protocol message.
    ProtocolMessage Message { get; }
  // A minted endpoint URL: the working Url (the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended), the GrantId to revoke it by, and the optional ExpiresAt when a TTL was requested (grants are non-expiring by default).
  sealed class MintedUrl : IEquatable<MintedUrl>
    // A minted endpoint URL: the working Url (the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended), the GrantId to revoke it by, and the optional ExpiresAt when a TTL was requested (grants are non-expiring by default).
    ctor(string Url, string GrantId, DateTimeOffset? ExpiresAt)
    DateTimeOffset? ExpiresAt { get; init; }
    string GrantId { get; init; }
    string Url { get; init; }
  static class ClientFunctions.Names
    static string CaptureImage
    static string ExitFullscreen
    static string GetBatteryLevel
    static string GetLanguage
    static string GetLocation
    static string GetMediaDevices
    static string GetNetworkType
    static string GetNotificationPermission
    static string GetPushSubscription
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
    static string ShowNotification
    static string StartAudioCapture
    static string StartVideoCapture
    static string StopCapture
    static string StopSound
    static string Vibrate
  class Navigation : IReactiveWithState
    Task<string?> GetPathAsync(int targetId)
    Task<string?> GetPathAsync()
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
  // Per-client convenience for sending a notification straight to a connected client — await app.Clients[id].NotifyAsync("Title", "Body") — without going through SendToSessionAsync with an explicit session id.
  static class NotificationClientExtensions
    static Task<NotificationPermission> NotifyAsync<TClientParameters>(IClient<TClientParameters> client, NotificationContent content, CancellationToken ct = null)
    static Task<NotificationPermission> NotifyAsync<TClientParameters>(IClient<TClientParameters> client, string title, string? body = null, CancellationToken ct = null)
  // Content of a user-facing notification surfaced on the client device (browser notification on the web, OS notification on Flutter native apps).
  sealed class NotificationContent : IEquatable<NotificationContent>
    // Content of a user-facing notification surfaced on the client device (browser notification on the web, OS notification on Flutter native apps).
    ctor(string Title, string? Body = null, string? IconUrl = null, string? Tag = null, string? LaunchUrl = null, string? Data = null)
    // Optional body text shown below the title.
    string? Body { get; init; }
    // Optional opaque JSON payload the app receives back when the user taps the notification.
    string? Data { get; init; }
    // Optional URL of an icon image shown with the notification.
    string? IconUrl { get; init; }
    // Optional in-app path the client navigates to when the user taps the notification.
    string? LaunchUrl { get; init; }
    // Optional collapse key — a later notification with the same tag replaces an existing one instead of stacking.
    string? Tag { get; init; }
    // Notification title. Required.
    string Title { get; init; }
  // The notification permission state of a client, as reported by the browser / OS.
  enum NotificationPermission
    Default
    Granted
    Denied
    Unsupported
  // Outcome of sending a notification to a single client session.
  sealed class NotificationSendResult : IEquatable<NotificationSendResult>
    // Outcome of sending a notification to a single client session.
    ctor(int SessionId, bool Delivered, NotificationPermission Permission)
    // True when the client actually displayed the notification (permission granted).
    bool Delivered { get; init; }
    // The client's resulting permission state after the send attempt.
    NotificationPermission Permission { get; init; }
    // The target client session id.
    int SessionId { get; init; }
  // Platform notification surface for an Ikon app — shows user-facing notifications on connected clients. Accessed via app.Notifications. Connected clients receive the notification immediately (foreground). Permission is requested lazily on the client the first time a notification is actually sent, not when the app opens. SendToUserAsync fans out to every connected session for that user; if the user has no connected session it falls back to offline push (an OS notification) through the backend push hub. Offline push is server-orchestrated: when a foreground send is granted, the client's push subscription is fetched and registered with the backend, which then delivers via Web Push / FCM while the user is disconnected.
  sealed class NotificationService
    // Shows a notification on all currently-connected client sessions. Returns one result per session.
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = null)
    // Reads a client's current notification permission state without sending anything.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = null)
    // Shows a notification on a single connected client session. The client requests notification permission lazily (on this first send) before displaying. Returns the per-session delivery and permission outcome.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = null)
    // Shows a notification on every currently-connected session belonging to userId (a user may be connected from several devices). When the user has no connected session, falls back to offline push — an OS notification delivered through the backend push hub. Returns one result per targeted session (empty when the user was offline and only push was attempted).
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = null)
  // A reactive value persisted globally for the app within its space. Shared across all session identities and users; one value per app deployment.
  class PersistentReactive<T> : Reactive<T>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A reactive value persisted per session identity. Apps with the same routing key share the same value; different routing keys have isolated values.
  class PersistentSessionReactive<T> : Reactive<T>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A reactive value persisted per user, partitioned at runtime by UserScope . Each user sees their own value across all of their client sessions.
  class PersistentUserReactive<T> : Reactive<T, UserScope>, IPersistedReactive
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // Read-only view of a client's address.
  sealed class ProfileAddress
    string? City { get; }
    string? Country { get; }
    string? Municipality { get; }
    string? State { get; }
    string? Street { get; }
    string? Zip { get; }
  // Mutable class for updating profile fields. Only properties that are set will be sent to the backend.
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
  // A client's push subscription, used to register the device for offline push. Web clients return Endpoint + P256dh + Auth ; Flutter clients return Token . Platform is "web" or "fcm".
  sealed class PushSubscriptionInfo : IEquatable<PushSubscriptionInfo>
    // A client's push subscription, used to register the device for offline push. Web clients return Endpoint + P256dh + Auth ; Flutter clients return Token . Platform is "web" or "fcm".
    ctor(string Platform, string? Endpoint, string? Token, string? P256dh, string? Auth, string? DeviceId)
    string? Auth { get; init; }
    string? DeviceId { get; init; }
    string? Endpoint { get; init; }
    string? P256dh { get; init; }
    string Platform { get; init; }
    string? Token { get; init; }
  // Manages per-client reactive graphs and update cycles for an Ikon app. Automatically stops when the app's StoppingAsync event fires.
  class ReactiveRoot
    // Creates a new reactive root for the specified app host.
    ctor(IAppBase app, int updateIntervalMs = 1000)
    // Gets the reactive manager that coordinates all reactive objects in the app.
    ReactiveManager ReactiveManager { get; }
    Task RunAsync(Func<Task> render, Func<Context, bool>? filter = null)
  // Event arguments raised when speech has been recognized from a captured audio stream.
  sealed class SpeechRecognizedEventArgs : EventArgs
    // Event arguments raised when speech has been recognized from a captured audio stream.
    ctor(string text, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount)
    // Client context of the speaker.
    Context ClientContext { get; }
    // Client session id of the speaker.
    int ClientSessionId { get; }
    // Correlation id of the originating CaptureButton (null for ad-hoc audio streams).
    string? CorrelationId { get; }
    // Duration of the captured audio segment.
    TimeSpan Duration { get; }
    // Total sample count fed to the recognizer.
    int SampleCount { get; }
    // Stream id from which the audio was captured.
    string StreamId { get; }
    // Recognized speech text.
    string Text { get; }
    // User id of the speaker.
    string UserId { get; }
  // Event arguments for the StartingAsync event.
  class StartingEventArgs : EventArgs
    ctor()
  // Event arguments for the StoppingAsync event.
  class StoppingEventArgs : EventArgs
    ctor()
  // Built-in user roles. Maps to role strings stored in profile.
  enum UserRole
    Guest
    User
    Moderator
    Admin
  // Handles video streaming for apps
  class Video
    ctor(IAppBase app)
    // Closes all video streams.
    ValueTask CloseAllAsync()
    // Closes a video stream and sends the stream end message.
    ValueTask CloseAsync(string? streamKey = null)
    // Gets information about an output stream if it exists.
    VideoOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Sends a video frame to the Ikon server.
    ValueTask SendAsync(byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, VideoCodec codec, int width, int height, double framerate, string? streamId = null, IReadOnlyList<int>? targetIds = null, int? trackId = null)
    // Event raised when an incoming video frame is received
    event AsyncEventHandler<VideoInputFrameEventArgs> VideoInputFrameAsync
    // Event raised when an incoming video stream begins
    event AsyncEventHandler<VideoInputStreamBeginEventArgs> VideoInputStreamBeginAsync
    // Event raised when an incoming video stream ends
    event AsyncEventHandler<VideoInputStreamEndEventArgs> VideoInputStreamEndAsync
  // Event arguments raised when an incoming video frame is received
  class VideoInputFrameEventArgs : EventArgs, ICaptureCorrelationArgs
    // Event arguments raised when an incoming video frame is received
    ctor(string streamId, Context clientContext, int trackId, byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Correlation identifier inherited from the originating VideoStreamBegin (e.g., set by a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Encoded video frame data
    byte[] Data { get; }
    // Frame duration in microseconds
    uint DurationInUs { get; }
    // Frame number in the sequence
    int FrameNumber { get; }
    // Whether this is a keyframe
    bool IsKey { get; }
    // Unique identifier for the video stream
    string StreamId { get; }
    // Timestamp in microseconds
    ulong TimestampInUs { get; }
    // Track id for the video stream
    int TrackId { get; }
    // User identifier
    string UserId { get; }
  // Event arguments raised when an incoming video stream begins
  class VideoInputStreamBeginEventArgs : EventArgs, ICaptureCorrelationArgs
    // Event arguments raised when an incoming video stream begins
    ctor(string streamId, string description, string sourceType, VideoCodec codec, string codecDetails, int width, int height, double framerate, Context clientContext, int trackId, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Video codec used for encoding
    VideoCodec Codec { get; }
    // Codec-specific details
    string CodecDetails { get; }
    // Optional correlation identifier set by the originator (e.g., a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Description of the video stream
    string Description { get; }
    // Video framerate
    double Framerate { get; }
    // Video height in pixels
    int Height { get; }
    // Source type of the video stream (e.g., "camera", "screen")
    string SourceType { get; }
    // Unique identifier for the video stream
    string StreamId { get; }
    // Track id for the video stream
    int TrackId { get; }
    // User identifier
    string UserId { get; }
    // Video width in pixels
    int Width { get; }
  // Event arguments raised when an incoming video stream ends
  class VideoInputStreamEndEventArgs : EventArgs, ICaptureCorrelationArgs
    // Event arguments raised when an incoming video stream ends
    ctor(string streamId, Context clientContext, int trackId, string? correlationId)
    // Client context containing user information
    Context ClientContext { get; }
    // Client session identifier
    int ClientSessionId { get; }
    // Correlation identifier inherited from the originating VideoStreamBegin (e.g., set by a CaptureButton). Null for ad-hoc streams.
    string? CorrelationId { get; }
    // Unique identifier for the video stream
    string StreamId { get; }
    // Track number for the video stream
    int TrackId { get; }
    // User identifier
    string UserId { get; }
  // Information about an output video stream
  class VideoOutputStreamInfo : IEquatable<VideoOutputStreamInfo>
    // Information about an output video stream
    ctor(string StreamId, int TrackId, VideoCodec Codec, int Width, int Height, double Framerate)
    VideoCodec Codec { get; init; }
    double Framerate { get; init; }
    int Height { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
    int Width { get; init; }

namespace Ikon.App.Auth
  // OAuth resource-server configuration the platform reads to advertise the protected-resource discovery document (RFC 9728), so an MCP client knows which authorization server to obtain a bearer token from. Bearer-token validation itself would be an edge /router/ bearer policy evaluated at the gateway before provisioning — not an in-process cell — but no such policy is implemented yet (the fail-closed oauth helper was removed).
  static class OAuthAuth
    // Configured issuer URL (IKON_OAUTH_ISSUER) — returned by the protected-resource discovery document. Null when unconfigured.
    static string? ConfiguredIssuer { get; }

namespace Ikon.App.Cells
  // Marks a class as a cell — a headless app addressed by a SessionIdentity record declared inside the class. Discovered by CellHost at startup via reflection over loaded assemblies.
  sealed class CellAttribute : Attribute
    ctor()
    // Number of concurrent instances per addressable key. Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin Resolve``1 across them. For globals (parameterless SessionIdentity) the N instances are eager-spawned at host construction — the load-balanced auth-cell pattern. For keyed cells the N instances are spawned together on first access; sharded keyed cells must tolerate eventual consistency between shards (cells should hold no per-instance state, or persist shared state through an external store).
    int Capacity { get; init; }
    // How long a keyed cell may remain idle before EvictIdle removes it from the directory. Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
    // Where this cell type is hosted. AppProcess (the default) keeps the cell in the app's own `CellHost` — every app process has its own copies, state is not shared across processes. Substrate declares that the cell should be hosted on the platform's cell-deployment substrate, where one instance per (cell-type, SessionIdentity) is shared across all app processes that connect.
    CellProcessScope ProcessScope { get; init; }
  // What the cell-client factory needs to open a standard-SDK connection to a substrate cell-host: the cell type's simple name and its SessionIdentity-record field values.
  sealed class CellConnectRequest : IEquatable<CellConnectRequest>
    // What the cell-client factory needs to open a standard-SDK connection to a substrate cell-host: the cell type's simple name and its SessionIdentity-record field values.
    ctor(string CellTypeName, IReadOnlyDictionary<string, string> Identity)
    string CellTypeName { get; init; }
    IReadOnlyDictionary<string, string> Identity { get; init; }
  // A live standard-SDK connection from an app process to a substrate cell-host IkonServer, paired with the ReactiveRegistry that mirrors the cell's Reactive<T> state. Created lazily by Cells on first need and shared by every SubstrateCellProxy`1 for the same (CellType, SessionIdentity).
  sealed class CellConnection : IAsyncDisposable
    // The connected SDK client to the cell-host IkonServer.
    IkonClient Client { get; }
    // Reactive-subscription layer over Client 's function registry.
    ReactiveRegistry Reactive { get; }
    ValueTask DisposeAsync()
  // In-process directory + spawn substrate for CellAttribute -decorated types. Maps wire interfaces to cell types at startup, then resolves (cellType, SessionIdentity) to a single shared instance per key.
  sealed class CellHost : IAsyncDisposable
    // Construct a host that scans the supplied assemblies for CellAttribute -decorated types. When assemblies is null, scans every loaded assembly in the current AppDomain. Cells whose SessionIdentity record is parameterless (= global) are eager-spawned at construction so they are always-already-provisioned by the time a request lands.
    ctor(IEnumerable<Assembly>? assemblies = null)
    // One canonical cell type per simple name — the exact set ResolveByCellTypeName dispatches to, and what every consumer that turns a cell into an externally-addressable surface iterates (typed-HTTP-endpoint discovery, MCP tool/resource discovery, the inbound path dispatcher). After a hot reload the same logical cell can linger in two AssemblyLoadContexts — the recompiled copy plus the not-yet-collected original — as two distinct Type identities sharing a FullName. The host keeps both internally for load-context-correct wire-interface mapping ( Resolve``1 ), but a surface must be discovered ONCE and bound to the type whose instance dispatch returns: otherwise a duplicate-name registration throws (MCP tools) or a handler built over the non-dispatched copy fails its invoke with a target-type mismatch. Keying on simple name collapses the duplicate to that single dispatchable type.
    IReadOnlyCollection<Type> CellTypes { get; }
    // Dispose every cell instance held by the host. Async disposal is preferred per BCL precedence; IDisposable is honored as a fallback. After disposal, Resolve``1 throws ObjectDisposedException .
    ValueTask DisposeAsync()
    // Evict every keyed cell instance whose idle time exceeds its cell type's IdleTtlSeconds . Cells without a configured TTL are never evicted by this method. Awaits DisposeAsync on each evicted instance that implements it; IDisposable is honored as a fallback. Returns the number of instances removed.
    Task<int> EvictIdleAsync()
    // Evict every keyed cell instance whose last access is strictly before cutoffUtc . Globals are never evicted. Awaits DisposeAsync on each evicted instance that implements it; IDisposable is honored as a fallback. Returns the number of instances removed.
    Task<int> EvictIdleOlderThanAsync(DateTime cutoffUtc)
    // The TSessionIdentity type a CellAttribute -decorated cell binds to, inferred from its primary-constructor's ICell`1 parameter. Returns null if the cell doesn't declare an ICell`1 ctor parameter at all.
    static Type? GetSessionIdentityType(Type cellType)
    // True when the identity record has at least one constructor parameter — i.e. the cell is keyed (different instances per identity value). False for parameterless / global identity types whose only constructor is the synthesised record copy-ctor.
    static bool HasIdentityParameters(Type identityType)
    // Register an externally-constructed instance (typically the running App<TSessionIdentity, TClientParameters> plugin) as a singleton cell. The host treats it like any other [Cell] for discovery + dispatch — its public methods show up in CellTypes , HttpEndpointDiscovery, McpToolDiscovery, and McpResourceDiscovery; ResolveByCellTypeName and Resolve``1 return the registered instance directly. The host does NOT construct, evict, or dispose singletons — lifecycle stays with the external owner.
    void RegisterSingleton(object instance)
    TInterface Resolve<TInterface>(object sessionIdentity) where TInterface : class
    // Resolve (or spawn) a cell instance by the cell type's simple name and a SessionIdentity field dict (typically the URL query params from an inbound endpoint). The host constructs the SessionIdentity record from the dict by matching the record's primary-constructor parameter names; missing nullable/default-valued fields use null/their default; missing required fields throw. Returns the cell instance as Object — callers cast to the wire interface they expect or use reflection to invoke methods.
    object ResolveByCellTypeName(string cellTypeName, IReadOnlyDictionary<string, string> sessionIdentityFields)
    // Look up the registered [Cell] concrete type whose wire-interface mapping matches iface . Returns the same type that Resolve``1 would dispatch to. Used by Cells.Connect<TInterface> to consult the cell's CellAttribute (e.g. for ProcessScope ) before deciding between local resolution and substrate-proxy routing.
    bool TryGetCellTypeForInterface(Type iface, out Type cellType)
    // Raised when a NEW cell type appears in the host after construction — specifically when RegisterSingleton registers an instance whose type wasn't already known. Higher layers (IkonServer) that snapshot the topology at build time — e.g. the discovered MCP-tool host and typed-HTTP-endpoint list — subscribe to rebuild those snapshots. This is load-bearing for app-level [Mcp]: the user's [App] instance is registered lazily on first client join (via HttpEndpointRouting.EnsureCellHost), long after the host's initial discovery walk.
    event Action? TopologyChanged
  // The wire-name conventions for cell members. Both the substrate-cell proxy (the caller) and the cell-host's endpoint-wrapper registration (the producer) build these names; keeping the format in one place stops the two sides from drifting apart.
  static class CellNaming
    // The endpoint registry name for a cell's [HttpGet]/[HttpPost] method: {CellType}_{Method}. The manifest carries this flat name as the endpoint's Name; the backend derives the upstream route /{Owner}/{Method} from it.
    static string EndpointFunctionName(Type cellType, string methodName)
    // The SDK function name for a cell's [Function] method: {CellType.FullName}.{Method}. Matches how FunctionRegistry.RegisterFromInstance names instance methods, so a substrate-cell proxy can call them over its SDK connection to the cell-host.
    static string SdkFunctionName(Type cellType, string methodName)
    // The SDK function name a cell-host exposes to advertise the base URL of its AppEndpointHost — the relay tunnel serving the cell's [HttpGet]/[HttpPost] + [Mcp] routes. A SubstrateCellProxy calls it over the cell-host SDK connection to learn where to POST [HttpGet]/[HttpPost] requests directly, instead of going through the cloud endpoint gateway. Producer (the cell-host startup path) and consumer (SubstrateCellProxy) must agree on this name.
    static string CellEndpointBaseUrlFunctionName
  // Where a CellAttribute -decorated type's instances live.
  enum CellProcessScope
    AppProcess
    Substrate
  // Per-server-scoped accessor (via AsyncLocalInstance`1 — use Cells.Instance) for that server's CellHost plus the wiring substrate-cell proxies need: the endpoint-URL resolver (for [HttpGet]/[HttpPost] methods) and the cell-client factory (for [Function] methods and Reactive<T> state, which ride a standard IkonClient SDK connection to the cell-host).
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // The currently installed process-wide cell host, or null if none has been installed yet. Use this when you want to reuse the shared host with a graceful fallback. For fail-fast access prefer Connect``1 .
    CellHost? Current { get; }
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    // Dispose every live cell-host connection. Call on app shutdown. Idempotent.
    ValueTask DisposeAsync()
    // Install the process-wide cell host AND reset the app wiring: clears the endpoint-URL resolver and the cell-client factory, and drops the connection + proxy registries. Use this for a clean slate — tests re-run it between scenarios, and an app with no platform-installed host falls back to it. Production startup installs the host via InstallHost instead, which keeps the wiring the app already registered (see that method for why).
    void Initialize(CellHost host)
    // Swap the process-wide cell host WITHOUT touching the app-registered endpoint-URL resolver or cell-client factory. Drops the connection + proxy registries because they reference the previous host's cell instances (whose types may live in a now-unloaded AssemblyLoadContext).
    void InstallHost(CellHost host)
    // Register the factory that opens a standard-SDK IkonClient connection to a substrate cell-host. Called by the app host at startup — the app process has the backend context (space id, login) the factory needs. SubstrateCellProxy`1 uses it for [Function]-marked methods and Reactive<T> members; without it, those throw a clear error while [HttpGet]/[HttpPost] methods still work.
    void SetCellClientFactory(Func<CellConnectRequest, Task<IkonClient>> factory)
    // Register the function that maps a endpoint function name (e.g. "LabCell_IncrementHttp") to its public URL. Called by the app host at startup so SubstrateCellProxy`1 can dispatch a substrate cell's [HttpGet]/[HttpPost] methods over stateless HTTP. Methods the resolver returns no URL for fall through to the SDK connection.
    void SetEndpointUrlResolver(Func<string, string?> resolver)
    // Reserved key in an SDK connection's parameters that names the substrate cell type to route to. The cell's SessionIdentity-record fields ride alongside it. MUST stay in sync with the cloud's CELL_TYPE_PARAM in cell-routing.ts — that's what ChannelInstanceService.create keys on to provision a cell-host channel-instance.
    static string CellTypeParam
  // Framework handle injected into a cell's primary constructor. Exposes the SessionIdentity the cell was instantiated for; future revisions add lifetime, config, etc.
  interface ICell<TSessionIdentity>
    // The SessionIdentity record value this cell instance is keyed by.
    TSessionIdentity Identity { get; }
  // Runtime DispatchProxy for a [Cell(ProcessScope = Substrate)] cell type. App processes call the cell as if it were local; the proxy hides the network hop and picks a transport per member: [HttpGet]/[HttpPost] methods — dispatched as stateless HTTP POST. The target is the cell-host's own IkonClient -discovered endpoint base URL when available, falling back to the cloud endpoint-gateway URL otherwise.other methods — dispatched over a standard IkonClient SDK connection to the cell-host (the cell must expose them via [Function] / [RegisterAll] so they are callable on the wire).Reactive<T> members — return a cached local read-only mirror fed by an SDK subscription; reads and Changed events work locally, mutations flow through cell methods. The SDK connection is opened lazily on first need. Even a cell reached only through [HttpGet]/[HttpPost] methods opens one once, to discover the cell-host's endpoint base URL.
  class SubstrateCellProxy<TInterface> : DispatchProxy where TInterface : class
    ctor()
    // Build a proxy implementing TInterface for the given substrate cell.
    static TInterface Create(Type cellType, object sessionIdentity, Func<string, string?> endpointUrlResolver)

namespace Ikon.App.Client
  // Thread-safe implementation of IClientCollection`1 that synchronizes with GlobalState .
  class ClientCollection<TClientParameters> : IClientCollection<TClientParameters>, IEnumerable, IEnumerable<IClient<TClientParameters>>
    ctor()
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters>? Item { get; }
    IEnumerator<IClient<TClientParameters>> GetEnumerator()
  // Implementation of IClient`1 representing a connected client with typed parameters.
  class Client<TClientParameters> : IClient<TClientParameters>
    // Implementation of IClient`1 representing a connected client with typed parameters.
    ctor(int sessionId, TClientParameters parameters)
    TClientParameters Parameters { get; }
    int SessionId { get; }

namespace Ikon.App.Cron
  // Per-invocation context for a CronAttribute handler currently executing. A cron handler may optionally accept one of these (and/or a CancellationToken ) to learn when and why it fired; a parameterless handler is equally valid. AsyncLocal so handler code (and anything it calls) can read it without threading it through every method signature.
  sealed class CronContext : IEquatable<CronContext>
    // Per-invocation context for a CronAttribute handler currently executing. A cron handler may optionally accept one of these (and/or a CancellationToken ) to learn when and why it fired; a parameterless handler is equally valid. AsyncLocal so handler code (and anything it calls) can read it without threading it through every method signature.
    ctor(DateTime FireTimeUtc, string Schedule)
    // The cron context for the invocation currently running on this async flow, or null.
    static CronContext? Current { get; }
    DateTime FireTimeUtc { get; init; }
    string Schedule { get; init; }
    static IDisposable Use(CronContext context)

namespace Ikon.App.Http
  // Per-request context for an HttpMethodAttribute handler currently executing. AsyncLocal so handler code (and anything it calls) can read the request's resolved identity without threading the dict through every method signature. Relationship to other "context" concepts on the platform: SessionIdentity (the typed app/cell record): the routing / instance-partition key. Always present — it's what was used to address the channel-instance this handler runs in. Stable across the cell instance's lifetime.Context (Ikon protocol Context for WS clients): the live client *connection* — sessionId, deviceId, AuthSessionId, UserId from the connect-token. Absent for endpoint/MCP dispatches because there is no live client connection.HttpCallContext.Current (this) and McpCallContext .Current: the *request-scoped overlay* that exposes the per-call resolved identity for handler code to read. Set by the wrapper before the handler runs, cleared after. The point is that handlers reading "who is this call for?" get a non-empty answer on endpoint/MCP-dispatched calls, where the connection-level Context.UserId would be empty. The handler's SessionIdentity record (resolved by CellHost.ResolveByCellTypeName before this context is set) and HttpCallContext.Current.SessionIdentity carry the same information in different shapes: the former is typed and tied to the cell's lifetime; the latter is the raw wire dict tied to the call's lifetime. Headers and RawBody are the UNTRUSTED request inputs, exposed so a handler can do its own logic inline (e.g. verify a Stripe-Signature against the raw body) without a separate auth cell. They must never feed identity resolution — the target instance is already chosen from trusted sources (a signed ikon-grant / policy claims / platform-controlled path+query) before the handler runs, so reading a header cannot retarget the call.
  sealed class HttpCallContext : IEquatable<HttpCallContext>
    // Per-request context for an HttpMethodAttribute handler currently executing. AsyncLocal so handler code (and anything it calls) can read the request's resolved identity without threading the dict through every method signature. Relationship to other "context" concepts on the platform: SessionIdentity (the typed app/cell record): the routing / instance-partition key. Always present — it's what was used to address the channel-instance this handler runs in. Stable across the cell instance's lifetime.Context (Ikon protocol Context for WS clients): the live client *connection* — sessionId, deviceId, AuthSessionId, UserId from the connect-token. Absent for endpoint/MCP dispatches because there is no live client connection.HttpCallContext.Current (this) and McpCallContext .Current: the *request-scoped overlay* that exposes the per-call resolved identity for handler code to read. Set by the wrapper before the handler runs, cleared after. The point is that handlers reading "who is this call for?" get a non-empty answer on endpoint/MCP-dispatched calls, where the connection-level Context.UserId would be empty. The handler's SessionIdentity record (resolved by CellHost.ResolveByCellTypeName before this context is set) and HttpCallContext.Current.SessionIdentity carry the same information in different shapes: the former is typed and tied to the cell's lifetime; the latter is the raw wire dict tied to the call's lifetime. Headers and RawBody are the UNTRUSTED request inputs, exposed so a handler can do its own logic inline (e.g. verify a Stripe-Signature against the raw body) without a separate auth cell. They must never feed identity resolution — the target instance is already chosen from trusted sources (a signed ikon-grant / policy claims / platform-controlled path+query) before the handler runs, so reading a header cannot retarget the call.
    ctor(IReadOnlyDictionary<string, string>? SessionIdentity = null, CancellationToken CancellationToken = null, IReadOnlyDictionary<string, string>? Headers = null, string? RawBody = null)
    CancellationToken CancellationToken { get; init; }
    static HttpCallContext? Current { get; }
    IReadOnlyDictionary<string, string>? Headers { get; init; }
    string? RawBody { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentity { get; init; }
    // Convenience accessor for the conventional userid field of the request's SessionIdentity. Returns null when no HttpCallContext is current or when the identity dict has no userid key (e.g. an anonymous endpoint with no identity-bearing fields). Case-insensitive lookup — the same dict is built by the backend funnel from open `{userid}` path captures, policy claims, and a signed `ikon-grant`'s pinned identity.
    string? UserId { get; }
    // Case-insensitive lookup of a request header. UNTRUSTED request input — read it for handler logic (e.g. endpoint signature verification), NEVER to derive the SessionIdentity. Identity is resolved upstream before the handler runs and is the only thing that picks the target instance; headers cannot move it. Returns null when the header is absent. The accessor is case-insensitive because HTTP header names are, and the two dispatch paths build the header dictionary with different comparers.
    string? Header(string name)
    static IDisposable Use(HttpCallContext context)
  // Bridges in-process HTTP cell-method dispatch through the active GovernanceScope hook. With no hook active this is a pass-through; with one set, the invocation flows through RunAsync``1 with the structural {CellType}.{Method} subject id so the same Mission gates HTTP and MCP symmetrically.
  static class HttpDispatchGovernance
    static Task<object?> InvokeAsync(MethodInfo handler, Type ownerType, IReadOnlyDictionary<string, object?> args, Func<Task<object?>> invoke, CancellationToken ct = null)
  // Reflective discovery of the typed HTTP surface on a given type: every HttpMethodAttribute method. McpAttribute methods are NOT surfaced here — they are discovered separately by McpToolDiscovery and mounted by the framework both on the /{Type}/mcp multiplexer and as their own per-tool endpoints. Used at startup by the framework to enumerate the typed-HTTP surface of an app class and of every cell type.
  static class HttpEndpointDiscovery
    // Discover every typed HTTP endpoint on ownerType . Methods inherited from base classes are included; static methods and non-public methods are skipped (endpoints must be invokable on a specific instance). Requires an explicit [HttpGet]/[HttpPost].
    static IReadOnlyList<HttpEndpointInfo> ForType(Type ownerType)
    // Discover endpoints across every type in types . Convenience overload for the startup path that has already filtered an assembly's loaded types down to apps and cells.
    static IReadOnlyList<HttpEndpointInfo> ForTypes(IEnumerable<Type> types)
  // Metadata for a single HttpMethodAttribute -annotated method discovered at startup. Carries everything the dispatcher needs at request time: the HTTP method, path template, the name of the /router/ auth policy, the reflected MethodInfo , and the owner Type (an app class or a [Cell] class). Authorization itself runs at the gateway edge (the /router/ policy), not in-process — Auth is metadata carried into the manifest.
  sealed class HttpEndpointInfo : IEquatable<HttpEndpointInfo>
    // Metadata for a single HttpMethodAttribute -annotated method discovered at startup. Carries everything the dispatcher needs at request time: the HTTP method, path template, the name of the /router/ auth policy, the reflected MethodInfo , and the owner Type (an app class or a [Cell] class). Authorization itself runs at the gateway edge (the /router/ policy), not in-process — Auth is metadata carried into the manifest.
    ctor(string Method, string Path, string? Auth, MethodInfo Handler, Type OwnerType)
    string? Auth { get; init; }
    MethodInfo Handler { get; init; }
    string Method { get; init; }
    Type OwnerType { get; init; }
    string Path { get; init; }
  // Which wire protocol an HTTP-class endpoint speaks. Addressing, path templating, identity binding, auth, and abuse-control are identical across the kinds — only the handler stack (typed bind vs MCP JSON-RPC) and the schema advertised to clients differ. [Rest] maps to Rest and [Mcp] to Mcp ; both ride the same AppEndpointHost .
  enum HttpEndpointKind
    Rest
    Mcp
  // Compiled representation of a Path template. Each segment is either a literal or a {name} capture; matching is exact on segment count, ordinal on literals, case-insensitive on capture names. No wildcard / catch-all support; that's a deliberate simplification — the typed-endpoint surface is meant to be explicit.
  sealed class RouteTemplate
    // Names of every {capture} segment, in path order.
    IReadOnlyList<string> CaptureNames { get; }
    // The literal path with capture syntax preserved (e.g. spaces/{spaceId}/messages).
    string Pattern { get; }
    static RouteTemplate Parse(string template)
    // Try to match path against this template. On success, returns true and populates captures with the captured values keyed by name. On failure, returns false and captures is empty.
    bool TryMatch(string path, out IReadOnlyDictionary<string, string> captures)
  // RFC-6570 Level-1 URI template — {name} placeholders only, no list/operator modifiers. Compile once at registration time; match incoming URIs back to placeholder values. Used by McpResourceBridge to route resources/read URIs to the owning cell method.
  sealed class UriTemplate
    bool IsStatic { get; }
    IReadOnlyList<string> PlaceholderNames { get; }
    string Template { get; }
    // Match an incoming URI against the template. Returns the placeholder bindings on success, or null if the URI doesn't fit the template shape. Placeholder values are non-empty and do not cross the next literal segment.
    IReadOnlyDictionary<string, string>? Match(string uri)
    static UriTemplate Parse(string template)

namespace Ikon.App.Mcp
  sealed class CallToolParams : IEquatable<CallToolParams>
    ctor()
    JsonElement Arguments { get; init; }
    string Name { get; init; }
  sealed class CallToolResult : IEquatable<CallToolResult>
    ctor(IReadOnlyList<ToolContent> Content, bool IsError)
    IReadOnlyList<ToolContent> Content { get; init; }
    bool IsError { get; init; }
  // Params of a notifications/cancelled notification. RequestId identifies the in-flight call the client wants to abort.
  sealed class CancelledNotificationParams : IEquatable<CancelledNotificationParams>
    // Params of a notifications/cancelled notification. RequestId identifies the in-flight call the client wants to abort.
    ctor(JsonElement RequestId, string? Reason = null)
    string? Reason { get; init; }
    JsonElement RequestId { get; init; }
  // Transport-facing sink for server-initiated JSON-RPC notifications. McpHost calls this to push progress updates and similar events that aren't the response to a specific request.
  interface IMcpNotificationSink
    abstract Task SendNotificationAsync(string method, object params, CancellationToken ct)
  sealed class InitializeResult : IEquatable<InitializeResult>
    ctor(string ProtocolVersion, McpCapabilities Capabilities, McpServerInfo ServerInfo)
    McpCapabilities Capabilities { get; init; }
    string ProtocolVersion { get; init; }
    McpServerInfo ServerInfo { get; init; }
  sealed class JsonRpcError : IEquatable<JsonRpcError>
    ctor(int Code, string Message, JsonElement? Data = null)
    int Code { get; init; }
    JsonElement? Data { get; init; }
    string Message { get; init; }
  // JSON-RPC 2.0 + MCP message types. Minimal subset for an MCP server that answers initialize, tools/list, and tools/call. Reads / writes are routed through McpJson .
  sealed class JsonRpcRequest : IEquatable<JsonRpcRequest>
    ctor()
    JsonElement? Id { get; init; }
    bool IsNotification { get; }
    string JsonRpc { get; init; }
    string Method { get; init; }
    JsonElement? Params { get; init; }
  sealed class JsonRpcResponse : IEquatable<JsonRpcResponse>
    ctor()
    JsonRpcError? Error { get; init; }
    JsonElement? Id { get; init; }
    string JsonRpc { get; init; }
    object? Result { get; init; }
    static JsonRpcResponse Fail(JsonElement? id, int code, string message)
    static JsonRpcResponse Ok(JsonElement? id, object? result)
  // Builds JSON Schema objects from .NET reflection metadata (parameter lists, property bags). Used by McpToolBridge to derive an MCP tool's inputSchema from the method's parameter list. Defers per-type schema generation to JsonSchemaGenerator so MCP tools, Emerge.Run response schemas, and Ikon.AI tool definitions all speak the same dialect (currently OpenAI/Anthropic-strict 2020-12).
  static class JsonSchemaBuilder
    // Build an object-shaped JSON Schema describing the named property bag implied by a method's parameter list. Each non-optional parameter becomes a required property whose schema is derived from its type via JsonSchemaGenerator ; parameters with a default value are optional. [Description] attributes on parameters are surfaced as the property's description.
    static JsonElement BuildObjectSchema(IReadOnlyList<ParameterInfo> parameters)
    // As BuildObjectSchema but with an extra set of always-required properties prepended (used by the MCP bridge to inject a keyed cell's identity fields).
    static JsonElement BuildObjectSchema(IReadOnlyList<ParameterInfo> parameters, IReadOnlyList<ValueTuple<string, Type, string?>> extraRequired)
  sealed class ListResourceTemplatesResult : IEquatable<ListResourceTemplatesResult>
    ctor(IReadOnlyList<ResourceTemplate> ResourceTemplates)
    string? NextCursor { get; init; }
    IReadOnlyList<ResourceTemplate> ResourceTemplates { get; init; }
  sealed class ListResourcesResult : IEquatable<ListResourcesResult>
    ctor(IReadOnlyList<Resource> Resources)
    string? NextCursor { get; init; }
    IReadOnlyList<Resource> Resources { get; init; }
  sealed class ListToolsParams : IEquatable<ListToolsParams>
    ctor()
    // Opaque pagination cursor returned in a previous NextCursor . Clients pass it back verbatim to fetch the next page; first page omits it.
    string? Cursor { get; init; }
  sealed class ListToolsResult : IEquatable<ListToolsResult>
    ctor(IReadOnlyList<ToolDefinition> Tools)
    // Set when more tools remain. Clients echo this back in Cursor to get the next page. null when this is the last page.
    string? NextCursor { get; init; }
    IReadOnlyList<ToolDefinition> Tools { get; init; }
  // Per-request context for an MCP tools/call or resources/read in flight. AsyncLocal so the bridge can read it from inside parameter binding without threading another argument through every call site. Carries: The request's effective CancellationToken (linked to the transport CT and a per-request CTS the host can trip on notifications/cancelled).An optional progress sink the bridge wires IProgress`1 parameters into. SessionIdentityFields — the authenticated identity for this request (from claims merged by the transport). Bridges pass it to CellHost.ResolveByCellTypeName so keyed cells route to the right instance. Empty / null on the stdio path (single-user process).
  sealed class McpCallContext : IEquatable<McpCallContext>
    // Per-request context for an MCP tools/call or resources/read in flight. AsyncLocal so the bridge can read it from inside parameter binding without threading another argument through every call site. Carries: The request's effective CancellationToken (linked to the transport CT and a per-request CTS the host can trip on notifications/cancelled).An optional progress sink the bridge wires IProgress`1 parameters into. SessionIdentityFields — the authenticated identity for this request (from claims merged by the transport). Bridges pass it to CellHost.ResolveByCellTypeName so keyed cells route to the right instance. Empty / null on the stdio path (single-user process).
    ctor(CancellationToken CancellationToken, Func<ProgressUpdate, Task>? OnProgress, IReadOnlyDictionary<string, string>? SessionIdentityFields = null)
    CancellationToken CancellationToken { get; init; }
    static McpCallContext? Current { get; }
    Func<ProgressUpdate, Task>? OnProgress { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentityFields { get; init; }
    // Convenience accessor for the conventional userid field of the request's SessionIdentity. Returns null when no McpCallContext is current or when claims carried no userid. Mirror of UserId — same semantics across both request-scoped contexts.
    string? UserId { get; }
    static IDisposable Use(McpCallContext context)
  sealed class McpCapabilities : IEquatable<McpCapabilities>
    ctor(McpToolsCapability? Tools = null, McpResourcesCapability? Resources = null)
    McpResourcesCapability? Resources { get; init; }
    McpToolsCapability? Tools { get; init; }
  // Standard JSON-RPC error codes plus MCP additions. The MCP spec uses InvalidRequest for malformed envelopes and MethodNotFound for unknown methods.
  static class McpErrorCode
    static int GovernanceDenied
    static int GovernanceEscalated
    static int InternalError
    static int InvalidParams
    static int InvalidRequest
    static int MethodNotFound
    static int ParseError
  // MCP server core — owns a tool registry and routes JSON-RPC requests (initialize, tools/list, tools/call) to their handlers. Tool invocations are routed through Current so the same hook that governs in-process Ikon agents governs MCP-exposed tools — one mission, two transports, one audit chain.
  sealed class McpHost
    ctor(string serverName = "ikon-mcp", string serverVersion = "0.1.0", string protocolVersion = "2024-11-05")
    IReadOnlyCollection<McpResourceHandler> Resources { get; }
    McpServerInfo ServerInfo { get; }
    IReadOnlyCollection<McpToolHandler> Tools { get; }
    // Invoke a single registered tool by name with the given arguments object — the shared core behind both the JSON-RPC tools/call path and the per-tool HTTP endpoint ( HandleToolPostAsync ). Sets up the McpCallContext (identity + cancellation + optional progress) and runs the invoke through governance, so both transports gate and bind identically. Returns an error CallToolResult for an unknown tool; governance denials/escalations propagate as exceptions for the caller to map.
    Task<CallToolResult> CallToolAsync(string name, JsonElement arguments, CancellationToken ct = null, IReadOnlyDictionary<string, string>? sessionIdentityFields = null, Func<ProgressUpdate, Task>? onProgress = null)
    Task<JsonRpcResponse?> HandleRequestAsync(JsonRpcRequest request, CancellationToken ct = null, IReadOnlyDictionary<string, string>? sessionIdentityFields = null, IMcpNotificationSink? perRequestSink = null)
    McpHost RegisterResource(McpResourceHandler resource)
    McpHost RegisterTool(McpToolHandler handler)
    // Wire a transport's outbound notification sink. The host calls it to push notifications/progress events from in-flight tools. Optional — without a sink, progress emitted by handlers is silently dropped.
    void SetNotificationSink(IMcpNotificationSink sink)
  // MCP Streamable-HTTP entry point. The host (an AppEndpointHost map call or any ASP.NET WebApplication) wires HandlePostAsync at the MCP route — typically /mcp. The transport parses the JSON-RPC body, dispatches through the supplied McpHost with the caller-supplied sessionIdentityFields (so keyed cells resolve to the right per-identity instance), and writes the response back as application/json.
  static class McpHttpTransport
    static Task HandlePostAsync(HttpContext context, McpHost mcp, IReadOnlyDictionary<string, string>? sessionIdentityFields = null)
    // OAuth 2.1 Protected Resource Metadata discovery (RFC 9728). MCP clients GET /.well-known/oauth-protected-resource to discover which authorization server they should obtain tokens from before retrying a 401-rejected MCP request.
    static Task HandleProtectedResourceDiscoveryAsync(HttpContext context)
    // Invoke a single MCP tool over plain HTTP — the per-tool endpoint at /{Owner}/{Method} that sits alongside the /{Owner}/mcp multiplexer. The request body IS the tool's arguments object, bound exactly as tools/call binds it (record / named mode), so a multi-arg tool like Add(int a, int b) is callable as a direct POST {"a":1,"b":2}. Returns the tool's raw result (not the MCP content envelope): JSON when the tool returns an object/number, plain text when it returns a string. Goes through CallToolAsync so identity routing and governance are identical to the multiplexer.
    static Task HandleToolPostAsync(HttpContext context, McpHost mcp, string toolName, IReadOnlyDictionary<string, string>? sessionIdentityFields = null)
  static class McpJson
    static T Deserialize<T>(string json)
    static T DeserializeParams<T>(JsonElement? element)
    static string Serialize<T>(T value)
    static JsonSerializerOptions Options
  // Converts an McpResourceInfo (a discovered McpResourceAttribute -annotated cell method) into an McpResourceHandler that Ikon.Mcp.McpHost can register. On read, the handler matches the incoming URI against the template, binds placeholders to method parameters by name, resolves the owning cell, invokes the method, and packages the return value as ResourceContents — text for strings/JSON-serialisable types, base64 blob for byte[].
  static class McpResourceBridge
    static McpResourceHandler BuildHandler(CellHost cellHost, McpResourceInfo info)
  // Reflective discovery of McpResourceAttribute -decorated methods on cell types. Mirror of McpToolDiscovery .
  static class McpResourceDiscovery
    static IReadOnlyList<McpResourceInfo> ForType(Type ownerType)
    static IReadOnlyList<McpResourceInfo> ForTypes(IEnumerable<Type> types)
  // MCP resource handler — the bridge builds one per [McpResource] cell method. The host iterates handlers to answer resources/list + resources/templates/list and, on resources/read, picks the first handler whose TryMatch binds the incoming URI.
  sealed class McpResourceHandler : IEquatable<McpResourceHandler>
    // MCP resource handler — the bridge builds one per [McpResource] cell method. The host iterates handlers to answer resources/list + resources/templates/list and, on resources/read, picks the first handler whose TryMatch binds the incoming URI.
    ctor(string DisplayName, string Description, string MimeType, string UriTemplate, bool IsStatic, Func<string, IReadOnlyDictionary<string, string>?> TryMatch, Func<string, IReadOnlyDictionary<string, string>, CancellationToken, Task<ResourceContents>> Read)
    string Description { get; init; }
    string DisplayName { get; init; }
    bool IsStatic { get; init; }
    string MimeType { get; init; }
    Func<string, IReadOnlyDictionary<string, string>, CancellationToken, Task<ResourceContents>> Read { get; init; }
    // Stable governance subject id (e.g. "CatalogCell.GetItem"). Used as GovernanceCall.Subject on resources/read; the bridge always sets it explicitly.
    string SubjectId { get; init; }
    Func<string, IReadOnlyDictionary<string, string>?> TryMatch { get; init; }
    string UriTemplate { get; init; }
  // Discovered metadata for a single McpResourceAttribute -annotated cell method. Carries the parsed URI template + reflected MethodInfo so the bridge can match incoming reads and invoke without re-parsing per request.
  sealed class McpResourceInfo : IEquatable<McpResourceInfo>
    // Discovered metadata for a single McpResourceAttribute -annotated cell method. Carries the parsed URI template + reflected MethodInfo so the bridge can match incoming reads and invoke without re-parsing per request.
    ctor(string DisplayName, string Description, string MimeType, UriTemplate UriTemplate, MethodInfo Handler, Type OwnerCellType)
    string Description { get; init; }
    string DisplayName { get; init; }
    MethodInfo Handler { get; init; }
    // True when the URI template has no placeholders — the resource has a single concrete URI and is published in resources/list rather than resources/templates/list.
    bool IsStatic { get; }
    string MimeType { get; init; }
    Type OwnerCellType { get; init; }
    // Structural id used for governance subject + audit. Stable regardless of the MCP-wire display name.
    string SubjectId { get; }
    UriTemplate UriTemplate { get; init; }
  sealed class McpResourcesCapability : IEquatable<McpResourcesCapability>
    ctor()
  sealed class McpServerInfo : IEquatable<McpServerInfo>
    ctor(string Name, string Version)
    string Name { get; init; }
    string Version { get; init; }
  // Converts an McpToolInfo (a discovered McpAttribute -annotated cell method) into an McpToolHandler that Ikon.Mcp.McpHost can register. The handler resolves the cell instance via CellHost , deserialises method parameters from the incoming JSON-RPC arguments object, invokes the method, awaits a possible Task`1 / ValueTask`1 , and normalises the return value to a string MCP can ship as a "text" tool content. Two binding modes, picked by signature shape: Record mode — the method has exactly one parameter whose type serialises as a JSON object (a record, POCO, dictionary, or JsonElement ). The MCP inputSchema is the record's schema, derived top-level via JsonSchemaExporter . The whole arguments object is deserialised into that single parameter — no wrapper property name.Named mode — anything else (multiple parameters, or a single primitive parameter). Each parameter becomes a top-level property of the schema; at call time the bridge binds by parameter name. Authors don't write JSON schema strings — the C# signature is the schema.
  static class McpToolBridge
    static McpToolHandler BuildHandler(CellHost cellHost, McpToolInfo info)
  // Reflective discovery of McpAttribute -decorated methods on a cell type. Used at startup by the framework to enumerate the MCP-exposed surface of every registered cell type. Mirrors HttpEndpointDiscovery .
  static class McpToolDiscovery
    // Discover every McpAttribute -decorated public instance method on ownerType . Methods inherited from base classes are included; static methods and non-public methods are skipped (tools must be invokable on a specific cell instance).
    static IReadOnlyList<McpToolInfo> ForType(Type ownerType)
    // Discover tools across every type in types . Convenience overload for the startup path that has already filtered an assembly's loaded types down to cells.
    static IReadOnlyList<McpToolInfo> ForTypes(IEnumerable<Type> types)
  sealed class McpToolHandler : IEquatable<McpToolHandler>
    ctor(string Name, string Description, JsonElement InputSchema, Func<JsonElement, CancellationToken, Task<string>> Invoke)
    string Description { get; init; }
    JsonElement InputSchema { get; init; }
    Func<JsonElement, CancellationToken, Task<string>> Invoke { get; init; }
    string Name { get; init; }
    // Optional JSON schema for the tool's return value. Auto-derived from the method's return type by Ikon.App.McpToolBridge. Surfaced to MCP clients via OutputSchema .
    JsonElement? OutputSchema { get; init; }
    // Stable governance subject id, decoupled from the MCP-wire Name . When non-empty, the host uses this as GovernanceCall.Subject so missions can address the tool by a structural id (e.g. "RefundsCell.Refund") regardless of any client-facing name override. Defaults to Name .
    string SubjectId { get; init; }
  // Metadata for a single McpAttribute -annotated method discovered at startup. Carries everything the bridge needs at request time: the MCP-wire name, description, the reflected MethodInfo , and the owner cell Type .
  sealed class McpToolInfo : IEquatable<McpToolInfo>
    // Metadata for a single McpAttribute -annotated method discovered at startup. Carries everything the bridge needs at request time: the MCP-wire name, description, the reflected MethodInfo , and the owner cell Type .
    ctor(string Name, string Description, MethodInfo Handler, Type OwnerCellType)
    string Description { get; init; }
    MethodInfo Handler { get; init; }
    string Name { get; init; }
    Type OwnerCellType { get; init; }
    // Optional override for the tool's standalone HTTP endpoint path (from Path ). Empty → the path is derived from the method name. Does not affect the MCP wire Name .
    string Path { get; init; }
    // Structural identifier used for governance and audit. Stable regardless of the Name override — missions and policies always reference tools by this id.
    string SubjectId { get; }
  sealed class McpToolsCapability : IEquatable<McpToolsCapability>
    ctor()
  // Params of a notifications/progress notification. ProgressToken echoes the request id (or a client-supplied token) so clients can match progress events back to the call they kicked off.
  sealed class ProgressNotificationParams : IEquatable<ProgressNotificationParams>
    // Params of a notifications/progress notification. ProgressToken echoes the request id (or a client-supplied token) so clients can match progress events back to the call they kicked off.
    ctor(JsonElement ProgressToken, double Progress, double? Total = null, string? Message = null)
    string? Message { get; init; }
    double Progress { get; init; }
    JsonElement ProgressToken { get; init; }
    double? Total { get; init; }
  // One progress update emitted by a long-running tool. Progress is a monotonic counter; Total is optional but expected to stay constant across updates so clients can render a percentage. Message is freeform display text.
  sealed class ProgressUpdate : IEquatable<ProgressUpdate>
    // One progress update emitted by a long-running tool. Progress is a monotonic counter; Total is optional but expected to stay constant across updates so clients can render a percentage. Message is freeform display text.
    ctor(double Progress, double? Total = null, string? Message = null)
    string? Message { get; init; }
    double Progress { get; init; }
    double? Total { get; init; }
  sealed class ReadResourceParams : IEquatable<ReadResourceParams>
    ctor(string Uri)
    string Uri { get; init; }
  sealed class ReadResourceResult : IEquatable<ReadResourceResult>
    ctor(IReadOnlyList<ResourceContents> Contents)
    IReadOnlyList<ResourceContents> Contents { get; init; }
  sealed class Resource : IEquatable<Resource>
    ctor(string Uri, string Name, string? Description = null, string? MimeType = null)
    string? Description { get; init; }
    string? MimeType { get; init; }
    string Name { get; init; }
    string Uri { get; init; }
  sealed class ResourceContents : IEquatable<ResourceContents>
    ctor(string Uri, string? MimeType = null, string? Text = null, string? Blob = null)
    string? Blob { get; init; }
    string? MimeType { get; init; }
    string? Text { get; init; }
    string Uri { get; init; }
  sealed class ResourceTemplate : IEquatable<ResourceTemplate>
    ctor(string UriTemplate, string Name, string? Description = null, string? MimeType = null)
    string? Description { get; init; }
    string? MimeType { get; init; }
    string Name { get; init; }
    string UriTemplate { get; init; }
  // Newline-delimited JSON-RPC over stdin / stdout — the transport Claude Desktop and other MCP clients use to talk to local servers. One line per message; malformed input yields a JSON-RPC parse-error response (rather than killing the loop) so a flaky client can't poison the server. Also acts as the outbound IMcpNotificationSink for the host: in-flight tools that emit progress write notifications/progress lines back through the same stdout pipe. Writes are serialised on a per-transport lock so request-response and server-push don't interleave.
  sealed class StdioTransport : IMcpNotificationSink
    ctor(McpHost host, TextReader? input = null, TextWriter? output = null)
    Task RunAsync(CancellationToken ct = null)
    Task SendNotificationAsync(string method, object params, CancellationToken ct)
  sealed class ToolContent : IEquatable<ToolContent>
    ctor(string Type, string Text)
    string Text { get; init; }
    string Type { get; init; }
  sealed class ToolDefinition : IEquatable<ToolDefinition>
    ctor(string Name, string Description, JsonElement InputSchema)
    string Description { get; init; }
    JsonElement InputSchema { get; init; }
    string Name { get; init; }
    // Optional JSON schema for the tool's return value. Derived from the method's return type (after Task/ValueTask unwrap) by Ikon.App.McpToolBridge; authors never specify it directly. Helps MCP clients validate / type-check what they get back.
    JsonElement? OutputSchema { get; init; }

namespace Ikon.App.Payments
  // A single payment record (a one-off charge or a subscription renewal).
  sealed class Payment : IEquatable<Payment>
    // A single payment record (a one-off charge or a subscription renewal).
    ctor(string Id, PaymentProvider? Provider, string Status, string? Kind, long AmountMinor, string Currency, long AmountRefundedMinor, DateTimeOffset? CreatedAt)
    long AmountMinor { get; init; }
    long AmountRefundedMinor { get; init; }
    DateTimeOffset? CreatedAt { get; init; }
    string Currency { get; init; }
    string Id { get; init; }
    string? Kind { get; init; }
    PaymentProvider? Provider { get; init; }
    string Status { get; init; }
  // "Does this customer have access to this offer" snapshot. The [PaymentsRequireSubscription] policy gates on it.
  sealed class PaymentEntitlement : IEquatable<PaymentEntitlement>
    // "Does this customer have access to this offer" snapshot. The [PaymentsRequireSubscription] policy gates on it.
    ctor(string OfferId, bool SubscriptionActive, DateTimeOffset? SubscriptionEndsAt, string? SubscriptionStatus)
    string OfferId { get; init; }
    bool SubscriptionActive { get; init; }
    DateTimeOffset? SubscriptionEndsAt { get; init; }
    string? SubscriptionStatus { get; init; }
  // A normalized payment event the backend pushes to the app.
  sealed class PaymentEvent : IEquatable<PaymentEvent>
    // A normalized payment event the backend pushes to the app.
    ctor(string EventId, PaymentProvider? Provider, PaymentEventType? Type, DateTimeOffset? OccurredAt, long Sequence, string PayloadJson)
    string EventId { get; init; }
    DateTimeOffset? OccurredAt { get; init; }
    string PayloadJson { get; init; }
    PaymentProvider? Provider { get; init; }
    long Sequence { get; init; }
    PaymentEventType? Type { get; init; }
    // The normalized projection as a JSON element.
    JsonElement Payload()
  // The kind of a normalized PaymentEvent .
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
  // A provider-hosted page the customer is redirected to in order to pay. Send them to Url .
  sealed class PaymentLink : IEquatable<PaymentLink>
    // A provider-hosted page the customer is redirected to in order to pay. Send them to Url .
    ctor(string Url, string Reference, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    string Reference { get; init; }
    string Url { get; init; }
  // A purchasable offer in the app's catalog — recurring (subscription) or one-time, per its prices.
  sealed class PaymentOffer : IEquatable<PaymentOffer>
    // A purchasable offer in the app's catalog — recurring (subscription) or one-time, per its prices.
    ctor(string OfferId, string Name, IReadOnlyList<PaymentPrice> Prices)
    string Name { get; init; }
    string OfferId { get; init; }
    IReadOnlyList<PaymentPrice> Prices { get; init; }
  // One price on an offer. Type is recurring or one_time.
  sealed class PaymentPrice : IEquatable<PaymentPrice>
    // One price on an offer. Type is recurring or one_time.
    ctor(long AmountMinor, string Currency, string Type, string? Interval, int? IntervalCount)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    string? Interval { get; init; }
    int? IntervalCount { get; init; }
    string Type { get; init; }
  // The payment provider that moves the money. An app picks a default provider on DefaultProvider and may override it per call.
  enum PaymentProvider
    Stripe
    Mollie
    Surfboard
  // Result of a refund.
  sealed class PaymentRefund : IEquatable<PaymentRefund>
    // Result of a refund.
    ctor(string Reference, string Status)
    string Reference { get; init; }
    string Status { get; init; }
  // A customer's live subscription, created by paying for a recurring offer.
  sealed class PaymentSubscription : IEquatable<PaymentSubscription>
    // A customer's live subscription, created by paying for a recurring offer.
    ctor(string Id, PaymentProvider? Provider, string Status, string? OfferId, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd)
    bool CancelAtPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    string Id { get; init; }
    string? OfferId { get; init; }
    PaymentProvider? Provider { get; init; }
    string Status { get; init; }
  // Declares the function requires the current customer to hold an active subscription for offerId . Resolves the customer from UserId and reads the entitlement from Instance . On missing entitlement it DENIES with a stable code (payments_subscription_required); the app's UI catches it and opens a payment link via CreatePaymentLinkAsync . The provider webhook then flips the entitlement and the user retries.
  sealed class PaymentsRequireSubscriptionAttribute : PolicyAttribute
    ctor(string offerId)
    // Offer the subscription is keyed to.
    string OfferId { get; }
    override IFunctionPolicy CreatePolicy()
  // App-level entry point for payments, reached via app.Payments. The app picks a default PaymentProvider , creates payment links (for an offer or an ad-hoc amount), and reacts to PaymentEventReceived events. Every command accepts an optional per-call provider override. The app holds no payment state. One instance per app (an AsyncLocalInstance`1 singleton).
  sealed class PaymentsService : AsyncLocalInstance<PaymentsService>
    ctor()
    // Default cancel URL used when a command does not specify one.
    string? DefaultCancelUrl { get; set; }
    // The provider used when a command does not specify one.
    PaymentProvider DefaultProvider { get; set; }
    // Default success URL used when a command does not specify one.
    string? DefaultSuccessUrl { get; set; }
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = null)
    // Create a provider-hosted payment link for an offer. Recurring offers start a subscription.
    Task<PaymentLink> CreatePaymentLinkAsync(string offerId, string appCustomerKey, string? email = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = null)
    // Create a provider-hosted payment link for an ad-hoc amount (tip, one-off charge).
    Task<PaymentLink> CreatePaymentLinkAsync(long amountMinor, string currency, string appCustomerKey, string? description = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = null)
    // The customer's access snapshot for an offer. Used by the [PaymentsRequireSubscription] policies.
    Task<PaymentEntitlement> GetEntitlementAsync(string offerId, string appCustomerKey, CancellationToken cancellationToken = null)
    // The app's catalog of purchasable offers.
    Task<IReadOnlyList<PaymentOffer>> ListOffersAsync(CancellationToken cancellationToken = null)
    Task<IReadOnlyList<Payment>> ListPaymentsAsync(string appCustomerKey, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<PaymentSubscription>> ListSubscriptionsAsync(string appCustomerKey, CancellationToken cancellationToken = null)
    Task<PaymentRefund> RefundAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = null)
    // Raised for each normalized payment event the backend pushes (paid, refunded, subscription renewed/canceled). Subscribing registers the receiver on first use.
    event Func<PaymentEvent, Task>? PaymentEventReceived
