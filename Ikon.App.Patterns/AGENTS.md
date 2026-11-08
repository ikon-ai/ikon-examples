<!-- This file is automatically updated by ikon tool commands. User edits are preserved only below the ikon-user-content-below marker. -->

# IKON App Development Guidelines

## Quick start

1. New app, in order: (1) `ikon docs product-design --section "First build checklist"`, then write `branding/brand.md`; (2) `ikon docs --search "<request words>"` and the patterns it names (`ikon docs _start --section "By kind of app"`); (3) write the app; (4) `ikon link --target staging --new-app --detach`, then `ikon run --target staging --mock-ai --detach` and `ikon browse --steps "observe"`; (5) `ikon deploy` only when asked. Converting: `ikon docs convert` first.
2. Pick a visual direction (below), the language (not English: write the C# text in it or use `multilingual-csharp-ui`; set `<html lang>` and add `src/i18n/xx.ts` for sign-in; `frontend-fundamentals`) and who shares an instance (`app-structure`): the scaffold's `SessionIdentity(string? UserId)` gives each user their own; for one shared room use `SessionIdentity()`, `SessionIdentity(string? Room)` one per `?room=`.
   Private app (`ikon new --private`): no `guest` or `global` in `[Surface.Auth] Methods`; staff-only screens check a role; none holds `admin` by default (`first-user-becomes-admin`). Signed-out visitors: `app-structure`.
3. Only `Global*` reactives are shared by every instance; `[Cron]`, MCP tools and endpoints run in the empty-identity one unless a minted URL or a user bearer token (`[Mcp]` default; needs `UserId` in `SessionIdentity`) picks another. Records go in a database; an open screen shows another instance's new row only when told (`reactive-state`).
4. A local run reads and writes the deployed app's databases, files and persistent state. Test on a staging copy (`app.Seed` runs on staging and gym copies after `OnStarting`, so reload there a list loaded in it; the deployed app opens empty: give lists and charts empty states). Set `[[Surface]]` before linking: staging takes it then; later changes go in every `ikon-config*.toml`.
5. `browse` without `--target` drives the local run; a deployed copy spends credits. `ikon db query` acts on the live app unless `--target` names one.
   `--mock-ai`: an `Emerge.Run<T>` array gets one item; `[Description("...", minArrayItems: 4)]` on each list property sets four. No tool is called unless the run started with `--mock-ai-script turns.json` scripting `tool` turns, and a typed call needs a `{"json": {...}}` turn (`ai-models`).

## Architecture

A deployed instance has 1 CPU, 512 MB; its image has .NET and `ffmpeg`, no Python or extra fonts. Databases and files stay in the EU (AI: `region-pinned-ai`); media models (music, video, mesh) are global-only (`ikon model list --eu`).

**UI** is declared in C# inside `UI.Root([Page.Default], content: view => { ... })`. Components are methods on `view` (`UIView`). 3D (three.js), WebGL and canvas games are custom React nodes (`frontend-fundamentals`, "A 3D scene or a game").

**Styling:** Crosswind, IKON's Tailwind superset (v4 class names), in C# string arrays. Use semantic theme classes and the `Ikon.Parallax.Theming` tokens built from them (`Button.PrimaryMd`, `Text.H1`; `styling-and-motion`), in one array. A missing colour is a theme role, set with `new IkonTheme { ["primary"] = "amber-400", … }` (`theming`); a raw hex (`bg-[#…]`) is IKON013.

## Running and checking the app

`ikon run --format text` blocks. `ikon run --detach` returns ready or still starting.
Builds take minutes. Avoid short timeouts and `| tail`. Use `--detach` on `run`, `link`
and `deploy`, then poll `ikon status --wait --timeout 80`. `build/app/artifacts/bin/<App>/debug/ikon-server-info.json`
has the URLs. `ikon stop` saves persistent state and ends the run. A `.cs` edit under `app/<App>/` hot-reloads
the run (a compile error keeps the previous build, an `OnStarting` that throws leaves
none, and `browse` prints the error;
`ikon status --wait` returns once the reload has applied or failed); a `.csproj`
or `ikon-config.toml` change needs `ikon stop` and a new run. Check the app as text first (`--text-only` needs no browser):

```bash
ikon browse --text-only --steps "observe"  # text + [id] handles
ikon browse --steps "fill Message hello; tap Send; wait-absent 60s Thinking; wait 5s --new Sent"
ikon browse --steps 'tap Host; capture code /Room (\w+)/; as bob: fill Room code ${code}; tap Join'
```

`browse --help` lists steps; `--full` prints whole texts. Acting steps print only what changed; `click`,
`eval` and `console` reach custom React components; `observe` lists a canvas or custom node as one line; its `content` prop adds a text summary. `look` is for pixels only. A run starts at `/`: `--path /room/7` opens another page in the run's own client, which uses up a one-time `?k=v` link before any `as` client. `wait 15s Sent` returns once the text shows, or if it showed before
the last tap, once the tap's handlers return. `--new` waits for new text. A bare `wait 15s` ends at the
app's first update or when those handlers return: not a pause, only `sleep 15s` pauses. A bare number is milliseconds. Each run is a new
client (new ids, empty `ClientReactive` state) that ends with the run, and so does what the app keeps only while a client is connected (a room): a script names controls by label
and plays the whole flow in one run. Where the shell strips quotes (PowerShell), use
`--steps-file scratch/steps.txt`. `hold`, `mic` and `--fake-media-file` (a .wav loops into
the microphone, or plays once with `--mic-once`; a .y4m is the camera) test a voice or camera app.
`look`, `upload`, `mic`, `audio` and `--fake-media-file` need the page, so no `--text-only`; on a local run `as bob:`
gets its own page once a step acts in it.

A script worth keeping is a **check gate**, `.ikon/gates/<name>.toml`: `ikon test` runs it against
the local run or the deployed app, and
`ikon task set <task-id> --reproduction <name>` records it as a defect's reproduction.

`--app-id <space>` or `--target <name>` makes `browse` and `test` drive the **deployed** app;
without them they drive the local run, else the linked deployed app. The page signs in as you when
the app offers the method you used for `ikon login` (or email, for an organisation app); `--as-guest` is a new guest each run, `--guest-name <name>` the same one; on a
local run `--as-user <name>` acts as a named test user (`--role`), and `as <name>+<role>:` as another up to the
next `as` (`as me:` is the run's own). Deployed, `as <name>:` is a guest (none on a private app), pageless, no upload; a room it hosts ends with it. When
a surface lists "global", every guest is one shared user, which `browse` cannot open a page as: use `--text-only` there, a local run for `look`, or list "guest". `click <name>` is a real mouse click.

Develop on the primary app first. When existing users or data need isolation,
`ikon link --target staging` makes a separately billed staging app; `--target staging` selects it
for `deploy`, `browse`, `test` and `logs`. Cloud staging runs real AI and delivers real messages.
Local `run` controls are `--ai real|mock`, `--ai-script <file>`, `--messages send|capture`
(overridden by `--email`, `--sms`, `--whatsapp`) and `--cron auto|manual`. They never change cloud execution.
Delete unused staging apps with `ikon app delete --target staging`; stopping sessions does not remove database charges.

## The ikon tool's commands

`ikon status` returns JSON when captured. Use `ikon help <verb-or-group> --format json`.
`--format text|json` overrides defaults; `IKON_OUTPUT_FORMAT` sets the preference.
Help, browse, test and logs default to text; browse/test/login JSON is one object per line.
On failure, stderr ends with `{code,message,next}` after notices. `ikon deploy` commits and pushes first (`--no-save`: neither). Settings: `ikon-config.toml`,
each commented: `ikon-config.development.toml` for an app on the development
platform (`run` and `deploy` read that one file), `ikon-config.<name>.toml` for `--target <name>`.
Use file tools. PowerShell 5.1's `cat`/`Set-Content` garble non-ASCII.
IKON bugs: `ikon report create`.

Root folders (`app-structure`): `app/`, `frontend-node/`, `frontend-flutter/`, `data/`,
`public/`, `branding/` (replace the placeholder `logo.png`, then `ikon icon`), `emails/`, `legal/`, `router/`, `schema/`, `tools/<runtime>/` (programs the
app runs; source in `tools/source/`), `container/`, `devices/` (programs on other
machines), `scripts/` (scripts the app keeps), `scratch/` (git-ignored steps files and throwaway
scripts, not `/tmp`), `reference/`, `test/`, `release-notes/`, `.ikon/gates/`, generated
`build/` and `docs/`.

## What the app must contain

Build what the KIND of product's ordinary workflow needs from the first build, nothing more.
Every control works or is disabled with a visible reason; changes, AI-made too, can be undone; a filter never loses records; and anything reaching outside the app (`app.Payments`, `app.Email`, `app.Telephony`, `app.Notifications`, a publish, a permanent delete) happens only when a person presses a control or turns on a setting naming it, never as a side effect of an AI turn.

## How the app must look

Before the first `view.*` call pick a visual direction, set it with `new IkonTheme { … }`, `["primary"]` included (IKON015; one look, dark or light, only with `Mode = ThemeMode.Fixed`; the scaffold is Adaptive), and write it to `branding/brand.md`, the brand book (`product-design`).
Then one primary action per screen; `Card.Elevated` for what matters and one flat colour per card (IKON011); padding in a max-width `Layout.Page` (a Column, never a Row; header rows: `Layout.PageWidth`); `view.Card` pads itself: no `p-*` on it or its only child (IKON026). Outside `view.Card`, a `Column` with a `Card.*` token pads nothing and needs `p-5` (IKON034);
empty (`view.EmptyState`), loading and error states; action buttons beside a title in `Layout.Row.Actions`; a phone is the default client: an `h-screen` root, a `ScrollArea` in a `flex-1 min-h-0` parent, rows that wrap or stack below `md:`, checked at a phone viewport before the first run report (`product-design`, "Every viewport"); tabs: four at most (IKON052), `view.Tabs` + `Tabs.NavList` (IKON039); pages: `view.NavOutlet` + `path:` (`app-structure`).

## Common Pitfalls

`pitfalls` has each rule in full.

- **State**: reactives are `private readonly` fields built with their initial value, `private readonly ClientReactive<int> _count = new(0);`, no `app` argument (CS1503). Lists are `[Client]ReactiveList<T>`/`ReactiveDictionary`/`ReactiveHashSet` (IKON002), changed on the reactive (`_todos.Add(x)`; `.Value.Add` is CS1061). A render reading `_x.Value` reruns on change: there is no `view.Dynamic`/`Watch`/`Bind`/`Keyed` (CS1061). A render only reads (IKON031), cannot `await` (CS4034): load in the handler. Outside a handler, name the client: `_x.SetFor(cid, v)`.
  A field a `TextField`/`Select` binds to is a `ClientReactive`: a plain `Reactive<T>` is shared by every browser of a `SessionIdentity()` instance (IKON018/019 warn only in `build/ikon-run.log`).
- **Emerge** is static: `await Emerge.Run<T>(LLMModel.Claude55Sonnet, pass => { pass.Command = prompt; })`. AI calls throw: catch and show a retry.
- **View calls**: the leading style array is positional (`CopyField`: `value` first), `"default"` first or the theme goes (IKON025), and the rest is named (CS1744, CS8323, IKON003). The display string is `text:` (`Markdown`: `content:`); Dialog and Alert: `title:`/`description:`. Form controls take `bind:` or `value:` + `onValueChange: async v => …` (a lone `value:` is read-only, IKON027). Only `Box`, `Button`, `Card`, `Image`, `Link`, `NavItem`, `TableRow`, `ToolbarButton` and charts take `onClick:`.
  Buttons are one token in an array, `[Button.PrimaryMd]`, with no `variant:`/`size:`.
- **Databases**: Dapper and EF Core are scaffolded; a `snake_case` column needs a Dapper alias (`client_id AS ClientId`), records too; `bigserial`, `bigint`, `count(*)` read as `long`, not `int`; `timestamptz` reads as `DateTime` (UTC), never `DateTimeOffset`, and `date` as `DateOnly`.
- **Logging**: `Log.Instance.Info/Warning/Error/Debug($"…")`, not `app.Logger` (CS1061).
- **Inputs**: `TextField`/`TextArea`, not `Input`; `Select(options: [new SelectOption(v, l)])`, not `items:`.
- **Colours**: `bg-primary` is the page surface, not the brand: brand fill is `bg-brand-solid`, brand text `text-brand-secondary`; the tone is `SemanticTone.Error`, no `Danger`.
- **Structure**: keep the `[App]` class, its two records, the top-level `return await App.Run(args);` (CS5001) and the global namespace (CS0234/CS8956). Every extra .cs file wraps its code in `public partial class <App>`; members outside a class give CS0106/CS8803. Keep the `__IKON_APP_NAME__` marker in `index.html` and the manifest.

## API Reference Guides

Each agent guide is `docs/Ikon.Agent.Docs/guides/<name>.md`, one subject per guide:

- **accessibility**: names, headings, landmarks and focus for screen readers
- **agent-stages-and-history**: stage machines, pass records, journals and checkpoints of agent threads
- **agent-threads**: long-running agent threads, sub-agents, artifacts, budgets, asking the user
- **ai-advanced**: text-to-SQL, AI database and storage
- **ai-image**: image generation, upscaling, depth maps, segmentation and 3D meshes
- **ai-models**: choosing an LLM model; KernelContext
- **ai-speech-and-audio**: text to speech, speech recognition, sound effects and music
- **ai-video**: video generation and enhancement; playing a video file
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
- **frontend-fundamentals**: the React frontend: custom components, sign-in, embedding, branding
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
- **styling-and-motion**: Crosswind classes, theme tokens, colour tokens and motion
- **tailwind-reference**: every Tailwind utility Crosswind supports
- **telephony**: SMS and phone calls
- **theming**: IkonTheme: palettes, dark mode and design tokens
- **ui-api-reference**: the parameters every component shares; where signatures are
- **ui-components**: every UI component with examples: layout, inputs, overlays, drag and drop
- **utilities**: retries, embedded resources, fuzzy matching, logger providers


## Playbooks

A whole job's procedure, `docs/Ikon.Agent.Docs/playbooks/<slug>/SKILL.md` with its files beside it; read it before planning:

- **convert**: Convert, port or migrate an existing web app (Lovable, Replit, Next.js, React and Tailwind) into an IKON app, its screens rewritten in C#, from a folder, zip, repository link or live site, or when a brief names an export under reference/.


## API Signatures

Exact signatures of every public platform type are in `docs/Ikon.Agent.Docs/api/`, one file per namespace (`Ikon.App.md`; `-2`, `-3` parts where one needs several, listed in `_index.md`), generated from the assemblies, so they win when a guide disagrees. Grep them by type or member name before guessing a signature (CS1739, CS1061). A type's line is indented two spaces and its members four: `OpenDatabaseAsync` is on `AppServices`, not on `app`. Grep can print `[Omitted long matching line]` for a long signature; read the file at that line instead.


## Code Patterns

`docs/Ikon.Agent.Docs/patterns/_start.md` routes each kind of app to patterns from production IKON apps, each compiled and render tested: adapt the closest one rather than writing a screen or a flow from scratch.

<!-- ikon-user-content-below -->

## What this app is for

Every C# example the platform's docs show is code this app compiles and, where it can, runs. The
docs quote it through `<!-- ikon-example: <id> -->` markers over a fence the docs build rewrites
from the matching `#region example:<id>`, so an example cannot drift from the API without failing
the build. `docs/private/specs/app-developer-docs-spec.md` (repo root) is the authority on how the
docs, their examples and their gates fit together — read it before changing how examples are
written, named or checked.

- `Patterns/` holds the patterns (`Ikon.Agent.Docs/content/patterns/*.md`), one gallery demo each.
- `Examples/` holds the examples the docs quote, one file per doc: `Guides/<Guide>.cs` for a
  published guide, `AgentGuides/<Guide>.cs` for an agent guide, `Shared/` for one several docs
  quote. `ExampleGallery` renders every one that draws something as an "Examples" demo;
  `AgentGuideExamples` is the "Agent guide examples" demo, split across the agent guide files.

`PatternRenderSmokeTests` in `Ikon.Agent.Test` renders every gallery demo headlessly and fails
one that throws, shows nothing a person can see, or draws a node type this app's frontend cannot
render (the spec's "What a gallery demo must show"), and
`ExampleTests` runs the examples whose worth is in running them (the theming moods, a
Teleport round trip, the value-mutation catalogue). Every AI service this app constructs runs on
its in-process mock (`GalleryMocks`, `ImplementationSelector.UseMocks`), so no render and no button
press ever calls a provider or costs anything; the examples that call one run when their button is
pressed, and the smoke test presses them all.

Change what a region contains and the doc that quotes it changes with it. Moving a region between
files changes nothing in any doc: the markers match by id.
