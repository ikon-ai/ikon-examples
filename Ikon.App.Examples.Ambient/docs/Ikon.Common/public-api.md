# Ikon.Common Public API

namespace Ikon.Common
  class AsyncLocalInstances
    void Capture(object owner, bool allowOverride = false)
    void InitializeAll()
    void InitializeAll(IReadOnlyList<Type> explicitTypes)
    void Remove(object owner)
    void Restore(object owner)
    bool TryRestore(object owner)
    static AsyncLocalInstances Instance
  sealed class DatabaseConnectionInfo
    ctor()
    string ConnectionString { get; set; }
    string Name { get; set; }
    string Type { get; set; }
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
  sealed class IkonLoggerProvider : IDisposable, ILoggerProvider
    ctor()
    ILogger CreateLogger(string categoryName)
    void Dispose()
  static class IkonTaskExtensions
    // Intentionally does not await the task. Exceptions are observed and sent to onException .
    static void RunParallel(this Task task, Action<Exception>? onException = null)
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
    static string ApplicationExcel
    static string ApplicationJavascript
    static string ApplicationJson
    static string ApplicationMsword
    static string ApplicationOctetStream
    static string ApplicationPdf
    static string ApplicationSql
    static string ApplicationVndOpenxmlformatsOfficedocumentPresentationmlPresentation
    static string ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet
    static string ApplicationVndOpenxmlformatsOfficedocumentWordprocessingmlDocument
    static string ApplicationXml
    static string ApplicationZip
    static string AudioMpeg
    static string AudioXWav
    static string Binary
    static string DefaultExtension
    static string DefaultMimeType
    static string ImageBmp
    static string ImageGif
    static string ImageHeif
    static string ImageJpeg
    static string ImagePng
    static string ImageSvg
    static string ImageSvgXml
    static string ImageTiff
    static string ImageWebp
    static string TextCss
    static string TextCsv
    static string TextHtml
    static string TextJavascript
    static string TextMarkdown
    static string TextPlain
    static string TextXml
    static string VideoMp4
  enum PipelineExecutionMode
    None
    HttpsEndpoint
    Scheduled
  // A combined polymorphic converter that supports both single instances of TBase and collections of TBase. When reading, it searches for the "Type" property (in any order) to determine the concrete type. When writing, it writes a dictionary that always includes "Type" (as the first entry).
  class PolymorphicConverter<TBase> : JsonConverter<object> where TBase : class
    ctor()
    override bool CanConvert(Type typeToConvert)
    override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
  class RateLimiter
    ctor(TimeSpan window, int rateLimit)
    int Rate { get; }
    bool Guard()
  // A relay endpoint. Exposes the locally bound port and the publicly reachable host/port. Dispose to release the endpoint and its local port reservation.
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
    static T Run<T>(List<Type>? retryableExceptions, int retries, Func<T> func)
    static T Run<T>(Func<T> func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static void Run(List<Type>? retryableExceptions, int retries, Action func)
    static void Run(Action func, List<Type>? retryableExceptions = null, int retries = 5, Action<Exception>? onRetry = null, Action<Exception>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task<T> RunAsync<T>(List<Type>? retryableExceptions, int retries, Func<Task<T>> func)
    static Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task<T> RunAsync<T>(Func<Task<T>> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
    static Task RunAsync(List<Type>? retryableExceptions, int retries, Func<Task> func)
    static Task RunAsync(Func<Task> func, List<Type>? retryableExceptions = null, int retries = 5, Func<Exception, Task>? onRetry = null, Func<Exception, Task>? onFailure = null, bool useExponentialBackoff = true, string? description = null)
  static class Utils
    static string GenerateRandomToken(int size = 32)
    static string GetCSharpTypeName(object? obj)
    static IPAddress GetFirstIPv4AddressOrLocalhost()
    static string ToUnescapedString(string input, bool unicodeOnly = false)

namespace Ikon.Common.Assets
  static class StorageExtensions
    static Task AddCloudFilePublicStorageAsync(this Asset asset)
    static Task AddCloudFileStorageAsync(this Asset asset, TimeSpan? uploadTimeout = null)
    static Task AddCloudJsonStorageAsync(this Asset asset)
    static Task AddCloudProfileStorageAsync(this Asset asset)
    static Task AddLocalFileStorageAsync(this Asset asset, string root)

namespace Ikon.Common.Git
  // Git branch information.
  class GitBranch : IEquatable<GitBranch>
    ctor(string Name, bool IsRemote, bool IsCurrent)
    bool IsCurrent { get; init; }
    bool IsRemote { get; init; }
    string Name { get; init; }
  // Git file change type.
  enum GitChangeType
    Added
    Modified
    Deleted
    Renamed
    Untracked
  // Options for cloning a repository.
  class GitCloneOptions : IEquatable<GitCloneOptions>
    ctor(string? Branch = null, bool Shallow = false, GitCredentials? Credentials = null)
    string? Branch { get; init; }
    GitCredentials? Credentials { get; init; }
    bool Shallow { get; init; }
  // Git commit information.
  class GitCommit : IEquatable<GitCommit>
    ctor(string Sha, string ShortSha, string Author, string AuthorEmail, DateTimeOffset Date, string Message)
    string Author { get; init; }
    string AuthorEmail { get; init; }
    DateTimeOffset Date { get; init; }
    string Message { get; init; }
    string Sha { get; init; }
    string ShortSha { get; init; }
  // Git credentials for authenticated operations.
  class GitCredentials : IEquatable<GitCredentials>
    ctor(string Username, string Password)
    string Password { get; init; }
    string Username { get; init; }
  // Git diff between two commits.
  class GitDiff : IEquatable<GitDiff>
    ctor(string? FromSha, string? ToSha, List<GitFileDiff> Files)
    List<GitFileDiff> Files { get; init; }
    string? FromSha { get; init; }
    string? ToSha { get; init; }
  // A changed file in git status or diff.
  class GitFileChange : IEquatable<GitFileChange>
    ctor(string Path, GitChangeType Type)
    string Path { get; init; }
    GitChangeType Type { get; init; }
  // File diff information.
  class GitFileDiff : IEquatable<GitFileDiff>
    ctor(string Path, GitChangeType Type, int LinesAdded, int LinesRemoved, string? Patch = null)
    int LinesAdded { get; init; }
    int LinesRemoved { get; init; }
    string? Patch { get; init; }
    string Path { get; init; }
    GitChangeType Type { get; init; }
  // Strongly-typed git repository operations.
  class GitRepository
    ctor(string workingDirectory, GitCredentials? credentials = null)
    GitCredentials? Credentials { get; }
    string WorkingDirectory { get; }
    // Abort all in-progress operations (merge, rebase, cherry-pick).
    Task AbortAllInProgressOperationsAsync(CancellationToken ct = default)
    // Abort an in-progress cherry-pick.
    Task<bool> AbortCherryPickAsync(CancellationToken ct = default)
    // Abort an in-progress merge.
    Task<bool> AbortMergeAsync(CancellationToken ct = default)
    // Abort an in-progress rebase.
    Task<bool> AbortRebaseAsync(CancellationToken ct = default)
    // Add a remote. Credentials are stripped from the URL.
    Task AddRemoteAsync(string name, string url, CancellationToken ct = default)
    // Checkout an existing branch.
    Task CheckoutAsync(string branchOrRef, CancellationToken ct = default)
    // Checkout files from a specific ref without changing HEAD.
    Task CheckoutFilesFromRefAsync(string refName, string path = ".", CancellationToken ct = default)
    // Clone a repository to a target directory.
    static Task<GitRepository> CloneAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    // Clone a repository or sync if it already exists. Returns the repository instance with the current SHA.
    static Task<(GitRepository Repo, string? Sha, bool WasCloned)> CloneOrSyncAsync(string url, string targetDir, GitCloneOptions? options = null, CancellationToken ct = default)
    // Commit staged changes.
    Task<GitCommit> CommitAsync(string message, CancellationToken ct = default)
    // Commit staged changes with custom author.
    Task<GitCommit> CommitAsync(string message, string authorName, string authorEmail, bool allowEmpty = false, CancellationToken ct = default)
    // Build per-invocation environment variables that authenticate git HTTP(S) operations. Uses git's environment config mechanism (git 2.31+) to inject an Authorization header, appending to any GIT_CONFIG_COUNT entries already present in the process environment.
    static Dictionary<string, string?> CreateAuthEnvironment(GitCredentials credentials)
    // Create and checkout a new branch.
    Task CreateBranchAsync(string name, string? startPoint = null, CancellationToken ct = default)
    // Create a tag.
    Task<GitTag> CreateTagAsync(string name, string? message = null, CancellationToken ct = default)
    // Delete a tag.
    Task DeleteTagAsync(string name, CancellationToken ct = default)
    // Discard all uncommitted changes.
    Task DiscardChangesAsync(CancellationToken ct = default)
    // Rewrite the remote URL to its credential-free form.
    Task EnsureCleanRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    // Escape a commit message for shell.
    static string EscapeMessage(string message)
    // Fetch from remote.
    Task FetchAsync(bool includeTags = false, CancellationToken ct = default)
    // Count how many commits the local branch is ahead of and behind its origin counterpart. Returns null when the counts cannot be determined (e.g. origin/{branch} does not exist).
    Task<(int Ahead, int Behind)?> GetAheadBehindAsync(string branch, CancellationToken ct = default)
    // Get all branches.
    Task<List<GitBranch>> GetBranchesAsync(CancellationToken ct = default)
    // Get a local git config value.
    Task<string?> GetConfigAsync(string key, CancellationToken ct = default)
    // Get the current branch name.
    Task<string> GetCurrentBranchAsync(CancellationToken ct = default)
    // Get diff between HEAD and another target (or working directory if null).
    Task<GitDiff> GetDiffAsync(string? target = null, CancellationToken ct = default)
    // Get the HEAD commit.
    Task<GitCommit?> GetHeadCommitAsync(CancellationToken ct = default)
    // Get the HEAD SHA.
    Task<string?> GetHeadShaAsync(bool shortSha = false, CancellationToken ct = default)
    // Get commit history.
    Task<List<GitCommit>> GetHistoryAsync(int limit = 20, string? fromRef = null, CancellationToken ct = default)
    // Get remote URL exactly as stored in .git/config, including any embedded credentials.
    Task<string?> GetRawRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    // Get remote URL (without credentials).
    Task<string?> GetRemoteUrlAsync(string name = "origin", CancellationToken ct = default)
    // Get the current repository status.
    Task<GitStatus> GetStatusAsync(CancellationToken ct = default)
    // Get all tags.
    Task<List<GitTag>> GetTagsAsync(CancellationToken ct = default)
    // Check if repository has any commits.
    Task<bool> HasCommitsAsync(CancellationToken ct = default)
    // Check if a remote exists.
    Task<bool> HasRemoteAsync(string name = "origin", CancellationToken ct = default)
    // Check if there are uncommitted changes.
    Task<bool> HasUncommittedChangesAsync(CancellationToken ct = default)
    // Check if there are uncommitted changes under a specific path.
    Task<bool> HasUncommittedChangesAsync(string path, CancellationToken ct = default)
    // Check if the local branch has commits that have not been pushed to origin. A branch that does not exist on origin counts as unpushed when local commits exist.
    Task<bool> HasUnpushedCommitsAsync(string branch, CancellationToken ct = default)
    // Initialize a git repository and connect to a remote, preserving local files. Local files are kept as-is and NOT merged with remote content. Returns the repository instance ready for use.
    static Task<GitRepository> InitAndConnectAsync(string directory, string remoteUrl, GitCredentials? credentials = null, string? configKey = null, string? configValue = null, CancellationToken ct = default)
    // Initialize a new git repository.
    static Task<GitRepository> InitAsync(string directory, CancellationToken ct = default)
    // Check if the working directory is a git repository.
    Task<bool> IsGitRepositoryAsync(CancellationToken ct = default)
    // Check if a directory is a git repository.
    static Task<bool> IsGitRepositoryAsync(string directory, CancellationToken ct = default)
    // List all worktrees attached to this repository (including the primary one). Parses the output of `git worktree list --porcelain`.
    Task<List<GitWorktreeInfo>> ListWorktreesAsync(CancellationToken ct = default)
    // Push to remote.
    Task PushAsync(bool setUpstream = false, CancellationToken ct = default)
    // Check if a ref exists.
    Task<bool> RefExistsAsync(string refName, CancellationToken ct = default)
    // Rename current branch.
    Task RenameBranchAsync(string oldName, string newName, CancellationToken ct = default)
    // Reset HEAD to a specific ref (hard reset).
    Task ResetHardAsync(string target, CancellationToken ct = default)
    // Reset HEAD to a specific ref (soft reset - keeps changes staged).
    Task ResetSoftAsync(string target, CancellationToken ct = default)
    // Restore to a specific target (tag, sha, or branch).
    Task<GitSyncResult> RestoreAsync(string target, CancellationToken ct = default)
    // Run a git command (throws on failure).
    Task<string> RunAsync(string args, CancellationToken ct = default)
    // Save changes (stage, commit, push).
    Task<GitSyncResult> SaveAsync(string message, CancellationToken ct = default)
    // Set a local git config value.
    Task SetConfigAsync(string key, string value, CancellationToken ct = default)
    // Set remote URL. Credentials are stripped from the URL.
    Task SetRemoteUrlAsync(string name, string url, CancellationToken ct = default)
    // Set up tracking for a branch.
    Task SetUpstreamAsync(string remoteBranch, CancellationToken ct = default)
    static string ShortCommitHash(string? hash)
    // Stage all changes.
    Task StageAllAsync(CancellationToken ct = default)
    // Stage a specific path (file or directory).
    Task StagePathAsync(string path, CancellationToken ct = default)
    // Stash all changes.
    Task<bool> StashAsync(string? message = null, CancellationToken ct = default)
    // Pop the latest stash.
    Task<bool> StashPopAsync(CancellationToken ct = default)
    // Strip credentials from a git URL for safe display/comparison.
    static string StripCredentialsFromUrl(string url)
    // Sync to latest remote (fetch + reset --hard).
    Task<GitSyncResult> SyncAsync(CancellationToken ct = default)
    // Try to open an existing git repository.
    static GitRepository? TryOpen(string directory)
    // Run a git command (doesn't throw on failure).
    Task<(bool Success, string StdOut, string StdErr)> TryRunAsync(string args, CancellationToken ct = default)
    // Compare two git URLs, ignoring credentials and trailing slashes.
    static bool UrlsMatch(string? url1, string? url2)
  // Git repository status.
  class GitStatus : IEquatable<GitStatus>
    ctor(string Branch, string? HeadSha, bool HasUncommittedChanges, bool IsDetachedHead, int AheadBy, int BehindBy, List<GitFileChange> Changes)
    int AheadBy { get; init; }
    int BehindBy { get; init; }
    string Branch { get; init; }
    List<GitFileChange> Changes { get; init; }
    bool HasUncommittedChanges { get; init; }
    string? HeadSha { get; init; }
    bool IsDetachedHead { get; init; }
  // Result of a sync/restore/save operation.
  class GitSyncResult : IEquatable<GitSyncResult>
    ctor(bool Success, string? PreviousSha, string? CurrentSha, string? Error = null)
    string? CurrentSha { get; init; }
    string? Error { get; init; }
    string? PreviousSha { get; init; }
    bool Success { get; init; }
  // Git tag information.
  class GitTag : IEquatable<GitTag>
    ctor(string Name, string Sha, GitCommit? Commit = null)
    GitCommit? Commit { get; init; }
    string Name { get; init; }
    string Sha { get; init; }
  // Git worktree entry reported by `git worktree list`.
  class GitWorktreeInfo : IEquatable<GitWorktreeInfo>
    ctor(string Path, string? Head, string? Branch)
    string? Branch { get; init; }
    string? Head { get; init; }
    string Path { get; init; }
