# UI API Reference

## UI Component API Reference

Full API reference for all Ikon.Parallax UI components.

### Parameters every component shares

These mean the same thing on every component below, so the signatures do not repeat them:

- **`style`** — Crosswind utility classes. Lead with the `"default"` marker or a `Theming.*` composite to merge the component's themed default underneath; without one you get exactly the classes you passed.
- **`styleId`** — a CSS class name applied directly. For exceptional cases; prefer `style`.
- **`key`** — a stable identity to help diffing across renders. For exceptional cases; the builder derives one otherwise.
- **`ariaLabel`** — the accessible name for a control whose visible content cannot supply one. Prefer a visible label.
- **`content`** — a nested build lambda receiving its own `UIView`.
- **`props`** — raw props passed through to the underlying component.

On the components that take them, these are also uniform:

- **`value`** / **`onValueChange`** — the controlled value and its change callback. Pass `value` to control the component yourself.
- **`defaultValue`** — the initial value in uncontrolled mode. Pass this *or* `value`, never both.
- **`forceMount`** — when true, keeps the content in the DOM while hidden (so it can animate out, or be measured).
- **`loop`** — when true, keyboard navigation wraps from the last item back to the first.
Every component's signature lives in the API sections, one per namespace —
`Ikon.Parallax.Components.Standard` for the components themselves, `Ikon.Parallax.Theming` for the
theme slots. Ask the oracle for a component by name and it returns the section that declares it.

---

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

Styling uses Crosswind, a Tailwind-compatible utility class system, written as `string[]` style arrays. Every styled component ships a themed default, so the minimal call renders a complete, themed control. A `style:` array *replaces* that default — it renders exactly what you passed — unless it asks for the merge with the `"default"` marker (see [Styling](#styling-with-crosswind) below).

```csharp
view.Button(text: "Save", onClick: SaveAsync);                  // fully themed as-is
view.Button([Button.PrimaryMd, "w-full"], text: "Save", onClick: SaveAsync);
view.Button(["default", "w-full"], text: "Save", onClick: SaveAsync);   // same: Button's default IS PrimaryMd
```

## Setting Up a UI

Create a `UI` instance with the app host and a theme, and call `Root` to define the UI tree:

```csharp
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

`ThemeExtensions` reads the calling client's choice off a `Context` — `clientContext.IsDarkTheme()`, which is false for the light theme, for custom theme names, and for a client that has not reported one, and `theme.ToThemeName()` for the string form. `ThemeControl.Current` is a `ClientReactive<Theme>` bindable in views; `ToggleAsync`/`SetAsync` flip the calling client and push the change to it. By default a joining client that already has a saved theme keeps it (`followClient: true`).

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

private readonly UserReactiveList<string> _cart =
    new(userId => LoadCart(userId));
```

### Reactive Collections: ReactiveList and ReactiveDictionary

List and dictionary state goes in `ReactiveList<T>` / `ReactiveDictionary<TKey, TValue>` — not in a `Reactive<T>` wrapping a mutable collection (that shape is build warning IKON002: in-place mutations bypass change detection). Every mutation method is one change notification, and reads (`Count`, indexer, enumeration, `ContainsKey`, …) are tracked so the UI re-renders on change:

```csharp
private readonly ReactiveList<TodoItem> _todos = new();
private readonly ReactiveDictionary<string, int> _scores = new();
```

Every mutation notifies once, from anywhere in the app:

```csharp
_todos.Add(item);                    // also: AddRange, Insert, Remove, RemoveAt,
_todos.RemoveAll(t => t.Done);       // RemoveAll, Clear, ReplaceAll, Sort
_todos.Update(list => list.OrderBy(t => t.Priority));  // whole-list transform, one notification

_scores["anna"] = 10;                // add-or-replace, one notification
_scores.Update(map => map["anna"]++); // atomic read-modify-write under the lock

foreach (var todo in _todos) { Render(todo); } // enumerate the reactive directly
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
- `view.PanZoom()` - Viewport the user pans and zooms by wheel, pinch and drag; the offset stays client-side

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
- `view.Composer()` - The standard message input bar (`ComposerExtensions`): attachment chips with drag-drop/paste upload, auto-growing text, optional push-to-talk; pending files as `ComposerAttachment` records, per-slot styling and label overrides
- `view.ContentGrid()` - Column-defined grid taking `ContentGridColumn[]`, for card and media layouts a `DataTable` would over-structure
- `view.Paginate()` - Not a component: slices a list against a field-level `ClientReactive<int>` page and returns a `Page<T>` snapshot. Style the controls you draw with the `Pagination` slots

**Editors:**
- `view.CodeEditor()` - Syntax-highlighted code with a language badge, line numbers and Ctrl+Enter `onSubmit`; a controlled `value` with no write-back handler renders read-only
- `view.RichTextEditor()` - HTML-valued editor whose toolbar you can narrow with `RichTextTool` (`Bold`, `Italic`, …); same read-only rule as `CodeEditor`
- `view.ImageEditorCanvas()` - Brush, shape and text annotation over an image, driven by `ImageEditorTool`; `onSave` yields `ImageEditorSaveArgs` and `onHistoryChange` an `ImageEditorHistoryArgs` for undo/redo affordances
- `view.ShadertoyCanvas()` - A GLSL fragment shader as a live surface, with named `ShaderUniform` values and optional pointer input

**Command and menus:**
- `view.CommandPalette()` - ⌘K palette over `SelectOptionGroup` groups, with its own search field and empty state; prefer it over hand-building a `Dialog` for jump-to menus
- `view.Kbd()` - Keycap glyphs, from `text` or a `keys` list
- `view.Menubar()` - Application menu bar, built from `MenubarMenu` / `MenubarTrigger` / `MenubarContent` / `MenubarItem`, plus `MenubarCheckboxItem`, `MenubarRadioGroup` / `MenubarRadioItem`, `MenubarSub` / `MenubarSubTrigger` / `MenubarSubContent`, `MenubarSeparator` and `MenubarItemIndicator`
- `view.ToolbarButton()` / `view.ToolbarLink()` / `view.ToolbarSeparator()` / `view.ToolbarToggleGroupSingle()` / `view.ToolbarToggleGroupMultiple()` / `view.ToolbarToggleItem()` - Toolbar parts
- `view.ToggleGroupSingle()` / `view.ToggleGroupMultiple()` with `view.ToggleGroupItem()` - Segmented single- or multi-select
- `view.NavigationMenuIndicator()` / `view.NavigationMenuViewport()` - The moving indicator and shared viewport of a navigation menu

**Media and feeds:**
- `view.FeedScroller()` - Full-height snap feed over `FeedSlide` items, with `FeedMediaKind` media, preload windows and an `onScrollNearEnd` hook for paging
- `view.FilePicker()` - Native file chooser wrapping your own trigger content; `onFileSelected` yields `FilePickerSelectedArgs` and rejections arrive as `FilePickerValidationErrorArgs`
- `view.AudioUrlPlayer()` - Plain audio playback over a URL, for the cases that need no mixer
- `view.AvatarImage()` / `view.AvatarFallback()` - Avatar image with an initials fallback

**Tables and forms, part by part:**
- `view.TableHeader()` / `view.TableBody()` / `view.TableRow()` / `view.TableHead()` / `view.TableCell()` - Hand-built tables, for what `DataTable` does not cover
- `view.FormControl()` / `view.FormLabel()` / `view.FormMessage()` - Field parts that wire label, control and validation message together
- `view.SliderRange()` / `view.SliderThumb()` - Slider parts, for a two-thumb range
- `view.OtpField()` with `view.OtpFieldInput()` - One-time-code entry, with `autoSubmit` on the last digit
- `view.PasswordToggleField()` with `view.PasswordToggleFieldInput()` / `view.PasswordToggleFieldToggle()` / `view.PasswordToggleFieldIcon()` - Password field with a reveal toggle

**Drag and drop:**
- `view.SortableContext()` with `view.SortableItem()` and `view.SortableHandle()` - Reorderable lists; `SortStrategy` picks the axis and reorders arrive as `SortableReorderArgs`

**Structure:**
- `view.Routed()` - Renders one of a `Dictionary<T, Action<UIView>>` cases by a `ClientReactive<T>` signal
- `view.DirectionProvider()` - Sets `Dir` (LTR/RTL) for the subtree
- `view.VisuallyHidden()` - Content for screen readers only
- `view.AccessibleIcon()` - An icon with a required `label`, so it is not silent to assistive technology

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
```

```csharp
// In UI.Root, mount exactly once:
view.ToastHost(_toasts);

// From any handler:
_toasts.Success("Saved");
_toasts.Error("Upload failed", ex.Message);
```

### Forms and Dialogs with FormState

`FormState<T>` holds everything one editing form needs per client — open, busy, dirty, the draft, and form-level and field-level errors — so a dialog is one declared field rather than a hand-written set of `ClientReactive` flags. Declare it as an app field with a factory for a fresh draft; the composites `FormDialog`, `FormField`, `FormError` and `FormSubmit` bind Dialog, Form, FormField and a submit Button to it:

```csharp
private sealed record PresetDraft(string Name = "", bool Public = false);
private readonly FormState<PresetDraft> _preset = new(() => new PresetDraft());
```

```csharp
// Open on a fresh draft, or on a copy of the record being edited:
_preset.Show();
_preset.Show(new PresetDraft(existing.Name, existing.IsPublic));

// In the UI:
view.FormDialog(_preset, title: "New preset", content: form =>
{
    form.FormField(_preset, "Name", content: f =>
        f.TextField(value: _preset.Draft.Name,
            onValueChange: v => { _preset.Edit(d => d with { Name = v }); return Task.CompletedTask; }));
    form.FormError(_preset);
    form.FormSubmit(_preset, "Save", SavePresetAsync,
        validate: d => string.IsNullOrWhiteSpace(d.Name) ? [new FormFieldError("Name", "Required")] : []);
});
```

`SubmitAsync` runs the validator, then the handler under the busy flag, and closes on success. A `FormException` thrown from the handler becomes a field or form-level error; any other exception becomes a form-level error with its message, and the form stays open with the draft intact.

## Styling with Crosswind

Three layers compose freely in the same style array:

1. **Semantic theme-aware classes** (`bg-card`, `text-primary`, `bg-brand-solid`, `border-secondary`, `bg-background`) — the default path. They resolve through CSS variables that flip between light and dark and respond to per-app `IkonTheme` overrides, so re-theming never touches individual style arrays.
2. **`Ikon.Parallax.Theming` token classes** (`Button.PrimaryMd`, `Card.Default`, `Text.H1`, `Layout.Page`, `Tone.Solid`, …) — pre-composed bundles of the semantic classes with tested defaults.
3. **Hardcoded palette classes** (`bg-amber-400`) and raw hex — for looks that intentionally should not change with the theme. These bypass the theme system entirely.

```csharp
view.Button([Button.PrimaryMd, "mt-4 self-center"], text: "Submit", onClick: SubmitAsync);
view.Box(["bg-card border border-secondary p-6 rounded-2xl"], content: v => { });
view.Text([Text.Caption], text: "Updated just now");
```

### Merge Semantics: the `default` Marker

**The merge is opt-in.** A `style:` array is *exactly* what renders — the themed default is **not** added underneath it. Omit `style:` entirely and you get the full themed control; pass `["w-full"]` and you get an element whose only class is `w-full`.

```csharp
view.TextField(bind: _name);                        // fully themed input
view.TextField(["default", "w-full"], bind: _name); // themed input, full width  ← what you usually want
view.TextField(["w-full"], bind: _name);            // an unstyled box that is full width
```

The marker is what asks for the theme — with it the base merges *under* what you wrote, without it there is no merge at all:

- **`default`** — put the literal `"default"` first and the component's themed base is merged **under** your classes: `["default", "h-14"]` is the themed input at your height. Only `display` and `position` classes are *replaced* rather than added to (a caller `flex` genuinely drops the base's `inline-flex`); everything else is layered.

- **Theme token composites** (`Button.PrimaryMd`, `Input.Default`, `Card.Interactive`, `Badge.SuccessMd`, …) are already complete component styles, so they need no marker: `[Button.OutlineMd]` renders exactly the outline button, and `[Button.OutlineMd, "mt-2 w-full"]` layers on top of it. Fragments (`Button.Base`, `Layout.*`, `Text.*`) are partial and are meant to be combined.

**Order inside the array decides conflicts.** The whole array compiles to a single CSS declaration block in array order, and a repeated property keeps its last value — no specificity, no stylesheet ordering, just "later wins". So `[Button.GhostMd, Button.IconSm]` is a 32px square (the size token's `px-4`/`min-h-10` is overridden by the square's `p-0`/`min-h-0`), and the same two tokens the other way round are a 40px-high button with 32px of side padding.

**What a bare array costs you.** Writing `view.Button(["px-3 py-1 rounded bg-card"], …)` is legal and sometimes exactly right — but it is the whole style, so the button loses every affordance the base carried:

| Dropped | Symptom |
| --- | --- |
| `disabled:pointer-events-none disabled:opacity-50` | A `disabled:` control looks and reads identical to an enabled one (a dev warning names the call site). |
| `hover:` / `active:` variants | Nothing responds to the pointer. |
| `transition-colors` | State changes snap. |
| `whitespace-nowrap` | A two-word label wraps inside the control. |
| The variant's colours | Intentional when you are restyling — surprising when you only meant to add a margin. |

Cursor and the focus ring are *not* on that list: those come from the platform's button reset, so a hand-styled button still shows a pointer on hover and a visible ring on keyboard focus.

The rule of thumb: **adding to a control → start the array with `"default"`; replacing its look → don't.**

**Slots follow the same rule as `style:`.** A component built from several parts exposes one array per part —
`triggerStyle:`, `contentStyle:`, `dayStyle:`, `toolbarStyle:`, `gutterStyle:` and so on. Each carries its own
themed default, each renders exactly what you pass, and each takes `"default"` to merge the theme back under it.
Style one part and the rest stay themed:

```csharp
view.DatePicker();                                        // fully themed trigger + popover + calendar
view.DatePicker(triggerStyle: ["default", "w-full"]);     // themed trigger, full width; popover untouched
view.DatePicker(triggerStyle: ["w-full"]);                // an unstyled trigger; popover still themed
```

A slot you never mention is themed, so a popover keeps the background, border, shadow and stacking that make it
readable over the content behind it — leaving `contentStyle:` out is not the same as asking for a bare one.

Note that `style:` on a composite is the **outermost container**, not the control: on `DatePicker`, `TimePicker`,
`ColorPicker` and `Select` it is the wrapper, and the thing that looks like the control is `triggerStyle:`.

**Some bases always apply.** A handful of components carry classes they cannot function without — the layout
primitives (`Row`, `Column`, `Grid`, `Stack`), `ScrollArea` and its viewport/scrollbar/thumb, `Select`'s parts,
`ResizableSplit`, and size classes a component derives from its own parameters (`Spinner`, `Skeleton`, `Icon`).
There your array is *appended* to that base rather than replacing it, and later classes win:

```csharp
view.Column(["gap-4"]);                    // flex flex-col gap-4 — the flex base is not droppable
view.ScrollArea(viewportStyle: ["px-8"]);  // h-full w-full px-8 — the viewport still fills, and scrolls
```

You never need `"default"` on these, and you cannot remove the base — override it instead (`min-h-[300px]` beats
the base's `min-h-0` by coming later). The distinction is what the base *is*: a complete look you may replace
(`Button`, `Card`, `Badge`, `Input`, and every slot above) versus structure the part needs to work. Dropping
`h-full` from a scroll viewport would not restyle it — it would stop it scrolling.

### Default Styling and Auto-Composed Indicators

`style:` is optional. When it is omitted, interactive and input controls fall back to their default theme token, so the minimal call renders a complete, themed control:

```csharp
view.Checkbox(bind: _done);
view.Switch(bind: _on);
view.TextField(bind: _text);
view.Button(text: "Submit", onClick: async () => { });
```

Controls that have a visible inner part also compose it automatically when no `content:` is given — Checkbox gets its check indicator, Switch its thumb, Slider its track/range/thumb, Select its trigger and items. You only pass `content:` to customise the inner part. To render a checkbox with no check mark, opt out explicitly with `content: _ => { }`. Layout primitives (`Box`, `Row`, `Column`, `Grid`, `Stack`) carry no *themed* default — no colours, no spacing, no chrome — so "no style" is the normal usage there. They are not blank, though: all but `Box` keep the display class that makes them what they are (`Column` is `flex flex-col`), and that base is not droppable — see [Some bases always apply](#merge-semantics-the-default-marker).

### Icon Buttons

An icon-only button is a square, and squares are where the padding arithmetic bites. A size token brings `px-4` and `min-h-10`, so `["default", "w-7 h-7"]` renders **32×40** — the height wins, the width is padded out, and the row it sits in gets taller than asked. Use a square token, **after** the variant:

```csharp
view.Button([Button.GhostMd, Button.IconSm],   // h-8 w-8 p-0 min-h-0 — last wins
    icon: "refresh-cw",
    tooltip: "Refresh",
    onClick: RefreshAsync);
```

`Button.Icon` (40×40 — the comfortable touch target), `Button.IconSm` (32) and `Button.IconXs` (28) each carry `p-0 min-h-0` for exactly this reason, and each must come after the variant token to win. Measure the rendered box when the size matters: an entrance animation (`scale-75`) or a `pointer-coarse:` rule can make it something other than what the classes say.

### Tooltips and Naming Controls

An icon has no text, so an icon-only control needs a name given to it — twice over, because a sighted mouse user and a screen-reader user read different things:

```csharp
view.Button([Button.GhostMd, Button.Icon],
    icon: "trash-2",
    text: "Delete",       // the accessible name (aria-label in icon mode)
    tooltip: "Delete",    // the visible hover bubble
    onClick: DeleteAsync);
```

- **Never use a `title` prop for a hover name.** That is the browser's own tooltip: unstyleable, unpositionable, ~1s late, and invisible to touch. `Dialog(title:)` is a different thing — an accessible title, not a tooltip.
- `tooltip:` on `Button` is sugar for wrapping it in `Tooltip`. Reach for the `Tooltip` component directly when the trigger is not a Button, or when you need `open:`/`delayDuration:` control.
- **The tooltip wrapper becomes the flex child.** `Tooltip` renders a `span` around its trigger, so layout, responsive and absolute classes belong on the wrapper, not on the button inside it — `hidden lg:inline-flex`, `shrink-0`, `absolute top-2 right-2` left on the button are all pinned inside a static span and do nothing. Pass them as `tooltipRootStyle:` (or `rootStyle:` on `Tooltip`).
- **Nesting inside another overlay:** a tooltipped menu button goes *inside* the Popover's `trigger:` slot, not around the Popover.

Form controls name themselves the same way. `Checkbox`, `Switch` and `Toggle` take `label:`, which wraps control and text in a `<label>` — that association is what makes the text clickable *and* the control's accessible name. A `Text` placed next to a bare control looks identical and associates nothing. `Slider` is the odd one: `role="slider"` lives on the thumb, so its `label:` (or an `ariaLabel:`) is routed there; a name left on the root names nothing. `Checkbox`, `TriStateCheckbox`, `Switch` and `Slider` warn at dev time when they render with no name at all — `Toggle` does not, because its `content:` is usually a label of its own.

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

**`max-h-` is not a height.** A ScrollArea needs a *definite* height to scroll against: `h-[400px]`, `h-full` inside a sized parent, or `flex-1` inside a flex column. `rootStyle: ["max-h-96"]` is the common near-miss — the root grows with its content until the cap, and the viewport inside it, having no resolved height of its own, never overflows. The symptom is a panel that grows past the cap or clips instead of scrolling. If a cap is genuinely what you want, give it a height too: `["h-full max-h-96"]`.

**Canonical recipe — `ScrollArea` inside a flex column:**

```csharp
view.Column(["h-[82vh] flex flex-col"], content: dialog =>
{
    dialog.Row(["items-center px-5 py-4 border-b"], content: header => { });

    dialog.ScrollArea(
        rootStyle: ["flex-1"],              // min-h-0 is injected automatically
        scrollbars: ScrollAreaScrollbars.Vertical,
        content: body => { });

    dialog.Row(["items-center px-3 py-2 border-t"], content: composer => { });
});
```

**Or use the `ScrollColumn` primitive** that wraps the header/body/footer pattern in a single call, so the shape can't be misused:

```csharp
view.ScrollColumn(
    style: ["h-[82vh] w-full sm:max-w-[560px] rounded-2xl bg-card"],
    header: h => h.Row(["px-5 py-4 border-b"], content: title => { }),
    footer: f => f.Row(["p-3 border-t"], content: composer => { }),
    content: body => body.Column(["gap-3"], content: messages => { }));
```

For chat specifically, `view.ChatLog()` wraps `ScrollColumn` with chat-friendly defaults (auto-scroll on).

**Raw `Column`/`Row` with overflow-auto.** If you're not using `ScrollArea`, you still need `min-h-0` (or a fixed height) yourself — the framework fix only applies to the `ScrollArea` component:

```csharp
view.Column(["flex-1 min-h-0 overflow-y-auto", Scrollbar.Thin], content: rows => { });
```

Dev builds (debugger attached or `IKON_DEV_WARNINGS=1`) emit a single `Log.Instance.Warning` when they detect a `Column`/`Row`/`Box`/`Flex` with `overflow-y-auto` + `flex-1` and no `min-h-0` — with the exact `file:line` of the offending callsite.

**Theme the native scrollbar when you scroll a container yourself.** A bare `overflow-auto` shows the OS scrollbar — on Windows a wide grey slab that matches no theme and shifts the layout when it appears. `Theming.Scrollbar.Thin` is the themed thin bar (`Scrollbar.Hidden` removes it entirely, for a strip whose overflow is visually obvious):

```csharp
view.Row(["overflow-x-auto gap-2", Scrollbar.Thin], content: chips => { });
```

It sizes both axes on purpose. Styling only the width leaves a *horizontal* bar at its default height, which is the usual way this lands half-applied — and a horizontally scrolling row is exactly where it is most often needed.

## PanZoom: Viewing Something Larger Than the Screen

`view.PanZoom()` (`PanZoomExtensions`) is a clipped viewport whose content the user moves and scales in the client: scrolling pans, Ctrl/⌘+scroll or a pinch zooms about the pointer, dragging pans. The pan offset and every in-gesture frame stay in the browser; the app hears only the scale at the end of a gesture, so a wheel burst or a pinch costs one round trip rather than one per event. Use it for a floor plan, a board of cards, a diagram, a large image — anything laid out at its natural size that has to be *looked at* rather than edited.

```csharp
view.PanZoom(
    ["h-96 w-full rounded-lg border border-secondary bg-secondary"],
    scale: _scale.Value,
    minScale: 0.25,
    maxScale: 4,
    onScaleChange: async scale => _scale.Value = scale,
    content: canvas =>
    {
        canvas.Box(["w-[1600px] p-6 flex flex-wrap gap-4"], content: sheet => { /* the large thing */ });
    });
```

Give the viewport a size with its style array — it clips, so without a height it collapses. `scale:` + `onScaleChange:` is the controlled pair, which is what lets preset buttons (`_scale.Value = 1`) drive the view; a server-written scale zooms about the viewport centre. Omit both and pass `defaultScale:` for a purely client-side zoom. A `scale:` with no handler renders read-only, like every other controlled axis.

What it is not: an editor surface. Nothing inside a `PanZoom` knows it is scaled — a `DndContext` or a click handler in the content still works, but its coordinates are screen-space. For a canvas with selection, marquee or drag in document space, build a custom node with its own `.tp` transport (see `custom-map-component-guide.md`); `PanZoom` is the viewer, not the workbench.

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

The platform captures an app's **initial UI at build time** and ships it as a static snapshot asset, so the first paint appears instantly — before the WebSocket connects. Capture is **on by default**: `[BootSnapshot] Routes` lists the routes to snapshot (scaffolded as `["/"]`), and `ikon app bundle` / `ikon app deploy` renders the app once per route for a synthetic snapshot client and serializes the resulting UI tree. An empty list disables boot snapshotting.

```toml
[BootSnapshot]
Routes = ["/"]   # the default; [] disables boot snapshotting
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

### Public pages: opting a whole page out of skeletonization

A public landing-type page — marketing copy, docs, pricing — has nothing to protect, and skeletons only cost it SEO content. There is deliberately no config switch for this (the privacy decision belongs next to the content it exposes): wrap the **page root** in a single `SnapshotReveal` and the whole page renders for real in the snapshot. The proven shape is a per-page wrapper, as in Studio's guest pages:

```csharp
private static void RenderGuestPage(UIView view, Action<UIView> content)
{
    // The whole guest page is public marketing content — safe to reveal in the snapshot. Never
    // route per-user data through this wrapper.
    view.SnapshotReveal(v => v.Column(["min-h-screen"], content: content));
}
```

Everything routed through the wrapper is real HTML for crawlers and an instant real first paint for visitors; every other page in the app keeps the skeleton default. The capture client is an anonymous guest, so only content an anonymous guest may see can ever pass through it — keep it that way.

### Hand-built skeletons

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

### Per-route snapshots and SEO

Beyond the single boot view, an app can declare **public routes** to snapshot individually. Each declared route is captured with its own synthetic client (connected with that route as its initial path), rendered to its own snapshot — and, at bundle time, **prerendered to static HTML** through the same React component pipeline the browser uses. The gateway serves that HTML **to crawlers** for the route's URL, so search engines and link-preview fetchers get real, styled, content-bearing markup (whatever `SnapshotReveal` opts in). **Human visitors** always get the SPA index instead: the SDK seeds the route's snapshot JSON (the bundle bakes a route→snapshot map into the index) for an instant, pixel-faithful first paint, and the live app takes over seamlessly when the WebSocket connects. A `sitemap.xml` and `robots.txt` are generated from the same route set at deploy time (an app-shipped `public/robots.txt` or `sitemap.xml` wins).

```toml
[BootSnapshot]
Routes = ["/", "/pricing", "/about"]     # static routes to snapshot
SignedInSeeds = ["/**:shell"]            # optional seed rules; see below
```

Content-driven routes (one per store listing, article, …) are declared in app code and unioned with the static list at capture time:

```csharp
app.OnSnapshotRoutes(async () => (await store.GetListingsAsync()).Select(l => $"/listing/{l.Id}"));
```

Two capture-quality tools:

- **Settle signal** — capture treats a quiet UI stream as "settled" and additionally listens for an explicit ready signal; whichever arrives first wins, and everything is bounded by a per-route cap. A route whose content loads asynchronously after a silent gap could quiesce too early and bake its loading skeleton into the snapshot — call `ClientFunctions.SnapshotReadyAsync()` when the route's content is loaded and capture snapshots at exactly that moment. Nothing to configure; the call is a harmless no-op for normal browser clients.
- **Redirect detection** — a route that navigates elsewhere during capture (e.g. bouncing to a login view) is never captured under the wrong URL: the capture fails with an error naming the route and where it settled, and the bundle/deploy fails with it. Declared routes must be paths the app serves directly — if the app rewrites `/` to `/home` on load, either render the page at `/` without rewriting or declare `/home` instead (and fix `OnSnapshotRoutes` results the same way).

Routes must be app-owned paths: `/`-prefixed, no query/fragment, not under the platform-reserved `/ikon` or `/api` prefixes. Prerendered crawler HTML (and the sitemap built from it) requires the app to be openable without login (`[Auth]` disabled, or `guest` among the methods) — serving marketing HTML to crawlers in front of a hard login wall is a cloaking pattern, so for login-only apps the bundle skips the prerender and ships the JSON snapshots alone (the instant skeletonized first paint still works).

#### How to use it

Reach for per-route snapshots when an app has **public, content-bearing pages that should rank in search** — a marketing home page, pricing/about pages, a storefront's product pages, a blog's articles. It does nothing for a signed-in dashboard (that content is skeletonized and gated behind login), so enable it only on the public surface.

1. **Make the public routes openable without signing in.** In `ikon-config.toml`, either leave `[Auth] RequireSignIn = false` (the default), or set it with `guest` or `global` in `Methods` so a crawler can connect without a login wall. If neither holds, the bundle skips the crawler HTML and ships JSON snapshots only.

2. **List the static routes.** Enumerate the fixed public paths (the scaffold starts you at `["/"]`):

   ```toml
   [BootSnapshot]
   Routes = ["/", "/pricing", "/about"]
   ```

3. **Add content routes in app code** (optional). For pages generated from data — one per listing, article, or profile — return them from `OnSnapshotRoutes`. They are unioned with the static list and de-duplicated, then capped at 50 routes per bundle:

```csharp
app.OnSnapshotRoutes(async () =>
    (await store.GetPublishedArticlesAsync()).Select(a => $"/blog/{a.Slug}"));
```

   The provider runs on the machine doing the bundle/deploy, so the captured set is as fresh as your last deploy — re-deploy to pick up new content.

4. **Decide what's public per route.** Capture skeletonizes everything by default. Wrap the parts that are safe and meaningful for a crawler — the headline, body copy, product name/price, hero image — in `SnapshotReveal` so they render as real HTML. Anything left unrevealed ships as a skeleton and contributes nothing to SEO. Use `SnapshotHide` for controls that are dead before the socket connects, and `SnapshotOnly` for snapshot-specific filler. Sensitive or per-user content should stay skeletonized — never `SnapshotReveal` it.

5. **Handle async content.** If a route paints its real content only after an async load (a fetch, a DB read), the quiescence timer could settle on the loading skeleton. Call `ClientFunctions.SnapshotReadyAsync()` once the route's content is in place — capture races that signal against quiescence automatically and snapshots as soon as either arrives. It's a no-op for live browser clients, so it's safe to leave in.

   If the app registers **custom UI modules** (`useIkonApp({ modules })`), the prerender needs them too or those elements render as skeletons in the static HTML: create `src/prerender-modules.ts` in the frontend exporting `prerenderModules: IkonUiModuleRegistration[]` with the same registrations, and the prerender build picks it up automatically.

6. **Bundle and verify.** Run `ikon app bundle` (locally built ikon tool). In `build/bundle/frontend-node/` you'll find a per-route `boot-snapshot-*.json`, `ikon/routes/*.html`, and `route-manifest.json`. **Open an `ikon/routes/*.html` file with JavaScript disabled** — the revealed content and its styles should be visible with no "JavaScript is required" notice. That is exactly what a crawler sees.

7. **Deploy.** `ikon app deploy` generates `sitemap.xml` and `robots.txt` from the route set and serves each route's prerendered HTML from the gateway. To override the defaults, ship your own `public/robots.txt` or `public/sitemap.xml` — an app-provided file always wins. Per-route `<title>` is derived from the route today; a full per-route meta/OG API is the natural follow-up.

**Preview a route's snapshot without a rebuild** the same way as the boot snapshot: open the running app at that path with `?ikon-snapshot=true` to render the capture path in your browser, confirming what's revealed and that nothing sensitive leaks.

#### Seed rules and snapshot variants (`GuestSeeds` / `SignedInSeeds`)

Route snapshots cover concrete paths that exist at deploy time. Two situations need more:

- **Identity-split pages** — the deferred-login pattern where the same path is a guest landing for anonymous visitors and a personal hub for signed-in users. The route snapshots depict the app's **public** entry views, so a signed-in session must not paint them; with nothing else cached, a fresh sign-in would stare at a blank page until the instance boots.
- **Dynamic paths** — user-created content like `/myapp/my-workshop`, whose slugs exist only in the database. No concrete route can be captured for a slug created after the deploy, so a visitor deep-linking there has nothing to seed.

Seed rules cover both. Each entry is `"pattern:variantId"` (the same colon-separated shape as `Databases`), listed per login state, **first match wins** in array order:

```toml
[BootSnapshot]
Routes = ["/"]
GuestSeeds = ["/**:welcome"]
SignedInSeeds = ["/:admin", "/*:dashboard", "/*/**:experience"]
```

A pattern segment is a literal (exact match), `*` (exactly one segment, any content), or a final `**` (zero or more remaining segments). So `/` matches only the front page, `/*` any one-segment path, `/*/**` any path two segments or deeper, and `/**` everything. Order matters — `**` means *zero* or more, so `/*/**` also matches one-segment paths; listing `/*:dashboard` first keeps those on the dashboard rule. A `/**` entry anywhere but last fails the bundle (the entries after it could never apply).

On load, the SDK picks the array from the stored-session heuristic (a non-anonymous localStorage session, or an OAuth callback token), then resolves per axis: **guests** seed an exact route snapshot first — real public content beats a generic fallback skeleton — and consult `GuestSeeds` for everything unmapped; **signed-in** visitors consult `SignedInSeeds` first — the rules exist precisely to override the public route snapshots — and fall back to the exact route snapshot, so an app with no rules still paints (route snapshots are skeletonized and identity-free).

Every distinct variant id becomes **one skeleton capture**: a capture client connects with the id in `Context.SnapshotVariant`, and the app branches to the matching skeleton — no magic paths, the toml ids are the `case` labels:

```csharp
if (view.IsSnapshot)
{
    switch (view.SnapshotVariant)
    {
        case "admin":      RenderAdminPanelSkeleton(view); break;
        case "dashboard":  RenderDashboardSkeleton(view);  break;
        case "experience": RenderExperienceSkeleton(view); break;
        default:           RenderWelcomeSkeleton(view);    break;   // "welcome" + route captures
    }

    return;
}
```

Because nothing in a variant skeleton is wrapped in `SnapshotReveal`, the whole capture skeletonizes into neutral blocks automatically: an app-shaped skeleton with no user data by construction. Render it from **local placeholder data** (a fixed heading, a few empty cards) rather than the app's real reactives — the capture client is unauthenticated, and the skeleton only needs the right geometry. Variant artifacts ship separately from the public routes: they are **not** prerendered to HTML and never appear in the route manifest or sitemap (a skeleton standing for unboundedly many URLs is not crawlable content — concrete SEO stays with `Routes` and `OnSnapshotRoutes`). A returning user's live-snapshot cache of their own last UI still wins over any seed rule. A declared variant the capture cannot produce fails the bundle/deploy, exactly like a missing route.

The old single-shell pattern is the simplest rule set: `SignedInSeeds = ["/**:shell"]` seeds one hub skeleton on every path for signed-in visitors, and `view.SnapshotVariant == "shell"` renders it.

### Open-as-guest (the default)

A login-gated app blocks the connection behind its sign-in screen — which also means its landing content can't be a server-drawn page, and the SEO pipeline above has nothing to capture. Leaving `RequireSignIn` off inverts that: visitors connect **immediately as an anonymous session**, the app decides what they see, and real sign-in happens on demand.

```toml
[Auth]
RequireSignIn = false            # the default — no sign-in wall
Methods = ["google", "guest"]    # "guest" is what visitors connect as
```

The two not-signed-in flavors differ in who shares an identity. `guest` mints a device-scoped anonymous user per visitor, so each drive-by gets their own `UserId` and their own app instance. `global` puts every visitor on one space-wide shared anonymous user — they collapse onto a single instance with no per-visitor database writes, which is the cost-efficient choice for a public landing surface. List both to enter as `global` by default and upgrade a visitor with `login('guest')` when they need an identity of their own.

The frontend establishes the session on first visit (no login wall, no click) and connects. On the server, `Context.IsAnonymous` distinguishes not-signed-in visitors from signed-in users — the authoritative flag; a guest still carries a valid device-scoped `UserId`. `Context.IsGlobal` tells you which flavor you are serving. The typical shape is a branch at the top of the UI root:

```csharp
UI.Root([Page.Default], content: view =>
{
    if (_isGuest.Value || view.IsSnapshot)   // _isGuest: ClientReactive set from Context.IsAnonymous at join
    {
        RenderLanding(view);                 // public marketing page, wrapped in SnapshotReveal
        return;
    }
    RenderApp(view);                         // the signed-in product
});
```

Trigger sign-in from the server-drawn landing with the client login primitive:

```csharp
view.Button([Button.PrimaryMd], text: "Sign in with Google",
    onClick: async () => await ClientFunctions.LoginAsync("google"));
```

`LoginAsync` starts the client's OAuth redirect for the given provider (`google`, `microsoft`, …); the user returns authenticated and the client reconnects with its real identity — the anonymous session is simply abandoned. Guest, global, email and passkey flows stay client-initiated. Call it from event handlers only (like all client functions), never from the render pass.

Guard your authed-only paths: skip user-backend calls, per-user persistence, and deep-link view restoration for anonymous sessions — a guest must not be able to navigate into the signed-in surface by URL. This matters more under `global`, where every visitor shares one `UserId`: never key per-user state on it.

**This is how an app with sign-in gets a crawlable landing page**: combine open-as-guest with `[BootSnapshot] Routes = ["/"]`. The capture client connects as an anonymous session, renders the same landing, and the SEO pipeline prerenders it to static HTML — crawlable markup, instant first paint from the static file, and the live session connecting invisibly underneath, taking over pixel-identically.

## Architecture Summary

1. **Server-side logic**: All UI logic, state, and event handlers run on the server
2. **Reactive updates**: Changes to reactive values trigger targeted re-renders
3. **Differential sync**: Only UI diffs are sent to clients
4. **Scoped state**: `ClientReactive<T>` / `UserReactive<T>` / `MountReactive<T>` (and their `ReactiveList` / `ReactiveDictionary` variants) give per-client, per-user, and per-mount state from the same UI code
5. **Lightweight clients**: Clients render the UI tree and forward events to the server
6. **Themed components**: Every styled component ships a themed default, rendered when `style:` is omitted; a `style:` array replaces it unless the array asks for the merge with the `"default"` marker
7. **Crosswind styling**: Tailwind-compatible utility classes with motion extensions
8. **Snapshot privacy**: the boot snapshot is skeletonized by default; `SnapshotReveal` opts safe content back in, `SnapshotHide` / `SnapshotOnly` cover the rest
