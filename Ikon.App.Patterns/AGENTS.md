<!-- This file is automatically updated by ikon tool commands. User edits are preserved only below the ikon-user-content-below marker. -->

# IKON App Development Guidelines

## Quick start

1. Converting an existing app? Its screens become C#: read the `convert` playbook first (`ikon docs convert`, `convert/lovable`). Else read the `product-design` guide first (what the product needs, undo, working controls), then `docs/Ikon.Agent.Docs/patterns/_start.md` (a row per kind of app) and your row's patterns; `ikon docs --search <feature>` finds any other feature.
2. Pick a visual direction (below) and who shares an instance (`app-structure`): the scaffold's `SessionIdentity(string? UserId)` gives each user their own; for a room everyone shares use `SessionIdentity()` (it shares every plain reactive; per-visitor ones: `Client*`/`User*`), `SessionIdentity(string? Room)` one per `?room=`, 200 clients each.
   Private app (`ikon new --private`): no `guest` or `global` in `[Surface.Auth] Methods`; any account signs in; staff-only screens check a role, and no one holds `admin` by default (`ikon profile role add admin --me`, or `first-user-becomes-admin`).
3. Each instance holds its own copy of every reactive, `Persistent*` too; `[Cron]`, MCP tools and endpoints run in the empty-identity one unless the URL was minted with an identity. Shared data goes in a database.
4. A local run reads and writes the deployed app's databases, files and persistent state. Test on a staging copy (its own data, the only one `app.Seed` runs on): `ikon link --target staging --new-app`. Set `[[Surface]]` before linking: staging takes it then; later changes go in every `ikon-config*.toml`.
5. The scaffold runs as is: `ikon run --target staging --mock-ai --detach` compiles it, then `ikon browse --steps "observe"` (a repeated label: `tap 'Save >> nth=1'`). `browse` without `--target` drives the local run; a deployed copy spends credits. `dev warning:` lines reach `build/ikon-run.log` only after a client renders the view: browse every screen. `ikon db query` acts on the live app unless `--target` names one.
   `--mock-ai`: speech recognition gives one fixed sentence (browse speech needs `--fake-media-file x.wav`). An `Emerge.Run<T>` array gets one item; `[Description("...", minArrayItems: 4)]` on the property sets four. No tool is called unless the run started with `--mock-ai-script turns.json` scripting `tool` turns, and a typed call needs a `{"json": {...}}` turn (`ai-models`).

## Architecture

An IKON app is a C# app in the cloud that streams reactive UI to browsers; an instance keeps running when every client disconnects. `frontend-node` is only for custom React components (`frontend-fundamentals`).

A deployed instance has 1 CPU and 512 MB; its image has .NET and `ffmpeg`, no Python or extra fonts (`app-container`). Databases and files stay in the EU (AI: `region-pinned-ai`); music, video, segmentation, depth and mesh models are global only (`ikon model list --eu`).

**AI services** (platform-held keys, except Rodin's): `Emerge.Run<T>()` for LLM text and typed output; `ImageGenerator`, `SpeechGenerator`, `SpeechRecognizer`, `VideoGenerator`, `MusicGenerator`, `EmbeddingGenerator`, `WebSearcher`, `Classifier`, `DepthEstimator`, `ImageSegmenter`, `VideoSegmenter`, `MeshGenerator`. Each has a static one-shot (`ImageGenerator.GenerateAsync(prompt)`, bytes: `GetDataAsync()`) and `new Xxx(model)` with `XxxConfig` (`ai-*` guides).

**UI** is declared in C# inside `UI.Root([Page.Default], content: view => { ... })`. Components are methods on `view` (`UIView`; no `ViewContext`, CS0246).

**Styling:** Crosswind, IKON's Tailwind superset (v4 class names), in C# string arrays. Use semantic theme classes and the `Ikon.Parallax.Theming` tokens built from them (`Button.PrimaryMd`, `Text.H1`; `styling-and-motion`), in one array: `[Button.PrimaryMd, "mt-4"]`. A `style:` array is exactly what renders (`"default"` first keeps the themed default). A missing colour is a theme role (IKON013), set with `new IkonTheme { ["primary"] = "amber-400", … }` (`theming`); a pinned dark palette needs `Mode = ThemeMode.Fixed`.

## Running and checking the app

From the app root, `ikon run --detach` starts the app, returns when ready; a plain
`ikon run` blocks. `build/app/artifacts/bin/<App>/debug/ikon-server-info.json`
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

`ikon browse --help` lists steps and options; `--full` prints whole texts. Acting steps print only what changed; `click`,
`eval` and `console` reach custom React components; `observe` lists a canvas or custom node as one line; its `content` prop adds a text summary. `look` is for pixels only. A run starts at `/`: `--path /room/7` opens another page in the run's own client, which uses up a one-time `?k=v` link before any `as` client. `wait 15s Sent` returns once the text shows, or if it showed before
the last tap, once the tap's handlers return. `--new` waits for new text. A bare `wait 15s` ends at the
app's first update or when those handlers return: not a pause, only `sleep 15s` pauses. A bare number is milliseconds. Each run is a new
client (new ids, empty `ClientReactive` state) that ends with the run, and so does what the app keeps only while a client is connected (a room): a script names controls by label
and plays the whole flow in one run. Where the shell strips quotes (PowerShell), use
`--steps-file scratch/steps.txt` (one step per line). `hold`, `mic` and `--fake-media-file` (a .wav loops into
the microphone, or plays once with `--mic-once`; a .y4m is the camera) test a voice or camera app.
`look`, `upload`, `mic` and `audio` need the page, so no `--text-only`; on a local run `as bob:`
gets its own page once a step acts in it.

A script worth keeping is a **check gate**, `.ikon/gates/<name>.toml`: `ikon test` runs it against
the local run or the deployed app, and
`ikon task set <task-id> --reproduction <name>` records it as a defect's reproduction.

`--app-id <space>` or `--target <name>` makes `browse` and `test` drive the **deployed** app;
without them they drive the local run, else the linked deployed app. The page signs in as you when
the app offers the method you used for `ikon login` (or email, for your organisation's app); `--as-guest` opens it as a new guest, and on a
local run `--as-user <name>` acts as a named test user (`--role`), and `as <name>+<role>:` as another up to the
next `as` (`as me:` is the run's own). On a deployed app `as <name>:` is the named guest
`--guest-name <name>` is. When
a surface lists "global", every guest is one shared user, which a deployed app's page cannot
join: use `--text-only` there, a local run for `look`, or list "guest". `click <name>` is a real mouse click.

`ikon link --target staging` makes a staging app; `--target staging` aims `deploy`, `browse`,
`test` and `logs` at it. `ikon deployment rollback` undoes it.

## The ikon tool's commands

`ikon help --json` covers every command, how the app and environment are chosen, and
the shape of `--format json` output (`ikon help <command> --json`: one). An unknown command a doc names
was renamed. `ikon deploy` commits and pushes first (`--no-save`: neither). Settings are in `ikon-config.toml`,
each explained by the comment above it: `ikon-config.development.toml` for an app on the development
platform (`run` and `deploy` read that one file), `ikon-config.<name>.toml` for `--target <name>`.
On Windows, read and edit files with your file tools: PowerShell 5.1's `cat`/`Set-Content` garble non-ASCII text.

Root folders (`app-structure`): `app/`, `frontend-node/`, `frontend-flutter/`, `data/`,
`public/`, `branding/` (replace the placeholder `logo.png`, then `ikon icon`), `emails/`, `legal/`, `router/`, `schema/`, `tools/<runtime>/` (programs the
app runs; source in `tools/source/`), `container/`, `devices/` (programs on other
machines), `scripts/` (scripts the app keeps), `scratch/` (git-ignored steps files and throwaway
scripts, not `/tmp`), `reference/`, `test/`, `release-notes/`, `.ikon/gates/`, generated
`build/` and `docs/`.

## What the app must contain

Build what the KIND of product's ordinary workflow needs from the first build, nothing more (records get search, sort, filters, export).
Every control works or is disabled with a visible reason; changes, AI-made too, can be undone; a filter never loses records; and anything reaching outside the app (`app.Payments`, `app.Email`, `app.Telephony`, `app.Notifications`, a publish, a permanent delete) happens only when a person presses a control or turns on a setting naming it, never as a side effect of an AI turn.

## How the app must look

Before the first `view.*` call pick a visual direction in one sentence (feel, palette, type, radius, density) and set it with `new IkonTheme { … }`, `["primary"]` included (IKON015).
Then: one primary action per screen, brand colour; `Card.Elevated` for what matters and one flat colour per card (IKON011); padding in a max-width `Layout.Page`; `view.Card` pads itself: no `p-*` on it or its only child (IKON026). Outside `view.Card`, a `Column` with a `Card.*` token pads nothing and needs `p-5` (IKON034);
empty (`view.EmptyState`), loading and error states; action buttons beside a title in `Layout.Row.Actions`; every viewport: an `h-screen` root, a `ScrollArea` in a `flex-1 min-h-0` parent, touch targets ≥ 44px; tabs: `view.Tabs` (a `defaultValue`), `Tabs.NavList`, `triggerStyle: [Tabs.NavTriggerMd]` (IKON039).

## Common Pitfalls

The `pitfalls` guide has each rule in full, with the rarer ones; read its section for an area first.

- **State**: reactives are `private readonly` fields built with their initial value, `private readonly ClientReactive<int> _count = new(0);`, no `app` argument (CS1503/CS7036). Lists are `ReactiveList<T>`/`ReactiveDictionary`/`ReactiveHashSet` (`Reactive<List<T>>` is IKON002), changed on the reactive (`_todos.Add(x)`; `.Value.Add` is CS1061). A render reading `_x.Value` reruns on change: there is no `view.Dynamic`/`Watch`/`Bind`/`Keyed` (CS1061). A render only reads (IKON031). Outside a handler, name the client: `_x.SetFor(cid, v)`.
- **Emerge** is static: `await Emerge.Run<T>(LLMModel.Claude55Sonnet, pass => { pass.Command = prompt; })` (positional prompt: CS1660); `Emerge.AskAsync(prompt, model)` is prompt-first (CS1503). AI calls throw: catch them and show a sentence and a retry.
- **View calls**: the leading style array is positional and everything else is named, lambdas too (CS1744, CS8323, IKON003). The display string is `text:` (`Markdown`: `content:`); Dialog and Alert take `title:`/`description:`. Form controls take `bind:` or `value:` + `onValueChange:` (a lone `value:` is read-only, IKON027). Only `Box`, `Button`, `Card`, `Image`, `Link`, `TableRow` and `ToolbarButton` take `onClick:`.
  Buttons are one token in an array, `[Button.PrimaryMd]`, with no `variant:`/`size:`; one with `disabled:` needs a `disabled:` class too (`[Button.PrimaryMd, "disabled:opacity-50"]`, `ui-components`).
  Tones: `SemanticTone.Neutral/Brand/Success/Warning/Error/Info`.
- **Colours**: `bg-primary` is the page surface, not the brand: brand fill is `bg-brand-solid`, brand text `text-brand-secondary`. Crosswind classes in a `.tsx` file do nothing.
- **Structure**: keep the scaffold's `[App]` class, its two records, the top-level `return await App.Run(args);` (CS5001) and the global namespace (CS0234/CS8956). Log via `Log.Instance`; no invented `using Ikon.*;` (CS0234); `QueryAsync` (CS1061) needs `dotnet add app/<Project> package Dapper --version 2.1.89` (the build adds the `global using`); a `snake_case` column needs an alias (`client_id AS ClientId`). Open a connection per operation. Leave the `__IKON_APP_NAME__` marker in `index.html` and the manifest: the build writes `[Target] Name` there.

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
- **reactive-state**: shared, per-client, per-user and persistent state; scopes
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
