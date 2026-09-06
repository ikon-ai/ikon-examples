namespace Ikon.Parallax.Components.Standard
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
    // forceMount: When true, the tab's content is mounted in the DOM even when inactive (Radix hides via data-state="inactive"). Use this for heavy panels you want to amortise into initial paint and keep mounted across tab switches; the trade-off is a slower first render and any mount-time effects firing on hidden panels.
    ctor(string value, string label, Action<UIView> content, bool disabled = false, bool forceMount = false)
    Action<UIView> Content { get; init; }
    bool Disabled { get; init; }
    bool ForceMount { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  // The styled middle ground between hand-rolled Grid/Row layouts and the payload-driven DataTable component. CSS table display utilities align columns automatically without a shared grid template. Compose Table > TableHeader/TableBody > TableRow > TableHead/TableCell.
  static class TableExtensions
    // Caller styles replace the base token; lead the array with "default" to merge over it.
    // style: Crosswind/Tailwind utility classes merged on top of the table base token.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for the table's header/body groups.
    static void Table(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Table(this UIView view, string[]? style, Action<UIView> children)
    // style: Crosswind/Tailwind utility classes merged on top of the row group token.
    // content: Builder function for the body rows.
    static void TableBody(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // style: Crosswind/Tailwind utility classes merged on top of the cell token.
    // text: Cell text. For custom content use content instead.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for custom cell content.
    static void TableCell(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void TableCell(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // style: Crosswind/Tailwind utility classes merged on top of the header cell token.
    // text: Column label. For custom content use content instead.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for custom cell content.
    static void TableHead(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void TableHead(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // style: Crosswind/Tailwind utility classes merged on top of the header group token.
    // content: Builder function for the header rows.
    static void TableHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Rows with onClick also get hover highlight + pointer cursor.
    // style: Crosswind/Tailwind utility classes merged on top of the row token.
    // striped: When true, even rows get a subtle background (zebra striping via CSS :nth-child).
    // onClick: Invoked when the user clicks the row. Accepts sync (() => …) and async (async () => …) lambdas alike.
    // content: Builder function for the row's cells.
    static void TableRow(this UIView view, string[]? style = null, bool striped = false, Delegate? onClick = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
  static class TabsExtensions
    // Style slots (default theme tokens): listStyle → Tabs.List, triggerStyle → Tabs.Trigger, contentStyle → Tabs.Content; rootStyle is the outer container (rarely needed).
    // listContainerStyle: When provided, wraps the TabsList in a styled Box.
    // disabledTriggerStyle: Style for disabled tab triggers; falls back to triggerStyle.
    // contentContainerStyle: When provided, wraps all content panels in a styled Box.
    // lazyPanels: When true (controlled tabs only), the server builds only the active tab's panel plus any TabItem.ForceMount panels; a switch fetches the new panel in the same round-trip that confirms it, and the client keeps the old panel visible until it arrives. Cuts per-client server memory and wire size by roughly the tab count at the cost of one round-trip per switch. Default false: every panel ships and switching is instant. Ignored for uncontrolled tabs (they switch client-side).
    static void Tabs(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, ActivationMode activationMode = Automatic, IEnumerable<TabItem>? tabs = null, string[]? listContainerStyle = null, string[]? listStyle = null, string[]? triggerStyle = null, string[]? disabledTriggerStyle = null, string[]? contentContainerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, bool lazyPanels = false)
  enum TimeGranularity
    Hour
    Minute
    Second
  static class TimePickerExtensions
    // Values are ISO-8601 HH:mm or HH:mm:ss strings; the emitted value is always 24-hour regardless of hourFormat. A controlled value without onValueChange renders read-only.
    // minuteStep: Minute step (5, 10, 15, 30…); defaults to 1.
    // secondStep: Second step; defaults to 1.
    // label: Optional field label rendered above the picker.
    static void TimePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, HourFormat hourFormat = Hour24, TimeGranularity granularity = Minute, int? minuteStep = null, int? secondStep = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string? placeholder = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? columnStyle = null, string[]? itemStyle = null, string[]? itemSelectedStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  sealed record ToastItem
    // Id: Queue-unique identifier used to dismiss the toast.
    // Title: Headline text.
    // Description: Optional muted body text.
    // Tone: Semantic tone controlling the icon and its color.
    // DurationMs: Milliseconds before the client auto-dismisses the toast.
    ctor(long Id, string Title, string? Description, SemanticTone Tone, int DurationMs)
    string? Description { get; init; }
    int DurationMs { get; init; }
    long Id { get; init; }
    string Title { get; init; }
    SemanticTone Tone { get; init; }
  sealed record ToastSwipeArgs
    ctor(ToastSwipeDirection Direction, double DeltaX, double DeltaY)
    double DeltaX { get; init; }
    double DeltaY { get; init; }
    ToastSwipeDirection Direction { get; init; }
  enum ToastSwipeDirection
    Left
    Right
    Up
    Down
  enum ToastType
    Foreground
    Background
  // Wiring: construct one instance as an app field, mount ToastsExtensions.ToastHost once in the root UI, then fire notifications (e.g. _toasts.Success(...)) from any handler. State lives in a ClientReactive<T>, so methods must be called where a client scope is active (UI render or event handlers) and each client sees only its own toasts. Auto-dismiss is client-driven off ToastItem.DurationMs.
  sealed class Toasts
    ctor()
    IReadOnlyList<ToastItem> Items { get; }
    void Clear()
    void Dismiss(long id)
    long Error(string title, string? description = null, int durationMs = 5000)
    long Info(string title, string? description = null, int durationMs = 5000)
    // title: Headline text.
    // description: Optional muted body text.
    // tone: Semantic tone controlling the icon and its color.
    // durationMs: Milliseconds before the client auto-dismisses the toast.
    long Show(string title, string? description = null, SemanticTone tone = Neutral, int durationMs = 5000)
    long Success(string title, string? description = null, int durationMs = 5000)
    long Warning(string title, string? description = null, int durationMs = 5000)
    const int DefaultDurationMs = 5000
  static class ToastsExtensions
    // Mount exactly once in the root UI; every queued toast renders as a themed toast (tone icon, title, description, close button) that the client auto-dismisses after its duration. Both auto-dismiss and the close button report back and remove the item from the queue.
    // toasts: The queue to render.
    // viewportStyle: Style for the toast viewport. Defaults to Theming.Toast.Viewport.
    // toastStyle: Crosswind/Tailwind utility classes merged on top of Theming.Toast.Default for each toast.
    // titleStyle: Style for the title. Defaults to Theming.Toast.Title.
    // descriptionStyle: Style for the description. Defaults to Theming.Toast.Description.
    // closeStyle: Style for the close button. Defaults to Theming.Toast.Close.
    // showClose: Whether to render the × close button on each toast.
    static void ToastHost(this UIView view, Toasts toasts, string[]? viewportStyle = null, string[]? toastStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, bool showClose = true)
  static class TreeViewExtensions
    // Expansion state lives in a caller-held ExpandedSet — declare it as an app field (private readonly ExpandedSet _expanded = new();). Clicking a branch toggles its expansion and selects it in the same click.
    // id: Stable unique id per node — used for diff keys, expansion, and selection.
    // children: Child nodes per node; null or empty marks a leaf.
    // style: Merged on top of Theming.NavPanel.Ghost for the tree container.
    // icon: Optional per-node Lucide icon name rendered before the label.
    // itemStyle: Row style; defaults to Theming.NavItem.Md + Theming.NavItem.Default.
    // selectedItemStyle: Selected-row style; defaults to Theming.NavItem.Md + Theming.NavItem.Active.
    static void TreeView<T>(this UIView view, IReadOnlyList<T> roots, Func<T, string> id, Func<T, string> label, Func<T, IReadOnlyList<T>?> children, ExpandedSet expanded, string[]? style = null, Func<T, Task>? onSelect = null, string? selectedId = null, Func<T, string?>? icon = null, string[]? itemStyle = null, string[]? selectedItemStyle = null, string[]? labelStyle = null, string[]? childrenStyle = null, string? styleId = null, string? key = null)
