<!-- checked-against: 610f06c70528314316e186d3 -->

# App Files Guide

How an IKON app stores, ships, and serves files: two folders in the repo, one API at runtime,
and automatic handling of binaries in git. Read this before adding images, media, datasets, or any
other file to an app.

## The model

One rule and two folders cover everything:

- **Git holds text; the IKON Asset store holds bytes.** Any binary file in your repo is stored in
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
Files **shipped in the repo** redeploy with the app. On a read, a runtime-written file wins over a
shipped file at the same path. A local run reads shipped files straight from the app's `data/` and
`public/` folders, so a file added there while the app runs is readable at once. In a deployed app, the
frontend serves the shipped `public/` files, and `app.Files.Public` reads one by fetching it from the
app's own URL.

## Referencing files

| The file | How to reference it |
|---|---|
| Shipped in `public/` | Its URL path: `public/textures/wall.png` → `"/textures/wall.png"`. On the server, `app.Files.Public.ReadBytesAsync(path)`, or `GetAbsoluteUrlAsync(path)` for `Audio.Play` |
| Shipped in `data/` | Its relative path via `app.Files.Data`: `"models/db.bin"` |
| Written at runtime (public) | In the browser, `await app.Files.Public.GetUrlAsync(path)`: a root-relative path on the app's own origin (`/ikon/assets/…`). For a model (`ImageUrlPart`), an email or share text, `GetAbsoluteUrlAsync(path)`. On the server, `app.Files.Public.ReadBytesAsync(path)` |
| Written at runtime (private) | `app.Files.Data.ReadBytesAsync(path)` — or `GetUrlAsync` for a signed URL an external tool (ffmpeg, a webhook) can fetch |

A runtime-written file's URL is not built from its path, and it changes on every write: the file
is stored under a new URL and the old one stops working. Ask `GetUrlAsync` for it after the write,
and give each version of a file you hand out (a feed entry, a link in an email) a path of its own.

A root-relative path has no host, so server code cannot fetch it: `new Uri(url)` throws, and
`Audio.Play` takes only absolute URLs. A file shipped in `public/` plays from
`GetAbsoluteUrlAsync(path)` in a local run and in a deployed app. A local run cannot fetch its own
local address, so it reads the file from the folder instead. To play a file the app wrote, read its bytes and play them as a stream:

<!-- ikon-example: app-files-play-written -->
```csharp
var bytes = await app.Files.Public.ReadBytesAsync("music/theme.mp3");
Audio.Play(MediaTargets.Everyone, new MemoryStream(bytes), "audio/mpeg");
```

Paths are plain relative file paths (`"thumbnails/42.png"`). A leading slash and backslashes are
normalized away rather than rejected; an empty path, a trailing slash, or a `.`/`..`/empty segment
throws `ArgumentException`.

Every `app.Files` call is a network round trip to cloud storage, in a local run as well as deployed.
A read through `app.Files.Data` makes one even for a file shipped in `data/`, because it looks for a
runtime-written file at that path first. Start independent calls together and await them with
`Task.WhenAll` rather than one after another. Do not call `ExistsAsync` before a read. Read the file
and catch `FileNotFoundException`, which costs one round trip rather than two.

## Binaries in git — nothing to run

Binary files never make a repo large: the tooling moves them to the Asset store and back when you
run these commands.

| You run | What happens to binaries |
|---|---|
| `ikon save`, `bundle`, `deploy` | Raw binaries upload to the Asset store; git records a small `.ikonasset` pointer. The working copy stays on disk. |
| `ikon run`, `clone`, `pull`, `restore` | Each pointer whose real file is missing or no longer matches its hash downloads the stored bytes over it — an offloaded binary edited since the last save reverts. |

Offloading rewrites the repository, so it happens only for an app that owns its git repository; an
app nested inside a larger one bundles and deploys its binaries as they are and leaves that
repository's files alone.

`ikon save` and the save before a deploy commit every file in the app that git does not ignore, and
offload the binaries among them, test fixtures included. Keep test files in `scratch/`, which the
app's `.gitignore` lists, or list their folder (`testdata/`) there, to keep them out of the
repository and the Asset store.

The native project folders of a Flutter frontend (`frontend-flutter/android`, `ios`, `macos`,
`windows`, `linux`) stay in git as they are, because the Android and iOS builds refuse files they do
not know beside their icons. So does the root `reference/` folder, where Studio and the `convert`
playbook put the material a person handed over (an export, a design, a recording), so that an agent
reading it in a task's working copy or a fresh clone gets the image, not a pointer.

A repository is checked out on Windows too, so the tooling renames any file whose name Windows
cannot store (`< > : " | ? * \`, a trailing dot or space, names like `CON`). `ikon save` renames it on
disk, and `clone`, `pull` and `restore` rename it in the version they check out. `|` becomes `_`,
for example. Each rename is listed, and code that loads the file by its old name has to be updated.

Files under `public/` upload as public (loadable by URL); everything else stays private. The
`ikon asset normalize` / `materialize` / `gc` verbs exist as manual overrides — normal
development never needs them. `materialize` replaces a binary that is only out of date (its pointer changed after a pull or a branch switch, and the file still has the bytes the tool last left there), keeps one you edited and warns (`--force` restores the stored bytes), and `normalize` leaves binaries git does not track yet alone unless given `--include-untracked`. `ikon duplicate` re-homes stored binaries under the copy.

## A shared app's node_modules

`node_modules` is bound to the machine that installed it: npm fetches only the native binaries
(rolldown, esbuild, fsevents, the @napi-rs packages) matching that OS and architecture, and nothing
in the tree says which one it was. Zip an app folder with Finder's **Compress** or Explorer's
**Send to > Zip** — neither reads `.gitignore` — and the tree travels to a machine it cannot run on,
where every presence check passes and the first native call fails inside the bundler.

`ikon run` detects this and reinstalls: the platform stamps the tree with the machine that
installed it and compares that stamp at startup. It deletes the tree before installing, because npm treats
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

**Never locate bundled files through `AppContext.BaseDirectory`.** In cloud the app bundle is loaded into a host process, and `AppContext.BaseDirectory` is the host's folder, so a path built from it finds nothing: it works locally and fails when deployed. Read through `app.Files.Data`, `app.DataDirectory` or an embedded resource. Code with no app instance at hand (a static cache, a `Lazy<T>`) resolves the app's own folder the way the platform does: `Path.GetDirectoryName(typeof(MyApp).Assembly.Location)`, falling back to `AppContext.BaseDirectory` only when `Location` is empty (a hot-reloaded assembly).

## Uploads from users

The `view.FileUpload` component streams a user's file wherever you point it — return a `FileUploadResult`
with an `AssetUri` from `onUploadStart` to store it in asset storage (see the
[asset system guide](asset-system-guide.md)), then serve or process it from there. For
files that should become part of the app's own tree, write them through `app.Files`.

Photos are made smaller on the user's device, not on the server. Set `maxImageDimension:` (the
longest side in pixels), `imageQuality:` (0 to 1, default 0.85) or `imageFormat:`
(`UploadImageFormat.Original`, `Jpeg`, `Png` or `Webp`) on `view.FileUpload`, `view.FileUploadZone`
or `view.FilePicker`, and the browser or Flutter client re-encodes a JPEG, PNG, WebP or HEIC photo
that is larger or in another format before it uploads. `maxFileSize:` is checked against the
result, and the file name's extension and MIME type follow the output format. WebP becomes JPEG
where the client cannot encode it: Safari before 17, and every Flutter platform. HEIC photos are
converted to JPEG, or to the `imageFormat:` given, and an image `accept:` list then offers them too; a browser that cannot decode
HEIC (Chrome, Firefox) uploads the original when `accept:` allows HEIC (`image/*`) and refuses it
otherwise. The server has no HEIC decoder, so an app that must display every photo should set one of
these options.

## Under the hood

`app.Files` and the `.ikonasset` scheme are built on the IKON asset system — `Asset.Instance`,
`AssetUri`, and the storage classes. The
[Asset System Developer Guide](asset-system-guide.md) documents that layer for advanced
use (optimistic concurrency, metadata, streams, custom storage paths).

The pointer scheme itself is `AssetLinkManager`, which the tooling drives over a repository:
`NormalizeAsync` converts raw binaries to pointers (upload the bytes, write the `.ikonasset`,
git-ignore the real path) and `MaterializeAsync` puts the real files back, hash-checked and
idempotent, throwing `AssetMaterializeException` — whose `Failures` lists `path: reason` — when a
blob is missing from the store. Storage is behind `IAssetBackend` (upload, download, delete, public URL), whose
platform implementation is `IkonAssetBackend`: public assets to a frontend-loadable class, private
ones readable only by the app, everything content-addressed by SHA-256 plus file extension and keyed
by space, so identical bytes of the same extension and class upload once per space and a pointer's
URI is immutable.

Space is reclaimed by a reachability GC in two steps: plan, then execute. `PlanGcAsync(repoDir, scope)`
returns an `AssetGcPlan` — the `RepoDir`, `Scope` and `WindowDays` it was made with, how many URIs
were `EverReferenced`, how many are `Kept`, and the `Orphans` (each an `AssetGcOrphan` holding one
URI) — without touching the store. `AssetGcScope` sets how much is kept: `History` keeps everything any
reachable commit references and so deletes nothing, `Window` keeps the working tree, the tip of every
branch, tag and stash, plus a recent day window, and `Current` keeps only what the working tree and
those tips reference, reclaiming every other historical version. `ExecuteGcAsync(plan)` then re-runs
the plan against the repo and deletes, best-effort, only the URIs both plans call orphans (do not
normalize while it runs), returning deleted and failed counts.
