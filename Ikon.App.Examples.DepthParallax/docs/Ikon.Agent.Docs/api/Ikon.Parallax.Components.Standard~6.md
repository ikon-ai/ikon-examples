namespace Ikon.Parallax.Components.Standard
  // Each component manages its Portal/Overlay wrapper automatically.
  static class OverlayExtensions
    // Style slots: overlayStyle → AlertDialog.Overlay, contentStyle → AlertDialog.Content, titleStyle → AlertDialog.Title, descriptionStyle → AlertDialog.Description, footerStyle → AlertDialog.Footer, cancelStyle → AlertDialog.Cancel, actionStyle → AlertDialog.Action.
    // cancelLabel: Label for the cancel button. Defaults to "Cancel".
    // contentSlot: When provided, overrides the title/description/action parameters for full custom control.
    static void AlertDialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? title = null, string? description = null, string? cancelLabel = null, string? actionLabel = null, Func<Task>? onAction = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? cancelStyle = null, string[]? actionStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Style slots: overlayStyle → Dialog.Overlay, contentStyle → Dialog.Content.
    // modal: When true, prevents interaction with elements behind the dialog.
    // footer: Rendered after the content in a themed footer row (buttons right-aligned on wide screens, stacked on narrow).
    static void Dialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? headerStyle = null, Action<UIView>? footer = null, string[]? footerStyle = null)
    // Style slots: contentStyle → HoverCard.Content.
    // style: Alias for contentStyle — the first positional styles the floating content panel; contentStyle wins when both are given.
    static void HoverCard(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: contentStyle → Popover.Content.
    // style: Alias for contentStyle — the first positional styles the floating content panel; contentStyle wins when both are given.
    // modal: When true, prevents interaction with elements outside the popover.
    static void Popover(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: toastStyle → Toast.Default, viewportStyle → Toast.Viewport, titleStyle → Toast.Title, descriptionStyle → Toast.Description, closeStyle → Toast.Close.
    // durationMs: Duration in milliseconds before auto-dismiss.
    // forceMount: When true, keeps the toast in the DOM even when closed.
    // showClose: Whether to show the close button. Defaults to true with the simplified API.
    // closeLabel: Label for the close button. Defaults to "×".
    // content: When provided, overrides the title/description/close parameters for full custom control.
    // onPause: Invoked when the toast timer pauses (e.g., on hover).
    static void Toast(this UIView view, string[]? style = null, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string? title = null, string? description = null, bool? showClose = null, string? closeLabel = null, Action<UIView>? content = null, string[]? toastStyle = null, string[]? viewportStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Func<Task>? onEscapeKeyDown = null, Func<Task>? onPause = null, Func<Task>? onResume = null, Func<ToastSwipeArgs, Task>? onSwipeStart = null, Func<ToastSwipeArgs, Task>? onSwipeMove = null, Func<ToastSwipeArgs, Task>? onSwipeEnd = null, Func<ToastSwipeArgs, Task>? onSwipeCancel = null)
    // Style slots: contentStyle → Tooltip.Content.
    // style: Alias for contentStyle — the first positional styles the floating content bubble; contentStyle wins when both are given.
    // skipDelayDuration: Delay in milliseconds when switching between tooltips.
    // disableHoverableContent: When true, prevents hoverable content from keeping the tooltip open.
    static void Tooltip(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
  // C# composites over the Popover/Dialog primitives (no bespoke node type). Filtering is server-side over the app's reactive search state.
  static class OverlayMenuExtensions
    // Filtering is server-side: bind searchValue to a reactive and echo edits via onSearchChange for the list to narrow by case-insensitive label match. Without a bound search value it renders as a plain Popover-select (no filtering).
    // options: The full option set; the component filters it by searchValue.
    // value: The selected option's value (drives the trigger label and the check mark).
    // onValueChange: Fires with the chosen option's value.
    // searchValue: Current search text (bind to a reactive for live filtering).
    // onSearchChange: Fires as the user types in the search field.
    // open: Controlled open state; omit to let the popover self-manage.
    // onOpenChange: Fires when the panel opens or closes.
    // placeholder: Trigger text when nothing is selected.
    // emptyText: Shown when the filter matches no option.
    static void Combobox(this UIView view, IReadOnlyList<SelectOption> options, string? value = null, Func<string, Task>? onValueChange = null, string? searchValue = null, Func<string, Task>? onSearchChange = null, bool? open = null, Func<bool, Task>? onOpenChange = null, string? placeholder = "Select…", string? searchPlaceholder = "Search…", string? emptyText = "No results.", string[]? style = null, string[]? triggerStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Filtering is server-side over searchValue: each group narrows by case-insensitive label match and empty groups drop out. onSelect fires with the chosen option's value.
    // groups: Grouped actions; each option's Value is what onSelect receives.
    // open: Controlled open state of the dialog.
    // onOpenChange: Fires when the palette opens or closes.
    // onSelect: Fires with the selected option's value.
    // searchValue: Current search text (bind to a reactive for live filtering).
    // onSearchChange: Fires as the user types.
    // placeholder: Search-field placeholder.
    // emptyText: Shown when nothing matches.
    static void CommandPalette(this UIView view, IReadOnlyList<SelectOptionGroup> groups, bool? open = null, Func<bool, Task>? onOpenChange = null, Func<string, Task>? onSelect = null, string? searchValue = null, Func<string, Task>? onSearchChange = null, string? placeholder = "Type a command or search…", string? emptyText = "No results.", string[]? panelStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Fill content with view.Button([Menu.Item]) / [Menu.ItemDestructive] rows plus Menu.Label / Menu.Separator; the component supplies the trigger wiring and the menu-shaped popover panel.
    // trigger: The clickable element that opens the menu (usually a Button).
    // content: The menu body — item rows, labels, separators.
    // open: Controlled open state; omit to let the popover self-manage.
    // side: Which side of the trigger the panel opens on. Defaults to below.
    // align: Panel alignment along the trigger edge. Defaults to start (left).
    // contentStyle: Extra classes on the menu panel (defaults to the popover-menu surface).
    // onOpenChange: Fires when the menu opens or closes.
    static void DropdownMenu(this UIView view, Action<UIView> trigger, Action<UIView> content, bool? open = null, Side side = Bottom, Align align = Start, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Pass text for a single key, or keys for a combo (one chip per key); keys wins over text.
    // text: The single key/label to render (e.g. "⌘K", "Esc"). Ignored when keys is set.
    // keys: A combo rendered as one chip per key. Wins over text.
    // style: Extra classes layered on the Theming.Kbd.Default chip (or the group wrapper when keys is set).
    static void Kbd(this UIView view, string? text = null, IReadOnlyList<string>? keys = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record Page<T>
    // Items: The slice of Source for the current page.
    // Index: Zero-based current page index.
    // TotalPages: Total number of pages (always >= 1, even when Source is empty).
    // PageSize: Items per page (the configured page size, not necessarily Items.Count).
    // CanPrev: True if there is a previous page.
    // CanNext: True if there is a next page.
    // Prev: Action to bind to a Prev button's onClick. Decrements page; no-op at first.
    // Next: Action to bind to a Next button's onClick. Increments page; no-op at last.
    // JumpTo: Action that moves to a specific page (0-based). Clamps to valid range.
    // First: Action that jumps to page 0.
    // Last: Action that jumps to the last page.
    // Source: The full input list, if the caller wants the original.
    ctor(IReadOnlyList<T> Items, int Index, int TotalPages, int PageSize, bool CanPrev, bool CanNext, Func<Task> Prev, Func<Task> Next, Func<int, Task> JumpTo, Func<Task> First, Func<Task> Last, IReadOnlyList<T> Source)
    bool CanNext { get; init; }
    bool CanPrev { get; init; }
    Func<Task> First { get; init; }
    int Index { get; init; }
    IReadOnlyList<T> Items { get; init; }
    Func<int, Task> JumpTo { get; init; }
    Func<Task> Last { get; init; }
    Func<Task> Next { get; init; }
    int PageSize { get; init; }
    Func<Task> Prev { get; init; }
    IReadOnlyList<T> Source { get; init; }
    int TotalPages { get; init; }
  // Slices an in-memory list and returns the slice plus bound actions (Prev/Next/JumpTo/First/Last) the caller binds to whatever UI fits; holds zero rendering opinion. Most apps don't need pagination — live feeds and large lists are covered by ReactiveList<T> + ScrollArea(autoScroll: true) or virtualization; use this for a static list large enough to warrant explicit page navigation. For DB-backed pagination (load only the current page), drive a ClientReactive<T> page index directly in your data-loading code.
  static class PaginationExtensions
    // page must be a field-level ClientReactive<T>; each client sees its own page, and the returned slice is a snapshot read once, not a live view.
    // view: UIView (extension receiver — unused, present for fluency).
    // items: Source list. Read once; the slice is a snapshot, not a live view.
    // page: Per-client page index. Use a field-level ClientReactive<T> initialized to 0.
    // pageSize: Items per page (must be >= 1; clamped if not).
    static Page<T> Paginate<T>(this UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  static class PanZoomExtensions
    // The pan offset and in-gesture zoom live in the client and never round-trip; only the scale at the end of a gesture is reported through onScaleChange. Scrolling pans, Ctrl/⌘+scroll or a pinch zooms about the pointer, and dragging pans. The viewport clips its content, so give it a size with style (e.g. h-96). For an editor that needs hit-testing or selection in the zoomed space, build a custom node instead.
    // scale: Controlled zoom factor, 1 being natural size. Pass with onScaleChange.
    // defaultScale: Initial zoom factor for uncontrolled mode; defaults to 1.
    // minScale: Lower zoom bound; defaults to 0.25.
    // maxScale: Upper zoom bound; defaults to 4.
    // onScaleChange: Fires once per completed gesture with the resulting scale, clamped to the bounds.
    static void PanZoom(this UIView view, string[]? style = null, double? scale = null, double? defaultScale = null, double? minScale = null, double? maxScale = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<double, Task>? onScaleChange = null)
    static void PanZoom(this UIView view, string[]? style, Action<UIView> children)
  sealed record PickContactsActionOptions : ActionOptions
    ctor()
    bool Multiple { get; init; }
  sealed record PointerDownOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  static class QrCodeExtensions
    // The QR code is generated server-side and rendered as an image.
    // size: Size of the QR code in pixels (default 256).
    static void QR(this UIView view, string[]? style = null, string? value = null, int size = 256, string? key = null)
  static class RichTextEditorExtensions
    // Values are HTML strings. A controlled value with no write-back handler (onValueChange or onSubmit) renders the editor read-only.
    // tools: Explicit toolbar contents; null shows a default toolbar.
    // maxRows: Rows before the content area scrolls.
    // toolbarStyle: Toolbar slot; merges over RichTextEditor.Toolbar.
    // toolbarButtonStyle: Toolbar-button slot; merges over RichTextEditor.ToolbarButton.
    // contentStyle: Editable-content slot; merges over RichTextEditor.Content.
    // onSubmit: Invoked when the user presses Ctrl+Enter.
    static void RichTextEditor(this UIView view, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, IReadOnlyList<RichTextTool>? tools = null, bool? showToolbar = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? toolbarStyle = null, string[]? toolbarButtonStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  enum RichTextTool
    Bold
    Italic
    Underline
    Strikethrough
    Heading1
    Heading2
    Heading3
    Paragraph
    AlignLeft
    AlignCenter
    AlignRight
    BulletList
    NumberedList
    Blockquote
    Code
    Link
    ClearFormatting
    Undo
    Redo
  // Routes, tabs, modes, panel selections, "which dialog is open" — same shape, same primitives; intentionally minimal, with no URL coupling or rendering bias. For URL ↔ signal sync (browser bar, deep links, back/forward), use Navigation on the host app instead.
  static class RoutingExtensions
    // signal holds the active key (per-client); cases maps each known key to a render lambda. Falls back to fallback (or empty) when the active key isn't in the dictionary.
    static void Routed<T>(this UIView view, ClientReactive<T> signal, Dictionary<T, Action<UIView>> cases, Action<UIView>? fallback = null)
    // For the "button that activates a specific route/tab/mode" case: view.Button(text: "Open settings", onClick: view.Set(_route, "settings")).
    static Func<Task> Set<T>(this UIView view, ClientReactive<T> signal, T value)
  enum ScrollAreaScrollbars
    None
    Vertical
    Horizontal
    Both
  enum ScrollAreaType
    Auto
    Always
    Scroll
    Hover
  static class ScrollColumnExtensions
    // Header and footer stay pinned; the body scrolls. Canonical shape for dialogs, side panels, and chat layouts needing fixed chrome around an overflowing region; avoids the flex-1 ScrollArea that won't shrink inside a flex parent (the min-height: auto quirk). The outer height is the caller's responsibility — set it via style (e.g. "h-[82vh]") or let a flex-1 parent provide bounds.
    // autoScroll: Auto-scroll the body to the bottom when content changes.
    // autoScrollKey: Anything whose value changes when the content changes — the reactive collection itself, a count, or a composite string.
    // bodyStyle: Applied to the inner ScrollArea root.
    static void ScrollColumn(this UIView view, string[]? style = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType scrollType = Hover, bool autoScroll = false, object? autoScrollKey = null, string[]? bodyStyle = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string? styleId = null, string? key = null)
  enum ScrollDirection
    Down
    Up
  sealed record ScrollNearEndArgs
    ctor(double ScrollTop, double ScrollHeight, double ClientHeight, ScrollDirection Direction)
    double ClientHeight { get; init; }
    ScrollDirection Direction { get; init; }
    double ScrollHeight { get; init; }
    double ScrollTop { get; init; }
