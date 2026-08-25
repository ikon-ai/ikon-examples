# Ikon.App Public API

namespace Ikon.App
  // Attribute that decorates app classes to configure their connection and messaging behavior. The decorated class must declare the app entry point as a public parameterless method named Main — either a synchronous void method or an async Task method, but NOT async void (an async void Main is fire-and-forget: it is never awaited, so its exceptions escape startup error handling and the app can report ready while Main faulted). It is discovered by reflection and invoked once at startup after dependencies are ready; a missing or misnamed Main throws at startup. Declare the UI and endpoints in Main and return — do not block or await indefinitely.
  sealed class AppAttribute : Attribute
    // name: Display name of the app. Defaults to the class name if not specified
    // productId: Unique identifier for the app. Defaults to the full type name if not specified
    // description: Human-readable description of the app. Defaults to "{ClassName} App" if not specified
    // version: Version number of the app
    // guid: Stable identifier for the app that persists across class renames. Used by external systems to identify apps independently of their type name
    // userType: Indicates whether the app operates autonomously (Machine) or represents a human user connecting through it (Human). Defaults to Machine
    // receiveOpcodeGroups: Opcode groups this app subscribes to receive messages from. Almost all apps leave this at the default; change it only for specialized protocol-level message filtering
    // sendOpcodeGroups: Opcode groups this app is allowed to send messages to. Almost all apps leave this at the default; change it only for specialized protocol-level message filtering
    // dependencies: Product IDs of other apps that must reach ready state before this app's Main() runs (and before its StartingAsync event fires); they are awaited during connect
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, string[]? dependencies = null)
    // Each listed app is awaited during connect — before this app's Main() runs and before its StartingAsync event fires — so ordering logic belongs in Main()/ StartingAsync, not in ClientJoinedAsync. Use it to order dependent app startup.
    string[] Dependencies { get; }
    // Human-readable description of the app. Defaults to "{ClassName} App" if not specified
    string? Description { get; }
    // Stable identifier for the app that persists across class renames. Used by external systems to identify apps independently of their type name
    string? Guid { get; }
    // Display name of the app. Defaults to the class name if not specified
    string? Name { get; }
    // Unique identifier for the app. Defaults to the full type name if not specified
    string? ProductId { get; }
    // Opcode groups this app subscribes to receive messages from. Almost all apps leave this at the default; change it only for specialized protocol-level message filtering
    Opcode ReceiveOpcodeGroups { get; }
    // Opcode groups this app is allowed to send messages to. Almost all apps leave this at the default; change it only for specialized protocol-level message filtering
    Opcode SendOpcodeGroups { get; }
    // Indicates whether the app operates autonomously (Machine) or represents a human user connecting through it (Human). Defaults to Machine
    UserType UserType { get; }
    // Version number of the app
    int Version { get; }
  // Register every route before calling StartAsync; routes added afterward are not served.
  sealed class AppEndpointHost : IAsyncDisposable
    // Creates a new HTTP/WebSocket endpoint host. The relay tunnel is not allocated until StartAsync is called.
    // app: The app instance.
    // secure: When true (the default) the public URL is https://… with TLS terminated at the relay. When false, plain http://….
    // webSocketKeepAliveInterval: WebSocket keep-alive ping interval. Defaults to 10 seconds.
    // stablePortName: When non-empty, the relay assigns a deterministic public port for this name, so PublicUrl stays the same across reconnects and process restarts. Empty = ephemeral.
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    // True once the relay tunnel is allocated and PublicUrl can be read. False before StartAsync, and after it when the relay was unreachable — the host then serves on LocalPort only and retries the allocation in the background; subscribe to PublicUrlAvailable to learn when the tunnel comes up.
    bool HasPublicUrl { get; }
    // Throws InvalidOperationException when read before StartAsync has completed.
    int LocalPort { get; }
    // Invoked once per inbound HTTP/WebSocket request before it is routed. Used to mark external activity (e.g. reset the server's idle timer) so an endpoint-served instance isn't reaped while it is serving traffic. Null = no hook.
    Action? OnRequest { get; set; }
    // Throws InvalidOperationException when read before the relay tunnel is allocated; guard with HasPublicUrl when the relay may be unreachable.
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
    // The framework closes and disposes the socket once the handler returns; do not dispose it or use it past the handler's completion.
    void MapWebSocket(string pattern, Func<HttpContext, WebSocket, Task> handler)
    // Returns as soon as the host is serving and keeps running in the background — it does not block for the host's lifetime. A failed relay allocation is non-fatal.
    Task StartAsync(CancellationToken cancellationToken = default)
    // Stops the endpoint host gracefully. Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = default)
    // Only for an app whose endpoints are useless without their public URL, and which would rather start late than start wrong — a relay being redeployed takes a few seconds to come back. Do NOT await this on the app initialization path of an app that renders UI: it blocks first paint on something the app does not need in order to draw.
    Task<bool> WaitForPublicUrlAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    // Fires only for the background-retry allocation; not raised when the tunnel was already allocated during StartAsync.
    event Action<string>? PublicUrlAvailable
  // Read precedence: a runtime-written file wins over a repo-seeded file at the same path. Writes always go to cloud storage (never the local disk), so they persist across deploys; repo-seeded files change by changing the repo. The public tree cannot READ repo-seeded files (in the cloud they live with the frontend, not the app) — it reads and writes runtime files, and GetUrlAsync covers seeded files by returning the path URL the frontend serves.
  sealed class AppFileTree
    // Deletes a runtime-written file; deleting a missing file is a no-op. A repo-seeded file cannot be deleted here — it ships with the app, so remove it from the repo instead.
    Task DeleteAsync(string path, CancellationToken ct = default)
    // Whether the file exists — as a runtime-written file or a repo-seeded one.
    Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    // The URL a browser (or an external service) loads this file from. A runtime-written file returns its cloud storage URL. On the public tree, any other path returns the root-relative path URL ("logo.png" → "/logo.png") the frontend serves repo-seeded statics at — derived from the path, not verified to exist. Private repo-seeded files have no URL: read them with ReadBytesAsync.
    Task<string> GetUrlAsync(string path, CancellationToken ct = default)
    // Reads a file — a runtime-written file first, then a repo-seeded one. Throws FileNotFoundException when neither exists.
    Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default)
    // Reads a file as UTF-8 text — a runtime-written file first, then a repo-seeded one. Throws FileNotFoundException when neither exists.
    Task<string> ReadTextAsync(string path, CancellationToken ct = default)
    // Writes a file to cloud storage, creating or replacing it. Pass mimeType for anything a browser will load, so it is served with the right content type.
    Task WriteBytesAsync(string path, byte[] bytes, string? mimeType = null, CancellationToken ct = default)
    // Writes UTF-8 text to cloud storage, creating or replacing the file.
    Task WriteTextAsync(string path, string text, CancellationToken ct = default)
  // The app's two file trees, one namespace each for repo-seeded and runtime-written files: Public is world-visible by URL, Data is private to the app. The repo seeds the trees (root public/ and data/ folders); the app writes to them at runtime through this API. Runtime-written files persist across deploys; repo files redeploy with the app.
  sealed class AppFiles
    // The private tree: readable only by the app. Repo-seeded files come from the app's root data/ folder (shipped with the app, read-only); files the app writes here land in private cloud storage and survive restarts and deploys.
    AppFileTree Data { get; }
    // The public tree: everything here is reachable by URL. Repo-seeded files under the app's root public/ folder are served by the frontend at their path (public/hero.png → /hero.png); files the app writes here land in public cloud storage with a stable URL. Use it for anything a browser should load — generated images, exports, share cards.
    AppFileTree Public { get; }
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own schema/*.tp files (compiled by ikon app teleport build); each carries its own Opcode.GROUP_APP_LOCAL opcode and is sent/received as a native type — no JSON marshalling. Delivery is server-controlled and explicit: SendMessageAsync<T> always takes the recipient client session IDs — there is no implicit broadcast to every client. Whether a type travels reliably or unreliably is declared on the .tp schema (unreliable = true), not here.
  static class AppMessaging
    // Filtered by the type's opcode; the handler receives the decoded payload and the sender's client session id. Dispose the returned handle to unsubscribe.
    static IDisposable OnMessage<T>(this IMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    // There is no implicit broadcast — you must pass the explicit recipient session IDs. Whether the type travels reliably or unreliably is declared on its .tp schema, not here.
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    // Send a typed app message to a single client.
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
  // The app session's ambient services — the sanctioned way for code with no IApp<TSessionIdentity, TClientParameters> reference (cell types above all) to reach the session's databases and secrets. Async-local per server session: under shared hosting several servers run in one process, each with its own instance on its own execution flow — which is why app code must reach these through AppServices.Instance and never cache them in true statics (a process-global would bleed one tenant's database and secrets into another). Cells can be CONSTRUCTED before the app instance finishes starting (the cell host instantiates cell types for endpoint discovery, and a cell-host process never runs the user's Main at all), so consumers must not assume initialization order: await WhenReadyAsync — or check IsReady from synchronous paths — before first use.
  sealed class AppServices : AsyncLocalInstance<AppServices>
    ctor()
    // The hosting app of a CELL-HOST session — the handle a cell needs to construct session services like Audio/Video and receive that session's media. Set ONLY in cell-host mode, where the session serves exactly one cell instance; null in ordinary app instances (a cell shared by many per-user instances has no single app, and media there belongs to whichever instance the client connected to).
    IAppBase? HostApp { get; }
    // False until the session's app startup has provided the services.
    bool IsReady { get; }
    Secrets Secrets { get; }
    // Create an unopened connection to one of the app's databases, or to its default one when no name is given. Provisions the built-in database on first use.
    Task<DbConnection> DatabaseAsync(string? databaseName = null)
    // Wait for readiness, then create and open a connection — the per-operation shape.
    Task<DbConnection> OpenDatabaseAsync(string? databaseName = null, CancellationToken ct = default)
    // Completes when the services are available. Safe to await from a cell constructor's background work regardless of construction order.
    Task WhenReadyAsync()
  // Delegate for async event handlers in the app lifecycle.
  delegate AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
  // Three ways to send audio, by pacing: SpeakAsync / SendSpeech are real-time paced by the speech mixer and new speech interrupts current speech with a fade — the default for spoken replies. StreamAsync plays a complete clip (decoded file, generated music) paced to real time, without the mixer's interruption semantics. SendImmediateAsync transmits at once with no pacing — only for audio already produced in real time or very short clips; a long clip sent this way arrives all at once and can overflow client audio buffers.
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
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    // Gets information about an output stream if it exists.
    // streamId: The stream id
    AudioOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Gets a client's most recent playback report for an output stream — how far it has actually rendered the audio and whether the user can currently hear it. Null when the client has not reported yet (older SDKs never report). Reports arrive roughly twice per second while audio is playing; check AudioPlaybackStatus.ReceivedAtUtc for staleness.
    // clientSessionId: The client session id
    // streamId: The output stream. Null uses the default (speech mixer) stream
    AudioPlaybackStatus? GetPlaybackStatus(int clientSessionId, string? streamId = null)
    // Delivery is unpaced: the client receives everything as fast as it encodes. Callers own the real-time pacing, so feed this method chunks as they are produced, not a whole clip at once.
    // samples: Floating point PCM samples in range [-1.0, 1.0]
    // sampleRate: Sample rate in Hz
    // channelCount: Number of audio channels
    // isFirst: True when this call carries the beginning of a clip (starts a new playback on the client)
    // isLast: True when this call carries the end of the clip (a single complete clip passes true for both)
    // streamId: Optional id to distinguish between multiple concurrent audio streams. Required when sending multiple streams simultaneously
    // totalDuration: Optional total duration of the audio to be output, if known
    // encoderOptions: Optional encoder options. Falls back to DefaultEncoderOptions if not specified
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    ValueTask SendImmediateAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Real-time paced by the speech mixer, so fast producers (typical TTS) cannot overflow client audio buffers; a chunk with a new id interrupts current playback with a fade. Returns immediately — playback happens in the background.
    // audio: Audio chunk with samples
    // effects: Optional audio effects to apply
    // analyzers: Optional audio analyzers
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    void SendSpeech(AudioChunk audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Completes at end of mixer playout (pause-aware, real-time paced), not at end of generation. Long texts are backpressure-paced against the bounded mixer buffer, so any length is safe. An interruption by a newer Speak call completes the task quietly.
    // text: The text to speak. Whitespace-only text is a no-op
    // model: The speech generator model to use
    // voice: Optional voice id. Null uses the model's default voice
    // instructions: Optional delivery instructions (tone, emotion, style). Support is model-specific; unsupported models ignore them
    // speed: Optional speaking speed, where 1.0 is normal (e.g. 0.8 is slower, 1.2 is faster). Null leaves the model's default. Support is model-specific; unsupported models ignore it
    // effects: Optional audio effects to apply
    // analyzers: Optional audio analyzers
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    // cancellationToken: Cancels generation and playback of this utterance
    Task SpeakAndWaitAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // Each call interrupts the previous one: it fades out whatever is still playing and cancels the prior call's generation, so a new utterance supersedes the old. Defaults to SpeechGeneratorModel.ElevenFlash25. Drive SpeechGenerator + SendSpeech yourself instead when you need overlapping speakers, playback that must not interrupt what is already playing, or raw access to the generated samples.
    // text: The text to speak. Whitespace-only text is a no-op
    // model: The speech generator model to use
    // voice: Optional voice id. Null uses the model's default voice
    // instructions: Optional delivery instructions (tone, emotion, style). Support is model-specific; unsupported models ignore them
    // speed: Optional speaking speed, where 1.0 is normal (e.g. 0.8 is slower, 1.2 is faster). Null leaves the model's default. Support is model-specific; unsupported models ignore it
    // effects: Optional audio effects to apply
    // analyzers: Optional audio analyzers
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    // cancellationToken: Cancels generation and playback of this utterance
    Task SpeakAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // One call streams one whole clip on its stream id. Do not run two concurrent calls on the same stream id — the interleaved frames would corrupt client playback; use distinct stream ids or await the previous call first. Cancelling stops the clip early and closes it with a final end-of-stream frame.
    // samples: Floating point PCM samples in range [-1.0, 1.0] for the whole clip
    // sampleRate: Sample rate in Hz
    // channelCount: Number of audio channels
    // streamId: Optional id to distinguish between multiple concurrent audio streams. Required when sending multiple streams simultaneously
    // encoderOptions: Optional encoder options. Falls back to DefaultEncoderOptions if not specified
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    // cancellationToken: Stops the clip early, closing the stream cleanly
    Task StreamAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, string? streamId = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, CancellationToken cancellationToken = default)
    // Call once during app setup. Mutually exclusive with UseTurnDetection, and calling it a second time throws — either conflict raises InvalidOperationException.
    // model: The speech recognizer model to use (e.g., WhisperLarge3Turbo).
    // silenceThresholdRms: RMS threshold below which the segment is treated as silence and skipped.
    // requireCorrelatedStream: When true (default), only fires for streams initiated through a CaptureButton (those with a CorrelationId). Set false to transcribe every audio stream including ad-hoc ones.
    // language: Optional language hint (e.g., "en", "fi"); empty string lets the model autodetect.
    // timeout: Per-segment recognition timeout.
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01f, bool requireCorrelatedStream = true, string language = "", TimeSpan? timeout = null)
    // Call once during app setup. Mutually exclusive with UseSpeechRecognition, and calling it a second time throws — either conflict raises InvalidOperationException.
    // model: The speech recognizer model to use (e.g., WhisperLarge3Turbo).
    // language: Optional language hint (e.g., "en", "fi"); empty string lets the model autodetect.
    // config: Optional turn detector tuning (silence windows, min speech length, VAD plug-in). Null uses defaults tuned for conversational voice.
    // speculative: When true (default), transcription starts at the probable turn end so the confirmed turn has zero added recognition latency.
    // pauseWhileAppSpeaking: When true (default), detection is suppressed while the app is audibly speaking, so the app's own voice played through speakers can't trigger turns. Set false for barge-in apps (best paired with an echo-robust TurnDetectorConfig.SpeechClassifier).
    // requireCorrelatedStream: When true (default), only detects turns on streams initiated through a CaptureButton (those with a CorrelationId). Set false to detect on every audio stream including ad-hoc ones.
    // timeout: Per-recognition timeout.
    void UseTurnDetection(SpeechRecognizerModel model = WhisperLarge3Turbo, string language = "", TurnDetectorConfig? config = null, bool speculative = true, bool pauseWhileAppSpeaking = true, bool requireCorrelatedStream = true, TimeSpan? timeout = null)
    // args.Samples are decoded float PCM at the sample rate from the stream's begin event; IsFirst/IsLast bracket one captured segment (e.g. one push-to-talk press).
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Handlers may set args.StreamingMode to control when the stream's frames are delivered (streamed live, or buffered until the total duration is known / until the last frame).
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    // Event raised when an incoming audio stream ends
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Event raised when a client reports its audio playback status — actual playout position and audibility (playing, blocked on a user gesture, or hidden). Clients send reports periodically while a stream is active and immediately on state changes. Use GetPlaybackStatus for the latest snapshot per client.
    event AsyncEventHandler<AudioPlaybackReportEventArgs> PlaybackReportReceivedAsync
    // Exactly one of this and SpeechRecognizedAsync fires per completed segment (neither fires once the app is shutting down). An app that latches busy state when capture stops — a "Transcribing..." spinner, a disabled button — must release it here as well as in SpeechRecognizedAsync; handling only the success event leaves that state stuck on for any press that produces no speech.
    event AsyncEventHandler<SpeechNotRecognizedEventArgs> SpeechNotRecognizedAsync
    // Fires only after UseSpeechRecognition or UseTurnDetection has been called once at setup; subscribing without one of those means this event never fires.
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
    // Event raised when a turn has probably ended and its speculative transcript is ready. Requires UseTurnDetection to be called once during app setup. Start downstream work (e.g. generating a reply) with the args' cancellation token: it is cancelled if the user resumes speaking; otherwise SpeechRecognizedAsync confirms the turn with the same TurnSpeculativeEventArgs.TurnId.
    event AsyncEventHandler<TurnSpeculativeEventArgs> TurnSpeculativeAsync
    // Event raised when a user starts a speech turn on a turn-detected stream. Requires UseTurnDetection to be called once during app setup. Useful as a barge-in or listening-indicator hook.
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
  record AudioOutputStreamInfo
    ctor(string StreamId, int TrackId, AudioCodec Codec, int SampleRate, int ChannelCount)
    int ChannelCount { get; init; }
    AudioCodec Codec { get; init; }
    int SampleRate { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
  // Event arguments for the Audio.PlaybackReportReceivedAsync event.
  class AudioPlaybackReportEventArgs : EventArgs
    ctor(AudioPlaybackStatus status)
    // The client's reported playback status
    AudioPlaybackStatus Status { get; }
  // A client's most recent playback report for an outgoing audio stream — how far it has actually rendered the audio and whether the user can currently hear it.
  sealed class AudioPlaybackStatus
    ctor()
    // Audio buffered on the client, awaiting playout
    TimeSpan BufferedDuration { get; init; }
    // The reporting client's session id
    int ClientSessionId { get; init; }
    // The stream epoch the report refers to
    uint Epoch { get; init; }
    // Playout position within the epoch. Null when the client cannot observe it (e.g. WebRTC playback)
    TimeSpan? PlayedDuration { get; init; }
    // When the report was received (UTC)
    DateTime ReceivedAtUtc { get; init; }
    // Whether the client is audibly playing, blocked on a user gesture, or hidden/backgrounded
    AudioPlaybackState State { get; init; }
    // The reported stream's track id
    int TrackId { get; init; }
  // Signals the server that the plugin is doing background work, preventing the idle shutdown timer from advancing. Supports ref counting for multiple concurrent background work scopes.
  class BackgroundWork
    // Calls are ref-counted: the server is notified only on the first StartAsync and the last StopAsync. Dispose the returned scope (or call StopAsync) to release — pair every Start with exactly one release or idle shutdown stays blocked.
    ValueTask<IAsyncDisposable> StartAsync()
    // Signals that one unit of background work has completed. The server is only notified when the last active scope is stopped.
    ValueTask StopAsync()
  // Options for a client-side microphone capture started with ClientFunctions.StartAudioCaptureAsync. Every property is optional; a null property leaves that setting to the client. Start from Default and override what you need.
  sealed record ClientAudioCaptureOptions
    ctor()
    // Whether the client normalizes the microphone level. Null lets the client choose.
    bool? AutoGainControl { get; init; }
    // Target encoder bitrate in bits per second. Null lets the client choose.
    int? Bitrate { get; init; }
    // Sensible speech defaults: 32 kbit/s, auto gain control and noise suppression on, echo cancellation off (nothing is being played back in the common server-transcription case). Device is left to the client; the server receives the stream.
    static ClientAudioCaptureOptions Default { get; }
    // Id of a specific microphone to use. Null uses the client's default device.
    string? DeviceId { get; init; }
    // Whether the client cancels the audio it is playing back out of the microphone signal. Needed for two-way calls on a loudspeaker; pointless — and lossy — when nothing is being played back, which is why Default leaves it off. Null lets the client choose.
    bool? EchoCancellation { get; init; }
    // Whether the client filters steady background noise out of the microphone signal. Null lets the client choose.
    bool? NoiseSuppression { get; init; }
  // Represents a contact picked from the client's contact list.
  sealed record ClientContact
    // Names: The contact's names.
    // Emails: The contact's email addresses.
    // Phones: The contact's phone numbers.
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    // The contact's email addresses.
    IReadOnlyList<string> Emails { get; init; }
    // The contact's names.
    IReadOnlyList<string> Names { get; init; }
    // The contact's phone numbers.
    IReadOnlyList<string> Phones { get; init; }
  // Each method targets the calling client resolved from the current reactive scope unless a targetId is supplied. When the target client has not registered the backing function the call degrades to the failure value (false/null/empty list) rather than throwing — except the capture methods (StartVideoCaptureAsync, StartAudioCaptureAsync, CaptureImageAsync), which throw NotSupportedException.
  static class ClientFunctions
    // Captures a single image from the client's camera.
    // options: Optional image capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support image capture.
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Requests the client to exit fullscreen mode.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> ExitFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current battery level on the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<int?> GetBatteryLevelAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the browser language preference from the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetLanguageAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current GPS location from the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<ClientLocation?> GetLocationAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the list of available media input devices on the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // The value is whatever the browser's Network Information API exposes and mixes two vocabularies: a speed class ("slow-2g", "2g", "3g", "4g") where only that is available — note a fast wifi connection commonly reports "4g" — or a connection medium ("wifi", "cellular", "ethernet", "bluetooth", "none", ...) on platforms that expose it. Treat it as an informational hint, not a reliable wifi/cellular discriminator.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetNetworkTypeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the browser timezone from the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetTimezoneAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current browser URL path and query string from the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetUrlAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Gets the current page visibility state on the client.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<ClientVisibility> GetVisibilityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Prevents or allows the screen to sleep on the client.
    // enabled: Whether to keep the screen awake.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> KeepScreenAwakeAsync(bool enabled, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts the client's sign-in flow for a redirect-based OAuth provider (e.g. "google", "microsoft"). The page navigates to the provider and returns authenticated, so the current session ends and the client reconnects with its real identity. Use from a server-drawn sign-in button in a deferred-login app; guest/email/passkey flows are client-initiated and not supported here
    // provider: The OAuth provider to sign in with (e.g. "google").
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LoginAsync(string provider, int? targetId = null, CancellationToken cancellationToken = default)
    // Prompts the client to show its login UI (deferred login flow).
    // reason: Optional reason shown in the login dialog.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LoginShowAsync(string? reason = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Clears the auth session and reloads the page, returning the client to the login screen.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LogoutAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Opens an external URL in a new browser tab on the client.
    // url: The URL to open. Must be absolute (e.g., starts with https://).
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> OpenExternalUrlAsync(string url, int? targetId = null, CancellationToken cancellationToken = default)
    // Plays a sound on the client from a URL.
    // url: The URL of the sound to play. Can be a regular URL or a data URL.
    // volume: Volume level from 0.0 to 1.0. Defaults to 1.0.
    // loop: Whether to loop the sound. Defaults to false.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> PlaySoundAsync(string url, double volume = 1.0, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Audio bytes are de-duplicated per client session by content hash: the first call uploads the data, later calls with identical bytes send only the hash reference, so a reused sound is never re-transmitted.
    // data: The audio data as a byte array.
    // mimeType: The MIME type of the audio (e.g., "audio/mp3", "audio/wav").
    // volume: Volume level from 0.0 to 1.0. Defaults to 1.0.
    // loop: Whether to loop the sound. Defaults to false.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> PlaySoundAsync(byte[] data, string mimeType, double volume = 1.0, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Requests the client to enter fullscreen mode.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> RequestFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Scrolls the page to a specific position on the client.
    // x: Horizontal scroll position in pixels.
    // y: Vertical scroll position in pixels.
    // smooth: Whether to animate the scroll.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the UI theme on the client.
    // theme: The theme to set.
    // persist: Whether to persist the theme as a user preference.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> SetThemeAsync(Theme theme, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the UI theme on the client by its wire name. Prefer SetThemeAsync for the built-in dark and light themes; this overload exists for custom theme names.
    // themeName: The theme name to set (e.g., "light", "dark", or a custom theme name).
    // persist: Whether to persist the theme as a user preference.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when themeName is null or whitespace.
    static Task<bool> SetThemeAsync(string themeName, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Updates the browser URL without triggering a page reload.
    // url: The URL path to set (relative paths only).
    // replace: If true, replaces current history entry instead of adding a new one.
    // preserveQueryParams: If true, preserves existing query parameters when the URL does not contain a query string.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Signals the build-time snapshot capture client that the current view has settled and is ready to be captured. Call when a route's content finishes loading (guard with Context.IsSnapshot); without the signal, capture falls back to a quiescence heuristic that may record loading skeletons for slow-loading routes. No-op outside snapshot capture.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> SnapshotReadyAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Starts audio capture on the client from the microphone.
    // options: Optional audio capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support audio capture.
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Starts video capture on the client from camera or screen.
    // source: The video source (Camera or Screen).
    // options: Optional video capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support video capture.
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a media capture on the client by its stream ID.
    // streamId: The stream ID of the capture to stop.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when streamId is null or whitespace.
    static Task<bool> StopCaptureAsync(string streamId, int? targetId = null, CancellationToken cancellationToken = default)
    // Stops a playing sound on the client.
    // playbackId: The playback ID returned from PlaySoundAsync.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopSoundAsync(string playbackId, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices for the given duration.
    // durationMs: The vibration duration in milliseconds.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentOutOfRangeException: Thrown when durationMs is not positive.
    static Task<bool> VibrateAsync(int durationMs, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices with a vibrate/pause pattern. Values alternate between vibration and pause durations in milliseconds, starting with a vibration — so [100, 50, 100] vibrates 100 ms, pauses 50 ms, then vibrates 100 ms again.
    // pattern: The alternating vibrate/pause durations in milliseconds.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when pattern is null, empty, or contains a negative duration.
    static Task<bool> VibrateAsync(IReadOnlyList<int> pattern, int? targetId = null, CancellationToken cancellationToken = default)
    // Triggers haptic feedback on supported devices from a pattern in its wire form. Prefer the typed overloads taking an int duration or an int pattern; this overload exists for pattern strings that already arrive pre-formatted.
    // pattern: Duration in ms, or comma-separated pattern (e.g., "200" or "100,50,100").
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when pattern is null or whitespace.
    static Task<bool> VibrateAsync(string pattern, int? targetId = null, CancellationToken cancellationToken = default)
  // Whether the client should prefer a hardware or a software video encoder. This is a preference, not a guarantee — the client falls back to whatever encoder it has.
  enum ClientHardwareAcceleration
    // Prefer a hardware encoder: lower CPU use, but the codec/parameter support is device-dependent.
    PreferHardware
    // Prefer a software encoder: more predictable across devices, at a higher CPU cost.
    PreferSoftware
  // A single still image captured on a client with ClientFunctions.CaptureImageAsync.
  sealed record ClientImageCapture
    // Mime: The image's mime type, as encoded by the client: image/jpeg or image/png.
    // Width: The image's actual width in pixels, which can differ from a requested width the client could not honor.
    // Height: The image's actual height in pixels, which can differ from a requested height the client could not honor.
    // Data: The encoded image bytes (a complete JPEG or PNG file, not raw pixels), ready to write to disk or hand to an asset or a vision model.
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
    // JPEG (image/jpeg): lossy, small — the right default for camera frames.
    Jpeg
    // PNG (image/png): lossless, much larger — for screenshots and graphics.
    Png
  // Options for a single still image captured with ClientFunctions.CaptureImageAsync. Every property is optional; a null property leaves that setting to the client. The captured image is always returned to the caller on the server.
  sealed record ClientImageCaptureOptions
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
  sealed record ClientLocation
    // Latitude: The latitude coordinate.
    // Longitude: The longitude coordinate.
    // Accuracy: The accuracy of the coordinates in meters.
    ctor(double Latitude, double Longitude, double Accuracy)
    // The accuracy of the coordinates in meters.
    double Accuracy { get; init; }
    // The latitude coordinate.
    double Latitude { get; init; }
    // The longitude coordinate.
    double Longitude { get; init; }
  // Represents a media input device available on the client.
  sealed record ClientMediaDevice
    // DeviceId: The unique identifier for the device.
    // Kind: The kind of device (audio input or video input).
    // Label: A human-readable label for the device.
    // GroupId: The group identifier for devices that share the same physical device.
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
    // The client reported a device kind this SDK does not recognize.
    Unknown
    // An audio input device, such as a microphone.
    AudioInput
    // A video input device, such as a camera.
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
    // Check if user has a specific built-in role. For roles outside UserRole, check Roles directly.
    bool HasRole(UserRole role)
    // Require that the user has the specified role. Throws RoleRequiredException if not.
    void RequireRole(UserRole role)
  // A connected client's profile is cached when it joins, so lookups for connected clients return from cache; a cache miss loads from the backend asynchronously. Lookups return null when the context carries no UserId or the backend has no matching profile.
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
    // H.264 / AVC.
    H264
    // VP8.
    Vp8
    // VP9.
    Vp9
    // AV1.
    Av1
  // Options for a client-side video capture started with ClientFunctions.StartVideoCaptureAsync. Every property is optional; a null property leaves that setting to the client. Start from DefaultCamera or DefaultScreen and override what you need.
  sealed record ClientVideoCaptureOptions
    ctor()
    // Target encoder bitrate in bits per second. Null lets the client choose.
    int? Bitrate { get; init; }
    // Sensible camera defaults: 720p (1280x720) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference. Codec, bitrate, and device are left to the client; the server receives the stream.
    static ClientVideoCaptureOptions DefaultCamera { get; }
    // Sensible screen-share defaults: 1080p (1920x1080) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference. Codec and bitrate are left to the client; the server receives the stream.
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
    // Target frame width in pixels. Null lets the client choose.
    int? Width { get; init; }
  // Where a client-side video capture takes its frames from.
  enum ClientVideoCaptureSource
    // The client's camera.
    Camera
    // A screen, window, or browser tab the user picks in the client's screen-share dialog.
    Screen
  // The page visibility state reported by a client.
  enum ClientVisibility
    // The visibility state could not be determined: no connected client, the client does not implement the visibility function, or it reported a state this SDK does not recognize.
    Unknown
    // The page is at least partially visible on the client.
    Visible
    // The page is not visible on the client (background tab, minimized window, locked screen).
    Hidden
  // Dates are inclusive and interpreted in UTC. Category filters to one usage category (e.g. llm, image-generation); EventName filters to one full usage event name (e.g. llm.openai.gpt4o.global.output-text-tokens); Scopes narrows to usage carrying the given scopes, and GroupByScopeType breaks the result down by the id of one scope type.
  sealed record CostQuery
    ctor(DateOnly StartDate, DateOnly EndDate, string? Category = null, string? EventName = null, IReadOnlyList<CostScopeFilter>? Scopes = null, string? GroupByScopeType = null)
    string? Category { get; init; }
    DateOnly EndDate { get; init; }
    string? EventName { get; init; }
    string? GroupByScopeType { get; init; }
    IReadOnlyList<CostScopeFilter>? Scopes { get; init; }
    DateOnly StartDate { get; init; }
  // Scopes are the app's own attribution: whatever the app pushed with Log.Instance.UseScope(new CustomScope(name, id)) around a piece of work is stamped on every usage that work emits, and can be filtered and grouped on here. Several filters are ANDed — usage must carry all of them.
  sealed record CostScopeFilter
    ctor(string Type, string? Value = null)
    string Type { get; init; }
    string? Value { get; init; }
  // Accessed via app.Costs. Costs are reported per day and per usage event name; credits are the billing unit. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
  sealed class CostsService
    // The date range still has to cover when the work ran: usage is stored by day, and a query is only as cheap as the range it scans. An operation that emitted no priced usage sums to zero, which is indistinguishable from one whose usage has not landed yet — see the note on aggregation delay on CostsService before showing the number as final.
    Task<double> GetCreditsForScopeAsync(string scopeType, string scopeId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    // Returns one row per day and usage event name; days without usage produce no rows. Under CostQuery.GroupByScopeType the breakdown is per scope id as well. The result is ordered by date, then event name.
    Task<IReadOnlyList<DailyCost>> GetDailyCostsAsync(CostQuery query, CancellationToken ct = default)
    // Sums the credit cost of all usage in the app's space over the date range (inclusive, UTC).
    Task<double> GetTotalCreditsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
  // A [Cron] method behaves like a [Function] in that the trigger resolves it through the FunctionRegistry by name. Applying [Cron] is enough to register the method (as a Local function) — you do not also need [Function], though combining them is fine. The handler takes no caller-supplied arguments. It may optionally accept a host-injected CronContext (fire time + schedule) and/or a CancellationToken that signals app shutdown, in any order — mirroring how an [HttpPost] handler may accept an HttpRequest. Any other parameter fails registration at startup, since the scheduler has nothing to bind it to. Overlap is allowed: a tick fires even if the previous invocation is still running, so guard re-entrancy yourself if it matters.
  sealed class CronAttribute : Attribute
    // Declares a cron job that runs on schedule.
    ctor(string schedule)
    // Optional registry-name override. When null or empty the function is registered (and triggered) under the full member name of the declaration carrying the attribute, "{DeclaringType.FullName}.{Method}" — the same identity the bundle manifest records, so the backend trigger resolves it even when the method is inherited or overridden.
    string? Name { get; init; }
    // The cron expression that schedules this method (standard 5/6-field cron syntax, e.g. "0 * * * *" for hourly). Evaluated by the backend scheduler. The platform enforces a minimum interval of 5 minutes: a faster schedule is clamped to a slower equivalent when a safe one exists, and rejected at bundle time otherwise.
    string Schedule { get; }
  // Credits is the cost in platform credits — the unit users are billed in. EventName identifies the AI model and usage kind (e.g. llm.openai.gpt4o.global.output-text-tokens) and Category is its first segment (e.g. llm). TotalUsage is the summed usage amount in the event's native unit (tokens, seconds, generations, ...). RawCostEur is the underlying provider cost in EUR and is null unless the space has raw cost visibility enabled. ScopeId is populated only under CostQuery.GroupByScopeType, and is null for usage carrying no scope of that type.
  sealed record DailyCost
    ctor(DateOnly Date, string Category, string EventName, double TotalUsage, double Credits, double? RawCostEur, string? ScopeId = null)
    string Category { get; init; }
    double Credits { get; init; }
    DateOnly Date { get; init; }
    string EventName { get; init; }
    double? RawCostEur { get; init; }
    string? ScopeId { get; init; }
    double TotalUsage { get; init; }
  // Accessed via app.Email. Every operation requires the app's space to have the Email feature enabled; a call against a non-entitled space throws FeatureNotEnabledException.
  sealed class EmailService
    // The backend resolves the id before deleting and rejects an unknown one, so a repeated delete throws HttpRequestException carrying a 404 rather than being treated as a no-op. Callers sweeping ids they no longer track should catch it.
    Task DeleteAsync(string id, CancellationToken ct = default)
    // The returned EmailAttachmentDownload owns the content stream; dispose it (e.g. await using) to release the underlying connection.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = default)
    // Lazily enumerates all received emails matching query, transparently following pages until exhausted. Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single page of received emails for the app's space. Paginate by passing the returned InboxPage.NextCursor back as InboxQuery.Cursor.
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = default)
    // Fetches a single inbound email with decrypted body and parsed envelope.
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = default)
    // A request that names a sender identity needs a verified sending domain: when the space has none, or the requested EmailSendRequest.SenderDomain is not one of the space's verified sending domains, the send throws EmailSenderNotAvailableException — catch it and resend without the sender fields to deliver from the platform's own address. Invalid field values throw ArgumentException before anything is sent, and a space without the Email feature throws FeatureNotEnabledException.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
  // Shared base for the two developer-facing inbound HTTP surfaces, the verb-named REST attributes (HttpMethodAttribute: [HttpGet], [HttpPost], …) and [Mcp]. They differ only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients; addressing, path templating, identity binding, auth, and abuse-control are identical and live here so there is exactly one place to reason about them.
  abstract class EndpointAttribute : Attribute
    // Defaults to EndpointAuth.Grant; setting AuthPolicy overrides it.
    EndpointAuth Auth { get; init; }
    // When non-empty, takes precedence over Auth.
    string? AuthPolicy { get; init; }
    // Empty = derived from the method name (kebab-cased). A {name} segment whose name matches a field of the owner's SessionIdentity record binds the routing identity; other {name} segments bind as ordinary handler parameters. Never declare a /.well-known/*, /ikon/*, or /api path — those are reserved.
    string Path { get; }
  // The built-in authorization for an endpoint — the discoverable, no-/router/-needed options. For a custom edge policy (an apiKey/hmac/ipAllow helper you defined in /router/), set EndpointAttribute.AuthPolicy to its name instead.
  enum EndpointAuth
    // Requires a valid signed grant in the URL (the default). Possession authorizes.
    Grant
    // Anonymous — no credential; identity comes from the URL, gated only by anti-abuse.
    Public
    // Always rejected. Declares an endpoint while keeping it closed.
    Deny
    // Unlike Grant, nothing here is minted by the app or pasted into a URL: the client discovers the space's authorization server, the human signs in with the space's own [Auth] Methods, and the client holds a short-lived token it refreshes itself. Anonymous sign-in methods (guest, global) cannot satisfy this — a global visitor is one shared space-wide user, so honouring it would hand every client the same identity and the same data. A space declaring only anonymous methods cannot host a User endpoint.
    User
  // Information about an HTTP endpoint exposed by the app — an [HttpGet]/[HttpPost]/[Mcp] surface. Returned by IAppBase.Endpoints for developer convenience.
  sealed record EndpointInfo
    ctor()
    // The cell type for a substrate-cell endpoint (empty for app + AppProcess-cell endpoints). When non-empty, the gateway cell-routes the request to that cell's partitioned instance, keyed by the cell's IdentityFields in the URL; empty means the endpoint resolves to the app instance.
    string CellType { get; init; }
    // The endpoint's registry name — {Owner}_{Method}, derived unconditionally from the owner type and the handler method; endpoints carry no name override. The backend resolves this name when routing.
    string FunctionName { get; init; }
    // Carries no grant: a public endpoint is callable as-is, but a grant/policy endpoint needs a working, identity-bound URL minted via IApp.MintUrlAsync.
    string PublicUrl { get; init; }
  // Fired per chunk with the raw bytes for streaming (transcode/scan/forward); the platform already writes the chunk itself. Bytes are not yet verified — the SHA-256 check runs only after the last chunk and a mismatch discards the whole upload, so never act irreversibly. Data is valid only during the callback — copy it to retain it.
  sealed record FileUploadChunkArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The total file size in bytes the client announced.
    // Data: This chunk's bytes. Only valid for the duration of the callback — copy them if you keep them.
    // BytesWritten: Total bytes received and written so far, including this chunk.
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
  // Fires only after the byte count and recomputed SHA-256 both match. Exactly one of LocalTempFilePath and AssetUri is non-null. The temp file is deleted when the app stops — move or copy it here to keep it.
  sealed record FileUploadCompleteArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The file size in bytes.
    // LocalTempFilePath: Path to the received file in a temp directory, when the upload was not redirected to the asset system. Null when AssetUri is set. The temp directory is deleted when the app stops, so move or copy anything you want to keep.
    // AssetUri: The asset the upload was written into, when an earlier hook set FileUploadResult.AssetUri. Null when the file went to a local temp file instead. Exactly one of the two is non-null. It is the same AssetUri every Asset.Instance.* call takes, so it needs no parsing — null-check it and pass .Value straight on.
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
  // Terminal hook for an upload that had started (cancel, 60 s stall, out-of-sequence chunk, byte-count or SHA-256 mismatch, write failure). Uploads the app rejected from PreStart or Start never reach here. Any partial file/asset is already deleted — clean up only app-side state.
  sealed record FileUploadErrorArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The file size in bytes the client announced.
    // ErrorMessage: Why the upload failed — the cancellation reason when the app cancelled it, otherwise the platform's description of the failure.
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
  // First hook, before any bytes transfer — the cheapest place to reject (return false or a FileUploadResult and nothing is sent). Hook order: PreStart → Start → Chunk/Progress (per chunk) → Complete on success or Error on failure. Capture Cancel to abort the upload later, e.g. from a UI cancel button.
  sealed record FileUploadPreStartArgs
    // UploadId: Id identifying this upload; the same value appears on every later hook's args.
    // FileName: The client-supplied file name. Untrusted — never join it into a path yourself.
    // MimeType: The client-supplied mime type. Untrusted — the bytes are not verified against it.
    // Size: The file size in bytes the client claims it will send. The upload fails with an error if the actual byte count differs.
    // Cancel: Aborts this upload: deletes whatever was written, fires the error hook with the reason, and tells the client to stop. Usable at any point during the upload, not just from this callback — capture it to cancel later (e.g. from a UI cancel button).
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
  sealed record FileUploadProgressArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The total file size in bytes the client announced.
    // ProgressPercentage: Bytes received so far as a percentage of Size, 0 to 100. Zero for the whole upload when the client announced a size of 0.
    // BytesUploaded: Bytes received and written so far.
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
  // Accepted defaults to true; return true; works via the implicit bool conversion. Set AssetUri to write the upload straight into the asset system instead of a local temp file.
  sealed record FileUploadResult
    ctor()
    bool Accepted { get; init; }
    AssetUri? AssetUri { get; init; }
    static implicit operator FileUploadResult(bool accepted)
  // Last chance to reject the upload, and the last hook where setting FileUploadResult.AssetUri can redirect the bytes into the asset system instead of a temp file. Only hook that carries Hash — do content-duplicate checks here.
  sealed record FileUploadStartArgs
    // UploadId: Id identifying this upload; the same value appears on every other hook's args.
    // FileName: The client-supplied file name. Untrusted — never join it into a path yourself.
    // MimeType: The client-supplied mime type. Untrusted — the bytes are not verified against it.
    // Size: The file size in bytes the client claims it will send.
    // Hash: The client-declared SHA-256 of the file contents, lowercase hex. The platform recomputes it while receiving and fails the upload with a hash mismatch if the received bytes disagree, so a match here is a genuine content identity — but it is the client's claim, not yet verification, at this point.
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
  // Marks a method on an app or cell as a GET REST endpoint. The framework mounts a route on the owner's AppEndpointHost, binds the request, invokes the method, and serializes the return value; authorization runs at the gateway edge (the endpoint's Auth /router/ policy), not in-process. Defaults to Auth = EndpointAuth.Grant (401 on the bare URL); set Auth = EndpointAuth.Public for an anonymous route. See EndpointAttribute for path templating and URL-supplied identity.
  sealed class HttpGetAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Shared base for the verb-named REST attributes ([HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch]). The verb is baked into the attribute type — there is no verb enum — which mirrors the ASP.NET Core idiom and so generates reliably from LLMs. All of them share the addressing + identity model on EndpointAttribute; only the HTTP method differs. Authorization defaults to Auth = EndpointAuth.Grant: the gateway rejects the bare URL with 401 unless the caller was handed a minted grant URL. For an endpoint meant to be anonymously reachable (a public webhook, a health check, an open REST route), set Auth = EndpointAuth.Public explicitly — see EndpointAttribute.Auth.
  abstract class HttpMethodAttribute : EndpointAttribute
    // HTTP verb as an uppercase string (GET / POST / PUT / DELETE / PATCH).
    abstract string Method { get; }
  // Marks a method as a PATCH REST endpoint. See EndpointAttribute.
  sealed class HttpPatchAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Marks a method as a POST REST endpoint — the common case (third-party webhooks included; verify the signature from the injected request context). Defaults to Auth = EndpointAuth.Grant (401 on the bare URL); a public webhook must set Auth = EndpointAuth.Public. See EndpointAttribute.
  sealed class HttpPostAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Marks a method as a PUT REST endpoint. See EndpointAttribute.
  sealed class HttpPutAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // Immutable view of an inbound HTTP request — its method, path, query, headers, and raw body. The dispatcher constructs one per inbound request and passes it to any handler that declares an HttpRequest parameter, surfacing the untrusted inputs the typed binding doesn't, such as the raw body needed to verify a webhook signature inline.
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
  // App host interface providing typed session identity and client parameters.
  interface IApp<out TSessionIdentity, out TClientParameters> : IAppBase
    // Resolves the current client from the ambient reactive scope — call it only inside UI.Root() or another ReactiveScope context; outside one there is no current client and it throws.
    virtual TClientParameters ClientParameters { get; }
    // Gets the collection of connected clients with typed parameters. Automatically synced with IAppBase.GlobalState.
    IClientCollection<TClientParameters> Clients { get; }
    // Gets the typed session identity used to determine app instance routing.
    TSessionIdentity SessionIdentity { get; }
  // Base interface for Ikon app hosts providing access to shared state, reactive infrastructure, and lifecycle events.
  interface IAppBase : IMessageChannel
    // Gets the background work tracker that prevents server idle shutdown while work is in progress.
    BackgroundWork BackgroundWork { get; }
    // Costs are reported per day and per usage event name; credits are the billing unit. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
    CostsService Costs { get; }
    // Resolved from the ambient reactive scope: null outside a client scope (e.g. background work, a timer). Identifies the client being served, never this plugin's own connection context.
    virtual Context? CurrentClientContext { get; }
    // Empty string when no client is in scope. This is the correct key for a payment customer key, subscription gating, and per-user state — always populated for a connected client (the real user id when authenticated, else a stable anonymous id).
    virtual string CurrentUserId { get; }
    // An escape hatch for libraries that need a real filesystem path. Prefer Files (Files.Data) — same seeded files, plus runtime writes that persist. Read-only in the cloud — writing to it throws.
    string DataDirectory { get; }
    // Gets the database connection configurations for this app instance.
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // It compares ABSOLUTE occupancy against a share of the memory limit, so it cannot tell an instance filling up with arrivals from an app that is simply large: an app whose own resting footprint already exceeds that share is refused from its first client onward, answering 429 to every one of them. Measure your app's idle footprint before turning this on.
    bool DynamicMaxClientsEnabled { get; set; }
    // Requires the Email feature enabled on the app's organisation/space; calls from a non-entitled space throw FeatureNotEnabledException.
    EmailService Email { get; }
    // Gets the HTTP endpoints ([HttpGet]/[HttpPost]/[Mcp] surfaces) exposed by this app instance, including ready-to-use public URLs with the current session identity and signed token prefilled. The list is built once before Main() runs, from the endpoints declared on the app class and on loaded [Cell] types.
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    // The default implementation throws so hand-rolled test doubles keep compiling; the real app host always provides it.
    virtual AppFiles Files { get; }
    // Gets the platform-wide shared state from the server containing clients, streams, and space/session info.
    GlobalState GlobalState { get; }
    // null except in local dev on a localhost address (no --host-public), where it lets an in-process client reach this exact process over loopback. Via the relay or in the cloud it is null — connect through the normal relay/ApiKey path instead.
    virtual (string Host, int Port)? LocalLoopbackEndpoint { get; }
    // 0 lifts the cap entirely, which means exactly that: nothing then stops arrivals before the container runs out of memory and the kernel kills the instance with no warning and no chance to shed load. Prefer a measured number, or turn on DynamicMaxClientsEnabled alongside it.
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
    // The app's public URL — the address a browser opens to join this app through its space domain. Replaces the app.ReactiveGlobalState.SpaceUrl.Value incantation; reading it inside UI code subscribes to changes the same way. For a URL with query parameters (e.g. a session join link) use JoinUrl.
    virtual string PublicUrl { get; }
    // Values are fetched once at startup and read synchronously; changes made with ikon app secret set while the app runs take effect only after a restart.
    Secrets Secrets { get; }
    // Consulted only during build-time snapshot capture. Returned routes are unioned with the [BootSnapshot] Routes list from ikon-config.toml, validated, and deduped.
    Func<Task<IEnumerable<string>>>? SnapshotRoutesProvider { get; set; }
    // Gets the database that backs persistent reactive state, named by StateDatabase in the app's ikon-config toml. Empty means the built-in app database. An app whose databases carry other names sets this so its state lives in Postgres rather than falling back to asset storage.
    virtual string StateDatabase { get; }
    // Call TelephonyService.GetStatusAsync to find out whether the space has telephony, or TelephonyService.GetNumbersAsync for the numbers themselves, rather than discovering either from a failed send.
    TelephonyService Telephony { get; }
    // Enabled by default. Applies only to clients that connect after it is set; already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    // Enabled by default. Disable (e.g. in Main) for apps with no audio/video or low-latency data to save per-client peer-setup cost. Applies only to clients that connect afterward; already-connected clients are unaffected until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Persist the returned bytes as your system of record — the platform's session retention is short. Blocks until the signer completes the ceremony and the platform packages the signed PDF.
    // signerClientSessionId: The client session ID whose browser should perform the signing ceremony.
    // request: The signature order specification (documents, signer policy, purpose).
    // ct: Cancellation token. The order expires server-side after the configured TTL regardless.
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default)
    // The connection comes back unopened: open it and dispose it yourself, e.g. await using var connection = await app.DatabaseAsync(); await connection.OpenAsync();. Running a command before opening throws connection not open. Name nothing and you get the app's default database — the built-in app one, or the app's own database when it declares exactly one. Naming is only needed to pick between several, and the name is the one from the Databases list in the app's env-specific ikon-config toml, applied with ikon app config and surfaced via Databases. The built-in database is provisioned on demand: an app that never asks for one is never given one, so the first call may wait while it is created. A database the app declares itself is provisioned at activation and is already there.
    // databaseName: The database to connect to, or null for the app's default one.
    // throws ArgumentException: Thrown when a named database is not among the app's databases, or when no name was given and the app has several to choose from.
    virtual Task<DbConnection> DatabaseAsync(string? databaseName = null)
    // Provisions the app's built-in database if the space does not have one yet and returns its connection info, adding it to Databases. Concurrent callers share one provisioning attempt. DatabaseAsync calls this for you; call it directly only to pay the first-use cost somewhere other than the first query.
    // throws InvalidOperationException: Thrown when the database could not be provisioned.
    virtual Task<DatabaseConnectionInfo> EnsureDefaultDatabaseAsync()
    // Completes only when the persisted deletions have finished. Erasure is idempotent — erasing a user with no stored state is a no-op.
    // userId: The user whose persistent state to erase.
    virtual Task EraseUserStateAsync(string userId)
    // Build a shareable link to this app: PublicUrl plus a query string built from queryParams — an anonymous object (or a string dictionary), following the identity-by-anonymous-object shape of MintUrlAsync. Each readable property becomes a URL-encoded name=value pair; null-valued properties are skipped. So app.JoinUrl(new { id = sessionId }) yields {PublicUrl}?id={sessionId}. Replaces hand-assembling $"{app.ReactiveGlobalState.SpaceUrl.Value}?id={sessionId}". Passing null returns PublicUrl as-is.
    // queryParams: Anonymous object (e.g. new { id = sessionId, host = true }) or string dictionary whose entries become the query string. Null for no query string.
    virtual string JoinUrl(object? queryParams = null)
    // Identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), never by URL path — the path is what minting returns. Omitting identity (null) pins this instance's own session on an app endpoint so the URL routes back here, and pins nothing on a cell endpoint. Grants are non-expiring unless you pass expiresIn.
    // endpoint: Identifies the endpoint by its HANDLER, NOT by its URL path: pass the handler method name (e.g. nameof(GetDocument)) — or the full {Owner}_{Method} registry name when the bare name is ambiguous. Use nameof so a rename stays in sync. You never pass the path here (an endpoint's path is often derived from the method name, and may be templated) — the path is what minting RETURNS, built from this handler's EndpointInfo.PublicUrl.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // Mint working URLs for several endpoints sharing one pinned identity, in a single backend round-trip. Returns a map keyed by the endpoints you passed. See MintUrlAsync.
    // endpoints: The endpoints to mint, each identified by its HANDLER (a method name such as nameof(GetDoc), or the full {Owner}_{Method} registry name) — never by its URL path. See MintUrlAsync.
    virtual Task<IReadOnlyDictionary<string, MintedUrl>> MintUrlsAsync(IEnumerable<string> endpoints, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // The counterpart to MintUrlAsync when the caller is a person rather than a registered machine. The result is NOT a URL — send it as Authorization: Bearer {token}, never as a query parameter. It is bound to this one endpoint, expires (15 minutes by default), and a call made with it runs under that user's UserScope.
    // endpoint: The endpoint's HANDLER, exactly as MintUrlAsync takes it — a method name, or the full {Owner}_{Method} registry name when the bare one is ambiguous. An owner's JSON-RPC multiplexer is {Owner}_mcp; bare "mcp" resolves only in an app with exactly one MCP surface, so an app with cells that expose tools must name the owner.
    // userId: The space user id the token runs as.
    virtual Task<MintedUserToken> MintUserTokenAsync(string endpoint, string userId, TimeSpan? expiresIn = null, IEnumerable<string>? scopes = null, CancellationToken ct = default)
    // Databases is the list the session was started with. A database created since then — with ikon app db create or from the Portal, neither of which restarts anything — is not in it. DatabaseAsync calls this for you when it meets a name it does not recognise, so an app rarely needs it directly; call it to pick up a new database without naming it, or to see one appear in Databases.
    virtual Task<IReadOnlyList<DatabaseConnectionInfo>> RefreshDatabasesAsync()
    // Bind your listener to the returned RelayEndpoint.LocalPort; the tunnel is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the endpoint to release it.
    // protocol: The endpoint protocol. EndpointProtocol.Tls enables TLS termination at the relay.
    // stablePortName: When non-empty, the relay assigns a deterministic public port for this name, so the endpoint's public URL stays the same across reconnects and process restarts. Empty = ephemeral.
    // localPort: When positive, the tunnel forwards to this local port instead of a freshly picked one — used to attach a tunnel to a listener that is already bound. 0 = pick automatically.
    // ct: Optional cancellation token.
    Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", int localPort = 0, CancellationToken ct = default)
    // Verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier. Blocks until the user completes the challenge in their browser.
    // clientSessionId: The client session ID whose browser should perform the challenge.
    // purpose: App-declared reason for the challenge, e.g. "case.delete".
    // acrValues: Optional identity-provider hints to constrain the authentication method, encoded in the platform's agreed format. When omitted, the platform uses its configured defaults.
    // clientReturnUrl: Optional URL the platform redirects the user's browser to after the IdP flow completes. The platform appends ?stepup=<completed|failed>&challengeId=<id>. When omitted, the user lands on a generic close-window page. Set this to bring the user back into the app UI after step-up.
    // ct: Cancellation token. The challenge expires server-side after the configured TTL regardless.
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
    // Fires after app creation but before Main(). Do not subscribe from inside Main() — it has already fired by then and the handler will never run.
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    // Event fired before the plugin disconnects, allowing cleanup of resources.
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
    // At-least-once delivery — the handler must be idempotent. Throwing marks the erasure incomplete and it is redelivered on a later session start.
    event AsyncEventHandler<UserDataErasureEventArgs> UserDataErasureAsync
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
    // Declare the app's dynamic public routes for build-time boot-snapshot capture (e.g. one route per store listing). The provider runs only in a snapshot-capture process; returned routes are unioned with the [BootSnapshot] Routes list from ikon-config.toml.
    static void OnSnapshotRoutes(this IAppBase app, Func<Task<IEnumerable<string>>> provider)
    // Subscribe to IAppBase.StartingAsync with a zero-arg async handler. The Starting event carries no data — there's nothing to forward.
    static void OnStarting(this IAppBase app, Func<Task> handler)
    // Subscribe to IAppBase.StoppingAsync with a zero-arg async handler.
    static void OnStopping(this IAppBase app, Func<Task> handler)
    // Subscribe to IAppBase.UserDataErasureAsync with a handler that receives the erased user's id directly. Clean APP-OWNED data here (own database tables, PII embedded in session/global values) — the platform has already erased the user's platform-managed state. Delivery is at-least-once, so the handler must be idempotent; throwing marks the erasure incomplete and it is redelivered on a later session start.
    static void OnUserDataErasure(this IAppBase app, Func<string, Task> handler)
  // Interface representing a connected client with typed parameters.
  interface IClient<out TClientParameters>
    // Gets the typed parameters for this client.
    TClientParameters Parameters { get; }
    // Gets the session id of this client — the same id used to index IClientCollection<TClientParameters> and to target client-directed APIs.
    int SessionId { get; }
  // Collection interface for accessing connected clients. Iterable for the common "broadcast / fan-out" pattern (`foreach (var client in app.Clients)`), indexable by session ID for direct lookups, and exposes Ids when only the connected-session-ids are needed.
  interface IClientCollection<out TClientParameters> : IEnumerable<IClient<TClientParameters>>
    // Gets the number of currently connected clients.
    int Count { get; }
    // Gets the connected client session IDs as an enumerable. Convenience for code that just needs the IDs without the full client objects — e.g. `foreach (var id in app.Clients.Ids) { _scores[id] = 0; }`.
    IEnumerable<int> Ids { get; }
    // Gets the client with the specified session ID, or null if not found.
    IClient<TClientParameters>? this[int clientSessionId] { get; }
  // Marker interface for custom profile attribute classes. Implement this interface on classes that define custom profile attributes.
  interface IProfileAttributes
  // The two streaming members are shaped to plug straight into Ikon.AI: ListenAsync yields what ISpeechRecognizer.RecognizeContinuousSpeechAsync consumes, and SpeakAsync takes what ISpeechGenerator.GenerateSpeechAsync produces. So a conversational loop needs no adapter between them:
  // await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new("How can I help?")));
  //
  // await foreach (var heard in ai.SpeechRecognizer.RecognizeContinuousSpeechAsync(config, call.ListenAsync()))
  // {
  //     await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new(await Reply(heard))));
  // }
  // Sample rates are handled here: the provider's telephony audio and whatever rate the model wants are resampled to meet, so an app never has to know that 8 kHz exists.
  interface IVoiceCall : IAsyncDisposable
    // The provider's id for this call, the same one its webhooks carry.
    string CallId { get; }
    // Who is calling, in E.164. Empty on a call the app placed, where there is no such person.
    string From { get; }
    // Whether the call is still up.
    bool IsConnected { get; }
    // The other end of the call, in E.164: the number they dialled on an incoming call, and the number the app asked for on one it placed.
    string To { get; }
    // Ends the call.
    Task HangUpAsync(CancellationToken ct = default)
    // Drops audio already sent but not yet heard — what barge-in needs when the caller starts talking over the agent.
    Task InterruptAsync(CancellationToken ct = default)
    // The caller's audio as it arrives, at sampleRate. Ends when the call does.
    // sampleRate: What the consumer wants, typically the recognizer's rate.
    IAsyncEnumerable<float[]> ListenAsync(int sampleRate = 16000, CancellationToken ct = default)
    // Returns once every chunk has been sent, which is before the caller has finished hearing it — the provider buffers and plays at its own rate. Use WaitForPlaybackAsync to wait for the audio to actually land, and InterruptAsync to abandon it.
    Task SpeakAsync(IAsyncEnumerable<AudioChunk> audio, CancellationToken ct = default)
    // Completes once the caller has heard everything sent so far.
    Task WaitForPlaybackAsync(CancellationToken ct = default)
  // Sibling of HttpMethodAttribute: both declare an inbound HTTP endpoint over the shared addressing + identity model (see EndpointAttribute), differing only in the wire protocol (typed HTTP vs MCP JSON-RPC) and the schema advertised to clients. Each tool is reachable two ways: through the owner's fixed JSON-RPC multiplexer ({owner}/mcp — tools/list + tools/call, and the only surface that streams notifications/progress over SSE), and as its own directly-callable POST endpoint whose body IS the tool's arguments object. That per-tool path defaults to the kebab-cased method name and is overridable via EndpointAttribute.Path — the override adjusts only this tool's own endpoint, never the shared multiplexer. The same method may also carry a verb-named REST attribute ([HttpPost] etc.); then that route serves the REST surface and the per-tool MCP endpoint is suppressed. The governance subject id is always the structural "{Type}.{Method}". The one place it parts company with its sibling is the default EndpointAttribute.Auth, which is EndpointAuth.User here rather than EndpointAuth.Grant. A grant is a signed URL handed to something the app provisioned, and an MCP client is the opposite of that: it arrives from outside, on behalf of a person, through a flow that ends in a token. Defaulting a tool to a credential no MCP client can obtain would make every tool either unreachable or, once someone widened it to get past that, wider than intended. Set Auth explicitly for a tool that really is reachable without a user.
  sealed class McpAttribute : EndpointAttribute
    // Declares an MCP tool whose own endpoint path is the kebab-cased method name.
    ctor()
    // Declares an MCP tool whose own directly-callable endpoint is served at path.
    ctor(string path)
    // Set this explicitly; the method's XML doc summary is never used as a fallback.
    string Description { get; init; }
    // MCP-wire tool name presented to clients in tools/list. Defaults to the method name when null or empty. The governance subject id is always "{Type}.{Method}" regardless of this.
    string? Name { get; init; }
    // Scopes narrow WITHIN an authorization; they do not replace it. A tool that names a scope must also be reachable — an EndpointAuth.User tool is the case this exists for, because only a token carries scopes at all. Naming one on a Public tool would be meaningless and is ignored. A caller whose token lacks the scope gets 403 with error="insufficient_scope", which is the one refusal an MCP client will re-authorize for. That is why it is a 403 and not a 401: a bare 401 says "who are you", and the client already knows.
    string Scope { get; init; }
  // Sibling of McpAttribute — same cell-method-as-callable model, different MCP verb shape: • Static resource — method takes no arguments; the URI is the literal UriTemplate with no placeholders. Lists in resources/list. • Dynamic resource — method takes parameters that map to {placeholder} segments in the URI template by name. Lists in resources/templates/list; the client crafts a concrete URI and reads it. Read-only by spec — authors should not put side effects in resource methods (the same governance hook still fires on every read with Operation = "resource", so policy authors can distinguish read access from tool dispatch).
  sealed class McpResourceAttribute : Attribute
    ctor(string uriTemplate)
    // Description shown to MCP clients so the agent (or user, via the client UI) can decide when to fetch the resource. Empty values pass through verbatim.
    string Description { get; init; }
    // MIME type advertised to clients. Defaults to text/plain for string returns and application/octet-stream for binary; override here to be more specific (text/markdown, application/json, image/png, etc.).
    string MimeType { get; init; }
    // Display name shown to MCP clients. Defaults to the method name when null or empty.
    string? Name { get; init; }
    // Required. Placeholder names must exactly match the cell method's parameter names.
    string UriTemplate { get; }
  // Event arguments for the IAppBase.MessageReceivedAsync event.
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    // Gets the received protocol message.
    ProtocolMessage Message { get; }
  // A minted endpoint URL: the working Url (the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended), the GrantId to revoke it by, and the optional ExpiresAt when a TTL was requested (grants are non-expiring by default).
  sealed record MintedUrl
    ctor(string Url, string GrantId, DateTimeOffset? ExpiresAt)
    DateTimeOffset? ExpiresAt { get; init; }
    string GrantId { get; init; }
    string Url { get; init; }
  // Deliberately not a URL. An access token in a query string is forbidden by the MCP specification and leaks into connector lists, access logs and proxies; this one belongs in a header and nowhere else.
  sealed record MintedUserToken
    ctor(string Token, string Resource, DateTimeOffset ExpiresAt)
    DateTimeOffset ExpiresAt { get; init; }
    string Resource { get; init; }
    string Token { get; init; }
  // The app's browser-history surface, reached through App.Navigation: reads and drives the URL of a connected client, and reports the navigations the client makes on its own. Navigation is per client, not per app: every path the app sets or reads belongs to one client session. The parameterless overloads act on the client of the ambient ClientScope — the client whose event, function call or reactive render is currently on the stack — so they must be called from a client-scoped context; the targetId overloads name the client session explicitly and work from anywhere (a background task, a timer, another client's handler). Paths under the platform-reserved prefixes /ikon and /api are rejected: the load balancer intercepts them before they ever reach the app, so navigating there would strand the client on a backend route. SetPathAsync throws ArgumentException rather than let that happen.
  class Navigation
    // The current URL path of the client in scope (query string stripped), or null outside a client scope or before any path is known. Tracked before the client's first frame renders, so route-dependent server UI can branch on it from the very first render — unlike state set from IAppBase joined handlers, which run on a background task and can lose the race against the first frame.
    string? CurrentPath { get; }
    // Round-trips to the live client over the connection rather than reading server state; returns null when the client doesn't answer or isn't connected.
    // targetId: Session id of the client to ask
    Task<string?> GetPathAsync(int targetId)
    // Acts on the client of the ambient ClientScope — call from a client-scoped context. Returns null outside a client scope or when the client doesn't answer.
    Task<string?> GetPathAsync()
    // Rejects paths under the platform-reserved /ikon and /api prefixes (throws ArgumentException) — the load balancer owns those. The client's existing query string is preserved unless path carries its own.
    // targetId: Session id of the client to navigate
    // path: App-owned path to navigate to, e.g. /orders/7
    // replace: Replaces the current history entry instead of pushing a new one, so the client's back button skips the path being left behind
    // throws ArgumentException: path falls under a platform-reserved prefix (/ikon or /api)
    Task<bool> SetPathAsync(int targetId, string path, bool replace = false)
    // Acts on the client of the ambient ClientScope — call from a client-scoped context (event handler, function call, reactive render). Rejects reserved /ikon and /api paths (throws ArgumentException), same as the targetId overload.
    // path: App-owned path to navigate to, e.g. /orders/7
    // replace: Replaces the current history entry instead of pushing a new one, so the client's back button skips the path being left behind
    // throws ArgumentException: path falls under a platform-reserved prefix (/ikon or /api)
    Task<bool> SetPathAsync(string path, bool replace = false)
    // Fires on any client URL change — link, back button, reload, or the app's own SetPathAsync. Handlers run on a background task in the navigating client's UserScope/ClientScope, so scoped reactives resolve to that client. A handler exception is logged and swallowed, never reaching the client.
    event AsyncEventHandler<NavigationPathChangedEventArgs> PathChangedAsync
  // Event arguments raised when a client navigates to a different URL — either through the app (Navigation.SetPathAsync) or on its own (a link, the browser's back button, a manual reload).
  class NavigationPathChangedEventArgs : EventArgs
    // Creates the event arguments, splitting url into path and query
    // url: The URL the client navigated to, query string included
    // clientContext: The client that navigated
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
  sealed record NotificationContent
    // Title: Notification title. Required.
    // Body: Optional body text shown below the title.
    // IconUrl: Optional URL of an icon image shown with the notification.
    // Tag: Optional collapse key — a later notification with the same tag replaces an existing one instead of stacking.
    // LaunchUrl: Optional in-app path the client navigates to when the user taps the notification.
    // Data: Optional opaque JSON payload the app receives back when the user taps the notification.
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
    // The user has not yet been asked; permission will be requested on the first send.
    Default
    // The user granted permission; notifications are shown.
    Granted
    // The user denied permission; nothing is shown until they change it in their browser/OS.
    Denied
    // The client cannot show notifications (API unavailable, or the function is not registered).
    Unsupported
  // Outcome of sending a notification to a single client session.
  sealed record NotificationSendResult
    // SessionId: The target client session id.
    // Delivered: True when the client actually displayed the notification (permission granted).
    // Permission: The client's resulting permission state after the send attempt.
    ctor(int SessionId, bool Delivered, NotificationPermission Permission)
    // True when the client actually displayed the notification (permission granted).
    bool Delivered { get; init; }
    // The client's resulting permission state after the send attempt.
    NotificationPermission Permission { get; init; }
    // The target client session id.
    int SessionId { get; init; }
  // Accessed via app.Notifications. Client permission is requested lazily on the first actual send, not when the app opens. SendToUserAsync automatically falls back to offline OS push (Web Push / FCM) when the target user has no connected session.
  sealed class NotificationService
    // Shows a notification on all currently-connected client sessions. Returns one result per session.
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    // Reads a client's current notification permission state without sending anything.
    // sessionId: The target client session id.
    // ct: Optional cancellation token.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    // Shows a notification on a single connected client session. The client requests notification permission lazily (on this first send) before displaying. Returns the per-session delivery and permission outcome.
    // sessionId: The target client session id.
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // Returns one result per connected session for the user. An empty list means the user had no connected session and only offline push was attempted — it is not an error.
    // userId: The persistent user id to notify.
    // content: The notification content.
    // ct: Optional cancellation token.
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
  // A reactive value persisted per user, partitioned at runtime by UserScope. Each user sees their own value across all of their client sessions.
  class PersistentUserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // Erases one user's value: the in-memory value is dropped (the next read sees the initial value) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when Postgres read-through applies, so the erased value cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // Writes one user's value regardless of which scope — if any — is active. This is the background-task form of Value = x: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then write to it from anywhere.
    void SetFor(string userId, T value)
    // Atomically read-modify-writes one user's value, under that user's lock, regardless of which scope — if any — is active.
    void UpdateFor(string userId, Func<T, T> mutator)
    // Reads one user's value regardless of which scope — if any — is active.
    T ValueFor(string userId)
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveDictionary<TKey, TValue> : ReactiveDictionary<TKey, TValue>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<KeyValuePair<TKey, TValue>> initialEntries, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // Erases one user's dictionary: the in-memory entries are dropped (the next read sees the initial entries) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when Postgres read-through applies, so the erased entries cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // Removes the entry for key from one user's dictionary regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, TKey key)
    // Adds or replaces one entry in one user's dictionary regardless of which scope — if any — is active. This is the background-task form of this[key] = value: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. One notification.
    void SetFor(string userId, TKey key, TValue value)
    // Atomically transforms one user's entries under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveDictionary<TKey, TValue>.Update.
    void UpdateFor(string userId, Action<Dictionary<TKey, TValue>> transform)
    // Reads one user's entries regardless of which scope — if any — is active.
    IReadOnlyDictionary<TKey, TValue> ValueFor(string userId)
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveHashSet<T> : ReactiveHashSet<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // Adds item to one user's set regardless of which scope — if any — is active. This is the background-task form of Add(item): capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. Returns whether it was added. One notification either way.
    bool AddFor(string userId, T item)
    // Erases one user's set: the in-memory members are dropped (the next read sees the initial members) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when Postgres read-through applies, so the erased members cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // Removes item from one user's set regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, T item)
    // Atomically transforms one user's members under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveHashSet<T>.Update.
    void UpdateFor(string userId, Action<HashSet<T>> transform)
    // Reads one user's members regardless of which scope — if any — is active.
    IReadOnlyCollection<T> ValueFor(string userId)
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>.
  class PersistentUserReactiveList<T> : ReactiveList<T>
    ctor(PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(IEnumerable<T> initialItems, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // Appends to one user's list regardless of which scope — if any — is active. This is the background-task form of ReactiveList<T>.Add: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then mutate from anywhere. One notification.
    void AddFor(string userId, T item)
    // Erases one user's list: the in-memory items are dropped (the next read sees the initial items) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when Postgres read-through applies, so the erased items cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // Removes the first occurrence of item from one user's list regardless of which scope — if any — is active. Returns whether it was found.
    bool RemoveFor(string userId, T item)
    // Atomically replaces one user's items under that user's lock, regardless of which scope — if any — is active. Same contract as ReactiveList<T>.Update.
    void UpdateFor(string userId, Func<IReadOnlyList<T>, IEnumerable<T>> transform)
    // Reads one user's items regardless of which scope — if any — is active.
    IReadOnlyList<T> ValueFor(string userId)
  // Read-only view of a client's address.
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
  // Exception thrown when a required role is missing.
  class RoleRequiredException : Exception
    ctor(string role, string? userId = null)
    string RequiredRole { get; }
    string? UserId { get; }
  // Shards do NOT share reactive state — each shard is an independent instance of the same identity. Declare sharding only for surfaces designed for it: stateless or read-mostly apps (public landing pages, broadcast views), or apps that synchronize through external state (database, assets). Clients are not sticky to a shard across reconnects. Example:
  // [Sharded(2000)]
  // public record SessionIdentity(string? UserId, [property: Sharded(50)] string? Team);
  sealed class ShardedAttribute : Attribute
    // maxClientsPerShard: Connected-client capacity of one shard before the platform spills to the next one
    ctor(int maxClientsPerShard = 100)
    // Connected-client capacity of one shard before the platform spills to the next one
    int MaxClientsPerShard { get; }
    // Cost ceiling on the shard family size; 0 (the default) means unlimited. When every allowed shard is at capacity, new connections still join the last shard over capacity — visitors are never turned away by sharding
    int MaxShards { get; set; }
  // Event arguments raised when a captured audio segment ended without producing a transcript.
  sealed class SpeechNotRecognizedEventArgs : EventArgs
    ctor(SpeechNotRecognizedReason reason, Context clientContext, string streamId, string? correlationId, Exception? error = null)
    // Client context of the speaker.
    Context ClientContext { get; }
    // Client session id of the speaker.
    int ClientSessionId { get; }
    // Correlation id of the originating CaptureButton (null for ad-hoc audio streams).
    string? CorrelationId { get; }
    // The failure when Reason is SpeechNotRecognizedReason.Error; otherwise null.
    Exception? Error { get; }
    // Why the segment produced no text.
    SpeechNotRecognizedReason Reason { get; }
    // Stream id from which the audio was captured.
    string StreamId { get; }
    // User id of the speaker.
    string UserId { get; }
  // Why a captured audio segment produced no transcript.
  enum SpeechNotRecognizedReason
    // The segment carried no audio — typically a press released before the microphone delivered a frame.
    NoAudio
    // The segment stayed below the configured silence threshold.
    Silence
    // The recognizer ran but returned no text.
    NoText
    // The recognizer failed; the failure is in SpeechNotRecognizedEventArgs.Error.
    Error
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
  // Accessed via app.Telephony. The space needs a number first (ikon app telephony create --country se); until then every operation throws TelephonyNumberNotAvailableException, which names that command. A space may hold several numbers, in different markets and on different providers — omit from and the platform picks one, or name one to send as it. Sending is metered, so a space out of credits is suspended like any other overspend.
  sealed class TelephonyService
    // The binding outlives this process: it pins an identity, not an instance, so if this one is reaped the next message provisions a fresh instance with the same identity rather than being lost. That is what makes an app wake up when someone texts it. Running locally is the exception. There the binding also carries this machine's instance id, which is minted fresh on every run and cannot outlive it — so a local binding is reverted automatically when the app shuts down, rather than leaving the number pointed at a dead process. It applies to every number the space holds: one number cannot serve two identities, so an app wanting inbound per user needs a number per user.
    Task BindInboundToThisInstanceAsync(CancellationToken ct = default)
    // The same IVoiceCall an incoming call gives, so a conversation reads the same whichever end started it — and plugs into Ikon.AI the same way:
    // await using var call = await app.Telephony.CallAsync("+358401234567");
    // await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new("Your build finished")));
    // Returns only once the call is connected and audio can flow; it throws if nobody answers before ringTimeout. Dispose it — or call IVoiceCall.HangUpAsync — to end the call. The call is metered and bounded like any other: it counts against the space's concurrent-call limit, carries the platform duration cap, and is refused for a destination the platform does not allow.
    // from: Which of the app's numbers to call from. Omit to let the platform choose: the app's default number if it has one, else a number local to the destination's market, else the first it holds. Naming a number the app does not hold is refused rather than substituted.
    Task<IVoiceCall> CallAsync(string to, TimeSpan? ringTimeout = null, string? from = null, CancellationToken ct = default)
    // Worth reading when the app wants to choose a sender itself rather than let the platform pick one — to answer as the same number a user last saw, say. Most apps never need it: omitting from already sends from a number local to the recipient.
    Task<IReadOnlyList<TelephonyNumber>> GetNumbersAsync(CancellationToken ct = default)
    // Reports whether telephony is enabled for the app's space and which numbers it holds. Use it to decide whether to offer SMS or calling at all, rather than discovering it from a failed send.
    Task<TelephonyStatus> GetStatusAsync(CancellationToken ct = default)
    // The caller's audio reaches the handler as it is spoken and the app can speak back over the same call; see IVoiceCall for the conversational loop. Nothing else has to be configured. Calling this tells the platform that this app answers calls, which is when the provider side is wired up — so an app can start answering the phone without anyone touching a number, and a call that arrives while the app is not running starts it, exactly as an incoming message does.
    Task HandleCallsAsync(Func<IVoiceCall, Task> handler, CancellationToken ct = default)
    // Sends inbound back to the app's default shared instance, undoing BindInboundToThisInstanceAsync.
    Task ResetInboundAsync(CancellationToken ct = default)
    // Check SmsSendResult.Replyable on the result: when it is false the recipient received the message but cannot answer it, because the space holds no number local to their market and a foreign sender is stripped in transit. Long messages are split into billable segments; SmsSendResult.Parts reports how many were charged.
    // from: Which of the app's numbers to send as. Omit to let the platform choose: the app's default number if it has one, else a number local to the recipient's market — which is what keeps a message replyable — else the first it holds. Naming a number the app does not hold is refused rather than substituted, since sending as a different number reaches the recipient as a stranger.
    Task<SmsSendResult> SendSmsAsync(string to, string text, string? from = null, CancellationToken ct = default)
    // The app declares no webhook: the platform owns the endpoint the provider posts to and delivers the message here, so a message reaches whichever instance inbound is bound to — starting one if none is running. Reply by calling SendSmsAsync with SmsMessage.From. There is deliberately no "return a string to reply" shortcut: a reply the provider sends on our behalf is billed inside the provider, where nothing can meter it or refuse it for a space out of credit.
    event Func<SmsMessage, Task>? SmsReceived
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
  // Event arguments raised when a turn has probably ended and its speculative transcript is ready (see Audio.UseTurnDetection). Start downstream work (e.g. generating a reply) with CancellationToken: it is cancelled if the user resumes speaking, and the matching SpeechRecognizedEventArgs (same TurnId) confirms the turn otherwise.
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
  // Event arguments for the IAppBase.UserDataErasureAsync event.
  class UserDataErasureEventArgs : EventArgs
    ctor(string userId)
    // Gets the id of the user whose data must be erased.
    string UserId { get; }
  // Built-in user roles. Maps to role strings stored in profile.
  enum UserRole
    // Anonymous/unauthenticated user (maps to "anonymous" role)
    Guest
    // Regular authenticated user (maps to "user" role)
    User
    // Moderator with elevated permissions (maps to "moderator" role)
    Moderator
    // Administrator with full permissions (maps to "admin" role)
    Admin
  // Handles video streaming for apps. Outgoing frames are transmitted immediately — call SendFrameAsync once per frame, paced by the caller at the source framerate (typically by forwarding each incoming frame as it arrives).
  class Video
    ctor(IAppBase app)
    // Closes all video streams.
    ValueTask CloseAllAsync()
    // Closes a video stream and sends the stream end message.
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    // Gets information about an output stream if it exists.
    // streamId: The stream id
    VideoOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Frames are transmitted immediately — the caller owns the pacing. Call once per frame at the source framerate (typically forwarding each incoming frame as it arrives); never loop over a stored clip's frames without pacing.
    // data: Encoded video frame data
    // frameNumber: Frame number in the sequence
    // isKey: Whether this is a keyframe
    // timestampInUs: Timestamp in microseconds
    // durationInUs: Frame duration in microseconds
    // codec: Video codec
    // width: Video width in pixels
    // height: Video height in pixels
    // framerate: Video framerate
    // streamId: Optional id to distinguish between multiple concurrent video streams. Required when sending multiple streams simultaneously
    // targetIds: Optional list of client session IDs to target. If null, broadcasts to all
    // trackId: Optional track id override. When specified, the protocol message will use this track id instead of an auto-assigned one. Use this when echoing WebRTC video to preserve the original track index
    ValueTask SendFrameAsync(byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, VideoCodec codec, int width, int height, double framerate, string? streamId = null, IReadOnlyList<int>? targetIds = null, int? trackId = null)
    // args.Data is encoded codec bitstream (see the codec on the stream's begin event), not decoded pixels — forward it as-is (e.g. via SendFrameAsync) or decode it before analysis.
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
    // Number of concurrent instances per addressable key. Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin CellHost.Resolve<TInterface> across them. For globals (parameterless SessionIdentity) the N instances are eager-spawned at host construction — the load-balanced auth-cell pattern. For keyed cells the N instances are spawned together on first access; sharded keyed cells must tolerate eventual consistency between shards (cells should hold no per-instance state, or persist shared state through an external store).
    int Capacity { get; init; }
    // How long a keyed cell may remain idle before CellHost.EvictIdleAsync removes it from the directory. Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
  // Each in-process server runs in its own async-local scope, so Cells.Instance resolves to that server's own host and wiring. The framework calls Initialize once at startup; apps call Connect<TInterface> for each cell access.
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // On a CLOUD run, when TInterface is an interface backed by a [Cell] type, returns a SubstrateCellProxy<TInterface> that dispatches per member: [HttpGet]/[HttpPost] methods over stateless HTTP, [Function] methods and Reactive<T> members over a standard SDK connection to the cell-host. Otherwise — a concrete-type request, or ANY cell on a LOCAL run — returns the local cell instance from this server's CellHost. Local runs host every cell in-process (there is no deployed cell-host to proxy to, and a local run is a single process), so every cell behaves as a normal shared instance locally.
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    // Dispose every live cell-host connection. Call on app shutdown. Idempotent.
    ValueTask DisposeAsync()
    // Reserved key in an SDK connection's parameters that names the substrate cell type to route to. The cell's SessionIdentity-record fields ride alongside it. MUST stay in sync with the cloud's CELL_TYPE_PARAM in cell-routing.ts — that's what the backend's app-session start keys on to provision a cell-host session.
    const string CellTypeParam
  // Framework handle injected into a cell's primary constructor. Exposes the SessionIdentity the cell was instantiated for; future revisions add lifetime, config, etc.
  interface ICell<out TSessionIdentity>
    // The SessionIdentity record value this cell instance is keyed by.
    TSessionIdentity Identity { get; }

namespace Ikon.App.Cron
  // Per-invocation context for a CronAttribute handler currently executing. A cron handler may optionally accept one of these (and/or a CancellationToken) to learn when and why it fired; a parameterless handler is equally valid. AsyncLocal so handler code (and anything it calls) can read it without threading it through every method signature.
  sealed record CronContext
    ctor(DateTime FireTimeUtc, string Schedule)
    // The cron context for the invocation currently running on this async flow, or null.
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
    // Case-insensitive lookup of a request header. UNTRUSTED request input — read it for handler logic (e.g. endpoint signature verification), NEVER to derive the SessionIdentity. Identity is resolved upstream before the handler runs and is the only thing that picks the target instance; headers cannot move it. Returns null when the header is absent. The accessor is case-insensitive because HTTP header names are, and the two dispatch paths build the header dictionary with different comparers.
    string? Header(string name)
    static IDisposable Use(HttpCallContext context)

namespace Ikon.App.Mcp
  // Per-request context for an MCP tools/call or resources/read in flight. AsyncLocal so the bridge can read it from inside parameter binding without threading another argument through every call site. Carries: • The request's effective CancellationToken (linked to the transport CT and a per-request CTS the host can trip on notifications/cancelled). • An optional progress sink the bridge wires IProgress<T> parameters into. • SessionIdentityFields — the authenticated identity for this request (from claims merged by the transport). Bridges pass it to CellHost.ResolveByCellTypeName so keyed cells route to the right instance. Empty / null on the stdio path (single-user process).
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
  // How a PaymentEntitlement was obtained.
  enum EntitlementSource
    Unknown
    Subscription
    OneTime
  // The price for a created offer. Omit Interval for a one-time offer.
  sealed record OfferPriceSpec
    ctor(long AmountMinor, string Currency, PriceKind Kind, PriceInterval? Interval = null, int? IntervalCount = null)
    long AmountMinor { get; init; }
    string Currency { get; init; }
    PriceInterval? Interval { get; init; }
    int? IntervalCount { get; init; }
    PriceKind Kind { get; init; }
  // Defines an offer to create via PaymentsService.CreateOfferAsync.
  sealed record OfferSpec
    ctor(string OfferId, string Name, OfferPriceSpec Price)
    string Name { get; init; }
    string OfferId { get; init; }
    OfferPriceSpec Price { get; init; }
  // A single payment record (a one-off charge or a subscription renewal). OfferId is null for ad-hoc charges and records written before offer tracking.
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
  // A normalized payment event the backend pushes to the app.
  sealed record PaymentEvent
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
  // A provider-hosted page the customer is redirected to in order to pay. Send them to Url.
  sealed record PaymentLink
    ctor(string Url, string Reference, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    string Reference { get; init; }
    string Url { get; init; }
  // A purchasable offer in the app's catalog — recurring (subscription) or one-time, per its prices.
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
  // The payment provider that moves the money. A command uses the space's enabled provider unless it names one, either per call or by pinning PaymentsService.DefaultProvider.
  enum PaymentProvider
    Stripe
    Mollie
    Surfboard
  // A receipt for a completed payment. Url is a provider-hosted receipt page. Pdf holds downloadable PDF bytes only when the provider exposes one; today every provider (Stripe, Surfboard) returns a hosted URL only, so Pdf is null — the field is populated when a provider offers a PDF.
  sealed record PaymentReceipt
    ctor(string? Url, byte[]? Pdf, string? PdfContentType)
    byte[]? Pdf { get; init; }
    string? PdfContentType { get; init; }
    string? Url { get; init; }
  // Result of a PaymentsService.ReconcileAsync request. Enqueued counts the provider objects queued for re-processing; their effects arrive asynchronously as normal payment events.
  sealed record PaymentReconcileResult
    ctor(PaymentProvider? Provider, int Enqueued)
    int Enqueued { get; init; }
    PaymentProvider? Provider { get; init; }
  // Result of a refund.
  sealed record PaymentRefund
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
    // Offer the entitlement is keyed to.
    string OfferId { get; }
    override IFunctionPolicy CreatePolicy()
  // Reached via app.Payments; one instance per app. Every command takes an optional per-call provider; with none given it uses DefaultProvider or, failing that, the space's enabled provider. The service holds no payment state — every read hits the backend except the synchronous IsEntitled.
  sealed class PaymentsService : AsyncLocalInstance<PaymentsService>
    ctor()
    // Off by default: a payment link for a guest throws InvalidOperationException, because the guest's device-scoped user id changes when they sign in, orphaning the payment and its entitlement. Enable only for purchases that may stay behind (e.g. anonymous tips).
    bool AllowAnonymousPayments { get; set; }
    // Default cancel URL used when a command does not specify one.
    string? DefaultCancelUrl { get; set; }
    // Leave null (the default) so each command uses the space's enabled provider; set it only to pin one provider for an app with several enabled. A per-call provider argument overrides it.
    PaymentProvider? DefaultProvider { get; set; }
    // Default success URL used when a command does not specify one.
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
    // The app's catalog of purchasable offers.
    Task<IReadOnlyList<PaymentOffer>> ListOffersAsync(CancellationToken cancellationToken = default)
    // The customer's payments. customerKey defaults to the current user.
    Task<IReadOnlyList<Payment>> ListPaymentsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // The customer's subscriptions. customerKey defaults to the current user.
    Task<IReadOnlyList<PaymentSubscription>> ListSubscriptionsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // Recovery path for a missed provider webhook or an app that was offline. Eventually consistent: pulled objects surface as ordinary PaymentEventReceived pushes and entitlement refreshes. A reference (a payment link's checkout-session reference or a subscription id) scopes the pull to one object; otherwise the customer's recent objects, or the space's recent window when no customer is in scope.
    Task<PaymentReconcileResult> ReconcileAsync(string? customerKey = null, string? reference = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Refunds in full by default, or partially via amountMinor. A refund does NOT revoke an entitlement the original payment granted.
    Task<PaymentRefund> RefundAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Remove an offer from the app's catalog (Stripe archives the Product/Price). Returns false if no such active offer existed.
    Task<bool> RemoveOfferAsync(string offerId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Fetch a receipt for a completed payment. PaymentReceipt.Url is a provider-hosted receipt page (present for Stripe and Surfboard). PaymentReceipt.Pdf carries downloadable PDF bytes only when the provider offers one; today both providers return a hosted URL only, so it is null.
    Task<PaymentReceipt> RequestReceiptAsync(string paymentId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Valid only while the subscription is cancel-at-period-end and its paid period has not ended; an immediate cancel or a fully-ended subscription needs a new checkout. Returns a SubscriptionResume whose SubscriptionResume.SubscriptionId may differ from the input when the provider recreated the subscription (Mollie).
    Task<SubscriptionResume> ResumeSubscriptionAsync(string subscriptionId, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Raised for each normalized payment event the backend pushes (paid, refunded, subscription renewed/canceled). Subscribing registers the receiver on first use.
    event Func<PaymentEvent, Task>? PaymentEventReceived
  // The direction of a subscription plan change — to a pricier (Upgrade) or cheaper/equal (Downgrade) offer.
  enum PlanChangeDirection
    Unknown
    Upgrade
    Downgrade
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
  // Result of PaymentsService.ChangeSubscriptionOfferAsync. Changed is false when the subscription was already on the requested offer (a no-op). On an upgrade ProrationAmountMinor was charged immediately and the new plan is active now; on a downgrade nothing is charged and the new plan takes over at the next renewal (Effective is "immediate" or "next_cycle").
  sealed record SubscriptionOfferChange
    ctor(bool Changed, PlanChangeDirection? Direction, long ProrationAmountMinor, string? ProratedChargeRef, string? Currency, string? Effective, PaymentProvider? Provider)
    bool Changed { get; init; }
    string? Currency { get; init; }
    PlanChangeDirection? Direction { get; init; }
    string? Effective { get; init; }
    string? ProratedChargeRef { get; init; }
    long ProrationAmountMinor { get; init; }
    PaymentProvider? Provider { get; init; }
  // Result of PaymentsService.ResumeSubscriptionAsync. SubscriptionId is the subscription reference after resume — a new one when the provider recreated the subscription (Mollie).
  sealed record SubscriptionResume
    ctor(bool Resumed, string? SubscriptionId, PaymentProvider? Provider)
    PaymentProvider? Provider { get; init; }
    bool Resumed { get; init; }
    string? SubscriptionId { get; init; }
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
