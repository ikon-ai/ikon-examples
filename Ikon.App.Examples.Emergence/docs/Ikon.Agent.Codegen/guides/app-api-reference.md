# App API Reference

## App API Reference

Full API reference for Ikon.App and Ikon.Common.

---

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
    // The public URL for this endpoint. Available after StartAsync completes.
    string PublicUrl { get; }
    // Stops the host, releases the relay tunnel, and releases all resources.
    ValueTask DisposeAsync()
    // Registers a handler for HTTP GET requests matching the specified route pattern.
    void MapGet(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for HTTP POST requests matching the specified route pattern.
    void MapPost(string pattern, Func<HttpContext, Task> handler)
    // Registers a handler for WebSocket connections matching the specified route pattern. The socket is automatically closed and disposed after the handler completes.
    void MapWebSocket(string pattern, Func<HttpContext, WebSocket, Task> handler)
    // Allocates the relay tunnel, starts Kestrel with the registered routes, and returns immediately while the host continues to run in the background.
    Task StartAsync(CancellationToken cancellationToken = null)
    // Stops the endpoint host gracefully. Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = null)
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own Schema/*.tp files (compiled by ikon app teleport build); each carries its own GROUP_APP_LOCAL opcode and is sent/received as a native type — no JSON marshalling. Delivery is server-controlled and explicit: SendMessageAsync``1 always takes the recipient client session IDs — there is no implicit broadcast to every client. Whether a type travels reliably or unreliably is declared on the .tp schema (unreliable = true), not here.
  static class AppMessaging
    static IDisposable OnMessage<T>(IProtocolMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    static ValueTask SendMessageAsync<T>(IProtocolMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    static ValueTask SendMessageAsync<T>(IProtocolMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
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
  // Return value of an authentication / pre-filter cell. Reject non-null short-circuits the request with that response. Reject null means "pass" — the dispatcher proceeds to the target endpoint, merging any Claims into the target's SessionIdentity with highest priority (claims beat route captures and query params; they're cell-validated, the URL is not).
  sealed class AuthOutcome : IEquatable<AuthOutcome>
    // Return value of an authentication / pre-filter cell. Reject non-null short-circuits the request with that response. Reject null means "pass" — the dispatcher proceeds to the target endpoint, merging any Claims into the target's SessionIdentity with highest priority (claims beat route captures and query params; they're cell-validated, the URL is not).
    ctor(HttpResult? Reject, IReadOnlyDictionary<string, string>? Claims = null)
    IReadOnlyDictionary<string, string>? Claims { get; init; }
    HttpResult? Reject { get; init; }
    static AuthOutcome Pass(IReadOnlyDictionary<string, string>? claims = null)
    static AuthOutcome RejectWith(HttpResult result)
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
  // Manages client profiles for an AI app. Automatically loads profiles when clients join and provides sync access to cached profile data.
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
    TAttributes GetAttributes<TAttributes>(Context clientContext) where TAttributes : IProfileAttributes, new()
    // Get profile for a connected client. Returns cached profile (guaranteed available after client joined).
    ClientProfile GetProfile(Context clientContext)
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
    // Try to get profile from cache. Returns null if not loaded.
    ClientProfile? TryGetProfile(Context clientContext)
    // Try to get profile from cache by userId. Returns null if not loaded.
    ClientProfile? TryGetProfile(string userId)
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
  // Marks a method on an app or cell as a typed HTTP endpoint. The framework discovers these at startup, registers a route on the platform's HTTP host, dispatches inbound requests through the configured auth cell, materializes a typed SessionIdentity record + body, invokes the method, and serializes the return value to the response.
  sealed class HttpEndpointAttribute : Attribute
    // Marks a method on an app or cell as a typed HTTP endpoint. The framework discovers these at startup, registers a route on the platform's HTTP host, dispatches inbound requests through the configured auth cell, materializes a typed SessionIdentity record + body, invokes the method, and serializes the return value to the response.
    ctor(string method, string path)
    // When true, the path mounts at the host root regardless of the app's configured prefix. Use for OAuth callbacks and well-known URIs (/.well-known/openid-configuration etc.).
    bool Absolute { get; init; }
    // Pre-filter cell type. The dispatcher resolves this cell, finds its method whose signature is (HttpRequest) → Task<AuthOutcome>, and runs it before this endpoint's handler. A non-null AuthOutcome.Reject short-circuits the request; otherwise AuthOutcome.Claims merge into the target SessionIdentity. No interface, no marker — just a cell with a method shape the dispatcher recognizes (validated at startup).
    Type? Auth { get; init; }
    // HTTP verb (GET / POST / PUT / DELETE / PATCH).
    string Method { get; }
    // Path relative to the app's mount point. Route params use {name} syntax.
    string Path { get; }
  // Serializable view of an inbound HTTP request — what an authentication or other pre-filter cell inspects. The dispatcher constructs one of these per inbound request and passes it to whichever pre-filter the endpoint declares ( Auth ).
  sealed class HttpRequest : IEquatable<HttpRequest>
    // Serializable view of an inbound HTTP request — what an authentication or other pre-filter cell inspects. The dispatcher constructs one of these per inbound request and passes it to whichever pre-filter the endpoint declares ( Auth ).
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  // Typed return value from an HttpEndpointAttribute -annotated method. Endpoints can return any serializable type for an automatic 200 + JSON response, or return an HttpResult when they need control over status code, content type, or custom body serialization.
  sealed class HttpResult : IEquatable<HttpResult>
    // Typed return value from an HttpEndpointAttribute -annotated method. Endpoints can return any serializable type for an automatic 200 + JSON response, or return an HttpResult when they need control over status code, content type, or custom body serialization.
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
  interface IAppBase : IProtocolMessageChannel
    // Gets the background work tracker that prevents server idle shutdown while work is in progress.
    BackgroundWork BackgroundWork { get; }
    // Gets the path to the Data directory for this app. Files placed in the Data folder of the app project can be accessed at runtime using this path. Note: in cloud, this directory is read-only and writing to it will throw an exception.
    string DataDirectory { get; }
    // Gets the database connection configurations for this app instance.
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // Gets the email service for this app — sending custom emails through the platform mailer and reading inbound emails delivered to this app's space. Requires the Email feature to be enabled on the app's organisation/space; calls against a non-entitled space throw FeatureNotEnabledException .
    EmailService Email { get; }
    // Gets the platform-wide shared state from the server containing clients, streams, and space/channel info.
    GlobalState GlobalState { get; }
    // Gets the configured maximum memory limit in megabytes for this server instance.
    int MaxMemoryLimitMb { get; }
    // The Parallax mounts this app renders. Each mount produces an independent UI stream addressable from a host UI as <ParallaxView mount="..." />. Defaults to a single mount named "ikon-ui" — the wire-identical shape of every Ikon app today. Apps with multiple panels or mixed Parallax/external regions can replace the value with a longer list at any time; the render loop reacts and emits UIStreamBegin/UIStreamEnd for additions and removals.
    Reactive<IReadOnlyList<string>> Mounts { get; }
    // Gets the navigation helper for managing URL paths and listening to URL changes.
    Navigation Navigation { get; }
    // Gets the reactive wrapper around GlobalState that provides change notifications.
    ReactiveGlobalState ReactiveGlobalState { get; }
    // Gets the reactive root that manages per-client reactive graphs and update cycles.
    ReactiveRoot ReactiveRoot { get; }
    // Gets the secrets (tokens, API keys, passwords) configured for this app. Values are fetched from the Ikon backend once at app startup and exposed synchronously; changes made via ikon app secret set while the app is running only take effect after a restart.
    Secrets Secrets { get; }
    // Gets the webhook function info for this app instance, including ready-to-use public URLs with the current session identity values prefilled as query parameters. The list is built once before Main() runs and is then refreshed automatically whenever a webhook-marked function is registered with the FunctionRegistry.
    IReadOnlyList<WebhookInfo> Webhooks { get; }
    // Creates a platform-managed eID-backed PAdES signature order for the supplied document(s). The platform navigates the signer's browser to the signing-ceremony URL through the existing client UI surface, awaits the asynchronous packaging completion, and resolves the returned task with the signed PDF and evidence metadata. The returned bytes are the long-term-validation PAdES PDF when the chosen scheme produces it; apps should persist them as the system of record because the platform's session retention is short.
    abstract Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = null)
    // Dynamically requests a raw TCP/TLS/UDP endpoint. Returns a RelayEndpoint whose LocalPort a listener should bind to; the endpoint is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the returned endpoint to release it. For HTTP/HTTPS endpoints use AppEndpointHost .
    abstract Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", CancellationToken ct = null)
    // Requests a fresh strong-authentication step-up challenge for the current user. Navigates the client browser to the platform's configured identity provider through the existing client UI surface, waits for the user to complete the challenge, and returns the platform-signed step-up assertion JWT. Apps must verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier .
    abstract Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = null)
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
  // Legacy app host interface providing access to app configuration (appVersion=1).
  interface IApp<TConfig> : IAppBase, IProtocolMessageChannel
    // Gets the app configuration provided by the developer.
    TConfig Config { get; }
  // App host interface providing typed session identity and client parameters.
  interface IApp<TSessionIdentity, TClientParameters> : IAppBase, IProtocolMessageChannel
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
  // Marker interface for custom profile attribute classes. Implement this interface on classes that define custom profile attributes.
  interface IProfileAttributes
  // Event arguments for the MessageReceivedAsync event.
  class MessageReceivedEventArgs : EventArgs
    // Event arguments for the MessageReceivedAsync event.
    ctor(ProtocolMessage message)
    // Gets the received protocol message.
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
  // Information about a webhook function exposed by the app. Returned by Webhooks for developer convenience.
  sealed class WebhookInfo
    ctor()
    // The cell type (or empty string for app-shell-hosted webhooks). When non-empty, the webhook is routed to a specific cell instance keyed by the SessionIdentity values in the URL query. Empty preserves the legacy app-shell dispatch path.
    string CellType { get; set; }
    // The function name as registered in the FunctionRegistry. Either the explicit FunctionAttribute.Name override or the auto-generated TypeFullName.MethodName. This is also the value used as the path segment in PublicUrl .
    string FunctionName { get; set; }
    // The full public URL for this webhook with session identity values prefilled as query parameters. In cloud mode this is a /ikon/webhook/{functionName} URL (legacy) or /ikon/webhook/{cellType}/{functionName} (when CellType is set). In local dev this is a /webhook/{...} URL on the server's public tunnel.
    string PublicUrl { get; set; }
  // A thin wrapper that holds the user’s configuration. This wrapper derives from BasePluginConfig so that it can be used internally by BasePlugin. Plugin developers only see the wrapped TConfig.
  class WrapperConfig<TConfig> : BasePluginConfig
    ctor()
    // A thin wrapper that holds the user’s configuration. This wrapper derives from BasePluginConfig so that it can be used internally by BasePlugin. Plugin developers only see the wrapped TConfig.
    ctor(TConfig userConfig)
    TConfig AppConfig { get; set; }

namespace Ikon.App.Auth
  // Pre-filter cell that always passes with no claims. Use for genuinely public endpoints (health probes, well-known URIs, public webhooks where the body is the only credential).
  sealed class AnonymousAuth
    ctor(ICell<AnonymousAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  // Pre-filter cell that validates an X-Api-Key header against a configured allowlist. Use for server-to-server callers (third-party backends, internal job runners) that don't have a user session to present a bearer token from. Keys are read from IKON_API_KEYS as a comma-separated list; equality is constant-time to defend against timing oracles.
  sealed class ApiKeyAuth
    ctor(ICell<ApiKeyAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  // Pre-filter cell for cloud-internal traffic — calls coming through the Ikon backend's signed AuthTicket header. Uses the same validation primitive as the existing webhook flow, surfaced as a typed pre-filter so any HttpEndpointAttribute can opt in.
  sealed class AuthTicketAuth
    ctor(ICell<AuthTicketAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  // Pre-filter cell that trusts an upstream edge function (Cloudflare Workers, Vercel Edge, Lambda@Edge, etc.) which has already authenticated the user and signed an X-Ikon-Identity header with a shared HMAC secret. Forms half of the polyglot-auth story: the actual auth logic can run in any language at the edge; this cell just verifies the signature and surfaces the claims to the framework.
  sealed class EdgeTrustedHeaderAuth
    ctor(ICell<EdgeTrustedHeaderAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)
  // Helper for the deferred-login flow. Prompts a specific client to show its login UI via the ikon.client.loginShow server→client RPC (see ClientFunctions ).
  static class LoginPrompt
    // Prompt a specific client to show its login UI.
    static Task ShowAsync(int targetClientSessionId, string? reason = null)
    // Prompt the current client (must be inside a ClientScope ) to show its login UI.
    static Task ShowAsync(string? reason = null)
    // The ConnectToken.Parameters key under which the client runtime stashes the deferred-login handoff envelope (JSON-encoded { appRoute, pendingCall }) when it reprovisions after a successful login.
    static string HandoffParameterKey
  // Pre-filter cell that validates an OAuth 2.1 bearer JWT issued by an external IdP (Auth0, Okta, Cognito, etc.). The cell acts as a resource server only — it validates tokens the IdP signed; it does not issue, refresh, or hand-shake. Use as [HttpEndpoint(Auth = typeof(OAuthAuth))] on the MCP HTTP route.
  sealed class OAuthAuth
    ctor(ICell<OAuthAuth.SessionIdentity> ctx)
    // Configured issuer URL — returned by the protected-resource discovery document so MCP clients know which authorization server they should obtain tokens from.
    static string? ConfiguredIssuer { get; }
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
  // Pre-filter cell that validates a bearer JWT signed with HS256 against a shared secret. The plain stateless pattern from the unified-contact-surface plan: the issuer (your auth backend / token-issuance app) signs the JWT with the same secret; this cell verifies the signature, honors the exp claim, and passes the rest of the payload through as claims.
  sealed class SessionTokenAuth
    ctor(ICell<SessionTokenAuth.SessionIdentity> ctx)
    Task<AuthOutcome> Authenticate(HttpRequest request)

namespace Ikon.App.Billing
  sealed class AssetBillingConnectAccountStore : IBillingConnectAccountStore
    ctor(string assetPath = "billing/connect-account-id.json")
    Task ClearAsync(CancellationToken cancellationToken = null)
    Task<string?> GetAsync(CancellationToken cancellationToken = null)
    Task SetAsync(string connectedAccountId, CancellationToken cancellationToken = null)
  // Result of CreateAccountSessionAsync .
  sealed class BillingAccountSession : IEquatable<BillingAccountSession>
    // Result of CreateAccountSessionAsync .
    ctor(string ClientSecret, DateTimeOffset ExpiresAt)
    string ClientSecret { get; init; }
    DateTimeOffset ExpiresAt { get; init; }
  // Request shape for CreateAccountSessionAsync . Toggle the components your app needs; Stripe rejects sessions with no enabled components.
  sealed class BillingAccountSessionRequest : IEquatable<BillingAccountSessionRequest>
    // Mount account management (update bank, business details, KYC).
    bool AccountManagement { get; init; }
    // Mount Stripe-hosted account onboarding inline.
    bool AccountOnboarding { get; init; }
    // Balance overview (available / pending).
    bool Balances { get; init; }
    // Connected account id (acct_...) the session is scoped to.
    string ConnectedAccountId { get; init; }
    // Disable the "Manage at Stripe" link inside account management. Useful when you embed everything inline.
    bool DisableStripeUserAuth { get; init; }
    // Tax documents (1099 in US, similar elsewhere).
    bool Documents { get; init; }
    // Allow account holder to add/edit external bank accounts. Default true.
    bool ExternalAccountCollection { get; init; }
    // Stripe-issued action items (verify ID, etc.).
    bool NotificationBanner { get; init; }
    // Payments list with refund / dispute / capture features enabled.
    bool Payments { get; init; }
    // Allow capturing manual-capture payments inside the embedded payments component.
    bool PaymentsCapturePayments { get; init; }
    // Allow dispute response inside the embedded payments component.
    bool PaymentsDisputeManagement { get; init; }
    // Allow refund actions inside the embedded payments component.
    bool PaymentsRefundManagement { get; init; }
    // Payouts list + schedule editor.
    bool Payouts { get; init; }
    // Allow the holder to edit the payout schedule inline.
    bool PayoutsEditPayoutSchedule { get; init; }
    // Show standard payouts in the embedded payouts component.
    bool PayoutsStandardPayouts { get; init; }
  static class BillingAppHelpers
    static BillingOptions AutoDetectFromApp(IAppBase app, string defaultAppId = "app")
    static string? GetSecretOrEnv(IAppBase app, string key)
  // Idempotent provisioning of a Stripe product+price catalog from an app-defined plan list. Apps declare plans in code (or config); this service makes sure each plan has a matching Stripe product + price, reusing existing rows by name and exact (amount, currency, interval) match. Returns a BillingPlanCatalogMap mapping app-side plan ids to Stripe price ids that adapters use in GetPlanAsync . Run once at app startup (it's network-bound but idempotent and short), or persist the map after first sync to skip the API hop on warm boots. Stripe is the source of truth for the price ids — they differ per account, so the map must be re-resolved per environment.
  sealed class BillingCatalogSync
    ctor(BillingService billing)
    Task<BillingPlanCatalogMap> SyncAsync(IReadOnlyList<BillingPlanSpec> plans, CancellationToken cancellationToken = null)
    // Ensure each plans entry has a matching Stripe product + price. Returns a map from app plan id to Stripe price id. Matching strategy: 1. Find an existing product whose Name matches ProductName . 2. If absent, create one (with Description and metadata.app_plan_id set). 3. Find an existing price under that product whose UnitAmountMinor, Currency, and RecurringInterval all match. 4. If absent, create one (Stripe prices are immutable, so changing a plan's price creates a new price; existing subscribers stay on the old one).
    Task<BillingPlanCatalogMap> SyncFromCatalogClassAsync(Type catalogClass, CancellationToken cancellationToken = null)
  // Slim view of a Stripe charge record. Returned by ListChargesAsync .
  sealed class BillingCharge : IEquatable<BillingCharge>
    // Slim view of a Stripe charge record. Returned by ListChargesAsync .
    ctor(string Id, string? PaymentIntentId, string? CustomerId, long AmountMinor, long AmountRefundedMinor, string Currency, string Status, bool Paid, bool Refunded, DateTimeOffset Created, string? Description, string? ReceiptUrl)
    // Charged amount in minor units.
    long AmountMinor { get; init; }
    // Refunded amount in minor units.
    long AmountRefundedMinor { get; init; }
    // When Stripe created the charge.
    DateTimeOffset Created { get; init; }
    // ISO 4217 currency code in lowercase.
    string Currency { get; init; }
    // Customer id, when present.
    string? CustomerId { get; init; }
    // Free-form description on the charge.
    string? Description { get; init; }
    // Stripe charge id (ch_...).
    string Id { get; init; }
    // True when the charge has been collected.
    bool Paid { get; init; }
    // Payment intent id, when present.
    string? PaymentIntentId { get; init; }
    // URL to the hosted receipt, when available.
    string? ReceiptUrl { get; init; }
    // True when the charge is fully refunded.
    bool Refunded { get; init; }
    // succeeded, pending, or failed.
    string Status { get; init; }
  // Declares the function deducts credits from the current customer's wallet for sku . Requires CreditStore wired on the ambient instance. Deduction happens inside the policy via DeductAsync with an idempotency key composed of the function name + caller id, so the same call evaluated twice (e.g. interrupted then retried) charges only once. Deny code: billing_credits_insufficient.
  sealed class BillingChargeCreditsAttribute : PolicyAttribute
    ctor(string sku, int credits = 1)
    int Credits { get; }
    string Sku { get; }
    override IFunctionPolicy CreatePolicy()
  // Result of OfferCheckoutAsync . Either the customer already holds the entitlement (no checkout needed — show the app's post-purchase UX directly) or a fresh Stripe Checkout session was minted and the app should redirect.
  sealed class BillingCheckoutOffer : IEquatable<BillingCheckoutOffer>
    // Result of OfferCheckoutAsync . Either the customer already holds the entitlement (no checkout needed — show the app's post-purchase UX directly) or a fresh Stripe Checkout session was minted and the app should redirect.
    ctor(bool AlreadyEntitled, string? SessionId, string? Url)
    // True when the customer already had an active subscription / unlock for the plan and no Stripe call was made.
    bool AlreadyEntitled { get; init; }
    // Stripe Checkout session id (only when AlreadyEntitled is false).
    string? SessionId { get; init; }
    // Stripe hosted-checkout URL (only when AlreadyEntitled is false). App passes to ClientFunctions.SetUrlAsync.
    string? Url { get; init; }
  // Result of creating a Stripe Checkout session. Apps redirect the user to Url .
  sealed class BillingCheckoutResult : IEquatable<BillingCheckoutResult>
    // Result of creating a Stripe Checkout session. Apps redirect the user to Url .
    ctor(string SessionId, string Url)
    string SessionId { get; init; }
    string Url { get; init; }
  // Result of RetrieveAccountAsync . Use ChargesEnabled + PayoutsEnabled as the gate for unlocking billing flows in your app.
  sealed class BillingConnectAccount : IEquatable<BillingConnectAccount>
    // Result of RetrieveAccountAsync . Use ChargesEnabled + PayoutsEnabled as the gate for unlocking billing flows in your app.
    ctor(string Id, bool DetailsSubmitted, bool ChargesEnabled, bool PayoutsEnabled, IReadOnlyList<string> RequirementsCurrentlyDue, IReadOnlyList<string> RequirementsEventuallyDue, string? RequirementsDisabledReason, string? Country = null)
    bool ChargesEnabled { get; init; }
    string? Country { get; init; }
    bool DetailsSubmitted { get; init; }
    string Id { get; init; }
    bool PayoutsEnabled { get; init; }
    IReadOnlyList<string> RequirementsCurrentlyDue { get; init; }
    string? RequirementsDisabledReason { get; init; }
    IReadOnlyList<string> RequirementsEventuallyDue { get; init; }
  sealed class BillingConnectFunctionHost
    ctor(BillingConnectService connect, Func<string?> connectedAccountIdGetter, Func<BillingConnectAccount, Task>? onStatusRefresh = null)
    Task<string> FetchConnectManagementSecretAsync()
    Task<string> FetchConnectOnboardingSecretAsync()
    Task OnConnectOnboardingExitAsync()
  // Stripe Connect helpers for marketplace apps where end-creators receive a share of payments. Apps create connected accounts, send creators through Stripe's hosted onboarding flow, then route checkouts to those accounts via BillingDestination on CreateCheckoutAsync or CreateCartCheckoutAsync .
  sealed class BillingConnectService
    ctor(BillingOptions options)
    // Most recently constructed BillingConnectService instance observable from the current execution flow. Same ambient-lookup pattern as Current — see that property for the full AsyncLocal`1 rationale.
    static BillingConnectService Current { get; }
    // Create an Account Session for embedded Connect components. The returned ClientSecret is short-lived (expires at ExpiresAt ) and is handed to Stripe's loadConnectAndInitialize on the frontend so embedded components (account onboarding, account management, payouts, balances, etc.) can mount inline in your app instead of redirecting to Stripe.
    Task<BillingAccountSession> CreateAccountSessionAsync(BillingAccountSessionRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a platform webhook endpoint that receives events from every connected account (one endpoint serves all). Use this instead of CreateWebhookEndpointAsync for the platform-managed Connect mode.
    Task<BillingWebhookEndpoint> CreateConnectWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe Connect express account for a creator. Express is the recommended type for marketplace platforms — Stripe hosts the dashboard and KYC.
    Task<string> CreateExpressAccountAsync(string email, string country, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, IEnumerable<string>? requestedCapabilities = null, CancellationToken cancellationToken = null)
    // Create a login link to the connected account's Stripe Express Dashboard. Only works after onboarding completed.
    Task<string> CreateLoginLinkAsync(string connectedAccountId, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create an account link to send the creator through Stripe's onboarding flow. The returned URL is single-use and short-lived.
    Task<string> CreateOnboardingLinkAsync(string connectedAccountId, string refreshUrl, string returnUrl, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Retrieve a connected account to inspect onboarding and capability status. After Embedded Onboarding's onExit fires, call this to confirm ChargesEnabled / PayoutsEnabled before unlocking billing flows in your app.
    Task<BillingConnectAccount> RetrieveAccountAsync(string connectedAccountId, CancellationToken cancellationToken = null)
    // Transfer funds from the platform balance to a connected account.
    Task<string> TransferAsync(string connectedAccountId, long amountMinor, string currency, string idempotencyKey, CancellationToken cancellationToken = null)
  enum BillingCouponDuration
    Once
    Forever
    Repeating
  // Coupon definition for CreateCouponAsync . Set exactly one of PercentOff or AmountOffMinor .
  sealed class BillingCouponInfo : IEquatable<BillingCouponInfo>
    ctor()
    long? AmountOffMinor { get; init; }
    string? Currency { get; init; }
    BillingCouponDuration Duration { get; init; }
    int? DurationInMonths { get; init; }
    string? Id { get; init; }
    int? MaxRedemptions { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? Name { get; init; }
    decimal? PercentOff { get; init; }
    DateTimeOffset? RedeemBy { get; init; }
  // Result of issuing a credit note.
  sealed class BillingCreditNote : IEquatable<BillingCreditNote>
    // Result of issuing a credit note.
    ctor(string Id, string Number, string Status, long AmountMinor, string? PdfUrl)
    // Total of the credit note.
    long AmountMinor { get; init; }
    // Credit note id (cn_...).
    string Id { get; init; }
    // Human-readable credit note number.
    string Number { get; init; }
    // URL of the generated PDF, when present.
    string? PdfUrl { get; init; }
    // issued or void.
    string Status { get; init; }
  // Inputs for CreateCreditNoteAsync . A credit note is the formal way to issue a partial refund or credit against a finalized Stripe invoice — Stripe handles the tax adjustment and regenerates the invoice PDF, which a plain Refund does not.
  sealed class BillingCreditNoteInfo : IEquatable<BillingCreditNoteInfo>
    // Amount of the credit note in minor units. Defaults to a full credit.
    long? AmountMinor { get; init; }
    // Amount to credit to the customer's balance, in minor units.
    long? CreditAmountMinor { get; init; }
    string InvoiceId { get; init; }
    string? Memo { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? Reason { get; init; }
    // Amount to refund to the original payment method, in minor units. Null = no out-of-pocket refund (credit only).
    long? RefundAmountMinor { get; init; }
  // Subset of Stripe customer fields the library reads or writes. Apps build one of these to call CreateCustomerAsync or UpdateCustomerAsync .
  sealed class BillingCustomerInfo : IEquatable<BillingCustomerInfo>
    ctor()
    string? AddressCity { get; init; }
    string? AddressCountry { get; init; }
    string? AddressLine1 { get; init; }
    string? AddressLine2 { get; init; }
    string? AddressPostalCode { get; init; }
    string? AddressState { get; init; }
    string? Description { get; init; }
    string? Email { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? Name { get; init; }
    string? Phone { get; init; }
    string? PreferredLocales { get; init; }
    // Stripe Tax exemption status. None = standard (default), Exempt = no tax charged, Reverse = EU B2B reverse-charge (customer self-accounts for VAT). Required for B2B SaaS in EU when the buyer carries a valid VAT id.
    BillingTaxExempt? TaxExempt { get; init; }
  // Marketplace / Stripe Connect destination for a charge. Use to route a checkout payment to a connected account while the platform takes an application fee.
  sealed class BillingDestination : IEquatable<BillingDestination>
    ctor(string ConnectedAccountId, long? ApplicationFeeAmountMinor = null, decimal? ApplicationFeePercent = null)
    long? ApplicationFeeAmountMinor { get; init; }
    decimal? ApplicationFeePercent { get; init; }
    string ConnectedAccountId { get; init; }
  // Result of creating an embedded Stripe Checkout session. Pass ClientSecret to Stripe.js / the embedded React component on the frontend to mount the checkout in-app.
  sealed class BillingEmbeddedCheckout : IEquatable<BillingEmbeddedCheckout>
    // Result of creating an embedded Stripe Checkout session. Pass ClientSecret to Stripe.js / the embedded React component on the frontend to mount the checkout in-app.
    ctor(string SessionId, string ClientSecret)
    // Client secret used by the frontend embed.
    string ClientSecret { get; init; }
    // Checkout session id (cs_...).
    string SessionId { get; init; }
  // One-stop "does this customer have access to this plan" snapshot. Composed by GetEntitlementAsync from Stripe subscription state, customer metadata, and an optional app-side credit store. Apps read this single record instead of orchestrating three Stripe roundtrips themselves.
  sealed class BillingEntitlement : IEquatable<BillingEntitlement>
    // One-stop "does this customer have access to this plan" snapshot. Composed by GetEntitlementAsync from Stripe subscription state, customer metadata, and an optional app-side credit store. Apps read this single record instead of orchestrating three Stripe roundtrips themselves.
    ctor(string PlanId, bool SubscriptionActive, DateTimeOffset? SubscriptionEndsAt, bool CancelAtPeriodEnd, string? SubscriptionStatus, bool UnlockGranted, DateTimeOffset? UnlockGrantedAt, int CreditsRemaining, DateTimeOffset? LastPurchaseAt)
    // True when the subscription is scheduled to cancel at SubscriptionEndsAt .
    bool CancelAtPeriodEnd { get; init; }
    // Wallet balance for credit-based products. Populated only when an IBillingCreditStore is supplied; otherwise 0.
    int CreditsRemaining { get; init; }
    // Customer-metadata-stamped last-purchase timestamp; nullable.
    DateTimeOffset? LastPurchaseAt { get; init; }
    // App-side plan identifier this snapshot describes.
    string PlanId { get; init; }
    // True when an active or trialing subscription for this plan exists on the customer.
    bool SubscriptionActive { get; init; }
    // Current period end when the subscription is active. Null when there's no subscription.
    DateTimeOffset? SubscriptionEndsAt { get; init; }
    // Raw Stripe status (active, trialing, past_due, etc.) when a subscription exists; null otherwise.
    string? SubscriptionStatus { get; init; }
    // True when the customer holds a one-time unlock for this plan. Sourced from customer metadata key unlock_{planId}; apps stamp it from their ApplyEventAsync on CheckoutCompleted .
    bool UnlockGranted { get; init; }
    // Timestamp parsed from the metadata stamp. Null when not held.
    DateTimeOffset? UnlockGrantedAt { get; init; }
  // Typed billing event surfaced by HandleWebhookAsync . Apps switch on Type and read the relevant fields. Unknown event types are surfaced as Unknown with the raw payload preserved for the app to inspect.
  sealed class BillingEvent : IEquatable<BillingEvent>
    // Typed billing event surfaced by HandleWebhookAsync . Apps switch on Type and read the relevant fields. Unknown event types are surfaced as Unknown with the raw payload preserved for the app to inspect.
    ctor(string EventId, BillingEventType Type, string? CustomerId, string? SubscriptionId, string? ClientReferenceId, string? PlanId, string? Status, DateTimeOffset? CurrentPeriodStart, DateTimeOffset? CurrentPeriodEnd, long? AmountPaid, string? Currency, JsonElement RawPayload)
    // Amount paid in minor units (cents), when relevant.
    long? AmountPaid { get; init; }
    // The client_reference_id set when creating checkout, when present. Apps use this to map the event back to their own entity.
    string? ClientReferenceId { get; init; }
    // ISO 4217 currency code in lowercase, when relevant.
    string? Currency { get; init; }
    // UTC period end, when present.
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    // UTC period start, when present on invoice/subscription events.
    DateTimeOffset? CurrentPeriodStart { get; init; }
    // Stripe customer id, when present on the payload.
    string? CustomerId { get; init; }
    // Stripe event id (evt_...). Use for idempotency.
    string EventId { get; init; }
    // Plan id from session metadata, when present.
    string? PlanId { get; init; }
    // Raw Stripe event JSON for app-side escape hatches.
    JsonElement RawPayload { get; init; }
    // Subscription status when relevant (active, past_due, canceled, ...).
    string? Status { get; init; }
    // Stripe subscription id, when present.
    string? SubscriptionId { get; init; }
    // Typed event kind.
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
  // Hosted Stripe invoice — for B2B net-30 flows where the customer pays via an emailed link rather than going through Checkout.
  sealed class BillingInvoice : IEquatable<BillingInvoice>
    // Hosted Stripe invoice — for B2B net-30 flows where the customer pays via an emailed link rather than going through Checkout.
    ctor(string Id, string? HostedInvoiceUrl, string? InvoicePdfUrl, string Status)
    string? HostedInvoiceUrl { get; init; }
    string Id { get; init; }
    string? InvoicePdfUrl { get; init; }
    string Status { get; init; }
  // Slim view of a Stripe invoice. Returned by ListInvoicesAsync .
  sealed class BillingInvoiceSummary : IEquatable<BillingInvoiceSummary>
    // Slim view of a Stripe invoice. Returned by ListInvoicesAsync .
    ctor(string Id, string? CustomerId, string? SubscriptionId, long AmountDueMinor, long AmountPaidMinor, string Currency, string Status, DateTimeOffset Created, DateTimeOffset? DueDate, string? HostedInvoiceUrl, string? InvoicePdfUrl)
    long AmountDueMinor { get; init; }
    long AmountPaidMinor { get; init; }
    DateTimeOffset Created { get; init; }
    string Currency { get; init; }
    string? CustomerId { get; init; }
    DateTimeOffset? DueDate { get; init; }
    string? HostedInvoiceUrl { get; init; }
    string Id { get; init; }
    string? InvoicePdfUrl { get; init; }
    string Status { get; init; }
    string? SubscriptionId { get; init; }
  // Single line item on a multi-line checkout. Use ForPrice for a preconfigured Stripe price, Dynamic for ad-hoc amounts (tipping, donations, custom-priced cart items).
  sealed class BillingLineItem : IEquatable<BillingLineItem>
    ctor()
    long? AdHocAmountMinor { get; init; }
    string? AdHocCurrency { get; init; }
    string? AdHocProductName { get; init; }
    bool AdHocRecurring { get; init; }
    string? AdHocRecurringInterval { get; init; }
    string? PriceId { get; init; }
    long Quantity { get; init; }
    static BillingLineItem Dynamic(long amountMinor, string currency, string productName, long quantity = 1)
    static BillingLineItem ForPrice(string priceId, long quantity = 1)
  enum BillingMode
    Subscription
    OneTime
  // Options needed by BillingService . Apps load secrets from their own configuration source (Ikon secrets, environment variables, vault) and pass them in here. The library never reads configuration directly.
  sealed class BillingOptions : IEquatable<BillingOptions>
    ctor()
    // Stripe secret API key (starts with sk_test_ or sk_live_). Required for Byok ; unused for IkonConnect (Ikon backend holds the platform key).
    string ApiKey { get; init; }
    // Stripe API version to pin (sent as Stripe-Version header). Null = account default.
    string? ApiVersion { get; init; }
    // Enable Stripe automatic tax calculation on Checkout sessions. Requires Tax to be configured in the Stripe Dashboard.
    bool AutomaticTax { get; init; }
    // Collect VAT / tax IDs at Checkout. When true, the Checkout session asks for a tax ID.
    bool CollectTaxId { get; init; }
    // Stripe Connect connected-account id (acct_...). When set, every Stripe API call is sent with the Stripe-Account header so charges, customers, prices etc. live on the connected account, not the platform account. Use this for the platform-managed Connect mode where one platform key serves many connected orgs/apps.
    string? ConnectedAccountId { get; init; }
    // Default cancel URL used when a checkout call does not specify one.
    string? DefaultCancelUrl { get; init; }
    // Free-form metadata merged into every Stripe object the library creates (customers, prices, products, checkout sessions, subscriptions, ...). Use to tag every record with the originating Ikon app id so a single connected account shared by multiple apps stays separable in reporting.
    IReadOnlyDictionary<string, string>? DefaultMetadata { get; init; }
    // Default Customer Portal return URL used when a portal call does not specify one.
    string? DefaultPortalReturnUrl { get; init; }
    // Default success URL used when a checkout call does not specify one.
    string? DefaultSuccessUrl { get; init; }
    string? IkonAppId { get; init; }
    string? IkonBackendUrl { get; init; }
    string? IkonWebhookSecret { get; init; }
    // Maximum number of retry attempts on transient failures (HTTP 429 / 5xx / network faults). 0 disables retries.
    int MaxRetryAttempts { get; init; }
    // Optional platform application fee in minor units applied to every one-time charge (Checkout in payment mode) when ConnectedAccountId is set. 0 disables.
    long? PlatformApplicationFeeAmountMinor { get; init; }
    // Optional platform application fee percent applied to every recurring charge (subscriptions / Checkout in subscription mode) when ConnectedAccountId is set. 0 disables. Range 0-100.
    decimal? PlatformApplicationFeePercent { get; init; }
    BillingProvider Provider { get; init; }
    // HTTP request timeout per Stripe call. Null = HttpClient default.
    TimeSpan? RequestTimeout { get; init; }
    // Base delay between retry attempts. Exponential backoff with jitter is layered on top.
    TimeSpan RetryBaseDelay { get; init; }
    // Stripe webhook signing secret (starts with whsec_). Required for webhook verification.
    string? WebhookSecret { get; init; }
  // One page of Stripe list results plus the cursor ( LastId ) to pass back to the next page call. HasMore reflects Stripe's has_more flag — true means at least one more page.
  sealed class BillingPage<T> : IEquatable<BillingPage<T>>
    // One page of Stripe list results plus the cursor ( LastId ) to pass back to the next page call. HasMore reflects Stripe's has_more flag — true means at least one more page.
    ctor(IReadOnlyList<T> Items, bool HasMore, string? LastId)
    bool HasMore { get; init; }
    IReadOnlyList<T> Items { get; init; }
    string? LastId { get; init; }
  // Result of creating a Stripe payment intent — used for custom payment flows outside of Checkout (in-app card forms, deferred capture, etc.).
  sealed class BillingPaymentIntent : IEquatable<BillingPaymentIntent>
    // Result of creating a Stripe payment intent — used for custom payment flows outside of Checkout (in-app card forms, deferred capture, etc.).
    ctor(string Id, string ClientSecret, string Status)
    // Client secret for confirmation via Stripe.js / Elements.
    string ClientSecret { get; init; }
    // Payment intent id (pi_...).
    string Id { get; init; }
    // Current status (requires_payment_method, requires_confirmation, requires_action, processing, succeeded, canceled).
    string Status { get; init; }
  // Result of creating a Stripe Payment Link — a shareable URL that opens a Stripe-hosted checkout for a fixed line item.
  sealed class BillingPaymentLink : IEquatable<BillingPaymentLink>
    // Result of creating a Stripe Payment Link — a shareable URL that opens a Stripe-hosted checkout for a fixed line item.
    ctor(string Id, string Url)
    string Id { get; init; }
    string Url { get; init; }
  // Slim view of a Stripe payment method. Returned by ListPaymentMethodsAsync .
  sealed class BillingPaymentMethod : IEquatable<BillingPaymentMethod>
    // Slim view of a Stripe payment method. Returned by ListPaymentMethodsAsync .
    ctor(string Id, string Type, string? CardBrand, string? CardLast4, int? CardExpMonth, int? CardExpYear)
    // Card brand when Type is card (e.g. visa).
    string? CardBrand { get; init; }
    // Card expiry month, when applicable.
    int? CardExpMonth { get; init; }
    // Card expiry year, when applicable.
    int? CardExpYear { get; init; }
    // Last four digits of the card, when applicable.
    string? CardLast4 { get; init; }
    // Stripe payment method id (pm_...).
    string Id { get; init; }
    // Stripe type (card, sepa_debit, etc.).
    string Type { get; init; }
  // App-plan-id → Stripe-price-id map produced by SyncAsync . Cache this in the app (memory or DB) and have your GetPlanAsync look up the price id from it.
  sealed class BillingPlanCatalogMap
    // App plan ids in the map.
    IEnumerable<string> AppPlanIds { get; }
    // Number of plans in the map.
    int Count { get; }
    // True when the map has a Stripe price id for this app plan.
    bool Contains(string appPlanId)
    // Look up the Stripe price id for an app plan. Throws when missing.
    string GetPriceId(string appPlanId)
    // Snapshot the map as a plain dictionary (for serialization, persistence).
    IReadOnlyDictionary<string, string> ToDictionary()
    // Try to look up a Stripe price id without throwing.
    bool TryGetPriceId(string appPlanId, out string priceId)
  // Describes a billable plan as the app sees it. Apps map their internal plan model onto this record before handing it to BillingService .
  sealed class BillingPlanDescriptor : IEquatable<BillingPlanDescriptor>
    ctor(string PlanId, string StripePriceId, BillingMode Mode, string? MeteredPriceId = null, long Quantity = 1, IReadOnlyDictionary<string, string>? Metadata = null, int? TrialPeriodDays = null, bool AllowPromotionCodes = false)
    bool AllowPromotionCodes { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? MeteredPriceId { get; init; }
    BillingMode Mode { get; init; }
    string PlanId { get; init; }
    long Quantity { get; init; }
    string StripePriceId { get; init; }
    int? TrialPeriodDays { get; init; }
    // Named factory for a credit-bundle top-up plan. Customer pays for a fixed bundle of credits; the app's IBillingCreditStore is granted the credits when the webhook completes. Same Stripe-side shape as Unlock — one-time charge against a fixed price — but semantically distinct because the entitlement is the granted credit balance, not a metadata stamp.
    static BillingPlanDescriptor Credits(string planId, string stripePriceId, int creditsGranted, IReadOnlyDictionary<string, string>? metadata = null)
    // Named factory for a recurring subscription plan. Sugar over the generic constructor that hides the Mode enum value and surfaces the most common subscription knobs explicitly.
    static BillingPlanDescriptor Subscription(string planId, string stripePriceId, int trialPeriodDays = 0, bool allowPromotionCodes = false, long quantity = 1, string? meteredPriceId = null, IReadOnlyDictionary<string, string>? metadata = null)
    // Named factory for a one-time unlock plan. The customer pays once and the entitlement is permanent (apps stamp customer metadata unlock_{planId} from ApplyEventAsync when the checkout completes; GetEntitlementAsync reads it back).
    static BillingPlanDescriptor Unlock(string planId, string stripePriceId, long quantity = 1, IReadOnlyDictionary<string, string>? metadata = null)
  // One row in an app's plan catalog. Apps declare these in code (or load from config) and hand the list to SyncAsync .
  sealed class BillingPlanSpec : IEquatable<BillingPlanSpec>
    // One row in an app's plan catalog. Apps declare these in code (or load from config) and hand the list to SyncAsync .
    ctor(string AppPlanId, string ProductName, long UnitAmountMinor, string Currency, string? Interval, int? IntervalCount = null, string? Description = null, string? Nickname = null, IReadOnlyDictionary<string, string>? Metadata = null, string? LookupKeyOverride = null)
    // App-side plan id (e.g. "pro"). Stable across environments — the platform key resolves to a different Stripe price per account.
    string AppPlanId { get; init; }
    // ISO 4217 currency, lowercase.
    string Currency { get; init; }
    // Optional product description.
    string? Description { get; init; }
    // Recurring interval (day, week, month, year) for subscriptions. Pass null for one-time prices — but typical SaaS catalogs are recurring.
    string? Interval { get; init; }
    // Multiplier on Interval (e.g. 3 with month = quarterly). Defaults to 1.
    int? IntervalCount { get; init; }
    string? LookupKeyOverride { get; init; }
    // Free-form metadata stamped on both the product (when first created) and the price.
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    // Optional price nickname (Stripe Dashboard label).
    string? Nickname { get; init; }
    // Stripe product name. Used as the idempotency lookup key — keep stable.
    string ProductName { get; init; }
    // Price in minor units (e.g. cents).
    long UnitAmountMinor { get; init; }
    // Credit-bundle spec. Metadata is stamped with credits_granted so the webhook handler knows how many credits to grant via GrantAsync .
    static BillingPlanSpec Credits(string appPlanId, string productName, long unitAmountMinor, string currency, int creditsGranted, string? description = null)
    // Recurring subscription spec. Sets Interval from interval .
    static BillingPlanSpec Subscription(string appPlanId, string productName, long unitAmountMinor, string currency, string interval, int? intervalCount = null, string? description = null)
    // One-time unlock spec. Interval is null.
    static BillingPlanSpec Unlock(string appPlanId, string productName, long unitAmountMinor, string currency, string? description = null)
  // Customer Portal feature toggles. When apps create their own portal configuration via CreatePortalConfigurationAsync they pass one of these and reference the returned id when creating portal sessions.
  sealed class BillingPortalConfigurationInfo : IEquatable<BillingPortalConfigurationInfo>
    ctor()
    bool AllowCustomerUpdate { get; init; }
    bool AllowInvoiceHistory { get; init; }
    bool AllowPaymentMethodUpdate { get; init; }
    bool AllowSubscriptionCancel { get; init; }
    bool AllowSubscriptionPause { get; init; }
    string? BusinessProfileHeadline { get; init; }
    string? PrivacyPolicyUrl { get; init; }
    string? SubscriptionCancelMode { get; init; }
    string? TermsOfServiceUrl { get; init; }
  // Result of creating a Customer Portal session.
  sealed class BillingPortalResult : IEquatable<BillingPortalResult>
    // Result of creating a Customer Portal session.
    ctor(string SessionId, string Url)
    string SessionId { get; init; }
    string Url { get; init; }
  // Slim view of a Stripe price.
  sealed class BillingPrice : IEquatable<BillingPrice>
    // Slim view of a Stripe price.
    ctor(string Id, string ProductId, long UnitAmountMinor, string Currency, string? RecurringInterval, bool Active, string? LookupKey = null)
    bool Active { get; init; }
    string Currency { get; init; }
    string Id { get; init; }
    string? LookupKey { get; init; }
    string ProductId { get; init; }
    string? RecurringInterval { get; init; }
    long UnitAmountMinor { get; init; }
  // Definition of a Stripe price. Use with CreatePriceAsync .
  sealed class BillingPriceInfo : IEquatable<BillingPriceInfo>
    bool Active { get; init; }
    string Currency { get; init; }
    // Stable Stripe-side lookup key (alphanumeric + underscores). Stripe price ids are opaque; setting LookupKey lets apps resolve a price via RetrievePriceByLookupKeyAsync without listing or storing the price id. Recommended pattern for app-owned plan catalogs.
    string? LookupKey { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string? Nickname { get; init; }
    string ProductId { get; init; }
    // Set for recurring prices (subscriptions). Null = one-time price.
    string? RecurringInterval { get; init; }
    int? RecurringIntervalCount { get; init; }
    // When true, if a price with the same LookupKey already exists, Stripe transfers the lookup key to the new price (silently detaching from the previous one). Use when replacing a price (since Stripe prices are immutable) so the lookup-key handle stays stable.
    bool TransferLookupKey { get; init; }
    long UnitAmountMinor { get; init; }
  // Slim view of a Stripe product.
  sealed class BillingProduct : IEquatable<BillingProduct>
    // Slim view of a Stripe product.
    ctor(string Id, string Name, bool Active, string? Description)
    bool Active { get; init; }
    string? Description { get; init; }
    string Id { get; init; }
    string Name { get; init; }
  // Definition of a Stripe product. Use with CreateProductAsync .
  sealed class BillingProductInfo : IEquatable<BillingProductInfo>
    bool Active { get; init; }
    string? Description { get; init; }
    string? Id { get; init; }
    IReadOnlyList<string>? Images { get; init; }
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    string Name { get; init; }
    string? StatementDescriptor { get; init; }
  // Selects the Stripe transport used by BillingService .
  enum BillingProvider
    Disabled
    Byok
    IkonConnect
  // Declares the function requires the current customer to hold an active subscription for planId . Resolves via the ambient Current instance and reads the customer from UserId . The policy is webhook-driven, not polling-driven: on missing entitlement it DENIES with a stable code (billing_subscription_required), and the app's UI catches it and opens checkout via CreateCheckoutAsync or CreateEmbeddedCheckoutAsync . Stripe's webhook then flips the entitlement and the user retries.
  sealed class BillingRequireSubscriptionAttribute : PolicyAttribute
    ctor(string planId)
    // App-side plan id the subscription is keyed to.
    string PlanId { get; }
    override IFunctionPolicy CreatePolicy()
  // Declares the function requires the current customer to hold a one-time unlock for planId . Reads UnlockGranted from the ambient Current . Deny code: billing_unlock_required. App UI handles checkout offer + retry.
  sealed class BillingRequireUnlockAttribute : PolicyAttribute
    ctor(string planId)
    string PlanId { get; }
    override IFunctionPolicy CreatePolicy()
  // Single entry point for app-level billing operations: hosted Stripe Checkout, Customer Portal, webhook verification + dispatch, metered usage reporting, subscription management, and refunds. Apps construct one instance per process and reuse it.
  sealed class BillingService
    ctor(BillingOptions options, IBillingAppAdapter adapter)
    // Optional app-supplied credit ledger. When set, GetEntitlementAsync uses it as the default credit-store unless caller passes their own, and the ChargeCreditsAttribute policy can locate it without extra plumbing. Mutable so apps can wire it after construction (e.g. once their DB context is available).
    IBillingCreditStore? CreditStore { get; set; }
    // Most recently constructed BillingService instance observable from the current execution flow. Set as a side effect of the constructor so ambient consumers (policy attributes like [BillingRequireSubscription], Parallax components that want a default) can resolve without DI. Backed by AsyncLocal`1 so per-flow values are isolated.
    static BillingService Current { get; }
    // Add a one-off line item to a customer's next invoice. Used for B2B usage true-ups, mid-cycle add-ons, or arbitrary chargebacks.
    Task AddInvoiceItemAsync(string stripeCustomerId, long amountMinor, string currency, string description, string? subscriptionId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Adjust a customer's balance, in minor units. Negative values credit the customer (reduce future invoice amounts); positive values debit. Useful for refund-as-credit, goodwill credits, or service-failure credits.
    Task AdjustCustomerBalanceAsync(string stripeCustomerId, long amountMinorDelta, string currency, string description, string idempotencyKey, CancellationToken cancellationToken = null)
    // Cancel a payment intent that hasn't been captured.
    Task<BillingPaymentIntent> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = null)
    // Cancel a Stripe subscription. immediate false = cancel at period end (Stripe keeps the subscription active until then); true = end now and prorate.
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, CancellationToken cancellationToken = null)
    // Cancel a subscription schedule. The current phase ends; no further phases run.
    Task CancelSubscriptionScheduleAsync(string scheduleId, CancellationToken cancellationToken = null)
    // Capture a previously authorized (manual capture) payment intent.
    Task<BillingPaymentIntent> CapturePaymentIntentAsync(string paymentIntentId, long? amountToCaptureMinor = null, CancellationToken cancellationToken = null)
    // Create a Stripe Checkout session with arbitrary line items — preconfigured prices, dynamic per-call amounts (donations, tipping, custom carts), or a mix. Use ForPrice and Dynamic .
    Task<BillingCheckoutResult> CreateCartCheckoutAsync(IEnumerable<BillingLineItem> lines, BillingMode mode, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe Checkout session for a single plan. Pass appCustomerKey to bind the session to an existing app entity (the adapter resolves a Stripe customer); pass null for guest checkout (Stripe creates a customer from the supplied email ).
    Task<BillingCheckoutResult> CreateCheckoutAsync(string planId, string? appCustomerKey, string? email, string? successUrl = null, string? cancelUrl = null, string? clientReferenceId = null, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe coupon. Set exactly one of PercentOff or AmountOffMinor . For repeating coupons supply DurationInMonths .
    Task<string> CreateCouponAsync(BillingCouponInfo coupon, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Issue a credit note against a finalized invoice. Use credit notes — not raw refunds — when tax was charged on the invoice; Stripe handles the tax reversal and regenerates the PDF. Apps split the credit between an out-of-pocket refund ( info . RefundAmountMinor ) and a customer-balance credit ( CreditAmountMinor ).
    Task<BillingCreditNote> CreateCreditNoteAsync(BillingCreditNoteInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a new Stripe customer directly (independent of checkout). Useful for B2B flows where the customer record needs to exist before any payment, or for invoice-only billing.
    Task<string> CreateCustomerAsync(BillingCustomerInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Attach a tax id (VAT, GST, etc.) to an existing customer.
    Task<BillingTaxId> CreateCustomerTaxIdAsync(string stripeCustomerId, string type, string value, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create an embedded Checkout session — same as CreateCheckoutAsync but returns a ClientSecret for mounting the checkout inside the app instead of redirecting to a hosted page. Apps pass the secret to Stripe.js / the embedded React component.
    Task<BillingEmbeddedCheckout> CreateEmbeddedCheckoutAsync(string planId, string? appCustomerKey, string? email, string returnUrl, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create, finalize, and (optionally) send a hosted Stripe invoice. Used for B2B net-30 flows: the customer receives a payable invoice URL by email and pays without a Checkout session.
    Task<BillingInvoice> CreateHostedInvoiceAsync(string stripeCustomerId, IEnumerable<BillingLineItem> lines, int daysUntilDue, bool autoSend = true, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe payment intent — the building block for custom in-app payment flows that don't use Checkout. Apps pass the returned ClientSecret to Stripe.js / Elements on the frontend.
    Task<BillingPaymentIntent> CreatePaymentIntentAsync(long amountMinor, string currency, string? stripeCustomerId = null, string captureMethod = "automatic", string? paymentMethodId = null, bool confirm = false, IReadOnlyDictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe Payment Link — a shareable hosted-checkout URL for a fixed line item. Useful for "pay link" flows in chat, email, QR codes.
    Task<BillingPaymentLink> CreatePaymentLinkAsync(IEnumerable<BillingLineItem> lines, IReadOnlyDictionary<string, string>? metadata = null, bool allowPromotionCodes = false, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Customer Portal session so the customer can manage their subscription.
    Task<BillingPortalResult> CreatePortalAsync(string stripeCustomerId, string? returnUrl = null, CancellationToken cancellationToken = null)
    // Create a Customer Portal configuration. Apps that want to control which self-serve features the portal exposes (cancel, update payment method, view invoices, etc.) call this once and reuse the returned id when opening portal sessions via CreatePortalAsync .
    Task<string> CreatePortalConfigurationAsync(BillingPortalConfigurationInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe price (one-time or recurring) attached to a product.
    Task<string> CreatePriceAsync(BillingPriceInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe product. Apps that build catalogs programmatically can call this instead of clicking through the Dashboard.
    Task<string> CreateProductAsync(BillingProductInfo info, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a promotion code attached to a Stripe coupon. Apps create promotion codes for marketing campaigns, partner deals, etc. The couponId must already exist in Stripe (managed in the Dashboard or via Stripe API or CreateCouponAsync ).
    Task<string> CreatePromotionCodeAsync(string couponId, string code, DateTimeOffset? expiresAt = null, long? maxRedemptions = null, string? restrictedToCustomerId = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Setup Intent — a placeholder for capturing a payment method without charging. Apps use this for "save card on file" or trial-to-paid transitions where the user authorizes a card during the trial.
    Task<BillingSetupIntent> CreateSetupIntentAsync(string stripeCustomerId, string usage = "off_session", string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Create a Stripe subscription schedule with multiple phases — useful for discounted intro phases that transition to standard pricing, or annual commitments built from a sequence of monthly phases.
    Task<string> CreateSubscriptionScheduleAsync(string stripeCustomerId, IEnumerable<BillingSubscriptionPhase> phases, DateTimeOffset? startDate = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Mint a one-time hosted checkout for a tip / voluntary payment. Confers no entitlement — apps record the transaction for attribution / reporting and (optionally) ack it in the UI. Wraps CreateCartCheckoutAsync with a dynamic line item; metadata is stamped with tip_amount_minor for downstream reporting.
    Task<BillingCheckoutResult> CreateTipCheckoutAsync(long amountMinor, string currency, string? title = null, string? message = null, string? appCustomerKey = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Register a webhook endpoint with Stripe programmatically. The returned Secret is the signing secret — store it securely; Stripe will not return it again on subsequent reads.
    Task<BillingWebhookEndpoint> CreateWebhookEndpointAsync(string url, IEnumerable<string> enabledEvents, string? description = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Delete a tax id from a customer.
    Task DeleteCustomerTaxIdAsync(string stripeCustomerId, string taxIdId, CancellationToken cancellationToken = null)
    // Delete a webhook endpoint by id.
    Task DeleteWebhookEndpointAsync(string webhookEndpointId, CancellationToken cancellationToken = null)
    // Detach a saved payment method from its customer.
    Task DetachPaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = null)
    // One-shot "does this customer have access to this plan" snapshot — composes the adapter customer resolution, a filtered subscription list, a customer-metadata read, and (optionally) a credit-store lookup into a single BillingEntitlement record. Subscription gate: filters Stripe subscriptions by the plan's StripePriceId + status in active|trialing. Cancel-at-period-end subscriptions stay SubscriptionActive =true until the period ends (mirrors Stripe semantics).Unlock gate: reads customer metadata key unlock_{planId}. Apps stamp this key (ISO-8601 timestamp value) from ApplyEventAsync when CheckoutCompleted arrives for a one-time plan.Credit gate: when creditStore is supplied, queries the customer's wallet for the SKU. Pass null when the plan is subscription-or-unlock only.
    Task<BillingEntitlement> GetEntitlementAsync(string planId, string appCustomerKey, IBillingCreditStore? creditStore = null, CancellationToken cancellationToken = null)
    // Verify + dispatch a webhook payload signed by Ikon's billing backend in IkonConnect mode. Use this method instead of HandleWebhookAsync when the customer app's webhook endpoint receives forwarded events from Ikon (signature in Ikon-Signature header, signed with the per-tenant shared secret from IkonWebhookSecret ).
    Task<BillingWebhookResult> HandleIkonWebhookAsync(string? signatureHeader, string body, TimeSpan? tolerance = null, CancellationToken cancellationToken = null)
    // Verify and dispatch a Stripe webhook delivery. Returns a structured result; never throws on signature failure. When Verified is true the parsed event has already been delivered to ApplyEventAsync .
    Task<BillingWebhookResult> HandleWebhookAsync(string? signatureHeader, string body, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListApplePayDomainsAsync(int limit = 100, CancellationToken cancellationToken = null)
    // List charges, optionally filtered to one customer. Used for app-side receipts and admin reporting screens.
    Task<IReadOnlyList<BillingCharge>> ListChargesAsync(string? stripeCustomerId = null, int limit = 100, DateTimeOffset? createdAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCouponsAsync(int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCreditNotesAsync(string? invoiceId = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListCustomerTaxIdsAsync(string stripeCustomerId, int limit = 100, CancellationToken cancellationToken = null)
    // List Stripe events for replay or audit. Apps that missed a webhook delivery (downtime) refetch via this and feed the events back through HandleWebhookAsync -equivalent dispatch.
    Task<IReadOnlyList<string>> ListEventIdsAsync(string? type = null, DateTimeOffset? createdAfter = null, int limit = 100, CancellationToken cancellationToken = null)
    // List invoices, optionally filtered to one customer or subscription.
    Task<IReadOnlyList<BillingInvoiceSummary>> ListInvoicesAsync(string? stripeCustomerId = null, string? subscriptionId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPaymentLinksAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    // List a customer's saved payment methods. Apps display these on a "manage payment methods" screen.
    Task<IReadOnlyList<BillingPaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card", int limit = 100, CancellationToken cancellationToken = null)
    // List prices, optionally filtered to a single product (single page). For catalogs > 100 prices use ListPricesPageAsync to paginate.
    Task<IReadOnlyList<BillingPrice>> ListPricesAsync(string? productId = null, bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    // One page of prices with cursor. Pass the returned LastId back as startingAfter on the next call to walk the full price set. Loop until HasMore is false.
    Task<BillingPage<BillingPrice>> ListPricesPageAsync(string? productId = null, bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    // List products in the catalog (single page). For catalogs > 100 products use ListProductsPageAsync to paginate.
    Task<IReadOnlyList<BillingProduct>> ListProductsAsync(bool activeOnly = true, int limit = 100, CancellationToken cancellationToken = null)
    // One page of products with cursor. Pass the returned LastId back as startingAfter on the next call to walk the full catalog. Loop until HasMore is false.
    Task<BillingPage<BillingProduct>> ListProductsPageAsync(bool activeOnly = true, int limit = 100, string? startingAfter = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListPromotionCodesAsync(int limit = 100, CancellationToken cancellationToken = null)
    // List Stripe subscriptions, optionally filtered by customer or status.
    Task<IReadOnlyList<BillingSubscription>> ListSubscriptionsAsync(string? stripeCustomerId = null, string? status = null, int limit = 100, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> ListWebhookEndpointsAsync(int limit = 100, CancellationToken cancellationToken = null)
    // Convenience: check entitlement first, then mint a checkout session only if the customer doesn't already have access. Returns a BillingCheckoutOffer describing which branch fired. App pattern: var offer = await billing.OfferCheckoutAsync("pro", appCustomerKey); if (offer.AlreadyEntitled) { } else { await ClientFunctions.SetUrlAsync(offer.Url!); } Subscription mode counts active+trialing as "entitled"; one-time mode counts a customer-metadata unlock stamp as "entitled".
    Task<BillingCheckoutOffer> OfferCheckoutAsync(string planId, string appCustomerKey, string? email = null, BillingDestination? destination = null, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Pause collection on a subscription. The subscription remains active for access purposes; Stripe just stops creating invoices until resumed.
    Task PauseSubscriptionAsync(string subscriptionId, string behavior = "void", CancellationToken cancellationToken = null)
    // Preview a customer's upcoming invoice. Use to show "your next bill" before committing a plan change, seat-count change, or coupon.
    Task<BillingUpcomingInvoice> PreviewUpcomingInvoiceAsync(string stripeCustomerId, string? subscriptionId = null, string? newPriceId = null, long? newQuantity = null, string? couponId = null, CancellationToken cancellationToken = null)
    // Refund a charge or payment intent, in full or partially. Use a stable idempotencyKey (typically the app's refund record id).
    Task RefundAsync(string paymentIntentId, long? amountMinor, string? reason, string idempotencyKey, CancellationToken cancellationToken = null)
    // Register an Apple Pay domain so the domain can host Apple Pay buttons.
    Task<string> RegisterApplePayDomainAsync(string domainName, string? idempotencyKey = null, CancellationToken cancellationToken = null)
    // Report a meter event for metered usage billing. Apps call this whenever a billable usage unit is consumed.
    Task ReportUsageAsync(string meterEventName, string stripeCustomerId, long value, string idempotencyKey, DateTimeOffset? timestamp = null, CancellationToken cancellationToken = null)
    // Un-cancel a subscription that was scheduled to cancel at period end. Clears cancel_at_period_end. The subscription continues normally. Has no effect if the subscription is already fully canceled.
    Task ResumeCanceledSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    // Resume collection on a paused subscription.
    Task ResumeSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = null)
    // Retrieve a single Stripe event by id, parsed into a typed BillingEvent . Apps use this for webhook replay: fetch the event and feed it into the same handler that ApplyEventAsync runs, but skip signature checks since the body came from Stripe directly.
    Task<BillingEvent> RetrieveEventAsync(string eventId, CancellationToken cancellationToken = null)
    // Resolve a price by its app-set LookupKey . Returns null when no active price has that lookup key. O(1) on the Stripe side; no listing or pagination needed.
    Task<BillingPrice?> RetrievePriceByLookupKeyAsync(string lookupKey, CancellationToken cancellationToken = null)
    // Search Stripe customers using Stripe's search query syntax (e.g. email:'biz@example.com', metadata['app_id']:'abc').
    Task<IReadOnlyList<string>> SearchCustomersAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    // Convenience wrapper over SearchCustomersAsync that builds the Stripe Search query metadata['app_customer_key']:'X' — the recommended idiom for resolving Stripe customer ids from an app's stable user key. Returns matched customer ids (typically 0 or 1).
    Task<IReadOnlyList<string>> SearchCustomersByAppKeyAsync(string appCustomerKey, string metadataKey = "app_customer_key", int limit = 1, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<string>> SearchCustomersDetailedAsync(string query, int limit = 100, CancellationToken cancellationToken = null)
    // Update mutable fields on an existing Stripe customer.
    Task UpdateCustomerAsync(string stripeCustomerId, BillingCustomerInfo info, CancellationToken cancellationToken = null)
    // Update mutable fields on an existing price. Stripe prices are immutable in their amount/currency/recurring shape, but active, nickname and metadata can change. Use active = false to archive an old price after migrating subscribers off it.
    Task UpdatePriceAsync(string priceId, bool? active = null, string? nickname = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    // Update mutable fields on an existing product. Use active = false to archive a product (and its prices) when retiring a plan.
    Task UpdateProductAsync(string productId, bool? active = null, string? name = null, string? description = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = null)
    // Update the quantity of a subscription item — typically used for seat-based billing where a customer adds or removes editor seats mid-cycle.
    Task UpdateSubscriptionItemQuantityAsync(string subscriptionItemId, long quantity, bool prorate = true, CancellationToken cancellationToken = null)
    // Swap the price on a subscription item (e.g. migrate an existing subscriber to a new price after a plan change, since Stripe prices are immutable). Stripe prorates by default — pass prorate = false for clean cycle boundaries. Typical flow after a plan-price bump: // 1. Sync catalog → new price id under same lookup_key var map = await catalogSync.SyncAsync(plans); // 2. Migrate active subscribers foreach (var sub in await billing.ListSubscriptionsAsync(status: "active")) { await billing.UpdateSubscriptionPriceAsync(sub.ItemIds[0], map.GetPriceId("pro")); }
    Task UpdateSubscriptionPriceAsync(string subscriptionItemId, string newPriceId, bool prorate = true, CancellationToken cancellationToken = null)
    // Replace the phases on an existing subscription schedule. Used when a schedule needs to be re-planned mid-flight (e.g. customer renegotiated).
    Task UpdateSubscriptionScheduleAsync(string scheduleId, IEnumerable<BillingSubscriptionPhase> phases, CancellationToken cancellationToken = null)
    // Void a previously issued credit note.
    Task VoidCreditNoteAsync(string creditNoteId, CancellationToken cancellationToken = null)
  // Result of creating a Stripe Setup Intent. Used to capture a payment method without charging (e.g. card-on-file before a trial converts to paid).
  sealed class BillingSetupIntent : IEquatable<BillingSetupIntent>
    // Result of creating a Stripe Setup Intent. Used to capture a payment method without charging (e.g. card-on-file before a trial converts to paid).
    ctor(string Id, string ClientSecret)
    // Client secret to be passed to Stripe.js for confirmation.
    string ClientSecret { get; init; }
    // Setup intent id (seti_...).
    string Id { get; init; }
  // Slim view of a Stripe subscription. Returned by ListSubscriptionsAsync .
  sealed class BillingSubscription : IEquatable<BillingSubscription>
    // Slim view of a Stripe subscription. Returned by ListSubscriptionsAsync .
    ctor(string Id, string CustomerId, string Status, DateTimeOffset? CurrentPeriodStart, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd, string? DefaultPaymentMethodId, string? LatestInvoiceId, IReadOnlyList<string> ItemIds)
    // True when subscription is scheduled to cancel at period end.
    bool CancelAtPeriodEnd { get; init; }
    // Current billing period end.
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    // Current billing period start.
    DateTimeOffset? CurrentPeriodStart { get; init; }
    // Customer id.
    string CustomerId { get; init; }
    // Saved payment method used for renewals.
    string? DefaultPaymentMethodId { get; init; }
    // Subscription id (sub_...).
    string Id { get; init; }
    // Subscription item ids — pass to UpdateSubscriptionItemQuantityAsync .
    IReadOnlyList<string> ItemIds { get; init; }
    // Most recent invoice id, when present.
    string? LatestInvoiceId { get; init; }
    // active, trialing, past_due, canceled, incomplete, etc.
    string Status { get; init; }
  // One phase of a subscription schedule — a price + duration pair. Used by CreateSubscriptionScheduleAsync for multi-phase billing (e.g. discounted intro then full price).
  sealed class BillingSubscriptionPhase : IEquatable<BillingSubscriptionPhase>
    // One phase of a subscription schedule — a price + duration pair. Used by CreateSubscriptionScheduleAsync for multi-phase billing (e.g. discounted intro then full price).
    ctor(string StripePriceId, long Quantity = 1, int? Iterations = null)
    // How many billing cycles this phase lasts. Final phase may be open-ended (omit iterations on the last phase to make it run forever).
    int? Iterations { get; init; }
    // Quantity of the subscription line item.
    long Quantity { get; init; }
    // Stripe Price id active during this phase.
    string StripePriceId { get; init; }
  // Stripe Tax exemption modes. Maps to tax_exempt on the Stripe customer object.
  enum BillingTaxExempt
    None
    Exempt
    Reverse
  // Slim view of a customer's tax id record (VAT, GST, etc.).
  sealed class BillingTaxId : IEquatable<BillingTaxId>
    // Slim view of a customer's tax id record (VAT, GST, etc.).
    ctor(string Id, string Type, string Value, string? Country)
    // ISO country code, when present.
    string? Country { get; init; }
    // Stripe tax id object id (txi_...).
    string Id { get; init; }
    // Stripe tax id type (e.g. eu_vat, gb_vat, us_ein).
    string Type { get; init; }
    // The tax id value as the customer entered it.
    string Value { get; init; }
  // Preview of a customer's next invoice — used to show "your next bill will be X" UI before a plan change is committed. Returned by PreviewUpcomingInvoiceAsync .
  sealed class BillingUpcomingInvoice : IEquatable<BillingUpcomingInvoice>
    // Preview of a customer's next invoice — used to show "your next bill will be X" UI before a plan change is committed. Returned by PreviewUpcomingInvoiceAsync .
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
    string? PriceId { get; init; }
    bool Proration { get; init; }
    long Quantity { get; init; }
  // Result of registering or fetching a Stripe webhook endpoint.
  sealed class BillingWebhookEndpoint : IEquatable<BillingWebhookEndpoint>
    // Result of registering or fetching a Stripe webhook endpoint.
    ctor(string Id, string Url, string? Secret, string Status)
    // Endpoint id (we_...).
    string Id { get; init; }
    // Webhook signing secret. Stripe returns this only on creation; subsequent fetches return null.
    string? Secret { get; init; }
    // enabled or disabled.
    string Status { get; init; }
    // URL Stripe posts events to.
    string Url { get; init; }
  sealed class BillingWebhookFunctionHost
    ctor(BillingService billing)
    Task<string> StripeWebhook(Dictionary<string, string> queryParams, Dictionary<string, string> headers, string body)
  // Outcome of HandleWebhookAsync . Surfaces signature verification status without throwing — apps return HTTP 200 either way to avoid Stripe retry storms, but log unverified deliveries.
  sealed class BillingWebhookResult : IEquatable<BillingWebhookResult>
    // Outcome of HandleWebhookAsync . Surfaces signature verification status without throwing — apps return HTTP 200 either way to avoid Stripe retry storms, but log unverified deliveries.
    ctor(bool Verified, string? Reason, BillingEvent? Event, string? AdapterError = null)
    // Set when the signature verified and event parsed cleanly but ApplyEventAsync threw. Apps decide whether to return 200 (acknowledge, retry isn't useful) or 500 (let Stripe retry). Null when the adapter call succeeded or wasn't reached.
    string? AdapterError { get; init; }
    // Parsed event when Verified is true; null otherwise.
    BillingEvent? Event { get; init; }
    // Reason for failure when Verified is false; null on success.
    string? Reason { get; init; }
    // True when the Stripe signature was validated against the configured webhook secret.
    bool Verified { get; init; }
  // Bridge between the library and an app's domain model. The library calls back into the adapter to look up plans, resolve customers, and to deliver verified webhook events. Apps own all persistence — the library never touches an app database directly.
  interface IBillingAppAdapter
    // Apply a verified billing event to the app's domain. The library calls this from HandleWebhookAsync after signature verification. Apps must implement idempotency using EventId .
    abstract Task ApplyEventAsync(BillingEvent evt, CancellationToken cancellationToken)
    // Resolve a plan by its app-side id. Return null if the plan is unknown or archived.
    abstract Task<BillingPlanDescriptor?> GetPlanAsync(string planId, CancellationToken cancellationToken)
    // Return a Stripe customer id for the given app-side customer key, creating one if it does not yet exist. Apps should persist the mapping so subsequent calls return the same Stripe customer id.
    abstract Task<string> ResolveStripeCustomerIdAsync(string appCustomerKey, string? email, CancellationToken cancellationToken)
  interface IBillingConnectAccountStore
    abstract Task ClearAsync(CancellationToken cancellationToken = null)
    abstract Task<string?> GetAsync(CancellationToken cancellationToken = null)
    abstract Task SetAsync(string connectedAccountId, CancellationToken cancellationToken = null)
  // App-owned credit ledger contract. The library never persists credit balances itself — credits are an app concern (wallet table in app DB, KV store, etc.). Apps implement this interface and pass it to GetEntitlementAsync and to [Billing.ChargeCredits] policy attributes. All methods are scoped by (appCustomerKey, sku). The library supplies a stable idempotencyKey so apps can dedupe concurrent deductions on the same charge event (e.g. a webhook replaying the same checkout.session.completed).
  interface IBillingCreditStore
    // Atomically deduct credits from the customer's balance. Returns the new balance. Throws or returns negative balance when insufficient — implementations choose; the policy-attribute layer treats < 0 as denial. idempotencyKey dedupes replays.
    abstract Task<int> DeductAsync(string appCustomerKey, string sku, int credits, string idempotencyKey, CancellationToken cancellationToken = null)
    // Current balance for the given customer + SKU. Returns 0 when no row exists.
    abstract Task<int> GetCreditsAsync(string appCustomerKey, string sku, CancellationToken cancellationToken = null)
    // Atomically grant credits to the customer's balance. Returns the new balance. Called from the adapter's ApplyEventAsync when a top-up checkout completes. idempotencyKey dedupes replays (typically the Stripe EventId ).
    abstract Task<int> GrantAsync(string appCustomerKey, string sku, int credits, string idempotencyKey, CancellationToken cancellationToken = null)

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
    // Every CellAttribute -decorated type the host discovered during construction. Read-only enumeration used by higher layers (e.g. typed-HTTP-endpoint discovery) that need to iterate cells without owning the directory.
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
    // Resolve (or spawn) a cell instance by the cell type's simple name and a SessionIdentity field dict (typically the URL query params from an inbound webhook). The host constructs the SessionIdentity record from the dict by matching the record's primary-constructor parameter names; missing nullable/default-valued fields use null/their default; missing required fields throw. Returns the cell instance as Object — callers cast to the wire interface they expect or use reflection to invoke methods.
    object ResolveByCellTypeName(string cellTypeName, IReadOnlyDictionary<string, string> sessionIdentityFields)
    // Look up the registered [Cell] concrete type whose wire-interface mapping matches iface . Returns the same type that Resolve``1 would dispatch to. Used by Cells.Connect<TInterface> to consult the cell's CellAttribute (e.g. for ProcessScope ) before deciding between local resolution and substrate-proxy routing.
    bool TryGetCellTypeForInterface(Type iface, out Type cellType)
    // Raised when a NEW cell type appears in the host after construction — specifically when RegisterSingleton registers an instance whose type wasn't already known. Higher layers (IkonServer) that snapshot the topology at build time — e.g. the discovered MCP-tool host and typed-HTTP-endpoint list — subscribe to rebuild those snapshots. This is load-bearing for app-level [McpTool]: the user's [App] instance is registered lazily on first client join (via HttpEndpointWrapperRegistration.RegisterAll), long after the host's initial discovery walk.
    event Action? TopologyChanged
  // The wire-name conventions for cell members. Both the substrate-cell proxy (the caller) and the cell-host's webhook-wrapper registration (the producer) build these names; keeping the format in one place stops the two sides from drifting apart.
  static class CellNaming
    // The SDK function name for a cell's [Function] method: {CellType.FullName}.{Method}. Matches how FunctionRegistry.RegisterFromInstance names instance methods, so a substrate-cell proxy can call them over its SDK connection to the cell-host.
    static string SdkFunctionName(Type cellType, string methodName)
    // The webhook function name for a cell's [HttpEndpoint] method: {CellType}_{Method}. The cloud's webhook ingress accepts a single path segment after /w/, so the cell-typed {Type}/{Method} shape is flattened to an underscore.
    static string WebhookFunctionName(Type cellType, string methodName)
    // The SDK function name a cell-host exposes to advertise the base URL of its AppEndpointHost — the relay tunnel serving the cell's [HttpEndpoint] + [McpTool] routes. A SubstrateCellProxy calls it over the cell-host SDK connection to learn where to POST [HttpEndpoint] requests directly, instead of going through the cloud webhook gateway. Producer (the cell-host startup path) and consumer (SubstrateCellProxy) must agree on this name.
    static string CellEndpointBaseUrlFunctionName
  // Where a CellAttribute -decorated type's instances live.
  enum CellProcessScope
    AppProcess
    Substrate
  // Static accessor for the process-wide CellHost plus the wiring substrate-cell proxies need: the webhook-URL resolver (for [HttpEndpoint] methods) and the cell-client factory (for [Function] methods and Reactive<T> state, which ride a standard IkonClient SDK connection to the cell-host).
  static class Cells
    // The currently installed process-wide cell host, or null if none has been installed yet. Use this when you want to reuse the shared host with a graceful fallback. For fail-fast access prefer Connect``1 .
    static CellHost? Current { get; }
    static TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    // Dispose every live cell-host connection. Call on app shutdown. Idempotent.
    static ValueTask DisposeAsync()
    // Install the process-wide cell host. Replaces any previous host (last-call-wins) so tests can swap freely. Also clears the webhook-URL resolver and the cell-client factory, and drops the connection registry — apps re-register the resolver/factory after each Initialize. Production calls Initialize once at startup, so this only matters in tests that re-run Initialize between scenarios.
    static void Initialize(CellHost host)
    // Register the factory that opens a standard-SDK IkonClient connection to a substrate cell-host. Called by the app host at startup — the app process has the backend context (space id, login) the factory needs. SubstrateCellProxy`1 uses it for [Function]-marked methods and Reactive<T> members; without it, those throw a clear error while [HttpEndpoint] methods still work.
    static void SetCellClientFactory(Func<CellConnectRequest, Task<IkonClient>> factory)
    // Register the function that maps a webhook function name (e.g. "LabCell_IncrementHttp") to its public URL. Called by the app host at startup so SubstrateCellProxy`1 can dispatch a substrate cell's [HttpEndpoint] methods over stateless HTTP. Methods the resolver returns no URL for fall through to the SDK connection.
    static void SetWebhookUrlResolver(Func<string, string?> resolver)
    // Reserved key in an SDK connection's parameters that names the substrate cell type to route to. The cell's SessionIdentity-record fields ride alongside it. MUST stay in sync with the cloud's CELL_TYPE_PARAM in cell-routing.ts — that's what ChannelInstanceService.create keys on to provision a cell-host channel-instance.
    static string CellTypeParam
  // Framework handle injected into a cell's primary constructor. Exposes the SessionIdentity the cell was instantiated for; future revisions add lifetime, config, etc.
  interface ICell<TSessionIdentity>
    // The SessionIdentity record value this cell instance is keyed by.
    TSessionIdentity Identity { get; }
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
  // Marks a method on a cell as an MCP-exposed tool. The framework discovers these at startup, reflects the method's parameters into a JSON Schema, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP tools/call requests to it.
  sealed class McpToolAttribute : Attribute
    ctor()
    // Description shown to MCP clients so the agent's LLM can decide when to invoke the tool. Empty values pass through verbatim — there is no XML-summary fallback today.
    string Description { get; init; }
    // MCP-wire tool name presented to clients in tools/list. Defaults to the method name when null or empty. The governance subject id is always the structural "{CellType}.{Method}" regardless of this property, so the same Mission rules gate both HTTP and MCP invocations even when the MCP wire name differs.
    string? Name { get; init; }
  // Runtime DispatchProxy for a [Cell(ProcessScope = Substrate)] cell type. App processes call the cell as if it were local; the proxy hides the network hop and picks a transport per member: [HttpEndpoint] methods — dispatched as stateless HTTP POST. The target is the cell-host's own IkonClient -discovered endpoint base URL when available, falling back to the cloud webhook-gateway URL otherwise.other methods — dispatched over a standard IkonClient SDK connection to the cell-host (the cell must expose them via [Function] / [RegisterAll] so they are callable on the wire).Reactive<T> members — return a cached local read-only mirror fed by an SDK subscription; reads and Changed events work locally, mutations flow through cell methods. The SDK connection is opened lazily on first need. Even a cell reached only through [HttpEndpoint] methods opens one once, to discover the cell-host's endpoint base URL.
  class SubstrateCellProxy<TInterface> : DispatchProxy where TInterface : class
    ctor()
    // Build a proxy implementing TInterface for the given substrate cell.
    static TInterface Create(Type cellType, object sessionIdentity, Func<string, string?> webhookUrlResolver)

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
    ctor(TClientParameters parameters)
    TClientParameters Parameters { get; }

namespace Ikon.App.Http
  // Bridges in-process HTTP cell-method dispatch through the active GovernanceScope hook. With no hook active this is a pass-through; with one set, the invocation flows through RunAsync``1 with the structural {CellType}.{Method} subject id so the same Mission gates HTTP and MCP symmetrically.
  static class HttpDispatchGovernance
    static Task<object?> InvokeAsync(MethodInfo handler, Type ownerType, IReadOnlyDictionary<string, object?> args, Func<Task<object?>> invoke, CancellationToken ct = null)
  // Reflective discovery of HttpEndpointAttribute -decorated methods on a given type. Used at startup by the framework to enumerate the typed HTTP surface of an app class and of every registered cell type.
  static class HttpEndpointDiscovery
    // Discover every HttpEndpointAttribute -decorated public instance method on ownerType . Methods inherited from base classes are included; static methods and non-public methods are skipped (endpoints must be invokable on a specific instance).
    static IReadOnlyList<HttpEndpointInfo> ForType(Type ownerType)
    // Discover endpoints across every type in types . Convenience overload for the startup path that has already filtered an assembly's loaded types down to apps and cells.
    static IReadOnlyList<HttpEndpointInfo> ForTypes(IEnumerable<Type> types)
  // Transport envelope serialized by the app-side wrapper, deserialized by IkonServer back into an HttpDispatchResult. Public because the JSON contract crosses the protocol boundary, but the type itself is internal-style — no apps reference it directly.
  sealed class HttpEndpointEnvelope : IEquatable<HttpEndpointEnvelope>
    // Transport envelope serialized by the app-side wrapper, deserialized by IkonServer back into an HttpDispatchResult. Public because the JSON contract crosses the protocol boundary, but the type itself is internal-style — no apps reference it directly.
    ctor(int StatusCode, string? Body, string ContentType)
    string? Body { get; init; }
    string ContentType { get; init; }
    int StatusCode { get; init; }
  // Metadata for a single HttpEndpointAttribute -annotated method discovered at startup. Carries everything the dispatcher needs at request time: the HTTP method, path template, auth handler type, the reflected MethodInfo , and the owner Type (an app class or a [Cell] class).
  sealed class HttpEndpointInfo : IEquatable<HttpEndpointInfo>
    // Metadata for a single HttpEndpointAttribute -annotated method discovered at startup. Carries everything the dispatcher needs at request time: the HTTP method, path template, auth handler type, the reflected MethodInfo , and the owner Type (an app class or a [Cell] class).
    ctor(string Method, string Path, Type? Auth, bool Absolute, MethodInfo Handler, Type OwnerType)
    bool Absolute { get; init; }
    Type? Auth { get; init; }
    MethodInfo Handler { get; init; }
    string Method { get; init; }
    Type OwnerType { get; init; }
    string Path { get; init; }
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
  // Converts an McpToolInfo (a discovered McpToolAttribute -annotated cell method) into an McpToolHandler that Ikon.Mcp.McpHost can register. The handler resolves the cell instance via CellHost , deserialises method parameters from the incoming JSON-RPC arguments object, invokes the method, awaits a possible Task`1 / ValueTask`1 , and normalises the return value to a string MCP can ship as a "text" tool content. Two binding modes, picked by signature shape: Record mode — the method has exactly one parameter whose type serialises as a JSON object (a record, POCO, dictionary, or JsonElement ). The MCP inputSchema is the record's schema, derived top-level via JsonSchemaExporter . The whole arguments object is deserialised into that single parameter — no wrapper property name.Named mode — anything else (multiple parameters, or a single primitive parameter). Each parameter becomes a top-level property of the schema; at call time the bridge binds by parameter name. Authors don't write JSON schema strings — the C# signature is the schema.
  static class McpToolBridge
    static McpToolHandler BuildHandler(CellHost cellHost, McpToolInfo info)
  // Reflective discovery of McpToolAttribute -decorated methods on a cell type. Used at startup by the framework to enumerate the MCP-exposed surface of every registered cell type. Mirrors HttpEndpointDiscovery .
  static class McpToolDiscovery
    // Discover every McpToolAttribute -decorated public instance method on ownerType . Methods inherited from base classes are included; static methods and non-public methods are skipped (tools must be invokable on a specific cell instance).
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
  // Metadata for a single McpToolAttribute -annotated method discovered at startup. Carries everything the bridge needs at request time: the MCP-wire name, description, the reflected MethodInfo , and the owner cell Type .
  sealed class McpToolInfo : IEquatable<McpToolInfo>
    // Metadata for a single McpToolAttribute -annotated method discovered at startup. Carries everything the bridge needs at request time: the MCP-wire name, description, the reflected MethodInfo , and the owner cell Type .
    ctor(string Name, string Description, MethodInfo Handler, Type OwnerCellType)
    string Description { get; init; }
    MethodInfo Handler { get; init; }
    string Name { get; init; }
    Type OwnerCellType { get; init; }
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


---

# Ikon.Common Public API

namespace Ikon.Common
  enum FileScanner.FileEventArgs.ActionType
    Added
    Changed
    Deleted
  class AppProjectConfig.ActivationConfig : ITomlMetadataProvider
    ctor()
    bool StopSessions { get; set; }
  class AppBundleConfig.ActivationConfig
    ctor()
    bool StopSessions { get; set; }
  class AppBundleConfig
    ctor()
    AppBundleConfig.ActivationConfig Activation { get; set; }
    AppBundleConfig.AuthConfig Auth { get; set; }
    // Per-cell entry points discovered in the bundle. The cloud uses this list to: (a) recognise URLs of the form /api/{CellType}/{path} and route them to a cell-instance (provisioned the same way an app-instance is, just with AppInitializationArgs.RunTarget = "{CellType}" instead of null); (b) hash the request's identity query params against the cell's IdentityFields to find-or-create the right channel-instance for that cell identity. Empty for apps without cells, or for cells whose ProcessScope is AppProcess (in-process — no separate cell-instance needed). See docs/private/cell-substrate-and-unified-http.md.
    List<AppBundleConfig.CellEntry> Cells { get; set; }
    string ChannelId { get; set; }
    string CreatedAt { get; set; }
    List<AppBundleConfig.DatabaseEntry> Databases { get; set; }
    List<AppBundleConfig.EmailTemplate> EmailTemplates { get; set; }
    string Hash { get; set; }
    string Name { get; set; }
    string OrganisationId { get; set; }
    List<AppBundleConfig.Pipeline> Pipelines { get; set; }
    List<string> SessionIdentityKeys { get; set; }
    string SpaceId { get; set; }
    string Version { get; set; }
    List<AppBundleConfig.WebhookFunction> Webhooks { get; set; }
    static string ConfigFileName
  class AppBundleConfigLegacy
    ctor()
    string AppTypeName { get; set; }
    string CreatedAt { get; set; }
    string DllName { get; set; }
    string Hash { get; set; }
    string Version { get; set; }
    static string ConfigFileName
  class AppBundleRuntimeConfig
    ctor()
    string AppTypeName { get; set; }
    string DllName { get; set; }
    static string ConfigFileName
  sealed class AppProjectUtils.AppDiscoveryResult
    ctor()
    bool Found { get; init; }
    string? TypeName { get; init; }
  class AppProjectConfig : ITomlMetadataProvider
    ctor()
    AppProjectConfig.ActivationConfig Activation { get; set; }
    AppProjectConfig.AuthConfig Auth { get; set; }
    List<string> Databases { get; set; }
    AppProjectConfig.TargetConfig Target { get; set; }
    static AppProjectConfig FromToml(string tomlContent)
    static string GetConfigFileName(IkonBackend.EnvironmentType environment)
    static string GetConfigFileName(string targetName)
    static string ConfigFileName
  class AppProjectConfigLegacy : ITomlMetadataProvider
    ctor()
    AppProjectConfig.ActivationConfig Activation { get; set; }
    AppProjectConfig.AuthConfig Auth { get; set; }
    List<string> Databases { get; set; }
    Dictionary<string, AppProjectConfigLegacy.Target> Targets { get; set; }
  static class AppProjectUtils
    static IEnumerable<string> EnumerateCsprojFiles(string rootDirectory, int maxDepth = 3)
    static AppProjectUtils.AppDiscoveryResult FindAppTypeInAssembly(string dllPath)
    static string FindBestProjectFilePath(string targetDirectory)
    // Finds the platform-dotnet directory by searching for ikon-platform.slnx. At each ancestor of the start directory, checks two containment candidates: 1. current/ikon-platform.slnx (current is platform-dotnet itself) 2. current/platform-dotnet/ikon-platform.slnx (current is the repo root) With includeSibling = true, also checks a third candidate: 3. current/ikon-platform/platform-dotnet/ikon-platform.slnx (current is a parent of a sibling ikon-platform/ checkout) Sibling matching is opt-in because it answers a different question — "is there a platform-dotnet anywhere nearby?" rather than "is startDirectory inside platform-dotnet?". Callers that decide whether to mutate the platform repo (e.g. add a new app to ikon-platform.slnx) must keep it off; the codegen lookup for external apps with a sibling checkout opts in.
    static string? FindIkonPlatformDotnetRoot(string startDirectory, bool includeSibling = false)
    // Finds the ikon-platform repo root (the parent of platform-dotnet).
    static string? FindIkonPlatformRepoRoot(string startDirectory)
    // Finds the ikon-platform.slnx file path by searching in the start directory and parent directories.
    static string? FindIkonPlatformSlnx(string startDirectory)
    // Generates tsconfig.paths.json in the frontend-node directory with appropriate TypeScript paths. Auto-detects internal/external mode based on whether the directory is inside ikon-platform. Internal mode: generates paths pointing to monorepo source files for IDE support. External mode: generates empty paths (uses node_modules).
    static Task GenerateTsconfigPathsJsonAsync(string frontendNodeDirectory)
    static AppProjectVariables GetAppProjectVars(string? targetDirectory)
    static string GetAssemblyNameFromCsproj(string csprojPath)
    static Type[] GetTypesSafely(Assembly assembly)
    static bool HasIkonAppAttribute(Type type)
    // Returns true if the given directory is inside the ikon-platform monorepo.
    static bool IsInsideIkonPlatform(string directory)
    static bool IsLegacyConfig(string tomlContent)
    static AppProjectConfig MigrateFromLegacy(string tomlContent, IkonBackend.EnvironmentType environment)
    static int ScoreProjectFile(string csprojPath)
    static string StripIkonAppPrefix(string projectName)
  sealed class AppProjectVariables
    ctor()
    string ConfigFilePath { get; init; }
    string CsProjectFilePath { get; init; }
    string CsProjectName { get; init; }
    string FrontendNodeDirectory { get; init; }
    string GitRootDirectory { get; init; }
    string ProjectDirectory { get; init; }
    string ProjectName { get; init; }
    string RelativeConfigFilePath { get; init; }
    string RelativeCsProjectFilePath { get; init; }
    string RelativeRootDirectory { get; init; }
    string RootDirectory { get; init; }
    string TargetDirectory { get; init; }
  class AsyncLocalInstances
    void Capture(object owner, bool allowOverride = false)
    void InitializeAll()
    void Remove(object owner)
    void Restore(object owner)
    bool TryRestore(object owner)
    static AsyncLocalInstances Instance
  class AppProjectConfig.AuthConfig : ITomlMetadataProvider
    ctor()
    List<string> DomainAllowlist { get; set; }
    bool Enabled { get; set; }
    List<string> Methods { get; set; }
  class AppBundleConfig.AuthConfig
    ctor()
    List<string> DomainAllowlist { get; set; }
    bool Enabled { get; set; }
    List<string> Methods { get; set; }
  class AppBundleConfig.CellEntry
    ctor()
    string DllName { get; set; }
    string FullTypeName { get; set; }
    List<AppBundleConfig.CellHttpEndpoint> HttpEndpoints { get; set; }
    List<AppBundleConfig.CellIdentityField> IdentityFields { get; set; }
    List<AppBundleConfig.CellMcpTool> McpTools { get; set; }
    string ProcessScope { get; set; }
    string TypeName { get; set; }
  class AppBundleConfig.CellHttpEndpoint
    ctor()
    string AuthCellName { get; set; }
    string HttpMethod { get; set; }
    string MethodName { get; set; }
    string Path { get; set; }
  class AppBundleConfig.CellIdentityField
    ctor()
    bool HasDefault { get; set; }
    string Name { get; set; }
    string TypeName { get; set; }
  class AppBundleConfig.CellMcpTool
    ctor()
    string Description { get; set; }
    string MethodName { get; set; }
    string ToolName { get; set; }
  struct CertificateStore.Certificate
    ctor(X509Certificate2 cert, X509Certificate2? rootCert, string certHash, string spkiHash, bool isDotnetDevCert)
    X509Certificate2 Cert { get; }
    string CertHash { get; }
    bool IsDotnetDevCert { get; }
    X509Certificate2? RootCert { get; }
    string SpkiHash { get; }
  static class CertificateStore
    static CertificateStore.Certificate GetCertificate(string host, X509Certificate2? rootCert = null, bool disableDotnetDevCerts = false)
  sealed class DatabaseConnectionInfo
    ctor()
    string ConnectionString { get; set; }
    string Name { get; set; }
    string Type { get; set; }
  class AppBundleConfig.DatabaseEntry
    ctor()
    string Name { get; set; }
    string Tier { get; set; }
    string Type { get; set; }
  class DescriptionAttribute : Attribute
    ctor(string description, object? example = null, RequiredStatus isRequired = Default, int minArrayItems = 0)
    string Description { get; }
    object? Example { get; }
    RequiredStatus IsRequired { get; }
    int MinArrayItems { get; }
  class AppBundleConfig.EmailTemplate
    ctor()
    string Name { get; set; }
    string Path { get; set; }
    string Subject { get; set; }
  enum EndpointProtocol
    Tcp
    Tls
    Udp
  class ExponentialMovingAverage
    ctor(double smoothingFactor = 0.1)
    double CurrentValue { get; }
    double Update(double value)
    double UpdatePerElapsedTime(double value)
  class FileScanner.FileEventArgs : EventArgs
    ctor(string absolutePath, string relativePath, FileScanner.FileEventArgs.ActionType action)
    string AbsolutePath { get; }
    FileScanner.FileEventArgs.ActionType Action { get; }
    string RelativePath { get; }
    override string ToString()
  class FileScanner : IAsyncDisposable, IStorage
    ctor()
    int QueueSize { get; }
    Task DeleteAsync(AssetUri assetUri)
    ValueTask DisposeAsync()
    Task<bool> ExistsAsync(AssetUri assetUri)
    List<string> GetPaths(string link)
    Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata, CancellationToken cancellationToken)
    Task InitializeAsync(string name, string dataDirectory, bool scanZipFiles = true, bool start = true)
    Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken)
    Stream OpenStream(string absoluteOrRelativePath)
    Task<string> ReadAllTextAsync(string absoluteOrRelativePath)
    Task StartAsync()
    Task StopAsync()
    Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    Task WaitUntilQueueEmptyAsync()
    event Func<AssetEventArgs, Task> AssetEventAsync
    event Func<object, FileScanner.FileEventArgs, Task>? FileEvent
  class GenericListCache<T> : IGenericListCache<T>
    ctor()
    void Add(string hash, T item)
    void Clear()
    void Delete(string hash)
    List<string> DeleteOlderThan(TimeSpan timeSpan)
    List<T>? Get(string hash)
  static class HashUtils
    static byte[] ComputeMD5Bytes(byte[] value)
    static byte[] ComputeMD5Bytes(string value)
    static string ComputeMD5FromFile(string path)
    static string ComputeMD5FromStream(Stream stream)
    static string ComputeMD5String(byte[] value)
    static string ComputeMD5String(string value)
    static byte[] ComputeSHA256Bytes(byte[] value)
    static string ComputeSHA256FromFile(string path)
    static string ComputeSHA256FromStream(Stream stream)
    static string ComputeSHA256String(byte[] value)
    static string ComputeSHA256String(string value)
    static string ToHexString(byte[] data)
  interface IGenericListCache<T>
    abstract void Add(string hash, T item)
    abstract void Clear()
    abstract void Delete(string hash)
    abstract List<string> DeleteOlderThan(TimeSpan timeSpan)
    abstract List<T>? Get(string hash)
  sealed class IkonLoggerProvider : IDisposable, ILoggerProvider
    ctor()
    ILogger CreateLogger(string categoryName)
    void Dispose()
  class IkonProjectConfigLegacy
    ctor()
    string ChannelId { get; set; }
    string OrganisationId { get; set; }
    string ProjectId { get; set; }
    string SpaceId { get; set; }
    static string ConfigFileName
  class IkonProjectConfigLegacyPerEnv
    ctor()
    Dictionary<string, IkonProjectConfigLegacy> Environments { get; set; }
  static class IkonTaskExtensions
    // Intentionally does not await the task. Exceptions are observed and sent to onException .
    static void RunParallel(Task task, Action<Exception>? onException = null)
  sealed class InMemoryProtocolMessageChannel : IProtocolMessageChannel
    ctor()
    Context ClientContext { get; }
    static ValueTuple<InMemoryProtocolMessageChannel, InMemoryProtocolMessageChannel> CreateConnectedPair()
    IDisposable RegisterMessageHandler(Func<ProtocolMessage, ValueTask> handler, Opcode? opcodeGroupMask = null, Opcode[]? opcodes = null)
    ValueTask SendMessageAsync(ProtocolMessage message)
    ValueTask SendMessageAsync(IProtocolMessagePayload payload)
  sealed class AppProjectUtils.IsolatedLoadContext : AssemblyLoadContext, IDisposable
    ctor(string mainAssemblyPath)
    void Dispose()
  static class Markdown
    static string To<T>(T obj, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, int maxDepth = 5)
  static class MimeTypes
    static void AddOrUpdate(string mime, string extension)
    static string GetExtensionFromMimeType(string mimeType)
    static string GetMimeTypeFromExtension(string extension)
    static string GetMimeTypeFromFilename(string fileName)
    static bool Is(string mimeType, string mimeTypeToCompare)
    static bool IsAudio(string mimeType)
    static bool IsBinary(string mimeType)
    static bool IsCsv(string mimeType)
    static bool IsImage(string mimeType)
    static bool IsJson(string mimeType)
    static bool IsMarkdown(string mimeType)
    static bool IsMicrosoftExcel(string mimeType)
    static bool IsMicrosoftPowerpoint(string mimeType)
    static bool IsMicrosoftWord(string mimeType)
    static bool IsNotes(string mimeType)
    static bool IsPdf(string mimeType)
    static bool IsText(string mimeType)
    static bool IsVideo(string mimeType)
    static bool IsXml(string mimeType)
    static bool IsZip(string mimeType)
    static bool TypeMatchesMimetype(string type, string mimeType)
    static string ApplicationAndrewInset
    static string ApplicationApplixware
    static string ApplicationAtomXml
    static string ApplicationAtomcatXml
    static string ApplicationAtomsvcXml
    static string ApplicationCcxmlXml
    static string ApplicationCdmiCapability
    static string ApplicationCdmiContainer
    static string ApplicationCdmiDomain
    static string ApplicationCdmiObject
    static string ApplicationCdmiQueue
    static string ApplicationCuSeeme
    static string ApplicationDavmountXml
    static string ApplicationDocbookXml
    static string ApplicationDsscDer
    static string ApplicationDsscXml
    static string ApplicationEcmascript
    static string ApplicationEmmaXml
    static string ApplicationEpubZip
    static string ApplicationExcel
    static string ApplicationExi
    static string ApplicationFontTdpfr
    static string ApplicationGmlXml
    static string ApplicationGpxXml
    static string ApplicationGxf
    static string ApplicationHyperstudio
    static string ApplicationInkmlXml
    static string ApplicationIpfix
    static string ApplicationJavaArchive
    static string ApplicationJavaSerializedObject
    static string ApplicationJavaVm
    static string ApplicationJavascript
    static string ApplicationJson
    static string ApplicationJsonmlJson
    static string ApplicationLostXml
    static string ApplicationMacBinhex40
    static string ApplicationMacCompactpro
    static string ApplicationMadsXml
    static string ApplicationMarc
    static string ApplicationMarcxmlXml
    static string ApplicationMathematica
    static string ApplicationMathmlXml
    static string ApplicationMbox
    static string ApplicationMediaservercontrolXml
    static string ApplicationMetalink4Xml
    static string ApplicationMetalinkXml
    static string ApplicationMetsXml
    static string ApplicationModsXml
    static string ApplicationMp21
    static string ApplicationMp4
    static string ApplicationMsword
    static string ApplicationMxf
    static string ApplicationOctetStream
    static string ApplicationOda
    static string ApplicationOebpsPackageXml
    static string ApplicationOgg
    static string ApplicationOmdocXml
    static string ApplicationOnenote
    static string ApplicationOxps
    static string ApplicationPatchOpsErrorXml
    static string ApplicationPdf
    static string ApplicationPgpEncrypted
    static string ApplicationPgpSignature
    static string ApplicationPicsRules
    static string ApplicationPkcs10
    static string ApplicationPkcs7Mime
    static string ApplicationPkcs7Signature
    static string ApplicationPkcs8
    static string ApplicationPkixAttrCert
    static string ApplicationPkixCert
    static string ApplicationPkixCrl
    static string ApplicationPkixPkipath
    static string ApplicationPkixcmp
    static string ApplicationPlsXml
    static string ApplicationPostscript
    static string ApplicationPrsCww
    static string ApplicationPskcXml
    static string ApplicationRdfXml
    static string ApplicationReginfoXml
    static string ApplicationRelaxNgCompactSyntax
    static string ApplicationResourceListsDiffXml
    static string ApplicationResourceListsXml
    static string ApplicationRlsServicesXml
    static string ApplicationRpkiGhostbusters
    static string ApplicationRpkiManifest
    static string ApplicationRpkiRoa
    static string ApplicationRsdXml
    static string ApplicationRssXml
    static string ApplicationRtf
    static string ApplicationSbmlXml
    static string ApplicationScvpCvRequest
    static string ApplicationScvpCvResponse
    static string ApplicationScvpVpRequest
    static string ApplicationScvpVpResponse
    static string ApplicationSdp
    static string ApplicationSetPaymentInitiation
    static string ApplicationSetRegistrationInitiation
    static string ApplicationShfXml
    static string ApplicationSmilXml
    static string ApplicationSparqlQuery
    static string ApplicationSparqlResultsXml
    static string ApplicationSql
    static string ApplicationSrgs
    static string ApplicationSrgsXml
    static string ApplicationSruXml
    static string ApplicationSsdlXml
    static string ApplicationSsmlXml
    static string ApplicationTeiXml
    static string ApplicationThraudXml
    static string ApplicationTimestampedData
    static string ApplicationVnd3gpp2Tcap
    static string ApplicationVnd3gppPicBwLarge
    static string ApplicationVnd3gppPicBwSmall
    static string ApplicationVnd3gppPicBwVar
    static string ApplicationVnd3mPostItNotes
    static string ApplicationVndAccpacSimplyAso
    static string ApplicationVndAccpacSimplyImp
    static string ApplicationVndAcucobol
    static string ApplicationVndAcucorp
    static string ApplicationVndAdobeAirApplicationInstallerPackageZip
    static string ApplicationVndAdobeFormscentralFcdt
    static string ApplicationVndAdobeFxp
    static string ApplicationVndAdobeXdpXml
    static string ApplicationVndAdobeXfdf
    static string ApplicationVndAheadSpace
    static string ApplicationVndAirzipFilesecureAzf
    static string ApplicationVndAirzipFilesecureAzs
    static string ApplicationVndAmazonEbook
    static string ApplicationVndAmericandynamicsAcc
    static string ApplicationVndAmigaAmi
    static string ApplicationVndAndroidPackageArchive
    static string ApplicationVndAnserWebCertificateIssueInitiation
    static string ApplicationVndAnserWebFundsTransferInitiation
    static string ApplicationVndAntixGameComponent
    static string ApplicationVndAppleInstallerXml
    static string ApplicationVndAppleMpegurl
    static string ApplicationVndAristanetworksSwi
    static string ApplicationVndAstraeaSoftwareIota
    static string ApplicationVndAudiograph
    static string ApplicationVndBlueiceMultipass
    static string ApplicationVndBmi
    static string ApplicationVndBusinessobjects
    static string ApplicationVndChemdrawXml
    static string ApplicationVndChipnutsKaraokeMmd
    static string ApplicationVndCinderella
    static string ApplicationVndClaymore
    static string ApplicationVndCloantoRp9
    static string ApplicationVndClonkC4group
    static string ApplicationVndCluetrustCartomobileConfig
    static string ApplicationVndCluetrustCartomobileConfigPkg
    static string ApplicationVndCommonspace
    static string ApplicationVndContactCmsg
    static string ApplicationVndCosmocaller
    static string ApplicationVndCrickClicker
    static string ApplicationVndCrickClickerKeyboard
    static string ApplicationVndCrickClickerPalette
    static string ApplicationVndCrickClickerTemplate
    static string ApplicationVndCrickClickerWordbank
    static string ApplicationVndCriticaltoolsWbsXml
    static string ApplicationVndCtcPosml
    static string ApplicationVndCupsPpd
    static string ApplicationVndCurlCar
    static string ApplicationVndCurlPcurl
    static string ApplicationVndDart
    static string ApplicationVndDataVisionRdz
    static string ApplicationVndDeceData
    static string ApplicationVndDeceTtmlXml
    static string ApplicationVndDeceUnspecified
    static string ApplicationVndDeceZip
    static string ApplicationVndDenovoFcselayoutLink
    static string ApplicationVndDna
    static string ApplicationVndDolbyMlp
    static string ApplicationVndDpgraph
    static string ApplicationVndDreamfactory
    static string ApplicationVndDsKeypoint
    static string ApplicationVndDvbAit
    static string ApplicationVndDvbService
    static string ApplicationVndDynageo
    static string ApplicationVndEcowinChart
    static string ApplicationVndEnliven
    static string ApplicationVndEpsonEsf
    static string ApplicationVndEpsonMsf
    static string ApplicationVndEpsonQuickanime
    static string ApplicationVndEpsonSalt
    static string ApplicationVndEpsonSsf
    static string ApplicationVndEszigno3Xml
    static string ApplicationVndEzpixAlbum
    static string ApplicationVndEzpixPackage
    static string ApplicationVndFdf
    static string ApplicationVndFdsnMseed
    static string ApplicationVndFdsnSeed
    static string ApplicationVndFlographit
    static string ApplicationVndFluxtimeClip
    static string ApplicationVndFramemaker
    static string ApplicationVndFrogansFnc
    static string ApplicationVndFrogansLtf
    static string ApplicationVndFscWeblaunch
    static string ApplicationVndFujitsuOasys
    static string ApplicationVndFujitsuOasys2
    static string ApplicationVndFujitsuOasys3
    static string ApplicationVndFujitsuOasysgp
    static string ApplicationVndFujitsuOasysprs
    static string ApplicationVndFujixeroxDdd
    static string ApplicationVndFujixeroxDocuworks
    static string ApplicationVndFujixeroxDocuworksBinder
    static string ApplicationVndFuzzysheet
    static string ApplicationVndGenomatixTuxedo
    static string ApplicationVndGeogebraFile
    static string ApplicationVndGeogebraTool
    static string ApplicationVndGeometryExplorer
    static string ApplicationVndGeonext
    static string ApplicationVndGeoplan
    static string ApplicationVndGeospace
    static string ApplicationVndGmx
    static string ApplicationVndGoogleEarthKmlXml
    static string ApplicationVndGoogleEarthKmz
    static string ApplicationVndGrafeq
    static string ApplicationVndGrooveAccount
    static string ApplicationVndGrooveHelp
    static string ApplicationVndGrooveIdentityMessage
    static string ApplicationVndGrooveInjector
    static string ApplicationVndGrooveToolMessage
    static string ApplicationVndGrooveToolTemplate
    static string ApplicationVndGrooveVcard
    static string ApplicationVndHalXml
    static string ApplicationVndHandheldEntertainmentXml
    static string ApplicationVndHbci
    static string ApplicationVndHheLessonPlayer
    static string ApplicationVndHpHpgl
    static string ApplicationVndHpHpid
    static string ApplicationVndHpHps
    static string ApplicationVndHpJlyt
    static string ApplicationVndHpPcl
    static string ApplicationVndHpPclxl
    static string ApplicationVndHydrostatixSofData
    static string ApplicationVndIbmMinipay
    static string ApplicationVndIbmModcap
    static string ApplicationVndIbmRightsManagement
    static string ApplicationVndIbmSecureContainer
    static string ApplicationVndIccprofile
    static string ApplicationVndIgloader
    static string ApplicationVndImmervisionIvp
    static string ApplicationVndImmervisionIvu
    static string ApplicationVndInsorsIgm
    static string ApplicationVndInterconFormnet
    static string ApplicationVndIntergeo
    static string ApplicationVndIntuQbo
    static string ApplicationVndIntuQfx
    static string ApplicationVndIpunpluggedRcprofile
    static string ApplicationVndIrepositoryPackageXml
    static string ApplicationVndIsXpr
    static string ApplicationVndIsacFcs
    static string ApplicationVndJam
    static string ApplicationVndJcpJavameMidletRms
    static string ApplicationVndJisp
    static string ApplicationVndJoostJodaArchive
    static string ApplicationVndKahootz
    static string ApplicationVndKdeKarbon
    static string ApplicationVndKdeKchart
    static string ApplicationVndKdeKformula
    static string ApplicationVndKdeKivio
    static string ApplicationVndKdeKontour
    static string ApplicationVndKdeKpresenter
    static string ApplicationVndKdeKspread
    static string ApplicationVndKdeKword
    static string ApplicationVndKenameaapp
    static string ApplicationVndKidspiration
    static string ApplicationVndKinar
    static string ApplicationVndKoan
    static string ApplicationVndKodakDescriptor
    static string ApplicationVndLasLasXml
    static string ApplicationVndLlamagraphicsLifeBalanceDesktop
    static string ApplicationVndLlamagraphicsLifeBalanceExchangeXml
    static string ApplicationVndLotus123
    static string ApplicationVndLotusApproach
    static string ApplicationVndLotusFreelance
    static string ApplicationVndLotusNotes
    static string ApplicationVndLotusOrganizer
    static string ApplicationVndLotusScreencam
    static string ApplicationVndLotusWordpro
    static string ApplicationVndMacportsPortpkg
    static string ApplicationVndMcd
    static string ApplicationVndMedcalcdata
    static string ApplicationVndMediastationCdkey
    static string ApplicationVndMfer
    static string ApplicationVndMfmp
    static string ApplicationVndMicrografxFlo
    static string ApplicationVndMicrografxIgx
    static string ApplicationVndMif
    static string ApplicationVndMobiusDaf
    static string ApplicationVndMobiusDis
    static string ApplicationVndMobiusMbk
    static string ApplicationVndMobiusMqy
    static string ApplicationVndMobiusMsl
    static string ApplicationVndMobiusPlc
    static string ApplicationVndMobiusTxf
    static string ApplicationVndMophunApplication
    static string ApplicationVndMophunCertificate
    static string ApplicationVndMozillaXulXml
    static string ApplicationVndMsArtgalry
    static string ApplicationVndMsCabCompressed
    static string ApplicationVndMsExcel
    static string ApplicationVndMsExcelAddinMacroenabled12
    static string ApplicationVndMsExcelSheetBinaryMacroenabled12
    static string ApplicationVndMsExcelSheetMacroenabled12
    static string ApplicationVndMsExcelTemplateMacroenabled12
    static string ApplicationVndMsFontobject
    static string ApplicationVndMsHtmlhelp
    static string ApplicationVndMsIms
    static string ApplicationVndMsLrm
    static string ApplicationVndMsOfficetheme
    static string ApplicationVndMsPkiSeccat
    static string ApplicationVndMsPkiStl
    static string ApplicationVndMsPowerpoint
    static string ApplicationVndMsPowerpointAddinMacroenabled12
    static string ApplicationVndMsPowerpointPresentationMacroenabled12
    static string ApplicationVndMsPowerpointSlideMacroenabled12
    static string ApplicationVndMsPowerpointSlideshowMacroenabled12
    static string ApplicationVndMsPowerpointTemplateMacroenabled12
    static string ApplicationVndMsProject
    static string ApplicationVndMsWordDocumentMacroenabled12
    static string ApplicationVndMsWordTemplateMacroenabled12
    static string ApplicationVndMsWorks
    static string ApplicationVndMsWpl
    static string ApplicationVndMsXpsdocument
    static string ApplicationVndMseq
    static string ApplicationVndMusician
    static string ApplicationVndMuveeStyle
    static string ApplicationVndMynfc
    static string ApplicationVndNeurolanguageNlu
    static string ApplicationVndNitf
    static string ApplicationVndNoblenetDirectory
    static string ApplicationVndNoblenetSealer
    static string ApplicationVndNoblenetWeb
    static string ApplicationVndNokiaNGageData
    static string ApplicationVndNokiaNGageSymbianInstall
    static string ApplicationVndNokiaRadioPreset
    static string ApplicationVndNokiaRadioPresets
    static string ApplicationVndNovadigmEdm
    static string ApplicationVndNovadigmEdx
    static string ApplicationVndNovadigmExt
    static string ApplicationVndOasisOpendocumentChart
    static string ApplicationVndOasisOpendocumentChartTemplate
    static string ApplicationVndOasisOpendocumentDatabase
    static string ApplicationVndOasisOpendocumentFormula
    static string ApplicationVndOasisOpendocumentFormulaTemplate
    static string ApplicationVndOasisOpendocumentGraphics
    static string ApplicationVndOasisOpendocumentGraphicsTemplate
    static string ApplicationVndOasisOpendocumentImage
    static string ApplicationVndOasisOpendocumentImageTemplate
    static string ApplicationVndOasisOpendocumentPresentation
    static string ApplicationVndOasisOpendocumentPresentationTemplate
    static string ApplicationVndOasisOpendocumentSpreadsheet
    static string ApplicationVndOasisOpendocumentSpreadsheetTemplate
    static string ApplicationVndOasisOpendocumentText
    static string ApplicationVndOasisOpendocumentTextMaster
    static string ApplicationVndOasisOpendocumentTextTemplate
    static string ApplicationVndOasisOpendocumentTextWeb
    static string ApplicationVndOlpcSugar
    static string ApplicationVndOmaDd2Xml
    static string ApplicationVndOpenofficeorgExtension
    static string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlPresentation
    static string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlSlide
    static string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlSlideshow
    static string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlTemplate
    static string ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet
    static string ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlTemplate
    static string ApplicationVndOpenxmlformatsOfficedocumentWordprocessingmlDocument
    static string ApplicationVndOpenxmlformatsOfficedocumentWordprocessingmlTemplate
    static string ApplicationVndOsgeoMapguidePackage
    static string ApplicationVndOsgiDp
    static string ApplicationVndOsgiSubsystem
    static string ApplicationVndPalm
    static string ApplicationVndPawaafile
    static string ApplicationVndPgFormat
    static string ApplicationVndPgOsasli
    static string ApplicationVndPicsel
    static string ApplicationVndPmiWidget
    static string ApplicationVndPocketlearn
    static string ApplicationVndPowerbuilder6
    static string ApplicationVndPreviewsystemsBox
    static string ApplicationVndProteusMagazine
    static string ApplicationVndPublishareDeltaTree
    static string ApplicationVndPviPtid1
    static string ApplicationVndQuarkQuarkxpress
    static string ApplicationVndRealvncBed
    static string ApplicationVndRecordareMusicxml
    static string ApplicationVndRecordareMusicxmlXml
    static string ApplicationVndRigCryptonote
    static string ApplicationVndRimCod
    static string ApplicationVndRnRealmedia
    static string ApplicationVndRnRealmediaVbr
    static string ApplicationVndRoute66Link66Xml
    static string ApplicationVndSailingtrackerTrack
    static string ApplicationVndSeemail
    static string ApplicationVndSema
    static string ApplicationVndSemd
    static string ApplicationVndSemf
    static string ApplicationVndShanaInformedFormdata
    static string ApplicationVndShanaInformedFormtemplate
    static string ApplicationVndShanaInformedInterchange
    static string ApplicationVndShanaInformedPackage
    static string ApplicationVndSimtechMindmapper
    static string ApplicationVndSmaf
    static string ApplicationVndSmartTeacher
    static string ApplicationVndSolentSdkmXml
    static string ApplicationVndSpotfireDxp
    static string ApplicationVndSpotfireSfs
    static string ApplicationVndStardivisionCalc
    static string ApplicationVndStardivisionDraw
    static string ApplicationVndStardivisionImpress
    static string ApplicationVndStardivisionMath
    static string ApplicationVndStardivisionWriter
    static string ApplicationVndStardivisionWriterGlobal
    static string ApplicationVndStepmaniaPackage
    static string ApplicationVndStepmaniaStepchart
    static string ApplicationVndSunXmlCalc
    static string ApplicationVndSunXmlCalcTemplate
    static string ApplicationVndSunXmlDraw
    static string ApplicationVndSunXmlDrawTemplate
    static string ApplicationVndSunXmlImpress
    static string ApplicationVndSunXmlImpressTemplate
    static string ApplicationVndSunXmlMath
    static string ApplicationVndSunXmlWriter
    static string ApplicationVndSunXmlWriterGlobal
    static string ApplicationVndSunXmlWriterTemplate
    static string ApplicationVndSusCalendar
    static string ApplicationVndSvd
    static string ApplicationVndSymbianInstall
    static string ApplicationVndSyncmlDmWbxml
    static string ApplicationVndSyncmlDmXml
    static string ApplicationVndSyncmlXml
    static string ApplicationVndTaoIntentModuleArchive
    static string ApplicationVndTcpdumpPcap
    static string ApplicationVndTmobileLivetv
    static string ApplicationVndTridTpt
    static string ApplicationVndTriscapeMxs
    static string ApplicationVndTrueapp
    static string ApplicationVndUfdl
    static string ApplicationVndUiqTheme
    static string ApplicationVndUmajin
    static string ApplicationVndUnity
    static string ApplicationVndUomlXml
    static string ApplicationVndVcx
    static string ApplicationVndVisio
    static string ApplicationVndVisionary
    static string ApplicationVndVsf
    static string ApplicationVndWapWbxml
    static string ApplicationVndWapWmlc
    static string ApplicationVndWapWmlscriptc
    static string ApplicationVndWebturbo
    static string ApplicationVndWolframPlayer
    static string ApplicationVndWordperfect
    static string ApplicationVndWqd
    static string ApplicationVndWtStf
    static string ApplicationVndXara
    static string ApplicationVndXfdl
    static string ApplicationVndYamahaHvDic
    static string ApplicationVndYamahaHvScript
    static string ApplicationVndYamahaHvVoice
    static string ApplicationVndYamahaOpenscoreformat
    static string ApplicationVndYamahaOpenscoreformatOsfpvgXml
    static string ApplicationVndYamahaSmafAudio
    static string ApplicationVndYamahaSmafPhrase
    static string ApplicationVndYellowriverCustomMenu
    static string ApplicationVndZul
    static string ApplicationVndZzazzDeckXml
    static string ApplicationVoicexmlXml
    static string ApplicationWidget
    static string ApplicationWinhlp
    static string ApplicationWsdlXml
    static string ApplicationWspolicyXml
    static string ApplicationX7zCompressed
    static string ApplicationXAbiword
    static string ApplicationXAceCompressed
    static string ApplicationXAppleDiskimage
    static string ApplicationXAuthorwareBin
    static string ApplicationXAuthorwareMap
    static string ApplicationXAuthorwareSeg
    static string ApplicationXBcpio
    static string ApplicationXBittorrent
    static string ApplicationXBlorb
    static string ApplicationXBzip
    static string ApplicationXBzip2
    static string ApplicationXCbr
    static string ApplicationXCdlink
    static string ApplicationXCfsCompressed
    static string ApplicationXChat
    static string ApplicationXChessPgn
    static string ApplicationXConference
    static string ApplicationXCpio
    static string ApplicationXCsh
    static string ApplicationXDebianPackage
    static string ApplicationXDgcCompressed
    static string ApplicationXDirector
    static string ApplicationXDoom
    static string ApplicationXDtbncxXml
    static string ApplicationXDtbookXml
    static string ApplicationXDtbresourceXml
    static string ApplicationXDvi
    static string ApplicationXEnvoy
    static string ApplicationXEva
    static string ApplicationXFontBdf
    static string ApplicationXFontGhostscript
    static string ApplicationXFontLinuxPsf
    static string ApplicationXFontPcf
    static string ApplicationXFontSnf
    static string ApplicationXFontType1
    static string ApplicationXFreearc
    static string ApplicationXFuturesplash
    static string ApplicationXGcaCompressed
    static string ApplicationXGlulx
    static string ApplicationXGnumeric
    static string ApplicationXGrampsXml
    static string ApplicationXGtar
    static string ApplicationXHdf
    static string ApplicationXInstallInstructions
    static string ApplicationXIso9660Image
    static string ApplicationXJavaJnlpFile
    static string ApplicationXLatex
    static string ApplicationXLzhCompressed
    static string ApplicationXMie
    static string ApplicationXMobipocketEbook
    static string ApplicationXMsApplication
    static string ApplicationXMsShortcut
    static string ApplicationXMsWmd
    static string ApplicationXMsWmz
    static string ApplicationXMsXbap
    static string ApplicationXMsaccess
    static string ApplicationXMsbinder
    static string ApplicationXMscardfile
    static string ApplicationXMsclip
    static string ApplicationXMsdownload
    static string ApplicationXMsmediaview
    static string ApplicationXMsmetafile
    static string ApplicationXMsmoney
    static string ApplicationXMspublisher
    static string ApplicationXMsschedule
    static string ApplicationXMsterminal
    static string ApplicationXMswrite
    static string ApplicationXNetcdf
    static string ApplicationXNzb
    static string ApplicationXPkcs12
    static string ApplicationXPkcs7Certificates
    static string ApplicationXPkcs7Certreqresp
    static string ApplicationXRarCompressed
    static string ApplicationXResearchInfoSystems
    static string ApplicationXSh
    static string ApplicationXShar
    static string ApplicationXShockwaveFlash
    static string ApplicationXSilverlightApp
    static string ApplicationXSql
    static string ApplicationXStuffit
    static string ApplicationXStuffitx
    static string ApplicationXSubrip
    static string ApplicationXSv4cpio
    static string ApplicationXSv4crc
    static string ApplicationXT3vmImage
    static string ApplicationXTads
    static string ApplicationXTar
    static string ApplicationXTcl
    static string ApplicationXTex
    static string ApplicationXTexTfm
    static string ApplicationXTexinfo
    static string ApplicationXTgif
    static string ApplicationXUstar
    static string ApplicationXWaisSource
    static string ApplicationXX509CaCert
    static string ApplicationXXfig
    static string ApplicationXXliffXml
    static string ApplicationXXpinstall
    static string ApplicationXXz
    static string ApplicationXZmachine
    static string ApplicationXamlXml
    static string ApplicationXcapDiffXml
    static string ApplicationXencXml
    static string ApplicationXhtmlXml
    static string ApplicationXml
    static string ApplicationXmlDtd
    static string ApplicationXopXml
    static string ApplicationXprocXml
    static string ApplicationXsltXml
    static string ApplicationXspfXml
    static string ApplicationXvXml
    static string ApplicationYang
    static string ApplicationYinXml
    static string ApplicationZip
    static string AudioAdpcm
    static string AudioBasic
    static string AudioMidi
    static string AudioMp4
    static string AudioMpeg
    static string AudioOgg
    static string AudioS3m
    static string AudioSilk
    static string AudioVndDeceAudio
    static string AudioVndDigitalWinds
    static string AudioVndDra
    static string AudioVndDts
    static string AudioVndDtsHd
    static string AudioVndLucentVoice
    static string AudioVndMsPlayreadyMediaPya
    static string AudioVndNueraEcelp4800
    static string AudioVndNueraEcelp7470
    static string AudioVndNueraEcelp9600
    static string AudioVndRip
    static string AudioWebm
    static string AudioXAac
    static string AudioXAiff
    static string AudioXCaf
    static string AudioXFlac
    static string AudioXMatroska
    static string AudioXMpegurl
    static string AudioXMsWax
    static string AudioXMsWma
    static string AudioXPnRealaudio
    static string AudioXPnRealaudioPlugin
    static string AudioXWav
    static string AudioXm
    static string Binary
    static string ChemicalXCdx
    static string ChemicalXCif
    static string ChemicalXCmdf
    static string ChemicalXCml
    static string ChemicalXCsml
    static string ChemicalXXyz
    static string DefaultExtension
    static string DefaultMimeType
    static string FontCollection
    static string FontOtf
    static string FontTtf
    static string FontWoff
    static string FontWoff2
    static string ImageBmp
    static string ImageCgm
    static string ImageG3fax
    static string ImageGif
    static string ImageHeif
    static string ImageIef
    static string ImageJpeg
    static string ImageKtx
    static string ImagePng
    static string ImagePrsBtif
    static string ImageSgi
    static string ImageSvg
    static string ImageSvgXml
    static string ImageTiff
    static string ImageVndAdobePhotoshop
    static string ImageVndDeceGraphic
    static string ImageVndDjvu
    static string ImageVndDvbSubtitle
    static string ImageVndDwg
    static string ImageVndDxf
    static string ImageVndFastbidsheet
    static string ImageVndFpx
    static string ImageVndFst
    static string ImageVndFujixeroxEdmicsMmr
    static string ImageVndFujixeroxEdmicsRlc
    static string ImageVndMsModi
    static string ImageVndMsPhoto
    static string ImageVndNetFpx
    static string ImageVndWapWbmp
    static string ImageVndXiff
    static string ImageWebp
    static string ImageX3ds
    static string ImageXCmuRaster
    static string ImageXCmx
    static string ImageXFreehand
    static string ImageXIcon
    static string ImageXMrsidImage
    static string ImageXPcx
    static string ImageXPict
    static string ImageXPortableAnymap
    static string ImageXPortableBitmap
    static string ImageXPortableGraymap
    static string ImageXPortablePixmap
    static string ImageXRgb
    static string ImageXTga
    static string ImageXXbitmap
    static string ImageXXpixmap
    static string ImageXXwindowdump
    static string MessageRfc822
    static string ModelIges
    static string ModelMesh
    static string ModelVndColladaXml
    static string ModelVndDwf
    static string ModelVndGdl
    static string ModelVndGtw
    static string ModelVndMts
    static string ModelVndVtu
    static string ModelVrml
    static string ModelX3dBinary
    static string ModelX3dVrml
    static string ModelX3dXml
    static string TextCacheManifest
    static string TextCalendar
    static string TextCss
    static string TextCsv
    static string TextHtml
    static string TextJavascript
    static string TextMarkdown
    static string TextN3
    static string TextPlain
    static string TextPrsLinesTag
    static string TextRichtext
    static string TextSgml
    static string TextTabSeparatedValues
    static string TextTroff
    static string TextTurtle
    static string TextUriList
    static string TextVcard
    static string TextVndCurl
    static string TextVndCurlDcurl
    static string TextVndCurlMcurl
    static string TextVndCurlScurl
    static string TextVndFly
    static string TextVndFmiFlexstor
    static string TextVndGraphviz
    static string TextVndIn3d3dml
    static string TextVndIn3dSpot
    static string TextVndSunJ2meAppDescriptor
    static string TextVndWapWml
    static string TextVndWapWmlscript
    static string TextXAsm
    static string TextXC
    static string TextXFortran
    static string TextXJavaSource
    static string TextXNfo
    static string TextXOpml
    static string TextXPascal
    static string TextXSetext
    static string TextXSfv
    static string TextXUuencode
    static string TextXVcalendar
    static string TextXVcard
    static string TextXml
    static string Video3gpp
    static string Video3gpp2
    static string VideoH261
    static string VideoH263
    static string VideoH264
    static string VideoJpeg
    static string VideoJpm
    static string VideoMj2
    static string VideoMp4
    static string VideoMpeg
    static string VideoOgg
    static string VideoQuicktime
    static string VideoVndDeceHd
    static string VideoVndDeceMobile
    static string VideoVndDecePd
    static string VideoVndDeceSd
    static string VideoVndDeceVideo
    static string VideoVndDvbFile
    static string VideoVndFvt
    static string VideoVndMpegurl
    static string VideoVndMsPlayreadyMediaPyv
    static string VideoVndUvvuMp4
    static string VideoVndVivo
    static string VideoWebm
    static string VideoXF4v
    static string VideoXFli
    static string VideoXFlv
    static string VideoXM4v
    static string VideoXMatroska
    static string VideoXMng
    static string VideoXMsAsf
    static string VideoXMsVob
    static string VideoXMsWm
    static string VideoXMsWmv
    static string VideoXMsWmx
    static string VideoXMsWvx
    static string VideoXMsvideo
    static string VideoXSgiMovie
    static string VideoXSmv
    static string XConferenceXCooltalk
  class MovingAverage
    ctor(int size = 32)
    void AddValue(double value)
    double GetAverage()
  class AppBundleConfig.Pipeline
    ctor()
    string? Description { get; set; }
    string DllName { get; set; }
    string? Guid { get; set; }
    string Name { get; set; }
    string OpenApiSpecJson { get; set; }
    string TypeName { get; set; }
    int Version { get; set; }
    List<AppBundleConfig.Workflow> Workflows { get; set; }
  class PipelineBundleConfigLegacy
    ctor()
    string CreatedAt { get; set; }
    string DllName { get; set; }
    string Hash { get; set; }
    string PipelineTypeName { get; set; }
    string Version { get; set; }
    static string ConfigFileName
    static string SpecFileName
  enum PipelineExecutionMode
    None
    HttpsEndpoint
    Scheduled
  sealed class PipelineProtocolHandler
    ctor(IDuplexPipe transport, Func<ProtocolMessage, Task> onMessageReceived, int maxMessageSize = 104857600)
    bool IsConnected { get; }
    Task CloseConnectionAsync()
    Task SendMessageAsync(ProtocolMessage message)
    Task StartReceiveAsync()
    void Stop()
  // A combined polymorphic converter that supports both single instances of TBase and collections of TBase. When reading, it searches for the "Type" property (in any order) to determine the concrete type. When writing, it writes a dictionary that always includes "Type" (as the first entry).
  class PolymorphicConverter<TBase> : JsonConverter<object> where TBase : class
    ctor()
    override bool CanConvert(Type typeToConvert)
    override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
  class PriceInfo
    ctor()
    bool IsCertain { get; set; }
    List<PriceTierInfo> PriceTiers { get; set; }
    string? UncertaintyReason { get; set; }
    string UsageName { get; set; }
  class PriceTierInfo
    ctor()
    string Currency { get; set; }
    double Price { get; set; }
    double Quantity { get; set; }
    double Threshold { get; set; }
    string? ThresholdCorrelatedUsageType { get; set; }
    string Unit { get; set; }
  class PricingOutput
    ctor()
    string CreatedAt { get; set; }
    List<PriceInfo> PriceInfos { get; set; }
  // Minimal QR code encoder. Generates QR codes as SVG without external dependencies. Supports byte mode encoding up to version 10.
  static class QrEncoder
    static string GenerateSvg(string data, int size)
  class RateLimiter
    ctor(TimeSpan window, int rateLimit)
    int Rate { get; }
    bool Guard()
  // Client-side agent for the in-house relay server. Establishes a single WebSocket to the relay, allocates endpoints on demand, and forwards incoming relay traffic to the matching local port.
  sealed class RelayAgent : IAsyncDisposable
    // Creates a relay agent with explicit connection parameters. Used when the relay host/port/token are already known (e.g. IkonServer's --public-access path). When stableId is non-empty, the relay assigns a fixed port-range segment to this identity so the public ports stay stable across reconnects.
    ctor(string relayServerAddress, int relayServerPort, string relayAuthToken, string stableId = "")
    // Allocates an endpoint. localPort of 0 picks an available port from an internal pool. The returned RelayEndpoint is disposable; dispose it to release the endpoint. When stablePortName is non-empty (and this agent has a non-empty stableId), the relay assigns a deterministic public port for that name within this agent's segment, so the endpoint's public URL stays the same across reconnects and process restarts. Empty = ephemeral, as before.
    Task<RelayEndpoint> AddEndpointAsync(EndpointProtocol protocol, int localPort = 0, string stablePortName = "", CancellationToken cancellationToken = null)
    // Connects to the relay server and authenticates. Called implicitly by AddEndpointAsync on first use; calling it explicitly is optional.
    Task ConnectAsync(CancellationToken cancellationToken = null)
    // Creates a relay agent whose host/port/token are fetched from IkonBackend on first connect. Pass a non-empty stableId to opt into stable public-port assignments.
    static RelayAgent CreateFromIkonBackend(string stableId = "")
    ValueTask DisposeAsync()
  // A relay endpoint. Exposes the locally bound port and the publicly reachable host/port. Dispose to release the endpoint and its local port reservation.
  sealed class RelayEndpoint : IAsyncDisposable
    int LocalPort { get; }
    EndpointProtocol Protocol { get; }
    string PublicHost { get; }
    int PublicPort { get; }
    ValueTask DisposeAsync()
  enum RequiredStatus
    Default
    Required
    Optional
  class Resources : AsyncLocalInstance<Resources>
    ctor()
    Task<byte[]> ReadAsBytesAsync(string resourcePath)
    Task<Stream> ReadAsStreamAsync(string resourcePath)
    Task<string> ReadAsStringAsync(string resourcePath)
  static class Retrier
    static T Run<T>(List<Type>? retryableExceptions, int retries, Func<T> func, string callerMemberName = "", string callerFilePath = "")
    static T Run<T>(Func<T> func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
    static void Run(List<Type>? retryableExceptions, int retries, Action func, string callerMemberName = "", string callerFilePath = "")
    static void Run(Action func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
    static Task<T> RunAsync<T>(List<Type>? retryableExceptions, int retries, Func<Task<T>> func, string callerMemberName = "", string callerFilePath = "")
    static Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
    static Task<T> RunAsync<T>(Func<Task<T>> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
    static Task RunAsync(List<Type>? retryableExceptions, int retries, Func<Task> func, string callerMemberName = "", string callerFilePath = "")
    static Task RunAsync(Func<Task> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null, string callerMemberName = "", string callerFilePath = "")
  class AppProjectConfigLegacy.Target : ITomlMetadataProvider
    ctor()
    string ChannelId { get; set; }
    string OrganisationId { get; set; }
    string SpaceId { get; set; }
  class AppProjectConfig.TargetConfig : ITomlMetadataProvider
    ctor()
    string ChannelId { get; set; }
    string Name { get; set; }
    string OrganisationId { get; set; }
    string SpaceId { get; set; }
  class TempDirectory
    ctor(string rootDirName)
    string FullPath { get; }
    long Size { get; }
    void Delete()
    string GetDirPath(string subDirName)
    string GetFilePath(string fileName)
  class TimeAverage
    ctor()
    void AddValue(double value)
    double GetValue()
  static class TimeSpanExtensions
    static string ToHumanReadable(TimeSpan timeSpan, int precision = 2)
  class TimedQueue<T>
    ctor()
    int Count { get; }
    void Enqueue(T item, long durationInMicroseconds)
    Task UpdateAsync(float deltaTime, Func<T, Task> process)
  class Translator
    ctor(string spaceId)
    ctor(string spaceId, string locale)
    Task InitializeAsync()
    void SetLocale(string newLocale)
    Task<string> TranslateAsync(string text, string description = "")
  class UsageTracker
    ctor()
    bool HasUsages { get; }
    Dictionary<string, double> Snapshot { get; }
    void Clear()
    void OnLogEvent(object sender, LogEvent logEvent)
    string PrettyPrint()
    void Register(string name, double value)
  static class Utils
    static string GenerateRandomToken(int size = 32)
    static string GetCSharpTypeName(object? obj)
    static IPAddress GetFirstIPv4AddressOrLocalhost()
    static string ToUnescapedString(string input, bool unicodeOnly = false)
  class ValueStatistics
    ctor(double alpha = 0.9, double interval = 30)
    string ResultString { get; }
    bool TimerElapsed { get; }
    void AddSample(double sample)
    void Reset()
    double Average
    double Maximum
    double Minimum
  class AppBundleConfig.WebhookFunction
    ctor()
    bool AutoRegistered { get; set; }
    string Name { get; set; }
  class AppBundleConfig.Workflow
    ctor()
    PipelineExecutionMode ExecutionMode { get; set; }
    string Name { get; set; }
    string? Schedule { get; set; }

namespace Ikon.Common.Assets
  static class StorageExtensions
    static Task AddCloudFilePublicStorageAsync(Asset asset)
    static Task AddCloudFileStorageAsync(Asset asset)
    static Task AddCloudJsonStorageAsync(Asset asset)
    static Task AddCloudProfileStorageAsync(Asset asset)
    static Task AddLocalFileStorageAsync(Asset asset, string root)

namespace Ikon.Common.Git
  // Git branch information.
  class GitBranch : IEquatable<GitBranch>
    // Git branch information.
    ctor(string Name, bool IsRemote, bool IsCurrent)
    bool IsCurrent { get; init; }
    bool IsRemote { get; init; }
    string Name { get; init; }
  // Git file change type.
  enum GitChangeType
    Added
    Modified
    Deleted
    Renamed
    Untracked
  // Options for cloning a repository.
  class GitCloneOptions : IEquatable<GitCloneOptions>
    // Options for cloning a repository.
    ctor(string? Branch = null, bool Shallow = false, GitCredentials? Credentials = null)
    string? Branch { get; init; }
    GitCredentials? Credentials { get; init; }
    bool Shallow { get; init; }
  // Git commit information.
  class GitCommit : IEquatable<GitCommit>
    // Git commit information.
    ctor(string Sha, string ShortSha, string Author, string AuthorEmail, DateTimeOffset Date, string Message)
    string Author { get; init; }
    string AuthorEmail { get; init; }
    DateTimeOffset Date { get; init; }
    string Message { get; init; }
    string Sha { get; init; }
    string ShortSha { get; init; }
  // Git credentials for authenticated operations.
  class GitCredentials : IEquatable<GitCredentials>
    // Git credentials for authenticated operations.
    ctor(string Username, string Password)
    string Password { get; init; }
    string Username { get; init; }
  // Git diff between two commits.
  class GitDiff : IEquatable<GitDiff>
    // Git diff between two commits.
    ctor(string? FromSha, string? ToSha, List<GitFileDiff> Files)
    List<GitFileDiff> Files { get; init; }
    string? FromSha { get; init; }
    string? ToSha { get; init; }
  // A changed file in git status or diff.
  class GitFileChange : IEquatable<GitFileChange>
    // A changed file in git status or diff.
    ctor(string Path, GitChangeType Type)
    string Path { get; init; }
    GitChangeType Type { get; init; }
  // File diff information.
  class GitFileDiff : IEquatable<GitFileDiff>
    // File diff information.
    ctor(string Path, GitChangeType Type, int LinesAdded, int LinesRemoved, string? Patch = null)
    int LinesAdded { get; init; }
    int LinesRemoved { get; init; }
    string? Patch { get; init; }
    string Path { get; init; }
    GitChangeType Type { get; init; }
  // Strongly-typed git repository operations.
  class GitRepository
    // Strongly-typed git repository operations.
    ctor(string workingDirectory, GitCredentials? credentials = null)
    GitCredentials? Credentials { get; }
    string WorkingDirectory { get; }
    // Abort all in-progress operations (merge, rebase, cherry-pick).
    Task AbortAllInProgressOperationsAsync(CancellationToken ct = null)
    // Abort an in-progress cherry-pick.
    Task<bool> AbortCherryPickAsync(CancellationToken ct = null)
    // Abort an in-progress merge.
    Task<bool> AbortMergeAsync(CancellationToken ct = null)
    // Abort an in-progress rebase.
    Task<bool> AbortRebaseAsync(CancellationToken ct = null)
    // Add a remote.
    Task AddRemoteAsync(string name, string url, CancellationToken ct = null)
    // Checkout an existing branch.
    Task CheckoutAsync(string branchOrRef, CancellationToken ct = null)
    // Checkout files from a specific ref without changing HEAD.
    Task CheckoutFilesFromRefAsync(string refName, string path = ".", CancellationToken ct = null)
    // Clone a repository to a target directory.
    static Task<GitRepository> CloneAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = null)
    // Clone a repository or sync if it already exists. Returns the repository instance with the current SHA.
    static Task<ValueTuple<GitRepository, string?, bool>> CloneOrSyncAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = null)
    // Commit staged changes.
    Task<GitCommit> CommitAsync(string message, CancellationToken ct = null)
    // Commit staged changes with custom author.
    Task<GitCommit> CommitAsync(string message, string authorName, string authorEmail, bool allowEmpty = false, CancellationToken ct = null)
    // Create and checkout a new branch.
    Task CreateBranchAsync(string name, string? startPoint = null, CancellationToken ct = null)
    // Create a tag.
    Task<GitTag> CreateTagAsync(string name, string? message = null, CancellationToken ct = null)
    // Delete a tag.
    Task DeleteTagAsync(string name, CancellationToken ct = null)
    // Discard all uncommitted changes.
    Task DiscardChangesAsync(CancellationToken ct = null)
    // Escape a commit message for shell.
    static string EscapeMessage(string message)
    // Fetch from remote.
    Task FetchAsync(bool includeTags = false, CancellationToken ct = null)
    // Construct an authenticated URL.
    static string GetAuthenticatedUrl(string url, GitCredentials credentials)
    // Get all branches.
    Task<List<GitBranch>> GetBranchesAsync(CancellationToken ct = null)
    // Get a local git config value.
    Task<string?> GetConfigAsync(string key, CancellationToken ct = null)
    // Get the current branch name.
    Task<string> GetCurrentBranchAsync(CancellationToken ct = null)
    // Get diff between HEAD and another target (or working directory if null).
    Task<GitDiff> GetDiffAsync(string? target = null, CancellationToken ct = null)
    // Get the HEAD commit.
    Task<GitCommit?> GetHeadCommitAsync(CancellationToken ct = null)
    // Get the HEAD SHA.
    Task<string?> GetHeadShaAsync(bool shortSha = false, CancellationToken ct = null)
    // Get commit history.
    Task<List<GitCommit>> GetHistoryAsync(int limit = 20, string? fromRef = null, CancellationToken ct = null)
    // Get remote URL (without credentials).
    Task<string?> GetRemoteUrlAsync(string name = "origin", CancellationToken ct = null)
    // Get the current repository status.
    Task<GitStatus> GetStatusAsync(CancellationToken ct = null)
    // Get all tags.
    Task<List<GitTag>> GetTagsAsync(CancellationToken ct = null)
    // Check if repository has any commits.
    Task<bool> HasCommitsAsync(CancellationToken ct = null)
    // Check if a remote exists.
    Task<bool> HasRemoteAsync(string name = "origin", CancellationToken ct = null)
    // Check if there are uncommitted changes.
    Task<bool> HasUncommittedChangesAsync(CancellationToken ct = null)
    // Check if there are uncommitted changes under a specific path.
    Task<bool> HasUncommittedChangesAsync(string path, CancellationToken ct = null)
    // Initialize a git repository and connect to a remote, preserving local files. Local files are kept as-is and NOT merged with remote content. Returns the repository instance ready for use.
    static Task<GitRepository> InitAndConnectAsync(string directory, string remoteUrl, GitCredentials? credentials = null, string? configKey = null, string? configValue = null, CancellationToken ct = null)
    // Initialize a new git repository.
    static Task<GitRepository> InitAsync(string directory, CancellationToken ct = null)
    // Check if the working directory is a git repository.
    Task<bool> IsGitRepositoryAsync(CancellationToken ct = null)
    // Check if a directory is a git repository.
    static Task<bool> IsGitRepositoryAsync(string directory, CancellationToken ct = null)
    // List all worktrees attached to this repository (including the primary one). Parses the output of `git worktree list --porcelain`.
    Task<List<GitWorktreeInfo>> ListWorktreesAsync(CancellationToken ct = null)
    // Push to remote.
    Task PushAsync(bool setUpstream = false, CancellationToken ct = null)
    // Check if a ref exists.
    Task<bool> RefExistsAsync(string refName, CancellationToken ct = null)
    // Rename current branch.
    Task RenameBranchAsync(string oldName, string newName, CancellationToken ct = null)
    // Reset HEAD to a specific ref (hard reset).
    Task ResetHardAsync(string target, CancellationToken ct = null)
    // Reset HEAD to a specific ref (soft reset - keeps changes staged).
    Task ResetSoftAsync(string target, CancellationToken ct = null)
    // Restore to a specific target (tag, sha, or branch).
    Task<GitSyncResult> RestoreAsync(string target, CancellationToken ct = null)
    // Run a git command (throws on failure).
    Task<string> RunAsync(string args, CancellationToken ct = null)
    // Save changes (stage, commit, push).
    Task<GitSyncResult> SaveAsync(string message, CancellationToken ct = null)
    // Set a local git config value.
    Task SetConfigAsync(string key, string value, CancellationToken ct = null)
    // Set remote URL.
    Task SetRemoteUrlAsync(string name, string url, CancellationToken ct = null)
    // Set up tracking for a branch.
    Task SetUpstreamAsync(string remoteBranch, CancellationToken ct = null)
    static string ShortCommitHash(string? hash)
    // Stage all changes.
    Task StageAllAsync(CancellationToken ct = null)
    // Stage a specific path (file or directory).
    Task StagePathAsync(string path, CancellationToken ct = null)
    // Stash all changes.
    Task<bool> StashAsync(string? message = null, CancellationToken ct = null)
    // Pop the latest stash.
    Task<bool> StashPopAsync(CancellationToken ct = null)
    // Strip credentials from a git URL for safe display/comparison.
    static string StripCredentialsFromUrl(string url)
    // Sync to latest remote (fetch + reset --hard).
    Task<GitSyncResult> SyncAsync(CancellationToken ct = null)
    // Try to open an existing git repository.
    static GitRepository? TryOpen(string directory)
    // Run a git command (doesn't throw on failure).
    Task<ValueTuple<bool, string, string>> TryRunAsync(string args, CancellationToken ct = null)
    // Compare two git URLs, ignoring credentials and trailing slashes.
    static bool UrlsMatch(string? url1, string? url2)
  // Git repository status.
  class GitStatus : IEquatable<GitStatus>
    // Git repository status.
    ctor(string Branch, string? HeadSha, bool HasUncommittedChanges, bool IsDetachedHead, int AheadBy, int BehindBy, List<GitFileChange> Changes)
    int AheadBy { get; init; }
    int BehindBy { get; init; }
    string Branch { get; init; }
    List<GitFileChange> Changes { get; init; }
    bool HasUncommittedChanges { get; init; }
    string? HeadSha { get; init; }
    bool IsDetachedHead { get; init; }
  // Result of a sync/restore/save operation.
  class GitSyncResult : IEquatable<GitSyncResult>
    // Result of a sync/restore/save operation.
    ctor(bool Success, string? PreviousSha, string? CurrentSha, string? Error = null)
    string? CurrentSha { get; init; }
    string? Error { get; init; }
    string? PreviousSha { get; init; }
    bool Success { get; init; }
  // Git tag information.
  class GitTag : IEquatable<GitTag>
    // Git tag information.
    ctor(string Name, string Sha, GitCommit? Commit = null)
    GitCommit? Commit { get; init; }
    string Name { get; init; }
    string Sha { get; init; }
  // Git worktree entry reported by `git worktree list`.
  class GitWorktreeInfo : IEquatable<GitWorktreeInfo>
    // Git worktree entry reported by `git worktree list`.
    ctor(string Path, string? Head, string? Branch)
    string? Branch { get; init; }
    string? Head { get; init; }
    string Path { get; init; }

namespace Ikon.Common.Maths
  // Determines the axis convention used.
  enum AxisConvention
    RightHanded_X_Up
    RightHanded_Y_Up
    RightHanded_Z_Up
    LeftHanded_X_Up
    LeftHanded_Y_Up
    LeftHanded_Z_Up
  // Axis-aligned bounding box value type in three dimensional space.
  struct BoundingBox : IEquatable<BoundingBox>
    // Constructs a new minimal bounding box fully enclosing a given bounding sphere.
    ctor(BoundingSphere sphere)
    // Creates a new bounding box from a given set of values in 3D space.
    ctor(Vector3[] points)
    // Constructs a new bounding box struct with given corner values.
    ctor(Vector3 a, Vector3 b)
    // Gets or sets the center point of the bounding box.
    Vector3 Center { get; set; }
    // Gets or sets the diagonal vector of the bounding box, which is a vector between the minimum and maximum corners.
    Vector3 Diagonal { get; set; }
    // Gets or sets half of the diagonal vector of the bounding box.
    Vector3 HalfDiagonal { get; set; }
    // Returns a cubic bounding box centered at the origin which spans from -1 to 1 in all axis.
    static BoundingBox UnitCube { get; }
    // Returns an array with all the eight corners comprising the bounding box.
    Vector3[] GetCorners()
    // Returns a new bounding box which fully encloses given bounding boxes. Remember this is in axis-aligned space.
    static BoundingBox Merge(BoundingBox value1, BoundingBox value2)
    // The maximum corner value of the bounding box.
    Vector3 Maximum
    // The minimum corner value of the bounding box.
    Vector3 Minimum
  class BoundingFrustum : IEquatable<BoundingFrustum>
    ctor(Matrix4x4 value)
    ctor(Matrix4x4 value, Vector2 offset, Vector2 size)
    Plane Bottom { get; }
    Plane Far { get; }
    Plane Left { get; }
    Matrix4x4 Matrix4x4 { get; set; }
    Plane Near { get; }
    Plane Right { get; }
    Plane Top { get; }
    ContainmentType Contains(BoundingBox box)
    void Contains(ref BoundingBox box, out ContainmentType result)
    ContainmentType Contains(BoundingFrustum frustum)
    ContainmentType Contains(BoundingSphere sphere)
    void Contains(ref BoundingSphere sphere, out ContainmentType result)
    ContainmentType Contains(Vector3 point)
    void Contains(ref Vector3 point, out ContainmentType result)
    Vector3[] GetCorners()
    bool Intersects(BoundingBox box)
    void Intersects(ref BoundingBox box, out bool result)
    bool Intersects(BoundingFrustum frustum)
    bool Intersects(BoundingSphere sphere)
    void Intersects(ref BoundingSphere sphere, out bool result)
    PlaneIntersectionType Intersects(Plane plane)
    void Intersects(ref Plane plane, out PlaneIntersectionType result)
    float? Intersects(Ray ray)
    void Intersects(ref Ray ray, out float? result)
  // Bounding sphere value type in three dimensional space.
  struct BoundingSphere : IEquatable<BoundingSphere>
    // Constructs a new minimal bounding sphere fully enclosing a given bounding box.
    ctor(BoundingBox value)
    // Creates a new bounding sphere from a given set of values in 3D space.
    ctor(Vector3[] points)
    // Constructs a new bounding sphere from given center and radius values.
    ctor(Vector3 center, float radius)
    // Returns a sphere centered at the origin with radius 1.
    static BoundingSphere UnitSphere { get; }
    // Constructs a new bounding sphere which fully encloses given bounding spheres.
    static BoundingSphere Merge(BoundingSphere value1, BoundingSphere value2)
    // The center of the bounding sphere.
    Vector3 Center
    // The radius of the bounding sphere.
    float Radius
  // Contains static methods to help in determining intersections, containment, etc.
  static class Collision
    // Determines whether a BoundingBox contains a BoundingBox .
    static ContainmentType BoxContainsBox(ref BoundingBox box1, ref BoundingBox box2)
    // Determines whether a BoundingBox contains a point.
    static ContainmentType BoxContainsPoint(ref BoundingBox box, ref Vector3 point)
    // Determines whether a BoundingBox contains a BoundingSphere .
    static ContainmentType BoxContainsSphere(ref BoundingBox box, ref BoundingSphere sphere)
    // Determines whether there is an intersection between a BoundingBox and a BoundingBox .
    static bool BoxIntersectsBox(ref BoundingBox box1, ref BoundingBox box2)
    // Determines whether there is an intersection between a BoundingBox and a BoundingSphere .
    static bool BoxIntersectsSphere(ref BoundingBox box, ref BoundingSphere sphere)
    // Determines the closest point between a BoundingBox and a point.
    static void ClosestPointBoxPoint(ref BoundingBox box, ref Vector3 point, out Vector3 result)
    // Determines the closest point between a Plane and a point.
    static void ClosestPointPlanePoint(ref Plane plane, ref Vector3 point, out Vector3 result)
    // Determines the closest point between a point and a triangle.
    static void ClosestPointPointTriangle(ref Vector3 point, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out Vector3 result)
    // Determines the closest point between a BoundingSphere and a point.
    static void ClosestPointSpherePoint(ref BoundingSphere sphere, ref Vector3 point, out Vector3 result)
    // Determines the closest point between a BoundingSphere and a BoundingSphere .
    static void ClosestPointSphereSphere(ref BoundingSphere sphere1, ref BoundingSphere sphere2, out Vector3 result)
    // Determines the distance between a BoundingBox and a BoundingBox .
    static float DistanceBoxBox(ref BoundingBox box1, ref BoundingBox box2)
    // Determines the distance between a BoundingBox and a point.
    static float DistanceBoxPoint(ref BoundingBox box, ref Vector3 point)
    // Determines the distance between a Plane and a point.
    static float DistancePlanePoint(ref Plane plane, ref Vector3 point)
    // Determines the distance between a BoundingSphere and a point.
    static float DistanceSpherePoint(ref BoundingSphere sphere, ref Vector3 point)
    // Determines the distance between a BoundingSphere and a BoundingSphere .
    static float DistanceSphereSphere(ref BoundingSphere sphere1, ref BoundingSphere sphere2)
    // Determines whether there is an intersection between a Plane and a BoundingBox .
    static PlaneIntersectionType PlaneIntersectsBox(ref Plane plane, ref BoundingBox box)
    // Determines whether there is an intersection between a Plane and a Plane .
    static bool PlaneIntersectsPlane(ref Plane plane1, ref Plane plane2)
    // Determines whether there is an intersection between a Plane and a Plane .
    static bool PlaneIntersectsPlane(ref Plane plane1, ref Plane plane2, out Ray line)
    // Determines whether there is an intersection between a Plane and a point.
    static PlaneIntersectionType PlaneIntersectsPoint(ref Plane plane, ref Vector3 point)
    // Determines whether there is an intersection between a Plane and a BoundingSphere .
    static PlaneIntersectionType PlaneIntersectsSphere(ref Plane plane, ref BoundingSphere sphere)
    // Determines whether there is an intersection between a Plane and a triangle.
    static PlaneIntersectionType PlaneIntersectsTriangle(ref Plane plane, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3)
    // Determines whether there is an intersection between a Ray and a BoundingBox .
    static bool RayIntersectsBox(ref Ray ray, ref BoundingBox box, out float distance)
    // Determines whether there is an intersection between a Ray and a Plane .
    static bool RayIntersectsBox(ref Ray ray, ref BoundingBox box, out Vector3 point)
    // Determines whether there is an intersection between a Ray and a Plane .
    static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out float distance)
    // Determines whether there is an intersection between a Ray and a Plane .
    static bool RayIntersectsPlane(ref Ray ray, ref Plane plane, out Vector3 point)
    // Determines whether there is an intersection between a Ray and a point.
    static bool RayIntersectsPoint(ref Ray ray, ref Vector3 point)
    // Determines whether there is an intersection between a Ray and a Ray .
    static bool RayIntersectsRay(ref Ray ray1, ref Ray ray2, out Vector3 point)
    // Determines whether there is an intersection between a Ray and a BoundingSphere .
    static bool RayIntersectsSphere(ref Ray ray, ref BoundingSphere sphere, out float distance)
    // Determines whether there is an intersection between a Ray and a BoundingSphere .
    static bool RayIntersectsSphere(ref Ray ray, ref BoundingSphere sphere, out Vector3 point)
    // Determines whether there is an intersection between a Ray and a triangle.
    static bool RayIntersectsTriangle(ref Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out float distance)
    // Determines whether there is an intersection between a Ray and a triangle.
    static bool RayIntersectsTriangle(ref Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out Vector3 point)
    // Determines whether a BoundingSphere contains a BoundingBox .
    static ContainmentType SphereContainsBox(ref BoundingSphere sphere, ref BoundingBox box)
    // Determines whether a BoundingSphere contains a point.
    static ContainmentType SphereContainsPoint(ref BoundingSphere sphere, ref Vector3 point)
    // Determines whether a BoundingSphere contains a BoundingSphere .
    static ContainmentType SphereContainsSphere(ref BoundingSphere sphere1, ref BoundingSphere sphere2)
    // Determines whether a BoundingSphere contains a triangle.
    static ContainmentType SphereContainsTriangle(ref BoundingSphere sphere, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3)
    // Determines whether there is an intersection between a BoundingSphere and a BoundingSphere .
    static bool SphereIntersectsSphere(ref BoundingSphere sphere1, ref BoundingSphere sphere2)
    // Determines whether there is an intersection between a BoundingSphere and a triangle.
    static bool SphereIntersectsTriangle(ref BoundingSphere sphere, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3)
  // Describes how one bounding volume contains another.
  enum ContainmentType
    Outside
    Inside
    Intersects
  // Keyframe that can be used by the interpolations.
  struct KeyFrame<T> where T : struct
    float Time
    T Value
  // Contains static functions to be used with the common mathematical value types: float, Vector2, Vector3, Vector4 and Matrix4x4.
  class Math
    ctor()
    // Returns the absolute value of a given value.
    static Vector4 Abs(Vector4 a)
    // Returns the absolute value of a given value.
    static Vector3 Abs(Vector3 a)
    // Returns the absolute value of a given value.
    static Vector2 Abs(Vector2 a)
    // Returns the absolute value of a given value.
    static float Abs(float a)
    // Returns the absolute value of a given value.
    static int Abs(int a)
    // Returns the angle whose cosine is the given value.
    static Vector4 Acos(Vector4 a)
    // Returns the angle whose cosine is the given value.
    static Vector3 Acos(Vector3 a)
    // Returns the angle whose cosine is the given value.
    static Vector2 Acos(Vector2 a)
    // Returns the angle whose cosine is the given value.
    static float Acos(float a)
    // Adds matrices together.
    static Matrix4x4 Add(Matrix4x4 a, Matrix4x4 b)
    // Determines the Euclidean angle between two given values.
    static float Angle(Vector4 a, Vector4 b)
    // Determines the Euclidean angle between two given values.
    static float Angle(Vector3 a, Vector3 b)
    // Determines the Euclidean angle between two given values.
    static float Angle(Vector2 a, Vector2 b)
    // Determines the Euclidean angle between two given values.
    static float Angle(float a, float b)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(float[] values)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(Vector2[] values)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(Vector3[] values)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(Vector4[] values)
    // Determines whether all the values in a given array are all zero.
    static bool AreZeroes(Quaternion[] values)
    // Returns the angle whose sine is the given value.
    static Vector4 Asin(Vector4 a)
    // Returns the angle whose sine is the given value.
    static Vector3 Asin(Vector3 a)
    // Returns the angle whose sine is the given value.
    static Vector2 Asin(Vector2 a)
    // Returns the angle whose sine is the given value.
    static float Asin(float a)
    // Returns the angle whose tangent is the given value.
    static Vector4 Atan(Vector4 a)
    // Returns the angle whose tangent is the given value.
    static Vector3 Atan(Vector3 a)
    // Returns the angle whose tangent is the given value.
    static Vector2 Atan(Vector2 a)
    // Returns the angle whose tangent is the given value.
    static float Atan(float a)
    // Returns the angle whose tangent is the quotient of two specified value.
    static Vector4 Atan2(Vector4 a, Vector4 b)
    // Returns the angle whose tangent is the quotient of two specified value.
    static Vector3 Atan2(Vector3 a, Vector3 b)
    // Returns the angle whose tangent is the quotient of two specified value.
    static Vector2 Atan2(Vector2 a, Vector2 b)
    // Returns the angle whose tangent is the quotient of two specified value.
    static float Atan2(float a, float b)
    static Vector3 CalculateCentroid(Vector3[] positions, double[]? weights = null)
    static float CalculateCentroidRootMeanSquareDistance(Vector3[] positions, Vector3 centroid, double[]? weights = null)
    static float CalculatePositionWiseRootMeanSquareDistance(Vector3[] positions)
    static float CalculateRootMeanSquareDistance(Vector3[] a, Vector3[] b)
    // Interpolates between given values using Catmull-Rom interpolation.
    static Vector4 CatmullRom(Vector4 value1, Vector4 value2, Vector4 value3, Vector4 value4, float weight)
    // Interpolates between given values using Catmull-Rom interpolation.
    static Vector3 CatmullRom(Vector3 value1, Vector3 value2, Vector3 value3, Vector3 value4, float weight)
    // Interpolates between given values using Catmull-Rom interpolation.
    static Vector2 CatmullRom(Vector2 value1, Vector2 value2, Vector2 value3, Vector2 value4, float weight)
    // Interpolates between given values using Catmull-Rom interpolation.
    static float CatmullRom(float value1, float value2, float value3, float value4, float weight)
    // Returns a value in a given time Position by interpolating the array of given keyframes.
    static Vector4 CatmullRom(KeyFrame<Vector4>[] values, float time)
    // Returns the smallest integer value that is greater than or equal to the specified value.
    static Vector4 Ceil(Vector4 a)
    // Returns the smallest integer value that is greater than or equal to the specified value.
    static Vector3 Ceil(Vector3 a)
    // Returns the smallest integer value that is greater than or equal to the specified value.
    static Vector2 Ceil(Vector2 a)
    // Returns the smallest integer value that is greater than or equal to the specified value.
    static float Ceil(float a)
    // Clamps the specified value to the specified range.
    static Vector4 Clamp(Vector4 a, Vector4 c1, Vector4 c2)
    // Clamps the specified value to the specified range.
    static Vector3 Clamp(Vector3 a, Vector3 c1, Vector3 c2)
    // Clamps the specified value to the specified range.
    static Vector2 Clamp(Vector2 a, Vector2 c1, Vector2 c2)
    // Clamps the specified value to the specified range.
    static Vector4 Clamp(Vector4 a, float c1, float c2)
    // Clamps the specified value to the specified range.
    static Vector3 Clamp(Vector3 a, float c1, float c2)
    // Clamps the specified value to the specified range.
    static Vector2 Clamp(Vector2 a, float c1, float c2)
    // Clamps the specified value to the specified range.
    static float Clamp(float a, float c1, float c2)
    // Clamps the specified value to the specified range.
    static int Clamp(int a, int c1, int c2)
    // Converts a given (3D) value to use another axis convention.
    static Vector3 ConvertAxis(Vector3 value, AxisConvention currentAxis, AxisConvention destinationAxis)
    // Converts a given (3D) value to use another axis convention.
    static Vector4 ConvertAxis(Vector4 value, AxisConvention currentAxis, AxisConvention destinationAxis)
    // Converts a given (3D) value to use another axis convention.
    static Matrix4x4 ConvertAxis(Matrix4x4 value, AxisConvention currentAxis, AxisConvention destinationAxis)
    // Returns the cosine of a given value.
    static Vector4 Cos(Vector4 a)
    // Returns the cosine of a given value.
    static Vector3 Cos(Vector3 a)
    // Returns the cosine of a given value.
    static Vector2 Cos(Vector2 a)
    // Returns the cosine of a given value.
    static float Cos(float a)
    // Returns the hyperbolic cosine of a given value.
    static Vector4 Cosh(Vector4 a)
    // Returns the hyperbolic cosine of a given value.
    static Vector3 Cosh(Vector3 a)
    // Returns the hyperbolic cosine of a given value.
    static Vector2 Cosh(Vector2 a)
    // Returns the hyperbolic cosine of a given value.
    static float Cosh(float a)
    // Returns the cross product of two vectors.
    static Vector3 Cross(Vector3 a, Vector3 b)
    // Converts a given radian value into degrees.
    static float Degrees(float radians)
    // Dehomogenizes a given vector by dividing all the other components with the last one.
    static Vector4 Dehomogenize(Vector4 a)
    // Returns the shortest Euclidean distance between two values.
    static float Distance(Vector4 a, Vector4 b)
    // Returns the shortest Euclidean distance between two values.
    static float Distance(Vector3 a, Vector3 b)
    // Returns the shortest Euclidean distance between two values.
    static float Distance(Vector2 a, Vector2 b)
    // Returns the shortest Euclidean distance between two values.
    static float Distance(float a, float b)
    // Returns the the shortest Euclidean distance between two values squared.
    static float DistanceSquared(Vector4 a, Vector4 b)
    // Returns the the shortest Euclidean distance between two values squared.
    static float DistanceSquared(Vector3 a, Vector3 b)
    // Returns the the shortest Euclidean distance between two values squared.
    static float DistanceSquared(Vector2 a, Vector2 b)
    // Returns the the shortest Euclidean distance between two values squared.
    static float DistanceSquared(float a, float b)
    // Returns the dot-product of two values.
    static float Dot(Plane a, Plane b)
    // Returns the dot-product of two values.
    static float Dot(Quaternion a, Quaternion b)
    // Returns the dot-product of two values.
    static float Dot(Vector4 a, Vector4 b)
    // Returns the dot-product of two values.
    static float Dot(Vector3 a, Vector3 b)
    // Returns the dot-product of two values.
    static float Dot(Vector2 a, Vector2 b)
    // Returns the dot-product of two values.
    static float Dot(float a, float b)
    // Returns the largest integer that is less than or equal to the specified value.
    static Vector4 Floor(Vector4 a)
    // Returns the largest integer that is less than or equal to the specified value.
    static Vector3 Floor(Vector3 a)
    // Returns the largest integer that is less than or equal to the specified value.
    static Vector2 Floor(Vector2 a)
    // Returns the largest integer that is less than or equal to the specified value.
    static float Floor(float a)
    // Calculates the point on a line with the shortest distance from a given point.
    static Vector2 GetClosestPoint(Vector2 start, Vector2 end, Vector2 position)
    // Calculates the point on an infinite line with the shortest distance from a given point.
    static Vector2 GetClosestPoint(Vector2 start, Vector2 end, Vector2 position, out float distance, out float u)
    // Calculates the point on a line with the shortest distance from a given point.
    static Vector2 GetClosestPoint(Vector2 start, Vector2 end, Vector2 position, out float distance, out float u, out float x)
    // Calculates the point on a segment with the shortest distance from a given point. This means that segment is not infinite line.
    static Vector2 GetClosestPointInClosedSegment(Vector2 start, Vector2 end, Vector2 position, out float distance, out float u)
    // Interpolates between given values using Hermite interpolation.
    static Vector4 Hermite(Vector4 value1, Vector4 tangent1, Vector4 value2, Vector4 tangent2, float weight)
    // Interpolates between given values using Hermite interpolation.
    static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float weight)
    // Interpolates between given values using Hermite interpolation.
    static Vector2 Hermite(Vector2 value1, Vector2 tangent1, Vector2 value2, Vector2 tangent2, float weight)
    // Interpolates between given values using Hermite interpolation.
    static float Hermite(float value1, float tangent1, float value2, float tangent2, float weight)
    // Calculates the inverse for a given matrix.
    static Matrix4x4 Invert(Matrix4x4 value)
    // Calculates the inverse for a given quaternion.
    static Quaternion Invert(Quaternion value)
    // Returns the reciprocal or multiplicative inverse for a given value.
    static Vector4 Invert(Vector4 a)
    // Returns the reciprocal or multiplicative inverse for a given value.
    static Vector3 Invert(Vector3 a)
    // Returns the reciprocal or multiplicative inverse for a given value.
    static Vector2 Invert(Vector2 a)
    // Returns the reciprocal or multiplicative inverse for a given value.
    static float Invert(float a)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(float value)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(Vector2 value)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(Vector3 value)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(Vector4 value)
    // Determines whether a given value is a real number, and not -inf, +inf, or NaN.
    static bool IsNumber(Quaternion value)
    // Determines whether a value is power-of-two.
    static bool IsPower2(int v)
    // Returns the Euclidean length of a given value.
    static float Length(Plane a)
    // Returns the Euclidean length of a given value.
    static float Length(Quaternion a)
    // Returns the Euclidean length of a given value.
    static float Length(Vector4 a)
    // Returns the Euclidean length of a given value.
    static float Length(Vector3 a)
    // Returns the Euclidean length of a given value.
    static float Length(Vector2 a)
    // Returns the Euclidean length of a given value.
    static float Length(float a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(Quaternion a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(Vector4 a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(Vector3 a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(Vector2 a)
    // Returns the Euclidean length of a given value squared.
    static float LengthSquared(float a)
    // Interpolates between given values using linear interpolation.
    static Quaternion Lerp(Quaternion value1, Quaternion value2, float weight)
    // Interpolates between given values using linear interpolation.
    static float[] Lerp(float[] value1, float[] value2, float weight)
    // Interpolates between given values using linear interpolation.
    static Vector4 Lerp(Vector4 value1, Vector4 value2, float weight)
    // Interpolates between given values using linear interpolation.
    static Vector3 Lerp(Vector3 value1, Vector3 value2, float weight)
    // Interpolates between given values using linear interpolation.
    static Vector2 Lerp(Vector2 value1, Vector2 value2, float weight)
    // Interpolates between given values using linear interpolation.
    static float Lerp(float value1, float value2, float weight)
    // Selects the larger values of a and b.
    static Vector4 Maximum(Vector4 a, Vector4 b)
    // Selects the larger values of a and b.
    static Vector3 Maximum(Vector3 a, Vector3 b)
    // Selects the larger values of a and b.
    static Vector2 Maximum(Vector2 a, Vector2 b)
    // Selects the larger values of a and b.
    static float Maximum(float a, float b)
    // Selects the larger values of a and b.
    static int Maximum(int a, int b)
    // Selects the smaller values of a and b.
    static Vector4 Minimum(Vector4 a, Vector4 b)
    // Selects the smaller values of a and b.
    static Vector3 Minimum(Vector3 a, Vector3 b)
    // Selects the smaller values of a and b.
    static Vector2 Minimum(Vector2 a, Vector2 b)
    // Selects the smaller values of a and b.
    static float Minimum(float a, float b)
    // Selects the smaller values of a and b.
    static int Minimum(int a, int b)
    // Computes the nearest power-of-two value that is greater or equal to a given value.
    static int NextPower2(int v)
    // Normalizes a given value to be unit length.
    static Plane Normalize(Plane a)
    // Normalizes a given value to be unit length.
    static Quaternion Normalize(Quaternion a)
    // Normalizes a given value to be unit length.
    static Vector4 Normalize(Vector4 a)
    // Normalizes a given value to be unit length.
    static Vector3 Normalize(Vector3 a)
    // Normalizes a given value to be unit length.
    static Vector2 Normalize(Vector2 a)
    // Normalizes a given value to be unit length.
    static float Normalize(float a)
    // The outer product of two coordinate vectors is a matrix
    static Matrix4x4 Outer(Vector3 a, Vector3 b)
    // Returns a specified value raised to the specified power.
    static Vector4 Pow(Vector4 a, float power)
    // Returns a specified value raised to the specified power.
    static Vector3 Pow(Vector3 a, float power)
    // Returns a specified value raised to the specified power.
    static Vector2 Pow(Vector2 a, float power)
    // Returns a specified value raised to the specified power.
    static float Pow(float a, float power)
    // Converts a given degree value into radians.
    static float Radians(float degrees)
    // Returns a new random number between the closed range [0,1].
    static float Random()
    // Returns a new random number between a closed range [0, maximum].
    static float Random(float maximum)
    // Returns a new random number between a closed range [0, maximum].
    static Vector2 Random(Vector2 maximum)
    // Returns a new random number between a closed range [0, maximum].
    static Vector3 Random(Vector3 maximum)
    // Returns a new random number between a closed range [0, maximum].
    static Vector4 Random(Vector4 maximum)
    // Rounds a given value to the nearest integer.
    static Vector4 Round(Vector4 a)
    // Rounds a given value to the nearest integer.
    static Vector3 Round(Vector3 a)
    // Rounds a given value to the nearest integer.
    static Vector2 Round(Vector2 a)
    // Rounds a given value to the nearest integer.
    static float Round(float value)
    // Rounds a given value to the nearest interval point (Round(a / interval) * interval).
    static Vector4 Round(Vector4 a, float interval)
    // Rounds a given value to the nearest interval point (Round(a / interval) * interval).
    static Vector3 Round(Vector3 a, float interval)
    // Rounds a given value to the nearest interval point (Round(a / interval) * interval).
    static Vector2 Round(Vector2 a, float interval)
    // Rounds a given value to the nearest interval point (Round(a / interval) * interval).
    static float Round(float a, float interval)
    // Saturates a given value to be between closed interval [0,1].
    static Vector4 Saturate(Vector4 a)
    // Saturates a given value to be between closed interval [0,1].
    static Vector3 Saturate(Vector3 a)
    // Saturates a given value to be between closed interval [0,1].
    static Vector2 Saturate(Vector2 a)
    // Saturates a given value to be between closed interval [0,1].
    static float Saturate(float a)
    // Returns the sine of a given value.
    static Vector4 Sin(Vector4 a)
    // Returns the sine of a given value.
    static Vector3 Sin(Vector3 a)
    // Returns the sine of a given value.
    static Vector2 Sin(Vector2 a)
    // Returns the sine of a given value.
    static float Sin(float a)
    // Returns the hyperbolic sine of a given value.
    static Vector4 Sinh(Vector4 a)
    // Returns the hyperbolic sine of a given value.
    static Vector3 Sinh(Vector3 a)
    // Returns the hyperbolic sine of a given value.
    static Vector2 Sinh(Vector2 a)
    // Returns the hyperbolic sine of a given value.
    static float Sinh(float a)
    // Interpolates between given values using spherical interpolation.
    static Quaternion Slerp(Quaternion value1, Quaternion value2, float weight)
    // Interpolates between given values using Smoothstep interpolation.
    static Vector4 SmoothStep(Vector4 value1, Vector4 value2, float weight)
    // Interpolates between given values using Smoothstep interpolation.
    static Vector3 SmoothStep(Vector3 value1, Vector3 value2, float weight)
    // Interpolates between given values using Smoothstep interpolation.
    static Vector2 SmoothStep(Vector2 value1, Vector2 value2, float weight)
    // Interpolates between given values using Smoothstep interpolation.
    static float SmoothStep(float value1, float value2, float weight)
    // Returns the square root of a given value.
    static Vector4 Sqrt(Vector4 a)
    // Returns the square root of a given value.
    static Vector3 Sqrt(Vector3 a)
    // Returns the square root of a given value.
    static Vector2 Sqrt(Vector2 a)
    // Returns the square root of a given value.
    static float Sqrt(float a)
    // Returns the sum of the components in a given value.
    static float Sum(Plane a)
    // Returns the sum of the components in a given value.
    static float Sum(Quaternion a)
    // Returns the sum of the components in a given value.
    static float Sum(Vector4 a)
    // Returns the sum of the components in a given value.
    static float Sum(Vector3 a)
    // Returns the sum of the components in a given value.
    static float Sum(Vector2 a)
    // Returns the sum of the components in a given value.
    static float Sum(float a)
    // Returns the tangent of a specified value, measured in radians.
    static Vector4 Tan(Vector4 a)
    // Returns the tangent of a specified value, measured in radians.
    static Vector3 Tan(Vector3 a)
    // Returns the tangent of a specified value, measured in radians.
    static Vector2 Tan(Vector2 a)
    // Returns the tangent of a specified value, measured in radians.
    static float Tan(float a)
    // Calculates the integral part of a given value.
    static Vector4 Trunc(Vector4 a)
    // Calculates the integral part of a given value.
    static Vector3 Trunc(Vector3 a)
    // Calculates the integral part of a given value.
    static Vector2 Trunc(Vector2 a)
    // Calculates the integral part of a given value.
    static float Trunc(float a)
    // Represents a mathematical constant whose value is half the PI.
    static float HalfPI
    // Represents a mathematical constant whose value is the ratio of a circle's circumference to its diameter in Euclidean space.
    static float PI
    // Represents a mathematical constant whose value is quarter the PI.
    static float QuarterPI
    // Represents a value for which all smaller absolute values are considered equal to zero.
    static float ZeroTolerance
  static class MathExtensions
    static Quaternion Divide(Quaternion value, float scale)
    static Plane Divide(Plane value1, Plane value2)
    static Plane Divide(Plane value, float scale)
    static Quaternion Multiply(Quaternion value, float scale)
    static Plane Multiply(Plane value1, Plane value2)
    static Plane Multiply(Plane value, float scale)
    // 4x4 Matrix class for 3D linear algebra. All: Col1 Col2 Col3 Col4 [M11 M12 M13 M14] Row1 [M21 M22 M23 M24] Row2 [M31 M32 M33 M34] Row3 [M41 M42 M43 M44] Row4 Rotation: [M11 M12 M13 - ] Right/X [M21 M22 M23 - ] Up/Y [M31 M32 M33 - ] Forward/Z [ - - - - ] Scale: [M11 - - - ] Scale Right/X [ - M22 - - ] Scale Up/Y [ - - M33 - ] Scale Forward/Z [ - - - - ] Translation: [ - - - - ] [ - - - - ] [ - - - - ] [M41 M42 M43 - ] Translation Right/X Up/Y Forward/Z
    static void Set(ref Matrix4x4 m, Vector3 x, Vector3 y, Vector3 z, Vector3 p)
    static Vector2 XY(Vector4 value)
    static Vector2 XY(Vector3 value)
    static Vector3 XYZ(Vector4 value)
    static Vector2 XZ(Vector3 value)
    static Vector2 YX(Vector4 value)
    static Vector2 YX(Vector3 value)
    static Vector2 YZ(Vector3 value)
    static Vector2 ZX(Vector3 value)
    static Vector3 ZXY(Vector4 value)
    static Vector2 ZY(Vector3 value)
  class OneEuroFilter
    ctor(float freq = 10, float mincutoff = 1, float beta = 0, float dcutoff = 1)
    float PrevValue { get; }
    float Value { get; }
    float Filter(float value, float timestamp = -1)
    void UpdateParams(float freq, float mincutoff = 1, float beta = 0, float dcutoff = 1)
  // Describes the result of an intersection with a plane in three dimensions.
  enum PlaneIntersectionType
    Back
    Front
    Intersecting
  // Represents a three dimensional line based on a point in space and a Direction.
  struct Ray : IEquatable<Ray>
    // Initializes a new ray with given Position and Direction values.
    ctor(Vector3 position, Vector3 direction)
    // The normalized Direction in which the ray points.
    Vector3 Direction
    // The Position in three dimensional space where the ray starts.
    Vector3 Position
