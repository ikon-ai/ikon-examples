<!-- This file is automatically updated by IKON tool commands. User edits are preserved only below the ikon-user-content-below marker. -->

# IKON app development guidelines

## Quick start

1. New app: from `ikon docs _start --outline`, read only the parts for your kind of app and the request's words, one at a time (`--section "Data and state"`), `ikon docs --search "<words>"`, their patterns and `ikon docs product-design --section "First build checklist"`. Write the app, run `ikon run --ai mock --messages capture --cron manual --detach`, then `ikon browse --steps "observe"`. Deploy when asked.
2. Pick the language (not English: change what `frontend-fundamentals` "Internationalization (i18n)" lists) and who shares an instance (`app-structure`): the scaffold's `SessionIdentity(string? UserId)` gives each user their own; one room for everyone is `SessionIdentity()` with `guest` (many visitors: `[Sharding] Enabled = true`, `scaling`), `SessionIdentity(string? Room)` one per `?room=`; `global` is not a room: it makes all signed-out visitors one user.
   Private app (`ikon new --private`): no `guest` or `global` in `[Surface.Auth] Methods`; a public site with staff keeps `guest`, no `--private` (`public-site-staff-area`); staff screens check a role; none holds `admin` by default (`first-user-becomes-admin`; skip `ctx.IsSnapshot`). Guests sign in from a button calling `ClientFunctions.LoginShowAsync(reason)`.
3. Only `Global*` reactives are shared by every instance; `[Cron]` (hourly at most), MCP tools and endpoints (`[HttpGet("/x")]` serves `/api/x`) run in the empty-identity one unless a minted URL or a user bearer token (`[Mcp]` default; needs `UserId` in `SessionIdentity`) picks another. Records go in a database; an open screen shows another instance's new row only when told (`reactive-state`). Any client, guests too, reads every `[App]` reactive, private or not: personal data goes in `User*` reactives or a database.
4. A local run uses the app's cloud data, a deployed copy's too (`databases`): once deployed, `--ai mock` needs `--yes`. Put required rows in `OnStarting`; samples: `ikon db seed --profile demo`.
5. Database commands act on the linked app unless `--target` names another.
   `--ai mock` calls no tools; `--ai-script turns.json` scripts replies and tool calls (`ai-models`).
6. Before reporting a change done, review its diff and fix what the review confirms (Claude Code: the code-review skill; else a review command or re-read).

## Architecture

A deployed instance has 1 CPU and 256 MB by default (more: `scaling`), shared with `ffmpeg` (`media-analysis`); the image has .NET and `ffmpeg`, no Python or extra fonts. Databases and files stay in the EU; AI calls do only with an EU-only policy (`ikon org set --ai-region eu-only`) or EU call regions; many models are global-only (`ikon model list --eu`, `region-pinned-ai`).

**UI** is C#: `view` (`UIView`) method calls in `UI.Root([Page.Default], content: view => { ... })`. `frontend-node` is that UI's shell: write React only in custom nodes (3D, WebGL, canvas games) and never `fetch` the app's endpoints (for outside callers).

**Styling:** Crosswind (Tailwind v4 classes and more) in one C# string array. Use semantic theme classes and `Ikon.Parallax.Theming` tokens (`styling-and-motion`). A missing colour is a theme role (`new IkonTheme { ["primary"] = "amber-400" }`, `theming`); a raw hex (`bg-[#…]`) is IKON013, except a colour from the data: `$"bg-[{item.Hex}]"`.

## Running and checking the app

Test locally, not deployed. `ikon run --format text` blocks.
Builds take minutes. Avoid short timeouts and `| tail`. Use `--detach` on `run`, `link`
and `deploy` (not `bundle`), then poll `ikon status --wait --timeout 80`. `ikon build` type-checks
the frontend too, even in a run. `build/app/artifacts/bin/<App>/debug/ikon-server-info.json`
has the URLs: share the local one after a UI change. `ikon stop` saves persistent state, ends the run. A `.cs` edit under `app/<App>/` hot-reloads
the run (a compile error keeps the old build, a throwing `OnStarting` leaves
none, a `Main` edit outside its lambdas restarts, and `browse` prints the error;
`ikon status --wait` waits for it, exits 1 if failed; wait after the last save); a `.csproj`
or `ikon-config*.toml` change needs `ikon stop` and a new run, and a `schema/*.tp` change `ikon schema` or a new run. Check the app as text first (`--text-only` needs no browser):

```bash
ikon browse --text-only --steps "observe"  # text + [id] handles
ikon browse --steps "fill Message hello; tap Send; wait-absent 60s Thinking; wait 5s --new Sent"
ikon browse --steps 'tap Host; capture code /Room (\w+)/; as bob: fill Room code ${code}; tap Join'
```

`browse --help` lists steps. Acting steps print only what changed; `click`,
`eval` and `console` reach custom React components; `observe` lists a canvas or custom node as one line; its `content` prop adds a text summary. `wait 15s Sent` returns once the text shows, or if it showed before
the last tap, once the tap's handlers return. `--new` waits for new text. A bare `wait 15s` ends at the
app's first update or when those handlers return: not a pause, only `sleep 15s` pauses. A bare number is milliseconds. Each run is a new
client (new ids, empty `ClientReactive` state) that ends with the run, as does what the app keeps only while a client is connected (a room): a script names controls by label
and plays the whole flow in one run. PowerShell strips quotes: use
`--steps-file scratch/steps.txt`. `hold`, `mic` and `--fake-media-file` test a voice or camera app.
`look`, `upload`, `mic`, `audio` and custom nodes need the page (no `--text-only`).

A script worth keeping is a **check gate**, `.ikon/gates/<name>.toml`: `ikon test` runs it on
the local run or the deployed app, `deploy --require-gates` needs all to pass locally, and
`ikon task set <task-id> --reproduction <name>` records it as a defect's reproduction.

`--app-id <space>` or `--target <name>` makes `browse` and `test` drive the **deployed** app;
without them, the local run, else the linked deployed app. The page signs in as you when
the app offers the method you used for `ikon login` (or email, for an organisation app); `--as-guest` is a new guest each run, `--guest-name <name>` the same one; on a
local run `--as-user <name>` acts as a named test user (`--role`), and `as <name>+<role>:` as another up to the
next `as` (`as me:` is the run's own). Deployed, `as <name>:` is a pageless guest (none on a private app: test a second person on the local run); a room it hosts ends with it. When
a surface lists "global", every guest is one shared user, which `browse` cannot open a page as: use `--text-only` there, a local run for `look`, or list "guest". `click <name>` is a real mouse click.

Local `run` controls are `--ai real|mock`, `--ai-script <file>`, `--messages send|capture`
(overridden by `--email`, `--sms`, `--whatsapp`) and `--cron auto|manual`.

## The IKON tool's commands

`ikon help` lists commands by group, e.g. source (`save`, `pull`, `history`, `restore`), ship (`deploy`),
services (`db`, `secret`, `phone`, `email`), operate (`logs`, `costs`), autopilot, account.
`ikon help <verb> --format json` shows one; `ikon status` returns JSON when captured; `ikon ai` calls AI models.
Help, browse, test and logs default to text; browse/test/login JSON is one object per line.
Save with `ikon save`, not `git commit`/`git push`. It moves binaries to the Asset store and merges others' saves.
`ikon pull` gets theirs. `ikon deploy` saves first once there is a commit, unless `--no-save`. Inside a larger repository, use git.
On failure, stderr ends with `{code,message,next}` after notices. Settings: `ikon-config.toml`
(`ikon-config.development.toml` on development), each commented; `ikon-config.<name>.toml` for `--target <name>`.
Use file tools. PowerShell 5.1's `cat`/`Set-Content` garble non-ASCII.
IKON bugs: `ikon report create`.

Root folders (`app-structure`): `app/`, `frontend-node/`, `frontend-flutter/`, `data/`,
`public/`, `branding/` (replace the placeholder `logo.png`, then `ikon icon`), `emails/`, `legal/`, `router/`, `schema/`, `tools/<runtime>/` (programs the
app runs; source in `tools/source/`), `container/`, `devices/` (programs on other
machines), `scripts/` (kept scripts), `scratch/` (git-ignored throwaway
files, not `/tmp`), `reference/`, `test/`, `release-notes/`, `.ikon/gates/`, generated
`build/` and `docs/`.

## What the app must contain

From the first build, build only what the KIND of product's ordinary workflow needs.
Every control works or is disabled with a visible reason; changes, AI-made too, can be undone; a filter never loses records; anything reaching outside the app (`app.Payments`/`Email`/`Telephony`/`Notifications`, a publish, a permanent delete) needs a person to use a control or setting naming it, never an AI turn alone.

## How the app must look

Before the first `view.*` call, set a visual direction with `new IkonTheme { … }`, `["primary"]` included (IKON015; one look, dark or light, only with `Mode = ThemeMode.Fixed`; the scaffold is Adaptive), and write it to `branding/brand.md` (`product-design`).
**Build warnings:** `view.Card` pads itself: no `p-*` on it or its only child (IKON026), but a `Column([Card.*])` needs `p-5` (IKON034); empty lists: `view.EmptyState` (IKON038); tabs: `view.Tabs`, `listStyle: [Tabs.NavList], triggerStyle: [Tabs.NavTriggerMd]` (IKON039), not buttons (IKON041), four at most (IKON052).
One primary action per screen; `Card.Elevated` for what matters and one flat colour per card (IKON011); padding in a max-width `Layout.Page` (a Column, never a Row; header rows: `Layout.PageWidth`);
loading and error states; buttons beside a title in `Layout.Row.Actions`; a phone is the default client: an `h-screen` root, a `ScrollArea` in a `flex-1 min-h-0` parent, rows that wrap or stack below `md:`, checked at phone width before the first report; pages: `view.NavOutlet` + `path:` (`app-structure`).

## Common Pitfalls

`pitfalls` has each rule in full.

- **State**: reactives are fields built with their initial value, `private readonly ClientReactive<int> _count = new(0);`, no `app` argument (CS1503). Lists are `[Client]ReactiveList<T>`/`ReactiveDictionary`/`ReactiveHashSet` (IKON002), changed on the reactive (`_todos.Add(x)`; `.Value.Add` is CS1061). A render reruns only for a reactive it read, not a plain field (join-time state: a `ClientReactive`); there is no `view.Dynamic`/`Watch`/`Bind`/`Keyed` (CS1061). A render only reads (IKON031/059), cannot `await` (CS4034): load in a handler.
  Outside a handler, name the client: `_x.SetFor(ctx.ClientSessionId, v)` (an `int`). A field a `TextField`/`Select` binds to is a `ClientReactive`: a `Reactive<T>` is shared by the instance's browsers (IKON018/019).
- **Emerge** is static: `await Emerge.Run<T>(LLMModel.Claude55Sonnet, pass => { pass.Command = prompt; })`. AI calls throw: catch and show a retry. A control handler that throws shows nothing: subscribe `UI.ActionFailedAsync` once in `Main`.
- **View calls**: the leading style array is positional (`CopyField`: `value` first), `"default"` first or the theme goes (IKON025), and the rest is named (CS1744, CS8323, IKON003). The display string is `text:` (`Markdown`: `content:`); Dialog and Alert: `title:`/`description:`. Form controls take `bind:` or `value:` + `onValueChange: async v => …` (a lone `value:` is read-only, IKON027). Only `Box`, `Button`, `Card`, `Image`, `Link`, `NavItem`, `TableRow`, `ToolbarButton` and charts take `onClick:`.
  A button's style is one token, `[Button.PrimaryMd]`; no `variant:`/`size:`.
- **Databases** (Dapper): a `snake_case` column needs an alias (`client_id AS ClientId`); a list filter is `= ANY(@ids)` (`IN @ids`: error 42601); `bigserial`/`bigint`/`count(*)` read as `long`; `EXTRACT`, `avg` of an integer, `sum` of a `bigint` as `decimal` (cast for `double`); `timestamptz` as `DateTime` (UTC, not `DateTimeOffset`), `date` as `DateOnly`.
- **Logging**: `Log.Instance.Info/Warning/Error($"…")`, not `app.Logger` (CS1061).
- **Inputs**: `TextField`/`TextArea`, not `Input`; `Select(ariaLabel: "x", options: [new SelectOption(v, l)])`.
- **Colours**: `bg-primary` is the page surface; brand fill `bg-brand-solid`, brand text `text-brand-secondary`; the tone is `SemanticTone.Error`, no `Danger`.
- **Structure**: keep the `[App]` class, its two records, the top-level `return await App.Run(args);` (CS5001) and the global namespace (CS0234/CS8956). Each extra .cs file wraps its code in `public partial class <App>` (CS0106/CS8803), extension methods in a top-level `public static class`.

## API Reference Guides

Each agent guide is `docs/Ikon.Agent.Docs/guides/<name>.md`, one subject per guide:

- **accessibility**: names, headings, landmarks and focus for screen readers
- **agent-stages-and-history**: stage machines, pass records, journals and checkpoints of agent threads
- **agent-threads**: long-running agent threads, sub-agents, artifacts, budgets, asking the user
- **ai-advanced**: text-to-SQL, AI database and storage
- **ai-image**: image generation, upscaling, depth maps, segmentation and 3D meshes
- **ai-models**: choosing an LLM model; KernelContext
- **ai-speech-and-audio**: text to speech, speech recognition, sound effects and music
- **ai-video**: video generation and enhancement; Video Playback
- **ai-web-and-data**: web search and scraping, classification, decisions, OCR, embeddings, retrieval
- **app-api-reference**: the signatures of IApp and the host services
- **app-container**: the cloud server image; bringing programs, fonts and packages
- **app-structure**: the app class, identity, lifecycle, files, legal pages, navigation, client functions, join-URL QR
- **asset-system**: asset storage: cloud files and JSON, metadata, expiry, concurrency
- **audio-video**: playing and capturing audio and video, effects, calls, broadcasts, synthesis
- **charts**: charts and sparklines
- **component-vocabulary**: the enums, records and event arguments components take
- **consent**: asking for and recording end-user consent
- **costs**: the app's AI costs in credits, per day and per usage event
- **crosswind-reference**: Crosswind styling recipes: layouts, gradients, overlays, effects
- **csharp-primer**: modern C# idioms and the syntax mistakes that fail a build
- **databases**: Postgres, SQL with Dapper, EF Core, pgvector, migrations and seed data
- **email**: sending and receiving email
- **emergence-strategies**: advanced Emerge patterns: map-reduce, tree search, critics, ensembles
- **emergence**: Emerge.Run<T>: structured output, tools, chat history, cancellation
- **endpoints-webhooks**: HTTP endpoints, public HTML pages, webhooks, MCP tools, raw sockets and cron methods
- **frontend-fundamentals**: the frontend shell: custom React nodes, sign-in, embedding, branding
- **function-registry**: functions the client and LLM tools can call
- **location**: one-off and continuous GPS location
- **logging**: logging from an app
- **media-analysis**: uploading large media and analysing it with ffmpeg
- **motion-reference**: the full motion and keyframe syntax
- **notifications**: push and in-app notifications, inbox, channels
- **parallax-overview**: the Parallax UI library as a whole, SEO and share cards
- **payments**: payments, subscriptions, offers and entitlements
- **pipelines-reference**: the pipeline API
- **pipelines**: background processing pipelines
- **pitfalls**: the rules behind most failed builds and first runs, by area; read an area's section before writing that kind of code
- **platform-runtime-types**: TOML, run type, name conversions, exceptions
- **product-design**: what each kind of app must contain and how it must look
- **profiles-and-roles**: user profiles, roles and custom attributes
- **reactive-state**: shared, per-client, per-user, persistent and global state; scopes
- **scaling**: instances, shards and cells for many users
- **secrets**: secrets and credentials
- **sso**: enterprise single sign-on
- **staging**: separate test apps, only when the person asks for one
- **styling-and-motion**: Crosswind classes, theme tokens, colour tokens and motion
- **tailwind-reference**: every Tailwind utility Crosswind supports
- **telephony**: SMS and phone calls
- **theming**: IkonTheme: palettes, dark mode and design tokens
- **ui-api-reference**: the parameters every component shares; where signatures are
- **ui-components**: every UI component with examples: layout, inputs, overlays, drag and drop
- **utilities**: retries, embedded resources, fuzzy matching, logger providers
- **whatsapp**: WhatsApp messaging


## Playbooks

A whole job's procedure, `docs/Ikon.Agent.Docs/playbooks/<slug>/SKILL.md` with its files beside it; read it before planning:

- **cad**: Manufacturable CAD for a physical product, on the developer's computer, exact B-rep solids in build123d with holes, threads, fits, an interference audit, STEP files, drawing sheets, a web GLB and a release manifest. For a visual mesh model or a Blender scene use `modeling`.
- **convert**: Convert, port or migrate an existing web app (Lovable, Replit, Next.js, React and Tailwind) into an IKON app, its screens rewritten in C#, from a folder, zip, repository link or live site, or when a brief names an export under reference/.
- **modeling**: Model a physical product, device, enclosure or assembly in 3D in a live Blender scene on the developer's computer, every internal part with real fits, then export a glTF digital twin with an explode animation. For STEP files and drawings use `cad`.


## API Signatures

Exact signatures of every public platform type are in `docs/Ikon.Agent.Docs/api/`, one file per namespace (`Ikon.App.md`; `-2`, `-3` parts listed in `_index.md`); they win over a guide. Grep them by name before guessing a signature (CS1739, CS1061). A type's line is indented two spaces and its members four. A long match prints `[Omitted long matching line]`: read the file at that line.
Database in a handler: `await using var conn = await app.DatabaseAsync(); await conn.OpenAsync();`; without `app`: `AppServices.Instance.OpenDatabaseAsync()` (no `app.Services`).


## Code Patterns

`ikon docs <pattern-name>` prints a tested pattern (`docs/Ikon.Agent.Docs/patterns/`); `_start` maps kinds of app and request words to them.

<!-- ikon-user-content-below -->

## What this app is for

The Validation app uses as much of the app-facing platform API as it can, so that a platform change
that breaks an app fails this app's build, and `validation/validate.py platform-current` then drives
it end to end so each feature is proven to work, not only to compile. Runtime coverage lives in the
tab files. The guides' code examples live in the Patterns app, not here.

A new app-facing feature belongs here in a tab, with a `data-testid` on its button and on a result
line that starts with `PASS` or `FAIL`, and a step in `validation/validate_platform_features.py`.

### How each tab is proven

| Tier | When | What |
|---|---|---|
| free | hourly | every tab rendered by the smoke sweep; Self-Test's in-process battery; the one-click checks on the Typography, Buttons, Inputs, Advanced Inputs, Forms, Overlays, Navigation, App Nav (the selection and the page's skeleton showing before the server answers; on a deployment, the Flutter boot snapshots it ships), Layout, Drawing, Pan & Zoom, Actions, Device, Notifications, Session Identity, Account (the legal page links, reading the account's scheduled removal), Consent (Require gating), React SDK (server functions from the browser, consent both ways between C# and the hook, sibling client), Costs and Telephony (status) tabs; on the Ikon.AI tab, the model catalog the backend serves this organisation and an app provider class called by name; the older phases (assets, files, the upload lifecycle lab, database, custom messages, media, MCP, cron, payments, connection transports, SEO) |
| ai | six-hourly | the Ikon.AI Library cards, including Decider, direct model access, the mesh animation library, word timings and provenance; a call on the space's own provider key; a spend budget tripping |
| daily | the 00:00 UTC six-hourly run, or a dispatch with `run_daily` | an SMS and a call each way between the space's two numbers, a signature order on the generated PDF completed through Signicat's Norwegian BankID simulator and one the app stops waiting for, a step-up completed and one cancelled through Signicat's Norwegian BankID simulator, which the sandbox step-up client offers, and one the app stops waiting for, an eID sign-in on `/surfaces/eid` through the same simulator, again from a second browser to the same account, and one cancelled (they need the organisation's `StrongAuth` feature on), SAM 3 video segmentation |
| manual | a person, often on a phone | Motion, Recordings and Live Activity (native clients only), background location, completing a signature, a step-up or an eID sign-in with a real eID (BankID, FTN, Smart-ID), answering calls, the capture hooks, account deletion, browsing and changing a SharePoint site on the SharePoint tab, signing in to Google and working a Drive on the Google Drive tab, the upload lab's app stop mid-upload, the upload lab through the Dart client (`platform-dart/ikon_sdk/test/upload_lab_live_test.dart` against a local run), the App Nav tab on a phone or simulator (`frontend-flutter/integration_test/app_nav_test.dart` against a local run: skeleton, pending bar, kept start-up screen) |

The Hot Reload tab is proven by its own battery, `validation/validate platform-hot-reload`, which every
scheduled run starts after every other lane has ended, never beside this app's deploy or a tool lane's `ikon run`. It runs this app from the checkout with `ikon run`, rewrites `HotReloadProbe.cs` three
times under a page that stays open, and checks that page against one opened after each reload, then the
reloaded app's tabs and features, then that nothing warned. The battery rewrites the file line by line, so it
keeps one `public const string` per value, and `ValidationTheme` reads its radius from there.
A reload stops the run's cells. After each reload the tab's Cell card shows that the new instance's
start-up call into `LabCell` reached a cell running the new build (`LabCell.DescribeAsync` reads
`HotReloadProbe.Build`), that the page's mirror is live again and moves on a call, and that the
instance holds one cell connection.

The app closes a cell connection after 15 seconds unused instead of 5 minutes. `InitCells` sets the
limit with `SetConnectionIdleLimit`, an internal Ikon.App method. In about two minutes the
Cells tab's `idle` check sees a connection close, its cell-host stop 60 seconds later, and a new
cell-host start on the next call.

The Flutter frontend is proven by its own battery, `validation/validate mobile`, nightly on an Android
emulator and an iOS simulator (`.github/workflows/mobile.yaml`) and on a phone with `--device`. It runs
this app from the checkout and the integration test
`frontend-flutter/integration_test/mobile_validation_test.dart` on the device, which opens every tab,
presses the tab checks by their `data-testid` (the Dart renderer gives each one a semantics identifier),
then the device, media and lifecycle checks. When you add a tab here, add it to that test's tab list too.

The app declares eleven `[[Surface]]` tables so the `surfaces` phase (`validation/validate_platform_surfaces.py`)
can drive every surface property. `app` is the fallback that serves the tabs, so its snapshot routes come
from `app.OnSnapshotRoutes`. Each probe surface has a page at `/surfaces/<name>` (`Validation.Surfaces.cs`,
and the React probe in `frontend-node/src/lib/surface-probe/`): `site` (onAction, no media, no presence,
a seed, `global`), `static` (`Connect = "none"`), `members` (sign-in required), `staff` (`Roles`),
`console` (its own hostname, `Placement = "own"`), `gated` (sign-in required, with `login` as its
`[Surface.Auth] SignIn` page) and `kiosk` (`Recovery = "unattended"`). `login` has no route: a visitor who
has not signed in enters it at `/surfaces/gated`. The page picks itself by the path, not by
`ctx.Surface`, so a client that entered the wrong surface shows two names that differ.
`eid` admits only an eID sign-in (`signicat`), at `/surfaces/eid` (`Validation.EidSignIn.cs`), and
names `eid-login` as its sign-in page: a guest surface that asks for the shared password before it
offers the eID, since the stock sign-in screen would offer it to every visitor and each sign-in is
billed. That is why `app` does not list `signicat`, and why a person signed in with an eID cannot
open the tabs: the sign-in is for `/surfaces/eid` only. The password only hides the button: the backend
admits an eID sign-in into the space from anyone who builds the start URL by hand. A `staff` surface
with `Roles` means every external function needs a `[Surface]` scope, so `ValidationFunctions` and
`UploadLabReport` carry `[Surface("app")]`. Keep the tables the same in `ikon-config.development.toml` and
`ikon-config.production.toml`; `ikon-config.history2.toml` belongs to the frozen history2 deployment.

The rest of `ikon-config.development.toml` is set so that the battery can check it: `[SignIn]` text and a
digits code, two `[[PathResponse]]` entries under `/legacy/`, a made-up `[Flutter]` team and certificate
fingerprint for the `/.well-known` app-link files, `[Analytics] UIActions` and
`[Activation] StopSessions = false`. `StateDatabase` names `validationdb`, where the Reactive Scopes tab's
persistent counter is read back as a row. `[Legal]` serves the privacy notice at `/legal/privacy-notice`
and redirects `/legal/terms` to `TermsUrl`. The four `[[Port]]` tables answer a line or datagram that
starts with `validation ` with the port's name and the rest of it (`Validation.Ports.cs`): a tcp, a udp
and a tls port, and `closed`, whose `Allow` names a documentation range, so no runner gets an answer.
The ports are public, so an answer is never longer than the probe and anything else gets none. `[Feedback] Button = "overlay"` is
checked against a page opened through a test link (`ikon feedback link create`), which is offered
feedback whatever the space's audience is. The `config` phase of `validation/validate_platform_current.py`
checks them, and the `activation` step checks that a guest's session outlives the deploy.

The Files tab's upload lifecycle lab runs the same scenarios hosted and locally, with one difference:
a hosted app's LocalFile store is its read-only bundle. There the scenarios that need a writable asset
(the refusals, hash-mismatch, app-cancel, client-abandon, replace, complete-throws) write to CloudFile,
`dest-local` reports `SKIP` (the validation script counts `SKIP` as not failed), and `zero` passes on
its temp-file half and names its LocalFile half as skipped; complete-throws expects a new CloudFile
asset to be deleted, which needs a backend that reports `created`; a local run uses LocalFile, and CloudFile beside it in the scenarios that cover both. Each
result line names the store it used. `resume` drops its session's connection on purpose, so the quiet
check accepts the web SDK's reconnect warning from the files page during the uploads phase.

The Ikon.AI tab's own-key check runs one GPT-OSS 120B call on a Groq key the space holds as its own,
and reports `SKIP own key` until a person stores one, from this folder:
`ikon secret set IKON_GROQ_API_KEY --stdin --description "Validation own-key route check" < groq-key.txt`.
It passes when the call ran in the app (`isRemote` false) on that key (`isUserCredential` true), was
served by the Groq route and was billed only under `.user` usage names. Every GPT-OSS 120B call of the
app then runs on that key, and the sessions running when the secret is set keep the values they started
with until the next deploy. Under an EU-only AI region policy the check reports `SKIP`, since Groq
serves the model globally only. The provider class check registers an in-process provider, so under
`ikon run --ai mock` it reports `SKIP`, because the mocks answer before any provider is called.

Telephony needs a 46elks and a Twilio number on the space (`ikon phone buy`). SharePoint needs an
Entra app registration with admin-consented Graph application permissions, given to the space as the
secrets `MICROSOFT_TENANT_ID`, `MICROSOFT_CLIENT_ID` and `MICROSOFT_CLIENT_SECRET`, and optionally
`SHAREPOINT_SITE_URL` for the site it opens by default; its search needs `Files.Read.All` or
`Sites.Read.All`, which `Sites.Selected` does not cover. A certificate given as
`MICROSOFT_CLIENT_CERTIFICATE` (base64 PFX, with `MICROSOFT_CLIENT_CERTIFICATE_PASSWORD` if it has one)
replaces the secret and enables the actions built on SharePoint's own REST API, which refuses
app-only tokens obtained with a secret; those need the SharePoint application permission
(`Sites.Selected` with a site grant, or `Sites.FullControl.All`) as well as Graph's. A scratch site
the app holds `fullcontrol` on is the one to test against. Without a certificate the tab's
REST-backed actions — site groups, role assignments, inheritance, attachments, the recycle bin,
resolved permissions — are hidden or report `SKIP`, and giving a list item an owner who has never
visited the site fails, since adding them to the site is a REST call. Signing in as yourself on the tab needs
`<public url>/sharepoint/signed-in` registered as a web redirect URI on the app registration, and
works only on the deployed app: the backend routes a path endpoint by the active bundle's declared
paths, and the sign-in's redirect carries no instance id to reach a local run. Subscribing to change
notifications needs the app reachable on a public https URL (a deployed app,
or a local run with a tunnel), since Graph calls `/sharepoint/notifications` to check the URL before
it subscribes.

Google Drive needs a Google OAuth client of type Web application, made by hand in the development
GCP project (`development-378413`, whose Drive API Terraform enables), given to the space as the secrets
`GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`, with `<public url>/google/signed-in` registered as an
authorized redirect URI; the tab shows the exact URI to register. Like SharePoint's, the sign-in
works only on the deployed app. It signs in as a person with the
full `drive` scope, a restricted scope that a client in Google's Testing status grants to its listed
test users without verification. Its push notifications need the app reachable on a public https URL,
as SharePoint's do, since Google posts to `/google/notifications`. A scratch folder in a test account
is the one to work in: the tab renames, moves, shares and deletes for real, and removes only the
permissions it granted itself.

The Telephony, Signatures, Costs, Database, Email, Payments, SharePoint, Google Drive and Ikon.AI
tabs sit behind the shared password gate, which local runs bypass.
