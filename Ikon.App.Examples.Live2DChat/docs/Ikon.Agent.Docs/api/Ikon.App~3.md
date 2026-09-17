namespace Ikon.App
  // Each method targets the calling client resolved from the current reactive scope unless a targetId is supplied. When the target client has not registered the backing function the call degrades to the failure value (false/null/empty list) rather than throwing — except the capture methods (StartVideoCaptureAsync, StartAudioCaptureAsync, CaptureImageAsync), which throw NotSupportedException.
  static class ClientFunctions
    // options: Optional image capture options.
    // throws NotSupportedException: Thrown when the client does not support image capture.
    static Task<ClientImageCapture> CaptureImageAsync(ClientImageCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Only the address crosses the app's transport — the browser fetches the file itself, straight from wherever it is hosted. That is the point: a place file, an export or a video never travels through the reactive channel. Pair it with an expiring signed URL from private asset storage. Returns false when the client has no download function registered.
    // url: Address the browser fetches. A signed, temporal asset URL is the usual source.
    // filename: Name the browser saves it under.
    // targetId: Client session to deliver to; defaults to the client in scope.
    static Task<bool> DownloadFileAsync(string url, string filename, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> EndLiveActivityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> ExitFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> FlushRecordingArchivesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<int?> GetBatteryLevelAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<string?> GetLanguageAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<ClientLocation?> GetLocationAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<IReadOnlyList<ClientMediaDevice>> GetMediaDevicesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // The value is whatever the browser's Network Information API exposes and mixes two vocabularies: a speed class ("slow-2g", "2g", "3g", "4g") where only that is available — note a fast wifi connection commonly reports "4g" — or a connection medium ("wifi", "cellular", "ethernet", "bluetooth", "none", ...) on platforms that expose it. Treat it as an informational hint, not a reliable wifi/cellular discriminator.
    static Task<string?> GetNetworkTypeAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<string?> GetTimezoneAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<string?> GetUrlAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<ClientVisibility> GetVisibilityAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // enabled: Whether to keep the screen awake.
    static Task<bool> KeepScreenAwakeAsync(bool enabled, int? targetId = null, CancellationToken cancellationToken = default)
    // The page navigates to the provider and returns authenticated, so the current session ends and the client reconnects with its real identity. Use from a server-drawn sign-in button in a deferred-login app; guest/email/passkey flows are client-initiated and not supported here.
    // provider: The OAuth provider to sign in with (e.g. "google").
    static Task<bool> LoginAsync(string provider, int? targetId = null, CancellationToken cancellationToken = default)
    // reason: Optional reason shown in the login dialog.
    static Task<bool> LoginShowAsync(string? reason = null, int? targetId = null, CancellationToken cancellationToken = default)
    // Clears the auth session and reloads the page, returning the client to the login screen.
    static Task<bool> LogoutAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // Absolute http(s) URLs only — SetUrlAsync is history navigation inside the app and refuses them, OpenExternalUrlAsync opens a new tab. The page unloads, so the client session ends and nothing sent to it afterwards arrives; a flow that leaves this way comes back through a landing route (the step-up and signing ceremonies do). Returns false when the client cannot leave — a client with no browser, or a URL that is not http(s).
    // url: The absolute http(s) URL to go to.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> NavigateToAsync(string url, int? targetId = null, CancellationToken cancellationToken = default)
    // url: The URL to open. Must be absolute (e.g., starts with https://).
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> OpenExternalUrlAsync(string url, int? targetId = null, CancellationToken cancellationToken = default)
    // url: The URL of the sound to play. Can be a regular URL or a data URL.
    // volume: Volume level from 0.0 to 1.0. Defaults to 1.0.
    // loop: Whether to loop the sound. Defaults to false.
    static Task<string?> PlaySoundAsync(string url, double volume = 1.0, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Audio bytes are de-duplicated per client session by content hash: the first call uploads the data, later calls with identical bytes send only the hash reference, so a reused sound is never re-transmitted.
    // data: The audio data as a byte array.
    // mimeType: The MIME type of the audio (e.g., "audio/mp3", "audio/wav").
    // volume: Volume level from 0.0 to 1.0. Defaults to 1.0.
    // loop: Whether to loop the sound. Defaults to false.
    static Task<string?> PlaySoundAsync(byte[] data, string mimeType, double volume = 1.0, bool loop = false, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> RequestFullscreenAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // x: Horizontal scroll position in pixels.
    // y: Vertical scroll position in pixels.
    // smooth: Whether to animate the scroll.
    static Task<bool> ScrollToAsync(double x, double y, bool smooth = false, int? targetId = null, CancellationToken cancellationToken = default)
    // theme: The theme to set.
    // persist: Whether to persist the theme as a user preference.
    static Task<bool> SetThemeAsync(Theme theme, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // Prefer SetThemeAsync for the built-in dark and light themes; this overload exists for custom theme names.
    // themeName: The theme name to set (e.g., "light", "dark", or a custom theme name).
    // persist: Whether to persist the theme as a user preference.
    // throws ArgumentException: Thrown when themeName is null or whitespace.
    static Task<bool> SetThemeAsync(string themeName, bool persist = true, int? targetId = null, CancellationToken cancellationToken = default)
    // url: The URL path to set (relative paths only).
    // replace: If true, replaces current history entry instead of adding a new one.
    // preserveQueryParams: If true, preserves existing query parameters when the URL does not contain a query string.
    // throws ArgumentException: Thrown when url is null or whitespace.
    static Task<bool> SetUrlAsync(string url, bool replace = false, bool preserveQueryParams = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Call when a route's content finishes loading (guard with Context.IsSnapshot); without the signal, capture falls back to a quiescence heuristic that may record loading skeletons for slow-loading routes. No-op outside snapshot capture.
    static Task<bool> SnapshotReadyAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // options: Optional audio capture options.
    // throws NotSupportedException: Thrown when the client does not support audio capture.
    static Task<string> StartAudioCaptureAsync(ClientAudioCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // title: Fixed for the life of the activity; the app's own name usually.
    // accentHex: The app's accent as #rrggbb, so the banner matches the app.
    // metricsJson: A JSON array of {"value","label"}, at most three shown.
    // status: The small tracked line above the metrics — a phase, a state, a name.
    // muted: Shows the activity as held or paused, which mutes the accent.
    static Task<bool> StartLiveActivityAsync(string title, string accentHex, string metricsJson, string status, bool muted = false, int? targetId = null, CancellationToken cancellationToken = default)
    // Prefer app.Locations.StartTrackingAsync over calling this directly; each fix is pushed back to the server and surfaces via app.Locations.OnUpdate.
    // intervalSeconds: Minimum seconds between fixes.
    // distanceFilterMeters: Minimum metres of movement before a new fix is reported.
    // background: Keep streaming while the app is backgrounded.
    // notificationTitle: Android foreground-service notification title.
    // notificationBody: Android foreground-service notification body.
    static Task<bool> StartLocationUpdatesAsync(int intervalSeconds = 10, int distanceFilterMeters = 10, bool background = true, string notificationTitle = "Sharing your location", string notificationBody = "Your location is shared while this is on.", int? targetId = null, CancellationToken cancellationToken = default)
    // hertz: Samples per second per sensor; honoured approximately.
    // sensors: Bit flags matching MotionSensors.
    // batchMilliseconds: How long the client buffers before sending.
    // background: Keep reading while the app is backgrounded.
    // liveHertz: Send only this many a second, keeping the rest for the device archive; 0 sends everything.
    static Task<bool> StartMotionUpdatesAsync(int hertz = 25, int sensors = 1, int batchMilliseconds = 200, bool background = false, int liveHertz = 0, int? targetId = null, CancellationToken cancellationToken = default)
    // archiveId: Names the activity; one id is one file.
    // fixes: Record position fixes.
    // motion: Record motion samples at their full rate.
    // maxBytes: Refuse to grow the file past this.
    static Task<bool> StartRecordingArchiveAsync(string archiveId, bool fixes = true, bool motion = true, long maxBytes = 268435456, int? targetId = null, CancellationToken cancellationToken = default)
    // source: The video source (Camera or Screen).
    // options: Optional video capture options.
    // throws NotSupportedException: Thrown when the client does not support video capture.
    static Task<string> StartVideoCaptureAsync(ClientVideoCaptureSource source = Camera, ClientVideoCaptureOptions? options = null, int? targetId = null, CancellationToken cancellationToken = default)
    // streamId: The stream ID of the capture to stop.
    // throws ArgumentException: Thrown when streamId is null or whitespace.
    static Task<bool> StopCaptureAsync(string streamId, int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> StopLocationUpdatesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    static Task<bool> StopMotionUpdatesAsync(int? targetId = null, CancellationToken cancellationToken = default)
    // archiveId: The id given to StartRecordingArchiveAsync.
    static Task<bool> StopRecordingArchiveAsync(string archiveId, int? targetId = null, CancellationToken cancellationToken = default)
    // playbackId: The playback ID returned from PlaySoundAsync.
    static Task<bool> StopSoundAsync(string playbackId, int? targetId = null, CancellationToken cancellationToken = default)
    // metricsJson: A JSON array of {"value","label"}, at most three shown.
    // status: The small tracked line above the metrics.
    // muted: Shows the activity as held or paused.
    static Task<bool> UpdateLiveActivityAsync(string metricsJson, string status, bool muted = false, int? targetId = null, CancellationToken cancellationToken = default)
    // durationMs: The vibration duration in milliseconds.
    // throws ArgumentOutOfRangeException: Thrown when durationMs is not positive.
    static Task<bool> VibrateAsync(int durationMs, int? targetId = null, CancellationToken cancellationToken = default)
    // pattern: The alternating vibrate/pause durations in milliseconds.
    // throws ArgumentException: Thrown when pattern is null, empty, or contains a negative duration.
    static Task<bool> VibrateAsync(IReadOnlyList<int> pattern, int? targetId = null, CancellationToken cancellationToken = default)
    // pattern: Duration in ms, or comma-separated pattern (e.g., "200" or "100,50,100").
    // throws ArgumentException: Thrown when pattern is null or whitespace.
    static Task<bool> VibrateAsync(string pattern, int? targetId = null, CancellationToken cancellationToken = default)
  // A preference, not a guarantee — the client falls back to whatever encoder it has.
  enum ClientHardwareAcceleration
    PreferHardware
    PreferSoftware
  sealed record ClientImageCapture
    // Mime: The image's mime type, as encoded by the client: image/jpeg or image/png.
    // Width: The image's actual width in pixels, which can differ from a requested width the client could not honor.
    // Height: The image's actual height in pixels, which can differ from a requested height the client could not honor.
    // Data: The encoded image bytes (a complete JPEG or PNG file, not raw pixels), ready to write to disk or hand to an asset or a vision model.
    ctor(string Mime, int Width, int Height, byte[] Data)
    byte[] Data { get; init; }
    int Height { get; init; }
    string Mime { get; init; }
    int Width { get; init; }
  enum ClientImageCaptureFormat
    Jpeg
    Png
  // Every null property leaves that setting to the client.
  sealed record ClientImageCaptureOptions
    ctor()
    // Null captures JPEG.
    ClientImageCaptureFormat? Format { get; init; }
    int? Height { get; init; }
    // 0.0 (smallest, most artifacts) to 1.0 (largest, near-lossless); only meaningful for ClientImageCaptureFormat.Jpeg — PNG is lossless and ignores it.
    double? Quality { get; init; }
    int? Width { get; init; }
  class ClientJoinedEventArgs : EventArgs
    ctor(Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string UserId { get; }
  class ClientLeftEventArgs : EventArgs
    ctor(Context clientContext)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    string UserId { get; }
  sealed record ClientLocation
    // Latitude: The latitude coordinate.
    // Longitude: The longitude coordinate.
    // Accuracy: The accuracy of the coordinates in meters.
    ctor(double Latitude, double Longitude, double Accuracy)
    double Accuracy { get; init; }
    double Latitude { get; init; }
    double Longitude { get; init; }
  sealed record ClientMediaDevice
    // DeviceId: The unique identifier for the device.
    // Kind: The kind of device (audio input or video input).
    // Label: A human-readable label for the device.
    // GroupId: The group identifier for devices that share the same physical device.
    ctor(string DeviceId, ClientMediaDeviceKind Kind, string Label, string GroupId)
    string DeviceId { get; init; }
    string GroupId { get; init; }
    ClientMediaDeviceKind Kind { get; init; }
    string Label { get; init; }
  enum ClientMediaDeviceKind
    Unknown
    AudioInput
    VideoInput
  sealed class ClientProfile
    ProfileAddress? Address { get; }
    string? BirthDate { get; }
    string? Email { get; }
    string? FirstName { get; }
    string? Gender { get; }
    string Id { get; }
    string? Language { get; }
    string? LastName { get; }
    string? Name { get; }
    string? PhoneNumber { get; }
    string? PreferredName { get; }
    IReadOnlyList<string> Roles { get; }
    string UserId { get; }
    // Computed: PreferredName ?? FirstName ?? empty
    string VisibleName { get; }
    object? GetAttribute(string key)
    // Throws InvalidOperationException naming the attribute when a stored value does not convert to the property's type; an absent attribute leaves the property at its default.
    TAttributes GetAttributes<TAttributes>() where TAttributes : IProfileAttributes, new()
    bool HasRole(UserRole role)
    void RequireRole(UserRole role)
  // A connected client's profile is cached when it joins, so lookups for connected clients return from cache; a cache miss loads from the backend asynchronously. Lookups return null only when the context carries no UserId or the backend has no matching profile; a backend call that fails throws (an HttpRequestException from the backend client), so "no profile" is never reported for an outage.
  class ClientProfiles
    ctor(IAppBase app)
    Task AddRoleAsync(Context clientContext, UserRole role)
    Task AddRoleAsync(Context clientContext, string role)
    void ClearCache()
    // Throws BackendPageCapException<T> when more than maxResults profiles match rather than returning a silently truncated list; the profiles fetched before the cap are on the exception. Raise the cap or narrow the filter.
    Task<IReadOnlyList<ClientProfile>> FindProfilesAsync(Dictionary<string, string> filters, int maxResults = 1000)
    // Throws BackendPageCapException<T> when the space holds more than maxResults profiles rather than returning a silently truncated list; the profiles fetched before the cap are on the exception. Raise the cap for a larger space.
    Task<IReadOnlyList<ClientProfile>> GetAllProfilesAsync(int maxResults = 1000)
    Task<TAttributes?> GetAttributesAsync<TAttributes>(Context clientContext) where TAttributes : IProfileAttributes, new()
    // Null only for a missing UserId or a user the backend has no profile for; a backend call that fails throws rather than reading as "no profile".
    Task<ClientProfile?> GetProfileAsync(Context clientContext)
    // Null only for a user the backend has no profile for; a backend call that fails throws rather than reading as "no profile".
    Task<ClientProfile?> GetProfileAsync(string userId)
    // Throws when the backend call fails; the cached profile is kept until a refresh succeeds.
    Task RefreshProfileAsync(Context clientContext)
    // Throws when the backend call fails; the cached profile is kept until a refresh succeeds.
    Task RefreshProfileAsync(string userId)
    Task RemoveRoleAsync(Context clientContext, UserRole role)
    Task RemoveRoleAsync(Context clientContext, string role)
    Task SetAttributesAsync<TAttributes>(Context clientContext, TAttributes attrs) where TAttributes : IProfileAttributes
    Task SetRolesAsync(Context clientContext, IEnumerable<UserRole> roles)
    Task SetRolesAsync(Context clientContext, IEnumerable<string> roles)
    Task UpdateAsync(Context clientContext, Action<ProfileData> update)
  // Listed in ClientVideoCaptureOptions.PreferredCodecs in priority order; the client picks the first one it can actually encode with and falls back to its own default if none are available.
  enum ClientVideoCaptureCodec
    H264
    Vp8
    Vp9
    Av1
