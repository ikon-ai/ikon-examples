# App API Reference

## App API Reference

Full API reference for Ikon.App and Ikon.Common.

---

# Ikon.App Public API

namespace Ikon.App
  // Attribute that decorates app classes to configure their connection and messaging behavior
  sealed class AppAttribute : Attribute
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL, GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL, GROUP_APP_LOCAL, string[]? dependencies = null)
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
  // A lightweight HTTP and WebSocket endpoint host built on ASP.NET Core. Construct the host, register routes with MapGet / MapPost / MapWebSocket , and call StartAsync to allocate the relay tunnel and begin serving requests.
  sealed class AppEndpointHost : IAsyncDisposable
    // Creates a new HTTP/WebSocket endpoint host. The relay tunnel is not allocated until StartAsync is called.
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    // True once the relay tunnel is allocated and PublicUrl can be read. False before StartAsync , and after it when the relay was unreachable — the host then serves on LocalPort only and retries the allocation in the background; subscribe to PublicUrlAvailable to learn when the tunnel comes up.
    bool HasPublicUrl { get; }
    // The local port Kestrel binds to. Available after StartAsync completes.
    int LocalPort { get; }
    // Invoked once per inbound HTTP/WebSocket request before it is routed. Used to mark external activity (e.g. reset the server's idle timer) so an endpoint-served instance isn't reaped while it is serving traffic. Null = no hook.
    Action? OnRequest { get; set; }
    // The public URL for this endpoint. Available once the relay tunnel is allocated — normally when StartAsync completes; check HasPublicUrl when the relay may be down.
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
    // Allocates the relay tunnel, starts Kestrel with the registered routes, and returns immediately while the host continues to run in the background. When the relay tunnel cannot be allocated (relay not configured, backend unreachable), Kestrel still starts on a locally picked port and the tunnel allocation is retried in the background — local traffic keeps working, and PublicUrlAvailable fires once the tunnel comes up.
    Task StartAsync(CancellationToken cancellationToken = default)
    // Stops the endpoint host gracefully. Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = default)
    // Raised with the public URL when the background retry allocates the relay tunnel after StartAsync completed without one. Not raised when the tunnel was allocated during StartAsync itself — read PublicUrl directly in that case.
    event Action<string>? PublicUrlAvailable
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own schema/*.tp files (compiled by ikon app teleport build); each carries its own GROUP_APP_LOCAL opcode and is sent/received as a native type — no JSON marshalling. Delivery is server-controlled and explicit: SendMessageAsync always takes the recipient client session IDs — there is no implicit broadcast to every client. Whether a type travels reliably or unreliably is declared on the .tp schema (unreliable = true), not here.
  static class AppMessaging
    // Subscribe to inbound app messages of type T (filtered by the type's opcode). The handler receives the decoded native payload and the sender's client session ID. Dispose the returned handle to unsubscribe.
    static IDisposable OnMessage<T>(IMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    // Send a typed app message to the given client session IDs. The server decides the recipients — pass the explicit target list (e.g. every current client, everyone-but-the-sender, or a single client).
    static ValueTask SendMessageAsync<T>(IMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    // Send a typed app message to a single client.
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
    ValueTask SendAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Sends audio data through the default speech mixer.
    void SendSpeech(AudioChunk audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Generate speech for text and play it to listeners. The verbose form
    // _speechCts?.Cancel();
    // _speechCts = new CancellationTokenSource();
    // Audio.SpeechMixer.FadeOut();
    // using var generator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
    // var config = new SpeechGeneratorConfig { Text = text, VoiceId = voiceId };
    // await foreach (var audio in generator.GenerateSpeechAsync(config, _speechCts.Token))
    // {
    //     Audio.SendSpeech(audio);
    // }
    // becomes
    // await Audio.SpeakAsync(text);
    // Each call interrupts the previous one — it fades out whatever is still playing and cancels the previous call's generation, which is what a voice app almost always wants (a new reply supersedes the old one). Uses ElevenFlash25 by default — cheap+fast, the platform's go-to tier for conversational TTS. Hand-roll the SpeechGenerator + SendSpeech loop instead when you need custom mixing (overlapping speakers), speech that must not interrupt what is already playing, raw access to the generated samples (duration math, waveform analysis), or generator config beyond text and voice (language, instructions, speed).
    Task SpeakAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
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
  // Information about an output audio stream
  class AudioOutputStreamInfo : IEquatable<AudioOutputStreamInfo>
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
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    // The contact's email addresses.
    IReadOnlyList<string> Emails { get; init; }
    // The contact's names.
    IReadOnlyList<string> Names { get; init; }
    // The contact's phone numbers.
    IReadOnlyList<string> Phones { get; init; }
  // Provides convenient access to pre-agreed client-side functions. These functions are registered by clients (e.g., TypeScript SDK) and can be called from the server. Every function targets the calling client resolved from the current reactive scope by default; pass targetId to address another client session.
  static class ClientFunctions
    // Captures a single image from the client's camera.
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Requests the client to exit fullscreen mode.
    static Task<bool> ExitFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current battery level on the client.
    static Task<int?> GetBatteryLevelAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the browser language preference from the client.
    static Task<string?> GetLanguageAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current GPS location from the client.
    static Task<ClientLocation?> GetLocationAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the list of available media input devices on the client.
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current network connection type on the client.
    static Task<string?> GetNetworkTypeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Reads the client's current notification permission state.
    static Task<NotificationPermission> GetNotificationPermissionAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the currently selected UI theme from the client as its wire string. To branch on dark versus light for the calling client, prefer IsDarkTheme on the client's Context — it needs no round-trip to the client.
    static Task<string?> GetThemeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the browser timezone from the client.
    static Task<string?> GetTimezoneAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current browser URL path and query string from the client.
    static Task<string?> GetUrlAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current page visibility state on the client.
    static Task<string?> GetVisibilityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Prevents or allows the screen to sleep on the client.
    static Task<bool> KeepScreenAwakeAsync(bool enabled, int? targetId = null, CancellationToken cancellationToken = default)
    // Prompts the client to show its login UI (deferred login flow).
    static Task<bool> LoginShowAsync(string? reason = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Clears the auth session and reloads the page, returning the client to the login screen.
    static Task<bool> LogoutAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Opens an external URL in a new browser tab on the client.
    static Task<bool> OpenExternalUrlAsync(string url, int? targetId = null, CancellationToken cancellationToken = default)
    // Plays a sound on the client from a URL.
    static Task<string?> PlaySoundAsync(string url, double volume = 1, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Plays a sound on the client from a byte array. The sound data is cached per session, so subsequent calls with the same data will not re-transmit the audio.
    static Task<string?> PlaySoundAsync(byte[] data, string mimeType, double volume = 1, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Requests the client to enter fullscreen mode.
    static Task<bool> RequestFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Scrolls the page to a specific position on the client.
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the UI theme on the client.
    static Task<bool> SetThemeAsync(Theme theme, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the UI theme on the client by its wire name. Prefer SetThemeAsync for the built-in dark and light themes; this overload exists for custom theme names.
    static Task<bool> SetThemeAsync(string themeName, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the browser URL without triggering a page reload.
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Shows a notification on the client. The client requests notification permission lazily on the first send before displaying. Returns the client's resulting permission state.
    static Task<NotificationPermission> ShowNotificationAsync(NotificationContent content, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts audio capture on the client from the microphone.
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts video capture on the client from camera or screen.
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a media capture on the client by its stream ID.
    static Task<bool> StopCaptureAsync(string streamId, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a playing sound on the client.
    static Task<bool> StopSoundAsync(string playbackId, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices.
    static Task<bool> VibrateAsync(string pattern, int? targetId = null, CancellationToken cancellationToken = default)
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
    ctor(Context clientContext)
    // Gets the context of the client that joined.
    Context ClientContext { get; }
    // Gets the session ID of the client that joined.
    int ClientSessionId { get; }
    // Gets the user ID of the client that joined, or an empty string if not authenticated.
    string UserId { get; }
  // Event arguments for the ClientLeftAsync event.
  class ClientLeftEventArgs : EventArgs
    ctor(Context clientContext)
    // Gets the context of the client that left.
    Context ClientContext { get; }
    // Gets the session ID of the client that left.
    int ClientSessionId { get; }
    // Gets the user ID of the client that left, or an empty string if not authenticated.
    string UserId { get; }
  // Represents a geolocation with latitude, longitude, and accuracy in meters.
  sealed class ClientLocation : IEquatable<ClientLocation>
    ctor(double Latitude, double Longitude, double Accuracy)
    // The accuracy of the coordinates in meters.
    double Accuracy { get; init; }
    // The latitude coordinate.
    double Latitude { get; init; }
    // The longitude coordinate.
    double Longitude { get; init; }
  // Represents a media input device available on the client.
  sealed class ClientMediaDevice : IEquatable<ClientMediaDevice>
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
    // Get typed custom attributes from profile
    TAttributes GetAttributes<TAttributes>() where TAttributes : IProfileAttributes, new()
    // Check if user has a specific built-in role
    bool HasRole(UserRole role)
    // Check if user has a specific role by string name
    bool HasRole(string role)
    // Check if user has a specific role from a custom enum
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
    // Get typed custom attributes for a client, loading the profile on a cache miss. Returns null if the client has no profile.
    Task<TAttributes> GetAttributesAsync<TAttributes>(Context clientContext) where TAttributes : IProfileAttributes, new()
    // Get a client's profile, loading it from the backend on a cache miss and caching the result. Connected clients are normally already cached (their profile is loaded when they join), so this usually returns instantly and only hits the backend for an uncached user. Returns null when the context carries no UserId or the backend has no profile for it.
    Task<ClientProfile?> GetProfileAsync(Context clientContext)
    // Get a profile by userId, loading it from the backend on a cache miss.
    Task<ClientProfile?> GetProfileAsync(string userId)
    // Check if client has a specific built-in role
    bool HasRole(Context clientContext, UserRole role)
    // Check if client has a specific role by string name
    bool HasRole(Context clientContext, string role)
    // Check if client has a specific role from a custom enum
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
    // Set custom attributes for a client
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
    Task DeleteAsync(string id, CancellationToken ct = default)
    // Streams a decrypted attachment from the platform. The returned EmailAttachmentDownload owns the content stream — dispose it (e.g. await using) when done.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = default)
    // Lazily enumerates all received emails matching query , transparently following pages until exhausted. Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single page of received emails for the app's space. Paginate by passing the returned NextCursor back as Cursor .
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single inbound email with decrypted body and parsed envelope.
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = default)
    // Sends a custom HTML email through the platform mailer. The platform sets the visible From address; pass ReplyTo to direct replies elsewhere. The send is enqueued for asynchronous delivery — a successful return means the platform has accepted the request, not that the recipient has received the message. Transient delivery failures are retried server-side. The total payload size (subject, body, attachments, metadata) is capped at roughly 10 MB.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
  // Shared base for the two developer-facing inbound HTTP surfaces, [Rest] and [Mcp]. They differ only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients; addressing, path templating, identity binding, auth, and abuse-control are identical and live here so there is exactly one place to reason about them.
  abstract class EndpointAttribute : Attribute
    // Built-in authorization for this endpoint, resolved at the gateway edge before (and without) provisioning the app. Defaults to Grant (a signed grant URL). Set AuthPolicy instead to name a custom /router/ policy.
    EndpointAuth Auth { get; init; }
    // Name of a custom /router/ edge policy that authorizes this endpoint (an apiKey/hmac/ipAllow helper you defined in router/index.ts). When set (non-empty) it takes precedence over Auth . Authorization lives in /router/, the single auth surface — not in C#.
    string? AuthPolicy { get; init; }
    // External path under the space domain (after {space}.ikonai.app/api). Optional: when omitted (empty) the path is derived from the method name (kebab-cased) — /{method} on the app class, /{cell-type}/{method} on a cell. A leading-slash path is absolute; a relative form ("bump") is resolved against the owner's auto-derived mount point at build time. Route params use {name} syntax. A {name} whose name matches a field of the owner's SessionIdentity record binds into the routing identity (the extrinsic resource the caller names); other {name} segments bind as ordinary handler parameters. Reserved paths the developer must NOT declare: /.well-known/* (RFC), and the /ikon/* + /api subtrees (platform-owned).
    string Path { get; }
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
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  // Typed return value from an HttpMethodAttribute -annotated method. Endpoints can return any serializable type for an automatic 200 + JSON response, or return an HttpResult when they need control over status code, content type, or custom body serialization.
  sealed class HttpResult : IEquatable<HttpResult>
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
    // The loopback endpoint (host + HTTPS port) of THIS instance's own local server, but ONLY when the server's own URL is a localhost address — i.e. local dev WITHOUT --public-access. This lets an in-process client (e.g. a simulated player, a self-test harness) connect directly over loopback to this exact process instead of routing through the relay. It returns null when the instance is exposed via the relay (--public-access) or runs in the cloud — there the server's own URL is the relay/space URL, a direct socket can't (and shouldn't) reach it, and callers should use the normal relay/ApiKey connect path (which routes to this registered serving instance) instead. The default is null for hosts that don't run a local server; IApp overrides it.
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
    // The app's public URL — the address a browser opens to join this app instance's channel. Replaces the app.ReactiveGlobalState.ChannelUrl.Value incantation; reading it inside UI code subscribes to changes the same way. For a URL with query parameters (e.g. a session join link) use JoinUrl .
    string PublicUrl { get; }
    // Gets the reactive wrapper around GlobalState that provides change notifications.
    ReactiveGlobalState ReactiveGlobalState { get; }
    // Gets the secrets (tokens, API keys, passwords) configured for this app. Values are fetched from the Ikon backend once at app startup and exposed synchronously; changes made via ikon app secret set while the app is running only take effect after a restart.
    Secrets Secrets { get; }
    // Whether this app instance offers the raw UDP / UDP-DTLS transports to connecting clients. Enabled by default. Set to false to disable them. Like WebRtcEnabled this takes effect for clients that connect after it is set (the transports are no longer advertised); already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    // Whether this app instance offers WebRTC transport to connecting clients. Enabled by default. Set to false (e.g. in Main) to disable WebRTC for apps that don't use audio/video or low-latency data — WebRTC peer setup (ICE candidate gathering, DTLS) is a notable per-client memory and allocation cost. Takes effect for clients that connect after it is set: the server stops advertising WebRTC and ignores WebRTC signaling, so no per-client peer state is created. Already-connected clients keep their channels until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Creates a platform-managed eID-backed PAdES signature order for the supplied document(s). The platform navigates the signer's browser to the signing-ceremony URL through the existing client UI surface, awaits the asynchronous packaging completion, and resolves the returned task with the signed PDF and evidence metadata. The returned bytes are the long-term-validation PAdES PDF when the chosen scheme produces it; apps should persist them as the system of record because the platform's session retention is short.
    abstract Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default)
    // Creates a DbConnection for one of the app's configured databases (the Databases list in the app's env-specific ikon-config toml, applied with ikon app config and surfaced via Databases ) by name; the caller opens and disposes it: await using var connection = app.Database("mydb");.
    virtual DbConnection Database(string databaseName)
    // Build a shareable link to this app: PublicUrl plus a query string built from queryParams — an anonymous object (or a string dictionary), following the identity-by-anonymous-object shape of MintUrlAsync . Each readable property becomes a URL-encoded name=value pair; null-valued properties are skipped. So app.JoinUrl(new { id = sessionId }) yields {PublicUrl}?id={sessionId}. Replaces hand-assembling $"{app.ReactiveGlobalState.ChannelUrl.Value}?id={sessionId}". Passing null returns PublicUrl as-is.
    virtual string JoinUrl(object? queryParams = null)
    // Mint a working, identity-bound URL for one endpoint — the single way to get a callable URL for a grant (default) or policy endpoint. You identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), NOT by its URL path — the path is often derived from the method name (and may be templated), so the path is what minting RETURNS, not what you pass in. The returned URL is the endpoint's PublicUrl with any pinned {placeholder} path segments substituted and a signed ?ikon-grant= appended. identity (an anonymous object, e.g. new { DocumentId = "doc-42" }, or a string dictionary) PINS those identity fields into the grant; fields you omit stay open {captures} for the caller to fill. Omitting identity entirely ( null ) pins THIS instance's own session identity, so the URL routes back to this app instance — the common case. Grants are non-expiring by default — pass expiresIn only for an ephemeral link, and an optional group to revoke a batch together via RevokeGroupAsync . Re-minting the same stable (non-expiring) URL returns an identical URL, so it survives restarts.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // Mint working URLs for several endpoints sharing one pinned identity, in a single backend round-trip. Returns a map keyed by the endpoints you passed. See MintUrlAsync .
    virtual Task<IReadOnlyDictionary<string, MintedUrl>> MintUrlsAsync(IEnumerable<string> endpoints, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // Dynamically requests a raw TCP/TLS/UDP endpoint. Returns a RelayEndpoint whose LocalPort a listener should bind to; the endpoint is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the returned endpoint to release it. For HTTP/HTTPS endpoints use AppEndpointHost .
    abstract Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", int localPort = 0, CancellationToken ct = default)
    // Requests a fresh strong-authentication step-up challenge for the current user. Navigates the client browser to the platform's configured identity provider through the existing client UI surface, waits for the user to complete the challenge, and returns the platform-signed step-up assertion JWT. Apps must verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier .
    abstract Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = default)
    // Revoke every URL minted under a shared group tag.
    virtual Task RevokeGroupAsync(string group, CancellationToken ct = default)
    // Revoke a single minted URL by its GrantId .
    virtual Task RevokeUrlAsync(string grantId, CancellationToken ct = default)
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
    // Subscribe to ClientJoinedAsync with a handler that receives both the joining client's Context AND its typed TClientParameters . Replaces the awkward app.Clients[ctx.SessionId]!.Parameters drill inside the handler body.
    static void OnClientJoined<TSessionIdentity, TClientParameters>(IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to ClientLeftAsync with a handler that receives the departing client's Context directly.
    static void OnClientLeft(IAppBase app, Func<Context, Task> handler)
    // Subscribe to ClientLeftAsync with a handler that receives both the departing client's Context AND its typed TClientParameters .
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
  // Collection interface for accessing connected clients. Iterable for the common "broadcast / fan-out" pattern (`foreach (var client in app.Clients)`), indexable by session ID for direct lookups, and exposes Ids when only the connected-session-ids are needed.
  interface IClientCollection<TClientParameters> : IEnumerable, IEnumerable<IClient<TClientParameters>>
    // Gets the number of currently connected clients.
    int Count { get; }
    // Gets the connected client session IDs as an enumerable. Convenience for code that just needs the IDs without the full client objects — e.g. `foreach (var id in app.Clients.Ids) { _scores[id] = 0; }`.
    IEnumerable<int> Ids { get; }
    // Gets the client with the specified session ID, or null if not found.
    IClient<TClientParameters>? this[int clientSessionId] { get; }
    // Alias for Ids — dictionary-shaped mental model. Generated code reaches for both interchangeably.
    IEnumerable<int> Keys { get; }
  // Interface representing a connected client with typed parameters.
  interface IClient<TClientParameters>
    // Gets the typed parameters for this client.
    TClientParameters Parameters { get; }
    // Gets the session id of this client — the same id used to index IClientCollection and to target client-directed APIs.
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
    ctor(ProtocolMessage message)
    // Gets the received protocol message.
    ProtocolMessage Message { get; }
  // A minted endpoint URL: the working Url (the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended), the GrantId to revoke it by, and the optional ExpiresAt when a TTL was requested (grants are non-expiring by default).
  sealed class MintedUrl : IEquatable<MintedUrl>
    ctor(string Url, string GrantId, DateTimeOffset? ExpiresAt)
    DateTimeOffset? ExpiresAt { get; init; }
    string GrantId { get; init; }
    string Url { get; init; }
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
  // Content of a user-facing notification surfaced on the client device (browser notification on the web, OS notification on Flutter native apps).
  sealed class NotificationContent : IEquatable<NotificationContent>
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
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    // Reads a client's current notification permission state without sending anything.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    // Shows a notification on a single connected client session. The client requests notification permission lazily (on this first send) before displaying. Returns the per-session delivery and permission outcome.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // Shows a notification on every currently-connected session belonging to userId (a user may be connected from several devices). When the user has no connected session, falls back to offline push — an OS notification delivered through the backend push hub. Returns one result per targeted session (empty when the user was offline and only push was attempted).
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = default)
  // A ReactiveList persisted globally for the app within its space. Shared across all session identities and users; one list per app deployment.
  class PersistentReactiveList<T> : ReactiveList<T>, IPersistedReactive
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
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
  // A ReactiveList persisted per user, partitioned at runtime by UserScope . Each user sees their own list across all of their client sessions.
  class PersistentUserReactiveList<T> : ReactiveList<T>, IPersistedReactive
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null, string file = "", string member = "")
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
  // Exception thrown when a required role is missing.
  class RoleRequiredException : Exception
    ctor(string role, string? userId = null)
    string RequiredRole { get; }
    string? UserId { get; }
  // Event arguments raised when speech has been recognized from a captured audio stream.
  sealed class SpeechRecognizedEventArgs : EventArgs
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
  // The built-in client UI themes. The wire protocol carries the theme as a string (custom theme names are allowed via SetThemeAsync ); ToThemeName maps these values to their wire names.
  enum Theme
    Dark
    Light
  // Helpers for mapping Theme values to and from the wire strings used by the client.
  static class ThemeExtensions
    // True when the client's reported theme is the dark theme. False for the light theme, custom theme names, and clients that have not reported a theme.
    static bool IsDarkTheme(Context clientContext)
    // Returns the wire name of the theme: "dark" or "light".
    static string ToThemeName(Theme theme)
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
    // Number of concurrent instances per addressable key. Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin Resolve across them. For globals (parameterless SessionIdentity) the N instances are eager-spawned at host construction — the load-balanced auth-cell pattern. For keyed cells the N instances are spawned together on first access; sharded keyed cells must tolerate eventual consistency between shards (cells should hold no per-instance state, or persist shared state through an external store).
    int Capacity { get; init; }
    // How long a keyed cell may remain idle before EvictIdle removes it from the directory. Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
    // Where this cell type is hosted. AppProcess (the default) keeps the cell in the app's own `CellHost` — every app process has its own copies, state is not shared across processes. Substrate declares that the cell should be hosted on the platform's cell-deployment substrate, where one instance per (cell-type, SessionIdentity) is shared across all app processes that connect.
    CellProcessScope ProcessScope { get; init; }
  // Where a CellAttribute -decorated type's instances live.
  enum CellProcessScope
    AppProcess
    Substrate
  // Per-server-scoped accessor (via AsyncLocalInstance — use Cells.Instance) for that server's CellHost plus the wiring substrate-cell proxies need: the endpoint-URL resolver (for [HttpGet]/[HttpPost] methods) and the cell-client factory (for [Function] methods and Reactive<T> state, which ride a standard IkonClient SDK connection to the cell-host).
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // Resolve (or spawn on first call) the cell implementation for TInterface keyed by sessionIdentity . Subsequent calls with an equal SessionIdentity return the same instance.
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    // Dispose every live cell-host connection. Call on app shutdown. Idempotent.
    ValueTask DisposeAsync()
    // Reserved key in an SDK connection's parameters that names the substrate cell type to route to. The cell's SessionIdentity-record fields ride alongside it. MUST stay in sync with the cloud's CELL_TYPE_PARAM in cell-routing.ts — that's what ChannelInstanceService.create keys on to provision a cell-host channel-instance.
    static string CellTypeParam
  // Framework handle injected into a cell's primary constructor. Exposes the SessionIdentity the cell was instantiated for; future revisions add lifetime, config, etc.
  interface ICell<TSessionIdentity>
    // The SessionIdentity record value this cell instance is keyed by.
    TSessionIdentity Identity { get; }

namespace Ikon.App.Connectors
  // Thrown when a connector's remote service returns an error response.
  sealed class ConnectorException : Exception
    ctor(string provider, string message)
    string Provider { get; }
  // Google Drive connector. Upload, download and list files with Google OAuth2 credentials. Raw — the agent skill lives in Ikon.Agent.Connectors.
  sealed class Drive
    ctor(GoogleCredentials credentials)
    Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    // Stream every file under a folder (or the whole drive), paging through the full result set. Pass an extra query clause such as "modifiedTime > '2024-01-01T00:00:00'" to bound a historical backfill by time.
    IAsyncEnumerable<DriveFile> ListAllAsync(string? folderId = null, string? extraQuery = null, CancellationToken ct = default)
    Task<IReadOnlyList<DriveFile>> ListAsync(string? folderId = null, int limit = 50, CancellationToken ct = default)
    Task<DriveFile> UploadAsync(string name, string mimeType, Stream content, string? folderId = null, CancellationToken ct = default)
  sealed class DriveFile : IEquatable<DriveFile>
    ctor(string Id, string Name, string MimeType, long? Size, string? WebViewLink, DateTimeOffset? ModifiedTime = null)
    string Id { get; init; }
    string MimeType { get; init; }
    DateTimeOffset? ModifiedTime { get; init; }
    string Name { get; init; }
    long? Size { get; init; }
    string? WebViewLink { get; init; }
  static class GoogleAuth
    static UserCredential CredentialFor(GoogleCredentials credentials, IEnumerable<string> scopes)
    // True when ex is a PERMANENT OAuth failure (revoked/expired refresh token, bad client) that retrying won't fix — the account must be reconnected. Lets connectors stop and surface a distinct "reconnect required" state instead of hammering the token endpoint forever.
    static bool IsAuthFailure(Exception ex)
  // OAuth2 credentials for Google connectors. The refresh token is long-lived; the access token is obtained and refreshed automatically by the Google client library.
  sealed class GoogleCredentials : IEquatable<GoogleCredentials>
    ctor(string ClientId, string ClientSecret, string RefreshToken)
    string ClientId { get; init; }
    string ClientSecret { get; init; }
    string RefreshToken { get; init; }
  // Slack messaging connector. Post and read messages with a bot token (xoxb-...). Raw — no agent coupling; the agent skill lives in Ikon.Agent.Connectors.
  sealed class Slack
    ctor(string botToken, HttpClient? http = null)
    Task<IReadOnlyList<SlackMessage>> HistoryAsync(string channel, int limit = 20, CancellationToken ct = default)
    Task<SlackMessage> PostAsync(string channel, string text, string? threadTs = null, CancellationToken ct = default)
  sealed class SlackMessage : IEquatable<SlackMessage>
    ctor(string Channel, string User, string Text, string Ts, string? ThreadTs = null)
    string Channel { get; init; }
    string Text { get; init; }
    string? ThreadTs { get; init; }
    string Ts { get; init; }
    string User { get; init; }
  // WhatsApp messaging connector (WhatsApp Business Cloud API via Meta Graph). Send with a system-user access token and the sender's phone number id. Raw — the agent skill lives in Ikon.Agent.Connectors.
  sealed class WhatsApp
    ctor(string accessToken, string phoneNumberId, HttpClient? http = null)
    Task<string> SendAsync(string to, string text, CancellationToken ct = default)

namespace Ikon.App.Cron
  // Per-invocation context for a CronAttribute handler currently executing. A cron handler may optionally accept one of these (and/or a CancellationToken ) to learn when and why it fired; a parameterless handler is equally valid. AsyncLocal so handler code (and anything it calls) can read it without threading it through every method signature.
  sealed class CronContext : IEquatable<CronContext>
    ctor(DateTime FireTimeUtc, string Schedule)
    // The cron context for the invocation currently running on this async flow, or null.
    static CronContext? Current { get; }
    DateTime FireTimeUtc { get; init; }
    string Schedule { get; init; }
    static IDisposable Use(CronContext context)

namespace Ikon.App.Http
  // Per-request context for an HttpMethodAttribute handler currently executing. AsyncLocal so handler code (and anything it calls) can read the request's resolved identity without threading the dict through every method signature. Relationship to other "context" concepts on the platform: SessionIdentity (the typed app/cell record): the routing / instance-partition key. Always present — it's what was used to address the channel-instance this handler runs in. Stable across the cell instance's lifetime.Context (Ikon protocol Context for WS clients): the live client *connection* — sessionId, deviceId, AuthSessionId, UserId from the connect-token. Absent for endpoint/MCP dispatches because there is no live client connection.HttpCallContext.Current (this) and McpCallContext .Current: the *request-scoped overlay* that exposes the per-call resolved identity for handler code to read. Set by the wrapper before the handler runs, cleared after. The point is that handlers reading "who is this call for?" get a non-empty answer on endpoint/MCP-dispatched calls, where the connection-level Context.UserId would be empty. The handler's SessionIdentity record (resolved by CellHost.ResolveByCellTypeName before this context is set) and HttpCallContext.Current.SessionIdentity carry the same information in different shapes: the former is typed and tied to the cell's lifetime; the latter is the raw wire dict tied to the call's lifetime. Headers and RawBody are the UNTRUSTED request inputs, exposed so a handler can do its own logic inline (e.g. verify a Stripe-Signature against the raw body) without a separate auth cell. They must never feed identity resolution — the target instance is already chosen from trusted sources (a signed ikon-grant / policy claims / platform-controlled path+query) before the handler runs, so reading a header cannot retarget the call.
  sealed class HttpCallContext : IEquatable<HttpCallContext>
    ctor(IReadOnlyDictionary<string, string>? SessionIdentity = null, CancellationToken CancellationToken = default, IReadOnlyDictionary<string, string>? Headers = null, string? RawBody = null)
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

namespace Ikon.App.Mcp
  // Per-request context for an MCP tools/call or resources/read in flight. AsyncLocal so the bridge can read it from inside parameter binding without threading another argument through every call site. Carries: The request's effective CancellationToken (linked to the transport CT and a per-request CTS the host can trip on notifications/cancelled).An optional progress sink the bridge wires IProgress parameters into. SessionIdentityFields — the authenticated identity for this request (from claims merged by the transport). Bridges pass it to CellHost.ResolveByCellTypeName so keyed cells route to the right instance. Empty / null on the stdio path (single-user process).
  sealed class McpCallContext : IEquatable<McpCallContext>
    ctor(CancellationToken CancellationToken, Func<ProgressUpdate, Task>? OnProgress, IReadOnlyDictionary<string, string>? SessionIdentityFields = null)
    CancellationToken CancellationToken { get; init; }
    static McpCallContext? Current { get; }
    Func<ProgressUpdate, Task>? OnProgress { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentityFields { get; init; }
    // Convenience accessor for the conventional userid field of the request's SessionIdentity. Returns null when no McpCallContext is current or when claims carried no userid. Mirror of UserId — same semantics across both request-scoped contexts.
    string? UserId { get; }
    static IDisposable Use(McpCallContext context)
  // One progress update emitted by a long-running tool. Progress is a monotonic counter; Total is optional but expected to stay constant across updates so clients can render a percentage. Message is freeform display text.
  sealed class ProgressUpdate : IEquatable<ProgressUpdate>
    ctor(double Progress, double? Total = null, string? Message = null)
    string? Message { get; init; }
    double Progress { get; init; }
    double? Total { get; init; }

namespace Ikon.App.Payments
  // How a PaymentEntitlement was obtained.
  enum EntitlementSource
    Unknown
    Subscription
    OneTime
  // The price for a created offer. Omit Interval for a one-time offer.
  sealed class OfferPriceSpec : IEquatable<OfferPriceSpec>
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval? Interval = null, int? IntervalCount = null)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval? Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  // Defines an offer to create via CreateOfferAsync .
  sealed class OfferSpec : IEquatable<OfferSpec>
    ctor(string OfferId, string Name, OfferPriceSpec Price)
    string Name { get; init; }
    string OfferId { get; init; }
    OfferPriceSpec Price { get; init; }
  // A single payment record (a one-off charge or a subscription renewal). OfferId is null for ad-hoc charges and records written before offer tracking.
  sealed class Payment : IEquatable<Payment>
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
  // A customer's access to an offer, whether from an active subscription or a one-time purchase. This is the access-control answer the [PaymentsRequireEntitlement] policy gates on. Subscription access carries ExpiresAt (period end plus a grace window) and reports inactive once it has passed; a one-time purchase has no expiry.
  sealed class PaymentEntitlement : IEquatable<PaymentEntitlement>
    ctor(string OfferId, bool Active, DateTimeOffset? ExpiresAt, EntitlementSource Source)
    bool Active { get; init; }
    DateTimeOffset? ExpiresAt { get; init; }
    string OfferId { get; init; }
    EntitlementSource Source { get; init; }
  // A normalized payment event the backend pushes to the app.
  sealed class PaymentEvent : IEquatable<PaymentEvent>
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
  // What a Payment paid for — a one-off charge or a subscription charge.
  enum PaymentKind
    Unknown
    OneTime
    Subscription
  // A provider-hosted page the customer is redirected to in order to pay. Send them to Url .
  sealed class PaymentLink : IEquatable<PaymentLink>
    ctor(string Url, string Reference, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    string Reference { get; init; }
    string Url { get; init; }
  // A purchasable offer in the app's catalog — recurring (subscription) or one-time, per its prices.
  sealed class PaymentOffer : IEquatable<PaymentOffer>
    ctor(string OfferId, string Name, IReadOnlyList<PaymentPrice> Prices)
    string Name { get; init; }
    string OfferId { get; init; }
    IReadOnlyList<PaymentPrice> Prices { get; init; }
  // One price on an offer. Interval and IntervalCount are meaningful only when Kind is Recurring ; a one-time price reports Unknown .
  sealed class PaymentPrice : IEquatable<PaymentPrice>
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval Interval, int? IntervalCount)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  // The payment provider that moves the money. A command uses the space's enabled provider unless it names one, either per call or by pinning DefaultProvider .
  enum PaymentProvider
    Stripe
    Mollie
    Surfboard
  // A receipt for a completed payment. Url is a provider-hosted receipt page. Pdf holds downloadable PDF bytes only when the provider exposes one; today every provider (Stripe, Surfboard) returns a hosted URL only, so Pdf is null — the field is populated when a provider offers a PDF.
  sealed class PaymentReceipt : IEquatable<PaymentReceipt>
    ctor(string? Url, byte[]? Pdf, string? PdfContentType)
    byte[]? Pdf { get; init; }
    string? PdfContentType { get; init; }
    string? Url { get; init; }
  // Result of a ReconcileAsync request. Enqueued counts the provider objects queued for re-processing; their effects arrive asynchronously as normal payment events.
  sealed class PaymentReconcileResult : IEquatable<PaymentReconcileResult>
    ctor(PaymentProvider? Provider, int Enqueued)
    int Enqueued { get; init; }
    PaymentProvider? Provider { get; init; }
  // Result of a refund.
  sealed class PaymentRefund : IEquatable<PaymentRefund>
    ctor(string Reference, RefundStatus Status)
    string Reference { get; init; }
    RefundStatus Status { get; init; }
  // The outcome of a Payment .
  enum PaymentStatus
    Unknown
    Pending
    Paid
    Failed
    Canceled
  // A customer's live subscription, created by paying for a recurring offer.
  sealed class PaymentSubscription : IEquatable<PaymentSubscription>
    ctor(string Id, PaymentProvider? Provider, SubscriptionStatus Status, string? OfferId, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd)
    bool CancelAtPeriodEnd { get; init; }
    DateTimeOffset? CurrentPeriodEnd { get; init; }
    string Id { get; init; }
    string? OfferId { get; init; }
    PaymentProvider? Provider { get; init; }
    SubscriptionStatus Status { get; init; }
  // Declares the function requires the current customer to hold an active entitlement for offerId — access granted by an active subscription or a one-time purchase. Resolves the customer from UserId and reads the entitlement from Instance . On missing access it DENIES with a stable code (payments_entitlement_required); the app's UI catches it and opens a payment link via CreatePaymentLinkAsync . The provider webhook then flips the entitlement and the user retries.
  sealed class PaymentsRequireEntitlementAttribute : PolicyAttribute
    ctor(string offerId)
    // Offer the entitlement is keyed to.
    string OfferId { get; }
    override IFunctionPolicy CreatePolicy()
  // App-level entry point for payments, reached via app.Payments. The app creates payment links (for an offer or an ad-hoc amount) and reacts to PaymentEventReceived events. Every command accepts an optional per-call provider override; when none is given the backend uses the space's enabled provider. The app holds no payment state. One instance per app (an AsyncLocalInstance singleton).
  sealed class PaymentsService : AsyncLocalInstance<PaymentsService>
    ctor()
    // Default cancel URL used when a command does not specify one.
    string? DefaultCancelUrl { get; set; }
    // Optional provider to use when a command does not specify one. Left null by default: the SDK then sends no provider and the backend charges with the space's enabled (default) provider. Set this only to pin a specific provider for an app that has more than one enabled.
    PaymentProvider? DefaultProvider { get; set; }
    // Default success URL used when a command does not specify one.
    string? DefaultSuccessUrl { get; set; }
    // Cancel a subscription at the period end (default) or right away with immediate . The entitlement lapses when the cancellation takes effect.
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Create (or update) an offer in the app's catalog so customers can pay for it by id. For Stripe this provisions a Product + Price; for providers without a catalog (Mollie, Surfboard) the offer is stored by the platform. Idempotent on OfferId .
    Task<PaymentOffer> CreateOfferAsync(OfferSpec offer, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Create a provider-hosted payment link for an offer. Recurring offers start a subscription; paying grants an entitlement. customerKey defaults to the current user.
    Task<PaymentLink> CreatePaymentLinkAsync(string offerId, string? customerKey = null, string? email = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Create a provider-hosted payment link for an ad-hoc amount (tip, one-off charge). Grants no entitlement — use an offer for that. customerKey defaults to the current user.
    Task<PaymentLink> CreatePaymentLinkAsync(long amountMinor, string currency, string? customerKey = null, string? description = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // The customer's access to an offer (a backend call). Used by the [PaymentsRequireEntitlement] policy. customerKey defaults to the current user. For gating UI, prefer the synchronous IsEntitled .
    Task<PaymentEntitlement> GetEntitlementAsync(string offerId, string? customerKey = null, CancellationToken cancellationToken = default)
    // Synchronous, cache-backed access check for gating UI — no backend call, safe to read every render. Reading it inside a UI lambda re-renders when the entitlement changes (after a purchase or a pushed event). customerKey defaults to the current user. The first read for an unseen offer returns false and warms the cache in the background, flipping to the real value on the next render.
    bool IsEntitled(string offerId, string? customerKey = null)
    // The app's catalog of purchasable offers.
    Task<IReadOnlyList<PaymentOffer>> ListOffersAsync(CancellationToken cancellationToken = default)
    // The customer's payments. customerKey defaults to the current user.
    Task<IReadOnlyList<Payment>> ListPaymentsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // The customer's subscriptions. customerKey defaults to the current user.
    Task<IReadOnlyList<PaymentSubscription>> ListSubscriptionsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // Ask the backend to re-pull live provider state — the recovery path when a provider webhook was missed or the app was offline when an event was pushed. Eventually consistent: the pulled objects flow through the normal pipeline and surface as ordinary PaymentEventReceived pushes and entitlement refreshes within seconds. With a reference (a payment link's checkout-session reference or a subscription id) only that object is pulled; otherwise the customer's recent objects; with neither and no current user in scope, the space's recent window.
    Task<PaymentReconcileResult> ReconcileAsync(string? customerKey = null, string? reference = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Refund a payment, in full by default or partially via amountMinor . Refunding does not revoke an entitlement the payment granted.
    Task<PaymentRefund> RefundAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Remove an offer from the app's catalog (Stripe archives the Product/Price). Returns false if no such active offer existed.
    Task<bool> RemoveOfferAsync(string offerId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Fetch a receipt for a completed payment. Url is a provider-hosted receipt page (present for Stripe and Surfboard). Pdf carries downloadable PDF bytes only when the provider offers one; today both providers return a hosted URL only, so it is null.
    Task<PaymentReceipt> RequestReceiptAsync(string paymentId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Raised for each normalized payment event the backend pushes (paid, refunded, subscription renewed/canceled). Subscribing registers the receiver on first use.
    event Func<PaymentEvent, Task>? PaymentEventReceived
  // The billing interval of a recurring price.
  enum PriceInterval
    Unknown
    Day
    Week
    Month
    Year
  // Whether a price bills once or on a recurring interval.
  enum PriceKind
    Unknown
    OneTime
    Recurring
  // The state of a PaymentRefund .
  enum RefundStatus
    Unknown
    Pending
    Succeeded
    Failed
  // The lifecycle state of a PaymentSubscription .
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
    // Per-cell entry points discovered in the bundle. The cloud uses this list to: (a) recognise URLs of the form /api/{CellType}/{path} and route them to a cell-instance (provisioned the same way an app-instance is, just with AppInitializationArgs.RunTarget = "{CellType}" instead of null); (b) hash the request's identity query params against the cell's IdentityFields to find-or-create the right channel-instance for that cell identity. Empty for apps without cells, or for cells whose ProcessScope is AppProcess (in-process — no separate cell-instance needed). See docs/private/endpoint-architecture.md.
    List<AppBundleConfig.CellEntry> Cells { get; set; }
    string ChannelId { get; set; }
    string CreatedAt { get; set; }
    List<AppBundleConfig.CronJobDescriptor> CronJobs { get; set; }
    List<AppBundleConfig.DatabaseEntry> Databases { get; set; }
    List<AppBundleConfig.EmailTemplate> EmailTemplates { get; set; }
    List<AppBundleConfig.EndpointDescriptor> Endpoints { get; set; }
    string Hash { get; set; }
    string Name { get; set; }
    string OrganisationId { get; set; }
    List<AppBundleConfig.Pipeline> Pipelines { get; set; }
    // The app's optional /router/ edge unit — sandboxed TypeScript that the gateway runs before (and without) provisioning the app server to authorize, resolve principal identity, and gate abuse. The tool only records presence + entry point; the backend bundles the source and extracts the exported policy names at activation. See docs/private/endpoint-orthogonal-authorization-design.md.
    AppBundleConfig.RouterConfig Router { get; set; }
    List<string> SessionIdentityKeys { get; set; }
    string SpaceId { get; set; }
    string Version { get; set; }
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
    AppProjectConfig.BootSnapshotConfig BootSnapshot { get; set; }
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
    // Generates (or removes) pubspec_overrides.yaml in the frontend-flutter directory so the app resolves ikon_sdk from the local platform-dart/ikon_sdk source while the ikon-platform repo is available, and from the published pub.dev package otherwise. The Dart analog of the C# -p:IkonRoot arg and GenerateTsconfigPathsJsonAsync . When platform is supplied (a context the CLI verb already resolved, honoring --platform-repo) it is used as-is; otherwise it falls back to the shared Resolve ladder, so a locally-built ikon tool still resolves the repo even for an app created far from it. Safe to call on every Flutter operation.
    static Task GenerateFlutterPubspecOverridesAsync(string flutterDirectory, PlatformContext? platform = null)
    // Generates tsconfig.paths.json in the frontend-node directory with appropriate TypeScript paths. Auto-detects internal/external mode based on whether the directory is inside ikon-platform. Internal mode: generates paths pointing to monorepo source files for IDE support. External mode: generates empty paths (uses node_modules).
    static Task GenerateTsconfigPathsJsonAsync(string frontendNodeDirectory)
    static AppProjectVariables GetAppProjectVars(string? targetDirectory)
    static string GetAssemblyNameFromCsproj(string csprojPath)
    static Type[] GetTypesSafely(Assembly assembly)
    static bool HasIkonAppAttribute(Type type)
    static bool IsLegacyConfig(string tomlContent)
    static AppProjectConfig MigrateFromLegacy(string tomlContent, IkonBackend.EnvironmentType environment)
    static int ScoreProjectFile(string csprojPath)
    static string StripIkonAppPrefix(string projectName)
  sealed class AppProjectVariables
    ctor()
    string ConfigFilePath { get; init; }
    string CsProjectFilePath { get; init; }
    string CsProjectName { get; init; }
    string FrontendFlutterDirectory { get; init; }
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
    void InitializeAll(IReadOnlyList<Type> explicitTypes)
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
  class AppProjectConfig.BootSnapshotConfig : ITomlMetadataProvider
    ctor()
    bool Enabled { get; set; }
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
    string Auth { get; set; }
    string HttpMethod { get; set; }
    string MethodName { get; set; }
    string Path { get; set; }
    List<AppBundleConfig.EndpointPathParam> PathParams { get; set; }
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
  class AppBundleConfig.CronJobDescriptor
    ctor()
    string Name { get; set; }
    string Owner { get; set; }
    string OwnerKind { get; set; }
    string Schedule { get; set; }
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
  class AppBundleConfig.EndpointDescriptor
    ctor()
    string Auth { get; set; }
    bool AutoRegistered { get; set; }
    bool IsMcpTool { get; set; }
    string Kind { get; set; }
    string Name { get; set; }
    string Owner { get; set; }
    string OwnerKind { get; set; }
    string? Path { get; set; }
    List<AppBundleConfig.EndpointPathParam> PathParams { get; set; }
  class AppBundleConfig.EndpointPathParam
    ctor()
    bool IsIdentity { get; set; }
    string Name { get; set; }
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
    ctor(TimeProvider? timeProvider = null)
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
  // Force-loads the Ikon.* dependency closure of an assembly so a subsequent InitializeAll discovers every [AsyncLocalInstance] type. App libraries such as Ikon.Parallax (StyleRegistry) and Ikon.App.Cells (Cells) otherwise load lazily on first use — AFTER the one-time discovery scan — and their async-local types would be missed and fall back to a single process-wide singleton shared across every in-process server (host + each embedded preview/sandbox). This deliberately lives on its own type, NOT on AsyncLocalInstances : invoking a static member of AsyncLocalInstances triggers its static constructor, which runs the discovery scan. Calling the loader from there would scan BEFORE the closure finished loading, defeating the purpose. Callers must run this BEFORE the first AsyncLocalInstances access.
  static class IkonAssemblyLoader
    static void ForceLoadIkonClosure(Assembly root)
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
  sealed class InMemoryProtocolMessageChannel : IMessageChannel, IProtocolMessageChannel
    ctor()
    Context ClientContext { get; }
    int SessionId { get; }
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
  // Convention for converting a developer-declared [HttpGet]/[HttpPost] path into the absolute external path under the space domain ({space}.ikonai.app{path}). Developers can write either absolute paths ("/billing/stripe", "/labs/{workspace}/increment") or the legacy relative form ("bump", "value" — what the cell-host's AppEndpointHost served under /{CellType}/{Method} before the inbound-unification plan landed). Relative names are auto-derived so the same endpoint flows through the same gateway path under both forms: On the app class: "bump" → /bumpOn a [Cell] LabCell: "bump" → /lab-cell/bump Used by both App.BuildEndpointsAsync (runtime, so BuildEndpointUrl emits the right PublicUrl) and AppBundleHandler.DiscoverEndpoints (build-time, so the bundle manifest carries the derived path through to the gateway's routing trie). One source of truth for the derivation keeps the two from drifting.
  static class PathConvention
    // Derive the absolute external path for an HTTP endpoint declared on ownerTypeName . Absolute paths (starting with '/') are returned unchanged. Empty / relative paths are kebab-cased and assembled under the cell-type prefix (or directly on the app class).
    static string DeriveAbsolutePath(string declaredPath, string ownerTypeName, bool isAppClass, string fallbackMethodName)
    // Derive the absolute external path for an owner's MCP JSON-RPC multiplexer: /mcp on the app class, /{kebab-owner}/mcp on a cell. The multiplexer path is fixed — it is never relocated by a tool's declared path. A [Mcp(path)] override adjusts only that single tool's own directly-callable endpoint (see DeriveAbsolutePath ), not the shared multiplexer.
    static string DeriveMcpPath(string ownerTypeName, bool isAppClass)
    // Extract the {name} placeholder names from a path template, in order. "/labs/{workspace}/x" → ["workspace"]. Shared by runtime and build-time discovery so the identity/param split of a path can't drift between them.
    static IReadOnlyList<string> ExtractPathParams(string path)
    // Convert a PascalCase / camelCase identifier to lowercase-kebab. "LabCell" → "lab-cell", "GetOrders" → "get-orders", "ID" → "id". Non-identifier characters (already-hyphens, underscores, slashes) pass through unchanged so a developer who wrote "my-path" gets "my-path" back.
    static string ToKebabCase(string s)
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
  // Captures whether the running ikon tool (or hosted ikon-server) has a platform-dotnet checkout it can build against, and exposes the flags every downstream build step needs: the -p:IkonRoot=... MSBuild arg for dotnet, and the VITE_IS_IKON_INTERNAL / VITE_IKON_PLATFORM_TYPESCRIPT_PATH env vars for vite.
  sealed class PlatformContext : IEquatable<PlatformContext>
    ctor(string? DotnetRoot)
    string? DotnetRoot { get; init; }
    bool IsIkonInternal { get; }
    string? RepoRoot { get; }
    string? SlnxPath { get; }
    string? TypescriptRoot { get; }
    // Returns the first of candidates that is internal, else External .
    static PlatformContext FirstAvailable(params PlatformContext[] candidates)
    // Reads [assembly: AssemblyMetadata("IkonRoot", ...)] baked in at .NET build time via the -p:IkonRoot=... arg — lets hosted code (e.g. ViteServerHandler) recover the platform location after the app DLL has been copied out of the repo tree. Returns External when absent or the path no longer exists.
    static PlatformContext FromAssemblyMetadata(Assembly assembly)
    // Probes the running tool binary's own location ( BaseDirectory ) — matches when the tool lives inside the platform repo (the in-repo artifacts/bin/IkonTool/... case).
    static PlatformContext FromBaseDirectory()
    // Walks upward from directory looking for the platform-dotnet directory (the one containing ikon-platform.slnx). At each ancestor it checks current/ikon-platform.slnx (current is platform-dotnet) and current/platform-dotnet/ikon-platform.slnx (current is the repo root). With includeSibling it also checks current/ikon-platform/platform-dotnet/ (a sibling checkout). Sibling matching is opt-in because it answers "is there a platform-dotnet nearby?" rather than "is directory inside platform-dotnet?" — callers that mutate the platform repo (e.g. add an app to the slnx) must keep it off. Returns External when not found.
    static PlatformContext FromDirectory(string? directory, bool includeSibling = false)
    // Resolves the --platform-repo argument. Accepts the ikon-platform repo root, any of its platform-* subdirectories (e.g. platform-dotnet, platform-typescript), or a nested path within them — all normalize up to the same platform-dotnet root via the upward walk. Returns External when input is null or blank; throws UserException when set but no ikon-platform.slnx can be found at or above it.
    static PlatformContext FromExplicit(string? explicitPlatformRepo)
    // The standard probe ladder: an explicit --platform-repo, then workingDirectory (defaulting to the current directory), then the running tool's own location — so a locally-built ikon tool resolves the repo even for an app created far away. A checkout that is merely a sibling of some ancestor directory is deliberately not probed — an app outside the repo tree builds against published packages unless --platform-repo points at the repo explicitly. noPlatformRepo (--no-platform-repo) hard-disables detection so the app builds against published packages even inside the repo. Returns External when nothing matches.
    static PlatformContext Resolve(string? explicitPlatformRepo = null, bool noPlatformRepo = false, string? workingDirectory = null)
    static PlatformContext External
  // Translates a PlatformContext (a pure detection result) into the tool-specific build inputs that pass the platform location to dotnet and to the SDK frontend's vite config. Kept off PlatformContext itself so the context doesn't carry dotnet/vite implementation detail.
  static class PlatformContextBuildExtensions
    // Stamps VITE_IS_IKON_INTERNAL and, when internal, VITE_IKON_PLATFORM_TYPESCRIPT_PATH onto an env dictionary before invoking npm/vite — the SDK frontend's vite config reads these to alias @ikonai/* to local monorepo source. Mutates and returns env for fluent use.
    static IDictionary<string, string?> ApplyViteEnv(PlatformContext platform, IDictionary<string, string?> env)
    // MSBuild argument to splice into a dotnet build/restore/run/publish command line: -p:IkonRoot="..." when internal, empty string when external.
    static string IkonRootMSBuildArg(PlatformContext platform)
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
  // Client-side agent for the in-house relay server. Establishes a WebSocket to the relay, allocates endpoints on demand, and forwards incoming relay traffic to the matching local port. When the connection drops the agent reconnects with bounded exponential backoff and re-establishes its tunnels; DisposeAsync is the only thing that stops it permanently.
  sealed class RelayAgent : IAsyncDisposable
    // Creates a relay agent with explicit connection parameters. Used when the relay host/port/token are already known (e.g. IkonServer's --public-access path). When stableId is non-empty, the relay assigns a fixed port-range segment to this identity so the public ports stay stable across reconnects.
    ctor(string relayServerAddress, int relayServerPort, string relayAuthToken, string stableId = "")
    // Allocates an endpoint. localPort of 0 picks an available port from an internal pool. The returned RelayEndpoint is disposable; dispose it to release the endpoint. When stablePortName is non-empty (and this agent has a non-empty stableId), the relay assigns a deterministic public port for that name within this agent's segment, so the endpoint's public URL stays the same across reconnects and process restarts. Empty = ephemeral, as before.
    Task<RelayEndpoint> AddEndpointAsync(EndpointProtocol protocol, int localPort = 0, string stablePortName = "", CancellationToken cancellationToken = default)
    // Ensures the connection supervisor is running and waits for a live session. Called implicitly by AddEndpointAsync on first use; calling it explicitly is optional. The very first connection attempt surfaces its failure to the caller; once a session has been established the supervisor reconnects on its own and this call simply waits for the next live session.
    Task ConnectAsync(CancellationToken cancellationToken = default)
    // Creates a relay agent whose host/port/token are fetched from IkonBackend on first connect. Pass a non-empty stableId to opt into stable public-port assignments.
    static RelayAgent CreateFromIkonBackend(string stableId = "")
    ValueTask DisposeAsync()
    // Raised after a reconnect re-establishes an endpoint on a different public address than before. The endpoint reference is unchanged; its PublicHost / PublicPort already reflect the new address.
    event Action<RelayEndpoint>? EndpointRebound
    // Raised when a new session goes live after a previous one was lost.
    event Action? Reconnected
    // Raised when a live session is lost and the agent has begun reconnecting.
    event Action? Reconnecting
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
  class AppBundleConfig.RouterConfig
    ctor()
    string EntryPoint { get; set; }
    List<string> PolicyNames { get; set; }
    bool Present { get; set; }
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
    ctor(string? Branch = null, bool Shallow = false, GitCredentials? Credentials = null)
    string? Branch { get; init; }
    GitCredentials? Credentials { get; init; }
    bool Shallow { get; init; }
  // Git commit information.
  class GitCommit : IEquatable<GitCommit>
    ctor(string Sha, string ShortSha, string Author, string AuthorEmail, DateTimeOffset Date, string Message)
    string Author { get; init; }
    string AuthorEmail { get; init; }
    DateTimeOffset Date { get; init; }
    string Message { get; init; }
    string Sha { get; init; }
    string ShortSha { get; init; }
  // Git credentials for authenticated operations.
  class GitCredentials : IEquatable<GitCredentials>
    ctor(string Username, string Password)
    string Password { get; init; }
    string Username { get; init; }
  // Git diff between two commits.
  class GitDiff : IEquatable<GitDiff>
    ctor(string? FromSha, string? ToSha, List<GitFileDiff> Files)
    List<GitFileDiff> Files { get; init; }
    string? FromSha { get; init; }
    string? ToSha { get; init; }
  // A changed file in git status or diff.
  class GitFileChange : IEquatable<GitFileChange>
    ctor(string Path, GitChangeType Type)
    string Path { get; init; }
    GitChangeType Type { get; init; }
  // File diff information.
  class GitFileDiff : IEquatable<GitFileDiff>
    ctor(string Path, GitChangeType Type, int LinesAdded, int LinesRemoved, string? Patch = null)
    int LinesAdded { get; init; }
    int LinesRemoved { get; init; }
    string? Patch { get; init; }
    string Path { get; init; }
    GitChangeType Type { get; init; }
  // Strongly-typed git repository operations.
  class GitRepository
    ctor(string workingDirectory, GitCredentials? credentials = null)
    GitCredentials? Credentials { get; }
    string WorkingDirectory { get; }
    // Abort all in-progress operations (merge, rebase, cherry-pick).
    Task AbortAllInProgressOperationsAsync(CancellationToken ct = default)
    // Abort an in-progress cherry-pick.
    Task<bool> AbortCherryPickAsync(CancellationToken ct = default)
    // Abort an in-progress merge.
    Task<bool> AbortMergeAsync(CancellationToken ct = default)
    // Abort an in-progress rebase.
    Task<bool> AbortRebaseAsync(CancellationToken ct = default)
    // Add a remote. Credentials are stripped from the URL.
    Task AddRemoteAsync(string name, string url, CancellationToken ct = default)
    // Checkout an existing branch.
    Task CheckoutAsync(string branchOrRef, CancellationToken ct = default)
    // Checkout files from a specific ref without changing HEAD.
    Task CheckoutFilesFromRefAsync(string refName, string path = ".", CancellationToken ct = default)
    // Clone a repository to a target directory.
    static Task<GitRepository> CloneAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    // Clone a repository or sync if it already exists. Returns the repository instance with the current SHA.
    static Task<ValueTuple<GitRepository, string?, bool>> CloneOrSyncAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    // Commit staged changes.
    Task<GitCommit> CommitAsync(string message, CancellationToken ct = default)
    // Commit staged changes with custom author.
    Task<GitCommit> CommitAsync(string message, string authorName, string authorEmail, bool allowEmpty = false, CancellationToken ct = default)
    // Build per-invocation environment variables that authenticate git HTTP(S) operations. Uses git's environment config mechanism (git 2.31+) to inject an Authorization header, appending to any GIT_CONFIG_COUNT entries already present in the process environment.
    static Dictionary<string, string?> CreateAuthEnvironment(GitCredentials credentials)
    // Create and checkout a new branch.
    Task CreateBranchAsync(string name, string? startPoint = null, CancellationToken ct = default)
    // Create a tag.
    Task<GitTag> CreateTagAsync(string name, string? message = null, CancellationToken ct = default)
    // Delete a tag.
    Task DeleteTagAsync(string name, CancellationToken ct = default)
    // Discard all uncommitted changes.
    Task DiscardChangesAsync(CancellationToken ct = default)
    // Rewrite the remote URL to its credential-free form.
    Task EnsureCleanRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    // Escape a commit message for shell.
    static string EscapeMessage(string message)
    // Fetch from remote.
    Task FetchAsync(bool includeTags = false, CancellationToken ct = default)
    // Count how many commits the local branch is ahead of and behind its origin counterpart. Returns null when the counts cannot be determined (e.g. origin/{branch} does not exist).
    Task<ValueTuple<int, int>?> GetAheadBehindAsync(string branch, CancellationToken ct = default)
    // Get all branches.
    Task<List<GitBranch>> GetBranchesAsync(CancellationToken ct = default)
    // Get a local git config value.
    Task<string?> GetConfigAsync(string key, CancellationToken ct = default)
    // Get the current branch name.
    Task<string> GetCurrentBranchAsync(CancellationToken ct = default)
    // Get diff between HEAD and another target (or working directory if null).
    Task<GitDiff> GetDiffAsync(string? target = null, CancellationToken ct = default)
    // Get the HEAD commit.
    Task<GitCommit?> GetHeadCommitAsync(CancellationToken ct = default)
    // Get the HEAD SHA.
    Task<string?> GetHeadShaAsync(bool shortSha = false, CancellationToken ct = default)
    // Get commit history.
    Task<List<GitCommit>> GetHistoryAsync(int limit = 20, string? fromRef = null, CancellationToken ct = default)
    // Get remote URL exactly as stored in .git/config, including any embedded credentials.
    Task<string?> GetRawRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    // Get remote URL (without credentials).
    Task<string?> GetRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    // Get the current repository status.
    Task<GitStatus> GetStatusAsync(CancellationToken ct = default)
    // Get all tags.
    Task<List<GitTag>> GetTagsAsync(CancellationToken ct = default)
    // Check if repository has any commits.
    Task<bool> HasCommitsAsync(CancellationToken ct = default)
    // Check if a remote exists.
    Task<bool> HasRemoteAsync(string name = "origin", CancellationToken ct = default)
    // Check if there are uncommitted changes.
    Task<bool> HasUncommittedChangesAsync(CancellationToken ct = default)
    // Check if there are uncommitted changes under a specific path.
    Task<bool> HasUncommittedChangesAsync(string path, CancellationToken ct = default)
    // Check if the local branch has commits that have not been pushed to origin. A branch that does not exist on origin counts as unpushed when local commits exist.
    Task<bool> HasUnpushedCommitsAsync(string branch, CancellationToken ct = default)
    // Initialize a git repository and connect to a remote, preserving local files. Local files are kept as-is and NOT merged with remote content. Returns the repository instance ready for use.
    static Task<GitRepository> InitAndConnectAsync(string directory, string remoteUrl, GitCredentials? credentials = null, string? configKey = null, string? configValue = null, CancellationToken ct = default)
    // Initialize a new git repository.
    static Task<GitRepository> InitAsync(string directory, CancellationToken ct = default)
    // Check if the working directory is a git repository.
    Task<bool> IsGitRepositoryAsync(CancellationToken ct = default)
    // Check if a directory is a git repository.
    static Task<bool> IsGitRepositoryAsync(string directory, CancellationToken ct = default)
    // List all worktrees attached to this repository (including the primary one). Parses the output of `git worktree list --porcelain`.
    Task<List<GitWorktreeInfo>> ListWorktreesAsync(CancellationToken ct = default)
    // Push to remote.
    Task PushAsync(bool setUpstream = false, CancellationToken ct = default)
    // Check if a ref exists.
    Task<bool> RefExistsAsync(string refName, CancellationToken ct = default)
    // Rename current branch.
    Task RenameBranchAsync(string oldName, string newName, CancellationToken ct = default)
    // Reset HEAD to a specific ref (hard reset).
    Task ResetHardAsync(string target, CancellationToken ct = default)
    // Reset HEAD to a specific ref (soft reset - keeps changes staged).
    Task ResetSoftAsync(string target, CancellationToken ct = default)
    // Restore to a specific target (tag, sha, or branch).
    Task<GitSyncResult> RestoreAsync(string target, CancellationToken ct = default)
    // Run a git command (throws on failure).
    Task<string> RunAsync(string args, CancellationToken ct = default)
    // Save changes (stage, commit, push).
    Task<GitSyncResult> SaveAsync(string message, CancellationToken ct = default)
    // Set a local git config value.
    Task SetConfigAsync(string key, string value, CancellationToken ct = default)
    // Set remote URL. Credentials are stripped from the URL.
    Task SetRemoteUrlAsync(string name, string url, CancellationToken ct = default)
    // Set up tracking for a branch.
    Task SetUpstreamAsync(string remoteBranch, CancellationToken ct = default)
    static string ShortCommitHash(string? hash)
    // Stage all changes.
    Task StageAllAsync(CancellationToken ct = default)
    // Stage a specific path (file or directory).
    Task StagePathAsync(string path, CancellationToken ct = default)
    // Stash all changes.
    Task<bool> StashAsync(string? message = null, CancellationToken ct = default)
    // Pop the latest stash.
    Task<bool> StashPopAsync(CancellationToken ct = default)
    // Strip credentials from a git URL for safe display/comparison.
    static string StripCredentialsFromUrl(string url)
    // Sync to latest remote (fetch + reset --hard).
    Task<GitSyncResult> SyncAsync(CancellationToken ct = default)
    // Try to open an existing git repository.
    static GitRepository? TryOpen(string directory)
    // Run a git command (doesn't throw on failure).
    Task<ValueTuple<bool, string, string>> TryRunAsync(string args, CancellationToken ct = default)
    // Compare two git URLs, ignoring credentials and trailing slashes.
    static bool UrlsMatch(string? url1, string? url2)
  // Git repository status.
  class GitStatus : IEquatable<GitStatus>
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
    ctor(bool Success, string? PreviousSha, string? CurrentSha, string? Error = null)
    string? CurrentSha { get; init; }
    string? Error { get; init; }
    string? PreviousSha { get; init; }
    bool Success { get; init; }
  // Git tag information.
  class GitTag : IEquatable<GitTag>
    ctor(string Name, string Sha, GitCommit? Commit = null)
    GitCommit? Commit { get; init; }
    string Name { get; init; }
    string Sha { get; init; }
  // Git worktree entry reported by `git worktree list`.
  class GitWorktreeInfo : IEquatable<GitWorktreeInfo>
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
