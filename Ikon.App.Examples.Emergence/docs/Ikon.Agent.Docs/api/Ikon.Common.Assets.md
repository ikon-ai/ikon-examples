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
