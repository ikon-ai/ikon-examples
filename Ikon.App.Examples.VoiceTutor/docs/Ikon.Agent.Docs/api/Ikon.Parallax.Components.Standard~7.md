namespace Ikon.Parallax.Components.Standard
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
    // Header and footer stay pinned; the body scrolls. Canonical shape for dialogs, side panels, and chat layouts needing fixed chrome around an overflowing region: one flex flex-col overflow-hidden column with a flex-1 ScrollArea body, so the chrome and the body's sizing cannot be mis-assembled by hand. The outer height is the caller's responsibility — set it via style (e.g. "h-[82vh]") or let a flex-1 parent provide bounds.
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
  static class SelectExtensions
    // Provide either options (a flat list) or groups — not both. An Input.* token passed as the Select's own style is ignored (with a dev warning) — the trigger is the field element and already carries the field theme; customize it through triggerStyle, where Select.Size tokens ([Select.Size.Sm] / [Select.Size.Lg], default medium) control sizing.
    // value: Controlled selected value. A controlled value with no write-back handler (no bind, no onValueChange) renders the select read-only.
    // label: Optional field label rendered above the select.
    // bind: Two-way binds the select to a Reactive<T> — reads bind.Value and writes it back on every selection. When set, value: is ignored and onValueChange still fires after the write-back.
    static void Select(this UIView view, string[]? style = null, IReadOnlyList<SelectOption>? options = null, IReadOnlyList<SelectOptionGroup>? groups = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, bool? required = null, bool? open = null, string? name = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? itemStyle = null, string[]? itemIndicatorStyle = null, string? indicatorIconName = "check", string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null, Reactive<string>? bind = null, string? ariaLabel = null)
  sealed record SelectOption
    ctor(string Value, string Label, bool Disabled = false)
    bool Disabled { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  sealed record SelectOptionGroup
    ctor(string? Label, IReadOnlyList<SelectOption> Options)
    string? Label { get; init; }
    IReadOnlyList<SelectOption> Options { get; init; }
  // Tones resolve to the theme's semantic color tokens, so they render correctly in both light and dark mode.
  enum SemanticTone
    Neutral
    Brand
    Success
    Warning
    Error
    Info
  readonly struct ShaderUniform
    string Type { get; }
    object Value { get; }
    static ShaderUniform Bool(bool value)
    static ShaderUniform Float(float value)
    static ShaderUniform Int(int value)
    static ShaderUniform Vec2(float x, float y)
    static ShaderUniform Vec3(float x, float y, float z)
    static ShaderUniform Vec4(float x, float y, float z, float w)
  static class ShadertoyExtensions
    // The shader source must define void mainImage(out vec4 color, in vec2 fragCoord). Built-in uniforms: iResolution (vec3: width, height, 1.0), iTime and iTimeDelta (float, seconds), iFrame (int), iMouse (vec4: x, y, click x, click y; requires enableMouse), iDate (vec4: year, month, day, seconds of day). Channel textures use Shadertoy's defaults (vertical flip on, repeat wrap, mipmap filtering); iChannelResolution[4] is 0 until a texture loads and iChannelTime[4] is always 0. Limitations: 2D image channels only — no cubemap, buffer, audio, or video — and single output.
    // shaderSource: Required — an empty source throws ArgumentException.
    // channels: Up to four image URLs (data URIs or http(s)) bound to iChannel0..iChannel3 in array order.
    static void ShadertoyCanvas(this UIView view, string[]? style = null, string? shaderSource = null, int? fps = null, IReadOnlyDictionary<string, ShaderUniform>? uniforms = null, IReadOnlyList<string>? channels = null, bool? enableMouse = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  sealed record ShareActionOptions : ActionOptions
    ctor()
    string? Text { get; init; }
    string? Title { get; init; }
    string? Url { get; init; }
  // The dialog's portal + content styling is repositioned per side via the Theming.Sheet / Theming.Drawer token recipes; slide-in/out motion is driven by the panel's data-state attribute.
  static class SheetExtensions
    // Same open/close model as Sheet: in controlled mode (open set) pass onOpenChange and flip your state to false there, or the drawer cannot be dismissed.
    // trigger: Builder for the element that opens the drawer (uncontrolled mode).
    static void Drawer(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showHandle = true, string[]? style = null, string[]? overlayStyle = null, string[]? handleStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string? key = null)
    // In controlled mode (open set) pass onOpenChange and flip your state to false there, or the close button and outside clicks cannot dismiss the sheet. Caller styles replace the themed panel token, or merge over it with a leading "default" marker.
    // trigger: Builder for the element that opens the sheet (uncontrolled mode).
    static void Sheet(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, Side side = Right, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showClose = true, string[]? style = null, string[]? overlayStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? closeStyle = null, string? key = null)
  enum Side
    Top
    Right
    Bottom
    Left
  static class SkeletonExtensions
    // The default fill for content redacted from the build-time boot snapshot (see SnapshotReveal). A typed convenience over the Skeleton.* theme tokens (a div with animate-pulse styling); size and shape via size / shape, or override freely through style.
    // style: Crosswind/Tailwind utility classes appended to the base skeleton styling (e.g. an explicit width).
    // shape: Outline shape — Rectangle (default), Circle, or Square.
    // size: Height preset — Xs, Sm, Md (default), Lg, or Xl.
    // props: Additional properties passed directly to the underlying element.
    static void Skeleton(this UIView view, string[]? style = null, SkeletonShape shape = Rectangle, SkeletonSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum SkeletonShape
    Rectangle
    Circle
    Square
  enum SkeletonSize
    Xs
    Sm
    Md
    Lg
    Xl
  // The boot snapshot is a public asset painted to everyone before the live connection, so by default the snapshot render replaces every content leaf with a skeleton — per-user content can never leak. These wrappers override that default for specific regions, branching on UIView.IsSnapshot so the app keeps a single UI.Root definition.
  static class SnapshotExtensions
    // Renders content live but omits it entirely from the boot snapshot — not even a skeleton placeholder.
    static void SnapshotHide(this UIView view, Action<UIView> content)
    // Renders content only in the boot snapshot, never live; the filler is rendered as authored (not auto-skeletonized).
    static void SnapshotOnly(this UIView view, Action<UIView> content)
    // Renders content as real content in the boot snapshot instead of skeletons — use only for content safe to bake into the public snapshot (logos, static chrome, marketing copy). The opt-out covers the whole subtree.
    static void SnapshotReveal(this UIView view, Action<UIView> content)
  enum SortStrategy
    VerticalList
    HorizontalList
  sealed record SortableReorderArgs
    ctor(string ActiveId, string OverId, int OldIndex, int NewIndex, IReadOnlyList<string> NewOrder)
    string ActiveId { get; init; }
    int NewIndex { get; init; }
    IReadOnlyList<string> NewOrder { get; init; }
    int OldIndex { get; init; }
    string OverId { get; init; }
  enum SpinnerSize
    Sm
    Md
    Lg
  enum StatTrend
    // The delta renders in a neutral tone without an arrow.
    Flat
    // Trending-up arrow in the success tone.
    Up
    // Trending-down arrow in the error tone.
    Down
  enum Sticky
    Partial
    Always
  // The constructor's parameters are camelCase like every view method's — content:, disabled: — so a tab is written the way the panel it holds is.
  record TabItem
    // value: Unique identifier for the tab.
    // label: Text label displayed on the tab trigger.
    // content: Builder function for rendering the tab's content panel.
    // disabled: When true, prevents user interaction with this tab.
    // forceMount: When true, the tab's content is mounted in the DOM even when inactive (Radix hides via data-state="inactive") and, with lazyPanels, is built alongside the active panel. Use this for heavy panels you want built once and kept mounted across tab switches; the trade-off is a slower first live render and any mount-time effects firing on hidden panels. The boot snapshot never includes an inactive panel's body, forced or not — it arrives with the live tree.
    ctor(string value, string label, Action<UIView> content, bool disabled = false, bool forceMount = false)
    Action<UIView> Content { get; init; }
    bool Disabled { get; init; }
    bool ForceMount { get; init; }
    string Label { get; init; }
    string Value { get; init; }
