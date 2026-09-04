namespace Ikon.Common.Core.Protocol
  sealed class ActionFunctionRegister : IProtocolMessagePayload
    ctor()
    ctor(Guid functionId, string functionName, List<ActionFunctionRegister.FunctionRegisterParameter> parameters, string resultTypeName, bool isEnumerable, string enumerableItemTypeName, bool isCancellable, string description, bool llmInlineResult, bool llmCallOnlyOnce, bool requiresInstance, List<string> versions)
    string Description { get; set; }
    string EnumerableItemTypeName { get; set; }
    Guid FunctionId { get; set; }
    string FunctionName { get; set; }
    bool IsCancellable { get; set; }
    bool IsEnumerable { get; set; }
    bool LlmCallOnlyOnce { get; set; }
    bool LlmInlineResult { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    List<ActionFunctionRegister.FunctionRegisterParameter> Parameters { get; set; }
    bool RequiresInstance { get; set; }
    string ResultTypeName { get; set; }
    List<string> Versions { get; set; }
    static ActionFunctionRegister.FunctionRegisterParameter CreateParameter(int parameterIndex, string parameterName, Type clrType, bool hasDefaultValue, object? defaultValue, bool isEnumerable, string enumerableItemTypeName, string description)
  sealed class ActionFunctionRegister.FunctionRegisterParameter
    ctor()
    ctor(int parameterIndex, string parameterName, string typeName, bool hasDefaultValue, string defaultValueJson, byte[] defaultValueData, bool isEnumerable, string enumerableItemTypeName, string description)
    byte[] DefaultValueData { get; set; }
    string DefaultValueJson { get; set; }
    string Description { get; set; }
    string EnumerableItemTypeName { get; set; }
    bool HasDefaultValue { get; set; }
    bool IsEnumerable { get; set; }
    int ParameterIndex { get; set; }
    string ParameterName { get; set; }
    string TypeName { get; set; }
  enum AudioCodec
    Unknown
    Opus
    Mp3
    RawPcm16
  enum AudioPlaybackState
    Unknown
    Playing
    Blocked
    Hidden
  sealed class AudioStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string streamId, string description, string sourceType, AudioCodec codec, string codecDetails, int sampleRate, int channels, List<AudioStreamBegin.AudioShapeSet>? shapeSets, string? correlationId)
    int Channels { get; set; }
    AudioCodec Codec { get; set; }
    string CodecDetails { get; set; }
    string? CorrelationId { get; set; }
    string Description { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SampleRate { get; set; }
    List<AudioStreamBegin.AudioShapeSet>? ShapeSets { get; set; }
    string SourceType { get; set; }
    string StreamId { get; set; }
  sealed class AudioStreamBegin.AudioShapeSet
    ctor()
    ctor(uint setId, string name, List<string> shapeNames)
    string Name { get; set; }
    uint SetId { get; set; }
    List<string> ShapeNames { get; set; }
  sealed class AuthResponse : IProtocolMessagePayload
    ctor()
    ctor(Context clientContext, Context serverContext, string certHash, List<Entrypoint> entrypoints, Dictionary<string, bool> featureFlags, string spaceId, string appSessionId, string ikonServerId, string primaryUserId, int keepaliveTimeoutMs, int serverCapability, int softRetryWindowMs)
    // The app session this server was provisioned for — the session's business identity, stable across the servers that run it.
    string AppSessionId { get; set; }
    string CertHash { get; set; }
    Context ClientContext { get; set; }
    List<Entrypoint> Entrypoints { get; set; }
    Dictionary<string, bool> FeatureFlags { get; set; }
    // The provisioned server instance serving this connection.
    string IkonServerId { get; set; }
    int KeepaliveTimeoutMs { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PrimaryUserId { get; set; }
    int ServerCapability { get; set; }
    Context ServerContext { get; set; }
    // How long a dropped client's session and auth ticket stay resumable, in milliseconds. A reconnect within this window may retry soft (reusing the ticket); past it the server has let the session go. 0 means the server predates the field; clients fall back to their built-in window.
    int SoftRetryWindowMs { get; set; }
    string SpaceId { get; set; }
    void CopyRetiredFieldsFrom(AuthResponse source)
    AuthResponse.RetiredFields GetOrCreateRetiredFields()
    AuthResponse.RetiredFields? GetRetiredFields()
    static readonly IReadOnlyList<string> RetiredKeys
  sealed class AuthResponse.RetiredFields
    ctor()
    // Renamed to IkonServerId. Teleport matches fields by name hash, so the server keeps writing this via the retired bag with the IkonServerId value until no frozen app bundle or cached SDK build in circulation still reads it.
    string? ServerSessionId { get; set; }
  sealed class ClientEnvironment : IProtocolMessagePayload
    ctor()
    ctor(string description, string deviceId, string productId, string versionId, string installId, string locale, string userAgent, ClientType clientType, SdkType sdkType, int sdkCapability, int protocolVersion, PayloadType payloadType, StyleFormat styleFormat, bool supportsCompression, bool hasInput, bool receiveAllMessages, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, string initialUrl, bool isSnapshot, string snapshotVariant)
    ClientType ClientType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    // Absolute URL the browser loaded, ikon-* query params stripped; copied into Context.InitialUrl. Empty for every non-browser client.
    string InitialUrl { get; set; }
    string InstallId { get; set; }
    // True for the build-time snapshot-capture client; copied into Context.IsSnapshot. Identifies the client whose initial UI is baked into boot-snapshot.json. Inert beyond identification in v1.
    bool IsSnapshot { get; set; }
    bool IsTouchDevice { get; set; }
    string Locale { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    PayloadType PayloadType { get; set; }
    string ProductId { get; set; }
    int ProtocolVersion { get; set; }
    bool ReceiveAllMessages { get; set; }
    // Opaque, monotonically-increasing capability level advertised by the connecting SDK (companion to SdkType). 0 = legacy/unknown. A client can only misreport this about itself: claiming a level it does not have makes the server send messages it cannot handle.
    int SdkCapability { get; set; }
    SdkType SdkType { get; set; }
    // Boot-snapshot variant id the capture client asks the app to render (a skeleton keyed by the [BootSnapshot] seed rules); empty for route captures and all live clients. Copied into Context.SnapshotVariant. Client-controlled like IsSnapshot — must never gate anything security-relevant.
    string SnapshotVariant { get; set; }
    StyleFormat StyleFormat { get; set; }
    bool SupportsCompression { get; set; }
    string Theme { get; set; }
    string Timezone { get; set; }
    string UserAgent { get; set; }
    string VersionId { get; set; }
    int ViewportHeight { get; set; }
    int ViewportWidth { get; set; }
  sealed class ClientInitialization : IProtocolMessagePayload
    ctor()
    ctor(Dictionary<int, List<ActionFunctionRegister>> functions)
    Dictionary<int, List<ActionFunctionRegister>> Functions { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
  enum ClientType
    Unknown
    MobileWeb
    MobileApp
    DesktopWeb
    DesktopApp
  sealed class ConnectToken : IProtocolMessagePayload
    ctor()
    ctor(uint expiresAt, ContextType contextType, UserType userType, bool isInternal, string userId, string authSessionId, bool isAnonymous, bool isGlobal, Opcode opcodeGroupsFromServer, Opcode opcodeGroupsToServer, Dictionary<string, string> parameters, string serverSessionId, string description, string deviceId, string productId, string versionId, string installId, string locale, string userAgent, ClientType clientType, SdkType sdkType, int sdkCapability, int protocolVersion, PayloadType payloadType, StyleFormat styleFormat, bool supportsCompression, bool hasInput, bool receiveAllMessages, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, string initialUrl, bool isSnapshot, string snapshotVariant)
    string AuthSessionId { get; set; }
    ClientType ClientType { get; set; }
    ContextType ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    // Epoch seconds after which the server refuses this token. 0 means no timed expiry, which is only appropriate when the signing process also owns the verifying server and randomizes the secret at startup — the token then dies with the process. Every minted token that leaves the machine sets it.
    uint ExpiresAt { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    // Absolute URL the browser loaded, ikon-* query params stripped; copied into Context.InitialUrl. Empty for every non-browser client. Client-supplied like InitialPath — never authoritative.
    string InitialUrl { get; set; }
    string InstallId { get; set; }
    // True when the user is anonymous (guest login or no login): a device-scoped identity rather than a real account. Filled by the backend from the user's role; copied into Context.IsAnonymous. AuthSessionId cannot express this — cloud logins (including anonymous ones) always carry a session id, so "has a session" does not mean "has an account".
    bool IsAnonymous { get; set; }
    // True when the anonymous user is the space's GLOBAL communal identity (the "global" login method) — every global visitor shares one UserId. Always false for device-scoped guests and signed-in users; implies IsAnonymous. Filled by the backend from the minted flavor; copied into Context.IsGlobal.
    bool IsGlobal { get; set; }
    bool IsInternal { get; set; }
    // True for the build-time snapshot-capture client; copied into Context.IsSnapshot. Identifies the client whose initial UI is baked into boot-snapshot.json. Inert beyond identification in v1.
    bool IsSnapshot { get; set; }
    bool IsTouchDevice { get; set; }
    string Locale { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Opcode OpcodeGroupsFromServer { get; set; }
    Opcode OpcodeGroupsToServer { get; set; }
    // Launch parameters. Signed, and NOT part of the removable block below, because Studio mints the preview inspector by putting `ikon-inspect` here — a client that could assert its own parameters would hand itself the inspector.
    Dictionary<string, string> Parameters { get; set; }
    PayloadType PayloadType { get; set; }
    string ProductId { get; set; }
    int ProtocolVersion { get; set; }
    bool ReceiveAllMessages { get; set; }
    // Opaque, monotonically-increasing capability level advertised by the connecting SDK (companion to SdkType). 0 = legacy/unknown. Threaded SDK connect-request -> backend -> ConnectToken -> ikon server -> client Context.
    int SdkCapability { get; set; }
    SdkType SdkType { get; set; }
    string ServerSessionId { get; set; }
    // Boot-snapshot variant id the capture client asks the app to render (a skeleton keyed by the [BootSnapshot] seed rules); empty for route captures and all live clients. Copied into Context.SnapshotVariant. Client-controlled like IsSnapshot — must never gate anything security-relevant.
    string SnapshotVariant { get; set; }
    StyleFormat StyleFormat { get; set; }
    bool SupportsCompression { get; set; }
    string Theme { get; set; }
    string Timezone { get; set; }
    string UserAgent { get; set; }
    string UserId { get; set; }
    UserType UserType { get; set; }
    string VersionId { get; set; }
    int ViewportHeight { get; set; }
    int ViewportWidth { get; set; }
    override string ToString()
  sealed class Context : IProtocolMessagePayload
    ctor()
    ctor(ContextType contextType, UserType userType, PayloadType payloadType, string description, string userId, string deviceId, string productId, string versionId, string installId, string locale, int sessionId, bool isInternal, bool isSnapshot, string snapshotVariant, bool isReady, bool hasInput, string authSessionId, bool isAnonymous, bool isGlobal, bool receiveAllMessages, ulong preciseJoinedAt, string userAgent, ClientType clientType, string uniqueSessionId, Dictionary<string, string> parameters, SdkType sdkType, int sdkCapability, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, string initialUrl, StyleFormat styleFormat, bool supportsCompression, bool isSoftDisconnected, ulong softDisconnectAt)
    string AuthSessionId { get; set; }
    // Alias for SessionId.
    int ClientSessionId { get; }
    ClientType ClientType { get; set; }
    ContextType ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    // Copied from ConnectToken.InitialUrl — the absolute URL the browser loaded, so an app can see the host it was reached on (a custom customer domain) and not just the path. A strict superset of InitialPath: same SDK-internal ikon-* query params stripped, everything else kept, so InitialUrl.PathAndQuery reproduces InitialPath. Empty for every non-browser client, so read it as an addition to InitialPath rather than a replacement. Client-controlled like InitialPath — treat it as a hint and re-authorize server-side; it must never gate anything security-relevant on its own.
    string InitialUrl { get; set; }
    string InstallId { get; set; }
    // Copied from ConnectToken.IsAnonymous — true when the user is anonymous (guest login or no login): a device-scoped identity rather than a real account. The authoritative signal for guest detection; AuthSessionId is a login-session identifier, not an authentication flag.
    bool IsAnonymous { get; set; }
    // Copied from ConnectToken.IsGlobal — true when this anonymous user is the space's GLOBAL communal identity (the "global" login method), where every global visitor shares one UserId. Always false for device-scoped guests and signed-in users; implies IsAnonymous. Lets an app offer "continue as guest" or per-visitor features only where they make sense.
    bool IsGlobal { get; set; }
    bool IsInternal { get; set; }
    bool IsReady { get; set; }
    bool IsSharedSession { get; }
    // Copied from ConnectToken.IsSnapshot — marks the build-time snapshot-capture client.
    bool IsSnapshot { get; set; }
    bool IsSoftDisconnected { get; set; }
    bool IsTouchDevice { get; set; }
    string Locale { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Dictionary<string, string> Parameters { get; set; }
    PayloadType PayloadType { get; set; }
    ulong PreciseJoinedAt { get; set; }
    string ProductId { get; set; }
    bool ReceiveAllMessages { get; set; }
    // Opaque, monotonically-increasing capability level advertised by the connecting SDK (companion to SdkType). 0 = legacy/unknown. Copied from ConnectToken.SdkCapability when the server builds the client Context.
    int SdkCapability { get; set; }
    SdkType SdkType { get; set; }
    int SessionId { get; set; }
    // Copied from ConnectToken.SnapshotVariant — the boot-snapshot variant id the capture client asks the app to render; empty for route captures and all live clients. Client-controlled like IsSnapshot — must never gate anything security-relevant.
    string SnapshotVariant { get; set; }
    ulong SoftDisconnectAt { get; set; }
    StyleFormat StyleFormat { get; set; }
    bool SupportsCompression { get; set; }
    string Theme { get; set; }
    string Timezone { get; set; }
    string UniqueSessionId { get; set; }
    string UserAgent { get; set; }
    string UserId { get; set; }
    UserType UserType { get; set; }
    string VersionId { get; set; }
    int ViewportHeight { get; set; }
    int ViewportWidth { get; set; }
    override string ToString()
    // Set Parameters["ikon-shared-session"] = "true" on a connect token minted by a trusted host and every channel that later presents the session's auth ticket joins it instead of taking it over: all of them stay connected and each receives the server's messages for the session.
    const string SharedSessionParameter
  enum ContextType
    Unknown
    Backend
    Server
    Plugin
    Browser
    Native
  sealed class Entrypoint : IProtocolMessagePayload
    ctor()
    ctor(EntrypointType type, string uri, Opcode opcodeGroupsFromServer, Opcode opcodeGroupsToServer, int priority, string description, byte[] authTicket, bool isUnreliable)
    byte[] AuthTicket { get; set; }
    string Description { get; set; }
    bool IsUnreliable { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Opcode OpcodeGroupsFromServer { get; set; }
    Opcode OpcodeGroupsToServer { get; set; }
    int Priority { get; set; }
    EntrypointType Type { get; set; }
    string Uri { get; set; }
    override string ToString()
  enum EntrypointType
    None
    WebSocket
    WebSocketProxy
    WebTransport
    WebTransportProxy
    Tcp
    TcpProxy
    Https
    WebRTC
    TcpTls
    Udp
    UdpDtls
    HttpStream
    HttpStreamProxy
  sealed class FunctionParameter : IProtocolMessagePayload
    ctor()
    ctor(int parameterIndex, string typeName, string valueJson, byte[] valueData, bool isEnumerable, string enumerableItemTypeName, Guid enumerationId, byte[] valueTeleport)
    string EnumerableItemTypeName { get; set; }
    Guid EnumerationId { get; set; }
    bool IsEnumerable { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int ParameterIndex { get; set; }
    string TypeName { get; set; }
    byte[] ValueData { get; set; }
    string ValueJson { get; set; }
    byte[] ValueTeleport { get; set; }
