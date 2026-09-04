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
  enum PipelineExecutionMode
    None
    HttpsEndpoint
    Scheduled
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
  // Across every overload, retries counts attempts beyond the first: the delegate runs once, then up to retries more times, for at most retries + 1 total invocations (e.g. retries = 5 allows up to 6 calls). When no retryableExceptions filter is supplied, only transient exceptions are retried: IOException, HttpRequestException and TimeoutException. Non-transient exceptions (bugs, validation failures) surface immediately instead of being retried. Pass an explicit filter to override this default — e.g. [typeof(Exception)] to retry every exception. maxDelay caps the backoff per exception, for a failure whose next attempt is an independent draw rather than a wait for something to recover; return TimeSpan.MaxValue to leave the ladder uncapped. It never changes how many attempts are made. maxRetries lowers retries per exception, for a failure where more attempts buy nothing however long the wait; Int32.MaxValue leaves it at retries. It never raises the budget.
  static class Retrier
    static T Run<T>(Func<T> func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
    static void Run(Action func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
    static Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
    static Task<T> RunAsync<T>(Func<Task<T>> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
    static Task RunAsync(Func<Task> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, Func<Exception, TimeSpan>? maxDelay = null, Func<Exception, int>? maxRetries = null, string? description = null)
  static class StringDistance
    // Returns the minimum number of single-character insertions, deletions, or substitutions to turn a into b. Empty / null inputs return the length of the other side. O(|a|·|b|) time and memory.
    static int Levenshtein(string? a, string? b)

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
    static bool IsPublicPath(string repoRelativePath)
    Task<IReadOnlyList<string>> ListForeignPointerFilesAsync(string repoDir, CancellationToken ct = default)
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
    static readonly string[] SkipDirs
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
  // A blob store binaries are offloaded to: git tracks a small text AssetPointer while the bytes live here, addressed by backend-agnostic string URIs.
  interface IAssetBackend
    // Best-effort; used by GC.
    Task DeleteAsync(string uri, CancellationToken ct = default)
    Task<byte[]> DownloadAsync(string uri, CancellationToken ct = default)
    // A stable public URL for a URI when the blob is publicly served; null for private/backend-only assets.
    Task<string?> GetPublicUrlAsync(string uri, CancellationToken ct = default)
    virtual bool Owns(string uri)
    // Stores bytes and returns the URI the pointer records. isPublic selects a publicly-served class (a frontend can load it by URL) over a private, backend-only one.
    Task<string> UploadAsync(byte[] content, string fileName, bool isPublic, CancellationToken ct = default)
  // IAssetBackend over the Ikon Asset system. Public assets go to AssetClass.CloudFilePublic (a frontend can load them by URL); private assets to AssetClass.CloudFile (C#-readable only). Blobs are content-addressed by SHA-256, so identical bytes upload once, a pointer's URI is immutable (checking out an old commit fetches exactly that version), and reachability GC can safely delete any stored hash no live pointer references.
  sealed class IkonAssetBackend : IAssetBackend
    ctor(string spaceId)
    Task DeleteAsync(string uri, CancellationToken ct = default)
    Task<byte[]> DownloadAsync(string uri, CancellationToken ct = default)
    Task<string?> GetPublicUrlAsync(string uri, CancellationToken ct = default)
    bool Owns(string uri)
    Task<string> UploadAsync(byte[] content, string fileName, bool isPublic, CancellationToken ct = default)
  static class StorageExtensions
    static Task AddCloudFilePublicStorageAsync(this Asset asset)
    static Task AddCloudFileStorageAsync(this Asset asset, TimeSpan? uploadTimeout = null)
    static Task AddCloudJsonStorageAsync(this Asset asset)
    static Task AddLocalFileStorageAsync(this Asset asset, string root)
