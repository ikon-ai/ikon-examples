# Ikon.Sdk Public API

namespace Ikon.Sdk
  sealed record ApiKeyConfig
    ctor()
    string ApiKey { get; init; }
    BackendType BackendType { get; init; }
    ClientType ClientType { get; init; }
    string ExternalUserId { get; init; }
    string? SessionIdentityHash { get; init; }
    string SpaceId { get; init; }
    UserType UserType { get; init; }
  sealed record BackendConfig
    ctor()
    ClientType ClientType { get; init; }
    string ExternalUserId { get; init; }
    string? SessionIdentityHash { get; init; }
    string SpaceId { get; init; }
    UserType UserType { get; init; }
  enum BackendType
    Production
    Development
  enum ConnectionState
    Idle
    Connecting
    Connected
    Reconnecting
    Offline
  static class ConnectionStateExtensions
    static bool IsConnected(this ConnectionState state)
    static bool IsConnecting(this ConnectionState state)
    static bool IsOffline(this ConnectionState state)
  sealed class IkonClient : IAsyncDisposable
    ctor(IkonClientConfig config)
    Context? ClientContext { get; }
    IkonClientConfig Config { get; }
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    FunctionRegistry FunctionRegistry { get; }
    GlobalState? GlobalState { get; }
    ConnectionState State { get; }
    // Valid only from ConnectionState.Idle or ConnectionState.Offline; throws InvalidOperationException if already connecting or connected. On failure the client returns to ConnectionState.Offline and the exception is rethrown.
    Task ConnectAsync(CancellationToken ct = default)
    Task DisconnectAsync()
    ValueTask DisposeAsync()
    ValueTask SendAudioAsync(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null)
    // No-op (logs a warning) when not connected; never throws.
    ValueTask SendMessageAsync(ProtocolMessage message)
    // No-op (logs a warning) when not connected; never throws.
    ValueTask SendMessageAsync<T>(T payload) where T : IProtocolMessagePayload
    // Call once your setup completes, typically from the ReadyAsync handler. Throws if not connected.
    Task SignalReadyAsync()
    // Waits up to timeout (30 seconds when left as default) for a client matching productId/userId. Throws if not connected.
    Task<bool> WaitForClientAsync(string? productId = null, string? userId = null, TimeSpan timeout = default)
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputFrameEventArgs> AudioInputFrameAsync
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event IkonClient.AsyncEventHandler<IkonClient.AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    event IkonClient.AsyncEventHandler<EventArgs>? DisconnectedAsync
    event IkonClient.AsyncEventHandler<IkonClient.ErrorEventArgs>? ErrorOccurredAsync
    event IkonClient.AsyncEventHandler<MessageEventArgs>? MessageReceivedAsync
    event IkonClient.AsyncEventHandler<EventArgs>? ReadyAsync
    event IkonClient.AsyncEventHandler<IkonClient.ConnectionStateEventArgs>? StateChangedAsync
    event IkonClient.AsyncEventHandler<EventArgs>? StoppingAsync
  delegate IkonClient.AsyncEventHandler<in TEventArgs> where TEventArgs : EventArgs
    Task AsyncEventHandler<in TEventArgs>(TEventArgs e)
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
  class IkonClient.ConnectionStateEventArgs : EventArgs
    ctor(ConnectionState state)
    ConnectionState State { get; }
  class IkonClient.ErrorEventArgs : EventArgs
    ctor(Exception error)
    Exception Error { get; }
  // Exactly one authentication mode — ExternalConnectUrl, Local, ApiKey, or Backend — must be set; the constructor rejects zero or multiple.
  sealed record IkonClientConfig
    ctor()
    ApiKeyConfig? ApiKey { get; init; }
    BackendConfig? Backend { get; init; }
    // Default ContextType.Plugin connects as a backend component: no UI, no per-connection ClientScope. Set ContextType.Native (or ContextType.Browser) to connect as a first-class PLAYER client that receives a ClientScope and streamed UI, like the web client.
    ContextType ContextType { get; init; }
    string Description { get; init; }
    string? DeviceId { get; init; }
    bool EnableUdpChannel { get; init; }
    // When set, authentication is skipped and the client connects straight through this URL — the same mechanism the TypeScript SDK reads from its query parameter. Mutually exclusive with Local, ApiKey, and Backend.
    string? ExternalConnectUrl { get; init; }
    // Delivered to the app as Context.InitialPath at join, like a web client opening a deep link. Empty means the app's root.
    string InitialPath { get; init; }
    string? InstallId { get; init; }
    // Sets Context.IsSnapshot on the server so the app renders its privacy-safe snapshot variant. Only the build-time boot-snapshot capture client sets this; leave false otherwise.
    bool IsSnapshot { get; init; }
    LocalConfig? Local { get; init; }
    string Locale { get; init; }
    Opcode OpcodeGroupsFromServer { get; init; }
    Opcode OpcodeGroupsToServer { get; init; }
    Dictionary<string, string>? Parameters { get; init; }
    PayloadType PayloadType { get; init; }
    string? ProductId { get; init; }
    string SnapshotVariant { get; init; }
    TimeoutConfig Timeouts { get; init; }
    string? UserAgent { get; init; }
    UserLoginConfig? UserLogin { get; init; }
    string? VersionId { get; init; }
  sealed record LocalConfig
    ctor()
    string Host { get; init; }
    int HttpsPort { get; init; }
    string? UserId { get; init; }
  class MessageEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
  // One registry per IkonClient: construct it over client.FunctionRegistry after the connection is established, and call Detach on teardown. The current value is fetched on first subscribe and pushed by the server on every change — no polling.
  sealed class ReactiveRegistry
    ctor(FunctionRegistry functionRegistry)
    void Detach()
    Task<IAsyncDisposable> SubscribeAsync<T>(string stableId, Action<T> callback, string mountId = "", CancellationToken cancellationToken = default)
  sealed record TimeoutConfig
    ctor()
    bool BackgroundReconnect { get; init; }
    TimeSpan InitialReconnectDelay { get; init; }
    int MaxReconnectAttempts { get; init; }
    TimeSpan MaxReconnectDelay { get; init; }
    TimeSpan ReconnectAttemptTimeout { get; init; }
  sealed record UserLoginConfig
    ctor()
    ClientType ClientType { get; init; }
    string SpaceId { get; init; }
    UserType UserType { get; init; }
  static class Version
    const string VersionString
