namespace Ikon.App
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
    Task<bool> RequestPendingAsync(int sessionId, CancellationToken ct = default)
    // sessionId: The client session that should record.
    // archiveId: Names the activity. The same id must be given to StopAsync, and it is what arrives back on RecordingArchive.ArchiveId. One id is one file, so starting and stopping repeatedly produces one archive per activity and never a blend of two.
    // options: What to record.
    Task<bool> StartAsync(int sessionId, string archiveId, RecordingOptions? options = null, CancellationToken ct = default)
    // sessionId: The client session that was recording.
    // archiveId: The id given to StartAsync.
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
    ctor(Transcript transcript, Context clientContext, string streamId, string? correlationId, TimeSpan duration, int sampleCount, int turnId = 0)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    TimeSpan Duration { get; }
    int SampleCount { get; }
    string StreamId { get; }
    string Text { get; }
    // The full result, including per-word and per-segment timings when Audio.UseSpeechRecognition or Audio.UseTurnDetection asked for them; its Transcript.Words and Transcript.Segments are empty otherwise. Offsets are relative to the start of the recognized segment, not of the stream.
    Transcript Transcript { get; }
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
  // A null targetIds broadcasts to every connected client; a list restricts delivery to those client session ids.
  class Video
    ctor(IAppBase app)
    ValueTask CloseAllAsync()
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    VideoOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Frames are transmitted immediately — the caller owns the pacing. Call once per frame at the source framerate (typically forwarding each incoming frame as it arrives); never loop over a stored clip's frames without pacing.
    // data: Encoded video frame data
    // durationInUs: Frame duration in microseconds
    // width: Video width in pixels
    // height: Video height in pixels
    // streamId: Optional id to distinguish between multiple concurrent video streams. Required when sending multiple streams simultaneously
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
