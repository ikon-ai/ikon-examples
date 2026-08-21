# Ikon.Parallax Public API

namespace Ikon.Parallax
  sealed class ActionArgs<T>
    ctor()
    Context ClientContext { get; init; }
    T Value { get; init; }
  static class ReactiveBusyExtensions
    static Task<bool> RunAsync(this Reactive<bool> busy, Reactive<string?> status, Func<Task> work)
  sealed class ThemeControl
    ClientReactive<Theme> Current { get; }
    Task SetAsync(Theme theme)
    Task ToggleAsync()
  class UI
    ctor(IAppBase app, ITheme theme)
    bool EnableProfiling { get; set; }
    // Default true. A subtree that reads only non-reactive data will not refresh until one of its reactive dependencies changes; set false to force a full re-render every cycle.
    bool EnableSubtreeCaching { get; set; }
    // Build the whole component tree inside content; it re-renders automatically when any reactive state read during the build changes. This is the app's root render entry point.
    void Root(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
    // Call once in Main, before clients join. With followClient true (the default) a joining client keeps its own saved theme and clients without one get defaultTheme; false forces defaultTheme on every client. Bind the returned Current in views and ToggleAsync to a button's onClick.
    ThemeControl UseTheme(Theme defaultTheme = Dark, bool followClient = true)
  class UIView
    string DefaultIconLibrary { get; }
    // True only while capturing the build-time boot snapshot — a public asset shown to everyone before the live UI connects (always false on the live render). Gate per-user or sensitive content on this, preferably via the SnapshotReveal/SnapshotHide/SnapshotOnly wrappers.
    bool IsSnapshot { get; }
    string SnapshotVariant { get; }
    void AddNode(string type, IDictionary? props = null, List<UIViewNode>? children = null, string? key = null, string[]? style = null, string? styleId = null)
    string? CreateAction<T>(Func<ActionArgs<T>, Task>? callback)
    // The returned string is an opaque reference to use as an image src (e.g. on an Image component), not a data URL.
    string RegisterPayload(byte[] data, string mimeType)
  sealed class UIViewNode
    ctor(string type, Guid viewId, Dictionary<string, object?> props, List<UIViewNode>? children = null, string? key = null, IReadOnlyList<string>? styleIds = null, ulong parentPathHash = 0, IReadOnlyCollection<IReadOnlyList<string>>? styleIdProps = null)
    List<UIViewNode> Children { get; }
    string? ContentFingerprint { get; }
    bool HasExplicitKey { get; }
    string Id { get; }
    int IdHash { get; }
    static bool IncludeSourceMarkers { get; set; }
    IReadOnlyDictionary<string, object?> Props { get; }
    string? SourceMarker { get; }
    string? StableHint { get; }
    IReadOnlyList<string> StyleIds { get; }
    string Type { get; }

namespace Ikon.Parallax.Components.Charts
  sealed record AxisConfig
    ctor()
    // For time scales this is a d3-time-format token string (e.g. "%H:%M", "%m/%d %H:%M"), not a .NET format.
    string? Format { get; init; }
    string? Legend { get; init; }
    int? LegendOffset { get; init; }
    int? TickCount { get; init; }
    int? TickPadding { get; init; }
    int? TickRotation { get; init; }
    int? TickSize { get; init; }
    int? TruncateTickAt { get; init; }
  enum BarGroupMode
    Stacked
    Grouped
  enum BarLayout
    Vertical
    Horizontal
  record ChartAxisStyle
    ctor()
    string? DomainColor { get; init; }
    ChartTextStyle? Legend { get; init; }
    string? TickColor { get; init; }
    ChartTextStyle? TickLabel { get; init; }
  sealed record ChartClickArgs
    ctor()
    string? Id { get; init; }
    string? IndexValue { get; init; }
    string? SerieId { get; init; }
    object? Value { get; init; }
  enum ChartColorScheme
    Nivo
    Category10
    Accent
    Dark2
    Paired
    Pastel1
    Pastel2
    Set1
    Set2
    Set3
    Tableau10
    BrownBlueGreen
    PurpleRedGreen
    PinkYellowGreen
    PurpleOrange
    RedBlue
    RedGrey
    RedYellowBlue
    RedYellowGreen
    Spectral
    Blues
    Greens
    Greys
    Oranges
    Purples
    Reds
    BlueGreen
    BluePurple
    GreenBlue
    OrangeRed
    PurpleBlueGreen
    PurpleBlue
    PurpleRed
    RedPurple
    YellowGreenBlue
    YellowGreen
    YellowOrangeBrown
    YellowOrangeRed
  record ChartCrosshairStyle
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  static class ChartExtensions
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip values and value-axis ticks human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void BarChart(this UIView view, string[]? style = null, IEnumerable<Dictionary<string, object>>? data = null, IEnumerable<string>? keys = null, string? indexBy = null, BarGroupMode? groupMode = null, BarLayout? layout = null, ScaleType? valueScale = null, ScaleType? indexScale = null, bool? reverse = null, double? minValue = null, double? maxValue = null, double? padding = null, double? innerPadding = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enableLabel = null, int? labelSkipWidth = null, int? labelSkipHeight = null, string? labelTextColor = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderRadius = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip Y values and left-axis ticks human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void LineChart(this UIView view, string[]? style = null, IEnumerable<LineChartSeries>? data = null, ScaleType? xScaleType = null, ScaleType? yScaleType = null, double? xScaleMin = null, double? xScaleMax = null, double? yScaleMin = null, double? yScaleMax = null, bool? yScaleStacked = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enablePoints = null, int? pointSize = null, string? pointColor = null, string? pointBorderColor = null, int? pointBorderWidth = null, bool? enableArea = null, double? areaOpacity = null, double? areaBaselineValue = null, bool? enableCrosshair = null, CrosshairType? crosshairType = null, LineCurve? curve = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, double? lineWidth = null, bool? isInteractive = null, bool? useMesh = null, bool? enableSlices = null, string? xFormat = null, string? yFormat = null, string? valueUnit = null, string? gradientFromColor = null, string? gradientToColor = null, IEnumerable<double>? gridXValues = null, IEnumerable<double>? gridYValues = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip values human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void PieChart(this UIView view, string[]? style = null, IEnumerable<PieChartDatum>? data = null, double? innerRadius = null, double? padAngle = null, double? cornerRadius = null, double? startAngle = null, double? endAngle = null, bool? sortByValue = null, ChartMargin? margin = null, bool? enableArcLabels = null, string? arcLabelsTextColor = null, double? arcLabelsSkipAngle = null, bool? enableArcLinkLabels = null, string? arcLinkLabelsTextColor = null, double? arcLinkLabelsSkipAngle = null, double? arcLinkLabelsThickness = null, string? arcLinkLabelsColor = null, double? activeOuterRadiusOffset = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? arcLabelAsPercentage = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
  record ChartGridStyle
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  record ChartLabelsStyle
    ctor()
    ChartTextStyle? Text { get; init; }
  record ChartLegendStyle
    ctor()
    ChartTextStyle? Text { get; init; }
    ChartTextStyle? Title { get; init; }
  sealed record ChartMargin
    ctor()
    int? Bottom { get; init; }
    int? Left { get; init; }
    int? Right { get; init; }
    int? Top { get; init; }
  record ChartTextStyle
    ctor()
    string? Color { get; init; }
    string? FontFamily { get; init; }
    int? FontSize { get; init; }
  record ChartTheme
    ctor()
    ChartAxisStyle? Axis { get; init; }
    ChartColorScheme? ColorScheme { get; init; }
    string[]? Colors { get; init; }
    ChartCrosshairStyle? Crosshair { get; init; }
    ChartGridStyle? Grid { get; init; }
    ChartLabelsStyle? Labels { get; init; }
    ChartLegendStyle? Legends { get; init; }
    ChartTextStyle? Text { get; init; }
    ChartTooltipStyle? Tooltip { get; init; }
  static class ChartThemes
    static ChartTheme DefaultDark { get; }
    static ChartTheme DefaultLight { get; }
  record ChartTooltipStyle
    ctor()
    string? BackgroundColor { get; init; }
    string? BorderColor { get; init; }
    int? BorderRadius { get; init; }
    ChartTextStyle? Text { get; init; }
  enum CrosshairType
    X
    Y
    TopLeft
    Top
    TopRight
    Right
    BottomRight
    Bottom
    BottomLeft
    Left
    Cross
  enum LegendAnchor
    Top
    TopRight
    Right
    BottomRight
    Bottom
    BottomLeft
    Left
    TopLeft
    Center
  sealed record LegendConfig
    ctor()
    LegendAnchor? Anchor { get; init; }
    LegendDirection? Direction { get; init; }
    int? ItemHeight { get; init; }
    int? ItemWidth { get; init; }
    int? ItemsSpacing { get; init; }
    int? SymbolSize { get; init; }
    int? TranslateX { get; init; }
    int? TranslateY { get; init; }
  enum LegendDirection
    Row
    Column
  sealed record LineChartPoint
    ctor()
    // Pass a string label for point scales, or a number for linear/time scales — the object type is genuinely mixed.
    required object X { get; init; }
    required double Y { get; init; }
  sealed record LineChartSeries
    ctor()
    string? Color { get; init; }
    IEnumerable<LineChartPoint>? Data { get; init; }
    required string Id { get; init; }
  enum LineCurve
    Linear
    MonotoneX
    Step
    StepBefore
    StepAfter
    Cardinal
    Basis
  sealed record PieChartDatum
    ctor()
    string? Color { get; init; }
    required string Id { get; init; }
    string? Label { get; init; }
    required double Value { get; init; }
  enum ScaleType
    Point
    Linear
    Time
    Log

namespace Ikon.Parallax.Components.DataTable
  record Cell
    ctor()
    string? ActionId { get; init; }
    CellAction[]? Actions { get; init; }
    bool? Disabled { get; init; }
    string? Label { get; init; }
    string[]? Style { get; init; }
    SemanticTone? Tone { get; init; }
    CellType Type { get; init; }
    string? Value { get; init; }
    static Cell Action(string label, string actionId, string[]? style = null)
    static Cell ActionGroup(CellAction[] actions)
    // style classes replace the themed tone token; lead the array with the "default" marker to merge the tone token underneath them instead.
    static Cell Badge(string value, SemanticTone? tone = null, string[]? style = null)
    static Cell Checkbox(bool value, string actionId, string[]? style = null, bool disabled = false)
    static Cell Text(string? value, string[]? style = null)
  record CellAction
    ctor(string Label, string ActionId, string[]? Style = null, string? Icon = null)
    string ActionId { get; init; }
    string? Icon { get; init; }
    string Label { get; init; }
    string[]? Style { get; init; }
  enum CellType
    Text
    Badge
    Action
    Actions
    Checkbox
  record DataTableColumn
    ctor(string Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left, string? MinWidth = null, bool Wrap = false)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string Header { get; init; }
    string? MinWidth { get; init; }
    string? Width { get; init; }
    bool Wrap { get; init; }
  static class DataTableExtensions
    static void DataTable(this UIView view, DataTableColumn[] columns, DataTableRow[] rows, int totalCount, int pageIndex, int pageSize, Func<int, Task>? onPageChange = null, Func<string, Task>? onRowClick = null, Func<string, Task>? onActionClick = null, Action<UIView>? emptyContent = null, int[]? columnWidths = null, Func<string, Task>? onColumnResize = null, string[]? style = null, DataTableStyles? styles = null, string? prevLabel = null, string? nextLabel = null, string? pageLabel = null, string? key = null)
  record DataTableRow
    ctor(string Id, Cell[] Cells)
    Cell[] Cells { get; init; }
    string Id { get; init; }
  // Each slot is a Crosswind class array that merges on top of the slot's themed default, exactly like a component's style: parameter; set only the slots you are changing.
  sealed record DataTableStyles
    ctor()
    string[]? ActionButton { get; init; }
    string[]? Cell { get; init; }
    string[]? DataCell { get; init; }
    string[]? Empty { get; init; }
    string[]? Header { get; init; }
    string[]? HeaderCell { get; init; }
    string[]? PageNumber { get; init; }
    string[]? PageNumberActive { get; init; }
    string[]? Pagination { get; init; }
    string[]? PaginationButton { get; init; }
    string[]? ResizeHandle { get; init; }
    string[]? Row { get; init; }
    string[]? Tooltip { get; init; }

namespace Ikon.Parallax.Components.ImageEditor
  static class ImageEditorExtensions
    // triggerSave/triggerUndo/triggerRedo are edge-triggered — increment the value to fire that action. highResolution keeps the canvas at native resolution (sharp zoom, full-quality export, but capped undo history); when false the canvas is downscaled to fit its container.
    static void ImageEditorCanvas(this UIView view, string[]? style = null, string? src = null, int? brushWidth = null, string? brushColor = null, ImageEditorTool? tool = null, double? zoom = null, bool? highResolution = null, bool? fitContainer = null, int? textMaxLength = null, int? textFontSize = null, int? textPadding = null, Func<ImageEditorSaveArgs, Task>? onSave = null, Func<ImageEditorHistoryArgs, Task>? onHistoryChange = null, int? triggerSave = null, int? triggerUndo = null, int? triggerRedo = null, string? styleId = null, string? key = null)
  sealed record ImageEditorHistoryArgs
    ctor(bool CanUndo, bool CanRedo)
    bool CanRedo { get; init; }
    bool CanUndo { get; init; }
  sealed record ImageEditorSaveArgs
    ctor(string ImageData)
    string ImageData { get; init; }
  enum ImageEditorTool
    Brush
    Eraser
    Text
    Arrow
    Region
    Lasso
    Line

namespace Ikon.Parallax.Components.Standard
  static class AccessibilityExtensions
    static void AccessibleIcon(this UIView view, string[]? style = null, string? label = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
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
    // Caller style replaces the tone's Theming.Alert token; lead the array with "default" to merge that token underneath it. The icon defaults per tone (success/warning/error/info).
    static void Alert(this UIView view, string title, SemanticTone tone = Neutral, string[]? style = null, string? description = null, string? icon = null, bool showIcon = true, Func<Task>? onDismiss = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? iconStyle = null, string[]? dismissStyle = null, Action<UIView>? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum Align
    Start
    Center
    End
  static class BadgeExtensions
    // With no style args it renders the themed Theming.Badge.* pill for the tone; caller styles replace the base token, or merge on top of it when the array leads with "default".
    static void Badge(this UIView view, string text, SemanticTone tone = Neutral, string[]? style = null, BadgeSize size = Md, bool outline = false, bool dot = false, string[]? dotStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum BadgeSize
    Sm
    Md
    Lg
  static class BreadcrumbExtensions
    // Items with an OnClick render as clickable links; the last item always renders as the non-clickable current page (aria-current="page") regardless of its OnClick.
    static void Breadcrumb(this UIView view, IReadOnlyList<BreadcrumbItem> items, string[]? style = null, string? separatorIcon = null, string[]? linkStyle = null, string[]? itemStyle = null, string[]? pageStyle = null, string[]? separatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record BreadcrumbItem
    ctor(string Label, Func<Task>? OnClick = null)
    string Label { get; init; }
    Func<Task>? OnClick { get; init; }
  static class CalendarExtensions
    // All date values (value, defaultValue, minDate, maxDate, callbacks) are ISO yyyy-MM-dd strings; month is yyyy-MM. Controlled via value+onValueChange; omit both and pass defaultValue for uncontrolled.
    static void Calendar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? month = null, string? defaultMonth = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, string? locale = null, bool? disabled = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onMonthChange = null)
    static void DatePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, string? format = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? calendarStyle = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  enum CameraFacing
    User
    Environment
  sealed record CaptureImageActionOptions : ActionOptions
    ctor()
    CaptureImageConstraints? Constraints { get; init; }
    ClientImageCaptureFormat? Format { get; init; }
    int? Height { get; init; }
    CaptureImageMode? Mode { get; init; }
    double? Quality { get; init; }
    int? Width { get; init; }
  sealed record CaptureImageConstraints
    ctor()
    string? DeviceId { get; init; }
    CameraFacing? FacingMode { get; init; }
  enum CaptureImageMode
    Native
    Headless
  static class CardExtensions
    // With no style args it renders the themed card token (Theming.Card.Default, or Theming.Card.Interactive when onClick is set); caller styles replace it, or merge on top of it when the array leads with "default".
    static void Card(this UIView view, string[]? style = null, string? title = null, string? description = null, Action<UIView>? header = null, Action<UIView>? content = null, Action<UIView>? footer = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? contentStyle = null, string[]? footerStyle = null, Delegate? onClick = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void Card(this UIView view, string[]? style, Action<UIView> children)
    static void EmptyState(this UIView view, string title, string[]? style = null, string? description = null, string? icon = null, Action<UIView>? action = null, string[]? iconWrapStyle = null, string[]? iconStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? actionsStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void StatCard(this UIView view, string label, string value, string[]? style = null, string? delta = null, StatTrend trend = Flat, string? trendLabel = null, string? icon = null, SemanticTone iconTone = Neutral, string[]? labelStyle = null, string[]? valueStyle = null, string[]? trendStyle = null, string[]? iconBoxStyle = null, string[]? iconStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum CarouselAlign
    Start
    Center
    End
  sealed record CarouselBreakpoint
    ctor(int MinWidth, int SlidesPerView, int? SlidesPerGroup = null, int? SlideGapPx = null)
    int MinWidth { get; init; }
    int? SlideGapPx { get; init; }
    int? SlidesPerGroup { get; init; }
    int SlidesPerView { get; init; }
  static class CarouselExtensions
    // Provide slides via slides for the simple case, or via the content builder using Slide for fully custom children.
    static void Carousel(this UIView view, string[]? style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, int? slidesPerView = null, int? slidesPerGroup = null, int? slideGapPx = null, IEnumerable<CarouselBreakpoint>? breakpoints = null, IEnumerable<CarouselSlideItem>? slides = null, bool? showArrows = null, bool? showIndicators = null, string? previousLabel = null, string? nextLabel = null, string? previousIconName = null, string? nextIconName = null, string[]? rootStyle = null, string[]? viewportStyle = null, string[]? slideStyle = null, string[]? previousStyle = null, string[]? nextStyle = null, string[]? indicatorsStyle = null, string[]? indicatorStyle = null, string[]? indicatorActiveStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<int, Task>? onIndexChange = null)
    static void Slide(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  sealed record CarouselSlideItem
    ctor(Action<UIView> Content, string? Key = null)
    Action<UIView> Content { get; init; }
    string? Key { get; init; }
  static class ChatLogExtensions
    // Use instead of a manual Column(overflow-auto) for any "newest at the bottom, follow when content grows" layout. autoScrollKey tells the framework when to re-anchor to the bottom — pass the reactive message collection, a count, or any other value that changes when the content does.
    static void ChatLog(this UIView view, string[]? style = null, object? autoScrollKey = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, string? styleId = null, string? key = null)
  enum CheckedState
    Unchecked
    Checked
    Indeterminate
  static class CodeEditorExtensions
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
    static void ColorPicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, ColorFormat format = Hex, bool? showAlpha = null, IReadOnlyList<string>? presets = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onValueCommit = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  enum ColumnAlign
    Left
    Center
    Right
  sealed record ContactsActionEvent : ActionEvent
    ctor(bool Success, IReadOnlyList<ClientContact>? Contacts)
    IReadOnlyList<ClientContact>? Contacts { get; init; }
  static class ContainerExtensions
    static void Box(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Box(this UIView view, string[]? style, Action<UIView> children)
    static void Column(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Column(this UIView view, string[]? style, Action<UIView> children)
    static void Flex(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Flex(this UIView view, string[]? style, Action<UIView> children)
    static void Grid(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Grid(this UIView view, string[]? style, Action<UIView> children)
    static void Layer(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Layer(this UIView view, string[]? style, Action<UIView> children)
    static void Row(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Row(this UIView view, string[]? style, Action<UIView> children)
    static void Spinner(this UIView view, string[]? style = null, SpinnerSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void Stack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void Stack(this UIView view, string[]? style, Action<UIView> children)
  record ContentGridColumn
    ctor(string? Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string? Header { get; init; }
    string? Width { get; init; }
  static class ContentGridExtensions
    static void ContentGrid(this UIView view, ContentGridColumn[] columns, Action<UIView>? content = null, string[]? style = null, string[]? headerStyle = null, string? key = null)
  sealed record CopyToClipboardActionOptions : ActionOptions
    ctor()
    required string Text { get; init; }
  static class CoreExtensions
    static void ActionButton(this UIView view, string[]? style = null, ActionKind action = Unknown, string? text = null, ActionOptions? options = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<ActionEvent, Task>? onActionComplete = null, Action<UIView>? content = null)
    static void Button(this UIView view, string[]? style = null, string? text = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null)
    static void Button(this UIView view, string buttonText, string[]? style = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null)
    static void Heading(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Heading(this UIView view, string headingText, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Icon(this UIView view, string[]? style = null, string? name = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Icon(this UIView view, string iconName, string[]? style = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Link(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Link(this UIView view, string linkText, string[]? style = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    static void Markdown(this UIView view, string[]? style = null, string? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void Markdown(this UIView view, string markdownContent, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void Text(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Text(this UIView view, string textContent, string[]? style = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Toggle(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null)
    static void ToggleGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void ToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    static void ToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
  enum Dir
    Ltr
    Rtl
  static class DisclosureExtensions
    static void AccordionContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void AccordionHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void AccordionItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void AccordionMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    static void AccordionSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? collapsible = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    static void AccordionTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Collapsible(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    static void CollapsibleContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void CollapsibleTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  sealed record DownloadFileActionOptions : ActionOptions
    ctor()
    byte[]? Data { get; init; }
    string? Filename { get; init; }
    string? MimeType { get; init; }
    string Url { get; init; }
  static class DragAndDropExtensions
    static void DndContext(this UIView view, string[]? style = null, CollisionDetection collisionDetection = ClosestCenter, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<DragStartArgs, Task>? onDragStart = null, Func<DragMoveArgs, Task>? onDragMove = null, Func<DragOverArgs, Task>? onDragOver = null, Func<DragEndArgs, Task>? onDragEnd = null, Func<Task>? onDragCancel = null, int? activationDistance = null, Action<UIView>? content = null)
    static void DragOverlay(this UIView view, string[]? style = null, bool? dropAnimation = true, string? activeDragId = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Draggable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, bool? hideOnDrag = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Droppable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void SortableContext(this UIView view, string[]? style = null, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void SortableHandle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void SortableItem(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Style slots: listStyle (container holding all sortable items), itemStyle (each item).
    static void SortableList(this UIView view, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, CollisionDetection collisionDetection = ClosestCenter, Func<SortableReorderArgs, Task>? onReorder = null, Func<DragStartArgs, Task>? onDragStart = null, Action<UIView, string>? itemContent = null, string[]? listStyle = null, string[]? itemStyle = null, int? activationDistance = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record DragCancelArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  sealed record DragEndArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  sealed record DragMoveArgs
    ctor(string ActiveId, double DeltaX, double DeltaY)
    string ActiveId { get; init; }
    double DeltaX { get; init; }
    double DeltaY { get; init; }
  sealed record DragOverArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  sealed record DragStartArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  sealed record EscapeKeyDownArgs
    ctor()
  // Backed by a ClientReactive<T>: each client expands and collapses independently, and reads during UI rendering are dependency-tracked, so the tree re-renders automatically. Access it where a client scope is active (UI render or event handlers).
  sealed class ExpandedSet
    ctor(params string[] expandedIds)
    void Clear()
    void Collapse(string id)
    void Expand(string id)
    bool IsExpanded(string id)
    void Set(string id, bool expanded)
    void Toggle(string id)
  enum FeedMediaKind
    None
    Image
    Video
    VideoFull
  // Performance model: • Native CSS scroll-snap drives the snap — no JS scroll loop. • Active slide is detected with IntersectionObserver, not scroll events. • Only slides inside [active - preloadBehind, active + preloadAhead] render their content; slides outside the window render as fixed-height spacers that preserve scroll position. • Media declared on FeedSlide is warmed with off-DOM Image/<video> elements as soon as a slide enters the preload window. • Autoplay is gated on the active slide only — neighbour videos are paused.
  static class FeedScrollerExtensions
    static void FeedScroller(this UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[]? style = null, string[]? slideStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onActiveChange = null, Func<double, Task>? onScrollNearEnd = null, Func<bool, Task>? onMuteChange = null)
    static void FeedSlide(this UIView view, int index, string[]? style = null, FeedMediaKind mediaKind = None, string? mediaUrl = null, string? mediaPoster = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  sealed record FeedSlide
    ctor(Action<UIView> Content, string? Key = null, FeedMediaKind MediaKind = None, string? MediaUrl = null, string? MediaPoster = null)
    Action<UIView> Content { get; init; }
    string? Key { get; init; }
    FeedMediaKind MediaKind { get; init; }
    string? MediaPoster { get; init; }
    string? MediaUrl { get; init; }
  static class FilePickerExtensions
    // Only reports picked-file metadata to the server — the bytes stay on the client and are not uploaded until a FileUploadExtensions.FileUpload with a matching seedSelectionIds prop is mounted. Without an onValidationError handler, client-side rejections (e.g. over maxFileSize) are silent.
    static void FilePicker(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FilePickerSelectedArgs, Task>? onFileSelected = null, Func<FilePickerValidationErrorArgs, Task>? onValidationError = null, Action<UIView>? content = null)
  sealed record FilePickerSelectedArgs
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  sealed record FilePickerValidationErrorArgs
    ctor(string FileName, string MimeType, long Size, string Reason)
    string FileName { get; init; }
    string MimeType { get; init; }
    string Reason { get; init; }
    long Size { get; init; }
  static class FileUploadExtensions
    static void FileUpload(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string? capture = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, string[]? seedSelectionIds = null, Action<UIView>? content = null)
    // Style slots: zoneStyle (drop-zone container), activeStyle (applied while a file is dragged over the zone). The MIME filter is the NAMED accept: parameter — a leading positional array is always the zone style, never the filter.
    static void FileUploadZone(this UIView view, string[]? style = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string[]? accept = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, Func<bool, Task>? onDragActiveChange = null, Action<UIView>? content = null, string[]? zoneStyle = null, string[]? activeStyle = null, string? activeStyleId = null, string? styleId = null, string? key = null, string[]? seedSelectionIds = null, IReadOnlyDictionary<string, object>? props = null)
  static class FocusHintExtensions
    static void FocusHint(this UIView view, FocusHintProps props, string? key = null, Guid? targetViewId = null)
  sealed record FocusHintProps
    ctor()
    TimeSpan? Cooldown { get; init; }
    bool FocusOnly { get; init; }
    FocusPriority Priority { get; init; }
    int Ranking { get; init; }
  sealed record FocusOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  enum FocusPriority
    Polite
    Assertive
  static class FormExtensions
    static void Checkbox(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    static void CheckboxIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Form(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClearServerErrors = null, Action<UIView>? content = null)
    static void FormControl(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void FormField(this UIView view, string[]? style = null, string? name = null, bool? serverInvalid = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void FormLabel(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void FormMessage(this UIView view, string[]? style = null, FormMessageMatch? match = null, bool? forceMatch = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void FormSubmit(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Label(this UIView view, string[]? style = null, string? htmlFor = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void RadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<string>? bind = null)
    static void RadioGroupIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void RadioGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, bool? required = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Slider(this UIView view, string[]? style = null, IReadOnlyList<double>? value = null, IReadOnlyList<double>? defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<double>, Task>? onValueChange = null, Func<IReadOnlyList<double>, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null, Reactive<double>? bind = null, string? ariaLabel = null)
    static void Slider(this UIView view, double value, string[]? style = null, double? min = null, double? max = null, double? step = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onValueChange = null, Func<double, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null)
    static void SliderRange(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void SliderThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void SliderTrack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Switch(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    static void SwitchThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void TriStateCheckbox(this UIView view, string[]? style = null, CheckedState? value = null, CheckedState? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onValueChange = null, Action<UIView>? content = null, string? ariaLabel = null)
  enum FormMessageMatch
    ValueMissing
    TypeMismatch
    TooShort
    TooLong
    PatternMismatch
    RangeUnderflow
    RangeOverflow
    StepMismatch
    BadInput
    CustomError
  enum HourFormat
    Hour24
    Hour12
  enum IconSize
    Xs
    Sm
    Md
    Lg
    Xl
  sealed record ImageCaptureActionEvent : ActionEvent
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string? Data { get; init; }
    int Height { get; init; }
    string? Mime { get; init; }
    int Width { get; init; }
  static class ImageExtensions
    static void Avatar(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void AvatarFallback(this UIView view, string[]? style = null, int? delayMs = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void AvatarImage(this UIView view, string[]? style = null, string? src = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onLoadingStatusChange = null)
    static void Image(this UIView view, string[]? style = null, string? src = null, AssetUri? assetUri = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
    static void Image(this UIView view, string[]? style = null, byte[]? data = null, string? mimeType = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
  static class InputExtensions
    static void OtpField(this UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string? label = null)
    static void OtpFieldInput(this UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void PasswordToggleField(this UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string? label = null)
    static void PasswordToggleFieldIcon(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null)
    static void PasswordToggleFieldInput(this UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void PasswordToggleFieldToggle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void TextArea(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, string? ariaLabel = null)
    // Controlled/read-only rule (shared by every input component — text, select, checkbox, calendar, color, OTP, …): passing a controlled value: with no write-back handler (bind:, onValueChange:, or onSubmit:) renders the field read-only, since edits would have nowhere to go. Pass bind: <reactive> to two-way bind a Reactive<T> in one call, or value: together with an onValueChange:/onSubmit: handler.
    static void TextField(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, bool? multiline = null, int? rows = null, string? ariaLabel = null)
  sealed record InteractOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  static class Key
    const string Alt
    const string ArrowDown
    const string ArrowLeft
    const string ArrowRight
    const string ArrowUp
    const string Backspace
    const string Control
    const string Delete
    const string End
    const string Enter
    const string Escape
    const string F1
    const string F10
    const string F11
    const string F12
    const string F2
    const string F3
    const string F4
    const string F5
    const string F6
    const string F7
    const string F8
    const string F9
    const string Home
    const string Meta
    const string PageDown
    const string PageUp
    const string Shift
    const string Space
    const string Tab
  sealed record KeyboardEventArgs
    ctor(string Key, string Code, bool AltKey, bool CtrlKey, bool MetaKey, bool ShiftKey, bool Repeat)
    bool AltKey { get; init; }
    string Code { get; init; }
    bool CtrlKey { get; init; }
    string Key { get; init; }
    bool MetaKey { get; init; }
    bool Repeat { get; init; }
    bool ShiftKey { get; init; }
  static class KeyboardExtensions
    static void KeyboardListener(this UIView view, Func<KeyboardEventArgs, Task>? onKeyDown = null, Func<KeyboardEventArgs, Task>? onKeyUp = null, IReadOnlyList<string>? keys = null, bool? global = true, bool? requireCtrlOrMeta = null, bool? preventDefault = null, bool? stopPropagation = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  static class LayoutExtensions
    static void AspectRatio(this UIView view, string[]? style = null, double ratio = 1.0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void DirectionProvider(this UIView view, string[]? style = null, Dir dir = Ltr, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb; rootStyle rarely needed.
    static void InfiniteScrollView(this UIView view, string[]? style = null, int threshold = 200, int debounceMs = 100, bool loading = false, bool hasMore = true, ScrollDirection direction = Down, ScrollAreaScrollbars scrollbars = Vertical, Action<UIView>? loadingIndicator = null, Func<ScrollNearEndArgs, Task>? onNearEnd = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // SemanticTone.Success, SemanticTone.Warning, and SemanticTone.Error tones map to the matching Theming.Progress.Variant tokens; other tones use the default (brand) fill. Style slots: rootStyle → Progress.Root, indicatorStyle → Progress.Indicator.
    static void Progress(this UIView view, string[]? style = null, double? value = null, double? max = null, SemanticTone tone = Neutral, bool indeterminate = false, Func<double?, string>? getValueLabel = null, string[]? rootStyle = null, string[]? indicatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void ResizableSplit(this UIView view, Orientation orientation = Horizontal, double initialSize = 200.0, double minSize = 100.0, double maxSize = 500.0, bool reversed = false, Func<double, Task>? onResized = null, Action<UIView>? first = null, Action<UIView>? second = null, string[]? style = null, string[]? handleStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb, cornerStyle (when both scrollbars show); rootStyle rarely needed.
    static void ScrollArea(this UIView view, string[]? style = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType type = Hover, int? scrollHideDelay = null, Dir dir = Ltr, bool autoScroll = false, object? autoScrollKey = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? cornerStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void ScrollArea(this UIView view, string[]? style, Action<UIView> children)
    static void Separator(this UIView view, string[]? style = null, Orientation orientation = Horizontal, bool decorative = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record LocationActionEvent : ActionEvent
    ctor(bool Success, double? Latitude, double? Longitude, double? Accuracy)
    double? Accuracy { get; init; }
    double? Latitude { get; init; }
    double? Longitude { get; init; }
  enum MediaCaptureButtonMode
    Hold
    Toggle
  sealed record MediaCaptureEvent
    ctor(string StreamId, MediaCaptureKind Kind)
    Context? ClientContext { get; init; }
    int? ClientSessionId { get; }
    MediaCaptureKind Kind { get; init; }
    string StreamId { get; init; }
    string? UserId { get; }
  enum MediaCaptureKind
    Audio
    Camera
    Screen
  static class MediaExtensions
    static void AudioUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, string? preload = null, string? styleId = null, string? key = null)
    static void AudioWave(this UIView view, string[]? style = null, int bars = 7, string[]? barStyle = null, string? key = null)
    static void CaptureButton(this UIView view, string[]? style = null, MediaCaptureKind kind = Audio, string? text = null, MediaCaptureButtonMode captureMode = Hold, ClientAudioCaptureOptions? audioOptions = null, ClientVideoCaptureOptions? videoOptions = null, int? holdReleaseDelayMs = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    static void MicToggleButton(this UIView view, string[]? style = null, string? text = "🎤", ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Enable speech recognition once via Audio.UseSpeechRecognition(...), then subscribe to Audio.SpeechRecognizedAsync to receive transcriptions when the button is released; the initiating user's client context is carried on the event args.
    static void PushToTalkButton(this UIView view, string[]? style = null, string? text = "⏺", int holdReleaseDelayMs = 500, ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    static void VideoStreamCanvas(this UIView view, string[]? style = null, string? streamId = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
    static void VideoUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, bool? playsInline = null, string? poster = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  static class NavigationExtensions
    static void Menubar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    static void MenubarCheckboxItem(this UIView view, string[]? style = null, CheckedState isChecked = Unchecked, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onCheckedChange = null, Action<UIView>? content = null)
    static void MenubarContent(this UIView view, string[]? style = null, bool loop = true, Side side = Bottom, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void MenubarItem(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    static void MenubarItemIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void MenubarMenu(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void MenubarRadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    static void MenubarRadioItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void MenubarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void MenubarSub(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    static void MenubarSubContent(this UIView view, string[]? style = null, bool loop = true, Side side = Right, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void MenubarSubTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void MenubarTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void NavigationMenu(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, int? delayDuration = null, int? skipDelayDuration = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    static void NavigationMenuContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void NavigationMenuIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void NavigationMenuItem(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void NavigationMenuLink(this UIView view, string[]? style = null, bool? active = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    static void NavigationMenuList(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void NavigationMenuTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void NavigationMenuViewport(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Toolbar(this UIView view, string[]? style = null, Orientation orientation = Horizontal, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void ToolbarButton(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null)
    static void ToolbarLink(this UIView view, string[]? style = null, string? href = null, string? target = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void ToolbarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void ToolbarToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    static void ToolbarToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    static void ToolbarToggleItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  enum Orientation
    Horizontal
    Vertical
  static class OverlayExtensions
    // Style slots: overlayStyle → AlertDialog.Overlay, contentStyle → AlertDialog.Content, titleStyle → AlertDialog.Title, descriptionStyle → AlertDialog.Description, footerStyle → AlertDialog.Footer, cancelStyle → AlertDialog.Cancel, actionStyle → AlertDialog.Action.
    static void AlertDialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? title = null, string? description = null, string? cancelLabel = null, string? actionLabel = null, Func<Task>? onAction = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? cancelStyle = null, string[]? actionStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Style slots: overlayStyle → Dialog.Overlay, contentStyle → Dialog.Content.
    static void Dialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? headerStyle = null)
    // Style slots: contentStyle → HoverCard.Content.
    static void HoverCard(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: contentStyle → Popover.Content.
    static void Popover(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: toastStyle → Toast.Root, viewportStyle → Toast.Viewport, titleStyle → Toast.Title, descriptionStyle → Toast.Description, closeStyle → Toast.Close.
    static void Toast(this UIView view, string[]? style = null, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string? title = null, string? description = null, bool? showClose = null, string? closeLabel = null, Action<UIView>? content = null, string[]? toastStyle = null, string[]? viewportStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Func<Task>? onEscapeKeyDown = null, Func<Task>? onPause = null, Func<Task>? onResume = null, Func<ToastSwipeArgs, Task>? onSwipeStart = null, Func<ToastSwipeArgs, Task>? onSwipeMove = null, Func<ToastSwipeArgs, Task>? onSwipeEnd = null, Func<ToastSwipeArgs, Task>? onSwipeCancel = null)
    // Style slots: contentStyle → Tooltip.Content.
    static void Tooltip(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
  static class OverlayMenuExtensions
    // Filtering is server-side: bind searchValue to a reactive and echo edits via onSearchChange for the list to narrow by case-insensitive label match. Without a bound search value it renders as a plain Popover-select (no filtering).
    static void Combobox(this UIView view, IReadOnlyList<SelectOption> options, string? value = null, Func<string, Task>? onValueChange = null, string? searchValue = null, Func<string, Task>? onSearchChange = null, bool? open = null, Func<bool, Task>? onOpenChange = null, string? placeholder = "Select…", string? searchPlaceholder = "Search…", string? emptyText = "No results.", string[]? style = null, string[]? triggerStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Filtering is server-side over searchValue: each group narrows by case-insensitive label match and empty groups drop out. onSelect fires with the chosen option's value.
    static void CommandPalette(this UIView view, IReadOnlyList<SelectOptionGroup> groups, bool? open = null, Func<bool, Task>? onOpenChange = null, Func<string, Task>? onSelect = null, string? searchValue = null, Func<string, Task>? onSearchChange = null, string? placeholder = "Type a command or search…", string? emptyText = "No results.", string[]? panelStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Fill content with view.Button([Menu.Item]) / [Menu.ItemDestructive] rows plus Menu.Label / Menu.Separator; the component supplies the trigger wiring and the menu-shaped popover panel.
    static void DropdownMenu(this UIView view, Action<UIView> trigger, Action<UIView> content, bool? open = null, Side side = Bottom, Align align = Start, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Pass text for a single key, or keys for a combo (one chip per key); keys wins over text.
    static void Kbd(this UIView view, string? text = null, IReadOnlyList<string>? keys = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  sealed record Page<T>
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
  static class PaginationExtensions
    // page must be a field-level ClientReactive<T>; each client sees its own page, and the returned slice is a snapshot read once, not a live view.
    static Page<T> Paginate<T>(this UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  sealed record PickContactsActionOptions : ActionOptions
    ctor()
    bool Multiple { get; init; }
  sealed record PointerDownOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  static class QrCodeExtensions
    static void QR(this UIView view, string[]? style = null, string? value = null, int size = 256, string? key = null)
  static class RichTextEditorExtensions
    // Values are HTML strings. A controlled value with no write-back handler (onValueChange or onSubmit) renders the editor read-only.
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
  static class RoutingExtensions
    static void Routed<T>(this UIView view, ClientReactive<T> signal, Dictionary<T, Action<UIView>> cases, Action<UIView>? fallback = null)
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
    // Canonical shape for dialogs, side panels, and chat layouts needing fixed chrome around an overflowing region; avoids the flex-1 ScrollArea that won't shrink inside a flex parent (the min-height: auto quirk). The outer height is the caller's responsibility — set it via style (e.g. "h-[82vh]") or let a flex-1 parent provide bounds.
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
    // An Input.* token passed as the Select's own style is ignored (with a dev warning) — it would style the outer wrapper, not the field element; the trigger already carries the field theme, so customize it through triggerStyle. Trigger sizing uses Select.Size tokens ([Select.Size.Sm] / [Select.Size.Lg], default medium) in triggerStyle.
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
    // Renders GLSL fragment shaders with Shadertoy-compatible uniforms. The shader code must define a mainImage function with signature: void mainImage(out vec4 color, in vec2 fragCoord) Built-in uniforms (automatically provided): • iResolution (vec3) - canvas width, height, and 1.0 • iTime (float) - elapsed time in seconds • iTimeDelta (float) - time since last frame • iFrame (int) - current frame number • iMouse (vec4) - mouse x, y, click x, click y (requires enableMouse=true) • iDate (vec4) - year, month, day, seconds of day Texture channels: Pass image URLs (data URIs or http(s)) via channels to bind them to the Shadertoy channel uniforms, matching Shadertoy's default sampler behavior so shaders copied from shadertoy.com that sample 2D textures render the same way: • iChannel0..iChannel3 (sampler2D) - channel textures, in array order • iChannelResolution[4] (vec3) - per-channel pixel size (0 until loaded) • iChannelTime[4] (float) - always 0 for static images Textures use Shadertoy's defaults: vertical flip on (upright with uv = fragCoord/iResolution), repeat wrap, and mipmap filtering. Sample with texture(iChannel0, uv). Limitations: 2D image channels only - no cubemap (samplerCube), buffer, audio, or video channels; single output only.
    static void ShadertoyCanvas(this UIView view, string[]? style = null, string? shaderSource = null, int? fps = null, IReadOnlyDictionary<string, ShaderUniform>? uniforms = null, IReadOnlyList<string>? channels = null, bool? enableMouse = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  sealed record ShareActionOptions : ActionOptions
    ctor()
    string? Text { get; init; }
    string? Title { get; init; }
    string? Url { get; init; }
  static class SheetExtensions
    // Same open/close model as Sheet: in controlled mode (open set) pass onOpenChange and flip your state to false there, or the drawer cannot be dismissed.
    static void Drawer(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showHandle = true, string[]? style = null, string[]? overlayStyle = null, string[]? handleStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string? key = null)
    // In controlled mode (open set) pass onOpenChange and flip your state to false there, or the close button and outside clicks cannot dismiss the sheet. Caller styles replace the themed panel token, or merge over it with a leading "default" marker.
    static void Sheet(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, Side side = Right, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showClose = true, string[]? style = null, string[]? overlayStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? closeStyle = null, string? key = null)
  enum Side
    Top
    Right
    Bottom
    Left
  static class SkeletonExtensions
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
    Flat
    Up
    Down
  enum Sticky
    Partial
    Always
  record TabItem
    ctor(string Value, string Label, Action<UIView> Content, bool Disabled = false, bool ForceMount = false)
    Action<UIView> Content { get; init; }
    bool Disabled { get; init; }
    bool ForceMount { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  static class TableExtensions
    static void Table(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void Table(this UIView view, string[]? style, Action<UIView> children)
    static void TableBody(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void TableCell(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void TableCell(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    static void TableHead(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void TableHead(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    static void TableHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    static void TableRow(this UIView view, string[]? style = null, bool striped = false, Delegate? onClick = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
  static class TabsExtensions
    // Style slots (default theme tokens): listStyle → Tabs.List, triggerStyle → Tabs.Trigger, contentStyle → Tabs.Content; rootStyle is the outer container (rarely needed).
    static void Tabs(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, ActivationMode activationMode = Automatic, IEnumerable<TabItem>? tabs = null, string[]? listContainerStyle = null, string[]? listStyle = null, string[]? triggerStyle = null, string[]? disabledTriggerStyle = null, string[]? contentContainerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, bool lazyPanels = false)
  enum TimeGranularity
    Hour
    Minute
    Second
  static class TimePickerExtensions
    // Values are ISO-8601 HH:mm or HH:mm:ss strings; the emitted value is always 24-hour regardless of hourFormat. A controlled value without onValueChange renders read-only.
    static void TimePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, HourFormat hourFormat = Hour24, TimeGranularity granularity = Minute, int? minuteStep = null, int? secondStep = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string? placeholder = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? columnStyle = null, string[]? itemStyle = null, string[]? itemSelectedStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  sealed record ToastItem
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
    long Show(string title, string? description = null, SemanticTone tone = Neutral, int durationMs = 5000)
    long Success(string title, string? description = null, int durationMs = 5000)
    long Warning(string title, string? description = null, int durationMs = 5000)
    const int DefaultDurationMs = 5000
  static class ToastsExtensions
    static void ToastHost(this UIView view, Toasts toasts, string[]? viewportStyle = null, string[]? toastStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, bool showClose = true)
  static class TreeViewExtensions
    // Expansion state lives in a caller-held ExpandedSet — declare it as an app field (private readonly ExpandedSet _expanded = new();). Clicking a branch toggles its expansion and selects it in the same click.
    static void TreeView<T>(this UIView view, IReadOnlyList<T> roots, Func<T, string> id, Func<T, string> label, Func<T, IReadOnlyList<T>?> children, ExpandedSet expanded, string[]? style = null, Func<T, Task>? onSelect = null, string? selectedId = null, Func<T, string?>? icon = null, string[]? itemStyle = null, string[]? selectedItemStyle = null, string[]? labelStyle = null, string[]? childrenStyle = null, string? styleId = null, string? key = null)
  // Performance model: the server emits one wrapper node per item up to itemCount and runs every per-item content builder eagerly server-side (keep content trees inexpensive); the client mounts only the wrappers inside [start - overscan, end + overscan] and leaves the rest out of the DOM entirely. onNearEnd fires when the window enters the last nearEndThreshold rows — append items to grow the list.
  static class VirtualListExtensions
    static void VirtualGrid(this UIView view, int itemCount, int columns, double rowHeight, Action<UIView, int> onRenderItem, int overscan = 2, int gap = 12, int? minItemWidthPx = null, int? maxColumns = null, double? aspectRatio = null, string? resetScrollKey = null, Func<int, Task>? onNearEnd = null, int nearEndThresholdRows = 2, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void VirtualList(this UIView view, int itemCount, double itemHeight, Action<UIView, int> onRenderItem, int overscan = 4, Func<int, Task>? onNearEnd = null, int nearEndThreshold = 5, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  enum WeekStart
    Sunday
    Monday

namespace Ikon.Parallax.Theming
  static class Accessibility
    static string RequiredLabel(string baseLabel)
    const string NotScreenReaderOnly
    const string ScreenReaderOnly
    const string SkipLink
  static class Accessibility.Aria
    const string Busy
    const string Checked
    const string CurrentPage
    const string CurrentStep
    const string Disabled
    const string Expanded
    const string Invalid
    const string Required
    const string Selected
  static class Accessibility.Focus
    const string HighContrast
    const string None
    const string Sentinel
    const string Within
  static class Accessibility.Motion
    const string Reduce
    const string ReduceFade
    const string Respectful
    const string Safe
  static class Accordion
    const string ChevronIcon
    const string Content
    const string ContentInner
    const string Default
    const string Header
    const string Item
    const string Root
    const string Trigger
  static class Alert
    const string Base
    const string Default
    const string Description
    const string Error
    const string Info
    const string Success
    const string Title
    const string Warning
  static class Alert.Variant
    const string Default
    const string Error
    const string Info
    const string Success
    const string Warning
  static class AlertDialog
    const string Action
    const string Cancel
    const string Content
    const string Default
    const string Description
    const string Footer
    const string Header
    const string Overlay
    const string Title
  static class AspectRatio
    const string Base
    const string Default
    const string PlaceholderContent
  static class AspectRatio.Ratio
    const string Photo
    const string Portrait
    const string Square
    const string Video
    const string Wide
  static class AudioWave
    const string Bar
    static readonly int[] Heights
    const string Root
  static class Avatar
    const string Base
    const string Default
    const string Fallback
    const string Image
    const string Root
  static class Avatar.Shape
    const string Circle
    const string Square
  static class Avatar.Size
    const string Lg
    const string Md
    const string Sm
    const string Xl
    const string Xl2
    const string Xs
  static class Badge
    const string Base
    const string Brand
    const string BrandLg
    const string BrandMd
    const string BrandSm
    const string Default
    const string DefaultLg
    const string DefaultMd
    const string DefaultSm
    const string Error
    const string ErrorLg
    const string ErrorMd
    const string ErrorSm
    const string IconLeft
    const string IconRight
    const string Info
    const string InfoLg
    const string InfoMd
    const string InfoSm
    const string Neutral
    const string NeutralLg
    const string NeutralMd
    const string NeutralSm
    const string OutlineBrand
    const string OutlineBrandLg
    const string OutlineBrandMd
    const string OutlineBrandSm
    const string OutlineError
    const string OutlineErrorLg
    const string OutlineErrorMd
    const string OutlineErrorSm
    const string OutlineInfo
    const string OutlineInfoLg
    const string OutlineInfoMd
    const string OutlineInfoSm
    const string OutlineNeutral
    const string OutlineNeutralLg
    const string OutlineNeutralMd
    const string OutlineNeutralSm
    const string OutlineSuccess
    const string OutlineSuccessLg
    const string OutlineSuccessMd
    const string OutlineSuccessSm
    const string OutlineWarning
    const string OutlineWarningLg
    const string OutlineWarningMd
    const string OutlineWarningSm
    const string Success
    const string SuccessLg
    const string SuccessMd
    const string SuccessSm
    const string Warning
    const string WarningLg
    const string WarningMd
    const string WarningSm
  static class Breadcrumb
    const string Ellipsis
    const string Item
    const string Link
    const string List
    const string Page
    const string Root
    const string Separator
  static class Button
    const string Base
    const string Default
    const string DefaultLg
    const string DefaultMd
    const string DefaultSm
    const string Error
    const string ErrorLg
    const string ErrorMd
    const string ErrorSm
    const string Ghost
    const string GhostLg
    const string GhostMd
    const string GhostSm
    const string Icon
    const string IconLeft
    const string IconRight
    const string IconSm
    const string IconXs
    const string Info
    const string InfoLg
    const string InfoMd
    const string InfoSm
    const string Link
    const string LinkLg
    const string LinkMd
    const string LinkSm
    const string Neutral
    const string NeutralLg
    const string NeutralMd
    const string NeutralSm
    const string Outline
    const string OutlineLg
    const string OutlineMd
    const string OutlineSm
    const string Primary
    const string PrimaryLg
    const string PrimaryMd
    const string PrimarySm
    const string Secondary
    const string SecondaryLg
    const string SecondaryMd
    const string SecondarySm
    const string SolidLg
    const string SolidMd
    const string SolidSm
    const string Success
    const string SuccessLg
    const string SuccessMd
    const string SuccessSm
    const string Warning
    const string WarningLg
    const string WarningMd
    const string WarningSm
  static class Button.Size
    const string Lg
    const string Md
    const string Sm
  static class Calendar
    const string Day
    const string DayDisabled
    const string DayOutside
    const string DaySelected
    const string DayToday
    const string Default
    const string Grid
    const string Header
    const string HeaderTitle
    const string NavButton
    const string Root
    const string Row
    const string Weekday
  static class Card
    const string Base
    const string Content
    const string Default
    const string Description
    const string Elevated
    const string Flat
    const string Footer
    const string Ghost
    const string Glass
    const string GlassSubtle
    const string Header
    const string HeaderRow
    const string Interactive
    const string InteractiveFill
    const string Outline
    const string Selected
    const string Strong
    const string Subtle
    const string Title
  static class Carousel
    const string Default
    const string Indicator
    const string IndicatorActive
    const string Indicators
    const string NavButton
    const string Next
    const string Previous
    const string Root
    const string Slide
    const string Track
    const string TrackVertical
    const string Viewport
  static class Chart
    const string Container
    const string ContainerLg
    const string ContainerMd
    const string ContainerSm
    const string ContainerXl
    const string Default
  static class Checkbox
    const string Default
    const string Indicator
    const string Root
  static class CodeEditor
    const string Body
    const string Content
    const string Default
    const string Gutter
    const string Header
    const string LanguageBadge
    const string Line
    const string Root
  static class Collapsible
    const string Content
    const string Default
    const string Root
    const string Trigger
    const string TriggerIcon
  static class ColorPicker
    const string AlphaTrack
    const string Content
    const string Default
    const string HexInput
    const string HueThumb
    const string HueTrack
    const string PresetSwatch
    const string PresetsGrid
    const string SaturationArea
    const string Swatch
    const string SwatchLg
    const string SwatchSm
    const string Thumb
    const string Trigger
  static class Combobox
    const string Content
    const string Empty
    const string Item
    const string ItemSelected
    const string List
    const string Search
    const string Trigger
  static class Command
    const string Default
    const string Dialog
    const string Empty
    const string Group
    const string GroupHeading
    const string Input
    const string InputWrapper
    const string Item
    const string List
    const string Root
    const string Separator
    const string Shortcut
  static class CommandPalette
    const string Empty
    const string GroupLabel
    const string Item
    const string List
    const string Panel
    const string Search
  static class Container
    const string Full
    const string Lg
    const string Md
    const string Prose
    const string Screen
    const string Sm
    const string Xl
    const string Xl2
    const string Xl3
    const string Xl4
    const string Xl5
    const string Xl6
    const string Xl7
    const string Xs
  static class ContentGrid
    const string Bordered
    const string Cell
    const string CellMuted
    const string Default
    const string Header
  static class DataTable
    const string Cell
    const string DataCell
    const string Default
    const string EmptyState
    const string Header
    const string HeaderCell
    const string PageNumber
    const string PageNumberActive
    const string Pagination
    const string PaginationButton
    const string ResizeHandle
    const string Row
    const string RowClickable
  static class DatePicker
    const string Content
    const string Default
    const string Trigger
    const string TriggerLg
    const string TriggerSm
  static class Dialog
    const string CloseButton
    const string Content
    const string Description
    const string Footer
    const string Header
    const string Overlay
    const string Title
  static class DragDrop
    const string Container
    const string ContainerHorizontal
    const string DropZone
    const string DropZoneActive
    const string Overlay
    const string OverlayContent
  static class DragDrop.Droppable
    const string Base
    const string Default
    const string Disabled
    const string Info
    const string Success
  static class DragDrop.Item
    const string Base
    const string Dashed
    const string Default
    const string Disabled
    const string Dragging
  static class Drawer
    const string Content
    const string Default
    const string Description
    const string Footer
    const string Handle
    const string Header
    const string Overlay
    const string Title
  static class Drawer.Snap
    const string Full
    const string Half
    const string Quarter
    const string ThreeQuarter
  static class DropdownMenu
    const string CheckboxItem
    const string Content
    const string Group
    const string Item
    const string Label
    const string RadioItem
    const string Separator
    const string Shortcut
    const string SubContent
    const string SubTrigger
  static class EmptyState
    const string Actions
    const string Description
    const string IconSize
    const string IconSizeSm
    const string IconWrap
    const string IconWrapSm
    const string IllustrationSize
    const string IllustrationWrap
    const string IllustrationWrapSm
    const string Root
    const string RootFull
    const string RootSm
    const string Title
  static class FeedScroller
    const string Default
    const string MuteToggle
    const string Root
    const string Slide
    const string SlideMedia
    const string SlideOverlay
  static class FileUpload
    const string FileItem
    const string FileList
    const string FileName
    const string FileSize
    const string RemoveButton
    const string TypeIcon
  static class FileUpload.Icon
    const string Base
    const string Brand
    const string Disabled
    const string Error
    const string Info
    const string Neutral
    const string Success
    const string Warning
  static class FileUpload.Zone
    const string Active
    const string ActiveRing
    const string Base
    const string Code
    const string Compact
    const string Default
    const string Disabled
    const string Documents
    const string DragOverlay
    const string Images
    const string Wrapper
  static class FormField
    const string ErrorText
    const string HelpText
    const string Label
    const string LabelRequired
    const string ParamRow
    const string Root
    const string SuccessText
    const string WarningText
  static class HoverCard
    const string Content
    const string Default
  interface ITheme
    string Css { get; }
    string DefaultIconLibrary { get; }
  static class Icon
    const string Default
    const string Lg
    const string Md
    const string Sm
    const string Spinner
    const string SpinnerLg
    const string SpinnerSm
    const string Xl
    const string Xs
  // A key/value override map on top of the Ikon CSS baseline. Keys are a vocabulary alias (ThemeVocabulary, e.g. primary, card, radius), a CSS variable name without the leading --, or a Tailwind token; values are Crosswind/Tailwind classes or raw CSS. Set entries via the indexer during object initialization; pair DarkMode for the dark scheme.
  sealed class IkonTheme : ITheme
    ctor()
    // Valid only in ThemeMode.Adaptive mode; combining it with ThemeMode.Fixed throws InvalidOperationException at render time.
    IkonTheme? DarkMode { get; init; }
    string this[string token] { get; set; }
    ThemeMode Mode { get; init; }
  static class ImageCard
    const string Caption
    const string Image
    const string Root
    const string Title
  static class ImageCard.Hover
    const string Dim
    const string Zoom
  static class ImageCard.Overlay
    const string Center
    const string Dim
    const string Reveal
  static class Input
    const string Base
    const string Default
    const string DefaultLg
    const string DefaultSm
    const string Error
    const string ErrorLg
    const string ErrorSm
    const string Ghost
    const string GhostLg
    const string GhostSm
    const string Invalid
    const string InvalidLg
    const string InvalidSm
    const string Success
    const string SuccessLg
    const string SuccessSm
    const string Warning
    const string WarningLg
    const string WarningSm
  static class Input.Password
    const string Input
    const string Toggle
    const string Wrapper
  static class Interaction
    const string HoverCard
    const string HoverGlow
    const string HoverLift
  static class Kbd
    const string Default
    const string Group
  static class Label
    const string Base
    const string Default
    const string Error
    const string Optional
    const string Required
  static class Layout
    const string Center
    const string Page
    const string RowWrap
    const string Section
    const string SectionBody
    const string SectionHeader
    const string Stretch
  static class Layout.Column
    const string Center
    const string Default
    const string Lg
    const string Md
    const string Sm
    const string Xl
    const string Xs
  static class Layout.Grid
    const string Cols2
    const string Cols3
    const string Cols4
  static class Layout.Row
    const string Default
    const string InlineCenter
    const string Lg
    const string Md
    const string Sm
    const string SpaceBetween
    const string Xl
    const string Xs
  static class Layout.Split
    const string Detail
    const string DetailLg
    const string Gapped
    const string Main
    const string Root
    const string Sidebar
    const string SidebarLg
    const string SidebarSm
  static class Markdown
    const string Blocks
    const string Code
    const string Default
    const string Headings
    const string Table
  static class Media
    const string CanvasFill
    const string Default
    const string EmptyState
    const string Fill
    const string ImageEmptyState
    const string Mirror
    const string PlaceholderHint
    const string PlaceholderIcon
    const string PlaceholderText
    const string VideoContainer
  static class Menu
    const string Content
    const string Item
    const string ItemDestructive
    const string Label
    const string Separator
    const string Shortcut
  static class Menubar
    const string Content
    const string Default
    const string Item
    const string Root
    const string Separator
    const string Trigger
  static class MicButton
    const string Active
    const string Base
    const string Default
    const string Lg
    const string Md
    const string Sm
    const string WhileCapturing
  static class NavItem
    const string Active
    const string ActiveAccent
    const string ActiveBrand
    const string ActiveSubtle
    const string Count
    const string Default
    const string Icon
    const string Label
    const string Lg
    const string Md
    const string Sm
    const string Subtle
  static class NavPanel
    const string Base
    const string Border
    const string Divided
    const string Filled
    const string Ghost
  static class NavSection
    const string Divider
    const string Label
    const string Root
  static class NavigationMenu
    const string Content
    const string ContentNarrow
    const string ContentPopover
    const string ContentPopoverSide
    const string ContentWide
    const string Default
    const string Indicator
    const string Link
    const string LinkCompact
    const string List
    const string ListVertical
    const string Root
    const string Trigger
    const string TriggerDisabled
    const string TriggerIcon
    const string TriggerIconRotate180
    const string TriggerIconRotate90
    const string TriggerVertical
    const string Viewport
  static class OnSurface.Card
    const string Caption
    const string Muted
    const string Subtle
    const string Text
  static class OnSurface.Default
    const string Caption
    const string Muted
    const string Subtle
    const string Text
  static class OnSurface.Popover
    const string Caption
    const string Muted
    const string Subtle
    const string Text
  static class OtpField
    const string Default
    const string Input
    const string Root
  static class Page
    const string Base
    const string Default
    const string Plain
  static class Pagination
    const string Active
    const string Disabled
    const string Ellipsis
    const string Item
    const string List
    const string Next
    const string Previous
    const string Root
  static class Panel
    const string Fill
    const string Side
    const string Sidebar
    const string SidebarNarrow
    const string Wide
  static class Popover
    const string Content
    const string Default
  static class Progress
    static string ComposeIndicator(string? variant = null, bool indeterminate = false, params string?[] overrides)
    static string IndicatorTransform(double value)
    const string Base
    const string Default
    const string Indeterminate
    const string Indicator
    const string IndicatorBase
    const string Label
    const string Root
    const string Value
  static class Progress.Size
    const string Lg
    const string Md
    const string Sm
    const string Xs
  static class Progress.Variant
    const string Default
    const string Error
    const string Success
    const string Warning
  static class RadioGroup
    const string Default
    const string Indicator
    const string Item
    const string Root
    const string RootHorizontal
  static class ResizableSplit
    const string FirstPane
    const string FirstPaneVertical
    const string Handle
    const string HandleVertical
    const string Root
    const string SecondPane
    const string SecondPaneVertical
  static class Responsive
    const string CenterToEnd
    const string CenterToLeft
    const string CenterToSpaceBetween
    const string CenterToStart
    const string ColToRow
    const string ColToRowMd
    const string HiddenDesktop
    const string HiddenMobile
    const string HiddenTablet
    const string LeftToCenter
    const string RowToCol
    const string VisibleMobile
    const string VisibleTablet
  static class RichTextEditor
    const string Content
    const string Default
    const string Root
    const string Toolbar
    const string ToolbarButton
    const string ToolbarSeparator
  static class ScrollArea
    const string Bordered
    const string Default
    const string Root
    const string Scrollbar
    const string Thumb
    const string Viewport
  static class Scrollbar
    const string Default
    const string Hidden
    const string Thin
  static class Select
    const string Content
    const string Default
    const string Item
    const string ItemIndicator
    const string Label
    const string ScrollButton
    const string Separator
    const string Trigger
    const string TriggerBase
  static class Select.Group
    const string Label
    const string Root
  static class Select.Size
    const string Lg
    const string Md
    const string Sm
  static class Separator
    const string Base
    const string Horizontal
    const string Vertical
  static class Separator.Orientation
    const string Horizontal
    const string Vertical
  static class Separator.Variant
    const string Default
    const string Strong
    const string Subtle
  static class Sheet
    const string Base
    const string CloseButton
    const string Default
    const string Description
    const string Footer
    const string Header
    const string Overlay
    const string Title
  static class Sheet.Side
    const string Bottom
    const string Left
    const string Right
    const string Top
  static class Skeleton
    const string Avatar
    const string AvatarLg
    const string AvatarSm
    const string Base
    const string Button
    const string Card
    const string Default
    const string Input
    const string Text
    const string TextLg
    const string TextSm
  static class Skeleton.Shape
    const string Circle
    const string Rectangle
    const string Square
  static class Skeleton.Size
    const string Lg
    const string Md
    const string Sm
    const string Xl
    const string Xs
  static class Slider
    const string Default
    const string Range
    const string Root
    const string RootVertical
    const string Thumb
    const string Track
    const string TrackVertical
  static class StatCard
    const string Header
    const string IconBox
    const string IconBoxBrand
    const string IconBoxError
    const string IconBoxInfo
    const string IconBoxSuccess
    const string IconBoxWarning
    const string IconSize
    const string Label
    const string Root
    const string Trend
    const string TrendIcon
    const string TrendLabel
    const string TrendValue
    const string Value
    const string ValueRow
  static class StatCard.TrendVariant
    const string Negative
    const string Neutral
    const string Positive
  static class State
    const string Checked
    const string Disabled
    const string Empty
    const string Focusable
    const string Indeterminate
    const string Invalid
    const string Loading
    const string Pending
    const string Pressable
    const string Readonly
    const string Selected
    const string Success
    const string Validating
    const string Warning
  static class Switch
    const string Default
    const string Root
    const string Thumb
  static class Tabs
    const string Content
    const string List
    const string ListVertical
    const string Trigger
    const string TriggerDisabled
  static class Text
    const string Body
    const string BodySm
    const string BodyStrong
    const string Caption
    const string Code
    const string Display
    const string DisplaySm
    const string H1
    const string H2
    const string H3
    const string H4
    const string H5
    const string H6
    const string Label
    const string Link
    const string Muted
    const string Numeric
    const string Overline
    const string Small
    const string Tabular
  static class Textarea
    const string Base
    const string Default
    const string DefaultLg
    const string DefaultSm
    const string Error
    const string ErrorLg
    const string ErrorSm
    const string Ghost
    const string GhostLg
    const string GhostSm
    const string Invalid
    const string InvalidLg
    const string InvalidSm
    const string Success
    const string SuccessLg
    const string SuccessSm
    const string Warning
    const string WarningLg
    const string WarningSm
  // Adaptive (the default) supports switchable light + dark; Fixed commits to one scheme so a client-side theme flip changes nothing the theme defines. Use Fixed for game, atmospheric, or brand-locked looks that must never light/dark switch.
  enum ThemeMode
    Adaptive
    Fixed
  static class ThemeVocabulary
    static IReadOnlyDictionary<string, ThemeVocabulary.Alias> Aliases { get; }
  sealed record ThemeVocabulary.Alias
    ctor(string Name, IReadOnlyList<string> Targets, ThemeVocabulary.ValueKind Kind)
    ThemeVocabulary.ValueKind Kind { get; init; }
    string Name { get; init; }
    IReadOnlyList<string> Targets { get; init; }
  enum ThemeVocabulary.ValueKind
    Color
    FontFamily
    Radius
    Duration
    Easing
    Spacing
  static class TimePicker
    const string Column
    const string ColumnSeparator
    const string Content
    const string Default
    const string Item
    const string ItemSelected
    const string Trigger
  static class Toast
    const string Action
    const string Base
    const string Close
    const string Default
    const string Description
    const string Title
    const string Viewport
    const string ViewportBottomCenter
  static class Toggle
    const string Base
    const string Default
    const string DefaultLg
    const string DefaultMd
    const string DefaultSm
    const string Group
    const string GroupVertical
    const string IconDefault
    const string IconDefaultLg
    const string IconDefaultMd
    const string IconDefaultSm
  static class Toggle.Size
    const string Lg
    const string Md
    const string Sm
  static class Toggle.Size.Icon
    const string Lg
    const string Md
    const string Sm
  static class Toggle.Variant
    const string Default
  static class Tokens.Blur
    const string Lg
    const string Md
    const string Sm
  static class Tokens.Duration
    const string Fast
    const string Instant
    const string Normal
    const string Slow
    const string Slower
  static class Tokens.Opacity
    const string GlassLg
    const string GlassMd
    const string GlassSm
    const string O10
    const string O15
    const string O20
    const string O25
    const string O30
    const string O40
    const string O5
    const string O50
  static class Tokens.Radius
    const string Full
    const string Lg
    const string Md
    const string None
    const string Sm
    const string Xl
    const string Xl2
  static class Tokens.Shadow
    const string Lg
    const string Md
    const string None
    const string Sm
    const string Xl
    const string Xl2
  static class Tokens.Width
    const string Dialog
    const string DialogLg
    const string DialogMd
    const string DialogSm
    const string DialogXl
    const string Drawer
    const string Popover
    const string Sheet
    const string Toast
  // The status/meaning axis, mapped to semantic tokens so colors read correctly in light and dark; for a meaning-neutral fill use Variant.
  static class Tone
    const string Error
    const string Ghost
    const string Info
    const string Link
    const string Muted
    const string Neutral
    const string Outline
    const string Primary
    const string Solid
    const string Subtle
    const string Success
    const string Warning
  static class Toolbar
    const string Button
    const string Default
    const string IconStyle
    const string Root
    const string Separator
    const string ToggleGroup
    const string ToggleItem
  static class Tooltip
    const string Content
    const string Default
  static class Transition
    const string Fast
    const string None
    const string Normal
    const string Slow
    const string Slower
  static class Transition.Ease
    const string In
    const string InOut
    const string Linear
    const string Out
  static class Transition.Property
    const string All
    const string Colors
    const string Opacity
    const string Shadow
    const string Transform
  // The fill axis, independent of meaning; pair with a Tone class when the button also carries a status color.
  static class Variant
    const string Ghost
    const string Link
    const string Muted
    const string Outline
    const string Primary
    const string Solid
    const string Subtle
  static class ZIndex
    const string Dropdown
    const string Modal
    const string Overlay
    const string Popover
    const string Sticky
    const string Toast
    const string Tooltip
