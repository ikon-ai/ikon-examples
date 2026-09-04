namespace Ikon.Common.Core.Protocol
  // Shared state synchronized across all clients and the server, providing access to connected clients, registered functions, active media streams, and session metadata
  sealed class GlobalState : ILogInfo, IProtocolMessagePayload
    ctor()
    ctor(Dictionary<int, Context> clients, Dictionary<int, List<ActionFunctionRegister>> functions, Dictionary<string, GlobalState.UIStreamState> uiStreams, Dictionary<string, GlobalState.AudioStreamState> audioStreams, Dictionary<string, GlobalState.VideoStreamState> videoStreams, string spaceId, string ikonServerId, string appSessionId, string sessionIdentityHash, string spaceUrl, string sessionUrl, string firstUserId, string primaryUserId, string organisationName, string spaceName, ServerRunType serverRunType, bool publicAccess, bool debugMode)
    // Unique identifier of the app session this server is serving — the session's business identity, stable across the servers that run it. Empty outside a cloud run.
    string AppSessionId { get; set; }
    // Active audio streams indexed by stream ID
    Dictionary<string, GlobalState.AudioStreamState> AudioStreams { get; set; }
    // All connected clients indexed by their client session ID, containing client metadata such as user ID, device info, viewport dimensions, and locale
    Dictionary<int, Context> Clients { get; set; }
    // Whether debug mode is enabled, providing additional logging and development features
    bool DebugMode { get; set; }
    // User ID of the first human user who joined this session, dynamically reassigned when that user leaves
    string FirstUserId { get; set; }
    // Registry of callable functions organized by client session ID
    Dictionary<int, List<ActionFunctionRegister>> Functions { get; set; }
    // Unique identifier of the specific Ikon server instance handling this session
    string IkonServerId { get; set; }
    object LogInfo { get; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    // Display name of the organization
    string OrganisationName { get; set; }
    // Static user ID of the session owner from server configuration, used for user-specific asset storage paths
    string PrimaryUserId { get; set; }
    // Tells whether the app is being run through publicly accessible endpoints (in local development)
    bool PublicAccess { get; set; }
    // Tells where the server is running from
    ServerRunType ServerRunType { get; set; }
    // Hash of the session identity values that this session was joined on
    string SessionIdentityHash { get; set; }
    // Full URL with session identifier for direct access to current session
    string SessionUrl { get; set; }
    // Unique identifier for the space where this session is running
    string SpaceId { get; set; }
    // Display name of the space
    string SpaceName { get; set; }
    // URL for accessing the app through its space domain
    string SpaceUrl { get; set; }
    // Active UI streams indexed by stream ID
    Dictionary<string, GlobalState.UIStreamState> UIStreams { get; set; }
    // Active video streams indexed by stream ID
    Dictionary<string, GlobalState.VideoStreamState> VideoStreams { get; set; }
    void AddAudioStream(GlobalState.AudioStreamState audioStreamState)
    void AddClient(Context clientContext)
    void AddFunction(int clientSessionId, ActionFunctionRegister function)
    void AddUIStream(GlobalState.UIStreamState uiStreamState)
    void AddVideoStream(GlobalState.VideoStreamState videoStreamState)
    void CopyRetiredFieldsFrom(GlobalState source)
    Context? GetClientContext(int clientSessionId)
    // Returns the context of the first connected client of this user, or null when the user has no connected client
    Context? GetClientContext(string userId)
    // Returns the session id of the first connected client of this user, or 0 when the user has no connected client. Check for 0 before targeting the result, or use GetClientContext, which returns null instead of a sentinel
    int GetClientSessionId(string userId)
    int[] GetClientSessionIds()
    int[] GetClientSessionIdsByProductId(string productId)
    int[] GetClientSessionIdsExcept(int[] clientSessionIds)
    int[] GetHumanClientSessionIds()
    int[] GetMachineClientSessionIds()
    GlobalState.RetiredFields GetOrCreateRetiredFields()
    GlobalState.RetiredFields? GetRetiredFields()
    // Returns null (not an empty list) when none of targetIds is connected; ids that name no connected client are skipped, so the result can be shorter than the input
    List<string>? GetUserIds(IEnumerable<int> targetIds)
    void RemoveAudioStream(string streamId)
    void RemoveClient(int clientSessionId)
    void RemoveFunction(Guid functionId)
    void RemoveUIStream(string streamId)
    void RemoveVideoStream(string streamId)
    void SetReady(int clientSessionId)
    void SetReconnected(int clientSessionId)
    void SetSoftDisconnected(int clientSessionId, ulong softDisconnectAt)
    override string ToString()
    static readonly IReadOnlyList<string> RetiredKeys
  sealed class GlobalState.AudioStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, AudioStreamBegin info)
    // Session ID of the client sending this stream
    int ClientSessionId { get; set; }
    // Stream configuration including codec, sample rate, and channels
    AudioStreamBegin Info { get; set; }
    // Unique identifier for this stream
    string StreamId { get; set; }
    // Track identifier within the client
    int TrackId { get; set; }
  sealed class GlobalState.RetiredFields
    ctor()
    // TODO(channel-compat): back-compat mirror of SpaceUrl for pre-channel-removal clients that still read the old field name. The server writes it via the retired bag with the SpaceUrl value; stop writing once no such old clients remain.
    string? ChannelUrl { get; set; }
    // Renamed to IkonServerId; the server writes it via the retired bag with the new field's value so clients built before the rename still resolve it. Stop writing once no frozen app bundle or cached SDK build in circulation reads it.
    string? ServerSessionId { get; set; }
    // TODO(channel-compat): back-compat mirror of SessionUrl, same lifecycle as ChannelUrl above.
    string? SessionChannelUrl { get; set; }
    // Renamed to SessionIdentityHash, same lifecycle as ServerSessionId above.
    string? SessionHash { get; set; }
  sealed class GlobalState.UIStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, UIStreamBegin info)
    // Session ID of the client sending this stream
    int ClientSessionId { get; set; }
    // Stream configuration including category and metadata
    UIStreamBegin Info { get; set; }
    // Unique identifier for this stream
    string StreamId { get; set; }
    // Track identifier within the client
    int TrackId { get; set; }
  sealed class GlobalState.VideoStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, VideoStreamBegin info)
    // Session ID of the client sending this stream
    int ClientSessionId { get; set; }
    // Stream configuration including codec and resolution
    VideoStreamBegin Info { get; set; }
    // Unique identifier for this stream
    string StreamId { get; set; }
    // Track identifier within the client
    int TrackId { get; set; }
  interface IProtocolMessagePayload
    virtual MessageFlag MessageDefaultFlags { get; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
  enum LogType
    None
    Trace
    Debug
    Info
    Warning
    Error
    Critical
    Event
    Usage
    Exception
  enum MessageFlag
    None
    SendBackToSender
    Delayable
    SendToUser
    Compressed
    Unreliable
  enum Opcode
    NONE
    CONSTANT_GROUP_BITS
    CONSTANT_GROUP_OFFSET
    GROUP_CORE
    CORE_AUTH_RESPONSE
    CORE_AUTH_TICKET
    CORE_GLOBAL_STATE
    CORE_ON_SERVER_STATUS_PING
    CORE_ON_USER_JOINED
    CORE_ON_USER_LEFT
    CORE_ON_CLIENT_JOINED
    CORE_ON_CLIENT_LEFT
    CORE_ON_SERVER_STARTED
    CORE_ON_SERVER_STOPPED
    CORE_ON_SERVER_STOPPING
    CORE_ON_CLIENT_READY
    CORE_CLIENT_READY
    CORE_SERVER_INIT
    CORE_ON_PLUGIN_RELOADED
    CORE_DYNAMIC_CONFIG
    CORE_PROXY_RPC_AUTH_TICKET
    CORE_UPDATE_CLIENT_CONTEXT
    CORE_BACKGROUND_WORK_ACTIVE
    CORE_RESET_IDLE
    CORE_CLIENT_DISCONNECTING
    CORE_ON_APP_READY
    CORE_ON_FRONTEND_RELOADED
    CORE_ON_USER_DATA_ERASED
    CORE_WEBRTC_OFFER
    CORE_WEBRTC_ANSWER
    CORE_WEBRTC_ICE_CANDIDATE
    CORE_WEBRTC_READY
    CORE_WEBRTC_AUDIO_SEGMENT
    CORE_WEBRTC_TRACK_MAP
    CORE_WEBRTC_VIDEO_CAPTURE
    CORE_WEBRTC_ICE_SERVERS_REQUEST
    CORE_WEBRTC_ICE_SERVERS_RESPONSE
    CORE_WEBRTC_CLOSE
    CORE_RELAY_AGENT_AUTH
    CORE_RELAY_AGENT_AUTH_RESULT
    CORE_RELAY_HEARTBEAT
    CORE_RELAY_TCP_CONNECTION_OPENED
    CORE_RELAY_TCP_CONNECTION_CLOSED
    CORE_RELAY_TCP_DATA
    CORE_RELAY_UDP_DATA
    CORE_RELAY_ADD_TUNNEL
    CORE_RELAY_TUNNEL_ADDED
    CORE_RELAY_REMOVE_TUNNEL
    CORE_IKON_SERVER_ENDPOINT_HOST_INFO
    CORE_CLIENT_INITIALIZATION
    CORE_CLIENT_LIFECYCLE_BATCH
    CORE_APP_CONFIG
    GROUP_KEEPALIVE
    KEEPALIVE_REQUEST
    KEEPALIVE_RESPONSE
    GROUP_EVENTS
    EVENTS_PROFILE_UPDATE
    EVENTS_SPEECH_PLAYBACK_COMPLETE
    GROUP_ANALYTICS
    ANALYTICS_LOGS
    ANALYTICS_EVENTS
    ANALYTICS_USAGES
    ANALYTICS_USAGE
    ANALYTICS_SPECIAL_LOG
    ANALYTICS_PROCESSING_UPDATE
    ANALYTICS_REACTIVE_PROCESSING_UPDATE
    ANALYTICS_IKON_PROXY_SERVER_STATS
    ANALYTICS_IKON_RELAY_SERVER_STATS
    ANALYTICS_IKON_TURN_SERVER_STATS
    ANALYTICS_IKON_HOST_SERVER_STATS
    ANALYTICS_TRAFFIC_USAGE
    GROUP_ACTIONS
    ACTION_CALL
    ACTION_ACTIVE
    ACTION_TEXT_OUTPUT
    ACTION_TEXT_OUTPUT_DELTA
    ACTION_TEXT_OUTPUT_DELTA_FULL
    ACTION_SET_STATE
    ACTION_TAP
    ACTION_PAN
    ACTION_ZOOM
    ACTION_OPEN_EXTERNAL_URL
    ACTION_FUNCTION_REGISTER
    ACTION_FUNCTION_CALL
    ACTION_FUNCTION_RESULT
    ACTION_GENERATE_ANSWER
    ACTION_REGENERATE_ANSWER
    ACTION_CLEAR_STATE
    ACTION_CLASSIFICATION_RESULT
    ACTION_AUDIO_STOP
    ACTION_CALL_TEXT
    ACTION_CANCEL_GENERATION
    ACTION_SPEECH_RECOGNIZED
    ACTION_CALL_RESULT
    ACTION_DOWNLOAD
    ACTION_PLAY_SOUND
    ACTION_STOP_SOUND
    ACTION_START_RECORDING
    ACTION_STOP_RECORDING
    ACTION_FUNCTION_ENUMERATION_ITEM
    ACTION_FUNCTION_ENUMERATION_END
    ACTION_FUNCTION_CANCEL
    ACTION_FUNCTION_DISPOSE
    ACTION_FUNCTION_ERROR
    ACTION_FUNCTION_ACK
    ACTION_FUNCTION_AWAITING_APPROVAL
    ACTION_FUNCTION_APPROVAL_REQUIRED
    ACTION_FUNCTION_APPROVAL_RESPONSE
    UI_UPDATE_ACK
    ACTION_CALL2
    ACTION_FUNCTION_REGISTER_BATCH
    ACTION_CUSTOM_USER_MESSAGE
    ACTION_URL_CHANGED
    ACTION_FILE_UPLOAD_PRE_START2
    ACTION_FILE_UPLOAD_PRE_START_RESPONSE2
    ACTION_FILE_UPLOAD_START2
    ACTION_FILE_UPLOAD_START_RESPONSE2
    ACTION_FILE_UPLOAD_DATA2
    ACTION_FILE_UPLOAD_ACK2
    ACTION_FILE_UPLOAD_END2
    ACTION_FILE_UPLOAD_COMPLETE2
    ACTION_FUNCTION_ENUMERATION_ITEM_BATCH
    ACTION_CALL_ACK
    ACTION_TRIGGER_CRON
    ACTION_RESULT
    UI_RESYNC_REQUEST
    ACTION_USER_DATA_ERASURE
    ACTION_FILE_UPLOAD_RESUME2
    ACTION_FILE_UPLOAD_RESUME_RESPONSE2
    GROUP_UI
    UI_STREAM_BEGIN
    UI_STREAM_END
    UI_STYLES
    UI_UPDATE
    UI_STYLES_BATCH
    UI_STYLES_DELETE
    GROUP_COMMON
    GROUP_AUDIO
    AUDIO_STREAM_BEGIN
    AUDIO_STREAM_END
    AUDIO_FRAME_VOLUME
    AUDIO_FRAME
    AUDIO_SHAPE_FRAME
    AUDIO_PLAYBACK_REPORT
    GROUP_VIDEO
    VIDEO_STREAM_BEGIN
    VIDEO_STREAM_END
    VIDEO_FRAME
    VIDEO_REQUEST_IDR_FRAME
    VIDEO_INVALIDATE_FRAME
    GROUP_ALL
    GROUP_APP_LOCAL
    CONSTANT_GROUP_MASK
  static class Opcodes
    static bool IsOpcodeInAnyGroup(Opcode opcode, Opcode groups)
  enum PayloadType
    Unknown
    MessagePack
    MemoryPack
    Json
    Teleport
    All
  class ProtocolMessage : AsyncLocalInstance<ProtocolMessage>
    ctor()
    // Wraps the buffer WITHOUT copying — data is aliased and every accessor reads straight from it. The caller must keep the buffer alive and unchanged for the lifetime of this message; a reused or overwritten buffer makes it silently read corrupted data. Use CopyFrom when the source buffer will be reused.
    ctor(Memory<byte> data)
    Memory<byte> Data { get; }
    MessageFlag Flags { get; }
    int Length { get; }
    Opcode Opcode { get; }
    Memory<byte> Payload { get; }
    Span<byte> PayloadSpan { get; }
    PayloadType PayloadType { get; }
    int PayloadVersion { get; }
    int SenderId { get; }
    int SequenceId { get; }
    string StreamId { get; }
    int TargetIdCount { get; }
    int[] TargetIds { get; }
    ReadOnlySpan<int> TargetIdsSpan { get; }
    int TrackId { get; }
    // Copies data, so the caller may reuse, return, or overwrite the source buffer immediately. Prefer this over the aliasing #ctor constructor whenever the source is a pooled or otherwise reused buffer.
    static ProtocolMessage CopyFrom(ReadOnlySpan<byte> data)
    static ProtocolMessage Create(int senderId, IProtocolMessagePayload payload, PayloadType payloadType = Unknown, int trackId = 0, int sequenceId = 0, MessageFlag flags = None, IReadOnlyList<int>? targetIds = null, bool compress = false)
    T GetPayload<T>() where T : IProtocolMessagePayload
    IProtocolMessagePayload GetPayload()
    static ProtocolMessage ModifyMessage(ProtocolMessage message, int? senderId = null, int? trackId = null, int? sequenceId = null, MessageFlag? flags = null, IReadOnlyList<int>? targetIds = null)
    static ProtocolMessage ModifyPayload(IProtocolMessagePayload payload, ProtocolMessage message, PayloadType payloadType = Unknown)
    // Registers an app-local message type (an app's own schema/*.tp type, opcode in Opcode.GROUP_APP_LOCAL) at runtime. Called from the generated type's static constructor — app-local types are compiled into the app assembly and are not visible to the platform's compile-time ProtocolMessage source generator.
    static void RegisterAppLocalMessageType(Type type, Opcode opcode, int version)
    override string ToString()
    static ProtocolMessage WithFlags(ProtocolMessage message, MessageFlag additionalFlags)
    PayloadType DefaultPayloadType
    const int MaxMessageSize = 20971520
    const int MinimumHeaderLength = 27
    static readonly Dictionary<Opcode, Type> OpcodeToType
    static readonly Dictionary<Type, Opcode> TypeToOpcode
    static readonly Dictionary<Type, int> TypeToVersion
  class ProtocolMessageAttribute : Attribute
    ctor(int version = 0, Opcode opcode = NONE, bool unreliable = false)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    bool Unreliable { get; }
  static class ProtocolVersion
    static int Version { get; }
  sealed class RouteToken : IProtocolMessagePayload
    ctor()
    ctor(uint expiresAt, string host, int httpsPort, int tlsPort)
    // Epoch seconds after which the gateway refuses the token. Always set — this one leaves the machine, and unlike the ConnectToken it authorizes a dial target rather than a session, so it is minted short-lived.
    uint ExpiresAt { get; set; }
    // The ikon server's hostname, as the gateway will dial and TLS-validate it. A fleet instance presents the platform wildcard certificate, so this must be the real FQDN and never an address.
    string Host { get; set; }
    // Both upstream ports, because the two legs land on different ones: /connect is HTTPS to HttpsPort, and the channel is a raw TCP socket to TlsPort. HostAgent allocates them separately from one range, so neither can be derived from the other.
    int HttpsPort { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int TlsPort { get; set; }
  enum SdkType
    Unknown
    DotNet
    TypeScript
    Cpp
    Dart
    Rust
  enum ServerRunType
    Local
    Cloud
  enum ServerStatus
    Unknown
    Starting
    Running
    Stopping
    Stopped
  enum StyleFormat
    Css
    Flutter
  static class UIElementLabels
    const string Blur
    const string ChatMessage
    const string Disabled
    const string ImageAvatar
    const string Markdown
    const string SizeExtraSmall
    const string SizeFitContent
    const string SizeFullWidth
    const string SizeLarge
    const string SizeMedium
    const string SizeSmall
    const string Wrap
  sealed class UIStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string category)
    string Category { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
  static class UIStreamCategories
    const string App
    const string Chat
    const string Collapsed
    const string DebugOverlay
    const string Footer
    const string Header
    const string Input
    const string Menu
    const string Overlay
    const string Preview
    const string SecondScreen
  static class UIStylesKeys
    const string Common
    const string Crosswind
    const string Css
    const string Flutter
    const string ReactNative
  enum UserType
    Unknown
    Machine
    Human
  enum VideoCodec
    Unknown
    H264
    Vp8
    Vp9
    Av1
  sealed class VideoStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string streamId, string description, string sourceType, VideoCodec codec, string codecDetails, int width, int height, double framerate, string? correlationId)
    VideoCodec Codec { get; set; }
    string CodecDetails { get; set; }
    string? CorrelationId { get; set; }
    string Description { get; set; }
    double Framerate { get; set; }
    int Height { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string SourceType { get; set; }
    string StreamId { get; set; }
    int Width { get; set; }
