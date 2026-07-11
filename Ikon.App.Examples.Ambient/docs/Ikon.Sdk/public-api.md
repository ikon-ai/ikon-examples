# Ikon.Sdk Public API

namespace Ikon.Sdk
  // Configuration for API key authentication mode. Use this for programmatic access to cloud channels.
  sealed class ApiKeyConfig : IEquatable<ApiKeyConfig>
    ctor()
    // API key for the space (from portal, format: 'ikon-xxxxx').
    string ApiKey { get; init; }
    // Backend environment. Defaults to Production.
    BackendType BackendType { get; init; }
    // Optional channel key (slug) for spaces with multiple channels. If not provided, connects to the first available channel.
    string? ChannelKey { get; init; }
    // Client type for this connection. Default: DesktopApp
    ClientType ClientType { get; init; }
    // External user identifier - an arbitrary string to identify the user. This does not need to be an internal Ikon user ID. The backend will create/map an internal user for this external ID.
    string ExternalUserId { get; init; }
    // Optional session ID for targeting precomputed sessions.
    string? SessionId { get; init; }
    // Space ID (MongoDB ObjectId from portal).
    string SpaceId { get; init; }
    // User type for this connection. Default: Human
    UserType UserType { get; init; }
  delegate IkonClient.AsyncEventHandler<TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler`1<TEventArgs>(TEventArgs e)
  class IkonClient.AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration)
    bool IsFirst { get; }
    bool IsLast { get; }
    float[] Samples { get; }
    string StreamId { get; }
    TimeSpan TotalDuration { get; set; }
  class IkonClient.AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, AudioCodec codec, string codecDetails, int sampleRate, int channelCount)
    int ChannelCount { get; }
    AudioCodec Codec { get; }
    string CodecDetails { get; }
    string Description { get; }
    int SampleRate { get; set; }
    string SourceType { get; }
    string StreamId { get; }
    AudioInputStreamingMode StreamingMode { get; set; }
  class IkonClient.AudioInputStreamEndEventArgs : EventArgs
    ctor(string streamId)
    string StreamId { get; }
  // Configuration for backend authentication mode. Uses existing IkonBackend login credentials (from login.json or environment variables). This is the preferred mode for internal Ikon C# applications.
  sealed class BackendConfig : IEquatable<BackendConfig>
    ctor()
    // Optional channel key (slug) for spaces with multiple channels. If not provided, connects to the first available channel.
    string? ChannelKey { get; init; }
    // Client type for this connection. Default: DesktopApp
    ClientType ClientType { get; init; }
    // External user identifier - an arbitrary string to identify the user. This does not need to be an internal Ikon user ID. The backend will create/map an internal user for this external ID.
    string ExternalUserId { get; init; }
    // Optional session ID for targeting precomputed sessions.
    string? SessionId { get; init; }
    // Space ID (MongoDB ObjectId from portal).
    string SpaceId { get; init; }
    // User type for this connection. Default: Human
    UserType UserType { get; init; }
  // Backend environment type.
  enum BackendType
    Production
    Development
  // Connection state of the IkonClient.
  enum ConnectionState
    Idle
    Connecting
    Connected
    Reconnecting
    Offline
  class IkonClient.ConnectionStateEventArgs : EventArgs
    ctor(ConnectionState state)
    ConnectionState State { get; }
  // Helper methods for ConnectionState.
  static class ConnectionStateExtensions
    // Returns true if the state represents a successful connection.
    static bool IsConnected(ConnectionState state)
    // Returns true if the state represents an active connection attempt.
    static bool IsConnecting(ConnectionState state)
    // Returns true if the state represents a disconnected state.
    static bool IsOffline(ConnectionState state)
  class IkonClient.ErrorEventArgs : EventArgs
    ctor(Exception error)
    Exception Error { get; }
  // Main client for connecting to Ikon servers. Features: - Single connection per client instance - Three authentication modes: Local, ApiKey, Backend - Automatic reconnection with exponential backoff - Audio encoding/decoding helpers - Function registration via FunctionRegistry
  sealed class IkonClient : IAsyncDisposable
    // Creates a new IkonClient with the specified configuration. Each IkonClient instance gets its own FunctionRegistry, enabling multiple SDK connections to run independently without conflicts (e.g., when running SDK inside an Ikon app).
    ctor(IkonClientConfig config)
    // Client context from the server. Available after connection is established.
    Context? ClientContext { get; }
    // Configuration used to create this client.
    IkonClientConfig Config { get; }
    // Default encoder options for audio output
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    // Function registry for this client instance. Each IkonClient has its own isolated FunctionRegistry, allowing multiple SDK connections to run independently (e.g., when running SDK inside an Ikon app, or multiple SDK clients).
    FunctionRegistry FunctionRegistry { get; }
    // Global state from the server. Available after connection is established.
    GlobalState? GlobalState { get; }
    // Current connection state.
    ConnectionState State { get; }
    // Connect to the Ikon server.
    Task ConnectAsync(CancellationToken ct = default)
    // Disconnect from the server and release connection-specific resources.
    Task DisconnectAsync()
    // Disposes the client and releases all resources.
    ValueTask DisposeAsync()
    // Sends audio data to the server.
    ValueTask SendAudioAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // Send a protocol message to the server.
    ValueTask SendMessageAsync(ProtocolMessage message)
    // Send a typed payload to the server.
    ValueTask SendMessageAsync<T>(T payload) where T : IProtocolMessagePayload
    // Signal that the client is ready. Should be called after initialization in the ReadyAsync event handler.
    Task SignalReadyAsync()
    // Wait for a specific client to connect and become ready.
    Task<bool> WaitForClientAsync(string? productId = null, string? userId = null, TimeSpan timeout = default)
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
  // Configuration for IkonClient. Exactly one of the four authentication modes must be provided: ExternalConnectUrl, Local, ApiKey, or Backend.
  sealed class IkonClientConfig : IEquatable<IkonClientConfig>
    ctor()
    // API key authentication for programmatic access. Use this for libraries, scripts, plugins that need to connect to cloud channels.
    ApiKeyConfig? ApiKey { get; init; }
    // Backend authentication using existing IkonBackend login. Use this for internal Ikon C# applications that have already logged in via CLI.
    BackendConfig? Backend { get; init; }
    // How this connection identifies to the server. Default Plugin (a backend component — no UI, no per-connection ClientScope). Set to Native (or Browser ) to connect as a first-class PLAYER client — the server then gives it a per-connection ClientScope and streams UI, exactly like the web (TypeScript SDK) client.
    ContextType ContextType { get; init; }
    // Description for this client. Default: "Ikon SDK C#"
    string Description { get; init; }
    // Device ID for the connection. If not provided, a random one will be generated.
    string? DeviceId { get; init; }
    // The fourth authentication mode: a pre-minted connect URL ("{serverUrl}/connect?token=…") issued by a trusted host — e.g. an embedded in-process app server whose /connect-token oracle is disabled mints these for its own clients (IAppHost.MintBrowserConnectUrl). When set, the authentication step is skipped and the client connects straight through this URL — the same external-connect-URL mechanism the TypeScript SDK consumes from its query parameter. Mutually exclusive with Local, ApiKey, and Backend; Validate rejects a config that combines them.
    string? ExternalConnectUrl { get; init; }
    // Installation ID.
    string? InstallId { get; init; }
    // Connect as the build-time boot-snapshot capture client, setting Context.IsSnapshot on the server so the app renders its privacy-safe snapshot variant (see the Parallax Snapshot* wrappers). Default false — only the snapshot-capture run sets this.
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
    // Timeout configuration.
    TimeoutConfig Timeouts { get; init; }
    // User agent string.
    string? UserAgent { get; init; }
    // Version identifier.
    string? VersionId { get; init; }
  // Configuration for local development mode. Connects directly to a local Ikon server.
  sealed class LocalConfig : IEquatable<LocalConfig>
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
  // Subscribes local callbacks to a server-side Reactive over the existing function-call wire. The current value is fetched on first subscribe and pushed by the server on every change — no polling.
  sealed class ReactiveRegistry
    // Create a registry over an IkonClient 's function registry. Registers the reactive-update handler immediately; call Detach on teardown.
    ctor(FunctionRegistry functionRegistry)
    // Drop all subscriptions and unregister the update handler. Intended for client teardown — does not notify the server per key (the server's per-session subscription map is cleaned up when the session disconnects).
    void Detach()
    // Subscribe to a server-side reactive identified by its stable id. callback fires once with the current value, then on every server-side change. Dispose the returned handle to unsubscribe — the last unsubscribe for a key notifies the server.
    Task<IAsyncDisposable> SubscribeAsync<T>(string stableId, Action<T> callback, string mountId = "", CancellationToken cancellationToken = default)
  // Timeout configuration for the SDK.
  sealed class TimeoutConfig : IEquatable<TimeoutConfig>
    ctor()
    // Initial delay before the first reconnection attempt. Each subsequent attempt doubles the delay (e.g. 500ms, 1s, 2s, 4s). Default: 500 milliseconds
    TimeSpan InitialReconnectDelay { get; init; }
    // Maximum number of reconnection attempts. Default: 4
    int MaxReconnectAttempts { get; init; }
  // Version class
  static class Version
    // Version string for the library
    static string VersionString
