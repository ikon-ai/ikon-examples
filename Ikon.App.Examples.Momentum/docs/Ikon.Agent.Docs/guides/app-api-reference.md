# App API Reference

## App API Reference

Full API reference for Ikon.App and Ikon.Common.

---

# Ikon.App Public API

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
    // app: The app instance.
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
  // Three ways to send audio, by pacing: SpeakAsync / SendSpeech are real-time paced by the speech mixer and new speech interrupts current speech with a fade — the default for spoken replies. StreamAsync plays a complete clip (decoded file, generated music) paced to real time, without the mixer's interruption semantics. SendImmediateAsync transmits at once with no pacing — only for audio already produced in real time or very short clips; a long clip sent this way arrives all at once and can overflow client audio buffers.
  class Audio
    ctor(IAppBase app)
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    AudioMetrics Metrics { get; }
    SpeechMixer SpeechMixer { get; }
    ValueTask CloseAllAsync()
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    // streamId: The stream id
    AudioOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // How far the client has actually rendered the audio and whether the user can currently hear it. Null when the client has not reported yet (older SDKs never report). Reports arrive roughly twice per second while audio is playing; check AudioPlaybackStatus.ReceivedAtUtc for staleness.
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
    // text: Whitespace-only text is a no-op
    // voice: Null uses the model's default voice
    // instructions: Delivery instructions (tone, emotion, style); unsupported models ignore them
    // speed: 1.0 is normal. Null leaves the model's default; unsupported models ignore it
    // targetIds: Null broadcasts to all clients
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
    // language: Language hint (e.g. "en", "fi"); empty lets the model autodetect.
    // config: Turn detector tuning; null uses defaults tuned for conversational voice.
    // speculative: Starts transcription at the probable turn end so a confirmed turn has zero added recognition latency.
    // pauseWhileAppSpeaking: Suppresses detection while the app is audibly speaking so its own voice can't trigger turns; set false for barge-in apps.
    // requireCorrelatedStream: Only detects turns on streams initiated through a CaptureButton (those with a CorrelationId); false detects on every stream.
    // timeout: Per-recognition timeout; null means one minute.
    void UseTurnDetection(SpeechRecognizerModel model = WhisperLarge3Turbo, string language = "", TurnDetectorConfig? config = null, bool speculative = true, bool pauseWhileAppSpeaking = true, bool requireCorrelatedStream = true, TimeSpan? timeout = null)
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
  class AudioInputStreamEndEventArgs : EventArgs
    ctor(string streamId, Context clientContext, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Inherited from the AudioStreamBegin (set by the originating CaptureButton); null for ad-hoc streams.
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
    // Null when the client cannot observe the playout position (e.g. WebRTC playback)
    TimeSpan? PlayedDuration { get; init; }
    DateTime ReceivedAtUtc { get; init; }
    AudioPlaybackState State { get; init; }
    int TrackId { get; init; }
  class BackgroundWork
    // Calls are ref-counted: the server is notified only on the first StartAsync and the last StopAsync. Dispose the returned scope (or call StopAsync) to release — pair every Start with exactly one release or idle shutdown stays blocked.
    ValueTask<IAsyncDisposable> StartAsync()
    ValueTask StopAsync()
  // Every null property leaves that setting to the client. Start from Default and override what you need.
  sealed record ClientAudioCaptureOptions
    ctor()
    bool? AutoGainControl { get; init; }
    int? Bitrate { get; init; }
    // 32 kbit/s, auto gain control and noise suppression on, echo cancellation off (nothing is being played back in the common server-transcription case); device is left to the client.
    static ClientAudioCaptureOptions Default { get; }
    string? DeviceId { get; init; }
    // Needed for two-way calls on a loudspeaker; pointless — and lossy — when nothing is being played back, which is why Default leaves it off.
    bool? EchoCancellation { get; init; }
    bool? NoiseSuppression { get; init; }
  sealed record ClientContact
    // Names: The contact's names.
    // Emails: The contact's email addresses.
    // Phones: The contact's phone numbers.
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    IReadOnlyList<string> Emails { get; init; }
    IReadOnlyList<string> Names { get; init; }
    IReadOnlyList<string> Phones { get; init; }
  // Each method targets the calling client resolved from the current reactive scope unless a targetId is supplied. When the target client has not registered the backing function the call degrades to the failure value (false/null/empty list) rather than throwing — except the capture methods (StartVideoCaptureAsync, StartAudioCaptureAsync, CaptureImageAsync), which throw NotSupportedException.
  static class ClientFunctions
    // options: Optional image capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support image capture.
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> EndLiveActivityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> ExitFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> FlushRecordingArchivesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<int?> GetBatteryLevelAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetLanguageAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<ClientLocation?> GetLocationAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // The value is whatever the browser's Network Information API exposes and mixes two vocabularies: a speed class ("slow-2g", "2g", "3g", "4g") where only that is available — note a fast wifi connection commonly reports "4g" — or a connection medium ("wifi", "cellular", "ethernet", "bluetooth", "none", ...) on platforms that expose it. Treat it as an informational hint, not a reliable wifi/cellular discriminator.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetNetworkTypeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetTimezoneAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<string?> GetUrlAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<ClientVisibility> GetVisibilityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // enabled: Whether to keep the screen awake.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> KeepScreenAwakeAsync(bool enabled, int? targetId = null, CancellationToken cancellationToken = default)
    // The page navigates to the provider and returns authenticated, so the current session ends and the client reconnects with its real identity. Use from a server-drawn sign-in button in a deferred-login app; guest/email/passkey flows are client-initiated and not supported here.
    // provider: The OAuth provider to sign in with (e.g. "google").
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LoginAsync(string provider, int? targetId = null, CancellationToken cancellationToken = default)
    // reason: Optional reason shown in the login dialog.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LoginShowAsync(string? reason = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Clears the auth session and reloads the page, returning the client to the login screen.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> LogoutAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // url: The URL to open. Must be absolute (e.g., starts with https://).
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> OpenExternalUrlAsync(string url, int? targetId = null, CancellationToken cancellationToken = default)
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
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> RequestFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // x: Horizontal scroll position in pixels.
    // y: Vertical scroll position in pixels.
    // smooth: Whether to animate the scroll.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, int? targetId = null, CancellationToken cancellationToken = default)
    // theme: The theme to set.
    // persist: Whether to persist the theme as a user preference.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> SetThemeAsync(Theme theme, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Prefer SetThemeAsync for the built-in dark and light themes; this overload exists for custom theme names.
    // themeName: The theme name to set (e.g., "light", "dark", or a custom theme name).
    // persist: Whether to persist the theme as a user preference.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when themeName is null or whitespace.
    static Task<bool> SetThemeAsync(string themeName, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // url: The URL path to set (relative paths only).
    // replace: If true, replaces current history entry instead of adding a new one.
    // preserveQueryParams: If true, preserves existing query parameters when the URL does not contain a query string.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Call when a route's content finishes loading (guard with Context.IsSnapshot); without the signal, capture falls back to a quiescence heuristic that may record loading skeletons for slow-loading routes. No-op outside snapshot capture.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> SnapshotReadyAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // options: Optional audio capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support audio capture.
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // title: Fixed for the life of the activity; the app's own name usually.
    // accentHex: The app's accent as #rrggbb, so the banner matches the app.
    // metricsJson: A JSON array of {"value","label"}, at most three shown.
    // status: The small tracked line above the metrics — a phase, a state, a name.
    // muted: Shows the activity as held or paused, which mutes the accent.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StartLiveActivityAsync(string title, string accentHex, string metricsJson, string status, bool muted = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Prefer app.Locations.StartTrackingAsync over calling this directly; each fix is pushed back to the server and surfaces via app.Locations.OnUpdate.
    // intervalSeconds: Minimum seconds between fixes.
    // distanceFilterMeters: Minimum metres of movement before a new fix is reported.
    // background: Keep streaming while the app is backgrounded.
    // notificationTitle: Android foreground-service notification title.
    // notificationBody: Android foreground-service notification body.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StartLocationUpdatesAsync(int intervalSeconds = 10, int distanceFilterMeters = 10, bool background = true, string notificationTitle = "Sharing your location", string notificationBody = "Your location is shared while this is on.", int? targetId = null, CancellationToken cancellationToken = default)
    // hertz: Samples per second per sensor; honoured approximately.
    // sensors: Bit flags matching MotionSensors.
    // batchMilliseconds: How long the client buffers before sending.
    // background: Keep reading while the app is backgrounded.
    // liveHertz: Send only this many a second, keeping the rest for the device archive; 0 sends everything.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StartMotionUpdatesAsync(int hertz = 25, int sensors = 1, int batchMilliseconds = 200, bool background = false, int liveHertz = 0, int? targetId = null, CancellationToken cancellationToken = default)
    // archiveId: Names the activity; one id is one file.
    // fixes: Record position fixes.
    // motion: Record motion samples at their full rate.
    // maxBytes: Refuse to grow the file past this.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StartRecordingArchiveAsync(string archiveId, bool fixes = true, bool motion = true, long maxBytes = 268435456, int? targetId = null, CancellationToken cancellationToken = default)
    // source: The video source (Camera or Screen).
    // options: Optional video capture options.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws NotSupportedException: Thrown when the client does not support video capture.
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // streamId: The stream ID of the capture to stop.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when streamId is null or whitespace.
    static Task<bool> StopCaptureAsync(string streamId, int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopLocationUpdatesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopMotionUpdatesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // archiveId: The id given to StartRecordingArchiveAsync.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopRecordingArchiveAsync(string archiveId, int? targetId = null, CancellationToken cancellationToken = default)
    // playbackId: The playback ID returned from PlaySoundAsync.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    static Task<bool> StopSoundAsync(string playbackId, int? targetId = null, CancellationToken cancellationToken = default)
    // metricsJson: A JSON array of {"value","label"}, at most three shown.
    // status: The small tracked line above the metrics.
    // muted: Shows the activity as held or paused.
    // targetId: Target client session id, or null for the calling client.
    // cancellationToken: Optional cancellation token.
    static Task<bool> UpdateLiveActivityAsync(string metricsJson, string status, bool muted = false, int? targetId = null, CancellationToken cancellationToken = default)
    // durationMs: The vibration duration in milliseconds.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentOutOfRangeException: Thrown when durationMs is not positive.
    static Task<bool> VibrateAsync(int durationMs, int? targetId = null, CancellationToken cancellationToken = default)
    // pattern: The alternating vibrate/pause durations in milliseconds.
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when pattern is null, empty, or contains a negative duration.
    static Task<bool> VibrateAsync(IReadOnlyList<int> pattern, int? targetId = null, CancellationToken cancellationToken = default)
    // pattern: Duration in ms, or comma-separated pattern (e.g., "200" or "100,50,100").
    // targetId: The target client session ID, or null to target the calling client resolved from the current reactive scope.
    // cancellationToken: Optional cancellation token.
    // throws ArgumentException: Thrown when pattern is null or whitespace.
    static Task<bool> VibrateAsync(string pattern, int? targetId = null, CancellationToken cancellationToken = default)
  // A preference, not a guarantee — the client falls back to whatever encoder it has.
  enum ClientHardwareAcceleration
    PreferHardware
    PreferSoftware
  sealed record ClientImageCapture
    // Mime: The image's mime type, as encoded by the client: image/jpeg or image/png.
    // Width: The image's actual width in pixels, which can differ from a requested width the client could not honor.
    // Height: The image's actual height in pixels, which can differ from a requested height the client could not honor.
    // Data: The encoded image bytes (a complete JPEG or PNG file, not raw pixels), ready to write to disk or hand to an asset or a vision model.
    ctor(string Mime, int Width, int Height, byte[] Data)
    byte[] Data { get; init; }
    int Height { get; init; }
    string Mime { get; init; }
    int Width { get; init; }
  enum ClientImageCaptureFormat
    Jpeg
    Png
  // Every null property leaves that setting to the client.
  sealed record ClientImageCaptureOptions
    ctor()
    // Null captures JPEG.
    ClientImageCaptureFormat? Format { get; init; }
    int? Height { get; init; }
    // 0.0 (smallest, most artifacts) to 1.0 (largest, near-lossless); only meaningful for ClientImageCaptureFormat.Jpeg — PNG is lossless and ignores it.
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
    // Latitude: The latitude coordinate.
    // Longitude: The longitude coordinate.
    // Accuracy: The accuracy of the coordinates in meters.
    ctor(double Latitude, double Longitude, double Accuracy)
    double Accuracy { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
  sealed record ClientMediaDevice
    // DeviceId: The unique identifier for the device.
    // Kind: The kind of device (audio input or video input).
    // Label: A human-readable label for the device.
    // GroupId: The group identifier for devices that share the same physical device.
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
    // Computed: PreferredName ?? FirstName ?? empty
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
  // Listed in ClientVideoCaptureOptions.PreferredCodecs in priority order; the client picks the first one it can actually encode with and falls back to its own default if none are available.
  enum ClientVideoCaptureCodec
    H264
    Vp8
    Vp9
    Av1
  // Every null property leaves that setting to the client. Start from DefaultCamera or DefaultScreen and override what you need.
  sealed record ClientVideoCaptureOptions
    ctor()
    int? Bitrate { get; init; }
    // 720p (1280x720) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference; codec, bitrate, and device are left to the client.
    static ClientVideoCaptureOptions DefaultCamera { get; }
    // 1080p (1920x1080) at 30 fps, a key frame every 90 frames (3 s), and a hardware encoder preference; codec and bitrate are left to the client.
    static ClientVideoCaptureOptions DefaultScreen { get; }
    // A camera id — ignored for screen capture. Null uses the client's default device.
    string? DeviceId { get; init; }
    int? Framerate { get; init; }
    ClientHardwareAcceleration? HardwareAcceleration { get; init; }
    int? Height { get; init; }
    // A receiver can only start decoding on a key frame, so this is the worst-case join latency for anyone who starts watching mid-stream, and the resync granularity after packet loss. Lower means faster joins and more bandwidth. The presets use 90 frames — three seconds at their 30 fps.
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
  // Filter parameters for a credit cost query. Dates are inclusive and interpreted in UTC. Category filters to one usage category (e.g. llm, image-generation); EventName filters to one full usage event name (e.g. llm.openai.gpt4o.global.output-text-tokens); Scopes narrows to usage carrying the given scopes, and GroupByScopeType breaks the result down by the id of one scope type.
  sealed record CostQuery
    ctor(DateOnly StartDate, DateOnly EndDate, string? Category = null, string? EventName = null, IReadOnlyList<CostScopeFilter>? Scopes = null, string? GroupByScopeType = null)
    string? Category { get; init; }
    DateOnly EndDate { get; init; }
    string? EventName { get; init; }
    string? GroupByScopeType { get; init; }
    IReadOnlyList<CostScopeFilter>? Scopes { get; init; }
    DateOnly StartDate { get; init; }
  // Narrows a cost query to usage carrying a scope; a null Value matches any id of that type. Scopes are the app's own attribution: whatever the app pushed with Log.Instance.UseScope(new CustomScope(name, id)) around a piece of work is stamped on every usage that work emits, and can be filtered and grouped on here. Several filters are ANDed — usage must carry all of them.
  sealed record CostScopeFilter
    ctor(string Type, string? Value = null)
    string Type { get; init; }
    string? Value { get; init; }
  // Credit cost surface for an Ikon app: what AI models its space has used and what that usage cost in platform credits. Accessed via app.Costs, reported per day and per usage event name. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
  sealed class CostsService
    // The date range still has to cover when the work ran: usage is stored by day, and a query is only as cheap as the range it scans. An operation that emitted no priced usage sums to zero, which is indistinguishable from one whose usage has not landed yet — see the note on aggregation delay on CostsService before showing the number as final.
    Task<double> GetCreditsForScopeAsync(string scopeType, string scopeId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    // Throws ArgumentException when CostQuery.StartDate is after CostQuery.EndDate. Returns one row per day and usage event name; days without usage produce no rows. Under CostQuery.GroupByScopeType the breakdown is per scope id as well. The result is ordered by date, then event name.
    Task<IReadOnlyList<DailyCost>> GetDailyCostsAsync(CostQuery query, CancellationToken ct = default)
    // The date range is inclusive and interpreted in UTC.
    Task<double> GetTotalCreditsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
  // A [Cron] method behaves like a [Function] in that the trigger resolves it through the FunctionRegistry by name. Applying [Cron] is enough to register the method (as a Local function) — you do not also need [Function], though combining them is fine. The handler takes no caller-supplied arguments. It may optionally accept a host-injected CronContext (fire time + schedule) and/or a CancellationToken that signals app shutdown, in any order — mirroring how an [HttpPost] handler may accept an HttpRequest. Any other parameter fails registration at startup, since the scheduler has nothing to bind it to. Overlap is allowed: a tick fires even if the previous invocation is still running, so guard re-entrancy yourself if it matters.
  sealed class CronAttribute : Attribute
    ctor(string schedule)
    // When null or empty the function is registered (and triggered) under "{DeclaringType.FullName}.{Method}" — the identity the bundle manifest records, so the backend trigger resolves it even when the method is inherited or overridden.
    string? Name { get; init; }
    // Standard 5/6-field cron syntax (e.g. "0 * * * *" for hourly), evaluated by the backend scheduler. The platform enforces a minimum interval of 5 minutes: a faster schedule is clamped to a slower equivalent when a safe one exists, and rejected at bundle time otherwise.
    string Schedule { get; }
  // Credit cost aggregate for one usage event name on one day. Credits is the cost in platform credits — the unit users are billed in. EventName identifies the AI model and usage kind (e.g. llm.openai.gpt4o.global.output-text-tokens) and Category is its first segment (e.g. llm). TotalUsage is the summed usage amount in the event's native unit (tokens, seconds, generations, ...). RawCostEur is the underlying provider cost in EUR and is null unless the space has raw cost visibility enabled. ScopeId is populated only under CostQuery.GroupByScopeType, and is null for usage carrying no scope of that type.
  sealed record DailyCost
    ctor(DateOnly Date, string Category, string EventName, double TotalUsage, double Credits, double? RawCostEur, string? ScopeId = null)
    string Category { get; init; }
    double Credits { get; init; }
    DateOnly Date { get; init; }
    string EventName { get; init; }
    double? RawCostEur { get; init; }
    string? ScopeId { get; init; }
    double TotalUsage { get; init; }
  sealed class EmailNotificationChannel : INotificationChannel
    // email: The app's email service.
    // addressOf: Returns the user's email address, or null when none is known.
    // senderLocalPart: Optional sender local part, as on EmailSendRequest.
    // senderDisplayName: Optional sender display name.
    ctor(EmailService email, Func<string, string?> addressOf, string? senderLocalPart = null, string? senderDisplayName = null)
    string Name { get; }
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  // Accessed via app.Email. Every operation requires the app's space to have the Email feature enabled; a call against a non-entitled space throws FeatureNotEnabledException.
  sealed class EmailService
    // The backend resolves the id before deleting and rejects an unknown one, so a repeated delete throws HttpRequestException carrying a 404 rather than being treated as a no-op. Callers sweeping ids they no longer track should catch it.
    Task DeleteAsync(string id, CancellationToken ct = default)
    // The returned EmailAttachmentDownload owns the content stream; dispose it (e.g. await using) to release the underlying connection.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = default)
    // Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = default)
    // Paginate by passing the returned InboxPage.NextCursor back as InboxQuery.Cursor.
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = default)
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = default)
    // A request that names a sender identity needs a verified sending domain: when the space has none, or the requested EmailSendRequest.SenderDomain is not one of the space's verified sending domains, the send throws EmailSenderNotAvailableException — catch it and resend without the sender fields to deliver from the platform's own address. Invalid field values throw ArgumentException before anything is sent, and a space without the Email feature throws FeatureNotEnabledException.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
  abstract class EndpointAttribute : Attribute
    // Defaults to EndpointAuth.Grant; setting AuthPolicy overrides it.
    EndpointAuth Auth { get; init; }
    // When non-empty, takes precedence over Auth.
    string? AuthPolicy { get; init; }
    // Empty = derived from the method name (kebab-cased). A {name} segment whose name matches a field of the owner's SessionIdentity record binds the routing identity; other {name} segments bind as ordinary handler parameters. Never declare a /.well-known/*, /ikon/*, or /api path — those are reserved.
    string Path { get; }
  enum EndpointAuth
    // Requires a valid signed grant in the URL (the default). Possession authorizes.
    Grant
    // Anonymous — no credential; identity comes from the URL, gated only by anti-abuse.
    Public
    // Always rejected. Declares an endpoint while keeping it closed.
    Deny
    // Unlike Grant, nothing here is minted by the app or pasted into a URL: the client discovers the space's authorization server, the human signs in with the space's own [Auth] Methods, and the client holds a short-lived token it refreshes itself. Anonymous sign-in methods (guest, global) cannot satisfy this — a global visitor is one shared space-wide user, so honouring it would hand every client the same identity and the same data. A space declaring only anonymous methods cannot host a User endpoint.
    User
  sealed record EndpointInfo
    ctor()
    // When non-empty, the gateway cell-routes the request to that cell's partitioned instance, keyed by the cell's IdentityFields in the URL; empty means the endpoint resolves to the app instance.
    string CellType { get; init; }
    // {Owner}_{Method}, derived unconditionally from the owner type and the handler method; the backend resolves this name when routing.
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
    long BytesWritten { get; init; }
    byte[] Data { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
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
    AssetUri? AssetUri { get; init; }
    string FileName { get; init; }
    string? LocalTempFilePath { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Terminal hook for an upload that had started (cancel, 60 s stall, out-of-sequence chunk, byte-count or SHA-256 mismatch, write failure). Uploads the app rejected from PreStart or Start never reach here. Any partial file/asset is already deleted — clean up only app-side state.
  sealed record FileUploadErrorArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The file size in bytes the client announced.
    // ErrorMessage: Why the upload failed — the cancellation reason when the app cancelled it, otherwise the platform's description of the failure.
    ctor(string UploadId, string FileName, string MimeType, long Size, string ErrorMessage)
    string ErrorMessage { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // First hook, before any bytes transfer — the cheapest place to reject (return false or a FileUploadResult and nothing is sent). Hook order: PreStart → Start → Chunk/Progress (per chunk) → Complete on success or Error on failure. Capture Cancel to abort the upload later, e.g. from a UI cancel button.
  sealed record FileUploadPreStartArgs
    // UploadId: Id identifying this upload; the same value appears on every later hook's args.
    // FileName: The client-supplied file name. Untrusted — never join it into a path yourself.
    // MimeType: The client-supplied mime type. Untrusted — the bytes are not verified against it.
    // Size: The file size in bytes the client claims it will send. The upload fails with an error if the actual byte count differs.
    // Cancel: Aborts this upload: deletes whatever was written, fires the error hook with the reason, and tells the client to stop. Usable at any point during the upload, not just from this callback — capture it to cancel later (e.g. from a UI cancel button).
    ctor(string UploadId, string FileName, string MimeType, long Size, Func<string?, Task> Cancel)
    Func<string?, Task> Cancel { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Fired once per received chunk, after the chunk has been written and acknowledged. Meant for driving a progress bar; use onChunkReceived if you need the bytes themselves.
  sealed record FileUploadProgressArgs
    // UploadId: Id identifying this upload.
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The total file size in bytes the client announced.
    // ProgressPercentage: Bytes received so far as a percentage of Size, 0 to 100. Zero for the whole upload when the client announced a size of 0.
    // BytesUploaded: Bytes received and written so far.
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
    // UploadId: Id identifying this upload; the same value appears on every other hook's args.
    // FileName: The client-supplied file name. Untrusted — never join it into a path yourself.
    // MimeType: The client-supplied mime type. Untrusted — the bytes are not verified against it.
    // Size: The file size in bytes the client claims it will send.
    // Hash: The client-declared SHA-256 of the file contents, lowercase hex. The platform recomputes it while receiving and fails the upload with a hash mismatch if the received bytes disagree, so a match here is a genuine content identity — but it is the client's claim, not yet verification, at this point.
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
  // All verbs share the addressing + identity model on EndpointAttribute. Auth defaults to EndpointAuth.Grant — the gateway answers 401 on the bare URL unless the caller holds a minted grant URL; set Auth = EndpointAuth.Public for an anonymously reachable route (a public webhook, a health check).
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
    // An escape hatch for libraries that need a real filesystem path. Prefer Files (Files.Data) — same seeded files, plus runtime writes that persist. Read-only in the cloud — writing to it throws.
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // It compares ABSOLUTE occupancy against a share of the memory limit, so it cannot tell an instance filling up with arrivals from an app that is simply large: an app whose own resting footprint already exceeds that share is refused from its first client onward, answering 429 to every one of them. Measure your app's idle footprint before turning this on.
    bool DynamicMaxClientsEnabled { get; set; }
    // Requires the Email feature enabled on the app's organisation/space; calls from a non-entitled space throw FeatureNotEnabledException.
    EmailService Email { get; }
    // Built once before Main() runs, from the endpoints declared on the app class and on loaded [Cell] types.
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    // The default implementation throws so hand-rolled test doubles keep compiling; the real app host always provides it.
    virtual AppFiles Files { get; }
    GlobalState GlobalState { get; }
    virtual LiveActivityService LiveActivity { get; }
    // null except in local dev on a localhost address (no --host-public), where it lets an in-process client reach this exact process over loopback. Via the relay or in the cloud it is null — connect through the normal relay/ApiKey path instead.
    virtual (string Host, int Port)? LocalLoopbackEndpoint { get; }
    virtual LocationService Locations { get; }
    // 0 lifts the cap entirely, which means exactly that: nothing then stops arrivals before the container runs out of memory and the kernel kills the instance with no warning and no chance to shed load. Prefer a measured number, or turn on DynamicMaxClientsEnabled alongside it.
    int MaxClients { get; set; }
    int MaxMemoryLimitMb { get; }
    virtual MotionService Motion { get; }
    // Each mount produces an independent UI stream addressable from a host UI as <ParallaxView mount="..." />. Defaults to a single mount named "ikon-ui". The value can be replaced with a longer list at any time; the render loop reacts and emits UIStreamBegin/UIStreamEnd for additions and removals.
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    NotificationService Notifications { get; }
    PaymentsService Payments { get; }
    // Reading it inside UI code subscribes to changes; for a URL with query parameters (e.g. a session join link) use JoinUrl.
    virtual string PublicUrl { get; }
    virtual RecordingArchiveService Recordings { get; }
    // Values are fetched once at startup and read synchronously; changes made with ikon app secret set while the app runs take effect only after a restart.
    Secrets Secrets { get; }
    // Consulted only during build-time snapshot capture. Returned routes are unioned with the [BootSnapshot] Routes list from ikon-config.toml, validated, and deduped.
    Func<Task<IEnumerable<string>>>? SnapshotRoutesProvider { get; set; }
    // Named by StateDatabase in the app's ikon-config toml; empty means the built-in app database. An app whose databases carry other names sets this so its state lives in Postgres rather than falling back to asset storage.
    virtual string StateDatabase { get; }
    // Call TelephonyService.GetStatusAsync to find out whether the space has telephony, or TelephonyService.GetNumbersAsync for the numbers themselves, rather than discovering either from a failed send.
    TelephonyService Telephony { get; }
    // Enabled by default. Applies only to clients that connect after it is set; already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    virtual UploadService Uploads { get; }
    // Enabled by default. Disable (e.g. in Main) for apps with no audio/video or low-latency data to save per-client peer-setup cost. Applies only to clients that connect afterward; already-connected clients are unaffected until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Persist the returned bytes as your system of record — the platform's session retention is short. Blocks until the signer completes the ceremony and the platform packages the signed PDF.
    // signerClientSessionId: The client session ID whose browser should perform the signing ceremony.
    // request: The signature order specification (documents, signer policy, purpose).
    // ct: Cancellation token. The order expires server-side after the configured TTL regardless.
    Task<SignedDocument> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default)
    // The connection comes back unopened — open and dispose it yourself: await using var connection = await app.DatabaseAsync(); await connection.OpenAsync();. Name nothing to get the app's default database — the built-in app one, or the app's own when it declares exactly one; names come from the Databases list in the app's ikon-config toml. The built-in database is provisioned on demand, so the first call may wait while it is created; a declared database is provisioned at activation.
    // databaseName: The database to connect to, or null for the app's default one.
    // throws ArgumentException: Thrown when a named database is not among the app's databases, or when no name was given and the app has several to choose from.
    virtual Task<DbConnection> DatabaseAsync(string? databaseName = null)
    // Provisions the built-in database if the space does not have one yet and adds it to Databases; concurrent callers share one provisioning attempt. DatabaseAsync calls this for you — call it directly only to pay the first-use cost somewhere other than the first query.
    // throws InvalidOperationException: Thrown when the database could not be provisioned.
    virtual Task<DatabaseConnectionInfo> EnsureDefaultDatabaseAsync()
    // Completes only when the persisted deletions have finished. Erasure is idempotent — erasing a user with no stored state is a no-op.
    // userId: The user whose persistent state to erase.
    virtual Task EraseUserStateAsync(string userId)
    // Each readable property becomes a URL-encoded name=value pair and null-valued properties are skipped, so app.JoinUrl(new { id = sessionId }) yields {PublicUrl}?id={sessionId}. Null returns PublicUrl as-is.
    // queryParams: Anonymous object (e.g. new { id = sessionId, host = true }) or string dictionary whose entries become the query string. Null for no query string.
    virtual string JoinUrl(object? queryParams = null)
    // Identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), never by URL path — the path is what minting returns. Omitting identity (null) pins this instance's own session on an app endpoint so the URL routes back here, and pins nothing on a cell endpoint. Grants are non-expiring unless you pass expiresIn.
    // endpoint: Identifies the endpoint by its HANDLER, NOT by its URL path: pass the handler method name (e.g. nameof(GetDocument)) — or the full {Owner}_{Method} registry name when the bare name is ambiguous. Use nameof so a rename stays in sync. You never pass the path here (an endpoint's path is often derived from the method name, and may be templated) — the path is what minting RETURNS, built from this handler's EndpointInfo.PublicUrl.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // One backend round-trip; the result is keyed by the endpoints you passed. See MintUrlAsync for identity pinning and grant lifetime.
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
    // Clean APP-OWNED data here (own database tables, PII embedded in session/global values) — the platform has already erased the user's platform-managed state. Delivery is at-least-once, so the handler must be idempotent.
    static void OnUserDataErasure(this IAppBase app, Func<string, Task> handler)
  interface IClient<out TClientParameters>
    TClientParameters Parameters { get; }
    int SessionId { get; }
  interface IClientCollection<out TClientParameters> : IEnumerable<IClient<TClientParameters>>
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters>? this[int clientSessionId] { get; }
  interface INotificationChannel
    // Used in NotificationInbox.NotifyAsync's channel list and in the per-user mutes — "email", "sms", "telegram", "whatsapp", or your own.
    string Name { get; }
    // Return false when the channel has no address for the user or is not configured; throw only for a real delivery failure.
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  interface IProfileAttributes
  // A phone call whose audio the app both hears and speaks, for building a voice agent. The two streaming members are shaped to plug straight into Ikon.AI: ListenAsync yields what ISpeechRecognizer.RecognizeContinuousSpeechAsync consumes, and SpeakAsync takes what ISpeechGenerator.GenerateSpeechAsync produces. So a conversational loop needs no adapter between them:
  // await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new("How can I help?")));
  //
  // await foreach (var heard in ai.SpeechRecognizer.RecognizeContinuousSpeechAsync(config, call.ListenAsync()))
  // {
  //     await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new(await Reply(heard))));
  // }
  // Sample rates are handled here: the provider's telephony audio and whatever rate the model wants are resampled to meet, so an app never has to know that 8 kHz exists.
  interface IVoiceCall : IAsyncDisposable
    string CallId { get; }
    // In E.164; empty on a call the app placed, where there is no such person.
    string From { get; }
    bool IsConnected { get; }
    // In E.164: the number they dialled on an incoming call, and the number the app asked for on one it placed.
    string To { get; }
    Task HangUpAsync(CancellationToken ct = default)
    // What barge-in needs when the caller starts talking over the agent.
    Task InterruptAsync(CancellationToken ct = default)
    // Ends when the call does.
    // sampleRate: What the consumer wants, typically the recognizer's rate.
    IAsyncEnumerable<float[]> ListenAsync(int sampleRate = 16000, CancellationToken ct = default)
    // Speaks audio to the caller, sending each chunk as it is produced. Returns once every chunk has been sent, which is before the caller has finished hearing it — the provider buffers and plays at its own rate. Use WaitForPlaybackAsync to wait for the audio to actually land, and InterruptAsync to abandon it.
    Task SpeakAsync(IAsyncEnumerable<AudioChunk> audio, CancellationToken ct = default)
    Task WaitForPlaybackAsync(CancellationToken ct = default)
  sealed record InboxItem
    // Id: Stable id, generated by the inbox.
    // Title: Notification title.
    // Body: Optional body text.
    // Kind: App-defined category, e.g. "order" or "payment". Free text.
    // LaunchUrl: Optional in-app path the UI opens when the item is tapped.
    // Data: Optional opaque payload the app stored with the item.
    // Tag: Optional collapse key — a later item with the same tag replaces this one, as it does for the push notification.
    // CreatedAt: UTC time the item was recorded.
    // Read: Whether the user has seen it.
    ctor(string Id, string Title, string? Body, string? Kind, string? LaunchUrl, string? Data, string? Tag, DateTime CreatedAt, bool Read)
    string? Body { get; init; }
    DateTime CreatedAt { get; init; }
    string? Data { get; init; }
    string Id { get; init; }
    string? Kind { get; init; }
    string? LaunchUrl { get; init; }
    bool Read { get; init; }
    string? Tag { get; init; }
    string Title { get; init; }
  // Every call answers false rather than throwing when the client cannot show one — a browser, an Android device, an iOS version below 16.2, or a Flutter app whose shell predates the bridge. A banner is a nicety and its absence must never take an app down with it.
  // await app.LiveActivity.StartAsync("Momentum", "#db176e",
  //     [new LiveMetric("0.00 km", "distance"), new LiveMetric("0:00", "moving")], "Run");
  sealed class LiveActivityService
    // Prefer EndEverywhereAsync when finishing whatever the activity was showing. A phone that reconnects — a dropped socket, a restarted app, a redeploy — comes back as a NEW session, so ending on the session that started the activity aims at an id that no longer exists and strands a live-looking banner on the lock screen.
    // sessionId: The client to clear, or null for the calling client.
    // ct: Optional cancellation token.
    Task<bool> EndAsync(int? sessionId = null, CancellationToken ct = default)
    // ct: Optional cancellation token.
    Task EndEverywhereAsync(CancellationToken ct = default)
    // title: Fixed for the life of the activity; usually the app's name.
    // accentHex: The app's accent as #rrggbb.
    // metrics: Up to three; any beyond that are not shown.
    // status: The tracked line above the metrics — a phase, a state, a kind.
    // muted: Show it held or paused, which mutes the accent.
    // sessionId: The client to show it on, or null for the calling client.
    // ct: Optional cancellation token.
    Task<bool> StartAsync(string title, string accentHex, IReadOnlyList<LiveMetric> metrics, string status, bool muted = false, int? sessionId = null, CancellationToken ct = default)
    // metrics: Up to three; any beyond that are not shown.
    // status: The tracked line above the metrics.
    // muted: Show it held or paused.
    // sessionId: The client to update, or null for the calling client.
    // ct: Optional cancellation token.
    Task<bool> UpdateAsync(IReadOnlyList<LiveMetric> metrics, string status, bool muted = false, int? sessionId = null, CancellationToken ct = default)
  sealed record LiveMetric
    // Value: Already formatted — the app owns its units and the banner must not reinvent them.
    // Label: The small caption under it, upper-cased by the banner.
    ctor(string Value, string Label)
    string Label { get; init; }
    string Value { get; init; }
  // The one-shot ClientFunctions.GetLocationAsync is a pull that only works while the client is connected and awake; this is the push model that survives backgrounding. Continuous background location needs the user's "Always"/background permission and is subject to app-store review, so start it only for a real reason (an active delivery, a live trip) and stop it when done.
  // app.Locations.OnUpdate(u => _couriers.Update(cs => cs.Select(c => c.SessionId == u.SessionId ? c with { Lat = u.Latitude, Lon = u.Longitude } : c)));
  // await app.Locations.StartTrackingAsync(ReactiveScope.ClientId, new LocationTrackingOptions(IntervalSeconds: 5));
  sealed class LocationService
    // Handlers run on the pushing client's reactive scope, so writing per-user or per-session reactive state from here just works.
    void OnUpdate(Action<LocationUpdate> handler)
    // Not for app code — call OnUpdate to observe. Public because the function registry binds to it by reflection.
    bool ReceiveLocationUpdate(double latitude, double longitude, double accuracy, double speed, double heading, double? altitude = null, double timestampMs = 0.0)
    void RemoveHandler(Action<LocationUpdate> handler)
    // Returns true when the client accepted (it supports geolocation and permission was not denied outright).
    // sessionId: The client session to track.
    // options: Interval, distance filter, background flag and the Android notification text.
    // ct: Optional cancellation token.
    Task<bool> StartTrackingAsync(int sessionId, LocationTrackingOptions? options = null, CancellationToken ct = default)
    // sessionId: The client session to stop tracking.
    // ct: Optional cancellation token.
    Task<bool> StopTrackingAsync(int sessionId, CancellationToken ct = default)
  sealed record LocationTrackingOptions
    // IntervalSeconds: Minimum seconds between reported fixes.
    // DistanceFilterMeters: Minimum metres of movement before a new fix is reported.
    // Background: Keep streaming while the app is backgrounded (Android foreground service + iOS background-location mode). When false the stream stops on backgrounding.
    // NotificationTitle: Android foreground-service notification title shown while tracking.
    // NotificationBody: Android foreground-service notification body.
    ctor(int IntervalSeconds = 10, int DistanceFilterMeters = 10, bool Background = true, string NotificationTitle = "Sharing your location", string NotificationBody = "Your location is shared while this is on.")
    bool Background { get; init; }
    int DistanceFilterMeters { get; init; }
    int IntervalSeconds { get; init; }
    string NotificationBody { get; init; }
    string NotificationTitle { get; init; }
  sealed record LocationUpdate
    // SessionId: The client session the fix came from.
    // UserId: The signed-in user id, or empty for an anonymous session.
    // Latitude: Latitude in degrees.
    // Longitude: Longitude in degrees.
    // AccuracyMeters: Reported horizontal accuracy in metres.
    // SpeedMps: Ground speed in metres/second, or 0 when unknown.
    // Heading: Heading in degrees (0–360), or -1 when unknown.
    // At: Server time the fix was received (UTC).
    // AltitudeMeters: Altitude in metres above the WGS-84 ellipsoid, or NaN when the device did not report one. Clients published before altitude was carried always report NaN.
    // MeasuredAt: Device time the fix was taken (UTC). Equal to At when the client did not report a timestamp. Prefer this over At for anything derived from elapsed time: a batch of fixes delivered after a network stall all arrive at once, so arrival time collapses the intervals between them and every speed and pace computed from it is wrong.
    ctor(int SessionId, string UserId, double Latitude, double Longitude, double AccuracyMeters, double SpeedMps, double Heading, DateTime At, double AltitudeMeters, DateTime MeasuredAt)
    double AccuracyMeters { get; init; }
    double AltitudeMeters { get; init; }
    DateTime At { get; init; }
    double Heading { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
    DateTime MeasuredAt { get; init; }
    int SessionId { get; init; }
    double SpeedMps { get; init; }
    string UserId { get; init; }
  // Sibling of HttpMethodAttribute: both declare an inbound HTTP endpoint over the shared addressing + identity model (see EndpointAttribute), differing only in the wire protocol. Each tool is reachable two ways: through the owner's fixed JSON-RPC multiplexer ({owner}/mcp — tools/list + tools/call, the only surface that streams progress over SSE), and as its own directly-callable POST endpoint whose body IS the tool's arguments object; that per-tool path defaults to the kebab-cased method name, and an EndpointAttribute.Path override adjusts only it, never the multiplexer. A method also carrying a verb-named REST attribute serves the REST surface and suppresses the per-tool MCP endpoint. The governance subject id is always "{Type}.{Method}". Unlike its sibling, EndpointAttribute.Auth defaults to EndpointAuth.User — a grant is a credential no MCP client can obtain; set Auth explicitly for a tool that really is reachable without a user.
  sealed class McpAttribute : EndpointAttribute
    ctor()
    ctor(string path)
    // Set this explicitly; the method's XML doc summary is never used as a fallback.
    string Description { get; init; }
    // Defaults to the method name when null or empty; the governance subject id stays "{Type}.{Method}" regardless.
    string? Name { get; init; }
    // Scopes narrow WITHIN an authorization; they do not replace it. A tool that names a scope must also be reachable — an EndpointAuth.User tool is the case this exists for, because only a token carries scopes at all. Naming one on a Public tool would be meaningless and is ignored. A caller whose token lacks the scope gets 403 with error="insufficient_scope", which is the one refusal an MCP client will re-authorize for. That is why it is a 403 and not a 401: a bare 401 says "who are you", and the client already knows.
    string Scope { get; init; }
  // Sibling of McpAttribute — same cell-method-as-callable model, different MCP verb shape: • Static resource — method takes no arguments; the URI is the literal UriTemplate with no placeholders. Lists in resources/list. • Dynamic resource — method takes parameters that map to {placeholder} segments in the URI template by name. Lists in resources/templates/list; the client crafts a concrete URI and reads it. Read-only by spec — authors should not put side effects in resource methods (the same governance hook still fires on every read with Operation = "resource", so policy authors can distinguish read access from tool dispatch).
  sealed class McpResourceAttribute : Attribute
    ctor(string uriTemplate)
    string Description { get; init; }
    // Defaults to text/plain for string returns and application/octet-stream for binary; override to be more specific (text/markdown, application/json, image/png).
    string MimeType { get; init; }
    // Defaults to the method name when null or empty.
    string? Name { get; init; }
    // Required. Placeholder names must exactly match the cell method's parameter names.
    string UriTemplate { get; }
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
  // Url is the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended; GrantId revokes it, and ExpiresAt is null for the default non-expiring grant.
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
  sealed record MotionBatch
    // SessionId: The client session the batch came from.
    // UserId: The signed-in user id, or empty for an anonymous session.
    // Samples: In the order the device produced them.
    // At: Server time the batch was received (UTC).
    ctor(int SessionId, string UserId, IReadOnlyList<MotionSample> Samples, DateTime At)
    DateTime At { get; init; }
    IReadOnlyList<MotionSample> Samples { get; init; }
    int SessionId { get; init; }
    string UserId { get; init; }
  sealed record MotionOptions
    // Hertz: Samples per second per sensor. 25 is plenty to tell a walk from a trot; a controller wants 60 or more. Devices honour this approximately.
    // Sensors: Which sensors to read.
    // BatchMilliseconds: How long the client buffers before sending. Sending each sample on its own would put a round trip on every one of them; batching turns fifty calls a second into five. Lower it for a controller, raise it to save battery.
    // Background: Keep streaming while the app is backgrounded. On iOS this needs an already-running background mode — motion alone does not keep an app alive, so pair it with location tracking if the app must keep reading in a pocket.
    // LiveHertz: Send only this many samples a second, while RecordingArchiveService keeps every one on the device. Zero streams everything. Use it when the live stream only drives a screen and the analysis happens afterwards.
    ctor(int Hertz = 25, MotionSensors Sensors = UserAcceleration, int BatchMilliseconds = 200, bool Background = false, int LiveHertz = 0)
    bool Background { get; init; }
    int BatchMilliseconds { get; init; }
    int Hertz { get; init; }
    int LiveHertz { get; init; }
    MotionSensors Sensors { get; init; }
  readonly record struct MotionSample
    // AtMillis: Device time in milliseconds since the epoch, when the sample was taken.
    // X: Acceleration in m/s², or rotation in rad/s, or field strength in µT.
    // Y: The second axis.
    // Z: The third axis.
    // Sensor: Which sensor produced it.
    ctor(double AtMillis, double X, double Y, double Z, MotionSensors Sensor)
    double AtMillis { get; init; }
    double Magnitude { get; }
    MotionSensors Sensor { get; init; }
    double X { get; init; }
    double Y { get; init; }
    double Z { get; init; }
  enum MotionSensors
    UserAcceleration
    Acceleration
    Gyroscope
    Magnetometer
  // Samples arrive in batches rather than one at a time, because a round trip per sample at fifty hertz is fifty round trips a second. MotionOptions.BatchMilliseconds is the knob: lower is more responsive, higher is cheaper. **This is not the right transport for a low-latency controller.** Batched function calls carry a scheduling delay of at least one batch, and every sample is delivered reliably whether or not it still matters. A phone used as a pointing device wants an unreliable app-defined .tp message instead, where a dropped sample is simply superseded by the next one. Use this for analysis — gait, cadence, activity, impact — and a .tp channel for control.
  // app.Motion.OnBatch(batch => _cadence.Push(batch.Samples));
  // await app.Motion.StartTrackingAsync(ReactiveScope.ClientId,
  //     new MotionOptions(Hertz: 50, Sensors: MotionSensors.UserAcceleration | MotionSensors.Gyroscope));
  sealed class MotionService
    void OnBatch(Action<MotionBatch> handler)
    bool ReceiveMotionBatch(string samplesJson)
    void RemoveHandler(Action<MotionBatch> handler)
    // sessionId: The client session to stream from.
    // options: Rate, sensors, batching and whether to keep going in the background.
    // ct: Optional cancellation token.
    Task<bool> StartTrackingAsync(int sessionId, MotionOptions? options = null, CancellationToken ct = default)
    // sessionId: The client session to stop.
    // ct: Optional cancellation token.
    Task<bool> StopTrackingAsync(int sessionId, CancellationToken ct = default)
  class Navigation
    // Query string stripped; null outside a client scope or before any path is known. Tracked before the client's first frame renders, so route-dependent server UI can branch on it from the very first render — unlike state set from joined handlers, which run on a background task and can lose the race against the first frame.
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
  class NavigationPathChangedEventArgs : EventArgs
    // url: The URL the client navigated to, query string included
    // clientContext: The client that navigated
    ctor(string url, Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string Path { get; }
    string Url { get; }
    string UserId { get; }
  // Tapping it opens the app and routes to the action's LaunchUrl, or reports its Id to the app's notification-tap handler.
  sealed record NotificationAction
    // Id: Stable id reported to the app when this action is tapped.
    // Title: Button label.
    // LaunchUrl: Optional in-app path to open when this action is tapped.
    ctor(string Id, string Title, string? LaunchUrl = null)
    string Id { get; init; }
    string? LaunchUrl { get; init; }
    string Title { get; init; }
  sealed record NotificationContent
    // Title: Notification title. Required.
    // Body: Optional body text shown below the title.
    // IconUrl: Optional URL of an icon image shown with the notification.
    // Tag: Optional collapse key — a later notification with the same tag replaces an existing one instead of stacking.
    // LaunchUrl: Optional in-app path the client navigates to when the user taps the notification.
    // Data: Optional opaque JSON payload the app receives back when the user taps the notification.
    ctor(string Title, string? Body = null, string? IconUrl = null, string? Tag = null, string? LaunchUrl = null, string? Data = null, NotificationPriority Priority = Normal, IReadOnlyList<NotificationAction>? Actions = null)
    IReadOnlyList<NotificationAction>? Actions { get; init; }
    string? Body { get; init; }
    string? Data { get; init; }
    string? IconUrl { get; init; }
    string? LaunchUrl { get; init; }
    NotificationPriority Priority { get; init; }
    string? Tag { get; init; }
    string Title { get; init; }
  // Declare it as a field of the app so it is constructed with the other persisted state, and register the channels the app can address:
  // private readonly NotificationInbox _inbox = new(app);
  //
  // _inbox.Channels.Add(new EmailNotificationChannel(app.Email, userId => _profiles.ValueFor(userId).Email));
  // _inbox.Channels.Add(new SmsNotificationChannel(app.Telephony, userId => _profiles.ValueFor(userId).Phone));
  //
  // await _inbox.NotifyAsync(order.CustomerUserId,
  //     new NotificationContent("Order delivered", "Enjoy your meal", LaunchUrl: $"/orders/{order.Id}", Tag: order.Id),
  //     kind: "order", route: NotificationRoute.Everywhere("email"));
  // Inside a UI lambda or handler Items and MarkRead act on the signed-in user; from a background task use the …For(userId) forms. A user mutes a channel with Mute; push is the channel named "push".
  sealed class NotificationInbox
    // app: The app; its Notifications service delivers the push side.
    // key: Storage key of the inbox list. Change it only to keep two inboxes apart.
    ctor(IAppBase app, string key = "ikon.notifications.inbox")
    // push: Null makes an inbox-only instance with no device push.
    ctor(NotificationService? push, string key = "ikon.notifications.inbox")
    List<INotificationChannel> Channels { get; }
    // Newest first. A tracked read — a UI lambda re-renders when it changes.
    IReadOnlyList<InboxItem> Items { get; }
    // Oldest items are dropped once a user's inbox grows past this; 200 by default.
    int MaxItems { get; init; }
    // 0 (the default) disables the cap. High-priority notifications ignore it, and the excess is still recorded in the inbox — only the device buzz is dropped.
    int MaxPushPerWindow { get; init; }
    // A tracked read.
    IReadOnlyList<string> Muted { get; }
    // Ten minutes by default.
    TimeSpan PushWindow { get; init; }
    // A tracked read.
    QuietHours? QuietHours { get; }
    // A tracked read.
    int UnreadCount { get; }
    void Clear()
    void ClearFor(string userId)
    void ClearQuietHours()
    void ClearQuietHoursFor(string userId)
    // A tracked read.
    bool IsMuted(string channel)
    IReadOnlyList<InboxItem> ItemsFor(string userId)
    void MarkAllRead()
    void MarkRead(string itemId)
    void MarkReadFor(string userId, string itemId)
    void Mute(string channel, bool muted = true)
    void MuteFor(string userId, string channel, bool muted = true)
    // userId: The user to notify.
    // content: Title, body, launch url, tag and data, as for NotificationService.
    // kind: App-defined category stored on the item for filtering.
    // route: Where to deliver; NotificationRoute.Default is inbox plus push.
    // ct: Optional cancellation token.
    Task<NotificationOutcome> NotifyAsync(string userId, NotificationContent content, string? kind = null, NotificationRoute? route = null, CancellationToken ct = default)
    QuietHours? QuietHoursFor(string userId)
    void Remove(string itemId)
    void SetQuietHours(TimeOnly startUtc, TimeOnly endUtc)
    void SetQuietHoursFor(string userId, TimeOnly startUtc, TimeOnly endUtc)
    int UnreadCountFor(string userId)
    const string PushChannel
  sealed record NotificationOutcome
    // Item: The inbox item, or null when the route skipped the inbox.
    // PushResults: Per-session push outcomes; empty when the user was offline or push was off.
    // Delivered: Names of the extra channels that sent ("email", "sms", …).
    // Skipped: Channels that had no address for the user, were unconfigured, or are muted by the user.
    // Failed: Channels that threw; the error is logged, the notification still stands in the inbox.
    ctor(InboxItem? Item, IReadOnlyList<NotificationSendResult> PushResults, IReadOnlyList<string> Delivered, IReadOnlyList<string> Skipped, IReadOnlyList<string> Failed)
    IReadOnlyList<string> Delivered { get; init; }
    IReadOnlyList<string> Failed { get; init; }
    InboxItem? Item { get; init; }
    IReadOnlyList<NotificationSendResult> PushResults { get; init; }
    IReadOnlyList<string> Skipped { get; init; }
  enum NotificationPermission
    Default
    Granted
    Denied
    Unsupported
  enum NotificationPriority
    // Ambient: recorded in the inbox, no device push or channel send.
    Low
    // Default: push and channels, subject to quiet hours and frequency caps.
    Normal
    // Urgent: bypasses quiet hours and frequency caps (an explicit mute still wins).
    High
  enum NotificationReach
    // Offline push is used solely when no session is connected — a user reading the app on a laptop does not also get a buzz on their phone.
    ConnectedFirst
    // Connected sessions get the foreground notification and the offline push hub delivers to each registered device as well. Set NotificationContent.Tag so a device that is connected collapses its foreground and push copies into one.
    AllDevices
  sealed record NotificationRoute
    // Inbox: Record the item in the user's in-app inbox.
    // Push: Show it on the user's devices through app.Notifications — web push on browsers, OS notifications on iOS and Android from the Flutter app.
    // Reach: Whether push stops at the connected devices or reaches every registered one.
    // Channels: Names of the extra channels to deliver on; each must be registered in NotificationInbox.Channels. Unknown names are skipped with a warning.
    ctor(bool Inbox = true, bool Push = true, NotificationReach Reach = ConnectedFirst, IReadOnlyList<string>? Channels = null)
    IReadOnlyList<string>? Channels { get; init; }
    bool Inbox { get; init; }
    bool Push { get; init; }
    NotificationReach Reach { get; init; }
    static NotificationRoute Everywhere(params string[] channels)
    NotificationRoute With(params string[] channels)
    static readonly NotificationRoute AllDevices
    static readonly NotificationRoute Default
    static readonly NotificationRoute Silent
  sealed record NotificationSendResult
    // SessionId: The target client session id.
    // Delivered: True when the client actually displayed the notification (permission granted).
    // Permission: The client's resulting permission state after the send attempt.
    ctor(int SessionId, bool Delivered, NotificationPermission Permission)
    bool Delivered { get; init; }
    NotificationPermission Permission { get; init; }
    int SessionId { get; init; }
  // Accessed via app.Notifications. Client permission is requested lazily on the first actual send, not when the app opens. SendToUserAsync automatically falls back to offline OS push (Web Push / FCM) when the target user has no connected session.
  sealed class NotificationService
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    // sessionId: The target client session id.
    // ct: Optional cancellation token.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    // sessionId: The target client session id.
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // Returns one result per connected session for the user. An empty list means the user had no connected session and only offline push was attempted — it is not an error.
    // userId: The persistent user id to notify.
    // content: The notification content.
    // ct: Optional cancellation token.
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = default)
    // userId: The persistent user id to notify.
    // content: The notification content. Give it a NotificationContent.Tag so a device that is both connected and pushed shows one notification, not two.
    // reach: How many of the user's devices to reach.
    // ct: Optional cancellation token.
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, NotificationReach reach, CancellationToken ct = default)
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
  // Partitioned at runtime by UserScope: each user sees their own value across all of their client sessions.
  class PersistentUserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    ctor(Func<string, T> initialValue, PersistenceBackend backend = Default, string? postgresDatabase = null, string? key = null)
    PersistenceBackend Backend { get; }
    string? PostgresDatabase { get; }
    string? PublicUrl { get; }
    // The in-memory value is dropped (the next read sees the initial value) and the persisted copy is deleted from every store it routes to, so it cannot resurrect on a later load. The deletion runs in the background; the user is excluded from the shutdown save immediately.
    void ClearFor(string userId)
    // The background-task form of Value = x: capture the id while the user scope is still active (var userId = ReactiveScope.UserId;), then write to it from anywhere.
    void SetFor(string userId, T value)
    // An atomic read-modify-write under that user's lock.
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
  // Within it, Normal and Low notifications are recorded in the inbox but not pushed to devices (High priority ignores it). The window may wrap past midnight (e.g. 21:00 → 06:00); convert from the user's local time before setting it.
  sealed record QuietHours
    // StartUtc: Inclusive start of the quiet window, as a UTC time of day.
    // EndUtc: Exclusive end of the quiet window, as a UTC time of day.
    ctor(TimeOnly StartUtc, TimeOnly EndUtc)
    TimeOnly EndUtc { get; init; }
    TimeOnly StartUtc { get; init; }
    bool Contains(TimeOnly utcTimeOfDay)
  // Raw on purpose. The app's own recorder is the processor — smoothing, auto-pause, elevation — and re-running it over a complete set of fixes gives a better track than one assembled live from whatever the network happened to deliver. Storing the processed result instead would bake in the gaps this archive exists to remove.
  readonly record struct RecordedFix
    ctor(double AtMillis, double Latitude, double Longitude, double AccuracyMeters, double SpeedMps, double Heading, double AltitudeMeters)
    double AccuracyMeters { get; init; }
    double AltitudeMeters { get; init; }
    double AtMillis { get; init; }
    double Heading { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
    double SpeedMps { get; init; }
  sealed record RecordingArchive
    // ArchiveId: The activity this archive belongs to, as the app named it.
    // SessionId: The client session that uploaded it.
    // UserId: The signed-in user, or empty.
    // StartedAt: When the device opened the archive (UTC).
    // Fixes: In the order the device recorded them.
    // Motion: In the order the device recorded them.
    // Asset: Where the raw bytes are stored. Keep it if the recording itself is worth keeping — a corpus to train on, or a re-analysis a later build will want to run.
    ctor(string ArchiveId, int SessionId, string UserId, DateTime StartedAt, IReadOnlyList<RecordedFix> Fixes, IReadOnlyList<MotionSample> Motion, AssetUri Asset)
    string ArchiveId { get; init; }
    AssetUri Asset { get; init; }
    IReadOnlyList<RecordedFix> Fixes { get; init; }
    IReadOnlyList<MotionSample> Motion { get; init; }
    int SessionId { get; init; }
    DateTime StartedAt { get; init; }
    string UserId { get; init; }
  // Little-endian throughout. File header, 24 bytes: magic IKAR (4), version u16, reserved u16, startedUnixMs i64, baseAtMs f64. Then records, each opening with kind u8 and offsetMs u32 measured from baseAtMs: a fix carries latitude f64, longitude f64, accuracy f32, speed f32, heading f32, altitude f32 (37 bytes in total); a motion sample carries sensor u8, x f32, y f32, z f32 (18 bytes). Offsets are relative to a base rather than absolute because a millisecond epoch is around 1.7e12, which single precision resolves no better than about 130 ms — coarser than the gap between samples, so absolute float timestamps would destroy every rhythm in the file.
  static class RecordingArchiveCodec
    // throws InvalidDataException: The header is missing or from a newer format.
    static (DateTime StartedAt, List<RecordedFix> Fixes, List<MotionSample> Motion) Decode(ReadOnlySpan<byte> archive)
    static byte[] EncodeFix(RecordedFix value, double baseAtMillis)
    static byte[] EncodeHeader(DateTime startedAt, double baseAtMillis)
    static byte[] EncodeMotion(MotionSample value, double baseAtMillis)
    const int FixBytes = 37
    const int HeaderBytes = 24
    const int MotionBytes = 18
  // It pairs with the live stream rather than replacing it: the live stream drives the screen and may be decimated and gappy, the archive arrives at the end and repairs the record. Keep the server-side recording as it is and let the archive correct it, so that a failed upload or a client too old to record degrades to the live track rather than to nothing. The device keeps each file until the server acknowledges it, so a failed upload is retried on the next connection, and deletes it after.
  // app.Recordings.OnArchive(archive => Repair(archive.Fixes));
  // await app.Recordings.StartAsync(sessionId, activityId);
  sealed class RecordingArchiveService
    void OnArchive(Action<RecordingArchive> handler)
    void RemoveHandler(Action<RecordingArchive> handler)
    // sessionId: The client session to ask.
    // ct: Optional cancellation token.
    Task<bool> RequestPendingAsync(int sessionId, CancellationToken ct = default)
    // sessionId: The client session that should record.
    // archiveId: Names the activity. The same id must be given to StopAsync, and it is what arrives back on RecordingArchive.ArchiveId. One id is one file, so starting and stopping repeatedly produces one archive per activity and never a blend of two.
    // options: What to record.
    // ct: Optional cancellation token.
    Task<bool> StartAsync(int sessionId, string archiveId, RecordingOptions? options = null, CancellationToken ct = default)
    // sessionId: The client session that was recording.
    // archiveId: The id given to StartAsync.
    // ct: Optional cancellation token.
    Task<bool> StopAsync(int sessionId, string archiveId, CancellationToken ct = default)
    const string UploadActionId
  sealed record RecordingOptions
    // Fixes: Record position fixes. Almost always yes — this is what survives an outage.
    // Motion: Record motion samples at the full rate asked of MotionService, independently of the decimated rate being streamed live.
    // MaxBytes: Refuse to grow the file past this. A device with no space left must fail the recording rather than the phone.
    ctor(bool Fixes = true, bool Motion = true, long MaxBytes = 268435456)
    bool Fixes { get; init; }
    long MaxBytes { get; init; }
    bool Motion { get; init; }
  enum RecordingRecordKind
    Fix
    Motion
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
    int MaxClientsPerShard { get; }
    // Cost ceiling on the shard family size; 0 (the default) means unlimited. When every allowed shard is at capacity, new connections still join the last shard over capacity — visitors are never turned away by sharding
    int MaxShards { get; set; }
  // The text is the title, then the body on the next line.
  sealed class SmsNotificationChannel : INotificationChannel
    // telephony: The app's telephony service.
    // phoneOf: Returns the user's E.164 phone number, or null when none is known.
    ctor(TelephonyService telephony, Func<string, string?> phoneOf)
    string Name { get; }
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  sealed class SpeechNotRecognizedEventArgs : EventArgs
    ctor(SpeechNotRecognizedReason reason, Context clientContext, string streamId, string? correlationId, Exception? error = null)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    // The failure when Reason is SpeechNotRecognizedReason.Error; otherwise null.
    Exception? Error { get; }
    SpeechNotRecognizedReason Reason { get; }
    string StreamId { get; }
    string UserId { get; }
  enum SpeechNotRecognizedReason
    NoAudio
    Silence
    NoText
    Error
  sealed class SpeechRecognizedEventArgs : EventArgs
    ctor(string text, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount, int turnId = 0)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    TimeSpan Duration { get; }
    int SampleCount { get; }
    string StreamId { get; }
    string Text { get; }
    // Identifier of the detected turn when the recognition came from Audio.UseTurnDetection, shared with the matching TurnStartedEventArgs and TurnSpeculativeEventArgs; 0 for push-to-talk recognitions.
    int TurnId { get; }
    string UserId { get; }
  class StartingEventArgs : EventArgs
    ctor()
  class StoppingEventArgs : EventArgs
    ctor()
  sealed class TelegramNotificationChannel : INotificationChannel
    // botToken: Bot token from @BotFather; empty disables the channel.
    // chatIdOf: Returns the user's Telegram chat id, or null when none is known.
    ctor(string botToken, Func<string, string?> chatIdOf)
    string Name { get; }
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  // Platform telephony surface for an Ikon app: sending SMS and placing phone calls from a number the platform holds for the app's space. Accessed via app.Telephony. The space needs a number first (ikon app telephony create --country se); until then every operation throws TelephonyNumberNotAvailableException, which names that command. A space may hold several numbers, in different markets and on different providers — omit from and the platform picks one, or name one to send as it. Sending is metered, so a space out of credits is suspended like any other overspend.
  sealed class TelephonyService
    // Routes incoming messages and calls to this app instance, so a reply reaches the person waiting for it rather than whichever instance an empty identity resolves to. The binding outlives this process: it pins an identity, not an instance, so if this one is reaped the next message provisions a fresh instance with the same identity rather than being lost. That is what makes an app wake up when someone texts it. Running locally is the exception. There the binding also carries this machine's instance id, which is minted fresh on every run and cannot outlive it — so a local binding is reverted automatically when the app shuts down, rather than leaving the number pointed at a dead process. It applies to every number the space holds: one number cannot serve two identities, so an app wanting inbound per user needs a number per user.
    Task BindInboundToThisInstanceAsync(CancellationToken ct = default)
    // The same IVoiceCall an incoming call gives, so a conversation reads the same whichever end started it:
    // await using var call = await app.Telephony.CallAsync("+358401234567");
    // await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new("Your build finished")));
    // Returns only once the call is connected and audio can flow; throws if nobody answers before ringTimeout. Dispose it — or call IVoiceCall.HangUpAsync — to end the call. It counts against the space's concurrent-call limit, carries the platform duration cap, and is refused for a disallowed destination.
    // from: Which of the app's numbers to call from. Omit to let the platform choose: the app's default number if it has one, else a number local to the destination's market, else the first it holds. Naming a number the app does not hold is refused rather than substituted.
    Task<IVoiceCall> CallAsync(string to, TimeSpan? ringTimeout = null, string? from = null, CancellationToken ct = default)
    // Every number the app holds, across every provider serving it. Worth reading when the app wants to choose a sender itself rather than let the platform pick one — to answer as the same number a user last saw, say. Most apps never need it: omitting from already sends from a number local to the recipient.
    Task<IReadOnlyList<TelephonyNumber>> GetNumbersAsync(CancellationToken ct = default)
    Task<TelephonyStatus> GetStatusAsync(CancellationToken ct = default)
    // Answers incoming calls with handler. Call it once at startup, and the space's phone number rings this app. The caller's audio reaches the handler as it is spoken and the app can speak back over the same call; see IVoiceCall for the conversational loop. Nothing else has to be configured. Calling this tells the platform that this app answers calls, which is when the provider side is wired up — so an app can start answering the phone without anyone touching a number, and a call that arrives while the app is not running starts it, exactly as an incoming message does.
    Task HandleCallsAsync(Func<IVoiceCall, Task> handler, CancellationToken ct = default)
    // Undoes BindInboundToThisInstanceAsync.
    Task ResetInboundAsync(CancellationToken ct = default)
    // Sends an SMS to the given number, which must be in E.164 form (+ followed by country code and number, for example +358401234567). Check SmsSendResult.Replyable on the result: when it is false the recipient received the message but cannot answer it, because the space holds no number local to their market and a foreign sender is stripped in transit. Long messages are split into billable segments; SmsSendResult.Parts reports how many were charged.
    // from: Which of the app's numbers to send as. Omit to let the platform choose: the app's default number if it has one, else a number local to the recipient's market — which is what keeps a message replyable — else the first it holds. Naming a number the app does not hold is refused rather than substituted, since sending as a different number reaches the recipient as a stranger.
    Task<SmsSendResult> SendSmsAsync(string to, string text, string? from = null, CancellationToken ct = default)
    // Raised for each message one of the space's numbers receives. The app declares no webhook: the platform owns the endpoint the provider posts to and delivers the message here, so a message reaches whichever instance inbound is bound to — starting one if none is running. Reply by calling SendSmsAsync with SmsMessage.From. There is deliberately no "return a string to reply" shortcut: a reply the provider sends on our behalf is billed inside the provider, where nothing can meter it or refuse it for a space out of credit.
    event Func<SmsMessage, Task>? SmsReceived
  enum Theme
    Dark
    Light
  static class ThemeExtensions
    // False for the light theme, custom theme names, and clients that have not reported a theme.
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
  // Return an AssetUri from onStart and the bytes stream straight into asset storage without ever being held in the app — which is what a large file needs, since an app container has far less memory than the files people send it.
  // app.Uploads.Register("my-app.telemetry",
  //     onStart: args => Task.FromResult(new FileUploadResult
  //     {
  //         AssetUri = new AssetUri(AssetClass.CloudFile, $"telemetry/{args.FileName}", app.GlobalState.SpaceId),
  //     }),
  //     onComplete: async args =>
  //     {
  //         if (args.AssetUri is { } uri) { await ProcessAsync(uri); }
  //     });
  sealed class UploadService
    // uploadActionId: The id clients tag their upload with. Namespace it — the ids rendered view.FileUpload components generate live in the same table.
    // onStart: Decides where the bytes go, and whether to accept at all. Return a FileUploadResult carrying an AssetUri to stream into asset storage, or one that is not accepted to refuse.
    // onComplete: Runs once every byte has landed.
    // onError: Runs when a transfer fails partway.
    void Register(string uploadActionId, Func<FileUploadStartArgs, Task<FileUploadResult>> onStart, Func<FileUploadCompleteArgs, Task>? onComplete = null, Func<FileUploadErrorArgs, Task>? onError = null)
  class UserDataErasureEventArgs : EventArgs
    ctor(string userId)
    string UserId { get; }
  enum UserRole
    // Maps to the "anonymous" role string, not "guest"
    Guest
    User
    Moderator
    Admin
  class Video
    ctor(IAppBase app)
    ValueTask CloseAllAsync()
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
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
    event AsyncEventHandler<VideoInputStreamBeginEventArgs> VideoInputStreamBeginAsync
    event AsyncEventHandler<VideoInputStreamEndEventArgs> VideoInputStreamEndAsync
  class VideoInputFrameEventArgs : EventArgs
    ctor(string streamId, Context clientContext, int trackId, byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Inherited from the originating VideoStreamBegin (set by a CaptureButton); null for ad-hoc streams.
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
    // Set by the originating CaptureButton; null for ad-hoc streams.
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
    // Inherited from the originating VideoStreamBegin (set by a CaptureButton); null for ad-hoc streams.
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
  // Free-form text reaches a user only inside the 24-hour customer-service window; outside it the API requires an approved template, so pass templateName to send the same notification text as the template's single body parameter instead.
  sealed class WhatsAppNotificationChannel : INotificationChannel
    // accessToken: Cloud API access token; empty disables the channel.
    // phoneNumberId: The business phone number id the message is sent from.
    // phoneOf: Returns the user's phone number in international format, or null.
    // templateName: Optional approved template with one body parameter.
    // templateLanguage: Template language code, "en" by default.
    ctor(string accessToken, string phoneNumberId, Func<string, string?> phoneOf, string? templateName = null, string templateLanguage = "en")
    string Name { get; }
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)

namespace Ikon.App.Cells
  // A cell is always shared by its SessionIdentity: every caller that Cells.Connects with the same identity reaches the same instance and its Reactive<T> state — the identity IS the sharing scope (parameterless = one global; keyed = one per key). The runtime picks the transport: a local run hosts every cell in-process (a direct object); in the cloud the cell lives in its own cell-host and callers reach it through a proxy ([HttpGet]/[HttpPost] over HTTP, [Function] methods and Reactive<T> members over an SDK connection). App authors never choose or think about placement — they declare [Cell] and a SessionIdentity, and get exactly what those mean.
  sealed class CellAttribute : Attribute
    ctor()
    // Defaults to 1 (per-key singleton). Values greater than 1 spawn that many instances and round-robin CellHost.Resolve<TInterface> across them: globals (parameterless SessionIdentity) eager-spawn at host construction, keyed cells spawn together on first access. Sharded keyed cells must tolerate eventual consistency between shards — hold no per-instance state, or persist shared state externally.
    int Capacity { get; init; }
    // Zero (the default) means no eviction — the instance lives until the host shuts down. Globals (cells whose SessionIdentity is parameterless) are never evicted regardless of this value.
    int IdleTtlSeconds { get; init; }
  // Each in-process server runs in its own async-local scope, so Cells.Instance resolves to that server's own host and wiring. The framework calls Initialize once at startup; apps call Connect<TInterface> for each cell access.
  class Cells : AsyncLocalInstance<Cells>
    ctor()
    // On a CLOUD run, when TInterface is an interface backed by a [Cell] type, returns a SubstrateCellProxy<TInterface> that dispatches per member: [HttpGet]/[HttpPost] methods over stateless HTTP, [Function] methods and Reactive<T> members over a standard SDK connection to the cell-host. Otherwise — a concrete-type request, or ANY cell on a LOCAL run — returns the local cell instance from this server's CellHost. Local runs host every cell in-process (there is no deployed cell-host to proxy to, and a local run is a single process), so every cell behaves as a normal shared instance locally.
    TInterface Connect<TInterface>(object sessionIdentity) where TInterface : class
    ValueTask DisposeAsync()
    const string CellTypeParam
  // Injected into a cell's primary constructor by the framework.
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
  // Omit Interval for a one-time offer.
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
  // OfferId is null for ad-hoc charges and records written before offer tracking.
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
  // Url is a provider-hosted receipt page. Pdf holds downloadable PDF bytes only when the provider exposes one; today every provider returns a hosted URL only, so Pdf is null.
  sealed record PaymentReceipt
    ctor(string? Url, byte[]? Pdf, string? PdfContentType)
    byte[]? Pdf { get; init; }
    string? PdfContentType { get; init; }
    string? Url { get; init; }
  // Enqueued counts the provider objects queued for re-processing; their effects arrive asynchronously as normal payment events.
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
    // customerKey defaults to the current user.
    Task<IReadOnlyList<Payment>> ListPaymentsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // customerKey defaults to the current user.
    Task<IReadOnlyList<PaymentSubscription>> ListSubscriptionsAsync(string? customerKey = null, CancellationToken cancellationToken = default)
    // Recovery path for a missed provider webhook or an app that was offline. Eventually consistent: pulled objects surface as ordinary PaymentEventReceived pushes and entitlement refreshes. A reference (a payment link's checkout-session reference or a subscription id) scopes the pull to one object; otherwise the customer's recent objects, or the space's recent window when no customer is in scope.
    Task<PaymentReconcileResult> ReconcileAsync(string? customerKey = null, string? reference = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Refunds in full by default, or partially via amountMinor. A refund does NOT revoke an entitlement the original payment granted.
    Task<PaymentRefund> RefundAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Returns false if no such active offer existed.
    Task<bool> RemoveOfferAsync(string offerId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    Task<PaymentReceipt> RequestReceiptAsync(string paymentId, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Valid only while the subscription is cancel-at-period-end and its paid period has not ended; an immediate cancel or a fully-ended subscription needs a new checkout. Returns a SubscriptionResume whose SubscriptionResume.SubscriptionId may differ from the input when the provider recreated the subscription (Mollie).
    Task<SubscriptionResume> ResumeSubscriptionAsync(string subscriptionId, string? idempotencyKey = null, PaymentProvider? provider = null, CancellationToken cancellationToken = default)
    // Subscribing registers the receiver on first use.
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
  // Changed is false when the subscription was already on the requested offer (a no-op). On an upgrade ProrationAmountMinor was charged immediately and the new plan is active now; on a downgrade nothing is charged and the new plan takes over at the next renewal (Effective is "immediate" or "next_cycle").
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


---

# Ikon.Common Public API

namespace Ikon.Common
  class AsyncLocalInstances
    bool AsyncLocalModeInitialized { get; }
    void Capture(object owner, bool allowOverride = false)
    void InitializeAll()
    void InitializeAll(IReadOnlyList<Type> explicitTypes)
    void Remove(object owner)
    void Restore(object owner)
    bool TryRemove(object owner)
    bool TryRestore(object owner)
    static readonly AsyncLocalInstances Instance
  // Read-only configuration handed to the app at startup and exposed through IAppBase.Databases: look a database up by Name or Type and open it (see IAppBase.Database or AppDatabaseConnection.Create). An app never constructs one — databases are created with ikon app db create (or the Portal) and provisioned by the backend.
  sealed record DatabaseConnectionInfo
    ctor()
    // Ready-to-use ADO.NET connection string, pointing at the app's own database through the connection pooler. It carries credentials — never log it or surface it to a client.
    string ConnectionString { get; init; }
    // The lookup key when an app has more than one database, as given to ikon app db create --name.
    string Name { get; init; }
    // "postgres" is the only engine the platform provisions today, and AppDatabaseConnection.Create throws NotSupportedException for anything else. Match on it rather than assuming.
    string Type { get; init; }
  // Derives from DescriptionAttribute so that every reader of the BCL attribute — Tool.Of lambda parameters, function registration — also picks this one up, and an app that has global using Ikon.Common; can write [Description] anywhere the BCL one is accepted. Adding using System.ComponentModel; next to it makes the bare name ambiguous (CS0104); qualify one of them.
  class DescriptionAttribute : DescriptionAttribute
    ctor(string description, object? example = null, RequiredStatus isRequired = Default, int minArrayItems = 0)
    object? Example { get; }
    // Not honoured by any schema generator: whether a property is required is derived from its nullability, and the OpenAI dialect lists every property as required regardless. Kept for source compatibility.
    RequiredStatus IsRequired { get; }
    int MinArrayItems { get; }
  enum EndpointProtocol
    Tcp
    Tls
    Udp
  sealed class IkonLoggerProvider : ILoggerProvider
    ctor()
    ILogger CreateLogger(string categoryName)
    void Dispose()
  static class IkonTaskExtensions
    // Intentionally does not await the task. Exceptions are observed and sent to onException.
    static void RunParallel(this Task task, Action<Exception>? onException = null)
  // Used wherever a caller supplies a destination the platform then reaches on their behalf — a TURN peer, a URL handed to an AI tool, a scraped page. Those all share one failure mode: the address is chosen by someone outside, but the connection is made from inside, so anything the host can see becomes reachable. That includes sibling containers, admin ports on the host, and on a cloud VM the metadata service on 169.254.169.254. Deliberately one implementation. Two copies of a rule like this drift, and the copy nobody remembers is the one still reachable.
  static class InternalAddressFilter
    // True when the address is a public, routable destination that is safe to reach on a caller's behalf. False for anything inside the host's own network, and for anything unrecognised — this fails closed.
    static bool IsPublicRoutable(IPAddress? address)
  static class MimeTypes
    // Registers a mime type for a file extension. The extension is normalized (leading dot stripped, lower-cased) so it matches what the lookups use, and the write is locked against the concurrent readers. Argument order is (extension, mimeType), matching the rest of the type.
    static void AddOrUpdate(string extension, string mimeType)
    // Returns the file extension registered for a mime type. When several extensions map to the same mime type, the first one in registration (insertion) order is returned. When no extension matches, the default extension (DefaultExtension, "bin") is returned.
    static string GetExtensionFromMimeType(string mimeType)
    static string GetMimeTypeFromExtension(string extension)
    static string GetMimeTypeFromFilename(string fileName)
    static bool Is(string mimeType, string mimeTypeToCompare)
    static bool IsAudio(string mimeType)
    // The negation of IsText: everything that is not text/* or ending in /json or /xml counts as binary — images, audio, video, and unknown or empty types included. Broader than application/octet-stream and does not imply that specific mime type.
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
    // Returns true when the mime type is textual: any text/* type, or one ending in /json or /xml. Everything else (images, audio, video, unknown types) is not text.
    static bool IsText(string mimeType)
    static bool IsVideo(string mimeType)
    static bool IsXml(string mimeType)
    static bool IsZip(string mimeType)
    // type: The category keyword — not a mime string. Recognized keywords are: text, markdown, video, image, audio, json, binary, csv, zip, xml, pdf, word, excel, powerpoint, notes, and any. "any" always returns true; an unrecognized keyword returns false.
    // mimeType: The mime type to classify.
    static bool TypeMatchesMimetype(string type, string mimeType)
    const string ApplicationExcel
    const string ApplicationJavascript
    const string ApplicationJson
    const string ApplicationMsword
    const string ApplicationOctetStream
    const string ApplicationPdf
    const string ApplicationSql
    const string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlPresentation
    const string ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet
    const string ApplicationVndOpenxmlformatsOfficedocumentWordprocessingmlDocument
    const string ApplicationXml
    const string ApplicationZip
    const string AudioMpeg
    const string AudioXWav
    const string Binary
    const string DefaultExtension
    const string DefaultMimeType
    const string ImageAvif
    const string ImageBmp
    const string ImageGif
    const string ImageHeif
    const string ImageJpeg
    const string ImagePng
    const string ImageSvg
    const string ImageSvgXml
    const string ImageTiff
    const string ImageWebp
    const string TextCss
    const string TextCsv
    const string TextHtml
    const string TextJavascript
    const string TextMarkdown
    const string TextPlain
    const string TextXml
    const string VideoMp4
  static class NetworkUtils
    // A candidate address for another host on the LAN to reach this machine at. Only operational (OperationalStatus.Up), non-loopback, non-tunnel interfaces are considered, and loopback (127.x) and APIPA link-local (169.254.x) addresses are skipped. Returns IPAddress.Loopback only when no such address exists.
    static IPAddress GetFirstIPv4AddressOrLocalhost()
  // Thrown when a declared package hook command exits non-zero.
  sealed class PackageHookException : Exception
    ctor(string command, string output)
    string Command { get; }
  // Runs an app's declared packaging pipeline commands during bundling by shelling out to whatever the app declared, exposing the bundle staging directory via IKON_BUNDLE_DIR so a script can write processed/compiled output there to include it in the package.
  static class PackageHooks
    // Runs each command in order in appDir, with IKON_APP_DIR and IKON_BUNDLE_DIR (plus any extraEnv) in the environment. Throws PackageHookException on the first command that fails. onCommandStart is invoked before each command (for progress reporting).
    static Task RunAsync(IReadOnlyList<string> commands, string appDir, string bundleDir, IReadOnlyDictionary<string, string?>? extraEnv = null, Action<string>? onCommandStart = null, CancellationToken ct = default)
  enum PipelineExecutionMode
    None
    HttpsEndpoint
    Scheduled
  class RateLimiter
    ctor(TimeSpan window, int rateLimit)
    // A side-effect-free read: it counts the calls still inside the window without mutating any state, so inspecting it (in a debugger or a metric) never prunes entries or changes what Guard will do. Expired entries are dropped on the Guard path, so the value still decays as the window drains.
    int Rate { get; }
    // Returns true when the call is admitted. A rejected call does not consume a window slot, so a caller retrying after a rejection recovers as soon as the window drains instead of pushing the limiter further over its limit with every attempt.
    bool Guard()
  // Exposes the locally bound port and the publicly reachable host/port. Dispose to release the endpoint and its local port reservation.
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
  // Across every overload, retries counts attempts beyond the first: the delegate runs once, then up to retries more times, for at most retries + 1 total invocations (e.g. retries = 5 allows up to 6 calls). When no retryableExceptions filter is supplied, only transient exceptions are retried: IOException, HttpRequestException and TimeoutException. Non-transient exceptions (bugs, validation failures) surface immediately instead of being retried. Pass an explicit filter to override this default — e.g. [typeof(Exception)] to retry every exception.
  static class Retrier
    static T Run<T>(Func<T> func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static void Run(Action func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task<T> RunAsync<T>(Func<Task<T>> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task RunAsync(Func<Task> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
  static class StringDistance
    // Returns the minimum number of single-character insertions, deletions, or substitutions to turn a into b. Empty / null inputs return the length of the other side. O(|a|·|b|) time and memory.
    static int Levenshtein(string? a, string? b)
  static class StringUtils
    // The returned string is hex-encoded, so it is 2×size characters long (the default of 32 bytes yields a 64-character string).
    // size: The number of random bytes to generate.
    static string GenerateRandomToken(int size = 32)
    static string GetCSharpTypeName(object? obj)
    static string ToUnescapedString(string input, bool unicodeOnly = false)

namespace Ikon.Common.Assets
  sealed record AssetGcOrphan
    ctor(string Uri)
    string Uri { get; init; }
  sealed record AssetGcPlan
    ctor(AssetGcScope Scope, IReadOnlyList<AssetGcOrphan> Orphans, int EverReferenced, int Kept)
    int EverReferenced { get; init; }
    int Kept { get; init; }
    IReadOnlyList<AssetGcOrphan> Orphans { get; init; }
    AssetGcScope Scope { get; init; }
  enum AssetGcScope
    // Keep every asset any reachable commit references — deletes nothing, so binary undo/redo across the whole history stays intact. The safe default.
    History
    // Keep the working tree plus assets referenced by commits within a recent day window; older historical versions are reclaimed (checking out a commit past the window may lack those binaries).
    Window
    // Keep only what the current working tree references; every historical version is reclaimed (most aggressive — older checkouts lose their binaries).
    Current
  // The binary-in-git scheme: a binary foobar.jpg is tracked in git as a text pointer foobar.jpg.ikonasset while the bytes live in an IAssetBackend; the real file is materialized on demand and git-ignored. NormalizeAsync enforces the convention (and self-heals a raw binary anyone committed); MaterializeAsync restores the real files; CollectReferencedUrisAsync feeds reachability GC.
  sealed class AssetLinkManager
    ctor(IAssetBackend backend)
    // Every asset URI reachable from the tree's pointers — the "live set" a reachability GC keeps; the backend expires blobs whose URI is not in this set.
    Task<IReadOnlySet<string>> CollectReferencedUrisAsync(string repoDir, CancellationToken ct = default)
    // Deletes the orphaned assets a PlanGcAsync plan found. Best-effort per asset (an already-deleted URI on a re-run counts as a failure, not a crash); returns deleted/failed counts.
    Task<(int Deleted, int Failed)> ExecuteGcAsync(AssetGcPlan plan, CancellationToken ct = default)
    // Produces the real binary next to every *.ikonasset pointer by downloading its bytes, and git-ignores it. Idempotent and hash-checked — a materialized file whose content already matches the pointer's hash is left untouched. Returns the real paths written.
    Task<IReadOnlyList<string>> MaterializeAsync(string repoDir, CancellationToken ct = default)
    // Converts every raw binary in the tree to the pointer scheme: upload the bytes, write the *.ikonasset pointer, git-ignore the real path, and untrack the real file if git was carrying it. The real file itself stays on disk — normalize only stops git from carrying the bytes, it never takes files away from a working tree. Idempotent — a file that is already a pointer, or already pointered with an unchanged hash, is skipped. Returns the real paths converted.
    Task<IReadOnlyList<string>> NormalizeAsync(string repoDir, CancellationToken ct = default)
    // Plans a reachability GC without touching the store: collect every asset URI any *.ikonasset pointer ever recorded across git history, subtract the set still referenced under scope, and return the difference as orphans, each deletable by its URI. AssetGcScope.History keeps everything (empty plan); Window/Current reclaim older versions.
    Task<AssetGcPlan> PlanGcAsync(string repoDir, AssetGcScope scope, int windowDays = 30, CancellationToken ct = default)
    // Moves a tree's offloaded binaries from one store to another: materialize every pointer from source, drop the pointers, and normalize under target so they reference blobs the new home owns.
    static Task RehomeAsync(IAssetBackend source, IAssetBackend target, string repoDir, CancellationToken ct = default)
    // Recomputes the managed .gitignore block from the pointers currently in the tree — for callers that move pointer files around (folder migration) without touching the store.
    static Task UpdateManagedGitignoreAsync(string repoDir, CancellationToken ct = default)
    // Files under root public/ are uploaded as public (frontend-loadable by stable URL); binaries anywhere else stay private, readable only by the app. The folder is the classification — there is nothing to configure.
    const string PublicFolderName
  // Thrown when AssetLinkManager.MaterializeAsync could not restore some assets (their blobs are missing — e.g. an old checkout after an aggressive GC). Everything that could be restored has been; Failures lists what could not, one path: reason per line.
  sealed class AssetMaterializeException : Exception
    ctor(IReadOnlyList<string> failures)
    IReadOnlyList<string> Failures { get; }
  // The content of a *.ikonasset pointer file — the small, versioned text git tracks in place of a binary. Checking out any commit restores that commit's pointers, so binary history/undo/redo works.
  sealed record AssetPointer
    ctor(string Uri, string Sha256, long Size, string Name, string? PublicUrl = null)
    string Name { get; init; }
    string? PublicUrl { get; init; }
    string Sha256 { get; init; }
    long Size { get; init; }
    string Uri { get; init; }
    static string PointerPathForReal(string realPath)
    static string RealPathForPointer(string pointerPath)
    string Serialize()
    static AssetPointer? TryParse(string text)
    // Marks a pointer file, appended after the real extension: foobar.jpg → foobar.jpg.ikonasset.
    const string Suffix
  static class BinaryContent
    // Content is binary when it is not valid UTF-8 or contains a NUL byte in its head — the same heuristic git itself uses to decide "binary". Empty content is text.
    static bool IsBinary(byte[] content)
    static string Sha256Hex(byte[] content)
    // The window IsBinary inspects — matching git's own binary heuristic, and the most a caller needs to read from a file to classify it.
    const int DetectionWindowBytes = 8000
  // A blob store binaries are offloaded to: git tracks a small text AssetPointer while the bytes live here, addressed by backend-agnostic string URIs.
  interface IAssetBackend
    // Best-effort; used by GC.
    Task DeleteAsync(string uri, CancellationToken ct = default)
    Task<byte[]> DownloadAsync(string uri, CancellationToken ct = default)
    // A stable public URL for a URI when the blob is publicly served; null for private/backend-only assets.
    Task<string?> GetPublicUrlAsync(string uri, CancellationToken ct = default)
    // Stores bytes and returns the URI the pointer records. isPublic selects a publicly-served class (a frontend can load it by URL) over a private, backend-only one.
    Task<string> UploadAsync(byte[] content, string fileName, bool isPublic, CancellationToken ct = default)
  // IAssetBackend over the Ikon Asset system. Public assets go to AssetClass.CloudFilePublic (a frontend can load them by URL); private assets to AssetClass.CloudFile (C#-readable only). Blobs are content-addressed by SHA-256, so identical bytes upload once, a pointer's URI is immutable (checking out an old commit fetches exactly that version), and reachability GC can safely delete any stored hash no live pointer references.
  sealed class IkonAssetBackend : IAssetBackend
    ctor(string spaceId)
    Task DeleteAsync(string uri, CancellationToken ct = default)
    Task<byte[]> DownloadAsync(string uri, CancellationToken ct = default)
    Task<string?> GetPublicUrlAsync(string uri, CancellationToken ct = default)
    Task<string> UploadAsync(byte[] content, string fileName, bool isPublic, CancellationToken ct = default)
  static class StorageExtensions
    static Task AddCloudFilePublicStorageAsync(this Asset asset)
    static Task AddCloudFileStorageAsync(this Asset asset, TimeSpan? uploadTimeout = null)
    static Task AddCloudJsonStorageAsync(this Asset asset)
    static Task AddLocalFileStorageAsync(this Asset asset, string root)

namespace Ikon.Common.Git
  record GitBranch
    ctor(string Name, bool IsRemote, bool IsCurrent)
    bool IsCurrent { get; init; }
    bool IsRemote { get; init; }
    string Name { get; init; }
  enum GitChangeType
    Added
    Modified
    Deleted
    Renamed
    Untracked
  record GitCloneOptions
    // Overwrite: When the clone target already exists and is non-empty, replace it instead of failing. Off by default so a populated directory is never destroyed silently.
    ctor(string? Branch = null, bool Shallow = false, GitCredentials? Credentials = null, bool Overwrite = false)
    string? Branch { get; init; }
    GitCredentials? Credentials { get; init; }
    bool Overwrite { get; init; }
    bool Shallow { get; init; }
  record GitCommit
    ctor(string Sha, string ShortSha, string Author, string AuthorEmail, DateTimeOffset Date, string Message)
    string Author { get; init; }
    string AuthorEmail { get; init; }
    DateTimeOffset Date { get; init; }
    string Message { get; init; }
    string Sha { get; init; }
    string ShortSha { get; init; }
  record GitCredentials
    ctor(string Username, string Password)
    string Password { get; init; }
    string Username { get; init; }
  record GitDiff
    ctor(string? FromSha, string? ToSha, IReadOnlyList<GitFileDiff> Files)
    IReadOnlyList<GitFileDiff> Files { get; init; }
    string? FromSha { get; init; }
    string? ToSha { get; init; }
  record GitFileChange
    ctor(string Path, GitChangeType Type)
    string Path { get; init; }
    GitChangeType Type { get; init; }
  record GitFileDiff
    ctor(string Path, GitChangeType Type, int LinesAdded, int LinesRemoved, string? Patch = null)
    int LinesAdded { get; init; }
    int LinesRemoved { get; init; }
    string? Patch { get; init; }
    string Path { get; init; }
    GitChangeType Type { get; init; }
  enum GitReconcileOutcome
    UpToDate
    Pushed
    Merged
    Conflicted
    NoRemote
    Detached
    Failed
  record GitReconcileResult
    ctor(GitReconcileOutcome Outcome, string Branch, IReadOnlyList<string> ConflictedFiles, string? Error = null)
    string Branch { get; init; }
    IReadOnlyList<string> ConflictedFiles { get; init; }
    string? Error { get; init; }
    GitReconcileOutcome Outcome { get; init; }
  class GitRepository
    ctor(string workingDirectory, GitCredentials? credentials = null)
    GitCredentials? Credentials { get; }
    string WorkingDirectory { get; }
    Task AbortAllInProgressOperationsAsync(CancellationToken ct = default)
    Task<bool> AbortCherryPickAsync(CancellationToken ct = default)
    Task<bool> AbortMergeAsync(CancellationToken ct = default)
    Task<bool> AbortRebaseAsync(CancellationToken ct = default)
    // Credentials are stripped from the URL before it is stored.
    Task AddRemoteAsync(string name, string url, CancellationToken ct = default)
    Task CheckoutAsync(string branchOrRef, CancellationToken ct = default)
    // HEAD is not changed.
    Task CheckoutFilesFromRefAsync(string refName, string path = ".", CancellationToken ct = default)
    // If targetDir already exists and is non-empty, the clone fails with an exception unless GitCloneOptions.Overwrite is set, in which case the existing directory is deleted recursively first — an unintended overwrite never happens silently.
    static Task<GitRepository> CloneAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    // Clones a repository, or syncs it to the remote if it already exists. The sync path is destructive: it runs reset --hard then clean -fd, so any uncommitted changes and untracked files in an existing checkout are discarded. It syncs the branch named by GitCloneOptions.Branch, or the checkout's current branch when that is null.
    static Task<(GitRepository Repo, string? Sha, bool WasCloned)> CloneOrSyncAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    Task<GitCommit> CommitAsync(string message, CancellationToken ct = default)
    Task<GitCommit> CommitAsync(string message, string authorName, string authorEmail, bool allowEmpty = false, CancellationToken ct = default)
    // Builds per-invocation environment variables that authenticate git HTTP(S) operations. Uses git's environment config mechanism (git 2.31+) to inject an Authorization header, appending to any GIT_CONFIG_COUNT entries already present in the process environment.
    static Dictionary<string, string?> CreateAuthEnvironment(GitCredentials credentials)
    Task CreateBranchAsync(string name, string? startPoint = null, CancellationToken ct = default)
    Task<GitTag> CreateTagAsync(string name, string? message = null, CancellationToken ct = default)
    Task DeleteTagAsync(string name, CancellationToken ct = default)
    Task DiscardChangesAsync(CancellationToken ct = default)
    Task EnsureCleanRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    static string EscapeMessage(string message)
    Task FetchAsync(bool includeTags = false, CancellationToken ct = default)
    // Counts how many commits the local branch is ahead of and behind its origin counterpart. Returns null when the counts cannot be determined (e.g. origin/{branch} does not exist).
    Task<(int Ahead, int Behind)?> GetAheadBehindAsync(string branch, CancellationToken ct = default)
    Task<IReadOnlyList<GitBranch>> GetBranchesAsync(CancellationToken ct = default)
    Task<string?> GetConfigAsync(string key, CancellationToken ct = default)
    Task<string> GetCurrentBranchAsync(CancellationToken ct = default)
    // Diff between HEAD and target; a null target diffs the working directory.
    Task<GitDiff> GetDiffAsync(string? target = null, CancellationToken ct = default)
    Task<GitCommit?> GetHeadCommitAsync(CancellationToken ct = default)
    Task<string?> GetHeadShaAsync(bool shortSha = false, CancellationToken ct = default)
    Task<IReadOnlyList<GitCommit>> GetHistoryAsync(int limit = 20, string? fromRef = null, CancellationToken ct = default)
    // The remote URL exactly as stored in .git/config, including any embedded credentials.
    Task<string?> GetRawRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    // Credentials are stripped from the returned URL; see GetRawRemoteUrlAsync for the stored form.
    Task<string?> GetRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    Task<GitStatus> GetStatusAsync(CancellationToken ct = default)
    Task<IReadOnlyList<GitTag>> GetTagsAsync(CancellationToken ct = default)
    Task<bool> HasCommitsAsync(CancellationToken ct = default)
    Task<bool> HasRemoteAsync(string name = "origin", CancellationToken ct = default)
    Task<bool> HasStagedChangesAsync(CancellationToken ct = default)
    Task<bool> HasUncommittedChangesAsync(CancellationToken ct = default)
    Task<bool> HasUncommittedChangesAsync(string path, CancellationToken ct = default)
    // A branch that does not exist on origin counts as unpushed when local commits exist.
    Task<bool> HasUnpushedCommitsAsync(string branch, CancellationToken ct = default)
    // Initializes a git repository and connects it to a remote. Local files are kept as-is and NOT merged with remote content.
    static Task<GitRepository> InitAndConnectAsync(string directory, string remoteUrl, GitCredentials? credentials = null, string? configKey = null, string? configValue = null, CancellationToken ct = default)
    static Task<GitRepository> InitAsync(string directory, CancellationToken ct = default)
    Task<bool> IsGitRepositoryAsync(CancellationToken ct = default)
    static Task<bool> IsGitRepositoryAsync(string directory, CancellationToken ct = default)
    // Lists all worktrees attached to this repository, including the primary one.
    Task<IReadOnlyList<GitWorktreeInfo>> ListWorktreesAsync(CancellationToken ct = default)
    Task PushAsync(bool setUpstream = false, CancellationToken ct = default)
    // Reconciles the current branch with its origin counterpart and pushes, resolving divergence by auto-merge. Behaviour by state (after fetching origin): no origin remote → GitReconcileOutcome.NoRemote; detached / no branch → GitReconcileOutcome.Detached; branch not on the remote yet, or only local ahead → push (GitReconcileOutcome.Pushed); identical → GitReconcileOutcome.UpToDate; only remote ahead → fast-forward local (GitReconcileOutcome.Merged); diverged and git 3-way merges cleanly → push the merge (GitReconcileOutcome.Merged); diverged with a real content conflict → abort the merge and return GitReconcileOutcome.Conflicted with the conflicted files for the caller to resolve. Never destroys local commits: a conflict aborts back to the pre-merge state.
    Task<GitReconcileResult> ReconcileAndPushAsync(string commitAuthorName = "Ikon", string commitAuthorEmail = "ikon@ikon.local", CancellationToken ct = default)
    Task<bool> RefExistsAsync(string refName, CancellationToken ct = default)
    Task RenameBranchAsync(string oldName, string newName, CancellationToken ct = default)
    Task ResetHardAsync(string target, CancellationToken ct = default)
    Task ResetSoftAsync(string target, CancellationToken ct = default)
    // Restores the working tree to a target (tag, sha, or branch) with a hard reset after a fetch. Uncommitted changes and staged files are discarded, exactly as by SyncAsync.
    Task<GitSyncResult> RestoreAsync(string target, CancellationToken ct = default)
    Task<string> RunAsync(string args, CancellationToken ct = default)
    // Stages all changes, commits, and pushes.
    Task<GitSyncResult> SaveAsync(string message, CancellationToken ct = default)
    Task SetConfigAsync(string key, string value, CancellationToken ct = default)
    // Credentials are stripped from the URL before it is stored.
    Task SetRemoteUrlAsync(string name, string url, CancellationToken ct = default)
    Task SetUpstreamAsync(string remoteBranch, CancellationToken ct = default)
    static string ShortCommitHash(string? hash)
    Task StageAllAsync(CancellationToken ct = default)
    Task StagePathAsync(string path, CancellationToken ct = default)
    // Stashes all changes, untracked files included, without touching the index so a pop restores each file to its previous state.
    Task<bool> StashAsync(string? message = null, CancellationToken ct = default)
    Task<bool> StashPopAsync(CancellationToken ct = default)
    static string StripCredentialsFromUrl(string url)
    // Syncs to the latest remote with fetch + reset --hard: uncommitted changes and staged files are discarded.
    Task<GitSyncResult> SyncAsync(CancellationToken ct = default)
    static GitRepository? TryOpen(string directory)
    Task<(bool Success, string StdOut, string StdErr)> TryRunAsync(string args, CancellationToken ct = default)
    // Credentials and trailing slashes are ignored in the comparison.
    static bool UrlsMatch(string? url1, string? url2)
  record GitStatus
    ctor(string Branch, string? HeadSha, bool HasUncommittedChanges, bool IsDetachedHead, int AheadBy, int BehindBy, IReadOnlyList<GitFileChange> Changes)
    int AheadBy { get; init; }
    int BehindBy { get; init; }
    string Branch { get; init; }
    IReadOnlyList<GitFileChange> Changes { get; init; }
    bool HasUncommittedChanges { get; init; }
    string? HeadSha { get; init; }
    bool IsDetachedHead { get; init; }
  record GitSyncResult
    ctor(bool Success, string? PreviousSha, string? CurrentSha, string? Error = null)
    string? CurrentSha { get; init; }
    string? Error { get; init; }
    string? PreviousSha { get; init; }
    bool Success { get; init; }
  record GitTag
    ctor(string Name, string Sha, GitCommit? Commit = null)
    GitCommit? Commit { get; init; }
    string Name { get; init; }
    string Sha { get; init; }
  record GitWorktreeInfo
    ctor(string Path, string? Head, string? Branch)
    string? Branch { get; init; }
    string? Head { get; init; }
    string Path { get; init; }

namespace Ikon.Common.Reflection
  static class TaskTypeUnwrap
    // Takes whatever MethodInfo.Invoke handed back and produces its observable result. Awaits Task, Task<TResult>, ValueTask, ValueTask<TResult>; returns null for void-shaped awaitables; passes non-task values straight through.
    static ValueTask<object?> AwaitAndGetResultAsync(object? raw)
    // Maps a method's declared return type to the type the method actually produces: Task/ValueTask → void (there is no result), Task<T>/ValueTask<T> → T, anything else → as-is.
    static Type UnwrapResultType(Type declaredReturnType)
