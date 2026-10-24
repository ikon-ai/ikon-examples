<!-- checked-against: 56525cec26087ad987337412 -->

# Ikon.Parallax Library Overview

## Introduction

Ikon.Parallax is a server-driven, reactive UI library for building interactive applications in C#. The library provides a declarative API for constructing user interfaces where all logic runs on the server, clients only render, and the framework updates clients by diffing the UI tree.

The name "Parallax" reflects the library's core capability: different clients can receive different views of the same underlying UI based on reactive scopes, similar to how parallax creates different viewpoints of the same scene.

## Core Concepts

### Reactive UI Updates

Ikon.Parallax uses the reactive system from `Ikon.Common.Core.Reactive`. When a reactive value changes, only the UI components that depend on that value are re-rendered. The framework tracks dependencies automatically during rendering — reading `_count.Value` inside a UI lambda registers the dependency, so rendering needs no explicit subscription. Code outside rendering can subscribe to `ValueChanged` or `ValueChangedAsync`.

<!-- ikon-example: px-reactive-ui-updates -->
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

<!-- ikon-example: px-themed-components-and-crosswind-styling -->
```csharp
view.Button(text: "Save", onClick: SaveAsync);                  // fully themed as-is
view.Button([Button.PrimaryMd, "w-full"], text: "Save", onClick: SaveAsync);
view.Button(["default", "w-full"], text: "Save", onClick: SaveAsync);   // same: Button's default IS PrimaryMd
```

## Setting Up a UI

Create a `UI` instance with the app host and a theme, and call `Root` to define the UI tree:

<!-- ikon-example: px-setting-up-a-ui -->
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

When `_counter.Value` changes, the Column whose content reads it re-renders (unchanged containers elsewhere are reused from cache), and only the diff — the Text's new text — is sent to clients.

A brand's colours are set in the `IkonTheme` initializer, an indexer-keyed object initializer where each entry sets one theme key. A key such as `primary` or `foreground` expands to a group of tokens. Base overrides restyle the light scheme, so a brand-locked look sets `Mode = ThemeMode.Fixed` (or pairs a `DarkMode`), as this snippet does. A theme left `Adaptive` with no `DarkMode` and with a surface (`background`, `card`, `popover` or `foreground`) pinned logs a half-committed-theme warning:

<!-- ikon-example: px-setting-up-a-ui-2 -->
```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"] = "amber-400",
    ["background"] = "zinc-950",
});
```

The full theme key vocabulary, dark-mode pairing (`IkonTheme.DarkMode`), and `ThemeMode.Fixed` for brand-locked looks are covered in the **IKON Theming Guide** (`theming-guide.md`).

### Light/Dark Switching with UseTheme

`UI.UseTheme()` sets up per-client theme state in one call, so you do not write your own ClientJoined handler and toggle:

<!-- ikon-example: px-light-dark-switching-with-usetheme -->
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

`ThemeExtensions` reads the calling client's choice off a `Context` — `clientContext.IsDarkTheme()`, which is false for the light theme, for custom theme names, and for a client that has not reported one, and `theme.ToThemeName()` for the string form. `ThemeControl.Current` is an `IReadOnlyReactive<Theme>` holding the calling client's theme. You can bind it in views, but it is read-only, because only `ToggleAsync` and `SetAsync` also tell the browser. `ToggleAsync` flips the calling client between dark and light and `SetAsync` sets it. Both update `Current` at once and push the change to that client; for a client that has not yet registered its theme function, the push waits until it does. By default a joining client that already has a saved theme keeps it (`followClient: true`).

### The Order Action Handlers Run In

Handlers run off the message loop, so a slow one never holds up the rest of the app. Calls of one
handler from one client run one after another, in the order they arrived. A call of a different
handler from the same client waits for that client's earlier calls to finish, but for at most
300 ms after each of their handlers started, and never more than 2 s after each of them arrived.
So a Send click sees the change the person made to the text field just before it, and a handler
that runs for seconds, or one that waits for the same person's next click, does not hold up their
other input.

### When an Action Handler Throws

An `onClick`, `onValueChange` or `onSubmit` handler that throws is logged, and the client is told the call failed — but nothing is rendered for the person who clicked unless the app does it. Subscribe `UI.ActionFailedAsync` once in `Main` to show one message for every failed handler instead of wrapping each one:

<!-- ikon-example: px-when-an-action-handler-throws -->
```csharp
private readonly ClientReactive<string?> _actionFailed = new(null);

public async Task Main()
{
    // Runs inside the failed handler's client scope, so the ClientReactive write lands on the
    // person who clicked. Show your own line — the exception's text is for the log.
    UI.ActionFailedAsync += args =>
    {
        _actionFailed.Value = "That change could not be saved — please try again.";
        return Task.CompletedTask;
    };

    UI.Root([Page.Default], content: view =>
    {
        view.Toast(open: _actionFailed.Value != null,
            onOpenChange: async open => _actionFailed.Value = open ? _actionFailed.Value : null,
            title: "Something went wrong", description: _actionFailed.Value ?? "");
    });
}
```

`ActionFailedEventArgs` carries the `Exception`, the `ClientContext`, the `ActionId` and the handler's `CallSite`. An app whose own exception type carries text written for the person can render that message and fall back to the generic line for everything else. The failure is still reported to the client afterwards, and a subscriber that throws is logged without stopping that report. Pair it with handlers that clear their form only after the save succeeded, so the person can retry by pressing the button again instead of retyping.

## Reactive State

### Shared, Per-Client, Per-User, Per-Mount

Reactive values are `private readonly` fields. The scope is picked by type:

| Type | One value per… | Typical use |
|---|---|---|
| `Reactive<T>` | app instance (shared by its clients; `GlobalReactive<T>` shares one across instances) | Shared game state, lobby lists |
| `ClientReactive<T>` | client session | Form input, selected tab, dialog open |
| `UserReactive<T>` | user (shared across their sessions) | Preferences, cart |
| `MountReactive<T>` | Parallax mount | Per-mount chat history |

<!-- ikon-example: px-shared-per-client-per-user-per-mount -->
```csharp
private readonly Reactive<int> _sharedCounter = new(0);
private readonly ClientReactive<string> _draft = new("");
private readonly UserReactive<string> _language = new("en");
```

In UI lambdas and action handlers you read and write `.Value`, and the active scope picks the right per-client or per-user value. This is the "parallax" effect: the same UI code produces different views for different clients.

To seed each scope's initial value from its id, `ClientReactive` and `MountReactive` have a static `Create` factory, and `UserReactive`, `UserReactiveList`, `ClientReactiveList` and `MountReactiveList` a seeding constructor (a fixed initial list shares its items by reference across scopes, so seed mutable items per scope):

<!-- ikon-example: px-shared-per-client-per-user-per-mount-2 -->
```csharp
private readonly ClientReactive<string> _welcome =
    ClientReactive.Create(sessionId => $"Welcome, session {sessionId}!");

private readonly UserReactiveList<string> _cart =
    new(userId => LoadCart(userId));
```

### Reactive Collections: ReactiveList and ReactiveDictionary

List and dictionary state goes in `ReactiveList<T>` / `ReactiveDictionary<TKey, TValue>` — not in a `Reactive<T>` wrapping a mutable collection (that shape is build warning IKON002, an error in the codegen build and in-repo apps: in-place mutations bypass change detection). Every mutation method is one change notification, and reads (`Count`, indexer, enumeration, `ContainsKey`, …) are tracked so the UI re-renders on change:

<!-- ikon-example: px-reactive-collections-reactivelist-and-reactivedictionary -->
```csharp
private readonly ReactiveList<TodoItem> _todos = new();
private readonly ReactiveDictionary<string, int> _scores = new();
```

Every mutation notifies once. Make the changes in a handler or in background work, because a change made while the UI renders does not re-render it:

<!-- ikon-example: px-reactive-collections-reactivelist-and-reactivedictionary-2 -->
```csharp
view.Button(text: "Tidy up", onClick: () =>
{
    _todos.Add(item);                    // also: AddRange, Insert, Remove, RemoveAt,
    _todos.RemoveAll(t => t.Done);       // RemoveAll, Clear, ReplaceAll, Sort
    _todos.Update(list => list.OrderBy(t => t.Priority));  // whole-list transform, one notification

    _scores["anna"] = 10;                // add-or-replace, one notification
    _scores.Update(map => map["anna"]++); // atomic read-modify-write under the lock
});

foreach (var todo in _todos) { Render(todo); } // the render enumerates the reactive directly
```

Both come in the same scoped variants as the scalars: `ClientReactiveList<T>` / `UserReactiveList<T>` / `MountReactiveList<T>` and `ClientReactiveDictionary<TKey, TValue>` / `UserReactiveDictionary<TKey, TValue>` / `MountReactiveDictionary<TKey, TValue>`.

### Background Work: the *For Methods

`.Value` works wherever the scope is active — inside `UI.Root()`, an action callback, or a `ReactiveScope.Use(new ClientScope(...))` block. A `Task.Run` started inside a scoped callback inherits that scope, but work started outside any scope (a loop started from `Main`, a timer, an endpoint handler) carries no client scope, so `.Value` there throws rather than writing to nowhere. Name the target instead with the `*For` methods, capturing the id while the scope is still active:

<!-- ikon-example: px-background-work-the-for-methods -->
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

Async handlers that flip a busy flag and show failures in the UI use the helpers instead of hand-rolled try/catch/finally:

<!-- ikon-example: px-the-busy-status-pattern -->
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

`RunAsync` clears the status, holds the busy flag for the duration of the work (it always returns to false, even on failure), routes an exception's message into the status reactive, and returns whether the work completed. An `OperationCanceledException` is not routed: it is rethrown to the caller after the busy flag resets. For a busy flag alone, `using var busy = _busy.AsToken();` covers the flag-with-guaranteed-reset half of the pattern.

### Persistent State

`PersistentReactive<T>` (the default choice) and `PersistentUserReactive<T>` keep an instance's values, and `PersistentGlobalReactive<T>` and `PersistentGlobalUserReactive<T>` keep one value for every instance of the app (the user form one per user), all across restarts and deploys. Each has `List`, `Dictionary` and `HashSet` forms with the same reactive API. See the **IKON Persistent State Guide** (`persistent-state-guide.md`).

## Components

The `UIView` class provides extension methods for UI components. One shape for most calls: the leading `string[]` style array is the only positional argument, everything else is named. Components built around required data take that first and the style after it (`view.DataTable(columns, rows, …)`, `view.CommandPalette(groups, …)` and the like — check the signature). `view.Badge`, `view.Alert`, `view.StatCard` and `view.EmptyState` take either: the data first (`view.Badge(text, tone, …)`) or the style first like the rest (`view.Badge([Badge.SuccessSm], text: "Live")`). `view.Text` / `view.Button` / `view.Heading` / `view.Icon` / `view.Markdown` also accept their text positionally first.

**Layout:**
- `view.Row()` / `view.Column()` / `view.Box()` / `view.Grid()` - Flex and grid containers
- `view.ScrollArea()` - Scrollable container with optional smart auto-scroll
- `view.ScrollColumn()` - Header/body/footer column where the body scrolls
- `view.VirtualList()` / `view.VirtualGrid()` - Long lists whose rows the browser mounts only near the viewport. The server still builds and sends every row, unless given a `scrollIndex` (a `ClientReactive<int>`), with which a VirtualList builds about 200 rows around the first visible one and a VirtualGrid about 480 items around the first visible item
- `view.InfiniteScrollView()` - Scroll area with near-end callbacks for lazy loading
- `view.PanZoom()` - Viewport the user pans and zooms by wheel, pinch and drag; the offset stays client-side

**Display:**
- `view.Text()` / `view.Heading()` / `view.Markdown()` - Text content; `view.Heading(level: 2)` renders `<h2>` (1 to 6); without `level:` a `Text.H1`…`Text.H6` token in the style sets it, else `<h1>`
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
- `view.DataTable()` - Paginated tables with typed cells, row actions, column resize and a `selectedRowId` row shown as selected (per-slot styling via the `DataTableStyles` record on `styles:`)
- `view.BarChart()` / `view.LineChart()` / `view.PieChart()` - Interactive charts
- `view.ChatLog()` - `ScrollColumn` with auto-scroll on and optional header/footer slots (pass your composer as the footer)
- `view.Composer()` - The standard message input bar (`ComposerExtensions`): attachment chips with drag-drop/paste upload when `onAttachmentStart` or `onAttachmentAdded` is wired, auto-growing text, optional push-to-talk; pending files as `ComposerAttachment` records, per-slot styling and label overrides
- `view.ContentGrid()` - A grid with fixed columns (`ContentGridColumn[]`: header, width or `Flex`, alignment) that the content fills cell by cell, row after row: a comparison table, a sheet of aligned values, a card or media grid. A `DataTable` adds typed cells, paging and row actions
- `view.Paginate()` - Not a component: slices a list against a field-level `ClientReactive<int>` page and returns a `Page<T>` snapshot. Style the controls you draw with the `Pagination` slots

**Editors:**
- `view.CodeEditor()` - A monospace text area with a line-number gutter, a `language` badge and Ctrl+Enter `onSubmit`, named by `label:` (shown above it) or `ariaLabel:`. It highlights no syntax. `readOnly: true` shows code without editing; a controlled `value` with no write-back handler also renders read-only, with a dev warning
- `view.RichTextEditor()` - HTML-valued editor whose toolbar you choose with `RichTextTool` (`Bold`, `Italic`, …). `FontSize`, `TextColor` and `Image` are not in the default toolbar; name them in `tools:` to use them, and the value keeps a colour, size or image only while its tool is there. Same `readOnly` and read-only rule as `CodeEditor`
- `view.ImageEditorCanvas()` - Brush, shape and text annotation over an image, or a blank pad the size of its container when there is no `src`, driven by `ImageEditorTool`; `onSave` yields `ImageEditorSaveArgs` (with `MaskData`, the changed area as a white-on-black mask, under `saveMask: true`) and `onHistoryChange` an `ImageEditorHistoryArgs` for undo/redo affordances
- `view.ShadertoyCanvas()` - A GLSL fragment shader as a live surface, with named `ShaderUniform` values and optional pointer input. `onCompileError` gets the WebGL log, with line numbers counted in your source; `onCompile` fires when a new source is drawing

**Command and menus:**
- `view.CommandPalette()` - ⌘K palette over `SelectOptionGroup` groups, with its own search field and empty state. It binds no shortcut and narrows only by the `searchValue:` it is given: the app drives `open:`/`onOpenChange:` (wire ⌘K yourself, e.g. with a `KeyboardListener`) and feeds `searchValue:`/`onSearchChange:`; prefer it over hand-building a `Dialog` for jump-to menus
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
- `view.OtpField(maxLength: 6)` (boxes drawn for you; `view.OtpFieldInput()` children lay out your own) - One-time-code entry, with `autoSubmit` on the last digit
- `view.PasswordToggleField()` with `view.PasswordToggleFieldInput()` / `view.PasswordToggleFieldToggle()` / `view.PasswordToggleFieldIcon()` - Password field with a reveal toggle

**Drag and drop:**
- `view.SortableList()` - Reorderable list; `SortStrategy` picks the axis and reorders arrive as `SortableReorderArgs` in `onReorder`
- `view.SortableContext()` with `view.SortableItem()` and `view.SortableHandle()` - The raw primitives, inside a `view.DndContext()`; reorders arrive as `DragEndArgs` in the context's `onDragEnd`

**Structure:**
- `view.Routed()` - Renders one of a `Dictionary<T, Action<UIView>>` cases by a `ClientReactive<T>` signal
- `view.DirectionProvider()` - Sets `Dir` (LTR/RTL) for the subtree
- `view.VisuallyHidden()` - Content for screen readers only
- `view.AccessibleIcon()` - An icon that carries a `label` for assistive technology. Nothing enforces it: `label:` is optional and defaults to null, and an omitted one renders as an empty name — so the icon is silent exactly when you forgot, with no compile or runtime complaint

### Two-Way Binding

Form controls take a `bind:` parameter as the two-way form — TextField/TextArea/Select/RadioGroup bind a `Reactive<string>`, Checkbox/Switch a `Reactive<bool>`:

<!-- ikon-example: px-two-way-binding -->
```csharp
view.TextField(["flex-1"], label: "Name", bind: _name);
view.Switch(bind: _subscribed, label: "Subscribe to newsletter");
```

Without a reactive at hand, use `value:` + `onValueChange:` explicitly; when both are passed, `bind:` wins over `value:`, and `onValueChange:` still fires after the bound reactive is written. A `value:` with no `bind:` and no handler that writes a change back (`onValueChange:`, or `onSubmit:` on a text field) renders the control read-only and logs a dev warning; `readOnly: true` on a TextField or TextArea renders the value read-only without the warning. A Select whose value, bound or passed, matches none of its options shows no selection and logs a dev warning, unless the value is empty and a `placeholder:` is set. When the value is certainly set and no `disabled:` (or, on TextField, TextArea, `CodeEditor` and `RichTextEditor`, `readOnly: true`) is passed, the build reports it too, as warning IKON027. An overlay's or picker's `open:` (Dialog, AlertDialog, Popover, Select, DatePicker and the rest) is controlled the same way: with no `onOpenChange:` its trigger, Esc and its own buttons cannot open or close it, which logs a dev warning and, when `open:` is certainly set, is warning IKON032 at build time.

### App Chrome and SemanticTone

The chrome composites — Badge, Alert, Toasts, StatCard's icon box — share the `SemanticTone` enum (`Neutral`, `Brand`, `Success`, `Warning`, `Error`, `Info`). Tones map to the theme's semantic color tokens, so they are correct in both light and dark mode:

<!-- ikon-example: px-app-chrome-and-semantictone -->
```csharp
view.Badge("Live", SemanticTone.Success);
view.Alert("Import failed", SemanticTone.Error, description: "The file is not valid CSV");
view.StatCard("Revenue", "$12,400", delta: "+8%", trend: StatTrend.Up, icon: "trending-up",
    iconTone: SemanticTone.Success);
```

Toasts are a per-client queue rendered by a single `ToastHost` in the root UI. The queue is a `ClientReactive`, so call `Toasts.Success`/`Error`/`Show` where a client scope is active (a UI render or a client event handler); from background work outside one they throw instead of showing a toast:

<!-- ikon-example: px-app-chrome-and-semantictone-2 -->
```csharp
private readonly Toasts _toasts = new();
```

<!-- ikon-example: px-app-chrome-and-semantictone-3 -->
```csharp
// In UI.Root, mount exactly once:
view.ToastHost(_toasts);

// From any handler:
_toasts.Success("Saved");
_toasts.Error("Upload failed", ex.Message);
```

### AI Disclosure

`AiDisclosure` is the one place an app tells a person they are dealing with AI. Use it instead of
writing the line yourself: it is IKON's uniform EU AI Act Article 50 disclosure, and the
human-readable counterpart of the machine-readable mark `Ikon.AI`'s generators write into every file
they produce.

`AiDisclosureKind` picks which disclosure duty the line fulfils — `Interaction` for "you are talking to an
AI", `GeneratedContent` for "what you are looking at was made by one". `AiDisclosureVariant` picks
where it sits: `Note` is one muted caption line for under a composer, `Banner` a bordered strip for
the top of a conversation or a page of generated content, and `Pill` a compact badge with its own
scrim for laying over generated media.

<!-- ikon-example: px-ai-disclosure -->
```csharp
// Under a chat composer
view.AiDisclosure(AiDisclosureKind.Interaction);

// Above a page of generated copy, with the accuracy caveat that is not itself a disclosure
view.AiDisclosure(AiDisclosureKind.GeneratedContent, AiDisclosureVariant.Banner,
    note: "AI can make mistakes. Check anything important.");

// Over a generated image
view.AiDisclosure(AiDisclosureKind.GeneratedContent, AiDisclosureVariant.Pill,
    style: ["default", "absolute bottom-2 end-2"]);
```

The default wording follows the kind and the variant, in the language of the client's `Context.Locale` (English, Finnish, Swedish, German, Spanish, French, Italian, Portuguese, Dutch, Polish, Estonian, Russian, Ukrainian, Arabic and Somali, English for any other; `Pill` gets the short "AI" / "AI-generated"); `note:` renders as a second line on `Banner` and after the disclosure on `Note`, and `Pill` drops it; pass `text:` for a product whose own voice says it
better or a market whose regulator words it differently. The `GeneratedContent` sentence a `Note` or
`Banner` shows by default is in the past tense ("This was generated by AI."), so show it beside what a model has already made; a
caption for a choice that will produce AI content (a "Picture round" switch) passes its own `text:`.
It renders themed from
`Theming.AiDisclosure.*` and carries `role="note"`, so a screen reader announces it as commentary
rather than as the app's own output.

### Forms and Dialogs with FormState

`FormState<T>` holds everything one editing form needs per client — open, busy, dirty, the draft, and form-level and field-level errors — so a dialog is one declared field rather than a hand-written set of `ClientReactive` flags. Declare it as an app field with a factory for a fresh draft; the composites `FormDialog`, `FormField`, `FormError` and `FormSubmit` bind Dialog, Form, FormField and a submit Button to it:

<!-- ikon-example: px-forms-and-dialogs-with-formstate -->
```csharp
private sealed record PresetDraft(string Name = "", bool Public = false);
private readonly FormState<PresetDraft> _preset = new(() => new PresetDraft());
private readonly ClientReactive<bool> _editingPreset = new(false);
```

<!-- ikon-example: px-forms-and-dialogs-with-formstate-2 -->
```csharp
// Open from a handler, never from the render body: Show resets the errors and reopens the form,
// so calling it on every render would undo validation and close-on-success.
view.Button(text: "New preset", onClick: () => { _editingPreset.Value = false; _preset.Show(); });
view.Button(text: "Edit preset", onClick: () => { _editingPreset.Value = true; _preset.Show(new PresetDraft(existing.Name, existing.IsPublic)); });

// In the UI:
view.FormDialog(_preset, title: _editingPreset.Value ? "Edit preset" : "New preset", content: form =>
{
    form.FormField(_preset, "Name", content: f =>
        f.TextField(value: _preset.Draft.Name,
            onValueChange: v => { _preset.Edit(d => d with { Name = v }); return Task.CompletedTask; }));
    form.FormError(_preset);
    form.FormSubmit(_preset, "Save", SavePresetAsync,
        validate: d => string.IsNullOrWhiteSpace(d.Name) ? [new FormFieldError("Name", "Required")] : []);
});
```

`FormField` and `FormSubmit` need an ancestor `Form` or `FormDialog` (`FormField` is a Radix form field, which throws outside a form and leaves the page blank). Call them inside a `FormDialog` or `view.Form` lambda, never on the page `view`.

`SubmitAsync` runs the validator, then the handler under the busy flag, and closes on success. A `FormException` thrown from the handler becomes a field or form-level error; any other exception becomes a form-level error with its message, and the form stays open with the draft intact.

## Styling with Crosswind

Three layers compose freely in the same style array:

1. **Semantic theme-aware classes** (`bg-card`, `text-primary`, `bg-brand-solid`, `border-secondary`, `bg-background`) — the default path. They resolve through CSS variables that flip between light and dark and respond to per-app `IkonTheme` overrides, so re-theming never touches individual style arrays.
2. **`Ikon.Parallax.Theming` token classes** (`Button.PrimaryMd`, `Card.Default`, `Text.H1`, `Layout.Page`, `Tone.Solid`, …) — pre-composed bundles of the semantic classes with tested defaults.
3. **Hardcoded palette classes** (`bg-amber-400`) and raw hex — for looks that intentionally should not change with the theme. These bypass the theme system entirely.

<!-- ikon-example: px-styling-with-crosswind -->
```csharp
view.Button([Button.PrimaryMd, "mt-4 self-center"], text: "Submit", onClick: SubmitAsync);
view.Box(["bg-card border border-card p-6 rounded-2xl"], content: v => { });
view.Text([Text.Caption], text: "Updated just now");
```

### Merge Semantics: the `default` Marker

**The merge is opt-in.** A `style:` array is *exactly* what renders — the themed default is **not** added underneath it. Omit `style:` entirely and you get the full themed control; pass `["w-full"]` and you get an element whose only class is `w-full`. A field given `label:` is the exception. Its placement classes (`flex-1`, `grow`, `basis-*`, `self-*`, `w-*`, margins, grid spans, `order-*`) move to the column that holds the caption and the field. When the array holds only placement classes, the field gets an empty array and renders fully themed: `view.TextField(["w-full"], label: "Name")` is a themed input, but `["w-full", "h-14"]` leaves it unstyled again.

<!-- ikon-example: px-merge-semantics-the-default-marker -->
```csharp
view.TextField(bind: _name);                        // fully themed input
view.TextField(["default", "w-full"], bind: _name); // themed input, full width  ← what you usually want
view.TextField(["w-full"], bind: _name);            // an unstyled box that is full width
```

A `style:` array that leaves out the classes that draw or place a component logs a dev warning with its file and line: a text field or text area with no background and no border, a progress bar with no background or no height, a dialog, sheet or drawer panel with no `fixed` or `absolute` class, a stat card, table cell or table head with no padding, a disabled button with no `disabled:` class, and a feed scroller with no `overflow-y-auto` (it then neither scrolls nor snaps). Any other themed component (a badge, card, stat card, table part, link, toggle, alert, button…) warns, naming itself, when its array holds only layout classes such as `self-start`, `mt-4`, `w-full`, `flex-1`, `gap-2` or `col-span-2`: `view.Badge(["self-start"], …)` renders the text with no badge around it. A badge array of colors with no padding or no rounding (`["bg-teal-100 text-teal-800"]`) warns too, since it renders as colored text without the pill. An array of string literals that holds only layout classes is also reported at build time, as warning IKON025, and the other warnings in this paragraph are warning IKON029 when every entry of the array is a constant (a literal, or a token such as `Card.Elevated`) and none is `"default"`. Both build warnings check the array written at the call, each branch of a conditional there, and the arrays a one-expression helper in the same file returns (`static string[] Tone(int side) => side == 0 ? [...] : [...]`). `view.Card` pads its `content:` (and its header) itself and stacks the content in a column 12px apart, which a `gap-*` or `items-*` class in the card's array respaces or aligns. A padding class in the card's own array (`[Card.Default, "py-2"]`) doubles the padding: that warns at run time, and at build time as IKON026. Drop the padding, or pass `contentStyle: ["p-0"]`. A `Column`, `Row` or `Box` that is the only content of a card and carries its own `p-*` doubles the padding the same way; that is a build warning (also IKON026) but not a run-time warning. The other way round, a `Card.*` token on a `Column`, `Row` or `Box` draws the card's border and background but adds no padding, so a `Text` drawn straight into it touches the border; when the array has no padding class, that is warning IKON034. A `Row` child with a fixed width of 16rem or more (`w-96`, `w-[380px]`) beside other children is warning IKON036: a Row keeps one line, so on a phone the other children shrink to a sliver. Lay such panes out with `view.Column(["md:flex-row"], …)` and give the pane its width from `md:` up. A page whose width is capped (`max-w-3xl` on the `UI.Root` style, a call directly in it, or one directly inside a full-width `ScrollArea`, `Column` or `Box` there) with nothing centring it is warning IKON037: it sits against the left edge of a wide screen. Add `mx-auto`, or use `Layout.Page`. The check treats each panel of a full-width `Tabs` at that level as a page and checks the calls in it too. A `Text` or `Markdown` call whose literal reads like an empty state (`"No messages yet"`, `"Nothing here yet"`, `"No results found"`) is warning IKON038: use `view.EmptyState(title: …)`. `Tabs` with `listStyle: [Tabs.NavList]` and the default or `Tabs.Trigger` triggers is warning IKON039: pass `triggerStyle: [Tabs.NavTriggerMd]`. Warning IKON041 is a tab bar built from buttons: three or more buttons in one container whose style switches between two `Button.*` tokens depending on comparing one state value, in a method that also renders two or more views depending on that state. Use `view.Tabs` instead.

The marker is what asks for the theme — with it the base merges *under* what you wrote, without it there is no merge at all:

- **`default`** — put the literal `"default"` first and the component's themed base is merged **under** your classes: `["default", "h-14"]` is the themed input at your height. Only `display` and `position` classes are *replaced* rather than added to (a caller `flex` drops the base's `inline-flex`); everything else is layered.

- **Theme token composites** (`Button.PrimaryMd`, `Input.Default`, `Card.Interactive`, `Badge.SuccessMd`, …) are already complete component styles, so they need no marker: `[Button.OutlineMd]` renders exactly the outline button, and `[Button.OutlineMd, "mt-2 w-full"]` layers on top of it. Fragments (`Button.Base`, `Layout.*`, `Text.*`) are partial and are meant to be combined.

**Order inside the array decides conflicts.** The whole array compiles to a single CSS declaration block in array order, and a repeated property keeps its last value — no specificity, no stylesheet ordering, just "later wins". So `[Button.GhostMd, Button.IconSm]` is a 32px square, 44px below the `sm` breakpoint (the size token's `px-5`/`min-h-10` is overridden by the square's `p-0`/`min-h-0`), and the same two tokens the other way round are a 40px-high button with 40px of side padding.

**What a bare array costs you.** Writing `view.Button(["px-3 py-1 rounded bg-card"], …)` is legal and sometimes exactly right — but it is the whole style, so the button loses every affordance the base carried:

| Dropped | Symptom |
| --- | --- |
| `disabled:pointer-events-none disabled:opacity-50` | A `disabled:` control looks and reads identical to an enabled one (a dev warning names the call site). |
| `hover:` / `active:` variants | Nothing responds to the pointer. |
| `transition-colors` | State changes snap. |
| The variant's colours | Intentional when you are restyling — surprising when you only meant to add a margin. |

Cursor and the focus ring are *not* on that list: those come from the platform's button reset, so a hand-styled button still shows a pointer on hover and a visible ring on keyboard focus.

The rule of thumb: **to add to a control, start the array with `"default"`; to replace its look, leave it out.**

**Slots follow the same rule as `style:`.** A component built from several parts exposes one array per part —
`triggerStyle:`, `contentStyle:`, `dayStyle:`, `toolbarStyle:`, `gutterStyle:` and so on. Each carries its own
themed default, each renders exactly what you pass, and each takes `"default"` to merge the theme back under it.
Style one part and the rest stay themed:

<!-- ikon-example: px-merge-semantics-the-default-marker-2 -->
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
primitives (`Row`, `Column`, `Grid`, `Stack`), `ScrollArea` and its viewport/scrollbar/thumb, `Select`'s parts, `Slider`'s root,
`ResizableSplit`, size classes a component derives from its own parameters (`Spinner`, `Icon`, and `Skeleton` unless your array sets a height), and the
display class of the table parts (`table-cell`, `table-row`, …) unless your array sets a display class of its own.
There your array is *appended* to that base rather than replacing it, and later classes win:

<!-- ikon-example: px-merge-semantics-the-default-marker-3 -->
```csharp
view.Column(["gap-4"]);                    // flex flex-col gap-4 — the flex base is not droppable
view.ScrollArea(viewportStyle: ["px-8"]);  // h-full w-full rounded-[inherit] md:data-[overflow-y=true]:px-3 px-8 — still fills and scrolls; px-8 wins over the scrollbar gutter
```

You never need `"default"` on these, and you cannot remove the base — override it instead (`min-h-[300px]` beats
the base's `min-h-0` by coming later). The distinction is what the base *is*: a complete look you may replace
(`Button`, `Card`, `Badge`, `Input`, and every slot above) versus structure the part needs to work. Dropping
`h-full` from a scroll viewport would not restyle it — it would stop it scrolling.

### Default Styling and Auto-Composed Indicators

`style:` is optional. When it is omitted, interactive and input controls fall back to their default theme token, so the minimal call renders a complete, themed control:

<!-- ikon-example: px-default-styling-and-auto-composed-indicators -->
```csharp
view.Checkbox(bind: _done, label: "Done");
view.Switch(bind: _on, label: "Notifications");
view.TextField(bind: _text, label: "Note");
view.Button(text: "Submit", onClick: async () => { });
```

Controls that have a visible inner part also compose it automatically when no `content:` is given — Checkbox gets its check indicator, Switch its thumb, Slider its track/range/thumb. You only pass `content:` to customise the inner part. Select takes no `content:`: it always composes its trigger and items from `options:`/`groups:`, styled through `triggerStyle:`/`contentStyle:`/`itemStyle:`/`itemIndicatorStyle:`. To render a checkbox with no check mark, opt out explicitly with `content: _ => { }`. Layout primitives (`Box`, `Row`, `Column`, `Grid`, `Stack`) carry no *themed* default — no colours, no spacing, no chrome — so "no style" is the normal usage there. They are not blank, though: all but `Box` keep the base class that makes them what they are (`Column` is `flex flex-col`, `Stack` is `relative`), and that base is not droppable — see [Some bases always apply](#merge-semantics-the-default-marker).

### Icon Buttons

An icon-only button is a square, and padding is easy to get wrong on a square. A size token brings `px-5` and `min-h-10`, so `["default", "w-7 h-7"]` renders **40×40** — the height wins, the width is padded out, and the row it sits in gets taller than asked. Use a square token, **after** the variant:

<!-- ikon-example: px-icon-buttons -->
```csharp
view.Button([Button.GhostMd, Button.IconSm],   // h-8 w-8 p-0 min-h-0 — last wins
    icon: "refresh-cw",
    tooltip: "Refresh",
    onClick: RefreshAsync);
```

`Button.Icon` (40×40), `Button.IconSm` (32) and `Button.IconXs` (28) each grow to 44×44 below the `sm` breakpoint, the comfortable touch target, and carry `p-0 min-h-0` (and `max-sm:min-h-0`, so the phone rule of the small sizes does not stretch them) for exactly this reason, and each must come after the variant token to win. Measure the rendered box when the size matters: an entrance animation (`scale-75`) or a `pointer-coarse:` rule can make it something other than what the classes say.

### Tooltips and Naming Controls

An icon has no text, so an icon-only control needs a name given to it, for a sighted mouse user and a screen-reader user alike. With no `text:` (which would render visibly beside the icon), `tooltip:` serves both — it also becomes the aria-label; `ariaLabel:` sets the name explicitly:

<!-- ikon-example: px-tooltips-and-naming-controls -->
```csharp
view.Button([Button.GhostMd, Button.Icon],
    icon: "trash-2",
    tooltip: "Delete",    // the hover bubble, and the aria-label since there is no text:
    onClick: DeleteAsync);
```

- **`ariaLabel:` replaces the visible text as the name** that screen readers, voice control and `ikon browse` use. Set it only where there is no visible text, or start it with that text (`$"Remove {item.Name}"` on a Remove button). A constant `ariaLabel:` on a Button, ActionButton or Link that does not contain its constant visible text is warning IKON028, and so is one on a capture or mic button whose constant `text:` has words and that has no `content:`, since that button ignores it. With `content:` it is an icon button, and `ariaLabel:` names it.
- **Never use a `title` prop for a hover name.** That is the browser's own tooltip: unstyleable, unpositionable, ~1s late, and invisible to touch. `Dialog(title:)` is a different thing — an accessible title, not a tooltip.
- `tooltip:` on `Button` is sugar for wrapping it in `Tooltip`. Reach for the `Tooltip` component directly when the trigger is not a Button, or when you need `open:` (which needs `onOpenChange:` too, or hover and focus can neither show nor hide it) or `delayDuration:` control.
- **The tooltip wrapper becomes the flex child.** `Tooltip` renders a `span` around its trigger, so layout, responsive and absolute classes belong on the wrapper, not on the button inside it — `hidden lg:inline-flex`, `shrink-0`, `absolute top-2 right-2` left on the button are all pinned inside a static span and do nothing. Pass them as `tooltipRootStyle:` (or `rootStyle:` on `Tooltip`).
- **Nesting inside another overlay:** a tooltipped menu button goes *inside* the Popover's `trigger:` slot, not around the Popover.

Form controls name themselves the same way. `Checkbox`, `Switch` and `Toggle` take `label:`, which wraps control and text in a `<label>` — that association is what makes the text clickable *and* the control's accessible name. A `Text` placed next to a bare control looks identical and associates nothing. `Slider` and `Select` are the odd ones: their role lives on an inner element (`role="slider"` on the thumb, `role="combobox"` on the trigger), so a `Slider`'s `label:` and either one's `ariaLabel:` are routed there; a name left on the root names nothing. `Checkbox`, `TriStateCheckbox`, `Switch`, `Slider` and `Select`, and a `TextField`, `TextArea`, `CodeEditor` or `RichTextEditor` with no `placeholder:` either, warn at dev time when they render with no name at all, unless they are drawn inside a `Label`, `FormField` or `FormControl`. `Toggle` does not warn, because its `content:` is usually a label of its own. When none of `label:`, `ariaLabel:`, `placeholder:` (on a text field) or `props:` is passed and the control is not inside one of those, the build reports it too, as warning IKON033.

The Crosswind class vocabulary and the motion/animation system are covered in the **Crosswind Styling and Motion Guide** (`crosswind-styling-and-motion-guide.md`); theme keys and brand palettes in the **IKON Theming Guide** (`theming-guide.md`).

## ScrollArea and Auto-Scroll

ScrollArea is a scrollable container that can scroll new content into view automatically, for chat interfaces and live feeds:

<!-- ikon-example: px-scrollarea-and-auto-scroll -->
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

The scrollbars float over the content, so the viewport adds a gutter on each axis that overflows (`px-3` beside a vertical bar from the `md` breakpoint up, `pb-3` under a horizontal one) and nothing slides under a scrollbar. On a phone the vertical bar overlays the content with no gutter. Content that fits keeps the full width. Padding of your own in `viewportStyle:` replaces the gutter on that side.

**Auto-scroll behavior (Polite priority):**
- At bottom: new content auto-scrolls into view
- Scrolled away: auto-scroll is suppressed, a floating indicator appears to notify the user
- Clicking the indicator or scrolling back to bottom resumes auto-scroll

For forced scrolling (always scroll regardless of position), use `FocusHint` with `FocusPriority.Assertive`:

<!-- ikon-example: px-scrollarea-and-auto-scroll-2 -->
```csharp
anchor.FocusHint(new FocusHintProps { Priority = FocusPriority.Assertive },
    key: $"scroll-{version}");
```

### Scrolling Inside a Flex Parent

The canonical dialog / side-panel pattern is a `Column` with a fixed height, a header + footer, and a scrolling body. `ScrollArea` automatically applies `min-h-0 min-w-0` to its root, so it shrinks correctly inside a flex parent with no extra classes.

**Why this matters.** A flex child's `min-height` defaults to `auto` (equal to its intrinsic content size), so without `min-h-0` a `flex-1` scroll region would grow to fit all its content — pushing siblings off-screen and bypassing the inner overflow. The framework handles this for `ScrollArea`. You only need to think about it on your own `Column`/`Row` with a manual `overflow-y-auto`.

**`max-h-` is not a height.** A ScrollArea needs a *definite* height to scroll against: `h-[400px]`, `h-full` inside a sized parent, or `flex-1` inside a flex column. `rootStyle: ["max-h-96"]` is a common mistake: the root grows with its content until the cap, and the viewport inside it, having no resolved height of its own, never overflows. The symptom is a panel that grows past the cap or clips instead of scrolling. If a cap is what you want, give it a height too: `["h-full max-h-96"]`.

**Canonical recipe — `ScrollArea` inside a flex column:**

<!-- ikon-example: px-scrolling-inside-a-flex-parent -->
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

<!-- ikon-example: px-scrolling-inside-a-flex-parent-2 -->
```csharp
view.ScrollColumn(
    style: ["h-[82vh] w-full sm:max-w-[560px] rounded-2xl bg-card"],
    header: h => h.Row(["px-5 py-4 border-b"], content: title => { }),
    footer: f => f.Row(["p-3 border-t"], content: composer => { }),
    content: body => body.Column(["gap-3"], content: messages => { }));
```

For chat specifically, `view.ChatLog()` wraps `ScrollColumn` with chat-friendly defaults (auto-scroll on).

**Raw `Column`/`Row` with overflow-auto.** If you're not using `ScrollArea`, you still need `min-h-0` (or a fixed height) yourself — the framework fix only applies to the `ScrollArea` component:

<!-- ikon-example: px-scrolling-inside-a-flex-parent-3 -->
```csharp
view.Column(["flex-1 min-h-0 overflow-y-auto", Scrollbar.Thin], content: rows => { });
```

Dev warnings are on by default (`IKON_DEV_WARNINGS=0` opts out): a single `Log.Instance.Warning` when a `Column`/`Row`/`Box`/`Flex` is detected with `overflow-y-auto` + `flex-1` and no `min-h-0` — with the exact `file:line` of the offending callsite.

**Theme the native scrollbar when you scroll a container yourself.** A bare `overflow-auto` shows the OS scrollbar — on Windows a wide grey slab that matches no theme and shifts the layout when it appears. `Theming.Scrollbar.Thin` is the themed thin bar (`Scrollbar.Hidden` removes it entirely, for a strip whose overflow is visually obvious):

<!-- ikon-example: px-scrolling-inside-a-flex-parent-4 -->
```csharp
view.Row(["overflow-x-auto gap-2", Scrollbar.Thin], content: chips => { });
```

It sizes both axes on purpose. Styling only the width leaves a *horizontal* bar at its default height, and a horizontally scrolling row is where a themed bar is most often needed.

## PanZoom: Viewing Something Larger Than the Screen

`view.PanZoom()` (`PanZoomExtensions`) is a clipped viewport whose content the user moves and scales in the client: scrolling pans, Ctrl/⌘+scroll or a pinch zooms about the pointer, dragging pans. The pan offset and every in-gesture frame stay in the browser; the app hears only the scale at the end of a gesture, so a wheel burst or a pinch costs one round trip rather than one per event. Use it for a floor plan, a board of cards, a diagram, a large image — anything laid out at its natural size that has to be *looked at* rather than edited.

<!-- ikon-example: px-panzoom-viewing-something-larger-than-the-screen -->
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

`onTap:` reports a tap or click on the content that did not pan, as a `PanZoomTapArgs` in the content's own unzoomed pixels from its top-left corner, whatever the pan and zoom. For a picture shown at its natural size that is the tapped pixel of the picture, so a marker drawn at that position inside a `relative` box the size of the picture lands where the person tapped and stays there at every zoom (the `pan-zoom-split-layout` pattern places a pin this way). A press on a button or link inside the content is that control's click, not a tap. The view opens at `defaultScale:` (or `scale:`) with the content's top-left corner in view, however large the content is. `anchor:` puts another part in view (`PanZoomAnchor.Right` opens a wide chart at its latest end), and `fit: PanZoomFit.Contain` (or `Width`, `Height`) opens the content scaled to the viewport, capped at `maxScale:` but not raised to `minScale:` (content too large for it still opens whole), and reports that scale through `onScaleChange:`. A 1400-pixel plan then opens whole in a 700-pixel viewport without working out a scale of 0.5 by hand. The client repeats this placement until the person pans or zooms or the app writes a new `scale:`, and again when `viewKey:` changes; a read-only `scale:` skips the fit.

What it is not: an editor surface. Nothing inside a `PanZoom` knows it is scaled, and the viewport captures a primary pointer press to pan once the pointer moves more than 5 pixels. A click on a custom node inside still reaches that node, but a drag that starts on the content pans the view, even a drag on a `DndContext` item. For a canvas with selection, marquee or drag in document space, build a custom node with its own `.tp` transport (see `custom-map-component-guide.md`).

## The IKON O

`view.LivingO()` (`LivingOExtensions`) draws the IKON O as a living character — the brand's one character, drawn and animated entirely in the browser, so the server mounts it once and sends nothing per frame. `LivingOVariant.Embedded` rests the O at `anchorX:`/`anchorY:` in its box, its radius `size:` as a fraction of the box's height; `LivingOVariant.Full` is the whole-screen character on a dark background, which the space key sends a gust through. On the embedded O, `candidates:` is JSON the O reads to try shapes, `calm: true` slows it, and `interactive: false` stops it following the pointer; the full O ignores all three (it is always calm and follows the pointer) and reads only `shapeSchedule:`, timed candidates the embedded O ignores. It has no size of its own, so give it a height in the style array (`["w-full h-48"]`, or `["absolute inset-0"]` inside a sized box); without one it draws nothing. Its code loads the first time a page shows it. A boot snapshot includes the O only inside a `SnapshotReveal` region, where the page then animates before its session opens. Outside one, the capture skeletonizes the O like any other leaf, `poster:` included. `poster:` names a still image drawn where the O rests until its first frame, and it is what a prerendered page shows a crawler. For a small, still O — beside text, in a header, as a loading mark — `view.OMark()` draws the same O as an image, in `LivingOExtensions.IkonPink` or any colour.

## Example: Interactive Form

<!-- ikon-example: px-example-interactive-form -->
```csharp
private readonly ClientReactive<string> _name = new("");
private readonly ClientReactive<bool> _subscribed = new(false);
private readonly Reactive<bool> _busy = new(false);
private readonly Reactive<string?> _status = new(null);

public async Task Main()
{
    UI.Root([Page.Default], content: view =>
    {
        view.Column(["mx-auto gap-4 max-w-md p-8"], content: view =>
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

The platform captures an app's **initial UI at build time** and ships it as a static snapshot asset, so the first paint appears instantly — before the WebSocket connects. Capture is **on by default**: each `[[Surface]]` in `ikon-config.toml` lists the routes to snapshot in `Routes` (an app with no surface declared captures `["/"]`), and `ikon bundle` / `ikon deploy` renders the app once per route for a synthetic snapshot client and serializes the resulting UI tree. When no surface has `Routes` or `Seeds` rules (below), boot snapshotting is off.

```toml
[[Surface]]
Name = "site"
Routes = ["/"]   # what an app with no [[Surface]] captures; [] with no Seeds disables boot snapshotting
```

Because that snapshot is a **public asset served to everyone**, anything in your initial UI — a signed-in user's name, a session link, private data — would otherwise be baked into it and shown to every visitor before the live UI loads. So the snapshot is **privacy-safe by default**: during capture, Parallax automatically **replaces every piece of content with a skeleton**. Each text, image, input, and control becomes a pulsing placeholder block while the layout shape (rows, columns, tabs, cards) is preserved — so the first paint looks like your app's skeleton screen and **no per-user content can leak**, with no work from you.

During snapshot capture the flag `view.IsSnapshot` is `true` (it is always `false` on the normal live render). You only need to act when some content is **safe to show** in the public snapshot — your logo, static chrome, marketing copy — and you want it to appear for real instead of as a skeleton. Wrap it in `SnapshotReveal`:

<!-- ikon-example: px-boot-snapshot-and-privacy -->
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

<!-- ikon-example: px-public-pages-opting-a-whole-page-out-of-skeletonization -->
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

The **`Skeleton`** component is also available directly — a pulsing placeholder block, sized and shaped via `SkeletonShape` / `SkeletonSize` (or any `style:`) — for hand-built loading states anywhere in your UI. `SkeletonSize` sets only the height, and a height in the style (`h-80`, `size-10`) replaces it. A circle needs its width and height in the style, or it fills the row as a pill:

<!-- ikon-example: px-hand-built-skeletons -->
```csharp
view.Skeleton(["w-1/3"], size: SkeletonSize.Xl);
view.Skeleton(["w-10 h-10 shrink-0"], shape: SkeletonShape.Circle);
```

**`Tabs` stays snapshot-aware automatically:** in snapshot mode only the **active** tab's content panel is rendered (its content skeletonized like everything else), while **every** tab trigger still renders. The snapshot therefore carries just the active tab — yet the tab row is identical to the live UI, so nothing pops into place when the live UI takes over.

Automatic skeletonization is a **one-off build-time render**, so it can afford to be thorough. On the **live path it is zero-cost**: the snapshot branch is gated on `IsSnapshot`, which is `false`, so it short-circuits on a single boolean read with no per-element metadata and no effect on the diff/serialize hot path. For finer control you can read the flag directly:

<!-- ikon-example: px-hand-built-skeletons-2 -->
```csharp
if (view.IsSnapshot) { /* snapshot-only branch */ }
```

**A Flutter frontend gets the same snapshots.** When the app has a `frontend-flutter`, the capture renders each route a second time with Flutter styles and ships them with `boot-snapshots-flutter.json`, which maps each route to its file. The phone downloads its route's snapshot after it first connects and starts on it next time, until it has a screen of its own to start on (see Responsiveness in the Flutter guide). The second capture doubles the capture time, and a route whose Flutter capture fails ships without one, which the deploy log names.

**Preview the snapshot UI in a browser** by opening the running app with `?ikon-snapshot=true`. The SDK then connects as a snapshot client — the same `Context.IsSnapshot = true` render path the build-time capture uses — so the live page shows exactly what the boot snapshot bakes: every unrevealed element as a skeleton, `SnapshotReveal` regions showing real content, `SnapshotHide` elements gone, `SnapshotOnly` filler present, and only the active tab's panel rendered. It needs no rebuild and works against any running instance — a local `ikon run` or a deployed URL — so you can confirm at a glance that no per-user or sensitive content leaks into the public first paint. A coding agent does the same with `ikon browse --path "/?ikon-snapshot=true"` (or `"/fi?ikon-snapshot=true"` for a route): `observe` reads the snapshot's screen and `look` screenshots it.

### Per-route snapshots and SEO

Beyond the single boot view, an app can declare **public routes** to snapshot individually. Each declared route is captured with its own synthetic client (connected with that route as its initial path), rendered to its own snapshot — and, at bundle time, **prerendered to static HTML** through the same React component pipeline the browser uses. The gateway serves that HTML **to crawlers** for the route's URL, so search engines and link-preview fetchers get real, styled, content-bearing markup (whatever `SnapshotReveal` opts in). **Human visitors** always get the SPA index instead: the SDK seeds the route's snapshot JSON (the bundle bakes a route→snapshot map into the index) for an instant, pixel-faithful first paint, and the live app takes over when the WebSocket connects. A `sitemap.xml` and `robots.txt` are generated from the same route set at deploy time (an app-shipped `public/robots.txt` or `sitemap.xml` wins).

```toml
[[Surface]]
Name = "site"
Routes = ["/", "/pricing", "/about"]     # static routes to snapshot
Seeds = []                               # optional seed rules; see below
```

Content-driven routes (one per store listing, article, …) are declared in app code and unioned with the static list at capture time:

<!-- ikon-example: px-per-route-snapshots-and-seo -->
```csharp
app.OnSnapshotRoutes(async () => (await store.GetListingsAsync()).Select(l => $"/listing/{l.Id}"));
```

Two capture-quality tools:

- **Settle signal** — capture treats a quiet UI stream as "settled" and additionally listens for an explicit ready signal; whichever arrives first wins, and everything is bounded by a per-route cap. A route whose content loads asynchronously after a silent gap could quiesce too early and bake its loading skeleton into the snapshot — call `ClientFunctions.SnapshotReadyAsync()` when the route's content is loaded and capture snapshots at exactly that moment. Nothing to configure; the call is a harmless no-op for normal browser clients.
- **Redirect detection** — a route that navigates elsewhere during capture (e.g. bouncing to a sign-in view) is never captured under the wrong URL: the capture fails with an error naming the route and where it settled. Declared routes must be paths the app serves directly — if the app rewrites `/` to `/home` on load, either render the page at `/` without rewriting or declare `/home` instead (and fix `OnSnapshotRoutes` results the same way).

**A capture that falls short stops the deploy.** When the capture misses a declared route or variant (a route redirected, it timed out) or the prerender fails, `ikon deploy` and `ikon bundle` stop before uploading anything. The error leads with the app's own error and names the file holding the full capture output, `build/snapshot-work/capture-output.log`. A surface's pages depend on their snapshots: a `Connect = "onAction"` page would connect at load without one, and a `Connect = "none"` page would be blank. Fix the route and deploy again, or pass `--no-boot-snapshots` to deploy without capturing. The app then has no first paint before a page connects, no prerendered pages and no generated `sitemap.xml` or `robots.txt` until a later deploy captures again. That flag refuses an app that has a `Connect = "none"` surface. An app that fails to start in the capture is refused too, and `--ignore-start-failure` deploys it without snapshots.

**The capture runs the app against the target's real data.** `ikon bundle` and `ikon deploy` start the built app on the deploying machine, signed in as the deployer, against the databases and storage of the app being deployed (the one `--target` names, or the environment's default), and its `Main` and starting handlers run there. Persistent reactives are not saved when the capture stops, but anything else `Main` and the handlers do (send an email, charge a card, seed or migrate data) happens. Guard those with `SnapshotCapture.IsCaptureProcess`. `Context.IsSnapshot` marks only the capture's clients, so it does not cover `Main`.

Routes must be app-owned paths: `/`-prefixed, no query/fragment, not under the platform-reserved `/ikon` or `/api` prefixes. A snapshot is matched by path alone, so `/?game=abc` first paints the `/` snapshot until the live page replaces it; an app whose page depends on the query string should keep that difference out of the snapshot (a skeleton, or `SnapshotHide`) or move it into the path. Prerendered crawler HTML (and the sitemap built from it) is produced only for the routes of a surface that admits visitors without sign-in (`guest` or `global` among its `Methods`) — serving marketing HTML to crawlers in front of a hard sign-in wall is a cloaking pattern, so the routes of a surface that requires sign-in ship the JSON snapshots alone (the instant skeletonized first paint still works).

#### How to use it

Reach for per-route snapshots when an app has **public, content-bearing pages that should rank in search** — a marketing home page, pricing/about pages, a storefront's product pages, a blog's articles. It does nothing for a signed-in dashboard (that content is skeletonized and gated behind sign-in), so enable it only on the public surface.

1. **Make the public routes openable without signing in.** In `ikon-config.toml`, keep `guest` or `global` in the `Methods` under `[Surface.Auth]` of the `[[Surface]]` that lists the routes (the default `Methods` include `guest`), so a crawler can connect without a sign-in wall. Otherwise the bundle skips the crawler HTML and ships JSON snapshots only.

2. **List the static routes.** Enumerate the fixed public paths (the scaffold starts you at `["/"]`):

   ```toml
   [[Surface]]
   Name = "site"
   Routes = ["/", "/pricing", "/about"]
   ```

3. **Add content routes in app code** (optional). For pages generated from data — one per listing, article, or profile — return them from `OnSnapshotRoutes`. They are unioned with the static list and de-duplicated, the static routes first. The first 50 routes are captured. The rest get no prerendered page, but they are still listed in `sitemap.xml` (up to 50,000 URLs in all), and the deploy's `BOOT_SNAPSHOT_ROUTES_CAPPED` warning names them, so an archive of thousands of pages returns all of them. Each captured route adds capture time to every deploy, and all routes share one 10-minute capture budget, so a route still waiting when it runs out is skipped and the bundle ships without snapshots. List the routes that matter most for search first:

<!-- ikon-example: px-how-to-use-it -->
```csharp
app.OnSnapshotRoutes(async () =>
    (await store.GetPublishedArticlesAsync()).Select(a => $"/blog/{a.Slug}"));
```

   The provider runs on the machine doing the bundle/deploy, so the captured set is as fresh as your last deploy — re-deploy to pick up new content.

4. **Decide what's public per route.** Capture skeletonizes everything by default. Wrap the parts that are safe and meaningful for a crawler — the headline, body copy, product name/price, hero image — in `SnapshotReveal` so they render as real HTML. Anything left unrevealed ships as a skeleton and contributes nothing to SEO. Use `SnapshotHide` for controls that are dead before the socket connects, and `SnapshotOnly` for snapshot-specific filler. Sensitive or per-user content should stay skeletonized — never `SnapshotReveal` it.

5. **Handle async content.** If a route paints its real content only after an async load (a fetch, a DB read), the quiescence timer could settle on the loading skeleton. Call `ClientFunctions.SnapshotReadyAsync()` once the route's content is in place — capture races that signal against quiescence automatically and snapshots as soon as either arrives. It's a no-op for live browser clients, so it's safe to leave in.

   If the app registers **custom UI modules** (`useIkonApp({ modules })`), the prerender needs them too or those elements render as skeletons in the static HTML: create `src/prerender-modules.ts` in the frontend exporting `prerenderModules: IkonUiModuleRegistration[]` with the same registrations, and the prerender build picks it up automatically.

6. **Give each route its title and description.** Without them a route's page gets `index.html`'s title with the route appended (`Shop — p · toffee-mug`) and `index.html`'s description. Call `ClientFunctions.SetPageMetadataAsync(title, description, imageUrl, language)` where the app decides which route a client is on (the join handler, a path change). The capture records it, and the route's prerendered page carries that `<title>`, description, Open Graph and Twitter card tags and `<html lang>`. In a browser the same call sets the tab title and the tags as the user moves between routes. The image must be an absolute URL (`app.Files.Public.GetAbsoluteUrlAsync`), and a null value leaves that field as it is. `noIndex: true` marks a page search engines must not list, such as the app's not-found screen: it gets a `noindex` robots tag, and a captured noIndex route stays out of `sitemap.xml`. The page still answers HTTP 200, and the next call without it removes the tag.

   When a page exists in several languages, pass `alternates:` as well: a dictionary from a language tag (`"fi"`, `"en-GB"`, or `"x-default"` for the version to show when none matches) to that version's route (`"/fi/hinnat"`) or an absolute http(s) URL. The deploy turns them into `<link rel="alternate" hreflang="…">` tags with absolute URLs in the route's prerendered page and into `xhtml:link` entries under the route in `sitemap.xml`. Give every version the same set: search engines ignore an alternate that the other page does not name back. When the set leaves out the route itself and `language` is set, the route is added under that language. Each call replaces the alternates the call before it set, and a call without them removes them, so a route with no other versions does not keep the links of the route the user came from. A tag that is not a language tag, or a value that is neither a route nor an absolute URL, throws `ArgumentException`.

7. **Bundle and verify.** Run `ikon bundle` (locally built ikon tool). In `build/bundle/frontend-node/` you'll find a per-route `boot-snapshot-*.json`, `ikon/routes/*.html`, and `route-manifest.json`. **Open an `ikon/routes/*.html` file with JavaScript disabled** — the revealed content and its styles should be visible with no "JavaScript is required" notice. That is exactly what a crawler sees.

8. **Deploy.** `ikon deploy` generates `sitemap.xml` and `robots.txt` from the route set and serves each route's prerendered HTML from the gateway. To override the defaults, ship your own `public/robots.txt` or `public/sitemap.xml` — an app-provided file always wins.

**Preview a route's snapshot without a rebuild** the same way as the boot snapshot: open the running app at that path with `?ikon-snapshot=true` to render the capture path in your browser, confirming what's revealed and that nothing sensitive leaks.

#### Seed rules and snapshot variants (`Seeds`)

Route snapshots cover concrete paths that exist at deploy time. Two situations need more:

- **Identity-split pages** — the deferred sign-in pattern where the same path is a guest landing for anonymous visitors and a personal hub for signed-in users. The route snapshots depict the app's **public** entry views, so a signed-in session must not paint them; with nothing else cached, someone who has just signed in would see a blank page until the instance boots.
- **Dynamic paths** — user-created content like `/myapp/my-workshop`, whose slugs exist only in the database. No concrete route can be captured for a slug created after the deploy, so a visitor deep-linking there has nothing to seed.

Seed rules cover both. Each entry is `"pattern:variantId"` in a surface's `Seeds`, and the **first match wins** in array order. A rule applies to the visitors its surface admits: rules on a surface with sign-in providers seed signed-in visitors, and rules on a surface with `guest` or `global` seed anonymous visitors. A variant id is 1-32 lowercase letters, digits or `-` (not leading) — `Dashboard` or `signed_in` fails the bundle:

```toml
[[Surface]]
Name = "public"
Routes = ["/"]
Seeds = ["/**:welcome"]
[Surface.Auth]
Methods = ["guest"]

[[Surface]]
Name = "app"
Seeds = ["/:admin", "/*:dashboard", "/*/**:experience"]
[Surface.Auth]
Methods = ["google", "email"]
```

A pattern segment is a literal (exact match), `*` (exactly one segment, any content), or a final `**` (zero or more remaining segments). So `/` matches only the front page, `/*` any one-segment path, `/*/**` any path two segments or deeper, and `/**` everything. Order matters — `**` means *zero* or more, so `/*/**` also matches one-segment paths; listing `/*:dashboard` first keeps those on the dashboard rule. A `/**` entry anywhere but last fails the bundle (the entries after it could never apply).

On load, the SDK works out which surface the visitor enters: the surface with the longest route covering the path, or the surface with neither routes nor host when no route covers it. A visitor who appears signed in (a non-anonymous localStorage session, or an OAuth callback token) skips a surface whose methods are all anonymous. The SDK then paints that surface's own snapshot of the route if the build captured one, otherwise the file of the surface's first matching seed rule. If the surface has neither, the page stays blank until it connects rather than painting another visitor's page. So in the example above, a signed-in person at `/` enters `app` and paints its `admin` skeleton, while an anonymous visitor paints the captured `/` page.

Every distinct variant id becomes **one skeleton capture**: a capture client connects with the id in `Context.SnapshotVariant`, and the app branches to the matching skeleton (the seed rules may name at most 16 distinct variant ids; more fails the bundle) — no magic paths, the toml ids are the `case` labels:

<!-- ikon-example: px-seed-rules-and-snapshot-variants -->
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

Because nothing in a variant skeleton is wrapped in `SnapshotReveal`, the whole capture skeletonizes into neutral blocks automatically: an app-shaped skeleton with no user data by construction. Render it from **local placeholder data** (a fixed heading, a few empty cards) rather than the app's real reactives — the capture client is unauthenticated, and the skeleton only needs the right geometry. Variant artifacts ship separately from the public routes: they are **not** prerendered to HTML and never appear in the route manifest or sitemap (a skeleton standing for unboundedly many URLs is not crawlable content — concrete SEO stays with `Routes` and `OnSnapshotRoutes`). A returning user's live-snapshot cache of their own last UI still wins over any seed rule. A declared variant the capture cannot produce leaves the bundle without boot snapshots, exactly like a missing route.

The simplest rule set is one shell: `Seeds = ["/**:shell"]` on the signed-in surface seeds one hub skeleton on every path for signed-in visitors, and `view.SnapshotVariant == "shell"` renders it.

### Open-as-guest (the default)

A sign-in-gated app blocks the connection behind its sign-in screen — which also means its landing content can't be a server-drawn page, and the SEO pipeline above has nothing to capture. A surface that lists `guest` or `global` in its `Methods` (the default lists `guest`) inverts that: visitors connect **immediately as an anonymous session**, the app decides what they see, and real sign-in happens on demand.

```toml
[[Surface]]
Name = "app"
[Surface.Auth]
Methods = ["google", "guest"]    # "guest" is what visitors connect as, so there is no sign-in wall
```

The two not-signed-in flavors differ in who shares an identity. `guest` mints a device-scoped anonymous user per visitor, so each drive-by gets their own `UserId` and their own app instance. `global` puts every visitor on one app-wide shared anonymous user — they collapse onto a single instance with no per-visitor database writes, which is the cost-efficient choice for a public landing surface. List both to enter as `global` by default and upgrade a visitor with `login('guest')` when they need an identity of their own.

The frontend establishes the session on first visit (no sign-in wall, no click) and connects. On the server, `Context.IsAnonymous` distinguishes not-signed-in visitors from signed-in users — the authoritative flag; a guest still carries a valid device-scoped `UserId`. `Context.IsGlobal` tells you which flavor you are serving. The typical shape is a branch at the top of the UI root:

<!-- ikon-example: px-open-as-guest-the-default -->
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

One way to trigger sign-in from the server-drawn landing is the client sign-in primitive:

<!-- ikon-example: px-open-as-guest-the-default-2 -->
```csharp
view.Button([Button.PrimaryMd], text: "Sign in with Google",
    onClick: async () => await ClientFunctions.LoginAsync("google"));
```

`LoginAsync` starts the client's OAuth redirect for the given provider (`google`, `microsoft`, …); the user returns authenticated and the client reconnects with its real identity — the anonymous session is abandoned. Guest and global flows stay client-initiated. The landing can also draw sign-in controls: `view.SignInButton(method: ...)` takes a provider, `sso:<connectionId>` or `passkey`, `view.EmailCodeSignIn()` signs in with an emailed code and `view.PhoneSignIn()` with a QR code a signed-in phone approves; the browser runs the sign-in when the person presses the control and the app reconnects as that person. A control works only for a method some surface lists in `[Surface.Auth] Methods`, and renders disabled in a prerendered page. Call `LoginAsync` from event handlers only (like all client functions), never from the render pass.

Guard your authed-only paths: skip user-backend calls, per-user persistence, and deep-link view restoration for anonymous sessions — a guest must not be able to navigate into the signed-in surface by URL. This matters more under `global`, where every visitor shares one `UserId`: never key per-user state on it.

**This is how an app with sign-in gets a crawlable landing page**: combine open-as-guest with `Routes = ["/"]` on that surface. The capture client connects as an anonymous session, renders the same landing, and the SEO pipeline prerenders it to static HTML — crawlable markup, instant first paint from the static file, and the live session connecting invisibly underneath, taking over pixel-identically.

## In-App Feedback

The platform can offer the people using an app a feedback sheet: they pick a kind (an idea, a problem, something they like), write or speak, attach files, and a screenshot of the screen they were on goes with it. Reports reach the app, where its members read them in Studio and with `ikon issue list --source person`. The platform draws the sheet above the app; the app writes no UI for it. `IkonApp` draws it, so a page built only from `ParallaxView`s, with no `IkonApp` on it, shows neither the button nor the sheet.

**A new app has the setting `off`**, so only the visitors of a test link are offered the sheet, whatever `ikon-config.toml` says, and `Button = "overlay"` shows nothing for anyone else until the setting changes. After `ikon feedback set --audience everyone`, a guest on an ordinary link is offered it too. Who is offered it is a setting of the app on the platform, not of its code: `off` (the default), `testers` (signed-in people with the tester role), `signed-in` or `everyone` (guests and the shared `global` visitor too), changed in Studio › Settings › Sharing or with `ikon feedback set --audience <value>`. Each app has its own setting, so a staging target needs the command again with `--target`. On a deployed app it applies to people who join from about 10 seconds after the change, with no build or deploy. Unless it is `off`, the app's own members are offered it too, and a test link admits its visitors whatever the setting. A local run reads the setting when it starts, so restart it to see a change, and offers it to every visitor unless it is `off`. A client with no page to draw the sheet on is never offered it, so `view.CanOpenFeedback()` is false for an `ikon browse --text-only` client, and for a browse of a deployed app whose page could not sign in as you.

Where the way in is drawn is the app's choice, in `ikon-config.toml`:

```toml
[Feedback]
Button = "overlay"     # "overlay" | "none"
```

- **`Button = "overlay"`** (the default) has the platform draw a floating feedback button for everyone offered feedback.
- **`Button = "none"`** draws no button at all, for an app that wants its own controls to be the only way in.

The floating button is the easy way in, and it floats over a corner of the page, where it can cover
a composer, an input bar or a bottom navigation. When it does, set `Button = "none"` and draw a
control of your own that opens the sheet, as below, in a header, a menu or beside the input.

An app may still place its own "Send feedback" control, such as in an account menu or a help page, but it does not need one:

<!-- ikon-example: px-in-app-feedback -->
```csharp
if (view.CanOpenFeedback())
{
    view.Button([Button.OutlineMd], text: "Send feedback",
        onClick: async () => await ClientFunctions.OpenFeedbackAsync());
}
```

`OpenFeedbackAsync` opens the same sheet the platform's button does, with the screenshot taken at the click. `view.CanOpenFeedback()` is false for anyone the setting does not reach, so the control is left out for them rather than shown and doing nothing; it turns true a moment after the person joins, and the view re-renders when it does. A link with `?ikon-feedback` opens the sheet as well, for pointing a tester straight at it.

## Architecture Summary

1. **Server-side logic**: All UI logic, state, and event handlers run on the server
2. **Reactive updates**: Changes to reactive values trigger targeted re-renders
3. **Differential sync**: Only UI diffs are sent to clients
4. **Scoped state**: `ClientReactive<T>` / `UserReactive<T>` / `MountReactive<T>` (and their `ReactiveList` / `ReactiveDictionary` variants) give per-client, per-user, and per-mount state from the same UI code
5. **Lightweight clients**: Clients render the UI tree and forward events to the server
6. **Themed components**: Every styled component ships a themed default, rendered when `style:` is omitted; a `style:` array replaces it unless the array asks for the merge with the `"default"` marker
7. **Crosswind styling**: Tailwind-compatible utility classes with motion extensions
8. **Snapshot privacy**: the boot snapshot is skeletonized by default; `SnapshotReveal` opts safe content back in, `SnapshotHide` / `SnapshotOnly` cover the rest
