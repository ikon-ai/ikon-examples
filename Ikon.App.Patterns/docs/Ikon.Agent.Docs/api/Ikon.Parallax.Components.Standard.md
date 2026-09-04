namespace Ikon.Parallax.Components.Standard
  static class AccessibilityExtensions
    static void AccessibleIcon(this UIView view, string[]? style = null, string? label = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Hidden visually but still exposed to screen readers.
    static void VisuallyHidden(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  record ActionEvent
    ctor(string ActionType, bool Success)
    string ActionType { get; init; }
    bool Success { get; init; }
  enum ActionKind
    Unknown
    CaptureImage
    CopyToClipboard
    DownloadFile
    ExitFullscreen
    GetLocation
    PickContacts
    RequestFullscreen
    Share
  abstract record ActionOptions
  enum ActivationMode
    Automatic
    Manual
  static class AlertExtensions
    // The icon defaults per tone (success check, warning triangle, error alert, info circle).
    // tone: Selects the Alert color variant; Neutral and Brand use the default surface.
    // description: Muted body text under the title.
    // icon: Lucide icon name overriding the tone's default icon.
    // onDismiss: When set, renders a dismiss button in the top-right corner.
    // content: Extra elements rendered under the description.
    static void Alert(this UIView view, string title, SemanticTone tone = Neutral, string[]? style = null, string? description = null, string? icon = null, bool showIcon = true, Func<Task>? onDismiss = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? iconStyle = null, string[]? dismissStyle = null, Action<UIView>? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum Align
    Start
    Center
    End
  static class BadgeExtensions
    // With no style args it renders the themed Theming.Badge.* pill for the tone; caller styles replace the base token, or merge on top of it when the array leads with "default".
    // outline: When true, uses the outlined variant: the tone's border becomes visible instead of transparent. The fill is unchanged.
    // dot: When true, renders a small status dot before the label in the badge's current color.
    // dotStyle: Style for the dot. Defaults to a 6px circle filled with the badge foreground color.
    static void Badge(this UIView view, string text, SemanticTone tone = Neutral, string[]? style = null, BadgeSize size = Md, bool outline = false, bool dot = false, string[]? dotStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum BadgeSize
    // 22px pill with extra-small text.
    Sm
    // 24px pill with small text (the default).
    Md
    // 28px pill with small text.
    Lg
  static class BreadcrumbExtensions
    // Items with an OnClick render as clickable links; the last item always renders as the non-clickable current page (aria-current="page") regardless of its OnClick.
    // items: Trail entries in root-to-current order.
    // separatorIcon: Lucide icon name for the separator; defaults to "chevron-right".
    static void Breadcrumb(this UIView view, IReadOnlyList<BreadcrumbItem> items, string[]? style = null, string? separatorIcon = null, string[]? linkStyle = null, string[]? itemStyle = null, string[]? pageStyle = null, string[]? separatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record BreadcrumbItem
    // Label: Visible text of the crumb.
    // OnClick: Navigation callback. Ignored for the last item, which always renders as the non-clickable current page.
    ctor(string Label, Func<Task>? OnClick = null)
    string Label { get; init; }
    Func<Task>? OnClick { get; init; }
  static class CalendarExtensions
    // All date values (value, defaultValue, minDate, maxDate, callbacks) are ISO yyyy-MM-dd strings; month is yyyy-MM. Controlled via value+onValueChange; omit both and pass defaultValue for uncontrolled.
    // month: Controlled display month; accepts yyyy-MM or yyyy-MM-dd.
    // minDate: Earliest selectable date (inclusive).
    // maxDate: Latest selectable date (inclusive).
    // locale: BCP-47 locale used for weekday and month labels (e.g. en-US).
    static void Calendar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? month = null, string? defaultMonth = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, string? locale = null, bool? disabled = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onMonthChange = null)
    // Renders a trigger button plus a popover Calendar. Date values are ISO yyyy-MM-dd strings; controlled via value+onValueChange, uncontrolled via defaultValue.
    // format: BCP-47 locale format hint for the trigger label (e.g. en-US).
    // minDate: Earliest selectable date (inclusive).
    // maxDate: Latest selectable date (inclusive).
    // label: Field label rendered above the picker, matching TextField.
    static void DatePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, string? format = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? calendarStyle = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Maps to the W3C MediaStream facingMode constraint as an "ideal" hint — the browser falls back to whatever camera is available if the requested side does not exist (e.g. desktops without a rear camera).
  enum CameraFacing
    User
    Environment
  sealed record CaptureImageActionOptions : ActionOptions
    ctor()
    CaptureImageConstraints? Constraints { get; init; }
    ClientImageCaptureFormat? Format { get; init; }
    int? Height { get; init; }
    // Defaults to CaptureImageMode.Headless.
    CaptureImageMode? Mode { get; init; }
    // 0.0 to 1.0; applies to lossy formats.
    double? Quality { get; init; }
    int? Width { get; init; }
  // Applied directly in CaptureImageMode.Headless mode. In CaptureImageMode.Native mode only FacingMode is honored (mapped to the file input's capture attribute); the OS camera UI ignores the other constraints.
  sealed record CaptureImageConstraints
    ctor()
    string? DeviceId { get; init; }
    CameraFacing? FacingMode { get; init; }
  enum CaptureImageMode
    // Native OS camera UI (preview + shutter + front/back toggle on phones). On mobile it uses a transient <input type="file" capture> and must be invoked from a user gesture; the user can dismiss without capturing. On desktop browsers it transparently falls back to the headless getUserMedia grab.
    Native
    // Silent capture: getUserMedia grabs a single frame off-screen and tears the stream down — no preview, no shutter. Honors CaptureImageConstraints.
    Headless
  static class CardExtensions
    // With no style args it renders the themed card token (Theming.Card.Default, or Theming.Card.Interactive when onClick is set); caller styles replace it, or merge on top of it when the array leads with "default".
    // header: Extra header elements rendered after the title/description.
    // contentStyle: Defaults to Theming.Card.Content when a header is present, plain padding otherwise.
    // onClick: Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Card(this UIView view, string[]? style = null, string? title = null, string? description = null, Action<UIView>? header = null, Action<UIView>? content = null, Action<UIView>? footer = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? contentStyle = null, string[]? footerStyle = null, Delegate? onClick = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void Card(this UIView view, string[]? style, Action<UIView> children)
    // description: Muted explanation text under the title.
    // icon: Lucide icon name rendered inside the tinted icon square.
    // action: Builder for the action row (e.g. a "Create" button).
    static void EmptyState(this UIView view, string title, string[]? style = null, string? description = null, string? icon = null, Action<UIView>? action = null, string[]? iconWrapStyle = null, string[]? iconStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? actionsStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // delta: Delta text rendered next to the value (e.g. "+12%").
    // trendLabel: Muted context text after the delta (e.g. "vs last month").
    // icon: Lucide icon name rendered inside the tinted icon box on the right.
    static void StatCard(this UIView view, string label, string value, string[]? style = null, string? delta = null, StatTrend trend = Flat, string? trendLabel = null, string? icon = null, SemanticTone iconTone = Neutral, string[]? labelStyle = null, string[]? valueStyle = null, string[]? trendStyle = null, string[]? iconBoxStyle = null, string[]? iconStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum CarouselAlign
    Start
    Center
    End
  sealed record CarouselBreakpoint
    // MinWidth: Container width (in CSS pixels) at which this breakpoint becomes active. The active breakpoint is the one with the largest MinWidth still less than or equal to the current container width.
    // SlidesPerView: Number of slides visible in the viewport at this breakpoint.
    // SlidesPerGroup: Number of slides advanced per navigation step at this breakpoint. Defaults to SlidesPerView when null.
    // SlideGapPx: Gap in CSS pixels between adjacent slides at this breakpoint. Defaults to the top-level slideGapPx when null.
    ctor(int MinWidth, int SlidesPerView, int? SlidesPerGroup = null, int? SlideGapPx = null)
    int MinWidth { get; init; }
    int? SlideGapPx { get; init; }
    int? SlidesPerGroup { get; init; }
    int SlidesPerView { get; init; }
  static class CarouselExtensions
    // Provide slides via slides for the simple case, or via the content builder using Slide for fully custom children.
    // index: Controlled zero-based slide index.
    // defaultIndex: Initial slide index for uncontrolled mode.
    // slidesPerView: Slides visible at once; defaults to 1. Overridden by the matching breakpoints entry.
    // slidesPerGroup: Slides advanced per navigation step; defaults to slidesPerView. Overridden by the matching breakpoints entry.
    // slideGapPx: Gap between adjacent slides; only takes effect when the effective slides-per-view exceeds 1. Defaults to 0.
    // breakpoints: Responsive overrides keyed by container width; see CarouselBreakpoint.
    // showArrows: Renders the Previous/Next buttons; defaults to true.
    // showIndicators: Renders the indicator dots; defaults to true.
    static void Carousel(this UIView view, string[]? style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, int? slidesPerView = null, int? slidesPerGroup = null, int? slideGapPx = null, IEnumerable<CarouselBreakpoint>? breakpoints = null, IEnumerable<CarouselSlideItem>? slides = null, bool? showArrows = null, bool? showIndicators = null, string? previousLabel = null, string? nextLabel = null, string? previousIconName = null, string? nextIconName = null, string[]? rootStyle = null, string[]? viewportStyle = null, string[]? slideStyle = null, string[]? previousStyle = null, string[]? nextStyle = null, string[]? indicatorsStyle = null, string[]? indicatorStyle = null, string[]? indicatorActiveStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<int, Task>? onIndexChange = null)
    static void Slide(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  sealed record CarouselSlideItem
    // Content: Builder function for rendering the slide.
    // Key: Optional stable key used for diffing.
    ctor(Action<UIView> Content, string? Key = null)
    Action<UIView> Content { get; init; }
    string? Key { get; init; }
  static class ChatLogExtensions
    // Use instead of a manual Column(overflow-auto) for any "newest at the bottom, follow when content grows" layout. autoScrollKey tells the framework when to re-anchor to the bottom — pass the reactive message collection, a count, or any other value that changes when the content does.
    // autoScrollKey: Anything whose value changes when the content changes — the reactive message collection, a count, or a composite string (see LayoutExtensions.ScrollArea).
    static void ChatLog(this UIView view, string[]? style = null, object? autoScrollKey = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, string? styleId = null, string? key = null)
  enum CheckedState
    Unchecked
    Checked
    Indeterminate
  static class CodeEditorExtensions
    // value: Controlled text value; with no write-back handler (onValueChange or onSubmit) the editor renders read-only.
    // language: Syntax-highlighting language identifier (e.g. typescript, csharp, json).
    // readOnly: Prevents editing but allows selection and copy.
    // showLineNumbers: Defaults to true.
    // tabSize: Spaces inserted by Tab; defaults to 2.
    // onSubmit: Invoked when the user presses Ctrl+Enter. The parameter contains the submitted value — prefer it over re-reading the bound reactive.
    static void CodeEditor(this UIView view, string? value = null, string? defaultValue = null, string? language = null, string? placeholder = null, bool? readOnly = null, bool? disabled = null, bool? showLineNumbers = null, int? tabSize = null, bool? insertSpaces = null, bool? wrap = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? gutterStyle = null, string[]? contentStyle = null, string[]? languageBadgeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  enum CollisionDetection
    ClosestCenter
    ClosestCorners
    RectIntersection
    PointerWithin
  enum ColorFormat
    Hex
    Rgb
    Hsl
  static class ColorPickerExtensions
    // value: Controlled color in the chosen format.
    // defaultValue: Initial color for uncontrolled mode.
    // format: Output format produced by onValueChange.
    // showAlpha: When true, shows an alpha slider and emits #RRGGBBAA/rgba()/hsla().
    // onValueChange: Fires continuously as the user drags or types a new color.
    // onValueCommit: Fires once the user releases a drag or commits a typed value.
    // label: Field label rendered above the picker, matching TextField.
    static void ColorPicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, ColorFormat format = Hex, bool? showAlpha = null, IReadOnlyList<string>? presets = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onValueCommit = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  enum ColumnAlign
    Left
    Center
    Right
  sealed record ComposerAttachment
    ctor(string Name, string Mime, long Size)
    string Mime { get; init; }
    string Name { get; init; }
    long Size { get; init; }
  static class ComposerExtensions
    // A complete input bar — attach button, drag-and-drop, paste, auto-growing text, optional push-to-talk — so apps do not rebuild it. Stateless: pass the draft in value and the pending files in attachments, and store what the callbacks hand back. onSubmit receives the submitted text; prefer it over re-reading the draft, which a surface switch can clear between the keystroke and the handler. The mic renders only when both capture callbacks are wired; transcription is the app's job. Per-slot style parameters restyle every part, and label parameters localize every string.
    // seedSelectionIds: Ids from a prior FilePicker selection, uploaded on mount (see FileUploadZone).
    // onAttachmentAdded: A file was picked, dropped, or pasted and finished uploading; its temp path is in the args.
    // onCaptureStop: Push-to-talk released — transcribe the capture and append it to the draft.
    static void Composer(this UIView view, string[]? style = null, string? value = null, string? placeholder = null, bool busy = false, IReadOnlyList<ComposerAttachment>? attachments = null, string[]? accept = null, long? maxFileSize = null, int? maxRows = null, bool? autoFocus = null, string[]? seedSelectionIds = null, string[]? fieldStyle = null, string[]? chipStyle = null, string[]? attachButtonStyle = null, string[]? sendButtonStyle = null, string[]? micStyle = null, string[]? activeStyle = null, string attachLabel = "Attach files", string sendLabel = "Send", string holdToTalkLabel = "Hold to talk", string releaseLabel = "Release to send", string? key = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<FileUploadCompleteArgs, Task>? onAttachmentAdded = null, Func<FileUploadErrorArgs, Task>? onAttachmentError = null, Func<int, Task>? onAttachmentRemoved = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null)
  sealed record ContactsActionEvent : ActionEvent
    ctor(bool Success, IReadOnlyList<ClientContact>? Contacts)
    IReadOnlyList<ClientContact>? Contacts { get; init; }
