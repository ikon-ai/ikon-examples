namespace Ikon.App
  // The decorated class must declare the entry point as a public parameterless method named Main — synchronous void or async Task, never async void (an async void Main is never awaited, so its exceptions escape startup error handling). It is discovered by reflection and invoked once after dependencies are ready; a missing or misnamed Main throws at startup. Declare the UI and endpoints in Main and return — do not block or await indefinitely.
  sealed class AppAttribute : Attribute
    // name: Defaults to the class name
    // productId: Defaults to the full type name
    // description: Defaults to "{ClassName} App"
    // guid: Stable identity that survives class renames, for external systems
    // userType: Machine runs autonomously; Human represents a human user connecting through the app
    // receiveOpcodeGroups: Leave at the default except for specialized protocol-level message filtering
    // sendOpcodeGroups: Leave at the default except for specialized protocol-level message filtering
    // dependencies: Product IDs of apps awaited during connect, before Main() runs and StartingAsync fires
    ctor(string? name = null, string? productId = null, string? description = null, int version = 1, string? guid = null, UserType userType = Machine, Opcode receiveOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, Opcode sendOpcodeGroups = GROUP_ALL | GROUP_APP_LOCAL, string[]? dependencies = null)
    // Each listed app is awaited during connect — before this app's Main() runs and before its StartingAsync event fires — so ordering logic belongs in Main()/ StartingAsync, not in ClientJoinedAsync. Use it to order dependent app startup.
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
    // The relay tunnel is not allocated until StartAsync is called.
    // secure: When true (the default) the public URL is https://… with TLS terminated at the relay. When false, plain http://….
    // webSocketKeepAliveInterval: WebSocket keep-alive ping interval. Defaults to 10 seconds.
    // stablePortName: When non-empty, the relay assigns a deterministic public port for this name, so PublicUrl stays the same across reconnects and process restarts. Empty = ephemeral.
    ctor(IAppBase app, bool secure = true, TimeSpan? webSocketKeepAliveInterval = null, string stablePortName = "")
    // False before StartAsync, and after it when the relay was unreachable — the host then serves on LocalPort only and retries the allocation in the background; subscribe to PublicUrlAvailable to learn when the tunnel comes up.
    bool HasPublicUrl { get; }
    // Throws InvalidOperationException when read before StartAsync has completed.
    int LocalPort { get; }
    // Marks external activity (e.g. resets the server's idle timer) so an endpoint-served instance isn't reaped while serving traffic. Null = no hook.
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
    // Waits up to 5 seconds for pending requests to complete.
    Task StopAsync(CancellationToken cancellationToken = default)
    // Only for an app whose endpoints are useless without their public URL, and which would rather start late than start wrong — a relay being redeployed takes a few seconds to come back. Do NOT await this on the app initialization path of an app that renders UI: it blocks first paint on something the app does not need in order to draw.
    Task<bool> WaitForPublicUrlAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    // Fires only for the background-retry allocation; not raised when the tunnel was already allocated during StartAsync.
    event Action<string>? PublicUrlAvailable
  // One of the app's two file trees (AppFiles.Public / AppFiles.Data). Paths are plain relative file paths ("thumbnails/42.png") — no leading slash, no .. segments; anything else throws ArgumentException. Read precedence: a runtime-written file wins over a repo-seeded file at the same path. Writes always go to cloud storage (never the local disk), so they persist across deploys; repo-seeded files change by changing the repo. The public tree cannot READ repo-seeded files (in the cloud they live with the frontend, not the app) — it reads and writes runtime files, and GetUrlAsync covers seeded files by returning the path URL the frontend serves.
  sealed class AppFileTree
    // Deleting a missing file is a no-op. A repo-seeded file cannot be deleted here — it ships with the app, so remove it from the repo instead.
    Task DeleteAsync(string path, CancellationToken ct = default)
    Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    // A runtime-written file returns its cloud storage URL. On the public tree, any other path returns the root-relative path URL ("logo.png" → "/logo.png") the frontend serves repo-seeded statics at — derived from the path, not verified to exist. Private repo-seeded files have no URL: read them with ReadBytesAsync.
    Task<string> GetUrlAsync(string path, CancellationToken ct = default)
    Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default)
    Task<string> ReadTextAsync(string path, CancellationToken ct = default)
    // mimeType: Set it for anything a browser will load, so the file is served with the right content type.
    Task WriteBytesAsync(string path, byte[] bytes, string? mimeType = null, CancellationToken ct = default)
    Task WriteTextAsync(string path, string text, CancellationToken ct = default)
  // Public is world-visible by URL (repo files under the root public/ folder are served at their path: public/hero.png → /hero.png); Data is private to the app, seeded from the root data/ folder. Runtime-written files persist across deploys; repo files redeploy with the app.
  sealed class AppFiles
    AppFileTree Data { get; }
    AppFileTree Public { get; }
  // Typed app↔client custom-message helpers over the app-local Teleport channel. The payload types come from the app's own schema/*.tp files (compiled by ikon app teleport build) and are sent and received as native types — no JSON marshalling.
  static class AppMessaging
    // Filtered by the type's opcode; the handler receives the decoded payload and the sender's client session id. Dispose the returned handle to unsubscribe.
    static IDisposable OnMessage<T>(this IMessageChannel app, Func<T, int, ValueTask> handler) where T : IProtocolMessagePayload, new()
    // There is no implicit broadcast — you must pass the explicit recipient session IDs. Whether the type travels reliably or unreliably is declared on its .tp schema, not here.
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, IReadOnlyList<int> targetIds) where T : IProtocolMessagePayload
    static ValueTask SendMessageAsync<T>(this IMessageChannel app, T message, int targetClientSessionId) where T : IProtocolMessagePayload
  // The app session's ambient databases and secrets, for code with no IApp<TSessionIdentity, TClientParameters> reference — cell types above all. Reach them through AppServices.Instance and never cache them in a static: they are async-local per server session, and a process-global would bleed one tenant's database and secrets into another. A cell can be constructed before the app has started, so await WhenReadyAsync — or check IsReady — before first use.
  sealed class AppServices : AsyncLocalInstance<AppServices>
    ctor()
    // Set ONLY in cell-host mode, where the session serves exactly one cell instance; null in ordinary app instances (a cell shared by many per-user instances has no single app, and media there belongs to whichever instance the client connected to).
    IAppBase? HostApp { get; }
    bool IsReady { get; }
    Secrets Secrets { get; }
    // The connection comes back unopened. No name means the app's default database; the built-in database is provisioned on first use.
    Task<DbConnection> DatabaseAsync(string? databaseName = null)
    // Waits for readiness, then creates and opens the connection — the per-operation shape.
    Task<DbConnection> OpenDatabaseAsync(string? databaseName = null, CancellationToken ct = default)
    Task WhenReadyAsync()
  delegate AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
  // Two send lanes. Speak* shares one mixer: one utterance at a time for the whole app, and a new one fades out the old — it interrupts even between calls aimed at different clients. Send*/Play* are independent streams keyed by stream id and overlap freely.
  class Audio
    ctor(IAppBase app)
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    AudioMetrics Metrics { get; }
    SpeechMixer SpeechMixer { get; }
    ValueTask CloseAllAsync()
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    AudioOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // How far the client has actually rendered the audio and whether the user can currently hear it. Null when the client has not reported yet (older SDKs never report). Reports arrive roughly twice per second while audio is playing; check AudioPlaybackStatus.ReceivedAtUtc for staleness.
    // streamId: The output stream. Null uses the default (speech mixer) stream
    AudioPlaybackStatus? GetPlaybackStatus(int clientSessionId, string? streamId = null)
    // Two concurrent calls on ONE stream id interleave their frames and corrupt playback — give each clip its own id. Cancelling closes the stream with a final end-of-stream frame.
    // streamId: Required when several streams run at once; null uses the default stream
    // encoderOptions: Null falls back to DefaultEncoderOptions
    // cancellationToken: Stops the clip early, closing the stream cleanly
    Task PlayClipAsync(MediaTargets targets, ReadOnlyMemory<float> samples, int sampleRate, int channelCount, string? streamId = null, AudioEncoderOptions? encoderOptions = null, CancellationToken cancellationToken = default)
    // Unpaced — callers own the real-time pacing. Feed chunks as they are produced; a whole clip sent this way can overflow client buffers, so use PlayClipAsync for that.
    // isFirst: True when this call carries the beginning of a clip (starts a new playback on the client)
    // isLast: True when this call carries the end of the clip (a single complete clip passes true for both)
    // streamId: Required when several streams run at once; null uses the default stream
    // encoderOptions: Null falls back to DefaultEncoderOptions
    ValueTask SendFrameAsync(MediaTargets targets, ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null)
    // Completes at end of mixer playout (pause-aware, real-time paced), not at end of generation. Long texts are backpressure-paced against the bounded mixer buffer, so any length is safe. An interruption by a newer Speak call completes the task quietly.
    // text: The text to speak. Whitespace-only text is a no-op
    // voice: Optional voice id. Null uses the model's default voice
    // instructions: Optional delivery instructions (tone, emotion, style). Support is model-specific; unsupported models ignore them
    // speed: Optional speaking speed, where 1.0 is normal (e.g. 0.8 is slower, 1.2 is faster). Null leaves the model's default. Support is model-specific; unsupported models ignore it
    // cancellationToken: Cancels generation and playback of this utterance
    Task SpeakAndWaitAsync(MediaTargets targets, string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, CancellationToken cancellationToken = default)
    // Also cancels the previous call's generation, not just its playback. Returns once the utterance is queued; await SpeakAndWaitAsync for playout.
    // text: Whitespace-only text is a no-op
    // voice: Null uses the model's default voice
    // instructions: Delivery instructions (tone, emotion, style); unsupported models ignore them
    // speed: 1.0 is normal. Null leaves the model's default; unsupported models ignore it
    // cancellationToken: Cancels generation and playback of this utterance
    Task SpeakAsync(MediaTargets targets, string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, CancellationToken cancellationToken = default)
    // Real-time paced by the speech mixer, so fast producers (typical TTS) cannot overflow client audio buffers; a chunk with a new id interrupts current playback with a fade. Returns immediately — playback happens in the background.
    void SpeakChunk(MediaTargets targets, AudioChunk audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null)
    // Call once during app setup. Mutually exclusive with UseTurnDetection, and calling it a second time throws — either conflict raises InvalidOperationException.
    // model: The speech recognizer model to use (e.g., WhisperLarge3Turbo).
    // silenceThresholdRms: RMS threshold below which the segment is treated as silence and skipped.
    // requireCorrelatedStream: When true (default), only fires for streams initiated through a CaptureButton (those with a CorrelationId). Set false to transcribe every audio stream including ad-hoc ones.
    // language: Optional language hint (e.g., "en", "fi"); empty string lets the model autodetect.
    // timeout: Per-segment recognition timeout.
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01f, bool requireCorrelatedStream = true, string language = "", SpeechTimestamps timestamps = None, TimeSpan? timeout = null)
    // Call once during app setup. Mutually exclusive with UseSpeechRecognition, and calling it a second time throws — either conflict raises InvalidOperationException.
    // language: Language hint (e.g. "en", "fi"); empty lets the model autodetect.
    // config: Turn detector tuning; null uses defaults tuned for conversational voice.
    // speculative: Starts transcription at the probable turn end so a confirmed turn has zero added recognition latency.
    // pauseWhileAppSpeaking: Suppresses detection while the app is audibly speaking so its own voice can't trigger turns; set false for barge-in apps.
    // requireCorrelatedStream: Only detects turns on streams initiated through a CaptureButton (those with a CorrelationId); false detects on every stream.
    // timeout: Per-recognition timeout; null means one minute.
    void UseTurnDetection(SpeechRecognizerModel model = WhisperLarge3Turbo, string language = "", TurnDetectorConfig? config = null, bool speculative = true, bool pauseWhileAppSpeaking = true, bool requireCorrelatedStream = true, SpeechTimestamps timestamps = None, TimeSpan? timeout = null)
    // args.Samples are decoded float PCM at the sample rate from the stream's begin event; IsFirst/IsLast bracket one captured segment (e.g. one push-to-talk press).
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Handlers may set args.StreamingMode to control when the stream's frames are delivered (streamed live, or buffered until the total duration is known / until the last frame).
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Reports arrive periodically while a stream is active and immediately on state changes; GetPlaybackStatus holds the latest snapshot per client.
    event AsyncEventHandler<AudioPlaybackReportEventArgs> PlaybackReportReceivedAsync
    // Exactly one of this and SpeechRecognizedAsync fires per completed segment (neither fires once the app is shutting down). An app that latches busy state when capture stops — a "Transcribing..." spinner, a disabled button — must release it here as well as in SpeechRecognizedAsync; handling only the success event leaves that state stuck on for any press that produces no speech.
    event AsyncEventHandler<SpeechNotRecognizedEventArgs> SpeechNotRecognizedAsync
    // Fires only after UseSpeechRecognition or UseTurnDetection has been called once at setup; subscribing without one of those means this event never fires.
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
    // Fires only after UseTurnDetection has been called once at setup. Start downstream work (e.g. generating a reply) with the args' cancellation token: it is cancelled if the user resumes speaking; otherwise SpeechRecognizedAsync confirms the turn with the same TurnSpeculativeEventArgs.TurnId.
    event AsyncEventHandler<TurnSpeculativeEventArgs> TurnSpeculativeAsync
    // Fires only after UseTurnDetection has been called once at setup. A barge-in or listening-indicator hook.
    event AsyncEventHandler<TurnStartedEventArgs> TurnStartedAsync
  class AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, Context clientContext, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Inherited from the AudioStreamBegin (set by the originating CaptureButton); null for ad-hoc streams.
    string? CorrelationId { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    // Decoded PCM samples in range [-1.0, 1.0]
    float[] Samples { get; }
    string StreamId { get; }
    TimeSpan TotalDuration { get; set; }
    string UserId { get; }
  class AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, int sampleRate, int channelCount, Context clientContext, int trackId, string? correlationId)
    int ChannelCount { get; }
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    string Description { get; }
    int SampleRate { get; }
    string SourceType { get; }
    string StreamId { get; }
    AudioInputStreamingMode StreamingMode { get; set; }
    int TrackId { get; }
    string UserId { get; }
