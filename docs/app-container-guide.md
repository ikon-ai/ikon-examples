<!-- checked-against: 38b91cecd6f3c7e0db3691ae -->

# App Container Guide

How an IKON app gets a native program, system library or font that the platform's server image
does not have: in its `tools/` folder, or by shipping its own Dockerfile. Read this before an app
starts a process other than `ffmpeg`.

## The model

A deployed app runs in the platform's server image: .NET, `ffmpeg`, and little else. An app that
needs more has two ways to bring it:

- **A `tools/` folder** for self-contained programs: a static binary, a portable build that carries
  its own runtime, a script. They ship in the bundle and are on the app's `PATH`. Sessions start
  exactly as fast as any other app's. Use this whenever the program runs from its own folder.
- **A `container/` folder** with a `Dockerfile`, for what has to be installed into the system:
  packages from `apt`, fonts, shared libraries other programs look for in fixed places. On deploy the
  platform builds that Dockerfile **on top of the server image** and runs the app's sessions in the
  result. Sessions of such an app never start on a prewarmed server, so their first start is slower.

## The tools folder

Put programs in `tools/<runtime>/` at the app root, one folder per platform they are built for:

| Folder | Used by |
|---|---|
| `tools/linux-x64/` | the deployed app, always |
| `tools/win-x64/`, `tools/osx-arm64/`, `tools/linux-x64/`, or the `arm64`/`x64` counterpart | `ikon run` on that kind of machine |
| `tools/source/<name>/` | nothing: the source of a program of your own, published into the folders above, never shipped |

`ikon deploy` puts `tools/linux-x64/` in the bundle, and the server puts it first on the app's
`PATH`, so the app starts a program by name. A `lib/` folder inside it is on `LD_LIBRARY_PATH`, for
every program the app's server starts. A binary or `#!` script keeps its execute bit; a symbolic link
is copied as the file or folder it points at. A link whose target does not exist on the machine
running the deploy (an absolute link from a Linux tree unpacked on a Mac or Windows machine) is left
out, and the deploy warns with `TOOLS_DANGLING_LINK` naming it. `ikon run` does the same with the
folder for the machine it runs on, so the same `Process.Start("renode")` works in both places.

Apps that share a server process with other apps (hosted mode) get no `PATH` change, since one app's
programs would shadow another's. Hosted mode is something IKON turns on per space; an app that ships
tools should stay off it.

A program that is too large to keep in the repository is better downloaded than committed. Declare
the download in `ikon-config.toml`, one `[[Tools]]` entry per file, and the tool fetches it before
`ikon run`, `ikon run --container`, `ikon bundle` and `ikon deploy`:

```toml
[[Tools]]
Runtime = "all"                      # or "linux-x64" for a deploy only, "win-x64" for a local run only
Url = "https://files.example.com/renode-1.17.0-portable.tar.gz"
Sha256 = "4ba7c68b59e2471f188ef4b4b112fcccd2802582c460beedceb63b84e5605a5f"
Archive = "tar.gz"                   # or "zip", or "none" for a single file
Strip = 1                            # path components dropped from every entry, like ADD --strip-components
Dest = "renode"                      # inside tools/<runtime>/; empty is the folder itself
```

Nothing runs to fetch them: the tool downloads what `Url` names, refuses it unless it hashes to
`Sha256` (64 hex characters), and extracts it inside `tools/<Runtime>/` only, dropping `Strip` leading
path components. An entry whose destination already holds what it fetched, by the marker the fetch
leaves, is skipped, so a second run costs nothing; change `Sha256` to fetch again. Add the runtime
folders to `.gitignore` (`/tools/linux-x64/`), not `tools/`, which would hide `tools/source/`. A
zip cannot carry executable bits, and a tar.gz entry that is a link is skipped rather than
followed; a program that needs its whole folder beside it gets a small launcher script there:

```sh
#!/bin/sh
exec "$(dirname "$0")/renode-portable/renode" "$@"
```

A program that loads plugins or reads a config folder (ImageMagick's `modules-*/coders` with their
`.la` files, and its XML config) ships those folders beside it too, and is started with the
variables that point at them set: in such a launcher, relative to `$(dirname "$0")`, or through
`ProcessStartInfo.Environment`.

The bundle, tools included, is limited to 512 MB.

## Custom containers

Custom containers are off for every space until IKON turns them on for it. A deploy with
`container/` to a space without them stops before anything is built and says so. Before writing
a Dockerfile, ask IKON to turn them on with `ikon report create --kind idea` naming the space id,
and ship the program in `tools/linux-x64/` meanwhile.

### The Dockerfile

```dockerfile
ARG IKON_BASE_IMAGE
FROM ${IKON_BASE_IMAGE}

USER root

RUN apt-get update \
  && apt-get install -y --no-install-recommends imagemagick \
  && rm -rf /var/lib/apt/lists/*

ADD --checksum=sha256:4ba7c68b59e2447f188ef4b4b112fcccd2802582c460beedceb63b84e5605a5f \
    https://github.com/renode/renode/releases/download/v1.17.0/renode-1.17.0.linux-portable.tar.gz /tmp/renode.tar.gz
RUN tar -xzf /tmp/renode.tar.gz -C /opt && rm /tmp/renode.tar.gz \
  && ln -s /opt/renode_1.17.0-portable/renode /usr/local/bin/renode
```

The rules:

- **The last stage starts `FROM ${IKON_BASE_IMAGE}`**, with `ARG IKON_BASE_IMAGE` before the first
  `FROM`. The platform passes its server image there. Earlier stages may start from anything, so a
  tool can be compiled in a `rust` or `golang` stage and copied across with `COPY --from`.
- **Use `USER root` for installing.** You do not need to switch back: the platform appends its own
  final lines, which run the app as uid 1000 with `HOME=/home/ikon`, remove set-user-id bits, and
  clear any `ENTRYPOINT` or `CMD` you set.
- **Put programs on `PATH`** (`/usr/local/bin`), so the app starts them by name and a local run finds
  the copy installed on your machine.
- **Pin what you download.** `ADD --checksum=sha256:…` and versioned package names keep a rebuild
  identical to the build you tested.
- **No secrets.** The image is stored by the platform and read by its hosts. An API key the program
  needs goes in `app.Secrets` and is passed to it at runtime.

Other files in `container/` are the build context: `COPY scripts/setup.sh /opt/setup.sh` copies
`container/scripts/setup.sh`. A `.dockerignore` there is honoured.

### What happens on deploy

1. `ikon deploy` checks the Dockerfile's base rule and packs `container/` into the bundle.
2. The platform builds the image. The first build of a Dockerfile takes minutes. A later deploy that
   leaves `container/` unchanged reuses the image, so it takes no longer than any other deploy.
3. The deployment activates once the image is ready, and `ikon deploy` waits for it, showing
   "Building the app image and activating". A build that fails fails the deploy, and the end of the
   build log is printed under the error.

When IKON updates the server image, for example with security fixes, the platform rebuilds your
image on it. Sessions keep starting on the previous image until the new one is ready.

### Limits

| | |
|---|---|
| `container/` folder | 100 MB. Download large files in the Dockerfile instead of shipping them. |
| Build time | 30 minutes |
| Built image | 10 GB uncompressed |
| Builds per space | 20 per day, shared by every app in the space |

Every image is scanned for known vulnerabilities. The findings are reported but never block a deploy.

## Starting the program from the app

The app starts the program as a child process, by name:

<!-- ikon-example: app-container-run -->
```csharp
using var renode = Process.Start(new ProcessStartInfo("renode", ["--disable-gui", "--plain", "machine.resc"])
{
    WorkingDirectory = Path.Combine(app.DataDirectory, "hardware"),
    RedirectStandardOutput = true,
    UseShellExecute = false,
});
```

Files the program reads that belong to the app, such as firmware or scenes, ship in the bundle's
`Data/` and are read from `app.DataDirectory`. A `[Scripts] PrePackage` step writes generated ones
into `$IKON_BUNDLE_APP_DIR/Data/`; a binary or `#!` script written under `IKON_BUNDLE_APP_DIR` keeps its execute bit.

### Processing the bundle before it is zipped

`ikon bundle` and `ikon deploy` run each `PrePackage` command in the app root, in order, after the
build and before the bundle is zipped. A command starts without a shell, and `python3` at its start
runs `python` on Windows. A command that fails stops the bundle or the deploy and prints its output.
A command string is split on spaces, with quotes grouping words.

The commands are in the app's own file, which anyone a repository is shared with receives, so they
run only in a project that has allowed them: a terminal asks once — and keeps the answer — the
first time `ikon bundle` or `ikon deploy` meets them, and anything without a terminal (a coding
agent, CI) is refused with the commands it would have run until `ikon scripts enable` is run in the
app. The allowance covers the exact command list, so editing `PrePackage` asks again. `ikon scripts
disable` refuses them again. `ikon run` does not run `PrePackage`, and `ikon build` runs neither
it nor any `[[Tools]]` fetch.

A program that must run on every compile (it writes code or data the app compiles in or reads at start) is an MSBuild target in the app's csproj, not a `[Scripts]` step: `<Target Name="Generate" BeforeTargets="BeforeBuild"><Exec Command="node scripts/gen.mjs" /></Target>`. It runs on the machine that builds, so that machine needs the program.

```toml
[Scripts]
PrePackage = ["python3 scripts/pack_assets.py"]
```

The command finds the staged bundle through environment variables:

| Variable | What it is |
|---|---|
| `IKON_BUNDLE_APP_DIR` | the app's folder on the server: its `Data/` is `app.Files.Data` |
| `IKON_BUNDLE_TOOLS_DIR` | programs written here are on the deployed app's `PATH` |
| `IKON_BUNDLE_FRONTEND_DIR` | the frontend build |
| `IKON_BUNDLE_DIR` | the whole staging folder; anything outside the three above is zipped but never reaches the server |
| `IKON_APP_DIR` | the app root |

## Local runs

`ikon run` runs the app on your machine, not in the image. A program in `tools/` for your machine's
runtime is on `PATH` as it is in the cloud; one installed by `container/Dockerfile` has to be
installed locally as well, or found through an environment variable the app reads.

`ikon run --container` runs it in a Linux container instead, which is how to try `tools/linux-x64`
(`tools/linux-arm64` on an Arm machine) or `container/Dockerfile` on Windows or macOS before deploying. It needs Podman or Docker
(`ikon install podman` installs Podman). The tool builds the platform's server image with Node.js
locally, the first time in a few minutes, then your `container/Dockerfile` on top of it, and serves
the app from it on `localhost` as a plain run does. It differs from a plain run in three ways:

- C# changes do not hot-reload. Stop the run and start it again to pick one up.
- The frontend is served from the published SDK packages, and its dependencies are installed for
  Linux into a volume of their own, so the first start runs `npm install`.
- `--host`, `--host-lan`, `--host-public`, `--email capture`, `--sms capture`, `--whatsapp capture`, `--ai-script` and
  the `--flutter` options are not available.
