# Ikon.Sdk Public API

namespace Ikon.Sdk
  sealed record ApiKeyConfig
    ctor()
    // From the portal; format 'ikon-xxxxx'.
    string ApiKey { get; init; }
    // Defaults to Production.
    BackendType BackendType { get; init; }
    // Default: DesktopApp
    ClientType ClientType { get; init; }
    // An arbitrary string, not an internal Ikon user ID — the backend creates/maps an internal user for it.
    string ExternalUserId { get; init; }
    // Join the live app session that owns this sessionIdentityHash, bypassing identity resolution. The connect fails when no live session has it — a hash never starts a fresh instance.
    string? SessionIdentityHash { get; init; }
    // MongoDB ObjectId, from the portal.
    string SpaceId { get; init; }
    // Default: Human
    UserType UserType { get; init; }
  // Uses the existing IkonBackend login credentials (login.json or environment variables); the preferred mode for internal Ikon C# applications.
  sealed record BackendConfig
    ctor()
    // Default: DesktopApp
    ClientType ClientType { get; init; }
    // An arbitrary string, not an internal Ikon user ID — the backend creates/maps an internal user for it.
    string ExternalUserId { get; init; }
    // Join the live app session that owns this sessionIdentityHash, bypassing identity resolution. The connect fails when no live session has it — a hash never starts a fresh instance.
    string? SessionIdentityHash { get; init; }
    // MongoDB ObjectId, from the portal.
    string SpaceId { get; init; }
    // Default: Human
    UserType UserType { get; init; }
  enum BackendType
    Production
    Development
  enum ConnectionState
    // Intentionally not connected: the initial state before ConnectAsync, and the state after a user-requested DisconnectAsync. Ready to connect; nothing went wrong.
    Idle
    Connecting
    Connected
    Reconnecting
    // Unexpectedly disconnected and not retrying: automatic reconnection was exhausted, or the server signalled an intentional shutdown. (A user-requested disconnect goes to Idle.)
    Offline
  static class ConnectionStateExtensions
    static bool IsConnected(this ConnectionState state)
    static bool IsConnecting(this ConnectionState state)
    // Returns true if the client is not connected and not connecting — this covers BOTH the pristine ConnectionState.Idle state (nothing went wrong) and the failure ConnectionState.Offline state. The name deliberately reads as "no live connection", not "failed": use IsFaulted to detect a failure specifically.
    static bool IsDisconnected(this ConnectionState state)
    // Returns true only for ConnectionState.Offline — the connection failed (auto-reconnect exhausted or the server shut down), as opposed to the intentional ConnectionState.Idle state before connect or after a requested disconnect.
    static bool IsFaulted(this ConnectionState state)
  sealed class IkonClient : IAsyncDisposable
    // config: Client configuration. Exactly one of ExternalConnectUrl, Local, ApiKey, Backend, or UserLogin must be specified.
    // throws ArgumentException: Thrown when configuration is invalid.
    ctor(IkonClientConfig config)
    // Null until the connection is established.
    Context? ClientContext { get; }
    IkonClientConfig Config { get; }
    // Default encoder options for audio output, used when a SendAudioAsync call passes no explicit encoderOptions. Captured when a stream's encoder is first created (the first SendAudioAsync for a given streamId, or for the shared fallback stream when none is given); changing it afterwards has no effect on already-active streams.
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    // Each IkonClient has its own isolated FunctionRegistry, so multiple SDK connections run independently (e.g. when running the SDK inside an Ikon app, or several clients in one process).
    FunctionRegistry FunctionRegistry { get; }
    // Null until the connection is established.
    GlobalState? GlobalState { get; }
    ConnectionState State { get; }
    // Valid only from ConnectionState.Idle or ConnectionState.Offline; throws InvalidOperationException if already connecting or connected. Calling it from ConnectionState.Offline while the background reconnect loop is still running stops that loop first, so the connection this call makes is the one the client keeps (if the loop happened to finish connecting meanwhile, the call returns with that connection). On failure the client returns to ConnectionState.Offline and the exception is rethrown. The same failure is also delivered to the ErrorOccurredAsync event before it is rethrown, so a caller that both handles that event and catches this call sees the failure twice — guard against double handling if both paths are wired.
    // ct: Cancellation token.
    // throws InvalidOperationException: Thrown if already connected or connecting.
    // throws Exception: Thrown on connection failure.
    Task ConnectAsync(CancellationToken ct = default)
    Task DisconnectAsync()
    ValueTask DisposeAsync()
    // Throws InvalidOperationException when the client is not connected — send only after ReadyAsync has fired; audio is never silently dropped. Safe to call concurrently: sends are serialized, so frames of one stream never interleave. A reconnect that lands on a new server session re-announces every active stream before its next frame.
    // samples: PCM samples in range [-1.0, 1.0]
    // sampleRate: Fixed per stream: the first call for a streamId configures its encoder and announces the format, so every later call must pass the same rate — a different one throws ArgumentException; use a new streamId for another format
    // channelCount: Fixed per stream like sampleRate
    // encoderOptions: Falls back to DefaultEncoderOptions; applied only when the stream's encoder is first created — later changes do not reconfigure an active stream
    ValueTask SendAudioAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Throws InvalidOperationException when the client is not connected — send only after ReadyAsync has fired. It does not silently drop the message.
    ValueTask SendMessageAsync(ProtocolMessage message)
    // Throws InvalidOperationException when the client is not connected — send only after ReadyAsync has fired. It does not silently drop the payload.
    ValueTask SendMessageAsync<T>(T payload) where T : IProtocolMessagePayload
    // Call once your setup completes, typically from the ReadyAsync handler. Throws if not connected.
    Task SignalReadyAsync()
    // Waits up to timeout (30 seconds when null) for a client matching productId/userId. An explicit TimeSpan.Zero is honored as a single poll rather than promoted to the default. Throws if not connected.
    Task<bool> WaitForClientAsync(string? productId = null, string? userId = null, TimeSpan? timeout = null)
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputFrameEventArgs> AudioInputFrameAsync
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    event IkonClient.AsyncEventHandler<EventArgs>? DisconnectedAsync
    event IkonClient.AsyncEventHandler<IkonClient.ErrorEventArgs>? ErrorOccurredAsync
    event IkonClient.AsyncEventHandler<MessageEventArgs>? MessageReceivedAsync
    event IkonClient.AsyncEventHandler<EventArgs>? ReadyAsync
    // Unlike the other events this one is not awaited by the transition that raised it: the client moves on as soon as the state is set, so a handler that awaits may observe State already past ConnectionStateEventArgs.State — read the state from the args. A handler exception is delivered to ErrorOccurredAsync rather than lost as an unobserved task.
    event IkonClient.AsyncEventHandler<IkonClient.ConnectionStateEventArgs>? StateChangedAsync
    // Messages can still be sent from this handler.
    event IkonClient.AsyncEventHandler<EventArgs>? StoppingAsync
  delegate IkonClient.AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
  class IkonClient.AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration)
    bool IsFirst { get; }
    bool IsLast { get; }
    // Decoded floating point PCM samples in range [-1.0, 1.0]
    float[] Samples { get; }
    string StreamId { get; }
    // Total duration of the audio if known, otherwise zero
    TimeSpan TotalDuration { get; }
  class IkonClient.AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, AudioCodec codec, string codecDetails, int sampleRate, int channelCount)
    int ChannelCount { get; }
    AudioCodec Codec { get; }
    string CodecDetails { get; }
    string Description { get; }
    // A begin-event handler may set it to choose the rate the stream is decoded at.
    int SampleRate { get; set; }
    string SourceType { get; }
    string StreamId { get; }
    // Default Streaming; a begin-event handler may set it to delay frame delivery.
    AudioInputStreamingMode StreamingMode { get; set; }
  class IkonClient.AudioInputStreamEndEventArgs : EventArgs
    ctor(string streamId)
    string StreamId { get; }
  class IkonClient.ConnectionStateEventArgs : EventArgs
    ctor(ConnectionState state)
    ConnectionState State { get; }
  class IkonClient.ErrorEventArgs : EventArgs
    ctor(Exception error)
    Exception Error { get; }
  // Exactly one authentication mode — ExternalConnectUrl, Local, ApiKey, Backend, or UserLogin — must be set; the constructor rejects zero or multiple.
  sealed record IkonClientConfig
    ctor()
    ApiKeyConfig? ApiKey { get; init; }
    BackendConfig? Backend { get; init; }
    // Default ContextType.Plugin connects as a backend component: no UI, no per-connection ClientScope. Set ContextType.Native (or ContextType.Browser) to connect as a first-class PLAYER client that receives a ClientScope and streamed UI, like the web client.
    ContextType ContextType { get; init; }
    // Default: "Ikon SDK C#"
    string Description { get; init; }
    // If not provided, a random one is generated.
    string? DeviceId { get; init; }
    // Whether to establish the unreliable UDP side channel alongside the TCP connection when the server advertises one. Default true. Set false to run over TCP only — unreliable-flagged messages then fall back to the reliable channel.
    bool EnableUdpChannel { get; init; }
    // When set, authentication is skipped and the client connects straight through this URL — the same mechanism the TypeScript SDK reads from its query parameter. Mutually exclusive with Local, ApiKey, Backend, and UserLogin.
    string? ExternalConnectUrl { get; init; }
    // Delivered to the app as Context.InitialPath at join, like a web client opening a deep link. Empty means the app's root.
    string InitialPath { get; init; }
    string? InstallId { get; init; }
    // Sets Context.IsSnapshot on the server so the app renders its privacy-safe snapshot variant. Only the build-time boot-snapshot capture client sets this; leave false otherwise.
    bool IsSnapshot { get; init; }
    LocalConfig? Local { get; init; }
    // Default: "en-US"
    string Locale { get; init; }
    // Default: All groups
    Opcode OpcodeGroupsFromServer { get; init; }
    // Default: All groups
    Opcode OpcodeGroupsToServer { get; init; }
    Dictionary<string, string>? Parameters { get; init; }
    // Default: Teleport
    PayloadType PayloadType { get; init; }
    string? ProductId { get; init; }
    // Boot-snapshot variant id this capture client asks the app to render, carried into Context.SnapshotVariant. Empty for route captures and all live clients; only variant captures set this (together with IsSnapshot).
    string SnapshotVariant { get; init; }
    TimeoutConfig Timeouts { get; init; }
    string? UserAgent { get; init; }
    UserLoginConfig? UserLogin { get; init; }
    // Version identifier, as a whole number in string form ("3"). Sent to the backend verbatim, but the ikon-server sees it as an integer: a value that does not parse as one is reported as version 1 and logged as a warning at connect time.
    string? VersionId { get; init; }
  sealed record LocalConfig
    ctor()
    string Host { get; init; }
    int HttpsPort { get; init; }
    // Falls back to "local" if not provided (with a warning).
    string? UserId { get; init; }
  class MessageEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
  // One registry per IkonClient: construct it over client.FunctionRegistry after the connection is established, and call Detach on teardown. The current value is fetched on first subscribe and pushed by the server on every change — no polling. Subscriptions live in the server session. A reconnect that resumes the same session keeps them, but one that creates a new session (ClientContext.SessionId changed since the subscribe) drops them server-side while the local callbacks stay registered and silently stop firing. Call ResubscribeAsync from the client's ReadyAsync handler to re-establish them.
  sealed class ReactiveRegistry
    ctor(FunctionRegistry functionRegistry)
    // Drop all subscriptions and unregister the update handler. Intended for client teardown — does not notify the server per key (the server's per-session subscription map is cleaned up when the session disconnects).
    void Detach()
    // Re-send Subscribe for every key that still has local subscribers and hand each of them the value the server returns. Call it after a reconnect that produced a new server session, where the server-side subscriptions are gone but the local callbacks remain. Safe to call after a reconnect that resumed the session — the server treats the repeat subscribe as a no-op and the callbacks simply receive the current value once more.
    // throws AggregateException: One or more keys could not be re-subscribed; the rest were.
    Task<int> ResubscribeAsync(CancellationToken cancellationToken = default)
    // Dispose the returned handle to unsubscribe — the last unsubscribe for a key notifies the server.
    // stableId: The reactive's IReactiveWithState.StableId.
    // callback: Invoked with each value. JSON is deserialized to T.
    // mountId: Mount id when subscribing to a server-side MountReactive<T>; empty (the default) works for unscoped Reactive<T>, ClientReactive<T>, and UserReactive<T>.
    // cancellationToken: Cancels the initial Subscribe call.
    Task<IAsyncDisposable> SubscribeAsync<T>(string stableId, Action<T> callback, string mountId = "", CancellationToken cancellationToken = default)
  sealed record TimeoutConfig
    ctor()
    // When true, the client keeps retrying with capped exponential backoff after the fast reconnection ladder is exhausted, instead of staying Offline until the next explicit call. Default: true
    bool BackgroundReconnect { get; init; }
    // Each subsequent attempt doubles the delay (500ms, 1s, 2s, 4s). Default: 500 milliseconds
    TimeSpan InitialReconnectDelay { get; init; }
    // Maximum number of attempts for the initial connect. Retries are spaced by the same capped exponential backoff as the reconnection ladder, and only a transport-level failure is retried — a rejection the backend actually answered fails on the first attempt. Default: 3
    int MaxConnectAttempts { get; init; }
    // Default: 4
    int MaxReconnectAttempts { get; init; }
    // Upper bound for the delay between background reconnection attempts. Default: 30 seconds
    TimeSpan MaxReconnectDelay { get; init; }
    // Time budget for a single reconnection attempt (per tier), bounding an attempt against a half-open connection that would otherwise hang the recovery ladder. Default: 30 seconds
    TimeSpan ReconnectAttemptTimeout { get; init; }
  // Authenticate as the developer logged in on this machine (the ikon CLI's stored login). Connects through the cloud gateway exactly like a browser client, so gateway features — cell routing via Parameters above all — apply. Intended for dev tooling, spikes, and headless tests; production clients use ApiKeyConfig or BackendConfig.
  sealed record UserLoginConfig
    ctor()
    // Default: DesktopApp
    ClientType ClientType { get; init; }
    // MongoDB ObjectId, from the portal.
    string SpaceId { get; init; }
    // Default: Human
    UserType UserType { get; init; }
  // Build stamp for this component: the version of the build it was compiled from, exposed as a compile-time constant. Generated on every build from versions.json and git state, so never edit it by hand. Note that this type shadows System.Version in any file that imports this namespace — write System.Version explicitly there when you mean the BCL type.
  static class Version
    // The version this build was produced from, in the shape git describe uses: the release version, the number of commits since that release, the short commit hash, a -dirty suffix when the working tree had uncommitted changes, and the branch name on any branch other than main.
    const string VersionString
