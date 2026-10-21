<!-- This file is automatically updated by ikon tool commands. User edits are preserved only below the ikon-user-content-below marker. -->

# IKON App Development Guidelines

## Quick start

1. Converting an existing app? Its screens become C#: read the `convert` playbook first. Else read `docs/Ikon.Agent.Docs/patterns/_start.md` (a row per kind of app) and the patterns your row names; `ikon docs --search <feature>` finds any other feature.
2. Pick a visual direction (below) and who shares an instance (`app-structure`): the scaffold's `SessionIdentity(string? UserId)` gives each user their own, `SessionIdentity()` one for all, `SessionIdentity(string? Room)` one per `?room=`, 200 clients each (more: `[Sharded]`). Private app (`ikon new --private`): no `guest` or `global` in `[Surface.Auth] Methods`; any account signs in; staff-only screens check a role, and no one holds `admin` by default (`ikon profile role add admin --me`, or `first-user-becomes-admin`).
3. Each instance holds its own copy of every reactive, `Persistent*` too; endpoints and `[Cron]` run in the empty-identity one. Shared data goes in a database, or use `SessionIdentity()`, which shares every plain reactive (per-visitor ones: `Client*`/`User*`).
4. A local run reads and writes the deployed app's databases, files and persistent state. Test on a staging copy (its own data, the only one `app.Seed` runs on): `ikon link --target staging --new-app`. Set `[[Surface]]` before linking: staging takes it then; later changes go in every `ikon-config*.toml`.
5. The scaffold runs as is: `ikon run --target staging --mock-ai --detach` compiles it, then `ikon browse --steps "observe"` ("Running and checking the app"). `--mock-ai`: speech recognition gives one fixed sentence (browse speech needs `--fake-media-file x.wav`), `Emerge.Run<T>` gets one item per array (`ai-models`), no tool is called unless `--mock-ai-script turns.json` scripts it; a deployed copy spends credits. `browse` without `--target` drives the local run. Fix every warning in `build/ikon-run.log`. `ikon feedback set --audience everyone` shows the feedback button.

## Architecture

An IKON app is a C# app in the cloud that streams reactive UI to browsers; an instance keeps running when every client disconnects. `frontend-node` is only for custom React components (`frontend-fundamentals`). Packages: `dotnet add app/<Project>/<Project>.csproj package <Id>`; `npm install` in `frontend-node`.

A deployed instance has 1 CPU and 512 MB; its image has .NET and `ffmpeg`, no Python or extra fonts (`app-container`). Its databases and files stay in the EU (AI: `region-pinned-ai` pattern); music, video, segmentation, depth and mesh models are global only (`ikon model list --eu`).

**AI services** (platform-held keys, except Rodin's): `Emerge.Run<T>()` for LLM text and typed output, `Emerge.Refine` for a draft improved in rounds; `ImageGenerator`, `SpeechGenerator`, `SpeechRecognizer`, `VideoGenerator`, `MusicGenerator`, `EmbeddingGenerator`, `WebSearcher`, `Classifier` (moderation, free), `DepthEstimator`, `ImageSegmenter`, `VideoSegmenter`, `MeshGenerator`. Each has a static one-shot (`ImageGenerator.GenerateAsync(prompt)`, bytes: `GetDataAsync()`) and `new Xxx(model)` with `XxxConfig` for options (the `ai-*` guides).

**UI** is declared in C# inside `UI.Root([Page.Default], content: view => { ... })`. Components are methods on `view` (`UIView`, alias `IView`; no `ViewContext`, CS0246); nested lambdas get their own.

**Styling:** Crosswind, IKON's Tailwind superset in C# string arrays.
Use **semantic theme classes** and the **`Ikon.Parallax.Theming` tokens** built from them (`Button.PrimaryMd`, `Text.H1`; `styling-and-motion` lists them), in one array: `[Button.PrimaryMd, "mt-4"]`. Fixed palette classes (`bg-amber-400`) are for looks that ignore the theme; a missing colour is a theme role (IKON013).

Animations are keyframe steps `time:utilities` joined by `_`: `"motion-[0:opacity-0_translate-y-[8px],100:opacity-100_translate-y-0] motion-duration-300ms"`. There are no named animations and no `motion-preset-*` (IKON022).

**A `style:` array is exactly what renders.** With none a component gets its themed default; with one, only those classes (`view.Button(["px-6"], …)` is a bare button; layout-only: IKON025). `"default"` first keeps the themed default under yours: `view.Button(["default", "px-6"], …)`. Token composites (`Button.OutlineMd`) are complete: add classes after, never `"default"` before. `view.Card` pads `content:` (style padding doubles it, IKON026; `contentStyle: ["p-0"]` for edge to edge); a `Card.*` token on a Column adds no padding (IKON034).

**Theming:** `new IkonTheme { ["primary"] = "amber-400", ["background"] = "zinc-950", … }`. The only named properties are `Mode`, `DarkMode` (or `Dark`) and `Light`. Status colours other than `["destructive"]` are not keys (`theming`).

## Running and checking the app

From the app root, `ikon run --detach` starts the app, returns when ready; a plain
`ikon run` blocks. `build/app/artifacts/bin/<App>/debug/ikon-server-info.json`
has the URLs. `ikon stop` saves persistent state and ends the run. A `.cs` edit under `app/<App>/` hot-reloads
the run (a compile error keeps the previous build, an `OnStarting` that throws leaves
none, and `browse` prints the error;
`ikon status --wait` returns once the reload has applied or failed); a `.csproj`
or `ikon-config.toml` change needs `ikon stop` and a new run. Check the app as text first (`--text-only` needs no browser, runs anywhere):

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
gets a page of its own, signed in as bob.

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

Name the KIND of product and build what its ordinary workflow needs from the first build, nothing unrequested: the `product-design` guide lists it per kind (records get search, sort, filters and export).
Every visible control works or is disabled with a visible reason; changes, AI-made too, can be undone; a filter never loses records; and anything reaching outside the app (`app.Payments`, `app.Email`, `app.Telephony`, `app.Notifications`, a publish, a permanent delete) happens only when a person presses a control or turns on a setting naming it, never as a side effect of an AI turn.

## How the app must look

Before the first `view.*` call pick a visual direction in one sentence (feel, palette, type, radius, density) and set it with `new IkonTheme { … }`, `["primary"]` included (IKON015).
Then: one primary action per screen in the brand colour; `Card.Elevated` for what matters and one flat colour per card (IKON011); padding in a max-width `Layout.Page`; empty, loading and error states; every viewport: an `h-screen` root, a `ScrollArea` in a `flex-1 min-h-0` parent, touch targets ≥ 44px; tabs are `view.Tabs` (always a `defaultValue`)+`Tabs.NavList`, not a `flex-wrap` button row.

## Common Pitfalls

1. Collection state uses the reactive collections: `ReactiveList<T>`, `ReactiveDictionary<TKey,TValue>`, `ReactiveHashSet<T>` (also prefixed `Client`/`User`/`Mount`/`Persistent`/`PersistentUser`). A `Reactive<List<T>>` is IKON002.
2. There is NO `view.Keyed(...)` (CS1061). Render list items directly in the loop; `key:` is a named parameter, needed only where a stateful component must keep or drop its state.

### Reactive state

More in `reactive-state` ("Rules that are easy to get wrong").

- **Re-render is implicit.** Reading `_x.Value` in a UI lambda registers the dependency, and only the diff is streamed. There is NO `view.Dynamic`/`Watch`/`Bind`/`Observe` (CS1061).
- **Reactives are `private readonly` fields constructed with the initial value**: `private readonly ClientReactive<int> _count = new(0);`, a nullable one `new(null)`, collections `= new();`. No `app` argument, no `app.ClientReactive(...)` (CS1503/CS7036). A `ClientReactive` of a mutable type needs `ClientReactive.Create(_ => new Settings())`.
- **Mutate collections on the reactive** (`_todos.Add(item)`, `.RemoveAll(match)`, `.ReplaceWhere(match, t => t with { … })`, `.ReplaceAll(items)`, `.Update(list => …)` that returns new items or edits the copy, `_map[key] = v`) and enumerate it directly. `.Value` is read-only (`.Value.Add` is CS1061).
- **Scope by lifetime**: `Reactive<T>` is shared by its instance's clients, readable by any (no personal records); view state (open screen or dialog, in-flight flag) is `ClientReactive`, per browser tab even in a per-user instance; typed text, drafts, a pending choice and the open record `UserReactive` (a reload is a new client). `Persistent*` is stored once per app, `PersistentSession*` per `SessionIdentity`, `PersistentUser*` per user; they autosave in seconds (await `app.SavePersistentStateAsync()` before "Saved"); records that can't be lost go in a database.
- **`_x.Value` is the current client's** in a UI lambda or handler. Outside a scope (`Main`, a timer, an endpoint) name the target: `_x.SetFor(cid, value)` (list: `AddFor`, `ReplaceWhereFor`), `_x.ValueFor(cid)` or `ReactiveScope.Use(new ClientScope(cid))`; `cid` is `ReactiveScope.ClientId` captured in a scope, and a `User*` one takes the user id string.
- The user is `ReactiveScope.UserId` (`UserIdOrNull`), not `app.SessionIdentity.UserId`; `app.CurrentClientContext` (nullable; no `Context.Current`) has `IsAnonymous` and `Timezone`, no email or name: use `await new ClientProfiles(app).GetProfileAsync(ctx)`.
- **Render only reads.** A reactive written inside a render lambda, or by work it starts, is not propagated (IKON031): load a screen's data in the handler that opens it (`reactive-state`).

### AI services

- **`Emerge` is static, and `await Emerge.Run<T>(…)` returns a non-null `T`** (no `IEmerge`): `var result = await Emerge.Run<MyResult>(LLMModel.Claude55Sonnet, pass => { pass.Command = userPrompt; });`. The prompt goes in `pass.Command` (CS1660 if positional). Images, PDFs and prior turns go in a `KernelContext`, a struct.
- **`Emerge.AskAsync` is prompt-first**: `AskAsync(userPrompt)` or `AskAsync(userPrompt, LLMModel.Claude45Haiku)` (model-first is CS1503). It returns a `string`; `AskAsync<T>(prompt)` a `T` (a `public record`/`class`, never a `struct`).
- **Models.** `LLMModel.Default` is `Claude45Haiku`, the one-shots' model. For reasoning and chat name `Claude55Sonnet`, for the hardest tasks `Claude55Opus` (`ai-models`).
- **AI calls never return null — catch instead of guarding.** The services throw `AIException` (`InsufficientCreditsException` when credits run out), Emerge any exception (`EmergenceStoppedException` for no result): `catch (AIException)` around the services, `catch (Exception)` around Emerge.
- **The catch shows a human sentence and a retry, never the exception**.
- **Disclose AI** with `view.AiDisclosure(AiDisclosureKind.Interaction)` where an AI conversation starts, and `kind: AiDisclosureKind.GeneratedContent` in its card, beside what a model made. Show model text with `view.Markdown`.

### The view-call shape

More in `ui-components` ("Call-shape details").

- **The leading `string[]` style array is positional; name everything else.** `view.Column(["flex gap-4"], content: v => …)`, `view.Button(["px-4"], text: "Save", onClick: async () => …)`. Not also `style:` (CS1744), no named arg before a positional (CS8323); `VirtualList`/`VirtualGrid` take `itemCount` first, theirs is `style:`. Lambdas are named (IKON003).
- **The display string is `text:`** on Text, Button, Link and ActionButton (not `value:`/`label:`); `view.Markdown("…")` takes it positional.
- **Form controls take `bind:`** (TextField/TextArea/Select/RadioGroup a `Reactive<string>`, Checkbox/Switch `Reactive<bool>`, Slider `Reactive<double>`) or `value:` + `onValueChange:` (Slider's take a list); a `value:` alone is read-only (IKON027). `view.Select` takes `options:` of `new SelectOption(value, label)`; `view.RadioGroup` takes `content:` with a `RadioGroupItem(value:, label:)` per option.
- **Handlers.** `onSubmit:` is `Func<string, Task>`, always `async`; `onClick:` takes any parameterless lambda. Only `Box`, `Button`, `Card`, `Image`, `Link`, `TableRow` (in a `Table`) and `ToolbarButton` take `onClick:` (charts one with `ChartClickArgs`); a clickable list row is `view.Box(["flex flex-row …"], onClick: …)`. `onOpenChange` is `Func<bool, Task>`: `async open => …`.
- **`ariaLabel:`** (on controls, `Box` and `Icon`; not `Text`, `Badge` or `RadioGroup`, whose `label:` also names it; CS1739) replaces the visible text for screen readers and `ikon browse`: set it only where there is none, or start it with that text (`$"Delete {item.Name}"` on a per-item button; IKON028).
- **The style array is classes, not CSS**; there is no `inlineStyle:`. A runtime value is an arbitrary value: `$"w-[{pct:0.#}%]"`.
- **Never name an app type, method or property like a style class** (`Page`, `Text`, `Card`; IKON030): it hides the `Theming` class (CS0117/CS0119/CS1929).
- **Dialog, AlertDialog and Alert** take `title:`/`description:`/`content:`, not `text:`. A field's `label:` is a VISIBLE label above it (with none, give `ariaLabel:` or `placeholder:`; IKON033); on Checkbox/Switch/Toggle it is the clickable trailing text.
- **Buttons bake variant and size into one constant** (`Button.PrimaryMd`, `Button.ErrorMd`, `Button.OutlineErrorMd`); no `Button.Danger*`, no `variant:`/`size:`. Even one token goes in an array: `[Button.PrimaryMd]`.
- **There is no** `Text.Xs`, `Tabs.Default` (`Tabs.List`/`Trigger`/`Content`), `Destructive` or `AlertStatus` (`tone: SemanticTone.Error`/`Warning`/`Info`/`Success` on Alert and Badge), `text:` on `ToggleGroupItem` (a `Text` in `content:`) or `title:` on `StatCard` (`label:`).
- **Copy, share and download are `view.ActionButton`** with `options:` `new CopyToClipboardActionOptions { Text = … }`, `ShareActionOptions` or `DownloadFileActionOptions`. There is no `ClientFunctions.CopyToClipboardAsync`/`RunJavaScriptAsync`. The app's link is `app.PublicUrl` (no `app.Origin`), with a query `app.JoinUrl(new { room })`.

### Custom frontend and real-time

- **Native `view.*` first** (`view.RichTextEditor`). For a highlighting editor, a map, a 3D view, a chart library or a signature pad, follow the `custom-react-node-embed` pattern (`leaflet-map`: a map) and write all four of its parts at once. A floor plan or big picture with markers is `view.PanZoom`.
- **Crosswind classes in a `.tsx` file do nothing**; they exist only in C# style arrays. Style a custom component's elements with inline `style={{ … }}`, an imported CSS file or `var(--ikon-…)`, and its root from C#.
- **Blank page after a clean build**: give the root `h-screen`, paint on `bg-background`/`bg-card` under a committed theme, and if a red "Unregistered node type" shows, register the custom module.
- **Real-time MULTI-USER surfaces** (whiteboard, per-frame game; turn-based is `multi-user-game`) are a custom canvas plus a `schema/<Name>.tp` message: `view.*` has no client-to-client channel (`realtime-game-loop` pattern). Single-user drawing is `view.ImageEditorCanvas`.

### Audio, files, media

- **Audio is mixed SERVER-side** and played through one `private Audio Audio { get; } = new(app);` (`Play`, `Speak`, `PlayLive`, microphones), never browser Web Audio. **Video** (cameras, screens) is one `private Video Video { get; } = new(app);` playing on named surfaces: clients render `view.VideoSurface(surface: "stage")` (`audio-video`).
- **A `FileUpload` that keeps the file** puts it in asset storage: `onUploadStart: async args => new FileUploadResult { AssetUri = new AssetUri(AssetClass.CloudFile, path, app.GlobalState.SpaceId) }`, then `onUploadComplete` gets `args.AssetUri`; a small file read once needs no `onUploadStart` and is at `args.LocalTempFilePath` (`media-analysis`).
- **Never copy video or audio into the container**: `ffprobe`/`ffmpeg` read `(await Asset.Instance.GetMetadataAsync(uri)).Url` over HTTP.
- **App files: root `public/` (served by path, `src: "/hero.png"`) and `data/` (private, no URL), `app.Files.Public` and `app.Files.Data` at runtime**; a written file's `src` comes from `GetUrlAsync(path)`, never from its path. A private image: `private-generated-media` pattern. Never read through `AppContext.BaseDirectory` (`app-structure`).

### App structure and platform

- **Keep the scaffold's shape**: `[App] public partial class MyApp(IApp<SessionIdentity, ClientParameters> app)` and its two records; `app/` holds one `.csproj`.
- **`Main()` returns after `UI.Root(...)`**: a Main that never returns (`await Task.Delay(Timeout.Infinite)`) fails the boot.
- **Lifecycle**, wired in `Main()`: `app.OnStarting(async () => …)`, `app.OnStopping`, `app.OnClientJoined(async ctx => …)`, `app.OnClientLeft`, `app.OnMessageReceived(async msg => …)`, `app.Navigation.PathChangedAsync += async e => …` (`e.Path`), not `(sender, args)` (CS1593).
- **A hot reload starts a new instance** and runs `Main`/`OnStarting` again: reactives are restored, plain fields (engines, caches, counters) are not. In `OnStarting` rebuild them and settle restored in-flight state. End loops on `app.StoppingToken` (`app-structure`).
- **Test data goes in `app.Seed`** (staging only, Quick start 4); rows the live app needs, in `app.OnStarting` when the table is empty. `ikon deploy`/`save` commit every change in the folder; `bundle`/`deploy` run `Main`, `OnStarting` and a capture client's `OnClientJoined` against the real databases for a boot snapshot: guard side effects with `SnapshotCapture.IsCaptureProcess`, and the capture client's with `ctx.IsSnapshot`.
- **Wrap every `_ = Task.Run` body in try/catch with `Log.Instance.Error`**.
- **A value presented as unique is GENERATED** (codes, ids) from `Random.Shared` or `Guid`.
- **Cloud servers run in UTC**: store UTC and show times in the viewer's `ctx.Timezone` (`app-structure`).
- **Log with `Log.Instance.Info/Warning/Error/Debug(…)`**, not `app.Logger`.
- **A "Sign in" button drawn in C#** calls `ClientFunctions.LoginShowAsync(reason)`; `LoginAsync(provider)` is OAuth only.
- **Open a database connection PER operation**: `await using var conn = await app.DatabaseAsync("mydb"); await conn.OpenAsync();` in each method, never a field (`databases`). `QueryAsync`/`ExecuteAsync` (CS1061) need package `Dapper` and `global using Dapper;` in `GlobalUsings.cs`. A Dapper record needs the exact column types, in the constructor's order and count (name columns): `timestamptz` is `DateTime`, `count(*)` `long`; an array column (`text[]`) is `System.Array`: use a class with settable properties.
- **Don't invent a `using Ikon.*;`** (CS0234), but a parent does not import its nested namespaces (CS0246). Outside `GlobalUsings.cs`: `Ikon.Common.Core.Protocol` (`TriggerEventType`, `VideoCodec`; `Action` clashes: CS0104), `Ikon.App.Cells`, `Ikon.App.Mcp`, `Ikon.App.Triggers`, `Ikon.AI.Retrieving`, `Ikon.AI.Reranking`, `Ikon.AI.Database`, `Ikon.AI.Storage`, `Ikon.AI.Emergence.Structured`, `Ikon.AI.Emergence.Tree`, `Ikon.Crosswind`, `System.Globalization`, `System.Diagnostics`.
- **A real `Ikon.*` namespace failing with CS0234 needs its package**: line 2 of its `api/<namespace>.md` names it.
- **EF Core**: CS1061/CS0246/CS0411 on `MigrateAsync`, `ToListAsync` are its missing usings (`databases`).
- **Keep the entry point and the global namespace.** Never delete the top-level `return await App.Run(args);` (CS5001) or move it to a `Program.cs`. Never add a `namespace` to app files: inside `namespace Ikon.App.<Anything>`, `App` is the `Ikon.App` namespace (CS0234/CS0305/CS8956).

### Color tokens and theming

- **`bg-primary` is NOT the brand fill** but the PAGE SURFACE (white in light, near-black in dark); the theme key `["primary"]` sets the brand colour that the `brand` classes use. Brand fill is `bg-brand-solid`, brand text `text-brand-secondary`, brand border `border-brand`; neutrals are `bg-background`/`bg-card`, `text-foreground`, `border-secondary`.
- **A hardcoded palette, or an always-dark app, needs `Mode = ThemeMode.Fixed`** (`Adaptive` or `Fixed`; no `Dark`/`Auto`); for both schemes set `DarkMode`.
- **Text colour goes on the Text**, after its token: `[Text.Body, "text-white"]`. Only the role tokens take a numeric shade (`brand-100`, `error-700`); `text-tertiary-600` is IKON006.
- **Tailwind class names are v4's** (`shadow-xs`, `outline-hidden`; `styling-and-motion`).

### C# syntax

More in `csharp-primer` (CS8917, CS9176, raw strings).

- **Escape inner quotes in `$"…"`** (`$"Add card to \"{col}\""`): one unescaped `"` cascades into CS1002/CS1010/CS1026 across the file; fix that quote, not the lines the errors point at. Many quotes → `$"""…"""`.

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

The Validation app uses as much of the app-facing platform API as it can, so that a platform change
that breaks an app fails this app's build, and `validation/validate.py platform-current` then drives
it end to end so each feature is proven to work, not only to compile. Runtime coverage lives in the
tab files. The guides' code examples live in the Patterns app, not here.

A new app-facing feature belongs here in a tab, with a `data-testid` on its button and on a result
line that starts with `PASS` or `FAIL`, and a step in `validation/validate_platform_features.py`.

### How each tab is proven

| Tier | When | What |
|---|---|---|
| free | hourly | every tab rendered by the smoke sweep; Self-Test's in-process battery; the one-click checks on the Typography, Buttons, Inputs, Advanced Inputs, Forms, Overlays, Navigation, Layout, Drawing, Pan & Zoom, Actions, Device, Notifications, Session Identity, Account (the legal page links, reading the account's scheduled removal), Consent (Require gating), React SDK (server functions from the browser, consent both ways between C# and the hook, sibling client), Costs and Telephony (status) tabs; the older phases (assets, files, the upload lifecycle lab, database, custom messages, media, MCP, cron, payments, connection transports, SEO) |
| ai | six-hourly | the Ikon.AI Library cards, including Decider, direct model access, the mesh animation library, word timings and provenance; a spend budget tripping |
| daily | the 00:00 UTC six-hourly run, or a dispatch with `run_daily` | an SMS and a call each way between the space's two numbers, a signature order reaching the Signicat ceremony, SAM 3 video segmentation |
| manual | a person, often on a phone | Motion, Recordings and Live Activity (native clients only), background location, completing a signature, answering calls, the capture hooks, account deletion, browsing and changing a SharePoint site on the SharePoint tab, signing in to Google and working a Drive on the Google Drive tab, the upload lab's app stop mid-upload, the upload lab through the Dart client (`platform-dart/ikon_sdk/test/upload_lab_live_test.dart` against a local run) |

The Files tab's upload lifecycle lab runs the same scenarios hosted and locally, with one difference:
a hosted app's LocalFile store is its read-only bundle. There the scenarios that need a writable asset
(the refusals, hash-mismatch, app-cancel, client-abandon, replace, complete-throws) write to CloudFile,
`dest-local` reports `SKIP` (the validation script counts `SKIP` as not failed), and `zero` passes on
its temp-file half and names its LocalFile half as skipped; complete-throws expects a new CloudFile
asset to be deleted, which needs a backend that reports `created`; a local run uses LocalFile, and CloudFile beside it in the scenarios that cover both. Each
result line names the store it used. `resume` drops its session's connection on purpose, so the quiet
check accepts the web SDK's reconnect warning from the files page during the uploads phase, and only
there.

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
