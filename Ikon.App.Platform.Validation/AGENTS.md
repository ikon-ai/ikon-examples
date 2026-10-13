<!-- This file is automatically updated by ikon tool commands. User edits are preserved only below the ikon-user-content-below marker. -->

# IKON App Development Guidelines

**Read docs/Ikon.Agent.Docs/: its agent guides (guides/, indexed at the bottom) include most library guides in the other docs/ folders.**

## Quick start

1. The scaffold already runs: the `[App]` class in `app/<Project>/` declares its UI in `Main()` with `UI.Root(...)`.
2. Name the kind of product and a visual direction ("What the app must contain", "How the app must look"); set an `IkonTheme` in the constructor.
3. Before writing a screen, adapt the closest pattern from `docs/Ikon.Agent.Docs/patterns/_index.md` and read the agent guide for each service it uses (table at the bottom).
4. Check "Common Pitfalls" before the first build.
5. `ikon run --detach`, then `ikon browse --steps "observe"` to check the app as text ("Running and checking the app").

## Architecture

An IKON app is a C# app that runs in the cloud and streams reactive UI to browser clients over WebSocket. Many clients share one app instance, which keeps running when every client disconnects. Nearly all development, styling included, happens in the C# app; touch `frontend-node` only to add custom React components (`frontend-fundamentals` guide). Any NuGet or npm package may be added.

`ikon new` creates a cloud app beside the project, and a local run uses it too: AI calls are billed to that app's space. `ikon new --local` skips it, for debugging only; such an app cannot run until `ikon link`. A deployed instance has 1 CPU and 512 MB of memory by default, of which the app uses about 300 MB (`media-analysis` guide).

**AI services** — the platform holds every API key; no setup: `Emerge.Run<T>()` for LLM text and structured output; `ImageGenerator.GenerateAsync(prompt)`, `SpeechGenerator.GenerateAsync(text)`, `SpeechRecognizer.RecognizeAsync(samples, sampleRate)`, `VideoGenerator.GenerateAsync(prompt)`, `MusicGenerator.GenerateAsync(prompt)`, `EmbeddingGenerator.EmbedAsync(texts)`, `WebSearcher.SearchAsync(query)`, `Classifier.ClassifyAsync(text)` (moderation, free); `DepthEstimator`, `ImageSegmenter`, `MeshGenerator`.
Each static one-shot runs a default model; `new Xxx(model)` with its `XxxConfig` gives the advanced options (the `ai-*` guides).

**UI:** declared in C# inside `UI.Root([Page.Default], content: view => { ... })`. Components are methods on `view` (`UIView`; `IView` is a global alias for it — there is no `ViewContext`, CS0246). Nested content lambdas get their own `UIView view`.

**State:** `Reactive<T>` (shared by all clients), `ClientReactive<T>` (per client), `UserReactive<T>` (per user); collections are `ReactiveList<T>` and friends. Changes re-render automatically and only the diff is streamed.

**Styling:** Crosswind — Tailwind v4 classes in C# string arrays. Three layers compose in one array (`[Button.PrimaryMd, "mt-4 self-center"]`):

1. **Semantic theme classes** (`bg-card`, `text-foreground`, `bg-brand-solid`, `border-secondary`, `bg-background`) — the default. They follow light/dark and the app's `IkonTheme`.
2. **`Ikon.Parallax.Theming` tokens** (`Button.PrimaryMd`, `Card.Default`, `Text.H1`, `Layout.Page`, …) — pre-composed bundles of layer 1.
3. **Fixed palette classes** (`bg-amber-400`, `text-zinc-950`) — only for looks that deliberately ignore the theme. A colour the palette lacks goes in the theme as a role, not as a hex class (IKON013).

Animations are keyframe steps, each `time:utilities` with the utilities joined by `_`: `"motion-[0:opacity-0_translate-y-[8px],100:opacity-100_translate-y-0] motion-duration-300ms"`. There are no named animations and no `motion-preset-*` (IKON022).

**A `style:` array is exactly what renders.** With no array a component gets its themed default (`view.Button` → `Button.Default`); with one, only those classes — `view.Button(["px-6"], …)` is a bare button. Put `"default"` first to keep the themed default under your classes: `view.Button(["default", "px-6"], …)`. Token composites (`Button.OutlineMd`, `Card.Interactive`) are complete styles: pass them as-is, add classes after, never `"default"` before one.
Slot styles (`contentStyle:`, `titleStyle:`, …) follow the same rule. `view.Card` pads its `content:`; `contentStyle: ["p-0"]` makes it edge to edge.

**Theming:** `new IkonTheme { ["primary"] = "amber-400", ["background"] = "zinc-950", … }` at the `UI` initializer. The only named properties are `Mode`, `DarkMode` (or `Dark`) and `Light`. The one status alias is `["destructive"]`: `success`, `warning`, `info`, `error` and `accent` are not keys (they set a variable nothing reads), so set a scale step (`["success-500"]`) or a variable by name (`theming` guide).

**Audio and video:** `private Audio Audio { get; } = new(app);` — synthesis, effects, playback, microphone capture. Cameras, screens and live video are `private Video Video { get; } = new(app);`. Hold one of each per app.

## Running and checking the app

Run it from the app root with `ikon run` (`--log-debug` for the server's own detail). It prints a
`Ready:` line with the app's address once the app is up, and writes the URLs to
`build/app/artifacts/bin/<App>/debug/ikon-server-info.json`. `ikon run` does not return while the app
runs, so where a shell kills long commands use `ikon run --detach`, which returns once the app is
ready; `ikon status --wait` waits for a run started some other way. `ikon stop` ends the run, and the
app saves its persistent state before it exits. When the run says browsers mark it not secure, run
`ikon install certificate` or ask the person to, and tell them to approve the dialog it opens. Then
check the app as text before reaching for a browser:

```bash
ikon browse --steps "observe"                                   # screen text + [id] action handles
ikon browse --steps "fill Message hello; tap Send; expect Sent; look after.png"
ikon browse --text-only --format json < steps.txt               # one step per line, one JSON object per step
```

`ikon browse --help` lists every step and option. Steps that act print only what changed, and
`click`, `eval` and `console` reach the DOM of custom React components. An observation costs a few
hundred tokens where a screenshot costs thousands, so read the text first and `look` only when the
question is about pixels. `look` screenshots a browser that has joined the same client session.
Each run is a new client session with new ids, so a saved script names controls by label. Where the
shell strips quotes (Windows PowerShell), put the steps in a file, one per line:
`--steps-file steps.txt`. `hold`, `audio` and fake media files test a voice or camera app without
a person.

Save a script worth keeping as a **check gate**, `.ikon/gates/<name>.toml`. `ikon test` runs gates
against the local run or, with `--target`/`--app-id`, the deployed app; `ikon help test --json`
lists the file's keys. `ikon task set <task-id> --reproduction <name>` records the gate that shows a
defect as the task's reproduction.

With `--app-id <space>` or `--target <name>`, `browse` and `test` drive the **deployed** app, signed
in as you. Without them, they drive the local run when one is up, else the linked deployed app. The
page is signed in as you too when the app offers the method you used for `ikon login`; otherwise
browse says why. `--as-guest` opens the app as a new guest each run. An app whose `[Auth] Methods`
list "global" signs every guest in as one shared user, which a deployed app's page cannot join, so
use `--text-only` there or list "guest" instead. An app that lets no guests in refuses it.
`click <name>` is a real mouse click; audio in the page plays without one, since the browser needs no user gesture.

## The ikon tool's commands

This part of AGENTS.md comes from the ikon tool and is rewritten whenever the tool or the app's
IKON libraries change. The tool's own help is the authority on its commands, ahead of any guide:

- `ikon help <section> --json` — one section's commands with their options, positionals and
  remarks; the sections are Develop, Source, Ship, Services, Operate, Autopilot, Account and Machine
- `ikon help <command> --json` — one command; `ikon <command> --help` prints the same for a person
- `ikon help --json` — every command, and what holds for all of them: how the app and the platform
  environment are chosen, and the shape of `--format json` output and failures

A command a doc names that the tool rejects as unknown has been renamed — look it up there rather
than guessing. Settings are in `ikon-config.toml` (`ikon-config.development.toml` on the
development platform, `ikon-config.<name>.toml` for `--target <name>`), which is what a guide's
`ikon-config.toml` means; the comment above each setting explains it.

The tool uses these top-level folders: `app/`, `frontend-node/`, `frontend-flutter/`, `data/`,
`public/`, `branding/`, `emails/`, `tools/<runtime>/` (programs the app runs), `container/` (the
app's image), and the generated `build/` and `docs/`. Keep your own scripts elsewhere, such as
`scripts/`.

## What the app must contain

Before writing UI, name the KIND of product and build what its ordinary workflow needs:

| The app is… | It needs, from the first build |
|---|---|
| records people accumulate (contacts, invoices, tasks, recipes) | search · sort · filters on the high-value fields · a date range when time changes which records matter · export where taking data out is a real task |
| messaging or comments | reply · copy · edit and delete for the author · a per-message action row that works on touch |
| a board or pipeline | move by drag AND a non-drag path · a card detail |
| a dashboard | the summary numbers first · a chart matched to the question · drill-down to the rows |
| an editor or canvas | undo/redo · select · delete · duplicate · zoom by wheel and pinch where the surface scales |
| media playback | play/pause · seek · elapsed and total time |
| a queue someone reviews | the review verbs — Approve · Reject · Request changes, never a bare Yes/No · the evidence to decide, in one place |
| a builder of multi-step automations | one stated trigger · labelled branches with a default path · per-step test · a run log |

Add nothing else unrequested. Every visible control works or is disabled with a visible reason; changes, AI-made ones included, can be undone; a filter never loses records; and anything reaching outside the app (`app.Payments`, `app.Email`, `app.Telephony`, `app.Notifications`, a publish, a permanent delete) happens only when a person presses a control naming it, never as a side effect of an AI turn. The `product-design` guide has each rule.

## How the app must look

Every app ships polished on the first build, unasked. Before the first `view.*` call pick a visual direction in one sentence — feel, palette, type, radius, density — and set it in the constructor with `new IkonTheme { … }`, `["primary"]` included (IKON015).
Then: one primary action per screen in the brand colour; a real type scale; `Card.Elevated` for what matters and one flat colour per card (IKON011); generous padding inside a max-width `Layout.Page`; a tinted background, not a blank one; motion only where state changes; designed empty, loading and error states (`EmptyState.*`, `Skeleton.*`, `Alert.*`); and every viewport — an `h-screen` root, a `ScrollArea` in a `flex-1 min-h-0` parent, touch targets ≥ 40px (`product-design` guide).

## Common Pitfalls

Each of these, written the wrong way, fails to compile or misbehaves at runtime. The two most common draft compile failures:
1. Collection state uses the reactive collections — `ReactiveList<T>`, `ReactiveDictionary<TKey,TValue>`, `ReactiveHashSet<T>` (+ `Client*`/`User*`/`Mount*`/`Persistent*`/`PersistentUser*` variants). A `Reactive<T>` wrapping a mutable collection (`Reactive<List<T>>`) is IKON002, an error in the codegen build and in-repo apps.
2. There is NO `view.Keyed(...)` (CS1061). Render list items directly in the loop; the diff tracks identity. `key:` is a named parameter on the component, needed only where a stateful component must keep or drop its state.

### Reactive state

The `reactive-state` guide has the rest ("Rules that are easy to get wrong").

- **Re-render is implicit.** Reading `_x.Value` in a UI lambda registers the dependency. There is NO `view.Dynamic`/`Watch`/`Bind`/`Observe`/`When`/`Live`/`Reactive` (CS1061); `UIView` only renders components.
- **Reactives are `private readonly` fields constructed with the initial value**: `private readonly ClientReactive<int> _count = new(0);`, `new("")`, collections `= new();`. No `app` argument, no `app.ClientReactive(...)` (CS1503/CS7036). A `ClientReactive` of a mutable type needs `ClientReactive.Create(_ => new Settings())`, or every client shares one instance.
- **Mutate collections on the reactive itself** — `_todos.Add(item)`, `.Remove`, `.RemoveAll`, `.ReplaceAll`, `.Update(…)`, `_map[key] = v` — and enumerate it directly. `.Value` is read-only (`.Value.Add` is CS1061).
- **Pick the scope by how long the state lives.** View state (an open menu, hover, an in-flight flag) is `ClientReactive`; typed text and drafts are `UserReactive`, because a reload is a new client. `Persistent*` is ONE value for the whole app, `PersistentSession*` one per `SessionIdentity`, `PersistentUser*` one per user; they save only when an instance stops cleanly, so records go in a database.
- **Inside a UI lambda or handler, `_x.Value` is the current client's** — no `_x.Get(clientId)`. Outside a scope (a loop started from `Main`, a timer, an endpoint handler) or for another client, name the target: `_x.SetFor(clientSessionId, value)` / `_x.ValueFor(clientSessionId)`, or `using (ReactiveScope.Use(new ClientScope(cid))) { … }`; capture `var cid = ReactiveScope.ClientId;` while the scope exists.
- **Render only reads.** A reactive written inside a render lambda is not propagated; create-if-missing goes in a handler or lifecycle callback.

### AI services

- **`Emerge` is static, and `await Emerge.Run<T>(…)` returns the result** — no `IEmerge`, no `.ResultAsync()`: `var result = await Emerge.Run<MyResult>(LLMModel.Claude55Sonnet, pass => { pass.Command = userPrompt; });`. The prompt goes in `pass.Command`, never positionally (CS1660); never interpolate `pass.JsonSchema`, which is appended for you. The result is a non-null `T`, not a tuple (CS8130). Images, PDFs and prior turns go in a `KernelContext` (`emergence` guide).
- **`Emerge.AskAsync` is prompt-first** — `AskAsync(userPrompt)` or `AskAsync(userPrompt, LLMModel.Claude45Haiku)` (model-first is CS1503). It returns a `string`; `AskAsync<T>(prompt)` returns a `T` (a `public record`/`class`, never a `struct`).
- **Models.** `LLMModel.Default` is `Claude45Haiku`, cheap and fast, and what the one-shots run on. For reasoning and chat name `Claude55Sonnet`, for the hardest tasks `Claude55Opus` (`ai-models` guide).
- **AI calls never return null — catch instead of guarding.** The services throw `AIException`; Emerge throws `EmergenceStoppedException` when a run ends without a result and otherwise rethrows the generation failure. So `catch (AIException)` around the services, `catch (Exception)` around Emerge.
- **The catch renders a human sentence and a retry, never the exception**: `"Couldn't create the image — try again."`, not `ex.Message` or a model id. Keep the prompt and inputs so Retry is one press. Every surface showing a generated result has three states: waiting, failed-with-retry, empty.
- **Disclose AI** with `view.AiDisclosure(AiDisclosureKind.Interaction)` where a conversation with an AI starts, and `kind: AiDisclosureKind.GeneratedContent` beside what a model made.

### The view-call shape

The `ui-components` guide has the rest ("Call-shape details").

- **The leading `string[]` style array is positional; name everything else.** `view.Column(["flex gap-4"], content: v => …)`, `view.Text(["text-lg"], text: "Hi")`, `view.Button(["px-4 py-2"], text: "Save", onClick: async () => …)`. Never also write `style:` (CS1744), never a named arg before a positional (CS8323). Lambdas are always named (IKON003).
- **The display string is `text:`** on Text, Button, Link and ActionButton (not `value:`/`label:`). Content-first overloads also exist: `view.Text("Hi")`, `view.Button("Save", onClick: …)`, `view.Heading("…")`, `view.Markdown("# hi")`, `view.Icon("check")`.
- **Form controls take `bind:`** — TextField/TextArea/Select/RadioGroup a `Reactive<string>`, Checkbox/Switch `Reactive<bool>`, Slider `Reactive<double>` — or `value:` + `onValueChange:`; a `value:` with neither renders read-only. `view.Select` options are `new SelectOption(value, label)`.
- **Slider has two overloads.** On the style-first one `value:` is a LIST of thumbs — `view.Slider([Slider.Default], value: [50.0], onValueChange: async v => …)` (`bind:` lives here). The scalar one is value-first: `view.Slider(50, style: [Slider.Default], onValueChange: async v => …)`.
- **Handlers.** `onSubmit:` is `Func<string, Task>` — always `async`. `onClick:` takes any parameterless lambda. Only `Box`, `Button`, `Card`, `Image`, `Link`, `TableRow` and `ToolbarButton` take `onClick:` (charts take one with `ChartClickArgs`); Text and layout primitives do not. `onOpenChange` hands a plain `bool`.
- **`ariaLabel:`** is on Box, Button, ActionButton, Link, Icon, the form fields and the capture/mic buttons (CaptureButton, PushToTalkButton, MicToggleButton) — give an icon-only control one. Card, Image (it has `alt:`), RadioGroup (its `label:` is the name), TableRow and ToolbarButton have none (CS1739); Card and an unlabelled RadioGroup take `props: new Dictionary<string, object> { ["aria-label"] = "…" }`.
- **`props:` needs `new Dictionary<string, object> { … }`**, not target-typed `new()`. Column/Row/Grid take no `props:`.
- **The style array is classes, not CSS** — there is no `inlineStyle:`. A runtime value goes in as an arbitrary value: `$"w-[{pct:0.#}%]"`, `$"h-[{n}px]"`.
- **Lambda parameters can't be reserved words** (`checked`, `event`, `params`, `lock`, `default`, `base`, `new`, `object`, `string`) — name it `isChecked`.
- **A ternary of two style arrays needs a target type** (CS0173 on `var`): a `string[]` local, the ternary passed straight into the call, or a conditional element: `["rounded p-2", isActive ? "bg-active" : "bg-card"]`. CS1003/CS1525/CS1026 near a style array is an unescaped quote, unbalanced brace or reserved-word parameter instead.
- **Component parameters.** Dialog: `title:`/`description:`/`content:` + `titleStyle:`/`descriptionStyle:`/`headerStyle:`. AlertDialog: the same plus `cancelLabel:`/`actionLabel:`/`onAction:`, slot styles `titleStyle:`/`descriptionStyle:`/`footerStyle:`/`cancelStyle:`/`actionStyle:` — no `headerStyle:`. The field family's `label:` renders a VISIBLE label above the control — omit it when the design has none (`placeholder:` is the handle); on Checkbox/Switch/Toggle it is the clickable trailing text.
- **Size constants.** `view.Icon(name: "check", size: IconSize.Lg)`. Size ENUMS never go in a style array (CS0029) — use the class constant (`Icon.Sm`, not `IconSize.Sm`). Buttons bake variant and size into one constant: `Button.PrimaryMd`, `Button.ErrorMd`, `Button.OutlineErrorMd` — no `Button.Danger*`, no `variant:`/`size:` params.
- **There is no** `Text.Xs`, `Tabs.Default` (`Tabs.List`/`Trigger`/`Content`), `SemanticTone.Destructive` (`Error`), `text:` on `ToggleGroupItem` (a `Text` in `content:`) or `title:` on `StatCard` (`label:`).
- **`ScrollArea`**: the style array (= `rootStyle:`) is the outer wrapper, `viewportStyle:` the scrolling area.
- **`view.Image(["w-72"], src: url, alt: "…")`** or `(…, data: bytes, mimeType: "image/png")` — source named.
- **Copy, share and download are `view.ActionButton`** with `ActionKind.CopyToClipboard`, `Share` or `DownloadFile`. There is no `ClientFunctions.CopyToClipboardAsync`/`RunJavaScriptAsync`.
- **`hover:` never fires on touch**: a hover-revealed control also needs `pointer-coarse:opacity-100` or `focus-within:opacity-100`.

### Custom frontend and real-time

- **Native `view.*` first; when Parallax lacks the piece, build it.** For a rich text or code editor, a map, a 3D view, a chart library, a timeline or a signature pad, never ship a worse approximation or drop the feature: read the `frontend-fundamentals` guide and the `custom-react-node-embed` pattern, add the npm package, and write all four parts in one change — React component, resolver, `registerModule` in `app.tsx`, C# extension method.
- **Real-time MULTI-USER surfaces** (shared whiteboard, per-frame multiplayer game) are a custom canvas plus a `schema/<Name>.tp` message, because `view.*` has no client-to-client channel and per-frame `Reactive` diffs flood the connection (the `multi-user-game` and `realtime-shared-canvas` patterns). Single-user drawing is `view.ImageEditorCanvas`.

### Audio, files, media

- **Audio is synthesized and mixed SERVER-side and played through `Audio`** — no browser Web Audio. `Audio.Play(MediaTargets.Everyone, samples, sampleRate)` plays PCM you compute, `Audio.Speak(targets, text)` speaks, `Audio.PlayLive(targets, sampleRate)` takes buffers as you make them, and `Audio.Play(targets, new Uri(url), …)` plays a file. Each returns an `AudioPlayback` whose `Completion` yields an outcome instead of throwing (`audio-video` guide).
- **Video plays on named surfaces; the platform never decodes or encodes it.** Clients render `view.VideoSurface(surface: "stage")`; the app relays a camera with `Video.Play(targets, "stage", input)` once, from `Video.InputStartedAsync`. A file one person watches at their own pace is `view.VideoUrlPlayer(url: …)`.
- **`FileUpload` `accept:` is a string array** — `accept: ["image/*", ".pdf"]`. Anything kept, and all media, goes to asset storage: `onUploadStart: async args => new FileUploadResult { AssetUri = new AssetUri(AssetClass.CloudFile, path, app.GlobalState.SpaceId) }`, then `onUploadComplete` gets `args.AssetUri`. Without a start hook a small file lands in `args.LocalTempFilePath`. Hooks have deadlines and run concurrently (`media-analysis` guide).
- **Never copy video or audio into the app container** (disk and RAM are far too small): analyze it through `(await Asset.Instance.GetMetadataAsync(uri)).Url`, a signed URL the preinstalled `ffprobe`/`ffmpeg` read over HTTP.
- **App files: root `public/` (served by URL path, `view.Image(["w-full"], src: "/hero.png")`) and `data/` (private); `app.Files.Data` and `app.Files.Public` at runtime.** Their writes go to the cloud app's storage, even under a local `ikon run`. Never read through `AppContext.BaseDirectory` (in cloud it is the host's folder) (`app-structure` guide, "App Files").

### App structure and platform

- **A "Sign in" button drawn in C#** calls `ClientFunctions.LoginShowAsync(reason)` to open the app's sign-in screen with every configured method. `LoginAsync(provider)` is OAuth only.
- **Keep the scaffold's shape**: `[App] public partial class MyApp(IApp<SessionIdentity, ClientParameters> app)` with `public record SessionIdentity(string? UserId)` / `public record ClientParameters(string Name)`. The per-user identity gives each user their own instance, so a shared or multiplayer app declares `SessionIdentity()` or a room id instead. A local `ikon run` hosts exactly one instance whatever the identity.
- **Field initializers may use the primary-constructor `app`** (`private Audio Audio { get; } = new(app);`) but no other instance member (CS0236). Wire events (`app.OnClientJoined(...)`) in `Main()`.
- **`Main()` returns after `UI.Root(...)`.** No `await Task.Delay(Timeout.Infinite)` — a Main that never returns fails the boot at the start timeout; ongoing work belongs in callbacks or background tasks.
- **A hot reload starts a new instance** and runs `Main`/`OnStarting` again: reactives are restored, plain fields (engines, caches) are not, so rebuild them in `OnStarting`, and end background loops on `app.StoppingToken` (`app-structure` guide, "Work That Outlives a Hot Reload or a Deploy").
- **Lifecycle**: `app.OnStarting(async () => …)`, `app.OnStopping`, `app.OnClientJoined(async ctx => …)` (or `(ctx, parameters)`), `app.OnClientLeft`, `app.OnMessageReceived(async msg => …)` — not `(sender, args)` (CS1593).
- **Seed demo content** for shared state in `app.OnStarting` when the store is empty; per-user state in `app.OnClientJoined` — there is no user scope in `OnStarting`. Staging-only test rows go in `app.Seed(async () => …)` (`databases` guide).
- **A value presented as unique is GENERATED.** Room, join and invite codes, share slugs and ids come from `Random.Shared` or `Guid` — a readable placeholder ships as the real thing. Seeded identities are distinct, and demo rows look like real data, not `Item 1`.
- **Open a database connection PER operation**: `await using var conn = await app.DatabaseAsync("mydb"); await conn.OpenAsync();` in each method, never a connection field; `CREATE TABLE IF NOT EXISTS` in `app.OnStarting`, or EF Core (`databases` guide). A database `"X"` needs `ikon db create --name X`; the built-in one needs nothing.
- **Don't invent a `using Ikon.*;`** (CS0234), but a nested namespace is not imported with its parent (CS0246/CS0103 on a real type). Outside `GlobalUsings.cs`: `Ikon.Common.Core.Protocol` (`Context`, `TriggerEventType`, `VideoCodec`, `ServerRunType`), `Ikon.App.Cells`, `Ikon.App.Cron`, `Ikon.App.Mcp`, `Ikon.App.Triggers`, `Ikon.AI.Retrieving`, `Ikon.AI.Reranking`, `Ikon.AI.Emergence.Structured`, `Ikon.AI.Emergence.Tree`, `Ikon.AI.Database`, `Ikon.AI.Storage`, `Ikon.Common.Core.Email`, `Ikon.Sdk`, `Ikon.Crosswind`.
- **Connector and pipeline namespaces need their package first** — a `PackageReference` named like the namespace (`Ikon.Connectors.Google`, `Ikon.Pipeline`, …; `pipelines` guide).
- **`[Description]` is `Ikon.Common.DescriptionAttribute`**, already imported; adding `using System.ComponentModel;` makes it ambiguous (CS0104).
- **No `await` inside a `Log.Instance.*($"…")` message** (CS4007): await into a local, then log it.
- **Name a `using` local**: `using var busy = _busy.AsToken();`. `using var _ = …` declares a variable named `_`, so a later `_ = SomeTask();` discard in that method fails (CS1656).
- **The message author type is `Author`**, not `MessageAuthor`: `msg.Author == Author.User` (`agent-threads` guide).
- **Keep the entry point and the global namespace.** `using` directives precede the top-level `return await App.Run(args);` (CS1529); never delete that line (CS5001) or move it into a `Program.cs`. Never add a `namespace` to app files: inside `namespace Ikon.App.<Anything>`, `App` resolves to the `Ikon.App` namespace, and `App.Run`/`App<…>` fail with CS0234/CS0305/CS8956. New partial files go beside the main one, namespace-free; shared `using`s go in `GlobalUsings.cs`.

### Color tokens and theming

- **`bg-primary` is NOT the brand fill** — it is the PAGE SURFACE (white in light, near-black in dark). Brand fill is `bg-brand-solid`, brand text `text-brand-secondary`, brand border `border-brand`; neutrals are `bg-background`/`bg-card`, `text-foreground`, `border-secondary`.
- **A hardcoded palette needs `Mode = ThemeMode.Fixed`**, or dark-preference clients get dark-scheme text on your light surfaces; for both schemes provide `DarkMode`.
- **Only the role tokens take a numeric shade** (`brand-100`, `error-700`); `text-tertiary-600` is IKON006.
- **Author Tailwind v4 names**: `shadow-xs`/`shadow-sm`/`rounded-xs`/`rounded-sm` where v3 wrote `shadow-sm`/`shadow`/`rounded-sm`/`rounded`, `outline-hidden`, `ring-3`, `bg-black/50` for `bg-opacity-50` (`styling-and-motion` guide, "Colour tokens", has the table and the contrast rules).

### C# string literals

- **ASCII `"` delimiters only** — a curly `“”` used as a delimiter is CS1056; inside a literal it is just a character.
- **Escape inner quotes in `$"…"`** (`$"Add card to \"{col}\""`) — one unescaped `"` cascades into CS1002/CS1010/CS1026 across the file; fix that quote, not the phantom lines. Many quotes → `$"""…"""`.
- **Raw-string `$` count matches brace nesting**: literal `{}` (JSON in prompts) plus interpolation → `$$"""…"""` with `{{x}}`; interpolation only → `$"""…"""`; static → `"""…"""`. A mismatch is CS9006/CS9007.

### Runtime blank-screen triage

**Build-clean but the page shows nothing** — check in order: (1) the root doesn't fill the viewport → `UI.Root([Page.Default], content: view => { view.Column(["h-screen w-full …"], …); })`; (2) white-on-white → commit a theme and paint on `bg-background`/`bg-card`; (3) a red "Unregistered node type" placeholder → register the custom module or use native components. Fix root, height and background before restyling cards.

## API Reference Guides

Each agent guide is `docs/Ikon.Agent.Docs/guides/<name>.md`, one subject per guide:

- **agent-stages-and-history**: stage machines, pass records, journals and checkpoints of agent threads
- **agent-threads**: long-running agent threads, sub-agents, artifacts, budgets, asking the user
- **ai-advanced**: the low-level AI database and storage
- **ai-image**: image generation, upscaling, depth maps, segmentation and 3D meshes
- **ai-models**: choosing an LLM model; KernelContext
- **ai-speech-and-audio**: text to speech, speech recognition, sound effects and music
- **ai-video**: video generation and enhancement; playing a video file
- **ai-web-and-data**: web search and scraping, classification, decisions, OCR, embeddings, retrieval
- **app-api-reference**: where the signatures of IApp and the host services are
- **app-structure**: the app class, identity, lifecycle, files, navigation, client functions
- **asset-system**: asset storage: cloud files and JSON, metadata, expiry, concurrency
- **audio-video**: playing and capturing audio and video, effects, calls, broadcasts, synthesis
- **charts**: pie, bar and line charts
- **component-vocabulary**: the enums, records and event arguments components take
- **consent**: asking for and recording end-user consent
- **costs**: the app's AI costs in credits, per day and per usage event
- **crosswind-reference**: Crosswind styling recipes: layouts, gradients, overlays, effects
- **csharp-primer**: modern C# idioms and the syntax mistakes that fail a build
- **databases**: Postgres, SQL, EF Core, migrations and seed data
- **email**: sending and receiving email
- **emergence-strategies**: advanced Emerge patterns: map-reduce, tree search, critics, ensembles
- **emergence**: Emerge.Run<T>: structured output, tools, chat history, cancellation
- **endpoints-webhooks**: HTTP endpoints, webhooks, MCP tools, raw sockets and cron methods
- **frontend-fundamentals**: the React frontend: custom components, sign-in, embedding, branding
- **function-registry**: functions the client and LLM tools can call
- **location**: one-off and continuous GPS location
- **logging**: logging from an app
- **media-analysis**: uploading large media and analysing it with ffmpeg
- **motion-reference**: the full motion and keyframe syntax
- **notifications**: push and in-app notifications, inbox, channels
- **parallax-overview**: the Parallax UI library as a whole
- **payments**: payments, subscriptions, offers and entitlements
- **pipelines-reference**: the pipeline API
- **pipelines**: background processing pipelines
- **platform-runtime-types**: TOML, run type, name conversions, exceptions and other runtime types
- **product-design**: what each kind of app must contain, and how it must look on the first build
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


## API Signatures

Exact signatures for every public platform type live in `docs/Ikon.Agent.Docs/api/`, one file per namespace (`Ikon.Parallax.Components.Standard.md`, `Ikon.App.md`, …), with a `~2`, `~3` suffix where one namespace needs several. These are generated from the compiled assemblies, so they are the authority on a parameter list, a default value or a return type whenever a guide and a signature disagree.

Grep that directory by type or member name before guessing a signature — it is what turns a CS1739 or CS1061 into a one-line fix. A type's line is indented two spaces and its members four, so read up from a member's line to the nearest two-space line to see which type it belongs to: `OpenDatabaseAsync` is on `AppServices`, not on `app`.


## Code Patterns

`docs/Ikon.Agent.Docs/patterns/` holds short, self-contained patterns lifted from production IKON apps, each one compiled against the real assemblies and render tested, so a pattern cannot drift from the API it uses.

Start at `docs/Ikon.Agent.Docs/patterns/_index.md`, which groups every pattern by the task it solves. Adapting the closest pattern is faster and safer than writing a screen or a flow from scratch.

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
