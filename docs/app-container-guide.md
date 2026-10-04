# App Container Guide
<!-- checked-against: f91c508595057f85 -->
How an Ikon AI app gets a native program, system library or font that the platform's server image
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
| `tools/win-x64/`, `tools/osx-arm64/`, `tools/linux-x64/` | `ikon run` on that kind of machine |

`ikon deploy` puts `tools/linux-x64/` in the bundle, and the server puts it first on the app's
`PATH`, so the app starts a program by name. A `lib/` folder inside it is on `LD_LIBRARY_PATH`, for
every program the app's server starts. A binary or `#!` script keeps its execute bit; a symbolic link
is copied as the file it points at. `ikon run` does the same with the folder for the machine it runs
on, so the same `Process.Start("renode")` works in both places.

Apps that share a server process with other apps (hosted mode) get no `PATH` change, since one app's
programs would shadow another's. Hosted mode is something Ikon turns on per space; an app that ships
tools should stay off it.

A program that is too large to keep in the repository is better fetched at deploy time than
committed: a `[Package] PrePackage` step can download it, check its SHA-256, and unpack it into
`IKON_BUNDLE_TOOLS_DIR`, which is the deployed `tools/linux-x64/`. A program that needs its whole
folder beside it gets a small launcher script there:

```sh
#!/bin/sh
exec "$(dirname "$0")/renode-portable/renode" "$@"
```

The bundle, tools included, is limited to 512 MB.

## Custom containers

Custom containers are off for every space until Ikon turns them on for it. A deploy with
`container/` to a space without them stops before anything is built and says so.

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

When Ikon updates the server image, for example with security fixes, the platform rebuilds your
image on it. Sessions keep starting on the previous image until the new one is ready.

### Limits

| | |
|---|---|
| `container/` folder | 100 MB. Download large files in the Dockerfile instead of shipping them. |
| Build time | 30 minutes |
| Built image | 10 GB uncompressed |
| Builds per app | 20 per day |

Every image is scanned for known vulnerabilities. The findings are reported, never block a deploy.

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
`Data/` and are read from `app.DataDirectory`. A `[Package] PrePackage` step writes generated ones
into `IKON_BUNDLE_APP_DIR`; a binary or `#!` script written there keeps its execute bit.

## Local runs

`ikon run` runs the app on your machine, not in the image. A program in `tools/` for your machine's
runtime is on `PATH` as it is in the cloud; one installed by `container/Dockerfile` has to be
installed locally as well, or found through an environment variable the app reads.
