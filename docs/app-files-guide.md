<!-- checked-against: 9c3b5fea6c72e9f48135340b -->

# App Files Guide

How an Ikon AI app stores, ships, and serves files: two folders in the repo, one API at runtime,
and automatic handling of binaries in git. Read this before adding images, media, datasets, or any
other file to an app.

## The model

One law and two folders cover everything:

- **Git holds text; the Ikon Asset store holds bytes.** Any binary file in your repo is stored in
  the Asset store automatically — git tracks a small `.ikonasset` pointer instead, and the tooling
  restores real files wherever they are needed. You never manage this.
- **`public/`** (at the app root) — files your app *shows*. Served by the frontend at their path:
  `public/hero.png` is `<img src="/hero.png">` or `view.Image(["w-full"], src: "/hero.png")`.
- **`data/`** (at the app root) — files your app's code *reads*. Never served to anyone.

At runtime, `app.Files` (an `AppFiles`) is the one API over both folders, each side an `AppFileTree`:

<!-- ikon-example: app-files -->
```csharp
// Read a shipped (or previously written) private file.
var rules = await app.Files.Data.ReadTextAsync("rules.md");

// Store a generated image and get the URL to show it.
await app.Files.Public.WriteBytesAsync($"thumbnails/{id}.png", bytes, "image/png");
var url = await app.Files.Public.GetUrlAsync($"thumbnails/{id}.png");
```

Files your app **writes at runtime** go to cloud storage and persist across deploys and restarts.
Files **shipped in the repo** redeploy with the app. On a read through `app.Files.Data`, a runtime-written file wins over a
shipped file at the same path; `app.Files.Public` cannot read shipped `public/` files, which are
reachable only by their URL path.

## Referencing files

| The file | How to reference it |
|---|---|
| Shipped in `public/` | Its URL path: `public/textures/wall.png` → `"/textures/wall.png"` |
| Shipped in `data/` | Its relative path via `app.Files.Data`: `"models/db.bin"` |
| Written at runtime (public) | `await app.Files.Public.GetUrlAsync(path)` |
| Written at runtime (private) | `app.Files.Data.ReadBytesAsync(path)` — or `GetUrlAsync` for a signed URL an external tool (ffmpeg, a webhook) can fetch |

Paths are plain relative file paths (`"thumbnails/42.png"`). A leading slash and backslashes are
normalized away rather than rejected; an empty path, a trailing slash, or a `.`/`..`/empty segment
throws `ArgumentException`.

## Binaries in git — nothing to run

Binary files never weigh a repo down: the tooling offloads them to the Asset store and back at
every natural boundary.

| You run | What happens to binaries |
|---|---|
| `ikon save`, `bundle`, `deploy` | Raw binaries upload to the Asset store; git records a small `.ikonasset` pointer. The working copy stays on disk. |
| `ikon run`, `clone`, `pull`, `restore` | Each pointer whose real file is missing or no longer matches its hash downloads the stored bytes over it — an offloaded binary edited since the last save reverts. |

Offloading rewrites the repository, so it happens only for an app that owns its git repository; an
app nested inside a larger one bundles and deploys its binaries as they are and leaves that
repository's files alone.

Files under `public/` upload as public (loadable by URL); everything else stays private. The
`ikon asset normalize` / `materialize` / `gc` verbs exist as manual overrides — normal
development never needs them. `ikon duplicate` re-homes stored binaries under the copy.

## A shared app's node_modules

`node_modules` is bound to the machine that installed it: npm fetches only the native binaries
(rolldown, esbuild, fsevents, the @napi-rs packages) matching that OS and architecture, and nothing
in the tree says which one it was. Zip an app folder with Finder's **Compress** or Explorer's
**Send to > Zip** — neither reads `.gitignore` — and the tree travels to a machine it cannot run on,
where every presence check passes and the first native call fails inside the bundler.

`ikon run` detects this and reinstalls: the platform stamps the tree with the machine that
installed it and compares that stamp on the way up. The repair is a deletion, not an install: npm treats
a populated tree's optional dependencies as already resolved and never fetches the missing binary
([npm/cli#4828](https://github.com/npm/cli/issues/4828)).

Share an app with `ikon share` or `ikon share --package` and none of this arises — `share` prints a
clone command so the recipient installs their own dependencies, and `--package` builds a zip without
carrying one machine's install to another.

## Older apps

Apps created before this layout keep files in `frontend-node/public/` and `app/<Project>/Data/`.
Run `ikon update` — it moves both folders to the root layout, repoints the project files, and
leaves everything referenced the same way (`/hero.png` URLs don't change).

`app.DataDirectory` remains as an escape hatch when a library needs a real filesystem path for the
shipped data files. It is read-only in the cloud — anything written at runtime goes through
`app.Files`.

**Never locate bundled files through `AppContext.BaseDirectory`.** In cloud the app bundle is loaded into a host process, and `AppContext.BaseDirectory` is the host's folder, so a path built from it finds nothing — it works locally, fails deployed. Read through `app.Files.Data`, `app.DataDirectory` or an embedded resource. Code with no app instance at hand (a static cache, a `Lazy<T>`) resolves the app's own folder the way the platform does: `Path.GetDirectoryName(typeof(MyApp).Assembly.Location)`, falling back to `AppContext.BaseDirectory` only when `Location` is empty (a hot-reloaded assembly).

## Uploads from users

The `view.FileUpload` component streams a user's file wherever you point it — return a `FileUploadResult`
with an `AssetUri` from `onUploadStart` to land it in asset storage (see the
[asset system guide](asset-system-guide.md)), then serve or process it from there. For
files that should become part of the app's own tree, write them through `app.Files`.

## Under the hood

`app.Files` and the `.ikonasset` scheme are built on the Ikon asset system — `Asset.Instance`,
`AssetUri`, and the storage classes. The
[Asset System Developer Guide](asset-system-guide.md) documents that layer for advanced
use (optimistic concurrency, metadata, streams, custom storage paths).

The pointer scheme itself is `AssetLinkManager`, which the tooling drives over a repository:
`NormalizeAsync` converts raw binaries to pointers (upload the bytes, write the `.ikonasset`,
git-ignore the real path) and `MaterializeAsync` puts the real files back, hash-checked and
idempotent, throwing `AssetMaterializeException` — whose `Failures` lists `path: reason` — when some
blob is simply gone. Storage is behind `IAssetBackend` (upload, download, delete, public URL), whose
platform implementation is `IkonAssetBackend`: public assets to a frontend-loadable class, private
ones readable only by the app, everything content-addressed by SHA-256 plus file extension and keyed
by space, so identical bytes of the same extension and class upload once per space and a pointer's
URI is immutable.

Reclaiming space is a two-step, plan-then-execute reachability GC. `PlanGcAsync(repoDir, scope)`
returns an `AssetGcPlan` — the `RepoDir`, `Scope` and `WindowDays` it was made with, how many URIs
were `EverReferenced`, how many are `Kept`, and the `Orphans` (each an `AssetGcOrphan` holding one
URI) — without touching the store. `AssetGcScope` is the safety dial: `History` keeps everything any
reachable commit references and so deletes nothing, `Window` keeps the working tree, the tip of every
branch, tag and stash, plus a recent day window, and `Current` keeps only what the working tree and
those tips reference, reclaiming every other historical version. `ExecuteGcAsync(plan)` then re-runs
the plan against the repo and deletes, best-effort, only the URIs both plans call orphans (do not
normalize while it runs), returning deleted and failed counts.
