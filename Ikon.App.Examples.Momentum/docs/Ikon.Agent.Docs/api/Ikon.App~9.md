namespace Ikon.App
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
    // extension methods: ThemeExtensions{ToThemeName}
  static class ThemeExtensions
    // False for the light theme, custom theme names, and clients that have not reported a theme.
    static bool IsDarkTheme(this Context clientContext)
    static string ToThemeName(this Theme theme)
  // Applying [Trigger] registers the method as a Local function — no [Function] needed. The handler takes no caller-supplied arguments; it may accept a TriggerContext and/or a CancellationToken that signals app shutdown, in any order. Any other parameter fails registration at startup. Delivery is at-least-once and durable: returning acknowledges the event, throwing leaves it pending for redelivery with backoff, and events never expire. Handlers must therefore be idempotent (TriggerContext.EventId is the dedup key) and should acknowledge within about two minutes, handing longer work to app.BackgroundWork. With MaxParallelism 1 (the default) events of one type arrive strictly in order. One listener per event type per app.
  sealed class TriggerAttribute : Attribute
    ctor(string eventType)
    // A TriggerEventType value such as TriggerEventType.EmailReceived. Checked at startup and at bundle time against the values this SDK knows; any other string fails both.
    string EventType { get; }
    // Default 1: events are delivered one at a time, in order. A higher value lets that many run concurrently and gives up ordering — right only for events that are independent of one another. Values below 1 fail registration and bundling.
    int MaxParallelism { get; init; }
    // When null or empty the function is registered (and invoked) under "{DeclaringType.FullName}.{Method}" — the identity the bundle manifest records, so the backend resolves it even when the method is inherited or overridden.
    string? Name { get; init; }
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
  // One instance per id in the erased account's identity closure, so a handler sees the same ErasureId several times for one erasure.
  class UserDataErasureEventArgs : EventArgs
    ctor(string userId, string erasureId)
    // Correlates the app's own record of the erasure with the platform's; it identifies the erasure, never the person.
    string ErasureId { get; }
    string UserId { get; }
  enum UserRole
    // Maps to the "anonymous" role string, not "guest"
    Guest
    User
    Moderator
    Admin
  // SendFrameAsync takes a MediaTargets first, so a call site always states who it reaches; there is no broadcast default.
  class Video
    ctor(IAppBase app)
    ValueTask CloseAllAsync()
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    // streamId: The stream id
    VideoOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // Frames are transmitted immediately — the caller owns the pacing. Call once per frame at the source framerate (typically forwarding each incoming frame as it arrives); never loop over a stored clip's frames without pacing.
    // data: Encoded video frame data
    // streamId: Required when several streams run at once; null uses the default stream
    // trackId: Overrides the track id assigned when the output stream is first created; ignored on later frames. Not needed to echo a client's stream — passing its input track id only lets a viewer's RequestIdrVideoFrame (which names the output track) be forwarded to the source unchanged
    ValueTask SendFrameAsync(MediaTargets targets, byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs, VideoCodec codec, int width, int height, double framerate, string? streamId = null, int? trackId = null)
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
