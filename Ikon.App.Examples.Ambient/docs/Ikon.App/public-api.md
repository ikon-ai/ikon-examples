# Ikon.App Public API

namespace Ikon.App
  // Attribute that decorates app classes to configure their connection and messaging behavior
  sealed class AppAttribute : Attribute
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, string[]? dependencies = null)
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
  // A lightweight HTTP and WebSocket endpoint host built on ASP.NET Core. Construct the host, register routes with AppEndpointHost.MapGet / AppEndpointHost.MapPost / AppEndpointHost.MapWebSocket, and call AppEndpointHost.StartAsync to allocate the relay tunnel and begin serving requests.
  sealed class AppEndpointHost : IAsyncDisposable
    // Creates a new HTTP/WebSocket endpoint host. The relay tunnel is not allocated until AppEndpointHost.StartAsync is called.
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    // True once the relay tunnel is allocated and AppEndpointHost.PublicUrl can be read. False before AppEndpointHost.StartAsync, and after it when the relay was unreachable — the host then serves on AppEndpointHost.LocalPort only and retries the allocation in the background; subscribe to AppEndpointHost.PublicUrlAvailable to learn when the tunnel comes up.
    bool HasPublicUrl { get; }
    // The local port Kestrel binds to. Available after AppEndpointHost.StartAsync completes.
    int LocalPort { get; }
    // Invoked once per inbound HTTP/WebSocket request before it is routed. Used to mark external activity (e.g. reset the server's idle timer) so an endpoint-served instance isn't reaped while it is serving traffic. Null = no hook.
    Action? OnRequest { get; set; }
    // The public URL for this endpoint. Available once the relay tunnel is allocated — normally when AppEndpointHost.StartAsync completes; check AppEndpointHost.HasPublicUrl when the relay may be down.
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
    // Allocates the relay tunnel, starts Kestrel with the registered routes, and returns immediately while the host continues to run in the background. When the relay tunnel cannot be allocated (relay not configured, backend unreachable), Kestrel still starts on a locally picked port and the tunnel allocation is retried in the background — local traffic keeps working, and AppEndpointHost.PublicUrlAvailable fires once the tunnel comes up.
    Task StartAsync(CancellationToken cancellationToken = default)
    // Stops the endpoint host gracefully. Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = default)
    // Raised with the public URL when the background retry allocates the relay tunnel after AppEndpointHost.StartAsync completed without one. Not raised when the tunnel was allocated during AppEndpointHost.StartAsync itself — read AppEndpointHost.PublicUrl directly in that case.
    event Action<string>? PublicUrlAvailable
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own schema/*.tp files (compiled by ikon app teleport build); each carries its own Opcode.GROUP_APP_LOCAL opcode and is sent/received as a native type — no JSON marshalling. Delivery is server-controlled and explicit: AppMessaging.SendMessageAsync<T> always takes the recipient client session IDs — there is no implicit broadcast to every client. Whether a type travels reliably or unreliably is declared on the .tp schema (unreliable = true), not here.
  static class AppMessaging
    // Subscribe to inbound app messages of type T (filtered by the type's opcode). The handler receives the decoded native payload and the sender's client session ID. Dispose the returned handle to unsubscribe.
    static IDisposable OnMessage<T>(this IMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    // Send a typed app message to the given client session IDs. The server decides the recipients — pass the explicit target list (e.g. every current client, everyone-but-the-sender, or a single client).
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    // Send a typed app message to a single client.
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
  // Delegate for async event handlers in the app lifecycle.
  delegate AsyncEventHandler<TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<TEventArgs>(TEventArgs e)
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
    // Each call interrupts the previous one — it fades out whatever is still playing and cancels the previous call's generation, which is what a voice app almost always wants (a new reply supersedes the old one). Uses SpeechGeneratorModel.ElevenFlash25 by default — cheap+fast, the platform's go-to tier for conversational TTS. Hand-roll the SpeechGenerator + Audio.SendSpeech loop instead when you need custom mixing (overlapping speakers), speech that must not interrupt what is already playing, raw access to the generated samples (duration math, waveform analysis), or generator config beyond text, voice, instructions, and speed (e.g. language).
    Task SpeakAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // Enable speech-to-text on captured audio. After calling this, every captured audio segment (typically initiated by a CaptureButton or PushToTalkButton) is transcribed when the segment ends, and Audio.SpeechRecognizedAsync fires with the recognized text and originating client context.
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01, bool requireCorrelatedStream = true, string language = "", TimeSpan? timeout = null)
    // Enable open-mic turn detection with speech-to-text — the continuous-listening counterpart of Audio.UseSpeechRecognition: instead of a segment ending on button release, a turn ends when the user stops talking. Each completed turn fires Audio.SpeechRecognizedAsync with the transcript, so an app upgrades from push-to-talk to open mic by swapping this one setup call and keeping its recognition handler unchanged. Optional companion events: Audio.TurnStartedAsync on sustained speech onset and Audio.TurnSpeculativeAsync when a turn has probably ended (its transcript is ready early; the args' token cancels if speech resumes, and a confirmed turn reuses the speculative transcript so no second recognition runs). Turn-end timing is frame-driven: silence windows advance as mic frames arrive, which holds for platform mic capture (it streams continuously while active) — a client mode that stops sending frames during silence would need a wall-clock fallback here.
    void UseTurnDetection(SpeechRecognizerModel model = WhisperLarge3Turbo, string language = "", TurnDetectorConfig? config = null, bool speculative = true, bool pauseWhileAppSpeaking = true, bool requireCorrelatedStream = true, TimeSpan? timeout = null)
    // Event raised when an incoming audio frame is received and decoded
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Event raised when an incoming audio stream begins
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    // Event raised when an incoming audio stream ends
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Event raised when speech-to-text recognition completes for a captured audio segment. Requires Audio.UseSpeechRecognition to be called once during app setup. Each press of a PushToTalkButton (or any other capture-button-initiated stream) produces one recognition event when the user releases. Args carry the recognized text plus the originating client context — no streamId-to-client plumbing needed.
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
    // Event raised when a turn has probably ended and its speculative transcript is ready. Requires Audio.UseTurnDetection to be called once during app setup. Start downstream work (e.g. generating a reply) with the args' cancellation token: it is cancelled if the user resumes speaking; otherwise Audio.SpeechRecognizedAsync confirms the turn with the same TurnSpeculativeEventArgs.TurnId.
    event AsyncEventHandler<TurnSpeculativeEventArgs> TurnSpeculativeAsync
    // Event raised when a user starts a speech turn on a turn-detected stream. Requires Audio.UseTurnDetection to be called once during app setup. Useful as a barge-in or listening-indicator hook.
    event AsyncEventHandler<TurnStartedEventArgs> TurnStartedAsync
  // Event arguments raised when an incoming audio frame is received
  class AudioInputFrameEventArgs : EventArgs
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
  // Options for a client-side microphone capture started with ClientFunctions.StartAudioCaptureAsync. Every property is optional; a null property leaves that setting to the client. Start from ClientAudioCaptureOptions.Default and override what you need.
  sealed class ClientAudioCaptureOptions : IEquatable<ClientAudioCaptureOptions>
    ctor()
    // Whether the client normalizes the microphone level. Null lets the client choose.
    bool? AutoGainControl { get; init; }
    // Target encoder bitrate in bits per second. Null lets the client choose.
    int? Bitrate { get; init; }
    // Sensible speech defaults: 32 kbit/s, auto gain control and noise suppression on, echo cancellation off (nothing is being played back in the common server-transcription case). Device is left to the client, and ClientAudioCaptureOptions.TargetIds is unset, so the server receives the stream.
    static ClientAudioCaptureOptions Default { get; }
    // Id of a specific microphone to use. Null uses the client's default device.
    string? DeviceId { get; init; }
    // Whether the client cancels the audio it is playing back out of the microphone signal. Needed for two-way calls on a loudspeaker; pointless — and lossy — when nothing is being played back, which is why ClientAudioCaptureOptions.Default leaves it off. Null lets the client choose.
    bool? EchoCancellation { get; init; }
    // Whether the client filters steady background noise out of the microphone signal. Null lets the client choose.
    bool? NoiseSuppression { get; init; }
    // Client session ids the encoded audio is routed to. Leave this null if the server-side app is supposed to receive the audio. Setting it addresses the stream to exactly those client sessions — the app's own audio handlers then never fire, silently: the capture starts, audio flows, and nothing on the server (transcription, recording, analysis) sees it. Set it only for client-to-client streaming where the server deliberately stays out of the media path.
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
    // Remarks:
    // The value is whatever the browser's Network Information API exposes and mixes two vocabularies: a speed class ("slow-2g", "2g", "3g", "4g") where only that is available — note a fast wifi connection commonly reports "4g" — or a connection medium ("wifi", "cellular", "ethernet", "bluetooth", "none", ...) on platforms that expose it. Treat it as an informational hint, not a reliable wifi/cellular discriminator.
    static Task<string?> GetNetworkTypeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the browser timezone from the client.
    static Task<string?> GetTimezoneAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current browser URL path and query string from the client.
    static Task<string?> GetUrlAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current page visibility state on the client.
    static Task<ClientVisibility> GetVisibilityAsync(int? targetId = null, CancellationToken cancellationToken = default)
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
    // Updates the UI theme on the client by its wire name. Prefer ClientFunctions.SetThemeAsync for the built-in dark and light themes; this overload exists for custom theme names.
    static Task<bool> SetThemeAsync(string themeName, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the browser URL without triggering a page reload.
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts audio capture on the client from the microphone.
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts video capture on the client from camera or screen.
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a media capture on the client by its stream ID.
    static Task<bool> StopCaptureAsync(string streamId, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a playing sound on the client.
    static Task<bool> StopSoundAsync(string playbackId, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices for the given duration.
    static Task<bool> VibrateAsync(int durationMs, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices with a vibrate/pause pattern. Values alternate between vibration and pause durations in milliseconds, starting with a vibration — so [100, 50, 100] vibrates 100 ms, pauses 50 ms, then vibrates 100 ms again.
    static Task<bool> VibrateAsync(IReadOnlyList<int> pattern, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices from a pattern in its wire form. Prefer the typed overloads taking an int duration or an int pattern; this overload exists for pattern strings that already arrive pre-formatted.
    static Task<bool> VibrateAsync(string pattern, int? targetId = null, CancellationToken cancellationToken = default)
  // Whether the client should prefer a hardware or a software video encoder. This is a preference, not a guarantee — the client falls back to whatever encoder it has.
  enum ClientHardwareAcceleration
    PreferHardware
    PreferSoftware
  // A single still image captured on a client with ClientFunctions.CaptureImageAsync.
  sealed class ClientImageCapture : IEquatable<ClientImageCapture>
    ctor(string Mime, int Width, int Height, byte[] Data)
    // The encoded image bytes (a complete JPEG or PNG file, not raw pixels), ready to write to disk or hand to an asset or a vision model.
    byte[] Data { get; init; }
    // The image's actual height in pixels, which can differ from a requested height the client could not honor.
    int Height { get; init; }
    // The image's mime type, as encoded by the client: image/jpeg or image/png.
    string Mime { get; init; }
    // The image's actual width in pixels, which can differ from a requested width the client could not honor.
    int Width { get; init; }
  // Encoding of a single image captured with ClientFunctions.CaptureImageAsync.
  enum ClientImageCaptureFormat
    Jpeg
    Png
  // Options for a single still image captured with ClientFunctions.CaptureImageAsync. Every property is optional; a null property leaves that setting to the client. Unlike the video and audio capture options there is no TargetIds: the captured image is always returned to the caller on the server.
  sealed class ClientImageCaptureOptions : IEquatable<ClientImageCaptureOptions>
    ctor()
    // Image encoding. Null captures JPEG.
    ClientImageCaptureFormat? Format { get; init; }
    // Target image height in pixels. Null keeps the capture device's own height.
    int? Height { get; init; }
    // Encoder quality from 0.0 (smallest, most artifacts) to 1.0 (largest, near-lossless). Only meaningful for ClientImageCaptureFormat.Jpeg — PNG is lossless and ignores it. Null lets the client choose.
    double? Quality { get; init; }
    // Target image width in pixels. Null keeps the capture device's own width.
    int? Width { get; init; }
  // Event arguments for the IAppBase.ClientJoinedAsync event.
  class ClientJoinedEventArgs : EventArgs
    ctor(Context clientContext)
    // Gets the context of the client that joined.
    Context ClientContext { get; }
    // Gets the session ID of the client that joined.
    int ClientSessionId { get; }
    // Gets the user ID of the client that joined, or an empty string if not authenticated.
    string UserId { get; }
  // Event arguments for the IAppBase.ClientLeftAsync event.
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
    ctor(string DeviceId, ClientMediaDeviceKind Kind, string Label, string GroupId)
    // The unique identifier for the device.
    string DeviceId { get; init; }
    // The group identifier for devices that share the same physical device.
    string GroupId { get; init; }
    // The kind of device (audio input or video input).
    ClientMediaDeviceKind Kind { get; init; }
    // A human-readable label for the device.
    string Label { get; init; }
  // The kind of a media input device available on the client.
  enum ClientMediaDeviceKind
    Unknown
    AudioInput
    VideoInput
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
    // Check if user has a specific built-in role. For roles outside UserRole, check ClientProfile.Roles directly.
    bool HasRole(UserRole role)
    // Require that the user has the specified role. Throws RoleRequiredException if not.
    void RequireRole(UserRole role)
  // Manages client profiles for an AI app. Profiles are loaded and cached when clients join, and ClientProfiles.GetProfileAsync loads any uncached profile from the backend on demand.
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
    Task<TAttributes?> GetAttributesAsync<TAttributes>(Context clientContext) where TAttributes : IProfileAttributes, new()
    // Get a client's profile, loading it from the backend on a cache miss and caching the result. Connected clients are normally already cached (their profile is loaded when they join), so this usually returns instantly and only hits the backend for an uncached user. Returns null when the context carries no UserId or the backend has no profile for it.
    Task<ClientProfile?> GetProfileAsync(Context clientContext)
    // Get a profile by userId, loading it from the backend on a cache miss.
    Task<ClientProfile?> GetProfileAsync(string userId)
    // Refresh a client's profile from the backend
    Task RefreshProfileAsync(Context clientContext)
    // Refresh a profile from the backend by userId
    Task RefreshProfileAsync(string userId)
    // Remove a role from a client
    Task RemoveRoleAsync(Context clientContext, UserRole role)
    // Remove a role from a client using string role name
    Task RemoveRoleAsync(Context clientContext, string role)
    // Set custom attributes for a client
    Task SetAttributesAsync<TAttributes>(Context clientContext, TAttributes attrs) where TAttributes : IProfileAttributes
    // Set roles for a client
    Task SetRolesAsync(Context clientContext, IEnumerable<UserRole> roles)
    // Set roles for a client using string role names
    Task SetRolesAsync(Context clientContext, IEnumerable<string> roles)
    // Update profile fields using a typed ProfileData object
    Task UpdateAsync(Context clientContext, Action<ProfileData> update)
  // A video codec a client may encode a capture with. Listed in ClientVideoCaptureOptions.PreferredCodecs in priority order; the client picks the first one it can actually encode with and falls back to its own default if none are available.
  enum ClientVideoCaptureCodec
    H264
    Vp8
    Vp9
    Av1
  // Options for a client-side video capture started with ClientFunctions.StartVideoCaptureAsync. Every property is optional; a null property leaves that setting to the client. Start from ClientVideoCaptureOptions.DefaultCamera or ClientVideoCaptureOptions.DefaultScreen and override what you need.
  sealed class ClientVideoCaptureOptions : IEquatable<ClientVideoCaptureOptions>
    ctor()
    // Target encoder bitrate in bits per second. Null lets the client choose.
    int? Bitrate { get; init; }
    // Sensible camera defaults: 720p (1280x720) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference. Codec, bitrate, and device are left to the client, and ClientVideoCaptureOptions.TargetIds is unset, so the server receives the stream.
    static ClientVideoCaptureOptions DefaultCamera { get; }
    // Sensible screen-share defaults: 1080p (1920x1080) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference. Codec and bitrate are left to the client, and ClientVideoCaptureOptions.TargetIds is unset, so the server receives the stream.
    static ClientVideoCaptureOptions DefaultScreen { get; }
    // Id of a specific capture device to use (a camera; ignored for screen capture). Null uses the client's default device.
    string? DeviceId { get; init; }
    // Target frames per second. Null lets the client choose.
    int? Framerate { get; init; }
    // Hardware vs software encoder preference. Null lets the client choose.
    ClientHardwareAcceleration? HardwareAcceleration { get; init; }
    // Target frame height in pixels. Null lets the client choose.
    int? Height { get; init; }
    // How many frames apart key frames (full, independently decodable frames) are emitted. A receiver can only start decoding on a key frame, so this is the worst-case join latency for anyone who starts watching mid-stream, and the resync granularity after packet loss. Lower means faster joins and more bandwidth; higher means the opposite. The presets use 90 frames — three seconds at their 30 fps. Null lets the client choose.
    int? KeyFrameIntervalFrames { get; init; }
    // Codecs to try, in priority order. Null lets the client choose.
    IReadOnlyList<ClientVideoCaptureCodec>? PreferredCodecs { get; init; }
    // Client session ids the encoded frames are routed to. Leave this null if the server-side app is supposed to receive the media. Setting it addresses every frame to exactly those client sessions — the app's own video handlers then never fire, silently: the capture starts, frames flow, and nothing on the server sees them. Set it only for client-to-client streaming (e.g. a call where one participant's camera goes straight to the other participants) where the server deliberately stays out of the media path.
    IReadOnlyList<int>? TargetIds { get; init; }
    // Target frame width in pixels. Null lets the client choose.
    int? Width { get; init; }
  // Where a client-side video capture takes its frames from.
  enum ClientVideoCaptureSource
    Camera
    Screen
  // The page visibility state reported by a client.
  enum ClientVisibility
    Unknown
    Visible
    Hidden
  // Marks a method to run on a cron schedule. Unlike HttpMethodAttribute / [Mcp], a cron job is not externally addressable — it has no path and no edge authorization. The platform discovers [Cron] methods at build time, records each in the app bundle manifest, and the backend schedules them; when a tick fires the app is run under the global (empty) session identity and the target function is invoked through the FunctionRegistry.
  // Remarks:
  // A [Cron] method behaves like a [Function] in that the trigger resolves it through the FunctionRegistry by name. Applying [Cron] is enough to register the method (as a Local function) — you do not also need [Function], though combining them is fine. The handler takes no caller-supplied arguments. It may optionally accept a host-injected CronContext (fire time + schedule) and/or a CancellationToken that signals app shutdown, in any order — mirroring how an [HttpPost] handler may accept an HttpRequest. Any other parameter fails registration at startup, since the scheduler has nothing to bind it to. Overlap is allowed: a tick fires even if the previous invocation is still running, so guard re-entrancy yourself if it matters.
  sealed class CronAttribute : Attribute
    // Declares a cron job that runs on schedule.
    ctor(string schedule)
    // Optional registry-name override. When null or empty the function is registered (and triggered) under the full member name of the declaration carrying the attribute, "{DeclaringType.FullName}.{Method}" — the same identity the bundle manifest records, so the backend trigger resolves it even when the method is inherited or overridden.
    string? Name { get; init; }
    // The cron expression that schedules this method (standard 5/6-field cron syntax, e.g. "0 * * * *" for hourly). Evaluated by the backend scheduler.
    string Schedule { get; }
  // Platform email surface for an Ikon app — sending custom emails through the platform mailer and reading inbound emails delivered to the app's space. Accessed via app.Email. All operations require the app's organisation/space to have the Email feature enabled; calls against a non-entitled space throw FeatureNotEnabledException.
  sealed class EmailService
    // Removes an inbound email and frees its attachment storage. Idempotent — deleting a missing message succeeds silently.
    Task DeleteAsync(string id, CancellationToken ct = default)
    // Streams a decrypted attachment from the platform. The returned EmailAttachmentDownload owns the content stream — dispose it (e.g. await using) when done.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = default)
    // Lazily enumerates all received emails matching query, transparently following pages until exhausted. Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single page of received emails for the app's space. Paginate by passing the returned InboxPage.NextCursor back as InboxQuery.Cursor.
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single inbound email with decrypted body and parsed envelope.
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = default)
    // Sends a custom HTML email through the platform mailer. The platform sets the visible From address; pass EmailSendRequest.ReplyTo to direct replies elsewhere. The send is enqueued for asynchronous delivery — a successful return means the platform has accepted the request, not that the recipient has received the message. Transient delivery failures are retried server-side. The total payload size (subject, body, attachments, metadata) is capped at roughly 10 MB.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
  // Shared base for the two developer-facing inbound HTTP surfaces, [Rest] and [Mcp]. They differ only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients; addressing, path templating, identity binding, auth, and abuse-control are identical and live here so there is exactly one place to reason about them.
  abstract class EndpointAttribute : Attribute
    // Built-in authorization for this endpoint, resolved at the gateway edge before (and without) provisioning the app. Defaults to EndpointAuth.Grant (a signed grant URL). Set EndpointAttribute.AuthPolicy instead to name a custom /router/ policy.
    EndpointAuth Auth { get; init; }
    // Name of a custom /router/ edge policy that authorizes this endpoint (an apiKey/hmac/ipAllow helper you defined in router/index.ts). When set (non-empty) it takes precedence over EndpointAttribute.Auth. Authorization lives in /router/, the single auth surface — not in C#.
    string? AuthPolicy { get; init; }
    // External path under the space domain (after {space}.ikonai.app/api). Optional: when omitted (empty) the path is derived from the method name (kebab-cased) — /{method} on the app class, /{cell-type}/{method} on a cell. A leading-slash path is absolute; a relative form ("bump") is resolved against the owner's auto-derived mount point at build time. Route params use {name} syntax. A {name} whose name matches a field of the owner's SessionIdentity record binds into the routing identity (the extrinsic resource the caller names); other {name} segments bind as ordinary handler parameters. Reserved paths the developer must NOT declare: /.well-known/* (RFC), and the /ikon/* + /api subtrees (platform-owned).
    string Path { get; }
  // The built-in authorization for an endpoint — the discoverable, no-/router/-needed options. For a custom edge policy (an apiKey/hmac/ipAllow helper you defined in /router/), set EndpointAttribute.AuthPolicy to its name instead.
  enum EndpointAuth
    Grant
    Public
    Deny
  // Information about an HTTP endpoint exposed by the app — an [HttpGet]/[HttpPost]/[Mcp] surface. Returned by IAppBase.Endpoints for developer convenience.
  sealed class EndpointInfo : IEquatable<EndpointInfo>
    ctor()
    // The cell type for a substrate-cell endpoint (empty for app + AppProcess-cell endpoints). When non-empty, the gateway cell-routes the request to that cell's partitioned instance, keyed by the cell's IdentityFields in the URL; empty means the endpoint resolves to the app instance.
    string CellType { get; init; }
    // The endpoint's registry name — {Owner}_{Method} for typed endpoints (or the explicit FunctionAttribute.Name override). The backend resolves this name when routing.
    string FunctionName { get; init; }
    // The bare public URL for this endpoint under the space domain ({space}.ikonai.app/api/{path}), templated where the path has open {segment}s. It carries NO grant: a public endpoint is callable as-is; a grant/policy endpoint needs a working, identity-bound URL from IApp.MintUrl. The backend reverse-proxies to this instance — cold-starting it in the cloud, or routing to a registered local run.
    string PublicUrl { get; init; }
  // Passed to the onChunkReceived callback — fired for every chunk as it arrives, with the raw bytes, so an app can stream the upload somewhere (transcode, scan, forward) instead of waiting for the whole file. The platform has already written the chunk itself, so this hook does not have to. The bytes are not yet verified: the SHA-256 check only happens once the last chunk is in, and a mismatch discards the whole upload — never act irreversibly on a chunk.
  sealed class FileUploadChunkArgs : IEquatable<FileUploadChunkArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, byte[] Data, long BytesWritten)
    // Total bytes received and written so far, including this chunk.
    long BytesWritten { get; init; }
    // This chunk's bytes. Only valid for the duration of the callback — copy them if you keep them.
    byte[] Data { get; init; }
    // The client-supplied file name.
    string FileName { get; init; }
    // The client-supplied mime type.
    string MimeType { get; init; }
    // The total file size in bytes the client announced.
    long Size { get; init; }
    // Id identifying this upload.
    string UploadId { get; init; }
  // Passed to the onUploadComplete callback — fired once the last chunk is in, the byte count matches, and the recomputed SHA-256 matches the client's declared hash. The file is fully written and closed at this point, and this is the only hook that tells you where it landed.
  sealed class FileUploadCompleteArgs : IEquatable<FileUploadCompleteArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string? LocalTempFilePath, AssetUri? AssetUri)
    // The asset the upload was written into, when an earlier hook set FileUploadResult.AssetUri. Null when the file went to a local temp file instead. Exactly one of the two is non-null. It is the same AssetUri every Asset.Instance.* call takes, so it needs no parsing — null-check it and pass .Value straight on.
    AssetUri? AssetUri { get; init; }
    // The client-supplied file name.
    string FileName { get; init; }
    // Path to the received file in a temp directory, when the upload was not redirected to the asset system. Null when AssetUri is set. The temp directory is deleted when the app stops, so move or copy anything you want to keep.
    string? LocalTempFilePath { get; init; }
    // The client-supplied mime type.
    string MimeType { get; init; }
    // The file size in bytes.
    long Size { get; init; }
    // Id identifying this upload.
    string UploadId { get; init; }
  // Passed to the onUploadError callback — the terminal hook when an upload that had started does not succeed: cancelled, stalled for 60 s, chunks out of sequence, byte count mismatch, SHA-256 mismatch, or a write failure. An upload the app itself rejected from onUploadPreStart or onUploadStart does not reach this hook. Any partially written file or asset has already been deleted by the time this fires, so there is nothing to clean up on disk — only app-side state.
  sealed class FileUploadErrorArgs : IEquatable<FileUploadErrorArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string ErrorMessage)
    // Why the upload failed — the cancellation reason when the app cancelled it, otherwise the platform's description of the failure.
    string ErrorMessage { get; init; }
    // The client-supplied file name.
    string FileName { get; init; }
    // The client-supplied mime type.
    string MimeType { get; init; }
    // The file size in bytes the client announced.
    long Size { get; init; }
    // Id identifying this upload.
    string UploadId { get; init; }
  // Passed to the onUploadPreStart callback — the first hook of an upload, fired when the client announces a file but before it has sent a single byte. This is the cheap place to reject an upload: return false (or a FileUploadResult) and nothing is ever transferred. Hook order for one upload: PreStart → Start → Chunk then Progress (repeating, once per received chunk) → Complete on success, or Error on a failure, cancellation, or 60 s stall. An upload the app rejects from PreStart or Start ends there and fires neither Complete nor Error.
  sealed class FileUploadPreStartArgs : IEquatable<FileUploadPreStartArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, Func<string?, Task> Cancel)
    // Aborts this upload: deletes whatever was written, fires the error hook with the reason, and tells the client to stop. Usable at any point during the upload, not just from this callback — capture it to cancel later (e.g. from a UI cancel button).
    Func<string?, Task> Cancel { get; init; }
    // The client-supplied file name. Untrusted — never join it into a path yourself.
    string FileName { get; init; }
    // The client-supplied mime type. Untrusted — the bytes are not verified against it.
    string MimeType { get; init; }
    // The file size in bytes the client claims it will send. The upload fails with an error if the actual byte count differs.
    long Size { get; init; }
    // Id identifying this upload; the same value appears on every later hook's args.
    string UploadId { get; init; }
  // Passed to the onUploadProgress callback — fired once per received chunk, after the chunk has been written and acknowledged. Meant for driving a progress bar; use onChunkReceived if you need the bytes themselves.
  sealed class FileUploadProgressArgs : IEquatable<FileUploadProgressArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, double ProgressPercentage, long BytesUploaded)
    // Bytes received and written so far.
    long BytesUploaded { get; init; }
    // The client-supplied file name.
    string FileName { get; init; }
    // The client-supplied mime type.
    string MimeType { get; init; }
    // Bytes received so far as a percentage of Size, 0 to 100. Zero for the whole upload when the client announced a size of 0.
    double ProgressPercentage { get; init; }
    // The total file size in bytes the client announced.
    long Size { get; init; }
    // Id identifying this upload.
    string UploadId { get; init; }
  // Accept/reject decision returned from the onUploadPreStart and onUploadStart callbacks. FileUploadResult.Accepted defaults to true; return true; works via the implicit bool conversion. Set FileUploadResult.AssetUri to write the upload straight into the asset system instead of a local temp file.
  sealed class FileUploadResult : IEquatable<FileUploadResult>
    ctor()
    bool Accepted { get; init; }
    AssetUri? AssetUri { get; init; }
  // Passed to the onUploadStart callback — fired after onUploadPreStart accepted the upload and the client has sent the file's hash, but still before any bytes arrive. This is the last point where the upload can be rejected, and the last point where FileUploadResult.AssetUri can redirect the bytes into the asset system instead of a local temp file. It is the only hook that sees FileUploadStartArgs.Hash, so it is where a duplicate check ("do I already have this content?") goes.
  sealed class FileUploadStartArgs : IEquatable<FileUploadStartArgs>
    ctor(string UploadId, string FileName, string MimeType, long Size, string Hash)
    // The client-supplied file name. Untrusted — never join it into a path yourself.
    string FileName { get; init; }
    // The client-declared SHA-256 of the file contents, lowercase hex. The platform recomputes it while receiving and fails the upload with a hash mismatch if the received bytes disagree, so a match here is a genuine content identity — but it is the client's claim, not yet verification, at this point.
    string Hash { get; init; }
    // The client-supplied mime type. Untrusted — the bytes are not verified against it.
    string MimeType { get; init; }
    // The file size in bytes the client claims it will send.
    long Size { get; init; }
    // Id identifying this upload; the same value appears on every other hook's args.
    string UploadId { get; init; }
  // Marks a method as a DELETE REST endpoint. See EndpointAttribute.
  sealed class HttpDeleteAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Marks a method on an app or cell as a GET REST endpoint. The framework mounts a route on the owner's AppEndpointHost, binds the request, invokes the method, and serializes the return value; authorization runs at the gateway edge (the endpoint's Auth/router/ policy), not in-process. See EndpointAttribute for path templating and URL-supplied identity.
  sealed class HttpGetAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Shared base for the verb-named REST attributes ([HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch]). The verb is baked into the attribute type — there is no verb enum — which mirrors the ASP.NET Core idiom and so generates reliably from LLMs. All of them share the addressing + identity model on EndpointAttribute; only the HTTP method differs.
  abstract class HttpMethodAttribute : EndpointAttribute
    // HTTP verb as an uppercase string (GET / POST / PUT / DELETE / PATCH).
    abstract string Method { get; }
  // Marks a method as a PATCH REST endpoint. See EndpointAttribute.
  sealed class HttpPatchAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Marks a method as a POST REST endpoint — the common case (third-party webhooks included; verify the signature from the injected request context). See EndpointAttribute.
  sealed class HttpPostAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Marks a method as a PUT REST endpoint. See EndpointAttribute.
  sealed class HttpPutAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Serializable view of an inbound HTTP request — its method, path, query, headers, and raw body. The dispatcher constructs one per inbound request; a handler reads it (e.g. via HttpCallContext) for the untrusted inputs the typed binding doesn't surface, such as verifying a webhook signature inline.
  sealed class HttpRequest : IEquatable<HttpRequest>
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  // Typed return value from an HttpMethodAttribute-annotated method. Endpoints can return any serializable type for an automatic 200 + JSON response, or return an HttpResult when they need control over status code, content type, or custom body serialization.
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
    virtual Context? CurrentClientContext { get; }
    // The user id of the client currently being served, or an empty string when no client is in scope. Always populated for a connected client — the real user id for authenticated users, a stable anonymous id otherwise. This is the correct source for a payment customer key, subscription gating, per-user state, etc.
    virtual string CurrentUserId { get; }
    // Gets the path to the Data directory for this app. Files placed in the Data folder of the app project can be accessed at runtime using this path. Note: in cloud, this directory is read-only and writing to it will throw an exception.
    string DataDirectory { get; }
    // Gets the database connection configurations for this app instance.
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // Gets the email service for this app — sending custom emails through the platform mailer and reading inbound emails delivered to this app's space. Requires the Email feature to be enabled on the app's organisation/space; calls against a non-entitled space throw FeatureNotEnabledException.
    EmailService Email { get; }
    // Gets the HTTP endpoints ([HttpGet]/[HttpPost]/[Mcp] surfaces) exposed by this app instance, including ready-to-use public URLs with the current session identity and signed token prefilled. The list is built once before Main() runs, from the endpoints declared on the app class and on loaded [Cell] types.
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    // Gets the platform-wide shared state from the server containing clients, streams, and space/channel info.
    GlobalState GlobalState { get; }
    // The loopback endpoint (host + HTTPS port) of THIS instance's own local server, but ONLY when the server's own URL is a localhost address — i.e. local dev WITHOUT --public-access. This lets an in-process client (e.g. a simulated player, a self-test harness) connect directly over loopback to this exact process instead of routing through the relay. It returns null when the instance is exposed via the relay (--public-access) or runs in the cloud — there the server's own URL is the relay/space URL, a direct socket can't (and shouldn't) reach it, and callers should use the normal relay/ApiKey connect path (which routes to this registered serving instance) instead. The default is null for hosts that don't run a local server; IApp<TSessionIdentity, TClientParameters> overrides it.
    virtual (string Host, int Port)? LocalLoopbackEndpoint { get; }
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
    // The app's public URL — the address a browser opens to join this app instance's channel. Replaces the app.ReactiveGlobalState.ChannelUrl.Value incantation; reading it inside UI code subscribes to changes the same way. For a URL with query parameters (e.g. a session join link) use IAppBase.JoinUrl.
    virtual string PublicUrl { get; }
    // Gets the secrets (tokens, API keys, passwords) configured for this app. Values are fetched from the Ikon backend once at app startup and exposed synchronously; changes made via ikon app secret set while the app is running only take effect after a restart.
    Secrets Secrets { get; }
    // Whether this app instance offers the raw UDP / UDP-DTLS transports to connecting clients. Enabled by default. Set to false to disable them. Like IAppBase.WebRtcEnabled this takes effect for clients that connect after it is set (the transports are no longer advertised); already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    // Whether this app instance offers WebRTC transport to connecting clients. Enabled by default. Set to false (e.g. in Main) to disable WebRTC for apps that don't use audio/video or low-latency data — WebRTC peer setup (ICE candidate gathering, DTLS) is a notable per-client memory and allocation cost. Takes effect for clients that connect after it is set: the server stops advertising WebRTC and ignores WebRTC signaling, so no per-client peer state is created. Already-connected clients keep their channels until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Creates a platform-managed eID-backed PAdES signature order for the supplied document(s). The platform navigates the signer's browser to the signing-ceremony URL through the existing client UI surface, awaits the asynchronous packaging completion, and resolves the returned task with the signed PDF and evidence metadata. The returned bytes are the long-term-validation PAdES PDF when the chosen scheme produces it; apps should persist them as the system of record because the platform's session retention is short.
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default)
    // Creates a DbConnection for one of the app's configured databases (the Databases list in the app's env-specific ikon-config toml, applied with ikon app config and surfaced via IAppBase.Databases) by name; the caller opens and disposes it: await using var connection = app.Database("mydb");.
    virtual DbConnection Database(string databaseName)
    // Build a shareable link to this app: IAppBase.PublicUrl plus a query string built from queryParams — an anonymous object (or a string dictionary), following the identity-by-anonymous-object shape of IAppBase.MintUrlAsync. Each readable property becomes a URL-encoded name=value pair; null-valued properties are skipped. So app.JoinUrl(new { id = sessionId }) yields {PublicUrl}?id={sessionId}. Replaces hand-assembling $"{app.ReactiveGlobalState.ChannelUrl.Value}?id={sessionId}". Passing null returns IAppBase.PublicUrl as-is.
    virtual string JoinUrl(object? queryParams = null)
    // Mint a working, identity-bound URL for one endpoint — the single way to get a callable URL for a grant (default) or policy endpoint. You identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), NOT by its URL path — the path is often derived from the method name (and may be templated), so the path is what minting RETURNS, not what you pass in. The returned URL is the endpoint's EndpointInfo.PublicUrl with any pinned {placeholder} path segments substituted and a signed ?ikon-grant= appended. identity (an anonymous object, e.g. new { DocumentId = "doc-42" }, or a string dictionary) PINS those identity fields into the grant; fields you omit stay open {captures} for the caller to fill. Omitting identity entirely (null) pins THIS instance's own session identity, so the URL routes back to this app instance — the common case. Grants are non-expiring by default — pass expiresIn only for an ephemeral link, and an optional group to revoke a batch together via IAppBase.RevokeGroupAsync. Re-minting the same stable (non-expiring) URL returns an identical URL, so it survives restarts.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // Mint working URLs for several endpoints sharing one pinned identity, in a single backend round-trip. Returns a map keyed by the endpoints you passed. See IAppBase.MintUrlAsync.
    virtual Task<IReadOnlyDictionary<string, MintedUrl>> MintUrlsAsync(IEnumerable<string> endpoints, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // Dynamically requests a raw TCP/TLS/UDP endpoint. Returns a RelayEndpoint whose RelayEndpoint.LocalPort a listener should bind to; the endpoint is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the returned endpoint to release it. For HTTP/HTTPS endpoints use AppEndpointHost.
    Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", int localPort = 0, CancellationToken ct = default)
    // Requests a fresh strong-authentication step-up challenge for the current user. Navigates the client browser to the platform's configured identity provider through the existing client UI surface, waits for the user to complete the challenge, and returns the platform-signed step-up assertion JWT. Apps must verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier.
    Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = default)
    // Revoke every URL minted under a shared group tag.
    virtual Task RevokeGroupAsync(string group, CancellationToken ct = default)
    // Revoke a single minted URL by its MintedUrl.GrantId.
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
  // Convenience subscription helpers for the lifecycle events on IAppBase. The raw event handler shape is AsyncEventHandler<TEventArgs> which expects a single EventArgs parameter — LLM-generated code routinely reaches for app.StartingAsync += async () => ... (zero-arg) or async (sender, args) => ... (two-arg, .NET prior). Both fail to compile against the canonical one-arg delegate. These extension methods accept the LLM-natural shapes directly: app.OnStarting(async () => ...) wires the underlying event; app.OnClientJoined(async ctx => ...) passes the Context straight through so the handler doesn't need to remember to drill into the event-args wrapper.
  static class IAppEventExtensions
    // Subscribe to IAppBase.ClientJoinedAsync with a handler that receives the joining client's Context directly (SessionId, UserId, etc) — skipping the ClientJoinedEventArgs wrapper the raw event emits.
    static void OnClientJoined(this IAppBase app, Func<Context, Task> handler)
    // Subscribe to IAppBase.ClientJoinedAsync with a handler that receives both the joining client's Context AND its typed TClientParameters. Replaces the awkward app.Clients[ctx.SessionId]!.Parameters drill inside the handler body.
    static void OnClientJoined<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to IAppBase.ClientLeftAsync with a handler that receives the departing client's Context directly.
    static void OnClientLeft(this IAppBase app, Func<Context, Task> handler)
    // Subscribe to IAppBase.ClientLeftAsync with a handler that receives both the departing client's Context AND its typed TClientParameters.
    static void OnClientLeft<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    // Subscribe to IAppBase.MessageReceivedAsync with a handler that receives the protocol message directly.
    static void OnMessageReceived(this IAppBase app, Func<ProtocolMessage, Task> handler)
    // Subscribe to IAppBase.StartingAsync with a zero-arg async handler. The Starting event carries no data — there's nothing to forward.
    static void OnStarting(this IAppBase app, Func<Task> handler)
    // Subscribe to IAppBase.StoppingAsync with a zero-arg async handler.
    static void OnStopping(this IAppBase app, Func<Task> handler)
  // App host interface providing typed session identity and client parameters.
  interface IApp<TSessionIdentity, TClientParameters> : IAppBase, IMessageChannel
    // Gets the typed parameters for the current client (determined by ReactiveScope). Must be called inside UI.Root() or a ReactiveScope context.
    virtual TClientParameters ClientParameters { get; }
    // Gets the collection of connected clients with typed parameters. Automatically synced with IAppBase.GlobalState.
    IClientCollection<TClientParameters> Clients { get; }
    // Gets the typed session identity used to determine app instance routing.
    TSessionIdentity SessionIdentity { get; }
  // Collection interface for accessing connected clients. Iterable for the common "broadcast / fan-out" pattern (`foreach (var client in app.Clients)`), indexable by session ID for direct lookups, and exposes IClientCollection<TClientParameters>.Ids when only the connected-session-ids are needed.
  interface IClientCollection<TClientParameters> : IEnumerable, IEnumerable<IClient<TClientParameters>>
    // Gets the number of currently connected clients.
    int Count { get; }
    // Gets the connected client session IDs as an enumerable. Convenience for code that just needs the IDs without the full client objects — e.g. `foreach (var id in app.Clients.Ids) { _scores[id] = 0; }`.
    IEnumerable<int> Ids { get; }
    // Gets the client with the specified session ID, or null if not found.
    IClient<TClientParameters>? this[int clientSessionId] { get; }
  // Interface representing a connected client with typed parameters.
  interface IClient<TClientParameters>
    // Gets the typed parameters for this client.
    TClientParameters Parameters { get; }
    // Gets the session id of this client — the same id used to index IClientCollection<TClientParameters> and to target client-directed APIs.
    int SessionId { get; }
  // Marker interface for custom profile attribute classes. Implement this interface on classes that define custom profile attributes.
  interface IProfileAttributes
  // Marks a method on an app or cell as an MCP tool. The framework discovers these at startup, reflects the method's parameters into a JSON Schema, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP tools/call requests to it.
  // Remarks:
  // Sibling of HttpMethodAttribute: both declare an inbound HTTP endpoint over the shared addressing + identity model (see EndpointAttribute), differing only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients. Each tool is reachable two ways: through the owner's fixed JSON-RPC multiplexer ({owner}/mcp — tools/list + tools/call, and the only surface that streams notifications/progress over SSE), and as its own directly-callable POST endpoint whose body IS the tool's arguments object. That per-tool path defaults to the kebab-cased method name and is overridable via EndpointAttribute.Path — the override adjusts only this tool's own endpoint, never the shared multiplexer. The same method may also carry a verb-named REST attribute ([HttpPost] etc.); then that route serves the REST surface and the per-tool MCP endpoint is suppressed. The governance subject id is always the structural "{Type}.{Method}".
  sealed class McpAttribute : EndpointAttribute
    // Declares an MCP tool whose own endpoint path is the kebab-cased method name.
    ctor()
    // Declares an MCP tool whose own directly-callable endpoint is served at path.
    ctor(string path)
    // Description shown to MCP clients so the agent's LLM can decide when to invoke the tool. Empty values pass through verbatim — there is no XML-summary fallback.
    string Description { get; init; }
    // MCP-wire tool name presented to clients in tools/list. Defaults to the method name when null or empty. The governance subject id is always "{Type}.{Method}" regardless of this.
    string? Name { get; init; }
  // Marks a method on a cell as an MCP-exposed resource — read-only data addressed by a URI. The framework reflects the method's parameters into a URI template, registers the method on an Ikon.Mcp.McpHost, and routes incoming MCP resources/read requests against the matching URI.
  // Remarks:
  // Sibling of McpAttribute — same cell-method-as-callable model, different MCP verb shape: • Static resource — method takes no arguments; the URI is the literal McpResourceAttribute.UriTemplate with no placeholders. Lists in resources/list. • Dynamic resource — method takes parameters that map to {placeholder} segments in the URI template by name. Lists in resources/templates/list; the client crafts a concrete URI and reads it. Read-only by spec — authors should not put side effects in resource methods (the same governance hook still fires on every read with Operation = "resource", so policy authors can distinguish read access from tool dispatch).
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
  // Event arguments for the IAppBase.MessageReceivedAsync event.
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    // Gets the received protocol message.
    ProtocolMessage Message { get; }
  // A minted endpoint URL: the working MintedUrl.Url (the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended), the MintedUrl.GrantId to revoke it by, and the optional MintedUrl.ExpiresAt when a TTL was requested (grants are non-expiring by default).
  sealed class MintedUrl : IEquatable<MintedUrl>
    ctor(string Url, string GrantId, DateTimeOffset? ExpiresAt)
    DateTimeOffset? ExpiresAt { get; init; }
    string GrantId { get; init; }
    string Url { get; init; }
  // The app's browser-history surface, reached through App.Navigation: reads and drives the URL of a connected client, and reports the navigations the client makes on its own. Navigation is per client, not per app: every path the app sets or reads belongs to one client session. The parameterless overloads act on the client of the ambient ClientScope — the client whose event, function call or reactive render is currently on the stack — so they must be called from a client-scoped context; the targetId overloads name the client session explicitly and work from anywhere (a background task, a timer, another client's handler). Paths under the platform-reserved prefixes /ikon and /api are rejected: the load balancer intercepts them before they ever reach the app, so navigating there would strand the client on a backend route. Navigation.SetPathAsync throws ArgumentException rather than let that happen.
  class Navigation
    // Asks one client where it currently is. The path is read from the live client, so it round trips over the connection rather than reading server-side state.
    Task<string?> GetPathAsync(int targetId)
    // Asks the client of the ambient ClientScope where it currently is. Call this from a client-scoped context (an event handler, a function call, a reactive render).
    Task<string?> GetPathAsync()
    // Navigates one client to path. The client's existing query parameters are carried over unless path brings a query string of its own.
    Task<bool> SetPathAsync(int targetId, string path, bool replace = false)
    // Navigates the client of the ambient ClientScope to path, carrying over the client's existing query parameters unless path brings a query string of its own. Call this from a client-scoped context (an event handler, a function call, a reactive render); outside one there is no client to navigate.
    Task<bool> SetPathAsync(string path, bool replace = false)
    // Raised after a client's URL changes, whichever side caused it: the client following a link, pressing back, reloading, or the app calling Navigation.SetPathAsync. Handlers run on a background task inside the navigating client's UserScope and ClientScope, so scoped reactives resolve to that client. An exception thrown by a handler is logged and swallowed — it never propagates back to the client.
    event AsyncEventHandler<NavigationPathChangedEventArgs> PathChangedAsync
  // Event arguments raised when a client navigates to a different URL — either through the app (Navigation.SetPathAsync) or on its own (a link, the browser's back button, a manual reload).
  class NavigationPathChangedEventArgs : EventArgs
    // Creates the event arguments, splitting url into path and query
    ctor(string url, Context clientContext)
    // The client that navigated
    Context ClientContext { get; }
    // Session id of the client that navigated
    int ClientSessionId { get; }
    // The new path without its query string (e.g. /orders for /orders?id=7)
    string Path { get; }
    // The new URL as the client reported it, query string included
    string Url { get; }
    // Id of the user the navigating client is signed in as
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
  // Platform notification surface for an Ikon app — shows user-facing notifications on connected clients. Accessed via app.Notifications. Connected clients receive the notification immediately (foreground). Permission is requested lazily on the client the first time a notification is actually sent, not when the app opens. NotificationService.SendToUserAsync fans out to every connected session for that user; if the user has no connected session it falls back to offline push (an OS notification) through the backend push hub. Offline push is server-orchestrated: when a foreground send is granted, the client's push subscription is fetched and registered with the backend, which then delivers via Web Push / FCM while the user is disconnected.
  sealed class NotificationService
    // Shows a notification on all currently-connected client sessions. Returns one result per session.
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    // Reads a client's current notification permission state without sending anything.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    // Shows a notification on a single connected client session. The client requests notification permission lazily (on this first send) before displaying. Returns the per-session delivery and permission outcome.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // Shows a notification on every currently-connected session belonging to userId (a user may be connected from several devices). When the user has no connected session, falls back to offline push — an OS notification delivered through the backend push hub. Returns one result per targeted session (empty when the user was offline and only push was attempted).
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = default)
  // A ReactiveDictionary<TKey, TValue> persisted globally for the app within its space. Shared across all session identities and users; one dictionary per app deployment.
  // Remarks:
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentReactive<T>. For per-user dictionaries use PersistentUserReactiveDictionary<TKey, TValue>.
  class PersistentReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A ReactiveList<T> persisted globally for the app within its space. Shared across all session identities and users; one list per app deployment.
  // Remarks:
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentReactive<T>. For per-user lists use PersistentUserReactiveList<T>.
  class PersistentReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A reactive value persisted globally for the app within its space. Shared across all session identities and users; one value per app deployment.
  // Remarks:
  // Use for app-wide configuration the app instance owns. For per-session-identity state (the typical app routing key) use PersistentSessionReactive<T>; for per-user state use PersistentUserReactive<T>.
  class PersistentReactive<T> : Reactive<T>
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A ReactiveDictionary<TKey, TValue> persisted per session identity. Apps with the same routing key share the same dictionary; different routing keys have isolated dictionaries.
  // Remarks:
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentSessionReactive<T>, which is the natural choice for dictionary state belonging to a specific app instance.
  class PersistentSessionReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A ReactiveList<T> persisted per session identity. Apps with the same routing key share the same list; different routing keys have isolated lists.
  // Remarks:
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentSessionReactive<T>, which is the natural choice for list state belonging to a specific app instance.
  class PersistentSessionReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A reactive value persisted per session identity. Apps with the same routing key share the same value; different routing keys have isolated values.
  // Remarks:
  // This is the natural choice for state that belongs to a specific app instance, since the session identity already determines instance routing.
  class PersistentSessionReactive<T> : Reactive<T>
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
  // A ReactiveDictionary<TKey, TValue> persisted per user, partitioned at runtime by UserScope. Each user sees their own dictionary across all of their client sessions.
  // Remarks:
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
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
  // A ReactiveList<T> persisted per user, partitioned at runtime by UserScope. Each user sees their own list across all of their client sessions.
  // Remarks:
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
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
  // A reactive value persisted per user, partitioned at runtime by UserScope. Each user sees their own value across all of their client sessions.
  class PersistentUserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Private, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // Writes one user's value regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then write to it from anywhere.
    void SetFor(string userId, T value)
    // Atomically read-modify-writes one user's value, under that user's lock, regardless of which scope — if any — is active.
    void UpdateFor(string userId, Func<T, T> mutator)
    // Reads one user's value regardless of which scope — if any — is active.
    T ValueFor(string userId)
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
    ctor(string text, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount, int turnId = 0)
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
    // Identifier of the detected turn when the recognition came from Audio.UseTurnDetection, shared with the matching TurnStartedEventArgs and TurnSpeculativeEventArgs. 0 for push-to-talk recognitions (Audio.UseSpeechRecognition).
    int TurnId { get; }
    // User id of the speaker.
    string UserId { get; }
  // Event arguments for the IAppBase.StartingAsync event.
  class StartingEventArgs : EventArgs
    ctor()
  // Event arguments for the IAppBase.StoppingAsync event.
  class StoppingEventArgs : EventArgs
    ctor()
  // The built-in client UI themes. The wire protocol carries the theme as a string (custom theme names are allowed via ClientFunctions.SetThemeAsync); ThemeExtensions.ToThemeName maps these values to their wire names.
  enum Theme
    Dark
    Light
  // Helpers for mapping Theme values to and from the wire strings used by the client.
  static class ThemeExtensions
    // True when the client's reported theme is the dark theme. False for the light theme, custom theme names, and clients that have not reported a theme.
    static bool IsDarkTheme(this Context clientContext)
    // Returns the wire name of the theme: "dark" or "light".
    static string ToThemeName(this Theme theme)
  // Event arguments raised when a turn has probably ended and its speculative transcript is ready (see Audio.UseTurnDetection). Start downstream work (e.g. generating a reply) with TurnSpeculativeEventArgs.CancellationToken: it is cancelled if the user resumes speaking, and the matching SpeechRecognizedEventArgs (same TurnSpeculativeEventArgs.TurnId) confirms the turn otherwise.
  sealed class TurnSpeculativeEventArgs : EventArgs
    ctor(int turnId, string text, TimeSpan duration, CancellationToken cancellationToken, string streamId, Context clientContext)
    // Cancelled if the user resumes speaking, invalidating this speculative transcript.
    CancellationToken CancellationToken { get; }
    // Client context of the speaker.
    Context ClientContext { get; }
    // Client session id of the speaker.
    int ClientSessionId { get; }
    // Duration of the audio the transcript was recognized from.
    TimeSpan Duration { get; }
    // Stream id the turn was detected on.
    string StreamId { get; }
    // Speculative transcript of the turn so far.
    string Text { get; }
    // Identifier of this turn, shared with the matching started and recognized events.
    int TurnId { get; }
    // User id of the speaker.
    string UserId { get; }
  // Event arguments raised when a user starts a speech turn on a turn-detected stream (see Audio.UseTurnDetection). Useful as a barge-in or listening-indicator hook.
  sealed class TurnStartedEventArgs : EventArgs
    ctor(int turnId, string streamId, Context clientContext)
    // Client context of the speaker.
    Context ClientContext { get; }
    // Client session id of the speaker.
    int ClientSessionId { get; }
    // Stream id the turn was detected on.
    string StreamId { get; }
    // Identifier of this turn, shared with the matching speculative and recognized events.
    int TurnId { get; }
    // User id of the speaker.
    string UserId { get; }
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
    ValueTask CloseAsync(string? streamId = null)
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
  class VideoInputFrameEventArgs : EventArgs
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
  class VideoInputStreamBeginEventArgs : EventArgs
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
  class VideoInputStreamEndEventArgs : EventArgs
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

namespace Ikon.App.Cells
  // Marks a class as a cell — a headless app addressed by a SessionIdentity record declared inside the class. Discovered by CellHost at startup via reflection over loaded assemblies.
  sealed class CellAttribute : Attribute
    ctor()
    // Number of concurrent instances per addressable key. Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin CellHost.Resolve<TInterface> across them. For globals (parameterless SessionIdentity) the N instances are eager-spawned at host construction — the load-balanced auth-cell pattern. For keyed cells the N instances are spawned together on first access; sharded keyed cells must tolerate eventual consistency between shards (cells should hold no per-instance state, or persist shared state through an external store).
    int Capacity { get; init; }
    // How long a keyed cell may remain idle before EvictIdle removes it from the directory. Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
    // Where this cell type is hosted. CellProcessScope.AppProcess (the default) keeps the cell in the app's own `CellHost` — every app process has its own copies, state is not shared across processes. CellProcessScope.Substrate declares that the cell should be hosted on the platform's cell-deployment substrate, where one instance per (cell-type, SessionIdentity) is shared across all app processes that connect.
    // Remarks:
    // The substrate is the architectural commitment that makes cell SessionIdentity authoritative for cell deployment (independent of how many app processes exist). See the "cell-substrate + unified HTTP surface" RFC for the full design. Today's shipped behaviour:Cells.Connect<TInterface>(identity) for a CellProcessScope.Substrate cell returns a SubstrateCellProxy that dispatches method calls over HTTP to the cell's [HttpGet]/[HttpPost] endpoint URL. Cross-process Reactive<T> subscriptions are NOT yet plumbed — until the reactive wire protocol lands, reactive state continues to behave per-process. Concrete-class access (Cells.Connect<ConcreteCellType>) returns the local instance unchanged, regardless of CellAttribute.ProcessScope.
    CellProcessScope ProcessScope { get; init; }
  // Where a CellAttribute-decorated type's instances live.
  enum CellProcessScope
    AppProcess
    Substrate
  // Per-server-scoped accessor (via AsyncLocalInstance<T> — use Cells.Instance) for that server's CellHost plus the wiring substrate-cell proxies need: the endpoint-URL resolver (for [HttpGet]/[HttpPost] methods) and the cell-client factory (for [Function] methods and Reactive<T> state, which ride a standard IkonClient SDK connection to the cell-host).
  // Remarks:
  // Each in-process server runs in its own async-local scope (enabled by the server's InitializeAll()), so Cells.Instance resolves to that server's own host/wiring — multiple servers (Studio + preview + sandbox) no longer share or clobber one process-global host. The framework calls Cells.Initialize once at startup; apps call Cells.Connect<TInterface> for each cell access. Tests construct their own CellHost over a controlled assembly set and re-initialize between scenarios.
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // Resolve (or spawn on first call) the cell implementation for TInterface keyed by sessionIdentity. Subsequent calls with an equal SessionIdentity return the same instance.
    // Remarks:
    // For cell types annotated [Cell(ProcessScope = CellProcessScope.Substrate)] AND when TInterface is an interface, returns a SubstrateCellProxy<TInterface> that dispatches per member: [HttpGet]/[HttpPost] methods over stateless HTTP, [Function] methods and Reactive<T> members over a standard SDK connection to the cell-host. Otherwise returns the local cell instance from the process-wide CellHost.
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    // Dispose every live cell-host connection. Call on app shutdown. Idempotent.
    ValueTask DisposeAsync()
    // Reserved key in an SDK connection's parameters that names the substrate cell type to route to. The cell's SessionIdentity-record fields ride alongside it. MUST stay in sync with the cloud's CELL_TYPE_PARAM in cell-routing.ts — that's what ChannelInstanceService.create keys on to provision a cell-host channel-instance.
    const string CellTypeParam
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
    // Builds a ready-to-use Google API credential from stored refresh-token credentials — the access token is obtained and refreshed automatically on first use.
    // Remarks:
    // The returned UserCredential is a third-party type from the Google.Apis.Auth NuGet package (namespace Google.Apis.Auth.OAuth2), which ships transitively with this library. Assign it as the HttpClientInitializer in any Google API service initializer (Drive, Sheets, Gmail, Calendar, ...) from the corresponding Google.Apis.* package.
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
  // Per-invocation context for a CronAttribute handler currently executing. A cron handler may optionally accept one of these (and/or a CancellationToken) to learn when and why it fired; a parameterless handler is equally valid. AsyncLocal so handler code (and anything it calls) can read it without threading it through every method signature.
  sealed class CronContext : IEquatable<CronContext>
    ctor(DateTime FireTimeUtc, string Schedule)
    // The cron context for the invocation currently running on this async flow, or null.
    static CronContext? Current { get; }
    DateTime FireTimeUtc { get; init; }
    string Schedule { get; init; }
    static IDisposable Use(CronContext context)

namespace Ikon.App.Http
  // Per-request context for an HttpMethodAttribute handler currently executing. AsyncLocal so handler code (and anything it calls) can read the request's resolved identity without threading the dict through every method signature. Relationship to other "context" concepts on the platform: • SessionIdentity (the typed app/cell record): the routing / instance-partition key. Always present — it's what was used to address the channel-instance this handler runs in. Stable across the cell instance's lifetime. • Context (Ikon protocol Context for WS clients): the live client *connection* — sessionId, deviceId, AuthSessionId, UserId from the connect-token. Absent for endpoint/MCP dispatches because there is no live client connection. • HttpCallContext.Current (this) and McpCallContext.Current: the *request-scoped overlay* that exposes the per-call resolved identity for handler code to read. Set by the wrapper before the handler runs, cleared after. The point is that handlers reading "who is this call for?" get a non-empty answer on endpoint/MCP-dispatched calls, where the connection-level Context.UserId would be empty. The handler's SessionIdentity record (resolved by CellHost.ResolveByCellTypeName before this context is set) and HttpCallContext.Current.SessionIdentity carry the same information in different shapes: the former is typed and tied to the cell's lifetime; the latter is the raw wire dict tied to the call's lifetime. Headers and RawBody are the UNTRUSTED request inputs, exposed so a handler can do its own logic inline (e.g. verify a Stripe-Signature against the raw body) without a separate auth cell. They must never feed identity resolution — the target instance is already chosen from trusted sources (a signed ikon-grant / policy claims / platform-controlled path+query) before the handler runs, so reading a header cannot retarget the call.
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
  // Per-request context for an MCP tools/call or resources/read in flight. AsyncLocal so the bridge can read it from inside parameter binding without threading another argument through every call site. Carries: • The request's effective McpCallContext.CancellationToken (linked to the transport CT and a per-request CTS the host can trip on notifications/cancelled). • An optional progress sink the bridge wires IProgress<T> parameters into. • McpCallContext.SessionIdentityFields — the authenticated identity for this request (from claims merged by the transport). Bridges pass it to CellHost.ResolveByCellTypeName so keyed cells route to the right instance. Empty / null on the stdio path (single-user process).
  sealed class McpCallContext : IEquatable<McpCallContext>
    ctor(CancellationToken CancellationToken, Func<ProgressUpdate, Task>? OnProgress, IReadOnlyDictionary<string, string>? SessionIdentityFields = null)
    CancellationToken CancellationToken { get; init; }
    static McpCallContext? Current { get; }
    Func<ProgressUpdate, Task>? OnProgress { get; init; }
    IReadOnlyDictionary<string, string>? SessionIdentityFields { get; init; }
    // Convenience accessor for the conventional userid field of the request's SessionIdentity. Returns null when no McpCallContext is current or when claims carried no userid. Mirror of HttpCallContext.UserId — same semantics across both request-scoped contexts.
    string? UserId { get; }
    static IDisposable Use(McpCallContext context)
  // One progress update emitted by a long-running tool. ProgressUpdate.Progress is a monotonic counter; ProgressUpdate.Total is optional but expected to stay constant across updates so clients can render a percentage. ProgressUpdate.Message is freeform display text.
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
  // The price for a created offer. Omit OfferPriceSpec.Interval for a one-time offer.
  sealed class OfferPriceSpec : IEquatable<OfferPriceSpec>
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval? Interval = null, int? IntervalCount = null)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval? Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  // Defines an offer to create via PaymentsService.CreateOfferAsync.
  sealed class OfferSpec : IEquatable<OfferSpec>
    ctor(string OfferId, string Name, OfferPriceSpec Price)
    string Name { get; init; }
    string OfferId { get; init; }
    OfferPriceSpec Price { get; init; }
  // A single payment record (a one-off charge or a subscription renewal). Payment.OfferId is null for ad-hoc charges and records written before offer tracking.
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
  // A customer's access to an offer, whether from an active subscription or a one-time purchase. This is the access-control answer the [PaymentsRequireEntitlement] policy gates on. Subscription access carries PaymentEntitlement.ExpiresAt (period end plus a grace window) and reports inactive once it has passed; a one-time purchase has no expiry.
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
  // The kind of a normalized PaymentEvent.
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
  // A provider-hosted page the customer is redirected to in order to pay. Send them to PaymentLink.Url.
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
  // One price on an offer. PaymentPrice.Interval and PaymentPrice.IntervalCount are meaningful only when PaymentPrice.Kind is PriceKind.Recurring; a one-time price reports PriceInterval.Unknown.
  sealed class PaymentPrice : IEquatable<PaymentPrice>
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval Interval, int? IntervalCount)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  // The payment provider that moves the money. A command uses the space's enabled provider unless it names one, either per call or by pinning PaymentsService.DefaultProvider.
  enum PaymentProvider
    Stripe
    Mollie
    Surfboard
  // A receipt for a completed payment. PaymentReceipt.Url is a provider-hosted receipt page. PaymentReceipt.Pdf holds downloadable PDF bytes only when the provider exposes one; today every provider (Stripe, Surfboard) returns a hosted URL only, so PaymentReceipt.Pdf is null — the field is populated when a provider offers a PDF.
  sealed class PaymentReceipt : IEquatable<PaymentReceipt>
    ctor(string? Url, byte[]? Pdf, string? PdfContentType)
    byte[]? Pdf { get; init; }
    string? PdfContentType { get; init; }
    string? Url { get; init; }
  // Result of a PaymentsService.ReconcileAsync request. PaymentReconcileResult.Enqueued counts the provider objects queued for re-processing; their effects arrive asynchronously as normal payment events.
  sealed class PaymentReconcileResult : IEquatable<PaymentReconcileResult>
    ctor(PaymentProvider? Provider, int Enqueued)
    int Enqueued { get; init; }
    PaymentProvider? Provider { get; init; }
  // Result of a refund.
  sealed class PaymentRefund : IEquatable<PaymentRefund>
    ctor(string Reference, RefundStatus Status)
    string Reference { get; init; }
    RefundStatus Status { get; init; }
  // The outcome of a Payment.
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
  // Declares the function requires the current customer to hold an active entitlement for offerId — access granted by an active subscription or a one-time purchase. Resolves the customer from PolicyCallContext.UserId and reads the entitlement from Instance. On missing access it DENIES with a stable code (payments_entitlement_required); the app's UI catches it and opens a payment link via PaymentsService.CreatePaymentLinkAsync. The provider webhook then flips the entitlement and the user retries.
  sealed class PaymentsRequireEntitlementAttribute : PolicyAttribute
    ctor(string offerId)
    // Offer the entitlement is keyed to.
    string OfferId { get; }
    override IFunctionPolicy CreatePolicy()
  // App-level entry point for payments, reached via app.Payments. The app creates payment links (for an offer or an ad-hoc amount) and reacts to PaymentsService.PaymentEventReceived events. Every command accepts an optional per-call provider override; when none is given the backend uses the space's enabled provider. The app holds no payment state. One instance per app (an AsyncLocalInstance<T> singleton).
  sealed class PaymentsService : AsyncLocalInstance<PaymentsService>
    ctor()
    // Default cancel URL used when a command does not specify one.
    string? DefaultCancelUrl { get; set; }
    // Optional provider to use when a command does not specify one. Left null by default: the SDK then sends no provider and the backend charges with the space's enabled (default) provider. Set this only to pin a specific provider for an app that has more than one enabled.
    PaymentProvider? DefaultProvider { get; set; }
    // Default success URL used when a command does not specify one.
    string? DefaultSuccessUrl { get; set; }
    // Cancel a subscription at the period end (default) or right away with immediate. The entitlement lapses when the cancellation takes effect.
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Create (or update) an offer in the app's catalog so customers can pay for it by id. For Stripe this provisions a Product + Price; for providers without a catalog (Mollie, Surfboard) the offer is stored by the platform. Idempotent on OfferSpec.OfferId.
    Task<PaymentOffer> CreateOfferAsync(OfferSpec offer, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Create a provider-hosted payment link for an offer. Recurring offers start a subscription; paying grants an entitlement. customerKey defaults to the current user. allowPromotionCodes lets the customer enter a promotion code on the checkout page (Stripe only — codes are managed in the merchant's Stripe dashboard; other providers ignore it).
    Task<PaymentLink> CreatePaymentLinkAsync(string offerId, string? customerKey = null, string? email = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, bool allowPromotionCodes = false, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Create a provider-hosted payment link for an ad-hoc amount (tip, one-off charge). Grants no entitlement — use an offer for that. customerKey defaults to the current user. allowPromotionCodes lets the customer enter a promotion code on the checkout page (Stripe only — codes are managed in the merchant's Stripe dashboard; other providers ignore it).
    Task<PaymentLink> CreatePaymentLinkAsync(long amountMinor, string currency, string? customerKey = null, string? description = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, bool allowPromotionCodes = false, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // The customer's access to an offer (a backend call). Used by the [PaymentsRequireEntitlement] policy. customerKey defaults to the current user. For gating UI, prefer the synchronous PaymentsService.IsEntitled.
    Task<PaymentEntitlement> GetEntitlementAsync(string offerId, string? customerKey = null, CancellationToken cancellationToken = default)
    // Synchronous, cache-backed access check for gating UI — no backend call, safe to read every render. Reading it inside a UI lambda re-renders when the entitlement changes (after a purchase or a pushed event). customerKey defaults to the current user. The first read for an unseen offer returns false and warms the cache in the background, flipping to the real value on the next render.
    bool IsEntitled(string offerId, string? customerKey = null)
    // The app's catalog of purchasable offers.
    Task<IReadOnlyList<PaymentOffer>> ListOffersAsync(CancellationToken cancellationToken = default)
    // The customer's payments. customerKey defaults to the current user.
    Task<IReadOnlyList<Payment>> ListPaymentsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // The customer's subscriptions. customerKey defaults to the current user.
    Task<IReadOnlyList<PaymentSubscription>> ListSubscriptionsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // Ask the backend to re-pull live provider state — the recovery path when a provider webhook was missed or the app was offline when an event was pushed. Eventually consistent: the pulled objects flow through the normal pipeline and surface as ordinary PaymentsService.PaymentEventReceived pushes and entitlement refreshes within seconds. With a reference (a payment link's checkout-session reference or a subscription id) only that object is pulled; otherwise the customer's recent objects; with neither and no current user in scope, the space's recent window.
    Task<PaymentReconcileResult> ReconcileAsync(string? customerKey = null, string? reference = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Refund a payment, in full by default or partially via amountMinor. Refunding does not revoke an entitlement the payment granted.
    Task<PaymentRefund> RefundAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Remove an offer from the app's catalog (Stripe archives the Product/Price). Returns false if no such active offer existed.
    Task<bool> RemoveOfferAsync(string offerId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Fetch a receipt for a completed payment. PaymentReceipt.Url is a provider-hosted receipt page (present for Stripe and Surfboard). PaymentReceipt.Pdf carries downloadable PDF bytes only when the provider offers one; today both providers return a hosted URL only, so it is null.
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
  // The state of a PaymentRefund.
  enum RefundStatus
    Unknown
    Pending
    Succeeded
    Failed
  // The lifecycle state of a PaymentSubscription.
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
