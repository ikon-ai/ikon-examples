namespace Ikon.App
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
    // content: Title, body, launch url, tag and data, as for NotificationService.
    // kind: App-defined category stored on the item for filtering.
    // route: Where to deliver; NotificationRoute.Default is inbox plus push.
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
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    // sessionId: The target client session id.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    // sessionId: The target client session id.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // Returns one result per connected session for the user. An empty list means the user had no connected session and only offline push was attempted — it is not an error.
    // userId: The persistent user id to notify.
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = default)
    // userId: The persistent user id to notify.
    // content: The notification content. Give it a NotificationContent.Tag so a device that is both connected and pushed shows one notification, not two.
    // reach: How many of the user's devices to reach.
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
