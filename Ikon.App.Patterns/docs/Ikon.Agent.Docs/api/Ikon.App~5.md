namespace Ikon.App
  interface IAppBase : IMessageChannel
    BackgroundWork BackgroundWork { get; }
    // Costs are reported per day and per usage event name; credits are the billing unit. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
    CostsService Costs { get; }
    // Resolved from the ambient reactive scope: null outside a client scope (e.g. background work, a timer). Identifies the client being served, never this plugin's own connection context.
    virtual Context? CurrentClientContext { get; }
    // Empty string when no client is in scope. This is the correct key for a payment customer key, subscription gating, and per-user state — always populated for a connected client (the real user id when authenticated, else a stable anonymous id).
    virtual string CurrentUserId { get; }
    // An escape hatch for libraries that need a real filesystem path. Prefer Files (Files.Data) — same seeded files, plus runtime writes that persist. Read-only in the cloud — writing to it throws.
    string DataDirectory { get; }
    IReadOnlyList<DatabaseConnectionInfo> Databases { get; }
    // It compares ABSOLUTE occupancy against a share of the memory limit, so it cannot tell an instance filling up with arrivals from an app that is simply large: an app whose own resting footprint already exceeds that share is refused from its first client onward, answering 429 to every one of them. Measure your app's idle footprint before turning this on.
    bool DynamicMaxClientsEnabled { get; set; }
    // Requires the Email feature enabled on the app's organisation/space; calls from a non-entitled space throw FeatureNotEnabledException.
    EmailService Email { get; }
    // Built once before Main() runs, from the endpoints declared on the app class and on loaded [Cell] types.
    IReadOnlyList<EndpointInfo> Endpoints { get; }
    // The default implementation throws so hand-rolled test doubles keep compiling; the real app host always provides it.
    virtual AppFiles Files { get; }
    GlobalState GlobalState { get; }
    virtual LiveActivityService LiveActivity { get; }
    // null except in local dev on a localhost address (no --host-public), where it lets an in-process client reach this exact process over loopback. Via the relay or in the cloud it is null — connect through the normal relay/ApiKey path instead.
    virtual (string Host, int Port)? LocalLoopbackEndpoint { get; }
    virtual LocationService Locations { get; }
    // 0 lifts the cap entirely, which means exactly that: nothing then stops arrivals before the container runs out of memory and the kernel kills the instance with no warning and no chance to shed load. Prefer a measured number, or turn on DynamicMaxClientsEnabled alongside it.
    int MaxClients { get; set; }
    int MaxMemoryLimitMb { get; }
    virtual MotionService Motion { get; }
    // Each mount produces an independent UI stream addressable from a host UI as <ParallaxView mount="..." />. Defaults to a single mount named "ikon-ui". The value can be replaced with a longer list at any time; the render loop reacts and emits UIStreamBegin/UIStreamEnd for additions and removals.
    Reactive<IReadOnlyList<string>> Mounts { get; }
    Navigation Navigation { get; }
    NotificationService Notifications { get; }
    PaymentsService Payments { get; }
    // Reading it inside UI code subscribes to changes; for a URL with query parameters (e.g. a session join link) use JoinUrl.
    virtual string PublicUrl { get; }
    virtual RecordingArchiveService Recordings { get; }
    // Values are fetched once at startup and read synchronously; changes made with ikon app secret set while the app runs take effect only after a restart.
    Secrets Secrets { get; }
    // Consulted only during build-time snapshot capture. Returned routes are unioned with the [BootSnapshot] Routes list from ikon-config.toml, validated, and deduped.
    Func<Task<IEnumerable<string>>>? SnapshotRoutesProvider { get; set; }
    // Named by StateDatabase in the app's ikon-config toml; empty means the built-in app database. An app whose databases carry other names sets this so its state lives in Postgres rather than falling back to asset storage.
    virtual string StateDatabase { get; }
    // Call TelephonyService.GetStatusAsync to find out whether the space has telephony, or TelephonyService.GetNumbersAsync for the numbers themselves, rather than discovering either from a failed send.
    TelephonyService Telephony { get; }
    // Enabled by default. Applies only to clients that connect after it is set; already-connected clients are unaffected until they reconnect.
    bool UdpEnabled { get; set; }
    virtual UploadService Uploads { get; }
    // Enabled by default. Disable (e.g. in Main) for apps with no audio/video or low-latency data to save per-client peer-setup cost. Applies only to clients that connect afterward; already-connected clients are unaffected until they reconnect.
    bool WebRtcEnabled { get; set; }
    // Blocks until the signer completes the ceremony and the platform has the sealed result. SignatureResult carries a SignedDocument per artefact — persist those bytes as your system of record, the platform's retention is short — and a SignatureSignatoryResult per party, whose SignatoryStatus says whether they signed and whose SignatureSignerIdentity is what the eID reported about them. Name, date of birth and scheme arrive in the clear; the national identity number only ever as a platform-keyed hash.
    // signerClientSessionId: The client session ID whose browser should perform the signing ceremony.
    // request: The order specification: documents plus one SignatureSignatory naming the policy, identity schemes and attributes to request.
    // ct: Cancellation token. The order expires server-side after the configured TTL regardless.
    Task<SignatureResult> CreateSignatureOrderAsync(int signerClientSessionId, SignatureOrderRequest request, CancellationToken ct = default)
    // The connection comes back unopened — open and dispose it yourself: await using var connection = await app.DatabaseAsync(); await connection.OpenAsync();. Name nothing to get the app's default database — the built-in app one, or the app's own when it declares exactly one; names come from the Databases list in the app's ikon-config toml. The built-in database is provisioned on demand, so the first call may wait while it is created; a declared database is provisioned at activation.
    // databaseName: The database to connect to, or null for the app's default one.
    // throws ArgumentException: Thrown when a named database is not among the app's databases, or when no name was given and the app has several to choose from.
    virtual Task<DbConnection> DatabaseAsync(string? databaseName = null)
    // Provisions the built-in database if the space does not have one yet and adds it to Databases; concurrent callers share one provisioning attempt. DatabaseAsync calls this for you — call it directly only to pay the first-use cost somewhere other than the first query.
    // throws InvalidOperationException: Thrown when the database could not be provisioned.
    virtual Task<DatabaseConnectionInfo> EnsureDefaultDatabaseAsync()
    // Completes only when the persisted deletions have finished. Erasure is idempotent — erasing a user with no stored state is a no-op.
    // userId: The user whose persistent state to erase.
    virtual Task EraseUserStateAsync(string userId)
    // Each readable property becomes a URL-encoded name=value pair and null-valued properties are skipped, so app.JoinUrl(new { id = sessionId }) yields {PublicUrl}?id={sessionId}. Null returns PublicUrl as-is.
    // queryParams: Anonymous object (e.g. new { id = sessionId, host = true }) or string dictionary whose entries become the query string. Null for no query string.
    virtual string JoinUrl(object? queryParams = null)
    // Identify the endpoint by its HANDLER (the method name, e.g. nameof(GetDocument)), never by URL path — the path is what minting returns. Omitting identity (null) pins this instance's own session on an app endpoint so the URL routes back here, and pins nothing on a cell endpoint. Grants are non-expiring unless you pass expiresIn.
    // endpoint: Identifies the endpoint by its HANDLER, NOT by its URL path: pass the handler method name (e.g. nameof(GetDocument)) — or the full {Owner}_{Method} registry name when the bare name is ambiguous. Use nameof so a rename stays in sync. You never pass the path here (an endpoint's path is often derived from the method name, and may be templated) — the path is what minting RETURNS, built from this handler's EndpointInfo.PublicUrl.
    virtual Task<MintedUrl> MintUrlAsync(string endpoint, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // One backend round-trip; the result is keyed by the endpoints you passed. See MintUrlAsync for identity pinning and grant lifetime.
    // endpoints: The endpoints to mint, each identified by its HANDLER (a method name such as nameof(GetDoc), or the full {Owner}_{Method} registry name) — never by its URL path. See MintUrlAsync.
    virtual Task<IReadOnlyDictionary<string, MintedUrl>> MintUrlsAsync(IEnumerable<string> endpoints, object? identity = null, TimeSpan? expiresIn = null, string? group = null, CancellationToken ct = default)
    // The counterpart to MintUrlAsync when the caller is a person rather than a registered machine. The result is NOT a URL — send it as Authorization: Bearer {token}, never as a query parameter. It is bound to this one endpoint, expires (15 minutes by default), and a call made with it runs under that user's UserScope.
    // endpoint: The endpoint's HANDLER, exactly as MintUrlAsync takes it — a method name, or the full {Owner}_{Method} registry name when the bare one is ambiguous. An owner's JSON-RPC multiplexer is {Owner}_mcp; bare "mcp" resolves only in an app with exactly one MCP surface, so an app with cells that expose tools must name the owner.
    // userId: The space user id the token runs as.
    virtual Task<MintedUserToken> MintUserTokenAsync(string endpoint, string userId, TimeSpan? expiresIn = null, IEnumerable<string>? scopes = null, CancellationToken ct = default)
    // Databases is the list the session was started with. A database created since then — with ikon app db create or from the Portal, neither of which restarts anything — is not in it. DatabaseAsync calls this for you when it meets a name it does not recognise, so an app rarely needs it directly; call it to pick up a new database without naming it, or to see one appear in Databases.
    virtual Task<IReadOnlyList<DatabaseConnectionInfo>> RefreshDatabasesAsync()
    // Bind your listener to the returned RelayEndpoint.LocalPort; the tunnel is reachable from the internet at {PublicHost}:{PublicPort}. Dispose the endpoint to release it.
    // protocol: The endpoint protocol. EndpointProtocol.Tls enables TLS termination at the relay.
    // stablePortName: When non-empty, the relay assigns a deterministic public port for this name, so the endpoint's public URL stays the same across reconnects and process restarts. Empty = ephemeral.
    // localPort: When positive, the tunnel forwards to this local port instead of a freshly picked one — used to attach a tunnel to a listener that is already bound. 0 = pick automatically.
    Task<RelayEndpoint> RequestEndpointAsync(EndpointProtocol protocol, string stablePortName = "", int localPort = 0, CancellationToken ct = default)
    // Verify the returned JWT (issuer, audience, signature, expiry) before trusting any of its claims — see AssertionVerifier. Blocks until the user completes the challenge in their browser.
    // clientSessionId: The client session ID whose browser should perform the challenge.
    // purpose: App-declared reason for the challenge, e.g. "case.delete".
    // acrValues: Optional identity-provider hints to constrain the authentication method, encoded in the platform's agreed format. When omitted, the platform uses its configured defaults.
    // clientReturnUrl: Optional URL the platform redirects the user's browser to after the IdP flow completes. The platform appends ?stepup=<completed|failed>&challengeId=<id>. When omitted, the user lands on a generic close-window page. Set this to bring the user back into the app UI after step-up.
    // ct: Cancellation token. The challenge expires server-side after the configured TTL regardless.
    Task<string> RequestStepUpAsync(int clientSessionId, string purpose, IReadOnlyList<string>? acrValues = null, string? clientReturnUrl = null, CancellationToken ct = default)
    virtual Task RevokeGroupAsync(string group, CancellationToken ct = default)
    virtual Task RevokeUrlAsync(string grantId, CancellationToken ct = default)
    event AsyncEventHandler<ClientJoinedEventArgs> ClientJoinedAsync
    event AsyncEventHandler<ClientLeftEventArgs> ClientLeftAsync
    event AsyncEventHandler<MessageReceivedEventArgs> MessageReceivedAsync
    // Fires after app creation but before Main(). Do not subscribe from inside Main() — it has already fired by then and the handler will never run.
    event AsyncEventHandler<StartingEventArgs> StartingAsync
    event AsyncEventHandler<StoppingEventArgs> StoppingAsync
    // At-least-once delivery — the handler must be idempotent. Throwing marks the erasure incomplete and it is redelivered on a later session start.
    event AsyncEventHandler<UserDataErasureEventArgs> UserDataErasureAsync
  static class IAppEventExtensions
    static void OnClientJoined(this IAppBase app, Func<Context, Task> handler)
    static void OnClientJoined<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnClientLeft(this IAppBase app, Func<Context, Task> handler)
    static void OnClientLeft<TSessionIdentity, TClientParameters>(this IApp<TSessionIdentity, TClientParameters> app, Func<Context, TClientParameters, Task> handler)
    static void OnMessageReceived(this IAppBase app, Func<ProtocolMessage, Task> handler)
    static void OnSnapshotRoutes(this IAppBase app, Func<Task<IEnumerable<string>>> provider)
    static void OnStarting(this IAppBase app, Func<Task> handler)
    static void OnStopping(this IAppBase app, Func<Task> handler)
    // Clean APP-OWNED data here (own database tables, PII embedded in session/global values) — the platform has already erased the user's platform-managed state. Delivery is at-least-once, so the handler must be idempotent.
    static void OnUserDataErasure(this IAppBase app, Func<string, Task> handler)
  interface IClient<out TClientParameters>
    TClientParameters Parameters { get; }
    int SessionId { get; }
  interface IClientCollection<out TClientParameters> : IEnumerable<IClient<TClientParameters>>
    int Count { get; }
    IEnumerable<int> Ids { get; }
    IClient<TClientParameters>? this[int clientSessionId] { get; }
  interface INotificationChannel
    // Used in NotificationInbox.NotifyAsync's channel list and in the per-user mutes — "email", "sms", "telegram", "whatsapp", or your own.
    string Name { get; }
    // Return false when the channel has no address for the user or is not configured; throw only for a real delivery failure.
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  interface IProfileAttributes
  // A phone call whose audio the app both hears and speaks, for building a voice agent. The two streaming members are shaped to plug straight into Ikon.AI: ListenAsync yields what ISpeechRecognizer.RecognizeContinuousSpeechAsync consumes, and SpeakAsync takes what ISpeechGenerator.GenerateSpeechAsync produces. So a conversational loop needs no adapter between them:
  // await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new("How can I help?")));
  //
  // await foreach (var heard in ai.SpeechRecognizer.RecognizeContinuousSpeechAsync(config, call.ListenAsync()))
  // {
  //     await call.SpeakAsync(ai.SpeechGenerator.GenerateSpeechAsync(new(await Reply(heard))));
  // }
  // Sample rates are handled here: the provider's telephony audio and whatever rate the model wants are resampled to meet, so an app never has to know that 8 kHz exists.
  interface IVoiceCall : IAsyncDisposable
    string CallId { get; }
    // In E.164; empty on a call the app placed, where there is no such person.
    string From { get; }
    bool IsConnected { get; }
    // In E.164: the number they dialled on an incoming call, and the number the app asked for on one it placed.
    string To { get; }
    Task HangUpAsync(CancellationToken ct = default)
    // What barge-in needs when the caller starts talking over the agent.
    Task InterruptAsync(CancellationToken ct = default)
    // Ends when the call does.
    // sampleRate: What the consumer wants, typically the recognizer's rate.
    IAsyncEnumerable<float[]> ListenAsync(int sampleRate = 16000, CancellationToken ct = default)
    // Speaks audio to the caller, sending each chunk as it is produced. Returns once every chunk has been sent, which is before the caller has finished hearing it — the provider buffers and plays at its own rate. Use WaitForPlaybackAsync to wait for the audio to actually land, and InterruptAsync to abandon it.
    Task SpeakAsync(IAsyncEnumerable<AudioChunk> audio, CancellationToken ct = default)
    Task WaitForPlaybackAsync(CancellationToken ct = default)
  sealed record InboxItem
    // Id: Stable id, generated by the inbox.
    // Kind: App-defined category, e.g. "order" or "payment". Free text.
    // LaunchUrl: Optional in-app path the UI opens when the item is tapped.
    // Data: Optional opaque payload the app stored with the item.
    // Tag: Optional collapse key — a later item with the same tag replaces this one, as it does for the push notification.
    // CreatedAt: UTC time the item was recorded.
    // Read: Whether the user has seen it.
    ctor(string Id, string Title, string? Body, string? Kind, string? LaunchUrl, string? Data, string? Tag, DateTime CreatedAt, bool Read)
    string? Body { get; init; }
    DateTime CreatedAt { get; init; }
    string? Data { get; init; }
    string Id { get; init; }
    string? Kind { get; init; }
    string? LaunchUrl { get; init; }
    bool Read { get; init; }
    string? Tag { get; init; }
    string Title { get; init; }
