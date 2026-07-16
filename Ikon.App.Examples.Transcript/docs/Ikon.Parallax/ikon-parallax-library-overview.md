# Ikon.Parallax Library Overview

## Introduction

Ikon.Parallax is a server-driven, reactive UI library for building interactive applications in C#. The library provides a declarative API for constructing user interfaces where all logic runs on the server, clients act as lightweight renderers, and the framework automatically handles efficient UI updates through intelligent diffing.

The name "Parallax" reflects the library's core capability: different clients can receive different views of the same underlying UI based on reactive scopes, similar to how parallax creates different viewpoints of the same scene.

## Core Concepts

### Reactive UI Updates

Ikon.Parallax uses the reactive system from `Ikon.Common.Core.Reactive`. When a reactive value changes, only the UI components that depend on that value are re-rendered. The framework tracks dependencies automatically during rendering — reading `_count.Value` inside a UI lambda registers the dependency; there is no explicit subscription API.

```csharp
private readonly Reactive<int> _count = new(0);
private readonly Reactive<string> _message = new("Hello");

// When _count.Value changes, only UI that reads _count.Value re-renders
// When _message.Value changes, only UI that reads _message.Value re-renders
```

### Server-Side Diffing

The UI tree is constructed and diffed entirely on the server. When changes occur:

1. The reactive system detects which values changed
2. Affected UI components re-render on the server
3. The framework computes a minimal diff
4. Only the diff is sent to clients

This architecture means clients can be thin renderers with minimal logic.

### Themed Components and Crosswind Styling

Styling uses Crosswind, a Tailwind-compatible utility class system, written as `string[]` style arrays. Every styled component ships a themed default, so the minimal call renders a complete, themed control — a `style:` array *merges on top of* that default rather than replacing it (see [Styling](#styling-with-crosswind) below).

```csharp
view.Button(text: "Save", onClick: SaveAsync);                  // fully themed as-is
view.Button([Button.PrimaryMd, "w-full"], text: "Save", onClick: SaveAsync);
```

## Setting Up a UI

Create a `UI` instance with the app host and a theme, and call `Root` to define the UI tree:

```csharp
[App]
public class MyApp(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    private readonly Reactive<int> _counter = new(0);

    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column(["items-center gap-4 p-6"], content: view =>
            {
                view.Heading("Counter App", style: [Text.H2]);
                view.Text([Text.Body], text: $"Count: {_counter.Value}");
                view.Button([Button.PrimaryMd], text: "Increment",
                    onClick: async () => _counter.Value++);
            });
        });
    }
}
```

When `_counter.Value` changes, only the Text displaying the count re-renders, and only that diff is sent to clients.

Committing to a brand happens in the `IkonTheme` initializer — an indexer-keyed object initializer where each entry sets one theme token:

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    ["primary"] = "amber-400",
    ["background"] = "zinc-950",
});
```

The full theme key vocabulary, dark-mode pairing (`IkonTheme.DarkMode`), and `ThemeMode.Fixed` for brand-locked looks are covered in the **Ikon Theming Guide** (`ikon-theming-guide.md`).

### Light/Dark Switching with UseTheme

`UI.UseTheme()` wires per-client theme state in one call, replacing the hand-rolled ClientJoined + toggle plumbing apps used to repeat:

```csharp
private ThemeControl _theme = null!;

public async Task Main()
{
    _theme = UI.UseTheme();   // call once, before clients join

    UI.Root([Page.Default], content: view =>
    {
        view.Button(
            icon: _theme.Current.Value == Theme.Dark ? "sun" : "moon",
            text: "Toggle theme",
            onClick: _theme.ToggleAsync);
    });
}
```

`ThemeControl.Current` is a `ClientReactive<Theme>` bindable in views; `ToggleAsync`/`SetAsync` flip the calling client and push the change to it. By default a joining client that already has a saved theme keeps it (`followClient: true`).

## Reactive State

### Shared, Per-Client, Per-User, Per-Mount

Reactive values are `private readonly` fields. The scope is picked by type:

| Type | One value per… | Typical use |
|---|---|---|
| `Reactive<T>` | app (shared by all clients) | Shared game state, lobby lists |
| `ClientReactive<T>` | client session | Form input, selected tab, dialog open |
| `UserReactive<T>` | user (shared across their sessions) | Preferences, cart |
| `MountReactive<T>` | Parallax mount | Per-mount chat history |

```csharp
private readonly Reactive<int> _sharedCounter = new(0);
private readonly ClientReactive<string> _draft = new("");
private readonly UserReactive<string> _language = new("en");
```

In UI lambdas and action handlers you just read and write `.Value` — the active scope resolves the right per-client or per-user slot implicitly. This is the "parallax" effect: the same UI code produces different views for different clients.

To seed each scope's initial value from its id, `ClientReactive` and `MountReactive` have a static `Create` factory and `UserReactive` a seeding constructor:

```csharp
private readonly ClientReactive<string> _welcome =
    ClientReactive.Create(sessionId => $"Welcome, session {sessionId}!");

private readonly UserReactive<List<string>> _cart =
    new(userId => LoadCart(userId));
```

### Reactive Collections: ReactiveList and ReactiveDictionary

List and dictionary state goes in `ReactiveList<T>` / `ReactiveDictionary<TKey, TValue>` — not in a `Reactive<T>` wrapping a mutable collection (that shape is build warning IKON002: in-place mutations bypass change detection). Every mutation method is one change notification, and reads (`Count`, indexer, enumeration, `ContainsKey`, …) are tracked so the UI re-renders on change:

```csharp
private readonly ReactiveList<TodoItem> _todos = new();
private readonly ReactiveDictionary<string, int> _scores = new();

_todos.Add(item);                    // also: AddRange, Insert, Remove, RemoveAt,
_todos.RemoveAll(t => t.Done);       // RemoveAll, Clear, ReplaceAll, Sort
_todos.Update(list => list.OrderBy(t => t.Priority));  // whole-list transform, one notification

_scores["anna"] = 10;                // add-or-replace, one notification
_scores.Update(map => map["anna"]++); // atomic read-modify-write under the lock

foreach (var todo in _todos) { ... } // enumerate the reactive directly
```

Both come in the same scoped variants as the scalars: `ClientReactiveList<T>` / `UserReactiveList<T>` / `MountReactiveList<T>` and `ClientReactiveDictionary<TKey, TValue>` / `UserReactiveDictionary<TKey, TValue>` / `MountReactiveDictionary<TKey, TValue>`.

### Background Work: the *For Methods

`.Value` works wherever the scope is active — inside `UI.Root()`, an action callback, or a `ReactiveScope.Use(new ClientScope(...))` block. Background work (a `Task.Run` loop, a timer, an endpoint handler) carries no client scope, so `.Value` there throws rather than writing to nowhere. Name the target instead with the `*For` methods, capturing the id while the scope is still active:

```csharp
var clientSessionId = ReactiveScope.ClientId;   // capture inside the callback

_ = Task.Run(async () =>
{
    var draft = await LoadDraftAsync();
    _draft.SetFor(clientSessionId, draft);      // scalar: SetFor / ValueFor / UpdateFor
    _items.AddFor(clientSessionId, item);       // list: AddFor / RemoveFor / ClearFor / UpdateFor
});
```

`ClientReactive` variants key by the `int` client session id, `UserReactive` by the `string` user id, `MountReactive` by the `string` mount id.

### The Busy/Status Pattern

Async handlers that flip a busy flag and surface failures in the UI use the helpers instead of hand-rolled try/catch/finally:

```csharp
private readonly Reactive<bool> _busy = new(false);
private readonly Reactive<string?> _status = new(null);

private async Task RefreshAsync()
{
    await _busy.RunAsync(_status, async () =>
    {
        _entries.Value = await LoadEntriesAsync();
    });
}
```

`RunAsync` clears the status, holds the busy flag for the duration of the work (it always returns to false, even on failure), routes an exception's message into the status reactive, and returns whether the work completed. For a busy flag alone, `using var _ = _busy.AsToken();` covers the flag-with-guaranteed-reset half of the pattern.

### Persistent State

Durable variants — `PersistentSessionReactive<T>` (the default choice), `PersistentUserReactive<T>`, `PersistentReactive<T>`, and the matching `...ReactiveList` / `...ReactiveDictionary` families — persist across app restarts with the same reactive API. See the **Ikon Persistent State Guide** (`ikon-persistent-state.md`).

## Components

The `UIView` class provides extension methods for UI components. One shape for every call: the leading `string[]` style array is the only positional argument, everything else is named (`view.Text` / `view.Button` / `view.Heading` / `view.Icon` / `view.Markdown` also accept their text positionally first).

**Layout:**
- `view.Row()` / `view.Column()` / `view.Box()` / `view.Grid()` - Flex and grid containers
- `view.ScrollArea()` - Scrollable container with optional smart auto-scroll
- `view.ScrollColumn()` - Header/body/footer column where the body scrolls
- `view.VirtualList()` / `view.VirtualGrid()` - DOM-virtualized large collections
- `view.InfiniteScrollView()` - Scroll area with near-end callbacks for lazy loading

**Display:**
- `view.Text()` / `view.Heading()` / `view.Markdown()` - Text content
- `view.Image()` - Images from URL or bytes
- `view.Icon()` - Icon library glyphs
- `view.Spinner()` / `view.Skeleton()` - Loading states

**Inputs:**
- `view.Button()` - Clickable button
- `view.TextField()` / `view.TextArea()` - Text input
- `view.Checkbox()` / `view.Switch()` / `view.Toggle()` - Toggles
- `view.Select()` / `view.RadioGroup()` - Choice inputs
- `view.Slider()` - Range slider
- `view.FileUpload()` - File upload zone

**Overlays:**
- `view.Dialog()` / `view.AlertDialog()` - Modal dialogs
- `view.Popover()` / `view.Tooltip()` / `view.HoverCard()` - Anchored overlays
- `view.Sheet()` / `view.Drawer()` - Edge panels

**Navigation:**
- `view.Tabs()` - Tabbed interface
- `view.AccordionSingle()` / `view.AccordionMultiple()` - Collapsible sections
- `view.TreeView()` - Hierarchical trees
- `view.Breadcrumb()` - Path navigation

**Data:**
- `view.DataTable()` - Paginated tables with typed cells, row actions, and column resize (per-slot styling via the `DataTableStyles` record on `styles:`)
- `view.BarChart()` / `view.LineChart()` / `view.PieChart()` - Interactive charts
- `view.ChatLog()` - Chat-bubble layout with auto-scroll and composer

### Two-Way Binding

Form controls take a `bind:` parameter as the two-way form — TextField/TextArea/Select/RadioGroup bind a `Reactive<string>`, Checkbox/Switch a `Reactive<bool>`:

```csharp
view.TextField(["flex-1"], label: "Name", bind: _name);
view.Switch(bind: _subscribed, label: "Subscribe to newsletter");
```

Without a reactive at hand, use `value:` + `onValueChange:` explicitly; when both are passed, `bind:` wins.

### App Chrome and SemanticTone

The chrome composites — Badge, Alert, Toasts, StatCard's icon box — share the `SemanticTone` enum (`Neutral`, `Brand`, `Success`, `Warning`, `Error`, `Info`). Tones map to the theme's semantic color tokens, so they are correct in both light and dark mode:

```csharp
view.Badge("Live", SemanticTone.Success);
view.Alert("Import failed", SemanticTone.Error, description: "The file is not valid CSV");
view.StatCard("Revenue", "$12,400", delta: "+8%", trend: StatTrend.Up, icon: "trending-up",
    iconTone: SemanticTone.Success);
```

Toasts are a per-client queue rendered by a single `ToastHost` in the root UI:

```csharp
private readonly Toasts _toasts = new();

// In UI.Root, mount exactly once:
view.ToastHost(_toasts);

// From any handler:
_toasts.Success("Saved");
_toasts.Error("Upload failed", ex.Message);
```

## Styling with Crosswind

Three layers compose freely in the same style array:

1. **Semantic theme-aware classes** (`bg-card`, `text-primary`, `bg-brand-solid`, `border-secondary`, `bg-background`) — the default path. They resolve through CSS variables that flip between light and dark and respond to per-app `IkonTheme` overrides, so re-theming never touches individual style arrays.
2. **`Ikon.Parallax.Theming` token classes** (`Button.PrimaryMd`, `Card.Default`, `Text.H1`, `Layout.Page`, `Tone.Solid`, …) — pre-composed bundles of the semantic classes with tested defaults.
3. **Hardcoded palette classes** (`bg-amber-400`) and raw hex — for looks that intentionally should not change with the theme. These bypass the theme system entirely.

```csharp
view.Button([Button.PrimaryMd, "mt-4 self-center"], text: "Submit", onClick: SubmitAsync);
view.Box(["bg-card border border-secondary p-6 rounded-2xl"], content: v => { ... });
view.Text([Text.Caption], text: "Updated just now");
```

### Merge Semantics: Defaults, `default`, and `unstyled`

A component's `style:` array **merges with (adds to) its built-in themed default — it does not replace it**. `view.TextField(["w-full"], …)` renders a fully themed input that is also full-width, and your explicit classes win on conflict (`["h-14"]` overrides the default height). Write only the classes you are adding or changing.

Two markers control the merge:

- **`default`** — platform theme token *composites* (`Button.PrimaryMd`, `Input.Default`, `Card.Interactive`, `Badge.SuccessMd`, …) are complete component styles. Each begins with the literal `"default"` marker, which tells the component the constant IS the whole style, so the themed default is not merged underneath it: `[Button.OutlineMd]` renders exactly the outline button. Extra classes in the same array still layer on top and win on conflict (`[Button.OutlineMd, "mt-2 w-full"]`). Fragments (`Button.Base`, `Layout.*`, `Text.*`) carry no marker and merge normally.
- **`unstyled`** — to restyle a control from scratch with NO themed default, make the literal `"unstyled"` marker the first entry: `view.TextField(["unstyled", "border-4 border-pink-500 …"], …)`. Never prepend `"unstyled"` to a token composite — composites already skip the merge via their `default` marker.

Slot-style parameters with themed defaults (`contentStyle:` on Popover/Tooltip/HoverCard, Dialog's `titleStyle:`/`descriptionStyle:`/`headerStyle:`) follow the same merge rule.

### Default Styling and Auto-Composed Indicators

`style:` is optional. When it is omitted, interactive and input controls fall back to their default theme token, so the minimal call renders a complete, themed control:

```csharp
view.Checkbox(bind: _done);
view.Switch(bind: _on);
view.TextField(bind: _text);
view.Button(text: "Submit", onClick: async () => { });
```

Controls that have a visible inner part also compose it automatically when no `content:` is given — Checkbox gets its check indicator, Switch its thumb, Slider its track/range/thumb, Select its trigger and items. You only pass `content:` to customise the inner part, and an explicit `style:` array always merges on top of the default. To render a checkbox with no check mark, opt out explicitly with `content: _ => { }`. Layout primitives (`Box`, `Row`, `Column`, `Grid`, `Stack`) stay unstyled by default — there "no style" is the normal usage.

The Crosswind class vocabulary and the motion/animation system are covered in the **Crosswind Styling and Motion Guide** (`crosswind-styling-and-motion-guide.md`); theme keys and brand palettes in the **Ikon Theming Guide** (`ikon-theming-guide.md`).

## ScrollArea and Auto-Scroll

ScrollArea provides a scrollable container with smart auto-scroll support, ideal for chat interfaces and live feeds:

```csharp
view.ScrollArea(
    rootStyle: ["h-[400px]"],
    autoScroll: true,
    autoScrollKey: _messages,
    content: view =>
    {
        foreach (var msg in _messages)
        {
            view.Text([Text.Body], text: msg);
        }
    });
```

`autoScrollKey:` takes the thing that changes — the reactive collection itself, a count, or a composite string.

**Auto-scroll behavior (Polite priority):**
- At bottom: new content auto-scrolls into view
- Scrolled away: auto-scroll is suppressed, a floating indicator appears to notify the user
- Clicking the indicator or scrolling back to bottom resumes auto-scroll

For forced scrolling (always scroll regardless of position), use `FocusHint` with `FocusPriority.Assertive`:

```csharp
anchor.FocusHint(new FocusHintProps { Priority = FocusPriority.Assertive },
    key: $"scroll-{version}");
```

### Scrolling Inside a Flex Parent

The canonical dialog / side-panel pattern is a `Column` with a fixed height, a header + footer, and a scrolling body. `ScrollArea` automatically applies `min-h-0 min-w-0` to its root so shrinking inside a flex parent Just Works — no ceremony required.

**Why this matters.** A flex child's `min-height` defaults to `auto` (equal to its intrinsic content size), so without `min-h-0` a `flex-1` scroll region would grow to fit all its content — pushing siblings off-screen and bypassing the inner overflow. The framework handles this for `ScrollArea`. You only need to think about it on your own `Column`/`Row` with a manual `overflow-y-auto`.

**Canonical recipe — `ScrollArea` inside a flex column:**

```csharp
view.Column(["h-[82vh] flex flex-col"], content: dialog =>
{
    dialog.Row(["items-center px-5 py-4 border-b"], content: header => ...);

    dialog.ScrollArea(
        rootStyle: ["flex-1"],              // min-h-0 is injected automatically
        scrollbars: ScrollAreaScrollbars.Vertical,
        content: body => ...);

    dialog.Row(["items-center px-3 py-2 border-t"], content: composer => ...);
});
```

**Or use the `ScrollColumn` primitive** that wraps the header/body/footer pattern in a single call, so the shape can't be misused:

```csharp
view.ScrollColumn(
    style: ["h-[82vh] w-full sm:max-w-[560px] rounded-2xl bg-card"],
    header: h => h.Row(["px-5 py-4 border-b"], content: title => ...),
    footer: f => f.Row(["p-3 border-t"], content: composer => ...),
    content: body => body.Column(["gap-3"], content: messages => ...));
```

For chat specifically, `view.ChatLog()` wraps `ScrollColumn` with chat-friendly defaults (auto-scroll on).

**Raw `Column`/`Row` with overflow-auto.** If you're not using `ScrollArea`, you still need `min-h-0` (or a fixed height) yourself — the framework fix only applies to the `ScrollArea` component:

```csharp
view.Column(["flex-1 min-h-0 overflow-y-auto", ...], ...);
```

Dev builds (debugger attached or `IKON_DEV_WARNINGS=1`) emit a single `Log.Instance.Warning` when they detect a `Column`/`Row`/`Box`/`Flex` with `overflow-y-auto` + `flex-1` and no `min-h-0` — with the exact `file:line` of the offending callsite.

## Example: Interactive Form

```csharp
private readonly ClientReactive<string> _name = new("");
private readonly ClientReactive<bool> _subscribed = new(false);
private readonly Reactive<bool> _busy = new(false);
private readonly Reactive<string?> _status = new(null);

public async Task Main()
{
    UI.Root([Page.Default], content: view =>
    {
        view.Column(["gap-4 max-w-md p-8"], content: view =>
        {
            view.TextField(label: "Name", placeholder: "Your name", bind: _name);

            view.Switch(bind: _subscribed, label: "Subscribe to newsletter");

            view.Button([Button.PrimaryMd], text: "Save",
                disabled: _busy.Value,
                onClick: SaveAsync);

            if (_status.Value is { } status)
            {
                view.Alert("Save failed", SemanticTone.Error, description: status);
            }
        });
    });
}

private async Task SaveAsync()
{
    await _busy.RunAsync(_status, async () =>
    {
        await StoreAsync(_name.Value, _subscribed.Value);
    });
}
```

Each client edits its own `_name`/`_subscribed` (they are `ClientReactive`), the Save button disables while the shared `_busy` flag is up, and a failure surfaces as an error Alert via `_status`.

## Boot Snapshot and Privacy

The platform can capture an app's **initial UI at build time** and ship it as a static `boot-snapshot.json`, so the first paint appears instantly — before the WebSocket connects. Capture is **opt-in per app** via `ikon-config.toml`: enable the `[BootSnapshot]` section, then `ikon app bundle` / `ikon app deploy` renders the app once for a synthetic snapshot client and serializes the resulting UI tree.

```toml
[BootSnapshot]
Enabled = true
```

Because that snapshot is a **public asset served to everyone**, anything in your initial UI — a signed-in user's name, a session link, private data — would otherwise be baked into it and shown to every visitor before the live UI loads. So the snapshot is **privacy-safe by default**: during capture, Parallax automatically **replaces every piece of content with a skeleton**. Each text, image, input, and control becomes a pulsing placeholder block while the layout shape (rows, columns, tabs, cards) is preserved — so the first paint looks like your app's skeleton screen and **no per-user content can leak**, with no work from you.

During snapshot capture the flag `view.IsSnapshot` is `true` (it is always `false` on the normal live render). You only need to act when some content is **safe to show** in the public snapshot — your logo, static chrome, marketing copy — and you want it to appear for real instead of as a skeleton. Wrap it in `SnapshotReveal`:

```csharp
// Live: real content. Snapshot: real content too (opted out of skeletonization).
view.SnapshotReveal(v =>
{
    v.Image(["h-8"], src: "/logo.svg", alt: "Acme");
    v.Text([Text.H1], text: "Welcome to Acme");
});

// Live: real content. Snapshot: nothing (omit entirely — e.g. a control that is dead before connect).
view.SnapshotHide(v => v.Button(text: "Sign out", onClick: SignOutAsync));

// Live: nothing. Snapshot: snapshot-only filler, rendered as authored (not skeletonized).
view.SnapshotOnly(v => v.Text([Text.Caption], text: "Loading your dashboard…"));
```

- **`SnapshotReveal(content)`** — opts `content` out of automatic skeletonization, so it renders for real in the snapshot. The opt-out covers the whole subtree (nested containers and leaves included). Use only for content you are certain is safe to make public.
- **`SnapshotHide(content)`** — renders `content` live and omits it from the snapshot (not even a skeleton).
- **`SnapshotOnly(content)`** — renders `content` only in the snapshot (never live), for snapshot-specific filler; it is shown as authored rather than skeletonized.

The **`Skeleton`** component is also available directly — a pulsing placeholder block, sized and shaped via `SkeletonShape` / `SkeletonSize` (or any `style:`) — for hand-built loading states anywhere in your UI:

```csharp
view.Skeleton(["w-1/3"], size: SkeletonSize.Xl);
view.Skeleton(shape: SkeletonShape.Circle, size: SkeletonSize.Lg);
```

**`Tabs` stays snapshot-aware automatically:** in snapshot mode only the **active** tab's content panel is rendered (its content skeletonized like everything else), while **every** tab trigger still renders. The snapshot therefore carries just the active tab — yet the tab row is identical to the live UI, so nothing pops into place when the live UI takes over.

Automatic skeletonization is a **one-off build-time render**, so it can afford to be thorough. On the **live path it is zero-cost**: the snapshot branch is gated on `IsSnapshot`, which is `false`, so it short-circuits on a single boolean read with no per-element metadata and no effect on the diff/serialize hot path. For finer control you can read the flag directly:

```csharp
if (view.IsSnapshot) { /* snapshot-only branch */ }
```

**Preview the snapshot UI in a browser** by opening the running app with `?ikon-snapshot=true`. The SDK then connects as a snapshot client — the same `Context.IsSnapshot = true` render path the build-time capture uses — so the live page shows exactly what the boot snapshot bakes: every unrevealed element as a skeleton, `SnapshotReveal` regions showing real content, `SnapshotHide` elements gone, `SnapshotOnly` filler present, and only the active tab's panel rendered. It needs no rebuild and works against any running instance — a local `ikon app run` or a deployed URL — so you can confirm at a glance that no per-user or sensitive content leaks into the public first paint.

## Architecture Summary

1. **Server-side logic**: All UI logic, state, and event handlers run on the server
2. **Reactive updates**: Changes to reactive values trigger targeted re-renders
3. **Differential sync**: Only UI diffs are sent to clients
4. **Scoped state**: `ClientReactive<T>` / `UserReactive<T>` / `MountReactive<T>` (and their `ReactiveList` / `ReactiveDictionary` variants) give per-client, per-user, and per-mount state from the same UI code
5. **Lightweight clients**: Clients render the UI tree and forward events to the server
6. **Themed components**: Every styled component ships a themed default; `style:` arrays merge on top, with `default`-marked token composites and the `unstyled` opt-out controlling the merge
7. **Crosswind styling**: Tailwind-compatible utility classes with motion extensions
8. **Snapshot privacy**: the boot snapshot is skeletonized by default; `SnapshotReveal` opts safe content back in, `SnapshotHide` / `SnapshotOnly` cover the rest
