# Ikon.Common Public API

namespace Ikon.Common
  class AsyncLocalInstances
    bool AsyncLocalModeInitialized { get; }
    void Capture(object owner, bool allowOverride = false)
    void InitializeAll()
    void InitializeAll(IReadOnlyList<Type> explicitTypes)
    void Remove(object owner)
    void Restore(object owner)
    bool TryRemove(object owner)
    bool TryRestore(object owner)
    static readonly AsyncLocalInstances Instance
  // Read-only configuration handed to the app at startup and exposed through IAppBase.Databases: look a database up by Name or Type and open it (see IAppBase.Database or AppDatabaseConnection.Create). An app never constructs one — databases are created with ikon app db create (or the Portal) and provisioned by the backend.
  sealed record DatabaseConnectionInfo
    ctor()
    // Ready-to-use ADO.NET connection string, pointing at the app's own database through the connection pooler. It carries credentials — never log it or surface it to a client.
    string ConnectionString { get; init; }
    // The lookup key when an app has more than one database, as given to ikon app db create --name.
    string Name { get; init; }
    // "postgres" is the only engine the platform provisions today, and AppDatabaseConnection.Create throws NotSupportedException for anything else. Match on it rather than assuming.
    string Type { get; init; }
  // Derives from DescriptionAttribute so that every reader of the BCL attribute — Tool.Of lambda parameters, function registration — also picks this one up, and an app that has global using Ikon.Common; can write [Description] anywhere the BCL one is accepted. Adding using System.ComponentModel; next to it makes the bare name ambiguous (CS0104); qualify one of them.
  class DescriptionAttribute : DescriptionAttribute
    ctor(string description, object? example = null, RequiredStatus isRequired = Default, int minArrayItems = 0)
    object? Example { get; }
    // Not honoured by any schema generator: whether a property is required is derived from its nullability, and the OpenAI dialect lists every property as required regardless. Kept for source compatibility.
    RequiredStatus IsRequired { get; }
    int MinArrayItems { get; }
  enum EndpointProtocol
    Tcp
    Tls
    Udp
  sealed class IkonLoggerProvider : ILoggerProvider
    ctor()
    ILogger CreateLogger(string categoryName)
    void Dispose()
  static class IkonTaskExtensions
    // Intentionally does not await the task. Exceptions are observed and sent to onException.
    static void RunParallel(this Task task, Action<Exception>? onException = null)
  // Used wherever a caller supplies a destination the platform then reaches on their behalf — a TURN peer, a URL handed to an AI tool, a scraped page. Those all share one failure mode: the address is chosen by someone outside, but the connection is made from inside, so anything the host can see becomes reachable. That includes sibling containers, admin ports on the host, and on a cloud VM the metadata service on 169.254.169.254. Deliberately one implementation. Two copies of a rule like this drift, and the copy nobody remembers is the one still reachable.
  static class InternalAddressFilter
    // True when the address is a public, routable destination that is safe to reach on a caller's behalf. False for anything inside the host's own network, and for anything unrecognised — this fails closed.
    static bool IsPublicRoutable(IPAddress? address)
  static class MimeTypes
    // Registers a mime type for a file extension. The extension is normalized (leading dot stripped, lower-cased) so it matches what the lookups use, and the write is locked against the concurrent readers. Argument order is (extension, mimeType), matching the rest of the type.
    static void AddOrUpdate(string extension, string mimeType)
    // Returns the file extension registered for a mime type. When several extensions map to the same mime type, the first one in registration (insertion) order is returned. When no extension matches, the default extension (DefaultExtension, "bin") is returned.
    static string GetExtensionFromMimeType(string mimeType)
    static string GetMimeTypeFromExtension(string extension)
    static string GetMimeTypeFromFilename(string fileName)
    static bool Is(string mimeType, string mimeTypeToCompare)
    static bool IsAudio(string mimeType)
    // The negation of IsText: everything that is not text/* or ending in /json or /xml counts as binary — images, audio, video, and unknown or empty types included. Broader than application/octet-stream and does not imply that specific mime type.
    static bool IsBinary(string mimeType)
    static bool IsCsv(string mimeType)
    static bool IsImage(string mimeType)
    static bool IsJson(string mimeType)
    static bool IsMarkdown(string mimeType)
    static bool IsMicrosoftExcel(string mimeType)
    static bool IsMicrosoftPowerpoint(string mimeType)
    static bool IsMicrosoftWord(string mimeType)
    static bool IsNotes(string mimeType)
    static bool IsPdf(string mimeType)
    // Returns true when the mime type is textual: any text/* type, or one ending in /json or /xml. Everything else (images, audio, video, unknown types) is not text.
    static bool IsText(string mimeType)
    static bool IsVideo(string mimeType)
    static bool IsXml(string mimeType)
    static bool IsZip(string mimeType)
    // type: The category keyword — not a mime string. Recognized keywords are: text, markdown, video, image, audio, json, binary, csv, zip, xml, pdf, word, excel, powerpoint, notes, and any. "any" always returns true; an unrecognized keyword returns false.
    // mimeType: The mime type to classify.
    static bool TypeMatchesMimetype(string type, string mimeType)
    const string ApplicationExcel
    const string ApplicationJavascript
    const string ApplicationJson
    const string ApplicationMsword
    const string ApplicationOctetStream
    const string ApplicationPdf
    const string ApplicationSql
    const string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlPresentation
    const string ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet
    const string ApplicationVndOpenxmlformatsOfficedocumentWordprocessingmlDocument
    const string ApplicationXml
    const string ApplicationZip
    const string AudioMpeg
    const string AudioXWav
    const string Binary
    const string DefaultExtension
    const string DefaultMimeType
    const string ImageAvif
    const string ImageBmp
    const string ImageGif
    const string ImageHeif
    const string ImageJpeg
    const string ImagePng
    const string ImageSvg
    const string ImageSvgXml
    const string ImageTiff
    const string ImageWebp
    const string TextCss
    const string TextCsv
    const string TextHtml
    const string TextJavascript
    const string TextMarkdown
    const string TextPlain
    const string TextXml
    const string VideoMp4
  static class NetworkUtils
    // A candidate address for another host on the LAN to reach this machine at. Only operational (OperationalStatus.Up), non-loopback, non-tunnel interfaces are considered, and loopback (127.x) and APIPA link-local (169.254.x) addresses are skipped. Returns IPAddress.Loopback only when no such address exists.
    static IPAddress GetFirstIPv4AddressOrLocalhost()
  // Thrown when a declared package hook command exits non-zero.
  sealed class PackageHookException : Exception
    ctor(string command, string output)
    string Command { get; }
  // Runs an app's declared packaging pipeline commands during bundling by shelling out to whatever the app declared, exposing the bundle staging directory via IKON_BUNDLE_DIR so a script can write processed/compiled output there to include it in the package.
  static class PackageHooks
    // Runs each command in order in appDir, with IKON_APP_DIR and IKON_BUNDLE_DIR (plus any extraEnv) in the environment. Throws PackageHookException on the first command that fails. onCommandStart is invoked before each command (for progress reporting).
    static Task RunAsync(IReadOnlyList<string> commands, string appDir, string bundleDir, IReadOnlyDictionary<string, string?>? extraEnv = null, Action<string>? onCommandStart = null, CancellationToken ct = default)
  enum PipelineExecutionMode
    None
    HttpsEndpoint
    Scheduled
  class RateLimiter
    ctor(TimeSpan window, int rateLimit)
    // A side-effect-free read: it counts the calls still inside the window without mutating any state, so inspecting it (in a debugger or a metric) never prunes entries or changes what Guard will do. Expired entries are dropped on the Guard path, so the value still decays as the window drains.
    int Rate { get; }
    // Returns true when the call is admitted. A rejected call does not consume a window slot, so a caller retrying after a rejection recovers as soon as the window drains instead of pushing the limiter further over its limit with every attempt.
    bool Guard()
  // Exposes the locally bound port and the publicly reachable host/port. Dispose to release the endpoint and its local port reservation.
  sealed class RelayEndpoint : IAsyncDisposable
    int LocalPort { get; }
    EndpointProtocol Protocol { get; }
    string PublicHost { get; }
    int PublicPort { get; }
    ValueTask DisposeAsync()
  enum RequiredStatus
    Default
    Required
    Optional
  class Resources : AsyncLocalInstance<Resources>
    ctor()
    Task<byte[]> ReadAsBytesAsync(string resourcePath)
    Task<Stream> ReadAsStreamAsync(string resourcePath)
    Task<string> ReadAsStringAsync(string resourcePath)
  // Across every overload, retries counts attempts beyond the first: the delegate runs once, then up to retries more times, for at most retries + 1 total invocations (e.g. retries = 5 allows up to 6 calls). When no retryableExceptions filter is supplied, only transient exceptions are retried: IOException, HttpRequestException and TimeoutException. Non-transient exceptions (bugs, validation failures) surface immediately instead of being retried. Pass an explicit filter to override this default — e.g. [typeof(Exception)] to retry every exception.
  static class Retrier
    static T Run<T>(Func<T> func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static void Run(Action func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task<T> RunAsync<T>(Func<Task<T>> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task RunAsync(Func<Task> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
  static class StringDistance
    // Returns the minimum number of single-character insertions, deletions, or substitutions to turn a into b. Empty / null inputs return the length of the other side. O(|a|·|b|) time and memory.
    static int Levenshtein(string? a, string? b)
  static class StringUtils
    // The returned string is hex-encoded, so it is 2×size characters long (the default of 32 bytes yields a 64-character string).
    // size: The number of random bytes to generate.
    static string GenerateRandomToken(int size = 32)
    static string GetCSharpTypeName(object? obj)
    static string ToUnescapedString(string input, bool unicodeOnly = false)

namespace Ikon.Common.Assets
  sealed record AssetGcOrphan
    ctor(string Uri)
    string Uri { get; init; }
  sealed record AssetGcPlan
    ctor(AssetGcScope Scope, IReadOnlyList<AssetGcOrphan> Orphans, int EverReferenced, int Kept)
    int EverReferenced { get; init; }
    int Kept { get; init; }
    IReadOnlyList<AssetGcOrphan> Orphans { get; init; }
    AssetGcScope Scope { get; init; }
  enum AssetGcScope
    // Keep every asset any reachable commit references — deletes nothing, so binary undo/redo across the whole history stays intact. The safe default.
    History
    // Keep the working tree plus assets referenced by commits within a recent day window; older historical versions are reclaimed (checking out a commit past the window may lack those binaries).
    Window
    // Keep only what the current working tree references; every historical version is reclaimed (most aggressive — older checkouts lose their binaries).
    Current
  // The binary-in-git scheme: a binary foobar.jpg is tracked in git as a text pointer foobar.jpg.ikonasset while the bytes live in an IAssetBackend; the real file is materialized on demand and git-ignored. NormalizeAsync enforces the convention (and self-heals a raw binary anyone committed); MaterializeAsync restores the real files; CollectReferencedUrisAsync feeds reachability GC.
  sealed class AssetLinkManager
    ctor(IAssetBackend backend)
    // Every asset URI reachable from the tree's pointers — the "live set" a reachability GC keeps; the backend expires blobs whose URI is not in this set.
    Task<IReadOnlySet<string>> CollectReferencedUrisAsync(string repoDir, CancellationToken ct = default)
    // Deletes the orphaned assets a PlanGcAsync plan found. Best-effort per asset (an already-deleted URI on a re-run counts as a failure, not a crash); returns deleted/failed counts.
    Task<(int Deleted, int Failed)> ExecuteGcAsync(AssetGcPlan plan, CancellationToken ct = default)
    // Produces the real binary next to every *.ikonasset pointer by downloading its bytes, and git-ignores it. Idempotent and hash-checked — a materialized file whose content already matches the pointer's hash is left untouched. Returns the real paths written.
    Task<IReadOnlyList<string>> MaterializeAsync(string repoDir, CancellationToken ct = default)
    // Converts every raw binary in the tree to the pointer scheme: upload the bytes, write the *.ikonasset pointer, git-ignore the real path, and untrack the real file if git was carrying it. The real file itself stays on disk — normalize only stops git from carrying the bytes, it never takes files away from a working tree. Idempotent — a file that is already a pointer, or already pointered with an unchanged hash, is skipped. Returns the real paths converted.
    Task<IReadOnlyList<string>> NormalizeAsync(string repoDir, CancellationToken ct = default)
    // Plans a reachability GC without touching the store: collect every asset URI any *.ikonasset pointer ever recorded across git history, subtract the set still referenced under scope, and return the difference as orphans, each deletable by its URI. AssetGcScope.History keeps everything (empty plan); Window/Current reclaim older versions.
    Task<AssetGcPlan> PlanGcAsync(string repoDir, AssetGcScope scope, int windowDays = 30, CancellationToken ct = default)
    // Moves a tree's offloaded binaries from one store to another: materialize every pointer from source, drop the pointers, and normalize under target so they reference blobs the new home owns.
    static Task RehomeAsync(IAssetBackend source, IAssetBackend target, string repoDir, CancellationToken ct = default)
    // Recomputes the managed .gitignore block from the pointers currently in the tree — for callers that move pointer files around (folder migration) without touching the store.
    static Task UpdateManagedGitignoreAsync(string repoDir, CancellationToken ct = default)
    // Files under root public/ are uploaded as public (frontend-loadable by stable URL); binaries anywhere else stay private, readable only by the app. The folder is the classification — there is nothing to configure.
    const string PublicFolderName
  // Thrown when AssetLinkManager.MaterializeAsync could not restore some assets (their blobs are missing — e.g. an old checkout after an aggressive GC). Everything that could be restored has been; Failures lists what could not, one path: reason per line.
  sealed class AssetMaterializeException : Exception
    ctor(IReadOnlyList<string> failures)
    IReadOnlyList<string> Failures { get; }
  // The content of a *.ikonasset pointer file — the small, versioned text git tracks in place of a binary. Checking out any commit restores that commit's pointers, so binary history/undo/redo works.
  sealed record AssetPointer
    ctor(string Uri, string Sha256, long Size, string Name, string? PublicUrl = null)
    string Name { get; init; }
    string? PublicUrl { get; init; }
    string Sha256 { get; init; }
    long Size { get; init; }
    string Uri { get; init; }
    static string PointerPathForReal(string realPath)
    static string RealPathForPointer(string pointerPath)
    string Serialize()
    static AssetPointer? TryParse(string text)
    // Marks a pointer file, appended after the real extension: foobar.jpg → foobar.jpg.ikonasset.
    const string Suffix
  static class BinaryContent
    // Content is binary when it is not valid UTF-8 or contains a NUL byte in its head — the same heuristic git itself uses to decide "binary". Empty content is text.
    static bool IsBinary(byte[] content)
    static string Sha256Hex(byte[] content)
    // The window IsBinary inspects — matching git's own binary heuristic, and the most a caller needs to read from a file to classify it.
    const int DetectionWindowBytes = 8000
  // A blob store binaries are offloaded to: git tracks a small text AssetPointer while the bytes live here, addressed by backend-agnostic string URIs.
  interface IAssetBackend
    // Best-effort; used by GC.
    Task DeleteAsync(string uri, CancellationToken ct = default)
    Task<byte[]> DownloadAsync(string uri, CancellationToken ct = default)
    // A stable public URL for a URI when the blob is publicly served; null for private/backend-only assets.
    Task<string?> GetPublicUrlAsync(string uri, CancellationToken ct = default)
    // Stores bytes and returns the URI the pointer records. isPublic selects a publicly-served class (a frontend can load it by URL) over a private, backend-only one.
    Task<string> UploadAsync(byte[] content, string fileName, bool isPublic, CancellationToken ct = default)
  // IAssetBackend over the Ikon Asset system. Public assets go to AssetClass.CloudFilePublic (a frontend can load them by URL); private assets to AssetClass.CloudFile (C#-readable only). Blobs are content-addressed by SHA-256, so identical bytes upload once, a pointer's URI is immutable (checking out an old commit fetches exactly that version), and reachability GC can safely delete any stored hash no live pointer references.
  sealed class IkonAssetBackend : IAssetBackend
    ctor(string spaceId)
    Task DeleteAsync(string uri, CancellationToken ct = default)
    Task<byte[]> DownloadAsync(string uri, CancellationToken ct = default)
    Task<string?> GetPublicUrlAsync(string uri, CancellationToken ct = default)
    Task<string> UploadAsync(byte[] content, string fileName, bool isPublic, CancellationToken ct = default)
  static class StorageExtensions
    static Task AddCloudFilePublicStorageAsync(this Asset asset)
    static Task AddCloudFileStorageAsync(this Asset asset, TimeSpan? uploadTimeout = null)
    static Task AddCloudJsonStorageAsync(this Asset asset)
    static Task AddLocalFileStorageAsync(this Asset asset, string root)

namespace Ikon.Common.Git
  record GitBranch
    ctor(string Name, bool IsRemote, bool IsCurrent)
    bool IsCurrent { get; init; }
    bool IsRemote { get; init; }
    string Name { get; init; }
  enum GitChangeType
    Added
    Modified
    Deleted
    Renamed
    Untracked
  record GitCloneOptions
    // Overwrite: When the clone target already exists and is non-empty, replace it instead of failing. Off by default so a populated directory is never destroyed silently.
    ctor(string? Branch = null, bool Shallow = false, GitCredentials? Credentials = null, bool Overwrite = false)
    string? Branch { get; init; }
    GitCredentials? Credentials { get; init; }
    bool Overwrite { get; init; }
    bool Shallow { get; init; }
  record GitCommit
    ctor(string Sha, string ShortSha, string Author, string AuthorEmail, DateTimeOffset Date, string Message)
    string Author { get; init; }
    string AuthorEmail { get; init; }
    DateTimeOffset Date { get; init; }
    string Message { get; init; }
    string Sha { get; init; }
    string ShortSha { get; init; }
  record GitCredentials
    ctor(string Username, string Password)
    string Password { get; init; }
    string Username { get; init; }
  record GitDiff
    ctor(string? FromSha, string? ToSha, IReadOnlyList<GitFileDiff> Files)
    IReadOnlyList<GitFileDiff> Files { get; init; }
    string? FromSha { get; init; }
    string? ToSha { get; init; }
  record GitFileChange
    ctor(string Path, GitChangeType Type)
    string Path { get; init; }
    GitChangeType Type { get; init; }
  record GitFileDiff
    ctor(string Path, GitChangeType Type, int LinesAdded, int LinesRemoved, string? Patch = null)
    int LinesAdded { get; init; }
    int LinesRemoved { get; init; }
    string? Patch { get; init; }
    string Path { get; init; }
    GitChangeType Type { get; init; }
  enum GitReconcileOutcome
    UpToDate
    Pushed
    Merged
    Conflicted
    NoRemote
    Detached
    Failed
  record GitReconcileResult
    ctor(GitReconcileOutcome Outcome, string Branch, IReadOnlyList<string> ConflictedFiles, string? Error = null)
    string Branch { get; init; }
    IReadOnlyList<string> ConflictedFiles { get; init; }
    string? Error { get; init; }
    GitReconcileOutcome Outcome { get; init; }
  class GitRepository
    ctor(string workingDirectory, GitCredentials? credentials = null)
    GitCredentials? Credentials { get; }
    string WorkingDirectory { get; }
    Task AbortAllInProgressOperationsAsync(CancellationToken ct = default)
    Task<bool> AbortCherryPickAsync(CancellationToken ct = default)
    Task<bool> AbortMergeAsync(CancellationToken ct = default)
    Task<bool> AbortRebaseAsync(CancellationToken ct = default)
    // Credentials are stripped from the URL before it is stored.
    Task AddRemoteAsync(string name, string url, CancellationToken ct = default)
    Task CheckoutAsync(string branchOrRef, CancellationToken ct = default)
    // HEAD is not changed.
    Task CheckoutFilesFromRefAsync(string refName, string path = ".", CancellationToken ct = default)
    // If targetDir already exists and is non-empty, the clone fails with an exception unless GitCloneOptions.Overwrite is set, in which case the existing directory is deleted recursively first — an unintended overwrite never happens silently.
    static Task<GitRepository> CloneAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    // Clones a repository, or syncs it to the remote if it already exists. The sync path is destructive: it runs reset --hard then clean -fd, so any uncommitted changes and untracked files in an existing checkout are discarded. It syncs the branch named by GitCloneOptions.Branch, or the checkout's current branch when that is null.
    static Task<(GitRepository Repo, string? Sha, bool WasCloned)> CloneOrSyncAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    Task<GitCommit> CommitAsync(string message, CancellationToken ct = default)
    Task<GitCommit> CommitAsync(string message, string authorName, string authorEmail, bool allowEmpty = false, CancellationToken ct = default)
    // Builds per-invocation environment variables that authenticate git HTTP(S) operations. Uses git's environment config mechanism (git 2.31+) to inject an Authorization header, appending to any GIT_CONFIG_COUNT entries already present in the process environment.
    static Dictionary<string, string?> CreateAuthEnvironment(GitCredentials credentials)
    Task CreateBranchAsync(string name, string? startPoint = null, CancellationToken ct = default)
    Task<GitTag> CreateTagAsync(string name, string? message = null, CancellationToken ct = default)
    Task DeleteTagAsync(string name, CancellationToken ct = default)
    Task DiscardChangesAsync(CancellationToken ct = default)
    Task EnsureCleanRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    static string EscapeMessage(string message)
    Task FetchAsync(bool includeTags = false, CancellationToken ct = default)
    // Counts how many commits the local branch is ahead of and behind its origin counterpart. Returns null when the counts cannot be determined (e.g. origin/{branch} does not exist).
    Task<(int Ahead, int Behind)?> GetAheadBehindAsync(string branch, CancellationToken ct = default)
    Task<IReadOnlyList<GitBranch>> GetBranchesAsync(CancellationToken ct = default)
    Task<string?> GetConfigAsync(string key, CancellationToken ct = default)
    Task<string> GetCurrentBranchAsync(CancellationToken ct = default)
    // Diff between HEAD and target; a null target diffs the working directory.
    Task<GitDiff> GetDiffAsync(string? target = null, CancellationToken ct = default)
    Task<GitCommit?> GetHeadCommitAsync(CancellationToken ct = default)
    Task<string?> GetHeadShaAsync(bool shortSha = false, CancellationToken ct = default)
    Task<IReadOnlyList<GitCommit>> GetHistoryAsync(int limit = 20, string? fromRef = null, CancellationToken ct = default)
    // The remote URL exactly as stored in .git/config, including any embedded credentials.
    Task<string?> GetRawRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    // Credentials are stripped from the returned URL; see GetRawRemoteUrlAsync for the stored form.
    Task<string?> GetRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    Task<GitStatus> GetStatusAsync(CancellationToken ct = default)
    Task<IReadOnlyList<GitTag>> GetTagsAsync(CancellationToken ct = default)
    Task<bool> HasCommitsAsync(CancellationToken ct = default)
    Task<bool> HasRemoteAsync(string name = "origin", CancellationToken ct = default)
    Task<bool> HasStagedChangesAsync(CancellationToken ct = default)
    Task<bool> HasUncommittedChangesAsync(CancellationToken ct = default)
    Task<bool> HasUncommittedChangesAsync(string path, CancellationToken ct = default)
    // A branch that does not exist on origin counts as unpushed when local commits exist.
    Task<bool> HasUnpushedCommitsAsync(string branch, CancellationToken ct = default)
    // Initializes a git repository and connects it to a remote. Local files are kept as-is and NOT merged with remote content.
    static Task<GitRepository> InitAndConnectAsync(string directory, string remoteUrl, GitCredentials? credentials = null, string? configKey = null, string? configValue = null, CancellationToken ct = default)
    static Task<GitRepository> InitAsync(string directory, CancellationToken ct = default)
    Task<bool> IsGitRepositoryAsync(CancellationToken ct = default)
    static Task<bool> IsGitRepositoryAsync(string directory, CancellationToken ct = default)
    // Lists all worktrees attached to this repository, including the primary one.
    Task<IReadOnlyList<GitWorktreeInfo>> ListWorktreesAsync(CancellationToken ct = default)
    Task PushAsync(bool setUpstream = false, CancellationToken ct = default)
    // Reconciles the current branch with its origin counterpart and pushes, resolving divergence by auto-merge. Behaviour by state (after fetching origin): no origin remote → GitReconcileOutcome.NoRemote; detached / no branch → GitReconcileOutcome.Detached; branch not on the remote yet, or only local ahead → push (GitReconcileOutcome.Pushed); identical → GitReconcileOutcome.UpToDate; only remote ahead → fast-forward local (GitReconcileOutcome.Merged); diverged and git 3-way merges cleanly → push the merge (GitReconcileOutcome.Merged); diverged with a real content conflict → abort the merge and return GitReconcileOutcome.Conflicted with the conflicted files for the caller to resolve. Never destroys local commits: a conflict aborts back to the pre-merge state.
    Task<GitReconcileResult> ReconcileAndPushAsync(string commitAuthorName = "Ikon", string commitAuthorEmail = "ikon@ikon.local", CancellationToken ct = default)
    Task<bool> RefExistsAsync(string refName, CancellationToken ct = default)
    Task RenameBranchAsync(string oldName, string newName, CancellationToken ct = default)
    Task ResetHardAsync(string target, CancellationToken ct = default)
    Task ResetSoftAsync(string target, CancellationToken ct = default)
    // Restores the working tree to a target (tag, sha, or branch) with a hard reset after a fetch. Uncommitted changes and staged files are discarded, exactly as by SyncAsync.
    Task<GitSyncResult> RestoreAsync(string target, CancellationToken ct = default)
    Task<string> RunAsync(string args, CancellationToken ct = default)
    // Stages all changes, commits, and pushes.
    Task<GitSyncResult> SaveAsync(string message, CancellationToken ct = default)
    Task SetConfigAsync(string key, string value, CancellationToken ct = default)
    // Credentials are stripped from the URL before it is stored.
    Task SetRemoteUrlAsync(string name, string url, CancellationToken ct = default)
    Task SetUpstreamAsync(string remoteBranch, CancellationToken ct = default)
    static string ShortCommitHash(string? hash)
    Task StageAllAsync(CancellationToken ct = default)
    Task StagePathAsync(string path, CancellationToken ct = default)
    // Stashes all changes, untracked files included, without touching the index so a pop restores each file to its previous state.
    Task<bool> StashAsync(string? message = null, CancellationToken ct = default)
    Task<bool> StashPopAsync(CancellationToken ct = default)
    static string StripCredentialsFromUrl(string url)
    // Syncs to the latest remote with fetch + reset --hard: uncommitted changes and staged files are discarded.
    Task<GitSyncResult> SyncAsync(CancellationToken ct = default)
    static GitRepository? TryOpen(string directory)
    Task<(bool Success, string StdOut, string StdErr)> TryRunAsync(string args, CancellationToken ct = default)
    // Credentials and trailing slashes are ignored in the comparison.
    static bool UrlsMatch(string? url1, string? url2)
  record GitStatus
    ctor(string Branch, string? HeadSha, bool HasUncommittedChanges, bool IsDetachedHead, int AheadBy, int BehindBy, IReadOnlyList<GitFileChange> Changes)
    int AheadBy { get; init; }
    int BehindBy { get; init; }
    string Branch { get; init; }
    IReadOnlyList<GitFileChange> Changes { get; init; }
    bool HasUncommittedChanges { get; init; }
    string? HeadSha { get; init; }
    bool IsDetachedHead { get; init; }
  record GitSyncResult
    ctor(bool Success, string? PreviousSha, string? CurrentSha, string? Error = null)
    string? CurrentSha { get; init; }
    string? Error { get; init; }
    string? PreviousSha { get; init; }
    bool Success { get; init; }
  record GitTag
    ctor(string Name, string Sha, GitCommit? Commit = null)
    GitCommit? Commit { get; init; }
    string Name { get; init; }
    string Sha { get; init; }
  record GitWorktreeInfo
    ctor(string Path, string? Head, string? Branch)
    string? Branch { get; init; }
    string? Head { get; init; }
    string Path { get; init; }

namespace Ikon.Common.Reflection
  static class TaskTypeUnwrap
    // Takes whatever MethodInfo.Invoke handed back and produces its observable result. Awaits Task, Task<TResult>, ValueTask, ValueTask<TResult>; returns null for void-shaped awaitables; passes non-task values straight through.
    static ValueTask<object?> AwaitAndGetResultAsync(object? raw)
    // Maps a method's declared return type to the type the method actually produces: Task/ValueTask → void (there is no result), Task<T>/ValueTask<T> → T, anything else → as-is.
    static Type UnwrapResultType(Type declaredReturnType)
