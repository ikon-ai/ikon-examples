<!-- This file is automatically updated by ikon tool commands. User edits are preserved only below the ikon-user-content-below marker. -->

# Ikon AI App Development Guidelines

**Detailed API references are in docs/guides/. See the guide index at the bottom of this document.**

## Architecture

Ikon AI App is a cloud-native platform for building interactive, AI-powered multi-user experiences.

**App structure:** C# app runs in the cloud, streams reactive UI to browser clients via WebSocket. The app persists even when all clients disconnect. Multiple clients connect to the same app instance for real-time collaboration, games, and shared experiences.

**AI services (no setup required — the platform handles all API keys and connections):**
- `Emerge.Run<T>()` — LLM text generation and structured JSON output (Claude, GPT, Gemini, Grok, and more)
- `new ImageGenerator(model)` — AI image generation (Gemini, DALL-E, Flux, etc.)
- `new SpeechGenerator(model)` — text-to-speech (ElevenLabs, Azure, OpenAI, Google)
- `new SpeechRecognizer(model)` — speech-to-text (Whisper)
- `new VideoGenerator(model)`, `new EmbeddingGenerator(model)`, `new WebSearcher(model)` — other AI services

**UI system:** Declared in C# inside `UI.Root(content: view => { ... })`. The `view` parameter type is `UIView` (a class — NOT `IView`, there is no `I`-prefixed interface, that's a hallucination that produces CS0246). Helper methods that accept a view parameter take `UIView view`, never `IView view`. `UIView` provides component methods: `view.Text()`, `view.Button()`, `view.ScrollArea()`, `view.TextField()`, etc. Nested content lambdas receive their own `UIView view` parameter.

**State:** `Reactive<T>` (shared across all clients), `ClientReactive<T>` (per-client), `UserReactive<T>` (per-user). Changes automatically trigger UI updates — only the diff is streamed to clients.

**Styling:** Crosswind — Tailwind-like utility classes in C# string arrays, with a motion animation system for declarative keyframe animations (`motion-[...]`).

**Audio:** `private Audio Audio { get; } = new(app);` — audio synthesis (Ikon.Resonance), effects, playback, and client microphone/camera capture.

**Namespaces:** All Ikon namespaces are auto-imported via `GlobalUsings.cs` — no manual `using` statements needed for Ikon types.

## Development Focus

- Most development happens in the C# app, including styling
- LLM text generation uses `Emerge.Run<T>()`. Other AI services (image, speech, video) are standalone classes instantiated directly
- UI is declared inside `UI.Root([Page.Default], content: view => { ... })` — components are methods on the `view` lambda parameter
- Crosswind styling: Tailwind-like syntax in style arrays, with motion animation system
- `Reactive<T>` for shared state, `ClientReactive<T>` for per-client state — changes trigger UI updates automatically
- All Ikon namespaces are auto-imported via `GlobalUsings.cs`
- Only modify frontend-node when integrating custom React UI components
- Developers can freely add any NuGet packages to C# app or npm packages to frontend

## Common Pitfalls

These are the recurring hallucination + footgun classes seen across generated Ikon apps. Each one compiles or runs cleanly in the wrong shape and silently breaks the app — read this list before writing UI code.

- **`UIView` is the view type, not `IView`.** Helper methods that take a view parameter declare `static void Render(UIView view, ...)`. There is no I-prefixed interface; `IView` produces CS0246. The C# convention "I-prefix means interface" doesn't apply here.
- **Reactive UI re-render is implicit. There is NO `view.Dynamic`, `view.Watch`, `view.Bind`, `view.Observe`, `view.When`, `view.Live`, `view.Reactive`, or any other wrapper component for reactive rendering.** When you read `_x.Value` inside a UI lambda (e.g. `view.Text([…], _name.Value)` or `if (_open.Value) { view.Box(…); }`), the framework records that dependency and automatically re-renders the enclosing subtree on change. Just read `.Value` directly. Inventing `view.Dynamic(reactive, content: view => …)` causes CS1061 ("UIView does not contain a definition for Dynamic").
- **`Emerge` is a static class, not an injected service.** Call `Emerge.AskAsync(prompt)` or `Emerge.Run<T>(model, ctx, pass => …)` directly. There is no `IEmerge`, no `app.Services.GetRequiredService<IEmerge>()`, no constructor-injection pattern. The same goes for `Audio`, `UI`, and other helper handles — `Audio` is `new(app)` in the app class, not service-resolved.
- **Not every view component takes `props:`.** `view.Text`, `view.Button`, `view.Box`, `view.Image` and most input/display components accept `props:` for HTML attributes. `view.Column`, `view.Row`, `view.Grid` and pure layout primitives do NOT — `style:` / `styleId:` / `key:` / `content:` only. Passing `props:` to a layout primitive produces CS1739.
- **`LLMModel` has NO `Default` value.** Canonical enum values include `Claude46Sonnet`, `Claude45Sonnet`, `Claude45Haiku`, `Gemini25Flash`, `Gemini25Pro`, `Gpt5Mini`, `Gpt5`, `Grok420Reasoning`. Pick a specific model per `Emerge.Run<T>(LLMModel.X, …)` call. `LLMModel.Default` produces CS0117 — default to `Claude46Sonnet` for general chat / reasoning, `Claude45Haiku` for fast cheap text.
- **`onSubmit:` and `onClick:` lambdas that contain `await` must be `async`.** Writing `onSubmit: text => { return; }` inside a `Func<string, Task>` produces CS0126 and CS1643. Use `onSubmit: async text => { await SendAsync(); }`.
- **`autoScrollKey:` on `view.ScrollArea` is `string?`, not a Reactive or list.** Pass a string that changes when content changes: `autoScrollKey: _messages.Value.Count.ToString()`. Passing a `Reactive<List<…>>` produces CS1503.
- **`view.TextField` has TWO overloads — never mix positional + named args.** Overload A starts `(view, Reactive<string> bind, style, …)`; Overload B starts `(view, style, value, …)`. Writing `view.TextField(["flex-1"], bind: _input, …)` makes C# pick overload A (because `bind:` exists only there), and the positional `["flex-1"]` falls into the `Reactive<string>` slot → CS1744. Use ALL named arguments: `view.TextField(bind: _input, style: ["flex-1"], onSubmit: async t => …)`. Same caution applies to any component with multiple positional-first overloads — when in doubt, name everything.
- **TextField `disabled:` must NOT be bound to fast-changing reactive state.** Binding `disabled: _busy.Value` or `disabled: _phase.Value != null` re-mounts the input on every flip and drops keyboard focus mid-typing — the textbox becomes unusable. Static `disabled: true` for read-only fields is fine. To gate a submit during async work: leave the TextField enabled, gate the action on the Button (`disabled: _busy.Value`) and early-return in the submit handler.
- **`onClick:` lambdas with assignments need braces.** `onClick: () => _foo.Value = x` is interpreted as `Func<T>` (returning the assigned value) and throws at runtime when the platform expects `Action`/`Func<Task>`. Wrap assignments: `onClick: () => { _foo.Value = x; }` or use an `async` lambda body block: `onClick: async () => { _foo.Value = x; }`.
- **`ScrollArea` uses `rootStyle:` and `viewportStyle:`, not the positional `style:`.** `view.ScrollArea(rootStyle: ["flex-1 min-h-0"], viewportStyle: ["px-4"], content: …)` for full-page scroll wrapping. The bare `style:` exists for outer wrapper styling only; content-area styling goes through `viewportStyle`.
- **`AudioContainer` carries PCM samples, not encoded bytes.** Properties are `float[] Samples`, `int SampleRate`, `int ChannelCount` — there is no `.Data` / `.MimeType`. To play `SpeechGenerator` output: declare `private Audio Audio { get; } = new(app);` then `Audio.SendSpeech(audio)` per chunk in the `await foreach`. `ClientFunctions.PlaySoundAsync(byte[], string)` is a different API for already-encoded MP3/WAV blobs.
- **`WebSearcher` uses `SearchPagesAsync(SearchConfig)`, not a positional `SearchAsync(string, int)`.** Real shape: `await searcher.SearchPagesAsync(new SearchConfig { Query = q, MaxResults = 5 });`. `SearchResult` exposes `Url`, `Title`, `Content`, `Mimetype`, `Keywords` — there is no `Snippet`; use `result.Content` for the body text.
- **`MessageAuthor` value comparison uses the static instances, not the nested types.** `Author == MessageAuthor.User` works (User is a static `MessageAuthor` instance). `Author == MessageAuthor.UserAuthor` does NOT compile — `UserAuthor` is a nested record type, not a value. For type-pattern matching use `is`: `Author is MessageAuthor.UserAuthor`. Same for `System` / `SystemAuthor`, `Agent(name)` / `AgentAuthor`, `Thread(id)` / `ThreadAuthor`.
- **`Reactive<List<T>>` collection mutation must go through the extensions, not `.Value.Add(...)`.** `_items.Add(x)` mutates and notifies in one call. `_items.Value.Add(x)` mutates the underlying list without firing the change event — UI doesn't refresh. Same for `Remove`, `Clear`, `RemoveAt`, `Insert`, `RemoveAll`.
- **`Reactive<T>` / `ClientReactive<T>` / `PersistentReactive<T>` / `PersistentUserReactive<T>` / `PersistentSessionReactive<T>` are `private readonly` FIELDS initialized with `new(defaultValue)` — ONE positional arg.** They derive their storage key from the field name via `[CallerFilePath]` / `[CallerMemberName]` — pass NOTHING else. RIGHT: `private readonly ClientReactive<int> _count = new(0);`, `private readonly PersistentUserReactive<List<TodoItem>> _todos = new([]);`. WRONG (all produce CS1503 / CS7036 build errors): `new ClientReactive<int>(app, 0)` — IApp is NOT a ctor arg; `app.ClientReactive<int>(0)` / `view.ClientReactive(0)` — there is NO such method on IApp or UIView; `new PersistentUserReactive<T>("key", default)` — there is no key arg. The ASP.NET / EF Core mental models (`context.Get<T>("key")` or `new(context, "key", default)`) do not apply to Ikon reactives.
- **`UIView` is a component-rendering surface only — it has NO methods for creating or subscribing to reactive state.** `view.ClientReactive(...)`, `view.Reactive(reactive, lambda)`, `view.Persistent(...)`, `view.Watch(...)`, `view.Bind(...)` ALL fail with CS1061. Reactive state is declared at class level (see field-init pitfall above), and subscription is implicit — reading `.Value` inside a UI lambda registers the dependency. There is no view-side "reactive scope" API.
- **`Main()` MUST return after `UI.Root(...)`. Do NOT add `await Task.Delay(Timeout.Infinite)` or any other infinite wait at the end of Main().** `UI.Root` registers the reactive renderer and returns immediately; the Ikon server framework keeps the process alive on its own. If Main() blocks forever, `OnServerStartedAsync()` never completes and the server dies with `TimeoutException` after 2 minutes (visible only in logs — the build is clean and the Critic gives the file a 9, but the app never actually starts). The complete Main() body is just `UI.Root([Page.Default], content: view => { … });` followed by closing braces.
- **ASCII quotes ONLY in C# source — never Unicode curly quotes `“` `”` `‘` `’`.** Even inside a `$"..."` interpolated string literal, the C# compiler does not accept smart quotes — CS1002 "expected ;" appears at the curly-quote position because the lexer sees a stray Unicode character, not a string delimiter. Some code editors auto-correct typed `"` into `“` and `”`; do not let that happen in your output. The same rule applies to apostrophes (`'` is C#, `’` is not).
- **TextField's `bind:` is a non-nullable `Reactive<string>`. Don't pass `bind: null`.** When you don't have a Reactive to bind to, OMIT the `bind:` argument entirely; the value flows through `value:` + `onValueChange:` instead. Passing `bind: null` produces CS8625 ("Cannot convert null literal to non-nullable reference type"). The full no-bind pattern: `view.TextField(value: _input.Value, onValueChange: async v => { _input.Value = v; }, onSubmit: async _ => …, clearOnSubmit: true)`.

## API Reference Guides

Detailed API docs are available in `docs/guides/`. Each guide covers a specific topic:

- **app-structure** (`docs/guides/app-structure.md`): app file structure, session identity, client parameters, partial class, global usings, lifecycle, host services, navigation, background work, client functions, messages, minimal app template, viewport layout, auto-scroll, QR code, join URL, multi-user session, invite link
- **csharp-primer** (`docs/guides/csharp-primer.md`): C# 13, modern C#, dictionary literal, collection expression, primary constructor, raw string literal, async, await, ValueTask, IAsyncEnumerable, target typing, nullable reference types, records, pattern matching, file-scoped namespace, top-level statements, modern idioms, enterprise patterns, abstractions, factory, IUnitOfWork, dependency injection, mock, interface, abstract base class, syntax error, CS1003, CS1525, CS1026, CS0173, CS8917, CS0234
- **app-api-reference** (`docs/guides/app-api-reference.md`): IApp, host services, server API, navigation, session, common utilities
- **reactive-state** (`docs/guides/reactive-state.md`): reactive, client reactive, user reactive, persistent reactive, persistent session reactive, persistent user reactive, persistence backend, postgres backend, public asset backend, reactive scope, value mutation
- **ui-components** (`docs/guides/ui-components.md`): layout, overlays, inputs, display, navigation, drag-and-drop, text, button, dialog, tabs, accordion, scroll area, toast, popover, chat interface, message bubbles
- **ui-api-reference** (`docs/guides/ui-api-reference.md`): parallax, UI components API, method signatures, component parameters, props
- **styling-and-motion** (`docs/guides/styling-and-motion.md`): crosswind, tailwind, theme constants, style arrays, motion, animation, UI guidelines, theme customization
- **styling-guide** (`docs/guides/styling-guide.md`): crosswind guide, UI design patterns, common pitfalls, sophisticated UI, layout patterns, gradient, overlay, CRT, scanline
- **motion-reference** (`docs/guides/motion-reference.md`): motion spec, keyframe, animation, timing, staggered text, 3D transform, filter animation, animatable properties, motion syntax grammar
- **tailwind-reference** (`docs/guides/tailwind-reference.md`): tailwind spec, utility classes, layout, flexbox, grid, spacing, typography, backgrounds, borders, effects, shadows, transitions
- **emergence** (`docs/guides/emergence.md`): emergence, emerge run, structured output, json, tools, agent, bestof, mapreduce, taskgraph, patterns, cancellation, timeout
- **emergence-patterns** (`docs/guides/emergence-patterns.md`): mapreduce, taskgraph, treesearch, tree of thought, solver critic, debate, refine, plan and execute, router, ensemble, swarm, advanced patterns
- **ai-models** (`docs/guides/ai-models.md`): LLM model, model selection, Claude, Gemini, GPT, Grok, model enum, KernelContext, AI connection
- **ai-image** (`docs/guides/ai-image.md`): image generation, ImageGenerator, AI image, photo, generate image, ImageGeneratorConfig
- **ai-speech-and-audio** (`docs/guides/ai-speech-and-audio.md`): speech, TTS, STT, voice, transcribe, whisper, sound effect, SpeechGenerator, SpeechRecognizer, SoundEffectGenerator
- **ai-video** (`docs/guides/ai-video.md`): video generation, video enhancement, AI video, VideoGenerator, VideoEnhancer
- **ai-web-and-data** (`docs/guides/ai-web-and-data.md`): web search, scrape, crawl, classify, OCR, embedding, vector, retrieve, rerank, file convert, WebSearcher, EmbeddingGenerator
- **ai-advanced** (`docs/guides/ai-advanced.md`): database AI, vector store, AI policy, storage
- **audio-video** (`docs/guides/audio-video.md`): audio, video, capture, speech, stream, effects, reverb, delay, mixer, synthesizer, oscillator, filter
- **asset-system** (`docs/guides/asset-system.md`): asset, cloud file, local file, cloud json, storage, metadata, URI, optimistic concurrency
- **endpoints-webhooks** (`docs/guides/endpoints-webhooks.md`): endpoints, webhooks, HTTP, HTTPS, WebSocket, TCP, TLS, UDP, public URL, tunneling, AppEndpointHost, function
- **databases** (`docs/guides/databases.md`): databases, PostgreSQL, SQL, db, AppDatabaseConnection
- **secrets** (`docs/guides/secrets.md`): secrets, tokens, API keys, credentials, passwords, app.Secrets, ikon app secret
- **function-registry** (`docs/guides/function-registry.md`): function registry, registration, attribute, visibility, LLM tools, callable functions
- **logging** (`docs/guides/logging.md`): log, logging, debug, warning, error, diagnostics
- **pipelines** (`docs/guides/pipelines.md`): pipeline, background processing, transform, processor, scheduled, cron
- **pipelines-reference** (`docs/guides/pipelines-reference.md`): pipeline API, transform, processor, pipeline guide
- **frontend-fundamentals** (`docs/guides/frontend-fundamentals.md`): frontend, SDK, auth, connection, i18n, styling, query params, custom UI component, module, resolver, React, magic link login email template, emails folder
- **config-and-cli** (`docs/guides/config-and-cli.md`): ikon-config.toml, CLI commands, build, deploy, run, auth, activation, targets


<!-- ikon-user-content-below -->
