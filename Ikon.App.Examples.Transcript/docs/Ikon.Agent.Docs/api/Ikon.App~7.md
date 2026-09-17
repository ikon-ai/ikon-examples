namespace Ikon.App
  class Navigation
    // Query string stripped; null outside a client scope or before any path is known. Tracked before the client's first frame renders, so route-dependent server UI can branch on it from the very first render — unlike state set from joined handlers, which run on a background task and can lose the race against the first frame.
    string? CurrentPath { get; }
    // Round-trips to the live client over the connection rather than reading server state; returns null when the client doesn't answer or isn't connected.
    // targetId: Session id of the client to ask
    Task<string?> GetPathAsync(int targetId)
    // Acts on the client of the ambient ClientScope — call from a client-scoped context; outside one it throws InvalidOperationException (same as SetPathAsync). Returns null when the client doesn't answer.
    // throws InvalidOperationException: No ClientScope is active
    Task<string?> GetPathAsync()
    // Rejects paths under the platform-reserved /ikon and /api prefixes (throws ArgumentException) — the load balancer owns those. The client's existing query string is preserved unless path carries its own. A client that does not acknowledge returns false and leaves CurrentPath where it was.
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
    // Fires only for a move the client makes on its own — an in-app link or back/forward. The app's own SetPathAsync is not echoed back, and a cold load or reload raises nothing: read that path from CurrentPath. Handlers run on a background task in the navigating client's UserScope/ClientScope, so scoped reactives resolve to that client. A handler exception is logged and swallowed, never reaching the client.
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
    Task<NotificationOutcome> NotifyAsync(string userId, NotificationContent content, string? kind = null, NotificationRoute? route = null, CancellationToken ct = default)
    QuietHours? QuietHoursFor(string userId)
    void Remove(string itemId)
    void SetQuietHours(TimeOnly startUtc, TimeOnly endUtc)
    void SetQuietHoursFor(string userId, TimeOnly startUtc, TimeOnly endUtc)
    int UnreadCountFor(string userId)
    const string PushChannel
  sealed record NotificationOutcome
    // Item: The inbox item, or null when the route skipped the inbox.
    // PushResults: Per-session push outcomes plus the offline push row when one was sent; empty when push was off or not attempted.
    // Delivered: Channels that reached the user: "push" only when a session showed it or the push hub accepted it, plus the extra channels that sent ("email", "sms", …).
    // Skipped: Channels that had no address for the user, were unconfigured, are muted by the user, or ("push") were refused by every client.
    // Failed: Channels that threw, and "push" when the push hub refused it; the error is logged, the notification still stands in the inbox.
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
  enum NotificationSendChannel
    // A connected client session; NotificationSendResult.SessionId names it.
    Session
    // The backend push hub, reached when the user had no connected session or the reach was NotificationReach.AllDevices; NotificationSendResult.SessionId is 0.
    OfflinePush
  sealed record NotificationSendResult
    // SessionId: The target client session id; 0 for the offline push row.
    // Delivered: True when the client actually displayed the notification (permission granted), or the push hub accepted the offline push.
    // Permission: The client's resulting permission state after the send attempt; NotificationPermission.Default on the offline push row, which has no client to ask.
    // Channel: Whether this row is a connected session or the offline push.
    // Error: Why the offline push was not accepted; null when it was, and on session rows.
    ctor(int SessionId, bool Delivered, NotificationPermission Permission, NotificationSendChannel Channel = Session, string? Error = null)
    NotificationSendChannel Channel { get; init; }
    bool Delivered { get; init; }
    string? Error { get; init; }
    NotificationPermission Permission { get; init; }
    int SessionId { get; init; }
    // The SessionId of the offline push row.
    const int OfflinePushSessionId = 0
  // Accessed via app.Notifications. Client permission is requested lazily on the first actual send, not when the app opens. SendToUserAsync automatically falls back to offline OS push (Web Push / FCM) when the target user has no connected session.
  sealed class NotificationService
    // content: The notification content.
    Task<IReadOnlyList<NotificationSendResult>> BroadcastAsync(NotificationContent content, CancellationToken ct = default)
    // sessionId: The target client session id.
    Task<NotificationPermission> GetPermissionAsync(int sessionId, CancellationToken ct = default)
    // sessionId: The target client session id.
    // content: The notification content.
    Task<NotificationSendResult> SendToSessionAsync(int sessionId, NotificationContent content, CancellationToken ct = default)
    // One result per connected session for the user. When the user had no connected session the list holds one NotificationSendChannel.OfflinePush row whose Delivered says whether the push hub accepted the push and whose Error says why not; a push failure is also logged at warning, never thrown.
    // userId: The persistent user id to notify.
    // content: The notification content.
    Task<IReadOnlyList<NotificationSendResult>> SendToUserAsync(string userId, NotificationContent content, CancellationToken ct = default)
    // With NotificationReach.AllDevices the offline push row is appended after the session rows, so the caller sees the push hub's acceptance alongside each session's outcome.
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
