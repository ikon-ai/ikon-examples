namespace Ikon.App
  // Credit cost surface for an Ikon app: what AI models its space has used and what that usage cost in platform credits. Accessed via app.Costs, reported per day and per usage event name. Cost data is aggregated in the analytics pipeline, so very recent usage can take a short while to appear.
  sealed class CostsService
    // The date range still has to cover when the work ran: usage is stored by day, and a query is only as cheap as the range it scans. An operation that emitted no priced usage sums to zero, which is indistinguishable from one whose usage has not landed yet — see the note on aggregation delay on CostsService before showing the number as final.
    Task<double> GetCreditsForScopeAsync(string scopeType, string scopeId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    // Throws ArgumentException when CostQuery.StartDate is after CostQuery.EndDate. Returns one row per day and usage event name; days without usage produce no rows. Under CostQuery.GroupByScopeType the breakdown is per scope id as well. The result is ordered by date, then event name.
    Task<IReadOnlyList<DailyCost>> GetDailyCostsAsync(CostQuery query, CancellationToken ct = default)
    // The date range is inclusive and interpreted in UTC.
    Task<double> GetTotalCreditsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
  // A [Cron] method behaves like a [Function] in that the trigger resolves it through the FunctionRegistry by name. Applying [Cron] is enough to register the method (as a Local function) — you do not also need [Function], though combining them is fine. The handler takes no caller-supplied arguments. It may optionally accept a host-injected CronContext (fire time + schedule) and/or a CancellationToken that signals app shutdown, in any order — mirroring how an [HttpPost] handler may accept an HttpRequest. Any other parameter fails registration at startup, since the scheduler has nothing to bind it to. Overlap is allowed: a tick fires even if the previous invocation is still running, so guard re-entrancy yourself if it matters.
  sealed class CronAttribute : Attribute
    ctor(string schedule)
    // When null or empty the function is registered (and triggered) under "{DeclaringType.FullName}.{Method}" — the identity the bundle manifest records, so the backend trigger resolves it even when the method is inherited or overridden.
    string? Name { get; init; }
    // Standard 5/6-field cron syntax (e.g. "0 * * * *" for hourly), evaluated by the backend scheduler. The platform enforces a minimum interval of 5 minutes: a faster schedule is clamped to a slower equivalent when a safe one exists, and rejected at bundle time otherwise.
    string Schedule { get; }
  // Credit cost aggregate for one usage event name on one day. Credits is the cost in platform credits — the unit users are billed in. EventName identifies the AI model and usage kind (e.g. llm.openai.gpt4o.global.output-text-tokens) and Category is its first segment (e.g. llm). TotalUsage is the summed usage amount in the event's native unit (tokens, seconds, generations, ...). RawCostEur is the underlying provider cost in EUR and is null unless the space has raw cost visibility enabled. ScopeId is populated only under CostQuery.GroupByScopeType, and is null for usage carrying no scope of that type.
  sealed record DailyCost
    ctor(DateOnly Date, string Category, string EventName, double TotalUsage, double Credits, double? RawCostEur, string? ScopeId = null)
    string Category { get; init; }
    double Credits { get; init; }
    DateOnly Date { get; init; }
    string EventName { get; init; }
    double? RawCostEur { get; init; }
    string? ScopeId { get; init; }
    double TotalUsage { get; init; }
  sealed class EmailNotificationChannel : INotificationChannel
    // email: The app's email service.
    // addressOf: Returns the user's email address, or null when none is known.
    // senderLocalPart: Optional sender local part, as on EmailSendRequest.
    ctor(EmailService email, Func<string, string?> addressOf, string? senderLocalPart = null, string? senderDisplayName = null)
    string Name { get; }
    Task<bool> SendAsync(string userId, NotificationContent content, CancellationToken ct)
  // Accessed via app.Email. Every operation requires the app's space to have the Email feature enabled; a call against a non-entitled space throws FeatureNotEnabledException.
  sealed class EmailService
    // The backend resolves the id before deleting and rejects an unknown one, so a repeated delete throws HttpRequestException carrying a 404 rather than being treated as a no-op. Callers sweeping ids they no longer track should catch it.
    Task DeleteAsync(string id, CancellationToken ct = default)
    // The returned EmailAttachmentDownload owns the content stream; dispose it (e.g. await using) to release the underlying connection.
    Task<EmailAttachmentDownload> DownloadAttachmentAsync(string emailId, string attachmentId, CancellationToken ct = default)
    // Pages are fetched on demand as the sequence is consumed, so breaking out of the await foreach stops fetching further pages.
    IAsyncEnumerable<InboundEmailSummary> EnumerateInboxAsync(InboxQuery query, CancellationToken ct = default)
    // Paginate by passing the returned InboxPage.NextCursor back as InboxQuery.Cursor.
    Task<InboxPage> GetInboxPageAsync(InboxQuery query, CancellationToken ct = default)
    Task<InboundEmailDetail> GetMessageAsync(string id, CancellationToken ct = default)
    // A request that names a sender identity needs a verified sending domain: when the space has none, or the requested EmailSendRequest.SenderDomain is not one of the space's verified sending domains, the send throws EmailSenderNotAvailableException — catch it and resend without the sender fields to deliver from the platform's own address. Invalid field values throw ArgumentException before anything is sent, and a space without the Email feature throws FeatureNotEnabledException.
    Task SendAsync(EmailSendRequest request, CancellationToken ct = default)
  abstract class EndpointAttribute : Attribute
    // Defaults to EndpointAuth.Grant; setting AuthPolicy overrides it.
    EndpointAuth Auth { get; init; }
    // When non-empty, takes precedence over Auth.
    string? AuthPolicy { get; init; }
    // Empty = derived from the method name (kebab-cased). A {name} segment whose name matches a field of the owner's SessionIdentity record binds the routing identity; other {name} segments bind as ordinary handler parameters. Never declare a /.well-known/*, /ikon/*, or /api path — those are reserved.
    string Path { get; }
  enum EndpointAuth
    // Requires a valid signed grant in the URL (the default). Possession authorizes.
    Grant
    // Anonymous — no credential; identity comes from the URL, gated only by anti-abuse.
    Public
    // Always rejected. Declares an endpoint while keeping it closed.
    Deny
    // Unlike Grant, nothing here is minted by the app or pasted into a URL: the client discovers the space's authorization server, the human signs in with the space's own [Auth] Methods, and the client holds a short-lived token it refreshes itself. Anonymous sign-in methods (guest, global) cannot satisfy this — a global visitor is one shared space-wide user, so honouring it would hand every client the same identity and the same data. A space declaring only anonymous methods cannot host a User endpoint.
    User
  sealed record EndpointInfo
    ctor()
    // When non-empty, the gateway cell-routes the request to that cell's partitioned instance, keyed by the cell's IdentityFields in the URL; empty means the endpoint resolves to the app instance.
    string CellType { get; init; }
    // {Owner}_{Method}, derived unconditionally from the owner type and the handler method; the backend resolves this name when routing.
    string FunctionName { get; init; }
    // Carries no grant: a public endpoint is callable as-is, but a grant/policy endpoint needs a working, identity-bound URL minted via IApp.MintUrlAsync.
    string PublicUrl { get; init; }
  // Fired per chunk with the raw bytes for streaming (transcode/scan/forward); the platform already writes the chunk itself. Bytes are not yet verified — the SHA-256 check runs only after the last chunk and a mismatch discards the whole upload, so never act irreversibly. Data is valid only during the callback — copy it to retain it.
  sealed record FileUploadChunkArgs
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The total file size in bytes the client announced.
    // Data: This chunk's bytes. Only valid for the duration of the callback — copy them if you keep them.
    // BytesWritten: Total bytes received and written so far, including this chunk.
    ctor(string UploadId, string FileName, string MimeType, long Size, byte[] Data, long BytesWritten)
    long BytesWritten { get; init; }
    byte[] Data { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Fires only after the byte count and recomputed SHA-256 both match. Exactly one of LocalTempFilePath and AssetUri is non-null. The temp file is deleted when the app stops — move or copy it here to keep it.
  sealed record FileUploadCompleteArgs
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The file size in bytes.
    // LocalTempFilePath: Path to the received file in a temp directory, when the upload was not redirected to the asset system. Null when AssetUri is set. The temp directory is deleted when the app stops, so move or copy anything you want to keep.
    // AssetUri: The asset the upload was written into, when an earlier hook set FileUploadResult.AssetUri. Null when the file went to a local temp file instead. Exactly one of the two is non-null. It is the same AssetUri every Asset.Instance.* call takes, so it needs no parsing — null-check it and pass .Value straight on.
    ctor(string UploadId, string FileName, string MimeType, long Size, string? LocalTempFilePath, AssetUri? AssetUri)
    AssetUri? AssetUri { get; init; }
    string FileName { get; init; }
    string? LocalTempFilePath { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Terminal hook for an upload that had started (cancel, 60 s stall, out-of-sequence chunk, byte-count or SHA-256 mismatch, write failure). Uploads the app rejected from PreStart or Start never reach here. Any partial file/asset is already deleted — clean up only app-side state.
  sealed record FileUploadErrorArgs
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The file size in bytes the client announced.
    // ErrorMessage: Why the upload failed — the cancellation reason when the app cancelled it, otherwise the platform's description of the failure.
    ctor(string UploadId, string FileName, string MimeType, long Size, string ErrorMessage)
    string ErrorMessage { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // First hook, before any bytes transfer — the cheapest place to reject (return false or a FileUploadResult and nothing is sent). Hook order: PreStart → Start → Chunk/Progress (per chunk) → Complete on success or Error on failure. Capture Cancel to abort the upload later, e.g. from a UI cancel button.
  sealed record FileUploadPreStartArgs
    // UploadId: Id identifying this upload; the same value appears on every later hook's args.
    // FileName: The client-supplied file name. Untrusted — never join it into a path yourself.
    // MimeType: The client-supplied mime type. Untrusted — the bytes are not verified against it.
    // Size: The file size in bytes the client claims it will send. The upload fails with an error if the actual byte count differs.
    // Cancel: Aborts this upload: deletes whatever was written, fires the error hook with the reason, and tells the client to stop. Usable at any point during the upload, not just from this callback — capture it to cancel later (e.g. from a UI cancel button).
    ctor(string UploadId, string FileName, string MimeType, long Size, Func<string?, Task> Cancel)
    Func<string?, Task> Cancel { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Fired once per received chunk, after the chunk has been written and acknowledged. Meant for driving a progress bar; use onChunkReceived if you need the bytes themselves.
  sealed record FileUploadProgressArgs
    // FileName: The client-supplied file name.
    // MimeType: The client-supplied mime type.
    // Size: The total file size in bytes the client announced.
    // ProgressPercentage: Bytes received so far as a percentage of Size, 0 to 100. Zero for the whole upload when the client announced a size of 0.
    // BytesUploaded: Bytes received and written so far.
    ctor(string UploadId, string FileName, string MimeType, long Size, double ProgressPercentage, long BytesUploaded)
    long BytesUploaded { get; init; }
    string FileName { get; init; }
    string MimeType { get; init; }
    double ProgressPercentage { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  // Accepted defaults to true; return true; works via the implicit bool conversion. Set AssetUri to write the upload straight into the asset system instead of a local temp file.
  sealed record FileUploadResult
    ctor()
    bool Accepted { get; init; }
    AssetUri? AssetUri { get; init; }
    static implicit operator FileUploadResult(bool accepted)
  // Last chance to reject the upload, and the last hook where setting FileUploadResult.AssetUri can redirect the bytes into the asset system instead of a temp file. Only hook that carries Hash — do content-duplicate checks here.
  sealed record FileUploadStartArgs
    // UploadId: Id identifying this upload; the same value appears on every other hook's args.
    // FileName: The client-supplied file name. Untrusted — never join it into a path yourself.
    // MimeType: The client-supplied mime type. Untrusted — the bytes are not verified against it.
    // Size: The file size in bytes the client claims it will send.
    // Hash: The client-declared SHA-256 of the file contents, lowercase hex. The platform recomputes it while receiving and fails the upload with a hash mismatch if the received bytes disagree, so a match here is a genuine content identity — but it is the client's claim, not yet verification, at this point.
    ctor(string UploadId, string FileName, string MimeType, long Size, string Hash)
    string FileName { get; init; }
    string Hash { get; init; }
    string MimeType { get; init; }
    long Size { get; init; }
    string UploadId { get; init; }
  sealed class HttpDeleteAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed class HttpGetAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  // All verbs share the addressing + identity model on EndpointAttribute. Auth defaults to EndpointAuth.Grant — the gateway answers 401 on the bare URL unless the caller holds a minted grant URL; set Auth = EndpointAuth.Public for an anonymously reachable route (a public webhook, a health check).
  abstract class HttpMethodAttribute : EndpointAttribute
    abstract string Method { get; }
  sealed class HttpPatchAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed class HttpPostAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed class HttpPutAttribute : HttpMethodAttribute
    ctor(string path = "")
    override string Method { get; }
  sealed record HttpRequest
    ctor(string Method, string Path, IReadOnlyDictionary<string, string> Query, IReadOnlyDictionary<string, string> Headers, string Body)
    string Body { get; init; }
    IReadOnlyDictionary<string, string> Headers { get; init; }
    string Method { get; init; }
    string Path { get; init; }
    IReadOnlyDictionary<string, string> Query { get; init; }
  // An endpoint method may return any serializable value for an automatic 200 + JSON response, or return an HttpResult to control status code, content type, and body.
  sealed record HttpResult
    ctor(int StatusCode, object? Body = null, string ContentType = "application/json")
    object? Body { get; init; }
    string ContentType { get; init; }
    int StatusCode { get; init; }
    static HttpResult Accepted(object? body = null)
    static HttpResult BadRequest(string? reason = null)
    static HttpResult Conflict(string? reason = null)
    static HttpResult Created(object? body = null)
    static HttpResult Forbidden(string? reason = null)
    static HttpResult Json(object body, int statusCode = 200)
    static HttpResult NoContent()
    static HttpResult NotFound(string? reason = null)
    static HttpResult Ok(object? body = null)
    static HttpResult Text(string body, int statusCode = 200)
    static HttpResult Unauthorized(string? reason = null)
  interface IApp<out TSessionIdentity, out TClientParameters> : IAppBase
    // Resolves the current client from the ambient reactive scope — call it only inside UI.Root() or another ReactiveScope context; outside one there is no current client and it throws.
    virtual TClientParameters ClientParameters { get; }
    IClientCollection<TClientParameters> Clients { get; }
    TSessionIdentity SessionIdentity { get; }
