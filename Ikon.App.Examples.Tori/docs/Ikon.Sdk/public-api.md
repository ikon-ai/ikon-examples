# Ikon.Sdk Public API

namespace Ikon.Sdk
  // Configuration for API key authentication mode. Use this for programmatic access to cloud apps.
  sealed record ApiKeyConfig
    ctor()
    // API key for the space (from portal, format: 'ikon-xxxxx').
    string ApiKey { get; init; }
    // Backend environment. Defaults to Production.
    BackendType BackendType { get; init; }
    // Client type for this connection. Default: DesktopApp
    ClientType ClientType { get; init; }
    // External user identifier - an arbitrary string to identify the user. This does not need to be an internal Ikon user ID. The backend will create/map an internal user for this external ID.
    string ExternalUserId { get; init; }
    // Join the live app session that owns this sessionIdentityHash, bypassing identity resolution. The connect fails when no live session has it — a hash never starts a fresh instance.
    string? SessionIdentityHash { get; init; }
    // Space ID (MongoDB ObjectId from portal).
    string SpaceId { get; init; }
    // User type for this connection. Default: Human
    UserType UserType { get; init; }
  // Configuration for backend authentication mode. Uses existing IkonBackend login credentials (from login.json or environment variables). This is the preferred mode for internal Ikon C# applications.
  sealed record BackendConfig
    ctor()
    // Client type for this connection. Default: DesktopApp
    ClientType ClientType { get; init; }
    // External user identifier - an arbitrary string to identify the user. This does not need to be an internal Ikon user ID. The backend will create/map an internal user for this external ID.
    string ExternalUserId { get; init; }
    // Join the live app session that owns this sessionIdentityHash, bypassing identity resolution. The connect fails when no live session has it — a hash never starts a fresh instance.
    string? SessionIdentityHash { get; init; }
    // Space ID (MongoDB ObjectId from portal).
    string SpaceId { get; init; }
    // User type for this connection. Default: Human
    UserType UserType { get; init; }
  // Backend environment type.
  enum BackendType
    // Production backend (api.prod.ikon.live).
    Production
    // Development backend (api.dev.ikon.live).
    Development
  // Connection state of the IkonClient.
  enum ConnectionState
    // Intentionally not connected: the initial state before ConnectAsync, and the state after a user-requested DisconnectAsync. Ready to connect; nothing went wrong.
    Idle
    // Authentication and connection in progress.
    Connecting
    // Fully connected and ready.
    Connected
    // Lost connection, attempting automatic reconnect.
    Reconnecting
    // Unexpectedly disconnected and not retrying: automatic reconnection was exhausted, or the server signalled an intentional shutdown. (A user-requested disconnect goes to Idle.)
    Offline
  // Helper methods for ConnectionState.
  static class ConnectionStateExtensions
    // Returns true if the state represents a successful connection.
    static bool IsConnected(this ConnectionState state)
    // Returns true if the state represents an active connection attempt.
    static bool IsConnecting(this ConnectionState state)
    // Returns true if the client is not connected and not connecting — this covers BOTH the pristine ConnectionState.Idle state (nothing went wrong) and the failure ConnectionState.Offline state. The name deliberately reads as "no live connection", not "failed": use IsFaulted to detect a failure specifically.
    static bool IsDisconnected(this ConnectionState state)
    // Returns true only for ConnectionState.Offline — the connection failed (auto-reconnect exhausted or the server shut down), as opposed to the intentional ConnectionState.Idle state before connect or after a requested disconnect.
    static bool IsFaulted(this ConnectionState state)
  // Main client for connecting to Ikon servers. Features: - Single connection per client instance - Five authentication modes: ExternalConnectUrl, Local, ApiKey, Backend, UserLogin - Automatic reconnection with exponential backoff - Audio encoding/decoding helpers - Function registration via FunctionRegistry
  sealed class IkonClient : IAsyncDisposable
    // Creates a new IkonClient with the specified configuration. Each IkonClient instance gets its own FunctionRegistry, enabling multiple SDK connections to run independently without conflicts (e.g., when running SDK inside an Ikon app).
    // config: Client configuration. Exactly one of ExternalConnectUrl, Local, ApiKey, Backend, or UserLogin must be specified.
    // throws ArgumentException: Thrown when configuration is invalid.
    ctor(IkonClientConfig config)
    // Client context from the server. Available after connection is established.
    Context? ClientContext { get; }
    // Configuration used to create this client.
    IkonClientConfig Config { get; }
    // Default encoder options for audio output, used when a SendAudioAsync call passes no explicit encoderOptions. Captured when a stream's encoder is first created (the first SendAudioAsync for a given streamId, or for the shared fallback stream when none is given); changing it afterwards has no effect on already-active streams.
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    // Function registry for this client instance. Each IkonClient has its own isolated FunctionRegistry, allowing multiple SDK connections to run independently (e.g., when running SDK inside an Ikon app, or multiple SDK clients).
    FunctionRegistry FunctionRegistry { get; }
    // Global state from the server. Available after connection is established.
    GlobalState? GlobalState { get; }
    // Current connection state.
    ConnectionState State { get; }
    // Valid only from ConnectionState.Idle or ConnectionState.Offline; throws InvalidOperationException if already connecting or connected. On failure the client returns to ConnectionState.Offline and the exception is rethrown. The same failure is also delivered to the ErrorOccurredAsync event before it is rethrown, so a caller that both handles that event and catches this call sees the failure twice — guard against double handling if both paths are wired.
    // ct: Cancellation token.
    // throws InvalidOperationException: Thrown if already connected or connecting.
    // throws Exception: Thrown on connection failure.
    Task ConnectAsync(CancellationToken ct = default)
    // Disconnect from the server and release connection-specific resources.
    Task DisconnectAsync()
    // Disposes the client and releases all resources.
    ValueTask DisposeAsync()
    // Throws InvalidOperationException when the client is not connected — send only after ReadyAsync has fired. It does not silently drop the audio.
    // samples: Floating point PCM samples in range [-1.0, 1.0]
    // sampleRate: Sample rate
    // channelCount: Number of channels
    // isFirst: Whether the first sample of the sent audio is the beginning
    // isLast: Whether the last sample of the sent audio is the end
    // streamId: Optional unique identifier for this audio stream
    // totalDuration: Optional total duration of the audio to be output, if known
    // encoderOptions: Optional encoder options. Falls back to DefaultEncoderOptions if not specified. Applied only when this stream's encoder is first created; later changes do not reconfigure an already-active stream
    // targetIds: Optional list of target session IDs to send to
    ValueTask SendAudioAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Throws InvalidOperationException when the client is not connected — send only after ReadyAsync has fired. It does not silently drop the message.
    ValueTask SendMessageAsync(ProtocolMessage message)
    // Throws InvalidOperationException when the client is not connected — send only after ReadyAsync has fired. It does not silently drop the payload.
    ValueTask SendMessageAsync<T>(T payload) where T : IProtocolMessagePayload
    // Call once your setup completes, typically from the ReadyAsync handler. Throws if not connected.
    Task SignalReadyAsync()
    // Waits up to timeout (30 seconds when null) for a client matching productId/userId. An explicit TimeSpan.Zero is honored as a single poll rather than promoted to the default. Throws if not connected.
    Task<bool> WaitForClientAsync(string? productId = null, string? userId = null, TimeSpan? timeout = null)
    // Event raised when an incoming audio frame is received and decoded
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputFrameEventArgs> AudioInputFrameAsync
    // Event raised when an incoming audio stream begins
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    // Event raised when an incoming audio stream ends
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Event triggered after disconnection.
    event IkonClient.AsyncEventHandler<EventArgs>? DisconnectedAsync
    // Event triggered when an error occurs.
    event IkonClient.AsyncEventHandler<IkonClient.ErrorEventArgs>? ErrorOccurredAsync
    // Event triggered when a protocol message is received.
    event IkonClient.AsyncEventHandler<MessageEventArgs>? MessageReceivedAsync
    // Event triggered when connection is fully established and ready. Called before SignalReadyAsync() should be called.
    event IkonClient.AsyncEventHandler<EventArgs>? ReadyAsync
    // Event triggered when connection state changes.
    event IkonClient.AsyncEventHandler<IkonClient.ConnectionStateEventArgs>? StateChangedAsync
    // Event triggered when server is stopping. Messages can still be sent in this handler.
    event IkonClient.AsyncEventHandler<EventArgs>? StoppingAsync
  // Async event handler delegate for IkonClient events.
  delegate IkonClient.AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
  // Event arguments raised when an incoming audio frame is received
  class IkonClient.AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration)
    // Whether this is the first frame in a sequence
    bool IsFirst { get; }
    // Whether this is the last frame in a sequence
    bool IsLast { get; }
    // Decoded floating point PCM samples in range [-1.0, 1.0]
    float[] Samples { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // Total duration of the audio if known, otherwise zero
    TimeSpan TotalDuration { get; }
  // Event arguments raised when an incoming audio stream begins
  class IkonClient.AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, AudioCodec codec, string codecDetails, int sampleRate, int channelCount)
    // Number of audio channels
    int ChannelCount { get; }
    // Audio codec used for encoding/decoding
    AudioCodec Codec { get; }
    // Codec-specific details
    string CodecDetails { get; }
    // Description of the audio stream
    string Description { get; }
    // Sample rate in Hz (can be modified by event handler)
    int SampleRate { get; set; }
    // Source type of the audio stream (e.g., "microphone")
    string SourceType { get; }
    // Unique identifier for the audio stream
    string StreamId { get; }
    // Controls when frames are output (can be modified by event handler)
    AudioInputStreamingMode StreamingMode { get; set; }
  // Event arguments raised when an incoming audio stream ends
  class IkonClient.AudioInputStreamEndEventArgs : EventArgs
    ctor(string streamId)
    // Unique identifier for the audio stream
    string StreamId { get; }
  // Event arguments for connection state changes.
  class IkonClient.ConnectionStateEventArgs : EventArgs
    ctor(ConnectionState state)
    // The new connection state.
    ConnectionState State { get; }
  // Event arguments for errors.
  class IkonClient.ErrorEventArgs : EventArgs
    ctor(Exception error)
    // The error that occurred.
    Exception Error { get; }
  // Exactly one authentication mode — ExternalConnectUrl, Local, ApiKey, Backend, or UserLogin — must be set; the constructor rejects zero or multiple.
  sealed record IkonClientConfig
    ctor()
    // API key authentication for programmatic access. Use this for libraries, scripts, plugins that need to connect to cloud apps.
    ApiKeyConfig? ApiKey { get; init; }
    // Backend authentication using existing IkonBackend login. Use this for internal Ikon C# applications that have already logged in via CLI.
    BackendConfig? Backend { get; init; }
    // Default ContextType.Plugin connects as a backend component: no UI, no per-connection ClientScope. Set ContextType.Native (or ContextType.Browser) to connect as a first-class PLAYER client that receives a ClientScope and streamed UI, like the web client.
    ContextType ContextType { get; init; }
    // Description for this client. Default: "Ikon SDK C#"
    string Description { get; init; }
    // Device ID for the connection. If not provided, a random one will be generated.
    string? DeviceId { get; init; }
    // Whether to establish the unreliable UDP side channel alongside the TCP connection when the server advertises one. Default true. Set false to run over TCP only — unreliable-flagged messages then fall back to the reliable channel.
    bool EnableUdpChannel { get; init; }
    // When set, authentication is skipped and the client connects straight through this URL — the same mechanism the TypeScript SDK reads from its query parameter. Mutually exclusive with Local, ApiKey, Backend, and UserLogin.
    string? ExternalConnectUrl { get; init; }
    // Delivered to the app as Context.InitialPath at join, like a web client opening a deep link. Empty means the app's root.
    string InitialPath { get; init; }
    // Installation ID.
    string? InstallId { get; init; }
    // Sets Context.IsSnapshot on the server so the app renders its privacy-safe snapshot variant. Only the build-time boot-snapshot capture client sets this; leave false otherwise.
    bool IsSnapshot { get; init; }
    // Local server configuration for development mode. Use this when connecting to a local Ikon server.
    LocalConfig? Local { get; init; }
    // User locale (e.g., "en-US"). Default: "en-US"
    string Locale { get; init; }
    // Opcode groups to receive from server. Default: All groups
    Opcode OpcodeGroupsFromServer { get; init; }
    // Opcode groups to send to server. Default: All groups
    Opcode OpcodeGroupsToServer { get; init; }
    // Client parameters passed to the server.
    Dictionary<string, string>? Parameters { get; init; }
    // Payload type for protocol messages. Default: Teleport
    PayloadType PayloadType { get; init; }
    // Product identifier.
    string? ProductId { get; init; }
    // Boot-snapshot variant id this capture client asks the app to render, carried into Context.SnapshotVariant. Empty for route captures and all live clients; only variant captures set this (together with IsSnapshot).
    string SnapshotVariant { get; init; }
    // Timeout configuration.
    TimeoutConfig Timeouts { get; init; }
    // User agent string.
    string? UserAgent { get; init; }
    // Developer-login authentication (the machine's ikon CLI login). See UserLoginConfig.
    UserLoginConfig? UserLogin { get; init; }
    // Version identifier.
    string? VersionId { get; init; }
  // Configuration for local development mode. Connects directly to a local Ikon server.
  sealed record LocalConfig
    ctor()
    // Host of the local Ikon server. Example: "localhost"
    string Host { get; init; }
    // HTTPS port of the local Ikon server. Example: 8443
    int HttpsPort { get; init; }
    // User ID for the connection. Falls back to "local" if not provided (with a warning).
    string? UserId { get; init; }
  // Event arguments for protocol messages.
  class MessageEventArgs : EventArgs
    ctor(ProtocolMessage message)
    // The protocol message.
    ProtocolMessage Message { get; }
  // One registry per IkonClient: construct it over client.FunctionRegistry after the connection is established, and call Detach on teardown. The current value is fetched on first subscribe and pushed by the server on every change — no polling.
  sealed class ReactiveRegistry
    // Create a registry over an IkonClient's function registry. Registers the reactive-update handler immediately; call Detach on teardown.
    ctor(FunctionRegistry functionRegistry)
    // Drop all subscriptions and unregister the update handler. Intended for client teardown — does not notify the server per key (the server's per-session subscription map is cleaned up when the session disconnects).
    void Detach()
    // Subscribe to a server-side reactive identified by its stable id. callback fires once with the current value, then on every server-side change. Dispose the returned handle to unsubscribe — the last unsubscribe for a key notifies the server.
    // stableId: The reactive's IReactiveWithState.StableId.
    // callback: Invoked with each value. JSON is deserialized to T.
    // mountId: Mount id when subscribing to a server-side MountReactive<T>; empty (the default) works for unscoped Reactive<T>, ClientReactive<T>, and UserReactive<T>.
    // cancellationToken: Cancels the initial Subscribe call.
    Task<IAsyncDisposable> SubscribeAsync<T>(string stableId, Action<T> callback, string mountId = "", CancellationToken cancellationToken = default)
  // Timeout configuration for the SDK.
  sealed record TimeoutConfig
    ctor()
    // When true, the client keeps retrying with capped exponential backoff after the fast reconnection ladder is exhausted, instead of staying Offline until the next explicit call. Default: true
    bool BackgroundReconnect { get; init; }
    // Initial delay before the first reconnection attempt. Each subsequent attempt doubles the delay (e.g. 500ms, 1s, 2s, 4s). Default: 500 milliseconds
    TimeSpan InitialReconnectDelay { get; init; }
    // Maximum number of attempts for the initial connect. Retries are spaced by the same capped exponential backoff as the reconnection ladder, and only a transport-level failure is retried — a rejection the backend actually answered fails on the first attempt. Default: 3
    int MaxConnectAttempts { get; init; }
    // Maximum number of reconnection attempts. Default: 4
    int MaxReconnectAttempts { get; init; }
    // Upper bound for the delay between background reconnection attempts. Default: 30 seconds
    TimeSpan MaxReconnectDelay { get; init; }
    // Time budget for a single reconnection attempt (per tier). Without it, a reconnect attempt against a half-open connection can hang indefinitely and block the whole recovery ladder. Default: 30 seconds
    TimeSpan ReconnectAttemptTimeout { get; init; }
  // Authenticate as the developer logged in on this machine (the ikon CLI's stored login). Connects through the cloud gateway exactly like a browser client, so gateway features — cell routing via Parameters above all — apply. Intended for dev tooling, spikes, and headless tests; production clients use ApiKeyConfig or BackendConfig.
  sealed record UserLoginConfig
    ctor()
    // Client type for this connection. Default: DesktopApp
    ClientType ClientType { get; init; }
    // Space ID (MongoDB ObjectId from portal).
    string SpaceId { get; init; }
    // User type for this connection. Default: Human
    UserType UserType { get; init; }
  // Build stamp for this component: the version of the build it was compiled from, exposed as a compile-time constant. Generated on every build from versions.json and git state, so never edit it by hand. Note that this type shadows System.Version in any file that imports this namespace — write System.Version explicitly there when you mean the BCL type.
  static class Version
    // The version this build was produced from, in the shape git describe uses: the release version, the number of commits since that release, the short commit hash, a -dirty suffix when the working tree had uncommitted changes, and the branch name on any branch other than main.
    const string VersionString
