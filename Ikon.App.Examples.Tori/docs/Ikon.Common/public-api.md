# Ikon.Common Public API

namespace Ikon.Common
  class AsyncLocalInstances
    void Capture(object owner, bool allowOverride = false)
    void InitializeAll()
    void InitializeAll(IReadOnlyList<Type> explicitTypes)
    void Remove(object owner)
    void Restore(object owner)
    bool TryRemove(object owner)
    bool TryRestore(object owner)
    static readonly AsyncLocalInstances Instance
  sealed record DatabaseConnectionInfo
    ctor()
    string ConnectionString { get; init; }
    string Name { get; init; }
    string Type { get; init; }
  class DescriptionAttribute : Attribute
    ctor(string description, object? example = null, RequiredStatus isRequired = Default, int minArrayItems = 0)
    string Description { get; }
    object? Example { get; }
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
    static void RunParallel(this Task task, Action<Exception>? onException = null)
  // Used wherever a caller supplies a destination the platform then reaches on their behalf — a TURN peer, a URL handed to an AI tool, a scraped page. Those all share one failure mode: the address is chosen by someone outside, but the connection is made from inside, so anything the host can see becomes reachable. That includes sibling containers, admin ports on the host, and on a cloud VM the metadata service on 169.254.169.254. Deliberately one implementation. Two copies of a rule like this drift, and the copy nobody remembers is the one still reachable.
  static class InternalAddressFilter
    static bool IsPublicRoutable(IPAddress? address)
  static class MimeTypes
    static void AddOrUpdate(string mime, string extension)
    static string GetExtensionFromMimeType(string mimeType)
    static string GetMimeTypeFromExtension(string extension)
    static string GetMimeTypeFromFilename(string fileName)
    static bool Is(string mimeType, string mimeTypeToCompare)
    static bool IsAudio(string mimeType)
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
    static bool IsText(string mimeType)
    static bool IsVideo(string mimeType)
    static bool IsXml(string mimeType)
    static bool IsZip(string mimeType)
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
    static IPAddress GetFirstIPv4AddressOrLocalhost()
  sealed class PackageHookException : Exception
    ctor(string command, string output)
    string Command { get; }
  static class PackageHooks
    static Task RunAsync(IReadOnlyList<string> commands, string appDir, string bundleDir, IReadOnlyDictionary<string, string?>? extraEnv = null, Action<string>? onCommandStart = null, CancellationToken ct = default)
  enum PipelineExecutionMode
    None
    HttpsEndpoint
    Scheduled
  class RateLimiter
    ctor(TimeSpan window, int rateLimit)
    int Rate { get; }
    bool Guard()
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
  static class Retrier
    static T Run<T>(Func<T> func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static void Run(Action func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task<T> RunAsync<T>(Func<Task<T>> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task RunAsync(Func<Task> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
  static class StringDistance
    static int Levenshtein(string? a, string? b)
  static class StringUtils
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
    History
    Window
    Current
  sealed class AssetLinkManager
    ctor(IAssetBackend backend)
    Task<IReadOnlySet<string>> CollectReferencedUrisAsync(string repoDir, CancellationToken ct = default)
    Task<(int Deleted, int Failed)> ExecuteGcAsync(AssetGcPlan plan, CancellationToken ct = default)
    Task<IReadOnlyList<string>> MaterializeAsync(string repoDir, CancellationToken ct = default)
    Task<IReadOnlyList<string>> NormalizeAsync(string repoDir, CancellationToken ct = default)
    Task<AssetGcPlan> PlanGcAsync(string repoDir, AssetGcScope scope, int windowDays = 30, CancellationToken ct = default)
    static Task RehomeAsync(IAssetBackend source, IAssetBackend target, string repoDir, CancellationToken ct = default)
    static Task UpdateManagedGitignoreAsync(string repoDir, CancellationToken ct = default)
    const string PublicFolderName
  sealed class AssetMaterializeException : Exception
    ctor(IReadOnlyList<string> failures)
    IReadOnlyList<string> Failures { get; }
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
    const string Suffix
  static class BinaryContent
    static bool IsBinary(byte[] content)
    static string Sha256Hex(byte[] content)
    const int DetectionWindowBytes = 8000
  interface IAssetBackend
    Task DeleteAsync(string uri, CancellationToken ct = default)
    Task<byte[]> DownloadAsync(string uri, CancellationToken ct = default)
    Task<string?> GetPublicUrlAsync(string uri, CancellationToken ct = default)
    Task<string> UploadAsync(byte[] content, string fileName, bool isPublic, CancellationToken ct = default)
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
    ctor(string? Branch = null, bool Shallow = false, GitCredentials? Credentials = null)
    string? Branch { get; init; }
    GitCredentials? Credentials { get; init; }
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
    Task AddRemoteAsync(string name, string url, CancellationToken ct = default)
    Task CheckoutAsync(string branchOrRef, CancellationToken ct = default)
    Task CheckoutFilesFromRefAsync(string refName, string path = ".", CancellationToken ct = default)
    static Task<GitRepository> CloneAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    static Task<(GitRepository Repo, string? Sha, bool WasCloned)> CloneOrSyncAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    Task<GitCommit> CommitAsync(string message, CancellationToken ct = default)
    Task<GitCommit> CommitAsync(string message, string authorName, string authorEmail, bool allowEmpty = false, CancellationToken ct = default)
    static Dictionary<string, string?> CreateAuthEnvironment(GitCredentials credentials)
    Task CreateBranchAsync(string name, string? startPoint = null, CancellationToken ct = default)
    Task<GitTag> CreateTagAsync(string name, string? message = null, CancellationToken ct = default)
    Task DeleteTagAsync(string name, CancellationToken ct = default)
    Task DiscardChangesAsync(CancellationToken ct = default)
    Task EnsureCleanRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    static string EscapeMessage(string message)
    Task FetchAsync(bool includeTags = false, CancellationToken ct = default)
    Task<(int Ahead, int Behind)?> GetAheadBehindAsync(string branch, CancellationToken ct = default)
    Task<IReadOnlyList<GitBranch>> GetBranchesAsync(CancellationToken ct = default)
    Task<string?> GetConfigAsync(string key, CancellationToken ct = default)
    Task<string> GetCurrentBranchAsync(CancellationToken ct = default)
    Task<GitDiff> GetDiffAsync(string? target = null, CancellationToken ct = default)
    Task<GitCommit?> GetHeadCommitAsync(CancellationToken ct = default)
    Task<string?> GetHeadShaAsync(bool shortSha = false, CancellationToken ct = default)
    Task<IReadOnlyList<GitCommit>> GetHistoryAsync(int limit = 20, string? fromRef = null, CancellationToken ct = default)
    Task<string?> GetRawRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    Task<string?> GetRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    Task<GitStatus> GetStatusAsync(CancellationToken ct = default)
    Task<IReadOnlyList<GitTag>> GetTagsAsync(CancellationToken ct = default)
    Task<bool> HasCommitsAsync(CancellationToken ct = default)
    Task<bool> HasRemoteAsync(string name = "origin", CancellationToken ct = default)
    Task<bool> HasStagedChangesAsync(CancellationToken ct = default)
    Task<bool> HasUncommittedChangesAsync(CancellationToken ct = default)
    Task<bool> HasUncommittedChangesAsync(string path, CancellationToken ct = default)
    Task<bool> HasUnpushedCommitsAsync(string branch, CancellationToken ct = default)
    static Task<GitRepository> InitAndConnectAsync(string directory, string remoteUrl, GitCredentials? credentials = null, string? configKey = null, string? configValue = null, CancellationToken ct = default)
    static Task<GitRepository> InitAsync(string directory, CancellationToken ct = default)
    Task<bool> IsGitRepositoryAsync(CancellationToken ct = default)
    static Task<bool> IsGitRepositoryAsync(string directory, CancellationToken ct = default)
    Task<IReadOnlyList<GitWorktreeInfo>> ListWorktreesAsync(CancellationToken ct = default)
    Task PushAsync(bool setUpstream = false, CancellationToken ct = default)
    Task<GitReconcileResult> ReconcileAndPushAsync(string commitAuthorName = "Ikon", string commitAuthorEmail = "ikon@ikon.local", CancellationToken ct = default)
    Task<bool> RefExistsAsync(string refName, CancellationToken ct = default)
    Task RenameBranchAsync(string oldName, string newName, CancellationToken ct = default)
    Task ResetHardAsync(string target, CancellationToken ct = default)
    Task ResetSoftAsync(string target, CancellationToken ct = default)
    Task<GitSyncResult> RestoreAsync(string target, CancellationToken ct = default)
    Task<string> RunAsync(string args, CancellationToken ct = default)
    Task<GitSyncResult> SaveAsync(string message, CancellationToken ct = default)
    Task SetConfigAsync(string key, string value, CancellationToken ct = default)
    Task SetRemoteUrlAsync(string name, string url, CancellationToken ct = default)
    Task SetUpstreamAsync(string remoteBranch, CancellationToken ct = default)
    static string ShortCommitHash(string? hash)
    Task StageAllAsync(CancellationToken ct = default)
    Task StagePathAsync(string path, CancellationToken ct = default)
    Task<bool> StashAsync(string? message = null, CancellationToken ct = default)
    Task<bool> StashPopAsync(CancellationToken ct = default)
    static string StripCredentialsFromUrl(string url)
    Task<GitSyncResult> SyncAsync(CancellationToken ct = default)
    static GitRepository? TryOpen(string directory)
    Task<(bool Success, string StdOut, string StdErr)> TryRunAsync(string args, CancellationToken ct = default)
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
    static ValueTask<object?> AwaitAndGetResultAsync(object? raw)
    static Type UnwrapResultType(Type declaredReturnType)
