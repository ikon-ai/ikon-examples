# Ikon.Common.Core Public API

namespace Ikon.Common.Core
  class IkonBackend.Address
    ctor()
    string City { get; set; }
    string Country { get; set; }
    List<float> Location { get; set; }
    string Municipality { get; set; }
    string State { get; set; }
    string Street { get; set; }
    string Zip { get; set; }
    override string ToString()
  class IkonBackend.ApiKeyResponse
    ctor()
    string Token { get; set; }
  class IkonBackend.AppBundle
    ctor()
    List<IkonBackend.AppBundleWarning>? ActivationErrors { get; set; }
    List<IkonBackend.AppBundleWarning>? ActivationWarnings { get; set; }
    DateTime CreatedAt { get; set; }
    string Hash { get; set; }
    string Id { get; set; }
    string Item { get; set; }
    string SpaceId { get; set; }
    IkonBackend.AppBundleState State { get; set; }
    DateTime UpdatedAt { get; set; }
    string Version { get; set; }
  enum IkonBackend.AppBundleState
    Received
    Inactive
    Activating
    Active
    ActivationFailed
    Failed
  class IkonBackend.AppBundleWarning
    ctor()
    string Code { get; set; }
    string Message { get; set; }
  // Options for booting a target app via an IAppHost .
  sealed class AppHostOptions : IEquatable<AppHostOptions>
    // Options for booting a target app via an IAppHost .
    ctor(bool NeedsFrontend = true, bool ForceRelay = false, string LogPrefix = "Preview", bool WatchForReload = true, bool HostForwardsLogsToBackend = false)
    // Expose the app through the relay instead of a direct localhost URL — required when the viewer's browser can't reach this host's localhost (cloud run or --public-access).
    bool ForceRelay { get; init; }
    // Whether the HOST forwards its own logs to the backend (true in cloud, false for local runs — including --public-access). When true, the embedded server forwards its logs over the host's backend connection so they land under the host's session; when false it stays silent, mirroring the host's "no backend logs locally" behaviour. Decouples log forwarding from ForceRelay , which is about reachability, not log policy.
    bool HostForwardsLogsToBackend { get; init; }
    // Bare prefix token tagging the embedded server's log lines so they're attributable when they interleave with the host's (and, when forwarded, in the backend/portal). The renderers add the […] decoration. Defaults to Preview (the Studio live preview); the codegen smoke gate overrides it to Sandbox.
    string LogPrefix { get; init; }
    // Start the app's Vite frontend and resolve a browsable URL. The Studio preview and the validator-driven smoke need it; a plain boot-check smoke does not (saves the node process).
    bool NeedsFrontend { get; init; }
    // Watch the built DLL and hot-reload the plugin in place when it changes. True for the live preview (iterative edits reload without restart). The codegen smoke MUST set this false: it is a cheap one-shot boot of the freshly-built DLL, not a live editing surface — a watcher there reloads on every Coder edit mid-run, and a reload racing the smoke's teardown throws "Cannot access a disposed object: CellHost", which gets misreported to the agent as an app crash and sends it into a phantom fix loop.
    bool WatchForReload { get; init; }
  // Outcome of StartAsync . Url is the browsable frontend URL when NeedsFrontend was set, else null.
  struct AppHostResult : IEquatable<AppHostResult>
    // Outcome of StartAsync . Url is the browsable frontend URL when NeedsFrontend was set, else null.
    ctor(bool Ok, string? Url, string Message)
    string Message { get; init; }
    bool Ok { get; init; }
    string? Url { get; init; }
  class IkonBackend.AppPaymentsInitResult
    ctor()
    string? BackendUrl { get; set; }
    string Mode { get; set; }
    string? PublishableKey { get; set; }
  class IkonBackend.AppPaymentsMerchantRequest
    ctor()
    string ContactEmail { get; set; }
    string? CorporateId { get; set; }
    string? Country { get; set; }
    string? DefaultCurrency { get; set; }
    string? DisplayName { get; set; }
    bool? IsDefault { get; set; }
    string? Provider { get; set; }
    string? RefreshUrl { get; set; }
    string? ReturnUrl { get; set; }
  class IkonBackend.AppPaymentsMerchantResult
    ctor()
    string DashboardUrl { get; set; }
    string KycUrl { get; set; }
    string MerchantId { get; set; }
  class IkonBackend.AppPaymentsOffer
    ctor()
    string Name { get; set; }
    string OfferId { get; set; }
    List<IkonBackend.AppPaymentsOfferPrice> Prices { get; set; }
  class IkonBackend.AppPaymentsOfferPrice
    ctor()
    long AmountMinor { get; set; }
    string Currency { get; set; }
    string? Interval { get; set; }
    int? IntervalCount { get; set; }
    string Kind { get; set; }
  class IkonBackend.AppPaymentsOffersResult
    ctor()
    List<IkonBackend.AppPaymentsOffer> Offers { get; set; }
  class IkonBackend.AppPaymentsRemoveResult
    ctor()
    bool Removed { get; set; }
  class IkonBackend.AppPaymentsStatusResult
    ctor()
    bool ChargesEnabled { get; set; }
    string? DashboardUrl { get; set; }
    bool DetailsSubmitted { get; set; }
    string? MerchantId { get; set; }
    bool PayoutsEnabled { get; set; }
    string? Provider { get; set; }
    List<string> RequirementsCurrentlyDue { get; set; }
  class IkonBackend.ApplyAppBundleConfigResponse
    ctor()
    List<IkonBackend.AppBundleWarning> Warnings { get; set; }
  // High performance custom queue type for value type arrays. The backing array starts small and grows on demand up to MaxCapacity ; pre-allocating the maximum upfront would waste large amounts of memory for queues that are usually only partially filled (e.g. audio buffers sized for a worst-case duration that is rarely reached).
  class ArrayQueue<T> where T : struct
    ctor(int maxCapacity)
    ctor(int maxCapacity, int initialCapacity)
    int Capacity { get; }
    int Count { get; }
    int FreeCount { get; }
    T Item { get; }
    int MaxCapacity { get; }
    Span<T> Span { get; }
    void Clear()
    void Dequeue(Span<T> target, int skipCount, int count)
    void Dequeue(Span<T> target, int count)
    void DequeueMemory(int count)
    void Enqueue(ReadOnlySpan<T> source, int count)
    void Enqueue(ReadOnlySpan<T> source)
    void EnqueueMemory(int count)
    Memory<T> GetDequeueMemory(int skipCount, int count)
    Memory<T> GetEnqueueMemory(int count)
    // Releases excess buffer capacity, shrinking the backing array down to fit the current content. Useful in long-lived queues that have hit a transient peak and now want to return memory (in particular Large Object Heap memory) to the GC.
    void TrimExcess()
  // Verifies platform-signed assertions (e.g. StepUpAssertion ) issued by the Ikon platform backend. Fetches the platform JWKS from {platformBaseUrl}/.well-known/jwks.json on demand and caches the keys for the lifetime of the verifier instance.
  sealed class AssertionVerifier
    ctor(string platformBaseUrl, HttpClient? httpClient = null, Func<DateTimeOffset>? clock = null)
    // Generic JWT validation: JWKS-backed signature verification + standard iss/aud/exp checks + (when present) iat clock-skew guard. Returns the decoded claims as a JsonDocument — caller owns disposal — plus the token's exp so a caller can cache the validated result against the token lifetime. Use this for OAuth 2.1 bearer-token resource-server validation where the step-up-specific projection in VerifyAsync isn't relevant.
    Task<ValueTuple<JsonDocument, DateTimeOffset>> VerifyAndExtractClaimsAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = null)
    Task<StepUpAssertion> VerifyAsync(string token, string expectedIssuer, string expectedAudience, CancellationToken ct = null)
  class IkonBackend.Asset
    ctor()
    string AssetId { get; set; }
    DateTime CreatedAt { get; set; }
    string Filename { get; set; }
    string Type { get; set; }
    string? Url { get; set; }
  class IkonBackend.AssetSignedDownload
    ctor()
    string Url { get; set; }
  delegate Log.AsyncFlowFinishedHandler
    void AsyncFlowFinishedHandler(object sender, int asyncFlowId)
  sealed class AsyncLocalInstanceAttribute : Attribute
    ctor()
  class AsyncLocalInstance<T> where T : new()
    ctor()
    static T Instance { get; }
    static void DisableAsyncLocalInstance()
    static void EnableAndInitAsyncLocalInstance()
    static void SetAsyncLocalInstance(T value)
  class BasePluginConfig
    ctor()
    AppSourceType AppSourceType
    string DataDirectory
    bool DebugMode
    bool EnableProxyMode
    bool HasInput
    string IkonBackendToken
    string IkonBackendUrl
    string Locale
    int MaxMessageSize
    PayloadType PayloadType
    int ReceiveQueueCapacity
    int SendQueueCapacity
    ServerRunType ServerRunType
    bool TcpNoDelay
    int TcpReceiveBufferSize
    int TcpSendBufferSize
    bool ThrowOnUdpConnectionFailure
    string UserId
  abstract class BasePlugin<TPlugin, TConfig> : ILogInfo, IMessageChannel, IPlugin, IProtocolMessageChannel where TConfig : BasePluginConfig, new()
    Context ClientContext { get; }
    ClientInitialization? ClientInitializationData { get; }
    string ConnectTokenJson { get; }
    Dictionary<string, object> DynamicConfig { get; }
    GlobalState GlobalState { get; }
    bool IsAuthTicketSent { get; }
    bool IsConnected { get; }
    bool IsUdpConnected { get; }
    AuthResponse? LastAuthResponse { get; }
    object LogInfo { get; }
    Reactive<Dictionary<string, object>> ReactiveDynamicConfig { get; }
    ReactiveGlobalState ReactiveGlobalState { get; }
    DateTime ServerInitTime { get; set; }
    // True once the server has signalled an intentional shutdown (CORE_ON_SERVER_STOPPING). The SDK uses this to suppress automatic reconnect — reconnecting to a deliberately-stopped server would just re-provision a fresh instance.
    bool ServerStopping { get; }
    int SessionId { get; }
    Task ConnectAsync2(string connectUrl, CancellationToken ct = null)
    Task ConnectAsync2(string host, int port, bool useTls, CancellationToken ct = null)
    void OverrideConfigValues(string overrideConfigJson)
    Task ReconnectWithAuthResponseAsync(AuthResponse cachedAuthResponse, CancellationToken ct = null)
    IDisposable RegisterMessageHandler(Func<ProtocolMessage, ValueTask> handler, Opcode? opcodeGroupMask = null, Opcode[]? opcodes = null)
    virtual ValueTask SendMessageAsync(ProtocolMessage message)
    ValueTask SendMessageAsync(IProtocolMessagePayload payload)
    Task SignalReadyAsync()
    Task StopAsync()
    override string ToString()
    Task<bool> WaitForClientAsync(int? clientSessionId = null, string? description = null, string? userId = null, string? deviceId = null, string? productId = null, TimeSpan timeout = null)
    Task WaitForUdpReadyAsync(CancellationToken ct = null)
    Func<Task> ConnectedAsync
    Func<Task> ConnectingAsync
    Func<Task> DisconnectedAsync
    Func<Task> JoinedAsync
    Func<ProtocolMessage, ValueTask> MessageReceivedAsync
    Func<Task> StoppingAsync
  class IkonBackend.BillingProduct
    ctor()
    bool ComingSoon { get; set; }
    bool Deprecated { get; set; }
    List<string> Features { get; set; }
    string Id { get; set; }
    IkonBackend.BillingProductMetadata Metadata { get; set; }
    bool MostPopular { get; set; }
    string Name { get; set; }
    Dictionary<string, IkonBackend.BillingProductPrice> Prices { get; set; }
    string Slug { get; set; }
    string? Tagline { get; set; }
  class IkonBackend.BillingProductMetadata
    ctor()
    int? Credits { get; set; }
    int? MonthlyCredits { get; set; }
    string PlanType { get; set; }
    string? Tier { get; set; }
  class IkonBackend.BillingProductPrice
    ctor()
    double Amount { get; set; }
    string Currency { get; set; }
    string Description { get; set; }
    string Id { get; set; }
    string Interval { get; set; }
    string LookupKey { get; set; }
  class IkonBackend.BillingRedirectResult
    ctor()
    string RedirectUrl { get; set; }
  class IkonBackend.BillingStatusResult
    ctor()
    bool CancelAtPeriodEnd { get; set; }
    string? PeriodEnd { get; set; }
    string? PeriodStart { get; set; }
    double PurchasedRemaining { get; set; }
    double SubscriptionAllocation { get; set; }
    double SubscriptionRemaining { get; set; }
    string? SubscriptionStatus { get; set; }
    double TotalRemaining { get; set; }
  class IkonBackend.BillingTransaction
    ctor()
    long? AmountMinor { get; set; }
    string? CreatedAt { get; set; }
    double Credits { get; set; }
    string? Currency { get; set; }
    string? HostedInvoiceUrl { get; set; }
    string Id { get; set; }
    string? InvoiceNumber { get; set; }
    string Type { get; set; }
  class IkonBackend.CampaignRedeemResult
    ctor()
    string CampaignName { get; set; }
    int? CreditsGranted { get; set; }
    string? SubscriptionExpiresAt { get; set; }
    string Type { get; set; }
  class IkonBackend.Channel
    ctor()
    List<IkonBackend.ChannelConditionGroup> Conditions { get; set; }
    string Description { get; set; }
    IkonBackend.ChannelHash Hash { get; set; }
    string Id { get; set; }
    bool? IsPrivate { get; set; }
    string Key { get; set; }
    bool? MainChannel { get; set; }
    string Name { get; set; }
    List<IkonBackend.ChannelPlugin> Plugins { get; set; }
    string ServerHostingMode { get; set; }
    string SpaceId { get; set; }
    List<string> Tags { get; set; }
    string Type { get; set; }
  class IkonBackend.ChannelCondition
    ctor()
    string Condition { get; set; }
    string Field { get; set; }
    object Value { get; set; }
  class IkonBackend.ChannelConditionGroup
    ctor()
    List<IkonBackend.ChannelCondition> Conditions { get; set; }
  class IkonBackend.ChannelHash
    ctor()
    bool Enabled { get; set; }
    bool IncludeUserId { get; set; }
  class IkonBackend.ChannelInstance
    ctor()
    string ChannelId { get; set; }
    string ChannelKey { get; set; }
    string ChannelTitle { get; set; }
    string Code { get; set; }
    string Id { get; set; }
    string IkonServerHost { get; set; }
    string Mode { get; set; }
    string Name { get; set; }
    string SpaceId { get; set; }
    bool UseInsecureConnection { get; set; }
  class IkonBackend.ChannelInstanceLaunchToken
    ctor()
    string Token { get; set; }
  class IkonBackend.ChannelInstanceSession
    ctor()
    string ChannelInstance { get; set; }
    string? ChannelTitle { get; set; }
    string? CrashLog { get; set; }
    string CreatedAt { get; set; }
    string? Hostname { get; set; }
    string Id { get; set; }
    string? Ip { get; set; }
    string Space { get; set; }
    string? Status { get; set; }
    string UpdatedAt { get; set; }
    string? UserSummary { get; set; }
  class IkonBackend.ChannelPlugin
    ctor()
    List<IkonBackend.ChannelPluginConfiguration> Configurations { get; set; }
    bool Enabled { get; set; }
    string PluginId { get; set; }
  class IkonBackend.ChannelPluginConfiguration
    ctor()
    string Code { get; set; }
    string Configuration { get; set; }
  class IkonBackend.ChatMessage
    ctor()
    string ChannelInstanceId { get; set; }
    string CreatedAt { get; set; }
    string Id { get; set; }
    string Text { get; set; }
    string UserId { get; set; }
  class IkonBackend.ConnectChannelInstanceConfiguration
    ctor()
    string ProxyUrl { get; set; }
    string Url { get; set; }
  class IkonBackend.ConnectChannelInstanceRequest
    ctor()
    ClientType ClientType { get; set; }
    string Code { get; set; }
    ContextType ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    bool HasInput { get; set; }
    string Hash { get; set; }
    string InitialPath { get; set; }
    string InstallId { get; set; }
    Dictionary<string, string> LaunchParameters { get; set; }
    string Locale { get; set; }
    Opcode OpcodeGroupsFromServer { get; set; }
    Opcode OpcodeGroupsToServer { get; set; }
    PayloadType PayloadType { get; set; }
    string ProductId { get; set; }
    int ProtocolVersion { get; set; }
    bool ReceiveAllMessages { get; set; }
    SdkType SdkType { get; set; }
    UserType UserType { get; set; }
    string VersionId { get; set; }
    bool WaitForRunning { get; set; }
  class IkonBackend.ConnectChannelInstanceResponse
    ctor()
    IkonBackend.ConnectChannelInstanceConfiguration? Configuration { get; set; }
    bool IsProvisioning { get; }
    bool IsRunning { get; }
    string State { get; set; }
  class IkonBackend.ConnectTokenRequest
    ctor()
    int? ClientType { get; set; }
    int ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    string? Hash { get; set; }
    string InstallId { get; set; }
    int OpcodeGroupsFromServer { get; set; }
    int OpcodeGroupsToServer { get; set; }
    int PayloadType { get; set; }
    string ProductId { get; set; }
    int ProtocolVersion { get; set; }
    string? Space { get; set; }
    int UserType { get; set; }
    string VersionId { get; set; }
  class IkonBackend.ConnectTokenResponse
    ctor()
    string ServerUrl { get; set; }
  sealed class CreateSignatureOrderDocumentDto
    ctor()
    string ContentBase64 { get; set; }
    string Filename { get; set; }
    string MimeType { get; set; }
  sealed class CreateSignatureOrderRequest
    ctor()
    string? ClientReturnUrl { get; set; }
    string? CostAttributionKey { get; set; }
    List<CreateSignatureOrderDocumentDto> Documents { get; set; }
    string Purpose { get; set; }
    List<CreateSignatureOrderSignerDto> Signers { get; set; }
    string? Title { get; set; }
  sealed class CreateSignatureOrderResponse
    ctor()
    string ExpiresAt { get; set; }
    string OrderId { get; set; }
    string SignatureUrl { get; set; }
  sealed class CreateSignatureOrderSignerDto
    ctor()
    List<string>? IdpNames { get; set; }
    List<string>? RequestedAttributes { get; set; }
    string SigningPolicy { get; set; }
    string? Vendor { get; set; }
  class IkonBackend.CursorResponse<T>
    ctor()
    int Count { get; set; }
    string NextCursor { get; set; }
    string PreviousCursor { get; set; }
    List<T> Results { get; set; }
    int TotalCount { get; set; }
  class IkonBackend.CustomField
    ctor()
    List<string> AllowedMimeTypes { get; set; }
    string Context { get; set; }
    string Entity { get; set; }
    string Field { get; set; }
    string Id { get; set; }
    bool IsEditable { get; set; }
    int MaxCount { get; set; }
    int MaxSize { get; set; }
    int MinCount { get; set; }
    bool Multiple { get; set; }
    string Name { get; set; }
    List<IkonBackend.CustomFieldOption> Options { get; set; }
    string Type { get; set; }
    List<IkonBackend.CustomFieldVisibility> Visibility { get; set; }
  class IkonBackend.CustomFieldOption
    ctor()
    string LongName { get; set; }
    string Name { get; set; }
    bool UserInput { get; set; }
    string UserInputField { get; set; }
    string UserInputName { get; set; }
    bool UserInputOptional { get; set; }
    string Value { get; set; }
  class IkonBackend.CustomFieldVisibility
    ctor()
    bool IsVisible { get; set; }
    string Target { get; set; }
  class IkonBackend.Database
    ctor()
    string DatabaseName { get; set; }
    string Id { get; set; }
    string Name { get; set; }
    string OrganisationId { get; set; }
    string Provider { get; set; }
    string SpaceId { get; set; }
    string Status { get; set; }
    string Tier { get; set; }
  class IkonBackend.DatabaseConnectionResponse
    ctor()
    string ConnectionString { get; set; }
    string DatabaseName { get; set; }
    string Host { get; set; }
    string Password { get; set; }
    int Port { get; set; }
    string Username { get; set; }
  static class DeveloperMode
    static string MarkerPath { get; }
    static string DescribeSource()
    static void Disable()
    static void Enable()
    static bool IsEnabled()
  static class DiagnosticUtils
    static string BuildMemoryInfo()
    // The container memory limit in bytes, or -1 when not under a finite cgroup limit (Windows, or a bare Linux VM — no container ceiling). Linux under a finite cgroup limit: memory.max (v2) / memory.limit_in_bytes (v1). Pairs with GetContainerMemoryUsedBytes for a "used / limit" readout.
    static long GetContainerMemoryLimitBytes()
    // Memory currently used by the WHOLE container, in bytes — the cgroup working set (memory.current minus the readily-reclaimable inactive_file). This charges every process in the container, including the child node/build processes the in-process app spawns, so it is what the kernel OOM-kills on and matches docker stats. Returns -1 when not under a finite cgroup limit (Windows, or a bare Linux VM) — there is no container, so callers treat it as 0. For this process alone, see GetProcessMemoryUsedBytes .
    static long GetContainerMemoryUsedBytes()
    // The process memory figure to compare against the container/cgroup limit. On Linux under a finite cgroup memory limit (cloud, Docker --memory) this is the cgroup working set — memory.current minus readily-reclaimable file cache (inactive_file). That is the number the kernel keeps under memory.max and OOM-kills on, and it matches what docker stats shows, so it is directly comparable to e.g. the 512 MB limit. WorkingSet64 (VmRSS) over-reports here because it counts shared file-backed pages that are not all charged to the cgroup; PrivateMemorySize64 on Linux is VmData (reserved virtual address space) and is wildly inflated. Falls back to WorkingSet64 on Windows or when no finite cgroup memory limit is set (where there is nothing to compare against anyway).
    static long GetProcessMemoryUsedBytes()
  class ReactiveGlobalState.DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
    ctor()
    bool Equals(Dictionary<TKey, TValue>? x, Dictionary<TKey, TValue>? y)
    int GetHashCode(Dictionary<TKey, TValue> obj)
    static ReactiveGlobalState.DictionaryComparer<TKey, TValue> Instance
  static class EditorLauncher
    static Task<string> EditAsync(string initialContent, string fileExtension, CancellationToken cancellationToken = null)
    static string ResolveEditorCommand(Func<string, string?> getEnv, bool isWindows)
    static string ResolveEditorCommand()
  enum IkonBackend.EnvironmentType
    Unknown
    Local
    Development
    Production
  static class ExceptionFormatter
    static string FormatException(Exception ex, bool includeFilePaths = true)
  // Provides resilient conversions between loosely typed LLM/tool payloads and strongly typed function parameters/results. Handles primitives, arrays (including single-item arrays), Newtonsoft JSON tokens, and falls back to System.Text.Json when needed.
  static class ExtendedCast
    static T Convert<T>(object? value)
    static object? Convert(object? value, Type targetType)
    // Deserializes a JsonElement into targetType , tolerating the placeholders LLMs often emit when a schema marks every property required but the underlying field is nullable: "" for collections/objects becomes null, "" for bool becomes false, etc. Falls back to ExtendedCast conversion on type mismatch so callers pick up array-wrap and single-item-array behaviour for free.
    static object? FromJsonElement(JsonElement element, Type targetType)
  static class ExtendedCastExtensions
    static T ExtendedCast<T>(object? value)
    static object? ExtendedCast(object? value, Type targetType)
  class FeatureFlagsStorage : AsyncLocalInstance<FeatureFlagsStorage>
    ctor()
    ImmutableDictionary<string, bool> ReadOnlyFeatureFlags { get; }
    bool Get(string featureFlagName)
    void Set(string featureFlagName, bool value, bool shouldOverride = false)
  class IkonBackend.FileUploadResponse
    ctor()
    string UploadUrl { get; set; }
  class IkonBackend.Folder
    ctor()
    string Id { get; set; }
    string OrganisationId { get; set; }
    string ParentPath { get; set; }
    string Path { get; set; }
    List<string> PathSegments { get; set; }
    string SpaceId { get; set; }
  sealed class GetSignatureOrderResponse
    ctor()
    string? EvidenceLevel { get; set; }
    string? FailureCode { get; set; }
    string? IdentityScheme { get; set; }
    string OrderId { get; set; }
    string SignatureUrl { get; set; }
    string? SignedAt { get; set; }
    string? SignedDocumentHash { get; set; }
    string? SignedDocumentItemId { get; set; }
    string? SignedDocumentMimeType { get; set; }
    string? SignerNameHash { get; set; }
    string Status { get; set; }
  class HighPrecisionTimestamp : AsyncLocalInstance<HighPrecisionTimestamp>
    ctor()
    DateTime UtcNow { get; }
  // Process-wide hints for running inside a memory-constrained host (e.g. a small cloud container that also hosts in-process app servers/previews). Set ONCE at startup by the host — Studio sets it when its ServerRunType is Cloud, and the ikon tool sets it from a CLI flag — and read by code that spawns memory-heavy child processes (NuGet restore, npm install, Vite) so it can cap peak usage. Default is unconstrained, so local dev and normal servers are completely unaffected (no slowdown).
  static class HostMemoryMode
    // When true, prefer lower PEAK memory over speed: otherwise-parallel prepare steps (NuGet restore + npm install + docs extraction) run serially so two heavy child processes don't run at once. Off by default — local dev keeps the faster parallel path.
    static bool Constrained { get; set; }
    // Node --max-old-space-size (MB) for a TRANSIENT one-shot build (e.g. vite build) rather than a resident dev server. A full production bundle peaks well above a dev server's lazy transform (a JS-heap OOM at the resident cap is what fails the build), so it needs a higher cap than NodeMaxOldSpaceMb — but only briefly, since the process exits after the build. 0 = fall back to the resident cap.
    static int NodeBuildMaxOldSpaceMb { get; set; }
    // The NODE_OPTIONS value for a transient build process — the build cap when set, otherwise the resident cap. Null when neither is configured (local dev).
    static string? NodeBuildOptions { get; }
    // Node --max-old-space-size (MB) applied to spawned npm/Vite processes when > 0. Bounds V8 heap growth from C# without any container/Dockerfile change. 0 = leave Node's default.
    static int NodeMaxOldSpaceMb { get; set; }
    // The NODE_OPTIONS value to add to spawned Node processes, or null when unset.
    static string? NodeOptions { get; }
    // Like NodeProcessEnv but uses the larger transient-build cap (see NodeBuildMaxOldSpaceMb ). Use for one-shot builds, not resident servers.
    static IDictionary<string, string?>? NodeBuildProcessEnv()
    // An environment override that appends the Node heap cap to any inherited NODE_OPTIONS, for spawning npm/Vite. Null when no cap is configured (local dev) so callers pass nothing.
    static IDictionary<string, string?>? NodeProcessEnv()
    // Runs op , serialized against other heavy spawns when Constrained is set — so two memory-heavy child processes (NuGet restore, npm install) never run at once in a tight container. When unconstrained (local dev) it runs immediately with no gating, preserving the faster parallel path.
    static Task RunHeavyProcessAsync(Func<Task> op)
  static class HotReloadGate
    static TimeSpan CooldownDuration { get; set; }
    static bool IsEnabled { get; }
    static bool IsInCooldown()
    static void MarkEnabled()
    static void MarkReloaded()
    static int LocalMemoryMarginMb
  // Runs a built Ikon app and exposes its live URL — the one abstraction behind both the Studio preview and the codegen smoke gate, so they share a boot path and a single isolation switch. Two implementations: an in-process embedded server (default, shares the host's loaded DLLs — low memory) and a child dotnet run process (full isolation, the fallback). Defined in Ikon.Common.Core so the codegen pipeline (which does not reference the server host) can take it injected, while the host supplies the concrete implementation.
  interface IAppHost : IAsyncDisposable
    bool IsRunning { get; }
    // App root of the currently-running app (null when stopped) — lets a caller tell which app a shared host is serving before reusing it.
    string? RunningRoot { get; }
    // The live URL of the running app (null when stopped).
    string? Url { get; }
    // Mints a signed connect URL ({serverUrl}/connect?token=…) for a browser client, in-process, using the running server's own secret — so the iframe can authenticate without the server exposing a public /connect-token minting oracle. Returns null when not running or when the host does not mint in-process (e.g. the child-process host, which keeps its own /connect-token).
    abstract string? MintBrowserConnectUrl()
    // Build-if-needed, start the app rooted at sandboxDir , and wait until it is ready (and, when NeedsFrontend , its frontend is up). Stops any app this host was previously running.
    abstract Task<AppHostResult> StartAsync(string sandboxDir, AppHostOptions options, CancellationToken ct = null)
    // Stop the running app and release its resources. Safe to call when nothing is running.
    abstract Task StopAsync()
    // Human-readable diagnostics (build status, frontend errors) for surfacing to the user.
    event Action<string>? Diagnostic
  interface ILogInfo
    object LogInfo { get; }
  interface IMessageChannel
    int SessionId { get; }
    abstract IDisposable RegisterMessageHandler(Func<ProtocolMessage, ValueTask> handler, Opcode? opcodeGroupMask = null, Opcode[]? opcodes = null)
    abstract ValueTask SendMessageAsync(ProtocolMessage message)
    abstract ValueTask SendMessageAsync(IProtocolMessagePayload payload)
  interface IPlugin : IMessageChannel, IProtocolMessageChannel
    string ConnectTokenJson { get; }
    bool IsAuthTicketSent { get; }
    bool IsConnected { get; }
    // The AuthResponse from the most recent successful connect (entrypoints + auth ticket + client session). Cache it to drive a later ReconnectWithAuthResponseAsync .
    AuthResponse? LastAuthResponse { get; }
    DateTime ServerInitTime { get; set; }
    abstract Task ConnectAsync2(string connectUrl, CancellationToken ct = null)
    abstract Task ConnectAsync2(string host, int port, bool useTls, CancellationToken ct = null)
    abstract void OverrideConfigValues(string overrideConfigJson)
    // Soft reconnect: reopen the transport reusing a previously-fetched AuthResponse (its entrypoints, auth ticket, and client session) WITHOUT re-fetching it via the /connect GET. Lets the server resume the same session within its disconnect grace. Use LastAuthResponse from the prior connection.
    abstract Task ReconnectWithAuthResponseAsync(AuthResponse cachedAuthResponse, CancellationToken ct = null)
    abstract Task StopAsync()
  interface IProtocolMessageChannel : IMessageChannel
    Context ClientContext { get; }
  // Runtime app-payments transport. A running Ikon app issues these to the space-token-guarded /payments/* routes (space resolved from the app's backend session token). Each returns the raw JSON body; PaymentsService deserializes it into the typed payment records.
  class IkonBackend : AsyncLocalInstance<IkonBackend>
    ctor()
    IReadOnlyList<string> Capabilities { get; }
    string ChannelDomain { get; }
    string ChannelDomainLegacy { get; }
    IkonBackend.EnvironmentType Environment { get; }
    static string IkonDataDirectory { get; }
    bool IsAdmin { get; }
    bool IsLoggedIn { get; }
    bool IsSpaceToken { get; }
    static string LoginJsonPath { get; }
    string OrganisationId { get; }
    string SpaceId { get; }
    string Token { get; set; }
    DateTimeOffset TokenExpiryDate { get; }
    int TotalSentMessageByteCount { get; }
    int TotalSentMessageCount { get; }
    string Url { get; set; }
    string UserAgent { get; set; }
    string UserId { get; }
    Task<IkonBackend.AppBundle> ActivateAppBundleAsync(string id)
    Task<IkonBackend.Organisation> AddOrganisationUserAsync(string organisationId, string email)
    Task<IkonBackend.ApplyAppBundleConfigResponse> ApplyAppBundleConfigAsync(object config)
    Task<string> AuthenticateSpaceTokenAsync(string spaceId, string externalUserId)
    Task CancelSignatureOrderAsync(string orderId)
    Task CancelSubscriptionAsync(string subscriptionId, bool immediate = false, string? idempotencyKey = null, string? provider = null, CancellationToken cancellationToken = null)
    Task CompleteItemSignedUploadAsync(string uri, string path, string? sha256 = null)
    Task<IkonBackend.ConnectChannelInstanceResponse> ConnectChannelInstanceAsync(IkonBackend.ConnectChannelInstanceRequest request)
    Task<IkonBackend.AppBundle> CreateAppBundleAsync(string spaceId, string version, string itemId, IkonBackend.AppBundleState? state = null)
    Task<IkonBackend.AppPaymentsMerchantResult> CreateAppPaymentsMerchantAsync(string spaceId, IkonBackend.AppPaymentsMerchantRequest request)
    Task<IkonBackend.AppPaymentsOffer> CreateAppPaymentsOfferAsync(string spaceId, object request)
    Task CreateAuditEventAsync(string eventName, string spaceId, string userId, string? entityType = null, string? entityId = null, string? ip = null)
    Task<IkonBackend.BillingRedirectResult> CreateBillingCustomerPortalAsync(string organisationId, string returnUrl)
    Task<IkonBackend.BillingRedirectResult> CreateBillingPaymentAsync(string organisationId, string productId, string lookupKey, string successUrl, string cancelUrl, int? quantity = null)
    Task<IkonBackend.BillingRedirectResult> CreateBillingSubscriptionAsync(string organisationId, string productId, string lookupKey, string successUrl, string cancelUrl)
    Task<IkonBackend.Channel> CreateChannelAsync(string spaceId, string name, string type, bool isPrivate)
    Task<IkonBackend.ConnectTokenResponse> CreateChannelConnectTokenAsync(IkonBackend.ConnectTokenRequest request)
    Task<IkonBackend.ChannelInstance> CreateChannelInstanceAsync(string channelId, string mode)
    Task<IkonBackend.ChannelInstanceLaunchToken> CreateChannelInstanceLaunchTokenAsync(string id, int? httpsPort = null, int? httpPort = null, int? tcpPort = null, int? tlsPort = null)
    Task CreateChatMessageAsync(string channelInstanceId, string userId, string text, string createdAt)
    Task<string> CreateOfferAsync(string offerId, string name, long amountMinor, string currency, string kind, string? interval = null, int? intervalCount = null, string? provider = null, CancellationToken cancellationToken = null)
    Task<string> CreateOfferPaymentAsync(string offerId, string customerKey, string? email = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, string? provider = null, CancellationToken cancellationToken = null)
    Task<IkonBackend.Organisation> CreateOrganisationAsync(string name)
    Task<string> CreatePaymentAsync(long amountMinor, string currency, string customerKey, string? description = null, string? successUrl = null, string? cancelUrl = null, string? idempotencyKey = null, string? provider = null, CancellationToken cancellationToken = null)
    Task<IkonBackend.Pipeline> CreatePipelineAsync(object form)
    Task<IkonBackend.Plugin> CreatePluginAsync(IkonBackend.Plugin plugin)
    Task CreateProfileLeadAsync(string profileId, string source)
    Task<CreateSignatureOrderResponse> CreateSignatureOrderAsync(CreateSignatureOrderRequest request)
    Task<IkonBackend.SpaceApiKey> CreateSpaceApiKeyAsync(string spaceId)
    Task<IkonBackend.Space> CreateSpaceAsync(string name, string organisationId, string domain)
    Task<IkonBackend.Secret> CreateSpaceSecretAsync(string spaceId, string key, string value, string? name = null, string? description = null)
    Task<string> DelegateSpaceTokenAsync(string spaceId, string userId)
    Task DeleteAppBundleAsync(string id)
    Task DeleteChannelAsync(string id)
    Task<IkonBackend.ChannelInstance> DeleteChannelInstanceAsync(string id)
    Task DeleteDatabaseAsync(string databaseId)
    Task DeleteInboundEmailAsync(string id)
    Task DeleteItemAsync(string id)
    Task DeletePluginAsync(string id)
    Task DeleteProfileFileAsync(string profileId, string assetId)
    Task DeleteSpaceApiKeyAsync(string id)
    Task DeleteSpaceAsync(string id)
    Task DeleteSpaceSecretAsync(string id)
    // Local-dev parity: drop this process's externally-managed registration on shutdown so the backend stops reverse-proxying {space}.ikonai.app/api/... to a relay tunnel that is no longer listening. localInstanceId is the id the register call returned. Best-effort and idempotent.
    Task DeregisterLocalInstanceAsync(string spaceId, string channelId, string localInstanceId)
    static IkonBackend.EnvironmentType DetermineEnvironment(string url)
    Task<HttpResponseMessage> DownloadInboundEmailAttachmentAsync(string emailId, string attachmentId)
    Task<List<IkonBackend.Profile>> FindProfilesAsync(string spaceId, Dictionary<string, string> filters, int maxResults = 1000)
    // Returns an IkonBackend that authenticates with token while sharing the global instance's backend URL. Lets a process issue backend requests on behalf of a caller whose space-scoped token differs from its own — e.g. an RPC proxy resolving assets that live in the caller's space.
    static IkonBackend ForToken(string token)
    Task<List<IkonBackend.Translation>> GetAllTranslationsAsync(string spaceId, int maxResults = 1000)
    Task<Dictionary<string, string>> GetApiKeysAsync(bool all = false)
    Task<IkonBackend.AppBundle> GetAppBundleAsync(string id)
    Task<List<IkonBackend.AppBundle>> GetAppBundlesAsync(string spaceId, IkonBackend.AppBundleState? state = null, int maxResults = 1000)
    Task<IkonBackend.CursorResponse<IkonBackend.SpaceEventRow>> GetAppEventsAsync(string spaceId, int days, int limit, string? cursor = null)
    Task<IkonBackend.AppPaymentsStatusResult> GetAppPaymentsStatusAsync(string spaceId)
    Task<IkonBackend.ChannelInstanceSession> GetAppSessionAsync(string sessionId)
    Task<IkonBackend.IkonLogQueryResult> GetAppSessionLogsAsync(string sessionId, int? level = null, string? cursor = null, int limit = 200)
    Task<IkonBackend.CursorResponse<IkonBackend.ChannelInstanceSession>> GetAppSessionsAsync(string spaceId, string? cursor = null, int limit = 50, string? searchId = null)
    Task<string> GetAssetSignedUrlAsync(string assetId)
    Task<List<IkonBackend.BillingProduct>> GetBillingProductsAsync()
    Task<IkonBackend.BillingStatusResult> GetBillingStatusAsync(string organisationId)
    Task<List<IkonBackend.BillingTransaction>> GetBillingTransactionsAsync(string organisationId, int maxResults = 100)
    Task<IkonBackend.Channel> GetChannelAsync(string id)
    Task<IkonBackend.ChannelInstance> GetChannelInstanceAsync(string id)
    Task<List<IkonBackend.ChannelInstance>> GetChannelInstancesAsync(string? spaceId = null, string? userId = null, string scope = "all", int maxResults = 1000)
    Task<List<IkonBackend.Channel>> GetChannelsAsync(string spaceId, int maxResults = 1000)
    Task<List<IkonBackend.ChatMessage>> GetChatMessagesAsync(string channelInstanceId, int maxResults = 1000)
    Task<List<IkonBackend.SpaceCostEventName>> GetCostEventNamesAsync(string spaceId)
    Task<List<IkonBackend.SpaceCostScope>> GetCostScopesAsync(string spaceId)
    Task<List<IkonBackend.SpaceCostRow>> GetCostsAsync(string spaceId, string startDate, string endDate, string? category = null, string? eventName = null, IReadOnlyList<IkonBackend.SpaceCostScopeFilter>? scopes = null)
    Task<IkonBackend.User> GetCurrentUserAsync()
    Task<List<IkonBackend.CustomField>> GetCustomFieldsAsync(string spaceId, int maxResults = 1000)
    Task<IkonBackend.DatabaseConnectionResponse> GetDatabaseConnectionAsync(string databaseId, string? via = null)
    Task<List<IkonBackend.Database>> GetDatabasesForSpaceAsync(string spaceId, int maxResults = 20)
    Task<string> GetEntitlementAsync(string offerId, string customerKey, CancellationToken cancellationToken = null)
    Task<IkonBackend.Folder> GetFolderByPathAsync(string spaceId, string path)
    Task<List<IkonBackend.Folder>> GetFoldersAsync(string spaceId, string path, int maxResults = 1000)
    static IEnumerable<string> GetIkonDataDirectoryCandidates()
    Task<InboundEmailDetailDto> GetInboundEmailAsync(string id)
    Task<InboundEmailPageDto> GetInboundEmailsAsync(string? recipient, string? fromAddress, DateTimeOffset? since, DateTimeOffset? until, int? limit, string? cursor)
    Task<IkonBackend.Item> GetItemAsync(AssetUri assetUri)
    Task<IkonBackend.ItemDownloadUrl> GetItemSignedDownloadUrlAsync(string id)
    Task<IkonBackend.ItemSignedUpload> GetItemSignedUploadUrlAsync(string uri, string filename, string mime, string[]? tags, bool? isAppServed = null, DateTime? expiresAt = null)
    Task<List<IkonBackend.Item>> GetItemsAsync(string spaceId, string folderId, int maxResults = 1000)
    Task<IkonBackend.LocalIkonServerTokenResponse> GetLocalIkonServerTokenAsync(string spaceId)
    Task<IkonBackend.Profile> GetOrCreateCurrentProfileAsync(string spaceId)
    Task<IkonBackend.Organisation> GetOrganisationAsync(string id)
    Task<List<IkonBackend.OrganisationInvitation>> GetOrganisationInvitationsAsync(string organisationId, int maxResults = 100)
    Task<List<IkonBackend.Organisation>> GetOrganisationsAsync(int maxResults = 1000)
    Task<string> GetPaymentsAsync(string customerKey, CancellationToken cancellationToken = null)
    Task<IkonBackend.Pipeline> GetPipelineAsync(string id)
    Task<IkonBackend.Pipeline?> GetPipelineByTypeNameAsync(string spaceId, string typeName)
    Task<List<IkonBackend.Pipeline>> GetPipelinesAsync(string spaceId, int maxResults = 1000)
    Task<IkonBackend.Plugin> GetPluginAsync(string id)
    Task<List<IkonBackend.Plugin>> GetPluginsAsync(string spaceId, int maxResults = 1000)
    Task<IkonBackend.Profile> GetProfileAsync(string spaceId, string userId)
    Task<List<IkonBackend.Profile>> GetProfilesAsync(string spaceId, int maxResults = 1000)
    Task<IkonBackend.RelayServerConfigResponse?> GetRelayServerConfigAsync()
    Task<List<string>> GetReleaseNoteVersions(string target)
    Task<List<IkonBackend.ReleaseNoteEntry>> GetReleaseNotes(string target, string? minVersion = null, int maxResults = 100)
    Task<Dictionary<string, string>> GetSecretsAsync(string spaceId)
    Task<GetSignatureOrderResponse> GetSignatureOrderAsync(string orderId)
    Task<IkonBackend.SpaceApiKey> GetSpaceApiKeyAsync(string id)
    Task<List<IkonBackend.SpaceApiKey>> GetSpaceApiKeysAsync(string spaceId, int maxResults = 1000)
    Task<IkonBackend.Space> GetSpaceAsync(string id)
    Task<IkonBackend.SpaceGitRepository> GetSpaceGitRepositoryAsync(string spaceId)
    Task<List<IkonBackend.Secret>> GetSpaceSecretsAsync(string spaceId, int maxResults = 1000)
    Task<List<IkonBackend.Space>> GetSpacesAsync(string organisationId, int maxResults = 1000)
    Task<List<IkonBackend.Space>> GetSpacesAsync(string organisationId, string search, int maxResults = 100)
    Task<StepUpAssertionResponse> GetStepUpAssertionAsync(string challengeId, string userId)
    Task<T> GetStorageAsync<T>(string spaceId, string entity, string entityId, IEnumerable<string> keys) where T : new()
    Task<string> GetSubscriptionsAsync(string customerKey, CancellationToken cancellationToken = null)
    Task<IkonBackend.Translation> GetTranslationAsync(string spaceId, string text, string locale, string description)
    Task<IkonBackend.TurnServerCredentialsResponse?> GetTurnServerCredentialsAsync(int sessionId)
    Task<IkonBackend.User> GetUserAsync(string id)
    Task<List<IkonBackend.User>> GetUsersAsync(string query, int limit = 20)
    bool HasCapability(string capability)
    Task IngestPaymentsProviderEventAsync(string providerEventJson, CancellationToken cancellationToken = null)
    Task<IkonBackend.AppPaymentsInitResult> InitAppPaymentsAsync(string spaceId, string mode = "ikon-connect", string provider = "stripe")
    Task<bool> IsSpaceDomainAvailableAsync(string domain)
    Task<IkonBackend.AppPaymentsOffersResult> ListAppPaymentsOffersAsync(string spaceId)
    Task<IkonBackend.ItemListResponse> ListItemsAsync(IkonBackend.ItemListRequest request)
    Task<string> ListOffersAsync(CancellationToken cancellationToken = null)
    bool Login(ValueTuple<string, string>? fromCommandLine = null, ValueTuple<string, string>? fromConfig = null, bool logSource = true, bool mustLogin = true)
    Task<List<IkonBackend.MintEndpointGrantResult>> MintEndpointGrantsAsync(IEnumerable<IkonBackend.MintEndpointGrantRequest> grants)
    static IkonBackend.LoginInfo? ReadLoginConfig()
    Task<IkonBackend.CampaignRedeemResult> RedeemCampaignAsync(string code, string organisationId)
    Task<string> RefundPaymentAsync(string paymentId, long? amountMinor = null, string? reason = null, string? idempotencyKey = null, string? provider = null, CancellationToken cancellationToken = null)
    // Local-dev parity: register this locally-run process as an externally-managed instance so the backend reverse-proxies {space}.ikonai.app/api/... to this machine's relay tunnel instead of provisioning a cloud instance. The backend mints a per-registration id (returned as LocalInstanceId ) that distinguishes this instance from other local runs sharing the same identity. Returns that id, which the host passes into MintUrl so its minted endpoint URLs carry the li claim and route to this process.
    Task<IkonBackend.RegisterLocalInstanceResponse> RegisterLocalInstanceAsync(string spaceId, string channelId, Dictionary<string, string> sessionIdentity, string relayEndpointPublicUrl)
    Task RegisterPushSubscriptionAsync(RegisterPushSubscriptionDto request)
    Task<IkonBackend.AppPaymentsRemoveResult> RemoveAppPaymentsMerchantAsync(string spaceId, string? provider = null)
    Task<IkonBackend.AppPaymentsRemoveResult> RemoveAppPaymentsOfferAsync(string spaceId, string offerId, string? provider = null)
    Task<bool> RemoveOfferAsync(string offerId, string? provider = null, CancellationToken cancellationToken = null)
    Task RemoveOrganisationInvitationAsync(string organisationId, string invitationId)
    Task<IkonBackend.Organisation> RemoveOrganisationUserAsync(string organisationId, string userId)
    Task RemovePushSubscriptionAsync(RemovePushSubscriptionDto request)
    Task<string> RequestAccessTokenAsync(string apiKey, string spaceId, string externalUserId)
    Task<IkonBackend.ChannelInstance> RequestChannelAsync(IkonBackend.RequestChannelRequest request)
    Task<StepUpStartResponse> RequestStepUpStartAsync(StepUpStartRequest request)
    void ResetCounters()
    Task ResetProfileAsync(string profileId)
    // Revoke a single endpoint grant by its id (denylisted edge-side).
    Task RevokeEndpointGrantAsync(string grantId)
    // Revoke every endpoint grant minted under a shared group tag.
    Task RevokeEndpointGrantGroupAsync(string group)
    Task SendEmailAsync(SendEmailDto request)
    void SendMessage(ProtocolMessage message)
    Task SendPushAsync(SendPushDto request)
    Task SetStorageAsync(string spaceId, string entity, string entityId, Dictionary<string, object> values)
    Task StopAsync()
    Task<IkonBackend.AppBundle> UpdateAppBundleAsync(string id, IkonBackend.AppBundleState state)
    Task<IkonBackend.Channel> UpdateChannelAsync(string id, object form)
    Task<IkonBackend.ChannelInstance> UpdateChannelInstanceAsync(string id, object form)
    Task<IkonBackend.Item> UpdateItemAsync(AssetUri assetUri, object text, string[]? tags, DateTime? ifUpdatedAt = null, DateTime? expiresAt = null)
    Task<IkonBackend.Pipeline> UpdatePipelineAsync(string id, object form)
    Task<IkonBackend.Plugin> UpdatePluginAsync(string id, object form)
    Task UpdateProfileFieldAsync(string profileId, object form)
    Task UpdateProfileFieldAsync(string profileId, string field, string value)
    Task UpdateProfileFieldAsync(string profileId, string field, List<string> value)
    Task<IkonBackend.Space> UpdateSpaceAsync(string id, object form)
    Task<IkonBackend.Secret> UpdateSpaceSecretAsync(string id, string key, string value, string? name = null, string? description = null)
    Task UploadFileAsync(string url, string mime, string filePath)
    Task<IkonBackend.FileUploadResponse> UploadProfileFileAsync(string profileId, string type, string filename, string mime)
    static void WriteLoginConfig(IkonBackend.LoginInfo info)
    static string DevelopmentAuthEndpointUrl
    static string DevelopmentBackendEndpointUrl
    static string ProductionAuthEndpointUrl
    static string ProductionBackendEndpointUrl
  class IkonBackend.IkonLogEntry
    ctor()
    Dictionary<string, string>? Labels { get; set; }
    JsonElement Message { get; set; }
    string? SenderId { get; set; }
    string? SenderType { get; set; }
    JsonElement Severity { get; set; }
    JsonElement Timestamp { get; set; }
  class IkonBackend.IkonLogQueryResult
    ctor()
    string? Cursor { get; set; }
    List<IkonBackend.IkonLogEntry> Logs { get; set; }
  sealed class InboundEmailAddressDto
    ctor()
    string Email { get; set; }
    string? Name { get; set; }
    string? Subaddress { get; set; }
  sealed class InboundEmailAttachmentDto
    ctor()
    string Filename { get; set; }
    string Id { get; set; }
    string MimeType { get; set; }
    long Size { get; set; }
  sealed class InboundEmailDetailDto
    ctor()
    List<InboundEmailAttachmentDto> Attachments { get; set; }
    string? BodyHtml { get; set; }
    string? BodyText { get; set; }
    List<InboundEmailAddressDto> Cc { get; set; }
    InboundEmailAddressDto From { get; set; }
    List<InboundEmailHeaderDto> Headers { get; set; }
    string Id { get; set; }
    DateTimeOffset ReceivedAt { get; set; }
    string Recipient { get; set; }
    string? ReplyTo { get; set; }
    double? SpamScore { get; set; }
    string Subject { get; set; }
    string? Tag { get; set; }
    List<InboundEmailAddressDto> To { get; set; }
  sealed class InboundEmailHeaderDto
    ctor()
    string Name { get; set; }
    string Value { get; set; }
  sealed class InboundEmailPageDto
    ctor()
    List<InboundEmailSummaryDto> Items { get; set; }
    string? NextCursor { get; set; }
  sealed class InboundEmailSummaryDto
    ctor()
    List<InboundEmailAttachmentDto> Attachments { get; set; }
    string FromAddress { get; set; }
    string Id { get; set; }
    DateTimeOffset ReceivedAt { get; set; }
    string Recipient { get; set; }
    double? SpamScore { get; set; }
    string Subject { get; set; }
    string? Tag { get; set; }
  class IkonBackend.Item
    ctor()
    IkonBackend.ItemAsset Asset { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? ExpiresAt { get; set; }
    string Folder { get; set; }
    string Id { get; set; }
    bool? IsAppServed { get; set; }
    bool IsPrivate { get; set; }
    string Name { get; set; }
    string OrganisationId { get; set; }
    string SpaceId { get; set; }
    string[] Tags { get; set; }
    string Text { get; set; }
    string Type { get; set; }
    DateTime UpdatedAt { get; set; }
  class IkonBackend.ItemAsset
    ctor()
    string Filename { get; set; }
    string Mime { get; set; }
    string? NativeUri { get; set; }
    string Sha256 { get; set; }
    long Size { get; set; }
    string Url { get; set; }
  class IkonBackend.ItemDownloadUrl
    ctor(string url)
    string Url { get; set; }
  class IkonBackend.ItemListRequest
    ctor()
    string? ContinuationToken { get; set; }
    string? FolderPrefix { get; set; }
    int? Limit { get; set; }
    string? SpaceId { get; set; }
    string[]? Tags { get; set; }
  class IkonBackend.ItemListResponse
    ctor()
    List<IkonBackend.Item> Items { get; set; }
    string? NextPageToken { get; set; }
  class IkonBackend.ItemSignedUpload
    ctor()
    string Path { get; set; }
    string Url { get; set; }
  static class Json
    static Dictionary<string, object> AsDict(string json)
    static Dictionary<string, object> ConvertDict(Dictionary<string, object> dict)
    static T DeepCopy<T>(T obj)
    static string Format(string json, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false)
    static T From<T>(string json, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false, bool caseInsensitive = false)
    static object? From(string json, Type type, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false)
    static object? From(string json, string typeName, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false)
    static T FromLLMResponse<T>(string text, JsonSerializerOptions? options)
    static T FromLLMResponse<T>(string text, bool useJson5 = false, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false, bool caseInsensitive = false)
    static Type? ResolveTypeByName(string typeName)
    static string To<T>(T obj, bool useJson5 = false, bool indentation = true, bool includeFields = true, bool enumsAsNames = true, bool camelCase = false, bool includeNull = true, bool enumCamelCase = false)
  static class JwtHelper
    // Decodes and verifies a JWT token with HS256 signature. Throws if signature is invalid.
    static string Decode(string token, byte[] key)
    // Decodes a JWT token payload without verifying the signature. Use this only when signature verification is not required (e.g., reading claims on the client side).
    static string DecodePayload(string token)
    // Creates a JWT token with HS256 signature.
    static string Encode(string payload, byte[] key)
  class IkonBackend.LocalIkonServerTokenResponse
    ctor()
    string Token { get; set; }
  class Log : AsyncLocalInstance<Log>
    ctor()
    IList<IScopeKey> CurrentScopes { get; }
    bool ShowTimeDelta { get; set; }
    void AddDefaultLogHandlers()
    void AddLogEvent(LogEvent logEvent)
    void AddScope(IScopeKey scope)
    IDisposable? BeginTimer(string name, LogType logType = Debug, string filePath = "", int lineNumber = 0, string memberName = "")
    IDisposable CreateAsyncFlow(string? description = null)
    void Critical(LogCriticalHandler handler)
    void Critical(string message, string filePath = "", int lineNumber = 0, string memberName = "")
    void Debug(LogDebugHandler handler)
    void Debug(string message, string filePath = "", int lineNumber = 0, string memberName = "")
    void DisableFileOutput()
    void EnableFileOutput(string filePath, bool append = false)
    void Error(LogErrorHandler handler)
    void Error(string message, string filePath = "", int lineNumber = 0, string memberName = "")
    // Log an error with an associated exception. Convenience overload for the .NET-conventional logger.Error(message, exception) shape — the exception's full ToString() is appended to the message so stack traces land in the log without needing to interpolate ex into the message.
    void Error(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
    // Log an exception with an associated message — same as Error but with the exception first, matching the Serilog / Microsoft.Extensions.Logging idiom logger.LogError(ex, message).
    void Error(Exception exception, string message, string filePath = "", int lineNumber = 0, string memberName = "")
    void Event(string name, object? parameters = null, string filePath = "", int lineNumber = 0, string memberName = "")
    string Exception(LogExceptionHandler handler)
    string Exception(string message, string filePath = "", int lineNumber = 0, string memberName = "")
    TScope GetScope<TScope>() where TScope : struct, IScopeKey
    IScopeKey GetScopeByName(string name)
    void Info(LogInfoHandler handler)
    void Info(string message, string filePath = "", int lineNumber = 0, string memberName = "")
    Task InitializeAsync()
    void LogMessage(LogType type, LogGeneralHandler handler)
    void LogMessage(LogType type, string message, string filePath = "", int lineNumber = 0, string memberName = "")
    void LogMessage2(LogType type, string filePath, int lineNumber, string memberName, LogGeneralHandler2 handler)
    void LogMessage2(LogType type, string filePath, int lineNumber, string memberName, string message)
    static LogParameter<T> Named<T>(string name, T value)
    void RemoveDefaultLogHandlers()
    static Sensitive<T> Sensitive<T>(T value, SensitivityPolicy sensitivityPolicy = Default)
    Task StopAsync()
    void Trace(LogTraceHandler handler)
    void Trace(string message, string filePath = "", int lineNumber = 0, string memberName = "")
    TScope? TryGetScope<TScope>() where TScope : struct, IScopeKey
    bool TryGetScope<TScope>(out TScope scope) where TScope : struct, IScopeKey
    IScopeKey? TryGetScopeByName(string name)
    void Usage(string usageName, double usage, string filePath = "", int lineNumber = 0, string memberName = "")
    void Usage(string usageName, Func<Task<double>> usageFunc, string filePath = "", int lineNumber = 0, string memberName = "")
    IDisposable UseScope(IScopeKey scope)
    IDisposable UseScopes(params IScopeKey[] scopes)
    Task WaitEmptyAsync()
    void Warning(LogWarningHandler handler)
    void Warning(string message, string filePath = "", int lineNumber = 0, string memberName = "")
    static void WriteErrorToConsole(string message)
    static void WriteToConsole(string message, ConsoleColor color)
    static void WriteWarningToConsole(string message)
    bool BlockWhenFull
    LogFilter ConsoleWriterFilter
    LogFilter FileWriterFilter
    LogFilter Filter
    // Optional prefix rendered at the very start of every console/file log line (before the timestamp). Because Log is an async-local instance, each isolated server scope (e.g. an embedded preview/sandbox server vs the host app) has its own instance and can carry its own prefix, making interleaved stdout from multiple in-process servers attributable at a glance.
    string Prefix
    static bool RequireInitCall
    bool ShowAsyncFlow
    string TraceFilter
    static event Log.AsyncFlowFinishedHandler? AsyncFlowFinished
    event Log.LogEventHandler? LogEvent
  class LogEvent
    ctor()
    Dictionary<string, object?> GetParameters(bool includeExtraParameters = true)
    string? GetParametersAsJson(bool includeExtraParameters = true)
    int AsyncFlowId
    string EventName
    object? EventParameters
    string? EventParametersJsonRedacted
    int LineNumber
    string MemberName
    string Message
    LogEvent.Parameter[] Parameters
    string Path
    string Prefix
    int PreviousAsyncFlowId
    LogScopeEntry[] Scopes
    DateTime Time
    LogType Type
    double Usage
    string UsageName
  delegate Log.LogEventHandler
    void LogEventHandler(object sender, LogEvent logEvent)
  class LogEventSender
    // Creates a log-event sender. By default it forwards over the ambient (async-local) Instance . Pass an explicit backend to forward over a specific backend connection regardless of the ambient scope — used to route an embedded server's logs to the HOST's backend session (so preview/sandbox logs land under the host session) while the embedded server keeps its own backend for usages.
    ctor(IkonBackend? backend = null)
    void Flush()
    Task InitializeAsync(bool sendLogs = true, bool sendEvents = true, bool sendUsages = true)
    void OnLogEvent(object sender, LogEvent logEvent)
    Task StopAsync()
  enum LogFilter
    None
    Critical
    Error
    Warning
    Info
    Debug
    Trace
  struct LogParameter<T>
    ctor(string name, T value)
    string Name
    T Value
  struct LogScopeEntry
    string Id { get; set; }
    string Type { get; set; }
  class IkonBackend.LoginInfo
    ctor()
    string? DefaultOrganisationId
    string? DefaultOrganisationName
    string? DefaultSpaceId
    string? DefaultSpaceName
    string? IkonBackendEnvironment
    string? IkonBackendToken
    string? IkonBackendUrl
  class IkonBackend.MintEndpointGrantRequest
    ctor()
    string EndpointName { get; set; }
    int? ExpiresInSeconds { get; set; }
    string? Group { get; set; }
    string? LocalInstanceId { get; set; }
    string? RunTarget { get; set; }
    Dictionary<string, string> SessionIdentity { get; set; }
  class IkonBackend.MintEndpointGrantResult
    ctor()
    string EndpointName { get; set; }
    long? ExpiresAt { get; set; }
    string Grant { get; set; }
    string GrantId { get; set; }
  // Provides optimized utility methods for converting strings between different naming conventions.
  static class NameConversions
    static string ToCamelCase(string input)
    static string ToDisplayName(string input)
    static string ToKebabCase(string input)
    static string ToPascalCase(string input)
    static string ToSlug(string input, int maxLength)
    static string ToSnakeCase(string input)
  static class NodeVersionGate
    static Task EnsureCompatibleAsync(CancellationToken cancellationToken = null)
  class IkonBackend.Organisation
    ctor()
    string Id { get; set; }
    string Name { get; set; }
    List<IkonBackend.OrganisationUser> Users { get; set; }
  class IkonBackend.OrganisationInvitation
    ctor()
    string? CreatedAt { get; set; }
    string Email { get; set; }
    string Id { get; set; }
    string Role { get; set; }
  class IkonBackend.OrganisationUser
    ctor()
    string Role { get; set; }
    string UserId { get; set; }
  class OverrideConfig
    ctor()
    AppSourceType AppSourceType { get; set; }
    string DataDirectory { get; set; }
    bool DebugMode { get; set; }
    Dictionary<string, object>? DynamicConfig { get; set; }
    string IkonBackendToken { get; set; }
    string IkonBackendUrl { get; set; }
    PayloadType PayloadType { get; set; }
    ServerRunType ServerRunType { get; set; }
  struct LogEvent.Parameter
    ctor(string name, object? value)
    string Name
    object? Value
  class IkonBackend.Pipeline
    ctor()
    string? BundleAsset { get; set; }
    object? Config { get; set; }
    DateTime CreatedAt { get; set; }
    string DllName { get; set; }
    string? Guid { get; set; }
    string? Hash { get; set; }
    string Id { get; set; }
    string? Name { get; set; }
    string? OpenApiSpecJson { get; set; }
    string SpaceId { get; set; }
    string TypeName { get; set; }
    DateTime UpdatedAt { get; set; }
  class IkonBackend.Plugin
    ctor()
    List<string> Api { get; set; }
    string Developer { get; set; }
    string DllName { get; set; }
    Guid? Guid { get; set; }
    string Id { get; set; }
    string Name { get; set; }
    string NupkgAsset { get; set; }
    string ProductId { get; set; }
    string SpaceId { get; set; }
    List<string> Targets { get; set; }
    string Type { get; set; }
    string TypeName { get; set; }
  class PluginAttribute : Attribute
    ctor(string name, string productId, string description, int version, string guid, UserType userType, Opcode opcodeGroupsFromServer, Opcode opcodeGroupsToServer, bool receiveAllMessages, string[]? dependencies = null, ContextType contextType = Plugin)
    // How this connection identifies to the server: Plugin (default, server-side component, no UI/per-client scope) vs Browser/Native (a player CLIENT — gets a per-connection ClientScope + UI). Lets a non-web C# SDK client connect as a first-class player, not a backend plugin.
    ContextType ContextType { get; }
    string[] Dependencies { get; }
    string Description { get; }
    string Guid { get; }
    string Name { get; }
    Opcode OpcodeGroupsFromServer { get; }
    Opcode OpcodeGroupsToServer { get; }
    string ProductId { get; }
    bool ReceiveAllMessages { get; }
    UserType UserType { get; }
    int Version { get; }
  class PortalPluginConfig
    ctor()
    object DefaultConfig
    string FileName
    string FileNameJsonPath
    string Format
    string Name
    string TypeName
  static class ProcessGuard
    static void HandleOutOfMemory()
  static class ProcessRunner
    static ProcessRunner.Result Run(string command, bool ignoreErrors = false, bool runInBackground = false, bool runInNewWindow = false, bool attachToConsole = false, string? workingDirectory = null, string? stdinInput = null, IDictionary<string, string?>? environmentVariables = null, TimeSpan waitAfterCancel = null, bool captureBinaryOutput = false, CancellationToken cancellationToken = null)
    static Task<ProcessRunner.Result> RunAsync(string command, bool ignoreErrors = false, bool runInBackground = false, bool runInNewWindow = false, bool attachToConsole = false, string? workingDirectory = null, string? stdinInput = null, IDictionary<string, string?>? environmentVariables = null, TimeSpan waitAfterCancel = null, bool captureBinaryOutput = false, CancellationToken cancellationToken = null)
  class IkonBackend.Profile
    ctor()
    IkonBackend.Address? Address { get; set; }
    List<IkonBackend.Asset> Assets { get; set; }
    Dictionary<string, object?> Attributes { get; set; }
    IkonBackend.Asset? Avatar { get; }
    string? BirthDate { get; set; }
    string? Email { get; set; }
    string? FirstName { get; set; }
    string? Gender { get; set; }
    string Id { get; set; }
    string? Language { get; set; }
    string? LastName { get; set; }
    IkonBackend.ProfileModules? Modules { get; set; }
    string Name { get; set; }
    string? NativeLanguage { get; set; }
    string? PhoneNumber { get; set; }
    string? PreferredName { get; set; }
    List<string> Roles { get; set; }
    List<string>? SpokenLanguages { get; set; }
    string UserId { get; set; }
    string VisibleAddress { get; }
    string VisibleName { get; }
    string? GetStringAttribute(string name)
  static class ProfileExtensions
    static string? GetValueByPath(IkonBackend.Profile profile, string path)
  class IkonBackend.ProfileModules
    ctor()
    IkonBackend.ProfileProviderModule? Provider { get; set; }
  class IkonBackend.ProfileProviderModule
    ctor()
    bool? Accepted { get; set; }
    DateTime? AcceptedAt { get; set; }
    string? Provider { get; set; }
  sealed class ProtocolMessageHandlerRegistry
    ctor()
    bool HasHandlers { get; }
    ValueTask DispatchAsync(ProtocolMessage message)
    IDisposable Register(Func<ProtocolMessage, ValueTask> handler, Opcode? opcodeGroupMask = null, Opcode[]? opcodes = null)
  sealed class PublicApiDocIgnoreAttribute : Attribute
    ctor()
  // A reactive version of the protocol GlobalState. Each property is wrapped in a ReactiveT so that any UI binding to it will update only when the value changes.
  class ReactiveGlobalState
    ctor()
    // Tells the source where the app is being run from
    Reactive<AppSourceType> AppSourceType { get; }
    // Active audio streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.AudioStreamState>> AudioStreams { get; }
    // Unique identifier for the channel within the space
    Reactive<string> ChannelId { get; }
    // Display name of the channel
    Reactive<string> ChannelName { get; }
    // URL for accessing the channel
    Reactive<string> ChannelUrl { get; }
    // All connected clients indexed by their client session ID, containing client metadata such as user ID, device info, viewport dimensions, and locale
    Reactive<Dictionary<int, Context>> Clients { get; }
    // Whether debug mode is enabled, providing additional logging and development features
    Reactive<bool> DebugMode { get; }
    // User ID of the first human user who joined this session, dynamically reassigned when that user leaves
    Reactive<string> FirstUserId { get; }
    // Registry of callable functions organized by client session ID
    Reactive<Dictionary<int, List<ActionFunctionRegister>>> Functions { get; }
    // Display name of the organization
    Reactive<string> OrganisationName { get; }
    // Static user ID of the session owner from server configuration, used for user-specific asset storage paths
    Reactive<string> PrimaryUserId { get; }
    // Tells whether the app is being run through publicly accessible endpoints (in local development)
    Reactive<bool> PublicAccess { get; }
    // Tells where the server is running from
    Reactive<ServerRunType> ServerRunType { get; }
    // Unique identifier of the specific Ikon server instance handling this session
    Reactive<string> ServerSessionId { get; }
    // Full URL with session identifier for direct access to current session
    Reactive<string> SessionChannelUrl { get; }
    // Hash derived from the session identity parameters
    Reactive<string> SessionHash { get; }
    // Unique identifier for the space where this session is running
    Reactive<string> SpaceId { get; }
    // Display name of the space
    Reactive<string> SpaceName { get; }
    // Active tracking streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.TrackingStreamState>> TrackingStreams { get; }
    // Active UI streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.UIStreamState>> UIStreams { get; }
    // Active video streams indexed by stream ID
    Reactive<Dictionary<string, GlobalState.VideoStreamState>> VideoStreams { get; }
    // Returns the client context against the clientSesssionId
    Context GetClientContext(int clientSessionId)
    // Returns the first or null client context against the userId
    Context? GetClientContext(string userId)
    // Gets a collection of all human client contexts. This includes all clients whose UserType includes the Human flag.
    IEnumerable<Context> GetHumanClients()
    // Gets a collection of client contexts grouped by unique AuthSessionId. If a user has multiple clients, only the first one (by the iteration order) is returned.
    IEnumerable<Context> GetUniqueAuthClientContexts()
    // Gets a collection of client contexts grouped by unique AuthSessionId. If a user has multiple clients, only the first one (by the iteration order) is returned.
    IEnumerable<Context> GetUniqueHumanAuthClientContexts()
    // Updates the ReactiveGlobalState from a new GlobalState. Only those reactive properties that have actually changed will trigger notifications.
    void UpdateFrom(GlobalState newState)
  class UdpFragmentation.ReassemblyBuffer
    ctor()
    void EvictStale(long maxAgeTicks = 10000000)
    byte[]? TryReassemble(ReadOnlySpan<byte> datagram)
  class IkonBackend.RegisterLocalInstanceResponse
    ctor()
    string LocalInstanceId { get; set; }
    string SpacePublicUrl { get; set; }
  sealed class RegisterPushSubscriptionDto
    ctor()
    string? Auth { get; set; }
    string? Channel { get; set; }
    string? DeviceId { get; set; }
    string? Endpoint { get; set; }
    string? P256dh { get; set; }
    string Platform { get; set; }
    string? Token { get; set; }
    string User { get; set; }
  class IkonBackend.RelayServerConfigResponse
    ctor()
    string AuthToken { get; set; }
    string Hostname { get; set; }
    int Port { get; set; }
  class IkonBackend.ReleaseNoteEntry
    ctor()
    string Content { get; set; }
    string Target { get; set; }
    string Version { get; set; }
  class IkonBackend.ReleaseNoteVersionsResponse
    ctor()
    List<string> Results { get; set; }
  sealed class RemovePushSubscriptionDto
    ctor()
    string EndpointOrToken { get; set; }
  class IkonBackend.RequestChannelRequest
    ctor()
    string? Hash { get; set; }
    string? Key { get; set; }
    Dictionary<string, string>? Params { get; set; }
    string Space { get; set; }
  sealed class ProcessRunner.Result
    ctor()
    int ExitCode { get; set; }
    Process? Process { get; set; }
    string StdErr { get; set; }
    string StdOut { get; set; }
    byte[]? StdOutBytes { get; set; }
    bool Success { get; set; }
  class IkonBackend.Secret
    ctor()
    DateTime CreatedAt { get; set; }
    string Description { get; set; }
    string Id { get; set; }
    string Key { get; set; }
    string Name { get; set; }
    string OrganisationId { get; set; }
    string SpaceId { get; set; }
    DateTime UpdatedAt { get; set; }
    string Value { get; set; }
  // Read-only view of the space-scoped secrets (tokens, API keys, passwords) loaded from the Ikon backend. Apps receive a Secrets via app.Secrets; pipelines receive one via host.Secrets on IPipelineHost<TConfig>. Manage values from the CLI with ikon app secret set/list/delete. Rotating a secret while an app or pipeline is running only takes effect after a restart.
  sealed class Secrets
    string Item { get; }
    // Keys of all secrets available for this space. Values are intentionally not exposed in bulk.
    IReadOnlyCollection<string> Keys { get; }
    // Non-throwing lookup. Returns true and sets value when the key exists; returns false and sets value to null otherwise.
    bool TryGet(string key, out string? value)
  sealed class SendEmailAttachmentDto
    ctor()
    string ContentBase64 { get; set; }
    string Filename { get; set; }
    string MimeType { get; set; }
  sealed class SendEmailDto
    ctor()
    List<SendEmailAttachmentDto>? Attachments { get; set; }
    string Html { get; set; }
    Dictionary<string, string>? Metadata { get; set; }
    string? ReplyTo { get; set; }
    string Subject { get; set; }
    string? Text { get; set; }
    string To { get; set; }
  sealed class SendEmailResponseDto
    ctor()
    bool Accepted { get; set; }
  sealed class SendPushDto
    ctor()
    string? Body { get; set; }
    string? Channel { get; set; }
    string? Data { get; set; }
    string? IconUrl { get; set; }
    string? LaunchUrl { get; set; }
    string? Tag { get; set; }
    string Title { get; set; }
    string User { get; set; }
  class Sensitive<T>
    ctor(T value, SensitivityPolicy sensitivityPolicy = Default)
    bool IsSensitive { get; }
    SensitivityPolicy Policy { get; }
    T Value { get; }
  enum SensitivityPolicy
    Default
  // High performance FIFO queue for value-type arrays, like ArrayQueue`1 but with an O(1) amortized dequeue. The live elements are held contiguously at a sliding offset: dequeuing advances a head index instead of shifting every remaining element down to the front. The backing array is only compacted (live data slid back to index 0) when a later enqueue needs contiguous tail space, so a fill-then-drain cycle moves each element at most once. This matters for audio: draining a large buffer one small frame at a time — e.g. the speech mixer pulling 20 ms frames out of a multi-second TTS clip — is O(N) overall here, versus O(N^2) with the shift-on-every-dequeue ArrayQueue`1 . The public surface mirrors ArrayQueue`1 ; Span and the indexer address the live region (index 0 is the oldest element), so callers see identical logical behaviour.
  class SlidingArrayQueue<T> where T : struct
    ctor(int maxCapacity)
    ctor(int maxCapacity, int initialCapacity)
    int Capacity { get; }
    int Count { get; }
    int FreeCount { get; }
    T Item { get; }
    int MaxCapacity { get; }
    Span<T> Span { get; }
    void Clear()
    void Dequeue(Span<T> target, int skipCount, int count)
    void Dequeue(Span<T> target, int count)
    void DequeueMemory(int count)
    void Enqueue(ReadOnlySpan<T> source, int count)
    void Enqueue(ReadOnlySpan<T> source)
    void EnqueueMemory(int count)
    Memory<T> GetDequeueMemory(int skipCount, int count)
    Memory<T> GetEnqueueMemory(int count)
    // Releases excess buffer capacity, shrinking the backing array to fit the current content and resetting the head offset. Useful in long-lived queues that have hit a transient peak and now want to return memory (in particular Large Object Heap memory) to the GC.
    void TrimExcess()
  class IkonBackend.Space
    ctor()
    string Domain { get; set; }
    string Id { get; set; }
    IkonBackend.SpaceLanguages Languages { get; set; }
    string Layout { get; set; }
    string Name { get; set; }
    string OrganisationId { get; set; }
    string Region { get; set; }
    string Slug { get; set; }
  class IkonBackend.SpaceApiKey
    ctor()
    DateTime CreatedAt { get; set; }
    string Id { get; set; }
    string Key { get; set; }
    string SpaceId { get; set; }
    DateTime UpdatedAt { get; set; }
  class IkonBackend.SpaceCostEventName
    ctor()
    string Category { get; set; }
    string EventName { get; set; }
  class IkonBackend.SpaceCostRow
    ctor()
    string Category { get; set; }
    string Currency { get; set; }
    string Date { get; set; }
    string EventName { get; set; }
    double TotalCreditCost { get; set; }
    double TotalRawCost { get; set; }
    double TotalRawCostEur { get; set; }
    double TotalUsage { get; set; }
    int UsageCount { get; set; }
  class IkonBackend.SpaceCostScope
    ctor()
    string ScopeId { get; set; }
    string ScopeType { get; set; }
  class IkonBackend.SpaceCostScopeFilter
    ctor()
    string Type { get; set; }
    string? Value { get; set; }
  class IkonBackend.SpaceDomainAvailability
    ctor()
    bool Available { get; set; }
    string Domain { get; set; }
  class IkonBackend.SpaceEventRow
    ctor()
    string EventName { get; set; }
    JsonElement? Parameters { get; set; }
    string? SessionId { get; set; }
    string Time { get; set; }
  class IkonBackend.SpaceGitRepository
    ctor()
    string GitPassword { get; set; }
    string? GitRepositoryPath { get; set; }
    string GitRepositoryUrl { get; set; }
    string GitUsername { get; set; }
  class IkonBackend.SpaceLanguages
    ctor()
    List<string> AvailableLanguages { get; set; }
    string DefaultLanguage { get; set; }
    bool UseUserLocale { get; set; }
  static class StableFileWriter
    static bool SaveXml(XDocument document, string path)
    static Task<bool> SaveXmlAsync(XDocument document, string path, CancellationToken cancellationToken = null)
    static bool WriteAllText(string path, string content)
    static Task<bool> WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = null)
  sealed class StepUpAssertionResponse
    ctor()
    string? Assertion { get; set; }
    string? FailureCode { get; set; }
    string Status { get; set; }
  sealed class StepUpStartRequest
    ctor()
    List<string>? AcrValues { get; set; }
    string? AppCallbackUrl { get; set; }
    string? ClientReturnUrl { get; set; }
    int? MaxAgeSeconds { get; set; }
    string Purpose { get; set; }
    string? SpaceId { get; set; }
    string? UserId { get; set; }
  sealed class StepUpStartResponse
    ctor()
    string ChallengeId { get; set; }
    string RedirectUrl { get; set; }
  class IkonBackend.StorageResponse<T> where T : new()
    ctor()
    string Entity { get; set; }
    string EntityId { get; set; }
    T Values { get; set; }
  // String-distance utilities. Single home for primitives that otherwise breed private copies in every caller (Levenshtein had three implementations across Ikon.Code, Ikon.Agent.Codegen, and the MiniAgent app before being consolidated here).
  static class StringDistance
    // Standard-shape Levenshtein edit distance. Returns the minimum number of single-character insertions, deletions, or substitutions to turn a into b . Empty / null inputs return the length of the other side. O(|a|·|b|) time and memory; fine for the sub-100-char identifiers and filenames the platform compares.
    static int Levenshtein(string? a, string? b)
  static class Throttler
    static bool TryExecute(Action action, TimeSpan throttleInterval = null, string? extraKey = null)
  static class Toml
    static T From<T>(string toml) where T : class, new()
    static string To<T>(T obj) where T : class
  class IkonBackend.Translation
    ctor()
    string Text { get; set; }
    Dictionary<string, string> Translations { get; set; }
  class IkonBackend.TurnServerCredentialsResponse
    ctor()
    string Credential { get; set; }
    string Url { get; set; }
    string Username { get; set; }
  // Application-layer fragmentation and reassembly for UDP datagrams. Fragment header (4 bytes prepended to each datagram): MessageId (uint16) — rolling counter correlating fragments of one message FragmentIndex (byte) — 0-based index within the message FragmentFlags (byte) — bit 0 = last fragment (E), bit 1 = single/unfragmented (B+E) Single-datagram messages carry flags = 0x03 (B+E). Multi-fragment messages: first has flags = 0x02 (B), middle = 0x00, last = 0x01 (E).
  static class UdpFragmentation
    static void ForEachFragment<TState>(ReadOnlySpan<byte> messageData, int maxDatagramSize, TState state, Action<TState, byte[]> onFragment)
    static List<byte[]> Fragment(ReadOnlySpan<byte> messageData, int maxDatagramSize)
    static bool IsLastFragment(byte flags)
    static bool IsSingleFragment(byte flags)
    static void SetMessageId(byte[] datagram, ushort messageId)
    static int HeaderSize
    static int MaxFragmentsPerMessage
    static int MaxReassemblyBuffers
  class IkonBackend.User
    ctor()
    string? Email { get; set; }
    string Id { get; set; }
    string Name { get; set; }
  static class Utils
    static int FindAvailableTcpAndUdpPort(int startPort, HashSet<int>? usedPorts = null)
    static int FindAvailableUdpPortRange(int startPort, int count)
    static string GenerateDeviceId()
    static void OpenBrowser(string url)
    static bool TcpPortIsAvailable(int port)
    static bool UdpPortIsAvailable(int port)

namespace Ikon.Common.Core.Assets
  sealed class Asset : AsyncLocalInstance<Asset>, IAsyncDisposable
    ctor()
    IkonBackend? Backend { get; set; }
    Task AddStorageAsync(AssetClass assetClass, IStorage storage, bool startInBackground = false)
    Task DeleteAsync(AssetUri assetUri)
    ValueTask DisposeAsync()
    Task<bool> ExistsAsync(AssetUri assetUri)
    Task<T> GetAsync<T>(AssetUri assetUri) where T : class
    Task<byte[]> GetBytesAsync(AssetUri assetUri)
    Task<AssetContent<byte[]>> GetBytesWithMetadataAsync(AssetUri assetUri)
    Task<AssetMetadata> GetMetadataAsync(AssetUri assetUri)
    Task<IAsyncDisposable> GetOrUpdateWithMetadataAsync<T>(AssetUri assetUri, Func<AssetEventArgs, AssetContent<T>?, Task> onAsset, Func<AssetEventArgs, Task>? onAssetNotFound = null) where T : class
    Task<IAsyncDisposable> GetOrUpdateWithMetadataAsync<T>(AssetUri assetUri, Action<AssetEventArgs, AssetContent<T>?> onAsset, Func<AssetEventArgs, Task>? onAssetNotFound = null) where T : class
    Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    Task<string> GetTextAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<string>> GetTextWithMetadataAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<T>> GetWithMetadataAsync<T>(AssetUri assetUri) where T : class
    Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<AssetUri>> ListAsync(AssetClass assetClass, string? prefix = null, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<AssetUri>> ListAsync(AssetUri folderUri, CancellationToken cancellationToken = null)
    Task NotifyUpdateAsync(AssetUri assetUri)
    Task SetAsync<T>(AssetUri assetUri, T asset, AssetMetadata? metadata = null, CancellationToken cancellationToken = null) where T : class
    Task SetBytesAsync(AssetUri assetUri, byte[] bytes, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
    Task SetTextAsync(AssetUri assetUri, string text, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
    Task<T> TryGetAsync<T>(AssetUri assetUri) where T : class
    Task<byte[]?> TryGetBytesAsync(AssetUri assetUri)
    Task<AssetContent<byte[]>?> TryGetBytesWithMetadataAsync(AssetUri assetUri)
    Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    Task<string?> TryGetTextAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<string>?> TryGetTextWithMetadataAsync(AssetUri assetUri, Encoding? encoding = null)
    Task<AssetContent<T>?> TryGetWithMetadataAsync<T>(AssetUri assetUri) where T : class
    Task<AssetWriteResult> TrySetBytesAsync(AssetUri assetUri, byte[] bytes, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
    Task<AssetWriteResult> TrySetTextAsync(AssetUri assetUri, string text, AssetMetadata? metadata = null, CancellationToken cancellationToken = null)
  // Ambient override for the IkonBackend that cloud asset storages resolve against. While a scope is active, asset reads and writes that fall back to the default backend use Current instead of Instance . Lets a process resolve a caller's assets with the caller's space-scoped token when it acts on behalf of another space (e.g. the LLM RPC proxy). The scope is never set automatically; callers opt in explicitly with Use .
  static class AssetBackendScope
    static IkonBackend? Current { get; }
    static IDisposable Use(IkonBackend backend)
  // Asset class determines which storage backend is used to store/retrieve the asset.
  enum AssetClass
    LocalFile
    EmbeddedFile
    CloudFile
    CloudFilePublic
    CloudJson
    CloudProfile
  sealed class AssetContent<T> : IDisposable
    ctor(T content, AssetMetadata? metaData = null)
    T Content { get; }
    AssetMetadata? MetaData { get; }
    void Dispose()
  class AssetEventArgs : EventArgs
    ctor(AssetUri assetUri, AssetStatus status)
    AssetUri AssetUri { get; }
    AssetStatus Status { get; }
  struct AssetListingEntry
    ctor(AssetUri assetUri, AssetMetadata metadata)
    AssetUri AssetUri { get; }
    AssetMetadata Metadata { get; }
  struct AssetMetadata
    ctor(string? mimeType = null, long? size = null, DateTime? lastModified = null, string? url = null, bool? urlIsTemporal = null, string[]? tags = null, string? internalPath = null, string? storageId = null, string? nativeUri = null, bool? isAppServed = null, DateTime? expiresAt = null)
    DateTime? ExpiresAt { get; }
    string? InternalPath { get; }
    bool? IsAppServed { get; }
    DateTime? LastModified { get; }
    string? MimeType { get; }
    string? NativeUri { get; }
    long? Size { get; }
    string? StorageId { get; }
    string[]? Tags { get; }
    string? Url { get; }
    bool? UrlIsTemporal { get; }
  sealed class AssetQuery
    ctor(AssetClass assetClass)
    ctor(AssetUri folderUri)
    string? ChannelId { get; set; }
    AssetClass Class { get; }
    string? ContinuationToken { get; set; }
    string? EffectiveChannelId { get; }
    string? EffectiveFolderPrefix { get; }
    string? EffectiveSpaceId { get; }
    string? EffectiveUserId { get; }
    string? FolderPrefix { get; set; }
    AssetUri? FolderUri { get; set; }
    int? Limit { get; set; }
    string? NextContinuationToken { get; set; }
    string? SpaceId { get; set; }
    string[]? Tags { get; set; }
    string? UserId { get; set; }
    AssetQuery Clone()
  enum AssetStatus
    None
    Added
    Exists
    Changed
    Deleted
  // AssetUris are used to store and retrieve data on the Ikon platform. Use the asset class to select the storage backend. Space ID, User ID, and Channel ID are optional identifiers to scope the asset. Path is the location of the asset within the storage backend. It may include subdirectories and/or a file name. Query is optional and is not used for now. Example asset URIs: assets://space/12345/user/67890/channel/12345/cloud-file/images/photos/pic1.jpg assets://cloud-json/config/settings.json assets://space/12345/local-file/documents/report.pdf assets://embedded-file/logo.png
  struct AssetUri : IEquatable<AssetUri>
    ctor(string uriString)
    ctor(AssetClass assetClass, string? path = null, string? spaceId = null, string? userId = null, string? channelId = null, string? query = null)
    string? ChannelId { get; }
    AssetClass Class { get; }
    string FileName { get; }
    string Path { get; }
    string? Query { get; }
    static string Scheme { get; }
    string? SpaceId { get; }
    string? UserId { get; }
    static AssetUri FromFilesystemPath(string relativePathToRoot, AssetClass defaultAssetClass = LocalFile)
    static bool IsValid(string uriString)
    static string ToFilesystemPath(AssetUri assetUri)
    static bool TryParse(string uriString, out AssetUri assetUri, out string? failureReason)
    static bool TryParse(string uriString, out AssetUri assetUri)
    AssetUri With(AssetClass? assetClass = null, string? path = null, string? spaceId = null, string? userId = null, string? channelId = null, string? query = null)
  // Serializes AssetUri as its canonical URI string so it round-trips correctly. Without this, System.Text.Json cannot reconstruct the immutable get-only struct and falls back to default(AssetUri) on deserialization (losing the path, class, and scope identifiers).
  sealed class AssetUriJsonConverter : JsonConverter<AssetUri>
    ctor()
    override AssetUri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    override void Write(Utf8JsonWriter writer, AssetUri value, JsonSerializerOptions options)
  struct AssetWriteResult
    ctor(AssetWriteStatus status, AssetMetadata? metadata = null)
    bool IsConflict { get; }
    AssetMetadata? Metadata { get; }
    AssetWriteStatus Status { get; }
    bool Succeeded { get; }
  enum AssetWriteStatus
    NotFound
    Conflict
    Skipped
    Success
  interface IHashableStream
    abstract void SetSha256Hash(string? hash)
  interface IStorage : IAsyncDisposable
    abstract Task DeleteAsync(AssetUri assetUri)
    abstract Task<bool> ExistsAsync(AssetUri assetUri)
    abstract Task<AssetContent<Stream>> GetReadStreamAsync(AssetUri assetUri)
    abstract Task<Stream> GetWriteStreamAsync(AssetUri assetUri, AssetMetadata? metadata, CancellationToken cancellationToken)
    abstract Task<IReadOnlyList<AssetListingEntry>> ListAsync(AssetQuery query, CancellationToken cancellationToken)
    abstract Task StartAsync()
    abstract Task<AssetMetadata?> TryGetMetadataAsync(AssetUri assetUri)
    abstract Task WaitUntilQueueEmptyAsync()
    event Func<AssetEventArgs, Task> AssetEventAsync
  static class StorageExtensions
    static Task AddEmbeddedFileStorageAsync(Asset asset, Assembly? assembly = null, string resourceNamespace = "")

namespace Ikon.Common.Core.Auth
  sealed class StepUpAssertion : IEquatable<StepUpAssertion>
    ctor(string Issuer, string Audience, string Subject, long IssuedAt, long ExpiresAt, string Jti, string UserId, string ChallengeId, string Purpose, string? SpaceId, string IdentityScheme, string? AssuranceLevel, string EidSubjectHash, string? IdentifierHash, string? VerifiedName, string? Birthdate, long VerifiedAt, string IdTokenHash, IReadOnlyDictionary<string, object?> RawClaims)
    string? AssuranceLevel { get; init; }
    string Audience { get; init; }
    string? Birthdate { get; init; }
    string ChallengeId { get; init; }
    string EidSubjectHash { get; init; }
    long ExpiresAt { get; init; }
    string IdTokenHash { get; init; }
    string? IdentifierHash { get; init; }
    string IdentityScheme { get; init; }
    long IssuedAt { get; init; }
    string Issuer { get; init; }
    string Jti { get; init; }
    string Purpose { get; init; }
    IReadOnlyDictionary<string, object?> RawClaims { get; init; }
    string? SpaceId { get; init; }
    string Subject { get; init; }
    string UserId { get; init; }
    long VerifiedAt { get; init; }
    string? VerifiedName { get; init; }

namespace Ikon.Common.Core.CommandLineParser
  static class CommandLineParser
    static Task<ParseResult<TGlobalOptions>> ParseAsync<TGlobalOptions>(string[] args, bool globalOptionsOnly = false) where TGlobalOptions : new()
    // Parse and invoke a verb IN-PROCESS — the same pipeline the CLI runs from the shell, minus the process boundary. This is the programmatic face of "the tool is a command-line parser over the tool API": verbs resolve from the assemblies loaded in the host process (the CLI itself, or any host that references a tool assembly such as IkonTool.Default), so there is no external binary to drift out of sync with the code that calls it. Login-requiring verbs authenticate from the saved login / environment exactly like the CLI; unlike the CLI there is never an interactive login prompt — an unauthenticated call fails with a clear message instead.
    static Task<VerbRunResult> RunAsync(string[] args, CancellationToken cancellationToken = null)
  sealed class OptionAttribute : Attribute
    ctor(string name, string description, bool required = false, string[]? synonyms = null)
    string Description { get; }
    string Name { get; }
    bool Required { get; set; }
    string[]? Synonyms { get; }
  sealed class ParseResult<TGlobalOptions>
    ctor(bool success, string? errorMessage, bool helpRequested, string? helpText, TGlobalOptions globalOptions, VerbInfo? verbInfo, bool unknownVerb = false)
    string? ErrorMessage { get; }
    TGlobalOptions GlobalOptions { get; }
    bool HelpRequested { get; }
    string? HelpText { get; }
    bool Success { get; }
    bool UnknownVerb { get; }
    VerbInfo? VerbInfo { get; }
  sealed class PositionalOptionAttribute : Attribute
    ctor(int index, string name, string description, bool required = false)
    string Description { get; }
    int Index { get; }
    string Name { get; }
    bool Required { get; set; }
  sealed class RemainingArgsOptionAttribute : Attribute
    ctor(string description)
    string Description { get; }
  sealed class VerbAttribute : Attribute
    ctor(string verb, string description, string? category = null, bool loginNeeded = false, bool spaceTokenNeeded = false, bool developerOnly = false, string[]? synonyms = null)
    string? Category { get; }
    string Description { get; }
    bool DeveloperOnly { get; }
    bool LoginNeeded { get; }
    bool SpaceTokenNeeded { get; }
    string[]? Synonyms { get; }
    string Verb { get; }
  // Container for the verb cache, including a hash for validation.
  sealed class VerbCache
    ctor()
    string Hash { get; set; }
    List<VerbCacheEntry> Verbs { get; set; }
  // Represents a single cached verb entry for serialization.
  sealed class VerbCacheEntry
    ctor()
    string AssemblyName { get; set; }
    string Description { get; set; }
    bool DeveloperOnly { get; set; }
    bool LoginNeeded { get; set; }
    string MethodName { get; set; }
    string OptionsTypeAssemblyName { get; set; }
    string OptionsTypeFullName { get; set; }
    bool SpaceTokenNeeded { get; set; }
    string[]? Synonyms { get; set; }
    string TypeFullName { get; set; }
    string Verb { get; set; }
  sealed class VerbInfo
    ctor(string verb, bool loginNeeded, bool spaceTokenNeeded, object? options, Func<object?, CancellationToken, ValueTask> callback)
    Func<object?, CancellationToken, ValueTask> Callback { get; }
    bool LoginNeeded { get; }
    object? Options { get; }
    bool SpaceTokenNeeded { get; }
    string Verb { get; }
  static class VerbResolver
    // Loads verb cache from a JSON file and populates the internal verb dictionary.
    static bool LoadVerbCache(string path)
    // Writes the current verb cache to a JSON file.
    static void WriteVerbCache(string path, string hash)
  // Outcome of an in-process verb invocation ( RunAsync ). Invoked separates "the verb ran and failed" from "the command line didn't parse" — callers that auto-append arguments retry a parse failure, but must never re-run an invoked verb.
  sealed class VerbRunResult
    // Outcome of an in-process verb invocation ( RunAsync ). Invoked separates "the verb ran and failed" from "the command line didn't parse" — callers that auto-append arguments retry a parse failure, but must never re-run an invoked verb.
    ctor(bool success, bool invoked, string message)
    // True when parsing succeeded and the verb callback actually ran (even if it then failed).
    bool Invoked { get; }
    string Message { get; }
    bool Success { get; }

namespace Ikon.Common.Core.Email
  // Sender or recipient entry parsed from an inbound email envelope.
  sealed class EmailAddress : IEquatable<EmailAddress>
    // Sender or recipient entry parsed from an inbound email envelope.
    ctor(string Email, string? Name, string? Subaddress)
    string Email { get; init; }
    string? Name { get; init; }
    string? Subaddress { get; init; }
  // Represents a single attachment on an outgoing app email. Bytes is the raw binary content; the platform encodes it as base64 before sending it on the wire.
  sealed class EmailAttachment : IEquatable<EmailAttachment>
    // Represents a single attachment on an outgoing app email. Bytes is the raw binary content; the platform encodes it as base64 before sending it on the wire.
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  // A streaming attachment download. The caller owns the Content stream; dispose this object (e.g. await using) to release it.
  sealed class EmailAttachmentDownload : IAsyncDisposable
    // Decrypted attachment bytes streamed from the platform.
    Stream Content { get; }
    // The sender-supplied filename, sanitized by the platform.
    string Filename { get; }
    // The attachment's MIME type, as recorded at ingest time.
    string MimeType { get; }
    // The decrypted (plaintext) attachment size in bytes.
    long Size { get; }
    ValueTask DisposeAsync()
  // A single SMTP header preserved on an inbound email.
  sealed class EmailHeader : IEquatable<EmailHeader>
    // A single SMTP header preserved on an inbound email.
    ctor(string Name, string Value)
    string Name { get; init; }
    string Value { get; init; }
  // Specification for a custom email sent by an app through the platform mailer. The platform enqueues the send for asynchronous delivery and returns once the request has been accepted; transient delivery failures are retried server-side.
  sealed class EmailSendRequest : IEquatable<EmailSendRequest>
    // Specification for a custom email sent by an app through the platform mailer. The platform enqueues the send for asynchronous delivery and returns once the request has been accepted; transient delivery failures are retried server-side.
    ctor(string To, string Subject, string HtmlBody, string? TextBody = null, string? ReplyTo = null, IReadOnlyList<EmailAttachment>? Attachments = null, IReadOnlyDictionary<string, string>? Metadata = null)
    // Optional list of binary attachments. Up to 10 per email.
    IReadOnlyList<EmailAttachment>? Attachments { get; init; }
    // Pre-rendered HTML body of the email.
    string HtmlBody { get; init; }
    // Optional string key/value pairs forwarded to the mail provider for tracking.
    IReadOnlyDictionary<string, string>? Metadata { get; init; }
    // Optional Reply-To address. The visible From address is set by the platform.
    string? ReplyTo { get; init; }
    // Email subject line.
    string Subject { get; init; }
    // Optional plain-text fallback for clients that do not render HTML.
    string? TextBody { get; init; }
    // Recipient email address.
    string To { get; init; }
  // Lightweight metadata for an inbound email's attachment — does not include the body bytes. Fetch the body via the email service's DownloadAttachmentAsync.
  sealed class InboundAttachmentInfo : IEquatable<InboundAttachmentInfo>
    // Lightweight metadata for an inbound email's attachment — does not include the body bytes. Fetch the body via the email service's DownloadAttachmentAsync.
    ctor(string Id, string Filename, string MimeType, long Size)
    string Filename { get; init; }
    string Id { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
  // Full inbound email with decrypted body and parsed envelope. Attachments expose metadata only; fetch each one via the email service's DownloadAttachmentAsync.
  sealed class InboundEmailDetail : IEquatable<InboundEmailDetail>
    // Full inbound email with decrypted body and parsed envelope. Attachments expose metadata only; fetch each one via the email service's DownloadAttachmentAsync.
    ctor(string Id, string Recipient, string From, string Subject, string? BodyText, string? BodyHtml, IReadOnlyList<EmailAddress> To, IReadOnlyList<EmailAddress> Cc, string? ReplyTo, IReadOnlyList<EmailHeader> Headers, IReadOnlyList<InboundAttachmentInfo> Attachments, DateTimeOffset ReceivedAt, double? SpamScore, string? Tag)
    IReadOnlyList<InboundAttachmentInfo> Attachments { get; init; }
    string? BodyHtml { get; init; }
    string? BodyText { get; init; }
    IReadOnlyList<EmailAddress> Cc { get; init; }
    string From { get; init; }
    IReadOnlyList<EmailHeader> Headers { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Recipient { get; init; }
    string? ReplyTo { get; init; }
    double? SpamScore { get; init; }
    string Subject { get; init; }
    string? Tag { get; init; }
    IReadOnlyList<EmailAddress> To { get; init; }
  // Inbox-listing entry. Subject is decrypted server-side; body and attachment bytes are not included here — call EmailService.GetMessageAsync for the full message.
  sealed class InboundEmailSummary : IEquatable<InboundEmailSummary>
    // Inbox-listing entry. Subject is decrypted server-side; body and attachment bytes are not included here — call EmailService.GetMessageAsync for the full message.
    ctor(string Id, string Recipient, string From, string Subject, DateTimeOffset ReceivedAt, int AttachmentCount, double? SpamScore, string? Tag)
    int AttachmentCount { get; init; }
    string From { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Recipient { get; init; }
    double? SpamScore { get; init; }
    string Subject { get; init; }
    string? Tag { get; init; }
  // One page of inbox results. NextCursor is null when there are no more pages.
  sealed class InboxPage : IEquatable<InboxPage>
    // One page of inbox results. NextCursor is null when there are no more pages.
    ctor(IReadOnlyList<InboundEmailSummary> Items, string? NextCursor)
    IReadOnlyList<InboundEmailSummary> Items { get; init; }
    string? NextCursor { get; init; }
  // Filter and pagination parameters for an inbox listing.
  sealed class InboxQuery : IEquatable<InboxQuery>
    ctor()
    // Opaque cursor returned by a previous NextCursor . null requests the first page.
    string? Cursor { get; init; }
    // Filter to messages sent from this address. Case-insensitive.
    string? From { get; init; }
    // Maximum number of messages to return for this page. The platform clamps to [1, 100]; values outside that range are silently adjusted. Defaults to 25.
    int Limit { get; init; }
    // Filter to messages delivered to this recipient address. Case-insensitive.
    string? Recipient { get; init; }
    // Inclusive lower bound on the SMTP receive timestamp.
    DateTimeOffset? Since { get; init; }
    // Inclusive upper bound on the SMTP receive timestamp.
    DateTimeOffset? Until { get; init; }

namespace Ikon.Common.Core.Functions
  // The type of callback a registered function uses.
  enum CallbackType
    Sync
    Async
    AsyncEnumerable
  // Immutable representation of a function with metadata and optional callbacks. Consolidates FunctionInfo, RegisteredFunction, and KernelContext.Function into a single type.
  struct Function
    // JSON deserialization constructor. Resolves ReturnType from ReturnTypeName string. Creates a function without callbacks (for remote/metadata-only use).
    ctor(Guid id, string name, FunctionParameter[] parameters, string returnTypeName, string description, FunctionVisibility visibility, bool llmInlineResult, bool llmCallOnlyOnce, CallbackType callbackType, int? clientSessionId, bool requiresInstance = false, string? version = null)
    // Primary constructor for creating functions with callbacks.
    ctor(Guid id, string name, FunctionParameter[] parameters, Type returnType, string description, FunctionVisibility visibility, bool llmInlineResult, bool llmCallOnlyOnce, CallbackType callbackType, int? clientSessionId, Func<object?[], object?>? callback, Func<object?[], Task<object?>>? callbackAsync, Func<object?[], IAsyncEnumerable<object?>>? callbackAsyncEnumerable, MethodInfo? methodInfo = null, bool requiresInstance = false, PolicyDelegate? policy = null, string? version = null)
    // The type of callback (Sync, Async, or AsyncEnumerable).
    CallbackType CallbackType { get; }
    // The clientSessionId of the client who registered this function. Null means this is a local function (registered in this process).
    int? ClientSessionId { get; }
    // Description of what the function does. Passed to LLM for tool description.
    string Description { get; }
    // True if this function has a callback that can be invoked locally.
    bool HasCallback { get; }
    // True if this function has a policy attached.
    bool HasPolicy { get; }
    // Unique identifier for this function.
    Guid Id { get; }
    // True if this function is local (registered in this process).
    bool IsLocal { get; }
    // True if this function is remote (registered by another client).
    bool IsRemote { get; }
    // If true, the LLM can only call this function once per generation pass.
    bool LlmCallOnlyOnce { get; }
    // If true, the LLM can inline the function result directly without tool call overhead.
    bool LlmInlineResult { get; }
    // The MethodInfo for the underlying method. Exposed so external introspection (e.g. the startup auth-marker audit in Ikon.App) can read method-level attributes. Null for delegate-based registrations, constructors, or remote functions.
    MethodInfo? MethodInfo { get; }
    // The name of the function (used for lookup and LLM tool name).
    string Name { get; }
    // The parameters of the function.
    FunctionParameter[] Parameters { get; }
    // Optional policy delegate for evaluating whether this function can be called. If null, the function is allowed to execute without policy checks.
    PolicyDelegate? Policy { get; }
    // True if this function requires an instance to be invoked. When true and no callback is set, the function is metadata-only and can only be invoked with a provided InstanceId.
    bool RequiresInstance { get; }
    // The return type of the function. Stored directly for performance. For async functions, this is the inner type (e.g., string for Task<string>). For async enumerable functions, this is the item type.
    Type ReturnType { get; }
    // The full name of the return type. Computed from ReturnType for JSON serialization.
    string ReturnTypeName { get; }
    // The version of the library that registered this function. Empty string means unversioned (legacy or latest).
    string Version { get; }
    // Whether the function should be distributed to other clients.
    FunctionVisibility Visibility { get; }
    // Calls the function synchronously. Only valid for local sync functions.
    object? Call(object?[] args)
    // Calls the function asynchronously. Only valid for local async functions.
    Task<object?> CallAsync(object?[] args)
    // Calls the function as an async enumerable call. Only valid for local async enumerable functions.
    IAsyncEnumerable<object?> CallAsyncEnumerable(object?[] args)
    // Calls the function synchronously and returns an enumerable result. Only valid for local sync functions whose result implements IEnumerable.
    IEnumerable<object?> CallEnumerable(object?[] args)
    static Function Create<TResult>(string name, string description, Func<TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, TResult>(string name, string description, Func<T1, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, TResult>(string name, string description, Func<T1, T2, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, TResult>(string name, string description, Func<T1, T2, T3, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, TResult>(string name, string description, Func<T1, T2, T3, T4, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, TResult> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function, PolicyDelegate? policy = null)
    static Function Create<TResult>(string name, string description, Func<Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, TResult>(string name, string description, Func<T1, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, TResult>(string name, string description, Func<T1, T2, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, TResult>(string name, string description, Func<T1, T2, T3, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, TResult>(string name, string description, Func<T1, T2, T3, T4, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, string description, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<TResult>(string name, string description, Func<IAsyncEnumerable<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, TResult>(string name, string description, Func<T1, IAsyncEnumerable<TResult>> function, PolicyDelegate? policy = null)
    static Function Create<T1, T2, TResult>(string name, string description, Func<T1, T2, IAsyncEnumerable<TResult>> function, PolicyDelegate? policy = null)
    // Creates a Function definition from a delegate.
    static Function Register(Delegate function, string? name = null, FunctionAttribute? attribute = null, MethodInfo? methodInfo = null, PolicyDelegate? policy = null, Dictionary<string, string>? paramDescriptions = null)
    static Function Register<TResult>(Func<TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, TResult>(Func<T1, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, TResult>(Func<T1, T2, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<TResult>(Func<Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, TResult>(Func<T1, Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, TResult>(Func<T1, T2, Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<TResult>(Func<IAsyncEnumerable<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, TResult>(Func<T1, IAsyncEnumerable<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    static Function Register<T1, T2, TResult>(Func<T1, T2, IAsyncEnumerable<TResult>> function, string? name = null, FunctionAttribute? attribute = null, PolicyDelegate? policy = null)
    override string ToString()
    // Creates a new Function with modified properties. Null parameters keep existing values. Use clearClientSessionId=true to explicitly set ClientSessionId to null. Use clearPolicy=true to explicitly set Policy to null.
    Function With(Guid? id = null, string? name = null, FunctionParameter[]? parameters = null, Type? returnType = null, string? description = null, FunctionVisibility? visibility = null, bool? llmInlineResult = null, bool? llmCallOnlyOnce = null, CallbackType? callbackType = null, int? clientSessionId = null, Func<object?[], object?>? callback = null, Func<object?[], Task<object?>>? callbackAsync = null, Func<object?[], IAsyncEnumerable<object?>>? callbackAsyncEnumerable = null, MethodInfo? methodInfo = null, bool? requiresInstance = null, PolicyDelegate? policy = null, bool clearClientSessionId = false, bool clearMethodInfo = false, bool clearPolicy = false, string? version = null)
    // Returns a new Function with the specified parameter's AllowedValues set. Pass null to clear an existing override and fall back to the type-based enum (or no enum at all). Use together with WithParamDescription to ship dynamic enum + dynamic doc per pass: rebuild the Function at the start of each pass, plumb the current allowed transitions through the parameter description and the allowed-values list, and re-add to EmergePass.Tools.
    Function WithAllowedValues(string paramName, IReadOnlyList<string>? allowedValues)
    // Returns a new Function with the specified parameter's description updated.
    Function WithParamDescription(string paramName, string description)
  // Marks a method as a registerable function for the FunctionRegistry. Used for auto-registration via RegisterFromInstance/RegisterFromType/RegisterFromAssembly.
  class FunctionAttribute : Attribute
    ctor()
    ctor(string description, bool llmInlineResult = false, bool llmCallOnlyOnce = false)
    // Description of what the function does. Passed to LLM for tool description.
    string Description { get; set; }
    // If true, the LLM can only call this function once per generation pass.
    bool LlmCallOnlyOnce { get; set; }
    // If true, the LLM can inline the function result directly without tool call overhead.
    bool LlmInlineResult { get; set; }
    // Override the function name. If null, the full type name plus method name is used.
    string? Name { get; set; }
    // Override the inherited TypeId property with JsonIgnore for serialization.
    object TypeId { get; }
    // Whether the function should be distributed to other clients. If not set, defaults to Local for standalone functions, or inherits from [RegisterAll] for methods in a class with that attribute.
    FunctionVisibility Visibility { get; set; }
  // Per-call ambient context exposed to the body of a function dispatched by FunctionRegistry . Set by the registry's inbound dispatch path before invoking the function and cleared after.
  static class FunctionCallContext
    // The session id of the client that issued the current function call, or null when the call did not originate from a remote client (e.g. local in-process invocation).
    static int? CallerSessionId { get; }
  // Metadata about a function parameter.
  struct FunctionParameter
    // Primary constructor with Type directly.
    ctor(int index, string name, string description, Type type, bool hasDefaultValue, object? defaultValue, IReadOnlyList<string>? allowedValues = null)
    // JSON deserialization constructor. Resolves Type from TypeName string.
    ctor(int index, string name, string description, string typeName, bool hasDefaultValue, object? defaultValue, IReadOnlyList<string>? allowedValues = null)
    // Optional override for the JSON-schema enum field emitted to the LLM. When non-null, the schema uses these values instead of Enum.GetNames(Type). Lets callers narrow a static enum at registration time (e.g. "of these 7 enum members, only these 3 are valid right now") or attach an enum to a non-enum parameter type (e.g. a string field whose allowed values come from runtime state). Pair with Description rebuilds for dynamic per-call documentation.
    IReadOnlyList<string>? AllowedValues { get; }
    // The default value if HasDefaultValue is true.
    object? DefaultValue { get; }
    // Description of the parameter. Used by LLM for tool parameter descriptions.
    string Description { get; }
    // Whether the parameter has a default value.
    bool HasDefaultValue { get; }
    // The position of the parameter in the parameter list (0-based).
    int Index { get; }
    // Whether the parameter type is a nullable value type (e.g. int?, bool?).
    bool IsNullableValueType { get; }
    // The name of the parameter.
    string Name { get; }
    // The CLR type of the parameter. Stored directly for performance.
    Type Type { get; }
    // The full name of the parameter type. Computed from Type for JSON serialization. Nullable value types are unwrapped to their underlying type for remote schema compatibility.
    string TypeName { get; }
    override string ToString()
  // Central registry for functions that can be called locally or remotely. Supports both local and shared (distributed) function scopes.
  class FunctionRegistry : AsyncLocalInstance<FunctionRegistry>, BuiltInApprovalHandlers.IApprovalProtocolBridge
    ctor()
    // Optional resolver that maps a caller session id to the auth session id. Returns null or empty for unauthenticated (guest) callers.
    Func<int, string?>? AuthSessionIdResolver { get; set; }
    // All registered functions grouped by name.
    IReadOnlyDictionary<string, IReadOnlyList<Function>> Functions { get; }
    // Invoked at the start of a remote function call execution. Runs in the async context of the executing function, so subscribers can set AsyncLocal state.
    static Action? RemoteCallExecutionStarting { get; set; }
    // When set, the dispatcher rejects any remote call whose restored scopes carry no BackendTokenScope with a space claim. Turned on by delegating proxy hosts (e.g. the Ikon.AI library) that make platform-key calls on behalf of a caller and must never execute for an unidentified caller. Off by default so ordinary RPC hosts are unaffected.
    bool RequireVerifiedCallerSpace { get; set; }
    // Optional resolver that maps a caller session id to the set of roles the caller holds. Wired by the host (e.g. Ikon.App.App) so that RequireRoleAttribute / RoleBasedPolicy can gate calls. Returns an empty/null collection for callers without any roles. The dispatcher copies the result into AdditionalContext under the key RolesContextKey .
    Func<int, IReadOnlyCollection<string>?>? RolesResolver { get; set; }
    // Optional resolver that maps a caller session id to the reactive scopes that should be active during the function body's execution — typically [ClientScope, UserScope] derived from the caller's Context . Wired by the host (e.g. Ikon.App.App) so that ClientReactive`1 and UserReactive`1 resolve naturally without the function body having to push scopes manually via FunctionCallContext.CallerSessionId + Use .
    Func<int, IReadOnlyList<IScopeKey>>? ScopeResolver { get; set; }
    // Optional resolver that maps a caller session id to the user id associated with that session. Wired by the host (e.g. Ikon.App.App) so that policy evaluation has access to the caller's identity. Returns null for unknown sessions or unauthenticated (guest) callers.
    Func<int, string?>? UserIdResolver { get; set; }
    void AddFunction(Function function, FunctionVisibility? visibilityOverride = null)
    // Hooks the registry to a protocol channel so that remote function calls and registrations are handled automatically.
    Task AttachProtocolAsync(IProtocolMessageChannel channel, int senderId)
    TResult Call<TResult>(string name, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    Task<TResult> CallAsync<TResult>(string name, CancellationToken cancellationToken = null, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    Task CallAsync(string name, CancellationToken cancellationToken = null, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    IAsyncEnumerable<TItem> CallAsyncEnumerable<TItem>(string name, CancellationToken cancellationToken = null, object?[]? args = null, int? targetId = null, bool propagateScopes = false, string? version = null, Guid? instanceId = null)
    IEnumerable<TItem> CallEnumerable<TItem>(string name, object?[]? args = null)
    // Removes all locally registered functions. Remote functions are preserved.
    void ClearLocalFunctions()
    // Removes every remote function, keeping only this registry's own local functions. Called on protocol detach (disconnect): remote functions were mirrored from the now-gone peer and are re-synced fresh from the peer's ClientInitialization/GlobalState on reconnect. Without this, reconnecting to a RESTARTED peer (new FunctionIds / new session id) leaves the pre-disconnect remote functions behind, so the same name ends up registered by two client sessions and a name-only call throws "Multiple remote clients (...) have registered function '...'". Local functions are preserved — the client re-advertises them to the peer via StartProtocolAsync.
    void ClearRemoteFunctions()
    // Stops protocol handling and detaches the registry from the channel.
    void DetachProtocol()
    // Disposes a remote instance.
    Task DisposeInstanceAsync(Guid instanceId, int? targetId = null)
    // Gets all client session IDs that have registered a function with the given name.
    IReadOnlyCollection<int> GetClientSessionsWithFunction(string name)
    // Gets the function with the given name. Throws if multiple functions with the same name are registered (use Call/CallAsync with targetId parameter instead).
    Function? GetFunction(string name)
    // Gets the function with the given name, using argument types to resolve overloads.
    Function? GetFunction(string name, object?[] args)
    // Gets the function with the given name, using protocol parameter type names to resolve overloads. Used by the protocol handler when receiving remote calls.
    Function? GetFunction(string name, IReadOnlyList<FunctionParameter> protocolParameters)
    // Gets a local function with the given name and version, using protocol parameter type names to resolve overloads. If version is non-empty, tries exact version match first, then falls back to greatest version. If version is empty, selects the greatest versioned function or falls back to unversioned.
    Function? GetFunction(string name, IReadOnlyList<FunctionParameter> protocolParameters, string version)
    // Gets a function with the given name from a specific client session.
    Function? GetFunction(string name, int clientSessionId)
    // Gets all functions with the given name.
    IReadOnlyList<Function> GetFunctions(string name)
    // Checks if a function with the given name exists.
    bool HasFunction(string name)
    // Checks if a function with the given name exists for a specific client session.
    bool HasFunction(string name, int clientSessionId)
    // Invoke an already-resolved local function with a pre-built positional argument array, bypassing the argument-type resolution that CallAsync performs. The args must already line up with the function's parameter list — used by callers that inject host-supplied parameters (e.g. a cron trigger building the array from MethodInfo to inject a context object). Returns the result, if any.
    Task<object?> InvokeLocalAsync(Function function, object?[] args)
    // Scans an assembly for types with [RegisterAll] or methods with [Function] attributes and registers them.
    void RegisterFromAssembly(Assembly assembly, FunctionVisibility? visibilityOverride = null, string? version = null)
    // Scans an instance for [RegisterAll] attribute or methods with [Function] attribute and registers them.
    void RegisterFromInstance(object instance, FunctionVisibility? visibilityOverride = null, string? version = null)
    void RegisterFromType<T>(FunctionVisibility? visibilityOverride = null, string? version = null)
    // Scans a type for [RegisterAll] attribute or methods with [Function] attribute and registers them. For instance methods, you need to use RegisterFromInstance instead.
    void RegisterFromType(Type type, FunctionVisibility? visibilityOverride = null, string? version = null)
    // Registers a single method as a function unless one is already registered under the same name. Used by the app layer to register [Cron] methods, which are registrable like [Function] even when they carry no [Function] attribute. Idempotent: a method already registered (e.g. because it also carries [Function] under the same name) is left untouched. When name is null or empty the full member name ("{Type.FullName}.{Method}") is used.
    void RegisterFunctionMethod(object instance, MethodInfo method, string? name = null, FunctionVisibility visibility = Local)
    void RegisterFunctionsFromClientInitialization(ClientInitialization? clientInitialization)
    // Registers a remote function (from another client via protocol).
    void RegisterRemoteFunction(Guid id, string name, FunctionParameter[] parameters, Type returnType, string description, FunctionVisibility visibility, bool llmInlineResult, bool llmCallOnlyOnce, CallbackType callbackType, int clientSessionId, bool requiresInstance = false)
    bool RemoveFunction(string name, FunctionVisibility visibility)
    // Removes all local functions with the given name. Remote functions with the same name are preserved. Returns true if any functions were removed.
    bool RemoveFunction(string name)
    // Removes all functions registered by a specific client session (when client disconnects).
    void RemoveFunctionsByClientSessionId(int clientSessionId)
    // Sends registrations for all functions and processes pending registrations.
    Task StartProtocolAsync()
    // Stops protocol handling but keeps the channel attached. Pending registrations are cleared.
    Task StopProtocolAsync()
    void SyncFunctionsFromGlobalState(GlobalState globalState)
    // Tries to get a function with the given name.
    bool TryGetFunction(string name, out Function? function)
    // Waits for a function with the given name to be registered.
    Task<bool> WaitForFunctionAsync(string functionName, TimeSpan timeout = null, CancellationToken ct = null)
    // Fired when an approval flow completes (approved or rejected). Use this event for audit logging of approval decisions.
    event Action<ApprovalAuditEntry>? ApprovalCompleted
    // Fired when all of a client session's functions are removed because it disconnected ( RemoveFunctionsByClientSessionId ). Lets services that track per-session state — e.g. ReactiveSubscriptionService's subscriber set — release it promptly instead of discovering the dead session only when a later push fails.
    event Action<int>? ClientSessionRemoved
    // Fired when a function is registered.
    event Action<Function>? FunctionRegistered
    // Fired when a function is unregistered.
    event Action<string>? FunctionUnregistered
    // Fired when a policy is evaluated for a function call.
    event Action<PolicyEvaluationResult>? PolicyEvaluated
  sealed class FunctionResultWithData<T>
    ctor(T value, byte[] data)
    byte[] Data { get; }
    T Value { get; }
  static class FunctionUtils
    static ValueTuple<string?, string> DecodeFunctionName(string encodedFunctionName)
    static string EncodeFunctionName(string? typeName, string functionName)
  // Determines whether a function is advertised over the protocol so remote clients can call it. This is a dispatch-scope axis only — auth gating is a separate concern declared via policy attributes ([RequireLogin], [AllowAnonymous], [RequireRole], ...).
  enum FunctionVisibility
    Local
    External
  // Marks a class for automatic registration of all public members (methods, properties, constructors). Used for auto-registration via RegisterFromInstance/RegisterFromType/RegisterFromAssembly. Function names are automatically generated using the full type name (e.g., Namespace.Class.MethodName). Individual members can use [Function] to override defaults.
  class RegisterAllAttribute : Attribute
    ctor()
    // If true, the LLM can only call each function once per generation pass. Individual members can override this with [Function].
    bool LlmCallOnlyOnce { get; set; }
    // If true, the LLM can inline function results directly without tool call overhead. Individual members can override this with [Function].
    bool LlmInlineResult { get; set; }
    // Whether the functions should be distributed to other clients. Default is Local (not distributed).
    FunctionVisibility Visibility { get; set; }
  sealed class RemoteFunctionCallRequest
    ctor(string functionName)
    CancellationToken CancellationToken { get; set; }
    string FunctionName { get; }
    Guid? InstanceId { get; set; }
    object?[]? Parameters { get; set; }
    bool PropagateScopes { get; set; }
    int? TargetId { get; set; }
    string? Version { get; set; }
  sealed class RemoteFunctionCaller
    ctor(IProtocolMessageChannel protocolMessageChannel, int senderId = 0, TimeSpan? actionAckTimeout = null, TimeSpan? callTimeout = null, int? enumerationBufferCapacity = null)
    TResult Call<TResult>(RemoteFunctionCallRequest request)
    void Call(RemoteFunctionCallRequest request)
    Task<TResult> CallAsync<TResult>(RemoteFunctionCallRequest request)
    Task CallAsync(RemoteFunctionCallRequest request)
    IAsyncEnumerable<TItem> CallAsyncEnumerable<TItem>(RemoteFunctionCallRequest request)
    // Cancels all pending calls with a connection closed exception. Called when the underlying connection is lost.
    void CancelAllPendingCalls()
    // Cancels pending calls targeting a specific client with a target-disconnected exception. Called when a target client leaves so callers fail fast instead of waiting for the ack timeout.
    void CancelPendingCallsForTarget(int targetId)
    static object CreateAsyncEnumerableParameter<T>(IAsyncEnumerable<T> source)
    static object CreateEnumerableParameter<T>(IEnumerable<T> source)
    static FunctionParameter CreateParameter<T>(T value)
    static FunctionParameter CreateParameter(Type type, object? value)
    Task DisposeInstanceAsync(Guid instanceId, int? targetId = null)
  // Records which path the version-aware function lookup took. Surfaced in failure events so the analytics tool can distinguish "no match at all" from "fell back from the requested version".
  enum VersionResolution
    None
    Exact
    Greatest
    Unversioned
    Other

namespace Ikon.Common.Core.Functions.Policy
  sealed class PolicyDecision.Allow : PolicyDecision
  // Marks an External function as deliberately callable without authentication. Pure marker — does not inject a policy, only documents intent and silences the startup audit warning for External functions that have no auth policy attached.
  sealed class AllowAnonymousAttribute : Attribute
    ctor()
  // Represents an audit log entry for an approval decision.
  sealed class ApprovalAuditEntry
    // Creates a new approval audit entry.
    ctor(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, bool approved, string? reason, string policyName, DateTimeOffset timestamp)
    // The unique identifier for this approval request.
    Guid ApprovalId { get; }
    // True if the approval was granted; false if rejected.
    bool Approved { get; }
    // The session ID of the approver who responded to the request.
    int ApproverSessionId { get; }
    // The user ID of the approver, if available.
    string? ApproverUserId { get; }
    // The unique identifier for the function call that required approval.
    Guid CallId { get; }
    // The name of the function that required approval.
    string FunctionName { get; }
    // The name of the policy that required approval.
    string PolicyName { get; }
    // The reason for rejection, if rejected.
    string? Reason { get; }
    // The timestamp when the approval decision was made.
    DateTimeOffset Timestamp { get; }
    // Creates an audit entry for an approved request.
    static ApprovalAuditEntry CreateApproved(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, string policyName)
    // Creates an audit entry for a rejected request.
    static ApprovalAuditEntry CreateRejected(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, string? reason, string policyName)
  // Context passed to approval handlers containing all information needed to process an approval request.
  sealed class ApprovalContext
    // Public identifier for this approval request. Can be shared with callers to track which approval they're waiting for.
    Guid ApprovalId { get; }
    // Hash of the secret token that must be echoed back by the approver. The raw token is only provided to the designated approver via protocol.
    string ApprovalTokenHash { get; }
    // The arguments being passed to the function.
    object?[] Args { get; }
    // Hash of the serialized arguments, used for token binding.
    string ArgsHash { get; }
    // The original policy call context.
    PolicyCallContext CallContext { get; }
    // The session ID of the original caller.
    int CallerSessionId { get; }
    // The time when this approval request expires.
    DateTimeOffset ExpiresAt { get; }
    // The name of the function requiring approval.
    string FunctionName { get; }
    // The reason why approval is required.
    string Reason { get; }
    // The timeout in seconds for the approval request. Always at least PolicyDecision.MinExpirySeconds (30 seconds).
    int TimeoutSeconds { get; }
    // Creates a new ApprovalContext with generated IDs and returns both the context and the raw token. The raw token should only be sent to the designated approver.
    static ValueTuple<ApprovalContext, Guid> Create(string functionName, string reason, object?[] args, PolicyCallContext callContext, int timeoutSeconds = 300)
    // Checks if this approval request has expired.
    bool IsExpired()
    // Validates that a provided token matches this context. Uses constant-time comparison of hashes to prevent timing attacks.
    bool ValidateToken(Guid providedToken)
    // Validates that a provided token string matches this context.
    bool ValidateToken(string providedToken)
  // Delegate type for approval handlers that process approval requests.
  delegate ApprovalHandlerDelegate
    Task<ApprovalResult> ApprovalHandlerDelegate(ApprovalContext context)
  // The result of an approval request returned by approval handlers.
  struct ApprovalResult
    // True if the request was approved.
    bool IsApproved { get; }
    // The reason for rejection, if applicable.
    string? RejectionReason { get; }
    // Creates an approved result.
    static ApprovalResult Approved()
    // Creates a rejected result with an optional reason.
    static ApprovalResult Rejected(string? reason = null)
    override string ToString()
  // Specifies who should receive the approval request.
  enum ApproverType
    Caller
    SpecificClient
    SpecificUser
  // Provides built-in approval handlers that send approval requests via protocol messages.
  static class BuiltInApprovalHandlers
    // Default handler that sends the approval request to the original caller's session. This is used when no explicit handler is provided in PolicyDecision.NeedsApproval().
    static ApprovalHandlerDelegate AskCaller { get; }
    // Creates a handler that sends the approval request to a specific client.
    static ApprovalHandlerDelegate AskClient(int clientSessionId)
    // Creates a handler that sends the approval request to a specific user's active session(s).
    static ApprovalHandlerDelegate AskUser(string userId)
  sealed class PolicyDecision.Deny : PolicyDecision
    string? Code { get; }
    string Reason { get; }
  // Interface for function policies that can be evaluated before function execution.
  interface IFunctionPolicy
    // The name of this policy (used for logging and error messages).
    string Name { get; }
    // The priority of this policy. Lower values are evaluated first. Default priority is 100.
    int Priority { get; }
    // Evaluates the policy for a function call.
    abstract ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
  // Interface for checking usage limits before function execution.
  interface IUsageLimitChecker
    // Checks if the call should be allowed based on usage limits.
    abstract ValueTask<UsageLimitCheckResult> CheckAsync(PolicyCallContext context, object?[] args)
  // Denies a function call when the caller has no authenticated session.
  sealed class LoggedInPolicy : IFunctionPolicy
    ctor()
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    static string LoginRequiredCode
  sealed class PolicyDecision.NeedsApproval : PolicyDecision
    int ExpirySeconds { get; }
    ApprovalHandlerDelegate? Handler { get; }
    string Message { get; }
  // A policy that maintains separate rate limits per caller session.
  sealed class PerSessionRateLimitPolicy : IFunctionPolicy
    // Creates a new per-session rate limit policy.
    ctor(int limit, int windowSeconds, string? name = null, int priority = 50)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a PolicyDelegate from this policy.
    PolicyDelegate ToDelegate()
  // Helper methods for extracting typed arguments from policy evaluation arguments.
  static class PolicyArgs
    // Checks if all required arguments are present at the specified indices.
    static bool HasAll(object?[] args, params int[] requiredIndices)
    static T Optional<T>(object?[] args, int index, T defaultValue = null)
    static T Required<T>(object?[] args, int index)
    static bool TryGet<T>(object?[] args, int index, out T value)
  // Base class for policy attributes that can be applied to functions.
  abstract class PolicyAttribute : Attribute
    // The priority of this policy. Lower values are evaluated first.
    int Priority { get; set; }
    // Creates a policy instance from this attribute.
    abstract IFunctionPolicy CreatePolicy()
  // Applies a custom policy class to the function.
  sealed class PolicyAttribute<TPolicy> : PolicyAttribute where TPolicy : IFunctionPolicy, new()
    ctor()
    override IFunctionPolicy CreatePolicy()
  // Rich context object for policy evaluation providing access to all relevant information about the function call being evaluated.
  sealed class PolicyCallContext
    ctor(Guid callId, string functionName, int callerSessionId, string? userId, string? tenantId, Guid? instanceId, bool isInternal, CancellationToken cancellationToken, string? authSessionId = null, DateTime? callTimestamp = null, IReadOnlyDictionary<string, object?>? additionalContext = null)
    // Additional context data that may have been provided with the call.
    IReadOnlyDictionary<string, object?>? AdditionalContext { get; }
    // The auth session ID of the caller, if available. Empty or null for unauthenticated (guest) callers.
    string? AuthSessionId { get; }
    // The unique identifier for this function call.
    Guid CallId { get; }
    // The timestamp when the call was initiated.
    DateTime CallTimestamp { get; }
    // The session ID of the caller.
    int CallerSessionId { get; }
    // The cancellation token for this call.
    CancellationToken CancellationToken { get; }
    // The name of the function being called.
    string FunctionName { get; }
    // The instance ID if this is a call on a specific instance.
    Guid? InstanceId { get; }
    // True if this call originated from the same process (internal call).
    bool IsInternal { get; }
    // The tenant ID, if available.
    string? TenantId { get; }
    // The user ID of the caller, if available.
    string? UserId { get; }
  // Provides utilities for composing multiple policies into a single policy.
  static class PolicyChain
    // Creates a policy that requires all provided policies to allow. Policies are evaluated in priority order (lower priority = evaluated first). Evaluation stops at the first non-Allow decision.
    static IFunctionPolicy All(params IFunctionPolicy[] policies)
    // Creates a PolicyDelegate that requires all provided policies to allow.
    static PolicyDelegate AllAsDelegate(params IFunctionPolicy[] policies)
  // Represents a policy decision about whether a function call should be allowed. This is a discriminated union with three possible states: Allow, Deny, or NeedsApproval. Use pattern matching to handle the different cases.
  abstract class PolicyDecision
    // Creates an Allow decision.
    static PolicyDecision Allowed()
    // Creates a Deny decision with a reason and optional error code.
    static PolicyDecision Denied(string reason, string? code = null)
    // Creates a RequireApproval decision with default expiry.
    static PolicyDecision RequireApproval(string message)
    // Creates a RequireApproval decision with custom expiry.
    static PolicyDecision RequireApproval(string message, int expirySeconds)
    // Creates a RequireApproval decision with a custom approval handler.
    static PolicyDecision RequireApproval(string message, ApprovalHandlerDelegate handler)
    // Creates a RequireApproval decision with custom expiry and handler.
    static PolicyDecision RequireApproval(string message, int expirySeconds, ApprovalHandlerDelegate handler)
    // Default expiry time for approval requests in seconds.
    static int DefaultExpirySeconds
    // Minimum expiry time for approval requests in seconds.
    static int MinExpirySeconds
  // Delegate type for policy evaluation.
  delegate PolicyDelegate
    ValueTask<PolicyDecision> PolicyDelegate(object?[] args, PolicyCallContext context)
  // Contains the complete result of evaluating a function's policy.
  sealed class PolicyEvaluationResult
    ctor(PolicyDecision decision, string functionName, Guid callId, string? decidingPolicyName, TimeSpan evaluationDuration)
    // The call ID of the function call that was evaluated.
    Guid CallId { get; }
    // The name of the policy that caused a Deny or RequireApproval decision. Null if the decision is Allow.
    string? DecidingPolicyName { get; }
    // The final policy decision.
    PolicyDecision Decision { get; }
    // Time taken to evaluate the policy.
    TimeSpan EvaluationDuration { get; }
    // The name of the function that was evaluated.
    string FunctionName { get; }
    // True if the decision allows the function call to proceed.
    bool IsAllowed { get; }
    // True if the decision denies the function call.
    bool IsDenied { get; }
    // True if the decision requires approval before proceeding.
    bool RequiresApproval { get; }
    // Creates an Allow result (used when no policy is attached to a function).
    static PolicyEvaluationResult Allowed(string functionName, Guid callId)
    // Creates a Denied result.
    static PolicyEvaluationResult Denied(string functionName, Guid callId, string? reason, string policyName, TimeSpan evaluationDuration)
    // Creates a Denied result with an error code.
    static PolicyEvaluationResult Denied(string functionName, Guid callId, string reason, string? code, string policyName, TimeSpan evaluationDuration)
    // Creates a RequiresApproval result.
    static PolicyEvaluationResult NeedsApproval(PolicyDecision decision, string functionName, Guid callId, string policyName, TimeSpan evaluationDuration)
    override string ToString()
  // Non-generic version of PolicyAttribute for use when generic attributes are not supported.
  sealed class PolicyTypeAttribute : PolicyAttribute
    // Creates a new policy type attribute.
    ctor(Type policyType)
    // The type of policy to create.
    Type PolicyType { get; }
    override IFunctionPolicy CreatePolicy()
  // Applies a rate limit policy to the function.
  sealed class RateLimitAttribute : PolicyAttribute
    // Creates a new rate limit attribute.
    ctor(int limit, int windowSeconds)
    // Maximum number of calls allowed in the window.
    int Limit { get; }
    // If true, rate limit is per-session. If false (default), it's global.
    bool PerSession { get; set; }
    // The time window in seconds.
    int WindowSeconds { get; }
    override IFunctionPolicy CreatePolicy()
  // A policy that limits the rate of function calls.
  sealed class RateLimitPolicy : IFunctionPolicy
    // Creates a new rate limit policy.
    ctor(int limit, int windowSeconds, string? name = null, int priority = 50)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a PolicyDelegate from this policy.
    PolicyDelegate ToDelegate()
  // Requires approval before the function can execute.
  sealed class RequireApprovalAttribute : PolicyAttribute
    // Creates a new require approval attribute.
    ctor()
    // The type of approver to ask.
    ApproverType ApproverType { get; set; }
    // The client session ID to ask for approval (only used when ApproverType is SpecificClient).
    int ClientSessionId { get; set; }
    // The reason why approval is required.
    string Reason { get; set; }
    // The user ID to ask for approval (only used when ApproverType is SpecificUser).
    string? UserId { get; set; }
    override IFunctionPolicy CreatePolicy()
  // A policy that always requires approval before the function can execute.
  sealed class RequireApprovalPolicy : IFunctionPolicy
    // Creates a new require approval policy that asks the caller for approval.
    ctor(string reason, string? name = null, int priority = 100)
    // Creates a new require approval policy with a custom approval handler.
    ctor(string reason, ApprovalHandlerDelegate handler, string? name = null, int priority = 100)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a new require approval policy that asks a specific client.
    static RequireApprovalPolicy ForClient(string reason, int clientSessionId, string? name = null, int priority = 100)
    // Creates a new require approval policy that asks a specific user.
    static RequireApprovalPolicy ForUser(string reason, string userId, string? name = null, int priority = 100)
    // Creates a PolicyDelegate from this policy.
    PolicyDelegate ToDelegate()
  // Marks a function as requiring an authenticated user session.
  sealed class RequireLoginAttribute : PolicyAttribute
    ctor()
    override IFunctionPolicy CreatePolicy()
  // Requires the caller to hold one (or all, when RequireAll is true) of the specified roles. Roles are sourced from PolicyCallContext.AdditionalContext["user_roles"], which the dispatcher populates via RolesResolver .
  sealed class RequireRoleAttribute : PolicyAttribute
    ctor(params string[] roles)
    // When false (default), the caller passes if they hold ANY of the listed roles. When true, the caller must hold ALL listed roles.
    bool RequireAll { get; set; }
    // The roles the caller must hold (any or all, see RequireAll ).
    string[] RequiredRoles { get; }
    override IFunctionPolicy CreatePolicy()
  // Policy that denies the call unless the caller has the required role(s). Roles are read from PolicyCallContext.AdditionalContext["user_roles"].
  sealed class RoleBasedPolicy : IFunctionPolicy
    ctor(string[] required, bool requireAll, int priority)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    static string MissingRoleCode
    static string RolesContextKey
  // Applies a usage limit policy to the function.
  sealed class UsageLimitAttribute : PolicyAttribute
    // Creates a new usage limit attribute with the specified checker type.
    ctor(Type checkerType)
    // The type of usage limit checker to use. Must implement IUsageLimitChecker and have a parameterless constructor.
    Type CheckerType { get; }
    override IFunctionPolicy CreatePolicy()
  // Result of a usage limit check.
  sealed class UsageLimitCheckResult
    // Whether the call is allowed.
    bool Allowed { get; }
    // The error code for denial (if not allowed).
    string? DenyCode { get; }
    // The reason for denial (if not allowed).
    string? DenyReason { get; }
    // Creates an allow result.
    static UsageLimitCheckResult Allow()
    // Creates a deny result with the specified reason and code.
    static UsageLimitCheckResult Deny(string reason, string? code = "usage_limit_exceeded")
  // A policy that checks for available credits/quota before execution.
  sealed class UsageLimitPolicy : IFunctionPolicy
    // Creates a new usage limit policy with the specified checker.
    ctor(IUsageLimitChecker checker, string? name = null, int priority = 10)
    string Name { get; }
    int Priority { get; }
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    // Creates a PolicyDelegate from this policy.
    PolicyDelegate ToDelegate()

namespace Ikon.Common.Core.Protocol
  sealed class Action : IProtocolMessagePayload
    ctor()
    ctor(string description, string actionId)
    string ActionId { get; set; }
    string Description { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static Action ReadFromTeleport(ReadOnlySpan<byte> data)
    static Action ReadFromTeleport(ReadOnlySpan<byte> data, Action? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionActive : IProtocolMessagePayload
    ctor()
    ctor(string description, bool isFinished)
    string Description { get; set; }
    bool IsFinished { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionActive ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionActive ReadFromTeleport(ReadOnlySpan<byte> data, ActionActive? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionAudioStop : IProtocolMessagePayload
    ctor()
    ctor(string audioStreamId, float fadeoutTimeInSec)
    string AudioStreamId { get; set; }
    float FadeoutTimeInSec { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionAudioStop ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionAudioStop ReadFromTeleport(ReadOnlySpan<byte> data, ActionAudioStop? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionCall : IProtocolMessagePayload
    ctor()
    ctor(string actionId, string callId, string callArgumentsJson)
    string ActionId { get; set; }
    string CallArgumentsJson { get; set; }
    string CallId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionCall ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionCall ReadFromTeleport(ReadOnlySpan<byte> data, ActionCall? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionCall2 : IProtocolMessagePayload
    ctor()
    ctor(Guid actionId, string payloadJson, Guid callId)
    Guid ActionId { get; set; }
    Guid CallId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PayloadJson { get; set; }
    static ActionCall2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionCall2 ReadFromTeleport(ReadOnlySpan<byte> data, ActionCall2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionCallAck : IProtocolMessagePayload
    ctor()
    ctor(Guid callId)
    Guid CallId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionCallAck ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionCallAck ReadFromTeleport(ReadOnlySpan<byte> data, ActionCallAck? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionCallResult : IProtocolMessagePayload
    ctor()
    ctor(string callId, string resultJson)
    string CallId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string ResultJson { get; set; }
    static ActionCallResult ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionCallResult ReadFromTeleport(ReadOnlySpan<byte> data, ActionCallResult? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionCallText : IProtocolMessagePayload
    ctor()
    ctor(string actionId, string text)
    string ActionId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Text { get; set; }
    static ActionCallText ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionCallText ReadFromTeleport(ReadOnlySpan<byte> data, ActionCallText? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionCancelGeneration : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionCancelGeneration ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionCancelGeneration ReadFromTeleport(ReadOnlySpan<byte> data, ActionCancelGeneration? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionClassificationResult.ActionClassificationDetail
    ctor()
    ctor(string label, string originalCategory, bool isFlagged, double score)
    bool IsFlagged { get; set; }
    string Label { get; set; }
    string OriginalCategory { get; set; }
    double Score { get; set; }
    static ActionClassificationResult.ActionClassificationDetail ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionClassificationResult.ActionClassificationDetail ReadFromTeleport(ReadOnlySpan<byte> data, ActionClassificationResult.ActionClassificationDetail? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionClassificationResult : IProtocolMessagePayload
    ctor()
    ctor(bool isFlagged, List<ActionClassificationResult.ActionClassificationDetail> details)
    List<ActionClassificationResult.ActionClassificationDetail> Details { get; set; }
    bool IsFlagged { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionClassificationResult ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionClassificationResult ReadFromTeleport(ReadOnlySpan<byte> data, ActionClassificationResult? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionClearChatMessageHistory : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionClearChatMessageHistory ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionClearChatMessageHistory ReadFromTeleport(ReadOnlySpan<byte> data, ActionClearChatMessageHistory? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionClearState : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionClearState ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionClearState ReadFromTeleport(ReadOnlySpan<byte> data, ActionClearState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionCustomUserMessage : IProtocolMessagePayload
    ctor()
    ctor(int? opcode, string? typeName, string? mimeType, string? jsonPayload, byte[]? binaryPayload)
    byte[]? BinaryPayload { get; set; }
    string? JsonPayload { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string? MimeType { get; set; }
    int? Opcode { get; set; }
    string? TypeName { get; set; }
    static ActionCustomUserMessage ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionCustomUserMessage ReadFromTeleport(ReadOnlySpan<byte> data, ActionCustomUserMessage? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionDownload : IProtocolMessagePayload
    ctor()
    ctor(string fileName, string mime, string data)
    string Data { get; set; }
    string FileName { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Mime { get; set; }
    static ActionDownload ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionDownload ReadFromTeleport(ReadOnlySpan<byte> data, ActionDownload? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionEnterFullscreen : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionEnterFullscreen ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionEnterFullscreen ReadFromTeleport(ReadOnlySpan<byte> data, ActionEnterFullscreen? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadAck : IProtocolMessagePayload
    ctor()
    ctor(string actionId, int sequenceId)
    string ActionId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SequenceId { get; set; }
    static ActionFileUploadAck ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadAck ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadAck? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadAck2 : IProtocolMessagePayload
    ctor()
    ctor(string uploadId, int sequenceId)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SequenceId { get; set; }
    string UploadId { get; set; }
    static ActionFileUploadAck2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadAck2 ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadAck2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadBegin : IProtocolMessagePayload
    ctor()
    ctor(string actionId, string fileName, string mime, int byteCount, bool checkHash, string hash)
    string ActionId { get; set; }
    int ByteCount { get; set; }
    bool CheckHash { get; set; }
    string FileName { get; set; }
    string Hash { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Mime { get; set; }
    static ActionFileUploadBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadBegin ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadCallback : IProtocolMessagePayload
    ctor()
    ctor(string actionId, string fileName, string mime, long size, string filePath)
    string ActionId { get; set; }
    string FileName { get; set; }
    string FilePath { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Mime { get; set; }
    long Size { get; set; }
    static ActionFileUploadCallback ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadCallback ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadCallback? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadComplete2 : IProtocolMessagePayload
    ctor()
    ctor(string uploadId)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string UploadId { get; set; }
    static ActionFileUploadComplete2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadComplete2 ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadComplete2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadData : IProtocolMessagePayload
    ctor()
    ctor(string actionId, byte[] data, int sequenceId)
    string ActionId { get; set; }
    byte[] Data { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SequenceId { get; set; }
    static ActionFileUploadData ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadData ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadData? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadData2 : IProtocolMessagePayload
    ctor()
    ctor(string uploadId, byte[] data, int sequenceId)
    byte[] Data { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SequenceId { get; set; }
    string UploadId { get; set; }
    static ActionFileUploadData2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadData2 ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadData2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadEnd : IProtocolMessagePayload
    ctor()
    ctor(string actionId)
    string ActionId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionFileUploadEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadEnd ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadEnd2 : IProtocolMessagePayload
    ctor()
    ctor(string uploadId)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string UploadId { get; set; }
    static ActionFileUploadEnd2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadEnd2 ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadEnd2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadPreStart2 : IProtocolMessagePayload
    ctor()
    ctor(string uploadId, string uploadActionId, string fileName, string mime, long byteCount)
    long ByteCount { get; set; }
    string FileName { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Mime { get; set; }
    string UploadActionId { get; set; }
    string UploadId { get; set; }
    static ActionFileUploadPreStart2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadPreStart2 ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadPreStart2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadPreStartResponse2 : IProtocolMessagePayload
    ctor()
    ctor(string uploadId, bool accepted)
    bool Accepted { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string UploadId { get; set; }
    static ActionFileUploadPreStartResponse2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadPreStartResponse2 ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadPreStartResponse2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadResult : IProtocolMessagePayload
    ctor()
    ctor(string actionId, bool isSuccess, string errorMessage)
    string ActionId { get; set; }
    string ErrorMessage { get; set; }
    bool IsSuccess { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionFileUploadResult ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadResult ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadResult? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadStart2 : IProtocolMessagePayload
    ctor()
    ctor(string uploadId, string hash)
    string Hash { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string UploadId { get; set; }
    static ActionFileUploadStart2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadStart2 ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadStart2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFileUploadStartResponse2 : IProtocolMessagePayload
    ctor()
    ctor(string uploadId, bool accepted)
    bool Accepted { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string UploadId { get; set; }
    static ActionFileUploadStartResponse2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFileUploadStartResponse2 ReadFromTeleport(ReadOnlySpan<byte> data, ActionFileUploadStartResponse2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionAck : IProtocolMessagePayload
    ctor()
    ctor(Guid callId, Guid instanceId)
    Guid CallId { get; set; }
    Guid InstanceId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionFunctionAck ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionAck ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionAck? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionApprovalRequired : IProtocolMessagePayload
    ctor()
    ctor(Guid callId, Guid approvalId, Guid approvalToken, string functionName, string reason, string argsJson, int timeoutSeconds)
    Guid ApprovalId { get; set; }
    Guid ApprovalToken { get; set; }
    string ArgsJson { get; set; }
    Guid CallId { get; set; }
    string FunctionName { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Reason { get; set; }
    int TimeoutSeconds { get; set; }
    static ActionFunctionApprovalRequired ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionApprovalRequired ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionApprovalRequired? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionApprovalResponse : IProtocolMessagePayload
    ctor()
    ctor(Guid approvalId, Guid approvalToken, bool approved, string rejectionReason)
    Guid ApprovalId { get; set; }
    Guid ApprovalToken { get; set; }
    bool Approved { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string RejectionReason { get; set; }
    static ActionFunctionApprovalResponse ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionApprovalResponse ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionApprovalResponse? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionAwaitingApproval : IProtocolMessagePayload
    ctor()
    ctor(Guid callId, Guid approvalId, string functionName, string reason, int timeoutSeconds)
    Guid ApprovalId { get; set; }
    Guid CallId { get; set; }
    string FunctionName { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Reason { get; set; }
    int TimeoutSeconds { get; set; }
    static ActionFunctionAwaitingApproval ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionAwaitingApproval ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionAwaitingApproval? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionCall : IProtocolMessagePayload
    ctor()
    ctor(Guid functionId, Guid callId, Guid instanceId, string functionName, List<FunctionParameter> parameters, List<ActionFunctionCall.ScopeEntry> scopes, string version, List<ActionFunctionCall.UserCredentialEntry> userCredentials)
    Guid CallId { get; set; }
    Guid FunctionId { get; set; }
    string FunctionName { get; set; }
    Guid InstanceId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    List<FunctionParameter> Parameters { get; set; }
    List<ActionFunctionCall.ScopeEntry> Scopes { get; set; }
    List<ActionFunctionCall.UserCredentialEntry> UserCredentials { get; set; }
    string Version { get; set; }
    static ActionFunctionCall ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionCall ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionCall? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionCancel : IProtocolMessagePayload
    ctor()
    ctor(Guid callId, Guid instanceId)
    Guid CallId { get; set; }
    Guid InstanceId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionFunctionCancel ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionCancel ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionCancel? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionDispose : IProtocolMessagePayload
    ctor()
    ctor(Guid callId, Guid instanceId)
    Guid CallId { get; set; }
    Guid InstanceId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionFunctionDispose ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionDispose ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionDispose? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionEnumerationEnd : IProtocolMessagePayload
    ctor()
    ctor(Guid callId, Guid instanceId, Guid enumerationId)
    Guid CallId { get; set; }
    Guid EnumerationId { get; set; }
    Guid InstanceId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionFunctionEnumerationEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionEnumerationEnd ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionEnumerationEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionEnumerationItem : IProtocolMessagePayload
    ctor()
    ctor(Guid callId, Guid instanceId, Guid enumerationId, long itemIndex, string itemTypeName, string itemJson, byte[] itemData, byte[] itemTeleport)
    Guid CallId { get; set; }
    Guid EnumerationId { get; set; }
    Guid InstanceId { get; set; }
    byte[] ItemData { get; set; }
    long ItemIndex { get; set; }
    string ItemJson { get; set; }
    byte[] ItemTeleport { get; set; }
    string ItemTypeName { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionFunctionEnumerationItem ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionEnumerationItem ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionEnumerationItem? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionEnumerationItemBatch : IProtocolMessagePayload
    ctor()
    ctor(List<ActionFunctionEnumerationItem> items)
    List<ActionFunctionEnumerationItem> Items { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionFunctionEnumerationItemBatch ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionEnumerationItemBatch ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionEnumerationItemBatch? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionError : IProtocolMessagePayload
    ctor()
    ctor(Guid callId, Guid instanceId, string errorMessage, string errorTypeName, string stackTrace, string errorCode)
    Guid CallId { get; set; }
    string ErrorCode { get; set; }
    string ErrorMessage { get; set; }
    string ErrorTypeName { get; set; }
    Guid InstanceId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StackTrace { get; set; }
    static ActionFunctionError ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionError ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionError? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static ActionFunctionRegister ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionRegister ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionRegister? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionRegisterBatch : IProtocolMessagePayload
    ctor()
    ctor(List<ActionFunctionRegister> functions)
    List<ActionFunctionRegister> Functions { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionFunctionRegisterBatch ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionRegisterBatch ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionRegisterBatch? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionResult : IProtocolMessagePayload
    ctor()
    ctor(Guid callId, Guid instanceId, string resultTypeName, string resultJson, byte[] resultData, byte[] resultTeleport)
    Guid CallId { get; set; }
    Guid InstanceId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    byte[] ResultData { get; set; }
    string ResultJson { get; set; }
    byte[] ResultTeleport { get; set; }
    string ResultTypeName { get; set; }
    static ActionFunctionResult ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionResult ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionResult? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionGenerateAnswer : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionGenerateAnswer ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionGenerateAnswer ReadFromTeleport(ReadOnlySpan<byte> data, ActionGenerateAnswer? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionOpenChannel : IProtocolMessagePayload
    ctor()
    ctor(string channelCode, string prompt)
    string ChannelCode { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Prompt { get; set; }
    static ActionOpenChannel ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionOpenChannel ReadFromTeleport(ReadOnlySpan<byte> data, ActionOpenChannel? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionOpenExternalUrl : IProtocolMessagePayload
    ctor()
    ctor(string name, string url)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Name { get; set; }
    string Url { get; set; }
    static ActionOpenExternalUrl ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionOpenExternalUrl ReadFromTeleport(ReadOnlySpan<byte> data, ActionOpenExternalUrl? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionPan : IProtocolMessagePayload
    ctor()
    ctor(Coordinate2D location, Coordinate2D delta)
    Coordinate2D Delta { get; set; }
    Coordinate2D Location { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionPan ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionPan ReadFromTeleport(ReadOnlySpan<byte> data, ActionPan? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionPlaySound : IProtocolMessagePayload
    ctor()
    ctor(string url, int count, string id)
    int Count { get; set; }
    string Id { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Url { get; set; }
    static ActionPlaySound ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionPlaySound ReadFromTeleport(ReadOnlySpan<byte> data, ActionPlaySound? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionRegenerateAnswer : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionRegenerateAnswer ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionRegenerateAnswer ReadFromTeleport(ReadOnlySpan<byte> data, ActionRegenerateAnswer? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionReloadApplication : IProtocolMessagePayload
    ctor()
    ctor(string applicationId)
    string ApplicationId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionReloadApplication ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionReloadApplication ReadFromTeleport(ReadOnlySpan<byte> data, ActionReloadApplication? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionReloadChannels : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionReloadChannels ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionReloadChannels ReadFromTeleport(ReadOnlySpan<byte> data, ActionReloadChannels? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionReloadProfile : IProtocolMessagePayload
    ctor()
    ctor(string profileId, string userId)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string ProfileId { get; set; }
    string UserId { get; set; }
    static ActionReloadProfile ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionReloadProfile ReadFromTeleport(ReadOnlySpan<byte> data, ActionReloadProfile? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionReloadProvider : IProtocolMessagePayload
    ctor()
    ctor(string providerId)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string ProviderId { get; set; }
    static ActionReloadProvider ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionReloadProvider ReadFromTeleport(ReadOnlySpan<byte> data, ActionReloadProvider? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionScrollToContainer : IProtocolMessagePayload
    ctor()
    ctor(string containerId)
    string ContainerId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionScrollToContainer ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionScrollToContainer ReadFromTeleport(ReadOnlySpan<byte> data, ActionScrollToContainer? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionSetState : IProtocolMessagePayload
    ctor()
    ctor(string key, string typeName, string valueJson)
    string Key { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string TypeName { get; set; }
    string ValueJson { get; set; }
    static ActionSetState ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionSetState ReadFromTeleport(ReadOnlySpan<byte> data, ActionSetState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionSpeechRecognized : IProtocolMessagePayload
    ctor()
    ctor(bool wasSuccessful, string text)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Text { get; set; }
    bool WasSuccessful { get; set; }
    static ActionSpeechRecognized ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionSpeechRecognized ReadFromTeleport(ReadOnlySpan<byte> data, ActionSpeechRecognized? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionStartRecording : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionStartRecording ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionStartRecording ReadFromTeleport(ReadOnlySpan<byte> data, ActionStartRecording? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionStopRecording : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionStopRecording ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionStopRecording ReadFromTeleport(ReadOnlySpan<byte> data, ActionStopRecording? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionStopSound : IProtocolMessagePayload
    ctor()
    ctor(string id, float fadeoutTimeInSec)
    float FadeoutTimeInSec { get; set; }
    string Id { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionStopSound ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionStopSound ReadFromTeleport(ReadOnlySpan<byte> data, ActionStopSound? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionTap : IProtocolMessagePayload
    ctor()
    ctor(Coordinate2D location)
    Coordinate2D Location { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionTap ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionTap ReadFromTeleport(ReadOnlySpan<byte> data, ActionTap? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionTextOutput : IProtocolMessagePayload
    ctor()
    ctor(string userId, string text, bool generateChatMessage, string createdAt, ulong preciseCreatedAt)
    string CreatedAt { get; set; }
    bool GenerateChatMessage { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    ulong PreciseCreatedAt { get; set; }
    string Text { get; set; }
    string UserId { get; set; }
    static ActionTextOutput ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionTextOutput ReadFromTeleport(ReadOnlySpan<byte> data, ActionTextOutput? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionTextOutputDelta : IProtocolMessagePayload
    ctor()
    ctor(string delta)
    string Delta { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionTextOutputDelta ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionTextOutputDelta ReadFromTeleport(ReadOnlySpan<byte> data, ActionTextOutputDelta? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionTextOutputDeltaFull : IProtocolMessagePayload
    ctor()
    ctor(string full)
    string Full { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionTextOutputDeltaFull ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionTextOutputDeltaFull ReadFromTeleport(ReadOnlySpan<byte> data, ActionTextOutputDeltaFull? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionTriggerCron : IProtocolMessagePayload
    ctor()
    ctor(string functionName, string schedule, string fireTimeUtc)
    string FireTimeUtc { get; set; }
    string FunctionName { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Schedule { get; set; }
    static ActionTriggerCron ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionTriggerCron ReadFromTeleport(ReadOnlySpan<byte> data, ActionTriggerCron? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionTriggerGitPull : IProtocolMessagePayload
    ctor()
    ctor(bool forceFullRebuild, string? target)
    bool ForceFullRebuild { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string? Target { get; set; }
    static ActionTriggerGitPull ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionTriggerGitPull ReadFromTeleport(ReadOnlySpan<byte> data, ActionTriggerGitPull? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUIBlockingBegin : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionUIBlockingBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUIBlockingBegin ReadFromTeleport(ReadOnlySpan<byte> data, ActionUIBlockingBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUIBlockingEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionUIBlockingEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUIBlockingEnd ReadFromTeleport(ReadOnlySpan<byte> data, ActionUIBlockingEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUIClearStream : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionUIClearStream ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUIClearStream ReadFromTeleport(ReadOnlySpan<byte> data, ActionUIClearStream? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUICloseView : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionUICloseView ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUICloseView ReadFromTeleport(ReadOnlySpan<byte> data, ActionUICloseView? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUIDeleteContainer : IProtocolMessagePayload
    ctor()
    ctor(string containerId)
    string ContainerId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionUIDeleteContainer ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUIDeleteContainer ReadFromTeleport(ReadOnlySpan<byte> data, ActionUIDeleteContainer? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUIOpenView : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionUIOpenView ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUIOpenView ReadFromTeleport(ReadOnlySpan<byte> data, ActionUIOpenView? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUISetContainerStable : IProtocolMessagePayload
    ctor()
    ctor(string containerId)
    string ContainerId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionUISetContainerStable ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUISetContainerStable ReadFromTeleport(ReadOnlySpan<byte> data, ActionUISetContainerStable? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUIUpdateTextDelta : IProtocolMessagePayload
    ctor()
    ctor(string containerId, int elementId, string delta)
    string ContainerId { get; set; }
    string Delta { get; set; }
    int ElementId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ActionUIUpdateTextDelta ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUIUpdateTextDelta ReadFromTeleport(ReadOnlySpan<byte> data, ActionUIUpdateTextDelta? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUpdateGfxShader : IProtocolMessagePayload
    ctor()
    ctor(string name, float fps, string content, string contentHash)
    string Content { get; set; }
    string ContentHash { get; set; }
    float Fps { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Name { get; set; }
    static ActionUpdateGfxShader ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUpdateGfxShader ReadFromTeleport(ReadOnlySpan<byte> data, ActionUpdateGfxShader? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionUrlChanged : IProtocolMessagePayload
    ctor()
    ctor(string path)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Path { get; set; }
    static ActionUrlChanged ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionUrlChanged ReadFromTeleport(ReadOnlySpan<byte> data, ActionUrlChanged? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionZoom : IProtocolMessagePayload
    ctor()
    ctor(float startScale, float currentScale)
    float CurrentScale { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    float StartScale { get; set; }
    static ActionZoom ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionZoom ReadFromTeleport(ReadOnlySpan<byte> data, ActionZoom? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsEvents : IProtocolMessagePayload
    ctor()
    ctor(List<AnalyticsEvents.AnalyticsEventsItem> events)
    List<AnalyticsEvents.AnalyticsEventsItem> Events { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static AnalyticsEvents ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsEvents ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsEvents? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsEvents.AnalyticsEventsItem
    ctor()
    ctor(string time, string eventName, string message, string parameters)
    string EventName { get; set; }
    string Message { get; set; }
    string Parameters { get; set; }
    string Time { get; set; }
    static AnalyticsEvents.AnalyticsEventsItem ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsEvents.AnalyticsEventsItem ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsEvents.AnalyticsEventsItem? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsIkonProxyServerStats : IProtocolMessagePayload
    ctor()
    ctor(string time, int channelCount, double sentMessagesPerSecond, double sentMessagesBandwidthKb, int sentMessagesCount, double receivedMessagesPerSecond, double receivedMessagesBandwidthKb, int receivedMessagesCount, double cpuUsagePercentage, double processMemoryUsedMb, double managedMemoryUsedMb, string memoryInfo)
    int ChannelCount { get; set; }
    double CpuUsagePercentage { get; set; }
    double ManagedMemoryUsedMb { get; set; }
    string MemoryInfo { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    double ProcessMemoryUsedMb { get; set; }
    double ReceivedMessagesBandwidthKb { get; set; }
    int ReceivedMessagesCount { get; set; }
    double ReceivedMessagesPerSecond { get; set; }
    double SentMessagesBandwidthKb { get; set; }
    int SentMessagesCount { get; set; }
    double SentMessagesPerSecond { get; set; }
    string Time { get; set; }
    static AnalyticsIkonProxyServerStats ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsIkonProxyServerStats ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsIkonProxyServerStats? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsIkonRelayServerStats : IProtocolMessagePayload
    ctor()
    ctor(string time, int agentCount, int tcpTunnelCount, int udpTunnelCount, int tcpConnectionCount, double cpuUsagePercentage, double processMemoryUsedMb, double managedMemoryUsedMb, string memoryInfo)
    int AgentCount { get; set; }
    double CpuUsagePercentage { get; set; }
    double ManagedMemoryUsedMb { get; set; }
    string MemoryInfo { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    double ProcessMemoryUsedMb { get; set; }
    int TcpConnectionCount { get; set; }
    int TcpTunnelCount { get; set; }
    string Time { get; set; }
    int UdpTunnelCount { get; set; }
    static AnalyticsIkonRelayServerStats ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsIkonRelayServerStats ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsIkonRelayServerStats? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsIkonTurnServerStats : IProtocolMessagePayload
    ctor()
    ctor(string time, int activeAllocationCount, int activeConnectionCount, int channelBindingCount, long totalAllocationsCreated, double cpuUsagePercentage, double processMemoryUsedMb, double managedMemoryUsedMb, string memoryInfo)
    int ActiveAllocationCount { get; set; }
    int ActiveConnectionCount { get; set; }
    int ChannelBindingCount { get; set; }
    double CpuUsagePercentage { get; set; }
    double ManagedMemoryUsedMb { get; set; }
    string MemoryInfo { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    double ProcessMemoryUsedMb { get; set; }
    string Time { get; set; }
    long TotalAllocationsCreated { get; set; }
    static AnalyticsIkonTurnServerStats ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsIkonTurnServerStats ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsIkonTurnServerStats? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsLogs : IProtocolMessagePayload
    ctor()
    ctor(List<AnalyticsLogs.AnalyticsLogsItem> logs)
    List<AnalyticsLogs.AnalyticsLogsItem> Logs { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static AnalyticsLogs ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsLogs ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsLogs? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsLogs.AnalyticsLogsItem
    ctor()
    ctor(string time, int type, string message, string parameters)
    string Message { get; set; }
    string Parameters { get; set; }
    string Time { get; set; }
    int Type { get; set; }
    static AnalyticsLogs.AnalyticsLogsItem ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsLogs.AnalyticsLogsItem ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsLogs.AnalyticsLogsItem? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsProcessingUpdate : IProtocolMessagePayload
    ctor()
    ctor(string processingId, int totalRuns, int totalItems, int totalPages, int totalRetries, int totalFailures, string startedAt, float elapsedSeconds, Dictionary<string, double> usages, int runsRemaining, int itemsRemaining, float estimatedTimeLeftSecondsRuns, float estimatedTimeLeftSecondsItems)
    float ElapsedSeconds { get; set; }
    float EstimatedTimeLeftSecondsItems { get; set; }
    float EstimatedTimeLeftSecondsRuns { get; set; }
    int ItemsRemaining { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string ProcessingId { get; set; }
    int RunsRemaining { get; set; }
    string StartedAt { get; set; }
    int TotalFailures { get; set; }
    int TotalItems { get; set; }
    int TotalPages { get; set; }
    int TotalRetries { get; set; }
    int TotalRuns { get; set; }
    Dictionary<string, double> Usages { get; set; }
    static AnalyticsProcessingUpdate ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsProcessingUpdate ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsProcessingUpdate? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsReactiveProcessingUpdate : IProtocolMessagePayload
    ctor()
    ctor(string processingId, string startedAt, float elapsedSeconds, int inputItemCount, int inputItemCacheHits, int inputItemCacheMiss, int processedItemCount, int processedItemCacheHits, int processedItemCacheMiss, int outputItemCount, int outputItemCacheHits, int outputItemCacheMiss, int invalidItemCount, int duplicateItemCount, int processRetryCount, int processFailureCount, int warningLogCount, int errorLogCount, bool hasCompleted, bool hasFaulted, bool wasCancelled, Dictionary<string, float> usages)
    int DuplicateItemCount { get; set; }
    float ElapsedSeconds { get; set; }
    int ErrorLogCount { get; set; }
    bool HasCompleted { get; set; }
    bool HasFaulted { get; set; }
    int InputItemCacheHits { get; set; }
    int InputItemCacheMiss { get; set; }
    int InputItemCount { get; set; }
    int InvalidItemCount { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int OutputItemCacheHits { get; set; }
    int OutputItemCacheMiss { get; set; }
    int OutputItemCount { get; set; }
    int ProcessFailureCount { get; set; }
    int ProcessRetryCount { get; set; }
    int ProcessedItemCacheHits { get; set; }
    int ProcessedItemCacheMiss { get; set; }
    int ProcessedItemCount { get; set; }
    string ProcessingId { get; set; }
    string StartedAt { get; set; }
    Dictionary<string, float> Usages { get; set; }
    int WarningLogCount { get; set; }
    bool WasCancelled { get; set; }
    static AnalyticsReactiveProcessingUpdate ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsReactiveProcessingUpdate ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsReactiveProcessingUpdate? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsSpecialLog : IProtocolMessagePayload
    ctor()
    ctor(string title, string message)
    string Message { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Title { get; set; }
    static AnalyticsSpecialLog ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsSpecialLog ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsSpecialLog? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsUsage : IProtocolMessagePayload
    ctor()
    ctor(string usageName, float usage)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    float Usage { get; set; }
    string UsageName { get; set; }
    static AnalyticsUsage ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsUsage ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsUsage? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsUsages : IProtocolMessagePayload
    ctor()
    ctor(List<AnalyticsUsages.AnalyticsUsagesItem> usages)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    List<AnalyticsUsages.AnalyticsUsagesItem> Usages { get; set; }
    static AnalyticsUsages ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsUsages ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsUsages? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AnalyticsUsages.AnalyticsUsagesItem
    ctor()
    ctor(string time, string eventName, float usage, string parameters)
    string EventName { get; set; }
    string Parameters { get; set; }
    string Time { get; set; }
    float Usage { get; set; }
    static AnalyticsUsages.AnalyticsUsagesItem ReadFromTeleport(ReadOnlySpan<byte> data)
    static AnalyticsUsages.AnalyticsUsagesItem ReadFromTeleport(ReadOnlySpan<byte> data, AnalyticsUsages.AnalyticsUsagesItem? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AppConfig : IProtocolMessagePayload
    ctor()
    ctor(int maxClients, bool disableWebRtc, bool disableUdp)
    bool DisableUdp { get; set; }
    bool DisableWebRtc { get; set; }
    int MaxClients { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static AppConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    static AppConfig ReadFromTeleport(ReadOnlySpan<byte> data, AppConfig? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum AppSourceType
    Bundle
    GitSource
  enum AudioCodec
    Unknown
    Opus
    Mp3
    RawPcm16
  sealed class AudioFrame : IProtocolMessagePayload
    ctor()
    ctor(byte[] data, bool isKey, bool isLast, ulong timestampInUs, uint durationInUs, bool isFirst, uint totalDurationInUs, float volume, int volumeSampleCount)
    byte[] Data { get; set; }
    uint DurationInUs { get; set; }
    bool IsFirst { get; set; }
    bool IsKey { get; set; }
    bool IsLast { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    ulong TimestampInUs { get; set; }
    uint TotalDurationInUs { get; set; }
    float Volume { get; set; }
    int VolumeSampleCount { get; set; }
    static AudioFrame ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioFrame ReadFromTeleport(ReadOnlySpan<byte> data, AudioFrame? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AudioFrame2 : IProtocolMessagePayload
    ctor()
    ctor(byte[] samples, uint epoch, uint sequence, uint frameSizeInInterleavedSamples, ulong timeStampInInterleavedSamples, bool isFirst, bool isLast, float averageVolume, float audioEventEstimatedDuration)
    float AudioEventEstimatedDuration { get; set; }
    float AverageVolume { get; set; }
    uint Epoch { get; set; }
    uint FrameSizeInInterleavedSamples { get; set; }
    bool IsFirst { get; set; }
    bool IsLast { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    byte[] Samples { get; set; }
    uint Sequence { get; set; }
    ulong TimeStampInInterleavedSamples { get; set; }
    static AudioFrame2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioFrame2 ReadFromTeleport(ReadOnlySpan<byte> data, AudioFrame2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AudioFrameVolume : IProtocolMessagePayload
    ctor()
    ctor(float volume, int count)
    int Count { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    float Volume { get; set; }
    static AudioFrameVolume ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioFrameVolume ReadFromTeleport(ReadOnlySpan<byte> data, AudioFrameVolume? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AudioShapeFrame : IProtocolMessagePayload
    ctor()
    ctor(uint epoch, uint sequence, uint frameSizeInInterleavedSamples, ulong timeStampInInterleavedSamples, List<AudioShapeFrame.AudioShapeSetValues> shapeSetValues)
    uint Epoch { get; set; }
    uint FrameSizeInInterleavedSamples { get; set; }
    MessageFlag MessageDefaultFlags { get; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    uint Sequence { get; set; }
    List<AudioShapeFrame.AudioShapeSetValues> ShapeSetValues { get; set; }
    ulong TimeStampInInterleavedSamples { get; set; }
    static AudioShapeFrame ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioShapeFrame ReadFromTeleport(ReadOnlySpan<byte> data, AudioShapeFrame? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AudioStreamBegin.AudioShapeSet
    ctor()
    ctor(uint setId, string name, List<string> shapeNames)
    string Name { get; set; }
    uint SetId { get; set; }
    List<string> ShapeNames { get; set; }
    static AudioStreamBegin.AudioShapeSet ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioStreamBegin.AudioShapeSet ReadFromTeleport(ReadOnlySpan<byte> data, AudioStreamBegin.AudioShapeSet? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AudioShapeFrame.AudioShapeSetValues
    ctor()
    ctor(uint setId, List<float> values)
    uint SetId { get; set; }
    List<float> Values { get; set; }
    static AudioShapeFrame.AudioShapeSetValues ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioShapeFrame.AudioShapeSetValues ReadFromTeleport(ReadOnlySpan<byte> data, AudioShapeFrame.AudioShapeSetValues? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static AudioStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data, AudioStreamBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AudioStreamEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static AudioStreamEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static AudioStreamEnd ReadFromTeleport(ReadOnlySpan<byte> data, AudioStreamEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class GlobalState.AudioStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, AudioStreamBegin info)
    int ClientSessionId { get; set; }
    AudioStreamBegin Info { get; set; }
    string StreamId { get; set; }
    int TrackId { get; set; }
    static GlobalState.AudioStreamState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState.AudioStreamState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState.AudioStreamState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AuthResponse : IProtocolMessagePayload
    ctor()
    ctor(Context clientContext, Context serverContext, string certHash, List<Entrypoint> entrypoints, Dictionary<string, bool> featureFlags, string spaceId, string channelId, string channelInstanceId, string primaryUserId, string serverSessionId, int keepaliveTimeoutMs, int serverCapability)
    string CertHash { get; set; }
    string ChannelId { get; set; }
    string ChannelInstanceId { get; set; }
    Context ClientContext { get; set; }
    List<Entrypoint> Entrypoints { get; set; }
    Dictionary<string, bool> FeatureFlags { get; set; }
    int KeepaliveTimeoutMs { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PrimaryUserId { get; set; }
    int ServerCapability { get; set; }
    Context ServerContext { get; set; }
    string ServerSessionId { get; set; }
    string SpaceId { get; set; }
    static AuthResponse ReadFromTeleport(ReadOnlySpan<byte> data)
    static AuthResponse ReadFromTeleport(ReadOnlySpan<byte> data, AuthResponse? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class AuthTicket : IProtocolMessagePayload
    ctor()
    ctor(string host, int httpsPort, int tcpPort, string secret, Opcode opcodeGroupsFromServer, Opcode opcodeGroupsToServer, Context clientContext, int tlsPort, int udpPort, int udpDtlsPort)
    Context ClientContext { get; set; }
    string Host { get; set; }
    int HttpsPort { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Opcode OpcodeGroupsFromServer { get; set; }
    Opcode OpcodeGroupsToServer { get; set; }
    string Secret { get; set; }
    int TcpPort { get; set; }
    int TlsPort { get; set; }
    int UdpDtlsPort { get; set; }
    int UdpPort { get; set; }
    static AuthTicket ReadFromTeleport(ReadOnlySpan<byte> data)
    static AuthTicket ReadFromTeleport(ReadOnlySpan<byte> data, AuthTicket? destination)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class BackgroundWorkActive : IProtocolMessagePayload
    ctor()
    ctor(bool isActive)
    bool IsActive { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static BackgroundWorkActive ReadFromTeleport(ReadOnlySpan<byte> data)
    static BackgroundWorkActive ReadFromTeleport(ReadOnlySpan<byte> data, BackgroundWorkActive? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ClientDisconnecting : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ClientDisconnecting ReadFromTeleport(ReadOnlySpan<byte> data)
    static ClientDisconnecting ReadFromTeleport(ReadOnlySpan<byte> data, ClientDisconnecting? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ClientInitialization : IProtocolMessagePayload
    ctor()
    ctor(Dictionary<int, List<ActionFunctionRegister>> functions)
    Dictionary<int, List<ActionFunctionRegister>> Functions { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ClientInitialization ReadFromTeleport(ReadOnlySpan<byte> data)
    static ClientInitialization ReadFromTeleport(ReadOnlySpan<byte> data, ClientInitialization? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ClientLifecycleBatch : IProtocolMessagePayload
    ctor()
    ctor(List<ClientLifecycleEvent> events)
    List<ClientLifecycleEvent> Events { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ClientLifecycleBatch ReadFromTeleport(ReadOnlySpan<byte> data)
    static ClientLifecycleBatch ReadFromTeleport(ReadOnlySpan<byte> data, ClientLifecycleBatch? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ClientLifecycleEvent : IProtocolMessagePayload
    ctor()
    ctor(int eventOpcode, Context clientContext)
    Context ClientContext { get; set; }
    int EventOpcode { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ClientLifecycleEvent ReadFromTeleport(ReadOnlySpan<byte> data)
    static ClientLifecycleEvent ReadFromTeleport(ReadOnlySpan<byte> data, ClientLifecycleEvent? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ClientReady : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ClientReady ReadFromTeleport(ReadOnlySpan<byte> data)
    static ClientReady ReadFromTeleport(ReadOnlySpan<byte> data, ClientReady? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ClientType
    Unknown
    MobileWeb
    MobileApp
    DesktopWeb
    DesktopApp
  sealed class ConnectToken : IProtocolMessagePayload
    ctor()
    ctor(string serverSessionId, ContextType contextType, UserType userType, PayloadType payloadType, bool isInternal, bool isSnapshot, string description, string userId, string deviceId, string productId, string versionId, string installId, string locale, Opcode opcodeGroupsFromServer, Opcode opcodeGroupsToServer, int protocolVersion, bool hasInput, string channelLocale, string embeddedSpaceId, string authSessionId, bool receiveAllMessages, string userAgent, ClientType clientType, Dictionary<string, string> parameters, SdkType sdkType, int sdkCapability, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, StyleFormat styleFormat, bool supportsCompression)
    string AuthSessionId { get; set; }
    string ChannelLocale { get; set; }
    ClientType ClientType { get; set; }
    ContextType ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    string EmbeddedSpaceId { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    string InstallId { get; set; }
    bool IsInternal { get; set; }
    // True for the build-time snapshot-capture client; copied into Context.IsSnapshot. Identifies the client whose initial UI is baked into boot-snapshot.json. Inert beyond identification in v1.
    bool IsSnapshot { get; set; }
    bool IsTouchDevice { get; set; }
    string Locale { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Opcode OpcodeGroupsFromServer { get; set; }
    Opcode OpcodeGroupsToServer { get; set; }
    Dictionary<string, string> Parameters { get; set; }
    PayloadType PayloadType { get; set; }
    string ProductId { get; set; }
    int ProtocolVersion { get; set; }
    bool ReceiveAllMessages { get; set; }
    // Opaque, monotonically-increasing capability level advertised by the connecting SDK (companion to SdkType). 0 = legacy/unknown. Threaded SDK connect-request -> backend -> ConnectToken -> ikon server -> client Context.
    int SdkCapability { get; set; }
    SdkType SdkType { get; set; }
    string ServerSessionId { get; set; }
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
    static ConnectToken ReadFromTeleport(ReadOnlySpan<byte> data)
    static ConnectToken ReadFromTeleport(ReadOnlySpan<byte> data, ConnectToken? destination)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Context : IProtocolMessagePayload
    ctor()
    ctor(ContextType contextType, UserType userType, PayloadType payloadType, string description, string userId, string deviceId, string productId, string versionId, string installId, string locale, int sessionId, bool isInternal, bool isSnapshot, bool isReady, bool hasInput, string channelLocale, string embeddedSpaceId, string authSessionId, bool receiveAllMessages, ulong preciseJoinedAt, string userAgent, ClientType clientType, string uniqueSessionId, Dictionary<string, string> parameters, SdkType sdkType, int sdkCapability, int viewportWidth, int viewportHeight, string theme, string timezone, bool isTouchDevice, string initialPath, StyleFormat styleFormat, bool supportsCompression, bool isSoftDisconnected, ulong softDisconnectAt)
    string AuthSessionId { get; set; }
    string ChannelLocale { get; set; }
    // Alias for SessionId . The protocol surfaces this same int as ClientSessionId on event-args types like ClientJoinedEventArgs.ClientSessionId — code generated against the event-args shape naturally reaches for ctx.ClientSessionId after switching to the Context directly. Provide both names so the natural reach resolves without renaming.
    int ClientSessionId { get; }
    ClientType ClientType { get; set; }
    ContextType ContextType { get; set; }
    string Description { get; set; }
    string DeviceId { get; set; }
    string EmbeddedSpaceId { get; set; }
    bool HasInput { get; set; }
    string InitialPath { get; set; }
    string InstallId { get; set; }
    bool IsInternal { get; set; }
    bool IsReady { get; set; }
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
    static Context ReadFromTeleport(ReadOnlySpan<byte> data)
    static Context ReadFromTeleport(ReadOnlySpan<byte> data, Context? destination)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ContextType
    Unknown
    Backend
    Server
    Plugin
    Browser
    Native
  sealed class Coordinate2D : IProtocolMessagePayload
    ctor()
    ctor(float x, float y)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    float X { get; set; }
    float Y { get; set; }
    static Coordinate2D ReadFromTeleport(ReadOnlySpan<byte> data)
    static Coordinate2D ReadFromTeleport(ReadOnlySpan<byte> data, Coordinate2D? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class DynamicConfig : IProtocolMessagePayload
    ctor()
    ctor(string configJsonContent)
    string ConfigJsonContent { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static DynamicConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    static DynamicConfig ReadFromTeleport(ReadOnlySpan<byte> data, DynamicConfig? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static Entrypoint ReadFromTeleport(ReadOnlySpan<byte> data)
    static Entrypoint ReadFromTeleport(ReadOnlySpan<byte> data, Entrypoint? destination)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
  sealed class EventsOnChannelComplete : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static EventsOnChannelComplete ReadFromTeleport(ReadOnlySpan<byte> data)
    static EventsOnChannelComplete ReadFromTeleport(ReadOnlySpan<byte> data, EventsOnChannelComplete? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class EventsOnProfileUpdate : IProtocolMessagePayload
    ctor()
    ctor(string userId, string valuesAsJson)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string UserId { get; set; }
    string ValuesAsJson { get; set; }
    static EventsOnProfileUpdate ReadFromTeleport(ReadOnlySpan<byte> data)
    static EventsOnProfileUpdate ReadFromTeleport(ReadOnlySpan<byte> data, EventsOnProfileUpdate? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class EventsSpeechPlaybackCompleted : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static EventsSpeechPlaybackCompleted ReadFromTeleport(ReadOnlySpan<byte> data)
    static EventsSpeechPlaybackCompleted ReadFromTeleport(ReadOnlySpan<byte> data, EventsSpeechPlaybackCompleted? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static FunctionParameter ReadFromTeleport(ReadOnlySpan<byte> data)
    static FunctionParameter ReadFromTeleport(ReadOnlySpan<byte> data, FunctionParameter? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static ActionFunctionRegister.FunctionRegisterParameter ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionRegister.FunctionRegisterParameter ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionRegister.FunctionRegisterParameter? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  // Shared state synchronized across all clients and the server, providing access to connected clients, registered functions, active media streams, and session metadata
  sealed class GlobalState : ILogInfo, IProtocolMessagePayload
    ctor()
    ctor(Dictionary<int, Context> clients, Dictionary<int, List<ActionFunctionRegister>> functions, Dictionary<string, GlobalState.UIStreamState> uiStreams, Dictionary<string, GlobalState.AudioStreamState> audioStreams, Dictionary<string, GlobalState.VideoStreamState> videoStreams, Dictionary<string, GlobalState.TrackingStreamState> trackingStreams, string spaceId, string channelId, string serverSessionId, string sessionHash, string channelUrl, string sessionChannelUrl, string firstUserId, string primaryUserId, string organisationName, string spaceName, string channelName, ServerRunType serverRunType, AppSourceType appSourceType, bool publicAccess, bool debugMode)
    // Tells the source where the app is being run from
    AppSourceType AppSourceType { get; set; }
    // Active audio streams indexed by stream ID
    Dictionary<string, GlobalState.AudioStreamState> AudioStreams { get; set; }
    // Unique identifier for the channel within the space
    string ChannelId { get; set; }
    // Display name of the channel
    string ChannelName { get; set; }
    // URL for accessing the channel
    string ChannelUrl { get; set; }
    // All connected clients indexed by their client session ID, containing client metadata such as user ID, device info, viewport dimensions, and locale
    Dictionary<int, Context> Clients { get; set; }
    // Whether debug mode is enabled, providing additional logging and development features
    bool DebugMode { get; set; }
    // User ID of the first human user who joined this session, dynamically reassigned when that user leaves
    string FirstUserId { get; set; }
    // Registry of callable functions organized by client session ID
    Dictionary<int, List<ActionFunctionRegister>> Functions { get; set; }
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
    // Unique identifier of the specific Ikon server instance handling this session
    string ServerSessionId { get; set; }
    // Full URL with session identifier for direct access to current session
    string SessionChannelUrl { get; set; }
    // Hash derived from the session identity parameters
    string SessionHash { get; set; }
    // Unique identifier for the space where this session is running
    string SpaceId { get; set; }
    // Display name of the space
    string SpaceName { get; set; }
    // Active tracking streams indexed by stream ID
    Dictionary<string, GlobalState.TrackingStreamState> TrackingStreams { get; set; }
    // Active UI streams indexed by stream ID
    Dictionary<string, GlobalState.UIStreamState> UIStreams { get; set; }
    // Active video streams indexed by stream ID
    Dictionary<string, GlobalState.VideoStreamState> VideoStreams { get; set; }
    void AddAudioStream(GlobalState.AudioStreamState audioStreamState)
    void AddClient(Context clientContext)
    void AddFunction(int clientSessionId, ActionFunctionRegister function)
    void AddTrackingStream(GlobalState.TrackingStreamState trackingStreamState)
    void AddUIStream(GlobalState.UIStreamState uiStreamState)
    void AddVideoStream(GlobalState.VideoStreamState videoStreamState)
    Context GetClientContext(int clientSessionId)
    Context GetClientContext(string userId)
    int GetClientSessionId(string userId)
    int[] GetClientSessionIds()
    int[] GetClientSessionIdsByProductId(string productId)
    int[] GetClientSessionIdsExcept(int[] clientSessionIds)
    int[] GetHumanClientSessionIds()
    int[] GetMachineClientSessionIds()
    List<string>? GetUserIds(IEnumerable<int> targetIds)
    static GlobalState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState? destination)
    void RemoveAudioStream(string streamId)
    void RemoveClient(int clientSessionId)
    void RemoveFunction(Guid functionId)
    void RemoveTrackingStream(string streamId)
    void RemoveUIStream(string streamId)
    void RemoveVideoStream(string streamId)
    void SetReady(int clientSessionId)
    void SetReconnected(int clientSessionId)
    void SetSoftDisconnected(int clientSessionId, ulong softDisconnectAt)
    override string ToString()
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface IProtocolMessagePayload
    MessageFlag MessageDefaultFlags { get; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
  interface IUIContainerElement
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    string StyleId { get; set; }
  sealed class IkonServerEndpointHostInfo : IProtocolMessagePayload
    ctor()
    ctor(string relayEndpointPublicUrl)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string RelayEndpointPublicUrl { get; set; }
    static IkonServerEndpointHostInfo ReadFromTeleport(ReadOnlySpan<byte> data)
    static IkonServerEndpointHostInfo ReadFromTeleport(ReadOnlySpan<byte> data, IkonServerEndpointHostInfo? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class InvalidateVideoFrame : IProtocolMessagePayload
    ctor()
    ctor(ulong frameNumber, ulong timeStampInUs)
    ulong FrameNumber { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    ulong TimeStampInUs { get; set; }
    static InvalidateVideoFrame ReadFromTeleport(ReadOnlySpan<byte> data)
    static InvalidateVideoFrame ReadFromTeleport(ReadOnlySpan<byte> data, InvalidateVideoFrame? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class KeepaliveRequest : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static KeepaliveRequest ReadFromTeleport(ReadOnlySpan<byte> data)
    static KeepaliveRequest ReadFromTeleport(ReadOnlySpan<byte> data, KeepaliveRequest? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class KeepaliveResponse : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static KeepaliveResponse ReadFromTeleport(ReadOnlySpan<byte> data)
    static KeepaliveResponse ReadFromTeleport(ReadOnlySpan<byte> data, KeepaliveResponse? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
  sealed class OnAppReady : IProtocolMessagePayload
    ctor()
    ctor(bool success)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    bool Success { get; set; }
    static OnAppReady ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnAppReady ReadFromTeleport(ReadOnlySpan<byte> data, OnAppReady? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnClientJoined : IProtocolMessagePayload
    ctor()
    ctor(Context clientContext)
    Context ClientContext { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static OnClientJoined ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnClientJoined ReadFromTeleport(ReadOnlySpan<byte> data, OnClientJoined? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnClientLeft : IProtocolMessagePayload
    ctor()
    ctor(Context clientContext)
    Context ClientContext { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static OnClientLeft ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnClientLeft ReadFromTeleport(ReadOnlySpan<byte> data, OnClientLeft? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnClientReady : IProtocolMessagePayload
    ctor()
    ctor(Context clientContext)
    Context ClientContext { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static OnClientReady ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnClientReady ReadFromTeleport(ReadOnlySpan<byte> data, OnClientReady? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnFrontendReloaded : IProtocolMessagePayload
    ctor()
    ctor(Context serverContext)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Context ServerContext { get; set; }
    static OnFrontendReloaded ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnFrontendReloaded ReadFromTeleport(ReadOnlySpan<byte> data, OnFrontendReloaded? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnHostedServerExit : IProtocolMessagePayload
    ctor()
    ctor(string serverSessionId, bool wasSuccessful)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string ServerSessionId { get; set; }
    bool WasSuccessful { get; set; }
    static OnHostedServerExit ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnHostedServerExit ReadFromTeleport(ReadOnlySpan<byte> data, OnHostedServerExit? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnPluginReloaded : IProtocolMessagePayload
    ctor()
    ctor(Context serverContext, string pluginName)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PluginName { get; set; }
    Context ServerContext { get; set; }
    static OnPluginReloaded ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnPluginReloaded ReadFromTeleport(ReadOnlySpan<byte> data, OnPluginReloaded? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnServerStarted : IProtocolMessagePayload
    ctor()
    ctor(Context serverContext)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Context ServerContext { get; set; }
    static OnServerStarted ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnServerStarted ReadFromTeleport(ReadOnlySpan<byte> data, OnServerStarted? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnServerStatusPing : IProtocolMessagePayload
    ctor()
    ctor(Context serverContext, ServerStatus status, int userCount, int clientCount, int humanClientCount, int idleTimeInSeconds, float sentMessagesPerSecond, float sentMessagesBandwidth, int sentMessagesCount, float receivedMessagesPerSecond, float receivedMessagesBandwidth, int receivedMessagesCount, float processCpuUsage, float processMemoryUsedMb, float managedMemoryUsedMb, string memoryInfo, bool isDoingBackgroundWork)
    int ClientCount { get; set; }
    int HumanClientCount { get; set; }
    int IdleTimeInSeconds { get; set; }
    bool IsDoingBackgroundWork { get; set; }
    float ManagedMemoryUsedMb { get; set; }
    string MemoryInfo { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    float ProcessCpuUsage { get; set; }
    float ProcessMemoryUsedMb { get; set; }
    float ReceivedMessagesBandwidth { get; set; }
    int ReceivedMessagesCount { get; set; }
    float ReceivedMessagesPerSecond { get; set; }
    float SentMessagesBandwidth { get; set; }
    int SentMessagesCount { get; set; }
    float SentMessagesPerSecond { get; set; }
    Context ServerContext { get; set; }
    ServerStatus Status { get; set; }
    int UserCount { get; set; }
    static OnServerStatusPing ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnServerStatusPing ReadFromTeleport(ReadOnlySpan<byte> data, OnServerStatusPing? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnServerStopped : IProtocolMessagePayload
    ctor()
    ctor(Context serverContext)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Context ServerContext { get; set; }
    static OnServerStopped ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnServerStopped ReadFromTeleport(ReadOnlySpan<byte> data, OnServerStopped? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnServerStopping : IProtocolMessagePayload
    ctor()
    ctor(Context serverContext)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Context ServerContext { get; set; }
    static OnServerStopping ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnServerStopping ReadFromTeleport(ReadOnlySpan<byte> data, OnServerStopping? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnUserJoined : IProtocolMessagePayload
    ctor()
    ctor(Context clientContext)
    Context ClientContext { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static OnUserJoined ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnUserJoined ReadFromTeleport(ReadOnlySpan<byte> data, OnUserJoined? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class OnUserLeft : IProtocolMessagePayload
    ctor()
    ctor(Context clientContext)
    Context ClientContext { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static OnUserLeft ReadFromTeleport(ReadOnlySpan<byte> data)
    static OnUserLeft ReadFromTeleport(ReadOnlySpan<byte> data, OnUserLeft? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    CORE_SERVER_START
    CORE_SERVER_STOP
    CORE_ON_HOSTED_SERVER_EXIT
    CORE_DYNAMIC_CONFIG
    CORE_PROXY_RPC_AUTH_TICKET
    CORE_SERVER_INIT2
    CORE_UPDATE_CLIENT_CONTEXT
    CORE_BACKGROUND_WORK_ACTIVE
    CORE_RESET_IDLE
    CORE_CLIENT_DISCONNECTING
    CORE_ON_APP_READY
    CORE_ON_FRONTEND_RELOADED
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
    EVENTS_CHANNEL_COMPLETE
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
    ACTION_FILE_UPLOAD_BEGIN
    ACTION_FILE_UPLOAD_DATA
    ACTION_FILE_UPLOAD_ACK
    ACTION_FILE_UPLOAD_END
    ACTION_FILE_UPLOAD_RESULT
    ACTION_OPEN_CHANNEL
    ACTION_OPEN_EXTERNAL_URL
    ACTION_UI_OPEN_VIEW
    ACTION_UI_CLOSE_VIEW
    ACTION_UI_BLOCKING_BEGIN
    ACTION_UI_BLOCKING_END
    ACTION_UI_UPDATE_TEXT_DELTA
    ACTION_UI_DELETE_CONTAINER
    ACTION_UPDATE_GFX_SHADER
    ACTION_FUNCTION_REGISTER
    ACTION_FUNCTION_CALL
    ACTION_FUNCTION_RESULT
    ACTION_GENERATE_ANSWER
    ACTION_REGENERATE_ANSWER
    ACTION_CLEAR_CHAT_MESSAGE_HISTORY
    ACTION_CLEAR_STATE
    ACTION_RELOAD_CHANNELS
    ACTION_RELOAD_PROFILE
    ACTION_CLASSIFICATION_RESULT
    ACTION_AUDIO_STOP
    ACTION_CALL_TEXT
    ACTION_RELOAD_APPLICATION
    ACTION_CANCEL_GENERATION
    ACTION_UI_SET_CONTAINER_STABLE
    ACTION_SPEECH_RECOGNIZED
    ACTION_CALL_RESULT
    ACTION_RELOAD_PROVIDER
    ACTION_DOWNLOAD
    ACTION_SCROLL_TO_CONTAINER
    ACTION_UI_CLEAR_STREAM
    ACTION_PLAY_SOUND
    ACTION_ENTER_FULLSCREEN
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
    ACTION_TRIGGER_GIT_PULL
    ACTION_FILE_UPLOAD_CALLBACK
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
    GROUP_UI
    UI_STREAM_BEGIN
    UI_STREAM_END
    UI_CONTAINER_BEGIN
    UI_CONTAINER_END
    UI_SECTION_BEGIN
    UI_SECTION_END
    UI_LIST_BEGIN
    UI_LIST_ITEM
    UI_LIST_END
    UI_TEXT
    UI_HEADER
    UI_SEPARATOR
    UI_BUTTON
    UI_ICON_BUTTON
    UI_IMAGE
    UI_FILE
    UI_BADGE
    UI_CONTENT_LINK
    UI_MAP
    UI_VEGA_CHART
    UI_ICON
    UI_FILE_UPLOAD_SECTION_BEGIN
    UI_FILE_UPLOAD_SECTION_END
    UI_MATERIAL_SYMBOL
    UI_BUTTON_BEGIN
    UI_BUTTON_END
    UI_CONTAINER_DELETE
    UI_INPUT_TEXT
    UI_PROGRESS_BAR
    UI_UPDATE_BEGIN
    UI_UPDATE_END
    UI_AUTOCOMPLETE
    UI_CHECKBOX
    UI_QS
    UI_ELEMENT
    UI_STYLES
    UI_SVG
    UI_UPDATE
    UI_INIT
    UI_STYLES_BATCH
    UI_STYLES_DELETE
    GROUP_COMMON
    GROUP_AUDIO
    AUDIO_STREAM_BEGIN
    AUDIO_STREAM_END
    AUDIO_FRAME
    AUDIO_FRAME_VOLUME
    AUDIO_FRAME2
    AUDIO_SHAPE_FRAME
    GROUP_VIDEO
    VIDEO_STREAM_BEGIN
    VIDEO_STREAM_END
    VIDEO_FRAME
    VIDEO_REQUEST_IDR_FRAME
    VIDEO_INVALIDATE_FRAME
    GROUP_TRACKING
    TRACKING_STREAM_BEGIN
    TRACKING_STREAM_END
    TRACKING_FRAME
    GROUP_SCENE
    SCENE_MESH
    SCENE_ARRAY
    GROUP_ALL
    GROUP_APP_LOCAL
    CONSTANT_GROUP_MASK
  static class Opcodes
    static bool IsOpcodeInAnyGroup(Opcode opcode, Opcode groups)
  static class PayloadCompression
    static ValueTuple<byte[]?, int> Compress(ReadOnlySpan<byte> data)
    static ValueTuple<byte[], int> Decompress(ReadOnlySpan<byte> compressedData, int estimatedSize = 0)
    static void ReturnBuffer(byte[]? buffer)
    static bool ShouldCompress(int payloadSize)
    static int CompressionThreshold
  enum PayloadType
    Unknown
    MessagePack
    MemoryPack
    Json
    Teleport
    All
  class ProtocolMessage : AsyncLocalInstance<ProtocolMessage>
    ctor()
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
    static ProtocolMessage Create(int senderId, IProtocolMessagePayload payload, PayloadType payloadType = Unknown, int trackId = 0, int sequenceId = 0, MessageFlag flags = None, IReadOnlyList<int>? targetIds = null, bool compress = false)
    T GetPayload<T>() where T : IProtocolMessagePayload
    IProtocolMessagePayload GetPayload()
    static ProtocolMessage ModifyMessage(ProtocolMessage message, int? senderId = null, int? trackId = null, int? sequenceId = null, MessageFlag? flags = null, IReadOnlyList<int>? targetIds = null)
    static ProtocolMessage ModifyPayload(IProtocolMessagePayload payload, ProtocolMessage message, PayloadType payloadType = Unknown)
    // Register an app-local message type (an app's own schema/*.tp type, opcode in GROUP_APP_LOCAL ) at runtime. Called from the generated type's static constructor — app-local types are compiled into the app assembly and are not visible to the platform's compile-time ProtocolMessage source generator.
    static void RegisterAppLocalMessageType(Type type, Opcode opcode, int version)
    override string ToString()
    static ProtocolMessage WithFlags(ProtocolMessage message, MessageFlag additionalFlags)
    PayloadType DefaultPayloadType
    static int MaxMessageSize
    static int MinimumHeaderLength
    static Dictionary<Opcode, Type> OpcodeToType
    static Dictionary<Type, Opcode> TypeToOpcode
    static Dictionary<Type, int> TypeToVersion
  class ProtocolMessageAttribute : Attribute
    ctor(int version = 0, Opcode opcode = NONE, bool unreliable = false)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    bool Unreliable { get; }
  static class ProtocolVersion
    static int Version { get; }
  sealed class ProxyRpcAuthTicket : IProtocolMessagePayload
    ctor()
    ctor(string proxyServerToken, string clientIkonBackendToken)
    string ClientIkonBackendToken { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string ProxyServerToken { get; set; }
    static ProxyRpcAuthTicket ReadFromTeleport(ReadOnlySpan<byte> data)
    static ProxyRpcAuthTicket ReadFromTeleport(ReadOnlySpan<byte> data, ProxyRpcAuthTicket? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayAddTunnel : IProtocolMessagePayload
    ctor()
    ctor(uint requestId, string protocol, int localPort, bool terminateTls, string stablePortName)
    int LocalPort { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Protocol { get; set; }
    uint RequestId { get; set; }
    string StablePortName { get; set; }
    bool TerminateTls { get; set; }
    static RelayAddTunnel ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayAddTunnel ReadFromTeleport(ReadOnlySpan<byte> data, RelayAddTunnel? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayAgentAuth : IProtocolMessagePayload
    ctor()
    ctor(string authToken, string stableId, string agentInstanceId)
    string AgentInstanceId { get; set; }
    string AuthToken { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StableId { get; set; }
    static RelayAgentAuth ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayAgentAuth ReadFromTeleport(ReadOnlySpan<byte> data, RelayAgentAuth? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayAgentAuthResult : IProtocolMessagePayload
    ctor()
    ctor(bool success, string publicHost, double heartbeatIntervalSeconds)
    double HeartbeatIntervalSeconds { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PublicHost { get; set; }
    bool Success { get; set; }
    static RelayAgentAuthResult ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayAgentAuthResult ReadFromTeleport(ReadOnlySpan<byte> data, RelayAgentAuthResult? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayHeartbeat : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static RelayHeartbeat ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayHeartbeat ReadFromTeleport(ReadOnlySpan<byte> data, RelayHeartbeat? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayRemoveTunnel : IProtocolMessagePayload
    ctor()
    ctor(uint tunnelId)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    uint TunnelId { get; set; }
    static RelayRemoveTunnel ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayRemoveTunnel ReadFromTeleport(ReadOnlySpan<byte> data, RelayRemoveTunnel? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayTcpConnectionClosed : IProtocolMessagePayload
    ctor()
    ctor(uint tunnelId, uint connectionId)
    uint ConnectionId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    uint TunnelId { get; set; }
    static RelayTcpConnectionClosed ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayTcpConnectionClosed ReadFromTeleport(ReadOnlySpan<byte> data, RelayTcpConnectionClosed? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayTcpConnectionOpened : IProtocolMessagePayload
    ctor()
    ctor(uint tunnelId, uint connectionId)
    uint ConnectionId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    uint TunnelId { get; set; }
    static RelayTcpConnectionOpened ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayTcpConnectionOpened ReadFromTeleport(ReadOnlySpan<byte> data, RelayTcpConnectionOpened? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayTcpData : IProtocolMessagePayload
    ctor()
    ctor(uint tunnelId, uint connectionId, byte[] data)
    uint ConnectionId { get; set; }
    byte[] Data { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    uint TunnelId { get; set; }
    static RelayTcpData ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayTcpData ReadFromTeleport(ReadOnlySpan<byte> data, RelayTcpData? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayTunnelAdded : IProtocolMessagePayload
    ctor()
    ctor(uint requestId, bool success, string errorMessage, uint tunnelId, int publicPort)
    string ErrorMessage { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int PublicPort { get; set; }
    uint RequestId { get; set; }
    bool Success { get; set; }
    uint TunnelId { get; set; }
    static RelayTunnelAdded ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayTunnelAdded ReadFromTeleport(ReadOnlySpan<byte> data, RelayTunnelAdded? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RelayUdpData : IProtocolMessagePayload
    ctor()
    ctor(uint tunnelId, string sourceAddress, int sourcePort, byte[] data)
    byte[] Data { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string SourceAddress { get; set; }
    int SourcePort { get; set; }
    uint TunnelId { get; set; }
    static RelayUdpData ReadFromTeleport(ReadOnlySpan<byte> data)
    static RelayUdpData ReadFromTeleport(ReadOnlySpan<byte> data, RelayUdpData? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RequestIdrVideoFrame : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static RequestIdrVideoFrame ReadFromTeleport(ReadOnlySpan<byte> data)
    static RequestIdrVideoFrame ReadFromTeleport(ReadOnlySpan<byte> data, RequestIdrVideoFrame? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ResetIdle : IProtocolMessagePayload
    ctor()
    ctor(string? reason)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string? Reason { get; set; }
    static ResetIdle ReadFromTeleport(ReadOnlySpan<byte> data)
    static ResetIdle ReadFromTeleport(ReadOnlySpan<byte> data, ResetIdle? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SceneArray : IProtocolMessagePayload
    ctor()
    ctor(int serializerType, string type, string subId, int elementOffset, int elementCount, int byteOffset, int typeSize, int strideSize, byte[] byteArray)
    byte[] ByteArray { get; set; }
    int ByteOffset { get; set; }
    int ElementCount { get; set; }
    int ElementOffset { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SerializerType { get; set; }
    int StrideSize { get; set; }
    string SubId { get; set; }
    string Type { get; set; }
    int TypeSize { get; set; }
    static SceneArray ReadFromTeleport(ReadOnlySpan<byte> data)
    static SceneArray ReadFromTeleport(ReadOnlySpan<byte> data, SceneArray? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SceneMesh : IProtocolMessagePayload
    ctor()
    ctor(List<float> vertices)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    List<float> Vertices { get; set; }
    static SceneMesh ReadFromTeleport(ReadOnlySpan<byte> data)
    static SceneMesh ReadFromTeleport(ReadOnlySpan<byte> data, SceneMesh? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionCall.ScopeEntry
    ctor()
    ctor(string type, string id)
    string Id { get; set; }
    string Type { get; set; }
    static ActionFunctionCall.ScopeEntry ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionCall.ScopeEntry ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionCall.ScopeEntry? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  // Capability levels advertised by a connecting SDK via SdkCapability (companion to SdkType ). Opaque and monotonically increasing — bump when adding a capability the ikon server must detect per connected client. 0 means a legacy client that predates capability negotiation.
  static class SdkCapabilities
    // Client handles the CORE_CLIENT_INITIALIZATION message — the server/app function registry the server sends out-of-band right after the joining client's GlobalState — and registers those functions during connect. When any connected client advertises less than this, the server keeps the function registry embedded in GlobalState.Functions for the whole session so the older client can still learn server functions. This is a distinct level from FunctionRegistryOutsideGlobalState because the ClientInitialization message was introduced after it: clients advertising only levels 1-3 cannot parse it and would silently receive no functions if the server stripped them from GlobalState.
    static int ClientInitializationMessage
    // Client understands the batched CORE_CLIENT_LIFECYCLE_BATCH message (client joined/ready/left and user joined/left events coalesced into one payload) and unpacks it into the individual events. When all connected external clients advertise at least this, the server coalesces and debounces those broadcasts to external clients instead of one fan-out message per event; otherwise it falls back to per-event broadcasts. Internal (localhost) clients always receive the events immediately, unbatched.
    static int ClientLifecycleBatching
    // The highest capability level this build supports; advertised by first-party SDKs and the server itself.
    static int Current
    // Client understands server functions delivered out-of-band (the original targeted ACTION_FUNCTION_REGISTER_BATCH on join) rather than embedded in GlobalState.Functions. Superseded by ClientInitializationMessage : the out-of-band delivery is now the CORE_CLIENT_INITIALIZATION message, which a level-1 client does NOT understand. Do not gate the functions-out-of-GlobalState decision on this level — it is too low and matches clients that predate the ClientInitialization message.
    static int FunctionRegistryOutsideGlobalState
    // Client honors the keepalive watchdog timeout communicated by the server in AuthResponse.KeepaliveTimeoutMs instead of hard-coding it. When all connected clients advertise at least this, the server may stretch its keepalive send interval well beyond the legacy client's fixed watchdog; otherwise it stays within the legacy-safe cap.
    static int KeepaliveTimeoutNegotiation
  enum SdkType
    Unknown
    DotNet
    TypeScript
    Cpp
    Dart
    Rust
  // Capability levels advertised by the ikon server to a connecting client via AuthResponse.ServerCapability (companion to the client's Context.SdkCapability). Opaque and monotonically increasing — bump when adding a server behavior a client must detect to alter its connect handling. 0 means a legacy server that predates capability negotiation.
  static class ServerCapabilities
    // Server sends a ClientInitialization message immediately after the joining client's GlobalState, carrying the server/app function registry out-of-band. A client that sees at least this waits for that message during connect (so server functions are registered before the connect call returns) instead of expecting functions embedded in GlobalState.
    static int ClientInitializationMessage
    // The highest capability level this server build supports; advertised in AuthResponse.
    static int Current
  sealed class ServerInit.ServerExtensionInit
    ctor()
    ctor(bool enabled, string typeName, string configJsonContent)
    string ConfigJsonContent { get; set; }
    bool Enabled { get; set; }
    string TypeName { get; set; }
    static ServerInit.ServerExtensionInit ReadFromTeleport(ReadOnlySpan<byte> data)
    static ServerInit.ServerExtensionInit ReadFromTeleport(ReadOnlySpan<byte> data, ServerInit.ServerExtensionInit? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ServerInit : IProtocolMessagePayload
    ctor()
    ctor(string ikonBackendUrl, string ikonBackendToken, string spaceId, string channelId, List<ServerInit.ServerPluginInit> plugins, string primaryUserId, string channelInstanceId, string channelUrl, List<ServerInit.ServerExtensionInit> extensions, Dictionary<string, string> dynamicConfigObsolete, string organisationName, string spaceName, string channelName, string dynamicConfigJsonContent, string spaceGitRepositoryUrl, string sessionId, string legacyChannelCode, bool disableLegacyDefaultExtensions, Dictionary<string, string> sessionIdentity, int frontendPort, AppSourceType appSourceType, bool debugMode, List<ServerInit.ServerInitDatabaseConnectionInfo> databaseConnectionInfos, string runTarget, string connectTraceId)
    AppSourceType AppSourceType { get; set; }
    string ChannelId { get; set; }
    string ChannelInstanceId { get; set; }
    string ChannelName { get; set; }
    string ChannelUrl { get; set; }
    // Correlation id of the client connect that triggered this warm prestart-swap (from the backend's /init handling). The server folds its CORE_SERVER_INIT boot timing into a single connect-latency event keyed by this id, so the warm-boot cost stitches with the other per-connect tiers.
    string ConnectTraceId { get; set; }
    List<ServerInit.ServerInitDatabaseConnectionInfo> DatabaseConnectionInfos { get; set; }
    bool DebugMode { get; set; }
    bool DisableLegacyDefaultExtensions { get; set; }
    string DynamicConfigJsonContent { get; set; }
    Dictionary<string, string> DynamicConfigObsolete { get; set; }
    List<ServerInit.ServerExtensionInit> Extensions { get; set; }
    int FrontendPort { get; set; }
    string IkonBackendToken { get; set; }
    string IkonBackendUrl { get; set; }
    string LegacyChannelCode { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string OrganisationName { get; set; }
    List<ServerInit.ServerPluginInit> Plugins { get; set; }
    string PrimaryUserId { get; set; }
    string RunTarget { get; set; }
    string SessionId { get; set; }
    Dictionary<string, string> SessionIdentity { get; set; }
    string SpaceGitRepositoryUrl { get; set; }
    string SpaceId { get; set; }
    string SpaceName { get; set; }
    static ServerInit ReadFromTeleport(ReadOnlySpan<byte> data)
    static ServerInit ReadFromTeleport(ReadOnlySpan<byte> data, ServerInit? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ServerInit2 : IProtocolMessagePayload
    ctor()
    ctor(string sessionId, Dictionary<string, string> sessionIdentity)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string SessionId { get; set; }
    Dictionary<string, string> SessionIdentity { get; set; }
    static ServerInit2 ReadFromTeleport(ReadOnlySpan<byte> data)
    static ServerInit2 ReadFromTeleport(ReadOnlySpan<byte> data, ServerInit2? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ServerInit.ServerInitDatabaseConnectionInfo
    ctor()
    ctor(string name, string type, string connectionString)
    string ConnectionString { get; set; }
    string Name { get; set; }
    string Type { get; set; }
    static ServerInit.ServerInitDatabaseConnectionInfo ReadFromTeleport(ReadOnlySpan<byte> data)
    static ServerInit.ServerInitDatabaseConnectionInfo ReadFromTeleport(ReadOnlySpan<byte> data, ServerInit.ServerInitDatabaseConnectionInfo? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ServerInit.ServerPluginInit
    ctor()
    ctor(bool enabled, string bundleDirectoryPath, byte[] bundleDirectoryZipContent, string dllName, string typeName, string configFilePath, string configJsonContent, List<ServerInit.ServerPluginInitExtraConfig> extraConfigs)
    string BundleDirectoryPath { get; set; }
    byte[] BundleDirectoryZipContent { get; set; }
    string ConfigFilePath { get; set; }
    string ConfigJsonContent { get; set; }
    string DllName { get; set; }
    bool Enabled { get; set; }
    List<ServerInit.ServerPluginInitExtraConfig> ExtraConfigs { get; set; }
    string TypeName { get; set; }
    static ServerInit.ServerPluginInit ReadFromTeleport(ReadOnlySpan<byte> data)
    static ServerInit.ServerPluginInit ReadFromTeleport(ReadOnlySpan<byte> data, ServerInit.ServerPluginInit? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ServerInit.ServerPluginInitExtraConfig
    ctor()
    ctor(string filePath, string content)
    string Content { get; set; }
    string FilePath { get; set; }
    static ServerInit.ServerPluginInitExtraConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    static ServerInit.ServerPluginInitExtraConfig ReadFromTeleport(ReadOnlySpan<byte> data, ServerInit.ServerPluginInitExtraConfig? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ServerRunType
    Local
    Cloud
  sealed class ServerStart : IProtocolMessagePayload
    ctor()
    ctor(string hostServerSessionId, string configJsonContent)
    string ConfigJsonContent { get; set; }
    string HostServerSessionId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static ServerStart ReadFromTeleport(ReadOnlySpan<byte> data)
    static ServerStart ReadFromTeleport(ReadOnlySpan<byte> data, ServerStart? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum ServerStatus
    Unknown
    Starting
    Running
    Stopping
    Stopped
  sealed class ServerStop : IProtocolMessagePayload
    ctor()
    ctor(string hostServerSessionId, string targetServerSessionId)
    string HostServerSessionId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string TargetServerSessionId { get; set; }
    static ServerStop ReadFromTeleport(ReadOnlySpan<byte> data)
    static ServerStop ReadFromTeleport(ReadOnlySpan<byte> data, ServerStop? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum StyleFormat
    Css
    Flutter
  sealed class TrackingFrame : IProtocolMessagePayload
    ctor()
    ctor(ulong timestampInUs, uint durationInUs, List<float> faceBlendshapes, List<float> faceTransformationMatrix)
    uint DurationInUs { get; set; }
    List<float> FaceBlendshapes { get; set; }
    List<float> FaceTransformationMatrix { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    ulong TimestampInUs { get; set; }
    static TrackingFrame ReadFromTeleport(ReadOnlySpan<byte> data)
    static TrackingFrame ReadFromTeleport(ReadOnlySpan<byte> data, TrackingFrame? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class TrackingStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string category, TrackingType type, List<string> faceBlendshapes)
    string Category { get; set; }
    List<string> FaceBlendshapes { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    TrackingType Type { get; set; }
    static TrackingStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static TrackingStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data, TrackingStreamBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class TrackingStreamEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static TrackingStreamEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static TrackingStreamEnd ReadFromTeleport(ReadOnlySpan<byte> data, TrackingStreamEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class GlobalState.TrackingStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, TrackingStreamBegin info)
    int ClientSessionId { get; set; }
    TrackingStreamBegin Info { get; set; }
    string StreamId { get; set; }
    int TrackId { get; set; }
    static GlobalState.TrackingStreamState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState.TrackingStreamState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState.TrackingStreamState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum TrackingType
    Face
    Hands
    Pose
    All
  sealed class UIAutocomplete : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, List<UIAutocomplete.UIAutocompleteOption> options, string updateActionId, int minCount, int maxCount, UIColor color, UIInputVariant variant, List<string> initialValue, string placeholder)
    UIColor Color { get; set; }
    int ElementId { get; set; }
    List<string> InitialValue { get; set; }
    List<string> Labels { get; set; }
    int MaxCount { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int MinCount { get; set; }
    string Name { get; set; }
    List<UIAutocomplete.UIAutocompleteOption> Options { get; set; }
    string Placeholder { get; set; }
    string StyleId { get; set; }
    string UpdateActionId { get; set; }
    UIInputVariant Variant { get; set; }
    static UIAutocomplete ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIAutocomplete ReadFromTeleport(ReadOnlySpan<byte> data, UIAutocomplete? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIAutocomplete.UIAutocompleteOption
    ctor()
    ctor(string name, string value, string group)
    string Group { get; set; }
    string Name { get; set; }
    string Value { get; set; }
    static UIAutocomplete.UIAutocompleteOption ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIAutocomplete.UIAutocompleteOption ReadFromTeleport(ReadOnlySpan<byte> data, UIAutocomplete.UIAutocompleteOption? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIBadge : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string text, UIColor color, string clickActionId, UIBadgeVariant variant, string pressStartActionId, string pressEndActionId, string pressChangeActionId, string pressUpActionId, string dragStartActionId, string dragEnterActionId, string dragLeaveActionId, string dragOverActionId, string dropActionId, string dragEndActionId)
    string ClickActionId { get; set; }
    UIColor Color { get; set; }
    string DragEndActionId { get; set; }
    string DragEnterActionId { get; set; }
    string DragLeaveActionId { get; set; }
    string DragOverActionId { get; set; }
    string DragStartActionId { get; set; }
    string DropActionId { get; set; }
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PressChangeActionId { get; set; }
    string PressEndActionId { get; set; }
    string PressStartActionId { get; set; }
    string PressUpActionId { get; set; }
    string StyleId { get; set; }
    string Text { get; set; }
    UIBadgeVariant Variant { get; set; }
    static UIBadge ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIBadge ReadFromTeleport(ReadOnlySpan<byte> data, UIBadge? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIBadgeVariant
    Filled
    Outlined
  sealed class UIButton : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string text, UIIconType icon, UIColor color, string clickActionId, UIButtonVariant variant, string pressStartActionId, string pressEndActionId, string pressChangeActionId, string pressUpActionId, string dragStartActionId, string dragEnterActionId, string dragLeaveActionId, string dragOverActionId, string dropActionId, string dragEndActionId)
    string ClickActionId { get; set; }
    UIColor Color { get; set; }
    string DragEndActionId { get; set; }
    string DragEnterActionId { get; set; }
    string DragLeaveActionId { get; set; }
    string DragOverActionId { get; set; }
    string DragStartActionId { get; set; }
    string DropActionId { get; set; }
    int ElementId { get; set; }
    UIIconType Icon { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PressChangeActionId { get; set; }
    string PressEndActionId { get; set; }
    string PressStartActionId { get; set; }
    string PressUpActionId { get; set; }
    string StyleId { get; set; }
    string Text { get; set; }
    UIButtonVariant Variant { get; set; }
    static UIButton ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIButton ReadFromTeleport(ReadOnlySpan<byte> data, UIButton? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIButtonBegin : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, UIColor color, string clickActionId, UIButtonVariant variant, string pressStartActionId, string pressEndActionId, string pressChangeActionId, string pressUpActionId, string dragStartActionId, string dragEnterActionId, string dragLeaveActionId, string dragOverActionId, string dropActionId, string dragEndActionId)
    string ClickActionId { get; set; }
    UIColor Color { get; set; }
    string DragEndActionId { get; set; }
    string DragEnterActionId { get; set; }
    string DragLeaveActionId { get; set; }
    string DragOverActionId { get; set; }
    string DragStartActionId { get; set; }
    string DropActionId { get; set; }
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PressChangeActionId { get; set; }
    string PressEndActionId { get; set; }
    string PressStartActionId { get; set; }
    string PressUpActionId { get; set; }
    string StyleId { get; set; }
    UIButtonVariant Variant { get; set; }
    static UIButtonBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIButtonBegin ReadFromTeleport(ReadOnlySpan<byte> data, UIButtonBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIButtonEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIButtonEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIButtonEnd ReadFromTeleport(ReadOnlySpan<byte> data, UIButtonEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIButtonVariant
    Outlined
    Contained
    Text
  sealed class UICheckbox : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, string updateActionId, bool selected)
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Name { get; set; }
    bool Selected { get; set; }
    string StyleId { get; set; }
    string UpdateActionId { get; set; }
    static UICheckbox ReadFromTeleport(ReadOnlySpan<byte> data)
    static UICheckbox ReadFromTeleport(ReadOnlySpan<byte> data, UICheckbox? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIColor
    Default
    Primary
    Secondary
    Error
    Warning
    Info
    Success
  sealed class UIContainerBegin : IProtocolMessagePayload
    ctor()
    ctor(string containerId, string userId, string createdAt, string updatedAt, string alternativeText, bool isTransient, bool isHistory, bool isUpdate, int groupId, int sortingId, bool isStable, UIVisibilityType visibility, ulong preciseCreatedAt, string optimisticActionId)
    string AlternativeText { get; set; }
    string ContainerId { get; set; }
    string CreatedAt { get; set; }
    int GroupId { get; set; }
    bool IsHistory { get; set; }
    bool IsStable { get; set; }
    bool IsTransient { get; set; }
    bool IsUpdate { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string OptimisticActionId { get; set; }
    ulong PreciseCreatedAt { get; set; }
    int SortingId { get; set; }
    string UpdatedAt { get; set; }
    string UserId { get; set; }
    UIVisibilityType Visibility { get; set; }
    static UIContainerBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIContainerBegin ReadFromTeleport(ReadOnlySpan<byte> data, UIContainerBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIContainerDelete : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIContainerDelete ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIContainerDelete ReadFromTeleport(ReadOnlySpan<byte> data, UIContainerDelete? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIContainerEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIContainerEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIContainerEnd ReadFromTeleport(ReadOnlySpan<byte> data, UIContainerEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIContentLink : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, UIContentLinkType type, string code)
    string Code { get; set; }
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StyleId { get; set; }
    UIContentLinkType Type { get; set; }
    static UIContentLink ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIContentLink ReadFromTeleport(ReadOnlySpan<byte> data, UIContentLink? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIContentLinkType
    Unknown
    Youtube
  sealed class UIElement : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, string description, string argumentsJson, Dictionary<string, Action> actionIds, Dictionary<string, UIPayload> payloads)
    Dictionary<string, Action> ActionIds { get; set; }
    string ArgumentsJson { get; set; }
    string Description { get; set; }
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Name { get; set; }
    Dictionary<string, UIPayload> Payloads { get; set; }
    string StyleId { get; set; }
    static UIElement ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIElement ReadFromTeleport(ReadOnlySpan<byte> data, UIElement? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class UIElementLabels
    static string Blur
    static string ChatMessage
    static string Disabled
    static string ImageAvatar
    static string Markdown
    static string SizeExtraSmall
    static string SizeFitContent
    static string SizeFullWidth
    static string SizeLarge
    static string SizeMedium
    static string SizeSmall
    static string Wrap
  sealed class UIFile : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, string secondaryText, string type, string mime, List<string> allowedMimeTypes, int maxSize, UIFile.UIFileInfo fileInfo, string openActionId, string uploadActionId, string removeActionId)
    List<string> AllowedMimeTypes { get; set; }
    int ElementId { get; set; }
    UIFile.UIFileInfo FileInfo { get; set; }
    List<string> Labels { get; set; }
    int MaxSize { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Mime { get; set; }
    string Name { get; set; }
    string OpenActionId { get; set; }
    string RemoveActionId { get; set; }
    string SecondaryText { get; set; }
    string StyleId { get; set; }
    string Type { get; set; }
    string UploadActionId { get; set; }
    static UIFile ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIFile ReadFromTeleport(ReadOnlySpan<byte> data, UIFile? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIFile.UIFileInfo
    ctor()
    ctor(string id, string name, string fileName, string createdAt, int size, string url)
    string CreatedAt { get; set; }
    string FileName { get; set; }
    string Id { get; set; }
    string Name { get; set; }
    int Size { get; set; }
    string Url { get; set; }
    static UIFile.UIFileInfo ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIFile.UIFileInfo ReadFromTeleport(ReadOnlySpan<byte> data, UIFile.UIFileInfo? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIFileUploadSectionBegin : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, int gap, string uploadActionId, List<string> allowedMimeTypes, int maxSize)
    List<string> AllowedMimeTypes { get; set; }
    int ElementId { get; set; }
    int Gap { get; set; }
    List<string> Labels { get; set; }
    int MaxSize { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StyleId { get; set; }
    string UploadActionId { get; set; }
    static UIFileUploadSectionBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIFileUploadSectionBegin ReadFromTeleport(ReadOnlySpan<byte> data, UIFileUploadSectionBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIFileUploadSectionEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIFileUploadSectionEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIFileUploadSectionEnd ReadFromTeleport(ReadOnlySpan<byte> data, UIFileUploadSectionEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIHeader : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string text, string subText, UIHeaderLevel level)
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    UIHeaderLevel Level { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StyleId { get; set; }
    string SubText { get; set; }
    string Text { get; set; }
    static UIHeader ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIHeader ReadFromTeleport(ReadOnlySpan<byte> data, UIHeader? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIHeaderLevel
    Default
    Large
    Medium
    Normal
    Small
  sealed class UIIcon : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, UIIconType icon, UIColor color)
    UIColor Color { get; set; }
    int ElementId { get; set; }
    UIIconType Icon { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StyleId { get; set; }
    static UIIcon ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIIcon ReadFromTeleport(ReadOnlySpan<byte> data, UIIcon? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIIconButton : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, UIIconType icon, UIColor color, string clickActionId, string pressStartActionId, string pressEndActionId, string pressChangeActionId, string pressUpActionId, string dragStartActionId, string dragEnterActionId, string dragLeaveActionId, string dragOverActionId, string dropActionId, string dragEndActionId)
    string ClickActionId { get; set; }
    UIColor Color { get; set; }
    string DragEndActionId { get; set; }
    string DragEnterActionId { get; set; }
    string DragLeaveActionId { get; set; }
    string DragOverActionId { get; set; }
    string DragStartActionId { get; set; }
    string DropActionId { get; set; }
    int ElementId { get; set; }
    UIIconType Icon { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PressChangeActionId { get; set; }
    string PressEndActionId { get; set; }
    string PressStartActionId { get; set; }
    string PressUpActionId { get; set; }
    string StyleId { get; set; }
    static UIIconButton ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIIconButton ReadFromTeleport(ReadOnlySpan<byte> data, UIIconButton? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIIconType
    None
    Close
    Download
    Delete
    PinDrop
    Favorite
    FavoriteBorder
    AddCircle
    AddCircleOutline
    StarOutline
    Document
    GenderMale
    GenderFemale
    Upload
    GenderOther
  sealed class UIImage : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, string url, string mime, byte[] data, string clickActionId, string pressStartActionId, string pressEndActionId, string pressChangeActionId, string pressUpActionId, string dragStartActionId, string dragEnterActionId, string dragLeaveActionId, string dragOverActionId, string dropActionId, string dragEndActionId)
    string ClickActionId { get; set; }
    byte[] Data { get; set; }
    string DragEndActionId { get; set; }
    string DragEnterActionId { get; set; }
    string DragLeaveActionId { get; set; }
    string DragOverActionId { get; set; }
    string DragStartActionId { get; set; }
    string DropActionId { get; set; }
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Mime { get; set; }
    string Name { get; set; }
    string PressChangeActionId { get; set; }
    string PressEndActionId { get; set; }
    string PressStartActionId { get; set; }
    string PressUpActionId { get; set; }
    string StyleId { get; set; }
    string Url { get; set; }
    static UIImage ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIImage ReadFromTeleport(ReadOnlySpan<byte> data, UIImage? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIInit : IProtocolMessagePayload
    ctor()
    ctor(List<UIInit.UIInitModule> modules)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    List<UIInit.UIInitModule> Modules { get; set; }
    static UIInit ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIInit ReadFromTeleport(ReadOnlySpan<byte> data, UIInit? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIInit.UIInitModule
    ctor()
    ctor(string name, string? javascript)
    string? Javascript { get; set; }
    string Name { get; set; }
    static UIInit.UIInitModule ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIInit.UIInitModule ReadFromTeleport(ReadOnlySpan<byte> data, UIInit.UIInitModule? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIInputText : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, string updateActionId, UIColor color, UIInputVariant variant, int rows, string initialValue, string submitActionId)
    UIColor Color { get; set; }
    int ElementId { get; set; }
    string InitialValue { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Name { get; set; }
    int Rows { get; set; }
    string StyleId { get; set; }
    string SubmitActionId { get; set; }
    string UpdateActionId { get; set; }
    UIInputVariant Variant { get; set; }
    static UIInputText ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIInputText ReadFromTeleport(ReadOnlySpan<byte> data, UIInputText? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIInputVariant
    Outlined
    Filled
    Standard
  sealed class UIListBegin : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, UIListType type)
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StyleId { get; set; }
    UIListType Type { get; set; }
    static UIListBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIListBegin ReadFromTeleport(ReadOnlySpan<byte> data, UIListBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIListEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIListEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIListEnd ReadFromTeleport(ReadOnlySpan<byte> data, UIListEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIListItem : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, string text)
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Name { get; set; }
    string StyleId { get; set; }
    string Text { get; set; }
    static UIListItem ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIListItem ReadFromTeleport(ReadOnlySpan<byte> data, UIListItem? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIListType
    Default
    Unordered
    Ordered
    Definition
  sealed class UIMap : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, UIMap.UIMapMarker marker, List<UIMap.UIMapMarker> markers)
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    UIMap.UIMapMarker Marker { get; set; }
    List<UIMap.UIMapMarker> Markers { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Name { get; set; }
    string StyleId { get; set; }
    static UIMap ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIMap ReadFromTeleport(ReadOnlySpan<byte> data, UIMap? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIMap.UIMapMarker
    ctor()
    ctor(string title, float latitude, float longitude)
    float Latitude { get; set; }
    float Longitude { get; set; }
    string Title { get; set; }
    static UIMap.UIMapMarker ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIMap.UIMapMarker ReadFromTeleport(ReadOnlySpan<byte> data, UIMap.UIMapMarker? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIMaterialSymbol : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, UIColor color, UIMaterialSymbolVariant variant)
    UIColor Color { get; set; }
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Name { get; set; }
    string StyleId { get; set; }
    UIMaterialSymbolVariant Variant { get; set; }
    static UIMaterialSymbol ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIMaterialSymbol ReadFromTeleport(ReadOnlySpan<byte> data, UIMaterialSymbol? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIMaterialSymbolVariant
    Default
    Filled
  sealed class UIPayload : IProtocolMessagePayload
    ctor()
    ctor(string mimeType, byte[] value)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string MimeType { get; set; }
    byte[] Value { get; set; }
    static UIPayload ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIPayload ReadFromTeleport(ReadOnlySpan<byte> data, UIPayload? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIProgressBar : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, float percentage)
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    float Percentage { get; set; }
    string StyleId { get; set; }
    static UIProgressBar ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIProgressBar ReadFromTeleport(ReadOnlySpan<byte> data, UIProgressBar? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIQS : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string name, string eventActionId, Dictionary<string, string> argumentsJson)
    Dictionary<string, string> ArgumentsJson { get; set; }
    int ElementId { get; set; }
    string EventActionId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Name { get; set; }
    string StyleId { get; set; }
    static UIQS ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIQS ReadFromTeleport(ReadOnlySpan<byte> data, UIQS? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UISectionBegin : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, UISectionType type, int gap, string clickActionId, string pressStartActionId, string pressEndActionId, string pressChangeActionId, string pressUpActionId, string dragStartActionId, string dragEnterActionId, string dragLeaveActionId, string dragOverActionId, string dropActionId, string dragEndActionId)
    string ClickActionId { get; set; }
    string DragEndActionId { get; set; }
    string DragEnterActionId { get; set; }
    string DragLeaveActionId { get; set; }
    string DragOverActionId { get; set; }
    string DragStartActionId { get; set; }
    string DropActionId { get; set; }
    int ElementId { get; set; }
    int Gap { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string PressChangeActionId { get; set; }
    string PressEndActionId { get; set; }
    string PressStartActionId { get; set; }
    string PressUpActionId { get; set; }
    string StyleId { get; set; }
    UISectionType Type { get; set; }
    static UISectionBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static UISectionBegin ReadFromTeleport(ReadOnlySpan<byte> data, UISectionBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UISectionEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UISectionEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static UISectionEnd ReadFromTeleport(ReadOnlySpan<byte> data, UISectionEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UISectionType
    Default
    ColumnLayout
    RowLayout
    Card
    Right
    Carousel
    ScrollView
  sealed class UISeparator : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId)
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StyleId { get; set; }
    static UISeparator ReadFromTeleport(ReadOnlySpan<byte> data)
    static UISeparator ReadFromTeleport(ReadOnlySpan<byte> data, UISeparator? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIStreamBegin : IProtocolMessagePayload
    ctor()
    ctor(string category)
    string Category { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data, UIStreamBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class UIStreamCategories
    static string App
    static string Chat
    static string Collapsed
    static string DebugOverlay
    static string Footer
    static string Header
    static string Input
    static string Menu
    static string Overlay
    static string Preview
    static string SecondScreen
  sealed class UIStreamEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIStreamEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIStreamEnd ReadFromTeleport(ReadOnlySpan<byte> data, UIStreamEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class GlobalState.UIStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, UIStreamBegin info)
    int ClientSessionId { get; set; }
    UIStreamBegin Info { get; set; }
    string StreamId { get; set; }
    int TrackId { get; set; }
    static GlobalState.UIStreamState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState.UIStreamState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState.UIStreamState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIStyles : IProtocolMessagePayload
    ctor()
    ctor(string styleId, Dictionary<string, string> style)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Dictionary<string, string> Style { get; set; }
    string StyleId { get; set; }
    static UIStyles ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIStyles ReadFromTeleport(ReadOnlySpan<byte> data, UIStyles? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIStylesBatch : IProtocolMessagePayload
    ctor()
    ctor(List<UIStylesBatch.UIStylesBatchItem> styles)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    List<UIStylesBatch.UIStylesBatchItem> Styles { get; set; }
    static UIStylesBatch ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIStylesBatch ReadFromTeleport(ReadOnlySpan<byte> data, UIStylesBatch? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIStylesBatch.UIStylesBatchItem
    ctor()
    ctor(string styleId, Dictionary<string, string> style)
    Dictionary<string, string> Style { get; set; }
    string StyleId { get; set; }
    static UIStylesBatch.UIStylesBatchItem ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIStylesBatch.UIStylesBatchItem ReadFromTeleport(ReadOnlySpan<byte> data, UIStylesBatch.UIStylesBatchItem? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIStylesDelete : IProtocolMessagePayload
    ctor()
    ctor(List<string> styleIds)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    List<string> StyleIds { get; set; }
    static UIStylesDelete ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIStylesDelete ReadFromTeleport(ReadOnlySpan<byte> data, UIStylesDelete? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class UIStylesKeys
    static string Common
    static string Crosswind
    static string Css
    static string Flutter
    static string ReactNative
  sealed class UISvg : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string svg)
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StyleId { get; set; }
    string Svg { get; set; }
    static UISvg ReadFromTeleport(ReadOnlySpan<byte> data)
    static UISvg ReadFromTeleport(ReadOnlySpan<byte> data, UISvg? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIText : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string text, UITextType type, UIColor color)
    UIColor Color { get; set; }
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string StyleId { get; set; }
    string Text { get; set; }
    UITextType Type { get; set; }
    static UIText ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIText ReadFromTeleport(ReadOnlySpan<byte> data, UIText? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UITextType
    Normal
    Caption
    Strong
    CaptionSmall
    Small
  sealed class UIUpdate : IProtocolMessagePayload
    ctor()
    ctor(string json, Dictionary<string, UIPayload> payloads)
    string Json { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Dictionary<string, UIPayload> Payloads { get; set; }
    static UIUpdate ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIUpdate ReadFromTeleport(ReadOnlySpan<byte> data, UIUpdate? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIUpdateAck : IProtocolMessagePayload
    ctor()
    ctor(uint version)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    uint Version { get; set; }
    static UIUpdateAck ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIUpdateAck ReadFromTeleport(ReadOnlySpan<byte> data, UIUpdateAck? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIUpdateBegin : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIUpdateBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIUpdateBegin ReadFromTeleport(ReadOnlySpan<byte> data, UIUpdateBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIUpdateEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static UIUpdateEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIUpdateEnd ReadFromTeleport(ReadOnlySpan<byte> data, UIUpdateEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class UIVegaChart : IProtocolMessagePayload, IUIContainerElement
    ctor()
    ctor(int elementId, List<string> labels, string styleId, string dataJson, string specJson)
    string DataJson { get; set; }
    int ElementId { get; set; }
    List<string> Labels { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string SpecJson { get; set; }
    string StyleId { get; set; }
    static UIVegaChart ReadFromTeleport(ReadOnlySpan<byte> data)
    static UIVegaChart ReadFromTeleport(ReadOnlySpan<byte> data, UIVegaChart? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum UIVisibilityType
    Always
    AfterEarlierStable
  sealed class UpdateClientContext : IProtocolMessagePayload
    ctor()
    ctor(int viewportWidth, int viewportHeight, string theme, string timezone)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Theme { get; set; }
    string Timezone { get; set; }
    int ViewportHeight { get; set; }
    int ViewportWidth { get; set; }
    static UpdateClientContext ReadFromTeleport(ReadOnlySpan<byte> data)
    static UpdateClientContext ReadFromTeleport(ReadOnlySpan<byte> data, UpdateClientContext? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class ActionFunctionCall.UserCredentialEntry
    ctor()
    ctor(string name, string value)
    string Name { get; set; }
    string Value { get; set; }
    static ActionFunctionCall.UserCredentialEntry ReadFromTeleport(ReadOnlySpan<byte> data)
    static ActionFunctionCall.UserCredentialEntry ReadFromTeleport(ReadOnlySpan<byte> data, ActionFunctionCall.UserCredentialEntry? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
  sealed class VideoFrame : IProtocolMessagePayload
    ctor()
    ctor(byte[] data, int frameNumber, bool isKey, ulong timestampInUs, uint durationInUs)
    byte[] Data { get; set; }
    uint DurationInUs { get; set; }
    int FrameNumber { get; set; }
    bool IsKey { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    ulong TimestampInUs { get; set; }
    static VideoFrame ReadFromTeleport(ReadOnlySpan<byte> data)
    static VideoFrame ReadFromTeleport(ReadOnlySpan<byte> data, VideoFrame? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
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
    static VideoStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data)
    static VideoStreamBegin ReadFromTeleport(ReadOnlySpan<byte> data, VideoStreamBegin? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class VideoStreamEnd : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static VideoStreamEnd ReadFromTeleport(ReadOnlySpan<byte> data)
    static VideoStreamEnd ReadFromTeleport(ReadOnlySpan<byte> data, VideoStreamEnd? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class GlobalState.VideoStreamState
    ctor()
    ctor(string streamId, int clientSessionId, int trackId, VideoStreamBegin info)
    int ClientSessionId { get; set; }
    VideoStreamBegin Info { get; set; }
    string StreamId { get; set; }
    int TrackId { get; set; }
    static GlobalState.VideoStreamState ReadFromTeleport(ReadOnlySpan<byte> data)
    static GlobalState.VideoStreamState ReadFromTeleport(ReadOnlySpan<byte> data, GlobalState.VideoStreamState? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCAnswer : IProtocolMessagePayload
    ctor()
    ctor(string sdp, string iceServersJson)
    string IceServersJson { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    string Sdp { get; set; }
    static WebRTCAnswer ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCAnswer ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCAnswer? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCAudioSegment : IProtocolMessagePayload
    ctor()
    ctor(bool isStart, string? correlationId)
    string? CorrelationId { get; set; }
    bool IsStart { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static WebRTCAudioSegment ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCAudioSegment ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCAudioSegment? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCClose : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static WebRTCClose ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCClose ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCClose? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCIceCandidate : IProtocolMessagePayload
    ctor()
    ctor(string candidate, string sdpMid, int sdpMLineIndex)
    string Candidate { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SdpMLineIndex { get; set; }
    string SdpMid { get; set; }
    static WebRTCIceCandidate ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCIceCandidate ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCIceCandidate? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCIceServersRequest : IProtocolMessagePayload
    ctor()
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static WebRTCIceServersRequest ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCIceServersRequest ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCIceServersRequest? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCIceServersResponse : IProtocolMessagePayload
    ctor()
    ctor(string iceServersJson)
    string IceServersJson { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    static WebRTCIceServersResponse ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCIceServersResponse ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCIceServersResponse? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCOffer : IProtocolMessagePayload
    ctor()
    ctor(string sdp, Opcode opcodeGroupsFromServer, Opcode opcodeGroupsToServer, bool useAudioTrack, bool useVideoTrack, bool useDataChannel)
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    Opcode OpcodeGroupsFromServer { get; set; }
    Opcode OpcodeGroupsToServer { get; set; }
    string Sdp { get; set; }
    bool UseAudioTrack { get; set; }
    bool UseDataChannel { get; set; }
    bool UseVideoTrack { get; set; }
    static WebRTCOffer ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCOffer ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCOffer? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCReady : IProtocolMessagePayload
    ctor()
    ctor(bool success, string errorMessage)
    string ErrorMessage { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    bool Success { get; set; }
    static WebRTCReady ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCReady ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCReady? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCTrackMap : IProtocolMessagePayload
    ctor()
    ctor(string kind, int trackIndex, int senderId, int senderTrackId, string streamId, string sourceType, bool active)
    bool Active { get; set; }
    string Kind { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SenderId { get; set; }
    int SenderTrackId { get; set; }
    string SourceType { get; set; }
    string StreamId { get; set; }
    int TrackIndex { get; set; }
    static WebRTCTrackMap ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCTrackMap ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCTrackMap? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class WebRTCVideoCapture : IProtocolMessagePayload
    ctor()
    ctor(int senderIndex, string? correlationId)
    string? CorrelationId { get; set; }
    Opcode MessageOpcode { get; }
    int MessageVersion { get; }
    int SenderIndex { get; set; }
    static WebRTCVideoCapture ReadFromTeleport(ReadOnlySpan<byte> data)
    static WebRTCVideoCapture ReadFromTeleport(ReadOnlySpan<byte> data, WebRTCVideoCapture? destination)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion

namespace Ikon.Common.Core.Reactive
  // Factory methods for creating ClientReactive`1 with per-client initialization.
  static class ClientReactive
    static ClientReactive<T> Create<T>(Func<int, T> factory, string file = "", string member = "")
  // Shorthand for ReactiveEffect<ClientScope>. Mirrors ClientReactive<T> as the per-client variant of Reactive<T>. Each connected client gets its own runner with independent cancel/queue, materialized on first dep change inside that client's scope.
  class ClientReactiveEffect : ReactiveEffect<ClientScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // A reactive variable with a separate value for each client session.
  class ClientReactive<T> : Reactive<T, ClientScope>
    ctor(T initialValue, string file = "", string member = "")
  sealed class ReactiveManager.Handle
    string DebugDescription { get; set; }
    int? GroupId { get; set; }
    Guid Id { get; }
    bool IsUpdate { get; }
    DateTime? UpdatedAt { get; }
    void StopTracking(bool isUpdating)
    override string ToString()
  sealed class HotReloadStateStore : AsyncLocalInstance<HotReloadStateStore>
    ctor()
    Dictionary<string, StoredReactiveState> CaptureAllForHotReload()
    void Clear()
    IReadOnlyList<PersistedRegistration> GetPersistedRegistrations()
    void LoadHotReloadStates(Dictionary<string, StoredReactiveState> states)
    void Register(string stableId, IReactiveWithState reactive, PersistenceScope persistence, PersistenceBackend backend = Private, string? postgresDatabase = null)
    // Look up a registered reactive by its stable id. Returns false if no reactive is registered under stableId or if the underlying reactive has been garbage-collected.
    bool TryGet(string stableId, out IReactiveWithState? reactive)
  interface IPersistedReactive : IReactiveWithState
    abstract void SetPublicUrl(string? url)
  interface IReactive
    long Version { get; }
    // Fires whenever this reactive's value changes (in any scope, for scoped variants). Payload-free so a single subscription can be taken across heterogeneous reactives — handlers fetch the new value via .Value when they need it. Used by ReactiveEffect and other dependency-tracked consumers.
    event Action? Changed
    // Fires with the scope-derived session id whose Signal<T> value just changed. For unscoped reactives the id is always 0; for ClientReactive<T> it is the hash of ClientScope; for UserReactive<T> the hash of UserScope; etc. Lets external subscription routing fan out to only the clients whose scope matches the changed signal.
    event Action<int>? SessionChanged
  interface IReactiveWithState
    // Whether this reactive's value is captured for hot-reload state preservation. Default true. Runtime-only caches that hold non-serializable or cyclic object graphs — and that rehydrate from their own backing store after a reload — opt out by returning false, so the hot-reload capture pass skips them instead of logging a (harmless) serialization warning every reload. Does not affect long-term persistence (which only ever touches non-None PersistenceScope s).
    bool CaptureForHotReload { get; }
    // Hash-derived session id that this reactive's .Value would resolve to under the currently-active ReactiveScope . Used by the subscription service to key per-scope subscriber routing. Default implementation returns 0 — override on per-scope reactives.
    int CurrentScopeSessionId { get; }
    string StableId { get; }
    abstract StoredReactiveState CaptureState()
    // Read this reactive's value for the currently-active scope, serialize to JSON, and trigger per-scope initialization if needed. Default implementation returns the session-0 value from CaptureState .
    virtual string ReadCurrentValueAsJson()
    abstract void RestoreState(StoredReactiveState state)
  // Factory methods for creating MountReactive`1 with per-mount initialization.
  static class MountReactive
    static MountReactive<T> Create<T>(Func<string, T> factory, string file = "", string member = "")
  // A reactive variable with a separate value for each Parallax mount in the active render iteration.
  class MountReactive<T> : Reactive<T, MountScope>
    ctor(T initialValue, string file = "", string member = "")
  sealed class PersistedRegistration
    ctor(string stableId, IReactiveWithState reactive, PersistenceScope persistence, PersistenceBackend backend, string? postgresDatabase)
    PersistenceBackend Backend { get; }
    PersistenceScope Persistence { get; }
    string? PostgresDatabase { get; }
    IReactiveWithState Reactive { get; }
    string StableId { get; }
  // Selects the backing store for a persistent reactive.
  enum PersistenceBackend
    Private
    Public
    Postgres
  // Identifies where a reactive's value is persisted in cloud storage and how it is keyed.
  enum PersistenceScope
    None
    Global
    Session
    User
  static class Reactive
    static void Run<T>(Reactive<T> reactiveValue, Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken token = null)
    static void Run<T>(Reactive<T> reactiveValue, Func<CancellationToken, Task<T>> action, Action<Exception>? onError = null, CancellationToken token = null)
  // Convenience helpers on Reactive`1 for the busy-flag pattern that every async handler uses. Without these, the standard shape is verbose and easy to break: _busy.Value = true; try { await SlowThingAsync(); } finally { _busy.Value = false; } Forgetting finally leaves the flag stuck on if the call throws. AsToken collapses the shape to: using var _ = _busy.AsToken(); await SlowThingAsync(); — the flag flips to true on entry, the IDisposable returns it to false on dispose (including the catch-and-rethrow path of using).
  static class ReactiveBoolExtensions
    // Set the flag to true and return an IDisposable that returns it to false on dispose. Idempotent — disposing twice is safe (the second dispose is a no-op).
    static IDisposable AsToken(Reactive<bool> reactive)
  // Mutation helpers for Reactive`1 wrapping a collection. They mutate the underlying instance AND fire NotifyUpdate in one call so callers can write _items.Add(x) instead of the two-step _items.Value.Add(x); _items.NotifyUpdate();. Why these exist on a Reactive wrapping a mutable collection: the reference-equality check at the Value setter doesn't trigger when the underlying list is mutated in-place. Forgetting NotifyUpdate is the dominant "UI doesn't update after Add/Remove" bug class. These helpers make the right thing the easy thing. Reassignment (_items.Value = [.. _items.Value, x]) still works and stays the right form when callers want immutable-style updates; these helpers are the in-place alternative for the common case.
  static class ReactiveCollectionExtensions
    static void Add<T>(Reactive<List<T>> reactive, T item)
    static bool Add<T>(Reactive<HashSet<T>> reactive, T item)
    static void AddRange<T>(Reactive<List<T>> reactive, IEnumerable<T> items)
    static void Clear<T>(Reactive<List<T>> reactive)
    static void Clear<T>(Reactive<HashSet<T>> reactive)
    static void Clear<TKey, TValue>(Reactive<Dictionary<TKey, TValue>> reactive)
    static void Insert<T>(Reactive<List<T>> reactive, int index, T item)
    static void Mutate<T>(Reactive<T> reactive, Action<T> mutator)
    static bool Remove<T>(Reactive<List<T>> reactive, T item)
    static bool Remove<T>(Reactive<HashSet<T>> reactive, T item)
    static bool Remove<TKey, TValue>(Reactive<Dictionary<TKey, TValue>> reactive, TKey key)
    static int RemoveAll<T>(Reactive<List<T>> reactive, Predicate<T> match)
    static void RemoveAt<T>(Reactive<List<T>> reactive, int index)
    static void Set<TKey, TValue>(Reactive<Dictionary<TKey, TValue>> reactive, TKey key, TValue value)
  // Side-effect primitive that runs on tracked IReactive dependency changes. Mirrors the shape of Reactive`1 / Reactive`2 : this class is the unscoped (global) variant; ReactiveEffect`1 binds to a single scope type; further generic variants (forthcoming) compose multiple scopes the same way Reactive<T, TScope1, TScope2> does.
  class ReactiveEffect : IDisposable
    // Create an effect with an async body. The token cancels when a dep changes mid-run; respect it for clean cancellation.
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    // Create an effect with a sync body.
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  // Side-effect primitive bound to a single scope type. Mirrors Reactive<T, TScope>: each instance of TScope gets its own per-scope effect runner with independent cancel/queue state, materialized lazily on first dep change in that scope. Unlike the global ReactiveEffect , this variant does NOT fire eagerly at construction — there's no scope active yet. The first dep change observed inside a scope of type TScope instantiates that scope's runner and fires the body for the first time. For "fire when scope first opens regardless of deps" lifecycle hooks (e.g. preload data on client connect), use the host app's existing scope-creation events directly.
  class ReactiveEffect<TScope> : IDisposable where TScope : struct, IScopeKey
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
    void Dispose()
  class ReactiveManager : IDisposable
    ctor(string category)
    string Category { get; }
    int UpdatedHandleCount { get; }
    void DecrementUICreationOngoing()
    void Dispose()
    void IncrementUICreationOngoing()
    void OnDeleted(Guid id)
    void Reactive(Action<ReactiveManager.Handle> callback)
    Task ReactiveAsync(Func<ReactiveManager.Handle, Task> callback)
    void StopTrackingAll()
    // Detach the current execution flow from any in-progress reactive callback so that reactive reads and writes made here are treated as ordinary access instead of being attributed to — or, for writes, swallowed by the re-entrancy guard of — the enclosing callback. This is needed when background work (a Run , a continuation, a timer) is started from INSIDE a reactive callback, e.g. a UI render that, while rendering, kicks off a fire-and-forget task to resolve an image and then bumps a reactive to re-render once it arrives. ExecutionContext flows the callback's async-local into that task, so without detaching the bump is misclassified as happening "within a reactive callback", dropped, and the UI never refreshes. Unlike SuppressFlow , this leaves every other ambient value (reactive scopes, async-local singletons) intact, so code inside still resolves the session's services correctly. Wrap the detached work in a using block (or hold the returned handle for the lifetime of the background task) and the original tracking is restored on dispose.
    static IDisposable SuppressCallbackTracking()
    Task UpdateAsync()
    event EventHandler<Guid>? Deleted
    event EventHandler? ReactiveObjectUpdated
    event EventHandler<Guid>? Updating
  // App-scoped lookup mapping a reactive's source-code member name (the C# field or property declared on the App class) to its StableId . Built by reflection at App startup; consumed by the Ikon.Reactive.GetStableIdByName framework function so frontends can subscribe by member name instead of needing a per-app helper RPC to fetch the hashed id.
  static class ReactiveNameIndex
    // Drop every registered mapping. Used by hot-reload to clear stale entries from the previous App instance before the next instance re-indexes.
    static void Clear()
    // Register a reactive member-name → stableId mapping. Idempotent; re-registering the same name with the same stableId is a no-op.
    static void Register(string memberName, string stableId)
    // Look up a stableId by reactive member name. Returns false when the name was not indexed at startup (e.g. reactive declared in a helper class outside the App).
    static bool TryGet(string memberName, out string stableId)
  // A general-purpose scope stack that supports multiple overlapping scope types (Client, User, Tenant, etc.), each tracked independently. This is a static wrapper around a shared ScopeStack instance for the reactive system. Scope changes are automatically mirrored to Log.Instance for logging purposes.
  static class ReactiveScope
    static int ClientId { get; }
    static int? ClientIdOrNull { get; }
    static IList<IScopeKey> Current { get; }
    static string MountId { get; }
    static string? MountIdOrNull { get; }
    static string UserId { get; }
    static string? UserIdOrNull { get; }
    static void Add(IScopeKey scope)
    static TScope Get<TScope>() where TScope : struct, IScopeKey
    static IScopeKey GetByName(string name)
    static TScope? TryGet<TScope>() where TScope : struct, IScopeKey
    static bool TryGet<TScope>(out TScope scope) where TScope : struct, IScopeKey
    static IScopeKey? TryGetByName(string name)
    static IDisposable Use(IScopeKey scope)
    static IDisposable Use(params IScopeKey[] scopes)
  static class ReactiveScopeRestorer
    static IDisposable? Activate(IReadOnlyList<IScopeKey> scopes)
    static IScopeKey[] CaptureCurrent()
    static IScopeKey[] CopyInRestorableOrder(IList<IScopeKey> scopes)
  // Bridges Reactive`1 change notifications to remote clients over the existing function-call wire. Exposes three framework-shipped shared functions — Ikon.Reactive.Subscribe, Ikon.Reactive.Unsubscribe, and Ikon.Reactive.Update — so any FunctionRegistry -connected client can observe a server-side reactive value without registering a Parallax UI tree.
  sealed class ReactiveSubscriptionService : AsyncLocalInstance<ReactiveSubscriptionService>
    ctor()
    // Optional resolver: given a calling session id, returns the scopes that should be active during Subscribe/Unsubscribe so per-scope reactives resolve to the caller's natural session/user. Typically wired in app startup as sid => { var ctx = app.GlobalState.GetClientContext(sid); return [new ClientScope(ctx), new UserScope(ctx)]; }. When unset, the service falls back to [new ClientScope(sessionId)] only — ClientReactive`1 works, UserReactive`1 throws.
    Func<int, IReadOnlyList<IScopeKey>>? ScopeResolver { get; set; }
    // Wires this service's framework functions into the given registry. Call once during app/server startup, after the registry has its protocol channel attached.
    void AttachTo(FunctionRegistry registry)
    // Resolves a reactive's StableId by the C# field or property name on the App class. Used by the JS-side useReactive(client, name, ...) hook so frontends don't need a per-app helper RPC just to learn the stable id.
    string GetStableIdByName(string memberName)
    // Drop all subscriptions belonging to a session — call when a client disconnects to release subscriber-state without waiting for explicit unsubscribes.
    void RemoveSession(int sessionId)
    // Subscribe the calling session to changes on the reactive identified by stableId . Returns the current value as JSON. The caller receives subsequent updates via Ikon.Reactive.Update calls routed only to subscribers whose scope hash matches the changed signal.
    string Subscribe(string stableId, string mountId)
    // Unsubscribe the calling session from a reactive. Idempotent; calling for an unsubscribed reactive is a no-op. The mountId must match the value passed to Subscribe so the same scope hash is computed for symmetric removal.
    void Unsubscribe(string stableId, string mountId)
    static string GetStableIdByNameFunctionName
    static string SubscribeFunctionName
    static string UnsubscribeFunctionName
    static string UpdateFunctionName
  // A reactive variable that automatically triggers UI updates when its value changes.
  class Reactive<T> : IReactive, IReactiveWithState
    ctor(UseDefault _ = null, string file = "", string member = "")
    ctor(T initialValue, string file = "", string member = "")
    bool CaptureForHotReload { get; }
    // Hash-derived session id that Value would resolve to under the currently-active ReactiveScope . Throws if a required scope is missing — same conditions as accessing Value . External subscribers use this to key their subscription routing.
    int CurrentScopeSessionId { get; }
    T Peek { get; }
    string StableId { get; }
    T Value { get; set; }
    long Version { get; }
    StoredReactiveState CaptureState()
    // Opt this reactive out of hot-reload state capture. Use for runtime-only caches that hold non-serializable or cyclic object graphs and are rebuilt from their own backing store after a reload (e.g. orchestrator caches of live domain objects) — capturing them only fails noisily. Fluent: returns this so it can be chained onto a field initializer. Has no effect on long-term persistence, which only applies to non-None PersistenceScope s.
    Reactive<T> ExcludeFromHotReloadCapture()
    void NotifyUpdate()
    // Read this reactive's value for the currently-active scope and serialize it to JSON. Triggers per-scope initialization if no signal exists yet — the returned JSON is the initial value the consumer should observe.
    string ReadCurrentValueAsJson()
    void RestoreState(StoredReactiveState state)
    override string ToString()
    event Action? Changed
    event Action<int>? SessionChanged
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
  // A reactive variable scoped to a specific scope type, providing isolated values per scope instance.
  class Reactive<T, TScope> : Reactive<T> where TScope : IScopeKey
    ctor(T initialValue, string file = "", string member = "")
    ctor(Func<T> initialValue, string file = "", string member = "")
  class Signal<T> : IReactive
    ctor(T initial)
    T Peek { get; }
    T Value { get; set; }
    long Version { get; }
    void NotifyUpdate()
    event Action? Changed
    event Action<int>? SessionChanged
    event Action<T>? ValueChanged
    event Func<T, Task>? ValueChangedAsync
  class StoredReactiveState
    ctor()
    ctor(string typeName, string memberName, int ordinal, Dictionary<int, string> sessionValues)
    string MemberName { get; set; }
    int Ordinal { get; set; }
    Dictionary<int, string> SessionValues { get; set; }
    string TypeName { get; set; }
  struct UseDefault
  // Shorthand for ReactiveEffect<UserScope>. Mirrors UserReactive<T> as the per-user variant of Reactive<T>. Each distinct user gets its own runner; the same user across multiple sessions shares one runner.
  class UserReactiveEffect : ReactiveEffect<UserScope>
    ctor(Func<CancellationToken, Task> body, params IReactive[] deps)
    ctor(Action body, params IReactive[] deps)
  // A reactive variable with a separate value for each user, shared across their client sessions.
  class UserReactive<T> : Reactive<T, UserScope>
    ctor(T initialValue, string file = "", string member = "")
    ctor(Func<string, T> initialValue, string file = "", string member = "")

namespace Ikon.Common.Core.Reflection
  // Reflection helpers for Task / ValueTask result types. Two pieces: a compile-time-style type unwrap for schema generation ( UnwrapResultType ), and a runtime await-and-extract for invocation sites that get an object? back from MethodInfo.Invoke ( AwaitAndGetResultAsync ).
  static class TaskTypeUnwrap
    // Take whatever MethodInfo.Invoke handed back and produce its observable result. Awaits Task , Task`1 , ValueTask , ValueTask`1 ; returns null for void-shaped awaitables; passes non-task values straight through. Used by dispatchers that hand off to user code reflectively and need a uniform object? back regardless of whether the method was sync, Task, or ValueTask.
    static ValueTask<object?> AwaitAndGetResultAsync(object? raw)
    // Map a method's declared return type to the type the method actually produces: Task/ValueTask → Object (void-equivalent — there is no result), Task<T>/ValueTask<T> → T, anything else → as-is. Schema generators feed the result of this through the type → JSON-schema pipeline so async methods produce sensible outputSchema entries.
    static Type UnwrapResultType(Type declaredReturnType)

namespace Ikon.Common.Core.Scope
  // Scope for backend token context, transports the backend token of the caller.
  struct BackendTokenScope : IScopeKey
    // Scope for backend token context, transports the backend token of the caller.
    ctor(string token)
    string Id { get; }
    string Name { get; }
  // Scope for client session context, providing unique identity for each connected client.
  struct ClientScope : IScopeKey
    ctor(int sessionId)
    ctor(Context context)
    int Id { get; }
    string Name { get; }
  // Scope with a user-specified name and ID, enabling dynamic scoping without needing new struct types.
  struct CustomScope : IScopeKey
    ctor(string name, string id)
    string Id { get; }
    string Name { get; }
  interface IScopeKey
    object Id { get; }
    string Name { get; }
  // Identifies the Parallax render target ("mount") an app is currently producing UI for. An app may declare multiple mounts via Mounts ; each ( ClientScope , MountScope ) pair gets its own per-render UI tree and an independent stream on the wire. Default mount id is "ikon-ui" — the value every app emits today on its single stream.
  struct MountScope : IScopeKey
    ctor(string mountId)
    string Id { get; }
    string Name { get; }
    // The mount id every Ikon app emits today on its single Parallax stream. Apps that don't override IAppBase.Mounts render under this id.
    static string DefaultMountId
  // Scope for grouping a single logical operation (e.g., LLM generation, image generation).
  struct OperationScope : IScopeKey
    ctor()
    // Scope for grouping a single logical operation (e.g., LLM generation, image generation).
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  // Scope for application run context, typically set at program startup in Program.cs. Used to group all log events and operations within a single application run.
  struct RunScope : IScopeKey
    ctor()
    // Scope for application run context, typically set at program startup in Program.cs. Used to group all log events and operations within a single application run.
    ctor(Guid id)
    Guid Id { get; }
    string Name { get; }
  class ScopeRestorer
    ctor(ScopeStack scopeStack)
    IDisposable? Activate(IReadOnlyList<IScopeKey> scopes)
    IScopeKey[] CaptureCurrent()
    static IScopeKey[] CopyInRestorableOrder(IList<IScopeKey> scopes)
  // Serializes and deserializes scopes for function call propagation.
  static class ScopeSerializer
    // Captures current scopes for inclusion in a function call. Excludes RunScope since each process has its own run context.
    static List<ActionFunctionCall.ScopeEntry> CaptureForFunctionCall()
    static IScopeKey[] Deserialize(IReadOnlyList<ActionFunctionCall.ScopeEntry> entries)
  class ScopeStack
    ctor()
    IList<IScopeKey> Current { get; }
    void Add(IScopeKey scope)
    TScope Get<TScope>() where TScope : struct, IScopeKey
    IScopeKey GetByName(string name)
    TScope? TryGet<TScope>() where TScope : struct, IScopeKey
    bool TryGet<TScope>(out TScope scope) where TScope : struct, IScopeKey
    IScopeKey? TryGetByName(string name)
    IDisposable Use(IScopeKey scope)
    IDisposable UseScopes(params IScopeKey[] scopes)
  // Scope for tenant/customer context, an arbitrary user-specified ID for scoping AI app logic.
  struct TenantScope : IScopeKey
    // Scope for tenant/customer context, an arbitrary user-specified ID for scoping AI app logic.
    ctor(string tenantId)
    string Id { get; }
    string Name { get; }
  // Scope for end user identity context, providing unique identity for each user.
  struct UserScope : IScopeKey
    ctor(string userId)
    ctor(Context context)
    string Id { get; }
    string Name { get; }

namespace Ikon.Common.Core.Signing
  sealed class SignatureDocument : IEquatable<SignatureDocument>
    ctor(string Filename, string MimeType, byte[] Bytes)
    byte[] Bytes { get; init; }
    string Filename { get; init; }
    string MimeType { get; init; }
  sealed class SignatureOrderRequest : IEquatable<SignatureOrderRequest>
    ctor(string Purpose, IReadOnlyList<SignatureDocument> Documents, SignatureSigner Signer, string? CostAttributionKey = null, string? Title = null, string? ClientReturnUrl = null)
    string? ClientReturnUrl { get; init; }
    string? CostAttributionKey { get; init; }
    IReadOnlyList<SignatureDocument> Documents { get; init; }
    string Purpose { get; init; }
    SignatureSigner Signer { get; init; }
    string? Title { get; init; }
  enum SignaturePolicy
    PkiSigning
    EidHub
  sealed class SignatureSigner : IEquatable<SignatureSigner>
    ctor(SignaturePolicy Policy, string? Vendor = null, IReadOnlyList<string>? IdpNames = null, IReadOnlyList<string>? RequestedAttributes = null)
    IReadOnlyList<string>? IdpNames { get; init; }
    SignaturePolicy Policy { get; init; }
    IReadOnlyList<string>? RequestedAttributes { get; init; }
    string? Vendor { get; init; }
  // Represents a successfully signed document returned by the platform signing service. The platform downloads the result from the upstream signing vendor, hashes it, and hands the signed bytes plus evidence metadata to the requesting app. Apps should persist Bytes as the system of record — the platform retention is short.
  sealed class SignedDocument : IEquatable<SignedDocument>
    // Represents a successfully signed document returned by the platform signing service. The platform downloads the result from the upstream signing vendor, hashes it, and hands the signed bytes plus evidence metadata to the requesting app. Apps should persist Bytes as the system of record — the platform retention is short.
    ctor(string OrderId, byte[] Bytes, string MimeType, DateTimeOffset SignedAt, string SignedDocumentHash, string IdentityScheme, string? SignerNameHash, string? EvidenceLevel)
    byte[] Bytes { get; init; }
    string? EvidenceLevel { get; init; }
    string IdentityScheme { get; init; }
    string MimeType { get; init; }
    string OrderId { get; init; }
    DateTimeOffset SignedAt { get; init; }
    string SignedDocumentHash { get; init; }
    string? SignerNameHash { get; init; }
