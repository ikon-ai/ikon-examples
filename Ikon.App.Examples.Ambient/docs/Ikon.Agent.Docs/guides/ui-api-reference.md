# UI API Reference

## UI Component API Reference

Full API reference for all Ikon.Parallax UI components.

---

# Ikon.Parallax Public API

namespace Ikon.Parallax
  // Arguments passed to a UI action callback, containing the client context and the deserialized payload.
  sealed class ActionArgs<T>
    ctor()
    // The client context of the user who triggered the action.
    Context ClientContext { get; init; }
    // The deserialized action payload.
    T Value { get; init; }
  // The busy/status pattern every async UI handler repeats, as one call. Without this, the standard shape is five lines of ceremony around one line of work:
  // _busy.Value = true;
  // _status.Value = null;
  //
  // try { await LoadAsync(); }
  // catch (Exception ex) { _status.Value = ex.Message; }
  // finally { _busy.Value = false; }
  // RunAsync collapses it to:
  // await _busy.RunAsync(_status, LoadAsync);
  // For the busy flag alone (no status reactive), use _busy.AsToken() from Ikon.Common.Core.Reactive instead.
  static class ReactiveBusyExtensions
    // Runs work with busy raised: clears status, sets the flag for the duration of the work (via ReactiveBoolExtensions.AsToken, so it always returns to false), and routes a failure's message into status instead of throwing. Cancellation (OperationCanceledException) is not treated as a failure and propagates to the caller. Returns whether the work completed, so callers can add their own failure handling on top:
    // if (!await _busy.RunAsync(_status, RefreshAsync))
    // {
    //     _entries.Value = [];
    // }
    static Task<bool> RunAsync(this Reactive<bool> busy, Reactive<string?> status, Func<Task> work)
  // Per-client theme state created by UI.UseTheme. Holds each client's active theme and switches it: Current is bindable in views, and ToggleAsync can be bound directly to a button's onClick.
  sealed class ThemeControl
    // The calling client's active theme. Bindable in views, e.g. name: theme.Current.Value == Theme.Dark ? "sun" : "moon".
    ClientReactive<Theme> Current { get; }
    // Sets the calling client's theme and pushes it to that client.
    Task SetAsync(Theme theme)
    // Flips the calling client between dark and light.
    Task ToggleAsync()
  // Main entry point for the Ikon Parallax reactive UI system. Manages client connections, render cycles, style distribution, and action handling for server-driven UI.
  class UI
    // Creates a new UI instance bound to the given app and theme.
    ctor(IAppBase app, ITheme theme)
    // When true, each render cycle logs per-frame timing breakdowns to the app log.
    bool EnableProfiling { get; set; }
    // Default true. A subtree that reads only non-reactive data will not refresh until one of its reactive dependencies changes; set false to force a full re-render every cycle.
    bool EnableSubtreeCaching { get; set; }
    // Build the whole component tree inside content; it re-renders automatically when any reactive state read during the build changes. This is the app's root render entry point. This overload BLOCKS the calling thread until the initial render completes (it drives the async render with GetAwaiter().GetResult()). Call it from a synchronous startup path; from an async or single-threaded synchronization context call RootAsync and await it instead, to avoid stalling or deadlocking that context.
    void Root(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
    // Build the whole component tree inside content; it re-renders automatically when any reactive state read during the build changes. This is the app's root render entry point.
    Task RootAsync(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
    // Call once in Main, before clients join. With followClient true (the default) a joining client keeps its own saved theme and clients without one get defaultTheme; false forces defaultTheme on every client. Bind the returned Current in views and ToggleAsync to a button's onClick.
    // defaultTheme: The theme applied to clients that have none of their own (or to all clients when followClient is false).
    // followClient: When true, respects a joining client's own saved theme; when false, forces defaultTheme on every join.
    ThemeControl UseTheme(Theme defaultTheme = Dark, bool followClient = true)
  // Represents a UI view scope for building the component tree. Extension methods on this type provide the component API (e.g. Text, Button, Input).
  class UIView
    // The default icon library name used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }
    // True only while capturing the build-time boot snapshot — a public asset shown to everyone before the live UI connects (always false on the live render). Gate per-user or sensitive content on this, preferably via the SnapshotReveal/SnapshotHide/SnapshotOnly wrappers.
    bool IsSnapshot { get; }
    // The boot-snapshot variant id this capture render was asked for (the client's Context.SnapshotVariant): the app's [BootSnapshot] seed rules name variant skeletons, and the capture client passes each id here so the app can branch to the matching skeleton. Empty on route captures (render the real page) and on every live render.
    string SnapshotVariant { get; }
    // Adds a child node with the given type and props. The props parameter is the non-generic IDictionary on purpose: it's the ONLY type that cleanly accepts BOTH a `Dictionary<string, object>` (the natural non-null shape a model builds) AND a `Dictionary<string, object?>` (props that carry null values) with no nullability warning and no suppression. A generic `Dictionary<string, object?>` param warns CS8620 on the non-null form (identity-modulo-nullability), and no PAIR of generic overloads works either — nullability annotations are erased for overload resolution, so two such overloads are CS0111 (same signature) or CS0121 (ambiguous).
    void AddNode(string type, IDictionary? props = null, List<UIViewNode>? children = null, string? key = null, string[]? style = null, string? styleId = null)
    // Registers a callback as a UI action and returns its ID for use in component props.
    string? CreateAction<T>(Func<ActionArgs<T>, Task>? callback)
    // The returned string is an opaque reference to use as an image src (e.g. on an Image component), not a data URL. The data buffer is copied on registration, so the caller may reuse or mutate it immediately after the call. data must be non-empty — an empty buffer has no valid reference and throws ArgumentException.
    string RegisterPayload(byte[] data, string mimeType)
  // Represents a single node in the UI view tree, with identity, props, children, and style information.
  sealed class UIViewNode
    // Ordered child nodes. Treat as immutable: the node is shared by reference into the subtree cache, and the differ relies on the child list being the pristine as-built content, so mutating it corrupts diffing and the cache. The mutable backing list is builder-internal.
    IReadOnlyList<UIViewNode> Children { get; }
    // Lazily computed content signature used for subtree caching and diffing.
    string? ContentFingerprint { get; }
    // True when StableHint came from an explicit key argument, not from a prop such as value or text.
    bool HasExplicitKey { get; }
    // Stable unique identifier for this node.
    string Id { get; }
    // Precomputed hash of Id for fast lookups.
    int IdHash { get; }
    // Debug-only, process-global switch: when true, EVERY node built by ANY view on ANY thread and for ANY client emits a source file/line marker that is serialized into the wire payload, inflating all UI updates. Despite reading like a per-instance toggle it is static mutable state with no thread-safety, so flip it only for local debugging (the runtime sets it from the app's DebugMode) and never leave it on in production.
    static bool IncludeSourceMarkers { get; set; }
    // Component properties passed to the frontend renderer. Backed by the compact shape-interned PropsMap on server-built trees; treat as immutable.
    IReadOnlyDictionary<string, object?> Props { get; }
    // Source file and line marker for debugging, included only when IncludeSourceMarkers is true.
    string? SourceMarker { get; }
    // Hint string used by the stable ID generator to produce deterministic IDs.
    string? StableHint { get; }
    // Resolved Crosswind style class identifiers.
    IReadOnlyList<string> StyleIds { get; }
    // The component type name (e.g. "div", "button").
    string Type { get; }

namespace Ikon.Parallax.Components.Charts
  // Configuration for a chart axis including legend text, tick appearance, and label truncation.
  sealed record AxisConfig
    ctor()
    // For time scales this is a d3-time-format token string (e.g. "%H:%M", "%m/%d %H:%M"), not a .NET format.
    string? Format { get; init; }
    string? Legend { get; init; }
    int? LegendOffset { get; init; }
    // Number of ticks to display. When set, the axis will show approximately this many evenly-spaced ticks instead of one per data point.
    int? TickCount { get; init; }
    int? TickPadding { get; init; }
    int? TickRotation { get; init; }
    int? TickSize { get; init; }
    // Truncate tick label text at this character length.
    int? TruncateTickAt { get; init; }
  // Controls how multiple bar series are displayed.
  enum BarGroupMode
    Stacked
    Grouped
  // Controls the orientation of a bar chart.
  enum BarLayout
    Vertical
    Horizontal
  // Styling for chart axis elements including ticks, legends, and domain lines.
  record ChartAxisStyle
    ctor()
    string? DomainColor { get; init; }
    ChartTextStyle? Legend { get; init; }
    string? TickColor { get; init; }
    ChartTextStyle? TickLabel { get; init; }
  // Event arguments for chart click interactions.
  sealed record ChartClickArgs
    ctor()
    string? Id { get; init; }
    string? IndexValue { get; init; }
    string? SerieId { get; init; }
    object? Value { get; init; }
  // Predefined color schemes for chart series, based on D3 color scales.
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
  // Styling for chart crosshair lines.
  record ChartCrosshairStyle
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Extension methods for rendering interactive chart components (bar, line, pie).
  static class ChartExtensions
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip values and value-axis ticks human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void BarChart(this UIView view, string[]? style = null, IEnumerable<Dictionary<string, object>>? data = null, IEnumerable<string>? keys = null, string? indexBy = null, BarGroupMode? groupMode = null, BarLayout? layout = null, ScaleType? valueScale = null, ScaleType? indexScale = null, bool? reverse = null, double? minValue = null, double? maxValue = null, double? padding = null, double? innerPadding = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enableLabel = null, int? labelSkipWidth = null, int? labelSkipHeight = null, string? labelTextColor = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderRadius = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip Y values and left-axis ticks human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void LineChart(this UIView view, string[]? style = null, IEnumerable<LineChartSeries>? data = null, ScaleType? xScaleType = null, ScaleType? yScaleType = null, double? xScaleMin = null, double? xScaleMax = null, double? yScaleMin = null, double? yScaleMax = null, bool? yScaleStacked = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enablePoints = null, int? pointSize = null, string? pointColor = null, string? pointBorderColor = null, int? pointBorderWidth = null, bool? enableArea = null, double? areaOpacity = null, double? areaBaselineValue = null, bool? enableCrosshair = null, CrosshairType? crosshairType = null, LineCurve? curve = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, double? lineWidth = null, bool? isInteractive = null, bool? useMesh = null, bool? enableSlices = null, string? xFormat = null, string? yFormat = null, string? valueUnit = null, string? gradientFromColor = null, string? gradientToColor = null, IEnumerable<double>? gridXValues = null, IEnumerable<double>? gridYValues = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Well-known valueUnit values — "milliseconds", "seconds", "bytes", "percent", "usd" — render tooltip values human-scaled (e.g. 1333.9 milliseconds → "1.33 s"); any other string is appended verbatim as a suffix.
    static void PieChart(this UIView view, string[]? style = null, IEnumerable<PieChartDatum>? data = null, double? innerRadius = null, double? padAngle = null, double? cornerRadius = null, double? startAngle = null, double? endAngle = null, bool? sortByValue = null, ChartMargin? margin = null, bool? enableArcLabels = null, string? arcLabelsTextColor = null, double? arcLabelsSkipAngle = null, bool? enableArcLinkLabels = null, string? arcLinkLabelsTextColor = null, double? arcLinkLabelsSkipAngle = null, double? arcLinkLabelsThickness = null, string? arcLinkLabelsColor = null, double? activeOuterRadiusOffset = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? arcLabelAsPercentage = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
  // Styling for chart grid lines.
  record ChartGridStyle
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Styling for chart data labels.
  record ChartLabelsStyle
    ctor()
    ChartTextStyle? Text { get; init; }
  // Styling for chart legend text and title.
  record ChartLegendStyle
    ctor()
    ChartTextStyle? Text { get; init; }
    ChartTextStyle? Title { get; init; }
  // Margin configuration for chart containers.
  sealed record ChartMargin
    ctor()
    int? Bottom { get; init; }
    int? Left { get; init; }
    int? Right { get; init; }
    int? Top { get; init; }
  // Text styling for chart elements.
  record ChartTextStyle
    ctor()
    string? Color { get; init; }
    string? FontFamily { get; init; }
    int? FontSize { get; init; }
  // Complete theme configuration for chart components, combining all styling aspects.
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
  // Built-in chart theme presets for light and dark backgrounds.
  static class ChartThemes
    // Chart theme optimized for dark backgrounds with muted but saturated series colors.
    static ChartTheme DefaultDark { get; }
    // Chart theme optimized for light backgrounds with soft, pastel-like series colors.
    static ChartTheme DefaultLight { get; }
  // Styling for chart tooltips.
  record ChartTooltipStyle
    ctor()
    string? BackgroundColor { get; init; }
    string? BorderColor { get; init; }
    int? BorderRadius { get; init; }
    ChartTextStyle? Text { get; init; }
  // Crosshair display type for interactive charts.
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
  // Where a chart legend is anchored within the chart area.
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
  // Configuration for a chart legend including positioning, layout direction, and item sizing.
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
  // Layout direction for chart legend items.
  enum LegendDirection
    Row
    Column
  // A single data point in a line chart series.
  sealed record LineChartPoint
    ctor()
    // Pass a string label for point scales, or a number for linear/time scales — the object type is genuinely mixed.
    required object X { get; init; }
    required double Y { get; init; }
  // A named data series for a line chart, containing an ordered collection of points.
  sealed record LineChartSeries
    ctor()
    string? Color { get; init; }
    IEnumerable<LineChartPoint>? Data { get; init; }
    required string Id { get; init; }
  // Interpolation curve type for line charts.
  enum LineCurve
    Linear
    MonotoneX
    Step
    StepBefore
    StepAfter
    Cardinal
    Basis
  // A single slice in a pie chart.
  sealed record PieChartDatum
    ctor()
    string? Color { get; init; }
    required string Id { get; init; }
    string? Label { get; init; }
    required double Value { get; init; }
  // Scale type for chart axes.
  enum ScaleType
    Point
    Linear
    Time
    Log

namespace Ikon.Parallax.Components.DataTable
  // A single cell in a data table row. Use the static factory methods to create typed cells.
  record Cell
    ctor()
    // Action identifier passed to the onActionClick callback.
    string? ActionId { get; init; }
    // Action buttons for "actions" type cells.
    CellAction[]? Actions { get; init; }
    // When true, the cell's interactive element is disabled.
    bool? Disabled { get; init; }
    // Button label for action cells.
    string? Label { get; init; }
    // Crosswind style classes for the cell.
    string[]? Style { get; init; }
    // Semantic tone for badge cells.
    SemanticTone? Tone { get; init; }
    // The kind of content this cell renders.
    CellType Type { get; init; }
    // Display value or checkbox state ("true"/"false").
    string? Value { get; init; }
    // Creates an action button cell.
    static Cell Action(string label, string actionId, string[]? style = null)
    // Creates a cell containing multiple action buttons.
    static Cell ActionGroup(CellAction[] actions)
    // style classes replace the themed tone token; lead the array with the "default" marker to merge the tone token underneath them instead.
    static Cell Badge(string value, SemanticTone? tone = null, string[]? style = null)
    // Creates a checkbox cell.
    static Cell Checkbox(bool value, string actionId, string[]? style = null, bool disabled = false)
    // Creates a text cell.
    static Cell Text(string? value, string[]? style = null)
  // An action button that can be displayed within a data table cell.
  record CellAction
    ctor(string Label, string ActionId, string[]? Style = null, string? Icon = null)
    string ActionId { get; init; }
    string? Icon { get; init; }
    string Label { get; init; }
    string[]? Style { get; init; }
  // The kind of content a data table cell renders.
  enum CellType
    // Plain display text.
    Text
    // Status badge with an optional semantic tone.
    Badge
    // Single action button.
    Action
    // Group of action buttons.
    Actions
    // Checkbox bound to an action id.
    Checkbox
  // Defines a column in a data table including header text, width, and alignment.
  record DataTableColumn
    ctor(string Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left, string? MinWidth = null, bool Wrap = false)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string Header { get; init; }
    string? MinWidth { get; init; }
    string? Width { get; init; }
    bool Wrap { get; init; }
  // Extension methods for rendering paginated data tables.
  static class DataTableExtensions
    // Renders a paginated data table with configurable columns, rows, actions, and styling. Per-slot styling (header, rows, cells, pagination, …) goes through styles; see DataTableStyles for the slots.
    static void DataTable(this UIView view, DataTableColumn[] columns, DataTableRow[] rows, int totalCount, int pageIndex, int pageSize, Func<int, Task>? onPageChange = null, Func<string, Task>? onRowClick = null, Func<string, Task>? onActionClick = null, Action<UIView>? emptyContent = null, int[]? columnWidths = null, Func<string, Task>? onColumnResize = null, string[]? style = null, DataTableStyles? styles = null, string? prevLabel = null, string? nextLabel = null, string? pageLabel = null, string? key = null)
  // A single row in a data table, identified by a unique ID and containing an array of cells.
  record DataTableRow
    ctor(string Id, Cell[] Cells)
    Cell[] Cells { get; init; }
    string Id { get; init; }
  // Each slot is a Crosswind class array that merges on top of the slot's themed default, exactly like a component's style: parameter; set only the slots you are changing.
  sealed record DataTableStyles
    ctor()
    // Action buttons rendered from action cells.
    string[]? ActionButton { get; init; }
    // Every cell (header and data).
    string[]? Cell { get; init; }
    // Data cells only.
    string[]? DataCell { get; init; }
    // The empty-state container shown when there are no rows.
    string[]? Empty { get; init; }
    // The header row.
    string[]? Header { get; init; }
    // Header cells only.
    string[]? HeaderCell { get; init; }
    // Page number buttons.
    string[]? PageNumber { get; init; }
    // The active page number button.
    string[]? PageNumberActive { get; init; }
    // The pagination bar.
    string[]? Pagination { get; init; }
    // The previous/next pagination buttons.
    string[]? PaginationButton { get; init; }
    // Column resize handles.
    string[]? ResizeHandle { get; init; }
    // Every data row.
    string[]? Row { get; init; }
    // Truncated-cell hover tooltips.
    string[]? Tooltip { get; init; }

namespace Ikon.Parallax.Components.ImageEditor
  // Extension methods for the image editor canvas component.
  static class ImageEditorExtensions
    // triggerSave/triggerUndo/triggerRedo are edge-triggered — increment the value to fire that action. highResolution keeps the canvas at native resolution (sharp zoom, full-quality export, but capped undo history); when false the canvas is downscaled to fit its container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // src: URL or data URL of the image to edit.
    // brushWidth: Brush size in pixels.
    // brushColor: Brush color as hex code (e.g. "#ff0000").
    // tool: Currently active drawing tool. Defaults to ImageEditorTool.Brush on the frontend.
    // zoom: Zoom level (1.0 = 100%, 2.0 = 200%, etc.).
    // highResolution: Keep the canvas at the image's native resolution (capped) so zooming stays sharp and saves export at full quality; also caps undo history. When false the canvas is downscaled to fit its container.
    // textMaxLength: Max character length for the floating text input shown when the text tool is active. Null means no limit.
    // textFontSize: Font size in pixels for the rendered text. Null = derived from brush width.
    // textPadding: Padding in pixels around the text (applied to both input overlay and rendered background). Null = default 4.
    // onSave: Callback when user saves, receives base64 image data.
    // onHistoryChange: Callback when undo/redo history state changes.
    // triggerSave: Increment to trigger a save action.
    // triggerUndo: Increment to trigger an undo action.
    // triggerRedo: Increment to trigger a redo action.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    static void ImageEditorCanvas(this UIView view, string[]? style = null, string? src = null, int? brushWidth = null, string? brushColor = null, ImageEditorTool? tool = null, double? zoom = null, bool? highResolution = null, bool? fitContainer = null, int? textMaxLength = null, int? textFontSize = null, int? textPadding = null, Func<ImageEditorSaveArgs, Task>? onSave = null, Func<ImageEditorHistoryArgs, Task>? onHistoryChange = null, int? triggerSave = null, int? triggerUndo = null, int? triggerRedo = null, string? styleId = null, string? key = null)
  // Event args for when the undo/redo history state changes.
  sealed record ImageEditorHistoryArgs
    ctor(bool CanUndo, bool CanRedo)
    bool CanRedo { get; init; }
    bool CanUndo { get; init; }
  // Event args for when the image editor saves the edited image.
  sealed record ImageEditorSaveArgs
    ctor(string ImageData)
    string ImageData { get; init; }
  // Drawing tool active on an ImageEditorCanvas.
  enum ImageEditorTool
    // Freehand brush strokes.
    Brush
    // Erases previously drawn content.
    Eraser
    // Places text via a floating input.
    Text
    // Draws arrow annotations.
    Arrow
    // Marks a rectangular region.
    Region
    // Draws a freehand outline that closes into a region on release.
    Lasso
    // Draws a straight line segment from press to release.
    Line

namespace Ikon.Parallax.Components.Standard
  // Extension methods for accessibility components.
  static class AccessibilityExtensions
    // Wraps an icon with accessible label for screen readers.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // label: Accessible label announced by screen readers.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering the icon within this component.
    static void AccessibleIcon(this UIView view, string[]? style = null, string? label = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Hides content visually while keeping it accessible to screen readers.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void VisuallyHidden(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Base event returned from a client-side action, indicating the action type and whether it succeeded.
  record ActionEvent
    ctor(string ActionType, bool Success)
    string ActionType { get; init; }
    bool Success { get; init; }
  // Types of client-side actions that can be triggered from the server.
  enum ActionKind
    // Unknown or unrecognized action.
    Unknown
    // Capture an image from the client's camera.
    CaptureImage
    // Copy text to the system clipboard.
    CopyToClipboard
    // Download a file to the client.
    DownloadFile
    // Exit fullscreen mode.
    ExitFullscreen
    // Request the client's geographic location.
    GetLocation
    // Open the contact picker.
    PickContacts
    // Enter fullscreen mode.
    RequestFullscreen
    // Open the native share dialog.
    Share
  // Base class for client-side action configuration.
  abstract record ActionOptions
  // Represents activation mode for Tabs.
  enum ActivationMode
    Automatic
    Manual
  // Inline alert banner composite over the theme's Alert token recipe.
  static class AlertExtensions
    // Caller style replaces the tone's Theming.Alert token; lead the array with "default" to merge that token underneath it. The icon defaults per tone (success/warning/error/info).
    // view: The UIView to render into.
    // title: Alert headline.
    // tone: Semantic tone selecting the Alert color variant (Neutral and Brand use the default surface).
    // style: Crosswind/Tailwind utility classes merged on top of the themed alert token.
    // description: Muted body text under the title.
    // icon: Lucide icon name overriding the tone's default icon.
    // showIcon: When false, no icon is rendered.
    // onDismiss: When set, renders a dismiss (×) button in the top-right corner that invokes this callback.
    // titleStyle: Style for the title text. Defaults to Theming.Alert.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.Alert.Description.
    // iconStyle: Style for the icon.
    // dismissStyle: Style for the dismiss button.
    // content: Builder for extra elements rendered under the description (e.g. action links).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Alert(this UIView view, string title, SemanticTone tone = Neutral, string[]? style = null, string? description = null, string? icon = null, bool showIcon = true, Func<Task>? onDismiss = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? iconStyle = null, string[]? dismissStyle = null, Action<UIView>? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Represents alignment for overlay positioning.
  enum Align
    Start
    Center
    End
  // Status pill composite over the theme's Badge token recipe. Replaces the hand-rolled inline-flex/rounded-full/px-2 pill pattern with a one-liner: view.Badge("Live", SemanticTone.Success).
  static class BadgeExtensions
    // With no style args it renders the themed Theming.Badge.* pill for the tone; caller styles replace the base token, or merge on top of it when the array leads with "default".
    // view: The UIView to render into.
    // text: Badge label.
    // tone: Semantic tone selecting the Badge color variant.
    // style: Crosswind/Tailwind utility classes merged on top of the themed badge token.
    // size: Pill size (Sm/Md/Lg).
    // outline: When true, uses the outlined variant: the tone's border becomes visible instead of transparent. The fill is unchanged.
    // dot: When true, renders a small status dot before the label in the badge's current color.
    // dotStyle: Style for the dot. Defaults to a 6px circle filled with the badge foreground color.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Badge(this UIView view, string text, SemanticTone tone = Neutral, string[]? style = null, BadgeSize size = Md, bool outline = false, bool dot = false, string[]? dotStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Size of a BadgeExtensions.Badge.
  enum BadgeSize
    // Compact — 22px pill with extra-small text.
    Sm
    // Default — 24px pill with small text.
    Md
    // Roomy — 28px pill with small text.
    Lg
  // Breadcrumb trail composite over the theme's Breadcrumb token recipe.
  static class BreadcrumbExtensions
    // Items with an OnClick render as clickable links; the last item always renders as the non-clickable current page (aria-current="page") regardless of its OnClick.
    // view: The UIView to render into.
    // items: Trail entries in root-to-current order.
    // style: Crosswind/Tailwind utility classes merged on top of Theming.Breadcrumb.Root.
    // separatorIcon: Lucide icon name for the separator. Defaults to "chevron-right".
    // linkStyle: Style for clickable items. Defaults to Theming.Breadcrumb.Link.
    // itemStyle: Style for non-clickable, non-current items. Defaults to Theming.Breadcrumb.Item.
    // pageStyle: Style for the current page (last item). Defaults to Theming.Breadcrumb.Page.
    // separatorStyle: Style for the separator icon. Defaults to Theming.Breadcrumb.Separator with a 14px size.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Breadcrumb(this UIView view, IReadOnlyList<BreadcrumbItem> items, string[]? style = null, string? separatorIcon = null, string[]? linkStyle = null, string[]? itemStyle = null, string[]? pageStyle = null, string[]? separatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // One entry in a BreadcrumbExtensions.Breadcrumb trail.
  sealed record BreadcrumbItem
    // Label: Visible text of the crumb.
    // OnClick: Navigation callback. Ignored for the last item, which always renders as the non-clickable current page.
    ctor(string Label, Func<Task>? OnClick = null)
    // Visible text of the crumb.
    string Label { get; init; }
    // Navigation callback. Ignored for the last item, which always renders as the non-clickable current page.
    Func<Task>? OnClick { get; init; }
  // Extension methods for Calendar and DatePicker components.
  static class CalendarExtensions
    // All date values (value, defaultValue, minDate, maxDate, callbacks) are ISO yyyy-MM-dd strings; month is yyyy-MM. Controlled via value+onValueChange; omit both and pass defaultValue for uncontrolled.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for the root grid container. Use Calendar.Root.
    // value: Controlled selected date (ISO yyyy-MM-dd).
    // defaultValue: Initial selected date for uncontrolled mode.
    // month: Controlled display month (ISO yyyy-MM or yyyy-MM-dd).
    // defaultMonth: Initial display month for uncontrolled mode.
    // minDate: Earliest selectable date (inclusive).
    // maxDate: Latest selectable date (inclusive).
    // disabledDates: Explicit dates that cannot be selected.
    // weekStart: Day of the week the grid starts on. Defaults to Monday.
    // locale: BCP-47 locale used for weekday and month labels (e.g. en-US).
    // disabled: When true, prevents user interaction with this component.
    // headerStyle: Style for the month header row. Use Calendar.Header.
    // weekdayStyle: Style for the weekday-label row. Use Calendar.Weekday.
    // dayStyle: Style for day cells. Use Calendar.Day.
    // daySelectedStyle: Style for the selected day cell. Use Calendar.DaySelected.
    // dayTodayStyle: Style for today's cell. Use Calendar.DayToday.
    // dayOutsideStyle: Style for cells outside the current month. Use Calendar.DayOutside.
    // dayDisabledStyle: Style for disabled day cells. Use Calendar.DayDisabled.
    // navButtonStyle: Style for the previous/next month nav buttons. Use Calendar.NavButton.
    // titleStyle: Style for the month/year title. Use Calendar.HeaderTitle.
    // gridStyle: Style for the body container that stacks weekday + week rows. Use Calendar.Grid.
    // rowStyle: Style for each 7-cell week row (also the weekday-label row). Use Calendar.Row.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the selected date changes (ISO yyyy-MM-dd).
    // onMonthChange: Invoked when the display month changes (ISO yyyy-MM).
    static void Calendar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? month = null, string? defaultMonth = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, string? locale = null, bool? disabled = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onMonthChange = null)
    // Button that opens a popover containing a Calendar.
    // view: The UIView to render into.
    // value: Controlled selected date (ISO yyyy-MM-dd).
    // defaultValue: Initial selected date for uncontrolled mode.
    // placeholder: Text shown in the trigger when no date is selected.
    // format: BCP-47 locale format hint for the trigger label (e.g. en-US).
    // minDate: Earliest selectable date (inclusive).
    // maxDate: Latest selectable date (inclusive).
    // disabledDates: Explicit dates that cannot be selected.
    // weekStart: Day of the week the grid starts on. Defaults to Monday.
    // disabled: When true, prevents user interaction.
    // open: Controlled open state of the popover.
    // defaultOpen: Initial open state for uncontrolled mode.
    // side: Side the popover content renders against.
    // align: Alignment of the content along the chosen side.
    // triggerStyle: Style for the trigger button. Use DatePicker.Trigger.
    // contentStyle: Style for the popover content container. Use DatePicker.Content.
    // calendarStyle: Style applied to the embedded Calendar grid root.
    // headerStyle: Style for the embedded Calendar's month header row.
    // weekdayStyle: Style for the embedded Calendar's weekday labels.
    // dayStyle: Style for the embedded Calendar's day cells.
    // daySelectedStyle: Style for the embedded Calendar's selected day cell.
    // dayTodayStyle: Style for the embedded Calendar's today cell.
    // dayOutsideStyle: Style for the embedded Calendar's cells outside the current month.
    // dayDisabledStyle: Style for the embedded Calendar's disabled day cells.
    // navButtonStyle: Style for the embedded Calendar's previous/next month nav buttons.
    // titleStyle: Style for the embedded Calendar's month/year title.
    // gridStyle: Style for the embedded Calendar's body container.
    // rowStyle: Style for each 7-cell row in the embedded Calendar.
    // rootStyle: Style for the outermost container. Rarely needed.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the selected date changes (ISO yyyy-MM-dd).
    // onOpenChange: Invoked when the popover open state changes.
    // label: Optional field label rendered above the date picker (same field ergonomics as TextField).
    static void DatePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, string? format = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? calendarStyle = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Which physical camera to prefer when starting the capture. Maps to the W3C MediaStream facingMode constraint and is treated as an "ideal" hint — the browser falls back to whatever camera is available if the requested side does not exist (e.g. desktops without a rear camera).
  enum CameraFacing
    // Front-facing camera (user-facing). Maps to facingMode: "user".
    User
    // Rear-facing camera (away from the user). Maps to facingMode: "environment".
    Environment
  // Options for capturing an image from the client's camera.
  sealed record CaptureImageActionOptions : ActionOptions
    ctor()
    // Hardware constraints for camera selection.
    CaptureImageConstraints? Constraints { get; init; }
    // Output image format.
    ClientImageCaptureFormat? Format { get; init; }
    // Desired image height in pixels.
    int? Height { get; init; }
    // How the capture is presented (native OS camera UI vs. headless silent grab). Defaults to CaptureImageMode.Headless — silent webcam capture via getUserMedia, which works uniformly on desktop and mobile. Set to CaptureImageMode.Native to opt in to the OS camera app on phones (preview + shutter + front/back toggle); on desktop browsers Native transparently falls back to the headless path because the web platform doesn't expose a camera-app launch.
    CaptureImageMode? Mode { get; init; }
    // Image quality (0.0 to 1.0) for lossy formats.
    double? Quality { get; init; }
    // Desired image width in pixels.
    int? Width { get; init; }
  // Hardware constraints for image capture. Applied directly when CaptureImageActionOptions.Mode is CaptureImageMode.Headless. In CaptureImageMode.Native mode only FacingMode is honored (mapped to the file input's capture attribute); the OS camera UI ignores other constraints.
  sealed record CaptureImageConstraints
    ctor()
    // Preferred camera device ID. Headless mode only.
    string? DeviceId { get; init; }
    // Preferred camera side (front vs. rear). Most useful on phones where CameraFacing.Environment opens the rear camera by default. On desktops with only a webcam this is ignored.
    CameraFacing? FacingMode { get; init; }
  // How the image capture is presented to the user. Controls whether the OS camera UI is invoked or whether the capture happens silently.
  enum CaptureImageMode
    // Open the native OS camera UI (preview + shutter + front/back toggle on phones). Best UX for "take a photo" buttons. On mobile this is implemented via a transient <input type="file" capture> and therefore must be invoked from a user gesture; the user can dismiss without capturing. On desktop browsers — where that input degrades to a plain file picker — the SDK transparently falls back to the headless getUserMedia grab so the click still produces a webcam frame.
    Native
    // Silent, headless capture: getUserMedia opens the camera, the SDK grabs a single frame off-screen and tears the stream down. No preview, no shutter. Useful for kiosks, automation, ID-scan flows where the timing is server-driven, or when you render your own preview UI elsewhere. Honors CaptureImageConstraints.
    Headless
  // Card-family composites: Card, StatCard, and EmptyState. All are server-side compositions over the container/text primitives styled by the Theming.Card / Theming.StatCard / Theming.EmptyState token recipes — beautiful by default, every part overridable.
  static class CardExtensions
    // With no style args it renders the themed card token (Theming.Card.Default, or Theming.Card.Interactive when onClick is set); caller styles replace it, or merge on top of it when the array leads with "default".
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the themed card base token.
    // title: Title text rendered in the card header.
    // description: Muted description text rendered under the title.
    // header: Builder for extra header elements rendered after the title/description.
    // content: Builder for the card body.
    // footer: Builder for the card footer (actions row).
    // headerStyle: Style for the header container. Defaults to Theming.Card.Header.
    // titleStyle: Style for the title text. Defaults to Theming.Card.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.Card.Description.
    // contentStyle: Style for the body container. Defaults to Theming.Card.Content when a header is present, plain padding otherwise.
    // footerStyle: Style for the footer container. Defaults to Theming.Card.Footer.
    // onClick: Invoked when the user clicks the card. Accepts sync (() => …) and async (async () => …) lambdas alike. When set, the interactive card token is used by default.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Card(this UIView view, string[]? style = null, string? title = null, string? description = null, Action<UIView>? header = null, Action<UIView>? content = null, Action<UIView>? footer = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? contentStyle = null, string[]? footerStyle = null, Delegate? onClick = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Card — positional (style, children) overload so view.Card([style], v => {...}) binds the lambda to the body instead of tripping on the title parameter.
    static void Card(this UIView view, string[]? style, Action<UIView> children)
    // Centered placeholder for empty lists/pages — optional icon, title, optional description, and an optional action row — per the theme's EmptyState recipe.
    // view: The UIView to render into.
    // title: Short headline (e.g. "No results yet").
    // style: Crosswind/Tailwind utility classes merged on top of Theming.EmptyState.Root.
    // description: Muted explanation text under the title.
    // icon: Lucide icon name rendered inside the tinted icon square.
    // action: Builder for the action row (e.g. a "Create" button).
    // iconWrapStyle: Style for the icon square. Defaults to Theming.EmptyState.IconWrap.
    // iconStyle: Style for the icon itself. Defaults to Theming.EmptyState.IconSize.
    // titleStyle: Style for the title text. Defaults to Theming.EmptyState.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.EmptyState.Description.
    // actionsStyle: Style for the action row. Defaults to Theming.EmptyState.Actions.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void EmptyState(this UIView view, string title, string[]? style = null, string? description = null, string? icon = null, Action<UIView>? action = null, string[]? iconWrapStyle = null, string[]? iconStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? actionsStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Dashboard statistic card — label, large value, optional delta with trend arrow, and an optional icon box — per the theme's StatCard recipe.
    // view: The UIView to render into.
    // label: Small muted metric label (e.g. "Revenue").
    // value: Large headline value (e.g. "€12,400").
    // style: Crosswind/Tailwind utility classes merged on top of Theming.StatCard.Root.
    // delta: Delta text rendered next to the value (e.g. "+12%").
    // trend: Direction of the delta — controls the trend arrow and its tone.
    // trendLabel: Muted context text after the delta (e.g. "vs last month").
    // icon: Lucide icon name rendered inside the tinted icon box on the right.
    // iconTone: SemanticTone of the icon box background/foreground.
    // labelStyle: Style for the label text. Defaults to Theming.StatCard.Label.
    // valueStyle: Style for the value text. Defaults to Theming.StatCard.Value.
    // trendStyle: Style for the delta row. Defaults to Theming.StatCard.Trend plus the trend tone.
    // iconBoxStyle: Style for the icon box. Defaults to the tone variant of Theming.StatCard.IconBox.
    // iconStyle: Style for the icon itself. Defaults to Theming.StatCard.IconSize.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void StatCard(this UIView view, string label, string value, string[]? style = null, string? delta = null, StatTrend trend = Flat, string? trendLabel = null, string? icon = null, SemanticTone iconTone = Neutral, string[]? labelStyle = null, string[]? valueStyle = null, string[]? trendStyle = null, string[]? iconBoxStyle = null, string[]? iconStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Alignment of slides relative to the carousel viewport.
  enum CarouselAlign
    // Align slides to the start of the viewport.
    Start
    // Align slides to the center of the viewport.
    Center
    // Align slides to the end of the viewport.
    End
  // Responsive carousel configuration applied above a container-width threshold.
  sealed record CarouselBreakpoint
    // MinWidth: Container width (in CSS pixels) at which this breakpoint becomes active. The active breakpoint is the one with the largest MinWidth still less than or equal to the current container width.
    // SlidesPerView: Number of slides visible in the viewport at this breakpoint.
    // SlidesPerGroup: Number of slides advanced per navigation step at this breakpoint. Defaults to SlidesPerView when null.
    // SlideGapPx: Gap in CSS pixels between adjacent slides at this breakpoint. Defaults to the top-level slideGapPx when null.
    ctor(int MinWidth, int SlidesPerView, int? SlidesPerGroup = null, int? SlideGapPx = null)
    // Container width (in CSS pixels) at which this breakpoint becomes active. The active breakpoint is the one with the largest MinWidth still less than or equal to the current container width.
    int MinWidth { get; init; }
    // Gap in CSS pixels between adjacent slides at this breakpoint. Defaults to the top-level slideGapPx when null.
    int? SlideGapPx { get; init; }
    // Number of slides advanced per navigation step at this breakpoint. Defaults to SlidesPerView when null.
    int? SlidesPerGroup { get; init; }
    // Number of slides visible in the viewport at this breakpoint.
    int SlidesPerView { get; init; }
  // Extension methods for Carousel components.
  static class CarouselExtensions
    // Provide slides via slides for the simple case, or via the content builder using Slide for fully custom children.
    // view: The UIView to render into.
    // index: Controlled zero-based slide index.
    // defaultIndex: Initial slide index for uncontrolled mode.
    // orientation: Scroll axis.
    // align: Alignment of slides in the viewport.
    // loop: When true, navigation wraps from last to first and vice versa.
    // autoPlayMs: When set, advances to the next slide every N milliseconds.
    // slidesPerView: Default number of slides visible in the viewport at once. Defaults to 1 (single-slide carousel). Set to a value greater than 1 for a multi-item carousel. Overridden by the matching entry in breakpoints when one applies.
    // slidesPerGroup: Default number of slides advanced per navigation step (arrows, indicators, autoplay). Defaults to slidesPerView so paging matches the visible window; set to 1 for one-at-a-time scrolling through a multi-item view. Overridden by the matching entry in breakpoints when one applies.
    // slideGapPx: Default gap in CSS pixels between adjacent slides. Only takes effect when the effective slides-per-view is greater than 1. Defaults to 0. Overridden by the matching entry in breakpoints when one applies.
    // breakpoints: Responsive configurations applied based on the carousel container width. The active breakpoint is the one with the largest MinWidth still less than or equal to the current container width; values fall back to the top-level slidesPerView / slidesPerGroup / slideGapPx when no breakpoint applies.
    // slides: Collection of slides rendered in order.
    // showArrows: When true, renders Previous/Next buttons. Defaults to true.
    // showIndicators: When true, renders indicator dots. Defaults to true.
    // previousLabel: Accessible label for the previous button.
    // nextLabel: Accessible label for the next button.
    // previousIconName: Icon name for the previous button.
    // nextIconName: Icon name for the next button.
    // rootStyle: Style for the outermost container. Use Carousel.Root.
    // viewportStyle: Style for the scrolling viewport. Use Carousel.Viewport.
    // slideStyle: Style applied to each slide.
    // previousStyle: Style for the previous button. Use Carousel.Previous.
    // nextStyle: Style for the next button. Use Carousel.Next.
    // indicatorsStyle: Style for the indicator bar. Use Carousel.Indicators.
    // indicatorStyle: Style for a single indicator dot.
    // indicatorActiveStyle: Style for the active indicator dot.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering custom slides via Slide.
    // onIndexChange: Invoked when the active slide index changes.
    static void Carousel(this UIView view, string[]? style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, int? slidesPerView = null, int? slidesPerGroup = null, int? slideGapPx = null, IEnumerable<CarouselBreakpoint>? breakpoints = null, IEnumerable<CarouselSlideItem>? slides = null, bool? showArrows = null, bool? showIndicators = null, string? previousLabel = null, string? nextLabel = null, string? previousIconName = null, string? nextIconName = null, string[]? rootStyle = null, string[]? viewportStyle = null, string[]? slideStyle = null, string[]? previousStyle = null, string[]? nextStyle = null, string[]? indicatorsStyle = null, string[]? indicatorStyle = null, string[]? indicatorActiveStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<int, Task>? onIndexChange = null)
    // A single slide inside a Carousel. Use when rendering slides manually.
    // view: The UIView to render into.
    // style: Style classes for the slide container.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this slide.
    static void Slide(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Declarative slide definition for CarouselExtensions.Carousel.
  sealed record CarouselSlideItem
    // Content: Builder function for rendering the slide.
    // Key: Optional stable key used for diffing.
    ctor(Action<UIView> Content, string? Key = null)
    // Builder function for rendering the slide.
    Action<UIView> Content { get; init; }
    // Optional stable key used for diffing.
    string? Key { get; init; }
  // Extension methods for the ChatLog primitive — the canonical chat-bubble layout shape: header + scrolling auto-scrolled body + composer. Wraps ScrollColumnExtensions.ScrollColumn with chat-friendly defaults so callers don't have to remember to set autoScroll: true.
  static class ChatLogExtensions
    // Use instead of a manual Column(overflow-auto) for any "newest at the bottom, follow when content grows" layout. autoScrollKey tells the framework when to re-anchor to the bottom — pass the reactive message collection, a count, or any other value that changes when the content does.
    // autoScrollKey: Anything whose value changes when the content changes — the reactive message collection, a count, or a composite string (see LayoutExtensions.ScrollArea).
    static void ChatLog(this UIView view, string[]? style = null, object? autoScrollKey = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, string? styleId = null, string? key = null)
  // Represents the checked state for checkbox-like components.
  enum CheckedState
    Unchecked
    Checked
    Indeterminate
  // Extension methods for the CodeEditor component.
  static class CodeEditorExtensions
    // Monospace code editor with an optional line-number gutter.
    // view: The UIView to render into.
    // value: Controlled text value. A controlled value with no write-back handler (no onValueChange, no onSubmit) renders the editor read-only, since edits would have nowhere to go.
    // defaultValue: Initial value for uncontrolled mode.
    // language: Language identifier used for syntax highlighting (e.g. typescript, csharp, json).
    // placeholder: Placeholder text shown when empty.
    // readOnly: When true, prevents editing but allows selection and copy.
    // disabled: When true, prevents user interaction entirely.
    // showLineNumbers: When true, renders a line-number gutter. Defaults to true.
    // tabSize: Number of spaces inserted by Tab. Defaults to 2.
    // insertSpaces: When true, Tab inserts spaces; when false, a tab character.
    // wrap: When true, long lines wrap instead of scrolling horizontally.
    // minRows: Minimum number of visible rows.
    // maxRows: Maximum number of rows before scrolling.
    // style: Style for the outermost container. Use CodeEditor.Root.
    // gutterStyle: Style for the line-number gutter. Use CodeEditor.Gutter.
    // contentStyle: Style for the code content area. Use CodeEditor.Content.
    // languageBadgeStyle: Style for the language badge in the top-right corner.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the text value changes.
    // onSubmit: Invoked when the user presses Ctrl+Enter. The parameter contains the submitted value — prefer it over re-reading the bound reactive.
    static void CodeEditor(this UIView view, string? value = null, string? defaultValue = null, string? language = null, string? placeholder = null, bool? readOnly = null, bool? disabled = null, bool? showLineNumbers = null, int? tabSize = null, bool? insertSpaces = null, bool? wrap = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? gutterStyle = null, string[]? contentStyle = null, string[]? languageBadgeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  // Represents collision detection strategy for @dnd-kit.
  enum CollisionDetection
    ClosestCenter
    ClosestCorners
    RectIntersection
    PointerWithin
  // Output string format for ColorPickerExtensions.ColorPicker.
  enum ColorFormat
    // Hex notation, e.g. #ff0000.
    Hex
    // CSS rgb() notation, e.g. rgb(255, 0, 0).
    Rgb
    // CSS hsl() notation, e.g. hsl(0, 100%, 50%).
    Hsl
  // Extension methods for ColorPicker components.
  static class ColorPickerExtensions
    // Swatch-triggered color picker with hue slider, saturation/lightness square, and hex input.
    // view: The UIView to render into.
    // value: Controlled color in the chosen format.
    // defaultValue: Initial color for uncontrolled mode.
    // format: Output format produced by onValueChange.
    // showAlpha: When true, shows an alpha slider and emits #RRGGBBAA/rgba()/hsla().
    // presets: Optional preset swatches displayed beneath the picker.
    // disabled: When true, prevents user interaction.
    // open: Controlled open state of the popover.
    // defaultOpen: Initial open state for uncontrolled mode.
    // side: Side the popover content renders against.
    // align: Alignment of the content along the chosen side.
    // triggerStyle: Style for the swatch trigger. Use ColorPicker.Trigger.
    // contentStyle: Style for the popover content container. Use ColorPicker.Content.
    // rootStyle: Style for the outermost container. Rarely needed.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked as the user drags or types a new color.
    // onValueCommit: Invoked once the user releases a drag or commits a typed value.
    // onOpenChange: Invoked when the popover open state changes.
    // label: Optional field label rendered above the color picker (same field ergonomics as TextField).
    static void ColorPicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, ColorFormat format = Hex, bool? showAlpha = null, IReadOnlyList<string>? presets = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onValueCommit = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Horizontal alignment for a content grid or data table column.
  enum ColumnAlign
    // Align content to the left.
    Left
    // Align content to the center.
    Center
    // Align content to the right.
    Right
  // Event returned from a contact picker action with the selected contacts.
  sealed record ContactsActionEvent : ActionEvent
    ctor(bool Success, IReadOnlyList<ClientContact>? Contacts)
    IReadOnlyList<ClientContact>? Contacts { get; init; }
  // Extension methods for container components.
  static class ContainerExtensions
    // Generic container element.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the user clicks the container. Accepts sync (() => …) and async (async () => …) lambdas alike. A clickable Box carries button semantics automatically — role="button", tabIndex=0 and Enter/Space activation — so it is reachable by keyboard, by assistive tech and by the app validator. Override either prop through props (e.g. ["role"] = "listitem"), and give an icon-only Box an ["aria-label"], since a button with no text content has no accessible name.
    // content: Builder function for rendering child elements within this component.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Box(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Box — positional (style, children) overload. Models reach for view.Box([style], v => {...}) with the lambda as the 2nd positional; without this overload it tries to bind to styleId (string?) and trips CS1660. The lambda parameter is named children (not content) so existing callers that use content: by name unambiguously match the original.
    static void Box(this UIView view, string[]? style, Action<UIView> children)
    // Container with vertical flexbox layout (flex-col).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Column(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Column — positional (style, children) overload.
    static void Column(this UIView view, string[]? style, Action<UIView> children)
    // Container with flexbox layout enabled.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Flex(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Flex — positional (style, children) overload (see Box).
    static void Flex(this UIView view, string[]? style, Action<UIView> children)
    // Container with CSS grid layout enabled.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Grid(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Grid — positional (style, children) overload.
    static void Grid(this UIView view, string[]? style, Action<UIView> children)
    // Absolutely positioned layer within a Stack container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Layer(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Layer — positional (style, children) overload (see Box).
    static void Layer(this UIView view, string[]? style, Action<UIView> children)
    // Container with horizontal flexbox layout (flex-row).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Row(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Row — positional (style, children) overload (see Box).
    static void Row(this UIView view, string[]? style, Action<UIView> children)
    // Loading spinner — an animated circular indicator for async/pending states. A typed convenience over the spin utility classes (equivalent to a div with the Theming.Icon.Spinner style): render it while waiting on data, e.g. if (_loading.Value) { view.Spinner(); }. Override colour/size via the style array; the default tracks the theme's muted foreground.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes appended to the base spinner styling (e.g. a colour or margin).
    // size: Spinner diameter — Sm, Md (default), or Lg.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Spinner(this UIView view, string[]? style = null, SpinnerSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Container for layering children on top of each other. Use with Layer components as children.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. These layout containers ALWAYS prepend a fixed base class (e.g. flex flex-col for Column) that concatenates with — and cannot be removed or replaced by — the classes you pass.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for rendering child elements within this component.
    static void Stack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Stack — positional (style, children) overload (see Box).
    static void Stack(this UIView view, string[]? style, Action<UIView> children)
  // Defines a column in a content grid including optional header, width, flex, and alignment.
  record ContentGridColumn
    ctor(string? Header, string? Width = null, int Flex = 0, ColumnAlign Align = Left)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string? Header { get; init; }
    string? Width { get; init; }
  // Extension methods for CSS grid-based content layout.
  static class ContentGridExtensions
    // Renders a CSS grid layout with configurable columns, optional headers, and child content.
    static void ContentGrid(this UIView view, ContentGridColumn[] columns, Action<UIView>? content = null, string[]? style = null, string[]? headerStyle = null, string? key = null)
  // Options for copying text to the clipboard.
  sealed record CopyToClipboardActionOptions : ActionOptions
    ctor()
    // The text to copy.
    required string Text { get; init; }
  // Extension methods for core UI components including buttons, toggles, text inputs, dialogs, and typography.
  static class CoreExtensions
    // Button that triggers a client-side action (e.g., clipboard, download). Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // action: The type of action to perform.
    // text: Text displayed on the button. When content is provided, becomes aria-label for accessibility.
    // options: Configuration options for the action.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onActionComplete: Invoked when the action completes. The parameter contains action result details.
    // content: Builder function for rendering child elements within this component. When provided together with text, enables icon mode where text becomes aria-label.
    static void ActionButton(this UIView view, string[]? style = null, ActionKind action = Unknown, string? text = null, ActionOptions? options = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<ActionEvent, Task>? onActionComplete = null, Action<UIView>? content = null)
    // Clickable button that triggers an action. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // text: Text displayed on the button. When content is provided, becomes aria-label for accessibility.
    // disabled: When true, prevents user interaction with this component.
    // href: URL to navigate to when clicked. Renders as an anchor element when specified.
    // type: Button type attribute (e.g., "submit", "button", "reset").
    // target: Link target (e.g., "_blank" for new tab). Only applies when href is set.
    // rel: Link relationship (e.g., "noopener noreferrer"). Only applies when href is set.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the user clicks the button.
    // icon: Optional Lucide icon name (e.g. "arrow-right", "refresh-cw"). When set, renders the icon alongside the text using a horizontal flex layout. Use iconPosition to switch sides. For full custom icon layouts use content instead.
    // iconPosition: Align.Start (default) puts the icon before the text; Align.End puts it after. Ignored when icon is null.
    // content: Builder function for rendering child elements within this component. When provided together with text, enables icon mode where text becomes aria-label.
    // tooltip: Hover name for the button, rendered with the themed Tooltip. This is all an icon-only button needs: the tooltip also becomes the accessible name when nothing else names the control, so there is no aria-label to write by hand. An explicit text or props["aria-label"] still wins. Do NOT reach for a title prop instead — that is the browser's own unstyleable tooltip.
    // tooltipRootStyle: Styles for the tooltip's wrapper, which is the element that sits in the parent's layout — so responsive and positioning classes (hidden lg:inline-flex, absolute top-2 right-2) belong here, not on the button. Defaults to inline-flex shrink-0, which is what an icon button in a flex row wants.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Button(this UIView view, string[]? style = null, string? text = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null)
    // Button — positional-text-first overload accepting the label as the first argument.
    static void Button(this UIView view, string buttonText, string[]? style = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null, string? tooltip = null, string[]? tooltipRootStyle = null, string? ariaLabel = null)
    // Semantic heading element for titles and section headers.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // text: Heading text to display (alternative to content).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Heading(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Heading — positional-text-first overload, same rationale as the matching Text overload: view.Heading("Settings", style: [Text.H2]) is the shape models reach for. Parameter is named headingText to avoid ambiguity with callers using text: by name.
    static void Heading(this UIView view, string headingText, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Renders an icon from an icon library.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // name: Name of the icon in the icon library.
    // size: Icon size, the way Spinner takes one (size: IconSize.Lg). Merged as the icon's base sizing, so a w-*/h-* class in style — including the equivalent Theming.Icon.Xs..Xl token — still wins. Omit it to leave sizing entirely to style.
    // library: Icon library to use. Defaults to the view's default icon library.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering custom icon content (alternative to name).
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Icon(this UIView view, string[]? style = null, string? name = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Icon — positional-name-first overload. Same rationale as the matching Text overload: view.Icon("check", style: [Icon.Sm]) is the shape models reach for. Parameter is named iconName to avoid ambiguity with callers using name: by name.
    static void Icon(this UIView view, string iconName, string[]? style = null, IconSize? size = null, string? library = null, bool? filled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Inline anchor link — sugar for a `Button` styled like a hyperlink with an `href`. Mirrors HTML anchor semantics. By default opens in the same tab; pass target: "_blank" to open in a new tab (we automatically add `rel="noopener noreferrer"` for `_blank` if no other `rel` is provided). Generated code naturally reaches for `view.Link(text:, href:)`; this gives it the canonical shape rather than forcing every link into `view.Button(href:, …)`.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. Defaults to the theme's `Button.Link` styling — call-sites can override by passing any style array.
    // text: Link text shown to the user (the anchor body).
    // href: URL the link points to. Required. A same-origin path is routed in place: the client turns the click into a path change (surfacing as app.Navigation.PathChangedAsync) instead of reloading the document, so the connection survives it and the link needs no onClick to navigate.
    // target: Anchor target — pass `"_blank"` for new-tab. Default: same tab.
    // rel: Anchor rel attribute. When `target == "_blank"` and rel is null, defaults to `"noopener noreferrer"`. Pass `"external"` to force a full document load for a same-origin link.
    // onClick: Optional click handler (fires alongside navigation). For a side effect only — analytics, closing a menu — never to restate the destination `href` already names. Most use cases don't need this.
    // icon: Optional Lucide icon name rendered alongside the link text.
    // iconPosition: Align.Start (default) or Align.End — which side the icon sits on.
    // styleId: CSS class name applied directly. For exceptional cases.
    // key: Stable diff key.
    // props: HTML attributes forwarded to the anchor.
    // content: Custom child content; if provided, `text` becomes aria-label.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Link(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Link — positional-text-first overload. Same rationale as the matching Text overload: view.Link("Docs", href: "https://…") is the shape models reach for. Parameter is named linkText to avoid ambiguity with callers using text: by name.
    static void Link(this UIView view, string linkText, string[]? style = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, string? ariaLabel = null)
    // Renders markdown content with formatting support.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling. With no array the body renders with Markdown.Default — heading scale, list markers, table rules, blockquote bar and a self-scrolling fenced-code box. Pass "default" as the first class to keep those and add your own on top; any other array replaces them.
    // content: Markdown text to render.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void Markdown(this UIView view, string[]? style = null, string? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Markdown — positional-content-first overload: view.Markdown("# Hello"). Parameter is named markdownContent to avoid ambiguity with callers using content: by name.
    static void Markdown(this UIView view, string markdownContent, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Text element for displaying content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // text: Text content to display.
    // href: URL to navigate to when clicked. Renders as an anchor element when specified.
    // target: Link target (e.g., "_blank" for new tab). Only applies when href is set.
    // rel: Link relationship (e.g., "noopener noreferrer"). Only applies when href is set.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Text(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Text element — positional-text-first overload accepting the content as the first argument.
    static void Text(this UIView view, string textContent, string[]? style = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Single toggle button.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled pressed state of the toggle.
    // defaultValue: The default pressed state when initially rendered. Use when not controlling the state.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the pressed state changes. The parameter is true when pressed, false when released.
    // content: Builder function for rendering child elements within this component.
    // label: Optional trailing text label. Wraps the toggle and the text in a <label>, so clicking the text toggles the control and the text is the toggle's accessible name.
    static void Toggle(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null)
    // Item within a toggle group.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The unique value for this toggle item within the group.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void ToggleGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggle group with multiple selection.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled list of selected toggle item values.
    // defaultValue: The default list of values when initially rendered. Use when not controlling the state.
    // rovingFocus: When true, enables roving tabindex for keyboard navigation between items.
    // loop: When true, keyboard navigation loops from last item to first, and vice versa.
    // orientation: The orientation of the toggle group for keyboard navigation.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the selection. The parameter contains the new list of selected values.
    // content: Builder function for rendering child elements within this component.
    static void ToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggle group with single selection.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled value of the selected toggle item.
    // defaultValue: The default value when initially rendered. Use when not controlling the state.
    // rovingFocus: When true, enables roving tabindex for keyboard navigation between items.
    // loop: When true, keyboard navigation loops from last item to first, and vice versa.
    // orientation: The orientation of the toggle group for keyboard navigation.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // content: Builder function for rendering child elements within this component.
    static void ToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
  // Represents the text direction for DirectionProvider.
  enum Dir
    Ltr
    Rtl
  // Extension methods for Accordion and Collapsible components.
  static class DisclosureExtensions
    // Content for an accordion item, collapsed or expanded.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AccordionContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Wraps an AccordionTrigger.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AccordionHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for an accordion item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the accordion item.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AccordionItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Accordion with multiple items open at a time.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the open accordion items.
    // defaultValue: Initial value for uncontrolled mode.
    // orientation: Layout orientation.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value.
    // content: Builder function for rendering child elements within this component.
    static void AccordionMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Accordion with single item open at a time.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the open accordion item.
    // defaultValue: Initial value for uncontrolled mode.
    // collapsible: Whether the open item can be collapsed.
    // orientation: Layout orientation.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value.
    // content: Builder function for rendering child elements within this component.
    static void AccordionSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? collapsible = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggles the collapsed state of an accordion item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AccordionTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Expandable/collapsible container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when open state changes.
    // content: Builder function for rendering child elements within this component.
    static void Collapsible(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Content that is shown or hidden.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void CollapsibleContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggles the collapsed state.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void CollapsibleTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Options for downloading a file to the client.
  sealed record DownloadFileActionOptions : ActionOptions
    ctor()
    // Binary data to download. When set, Url is auto-generated as a data URL.
    byte[]? Data { get; init; }
    // Suggested filename for the downloaded file.
    string? Filename { get; init; }
    // MIME type for binary data (e.g. "image/png"). Optional — defaults to "application/octet-stream" when Data is set without a MIME type.
    string? MimeType { get; init; }
    // URL to download. Can be a regular URL or a data URL. If Data is provided, this is auto-generated from the binary data using MimeType, falling back to "application/octet-stream" when MimeType is unset so the download still fires.
    string Url { get; init; }
  // Extension methods for drag and drop components.
  static class DragAndDropExtensions
    // Root context for drag and drop operations.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // collisionDetection: Algorithm for detecting which droppable is under the dragged item.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onDragStart: Invoked when a drag operation begins.
    // onDragMove: Invoked as the dragged item moves.
    // onDragOver: Invoked when the dragged item moves over a droppable.
    // onDragEnd: Invoked when the drag operation ends (dropped or cancelled).
    // onDragCancel: Invoked when the drag operation is cancelled.
    // activationDistance: Pixels of pointer movement required before a drag activates. When set, a pointerdown that doesn't move past this threshold is delivered as a normal click instead of starting a drag — so an inner Button.onClick fires. Leave null for the default behaviour (drag activates immediately).
    // content: Builder function for rendering child elements within this component.
    static void DndContext(this UIView view, string[]? style = null, CollisionDetection collisionDetection = ClosestCenter, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<DragStartArgs, Task>? onDragStart = null, Func<DragMoveArgs, Task>? onDragMove = null, Func<DragOverArgs, Task>? onDragOver = null, Func<DragEndArgs, Task>? onDragEnd = null, Func<Task>? onDragCancel = null, int? activationDistance = null, Action<UIView>? content = null)
    // Overlay shown while dragging.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // dropAnimation: When true, animates the drop action.
    // activeDragId: The ID of the currently dragged item. When set, the overlay only renders its content after the server has sent content matching this drag ID, preventing stale content from a previous drag.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering the drag overlay content.
    static void DragOverlay(this UIView view, string[]? style = null, bool? dropAnimation = true, string? activeDragId = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Element that can be dragged.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // id: Unique identifier for this draggable element.
    // disabled: When true, prevents user interaction with this component.
    // hideOnDrag: When true, hides the original element during drag. Use with DragOverlay.
    // data: Custom data attached to this draggable, available in drag event arguments.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Draggable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, bool? hideOnDrag = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Drop target area.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // id: Unique identifier for this droppable area.
    // disabled: When true, prevents user interaction with this component.
    // data: Custom data attached to this droppable, available in drag event arguments.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Droppable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Context for sortable list operations.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // items: List of item identifiers in the current sort order.
    // strategy: Sorting strategy (VerticalList or HorizontalList).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SortableContext(this UIView view, string[]? style = null, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Drag handle for a SortableItem. When a SortableHandle descendant is present, only pointerdown on the handle starts a drag; the rest of the item remains free for inner clickable elements like buttons. Place inside a SortableItem (or a SortableList itemContent). Outside a SortableItem the handle renders as a plain container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SortableHandle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Sortable item within a SortableContext.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // id: Unique identifier for this sortable item.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SortableItem(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Style slots: listStyle (container holding all sortable items), itemStyle (each item).
    // view: The UIView to render into.
    // items: List of item identifiers in the current sort order.
    // strategy: Sorting strategy (VerticalList or HorizontalList).
    // collisionDetection: Algorithm for detecting which droppable is under the dragged item.
    // onReorder: Invoked when items are reordered. The parameter contains the new order.
    // onDragStart: Invoked when a drag operation begins.
    // itemContent: Builder function for rendering each item's content. Receives the item id.
    // listStyle: Style classes for the container holding all sortable items.
    // itemStyle: Style classes applied to each sortable item.
    // activationDistance: Pixels of pointer movement required before a drag activates. When set, a pointerdown that doesn't move past this threshold is delivered as a normal click instead of starting a drag — so an inner Button.onClick fires. Leave null for the default behaviour (drag activates immediately).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void SortableList(this UIView view, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, CollisionDetection collisionDetection = ClosestCenter, Func<SortableReorderArgs, Task>? onReorder = null, Func<DragStartArgs, Task>? onDragStart = null, Action<UIView, string>? itemContent = null, string[]? listStyle = null, string[]? itemStyle = null, int? activationDistance = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Event args for drag cancel in @dnd-kit.
  sealed record DragCancelArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for drag end in @dnd-kit.
  sealed record DragEndArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag move in @dnd-kit.
  sealed record DragMoveArgs
    ctor(string ActiveId, double DeltaX, double DeltaY)
    string ActiveId { get; init; }
    double DeltaX { get; init; }
    double DeltaY { get; init; }
  // Event args for drag over in @dnd-kit.
  sealed record DragOverArgs
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag start in @dnd-kit.
  sealed record DragStartArgs
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for escape key down events on overlays.
  sealed record EscapeKeyDownArgs
    ctor()
  // Backed by a ClientReactive<T>: each client expands and collapses independently, and reads during UI rendering are dependency-tracked, so the tree re-renders automatically. Access it where a client scope is active (UI render or event handlers).
  sealed class ExpandedSet
    // Create the set, optionally pre-expanding the given node ids for every client.
    // expandedIds: Node ids that start expanded.
    ctor(params string[] expandedIds)
    // Collapse every node for the calling client.
    void Clear()
    // Collapse the node for the calling client.
    void Collapse(string id)
    // Expand the node for the calling client.
    void Expand(string id)
    // Whether the node is expanded for the calling client (reactive read).
    bool IsExpanded(string id)
    // Set the node's expanded state for the calling client.
    void Set(string id, bool expanded)
    // Toggle the node's expanded state for the calling client.
    void Toggle(string id)
  // Hint used by FeedSlide to preload the slide's primary media asset.
  enum FeedMediaKind
    // Slide has no external media to preload.
    None
    // Preload an image URL with an off-DOM Image.
    Image
    // Preload a video URL's metadata (not full payload) via a hidden <video preload="metadata">.
    Video
    // Preload the full video payload. Use sparingly — costs bandwidth.
    VideoFull
  // Performance model: • Native CSS scroll-snap drives the snap — no JS scroll loop. • Active slide is detected with IntersectionObserver, not scroll events. • Only slides inside [active - preloadBehind, active + preloadAhead] render their content; slides outside the window render as fixed-height spacers that preserve scroll position. • Media declared on FeedSlide is warmed with off-DOM Image/<video> elements as soon as a slide enters the preload window. • Autoplay is gated on the active slide only — neighbour videos are paused.
  static class FeedScrollerExtensions
    // Renders a TikTok-style vertical feed: each slide occupies the viewport and snaps into place.
    // view: The UIView to render into.
    // slides: Collection of slides rendered in order. Pass an async-growing list and use onScrollNearEnd to page more in.
    // activeIndex: Controlled zero-based index of the active (centered) slide.
    // defaultActiveIndex: Initial active slide for uncontrolled mode.
    // preloadAhead: Number of slides after the active one to mount and warm media for. Defaults to 2.
    // preloadBehind: Number of slides before the active one to keep mounted. Defaults to 1.
    // autoPlay: When true, videos on the active slide play automatically. Defaults to true.
    // muted: Controlled mute state applied to all media. Defaults to true (required for browser autoplay).
    // loop: When true, passing the last slide wraps to the first.
    // scrollEndThreshold: How many slides from the end before onScrollNearEnd fires. Defaults to 2.
    // style: Style for the outermost viewport container. Use FeedScroller.Root.
    // slideStyle: Style applied to every slide. Use FeedScroller.Slide.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onActiveChange: Invoked when the active slide changes. The parameter is the zero-based active slide index as an int (the same value as activeIndex).
    // onScrollNearEnd: Invoked when the user scrolls within scrollEndThreshold slides of the end — use this to fetch the next page of content. The parameter is the zero-based active slide index as an int (the same value as activeIndex).
    // onMuteChange: Invoked when the user toggles mute on an in-slide control.
    static void FeedScroller(this UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[]? style = null, string[]? slideStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<int, Task>? onActiveChange = null, Func<int, Task>? onScrollNearEnd = null, Func<bool, Task>? onMuteChange = null)
    // A single slide inside a FeedScroller. Use when rendering slides manually rather than via the FeedSlide declarative API.
    // view: The UIView to render into.
    // index: Zero-based index of this slide.
    // style: Style classes for the slide container.
    // mediaKind: Kind of media to preload for this slide.
    // mediaUrl: URL of the media asset.
    // mediaPoster: Optional poster image URL for video slides.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this slide.
    static void FeedSlide(this UIView view, int index, string[]? style = null, FeedMediaKind mediaKind = None, string? mediaUrl = null, string? mediaPoster = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // A single slide in a FeedScrollerExtensions.FeedScroller.
  sealed record FeedSlide
    // Content: Builder invoked to render the slide. Only slides inside the render window are realized.
    // Key: Stable key used for diffing and preload identity. Defaults to slide index.
    // MediaKind: Kind of media the slide needs preloaded.
    // MediaUrl: URL of the media asset matching MediaKind.
    // MediaPoster: Optional poster image URL for video slides.
    ctor(Action<UIView> Content, string? Key = null, FeedMediaKind MediaKind = None, string? MediaUrl = null, string? MediaPoster = null)
    // Builder invoked to render the slide. Only slides inside the render window are realized.
    Action<UIView> Content { get; init; }
    // Stable key used for diffing and preload identity. Defaults to slide index.
    string? Key { get; init; }
    // Kind of media the slide needs preloaded.
    FeedMediaKind MediaKind { get; init; }
    // Optional poster image URL for video slides.
    string? MediaPoster { get; init; }
    // URL of the media asset matching MediaKind.
    string? MediaUrl { get; init; }
  // Extension methods for file picker components. Unlike FileUploadExtensions.FileUpload, a FilePicker only opens the native file picker and reports selected file metadata to the server — it does not transfer bytes. The picked File handles are cached on the client and uploaded later by a FileUploadExtensions.FileUpload rendered with a matching seedSelectionIds prop.
  static class FilePickerExtensions
    // Only reports picked-file metadata to the server — the bytes stay on the client and are not uploaded until a FileUploadExtensions.FileUpload with a matching seedSelectionIds prop is mounted. Without an onValidationError handler, client-side rejections (e.g. over maxFileSize) are silent.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // multiple: When true, allows multiple file selection.
    // maxFileSize: Maximum file size in bytes (enforced client-side before emitting selection).
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    // props: Additional properties passed directly to the underlying component.
    // onFileSelected: Invoked once per picked file, with client-generated SelectionId and metadata.
    // onValidationError: Invoked when a picked file is rejected client-side (e.g. exceeds maxFileSize). Surface Reason to the user — without this the rejection is silent and looks like "click did nothing".
    // content: Builder function for custom content rendered inside the picker surface.
    static void FilePicker(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FilePickerSelectedArgs, Task>? onFileSelected = null, Func<FilePickerValidationErrorArgs, Task>? onValidationError = null, Action<UIView>? content = null)
  // Metadata for a file chosen in a FilePickerExtensions.FilePicker. The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
  sealed record FilePickerSelectedArgs
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  // Reported when client-side validation rejects a picked file (e.g. file too large for maxFileSize). Host UIs should surface Reason to the user — without a handler the rejection is silent and the user just sees "nothing happened" after clicking the picker.
  sealed record FilePickerValidationErrorArgs
    ctor(string FileName, string MimeType, long Size, string Reason)
    string FileName { get; init; }
    string MimeType { get; init; }
    string Reason { get; init; }
    long Size { get; init; }
  // Extension methods for file upload components.
  static class FileUploadExtensions
    // File upload component with explicit upload area, button click, drag-drop, and paste support.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]).
    // multiple: When true, allows multiple file selection.
    // maxFileSize: Maximum file size in bytes.
    // disabled: When true, prevents user interaction with this component.
    // allowPaste: When true, enables paste support for file upload.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onUploadPreStart: First accept/reject hook: invoked when a file upload is initiated, before the client hashes or transfers anything. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system. Receives a Cancel delegate for user-initiated cancellation.
    // onUploadStart: Second accept/reject hook: invoked after onUploadPreStart once the file hash is computed, before any data chunks arrive. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system.
    // onUploadProgress: Invoked with upload progress updates.
    // onUploadComplete: Invoked when a file upload completes successfully.
    // onUploadError: Invoked when a file upload fails, is cancelled, or times out.
    // onChunkReceived: Invoked when a file chunk is received during chunked upload.
    // seedSelectionIds: When set, on first mount the client looks these ids up in the shared pendingSelections stash (populated by a prior FilePickerExtensions.FilePicker) and uploads those File handles through the normal upload pipeline. Each SelectionId is reused verbatim as the UploadId.
    // content: Builder function for custom content inside the upload area.
    static void FileUpload(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string? capture = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, string[]? seedSelectionIds = null, Action<UIView>? content = null)
    // Style slots: zoneStyle (drop-zone container), activeStyle (applied while a file is dragged over the zone). The MIME filter is the NAMED accept: parameter — a leading positional array is always the zone style, never the filter.
    // view: The UIView to render into.
    // style: Style classes for the drop zone container (the ergonomic first-positional; alias of zoneStyle). The MIME filter is the NAMED accept: parameter — a leading positional array is always the zone style, never the filter.
    // multiple: When true, allows multiple file selection.
    // maxFileSize: Maximum file size in bytes.
    // disabled: When true, prevents user interaction with this component.
    // allowPaste: When true, enables paste support for file upload.
    // accept: Accepted MIME types or extensions (e.g., ["image/*", ".pdf"]). Pass by name.
    // onUploadPreStart: First accept/reject hook: invoked when a file upload is initiated, before the client hashes or transfers anything. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system. Receives a Cancel delegate for user-initiated cancellation.
    // onUploadStart: Second accept/reject hook: invoked after onUploadPreStart once the file hash is computed, before any data chunks arrive. Return a FileUploadResult (or `true`/`false`); set AssetUri to write directly to the Asset system.
    // onUploadProgress: Invoked with upload progress updates.
    // onUploadComplete: Invoked when a file upload completes successfully.
    // onUploadError: Invoked when a file upload fails, is cancelled, or times out.
    // onChunkReceived: Invoked when a file chunk is received during chunked upload.
    // onDragActiveChange: Invoked when drag active state changes. The parameter is true when drag enters, false when it leaves.
    // content: Builder function for rendering child elements to wrap with file upload capability.
    // zoneStyle: Style classes for the drop zone container.
    // activeStyle: Style classes applied when drag is active over the zone.
    // activeStyleId: CSS class name for the active drag state.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // seedSelectionIds: When set, on mount the client looks these ids up in the shared pendingSelections stash (populated by a prior FilePickerExtensions.FilePicker) and uploads those File handles through the normal upload pipeline. Each SelectionId is reused verbatim as the UploadId.
    // props: Additional properties passed directly to the underlying component.
    static void FileUploadZone(this UIView view, string[]? style = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string[]? accept = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, Func<bool, Task>? onDragActiveChange = null, Action<UIView>? content = null, string[]? zoneStyle = null, string[]? activeStyle = null, string? activeStyleId = null, string? styleId = null, string? key = null, string[]? seedSelectionIds = null, IReadOnlyDictionary<string, object>? props = null)
  // Extension methods for focus hint management.
  static class FocusHintExtensions
    // Requests focus attention for a UI element, typically for accessibility announcements.
    // view: The UIView to render into.
    // props: Configuration for the focus hint behavior.
    // key: Unique identifier for this focus hint request.
    // targetViewId: View ID to receive focus. Defaults to the current view.
    static void FocusHint(this UIView view, FocusHintProps props, string? key = null, Guid? targetViewId = null)
  // Configuration for a focus hint request including priority, ranking, and cooldown behavior.
  sealed record FocusHintProps
    ctor()
    // Minimum time between repeated focus hints for the same element.
    TimeSpan? Cooldown { get; init; }
    // When true, only moves focus without making an accessibility announcement.
    bool FocusOnly { get; init; }
    // Announcement priority level. Polite waits for idle; Assertive interrupts immediately.
    FocusPriority Priority { get; init; }
    // Numeric ranking to resolve conflicts when multiple hints compete.
    int Ranking { get; init; }
  // Event args for focus outside events on overlays.
  sealed record FocusOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Priority level for focus hint announcements, matching ARIA live region politeness.
  enum FocusPriority
    // Waits for the user agent to finish current tasks before announcing.
    Polite
    // Interrupts immediately to announce the change.
    Assertive
  // Extension methods for Form, Checkbox, RadioGroup, Switch, Slider, and Label components.
  static class FormExtensions
    // Checkbox control with simple boolean state. For tri-state support (indeterminate), use TriStateCheckbox.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled checked state of the checkbox.
    // defaultValue: The default checked state when initially rendered. Use when not controlling the state.
    // required: When true, indicates the checkbox must be checked before the form can be submitted.
    // disabled: When true, prevents user interaction with this component.
    // name: The name of the checkbox for form submission.
    // formValue: The HTML form value submitted when checked (the value attribute).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the checked state changes. The parameter is true when checked, false when unchecked.
    // content: Builder function for rendering child elements within this component.
    // label: Optional trailing text label — the idiomatic checkbox row. Wraps the checkbox and the text in a <label>, so clicking the text toggles the control and the text is the checkbox's accessible name. Prefer this to placing your own Text beside a bare Checkbox, which looks the same but associates nothing.
    // bind: Two-way binds the checkbox to a Reactive<T> in one call — reads bind.Value for the controlled state and writes it back on every toggle. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Checkbox(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    // Visual indicator for the checkbox state.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, forces the indicator to render even when the checkbox is unchecked.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void CheckboxIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Form container with validation support.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClearServerErrors: Invoked when server-side validation errors should be cleared.
    // content: Builder function for rendering child elements within this component.
    static void Form(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClearServerErrors = null, Action<UIView>? content = null)
    // Wraps the input control.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormControl(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for a form field with label and validation.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // name: The name of the form field, used for validation and form submission.
    // serverInvalid: When true, indicates the field has a server-side validation error.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormField(this UIView view, string[]? style = null, string? name = null, bool? serverInvalid = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Label for a form field.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormLabel(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Validation message for a form field.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // match: The validation condition that must be met for this message to display.
    // forceMatch: When true, forces the message to display regardless of the match condition.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormMessage(this UIView view, string[]? style = null, FormMessageMatch? match = null, bool? forceMatch = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Submit button for the form.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void FormSubmit(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Accessible label for form controls.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // htmlFor: The id of the element this label is associated with.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Label(this UIView view, string[]? style = null, string? htmlFor = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for radio buttons.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled value of the selected radio item.
    // defaultValue: The default value when initially rendered. Use when not controlling the state.
    // required: When true, indicates a selection must be made before the form can be submitted.
    // disabled: When true, prevents user interaction with this component.
    // loop: When true, keyboard navigation loops from last item to first, and vice versa.
    // orientation: The orientation of the radio group for keyboard navigation.
    // name: The name of the radio group for form submission.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // content: Builder function for rendering child elements within this component.
    // label: Optional group-level label rendered above the radio group (same field ergonomics as TextField).
    // bind: Two-way binds the group to a Reactive<T> in one call — reads bind.Value for the selected value and writes it back on every selection. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    static void RadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<string>? bind = null)
    // Visual indicator for the selected radio.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, forces the indicator to render even when the radio is not selected.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void RadioGroupIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual radio button.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The unique value for this radio item within the group.
    // disabled: When true, prevents user interaction with this component.
    // required: When true, indicates this radio item must be selected before the form can be submitted.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void RadioGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, bool? required = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Range slider control.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled value of the slider as a list of thumb positions.
    // defaultValue: The default value when initially rendered. Use when not controlling the state.
    // min: The minimum value for the slider.
    // max: The maximum value for the slider.
    // step: The stepping interval between selectable values.
    // minStepsBetweenThumbs: The minimum number of steps between thumbs in a multi-thumb slider.
    // orientation: The orientation of the slider.
    // disabled: When true, prevents user interaction with this component.
    // inverted: When true, inverts the slider direction.
    // name: The name of the slider for form submission.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the value changes during dragging. The parameter contains the current thumb positions.
    // onValueCommit: Invoked when the user finishes dragging. The parameter contains the final thumb positions.
    // content: Builder function for rendering the slider's track and thumbs. Note: a read-only slider (a controlled value: with no write-back handler) is still functionally inert — the root gates all writes — but the default content carries the aria-readonly signal on its thumb, which the slider's root cannot legally hold. Custom content that replaces the thumbs should put aria-readonly="true" on each thumb to keep the a11y state, or omit content: and style the default thumbs via the theme tokens.
    // label: Optional field label rendered above the slider (same field ergonomics as TextField). It also becomes the accessible name of the slider's thumbs, which is where role="slider" lives — a name left on the root names nothing. Thumbs on a multi-thumb range are numbered from it.
    // bind: Two-way binds a single-thumb slider to a Reactive<T> in one call — reads bind.Value for the thumb position and writes it back as the user drags. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back. For multi-thumb ranges use the value: list form.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Slider(this UIView view, string[]? style = null, IReadOnlyList<double>? value = null, IReadOnlyList<double>? defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<double>, Task>? onValueChange = null, Func<IReadOnlyList<double>, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null, Reactive<double>? bind = null, string? ariaLabel = null)
    // Single-thumb slider with a scalar value — the common case. Sugar over the list form so callers write Slider(value: 50, onValueChange: async v => …) without the one-element-list dance. Use the list form for multi-thumb ranges.
    static void Slider(this UIView view, double value, string[]? style = null, double? min = null, double? max = null, double? step = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onValueChange = null, Func<double, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null)
    // Filled range portion of the slider.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void SliderRange(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Draggable thumb on the slider.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void SliderThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Track for the slider.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SliderTrack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggle switch control.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled checked state of the switch.
    // defaultValue: The default checked state when initially rendered. Use when not controlling the state.
    // required: When true, indicates the switch must be checked before the form can be submitted.
    // disabled: When true, prevents user interaction with this component.
    // name: The name of the switch for form submission.
    // formValue: The HTML form value submitted when checked (the value attribute).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the checked state changes. The parameter is true when checked, false when unchecked.
    // content: Builder function for rendering child elements within this component.
    // label: Optional trailing text label. Wraps the switch and the text in a <label>, so clicking the text toggles the control and the text is the switch's accessible name — a switch has no name of its own, so this or an aria-label is what keeps it from being announced as an unlabelled control.
    // bind: Two-way binds the switch to a Reactive<T> in one call — reads bind.Value for the controlled state and writes it back on every toggle. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Switch(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    // The thumb that moves when the switch is toggled.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void SwitchThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Checkbox control with tri-state support (checked, unchecked, indeterminate).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: The controlled tri-state value: Checked, Unchecked, or Indeterminate.
    // defaultValue: The default tri-state value when initially rendered. Use when not controlling the state.
    // required: When true, indicates the checkbox must be checked before the form can be submitted.
    // disabled: When true, prevents user interaction with this component.
    // name: The name of the checkbox for form submission.
    // formValue: The HTML form value submitted when checked (the value attribute).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the checked state changes. The parameter contains the new CheckedState value.
    // content: Builder function for rendering child elements within this component.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void TriStateCheckbox(this UIView view, string[]? style = null, CheckedState? value = null, CheckedState? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onValueChange = null, Action<UIView>? content = null, string? ariaLabel = null)
  // Represents form validation message types matching browser constraint validation.
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
  // Hour display format for TimePickerExtensions.TimePicker.
  enum HourFormat
    // 24-hour display, e.g. 13:45.
    Hour24
    // 12-hour display with AM/PM, e.g. 1:45 PM.
    Hour12
  // Size of an Icon — the size: form of the Theming.Icon.Xs..Xl tokens, so an icon sizes the same way a Spinner does (size: IconSize.Lg). The style-array form (view.Icon([Icon.Lg], name: "check")) stays valid and, being a caller class, still wins over size: when both are given.
  enum IconSize
    Xs
    Sm
    Md
    Lg
    Xl
  // Event returned from an image capture action with the captured image data.
  sealed record ImageCaptureActionEvent : ActionEvent
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string? Data { get; init; }
    int Height { get; init; }
    string? Mime { get; init; }
    int Width { get; init; }
  // Extension methods for image and avatar components.
  static class ImageExtensions
    // Avatar container with image and fallback.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Avatar(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Fallback content shown when image fails to load.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // delayMs: Delay in milliseconds before showing the fallback.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AvatarFallback(this UIView view, string[]? style = null, int? delayMs = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Image element for the avatar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // src: URL of the image to display.
    // alt: Alternative text description for accessibility.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onLoadingStatusChange: Invoked when the image loading status changes.
    static void AvatarImage(this UIView view, string[]? style = null, string? src = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onLoadingStatusChange = null)
    // Image element.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // src: URL or path to the image source.
    // assetUri: Asset URI to resolve the image source from. Takes precedence over src.
    // alt: Alternative text description for accessibility.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, string? src = null, AssetUri? assetUri = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
    // Image element with binary data payload.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // data: Binary image data.
    // mimeType: MIME type of the image (e.g., "image/png", "image/jpeg").
    // alt: Alternative text description for accessibility.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, byte[]? data = null, string? mimeType = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
  // Extension methods for input components (TextField, TextArea, OTP, Password).
  static class InputExtensions
    // One-time password input field.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value of the OTP input. A controlled value with no onValueChange renders the field read-only, since entered digits would have nowhere to go.
    // maxLength: Maximum number of characters allowed.
    // autoSubmit: When true, automatically triggers onAutoSubmit when all characters are entered.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // onAutoSubmit: Invoked when all characters are entered and autoSubmit is enabled.
    // content: Builder function for rendering OtpFieldInput slots within this component.
    // label: Optional field label rendered above the OTP slots (same field ergonomics as TextField).
    static void OtpField(this UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string? label = null)
    // Individual input slot for OTP.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // index: Zero-based index of this slot in the OTP field.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void OtpFieldInput(this UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Password input with visibility toggle.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // visible: Controlled visibility state. When true, password is shown as plain text.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onVisibilityChange: Invoked when visibility changes. The parameter is true when visible, false when hidden.
    // content: Builder function for rendering PasswordToggleFieldInput and PasswordToggleFieldToggle within this component.
    // label: Optional field label rendered above the field (same field ergonomics as TextField).
    static void PasswordToggleField(this UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string? label = null)
    // Icon that changes based on visibility state.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // visibleIcon: Builder function for the icon shown when password is visible.
    // hiddenIcon: Builder function for the icon shown when password is hidden.
    static void PasswordToggleFieldIcon(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null)
    // The password input element.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // autoComplete: Browser autocomplete hint (e.g., "current-password", "new-password").
    // placeholder: Placeholder text shown when empty.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void PasswordToggleFieldInput(this UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Button to toggle password visibility.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering the toggle button content (typically an icon).
    static void PasswordToggleFieldToggle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Multi-line text input area.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value of the input. A controlled value with no write-back handler (no bind, no onValueChange, no onSubmit) renders the input read-only, since edits would have nowhere to go.
    // defaultValue: Initial value for uncontrolled mode.
    // placeholder: Placeholder text shown when empty.
    // disabled: When true, prevents user interaction with this component.
    // rows: Number of visible text rows.
    // autoResize: When true, the textarea grows to fit its content up to maxRows, then scrolls.
    // maxRows: Upper bound on visible rows when autoResize is true. Defaults to 6 if not specified.
    // submitOnEnter: When true, plain Enter submits and Shift+Enter inserts a newline. Default is false (Ctrl/Cmd+Enter submits, Enter inserts newline) — matches the platform default.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // onSubmit: Invoked when the user submits (e.g., Ctrl+Enter). The parameter contains the submitted value — prefer it over re-reading the bound reactive, which may lag the keystroke (onValueChange is a separate round-trip and is not guaranteed to land before onSubmit).
    // onSubmitWithContext: Invoked when the user submits, with additional context information.
    // clearOnSubmit: When true, clears the input value after submit. Defaults to true when onSubmit/onSubmitWithContext is set.
    // content: Builder function for rendering child elements within this component.
    // autoFocus: When true, the input takes keyboard focus as soon as it mounts — e.g. an inline add/edit form that appears on a click. Defaults to false.
    // label: Optional field label rendered above the textarea (same field ergonomics as TextField).
    // debounceMs: Throttles the onValueChange round-trip, in milliseconds.
    // bind: Two-way binds the textarea to a Reactive<T> in one call — reads bind.Value for the controlled value and writes bind.Value = v on every keystroke. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void TextArea(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, string? ariaLabel = null)
    // Controlled/read-only rule (shared by every input component — text, select, checkbox, calendar, color, OTP, …): passing a controlled value: with no write-back handler (bind:, onValueChange:, or onSubmit:) renders the field read-only, since edits would have nowhere to go. Pass bind: <reactive> to two-way bind a Reactive<T> in one call, or value: together with an onValueChange:/onSubmit: handler.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value of the input. A controlled value with no write-back handler (no bind, no onValueChange, no onSubmit) renders the input read-only, since edits would have nowhere to go.
    // defaultValue: Initial value for uncontrolled mode.
    // placeholder: Placeholder text shown when empty.
    // disabled: When true, prevents user interaction with this component.
    // type: Input type (e.g., "text", "email", "number", "password").
    // step: Step increment for number inputs.
    // min: Minimum value for number inputs.
    // max: Maximum value for number inputs.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // onSubmit: Invoked when the user presses Enter. The parameter contains the submitted value — prefer it over re-reading the bound reactive, which may lag the keystroke (onValueChange is a separate round-trip and is not guaranteed to land before onSubmit).
    // clearOnSubmit: When true, clears the input value after submit. Defaults to clearing only when an onSubmit handler is set (matching TextArea); a bound field with no onSubmit is not emptied on Enter. Pass true/false to override.
    // content: Builder function for rendering child elements within this component.
    // autoFocus: When true, the input takes keyboard focus as soon as it mounts — e.g. an inline add/edit form that appears on a click. Defaults to false.
    // label: Optional field label rendered above the input, wrapped together with it in a Column.
    // debounceMs: Throttles the onValueChange round-trip, in milliseconds.
    // bind: Two-way binds the field to a Reactive<T> in one call — reads bind.Value for the controlled value and writes bind.Value = v on every keystroke. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // multiline: MUI / Chakra / Material-style API: TextField(multiline: true, rows: 4). Parallax has a dedicated TextArea component, but the multi-line use case is so commonly expressed as "TextField with multiline=true" that accepting it here saves the caller from learning a different component name. Delegates to TextArea.
    // rows: Number of visible text rows; setting it implies multiline (delegates to TextArea).
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void TextField(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, bool? multiline = null, int? rows = null, string? ariaLabel = null)
  // Event args for interact outside events on overlays (combines pointer and focus).
  sealed record InteractOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // String constants for common keyboard key names, matching the browser KeyboardEvent.key specification. Use these with KeyboardExtensions.KeyboardListener for type-safe key filtering. Raw strings can also be used for uncommon keys not listed here.
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
  // Event args for keyboard events, matching the browser KeyboardEvent properties.
  sealed record KeyboardEventArgs
    ctor(string Key, string Code, bool AltKey, bool CtrlKey, bool MetaKey, bool ShiftKey, bool Repeat)
    bool AltKey { get; init; }
    string Code { get; init; }
    bool CtrlKey { get; init; }
    string Key { get; init; }
    bool MetaKey { get; init; }
    bool Repeat { get; init; }
    bool ShiftKey { get; init; }
  // Extension methods for keyboard input listening.
  static class KeyboardExtensions
    // Listens for keyboard events and invokes callbacks on key presses.
    // view: The UIView to render into.
    // onKeyDown: Invoked when a key is pressed down.
    // onKeyUp: Invoked when a key is released.
    // keys: Optional filter: only forward events for these key names (use Key constants). When null, all key events are forwarded.
    // global: When true (default), listens on the document level. When false, listens only on the wrapper element.
    // requireCtrlOrMeta: When true, the CLIENT ignores events without Ctrl or Cmd held — the filter every ⌘X-style shortcut needs. Filtering only in the server callback is not enough: preventDefault applies client-side to every matched key, so a bare-key shortcut without this flag swallows that letter in every text field of the app (a global ["k"] + preventDefault listener made the letter k untypeable product-wide).
    // preventDefault: When true, prevents the default browser behavior for matched keys. For modifier shortcuts, pair with requireCtrlOrMeta — see its remarks.
    // stopPropagation: When true, stops event propagation for matched keys.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void KeyboardListener(this UIView view, Func<KeyboardEventArgs, Task>? onKeyDown = null, Func<KeyboardEventArgs, Task>? onKeyUp = null, IReadOnlyList<string>? keys = null, bool? global = true, bool? requireCtrlOrMeta = null, bool? preventDefault = null, bool? stopPropagation = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Extension methods for scroll area and layout components.
  static class LayoutExtensions
    // Maintains a specific aspect ratio for content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // ratio: The width-to-height ratio to maintain (e.g., 16.0/9.0 for widescreen).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void AspectRatio(this UIView view, string[]? style = null, double ratio = 1.0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Provides text direction context (ltr/rtl) to descendants.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // dir: Text direction for descendants.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void DirectionProvider(this UIView view, string[]? style = null, Dir dir = Ltr, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb; rootStyle rarely needed.
    // view: The UIView to render into.
    // threshold: Distance from end (in pixels) to trigger onNearEnd. Default 200.
    // debounceMs: Debounce time in ms to prevent rapid callback firing. Default 100.
    // loading: When true, shows loading indicator and prevents duplicate callbacks.
    // hasMore: When false, disables the onNearEnd callback (end of data reached).
    // direction: Whether to detect scroll near end going Down (append) or Up (prepend).
    // scrollbars: Which scrollbars to display.
    // loadingIndicator: Builder for custom loading indicator content.
    // onNearEnd: Invoked when user scrolls near the end of content.
    // content: Builder function for rendering child elements.
    // viewportStyle: Style classes for the viewport element. Use ScrollArea.Viewport for default styling.
    // scrollbarStyle: Style classes for the scrollbar elements. Use ScrollArea.Scrollbar for default styling.
    // thumbStyle: Style classes for the scrollbar thumb elements. Use ScrollArea.Thumb for default styling.
    // rootStyle: Style for the outermost container. Rarely needed; prefer styling the viewport instead.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    // props: Additional properties passed directly to the underlying component.
    static void InfiniteScrollView(this UIView view, string[]? style = null, int threshold = 200, int debounceMs = 100, bool loading = false, bool hasMore = true, ScrollDirection direction = Down, ScrollAreaScrollbars scrollbars = Vertical, Action<UIView>? loadingIndicator = null, Func<ScrollNearEndArgs, Task>? onNearEnd = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // SemanticTone.Success, SemanticTone.Warning, and SemanticTone.Error tones map to the matching Theming.Progress.Variant tokens; other tones use the default (brand) fill. Style slots: rootStyle → Progress.Root, indicatorStyle → Progress.Indicator.
    // view: The UIView to render into.
    // value: Controlled value representing current progress.
    // max: Maximum value for the progress indicator.
    // tone: Semantic tone of the indicator fill.
    // indeterminate: When true, displays an indeterminate progress animation.
    // getValueLabel: Function to format the value for display.
    // rootStyle: Style classes for the progress track/container. Use Progress.Root for default styling.
    // indicatorStyle: Style classes for the progress indicator element. Use Progress.Indicator for default styling.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void Progress(this UIView view, string[]? style = null, double? value = null, double? max = null, SemanticTone tone = Neutral, bool indeterminate = false, Func<double?, string>? getValueLabel = null, string[]? rootStyle = null, string[]? indicatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Resizable split panel with a drag handle between two panes. Resize is handled entirely on the client — only the final size is sent to the server via onResized.
    static void ResizableSplit(this UIView view, Orientation orientation = Horizontal, double initialSize = 200.0, double minSize = 100.0, double maxSize = 500.0, bool reversed = false, Func<double, Task>? onResized = null, Action<UIView>? first = null, Action<UIView>? second = null, string[]? style = null, string[]? handleStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Style slots (default theme tokens): viewportStyle → ScrollArea.Viewport, scrollbarStyle → ScrollArea.Scrollbar, thumbStyle → ScrollArea.Thumb, cornerStyle (when both scrollbars show); rootStyle rarely needed.
    // view: The UIView to render into.
    // scrollbars: Which scrollbars to display (Vertical, Horizontal, or Both).
    // type: Scrollbar visibility behavior (Always, Scroll, Hover, or Auto).
    // scrollHideDelay: Delay in milliseconds before hiding scrollbars when type is Scroll or Hover.
    // dir: Text direction (Ltr or Rtl).
    // autoScroll: When true, automatically scrolls to the bottom when content changes. Ideal for chat interfaces.
    // autoScrollKey: Anything whose value changes when the content changes — auto-scroll re-fires when it does. Pass the collection itself (autoScrollKey: _messages — any reactive contributes its change version), a count (autoScrollKey: _messages.Count), or a composite string. Required when autoScroll is true.
    // content: Builder function for rendering child elements within this component.
    // viewportStyle: Style classes for the viewport element. Use ScrollArea.Viewport for default styling.
    // scrollbarStyle: Style classes for the scrollbar elements. Use ScrollArea.Scrollbar for default styling.
    // thumbStyle: Style classes for the scrollbar thumb elements. Use ScrollArea.Thumb for default styling.
    // cornerStyle: Style classes for the corner element.
    // rootStyle: Style for the outermost ScrollArea container. Rarely needed; prefer styling the viewport instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void ScrollArea(this UIView view, string[]? style = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType type = Hover, int? scrollHideDelay = null, Dir dir = Ltr, bool autoScroll = false, object? autoScrollKey = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? cornerStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // ScrollArea — positional (style, children) overload (see ContainerExtensions.Box).
    static void ScrollArea(this UIView view, string[]? style, Action<UIView> children)
    // Visual separator between content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // orientation: Whether the separator is horizontal or vertical.
    // decorative: When true, the separator is purely visual and not announced by screen readers.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void Separator(this UIView view, string[]? style = null, Orientation orientation = Horizontal, bool decorative = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Event returned from a geolocation action with latitude/longitude coordinates.
  sealed record LocationActionEvent : ActionEvent
    ctor(bool Success, double? Latitude, double? Longitude, double? Accuracy)
    double? Accuracy { get; init; }
    double? Latitude { get; init; }
    double? Longitude { get; init; }
  // Specifies the behavior of a CaptureButton when pressed.
  enum MediaCaptureButtonMode
    // Start capturing when pressed; stop capturing when released.
    Hold
    // Toggle capturing on and off when pressed.
    Toggle
  // Event data for media capture start/stop callbacks, containing the stream identifier and capture kind. ClientContext identifies the user who initiated the capture and is populated for all capture kinds (audio, camera, screen). Prefer reading ClientSessionId / UserId rather than tracking streamId-to-client mappings yourself.
  sealed record MediaCaptureEvent
    ctor(string StreamId, MediaCaptureKind Kind)
    // Client context of the user who initiated the capture.
    Context? ClientContext { get; init; }
    // Client session id of the user who initiated the capture.
    int? ClientSessionId { get; }
    MediaCaptureKind Kind { get; init; }
    string StreamId { get; init; }
    // User id of the user who initiated the capture.
    string? UserId { get; }
  // Specifies the type of media to capture with a CaptureButton.
  enum MediaCaptureKind
    // Capture audio from the user's microphone.
    Audio
    // Capture video from the user's camera.
    Camera
    // Capture the user's screen.
    Screen
  // Extension methods for media playback components.
  static class MediaExtensions
    // Audio player for URL-based audio content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // url: URL of the audio source.
    // controls: When true, displays audio playback controls.
    // autoplay: When true, audio starts playing automatically.
    // loop: When true, audio loops continuously.
    // muted: When true, audio is muted.
    // preload: Specifies if/how the audio should be loaded when the page loads ("none", "metadata", or "auto").
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void AudioUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, string? preload = null, string? styleId = null, string? key = null)
    // Animated audio level bars — the "you are being heard" cue. Pure presentation: it renders the same loop whatever the microphone is doing, because per-frame amplitude would cost a server round trip per frame and the point of this cue is that it costs none. The usual placement is over the text input, so pressing a PushToTalkButton visibly turns the field into a recording surface rather than leaving a button to look toggled. Give the row containing both the button and the wave the group class and style the wave's container with Theming.MicButton.WhileCapturing: the reveal then keys on the client-stamped data-ikon-capture-active attribute and lands on press, with no server involvement.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged over Theming.AudioWave.Root.
    // bars: How many bars to draw.
    // barStyle: Style for each bar. Defaults to Theming.AudioWave.Bar.
    // key: Stable diffing key.
    static void AudioWave(this UIView view, string[]? style = null, int bars = 7, string[]? barStyle = null, string? key = null)
    // Button that captures media (audio, camera, or screen) based on the specified kind. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // kind: The type of media to capture (Audio, Camera, or Screen).
    // text: Text displayed on the button. When content is provided, becomes aria-label for accessibility.
    // captureMode: Whether to hold the button to capture (Hold) or toggle capture on/off (Toggle).
    // audioOptions: Audio capture configuration options. Used when kind is Audio.
    // videoOptions: Video capture configuration options. Used when kind is Camera or Screen.
    // holdReleaseDelayMs: In Hold mode, delays stopping capture by this many milliseconds after the button is released. Useful for speech capture where users may release the button slightly before finishing their sentence.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onCaptureStart: Invoked when capture begins. The parameter contains capture event details.
    // onCaptureStop: Invoked when capture ends. The parameter contains capture event details.
    // content: Builder function for rendering child elements within this component. When provided together with text, enables icon mode where text becomes aria-label.
    static void CaptureButton(this UIView view, string[]? style = null, MediaCaptureKind kind = Audio, string? text = null, MediaCaptureButtonMode captureMode = Hold, ClientAudioCaptureOptions? audioOptions = null, ClientVideoCaptureOptions? videoOptions = null, int? holdReleaseDelayMs = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Toggle microphone button: a CaptureButton(kind: Audio, captureMode: Toggle). Tap to open the microphone, tap again to close it — the segment in between is one utterance. After enabling speech recognition once (Audio.UseSpeechRecognition(...)), subscribe to Audio.SpeechRecognizedAsync to receive the transcription when the mic is toggled off, exactly like PushToTalkButton. Ships the same MicButton.Default themed default: the button stays visibly red (via the zero-latency data-ikon-capture-active attribute) for as long as the mic is open — essential for a toggle, where an invisible open mic means recording without knowing it. A custom style array replaces the default; start with "default" to layer, or include MicButton.Active.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // text: Text or icon shown on the button.
    // audioOptions: Audio capture configuration options (auto gain, noise suppression, etc.).
    // disabled: When true, prevents user interaction.
    // styleId: CSS class name to apply directly. Prefer style.
    // key: Stable diffing key.
    // props: Additional properties forwarded to the underlying component.
    // onCaptureStart: Optional callback fired when the mic opens (audio path; race-free with frame handlers).
    // onCaptureStop: Optional callback fired when the mic closes.
    // content: Optional child content; when provided, text becomes the aria-label.
    static void MicToggleButton(this UIView view, string[]? style = null, string? text = "🎤", ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Enable speech recognition once via Audio.UseSpeechRecognition(...), then subscribe to Audio.SpeechRecognizedAsync to receive transcriptions when the button is released; the initiating user's client context is carried on the event args.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // text: Text or icon shown on the button.
    // holdReleaseDelayMs: Delay before stopping capture after release. Useful for trailing-syllable tolerance.
    // audioOptions: Audio capture configuration options (auto gain, noise suppression, etc.).
    // disabled: When true, prevents user interaction.
    // styleId: CSS class name to apply directly. Prefer style.
    // key: Stable diffing key.
    // props: Additional properties forwarded to the underlying component.
    // onCaptureStart: Optional callback fired when capture begins (audio path; race-free with frame handlers).
    // onCaptureStop: Optional callback fired when capture ends.
    // content: Optional child content; when provided, text becomes the aria-label.
    static void PushToTalkButton(this UIView view, string[]? style = null, string? text = "⏺", int holdReleaseDelayMs = 500, ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Canvas element for rendering a live video stream.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // streamId: Identifier of the video stream to display.
    // width: Width of the canvas in pixels.
    // height: Height of the canvas in pixels.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void VideoStreamCanvas(this UIView view, string[]? style = null, string? streamId = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
    // Video player for URL-based video content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // url: URL of the video source.
    // controls: When true, displays video playback controls.
    // autoplay: When true, video starts playing automatically.
    // loop: When true, video loops continuously.
    // muted: When true, video is muted.
    // playsInline: When true, plays inline on mobile devices instead of fullscreen.
    // poster: URL of the poster image shown before playback.
    // width: Width of the video player in pixels.
    // height: Height of the video player in pixels.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void VideoUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, bool? playsInline = null, string? poster = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  // Extension methods for NavigationMenu, Menubar, and Toolbar components.
  static class NavigationExtensions
    // Menubar root container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // dir: Text direction (ltr/rtl).
    // loop: When true, keyboard navigation wraps.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void Menubar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Checkbox item in menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // isChecked: Checked state for checkbox items.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onCheckedChange: Invoked when checked changes.
    // content: Builder function for rendering child elements within this component.
    static void MenubarCheckboxItem(this UIView view, string[]? style = null, CheckedState isChecked = Unchecked, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onCheckedChange = null, Action<UIView>? content = null)
    // Dropdown content for the menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // loop: When true, keyboard navigation wraps.
    // side: Which side content appears on.
    // align: Content alignment.
    // sideOffset: Pixel offset from anchor on the side axis.
    // alignOffset: Pixel offset from anchor on the align axis.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarContent(this UIView view, string[]? style = null, bool loop = true, Side side = Bottom, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Clickable menu item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onSelect: Invoked when item is selected.
    // content: Builder function for rendering child elements within this component.
    static void MenubarItem(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // Visual indicator for checkbox/radio state.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarItemIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual menu in the menubar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarMenu(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Radio group in menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void MenubarRadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Radio item in menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarRadioItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Separator between menu items.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void MenubarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Submenu container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when open state changes.
    // content: Builder function for rendering child elements within this component.
    static void MenubarSub(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Content for submenu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // loop: When true, keyboard navigation wraps.
    // side: Which side content appears on.
    // align: Content alignment.
    // sideOffset: Pixel offset from anchor on the side axis.
    // alignOffset: Pixel offset from anchor on the align axis.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarSubContent(this UIView view, string[]? style = null, bool loop = true, Side side = Right, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Trigger for submenu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarSubTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Button that opens a menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void MenubarTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Navigation menu root.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // orientation: Layout orientation.
    // delayDuration: Timing delay in milliseconds.
    // skipDelayDuration: Skip delay duration in milliseconds.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenu(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, int? delayDuration = null, int? skipDelayDuration = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Content shown when navigation item is active.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Visual indicator for active navigation item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual navigation menu item.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuItem(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Link within navigation menu.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // active: Whether item is marked as active.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onSelect: Invoked when item is selected.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuLink(this UIView view, string[]? style = null, bool? active = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // List of navigation menu items.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuList(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Trigger that opens navigation content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Viewport for navigation menu content.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // forceMount: When true, keeps content in DOM when hidden.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void NavigationMenuViewport(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toolbar container.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // orientation: Layout orientation.
    // dir: Text direction (ltr/rtl).
    // loop: When true, keyboard navigation wraps.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void Toolbar(this UIView view, string[]? style = null, Orientation orientation = Horizontal, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Button in the toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onClick: Invoked when the button is clicked.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarButton(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null)
    // Link in the toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // href: URL to navigate to.
    // target: Link target attribute.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarLink(this UIView view, string[]? style = null, string? href = null, string? target = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Separator in the toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    static void ToolbarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Multi-select toggle group in toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active items.
    // defaultValue: Initial value for uncontrolled mode.
    // rovingFocus: Whether roving focus is enabled.
    // loop: When true, keyboard navigation wraps.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Single-select toggle group in toolbar.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // defaultValue: Initial value for uncontrolled mode.
    // rovingFocus: Whether roving focus is enabled.
    // loop: When true, keyboard navigation wraps.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when value changes.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggle item in toolbar toggle group.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // value: Controlled value identifying the active item.
    // disabled: When true, prevents user interaction with this component.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // content: Builder function for rendering child elements within this component.
    static void ToolbarToggleItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Represents the orientation for components like Tabs, Slider, etc.
  enum Orientation
    Horizontal
    Vertical
  // Overlay components (Dialog, AlertDialog, Popover, Tooltip, HoverCard, Toast). Each handles Portal/Overlay management automatically.
  static class OverlayExtensions
    // Style slots: overlayStyle → AlertDialog.Overlay, contentStyle → AlertDialog.Content, titleStyle → AlertDialog.Title, descriptionStyle → AlertDialog.Description, footerStyle → AlertDialog.Footer, cancelStyle → AlertDialog.Cancel, actionStyle → AlertDialog.Action.
    // view: The UIView to render into.
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // title: Title text for the alert dialog.
    // description: Description text for the alert dialog.
    // cancelLabel: Label for the cancel button. Defaults to "Cancel".
    // actionLabel: Label for the action button.
    // onAction: Callback invoked when the action button is clicked.
    // trigger: Builder function for the element that triggers the alert dialog.
    // contentSlot: Builder function for the alert dialog content. When provided, overrides title/description/action parameters for full custom control.
    // overlayStyle: Style classes for the background overlay. Use AlertDialog.Overlay for default styling.
    // overlayStyleId: CSS class name for the overlay.
    // contentStyle: Style classes for the dialog content container. Use AlertDialog.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // titleStyle: Style classes for the title. Use AlertDialog.Title for default styling.
    // descriptionStyle: Style classes for the description. Use AlertDialog.Description for default styling.
    // footerStyle: Style classes for the footer container. Use AlertDialog.Footer for default styling.
    // cancelStyle: Style classes for the cancel button. Use AlertDialog.Cancel for default styling.
    // actionStyle: Style classes for the action button. Use AlertDialog.Action for default styling.
    // rootStyle: Style for the outermost AlertDialog container. Rarely needed; prefer styling the overlay and content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void AlertDialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? title = null, string? description = null, string? cancelLabel = null, string? actionLabel = null, Func<Task>? onAction = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? cancelStyle = null, string[]? actionStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Style slots: overlayStyle → Dialog.Overlay, contentStyle → Dialog.Content.
    // view: The UIView to render into.
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // modal: When true, prevents interaction with elements behind the dialog.
    // trigger: Builder function for the element that triggers the dialog.
    // contentSlot: Builder function for the dialog content.
    // content: Builder function for rendering child elements within this component.
    // overlayStyle: Style classes for the background overlay. Use Dialog.Overlay for default styling.
    // overlayStyleId: CSS class name for the overlay.
    // contentStyle: Style classes for the dialog content container. Use Dialog.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // rootStyle: Style for the outermost Dialog container. Rarely needed; prefer styling the overlay and content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void Dialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? headerStyle = null)
    // Style slots: contentStyle → HoverCard.Content.
    // view: The UIView to render into.
    // style: Alias for contentStyle — on a hover card the surface the ergonomic first-positional styles is the floating content panel. Ignored when contentStyle is also provided (contentStyle wins).
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // openDelay: Delay in milliseconds before showing the card.
    // closeDelay: Delay in milliseconds before hiding the card after mouse leaves.
    // trigger: Builder function for the element that triggers the hover card.
    // contentSlot: Builder function for the hover card content.
    // content: Builder function for rendering child elements within this component.
    // contentStyle: Style classes for the hover card content. Use HoverCard.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // rootStyle: Style for the HoverCard container. Rarely needed; prefer styling the content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void HoverCard(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: contentStyle → Popover.Content.
    // view: The UIView to render into.
    // style: Alias for contentStyle — on a popover the surface the ergonomic first-positional styles is the floating content panel. Ignored when contentStyle is also provided (contentStyle wins).
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // modal: When true, prevents interaction with elements outside the popover.
    // side: Side of the trigger the content is rendered against.
    // align: Alignment of the content relative to the trigger along the chosen side.
    // sideOffset: Distance in pixels between the trigger and the content along side.
    // alignOffset: Offset in pixels of the content from the aligned edge.
    // trigger: Builder function for the element that triggers the popover.
    // contentSlot: Builder function for the popover content.
    // content: Builder function for rendering child elements within this component.
    // contentStyle: Style classes for the popover content container. Use Popover.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // rootStyle: Style for the outermost Popover container. Rarely needed; prefer styling the content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void Popover(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Style slots: toastStyle → Toast.Default, viewportStyle → Toast.Viewport, titleStyle → Toast.Title, descriptionStyle → Toast.Description, closeStyle → Toast.Close.
    // view: The UIView to render into.
    // type: Toast type affecting layering behavior (Foreground or Background).
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // durationMs: Duration in milliseconds before auto-dismiss.
    // forceMount: When true, keeps the toast in the DOM even when closed.
    // swipeDirection: Direction to swipe to dismiss the toast.
    // swipeThreshold: Distance in pixels required to trigger a swipe dismiss.
    // title: Title text for the toast.
    // description: Description text for the toast.
    // showClose: Whether to show the close button. Defaults to true when using simplified API.
    // closeLabel: Label for the close button. Defaults to "×".
    // content: Builder function for rendering child elements within this component. When provided, overrides title/description/close parameters for full custom control.
    // toastStyle: Style classes for the toast container. Use Toast.Default for default styling.
    // viewportStyle: Style classes for the viewport where toasts are rendered. Use Toast.Viewport for default styling.
    // titleStyle: Style classes for the title. Use Toast.Title for default styling.
    // descriptionStyle: Style classes for the description. Use Toast.Description for default styling.
    // closeStyle: Style classes for the close button. Use Toast.Close for default styling.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    // onEscapeKeyDown: Invoked when the escape key is pressed.
    // onPause: Invoked when the toast timer pauses (e.g., on hover).
    // onResume: Invoked when the toast timer resumes.
    // onSwipeStart: Invoked when a swipe gesture starts.
    // onSwipeMove: Invoked during a swipe gesture.
    // onSwipeEnd: Invoked when a swipe gesture completes.
    // onSwipeCancel: Invoked when a swipe gesture is cancelled.
    static void Toast(this UIView view, string[]? style = null, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string? title = null, string? description = null, bool? showClose = null, string? closeLabel = null, Action<UIView>? content = null, string[]? toastStyle = null, string[]? viewportStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Func<Task>? onEscapeKeyDown = null, Func<Task>? onPause = null, Func<Task>? onResume = null, Func<ToastSwipeArgs, Task>? onSwipeStart = null, Func<ToastSwipeArgs, Task>? onSwipeMove = null, Func<ToastSwipeArgs, Task>? onSwipeEnd = null, Func<ToastSwipeArgs, Task>? onSwipeCancel = null)
    // Style slots: contentStyle → Tooltip.Content.
    // view: The UIView to render into.
    // style: Alias for contentStyle — on a tooltip the surface the ergonomic first-positional styles is the floating content bubble. Ignored when contentStyle is also provided (contentStyle wins).
    // open: Controlled open state.
    // defaultOpen: Initial open state for uncontrolled mode.
    // delayDuration: Delay in milliseconds before showing the tooltip.
    // skipDelayDuration: Delay in milliseconds when switching between tooltips.
    // disableHoverableContent: When true, prevents hoverable content from keeping the tooltip open.
    // trigger: Builder function for the element that triggers the tooltip.
    // contentSlot: Builder function for the tooltip content.
    // content: Builder function for rendering child elements within this component.
    // contentStyle: Style classes for the tooltip content. Use Tooltip.Content for default styling.
    // contentStyleId: CSS class name for the content container.
    // rootStyle: Style for the Tooltip container. Rarely needed; prefer styling the content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    static void Tooltip(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
  // Composite overlay-menu components built on the Popover/Dialog primitives and the Menu/Combobox/ Command theme tokens — the shadcn Combobox, DropdownMenu, Command-palette, and Kbd, expressed as C# composites (no bespoke node type). Filtering is server-side over the app's reactive search state, matching Parallax's reactive model; client-side typeahead/roving-focus is a later renderer concern, not required for the components to work.
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
    // searchPlaceholder: Placeholder in the search field.
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
  // One page of items plus the controls needed to render prev/next buttons. Returned by PaginationExtensions.Paginate<T>.
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
    // True if there is a next page.
    bool CanNext { get; init; }
    // True if there is a previous page.
    bool CanPrev { get; init; }
    // Action that jumps to page 0.
    Func<Task> First { get; init; }
    // Zero-based current page index.
    int Index { get; init; }
    // The slice of Source for the current page.
    IReadOnlyList<T> Items { get; init; }
    // Action that moves to a specific page (0-based). Clamps to valid range.
    Func<int, Task> JumpTo { get; init; }
    // Action that jumps to the last page.
    Func<Task> Last { get; init; }
    // Action to bind to a Next button's onClick. Increments page; no-op at last.
    Func<Task> Next { get; init; }
    // Items per page (the configured page size, not necessarily Items.Count).
    int PageSize { get; init; }
    // Action to bind to a Prev button's onClick. Decrements page; no-op at first.
    Func<Task> Prev { get; init; }
    // The full input list, if the caller wants the original.
    IReadOnlyList<T> Source { get; init; }
    // Total number of pages (always >= 1, even when Source is empty).
    int TotalPages { get; init; }
  // Bounded-cursor primitive on top of ClientReactive<T>. Slices an in-memory list, returns the slice + bound actions (Prev/Next/JumpTo/First/Last) the caller binds to whatever UI fits. Holds zero rendering opinion — no tab bars, no default control rows, no opinionated layout. Most Ikon apps don't need pagination at all (live feeds, autoscroll, virtualization handle the common cases via ReactiveList<T> + ScrollArea(autoScroll: true)). Use this when you have a static list large enough to warrant explicit page navigation. For DB-backed pagination (load only the current page from a backend), drive ClientReactive<T> directly and observe its value in your data-loading code — same per-client semantics, no special helper needed.
  static class PaginationExtensions
    // page must be a field-level ClientReactive<T>; each client sees its own page, and the returned slice is a snapshot read once, not a live view.
    // view: UIView (extension receiver — unused, present for fluency).
    // items: Source list. Read once; the slice is a snapshot, not a live view.
    // page: Per-client page index. Use a field-level ClientReactive<T> initialized to 0.
    // pageSize: Items per page (must be >= 1; clamped if not).
    static Page<T> Paginate<T>(this UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  // Options for the Contact Picker API action.
  sealed record PickContactsActionOptions : ActionOptions
    ctor()
    // When true, allows selecting multiple contacts.
    bool Multiple { get; init; }
  // Event args for pointer down outside events on overlays.
  sealed record PointerDownOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Extension methods for QR code generation.
  static class QrCodeExtensions
    // QR code image. Generates a QR code server-side and renders it as an image.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for styling.
    // value: The text or URL to encode in the QR code.
    // size: Size of the QR code in pixels (default 256).
    // key: Unique identifier to assist stable diffing across renders.
    static void QR(this UIView view, string[]? style = null, string? value = null, int size = 256, string? key = null)
  // Extension methods for the RichTextEditor component.
  static class RichTextEditorExtensions
    // Values are HTML strings. A controlled value with no write-back handler (onValueChange or onSubmit) renders the editor read-only.
    // view: The UIView to render into.
    // value: Controlled HTML value. A controlled value with no write-back handler (no onValueChange, no onSubmit) renders the editor read-only, since edits would have nowhere to go.
    // defaultValue: Initial HTML value for uncontrolled mode.
    // placeholder: Placeholder text shown when empty.
    // disabled: When true, prevents user interaction.
    // tools: Explicit toolbar contents. When null, a sensible default toolbar is shown.
    // showToolbar: When false, hides the toolbar entirely.
    // minRows: Minimum number of text rows.
    // maxRows: Maximum number of text rows before scrolling.
    // style: Style for the outermost container. Use RichTextEditor.Root.
    // toolbarStyle: Style for the toolbar. Use RichTextEditor.Toolbar.
    // toolbarButtonStyle: Style for toolbar buttons. Use RichTextEditor.ToolbarButton.
    // contentStyle: Style for the editable content area. Use RichTextEditor.Content.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the HTML value changes.
    // onSubmit: Invoked when the user presses Ctrl+Enter.
    static void RichTextEditor(this UIView view, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, IReadOnlyList<RichTextTool>? tools = null, bool? showToolbar = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? toolbarStyle = null, string[]? toolbarButtonStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  // Formatting action available in the RichTextEditorExtensions.RichTextEditor toolbar.
  enum RichTextTool
    // Bold toggle.
    Bold
    // Italic toggle.
    Italic
    // Underline toggle.
    Underline
    // Strikethrough toggle.
    Strikethrough
    // Convert block to H1.
    Heading1
    // Convert block to H2.
    Heading2
    // Convert block to H3.
    Heading3
    // Convert block to paragraph.
    Paragraph
    // Align text left.
    AlignLeft
    // Align text center.
    AlignCenter
    // Align text right.
    AlignRight
    // Bullet list.
    BulletList
    // Numbered list.
    NumberedList
    // Block quote.
    Blockquote
    // Inline or block code.
    Code
    // Insert link.
    Link
    // Clear inline formatting.
    ClearFormatting
    // Undo.
    Undo
    // Redo.
    Redo
  // Tiny primitives for using ClientReactive<T> as a signal the app reads to decide what to render. Routes, tabs, modes, panel selections, "which dialog is open" — same shape, same primitives. Intentionally minimal: no opinionated tab bars, no URL coupling, no rendering bias. The signal is the building block; the app decides how to consume it. For URL ↔ signal sync (browser bar, deep links, back/forward), use Navigation on the host app — keeps URL concerns in one place instead of forking them through this layer.
  static class RoutingExtensions
    // Renders the content for the currently-active key. signal holds the active key (per-client); cases maps each known key to a render lambda. Falls back to fallback (or empty) when the active key isn't in the dictionary. private ClientReactive<string> _route = new("home"); ... view.Routed(_route, new() { ["home"] = v => RenderHome(v), ["about"] = v => RenderAbout(v), ["settings"] = v => RenderSettings(v), });
    static void Routed<T>(this UIView view, ClientReactive<T> signal, Dictionary<T, Action<UIView>> cases, Action<UIView>? fallback = null)
    // Returns an onClick-shaped handler that sets the signal to a constant value. Convenience for the very common "button that activates a specific route/tab/mode" case so the caller doesn't write a lambda at every call site. view.Button(text: "Open settings", onClick: view.Set(_route, "settings"));
    static Func<Task> Set<T>(this UIView view, ClientReactive<T> signal, T value)
  // Represents which scrollbars to show in a ScrollArea.
  enum ScrollAreaScrollbars
    None
    Vertical
    Horizontal
    Both
  // Represents the scrollbar visibility type for ScrollArea.
  enum ScrollAreaType
    Auto
    Always
    Scroll
    Hover
  // Extension methods for the ScrollColumn primitive — a header/body/footer dialog pattern where the body scrolls. Wraps a LayoutExtensions.ScrollArea with the correct flex sizing so scrolling engages without ceremony.
  static class ScrollColumnExtensions
    // Canonical shape for dialogs, side panels, and chat layouts needing fixed chrome around an overflowing region; avoids the flex-1 ScrollArea that won't shrink inside a flex parent (the min-height: auto quirk). The outer height is the caller's responsibility — set it via style (e.g. "h-[82vh]") or let a flex-1 parent provide bounds.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes for the outer flex column. Provide a bounded height here (e.g. h-[82vh]).
    // header: Optional builder for the pinned top region.
    // footer: Optional builder for the pinned bottom region.
    // content: Builder for the scrollable body region.
    // scrollbars: Which scrollbars to display inside the body (default ScrollAreaScrollbars.Vertical).
    // scrollType: Scrollbar visibility behavior.
    // autoScroll: Auto-scroll the body to bottom when content changes — ideal for chat.
    // autoScrollKey: Anything whose value changes when the content changes — pass the reactive collection itself, a count, or a composite string (see LayoutExtensions.ScrollArea).
    // bodyStyle: Extra utility classes applied to the ScrollArea root (rarely needed).
    // viewportStyle: Extra utility classes applied to the ScrollArea viewport.
    // scrollbarStyle: Extra utility classes applied to the ScrollArea scrollbar.
    // thumbStyle: Extra utility classes applied to the ScrollArea thumb.
    // styleId: CSS class name to apply directly to the outer column. For exceptional cases.
    // key: Unique identifier to assist stable diffing across renders.
    static void ScrollColumn(this UIView view, string[]? style = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType scrollType = Hover, bool autoScroll = false, object? autoScrollKey = null, string[]? bodyStyle = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string? styleId = null, string? key = null)
  // Direction for infinite scroll loading.
  enum ScrollDirection
    Down
    Up
  // Event args for when user scrolls near the end of content.
  sealed record ScrollNearEndArgs
    ctor(double ScrollTop, double ScrollHeight, double ClientHeight, ScrollDirection Direction)
    double ClientHeight { get; init; }
    ScrollDirection Direction { get; init; }
    double ScrollHeight { get; init; }
    double ScrollTop { get; init; }
  // Extension methods for Select components.
  static class SelectExtensions
    // An Input.* token passed as the Select's own style is ignored (with a dev warning) — it would style the outer wrapper, not the field element; the trigger already carries the field theme, so customize it through triggerStyle. Trigger sizing uses Select.Size tokens ([Select.Size.Sm] / [Select.Size.Lg], default medium) in triggerStyle.
    // view: The UIView to render into.
    // options: A flat list of selectable options.
    // groups: Grouped selectable options with optional labels.
    // value: The controlled value of the selected option. A controlled value with no write-back handler (no bind, no onValueChange) renders the select read-only, since a change would have nowhere to go.
    // defaultValue: The default value when initially rendered. Use when not controlling the state.
    // placeholder: Text displayed when no value is selected.
    // disabled: When true, prevents user interaction with this component.
    // required: When true, indicates a selection must be made before the form can be submitted.
    // open: The controlled open state of the dropdown.
    // name: The name of the select for form submission.
    // triggerStyle: Additional styles for the trigger button. Use Select.Size.* for sizing.
    // contentStyle: Additional styles for the dropdown content panel.
    // itemStyle: Additional styles for each selectable item in the dropdown.
    // itemIndicatorStyle: Additional styles for the selected item indicator (checkmark).
    // indicatorIconName: The name of the icon to display for the selected item indicator.
    // rootStyle: Styles for the root Select container. Rarely needed; prefer triggerStyle for most customizations.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using style parameters.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // onOpenChange: Invoked when the open state changes. The parameter is true when opening, false when closing.
    // label: Optional field label rendered above the select (same field ergonomics as TextField).
    // bind: Two-way binds the select to a Reactive<T> in one call — reads bind.Value for the selected value and writes it back on every selection. Use this instead of pairing value: with a manual onValueChange; when set, value: is ignored and onValueChange still fires after the write-back.
    // ariaLabel: Accessible name for assistive technology, for a control whose visible content cannot supply one — an icon, a swatch, a bare input. Prefer a visible label where the design allows one; this is for when there is nothing on screen to read.
    static void Select(this UIView view, string[]? style = null, IReadOnlyList<SelectOption>? options = null, IReadOnlyList<SelectOptionGroup>? groups = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, bool? required = null, bool? open = null, string? name = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? itemStyle = null, string[]? itemIndicatorStyle = null, string? indicatorIconName = "check", string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null, Reactive<string>? bind = null, string? ariaLabel = null)
  // Represents a selectable option in a Select component.
  sealed record SelectOption
    ctor(string Value, string Label, bool Disabled = false)
    bool Disabled { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  // Represents a group of selectable options in a Select component.
  sealed record SelectOptionGroup
    ctor(string? Label, IReadOnlyList<SelectOption> Options)
    string? Label { get; init; }
    IReadOnlyList<SelectOption> Options { get; init; }
  // Tones resolve to the theme's semantic color tokens, so they render correctly in both light and dark mode.
  enum SemanticTone
    // Neutral grey — default, carries no signal.
    Neutral
    // Brand accent color.
    Brand
    // Positive / confirmation (green family).
    Success
    // Caution (amber family).
    Warning
    // Failure / destructive (red family).
    Error
    // Informational (blue family).
    Info
  // A typed uniform value to pass to a WebGL shader. Use the static factory methods to create instances.
  readonly struct ShaderUniform
    // The GLSL type name (e.g. "float", "vec2", "vec3").
    string Type { get; }
    // The uniform value.
    object Value { get; }
    // Creates a boolean uniform.
    static ShaderUniform Bool(bool value)
    // Creates a float uniform.
    static ShaderUniform Float(float value)
    // Creates an integer uniform.
    static ShaderUniform Int(int value)
    // Creates a vec2 uniform from two floats.
    static ShaderUniform Vec2(float x, float y)
    // Creates a vec3 uniform from three floats.
    static ShaderUniform Vec3(float x, float y, float z)
    // Creates a vec4 uniform from four floats.
    static ShaderUniform Vec4(float x, float y, float z, float w)
  // Extension methods for WebGL shader components.
  static class ShadertoyExtensions
    // Renders GLSL fragment shaders with Shadertoy-compatible uniforms. The shader code must define a mainImage function with signature: void mainImage(out vec4 color, in vec2 fragCoord) Built-in uniforms (automatically provided): • iResolution (vec3) - canvas width, height, and 1.0 • iTime (float) - elapsed time in seconds • iTimeDelta (float) - time since last frame • iFrame (int) - current frame number • iMouse (vec4) - mouse x, y, click x, click y (requires enableMouse=true) • iDate (vec4) - year, month, day, seconds of day Texture channels: Pass image URLs (data URIs or http(s)) via channels to bind them to the Shadertoy channel uniforms, matching Shadertoy's default sampler behavior so shaders copied from shadertoy.com that sample 2D textures render the same way: • iChannel0..iChannel3 (sampler2D) - channel textures, in array order • iChannelResolution[4] (vec3) - per-channel pixel size (0 until loaded) • iChannelTime[4] (float) - always 0 for static images Textures use Shadertoy's defaults: vertical flip on (upright with uv = fragCoord/iResolution), repeat wrap, and mipmap filtering. Sample with texture(iChannel0, uv). Limitations: 2D image channels only - no cubemap (samplerCube), buffer, audio, or video channels; single output only.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes. The array renders exactly as passed; lead it with the "default" marker (or a complete Theming.* composite like Theming.Button.PrimaryMd) to merge the component's themed default underneath. Omitting style: renders that default alone — layout primitives have none.
    // shaderSource: GLSL fragment shader source code.
    // fps: Target frames per second for shader rendering.
    // uniforms: Custom uniform values to pass to the shader.
    // channels: Image URLs (data URIs or http(s)) bound to iChannel0..3 in order. Up to four.
    // enableMouse: When true, passes mouse position as a uniform.
    // width: Width of the shader canvas in pixels.
    // height: Height of the shader canvas in pixels.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void ShadertoyCanvas(this UIView view, string[]? style = null, string? shaderSource = null, int? fps = null, IReadOnlyDictionary<string, ShaderUniform>? uniforms = null, IReadOnlyList<string>? channels = null, bool? enableMouse = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  // Options for the Web Share API action.
  sealed record ShareActionOptions : ActionOptions
    ctor()
    // Text body for the shared content.
    string? Text { get; init; }
    // Title for the shared content.
    string? Title { get; init; }
    // URL to share.
    string? Url { get; init; }
  // Slide-over panel composites (Sheet, Drawer) built on the Dialog primitive. The dialog's portal + content styling is repositioned per side via the Theming.Sheet / Theming.Drawer token recipes, including Crosswind slide-in/out motion classes driven by the panel's data-state attribute.
  static class SheetExtensions
    // Same open/close model as Sheet: in controlled mode (open set) pass onOpenChange and flip your state to false there, or the drawer cannot be dismissed.
    // view: The UIView to render into.
    // open: Controlled open state.
    // onOpenChange: Invoked when the open state changes (true when opening, false when closing).
    // title: Title rendered in the drawer header.
    // description: Muted description rendered under the title.
    // trigger: Builder for the element that opens the drawer (uncontrolled mode).
    // content: Builder for the drawer body.
    // footer: Builder for the footer (actions column).
    // defaultOpen: Initial open state for uncontrolled mode.
    // modal: When true, prevents interaction with elements behind the drawer.
    // showHandle: Whether to render the drag handle bar at the top of the panel.
    // style: Crosswind/Tailwind utility classes merged on top of Theming.Drawer.Content.
    // overlayStyle: Style for the background overlay. Defaults to Theming.Drawer.Overlay.
    // handleStyle: Style for the drag handle. Defaults to Theming.Drawer.Handle.
    // headerStyle: Style for the header container. Defaults to Theming.Drawer.Header.
    // titleStyle: Style for the title text. Defaults to Theming.Drawer.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.Drawer.Description.
    // footerStyle: Style for the footer container. Defaults to Theming.Drawer.Footer.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void Drawer(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showHandle = true, string[]? style = null, string[]? overlayStyle = null, string[]? handleStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string? key = null)
    // In controlled mode (open set) pass onOpenChange and flip your state to false there, or the close button and outside clicks cannot dismiss the sheet. Caller styles replace the themed panel token, or merge over it with a leading "default" marker.
    // view: The UIView to render into.
    // open: Controlled open state.
    // onOpenChange: Invoked when the open state changes (true when opening, false when closing).
    // side: Screen edge the panel is anchored to and slides in from.
    // title: Title rendered in the sheet header.
    // description: Muted description rendered under the title.
    // trigger: Builder for the element that opens the sheet (uncontrolled mode).
    // content: Builder for the sheet body.
    // footer: Builder for the footer (actions row).
    // defaultOpen: Initial open state for uncontrolled mode.
    // modal: When true, prevents interaction with elements behind the sheet.
    // showClose: Whether to render the × close button in the top-right corner.
    // style: Crosswind/Tailwind utility classes merged on top of the themed panel token.
    // overlayStyle: Style for the background overlay. Defaults to Theming.Sheet.Overlay.
    // headerStyle: Style for the header container. Defaults to Theming.Sheet.Header.
    // titleStyle: Style for the title text. Defaults to Theming.Sheet.Title.
    // descriptionStyle: Style for the description text. Defaults to Theming.Sheet.Description.
    // footerStyle: Style for the footer container. Defaults to Theming.Sheet.Footer.
    // closeStyle: Style for the close button. Defaults to Theming.Sheet.CloseButton.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void Sheet(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, Side side = Right, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showClose = true, string[]? style = null, string[]? overlayStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? closeStyle = null, string? key = null)
  // Represents the side for positioning overlays.
  enum Side
    Top
    Right
    Bottom
    Left
  // Extension methods for the Skeleton component.
  static class SkeletonExtensions
    // Pulsing placeholder block for loading / not-yet-available content — the visual stand-in used while real content is pending, and the default fill for content redacted from the build-time boot snapshot (see SnapshotReveal). A typed convenience over the Skeleton.* theme tokens (a div with animate-pulse styling); size and shape via size / shape, or override freely through style.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes appended to the base skeleton styling (e.g. an explicit width).
    // shape: Outline shape — Rectangle (default), Circle, or Square.
    // size: Height preset — Xs, Sm, Md (default), Lg, or Xl.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    static void Skeleton(this UIView view, string[]? style = null, SkeletonShape shape = Rectangle, SkeletonSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Outline shape of a SkeletonExtensions.Skeleton placeholder.
  enum SkeletonShape
    Rectangle
    Circle
    Square
  // Height preset for a SkeletonExtensions.Skeleton placeholder.
  enum SkeletonSize
    Xs
    Sm
    Md
    Lg
    Xl
  // Wrappers for controlling how the UI renders into the build-time boot snapshot. The boot snapshot is a public asset painted to everyone before the live connection, so by default the snapshot render replaces every content leaf with a skeleton — per-user content can never leak. These wrappers let the app override that default for specific regions, branching on UIView.IsSnapshot at build time so it keeps a single UI.Root definition instead of two separate UIs. On the normal live render path every wrapper is a single bool check plus the content the developer already wrote.
  static class SnapshotExtensions
    // Renders content live but omits it entirely from the boot snapshot — not even a skeleton placeholder.
    static void SnapshotHide(this UIView view, Action<UIView> content)
    // Renders content only in the boot snapshot, never live; the filler is rendered as authored (not auto-skeletonized).
    static void SnapshotOnly(this UIView view, Action<UIView> content)
    // Renders content as real content in the boot snapshot instead of skeletons — use only for content safe to bake into the public snapshot (logos, static chrome, marketing copy). The opt-out covers the whole subtree.
    static void SnapshotReveal(this UIView view, Action<UIView> content)
  // Represents sort strategy for @dnd-kit SortableContext.
  enum SortStrategy
    VerticalList
    HorizontalList
  // Contains information about a reorder operation in SortableList.
  sealed record SortableReorderArgs
    ctor(string ActiveId, string OverId, int OldIndex, int NewIndex, IReadOnlyList<string> NewOrder)
    string ActiveId { get; init; }
    int NewIndex { get; init; }
    IReadOnlyList<string> NewOrder { get; init; }
    int OldIndex { get; init; }
    string OverId { get; init; }
  // Size of the loading Spinner.
  enum SpinnerSize
    Sm
    Md
    Lg
  // Trend direction for a CardExtensions.StatCard delta.
  enum StatTrend
    // No direction — the delta renders in a neutral tone without an arrow.
    Flat
    // Upward trend — trending-up arrow in the success tone.
    Up
    // Downward trend — trending-down arrow in the error tone.
    Down
  // Represents sticky behavior for Select/DropdownMenu.
  enum Sticky
    Partial
    Always
  // Defines a tab for use with the Tabs component.
  record TabItem
    // Value: Unique identifier for the tab.
    // Label: Text label displayed on the tab trigger.
    // Content: Builder function for rendering the tab's content panel.
    // Disabled: When true, prevents user interaction with this tab.
    // ForceMount: When true, the tab's content is mounted in the DOM even when inactive (Radix hides via data-state="inactive"). Use this for heavy panels you want to amortise into initial paint and keep mounted across tab switches; the trade-off is a slower first render and any mount-time effects firing on hidden panels.
    ctor(string Value, string Label, Action<UIView> Content, bool Disabled = false, bool ForceMount = false)
    // Builder function for rendering the tab's content panel.
    Action<UIView> Content { get; init; }
    // When true, prevents user interaction with this tab.
    bool Disabled { get; init; }
    // When true, the tab's content is mounted in the DOM even when inactive (Radix hides via data-state="inactive"). Use this for heavy panels you want to amortise into initial paint and keep mounted across tab switches; the trade-off is a slower first render and any mount-time effects firing on hidden panels.
    bool ForceMount { get; init; }
    // Text label displayed on the tab trigger.
    string Label { get; init; }
    // Unique identifier for the tab.
    string Value { get; init; }
  // Lightweight semantic table composites — the styled middle ground between hand-rolled Grid/Row layouts and the payload-driven DataTable component. Uses CSS table display utilities, so columns align automatically without a shared grid template:
  // view.Table(content: t =>
  // {
  //     t.TableHeader(content: h => h.TableRow(content: r =>
  //     {
  //         r.TableHead("Name");
  //         r.TableHead("Status");
  //     }));
  //     t.TableBody(content: b =>
  //     {
  //         foreach (var user in users)
  //         {
  //             b.TableRow(key: user.Id, striped: true, content: r =>
  //             {
  //                 r.TableCell(user.Name);
  //                 r.TableCell(content: c => c.Badge(user.Status, SemanticTone.Success));
  //             });
  //         }
  //     });
  // });
  static class TableExtensions
    // Table container (CSS display: table). Compose with TableHeader, TableBody, TableRow, TableHead, and TableCell. Caller styles replace the base token; lead the array with "default" to merge over it.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the table base token.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for the table's header/body groups.
    static void Table(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Table — positional (style, children) overload.
    static void Table(this UIView view, string[]? style, Action<UIView> children)
    // Body row group (CSS display: table-row-group).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the row group token.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for the body rows.
    static void TableBody(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Data cell (CSS display: table-cell).
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the cell token.
    // text: Cell text. For custom content use content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for custom cell content.
    static void TableCell(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // TableCell — positional-text-first overload: r.TableCell(user.Name).
    static void TableCell(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // Header cell (CSS display: table-cell) with muted uppercase column-label styling.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the header cell token.
    // text: Column label. For custom content use content instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying element.
    // content: Builder function for custom cell content.
    static void TableHead(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // TableHead — positional-text-first overload: r.TableHead("Name").
    static void TableHead(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // Header row group (CSS display: table-header-group). Put one TableRow of TableHead cells inside.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the header group token.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for the header rows.
    static void TableHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Table row (CSS display: table-row) with a bottom border. Rows with onClick also get hover highlight + pointer cursor.
    // view: The UIView to render into.
    // style: Crosswind/Tailwind utility classes merged on top of the row token.
    // striped: When true, even rows get a subtle background (zebra striping via CSS :nth-child).
    // onClick: Invoked when the user clicks the row. Accepts sync (() => …) and async (async () => …) lambdas alike.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // content: Builder function for the row's cells.
    static void TableRow(this UIView view, string[]? style = null, bool striped = false, Delegate? onClick = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
  // Extension methods for Tabs components.
  static class TabsExtensions
    // Style slots (default theme tokens): listStyle → Tabs.List, triggerStyle → Tabs.Trigger, contentStyle → Tabs.Content; rootStyle is the outer container (rarely needed).
    // view: The UIView to render into.
    // value: Controlled value identifying the active tab/item.
    // defaultValue: Initial value for uncontrolled mode.
    // orientation: Layout orientation (horizontal or vertical).
    // activationMode: How tabs are activated: Automatic (on focus) or Manual (on click/enter).
    // tabs: Collection of tab definitions that defines all tabs.
    // listContainerStyle: Style for an optional Box wrapper around the TabsList. When provided, wraps the list in a styled container.
    // listStyle: Style for the TabsList container. Use Tabs.List for default styling.
    // triggerStyle: Default style for enabled tab triggers. Use Tabs.Trigger for default styling.
    // disabledTriggerStyle: Style for disabled tab triggers. If not provided, uses triggerStyle for all triggers.
    // contentContainerStyle: Style for an optional Box wrapper around all TabsContent panels. When provided, wraps content in a styled container.
    // contentStyle: Default style for all content panels. Use Tabs.Content for default styling.
    // rootStyle: Style for the outermost Tabs container. Rarely needed; prefer styling the list and triggers instead.
    // styleId: CSS class name to apply directly. For exceptional cases; prefer using the style parameter with Crosswind utilities.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the user changes the value. The parameter contains the new value.
    // lazyPanels: When true (controlled tabs only), the server builds only the active tab's panel plus any TabItem.ForceMount panels; a tab switch then fetches the new panel in the same round-trip that confirms the switch, and the client keeps the old panel visible until the new one arrives (no flicker). Opt in for apps with many or heavy tabs to cut per-client server memory and wire size by roughly the tab count — the trade-off is one network round-trip of latency per switch instead of an instant client-side swap. Default false: every panel ships and switching is instant. Ignored for uncontrolled tabs, which switch client-side and therefore always need every panel.
    static void Tabs(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, ActivationMode activationMode = Automatic, IEnumerable<TabItem>? tabs = null, string[]? listContainerStyle = null, string[]? listStyle = null, string[]? triggerStyle = null, string[]? disabledTriggerStyle = null, string[]? contentContainerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, bool lazyPanels = false)
  // Smallest time unit shown by a TimePickerExtensions.TimePicker.
  enum TimeGranularity
    // Hours only.
    Hour
    // Hours and minutes.
    Minute
    // Hours, minutes, and seconds.
    Second
  // Extension methods for TimePicker components.
  static class TimePickerExtensions
    // Values are ISO-8601 HH:mm or HH:mm:ss strings; the emitted value is always 24-hour regardless of hourFormat. A controlled value without onValueChange renders read-only.
    // view: The UIView to render into.
    // value: Controlled value (HH:mm or HH:mm:ss).
    // defaultValue: Initial value for uncontrolled mode.
    // hourFormat: 12h or 24h display. Emitted value is always 24h.
    // granularity: Smallest unit shown.
    // minuteStep: Minute step (1, 5, 10, 15, 30…). Defaults to 1.
    // secondStep: Second step. Defaults to 1.
    // disabled: When true, prevents user interaction.
    // open: Controlled open state of the popover.
    // defaultOpen: Initial open state for uncontrolled mode.
    // side: Side the popover content renders against.
    // align: Alignment of the content along the chosen side.
    // placeholder: Text shown in the trigger when no time is selected.
    // triggerStyle: Style for the trigger button. Use TimePicker.Trigger.
    // contentStyle: Style for the popover content container. Use TimePicker.Content.
    // columnStyle: Style for each hour/minute/second column.
    // itemStyle: Style for a single time option.
    // itemSelectedStyle: Style for the selected time option.
    // rootStyle: Style for the outermost container. Rarely needed.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    // props: Additional properties passed directly to the underlying component.
    // onValueChange: Invoked when the selected time changes.
    // onOpenChange: Invoked when the popover open state changes.
    static void TimePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, HourFormat hourFormat = Hour24, TimeGranularity granularity = Minute, int? minuteStep = null, int? secondStep = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string? placeholder = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? columnStyle = null, string[]? itemStyle = null, string[]? itemSelectedStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // One notification held by a Toasts queue.
  sealed record ToastItem
    // Id: Queue-unique identifier used to dismiss the toast.
    // Title: Headline text.
    // Description: Optional muted body text.
    // Tone: Semantic tone controlling the icon and its color.
    // DurationMs: Milliseconds before the client auto-dismisses the toast.
    ctor(long Id, string Title, string? Description, SemanticTone Tone, int DurationMs)
    // Optional muted body text.
    string? Description { get; init; }
    // Milliseconds before the client auto-dismisses the toast.
    int DurationMs { get; init; }
    // Queue-unique identifier used to dismiss the toast.
    long Id { get; init; }
    // Headline text.
    string Title { get; init; }
    // Semantic tone controlling the icon and its color.
    SemanticTone Tone { get; init; }
  // Event args for toast swipe events.
  sealed record ToastSwipeArgs
    ctor(ToastSwipeDirection Direction, double DeltaX, double DeltaY)
    double DeltaX { get; init; }
    double DeltaY { get; init; }
    ToastSwipeDirection Direction { get; init; }
  // Represents swipe direction for Toast.
  enum ToastSwipeDirection
    Left
    Right
    Up
    Down
  // Represents the type of Toast (foreground/background).
  enum ToastType
    Foreground
    Background
  // Wiring: construct one instance as an app field, mount ToastsExtensions.ToastHost once in the root UI, then fire notifications (e.g. _toasts.Success(...)) from any handler. State lives in a ClientReactive<T>, so methods must be called where a client scope is active (UI render or event handlers) and each client sees only its own toasts. Auto-dismiss is client-driven off ToastItem.DurationMs.
  sealed class Toasts
    ctor()
    // Toasts currently visible for the calling client (reactive read).
    IReadOnlyList<ToastItem> Items { get; }
    // Remove all toasts from the calling client's queue.
    void Clear()
    // Remove one toast from the calling client's queue.
    void Dismiss(long id)
    // Enqueue an error toast.
    long Error(string title, string? description = null, int durationMs = 5000)
    // Enqueue an info toast.
    long Info(string title, string? description = null, int durationMs = 5000)
    // Enqueue a toast for the calling client.
    // title: Headline text.
    // description: Optional muted body text.
    // tone: Semantic tone controlling the icon and its color.
    // durationMs: Milliseconds before the client auto-dismisses the toast.
    long Show(string title, string? description = null, SemanticTone tone = Neutral, int durationMs = 5000)
    // Enqueue a success toast.
    long Success(string title, string? description = null, int durationMs = 5000)
    // Enqueue a warning toast.
    long Warning(string title, string? description = null, int durationMs = 5000)
    // Default auto-dismiss duration in milliseconds.
    const int DefaultDurationMs = 5000
  // Host composite that renders a Toasts queue with the toast primitives.
  static class ToastsExtensions
    // Render the toast viewport for a Toasts queue. Mount exactly once in the root UI; every queued toast renders as a themed toast (tone icon, title, description, close button) that the client auto-dismisses after its duration. Both auto-dismiss and the close button report back and remove the item from the queue.
    // view: The UIView to render into.
    // toasts: The queue to render.
    // viewportStyle: Style for the toast viewport. Defaults to Theming.Toast.Viewport.
    // toastStyle: Crosswind/Tailwind utility classes merged on top of Theming.Toast.Default for each toast.
    // titleStyle: Style for the title. Defaults to Theming.Toast.Title.
    // descriptionStyle: Style for the description. Defaults to Theming.Toast.Description.
    // closeStyle: Style for the close button. Defaults to Theming.Toast.Close.
    // showClose: Whether to render the × close button on each toast.
    static void ToastHost(this UIView view, Toasts toasts, string[]? viewportStyle = null, string[]? toastStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, bool showClose = true)
  // Recursive tree composite over the Collapsible primitive, styled with the NavPanel/NavItem token recipes.
  static class TreeViewExtensions
    // Expansion state lives in a caller-held ExpandedSet — declare it as an app field (private readonly ExpandedSet _expanded = new();). Clicking a branch toggles its expansion and selects it in the same click.
    // view: The UIView to render into.
    // roots: Top-level nodes.
    // id: Stable unique id per node — used for diff keys, expansion, and selection.
    // label: Visible text per node.
    // children: Child nodes per node; null or empty marks a leaf.
    // expanded: Per-client expansion state. Declare as an app field: private readonly ExpandedSet _expanded = new();
    // style: Crosswind/Tailwind utility classes merged on top of Theming.NavPanel.Ghost for the tree container.
    // onSelect: Invoked when a row is clicked (branches toggle and select on the same click).
    // selectedId: Id of the currently selected node, rendered with the active item style.
    // icon: Optional per-node Lucide icon name rendered before the label.
    // itemStyle: Style for rows. Defaults to Theming.NavItem.Md + Theming.NavItem.Default.
    // selectedItemStyle: Style for the selected row. Defaults to Theming.NavItem.Md + Theming.NavItem.Active.
    // labelStyle: Style for row labels. Defaults to Theming.NavItem.Label.
    // childrenStyle: Style for the nested children container (indent + guide line).
    // styleId: CSS class name to apply directly. For exceptional cases; prefer the style parameter.
    // key: Unique identifier to assist stable diffing across renders in exceptional cases.
    static void TreeView<T>(this UIView view, IReadOnlyList<T> roots, Func<T, string> id, Func<T, string> label, Func<T, IReadOnlyList<T>?> children, ExpandedSet expanded, string[]? style = null, Func<T, Task>? onSelect = null, string? selectedId = null, Func<T, string?>? icon = null, string[]? itemStyle = null, string[]? selectedItemStyle = null, string[]? labelStyle = null, string[]? childrenStyle = null, string? styleId = null, string? key = null)
  // Performance model: the server emits one wrapper node per item up to itemCount and runs every per-item content builder eagerly server-side (keep content trees inexpensive); the client mounts only the wrappers inside [start - overscan, end + overscan] and leaves the rest out of the DOM entirely. onNearEnd fires when the window enters the last nearEndThreshold rows — append items to grow the list.
  static class VirtualListExtensions
    // DOM-virtualized scrollable grid. Items are laid out in a fixed number of columns and rows outside the visible window are not mounted in the DOM.
    // view: The UIView to render into.
    // itemCount: Total number of items in the grid.
    // columns: Fixed number of columns. Ignored when minItemWidthPx is set.
    // rowHeight: Fixed height in pixels for every row. Ignored when aspectRatio is set.
    // onRenderItem: Callback invoked per item with its zero-based index. Builds the cell content.
    // overscan: Extra rows to render before/after the visible window. Default 2.
    // gap: Pixels of gap between rows and between columns. Default 12.
    // minItemWidthPx: When set, column count is computed from container width / minItemWidthPx, overriding columns. Use for responsive layouts.
    // maxColumns: Upper bound on auto-computed columns (only relevant with minItemWidthPx).
    // aspectRatio: Ratio of row height to column width (rowHeight = columnWidth × aspectRatio). Use for cells that should scale proportionally with column width across screen sizes (e.g. 1.0 = square, 0.75 = 4:3 landscape, 1.4 = portrait card). Overrides rowHeight when set.
    // resetScrollKey: Optional opaque token that resets the scroll position to the top whenever it changes. Use to reset scroll on filter/sort changes without remounting the grid (which would churn all child actions).
    // onNearEnd: Fires when the user scrolls within nearEndThresholdRows rows of the end.
    // nearEndThresholdRows: Distance from end (in rows) to trigger onNearEnd. Default 2.
    // style: Style for the outermost scrollable viewport container.
    // itemStyle: Style applied to each cell wrapper.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    // props: Additional properties passed directly to the underlying component.
    static void VirtualGrid(this UIView view, int itemCount, int columns, double rowHeight, Action<UIView, int> onRenderItem, int overscan = 2, int gap = 12, int? minItemWidthPx = null, int? maxColumns = null, double? aspectRatio = null, string? resetScrollKey = null, Func<int, Task>? onNearEnd = null, int nearEndThresholdRows = 2, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // DOM-virtualized vertical list with fixed item height. Renders only items inside the visible window plus an overscan buffer.
    // view: The UIView to render into.
    // itemCount: Total number of items in the list. Wrapper divs are emitted for all of them.
    // itemHeight: Fixed height in pixels for every item. Required for windowing math.
    // onRenderItem: Callback invoked per item with its zero-based index. Builds the item content.
    // overscan: Extra items to render before/after the visible window. Default 4.
    // onNearEnd: Fires when the user scrolls within nearEndThreshold items of the end. Use to fetch more data and grow itemCount.
    // nearEndThreshold: Distance from end (in items) to trigger onNearEnd. Default 5.
    // style: Style for the outermost scrollable viewport container.
    // itemStyle: Style applied to each item wrapper. Use sparingly — wrappers are sized by itemHeight.
    // styleId: CSS class name to apply directly.
    // key: Unique identifier to assist stable diffing across renders.
    // props: Additional properties passed directly to the underlying component.
    static void VirtualList(this UIView view, int itemCount, double itemHeight, Action<UIView, int> onRenderItem, int overscan = 4, Func<int, Task>? onNearEnd = null, int nearEndThreshold = 5, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Day of the week used as the first column in the calendar grid.
  enum WeekStart
    // Week starts on Sunday.
    Sunday
    // Week starts on Monday (ISO-8601).
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
  // The animated level bars of view.AudioWave — the "you are being heard" cue that replaces an input field, or sits beside a mic, while audio is captured.
  static class AudioWave
    const string Bar
    // Bar heights in spacing units, cycled across however many bars are asked for. Uneven on purpose: an even ramp reads as a loading spinner rather than a level meter.
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
  // Combobox (searchable Select): a Popover whose trigger shows the current value and whose content is a search field over a filtered option list. Slot tokens for the whole surface; the trigger deliberately reuses the outline Button look so a Combobox and a Select read the same in a form.
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
  // Command palette (the shadcn Command in a dialog): a centred search field over a grouped, filtered action list. Slot tokens for the surface, groups, and rows.
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
  // Defines a UI theme providing base CSS and a default icon library.
  interface ITheme
    // Global CSS injected into the client as the theme baseline.
    string Css { get; }
    // The default icon library name (e.g. "lucide") used when no library is specified on an icon component.
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
    // Per-token override addressed by CSS variable name (without the leading --) or by Tailwind utility token. Set during object initialization.
    string this[string token] { get; set; }
    // How the app relates to light/dark switching. ThemeMode.Adaptive (the default) keeps today's behavior: overrides restyle the light theme, DarkMode restyles the dark one, and the client's theme preference picks between them. ThemeMode.Fixed commits to ONE scheme: every override is also emitted under the dark selectors, so a client-side theme flip cannot pull the platform's dark palette in under the app's committed colors. For atmospheric, game, or brand-locked looks that should never light/dark switch.
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
  // Keyboard-key display (the shadcn Kbd): a small inset chip for a shortcut key or combo. Complete default-marked composite for view.Kbd; the Group wrapper spaces several keys in a combo.
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
  // Rendered-markdown body. The renderer hands react-markdown's plain HTML straight to the document, and the Crosswind preflight zeroes borders and spacing on every element, so a markdown document with no token renders as undifferentiated prose — tables without rules, blockquotes without a bar, fenced code indistinguishable from a paragraph. Deliberately sets no base color, size or width: markdown is embedded in a page that has already chosen those, and a token color here would override the surrounding text. Anchors are styled by the renderer itself.
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
  // Menu-surface primitives (the shadcn DropdownMenuItem / Label / Separator family), for the rows inside popover menus, account menus, and context menus. A menu row is NOT a button look: it rests transparent, fills the row, reads left, and highlights on hover — so these are complete default-marked composites for view.Button rather than additions to the Button tones. Selection/active state stays a caller concern (add bg-brand-selected on the active row).
  static class Menu
    const string Content
    const string Item
    // The destructive row (Log out, Delete) — error text with an error-tinted hover, same geometry as Item.
    const string ItemDestructive
    // A non-interactive section heading between item groups.
    const string Label
    // The thin rule between item groups.
    const string Separator
    // Right-aligned muted shortcut hint on a menu row (pairs with Kbd).
    const string Shortcut
  static class Menubar
    const string Content
    const string Default
    const string Item
    const string Root
    const string Separator
    const string Trigger
  // Microphone capture buttons (PushToTalkButton, MicToggleButton). A mic button must always show its live state: Active keys on the client-stamped data-ikon-capture-active attribute, so the recording feedback is zero-latency — it flips the moment the capture starts, with no server round trip. Compose Active into any custom mic style so recording never becomes invisible.
  static class MicButton
    const string Active
    const string Base
    const string Default
    const string Lg
    const string Md
    const string Sm
    // Reveals its element only while a capture button inside the same group is held. Like Active it keys on the client-stamped attribute, so it lands on press rather than a round trip later — which is what makes a hold-to-talk control read as held rather than toggled. Put group on the row containing both the button and this element; pair with AudioWave for the recording cue.
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
    // Composes the indicator class list from the base recipe, a fill variant (Variant, defaulting to the brand fill), the optional indeterminate shimmer, and caller overrides appended last so they win.
    static string ComposeIndicator(string? variant = null, bool indeterminate = false, params string?[] overrides)
    // Arbitrary-value transform class that fills the indicator to value percent (clamped to 0–100) by translating it left from the fully-filled position.
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
  // Themed native scrollbars, for the container that has to scroll itself — a header strip, a horizontal chip row, anything an overlay-based ScrollArea would over-serve. A bare overflow-auto shows the OS scrollbar, which on Windows is a wide grey slab that matches no theme and moves the layout when it appears. scrollbar-width/scrollbar-color cover Firefox; the ::-webkit-scrollbar rules cover Chrome and Safari, which ignore them. Both axes are sized on purpose: a width alone leaves the HORIZONTAL bar at its default height, which is the usual way this lands half-applied.
  static class Scrollbar
    const string Default
    // No scrollbar at all, still scrollable by wheel, drag and keyboard. Only for a strip whose overflow is obvious from its content (a carousel, a chip row that visibly cuts off) — content that scrolls with nothing to say so is content most people never find.
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
    // Light + dark, switchable (the default). Base overrides restyle the light theme, IkonTheme.DarkMode restyles the dark one, and the client preference picks. Style code should use theme-aware semantic classes for every surface that must adapt.
    Adaptive
    // One committed scheme, no light/dark switching. Every override is emitted for both theme states, so a client-side flip changes nothing the theme defines. Hardcoded palette classes are first-class citizens here — the look is intentionally theme-invariant.
    Fixed
  // The canonical theming vocabulary: shadcn-style theme keys and what they commit. Each alias expands to the canonical CSS variables that make its intent real across every consumer (components, focus rings, native clients). This table is the single source of truth — the theme renderer expands aliases through it, the codegen styling tools fan roles out through it, and the docs drift tests lock the published reference tables to it. Collision policy: `primary` as a THEME KEY means brand (the shadcn reading; the Untitled-UI tiered reading only ever existed on the prefixed utility classes, which are untouched). Bare `accent` and `secondary` are deliberately NOT aliases — their shadcn and Ikon meanings genuinely conflict, so they stay unknown-key warnings instead of guessing.
  static class ThemeVocabulary
    // Every accepted alias, keyed by name.
    static IReadOnlyDictionary<string, ThemeVocabulary.Alias> Aliases { get; }
  // One vocabulary entry: an accepted theme key and the canonical variable keys it commits. Targets are always canonical (never other aliases), so expansion is one step.
  sealed record ThemeVocabulary.Alias
    ctor(string Name, IReadOnlyList<string> Targets, ThemeVocabulary.ValueKind Kind)
    ThemeVocabulary.ValueKind Kind { get; init; }
    string Name { get; init; }
    IReadOnlyList<string> Targets { get; init; }
  // What value shape an alias expects, for docs and tooling.
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

**Theme the native scrollbar when you scroll a container yourself.** A bare `overflow-auto` shows the OS scrollbar — on Windows a wide grey slab that matches no theme and shifts the layout when it appears. `Theming.Scrollbar.Thin` is the themed thin bar (`Scrollbar.Hidden` removes it entirely, for a strip whose overflow is visually obvious):

```csharp
view.Row(["overflow-x-auto gap-2", Scrollbar.Thin], content: chips => ...);
```

It sizes both axes on purpose. Styling only the width leaves a *horizontal* bar at its default height, which is the usual way this lands half-applied — and a horizontally scrolling row is exactly where it is most often needed.

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
view.Button(["..."], text: "Sign in with Google",
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
