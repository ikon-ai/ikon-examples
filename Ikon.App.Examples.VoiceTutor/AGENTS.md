<!-- This file is automatically updated by IKON tool commands. User edits are preserved only below the ikon-user-content-below marker. -->

# IKON app development guidelines

## Quick start

1. New app: the word table's six `###` parts (`ikon docs _start --outline`), read those the request touches (`--section "<subsection>"`) or `--search` each feature word, then your kind of app's part, their patterns and `ikon docs product-design --section "First build checklist"`. Write it, `ikon run --ai mock --messages capture --cron manual --detach`, then `ikon browse --steps "observe"`. Any guide: `ikon docs <name>`; changing an app: `--search` each feature word.
2. Pick the language (not English: translate all `frontend-fundamentals` "Internationalization (i18n)" lists, `emails/*.html` and `en.ts` too) and who shares an instance (`app-structure`): the scaffold's `SessionIdentity(string? UserId)` gives each user their own; one room for everyone is `SessionIdentity()` with `guest`, `SessionIdentity(string? Room)` one per `?room=`; `global` is not a room: it makes all signed-out visitors one user.
   Private app (`ikon new --private`): no `guest` or `global` in `[Surface.Auth] Methods`; a public site with staff keeps `guest` (`public-site-staff-area`); staff screens check a role; nobody is `admin` until `ikon profile role add admin --me` or `first-user-becomes-admin`.
3. Instances share nothing but `Global*` reactives, so a local run and a deployed one differ; `[Cron]` (hourly at most), MCP tools and endpoints (`[HttpGet("/x")]` serves `/api/x`) run in the empty-identity one unless a minted URL or a user bearer token (`[Mcp]` default; needs `UserId` in `SessionIdentity`) picks another. Records go in a database; an open screen shows another instance's new row only when told (`reactive-state`). Any client, guests too, reads every `[App]` reactive: personal data goes in `User*` reactives or a database.
4. A local run uses the app's cloud data, and a deploy then serves those rows, mock answers too (`databases`; once deployed, `--ai mock` needs `--yes`). Delete test data: `ikon db query "delete ..." --yes` (restart a running app), `ikon file delete`; `db state reset` keeps `PersistentGlobal*`. Required rows go in `OnStarting`.
5. Database commands act on the linked app unless `--target` names another. A program outside the app: `docs/Ikon.Sdk/`.
   `--ai mock` calls no tools; `--ai-script turns.json` scripts them (`ai-models`).
6. Before calling a change done, review its diff and fix what it confirms.

## Architecture

A deployed instance has 1 CPU and 256 MB by default (`scaling`); the image has .NET and `ffmpeg`, no Python or extra fonts. Databases and files stay in the EU; AI calls do only with an EU-only policy (`ikon org set --ai-region eu-only`) or EU call regions; many models are global-only (`region-pinned-ai`).

**UI** is C#: `view` (`UIView`) method calls in `UI.Root([Page.Default], content: view => { ... })`. `frontend-node` is that UI's shell: write React only in custom nodes (3D, WebGL, canvas games) and never `fetch` the app's endpoints.

**Styling:** Crosswind (Tailwind v4 classes and more) in one C# string array. Use semantic theme classes (`styling-and-motion`). A missing colour is a theme role (`new IkonTheme { ["primary"] = "amber-400" }`, `theming`); a raw hex (`bg-[#…]`) is IKON013, except a colour from the data: `$"bg-[{item.Hex}]"`.

## Running and checking the app

Test locally, not deployed. `ikon run --format text` blocks.
Builds take minutes: no short timeouts or `| tail`. Use `--detach` on `run`, `link`
and `deploy` (not `bundle`), then `ikon status --wait --timeout 80` (at most 100; exit 3 = wait again). After a run starts, only
`ikon build` type-checks edits. `build/app/artifacts/bin/<App>/debug/ikon-server-info.json`
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
**Build warnings:** `view.Card` pads itself: no `p-*` on it or its only child (IKON026), but a Column styled as a card without `view.Card` needs `p-5` (IKON034); empty lists: `view.EmptyState` (IKON038); tabs: `view.Tabs`, `listStyle: [Tabs.NavList], triggerStyle: [Tabs.NavTriggerMd]` (IKON039), not buttons (IKON041), four at most (IKON052).
One primary action per screen; `Card.Elevated` for what matters and one flat colour per card (IKON011); padding in a max-width `Layout.Page` (a Column, never a Row; header rows: `Layout.PageWidth`);
loading and error states; buttons beside a title in `Layout.Row.Actions`; a phone is the default client: an `h-screen` root, a `ScrollArea` in a `flex-1 min-h-0` parent, rows that wrap or stack below `md:`, checked at phone width before the first report; pages: one `view.NavOutlet` per screen, switch on `app.Navigation.CurrentPath`, `path:` on the opening control (`app-structure`); tab titles: `accessibility`.

## Common Pitfalls

`pitfalls`: each rule in full.

- **State**: reactives are fields built with their initial value, `private readonly ClientReactive<int> _count = new(0);`, no `app` argument (CS1503). Lists are `[Client]ReactiveList<T>`/`ReactiveDictionary`/`ReactiveHashSet` (IKON002), changed on the reactive (`_todos.Add(x)`; `.Value.Add` is CS1061). A render reruns only for a reactive it read, not a plain field (join-time state: a `ClientReactive`); there is no `view.Dynamic`/`Watch`/`Bind`/`Keyed` (CS1061). A render only reads (IKON031/059), cannot `await` (CS4034): load in a handler.
  Outside a handler, name the client: `_x.SetFor(ctx.ClientSessionId, v)` (an `int`). A field a `TextField`/`Select` binds to is a `ClientReactive`: a `Reactive<T>` is shared by the instance's browsers (IKON018/019).
- **Emerge** is static: `await Emerge.Run<T>(LLMModel.Claude55Sonnet, pass => { pass.Command = prompt; })`. AI calls throw: catch, show a retry. A control handler that throws shows nothing: subscribe `UI.ActionFailedAsync` once in `Main`.
- **View calls**: the leading style array is positional (`CopyField`: `value` first), `"default"` first or the theme goes (IKON025: `["default", "gap-3"]`), the rest named (CS1744, CS8323, IKON003). Display text is `text:` (`Markdown`: `content:`); Dialog, Alert, EmptyState: `title:`/`description:`. Form controls take `bind:` or `value:` + `onValueChange: async v => …` (a lone `value:` is read-only, IKON027). Only `Box`, `Button`, `Card`, `Image`, `Link`, `NavItem`, `TableRow`, `ToolbarButton` and charts take `onClick:`.
  A button's style is one token, `[Button.PrimaryMd]`; no `variant:`/`size:`.
- **Databases** (Dapper): a `snake_case` column needs an alias (`client_id AS ClientId`); a list filter is `= ANY(@ids)` (`IN @ids`: error 42601); `bigserial`/`bigint`/`count(*)` read as `long`; `EXTRACT`, `avg` of an integer, `sum` of a `bigint` as `decimal` (cast for `double`); `timestamptz` as `DateTime` (UTC, not `DateTimeOffset`), `date` as `DateOnly`.
- **Logging**: `Log.Instance.Info/Warning/Error($"…")`, not `app.Logger` (CS1061).
- **Inputs**: `TextField`/`TextArea`, not `Input`; `Select(ariaLabel: "x", options: [new SelectOption(v, l)])`.
- **Colours**: `bg-primary` is the page surface; brand fill `bg-brand-solid`, brand text `text-brand-secondary`.
- **Structure**: keep the `[App]` class, its two records, the top-level `return await App.Run(args);` (CS5001) and the global namespace (CS0234/CS8956). Each extra .cs file wraps its code in `public partial class <App>` (CS0106/CS8803), extension methods in a top-level `public static class`; a record a top-level type uses stays top-level (CS0246).

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
- **ui-components**: every UI component with examples: layout, inputs, overlays, drag and drop; charts: `charts`
- **utilities**: retries, embedded resources, fuzzy matching, logger providers
- **whatsapp**: WhatsApp messaging


## Playbooks

A whole job's procedure, `docs/Ikon.Agent.Docs/playbooks/<slug>/SKILL.md` with its files beside it; read it before planning:

- **cad**: Manufacturable CAD for a physical product on the developer's computer: exact B-rep solids in build123d with holes, threads, fits, STEP files, drawing sheets and a web GLB. For a visual mesh model or a Blender scene use `modeling`.
- **convert**: Convert, port or migrate an existing web app (Lovable, Replit, Next.js, React and Tailwind) into an IKON app, from a folder, zip, repository link or live site, or when a brief names an export under reference/.
- **modeling**: Model a physical product or assembly in 3D in a live Blender scene on the developer's computer, with real fits, then export a glTF digital twin with an explode animation. For STEP files and drawings use `cad`.


## API Signatures

Exact signatures of every public platform type are in `docs/Ikon.Agent.Docs/api/`, one file per namespace (`Ikon.App.md`; `-2`, `-3` parts listed in `_index.md`); they win over a guide. Grep them before guessing a signature (CS1739, CS1061). A type's line is indented two spaces and its members four. A long match prints `[Omitted long matching line]`: read the file there.
Database in a handler: `await using var conn = await app.DatabaseAsync(); await conn.OpenAsync();`; without `app`: `AppServices.Instance.OpenDatabaseAsync()` (already open; no `app.Services`).


## Code Patterns

`ikon docs <pattern-name>` prints a tested pattern (`docs/Ikon.Agent.Docs/patterns/`); `_start` maps kinds of app and request words to them.

<!-- ikon-user-content-below -->
