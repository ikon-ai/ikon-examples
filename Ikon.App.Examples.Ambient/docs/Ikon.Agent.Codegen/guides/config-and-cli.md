# Config & CLI

## ikon-config.toml

```toml
# Database configurations for allocating databases for the app (run 'ikon app config' to apply changes)
# Assigned database connection info is available via app.Databases in the app. Use 'ikon app db' commands to manage databases.
# Format: "name:type" - valid types: postgres
# Examples: "mydb:postgres"
Databases = []

# Activation configuration for the app (app activation is separate from deployment)
[Activation]
# Whether to stop all active sessions of the app when a version of the app is activated
StopSessions = true

# Authentication configuration for the app frontend
[Auth]
# Whether the frontend requires authentication before connection to the app is established
Enabled = false
# Available methods: google, microsoft, email, guest
Methods = ["google", "microsoft", "email", "guest"]
# If non-empty, only allow users with email addresses from these domains to authenticate (e.g. ["mycompany.com"])
DomainAllowlist = []

# Target information for deployment (automatically configured with 'ikon app config')
# Use environment-specific config files: ikon-config.development.toml, ikon-config.production.toml
[Target]
OrganisationId = ""
SpaceId = ""
ChannelId = ""
Name = ""
```

## CLI Commands

### Project Management

- `ikon app new <name>`: create a new Ikon AI app
- `ikon app copy [source]`: copy an existing app into a new project with its own git, space, and database
  - `--from <dir>`, `--name <name>`, `--target-dir <dir>`, `--organisation-id <id>` / `--new-org <name>`, `--domain <prefix>`, `--no-commit`
- `ikon app load`: download an existing AI app from the cloud into a target directory (`--organisation-id`, `--space-id`, `--target-dir`)
- `ikon app build`: build the app
- `ikon app clean`: clean build artifacts
- `ikon app run`: run in local dev mode (starts both C# app and Vite frontend dev server)
  - `--host-lan`: run with LAN hostname instead of localhost
  - `--public-access`: expose to public internet
  - `--host <ip>`: run with explicit hostname
  - `--log-trace` / `--log-debug`: enable verbose logging
  - `--debug`: enable debug mode
  - `--skip-npm-install`: skip npm install step
- `ikon app stop`: stop a running app instance
- `ikon app info`: show running app instance info (URLs, PIDs)
- `ikon app update`: update Ikon NuGet and NPM package references to the latest version
- `ikon app icons`: regenerate every frontend's icon set from `branding/logo.svg` — web favicons + PWA manifest icons, and (when a Flutter frontend exists) the Android/iOS/web launcher icons. Only the generated icons and each manifest's `icons` array are rewritten; manual manifest edits (e.g. a custom PWA `name`) are preserved.
- `ikon app teleport build`: compile this app's `Schema/*.tp` files into C# + configured SDKs (run after editing a `.tp` schema)
- `ikon app release`: generate release notes and bump the app version (`--bump major|minor|patch`)
- `ikon app pipeline run <PipelineName>`: build the app and run one of its pipelines from the CLI. Auto-resolves the DLL and uses the space ID from `ikon-config.toml`.
  - `<PipelineName>`: short class name (e.g. `MyPipeline`) or fully qualified name; alternatively pass `--type-name <FQN>`
  - `--no-build`: reuse the existing build output instead of rebuilding
  - `--target <name>`: pick a specific `ikon-config.<name>.toml`
  - All `ikon pipeline run` flags pass through (`--input`, `--output`, `--config`, `--cache`, `--keep-running`, etc.) — run with `--help` for the full list

### Configuration & Deployment

- `ikon app config`: configure the app for cloud deployment (organisation/space; `--add-database`, `--target <name>`)
- `ikon app status`: check app configuration status
- `ikon app target list`: list all configured targets
- `ikon app secret set <key> [value]`: store/update a secret (token / API key); `--stdin` reads from stdin
- `ikon app secret list`: list secret keys for this app (values are never shown)
- `ikon app secret delete <key>`: delete a secret (`--yes` to skip prompt)
- `ikon app bundle`: create the app bundle without deploying
- `ikon app deploy`: create the app bundle and deploy it to the cloud
- `ikon app deploy list`: list deployed app bundles
- `ikon app deploy activate`: activate a specific app bundle (`--id <bundle-id>` or `--latest`; `--yes`)
- `ikon app deploy delete`: delete an app bundle (`--id <bundle-id>` required; `--yes`)
- `ikon app export`: export an app to a distributable zip-file or directory
- `ikon app docs`: (re)generate app documentation under `docs/` and update `AGENTS.md`

### Database Management

- `ikon app db list`: list databases provisioned for the current space
- `ikon app db connection-string`: print the database connection string
- `ikon app db reset`: empty the database — drops all data; the app recreates its schema on next start
- `ikon app db remove`: remove a database from the current space (destructive; `--database <name>` if multiple)
- `ikon app db ef-migrate-add <name>` (EF Core apps only): create a new EF Core migration
- `ikon app db ef-migrate-apply` (EF Core apps only): apply pending EF Core migrations
- `ikon app db ef-migrate-list` (EF Core apps only): list EF Core migrations and their status
- `ikon app db ef-migrate-remove` (EF Core apps only): remove the last unapplied EF Core migration

### Version Control

- `ikon app save`: save all changes to version control
- `ikon app sync`: download the latest version from version control
- `ikon app changes`: show uncommitted changes
- `ikon app compare`: compare current files with a saved version
- `ikon app discard`: discard all uncommitted changes
- `ikon app history`: list saved versions
- `ikon app label`: create or update a named label for the current version
- `ikon app restore`: switch local files to a version
- `ikon app promote`: make an older version the new latest in version control

### Testing

- `ikon app test list`: list recorded Playwright tests
- `ikon app test record <name>`: record a Playwright test for the app
- `ikon app test play [name]`: play back recorded tests (`--all`, `--headless`)

### Operations & Observability

Inspect what a deployed app is doing in the cloud — analytics events, live sessions, and per-session logs.

- `ikon app events`: fetch recent analytics events for an app's space (`--days <1-30>`, `--limit`, `--all`, `--format table|json|csv`, `--output <file>`)
- `ikon app sessions list`: list sessions for an app's space (`--search-id <substr>`, `--limit`, `--all`)
- `ikon app sessions logs <session-id>`: fetch logs for a specific app session (`--level <0|10|20|30|40|50>`, `--limit`, `--all`)

### Top-level Ikon CLI

These are not under `ikon app` but are part of the everyday loop:

- `ikon login [target]` / `ikon logout`: authenticate / sign out
- `ikon status`: show current login status and backend environment
- `ikon version`: print the installed ikon tool version
- `ikon update`: update the ikon tool to the latest version
- `ikon examples open|download`: open or download Ikon example projects

Note: during development, there is no need to run `ikon app build` if app is running because the Ikon server also hot reloads all C# changes. To see any compilation errors, search for latest `ikon-server-*.log` under the root `build/app/artifacts/` directory.
