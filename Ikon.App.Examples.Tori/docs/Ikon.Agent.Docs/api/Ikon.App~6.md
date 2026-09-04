namespace Ikon.App
  // Every call answers false rather than throwing when the client cannot show one — a browser, an Android device, an iOS version below 16.2, or a Flutter app whose shell predates the bridge. A banner is a nicety and its absence must never take an app down with it.
  // await app.LiveActivity.StartAsync("Momentum", "#db176e",
  //     [new LiveMetric("0.00 km", "distance"), new LiveMetric("0:00", "moving")], "Run");
  sealed class LiveActivityService
    // Prefer EndEverywhereAsync when finishing whatever the activity was showing. A phone that reconnects — a dropped socket, a restarted app, a redeploy — comes back as a NEW session, so ending on the session that started the activity aims at an id that no longer exists and strands a live-looking banner on the lock screen.
    // sessionId: The client to clear, or null for the calling client.
    Task<bool> EndAsync(int? sessionId = null, CancellationToken ct = default)
    Task EndEverywhereAsync(CancellationToken ct = default)
    // title: Fixed for the life of the activity; usually the app's name.
    // accentHex: The app's accent as #rrggbb.
    // metrics: Up to three; any beyond that are not shown.
    // status: The tracked line above the metrics — a phase, a state, a kind.
    // muted: Show it held or paused, which mutes the accent.
    // sessionId: The client to show it on, or null for the calling client.
    Task<bool> StartAsync(string title, string accentHex, IReadOnlyList<LiveMetric> metrics, string status, bool muted = false, int? sessionId = null, CancellationToken ct = default)
    // metrics: Up to three; any beyond that are not shown.
    // status: The tracked line above the metrics.
    // muted: Show it held or paused.
    // sessionId: The client to update, or null for the calling client.
    Task<bool> UpdateAsync(IReadOnlyList<LiveMetric> metrics, string status, bool muted = false, int? sessionId = null, CancellationToken ct = default)
  sealed record LiveMetric
    // Value: Already formatted — the app owns its units and the banner must not reinvent them.
    // Label: The small caption under it, upper-cased by the banner.
    ctor(string Value, string Label)
    string Label { get; init; }
    string Value { get; init; }
  // The one-shot ClientFunctions.GetLocationAsync is a pull that only works while the client is connected and awake; this is the push model that survives backgrounding. Continuous background location needs the user's "Always"/background permission and is subject to app-store review, so start it only for a real reason (an active delivery, a live trip) and stop it when done.
  // app.Locations.OnUpdate(u => _couriers.Update(cs => cs.Select(c => c.SessionId == u.SessionId ? c with { Lat = u.Latitude, Lon = u.Longitude } : c)));
  // await app.Locations.StartTrackingAsync(ReactiveScope.ClientId, new LocationTrackingOptions(IntervalSeconds: 5));
  sealed class LocationService
    // Handlers run on the pushing client's reactive scope, so writing per-user or per-session reactive state from here just works.
    void OnUpdate(Action<LocationUpdate> handler)
    // Not for app code — call OnUpdate to observe. Public because the function registry binds to it by reflection.
    bool ReceiveLocationUpdate(double latitude, double longitude, double accuracy, double speed, double heading, double? altitude = null, double timestampMs = 0.0)
    void RemoveHandler(Action<LocationUpdate> handler)
    // Returns true when the client accepted (it supports geolocation and permission was not denied outright).
    // sessionId: The client session to track.
    // options: Interval, distance filter, background flag and the Android notification text.
    Task<bool> StartTrackingAsync(int sessionId, LocationTrackingOptions? options = null, CancellationToken ct = default)
    // sessionId: The client session to stop tracking.
    Task<bool> StopTrackingAsync(int sessionId, CancellationToken ct = default)
  sealed record LocationTrackingOptions
    // IntervalSeconds: Minimum seconds between reported fixes.
    // DistanceFilterMeters: Minimum metres of movement before a new fix is reported.
    // Background: Keep streaming while the app is backgrounded (Android foreground service + iOS background-location mode). When false the stream stops on backgrounding.
    // NotificationTitle: Android foreground-service notification title shown while tracking.
    // NotificationBody: Android foreground-service notification body.
    ctor(int IntervalSeconds = 10, int DistanceFilterMeters = 10, bool Background = true, string NotificationTitle = "Sharing your location", string NotificationBody = "Your location is shared while this is on.")
    bool Background { get; init; }
    int DistanceFilterMeters { get; init; }
    int IntervalSeconds { get; init; }
    string NotificationBody { get; init; }
    string NotificationTitle { get; init; }
  sealed record LocationUpdate
    // SessionId: The client session the fix came from.
    // UserId: The signed-in user id, or empty for an anonymous session.
    // AccuracyMeters: Reported horizontal accuracy in metres.
    // SpeedMps: Ground speed in metres/second, or 0 when unknown.
    // Heading: Heading in degrees (0–360), or -1 when unknown.
    // At: Server time the fix was received (UTC).
    // AltitudeMeters: Altitude in metres above the WGS-84 ellipsoid, or NaN when the device did not report one. Clients published before altitude was carried always report NaN.
    // MeasuredAt: Device time the fix was taken (UTC). Equal to At when the client did not report a timestamp. Prefer this over At for anything derived from elapsed time: a batch of fixes delivered after a network stall all arrive at once, so arrival time collapses the intervals between them and every speed and pace computed from it is wrong.
    ctor(int SessionId, string UserId, double Latitude, double Longitude, double AccuracyMeters, double SpeedMps, double Heading, DateTime At, double AltitudeMeters, DateTime MeasuredAt)
    double AccuracyMeters { get; init; }
    double AltitudeMeters { get; init; }
    DateTime At { get; init; }
    double Heading { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
    DateTime MeasuredAt { get; init; }
    int SessionId { get; init; }
    double SpeedMps { get; init; }
    string UserId { get; init; }
  // Sibling of HttpMethodAttribute: both declare an inbound HTTP endpoint over the shared addressing + identity model (see EndpointAttribute), differing only in the wire protocol. Each tool is reachable two ways: through the owner's fixed JSON-RPC multiplexer ({owner}/mcp — tools/list + tools/call, the only surface that streams progress over SSE), and as its own directly-callable POST endpoint whose body IS the tool's arguments object; that per-tool path defaults to the kebab-cased method name, and an EndpointAttribute.Path override adjusts only it, never the multiplexer. A method also carrying a verb-named REST attribute serves the REST surface and suppresses the per-tool MCP endpoint. The governance subject id is always "{Type}.{Method}". Unlike its sibling, EndpointAttribute.Auth defaults to EndpointAuth.User — a grant is a credential no MCP client can obtain; set Auth explicitly for a tool that really is reachable without a user.
  sealed class McpAttribute : EndpointAttribute
    ctor()
    ctor(string path)
    // Set this explicitly; the method's XML doc summary is never used as a fallback.
    string Description { get; init; }
    // Defaults to the method name when null or empty; the governance subject id stays "{Type}.{Method}" regardless.
    string? Name { get; init; }
    // Scopes narrow WITHIN an authorization; they do not replace it. A tool that names a scope must also be reachable — an EndpointAuth.User tool is the case this exists for, because only a token carries scopes at all. Naming one on a Public tool would be meaningless and is ignored. A caller whose token lacks the scope gets 403 with error="insufficient_scope", which is the one refusal an MCP client will re-authorize for. That is why it is a 403 and not a 401: a bare 401 says "who are you", and the client already knows.
    string Scope { get; init; }
  // Sibling of McpAttribute — same cell-method-as-callable model, different MCP verb shape: • Static resource — method takes no arguments; the URI is the literal UriTemplate with no placeholders. Lists in resources/list. • Dynamic resource — method takes parameters that map to {placeholder} segments in the URI template by name. Lists in resources/templates/list; the client crafts a concrete URI and reads it. Read-only by spec — authors should not put side effects in resource methods (the same governance hook still fires on every read with Operation = "resource", so policy authors can distinguish read access from tool dispatch).
  sealed class McpResourceAttribute : Attribute
    ctor(string uriTemplate)
    string Description { get; init; }
    // Defaults to text/plain for string returns and application/octet-stream for binary; override to be more specific (text/markdown, application/json, image/png).
    string MimeType { get; init; }
    // Defaults to the method name when null or empty.
    string? Name { get; init; }
    // Required. Placeholder names must exactly match the cell method's parameter names.
    string UriTemplate { get; }
  class MessageReceivedEventArgs : EventArgs
    ctor(ProtocolMessage message)
    ProtocolMessage Message { get; }
  // Url is the endpoint URL with pinned path placeholders substituted and the signed ?ikon-grant= appended; GrantId revokes it, and ExpiresAt is null for the default non-expiring grant.
  sealed record MintedUrl
    ctor(string Url, string GrantId, DateTimeOffset? ExpiresAt)
    DateTimeOffset? ExpiresAt { get; init; }
    string GrantId { get; init; }
    string Url { get; init; }
  // Deliberately not a URL. An access token in a query string is forbidden by the MCP specification and leaks into connector lists, access logs and proxies; this one belongs in a header and nowhere else.
  sealed record MintedUserToken
    ctor(string Token, string Resource, DateTimeOffset ExpiresAt)
    DateTimeOffset ExpiresAt { get; init; }
    string Resource { get; init; }
    string Token { get; init; }
  sealed record MotionBatch
    // SessionId: The client session the batch came from.
    // UserId: The signed-in user id, or empty for an anonymous session.
    // Samples: In the order the device produced them.
    // At: Server time the batch was received (UTC).
    ctor(int SessionId, string UserId, IReadOnlyList<MotionSample> Samples, DateTime At)
    DateTime At { get; init; }
    IReadOnlyList<MotionSample> Samples { get; init; }
    int SessionId { get; init; }
    string UserId { get; init; }
  sealed record MotionOptions
    // Hertz: Samples per second per sensor. 25 is plenty to tell a walk from a trot; a controller wants 60 or more. Devices honour this approximately.
    // Sensors: Which sensors to read.
    // BatchMilliseconds: How long the client buffers before sending. Sending each sample on its own would put a round trip on every one of them; batching turns fifty calls a second into five. Lower it for a controller, raise it to save battery.
    // Background: Keep streaming while the app is backgrounded. On iOS this needs an already-running background mode — motion alone does not keep an app alive, so pair it with location tracking if the app must keep reading in a pocket.
    // LiveHertz: Send only this many samples a second, while RecordingArchiveService keeps every one on the device. Zero streams everything. Use it when the live stream only drives a screen and the analysis happens afterwards.
    ctor(int Hertz = 25, MotionSensors Sensors = UserAcceleration, int BatchMilliseconds = 200, bool Background = false, int LiveHertz = 0)
    bool Background { get; init; }
    int BatchMilliseconds { get; init; }
    int Hertz { get; init; }
    int LiveHertz { get; init; }
    MotionSensors Sensors { get; init; }
  readonly record struct MotionSample
    // AtMillis: Device time in milliseconds since the epoch, when the sample was taken.
    // X: Acceleration in m/s², or rotation in rad/s, or field strength in µT.
    // Y: The second axis.
    // Z: The third axis.
    // Sensor: Which sensor produced it.
    ctor(double AtMillis, double X, double Y, double Z, MotionSensors Sensor)
    double AtMillis { get; init; }
    double Magnitude { get; }
    MotionSensors Sensor { get; init; }
    double X { get; init; }
    double Y { get; init; }
    double Z { get; init; }
  enum MotionSensors
    UserAcceleration
    Acceleration
    Gyroscope
    Magnetometer
  // Samples arrive in batches rather than one at a time, because a round trip per sample at fifty hertz is fifty round trips a second. MotionOptions.BatchMilliseconds is the knob: lower is more responsive, higher is cheaper. **This is not the right transport for a low-latency controller.** Batched function calls carry a scheduling delay of at least one batch, and every sample is delivered reliably whether or not it still matters. A phone used as a pointing device wants an unreliable app-defined .tp message instead, where a dropped sample is simply superseded by the next one. Use this for analysis — gait, cadence, activity, impact — and a .tp channel for control.
  // app.Motion.OnBatch(batch => _cadence.Push(batch.Samples));
  // await app.Motion.StartTrackingAsync(ReactiveScope.ClientId,
  //     new MotionOptions(Hertz: 50, Sensors: MotionSensors.UserAcceleration | MotionSensors.Gyroscope));
  sealed class MotionService
    void OnBatch(Action<MotionBatch> handler)
    // Anonymous by policy, as for a location fix: the dispatcher attributes each batch to the calling session, so a client cannot push motion as somebody else.
    bool ReceiveMotionBatch(string samplesJson)
    void RemoveHandler(Action<MotionBatch> handler)
    // sessionId: The client session to stream from.
    // options: Rate, sensors, batching and whether to keep going in the background.
    Task<bool> StartTrackingAsync(int sessionId, MotionOptions? options = null, CancellationToken ct = default)
    // sessionId: The client session to stop.
    Task<bool> StopTrackingAsync(int sessionId, CancellationToken ct = default)
  class Navigation
    // Query string stripped; null outside a client scope or before any path is known. Tracked before the client's first frame renders, so route-dependent server UI can branch on it from the very first render — unlike state set from joined handlers, which run on a background task and can lose the race against the first frame.
    string? CurrentPath { get; }
    // Round-trips to the live client over the connection rather than reading server state; returns null when the client doesn't answer or isn't connected.
    // targetId: Session id of the client to ask
    Task<string?> GetPathAsync(int targetId)
    // Acts on the client of the ambient ClientScope — call from a client-scoped context. Returns null outside a client scope or when the client doesn't answer.
    Task<string?> GetPathAsync()
    // Rejects paths under the platform-reserved /ikon and /api prefixes (throws ArgumentException) — the load balancer owns those. The client's existing query string is preserved unless path carries its own.
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
    // Fires on any client URL change — link, back button, reload, or the app's own SetPathAsync. Handlers run on a background task in the navigating client's UserScope/ClientScope, so scoped reactives resolve to that client. A handler exception is logged and swallowed, never reaching the client.
    event AsyncEventHandler<NavigationPathChangedEventArgs> PathChangedAsync
  class NavigationPathChangedEventArgs : EventArgs
    // url: The URL the client navigated to, query string included
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
