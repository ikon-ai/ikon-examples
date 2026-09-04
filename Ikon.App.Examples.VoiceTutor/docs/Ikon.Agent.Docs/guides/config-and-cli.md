# Config & CLI

## ikon-config.toml

```toml
# Activation configuration for the app (app activation is separate from deployment)
[Activation]
# Whether to stop all active sessions of the app when a version of the app is activated
StopSessions = true

# Authentication configuration for the app frontend
[Auth]
# How visitors may enter: real sign-in providers (google, microsoft, email) plus two flavors of
# not-signed-in entry — "guest" gives each visitor their own device-scoped anonymous user (private
# per-visitor sessions), while "global" gives ALL visitors one shared space-wide anonymous user
# (they collapse onto a shared app instance — the cost-efficient mode for public landing surfaces;
# pair with [Sharded] on the SessionIdentity record to let it scale out; the app can distinguish a
# global visitor from a guest via Context.IsGlobal). List both to enter as
# "global" by default and let the app upgrade a visitor to a "guest" identity on demand. Signing
# in elevates to the real account from either.
Methods = ["google", "microsoft", "email", "guest"]
# If non-empty, only allow users with email addresses from these domains to authenticate (e.g. ["mycompany.com"])
DomainAllowlist = []
# When false (the default) visitors enter immediately as an anonymous session and can sign in on
# demand — branch on Context.IsAnonymous in the app and trigger sign-in with
# ClientFunctions.LoginAsync("google"). When true the frontend shows a mandatory sign-in wall before
# the app loads; listing "guest" in Methods adds a "Continue as guest" button to that wall.
# Anonymous browsing stays SEO-crawlable via [BootSnapshot] Routes regardless.
RequireSignIn = false

# Boot snapshot: capture the app's initial UI at build/deploy time so the first paint appears
# instantly. The snapshot is a PUBLIC asset, but it is privacy-safe by default — every piece of
# content is automatically replaced with a skeleton, so no sensitive data is baked in. Use
# SnapshotReveal to opt safe content (logos, static chrome) back in; a fully public landing-type
# page opts out wholesale by wrapping its page root in SnapshotReveal.
[BootSnapshot]
# Public routes to snapshot (e.g. ["/", "/pricing"]); an empty list disables boot snapshotting.
# Routes must be paths the app serves directly — a route the app redirects away from during
# capture fails the bundle/deploy, so declare the real path (e.g. "/home" instead of "/").
# Each route's snapshot paints instantly on load, and for apps openable without signing in (no
# sign-in wall, or "guest"/"global" in Auth.Methods) each route is also prerendered to crawlable
# static HTML with a generated sitemap.xml/robots.txt. Content routes (one per listing/article)
# can be added in app code via app.OnSnapshotRoutes(...). See the "Per-route snapshots and SEO"
# section of the Parallax docs.
Routes = ["/"]
# Seed rules (only for apps that render different pages per identity or path shape): ordered
# "pattern:variantId" entries, first match wins. A pattern segment is a literal, "*" (exactly one
# segment), or a final "**" (any remainder) — so "/*:dashboard" seeds the dashboard skeleton on any
# one-segment path and "/**:shell" on every path. Each distinct variant id is captured once as a
# skeleton the app renders by branching on view.SnapshotVariant == "id". Guests seed an exact route
# snapshot before these rules; signed-in visitors consult these rules first, then fall back to the
# route snapshot. Variant skeletons are never prerendered to HTML and never listed in the sitemap.
GuestSeeds = []
SignedInSeeds = []

# Custom packaging steps for 'ikon app bundle' and 'ikon app deploy'
[Package]
# Commands run in the app root after the app + frontend build, before the bundle is zipped. The bundle
# staging directory is exposed as IKON_BUNDLE_DIR, so a command can write processed output straight
# into the package (e.g. ["python scripts/pack_assets.py"])
PrePackage = []

# Target information for deployment (automatically configured with 'ikon app config')
# Use environment-specific config files: ikon-config.development.toml, ikon-config.production.toml
[Target]
SpaceId = ""
Name = ""
```

## CLI Commands

### Project Management

- `ikon app new <name>`: create a new Ikon AI app
- `ikon app share`: give someone else this app. Prints the one line they run — `ikon app clone --space-id <id> --prod|--dev` — and saves first, so what they get is what you have. Nothing is packaged and nothing is copied from your machine: they clone from version control and their dependencies install on their own. Reach for this before `app copy`, and never zip an app folder by hand — a zip carries `node_modules` and `build` with it, and those hold native binaries compiled for your CPU that will not run on theirs.
  - `--package`: for a recipient outside the app's organisation. Access is organisation-level, so there is no way to grant someone a single app; this produces the same distributable as `app copy --to zip` and the copy has no link back to your version control (`--dest-dir <dir>`, `--no-verify-build`)
  - `--no-save`: share the last saved version as it stands, without saving current changes first
  - `--project-dir <dir>`, `--target <name>`
- `ikon app copy`: copy an app into a distributable package (zip/folder to hand out) or into a new platform app with its own git, space, and database. This makes a *separate* thing — to give someone the app you already have, use `ikon app share`
  - `--from <dir>` (source app, defaults to the current directory), `--to <zip|dir|platform>` (prompted when omitted), `--name <name>`, `--no-update-libraries`, `--no-verify-build`
  - distributable only: `--output <dir>`
  - platform only: `--project-dir <dir>`, `--org-id <id>` / `--new-org <name>`, `--domain <prefix>`, `--no-commit`, `--no-local`
- `ikon app clone`: download an existing AI app from the cloud into a target directory (`--org-id`, `--space-id`, `--project-dir`)
- `ikon app delete`: delete the app's cloud space and all its cloud resources (databases, deployed bundles). Local project files are kept. Schedules removal after a grace period by default; `--now` tears it down immediately. Requires `--yes` in non-interactive mode.
- `ikon app build`: build the app
- `ikon app clean`: clean build artifacts
- `ikon app run`: run in local dev mode (starts both C# app and Vite frontend dev server)
  - `--host-lan`: run with LAN hostname instead of localhost
  - `--host-public`: expose to public internet
  - `--host <ip>`: run with explicit hostname
  - `--log-trace` / `--log-debug`: enable verbose logging
  - `--debug`: enable debug mode
  - `--skip-npm-install`: skip npm install step
- `ikon app stop`: stop a running app instance
- `ikon app update [dev|stable]`: update Ikon NuGet and NPM package references to the latest version. With no argument it stays on the channel the app is already pinned to; `dev` moves onto the unstable dev channel and `stable` returns to released packages
- `ikon app icon generate`: regenerate every frontend's icon set from `branding/logo.png` — web favicons + PWA manifest icons, and (when a Flutter frontend exists) the Android/iOS/web launcher icons. Only the generated icons and each manifest's icon/color entries are rewritten; manual manifest edits (e.g. a custom PWA `name`) are preserved.
- `ikon app teleport build`: compile this app's `schema/*.tp` files into C# + configured SDKs (run after editing a `.tp` schema)
- `ikon app release`: generate release notes and bump the app version (`--bump major|minor|patch`)
- `ikon app pipeline run <PipelineName>`: build the app and run one of its pipelines from the CLI. Auto-resolves the DLL and uses the space ID from `ikon-config.toml`.
  - `<PipelineName>`: short class name (e.g. `MyPipeline`) or fully qualified name; alternatively pass `--type-name <FQN>`
  - `--no-build`: reuse the existing build output instead of rebuilding
  - `--target <name>`: pick a specific `ikon-config.<name>.toml`
  - All `ikon pipeline run` flags pass through (`--input`, `--output`, `--config`, `--cache`, `--keep-running`, etc.) — run with `--help` for the full list

### Configuration & Deployment

- `ikon app config`: configure the app for cloud deployment (organisation/space; `--target <name>`)
- `ikon app db create|list|delete`: the space's databases (`--name`, `--type`, `--tier`); see the Databases section
- `ikon app status`: show the app's cloud bindings and running-instance info (URLs, PIDs; `--wait` polls until the app starts, up to `--timeout` seconds, default 30)
- `ikon app target list`: list all configured targets (`--format table|json|csv`, `--output <file>`)
- `ikon app secret set <key> [value]`: store/update a secret (token / API key); `--stdin` reads from stdin
- `ikon app secret list`: list secret keys for this app (values are never shown) (`--format table|json|csv`, `--output <file>`)
- `ikon app secret delete <key>`: delete a secret (`--yes` to skip prompt)
- `ikon app bundle`: create the app bundle without deploying
- `ikon app deploy`: create the app bundle and deploy it to the cloud
- `ikon app deploy list`: list deployed app bundles, newest first, with their IDs and state (`--state <state>` to filter, `--format table|json|csv`, `--output <file>`)
- `ikon app deploy show <id>`: show one bundle's full details (`--format table|json`, `--output <file>`)
- `ikon app deploy activate <id>`: activate a bundle by ID, or the newest with `--latest` (`--yes` to skip prompt)
- `ikon app deploy delete <id>`: delete a bundle by ID (`--yes` to skip prompt)
- `ikon app distribute`: build, sign, and push the Flutter app to testers' phones (`--flutter-android` → Firebase App Distribution, `--flutter-ios` → TestFlight, requires macOS; `--notes`, `--testers`, `--channel`). The upload is irreversible and goes to a shared tester group, so the run prints its plan and asks first — `--yes` to skip the prompt (required non-interactively), `--dry-run` to build and sign but upload nothing
- `ikon app install ios|android`: build the Flutter app and install it on a phone plugged into this computer — your own phone, not testers'. A release build carrying the same dart-defines and the same bundle id a deploy uses, so repeat installs replace the app instead of adding a second copy; signed by Xcode's automatic signing (pick the team in Xcode once) or the Android debug keystore. `--device` picks between several connected phones. Nothing is uploaded anywhere — `ikon app distribute` is the one that ships to testers
- `ikon app payments enable|disable|list|status`: manage the app's payment provider (`enable stripe|surfboard|mollie` auto-stores provider secrets; `status` shows merchant onboarding state)
- `ikon app payments offer create|delete|list`: manage the payment offers customers can pay for by id
- `ikon app docs`: (re)generate app documentation under `docs/` and update `AGENTS.md`

### Database Management

- `ikon app db list`: list databases provisioned for the current space (`--format table|json|csv`, `--output <file>`)
- `ikon app db connection-string show`: print the database connection string (bare string by default; `--format json` wraps it, `--output <file>`)
- `ikon app db reset`: empty the database — drops all data; the app recreates its schema on next start (`--yes` to skip prompt; required in non-interactive mode)
- `ikon app db delete`: delete a database from the current space (destructive; `--database <name>` if multiple; `--yes` to skip prompt, required in non-interactive mode)
- `ikon app db migrate add <name>` (EF Core apps only): create a new EF Core migration
- `ikon app db migrate apply` (EF Core apps only): apply pending EF Core migrations
- `ikon app db migrate list` (EF Core apps only): list EF Core migrations and their status
- `ikon app db migrate delete` (EF Core apps only): delete the last unapplied EF Core migration

### Version Control

These verbs require the app to own its git repository — the app directory must be the repository
root. An app created inside an existing repository does not get one of its own, so there they would
act on the enclosing repository instead: `ikon app discard` would clean all of it, `ikon app save`
would push every sibling app's source into this app's cloud space. They refuse in that case and name
the repository; use git directly there. `ikon app diff` and `ikon app history` still work, reporting
the enclosing repository.

- `ikon app save`: save all changes to version control
- `ikon app pull`: download the latest version from version control
- `ikon app diff [version]`: show uncommitted changes, or changes against a saved version
- `ikon app discard`: discard all uncommitted changes (`--yes` to skip prompt; required in non-interactive mode)
- `ikon app history`: list saved versions
- `ikon app label <name>`: create or update a named label for the current version
- `ikon app restore [version]`: switch local files to a version (`--yes` to skip prompt; required in non-interactive mode)
- `ikon app revert [version]`: restore an older version as the new latest in version control (`--yes` to skip prompt; required in non-interactive mode)
- `ikon app git show`: show the repository this app saves to (`--format table|json`, `--output <file>`)
- `ikon app git connect`: connect an external git repository that is kept in sync with saves (`--url`, `--provider github|gitlab|azure-devops|generic`, `--create` to create a private GitHub repo)
- `ikon app git list`: list connected external git repositories
- `ikon app git sync`: sync connected git repositories both ways
- `ikon app git disconnect`: disconnect a connected external git repository (`--url` optional when only one is connected)

### Asset Management

- Binary offloading is automatic: `ikon app save`, `app bundle`, and `app deploy` offload raw binaries to the Asset store (git tracks a small `.ikonasset` pointer, the working copy stays on disk), and `app run`, `app clone`, and `app restore` restore the real bytes wherever pointers lack them. Files under root `public/` upload as public (URL-loadable); everything else stays private. The verbs below exist as manual overrides:
- `ikon app asset normalize`: offload raw binaries now (self-heals binaries committed directly)
- `ikon app asset materialize`: download the real binary next to every git-tracked pointer
- `ikon app asset gc`: delete stored binary assets the repo no longer references (`--dry-run` to only list them, `--yes` to skip the confirmation, `--scope history|window|current`)

### Testing

- `ikon app test list`: list recorded Playwright tests (`--format table|json|csv`, `--output <file>`)
- `ikon app test record <name>`: record a Playwright test for the app
- `ikon app test play [name]`: play back recorded tests (`--all`, `--headed` to watch in a visible browser)

### Operations & Observability

Inspect what a deployed app is doing in the cloud — analytics events, live sessions, and per-session logs.

- `ikon app events`: fetch analytics events for an app's space (`--days <1-365>` or `--start`/`--end` as inclusive `yyyy-MM-dd` dates, `--limit`, `--all`, `--format table|json|csv`, `--output <file>`)
- `ikon app sessions`: list sessions for an app's space (`--search-id <substr>`, `--limit`, `--all`, `--format table|json|csv`, `--output <file>`)
- `ikon app sessions stop <session-id>`: stop a running cloud session (`--yes` to skip the confirmation); `ikon app stop` stops a local run instead
- `ikon app logs [session-id]`: fetch logs for an app session; defaults to the latest session (`--level <trace|debug|info|warn|error|critical>` (default info), `--format lines|json|csv`, `--limit`, `--all`, `--output <file>`)

### Platform environment

Every command runs against exactly one platform environment, resolved per invocation in this order:

1. `--dev` / `--prod` (mutually exclusive; either can go on any command)
2. `IKON_BACKEND_ENV=dev|prod`
3. Explicit credentials (`--backend-url`/`--backend-token`, `IKON_BACKEND_URL`/`IKON_BACKEND_TOKEN`) — the URL decides
4. The app project: exactly one of `ikon-config.development.toml` / `ikon-config.toml` (production) present ⇒ that environment
5. The machine default from `ikon defaults`
6. Otherwise: prompt when interactive, fail naming `--dev`/`--prod` when not

The chosen environment and the reason for it are printed before the command runs. There is never a
fallback to the other environment: a command that resolved to development and has no development
login fails saying so. `ikon app run` passes the resolved environment to the Ikon server, and running
the app straight from an IDE resolves the same way (minus the prompt).

Every `list` and `show` command takes `--format` and `--output <file>`: `table|json|csv` when it
returns rows, `table|json` when it returns one record. Data goes to stdout, notices and
pagination hints to stderr, so `--format json > file.json` is always valid JSON. The one
exception is `ikon app db migrate list`, which streams `dotnet ef` output verbatim.

### Top-level Ikon CLI

These are not under `ikon app` but are part of the everyday loop:

- `ikon login` / `ikon logout`: authenticate / sign out. `ikon login` signs in to production, and renews any other environment the machine is already signed in to in the same browser flow. `ikon logout dev` or `ikon logout prod` drops just one.
- `ikon status`: show login status for both environments — token expiry, default organisation and space, and which one commands use by default
- `ikon defaults`: set the default environment, organisation and space used by commands. `ikon defaults dev` / `ikon defaults prod` switches the environment on its own, offline and without a browser
- `ikon org list`: list the organisations you belong to, marking the default (`--limit`, `--format table|json|csv`, `--output <file>`)
- `ikon space list`: list your spaces — every organisation's by default, one organisation's with `--org-id <id>` — marking the default (`--search <term>` to filter by name, `--limit`, `--format table|json|csv`, `--output <file>`). Neither verb ever prompts, so a space id for `ikon app clone --space-id` can be looked up in one non-interactive call
- `ikon version`: print the installed ikon tool version
- `ikon update`: update the ikon tool to the latest version
- `ikon examples open|download`: open or download Ikon example projects
- `ikon networks`: list local network interfaces with their IPv4 addresses (handy with `ikon app run --host`)
- `ikon install dotnet|flutter|node`: install the pinned .NET SDK / the Flutter SDK / the Node.js toolchain (needed for `ikon app config --flutter` and `ikon app distribute`)
- `ikon install android|ios`: install one mobile toolchain on its own — the Android SDK and its JDK, or Xcode, CocoaPods and the iOS platform (macOS only). `ikon install flutter` runs both after the SDK, so these are for resuming whichever half stopped at a manual step, or for a machine that only ever ships one of the two
- `ikon install claude|codex`: install the Claude Code CLI or the Codex CLI, so the app can be developed with a coding agent. Installs the latest via npm — these are not pinned, they update themselves
- `ikon asset download|upload|metadata <source> [destination]`: download, upload, or inspect an individual Ikon asset (print text to console, save to file, or show mime type/size/tags)
- `ikon release-notes <target>`: print platform release notes for `ikon-tool`, `ikon-dotnet-libs`, `ikon-sdk-dotnet`, or `ikon-sdk-typescript` (`--limit`, `--order`, `--format json|csv`, `--summary` for an AI summary) — handy after `ikon app update`. `ikon release-notes list` names every target, `ikon release-notes versions <target>` lists the versions one has published

Note: during development, there is no need to run `ikon app build` if app is running because the Ikon server also hot reloads all C# changes. To see any compilation errors, search for latest `ikon-server-*.log` under the root `build/app/artifacts/` directory.
