namespace Ikon.App
  // Partitioned at runtime by UserScope: each user sees their own value across all of their client sessions. Reads and writes resolve against the active scope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throw when none is active: Main(), the constructor, Task.Run loops, timers and endpoint handlers carry no user scope, so reach one user's value from there through ValueFor / SetFor / UpdateFor with an id captured where the scope existed. The constructor's value is what every user starts with until they have one of their own.
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
  // Same contract as ReactiveDictionary<TKey, TValue> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>. Reads and mutations resolve against the active UserScope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throw when none is active: Main(), the constructor, Task.Run loops, timers and endpoint handlers carry no user scope, so reach one user's partition from there through the …For(userId, …) accessors with an id captured where the scope existed. Items given to the constructor are what every user starts with until they have state of their own.
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
  // Same contract as ReactiveHashSet<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>. Reads and mutations resolve against the active UserScope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throw when none is active: Main(), the constructor, Task.Run loops, timers and endpoint handlers carry no user scope, so reach one user's partition from there through the …For(userId, …) accessors with an id captured where the scope existed. Items given to the constructor are what every user starts with until they have state of their own.
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
  // Same contract as ReactiveList<T> — tracked reads, one notification per mutation, copy-on-write snapshots — persisted exactly like PersistentUserReactive<T>. Reads and mutations resolve against the active UserScope — inside UI.Root(), an action callback, or a ReactiveScope.Use(new UserScope(...)) block — and throw when none is active: Main(), the constructor, Task.Run loops, timers and endpoint handlers carry no user scope, so reach one user's partition from there through the …For(userId, …) accessors with an id captured where the scope existed. Items given to the constructor are what every user starts with until they have state of their own.
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
  // Little-endian throughout. File header, 24 bytes: magic IKAR (4), version u16, reserved u16, startedUnixMs i64, baseAtMs f64. Then records, each opening with kind u8 and offsetMs u32 measured from baseAtMs: a fix carries latitude f64, longitude f64, accuracy f32, speed f32, heading f32, altitude f32 (37 bytes in total); a motion sample carries sensor u8, x f32, y f32, z f32 (18 bytes). Offsets are relative to a base rather than absolute because a millisecond epoch is around 1.7e12, which single precision resolves no better than about 130 seconds — coarser than the gap between samples, so absolute float timestamps would destroy every rhythm in the file.
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
    ctor(SpeechNotRecognizedReason reason, Context clientContext, string streamId, string? correlationId, Exception? error = null, int turnId = 0)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    // The failure when Reason is SpeechNotRecognizedReason.Error; otherwise null.
    Exception? Error { get; }
    SpeechNotRecognizedReason Reason { get; }
    string StreamId { get; }
    // Identifier of the detected turn when the segment came from Audio.UseTurnDetection, shared with the matching TurnStartedEventArgs; 0 for push-to-talk segments.
    int TurnId { get; }
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
